namespace WindowsFormsApplication1
{
    partial class Form_SolarKollektoren
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
            this.btn_Kollektor_DB_Edit = new System.Windows.Forms.Button();
            this.btn_Kollektor_DB_neu = new System.Windows.Forms.Button();
            this.btn_Kollektor_DB_loeschen = new System.Windows.Forms.Button();
            this.Label10 = new System.Windows.Forms.Label();
            this.textBox_Anzahl = new System.Windows.Forms.TextBox();
            this.Label13 = new System.Windows.Forms.Label();
            this.textBox_Kollektorneigung = new System.Windows.Forms.TextBox();
            this.btn_OK = new System.Windows.Forms.Button();
            this.listBox_Auswahl = new System.Windows.Forms.ListBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.radioButton_Sued90 = new System.Windows.Forms.RadioButton();
            this.radioButton_SuedWest = new System.Windows.Forms.RadioButton();
            this.radioButton_flach = new System.Windows.Forms.RadioButton();
            this.radioButton_Sued = new System.Windows.Forms.RadioButton();
            this.radioButton_SuedOst = new System.Windows.Forms.RadioButton();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.btn_Hinzzu = new System.Windows.Forms.Button();
            this.btn_Entfernen = new System.Windows.Forms.Button();
            this.groupBox_Kollektor = new System.Windows.Forms.GroupBox();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.label_Type = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox_Name = new System.Windows.Forms.TextBox();
            this.Label11 = new System.Windows.Forms.Label();
            this.textBox_Kollektor_A = new System.Windows.Forms.TextBox();
            this.Label5 = new System.Windows.Forms.Label();
            this.Label6 = new System.Windows.Forms.Label();
            this.Label7 = new System.Windows.Forms.Label();
            this.Label8 = new System.Windows.Forms.Label();
            this.textBox_Kollektortype = new System.Windows.Forms.TextBox();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.textBox_Firma = new System.Windows.Forms.TextBox();
            this.textBox_Modul_A = new System.Windows.Forms.TextBox();
            this.Label9 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.btn_Speichern = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.groupBox_Kollektor.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // btn_Kollektor_DB_Edit
            // 
            this.btn_Kollektor_DB_Edit.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_Kollektor_DB_Edit.Location = new System.Drawing.Point(650, 404);
            this.btn_Kollektor_DB_Edit.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Kollektor_DB_Edit.Name = "btn_Kollektor_DB_Edit";
            this.btn_Kollektor_DB_Edit.Size = new System.Drawing.Size(162, 33);
            this.btn_Kollektor_DB_Edit.TabIndex = 10;
            this.btn_Kollektor_DB_Edit.Text = "Kollektor in DB ändern...";
            this.btn_Kollektor_DB_Edit.UseVisualStyleBackColor = true;
            this.btn_Kollektor_DB_Edit.Click += new System.EventHandler(this.btn_Kollektor_DB_Edit_Click);
            // 
            // btn_Kollektor_DB_neu
            // 
            this.btn_Kollektor_DB_neu.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_Kollektor_DB_neu.Location = new System.Drawing.Point(650, 445);
            this.btn_Kollektor_DB_neu.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Kollektor_DB_neu.Name = "btn_Kollektor_DB_neu";
            this.btn_Kollektor_DB_neu.Size = new System.Drawing.Size(162, 33);
            this.btn_Kollektor_DB_neu.TabIndex = 11;
            this.btn_Kollektor_DB_neu.Text = "Kollektor in DB neu...";
            this.btn_Kollektor_DB_neu.UseVisualStyleBackColor = true;
            this.btn_Kollektor_DB_neu.Click += new System.EventHandler(this.btn_Kollektor_DB_neu_Click);
            // 
            // btn_Kollektor_DB_loeschen
            // 
            this.btn_Kollektor_DB_loeschen.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_Kollektor_DB_loeschen.Location = new System.Drawing.Point(650, 486);
            this.btn_Kollektor_DB_loeschen.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Kollektor_DB_loeschen.Name = "btn_Kollektor_DB_loeschen";
            this.btn_Kollektor_DB_loeschen.Size = new System.Drawing.Size(162, 33);
            this.btn_Kollektor_DB_loeschen.TabIndex = 12;
            this.btn_Kollektor_DB_loeschen.Text = "Kollektor in DB löschen";
            this.btn_Kollektor_DB_loeschen.UseVisualStyleBackColor = true;
            this.btn_Kollektor_DB_loeschen.Click += new System.EventHandler(this.btn_Kollektor_DB_loeschen_Click);
            // 
            // Label10
            // 
            this.Label10.AutoSize = true;
            this.Label10.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label10.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.Label10.Location = new System.Drawing.Point(6, 71);
            this.Label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label10.Name = "Label10";
            this.Label10.Size = new System.Drawing.Size(92, 17);
            this.Label10.TabIndex = 13;
            this.Label10.Text = "Modulanzahl:";
            this.Label10.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBox_Anzahl
            // 
            this.textBox_Anzahl.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Anzahl.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Anzahl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.textBox_Anzahl.Location = new System.Drawing.Point(11, 90);
            this.textBox_Anzahl.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Anzahl.Name = "textBox_Anzahl";
            this.textBox_Anzahl.Size = new System.Drawing.Size(51, 25);
            this.textBox_Anzahl.TabIndex = 14;
            this.textBox_Anzahl.TextChanged += new System.EventHandler(this.textBox_Anzahl_TextChanged);
            // 
            // Label13
            // 
            this.Label13.AutoSize = true;
            this.Label13.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label13.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.Label13.Location = new System.Drawing.Point(117, 71);
            this.Label13.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label13.Name = "Label13";
            this.Label13.Size = new System.Drawing.Size(84, 17);
            this.Label13.TabIndex = 18;
            this.Label13.Text = "Neigung [°]:";
            this.Label13.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Kollektorneigung
            // 
            this.textBox_Kollektorneigung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Kollektorneigung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kollektorneigung.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.textBox_Kollektorneigung.Location = new System.Drawing.Point(120, 90);
            this.textBox_Kollektorneigung.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Kollektorneigung.Name = "textBox_Kollektorneigung";
            this.textBox_Kollektorneigung.Size = new System.Drawing.Size(57, 25);
            this.textBox_Kollektorneigung.TabIndex = 19;
            this.textBox_Kollektorneigung.TextChanged += new System.EventHandler(this.textBox_Kollektorneigung_TextChanged);
            // 
            // btn_OK
            // 
            this.btn_OK.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_OK.Location = new System.Drawing.Point(711, 576);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(4);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(105, 31);
            this.btn_OK.TabIndex = 22;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // listBox_Auswahl
            // 
            this.listBox_Auswahl.ItemHeight = 17;
            this.listBox_Auswahl.Location = new System.Drawing.Point(21, 54);
            this.listBox_Auswahl.Margin = new System.Windows.Forms.Padding(4);
            this.listBox_Auswahl.Name = "listBox_Auswahl";
            this.listBox_Auswahl.Size = new System.Drawing.Size(310, 157);
            this.listBox_Auswahl.TabIndex = 32;
            this.listBox_Auswahl.SelectedIndexChanged += new System.EventHandler(this.listBox_Auswahl_SelectedIndexChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.radioButton_Sued90);
            this.groupBox1.Controls.Add(this.radioButton_SuedWest);
            this.groupBox1.Controls.Add(this.radioButton_flach);
            this.groupBox1.Controls.Add(this.radioButton_Sued);
            this.groupBox1.Controls.Add(this.radioButton_SuedOst);
            this.groupBox1.Location = new System.Drawing.Point(9, 17);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(364, 48);
            this.groupBox1.TabIndex = 33;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Ausrichtung";
            // 
            // radioButton_Sued90
            // 
            this.radioButton_Sued90.AutoSize = true;
            this.radioButton_Sued90.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButton_Sued90.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.radioButton_Sued90.Location = new System.Drawing.Point(287, 24);
            this.radioButton_Sued90.Name = "radioButton_Sued90";
            this.radioButton_Sued90.Size = new System.Drawing.Size(72, 21);
            this.radioButton_Sued90.TabIndex = 4;
            this.radioButton_Sued90.TabStop = true;
            this.radioButton_Sued90.Text = "Süd 90°";
            this.radioButton_Sued90.UseVisualStyleBackColor = true;
            // 
            // radioButton_SuedWest
            // 
            this.radioButton_SuedWest.AutoSize = true;
            this.radioButton_SuedWest.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButton_SuedWest.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.radioButton_SuedWest.Location = new System.Drawing.Point(144, 24);
            this.radioButton_SuedWest.Name = "radioButton_SuedWest";
            this.radioButton_SuedWest.Size = new System.Drawing.Size(85, 21);
            this.radioButton_SuedWest.TabIndex = 3;
            this.radioButton_SuedWest.TabStop = true;
            this.radioButton_SuedWest.Text = "Süd-West";
            this.radioButton_SuedWest.UseVisualStyleBackColor = true;
            // 
            // radioButton_flach
            // 
            this.radioButton_flach.AutoSize = true;
            this.radioButton_flach.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButton_flach.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.radioButton_flach.Location = new System.Drawing.Point(229, 24);
            this.radioButton_flach.Name = "radioButton_flach";
            this.radioButton_flach.Size = new System.Drawing.Size(56, 21);
            this.radioButton_flach.TabIndex = 2;
            this.radioButton_flach.TabStop = true;
            this.radioButton_flach.Text = "flach";
            this.radioButton_flach.UseVisualStyleBackColor = true;
            // 
            // radioButton_Sued
            // 
            this.radioButton_Sued.AutoSize = true;
            this.radioButton_Sued.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButton_Sued.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.radioButton_Sued.Location = new System.Drawing.Point(81, 24);
            this.radioButton_Sued.Name = "radioButton_Sued";
            this.radioButton_Sued.Size = new System.Drawing.Size(49, 21);
            this.radioButton_Sued.TabIndex = 1;
            this.radioButton_Sued.TabStop = true;
            this.radioButton_Sued.Text = "Süd";
            this.radioButton_Sued.UseVisualStyleBackColor = true;
            // 
            // radioButton_SuedOst
            // 
            this.radioButton_SuedOst.AutoSize = true;
            this.radioButton_SuedOst.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButton_SuedOst.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.radioButton_SuedOst.Location = new System.Drawing.Point(8, 24);
            this.radioButton_SuedOst.Name = "radioButton_SuedOst";
            this.radioButton_SuedOst.Size = new System.Drawing.Size(75, 21);
            this.radioButton_SuedOst.TabIndex = 0;
            this.radioButton_SuedOst.TabStop = true;
            this.radioButton_SuedOst.Text = "Süd-Ost";
            this.radioButton_SuedOst.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(23, 33);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(116, 17);
            this.label2.TabIndex = 36;
            this.label2.Text = "Auswahl in Projekt:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(456, 42);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(92, 17);
            this.label3.TabIndex = 37;
            this.label3.Text = "Auswahl in DB:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.BackgroundColor = System.Drawing.Color.Silver;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(457, 63);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(359, 334);
            this.dataGridView1.TabIndex = 76;
            this.dataGridView1.Click += new System.EventHandler(this.dataGridView1_Click);
            this.dataGridView1.Leave += new System.EventHandler(this.dataGridView1_Leave);
            // 
            // btn_Hinzzu
            // 
            this.btn_Hinzzu.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_Hinzzu.Location = new System.Drawing.Point(343, 93);
            this.btn_Hinzzu.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Hinzzu.Name = "btn_Hinzzu";
            this.btn_Hinzzu.Size = new System.Drawing.Size(98, 31);
            this.btn_Hinzzu.TabIndex = 77;
            this.btn_Hinzzu.Text = "<-- Hinzufügen";
            this.btn_Hinzzu.UseVisualStyleBackColor = true;
            this.btn_Hinzzu.Click += new System.EventHandler(this.btn_Hinzzu_Click);
            // 
            // btn_Entfernen
            // 
            this.btn_Entfernen.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_Entfernen.Location = new System.Drawing.Point(343, 132);
            this.btn_Entfernen.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Entfernen.Name = "btn_Entfernen";
            this.btn_Entfernen.Size = new System.Drawing.Size(98, 31);
            this.btn_Entfernen.TabIndex = 78;
            this.btn_Entfernen.Text = "Entfernen -->";
            this.btn_Entfernen.UseVisualStyleBackColor = true;
            this.btn_Entfernen.Click += new System.EventHandler(this.btn_Entfernen_Click);
            // 
            // groupBox_Kollektor
            // 
            this.groupBox_Kollektor.BackColor = System.Drawing.Color.Khaki;
            this.groupBox_Kollektor.Controls.Add(this.pictureBox1);
            this.groupBox_Kollektor.Controls.Add(this.btn_Speichern);
            this.groupBox_Kollektor.Controls.Add(this.textBox_Anzahl);
            this.groupBox_Kollektor.Controls.Add(this.Label10);
            this.groupBox_Kollektor.Controls.Add(this.Label11);
            this.groupBox_Kollektor.Controls.Add(this.textBox_Kollektorneigung);
            this.groupBox_Kollektor.Controls.Add(this.textBox_Kollektor_A);
            this.groupBox_Kollektor.Controls.Add(this.Label13);
            this.groupBox_Kollektor.Controls.Add(this.groupBox1);
            this.groupBox_Kollektor.Location = new System.Drawing.Point(21, 218);
            this.groupBox_Kollektor.Name = "groupBox_Kollektor";
            this.groupBox_Kollektor.Size = new System.Drawing.Size(400, 154);
            this.groupBox_Kollektor.TabIndex = 79;
            this.groupBox_Kollektor.TabStop = false;
            this.groupBox_Kollektor.Text = "Kollektor";
            // 
            // btn_Abbrechen
            // 
            this.btn_Abbrechen.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Abbrechen.Location = new System.Drawing.Point(612, 576);
            this.btn_Abbrechen.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.Size = new System.Drawing.Size(91, 31);
            this.btn_Abbrechen.TabIndex = 80;
            this.btn_Abbrechen.Text = "Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click_1);
            // 
            // label_Type
            // 
            this.label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.label_Type.Dock = System.Windows.Forms.DockStyle.Top;
            this.label_Type.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Type.Location = new System.Drawing.Point(0, 0);
            this.label_Type.Name = "label_Type";
            this.label_Type.Padding = new System.Windows.Forms.Padding(0, 5, 0, 5);
            this.label_Type.Size = new System.Drawing.Size(825, 31);
            this.label_Type.TabIndex = 81;
            this.label_Type.Text = "Eingabe der Solarkollektoren";
            this.label_Type.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.textBox_Name);
            this.groupBox2.Controls.Add(this.Label5);
            this.groupBox2.Controls.Add(this.Label6);
            this.groupBox2.Controls.Add(this.Label7);
            this.groupBox2.Controls.Add(this.Label8);
            this.groupBox2.Controls.Add(this.textBox_Kollektortype);
            this.groupBox2.Controls.Add(this.textBox_Beschreibung);
            this.groupBox2.Controls.Add(this.textBox_Firma);
            this.groupBox2.Controls.Add(this.textBox_Modul_A);
            this.groupBox2.Controls.Add(this.Label9);
            this.groupBox2.Location = new System.Drawing.Point(21, 378);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(400, 229);
            this.groupBox2.TabIndex = 82;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Modul";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(16, 38);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(46, 17);
            this.label1.TabIndex = 48;
            this.label1.Text = "Name:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Name
            // 
            this.textBox_Name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Name.Enabled = false;
            this.textBox_Name.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Name.Location = new System.Drawing.Point(106, 35);
            this.textBox_Name.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Name.Name = "textBox_Name";
            this.textBox_Name.Size = new System.Drawing.Size(280, 25);
            this.textBox_Name.TabIndex = 49;
            // 
            // Label11
            // 
            this.Label11.AutoSize = true;
            this.Label11.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label11.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.Label11.Location = new System.Drawing.Point(222, 71);
            this.Label11.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label11.Name = "Label11";
            this.Label11.Size = new System.Drawing.Size(136, 17);
            this.Label11.TabIndex = 36;
            this.Label11.Text = "Kollektorfläche [m²]:";
            this.Label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBox_Kollektor_A
            // 
            this.textBox_Kollektor_A.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Kollektor_A.Enabled = false;
            this.textBox_Kollektor_A.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kollektor_A.Location = new System.Drawing.Point(225, 90);
            this.textBox_Kollektor_A.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Kollektor_A.Name = "textBox_Kollektor_A";
            this.textBox_Kollektor_A.Size = new System.Drawing.Size(85, 25);
            this.textBox_Kollektor_A.TabIndex = 37;
            // 
            // Label5
            // 
            this.Label5.AutoSize = true;
            this.Label5.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label5.Location = new System.Drawing.Point(16, 71);
            this.Label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label5.Name = "Label5";
            this.Label5.Size = new System.Drawing.Size(63, 17);
            this.Label5.TabIndex = 39;
            this.Label5.Text = "Kollektor:";
            this.Label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label6
            // 
            this.Label6.AutoSize = true;
            this.Label6.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label6.Location = new System.Drawing.Point(16, 102);
            this.Label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label6.Name = "Label6";
            this.Label6.Size = new System.Drawing.Size(89, 17);
            this.Label6.TabIndex = 40;
            this.Label6.Text = "Beschreibung:";
            this.Label6.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label7
            // 
            this.Label7.AutoSize = true;
            this.Label7.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label7.Location = new System.Drawing.Point(16, 166);
            this.Label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label7.Name = "Label7";
            this.Label7.Size = new System.Drawing.Size(67, 17);
            this.Label7.TabIndex = 41;
            this.Label7.Text = "Hersteller:";
            this.Label7.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label8
            // 
            this.Label8.AutoSize = true;
            this.Label8.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label8.Location = new System.Drawing.Point(16, 196);
            this.Label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label8.Name = "Label8";
            this.Label8.Size = new System.Drawing.Size(83, 17);
            this.Label8.TabIndex = 42;
            this.Label8.Text = "Modulfläche:";
            this.Label8.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Kollektortype
            // 
            this.textBox_Kollektortype.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Kollektortype.Enabled = false;
            this.textBox_Kollektortype.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kollektortype.Location = new System.Drawing.Point(106, 68);
            this.textBox_Kollektortype.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Kollektortype.Name = "textBox_Kollektortype";
            this.textBox_Kollektortype.Size = new System.Drawing.Size(280, 25);
            this.textBox_Kollektortype.TabIndex = 43;
            // 
            // textBox_Beschreibung
            // 
            this.textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Beschreibung.Enabled = false;
            this.textBox_Beschreibung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Beschreibung.Location = new System.Drawing.Point(106, 100);
            this.textBox_Beschreibung.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Beschreibung.Multiline = true;
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            this.textBox_Beschreibung.Size = new System.Drawing.Size(280, 56);
            this.textBox_Beschreibung.TabIndex = 44;
            // 
            // textBox_Firma
            // 
            this.textBox_Firma.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Firma.Enabled = false;
            this.textBox_Firma.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Firma.Location = new System.Drawing.Point(106, 164);
            this.textBox_Firma.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Firma.Name = "textBox_Firma";
            this.textBox_Firma.Size = new System.Drawing.Size(280, 25);
            this.textBox_Firma.TabIndex = 45;
            // 
            // textBox_Modul_A
            // 
            this.textBox_Modul_A.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Modul_A.Enabled = false;
            this.textBox_Modul_A.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Modul_A.Location = new System.Drawing.Point(106, 196);
            this.textBox_Modul_A.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Modul_A.Name = "textBox_Modul_A";
            this.textBox_Modul_A.Size = new System.Drawing.Size(113, 25);
            this.textBox_Modul_A.TabIndex = 46;
            // 
            // Label9
            // 
            this.Label9.AutoSize = true;
            this.Label9.BackColor = System.Drawing.Color.Black;
            this.Label9.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label9.ForeColor = System.Drawing.Color.White;
            this.Label9.Location = new System.Drawing.Point(224, 200);
            this.Label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label9.Name = "Label9";
            this.Label9.Size = new System.Drawing.Size(24, 17);
            this.Label9.TabIndex = 47;
            this.Label9.Text = "m²";
            this.Label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImage = global::WindowsFormsApplication1.Properties.Resources.setup_trans;
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pictureBox1.Location = new System.Drawing.Point(169, 120);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(28, 27);
            this.pictureBox1.TabIndex = 117;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Visible = false;
            // 
            // btn_Speichern
            // 
            this.btn_Speichern.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Speichern.Image = global::WindowsFormsApplication1.Properties.Resources.speichern;
            this.btn_Speichern.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btn_Speichern.Location = new System.Drawing.Point(10, 120);
            this.btn_Speichern.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Speichern.Name = "btn_Speichern";
            this.btn_Speichern.Size = new System.Drawing.Size(155, 27);
            this.btn_Speichern.TabIndex = 81;
            this.btn_Speichern.Text = "Übernehmen";
            this.btn_Speichern.UseVisualStyleBackColor = true;
            this.btn_Speichern.Click += new System.EventHandler(this.btn_Speichern_Click);
            // 
            // Form_SolarKollektoren
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(825, 616);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.label_Type);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.groupBox_Kollektor);
            this.Controls.Add(this.btn_Hinzzu);
            this.Controls.Add(this.btn_Entfernen);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.listBox_Auswahl);
            this.Controls.Add(this.btn_Kollektor_DB_Edit);
            this.Controls.Add(this.btn_Kollektor_DB_neu);
            this.Controls.Add(this.btn_Kollektor_DB_loeschen);
            this.Controls.Add(this.btn_OK);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Form_SolarKollektoren";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Eingabe der Solarkollektoren";
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.Form_SolarKollektoren_Paint);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.groupBox_Kollektor.ResumeLayout(false);
            this.groupBox_Kollektor.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
