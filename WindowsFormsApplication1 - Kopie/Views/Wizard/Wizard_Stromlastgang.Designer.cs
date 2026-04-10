namespace WindowsFormsApplication1
{
    partial class Wizard_Stromlastgang 
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Wizard_Stromlastgang));
            this.label_Type = new System.Windows.Forms.Label();
            this.Label1 = new System.Windows.Forms.Label();
            this.listBox_Auswahl = new System.Windows.Forms.ListBox();
            this.btn_Hinzufuegen = new System.Windows.Forms.Button();
            this.btn_Entfernen = new System.Windows.Forms.Button();
            this.listBox_Extern = new System.Windows.Forms.ListBox();
            this.Label2 = new System.Windows.Forms.Label();
            this.btn_Bearbeiten = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label_Type
            // 
            resources.ApplyResources(this.label_Type, "label_Type");
            this.label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.label_Type.Name = "label_Type";
            // 
            // Label1
            // 
            resources.ApplyResources(this.Label1, "Label1");
            this.Label1.Name = "Label1";
            // 
            // listBox_Auswahl
            // 
            resources.ApplyResources(this.listBox_Auswahl, "listBox_Auswahl");
            this.listBox_Auswahl.Name = "listBox_Auswahl";
            // 
            // btn_Hinzufuegen
            // 
            resources.ApplyResources(this.btn_Hinzufuegen, "btn_Hinzufuegen");
            this.btn_Hinzufuegen.Name = "btn_Hinzufuegen";
            this.btn_Hinzufuegen.UseVisualStyleBackColor = true;
            this.btn_Hinzufuegen.Click += new System.EventHandler(this.btn_Hinzu_Click);
            // 
            // btn_Entfernen
            // 
            resources.ApplyResources(this.btn_Entfernen, "btn_Entfernen");
            this.btn_Entfernen.Name = "btn_Entfernen";
            this.btn_Entfernen.UseVisualStyleBackColor = true;
            this.btn_Entfernen.Click += new System.EventHandler(this.btn_Entfernen_Click);
            // 
            // listBox_Extern
            // 
            resources.ApplyResources(this.listBox_Extern, "listBox_Extern");
            this.listBox_Extern.Name = "listBox_Extern";
            // 
            // Label2
            // 
            resources.ApplyResources(this.Label2, "Label2");
            this.Label2.Name = "Label2";
            // 
            // btn_Bearbeiten
            // 
            resources.ApplyResources(this.btn_Bearbeiten, "btn_Bearbeiten");
            this.btn_Bearbeiten.Name = "btn_Bearbeiten";
            this.btn_Bearbeiten.UseVisualStyleBackColor = true;
            this.btn_Bearbeiten.Click += new System.EventHandler(this.btn_Bearbeiten_Click);
            // 
            // Wizard_Stromlastgang
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.btn_Bearbeiten);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.listBox_Auswahl);
            this.Controls.Add(this.btn_Hinzufuegen);
            this.Controls.Add(this.btn_Entfernen);
            this.Controls.Add(this.listBox_Extern);
            this.Controls.Add(this.Label2);
            this.Controls.Add(this.label_Type);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Wizard_Stromlastgang";
            this.Load += new System.EventHandler(this.Wizard_Stromlastgang_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label_Type;
        private System.Windows.Forms.Label Label1;
        private System.Windows.Forms.ListBox listBox_Auswahl;
        private System.Windows.Forms.Button btn_Hinzufuegen;
        private System.Windows.Forms.Button btn_Entfernen;
        private System.Windows.Forms.ListBox listBox_Extern;
        private System.Windows.Forms.Label Label2;
        private System.Windows.Forms.Button btn_Bearbeiten;
    }
}