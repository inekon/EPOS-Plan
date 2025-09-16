namespace WindowsFormsApplication1
{
    partial class FormMain
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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Title title2 = new System.Windows.Forms.DataVisualization.Charting.Title();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.button_Beenden = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox_Projekt = new System.Windows.Forms.TextBox();
            this.textBox_Bearbeiter = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox_Kunde = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.textBox_Datum = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.tabControl_Komponenten = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.label7 = new System.Windows.Forms.Label();
            this.listView_WP_Ref = new System.Windows.Forms.ListView();
            this.listView_WP = new System.Windows.Forms.ListView();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.label10 = new System.Windows.Forms.Label();
            this.listView_SP_REF = new System.Windows.Forms.ListView();
            this.listView_SP = new System.Windows.Forms.ListView();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.tabPage6 = new System.Windows.Forms.TabPage();
            this.listView_Heizkessel = new System.Windows.Forms.ListView();
            this.tabPage5 = new System.Windows.Forms.TabPage();
            this.listView_Gebaeude = new System.Windows.Forms.ListView();
            this.comboBox_Klima = new System.Windows.Forms.ComboBox();
            this.label11 = new System.Windows.Forms.Label();
            this.tabPage8 = new System.Windows.Forms.TabPage();
            this.tabPage7 = new System.Windows.Forms.TabPage();
            this.textBox_MaxWaermelast = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.textBox_Netzverluste = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.comboBox_NetzvEinheit = new System.Windows.Forms.ComboBox();
            this.tabControl_Simulation = new System.Windows.Forms.TabControl();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.btn_SimulSpeichern = new System.Windows.Forms.Button();
            this.tabPage9 = new System.Windows.Forms.TabPage();
            this.listView_WaermebedatfExtern = new System.Windows.Forms.ListView();
            this.label15 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.textBox_Gesamt_Waermebedarf = new System.Windows.Forms.TextBox();
            this.tabControl_Komponenten.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage6.SuspendLayout();
            this.tabPage5.SuspendLayout();
            this.tabPage7.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.tabControl_Simulation.SuspendLayout();
            this.tabPage9.SuspendLayout();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.White;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label2.ForeColor = System.Drawing.Color.Black;
            this.label2.Location = new System.Drawing.Point(12, 143);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(74, 19);
            this.label2.TabIndex = 3;
            this.label2.Text = "Klimazone:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.White;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label3.ForeColor = System.Drawing.Color.Black;
            this.label3.Location = new System.Drawing.Point(12, 113);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(110, 19);
            this.label3.TabIndex = 5;
            this.label3.Text = "Projekt geöffnet:";
            // 
            // button_Beenden
            // 
            this.button_Beenden.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.button_Beenden.ForeColor = System.Drawing.Color.Black;
            this.button_Beenden.Location = new System.Drawing.Point(1052, 754);
            this.button_Beenden.Name = "button_Beenden";
            this.button_Beenden.Size = new System.Drawing.Size(168, 35);
            this.button_Beenden.TabIndex = 6;
            this.button_Beenden.Text = "Projekt schließen";
            this.button_Beenden.UseVisualStyleBackColor = true;
            this.button_Beenden.Click += new System.EventHandler(this.button_Beenden_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Black;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(28, 61);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(173, 21);
            this.label1.TabIndex = 11;
            this.label1.Text = "Projekteigenschaften";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.BackColor = System.Drawing.Color.Black;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Location = new System.Drawing.Point(501, 61);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(109, 21);
            this.label4.TabIndex = 12;
            this.label4.Text = "Projektdaten";
            // 
            // textBox_Projekt
            // 
            this.textBox_Projekt.BackColor = System.Drawing.Color.White;
            this.textBox_Projekt.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Projekt.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Projekt.Location = new System.Drawing.Point(141, 112);
            this.textBox_Projekt.Name = "textBox_Projekt";
            this.textBox_Projekt.Size = new System.Drawing.Size(304, 25);
            this.textBox_Projekt.TabIndex = 13;
            // 
            // textBox_Bearbeiter
            // 
            this.textBox_Bearbeiter.BackColor = System.Drawing.Color.White;
            this.textBox_Bearbeiter.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Bearbeiter.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Bearbeiter.ForeColor = System.Drawing.Color.Black;
            this.textBox_Bearbeiter.Location = new System.Drawing.Point(141, 170);
            this.textBox_Bearbeiter.Name = "textBox_Bearbeiter";
            this.textBox_Bearbeiter.ReadOnly = true;
            this.textBox_Bearbeiter.Size = new System.Drawing.Size(304, 25);
            this.textBox_Bearbeiter.TabIndex = 15;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.BackColor = System.Drawing.Color.White;
            this.label5.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label5.ForeColor = System.Drawing.Color.Black;
            this.label5.Location = new System.Drawing.Point(12, 172);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(74, 19);
            this.label5.TabIndex = 14;
            this.label5.Text = "Bearbeiter:";
            // 
            // textBox_Kunde
            // 
            this.textBox_Kunde.BackColor = System.Drawing.Color.White;
            this.textBox_Kunde.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Kunde.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kunde.ForeColor = System.Drawing.Color.Black;
            this.textBox_Kunde.Location = new System.Drawing.Point(141, 199);
            this.textBox_Kunde.Name = "textBox_Kunde";
            this.textBox_Kunde.ReadOnly = true;
            this.textBox_Kunde.Size = new System.Drawing.Size(304, 25);
            this.textBox_Kunde.TabIndex = 17;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.BackColor = System.Drawing.Color.White;
            this.label6.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label6.ForeColor = System.Drawing.Color.Black;
            this.label6.Location = new System.Drawing.Point(12, 201);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(51, 19);
            this.label6.TabIndex = 16;
            this.label6.Text = "Kunde:";
            // 
            // textBox_Datum
            // 
            this.textBox_Datum.BackColor = System.Drawing.Color.White;
            this.textBox_Datum.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Datum.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Datum.ForeColor = System.Drawing.Color.Black;
            this.textBox_Datum.Location = new System.Drawing.Point(141, 228);
            this.textBox_Datum.Name = "textBox_Datum";
            this.textBox_Datum.ReadOnly = true;
            this.textBox_Datum.Size = new System.Drawing.Size(116, 25);
            this.textBox_Datum.TabIndex = 19;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.BackColor = System.Drawing.Color.White;
            this.label8.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label8.ForeColor = System.Drawing.Color.Black;
            this.label8.Location = new System.Drawing.Point(12, 230);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(119, 19);
            this.label8.TabIndex = 18;
            this.label8.Text = "Änderungsdatum:";
            // 
            // textBox_Beschreibung
            // 
            this.textBox_Beschreibung.BackColor = System.Drawing.Color.White;
            this.textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Beschreibung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Beschreibung.ForeColor = System.Drawing.Color.Black;
            this.textBox_Beschreibung.Location = new System.Drawing.Point(141, 257);
            this.textBox_Beschreibung.Multiline = true;
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            this.textBox_Beschreibung.ReadOnly = true;
            this.textBox_Beschreibung.Size = new System.Drawing.Size(304, 53);
            this.textBox_Beschreibung.TabIndex = 21;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.BackColor = System.Drawing.Color.White;
            this.label9.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label9.ForeColor = System.Drawing.Color.Black;
            this.label9.Location = new System.Drawing.Point(12, 259);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(94, 19);
            this.label9.TabIndex = 20;
            this.label9.Text = "Beschreibung:";
            // 
            // tabControl_Komponenten
            // 
            this.tabControl_Komponenten.Controls.Add(this.tabPage5);
            this.tabControl_Komponenten.Controls.Add(this.tabPage9);
            this.tabControl_Komponenten.Controls.Add(this.tabPage1);
            this.tabControl_Komponenten.Controls.Add(this.tabPage2);
            this.tabControl_Komponenten.Controls.Add(this.tabPage3);
            this.tabControl_Komponenten.Controls.Add(this.tabPage4);
            this.tabControl_Komponenten.Controls.Add(this.tabPage6);
            this.tabControl_Komponenten.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.tabControl_Komponenten.Location = new System.Drawing.Point(495, 102);
            this.tabControl_Komponenten.Multiline = true;
            this.tabControl_Komponenten.Name = "tabControl_Komponenten";
            this.tabControl_Komponenten.SelectedIndex = 0;
            this.tabControl_Komponenten.Size = new System.Drawing.Size(662, 279);
            this.tabControl_Komponenten.TabIndex = 22;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label7);
            this.tabPage1.Controls.Add(this.listView_WP_Ref);
            this.tabPage1.Controls.Add(this.listView_WP);
            this.tabPage1.Location = new System.Drawing.Point(4, 26);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(0);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Size = new System.Drawing.Size(654, 249);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Wärmepumpen";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.ForeColor = System.Drawing.Color.Black;
            this.label7.Location = new System.Drawing.Point(5, 131);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(104, 19);
            this.label7.TabIndex = 12;
            this.label7.Text = "Referenzsystem";
            // 
            // listView_WP_Ref
            // 
            this.listView_WP_Ref.BackColor = System.Drawing.Color.White;
            this.listView_WP_Ref.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.listView_WP_Ref.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.listView_WP_Ref.FullRowSelect = true;
            this.listView_WP_Ref.GridLines = true;
            this.listView_WP_Ref.HideSelection = false;
            this.listView_WP_Ref.Location = new System.Drawing.Point(3, 147);
            this.listView_WP_Ref.MultiSelect = false;
            this.listView_WP_Ref.Name = "listView_WP_Ref";
            this.listView_WP_Ref.Size = new System.Drawing.Size(597, 90);
            this.listView_WP_Ref.TabIndex = 11;
            this.listView_WP_Ref.UseCompatibleStateImageBehavior = false;
            // 
            // listView_WP
            // 
            this.listView_WP.BackColor = System.Drawing.Color.White;
            this.listView_WP.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.listView_WP.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.listView_WP.FullRowSelect = true;
            this.listView_WP.GridLines = true;
            this.listView_WP.HideSelection = false;
            this.listView_WP.Location = new System.Drawing.Point(3, 3);
            this.listView_WP.MultiSelect = false;
            this.listView_WP.Name = "listView_WP";
            this.listView_WP.Size = new System.Drawing.Size(597, 97);
            this.listView_WP.TabIndex = 9;
            this.listView_WP.UseCompatibleStateImageBehavior = false;
            this.listView_WP.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listView_WP_MouseDoubleClick);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.label10);
            this.tabPage2.Controls.Add(this.listView_SP_REF);
            this.tabPage2.Controls.Add(this.listView_SP);
            this.tabPage2.Location = new System.Drawing.Point(4, 26);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(654, 249);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Stromspeicher";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.ForeColor = System.Drawing.Color.Black;
            this.label10.Location = new System.Drawing.Point(6, 130);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(104, 19);
            this.label10.TabIndex = 13;
            this.label10.Text = "Referenzsystem";
            // 
            // listView_SP_REF
            // 
            this.listView_SP_REF.BackColor = System.Drawing.Color.White;
            this.listView_SP_REF.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.listView_SP_REF.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.listView_SP_REF.FullRowSelect = true;
            this.listView_SP_REF.GridLines = true;
            this.listView_SP_REF.HideSelection = false;
            this.listView_SP_REF.Location = new System.Drawing.Point(6, 146);
            this.listView_SP_REF.MultiSelect = false;
            this.listView_SP_REF.Name = "listView_SP_REF";
            this.listView_SP_REF.Size = new System.Drawing.Size(514, 97);
            this.listView_SP_REF.TabIndex = 11;
            this.listView_SP_REF.UseCompatibleStateImageBehavior = false;
            // 
            // listView_SP
            // 
            this.listView_SP.BackColor = System.Drawing.Color.White;
            this.listView_SP.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.listView_SP.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.listView_SP.FullRowSelect = true;
            this.listView_SP.GridLines = true;
            this.listView_SP.HideSelection = false;
            this.listView_SP.Location = new System.Drawing.Point(6, 13);
            this.listView_SP.MultiSelect = false;
            this.listView_SP.Name = "listView_SP";
            this.listView_SP.Size = new System.Drawing.Size(514, 97);
            this.listView_SP.TabIndex = 10;
            this.listView_SP.UseCompatibleStateImageBehavior = false;
            // 
            // tabPage3
            // 
            this.tabPage3.Location = new System.Drawing.Point(4, 26);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(654, 249);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Solar";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // tabPage4
            // 
            this.tabPage4.Location = new System.Drawing.Point(4, 26);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(654, 249);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "PV";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // tabPage6
            // 
            this.tabPage6.Controls.Add(this.listView_Heizkessel);
            this.tabPage6.Location = new System.Drawing.Point(4, 26);
            this.tabPage6.Name = "tabPage6";
            this.tabPage6.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage6.Size = new System.Drawing.Size(654, 249);
            this.tabPage6.TabIndex = 5;
            this.tabPage6.Text = "Heizkessel Ref. System";
            this.tabPage6.UseVisualStyleBackColor = true;
            // 
            // listView_Heizkessel
            // 
            this.listView_Heizkessel.BackColor = System.Drawing.Color.White;
            this.listView_Heizkessel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.listView_Heizkessel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.listView_Heizkessel.FullRowSelect = true;
            this.listView_Heizkessel.GridLines = true;
            this.listView_Heizkessel.HideSelection = false;
            this.listView_Heizkessel.Location = new System.Drawing.Point(6, 13);
            this.listView_Heizkessel.MultiSelect = false;
            this.listView_Heizkessel.Name = "listView_Heizkessel";
            this.listView_Heizkessel.Size = new System.Drawing.Size(514, 97);
            this.listView_Heizkessel.TabIndex = 12;
            this.listView_Heizkessel.UseCompatibleStateImageBehavior = false;
            // 
            // tabPage5
            // 
            this.tabPage5.Controls.Add(this.listView_Gebaeude);
            this.tabPage5.Location = new System.Drawing.Point(4, 26);
            this.tabPage5.Name = "tabPage5";
            this.tabPage5.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage5.Size = new System.Drawing.Size(654, 249);
            this.tabPage5.TabIndex = 6;
            this.tabPage5.Text = "Gebäude";
            this.tabPage5.UseVisualStyleBackColor = true;
            // 
            // listView_Gebaeude
            // 
            this.listView_Gebaeude.BackColor = System.Drawing.Color.White;
            this.listView_Gebaeude.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.listView_Gebaeude.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.listView_Gebaeude.FullRowSelect = true;
            this.listView_Gebaeude.GridLines = true;
            this.listView_Gebaeude.HideSelection = false;
            this.listView_Gebaeude.Location = new System.Drawing.Point(16, 17);
            this.listView_Gebaeude.MultiSelect = false;
            this.listView_Gebaeude.Name = "listView_Gebaeude";
            this.listView_Gebaeude.Size = new System.Drawing.Size(581, 133);
            this.listView_Gebaeude.TabIndex = 14;
            this.listView_Gebaeude.UseCompatibleStateImageBehavior = false;
            // 
            // comboBox_Klima
            // 
            this.comboBox_Klima.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.comboBox_Klima.FormattingEnabled = true;
            this.comboBox_Klima.Location = new System.Drawing.Point(141, 141);
            this.comboBox_Klima.Name = "comboBox_Klima";
            this.comboBox_Klima.Size = new System.Drawing.Size(304, 25);
            this.comboBox_Klima.TabIndex = 23;
            this.comboBox_Klima.SelectedIndexChanged += new System.EventHandler(this.comboBox_Klima_SelectedIndexChanged);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.BackColor = System.Drawing.Color.Black;
            this.label11.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.ForeColor = System.Drawing.Color.White;
            this.label11.Location = new System.Drawing.Point(501, 397);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(94, 21);
            this.label11.TabIndex = 28;
            this.label11.Text = "Simulation";
            // 
            // tabPage8
            // 
            this.tabPage8.Location = new System.Drawing.Point(4, 26);
            this.tabPage8.Name = "tabPage8";
            this.tabPage8.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage8.Size = new System.Drawing.Size(603, 248);
            this.tabPage8.TabIndex = 1;
            this.tabPage8.Text = "Prozesswärme";
            this.tabPage8.UseVisualStyleBackColor = true;
            // 
            // tabPage7
            // 
            this.tabPage7.Controls.Add(this.label15);
            this.tabPage7.Controls.Add(this.label16);
            this.tabPage7.Controls.Add(this.textBox_Gesamt_Waermebedarf);
            this.tabPage7.Controls.Add(this.btn_SimulSpeichern);
            this.tabPage7.Controls.Add(this.label14);
            this.tabPage7.Controls.Add(this.label13);
            this.tabPage7.Controls.Add(this.comboBox_NetzvEinheit);
            this.tabPage7.Controls.Add(this.label12);
            this.tabPage7.Controls.Add(this.textBox_Netzverluste);
            this.tabPage7.Controls.Add(this.textBox_MaxWaermelast);
            this.tabPage7.Controls.Add(this.chart1);
            this.tabPage7.Controls.Add(this.button1);
            this.tabPage7.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.tabPage7.Location = new System.Drawing.Point(4, 26);
            this.tabPage7.Name = "tabPage7";
            this.tabPage7.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage7.Size = new System.Drawing.Size(603, 303);
            this.tabPage7.TabIndex = 0;
            this.tabPage7.Text = "Wärmebedarf Gebäude";
            this.tabPage7.UseVisualStyleBackColor = true;
            // 
            // textBox_MaxWaermelast
            // 
            this.textBox_MaxWaermelast.Location = new System.Drawing.Point(463, 147);
            this.textBox_MaxWaermelast.Name = "textBox_MaxWaermelast";
            this.textBox_MaxWaermelast.Size = new System.Drawing.Size(60, 25);
            this.textBox_MaxWaermelast.TabIndex = 26;
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.button1.ForeColor = System.Drawing.Color.Black;
            this.button1.Location = new System.Drawing.Point(452, 91);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(134, 28);
            this.button1.TabIndex = 24;
            this.button1.Text = "Simulation Start...";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // chart1
            // 
            chartArea2.AxisX.IsLabelAutoFit = false;
            chartArea2.AxisX.LabelStyle.Font = new System.Drawing.Font("Segoe UI", 10F);
            chartArea2.AxisX.Title = "Jahresstunden";
            chartArea2.AxisX.TitleFont = new System.Drawing.Font("Segoe UI", 10F);
            chartArea2.AxisY.Title = "Wärmelast in % ";
            chartArea2.AxisY.TitleFont = new System.Drawing.Font("Segoe UI", 10F);
            chartArea2.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chartArea2.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea2);
            this.chart1.Location = new System.Drawing.Point(6, 39);
            this.chart1.Name = "chart1";
            series2.ChartArea = "ChartArea1";
            series2.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series2.Name = "Series1";
            this.chart1.Series.Add(series2);
            this.chart1.Size = new System.Drawing.Size(422, 246);
            this.chart1.TabIndex = 25;
            this.chart1.Text = "chart1";
            title2.Name = "Title1";
            title2.Text = "Wärmelast Jahresganglinie (incl. externer Wärmebedarf)";
            this.chart1.Titles.Add(title2);
            // 
            // textBox_Netzverluste
            // 
            this.textBox_Netzverluste.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.textBox_Netzverluste.Location = new System.Drawing.Point(97, 11);
            this.textBox_Netzverluste.Name = "textBox_Netzverluste";
            this.textBox_Netzverluste.Size = new System.Drawing.Size(77, 22);
            this.textBox_Netzverluste.TabIndex = 27;
            this.textBox_Netzverluste.Text = "10";
            this.textBox_Netzverluste.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Netzverluste_Validating);
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.label12.ForeColor = System.Drawing.Color.Black;
            this.label12.Location = new System.Drawing.Point(21, 14);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(70, 13);
            this.label12.TabIndex = 28;
            this.label12.Text = "Netzverluste";
            // 
            // comboBox_NetzvEinheit
            // 
            this.comboBox_NetzvEinheit.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.comboBox_NetzvEinheit.FormattingEnabled = true;
            this.comboBox_NetzvEinheit.Items.AddRange(new object[] {
            "%",
            "kWh/a"});
            this.comboBox_NetzvEinheit.Location = new System.Drawing.Point(180, 11);
            this.comboBox_NetzvEinheit.Name = "comboBox_NetzvEinheit";
            this.comboBox_NetzvEinheit.Size = new System.Drawing.Size(68, 21);
            this.comboBox_NetzvEinheit.TabIndex = 29;
            this.comboBox_NetzvEinheit.Text = "%";
            // 
            // tabControl_Simulation
            // 
            this.tabControl_Simulation.Controls.Add(this.tabPage7);
            this.tabControl_Simulation.Controls.Add(this.tabPage8);
            this.tabControl_Simulation.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.tabControl_Simulation.Location = new System.Drawing.Point(495, 427);
            this.tabControl_Simulation.Name = "tabControl_Simulation";
            this.tabControl_Simulation.SelectedIndex = 0;
            this.tabControl_Simulation.Size = new System.Drawing.Size(611, 333);
            this.tabControl_Simulation.TabIndex = 27;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.label13.ForeColor = System.Drawing.Color.Black;
            this.label13.Location = new System.Drawing.Point(460, 131);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(87, 13);
            this.label13.TabIndex = 30;
            this.label13.Text = "max. Wärmelast";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.BackColor = System.Drawing.Color.Black;
            this.label14.Location = new System.Drawing.Point(540, 150);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(29, 19);
            this.label14.TabIndex = 31;
            this.label14.Text = "kW";
            // 
            // btn_SimulSpeichern
            // 
            this.btn_SimulSpeichern.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_SimulSpeichern.ForeColor = System.Drawing.Color.Black;
            this.btn_SimulSpeichern.Location = new System.Drawing.Point(452, 245);
            this.btn_SimulSpeichern.Name = "btn_SimulSpeichern";
            this.btn_SimulSpeichern.Size = new System.Drawing.Size(134, 40);
            this.btn_SimulSpeichern.TabIndex = 29;
            this.btn_SimulSpeichern.Text = "Ergebnisse Simulation Speichern";
            this.btn_SimulSpeichern.UseVisualStyleBackColor = true;
            this.btn_SimulSpeichern.Click += new System.EventHandler(this.btn_SimulSpeichern_Click);
            // 
            // tabPage9
            // 
            this.tabPage9.Controls.Add(this.listView_WaermebedatfExtern);
            this.tabPage9.Location = new System.Drawing.Point(4, 26);
            this.tabPage9.Name = "tabPage9";
            this.tabPage9.Size = new System.Drawing.Size(654, 249);
            this.tabPage9.TabIndex = 7;
            this.tabPage9.Text = "Wärmebedarf extern";
            this.tabPage9.UseVisualStyleBackColor = true;
            // 
            // listView_WaermebedatfExtern
            // 
            this.listView_WaermebedatfExtern.BackColor = System.Drawing.Color.White;
            this.listView_WaermebedatfExtern.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.listView_WaermebedatfExtern.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.listView_WaermebedatfExtern.FullRowSelect = true;
            this.listView_WaermebedatfExtern.GridLines = true;
            this.listView_WaermebedatfExtern.HideSelection = false;
            this.listView_WaermebedatfExtern.Location = new System.Drawing.Point(22, 17);
            this.listView_WaermebedatfExtern.MultiSelect = false;
            this.listView_WaermebedatfExtern.Name = "listView_WaermebedatfExtern";
            this.listView_WaermebedatfExtern.Size = new System.Drawing.Size(581, 133);
            this.listView_WaermebedatfExtern.TabIndex = 15;
            this.listView_WaermebedatfExtern.UseCompatibleStateImageBehavior = false;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.BackColor = System.Drawing.Color.Black;
            this.label15.Location = new System.Drawing.Point(540, 206);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(35, 19);
            this.label15.TabIndex = 34;
            this.label15.Text = "MW";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.label16.ForeColor = System.Drawing.Color.Black;
            this.label16.Location = new System.Drawing.Point(460, 187);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(128, 13);
            this.label16.TabIndex = 33;
            this.label16.Text = "Gesamter Wärmebedarf";
            // 
            // textBox_Gesamt_Waermebedarf
            // 
            this.textBox_Gesamt_Waermebedarf.Location = new System.Drawing.Point(463, 203);
            this.textBox_Gesamt_Waermebedarf.Name = "textBox_Gesamt_Waermebedarf";
            this.textBox_Gesamt_Waermebedarf.Size = new System.Drawing.Size(60, 25);
            this.textBox_Gesamt_Waermebedarf.TabIndex = 32;
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ClientSize = new System.Drawing.Size(1234, 801);
            this.ControlBox = false;
            this.Controls.Add(this.label11);
            this.Controls.Add(this.tabControl_Simulation);
            this.Controls.Add(this.comboBox_Klima);
            this.Controls.Add(this.tabControl_Komponenten);
            this.Controls.Add(this.textBox_Beschreibung);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.textBox_Datum);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.textBox_Kunde);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.textBox_Bearbeiter);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textBox_Projekt);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.button_Beenden);
            this.Controls.Add(this.label2);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.Color.White;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "FormMain";
            this.Text = "Hauptformular";
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.tabControl_Komponenten.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.tabPage6.ResumeLayout(false);
            this.tabPage5.ResumeLayout(false);
            this.tabPage7.ResumeLayout(false);
            this.tabPage7.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.tabControl_Simulation.ResumeLayout(false);
            this.tabPage9.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button button_Beenden;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox_Projekt;
        private System.Windows.Forms.TextBox textBox_Bearbeiter;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox_Kunde;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBox_Datum;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox textBox_Beschreibung;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TabControl tabControl_Komponenten;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.ListView listView_SP;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.TabPage tabPage6;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.ListView listView_WP_Ref;
        private System.Windows.Forms.ListView listView_WP;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ListView listView_SP_REF;
        private System.Windows.Forms.ListView listView_Heizkessel;
        private System.Windows.Forms.ListView listView_Gebaeude;
        private System.Windows.Forms.TabPage tabPage5;
        private System.Windows.Forms.ComboBox comboBox_Klima;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TabPage tabPage8;
        private System.Windows.Forms.TabPage tabPage7;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.ComboBox comboBox_NetzvEinheit;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox textBox_Netzverluste;
        private System.Windows.Forms.TextBox textBox_MaxWaermelast;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TabControl tabControl_Simulation;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Button btn_SimulSpeichern;
        private System.Windows.Forms.TabPage tabPage9;
        private System.Windows.Forms.ListView listView_WaermebedatfExtern;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TextBox textBox_Gesamt_Waermebedarf;
    }
}