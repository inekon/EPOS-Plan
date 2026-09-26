using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Waermepumpe;
using EPOS.UI.Seiten.Simulation;
using Microsoft.AspNetCore.Components;
using SkiaSharp;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Simulationsergebnisses (iU9-W11b.13) — der Ersatz für
    /// <c>Form_Simulation_Detail</c> (7 629 Z. + 3 082 Designer) samt
    /// <c>DashboardForm</c>, <c>NavigatorUebersicht</c>, <c>NavigatorStrom</c>,
    /// <c>NavigatorWaerme</c> und <c>Form_SpeicherVariantenVergleich</c>.
    ///
    /// <para><b>Entscheid R-W11-1.</b> Die Komponente ist eine SEITE
    /// (<c>EPOS.UI/Seiten/Simulation/SimulationErgebnisSeite.razor</c>) mit
    /// <c>SeitenZustand</c>, einem Eintrag in <c>Seitenschluessel</c> und einem Zweig in
    /// <c>AppWurzel</c>. Unter Windows erscheint sie <b>bis W16</b> modal in
    /// <see cref="BlazorDialogForm{T}"/> (1 474 × 821, <c>Sizable</c> — die Maße des
    /// Vorläufers). Gründe: Die beiden Bedarfsobjekte gehören <c>Form_Start</c> und
    /// werden hier weitergeschrieben (Befund W11-B3); nebeneinander offen wären beide
    /// Fenster im Streit.</para>
    ///
    /// <para><b>Hier steht die ganze Datenseite.</b> Die Komponente kennt weder
    /// Controller noch Renderer; sie bekommt fertige Zahlen (die DTO aus iU9-W11a) und
    /// fertige Zeichenmodelle. Der LAUF läuft in <c>Task.Run</c>, meldet Phasen über
    /// <c>IProgress&lt;LaufFortschritt&gt;</c> und nimmt einen
    /// <c>CancellationToken</c> — die Aufteilung ist die aus
    /// <c>Form_SpeicherOptimierung</c>: Vorprüfen, Bedarf und Bestücken lesen die
    /// Datenbank und bleiben auf dem Bedienfaden.</para>
    ///
    /// <para><b>Bilder erst auf Anforderung.</b> Sechzehn Zeichenmodelle je Lauf im
    /// Voraus zu bauen wäre zu teuer; die Seite fragt je Reiter und Schalterstellung
    /// nach und hält das Ergebnis in ihrem Zwischenspeicher. Der ist seit der Etappe
    /// DG-E3 zugleich Bedingung und nicht nur Ersparnis: <c>DiagrammSvg</c> baut
    /// seinen Knotenbaum nur neu, wenn die REFERENZ des Modells wechselt.</para>
    /// </summary>
    internal sealed partial class SimulationErgebnisHuelle
    {
        // MASS (1 474 x 821, die ClientSize des Vorlaeufers) ist mit Auftrag #208
        // gefallen: Die Ergebnisseite hat seit Entscheid E-5 kein eigenes Fenster
        // mehr, das Feld hatte keinen Leser - und System.Drawing.Size gibt es in der
        // plattformfreien Datenseite nicht.

        // =================================================================
        // Anlegen
        // =================================================================

        /// <summary>
        /// Legt die Hülle an und meldet den Hilfebereich.
        /// </summary>
        /// <param name="bedarf">
        /// Die zwei Bedarfsrechnungen des Projekts — sie werden hier
        /// WEITERGESCHRIEBEN und von der Startseite für die Kachelbeschriftungen
        /// weiterverwendet (Befund W11-B3, Entscheid E-5).
        /// </param>
        /// <remarks>
        /// <para><b>iU9-W16b.4 (Entscheid E-5): kein zweites Fenster mehr.</b> Hier
        /// stand einmal ein <c>Oeffnen(besitzer, idProjekt, waermebedarf,
        /// strombedarf)</c> — ein <c>BlazorDialogForm</c> mit der Seite darin,
        /// 1 474 × 821, der Zwischenstand aus W11b (Entscheid R-W11-1). Der
        /// ausdrückliche Grund für die Modalität war Befund W11-B3: Die zwei
        /// Bedarfsobjekte gehörten <c>Form_Start</c> und wurden hier
        /// weitergeschrieben — nebeneinander offen wären beide Fenster im Streit
        /// gewesen. Sie gehören seither dem PROJEKT
        /// (<see cref="BedarfsZustand"/> im Kern).</para>
        ///
        /// <para><b>Sie liefert die INSTANZ, nicht bloss deren Gaben</b> (Auftrag
        /// #207). Daneben stand bis dahin ein statisches <c>Gaben(…)</c>, das die
        /// Hülle anlegte und gleich wieder vergass — <c>StartseiteHuelle</c> rief es
        /// bei JEDEM Kachelklick. Der gerechnete Lauf (<c>sim</c>), die zwölf Bilder
        /// und der Ergebniszustand <c>_ergebniszustand</c> leben aber HIER und nicht
        /// in der Razor-Seite: Wer die Ansicht wechselt — auf die
        /// Stromspeicher-Auslegung und zurück —, verliert die Seite, und nur eine
        /// GEHALTENE Hülle bringt den Lauf wieder mit. <c>SimulationHuelle</c> hält
        /// sie je Projekt; auch der Nachzug aus <see cref="AuslegungOeffnen"/>
        /// (Zustand „veraltet") findet so die Hülle wieder, die Schritt
        /// ③ danach zeigt.</para>
        /// </remarks>
        internal static SimulationErgebnisHuelle Erzeugen(
            SimulationPlattformwege wege, int idProjekt, BedarfsZustand bedarf)
        {
            if (bedarf == null) throw new ArgumentNullException(nameof(bedarf));

            bedarf.FuerProjekt(idProjekt);

            SimulationErgebnisHuelle huelle =
                new SimulationErgebnisHuelle(idProjekt, bedarf.Waerme, bedarf.Strom);
            huelle._wege = wege ?? new SimulationPlattformwege();

            // Bereich fuer den KI-Hilfe-Assistenten melden - woertlich wie im
            // Vorlaeufer (:436, Befund W11-B5), nur nicht mehr am Activated-Ereignis.
            KiChatKontext.BereichMelden("Detaillierte Simulation");

            return huelle;
        }

        // =================================================================
        // Zustand
        // =================================================================

        private readonly int m_ID_Projekt;
        private readonly SimulationWaermebedarf _waermebedarf;
        private readonly SimulationStrombedarf _strombedarf;

        private SimulationControl sim = new SimulationControl();
        private readonly KonfigurationCtrl ctrl = new KonfigurationCtrl();
        private readonly ProjektCtrl projektCtrl = new ProjektCtrl();

        private readonly EPOS.UI.Dienste.SeitenZustand _zustand =
            new EPOS.UI.Dienste.SeitenZustand();

        /// <summary>
        /// Was die SCHALE beisteuert — der einzige Rest, der nicht plattformfrei ist
        /// (Auftrag #208). Bis iU9-W16b.4 stand hier ein <c>Func&lt;Form&gt;</c>: das
        /// Fenster, über dem Unterdialoge erscheinen. Seit #208 liegt die Datenseite
        /// in EPOS.UI.Daten und kennt kein Fenster mehr; was eines braucht, kommt als
        /// benannter Weg herein (<see cref="SimulationPlattformwege"/>).
        /// </summary>
        private SimulationPlattformwege _wege = new SimulationPlattformwege();

        /// <summary>Die aktive Speichervariante — die Parameterseite bearbeitet sie.</summary>
        private StromspeicherVarianteModel _speicherVariante;

        /// <summary>
        /// Zustandsmaschine „Ergebnis speichern" (Nacharbeit Paket 8, Befund N1) — seit
        /// Auftrag <b>#236</b> ein benannter ZUSTAND statt eines Schalters.
        /// </summary>
        /// <remarks>
        /// Der Schalter sagte nur, ob gespeichert werden darf. Warum nicht, wusste
        /// niemand — und die Uebersicht zeichnete deshalb in allen drei Faellen (nie
        /// gerechnet, veraltet, abgebrochen) dasselbe Nullobjekt wie ein Ergebnis.
        /// </remarks>
        private ErgebnisZustand _ergebniszustand = ErgebnisZustand.NichtGerechnet;

        /// <summary>Der Anlass des Zustands in Anwendersprache; leer = keiner bekannt.</summary>
        private string _zustandsgrund = "";

        /// <summary>Liegt ein vollstaendiges Ergebnis vor? (die alte Marke als Ableitung)</summary>
        private bool ErgebnisIstGueltig => _ergebniszustand == ErgebnisZustand.Gueltig;

        /// <summary>
        /// <c>Tab_Projekt.Aenderungsdatum</c>, wie es UNMITTELBAR NACH dem Lauf stand;
        /// <c>null</c> = damals keines. Gegen diesen Stand haelt
        /// <see cref="VeraltungPruefen"/> das heutige Datum.
        /// </summary>
        /// <remarks>
        /// <b>Warum der Stand NACH dem Lauf und nicht die Uhrzeit.</b> Der Lauf darf
        /// selbst schreiben (Vorbelegungen nachziehen, Flotte uebernehmen); was er dabei
        /// am Aenderungsdatum setzt, gehoert zu SEINEM Stand. Verglichen wird deshalb der
        /// gelesene Wert mit dem gelesenen Wert - keine Uhr, keine Rundung auf Sekunden.
        /// </remarks>
        private DateTime? _aenderungsdatumBeimLauf;

        /// <summary>
        /// ERGEBNIS VERALTET BEI GEAENDERTEM PROJEKT. Die Huelle lebt je Projekt so lange,
        /// wie das Projekt offen ist, und mit ihr der gerechnete Lauf. Wer danach Bedarf,
        /// Senken oder Anlagen aendert und zur Ergebnisansicht zurueckkehrt, sah bis
        /// hierher den alten Lauf, als waere nichts gewesen - „veraltet" setzte nur die
        /// Speicherflotte.
        /// </summary>
        /// <remarks>
        /// <para>Jeder Schreibweg dieser Eingaben setzt das Aenderungsdatum des Projekts
        /// (<c>MerkmalUebernahmeCtrl.MarkiereProjektGeaendert</c> in den Controllern);
        /// ist es juenger als der Stand beim Lauf, faellt das Ergebnis auf
        /// <see cref="ErgebnisZustand.Veraltet"/> mit eigenem Anlass - derselbe
        /// Mechanismus wie nach der Stromspeicher-Auslegung.</para>
        /// <para>Der BEDARF wird dann beim naechsten Laden neu gerechnet
        /// (<see cref="BedarfSicherstellen"/>): Die Zahlen, die der Leerzustand zeigt,
        /// gehoeren sonst zum alten Stand.</para>
        /// </remarks>
        internal void VeraltungPruefen()
        {
            if (_ergebniszustand != ErgebnisZustand.Gueltig || m_ID_Projekt <= 0) return;

            if (!MerkmalUebernahmeCtrl.NachStandGeaendert(
                    _aenderungsdatumBeimLauf, MerkmalUebernahmeCtrl.Aenderungsdatum(m_ID_Projekt)))
                return;

            ZustandSetzen(ErgebnisZustand.Veraltet, MyResource.Resource.SIMERG_ZUSTAND_ANLASS_PROJEKT);
            _bedarfGerechnet = false;
        }

        /// <summary>
        /// Der Kanalbedarf des GERECHNETEN, NOCH GUELTIGEN Laufs [MWh/a] — fuer die
        /// Hydraulikuebersicht (Abnehmer ohne Versorger); <c>null</c>, solange kein
        /// gueltiger Lauf steht. Ein veralteter Lauf liefert nichts: Seine Mengen
        /// gehoeren zu einem Stand, den es nicht mehr gibt.
        /// </summary>
        internal double[] AktuellerKanalbedarf()
        {
            VeraltungPruefen();
            return _ergebniszustand == ErgebnisZustand.Gueltig
                ? SimulationRunner.BedarfJeKanal(_waermebedarf)
                : null;
        }

        /// <summary>Merkt den Zustand samt Anlass (#236).</summary>
        private void ZustandSetzen(ErgebnisZustand zustand, string grund)
        {
            _ergebniszustand = zustand;
            _zustandsgrund = grund ?? "";
        }

        /// <summary>
        /// Der Lauf ist nicht durchgegangen: Zustand merken und die Rueckmeldung
        /// liefern, die die Seite als Warnbanner zeigt (#236).
        /// </summary>
        private EPOS.UI.Seiten.Simulation.Rueckmeldung Abbruch(string grund)
        {
            ZustandSetzen(ErgebnisZustand.Abgebrochen, grund);
            return new EPOS.UI.Seiten.Simulation.Rueckmeldung(false, grund);
        }

        /// <summary>
        /// Ist ueberhaupt schon einmal gerechnet worden? (Auftrag #207)
        /// </summary>
        /// <remarks>
        /// <b>Nicht dasselbe wie <see cref="_ergebniszustand"/>.</b> Jener Zustand sagt,
        /// ob das ANGEZEIGTE Ergebnis gespeichert werden darf, und faellt bei jeder
        /// Aenderung an der Projektflotte auf <c>Veraltet</c>. Diese hier sagt nur, dass
        /// Zahlen dastehen — Schritt ③ der Ablaufleiste haengt daran, und wer aus der
        /// Auslegung zurueckkommt, soll sie samt dem Banner „Flotte geaendert" sehen
        /// duerfen. Sie faellt nie zurueck: Ein Lauf, der gelaufen ist, ist gelaufen.
        /// </remarks>
        private bool _laufGerechnet;

        /// <summary>Liegt ein gerechneter Lauf vor? (Auftrag #207, Schritt ③)</summary>
        internal bool LaufGerechnet { get { return _laufGerechnet; } }

        /// <summary>
        /// Der Grund, aus dem GAR NICHT gerechnet werden kann — die rote Vorpruefung
        /// der Ablaufleiste (Auftrag #207). Leer = frei.
        /// </summary>
        /// <remarks>
        /// Es ist dieselbe Sperre, die <see cref="Laufen"/> als Erstes prueft und die
        /// die Seite als Warnbanner zeigt (ADR-001, nicht abgeschlossene
        /// Schema-Migration). Sie wird HIER gefragt und nicht in der Komponente:
        /// <c>SchemaMigration</c> liest die Datenbank.
        /// </remarks>
        internal static string Sperrgrund()
        {
            string grund;
            return SchemaStand.SimulationGesperrt(out grund) ? (grund ?? "") : "";
        }

        /// <summary>Abbruchmarke des laufenden Simulationslaufs; <c>null</c> = kein Lauf.</summary>
        private CancellationTokenSource _laufAbbruch;

        /// <summary>Die BHKW-Betriebsart der Bedienelemente (0/1/2).</summary>
        private int _bhkwBetriebsart;

        /// <summary>
        /// Die unterste Leistungsgrenze der BHKW-Module [%] —
        /// <c>Tab_Einstellungen.Leistungsgrenze</c>.
        ///
        /// <para><b>BEFUND W6‑E‑7 (07.09.2026), hier behoben.</b> Der Blazor-Port
        /// (iU9‑W10a) las und schrieb diesen Wert in <c>m_BHKW_Grenzleistung</c> — die
        /// Spalte <c>BHKW_Grenzleistung</c> derselben Tabelle. Das ist die FALSCHE:
        /// Der Rechenweg liest <c>Leistungsgrenze</c> (<c>SimulationRunner</c>), und der
        /// Vorläufer <c>Form_Simulation_Detail</c> tat es auch
        /// (<c>numericUpDown_UnteresteLG</c> ↔ <c>ctrl.model.Leistungsgrenze</c>, Z. 5370
        /// und 5387). <c>BHKW_Grenzleistung</c> steht im gesamten Bestand auf 0 und wird
        /// von keinem Rechenweg gelesen — das Feld schrieb also ins Leere, und der
        /// interaktive Lauf bekam als Grenze stets die 0. Solange der stille Fallback in
        /// <c>SimulationBHKW</c> stand, fiel das nicht auf (0 → 30 %); ohne ihn liefe der
        /// interaktive Lauf ohne Untergrenze, während der Stapellauf mit dem gepflegten
        /// Wert rechnet. Seit W6‑E‑7 lesen und schreiben beide Wege dieselbe
        /// Spalte.</para>
        ///
        /// <para>Die Vorbelegung steht bei <c>KonfigurationModel</c>
        /// (<c>BhkwLeistungsgrenzeVorgabe.VORGABE_PROZENT</c>) und gilt für ein Projekt
        /// ohne gepflegten Wert; Migrationsschritt 67 schreibt sie in die
        /// Bestandssätze.</para>
        /// </summary>
        private int _grenzleistungBhkw = BhkwLeistungsgrenzeVorgabe.VORGABE_PROZENT;

        /// <summary>Die Was-wäre-wenn-Kapazität der Autarkiekachel (NICHT persistiert).</summary>
        private double _autarkieKwh;

        private bool _autarkieGesetzt;

        private SimulationErgebnisHuelle(int idProjekt, SimulationWaermebedarf waermebedarf,
                                         SimulationStrombedarf strombedarf)
        {
            m_ID_Projekt = idProjekt;
            _waermebedarf = waermebedarf ?? new SimulationWaermebedarf();
            _strombedarf = strombedarf ?? new SimulationStrombedarf();

            _zustand.ProjektSetzen(idProjekt, "");

            ctrl.ProjektLesen(idProjekt);
            _bhkwBetriebsart = ctrl.model != null ? ctrl.model.Betriebsart : 0;
            // W6-E-7: Leistungsgrenze, NICHT m_BHKW_Grenzleistung - Begruendung am Feld.
            _grenzleistungBhkw = ctrl.model != null
                ? ctrl.model.Leistungsgrenze
                : BhkwLeistungsgrenzeVorgabe.VORGABE_PROZENT;

            VarianteLesen();
        }

        // =================================================================
        // Der Parametersatz
        // =================================================================

        internal IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Zustand"] = _zustand,
                ["StartProjekt"] = m_ID_Projekt,
                ["Dienste"] = DiensteSatz()
            };
        }

        private SimulationErgebnisDienste DiensteSatz()
        {
            return new SimulationErgebnisDienste
            {
                Laden = Laden,
                Laufen = Laufen,
                Abbrechen = Abbrechen,
                Speichern = ErgebnisSpeichern,
                Modell = Modell,

                // Die Farbe einer Reihe gilt ANWENDUNGSWEIT (Farbrollen, Bedienung
                // Teil 2): Diagrammfarben schreibt sie ueber EinstellungenCtrl, und
                // schon das naechste Bild traegt sie - Bildschirm wie Bericht.
                FarbeSetzen = FarbeSetzen,
                FarbeZuruecksetzen = FarbeZuruecksetzen,

                // Die FUENF Laufparameter stehen seit #216 in Schritt ①; ihre
                // Delegaten liefert ParameterGaben() derselben Huelle.

                // W11b-B-29: EIN Schreibweg fuer die Speicherparameter. Ihn nehmen
                // der Parameterblock des Ergebnisreiters (jedes Feld sofort) und die
                // Auslegungsoptimierung fuer ihren Leistungspreis (W11b-E-3).
                SpeicherfeldSchreiben = SpeicherfeldSchreiben,
                SpeicherPreisreihen = SpeicherPreisreihen,

                // KonfigurationGaben stand hier bis Auftrag #207: Die Ergebnisseite
                // oeffnete die Konfiguration als zweite Ueberlagerung in ihrer
                // eigenen. Seither ist sie Schritt ① DERSELBEN Ansicht, und ihren
                // Parametersatz legt SimulationHuelle unmittelbar daneben.
                BedarfGaben = waerme => waerme
                    ? BedarfErgebnisHuelle.Gaben(_waermebedarf, true, 1, "")
                    : BedarfErgebnisHuelle.Gaben(_strombedarf, 0),

                WaermepumpenGaben = WaermepumpenGaben,
                WaermepumpenFertig = WaermepumpenFertig,

                VergleichRechnen = VergleichRechnen,
                VarianteAktivSetzen = VarianteAktivSetzen,
                VergleichCsv = VergleichCsv,

                // W11b-B-5: Hier stand EIN Delegat - die Sprungbruecke mit dem
                // einzigen Schluessel Sprungziel.SpeicherOptimierung, die
                // Form_SpeicherOptimierung modal ueber der WebView oeffnete. Danach
                // waren es zwoelf, die Datenseite der zwei Ueberlagerungen. Seit
                // Paket P3 (#192) waren es fuenf; seit Auftrag #274 sind es ZWEI.
                //
                // Der Einstieg in die Auslegung steht seither in Schritt ① neben der
                // Pufferverwaltung (Anwenderwunsch 14.09.2026) - gegangen wird er
                // weiterhin von HIER (AuslegungOeffnen, eingelegt von
                // SimulationAnsichtQuelle), nur eben nicht mehr ueber diese Datenseite.
                // Der Reiter „Stromspeicher" ist reines Ergebnis: Er braucht die CSV
                // der Flottenansicht und den Verweis zurueck nach ①.
                OptimierungCsv = OptimierungCsv,
                KonfigurationOeffnen = KonfigurationOeffnen,

                CsvBedarf = CsvBedarf,
                CsvKaelte = CsvKaelte,
                CsvWaermepumpe = CsvWaermepumpe,
                CsvHeizkessel = CsvHeizkessel,
                CsvSpeicher = CsvSpeicher,
                CsvWaermegang = CsvWaermegang,
                CsvStromgang = CsvStromgang,

                AutarkieRechnen = AutarkieRechnen
            };
        }

        // =================================================================
        // Laden — was die Seite nach EINEM Lauf zeigt
        // =================================================================

        /// <summary>
        /// Der ZULETZT zusammengetragene Stand — das, was die Seite gerade zeigt;
        /// <c>null</c> = sie hat noch nichts geladen (Auftrag #221).
        /// </summary>
        private SimulationErgebnisDaten _letzterStand;

        /// <summary>
        /// Der zuletzt geladene Stand, ohne ihn neu zusammenzutragen (Auftrag #221).
        /// </summary>
        /// <remarks>
        /// <b>Wozu.</b> Die Ansicht meldet ihre Felder beim Hilfe-Assistenten an
        /// (KI-D-E-1), und zu ihnen gehoeren die Kennzahlen des Laufs. Ein eigener
        /// Ladeweg dafuer waere ein ZWEITER Stand derselben Zahlen - und jede
        /// Leseanfrage des Assistenten ein weiterer Datenbankzugriff. Hier wird nur
        /// hergegeben, was <see cref="Laden"/> ohnehin gebaut hat.
        /// </remarks>
        internal SimulationErgebnisDaten LetzterStand { get { return _letzterStand; } }

        private SimulationErgebnisDaten Laden(int idProjekt)
        {
            _letzterStand = Zusammentragen(idProjekt);
            return _letzterStand;
        }

        /// <summary>
        /// Ist das Projekt ein Stammprojekt (keine Variante)? Dieselbe Frage wie die
        /// Projektliste (<see cref="VariantenCtrl.StammRefDerVariante"/>); ein Fehler heißt
        /// „Stamm“ — die Marke nennt dann den Schlüssel des Stammprojekts.
        /// </summary>
        private static bool IstStamm(int idProjekt)
        {
            try { return new VariantenCtrl().StammRefDerVariante(idProjekt) <= 0; }
            catch { return true; }
        }

        /// <summary>
        /// BV-E6: setzt je gezeigter Zahl und je Bild den Platzhalter, der genau diesen Wert
        /// erzeugt (Konzept Berichtsvorlagen 9.5), gehalten von
        /// <c>VorlagenfeldAnzeigewertWacheTests</c>. Deckung Wärme und Strom führt der Katalog
        /// nicht (die Kennzahlen sind je Kanal bzw. die Autarkie) — sie bleiben ohne Marke;
        /// den Strombedarf zeigt die App samt Eigenverbrauch der Erzeuger, der Katalog ohne.
        /// </summary>
        internal static void Vorlagenfelder(SimulationErgebnisDaten d, bool stamm)
        {
            if (d.Uebersicht is UebersichtDaten u)
            {
                u.WaermebedarfFeld = EPOS.UI.Dienste.Vorlagenfeldorte.Kennzahl(stamm, "energie.waermebedarf");
                u.RestwaermeFeld = EPOS.UI.Dienste.Vorlagenfeldorte.Kennzahl(stamm, "energie.waermerest");
                u.ReststromFeld = EPOS.UI.Dienste.Vorlagenfeldorte.Kennzahl(stamm, "energie.netzbezug");
                u.RingWaermeFeld = "stand.bild.deckung_waerme";
                u.RingStromFeld = "stand.bild.deckung_strom";
            }
            if (d.Bedarf?.Kaelte is KaelteDaten k && k.KaeltebedarfMwh > 0)
            {
                k.BedarfFeld = EPOS.UI.Dienste.Vorlagenfeldorte.Kennzahl(stamm, KennzahlenKatalog.SCHLUESSEL_KAELTE_JAHRESBEDARF);
                k.LastFeld = EPOS.UI.Dienste.Vorlagenfeldorte.Kennzahl(stamm, KennzahlenKatalog.SCHLUESSEL_KAELTE_SPITZE);
                k.RestFeld = EPOS.UI.Dienste.Vorlagenfeldorte.Kennzahl(stamm, KennzahlenKatalog.SCHLUESSEL_KAELTE_REST);
                if (k.Erzeuger.Count > 0)
                {
                    k.DeckungFeld = EPOS.UI.Dienste.Vorlagenfeldorte.Kennzahl(stamm, KennzahlenKatalog.SCHLUESSEL_KAELTE_DECKUNGSGRAD);
                    k.StromFeld = EPOS.UI.Dienste.Vorlagenfeldorte.Kennzahl(stamm, KennzahlenKatalog.SCHLUESSEL_KAELTE_STROM);
                    k.JazFeld = EPOS.UI.Dienste.Vorlagenfeldorte.Kennzahl(stamm, KennzahlenKatalog.SCHLUESSEL_KAELTE_JAZ);
                }
            }
            d.SpeichertemperaturFeld = stamm && d.Speichertemperaturen ? "stamm.bild.speichertemperaturen" : "";
        }

        private SimulationErgebnisDaten Zusammentragen(int idProjekt)
        {
            SimulationErgebnisDaten d = new SimulationErgebnisDaten { IdProjekt = idProjekt };

            // Blockade bei nicht abgeschlossener Schema-Migration (ADR-001) - wörtlich
            // aus SimulationBlockiert :3406-3419; nur meldet sie hier als Banner statt
            // als MessageBox.
            string sperrgrund;
            d.Gesperrt = SchemaStand.SimulationGesperrt(out sperrgrund);
            d.Sperrgrund = d.Gesperrt ? sperrgrund : "";

            ctrl.ProjektLesen(idProjekt);
            string[] tool = Tools();

            d.Parameter = Parametersatz();

            // Ist das Projekt nach dem Lauf geaendert worden, ist das Ergebnis
            // VERALTET - bevor der Zustand in den Stand geht.
            VeraltungPruefen();

            // DER ZUSTAND, NICHT NUR DIE MARKE (#236). Eine offene Schema-Migration
            // sperrt alles; sie hat ihr eigenes Banner, und ihr Grund ist zugleich der
            // Grund, aus dem hier kein Ergebnis steht.
            d.Zustand = d.Gesperrt ? ErgebnisZustand.NichtGerechnet : _ergebniszustand;
            d.Zustandsgrund = d.Gesperrt ? d.Sperrgrund : _zustandsgrund;

            // Die Reiterleiste folgt derselben Regel wie die Menüliste des Vorläufers
            // (BefuelleQuellenListe :2876-2970) - samt der drei Zweige für PV und
            // Speicher (Befund W11-B6, wörtlich übernommen).
            d.ReiterWaermepumpe = ErzeugerGewaehlt(tool, DbWerte.ERZEUGER_WAERMEPUMPE);
            d.ReiterHeizkessel = ErzeugerGewaehlt(tool, DbWerte.ERZEUGER_HEIZKESSEL);
            d.ReiterBhkw = ErzeugerGewaehlt(tool, DbWerte.ERZEUGER_BHKW);
            d.ReiterSolarthermie = ErzeugerGewaehlt(tool, DbWerte.ERZEUGER_SOLARTHERMIE);
            d.ReiterPhotovoltaik = tool[4] == DbWerte.ERZEUGER_PHOTOVOLTAIK || tool[4] == "true"
                                   || tool.Contains(DbWerte.ERZEUGER_PHOTOVOLTAIK);
            d.ReiterStromspeicher = tool[5] == DbWerte.ERZEUGER_STROMSPEICHER || tool[5] == "true"
                                    || tool.Contains(DbWerte.ERZEUGER_STROMSPEICHER);

            // ---- Die Zahlen der Reiter ------------------------------------
            // DER BEDARF STEHT IN JEDEM ZUSTAND (#236). Er haengt nicht am Lauf,
            // sondern am Projekt - und ohne ihn zeigte die Uebersicht auch im
            // Leerzustand „0,00 MWh/a", wo die Zusammenfassung daneben die richtige
            // Zahl nennt.
            BedarfSicherstellen(idProjekt);
            var bedarf = SimulationErgebnisCtrl.Bedarf(_waermebedarf, _strombedarf);
            d.Bedarf = BedarfDaten(bedarf);

            // Die Kaelteseite (Stufe KU1) haengt wie der Bedarf am Projekt, nicht am Lauf; null
            // = nicht erhoben (Projektschalter aus) - dann zeigt keine Ansicht eine Kaeltegruppe.
            d.Bedarf.Kaelte = KaelteDaten(SimulationErgebnisCtrl.Kaelte(_waermebedarf));

            // OHNE gueltiges Ergebnis wird KEINE Uebersicht gebaut - das Feld bleibt
            // null, und die Anzeige zeigt ihren Leerzustand samt Grund (#236).
            if (d.Zustand != ErgebnisZustand.Gueltig) return d;

            ErgebnisPraesenz p = ErgebnisPraesenz.Ermitteln(sim);

            d.Kennzahlen = SimulationErgebnisCtrl.Uebersicht(sim, _waermebedarf, _strombedarf);
            d.Uebersicht = UebersichtDaten(d.Kennzahlen, p, tool);

            if (sim.bSimulationWP)
                d.Waermepumpe = SimulationErgebnisCtrl.Waermepumpe(sim, _waermebedarf);
            if (sim.bSimulationKessel)
                d.Heizkessel = SimulationErgebnisCtrl.Heizkessel(sim, _waermebedarf);
            if (sim.bSimulationSolarthermie)
                d.Solarthermie = SimulationErgebnisCtrl.Solarthermie(sim, _waermebedarf);

            // BHKW und PV standen im Vorläufer AUSSERHALB jeder Bedingung - sie werden
            // nach jedem Lauf gefüllt, damit die Rubrik nach einem Folgelauf ohne die
            // Komponente nicht die Zahlen des Vorlaufs zeigt.
            d.Bhkw = SimulationErgebnisCtrl.Bhkw(sim, _waermebedarf, _strombedarf);
            d.Photovoltaik = SimulationErgebnisCtrl.Photovoltaik(sim);

            d.KesselBrennstoffe = Kesselbrennstoffe();
            d.BhkwBrennstoffe = Bhkwbrennstoffe(d.Bhkw);

            // BH-1: Der Leerhinweis der beiden Brennstoffbloecke braucht den Unterschied
            // zwischen „nicht gelaufen" und „kein Brennstoff gepflegt". Die Antwort steht
            // in der Projektdatenbank, nicht im Ergebnis - deshalb hier und nicht im
            // Reiter. Die Reihenfolge ist Absicht: Kesselbrennstoffe() setzt den
            // Kesselbefund unterwegs.
            d.KesselBrennstoffDefiniert = _kesselBrennstoffDefiniert;
            d.BhkwBrennstoffDefiniert = BhkwBrennstoffDefiniert();

            if (d.Waermepumpe != null)
            {
                d.ErdreichHinweise = d.Waermepumpe.ErdreichHinweise;
                d.ErdreichWarnung = d.Waermepumpe.ErdreichWarnung;
            }
            d.Speichertemperaturen = Temperaturreihen().Count > 0;

            // BV-E6 (Konzept Berichtsvorlagen 9.5): die Platzhalter der Zahlen und Bilder —
            // Stamm → stamm.*, Variante → stand.*. Erst hier, denn es gibt sie nur mit
            // gültigem Ergebnis.
            Vorlagenfelder(d, IstStamm(idProjekt));

            d.Speicher = SpeicherDaten();
            d.Speicher.AktiveFlotte = SpeicherFlottenProjektCtrl.AktiveKonfiguration(m_ID_Projekt);
            d.Speicher.FlotteImProjektAktiv = d.Speicher.AktiveFlotte != null;
            d.Speicher.FlottenAenderungOhneNeuenLauf = _flotteProjektGeaendert;
            if (sim.Speicherflottenergebnis != null && sim.Speicherflottenkonfiguration != null)
                d.Speicher.Flottenergebnis = new SpeicherFlottenErgebnis
                {
                    Erfolg = true, Studie = sim.Speicherflottenergebnis,
                    Eingaben = sim.Speicherflottenlauf?.Eingaben,
                    Konfiguration = sim.Speicherflottenkonfiguration,
                    Hinweise = new() { "Letzter erfolgreicher Projektlauf mit der unten genannten Betriebsführung und dem zugehörigen Eingabestand." }
                };
            d.Autarkie = AutarkieRechnen(AutarkieKapazitaet());
            d.Waermegang = WaermegangDaten(p);
            d.Stromgang = StromgangDaten(p);

            // Die Warnungen und Hinweise des Laufs - im Vorläufer eine Zeile in der
            // Fußzeile mit dem Volltext als ToolTip (LaufmeldungenAnzeigen :3777).
            d.LaufmeldungenAnzahl = SimulationProtokoll.Aktuell.AnzahlWarnungenUndHinweise;
            d.Laufmeldungen = d.LaufmeldungenAnzahl > 0
                ? SimulationProtokoll.Aktuell.HinweistextFuerAnzeige()
                : "";

            return d;
        }

        /// <summary>
        /// Die beiden Bedarfsrechnungen sind gerechnet — EINMAL je Hülle (Auftrag
        /// <b>#236</b>).
        /// </summary>
        private bool _bedarfGerechnet;

        /// <summary>
        /// Rechnet die zwei Bedarfe, wenn es noch niemand getan hat (Auftrag
        /// <b>#236</b>).
        /// </summary>
        /// <remarks>
        /// <para><b>Warum das hier stehen muss.</b> <see cref="BedarfDaten"/> liest die
        /// zwei Bedarfsobjekte des Projekts (<see cref="BedarfsZustand"/>), und die sind
        /// leer, solange sie niemand gerechnet hat. Unter Windows tut es die
        /// Projektzusammenfassung des Startreiters beim Betreten — derselbe
        /// <c>BedarfsZustand</c>, dieselben Zahlen. Wer aber die ANSICHT „Simulation"
        /// betritt, ohne vorher im Reiter gewesen zu sein, sah bis #236 „0,00 MWh/a",
        /// und auf iOS gibt es den Reiter gar nicht.</para>
        /// <para><b>Es ist derselbe Weg wie im Lauf</b>
        /// (<c>SimulationLaufCtrl.Bedarf</c>), nur ohne Simulation dahinter — und er
        /// laeuft genau einmal je Huelle: Der Lauf selbst rechnet ihn ohnehin neu und
        /// setzt die Marke.</para>
        /// <para>Ein Fehlschlag (kein Projekt, keine Klimaregion, keine Zeitreihe) ist
        /// hier KEIN Abbruch: Die Seite zeigt dann ihre Nullen, wie sie es vorher immer
        /// tat, und der Lauf nennt den Grund beim Namen.</para>
        /// </remarks>
        private void BedarfSicherstellen(int idProjekt)
        {
            if (_bedarfGerechnet || idProjekt <= 0) return;

            _bedarfGerechnet = true;   // auch ein Fehlschlag wird nicht bei jedem Zeichnen wiederholt

            try
            {
                projektCtrl.ReadSingle(idProjekt);
                int idKlimaregion = projektCtrl.m_ID_Klimaregion;
                if (idKlimaregion <= 0) return;

                string grund = SimulationLaufCtrl.Bedarf(idProjekt, idKlimaregion,
                                                         ctrl.m_Netzverluste, ctrl.m_szNetzverlusteEinheit,
                                                         _waermebedarf, _strombedarf);

                // Ein benannter Abbruch (Zapfprofilgenerator, Umsetzungskonzept 2.2, N8; oder die
                // Stromrechnung) geht ins Protokoll — die Wärmefelder stehen dann auf 0, nicht auf
                // einem früheren Stand. Den Grund der Wärmerechnung trägt der Kern schon als
                // Fehlermeldung ein; nur ein anderer kommt als Warnung dazu.
                if (!string.IsNullOrEmpty(grund) && !SimulationProtokoll.Aktuell.Fehler.Contains(grund))
                    SimulationProtokoll.Aktuell.WarnungEinmal("bedarf-vorab-" + grund,
                        "Der Bedarf konnte nicht vorab gerechnet werden: " + grund);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Der Bedarf konnte nicht vorab gerechnet werden: " + ex.Message);
            }
        }

        private string[] Tools()
        {
            string[] tool = new string[6];
            if (ctrl.model == null) return tool;

            tool[0] = ctrl.model.m_Tool_1;
            tool[1] = ctrl.model.m_Tool_2;
            tool[2] = ctrl.model.m_Tool_3;
            tool[3] = ctrl.model.m_Tool_4;
            tool[4] = ctrl.model.m_Tool_5;
            tool[5] = ctrl.model.m_Tool_6;
            return tool;
        }

        /// <summary>Steht der Erzeuger auf einem der vier Wärmeplätze? (:2900-2946)</summary>
        private static bool ErzeugerGewaehlt(string[] tool, string dbWert)
        {
            for (int i = 0; i < 4; i++)
                if (!string.IsNullOrEmpty(tool[i]) && tool[i].Trim() == dbWert) return true;
            return false;
        }

        // =================================================================
        // Die fuenf Laufparameter (bis #216 der Reiter „Parameter")
        // =================================================================

        /// <summary>
        /// Die fuenf Laufparameter samt dem Stand des Speicherblocks.
        /// </summary>
        /// <remarks>
        /// <para><b>Der Reiter „Parameter" ist mit Auftrag #216 gefallen</b>
        /// (Windows-Abnahme 11.09.2026, Punkt 3). Mit ihm die Liste seiner
        /// Unterblaetter und die Abbildung <c>BlattZuTool</c>: Welche Erzeugerkarte
        /// von Schritt ① welches Feld zeigt, entscheidet dort der <c>DbWert</c> der
        /// Zeile. Der Reiter „Stromspeicher" haengt unveraendert an
        /// <c>d.ReiterStromspeicher</c>, nicht an dieser Abbildung.</para>
        /// <para>Gelesen wird aus <c>ctrl.model</c> — der Aufrufer hat unmittelbar
        /// davor <c>ProjektLesen</c> gerufen. Betriebsart und Leistungsgrenze
        /// kommen aus den zwei Feldern der Huelle: Sie sind der Stand, mit dem
        /// auch der Lauf bestueckt wird.</para>
        /// </remarks>
        private ParameterDaten Parametersatz()
        {
            KonfigurationModel m = ctrl.model ?? new KonfigurationModel();

            return new ParameterDaten
            {
                Netzverluste = m.m_Netzverluste,
                NetzverlusteEinheit = string.IsNullOrEmpty(m.m_szNetzverlusteEinheit)
                    ? "%" : m.m_szNetzverlusteEinheit,
                Betriebsart = _bhkwBetriebsart,
                UntersteLeistungsgrenze = _grenzleistungBhkw,
                Bereitschaft = m.m_Kessel_Betriebsbereitschaft,
                Kuehlbetrieb = KonfigurationCtrl.KuehlbetriebLesen(m_ID_Projekt),
                Anlagenkopplung = KonfigurationCtrl.AnlagenkopplungLesen(m_ID_Projekt),
                Speicher = SpeicherParameter()
            };
        }

        /// <summary>
        /// Die fuenf Laufparameter fuer SCHRITT ① (Auftrag #216) — <b>derselbe
        /// Lese- und Schreibweg</b>, den bis #216 der Reiter „Parameter" der
        /// Ergebnisseite nahm.
        /// </summary>
        /// <remarks>
        /// <b>Warum die ERGEBNIShuelle und nicht die Konfigurationshuelle.</b> Die
        /// zwei Felder <c>_bhkwBetriebsart</c> und <c>_grenzleistungBhkw</c> sind
        /// der Stand, mit dem <c>SimulationLaufCtrl.Bestuecken</c> den Lauf
        /// bestueckt. Ein zweiter Schreibweg ueber die Konfigurationshuelle
        /// schriebe zwar dieselben Spalten, liesse diese Felder aber stehen — der
        /// naechste Lauf rechnete mit der alten Betriebsart. Ohne den
        /// Speicherblock: den braucht Schritt ① nicht, und sein Lesen zieht eine
        /// Variantenpflege nach sich.
        /// </remarks>
        internal SimulationParameterDienste ParameterGaben()
        {
            return new SimulationParameterDienste
            {
                Laden = () =>
                {
                    ctrl.ProjektLesen(m_ID_Projekt);
                    KonfigurationModel m = ctrl.model ?? new KonfigurationModel();
                    return new ParameterDaten
                    {
                        Netzverluste = m.m_Netzverluste,
                        NetzverlusteEinheit = string.IsNullOrEmpty(m.m_szNetzverlusteEinheit)
                            ? "%" : m.m_szNetzverlusteEinheit,
                        Betriebsart = _bhkwBetriebsart,
                        UntersteLeistungsgrenze = _grenzleistungBhkw,
                        Bereitschaft = m.m_Kessel_Betriebsbereitschaft,
                        Kuehlbetrieb = KonfigurationCtrl.KuehlbetriebLesen(m_ID_Projekt),
                        Anlagenkopplung = KonfigurationCtrl.AnlagenkopplungLesen(m_ID_Projekt)
                    };
                },
                // KUEHLUNG RECHNEN (Stufe KU1, Kuehlkonzept 8.3): der eine Schreibweg der
                // Projekteinstellung. Er liest die Programmeinstellung nicht; ohne
                // Einstellungssatz legt „ein" den Vormerksatz an (KonfigurationCtrl).
                KuehlbetriebSchreiben = an => KonfigurationCtrl.KuehlbetriebSetzen(m_ID_Projekt, an),
                // ANLAGENKOPPLUNG (Konzept Anlagenkopplung 9.4): der eine Schreibweg der
                // Projektstufe - nach der Regel des Kuehlschalters (Vormerksatz ohne Satz).
                AnlagenkopplungSchreiben = stufe => KonfigurationCtrl.AnlagenkopplungSetzen(m_ID_Projekt, stufe),
                NetzverlusteSchreiben = (wert, einheit) => KonfigSchreiben(m =>
                {
                    m.m_Netzverluste = wert;
                    m.m_szNetzverlusteEinheit = einheit;
                }),
                BetriebsartSchreiben = wert =>
                {
                    _bhkwBetriebsart = wert;
                    KonfigSchreiben(m => m.Betriebsart = wert);
                },
                LeistungsgrenzeSchreiben = wert =>
                {
                    // W6-E-7: in Leistungsgrenze, NICHT in m_BHKW_Grenzleistung.
                    _grenzleistungBhkw = wert;
                    KonfigSchreiben(m => m.Leistungsgrenze = wert);
                },
                // 16.09.2026 (Auftrag #299): Hier stand HeizstabSchreiben - der
                // Schreibweg des PROJEKTweiten Heizstabs. Er geht seither mit den
                // uebrigen Anlagenfeldern ueber WaermepumpeKonfigurationSpeichern.
                BereitschaftSchreiben = wert =>
                    KonfigSchreiben(m => m.m_Kessel_Betriebsbereitschaft = (int)wert),

                // ANWENDERWUNSCH 16.09.2026: Der Konfigurationsknopf der
                // Waermepumpenkarte zeigt die Konfiguration DIESER Anlage. Sie steht
                // in Tab_Energieanlagen, ihre Abbildung auf den Feldsatz liegt in der
                // Schale (WaermepumpeAnlageHuelle.AusModell/NachModell) - deshalb die
                // Naht. Ohne sie zeigt der Dialog nur die Projekteinstellung.
                WaermepumpeKonfigurationLaden = _wege.WaermepumpeKonfigLesen == null
                    ? (Func<int, WaermepumpeAnlageDaten>)null
                    : idAnlage => _wege.WaermepumpeKonfigLesen(m_ID_Projekt, idAnlage),

                WaermepumpeKonfigurationSpeichern = _wege.WaermepumpeKonfigSchreiben == null
                    ? (Func<int, WaermepumpeAnlageDaten, AnlagenkonfigErgebnis>)null
                    : (idAnlage, daten) =>
                        _wege.WaermepumpeKonfigSchreiben(m_ID_Projekt, idAnlage, daten),

                WaermepumpeTraegerkatalog = _wege.WaermepumpeTraegerkatalog,

                // STUFE KU2 WELLE 3 (Kuehlkonzept 8.2): die Kuehlgaben der Konfiguration -
                // plattformfrei aus dem Kern gebaut, darum KEINE Naht. Nur mit dem Leseweg
                // der Anlage: ohne ihn gibt es keine Konfiguration, in der sie stuenden.
                WaermepumpeKuehlGaben = _wege.WaermepumpeKonfigLesen == null
                    ? (Func<WaermepumpeKuehlGaben>)null
                    : () => WaermepumpeKuehlGabenBau.Bauen(m_ID_Projekt)
            };
        }

        private void KonfigSchreiben(Action<KonfigurationModel> aenderung)
        {
            try
            {
                ctrl.ProjektLesen(m_ID_Projekt);
                if (ctrl.rows == 0) return;

                aenderung(ctrl.model);
                ctrl.Update(m_ID_Projekt);
            }
            catch (Exception ex)
            {
                // Wörtlich wie SpeichereKonfigurationsAenderung :5432-5439: still.
                Console.WriteLine("Fehler beim automatischen Speichern: " + ex.Message);
            }
        }

        // =================================================================
        // Die Speichervariante (P3)
        // =================================================================

        /// <summary>
        /// Die aktive Speichervariante des Projekts — und, wenn es keine gibt, das
        /// <b>Nachziehen</b> der Regel „jede Speicheranlage führt eine Variante, genau eine
        /// ist aktiv" (W11b‑B‑27, Anwenderbefund 10.09.2026).
        /// </summary>
        /// <remarks>
        /// <para><b>Warum ein LESEWEG schreibt.</b> Eine Speicheranlage ohne Variantenzeile
        /// ist kein Zustand, den der Anwender gewählt hat, sondern einer, den der
        /// Anlagepfad hinterlassen kann (erster Speicher eines Projekts, siehe
        /// <c>StromspeicherVarianteCtrl.AktiveVarianteSicherstellen</c>). Sichtbar wird er
        /// erst hier — der Reiter „Parameter" sperrt dann seine Eingaben, und der
        /// Leistungspreis der Auslegungsoptimierung hätte kein Ziel. Nachgezogen wird
        /// deshalb an der Stelle, an der es auffällt; genau so hielt es der frühere
        /// WinForms-Weg (<c>StromspeicherKontextMenuCtrl</c>), der mit Commit 55a3f0ec
        /// gefallen ist.</para>
        /// <para><b>Idempotent und still.</b> Gibt es eine aktive Variante, schreibt der
        /// Aufruf nichts; jeder Fehlschlag ist eine Konsolenmeldung und <c>null</c> —
        /// derselbe Ausgang wie bisher, und die Maske bleibt stehen.</para>
        /// </remarks>
        private void VarianteLesen()
        {
            try
            {
                _speicherVariante = new StromspeicherVarianteCtrl().AktiveVarianteSicherstellen(m_ID_Projekt);
            }
            catch (Exception ex)
            {
                _speicherVariante = null;
                Console.WriteLine("Die Speichervariante konnte nicht gelesen werden: " + ex.Message);
            }
        }

        private SpeicherParameterDaten SpeicherParameter()
        {
            VarianteLesen();

            bool vorhanden = _speicherVariante != null;
            StromspeicherVarianteModel v = _speicherVariante ?? new StromspeicherVarianteModel();

            var geraet = StromspeicherStammCtrl.KapazitaetUndLeistung(
                m_ID_Projekt, vorhanden ? v.ID_Energieanlage : 0);
            double kapazitaetKwh = geraet.Kwh;
            double leistungKw = geraet.Kw;

            var d = new SpeicherParameterDaten
            {
                VarianteVorhanden = vorhanden,

                // W11b-B-28: Kapazitaet und Leistung sind aenderbar - aber nur bei GENAU
                // EINER Speicheranlage. Varianten desselben Speichers teilen sich EINE
                // Geraetekopie in Tab_Stromspeicher; dieselbe Grenze zieht auch
                // StromspeicherSimCtrl.UebernehmeAuslegung, ueber das hier geschrieben
                // wird.
                GeraetegroesseAenderbar = StromspeicherStammCtrl.AnlagenAnzahl(m_ID_Projekt) == 1,
                Variantenstatus = vorhanden
                    ? string.Format(MyResource.Resource.SP_PARAM_STATUS_VARIANTE, Variantenname(v))
                    : MyResource.Resource.SP_PARAM_STATUS_KEINE_VARIANTE,

                SoCMinProzent = v.SoC_Min_Prozent,
                SoCMaxProzent = v.SoC_Max_Prozent,
                SoCMinKwh = SoCText(v.SoC_Min_Prozent, kapazitaetKwh),
                SoCMaxKwh = SoCText(v.SoC_Max_Prozent, kapazitaetKwh),
                Ladeschwellwert = v.Ladeschwellwert,
                LadeleistungKw = leistungKw,
                KapazitaetKwh = kapazitaetKwh,

                Betriebsart = v.Betriebsart,

                // BENANNTE UMSETZUNG eines gespeicherten Standes: Trägt die Variante
                // die entfallene Berechnungsart, steht die Klappliste auf der
                // Dauernutzung statt auf nichts — und der Chip bzw. die Kopfzeile sagt
                // über SpeicherAnzeigeCtrl.BerechnungsartText, woher der Wert kommt.
                // Geschrieben wird erst, wenn der Anwender die Wahl selbst bestätigt.
                Berechnungsart = SpeicherAltstand.Berechnungsart(v.Berechnungsart),
                Betriebsarten = new[]
                {
                    new Steuerwahl(DbWerte.SP_BETRIEBSART_GRUENSTROM,
                                   MyResource.Resource.SP_BETRIEBSART_ANZEIGE_GRUENSTROM),
                    new Steuerwahl(DbWerte.SP_BETRIEBSART_GRAUSTROM,
                                   MyResource.Resource.SP_BETRIEBSART_ANZEIGE_GRAUSTROM)
                },
                Berechnungsarten = new[]
                {
                    new Steuerwahl(DbWerte.SP_BERECHNUNG_DAUERNUTZUNG,
                                   MyResource.Resource.SP_BERECHNUNG_ANZEIGE_DAUERNUTZUNG),
                    new Steuerwahl(DbWerte.SP_BERECHNUNG_ARBITRAGE,
                                   MyResource.Resource.SP_BERECHNUNG_ANZEIGE_ARBITRAGE),

                    // LS-E-1 (a): Die Lastspitzenkappung ist eine BERECHNUNGSART des
                    // Einzelspeichers - die eigene Maske bleibt daneben bestehen und
                    // schreibt ihr Ergebnis ueber „In Variante uebernehmen" hierher.
                    new Steuerwahl(DbWerte.SP_BERECHNUNG_PEAKSHAVING,
                                   MyResource.Resource.SP_BERECHNUNG_ANZEIGE_PEAKSHAVING)
                },

                PeakZiel = v.PeakZiel_kW.HasValue ? v.PeakZiel_kW.Value : 0.0,
                PeakZielAdaptiv = v.PeakZiel_Adaptiv,
                PeakZielMoeglich =
                    SpeicherAltstand.Berechnungsart(v.Berechnungsart) == DbWerte.SP_BERECHNUNG_PEAKSHAVING,
                PeakHerleitung = PeakHerleitung(),

                Kompatibilitaet = v.Kompatibilitaetsmodus,

                // Der Excel-Kompatibilitätsmodus gehört zur DAUERNUTZUNG — nur sie hat
                // eine Excel-Vorlage (Fachkonzept 5.2, dieselbe Regel, die
                // StromspeicherSimCtrl.BaueStrategie im Lauf anwendet). Die
                // Preissteuerung hat keine; dort bleibt der Schalter gesperrt.
                KompatibilitaetMoeglich =
                    SpeicherAltstand.Berechnungsart(v.Berechnungsart) == DbWerte.SP_BERECHNUNG_DAUERNUTZUNG,

                LadenAusPv = v.PV_Zulaessig,
                LadenAusBhkw = v.BHKW_Ueberschuss_Zulaessig,
                Netzentladung = v.Netzentladung,
                BhkwStromgefuehrt = v.BHKW_Stromgefuehrt,

                Kapitalzins = v.Kapitalzins,
                Nutzungsdauer = v.Nutzungsdauer,
                Leistungspreis = v.L_P,
                Netzladeaufschlag = v.A_Netzlade,

                Preisquelle = v.Preisquelle,
                Preisquellen = new[]
                {
                    new Steuerwahl(DbWerte.SP_PREISQUELLE_FIXPREIS,
                                   MyResource.Resource.PREIS_QUELLE_ANZEIGE_FIXPREIS),
                    new Steuerwahl(DbWerte.SP_PREISQUELLE_PROFIL,
                                   MyResource.Resource.PREIS_QUELLE_ANZEIGE_PROFIL),
                    new Steuerwahl(DbWerte.SP_PREISQUELLE_SPOTMARKT,
                                   MyResource.Resource.PREIS_QUELLE_ANZEIGE_SPOTMARKT)
                },
                Aufschlag = v.Aufschlag_Anwenden
            };

            Preisreihen(d, v);
            Preisinfo(d);
            return d;
        }

        /// <summary>
        /// Die HERLEITUNGSZEILE unter dem Peak-Ziel: die Netzbezugsspitze OHNE Speicher
        /// des letzten Laufs dieser Sitzung (LS-E-3). Leer, solange keiner gelaufen ist
        /// — dann steht dort nichts, statt eine Zahl zu erfinden.
        /// </summary>
        private string PeakHerleitung()
        {
            StromspeicherLaufKontext kontext = sim != null ? sim.Speicherkontext : null;
            if (kontext == null || !(kontext.PeakReferenzspitzeKw > 0.0)) return "";

            return string.Format(MyResource.Resource.SP_PARAM_HINWEIS_PEAKZIEL,
                                 kontext.PeakReferenzspitzeKw.ToString("N1", CultureInfo.CurrentCulture));
        }

        /// <summary>
        /// Anzeigename der Variante: der Bezeichner der Anlagenzeile, sonst deren ID.
        /// Die Variantentabelle führt keinen Namen (Fachkonzept 7.3).
        /// </summary>
        private static string Variantenname(StromspeicherVarianteModel v)
        {
            string name = WErzeugerCtrl.AnlagenBezeichner(v.ID_Energieanlage);
            if (!string.IsNullOrEmpty(name)) return name;
            return v.ID_Energieanlage.ToString(CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Das kWh-Äquivalent eines SoC-Prozentwerts (Abnahmebefund 1). Ohne
        /// Gerätekapazität bleibt die Zeile leer statt eine Zahl zu erfinden.
        /// </summary>
        private static string SoCText(double prozent, double kapazitaetKwh)
        {
            if (kapazitaetKwh <= 0) return "";
            return string.Format(MyResource.Resource.SP_PARAM_SOC_KWH,
                                 (kapazitaetKwh * prozent / 100.0).ToString("N1", CultureInfo.CurrentCulture));
        }

        /// <summary>
        /// Die Reihenauswahl passend zur Preisquelle: Spotreihen aus
        /// <c>Tab_Preisreihe</c>, Kostenprofile aus <c>Tab_Kostenprofil</c>. Beim
        /// Fixpreis ist die Liste leer und gesperrt (<c>SpReihenlisteFuellen</c> :6010).
        /// </summary>
        private void Preisreihen(SpeicherParameterDaten d, StromspeicherVarianteModel v)
        {
            var liste = new List<(int, string)>();
            d.PreisreiheLabel = MyResource.Resource.PREIS_PARAM_LABEL_REIHE;
            d.PreisreiheMoeglich = v.Preisquelle != DbWerte.SP_PREISQUELLE_FIXPREIS;

            try
            {
                if (v.Preisquelle == DbWerte.SP_PREISQUELLE_SPOTMARKT)
                {
                    foreach (PreisreiheModel p in new PreisreiheCtrl().ReadVerfuegbare(m_ID_Projekt))
                        liste.Add((p.ID, string.Format(MyResource.Resource.PREIS_PARAM_REIHE_EINTRAG,
                                                       p.Bezeichner, p.Jahr, p.Werteanzahl)));
                    d.PreisreiheId = v.ID_Preisreihe;
                }
                else if (v.Preisquelle == DbWerte.SP_PREISQUELLE_PROFIL)
                {
                    d.PreisreiheLabel = MyResource.Resource.PREIS_PARAM_LABEL_PROFIL;
                    foreach (KostenprofilModel p in new KostenprofilCtrl().ReadAllByProjekt(m_ID_Projekt))
                        liste.Add((p.ID, p.Bezeichner));
                    d.PreisreiheId = v.ID_Kostenprofil;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die Preisreihen konnten nicht gelesen werden: " + ex.Message);
            }

            d.Preisreihen = liste;
        }

        /// <summary>
        /// Die Preisvorschau — dieselbe Kette, die auch die Simulation durchläuft
        /// (<c>StromPreisCtrl</c>), damit auf dem Bildschirm keine zweite Preisrechnung
        /// steht.
        /// </summary>
        private void Preisinfo(SpeicherParameterDaten d)
        {
            try
            {
                StromPreisErgebnis p = new StromPreisCtrl().Baue(
                    m_ID_Projekt, _speicherVariante, SpeicherEngine.RasterAdapter.ViertelstundenJahr);

                CultureInfo k = CultureInfo.CurrentCulture;
                string text = string.Format(MyResource.Resource.PREIS_PARAM_INFO,
                                            p.EnergiepreisMittelCtKwh.ToString("0.###", k),
                                            p.AufschlagCtKwh.ToString("0.###", k),
                                            p.BezugspreisMittelCtKwh.ToString("0.###", k),
                                            p.Preisversion);

                if (!string.IsNullOrEmpty(p.Hinweis))
                    text += Environment.NewLine + p.Hinweis.Replace(Environment.NewLine, "  ");

                d.Preisinfo = text;
                d.PreisinfoWarnung = !string.IsNullOrEmpty(p.Hinweis);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die Preisvorschau konnte nicht gerechnet werden: " + ex.Message);
                d.Preisinfo = "";
            }
        }

        /// <summary>
        /// Die Reihenauswahl zu EINER Preisquelle (W11b‑B‑28) — derselbe Auszug, den
        /// <see cref="Preisreihen"/> beim Lesen macht, aber OHNE zu schreiben.
        /// </summary>
        /// <remarks>
        /// Der gepufferte Parameterblock wechselt die Preisquelle, ohne die Variante
        /// anzufassen; die Reihenliste und ihre Beschriftung müssen trotzdem sofort
        /// umspringen (aus „Spotmarkt" wird „Preisreihe", aus „Profil" wird
        /// „Kostenprofil", beim Fixpreis ist die Liste leer und gesperrt). Gelesen wird
        /// dabei die AKTUELLE Variante nur für die vorbelegte Id — sie bleibt stehen,
        /// solange der Anwender nicht speichert.
        /// </remarks>
        private SpeicherPreisreihenDaten SpeicherPreisreihen(string quelle)
        {
            StromspeicherVarianteModel v = _speicherVariante ?? new StromspeicherVarianteModel();

            // Preisreihen(d, v) liest die Quelle AUS DER VARIANTE; hier gilt die
            // gewaehlte. Eine Kopie waere ein zweiter Zustand daneben - stattdessen
            // wird das eine Feld gesetzt, gelesen und wieder zurueckgestellt.
            string vorher = v.Preisquelle;
            var d = new SpeicherParameterDaten();
            try
            {
                v.Preisquelle = quelle;
                Preisreihen(d, v);
            }
            finally
            {
                v.Preisquelle = vorher;
            }

            return new SpeicherPreisreihenDaten
            {
                Label = d.PreisreiheLabel,
                Moeglich = d.PreisreiheMoeglich,
                Reihen = d.Preisreihen,
                Id = d.PreisreiheId
            };
        }

        /// <summary>
        /// <b>Ein Feld der Speicherparameter schreiben — SOFORT</b> (W11b‑B‑29,
        /// Anwenderentscheid 1 vom 10.09.2026: „Sofort schreiben, wie überall sonst im
        /// Programm."). Die Rückmeldung sagt, was daraus geworden ist.
        /// </summary>
        /// <remarks>
        /// <para><b>Erst prüfen, dann schreiben.</b> Die Regeln stehen als reine
        /// Funktionen im Kern (<see cref="SpeicherParameterPruefung"/>) und sind dort
        /// geprüft; geprüft wird die Regel, die zu DIESEM Feld gehört — das SoC-Band
        /// gegen den jeweils anderen GESPEICHERTEN Wert, die Gerätegröße gegen den
        /// anderen Gerätewert, die vier Wirtschaftswerte je für sich. Ein Verstoß weist
        /// ab, und es geht keine Zeile in die Datenbank.</para>
        /// <para><b>Zwei Ziele.</b> Die Betriebsführung geht in
        /// <c>Tab_StromspeicherVariante</c> (ein <c>Update</c> je Tastendruck, wie im
        /// Vorläufer <c>SpeichereVariantenAenderung</c> :6379), die Gerätegröße in
        /// <c>Tab_Stromspeicher</c> — und die über denselben Weg, den die
        /// Auslegungsoptimierung nimmt (<c>StromspeicherSimCtrl.UebernehmeAuslegung</c>),
        /// samt seiner Wache „genau eine SP-Anlage" und seinem <c>LetzterHinweis</c>.</para>
        /// <para><b>Kein stummer Fehlschlag mehr</b> (Befund W11b‑B‑29): <c>Update</c>
        /// liefert <c>bool</c>, und <c>false</c> wie jede Ausnahme wird zu einer
        /// Abweisung. Bis dahin wurde der Rückgabewert weggeworfen, und der Block meldete
        /// „gespeichert", obwohl nichts geschrieben war.</para>
        /// <para><b>Nicht neu gerechnet.</b> Wörtlich wie überall sonst: Die Meldung sagt,
        /// dass der nächste Lauf mit diesen Werten rechnet; wann er läuft, entscheidet
        /// der Anwender.</para>
        /// </remarks>
        private EPOS.UI.Seiten.Simulation.Rueckmeldung SpeicherfeldSchreiben(
            string feld, string wert)
        {
            if (_speicherVariante == null)
                return new EPOS.UI.Seiten.Simulation.Rueckmeldung(
                    false, MyResource.Resource.SP_PARAM_MSG_KEINE_VARIANTE);

            double zahl;
            double.TryParse(wert, NumberStyles.Float, CultureInfo.InvariantCulture, out zahl);
            bool ja = wert == "1";

            // DIE GERAETEGROESSE geht in die ANLAGE und nicht in die Variante; den
            // jeweils anderen Wert holt sie sich, weil UebernehmeAuslegung beide
            // zusammen schreibt.
            if (feld == SpeicherFeld.Kapazitaet || feld == SpeicherFeld.Leistung)
                return GeraetegroesseSchreiben(feld, zahl);

            string fehler = FeldPruefen(feld, zahl);
            if (fehler != null)
                return new EPOS.UI.Seiten.Simulation.Rueckmeldung(false, fehler);

            Action<StromspeicherVarianteModel> aenderung = null;
            switch (feld)
            {
                case SpeicherFeld.SoCMin: aenderung = v => v.SoC_Min_Prozent = zahl; break;
                case SpeicherFeld.SoCMax: aenderung = v => v.SoC_Max_Prozent = zahl; break;
                case SpeicherFeld.Ladeschwelle: aenderung = v => v.Ladeschwellwert = zahl; break;
                case SpeicherFeld.Betriebsart: aenderung = v => v.Betriebsart = wert; break;
                case SpeicherFeld.Berechnungsart: aenderung = v => v.Berechnungsart = wert; break;

                // SCHEMASCHRITT 86. Die 0 bleibt eine 0 und wird NICHT zu NULL: Wer das
                // Feld leert, sagt "kein Ziel", und genau das liest der Lauf als
                // Ruckfall auf die Dauernutzung (benannt im Protokoll).
                case SpeicherFeld.PeakZiel:
                    aenderung = v => v.PeakZiel_kW = zahl > 0.0 ? (double?)zahl : null;
                    break;
                case SpeicherFeld.PeakZielAdaptiv: aenderung = v => v.PeakZiel_Adaptiv = ja; break;
                case SpeicherFeld.Kompatibilitaet: aenderung = v => v.Kompatibilitaetsmodus = ja; break;
                case SpeicherFeld.LadenPv: aenderung = v => v.PV_Zulaessig = ja; break;
                case SpeicherFeld.LadenBhkw: aenderung = v => v.BHKW_Ueberschuss_Zulaessig = ja; break;
                case SpeicherFeld.Netzentladung: aenderung = v => v.Netzentladung = ja; break;
                case SpeicherFeld.Kapitalzins: aenderung = v => v.Kapitalzins = zahl; break;
                case SpeicherFeld.Nutzungsdauer: aenderung = v => v.Nutzungsdauer = zahl; break;
                case SpeicherFeld.Leistungspreis: aenderung = v => v.L_P = zahl; break;
                case SpeicherFeld.Netzladeaufschlag: aenderung = v => v.A_Netzlade = zahl; break;
                case SpeicherFeld.Aufschlag: aenderung = v => v.Aufschlag_Anwenden = ja; break;
                case SpeicherFeld.Preisquelle: aenderung = v => v.Preisquelle = wert; break;
                case SpeicherFeld.Preisreihe:
                    aenderung = v =>
                    {
                        if (v.Preisquelle == DbWerte.SP_PREISQUELLE_SPOTMARKT) v.ID_Preisreihe = (int)zahl;
                        else if (v.Preisquelle == DbWerte.SP_PREISQUELLE_PROFIL) v.ID_Kostenprofil = (int)zahl;
                    };
                    break;
            }

            // Ein unbekannter Schluessel ist ein Programmierfehler, kein Anwenderfall -
            // er meldet nichts und schreibt nichts.
            if (aenderung == null) return EPOS.UI.Seiten.Simulation.Rueckmeldung.Still;

            bool ok;
            try
            {
                aenderung(_speicherVariante);
                ok = new StromspeicherVarianteCtrl().Update(_speicherVariante);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die Speichervariante konnte nicht geschrieben werden: " + ex.Message);
                return new EPOS.UI.Seiten.Simulation.Rueckmeldung(
                    false, MyResource.Resource.SP_PARAM_MSG_FEHLER);
            }

            if (!ok)
                return new EPOS.UI.Seiten.Simulation.Rueckmeldung(
                    false, MyResource.Resource.SP_PARAM_MSG_FEHLER);

            return new EPOS.UI.Seiten.Simulation.Rueckmeldung(
                true, MyResource.Resource.SP_PARAM_MSG_GESPEICHERT);
        }

        /// <summary>
        /// Die Regel, die zu EINEM Feld gehört — <c>null</c> heißt „in Ordnung"
        /// (W11b‑B‑29). Felder ohne Zahlenregel (Betriebsart, Schalter, Preisquelle,
        /// Preisreihe) haben keine.
        /// </summary>
        /// <remarks>
        /// Das SoC-Band braucht beide Kanten: Wer eine eingibt, gibt sie gegen die andere
        /// ein, wie sie in der Variante steht — deshalb kommt der Gegenwert aus
        /// <c>_speicherVariante</c> und nicht aus der Oberfläche.
        /// </remarks>
        private string FeldPruefen(string feld, double zahl)
        {
            switch (feld)
            {
                case SpeicherFeld.SoCMin:
                    return SpeicherParameterPruefung.SoCBand(zahl, _speicherVariante.SoC_Max_Prozent);
                case SpeicherFeld.SoCMax:
                    return SpeicherParameterPruefung.SoCBand(_speicherVariante.SoC_Min_Prozent, zahl);
                case SpeicherFeld.Kapitalzins:
                case SpeicherFeld.Nutzungsdauer:
                case SpeicherFeld.Leistungspreis:
                case SpeicherFeld.Netzladeaufschlag:
                case SpeicherFeld.PeakZiel:
                    return SpeicherParameterPruefung.NichtNegativ(zahl);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Kapazität oder Leistung in die Speicheranlage schreiben (W11b‑B‑29) — über
        /// denselben Weg wie die Auslegungsoptimierung.
        /// </summary>
        /// <remarks>
        /// <para><c>UebernehmeAuslegung</c> schreibt BEIDE Werte zusammen; der nicht
        /// eingegebene kommt deshalb aus <c>StromspeicherStammCtrl.KapazitaetUndLeistung</c>,
        /// also aus der Zeile, die gerade dort steht.</para>
        /// <para>Die Wache „genau eine SP-Anlage im Projekt" liegt in
        /// <c>UebernehmeAuslegung</c> selbst und begründet sich dort: Varianten desselben
        /// Speichers teilen sich EINE Gerätekopie. Ihr Grund steht im
        /// <c>LetzterHinweis</c> und wird hier zur Rückmeldung — die Oberfläche sperrt
        /// die beiden Felder ohnehin schon (<c>GeraetegroesseAenderbar</c>).</para>
        /// </remarks>
        private EPOS.UI.Seiten.Simulation.Rueckmeldung GeraetegroesseSchreiben(
            string feld, double zahl)
        {
            var geraet = StromspeicherStammCtrl.KapazitaetUndLeistung(
                m_ID_Projekt, _speicherVariante.ID_Energieanlage);

            double kwh = feld == SpeicherFeld.Kapazitaet ? zahl : geraet.Kwh;
            double kw = feld == SpeicherFeld.Leistung ? zahl : geraet.Kw;

            string fehler = SpeicherParameterPruefung.Geraet(kwh, kw);
            if (fehler != null)
                return new EPOS.UI.Seiten.Simulation.Rueckmeldung(false, fehler);

            StromspeicherSimCtrl simCtrl = new StromspeicherSimCtrl();
            bool ok;
            try
            {
                ok = simCtrl.UebernehmeAuslegung(m_ID_Projekt, kwh, kw);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die Geraetegroesse konnte nicht geschrieben werden: " + ex.Message);
                return new EPOS.UI.Seiten.Simulation.Rueckmeldung(
                    false, MyResource.Resource.SP_PARAM_MSG_FEHLER);
            }

            if (!ok)
                return new EPOS.UI.Seiten.Simulation.Rueckmeldung(
                    false, string.IsNullOrEmpty(simCtrl.LetzterHinweis)
                        ? MyResource.Resource.OPT_MSG_UEBERNAHME_FEHLER
                        : simCtrl.LetzterHinweis);

            return new EPOS.UI.Seiten.Simulation.Rueckmeldung(
                true, MyResource.Resource.SP_PARAM_MSG_GESPEICHERT);
        }

        // =================================================================
        // Der Lauf (W11a.4)
        // =================================================================

        /// <summary>
        /// Der Simulationslauf. Vorprüfen, Bedarf und Bestücken LESEN die Datenbank und
        /// bleiben auf dem Bedienfaden; nur <c>SimulationLaufCtrl.Laufen</c> geht in
        /// <c>Task.Run</c> — die Aufteilung aus <c>Form_SpeicherOptimierung</c>.
        /// </summary>
        private async Task<EPOS.UI.Seiten.Simulation.Rueckmeldung> Laufen(
            Action<double?, string> melder)
        {
            // NACHARBEIT PAKET 8, BEFUND N1: ZUERST - ab hier ist das angezeigte
            // Ergebnis nicht mehr gültig, und jeder Frühausstieg lässt „Ergebnis
            // speichern" gesperrt zurück. Seit #236 merkt sich die Hülle dabei AUCH
            // den Grund: Jeder Frühausstieg geht über Abbruch(...).
            ZustandSetzen(ErgebnisZustand.Abgebrochen, "");

            string sperrgrund;
            if (SchemaStand.SimulationGesperrt(out sperrgrund))
                return Abbruch(sperrgrund);

            // PAKET 8 (Konzept 13.4): EIN Protokollkanal je Lauf, angelegt VOR der
            // Bedarfsrechnung.
            SimulationProtokoll.NeuStarten();

            ctrl.ProjektLesen(m_ID_Projekt);
            if (ctrl.rows == 0)
                return Abbruch(MyResource.Resource.SIM_MSG_KONFIGURATION_FEHLT);

            projektCtrl.ReadSingle(m_ID_Projekt);
            int idKlimaregion = projektCtrl.m_ID_Klimaregion;

            // ÜBERGEBEN WIRD "ctrl" SELBST, nicht "ctrl.model" - wörtlich wie im
            // Vorläufer (:4132-4136, offener Punkt W11a-O-5).
            string fehler = SimulationLaufCtrl.Vorpruefen(m_ID_Projekt, ctrl, idKlimaregion);
            if (fehler != null) return Abbruch(fehler);

            string bedarfsfehler = SimulationLaufCtrl.Bedarf(
                m_ID_Projekt, idKlimaregion,
                ctrl.m_Netzverluste, ctrl.m_szNetzverlusteEinheit,
                _waermebedarf, _strombedarf);

            // Der Bedarf ist gerechnet - die Vorabrechnung aus BedarfSicherstellen
            // braucht es danach nicht mehr (#236).
            _bedarfGerechnet = true;

            if (bedarfsfehler != null) return Abbruch(Mitkanal(bedarfsfehler));

            // Erst ein vollständig erfolgreicher Lauf ersetzt die vorherige Anzeige.
            // Die Flotte wird am Speicherzweig aus den frisch gerechneten Quellen
            // vorbereitet; damit funktioniert derselbe Weg auch beim ersten Öffnen.
            var neuerLauf = new SimulationControl();
            try
            {
                var eingaben = OptimierungVorgaben().Eingaben.Kopie();
                if (eingaben.Auslegung.Flotte?.Einheiten?.Count > 0 &&
                    !eingaben.Auslegung.FlottenProjektbetriebDeaktiviert)
                {
                    eingaben.Auslegung.FlottenGroessenOptimieren = false;
                    neuerLauf.SpeicherflottenEingaben = eingaben;
                }
            }
            catch (Exception ex)
            {
                return Abbruch("Die Speicher-Einstellungen konnten nicht vorbereitet werden: " + ex.Message);
            }

            SimulationLaufCtrl.Bestuecken(neuerLauf, m_ID_Projekt, Tools(),
                                          _waermebedarf, _strombedarf, ctrl,
                                          _grenzleistungBhkw, _bhkwBetriebsart);

            _laufAbbruch = new CancellationTokenSource();
            CancellationToken marke = _laufAbbruch.Token;
            IProgress<LaufFortschritt> fortschritt = new Progress<LaufFortschritt>(
                f => melder(f != null ? f.Anteil : (double?)null, Phasentext(f)));

            try
            {
                await SpeicherEngine.Kulturweitergabe.Starten(
                    () => SimulationLaufCtrl.Laufen(neuerLauf, m_ID_Projekt, fortschritt, marke), marke);
            }
            catch (OperationCanceledException)
            {
                return Abbruch("");
            }
            catch (Exception ex)
            {
                return Abbruch(ex.Message);
            }
            finally
            {
                CancellationTokenSource quelle = _laufAbbruch;
                _laufAbbruch = null;
                if (quelle != null) quelle.Dispose();
            }

            string abbruch = SimulationLaufCtrl.Abbruchgrund(neuerLauf);
            if (abbruch != null) return Abbruch(abbruch);

            if (neuerLauf.SpeicherflottenEingaben is not null && neuerLauf.Speicherflottenlauf is { } flotte)
            {
                try
                {
                    SpeicherFlottenProjektCtrl.Aktivieren(m_ID_Projekt, new SpeicherFlottenErgebnis
                    {
                        Erfolg = true, Eingaben = flotte.Eingaben,
                        Konfiguration = flotte.Konfiguration, Studie = flotte.Studie
                    });
                }
                catch (Exception ex)
                {
                    return Abbruch("Die gerechnete Speicherflotte konnte nicht übernommen werden: " + ex.Message);
                }
            }
            sim = neuerLauf;

            // Erst JETZT ist ein Ergebnis da, das gespeichert werden darf (Befund N1).
            ZustandSetzen(ErgebnisZustand.Gueltig, "");

            // Der Stand, gegen den VeraltungPruefen spaeter haelt - NACH dem Lauf gelesen,
            // damit alles, was der Lauf selbst geschrieben hat, dazugehoert.
            _aenderungsdatumBeimLauf = MerkmalUebernahmeCtrl.Aenderungsdatum(m_ID_Projekt);
            _laufGerechnet = true;
            _flotteProjektGeaendert = false;
            _autarkieGesetzt = false;

            // DER LAUF SPEICHERT SELBST (Anwenderbefund 22.09.2026).
            //
            // Bis hierher endete die Kachel „Simulation“ mit einem gerechneten, aber
            // UNGESPEICHERTEN Ergebnis: Wer danach nicht eigens „Ergebnis speichern“
            // drückte, sah in Übersicht, Bericht und Wirtschaftlichkeit weiter den alten
            // Lauf — während der Weg über die Übersichtsseite
            // (SimulationRunner.SimuliereUndSpeichere) längst in EINEM Zug rechnete und
            // speicherte. Zwei Wege, dieselbe Rechnung, verschiedene Folgen.
            //
            // DER KNOPF BLEIBT — er schreibt ein stehendes Ergebnis erneut fest.
            // SCHEITERT das Speichern, bleibt der Zustand GÜLTIG (gerechnet ist
            // gerechnet), und die benannte Meldung geht an die Seite;
            // ErgebnisSpeichern prüft ErgebnisIstGueltig selbst.
            return ErgebnisSpeichern();
        }

        private static string Phasentext(LaufFortschritt f)
            => f == null ? "" : (f.Text ?? "");

        private static string Mitkanal(string grund)
        {
            string zusatz = SimulationProtokoll.Aktuell.FehlertextFuerAnzeige(grund);
            if (string.IsNullOrEmpty(zusatz)) return grund;

            return grund + Environment.NewLine + Environment.NewLine +
                   MyResource.Resource.SIM_MSG_WEITERE_FEHLERMELDUNGEN + Environment.NewLine + zusatz;
        }

        private void Abbrechen()
        {
            CancellationTokenSource quelle = _laufAbbruch;
            if (quelle != null && !quelle.IsCancellationRequested) quelle.Cancel();
        }

        private EPOS.UI.Seiten.Simulation.Rueckmeldung ErgebnisSpeichern()
        {
            if (m_ID_Projekt <= 0)
                return new EPOS.UI.Seiten.Simulation.Rueckmeldung(
                    false, MyResource.Resource.SIM_MSG_KEIN_PROJEKT);

            if (!ErgebnisIstGueltig)
                return new EPOS.UI.Seiten.Simulation.Rueckmeldung(
                    false, MyResource.Resource.SIM_MSG_KEIN_VOLLSTAENDIGES_ERGEBNIS);

            bool ok = SimulationLaufCtrl.ErgebnisSpeichern(
                m_ID_Projekt, _waermebedarf, _strombedarf, sim);

            // iU9-W16b.1 (E-7, K6-a): Hier stand ein Auffrischen der
            // Stromspeicherliste im Detailformular (Program.mainfrm.SetSPControl,
            // wörtlich :3748-3749, in try/catch, weil das Fenster geschlossen sein
            // konnte). FormMain ist gelöscht; die Startseite liest ihren Bestand beim
            // nächsten Zeichnen ohnehin neu.

            return new EPOS.UI.Seiten.Simulation.Rueckmeldung(
                ok,
                ok ? MyResource.Resource.SIM_MSG_ERGEBNIS_GESPEICHERT
                   : MyResource.Resource.SIM_MSG_ERGEBNIS_NICHT_GESPEICHERT);
        }
    }
}
