namespace WindowsFormsApplication1
{
    partial class Form_StromverbraucherEingabe
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
            this.btn_VerbraucherHinzu = new System.Windows.Forms.Button();
            this.btn_VerbraucherLöschen = new System.Windows.Forms.Button();
            this.btn_VerbraucherDBneu = new System.Windows.Forms.Button();
            this.btn_VerbraucherDBloeschen = new System.Windows.Forms.Button();
            this.lb_ListeVerbraucherEing = new System.Windows.Forms.ListBox();
            this.Label24 = new System.Windows.Forms.Label();
            this.btn_TypeDBedit = new System.Windows.Forms.Button();
            this.lb_ListeVerbraucherDB = new System.Windows.Forms.ListBox();
            this.btn_VerbraucherDBedit = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.btn_Simulation = new System.Windows.Forms.Button();
            this.btn_Ergebnisse_BHKW = new System.Windows.Forms.Button();
            this.btn_Ganglinie_Speichern = new System.Windows.Forms.Button();
            this.btn_Hilfe = new System.Windows.Forms.Button();
            this.btn_Protokoll_BHKW = new System.Windows.Forms.Button();
            this.Label8 = new System.Windows.Forms.Label();
            this.tb_ScalVerbrauch = new System.Windows.Forms.TextBox();
            this.btn_neuerWert = new System.Windows.Forms.Button();
            this.Label13 = new System.Windows.Forms.Label();
            this.Label10 = new System.Windows.Forms.Label();
            this.tb_VerbraucherName = new System.Windows.Forms.TextBox();
            this.Label12 = new System.Windows.Forms.Label();
            this.tb_J_Verbrauch = new System.Windows.Forms.TextBox();
            this.tb_Beschreibung = new System.Windows.Forms.TextBox();
            this.tb_VerbraucherType = new System.Windows.Forms.TextBox();
            this.Label15 = new System.Windows.Forms.Label();
            this.Label11 = new System.Windows.Forms.Label();
            this.Label19 = new System.Windows.Forms.Label();
            this.Label18 = new System.Windows.Forms.Label();
            this.tb_SummeVerbrauch = new System.Windows.Forms.TextBox();
            this.ComboBox1 = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // Label1
            // 
            this.Label1.AutoSize = true;
            this.Label1.Font = new System.Drawing.Font("Tahoma", 8F);
            this.Label1.Location = new System.Drawing.Point(9, 8);
            this.Label1.Name = "Label1";
            this.Label1.Size = new System.Drawing.Size(163, 13);
            this.Label1.TabIndex = 0;
            this.Label1.Text = "Ausgewählte Stromverbraucher:";
            this.Label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btn_VerbraucherHinzu
            // 
            this.btn_VerbraucherHinzu.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_VerbraucherHinzu.Location = new System.Drawing.Point(198, 30);
            this.btn_VerbraucherHinzu.Name = "btn_VerbraucherHinzu";
            this.btn_VerbraucherHinzu.Size = new System.Drawing.Size(264, 24);
            this.btn_VerbraucherHinzu.TabIndex = 1;
            this.btn_VerbraucherHinzu.Text = "<-------  Aus der Datenbank hinzufügen";
            this.btn_VerbraucherHinzu.UseVisualStyleBackColor = true;
            // 
            // btn_VerbraucherLöschen
            // 
            this.btn_VerbraucherLöschen.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_VerbraucherLöschen.Location = new System.Drawing.Point(198, 60);
            this.btn_VerbraucherLöschen.Name = "btn_VerbraucherLöschen";
            this.btn_VerbraucherLöschen.Size = new System.Drawing.Size(264, 24);
            this.btn_VerbraucherLöschen.TabIndex = 2;
            this.btn_VerbraucherLöschen.Text = "Aus der Auswahl löschen ------>";
            this.btn_VerbraucherLöschen.UseVisualStyleBackColor = true;
            // 
            // btn_VerbraucherDBneu
            // 
            this.btn_VerbraucherDBneu.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_VerbraucherDBneu.Location = new System.Drawing.Point(492, 192);
            this.btn_VerbraucherDBneu.Name = "btn_VerbraucherDBneu";
            this.btn_VerbraucherDBneu.Size = new System.Drawing.Size(162, 24);
            this.btn_VerbraucherDBneu.TabIndex = 3;
            this.btn_VerbraucherDBneu.Text = "Verbraucher in DB neu";
            this.btn_VerbraucherDBneu.UseVisualStyleBackColor = true;
            // 
            // btn_VerbraucherDBloeschen
            // 
            this.btn_VerbraucherDBloeschen.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_VerbraucherDBloeschen.Location = new System.Drawing.Point(492, 222);
            this.btn_VerbraucherDBloeschen.Name = "btn_VerbraucherDBloeschen";
            this.btn_VerbraucherDBloeschen.Size = new System.Drawing.Size(162, 24);
            this.btn_VerbraucherDBloeschen.TabIndex = 4;
            this.btn_VerbraucherDBloeschen.Text = "Verbraucher in DB löschen";
            this.btn_VerbraucherDBloeschen.UseVisualStyleBackColor = true;
            // 
            // lb_ListeVerbraucherEing
            // 
            this.lb_ListeVerbraucherEing.Location = new System.Drawing.Point(9, 24);
            this.lb_ListeVerbraucherEing.Name = "lb_ListeVerbraucherEing";
            this.lb_ListeVerbraucherEing.Size = new System.Drawing.Size(120, 108);
            this.lb_ListeVerbraucherEing.TabIndex = 5;
            // 
            // Label24
            // 
            this.Label24.AutoSize = true;
            this.Label24.Font = new System.Drawing.Font("Tahoma", 8F);
            this.Label24.Location = new System.Drawing.Point(492, 12);
            this.Label24.Name = "Label24";
            this.Label24.Size = new System.Drawing.Size(154, 13);
            this.Label24.TabIndex = 6;
            this.Label24.Text = "Stromverbraucher-Datenbank:";
            this.Label24.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btn_TypeDBedit
            // 
            this.btn_TypeDBedit.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_TypeDBedit.Location = new System.Drawing.Point(324, 162);
            this.btn_TypeDBedit.Name = "btn_TypeDBedit";
            this.btn_TypeDBedit.Size = new System.Drawing.Size(162, 24);
            this.btn_TypeDBedit.TabIndex = 7;
            this.btn_TypeDBedit.Text = "Typ in DB ändern";
            this.btn_TypeDBedit.UseVisualStyleBackColor = true;
            // 
            // lb_ListeVerbraucherDB
            // 
            this.lb_ListeVerbraucherDB.Location = new System.Drawing.Point(489, 30);
            this.lb_ListeVerbraucherDB.Name = "lb_ListeVerbraucherDB";
            this.lb_ListeVerbraucherDB.Size = new System.Drawing.Size(165, 95);
            this.lb_ListeVerbraucherDB.TabIndex = 8;
            // 
            // btn_VerbraucherDBedit
            // 
            this.btn_VerbraucherDBedit.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_VerbraucherDBedit.Location = new System.Drawing.Point(492, 162);
            this.btn_VerbraucherDBedit.Name = "btn_VerbraucherDBedit";
            this.btn_VerbraucherDBedit.Size = new System.Drawing.Size(162, 24);
            this.btn_VerbraucherDBedit.TabIndex = 9;
            this.btn_VerbraucherDBedit.Text = "Verbraucher in DB ändern";
            this.btn_VerbraucherDBedit.UseVisualStyleBackColor = true;
            // 
            // btn_OK
            // 
            this.btn_OK.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_OK.Location = new System.Drawing.Point(576, 444);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(78, 24);
            this.btn_OK.TabIndex = 10;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            // 
            // btn_Simulation
            // 
            this.btn_Simulation.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_Simulation.Location = new System.Drawing.Point(438, 444);
            this.btn_Simulation.Name = "btn_Simulation";
            this.btn_Simulation.Size = new System.Drawing.Size(102, 24);
            this.btn_Simulation.TabIndex = 11;
            this.btn_Simulation.Text = "Simulation";
            this.btn_Simulation.UseVisualStyleBackColor = true;
            // 
            // btn_Ergebnisse_BHKW
            // 
            this.btn_Ergebnisse_BHKW.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_Ergebnisse_BHKW.Location = new System.Drawing.Point(222, 444);
            this.btn_Ergebnisse_BHKW.Name = "btn_Ergebnisse_BHKW";
            this.btn_Ergebnisse_BHKW.Size = new System.Drawing.Size(96, 24);
            this.btn_Ergebnisse_BHKW.TabIndex = 12;
            this.btn_Ergebnisse_BHKW.Text = "Strom-Ganglinie einlesen";
            this.btn_Ergebnisse_BHKW.UseVisualStyleBackColor = true;
            // 
            // btn_Ganglinie_Speichern
            // 
            this.btn_Ganglinie_Speichern.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_Ganglinie_Speichern.Location = new System.Drawing.Point(108, 444);
            this.btn_Ganglinie_Speichern.Name = "btn_Ganglinie_Speichern";
            this.btn_Ganglinie_Speichern.Size = new System.Drawing.Size(102, 24);
            this.btn_Ganglinie_Speichern.TabIndex = 13;
            this.btn_Ganglinie_Speichern.Text = "Ganglinie speichern";
            this.btn_Ganglinie_Speichern.UseVisualStyleBackColor = true;
            // 
            // btn_Hilfe
            // 
            this.btn_Hilfe.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_Hilfe.Location = new System.Drawing.Point(12, 444);
            this.btn_Hilfe.Name = "btn_Hilfe";
            this.btn_Hilfe.Size = new System.Drawing.Size(78, 24);
            this.btn_Hilfe.TabIndex = 14;
            this.btn_Hilfe.Text = "Hilfe";
            this.btn_Hilfe.UseVisualStyleBackColor = true;
            // 
            // btn_Protokoll_BHKW
            // 
            this.btn_Protokoll_BHKW.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_Protokoll_BHKW.Location = new System.Drawing.Point(330, 444);
            this.btn_Protokoll_BHKW.Name = "btn_Protokoll_BHKW";
            this.btn_Protokoll_BHKW.Size = new System.Drawing.Size(96, 24);
            this.btn_Protokoll_BHKW.TabIndex = 15;
            this.btn_Protokoll_BHKW.Text = "Protokoll";
            this.btn_Protokoll_BHKW.UseVisualStyleBackColor = true;
            // 
            // Label8
            // 
            this.Label8.AutoSize = true;
            this.Label8.Font = new System.Drawing.Font("Tahoma", 8F);
            this.Label8.Location = new System.Drawing.Point(78, 161);
            this.Label8.Name = "Label8";
            this.Label8.Size = new System.Drawing.Size(31, 13);
            this.Label8.TabIndex = 16;
            this.Label8.Text = "MWh";
            this.Label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tb_ScalVerbrauch
            // 
            this.tb_ScalVerbrauch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tb_ScalVerbrauch.Enabled = false;
            this.tb_ScalVerbrauch.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.tb_ScalVerbrauch.Location = new System.Drawing.Point(18, 156);
            this.tb_ScalVerbrauch.Margin = new System.Windows.Forms.Padding(4);
            this.tb_ScalVerbrauch.Name = "tb_ScalVerbrauch";
            this.tb_ScalVerbrauch.Size = new System.Drawing.Size(54, 22);
            this.tb_ScalVerbrauch.TabIndex = 17;
            // 
            // btn_neuerWert
            // 
            this.btn_neuerWert.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_neuerWert.Location = new System.Drawing.Point(18, 180);
            this.btn_neuerWert.Name = "btn_neuerWert";
            this.btn_neuerWert.Size = new System.Drawing.Size(84, 18);
            this.btn_neuerWert.TabIndex = 18;
            this.btn_neuerWert.Text = "Neuer Wert";
            this.btn_neuerWert.UseVisualStyleBackColor = true;
            // 
            // Label13
            // 
            this.Label13.AutoSize = true;
            this.Label13.Font = new System.Drawing.Font("Tahoma", 8F);
            this.Label13.Location = new System.Drawing.Point(18, 300);
            this.Label13.Name = "Label13";
            this.Label13.Size = new System.Drawing.Size(75, 13);
            this.Label13.TabIndex = 19;
            this.Label13.Text = "Beschreibung:";
            this.Label13.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label10
            // 
            this.Label10.AutoSize = true;
            this.Label10.Font = new System.Drawing.Font("Tahoma", 8F);
            this.Label10.Location = new System.Drawing.Point(54, 264);
            this.Label10.Name = "Label10";
            this.Label10.Size = new System.Drawing.Size(38, 13);
            this.Label10.TabIndex = 20;
            this.Label10.Text = "Name:";
            this.Label10.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // tb_VerbraucherName
            // 
            this.tb_VerbraucherName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tb_VerbraucherName.Enabled = false;
            this.tb_VerbraucherName.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.tb_VerbraucherName.Location = new System.Drawing.Point(120, 258);
            this.tb_VerbraucherName.Margin = new System.Windows.Forms.Padding(4);
            this.tb_VerbraucherName.Name = "tb_VerbraucherName";
            this.tb_VerbraucherName.Size = new System.Drawing.Size(156, 22);
            this.tb_VerbraucherName.TabIndex = 21;
            // 
            // Label12
            // 
            this.Label12.AutoSize = true;
            this.Label12.Font = new System.Drawing.Font("Tahoma", 8F);
            this.Label12.Location = new System.Drawing.Point(6, 282);
            this.Label12.Name = "Label12";
            this.Label12.Size = new System.Drawing.Size(135, 13);
            this.Label12.TabIndex = 22;
            this.Label12.Text = "jährlicher Stromverbrauch:";
            this.Label12.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // tb_J_Verbrauch
            // 
            this.tb_J_Verbrauch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tb_J_Verbrauch.Enabled = false;
            this.tb_J_Verbrauch.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.tb_J_Verbrauch.Location = new System.Drawing.Point(120, 276);
            this.tb_J_Verbrauch.Margin = new System.Windows.Forms.Padding(4);
            this.tb_J_Verbrauch.Name = "tb_J_Verbrauch";
            this.tb_J_Verbrauch.Size = new System.Drawing.Size(54, 22);
            this.tb_J_Verbrauch.TabIndex = 23;
            // 
            // tb_Beschreibung
            // 
            this.tb_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tb_Beschreibung.Enabled = false;
            this.tb_Beschreibung.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.tb_Beschreibung.Location = new System.Drawing.Point(120, 300);
            this.tb_Beschreibung.Margin = new System.Windows.Forms.Padding(4);
            this.tb_Beschreibung.Name = "tb_Beschreibung";
            this.tb_Beschreibung.Size = new System.Drawing.Size(510, 22);
            this.tb_Beschreibung.TabIndex = 24;
            // 
            // tb_VerbraucherType
            // 
            this.tb_VerbraucherType.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tb_VerbraucherType.Enabled = false;
            this.tb_VerbraucherType.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.tb_VerbraucherType.Location = new System.Drawing.Point(348, 258);
            this.tb_VerbraucherType.Margin = new System.Windows.Forms.Padding(4);
            this.tb_VerbraucherType.Name = "tb_VerbraucherType";
            this.tb_VerbraucherType.Size = new System.Drawing.Size(156, 22);
            this.tb_VerbraucherType.TabIndex = 25;
            // 
            // Label15
            // 
            this.Label15.AutoSize = true;
            this.Label15.Font = new System.Drawing.Font("Tahoma", 8F);
            this.Label15.Location = new System.Drawing.Point(312, 264);
            this.Label15.Name = "Label15";
            this.Label15.Size = new System.Drawing.Size(29, 13);
            this.Label15.TabIndex = 26;
            this.Label15.Text = "Typ:";
            this.Label15.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label11
            // 
            this.Label11.AutoSize = true;
            this.Label11.Font = new System.Drawing.Font("Tahoma", 8F);
            this.Label11.Location = new System.Drawing.Point(180, 282);
            this.Label11.Name = "Label11";
            this.Label11.Size = new System.Drawing.Size(31, 13);
            this.Label11.TabIndex = 27;
            this.Label11.Text = "MWh";
            this.Label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label19
            // 
            this.Label19.AutoSize = true;
            this.Label19.Font = new System.Drawing.Font("Tahoma", 8F);
            this.Label19.Location = new System.Drawing.Point(30, 366);
            this.Label19.Name = "Label19";
            this.Label19.Size = new System.Drawing.Size(163, 13);
            this.Label19.TabIndex = 28;
            this.Label19.Text = "aller ausgewählten Verbraucher:";
            this.Label19.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label18
            // 
            this.Label18.AutoSize = true;
            this.Label18.Font = new System.Drawing.Font("Tahoma", 8F);
            this.Label18.Location = new System.Drawing.Point(228, 371);
            this.Label18.Name = "Label18";
            this.Label18.Size = new System.Drawing.Size(31, 13);
            this.Label18.TabIndex = 29;
            this.Label18.Text = "MWh";
            this.Label18.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tb_SummeVerbrauch
            // 
            this.tb_SummeVerbrauch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tb_SummeVerbrauch.Enabled = false;
            this.tb_SummeVerbrauch.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.tb_SummeVerbrauch.Location = new System.Drawing.Point(168, 366);
            this.tb_SummeVerbrauch.Margin = new System.Windows.Forms.Padding(4);
            this.tb_SummeVerbrauch.Name = "tb_SummeVerbrauch";
            this.tb_SummeVerbrauch.Size = new System.Drawing.Size(54, 22);
            this.tb_SummeVerbrauch.TabIndex = 30;
            // 
            // ComboBox1
            // 
            this.ComboBox1.Location = new System.Drawing.Point(168, 128);
            this.ComboBox1.Name = "ComboBox1";
            this.ComboBox1.Size = new System.Drawing.Size(121, 21);
            this.ComboBox1.TabIndex = 31;
            // 
            // Form_StromverbraucherEingabe
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(675, 479);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.btn_VerbraucherHinzu);
            this.Controls.Add(this.btn_VerbraucherLöschen);
            this.Controls.Add(this.btn_VerbraucherDBneu);
            this.Controls.Add(this.btn_VerbraucherDBloeschen);
            this.Controls.Add(this.lb_ListeVerbraucherEing);
            this.Controls.Add(this.Label24);
            this.Controls.Add(this.btn_TypeDBedit);
            this.Controls.Add(this.lb_ListeVerbraucherDB);
            this.Controls.Add(this.btn_VerbraucherDBedit);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.btn_Simulation);
            this.Controls.Add(this.btn_Ergebnisse_BHKW);
            this.Controls.Add(this.btn_Ganglinie_Speichern);
            this.Controls.Add(this.btn_Hilfe);
            this.Controls.Add(this.btn_Protokoll_BHKW);
            this.Controls.Add(this.Label8);
            this.Controls.Add(this.tb_ScalVerbrauch);
            this.Controls.Add(this.btn_neuerWert);
            this.Controls.Add(this.Label13);
            this.Controls.Add(this.Label10);
            this.Controls.Add(this.tb_VerbraucherName);
            this.Controls.Add(this.Label12);
            this.Controls.Add(this.tb_J_Verbrauch);
            this.Controls.Add(this.tb_Beschreibung);
            this.Controls.Add(this.tb_VerbraucherType);
            this.Controls.Add(this.Label15);
            this.Controls.Add(this.Label11);
            this.Controls.Add(this.Label19);
            this.Controls.Add(this.Label18);
            this.Controls.Add(this.tb_SummeVerbrauch);
            this.Controls.Add(this.ComboBox1);
            this.Name = "Form_StromverbraucherEingabe";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

