namespace WindowsFormsApplication1
{
    partial class Form_SolarDB 
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_SolarDB));
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.btn_Überschreiben = new System.Windows.Forms.Button();
            this.btn_Speichern_Unter = new System.Windows.Forms.Button();
            this.btn_Speichern = new System.Windows.Forms.Button();
            this.Label7 = new System.Windows.Forms.Label();
            this.Label8 = new System.Windows.Forms.Label();
            this.Label9 = new System.Windows.Forms.Label();
            this.textBox_Modul_A = new System.Windows.Forms.TextBox();
            this.Label11 = new System.Windows.Forms.Label();
            this.textBox_Absorber_A = new System.Windows.Forms.TextBox();
            this.Label12 = new System.Windows.Forms.Label();
            this.textBox_h0 = new System.Windows.Forms.TextBox();
            this.Label14 = new System.Windows.Forms.Label();
            this.Label15 = new System.Windows.Forms.Label();
            this.textBox_k1 = new System.Windows.Forms.TextBox();
            this.Label17 = new System.Windows.Forms.Label();
            this.textBox_k2 = new System.Windows.Forms.TextBox();
            this.Label18 = new System.Windows.Forms.Label();
            this.Label20 = new System.Windows.Forms.Label();
            this.Label21 = new System.Windows.Forms.Label();
            this.textBox_Kdir = new System.Windows.Forms.TextBox();
            this.textBox_Kdiff = new System.Windows.Forms.TextBox();
            this.Label23 = new System.Windows.Forms.Label();
            this.Label1 = new System.Windows.Forms.Label();
            this.Label2 = new System.Windows.Forms.Label();
            this.Label3 = new System.Windows.Forms.Label();
            this.textBox_Name = new System.Windows.Forms.TextBox();
            this.textBox_Firma = new System.Windows.Forms.TextBox();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.Label4 = new System.Windows.Forms.Label();
            this.textBox_Typ = new System.Windows.Forms.TextBox();
            this.Label25 = new System.Windows.Forms.Label();
            this.textBox_Kosten = new System.Windows.Forms.TextBox();
            this.Label26 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btn_Abbrechen
            // 
            resources.ApplyResources(this.btn_Abbrechen, "btn_Abbrechen");
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // btn_Überschreiben
            // 
            resources.ApplyResources(this.btn_Überschreiben, "btn_Überschreiben");
            this.btn_Überschreiben.Name = "btn_Überschreiben";
            this.btn_Überschreiben.UseVisualStyleBackColor = true;
            this.btn_Überschreiben.Click += new System.EventHandler(this.btn_Überschreiben_Click);
            // 
            // btn_Speichern_Unter
            // 
            resources.ApplyResources(this.btn_Speichern_Unter, "btn_Speichern_Unter");
            this.btn_Speichern_Unter.Name = "btn_Speichern_Unter";
            this.btn_Speichern_Unter.UseVisualStyleBackColor = true;
            this.btn_Speichern_Unter.Click += new System.EventHandler(this.btn_Speichern_Unter_Click);
            // 
            // btn_Speichern
            // 
            resources.ApplyResources(this.btn_Speichern, "btn_Speichern");
            this.btn_Speichern.Name = "btn_Speichern";
            this.btn_Speichern.UseVisualStyleBackColor = true;
            this.btn_Speichern.Click += new System.EventHandler(this.btn_Speichern_Click);
            // 
            // Label7
            // 
            resources.ApplyResources(this.Label7, "Label7");
            this.Label7.Name = "Label7";
            // 
            // Label8
            // 
            resources.ApplyResources(this.Label8, "Label8");
            this.Label8.Name = "Label8";
            // 
            // Label9
            // 
            resources.ApplyResources(this.Label9, "Label9");
            this.Label9.Name = "Label9";
            // 
            // textBox_Modul_A
            // 
            resources.ApplyResources(this.textBox_Modul_A, "textBox_Modul_A");
            this.textBox_Modul_A.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Modul_A.Name = "textBox_Modul_A";
            this.textBox_Modul_A.TextChanged += new System.EventHandler(this.textBox_Modul_A_TextChanged);
            // 
            // Label11
            // 
            resources.ApplyResources(this.Label11, "Label11");
            this.Label11.Name = "Label11";
            // 
            // textBox_Absorber_A
            // 
            resources.ApplyResources(this.textBox_Absorber_A, "textBox_Absorber_A");
            this.textBox_Absorber_A.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Absorber_A.Name = "textBox_Absorber_A";
            this.textBox_Absorber_A.TextChanged += new System.EventHandler(this.textBox_Absorber_A_TextChanged);
            // 
            // Label12
            // 
            resources.ApplyResources(this.Label12, "Label12");
            this.Label12.Name = "Label12";
            // 
            // textBox_h0
            // 
            resources.ApplyResources(this.textBox_h0, "textBox_h0");
            this.textBox_h0.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_h0.Name = "textBox_h0";
            this.textBox_h0.TextChanged += new System.EventHandler(this.textBox_h0_TextChanged);
            // 
            // Label14
            // 
            resources.ApplyResources(this.Label14, "Label14");
            this.Label14.Name = "Label14";
            // 
            // Label15
            // 
            resources.ApplyResources(this.Label15, "Label15");
            this.Label15.Name = "Label15";
            // 
            // textBox_k1
            // 
            resources.ApplyResources(this.textBox_k1, "textBox_k1");
            this.textBox_k1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_k1.Name = "textBox_k1";
            this.textBox_k1.TextChanged += new System.EventHandler(this.textBox_k1_TextChanged);
            // 
            // Label17
            // 
            resources.ApplyResources(this.Label17, "Label17");
            this.Label17.Name = "Label17";
            // 
            // textBox_k2
            // 
            resources.ApplyResources(this.textBox_k2, "textBox_k2");
            this.textBox_k2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_k2.Name = "textBox_k2";
            this.textBox_k2.TextChanged += new System.EventHandler(this.textBox_k2_TextChanged);
            // 
            // Label18
            // 
            resources.ApplyResources(this.Label18, "Label18");
            this.Label18.Name = "Label18";
            // 
            // Label20
            // 
            resources.ApplyResources(this.Label20, "Label20");
            this.Label20.Name = "Label20";
            // 
            // Label21
            // 
            resources.ApplyResources(this.Label21, "Label21");
            this.Label21.Name = "Label21";
            // 
            // textBox_Kdir
            // 
            resources.ApplyResources(this.textBox_Kdir, "textBox_Kdir");
            this.textBox_Kdir.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Kdir.Name = "textBox_Kdir";
            this.textBox_Kdir.TextChanged += new System.EventHandler(this.textBox_Kdir_TextChanged);
            // 
            // textBox_Kdiff
            // 
            resources.ApplyResources(this.textBox_Kdiff, "textBox_Kdiff");
            this.textBox_Kdiff.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Kdiff.Name = "textBox_Kdiff";
            this.textBox_Kdiff.TextChanged += new System.EventHandler(this.textBox_Kdiff_TextChanged);
            // 
            // Label23
            // 
            resources.ApplyResources(this.Label23, "Label23");
            this.Label23.Name = "Label23";
            // 
            // Label1
            // 
            resources.ApplyResources(this.Label1, "Label1");
            this.Label1.Name = "Label1";
            // 
            // Label2
            // 
            resources.ApplyResources(this.Label2, "Label2");
            this.Label2.Name = "Label2";
            // 
            // Label3
            // 
            resources.ApplyResources(this.Label3, "Label3");
            this.Label3.Name = "Label3";
            // 
            // textBox_Name
            // 
            resources.ApplyResources(this.textBox_Name, "textBox_Name");
            this.textBox_Name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Name.Name = "textBox_Name";
            // 
            // textBox_Firma
            // 
            resources.ApplyResources(this.textBox_Firma, "textBox_Firma");
            this.textBox_Firma.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Firma.Name = "textBox_Firma";
            // 
            // textBox_Beschreibung
            // 
            this.textBox_Beschreibung.AcceptsReturn = true;
            resources.ApplyResources(this.textBox_Beschreibung, "textBox_Beschreibung");
            this.textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            // 
            // Label4
            // 
            resources.ApplyResources(this.Label4, "Label4");
            this.Label4.Name = "Label4";
            // 
            // textBox_Typ
            // 
            resources.ApplyResources(this.textBox_Typ, "textBox_Typ");
            this.textBox_Typ.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Typ.Name = "textBox_Typ";
            // 
            // Label25
            // 
            resources.ApplyResources(this.Label25, "Label25");
            this.Label25.Name = "Label25";
            // 
            // textBox_Kosten
            // 
            resources.ApplyResources(this.textBox_Kosten, "textBox_Kosten");
            this.textBox_Kosten.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Kosten.Name = "textBox_Kosten";
            this.textBox_Kosten.TextChanged += new System.EventHandler(this.textBox_Kosten_TextChanged);
            // 
            // Label26
            // 
            resources.ApplyResources(this.Label26, "Label26");
            this.Label26.Name = "Label26";
            // 
            // Form_SolarDB
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.btn_Überschreiben);
            this.Controls.Add(this.btn_Speichern_Unter);
            this.Controls.Add(this.btn_Speichern);
            this.Controls.Add(this.Label7);
            this.Controls.Add(this.Label8);
            this.Controls.Add(this.Label9);
            this.Controls.Add(this.textBox_Modul_A);
            this.Controls.Add(this.Label11);
            this.Controls.Add(this.textBox_Absorber_A);
            this.Controls.Add(this.Label12);
            this.Controls.Add(this.textBox_h0);
            this.Controls.Add(this.Label14);
            this.Controls.Add(this.Label15);
            this.Controls.Add(this.textBox_k1);
            this.Controls.Add(this.Label17);
            this.Controls.Add(this.textBox_k2);
            this.Controls.Add(this.Label18);
            this.Controls.Add(this.Label20);
            this.Controls.Add(this.Label21);
            this.Controls.Add(this.textBox_Kdir);
            this.Controls.Add(this.textBox_Kdiff);
            this.Controls.Add(this.Label23);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.Label2);
            this.Controls.Add(this.Label3);
            this.Controls.Add(this.textBox_Name);
            this.Controls.Add(this.textBox_Firma);
            this.Controls.Add(this.textBox_Beschreibung);
            this.Controls.Add(this.Label4);
            this.Controls.Add(this.textBox_Typ);
            this.Controls.Add(this.Label25);
            this.Controls.Add(this.textBox_Kosten);
            this.Controls.Add(this.Label26);
            this.Name = "Form_SolarDB";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