private System.Windows.Forms.Button btn_Kollektor_DB_Edit;
private System.Windows.Forms.Button btn_Kollektor_DB_neu;
private System.Windows.Forms.Button btn_Kollektor_DB_loeschen;
private System.Windows.Forms.Label Label10;
private System.Windows.Forms.TextBox textBox_Anzahl;
private System.Windows.Forms.Label Label13;
private System.Windows.Forms.TextBox textBox_Kollektorneigung;
private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.ListBox listBox_Auswahl;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton radioButton_Sued90;
        private System.Windows.Forms.RadioButton radioButton_SuedWest;
        private System.Windows.Forms.RadioButton radioButton_flach;
        private System.Windows.Forms.RadioButton radioButton_Sued;
        private System.Windows.Forms.RadioButton radioButton_SuedOst;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button btn_Hinzzu;
        private System.Windows.Forms.Button btn_Entfernen;
        private System.Windows.Forms.GroupBox groupBox_Kollektor;
        private System.Windows.Forms.Button btn_Abbrechen;
        private System.Windows.Forms.Button btn_Speichern;
        private System.Windows.Forms.Label label_Type;
        private System.Windows.Forms.Label Label11;
        private System.Windows.Forms.TextBox textBox_Kollektor_A;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox_Name;
        private System.Windows.Forms.Label Label5;
        private System.Windows.Forms.Label Label6;
        private System.Windows.Forms.Label Label7;
        private System.Windows.Forms.Label Label8;
        private System.Windows.Forms.TextBox textBox_Kollektortype;
        private System.Windows.Forms.TextBox textBox_Beschreibung;
        private System.Windows.Forms.TextBox textBox_Firma;
        private System.Windows.Forms.TextBox textBox_Modul_A;
        private System.Windows.Forms.Label Label9;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}