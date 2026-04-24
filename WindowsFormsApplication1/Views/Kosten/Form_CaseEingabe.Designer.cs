namespace WindowsFormsApplication1
{
    partial class Form_CaseEingabe
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
            this.lblName = new System.Windows.Forms.Label();
            this.numBestCase = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.numWorstCase = new System.Windows.Forms.NumericUpDown();
            this.btn_OK = new System.Windows.Forms.Button();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numBestCase)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWorstCase)).BeginInit();
            this.SuspendLayout();
            // 
            // lblName
            // 
            this.lblName.AutoSize = true;
            this.lblName.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lblName.Location = new System.Drawing.Point(16, 27);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(86, 17);
            this.lblName.TabIndex = 45;
            this.lblName.Text = "Best Case [€]:";
            // 
            // numBestCase
            // 
            this.numBestCase.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.numBestCase.Location = new System.Drawing.Point(106, 25);
            this.numBestCase.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.numBestCase.Name = "numBestCase";
            this.numBestCase.Size = new System.Drawing.Size(97, 25);
            this.numBestCase.TabIndex = 46;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.label1.Location = new System.Drawing.Point(7, 61);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(96, 17);
            this.label1.TabIndex = 47;
            this.label1.Text = "Worst Case [€]:";
            // 
            // numWorstCase
            // 
            this.numWorstCase.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.numWorstCase.Location = new System.Drawing.Point(106, 58);
            this.numWorstCase.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.numWorstCase.Name = "numWorstCase";
            this.numWorstCase.Size = new System.Drawing.Size(97, 25);
            this.numWorstCase.TabIndex = 48;
            // 
            // btn_OK
            // 
            this.btn_OK.Location = new System.Drawing.Point(128, 98);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(75, 23);
            this.btn_OK.TabIndex = 49;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // btn_Abbrechen
            // 
            this.btn_Abbrechen.Location = new System.Drawing.Point(15, 98);
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.Size = new System.Drawing.Size(75, 23);
            this.btn_Abbrechen.TabIndex = 50;
            this.btn_Abbrechen.Text = "Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // Form_CaseEingabe
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(221, 129);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.numWorstCase);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.numBestCase);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_CaseEingabe";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Eingabe";
            ((System.ComponentModel.ISupportInitialize)(this.numBestCase)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWorstCase)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.NumericUpDown numBestCase;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numWorstCase;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Button btn_Abbrechen;
    }
}