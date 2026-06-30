namespace WindowsFormsApplication1
{
    partial class Form_SolarKollektoren_einlesen
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_SolarKollektoren_einlesen));
            this.Label2 = new System.Windows.Forms.Label();
            this.btn_Uebernehmen = new System.Windows.Forms.Button();
            this.btn_Beenden = new System.Windows.Forms.Button();
            this.Liste_Kollektoren = new System.Windows.Forms.ListBox();
            this.btn_VDI3805 = new System.Windows.Forms.Button();
            this.Label3 = new System.Windows.Forms.Label();
            this.textBox_Firma = new System.Windows.Forms.TextBox();
            this.Label9 = new System.Windows.Forms.Label();
            this.textBox_Name = new System.Windows.Forms.TextBox();
            this.Label17 = new System.Windows.Forms.Label();
            this.textBox_Bauart = new System.Windows.Forms.TextBox();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label13 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.textBox_Kdir = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.textBox_Kdiff = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.textBox_a2 = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox_a1 = new System.Windows.Forms.TextBox();
            this.Label10 = new System.Windows.Forms.Label();
            this.textBox_Aperturflaeche = new System.Windows.Forms.TextBox();
            this.textBox_Leistung = new System.Windows.Forms.TextBox();
            this.Label12 = new System.Windows.Forms.Label();
            this.Label14 = new System.Windows.Forms.Label();
            this.Label16 = new System.Windows.Forms.Label();
            this.textBox_h0 = new System.Windows.Forms.TextBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // Label2
            // 
            resources.ApplyResources(this.Label2, "Label2");
            this.Label2.Name = "Label2";
            // 
            // btn_Uebernehmen
            // 
            resources.ApplyResources(this.btn_Uebernehmen, "btn_Uebernehmen");
            this.btn_Uebernehmen.ForeColor = System.Drawing.Color.Black;
            this.btn_Uebernehmen.Image = global::WindowsFormsApplication1.Properties.Resources.save_icon_36513;
            this.btn_Uebernehmen.Name = "btn_Uebernehmen";
            this.btn_Uebernehmen.UseVisualStyleBackColor = true;
            this.btn_Uebernehmen.Click += new System.EventHandler(this.btn_Uebernehmen_Click);
            // 
            // btn_Beenden
            // 
            resources.ApplyResources(this.btn_Beenden, "btn_Beenden");
            this.btn_Beenden.ForeColor = System.Drawing.Color.Black;
            this.btn_Beenden.Name = "btn_Beenden";
            this.btn_Beenden.UseVisualStyleBackColor = true;
            this.btn_Beenden.Click += new System.EventHandler(this.btn_Beenden_Click);
            // 
            // Liste_Kollektoren
            // 
            resources.ApplyResources(this.Liste_Kollektoren, "Liste_Kollektoren");
            this.Liste_Kollektoren.ForeColor = System.Drawing.Color.Black;
            this.Liste_Kollektoren.Name = "Liste_Kollektoren";
            this.Liste_Kollektoren.SelectedIndexChanged += new System.EventHandler(this.Liste_WP_SelectedIndexChanged);
            // 
            // btn_VDI3805
            // 
            resources.ApplyResources(this.btn_VDI3805, "btn_VDI3805");
            this.btn_VDI3805.ForeColor = System.Drawing.Color.Black;
            this.btn_VDI3805.Name = "btn_VDI3805";
            this.btn_VDI3805.UseVisualStyleBackColor = true;
            this.btn_VDI3805.Click += new System.EventHandler(this.btn_VDI3805_Click);
            // 
            // Label3
            // 
            resources.ApplyResources(this.Label3, "Label3");
            this.Label3.ForeColor = System.Drawing.Color.Black;
            this.Label3.Name = "Label3";
            // 
            // textBox_Firma
            // 
            this.textBox_Firma.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Firma, "textBox_Firma");
            this.textBox_Firma.ForeColor = System.Drawing.Color.Black;
            this.textBox_Firma.Name = "textBox_Firma";
            // 
            // Label9
            // 
            resources.ApplyResources(this.Label9, "Label9");
            this.Label9.ForeColor = System.Drawing.Color.Black;
            this.Label9.Name = "Label9";
            // 
            // textBox_Name
            // 
            this.textBox_Name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Name, "textBox_Name");
            this.textBox_Name.ForeColor = System.Drawing.Color.Black;
            this.textBox_Name.Name = "textBox_Name";
            // 
            // Label17
            // 
            resources.ApplyResources(this.Label17, "Label17");
            this.Label17.ForeColor = System.Drawing.Color.Black;
            this.Label17.Name = "Label17";
            // 
            // textBox_Bauart
            // 
            this.textBox_Bauart.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Bauart, "textBox_Bauart");
            this.textBox_Bauart.ForeColor = System.Drawing.Color.Black;
            this.textBox_Bauart.Name = "textBox_Bauart";
            // 
            // textBox_Beschreibung
            // 
            this.textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Beschreibung, "textBox_Beschreibung");
            this.textBox_Beschreibung.ForeColor = System.Drawing.Color.Black;
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.ForeColor = System.Drawing.Color.Black;
            this.label6.Name = "label6";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label13);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Controls.Add(this.textBox_Kdir);
            this.groupBox1.Controls.Add(this.label11);
            this.groupBox1.Controls.Add(this.textBox_Kdiff);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.textBox_a2);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.textBox_a1);
            this.groupBox1.Controls.Add(this.Label10);
            this.groupBox1.Controls.Add(this.textBox_Aperturflaeche);
            this.groupBox1.Controls.Add(this.textBox_Leistung);
            this.groupBox1.Controls.Add(this.Label12);
            this.groupBox1.Controls.Add(this.Label14);
            this.groupBox1.Controls.Add(this.Label16);
            this.groupBox1.Controls.Add(this.textBox_h0);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // label13
            // 
            resources.ApplyResources(this.label13, "label13");
            this.label13.BackColor = System.Drawing.Color.Black;
            this.label13.ForeColor = System.Drawing.Color.White;
            this.label13.Name = "label13";
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.ForeColor = System.Drawing.Color.Black;
            this.label8.Name = "label8";
            // 
            // textBox_Kdir
            // 
            this.textBox_Kdir.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Kdir, "textBox_Kdir");
            this.textBox_Kdir.ForeColor = System.Drawing.Color.Black;
            this.textBox_Kdir.Name = "textBox_Kdir";
            // 
            // label11
            // 
            resources.ApplyResources(this.label11, "label11");
            this.label11.ForeColor = System.Drawing.Color.Black;
            this.label11.Name = "label11";
            // 
            // textBox_Kdiff
            // 
            this.textBox_Kdiff.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Kdiff, "textBox_Kdiff");
            this.textBox_Kdiff.ForeColor = System.Drawing.Color.Black;
            this.textBox_Kdiff.Name = "textBox_Kdiff";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.BackColor = System.Drawing.Color.Black;
            this.label5.ForeColor = System.Drawing.Color.White;
            this.label5.Name = "label5";
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.ForeColor = System.Drawing.Color.Black;
            this.label7.Name = "label7";
            // 
            // textBox_a2
            // 
            this.textBox_a2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_a2, "textBox_a2");
            this.textBox_a2.ForeColor = System.Drawing.Color.Black;
            this.textBox_a2.Name = "textBox_a2";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.BackColor = System.Drawing.Color.Black;
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Name = "label4";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.ForeColor = System.Drawing.Color.Black;
            this.label1.Name = "label1";
            // 
            // textBox_a1
            // 
            this.textBox_a1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_a1, "textBox_a1");
            this.textBox_a1.ForeColor = System.Drawing.Color.Black;
            this.textBox_a1.Name = "textBox_a1";
            // 
            // Label10
            // 
            resources.ApplyResources(this.Label10, "Label10");
            this.Label10.ForeColor = System.Drawing.Color.Black;
            this.Label10.Name = "Label10";
            // 
            // textBox_Aperturflaeche
            // 
            this.textBox_Aperturflaeche.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Aperturflaeche, "textBox_Aperturflaeche");
            this.textBox_Aperturflaeche.ForeColor = System.Drawing.Color.Black;
            this.textBox_Aperturflaeche.Name = "textBox_Aperturflaeche";
            // 
            // textBox_Leistung
            // 
            this.textBox_Leistung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Leistung, "textBox_Leistung");
            this.textBox_Leistung.ForeColor = System.Drawing.Color.Black;
            this.textBox_Leistung.Name = "textBox_Leistung";
            // 
            // Label12
            // 
            resources.ApplyResources(this.Label12, "Label12");
            this.Label12.ForeColor = System.Drawing.Color.Black;
            this.Label12.Name = "Label12";
            // 
            // Label14
            // 
            resources.ApplyResources(this.Label14, "Label14");
            this.Label14.BackColor = System.Drawing.Color.Black;
            this.Label14.ForeColor = System.Drawing.Color.White;
            this.Label14.Name = "Label14";
            // 
            // Label16
            // 
            resources.ApplyResources(this.Label16, "Label16");
            this.Label16.ForeColor = System.Drawing.Color.Black;
            this.Label16.Name = "Label16";
            // 
            // textBox_h0
            // 
            this.textBox_h0.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_h0, "textBox_h0");
            this.textBox_h0.ForeColor = System.Drawing.Color.Black;
            this.textBox_h0.Name = "textBox_h0";
            // 
            // Form_SolarKollektoren_einlesen
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.textBox_Beschreibung);
            this.Controls.Add(this.Label2);
            this.Controls.Add(this.btn_Uebernehmen);
            this.Controls.Add(this.btn_Beenden);
            this.Controls.Add(this.Liste_Kollektoren);
            this.Controls.Add(this.btn_VDI3805);
            this.Controls.Add(this.Label3);
            this.Controls.Add(this.textBox_Firma);
            this.Controls.Add(this.Label9);
            this.Controls.Add(this.textBox_Name);
            this.Controls.Add(this.Label17);
            this.Controls.Add(this.textBox_Bauart);
            this.ForeColor = System.Drawing.Color.Black;
            this.Name = "Form_SolarKollektoren_einlesen";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label Label2;
private System.Windows.Forms.Button btn_Uebernehmen;
private System.Windows.Forms.Button btn_Beenden;
private System.Windows.Forms.ListBox Liste_Kollektoren;
private System.Windows.Forms.Button btn_VDI3805;
private System.Windows.Forms.Label Label3;
private System.Windows.Forms.TextBox textBox_Firma;
private System.Windows.Forms.Label Label9;
private System.Windows.Forms.TextBox textBox_Name;
private System.Windows.Forms.Label Label17;
private System.Windows.Forms.TextBox textBox_Bauart;
        private System.Windows.Forms.TextBox textBox_Beschreibung;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox textBox_Kdir;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox textBox_Kdiff;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textBox_a2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox_a1;
        private System.Windows.Forms.Label Label10;
        private System.Windows.Forms.TextBox textBox_Aperturflaeche;
        private System.Windows.Forms.TextBox textBox_Leistung;
        private System.Windows.Forms.Label Label12;
        private System.Windows.Forms.Label Label14;
        private System.Windows.Forms.Label Label16;
        private System.Windows.Forms.TextBox textBox_h0;
    }
}