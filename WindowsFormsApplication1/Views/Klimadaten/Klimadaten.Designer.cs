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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea6 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series6 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Title title6 = new System.Windows.Forms.DataVisualization.Charting.Title();
            this.listBoxKlimreg = new System.Windows.Forms.ListBox();
            this.butt_Delete = new System.Windows.Forms.Button();
            this.btn_ExcelDatei = new System.Windows.Forms.Button();
            this.btn_Beenden = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox_Excel = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btn_Import = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.SuspendLayout();
            // 
            // listBoxKlimreg
            // 
            this.listBoxKlimreg.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.listBoxKlimreg.FormattingEnabled = true;
            this.listBoxKlimreg.ItemHeight = 17;
            this.listBoxKlimreg.Location = new System.Drawing.Point(35, 160);
            this.listBoxKlimreg.Margin = new System.Windows.Forms.Padding(4);
            this.listBoxKlimreg.Name = "listBoxKlimreg";
            this.listBoxKlimreg.Size = new System.Drawing.Size(200, 225);
            this.listBoxKlimreg.TabIndex = 2;
            this.listBoxKlimreg.SelectedIndexChanged += new System.EventHandler(this.listBoxWP_SelectedIndexChanged);
            // 
            // butt_Delete
            // 
            this.butt_Delete.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.butt_Delete.Location = new System.Drawing.Point(35, 393);
            this.butt_Delete.Margin = new System.Windows.Forms.Padding(4);
            this.butt_Delete.Name = "butt_Delete";
            this.butt_Delete.Size = new System.Drawing.Size(98, 30);
            this.butt_Delete.TabIndex = 5;
            this.butt_Delete.Text = "Löschen";
            this.butt_Delete.UseVisualStyleBackColor = true;
            this.butt_Delete.Click += new System.EventHandler(this.butt_Delete_Click);
            // 
            // btn_ExcelDatei
            // 
            this.btn_ExcelDatei.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_ExcelDatei.Location = new System.Drawing.Point(35, 47);
            this.btn_ExcelDatei.Margin = new System.Windows.Forms.Padding(4);
            this.btn_ExcelDatei.Name = "btn_ExcelDatei";
            this.btn_ExcelDatei.Size = new System.Drawing.Size(129, 29);
            this.btn_ExcelDatei.TabIndex = 7;
            this.btn_ExcelDatei.Text = "Excel File...";
            this.btn_ExcelDatei.UseVisualStyleBackColor = true;
            this.btn_ExcelDatei.Click += new System.EventHandler(this.btn_ExcelDatei_Click);
            // 
            // btn_Beenden
            // 
            this.btn_Beenden.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.btn_Beenden.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Beenden.Location = new System.Drawing.Point(627, 455);
            this.btn_Beenden.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Beenden.Name = "btn_Beenden";
            this.btn_Beenden.Size = new System.Drawing.Size(98, 28);
            this.btn_Beenden.TabIndex = 10;
            this.btn_Beenden.Text = "Beenden";
            this.btn_Beenden.UseVisualStyleBackColor = true;
            this.btn_Beenden.Click += new System.EventHandler(this.btn_Beenden_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(501, 492);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(0, 19);
            this.label3.TabIndex = 11;
            // 
            // textBox_Excel
            // 
            this.textBox_Excel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Excel.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Excel.Location = new System.Drawing.Point(172, 50);
            this.textBox_Excel.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_Excel.Multiline = true;
            this.textBox_Excel.Name = "textBox_Excel";
            this.textBox_Excel.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
            this.textBox_Excel.Size = new System.Drawing.Size(541, 41);
            this.textBox_Excel.TabIndex = 12;
            this.textBox_Excel.WordWrap = false;
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(738, 28);
            this.label1.TabIndex = 13;
            this.label1.Text = "Importieren Sie hier die Jahrestemperatur Ganglinie für die Klimaregion";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btn_Import
            // 
            this.btn_Import.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Import.Location = new System.Drawing.Point(35, 84);
            this.btn_Import.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Import.Name = "btn_Import";
            this.btn_Import.Size = new System.Drawing.Size(129, 30);
            this.btn_Import.TabIndex = 14;
            this.btn_Import.Text = "Daten Einlesen";
            this.btn_Import.UseVisualStyleBackColor = true;
            this.btn_Import.Click += new System.EventHandler(this.btn_Import_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(31, 137);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(202, 19);
            this.label2.TabIndex = 15;
            this.label2.Text = "Liste der importierten Regionen";
            // 
            // chart1
            // 
            chartArea6.AlignmentOrientation = ((System.Windows.Forms.DataVisualization.Charting.AreaAlignmentOrientations)((System.Windows.Forms.DataVisualization.Charting.AreaAlignmentOrientations.Vertical | System.Windows.Forms.DataVisualization.Charting.AreaAlignmentOrientations.Horizontal)));
            chartArea6.AxisX.Title = "Tage";
            chartArea6.AxisY.Title = "Temperatur [°C]";
            chartArea6.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Solid;
            chartArea6.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea6);
            this.chart1.Location = new System.Drawing.Point(242, 160);
            this.chart1.Name = "chart1";
            series6.BackGradientStyle = System.Windows.Forms.DataVisualization.Charting.GradientStyle.DiagonalLeft;
            series6.ChartArea = "ChartArea1";
            series6.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series6.IsVisibleInLegend = false;
            series6.LabelBorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            series6.Name = "Series1";
            series6.SmartLabelStyle.Enabled = false;
            this.chart1.Series.Add(series6);
            this.chart1.Size = new System.Drawing.Size(481, 282);
            this.chart1.TabIndex = 16;
            this.chart1.Text = "chart1";
            title6.Name = "Jahrestemperatur Ganglinie";
            title6.Text = "Jahrestemperatur Ganglinie";
            this.chart1.Titles.Add(title6);
            this.chart1.SelectionRangeChanging += new System.EventHandler<System.Windows.Forms.DataVisualization.Charting.CursorEventArgs>(this.test);
            this.chart1.SelectionRangeChanged += new System.EventHandler<System.Windows.Forms.DataVisualization.Charting.CursorEventArgs>(this.test2);
            this.chart1.AxisScrollBarClicked += new System.EventHandler<System.Windows.Forms.DataVisualization.Charting.ScrollBarEventArgs>(this.test3);
            // 
            // Form_Klimadaten
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(738, 496);
            this.Controls.Add(this.chart1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btn_Import);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox_Excel);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btn_Beenden);
            this.Controls.Add(this.btn_ExcelDatei);
            this.Controls.Add(this.butt_Delete);
            this.Controls.Add(this.listBoxKlimreg);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_Klimadaten";
            this.Text = "Klimadaten";
            this.Load += new System.EventHandler(this.Form_Klimadaten_Load);
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBoxKlimreg;
        private System.Windows.Forms.Button butt_Delete;
        private System.Windows.Forms.Button btn_ExcelDatei;
        private System.Windows.Forms.Button btn_Beenden;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox_Excel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btn_Import;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart1;
    }
}