private System.Windows.Forms.Label Label1;
private System.Windows.Forms.Button btn_VerbraucherHinzu;
private System.Windows.Forms.Button btn_VerbraucherLöschen;
private System.Windows.Forms.Button btn_VerbraucherDBneu;
private System.Windows.Forms.Button btn_VerbraucherDBloeschen;
private System.Windows.Forms.ListBox lb_ListeVerbraucherEing;
private System.Windows.Forms.Label Label24;
private System.Windows.Forms.Button btn_TypeDBedit;
private System.Windows.Forms.ListBox lb_ListeVerbraucherDB;
private System.Windows.Forms.Button btn_VerbraucherDBedit;
private System.Windows.Forms.Button btn_OK;
private System.Windows.Forms.Button btn_Simulation;
private System.Windows.Forms.Button btn_Ergebnisse_BHKW;
private System.Windows.Forms.Button btn_Ganglinie_Speichern;
private System.Windows.Forms.Button btn_Hilfe;
private System.Windows.Forms.Button btn_Protokoll_BHKW;
private System.Windows.Forms.Label Label8;
private System.Windows.Forms.TextBox tb_ScalVerbrauch;
private System.Windows.Forms.Button btn_neuerWert;
private System.Windows.Forms.Label Label13;
private System.Windows.Forms.Label Label10;
private System.Windows.Forms.TextBox tb_VerbraucherName;
private System.Windows.Forms.Label Label12;
private System.Windows.Forms.TextBox tb_J_Verbrauch;
private System.Windows.Forms.TextBox tb_Beschreibung;
private System.Windows.Forms.TextBox tb_VerbraucherType;
private System.Windows.Forms.Label Label15;
private System.Windows.Forms.Label Label11;
private System.Windows.Forms.Label Label19;
private System.Windows.Forms.Label Label18;
private System.Windows.Forms.TextBox tb_SummeVerbrauch;
private System.Windows.Forms.ComboBox ComboBox1;


 
    }
}