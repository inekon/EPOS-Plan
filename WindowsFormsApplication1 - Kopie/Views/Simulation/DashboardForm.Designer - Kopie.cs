namespace WindowsFormsApplication1
{
    partial class DashboardForm
    {
        private System.ComponentModel.IContainer components = null;

        // Controls Definition
        private System.Windows.Forms.GroupBox groupPV;
        private System.Windows.Forms.ProgressBar pbPV;
        private System.Windows.Forms.Label lblPVAutarkie;
        private System.Windows.Forms.GroupBox groupST;
        private System.Windows.Forms.ProgressBar pbST;
        private System.Windows.Forms.Label lblSTDeckung;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartSolar;
        private System.Windows.Forms.Label lblCO2;
        private System.Windows.Forms.NumericUpDown numSpeicherKWh;
        private System.Windows.Forms.Label lblSpeicherInfo;
        private System.Windows.Forms.Label lblNutzungsgradST;
   

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series3 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series4 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.lblNutzungsgradST = new System.Windows.Forms.Label();
            this.lblCO2 = new System.Windows.Forms.Label();
            this.numSpeicherKWh = new System.Windows.Forms.NumericUpDown();
            this.lblSpeicherInfo = new System.Windows.Forms.Label();
            this.groupPV = new System.Windows.Forms.GroupBox();
            this.pbPV = new System.Windows.Forms.ProgressBar();
            this.lblPVAutarkie = new System.Windows.Forms.Label();
            this.groupST = new System.Windows.Forms.GroupBox();
            this.pbST = new System.Windows.Forms.ProgressBar();
            this.lblSTDeckung = new System.Windows.Forms.Label();
            this.chartSolar = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.lblTest = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numSpeicherKWh)).BeginInit();
            this.groupPV.SuspendLayout();
            this.groupST.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chartSolar)).BeginInit();
            this.SuspendLayout();
            // 
            // lblNutzungsgradST
            // 
            this.lblNutzungsgradST.Location = new System.Drawing.Point(234, 100);
            this.lblNutzungsgradST.Name = "lblNutzungsgradST";
            this.lblNutzungsgradST.Size = new System.Drawing.Size(159, 20);
            this.lblNutzungsgradST.TabIndex = 0;
            this.lblNutzungsgradST.Text = "Therm. Nutzungsgrad: 0 %";
            // 
            // lblCO2
            // 
            this.lblCO2.AutoSize = true;
            this.lblCO2.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold);
            this.lblCO2.Location = new System.Drawing.Point(12, 98);
            this.lblCO2.Name = "lblCO2";
            this.lblCO2.Size = new System.Drawing.Size(127, 16);
            this.lblCO2.TabIndex = 0;
            this.lblCO2.Text = "0 kg CO2 gespart";
            // 
            // numSpeicherKWh
            // 
            this.numSpeicherKWh.Location = new System.Drawing.Point(450, 35);
            this.numSpeicherKWh.Name = "numSpeicherKWh";
            this.numSpeicherKWh.Size = new System.Drawing.Size(120, 20);
            this.numSpeicherKWh.TabIndex = 0;
            this.numSpeicherKWh.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numSpeicherKWh.ValueChanged += new System.EventHandler(this.numSpeicherKWh_ValueChanged);
            // 
            // lblSpeicherInfo
            // 
            this.lblSpeicherInfo.Location = new System.Drawing.Point(447, 12);
            this.lblSpeicherInfo.Name = "lblSpeicherInfo";
            this.lblSpeicherInfo.Size = new System.Drawing.Size(165, 23);
            this.lblSpeicherInfo.TabIndex = 1;
            this.lblSpeicherInfo.Text = "Theoretischer Speicher (PV) (kWh):";
            // 
            // groupPV
            // 
            this.groupPV.Controls.Add(this.pbPV);
            this.groupPV.Controls.Add(this.lblPVAutarkie);
            this.groupPV.Location = new System.Drawing.Point(12, 12);
            this.groupPV.Name = "groupPV";
            this.groupPV.Size = new System.Drawing.Size(200, 80);
            this.groupPV.TabIndex = 0;
            this.groupPV.TabStop = false;
            this.groupPV.Text = "Photovoltaik Autarkie";
            // 
            // pbPV
            // 
            this.pbPV.Location = new System.Drawing.Point(10, 45);
            this.pbPV.Name = "pbPV";
            this.pbPV.Size = new System.Drawing.Size(180, 23);
            this.pbPV.TabIndex = 0;
            // 
            // lblPVAutarkie
            // 
            this.lblPVAutarkie.Location = new System.Drawing.Point(10, 25);
            this.lblPVAutarkie.Name = "lblPVAutarkie";
            this.lblPVAutarkie.Size = new System.Drawing.Size(100, 23);
            this.lblPVAutarkie.TabIndex = 1;
            // 
            // groupST
            // 
            this.groupST.Controls.Add(this.pbST);
            this.groupST.Controls.Add(this.lblSTDeckung);
            this.groupST.Location = new System.Drawing.Point(227, 12);
            this.groupST.Name = "groupST";
            this.groupST.Size = new System.Drawing.Size(200, 80);
            this.groupST.TabIndex = 1;
            this.groupST.TabStop = false;
            this.groupST.Text = "Solarthermie Deckung";
            // 
            // pbST
            // 
            this.pbST.Location = new System.Drawing.Point(10, 45);
            this.pbST.Name = "pbST";
            this.pbST.Size = new System.Drawing.Size(180, 23);
            this.pbST.TabIndex = 0;
            // 
            // lblSTDeckung
            // 
            this.lblSTDeckung.Location = new System.Drawing.Point(10, 25);
            this.lblSTDeckung.Name = "lblSTDeckung";
            this.lblSTDeckung.Size = new System.Drawing.Size(100, 23);
            this.lblSTDeckung.TabIndex = 1;
            // 
            // chartSolar
            // 
            chartArea1.AxisX.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.DashDot;
            chartArea1.AxisX.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.DashDot;
            chartArea1.AxisX.MinorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.DashDot;
            chartArea1.AxisY.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.DashDot;
            chartArea1.AxisY.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.DashDot;
            chartArea1.AxisY.MinorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.DashDot;
            chartArea1.AxisY.TitleAlignment = System.Drawing.StringAlignment.Far;
            chartArea1.AxisY.TitleFont = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            chartArea1.Name = "MainArea";
            this.chartSolar.ChartAreas.Add(chartArea1);
            this.chartSolar.Location = new System.Drawing.Point(12, 138);
            this.chartSolar.Name = "chartSolar";
            series1.ChartArea = "MainArea";
            series1.Color = System.Drawing.Color.OrangeRed;
            series1.Name = "Bedarf";
            series2.ChartArea = "MainArea";
            series2.Color = System.Drawing.Color.DodgerBlue;
            series2.Name = "Produktion";
            series3.ChartArea = "MainArea";
            series3.Color = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            series3.Name = "Restbedarf Netz";
            series4.ChartArea = "MainArea";
            series4.Color = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(64)))), ((int)(((byte)(0)))));
            series4.Name = "Überschuss";
            this.chartSolar.Series.Add(series1);
            this.chartSolar.Series.Add(series2);
            this.chartSolar.Series.Add(series3);
            this.chartSolar.Series.Add(series4);
            this.chartSolar.Size = new System.Drawing.Size(806, 345);
            this.chartSolar.TabIndex = 2;
            // 
            // lblTest
            // 
            this.lblTest.AutoSize = true;
            this.lblTest.Location = new System.Drawing.Point(447, 58);
            this.lblTest.Name = "lblTest";
            this.lblTest.Size = new System.Drawing.Size(35, 13);
            this.lblTest.TabIndex = 3;
            this.lblTest.Text = "label1";
            // 
            // DashboardForm
            // 
            this.ClientSize = new System.Drawing.Size(839, 505);
            this.Controls.Add(this.lblTest);
            this.Controls.Add(this.numSpeicherKWh);
            this.Controls.Add(this.lblSpeicherInfo);
            this.Controls.Add(this.groupPV);
            this.Controls.Add(this.groupST);
            this.Controls.Add(this.chartSolar);
            this.Controls.Add(this.lblNutzungsgradST);
            this.Controls.Add(this.lblCO2);
            this.Name = "DashboardForm";
            this.Text = "Planer Dashboard - Solar Simulation";
            ((System.ComponentModel.ISupportInitialize)(this.numSpeicherKWh)).EndInit();
            this.groupPV.ResumeLayout(false);
            this.groupST.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.chartSolar)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Label lblTest;
    }
}