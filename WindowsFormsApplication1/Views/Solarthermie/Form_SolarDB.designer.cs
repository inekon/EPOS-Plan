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
            btn_Abbrechen = new System.Windows.Forms.Button();
            btn_Überschreiben = new System.Windows.Forms.Button();
            btn_Speichern_Unter = new System.Windows.Forms.Button();
            btn_Speichern = new System.Windows.Forms.Button();
            Label7 = new System.Windows.Forms.Label();
            Label8 = new System.Windows.Forms.Label();
            Label9 = new System.Windows.Forms.Label();
            textBox_Modul_A = new System.Windows.Forms.TextBox();
            Label11 = new System.Windows.Forms.Label();
            textBox_Absorber_A = new System.Windows.Forms.TextBox();
            Label12 = new System.Windows.Forms.Label();
            textBox_h0 = new System.Windows.Forms.TextBox();
            Label14 = new System.Windows.Forms.Label();
            Label15 = new System.Windows.Forms.Label();
            textBox_k1 = new System.Windows.Forms.TextBox();
            Label17 = new System.Windows.Forms.Label();
            textBox_k2 = new System.Windows.Forms.TextBox();
            Label18 = new System.Windows.Forms.Label();
            Label20 = new System.Windows.Forms.Label();
            Label21 = new System.Windows.Forms.Label();
            textBox_Kdir = new System.Windows.Forms.TextBox();
            textBox_Kdiff = new System.Windows.Forms.TextBox();
            Label23 = new System.Windows.Forms.Label();
            Label1 = new System.Windows.Forms.Label();
            Label2 = new System.Windows.Forms.Label();
            Label3 = new System.Windows.Forms.Label();
            textBox_Name = new System.Windows.Forms.TextBox();
            textBox_Firma = new System.Windows.Forms.TextBox();
            textBox_Beschreibung = new System.Windows.Forms.TextBox();
            Label4 = new System.Windows.Forms.Label();
            textBox_Typ = new System.Windows.Forms.TextBox();
            Label25 = new System.Windows.Forms.Label();
            textBox_Kosten = new System.Windows.Forms.TextBox();
            Label26 = new System.Windows.Forms.Label();
            textBox_Ruecklauf = new System.Windows.Forms.TextBox();
            label5 = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            textBox_Vorlauf = new System.Windows.Forms.TextBox();
            label10 = new System.Windows.Forms.Label();
            label13 = new System.Windows.Forms.Label();
            SuspendLayout();
            // 
            // btn_Abbrechen
            // 
            resources.ApplyResources(btn_Abbrechen, "btn_Abbrechen");
            btn_Abbrechen.Name = "btn_Abbrechen";
            btn_Abbrechen.UseVisualStyleBackColor = true;
            btn_Abbrechen.Click += btn_Abbrechen_Click;
            // 
            // btn_Überschreiben
            // 
            resources.ApplyResources(btn_Überschreiben, "btn_Überschreiben");
            btn_Überschreiben.Name = "btn_Überschreiben";
            btn_Überschreiben.UseVisualStyleBackColor = true;
            btn_Überschreiben.Click += btn_Überschreiben_Click;
            // 
            // btn_Speichern_Unter
            // 
            resources.ApplyResources(btn_Speichern_Unter, "btn_Speichern_Unter");
            btn_Speichern_Unter.Name = "btn_Speichern_Unter";
            btn_Speichern_Unter.UseVisualStyleBackColor = true;
            btn_Speichern_Unter.Click += btn_Speichern_Unter_Click;
            // 
            // btn_Speichern
            // 
            resources.ApplyResources(btn_Speichern, "btn_Speichern");
            btn_Speichern.Name = "btn_Speichern";
            btn_Speichern.UseVisualStyleBackColor = true;
            btn_Speichern.Click += btn_Speichern_Click;
            // 
            // Label7
            // 
            resources.ApplyResources(Label7, "Label7");
            Label7.Name = "Label7";
            // 
            // Label8
            // 
            resources.ApplyResources(Label8, "Label8");
            Label8.Name = "Label8";
            // 
            // Label9
            // 
            resources.ApplyResources(Label9, "Label9");
            Label9.Name = "Label9";
            // 
            // textBox_Modul_A
            // 
            textBox_Modul_A.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Modul_A, "textBox_Modul_A");
            textBox_Modul_A.Name = "textBox_Modul_A";
            textBox_Modul_A.TextChanged += textBox_Modul_A_TextChanged;
            // 
            // Label11
            // 
            resources.ApplyResources(Label11, "Label11");
            Label11.Name = "Label11";
            // 
            // textBox_Absorber_A
            // 
            textBox_Absorber_A.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Absorber_A, "textBox_Absorber_A");
            textBox_Absorber_A.Name = "textBox_Absorber_A";
            textBox_Absorber_A.TextChanged += textBox_Absorber_A_TextChanged;
            // 
            // Label12
            // 
            resources.ApplyResources(Label12, "Label12");
            Label12.Name = "Label12";
            // 
            // textBox_h0
            // 
            textBox_h0.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_h0, "textBox_h0");
            textBox_h0.Name = "textBox_h0";
            textBox_h0.TextChanged += textBox_h0_TextChanged;
            // 
            // Label14
            // 
            resources.ApplyResources(Label14, "Label14");
            Label14.Name = "Label14";
            // 
            // Label15
            // 
            resources.ApplyResources(Label15, "Label15");
            Label15.Name = "Label15";
            // 
            // textBox_k1
            // 
            textBox_k1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_k1, "textBox_k1");
            textBox_k1.Name = "textBox_k1";
            textBox_k1.TextChanged += textBox_k1_TextChanged;
            // 
            // Label17
            // 
            resources.ApplyResources(Label17, "Label17");
            Label17.Name = "Label17";
            // 
            // textBox_k2
            // 
            textBox_k2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_k2, "textBox_k2");
            textBox_k2.Name = "textBox_k2";
            textBox_k2.TextChanged += textBox_k2_TextChanged;
            // 
            // Label18
            // 
            resources.ApplyResources(Label18, "Label18");
            Label18.Name = "Label18";
            // 
            // Label20
            // 
            resources.ApplyResources(Label20, "Label20");
            Label20.Name = "Label20";
            // 
            // Label21
            // 
            resources.ApplyResources(Label21, "Label21");
            Label21.Name = "Label21";
            // 
            // textBox_Kdir
            // 
            textBox_Kdir.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Kdir, "textBox_Kdir");
            textBox_Kdir.Name = "textBox_Kdir";
            textBox_Kdir.TextChanged += textBox_Kdir_TextChanged;
            // 
            // textBox_Kdiff
            // 
            textBox_Kdiff.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Kdiff, "textBox_Kdiff");
            textBox_Kdiff.Name = "textBox_Kdiff";
            textBox_Kdiff.TextChanged += textBox_Kdiff_TextChanged;
            // 
            // Label23
            // 
            resources.ApplyResources(Label23, "Label23");
            Label23.Name = "Label23";
            // 
            // Label1
            // 
            resources.ApplyResources(Label1, "Label1");
            Label1.Name = "Label1";
            // 
            // Label2
            // 
            resources.ApplyResources(Label2, "Label2");
            Label2.Name = "Label2";
            // 
            // Label3
            // 
            resources.ApplyResources(Label3, "Label3");
            Label3.Name = "Label3";
            // 
            // textBox_Name
            // 
            textBox_Name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Name, "textBox_Name");
            textBox_Name.Name = "textBox_Name";
            // 
            // textBox_Firma
            // 
            textBox_Firma.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Firma, "textBox_Firma");
            textBox_Firma.Name = "textBox_Firma";
            // 
            // textBox_Beschreibung
            // 
            textBox_Beschreibung.AcceptsReturn = true;
            textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Beschreibung, "textBox_Beschreibung");
            textBox_Beschreibung.Name = "textBox_Beschreibung";
            // 
            // Label4
            // 
            resources.ApplyResources(Label4, "Label4");
            Label4.Name = "Label4";
            // 
            // textBox_Typ
            // 
            textBox_Typ.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Typ, "textBox_Typ");
            textBox_Typ.Name = "textBox_Typ";
            // 
            // Label25
            // 
            resources.ApplyResources(Label25, "Label25");
            Label25.Name = "Label25";
            // 
            // textBox_Kosten
            // 
            textBox_Kosten.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Kosten, "textBox_Kosten");
            textBox_Kosten.Name = "textBox_Kosten";
            textBox_Kosten.TextChanged += textBox_Kosten_TextChanged;
            // 
            // Label26
            // 
            resources.ApplyResources(Label26, "Label26");
            Label26.Name = "Label26";
            // 
            // textBox_Ruecklauf
            // 
            textBox_Ruecklauf.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Ruecklauf, "textBox_Ruecklauf");
            textBox_Ruecklauf.Name = "textBox_Ruecklauf";
            textBox_Ruecklauf.TextChanged += textBox_Ruecklauf_TextChanged;
            // 
            // label5
            // 
            label5.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(label5, "label5");
            label5.ForeColor = System.Drawing.Color.Black;
            label5.Name = "label5";
            // 
            // label6
            // 
            resources.ApplyResources(label6, "label6");
            label6.Name = "label6";
            // 
            // textBox_Vorlauf
            // 
            textBox_Vorlauf.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Vorlauf, "textBox_Vorlauf");
            textBox_Vorlauf.Name = "textBox_Vorlauf";
            textBox_Vorlauf.TextChanged += textBox_Vorlauf_TextChanged;
            // 
            // label10
            // 
            label10.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(label10, "label10");
            label10.ForeColor = System.Drawing.Color.Black;
            label10.Name = "label10";
            // 
            // label13
            // 
            resources.ApplyResources(label13, "label13");
            label13.Name = "label13";
            // 
            // Form_SolarDB
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(textBox_Ruecklauf);
            Controls.Add(label5);
            Controls.Add(label6);
            Controls.Add(textBox_Vorlauf);
            Controls.Add(label10);
            Controls.Add(label13);
            Controls.Add(btn_Abbrechen);
            Controls.Add(btn_Überschreiben);
            Controls.Add(btn_Speichern_Unter);
            Controls.Add(btn_Speichern);
            Controls.Add(Label7);
            Controls.Add(Label8);
            Controls.Add(Label9);
            Controls.Add(textBox_Modul_A);
            Controls.Add(Label11);
            Controls.Add(textBox_Absorber_A);
            Controls.Add(Label12);
            Controls.Add(textBox_h0);
            Controls.Add(Label14);
            Controls.Add(Label15);
            Controls.Add(textBox_k1);
            Controls.Add(Label17);
            Controls.Add(textBox_k2);
            Controls.Add(Label18);
            Controls.Add(Label20);
            Controls.Add(Label21);
            Controls.Add(textBox_Kdir);
            Controls.Add(textBox_Kdiff);
            Controls.Add(Label23);
            Controls.Add(Label1);
            Controls.Add(Label2);
            Controls.Add(Label3);
            Controls.Add(textBox_Name);
            Controls.Add(textBox_Firma);
            Controls.Add(textBox_Beschreibung);
            Controls.Add(Label4);
            Controls.Add(textBox_Typ);
            Controls.Add(Label25);
            Controls.Add(textBox_Kosten);
            Controls.Add(Label26);
            Name = "Form_SolarDB";
            ResumeLayout(false);
            PerformLayout();

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
        private System.Windows.Forms.TextBox textBox_Ruecklauf;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBox_Vorlauf;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label13;
    }
}