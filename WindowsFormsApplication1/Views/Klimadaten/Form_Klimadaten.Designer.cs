namespace WindowsFormsApplication1
{
    partial class Form_Klimadaten
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
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Title title1 = new System.Windows.Forms.DataVisualization.Charting.Title();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Title title2 = new System.Windows.Forms.DataVisualization.Charting.Title();
            this.listBoxKlimreg = new System.Windows.Forms.ListBox();
            this.btn_Delete = new System.Windows.Forms.Button();
            this.btn_Beenden = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.pBar_Import = new System.Windows.Forms.ProgressBar();
            this.btn_Import = new System.Windows.Forms.Button();
            this.textBox_Display = new System.Windows.Forms.TextBox();
            this.comboBox_Ort = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox_Longitude = new System.Windows.Forms.TextBox();
            this.textBox_Latitude = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.textBox_Bezeichnung = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel_KlimaGraph = new System.Windows.Forms.Panel();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.chart2 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label10 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.panel_KlimaGraph.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart2)).BeginInit();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // listBoxKlimreg
            // 
            this.listBoxKlimreg.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.listBoxKlimreg.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBoxKlimreg.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.listBoxKlimreg.FormattingEnabled = true;
            this.listBoxKlimreg.ItemHeight = 17;
            this.listBoxKlimreg.Location = new System.Drawing.Point(10, 30);
            this.listBoxKlimreg.Margin = new System.Windows.Forms.Padding(4, 8, 4, 4);
            this.listBoxKlimreg.Name = "listBoxKlimreg";
            this.listBoxKlimreg.Size = new System.Drawing.Size(201, 282);
            this.listBoxKlimreg.TabIndex = 2;
            this.listBoxKlimreg.TabStop = false;
            this.listBoxKlimreg.SelectedIndexChanged += new System.EventHandler(this.listBoxWP_SelectedIndexChanged);
            // 
            // btn_Delete
            // 
            this.btn_Delete.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Delete.Location = new System.Drawing.Point(18, 606);
            this.btn_Delete.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Delete.Name = "btn_Delete";
            this.btn_Delete.Size = new System.Drawing.Size(98, 30);
            this.btn_Delete.TabIndex = 5;
            this.btn_Delete.TabStop = false;
            this.btn_Delete.Text = "Löschen";
            this.btn_Delete.UseVisualStyleBackColor = true;
            this.btn_Delete.Click += new System.EventHandler(this.btn_Delete_Click);
            // 
            // btn_Beenden
            // 
            this.btn_Beenden.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.btn_Beenden.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Beenden.Location = new System.Drawing.Point(647, 607);
            this.btn_Beenden.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Beenden.Name = "btn_Beenden";
            this.btn_Beenden.Size = new System.Drawing.Size(98, 28);
            this.btn_Beenden.TabIndex = 10;
            this.btn_Beenden.TabStop = false;
            this.btn_Beenden.Text = "Beenden";
            this.btn_Beenden.UseVisualStyleBackColor = true;
            this.btn_Beenden.Click += new System.EventHandler(this.btn_Beenden_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(504, 525);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(0, 19);
            this.label3.TabIndex = 11;
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(757, 28);
            this.label1.TabIndex = 13;
            this.label1.Text = "Importieren Sie hier die meteorologische Datensätze (TMY)  für die Klimaregion";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label2.Location = new System.Drawing.Point(7, 8);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(204, 21);
            this.label2.TabIndex = 15;
            this.label2.Text = "Liste der importierten Regionen";
            // 
            // pBar_Import
            // 
            this.pBar_Import.ForeColor = System.Drawing.Color.RoyalBlue;
            this.pBar_Import.Location = new System.Drawing.Point(18, 254);
            this.pBar_Import.Maximum = 9125;
            this.pBar_Import.Name = "pBar_Import";
            this.pBar_Import.Size = new System.Drawing.Size(203, 10);
            this.pBar_Import.Step = 1;
            this.pBar_Import.TabIndex = 17;
            this.pBar_Import.Visible = false;
            // 
            // btn_Import
            // 
            this.btn_Import.BackColor = System.Drawing.Color.MediumBlue;
            this.btn_Import.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Import.ForeColor = System.Drawing.Color.White;
            this.btn_Import.Location = new System.Drawing.Point(18, 213);
            this.btn_Import.Name = "btn_Import";
            this.btn_Import.Size = new System.Drawing.Size(203, 35);
            this.btn_Import.TabIndex = 18;
            this.btn_Import.TabStop = false;
            this.btn_Import.Text = "Daten Einlesen ▶";
            this.btn_Import.UseVisualStyleBackColor = false;
            this.btn_Import.Click += new System.EventHandler(this.btn_Import_Click);
            // 
            // textBox_Display
            // 
            this.textBox_Display.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(247)))), ((int)(((byte)(249)))));
            this.textBox_Display.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Display.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Display.Location = new System.Drawing.Point(375, 213);
            this.textBox_Display.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_Display.Multiline = true;
            this.textBox_Display.Name = "textBox_Display";
            this.textBox_Display.ReadOnly = true;
            this.textBox_Display.Size = new System.Drawing.Size(370, 48);
            this.textBox_Display.TabIndex = 19;
            this.textBox_Display.TabStop = false;
            // 
            // comboBox_Ort
            // 
            this.comboBox_Ort.FormattingEnabled = true;
            this.comboBox_Ort.Location = new System.Drawing.Point(16, 53);
            this.comboBox_Ort.Name = "comboBox_Ort";
            this.comboBox_Ort.Size = new System.Drawing.Size(280, 25);
            this.comboBox_Ort.TabIndex = 1;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 31);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(284, 19);
            this.label4.TabIndex = 21;
            this.label4.Text = "Region auswählen oder eingeben (z.B. Berlin):";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(246, 213);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(122, 19);
            this.label5.TabIndex = 23;
            this.label5.Text = "Details zur Region:";
            // 
            // textBox_Longitude
            // 
            this.textBox_Longitude.BackColor = System.Drawing.SystemColors.Window;
            this.textBox_Longitude.Location = new System.Drawing.Point(93, 118);
            this.textBox_Longitude.Name = "textBox_Longitude";
            this.textBox_Longitude.Size = new System.Drawing.Size(88, 25);
            this.textBox_Longitude.TabIndex = 24;
            this.textBox_Longitude.TabStop = false;
            this.textBox_Longitude.TextChanged += new System.EventHandler(this.textBox_Longitude_TextChanged);
            // 
            // textBox_Latitude
            // 
            this.textBox_Latitude.BackColor = System.Drawing.SystemColors.Window;
            this.textBox_Latitude.Location = new System.Drawing.Point(249, 118);
            this.textBox_Latitude.Name = "textBox_Latitude";
            this.textBox_Latitude.Size = new System.Drawing.Size(88, 25);
            this.textBox_Latitude.TabIndex = 26;
            this.textBox_Latitude.TabStop = false;
            this.textBox_Latitude.TextChanged += new System.EventHandler(this.textBox_Latitude_TextChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(19, 119);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(74, 19);
            this.label6.TabIndex = 27;
            this.label6.Text = "Longitude:";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(185, 120);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(62, 19);
            this.label7.TabIndex = 28;
            this.label7.Text = "Latitude:";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(23, 88);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(36, 17);
            this.label9.TabIndex = 31;
            this.label9.Text = "oder";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(350, 121);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(89, 19);
            this.label8.TabIndex = 30;
            this.label8.Text = "Bezeichnung:";
            // 
            // textBox_Bezeichnung
            // 
            this.textBox_Bezeichnung.BackColor = System.Drawing.SystemColors.Window;
            this.textBox_Bezeichnung.Location = new System.Drawing.Point(445, 120);
            this.textBox_Bezeichnung.Name = "textBox_Bezeichnung";
            this.textBox_Bezeichnung.Size = new System.Drawing.Size(188, 25);
            this.textBox_Bezeichnung.TabIndex = 29;
            this.textBox_Bezeichnung.TabStop = false;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.White;
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.listBoxKlimreg);
            this.panel1.Location = new System.Drawing.Point(18, 278);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(10, 30, 10, 10);
            this.panel1.Size = new System.Drawing.Size(221, 322);
            this.panel1.TabIndex = 31;
            // 
            // panel_KlimaGraph
            // 
            this.panel_KlimaGraph.BackColor = System.Drawing.Color.White;
            this.panel_KlimaGraph.Controls.Add(this.tabControl1);
            this.panel_KlimaGraph.Location = new System.Drawing.Point(245, 278);
            this.panel_KlimaGraph.Name = "panel_KlimaGraph";
            this.panel_KlimaGraph.Size = new System.Drawing.Size(500, 322);
            this.panel_KlimaGraph.TabIndex = 32;
            this.panel_KlimaGraph.Paint += new System.Windows.Forms.PaintEventHandler(this.panel_KlimaGraph_Paint);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(3, 8);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(493, 308);
            this.tabControl1.TabIndex = 31;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.chart1);
            this.tabPage1.Location = new System.Drawing.Point(4, 26);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(485, 278);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Temperatur";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // chart1
            // 
            chartArea1.AlignmentOrientation = ((System.Windows.Forms.DataVisualization.Charting.AreaAlignmentOrientations)((System.Windows.Forms.DataVisualization.Charting.AreaAlignmentOrientations.Vertical | System.Windows.Forms.DataVisualization.Charting.AreaAlignmentOrientations.Horizontal)));
            chartArea1.AxisX.Title = "Tage";
            chartArea1.AxisY.Title = "Temperatur [°C]";
            chartArea1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            chartArea1.BackGradientStyle = System.Windows.Forms.DataVisualization.Charting.GradientStyle.DiagonalLeft;
            chartArea1.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Solid;
            chartArea1.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea1);
            this.chart1.Location = new System.Drawing.Point(6, 6);
            this.chart1.Name = "chart1";
            series1.BackGradientStyle = System.Windows.Forms.DataVisualization.Charting.GradientStyle.DiagonalLeft;
            series1.ChartArea = "ChartArea1";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series1.IsVisibleInLegend = false;
            series1.LabelBorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            series1.Name = "Series1";
            series1.SmartLabelStyle.Enabled = false;
            this.chart1.Series.Add(series1);
            this.chart1.Size = new System.Drawing.Size(471, 261);
            this.chart1.TabIndex = 16;
            this.chart1.TabStop = false;
            this.chart1.Text = "chart1";
            title1.Name = "Jahrestemperatur Ganglinie";
            title1.Text = "Jahrestemperatur Ganglinie";
            this.chart1.Titles.Add(title1);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.chart2);
            this.tabPage2.Location = new System.Drawing.Point(4, 26);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(485, 278);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Sonnenwinkel";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // chart2
            // 
            chartArea2.AlignmentOrientation = ((System.Windows.Forms.DataVisualization.Charting.AreaAlignmentOrientations)((System.Windows.Forms.DataVisualization.Charting.AreaAlignmentOrientations.Vertical | System.Windows.Forms.DataVisualization.Charting.AreaAlignmentOrientations.Horizontal)));
            chartArea2.AxisX.Title = "Monat";
            chartArea2.AxisY.Title = "Sonnenwinkel [°]";
            chartArea2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(122)))), ((int)(((byte)(255)))), ((int)(((byte)(222)))));
            chartArea2.BackGradientStyle = System.Windows.Forms.DataVisualization.Charting.GradientStyle.DiagonalLeft;
            chartArea2.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Solid;
            chartArea2.Name = "ChartArea1";
            this.chart2.ChartAreas.Add(chartArea2);
            this.chart2.Location = new System.Drawing.Point(10, 6);
            this.chart2.Name = "chart2";
            series2.BackGradientStyle = System.Windows.Forms.DataVisualization.Charting.GradientStyle.DiagonalLeft;
            series2.ChartArea = "ChartArea1";
            series2.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series2.IsVisibleInLegend = false;
            series2.LabelBorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            series2.Name = "Series1";
            series2.SmartLabelStyle.Enabled = false;
            this.chart2.Series.Add(series2);
            this.chart2.Size = new System.Drawing.Size(481, 271);
            this.chart2.TabIndex = 17;
            this.chart2.TabStop = false;
            this.chart2.Text = "chart2";
            title2.Name = "Jahresverlauf Sonnenwinkel";
            title2.Text = "Jahresverlauf Sonnenwinkel";
            this.chart2.Titles.Add(title2);
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.White;
            this.panel2.Controls.Add(this.label10);
            this.panel2.Controls.Add(this.label8);
            this.panel2.Controls.Add(this.label9);
            this.panel2.Controls.Add(this.textBox_Bezeichnung);
            this.panel2.Controls.Add(this.label4);
            this.panel2.Controls.Add(this.comboBox_Ort);
            this.panel2.Controls.Add(this.label6);
            this.panel2.Controls.Add(this.textBox_Longitude);
            this.panel2.Controls.Add(this.label7);
            this.panel2.Controls.Add(this.textBox_Latitude);
            this.panel2.Location = new System.Drawing.Point(18, 42);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(727, 155);
            this.panel2.TabIndex = 33;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Dock = System.Windows.Forms.DockStyle.Top;
            this.label10.Location = new System.Drawing.Point(0, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(338, 19);
            this.label10.TabIndex = 32;
            this.label10.Text = "Ort auswählen oder Longitude und Latitude eingeben";
            // 
            // Form_Klimadaten
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(757, 641);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel_KlimaGraph);
            this.Controls.Add(this.btn_Delete);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.pBar_Import);
            this.Controls.Add(this.textBox_Display);
            this.Controls.Add(this.btn_Import);
            this.Controls.Add(this.btn_Beenden);
            this.Controls.Add(this.label5);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_Klimadaten";
            this.Text = "Klimadaten";
            this.Load += new System.EventHandler(this.Form_Klimadaten_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel_KlimaGraph.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.chart2)).EndInit();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBoxKlimreg;
        private System.Windows.Forms.Button btn_Delete;
        private System.Windows.Forms.Button btn_Beenden;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ProgressBar pBar_Import;
        private System.Windows.Forms.Button btn_Import;
        private System.Windows.Forms.TextBox textBox_Display;
        private System.Windows.Forms.ComboBox comboBox_Ort;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox_Longitude;
        private System.Windows.Forms.TextBox textBox_Latitude;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox textBox_Bezeichnung;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel_KlimaGraph;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart2;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label label10;
    }
}