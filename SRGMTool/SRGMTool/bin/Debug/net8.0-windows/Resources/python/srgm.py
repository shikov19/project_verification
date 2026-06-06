import sys
import json
import math
import numpy as np
from scipy.optimize import curve_fit

def general_goel(t, a, b, c):
    return a * (1 - np.exp(-b * t**c))

def gompertz_makeham(t, a, lam, alpha, beta):
    return a * (1 - np.exp(-lam * t - (alpha / beta) * (np.exp(beta * t) - 1)))

def zhang(t, a, alpha, b):
    denom = 1 + alpha * np.exp(-b * t)
    return a * (1 - ((1 + alpha) * np.exp(-b * t)) / denom)

def musa_okumoto(t, a, b):
    return a * np.log(1 + b * t)

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
    r2 = 1 - ss_res / ss_tot
    return r2

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
            "params": {k: float(v) for k, v in zip(
                get_param_names(name), popt)},
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
        "GeneralGoel": ["a", "b", "c"],
        "GompertzMakeham": ["a", "lambda", "alpha", "beta"],
        "Zhang": ["a", "alpha", "b"],
        "MusaOkumoto": ["a", "b"]
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

    n = len(t_data)
    max_t = int(t_data[-1])
    a_init = float(y_data[-1]) * 1.5

    results = {}

    # General Goel
    results["GeneralGoel"] = fit_model(
        "GeneralGoel", general_goel, t_data, y_data,
        p0=[a_init, 0.01, 1.0],
        bounds=([0, 1e-6, 0.01], [a_init * 10, 10, 5])
    )

    # Gompertz-Makeham
    results["GompertzMakeham"] = fit_model(
        "GompertzMakeham", gompertz_makeham, t_data, y_data,
        p0=[a_init, 0.05, 0.001, 0.1],
        bounds=([0, 1e-6, 1e-8, 1e-6], [a_init * 10, 10, 1, 1])
    )

    # Zhang
    results["Zhang"] = fit_model(
        "Zhang", zhang, t_data, y_data,
        p0=[a_init, 1.0, 0.1],
        bounds=([0, 0, 1e-6], [a_init * 10, 100, 10])
    )

    # Musa-Okumoto
    results["MusaOkumoto"] = fit_model(
        "MusaOkumoto", musa_okumoto, t_data, y_data,
        p0=[a_init, 0.01],
        bounds=([0, 1e-6], [a_init * 10, 10])
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
            "GeneralGoel": general_goel,
            "GompertzMakeham": gompertz_makeham,
            "Zhang": zhang,
            "MusaOkumoto": musa_okumoto
        }
        param_names = get_param_names(best_model)
        params = [results[best_model]["params"][p] for p in param_names]
        func = model_funcs[best_model]
        for future_t in range(max_t + 1, max_t + 40):
            pred_val = float(func(np.array([float(future_t)]), *params)[0])
            predictions.append([future_t, round(pred_val, 2)])

    output = {
        "models": results,
        "best_model": best_model,
        "data": [[float(t_data[i]), float(y_data[i])] for i in range(n)],
        "predictions": predictions
    }

    print(json.dumps(output))

if __name__ == "__main__":
    main()
