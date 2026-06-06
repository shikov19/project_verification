using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using OxyPlot.WindowsForms;

namespace SRGMTool
{
    public partial class MainForm : Form
    {
        private AnalysisResult? _result;
        private string? _csvPath;

        private static readonly Dictionary<string, OxyColor> ModelColors = new()
        {
            ["GoelOkumoto"]       = OxyColors.Blue,
            ["InflectionSShaped"] = OxyColors.Red,
            ["YamadaExponential"] = OxyColors.Green,
            ["Weibull"]           = OxyColors.Orange
        };

        private static readonly Dictionary<string, string> ModelLabels = new()
        {
            ["GoelOkumoto"]       = "Goel-Okumoto",
            ["InflectionSShaped"] = "Inflection S-Shaped",
            ["YamadaExponential"] = "Yamada Exponential",
            ["Weibull"]           = "Weibull"
        };

        public MainForm()
        {
            InitializeComponent();
            SetupMetricsTable();
            SetupPredictionTable();
        }

        private void SetupMetricsTable()
        {
            dgvMetrics.Columns.Clear();
            dgvMetrics.Columns.Add(new DataGridViewTextBoxColumn { Name = "Model",       HeaderText = "Model",     Width = 160, ReadOnly = true });
            dgvMetrics.Columns.Add(new DataGridViewTextBoxColumn { Name = "AIC",         HeaderText = "AIC",       Width = 80,  ReadOnly = true });
            dgvMetrics.Columns.Add(new DataGridViewTextBoxColumn { Name = "BIC",         HeaderText = "BIC",       Width = 80,  ReadOnly = true });
            dgvMetrics.Columns.Add(new DataGridViewTextBoxColumn { Name = "RSquared",    HeaderText = "R²",        Width = 80,  ReadOnly = true });
            dgvMetrics.Columns.Add(new DataGridViewTextBoxColumn { Name = "AdjRSquared", HeaderText = "Adj. R²",   Width = 80,  ReadOnly = true });
            dgvMetrics.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void SetupPredictionTable()
        {
            dgvPredictions.Columns.Clear();
            dgvPredictions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Period",   HeaderText = "Period",   Width = 60,  ReadOnly = true });
            dgvPredictions.Columns.Add(new DataGridViewTextBoxColumn { Name = "CumBugs",  HeaderText = "Cum. Bugs", Width = 80,  ReadOnly = true });
            dgvPredictions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void btnLoadCsv_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Open CSV Data File",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _csvPath = dlg.FileName;
                SetStatus($"Loaded: {Path.GetFileName(_csvPath)}");
            }
        }

