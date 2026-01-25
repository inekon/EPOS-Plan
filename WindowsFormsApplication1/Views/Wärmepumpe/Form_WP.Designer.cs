namespace WindowsFormsApplication1
{
    partial class Form_WP
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_WP));
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend2 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.btn_Beenden = new System.Windows.Forms.Button();
            this.label_WP = new System.Windows.Forms.Label();
            this.label30 = new System.Windows.Forms.Label();
            this.textBox_Nennleistung = new System.Windows.Forms.TextBox();
            this.label32 = new System.Windows.Forms.Label();
            this.textBox_Modulkosten = new System.Windows.Forms.TextBox();
            this.label29 = new System.Windows.Forms.Label();
            this.comboBox_Baujahr = new System.Windows.Forms.ComboBox();
            this.label28 = new System.Windows.Forms.Label();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.label27 = new System.Windows.Forms.Label();
            this.textBox_Hersteller = new System.Windows.Forms.TextBox();
            this.label26 = new System.Windows.Forms.Label();
            this.comboBox_Leistungsstufen = new System.Windows.Forms.ComboBox();
            this.label25 = new System.Windows.Forms.Label();
            this.comboBox_Waermepumpentyp = new System.Windows.Forms.ComboBox();
            this.label24 = new System.Windows.Forms.Label();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.label33 = new System.Windows.Forms.Label();
            this.listBox_WP = new System.Windows.Forms.ListBox();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBox_Aufstellung = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox_Heizstab = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.btn_Neu = new System.Windows.Forms.Button();
            this.btn_Kenndaten = new System.Windows.Forms.Button();
            this.btn_Speichern = new System.Windows.Forms.Button();
            this.btn_Loeschen = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.textBox_Name = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.chart2 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.textBox_Kuehlung = new System.Windows.Forms.TextBox();
            this.radioButton_Waerme = new System.Windows.Forms.RadioButton();
            this.radioButton_Kuehlung = new System.Windows.Forms.RadioButton();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart2)).BeginInit();
            this.SuspendLayout();
            // 
            // btn_Beenden
            // 
            resources.ApplyResources(this.btn_Beenden, "btn_Beenden");
            this.btn_Beenden.Name = "btn_Beenden";
            this.btn_Beenden.UseVisualStyleBackColor = true;
            this.btn_Beenden.Click += new System.EventHandler(this.butt_Beenden_Click);
            // 
            // label_WP
            // 
            resources.ApplyResources(this.label_WP, "label_WP");
            this.label_WP.Name = "label_WP";
            // 
            // label30
            // 
            resources.ApplyResources(this.label30, "label30");
            this.label30.Name = "label30";
            // 
            // textBox_Nennleistung
            // 
            resources.ApplyResources(this.textBox_Nennleistung, "textBox_Nennleistung");
            this.textBox_Nennleistung.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.textBox_Nennleistung.Name = "textBox_Nennleistung";
            // 
            // label32
            // 
            resources.ApplyResources(this.label32, "label32");
            this.label32.Name = "label32";
            // 
            // textBox_Modulkosten
            // 
            resources.ApplyResources(this.textBox_Modulkosten, "textBox_Modulkosten");
            this.textBox_Modulkosten.Name = "textBox_Modulkosten";
            this.textBox_Modulkosten.TextChanged += new System.EventHandler(this.textBox_Modulkosten_TextChanged);
            // 
            // label29
            // 
            resources.ApplyResources(this.label29, "label29");
            this.label29.Name = "label29";
            // 
            // comboBox_Baujahr
            // 
            resources.ApplyResources(this.comboBox_Baujahr, "comboBox_Baujahr");
            this.comboBox_Baujahr.FormattingEnabled = true;
            this.comboBox_Baujahr.Name = "comboBox_Baujahr";
            // 
            // label28
            // 
            resources.ApplyResources(this.label28, "label28");
            this.label28.Name = "label28";
            // 
            // textBox_Beschreibung
            // 
            resources.ApplyResources(this.textBox_Beschreibung, "textBox_Beschreibung");
            this.textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            // 
            // label27
            // 
            resources.ApplyResources(this.label27, "label27");
            this.label27.Name = "label27";
            // 
            // textBox_Hersteller
            // 
            resources.ApplyResources(this.textBox_Hersteller, "textBox_Hersteller");
            this.textBox_Hersteller.Name = "textBox_Hersteller";
            // 
            // label26
            // 
            resources.ApplyResources(this.label26, "label26");
            this.label26.Name = "label26";
            // 
            // comboBox_Leistungsstufen
            // 
            resources.ApplyResources(this.comboBox_Leistungsstufen, "comboBox_Leistungsstufen");
            this.comboBox_Leistungsstufen.FormattingEnabled = true;
            this.comboBox_Leistungsstufen.Name = "comboBox_Leistungsstufen";
            // 
            // label25
            // 
            resources.ApplyResources(this.label25, "label25");
            this.label25.Name = "label25";
            // 
            // comboBox_Waermepumpentyp
            // 
            resources.ApplyResources(this.comboBox_Waermepumpentyp, "comboBox_Waermepumpentyp");
            this.comboBox_Waermepumpentyp.FormattingEnabled = true;
            this.comboBox_Waermepumpentyp.Name = "comboBox_Waermepumpentyp";
            // 
            // label24
            // 
            resources.ApplyResources(this.label24, "label24");
            this.label24.Name = "label24";
            // 
            // chart1
            // 
            resources.ApplyResources(this.chart1, "chart1");
            chartArea1.AxisX.LineColor = System.Drawing.Color.DimGray;
            chartArea1.AxisX.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chartArea1.AxisX.MajorGrid.LineColor = System.Drawing.Color.Gray;
            chartArea1.AxisX.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chartArea1.AxisX.MinorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chartArea1.AxisY.LineColor = System.Drawing.Color.Gray;
            chartArea1.AxisY.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chartArea1.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chartArea1.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            this.chart1.Legends.Add(legend1);
            this.chart1.Name = "chart1";
            series1.ChartArea = "ChartArea1";
            series1.Legend = "Legend1";
            series1.MarkerBorderWidth = 3;
            series1.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Cross;
            series1.Name = "Series1";
            this.chart1.Series.Add(series1);
            // 
            // label33
            // 
            resources.ApplyResources(this.label33, "label33");
            this.label33.BackColor = System.Drawing.Color.Black;
            this.label33.ForeColor = System.Drawing.Color.White;
            this.label33.Name = "label33";
            // 
            // listBox_WP
            // 
            resources.ApplyResources(this.listBox_WP, "listBox_WP");
            this.listBox_WP.FormattingEnabled = true;
            this.listBox_WP.Name = "listBox_WP";
            this.listBox_WP.SelectedIndexChanged += new System.EventHandler(this.listBox_WP_SelectedIndexChanged);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // comboBox_Aufstellung
            // 
            resources.ApplyResources(this.comboBox_Aufstellung, "comboBox_Aufstellung");
            this.comboBox_Aufstellung.FormattingEnabled = true;
            this.comboBox_Aufstellung.Name = "comboBox_Aufstellung";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // textBox_Heizstab
            // 
            resources.ApplyResources(this.textBox_Heizstab, "textBox_Heizstab");
            this.textBox_Heizstab.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.textBox_Heizstab.Name = "textBox_Heizstab";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.BackColor = System.Drawing.Color.Black;
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Name = "label4";
            // 
            // btn_Neu
            // 
            resources.ApplyResources(this.btn_Neu, "btn_Neu");
            this.btn_Neu.Name = "btn_Neu";
            this.btn_Neu.UseVisualStyleBackColor = true;
            this.btn_Neu.Click += new System.EventHandler(this.btn_Neu_Click);
            // 
            // btn_Kenndaten
            // 
            resources.ApplyResources(this.btn_Kenndaten, "btn_Kenndaten");
            this.btn_Kenndaten.Name = "btn_Kenndaten";
            this.btn_Kenndaten.UseVisualStyleBackColor = true;
            this.btn_Kenndaten.Click += new System.EventHandler(this.btn_Kenndaten_Click);
            // 
            // btn_Speichern
            // 
            resources.ApplyResources(this.btn_Speichern, "btn_Speichern");
            this.btn_Speichern.Image = global::WindowsFormsApplication1.Properties.Resources.speichern;
            this.btn_Speichern.Name = "btn_Speichern";
            this.btn_Speichern.UseVisualStyleBackColor = true;
            this.btn_Speichern.Click += new System.EventHandler(this.btn_Speichern_Click);
            // 
            // btn_Loeschen
            // 
            resources.ApplyResources(this.btn_Loeschen, "btn_Loeschen");
            this.btn_Loeschen.Name = "btn_Loeschen";
            this.btn_Loeschen.UseVisualStyleBackColor = true;
            this.btn_Loeschen.Click += new System.EventHandler(this.btn_Loeschen_Click);
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.BackColor = System.Drawing.Color.Black;
            this.label5.ForeColor = System.Drawing.Color.White;
            this.label5.Name = "label5";
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // textBox_Name
            // 
            resources.ApplyResources(this.textBox_Name, "textBox_Name");
            this.textBox_Name.Name = "textBox_Name";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.label1.Name = "label1";
            // 
            // tabControl1
            // 
            resources.ApplyResources(this.tabControl1, "tabControl1");
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            // 
            // tabPage1
            // 
            resources.ApplyResources(this.tabPage1, "tabPage1");
            this.tabPage1.Controls.Add(this.chart1);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            resources.ApplyResources(this.tabPage2, "tabPage2");
            this.tabPage2.Controls.Add(this.chart2);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // chart2
            // 
            resources.ApplyResources(this.chart2, "chart2");
            chartArea2.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chartArea2.Name = "ChartArea1";
            this.chart2.ChartAreas.Add(chartArea2);
            legend2.Name = "Legend1";
            this.chart2.Legends.Add(legend2);
            this.chart2.Name = "chart2";
            series2.ChartArea = "ChartArea1";
            series2.Legend = "Legend1";
            series2.MarkerBorderWidth = 3;
            series2.Name = "Series1";
            this.chart2.Series.Add(series2);
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.BackColor = System.Drawing.Color.Black;
            this.label7.ForeColor = System.Drawing.Color.White;
            this.label7.Name = "label7";
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            // 
            // textBox_Kuehlung
            // 
            resources.ApplyResources(this.textBox_Kuehlung, "textBox_Kuehlung");
            this.textBox_Kuehlung.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.textBox_Kuehlung.Name = "textBox_Kuehlung";
            // 
            // radioButton_Waerme
            // 
            resources.ApplyResources(this.radioButton_Waerme, "radioButton_Waerme");
            this.radioButton_Waerme.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.radioButton_Waerme.Checked = true;
            this.radioButton_Waerme.Name = "radioButton_Waerme";
            this.radioButton_Waerme.TabStop = true;
            this.radioButton_Waerme.UseVisualStyleBackColor = false;
            this.radioButton_Waerme.CheckedChanged += new System.EventHandler(this.radioButton_Waerme_CheckedChanged);
            // 
            // radioButton_Kuehlung
            // 
            resources.ApplyResources(this.radioButton_Kuehlung, "radioButton_Kuehlung");
            this.radioButton_Kuehlung.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.radioButton_Kuehlung.Name = "radioButton_Kuehlung";
            this.radioButton_Kuehlung.UseVisualStyleBackColor = false;
            this.radioButton_Kuehlung.CheckedChanged += new System.EventHandler(this.radioButton_Kuehlung_CheckedChanged);
            // 
            // Form_WP
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ControlBox = false;
            this.Controls.Add(this.radioButton_Kuehlung);
            this.Controls.Add(this.radioButton_Waerme);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.textBox_Kuehlung);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.textBox_Name);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.btn_Loeschen);
            this.Controls.Add(this.btn_Speichern);
            this.Controls.Add(this.btn_Kenndaten);
            this.Controls.Add(this.btn_Neu);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBox_Heizstab);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboBox_Aufstellung);
            this.Controls.Add(this.listBox_WP);
            this.Controls.Add(this.label33);
            this.Controls.Add(this.label24);
            this.Controls.Add(this.label32);
            this.Controls.Add(this.textBox_Modulkosten);
            this.Controls.Add(this.label29);
            this.Controls.Add(this.comboBox_Baujahr);
            this.Controls.Add(this.label28);
            this.Controls.Add(this.textBox_Beschreibung);
            this.Controls.Add(this.label27);
            this.Controls.Add(this.textBox_Hersteller);
            this.Controls.Add(this.label26);
            this.Controls.Add(this.comboBox_Leistungsstufen);
            this.Controls.Add(this.label25);
            this.Controls.Add(this.comboBox_Waermepumpentyp);
            this.Controls.Add(this.label30);
            this.Controls.Add(this.textBox_Nennleistung);
            this.Controls.Add(this.label_WP);
            this.Controls.Add(this.btn_Beenden);
            this.Name = "Form_WP";
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.chart2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_Beenden;
        private System.Windows.Forms.Label label_WP;
        private System.Windows.Forms.Label label30;
        private System.Windows.Forms.TextBox textBox_Nennleistung;
        private System.Windows.Forms.Label label32;
        private System.Windows.Forms.TextBox textBox_Modulkosten;
        private System.Windows.Forms.Label label29;
        private System.Windows.Forms.ComboBox comboBox_Baujahr;
        private System.Windows.Forms.Label label28;
        private System.Windows.Forms.TextBox textBox_Beschreibung;
        private System.Windows.Forms.Label label27;
        private System.Windows.Forms.TextBox textBox_Hersteller;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.ComboBox comboBox_Leistungsstufen;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.ComboBox comboBox_Waermepumpentyp;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart1;
        private System.Windows.Forms.ListBox listBox_WP;
        private System.Windows.Forms.Label label33;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBox_Aufstellung;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox_Heizstab;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btn_Neu;
        private System.Windows.Forms.Button btn_Kenndaten;
        private System.Windows.Forms.Button btn_Speichern;
        private System.Windows.Forms.Button btn_Loeschen;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBox_Name;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart2;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox textBox_Kuehlung;
        private System.Windows.Forms.RadioButton radioButton_Waerme;
        private System.Windows.Forms.RadioButton radioButton_Kuehlung;
    }
}