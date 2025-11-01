namespace WindowsFormsApplication1
{
    partial class Form_WPEing 
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
            this.Label1 = new System.Windows.Forms.Label();
            this.listBox_WPEing = new System.Windows.Forms.ListBox();
            this.listBox_WP_DB = new System.Windows.Forms.ListBox();
            this.btn_Hinzu = new System.Windows.Forms.Button();
            this.btn_Entfernen = new System.Windows.Forms.Button();
            this.Label23 = new System.Windows.Forms.Label();
            this.comboBox_Leistungsfilter = new System.Windows.Forms.ComboBox();
            this.btn_DBWP_Edit = new System.Windows.Forms.Button();
            this.btn_DBWP_Neu = new System.Windows.Forms.Button();
            this.btn_DBWP_Löschen = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.Label7 = new System.Windows.Forms.Label();
            this.Label8 = new System.Windows.Forms.Label();
            this.textBox_Summe_Leistung = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.Label19 = new System.Windows.Forms.Label();
            this.Label17 = new System.Windows.Forms.Label();
            this.Label18 = new System.Windows.Forms.Label();
            this.Label20 = new System.Windows.Forms.Label();
            this.tb_Volumen_Pendelsp = new System.Windows.Forms.TextBox();
            this.tb_Größe_Pendelsp = new System.Windows.Forms.TextBox();
            this.cb_Rendemix = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox_Betriebsart = new System.Windows.Forms.TextBox();
            this.Label12 = new System.Windows.Forms.Label();
            this.Label10 = new System.Windows.Forms.Label();
            this.textBox_WPName = new System.Windows.Forms.TextBox();
            this.Label13 = new System.Windows.Forms.Label();
            this.textBox_Leistung_th = new System.Windows.Forms.TextBox();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.textBox_Firma = new System.Windows.Forms.TextBox();
            this.textBox_Leistung_el = new System.Windows.Forms.TextBox();
            this.Label14 = new System.Windows.Forms.Label();
            this.Label15 = new System.Windows.Forms.Label();
            this.Label16 = new System.Windows.Forms.Label();
            this.Label11 = new System.Windows.Forms.Label();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // Label1
            // 
            this.Label1.AutoSize = true;
            this.Label1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label1.Location = new System.Drawing.Point(20, 33);
            this.Label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label1.Name = "Label1";
            this.Label1.Size = new System.Drawing.Size(133, 17);
            this.Label1.TabIndex = 0;
            this.Label1.Text = "Ausgewählte Module:";
            this.Label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // listBox_WPEing
            // 
            this.listBox_WPEing.ItemHeight = 17;
            this.listBox_WPEing.Location = new System.Drawing.Point(21, 54);
            this.listBox_WPEing.Margin = new System.Windows.Forms.Padding(4);
            this.listBox_WPEing.Name = "listBox_WPEing";
            this.listBox_WPEing.Size = new System.Drawing.Size(305, 174);
            this.listBox_WPEing.TabIndex = 5;
            // 
            // listBox_WP_DB
            // 
            this.listBox_WP_DB.ItemHeight = 17;
            this.listBox_WP_DB.Location = new System.Drawing.Point(600, 54);
            this.listBox_WP_DB.Margin = new System.Windows.Forms.Padding(4);
            this.listBox_WP_DB.Name = "listBox_WP_DB";
            this.listBox_WP_DB.Size = new System.Drawing.Size(305, 174);
            this.listBox_WP_DB.TabIndex = 6;
            this.listBox_WP_DB.SelectedIndexChanged += new System.EventHandler(this.listBox_WP_DB_SelectedIndexChanged);
            // 
            // btn_Hinzu
            // 
            this.btn_Hinzu.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_Hinzu.Location = new System.Drawing.Point(339, 103);
            this.btn_Hinzu.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Hinzu.Name = "btn_Hinzu";
            this.btn_Hinzu.Size = new System.Drawing.Size(246, 31);
            this.btn_Hinzu.TabIndex = 8;
            this.btn_Hinzu.Text = "<-------  aus der Datenbank hinzufügen";
            this.btn_Hinzu.UseVisualStyleBackColor = true;
            // 
            // btn_Entfernen
            // 
            this.btn_Entfernen.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_Entfernen.Location = new System.Drawing.Point(339, 142);
            this.btn_Entfernen.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Entfernen.Name = "btn_Entfernen";
            this.btn_Entfernen.Size = new System.Drawing.Size(246, 31);
            this.btn_Entfernen.TabIndex = 9;
            this.btn_Entfernen.Text = "Aus der Auswahl löschen ------>";
            this.btn_Entfernen.UseVisualStyleBackColor = true;
            // 
            // Label23
            // 
            this.Label23.AutoSize = true;
            this.Label23.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label23.Location = new System.Drawing.Point(559, 248);
            this.Label23.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label23.Name = "Label23";
            this.Label23.Size = new System.Drawing.Size(198, 17);
            this.Label23.TabIndex = 13;
            this.Label23.Text = "Filtern nach thermischer Leistung";
            this.Label23.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // comboBox_Leistungsfilter
            // 
            this.comboBox_Leistungsfilter.Location = new System.Drawing.Point(765, 249);
            this.comboBox_Leistungsfilter.Margin = new System.Windows.Forms.Padding(4);
            this.comboBox_Leistungsfilter.Name = "comboBox_Leistungsfilter";
            this.comboBox_Leistungsfilter.Size = new System.Drawing.Size(140, 25);
            this.comboBox_Leistungsfilter.TabIndex = 15;
            // 
            // btn_DBWP_Edit
            // 
            this.btn_DBWP_Edit.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_DBWP_Edit.Location = new System.Drawing.Point(741, 317);
            this.btn_DBWP_Edit.Margin = new System.Windows.Forms.Padding(4);
            this.btn_DBWP_Edit.Name = "btn_DBWP_Edit";
            this.btn_DBWP_Edit.Size = new System.Drawing.Size(164, 31);
            this.btn_DBWP_Edit.TabIndex = 16;
            this.btn_DBWP_Edit.Text = "WP in DB ändern...";
            this.btn_DBWP_Edit.UseVisualStyleBackColor = true;
            // 
            // btn_DBWP_Neu
            // 
            this.btn_DBWP_Neu.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_DBWP_Neu.Location = new System.Drawing.Point(741, 356);
            this.btn_DBWP_Neu.Margin = new System.Windows.Forms.Padding(4);
            this.btn_DBWP_Neu.Name = "btn_DBWP_Neu";
            this.btn_DBWP_Neu.Size = new System.Drawing.Size(164, 31);
            this.btn_DBWP_Neu.TabIndex = 17;
            this.btn_DBWP_Neu.Text = "WP in DB neu...";
            this.btn_DBWP_Neu.UseVisualStyleBackColor = true;
            // 
            // btn_DBWP_Löschen
            // 
            this.btn_DBWP_Löschen.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_DBWP_Löschen.Location = new System.Drawing.Point(741, 395);
            this.btn_DBWP_Löschen.Margin = new System.Windows.Forms.Padding(4);
            this.btn_DBWP_Löschen.Name = "btn_DBWP_Löschen";
            this.btn_DBWP_Löschen.Size = new System.Drawing.Size(164, 31);
            this.btn_DBWP_Löschen.TabIndex = 18;
            this.btn_DBWP_Löschen.Text = "WP in DB löschen";
            this.btn_DBWP_Löschen.UseVisualStyleBackColor = true;
            // 
            // btn_OK
            // 
            this.btn_OK.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_OK.Location = new System.Drawing.Point(814, 681);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(4);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(91, 31);
            this.btn_OK.TabIndex = 25;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // Label7
            // 
            this.Label7.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label7.Location = new System.Drawing.Point(23, 238);
            this.Label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label7.Name = "Label7";
            this.Label7.Size = new System.Drawing.Size(159, 36);
            this.Label7.TabIndex = 48;
            this.Label7.Text = "Summe aller ausgewählten Module:";
            this.Label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label8
            // 
            this.Label8.AutoSize = true;
            this.Label8.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label8.Location = new System.Drawing.Point(265, 246);
            this.Label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label8.Name = "Label8";
            this.Label8.Size = new System.Drawing.Size(37, 17);
            this.Label8.TabIndex = 49;
            this.Label8.Text = "kWth";
            this.Label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBox_Summe_Leistung
            // 
            this.textBox_Summe_Leistung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Summe_Leistung.Enabled = false;
            this.textBox_Summe_Leistung.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Summe_Leistung.Location = new System.Drawing.Point(193, 243);
            this.textBox_Summe_Leistung.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Summe_Leistung.Name = "textBox_Summe_Leistung";
            this.textBox_Summe_Leistung.Size = new System.Drawing.Size(70, 25);
            this.textBox_Summe_Leistung.TabIndex = 50;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.Label19);
            this.groupBox1.Controls.Add(this.Label17);
            this.groupBox1.Controls.Add(this.Label18);
            this.groupBox1.Controls.Add(this.Label20);
            this.groupBox1.Controls.Add(this.tb_Volumen_Pendelsp);
            this.groupBox1.Controls.Add(this.tb_Größe_Pendelsp);
            this.groupBox1.Controls.Add(this.cb_Rendemix);
            this.groupBox1.Location = new System.Drawing.Point(23, 604);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(430, 108);
            this.groupBox1.TabIndex = 51;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Eingabe zu Pufferspeicher";
            // 
            // Label19
            // 
            this.Label19.AutoSize = true;
            this.Label19.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label19.Location = new System.Drawing.Point(21, 31);
            this.Label19.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label19.Name = "Label19";
            this.Label19.Size = new System.Drawing.Size(61, 17);
            this.Label19.TabIndex = 36;
            this.Label19.Text = "Volumen:";
            this.Label19.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label17
            // 
            this.Label17.AutoSize = true;
            this.Label17.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label17.Location = new System.Drawing.Point(361, 32);
            this.Label17.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label17.Name = "Label17";
            this.Label17.Size = new System.Drawing.Size(37, 17);
            this.Label17.TabIndex = 37;
            this.Label17.Text = "kWth";
            this.Label17.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label18
            // 
            this.Label18.AutoSize = true;
            this.Label18.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label18.Location = new System.Drawing.Point(187, 31);
            this.Label18.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label18.Name = "Label18";
            this.Label18.Size = new System.Drawing.Size(24, 17);
            this.Label18.TabIndex = 38;
            this.Label18.Text = "m³";
            this.Label18.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label20
            // 
            this.Label20.AutoSize = true;
            this.Label20.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label20.Location = new System.Drawing.Point(226, 32);
            this.Label20.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label20.Name = "Label20";
            this.Label20.Size = new System.Drawing.Size(65, 17);
            this.Label20.TabIndex = 39;
            this.Label20.Text = "Kapazität:";
            this.Label20.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tb_Volumen_Pendelsp
            // 
            this.tb_Volumen_Pendelsp.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tb_Volumen_Pendelsp.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tb_Volumen_Pendelsp.Location = new System.Drawing.Point(86, 29);
            this.tb_Volumen_Pendelsp.Margin = new System.Windows.Forms.Padding(5);
            this.tb_Volumen_Pendelsp.Name = "tb_Volumen_Pendelsp";
            this.tb_Volumen_Pendelsp.Size = new System.Drawing.Size(98, 25);
            this.tb_Volumen_Pendelsp.TabIndex = 40;
            // 
            // tb_Größe_Pendelsp
            // 
            this.tb_Größe_Pendelsp.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tb_Größe_Pendelsp.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tb_Größe_Pendelsp.Location = new System.Drawing.Point(295, 29);
            this.tb_Größe_Pendelsp.Margin = new System.Windows.Forms.Padding(5);
            this.tb_Größe_Pendelsp.Name = "tb_Größe_Pendelsp";
            this.tb_Größe_Pendelsp.Size = new System.Drawing.Size(63, 25);
            this.tb_Größe_Pendelsp.TabIndex = 41;
            // 
            // cb_Rendemix
            // 
            this.cb_Rendemix.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cb_Rendemix.Location = new System.Drawing.Point(23, 67);
            this.cb_Rendemix.Margin = new System.Windows.Forms.Padding(4);
            this.cb_Rendemix.Name = "cb_Rendemix";
            this.cb_Rendemix.Size = new System.Drawing.Size(357, 34);
            this.cb_Rendemix.TabIndex = 42;
            this.cb_Rendemix.Text = "Pufferspeicher mit optimiertem Ladesystem (rende MIX)";
            this.cb_Rendemix.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.textBox_Betriebsart);
            this.groupBox2.Controls.Add(this.Label12);
            this.groupBox2.Controls.Add(this.Label10);
            this.groupBox2.Controls.Add(this.textBox_WPName);
            this.groupBox2.Controls.Add(this.Label13);
            this.groupBox2.Controls.Add(this.textBox_Leistung_th);
            this.groupBox2.Controls.Add(this.textBox_Beschreibung);
            this.groupBox2.Controls.Add(this.textBox_Firma);
            this.groupBox2.Controls.Add(this.textBox_Leistung_el);
            this.groupBox2.Controls.Add(this.Label14);
            this.groupBox2.Controls.Add(this.Label15);
            this.groupBox2.Controls.Add(this.Label16);
            this.groupBox2.Controls.Add(this.Label11);
            this.groupBox2.Location = new System.Drawing.Point(21, 356);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(685, 230);
            this.groupBox2.TabIndex = 52;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Info markierte WP";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(5, 102);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(74, 17);
            this.label2.TabIndex = 60;
            this.label2.Text = "Betriebsart:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Betriebsart
            // 
            this.textBox_Betriebsart.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Betriebsart.Enabled = false;
            this.textBox_Betriebsart.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Betriebsart.Location = new System.Drawing.Point(137, 99);
            this.textBox_Betriebsart.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Betriebsart.Name = "textBox_Betriebsart";
            this.textBox_Betriebsart.Size = new System.Drawing.Size(182, 25);
            this.textBox_Betriebsart.TabIndex = 61;
            // 
            // Label12
            // 
            this.Label12.AutoSize = true;
            this.Label12.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label12.Location = new System.Drawing.Point(5, 66);
            this.Label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label12.Name = "Label12";
            this.Label12.Size = new System.Drawing.Size(126, 17);
            this.Label12.TabIndex = 48;
            this.Label12.Text = "thermische Leistung:";
            this.Label12.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label10
            // 
            this.Label10.AutoSize = true;
            this.Label10.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label10.Location = new System.Drawing.Point(42, 29);
            this.Label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label10.Name = "Label10";
            this.Label10.Size = new System.Drawing.Size(89, 17);
            this.Label10.TabIndex = 49;
            this.Label10.Text = "Modul-Name:";
            this.Label10.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_WPName
            // 
            this.textBox_WPName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_WPName.Enabled = false;
            this.textBox_WPName.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_WPName.Location = new System.Drawing.Point(137, 26);
            this.textBox_WPName.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_WPName.Name = "textBox_WPName";
            this.textBox_WPName.Size = new System.Drawing.Size(182, 25);
            this.textBox_WPName.TabIndex = 50;
            // 
            // Label13
            // 
            this.Label13.AutoSize = true;
            this.Label13.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label13.Location = new System.Drawing.Point(42, 165);
            this.Label13.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label13.Name = "Label13";
            this.Label13.Size = new System.Drawing.Size(89, 17);
            this.Label13.TabIndex = 51;
            this.Label13.Text = "Beschreibung:";
            this.Label13.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Leistung_th
            // 
            this.textBox_Leistung_th.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Leistung_th.Enabled = false;
            this.textBox_Leistung_th.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Leistung_th.Location = new System.Drawing.Point(137, 63);
            this.textBox_Leistung_th.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Leistung_th.Name = "textBox_Leistung_th";
            this.textBox_Leistung_th.Size = new System.Drawing.Size(63, 25);
            this.textBox_Leistung_th.TabIndex = 52;
            // 
            // textBox_Beschreibung
            // 
            this.textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Beschreibung.Enabled = false;
            this.textBox_Beschreibung.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Beschreibung.Location = new System.Drawing.Point(137, 163);
            this.textBox_Beschreibung.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Beschreibung.Multiline = true;
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            this.textBox_Beschreibung.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox_Beschreibung.Size = new System.Drawing.Size(527, 59);
            this.textBox_Beschreibung.TabIndex = 53;
            // 
            // textBox_Firma
            // 
            this.textBox_Firma.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Firma.Enabled = false;
            this.textBox_Firma.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Firma.Location = new System.Drawing.Point(450, 26);
            this.textBox_Firma.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Firma.Name = "textBox_Firma";
            this.textBox_Firma.Size = new System.Drawing.Size(133, 25);
            this.textBox_Firma.TabIndex = 54;
            // 
            // textBox_Leistung_el
            // 
            this.textBox_Leistung_el.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Leistung_el.Enabled = false;
            this.textBox_Leistung_el.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Leistung_el.Location = new System.Drawing.Point(450, 67);
            this.textBox_Leistung_el.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Leistung_el.Name = "textBox_Leistung_el";
            this.textBox_Leistung_el.Size = new System.Drawing.Size(63, 25);
            this.textBox_Leistung_el.TabIndex = 55;
            // 
            // Label14
            // 
            this.Label14.AutoSize = true;
            this.Label14.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label14.Location = new System.Drawing.Point(317, 67);
            this.Label14.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label14.Name = "Label14";
            this.Label14.Size = new System.Drawing.Size(124, 17);
            this.Label14.TabIndex = 56;
            this.Label14.Text = "elektrische Leistung:";
            this.Label14.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label15
            // 
            this.Label15.AutoSize = true;
            this.Label15.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label15.Location = new System.Drawing.Point(374, 26);
            this.Label15.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label15.Name = "Label15";
            this.Label15.Size = new System.Drawing.Size(67, 17);
            this.Label15.TabIndex = 57;
            this.Label15.Text = "Hersteller:";
            this.Label15.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label16
            // 
            this.Label16.AutoSize = true;
            this.Label16.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label16.Location = new System.Drawing.Point(518, 70);
            this.Label16.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label16.Name = "Label16";
            this.Label16.Size = new System.Drawing.Size(35, 17);
            this.Label16.TabIndex = 58;
            this.Label16.Text = "kWel";
            this.Label16.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label11
            // 
            this.Label11.AutoSize = true;
            this.Label11.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label11.Location = new System.Drawing.Point(203, 67);
            this.Label11.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label11.Name = "Label11";
            this.Label11.Size = new System.Drawing.Size(37, 17);
            this.Label11.TabIndex = 59;
            this.Label11.Text = "kWth";
            this.Label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btn_Abbrechen
            // 
            this.btn_Abbrechen.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Abbrechen.Location = new System.Drawing.Point(715, 681);
            this.btn_Abbrechen.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.Size = new System.Drawing.Size(91, 31);
            this.btn_Abbrechen.TabIndex = 53;
            this.btn_Abbrechen.Text = "Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // Form_WPEing
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(930, 721);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.listBox_WPEing);
            this.Controls.Add(this.listBox_WP_DB);
            this.Controls.Add(this.btn_Hinzu);
            this.Controls.Add(this.btn_Entfernen);
            this.Controls.Add(this.Label23);
            this.Controls.Add(this.comboBox_Leistungsfilter);
            this.Controls.Add(this.btn_DBWP_Edit);
            this.Controls.Add(this.btn_DBWP_Neu);
            this.Controls.Add(this.btn_DBWP_Löschen);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.Label7);
            this.Controls.Add(this.Label8);
            this.Controls.Add(this.textBox_Summe_Leistung);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Form_WPEing";
            this.Text = "Wärmepumpen Verwaltung";
            this.Load += new System.EventHandler(this.Form_WPEing_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

private System.Windows.Forms.Label Label1;
private System.Windows.Forms.ListBox listBox_WPEing;
private System.Windows.Forms.ListBox listBox_WP_DB;
private System.Windows.Forms.Button btn_Hinzu;
private System.Windows.Forms.Button btn_Entfernen;
private System.Windows.Forms.Label Label23;
private System.Windows.Forms.ComboBox comboBox_Leistungsfilter;
private System.Windows.Forms.Button btn_DBWP_Edit;
private System.Windows.Forms.Button btn_DBWP_Neu;
private System.Windows.Forms.Button btn_DBWP_Löschen;
private System.Windows.Forms.Button btn_OK;
private System.Windows.Forms.Label Label7;
private System.Windows.Forms.Label Label8;
private System.Windows.Forms.TextBox textBox_Summe_Leistung;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label Label19;
        private System.Windows.Forms.Label Label17;
        private System.Windows.Forms.Label Label18;
        private System.Windows.Forms.Label Label20;
        private System.Windows.Forms.TextBox tb_Volumen_Pendelsp;
        private System.Windows.Forms.TextBox tb_Größe_Pendelsp;
        private System.Windows.Forms.CheckBox cb_Rendemix;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label Label12;
        private System.Windows.Forms.Label Label10;
        private System.Windows.Forms.TextBox textBox_WPName;
        private System.Windows.Forms.Label Label13;
        private System.Windows.Forms.TextBox textBox_Leistung_th;
        private System.Windows.Forms.TextBox textBox_Beschreibung;
        private System.Windows.Forms.TextBox textBox_Firma;
        private System.Windows.Forms.TextBox textBox_Leistung_el;
        private System.Windows.Forms.Label Label14;
        private System.Windows.Forms.Label Label15;
        private System.Windows.Forms.Label Label16;
        private System.Windows.Forms.Label Label11;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox_Betriebsart;
        private System.Windows.Forms.Button btn_Abbrechen;
    }
}