namespace WindowsFormsApplication1
{
    partial class Form_ErgStromverbraucher
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_ErgStromverbraucher));
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.btn_Hilfe = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.textBox_WB_Gebaeude = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox_WB_Extern = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.textBox_WB_Gesamt = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.textBox_MaxWaermelast = new System.Windows.Forms.TextBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.Label27 = new System.Windows.Forms.Label();
            this.Monat_1 = new System.Windows.Forms.TextBox();
            this.Label28 = new System.Windows.Forms.Label();
            this.Monat_7 = new System.Windows.Forms.TextBox();
            this.Label31 = new System.Windows.Forms.Label();
            this.Label33 = new System.Windows.Forms.Label();
            this.Monat_2 = new System.Windows.Forms.TextBox();
            this.Label34 = new System.Windows.Forms.Label();
            this.Monat_8 = new System.Windows.Forms.TextBox();
            this.Label35 = new System.Windows.Forms.Label();
            this.Label38 = new System.Windows.Forms.Label();
            this.Monat_3 = new System.Windows.Forms.TextBox();
            this.Label39 = new System.Windows.Forms.Label();
            this.Monat_9 = new System.Windows.Forms.TextBox();
            this.Label40 = new System.Windows.Forms.Label();
            this.Monat_4 = new System.Windows.Forms.TextBox();
            this.Label43 = new System.Windows.Forms.Label();
            this.Monat_10 = new System.Windows.Forms.TextBox();
            this.Label44 = new System.Windows.Forms.Label();
            this.Label46 = new System.Windows.Forms.Label();
            this.Monat_5 = new System.Windows.Forms.TextBox();
            this.Label47 = new System.Windows.Forms.Label();
            this.Monat_11 = new System.Windows.Forms.TextBox();
            this.Label48 = new System.Windows.Forms.Label();
            this.Label51 = new System.Windows.Forms.Label();
            this.Monat_6 = new System.Windows.Forms.TextBox();
            this.Label52 = new System.Windows.Forms.Label();
            this.Monat_12 = new System.Windows.Forms.TextBox();
            this.Label53 = new System.Windows.Forms.Label();
            this.Label42 = new System.Windows.Forms.Label();
            this.Label54 = new System.Windows.Forms.Label();
            this.Label55 = new System.Windows.Forms.Label();
            this.Label56 = new System.Windows.Forms.Label();
            this.Label57 = new System.Windows.Forms.Label();
            this.Label58 = new System.Windows.Forms.Label();
            this.Label59 = new System.Windows.Forms.Label();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.SuspendLayout();
            // 
            // btn_Hilfe
            // 
            resources.ApplyResources(this.btn_Hilfe, "btn_Hilfe");
            this.btn_Hilfe.Name = "btn_Hilfe";
            this.btn_Hilfe.UseVisualStyleBackColor = true;
            // 
            // btn_OK
            // 
            resources.ApplyResources(this.btn_OK, "btn_OK");
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // tabControl1
            // 
            resources.ApplyResources(this.tabControl1, "tabControl1");
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            // 
            // tabPage1
            // 
            resources.ApplyResources(this.tabPage1, "tabPage1");
            this.tabPage1.Controls.Add(this.label5);
            this.tabPage1.Controls.Add(this.label6);
            this.tabPage1.Controls.Add(this.textBox_WB_Gebaeude);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.textBox_WB_Extern);
            this.tabPage1.Controls.Add(this.label15);
            this.tabPage1.Controls.Add(this.label16);
            this.tabPage1.Controls.Add(this.textBox_WB_Gesamt);
            this.tabPage1.Controls.Add(this.label14);
            this.tabPage1.Controls.Add(this.label13);
            this.tabPage1.Controls.Add(this.textBox_MaxWaermelast);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
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
            this.label6.ForeColor = System.Drawing.Color.Black;
            this.label6.Name = "label6";
            // 
            // textBox_WB_Gebaeude
            // 
            resources.ApplyResources(this.textBox_WB_Gebaeude, "textBox_WB_Gebaeude");
            this.textBox_WB_Gebaeude.Name = "textBox_WB_Gebaeude";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.BackColor = System.Drawing.Color.Black;
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Name = "label1";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.ForeColor = System.Drawing.Color.Black;
            this.label2.Name = "label2";
            // 
            // textBox_WB_Extern
            // 
            resources.ApplyResources(this.textBox_WB_Extern, "textBox_WB_Extern");
            this.textBox_WB_Extern.Name = "textBox_WB_Extern";
            // 
            // label15
            // 
            resources.ApplyResources(this.label15, "label15");
            this.label15.BackColor = System.Drawing.Color.Black;
            this.label15.ForeColor = System.Drawing.Color.White;
            this.label15.Name = "label15";
            // 
            // label16
            // 
            resources.ApplyResources(this.label16, "label16");
            this.label16.ForeColor = System.Drawing.Color.DarkRed;
            this.label16.Name = "label16";
            // 
            // textBox_WB_Gesamt
            // 
            resources.ApplyResources(this.textBox_WB_Gesamt, "textBox_WB_Gesamt");
            this.textBox_WB_Gesamt.ForeColor = System.Drawing.Color.DarkRed;
            this.textBox_WB_Gesamt.Name = "textBox_WB_Gesamt";
            // 
            // label14
            // 
            resources.ApplyResources(this.label14, "label14");
            this.label14.BackColor = System.Drawing.Color.Black;
            this.label14.ForeColor = System.Drawing.Color.White;
            this.label14.Name = "label14";
            // 
            // label13
            // 
            resources.ApplyResources(this.label13, "label13");
            this.label13.ForeColor = System.Drawing.Color.Black;
            this.label13.Name = "label13";
            // 
            // textBox_MaxWaermelast
            // 
            resources.ApplyResources(this.textBox_MaxWaermelast, "textBox_MaxWaermelast");
            this.textBox_MaxWaermelast.Name = "textBox_MaxWaermelast";
            // 
            // tabPage2
            // 
            resources.ApplyResources(this.tabPage2, "tabPage2");
            this.tabPage2.Controls.Add(this.groupBox1);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.Label27);
            this.groupBox1.Controls.Add(this.Monat_1);
            this.groupBox1.Controls.Add(this.Label28);
            this.groupBox1.Controls.Add(this.Monat_7);
            this.groupBox1.Controls.Add(this.Label31);
            this.groupBox1.Controls.Add(this.Label33);
            this.groupBox1.Controls.Add(this.Monat_2);
            this.groupBox1.Controls.Add(this.Label34);
            this.groupBox1.Controls.Add(this.Monat_8);
            this.groupBox1.Controls.Add(this.Label35);
            this.groupBox1.Controls.Add(this.Label38);
            this.groupBox1.Controls.Add(this.Monat_3);
            this.groupBox1.Controls.Add(this.Label39);
            this.groupBox1.Controls.Add(this.Monat_9);
            this.groupBox1.Controls.Add(this.Label40);
            this.groupBox1.Controls.Add(this.Monat_4);
            this.groupBox1.Controls.Add(this.Label43);
            this.groupBox1.Controls.Add(this.Monat_10);
            this.groupBox1.Controls.Add(this.Label44);
            this.groupBox1.Controls.Add(this.Label46);
            this.groupBox1.Controls.Add(this.Monat_5);
            this.groupBox1.Controls.Add(this.Label47);
            this.groupBox1.Controls.Add(this.Monat_11);
            this.groupBox1.Controls.Add(this.Label48);
            this.groupBox1.Controls.Add(this.Label51);
            this.groupBox1.Controls.Add(this.Monat_6);
            this.groupBox1.Controls.Add(this.Label52);
            this.groupBox1.Controls.Add(this.Monat_12);
            this.groupBox1.Controls.Add(this.Label53);
            this.groupBox1.Controls.Add(this.Label42);
            this.groupBox1.Controls.Add(this.Label54);
            this.groupBox1.Controls.Add(this.Label55);
            this.groupBox1.Controls.Add(this.Label56);
            this.groupBox1.Controls.Add(this.Label57);
            this.groupBox1.Controls.Add(this.Label58);
            this.groupBox1.Controls.Add(this.Label59);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // Label27
            // 
            resources.ApplyResources(this.Label27, "Label27");
            this.Label27.Name = "Label27";
            // 
            // Monat_1
            // 
            resources.ApplyResources(this.Monat_1, "Monat_1");
            this.Monat_1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Monat_1.Name = "Monat_1";
            // 
            // Label28
            // 
            resources.ApplyResources(this.Label28, "Label28");
            this.Label28.BackColor = System.Drawing.Color.Black;
            this.Label28.ForeColor = System.Drawing.Color.White;
            this.Label28.Name = "Label28";
            // 
            // Monat_7
            // 
            resources.ApplyResources(this.Monat_7, "Monat_7");
            this.Monat_7.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Monat_7.Name = "Monat_7";
            // 
            // Label31
            // 
            resources.ApplyResources(this.Label31, "Label31");
            this.Label31.BackColor = System.Drawing.Color.Black;
            this.Label31.ForeColor = System.Drawing.Color.White;
            this.Label31.Name = "Label31";
            // 
            // Label33
            // 
            resources.ApplyResources(this.Label33, "Label33");
            this.Label33.Name = "Label33";
            // 
            // Monat_2
            // 
            resources.ApplyResources(this.Monat_2, "Monat_2");
            this.Monat_2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Monat_2.Name = "Monat_2";
            // 
            // Label34
            // 
            resources.ApplyResources(this.Label34, "Label34");
            this.Label34.BackColor = System.Drawing.Color.Black;
            this.Label34.ForeColor = System.Drawing.Color.White;
            this.Label34.Name = "Label34";
            // 
            // Monat_8
            // 
            resources.ApplyResources(this.Monat_8, "Monat_8");
            this.Monat_8.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Monat_8.Name = "Monat_8";
            // 
            // Label35
            // 
            resources.ApplyResources(this.Label35, "Label35");
            this.Label35.BackColor = System.Drawing.Color.Black;
            this.Label35.ForeColor = System.Drawing.Color.White;
            this.Label35.Name = "Label35";
            // 
            // Label38
            // 
            resources.ApplyResources(this.Label38, "Label38");
            this.Label38.Name = "Label38";
            // 
            // Monat_3
            // 
            resources.ApplyResources(this.Monat_3, "Monat_3");
            this.Monat_3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Monat_3.Name = "Monat_3";
            // 
            // Label39
            // 
            resources.ApplyResources(this.Label39, "Label39");
            this.Label39.BackColor = System.Drawing.Color.Black;
            this.Label39.ForeColor = System.Drawing.Color.White;
            this.Label39.Name = "Label39";
            // 
            // Monat_9
            // 
            resources.ApplyResources(this.Monat_9, "Monat_9");
            this.Monat_9.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Monat_9.Name = "Monat_9";
            // 
            // Label40
            // 
            resources.ApplyResources(this.Label40, "Label40");
            this.Label40.BackColor = System.Drawing.Color.Black;
            this.Label40.ForeColor = System.Drawing.Color.White;
            this.Label40.Name = "Label40";
            // 
            // Monat_4
            // 
            resources.ApplyResources(this.Monat_4, "Monat_4");
            this.Monat_4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Monat_4.Name = "Monat_4";
            // 
            // Label43
            // 
            resources.ApplyResources(this.Label43, "Label43");
            this.Label43.BackColor = System.Drawing.Color.Black;
            this.Label43.ForeColor = System.Drawing.Color.White;
            this.Label43.Name = "Label43";
            // 
            // Monat_10
            // 
            resources.ApplyResources(this.Monat_10, "Monat_10");
            this.Monat_10.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Monat_10.Name = "Monat_10";
            // 
            // Label44
            // 
            resources.ApplyResources(this.Label44, "Label44");
            this.Label44.BackColor = System.Drawing.Color.Black;
            this.Label44.ForeColor = System.Drawing.Color.White;
            this.Label44.Name = "Label44";
            // 
            // Label46
            // 
            resources.ApplyResources(this.Label46, "Label46");
            this.Label46.Name = "Label46";
            // 
            // Monat_5
            // 
            resources.ApplyResources(this.Monat_5, "Monat_5");
            this.Monat_5.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Monat_5.Name = "Monat_5";
            // 
            // Label47
            // 
            resources.ApplyResources(this.Label47, "Label47");
            this.Label47.BackColor = System.Drawing.Color.Black;
            this.Label47.ForeColor = System.Drawing.Color.White;
            this.Label47.Name = "Label47";
            // 
            // Monat_11
            // 
            resources.ApplyResources(this.Monat_11, "Monat_11");
            this.Monat_11.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Monat_11.Name = "Monat_11";
            // 
            // Label48
            // 
            resources.ApplyResources(this.Label48, "Label48");
            this.Label48.BackColor = System.Drawing.Color.Black;
            this.Label48.ForeColor = System.Drawing.Color.White;
            this.Label48.Name = "Label48";
            // 
            // Label51
            // 
            resources.ApplyResources(this.Label51, "Label51");
            this.Label51.Name = "Label51";
            // 
            // Monat_6
            // 
            resources.ApplyResources(this.Monat_6, "Monat_6");
            this.Monat_6.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Monat_6.Name = "Monat_6";
            // 
            // Label52
            // 
            resources.ApplyResources(this.Label52, "Label52");
            this.Label52.BackColor = System.Drawing.Color.Black;
            this.Label52.ForeColor = System.Drawing.Color.White;
            this.Label52.Name = "Label52";
            // 
            // Monat_12
            // 
            resources.ApplyResources(this.Monat_12, "Monat_12");
            this.Monat_12.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Monat_12.Name = "Monat_12";
            // 
            // Label53
            // 
            resources.ApplyResources(this.Label53, "Label53");
            this.Label53.BackColor = System.Drawing.Color.Black;
            this.Label53.ForeColor = System.Drawing.Color.White;
            this.Label53.Name = "Label53";
            // 
            // Label42
            // 
            resources.ApplyResources(this.Label42, "Label42");
            this.Label42.Name = "Label42";
            // 
            // Label54
            // 
            resources.ApplyResources(this.Label54, "Label54");
            this.Label54.Name = "Label54";
            // 
            // Label55
            // 
            resources.ApplyResources(this.Label55, "Label55");
            this.Label55.Name = "Label55";
            // 
            // Label56
            // 
            resources.ApplyResources(this.Label56, "Label56");
            this.Label56.Name = "Label56";
            // 
            // Label57
            // 
            resources.ApplyResources(this.Label57, "Label57");
            this.Label57.Name = "Label57";
            // 
            // Label58
            // 
            resources.ApplyResources(this.Label58, "Label58");
            this.Label58.Name = "Label58";
            // 
            // Label59
            // 
            resources.ApplyResources(this.Label59, "Label59");
            this.Label59.Name = "Label59";
            // 
            // tabPage3
            // 
            resources.ApplyResources(this.tabPage3, "tabPage3");
            this.tabPage3.Controls.Add(this.chart1);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // chart1
            // 
            resources.ApplyResources(this.chart1, "chart1");
            chartArea1.AxisX.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chartArea1.AxisX.Title = "Monat";
            chartArea1.AxisX.TitleFont = new System.Drawing.Font("Segoe UI", 10F);
            chartArea1.AxisY.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chartArea1.AxisY.Title = "Strombedarf [MWh]";
            chartArea1.AxisY.TitleFont = new System.Drawing.Font("Segoe UI", 10F);
            chartArea1.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chartArea1.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            this.chart1.Legends.Add(legend1);
            this.chart1.Name = "chart1";
            series1.ChartArea = "ChartArea1";
            series1.IsVisibleInLegend = false;
            series1.Legend = "Legend1";
            series1.Name = "Series1";
            this.chart1.Series.Add(series1);
            // 
            // Form_ErgStromverbraucher
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.btn_Hilfe);
            this.Controls.Add(this.btn_OK);
            this.Name = "Form_ErgStromverbraucher";
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

