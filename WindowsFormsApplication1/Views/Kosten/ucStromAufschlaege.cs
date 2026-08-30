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
    /// <para>
    /// <b>ETAPPE B4 (BW4) — die Schnellwahl liest den Katalog, nicht Konstanten.</b>
    /// Der Regelsatz kommt aus <see cref="GesetzKatalog"/>
    /// (<c>STROMST_REGELSATZ</c>, 20,50 €/MWh ab 2026 = 2,050 ct/kWh), gelesen mit dem
    /// Bilanzjahr des Projekts — dieselbe Jahresermittlung wie in
    /// <see cref="ucBrennstoffBestandteile"/>. Damit ist die von Befund A7 benannte
    /// doppelte Wahrheit des Stromsteuersatzes aufgelöst.
    /// <c>StromAufschlagModel.STROMSTEUER_REGELFALL/_REDUZIERT</c> bleiben als
    /// <b>Rückfallebene</b> stehen und greifen nur, wenn der Katalog für das Jahr nichts
    /// liefert; der Kurzhinweis des Knopfes nennt dann genau das.
    /// </para>
    /// <para>
    /// <b>Warum die Knöpfe hier — anders als bei
    /// <see cref="ucBrennstoffBestandteile"/> — nie gesperrt sind.</b> Dort gibt es zu
    /// einem fehlenden Katalogsatz keinen Ersatz, und ein bedienbarer Knopf ohne Zahl
    /// wäre eine Behauptung. Hier existiert die Konstante seit AP4 als abgestimmter
    /// Vorschlagswert und ist mit dem Katalogsatz wertgleich — ihn zu sperren würde eine
    /// Bedienung wegnehmen, die es vor B4 gab, und wäre keine Verbesserung.
    /// </para>
    /// <para>
    /// <b>Die Unternehmensart ist das führende Feld (BW4, Befund B3).</b> Der Dialog
    /// liest <c>Tab_ProjektWirtschaftlichkeit.Unternehmensart</c> und HEBT den dazu
    /// passenden Knopf HERVOR (fett) — produzierendes Gewerbe und Land-/Forstwirtschaft
    /// sind nach § 9b StromStG entlastungsberechtigt, alle übrigen nicht. Es wird
    /// ausdrücklich <b>nichts</b> eingetragen: Der Vorschlag wirkt erst auf Knopfdruck
    /// (Regel BF4 des Konzepts).
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

        // ---- B4: Bezug der Schnellwahl auf Katalog und Projekt ----

        /// <summary>Jahr, für das die Katalogsätze gelesen werden (Bilanzjahr des Projekts).</summary>
        private readonly int _katalogJahr;

        /// <summary>
        /// Unternehmensart des Projekts, Steuerwert aus <c>DbWerte.UNTERNEHMENSART_*</c>
        /// — das führende Feld der Energieintensität (BW4).
        /// </summary>
        private readonly string _unternehmensart;

        /// <summary>Gesetzeskatalog — nach dem Laden datenbankfrei.</summary>
        private readonly GesetzKatalog _gesetze = new GesetzKatalog();

        /// <summary>Trägt Herkunft und Empfehlung der beiden Schnellwahlknöpfe.</summary>
        private readonly ToolTip _tip = new ToolTip();

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

            _katalogJahr = KatalogjahrErmitteln(idProjekt, out _unternehmensart);

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
            SchnellwahlBeschriften();
            ZeigeModell();
        }

        /// <summary>Der aktuelle Stand — nach <see cref="Uebernehmen"/> der gespeicherte.</summary>
        /// <summary>Ä16: der aktuell wirksame Aufschlag [ct/kWh] (UI-Stand).</summary>
        public double WirksamCtKwh
        {
            get
            {
                InsModell();
                return StromAufschlagCtrl.AlsAufschlagssatz(_modell).WirksamCtKwh;
            }
        }

        /// <summary>Ä16: der wirksame Aufschlag hat sich geändert (Modus, Wert,
        /// Komponente) — für die Effektivpreis-Zeile des Trägerdialogs.</summary>
        public event EventHandler WirksamGeaendert;

        public StromAufschlagModel Modell
        {
            get { return _modell; }
        }

        // ==================================================================
        // Texte
        // ==================================================================

        /// <summary>MyResource mit deutschem Rückfall (Drei-Schichten-Regel) — dasselbe
        /// Muster wie <c>ucBrennstoffBestandteile.T</c>. Der Rückfall greift auf einer
        /// Ressourcendatei ohne die neuen B4-Einträge.</summary>
        private static string T(string schluessel, string rueckfall)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(s) ? rueckfall : s;
            }
            catch { return rueckfall; }
        }

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

            // --- Stromsteuer-Schnellwahl (Fachkonzept 4.2; Quelle seit B4 der Katalog) ---
            //
            // Die Beschriftung setzt SchnellwahlBeschriften(), nicht diese Methode: Sie
            // ist der JAHRESSATZ und keine feste Zeichenkette. Die bisherigen Einträge
            // MyResource.PREIS_STROMSTEUER_REGELFALL/_REDUZIERT trugen die blanken
            // Zahlen „2,05" und „0,05" — eine dritte Stelle derselben Wahrheit neben
            // Konstante und Katalog (Befund A7) und obendrein ein Zahlenliteral in der
            // Anzeigeschicht. Sie werden deshalb nicht mehr gelesen; entfernt werden sie
            // erst mit dem nächsten Ressourcendurchgang, damit dieser Schnitt keine
            // .resx anfasst.

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
            StromsteuerUebernehmen(SatzRegelfall(_gesetze, _katalogJahr));
        }

        private void btnStromsteuerReduziert_Click(object sender, EventArgs e)
        {
            StromsteuerUebernehmen(SatzReduziert(_gesetze, _katalogJahr));
        }

        /// <summary>
        /// Trägt einen Schnellwahlwert in die Stromsteuerzeile (Index 2) ein und schaltet
        /// sie aktiv. Die beiden Knöpfe unterscheiden sich nur in der Herkunft des Werts.
        /// </summary>
        /// <remarks>
        /// Ein Schnellwahlergebnis ohne Wert kann es hier nicht geben (die
        /// Rückfallebene liefert immer eine Zahl); die Prüfung steht trotzdem, weil sie
        /// die Zusicherung sichtbar macht, statt sie vorauszusetzen.
        /// </remarks>
        private void StromsteuerUebernehmen(Schnellwahl s)
        {
            if (s == null || !s.CtKwh.HasValue) return;

            _felder[2].Text = s.CtKwh.Value.ToString("0.###", CultureInfo.CurrentCulture);
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

            if (WirksamGeaendert != null) WirksamGeaendert(this, EventArgs.Empty);
        }

        // ==================================================================
        // Schnellwahl aus dem Gesetzeskatalog (Konzept BW4, Befund A7 — Etappe B4)
        // ==================================================================

        /// <summary>Ergebnis einer Schnellwahl-Auflösung: der Satz, seine Beschriftung
        /// und die Herkunft (Katalogzeile oder Rückfallebene).</summary>
        /// <remarks>
        /// Anders als bei <c>ucBrennstoffBestandteile.Schnellwahl</c> trägt
        /// <see cref="CtKwh"/> hier IMMER einen Wert — zu jedem der beiden Sätze gibt es
        /// eine Rückfallebene in <see cref="StromAufschlagModel"/>. Die Frage ist nicht
        /// „gibt es eine Zahl", sondern „woher kommt sie": das sagt
        /// <see cref="AusKatalog"/> und im Klartext <see cref="Herkunft"/>.
        /// </remarks>
        private sealed class Schnellwahl
        {
            /// <summary>Der Satz in ct/kWh.</summary>
            public double? CtKwh;

            /// <summary>Beschriftung des Knopfes — der blanke Jahressatz.</summary>
            public string Beschriftung = "";

            /// <summary>Herkunft im Klartext (Katalogzeile mit Jahr und Quelle, oder
            /// der Grund, aus dem die Rückfallebene greift) — geht in den Tooltip.</summary>
            public string Herkunft = "";

            /// <summary>true = aus <c>Tab_Gesetzesparameter</c>, false = Konstante.</summary>
            public bool AusKatalog;
        }

        /// <summary>
        /// Beschriftet beide Schnellwahlknöpfe mit dem Jahressatz und hebt den hervor,
        /// der zur Unternehmensart des Projekts passt.
        /// </summary>
        /// <remarks>
        /// Läuft einmal beim Aufbau: Bilanzjahr und Unternehmensart werden in
        /// <c>Form_WirtschaftlichkeitParameter</c> gepflegt, und dieser Block wird bei
        /// jedem Öffnen des Trägerdialogs neu erzeugt — eine Nachführung zur Laufzeit
        /// bräuchte eine Benachrichtigung zwischen zwei Fenstern, die es hier nicht gibt
        /// und für die es keinen Anlass gibt.
        /// </remarks>
        private void SchnellwahlBeschriften()
        {
            bool reduziertEmpfohlen = ReduzierterSatzEmpfohlen(_unternehmensart);

            Knopf(_btnStromsteuerRegelfall, SatzRegelfall(_gesetze, _katalogJahr),
                  T("PREIS_ST_ZWECK_REGELFALL",
                    "Stromsteuer im Regelfall (§ 3 StromStG)."),
                  !reduziertEmpfohlen);

            Knopf(_btnStromsteuerReduziert, SatzReduziert(_gesetze, _katalogJahr),
                  T("PREIS_ST_ZWECK_REDUZIERT",
                    "Stromsteuer energieintensiver Unternehmen — was nach der Entlastung " +
                    "nach § 9b StromStG im Preis verbleibt."),
                  reduziertEmpfohlen);
        }

        /// <summary>
        /// Setzt Beschriftung, Hervorhebung und Kurzhinweis eines Schnellwahlknopfes.
        /// </summary>
        /// <remarks>
        /// <b>Die Hervorhebung ist fett und nicht farbig.</b> Ein eigener Hinweistext
        /// bräuchte ein neues Steuerelement, und Designer-Dateien werden nicht von Hand
        /// gepflegt (CLAUDE.md); eine Einfärbung wiederum wäre neben der bereits
        /// belegten Bedeutung von <c>Firebrick</c> (negativer Rest) missverständlich.
        /// Der Text der Knöpfe ist eine kurze Zahl — fett bleibt er in den 62 px des
        /// Designers.
        /// </remarks>
        private void Knopf(Button b, Schnellwahl s, string zweck, bool empfohlen)
        {
            b.Text = s.Beschriftung;

            FontStyle stil = empfohlen ? FontStyle.Bold : FontStyle.Regular;
            if (b.Font.Style != stil) b.Font = new Font(b.Font, stil);

            string tip = zweck + Environment.NewLine + s.Herkunft;
            if (empfohlen)
                tip += Environment.NewLine + string.Format(
                    T("PREIS_ST_EMPFOHLEN",
                      "Vorschlag zur Unternehmensart „{0}“ dieses Projekts. " +
                      "Eingetragen wird der Satz erst mit einem Klick."),
                    UnternehmensartAnzeige(_unternehmensart));

            _tip.SetToolTip(b, tip);
        }

        /// <summary>
        /// Der Stromsteuer-Regelsatz des Bilanzjahres: Katalogzeile
        /// <c>STROMST_REGELSATZ</c>, ersatzweise
        /// <c>StromAufschlagModel.STROMSTEUER_REGELFALL</c>.
        /// </summary>
        private static Schnellwahl SatzRegelfall(GesetzKatalog gesetze, int jahr)
        {
            return Satz(gesetze, jahr, DbWerte.GESETZ_STROMST_REGELSATZ,
                        StromAufschlagModel.STROMSTEUER_REGELFALL);
        }

        /// <summary>
        /// Der reduzierte Stromsteuersatz energieintensiver Unternehmen.
        /// </summary>
        /// <remarks>
        /// <b>Im Auslieferungsstand liefert der Katalog hier nichts</b> — der Schlüssel
        /// <c>DbWerte.GESETZ_STROMST_REDUZIERT</c> ist bewusst nicht eingesät (L4: der
        /// Restsatz nach § 9b darf nicht als Differenz aus Regelsatz und
        /// Entlastungssatz geraten werden). Der Knopf trägt deshalb regelmäßig den
        /// Rückfallwert 0,050 ct/kWh und sagt das im Kurzhinweis. Pflegt jemand die
        /// Zeile über „Gesetzliche Parameter" nach, gilt ab dem nächsten Öffnen sie.
        /// </remarks>
        private static Schnellwahl SatzReduziert(GesetzKatalog gesetze, int jahr)
        {
            return Satz(gesetze, jahr, DbWerte.GESETZ_STROMST_REDUZIERT,
                        StromAufschlagModel.STROMSTEUER_REDUZIERT);
        }

        /// <summary>
        /// Löst einen Satz auf: Katalogzeile des Jahres → ct/kWh; scheitert eine Stufe,
        /// greift <paramref name="rueckfall"/> und die Herkunft nennt den Grund.
        /// </summary>
        private static Schnellwahl Satz(GesetzKatalog gesetze, int jahr, string schluessel,
                                        double rueckfall)
        {
            GesetzParameter p = null;
            try { if (gesetze != null) p = gesetze.WertMitHerkunft(schluessel, jahr); }
            catch { }

            if (p == null || !p.Wert.HasValue)
                return Rueckfall(rueckfall, string.Format(
                    T("PREIS_ST_GRUND_KEIN_JAHR",
                      "Der Katalog führt für „{0}“ keinen Satz im Jahr {1}."),
                    schluessel, jahr.ToString(CultureInfo.InvariantCulture)));

            string grund;
            double? ct = InCtKwh(p.Wert.Value, p.Einheit, out grund);
            if (!ct.HasValue) return Rueckfall(rueckfall, grund);

            return new Schnellwahl
            {
                CtKwh = ct,
                AusKatalog = true,
                Beschriftung = Anzeige(ct.Value),
                Herkunft = string.Format(
                    T("PREIS_ST_QUELLE", "Katalog: {0} {1} (ab {2}, {3})"),
                    p.Wert.Value.ToString("0.####", CultureInfo.CurrentCulture),
                    p.Einheit, p.JahrVon.ToString(CultureInfo.InvariantCulture),
                    Herkunftstext(p))
            };
        }

        /// <summary>Die Rückfallebene mit dem Grund, aus dem sie greift.</summary>
        private static Schnellwahl Rueckfall(double wert, string grund)
        {
            return new Schnellwahl
            {
                CtKwh = wert,
                AusKatalog = false,
                Beschriftung = Anzeige(wert),
                Herkunft = string.Format(
                    T("PREIS_ST_QUELLE_RUECKFALL",
                      "Rückfallebene: {0} ct/kWh aus dem Programm. {1} " +
                      "Nachpflegbar über „Gesetzliche Parameter“."),
                    Anzeige(wert), grund)
            };
        }

        /// <summary>
        /// Bringt einen Katalogsatz in ct/kWh. <c>null</c> heißt „nicht umrechenbar",
        /// und <paramref name="grund"/> sagt warum.
        /// </summary>
        /// <remarks>
        /// <b>Ohne Brennwertbrücke.</b> Beim Brennstoff bemisst sich ein Satz je MWh am
        /// Brennwert und muss mit <c>Hs/Hi</c> auf den heizwertbezogenen Arbeitspreis
        /// gebracht werden (<c>ucBrennstoffBestandteile.InCtKwh</c>). Für Strom gibt es
        /// diese Frage nicht — eine Kilowattstunde Strom ist eine Kilowattstunde,
        /// dieselbe Begründung wie in <c>KohaerenzPruefung.Fall4Strom</c>.
        /// </remarks>
        private static double? InCtKwh(double wert, string einheit, out string grund)
        {
            grund = "";
            string e = (einheit ?? "").Trim();

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_CT_KWH, StringComparison.OrdinalIgnoreCase))
                return wert;

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_MWH, StringComparison.OrdinalIgnoreCase))
                return wert / 10.0;

            grund = string.Format(
                T("PREIS_ST_GRUND_EINHEIT", "Die Katalogeinheit „{0}“ lässt sich nicht in ct/kWh umrechnen."),
                e);
            return null;
        }

        /// <summary>Kurzform von Quelle und Status einer Katalogzeile für den Tooltip.</summary>
        private static string Herkunftstext(GesetzParameter p)
        {
            string q = (p.Quelle ?? "").Trim();
            string st = (p.Status ?? "").Trim();
            if (q.Length == 0) return st;
            if (st.Length == 0) return q;
            return q + ", " + st;
        }

        // ==================================================================
        // Projektbezug: Bilanzjahr und Unternehmensart (BW4)
        // ==================================================================

        /// <summary>
        /// Das Jahr, für das die Katalogsätze gelesen werden: das Bilanzjahr des
        /// Projekts, ersatzweise <c>BilanzKonvention.BILANZJAHR_RUECKFALL</c>. Im
        /// Katalogkontext (Projekt 0) gilt das laufende Kalenderjahr. Nebenbei kommt die
        /// Unternehmensart mit — beides steht in derselben Zeile, und ein zweiter
        /// Ladevorgang wäre nur ein zweiter Weg zur selben Wahrheit.
        /// </summary>
        /// <remarks>
        /// Wortgleich zu <c>ucBrennstoffBestandteile.KatalogjahrErmitteln</c>: Beide
        /// Blöcke sitzen im selben Trägerdialog, und zwei verschiedene Bilanzjahre in
        /// einer Maske wären für den Anwender nicht erklärbar.
        /// </remarks>
        private static int KatalogjahrErmitteln(int idProjekt, out string unternehmensart)
        {
            unternehmensart = DbWerte.UNTERNEHMENSART_KEIN_PROD_GEWERBE;
            if (idProjekt <= 0) return DateTime.Now.Year;

            try
            {
                WirtschaftlichkeitParameter p = new WirtschaftlichkeitCtrl().LadeParameter(idProjekt);
                if (p != null)
                {
                    if (!string.IsNullOrEmpty(p.Unternehmensart)) unternehmensart = p.Unternehmensart;
                    if (p.BilanzJahr > 0) return p.BilanzJahr;
                }
            }
            catch { }

            return BilanzKonvention.BILANZJAHR_RUECKFALL;
        }

        /// <summary>
        /// Rechtfertigt die Unternehmensart den reduzierten Satz? Produzierendes
        /// Gewerbe (§ 2 Nr. 3 StromStG) und Land-/Forstwirtschaft sind nach § 9b
        /// StromStG entlastungsberechtigt, alle übrigen nicht — dieselbe Bedingung, die
        /// <c>SteuerGutschriftRechner.ProduzierendesGewerbe</c> für die Rechnung prüft,
        /// bis hin zum <c>StringComparison.Ordinal</c>. Es gibt keine zweite Regel, nur
        /// einen zweiten Leser.
        /// </summary>
        internal static bool ReduzierterSatzEmpfohlen(string unternehmensart)
        {
            return string.Equals(unternehmensart, DbWerte.UNTERNEHMENSART_PROD_GEWERBE,
                                 StringComparison.Ordinal)
                || string.Equals(unternehmensart, DbWerte.UNTERNEHMENSART_LAND_FORST,
                                 StringComparison.Ordinal);
        }

        /// <summary>
        /// Anzeigename der Unternehmensart — wortgleich zu den Auswahltexten in
        /// <c>Form_WirtschaftlichkeitParameter</c>, damit der Anwender im Kurzhinweis
        /// dieselbe Bezeichnung liest, die er dort gewählt hat. Kein Anzeigetext ist je
        /// Steuerwert (Drei-Schichten-Regel).
        /// </summary>
        private static string UnternehmensartAnzeige(string wert)
        {
            if (string.Equals(wert, DbWerte.UNTERNEHMENSART_PROD_GEWERBE, StringComparison.Ordinal))
                return T("PREIS_ST_ART_PROD_GEWERBE", "produzierendes Gewerbe");
            if (string.Equals(wert, DbWerte.UNTERNEHMENSART_LAND_FORST, StringComparison.Ordinal))
                return T("PREIS_ST_ART_LAND_FORST", "Land- und Forstwirtschaft");
            return T("PREIS_ST_ART_KEIN_PROD_GEWERBE", "kein produzierendes Gewerbe");
        }

        // ==================================================================
        // Kleinwerkzeug
        // ==================================================================

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
