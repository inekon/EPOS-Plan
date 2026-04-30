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
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.numWorstCase_Nutzungsdauer = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.numBestCase_Nutzungsdauer = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.numBestCase)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWorstCase)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWorstCase_Nutzungsdauer)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numBestCase_Nutzungsdauer)).BeginInit();
            this.SuspendLayout();
            // 
            // lblName
            // 
            this.lblName.AutoSize = true;
            this.lblName.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lblName.Location = new System.Drawing.Point(16, 35);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(86, 17);
            this.lblName.TabIndex = 45;
            this.lblName.Text = "Best Case [€]:";
            // 
            // numBestCase
            // 
            this.numBestCase.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.numBestCase.Location = new System.Drawing.Point(106, 33);
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
            this.label1.Location = new System.Drawing.Point(209, 35);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(96, 17);
            this.label1.TabIndex = 47;
            this.label1.Text = "Worst Case [€]:";
            // 
            // numWorstCase
            // 
            this.numWorstCase.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.numWorstCase.Location = new System.Drawing.Point(308, 32);
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
            this.btn_OK.Location = new System.Drawing.Point(329, 168);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(75, 23);
            this.btn_OK.TabIndex = 49;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // btn_Abbrechen
            // 
            this.btn_Abbrechen.Location = new System.Drawing.Point(15, 168);
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.Size = new System.Drawing.Size(75, 23);
            this.btn_Abbrechen.TabIndex = 50;
            this.btn_Abbrechen.Text = "Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(16, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(54, 17);
            this.label2.TabIndex = 51;
            this.label2.Text = "Kosten:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(15, 77);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(107, 17);
            this.label3.TabIndex = 56;
            this.label3.Text = "Nutzungsdauer:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.label4.Location = new System.Drawing.Point(208, 103);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(96, 17);
            this.label4.TabIndex = 54;
            this.label4.Text = "Worst Case [€]:";
            // 
            // numWorstCase_Nutzungsdauer
            // 
            this.numWorstCase_Nutzungsdauer.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.numWorstCase_Nutzungsdauer.Location = new System.Drawing.Point(307, 100);
            this.numWorstCase_Nutzungsdauer.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.numWorstCase_Nutzungsdauer.Name = "numWorstCase_Nutzungsdauer";
            this.numWorstCase_Nutzungsdauer.Size = new System.Drawing.Size(97, 25);
            this.numWorstCase_Nutzungsdauer.TabIndex = 55;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.label5.Location = new System.Drawing.Point(15, 103);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(86, 17);
            this.label5.TabIndex = 52;
            this.label5.Text = "Best Case [€]:";
            // 
            // numBestCase_Nutzungsdauer
            // 
            this.numBestCase_Nutzungsdauer.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.numBestCase_Nutzungsdauer.Location = new System.Drawing.Point(105, 101);
            this.numBestCase_Nutzungsdauer.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.numBestCase_Nutzungsdauer.Name = "numBestCase_Nutzungsdauer";
            this.numBestCase_Nutzungsdauer.Size = new System.Drawing.Size(97, 25);
            this.numBestCase_Nutzungsdauer.TabIndex = 53;
            // 
            // Form_CaseEingabe
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(419, 203);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.numWorstCase_Nutzungsdauer);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.numBestCase_Nutzungsdauer);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.numWorstCase);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.numBestCase);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_CaseEingabe";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Eingabe";
            ((System.ComponentModel.ISupportInitialize)(this.numBestCase)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWorstCase)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWorstCase_Nutzungsdauer)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numBestCase_Nutzungsdauer)).EndInit();
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
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown numWorstCase_Nutzungsdauer;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown numBestCase_Nutzungsdauer;
    }
}