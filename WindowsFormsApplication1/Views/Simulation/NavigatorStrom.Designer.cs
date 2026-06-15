namespace WindowsFormsApplication1
{
    partial class NavigatorStrom 
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
            checkBox_PV = new System.Windows.Forms.CheckBox();
            checkBox_Profil_Lastgang = new System.Windows.Forms.CheckBox();
            checkBox_SPK = new System.Windows.Forms.CheckBox();
            checkBox_Heizstab = new System.Windows.Forms.CheckBox();
            checkBox_WP = new System.Windows.Forms.CheckBox();
            checkBox_Gesamt = new System.Windows.Forms.CheckBox();
            chart7 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            checkBox_BHKW = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)chart7).BeginInit();
            SuspendLayout();
            // 
            // checkBox_PV
            // 
            checkBox_PV.AutoSize = true;
            checkBox_PV.BackColor = System.Drawing.Color.Transparent;
            checkBox_PV.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            checkBox_PV.ForeColor = System.Drawing.Color.Black;
            checkBox_PV.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            checkBox_PV.Location = new System.Drawing.Point(556, 520);
            checkBox_PV.Name = "checkBox_PV";
            checkBox_PV.Size = new System.Drawing.Size(98, 21);
            checkBox_PV.TabIndex = 286;
            checkBox_PV.Text = "Photovoltaik";
            checkBox_PV.UseVisualStyleBackColor = false;
            checkBox_PV.CheckedChanged += checkBox_PV_CheckedChanged;
            // 
            // checkBox_Profil_Lastgang
            // 
            checkBox_Profil_Lastgang.AutoSize = true;
            checkBox_Profil_Lastgang.BackColor = System.Drawing.Color.Transparent;
            checkBox_Profil_Lastgang.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            checkBox_Profil_Lastgang.ForeColor = System.Drawing.Color.Black;
            checkBox_Profil_Lastgang.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            checkBox_Profil_Lastgang.Location = new System.Drawing.Point(678, 520);
            checkBox_Profil_Lastgang.Name = "checkBox_Profil_Lastgang";
            checkBox_Profil_Lastgang.Size = new System.Drawing.Size(115, 21);
            checkBox_Profil_Lastgang.TabIndex = 285;
            checkBox_Profil_Lastgang.Text = "Profil/Lastgang";
            checkBox_Profil_Lastgang.UseVisualStyleBackColor = false;
            checkBox_Profil_Lastgang.CheckedChanged += checkBox_Profil_Lastgang_CheckedChanged;
            // 
            // checkBox_SPK
            // 
            checkBox_SPK.AutoSize = true;
            checkBox_SPK.BackColor = System.Drawing.Color.Transparent;
            checkBox_SPK.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            checkBox_SPK.ForeColor = System.Drawing.Color.Black;
            checkBox_SPK.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            checkBox_SPK.Location = new System.Drawing.Point(432, 520);
            checkBox_SPK.Name = "checkBox_SPK";
            checkBox_SPK.Size = new System.Drawing.Size(87, 21);
            checkBox_SPK.TabIndex = 284;
            checkBox_SPK.Text = "Heizkessel";
            checkBox_SPK.UseVisualStyleBackColor = false;
            checkBox_SPK.CheckedChanged += checkBox_SPK_CheckedChanged;
            // 
            // checkBox_Heizstab
            // 
            checkBox_Heizstab.AutoSize = true;
            checkBox_Heizstab.BackColor = System.Drawing.Color.Transparent;
            checkBox_Heizstab.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            checkBox_Heizstab.ForeColor = System.Drawing.Color.Black;
            checkBox_Heizstab.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            checkBox_Heizstab.Location = new System.Drawing.Point(322, 520);
            checkBox_Heizstab.Name = "checkBox_Heizstab";
            checkBox_Heizstab.Size = new System.Drawing.Size(77, 21);
            checkBox_Heizstab.TabIndex = 283;
            checkBox_Heizstab.Text = "Heizstab";
            checkBox_Heizstab.UseVisualStyleBackColor = false;
            checkBox_Heizstab.CheckedChanged += checkBox_Heizstab_CheckedChanged;
            // 
            // checkBox_WP
            // 
            checkBox_WP.AutoSize = true;
            checkBox_WP.BackColor = System.Drawing.Color.Transparent;
            checkBox_WP.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            checkBox_WP.ForeColor = System.Drawing.Color.Black;
            checkBox_WP.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            checkBox_WP.Location = new System.Drawing.Point(198, 520);
            checkBox_WP.Name = "checkBox_WP";
            checkBox_WP.Size = new System.Drawing.Size(109, 21);
            checkBox_WP.TabIndex = 282;
            checkBox_WP.Text = "Wärmepumpe";
            checkBox_WP.UseVisualStyleBackColor = false;
            checkBox_WP.CheckedChanged += checkBox_WP_CheckedChanged;
            // 
            // checkBox_Gesamt
            // 
            checkBox_Gesamt.AutoSize = true;
            checkBox_Gesamt.BackColor = System.Drawing.Color.Transparent;
            checkBox_Gesamt.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            checkBox_Gesamt.ForeColor = System.Drawing.Color.Black;
            checkBox_Gesamt.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            checkBox_Gesamt.Location = new System.Drawing.Point(100, 520);
            checkBox_Gesamt.Name = "checkBox_Gesamt";
            checkBox_Gesamt.Size = new System.Drawing.Size(71, 21);
            checkBox_Gesamt.TabIndex = 281;
            checkBox_Gesamt.Text = "Gesamt";
            checkBox_Gesamt.UseVisualStyleBackColor = false;
            checkBox_Gesamt.CheckedChanged += checkBox_Gesamt_CheckedChanged;
            // 
            // chart7
            // 
            chart7.BorderlineColor = System.Drawing.Color.Black;
            chart7.BorderlineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.DashDotDot;
            chartArea1.AxisX.IsLabelAutoFit = false;
            chartArea1.AxisX.LabelStyle.Font = new System.Drawing.Font("Segoe UI", 10F);
            chartArea1.AxisX.Title = "Monate";
            chartArea1.AxisX.TitleFont = new System.Drawing.Font("Segoe UI", 10F);
            chartArea1.AxisY.Title = "Strombedarf in kW";
            chartArea1.AxisY.TitleFont = new System.Drawing.Font("Segoe UI", 10F);
            chartArea1.BackColor = System.Drawing.Color.LightGray;
            chartArea1.BackGradientStyle = System.Windows.Forms.DataVisualization.Charting.GradientStyle.DiagonalLeft;
            chartArea1.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chartArea1.Name = "ChartArea1";
            chart7.ChartAreas.Add(chartArea1);
            legend1.BackColor = System.Drawing.Color.Transparent;
            legend1.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Top;
            legend1.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            legend1.IsTextAutoFit = false;
            legend1.Name = "Legend1";
            legend1.TitleFont = new System.Drawing.Font("Segoe UI Semibold", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            chart7.Legends.Add(legend1);
            chart7.Location = new System.Drawing.Point(25, 11);
            chart7.Name = "chart7";
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
            series5.Name = "Profil/Lastgang";
            chart7.Series.Add(series1);
            chart7.Series.Add(series2);
            chart7.Series.Add(series3);
            chart7.Series.Add(series4);
            chart7.Series.Add(series5);
            chart7.Size = new System.Drawing.Size(890, 492);
            chart7.TabIndex = 280;
            chart7.Text = "chart7";
            title1.Name = "Title1";
            title1.Text = "Stromverlauf Jahresganglinie ";
            chart7.Titles.Add(title1);
            // 
            // checkBox_BHKW
            // 
            checkBox_BHKW.AutoSize = true;
            checkBox_BHKW.BackColor = System.Drawing.Color.Transparent;
            checkBox_BHKW.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            checkBox_BHKW.ForeColor = System.Drawing.Color.Black;
            checkBox_BHKW.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            checkBox_BHKW.Location = new System.Drawing.Point(799, 520);
            checkBox_BHKW.Name = "checkBox_BHKW";
            checkBox_BHKW.Size = new System.Drawing.Size(63, 21);
            checkBox_BHKW.TabIndex = 287;
            checkBox_BHKW.Text = "BHKW";
            checkBox_BHKW.UseVisualStyleBackColor = false;
            checkBox_BHKW.CheckedChanged += checkBox_BHKW_CheckedChanged;
            // 
            // NavigatorStrom
            // 
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            Controls.Add(checkBox_BHKW);
            Controls.Add(checkBox_PV);
            Controls.Add(checkBox_Profil_Lastgang);
            Controls.Add(checkBox_SPK);
            Controls.Add(checkBox_Heizstab);
            Controls.Add(checkBox_WP);
            Controls.Add(checkBox_Gesamt);
            Controls.Add(chart7);
            Name = "NavigatorStrom";
            Size = new System.Drawing.Size(990, 628);
            ((System.ComponentModel.ISupportInitialize)chart7).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox checkBox_PV;
        private System.Windows.Forms.CheckBox checkBox_Profil_Lastgang;
        private System.Windows.Forms.CheckBox checkBox_SPK;
        private System.Windows.Forms.CheckBox checkBox_Heizstab;
        private System.Windows.Forms.CheckBox checkBox_WP;
        private System.Windows.Forms.CheckBox checkBox_Gesamt;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart7;
        private System.Windows.Forms.CheckBox checkBox_BHKW;
    }
}
