namespace WindowsFormsApplication1
{
    partial class Wizard_Referenz
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
            this.label_Type = new System.Windows.Forms.Label();
            this.tabControl_SP = new System.Windows.Forms.TabControl();
            this.tabPage_WP = new System.Windows.Forms.TabPage();
            this.label19 = new System.Windows.Forms.Label();
            this.btn_Neu = new System.Windows.Forms.Button();
            this.textBox_WP = new System.Windows.Forms.TextBox();
            this.label20 = new System.Windows.Forms.Label();
            this.label21 = new System.Windows.Forms.Label();
            this.label22 = new System.Windows.Forms.Label();
            this.btn_Ansicht = new System.Windows.Forms.Button();
            this.label23 = new System.Windows.Forms.Label();
            this.btn_Löschen = new System.Windows.Forms.Button();
            this.listView_WP = new System.Windows.Forms.ListView();
            this.btn_Uebernehmen = new System.Windows.Forms.Button();
            this.tabPage_SP = new System.Windows.Forms.TabPage();
            this.label10 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.textBox_Ladezustand = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.textBox_Degradation = new System.Windows.Forms.TextBox();
            this.textBox_Energie = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox_Leistung = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox_Typ = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox_Name = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btn_Entfernen = new System.Windows.Forms.Button();
            this.btn_Hinzu = new System.Windows.Forms.Button();
            this.listBox_SP_DB = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.listBox_SP = new System.Windows.Forms.ListBox();
            this.tabPage_Standard = new System.Windows.Forms.TabPage();
            this.label18 = new System.Windows.Forms.Label();
            this.textBox_Kesselbeschreibung = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.textBox_Kesselleistung = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.textBox_Kesseltyp = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.textBox_Kesselname = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.btn_Kessel_Entfernen = new System.Windows.Forms.Button();
            this.btn_Kessel_Hinzu = new System.Windows.Forms.Button();
            this.listBox_Kessel_DB = new System.Windows.Forms.ListBox();
            this.label12 = new System.Windows.Forms.Label();
            this.listBox_Kessel = new System.Windows.Forms.ListBox();
            this.tabControl_SP.SuspendLayout();
            this.tabPage_WP.SuspendLayout();
            this.tabPage_SP.SuspendLayout();
            this.tabPage_Standard.SuspendLayout();
            this.SuspendLayout();
            // 
            // label_Type
            // 
            this.label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.label_Type.Dock = System.Windows.Forms.DockStyle.Top;
            this.label_Type.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Type.Location = new System.Drawing.Point(0, 0);
            this.label_Type.Name = "label_Type";
            this.label_Type.Padding = new System.Windows.Forms.Padding(0, 5, 0, 5);
            this.label_Type.Size = new System.Drawing.Size(685, 32);
            this.label_Type.TabIndex = 24;
            this.label_Type.Text = "Eingabe des Referenzsystems";
            this.label_Type.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // tabControl_SP
            // 
            this.tabControl_SP.Controls.Add(this.tabPage_WP);
            this.tabControl_SP.Controls.Add(this.tabPage_SP);
            this.tabControl_SP.Controls.Add(this.tabPage_Standard);
            this.tabControl_SP.Location = new System.Drawing.Point(21, 62);
            this.tabControl_SP.Name = "tabControl_SP";
            this.tabControl_SP.SelectedIndex = 0;
            this.tabControl_SP.Size = new System.Drawing.Size(642, 423);
            this.tabControl_SP.TabIndex = 25;
            // 
            // tabPage_WP
            // 
            this.tabPage_WP.Controls.Add(this.label19);
            this.tabPage_WP.Controls.Add(this.btn_Neu);
            this.tabPage_WP.Controls.Add(this.textBox_WP);
            this.tabPage_WP.Controls.Add(this.label20);
            this.tabPage_WP.Controls.Add(this.label21);
            this.tabPage_WP.Controls.Add(this.label22);
            this.tabPage_WP.Controls.Add(this.btn_Ansicht);
            this.tabPage_WP.Controls.Add(this.label23);
            this.tabPage_WP.Controls.Add(this.btn_Löschen);
            this.tabPage_WP.Controls.Add(this.listView_WP);
            this.tabPage_WP.Controls.Add(this.btn_Uebernehmen);
            this.tabPage_WP.Location = new System.Drawing.Point(4, 26);
            this.tabPage_WP.Name = "tabPage_WP";
            this.tabPage_WP.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage_WP.Size = new System.Drawing.Size(634, 393);
            this.tabPage_WP.TabIndex = 0;
            this.tabPage_WP.Text = "Wärmepumpen";
            this.tabPage_WP.UseVisualStyleBackColor = true;
            // 
            // label19
            // 
            this.label19.BackColor = System.Drawing.Color.White;
            this.label19.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.label19.Location = new System.Drawing.Point(49, 182);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(130, 21);
            this.label19.TabIndex = 67;
            this.label19.Text = "Neuer Eintrag";
            // 
            // btn_Neu
            // 
            this.btn_Neu.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Neu.Location = new System.Drawing.Point(185, 180);
            this.btn_Neu.Name = "btn_Neu";
            this.btn_Neu.Size = new System.Drawing.Size(75, 27);
            this.btn_Neu.TabIndex = 66;
            this.btn_Neu.Text = "Neu";
            this.btn_Neu.UseVisualStyleBackColor = true;
            this.btn_Neu.Click += new System.EventHandler(this.btn_Neu_Click);
            // 
            // textBox_WP
            // 
            this.textBox_WP.Enabled = false;
            this.textBox_WP.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.textBox_WP.Location = new System.Drawing.Point(290, 20);
            this.textBox_WP.Name = "textBox_WP";
            this.textBox_WP.Size = new System.Drawing.Size(215, 29);
            this.textBox_WP.TabIndex = 65;
            this.textBox_WP.Visible = false;
            // 
            // label20
            // 
            this.label20.BackColor = System.Drawing.Color.White;
            this.label20.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.label20.Location = new System.Drawing.Point(49, 308);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(130, 21);
            this.label20.TabIndex = 64;
            this.label20.Text = "Eintrag ändern";
            // 
            // label21
            // 
            this.label21.BackColor = System.Drawing.Color.White;
            this.label21.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.label21.Location = new System.Drawing.Point(49, 267);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(130, 21);
            this.label21.TabIndex = 63;
            this.label21.Text = "Detailansicht";
            // 
            // label22
            // 
            this.label22.BackColor = System.Drawing.Color.White;
            this.label22.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.label22.Location = new System.Drawing.Point(49, 225);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(130, 21);
            this.label22.TabIndex = 62;
            this.label22.Text = "Eintrag löschen";
            // 
            // btn_Ansicht
            // 
            this.btn_Ansicht.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Ansicht.Location = new System.Drawing.Point(185, 265);
            this.btn_Ansicht.Name = "btn_Ansicht";
            this.btn_Ansicht.Size = new System.Drawing.Size(75, 27);
            this.btn_Ansicht.TabIndex = 61;
            this.btn_Ansicht.Text = "Ansicht";
            this.btn_Ansicht.UseVisualStyleBackColor = true;
            this.btn_Ansicht.Click += new System.EventHandler(this.btn_Ansicht_Click);
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label23.Location = new System.Drawing.Point(34, 42);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(186, 19);
            this.label23.TabIndex = 59;
            this.label23.Text = "Ausgewählte Wärmepumpen";
            // 
            // btn_Löschen
            // 
            this.btn_Löschen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Löschen.Location = new System.Drawing.Point(185, 223);
            this.btn_Löschen.Name = "btn_Löschen";
            this.btn_Löschen.Size = new System.Drawing.Size(75, 27);
            this.btn_Löschen.TabIndex = 58;
            this.btn_Löschen.Text = "Löschen";
            this.btn_Löschen.UseVisualStyleBackColor = true;
            this.btn_Löschen.Click += new System.EventHandler(this.btn_Löschen_Click);
            // 
            // listView_WP
            // 
            this.listView_WP.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.listView_WP.FullRowSelect = true;
            this.listView_WP.GridLines = true;
            this.listView_WP.Location = new System.Drawing.Point(38, 64);
            this.listView_WP.MultiSelect = false;
            this.listView_WP.Name = "listView_WP";
            this.listView_WP.Size = new System.Drawing.Size(550, 97);
            this.listView_WP.TabIndex = 57;
            this.listView_WP.UseCompatibleStateImageBehavior = false;
            this.listView_WP.SelectedIndexChanged += new System.EventHandler(this.listView_WP_SelectedIndexChanged);
            // 
            // btn_Uebernehmen
            // 
            this.btn_Uebernehmen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Uebernehmen.Location = new System.Drawing.Point(185, 306);
            this.btn_Uebernehmen.Name = "btn_Uebernehmen";
            this.btn_Uebernehmen.Size = new System.Drawing.Size(75, 27);
            this.btn_Uebernehmen.TabIndex = 60;
            this.btn_Uebernehmen.Text = "Ändern";
            this.btn_Uebernehmen.UseVisualStyleBackColor = true;
            this.btn_Uebernehmen.Click += new System.EventHandler(this.btn_Uebernehmen_Click);
            // 
            // tabPage_SP
            // 
            this.tabPage_SP.Controls.Add(this.label10);
            this.tabPage_SP.Controls.Add(this.label9);
            this.tabPage_SP.Controls.Add(this.label17);
            this.tabPage_SP.Controls.Add(this.label6);
            this.tabPage_SP.Controls.Add(this.label8);
            this.tabPage_SP.Controls.Add(this.textBox_Ladezustand);
            this.tabPage_SP.Controls.Add(this.label7);
            this.tabPage_SP.Controls.Add(this.textBox_Degradation);
            this.tabPage_SP.Controls.Add(this.textBox_Energie);
            this.tabPage_SP.Controls.Add(this.label5);
            this.tabPage_SP.Controls.Add(this.textBox_Leistung);
            this.tabPage_SP.Controls.Add(this.label4);
            this.tabPage_SP.Controls.Add(this.textBox_Typ);
            this.tabPage_SP.Controls.Add(this.label3);
            this.tabPage_SP.Controls.Add(this.textBox_Name);
            this.tabPage_SP.Controls.Add(this.label2);
            this.tabPage_SP.Controls.Add(this.btn_Entfernen);
            this.tabPage_SP.Controls.Add(this.btn_Hinzu);
            this.tabPage_SP.Controls.Add(this.listBox_SP_DB);
            this.tabPage_SP.Controls.Add(this.label1);
            this.tabPage_SP.Controls.Add(this.listBox_SP);
            this.tabPage_SP.Location = new System.Drawing.Point(4, 26);
            this.tabPage_SP.Name = "tabPage_SP";
            this.tabPage_SP.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage_SP.Size = new System.Drawing.Size(634, 393);
            this.tabPage_SP.TabIndex = 1;
            this.tabPage_SP.Text = "Stromspeicher";
            this.tabPage_SP.UseVisualStyleBackColor = true;
            // 
            // label10
            // 
            this.label10.BackColor = System.Drawing.Color.Black;
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.ForeColor = System.Drawing.Color.White;
            this.label10.Location = new System.Drawing.Point(191, 334);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(33, 19);
            this.label10.TabIndex = 76;
            this.label10.Text = "%/a";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label9
            // 
            this.label9.BackColor = System.Drawing.Color.Black;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.ForeColor = System.Drawing.Color.White;
            this.label9.Location = new System.Drawing.Point(191, 359);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(23, 19);
            this.label9.TabIndex = 75;
            this.label9.Text = "%";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label17
            // 
            this.label17.BackColor = System.Drawing.Color.Black;
            this.label17.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label17.ForeColor = System.Drawing.Color.White;
            this.label17.Location = new System.Drawing.Point(191, 265);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(33, 19);
            this.label17.TabIndex = 74;
            this.label17.Text = "kW";
            this.label17.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label6.Location = new System.Drawing.Point(37, 331);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(85, 19);
            this.label6.TabIndex = 73;
            this.label6.Text = "Degradation";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label8.Location = new System.Drawing.Point(35, 362);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(86, 19);
            this.label8.TabIndex = 72;
            this.label8.Text = "Ladezustand";
            // 
            // textBox_Ladezustand
            // 
            this.textBox_Ladezustand.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Ladezustand.Location = new System.Drawing.Point(127, 359);
            this.textBox_Ladezustand.Name = "textBox_Ladezustand";
            this.textBox_Ladezustand.Size = new System.Drawing.Size(58, 25);
            this.textBox_Ladezustand.TabIndex = 71;
            this.textBox_Ladezustand.Text = "98";
            this.textBox_Ladezustand.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Ladezustand_Validating);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label7.Location = new System.Drawing.Point(35, 301);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(54, 19);
            this.label7.TabIndex = 70;
            this.label7.Text = "Energie";
            // 
            // textBox_Degradation
            // 
            this.textBox_Degradation.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Degradation.Location = new System.Drawing.Point(127, 328);
            this.textBox_Degradation.Name = "textBox_Degradation";
            this.textBox_Degradation.Size = new System.Drawing.Size(58, 25);
            this.textBox_Degradation.TabIndex = 69;
            this.textBox_Degradation.Text = "0,1";
            this.textBox_Degradation.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Degradation_Validating);
            // 
            // textBox_Energie
            // 
            this.textBox_Energie.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Energie.Location = new System.Drawing.Point(127, 298);
            this.textBox_Energie.Name = "textBox_Energie";
            this.textBox_Energie.Size = new System.Drawing.Size(117, 25);
            this.textBox_Energie.TabIndex = 68;
            this.textBox_Energie.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Energie_Validating);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label5.Location = new System.Drawing.Point(35, 268);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(61, 19);
            this.label5.TabIndex = 67;
            this.label5.Text = "Leistung";
            // 
            // textBox_Leistung
            // 
            this.textBox_Leistung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Leistung.Location = new System.Drawing.Point(127, 265);
            this.textBox_Leistung.Name = "textBox_Leistung";
            this.textBox_Leistung.Size = new System.Drawing.Size(58, 25);
            this.textBox_Leistung.TabIndex = 66;
            this.textBox_Leistung.Text = "10";
            this.textBox_Leistung.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Leistung_Validating);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label4.Location = new System.Drawing.Point(35, 237);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(31, 19);
            this.label4.TabIndex = 65;
            this.label4.Text = "Typ";
            // 
            // textBox_Typ
            // 
            this.textBox_Typ.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Typ.Location = new System.Drawing.Point(127, 234);
            this.textBox_Typ.Name = "textBox_Typ";
            this.textBox_Typ.Size = new System.Drawing.Size(235, 25);
            this.textBox_Typ.TabIndex = 64;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label3.Location = new System.Drawing.Point(35, 207);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(45, 19);
            this.label3.TabIndex = 63;
            this.label3.Text = "Name";
            // 
            // textBox_Name
            // 
            this.textBox_Name.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Name.Location = new System.Drawing.Point(127, 203);
            this.textBox_Name.Name = "textBox_Name";
            this.textBox_Name.Size = new System.Drawing.Size(235, 25);
            this.textBox_Name.TabIndex = 62;
            this.textBox_Name.Text = "Vitovalor 300-P C3TB Brennwertkessel";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label2.Location = new System.Drawing.Point(372, 12);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(192, 19);
            this.label2.TabIndex = 13;
            this.label2.Text = "Stromspeicher aus Datenbank";
            // 
            // btn_Entfernen
            // 
            this.btn_Entfernen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Entfernen.Location = new System.Drawing.Point(277, 94);
            this.btn_Entfernen.Name = "btn_Entfernen";
            this.btn_Entfernen.Size = new System.Drawing.Size(75, 28);
            this.btn_Entfernen.TabIndex = 12;
            this.btn_Entfernen.Text = "-->";
            this.btn_Entfernen.UseVisualStyleBackColor = true;
            this.btn_Entfernen.Click += new System.EventHandler(this.btn_Entfernen_Click);
            // 
            // btn_Hinzu
            // 
            this.btn_Hinzu.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Hinzu.Location = new System.Drawing.Point(277, 59);
            this.btn_Hinzu.Name = "btn_Hinzu";
            this.btn_Hinzu.Size = new System.Drawing.Size(75, 29);
            this.btn_Hinzu.TabIndex = 11;
            this.btn_Hinzu.Text = "<---";
            this.btn_Hinzu.UseVisualStyleBackColor = true;
            this.btn_Hinzu.Click += new System.EventHandler(this.btn_Hinzu_Click);
            // 
            // listBox_SP_DB
            // 
            this.listBox_SP_DB.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.listBox_SP_DB.FormattingEnabled = true;
            this.listBox_SP_DB.ItemHeight = 15;
            this.listBox_SP_DB.Location = new System.Drawing.Point(375, 34);
            this.listBox_SP_DB.Name = "listBox_SP_DB";
            this.listBox_SP_DB.Size = new System.Drawing.Size(229, 154);
            this.listBox_SP_DB.TabIndex = 10;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label1.Location = new System.Drawing.Point(16, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(176, 19);
            this.label1.TabIndex = 9;
            this.label1.Text = "ausgewählte Stromspeicher";
            // 
            // listBox_SP
            // 
            this.listBox_SP.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.listBox_SP.FormattingEnabled = true;
            this.listBox_SP.ItemHeight = 15;
            this.listBox_SP.Location = new System.Drawing.Point(20, 34);
            this.listBox_SP.Name = "listBox_SP";
            this.listBox_SP.Size = new System.Drawing.Size(235, 154);
            this.listBox_SP.TabIndex = 8;
            this.listBox_SP.SelectedIndexChanged += new System.EventHandler(this.listBox_SP_SelectedIndexChanged);
            // 
            // tabPage_Standard
            // 
            this.tabPage_Standard.Controls.Add(this.label18);
            this.tabPage_Standard.Controls.Add(this.textBox_Kesselbeschreibung);
            this.tabPage_Standard.Controls.Add(this.label13);
            this.tabPage_Standard.Controls.Add(this.label14);
            this.tabPage_Standard.Controls.Add(this.textBox_Kesselleistung);
            this.tabPage_Standard.Controls.Add(this.label15);
            this.tabPage_Standard.Controls.Add(this.textBox_Kesseltyp);
            this.tabPage_Standard.Controls.Add(this.label16);
            this.tabPage_Standard.Controls.Add(this.textBox_Kesselname);
            this.tabPage_Standard.Controls.Add(this.label11);
            this.tabPage_Standard.Controls.Add(this.btn_Kessel_Entfernen);
            this.tabPage_Standard.Controls.Add(this.btn_Kessel_Hinzu);
            this.tabPage_Standard.Controls.Add(this.listBox_Kessel_DB);
            this.tabPage_Standard.Controls.Add(this.label12);
            this.tabPage_Standard.Controls.Add(this.listBox_Kessel);
            this.tabPage_Standard.Location = new System.Drawing.Point(4, 26);
            this.tabPage_Standard.Name = "tabPage_Standard";
            this.tabPage_Standard.Size = new System.Drawing.Size(634, 393);
            this.tabPage_Standard.TabIndex = 2;
            this.tabPage_Standard.Text = "ÖL/Gas/Holz Heizkessel";
            this.tabPage_Standard.UseVisualStyleBackColor = true;
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label18.Location = new System.Drawing.Point(78, 251);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(91, 19);
            this.label18.TabIndex = 83;
            this.label18.Text = "Beschreibung";
            // 
            // textBox_Kesselbeschreibung
            // 
            this.textBox_Kesselbeschreibung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kesselbeschreibung.Location = new System.Drawing.Point(170, 248);
            this.textBox_Kesselbeschreibung.Multiline = true;
            this.textBox_Kesselbeschreibung.Name = "textBox_Kesselbeschreibung";
            this.textBox_Kesselbeschreibung.Size = new System.Drawing.Size(395, 40);
            this.textBox_Kesselbeschreibung.TabIndex = 82;
            // 
            // label13
            // 
            this.label13.BackColor = System.Drawing.Color.Black;
            this.label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.ForeColor = System.Drawing.Color.White;
            this.label13.Location = new System.Drawing.Point(234, 325);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(33, 19);
            this.label13.TabIndex = 81;
            this.label13.Text = "kW";
            this.label13.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label14.Location = new System.Drawing.Point(78, 328);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(61, 19);
            this.label14.TabIndex = 80;
            this.label14.Text = "Leistung";
            // 
            // textBox_Kesselleistung
            // 
            this.textBox_Kesselleistung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kesselleistung.Location = new System.Drawing.Point(170, 325);
            this.textBox_Kesselleistung.Name = "textBox_Kesselleistung";
            this.textBox_Kesselleistung.Size = new System.Drawing.Size(58, 25);
            this.textBox_Kesselleistung.TabIndex = 79;
            this.textBox_Kesselleistung.Text = "10";
            this.textBox_Kesselleistung.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Kesselleistung_Validating);
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label15.Location = new System.Drawing.Point(78, 297);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(31, 19);
            this.label15.TabIndex = 78;
            this.label15.Text = "Typ";
            // 
            // textBox_Kesseltyp
            // 
            this.textBox_Kesseltyp.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kesseltyp.Location = new System.Drawing.Point(170, 294);
            this.textBox_Kesseltyp.Name = "textBox_Kesseltyp";
            this.textBox_Kesseltyp.Size = new System.Drawing.Size(177, 25);
            this.textBox_Kesseltyp.TabIndex = 77;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label16.Location = new System.Drawing.Point(78, 220);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(45, 19);
            this.label16.TabIndex = 76;
            this.label16.Text = "Name";
            // 
            // textBox_Kesselname
            // 
            this.textBox_Kesselname.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kesselname.Location = new System.Drawing.Point(170, 217);
            this.textBox_Kesselname.Name = "textBox_Kesselname";
            this.textBox_Kesselname.Size = new System.Drawing.Size(258, 25);
            this.textBox_Kesselname.TabIndex = 75;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label11.Location = new System.Drawing.Point(361, 22);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(142, 19);
            this.label11.TabIndex = 19;
            this.label11.Text = "Kessel aus Datenbank";
            // 
            // btn_Kessel_Entfernen
            // 
            this.btn_Kessel_Entfernen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Kessel_Entfernen.Location = new System.Drawing.Point(272, 106);
            this.btn_Kessel_Entfernen.Name = "btn_Kessel_Entfernen";
            this.btn_Kessel_Entfernen.Size = new System.Drawing.Size(75, 28);
            this.btn_Kessel_Entfernen.TabIndex = 18;
            this.btn_Kessel_Entfernen.Text = "-->";
            this.btn_Kessel_Entfernen.UseVisualStyleBackColor = true;
            // 
            // btn_Kessel_Hinzu
            // 
            this.btn_Kessel_Hinzu.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Kessel_Hinzu.Location = new System.Drawing.Point(272, 71);
            this.btn_Kessel_Hinzu.Name = "btn_Kessel_Hinzu";
            this.btn_Kessel_Hinzu.Size = new System.Drawing.Size(75, 29);
            this.btn_Kessel_Hinzu.TabIndex = 17;
            this.btn_Kessel_Hinzu.Text = "<---";
            this.btn_Kessel_Hinzu.UseVisualStyleBackColor = true;
            this.btn_Kessel_Hinzu.Click += new System.EventHandler(this.btn_Kessel_Hinzu_Click);
            // 
            // listBox_Kessel_DB
            // 
            this.listBox_Kessel_DB.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.listBox_Kessel_DB.FormattingEnabled = true;
            this.listBox_Kessel_DB.HorizontalScrollbar = true;
            this.listBox_Kessel_DB.ItemHeight = 17;
            this.listBox_Kessel_DB.Location = new System.Drawing.Point(365, 44);
            this.listBox_Kessel_DB.Name = "listBox_Kessel_DB";
            this.listBox_Kessel_DB.Size = new System.Drawing.Size(250, 157);
            this.listBox_Kessel_DB.TabIndex = 16;
            this.listBox_Kessel_DB.SelectedIndexChanged += new System.EventHandler(this.listBox_Kessel_DB_SelectedIndexChanged);
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label12.Location = new System.Drawing.Point(13, 22);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(126, 19);
            this.label12.TabIndex = 15;
            this.label12.Text = "ausgewählte Kessel";
            // 
            // listBox_Kessel
            // 
            this.listBox_Kessel.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.listBox_Kessel.FormattingEnabled = true;
            this.listBox_Kessel.HorizontalScrollbar = true;
            this.listBox_Kessel.ItemHeight = 17;
            this.listBox_Kessel.Location = new System.Drawing.Point(15, 44);
            this.listBox_Kessel.Name = "listBox_Kessel";
            this.listBox_Kessel.Size = new System.Drawing.Size(238, 157);
            this.listBox_Kessel.TabIndex = 14;
            this.listBox_Kessel.SelectedIndexChanged += new System.EventHandler(this.listBox_Kessel_SelectedIndexChanged);
            // 
            // Wizard_Referenz
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(685, 524);
            this.Controls.Add(this.tabControl_SP);
            this.Controls.Add(this.label_Type);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Wizard_Referenz";
            this.Load += new System.EventHandler(this.Wizard_Referenz_Load);
            this.tabControl_SP.ResumeLayout(false);
            this.tabPage_WP.ResumeLayout(false);
            this.tabPage_WP.PerformLayout();
            this.tabPage_SP.ResumeLayout(false);
            this.tabPage_SP.PerformLayout();
            this.tabPage_Standard.ResumeLayout(false);
            this.tabPage_Standard.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label_Type;
        private System.Windows.Forms.TabControl tabControl_SP;
        private System.Windows.Forms.TabPage tabPage_WP;
        private System.Windows.Forms.TabPage tabPage_SP;
        private System.Windows.Forms.TabPage tabPage_Standard;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox listBox_SP;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btn_Entfernen;
        private System.Windows.Forms.Button btn_Hinzu;
        private System.Windows.Forms.ListBox listBox_SP_DB;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox textBox_Ladezustand;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textBox_Degradation;
        private System.Windows.Forms.TextBox textBox_Energie;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox_Leistung;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox_Typ;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox_Name;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Button btn_Kessel_Entfernen;
        private System.Windows.Forms.Button btn_Kessel_Hinzu;
        private System.Windows.Forms.ListBox listBox_Kessel_DB;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.ListBox listBox_Kessel;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox textBox_Kesselleistung;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox textBox_Kesseltyp;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TextBox textBox_Kesselname;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.TextBox textBox_Kesselbeschreibung;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Button btn_Neu;
        private System.Windows.Forms.TextBox textBox_WP;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.Button btn_Ansicht;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.Button btn_Löschen;
        private System.Windows.Forms.ListView listView_WP;
        private System.Windows.Forms.Button btn_Uebernehmen;
    }
}