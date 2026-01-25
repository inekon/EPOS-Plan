namespace WindowsFormsApplication1
{
    partial class Form_GebWohnflaeche
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_GebWohnflaeche));
            this.btn_Hilfe = new System.Windows.Forms.Button();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.txt_Gebaeudeart = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.txt_Baujahr = new System.Windows.Forms.TextBox();
            this.Label3 = new System.Windows.Forms.Label();
            this.Label4 = new System.Windows.Forms.Label();
            this.Label1 = new System.Windows.Forms.Label();
            this.txt_Gebaeudename = new System.Windows.Forms.TextBox();
            this.txt_Beschreibung = new System.Windows.Forms.TextBox();
            this.Label2 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.Label5 = new System.Windows.Forms.Label();
            this.txt_Verbrauch = new System.Windows.Forms.TextBox();
            this.txt_Einheit = new System.Windows.Forms.TextBox();
            this.Label6 = new System.Windows.Forms.Label();
            this.D_BW = new System.Windows.Forms.CheckBox();
            this.J_Text = new System.Windows.Forms.Label();
            this.Jahresnutzungsgrad = new System.Windows.Forms.TextBox();
            this.txt_Bedarfsart_Auswahl = new System.Windows.Forms.TextBox();
            this.cmb_Bedarfsart = new System.Windows.Forms.ListBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // btn_Hilfe
            // 
            resources.ApplyResources(this.btn_Hilfe, "btn_Hilfe");
            this.btn_Hilfe.Name = "btn_Hilfe";
            this.btn_Hilfe.UseVisualStyleBackColor = true;
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
            // txt_Gebaeudeart
            // 
            resources.ApplyResources(this.txt_Gebaeudeart, "txt_Gebaeudeart");
            this.txt_Gebaeudeart.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txt_Gebaeudeart.Name = "txt_Gebaeudeart";
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.txt_Baujahr);
            this.groupBox1.Controls.Add(this.Label3);
            this.groupBox1.Controls.Add(this.Label4);
            this.groupBox1.Controls.Add(this.Label1);
            this.groupBox1.Controls.Add(this.txt_Gebaeudename);
            this.groupBox1.Controls.Add(this.txt_Beschreibung);
            this.groupBox1.Controls.Add(this.Label2);
            this.groupBox1.Controls.Add(this.txt_Gebaeudeart);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // txt_Baujahr
            // 
            resources.ApplyResources(this.txt_Baujahr, "txt_Baujahr");
            this.txt_Baujahr.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txt_Baujahr.Name = "txt_Baujahr";
            // 
            // Label3
            // 
            resources.ApplyResources(this.Label3, "Label3");
            this.Label3.Name = "Label3";
            // 
            // Label4
            // 
            resources.ApplyResources(this.Label4, "Label4");
            this.Label4.Name = "Label4";
            // 
            // Label1
            // 
            resources.ApplyResources(this.Label1, "Label1");
            this.Label1.Name = "Label1";
            // 
            // txt_Gebaeudename
            // 
            resources.ApplyResources(this.txt_Gebaeudename, "txt_Gebaeudename");
            this.txt_Gebaeudename.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txt_Gebaeudename.Name = "txt_Gebaeudename";
            // 
            // txt_Beschreibung
            // 
            resources.ApplyResources(this.txt_Beschreibung, "txt_Beschreibung");
            this.txt_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txt_Beschreibung.Name = "txt_Beschreibung";
            // 
            // Label2
            // 
            resources.ApplyResources(this.Label2, "Label2");
            this.Label2.Name = "Label2";
            // 
            // groupBox2
            // 
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Controls.Add(this.Label5);
            this.groupBox2.Controls.Add(this.txt_Verbrauch);
            this.groupBox2.Controls.Add(this.txt_Einheit);
            this.groupBox2.Controls.Add(this.Label6);
            this.groupBox2.Controls.Add(this.D_BW);
            this.groupBox2.Controls.Add(this.J_Text);
            this.groupBox2.Controls.Add(this.Jahresnutzungsgrad);
            this.groupBox2.Controls.Add(this.txt_Bedarfsart_Auswahl);
            this.groupBox2.Controls.Add(this.cmb_Bedarfsart);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // Label5
            // 
            resources.ApplyResources(this.Label5, "Label5");
            this.Label5.Name = "Label5";
            // 
            // txt_Verbrauch
            // 
            resources.ApplyResources(this.txt_Verbrauch, "txt_Verbrauch");
            this.txt_Verbrauch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txt_Verbrauch.Name = "txt_Verbrauch";
            // 
            // txt_Einheit
            // 
            resources.ApplyResources(this.txt_Einheit, "txt_Einheit");
            this.txt_Einheit.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txt_Einheit.Name = "txt_Einheit";
            // 
            // Label6
            // 
            resources.ApplyResources(this.Label6, "Label6");
            this.Label6.Name = "Label6";
            // 
            // D_BW
            // 
            resources.ApplyResources(this.D_BW, "D_BW");
            this.D_BW.Name = "D_BW";
            this.D_BW.UseVisualStyleBackColor = true;
            // 
            // J_Text
            // 
            resources.ApplyResources(this.J_Text, "J_Text");
            this.J_Text.Name = "J_Text";
            // 
            // Jahresnutzungsgrad
            // 
            resources.ApplyResources(this.Jahresnutzungsgrad, "Jahresnutzungsgrad");
            this.Jahresnutzungsgrad.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Jahresnutzungsgrad.Name = "Jahresnutzungsgrad";
            // 
            // txt_Bedarfsart_Auswahl
            // 
            resources.ApplyResources(this.txt_Bedarfsart_Auswahl, "txt_Bedarfsart_Auswahl");
            this.txt_Bedarfsart_Auswahl.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txt_Bedarfsart_Auswahl.Name = "txt_Bedarfsart_Auswahl";
            // 
            // cmb_Bedarfsart
            // 
            resources.ApplyResources(this.cmb_Bedarfsart, "cmb_Bedarfsart");
            this.cmb_Bedarfsart.FormattingEnabled = true;
            this.cmb_Bedarfsart.Name = "cmb_Bedarfsart";
            this.cmb_Bedarfsart.SelectedIndexChanged += new System.EventHandler(this.cmb_Bedarfsart_SelectedIndexChanged);
            // 
            // Form_GebWohnflaeche
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btn_Hilfe);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.btn_OK);
            this.Name = "Form_GebWohnflaeche";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

