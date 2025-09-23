namespace WindowsFormsApplication1
{
    partial class Form_ProjektOpen
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.comboBox_Projekte = new System.Windows.Forms.ComboBox();
            this.button_Open = new System.Windows.Forms.Button();
            this.button_Abbrechen = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.comboBox_Klimaregion = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // comboBox_Projekte
            // 
            this.comboBox_Projekte.FormattingEnabled = true;
            this.comboBox_Projekte.Location = new System.Drawing.Point(107, 24);
            this.comboBox_Projekte.Name = "comboBox_Projekte";
            this.comboBox_Projekte.Size = new System.Drawing.Size(226, 21);
            this.comboBox_Projekte.TabIndex = 1;
            // 
            // button_Open
            // 
            this.button_Open.Location = new System.Drawing.Point(25, 182);
            this.button_Open.Name = "button_Open";
            this.button_Open.Size = new System.Drawing.Size(75, 23);
            this.button_Open.TabIndex = 2;
            this.button_Open.Text = "OK";
            this.button_Open.UseVisualStyleBackColor = true;
            this.button_Open.Click += new System.EventHandler(this.button_Open_Click);
            // 
            // button_Abbrechen
            // 
            this.button_Abbrechen.Location = new System.Drawing.Point(258, 182);
            this.button_Abbrechen.Name = "button_Abbrechen";
            this.button_Abbrechen.Size = new System.Drawing.Size(75, 23);
            this.button_Abbrechen.TabIndex = 3;
            this.button_Abbrechen.Text = "Abbrechen";
            this.button_Abbrechen.UseVisualStyleBackColor = true;
            this.button_Abbrechen.Click += new System.EventHandler(this.button_Abbrechen_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(22, 27);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(40, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Projekt";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(22, 74);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(61, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Klimaregion";
            // 
            // comboBox_Klimaregion
            // 
            this.comboBox_Klimaregion.FormattingEnabled = true;
            this.comboBox_Klimaregion.Location = new System.Drawing.Point(107, 71);
            this.comboBox_Klimaregion.Name = "comboBox_Klimaregion";
            this.comboBox_Klimaregion.Size = new System.Drawing.Size(226, 21);
            this.comboBox_Klimaregion.TabIndex = 10;
            this.comboBox_Klimaregion.SelectedIndexChanged += new System.EventHandler(this.comboBox_Klimaregion_SelectedIndexChanged);
            // 
            // Form_ProjektOpen
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(367, 218);
            this.Controls.Add(this.comboBox_Klimaregion);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button_Abbrechen);
            this.Controls.Add(this.button_Open);
            this.Controls.Add(this.comboBox_Projekte);
            this.Name = "Form_ProjektOpen";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Projekt Öffnen";
            this.Load += new System.EventHandler(this.Form_ProjektOpen_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBox_Projekte;
        private System.Windows.Forms.Button button_Open;
        private System.Windows.Forms.Button button_Abbrechen;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox comboBox_Klimaregion;
    }
}