private System.Windows.Forms.Button btn_Abbrechen;
private System.Windows.Forms.Button btn_Überschreiben;
private System.Windows.Forms.Button btn_Speichern_Unter;
private System.Windows.Forms.Button btn_Speichern;
private System.Windows.Forms.Label Label7;
private System.Windows.Forms.Label Label8;
private System.Windows.Forms.Label Label9;
private System.Windows.Forms.TextBox textBox_Modul_A;
private System.Windows.Forms.Label Label11;
private System.Windows.Forms.TextBox textBox_Absorber_A;
private System.Windows.Forms.Label Label12;
private System.Windows.Forms.TextBox textBox_h0;
private System.Windows.Forms.Label Label14;
private System.Windows.Forms.Label Label15;
private System.Windows.Forms.TextBox textBox_k1;
private System.Windows.Forms.Label Label17;
private System.Windows.Forms.TextBox textBox_k2;
private System.Windows.Forms.Label Label18;
private System.Windows.Forms.Label Label20;
private System.Windows.Forms.Label Label21;
private System.Windows.Forms.TextBox textBox_Kdir;
private System.Windows.Forms.TextBox textBox_Kdiff;
private System.Windows.Forms.Label Label23;
private System.Windows.Forms.Label Label1;
private System.Windows.Forms.Label Label2;
private System.Windows.Forms.Label Label3;
private System.Windows.Forms.TextBox textBox_Name;
private System.Windows.Forms.TextBox textBox_Firma;
private System.Windows.Forms.TextBox textBox_Beschreibung;
private System.Windows.Forms.Label Label4;
private System.Windows.Forms.TextBox textBox_Typ;
private System.Windows.Forms.Label Label25;
private System.Windows.Forms.TextBox textBox_Kosten;
private System.Windows.Forms.Label Label26;


 
    }
}