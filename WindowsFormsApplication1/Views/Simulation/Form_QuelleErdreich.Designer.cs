namespace WindowsFormsApplication1
{
    partial class Form_QuelleErdreich
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
            this._gbSystem = new System.Windows.Forms.GroupBox();
            this._rbKollektor = new System.Windows.Forms.RadioButton();
            this._lblVerlegetiefe = new System.Windows.Forms.Label();
            this._tbTiefe = new System.Windows.Forms.TextBox();
            this._lblFlaeche = new System.Windows.Forms.Label();
            this._tbFlaeche = new System.Windows.Forms.TextBox();
            this._rbSonde = new System.Windows.Forms.RadioButton();
            this._lblLaengeSonde = new System.Windows.Forms.Label();
            this._tbLaenge = new System.Windows.Forms.TextBox();
            this._lblAnzahlSonden = new System.Windows.Forms.Label();
            this._tbAnzahl = new System.Windows.Forms.TextBox();
            this._lblBodentyp = new System.Windows.Forms.Label();
            this._cbBoden = new System.Windows.Forms.ComboBox();
            this._lblBodentypHinweis = new System.Windows.Forms.Label();
            this._lblBoden = new System.Windows.Forms.Label();
            this._lblKlimazone = new System.Windows.Forms.Label();
            this._cbZone = new System.Windows.Forms.ComboBox();
            this._lblKlimazoneHinweis = new System.Windows.Forms.Label();
            this._lblSpreizung = new System.Windows.Forms.Label();
            this._tbSpreizung = new System.Windows.Forms.TextBox();
            this._lblSpreizungHinweis = new System.Windows.Forms.Label();
            this._gbVorschau = new System.Windows.Forms.GroupBox();
            this._lblKennwerte = new System.Windows.Forms.Label();
            this._gbPruefung = new System.Windows.Forms.GroupBox();
            this._lblPruefung = new System.Windows.Forms.Label();
            this._lblAenderung = new System.Windows.Forms.Label();
            this._btnSimulation = new System.Windows.Forms.Button();
            this._btnOk = new System.Windows.Forms.Button();
            this._btnAbbruch = new System.Windows.Forms.Button();
            this._gbSystem.SuspendLayout();
            this._gbVorschau.SuspendLayout();
            this._gbPruefung.SuspendLayout();
            this.SuspendLayout();
            //
            // _rbKollektor
            //
            this._rbKollektor.AutoSize = true;
            this._rbKollektor.Checked = true;
            this._rbKollektor.Location = new System.Drawing.Point(16, 26);
            this._rbKollektor.Name = "_rbKollektor";
            this._rbKollektor.Text = "Erdkollektor";
            this._rbKollektor.CheckedChanged += new System.EventHandler(this.rbQuellsystem_CheckedChanged);
            //
            // _lblVerlegetiefe
            //
            this._lblVerlegetiefe.AutoSize = true;
            this._lblVerlegetiefe.Location = new System.Drawing.Point(160, 28);
            this._lblVerlegetiefe.Name = "_lblVerlegetiefe";
            this._lblVerlegetiefe.Text = "Verlegetiefe [m]:";
            //
            // _tbTiefe
            //
            this._tbTiefe.Location = new System.Drawing.Point(285, 25);
            this._tbTiefe.Name = "_tbTiefe";
            this._tbTiefe.Width = 70;
            this._tbTiefe.TextChanged += new System.EventHandler(this.eingabe_TextChanged);
            //
            // _lblFlaeche
            //
            this._lblFlaeche.AutoSize = true;
            this._lblFlaeche.Location = new System.Drawing.Point(390, 28);
            this._lblFlaeche.Name = "_lblFlaeche";
            this._lblFlaeche.Text = "Fläche [m²]:";
            //
            // _tbFlaeche
            //
            this._tbFlaeche.Location = new System.Drawing.Point(490, 25);
            this._tbFlaeche.Name = "_tbFlaeche";
            this._tbFlaeche.Width = 70;
            this._tbFlaeche.TextChanged += new System.EventHandler(this.eingabe_TextChanged);
            //
            // _rbSonde
            //
            this._rbSonde.AutoSize = true;
            this._rbSonde.Location = new System.Drawing.Point(16, 76);
            this._rbSonde.Name = "_rbSonde";
            this._rbSonde.Text = "Erdsonde";
            this._rbSonde.CheckedChanged += new System.EventHandler(this.rbQuellsystem_CheckedChanged);
            //
            // _lblLaengeSonde
            //
            this._lblLaengeSonde.AutoSize = true;
            this._lblLaengeSonde.Location = new System.Drawing.Point(160, 78);
            this._lblLaengeSonde.Name = "_lblLaengeSonde";
            this._lblLaengeSonde.Text = "Länge je Sonde [m]:";
            //
            // _tbLaenge
            //
            this._tbLaenge.Location = new System.Drawing.Point(285, 75);
            this._tbLaenge.Name = "_tbLaenge";
            this._tbLaenge.Width = 70;
            this._tbLaenge.TextChanged += new System.EventHandler(this.eingabe_TextChanged);
            //
            // _lblAnzahlSonden
            //
            this._lblAnzahlSonden.AutoSize = true;
            this._lblAnzahlSonden.Location = new System.Drawing.Point(390, 78);
            this._lblAnzahlSonden.Name = "_lblAnzahlSonden";
            this._lblAnzahlSonden.Text = "Anzahl Sonden:";
            //
            // _tbAnzahl
            //
            this._tbAnzahl.Location = new System.Drawing.Point(490, 75);
            this._tbAnzahl.Name = "_tbAnzahl";
            this._tbAnzahl.Width = 70;
            this._tbAnzahl.TextChanged += new System.EventHandler(this.eingabe_TextChanged);
            //
            // _gbSystem
            //
            this._gbSystem.Controls.Add(this._rbKollektor);
            this._gbSystem.Controls.Add(this._lblVerlegetiefe);
            this._gbSystem.Controls.Add(this._tbTiefe);
            this._gbSystem.Controls.Add(this._lblFlaeche);
            this._gbSystem.Controls.Add(this._tbFlaeche);
            this._gbSystem.Controls.Add(this._rbSonde);
            this._gbSystem.Controls.Add(this._lblLaengeSonde);
            this._gbSystem.Controls.Add(this._tbLaenge);
            this._gbSystem.Controls.Add(this._lblAnzahlSonden);
            this._gbSystem.Controls.Add(this._tbAnzahl);
            this._gbSystem.Location = new System.Drawing.Point(12, 10);
            this._gbSystem.Name = "_gbSystem";
            this._gbSystem.Size = new System.Drawing.Size(676, 120);
            this._gbSystem.Text = "Quellsystem";
            //
            // _lblBodentyp
            //
            this._lblBodentyp.AutoSize = true;
            this._lblBodentyp.Location = new System.Drawing.Point(28, 145);
            this._lblBodentyp.Name = "_lblBodentyp";
            this._lblBodentyp.Text = "Bodentyp:";
            //
            // _cbBoden
            //
            this._cbBoden.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbBoden.Location = new System.Drawing.Point(170, 142);
            this._cbBoden.Name = "_cbBoden";
            this._cbBoden.Width = 230;
            this._cbBoden.SelectedIndexChanged += new System.EventHandler(this.auswahl_SelectedIndexChanged);
            //
            // _lblBodentypHinweis
            //
            this._lblBodentypHinweis.AutoSize = true;
            this._lblBodentypHinweis.Location = new System.Drawing.Point(412, 145);
            this._lblBodentypHinweis.Name = "_lblBodentypHinweis";
            this._lblBodentypHinweis.Text = "(Katalog VDI 4640 Blatt 1, Entwurf 2021-12)";
            //
            // _lblBoden
            //
            this._lblBoden.AutoSize = false;
            this._lblBoden.ForeColor = System.Drawing.SystemColors.GrayText;
            this._lblBoden.Location = new System.Drawing.Point(28, 170);
            this._lblBoden.Name = "_lblBoden";
            this._lblBoden.Size = new System.Drawing.Size(660, 32);
            //
            // _lblKlimazone
            //
            this._lblKlimazone.AutoSize = true;
            this._lblKlimazone.Location = new System.Drawing.Point(28, 212);
            this._lblKlimazone.Name = "_lblKlimazone";
            this._lblKlimazone.Text = "Klimazone:";
            //
            // _cbZone
            //
            this._cbZone.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbZone.Location = new System.Drawing.Point(170, 209);
            this._cbZone.Name = "_cbZone";
            this._cbZone.Width = 230;
            this._cbZone.SelectedIndexChanged += new System.EventHandler(this.auswahl_SelectedIndexChanged);
            //
            // _lblKlimazoneHinweis
            //
            this._lblKlimazoneHinweis.AutoSize = true;
            this._lblKlimazoneHinweis.Location = new System.Drawing.Point(412, 212);
            this._lblKlimazoneHinweis.Name = "_lblKlimazoneHinweis";
            this._lblKlimazoneHinweis.Text = "(DIN 4710, Vorbelegung aus der Klimaregion)";
            //
            // _lblSpreizung
            //
            this._lblSpreizung.AutoSize = true;
            this._lblSpreizung.Location = new System.Drawing.Point(28, 242);
            this._lblSpreizung.Name = "_lblSpreizung";
            this._lblSpreizung.Text = "Nutzbare Spreizung [K]:";
            //
            // _tbSpreizung
            //
            this._tbSpreizung.Location = new System.Drawing.Point(170, 239);
            this._tbSpreizung.Name = "_tbSpreizung";
            this._tbSpreizung.Width = 70;
            this._tbSpreizung.TextChanged += new System.EventHandler(this.eingabe_TextChanged);
            //
            // _lblSpreizungHinweis
            //
            this._lblSpreizungHinweis.AutoSize = true;
            this._lblSpreizungHinweis.ForeColor = System.Drawing.SystemColors.GrayText;
            this._lblSpreizungHinweis.Location = new System.Drawing.Point(252, 242);
            this._lblSpreizungHinweis.MaximumSize = new System.Drawing.Size(436, 0);
            this._lblSpreizungHinweis.Name = "_lblSpreizungHinweis";
            this._lblSpreizungHinweis.Text = "(Quelleintritt minus Quellaustritt; Warnung, wenn Quelltemperatur − Spreizung daue" +
    "rhaft unter 0 °C liegt)";
            //
            // _lblKennwerte
            //
            this._lblKennwerte.AutoSize = false;
            this._lblKennwerte.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._lblKennwerte.Location = new System.Drawing.Point(14, 196);
            this._lblKennwerte.Name = "_lblKennwerte";
            this._lblKennwerte.Size = new System.Drawing.Size(650, 20);
            //
            // _gbVorschau
            //
            this._gbVorschau.Controls.Add(this._lblKennwerte);
            this._gbVorschau.Location = new System.Drawing.Point(12, 280);
            this._gbVorschau.Name = "_gbVorschau";
            this._gbVorschau.Size = new System.Drawing.Size(676, 230);
            this._gbVorschau.Text = "Vorschau: Jahresgang der Quelltemperatur";
            //
            // _lblPruefung
            //
            this._lblPruefung.AutoSize = false;
            this._lblPruefung.Font = new System.Drawing.Font(System.Drawing.FontFamily.GenericMonospace, 8.25F);
            this._lblPruefung.Location = new System.Drawing.Point(14, 22);
            this._lblPruefung.Name = "_lblPruefung";
            this._lblPruefung.Size = new System.Drawing.Size(650, 100);
            //
            // _lblAenderung
            //
            this._lblAenderung.AutoSize = false;
            this._lblAenderung.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(96)))), ((int)(((byte)(0)))));
            this._lblAenderung.Location = new System.Drawing.Point(14, 128);
            this._lblAenderung.Name = "_lblAenderung";
            this._lblAenderung.Size = new System.Drawing.Size(500, 48);
            //
            // _btnSimulation
            //
            this._btnSimulation.Location = new System.Drawing.Point(528, 126);
            this._btnSimulation.Name = "_btnSimulation";
            this._btnSimulation.Size = new System.Drawing.Size(134, 30);
            this._btnSimulation.Text = "Simulation";
            this._btnSimulation.Click += new System.EventHandler(this.btnSimulation_Click);
            //
            // _gbPruefung
            //
            this._gbPruefung.Controls.Add(this._lblPruefung);
            this._gbPruefung.Controls.Add(this._lblAenderung);
            this._gbPruefung.Controls.Add(this._btnSimulation);
            this._gbPruefung.Location = new System.Drawing.Point(12, 518);
            this._gbPruefung.Name = "_gbPruefung";
            this._gbPruefung.Size = new System.Drawing.Size(676, 182);
            this._gbPruefung.Text = "Auslegungsprüfung nach VDI 4640 Blatt 2 (nach der Simulation)";
            //
            // _btnOk
            //
            this._btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnOk.Location = new System.Drawing.Point(458, 708);
            this._btnOk.Name = "_btnOk";
            this._btnOk.Size = new System.Drawing.Size(110, 30);
            this._btnOk.Text = "OK";
            this._btnOk.Click += new System.EventHandler(this.btnOk_Click);
            //
            // _btnAbbruch
            //
            this._btnAbbruch.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnAbbruch.Location = new System.Drawing.Point(578, 708);
            this._btnAbbruch.Name = "_btnAbbruch";
            this._btnAbbruch.Size = new System.Drawing.Size(110, 30);
            this._btnAbbruch.Text = "Abbrechen";
            //
            // Form_QuelleErdreich
            //
            this.AcceptButton = this._btnOk;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this._btnAbbruch;
            this.ClientSize = new System.Drawing.Size(700, 748);
            this.Controls.Add(this._gbSystem);
            this.Controls.Add(this._lblBodentyp);
            this.Controls.Add(this._cbBoden);
            this.Controls.Add(this._lblBodentypHinweis);
            this.Controls.Add(this._lblBoden);
            this.Controls.Add(this._lblKlimazone);
            this.Controls.Add(this._cbZone);
            this.Controls.Add(this._lblKlimazoneHinweis);
            this.Controls.Add(this._lblSpreizung);
            this.Controls.Add(this._tbSpreizung);
            this.Controls.Add(this._lblSpreizungHinweis);
            this.Controls.Add(this._gbVorschau);
            this.Controls.Add(this._gbPruefung);
            this.Controls.Add(this._btnOk);
            this.Controls.Add(this._btnAbbruch);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_QuelleErdreich";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Wärmequelle Erdreich";
            this._gbSystem.ResumeLayout(false);
            this._gbSystem.PerformLayout();
            this._gbVorschau.ResumeLayout(false);
            this._gbPruefung.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox _gbSystem;
        private System.Windows.Forms.RadioButton _rbKollektor;
        private System.Windows.Forms.Label _lblVerlegetiefe;
        private System.Windows.Forms.TextBox _tbTiefe;
        private System.Windows.Forms.Label _lblFlaeche;
        private System.Windows.Forms.TextBox _tbFlaeche;
        private System.Windows.Forms.RadioButton _rbSonde;
        private System.Windows.Forms.Label _lblLaengeSonde;
        private System.Windows.Forms.TextBox _tbLaenge;
        private System.Windows.Forms.Label _lblAnzahlSonden;
        private System.Windows.Forms.TextBox _tbAnzahl;
        private System.Windows.Forms.Label _lblBodentyp;
        private System.Windows.Forms.ComboBox _cbBoden;
        private System.Windows.Forms.Label _lblBodentypHinweis;
        private System.Windows.Forms.Label _lblBoden;
        private System.Windows.Forms.Label _lblKlimazone;
        private System.Windows.Forms.ComboBox _cbZone;
        private System.Windows.Forms.Label _lblKlimazoneHinweis;
        private System.Windows.Forms.Label _lblSpreizung;
        private System.Windows.Forms.TextBox _tbSpreizung;
        private System.Windows.Forms.Label _lblSpreizungHinweis;
        private System.Windows.Forms.GroupBox _gbVorschau;
        private System.Windows.Forms.Label _lblKennwerte;
        private System.Windows.Forms.GroupBox _gbPruefung;
        private System.Windows.Forms.Label _lblPruefung;
        private System.Windows.Forms.Label _lblAenderung;
        private System.Windows.Forms.Button _btnSimulation;
        private System.Windows.Forms.Button _btnOk;
        private System.Windows.Forms.Button _btnAbbruch;
    }
}
