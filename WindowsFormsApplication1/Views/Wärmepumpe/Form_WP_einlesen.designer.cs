namespace WindowsFormsApplication1
{
    partial class Form_WP_einlesen
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_WP_einlesen));
            btn_Uebernehmen = new System.Windows.Forms.Button();
            btn_Beenden = new System.Windows.Forms.Button();
            Liste_WP = new System.Windows.Forms.ListBox();
            btn_VDI3805 = new System.Windows.Forms.Button();
            Label3 = new System.Windows.Forms.Label();
            textBox_Firma = new System.Windows.Forms.TextBox();
            Label9 = new System.Windows.Forms.Label();
            textBox_Name = new System.Windows.Forms.TextBox();
            Label10 = new System.Windows.Forms.Label();
            textBox_Aufstellung = new System.Windows.Forms.TextBox();
            textBox_ThLeistung = new System.Windows.Forms.TextBox();
            Label12 = new System.Windows.Forms.Label();
            Label13 = new System.Windows.Forms.Label();
            textBox_Zusatzheizung = new System.Windows.Forms.TextBox();
            Label14 = new System.Windows.Forms.Label();
            Label15 = new System.Windows.Forms.Label();
            Label16 = new System.Windows.Forms.Label();
            textBox__Wirkungsgrad = new System.Windows.Forms.TextBox();
            Label17 = new System.Windows.Forms.Label();
            textBox_Typ = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            textBox_Stufen = new System.Windows.Forms.TextBox();
            label4 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            textBox_MaxVorlauf = new System.Windows.Forms.TextBox();
            textBox_Kuehlleistung = new System.Windows.Forms.TextBox();
            label6 = new System.Windows.Forms.Label();
            label7 = new System.Windows.Forms.Label();
            num_LeistungVon = new System.Windows.Forms.NumericUpDown();
            num_LeistungBis = new System.Windows.Forms.NumericUpDown();
            lbl_LeistungFilter = new System.Windows.Forms.Label();
            lbl_LeistungBis = new System.Windows.Forms.Label();
            lbl_Filter = new System.Windows.Forms.Label();
            txt_Filter = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)num_LeistungVon).BeginInit();
            ((System.ComponentModel.ISupportInitialize)num_LeistungBis).BeginInit();
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
            // Liste_WP
            // 
            resources.ApplyResources(Liste_WP, "Liste_WP");
            Liste_WP.ForeColor = System.Drawing.Color.Black;
            Liste_WP.Name = "Liste_WP";
            // Mehrfachauswahl: der Anwender kann mehrere VDI-Eintraege in einem Vorgang laden.
            Liste_WP.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            Liste_WP.SelectedIndexChanged += Liste_WP_SelectedIndexChanged;
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
            // Label10
            // 
            resources.ApplyResources(Label10, "Label10");
            Label10.ForeColor = System.Drawing.Color.Black;
            Label10.Name = "Label10";
            // 
            // textBox_Aufstellung
            // 
            textBox_Aufstellung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Aufstellung, "textBox_Aufstellung");
            textBox_Aufstellung.ForeColor = System.Drawing.Color.Black;
            textBox_Aufstellung.Name = "textBox_Aufstellung";
            // 
            // textBox_ThLeistung
            // 
            textBox_ThLeistung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_ThLeistung, "textBox_ThLeistung");
            textBox_ThLeistung.ForeColor = System.Drawing.Color.Black;
            textBox_ThLeistung.Name = "textBox_ThLeistung";
            // 
            // Label12
            // 
            resources.ApplyResources(Label12, "Label12");
            Label12.ForeColor = System.Drawing.Color.Black;
            Label12.Name = "Label12";
            // 
            // Label13
            // 
            resources.ApplyResources(Label13, "Label13");
            Label13.ForeColor = System.Drawing.Color.Black;
            Label13.Name = "Label13";
            // 
            // textBox_Zusatzheizung
            // 
            textBox_Zusatzheizung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Zusatzheizung, "textBox_Zusatzheizung");
            textBox_Zusatzheizung.ForeColor = System.Drawing.Color.Black;
            textBox_Zusatzheizung.Name = "textBox_Zusatzheizung";
            // 
            // Label14
            // 
            resources.ApplyResources(Label14, "Label14");
            Label14.BackColor = System.Drawing.Color.Black;
            Label14.ForeColor = System.Drawing.Color.White;
            Label14.Name = "Label14";
            // 
            // Label15
            // 
            resources.ApplyResources(Label15, "Label15");
            Label15.BackColor = System.Drawing.Color.Black;
            Label15.ForeColor = System.Drawing.Color.White;
            Label15.Name = "Label15";
            // 
            // Label16
            // 
            resources.ApplyResources(Label16, "Label16");
            Label16.ForeColor = System.Drawing.Color.Black;
            Label16.Name = "Label16";
            // 
            // textBox__Wirkungsgrad
            // 
            textBox__Wirkungsgrad.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox__Wirkungsgrad, "textBox__Wirkungsgrad");
            textBox__Wirkungsgrad.ForeColor = System.Drawing.Color.Black;
            textBox__Wirkungsgrad.Name = "textBox__Wirkungsgrad";
            // 
            // Label17
            // 
            resources.ApplyResources(Label17, "Label17");
            Label17.ForeColor = System.Drawing.Color.Black;
            Label17.Name = "Label17";
            // 
            // textBox_Typ
            // 
            textBox_Typ.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Typ, "textBox_Typ");
            textBox_Typ.ForeColor = System.Drawing.Color.Black;
            textBox_Typ.Name = "textBox_Typ";
            // 
            // label1
            // 
            resources.ApplyResources(label1, "label1");
            label1.ForeColor = System.Drawing.Color.Black;
            label1.Name = "label1";
            // 
            // textBox_Stufen
            // 
            textBox_Stufen.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Stufen, "textBox_Stufen");
            textBox_Stufen.ForeColor = System.Drawing.Color.Black;
            textBox_Stufen.Name = "textBox_Stufen";
            // 
            // label4
            // 
            resources.ApplyResources(label4, "label4");
            label4.Name = "label4";
            // 
            // label5
            // 
            resources.ApplyResources(label5, "label5");
            label5.ForeColor = System.Drawing.Color.Black;
            label5.Name = "label5";
            // 
            // textBox_MaxVorlauf
            // 
            textBox_MaxVorlauf.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_MaxVorlauf, "textBox_MaxVorlauf");
            textBox_MaxVorlauf.ForeColor = System.Drawing.Color.Black;
            textBox_MaxVorlauf.Name = "textBox_MaxVorlauf";
            // 
            // textBox_Kuehlleistung
            // 
            textBox_Kuehlleistung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Kuehlleistung, "textBox_Kuehlleistung");
            textBox_Kuehlleistung.ForeColor = System.Drawing.Color.Black;
            textBox_Kuehlleistung.Name = "textBox_Kuehlleistung";
            // 
            // label6
            // 
            resources.ApplyResources(label6, "label6");
            label6.ForeColor = System.Drawing.Color.Black;
            label6.Name = "label6";
            // 
            // label7
            // 
            resources.ApplyResources(label7, "label7");
            label7.BackColor = System.Drawing.Color.Black;
            label7.ForeColor = System.Drawing.Color.White;
            label7.Name = "label7";
            // 
            // num_LeistungVon
            // 
            resources.ApplyResources(num_LeistungVon, "num_LeistungVon");
            num_LeistungVon.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            num_LeistungVon.Name = "num_LeistungVon";
            num_LeistungVon.ValueChanged += Leistungsfilter_ValueChanged;
            // 
            // num_LeistungBis
            // 
            resources.ApplyResources(num_LeistungBis, "num_LeistungBis");
            num_LeistungBis.Name = "num_LeistungBis";
            num_LeistungBis.Value = new decimal(new int[] { 100, 0, 0, 0 });
            num_LeistungBis.ValueChanged += Leistungsfilter_ValueChanged;
            // 
            // lbl_LeistungFilter
            // 
            resources.ApplyResources(lbl_LeistungFilter, "lbl_LeistungFilter");
            lbl_LeistungFilter.ForeColor = System.Drawing.Color.Black;
            lbl_LeistungFilter.Name = "lbl_LeistungFilter";
            // 
            // lbl_LeistungBis
            // 
            resources.ApplyResources(lbl_LeistungBis, "lbl_LeistungBis");
            lbl_LeistungBis.ForeColor = System.Drawing.Color.Black;
            lbl_LeistungBis.Name = "lbl_LeistungBis";
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
            // Form_WP_einlesen
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(lbl_LeistungFilter);
            Controls.Add(num_LeistungVon);
            Controls.Add(lbl_LeistungBis);
            Controls.Add(num_LeistungBis);
            Controls.Add(textBox_Kuehlleistung);
            Controls.Add(label6);
            Controls.Add(label7);
            Controls.Add(label5);
            Controls.Add(textBox_MaxVorlauf);
            Controls.Add(label4);
            Controls.Add(label1);
            Controls.Add(textBox_Stufen);
            Controls.Add(btn_Uebernehmen);
            Controls.Add(btn_Beenden);
            Controls.Add(Liste_WP);
            Controls.Add(btn_VDI3805);
            Controls.Add(Label3);
            Controls.Add(textBox_Firma);
            Controls.Add(Label9);
            Controls.Add(textBox_Name);
            Controls.Add(Label10);
            Controls.Add(textBox_Aufstellung);
            Controls.Add(textBox_ThLeistung);
            Controls.Add(Label12);
            Controls.Add(Label13);
            Controls.Add(textBox_Zusatzheizung);
            Controls.Add(Label14);
            Controls.Add(Label15);
            Controls.Add(Label16);
            Controls.Add(textBox__Wirkungsgrad);
            Controls.Add(Label17);
            Controls.Add(textBox_Typ);
            Controls.Add(lbl_Filter);
            Controls.Add(txt_Filter);
            ForeColor = System.Drawing.Color.Black;
            Name = "Form_WP_einlesen";
            ((System.ComponentModel.ISupportInitialize)num_LeistungVon).EndInit();
            ((System.ComponentModel.ISupportInitialize)num_LeistungBis).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btn_Uebernehmen;
