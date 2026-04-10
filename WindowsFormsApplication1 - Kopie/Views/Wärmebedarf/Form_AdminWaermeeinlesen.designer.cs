namespace WindowsFormsApplication1
{
    partial class Form_AdminWaermeeinlesen
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_AdminWaermeeinlesen));
            this.listBox_Extern = new System.Windows.Forms.ListBox();
            this.Label2 = new System.Windows.Forms.Label();
            this.btn_OK = new System.Windows.Forms.Button();
            this.textBox_Name = new System.Windows.Forms.TextBox();
            this.btn_Oeffnen = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.textBox_Ordner = new System.Windows.Forms.TextBox();
            this.btn_Datei = new System.Windows.Forms.Button();
            this.btn_Loeschen = new System.Windows.Forms.Button();
            this.btn_Einlesen = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
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
            // btn_OK
            // 
            resources.ApplyResources(this.btn_OK, "btn_OK");
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // textBox_Name
            // 
            resources.ApplyResources(this.textBox_Name, "textBox_Name");
            this.textBox_Name.BackColor = System.Drawing.Color.White;
            this.textBox_Name.Name = "textBox_Name";
            this.textBox_Name.ReadOnly = true;
            // 
            // btn_Oeffnen
            // 
            resources.ApplyResources(this.btn_Oeffnen, "btn_Oeffnen");
            this.btn_Oeffnen.Name = "btn_Oeffnen";
            this.btn_Oeffnen.UseVisualStyleBackColor = true;
            this.btn_Oeffnen.Click += new System.EventHandler(this.btn_Oeffnen_Click);
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // textBox_Ordner
            // 
            resources.ApplyResources(this.textBox_Ordner, "textBox_Ordner");
            this.textBox_Ordner.Name = "textBox_Ordner";
            // 
            // btn_Datei
            // 
            resources.ApplyResources(this.btn_Datei, "btn_Datei");
            this.btn_Datei.Name = "btn_Datei";
            this.btn_Datei.UseVisualStyleBackColor = true;
            this.btn_Datei.Click += new System.EventHandler(this.btn_Datei_Click);
            // 
            // btn_Loeschen
            // 
            resources.ApplyResources(this.btn_Loeschen, "btn_Loeschen");
            this.btn_Loeschen.Name = "btn_Loeschen";
            this.btn_Loeschen.UseVisualStyleBackColor = true;
            this.btn_Loeschen.Click += new System.EventHandler(this.btn_Loeschen_Click);
            // 
            // btn_Einlesen
            // 
            resources.ApplyResources(this.btn_Einlesen, "btn_Einlesen");
            this.btn_Einlesen.Name = "btn_Einlesen";
            this.btn_Einlesen.UseVisualStyleBackColor = true;
            this.btn_Einlesen.Click += new System.EventHandler(this.btn_Einlesen_Click);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // Form_AdminWaermeeinlesen
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btn_Einlesen);
            this.Controls.Add(this.btn_Loeschen);
            this.Controls.Add(this.btn_Datei);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.textBox_Ordner);
            this.Controls.Add(this.btn_Oeffnen);
            this.Controls.Add(this.listBox_Extern);
            this.Controls.Add(this.Label2);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.textBox_Name);
            this.Name = "Form_AdminWaermeeinlesen";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBox_Extern;
private System.Windows.Forms.Label Label2;
private System.Windows.Forms.Button btn_OK;
private System.Windows.Forms.TextBox textBox_Name;
private System.Windows.Forms.Button btn_Oeffnen;
private System.Windows.Forms.Label label6;
private System.Windows.Forms.TextBox textBox_Ordner;
private System.Windows.Forms.Button btn_Datei;
private System.Windows.Forms.Button btn_Loeschen;
private System.Windows.Forms.Button btn_Einlesen;
private System.Windows.Forms.Label label1;
    }
}