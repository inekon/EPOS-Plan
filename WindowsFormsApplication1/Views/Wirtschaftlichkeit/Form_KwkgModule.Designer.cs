namespace WindowsFormsApplication1
{
    partial class Form_KwkgModule
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

        // ---------------------------------------------------------------------------
        // Design-Politur 21.08.2026 — Geometrie
        //
        // Anlass: Im Designer standen bis hierher die Feldnamen als Platzhalter, die
        // echten Texte setzt Form_KwkgModule.TexteSetzen() erst zur Laufzeit — die
        // Entwurfsansicht war damit kein Abbild der Maske. Die Platzhalter sind jetzt
        // durch die ECHTEN deutschen Texte aus TexteSetzen() ersetzt. TexteSetzen()
        // bleibt die einzige Quelle und überschreibt sie beim Start erneut; die Texte
        // hier dienen allein der Entwurfsansicht.
        //
        // Am echten Text nachgemessen (Segoe UI 9 pt, 96 dpi, TextRenderer.MeasureText):
        //   „Satz Einspeisung [ct/kWh] (0 = Projektsatz):“   235 px — breiteste Beschriftung
        //   „Vorschlag in die Satzfelder übernehmen“         215 px — Knopf war 200 px breit
        //   „kein Tatbestand (kein Eigenstromzuschlag)“      232 px — Vorgabeeintrag _cbFall
        //
        // Daraus die Änderungen:
        //   * Beschriftungsspalte 240 -> 250 px (235 px Text + Reserve), Eingabespalte
        //     x = 522 -> 536. Der Abstand Beschriftung/Eingabe betrug 4 px und liegt
        //     jetzt bei 8 px; die Beschriftungen hatten nur 5 px Rest und standen dicht
        //     vor dem Überlauf.
        //   * _cbArt/_cbFall 186 -> 256 px: Eine DropDownList zeigt vom Eintrag nur
        //     Breite minus rund 23 px (Aufklappknopf + Rand), 232 px Text brauchen also
        //     255 px. Der Vorgabeeintrag von _cbFall war bisher abgeschnitten.
        //   * _btnUebernehmen 200x26 -> 240x30 (Text passte nicht; Höhe wie die Fußknöpfe).
        //   * Fußknöpfe einheitlich 110x30, rechte Kante 792, Abstand 10 px, y 493 -> 495
        //     (10 px unter dem Hinweis). Die ClientSize-HÖHE bleibt dadurch bei 537.
        //   * ClientSize 720 -> 804 px breit: 12 Rand + 250 Beschriftung + 8 + 256
        //     Auswahlliste + 12 Rand. _lblKopf/_lblVorschlag (430 -> 514) und
        //     _lblHinweis (696 -> 780) ziehen auf die neue rechte Kante 792 nach.
        //   * _lblHinweis bleibt 51 px hoch: Der Hinweistext bricht zwischen 640 px und
        //     rund 900 px Breite gleichbleibend auf 3 Zeilen (3 x 15 px + 6 px Luft).
        //     Die Nachmessung in Form_KwkgModule.HinweisHoeheAnpassen() rechnet deshalb
        //     ebenfalls mit 780 px — beide Werte müssen zusammen bleiben.
        // ---------------------------------------------------------------------------

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this._lblListe = new System.Windows.Forms.Label();
            this._liste = new System.Windows.Forms.ListBox();
            this._lblStichtag = new System.Windows.Forms.Label();
            this._dtStichtag = new System.Windows.Forms.DateTimePicker();
            this._lblIbn = new System.Windows.Forms.Label();
            this._dtIbn = new System.Windows.Forms.DateTimePicker();
            this._lblArt = new System.Windows.Forms.Label();
            this._cbArt = new System.Windows.Forms.ComboBox();
            this._lblFall = new System.Windows.Forms.Label();
            this._cbFall = new System.Windows.Forms.ComboBox();
            this._lblEinsp = new System.Windows.Forms.Label();
            this._numEinsp = new System.Windows.Forms.NumericUpDown();
            this._lblEigen = new System.Windows.Forms.Label();
            this._numEigen = new System.Windows.Forms.NumericUpDown();
            this._lblKontingent = new System.Windows.Forms.Label();
            this._numKontingent = new System.Windows.Forms.NumericUpDown();
            this._lblDeckel = new System.Windows.Forms.Label();
            this._numDeckel = new System.Windows.Forms.NumericUpDown();
            this._lblKopf = new System.Windows.Forms.Label();
            this._lblVorschlag = new System.Windows.Forms.Label();
            this._btnUebernehmen = new System.Windows.Forms.Button();
            this._lblHinweis = new System.Windows.Forms.Label();
            this._btnOk = new System.Windows.Forms.Button();
            this._btnAbbrechen = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this._numEinsp)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numEigen)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numKontingent)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numDeckel)).BeginInit();
            this.SuspendLayout();
            //
            // _lblListe
            //
            this._lblListe.Location = new System.Drawing.Point(12, 10);
            this._lblListe.Name = "_lblListe";
            this._lblListe.Size = new System.Drawing.Size(250, 18);
            this._lblListe.Text = "BHKW-Anlagen der Vergleichsgruppe:";
            //
            // _liste
            //
            this._liste.IntegralHeight = false;
            this._liste.Location = new System.Drawing.Point(12, 30);
            this._liste.Name = "_liste";
            this._liste.Size = new System.Drawing.Size(250, 230);
            this._liste.SelectedIndexChanged += new System.EventHandler(this.Liste_Wechsel);
            //
            // _lblStichtag
            //
            this._lblStichtag.Location = new System.Drawing.Point(278, 33);
            this._lblStichtag.Name = "_lblStichtag";
            this._lblStichtag.Size = new System.Drawing.Size(250, 18);
            this._lblStichtag.Text = "Stichtag (Bestellung/Genehmigung):";
            //
            // _dtStichtag
            //
            this._dtStichtag.Checked = false;
            this._dtStichtag.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this._dtStichtag.Location = new System.Drawing.Point(536, 30);
            this._dtStichtag.Name = "_dtStichtag";
            this._dtStichtag.ShowCheckBox = true;
            this._dtStichtag.Size = new System.Drawing.Size(160, 23);
            //
            // _lblIbn
            //
            this._lblIbn.Location = new System.Drawing.Point(278, 63);
            this._lblIbn.Name = "_lblIbn";
            this._lblIbn.Size = new System.Drawing.Size(250, 18);
            this._lblIbn.Text = "Inbetriebnahme:";
            //
            // _dtIbn
            //
            this._dtIbn.Checked = false;
            this._dtIbn.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this._dtIbn.Location = new System.Drawing.Point(536, 60);
            this._dtIbn.Name = "_dtIbn";
            this._dtIbn.ShowCheckBox = true;
            this._dtIbn.Size = new System.Drawing.Size(160, 23);
            //
            // _lblArt
            //
            this._lblArt.Location = new System.Drawing.Point(278, 93);
            this._lblArt.Name = "_lblArt";
            this._lblArt.Size = new System.Drawing.Size(250, 18);
            this._lblArt.Text = "Anlagenart:";
            //
            // _cbArt
            //
            this._cbArt.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbArt.Location = new System.Drawing.Point(536, 90);
            this._cbArt.Name = "_cbArt";
            this._cbArt.Size = new System.Drawing.Size(256, 23);
            this._cbArt.SelectedIndexChanged += new System.EventHandler(this.Feld_Wechsel);
            //
            // _lblFall
            //
            this._lblFall.Location = new System.Drawing.Point(278, 123);
            this._lblFall.Name = "_lblFall";
            this._lblFall.Size = new System.Drawing.Size(250, 18);
            this._lblFall.Text = "Eigenstrom nach § 6 Abs. 3:";
            //
            // _cbFall
            //
            this._cbFall.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbFall.Location = new System.Drawing.Point(536, 120);
            this._cbFall.Name = "_cbFall";
            this._cbFall.Size = new System.Drawing.Size(256, 23);
            this._cbFall.SelectedIndexChanged += new System.EventHandler(this.Feld_Wechsel);
            //
            // _lblEinsp
            //
            this._lblEinsp.Location = new System.Drawing.Point(278, 153);
            this._lblEinsp.Name = "_lblEinsp";
            this._lblEinsp.Size = new System.Drawing.Size(250, 18);
            this._lblEinsp.Text = "Satz Einspeisung [ct/kWh] (0 = Projektsatz):";
            //
            // _numEinsp
            //
            this._numEinsp.DecimalPlaces = 2;
            this._numEinsp.Increment = new decimal(new int[] { 1, 0, 0, 65536});
            this._numEinsp.Location = new System.Drawing.Point(536, 150);
            this._numEinsp.Maximum = new decimal(new int[] { 30, 0, 0, 0});
            this._numEinsp.Minimum = new decimal(new int[] { 0, 0, 0, 0});
            this._numEinsp.Name = "_numEinsp";
            this._numEinsp.Size = new System.Drawing.Size(160, 23);
            this._numEinsp.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            //
            // _lblEigen
            //
            this._lblEigen.Location = new System.Drawing.Point(278, 182);
            this._lblEigen.Name = "_lblEigen";
            this._lblEigen.Size = new System.Drawing.Size(250, 18);
            this._lblEigen.Text = "Satz Eigenstrom [ct/kWh] (0 = Projektsatz):";
            //
            // _numEigen
            //
            this._numEigen.DecimalPlaces = 2;
            this._numEigen.Increment = new decimal(new int[] { 1, 0, 0, 65536});
            this._numEigen.Location = new System.Drawing.Point(536, 179);
            this._numEigen.Maximum = new decimal(new int[] { 30, 0, 0, 0});
            this._numEigen.Minimum = new decimal(new int[] { 0, 0, 0, 0});
            this._numEigen.Name = "_numEigen";
            this._numEigen.Size = new System.Drawing.Size(160, 23);
            this._numEigen.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            //
            // _lblKontingent
            //
            this._lblKontingent.Location = new System.Drawing.Point(278, 211);
            this._lblKontingent.Name = "_lblKontingent";
            this._lblKontingent.Size = new System.Drawing.Size(250, 18);
            this._lblKontingent.Text = "Vbh-Kontingent [h] (0 = Projektwert):";
            //
            // _numKontingent
            //
            this._numKontingent.DecimalPlaces = 0;
            this._numKontingent.Increment = new decimal(new int[] { 1000, 0, 0, 0});
            this._numKontingent.Location = new System.Drawing.Point(536, 208);
            this._numKontingent.Maximum = new decimal(new int[] { 200000, 0, 0, 0});
            this._numKontingent.Minimum = new decimal(new int[] { 0, 0, 0, 0});
            this._numKontingent.Name = "_numKontingent";
            this._numKontingent.Size = new System.Drawing.Size(160, 23);
            this._numKontingent.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            //
            // _lblDeckel
            //
            this._lblDeckel.Location = new System.Drawing.Point(278, 240);
            this._lblDeckel.Name = "_lblDeckel";
            this._lblDeckel.Size = new System.Drawing.Size(250, 18);
            this._lblDeckel.Text = "Vbh-Jahresdeckel [h/a] (0 = Staffel):";
            //
            // _numDeckel
            //
            this._numDeckel.DecimalPlaces = 0;
            this._numDeckel.Increment = new decimal(new int[] { 100, 0, 0, 0});
            this._numDeckel.Location = new System.Drawing.Point(536, 237);
            this._numDeckel.Maximum = new decimal(new int[] { 8760, 0, 0, 0});
            this._numDeckel.Minimum = new decimal(new int[] { 0, 0, 0, 0});
            this._numDeckel.Name = "_numDeckel";
            this._numDeckel.Size = new System.Drawing.Size(160, 23);
            this._numDeckel.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            //
            // _lblKopf
            //
            this._lblKopf.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._lblKopf.Location = new System.Drawing.Point(278, 272);
            this._lblKopf.Name = "_lblKopf";
            this._lblKopf.Size = new System.Drawing.Size(514, 18);
            this._lblKopf.Text = "Katalogvorschlag (§ 7 KWKG 2025)";
            //
            // _lblVorschlag
            //
            // Ohne Text: Der Inhalt entsteht erst zur Laufzeit in
            // Form_KwkgModule.VorschlagZeigen() (zwei Absätze, je Satz und Herleitung).
            // 514 x 96 px fassen 6 Zeilen Segoe UI 9 pt.
            //
            this._lblVorschlag.ForeColor = System.Drawing.Color.DimGray;
            this._lblVorschlag.Location = new System.Drawing.Point(278, 292);
            this._lblVorschlag.Name = "_lblVorschlag";
            this._lblVorschlag.Size = new System.Drawing.Size(514, 96);
            //
            // _btnUebernehmen
            //
            this._btnUebernehmen.Location = new System.Drawing.Point(278, 394);
            this._btnUebernehmen.Name = "_btnUebernehmen";
            this._btnUebernehmen.Size = new System.Drawing.Size(240, 30);
            this._btnUebernehmen.Text = "Vorschlag in die Satzfelder übernehmen";
            this._btnUebernehmen.Click += new System.EventHandler(this.Uebernehmen_Klick);
            //
            // _lblHinweis
            //
            // Die Höhe ist der auf diesem Stand gemessene Umbruchwert (3 Zeilen Segoe UI
            // 9 pt à 15 px + 6 px Luft). Sie wird im Konstruktor-Nachlauf am echten Text
            // neu gemessen (Form_KwkgModule.HinweisHoeheAnpassen) — die dortige Breite
            // muss mit der hier eingetragenen übereinstimmen (Design-Politur 21.08.2026:
            // 696 -> 780 px, Zeilenzahl und damit die Höhe bleiben unverändert).
            //
            this._lblHinweis.ForeColor = System.Drawing.Color.DimGray;
            this._lblHinweis.Location = new System.Drawing.Point(12, 434);
            this._lblHinweis.Name = "_lblHinweis";
            this._lblHinweis.Size = new System.Drawing.Size(780, 51);
            this._lblHinweis.Text = "Leere Felder heißen „kein eigener Wert“ — dann gilt die Projektvorgabe aus dem Parameterdialog. Der Vorschlag wird NICHT automatisch angesetzt: Erst die Schaltfläche schreibt ihn in die Satzfelder, und erst dann rechnet diese Anlage mit einem eigenen Satz. Vollbenutzungsstunden, Jahresdeckel und Kontingent gelten nach § 8 KWKG je Anlage.";
            //
            // _btnOk
            //
            this._btnOk.Location = new System.Drawing.Point(562, 495);
            this._btnOk.Name = "_btnOk";
            this._btnOk.Size = new System.Drawing.Size(110, 30);
            this._btnOk.Text = "OK";
            this._btnOk.Click += new System.EventHandler(this.Speichern_Klick);
            //
            // _btnAbbrechen
            //
            this._btnAbbrechen.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnAbbrechen.Location = new System.Drawing.Point(682, 495);
            this._btnAbbrechen.Name = "_btnAbbrechen";
            this._btnAbbrechen.Size = new System.Drawing.Size(110, 30);
            this._btnAbbrechen.Text = "Abbrechen";
            //
            // Form_KwkgModule
            //
            this.AcceptButton = this._btnOk;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.AutoScroll = true;
            this.CancelButton = this._btnAbbrechen;
            this.ClientSize = new System.Drawing.Size(804, 537);
            this.Controls.Add(this._lblListe);
            this.Controls.Add(this._liste);
            this.Controls.Add(this._lblStichtag);
            this.Controls.Add(this._dtStichtag);
            this.Controls.Add(this._lblIbn);
            this.Controls.Add(this._dtIbn);
            this.Controls.Add(this._lblArt);
            this.Controls.Add(this._cbArt);
            this.Controls.Add(this._lblFall);
            this.Controls.Add(this._cbFall);
            this.Controls.Add(this._lblEinsp);
            this.Controls.Add(this._numEinsp);
            this.Controls.Add(this._lblEigen);
            this.Controls.Add(this._numEigen);
            this.Controls.Add(this._lblKontingent);
            this.Controls.Add(this._numKontingent);
            this.Controls.Add(this._lblDeckel);
            this.Controls.Add(this._numDeckel);
            this.Controls.Add(this._lblKopf);
            this.Controls.Add(this._lblVorschlag);
            this.Controls.Add(this._btnUebernehmen);
            this.Controls.Add(this._lblHinweis);
            this.Controls.Add(this._btnOk);
            this.Controls.Add(this._btnAbbrechen);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_KwkgModule";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "KWK-Zuschlag je BHKW-Modul";
            ((System.ComponentModel.ISupportInitialize)(this._numEinsp)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numEigen)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numKontingent)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numDeckel)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label _lblListe;
        private System.Windows.Forms.ListBox _liste;
        private System.Windows.Forms.Label _lblStichtag;
        private System.Windows.Forms.DateTimePicker _dtStichtag;
        private System.Windows.Forms.Label _lblIbn;
        private System.Windows.Forms.DateTimePicker _dtIbn;
        private System.Windows.Forms.Label _lblArt;
        private System.Windows.Forms.ComboBox _cbArt;
        private System.Windows.Forms.Label _lblFall;
        private System.Windows.Forms.ComboBox _cbFall;
        private System.Windows.Forms.Label _lblEinsp;
        private System.Windows.Forms.NumericUpDown _numEinsp;
        private System.Windows.Forms.Label _lblEigen;
        private System.Windows.Forms.NumericUpDown _numEigen;
        private System.Windows.Forms.Label _lblKontingent;
        private System.Windows.Forms.NumericUpDown _numKontingent;
        private System.Windows.Forms.Label _lblDeckel;
        private System.Windows.Forms.NumericUpDown _numDeckel;
        private System.Windows.Forms.Label _lblKopf;
        private System.Windows.Forms.Label _lblVorschlag;
        private System.Windows.Forms.Button _btnUebernehmen;
        private System.Windows.Forms.Label _lblHinweis;
        private System.Windows.Forms.Button _btnOk;
        private System.Windows.Forms.Button _btnAbbrechen;
    }
}
