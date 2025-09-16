namespace WindowsFormsApplication1
{
    partial class Form_Prozesswaerme
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
            this.btn_Prozess_DBneu = new System.Windows.Forms.Button();
            this.btn_Prozess_loeschen = new System.Windows.Forms.Button();
            this.btn_ErgebnisseVerbrauch = new System.Windows.Forms.Button();
            this.btn_Simulation = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.Label24 = new System.Windows.Forms.Label();
            this.btn_ProzTypeDBedit = new System.Windows.Forms.Button();
            this.btn_Prozess_DBedit = new System.Windows.Forms.Button();
            this.Label12 = new System.Windows.Forms.Label();
            this.Label10 = new System.Windows.Forms.Label();
            this.textBox_Prozess_Name = new System.Windows.Forms.TextBox();
            this.Label13 = new System.Windows.Forms.Label();
            this.textBox_Jahres_Verbrauch = new System.Windows.Forms.TextBox();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.textBox_Prozess_Type = new System.Windows.Forms.TextBox();
            this.Label15 = new System.Windows.Forms.Label();
            this.Label11 = new System.Windows.Forms.Label();
            this.Label19 = new System.Windows.Forms.Label();
            this.Label18 = new System.Windows.Forms.Label();
            this.textBox_SummeProzesswaerme = new System.Windows.Forms.TextBox();
            this.listBox_Prozess_DB = new System.Windows.Forms.ListBox();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.listView_Prozess_Auswahl = new System.Windows.Forms.ListView();
            this.Label1 = new System.Windows.Forms.Label();
            this.textBox_Verbrauch = new System.Windows.Forms.TextBox();
            this.Label8 = new System.Windows.Forms.Label();
            this.btn_neuerWert = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btn_Hinzu
            // 
            this.btn_Hinzu.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Hinzu.Location = new System.Drawing.Point(332, 110);
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
            this.btn_Entfernen.Location = new System.Drawing.Point(332, 151);
            this.btn_Entfernen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Entfernen.Name = "btn_Entfernen";
            this.btn_Entfernen.Size = new System.Drawing.Size(82, 31);
            this.btn_Entfernen.TabIndex = 2;
            this.btn_Entfernen.Text = "--->";
            this.btn_Entfernen.UseVisualStyleBackColor = true;
            this.btn_Entfernen.Click += new System.EventHandler(this.btn_Entfernen_Click);
            // 
            // btn_Prozess_DBneu
            // 
            this.btn_Prozess_DBneu.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Prozess_DBneu.Location = new System.Drawing.Point(573, 276);
            this.btn_Prozess_DBneu.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Prozess_DBneu.Name = "btn_Prozess_DBneu";
            this.btn_Prozess_DBneu.Size = new System.Drawing.Size(154, 31);
            this.btn_Prozess_DBneu.TabIndex = 3;
            this.btn_Prozess_DBneu.Text = "Prozess in DB neu";
            this.btn_Prozess_DBneu.UseVisualStyleBackColor = true;
            // 
            // btn_Prozess_loeschen
            // 
            this.btn_Prozess_loeschen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Prozess_loeschen.Location = new System.Drawing.Point(573, 315);
            this.btn_Prozess_loeschen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Prozess_loeschen.Name = "btn_Prozess_loeschen";
            this.btn_Prozess_loeschen.Size = new System.Drawing.Size(154, 31);
            this.btn_Prozess_loeschen.TabIndex = 4;
            this.btn_Prozess_loeschen.Text = "Prozess in DB löschen";
            this.btn_Prozess_loeschen.UseVisualStyleBackColor = true;
            // 
            // btn_ErgebnisseVerbrauch
            // 
            this.btn_ErgebnisseVerbrauch.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_ErgebnisseVerbrauch.Location = new System.Drawing.Point(190, 558);
            this.btn_ErgebnisseVerbrauch.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_ErgebnisseVerbrauch.Name = "btn_ErgebnisseVerbrauch";
            this.btn_ErgebnisseVerbrauch.Size = new System.Drawing.Size(144, 31);
            this.btn_ErgebnisseVerbrauch.TabIndex = 6;
            this.btn_ErgebnisseVerbrauch.Text = "monatlicher Verlauf";
            this.btn_ErgebnisseVerbrauch.UseVisualStyleBackColor = true;
            this.btn_ErgebnisseVerbrauch.Click += new System.EventHandler(this.btn_ErgebnisseVerbrauch_Click);
            // 
            // btn_Simulation
            // 
            this.btn_Simulation.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Simulation.Location = new System.Drawing.Point(340, 558);
            this.btn_Simulation.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Simulation.Name = "btn_Simulation";
            this.btn_Simulation.Size = new System.Drawing.Size(119, 31);
            this.btn_Simulation.TabIndex = 9;
            this.btn_Simulation.Text = "Simulation";
            this.btn_Simulation.UseVisualStyleBackColor = true;
            this.btn_Simulation.Click += new System.EventHandler(this.btn_Simulation_Click);
            // 
            // btn_OK
            // 
            this.btn_OK.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_OK.Location = new System.Drawing.Point(633, 558);
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
            this.Label24.Location = new System.Drawing.Point(435, 31);
            this.Label24.Name = "Label24";
            this.Label24.Size = new System.Drawing.Size(167, 19);
            this.Label24.TabIndex = 12;
            this.Label24.Text = "Datenbank Prozesswärme";
            this.Label24.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btn_ProzTypeDBedit
            // 
            this.btn_ProzTypeDBedit.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_ProzTypeDBedit.Location = new System.Drawing.Point(573, 354);
            this.btn_ProzTypeDBedit.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_ProzTypeDBedit.Name = "btn_ProzTypeDBedit";
            this.btn_ProzTypeDBedit.Size = new System.Drawing.Size(154, 31);
            this.btn_ProzTypeDBedit.TabIndex = 13;
            this.btn_ProzTypeDBedit.Text = "Typ in DB ändern";
            this.btn_ProzTypeDBedit.UseVisualStyleBackColor = true;
            // 
            // btn_Prozess_DBedit
            // 
            this.btn_Prozess_DBedit.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Prozess_DBedit.Location = new System.Drawing.Point(573, 237);
            this.btn_Prozess_DBedit.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Prozess_DBedit.Name = "btn_Prozess_DBedit";
            this.btn_Prozess_DBedit.Size = new System.Drawing.Size(154, 31);
            this.btn_Prozess_DBedit.TabIndex = 15;
            this.btn_Prozess_DBedit.Text = "Prozess in DB ändern";
            this.btn_Prozess_DBedit.UseVisualStyleBackColor = true;
            // 
            // Label12
            // 
            this.Label12.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label12.Location = new System.Drawing.Point(5, 372);
            this.Label12.Name = "Label12";
            this.Label12.Size = new System.Drawing.Size(144, 52);
            this.Label12.TabIndex = 19;
            this.Label12.Text = "jährlicher Prozesswärmebedarf:";
            this.Label12.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label10
            // 
            this.Label10.AutoSize = true;
            this.Label10.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label10.Location = new System.Drawing.Point(101, 315);
            this.Label10.Name = "Label10";
            this.Label10.Size = new System.Drawing.Size(48, 19);
            this.Label10.TabIndex = 20;
            this.Label10.Text = "Name:";
            this.Label10.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Prozess_Name
            // 
            this.textBox_Prozess_Name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Prozess_Name.Enabled = false;
            this.textBox_Prozess_Name.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.textBox_Prozess_Name.Location = new System.Drawing.Point(157, 317);
            this.textBox_Prozess_Name.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Prozess_Name.Name = "textBox_Prozess_Name";
            this.textBox_Prozess_Name.Size = new System.Drawing.Size(331, 22);
            this.textBox_Prozess_Name.TabIndex = 21;
            // 
            // Label13
            // 
            this.Label13.AutoSize = true;
            this.Label13.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label13.Location = new System.Drawing.Point(55, 430);
            this.Label13.Name = "Label13";
            this.Label13.Size = new System.Drawing.Size(94, 19);
            this.Label13.TabIndex = 22;
            this.Label13.Text = "Beschreibung:";
            this.Label13.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Jahres_Verbrauch
            // 
            this.textBox_Jahres_Verbrauch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Jahres_Verbrauch.Enabled = false;
            this.textBox_Jahres_Verbrauch.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.textBox_Jahres_Verbrauch.Location = new System.Drawing.Point(157, 388);
            this.textBox_Jahres_Verbrauch.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Jahres_Verbrauch.Name = "textBox_Jahres_Verbrauch";
            this.textBox_Jahres_Verbrauch.Size = new System.Drawing.Size(63, 22);
            this.textBox_Jahres_Verbrauch.TabIndex = 23;
            // 
            // textBox_Beschreibung
            // 
            this.textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Beschreibung.Enabled = false;
            this.textBox_Beschreibung.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.textBox_Beschreibung.Location = new System.Drawing.Point(157, 432);
            this.textBox_Beschreibung.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Beschreibung.Multiline = true;
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            this.textBox_Beschreibung.Size = new System.Drawing.Size(510, 57);
            this.textBox_Beschreibung.TabIndex = 24;
            // 
            // textBox_Prozess_Type
            // 
            this.textBox_Prozess_Type.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Prozess_Type.Enabled = false;
            this.textBox_Prozess_Type.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.textBox_Prozess_Type.Location = new System.Drawing.Point(157, 349);
            this.textBox_Prozess_Type.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Prozess_Type.Name = "textBox_Prozess_Type";
            this.textBox_Prozess_Type.Size = new System.Drawing.Size(156, 22);
            this.textBox_Prozess_Type.TabIndex = 25;
            // 
            // Label15
            // 
            this.Label15.AutoSize = true;
            this.Label15.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label15.Location = new System.Drawing.Point(115, 347);
            this.Label15.Name = "Label15";
            this.Label15.Size = new System.Drawing.Size(34, 19);
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
            this.Label11.Location = new System.Drawing.Point(228, 388);
            this.Label11.Name = "Label11";
            this.Label11.Size = new System.Drawing.Size(48, 19);
            this.Label11.TabIndex = 27;
            this.Label11.Text = "MWth";
            this.Label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label19
            // 
            this.Label19.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label19.Location = new System.Drawing.Point(32, 494);
            this.Label19.Name = "Label19";
            this.Label19.Size = new System.Drawing.Size(157, 42);
            this.Label19.TabIndex = 28;
            this.Label19.Text = "Summe aller ausgewählten Prozesse:";
            this.Label19.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label18
            // 
            this.Label18.AutoSize = true;
            this.Label18.BackColor = System.Drawing.Color.Black;
            this.Label18.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label18.ForeColor = System.Drawing.Color.White;
            this.Label18.Location = new System.Drawing.Point(268, 517);
            this.Label18.Name = "Label18";
            this.Label18.Size = new System.Drawing.Size(48, 19);
            this.Label18.TabIndex = 29;
            this.Label18.Text = "MW/h";
            this.Label18.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBox_SummeProzesswaerme
            // 
            this.textBox_SummeProzesswaerme.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_SummeProzesswaerme.Enabled = false;
            this.textBox_SummeProzesswaerme.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.textBox_SummeProzesswaerme.Location = new System.Drawing.Point(197, 514);
            this.textBox_SummeProzesswaerme.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_SummeProzesswaerme.Name = "textBox_SummeProzesswaerme";
            this.textBox_SummeProzesswaerme.Size = new System.Drawing.Size(63, 22);
            this.textBox_SummeProzesswaerme.TabIndex = 30;
            // 
            // listBox_Prozess_DB
            // 
            this.listBox_Prozess_DB.FormattingEnabled = true;
            this.listBox_Prozess_DB.ItemHeight = 17;
            this.listBox_Prozess_DB.Location = new System.Drawing.Point(439, 53);
            this.listBox_Prozess_DB.Name = "listBox_Prozess_DB";
            this.listBox_Prozess_DB.Size = new System.Drawing.Size(288, 174);
            this.listBox_Prozess_DB.TabIndex = 32;
            this.listBox_Prozess_DB.SelectedIndexChanged += new System.EventHandler(this.listBox_Prozess_DB_SelectedIndexChanged);
            // 
            // btn_Abbrechen
            // 
            this.btn_Abbrechen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Abbrechen.Location = new System.Drawing.Point(522, 558);
            this.btn_Abbrechen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.Size = new System.Drawing.Size(105, 31);
            this.btn_Abbrechen.TabIndex = 33;
            this.btn_Abbrechen.Text = "Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // listView_Prozess_Auswahl
            // 
            this.listView_Prozess_Auswahl.Location = new System.Drawing.Point(16, 53);
            this.listView_Prozess_Auswahl.Name = "listView_Prozess_Auswahl";
            this.listView_Prozess_Auswahl.Size = new System.Drawing.Size(284, 178);
            this.listView_Prozess_Auswahl.TabIndex = 41;
            this.listView_Prozess_Auswahl.UseCompatibleStateImageBehavior = false;
            // 
            // Label1
            // 
            this.Label1.AutoSize = true;
            this.Label1.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label1.Location = new System.Drawing.Point(12, 31);
            this.Label1.Name = "Label1";
            this.Label1.Size = new System.Drawing.Size(210, 19);
            this.Label1.TabIndex = 37;
            this.Label1.Text = "Ausgewählte Prozesse im Projekt";
            this.Label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBox_Verbrauch
            // 
            this.textBox_Verbrauch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Verbrauch.Enabled = false;
            this.textBox_Verbrauch.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.textBox_Verbrauch.Location = new System.Drawing.Point(16, 239);
            this.textBox_Verbrauch.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Verbrauch.Name = "textBox_Verbrauch";
            this.textBox_Verbrauch.Size = new System.Drawing.Size(63, 22);
            this.textBox_Verbrauch.TabIndex = 38;
            // 
            // Label8
            // 
            this.Label8.AutoSize = true;
            this.Label8.BackColor = System.Drawing.Color.Black;
            this.Label8.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label8.ForeColor = System.Drawing.Color.White;
            this.Label8.Location = new System.Drawing.Point(88, 242);
            this.Label8.Name = "Label8";
            this.Label8.Size = new System.Drawing.Size(43, 19);
            this.Label8.TabIndex = 39;
            this.Label8.Text = "MWh";
            this.Label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btn_neuerWert
            // 
            this.btn_neuerWert.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_neuerWert.Location = new System.Drawing.Point(16, 270);
            this.btn_neuerWert.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_neuerWert.Name = "btn_neuerWert";
            this.btn_neuerWert.Size = new System.Drawing.Size(115, 30);
            this.btn_neuerWert.TabIndex = 40;
            this.btn_neuerWert.Text = "Neuer Wert...";
            this.btn_neuerWert.UseVisualStyleBackColor = true;
            // 
            // Form_Prozesswaerme
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(758, 603);
            this.Controls.Add(this.listView_Prozess_Auswahl);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.textBox_Verbrauch);
            this.Controls.Add(this.Label8);
            this.Controls.Add(this.btn_neuerWert);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.listBox_Prozess_DB);
            this.Controls.Add(this.btn_Hinzu);
            this.Controls.Add(this.btn_Entfernen);
            this.Controls.Add(this.btn_Prozess_DBneu);
            this.Controls.Add(this.btn_Prozess_loeschen);
            this.Controls.Add(this.btn_ErgebnisseVerbrauch);
            this.Controls.Add(this.btn_Simulation);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.Label24);
            this.Controls.Add(this.btn_ProzTypeDBedit);
            this.Controls.Add(this.btn_Prozess_DBedit);
            this.Controls.Add(this.Label12);
            this.Controls.Add(this.Label10);
            this.Controls.Add(this.textBox_Prozess_Name);
            this.Controls.Add(this.Label13);
            this.Controls.Add(this.textBox_Jahres_Verbrauch);
            this.Controls.Add(this.textBox_Beschreibung);
            this.Controls.Add(this.textBox_Prozess_Type);
            this.Controls.Add(this.Label15);
            this.Controls.Add(this.Label11);
            this.Controls.Add(this.Label19);
            this.Controls.Add(this.Label18);
            this.Controls.Add(this.textBox_SummeProzesswaerme);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Form_Prozesswaerme";
            this.Text = "Prozesswärme Verwaltung";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_Hinzu;
