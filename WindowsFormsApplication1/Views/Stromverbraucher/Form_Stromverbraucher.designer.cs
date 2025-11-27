namespace WindowsFormsApplication1
{
    partial class Form_Stromverbraucher
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
            this.btn_Hinzu = new System.Windows.Forms.Button();
            this.btn_Entfernen = new System.Windows.Forms.Button();
            this.btn_Strom_DBneu = new System.Windows.Forms.Button();
            this.btn_Strom_loeschen = new System.Windows.Forms.Button();
            this.btn_ErgebnisseVerbrauch = new System.Windows.Forms.Button();
            this.btn_Simulation = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.Label24 = new System.Windows.Forms.Label();
            this.btn_StromtypDBedit = new System.Windows.Forms.Button();
            this.btn_Strom_DBedit = new System.Windows.Forms.Button();
            this.Label12 = new System.Windows.Forms.Label();
            this.Label10 = new System.Windows.Forms.Label();
            this.textBox_Stromname = new System.Windows.Forms.TextBox();
            this.Label13 = new System.Windows.Forms.Label();
            this.textBox_Jahres_Verbrauch = new System.Windows.Forms.TextBox();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.textBox_Stromtyp = new System.Windows.Forms.TextBox();
            this.Label15 = new System.Windows.Forms.Label();
            this.Label11 = new System.Windows.Forms.Label();
            this.Label19 = new System.Windows.Forms.Label();
            this.Label18 = new System.Windows.Forms.Label();
            this.textBox_StromSumme = new System.Windows.Forms.TextBox();
            this.listBox_Strom_DB = new System.Windows.Forms.ListBox();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.listView_Strom_Auswahl = new System.Windows.Forms.ListView();
            this.Label1 = new System.Windows.Forms.Label();
            this.label_Type = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.textBox_Verbrauch = new System.Windows.Forms.TextBox();
            this.Label8 = new System.Windows.Forms.Label();
            this.btn_neuerWert = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btn_Hinzu
            // 
            this.btn_Hinzu.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Hinzu.Location = new System.Drawing.Point(330, 127);
            this.btn_Hinzu.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Hinzu.Name = "btn_Hinzu";
            this.btn_Hinzu.Size = new System.Drawing.Size(82, 33);
            this.btn_Hinzu.TabIndex = 1;
            this.btn_Hinzu.Text = "<---";
            this.btn_Hinzu.UseVisualStyleBackColor = true;
            this.btn_Hinzu.Click += new System.EventHandler(this.btn__Hinzu_Click);
            // 
            // btn_Entfernen
            // 
            this.btn_Entfernen.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Entfernen.Location = new System.Drawing.Point(330, 168);
            this.btn_Entfernen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Entfernen.Name = "btn_Entfernen";
            this.btn_Entfernen.Size = new System.Drawing.Size(82, 31);
            this.btn_Entfernen.TabIndex = 2;
            this.btn_Entfernen.Text = "--->";
            this.btn_Entfernen.UseVisualStyleBackColor = true;
            this.btn_Entfernen.Click += new System.EventHandler(this.btn_Entfernen_Click);
            // 
            // btn_Strom_DBneu
            // 
            this.btn_Strom_DBneu.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Strom_DBneu.Location = new System.Drawing.Point(543, 310);
            this.btn_Strom_DBneu.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Strom_DBneu.Name = "btn_Strom_DBneu";
            this.btn_Strom_DBneu.Size = new System.Drawing.Size(184, 31);
            this.btn_Strom_DBneu.TabIndex = 3;
            this.btn_Strom_DBneu.Text = "Stromverbraucher neu...";
            this.btn_Strom_DBneu.UseVisualStyleBackColor = true;
            this.btn_Strom_DBneu.Click += new System.EventHandler(this.btn_Strom_DBneu_Click);
            // 
            // btn_Strom_loeschen
            // 
            this.btn_Strom_loeschen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Strom_loeschen.Location = new System.Drawing.Point(543, 349);
            this.btn_Strom_loeschen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Strom_loeschen.Name = "btn_Strom_loeschen";
            this.btn_Strom_loeschen.Size = new System.Drawing.Size(184, 34);
            this.btn_Strom_loeschen.TabIndex = 4;
            this.btn_Strom_loeschen.Text = "Stromverbraucher löschen";
            this.btn_Strom_loeschen.UseVisualStyleBackColor = true;
            this.btn_Strom_loeschen.Click += new System.EventHandler(this.btn_Strom_loeschen_Click);
            // 
            // btn_ErgebnisseVerbrauch
            // 
            this.btn_ErgebnisseVerbrauch.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_ErgebnisseVerbrauch.Location = new System.Drawing.Point(184, 530);
            this.btn_ErgebnisseVerbrauch.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_ErgebnisseVerbrauch.Name = "btn_ErgebnisseVerbrauch";
            this.btn_ErgebnisseVerbrauch.Size = new System.Drawing.Size(144, 31);
            this.btn_ErgebnisseVerbrauch.TabIndex = 6;
            this.btn_ErgebnisseVerbrauch.Text = "monatlicher Verlauf...";
            this.btn_ErgebnisseVerbrauch.UseVisualStyleBackColor = true;
            this.btn_ErgebnisseVerbrauch.Click += new System.EventHandler(this.btn_ErgebnisseVerbrauch_Click);
            // 
            // btn_Simulation
            // 
            this.btn_Simulation.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Simulation.Location = new System.Drawing.Point(334, 530);
            this.btn_Simulation.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Simulation.Name = "btn_Simulation";
            this.btn_Simulation.Size = new System.Drawing.Size(119, 31);
            this.btn_Simulation.TabIndex = 9;
            this.btn_Simulation.Text = "Simulation...";
            this.btn_Simulation.UseVisualStyleBackColor = true;
            this.btn_Simulation.Click += new System.EventHandler(this.btn_Simulation_Click);
            // 
            // btn_OK
            // 
            this.btn_OK.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_OK.Location = new System.Drawing.Point(622, 530);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(105, 31);
            this.btn_OK.TabIndex = 10;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // Label24
            // 
            this.Label24.AutoSize = true;
            this.Label24.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label24.Location = new System.Drawing.Point(435, 48);
            this.Label24.Name = "Label24";
            this.Label24.Size = new System.Drawing.Size(156, 19);
            this.Label24.TabIndex = 12;
            this.Label24.Text = "Datenbank Strombedarf";
            this.Label24.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btn_StromtypDBedit
            // 
            this.btn_StromtypDBedit.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_StromtypDBedit.Location = new System.Drawing.Point(543, 391);
            this.btn_StromtypDBedit.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_StromtypDBedit.Name = "btn_StromtypDBedit";
            this.btn_StromtypDBedit.Size = new System.Drawing.Size(184, 31);
            this.btn_StromtypDBedit.TabIndex = 13;
            this.btn_StromtypDBedit.Text = "Typ in DB ändern...";
            this.btn_StromtypDBedit.UseVisualStyleBackColor = true;
            this.btn_StromtypDBedit.Click += new System.EventHandler(this.btn_StromtypDBedit_Click);
            // 
            // btn_Strom_DBedit
            // 
            this.btn_Strom_DBedit.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Strom_DBedit.Location = new System.Drawing.Point(543, 268);
            this.btn_Strom_DBedit.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Strom_DBedit.Name = "btn_Strom_DBedit";
            this.btn_Strom_DBedit.Size = new System.Drawing.Size(184, 34);
            this.btn_Strom_DBedit.TabIndex = 15;
            this.btn_Strom_DBedit.Text = "Stromverbraucher ändern...";
            this.btn_Strom_DBedit.UseVisualStyleBackColor = true;
            this.btn_Strom_DBedit.Click += new System.EventHandler(this.btn_Strom_DBedit_Click);
            // 
            // Label12
            // 
            this.Label12.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label12.Location = new System.Drawing.Point(136, 488);
            this.Label12.Name = "Label12";
            this.Label12.Size = new System.Drawing.Size(153, 24);
            this.Label12.TabIndex = 19;
            this.Label12.Text = "jährlicher Strombedarf:";
            this.Label12.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label10
            // 
            this.Label10.AutoSize = true;
            this.Label10.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label10.Location = new System.Drawing.Point(66, 330);
            this.Label10.Name = "Label10";
            this.Label10.Size = new System.Drawing.Size(48, 19);
            this.Label10.TabIndex = 20;
            this.Label10.Text = "Name:";
            this.Label10.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Stromname
            // 
            this.textBox_Stromname.BackColor = System.Drawing.Color.White;
            this.textBox_Stromname.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Stromname.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.textBox_Stromname.Location = new System.Drawing.Point(122, 332);
            this.textBox_Stromname.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Stromname.Name = "textBox_Stromname";
            this.textBox_Stromname.ReadOnly = true;
            this.textBox_Stromname.Size = new System.Drawing.Size(331, 22);
            this.textBox_Stromname.TabIndex = 21;
            // 
            // Label13
            // 
            this.Label13.AutoSize = true;
            this.Label13.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label13.Location = new System.Drawing.Point(20, 394);
            this.Label13.Name = "Label13";
            this.Label13.Size = new System.Drawing.Size(94, 19);
            this.Label13.TabIndex = 22;
            this.Label13.Text = "Beschreibung:";
            this.Label13.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Jahres_Verbrauch
            // 
            this.textBox_Jahres_Verbrauch.BackColor = System.Drawing.Color.White;
            this.textBox_Jahres_Verbrauch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Jahres_Verbrauch.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.textBox_Jahres_Verbrauch.Location = new System.Drawing.Point(298, 491);
            this.textBox_Jahres_Verbrauch.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Jahres_Verbrauch.Name = "textBox_Jahres_Verbrauch";
            this.textBox_Jahres_Verbrauch.ReadOnly = true;
            this.textBox_Jahres_Verbrauch.Size = new System.Drawing.Size(63, 22);
            this.textBox_Jahres_Verbrauch.TabIndex = 23;
            // 
            // textBox_Beschreibung
            // 
            this.textBox_Beschreibung.BackColor = System.Drawing.Color.White;
            this.textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Beschreibung.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.textBox_Beschreibung.Location = new System.Drawing.Point(122, 391);
            this.textBox_Beschreibung.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Beschreibung.Multiline = true;
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            this.textBox_Beschreibung.ReadOnly = true;
            this.textBox_Beschreibung.Size = new System.Drawing.Size(331, 57);
            this.textBox_Beschreibung.TabIndex = 24;
            // 
            // textBox_Stromtyp
            // 
            this.textBox_Stromtyp.BackColor = System.Drawing.Color.White;
            this.textBox_Stromtyp.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Stromtyp.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.textBox_Stromtyp.Location = new System.Drawing.Point(122, 361);
            this.textBox_Stromtyp.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Stromtyp.Name = "textBox_Stromtyp";
            this.textBox_Stromtyp.ReadOnly = true;
            this.textBox_Stromtyp.Size = new System.Drawing.Size(156, 22);
            this.textBox_Stromtyp.TabIndex = 25;
            // 
            // Label15
            // 
            this.Label15.AutoSize = true;
            this.Label15.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label15.Location = new System.Drawing.Point(80, 359);
            this.Label15.Name = "Label15";
            this.Label15.Size = new System.Drawing.Size(33, 19);
            this.Label15.TabIndex = 26;
            this.Label15.Text = "Typ:";
            this.Label15.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label11
            // 
            this.Label11.AutoSize = true;
            this.Label11.BackColor = System.Drawing.Color.Black;
            this.Label11.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label11.ForeColor = System.Drawing.Color.White;
            this.Label11.Location = new System.Drawing.Point(370, 493);
            this.Label11.Name = "Label11";
            this.Label11.Size = new System.Drawing.Size(48, 19);
            this.Label11.TabIndex = 27;
            this.Label11.Text = "MWth";
            this.Label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label19
            // 
            this.Label19.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label19.Location = new System.Drawing.Point(26, 453);
            this.Label19.Name = "Label19";
            this.Label19.Size = new System.Drawing.Size(268, 36);
            this.Label19.TabIndex = 28;
            this.Label19.Text = "Summe aller ausgewählten Strombedarfe:";
            this.Label19.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label18
            // 
            this.Label18.AutoSize = true;
            this.Label18.BackColor = System.Drawing.Color.Black;
            this.Label18.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label18.ForeColor = System.Drawing.Color.White;
            this.Label18.Location = new System.Drawing.Point(369, 464);
            this.Label18.Name = "Label18";
            this.Label18.Size = new System.Drawing.Size(48, 19);
            this.Label18.TabIndex = 29;
            this.Label18.Text = "MW/h";
            this.Label18.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBox_StromSumme
            // 
            this.textBox_StromSumme.BackColor = System.Drawing.Color.White;
            this.textBox_StromSumme.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_StromSumme.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.textBox_StromSumme.Location = new System.Drawing.Point(298, 463);
            this.textBox_StromSumme.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_StromSumme.Name = "textBox_StromSumme";
            this.textBox_StromSumme.ReadOnly = true;
            this.textBox_StromSumme.Size = new System.Drawing.Size(63, 22);
            this.textBox_StromSumme.TabIndex = 30;
            // 
            // listBox_Strom_DB
            // 
            this.listBox_Strom_DB.FormattingEnabled = true;
            this.listBox_Strom_DB.ItemHeight = 17;
            this.listBox_Strom_DB.Location = new System.Drawing.Point(439, 70);
            this.listBox_Strom_DB.Name = "listBox_Strom_DB";
            this.listBox_Strom_DB.Size = new System.Drawing.Size(288, 174);
            this.listBox_Strom_DB.TabIndex = 32;
            this.listBox_Strom_DB.SelectedIndexChanged += new System.EventHandler(this.listBox_Prozess_DB_SelectedIndexChanged);
            // 
            // btn_Abbrechen
            // 
            this.btn_Abbrechen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Abbrechen.Location = new System.Drawing.Point(511, 530);
            this.btn_Abbrechen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.Size = new System.Drawing.Size(105, 31);
            this.btn_Abbrechen.TabIndex = 33;
            this.btn_Abbrechen.Text = "Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // listView_Strom_Auswahl
            // 
            this.listView_Strom_Auswahl.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.listView_Strom_Auswahl.HideSelection = false;
            this.listView_Strom_Auswahl.Location = new System.Drawing.Point(16, 70);
            this.listView_Strom_Auswahl.Name = "listView_Strom_Auswahl";
            this.listView_Strom_Auswahl.Size = new System.Drawing.Size(292, 178);
            this.listView_Strom_Auswahl.TabIndex = 41;
            this.listView_Strom_Auswahl.UseCompatibleStateImageBehavior = false;
            this.listView_Strom_Auswahl.SelectedIndexChanged += new System.EventHandler(this.listView_Prozess_Auswahl_SelectedIndexChanged);
            // 
            // Label1
            // 
            this.Label1.AutoSize = true;
            this.Label1.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label1.Location = new System.Drawing.Point(12, 48);
            this.Label1.Name = "Label1";
            this.Label1.Size = new System.Drawing.Size(240, 19);
            this.Label1.TabIndex = 37;
            this.Label1.Text = "Ausgewählte Strombedarfe im Projekt";
            this.Label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label_Type
            // 
            this.label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.label_Type.Dock = System.Windows.Forms.DockStyle.Top;
            this.label_Type.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Type.Location = new System.Drawing.Point(0, 0);
            this.label_Type.Name = "label_Type";
            this.label_Type.Padding = new System.Windows.Forms.Padding(0, 5, 0, 5);
            this.label_Type.Size = new System.Drawing.Size(742, 37);
            this.label_Type.TabIndex = 42;
            this.label_Type.Text = "Standard Stromprofil";
            this.label_Type.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.textBox_Verbrauch);
            this.groupBox1.Controls.Add(this.Label8);
            this.groupBox1.Controls.Add(this.btn_neuerWert);
            this.groupBox1.Location = new System.Drawing.Point(18, 256);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(264, 65);
            this.groupBox1.TabIndex = 43;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Ändern des Jahresverbrauchs";
            // 
            // textBox_Verbrauch
            // 
            this.textBox_Verbrauch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Verbrauch.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.textBox_Verbrauch.Location = new System.Drawing.Point(8, 27);
            this.textBox_Verbrauch.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Verbrauch.Name = "textBox_Verbrauch";
            this.textBox_Verbrauch.Size = new System.Drawing.Size(76, 22);
            this.textBox_Verbrauch.TabIndex = 41;
            this.textBox_Verbrauch.TextChanged += new System.EventHandler(this.textBox_Verbrauch_TextChanged);
            // 
            // Label8
            // 
            this.Label8.AutoSize = true;
            this.Label8.BackColor = System.Drawing.Color.Black;
            this.Label8.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label8.ForeColor = System.Drawing.Color.White;
            this.Label8.Location = new System.Drawing.Point(93, 29);
            this.Label8.Name = "Label8";
            this.Label8.Size = new System.Drawing.Size(43, 19);
            this.Label8.TabIndex = 42;
            this.Label8.Text = "MWh";
            this.Label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btn_neuerWert
            // 
            this.btn_neuerWert.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_neuerWert.Location = new System.Drawing.Point(142, 25);
            this.btn_neuerWert.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_neuerWert.Name = "btn_neuerWert";
            this.btn_neuerWert.Size = new System.Drawing.Size(115, 30);
            this.btn_neuerWert.TabIndex = 43;
            this.btn_neuerWert.Text = "Übernehmen";
            this.btn_neuerWert.UseVisualStyleBackColor = true;
            this.btn_neuerWert.Click += new System.EventHandler(this.btn_neuerWert_Click);
            // 
            // Form_Stromverbraucher
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(742, 571);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label_Type);
            this.Controls.Add(this.listView_Strom_Auswahl);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.listBox_Strom_DB);
            this.Controls.Add(this.btn_Hinzu);
            this.Controls.Add(this.btn_Entfernen);
            this.Controls.Add(this.btn_Strom_DBneu);
            this.Controls.Add(this.btn_Strom_loeschen);
            this.Controls.Add(this.btn_ErgebnisseVerbrauch);
            this.Controls.Add(this.btn_Simulation);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.Label24);
            this.Controls.Add(this.btn_StromtypDBedit);
            this.Controls.Add(this.btn_Strom_DBedit);
            this.Controls.Add(this.Label12);
            this.Controls.Add(this.Label10);
            this.Controls.Add(this.textBox_Stromname);
            this.Controls.Add(this.Label13);
            this.Controls.Add(this.textBox_Jahres_Verbrauch);
            this.Controls.Add(this.textBox_Beschreibung);
            this.Controls.Add(this.textBox_Stromtyp);
            this.Controls.Add(this.Label15);
            this.Controls.Add(this.Label11);
            this.Controls.Add(this.Label19);
            this.Controls.Add(this.Label18);
            this.Controls.Add(this.textBox_StromSumme);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Form_Stromverbraucher";
            this.Text = "Strombedarf Verwaltung";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_Hinzu;
