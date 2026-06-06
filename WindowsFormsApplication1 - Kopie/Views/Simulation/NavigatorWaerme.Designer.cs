namespace WindowsFormsApplication1
{
    partial class NavigatorWaerme
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series3 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series4 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series5 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Title title1 = new System.Windows.Forms.DataVisualization.Charting.Title();
            this.chart_Waerme = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.checkBox_ST = new System.Windows.Forms.CheckBox();
            this.checkBox_SPK = new System.Windows.Forms.CheckBox();
            this.checkBox_Heizstab = new System.Windows.Forms.CheckBox();
            this.checkBox_WP = new System.Windows.Forms.CheckBox();
            this.checkBox_Gesamt = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.chart_Waerme)).BeginInit();
            this.SuspendLayout();
            // 
            // chart_Waerme
            // 
            this.chart_Waerme.BorderlineColor = System.Drawing.Color.Black;
            this.chart_Waerme.BorderlineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.DashDotDot;
            chartArea1.AxisX.IsLabelAutoFit = false;
            chartArea1.AxisX.LabelStyle.Font = new System.Drawing.Font("Segoe UI", 10F);
            chartArea1.AxisX.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.DashDot;
            chartArea1.AxisX.Title = "Monate";
            chartArea1.AxisX.TitleFont = new System.Drawing.Font("Segoe UI", 10F);
            chartArea1.AxisY.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.DashDot;
            chartArea1.AxisY.Title = "Wärmebedarf in kW";
            chartArea1.AxisY.TitleFont = new System.Drawing.Font("Segoe UI", 10F);
            chartArea1.BackColor = System.Drawing.Color.LightGray;
            chartArea1.BackGradientStyle = System.Windows.Forms.DataVisualization.Charting.GradientStyle.DiagonalLeft;
            chartArea1.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chartArea1.Name = "ChartArea1";
            this.chart_Waerme.ChartAreas.Add(chartArea1);
            legend1.BackColor = System.Drawing.Color.Transparent;
            legend1.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Top;
            legend1.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            legend1.IsTextAutoFit = false;
            legend1.Name = "Legend1";
            legend1.TitleFont = new System.Drawing.Font("Segoe UI Semibold", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chart_Waerme.Legends.Add(legend1);
            this.chart_Waerme.Location = new System.Drawing.Point(50, 68);
            this.chart_Waerme.Name = "chart_Waerme";
            series1.ChartArea = "ChartArea1";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series1.Color = System.Drawing.Color.Red;
            series1.Legend = "Legend1";
            series1.LegendText = "Wärmepumpe";
            series1.Name = "Waermepumpe";
            series2.ChartArea = "ChartArea1";
            series2.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series2.Color = System.Drawing.Color.Yellow;
            series2.Legend = "Legend1";
            series2.LegendText = "Heizstab";
            series2.Name = "Heizstab";
            series3.ChartArea = "ChartArea1";
            series3.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series3.Color = System.Drawing.Color.Blue;
            series3.Legend = "Legend1";
            series3.LegendText = "Heizkessel";
            series3.Name = "Heizkessel";
            series4.ChartArea = "ChartArea1";
            series4.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series4.Legend = "Legend1";
            series4.LegendText = "Gesamt";
            series4.Name = "Gesamt";
            series5.ChartArea = "ChartArea1";
            series5.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series5.Legend = "Legend1";
            series5.Name = "Solarthermie";
            this.chart_Waerme.Series.Add(series1);
            this.chart_Waerme.Series.Add(series2);
            this.chart_Waerme.Series.Add(series3);
            this.chart_Waerme.Series.Add(series4);
            this.chart_Waerme.Series.Add(series5);
            this.chart_Waerme.Size = new System.Drawing.Size(890, 492);
            this.chart_Waerme.TabIndex = 281;
            this.chart_Waerme.Text = "chart7";
            title1.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            title1.Name = "Title1";
            title1.Text = "Wärmeproduktion Jahresganglinie ";
            this.chart_Waerme.Titles.Add(title1);
            // 
            // checkBox_ST
            // 
            this.checkBox_ST.AutoSize = true;
            this.checkBox_ST.BackColor = System.Drawing.Color.Transparent;
            this.checkBox_ST.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.checkBox_ST.ForeColor = System.Drawing.Color.Black;
            this.checkBox_ST.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.checkBox_ST.Location = new System.Drawing.Point(636, 576);
            this.checkBox_ST.Name = "checkBox_ST";
            this.checkBox_ST.Size = new System.Drawing.Size(101, 21);
            this.checkBox_ST.TabIndex = 291;
            this.checkBox_ST.Text = "Solarthermie";
            this.checkBox_ST.UseVisualStyleBackColor = false;
            this.checkBox_ST.CheckedChanged += new System.EventHandler(this.checkBox_ST_CheckedChanged);
            // 
            // checkBox_SPK
            // 
            this.checkBox_SPK.AutoSize = true;
            this.checkBox_SPK.BackColor = System.Drawing.Color.Transparent;
            this.checkBox_SPK.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.checkBox_SPK.ForeColor = System.Drawing.Color.Black;
            this.checkBox_SPK.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.checkBox_SPK.Location = new System.Drawing.Point(531, 576);
            this.checkBox_SPK.Name = "checkBox_SPK";
            this.checkBox_SPK.Size = new System.Drawing.Size(87, 21);
            this.checkBox_SPK.TabIndex = 290;
            this.checkBox_SPK.Text = "Heizkessel";
            this.checkBox_SPK.UseVisualStyleBackColor = false;
            this.checkBox_SPK.CheckedChanged += new System.EventHandler(this.checkBox_SPK_CheckedChanged);
            // 
            // checkBox_Heizstab
            // 
            this.checkBox_Heizstab.AutoSize = true;
            this.checkBox_Heizstab.BackColor = System.Drawing.Color.Transparent;
            this.checkBox_Heizstab.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.checkBox_Heizstab.ForeColor = System.Drawing.Color.Black;
            this.checkBox_Heizstab.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.checkBox_Heizstab.Location = new System.Drawing.Point(421, 576);
            this.checkBox_Heizstab.Name = "checkBox_Heizstab";
            this.checkBox_Heizstab.Size = new System.Drawing.Size(77, 21);
            this.checkBox_Heizstab.TabIndex = 289;
            this.checkBox_Heizstab.Text = "Heizstab";
            this.checkBox_Heizstab.UseVisualStyleBackColor = false;
            this.checkBox_Heizstab.CheckedChanged += new System.EventHandler(this.checkBox_Heizstab_CheckedChanged);
            // 
            // checkBox_WP
            // 
            this.checkBox_WP.AutoSize = true;
            this.checkBox_WP.BackColor = System.Drawing.Color.Transparent;
            this.checkBox_WP.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.checkBox_WP.ForeColor = System.Drawing.Color.Black;
            this.checkBox_WP.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.checkBox_WP.Location = new System.Drawing.Point(297, 576);
            this.checkBox_WP.Name = "checkBox_WP";
            this.checkBox_WP.Size = new System.Drawing.Size(109, 21);
            this.checkBox_WP.TabIndex = 288;
            this.checkBox_WP.Text = "Wärmepumpe";
            this.checkBox_WP.UseVisualStyleBackColor = false;
            this.checkBox_WP.CheckedChanged += new System.EventHandler(this.checkBox_WP_CheckedChanged);
            // 
            // checkBox_Gesamt
            // 
            this.checkBox_Gesamt.AutoSize = true;
            this.checkBox_Gesamt.BackColor = System.Drawing.Color.Transparent;
            this.checkBox_Gesamt.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.checkBox_Gesamt.ForeColor = System.Drawing.Color.Black;
            this.checkBox_Gesamt.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.checkBox_Gesamt.Location = new System.Drawing.Point(199, 576);
            this.checkBox_Gesamt.Name = "checkBox_Gesamt";
            this.checkBox_Gesamt.Size = new System.Drawing.Size(71, 21);
            this.checkBox_Gesamt.TabIndex = 287;
            this.checkBox_Gesamt.Text = "Gesamt";
            this.checkBox_Gesamt.UseVisualStyleBackColor = false;
            this.checkBox_Gesamt.CheckedChanged += new System.EventHandler(this.checkBox_Gesamt_CheckedChanged);
            // 
            // NavigatorWaerme
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.checkBox_ST);
            this.Controls.Add(this.checkBox_SPK);
            this.Controls.Add(this.checkBox_Heizstab);
            this.Controls.Add(this.checkBox_WP);
            this.Controls.Add(this.checkBox_Gesamt);
            this.Controls.Add(this.chart_Waerme);
            this.Name = "NavigatorWaerme";
            this.Size = new System.Drawing.Size(990, 628);
            ((System.ComponentModel.ISupportInitialize)(this.chart_Waerme)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataVisualization.Charting.Chart chart_Waerme;
        private System.Windows.Forms.CheckBox checkBox_ST;
        private System.Windows.Forms.CheckBox checkBox_SPK;
        private System.Windows.Forms.CheckBox checkBox_Heizstab;
        private System.Windows.Forms.CheckBox checkBox_WP;
        private System.Windows.Forms.CheckBox checkBox_Gesamt;
    }
}
