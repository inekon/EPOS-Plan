using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Aufschlagsblock und Vergütungssätze des Strom-Energieträgers
    /// (Fachkonzept Stromspeicher 4.2/4.3, Arbeitspaket AP4).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum ein eigenes Steuerelement statt weiterer Felder in
    /// <c>ucFuelSettings</c>.</b> <c>ucFuelSettings</c> gilt für JEDEN Energieträger;
    /// Netzentgelt, Umlagen und Stromsteuer gibt es nur beim Strom. Als eingebetteter
    /// Block bleibt der Bestandsdialog unangetastet — <c>ucFuelSettings</c> hängt ihn
    /// nur an, wenn <c>pricing_model = ELECTRICITY</c> ist, und wächst dafür um seine
    /// Höhe. Zudem bleibt <c>ucFuelSettings.Designer.cs</c> unberührt (CLAUDE.md:
    /// Designer-Dateien nicht von Hand editieren).
    /// </para>
    /// <para>
    /// <b>Oberfläche im Designer, ohne eigene <c>.resx</c>.</b> Der Aufbau steht in
    /// <c>ucStromAufschlaege.Designer.cs</c>; dort tragen alle sichtbaren Texte nur
    /// Platzhalter (den Feldnamen). Die echten Texte kommen aus
    /// <c>MyResource.Resource.PREIS_*</c> und <c>DbWerte</c> und werden in
    /// <see cref="TexteSetzen"/> gesetzt — zweisprachig wie bisher.
    /// </para>
    /// <para>
    /// <b>Kulturregel:</b> Eingabe und Anzeige über <c>Program.ZahlParsen</c> /
    /// <c>CurrentCulture</c> (der deutsche Anwender tippt „6,44"); in die Datenbank geht
    /// ausschließlich der <c>double</c>, nie eine Zeichenkette.
    /// </para>
    /// </remarks>
    public partial class ucStromAufschlaege : UserControl
    {
        /// <summary>
        /// Breite des Blocks — passend zu <c>panel1</c>/<c>dgvHistory</c> in ucFuelSettings.
        /// </summary>
        /// <remarks>
        /// <b>Muss mit <c>this.Size</c> im Designer übereinstimmen.</b> <c>ucFuelSettings</c>
        /// liest <see cref="HOEHE"/>, um die eigene Höhe zu vergrößern; die Designer-Größe
        /// kann keine Konstante referenzieren und steht deshalb dort als Zahl (548, 338).
        /// Wird eine der beiden Stellen geändert, muss die andere mitgeführt werden.
        /// </remarks>
        public const int BREITE = 548;

        /// <summary>Gesamthöhe des Blocks. Siehe Hinweis bei <see cref="BREITE"/>.</summary>
        public const int HOEHE = 338;

        // Das Raster, aus dem die Festkoordinaten im Designer gerechnet sind. Es stand
        // vor der Umstellung als privater Konstantensatz hier; jetzt ist es reine
        // Herleitung — die Wahrheit steht in ucStromAufschlaege.Designer.cs, und als
        // Konstante hier hätte es nur den Anschein erweckt, noch etwas zu bewegen.
        //   Spalten:  Schalter x = 14, Wertfeld x = 250, Einheit x = 350
        //   Zeilen:   Wertfeld y = 48 + i * 27 (i = 0..4, Zeilenhöhe 27),
        //             Schalter 2 px, Einheit 3 px tiefer als das Wertfeld
        //   Breiten:  Schalter 250 - 14 - 8 = 228, Wertfeld 92, Summen-/Restzeile
        //             548 - 2 * 14 = 520

        private readonly StromAufschlagModel _modell;

        /// <summary>
        /// Die fünf Wertfelder in der Reihenfolge Netzentgelt, Umlagen, Stromsteuer,
        /// Konzession, Vertrieb. Die Indexreihenfolge ist Vertrag: <see cref="ZeigeModell"/>,
        /// <see cref="InsModell"/> und die Stromsteuer-Schnellwahl (Index 2) hängen daran.
        /// Befüllt aus den Designer-Feldern im Konstruktor.
        /// </summary>
        private readonly TextBox[] _felder;

        /// <summary>Die zugehörigen Schalter, gleiche Indexreihenfolge wie <see cref="_felder"/>.</summary>
        private readonly CheckBox[] _schalter;

        /// <summary>Sperrt das Zurückschreiben, solange die Felder programmatisch gefüllt werden.</summary>
        private bool _laden;

        /// <summary>
        /// Erzeugt den Block für eine (Projekt, Energieträger)-Zeile und liest ihren
        /// Stand aus der Datenbank.
        /// </summary>
        public ucStromAufschlaege(int idProjekt, int idEnergietraeger)
        {
            // Die Datenbankarbeit läuft wie bisher VOR dem Aufbau der Oberfläche: Wirft
            // sie, ist kein einziges Steuerelement erzeugt, und ucFuelSettings fängt den
            // Fehler ab, ohne eine halb aufgebaute Maske stehen zu lassen.
            StromAufschlagCtrl.StelleSpaltenSicher();
            _modell = new StromAufschlagCtrl().Read(idProjekt, idEnergietraeger);

            // Der Designer setzt AutoScaleMode bewusst NICHT. Vor der Umstellung tat es
            // der handgebaute Aufbau ebenso wenig; damit bleibt es beim Klassenvorgabewert
            // AutoScaleMode.Inherit, das Steuerelement übernimmt also die Regel seines
            // Wirts (ucFuelSettings: AutoScaleMode.None). Das ist genau das bisherige
            // Verhalten — und passend dazu, dass die Anwendung DpiUnaware läuft
            // (app.manifest, Program.SetHighDpiMode).
            InitializeComponent();

            // Die Schleife von früher ist im Designer zu 15 benannten Feldern aufgelöst;
            // die Logik arbeitet unverändert über die Arrays. Reihenfolge NICHT ändern.
            _felder = new[] { _tbNetzentgelt, _tbUmlagen, _tbStromsteuer, _tbKonzession, _tbVertrieb };
            _schalter = new[] { _chkNetzentgelt, _chkUmlagen, _chkStromsteuer, _chkKonzession, _chkVertrieb };

            TexteSetzen();
            ZeigeModell();
        }

        /// <summary>Der aktuelle Stand — nach <see cref="Uebernehmen"/> der gespeicherte.</summary>
        public StromAufschlagModel Modell
        {
            get { return _modell; }
        }

        // ==================================================================
        // Texte
        // ==================================================================

        /// <summary>
        /// Setzt alle sichtbaren Texte aus <c>MyResource</c> und <c>DbWerte</c>. Läuft
        /// direkt nach <c>InitializeComponent()</c> und ersetzt die dortigen Platzhalter.
        /// </summary>
        private void TexteSetzen()
        {
            _gbAufschlag.Text = MyResource.Resource.PREIS_GRUPPE_AUFSCHLAG;

            // --- Modusumschalter (Fachkonzept 4.2) ---
            _rbAufgeschluesselt.Text = MyResource.Resource.PREIS_MODUS_AUFGESCHLUESSELT;
            _rbGesamtwert.Text = MyResource.Resource.PREIS_MODUS_GESAMTWERT;

            // --- Die fünf Komponenten ---
            _chkNetzentgelt.Text = MyResource.Resource.PREIS_KOMP_NETZENTGELT;
            _chkUmlagen.Text = MyResource.Resource.PREIS_KOMP_UMLAGEN;
            _chkStromsteuer.Text = MyResource.Resource.PREIS_KOMP_STROMSTEUER;
            _chkKonzession.Text = MyResource.Resource.PREIS_KOMP_KONZESSION;
            _chkVertrieb.Text = MyResource.Resource.PREIS_KOMP_VERTRIEB;

            _lblEinheitNetzentgelt.Text = DbWerte.PREISREIHE_EINHEIT_CT_KWH;
            _lblEinheitUmlagen.Text = DbWerte.PREISREIHE_EINHEIT_CT_KWH;
            _lblEinheitStromsteuer.Text = DbWerte.PREISREIHE_EINHEIT_CT_KWH;
            _lblEinheitKonzession.Text = DbWerte.PREISREIHE_EINHEIT_CT_KWH;
            _lblEinheitVertrieb.Text = DbWerte.PREISREIHE_EINHEIT_CT_KWH;

            // --- Stromsteuer-Schnellwahl (Fachkonzept 4.2) ---
            _btnStromsteuerRegelfall.Text = MyResource.Resource.PREIS_STROMSTEUER_REGELFALL;
            _btnStromsteuerReduziert.Text = MyResource.Resource.PREIS_STROMSTEUER_REDUZIERT;

            // --- Override-Gesamtwert ---
            _lblGesamtaufschlag.Text = MyResource.Resource.PREIS_LABEL_GESAMTAUFSCHLAG;
            _lblEinheitOverride.Text = DbWerte.PREISREIHE_EINHEIT_CT_KWH;

            // --- Vergütung (Fachkonzept 4.3) ---
            _gbVerguetung.Text = MyResource.Resource.PREIS_GRUPPE_VERGUETUNG;
            _lblVerguetungPv.Text = MyResource.Resource.PREIS_LABEL_VERGUETUNG_PV;
            _lblEinheitVerguetungPv.Text = DbWerte.PREISREIHE_EINHEIT_CT_KWH;
            _lblVerguetungBhkw.Text = MyResource.Resource.PREIS_LABEL_VERGUETUNG_BHKW;
            _lblEinheitVerguetungBhkw.Text = DbWerte.PREISREIHE_EINHEIT_CT_KWH;
        }

        // ==================================================================
        // Ereignisse
        // ==================================================================

        private void rbAufgeschluesselt_CheckedChanged(object sender, EventArgs e)
        {
            ModusGewechselt();
        }

        /// <summary>
        /// Gemeinsamer Schalter-Handler der fünf Komponentenzeilen — er arbeitet
        /// ausschließlich über das Modell, nicht über <c>sender</c>.
        /// </summary>
        private void KomponenteSchalter_CheckedChanged(object sender, EventArgs e)
        {
            SummeAktualisieren();
        }

        /// <summary>
        /// Gemeinsamer Handler der sechs Aufschlagsfelder (fünf Komponenten und der
        /// Override-Gesamtwert): einfärben und die Live-Summe nachziehen.
        /// </summary>
        private void Zahlenfeld_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
            SummeAktualisieren();
        }

        /// <summary>
        /// Die beiden Vergütungsfelder gehen NICHT in die Aufschlagssumme ein — hier
        /// wird nur eingefärbt.
        /// </summary>
        private void Verguetungsfeld_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void btnStromsteuerRegelfall_Click(object sender, EventArgs e)
        {
            StromsteuerUebernehmen(StromAufschlagModel.STROMSTEUER_REGELFALL);
        }

        private void btnStromsteuerReduziert_Click(object sender, EventArgs e)
        {
            StromsteuerUebernehmen(StromAufschlagModel.STROMSTEUER_REDUZIERT);
        }

        /// <summary>
        /// Trägt einen Schnellwahlwert in die Stromsteuerzeile (Index 2) ein und schaltet
        /// sie aktiv. Die beiden Knöpfe unterscheiden sich nur im Wert.
        /// </summary>
        private void StromsteuerUebernehmen(double wert)
        {
            _felder[2].Text = wert.ToString("0.###", CultureInfo.CurrentCulture);
            _schalter[2].Checked = true;
        }

        // ==================================================================
        // Modell <-> Oberfläche
        // ==================================================================

        private void ZeigeModell()
        {
            _laden = true;
            try
            {
                _felder[0].Text = Anzeige(_modell.Netzentgelt);
                _felder[1].Text = Anzeige(_modell.Umlagen);
                _felder[2].Text = Anzeige(_modell.Stromsteuer);
                _felder[3].Text = Anzeige(_modell.Konzession);
                _felder[4].Text = Anzeige(_modell.Vertrieb);

                _schalter[0].Checked = _modell.Netzentgelt_Aktiv;
                _schalter[1].Checked = _modell.Umlagen_Aktiv;
                _schalter[2].Checked = _modell.Stromsteuer_Aktiv;
                _schalter[3].Checked = _modell.Konzession_Aktiv;
                _schalter[4].Checked = _modell.Vertrieb_Aktiv;

                _tbOverride.Text = Anzeige(_modell.Override);
                _tbVerguetungPv.Text = Anzeige(_modell.Verguetung_PV);
                _tbVerguetungBhkw.Text = Anzeige(_modell.Verguetung_BHKW);

                bool gesamtwert = _modell.Modus == DbWerte.SP_AUFSCHLAG_MODUS_GESAMTWERT;
                _rbGesamtwert.Checked = gesamtwert;
                _rbAufgeschluesselt.Checked = !gesamtwert;
            }
            finally
            {
                _laden = false;
            }

            ModusGewechselt();
        }

        /// <summary>
        /// Liest die Felder in das Modell zurück. Unlesbare Felder behalten den
        /// bisherigen Wert — die Rückmeldung gibt die Einfärbung
        /// (<c>Program.ZahlFaerben</c>), nicht eine modale Meldung.
        /// </summary>
        public void InsModell()
        {
            _modell.Netzentgelt = Zahl(_felder[0], _modell.Netzentgelt);
            _modell.Umlagen = Zahl(_felder[1], _modell.Umlagen);
            _modell.Stromsteuer = Zahl(_felder[2], _modell.Stromsteuer);
            _modell.Konzession = Zahl(_felder[3], _modell.Konzession);
            _modell.Vertrieb = Zahl(_felder[4], _modell.Vertrieb);

            _modell.Netzentgelt_Aktiv = _schalter[0].Checked;
            _modell.Umlagen_Aktiv = _schalter[1].Checked;
            _modell.Stromsteuer_Aktiv = _schalter[2].Checked;
            _modell.Konzession_Aktiv = _schalter[3].Checked;
            _modell.Vertrieb_Aktiv = _schalter[4].Checked;

            _modell.Override = Zahl(_tbOverride, _modell.Override);
            _modell.Verguetung_PV = Zahl(_tbVerguetungPv, _modell.Verguetung_PV);
            _modell.Verguetung_BHKW = Zahl(_tbVerguetungBhkw, _modell.Verguetung_BHKW);

            _modell.Modus = _rbGesamtwert.Checked
                ? DbWerte.SP_AUFSCHLAG_MODUS_GESAMTWERT
                : DbWerte.SP_AUFSCHLAG_MODUS_AUFGESCHLUESSELT;
        }

        /// <summary>
        /// Übernimmt die Eingaben und schreibt sie zurück. Rückgabe false, wenn es
        /// keine Zeile in <c>energy_project_settings</c> gibt — dann ist der
        /// Energieträger dem Projekt nicht zugeordnet.
        /// </summary>
        public bool Uebernehmen()
        {
            InsModell();
            return new StromAufschlagCtrl().Update(_modell);
        }

        // ==================================================================
        // Live-Rechnung (die Formeln stehen in der Engine)
        // ==================================================================

        private void ModusGewechselt()
        {
            bool aufgeschluesselt = _rbAufgeschluesselt.Checked;

            // Im Override-Modus bleiben die Komponenten SICHTBAR und lesbar
            // (Fachkonzept 4.2: "die Komponentenliste bleibt sichtbar und informativ"),
            // aber sie steuern den Rechenweg nicht mehr.
            for (int i = 0; i < 5; i++)
            {
                _felder[i].ReadOnly = !aufgeschluesselt;
                _felder[i].BackColor = aufgeschluesselt ? SystemColors.Window : SystemColors.Control;
                _schalter[i].Enabled = aufgeschluesselt;
            }

            _tbOverride.Enabled = !aufgeschluesselt;
            SummeAktualisieren();
        }

        private void SummeAktualisieren()
        {
            if (_laden) return;

            InsModell();
            SpeicherEngine.Aufschlagssatz satz = StromAufschlagCtrl.AlsAufschlagssatz(_modell);

            _lblSumme.Text = string.Format(MyResource.Resource.PREIS_SUMME_AKTIV,
                                           Anzeige(satz.SummeAktivCtKwh),
                                           Anzeige(satz.WirksamCtKwh));

            _lblRest.Text = _rbGesamtwert.Checked
                ? string.Format(MyResource.Resource.PREIS_REST_NICHT_AUFGESCHLUESSELT,
                                Anzeige(satz.NichtAufgeschluesselterRestCtKwh))
                : MyResource.Resource.PREIS_REST_HINWEIS_MODUS;

            _lblRest.ForeColor = satz.NichtAufgeschluesselterRestCtKwh < 0.0
                ? Color.Firebrick
                : Color.FromArgb(100, 100, 100);
        }

        private static double Zahl(TextBox feld, double vorgabe)
        {
            double w;
            return Program.ZahlParsen(feld.Text, out w) ? w : vorgabe;
        }

        private static string Anzeige(double wert)
        {
            return wert.ToString("0.###", CultureInfo.CurrentCulture);
        }
    }
}