private System.Windows.Forms.Button btn_Entfernen;
private System.Windows.Forms.Button btn_Prozess_DBneu;
private System.Windows.Forms.Button btn_Prozess_loeschen;
private System.Windows.Forms.Button btn_ErgebnisseVerbrauch;
private System.Windows.Forms.Button btn_Simulation;
private System.Windows.Forms.Button btn_OK;
private System.Windows.Forms.Label Label24;
private System.Windows.Forms.Button btn_ProzTypeDBedit;
private System.Windows.Forms.Button btn_Prozess_DBedit;
private System.Windows.Forms.Label Label12;
private System.Windows.Forms.Label Label10;
private System.Windows.Forms.TextBox textBox_Prozess_Name;
private System.Windows.Forms.Label Label13;
private System.Windows.Forms.TextBox textBox_Jahres_Verbrauch;
private System.Windows.Forms.TextBox textBox_Beschreibung;
private System.Windows.Forms.TextBox textBox_Prozess_Type;
private System.Windows.Forms.Label Label15;
private System.Windows.Forms.Label Label11;
private System.Windows.Forms.Label Label19;
private System.Windows.Forms.Label Label18;
private System.Windows.Forms.TextBox textBox_SummeProzesswaerme;
private System.Windows.Forms.ListBox listBox_Prozess_DB;
private System.Windows.Forms.Button btn_Abbrechen;
private System.Windows.Forms.ListView listView_Prozess_Auswahl;
private System.Windows.Forms.Label Label1;
private System.Windows.Forms.TextBox textBox_Verbrauch;
private System.Windows.Forms.Label Label8;
private System.Windows.Forms.Button btn_neuerWert;


 
    }
}