private System.Windows.Forms.Button btn_Entfernen;
private System.Windows.Forms.Button btn_Strom_DBneu;
private System.Windows.Forms.Button btn_Strom_loeschen;
private System.Windows.Forms.Button btn_ErgebnisseVerbrauch;
private System.Windows.Forms.Button btn_Simulation;
private System.Windows.Forms.Button btn_OK;
private System.Windows.Forms.Label Label24;
private System.Windows.Forms.Button btn_StromtypDBedit;
private System.Windows.Forms.Button btn_Strom_DBedit;
private System.Windows.Forms.Label Label12;
private System.Windows.Forms.Label Label10;
private System.Windows.Forms.TextBox textBox_Stromname;
private System.Windows.Forms.Label Label13;
private System.Windows.Forms.TextBox textBox_Jahres_Verbrauch;
private System.Windows.Forms.TextBox textBox_Beschreibung;
private System.Windows.Forms.TextBox textBox_Stromtyp;
private System.Windows.Forms.Label Label15;
private System.Windows.Forms.Label Label11;
private System.Windows.Forms.Label Label19;
private System.Windows.Forms.Label Label18;
private System.Windows.Forms.TextBox textBox_StromSumme;
private System.Windows.Forms.ListBox listBox_Strom_DB;
private System.Windows.Forms.Button btn_Abbrechen;
private System.Windows.Forms.ListView listView_Strom_Auswahl;
private System.Windows.Forms.Label Label1;
        private System.Windows.Forms.Label label_Type;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox textBox_Verbrauch;
        private System.Windows.Forms.Label Label8;
        private System.Windows.Forms.Button btn_neuerWert;
    }
}