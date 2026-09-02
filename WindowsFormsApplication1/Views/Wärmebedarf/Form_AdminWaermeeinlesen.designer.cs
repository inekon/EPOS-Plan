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
            listBox_Extern = new System.Windows.Forms.ListBox();
            Label2 = new System.Windows.Forms.Label();
            btn_OK = new System.Windows.Forms.Button();
            textBox_Name = new System.Windows.Forms.TextBox();
            btn_Oeffnen = new System.Windows.Forms.Button();
            btn_Datei = new System.Windows.Forms.Button();
            btn_Loeschen = new System.Windows.Forms.Button();
            btn_Einlesen = new System.Windows.Forms.Button();
            label1 = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            textBox_Ordner = new System.Windows.Forms.TextBox();
            SuspendLayout();
            // 
            // listBox_Extern
            // 
            resources.ApplyResources(listBox_Extern, "listBox_Extern");
            listBox_Extern.BorderStyle = System.Windows.Forms.BorderStyle.None;
            listBox_Extern.Name = "listBox_Extern";
            listBox_Extern.SelectedIndexChanged += listBox_Extern_SelectedIndexChanged;
            // 
            // Label2
            // 
            resources.ApplyResources(Label2, "Label2");
            Label2.Name = "Label2";
            // 
            // btn_OK
            // 
            resources.ApplyResources(btn_OK, "btn_OK");
            btn_OK.Name = "btn_OK";
            btn_OK.UseVisualStyleBackColor = true;
            btn_OK.Click += btn_OK_Click;
            // 
            // textBox_Name
            // 
            resources.ApplyResources(textBox_Name, "textBox_Name");
            textBox_Name.BackColor = System.Drawing.Color.White;
            textBox_Name.Name = "textBox_Name";
            textBox_Name.ReadOnly = true;
            // 
            // btn_Oeffnen
            // 
            resources.ApplyResources(btn_Oeffnen, "btn_Oeffnen");
            btn_Oeffnen.Name = "btn_Oeffnen";
            btn_Oeffnen.UseVisualStyleBackColor = true;
            btn_Oeffnen.Click += btn_Oeffnen_Click;
            // 
            // btn_Datei
            // 
            resources.ApplyResources(btn_Datei, "btn_Datei");
            btn_Datei.Name = "btn_Datei";
            btn_Datei.UseVisualStyleBackColor = true;
            btn_Datei.Click += btn_Datei_Click;
            // 
            // btn_Loeschen
            // 
            resources.ApplyResources(btn_Loeschen, "btn_Loeschen");
            btn_Loeschen.Name = "btn_Loeschen";
            btn_Loeschen.UseVisualStyleBackColor = true;
            btn_Loeschen.Click += btn_Loeschen_Click;
            // 
            // btn_Einlesen
            // 
            resources.ApplyResources(btn_Einlesen, "btn_Einlesen");
            btn_Einlesen.Name = "btn_Einlesen";
            btn_Einlesen.UseVisualStyleBackColor = true;
            btn_Einlesen.Click += btn_Einlesen_Click;
            // 
            // label1
            // 
            resources.ApplyResources(label1, "label1");
            label1.Name = "label1";
            // 
            // label6
            // 
            resources.ApplyResources(label6, "label6");
            label6.Name = "label6";
            // 
            // textBox_Ordner
            // 
            resources.ApplyResources(textBox_Ordner, "textBox_Ordner");
            textBox_Ordner.Name = "textBox_Ordner";
            // 
            // Form_AdminWaermeeinlesen
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(label6);
            Controls.Add(textBox_Ordner);
            Controls.Add(label1);
            Controls.Add(btn_Einlesen);
            Controls.Add(btn_Loeschen);
            Controls.Add(btn_Datei);
            Controls.Add(btn_Oeffnen);
            Controls.Add(listBox_Extern);
            Controls.Add(Label2);
            Controls.Add(btn_OK);
            Controls.Add(textBox_Name);
            Name = "Form_AdminWaermeeinlesen";
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBox_Extern;
private System.Windows.Forms.Label Label2;
private System.Windows.Forms.Button btn_OK;
private System.Windows.Forms.TextBox textBox_Name;
private System.Windows.Forms.Button btn_Oeffnen;
private System.Windows.Forms.Button btn_Datei;
private System.Windows.Forms.Button btn_Loeschen;
private System.Windows.Forms.Button btn_Einlesen;
private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBox_Ordner;
    }
}