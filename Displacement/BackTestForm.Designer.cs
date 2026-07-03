namespace Displacement
{
    partial class BackTestForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            btnRun = new Button();
            dtStart = new DateTimePicker();
            lblDateStart = new Label();
            lblDateEnd = new Label();
            dtEnd = new DateTimePicker();
            lblTicker = new Label();
            txtTicker = new TextBox();
            cboView = new ComboBox();
            lblView = new Label();
            lblTrendResult = new Label();
            spChart = new ScottPlot.FormsPlot();
            checkCrosshair = new CheckBox();
            SuspendLayout();
            // 
            // btnRun
            // 
            btnRun.Location = new Point(781, 27);
            btnRun.Name = "btnRun";
            btnRun.Size = new Size(75, 23);
            btnRun.TabIndex = 0;
            btnRun.Text = "Run";
            btnRun.UseVisualStyleBackColor = true;
            btnRun.Click += btnRun_Click;
            // 
            // dtStart
            // 
            dtStart.Location = new Point(251, 25);
            dtStart.Name = "dtStart";
            dtStart.Size = new Size(200, 23);
            dtStart.TabIndex = 1;
            // 
            // lblDateStart
            // 
            lblDateStart.AutoSize = true;
            lblDateStart.Location = new Point(179, 31);
            lblDateStart.Name = "lblDateStart";
            lblDateStart.Size = new Size(61, 15);
            lblDateStart.TabIndex = 2;
            lblDateStart.Text = "Date Start:";
            // 
            // lblDateEnd
            // 
            lblDateEnd.AutoSize = true;
            lblDateEnd.Location = new Point(478, 31);
            lblDateEnd.Name = "lblDateEnd";
            lblDateEnd.Size = new Size(57, 15);
            lblDateEnd.TabIndex = 3;
            lblDateEnd.Text = "Date End:";
            // 
            // dtEnd
            // 
            dtEnd.Location = new Point(551, 25);
            dtEnd.Name = "dtEnd";
            dtEnd.Size = new Size(200, 23);
            dtEnd.TabIndex = 4;
            // 
            // lblTicker
            // 
            lblTicker.AutoSize = true;
            lblTicker.Location = new Point(12, 31);
            lblTicker.Name = "lblTicker";
            lblTicker.Size = new Size(42, 15);
            lblTicker.TabIndex = 5;
            lblTicker.Text = "Ticker:";
            // 
            // txtTicker
            // 
            txtTicker.Location = new Point(73, 25);
            txtTicker.Name = "txtTicker";
            txtTicker.Size = new Size(100, 23);
            txtTicker.TabIndex = 6;
            // 
            // cboView
            // 
            cboView.FormattingEnabled = true;
            cboView.Items.AddRange(new object[] { "Swings", "StructureBreaks", "LatestDisplacement", "--------------------", "SupportResistanceZones", "FvgZones", "OrderBlocks", "SupplyDemandZones", "LiquidityZones", "AllZones" });
            cboView.Location = new Point(204, 66);
            cboView.Name = "cboView";
            cboView.Size = new Size(165, 23);
            cboView.TabIndex = 7;
            cboView.SelectedIndexChanged += cboView_SelectedIndexChanged;
            // 
            // lblView
            // 
            lblView.AutoSize = true;
            lblView.Location = new Point(154, 71);
            lblView.Name = "lblView";
            lblView.Size = new Size(35, 15);
            lblView.TabIndex = 8;
            lblView.Text = "View:";
            // 
            // lblTrendResult
            // 
            lblTrendResult.AutoSize = true;
            lblTrendResult.Location = new Point(49, 71);
            lblTrendResult.Name = "lblTrendResult";
            lblTrendResult.Size = new Size(72, 15);
            lblTrendResult.TabIndex = 9;
            lblTrendResult.Text = "TrendResult:";
            // 
            // spChart
            // 
            spChart.AutoSize = true;
            spChart.Location = new Point(27, 115);
            spChart.Margin = new Padding(4, 3, 4, 3);
            spChart.Name = "spChart";
            spChart.Size = new Size(1377, 658);
            spChart.TabIndex = 10;
            // 
            // checkCrosshair
            // 
            checkCrosshair.AutoSize = true;
            checkCrosshair.Location = new Point(1304, 115);
            checkCrosshair.Name = "checkCrosshair";
            checkCrosshair.Size = new Size(75, 19);
            checkCrosshair.TabIndex = 11;
            checkCrosshair.Text = "Crosshair";
            checkCrosshair.UseVisualStyleBackColor = true;
            checkCrosshair.CheckedChanged += checkCrosshair_CheckedChanged;
            // 
            // BackTestForm
            // 
            ClientSize = new Size(1436, 803);
            Controls.Add(checkCrosshair);
            Controls.Add(spChart);
            Controls.Add(lblTrendResult);
            Controls.Add(lblView);
            Controls.Add(cboView);
            Controls.Add(txtTicker);
            Controls.Add(lblTicker);
            Controls.Add(dtEnd);
            Controls.Add(lblDateEnd);
            Controls.Add(lblDateStart);
            Controls.Add(dtStart);
            Controls.Add(btnRun);
            Name = "BackTestForm";
            Text = "Backtesting";
            Load += BackTestForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        private Button btnRun;
        private DateTimePicker dtStart;
        private Label lblDateStart;
        private Label lblDateEnd;
        private DateTimePicker dtEnd;
        private Label lblTicker;
        private TextBox txtTicker;
        private ComboBox cboView;
        private Label lblView;
        private Label lblTrendResult;
        private ScottPlot.FormsPlot spChart;
        private CheckBox checkCrosshair;
    }
}
