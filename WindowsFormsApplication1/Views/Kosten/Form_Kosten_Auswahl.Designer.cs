namespace WindowsFormsApplication1
{
    partial class Form_Kosten_Auswahl
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
            this.cmbBrennstoffArt = new System.Windows.Forms.ComboBox();
            this.txtCode = new System.Windows.Forms.TextBox();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // cmbBrennstoffArt
            // 
            this.cmbBrennstoffArt.FormattingEnabled = true;
            this.cmbBrennstoffArt.Location = new System.Drawing.Point(159, 26);
            this.cmbBrennstoffArt.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cmbBrennstoffArt.Name = "cmbBrennstoffArt";
            this.cmbBrennstoffArt.Size = new System.Drawing.Size(172, 25);
            this.cmbBrennstoffArt.TabIndex = 0;
            this.cmbBrennstoffArt.SelectedIndexChanged += new System.EventHandler(this.cmbBrennstoffArt_SelectedIndexChanged);
            // 
            // txtCode
            // 
            this.txtCode.Location = new System.Drawing.Point(159, 72);
            this.txtCode.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtCode.Name = "txtCode";
            this.txtCode.Size = new System.Drawing.Size(172, 25);
            this.txtCode.TabIndex = 1;
            // 
            // btn_Abbrechen
            // 
            this.btn_Abbrechen.Location = new System.Drawing.Point(32, 139);
            this.btn_Abbrechen.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.Size = new System.Drawing.Size(88, 30);
            this.btn_Abbrechen.TabIndex = 2;
            this.btn_Abbrechen.Text = "Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // btn_OK
            // 
            this.btn_OK.Location = new System.Drawing.Point(243, 139);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(88, 30);
            this.btn_OK.TabIndex = 3;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 29);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(107, 17);
            this.label1.TabIndex = 4;
            this.label1.Text = "Energieerzeuger:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 72);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(141, 17);
            this.label2.TabIndex = 5;
            this.label2.Text = "Varianten Bezeichnung:";
            // 
            // Form_Kosten_Auswahl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(356, 185);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.txtCode);
            this.Controls.Add(this.cmbBrennstoffArt);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_Kosten_Auswahl";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Energieerzeuger Variante";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cmbBrennstoffArt;
        private System.Windows.Forms.TextBox txtCode;
        private System.Windows.Forms.Button btn_Abbrechen;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
    }
}