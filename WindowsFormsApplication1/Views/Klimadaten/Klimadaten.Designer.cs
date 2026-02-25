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
            this.butt_Delete = new System.Windows.Forms.Button();
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.chart2 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart2)).BeginInit();
            this.SuspendLayout();
            // 
            // listBoxKlimreg
            // 
            this.listBoxKlimreg.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.listBoxKlimreg.FormattingEnabled = true;
            this.listBoxKlimreg.ItemHeight = 17;
            this.listBoxKlimreg.Location = new System.Drawing.Point(21, 235);
            this.listBoxKlimreg.Margin = new System.Windows.Forms.Padding(4);
            this.listBoxKlimreg.Name = "listBoxKlimreg";
            this.listBoxKlimreg.Size = new System.Drawing.Size(200, 225);
            this.listBoxKlimreg.TabIndex = 2;
            this.listBoxKlimreg.TabStop = false;
            this.listBoxKlimreg.SelectedIndexChanged += new System.EventHandler(this.listBoxWP_SelectedIndexChanged);
            // 
            // butt_Delete
            // 
            this.butt_Delete.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.butt_Delete.Location = new System.Drawing.Point(21, 464);
            this.butt_Delete.Margin = new System.Windows.Forms.Padding(4);
            this.butt_Delete.Name = "butt_Delete";
            this.butt_Delete.Size = new System.Drawing.Size(98, 30);
            this.butt_Delete.TabIndex = 5;
            this.butt_Delete.TabStop = false;
            this.butt_Delete.Text = "Löschen";
            this.butt_Delete.UseVisualStyleBackColor = true;
            this.butt_Delete.Click += new System.EventHandler(this.butt_Delete_Click);
            // 
            // btn_Beenden
            // 
            this.btn_Beenden.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.btn_Beenden.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Beenden.Location = new System.Drawing.Point(640, 536);
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
            this.label1.Size = new System.Drawing.Size(748, 28);
            this.label1.TabIndex = 13;
            this.label1.Text = "Importieren Sie hier die meteorologische Datensätze (TMY)  für die Klimaregion";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(17, 212);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(202, 19);
            this.label2.TabIndex = 15;
            this.label2.Text = "Liste der importierten Regionen";
            // 
            // pBar_Import
            // 
            this.pBar_Import.ForeColor = System.Drawing.Color.RoyalBlue;
            this.pBar_Import.Location = new System.Drawing.Point(474, 50);
            this.pBar_Import.Maximum = 9125;
            this.pBar_Import.Name = "pBar_Import";
            this.pBar_Import.Size = new System.Drawing.Size(236, 19);
            this.pBar_Import.Step = 1;
            this.pBar_Import.TabIndex = 17;
            this.pBar_Import.Visible = false;
            // 
            // btn_Import
            // 
            this.btn_Import.Location = new System.Drawing.Point(329, 45);
            this.btn_Import.Name = "btn_Import";
            this.btn_Import.Size = new System.Drawing.Size(139, 28);
            this.btn_Import.TabIndex = 18;
            this.btn_Import.TabStop = false;
            this.btn_Import.Text = "Daten Einlesen =>";
            this.btn_Import.UseVisualStyleBackColor = true;
            this.btn_Import.Click += new System.EventHandler(this.btn_Import_Click);
            // 
            // textBox_Display
            // 
            this.textBox_Display.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(122)))), ((int)(((byte)(255)))), ((int)(((byte)(222)))));
            this.textBox_Display.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Display.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Display.Location = new System.Drawing.Point(10, 103);
            this.textBox_Display.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_Display.Multiline = true;
            this.textBox_Display.Name = "textBox_Display";
            this.textBox_Display.ReadOnly = true;
            this.textBox_Display.Size = new System.Drawing.Size(316, 48);
            this.textBox_Display.TabIndex = 19;
            this.textBox_Display.TabStop = false;
            // 
            // comboBox_Ort
            // 
            this.comboBox_Ort.FormattingEnabled = true;
            this.comboBox_Ort.Location = new System.Drawing.Point(10, 46);
            this.comboBox_Ort.Name = "comboBox_Ort";
            this.comboBox_Ort.Size = new System.Drawing.Size(313, 25);
            this.comboBox_Ort.TabIndex = 1;
            this.comboBox_Ort.Click += new System.EventHandler(this.comboBox_Ort_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 24);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(284, 19);
            this.label4.TabIndex = 21;
            this.label4.Text = "Region auswählen oder eingeben (z.B. Berlin):";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 80);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(122, 19);
            this.label5.TabIndex = 23;
            this.label5.Text = "Details zur Region:";
            // 
            // textBox_Longitude
            // 
            this.textBox_Longitude.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(122)))), ((int)(((byte)(255)))), ((int)(((byte)(222)))));
            this.textBox_Longitude.Location = new System.Drawing.Point(407, 104);
            this.textBox_Longitude.Name = "textBox_Longitude";
            this.textBox_Longitude.ReadOnly = true;
            this.textBox_Longitude.Size = new System.Drawing.Size(109, 25);
            this.textBox_Longitude.TabIndex = 24;
            this.textBox_Longitude.TabStop = false;
            // 
            // textBox_Latitude
            // 
            this.textBox_Latitude.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(122)))), ((int)(((byte)(255)))), ((int)(((byte)(222)))));
            this.textBox_Latitude.Location = new System.Drawing.Point(600, 104);
            this.textBox_Latitude.Name = "textBox_Latitude";
            this.textBox_Latitude.ReadOnly = true;
            this.textBox_Latitude.Size = new System.Drawing.Size(109, 25);
            this.textBox_Latitude.TabIndex = 26;
            this.textBox_Latitude.TabStop = false;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(333, 105);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(74, 19);
            this.label6.TabIndex = 27;
            this.label6.Text = "Longitude:";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(536, 106);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(62, 19);
            this.label7.TabIndex = 28;
            this.label7.Text = "Latitude:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.pBar_Import);
            this.groupBox1.Controls.Add(this.btn_Import);
            this.groupBox1.Controls.Add(this.textBox_Latitude);
            this.groupBox1.Controls.Add(this.textBox_Display);
            this.groupBox1.Controls.Add(this.textBox_Longitude);
            this.groupBox1.Controls.Add(this.comboBox_Ort);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Location = new System.Drawing.Point(8, 37);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(720, 163);
            this.groupBox1.TabIndex = 29;
            this.groupBox1.TabStop = false;
            // 
            // chart1
            // 
            chartArea1.AlignmentOrientation = ((System.Windows.Forms.DataVisualization.Charting.AreaAlignmentOrientations)((System.Windows.Forms.DataVisualization.Charting.AreaAlignmentOrientations.Vertical | System.Windows.Forms.DataVisualization.Charting.AreaAlignmentOrientations.Horizontal)));
            chartArea1.AxisX.Title = "Tage";
            chartArea1.AxisY.Title = "Temperatur [°C]";
            chartArea1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(122)))), ((int)(((byte)(255)))), ((int)(((byte)(222)))));
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
            this.chart1.Size = new System.Drawing.Size(481, 271);
            this.chart1.TabIndex = 16;
            this.chart1.TabStop = false;
            this.chart1.Text = "chart1";
            title1.Name = "Jahrestemperatur Ganglinie";
            title1.Text = "Jahrestemperatur Ganglinie";
            this.chart1.Titles.Add(title1);
            this.chart1.AxisViewChanged += new System.EventHandler<System.Windows.Forms.DataVisualization.Charting.ViewEventArgs>(this.chart1_AxisViewChanged);
            this.chart1.AxisScrollBarClicked += new System.EventHandler<System.Windows.Forms.DataVisualization.Charting.ScrollBarEventArgs>(this.AxisScrollBarClicked);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(229, 216);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(509, 313);
            this.tabControl1.TabIndex = 30;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.chart1);
            this.tabPage1.Location = new System.Drawing.Point(4, 26);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(501, 283);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Temperatur";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.chart2);
            this.tabPage2.Location = new System.Drawing.Point(4, 26);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(501, 283);
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
            // Form_Klimadaten
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(748, 571);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btn_Beenden);
            this.Controls.Add(this.butt_Delete);
            this.Controls.Add(this.listBoxKlimreg);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_Klimadaten";
            this.Text = "Klimadaten";
            this.Load += new System.EventHandler(this.Form_Klimadaten_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.chart2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBoxKlimreg;
        private System.Windows.Forms.Button butt_Delete;
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
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart2;
    }
}