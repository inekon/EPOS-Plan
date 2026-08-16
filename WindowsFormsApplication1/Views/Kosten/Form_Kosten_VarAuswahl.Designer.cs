namespace WindowsFormsApplication1
{
    partial class Form_Kosten_VarAuswahl
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
            cmbBrennstoffArt = new System.Windows.Forms.ComboBox();
            btn_Abbrechen = new System.Windows.Forms.Button();
            btn_OK = new System.Windows.Forms.Button();
            label1 = new System.Windows.Forms.Label();
            comboBox_Varianten = new System.Windows.Forms.ComboBox();
            label2 = new System.Windows.Forms.Label();
            SuspendLayout();
            // 
            // cmbBrennstoffArt
            // 
            cmbBrennstoffArt.FormattingEnabled = true;
            cmbBrennstoffArt.Location = new System.Drawing.Point(159, 26);
            cmbBrennstoffArt.Margin = new System.Windows.Forms.Padding(4);
            cmbBrennstoffArt.Name = "cmbBrennstoffArt";
            cmbBrennstoffArt.Size = new System.Drawing.Size(172, 25);
            cmbBrennstoffArt.TabIndex = 0;
            cmbBrennstoffArt.SelectedIndexChanged += cmbBrennstoffArt_SelectedIndexChanged;
            // 
            // btn_Abbrechen
            // 
            btn_Abbrechen.Location = new System.Drawing.Point(32, 146);
            btn_Abbrechen.Margin = new System.Windows.Forms.Padding(4);
            btn_Abbrechen.Name = "btn_Abbrechen";
            btn_Abbrechen.Size = new System.Drawing.Size(88, 30);
            btn_Abbrechen.TabIndex = 2;
            btn_Abbrechen.Text = "Abbrechen";
            btn_Abbrechen.UseVisualStyleBackColor = true;
            btn_Abbrechen.Click += btn_Abbrechen_Click;
            // 
            // btn_OK
            // 
            btn_OK.Location = new System.Drawing.Point(243, 146);
            btn_OK.Margin = new System.Windows.Forms.Padding(4);
            btn_OK.Name = "btn_OK";
            btn_OK.Size = new System.Drawing.Size(88, 30);
            btn_OK.TabIndex = 3;
            btn_OK.Text = "OK";
            btn_OK.UseVisualStyleBackColor = true;
            btn_OK.Click += btnOk_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(13, 29);
            label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(107, 17);
            label1.TabIndex = 4;
            label1.Text = "Energieerzeuger:";
            // 
            // comboBox_Varianten
            // 
            comboBox_Varianten.FormattingEnabled = true;
            comboBox_Varianten.Location = new System.Drawing.Point(159, 74);
            comboBox_Varianten.Margin = new System.Windows.Forms.Padding(4);
            comboBox_Varianten.Name = "comboBox_Varianten";
            comboBox_Varianten.Size = new System.Drawing.Size(172, 25);
            comboBox_Varianten.TabIndex = 6;
            // 
            // label2
            // 
            label2.Location = new System.Drawing.Point(13, 74);
            label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(138, 57);
            label2.TabIndex = 7;
            label2.Text = "Varianten Auswahl:";
            // 
            // Form_Kosten_VarAuswahl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(356, 193);
            Controls.Add(label2);
            Controls.Add(comboBox_Varianten);
            Controls.Add(label1);
            Controls.Add(btn_OK);
            Controls.Add(btn_Abbrechen);
            Controls.Add(cmbBrennstoffArt);
            Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Margin = new System.Windows.Forms.Padding(4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form_Kosten_VarAuswahl";
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Energieerzeuger Variante";
            Load += Form_Kosten_VarAuswahl_Load;
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cmbBrennstoffArt;
        private System.Windows.Forms.Button btn_Abbrechen;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBox_Varianten;
        private System.Windows.Forms.Label label2;
    }
}