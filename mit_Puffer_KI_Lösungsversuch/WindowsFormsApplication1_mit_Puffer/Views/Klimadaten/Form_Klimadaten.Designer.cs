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
            listBoxKlimreg = new System.Windows.Forms.ListBox();
            btn_Delete = new System.Windows.Forms.Button();
            btn_Beenden = new System.Windows.Forms.Button();
            label3 = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            pBar_Import = new System.Windows.Forms.ProgressBar();
            btn_Import = new System.Windows.Forms.Button();
            textBox_Display = new System.Windows.Forms.TextBox();
            comboBox_Ort = new System.Windows.Forms.ComboBox();
            label4 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            textBox_Longitude = new System.Windows.Forms.TextBox();
            textBox_Latitude = new System.Windows.Forms.TextBox();
            label6 = new System.Windows.Forms.Label();
            label7 = new System.Windows.Forms.Label();
            label9 = new System.Windows.Forms.Label();
            label8 = new System.Windows.Forms.Label();
            textBox_Bezeichnung = new System.Windows.Forms.TextBox();
            panel1 = new System.Windows.Forms.Panel();
            panel_KlimaGraph = new System.Windows.Forms.Panel();
            tabControl1 = new System.Windows.Forms.TabControl();
            tabPage1 = new System.Windows.Forms.TabPage();
            chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            tabPage2 = new System.Windows.Forms.TabPage();
            chart2 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            panel2 = new System.Windows.Forms.Panel();
            label10 = new System.Windows.Forms.Label();
            panel1.SuspendLayout();
            panel_KlimaGraph.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)chart1).BeginInit();
            tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)chart2).BeginInit();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // listBoxKlimreg
            // 
            listBoxKlimreg.BorderStyle = System.Windows.Forms.BorderStyle.None;
            listBoxKlimreg.Dock = System.Windows.Forms.DockStyle.Fill;
            listBoxKlimreg.Font = new System.Drawing.Font("Segoe UI", 10F);
            listBoxKlimreg.FormattingEnabled = true;
            listBoxKlimreg.ItemHeight = 17;
            listBoxKlimreg.Location = new System.Drawing.Point(10, 30);
            listBoxKlimreg.Margin = new System.Windows.Forms.Padding(4, 8, 4, 4);
            listBoxKlimreg.Name = "listBoxKlimreg";
            listBoxKlimreg.Size = new System.Drawing.Size(201, 282);
            listBoxKlimreg.TabIndex = 2;
            listBoxKlimreg.TabStop = false;
            listBoxKlimreg.SelectedIndexChanged += listBoxWP_SelectedIndexChanged;
            // 
            // btn_Delete
            // 
            btn_Delete.Font = new System.Drawing.Font("Segoe UI", 10F);
            btn_Delete.Location = new System.Drawing.Point(18, 606);
            btn_Delete.Margin = new System.Windows.Forms.Padding(4);
            btn_Delete.Name = "btn_Delete";
            btn_Delete.Size = new System.Drawing.Size(98, 30);
            btn_Delete.TabIndex = 5;
            btn_Delete.TabStop = false;
            btn_Delete.Text = "Löschen";
            btn_Delete.UseVisualStyleBackColor = true;
            btn_Delete.Click += btn_Delete_Click;
            // 
            // btn_Beenden
            // 
            btn_Beenden.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            btn_Beenden.Font = new System.Drawing.Font("Segoe UI", 10F);
            btn_Beenden.Location = new System.Drawing.Point(647, 607);
            btn_Beenden.Margin = new System.Windows.Forms.Padding(4);
            btn_Beenden.Name = "btn_Beenden";
            btn_Beenden.Size = new System.Drawing.Size(98, 28);
            btn_Beenden.TabIndex = 10;
            btn_Beenden.TabStop = false;
            btn_Beenden.Text = "Beenden";
            btn_Beenden.UseVisualStyleBackColor = true;
            btn_Beenden.Click += btn_Beenden_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(504, 525);
            label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(0, 19);
            label3.TabIndex = 11;
            // 
            // label1
            // 
            label1.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            label1.Dock = System.Windows.Forms.DockStyle.Top;
            label1.Font = new System.Drawing.Font("Segoe UI", 12F);
            label1.Location = new System.Drawing.Point(0, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(757, 28);
            label1.TabIndex = 13;
            label1.Text = "Importieren Sie hier die meteorologische Datensätze (TMY)  für die Klimaregion";
            label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            label2.Location = new System.Drawing.Point(7, 8);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(204, 21);
            label2.TabIndex = 15;
            label2.Text = "Liste der importierten Regionen";
            // 
            // pBar_Import
            // 
            pBar_Import.ForeColor = System.Drawing.Color.RoyalBlue;
            pBar_Import.Location = new System.Drawing.Point(18, 254);
            pBar_Import.Maximum = 9125;
            pBar_Import.Name = "pBar_Import";
            pBar_Import.Size = new System.Drawing.Size(203, 10);
            pBar_Import.Step = 1;
            pBar_Import.TabIndex = 17;
            pBar_Import.Visible = false;
            // 
            // btn_Import
            // 
            btn_Import.BackColor = System.Drawing.Color.MediumBlue;
            btn_Import.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            btn_Import.ForeColor = System.Drawing.Color.White;
            btn_Import.Location = new System.Drawing.Point(18, 213);
            btn_Import.Name = "btn_Import";
            btn_Import.Size = new System.Drawing.Size(203, 35);
            btn_Import.TabIndex = 18;
            btn_Import.TabStop = false;
            btn_Import.Text = "Daten Einlesen ▶";
            btn_Import.UseVisualStyleBackColor = false;
            btn_Import.Click += btn_Import_Click;
            // 
            // textBox_Display
            // 
            textBox_Display.BackColor = System.Drawing.Color.FromArgb(245, 247, 249);
            textBox_Display.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            textBox_Display.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            textBox_Display.Location = new System.Drawing.Point(375, 213);
            textBox_Display.Margin = new System.Windows.Forms.Padding(4);
            textBox_Display.Multiline = true;
            textBox_Display.Name = "textBox_Display";
            textBox_Display.ReadOnly = true;
            textBox_Display.Size = new System.Drawing.Size(370, 48);
            textBox_Display.TabIndex = 19;
            textBox_Display.TabStop = false;
            // 
            // comboBox_Ort
            // 
            comboBox_Ort.FormattingEnabled = true;
            comboBox_Ort.Location = new System.Drawing.Point(16, 53);
            comboBox_Ort.Name = "comboBox_Ort";
            comboBox_Ort.Size = new System.Drawing.Size(280, 25);
            comboBox_Ort.TabIndex = 1;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(12, 31);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(284, 19);
            label4.TabIndex = 21;
            label4.Text = "Region auswählen oder eingeben (z.B. Berlin):";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(246, 213);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(122, 19);
            label5.TabIndex = 23;
            label5.Text = "Details zur Region:";
            // 
            // textBox_Longitude
            // 
            textBox_Longitude.BackColor = System.Drawing.SystemColors.Window;
            textBox_Longitude.Location = new System.Drawing.Point(93, 118);
            textBox_Longitude.Name = "textBox_Longitude";
            textBox_Longitude.Size = new System.Drawing.Size(88, 25);
            textBox_Longitude.TabIndex = 24;
            textBox_Longitude.TabStop = false;
            textBox_Longitude.TextChanged += textBox_Longitude_TextChanged;
            // 
            // textBox_Latitude
            // 
            textBox_Latitude.BackColor = System.Drawing.SystemColors.Window;
            textBox_Latitude.Location = new System.Drawing.Point(249, 118);
            textBox_Latitude.Name = "textBox_Latitude";
            textBox_Latitude.Size = new System.Drawing.Size(88, 25);
            textBox_Latitude.TabIndex = 26;
            textBox_Latitude.TabStop = false;
            textBox_Latitude.TextChanged += textBox_Latitude_TextChanged;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(19, 119);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(74, 19);
            label6.TabIndex = 27;
            label6.Text = "Longitude:";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(185, 120);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(62, 19);
            label7.TabIndex = 28;
            label7.Text = "Latitude:";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            label9.Location = new System.Drawing.Point(23, 88);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(36, 17);
            label9.TabIndex = 31;
            label9.Text = "oder";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new System.Drawing.Point(350, 121);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(89, 19);
            label8.TabIndex = 30;
            label8.Text = "Bezeichnung:";
            // 
            // textBox_Bezeichnung
            // 
            textBox_Bezeichnung.BackColor = System.Drawing.SystemColors.Window;
            textBox_Bezeichnung.Location = new System.Drawing.Point(445, 120);
            textBox_Bezeichnung.Name = "textBox_Bezeichnung";
            textBox_Bezeichnung.Size = new System.Drawing.Size(188, 25);
            textBox_Bezeichnung.TabIndex = 29;
            textBox_Bezeichnung.TabStop = false;
            // 
            // panel1
            // 
            panel1.BackColor = System.Drawing.Color.White;
            panel1.Controls.Add(label2);
            panel1.Controls.Add(listBoxKlimreg);
            panel1.Location = new System.Drawing.Point(18, 278);
            panel1.Name = "panel1";
            panel1.Padding = new System.Windows.Forms.Padding(10, 30, 10, 10);
            panel1.Size = new System.Drawing.Size(221, 322);
            panel1.TabIndex = 31;
            // 
            // panel_KlimaGraph
            // 
            panel_KlimaGraph.BackColor = System.Drawing.Color.White;
            panel_KlimaGraph.Controls.Add(tabControl1);
            panel_KlimaGraph.Location = new System.Drawing.Point(245, 278);
            panel_KlimaGraph.Name = "panel_KlimaGraph";
            panel_KlimaGraph.Size = new System.Drawing.Size(500, 322);
            panel_KlimaGraph.TabIndex = 32;
            panel_KlimaGraph.Paint += panel_KlimaGraph_Paint;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Location = new System.Drawing.Point(3, 8);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new System.Drawing.Size(493, 308);
            tabControl1.TabIndex = 31;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(chart1);
            tabPage1.Location = new System.Drawing.Point(4, 26);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new System.Windows.Forms.Padding(3);
            tabPage1.Size = new System.Drawing.Size(485, 278);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Temperatur";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // chart1
            // 
            chartArea1.AlignmentOrientation = System.Windows.Forms.DataVisualization.Charting.AreaAlignmentOrientations.Vertical | System.Windows.Forms.DataVisualization.Charting.AreaAlignmentOrientations.Horizontal;
            chartArea1.AxisX.Title = "Tage";
            chartArea1.AxisY.Title = "Temperatur [°C]";
            chartArea1.BackColor = System.Drawing.Color.FromArgb(224, 224, 224);
            chartArea1.BackGradientStyle = System.Windows.Forms.DataVisualization.Charting.GradientStyle.DiagonalLeft;
            chartArea1.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Solid;
            chartArea1.Name = "ChartArea1";
            chart1.ChartAreas.Add(chartArea1);
            chart1.Location = new System.Drawing.Point(6, 6);
            chart1.Name = "chart1";
            series1.BackGradientStyle = System.Windows.Forms.DataVisualization.Charting.GradientStyle.DiagonalLeft;
            series1.ChartArea = "ChartArea1";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series1.IsVisibleInLegend = false;
            series1.LabelBorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            series1.Name = "Series1";
            series1.SmartLabelStyle.Enabled = false;
            chart1.Series.Add(series1);
            chart1.Size = new System.Drawing.Size(471, 261);
            chart1.TabIndex = 16;
            chart1.TabStop = false;
            chart1.Text = "chart1";
            title1.Name = "Jahrestemperatur Ganglinie";
            title1.Text = "Jahrestemperatur Ganglinie";
            chart1.Titles.Add(title1);
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(chart2);
            tabPage2.Location = new System.Drawing.Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new System.Windows.Forms.Padding(3);
            tabPage2.Size = new System.Drawing.Size(485, 280);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Sonnenwinkel";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // chart2
            // 
            chartArea2.AlignmentOrientation = System.Windows.Forms.DataVisualization.Charting.AreaAlignmentOrientations.Vertical | System.Windows.Forms.DataVisualization.Charting.AreaAlignmentOrientations.Horizontal;
            chartArea2.AxisX.Title = "Monat";
            chartArea2.AxisY.Title = "Sonnenwinkel [°]";
            chartArea2.BackColor = System.Drawing.Color.FromArgb(122, 255, 222);
            chartArea2.BackGradientStyle = System.Windows.Forms.DataVisualization.Charting.GradientStyle.DiagonalLeft;
            chartArea2.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Solid;
            chartArea2.Name = "ChartArea1";
            chart2.ChartAreas.Add(chartArea2);
            chart2.Location = new System.Drawing.Point(10, 6);
            chart2.Name = "chart2";
            series2.BackGradientStyle = System.Windows.Forms.DataVisualization.Charting.GradientStyle.DiagonalLeft;
            series2.ChartArea = "ChartArea1";
            series2.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series2.IsVisibleInLegend = false;
            series2.LabelBorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            series2.Name = "Series1";
            series2.SmartLabelStyle.Enabled = false;
            chart2.Series.Add(series2);
            chart2.Size = new System.Drawing.Size(481, 271);
            chart2.TabIndex = 17;
            chart2.TabStop = false;
            chart2.Text = "chart2";
            title2.Name = "Jahresverlauf Sonnenwinkel";
            title2.Text = "Jahresverlauf Sonnenwinkel";
            chart2.Titles.Add(title2);
            // 
            // panel2
            // 
            panel2.BackColor = System.Drawing.Color.White;
            panel2.Controls.Add(label10);
            panel2.Controls.Add(label8);
            panel2.Controls.Add(label9);
            panel2.Controls.Add(textBox_Bezeichnung);
            panel2.Controls.Add(label4);
            panel2.Controls.Add(comboBox_Ort);
            panel2.Controls.Add(label6);
            panel2.Controls.Add(textBox_Longitude);
            panel2.Controls.Add(label7);
            panel2.Controls.Add(textBox_Latitude);
            panel2.Location = new System.Drawing.Point(18, 42);
            panel2.Name = "panel2";
            panel2.Size = new System.Drawing.Size(727, 155);
            panel2.TabIndex = 33;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Dock = System.Windows.Forms.DockStyle.Top;
            label10.Location = new System.Drawing.Point(0, 0);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(338, 19);
            label10.TabIndex = 32;
            label10.Text = "Ort auswählen oder Longitude und Latitude eingeben";
            // 
            // Form_Klimadaten
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(757, 641);
            Controls.Add(panel2);
            Controls.Add(panel_KlimaGraph);
            Controls.Add(btn_Delete);
            Controls.Add(panel1);
            Controls.Add(label1);
            Controls.Add(label3);
            Controls.Add(pBar_Import);
            Controls.Add(textBox_Display);
            Controls.Add(btn_Import);
            Controls.Add(btn_Beenden);
            Controls.Add(label5);
            Font = new System.Drawing.Font("Segoe UI", 10F);
            Margin = new System.Windows.Forms.Padding(4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form_Klimadaten";
            Text = "Klimadaten";
            Load += Form_Klimadaten_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel_KlimaGraph.ResumeLayout(false);
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)chart1).EndInit();
            tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)chart2).EndInit();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

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