private System.Windows.Forms.Button btn_Hilfe;
private System.Windows.Forms.Button btn_Abbrechen;
private System.Windows.Forms.Button btn_OK;
private System.Windows.Forms.TextBox txt_Gebaeudeart;
private System.Windows.Forms.GroupBox groupBox1;
private System.Windows.Forms.TextBox txt_Baujahr;
private System.Windows.Forms.Label Label3;
private System.Windows.Forms.Label Label4;
private System.Windows.Forms.Label Label1;
private System.Windows.Forms.TextBox txt_Gebaeudename;
private System.Windows.Forms.TextBox txt_Beschreibung;
private System.Windows.Forms.Label Label2;
private System.Windows.Forms.GroupBox groupBox2;
private System.Windows.Forms.Label Label5;
private System.Windows.Forms.TextBox txt_Verbrauch;
private System.Windows.Forms.TextBox txt_Einheit;
private System.Windows.Forms.Label Label6;
private System.Windows.Forms.CheckBox D_BW;
private System.Windows.Forms.Label J_Text;
private System.Windows.Forms.TextBox Jahresnutzungsgrad;
private System.Windows.Forms.TextBox txt_Bedarfsart_Auswahl;
private System.Windows.Forms.ListBox cmb_Bedarfsart;


 
    }
}