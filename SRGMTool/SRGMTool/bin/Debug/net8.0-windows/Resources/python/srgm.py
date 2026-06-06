import sys
import json
import math
import numpy as np
from scipy.optimize import curve_fit

def goel_okumoto(t, a, b):
    return a * (1 - np.exp(-b * t))

def inflection_s_shaped(t, a, b, beta):
    return a * (1 - np.exp(-b * t)) / (1 + beta * np.exp(-b * t))

def yamada_exponential(t, a, r, alpha, beta):
    return a * (1 - np.exp(-r * alpha * (1 - np.exp(-beta * t))))

def weibull(t, a, alpha, beta):
    return a * (1 - np.exp(-((t / beta) ** alpha)))

def compute_aic_bic(n, k, residuals):
    sse = np.sum(residuals**2)
    if sse <= 0:
        sse = 1e-10
    sigma2 = sse / n
    log_likelihood = -n / 2 * (math.log(2 * math.pi * sigma2) + 1)
    aic = 2 * k - 2 * log_likelihood
    bic = k * math.log(n) - 2 * log_likelihood
    return aic, bic

def compute_r2(y_actual, y_predicted):
    ss_res = np.sum((y_actual - y_predicted)**2)
    ss_tot = np.sum((y_actual - np.mean(y_actual))**2)
    if ss_tot == 0:
        return 1.0
    return 1 - ss_res / ss_tot

def compute_adj_r2(r2, n, k):
    if n - k - 1 <= 0:
        return r2
    return 1 - (1 - r2) * (n - 1) / (n - k - 1)

def fit_model(name, func, t_data, y_data, p0, bounds):
    n = len(t_data)
    try:
        popt, _ = curve_fit(func, t_data, y_data, p0=p0, bounds=bounds,
                            maxfev=50000, method='trf')
        y_pred = func(t_data, *popt)
        residuals = y_data - y_pred
        k = len(popt)
        aic, bic = compute_aic_bic(n, k, residuals)
        r2 = compute_r2(y_data, y_pred)
        adj_r2 = compute_adj_r2(r2, n, k)
        curve = [[float(t_data[i]), float(y_pred[i])] for i in range(n)]
        return {
            "params": {p: float(v) for p, v in zip(get_param_names(name), popt)},
            "metrics": {
                "AIC": round(aic, 4),
                "BIC": round(bic, 4),
                "RSquared": round(r2, 6),
                "AdjRSquared": round(adj_r2, 6)
            },
            "curve": curve
        }
    except Exception as e:
        return {"error": str(e)}

def get_param_names(model_name):
    names = {
        "GoelOkumoto":       ["a", "b"],
        "InflectionSShaped": ["a", "b", "beta"],
        "YamadaExponential": ["a", "r", "alpha", "beta"],
        "Weibull":           ["a", "alpha", "beta"]
    }
    return names.get(model_name, [])

def main():
    if len(sys.argv) < 2:
        print(json.dumps({"error": "No CSV file path provided"}))
        sys.exit(1)

    csv_path = sys.argv[1]
    try:
        data = np.loadtxt(csv_path, delimiter=',')
        if data.ndim == 1:
            data = data.reshape(1, -1)
        t_data = data[:, 0].astype(float)
        y_data = data[:, 1].astype(float)
    except Exception as e:
        print(json.dumps({"error": f"Invalid CSV format: {e}"}))
        sys.exit(1)

    max_t = int(t_data[-1])
    a_init = float(y_data[-1]) * 1.5

    results = {}

    # Goel-Okumoto
    results["GoelOkumoto"] = fit_model(
        "GoelOkumoto", goel_okumoto, t_data, y_data,
        p0=[a_init, 0.05],
        bounds=([0, 1e-6], [a_init * 10, 20])
    )

    # Inflection S-Shaped
    results["InflectionSShaped"] = fit_model(
        "InflectionSShaped", inflection_s_shaped, t_data, y_data,
        p0=[a_init, 0.05, 1.0],
        bounds=([0, 1e-6, 0], [a_init * 10, 20, 1000])
    )

    # Yamada Exponential
    results["YamadaExponential"] = fit_model(
        "YamadaExponential", yamada_exponential, t_data, y_data,
        p0=[a_init, 1.0, 1.0, 0.1],
        bounds=([0, 1e-6, 1e-6, 1e-6], [a_init * 10, 100, 100, 10])
    )

    # Weibull
    results["Weibull"] = fit_model(
        "Weibull", weibull, t_data, y_data,
        p0=[a_init, 2.0, float(max_t) / 2],
        bounds=([0, 0.1, 0.1], [a_init * 10, 20, float(max_t) * 5])
    )

    # Determine best model by lowest AIC
    best_model = None
    best_aic = float('inf')
    for name, result in results.items():
        if "error" not in result:
            aic = result["metrics"]["AIC"]
            if aic < best_aic:
                best_aic = aic
                best_model = name

    # Generate predictions using best model
    predictions = []
    if best_model:
        model_funcs = {
            "GoelOkumoto":       goel_okumoto,
            "InflectionSShaped": inflection_s_shaped,
            "YamadaExponential": yamada_exponential,
            "Weibull":           weibull
        }
        param_names = get_param_names(best_model)
        params = [results[best_model]["params"][p] for p in param_names]
        func = model_funcs[best_model]
        for future_t in range(max_t + 1, max_t + 40):
            pred_val = float(func(np.array([float(future_t)]), *params)[0])
            predictions.append([future_t, round(pred_val, 2)])

    n = len(t_data)
    output = {
        "models": results,
        "best_model": best_model,
        "data": [[float(t_data[i]), float(y_data[i])] for i in range(n)],
        "predictions": predictions
    }

    print(json.dumps(output))

if __name__ == "__main__":
    main()
