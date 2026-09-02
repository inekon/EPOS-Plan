namespace WindowsFormsApplication1
{
    partial class Form_Solarganglinie
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_Solarganglinie));
            this.Label1 = new System.Windows.Forms.Label();
            this.listBox_Auswahl = new System.Windows.Forms.ListBox();
            this.btn_Hinzufuegen = new System.Windows.Forms.Button();
            this.btn_Entfernen = new System.Windows.Forms.Button();
            this.listBox_Extern = new System.Windows.Forms.ListBox();
            this.Label2 = new System.Windows.Forms.Label();
            this.btn_OK = new System.Windows.Forms.Button();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.btn_Bearbeiten = new System.Windows.Forms.Button();
            this.textBox_Name = new System.Windows.Forms.TextBox();
            this.Label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
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
            this.listBox_Auswahl.SelectedIndexChanged += new System.EventHandler(this.listBox_Auswahl_SelectedIndexChanged);
            // 
            // btn_Hinzufuegen
            // 
            resources.ApplyResources(this.btn_Hinzufuegen, "btn_Hinzufuegen");
            this.btn_Hinzufuegen.Name = "btn_Hinzufuegen";
            this.btn_Hinzufuegen.UseVisualStyleBackColor = true;
            this.btn_Hinzufuegen.Click += new System.EventHandler(this.btn_Hinzufuegen_Click);
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
            this.listBox_Extern.SelectedIndexChanged += new System.EventHandler(this.listBox_Extern_SelectedIndexChanged);
            // 
            // Label2
            // 
            resources.ApplyResources(this.Label2, "Label2");
            this.Label2.Name = "Label2";
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
            // btn_Bearbeiten
            // 
            resources.ApplyResources(this.btn_Bearbeiten, "btn_Bearbeiten");
            this.btn_Bearbeiten.Name = "btn_Bearbeiten";
            this.btn_Bearbeiten.UseVisualStyleBackColor = true;
            this.btn_Bearbeiten.Click += new System.EventHandler(this.btn_Bearbeiten_Click);
            // 
            // textBox_Name
            // 
            this.textBox_Name.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.textBox_Name, "textBox_Name");
            this.textBox_Name.Name = "textBox_Name";
            this.textBox_Name.ReadOnly = true;
            // 
            // Label3
            // 
            resources.ApplyResources(this.Label3, "Label3");
            this.Label3.Name = "Label3";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.textBox1, "textBox1");
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            // 
            // Form_Solarganglinie
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.btn_Bearbeiten);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.listBox_Auswahl);
            this.Controls.Add(this.btn_Hinzufuegen);
            this.Controls.Add(this.btn_Entfernen);
            this.Controls.Add(this.listBox_Extern);
            this.Controls.Add(this.Label2);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.Label3);
            this.Controls.Add(this.textBox_Name);
            this.Name = "Form_Solarganglinie";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

private System.Windows.Forms.Label Label1;
private System.Windows.Forms.ListBox listBox_Auswahl;
private System.Windows.Forms.Button btn_Hinzufuegen;
private System.Windows.Forms.Button btn_Entfernen;
private System.Windows.Forms.ListBox listBox_Extern;
private System.Windows.Forms.Label Label2;
private System.Windows.Forms.Button btn_OK;
private System.Windows.Forms.Button btn_Abbrechen;
        private System.Windows.Forms.Button btn_Bearbeiten;
        private System.Windows.Forms.TextBox textBox_Name;
        private System.Windows.Forms.Label Label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox1;
    }
}