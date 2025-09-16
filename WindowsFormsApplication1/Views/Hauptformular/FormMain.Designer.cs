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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Title title1 = new System.Windows.Forms.DataVisualization.Charting.Title();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
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
            this.tabPage5 = new System.Windows.Forms.TabPage();
            this.listView_Gebaeude = new System.Windows.Forms.ListView();
            this.tabPage9 = new System.Windows.Forms.TabPage();
            this.listView_WaermebedarfExtern = new System.Windows.Forms.ListView();
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
            this.tabPage10 = new System.Windows.Forms.TabPage();
            this.listView_Prozesswaerme = new System.Windows.Forms.ListView();
            this.comboBox_Klima = new System.Windows.Forms.ComboBox();
            this.label11 = new System.Windows.Forms.Label();
            this.tabPage8 = new System.Windows.Forms.TabPage();
            this.tabPage7 = new System.Windows.Forms.TabPage();
            this.btn_Details = new System.Windows.Forms.Button();
            this.checkBox_Sortiert = new System.Windows.Forms.CheckBox();
            this.label15 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.textBox_Gesamt_Waermebedarf = new System.Windows.Forms.TextBox();
            this.btn_SimulSpeichern = new System.Windows.Forms.Button();
            this.label14 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.comboBox_NetzvEinheit = new System.Windows.Forms.ComboBox();
            this.label12 = new System.Windows.Forms.Label();
            this.textBox_Netzverluste = new System.Windows.Forms.TextBox();
            this.textBox_MaxWaermelast = new System.Windows.Forms.TextBox();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.btn_Start = new System.Windows.Forms.Button();
            this.tabControl_Simulation = new System.Windows.Forms.TabControl();
            this.btn_DragDestination = new System.Windows.Forms.Button();
            this.btn_Speichern = new System.Windows.Forms.Button();
            this.tabControl_Komponenten.SuspendLayout();
            this.tabPage5.SuspendLayout();
            this.tabPage9.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage6.SuspendLayout();
            this.tabPage10.SuspendLayout();
            this.tabPage7.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.tabControl_Simulation.SuspendLayout();
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
            this.button_Beenden.Location = new System.Drawing.Point(1054, 766);
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
            this.label1.BackColor = System.Drawing.Color.SteelBlue;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(12, 73);
            this.label1.Name = "label1";
            this.label1.Padding = new System.Windows.Forms.Padding(1);
            this.label1.Size = new System.Drawing.Size(175, 23);
            this.label1.TabIndex = 11;
            this.label1.Text = "Projekteigenschaften";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.BackColor = System.Drawing.Color.SteelBlue;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Location = new System.Drawing.Point(495, 73);
            this.label4.Name = "label4";
            this.label4.Padding = new System.Windows.Forms.Padding(1);
            this.label4.Size = new System.Drawing.Size(111, 23);
            this.label4.TabIndex = 12;
            this.label4.Text = "Projektdaten";
            // 
            // textBox_Projekt
            // 
            this.textBox_Projekt.BackColor = System.Drawing.Color.White;
            this.textBox_Projekt.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Projekt.Enabled = false;
            this.textBox_Projekt.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Projekt.Location = new System.Drawing.Point(141, 112);
            this.textBox_Projekt.Name = "textBox_Projekt";
            this.textBox_Projekt.ReadOnly = true;
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
            this.textBox_Datum.Enabled = false;
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
            this.textBox_Beschreibung.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox_Beschreibung.Size = new System.Drawing.Size(304, 61);
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
            this.tabControl_Komponenten.Controls.Add(this.tabPage10);
            this.tabControl_Komponenten.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.tabControl_Komponenten.Location = new System.Drawing.Point(495, 102);
            this.tabControl_Komponenten.Multiline = true;
            this.tabControl_Komponenten.Name = "tabControl_Komponenten";
            this.tabControl_Komponenten.SelectedIndex = 0;
            this.tabControl_Komponenten.Size = new System.Drawing.Size(685, 279);
            this.tabControl_Komponenten.TabIndex = 22;
            // 
            // tabPage5
            // 
            this.tabPage5.Controls.Add(this.listView_Gebaeude);
            this.tabPage5.Location = new System.Drawing.Point(4, 26);
            this.tabPage5.Name = "tabPage5";
            this.tabPage5.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage5.Size = new System.Drawing.Size(677, 249);
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
            this.listView_Gebaeude.GiveFeedback += new System.Windows.Forms.GiveFeedbackEventHandler(this.listView_Gebaeude_GiveFeedback);
            this.listView_Gebaeude.DoubleClick += new System.EventHandler(this.listView_Gebaeude_DoubleClick);
            this.listView_Gebaeude.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listView_Gebaeude_MouseDown);
            this.listView_Gebaeude.MouseMove += new System.Windows.Forms.MouseEventHandler(this.listView_Gebaeude_MouseMove);
            this.listView_Gebaeude.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listView_Gebaeude_MouseUp);
            // 
            // tabPage9
            // 
            this.tabPage9.Controls.Add(this.listView_WaermebedarfExtern);
            this.tabPage9.Location = new System.Drawing.Point(4, 26);
            this.tabPage9.Name = "tabPage9";
            this.tabPage9.Size = new System.Drawing.Size(677, 249);
            this.tabPage9.TabIndex = 7;
            this.tabPage9.Text = "Wärmebedarf extern";
            this.tabPage9.UseVisualStyleBackColor = true;
            // 
            // listView_WaermebedarfExtern
            // 
            this.listView_WaermebedarfExtern.BackColor = System.Drawing.Color.White;
            this.listView_WaermebedarfExtern.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.listView_WaermebedarfExtern.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.listView_WaermebedarfExtern.FullRowSelect = true;
            this.listView_WaermebedarfExtern.GridLines = true;
            this.listView_WaermebedarfExtern.HideSelection = false;
            this.listView_WaermebedarfExtern.Location = new System.Drawing.Point(22, 17);
            this.listView_WaermebedarfExtern.MultiSelect = false;
            this.listView_WaermebedarfExtern.Name = "listView_WaermebedarfExtern";
            this.listView_WaermebedarfExtern.Size = new System.Drawing.Size(581, 133);
            this.listView_WaermebedarfExtern.TabIndex = 15;
            this.listView_WaermebedarfExtern.UseCompatibleStateImageBehavior = false;
            this.listView_WaermebedarfExtern.DoubleClick += new System.EventHandler(this.listView_WaermebedatfExtern_DoubleClick);
            this.listView_WaermebedarfExtern.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listView_WaermebedarfExtern_MouseDown);
            this.listView_WaermebedarfExtern.MouseMove += new System.Windows.Forms.MouseEventHandler(this.listView_WaermebedarfExtern_MouseMove);
            this.listView_WaermebedarfExtern.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listView_WaermebedarfExtern_MouseUp);
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label7);
            this.tabPage1.Controls.Add(this.listView_WP_Ref);
            this.tabPage1.Controls.Add(this.listView_WP);
            this.tabPage1.Location = new System.Drawing.Point(4, 26);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(0);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Size = new System.Drawing.Size(677, 249);
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
            this.tabPage2.Size = new System.Drawing.Size(677, 249);
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
            this.listView_SP_REF.DoubleClick += new System.EventHandler(this.listView_SP_REF_DoubleClick);
            this.listView_SP_REF.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listView_SP_REF_MouseDown);
            this.listView_SP_REF.MouseMove += new System.Windows.Forms.MouseEventHandler(this.listView_SP_REF_MouseMove);
            this.listView_SP_REF.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listView_SP_REF_MouseUp);
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
            this.listView_SP.DoubleClick += new System.EventHandler(this.listView_SP_DoubleClick);
            this.listView_SP.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listView_SP_MouseDown);
            this.listView_SP.MouseMove += new System.Windows.Forms.MouseEventHandler(this.listView_SP_MouseMove);
            this.listView_SP.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listView_SP_MouseUp);
            // 
            // tabPage3
            // 
            this.tabPage3.Location = new System.Drawing.Point(4, 26);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(677, 249);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Solar";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // tabPage4
            // 
            this.tabPage4.Location = new System.Drawing.Point(4, 26);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(677, 249);
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
            this.tabPage6.Size = new System.Drawing.Size(677, 249);
            this.tabPage6.TabIndex = 5;
            this.tabPage6.Text = "Heizkessel";
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
            this.listView_Heizkessel.DoubleClick += new System.EventHandler(this.listView_Heizkessel_DoubleClick);
            this.listView_Heizkessel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listView_Heizkessel_MouseDown);
            this.listView_Heizkessel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.listView_Heizkessel_MouseMove);
            this.listView_Heizkessel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listView_Heizkessel_MouseUp);
            // 
            // tabPage10
            // 
            this.tabPage10.Controls.Add(this.listView_Prozesswaerme);
            this.tabPage10.Location = new System.Drawing.Point(4, 26);
            this.tabPage10.Name = "tabPage10";
            this.tabPage10.Size = new System.Drawing.Size(677, 249);
            this.tabPage10.TabIndex = 8;
            this.tabPage10.Text = "Prozesswärme";
            this.tabPage10.UseVisualStyleBackColor = true;
            // 
            // listView_Prozesswaerme
            // 
            this.listView_Prozesswaerme.BackColor = System.Drawing.Color.White;
            this.listView_Prozesswaerme.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.listView_Prozesswaerme.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.listView_Prozesswaerme.FullRowSelect = true;
            this.listView_Prozesswaerme.GridLines = true;
            this.listView_Prozesswaerme.HideSelection = false;
            this.listView_Prozesswaerme.Location = new System.Drawing.Point(102, 76);
            this.listView_Prozesswaerme.MultiSelect = false;
            this.listView_Prozesswaerme.Name = "listView_Prozesswaerme";
            this.listView_Prozesswaerme.Size = new System.Drawing.Size(514, 97);
            this.listView_Prozesswaerme.TabIndex = 13;
            this.listView_Prozesswaerme.UseCompatibleStateImageBehavior = false;
            this.listView_Prozesswaerme.DoubleClick += new System.EventHandler(this.listView_Prozesswaerme_DoubleClick);
            this.listView_Prozesswaerme.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listView_Prozesswaerme_MouseDown);
            this.listView_Prozesswaerme.MouseMove += new System.Windows.Forms.MouseEventHandler(this.listView_Prozesswaerme_MouseMove);
            this.listView_Prozesswaerme.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listView_Prozesswaerme_MouseUp);
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
            this.label11.BackColor = System.Drawing.Color.SteelBlue;
            this.label11.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.ForeColor = System.Drawing.Color.White;
            this.label11.Location = new System.Drawing.Point(495, 397);
            this.label11.Name = "label11";
            this.label11.Padding = new System.Windows.Forms.Padding(1);
            this.label11.Size = new System.Drawing.Size(96, 23);
            this.label11.TabIndex = 28;
            this.label11.Text = "Simulation";
            // 
            // tabPage8
            // 
            this.tabPage8.Location = new System.Drawing.Point(4, 26);
            this.tabPage8.Name = "tabPage8";
            this.tabPage8.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage8.Size = new System.Drawing.Size(719, 303);
            this.tabPage8.TabIndex = 1;
            this.tabPage8.Text = "Prozesswärme";
            this.tabPage8.UseVisualStyleBackColor = true;
            // 
            // tabPage7
            // 
            this.tabPage7.Controls.Add(this.btn_Details);
            this.tabPage7.Controls.Add(this.checkBox_Sortiert);
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
            this.tabPage7.Controls.Add(this.btn_Start);
            this.tabPage7.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.tabPage7.Location = new System.Drawing.Point(4, 26);
            this.tabPage7.Name = "tabPage7";
            this.tabPage7.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage7.Size = new System.Drawing.Size(719, 303);
            this.tabPage7.TabIndex = 0;
            this.tabPage7.Text = "Wärmebedarf Gebäude";
            this.tabPage7.UseVisualStyleBackColor = true;
            // 
            // btn_Details
            // 
            this.btn_Details.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_Details.ForeColor = System.Drawing.Color.Black;
            this.btn_Details.Location = new System.Drawing.Point(624, 196);
            this.btn_Details.Name = "btn_Details";
            this.btn_Details.Size = new System.Drawing.Size(80, 31);
            this.btn_Details.TabIndex = 36;
            this.btn_Details.Text = "Details...";
            this.btn_Details.UseVisualStyleBackColor = true;
            this.btn_Details.Click += new System.EventHandler(this.btn_Details_Click);
            // 
            // checkBox_Sortiert
            // 
            this.checkBox_Sortiert.AutoSize = true;
            this.checkBox_Sortiert.Checked = true;
            this.checkBox_Sortiert.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox_Sortiert.ForeColor = System.Drawing.Color.Black;
            this.checkBox_Sortiert.Location = new System.Drawing.Point(325, 42);
            this.checkBox_Sortiert.Name = "checkBox_Sortiert";
            this.checkBox_Sortiert.Size = new System.Drawing.Size(73, 23);
            this.checkBox_Sortiert.TabIndex = 35;
            this.checkBox_Sortiert.Text = "Sortiert";
            this.checkBox_Sortiert.UseVisualStyleBackColor = true;
            this.checkBox_Sortiert.CheckedChanged += new System.EventHandler(this.checkBox_Sortiert_CheckedChanged);
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.BackColor = System.Drawing.Color.Black;
            this.label15.Location = new System.Drawing.Point(561, 151);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(43, 19);
            this.label15.TabIndex = 34;
            this.label15.Text = "MWh";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.label16.ForeColor = System.Drawing.Color.Black;
            this.label16.Location = new System.Drawing.Point(492, 132);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(128, 13);
            this.label16.TabIndex = 33;
            this.label16.Text = "Gesamter Wärmebedarf";
            // 
            // textBox_Gesamt_Waermebedarf
            // 
            this.textBox_Gesamt_Waermebedarf.Location = new System.Drawing.Point(495, 148);
            this.textBox_Gesamt_Waermebedarf.Name = "textBox_Gesamt_Waermebedarf";
            this.textBox_Gesamt_Waermebedarf.Size = new System.Drawing.Size(60, 25);
            this.textBox_Gesamt_Waermebedarf.TabIndex = 32;
            // 
            // btn_SimulSpeichern
            // 
            this.btn_SimulSpeichern.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_SimulSpeichern.ForeColor = System.Drawing.Color.Black;
            this.btn_SimulSpeichern.Location = new System.Drawing.Point(484, 196);
            this.btn_SimulSpeichern.Name = "btn_SimulSpeichern";
            this.btn_SimulSpeichern.Size = new System.Drawing.Size(134, 32);
            this.btn_SimulSpeichern.TabIndex = 29;
            this.btn_SimulSpeichern.Text = "Simulation Speichern";
            this.btn_SimulSpeichern.UseVisualStyleBackColor = true;
            this.btn_SimulSpeichern.Click += new System.EventHandler(this.btn_SimulSpeichern_Click);
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.BackColor = System.Drawing.Color.Black;
            this.label14.Location = new System.Drawing.Point(561, 98);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(29, 19);
            this.label14.TabIndex = 31;
            this.label14.Text = "kW";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.label13.ForeColor = System.Drawing.Color.Black;
            this.label13.Location = new System.Drawing.Point(492, 82);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(87, 13);
            this.label13.TabIndex = 30;
            this.label13.Text = "max. Wärmelast";
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
            // textBox_MaxWaermelast
            // 
            this.textBox_MaxWaermelast.Location = new System.Drawing.Point(495, 98);
            this.textBox_MaxWaermelast.Name = "textBox_MaxWaermelast";
            this.textBox_MaxWaermelast.Size = new System.Drawing.Size(60, 25);
            this.textBox_MaxWaermelast.TabIndex = 26;
            // 
            // chart1
            // 
            chartArea1.AxisX.IsLabelAutoFit = false;
            chartArea1.AxisX.LabelStyle.Font = new System.Drawing.Font("Segoe UI", 10F);
            chartArea1.AxisX.Title = "Jahresstunden";
            chartArea1.AxisX.TitleFont = new System.Drawing.Font("Segoe UI", 10F);
            chartArea1.AxisY.Title = "Wärmelast in % ";
            chartArea1.AxisY.TitleFont = new System.Drawing.Font("Segoe UI", 10F);
            chartArea1.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chartArea1.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea1);
            this.chart1.Location = new System.Drawing.Point(6, 39);
            this.chart1.Name = "chart1";
            series1.ChartArea = "ChartArea1";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series1.Name = "Series1";
            this.chart1.Series.Add(series1);
            this.chart1.Size = new System.Drawing.Size(472, 246);
            this.chart1.TabIndex = 25;
            this.chart1.Text = "chart1";
            title1.Name = "Title1";
            title1.Text = "Wärmelast Jahresganglinie ";
            this.chart1.Titles.Add(title1);
            // 
            // btn_Start
            // 
            this.btn_Start.AllowDrop = true;
            this.btn_Start.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_Start.ForeColor = System.Drawing.Color.Black;
            this.btn_Start.Location = new System.Drawing.Point(484, 42);
            this.btn_Start.Name = "btn_Start";
            this.btn_Start.Size = new System.Drawing.Size(134, 28);
            this.btn_Start.TabIndex = 24;
            this.btn_Start.Text = "Simulation Start...";
            this.btn_Start.UseVisualStyleBackColor = true;
            this.btn_Start.Click += new System.EventHandler(this.btn_Start_Click);
            // 
            // tabControl_Simulation
            // 
            this.tabControl_Simulation.Controls.Add(this.tabPage7);
            this.tabControl_Simulation.Controls.Add(this.tabPage8);
            this.tabControl_Simulation.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.tabControl_Simulation.Location = new System.Drawing.Point(495, 427);
            this.tabControl_Simulation.Name = "tabControl_Simulation";
            this.tabControl_Simulation.SelectedIndex = 0;
            this.tabControl_Simulation.Size = new System.Drawing.Size(727, 333);
            this.tabControl_Simulation.TabIndex = 27;
            // 
            // btn_DragDestination
            // 
            this.btn_DragDestination.AllowDrop = true;
            this.btn_DragDestination.AutoSize = true;
            this.btn_DragDestination.ForeColor = System.Drawing.Color.Black;
            this.btn_DragDestination.Image = ((System.Drawing.Image)(resources.GetObject("btn_DragDestination.Image")));
            this.btn_DragDestination.Location = new System.Drawing.Point(824, 61);
            this.btn_DragDestination.Name = "btn_DragDestination";
            this.btn_DragDestination.Size = new System.Drawing.Size(33, 35);
            this.btn_DragDestination.TabIndex = 29;
            this.btn_DragDestination.Text = "..";
            this.btn_DragDestination.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btn_DragDestination.UseVisualStyleBackColor = true;
            this.btn_DragDestination.DragDrop += new System.Windows.Forms.DragEventHandler(this.button1_DragDrop);
            this.btn_DragDestination.DragEnter += new System.Windows.Forms.DragEventHandler(this.button1_DragEnter);
            this.btn_DragDestination.DragOver += new System.Windows.Forms.DragEventHandler(this.button1_DragOver);
            this.btn_DragDestination.MouseHover += new System.EventHandler(this.btn_DragDestination_MouseHover);
            // 
            // btn_Speichern
            // 
            this.btn_Speichern.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.btn_Speichern.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Speichern.ForeColor = System.Drawing.Color.Black;
            this.btn_Speichern.Location = new System.Drawing.Point(16, 288);
            this.btn_Speichern.Name = "btn_Speichern";
            this.btn_Speichern.Size = new System.Drawing.Size(106, 30);
            this.btn_Speichern.TabIndex = 30;
            this.btn_Speichern.Text = "Speichern";
            this.btn_Speichern.UseVisualStyleBackColor = false;
            this.btn_Speichern.Click += new System.EventHandler(this.btn_Speichern_Click);
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ClientSize = new System.Drawing.Size(1234, 801);
            this.ControlBox = false;
            this.Controls.Add(this.btn_Speichern);
            this.Controls.Add(this.btn_DragDestination);
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
            this.tabPage5.ResumeLayout(false);
            this.tabPage9.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.tabPage6.ResumeLayout(false);
            this.tabPage10.ResumeLayout(false);
            this.tabPage7.ResumeLayout(false);
            this.tabPage7.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.tabControl_Simulation.ResumeLayout(false);
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
        private System.Windows.Forms.Button btn_Start;
        private System.Windows.Forms.TabControl tabControl_Simulation;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Button btn_SimulSpeichern;
        private System.Windows.Forms.TabPage tabPage9;
        private System.Windows.Forms.ListView listView_WaermebedarfExtern;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TextBox textBox_Gesamt_Waermebedarf;
        private System.Windows.Forms.TabPage tabPage10;
        private System.Windows.Forms.ListView listView_Prozesswaerme;
        private System.Windows.Forms.CheckBox checkBox_Sortiert;
        private System.Windows.Forms.Button btn_Details;
        private System.Windows.Forms.Button btn_DragDestination;
        private System.Windows.Forms.Button btn_Speichern;
    }
}