private System.Windows.Forms.Button btn_Hilfe;
private System.Windows.Forms.Button btn_OK;
private System.Windows.Forms.TabControl tabControl1;
private System.Windows.Forms.TabPage tabPage1;
private System.Windows.Forms.Label label5;
private System.Windows.Forms.Label label6;
private System.Windows.Forms.TextBox textBox_WB_Gebaeude;
private System.Windows.Forms.Label label1;
private System.Windows.Forms.Label label2;
private System.Windows.Forms.TextBox textBox_WB_Extern;
private System.Windows.Forms.Label label15;
private System.Windows.Forms.Label label16;
private System.Windows.Forms.TextBox textBox_WB_Gesamt;
private System.Windows.Forms.Label label14;
private System.Windows.Forms.Label label13;
private System.Windows.Forms.TextBox textBox_MaxWaermelast;
private System.Windows.Forms.TabPage tabPage2;
private System.Windows.Forms.GroupBox groupBox1;
private System.Windows.Forms.Label Label27;
private System.Windows.Forms.TextBox Monat_1;
private System.Windows.Forms.Label Label28;
private System.Windows.Forms.TextBox Monat_7;
private System.Windows.Forms.Label Label31;
private System.Windows.Forms.Label Label33;
private System.Windows.Forms.TextBox Monat_2;
private System.Windows.Forms.Label Label34;
private System.Windows.Forms.TextBox Monat_8;
private System.Windows.Forms.Label Label35;
private System.Windows.Forms.Label Label38;
private System.Windows.Forms.TextBox Monat_3;
private System.Windows.Forms.Label Label39;
private System.Windows.Forms.TextBox Monat_9;
private System.Windows.Forms.Label Label40;
private System.Windows.Forms.TextBox Monat_4;
private System.Windows.Forms.Label Label43;
private System.Windows.Forms.TextBox Monat_10;
private System.Windows.Forms.Label Label44;
private System.Windows.Forms.Label Label46;
private System.Windows.Forms.TextBox Monat_5;
private System.Windows.Forms.Label Label47;
private System.Windows.Forms.TextBox Monat_11;
private System.Windows.Forms.Label Label48;
private System.Windows.Forms.Label Label51;
private System.Windows.Forms.TextBox Monat_6;
private System.Windows.Forms.Label Label52;
private System.Windows.Forms.TextBox Monat_12;
private System.Windows.Forms.Label Label53;
private System.Windows.Forms.Label Label42;
private System.Windows.Forms.Label Label54;
private System.Windows.Forms.Label Label55;
private System.Windows.Forms.Label Label56;
private System.Windows.Forms.Label Label57;
private System.Windows.Forms.Label Label58;
private System.Windows.Forms.Label Label59;
private System.Windows.Forms.TabPage tabPage3;
private System.Windows.Forms.DataVisualization.Charting.Chart chart1;


 
    }
}