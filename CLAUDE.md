# SRGM Reliability Analysis Tool — Agent Instructions

## Project Overview
Build a **Windows desktop application** (C# WinForms + .NET) for software reliability analysis using SRGM (Software Reliability Growth Models). The app fits 4 mathematical models to bug/failure data, compares them, and predicts future defects.

A companion **Python script** handles all numerical computation (model fitting, metrics). The C# app calls it as a subprocess and renders the results.

---

## Project Structure

```
SRGMTool/
├── CLAUDE.md                  ← this file
├── SRGMTool.sln
├── SRGMTool/                  ← C# WinForms project
│   ├── SRGMTool.csproj
│   ├── Program.cs
│   ├── MainForm.cs
│   ├── MainForm.Designer.cs
│   └── Resources/
│       └── python/
│           └── srgm.py        ← Python computation script
└── data/
    └── data.csv               ← sample dataset
```

---

## Stack & Tools

| Layer | Technology |
|---|---|
| GUI | C# WinForms, .NET 8 |
| Charts | **OxyPlot.WinForms** NuGet package |
| Computation | Python 3 + scipy + numpy |
| Data format | CSV (two columns: time period, cumulative bugs) |
| Communication | C# spawns Python as subprocess, reads JSON output via stdout |

### NuGet packages to install
- `OxyPlot.WinForms` — for rendering model curves
- `Newtonsoft.Json` — for parsing Python JSON output

### Python packages required (user must have these)
```
pip install numpy scipy
```

---

## The 4 SRGM Models

All models use `scipy.optimize.curve_fit` for parameter estimation.

### 1. Goel-Okumoto (studied — Goel & Okumoto, 1979)
```
m(t) = a * (1 - exp(-b * t))
```
Parameters: `a` (total expected bugs), `b` (failure rate)

### 2. Inflection S-Shaped (studied — Yamada, Ohba & Osaki, 1984)
```
m(t) = a * (1 - exp(-b*t)) / (1 + β * exp(-b*t))
```
Parameters: `a` (total expected bugs), `b` (failure rate), `β` (shape/inflection)

### 3. Yamada Exponential (studied — Yamada, 1986)
```
m(t) = a * (1 - exp(-r * α * (1 - exp(-β*t))))
```
Parameters: `a` (total expected bugs), `r` (testing effort), `α`, `β`

### 4. Weibull SRGM (non-studied — based on Weibull, 1951)
```
m(t) = a * (1 - exp(-(t/β)^α))
```
Parameters: `a` (total expected bugs), `α` (shape), `β` (scale)
Reference: Weibull, W. (1951). "A statistical distribution function of wide applicability." Journal of Applied Mechanics, 18, 293–297.

---

## Python Script (`srgm.py`)

The script must:
1. Accept a CSV file path as a command-line argument: `python srgm.py path/to/data.csv`
2. Read the CSV (two columns: time period, cumulative bugs — no header)
3. Fit all 4 models using `scipy.optimize.curve_fit`
4. Compute for each model: AIC, BIC, R², Adjusted R²
5. Determine the best model (lowest AIC)
6. Generate predictions using the best model for periods `(max_t + 1)` to `(max_t + 39)`
7. Output a single JSON object to stdout with this exact structure:

```json
{
  "models": {
    "GoelOkumoto": {
      "params": {"a": 549.1, "b": 0.017},
      "metrics": {"AIC": 317.3, "BIC": 324.2, "RSquared": 0.9983, "AdjRSquared": 0.9982},
      "curve": [[1, 7.2], [2, 14.1], ...]
    },
    "InflectionSShaped": { ... },
    "YamadaExponential": { ... },
    "Weibull": { ... }
  },
  "best_model": "InflectionSShaped",
  "data": [[1, 7], [2, 8], ...],
  "predictions": [[43, 345], [44, 347], ...]
}
```

If fitting fails for any model, include `"error": "message"` in that model's object and continue with the rest.

---

## C# WinForms UI Layout

### Window
- Title: `SRGM Reliability Analysis Tool`
- Minimum size: `1100 x 700`
- Single `MainForm` — no MDI, no multiple windows

### Layout (use `TableLayoutPanel` for structure)
```
┌─────────────────────────────────────────────────────┐
│  TOOLBAR: [Load CSV]  [Run Analysis]  status label  │
├──────────────┬──────────────────────────────────────┤
│              │                                      │
│  LEFT PANEL  │         CHART (OxyPlot)              │
│  (250px)     │                                      │
│              │                                      │
│  Model       │                                      │
│  checkboxes  │                                      │
│              │                                      │
│  Parameters  │                                      │
│  display     ├──────────────────────────────────────┤
│              │   METRICS TABLE (DataGridView)        │
│              │                                      │
└──────────────┴──────────────────────────────────────┘
```

### Left Panel contents
- **GroupBox** "Models" with 4 checkboxes (all checked by default):
  - `[ ] Goel-Okumoto`
  - `[ ] Inflection S-Shaped`
  - `[ ] Yamada Exponential`
  - `[ ] Weibull`
- **GroupBox** "Parameters" with a `RichTextBox` (read-only) showing fitted params for the best model
- **GroupBox** "Prediction" with a small `DataGridView` showing future periods + predicted cumulative bugs

### Chart area (OxyPlot `PlotView`)
- Show scatter points for the real data (black dots)
- Show one colored line per checked model
- Legend in top-left
- X axis: "Time Period", Y axis: "Cumulative Bugs"
- Each model gets a distinct color: Blue, Red, Green, Orange

### Metrics table (`DataGridView`, bottom right)
Columns: `Model | AIC | BIC | R² | Adj. R²`
- Bold/highlight the row of the best model
- Read-only, no editing

---

## C# Model Name Mappings

| Python key | UI Label |
|---|---|
| `GoelOkumoto` | `Goel-Okumoto` |
| `InflectionSShaped` | `Inflection S-Shaped` |
| `YamadaExponential` | `Yamada Exponential` |
| `Weibull` | `Weibull` |

---

## C# ↔ Python Communication

```csharp
var process = new Process {
    StartInfo = new ProcessStartInfo {
        FileName = "python",
        Arguments = $"srgm.py \"{csvPath}\"",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
        WorkingDirectory = pythonScriptDir
    }
};
process.Start();
string json = process.StandardOutput.ReadToEnd();
string err  = process.StandardError.ReadToEnd();
process.WaitForExit();
```

- Run the subprocess **asynchronously** (use `async/await`) so the UI doesn't freeze
- Show a "Running analysis..." status while waiting
- If `process.ExitCode != 0` or json is empty, show an error MessageBox with the stderr content

---

## Error Handling Rules

- If Python is not found: show MessageBox `"Python not found. Please install Python 3 and ensure it's on your PATH."`
- If CSV has wrong format: Python script should output `{"error": "Invalid CSV format"}` and C# shows it in a MessageBox
- If a model fails to converge: skip it in the chart and table, mark it as `"Failed"` in the metrics table
- Never crash silently — always surface errors to the user

---

## Data Format

The CSV has **no header**, two columns:
```
1,62
2,209
3,444
...
40,9508
```
Column 1 = time period index, Column 2 = cumulative bug count.

---

## Coding Standards

- Use `async/await` for subprocess calls — never block the UI thread
- Keep business logic out of form code — use a `PythonRunner` helper class
- Use `OxyPlot` model/series API correctly — do not draw manually on a Canvas
- Comments in English
- No hardcoded file paths — always use file dialogs or relative paths
