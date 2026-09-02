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
            btn_Uebernehmen = new System.Windows.Forms.Button();
            btn_Beenden = new System.Windows.Forms.Button();
            Liste_Kollektoren = new System.Windows.Forms.ListBox();
            btn_VDI3805 = new System.Windows.Forms.Button();
            Label3 = new System.Windows.Forms.Label();
            textBox_Firma = new System.Windows.Forms.TextBox();
            Label9 = new System.Windows.Forms.Label();
            textBox_Name = new System.Windows.Forms.TextBox();
            Label17 = new System.Windows.Forms.Label();
            textBox_Bauart = new System.Windows.Forms.TextBox();
            textBox_Beschreibung = new System.Windows.Forms.TextBox();
            label6 = new System.Windows.Forms.Label();
            groupBox1 = new System.Windows.Forms.GroupBox();
            label13 = new System.Windows.Forms.Label();
            label8 = new System.Windows.Forms.Label();
            textBox_Kdir = new System.Windows.Forms.TextBox();
            label11 = new System.Windows.Forms.Label();
            textBox_Kdiff = new System.Windows.Forms.TextBox();
            label5 = new System.Windows.Forms.Label();
            label7 = new System.Windows.Forms.Label();
            textBox_a2 = new System.Windows.Forms.TextBox();
            label4 = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            textBox_a1 = new System.Windows.Forms.TextBox();
            Label10 = new System.Windows.Forms.Label();
            textBox_Aperturflaeche = new System.Windows.Forms.TextBox();
            textBox_Leistung = new System.Windows.Forms.TextBox();
            Label12 = new System.Windows.Forms.Label();
            Label14 = new System.Windows.Forms.Label();
            Label16 = new System.Windows.Forms.Label();
            textBox_h0 = new System.Windows.Forms.TextBox();
            num_AperturVon = new System.Windows.Forms.NumericUpDown();
            num_AperturBis = new System.Windows.Forms.NumericUpDown();
            lbl_AperturFilter = new System.Windows.Forms.Label();
            lbl_AperturBis = new System.Windows.Forms.Label();
            lbl_Filter = new System.Windows.Forms.Label();
            txt_Filter = new System.Windows.Forms.TextBox();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)num_AperturVon).BeginInit();
            ((System.ComponentModel.ISupportInitialize)num_AperturBis).BeginInit();
            SuspendLayout();
            // 
            // btn_Uebernehmen
            // 
            resources.ApplyResources(btn_Uebernehmen, "btn_Uebernehmen");
            btn_Uebernehmen.ForeColor = System.Drawing.Color.Black;
            btn_Uebernehmen.Image = Properties.Resources.save_icon_36513;
            btn_Uebernehmen.Name = "btn_Uebernehmen";
            btn_Uebernehmen.UseVisualStyleBackColor = true;
            btn_Uebernehmen.Click += btn_Uebernehmen_Click;
            // 
            // btn_Beenden
            // 
            resources.ApplyResources(btn_Beenden, "btn_Beenden");
            btn_Beenden.ForeColor = System.Drawing.Color.Black;
            btn_Beenden.Name = "btn_Beenden";
            btn_Beenden.UseVisualStyleBackColor = true;
            btn_Beenden.Click += btn_Beenden_Click;
            // 
            // Liste_Kollektoren
            // 
            resources.ApplyResources(Liste_Kollektoren, "Liste_Kollektoren");
            Liste_Kollektoren.ForeColor = System.Drawing.Color.Black;
            Liste_Kollektoren.Name = "Liste_Kollektoren";
            // Mehrfachauswahl: der Anwender kann mehrere VDI-Eintraege in einem Vorgang laden.
            Liste_Kollektoren.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            Liste_Kollektoren.SelectedIndexChanged += Liste_WP_SelectedIndexChanged;
            // 
            // btn_VDI3805
            // 
            resources.ApplyResources(btn_VDI3805, "btn_VDI3805");
            btn_VDI3805.ForeColor = System.Drawing.Color.Black;
            btn_VDI3805.Name = "btn_VDI3805";
            btn_VDI3805.UseVisualStyleBackColor = true;
            btn_VDI3805.Click += btn_VDI3805_Click;
            // 
            // Label3
            // 
            resources.ApplyResources(Label3, "Label3");
            Label3.ForeColor = System.Drawing.Color.Black;
            Label3.Name = "Label3";
            // 
            // textBox_Firma
            // 
            textBox_Firma.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Firma, "textBox_Firma");
            textBox_Firma.ForeColor = System.Drawing.Color.Black;
            textBox_Firma.Name = "textBox_Firma";
            // 
            // Label9
            // 
            resources.ApplyResources(Label9, "Label9");
            Label9.ForeColor = System.Drawing.Color.Black;
            Label9.Name = "Label9";
            // 
            // textBox_Name
            // 
            textBox_Name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Name, "textBox_Name");
            textBox_Name.ForeColor = System.Drawing.Color.Black;
            textBox_Name.Name = "textBox_Name";
            // 
            // Label17
            // 
            resources.ApplyResources(Label17, "Label17");
            Label17.ForeColor = System.Drawing.Color.Black;
            Label17.Name = "Label17";
            // 
            // textBox_Bauart
            // 
            textBox_Bauart.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Bauart, "textBox_Bauart");
            textBox_Bauart.ForeColor = System.Drawing.Color.Black;
            textBox_Bauart.Name = "textBox_Bauart";
            // 
            // textBox_Beschreibung
            // 
            textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Beschreibung, "textBox_Beschreibung");
            textBox_Beschreibung.ForeColor = System.Drawing.Color.Black;
            textBox_Beschreibung.Name = "textBox_Beschreibung";
            // 
            // label6
            // 
            resources.ApplyResources(label6, "label6");
            label6.ForeColor = System.Drawing.Color.Black;
            label6.Name = "label6";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(label13);
            groupBox1.Controls.Add(label8);
            groupBox1.Controls.Add(textBox_Kdir);
            groupBox1.Controls.Add(label11);
            groupBox1.Controls.Add(textBox_Kdiff);
            groupBox1.Controls.Add(label5);
            groupBox1.Controls.Add(label7);
            groupBox1.Controls.Add(textBox_a2);
            groupBox1.Controls.Add(label4);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(textBox_a1);
            groupBox1.Controls.Add(Label10);
            groupBox1.Controls.Add(textBox_Aperturflaeche);
            groupBox1.Controls.Add(textBox_Leistung);
            groupBox1.Controls.Add(Label12);
            groupBox1.Controls.Add(Label14);
            groupBox1.Controls.Add(Label16);
            groupBox1.Controls.Add(textBox_h0);
            resources.ApplyResources(groupBox1, "groupBox1");
            groupBox1.Name = "groupBox1";
            groupBox1.TabStop = false;
            // 
            // label13
            // 
            label13.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(label13, "label13");
            label13.ForeColor = System.Drawing.Color.White;
            label13.Name = "label13";
            // 
            // label8
            // 
            resources.ApplyResources(label8, "label8");
            label8.ForeColor = System.Drawing.Color.Black;
            label8.Name = "label8";
            // 
            // textBox_Kdir
            // 
            textBox_Kdir.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Kdir, "textBox_Kdir");
            textBox_Kdir.ForeColor = System.Drawing.Color.Black;
            textBox_Kdir.Name = "textBox_Kdir";
            // 
            // label11
            // 
            resources.ApplyResources(label11, "label11");
            label11.ForeColor = System.Drawing.Color.Black;
            label11.Name = "label11";
            // 
            // textBox_Kdiff
            // 
            textBox_Kdiff.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Kdiff, "textBox_Kdiff");
            textBox_Kdiff.ForeColor = System.Drawing.Color.Black;
            textBox_Kdiff.Name = "textBox_Kdiff";
            // 
            // label5
            // 
            label5.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(label5, "label5");
            label5.ForeColor = System.Drawing.Color.White;
            label5.Name = "label5";
            // 
            // label7
            // 
            resources.ApplyResources(label7, "label7");
            label7.ForeColor = System.Drawing.Color.Black;
            label7.Name = "label7";
            // 
            // textBox_a2
            // 
            textBox_a2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_a2, "textBox_a2");
            textBox_a2.ForeColor = System.Drawing.Color.Black;
            textBox_a2.Name = "textBox_a2";
            // 
            // label4
            // 
            label4.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(label4, "label4");
            label4.ForeColor = System.Drawing.Color.White;
            label4.Name = "label4";
            // 
            // label1
            // 
            resources.ApplyResources(label1, "label1");
            label1.ForeColor = System.Drawing.Color.Black;
            label1.Name = "label1";
            // 
            // textBox_a1
            // 
            textBox_a1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_a1, "textBox_a1");
            textBox_a1.ForeColor = System.Drawing.Color.Black;
            textBox_a1.Name = "textBox_a1";
            // 
            // Label10
            // 
            resources.ApplyResources(Label10, "Label10");
            Label10.ForeColor = System.Drawing.Color.Black;
            Label10.Name = "Label10";
            // 
            // textBox_Aperturflaeche
            // 
            textBox_Aperturflaeche.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Aperturflaeche, "textBox_Aperturflaeche");
            textBox_Aperturflaeche.ForeColor = System.Drawing.Color.Black;
            textBox_Aperturflaeche.Name = "textBox_Aperturflaeche";
            // 
            // textBox_Leistung
            // 
            textBox_Leistung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Leistung, "textBox_Leistung");
            textBox_Leistung.ForeColor = System.Drawing.Color.Black;
            textBox_Leistung.Name = "textBox_Leistung";
            // 
            // Label12
            // 
            resources.ApplyResources(Label12, "Label12");
            Label12.ForeColor = System.Drawing.Color.Black;
            Label12.Name = "Label12";
            // 
            // Label14
            // 
            Label14.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(Label14, "Label14");
            Label14.ForeColor = System.Drawing.Color.White;
            Label14.Name = "Label14";
            // 
            // Label16
            // 
            resources.ApplyResources(Label16, "Label16");
            Label16.ForeColor = System.Drawing.Color.Black;
            Label16.Name = "Label16";
            // 
            // textBox_h0
            // 
            textBox_h0.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_h0, "textBox_h0");
            textBox_h0.ForeColor = System.Drawing.Color.Black;
            textBox_h0.Name = "textBox_h0";
            // 
            // num_AperturVon
            // 
            num_AperturVon.DecimalPlaces = 2;
            resources.ApplyResources(num_AperturVon, "num_AperturVon");
            num_AperturVon.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            num_AperturVon.Name = "num_AperturVon";
            num_AperturVon.ValueChanged += Kollektorfilter_ValueChanged;
            // 
            // num_AperturBis
            // 
            num_AperturBis.DecimalPlaces = 2;
            resources.ApplyResources(num_AperturBis, "num_AperturBis");
            num_AperturBis.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            num_AperturBis.Name = "num_AperturBis";
            num_AperturBis.Value = new decimal(new int[] { 5, 0, 0, 0 });
            num_AperturBis.ValueChanged += Kollektorfilter_ValueChanged;
            // 
            // lbl_AperturFilter
            // 
            resources.ApplyResources(lbl_AperturFilter, "lbl_AperturFilter");
            lbl_AperturFilter.ForeColor = System.Drawing.Color.Black;
            lbl_AperturFilter.Name = "lbl_AperturFilter";
            // 
            // lbl_AperturBis
            // 
            resources.ApplyResources(lbl_AperturBis, "lbl_AperturBis");
            lbl_AperturBis.ForeColor = System.Drawing.Color.Black;
            lbl_AperturBis.Name = "lbl_AperturBis";
            // 
            // lbl_Filter
            // 
            resources.ApplyResources(lbl_Filter, "lbl_Filter");
            lbl_Filter.ForeColor = System.Drawing.Color.Black;
            lbl_Filter.Name = "lbl_Filter";
            // 
            // txt_Filter
            // 
            txt_Filter.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(txt_Filter, "txt_Filter");
            txt_Filter.ForeColor = System.Drawing.Color.Black;
            txt_Filter.Name = "txt_Filter";
            txt_Filter.TextChanged += Suchfilter_TextChanged;
            // 
            // Form_SolarKollektoren_einlesen
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(lbl_AperturFilter);
            Controls.Add(num_AperturVon);
            Controls.Add(lbl_AperturBis);
            Controls.Add(num_AperturBis);
            Controls.Add(groupBox1);
            Controls.Add(label6);
            Controls.Add(textBox_Beschreibung);
            Controls.Add(btn_Uebernehmen);
            Controls.Add(btn_Beenden);
            Controls.Add(Liste_Kollektoren);
            Controls.Add(btn_VDI3805);
            Controls.Add(Label3);
            Controls.Add(textBox_Firma);
            Controls.Add(Label9);
            Controls.Add(textBox_Name);
            Controls.Add(Label17);
            Controls.Add(textBox_Bauart);
            Controls.Add(lbl_Filter);
            Controls.Add(txt_Filter);
            ForeColor = System.Drawing.Color.Black;
            Name = "Form_SolarKollektoren_einlesen";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)num_AperturVon).EndInit();
            ((System.ComponentModel.ISupportInitialize)num_AperturBis).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion
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
        private System.Windows.Forms.NumericUpDown num_AperturVon;
        private System.Windows.Forms.NumericUpDown num_AperturBis;
        private System.Windows.Forms.Label lbl_AperturFilter;
        private System.Windows.Forms.Label lbl_AperturBis;
        private System.Windows.Forms.Label lbl_Filter;
        private System.Windows.Forms.TextBox txt_Filter;
    }
}