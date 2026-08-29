namespace WindowsFormsApplication1
{
    partial class Form_ProjektAuswahl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_ProjektAuswahl));
            ucAuswahl = new ProjektAuswahl();
            btn_OK = new System.Windows.Forms.Button();
            btn_Abbrechen = new System.Windows.Forms.Button();
            SuspendLayout();
            //
            // ucAuswahl
            //
            resources.ApplyResources(ucAuswahl, "ucAuswahl");
            ucAuswahl.Name = "ucAuswahl";
            ucAuswahl.ProjektGewaehlt += ucAuswahl_ProjektGewaehlt;
            //
            // btn_OK
            //
            resources.ApplyResources(btn_OK, "btn_OK");
            btn_OK.Name = "btn_OK";
            btn_OK.UseVisualStyleBackColor = true;
            btn_OK.Click += btn_OK_Click;
            //
            // btn_Abbrechen
            //
            resources.ApplyResources(btn_Abbrechen, "btn_Abbrechen");
            btn_Abbrechen.Name = "btn_Abbrechen";
            btn_Abbrechen.UseVisualStyleBackColor = true;
            btn_Abbrechen.Click += btn_Abbrechen_Click;
            //
            // Form_ProjektAuswahl
            //
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(btn_Abbrechen);
            Controls.Add(btn_OK);
            Controls.Add(ucAuswahl);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form_ProjektAuswahl";
            ShowIcon = false;
            ShowInTaskbar = false;
            Load += Form_ProjektAuswahl_Load;
            ResumeLayout(false);
        }

        #endregion

        private ProjektAuswahl ucAuswahl;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Button btn_Abbrechen;
    }
}
