namespace Displacement
{
    partial class Main
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnStart = new Button();
            txtTickers = new TextBox();
            lblWatchList = new Label();
            label1 = new Label();
            label2 = new Label();
            cboTimeFrame = new ComboBox();
            label3 = new Label();
            dateWatchDateStart = new DateTimePicker();
            gridAlert = new DataGridView();
            chkIsBacktesting = new CheckBox();
            cboSpeed = new ComboBox();
            label5 = new Label();
            label6 = new Label();
            numCandles = new NumericUpDown();
            numATR = new NumericUpDown();
            label7 = new Label();
            lblCandleTime = new Label();
            btnOpenFormBackTest = new Button();
            ((System.ComponentModel.ISupportInitialize)gridAlert).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numCandles).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numATR).BeginInit();
            SuspendLayout();
            // 
            // btnStart
            // 
            btnStart.Location = new Point(403, 225);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(75, 23);
            btnStart.TabIndex = 0;
            btnStart.Text = "Start";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // txtTickers
            // 
            txtTickers.Location = new Point(100, 30);
            txtTickers.Name = "txtTickers";
            txtTickers.Size = new Size(378, 23);
            txtTickers.TabIndex = 1;
            // 
            // lblWatchList
            // 
            lblWatchList.AutoSize = true;
            lblWatchList.Location = new Point(100, 9);
            lblWatchList.Name = "lblWatchList";
            lblWatchList.Size = new Size(183, 15);
            lblWatchList.TabIndex = 2;
            lblWatchList.Text = "Example: Ticker1; Ticker2; Ticker3";
            lblWatchList.Click += lblWatchList_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(20, 38);
            label1.Name = "label1";
            label1.Size = new Size(65, 15);
            label1.TabIndex = 3;
            label1.Text = "Watch List:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 76);
            label2.Name = "label2";
            label2.Size = new Size(73, 15);
            label2.TabIndex = 4;
            label2.Text = "Time Frame:";
            // 
            // cboTimeFrame
            // 
            cboTimeFrame.FormattingEnabled = true;
            cboTimeFrame.Items.AddRange(new object[] { "1mili", "1sec", "1min", "5min", "15min", "1hour", "4hour", "1Day" });
            cboTimeFrame.Location = new Point(100, 76);
            cboTimeFrame.Name = "cboTimeFrame";
            cboTimeFrame.Size = new Size(121, 23);
            cboTimeFrame.TabIndex = 6;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(24, 142);
            label3.Name = "label3";
            label3.Size = new Size(61, 15);
            label3.TabIndex = 7;
            label3.Text = "Date Start:";
            // 
            // dateWatchDateStart
            // 
            dateWatchDateStart.CustomFormat = "MM/dd/yyyy HH:mm";
            dateWatchDateStart.Location = new Point(101, 136);
            dateWatchDateStart.Name = "dateWatchDateStart";
            dateWatchDateStart.Size = new Size(200, 23);
            dateWatchDateStart.TabIndex = 8;
            // 
            // gridAlert
            // 
            gridAlert.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridAlert.Location = new Point(20, 254);
            gridAlert.Name = "gridAlert";
            gridAlert.Size = new Size(847, 144);
            gridAlert.TabIndex = 9;
            // 
            // chkIsBacktesting
            // 
            chkIsBacktesting.AutoSize = true;
            chkIsBacktesting.Location = new Point(101, 106);
            chkIsBacktesting.Name = "chkIsBacktesting";
            chkIsBacktesting.Size = new Size(105, 19);
            chkIsBacktesting.TabIndex = 12;
            chkIsBacktesting.Text = "Is BackTesting?";
            chkIsBacktesting.UseVisualStyleBackColor = true;
            // 
            // cboSpeed
            // 
            cboSpeed.FormattingEnabled = true;
            cboSpeed.Items.AddRange(new object[] { "1x", "2x", "5x", "10x" });
            cboSpeed.Location = new Point(101, 175);
            cboSpeed.Name = "cboSpeed";
            cboSpeed.Size = new Size(121, 23);
            cboSpeed.TabIndex = 13;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(43, 175);
            label5.Name = "label5";
            label5.Size = new Size(42, 15);
            label5.TabIndex = 14;
            label5.Text = "Speed:";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(248, 79);
            label6.Name = "label6";
            label6.Size = new Size(52, 15);
            label6.TabIndex = 16;
            label6.Text = "Candles:";
            // 
            // numCandles
            // 
            numCandles.Location = new Point(306, 77);
            numCandles.Name = "numCandles";
            numCandles.Size = new Size(74, 23);
            numCandles.TabIndex = 17;
            // 
            // numATR
            // 
            numATR.DecimalPlaces = 1;
            numATR.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            numATR.Location = new Point(456, 77);
            numATR.Name = "numATR";
            numATR.Size = new Size(80, 23);
            numATR.TabIndex = 18;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(410, 81);
            label7.Name = "label7";
            label7.Size = new Size(43, 15);
            label7.TabIndex = 19;
            label7.Text = "X-ATR:";
            // 
            // lblCandleTime
            // 
            lblCandleTime.AutoSize = true;
            lblCandleTime.Location = new Point(516, 229);
            lblCandleTime.Name = "lblCandleTime";
            lblCandleTime.Size = new Size(71, 15);
            lblCandleTime.TabIndex = 20;
            lblCandleTime.Text = "CandleTime";
            // 
            // btnOpenFormBackTest
            // 
            btnOpenFormBackTest.Location = new Point(660, 225);
            btnOpenFormBackTest.Name = "btnOpenFormBackTest";
            btnOpenFormBackTest.Size = new Size(102, 23);
            btnOpenFormBackTest.TabIndex = 21;
            btnOpenFormBackTest.Text = "Backtesting";
            btnOpenFormBackTest.UseVisualStyleBackColor = true;
            btnOpenFormBackTest.Click += btnOpenFormBackTest_Click;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(906, 442);
            Controls.Add(btnOpenFormBackTest);
            Controls.Add(lblCandleTime);
            Controls.Add(label7);
            Controls.Add(numATR);
            Controls.Add(numCandles);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(cboSpeed);
            Controls.Add(chkIsBacktesting);
            Controls.Add(gridAlert);
            Controls.Add(dateWatchDateStart);
            Controls.Add(label3);
            Controls.Add(cboTimeFrame);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(lblWatchList);
            Controls.Add(txtTickers);
            Controls.Add(btnStart);
            Name = "Main";
            Text = "Displacement Alert";
            Load += Main_Load;
            ((System.ComponentModel.ISupportInitialize)gridAlert).EndInit();
            ((System.ComponentModel.ISupportInitialize)numCandles).EndInit();
            ((System.ComponentModel.ISupportInitialize)numATR).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnStart;
        private TextBox txtTickers;
        private Label lblWatchList;
        private Label label1;
        private Label label2;
        private ComboBox cboTimeFrame;
        private Label label3;
        private DateTimePicker dateWatchDateStart;
        private DataGridView gridAlert;
        private CheckBox chkIsBacktesting;
        private ComboBox cboSpeed;
        private Label label5;
        private Label label6;
        private NumericUpDown numCandles;
        private NumericUpDown numATR;
        private Label label7;
        private Label lblCandleTime;
        private Button btnOpenFormBackTest;
    }
}
