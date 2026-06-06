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
| Computation | Python 3 + scipy + numpy + matplotlib |
| Data format | CSV (two columns: time period, cumulative bugs) |
| Communication | C# spawns Python as subprocess, reads JSON output via stdout |

### NuGet packages to install
- `OxyPlot.WinForms` — for rendering model curves
- `Newtonsoft.Json` — for parsing Python JSON output

### Python packages required (user must have these)
```
pip install numpy scipy matplotlib
```

---

## The 4 SRGM Models

All models use `NonlinearModelFit` equivalent via `scipy.optimize.curve_fit`.

### 1. General Goel (studied)
```
m(t) = a * (1 - exp(-b * t^c))
```
Parameters: `a` (total expected bugs), `b` (failure rate), `c` (shape)

### 2. Gompertz-Makeham (studied)
```
m(t) = a * (1 - exp(-λ*t - (α/β)*(exp(β*t) - 1)))
```
Parameters: `a`, `λ`, `α`, `β`

### 3. Zhang (non-studied — the "unstudied model")
```
m(t) = a * (1 - ((1 + α) * exp(-b*t)) / (1 + α * exp(-b*t)))
```
Parameters: `a`, `α`, `b`

### 4. Musa-Okumoto (studied)
```
m(t) = a * ln(1 + b*t)
```
Parameters: `a`, `b`

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
    "GeneralGoel": {
      "params": {"a": 549.1, "b": 0.017, "c": 1.12},
      "metrics": {"AIC": 317.3, "BIC": 324.2, "RSquared": 0.9983, "AdjRSquared": 0.9982},
      "curve": [[1, 7.2], [2, 14.1], ...]
    },
    "GompertzMakeham": { ... },
    "Zhang": { ... },
    "MusaOkumoto": { ... }
  },
  "best_model": "GompertzMakeham",
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
  - `[ ] General Goel`
  - `[ ] Gompertz-Makeham`
  - `[ ] Zhang`
  - `[ ] Musa-Okumoto`
- **GroupBox** "Parameters" with a `RichTextBox` (read-only) showing fitted params for the selected/best model
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

## C# ↔ Python Communication

```csharp
// Pseudocode — implement in a helper class PythonRunner.cs
var process = new Process {
    StartInfo = new ProcessStartInfo {
        FileName = "python",          // or "python3"
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
// Parse json with Newtonsoft.Json
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
1,7
2,8
3,36
...
42,352
```
Column 1 = time period index, Column 2 = cumulative bug count.

---

## Coding Standards

- Use `async/await` for subprocess calls — never block the UI thread
- Keep business logic out of form code — use a `PythonRunner` helper class
- Use `OxyPlot` model/series API correctly — do not draw manually on a Canvas
- Comments in English
- No hardcoded file paths — always use file dialogs or relative paths

---

## What to Build First (suggested order)

1. `srgm.py` — get the Python computation working and outputting correct JSON
2. `MainForm` layout — scaffold the UI with placeholder controls
3. `PythonRunner.cs` — subprocess call + JSON parsing
4. Wire up `Load CSV` → `Run Analysis` → populate chart and table
5. Model checkboxes toggling curves on/off in the chart
6. Parameters panel + predictions table

---

## Sample Data

The file `data/data.csv` contains 42 records from the TROPICO R-1500 switching system test (Brazilian telecom, ~300KB assembly software, 1500 subscribers). Records 1–30 are from the VALIDATION phase, records 31–42 from FIELD TRIALS.
