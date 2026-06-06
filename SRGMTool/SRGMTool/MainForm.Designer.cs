using System.Windows.Forms;
using System.Drawing;
using OxyPlot.WindowsForms;

namespace SRGMTool
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        // Toolbar
        private Panel pnlToolbar;
        private Button btnLoadCsv;
        private Button btnRunAnalysis;
        private Label lblStatus;

        // Layout
        private TableLayoutPanel mainLayout;
        private TableLayoutPanel leftLayout;
        private TableLayoutPanel rightLayout;

        // Left panel controls
        private GroupBox grpModels;
        private TableLayoutPanel modelsInner;
        private CheckBox chkGeneralGoel;
        private CheckBox chkGompertzMakeham;
        private CheckBox chkZhang;
        private CheckBox chkMusaOkumoto;

        private GroupBox grpParameters;
        private RichTextBox rtbParameters;

        private GroupBox grpPredictions;
        private DataGridView dgvPredictions;

        // Right panel controls
        private PlotView plotView;
        private DataGridView dgvMetrics;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            SuspendLayout();

            // --- Toolbar panel ---
            btnLoadCsv = new Button
            {
                Text = "Load CSV",
                AutoSize = true,
                Margin = new Padding(6, 4, 4, 4)
            };
            btnRunAnalysis = new Button
            {
                Text = "Run Analysis",
                AutoSize = true,
                Margin = new Padding(4, 4, 4, 4)
            };
            lblStatus = new Label
            {
                Text = "Ready",
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                ForeColor = SystemColors.GrayText
            };
            btnLoadCsv.Click += btnLoadCsv_Click;
            btnRunAnalysis.Click += btnRunAnalysis_Click;

            var toolFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(2)
            };
            toolFlow.Controls.Add(btnLoadCsv);
            toolFlow.Controls.Add(btnRunAnalysis);

            pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = SystemColors.ControlLight,
                Padding = new Padding(0)
            };
            pnlToolbar.Controls.Add(lblStatus);
            pnlToolbar.Controls.Add(toolFlow);

            // --- Model checkboxes ---
            chkGeneralGoel     = new CheckBox { Text = "General Goel",     Checked = true, AutoSize = true, Margin = new Padding(4, 4, 4, 2) };
            chkGompertzMakeham = new CheckBox { Text = "Gompertz-Makeham", Checked = true, AutoSize = true, Margin = new Padding(4, 2, 4, 2) };
            chkZhang           = new CheckBox { Text = "Zhang",            Checked = true, AutoSize = true, Margin = new Padding(4, 2, 4, 2) };
            chkMusaOkumoto     = new CheckBox { Text = "Musa-Okumoto",     Checked = true, AutoSize = true, Margin = new Padding(4, 2, 4, 4) };
            chkGeneralGoel.CheckedChanged     += ModelCheckbox_CheckedChanged;
            chkGompertzMakeham.CheckedChanged += ModelCheckbox_CheckedChanged;
            chkZhang.CheckedChanged           += ModelCheckbox_CheckedChanged;
            chkMusaOkumoto.CheckedChanged     += ModelCheckbox_CheckedChanged;

            modelsInner = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1,
                Padding = new Padding(4, 2, 4, 4)
            };
            modelsInner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            modelsInner.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
            modelsInner.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
            modelsInner.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
            modelsInner.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
            modelsInner.Controls.Add(chkGeneralGoel,     0, 0);
            modelsInner.Controls.Add(chkGompertzMakeham, 0, 1);
            modelsInner.Controls.Add(chkZhang,           0, 2);
            modelsInner.Controls.Add(chkMusaOkumoto,     0, 3);

            grpModels = new GroupBox
            {
                Text = "Models",
                Dock = DockStyle.Fill,
                Padding = new Padding(4, 6, 4, 4),
                Margin = new Padding(0, 0, 0, 4)
            };
            grpModels.Controls.Add(modelsInner);

            // --- Parameters ---
            rtbParameters = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = SystemColors.Control,
                Font = new Font("Consolas", 8f),
                ScrollBars = RichTextBoxScrollBars.Vertical,
                BorderStyle = BorderStyle.None
            };
            grpParameters = new GroupBox
            {
                Text = "Parameters",
                Dock = DockStyle.Fill,
                Padding = new Padding(4, 6, 4, 4),
                Margin = new Padding(0, 0, 0, 4)
            };
            grpParameters.Controls.Add(rtbParameters);

            // --- Predictions ---
            dgvPredictions = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.None
            };
            grpPredictions = new GroupBox
            {
                Text = "Predictions (next 39 periods)",
                Dock = DockStyle.Fill,
                Padding = new Padding(4, 6, 4, 4),
                Margin = new Padding(0)
            };
            grpPredictions.Controls.Add(dgvPredictions);

            // --- Left layout (TableLayoutPanel with 3 rows) ---
            leftLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Padding = new Padding(4, 4, 4, 4)
            };
            leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 148f));   // Models group
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));    // Parameters group
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 185f));   // Predictions group
            leftLayout.Controls.Add(grpModels,      0, 0);
            leftLayout.Controls.Add(grpParameters,  0, 1);
            leftLayout.Controls.Add(grpPredictions, 0, 2);

            // --- Plot ---
            plotView = new PlotView { Dock = DockStyle.Fill };

            // --- Metrics table ---
            dgvMetrics = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                BorderStyle = BorderStyle.None
            };

            // --- Right layout ---
            rightLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = new Padding(0, 4, 4, 4)
            };
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 65f));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 35f));
            rightLayout.Controls.Add(plotView,   0, 0);
            rightLayout.Controls.Add(dgvMetrics, 0, 1);

            // --- Main layout ---
            mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2,
                Padding = new Padding(0)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 290f));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            mainLayout.Controls.Add(leftLayout,  0, 0);
            mainLayout.Controls.Add(rightLayout, 1, 0);

            // --- Form ---
            Text = "SRGM Reliability Analysis Tool";
            MinimumSize = new Size(1100, 700);
            Size = new Size(1250, 780);
            Controls.Add(mainLayout);
            Controls.Add(pnlToolbar);

            ResumeLayout(false);
            PerformLayout();
        }
    }
}
