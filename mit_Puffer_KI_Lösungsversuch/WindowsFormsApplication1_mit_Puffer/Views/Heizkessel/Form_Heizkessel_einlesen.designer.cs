namespace WindowsFormsApplication1
{
    partial class Form_Heizkessel_einlesen 
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_Heizkessel_einlesen));
            btn_Uebernehmen = new System.Windows.Forms.Button();
            btn_Beenden = new System.Windows.Forms.Button();
            Liste_Heizkessel = new System.Windows.Forms.ListBox();
            btn_VDI3805 = new System.Windows.Forms.Button();
            Label3 = new System.Windows.Forms.Label();
            textBox_Firma = new System.Windows.Forms.TextBox();
            Label9 = new System.Windows.Forms.Label();
            textBox_Name = new System.Windows.Forms.TextBox();
            Label10 = new System.Windows.Forms.Label();
            textBox_Brennstoff = new System.Windows.Forms.TextBox();
            textBox_ThLeistung = new System.Windows.Forms.TextBox();
            Label12 = new System.Windows.Forms.Label();
            Label14 = new System.Windows.Forms.Label();
            Label16 = new System.Windows.Forms.Label();
            textBox__Wirkungsgrad = new System.Windows.Forms.TextBox();
            Label17 = new System.Windows.Forms.Label();
            textBox_Bauart = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            textBox_Versluste = new System.Windows.Forms.TextBox();
            label4 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            lbl_LeistungVon = new System.Windows.Forms.Label();
            lbl_LeistungBis = new System.Windows.Forms.Label();
            num_LeistungVon = new System.Windows.Forms.NumericUpDown();
            num_LeistungBis = new System.Windows.Forms.NumericUpDown();
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
            // Liste_Heizkessel
            // 
            resources.ApplyResources(Liste_Heizkessel, "Liste_Heizkessel");
            Liste_Heizkessel.ForeColor = System.Drawing.Color.Black;
            Liste_Heizkessel.Name = "Liste_Heizkessel";
            Liste_Heizkessel.SelectedIndexChanged += Liste_WP_SelectedIndexChanged;
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
            // textBox_Brennstoff
            // 
            textBox_Brennstoff.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Brennstoff, "textBox_Brennstoff");
            textBox_Brennstoff.ForeColor = System.Drawing.Color.Black;
            textBox_Brennstoff.Name = "textBox_Brennstoff";
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
            // Label14
            // 
            resources.ApplyResources(Label14, "Label14");
            Label14.BackColor = System.Drawing.Color.Black;
            Label14.ForeColor = System.Drawing.Color.White;
            Label14.Name = "Label14";
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
            // textBox_Bauart
            // 
            textBox_Bauart.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Bauart, "textBox_Bauart");
            textBox_Bauart.ForeColor = System.Drawing.Color.Black;
            textBox_Bauart.Name = "textBox_Bauart";
            // 
            // label1
            // 
            resources.ApplyResources(label1, "label1");
            label1.ForeColor = System.Drawing.Color.Black;
            label1.Name = "label1";
            // 
            // textBox_Versluste
            // 
            textBox_Versluste.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Versluste, "textBox_Versluste");
            textBox_Versluste.ForeColor = System.Drawing.Color.Black;
            textBox_Versluste.Name = "textBox_Versluste";
            // 
            // label4
            // 
            resources.ApplyResources(label4, "label4");
            label4.BackColor = System.Drawing.Color.Black;
            label4.ForeColor = System.Drawing.Color.White;
            label4.Name = "label4";
            // 
            // label5
            // 
            resources.ApplyResources(label5, "label5");
            label5.BackColor = System.Drawing.Color.Black;
            label5.ForeColor = System.Drawing.Color.White;
            label5.Name = "label5";
            // 
            // lbl_LeistungVon
            // 
            resources.ApplyResources(lbl_LeistungVon, "lbl_LeistungVon");
            lbl_LeistungVon.ForeColor = System.Drawing.Color.Black;
            lbl_LeistungVon.Name = "lbl_LeistungVon";
            // 
            // lbl_LeistungBis
            // 
            resources.ApplyResources(lbl_LeistungBis, "lbl_LeistungBis");
            lbl_LeistungBis.ForeColor = System.Drawing.Color.Black;
            lbl_LeistungBis.Name = "lbl_LeistungBis";
            // 
            // num_LeistungVon
            // 
            num_LeistungVon.DecimalPlaces = 1;
            num_LeistungVon.ForeColor = System.Drawing.Color.Black;
            resources.ApplyResources(num_LeistungVon, "num_LeistungVon");
            num_LeistungVon.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            num_LeistungVon.Name = "num_LeistungVon";
            num_LeistungVon.Value = new decimal(new int[] { 10, 0, 0, 0 });
            num_LeistungVon.ValueChanged += Leistungsfilter_ValueChanged;
            // 
            // num_LeistungBis
            // 
            num_LeistungBis.DecimalPlaces = 1;
            num_LeistungBis.ForeColor = System.Drawing.Color.Black;
            resources.ApplyResources(num_LeistungBis, "num_LeistungBis");
            num_LeistungBis.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            num_LeistungBis.Name = "num_LeistungBis";
            num_LeistungBis.Value = new decimal(new int[] { 200, 0, 0, 0 });
            num_LeistungBis.ValueChanged += Leistungsfilter_ValueChanged;
            // 
            // Form_Heizkessel_einlesen
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label1);
            Controls.Add(textBox_Versluste);
            Controls.Add(btn_Uebernehmen);
            Controls.Add(btn_Beenden);
            Controls.Add(Liste_Heizkessel);
            Controls.Add(btn_VDI3805);
            Controls.Add(Label3);
            Controls.Add(textBox_Firma);
            Controls.Add(Label9);
            Controls.Add(textBox_Name);
            Controls.Add(Label10);
            Controls.Add(textBox_Brennstoff);
            Controls.Add(textBox_ThLeistung);
            Controls.Add(Label12);
            Controls.Add(Label14);
            Controls.Add(Label16);
            Controls.Add(textBox__Wirkungsgrad);
            Controls.Add(Label17);
            Controls.Add(textBox_Bauart);
            Controls.Add(lbl_LeistungVon);
            Controls.Add(num_LeistungVon);
            Controls.Add(lbl_LeistungBis);
            Controls.Add(num_LeistungBis);
            ForeColor = System.Drawing.Color.Black;
            Name = "Form_Heizkessel_einlesen";
            ((System.ComponentModel.ISupportInitialize)num_LeistungVon).EndInit();
            ((System.ComponentModel.ISupportInitialize)num_LeistungBis).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btn_Uebernehmen;
private System.Windows.Forms.Button btn_Beenden;
private System.Windows.Forms.ListBox Liste_Heizkessel;
private System.Windows.Forms.Button btn_VDI3805;
private System.Windows.Forms.Label Label3;
private System.Windows.Forms.TextBox textBox_Firma;
private System.Windows.Forms.Label Label9;
private System.Windows.Forms.TextBox textBox_Name;
private System.Windows.Forms.Label Label10;
private System.Windows.Forms.TextBox textBox_Brennstoff;
private System.Windows.Forms.TextBox textBox_ThLeistung;
private System.Windows.Forms.Label Label12;
private System.Windows.Forms.Label Label14;
private System.Windows.Forms.Label Label16;
private System.Windows.Forms.TextBox textBox__Wirkungsgrad;
private System.Windows.Forms.Label Label17;
private System.Windows.Forms.TextBox textBox_Bauart;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox_Versluste;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lbl_LeistungVon;
        private System.Windows.Forms.Label lbl_LeistungBis;
        private System.Windows.Forms.NumericUpDown num_LeistungVon;
        private System.Windows.Forms.NumericUpDown num_LeistungBis;
    }
}