        private async void btnRunAnalysis_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_csvPath))
            {
                MessageBox.Show("Please load a CSV file first.", "No File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnRunAnalysis.Enabled = false;
            btnLoadCsv.Enabled = false;
            SetStatus("Running analysis...");

            try
            {
                var runner = new PythonRunner();
                _result = await runner.RunAnalysisAsync(_csvPath);
                PopulateUI();
                SetStatus($"Analysis complete. Best model: {_result.BestModel ?? "N/A"}");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Python not found"))
            {
                MessageBox.Show("Python not found. Please install Python 3 and ensure it's on your PATH.",
                    "Python Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("Error: Python not found.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Analysis failed:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("Analysis failed.");
            }
            finally
            {
                btnRunAnalysis.Enabled = true;
                btnLoadCsv.Enabled = true;
            }
        }

        private void PopulateUI()
        {
            if (_result == null) return;
            UpdateChart();
            UpdateMetricsTable();
            UpdateParametersPanel();
            UpdatePredictionsTable();
        }

        private void UpdateChart()
        {
            if (_result == null) return;

            var model = new PlotModel { Title = "SRGM Curve Fitting" };
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Time Period" });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Cumulative Bugs" });

            // Scatter series for actual data
            if (_result.Data != null)
            {
                var scatter = new ScatterSeries
                {
                    Title = "Actual Data",
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 4,
                    MarkerFill = OxyColors.Black
                };
                foreach (var pt in _result.Data)
                    scatter.Points.Add(new ScatterPoint(pt[0], pt[1]));
                model.Series.Add(scatter);
            }

            // Model curves
            if (_result.Models != null)
            {
                foreach (var (name, modelResult) in _result.Models)
                {
                    bool isChecked = GetModelCheckbox(name)?.Checked ?? true;
                    if (!isChecked || modelResult.Curve == null || modelResult.Error != null)
                        continue;

                    var line = new LineSeries
                    {
                        Title = ModelLabels.GetValueOrDefault(name, name),
                        Color = ModelColors.GetValueOrDefault(name, OxyColors.Gray),
                        StrokeThickness = 2
                    };
                    foreach (var pt in modelResult.Curve)
                        line.Points.Add(new DataPoint(pt[0], pt[1]));
                    model.Series.Add(line);
                }
            }

            model.Legends.Add(new Legend { LegendPosition = LegendPosition.TopLeft });
            model.IsLegendVisible = true;
            plotView.Model = model;
        }

        private void UpdateMetricsTable()
        {
            if (_result?.Models == null) return;

            dgvMetrics.Rows.Clear();
            foreach (var (name, modelResult) in _result.Models)
            {
                string label = ModelLabels.GetValueOrDefault(name, name);
                if (modelResult.Error != null)
                {
                    dgvMetrics.Rows.Add(label, "Failed", "Failed", "Failed", "Failed");
                }
                else if (modelResult.Metrics != null)
                {
                    dgvMetrics.Rows.Add(
                        label,
                        modelResult.Metrics.AIC.ToString("F2"),
                        modelResult.Metrics.BIC.ToString("F2"),
                        modelResult.Metrics.RSquared.ToString("F4"),
                        modelResult.Metrics.AdjRSquared.ToString("F4")
                    );
                }
            }

            // Highlight best model row
            if (!string.IsNullOrEmpty(_result.BestModel))
            {
                string bestLabel = ModelLabels.GetValueOrDefault(_result.BestModel, _result.BestModel);
                foreach (DataGridViewRow row in dgvMetrics.Rows)
                {
                    if (row.Cells["Model"].Value?.ToString() == bestLabel)
                    {
                        row.DefaultCellStyle.BackColor = Color.LightYellow;
                        row.DefaultCellStyle.Font = new Font(dgvMetrics.Font, FontStyle.Bold);
                    }
                }
            }
        }

        private void UpdateParametersPanel()
        {
            if (_result?.Models == null || string.IsNullOrEmpty(_result.BestModel)) return;

            var sb = new StringBuilder();
            sb.AppendLine($"Best model: {ModelLabels.GetValueOrDefault(_result.BestModel, _result.BestModel)}");
            sb.AppendLine();

            foreach (var (name, modelResult) in _result.Models)
            {
                sb.AppendLine($"=== {ModelLabels.GetValueOrDefault(name, name)} ===");
                if (modelResult.Error != null)
                {
                    sb.AppendLine($"  Failed: {modelResult.Error}");
                }
                else if (modelResult.Params != null)
                {
                    foreach (var (pName, pVal) in modelResult.Params)
                        sb.AppendLine($"  {pName} = {pVal:G6}");
                }
                sb.AppendLine();
            }

            rtbParameters.Text = sb.ToString();
        }

        private void UpdatePredictionsTable()
        {
            dgvPredictions.Rows.Clear();
            if (_result?.Predictions == null) return;

            foreach (var pred in _result.Predictions)
                dgvPredictions.Rows.Add((int)pred[0], pred[1].ToString("F1"));
        }

        private CheckBox? GetModelCheckbox(string modelName) => modelName switch
        {
            "GoelOkumoto"       => chkGoelOkumoto,
            "InflectionSShaped" => chkInflectionSShaped,
            "YamadaExponential" => chkYamadaExponential,
            "Weibull"           => chkWeibull,
            _ => null
        };

        private void ModelCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (_result != null) UpdateChart();
        }

        private void SetStatus(string text)
        {
            lblStatus.Text = text;
            lblStatus.ForeColor = text.StartsWith("Error") ? Color.DarkRed : SystemColors.GrayText;
        }
    }
}
