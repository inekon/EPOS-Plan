namespace WindowsFormsApplication1
{
    partial class Form_Gebaeude
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_Gebaeude));
            this.btn_Entfernen = new System.Windows.Forms.Button();
            this.btn_Hinzu = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.radioBtn_Sonstige = new System.Windows.Forms.RadioButton();
            this.radioBtn_Wohngebäude = new System.Windows.Forms.RadioButton();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.comboBox_Baujahr = new System.Windows.Forms.ComboBox();
            this.comboBox_Gebäudeart = new System.Windows.Forms.ComboBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.textBox_TypEinheit = new System.Windows.Forms.TextBox();
            this.textBox_Wohnflaeche = new System.Windows.Forms.TextBox();
            this.btn_Aendern = new System.Windows.Forms.Button();
            this.textBox_Gebäudename = new System.Windows.Forms.TextBox();
            this.textBox_Gebaeudeart = new System.Windows.Forms.TextBox();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label_ListProjektGebaeude = new System.Windows.Forms.Label();
            this.label_ListGebaeudeDB = new System.Windows.Forms.Label();
            this.textBox_Baujahr = new System.Windows.Forms.TextBox();
            this.textBox_Jahresnutzungsgrad = new System.Windows.Forms.TextBox();
            this.checkBox_dezWarmwasser = new System.Windows.Forms.CheckBox();
            this.textBox_ID_Gebaeude = new System.Windows.Forms.TextBox();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.listView_Gebaeude = new System.Windows.Forms.ListView();
            this.btn_GebAendern_DB = new System.Windows.Forms.Button();
            this.btn_Geb_Neu_DB = new System.Windows.Forms.Button();
            this.btn_GebLoeschen_DB = new System.Windows.Forms.Button();
            this.btn_GebTypAendern_DB = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.label_Type = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // btn_Entfernen
            // 
            this.btn_Entfernen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Entfernen.Location = new System.Drawing.Point(284, 158);
            this.btn_Entfernen.Name = "btn_Entfernen";
            this.btn_Entfernen.Size = new System.Drawing.Size(74, 28);
            this.btn_Entfernen.TabIndex = 28;
            this.btn_Entfernen.Text = "-->";
            this.btn_Entfernen.UseVisualStyleBackColor = true;
            this.btn_Entfernen.Click += new System.EventHandler(this.btn_Entfernen_Click);
            // 
            // btn_Hinzu
            // 
            this.btn_Hinzu.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Hinzu.Location = new System.Drawing.Point(284, 123);
            this.btn_Hinzu.Name = "btn_Hinzu";
            this.btn_Hinzu.Size = new System.Drawing.Size(74, 29);
            this.btn_Hinzu.TabIndex = 27;
            this.btn_Hinzu.Text = "<---";
            this.btn_Hinzu.UseVisualStyleBackColor = true;
            this.btn_Hinzu.Click += new System.EventHandler(this.btn_Hinzu_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.radioBtn_Sonstige);
            this.groupBox1.Controls.Add(this.radioBtn_Wohngebäude);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.comboBox_Baujahr);
            this.groupBox1.Controls.Add(this.comboBox_Gebäudeart);
            this.groupBox1.Location = new System.Drawing.Point(356, 334);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(283, 122);
            this.groupBox1.TabIndex = 29;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Filter Gebäude DB";
            // 
            // radioBtn_Sonstige
            // 
            this.radioBtn_Sonstige.AutoSize = true;
            this.radioBtn_Sonstige.Location = new System.Drawing.Point(141, 24);
            this.radioBtn_Sonstige.Name = "radioBtn_Sonstige";
            this.radioBtn_Sonstige.Size = new System.Drawing.Size(143, 23);
            this.radioBtn_Sonstige.TabIndex = 5;
            this.radioBtn_Sonstige.Text = "Gewerbe+Sonstige";
            this.radioBtn_Sonstige.UseVisualStyleBackColor = true;
            this.radioBtn_Sonstige.Click += new System.EventHandler(this.radioBtn_Sonstige_CheckedChanged);
            // 
            // radioBtn_Wohngebäude
            // 
            this.radioBtn_Wohngebäude.AutoSize = true;
            this.radioBtn_Wohngebäude.Checked = true;
            this.radioBtn_Wohngebäude.Location = new System.Drawing.Point(18, 24);
            this.radioBtn_Wohngebäude.Name = "radioBtn_Wohngebäude";
            this.radioBtn_Wohngebäude.Size = new System.Drawing.Size(116, 23);
            this.radioBtn_Wohngebäude.TabIndex = 4;
            this.radioBtn_Wohngebäude.TabStop = true;
            this.radioBtn_Wohngebäude.Text = "Wohngebäude";
            this.radioBtn_Wohngebäude.UseVisualStyleBackColor = true;
            this.radioBtn_Wohngebäude.Click += new System.EventHandler(this.radioBtn_Wohngebäude_CheckedChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 83);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(55, 19);
            this.label2.TabIndex = 3;
            this.label2.Text = "Baujahr";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(11, 52);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(81, 19);
            this.label1.TabIndex = 2;
            this.label1.Text = "Gebäudeart";
            // 
            // comboBox_Baujahr
            // 
            this.comboBox_Baujahr.FormattingEnabled = true;
            this.comboBox_Baujahr.Location = new System.Drawing.Point(109, 83);
            this.comboBox_Baujahr.Name = "comboBox_Baujahr";
            this.comboBox_Baujahr.Size = new System.Drawing.Size(159, 25);
            this.comboBox_Baujahr.TabIndex = 1;
            this.comboBox_Baujahr.SelectedIndexChanged += new System.EventHandler(this.comboBox_Baujahr_SelectedIndexChanged);
            // 
            // comboBox_Gebäudeart
            // 
            this.comboBox_Gebäudeart.FormattingEnabled = true;
            this.comboBox_Gebäudeart.Location = new System.Drawing.Point(109, 52);
            this.comboBox_Gebäudeart.Name = "comboBox_Gebäudeart";
            this.comboBox_Gebäudeart.Size = new System.Drawing.Size(159, 25);
            this.comboBox_Gebäudeart.TabIndex = 0;
            this.comboBox_Gebäudeart.SelectedIndexChanged += new System.EventHandler(this.comboBox_Gebäudeart_SelectedIndexChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.textBox_TypEinheit);
            this.groupBox2.Controls.Add(this.textBox_Wohnflaeche);
            this.groupBox2.Controls.Add(this.btn_Aendern);
            this.groupBox2.Location = new System.Drawing.Point(26, 262);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(310, 124);
            this.groupBox2.TabIndex = 30;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Gebäude: Verbrauch";
            // 
            // textBox_TypEinheit
            // 
            this.textBox_TypEinheit.Enabled = false;
            this.textBox_TypEinheit.Location = new System.Drawing.Point(100, 35);
            this.textBox_TypEinheit.Name = "textBox_TypEinheit";
            this.textBox_TypEinheit.Size = new System.Drawing.Size(193, 25);
            this.textBox_TypEinheit.TabIndex = 32;
            // 
            // textBox_Wohnflaeche
            // 
            this.textBox_Wohnflaeche.Enabled = false;
            this.textBox_Wohnflaeche.Location = new System.Drawing.Point(15, 35);
            this.textBox_Wohnflaeche.Name = "textBox_Wohnflaeche";
            this.textBox_Wohnflaeche.Size = new System.Drawing.Size(79, 25);
            this.textBox_Wohnflaeche.TabIndex = 31;
            // 
            // btn_Aendern
            // 
            this.btn_Aendern.Location = new System.Drawing.Point(15, 79);
            this.btn_Aendern.Name = "btn_Aendern";
            this.btn_Aendern.Size = new System.Drawing.Size(278, 28);
            this.btn_Aendern.TabIndex = 31;
            this.btn_Aendern.Text = "Ändern";
            this.btn_Aendern.UseVisualStyleBackColor = true;
            this.btn_Aendern.Click += new System.EventHandler(this.btn_Aendern_Click);
            // 
            // textBox_Gebäudename
            // 
            this.textBox_Gebäudename.Enabled = false;
            this.textBox_Gebäudename.Location = new System.Drawing.Point(126, 402);
            this.textBox_Gebäudename.Name = "textBox_Gebäudename";
            this.textBox_Gebäudename.Size = new System.Drawing.Size(189, 25);
            this.textBox_Gebäudename.TabIndex = 31;
            // 
            // textBox_Gebaeudeart
            // 
            this.textBox_Gebaeudeart.Enabled = false;
            this.textBox_Gebaeudeart.Location = new System.Drawing.Point(126, 442);
            this.textBox_Gebaeudeart.Name = "textBox_Gebaeudeart";
            this.textBox_Gebaeudeart.Size = new System.Drawing.Size(189, 25);
            this.textBox_Gebaeudeart.TabIndex = 32;
            // 
            // textBox_Beschreibung
            // 
            this.textBox_Beschreibung.Enabled = false;
            this.textBox_Beschreibung.Location = new System.Drawing.Point(126, 483);
            this.textBox_Beschreibung.Multiline = true;
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            this.textBox_Beschreibung.Size = new System.Drawing.Size(478, 80);
            this.textBox_Beschreibung.TabIndex = 33;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(35, 442);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(81, 19);
            this.label3.TabIndex = 6;
            this.label3.Text = "Gebäudeart";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(22, 402);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(98, 19);
            this.label4.TabIndex = 34;
            this.label4.Text = "Gebäudename";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(22, 486);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(91, 19);
            this.label5.TabIndex = 35;
            this.label5.Text = "Beschreibung";
            // 
            // label_ListProjektGebaeude
            // 
            this.label_ListProjektGebaeude.AutoSize = true;
            this.label_ListProjektGebaeude.Location = new System.Drawing.Point(22, 45);
            this.label_ListProjektGebaeude.Name = "label_ListProjektGebaeude";
            this.label_ListProjektGebaeude.Size = new System.Drawing.Size(210, 19);
            this.label_ListProjektGebaeude.TabIndex = 6;
            this.label_ListProjektGebaeude.Text = "ausgewählte Gebäude im Projekt";
            // 
            // label_ListGebaeudeDB
            // 
            this.label_ListGebaeudeDB.AutoSize = true;
            this.label_ListGebaeudeDB.Location = new System.Drawing.Point(366, 45);
            this.label_ListGebaeudeDB.Name = "label_ListGebaeudeDB";
            this.label_ListGebaeudeDB.Size = new System.Drawing.Size(101, 19);
            this.label_ListGebaeudeDB.TabIndex = 36;
            this.label_ListGebaeudeDB.Text = "Gebäude in DB";
            // 
            // textBox_Baujahr
            // 
            this.textBox_Baujahr.Location = new System.Drawing.Point(295, 58);
            this.textBox_Baujahr.Name = "textBox_Baujahr";
            this.textBox_Baujahr.Size = new System.Drawing.Size(52, 25);
            this.textBox_Baujahr.TabIndex = 37;
            this.textBox_Baujahr.Text = "Baujahr";
            this.textBox_Baujahr.Visible = false;
            // 
            // textBox_Jahresnutzungsgrad
            // 
            this.textBox_Jahresnutzungsgrad.Location = new System.Drawing.Point(353, 58);
            this.textBox_Jahresnutzungsgrad.Name = "textBox_Jahresnutzungsgrad";
            this.textBox_Jahresnutzungsgrad.Size = new System.Drawing.Size(69, 25);
            this.textBox_Jahresnutzungsgrad.TabIndex = 38;
            this.textBox_Jahresnutzungsgrad.Text = "Jahresnutzungsgrad";
            this.textBox_Jahresnutzungsgrad.Visible = false;
            // 
            // checkBox_dezWarmwasser
            // 
            this.checkBox_dezWarmwasser.AutoSize = true;
            this.checkBox_dezWarmwasser.Location = new System.Drawing.Point(284, 67);
            this.checkBox_dezWarmwasser.Name = "checkBox_dezWarmwasser";
            this.checkBox_dezWarmwasser.Size = new System.Drawing.Size(126, 23);
            this.checkBox_dezWarmwasser.TabIndex = 39;
            this.checkBox_dezWarmwasser.Text = "dezWarmwasser";
            this.checkBox_dezWarmwasser.UseVisualStyleBackColor = true;
            this.checkBox_dezWarmwasser.Visible = false;
            // 
            // textBox_ID_Gebaeude
            // 
            this.textBox_ID_Gebaeude.Location = new System.Drawing.Point(428, 58);
            this.textBox_ID_Gebaeude.Name = "textBox_ID_Gebaeude";
            this.textBox_ID_Gebaeude.Size = new System.Drawing.Size(63, 25);
            this.textBox_ID_Gebaeude.TabIndex = 40;
            this.textBox_ID_Gebaeude.Text = "ID_Gebaeude";
            this.textBox_ID_Gebaeude.Visible = false;
            // 
            // btn_Abbrechen
            // 
            this.btn_Abbrechen.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_Abbrechen.Location = new System.Drawing.Point(618, 532);
            this.btn_Abbrechen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.Size = new System.Drawing.Size(91, 31);
            this.btn_Abbrechen.TabIndex = 42;
            this.btn_Abbrechen.Text = "Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // btn_OK
            // 
            this.btn_OK.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_OK.Location = new System.Drawing.Point(723, 532);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(77, 31);
            this.btn_OK.TabIndex = 43;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // listView_Gebaeude
            // 
            this.listView_Gebaeude.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.listView_Gebaeude.HideSelection = false;
            this.listView_Gebaeude.Location = new System.Drawing.Point(26, 67);
            this.listView_Gebaeude.Name = "listView_Gebaeude";
            this.listView_Gebaeude.Size = new System.Drawing.Size(252, 189);
            this.listView_Gebaeude.TabIndex = 44;
            this.listView_Gebaeude.UseCompatibleStateImageBehavior = false;
            this.listView_Gebaeude.SelectedIndexChanged += new System.EventHandler(this.listView_Gebaeude_SelectedIndexChanged);
            // 
            // btn_GebAendern_DB
            // 
            this.btn_GebAendern_DB.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_GebAendern_DB.Location = new System.Drawing.Point(646, 338);
            this.btn_GebAendern_DB.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_GebAendern_DB.Name = "btn_GebAendern_DB";
            this.btn_GebAendern_DB.Size = new System.Drawing.Size(155, 33);
            this.btn_GebAendern_DB.TabIndex = 45;
            this.btn_GebAendern_DB.Text = "Gebäude in DB ändern...";
            this.btn_GebAendern_DB.UseVisualStyleBackColor = true;
            this.btn_GebAendern_DB.Click += new System.EventHandler(this.btn_GebAendern_DB_Click);
            // 
            // btn_Geb_Neu_DB
            // 
            this.btn_Geb_Neu_DB.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_Geb_Neu_DB.Location = new System.Drawing.Point(646, 372);
            this.btn_Geb_Neu_DB.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Geb_Neu_DB.Name = "btn_Geb_Neu_DB";
            this.btn_Geb_Neu_DB.Size = new System.Drawing.Size(155, 33);
            this.btn_Geb_Neu_DB.TabIndex = 46;
            this.btn_Geb_Neu_DB.Text = "Gebäude in DB neu...";
            this.btn_Geb_Neu_DB.UseVisualStyleBackColor = true;
            this.btn_Geb_Neu_DB.Click += new System.EventHandler(this.btn_Geb_Neu_DB_Click);
            // 
            // btn_GebLoeschen_DB
            // 
            this.btn_GebLoeschen_DB.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_GebLoeschen_DB.Location = new System.Drawing.Point(646, 406);
            this.btn_GebLoeschen_DB.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_GebLoeschen_DB.Name = "btn_GebLoeschen_DB";
            this.btn_GebLoeschen_DB.Size = new System.Drawing.Size(155, 33);
            this.btn_GebLoeschen_DB.TabIndex = 47;
            this.btn_GebLoeschen_DB.Text = "Gebäude in DB löschen";
            this.btn_GebLoeschen_DB.UseVisualStyleBackColor = true;
            this.btn_GebLoeschen_DB.Click += new System.EventHandler(this.btn_GebLoeschen_DB_Click);
            // 
            // btn_GebTypAendern_DB
            // 
            this.btn_GebTypAendern_DB.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_GebTypAendern_DB.Location = new System.Drawing.Point(646, 440);
            this.btn_GebTypAendern_DB.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_GebTypAendern_DB.Name = "btn_GebTypAendern_DB";
            this.btn_GebTypAendern_DB.Size = new System.Drawing.Size(155, 45);
            this.btn_GebTypAendern_DB.TabIndex = 48;
            this.btn_GebTypAendern_DB.Text = "Gebäudetyp in DB ändern...";
            this.btn_GebTypAendern_DB.UseVisualStyleBackColor = true;
            this.btn_GebTypAendern_DB.Click += new System.EventHandler(this.btn_GebTypAendern_DB_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBox1.BackgroundImage")));
            this.pictureBox1.Location = new System.Drawing.Point(26, 73);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(247, 183);
            this.pictureBox1.TabIndex = 49;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Visible = false;
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.BackgroundColor = System.Drawing.Color.Silver;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(364, 67);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(436, 261);
            this.dataGridView1.TabIndex = 75;
            this.dataGridView1.Click += new System.EventHandler(this.listBox_Gebaeude_DB_SelectedIndexChanged);
            // 
            // label_Type
            // 
            this.label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.label_Type.Dock = System.Windows.Forms.DockStyle.Top;
            this.label_Type.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Type.Location = new System.Drawing.Point(0, 0);
            this.label_Type.Name = "label_Type";
            this.label_Type.Padding = new System.Windows.Forms.Padding(0, 5, 0, 5);
            this.label_Type.Size = new System.Drawing.Size(812, 32);
            this.label_Type.TabIndex = 76;
            this.label_Type.Text = "Eingabe der Energiedaten";
            this.label_Type.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // Form_Gebaeude
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(812, 573);
            this.Controls.Add(this.label_Type);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.listView_Gebaeude);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.btn_GebTypAendern_DB);
            this.Controls.Add(this.btn_GebLoeschen_DB);
            this.Controls.Add(this.btn_Geb_Neu_DB);
            this.Controls.Add(this.btn_GebAendern_DB);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.textBox_ID_Gebaeude);
            this.Controls.Add(this.checkBox_dezWarmwasser);
            this.Controls.Add(this.textBox_Jahresnutzungsgrad);
            this.Controls.Add(this.textBox_Baujahr);
            this.Controls.Add(this.label_ListGebaeudeDB);
            this.Controls.Add(this.label_ListProjektGebaeude);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBox_Beschreibung);
            this.Controls.Add(this.textBox_Gebaeudeart);
            this.Controls.Add(this.textBox_Gebäudename);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btn_Entfernen);
            this.Controls.Add(this.btn_Hinzu);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Name = "Form_Gebaeude";
            this.Text = "Eingabe der Gebäudedaten";
            this.Load += new System.EventHandler(this.Form_Gebaeude_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btn_Entfernen;
        private System.Windows.Forms.Button btn_Hinzu;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton radioBtn_Sonstige;
        private System.Windows.Forms.RadioButton radioBtn_Wohngebäude;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBox_Baujahr;
        private System.Windows.Forms.ComboBox comboBox_Gebäudeart;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox textBox_TypEinheit;
        private System.Windows.Forms.TextBox textBox_Wohnflaeche;
        private System.Windows.Forms.Button btn_Aendern;
        private System.Windows.Forms.TextBox textBox_Gebäudename;
        private System.Windows.Forms.TextBox textBox_Gebaeudeart;
        private System.Windows.Forms.TextBox textBox_Beschreibung;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label_ListProjektGebaeude;
        private System.Windows.Forms.Label label_ListGebaeudeDB;
        private System.Windows.Forms.TextBox textBox_Baujahr;
        private System.Windows.Forms.TextBox textBox_Jahresnutzungsgrad;
        private System.Windows.Forms.CheckBox checkBox_dezWarmwasser;
        private System.Windows.Forms.TextBox textBox_ID_Gebaeude;
        private System.Windows.Forms.Button btn_Abbrechen;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.ListView listView_Gebaeude;
        private System.Windows.Forms.Button btn_GebAendern_DB;
        private System.Windows.Forms.Button btn_Geb_Neu_DB;
        private System.Windows.Forms.Button btn_GebLoeschen_DB;
        private System.Windows.Forms.Button btn_GebTypAendern_DB;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Label label_Type;
    }
}