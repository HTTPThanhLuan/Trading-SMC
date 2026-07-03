namespace TradingUI
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnBacktesting = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnBacktesting
            // 
            this.btnBacktesting.Location = new System.Drawing.Point(494, 214);
            this.btnBacktesting.Name = "btnBacktesting";
            this.btnBacktesting.Size = new System.Drawing.Size(75, 23);
            this.btnBacktesting.TabIndex = 0;
            this.btnBacktesting.Text = "Backtesting";
            this.btnBacktesting.UseVisualStyleBackColor = true;
            this.btnBacktesting.Click += new System.EventHandler(this.btnBacktesting_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(790, 432);
            this.Controls.Add(this.btnBacktesting);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnBacktesting;
    }
}