private System.Windows.Forms.Button btn_Beenden;
private System.Windows.Forms.ListBox Liste_WP;
private System.Windows.Forms.Button btn_VDI3805;
private System.Windows.Forms.Label Label3;
private System.Windows.Forms.TextBox textBox_Firma;
private System.Windows.Forms.Label Label9;
private System.Windows.Forms.TextBox textBox_Name;
private System.Windows.Forms.Label Label10;
private System.Windows.Forms.TextBox textBox_Aufstellung;
private System.Windows.Forms.TextBox textBox_ThLeistung;
private System.Windows.Forms.Label Label12;
private System.Windows.Forms.Label Label13;
private System.Windows.Forms.TextBox textBox_Zusatzheizung;
private System.Windows.Forms.Label Label14;
private System.Windows.Forms.Label Label15;
private System.Windows.Forms.Label Label16;
private System.Windows.Forms.TextBox textBox__Wirkungsgrad;
private System.Windows.Forms.Label Label17;
private System.Windows.Forms.TextBox textBox_Typ;
private System.Windows.Forms.Label label1;
private System.Windows.Forms.TextBox textBox_Stufen;
private System.Windows.Forms.Label label4;
private System.Windows.Forms.Label label5;
private System.Windows.Forms.TextBox textBox_MaxVorlauf;
private System.Windows.Forms.TextBox textBox_Kuehlleistung;
private System.Windows.Forms.Label label6;
private System.Windows.Forms.Label label7;
        private System.Windows.Forms.NumericUpDown num_LeistungVon;
        private System.Windows.Forms.NumericUpDown num_LeistungBis;
        private System.Windows.Forms.Label lbl_LeistungFilter;
        private System.Windows.Forms.Label lbl_LeistungBis;
        private System.Windows.Forms.Label lbl_Filter;
        private System.Windows.Forms.TextBox txt_Filter;


 
    }
}