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
            this.btn_Beenden.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Beenden.Location = new System.Drawing.Point(748, 600);
            this.btn_Beenden.Name = "btn_Beenden";
            this.btn_Beenden.Size = new System.Drawing.Size(95, 30);
            this.btn_Beenden.TabIndex = 48;
            this.btn_Beenden.Text = "OK";
            this.btn_Beenden.UseVisualStyleBackColor = true;
            this.btn_Beenden.Click += new System.EventHandler(this.butt_Beenden_Click);
            // 
            // label_WP
            // 
            this.label_WP.AutoSize = true;
            this.label_WP.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label_WP.Location = new System.Drawing.Point(14, 39);
            this.label_WP.Name = "label_WP";
            this.label_WP.Size = new System.Drawing.Size(161, 19);
            this.label_WP.TabIndex = 59;
            this.label_WP.Text = "Wärmepumpen Auswahl:";
            // 
            // label30
            // 
            this.label30.AutoSize = true;
            this.label30.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label30.Location = new System.Drawing.Point(633, 89);
            this.label30.Name = "label30";
            this.label30.Size = new System.Drawing.Size(90, 19);
            this.label30.TabIndex = 96;
            this.label30.Text = "Nennleistung";
            // 
            // textBox_Nennleistung
            // 
            this.textBox_Nennleistung.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.textBox_Nennleistung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Nennleistung.Location = new System.Drawing.Point(725, 89);
            this.textBox_Nennleistung.Name = "textBox_Nennleistung";
            this.textBox_Nennleistung.Size = new System.Drawing.Size(89, 25);
            this.textBox_Nennleistung.TabIndex = 95;
            // 
            // label32
            // 
            this.label32.AutoSize = true;
            this.label32.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label32.Location = new System.Drawing.Point(631, 216);
            this.label32.Name = "label32";
            this.label32.Size = new System.Drawing.Size(90, 19);
            this.label32.TabIndex = 124;
            this.label32.Text = "Modulkosten";
            // 
            // textBox_Modulkosten
            // 
            this.textBox_Modulkosten.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Modulkosten.Location = new System.Drawing.Point(725, 216);
            this.textBox_Modulkosten.Name = "textBox_Modulkosten";
            this.textBox_Modulkosten.Size = new System.Drawing.Size(87, 25);
            this.textBox_Modulkosten.TabIndex = 123;
            // 
            // label29
            // 
            this.label29.AutoSize = true;
            this.label29.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label29.Location = new System.Drawing.Point(335, 216);
            this.label29.Name = "label29";
            this.label29.Size = new System.Drawing.Size(55, 19);
            this.label29.TabIndex = 122;
            this.label29.Text = "Baujahr";
            // 
            // comboBox_Baujahr
            // 
            this.comboBox_Baujahr.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.comboBox_Baujahr.FormattingEnabled = true;
            this.comboBox_Baujahr.Location = new System.Drawing.Point(470, 216);
            this.comboBox_Baujahr.Name = "comboBox_Baujahr";
            this.comboBox_Baujahr.Size = new System.Drawing.Size(89, 25);
            this.comboBox_Baujahr.TabIndex = 121;
            // 
            // label28
            // 
            this.label28.AutoSize = true;
            this.label28.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label28.Location = new System.Drawing.Point(336, 255);
            this.label28.Name = "label28";
            this.label28.Size = new System.Drawing.Size(91, 19);
            this.label28.TabIndex = 120;
            this.label28.Text = "Beschreibung";
            // 
            // textBox_Beschreibung
            // 
            this.textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Beschreibung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Beschreibung.Location = new System.Drawing.Point(470, 253);
            this.textBox_Beschreibung.Multiline = true;
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            this.textBox_Beschreibung.Size = new System.Drawing.Size(355, 43);
            this.textBox_Beschreibung.TabIndex = 119;
            // 
            // label27
            // 
            this.label27.AutoSize = true;
            this.label27.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label27.Location = new System.Drawing.Point(335, 120);
            this.label27.Name = "label27";
            this.label27.Size = new System.Drawing.Size(67, 19);
            this.label27.TabIndex = 118;
            this.label27.Text = "Hersteller";
            // 
            // textBox_Hersteller
            // 
            this.textBox_Hersteller.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Hersteller.Location = new System.Drawing.Point(470, 120);
            this.textBox_Hersteller.Name = "textBox_Hersteller";
            this.textBox_Hersteller.Size = new System.Drawing.Size(157, 25);
            this.textBox_Hersteller.TabIndex = 117;
            // 
            // label26
            // 
            this.label26.AutoSize = true;
            this.label26.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label26.Location = new System.Drawing.Point(335, 185);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(105, 19);
            this.label26.TabIndex = 116;
            this.label26.Text = "Leistungsstufen";
            // 
            // comboBox_Leistungsstufen
            // 
            this.comboBox_Leistungsstufen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.comboBox_Leistungsstufen.FormattingEnabled = true;
            this.comboBox_Leistungsstufen.Location = new System.Drawing.Point(470, 185);
            this.comboBox_Leistungsstufen.Name = "comboBox_Leistungsstufen";
            this.comboBox_Leistungsstufen.Size = new System.Drawing.Size(157, 25);
            this.comboBox_Leistungsstufen.TabIndex = 115;
            // 
            // label25
            // 
            this.label25.AutoSize = true;
            this.label25.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label25.Location = new System.Drawing.Point(335, 89);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(123, 19);
            this.label25.TabIndex = 114;
            this.label25.Text = "Wärmepumpentyp";
            // 
            // comboBox_Waermepumpentyp
            // 
            this.comboBox_Waermepumpentyp.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.comboBox_Waermepumpentyp.FormattingEnabled = true;
            this.comboBox_Waermepumpentyp.Location = new System.Drawing.Point(470, 89);
            this.comboBox_Waermepumpentyp.Name = "comboBox_Waermepumpentyp";
            this.comboBox_Waermepumpentyp.Size = new System.Drawing.Size(157, 25);
            this.comboBox_Waermepumpentyp.TabIndex = 113;
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label24.Location = new System.Drawing.Point(14, 295);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(145, 19);
            this.label24.TabIndex = 126;
            this.label24.Text = "Kenndaten Kennlinien:";
            // 
            // chart1
            // 
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
            this.chart1.Location = new System.Drawing.Point(7, 3);
            this.chart1.Name = "chart1";
            series1.ChartArea = "ChartArea1";
            series1.Legend = "Legend1";
            series1.MarkerBorderWidth = 3;
            series1.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Cross;
            series1.Name = "Series1";
            this.chart1.Series.Add(series1);
            this.chart1.Size = new System.Drawing.Size(484, 195);
            this.chart1.TabIndex = 125;
            this.chart1.Text = "COP";
            // 
            // label33
            // 
            this.label33.BackColor = System.Drawing.Color.Black;
            this.label33.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label33.ForeColor = System.Drawing.Color.White;
            this.label33.Location = new System.Drawing.Point(818, 216);
            this.label33.Name = "label33";
            this.label33.Size = new System.Drawing.Size(23, 19);
            this.label33.TabIndex = 42;
            this.label33.Text = "€";
            this.label33.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // listBox_WP
            // 
            this.listBox_WP.FormattingEnabled = true;
            this.listBox_WP.HorizontalScrollbar = true;
            this.listBox_WP.Location = new System.Drawing.Point(18, 61);
            this.listBox_WP.Name = "listBox_WP";
            this.listBox_WP.Size = new System.Drawing.Size(309, 212);
            this.listBox_WP.TabIndex = 128;
            this.listBox_WP.SelectedIndexChanged += new System.EventHandler(this.listBox_WP_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label2.Location = new System.Drawing.Point(333, 151);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(78, 19);
            this.label2.TabIndex = 132;
            this.label2.Text = "Aufstellung";
            // 
            // comboBox_Aufstellung
            // 
            this.comboBox_Aufstellung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.comboBox_Aufstellung.FormattingEnabled = true;
            this.comboBox_Aufstellung.Location = new System.Drawing.Point(470, 151);
            this.comboBox_Aufstellung.Name = "comboBox_Aufstellung";
            this.comboBox_Aufstellung.Size = new System.Drawing.Size(157, 25);
            this.comboBox_Aufstellung.TabIndex = 131;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label3.Location = new System.Drawing.Point(633, 120);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(61, 19);
            this.label3.TabIndex = 134;
            this.label3.Text = "Heizstab";
            // 
            // textBox_Heizstab
            // 
            this.textBox_Heizstab.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.textBox_Heizstab.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Heizstab.Location = new System.Drawing.Point(727, 120);
            this.textBox_Heizstab.Name = "textBox_Heizstab";
            this.textBox_Heizstab.Size = new System.Drawing.Size(87, 25);
            this.textBox_Heizstab.TabIndex = 133;
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.Color.Black;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Location = new System.Drawing.Point(820, 120);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(32, 19);
            this.label4.TabIndex = 135;
            this.label4.Text = "kW";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btn_Neu
            // 
            this.btn_Neu.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Neu.Location = new System.Drawing.Point(533, 600);
            this.btn_Neu.Name = "btn_Neu";
            this.btn_Neu.Size = new System.Drawing.Size(95, 30);
            this.btn_Neu.TabIndex = 136;
            this.btn_Neu.Text = "Neu";
            this.btn_Neu.UseVisualStyleBackColor = true;
            this.btn_Neu.Click += new System.EventHandler(this.btn_Neu_Click);
            // 
            // btn_Kenndaten
            // 
            this.btn_Kenndaten.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Kenndaten.Location = new System.Drawing.Point(525, 339);
            this.btn_Kenndaten.Name = "btn_Kenndaten";
            this.btn_Kenndaten.Size = new System.Drawing.Size(222, 52);
            this.btn_Kenndaten.TabIndex = 137;
            this.btn_Kenndaten.Text = "Kennliniendaten Ansicht/Bearbeiten...";
            this.btn_Kenndaten.UseVisualStyleBackColor = true;
            this.btn_Kenndaten.Click += new System.EventHandler(this.btn_Kenndaten_Click);
            // 
            // btn_Speichern
            // 
            this.btn_Speichern.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Speichern.Image = global::WindowsFormsApplication1.Properties.Resources.speichern;
            this.btn_Speichern.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btn_Speichern.Location = new System.Drawing.Point(410, 600);
            this.btn_Speichern.Name = "btn_Speichern";
            this.btn_Speichern.Size = new System.Drawing.Size(117, 30);
            this.btn_Speichern.TabIndex = 138;
            this.btn_Speichern.Text = "Speichern";
            this.btn_Speichern.UseVisualStyleBackColor = true;
            this.btn_Speichern.Click += new System.EventHandler(this.btn_Speichern_Click);
            // 
            // btn_Loeschen
            // 
            this.btn_Loeschen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Loeschen.Location = new System.Drawing.Point(634, 600);
            this.btn_Loeschen.Name = "btn_Loeschen";
            this.btn_Loeschen.Size = new System.Drawing.Size(95, 30);
            this.btn_Loeschen.TabIndex = 139;
            this.btn_Loeschen.Text = "Löschen";
            this.btn_Loeschen.UseVisualStyleBackColor = true;
            this.btn_Loeschen.Click += new System.EventHandler(this.btn_Loeschen_Click);
            // 
            // label5
            // 
            this.label5.BackColor = System.Drawing.Color.Black;
            this.label5.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label5.ForeColor = System.Drawing.Color.White;
            this.label5.Location = new System.Drawing.Point(820, 89);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(32, 19);
            this.label5.TabIndex = 140;
            this.label5.Text = "kW";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label6.Location = new System.Drawing.Point(336, 58);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(45, 19);
            this.label6.TabIndex = 142;
            this.label6.Text = "Name";
            // 
            // textBox_Name
            // 
            this.textBox_Name.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Name.Location = new System.Drawing.Point(387, 58);
            this.textBox_Name.Name = "textBox_Name";
            this.textBox_Name.Size = new System.Drawing.Size(427, 25);
            this.textBox_Name.TabIndex = 141;
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(877, 28);
            this.label1.TabIndex = 143;
            this.label1.Text = "Verwaltung Daten zu Wärmepumpen und deren Kennlinien";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(14, 317);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(505, 277);
            this.tabControl1.TabIndex = 144;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.chart1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(497, 251);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "COP";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.chart2);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(497, 251);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Leistung";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // chart2
            // 
            chartArea2.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chartArea2.Name = "ChartArea1";
            this.chart2.ChartAreas.Add(chartArea2);
            legend2.Name = "Legend1";
            this.chart2.Legends.Add(legend2);
            this.chart2.Location = new System.Drawing.Point(12, 6);
            this.chart2.Name = "chart2";
            series2.ChartArea = "ChartArea1";
            series2.Legend = "Legend1";
            series2.MarkerBorderWidth = 3;
            series2.Name = "Series1";
            this.chart2.Series.Add(series2);
            this.chart2.Size = new System.Drawing.Size(479, 195);
            this.chart2.TabIndex = 126;
            // 
            // label7
            // 
            this.label7.BackColor = System.Drawing.Color.Black;
            this.label7.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label7.ForeColor = System.Drawing.Color.White;
            this.label7.Location = new System.Drawing.Point(820, 151);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(32, 19);
            this.label7.TabIndex = 147;
            this.label7.Text = "kW";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label8.Location = new System.Drawing.Point(633, 151);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(84, 19);
            this.label8.TabIndex = 146;
            this.label8.Text = "Kühlleistung";
            // 
            // textBox_Kuehlung
            // 
            this.textBox_Kuehlung.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.textBox_Kuehlung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kuehlung.Location = new System.Drawing.Point(727, 151);
            this.textBox_Kuehlung.Name = "textBox_Kuehlung";
            this.textBox_Kuehlung.Size = new System.Drawing.Size(87, 25);
            this.textBox_Kuehlung.TabIndex = 145;
            // 
            // radioButton_Waerme
            // 
            this.radioButton_Waerme.AutoSize = true;
            this.radioButton_Waerme.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.radioButton_Waerme.Checked = true;
            this.radioButton_Waerme.Location = new System.Drawing.Point(176, 297);
            this.radioButton_Waerme.Name = "radioButton_Waerme";
            this.radioButton_Waerme.Size = new System.Drawing.Size(59, 17);
            this.radioButton_Waerme.TabIndex = 149;
            this.radioButton_Waerme.TabStop = true;
            this.radioButton_Waerme.Text = "Wärme";
            this.radioButton_Waerme.UseVisualStyleBackColor = false;
            this.radioButton_Waerme.CheckedChanged += new System.EventHandler(this.radioButton_Waerme_CheckedChanged);
            // 
            // radioButton_Kuehlung
            // 
            this.radioButton_Kuehlung.AutoSize = true;
            this.radioButton_Kuehlung.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.radioButton_Kuehlung.Location = new System.Drawing.Point(241, 297);
            this.radioButton_Kuehlung.Name = "radioButton_Kuehlung";
            this.radioButton_Kuehlung.Size = new System.Drawing.Size(64, 17);
            this.radioButton_Kuehlung.TabIndex = 150;
            this.radioButton_Kuehlung.Text = "Kühlung";
            this.radioButton_Kuehlung.UseVisualStyleBackColor = false;
            this.radioButton_Kuehlung.CheckedChanged += new System.EventHandler(this.radioButton_Kuehlung_CheckedChanged);
            // 
            // Form_WP
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(877, 642);
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
            this.Text = "Datenbank Wärmepumpen";
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