using System;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Preiszerlegung eines BRENNSTOFF-Energieträgers (Konzept BHKW-Wirtschaftlichkeit
    /// § 4.1 und § 6.2, Etappe B2, Befund BW1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum ein eigenes Steuerelement statt weiterer Felder in
    /// <c>ucFuelSettings</c>.</b> Dieselbe Begründung wie bei
    /// <see cref="ucStromAufschlaege"/>: <c>ucFuelSettings</c> gilt für JEDEN
    /// Energieträger; Energiesteuer und BEHG-Anteil gibt es nur bei Brennstoffen. Als
    /// eingebetteter Block bleibt der Bestandsdialog unangetastet — <c>ucFuelSettings</c>
    /// hängt ihn nur bei der Brennstoff-Familie an und wächst dafür um seine Höhe. Zudem
    /// bleibt <c>ucFuelSettings.Designer.cs</c> unberührt (CLAUDE.md: Designer-Dateien
    /// nicht von Hand editieren).
    /// </para>
    /// <para>
    /// <b>Der entscheidende Unterschied zum Strom-Block.</b> Beim Strom ist der
    /// Aufschlag ein ZUSCHLAG, der auf den Arbeitspreis addiert wird. Hier ist die
    /// Zerlegung eine AUFTEILUNG DESSELBEN Preises: Die Bestandteile stecken bereits im
    /// Arbeitspreis. Es wird deshalb <b>nie</b> etwas auf den Arbeitspreis addiert, und
    /// <c>Aufschlagssatz.WirksamCtKwh</c> wird hier <b>nicht</b> gelesen — im Modus
    /// „Gesamtwert" liefert es 0, weil es keinen Aufschlag gibt. Die aussagekräftige
    /// Größe ist <see cref="SpeicherEngine.Aufschlagssatz.SummeAktivCtKwh"/>.
    /// </para>
    /// <para>
    /// <b>Die beiden Modi (§ 4.1).</b>
    /// <list type="bullet">
    /// <item><description><b>Gesamtwert</b> (Vorbelegung): Der Arbeitspreis des Trägers
    /// ist gesetzt, die Bestandteile sind reine Transparenz. Die Restzeile nennt den
    /// „nicht aufgeschlüsselten Rest" = Arbeitspreis − Summe der AKTIVEN Anteile; ein
    /// negativer Rest steht in <c>Firebrick</c>, statt verschwiegen zu werden.</description></item>
    /// <item><description><b>Aufgeschlüsselt</b>: Die Summe der Bestandteile IST der
    /// Preis. Der Knopf „In Arbeitspreis übernehmen" feuert lediglich
    /// <see cref="InArbeitspreisUebernehmen"/> — den Wert trägt der Wirt in sein
    /// Arbeitspreisfeld ein. <b>Dieses Steuerelement schreibt den Preis nie selbst
    /// in die Datenbank</b>; es gibt genau eine Preiswahrheit, und die heißt
    /// <c>energy_project_settings.custom_price_work</c>.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>NULL heißt „kein Anteil".</b> Anders als beim Strom-Aufschlagsblock wird ein
    /// leeres Feld NICHT durch einen Vorschlagssatz ersetzt (Konzept § 5.1, Falle aus
    /// E5). Der Katalogsatz kommt ausschließlich über die Schnellwahlknöpfe ins Feld.
    /// </para>
    /// <para>
    /// <b>Die Schnellwahl liest den Katalog, nicht Konstanten</b> (§ 6.2, Befund A7):
    /// Sätze und CO₂-Preis kommen aus <see cref="GesetzKatalog"/>, die Zuordnung
    /// Brennstoff → Katalogschlüssel aus <c>WirtschaftlichkeitCtrl</c>. Ist eine Größe
    /// nicht belegbar, bleibt der Knopf gesperrt und der Kurzhinweis nennt den Grund —
    /// eine geratene Zahl wäre genau der Fehlertyp, den Leitentscheidung L3 verhindert.
    /// </para>
    /// <para>
    /// <b>Kulturregel:</b> Eingabe und Anzeige über <c>Program.ZahlParsen</c> /
    /// <c>CurrentCulture</c>; in die Datenbank geht ausschließlich der <c>double</c>.
    /// </para>
    /// </remarks>
    public partial class ucBrennstoffBestandteile : UserControl
    {
        /// <summary>
        /// Breite des Blocks — passend zu <c>panel1</c>/<c>dgvHistory</c> in ucFuelSettings.
        /// </summary>
        /// <remarks>
        /// <b>Muss mit <c>this.Size</c> im Designer übereinstimmen.</b> <c>ucFuelSettings</c>
        /// liest <see cref="HOEHE"/>, um die eigene Höhe zu vergrößern; die Designer-Größe
        /// kann keine Konstante referenzieren und steht deshalb dort als Zahl (548, 304).
        /// Wird eine der beiden Stellen geändert, muss die andere mitgeführt werden.
        /// </remarks>
        public const int BREITE = 548;

        /// <summary>Gesamthöhe des Blocks. Siehe Hinweis bei <see cref="BREITE"/>.</summary>
        public const int HOEHE = 304;

        // Das Raster, aus dem die Festkoordinaten im Designer gerechnet sind - dasselbe
        // wie bei ucStromAufschlaege, damit beide Bloecke im Traegerdialog buendig sind.
        // Die Wahrheit steht in ucBrennstoffBestandteile.Designer.cs.
        //   Spalten:  Schalter x = 14, Wertfeld x = 250, Einheit x = 350
        //   Zeilen:   Wertfeld y = 48 + i * 27 (i = 0..4, Zeilenhoehe 27); Zeile i = 1
        //             traegt statt eines Wertfeldes die drei Schnellwahlknoepfe
        //             der Energiesteuer
        //   Breiten:  Schalter 250 - 14 - 8 = 228, Wertfeld 92, Summen-/Restzeile
        //             548 - 2 * 14 = 520

        /// <summary>Faktor Gramm/Kilowattstunde und Euro/Tonne auf Cent/Kilowattstunde:
        /// g/kWh * EUR/t / 10 000 = ct/kWh (1 t = 1e6 g, 1 EUR = 100 ct).</summary>
        private const double G_KWH_MAL_EUR_T_JE_CT_KWH = 10000.0;

        /// <summary>Gigajoule je Megawattstunde — Umrechnung der Saetze in EUR/GJ.</summary>
        private const double GJ_JE_MWH = 3.6;

        private readonly BrennstoffBestandteilModel _modell;

        /// <summary>
        /// Die vier Wertfelder in der Reihenfolge Energiesteuer, CO₂, Netz-/Messentgelt,
        /// Vertrieb. Die Indexreihenfolge ist Vertrag: <see cref="ZeigeModell"/>,
        /// <see cref="InsModell"/> und die beiden Schnellwahlwege (Index 0 und 1) hängen
        /// daran. Befüllt aus den Designer-Feldern im Konstruktor.
        /// </summary>
        private readonly TextBox[] _felder;

        /// <summary>Die zugehörigen Schalter, gleiche Indexreihenfolge wie <see cref="_felder"/>.</summary>
        private readonly CheckBox[] _schalter;

        /// <summary>Sperrt das Zurückschreiben, solange die Felder programmatisch gefüllt werden.</summary>
        private bool _laden;

        // ---- Traegerbezug: Was die Einheitenkette der Schnellwahl braucht ----

        /// <summary><c>Tab_Brennstoff_Stamm.ID</c> des Trägers — der Schlüssel, über den
        /// <c>WirtschaftlichkeitCtrl</c> den Katalogsatz zuordnet.</summary>
        private readonly int _idBrennstoff;

        private readonly int _idProjekt;
        private readonly int _idEnergietraeger;

        /// <summary>Abrechnungseinheit des Trägers (kWh / Nm³ / L / kg).</summary>
        private string _abrechnungseinheit = "";

        /// <summary>Wirksamer Heizwert [kWh je Abrechnungseinheit], Projektwert vor Katalogwert.</summary>
        private double _effHi;

        /// <summary>Wirksamer Brennwert [kWh je Abrechnungseinheit]; 0 = nicht gepflegt.</summary>
        private double _effHs;

        /// <summary>Jahr, für das die Katalogsätze gelesen werden (Bilanzjahr des Projekts).</summary>
        private readonly int _katalogJahr;

        /// <summary>Gesetzeskatalog — nach dem Laden datenbankfrei.</summary>
        private readonly GesetzKatalog _gesetze = new GesetzKatalog();

        /// <summary>CO₂-Preis-Override des Projekts [€/t]; 0 = Katalogpfad (Muster
        /// <c>WirtschaftlichkeitCtrl.BaueCo2Reihe</c>).</summary>
        private double _co2PreisProjekt;

        /// <summary>Arbeitspreis des Trägers [ct/kWh], vom Wirt gesetzt.</summary>
        private double _arbeitspreisCtKwh;

        /// <summary>Trägt die Begründungen der gesperrten Schnellwahlknöpfe.</summary>
        private readonly ToolTip _tip = new ToolTip();

        /// <summary>
        /// Erzeugt den Block für eine (Projekt, Energieträger)-Zeile und liest ihren
        /// Stand aus der Datenbank.
        /// </summary>
        public ucBrennstoffBestandteile(int idProjekt, int idEnergietraeger)
        {
            // Die Datenbankarbeit läuft wie beim Strom-Block VOR dem Aufbau der
            // Oberfläche: Wirft sie, ist kein einziges Steuerelement erzeugt, und
            // ucFuelSettings fängt den Fehler ab, ohne eine halb aufgebaute Maske
            // stehen zu lassen.
            BrennstoffBestandteilCtrl.StelleSpaltenSicher();
            _modell = new BrennstoffBestandteilCtrl().Read(idProjekt, idEnergietraeger);

            _idProjekt = idProjekt;
            _idEnergietraeger = idEnergietraeger;
            _idBrennstoff = TraegerbezugLesen(idProjekt, idEnergietraeger);
            _katalogJahr = KatalogjahrErmitteln(idProjekt, out _co2PreisProjekt);

            // Der Designer setzt AutoScaleMode bewusst NICHT — das Steuerelement
            // übernimmt die Regel seines Wirts (ucFuelSettings: AutoScaleMode.None),
            // passend dazu, dass die Anwendung DpiUnaware läuft.
            InitializeComponent();

            // Reihenfolge NICHT ändern, siehe Feldkommentar.
            _felder = new[] { _tbEnergiesteuer, _tbCo2, _tbNetzentgelt, _tbVertrieb };
            _schalter = new[] { _chkEnergiesteuer, _chkCo2, _chkNetzentgelt, _chkVertrieb };

            TexteSetzen();
            SchnellwahlBeschriften();
            ZeigeModell();
        }

        // ==================================================================
        // Öffentliche Schnittstelle zum Wirt
        // ==================================================================

        /// <summary>
        /// Der Arbeitspreis des Trägers [ct/kWh]. Der Wirt setzt ihn beim Aufbau und
        /// zieht ihn bei jeder Änderung seines Eingabefeldes nach — er ist die
        /// Bezugsgröße der Restzeile im Modus „Gesamtwert".
        /// </summary>
        /// <remarks>
        /// Es gibt bewusst keinen Rückweg: Dieses Steuerelement schreibt den
        /// Arbeitspreis nie. Wer ihn ändern will, hört auf
        /// <see cref="InArbeitspreisUebernehmen"/>.
        /// </remarks>
        public double ArbeitspreisCtKwh
        {
            set
            {
                if (Math.Abs(_arbeitspreisCtKwh - value) < 1e-12) return;
                _arbeitspreisCtKwh = value;
                SummeAktualisieren();
            }
        }

        /// <summary>
        /// Der Preis, der sich aus den AKTIVEN Bestandteilen ergibt [ct/kWh] — der Wert,
        /// den der Knopf „In Arbeitspreis übernehmen" anbietet.
        /// </summary>
        public double PreisAusBestandteilenCtKwh
        {
            get
            {
                InsModell();
                return BrennstoffBestandteilCtrl.AlsAufschlagssatz(_modell).SummeAktivCtKwh;
            }
        }

        /// <summary>Die Zerlegung hat sich geändert (Modus, Wert, Komponente) — für die
        /// Effektivpreis-Zeile des Trägerdialogs. Muster
        /// <c>ucStromAufschlaege.WirksamGeaendert</c>.</summary>
        public event EventHandler WirksamGeaendert;

        /// <summary>
        /// Der Anwender möchte den Preis aus den Bestandteilen in das Arbeitspreisfeld
        /// des Trägerdialogs übernehmen. Der Wert steht in
        /// <see cref="PreisAusBestandteilenCtKwh"/>; das Eintragen ist Sache des Wirts.
        /// </summary>
        public event EventHandler InArbeitspreisUebernehmen;

        /// <summary>Der aktuelle Stand — nach <see cref="Uebernehmen"/> der gespeicherte.</summary>
        public BrennstoffBestandteilModel Modell
        {
            get { return _modell; }
        }

        /// <summary>
        /// Der Wirt hat Heizwert oder Brennwert geändert — die Einheitenkette der
        /// Schnellwahl rechnet danach mit den neuen Werten. Beide Größen in
        /// <b>kWh je Abrechnungseinheit</b> (Basiseinheit, nicht Anzeigeeinheit).
        /// </summary>
        public void HeizwerteAktualisieren(double effHi, double effHs)
        {
            if (effHi > 0) _effHi = effHi;
            if (effHs > 0) _effHs = effHs;
            SchnellwahlBeschriften();
        }

        // ==================================================================
        // Texte
        // ==================================================================

        /// <summary>MyResource mit deutschem Rückfall (Drei-Schichten-Regel) — dasselbe
        /// Muster wie <c>ucFuelSettings.TKd4</c>. Die Schlüssel tragen den Präfix
        /// <c>BB_</c>; der Rückfall greift auf einer Ressourcendatei ohne die neuen
        /// Einträge.</summary>
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
            _gbBestandteile.Text = T("BB_GRUPPE_BESTANDTEILE", "Preisbestandteile des Brennstoffs");

            // --- Modusumschalter (Konzept 4.1) ---
            _rbAufgeschluesselt.Text = T("BB_MODUS_AUFGESCHLUESSELT", "aufgeschlüsselt (Summe ist der Preis)");
            _rbGesamtwert.Text = T("BB_MODUS_GESAMTWERT", "Gesamtwert (Arbeitspreis gilt)");

            // --- Die vier Komponenten ---
            _chkEnergiesteuer.Text = T("BB_KOMP_ENERGIESTEUER", "Energiesteuer");
            _chkCo2.Text = T("BB_KOMP_CO2", "CO₂-Anteil (BEHG)");
            _chkNetzentgelt.Text = T("BB_KOMP_NETZENTGELT", "Netz-/Messentgelt");
            _chkVertrieb.Text = T("BB_KOMP_VERTRIEB", "Vertrieb");

            _lblEinheitEnergiesteuer.Text = DbWerte.PREISREIHE_EINHEIT_CT_KWH;
            _lblEinheitCo2.Text = DbWerte.PREISREIHE_EINHEIT_CT_KWH;
            _lblEinheitNetzentgelt.Text = DbWerte.PREISREIHE_EINHEIT_CT_KWH;
            _lblEinheitVertrieb.Text = DbWerte.PREISREIHE_EINHEIT_CT_KWH;
            _lblEinheitArbeitspreis.Text = DbWerte.PREISREIHE_EINHEIT_CT_KWH;

            _lblSchnellwahl.Text = T("BB_SCHNELLWAHL", "Schnellwahl (Katalog):");
            _lblArbeitspreis.Text = T("BB_LABEL_ARBEITSPREIS", "Arbeitspreis (Trägerdialog)");
            _btnInArbeitspreis.Text = T("BB_BTN_IN_ARBEITSPREIS", "In Arbeitspreis übernehmen");
        }

        // ==================================================================
        // Ereignisse
        // ==================================================================

        private void rbAufgeschluesselt_CheckedChanged(object sender, EventArgs e)
        {
            ModusGewechselt();
        }

        /// <summary>
        /// Gemeinsamer Schalter-Handler der vier Komponentenzeilen — er arbeitet
        /// ausschließlich über das Modell, nicht über <c>sender</c>.
        /// </summary>
        private void KomponenteSchalter_CheckedChanged(object sender, EventArgs e)
        {
            SummeAktualisieren();
        }

        /// <summary>
        /// Gemeinsamer Handler der vier Wertfelder: einfärben und die Live-Summe
        /// nachziehen.
        /// </summary>
        private void Zahlenfeld_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
            SummeAktualisieren();
        }

        private void btnSatzRegel_Click(object sender, EventArgs e)
        {
            SatzUebernehmen(SatzRegel(), 0);
        }

        private void btnSatz53a_Click(object sender, EventArgs e)
        {
            SatzUebernehmen(Satz53a(), 0);
        }

        private void btnSatz54_Click(object sender, EventArgs e)
        {
            SatzUebernehmen(Satz54(), 0);
        }

        private void btnCo2_Click(object sender, EventArgs e)
        {
            SatzUebernehmen(SatzCo2(), 1);
        }

        private void btnInArbeitspreis_Click(object sender, EventArgs e)
        {
            if (InArbeitspreisUebernehmen != null)
                InArbeitspreisUebernehmen(this, EventArgs.Empty);
        }

        /// <summary>
        /// Trägt einen Schnellwahlwert in die genannte Zeile ein, schaltet sie aktiv und
        /// schreibt die Herkunft in die Fußzeile. Nicht belegbare Sätze kommen hier nicht
        /// an — ihre Knöpfe sind gesperrt.
        /// </summary>
        private void SatzUebernehmen(Schnellwahl s, int index)
        {
            if (s == null || !s.CtKwh.HasValue) return;

            _felder[index].Text = s.CtKwh.Value.ToString("0.####", CultureInfo.CurrentCulture);
            _schalter[index].Checked = true;
            _lblQuelle.Text = s.Herkunft;
        }

        // ==================================================================
        // Modell <-> Oberfläche
        // ==================================================================

        private void ZeigeModell()
        {
            _laden = true;
            try
            {
                _felder[0].Text = Anzeige(_modell.Energiesteuer);
                _felder[1].Text = Anzeige(_modell.CO2);
                _felder[2].Text = Anzeige(_modell.Netzentgelt);
                _felder[3].Text = Anzeige(_modell.Vertrieb);

                _schalter[0].Checked = _modell.Energiesteuer_Aktiv;
                _schalter[1].Checked = _modell.CO2_Aktiv;
                _schalter[2].Checked = _modell.Netzentgelt_Aktiv;
                _schalter[3].Checked = _modell.Vertrieb_Aktiv;

                bool gesamtwert = _modell.Modus != DbWerte.SP_AUFSCHLAG_MODUS_AUFGESCHLUESSELT;
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
        /// Liest die Felder in das Modell zurück. Ein LEERES Feld heißt ausdrücklich
        /// „kein Anteil" und wird zu <c>null</c> — der Vorschlagssatz kommt nur über die
        /// Schnellwahl ins Feld (Konzept § 5.1). Unlesbare Felder behalten den bisherigen
        /// Wert; die Rückmeldung gibt die Einfärbung (<c>Program.ZahlFaerben</c>), nicht
        /// eine modale Meldung.
        /// </summary>
        public void InsModell()
        {
            _modell.Energiesteuer = Zahl(_felder[0], _modell.Energiesteuer);
            _modell.CO2 = Zahl(_felder[1], _modell.CO2);
            _modell.Netzentgelt = Zahl(_felder[2], _modell.Netzentgelt);
            _modell.Vertrieb = Zahl(_felder[3], _modell.Vertrieb);

            _modell.Energiesteuer_Aktiv = _schalter[0].Checked;
            _modell.CO2_Aktiv = _schalter[1].Checked;
            _modell.Netzentgelt_Aktiv = _schalter[2].Checked;
            _modell.Vertrieb_Aktiv = _schalter[3].Checked;

            _modell.Modus = _rbGesamtwert.Checked
                ? DbWerte.SP_AUFSCHLAG_MODUS_GESAMTWERT
                : DbWerte.SP_AUFSCHLAG_MODUS_AUFGESCHLUESSELT;
        }

        /// <summary>
        /// Übernimmt die Eingaben und schreibt sie zurück. Rückgabe false, wenn es keine
        /// Zeile in <c>energy_project_settings</c> gibt — dann ist der Energieträger dem
        /// Projekt nicht zugeordnet.
        /// </summary>
        /// <remarks>
        /// Geschrieben werden ausschließlich die Bestandteile. Der Arbeitspreis bleibt
        /// unberührt — auch dann, wenn die Summe der Bestandteile von ihm abweicht.
        /// </remarks>
        public bool Uebernehmen()
        {
            InsModell();
            return new BrennstoffBestandteilCtrl().Update(_modell);
        }

        // ==================================================================
        // Live-Rechnung
        // ==================================================================

        private void ModusGewechselt()
        {
            // Anders als beim Strom-Block bleiben die Komponentenfelder in BEIDEN Modi
            // schreibbar: Im Modus "Gesamtwert" sind sie die Transparenz zum gesetzten
            // Arbeitspreis, im Modus "aufgeschluesselt" bilden sie ihn. Es gibt hier
            // kein Override-Feld, das sie ersetzen koennte.
            _btnInArbeitspreis.Enabled = _rbAufgeschluesselt.Checked;
            SummeAktualisieren();
        }

        private void SummeAktualisieren()
        {
            if (_laden) return;

            InsModell();

            // NUR SummeAktivCtKwh: WirksamCtKwh ist die Strom-Semantik (Aufschlag) und
            // liefert im Modus "Gesamtwert" 0, weil es hier keinen Aufschlag gibt. Die
            // Zerlegung wird NIE auf den Arbeitspreis addiert.
            double summe = BrennstoffBestandteilCtrl.AlsAufschlagssatz(_modell).SummeAktivCtKwh;
            bool aufgeschluesselt = _rbAufgeschluesselt.Checked;

            _lblSumme.Text = aufgeschluesselt
                ? string.Format(T("BB_PREIS_AUS_BESTANDTEILEN",
                                  "Preis aus den Bestandteilen: {0} ct/kWh"), Anzeige(summe))
                : string.Format(T("BB_SUMME_AKTIV",
                                  "Summe der aktiven Bestandteile: {0} ct/kWh"), Anzeige(summe));

            _lblArbeitspreisWert.Text = Anzeige(_arbeitspreisCtKwh);

            if (aufgeschluesselt)
            {
                _lblRest.Text = T("BB_REST_HINWEIS_MODUS",
                    "Im Modus „aufgeschlüsselt“ ist die Summe der Bestandteile der Preis. " +
                    "Der Arbeitspreis ändert sich erst, wenn Sie ihn übernehmen.");
                _lblRest.ForeColor = Color.FromArgb(100, 100, 100);
            }
            else
            {
                double rest = _arbeitspreisCtKwh - summe;
                _lblRest.Text = string.Format(T("BB_REST",
                    "Nicht aufgeschlüsselter Rest: {0} ct/kWh"), Anzeige(rest));

                // Ein negativer Rest heisst: Die ausgewiesenen Bestandteile sind
                // zusammen teurer als der Preis. Das wird benannt, nicht geglaettet.
                _lblRest.ForeColor = rest < 0.0 ? Color.Firebrick : Color.FromArgb(100, 100, 100);
            }

            if (WirksamGeaendert != null) WirksamGeaendert(this, EventArgs.Empty);
        }

        // ==================================================================
        // Schnellwahl aus dem Gesetzeskatalog (Konzept § 6.2, Befund A7)
        // ==================================================================

        /// <summary>Ergebnis einer Schnellwahl-Auflösung: entweder ein belegter Wert
        /// samt Herkunft, oder ein Grund, warum es keinen gibt.</summary>
        private sealed class Schnellwahl
        {
            /// <summary>Der Satz in ct/kWh; <c>null</c> = nicht belegbar.</summary>
            public double? CtKwh;

            /// <summary>Beschriftung des Knopfes.</summary>
            public string Beschriftung = "";

            /// <summary>Herkunft (Quelle, Jahr, Status) bzw. der Grund, warum der Satz
            /// nicht belegbar ist — beides geht in den Tooltip.</summary>
            public string Herkunft = "";
        }

        /// <summary>
        /// Beschriftet die vier Schnellwahlknöpfe mit dem Jahressatz und sperrt die, für
        /// die der Katalog nichts hergibt. Läuft beim Aufbau und nach jeder Änderung von
        /// Heizwert oder Brennwert.
        /// </summary>
        private void SchnellwahlBeschriften()
        {
            Knopf(_btnSatzRegel, SatzRegel());
            Knopf(_btnSatz53a, Satz53a());
            Knopf(_btnSatz54, Satz54());
            Knopf(_btnCo2, SatzCo2());
        }

        private void Knopf(Button b, Schnellwahl s)
        {
            b.Text = s.Beschriftung;
            b.Enabled = s.CtKwh.HasValue;
            _tip.SetToolTip(b, s.Herkunft);
        }

        private Schnellwahl SatzRegel()
        {
            return Satz(WirtschaftlichkeitCtrl.EnergiesteuerSchluessel(_idBrennstoff, false),
                        T("BB_BTN_SATZ_REGEL", "§ 2: {0}"));
        }

        private Schnellwahl Satz53a()
        {
            return Satz(WirtschaftlichkeitCtrl.EnergiesteuerSchluessel(_idBrennstoff, true),
                        T("BB_BTN_SATZ_53A", "§ 53a: {0}"));
        }

        private Schnellwahl Satz54()
        {
            return Satz(WirtschaftlichkeitCtrl.Energiesteuer54Schluessel(_idBrennstoff),
                        T("BB_BTN_SATZ_54", "§ 54: {0}"));
        }

        /// <summary>
        /// Löst einen Energiesteuersatz auf: Katalogschlüssel → Jahressatz → ct/kWh.
        /// Jede Stufe kann leer ausgehen, und dann sagt <see cref="Schnellwahl.Herkunft"/>
        /// welche.
        /// </summary>
        private Schnellwahl Satz(string schluessel, string muster)
        {
            var s = new Schnellwahl();
            string leer = T("BB_BTN_KEIN_SATZ", "—");

            if (string.IsNullOrEmpty(schluessel))
            {
                // Die Zuordnung Brennstoff -> Katalogschluessel ist ausdruecklich
                // unvollstaendig (WirtschaftlichkeitCtrl.EnergiesteuerSchluessel):
                // Stadtgas, Wasserstoff, Kohle, Biogas, Holz und Fernwaerme haben
                // keinen Satz. Eine geratene Einordnung waere schlimmer als keine.
                s.Beschriftung = string.Format(muster, leer);
                s.Herkunft = T("BB_GRUND_KEIN_SCHLUESSEL",
                    "Diesem Energieträger ist im Katalog kein Energiesteuersatz zugeordnet.");
                return s;
            }

            GesetzParameter p = _gesetze.WertMitHerkunft(schluessel, _katalogJahr);
            if (p == null || !p.Wert.HasValue)
            {
                s.Beschriftung = string.Format(muster, leer);
                s.Herkunft = string.Format(T("BB_GRUND_KEIN_JAHR",
                    "Der Katalog führt für {0} keinen Satz im Jahr {1}."),
                    schluessel, _katalogJahr);
                return s;
            }

            string grund;
            double? ct = InCtKwh(p.Wert.Value, p.Einheit, out grund);
            if (!ct.HasValue)
            {
                s.Beschriftung = string.Format(muster, leer);
                s.Herkunft = grund;
                return s;
            }

            s.CtKwh = ct;
            s.Beschriftung = string.Format(muster, ct.Value.ToString("0.####", CultureInfo.CurrentCulture));
            s.Herkunft = string.Format(T("BB_QUELLE", "{0} {1} (ab {2}, {3})"),
                p.Wert.Value.ToString("0.####", CultureInfo.CurrentCulture),
                p.Einheit, p.JahrVon, Herkunftstext(p));
            return s;
        }

        /// <summary>
        /// Löst den CO₂-Anteil auf: BEHG-Preis [€/t] × Emissionsfaktor des Trägers
        /// [g/kWh] → ct/kWh.
        /// </summary>
        /// <remarks>
        /// <para><b>Der Preis folgt derselben Vorrangregel wie der Rechenweg</b>
        /// (<c>WirtschaftlichkeitCtrl.BaueCo2Reihe</c>): Ein Projektwert &gt; 0 in
        /// <c>Tab_ProjektWirtschaftlichkeit.CO2_Preis</c> geht vor, sonst gilt der
        /// Katalogpfad <c>CO2_PREIS_NEHS</c> des Bilanzjahres.</para>
        /// <para><b>Der Faktor ist das reine CO₂</b> aus
        /// <c>EmissionsFaktorLader.Lade(...).Co2GKwh</c> — dieselbe Größe, mit der
        /// <c>KostenEmissionRechner</c> die BEHG-Basis bildet, und nicht das
        /// CO₂-Äquivalent. Er ist <b>heizwertbezogen</b>, wie der Arbeitspreis des
        /// Dialogs (<c>Preis ÷ Hi</c>).</para>
        /// </remarks>
        private Schnellwahl SatzCo2()
        {
            var s = new Schnellwahl();
            string leer = T("BB_BTN_KEIN_SATZ", "—");
            string muster = T("BB_BTN_CO2", "BEHG: {0}");

            double preis = _co2PreisProjekt;
            string herkunftPreis;
            if (preis > 0.0)
            {
                herkunftPreis = string.Format(T("BB_QUELLE_CO2_PROJEKT",
                    "{0} €/t (Projektwert)"), preis.ToString("0.##", CultureInfo.CurrentCulture));
            }
            else
            {
                GesetzParameter g = _gesetze.WertMitHerkunft(DbWerte.GESETZ_CO2_PREIS_NEHS, _katalogJahr);
                if (g == null || !g.Wert.HasValue)
                {
                    s.Beschriftung = string.Format(muster, leer);
                    s.Herkunft = string.Format(T("BB_GRUND_KEIN_CO2_PREIS",
                        "Der Katalog führt für das Jahr {0} keinen CO₂-Preis."), _katalogJahr);
                    return s;
                }
                preis = g.Wert.Value;
                herkunftPreis = string.Format(T("BB_QUELLE_CO2_KATALOG",
                    "{0} €/t (ab {1}, {2})"),
                    preis.ToString("0.##", CultureInfo.CurrentCulture), g.JahrVon, Herkunftstext(g));
            }

            double? ef = null;
            try { ef = EmissionsFaktorLader.Lade(_idProjekt, _idEnergietraeger).Co2GKwh; }
            catch { }

            if (!ef.HasValue || ef.Value <= 0.0)
            {
                // Kein Faktor heisst entweder "nicht gepflegt" oder "biogen, also 0"
                // (Holz, Pellets, Hackschnitzel). Beides fuehrt zu keinem BEHG-Anteil.
                s.Beschriftung = string.Format(muster, leer);
                s.Herkunft = T("BB_GRUND_KEIN_EF",
                    "Für diesen Energieträger ist kein CO₂-Faktor größer null gepflegt.");
                return s;
            }

            double ct = ef.Value * preis / G_KWH_MAL_EUR_T_JE_CT_KWH;
            s.CtKwh = ct;
            s.Beschriftung = string.Format(muster, ct.ToString("0.####", CultureInfo.CurrentCulture));
            s.Herkunft = string.Format(T("BB_QUELLE_CO2", "{0} × {1} g/kWh"),
                herkunftPreis, ef.Value.ToString("0.##", CultureInfo.CurrentCulture));
            return s;
        }

        /// <summary>
        /// Die Einheitenkette des Konzepts § 6.2: bringt einen Katalogsatz in ct/kWh.
        /// <c>null</c> heißt „nicht belegbar", und <paramref name="grund"/> sagt warum —
        /// eine geratene Zahl gibt es hier nicht.
        /// </summary>
        /// <remarks>
        /// <para><b>EUR/MWh ist brennwertbezogen.</b> Der Satz je Megawattstunde bemisst
        /// sich am Brennwert (Ho); der Arbeitspreis des Dialogs entsteht dagegen als
        /// <c>Preis ÷ Hi</c>. Umgerechnet wird deshalb mit <c>Hs/Hi</c> — dieselbe Regel
        /// wie in <c>SteuerGutschriftRechner.MengeInGesetzlicherEinheit</c>. Fehlt der
        /// Brennwert, bleibt der Faktor 1 (konservativ, der Anteil fällt um rund 10 %
        /// zu niedrig aus) — ebenfalls wie dort.</para>
        /// <para><b>EUR/1000 l und EUR/1000 kg gehen nur bei passender
        /// Abrechnungseinheit.</b> Die Brücke Liter ↔ Kilogramm bräuchte die Dichte, und
        /// <c>energy_carrier.density</c> ist im gesamten Bestand leer (Konzept § 10
        /// Punkt 4). Lieber kein Wert als eine geratene Dichte.</para>
        /// </remarks>
        private double? InCtKwh(double wert, string einheit, out string grund)
        {
            grund = "";
            string e = (einheit ?? "").Trim();

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_CT_KWH, StringComparison.OrdinalIgnoreCase))
                return wert;

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_MWH, StringComparison.OrdinalIgnoreCase))
            {
                double faktor = (_effHi > 0.0 && _effHs > 0.0) ? _effHs / _effHi : 1.0;
                return wert / 10.0 * faktor;
            }

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_GJ, StringComparison.OrdinalIgnoreCase))
                return wert * GJ_JE_MWH / 10.0;

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_1000L, StringComparison.OrdinalIgnoreCase))
                return JeTausendEinheiten(wert, "l", out grund);

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_1000KG, StringComparison.OrdinalIgnoreCase))
                return JeTausendEinheiten(wert, "kg", out grund);

            grund = string.Format(T("BB_GRUND_EINHEIT_UNBEKANNT",
                "Die Katalogeinheit „{0}“ lässt sich nicht in ct/kWh umrechnen."), e);
            return null;
        }

        /// <summary>
        /// Satz je 1.000 Abrechnungseinheiten → ct/kWh:
        /// <c>Satz / 1000 [€/Einheit] × 100 [ct/€] ÷ Hi [kWh/Einheit]</c>.
        /// </summary>
        private double? JeTausendEinheiten(double wert, string erwartet, out string grund)
        {
            grund = "";

            if (!string.Equals(_abrechnungseinheit, erwartet, StringComparison.OrdinalIgnoreCase))
            {
                grund = string.Format(T("BB_GRUND_EINHEIT",
                    "Der Satz gilt je 1.000 {0}; dieser Träger rechnet je {1}. " +
                    "Ohne gepflegte Dichte ist die Umrechnung nicht belegbar."),
                    erwartet, string.IsNullOrEmpty(_abrechnungseinheit) ? "?" : _abrechnungseinheit);
                return null;
            }

            if (_effHi <= 0.0)
            {
                grund = T("BB_GRUND_HEIZWERT",
                    "Ohne Heizwert lässt sich der Satz nicht in ct/kWh umrechnen.");
                return null;
            }

            return wert / (10.0 * _effHi);
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
        // Trägerbezug und Bilanzjahr
        // ==================================================================

        /// <summary>
        /// Liest Abrechnungseinheit, wirksamen Heiz- und Brennwert sowie die
        /// Brennstoff-ID des Trägers; Rückgabe ist <c>Tab_Brennstoff_Stamm.ID</c>
        /// (0 = unbekannt).
        /// </summary>
        /// <remarks>
        /// <b>Dieselbe Kette wie im Rechenweg</b> (<c>WirtschaftlichkeitCtrl.Traeger</c>,
        /// dort <c>private</c> mit Lauf-Cache): zuerst die gespeicherte Abfrage
        /// <c>Abfrage_Energietraeger_Effektiv</c> (Projektwert vor Katalogwert), dann die
        /// Katalogzeile <c>energy_carrier</c> als Rückfall. Es gibt keine zweite Wahrheit
        /// über Heizwerte — nur einen zweiten Leser.
        /// </remarks>
        private int TraegerbezugLesen(int idProjekt, int idEnergietraeger)
        {
            if (idEnergietraeger <= 0) return 0;

            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT billing_unit, eff_hi, eff_hs FROM Abfrage_Energietraeger_Effektiv " +
                    "WHERE ID_Projekt = ? AND carrier_id = ?",
                    new OleDbParameter("@p", idProjekt), new OleDbParameter("@c", idEnergietraeger));
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    _abrechnungseinheit = TextWert(r, "billing_unit");
                    _effHi = Kommazahl(r, "eff_hi");
                    _effHs = Kommazahl(r, "eff_hs");
                }
            }
            catch { }

            int idBrennstoff = 0;
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT id_brennstoff, billing_unit, hi_kwh_per_unit, hs_kwh_per_unit " +
                    "FROM energy_carrier WHERE id = ?",
                    new OleDbParameter("@c", idEnergietraeger));
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    idBrennstoff = (int)Kommazahl(r, "id_brennstoff");
                    if (_abrechnungseinheit.Length == 0) _abrechnungseinheit = TextWert(r, "billing_unit");
                    if (_effHi <= 0.0) _effHi = Kommazahl(r, "hi_kwh_per_unit");
                    if (_effHs <= 0.0) _effHs = Kommazahl(r, "hs_kwh_per_unit");
                }
            }
            catch { }

            return idBrennstoff;
        }

        /// <summary>
        /// Das Jahr, für das die Katalogsätze gelesen werden: das Bilanzjahr des
        /// Projekts, ersatzweise <c>BilanzKonvention.BILANZJAHR_RUECKFALL</c>. Im
        /// Katalogkontext (Projekt 0) gilt das laufende Kalenderjahr. Nebenbei kommt der
        /// CO₂-Preis-Override des Projekts mit — beides steht in derselben Zeile.
        /// </summary>
        private static int KatalogjahrErmitteln(int idProjekt, out double co2PreisProjekt)
        {
            co2PreisProjekt = 0.0;
            if (idProjekt <= 0) return DateTime.Now.Year;

            try
            {
                WirtschaftlichkeitParameter p = new WirtschaftlichkeitCtrl().LadeParameter(idProjekt);
                if (p != null)
                {
                    co2PreisProjekt = p.CO2Preis;
                    if (p.BilanzJahr > 0) return p.BilanzJahr;
                }
            }
            catch { }

            return BilanzKonvention.BILANZJAHR_RUECKFALL;
        }

        // ==================================================================
        // Kleinwerkzeug
        // ==================================================================

        /// <summary>Ein LEERES Feld heißt „kein Anteil" (<c>null</c>); ein unlesbares
        /// behält den bisherigen Wert.</summary>
        private static double? Zahl(TextBox feld, double? vorgabe)
        {
            if (feld.Text.Trim().Length == 0) return null;

            double w;
            return Program.ZahlParsen(feld.Text, out w) ? (double?)w : vorgabe;
        }

        /// <summary><c>null</c> wird zum leeren Feld — nicht zu „0".</summary>
        private static string Anzeige(double? wert)
        {
            return wert.HasValue ? Anzeige(wert.Value) : "";
        }

        private static string Anzeige(double wert)
        {
            return wert.ToString("0.###", CultureInfo.CurrentCulture);
        }

        /// <summary>Heißt <c>TextWert</c> und nicht <c>Text</c>, weil ein
        /// <c>UserControl</c> bereits eine Eigenschaft <c>Text</c> führt (CS0108).</summary>
        private static string TextWert(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return "";
            return Convert.ToString(r[spalte]).Trim();
        }

        private static double Kommazahl(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return 0.0;
            try { return Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture); }
            catch { return 0.0; }
        }
    }
}
