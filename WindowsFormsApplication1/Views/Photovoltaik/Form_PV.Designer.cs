namespace WindowsFormsApplication1
{
    partial class Form_PV 
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
            this.label11 = new System.Windows.Forms.Label();
            this.btn__Entfernen = new System.Windows.Forms.Button();
            this.btn__Hinzu = new System.Windows.Forms.Button();
            this.listBox_DB = new System.Windows.Forms.ListBox();
            this.label12 = new System.Windows.Forms.Label();
            this.listBox_Auswahl = new System.Windows.Forms.ListBox();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBox_Hersteller = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox_Neigung = new System.Windows.Forms.TextBox();
            this.btn_Bearbeiten = new System.Windows.Forms.Button();
            this.btn_Löschen = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.textBox_Azimut = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.textBox_AnlagenLeistung = new System.Windows.Forms.TextBox();
            this.label_Type = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.textBox_Leistung = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.textBox_Hersteller = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.textBox_Name = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox_Gesamtleistung = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.btn_Help = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label11.Location = new System.Drawing.Point(448, 36);
            this.label11.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(152, 19);
            this.label11.TabIndex = 89;
            this.label11.Text = "Module aus Datenbank";
            // 
            // btn__Entfernen
            // 
            this.btn__Entfernen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn__Entfernen.Location = new System.Drawing.Point(353, 140);
            this.btn__Entfernen.Margin = new System.Windows.Forms.Padding(4);
            this.btn__Entfernen.Name = "btn__Entfernen";
            this.btn__Entfernen.Size = new System.Drawing.Size(88, 37);
            this.btn__Entfernen.TabIndex = 88;
            this.btn__Entfernen.TabStop = false;
            this.btn__Entfernen.Text = "▶";
            this.btn__Entfernen.UseVisualStyleBackColor = true;
            this.btn__Entfernen.Click += new System.EventHandler(this.btn_Entfernen_Click);
            // 
            // btn__Hinzu
            // 
            this.btn__Hinzu.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn__Hinzu.Location = new System.Drawing.Point(338, 94);
            this.btn__Hinzu.Margin = new System.Windows.Forms.Padding(4);
            this.btn__Hinzu.Name = "btn__Hinzu";
            this.btn__Hinzu.Size = new System.Drawing.Size(88, 38);
            this.btn__Hinzu.TabIndex = 87;
            this.btn__Hinzu.TabStop = false;
            this.btn__Hinzu.Text = "◀";
            this.btn__Hinzu.UseVisualStyleBackColor = true;
            this.btn__Hinzu.Click += new System.EventHandler(this.btn_Hinzu_Click);
            // 
            // listBox_DB
            // 
            this.listBox_DB.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.listBox_DB.FormattingEnabled = true;
            this.listBox_DB.HorizontalScrollbar = true;
            this.listBox_DB.ItemHeight = 17;
            this.listBox_DB.Location = new System.Drawing.Point(449, 59);
            this.listBox_DB.Margin = new System.Windows.Forms.Padding(4);
            this.listBox_DB.Name = "listBox_DB";
            this.listBox_DB.Size = new System.Drawing.Size(297, 191);
            this.listBox_DB.TabIndex = 86;
            this.listBox_DB.TabStop = false;
            this.listBox_DB.SelectedIndexChanged += new System.EventHandler(this.listBox_DB_SelectedIndexChanged);
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label12.Location = new System.Drawing.Point(19, 36);
            this.label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(136, 19);
            this.label12.TabIndex = 85;
            this.label12.Text = "ausgewählte Module";
            // 
            // listBox_Auswahl
            // 
            this.listBox_Auswahl.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.listBox_Auswahl.FormattingEnabled = true;
            this.listBox_Auswahl.HorizontalScrollbar = true;
            this.listBox_Auswahl.ItemHeight = 17;
            this.listBox_Auswahl.Location = new System.Drawing.Point(21, 59);
            this.listBox_Auswahl.Margin = new System.Windows.Forms.Padding(4);
            this.listBox_Auswahl.Name = "listBox_Auswahl";
            this.listBox_Auswahl.Size = new System.Drawing.Size(309, 191);
            this.listBox_Auswahl.TabIndex = 1;
            this.listBox_Auswahl.SelectedIndexChanged += new System.EventHandler(this.listBox_Auswahl_SelectedIndexChanged);
            // 
            // btn_Abbrechen
            // 
            this.btn_Abbrechen.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_Abbrechen.Location = new System.Drawing.Point(543, 536);
            this.btn_Abbrechen.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.Size = new System.Drawing.Size(90, 34);
            this.btn_Abbrechen.TabIndex = 9;
            this.btn_Abbrechen.Text = "Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // btn_OK
            // 
            this.btn_OK.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_OK.Location = new System.Drawing.Point(656, 536);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(90, 34);
            this.btn_OK.TabIndex = 8;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(449, 252);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(137, 17);
            this.label2.TabIndex = 104;
            this.label2.Text = "Filtern nach Hersteller:";
            // 
            // comboBox_Hersteller
            // 
            this.comboBox_Hersteller.FormattingEnabled = true;
            this.comboBox_Hersteller.Location = new System.Drawing.Point(452, 273);
            this.comboBox_Hersteller.Margin = new System.Windows.Forms.Padding(4);
            this.comboBox_Hersteller.Name = "comboBox_Hersteller";
            this.comboBox_Hersteller.Size = new System.Drawing.Size(228, 25);
            this.comboBox_Hersteller.TabIndex = 103;
            this.comboBox_Hersteller.TabStop = false;
            this.comboBox_Hersteller.SelectedIndexChanged += new System.EventHandler(this.comboBox_Leistung_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.label3.Location = new System.Drawing.Point(8, 10);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(85, 24);
            this.label3.TabIndex = 106;
            this.label3.Text = "Neigung [°]:";
            // 
            // textBox_Neigung
            // 
            this.textBox_Neigung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Neigung.Location = new System.Drawing.Point(93, 8);
            this.textBox_Neigung.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_Neigung.Name = "textBox_Neigung";
            this.textBox_Neigung.Size = new System.Drawing.Size(46, 25);
            this.textBox_Neigung.TabIndex = 1;
            this.textBox_Neigung.Text = "10";
            this.textBox_Neigung.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBox_Neigung.TextChanged += new System.EventHandler(this.textBox_Neigung_TextChanged);
            // 
            // btn_Bearbeiten
            // 
            this.btn_Bearbeiten.Location = new System.Drawing.Point(452, 312);
            this.btn_Bearbeiten.Name = "btn_Bearbeiten";
            this.btn_Bearbeiten.Size = new System.Drawing.Size(134, 32);
            this.btn_Bearbeiten.TabIndex = 6;
            this.btn_Bearbeiten.Text = "Modul Bearbeiten...";
            this.btn_Bearbeiten.UseVisualStyleBackColor = true;
            this.btn_Bearbeiten.Click += new System.EventHandler(this.btn_Bearbeiten_Click);
            // 
            // btn_Löschen
            // 
            this.btn_Löschen.Location = new System.Drawing.Point(612, 312);
            this.btn_Löschen.Name = "btn_Löschen";
            this.btn_Löschen.Size = new System.Drawing.Size(134, 32);
            this.btn_Löschen.TabIndex = 7;
            this.btn_Löschen.Text = "Modul Löschen";
            this.btn_Löschen.UseVisualStyleBackColor = true;
            this.btn_Löschen.Click += new System.EventHandler(this.btn_Löschen_Click);
            // 
            // label6
            // 
            this.label6.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.label6.Location = new System.Drawing.Point(9, 36);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(78, 24);
            this.label6.TabIndex = 111;
            this.label6.Text = "Azimut [°]:";
            // 
            // textBox_Azimut
            // 
            this.textBox_Azimut.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Azimut.Location = new System.Drawing.Point(93, 35);
            this.textBox_Azimut.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_Azimut.Name = "textBox_Azimut";
            this.textBox_Azimut.Size = new System.Drawing.Size(46, 25);
            this.textBox_Azimut.TabIndex = 32;
            this.textBox_Azimut.Text = "10";
            this.textBox_Azimut.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBox_Azimut.TextChanged += new System.EventHandler(this.textBox_Azimut_TextChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.label7.Location = new System.Drawing.Point(177, 10);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(105, 17);
            this.label7.TabIndex = 115;
            this.label7.Text = "Anzahl Module:";
            // 
            // textBox_AnlagenLeistung
            // 
            this.textBox_AnlagenLeistung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_AnlagenLeistung.Location = new System.Drawing.Point(180, 34);
            this.textBox_AnlagenLeistung.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_AnlagenLeistung.Name = "textBox_AnlagenLeistung";
            this.textBox_AnlagenLeistung.Size = new System.Drawing.Size(67, 25);
            this.textBox_AnlagenLeistung.TabIndex = 3;
            this.textBox_AnlagenLeistung.Text = "10";
            this.textBox_AnlagenLeistung.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBox_AnlagenLeistung.TextChanged += new System.EventHandler(this.textBox_AnlagenLeistung_TextChanged);
            // 
            // label_Type
            // 
            this.label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.label_Type.Dock = System.Windows.Forms.DockStyle.Top;
            this.label_Type.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Type.Location = new System.Drawing.Point(0, 0);
            this.label_Type.Name = "label_Type";
            this.label_Type.Padding = new System.Windows.Forms.Padding(0, 5, 0, 5);
            this.label_Type.Size = new System.Drawing.Size(762, 31);
            this.label_Type.TabIndex = 118;
            this.label_Type.Text = "Eingabe der Photovoltaik Anlagendaten";
            this.label_Type.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label18.Location = new System.Drawing.Point(5, 37);
            this.label18.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(94, 19);
            this.label18.TabIndex = 106;
            this.label18.Text = "Beschreibung:";
            // 
            // textBox_Beschreibung
            // 
            this.textBox_Beschreibung.Enabled = false;
            this.textBox_Beschreibung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Beschreibung.Location = new System.Drawing.Point(102, 37);
            this.textBox_Beschreibung.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_Beschreibung.Multiline = true;
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            this.textBox_Beschreibung.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox_Beschreibung.Size = new System.Drawing.Size(300, 59);
            this.textBox_Beschreibung.TabIndex = 105;
            this.textBox_Beschreibung.TabStop = false;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label14.Location = new System.Drawing.Point(5, 132);
            this.label14.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(141, 19);
            this.label14.TabIndex = 104;
            this.label14.Text = "Modul Leistung [KW]:";
            // 
            // textBox_Leistung
            // 
            this.textBox_Leistung.Enabled = false;
            this.textBox_Leistung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Leistung.Location = new System.Drawing.Point(150, 131);
            this.textBox_Leistung.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_Leistung.Name = "textBox_Leistung";
            this.textBox_Leistung.Size = new System.Drawing.Size(67, 25);
            this.textBox_Leistung.TabIndex = 103;
            this.textBox_Leistung.TabStop = false;
            this.textBox_Leistung.Text = "10";
            this.textBox_Leistung.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label15.Location = new System.Drawing.Point(5, 101);
            this.label15.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(70, 19);
            this.label15.TabIndex = 102;
            this.label15.Text = "Hersteller:";
            // 
            // textBox_Hersteller
            // 
            this.textBox_Hersteller.Enabled = false;
            this.textBox_Hersteller.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Hersteller.Location = new System.Drawing.Point(102, 100);
            this.textBox_Hersteller.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_Hersteller.Name = "textBox_Hersteller";
            this.textBox_Hersteller.Size = new System.Drawing.Size(206, 25);
            this.textBox_Hersteller.TabIndex = 101;
            this.textBox_Hersteller.TabStop = false;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label16.Location = new System.Drawing.Point(5, 10);
            this.label16.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(48, 19);
            this.label16.TabIndex = 100;
            this.label16.Text = "Name:";
            // 
            // textBox_Name
            // 
            this.textBox_Name.Enabled = false;
            this.textBox_Name.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Name.Location = new System.Drawing.Point(102, 8);
            this.textBox_Name.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_Name.Name = "textBox_Name";
            this.textBox_Name.Size = new System.Drawing.Size(300, 25);
            this.textBox_Name.TabIndex = 99;
            this.textBox_Name.TabStop = false;
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.textBox_AnlagenLeistung);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.label7);
            this.panel1.Controls.Add(this.textBox_Azimut);
            this.panel1.Controls.Add(this.textBox_Neigung);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Location = new System.Drawing.Point(22, 273);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(308, 71);
            this.panel1.TabIndex = 120;
            this.panel1.Leave += new System.EventHandler(this.panel1_Leave);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 255);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(154, 17);
            this.label1.TabIndex = 121;
            this.label1.Text = "PV Anlage Eigenschaften:";
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.label5);
            this.panel2.Controls.Add(this.textBox_Gesamtleistung);
            this.panel2.Controls.Add(this.label18);
            this.panel2.Controls.Add(this.textBox_Beschreibung);
            this.panel2.Controls.Add(this.textBox_Name);
            this.panel2.Controls.Add(this.label14);
            this.panel2.Controls.Add(this.label16);
            this.panel2.Controls.Add(this.textBox_Leistung);
            this.panel2.Controls.Add(this.textBox_Hersteller);
            this.panel2.Controls.Add(this.label15);
            this.panel2.Location = new System.Drawing.Point(22, 375);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(411, 191);
            this.panel2.TabIndex = 122;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label5.Location = new System.Drawing.Point(5, 160);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(140, 19);
            this.label5.TabIndex = 108;
            this.label5.Text = "Gesamtleistung [KW]:";
            // 
            // textBox_Gesamtleistung
            // 
            this.textBox_Gesamtleistung.Enabled = false;
            this.textBox_Gesamtleistung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Gesamtleistung.Location = new System.Drawing.Point(150, 159);
            this.textBox_Gesamtleistung.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_Gesamtleistung.Name = "textBox_Gesamtleistung";
            this.textBox_Gesamtleistung.Size = new System.Drawing.Size(67, 25);
            this.textBox_Gesamtleistung.TabIndex = 107;
            this.textBox_Gesamtleistung.TabStop = false;
            this.textBox_Gesamtleistung.Text = "10";
            this.textBox_Gesamtleistung.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(22, 356);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(133, 17);
            this.label4.TabIndex = 123;
            this.label4.Text = "Modul Eigenschaften:";
            // 
            // btn_Help
            // 
            this.btn_Help.BackColor = System.Drawing.Color.Transparent;
            this.btn_Help.BackgroundImage = Properties.Resources.help_icon;
            this.btn_Help.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btn_Help.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_Help.FlatAppearance.BorderSize = 0;
            this.btn_Help.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_Help.Location = new System.Drawing.Point(720, 33);
            this.btn_Help.Name = "btn_Help";
            this.btn_Help.Size = new System.Drawing.Size(24, 24);
            this.btn_Help.TabStop = false;
            this.btn_Help.UseVisualStyleBackColor = false;
            // 
            // Form_PV
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.ClientSize = new System.Drawing.Size(762, 582);
            this.Controls.Add(this.btn_Help);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label_Type);
            this.Controls.Add(this.btn_Löschen);
            this.Controls.Add(this.btn_Bearbeiten);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboBox_Hersteller);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.btn__Entfernen);
            this.Controls.Add(this.btn__Hinzu);
            this.Controls.Add(this.listBox_DB);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.listBox_Auswahl);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Form_PV";
            this.Text = "Verwaltung Photovoltaik Module";
            this.Load += new System.EventHandler(this.Form_PV_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.Form_PV_Paint);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btn_Help;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Button btn__Entfernen;
        private System.Windows.Forms.Button btn__Hinzu;
        private System.Windows.Forms.ListBox listBox_DB;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.ListBox listBox_Auswahl;
        private System.Windows.Forms.Button btn_Abbrechen;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBox_Hersteller;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox_Neigung;
        private System.Windows.Forms.Button btn_Bearbeiten;
        private System.Windows.Forms.Button btn_Löschen;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBox_Azimut;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textBox_AnlagenLeistung;
        private System.Windows.Forms.Label label_Type;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.TextBox textBox_Beschreibung;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox textBox_Leistung;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox textBox_Hersteller;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TextBox textBox_Name;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox_Gesamtleistung;
    }
}