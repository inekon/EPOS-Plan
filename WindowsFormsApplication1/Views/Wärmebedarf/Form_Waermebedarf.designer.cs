namespace WindowsFormsApplication1
{
    partial class Form_Waermebedarf 
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_Waermebedarf));
            this.Label1 = new System.Windows.Forms.Label();
            this.listBox_Auswahl = new System.Windows.Forms.ListBox();
            this.btn_Hinzufuegen = new System.Windows.Forms.Button();
            this.btn_Entfernen = new System.Windows.Forms.Button();
            this.listBox_Extern = new System.Windows.Forms.ListBox();
            this.Label2 = new System.Windows.Forms.Label();
            this.btn_Bearbeiten = new System.Windows.Forms.Button();
            this.label_Type = new System.Windows.Forms.Label();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.btn_Loeschen = new System.Windows.Forms.Button();
            this.btn_Help = new System.Windows.Forms.Button();
            this.SuspendLayout();
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
            // label_Type
            // 
            resources.ApplyResources(this.label_Type, "label_Type");
            this.label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.label_Type.Name = "label_Type";
            // 
            // btn_Abbrechen
            // 
            resources.ApplyResources(this.btn_Abbrechen, "btn_Abbrechen");
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // btn_OK
            // 
            resources.ApplyResources(this.btn_OK, "btn_OK");
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // btn_Loeschen
            // 
            resources.ApplyResources(this.btn_Loeschen, "btn_Loeschen");
            this.btn_Loeschen.Name = "btn_Loeschen";
            this.btn_Loeschen.UseVisualStyleBackColor = true;
            this.btn_Loeschen.Click += new System.EventHandler(this.btn_Loeschen_Click);
            // 
            // btn_Help
            // 
            this.btn_Help.BackColor = System.Drawing.Color.Transparent;
            this.btn_Help.BackgroundImage = Properties.Resources.help_icon;
            this.btn_Help.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btn_Help.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_Help.FlatAppearance.BorderSize = 0;
            this.btn_Help.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_Help.Location = new System.Drawing.Point(783, 43);
            this.btn_Help.Name = "btn_Help";
            this.btn_Help.Size = new System.Drawing.Size(28, 28);
            this.btn_Help.TabStop = false;
            this.btn_Help.UseVisualStyleBackColor = false;
            // 
            // Form_Waermebedarf
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btn_Help);
            this.Controls.Add(this.btn_Loeschen);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.label_Type);
            this.Controls.Add(this.btn_Bearbeiten);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.listBox_Auswahl);
            this.Controls.Add(this.btn_Hinzufuegen);
            this.Controls.Add(this.btn_Entfernen);
            this.Controls.Add(this.listBox_Extern);
            this.Controls.Add(this.Label2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "Form_Waermebedarf";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

private System.Windows.Forms.Button btn_Help;
private System.Windows.Forms.Label Label1;
private System.Windows.Forms.ListBox listBox_Auswahl;
private System.Windows.Forms.Button btn_Hinzufuegen;
private System.Windows.Forms.Button btn_Entfernen;
private System.Windows.Forms.ListBox listBox_Extern;
private System.Windows.Forms.Label Label2;
        private System.Windows.Forms.Button btn_Bearbeiten;
        private System.Windows.Forms.Label label_Type;
        private System.Windows.Forms.Button btn_Abbrechen;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Button btn_Loeschen;
    }
}