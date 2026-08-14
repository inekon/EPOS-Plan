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
            cmbBrennstoffArt = new System.Windows.Forms.ComboBox();
            TextBox_Variante = new System.Windows.Forms.TextBox();
            btn_Abbrechen = new System.Windows.Forms.Button();
            btn_OK = new System.Windows.Forms.Button();
            label1 = new System.Windows.Forms.Label();
            label_Variante = new System.Windows.Forms.Label();
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
            // TextBox_Variante
            // 
            TextBox_Variante.Location = new System.Drawing.Point(159, 72);
            TextBox_Variante.Margin = new System.Windows.Forms.Padding(4);
            TextBox_Variante.Name = "TextBox_Variante";
            TextBox_Variante.Size = new System.Drawing.Size(172, 25);
            TextBox_Variante.TabIndex = 1;
            // 
            // btn_Abbrechen
            // 
            btn_Abbrechen.Location = new System.Drawing.Point(32, 139);
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
            btn_OK.Location = new System.Drawing.Point(243, 139);
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
            // label_Variante
            // 
            label_Variante.AutoSize = true;
            label_Variante.Location = new System.Drawing.Point(13, 72);
            label_Variante.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label_Variante.Name = "label_Variante";
            label_Variante.Size = new System.Drawing.Size(141, 17);
            label_Variante.TabIndex = 5;
            label_Variante.Text = "Varianten Bezeichnung:";
            // 
            // Form_Kosten_Auswahl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(356, 185);
            Controls.Add(label_Variante);
            Controls.Add(label1);
            Controls.Add(btn_OK);
            Controls.Add(btn_Abbrechen);
            Controls.Add(TextBox_Variante);
            Controls.Add(cmbBrennstoffArt);
            Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Margin = new System.Windows.Forms.Padding(4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form_Kosten_Auswahl";
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Energieerzeuger Variante";
            Load += Form_Kosten_Auswahl_Load;
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cmbBrennstoffArt;
        private System.Windows.Forms.TextBox TextBox_Variante;
        private System.Windows.Forms.Button btn_Abbrechen;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label_Variante;
    }
}