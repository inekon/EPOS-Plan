namespace WindowsFormsApplication1
{
    partial class Form_ErgProzesswaerme
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
            this.btn_Hilfe = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.label_Netzverluste_Einheit = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.textBox_WB_Gebaeude = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox_WB_Prozess = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox_WB_Extern = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.textBox_WB_Gesamt = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.textBox_Netzverluste = new System.Windows.Forms.TextBox();
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
            this.btn_Hilfe.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Hilfe.Location = new System.Drawing.Point(27, 303);
            this.btn_Hilfe.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Hilfe.Name = "btn_Hilfe";
            this.btn_Hilfe.Size = new System.Drawing.Size(77, 31);
            this.btn_Hilfe.TabIndex = 0;
            this.btn_Hilfe.Text = "Hilfe";
            this.btn_Hilfe.UseVisualStyleBackColor = true;
            // 
            // btn_OK
            // 
            this.btn_OK.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_OK.Location = new System.Drawing.Point(474, 302);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(77, 31);
            this.btn_OK.TabIndex = 1;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Location = new System.Drawing.Point(23, 22);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(528, 265);
            this.tabControl1.TabIndex = 39;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label_Netzverluste_Einheit);
            this.tabPage1.Controls.Add(this.label5);
            this.tabPage1.Controls.Add(this.label6);
            this.tabPage1.Controls.Add(this.textBox_WB_Gebaeude);
            this.tabPage1.Controls.Add(this.label3);
            this.tabPage1.Controls.Add(this.label4);
            this.tabPage1.Controls.Add(this.textBox_WB_Prozess);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.textBox_WB_Extern);
            this.tabPage1.Controls.Add(this.label15);
            this.tabPage1.Controls.Add(this.label16);
            this.tabPage1.Controls.Add(this.textBox_WB_Gesamt);
            this.tabPage1.Controls.Add(this.label14);
            this.tabPage1.Controls.Add(this.label13);
            this.tabPage1.Controls.Add(this.label12);
            this.tabPage1.Controls.Add(this.textBox_Netzverluste);
            this.tabPage1.Controls.Add(this.textBox_MaxWaermelast);
            this.tabPage1.Location = new System.Drawing.Point(4, 26);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(520, 235);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Wärmebedarf Ergebnisse";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // label_Netzverluste_Einheit
            // 
            this.label_Netzverluste_Einheit.AutoSize = true;
            this.label_Netzverluste_Einheit.BackColor = System.Drawing.Color.Black;
            this.label_Netzverluste_Einheit.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.label_Netzverluste_Einheit.ForeColor = System.Drawing.Color.White;
            this.label_Netzverluste_Einheit.Location = new System.Drawing.Point(298, 154);
            this.label_Netzverluste_Einheit.Name = "label_Netzverluste_Einheit";
            this.label_Netzverluste_Einheit.Size = new System.Drawing.Size(35, 13);
            this.label_Netzverluste_Einheit.TabIndex = 74;
            this.label_Netzverluste_Einheit.Text = "MWh";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.BackColor = System.Drawing.Color.Black;
            this.label5.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.label5.ForeColor = System.Drawing.Color.White;
            this.label5.Location = new System.Drawing.Point(298, 24);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(35, 13);
            this.label5.TabIndex = 73;
            this.label5.Text = "MWh";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label6.ForeColor = System.Drawing.Color.Black;
            this.label6.Location = new System.Drawing.Point(40, 21);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(154, 19);
            this.label6.TabIndex = 72;
            this.label6.Text = "Wärmebedarf Gebäude:";
            // 
            // textBox_WB_Gebaeude
            // 
            this.textBox_WB_Gebaeude.Location = new System.Drawing.Point(209, 18);
            this.textBox_WB_Gebaeude.Name = "textBox_WB_Gebaeude";
            this.textBox_WB_Gebaeude.Size = new System.Drawing.Size(79, 25);
            this.textBox_WB_Gebaeude.TabIndex = 71;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.Black;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.label3.ForeColor = System.Drawing.Color.White;
            this.label3.Location = new System.Drawing.Point(298, 123);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(35, 13);
            this.label3.TabIndex = 70;
            this.label3.Text = "MWh";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label4.ForeColor = System.Drawing.Color.Black;
            this.label4.Location = new System.Drawing.Point(49, 123);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(145, 19);
            this.label4.TabIndex = 69;
            this.label4.Text = "Wärmebedarf Prozess:";
            // 
            // textBox_WB_Prozess
            // 
            this.textBox_WB_Prozess.Location = new System.Drawing.Point(209, 120);
            this.textBox_WB_Prozess.Name = "textBox_WB_Prozess";
            this.textBox_WB_Prozess.Size = new System.Drawing.Size(79, 25);
            this.textBox_WB_Prozess.TabIndex = 68;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Black;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(298, 60);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 67;
            this.label1.Text = "MWh";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label2.ForeColor = System.Drawing.Color.Black;
            this.label2.Location = new System.Drawing.Point(45, 57);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(149, 19);
            this.label2.TabIndex = 66;
            this.label2.Text = "Externer Wärmebedarf:";
            // 
            // textBox_WB_Extern
            // 
            this.textBox_WB_Extern.Location = new System.Drawing.Point(209, 54);
            this.textBox_WB_Extern.Name = "textBox_WB_Extern";
            this.textBox_WB_Extern.Size = new System.Drawing.Size(79, 25);
            this.textBox_WB_Extern.TabIndex = 65;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.BackColor = System.Drawing.Color.Black;
            this.label15.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.label15.ForeColor = System.Drawing.Color.White;
            this.label15.Location = new System.Drawing.Point(298, 195);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(35, 13);
            this.label15.TabIndex = 64;
            this.label15.Text = "MWh";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label16.ForeColor = System.Drawing.Color.DarkRed;
            this.label16.Location = new System.Drawing.Point(36, 191);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(158, 19);
            this.label16.TabIndex = 63;
            this.label16.Text = "Gesamter Wärmebedarf:";
            // 
            // textBox_WB_Gesamt
            // 
            this.textBox_WB_Gesamt.ForeColor = System.Drawing.Color.DarkRed;
            this.textBox_WB_Gesamt.Location = new System.Drawing.Point(209, 189);
            this.textBox_WB_Gesamt.Name = "textBox_WB_Gesamt";
            this.textBox_WB_Gesamt.Size = new System.Drawing.Size(79, 25);
            this.textBox_WB_Gesamt.TabIndex = 62;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.BackColor = System.Drawing.Color.Black;
            this.label14.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.label14.ForeColor = System.Drawing.Color.White;
            this.label14.Location = new System.Drawing.Point(298, 89);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(24, 13);
            this.label14.TabIndex = 61;
            this.label14.Text = "kW";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label13.ForeColor = System.Drawing.Color.Black;
            this.label13.Location = new System.Drawing.Point(85, 89);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(109, 19);
            this.label13.TabIndex = 60;
            this.label13.Text = "max. Wärmelast:";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label12.ForeColor = System.Drawing.Color.Black;
            this.label12.Location = new System.Drawing.Point(106, 151);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(88, 19);
            this.label12.TabIndex = 59;
            this.label12.Text = "Netzverluste:";
            // 
            // textBox_Netzverluste
            // 
            this.textBox_Netzverluste.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.textBox_Netzverluste.Location = new System.Drawing.Point(209, 151);
            this.textBox_Netzverluste.Name = "textBox_Netzverluste";
            this.textBox_Netzverluste.Size = new System.Drawing.Size(79, 22);
            this.textBox_Netzverluste.TabIndex = 58;
            this.textBox_Netzverluste.Text = "10";
            // 
            // textBox_MaxWaermelast
            // 
            this.textBox_MaxWaermelast.Location = new System.Drawing.Point(209, 86);
            this.textBox_MaxWaermelast.Name = "textBox_MaxWaermelast";
            this.textBox_MaxWaermelast.Size = new System.Drawing.Size(79, 25);
            this.textBox_MaxWaermelast.TabIndex = 57;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.groupBox1);
            this.tabPage2.Location = new System.Drawing.Point(4, 26);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(520, 235);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Prozesswärme monatlich";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
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
            this.groupBox1.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.groupBox1.Location = new System.Drawing.Point(27, 16);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(466, 196);
            this.groupBox1.TabIndex = 39;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Prozesswärme monatlicher Verlauf:";
            // 
            // Label27
            // 
            this.Label27.AutoSize = true;
            this.Label27.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Label27.Location = new System.Drawing.Point(18, 24);
            this.Label27.Name = "Label27";
            this.Label27.Size = new System.Drawing.Size(44, 13);
            this.Label27.TabIndex = 38;
            this.Label27.Text = "Januar:";
            this.Label27.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Monat_1
            // 
            this.Monat_1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Monat_1.Enabled = false;
            this.Monat_1.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Monat_1.Location = new System.Drawing.Point(78, 23);
            this.Monat_1.Margin = new System.Windows.Forms.Padding(5);
            this.Monat_1.Name = "Monat_1";
            this.Monat_1.Size = new System.Drawing.Size(84, 22);
            this.Monat_1.TabIndex = 39;
            // 
            // Label28
            // 
            this.Label28.AutoSize = true;
            this.Label28.BackColor = System.Drawing.Color.Black;
            this.Label28.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Label28.ForeColor = System.Drawing.Color.White;
            this.Label28.Location = new System.Drawing.Point(170, 25);
            this.Label28.Name = "Label28";
            this.Label28.Size = new System.Drawing.Size(35, 13);
            this.Label28.TabIndex = 40;
            this.Label28.Text = "MWh";
            this.Label28.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Monat_7
            // 
            this.Monat_7.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Monat_7.Enabled = false;
            this.Monat_7.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Monat_7.Location = new System.Drawing.Point(318, 24);
            this.Monat_7.Margin = new System.Windows.Forms.Padding(5);
            this.Monat_7.Name = "Monat_7";
            this.Monat_7.Size = new System.Drawing.Size(84, 22);
            this.Monat_7.TabIndex = 41;
            // 
            // Label31
            // 
            this.Label31.AutoSize = true;
            this.Label31.BackColor = System.Drawing.Color.Black;
            this.Label31.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Label31.ForeColor = System.Drawing.Color.White;
            this.Label31.Location = new System.Drawing.Point(410, 26);
            this.Label31.Name = "Label31";
            this.Label31.Size = new System.Drawing.Size(35, 13);
            this.Label31.TabIndex = 42;
            this.Label31.Text = "MWh";
            this.Label31.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label33
            // 
            this.Label33.AutoSize = true;
            this.Label33.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Label33.Location = new System.Drawing.Point(12, 50);
            this.Label33.Name = "Label33";
            this.Label33.Size = new System.Drawing.Size(50, 13);
            this.Label33.TabIndex = 43;
            this.Label33.Text = "Februar:";
            this.Label33.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Monat_2
            // 
            this.Monat_2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Monat_2.Enabled = false;
            this.Monat_2.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Monat_2.Location = new System.Drawing.Point(78, 50);
            this.Monat_2.Margin = new System.Windows.Forms.Padding(5);
            this.Monat_2.Name = "Monat_2";
            this.Monat_2.Size = new System.Drawing.Size(84, 22);
            this.Monat_2.TabIndex = 44;
            // 
            // Label34
            // 
            this.Label34.AutoSize = true;
            this.Label34.BackColor = System.Drawing.Color.Black;
            this.Label34.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Label34.ForeColor = System.Drawing.Color.White;
            this.Label34.Location = new System.Drawing.Point(170, 52);
            this.Label34.Name = "Label34";
            this.Label34.Size = new System.Drawing.Size(35, 13);
            this.Label34.TabIndex = 45;
            this.Label34.Text = "MWh";
            this.Label34.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Monat_8
            // 
            this.Monat_8.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Monat_8.Enabled = false;
            this.Monat_8.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Monat_8.Location = new System.Drawing.Point(318, 51);
            this.Monat_8.Margin = new System.Windows.Forms.Padding(5);
            this.Monat_8.Name = "Monat_8";
            this.Monat_8.Size = new System.Drawing.Size(84, 22);
            this.Monat_8.TabIndex = 46;
            // 
            // Label35
            // 
            this.Label35.AutoSize = true;
            this.Label35.BackColor = System.Drawing.Color.Black;
            this.Label35.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Label35.ForeColor = System.Drawing.Color.White;
            this.Label35.Location = new System.Drawing.Point(410, 52);
            this.Label35.Name = "Label35";
            this.Label35.Size = new System.Drawing.Size(35, 13);
            this.Label35.TabIndex = 47;
            this.Label35.Text = "MWh";
            this.Label35.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label38
            // 
            this.Label38.AutoSize = true;
            this.Label38.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Label38.Location = new System.Drawing.Point(27, 79);
            this.Label38.Name = "Label38";
            this.Label38.Size = new System.Drawing.Size(35, 13);
            this.Label38.TabIndex = 48;
            this.Label38.Text = "März:";
            this.Label38.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Monat_3
            // 
            this.Monat_3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Monat_3.Enabled = false;
            this.Monat_3.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Monat_3.Location = new System.Drawing.Point(78, 78);
            this.Monat_3.Margin = new System.Windows.Forms.Padding(5);
            this.Monat_3.Name = "Monat_3";
            this.Monat_3.Size = new System.Drawing.Size(84, 22);
            this.Monat_3.TabIndex = 49;
            // 
            // Label39
            // 
            this.Label39.AutoSize = true;
            this.Label39.BackColor = System.Drawing.Color.Black;
            this.Label39.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Label39.ForeColor = System.Drawing.Color.White;
            this.Label39.Location = new System.Drawing.Point(170, 80);
            this.Label39.Name = "Label39";
            this.Label39.Size = new System.Drawing.Size(35, 13);
            this.Label39.TabIndex = 50;
            this.Label39.Text = "MWh";
            this.Label39.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Monat_9
            // 
            this.Monat_9.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Monat_9.Enabled = false;
            this.Monat_9.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Monat_9.Location = new System.Drawing.Point(318, 79);
            this.Monat_9.Margin = new System.Windows.Forms.Padding(5);
            this.Monat_9.Name = "Monat_9";
            this.Monat_9.Size = new System.Drawing.Size(84, 22);
            this.Monat_9.TabIndex = 51;
            // 
            // Label40
            // 
            this.Label40.AutoSize = true;
            this.Label40.BackColor = System.Drawing.Color.Black;
            this.Label40.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Label40.ForeColor = System.Drawing.Color.White;
            this.Label40.Location = new System.Drawing.Point(410, 81);
            this.Label40.Name = "Label40";
            this.Label40.Size = new System.Drawing.Size(35, 13);
            this.Label40.TabIndex = 52;
            this.Label40.Text = "MWh";
            this.Label40.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Monat_4
            // 
            this.Monat_4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Monat_4.Enabled = false;
            this.Monat_4.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Monat_4.Location = new System.Drawing.Point(78, 106);
            this.Monat_4.Margin = new System.Windows.Forms.Padding(5);
            this.Monat_4.Name = "Monat_4";
            this.Monat_4.Size = new System.Drawing.Size(84, 22);
            this.Monat_4.TabIndex = 53;
            // 
            // Label43
            // 
            this.Label43.AutoSize = true;
            this.Label43.BackColor = System.Drawing.Color.Black;
            this.Label43.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Label43.ForeColor = System.Drawing.Color.White;
            this.Label43.Location = new System.Drawing.Point(170, 107);
            this.Label43.Name = "Label43";
            this.Label43.Size = new System.Drawing.Size(35, 13);
            this.Label43.TabIndex = 54;
            this.Label43.Text = "MWh";
            this.Label43.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Monat_10
            // 
            this.Monat_10.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Monat_10.Enabled = false;
            this.Monat_10.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Monat_10.Location = new System.Drawing.Point(318, 107);
            this.Monat_10.Margin = new System.Windows.Forms.Padding(5);
            this.Monat_10.Name = "Monat_10";
            this.Monat_10.Size = new System.Drawing.Size(84, 22);
            this.Monat_10.TabIndex = 55;
            // 
            // Label44
            // 
            this.Label44.AutoSize = true;
            this.Label44.BackColor = System.Drawing.Color.Black;
            this.Label44.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Label44.ForeColor = System.Drawing.Color.White;
            this.Label44.Location = new System.Drawing.Point(410, 108);
            this.Label44.Name = "Label44";
            this.Label44.Size = new System.Drawing.Size(35, 13);
            this.Label44.TabIndex = 56;
            this.Label44.Text = "MWh";
            this.Label44.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label46
            // 
            this.Label46.AutoSize = true;
            this.Label46.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Label46.Location = new System.Drawing.Point(33, 134);
            this.Label46.Name = "Label46";
            this.Label46.Size = new System.Drawing.Size(29, 13);
            this.Label46.TabIndex = 57;
            this.Label46.Text = "Mai:";
            this.Label46.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Monat_5
            // 
            this.Monat_5.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Monat_5.Enabled = false;
            this.Monat_5.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Monat_5.Location = new System.Drawing.Point(78, 134);
            this.Monat_5.Margin = new System.Windows.Forms.Padding(5);
            this.Monat_5.Name = "Monat_5";
            this.Monat_5.Size = new System.Drawing.Size(84, 22);
            this.Monat_5.TabIndex = 58;
            // 
            // Label47
            // 
            this.Label47.AutoSize = true;
            this.Label47.BackColor = System.Drawing.Color.Black;
            this.Label47.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Label47.ForeColor = System.Drawing.Color.White;
            this.Label47.Location = new System.Drawing.Point(170, 136);
            this.Label47.Name = "Label47";
            this.Label47.Size = new System.Drawing.Size(35, 13);
            this.Label47.TabIndex = 59;
            this.Label47.Text = "MWh";
            this.Label47.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Monat_11
            // 
            this.Monat_11.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Monat_11.Enabled = false;
            this.Monat_11.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Monat_11.Location = new System.Drawing.Point(318, 135);
            this.Monat_11.Margin = new System.Windows.Forms.Padding(5);
            this.Monat_11.Name = "Monat_11";
            this.Monat_11.Size = new System.Drawing.Size(84, 22);
            this.Monat_11.TabIndex = 60;
            // 
            // Label48
            // 
            this.Label48.AutoSize = true;
            this.Label48.BackColor = System.Drawing.Color.Black;
            this.Label48.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Label48.ForeColor = System.Drawing.Color.White;
            this.Label48.Location = new System.Drawing.Point(410, 136);
            this.Label48.Name = "Label48";
            this.Label48.Size = new System.Drawing.Size(35, 13);
            this.Label48.TabIndex = 61;
            this.Label48.Text = "MWh";
            this.Label48.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label51
            // 
            this.Label51.AutoSize = true;
            this.Label51.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Label51.Location = new System.Drawing.Point(31, 164);
            this.Label51.Name = "Label51";
            this.Label51.Size = new System.Drawing.Size(31, 13);
            this.Label51.TabIndex = 62;
            this.Label51.Text = "Juni:";
            this.Label51.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Monat_6
            // 
            this.Monat_6.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Monat_6.Enabled = false;
            this.Monat_6.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Monat_6.Location = new System.Drawing.Point(78, 164);
            this.Monat_6.Margin = new System.Windows.Forms.Padding(5);
            this.Monat_6.Name = "Monat_6";
            this.Monat_6.Size = new System.Drawing.Size(84, 22);
            this.Monat_6.TabIndex = 63;
            // 
            // Label52
            // 
            this.Label52.AutoSize = true;
            this.Label52.BackColor = System.Drawing.Color.Black;
            this.Label52.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Label52.ForeColor = System.Drawing.Color.White;
            this.Label52.Location = new System.Drawing.Point(170, 166);
            this.Label52.Name = "Label52";
            this.Label52.Size = new System.Drawing.Size(35, 13);
            this.Label52.TabIndex = 64;
            this.Label52.Text = "MWh";
            this.Label52.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Monat_12
            // 
            this.Monat_12.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Monat_12.Enabled = false;
            this.Monat_12.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Monat_12.Location = new System.Drawing.Point(318, 164);
            this.Monat_12.Margin = new System.Windows.Forms.Padding(5);
            this.Monat_12.Name = "Monat_12";
            this.Monat_12.Size = new System.Drawing.Size(84, 22);
            this.Monat_12.TabIndex = 65;
            // 
            // Label53
            // 
            this.Label53.AutoSize = true;
            this.Label53.BackColor = System.Drawing.Color.Black;
            this.Label53.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Label53.ForeColor = System.Drawing.Color.White;
            this.Label53.Location = new System.Drawing.Point(410, 166);
            this.Label53.Name = "Label53";
            this.Label53.Size = new System.Drawing.Size(35, 13);
            this.Label53.TabIndex = 66;
            this.Label53.Text = "MWh";
            this.Label53.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label42
            // 
            this.Label42.AutoSize = true;
            this.Label42.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Label42.Location = new System.Drawing.Point(28, 106);
            this.Label42.Name = "Label42";
            this.Label42.Size = new System.Drawing.Size(34, 13);
            this.Label42.TabIndex = 67;
            this.Label42.Text = "April:";
            this.Label42.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label54
            // 
            this.Label54.AutoSize = true;
            this.Label54.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Label54.Location = new System.Drawing.Point(279, 26);
            this.Label54.Name = "Label54";
            this.Label54.Size = new System.Drawing.Size(27, 13);
            this.Label54.TabIndex = 68;
            this.Label54.Text = "Juli:";
            this.Label54.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label55
            // 
            this.Label55.AutoSize = true;
            this.Label55.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Label55.Location = new System.Drawing.Point(259, 52);
            this.Label55.Name = "Label55";
            this.Label55.Size = new System.Drawing.Size(47, 13);
            this.Label55.TabIndex = 69;
            this.Label55.Text = "August:";
            this.Label55.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label56
            // 
            this.Label56.AutoSize = true;
            this.Label56.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Label56.Location = new System.Drawing.Point(241, 81);
            this.Label56.Name = "Label56";
            this.Label56.Size = new System.Drawing.Size(65, 13);
            this.Label56.TabIndex = 70;
            this.Label56.Text = "Seprember:";
            this.Label56.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label57
            // 
            this.Label57.AutoSize = true;
            this.Label57.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Label57.Location = new System.Drawing.Point(244, 137);
            this.Label57.Name = "Label57";
            this.Label57.Size = new System.Drawing.Size(62, 13);
            this.Label57.TabIndex = 71;
            this.Label57.Text = "November:";
            this.Label57.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label58
            // 
            this.Label58.AutoSize = true;
            this.Label58.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Label58.Location = new System.Drawing.Point(245, 164);
            this.Label58.Name = "Label58";
            this.Label58.Size = new System.Drawing.Size(61, 13);
            this.Label58.TabIndex = 72;
            this.Label58.Text = "Dezember:";
            this.Label58.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label59
            // 
            this.Label59.AutoSize = true;
            this.Label59.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Label59.Location = new System.Drawing.Point(253, 108);
            this.Label59.Name = "Label59";
            this.Label59.Size = new System.Drawing.Size(53, 13);
            this.Label59.TabIndex = 73;
            this.Label59.Text = "Oktober:";
            this.Label59.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.chart1);
            this.tabPage3.Location = new System.Drawing.Point(4, 26);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(520, 235);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Grafik Prozesswärme";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // chart1
            // 
            chartArea1.AxisX.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chartArea1.AxisX.Title = "Monat";
            chartArea1.AxisX.TitleFont = new System.Drawing.Font("Segoe UI", 10F);
            chartArea1.AxisY.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chartArea1.AxisY.Title = "Prozesswärme [MWh]";
            chartArea1.AxisY.TitleFont = new System.Drawing.Font("Segoe UI", 10F);
            chartArea1.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chartArea1.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            this.chart1.Legends.Add(legend1);
            this.chart1.Location = new System.Drawing.Point(16, 13);
            this.chart1.Name = "chart1";
            series1.ChartArea = "ChartArea1";
            series1.IsVisibleInLegend = false;
            series1.Legend = "Legend1";
            series1.Name = "Series1";
            this.chart1.Series.Add(series1);
            this.chart1.Size = new System.Drawing.Size(368, 209);
            this.chart1.TabIndex = 0;
            this.chart1.Text = "Grafik Prozesswärme";
            // 
            // Form_ErgProzesswaerme
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(568, 346);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.btn_Hilfe);
            this.Controls.Add(this.btn_OK);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Form_ErgProzesswaerme";
            this.Text = "Simulation Ergebnisse";
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
private System.Windows.Forms.Label label_Netzverluste_Einheit;
private System.Windows.Forms.Label label5;
private System.Windows.Forms.Label label6;
private System.Windows.Forms.TextBox textBox_WB_Gebaeude;
private System.Windows.Forms.Label label3;
private System.Windows.Forms.Label label4;
private System.Windows.Forms.TextBox textBox_WB_Prozess;
private System.Windows.Forms.Label label1;
private System.Windows.Forms.Label label2;
private System.Windows.Forms.TextBox textBox_WB_Extern;
private System.Windows.Forms.Label label15;
private System.Windows.Forms.Label label16;
private System.Windows.Forms.TextBox textBox_WB_Gesamt;
private System.Windows.Forms.Label label14;
private System.Windows.Forms.Label label13;
private System.Windows.Forms.Label label12;
private System.Windows.Forms.TextBox textBox_Netzverluste;
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