namespace WindowsFormsApplication1
{
    partial class Form_ProjektDelete
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_ProjektDelete));
            this.comboBox_Projekte = new System.Windows.Forms.ComboBox();
            this.btn_OK = new System.Windows.Forms.Button();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // comboBox_Projekte
            // 
            resources.ApplyResources(this.comboBox_Projekte, "comboBox_Projekte");
            this.comboBox_Projekte.FormattingEnabled = true;
            this.comboBox_Projekte.Name = "comboBox_Projekte";
            this.comboBox_Projekte.SelectedIndexChanged += new System.EventHandler(this.comboBox_Projekte_SelectedIndexChanged);
            // 
            // btn_OK
            // 
            resources.ApplyResources(this.btn_OK, "btn_OK");
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // btn_Abbrechen
            // 
            resources.ApplyResources(this.btn_Abbrechen, "btn_Abbrechen");
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // Form_ProjektDelete
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.comboBox_Projekte);
            this.Name = "Form_ProjektDelete";
            this.Load += new System.EventHandler(this.Form_ProjektDelete_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBox_Projekte;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Button btn_Abbrechen;
        private System.Windows.Forms.Label label1;
    }
}