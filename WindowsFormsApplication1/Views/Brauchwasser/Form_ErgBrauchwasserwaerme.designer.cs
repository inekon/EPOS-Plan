namespace WindowsFormsApplication1
{
    partial class Form_ErgBrauchwasserwaerme
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_ErgBrauchwasserwaerme));
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            btn_Hilfe = new System.Windows.Forms.Button();
            btn_OK = new System.Windows.Forms.Button();
            tabPage3 = new System.Windows.Forms.TabPage();
            checkBox_MonatJahr = new System.Windows.Forms.CheckBox();
            radioBtn_GrafikBrauchwasser = new System.Windows.Forms.RadioButton();
            radioBtn_GrafikGebäude = new System.Windows.Forms.RadioButton();
            radioBtn_GrafikProzesse = new System.Windows.Forms.RadioButton();
            chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            tabPage2 = new System.Windows.Forms.TabPage();
            radioBtn_Brauchwasser = new System.Windows.Forms.RadioButton();
            radioBtn_Gebäude = new System.Windows.Forms.RadioButton();
            radioBtn_Prozesse = new System.Windows.Forms.RadioButton();
            groupBox1 = new System.Windows.Forms.GroupBox();
            Label27 = new System.Windows.Forms.Label();
            Monat_1 = new System.Windows.Forms.TextBox();
            Label28 = new System.Windows.Forms.Label();
            Monat_7 = new System.Windows.Forms.TextBox();
            Label31 = new System.Windows.Forms.Label();
            Label33 = new System.Windows.Forms.Label();
            Monat_2 = new System.Windows.Forms.TextBox();
            Label34 = new System.Windows.Forms.Label();
            Monat_8 = new System.Windows.Forms.TextBox();
            Label35 = new System.Windows.Forms.Label();
            Label38 = new System.Windows.Forms.Label();
            Monat_3 = new System.Windows.Forms.TextBox();
            Label39 = new System.Windows.Forms.Label();
            Monat_9 = new System.Windows.Forms.TextBox();
            Label40 = new System.Windows.Forms.Label();
            Monat_4 = new System.Windows.Forms.TextBox();
            Label43 = new System.Windows.Forms.Label();
            Monat_10 = new System.Windows.Forms.TextBox();
            Label44 = new System.Windows.Forms.Label();
            Label46 = new System.Windows.Forms.Label();
            Monat_5 = new System.Windows.Forms.TextBox();
            Label47 = new System.Windows.Forms.Label();
            Monat_11 = new System.Windows.Forms.TextBox();
            Label48 = new System.Windows.Forms.Label();
            Label51 = new System.Windows.Forms.Label();
            Monat_6 = new System.Windows.Forms.TextBox();
            Label52 = new System.Windows.Forms.Label();
            Monat_12 = new System.Windows.Forms.TextBox();
            Label53 = new System.Windows.Forms.Label();
            Label42 = new System.Windows.Forms.Label();
            Label54 = new System.Windows.Forms.Label();
            Label55 = new System.Windows.Forms.Label();
            Label56 = new System.Windows.Forms.Label();
            Label57 = new System.Windows.Forms.Label();
            Label58 = new System.Windows.Forms.Label();
            Label59 = new System.Windows.Forms.Label();
            tabPage1 = new System.Windows.Forms.TabPage();
            textBox_WB_Brauchwasser = new System.Windows.Forms.TextBox();
            label7 = new System.Windows.Forms.Label();
            label8 = new System.Windows.Forms.Label();
            label14 = new System.Windows.Forms.Label();
            label13 = new System.Windows.Forms.Label();
            textBox_MaxWaermelast = new System.Windows.Forms.TextBox();
            textBox_WB_Gebaeude = new System.Windows.Forms.TextBox();
            textBox_WB_Prozess = new System.Windows.Forms.TextBox();
            textBox_WB_Extern = new System.Windows.Forms.TextBox();
            textBox_WB_Gesamt = new System.Windows.Forms.TextBox();
            textBox_Netzverluste = new System.Windows.Forms.TextBox();
            label_Netzverluste_Einheit = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            label15 = new System.Windows.Forms.Label();
            label16 = new System.Windows.Forms.Label();
            label12 = new System.Windows.Forms.Label();
            tabControl1 = new System.Windows.Forms.TabControl();
            tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)chart1).BeginInit();
            tabPage2.SuspendLayout();
            groupBox1.SuspendLayout();
            tabPage1.SuspendLayout();
            tabControl1.SuspendLayout();
            SuspendLayout();
            // 
            // btn_Hilfe
            // 
            resources.ApplyResources(btn_Hilfe, "btn_Hilfe");
            btn_Hilfe.Name = "btn_Hilfe";
            btn_Hilfe.UseVisualStyleBackColor = true;
            // 
            // btn_OK
            // 
            resources.ApplyResources(btn_OK, "btn_OK");
            btn_OK.Name = "btn_OK";
            btn_OK.UseVisualStyleBackColor = true;
            btn_OK.Click += btn_OK_Click;
            // 
            // tabPage3
            // 
            tabPage3.Controls.Add(checkBox_MonatJahr);
            tabPage3.Controls.Add(radioBtn_GrafikBrauchwasser);
            tabPage3.Controls.Add(radioBtn_GrafikGebäude);
            tabPage3.Controls.Add(radioBtn_GrafikProzesse);
            tabPage3.Controls.Add(chart1);
            resources.ApplyResources(tabPage3, "tabPage3");
            tabPage3.Name = "tabPage3";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // checkBox_MonatJahr
            // 
            resources.ApplyResources(checkBox_MonatJahr, "checkBox_MonatJahr");
            checkBox_MonatJahr.Name = "checkBox_MonatJahr";
            checkBox_MonatJahr.UseVisualStyleBackColor = true;
            checkBox_MonatJahr.CheckedChanged += checkBox_MonatJahr_CheckedChanged;
            // 
            // radioBtn_GrafikBrauchwasser
            // 
            resources.ApplyResources(radioBtn_GrafikBrauchwasser, "radioBtn_GrafikBrauchwasser");
            radioBtn_GrafikBrauchwasser.Name = "radioBtn_GrafikBrauchwasser";
            radioBtn_GrafikBrauchwasser.UseVisualStyleBackColor = true;
            radioBtn_GrafikBrauchwasser.CheckedChanged += radioBtn_GrafikBrauchwasser_CheckedChanged;
            // 
            // radioBtn_GrafikGebäude
            // 
            resources.ApplyResources(radioBtn_GrafikGebäude, "radioBtn_GrafikGebäude");
            radioBtn_GrafikGebäude.Name = "radioBtn_GrafikGebäude";
            radioBtn_GrafikGebäude.UseVisualStyleBackColor = true;
            radioBtn_GrafikGebäude.CheckedChanged += radioBtn_GrafikGebäude_CheckedChanged;
            // 
            // radioBtn_GrafikProzesse
            // 
            resources.ApplyResources(radioBtn_GrafikProzesse, "radioBtn_GrafikProzesse");
            radioBtn_GrafikProzesse.Name = "radioBtn_GrafikProzesse";
            radioBtn_GrafikProzesse.UseVisualStyleBackColor = true;
            radioBtn_GrafikProzesse.CheckedChanged += radioBtn_GrafikProzesse_CheckedChanged;
            // 
            // chart1
            // 
            chartArea1.AxisX.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chartArea1.AxisX.Title = "Monat";
            chartArea1.AxisX.TitleFont = new System.Drawing.Font("Segoe UI", 10F);
            chartArea1.AxisY.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chartArea1.AxisY.Title = "Wärmebedarf [MWh]";
            chartArea1.AxisY.TitleFont = new System.Drawing.Font("Segoe UI", 10F);
            chartArea1.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chartArea1.Name = "ChartArea1";
            chart1.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            chart1.Legends.Add(legend1);
            resources.ApplyResources(chart1, "chart1");
            chart1.Name = "chart1";
            series1.ChartArea = "ChartArea1";
            series1.IsVisibleInLegend = false;
            series1.Legend = "Legend1";
            series1.Name = "Series1";
            chart1.Series.Add(series1);
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(radioBtn_Brauchwasser);
            tabPage2.Controls.Add(radioBtn_Gebäude);
            tabPage2.Controls.Add(radioBtn_Prozesse);
            tabPage2.Controls.Add(groupBox1);
            resources.ApplyResources(tabPage2, "tabPage2");
            tabPage2.Name = "tabPage2";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // radioBtn_Brauchwasser
            // 
            resources.ApplyResources(radioBtn_Brauchwasser, "radioBtn_Brauchwasser");
            radioBtn_Brauchwasser.Name = "radioBtn_Brauchwasser";
            radioBtn_Brauchwasser.UseVisualStyleBackColor = true;
            radioBtn_Brauchwasser.CheckedChanged += radioBtn_Brauchwasser_CheckedChanged;
            // 
            // radioBtn_Gebäude
            // 
            resources.ApplyResources(radioBtn_Gebäude, "radioBtn_Gebäude");
            radioBtn_Gebäude.Name = "radioBtn_Gebäude";
            radioBtn_Gebäude.UseVisualStyleBackColor = true;
            radioBtn_Gebäude.CheckedChanged += radioBtn_Gebäude_CheckedChanged;
            // 
            // radioBtn_Prozesse
            // 
            resources.ApplyResources(radioBtn_Prozesse, "radioBtn_Prozesse");
            radioBtn_Prozesse.Name = "radioBtn_Prozesse";
            radioBtn_Prozesse.UseVisualStyleBackColor = true;
            radioBtn_Prozesse.CheckedChanged += radioBtn_Prozesse_CheckedChanged;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(Label27);
            groupBox1.Controls.Add(Monat_1);
            groupBox1.Controls.Add(Label28);
            groupBox1.Controls.Add(Monat_7);
            groupBox1.Controls.Add(Label31);
            groupBox1.Controls.Add(Label33);
            groupBox1.Controls.Add(Monat_2);
            groupBox1.Controls.Add(Label34);
            groupBox1.Controls.Add(Monat_8);
            groupBox1.Controls.Add(Label35);
            groupBox1.Controls.Add(Label38);
            groupBox1.Controls.Add(Monat_3);
            groupBox1.Controls.Add(Label39);
            groupBox1.Controls.Add(Monat_9);
            groupBox1.Controls.Add(Label40);
            groupBox1.Controls.Add(Monat_4);
            groupBox1.Controls.Add(Label43);
            groupBox1.Controls.Add(Monat_10);
            groupBox1.Controls.Add(Label44);
            groupBox1.Controls.Add(Label46);
            groupBox1.Controls.Add(Monat_5);
            groupBox1.Controls.Add(Label47);
            groupBox1.Controls.Add(Monat_11);
            groupBox1.Controls.Add(Label48);
            groupBox1.Controls.Add(Label51);
            groupBox1.Controls.Add(Monat_6);
            groupBox1.Controls.Add(Label52);
            groupBox1.Controls.Add(Monat_12);
            groupBox1.Controls.Add(Label53);
            groupBox1.Controls.Add(Label42);
            groupBox1.Controls.Add(Label54);
            groupBox1.Controls.Add(Label55);
            groupBox1.Controls.Add(Label56);
            groupBox1.Controls.Add(Label57);
            groupBox1.Controls.Add(Label58);
            groupBox1.Controls.Add(Label59);
            resources.ApplyResources(groupBox1, "groupBox1");
            groupBox1.Name = "groupBox1";
            groupBox1.TabStop = false;
            // 
            // Label27
            // 
            resources.ApplyResources(Label27, "Label27");
            Label27.Name = "Label27";
            // 
            // Monat_1
            // 
            Monat_1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(Monat_1, "Monat_1");
            Monat_1.Name = "Monat_1";
            // 
            // Label28
            // 
            Label28.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(Label28, "Label28");
            Label28.ForeColor = System.Drawing.Color.White;
            Label28.Name = "Label28";
            // 
            // Monat_7
            // 
            Monat_7.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(Monat_7, "Monat_7");
            Monat_7.Name = "Monat_7";
            // 
            // Label31
            // 
            Label31.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(Label31, "Label31");
            Label31.ForeColor = System.Drawing.Color.White;
            Label31.Name = "Label31";
            // 
            // Label33
            // 
            resources.ApplyResources(Label33, "Label33");
            Label33.Name = "Label33";
            // 
            // Monat_2
            // 
            Monat_2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(Monat_2, "Monat_2");
            Monat_2.Name = "Monat_2";
            // 
            // Label34
            // 
            Label34.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(Label34, "Label34");
            Label34.ForeColor = System.Drawing.Color.White;
            Label34.Name = "Label34";
            // 
            // Monat_8
            // 
            Monat_8.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(Monat_8, "Monat_8");
            Monat_8.Name = "Monat_8";
            // 
            // Label35
            // 
            Label35.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(Label35, "Label35");
            Label35.ForeColor = System.Drawing.Color.White;
            Label35.Name = "Label35";
            // 
            // Label38
            // 
            resources.ApplyResources(Label38, "Label38");
            Label38.Name = "Label38";
            // 
            // Monat_3
            // 
            Monat_3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(Monat_3, "Monat_3");
            Monat_3.Name = "Monat_3";
            // 
            // Label39
            // 
            Label39.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(Label39, "Label39");
            Label39.ForeColor = System.Drawing.Color.White;
            Label39.Name = "Label39";
            // 
            // Monat_9
            // 
            Monat_9.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(Monat_9, "Monat_9");
            Monat_9.Name = "Monat_9";
            // 
            // Label40
            // 
            Label40.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(Label40, "Label40");
            Label40.ForeColor = System.Drawing.Color.White;
            Label40.Name = "Label40";
            // 
            // Monat_4
            // 
            Monat_4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(Monat_4, "Monat_4");
            Monat_4.Name = "Monat_4";
            // 
            // Label43
            // 
            Label43.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(Label43, "Label43");
            Label43.ForeColor = System.Drawing.Color.White;
            Label43.Name = "Label43";
            // 
            // Monat_10
            // 
            Monat_10.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(Monat_10, "Monat_10");
            Monat_10.Name = "Monat_10";
            // 
            // Label44
            // 
            Label44.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(Label44, "Label44");
            Label44.ForeColor = System.Drawing.Color.White;
            Label44.Name = "Label44";
            // 
            // Label46
            // 
            resources.ApplyResources(Label46, "Label46");
            Label46.Name = "Label46";
            // 
            // Monat_5
            // 
            Monat_5.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(Monat_5, "Monat_5");
            Monat_5.Name = "Monat_5";
            // 
            // Label47
            // 
            Label47.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(Label47, "Label47");
            Label47.ForeColor = System.Drawing.Color.White;
            Label47.Name = "Label47";
            // 
            // Monat_11
            // 
            Monat_11.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(Monat_11, "Monat_11");
            Monat_11.Name = "Monat_11";
            // 
            // Label48
            // 
            Label48.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(Label48, "Label48");
            Label48.ForeColor = System.Drawing.Color.White;
            Label48.Name = "Label48";
            // 
            // Label51
            // 
            resources.ApplyResources(Label51, "Label51");
            Label51.Name = "Label51";
            // 
            // Monat_6
            // 
            Monat_6.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(Monat_6, "Monat_6");
            Monat_6.Name = "Monat_6";
            // 
            // Label52
            // 
            Label52.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(Label52, "Label52");
            Label52.ForeColor = System.Drawing.Color.White;
            Label52.Name = "Label52";
            // 
            // Monat_12
            // 
            Monat_12.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(Monat_12, "Monat_12");
            Monat_12.Name = "Monat_12";
            // 
            // Label53
            // 
            Label53.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(Label53, "Label53");
            Label53.ForeColor = System.Drawing.Color.White;
            Label53.Name = "Label53";
            // 
            // Label42
            // 
            resources.ApplyResources(Label42, "Label42");
            Label42.Name = "Label42";
            // 
            // Label54
            // 
            resources.ApplyResources(Label54, "Label54");
            Label54.Name = "Label54";
            // 
            // Label55
            // 
            resources.ApplyResources(Label55, "Label55");
            Label55.Name = "Label55";
            // 
            // Label56
            // 
            resources.ApplyResources(Label56, "Label56");
            Label56.Name = "Label56";
            // 
            // Label57
            // 
            resources.ApplyResources(Label57, "Label57");
            Label57.Name = "Label57";
            // 
            // Label58
            // 
            resources.ApplyResources(Label58, "Label58");
            Label58.Name = "Label58";
            // 
            // Label59
            // 
            resources.ApplyResources(Label59, "Label59");
            Label59.Name = "Label59";
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(textBox_WB_Brauchwasser);
            tabPage1.Controls.Add(label7);
            tabPage1.Controls.Add(label8);
            tabPage1.Controls.Add(label14);
            tabPage1.Controls.Add(label13);
            tabPage1.Controls.Add(textBox_MaxWaermelast);
            tabPage1.Controls.Add(textBox_WB_Gebaeude);
            tabPage1.Controls.Add(textBox_WB_Prozess);
            tabPage1.Controls.Add(textBox_WB_Extern);
            tabPage1.Controls.Add(textBox_WB_Gesamt);
            tabPage1.Controls.Add(textBox_Netzverluste);
            tabPage1.Controls.Add(label_Netzverluste_Einheit);
            tabPage1.Controls.Add(label5);
            tabPage1.Controls.Add(label6);
            tabPage1.Controls.Add(label3);
            tabPage1.Controls.Add(label4);
            tabPage1.Controls.Add(label1);
            tabPage1.Controls.Add(label2);
            tabPage1.Controls.Add(label15);
            tabPage1.Controls.Add(label16);
            tabPage1.Controls.Add(label12);
            resources.ApplyResources(tabPage1, "tabPage1");
            tabPage1.Name = "tabPage1";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // textBox_WB_Brauchwasser
            // 
            resources.ApplyResources(textBox_WB_Brauchwasser, "textBox_WB_Brauchwasser");
            textBox_WB_Brauchwasser.Name = "textBox_WB_Brauchwasser";
            // 
            // label7
            // 
            label7.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(label7, "label7");
            label7.ForeColor = System.Drawing.Color.White;
            label7.Name = "label7";
            // 
            // label8
            // 
            resources.ApplyResources(label8, "label8");
            label8.ForeColor = System.Drawing.Color.Black;
            label8.Name = "label8";
            // 
            // label14
            // 
            label14.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(label14, "label14");
            label14.ForeColor = System.Drawing.Color.White;
            label14.Name = "label14";
            // 
            // label13
            // 
            resources.ApplyResources(label13, "label13");
            label13.ForeColor = System.Drawing.Color.Black;
            label13.Name = "label13";
            // 
            // textBox_MaxWaermelast
            // 
            resources.ApplyResources(textBox_MaxWaermelast, "textBox_MaxWaermelast");
            textBox_MaxWaermelast.Name = "textBox_MaxWaermelast";
            // 
            // textBox_WB_Gebaeude
            // 
            resources.ApplyResources(textBox_WB_Gebaeude, "textBox_WB_Gebaeude");
            textBox_WB_Gebaeude.Name = "textBox_WB_Gebaeude";
            // 
            // textBox_WB_Prozess
            // 
            resources.ApplyResources(textBox_WB_Prozess, "textBox_WB_Prozess");
            textBox_WB_Prozess.Name = "textBox_WB_Prozess";
            // 
            // textBox_WB_Extern
            // 
            resources.ApplyResources(textBox_WB_Extern, "textBox_WB_Extern");
            textBox_WB_Extern.Name = "textBox_WB_Extern";
            // 
            // textBox_WB_Gesamt
            // 
            textBox_WB_Gesamt.ForeColor = System.Drawing.Color.DarkRed;
            resources.ApplyResources(textBox_WB_Gesamt, "textBox_WB_Gesamt");
            textBox_WB_Gesamt.Name = "textBox_WB_Gesamt";
            // 
            // textBox_Netzverluste
            // 
            resources.ApplyResources(textBox_Netzverluste, "textBox_Netzverluste");
            textBox_Netzverluste.Name = "textBox_Netzverluste";
            // 
            // label_Netzverluste_Einheit
            // 
            label_Netzverluste_Einheit.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(label_Netzverluste_Einheit, "label_Netzverluste_Einheit");
            label_Netzverluste_Einheit.ForeColor = System.Drawing.Color.White;
            label_Netzverluste_Einheit.Name = "label_Netzverluste_Einheit";
            // 
            // label5
            // 
            label5.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(label5, "label5");
            label5.ForeColor = System.Drawing.Color.White;
            label5.Name = "label5";
            // 
            // label6
            // 
            resources.ApplyResources(label6, "label6");
            label6.ForeColor = System.Drawing.Color.Black;
            label6.Name = "label6";
            // 
            // label3
            // 
            label3.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(label3, "label3");
            label3.ForeColor = System.Drawing.Color.White;
            label3.Name = "label3";
            // 
            // label4
            // 
            resources.ApplyResources(label4, "label4");
            label4.ForeColor = System.Drawing.Color.Black;
            label4.Name = "label4";
            // 
            // label1
            // 
            label1.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(label1, "label1");
            label1.ForeColor = System.Drawing.Color.White;
            label1.Name = "label1";
            // 
            // label2
            // 
            resources.ApplyResources(label2, "label2");
            label2.ForeColor = System.Drawing.Color.Black;
            label2.Name = "label2";
            // 
            // label15
            // 
            label15.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(label15, "label15");
            label15.ForeColor = System.Drawing.Color.White;
            label15.Name = "label15";
            // 
            // label16
            // 
            resources.ApplyResources(label16, "label16");
            label16.ForeColor = System.Drawing.Color.DarkRed;
            label16.Name = "label16";
            // 
            // label12
            // 
            resources.ApplyResources(label12, "label12");
            label12.ForeColor = System.Drawing.Color.Black;
            label12.Name = "label12";
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Controls.Add(tabPage3);
            resources.ApplyResources(tabControl1, "tabControl1");
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            // 
            // Form_ErgBrauchwasserwaerme
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.White;
            Controls.Add(tabControl1);
            Controls.Add(btn_Hilfe);
            Controls.Add(btn_OK);
            Name = "Form_ErgBrauchwasserwaerme";
            tabPage3.ResumeLayout(false);
            tabPage3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)chart1).EndInit();
            tabPage2.ResumeLayout(false);
            tabPage2.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            tabControl1.ResumeLayout(false);
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btn_Hilfe;
private System.Windows.Forms.Button btn_OK;
private System.Windows.Forms.TabPage tabPage3;
private System.Windows.Forms.DataVisualization.Charting.Chart chart1;
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
private System.Windows.Forms.TabPage tabPage1;
private System.Windows.Forms.Label label14;
private System.Windows.Forms.Label label13;
private System.Windows.Forms.TextBox textBox_MaxWaermelast;
private System.Windows.Forms.TextBox textBox_WB_Gebaeude;
private System.Windows.Forms.TextBox textBox_WB_Prozess;
private System.Windows.Forms.TextBox textBox_WB_Extern;
private System.Windows.Forms.TextBox textBox_WB_Gesamt;
private System.Windows.Forms.TextBox textBox_Netzverluste;
private System.Windows.Forms.Label label_Netzverluste_Einheit;
private System.Windows.Forms.Label label5;
private System.Windows.Forms.Label label6;
private System.Windows.Forms.Label label3;
private System.Windows.Forms.Label label4;
private System.Windows.Forms.Label label1;
private System.Windows.Forms.Label label2;
private System.Windows.Forms.Label label15;
private System.Windows.Forms.Label label16;
private System.Windows.Forms.Label label12;
private System.Windows.Forms.TabControl tabControl1;
private System.Windows.Forms.RadioButton radioBtn_GrafikGebäude;
private System.Windows.Forms.RadioButton radioBtn_GrafikProzesse;
private System.Windows.Forms.RadioButton radioBtn_Gebäude;
private System.Windows.Forms.RadioButton radioBtn_Prozesse;
private System.Windows.Forms.TextBox textBox_WB_Brauchwasser;
private System.Windows.Forms.Label label7;
private System.Windows.Forms.Label label8;
        private System.Windows.Forms.RadioButton radioBtn_Brauchwasser;
        private System.Windows.Forms.RadioButton radioBtn_GrafikBrauchwasser;
        private System.Windows.Forms.CheckBox checkBox_MonatJahr;
    }
}