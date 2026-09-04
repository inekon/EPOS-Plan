using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Bausteine;
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
    /// fertige PNG. Der LAUF läuft in <c>Task.Run</c>, meldet Phasen über
    /// <c>IProgress&lt;LaufFortschritt&gt;</c> und nimmt einen
    /// <c>CancellationToken</c> — die Aufteilung ist die aus
    /// <c>Form_SpeicherOptimierung</c>: Vorprüfen, Bedarf und Bestücken lesen die
    /// Datenbank und bleiben auf dem Bedienfaden.</para>
    ///
    /// <para><b>Bilder erst auf Anforderung.</b> Zwölf PNG je Lauf im Voraus zu rechnen
    /// wäre zu teuer; die Seite fragt je Reiter und Schalterstellung nach und hält das
    /// Ergebnis in ihrem Zwischenspeicher.</para>
    /// </summary>
    internal sealed partial class SimulationErgebnisHuelle
    {
        /// <summary>Wunschgröße des Fensters — die des Vorläufers (<c>ClientSize</c>).</summary>
        private static readonly Size MASS = new Size(1474, 821);

        // =================================================================
        // Öffnen
        // =================================================================

        /// <summary>
        /// Zeigt die Ergebnisseite in einem modalen Fenster (R-W11-1). Der Aufrufer
        /// liest nichts zurück — das tat auch der Vorläufer nicht (Befund W11-B26:
        /// <c>SetControls()</c> war leer und wurde trotzdem gerufen).
        /// </summary>
        /// <param name="waermebedarf">
        /// Das Wärmebedarfsobjekt des Aufrufers — es wird hier WEITERGESCHRIEBEN und
        /// dort für die Kachelbeschriftungen weiterverwendet (Befund W11-B3).
        /// </param>
        internal static void Oeffnen(IWin32Window besitzer, int idProjekt,
                                     SimulationWaermebedarf waermebedarf,
                                     SimulationStrombedarf strombedarf)
        {
            SimulationErgebnisHuelle huelle =
                new SimulationErgebnisHuelle(idProjekt, waermebedarf, strombedarf);

            BlazorDialogForm<SimulationErgebnisSeite> dlg = null;

            var werte = new Dictionary<string, object>(huelle.Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create(
                    new object(), () => { if (dlg != null) dlg.Schliessen(true); })
            };

            dlg = new BlazorDialogForm<SimulationErgebnisSeite>(
                MyResource.Resource.SIMERG_TITEL, MASS, werte);

            using (dlg)
            {
                huelle._fenster = dlg;

                // Bereich für den KI-Hilfe-Assistenten melden - wörtlich wie im
                // Vorläufer (:436, Befund W11-B5: dort stand der deutsche Literaltext
                // NEBEN dem Katalogeintrag HilfeKontext.cs:154; jetzt gibt es ihn
                // einmal).
                dlg.Activated += delegate { HilfeKontext.SetzeBereich("Detaillierte Simulation"); };

                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
        }

        // =================================================================
        // Zustand
        // =================================================================

        private readonly int m_ID_Projekt;
        private readonly SimulationWaermebedarf _waermebedarf;
        private readonly SimulationStrombedarf _strombedarf;

        private readonly SimulationControl sim = new SimulationControl();
        private readonly KonfigurationCtrl ctrl = new KonfigurationCtrl();
        private readonly ProjektCtrl projektCtrl = new ProjektCtrl();

        private readonly EPOS.UI.Dienste.SeitenZustand _zustand =
            new EPOS.UI.Dienste.SeitenZustand();

        private Form _fenster;

        /// <summary>Die aktive Speichervariante — die Parameterseite bearbeitet sie.</summary>
        private StromspeicherVarianteModel _speicherVariante;

        /// <summary>Zustandsmaschine „Ergebnis speichern" (Nacharbeit Paket 8, Befund N1).</summary>
        private bool _ergebnisGueltig;

        /// <summary>Abbruchmarke des laufenden Simulationslaufs; <c>null</c> = kein Lauf.</summary>
        private CancellationTokenSource _laufAbbruch;

        /// <summary>Die BHKW-Betriebsart der Bedienelemente (0/1/2).</summary>
        private int _bhkwBetriebsart;

        /// <summary>Die unterste Leistungsgrenze der BHKW-Module.</summary>
        private int _grenzleistungBhkw = 30;

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
            _grenzleistungBhkw = ctrl.model != null ? (int)ctrl.model.m_BHKW_Grenzleistung : 30;

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
                Bild = Bild,

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
                    _grenzleistungBhkw = wert;
                    KonfigSchreiben(m => m.m_BHKW_Grenzleistung = wert);
                },
                HeizstabSchreiben = wert => KonfigSchreiben(m => m.m_WP_Heizstab = wert),
                BereitschaftSchreiben = wert =>
                    KonfigSchreiben(m => m.m_Kessel_Betriebsbereitschaft = (int)wert),

                SpeicherfeldSchreiben = SpeicherfeldSchreiben,

                KonfigurationGaben = () => SimulationKonfigHuelle.Gaben(m_ID_Projekt),
                BedarfGaben = waerme => waerme
                    ? BedarfErgebnisHuelle.Gaben(_waermebedarf, true, 1, "")
                    : BedarfErgebnisHuelle.Gaben(_strombedarf, 0),

                WaermepumpenGaben = WaermepumpenGaben,
                WaermepumpenFertig = WaermepumpenFertig,

                VergleichRechnen = VergleichRechnen,
                VarianteAktivSetzen = VarianteAktivSetzen,
                VergleichCsv = VergleichCsv,

                Sprung = schluessel => Sprungbruecke.Fuer(_fenster, sim, m_ID_Projekt)(schluessel),

                CsvBedarf = CsvBedarf,
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

        private SimulationErgebnisDaten Laden(int idProjekt)
        {
            SimulationErgebnisDaten d = new SimulationErgebnisDaten { IdProjekt = idProjekt };

            // Blockade bei nicht abgeschlossener Schema-Migration (ADR-001) - wörtlich
            // aus SimulationBlockiert :3406-3419; nur meldet sie hier als Banner statt
            // als MessageBox.
            string sperrgrund;
            d.Gesperrt = SchemaMigration.SimulationGesperrt(out sperrgrund);
            d.Sperrgrund = d.Gesperrt ? sperrgrund : "";

            ctrl.ProjektLesen(idProjekt);
            string[] tool = Tools();

            d.Parameter = ParameterDaten(tool);
            d.ErgebnisGueltig = _ergebnisGueltig && !d.Gesperrt;

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
            var bedarf = SimulationErgebnisCtrl.Bedarf(_waermebedarf, _strombedarf);
            d.Bedarf = BedarfDaten(bedarf);

            if (!_ergebnisGueltig) return d;

            ErgebnisPraesenz p = ErgebnisPraesenz.Ermitteln(sim);

            d.Kennzahlen = SimulationErgebnisCtrl.Uebersicht(sim, _waermebedarf, _strombedarf);
            d.Uebersicht = UebersichtDaten(d.Kennzahlen, p);

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

            if (d.Waermepumpe != null)
            {
                d.ErdreichHinweise = d.Waermepumpe.ErdreichHinweise;
                d.ErdreichWarnung = d.Waermepumpe.ErdreichWarnung;
            }
            d.Speichertemperaturen = Temperaturreihen().Count > 0;

            d.Speicher = SpeicherDaten();
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
        // Die Parameterseite
        // =================================================================

        private ParameterDaten ParameterDaten(string[] tool)
        {
            var blaetter = new List<string> { ParameterBlatt.Bedarf };

            // Die Reihenfolge ist die von Tool_1..6 - wörtlich (UpdateTabPages :2848).
            foreach (string t in tool)
            {
                if (string.IsNullOrEmpty(t)) continue;
                string schluessel = BlattZuTool(t.Trim());
                if (schluessel != null && !blaetter.Contains(schluessel)) blaetter.Add(schluessel);
            }

            KonfigurationModel m = ctrl.model ?? new KonfigurationModel();

            return new ParameterDaten
            {
                Unterblaetter = blaetter,
                Netzverluste = m.m_Netzverluste,
                NetzverlusteEinheit = string.IsNullOrEmpty(m.m_szNetzverlusteEinheit)
                    ? "%" : m.m_szNetzverlusteEinheit,
                Betriebsart = _bhkwBetriebsart,
                UntersteLeistungsgrenze = _grenzleistungBhkw,
                Heizstab = m.m_WP_Heizstab,
                Bereitschaft = m.m_Kessel_Betriebsbereitschaft,
                Speicher = SpeicherParameter()
            };
        }

        private static string BlattZuTool(string tool)
        {
            if (tool == DbWerte.ERZEUGER_BHKW) return ParameterBlatt.Bhkw;
            if (tool == DbWerte.ERZEUGER_STROMSPEICHER) return ParameterBlatt.Stromspeicher;
            if (tool == DbWerte.ERZEUGER_WAERMEPUMPE) return ParameterBlatt.Waermepumpe;
            if (tool == DbWerte.ERZEUGER_HEIZKESSEL) return ParameterBlatt.Heizkessel;
            return null;
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

        private void VarianteLesen()
        {
            try
            {
                _speicherVariante = new StromspeicherVarianteCtrl().ReadAktiveVariante(m_ID_Projekt);
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
                Berechnungsart = v.Berechnungsart,
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
                    new Steuerwahl(DbWerte.SP_BERECHNUNG_NACHTNUTZUNG,
                                   MyResource.Resource.SP_BERECHNUNG_ANZEIGE_NACHTNUTZUNG),
                    new Steuerwahl(DbWerte.SP_BERECHNUNG_ARBITRAGE,
                                   MyResource.Resource.SP_BERECHNUNG_ANZEIGE_ARBITRAGE)
                },

                Kompatibilitaet = v.Kompatibilitaetsmodus,
                KompatibilitaetMoeglich = v.Berechnungsart == DbWerte.SP_BERECHNUNG_NACHTNUTZUNG,

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
        /// Ein Feld der Speichervariante schreiben — sofort, wie im Vorläufer
        /// (<c>SpeichereVariantenAenderung</c> :6379). Eine Fehleingabe kommt gar nicht
        /// erst an: Das Zahlenfeld färbt und meldet nichts nach außen.
        /// </summary>
        private void SpeicherfeldSchreiben(string feld, string wert)
        {
            if (_speicherVariante == null) return;

            double zahl;
            double.TryParse(wert, NumberStyles.Float, CultureInfo.InvariantCulture, out zahl);
            bool ja = wert == "1";

            Action<StromspeicherVarianteModel> aenderung = null;
            switch (feld)
            {
                case SpeicherFeld.SoCMin: aenderung = v => v.SoC_Min_Prozent = zahl; break;
                case SpeicherFeld.SoCMax: aenderung = v => v.SoC_Max_Prozent = zahl; break;
                case SpeicherFeld.Ladeschwelle: aenderung = v => v.Ladeschwellwert = zahl; break;
                case SpeicherFeld.Betriebsart: aenderung = v => v.Betriebsart = wert; break;
                case SpeicherFeld.Berechnungsart: aenderung = v => v.Berechnungsart = wert; break;
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

            if (aenderung == null) return;

            try
            {
                aenderung(_speicherVariante);
                new StromspeicherVarianteCtrl().Update(_speicherVariante);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die Speichervariante konnte nicht geschrieben werden: " + ex.Message);
            }
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
            // speichern" gesperrt zurück.
            _ergebnisGueltig = false;

            string sperrgrund;
            if (SchemaMigration.SimulationGesperrt(out sperrgrund))
                return new EPOS.UI.Seiten.Simulation.Rueckmeldung(false, sperrgrund);

            // PAKET 8 (Konzept 13.4): EIN Protokollkanal je Lauf, angelegt VOR der
            // Bedarfsrechnung.
            SimulationProtokoll.NeuStarten();

            ctrl.ProjektLesen(m_ID_Projekt);
            if (ctrl.rows == 0)
                return new EPOS.UI.Seiten.Simulation.Rueckmeldung(
                    false, MyResource.Resource.SIM_MSG_KONFIGURATION_FEHLT);

            projektCtrl.ReadSingle(m_ID_Projekt);
            int idKlimaregion = projektCtrl.m_ID_Klimaregion;

            // ÜBERGEBEN WIRD "ctrl" SELBST, nicht "ctrl.model" - wörtlich wie im
            // Vorläufer (:4132-4136, offener Punkt W11a-O-5).
            string fehler = SimulationLaufCtrl.Vorpruefen(m_ID_Projekt, ctrl, idKlimaregion);
            if (fehler != null) return new EPOS.UI.Seiten.Simulation.Rueckmeldung(false, fehler);

            string bedarfsfehler = SimulationLaufCtrl.Bedarf(
                m_ID_Projekt, idKlimaregion,
                ctrl.m_Netzverluste, ctrl.m_szNetzverlusteEinheit,
                _waermebedarf, _strombedarf);

            if (bedarfsfehler != null)
                return new EPOS.UI.Seiten.Simulation.Rueckmeldung(false, Mitkanal(bedarfsfehler));

            SimulationLaufCtrl.Bestuecken(sim, m_ID_Projekt, Tools(),
                                          _waermebedarf, _strombedarf, ctrl,
                                          _grenzleistungBhkw, _bhkwBetriebsart);

            _laufAbbruch = new CancellationTokenSource();
            CancellationToken marke = _laufAbbruch.Token;
            IProgress<LaufFortschritt> fortschritt = new Progress<LaufFortschritt>(
                f => melder(f != null ? f.Anteil : (double?)null, Phasentext(f)));

            try
            {
                await Task.Run(() => SimulationLaufCtrl.Laufen(sim, m_ID_Projekt, fortschritt, marke),
                               marke);
            }
            catch (OperationCanceledException)
            {
                return new EPOS.UI.Seiten.Simulation.Rueckmeldung(false, "");
            }
            catch (Exception ex)
            {
                return new EPOS.UI.Seiten.Simulation.Rueckmeldung(false, ex.Message);
            }
            finally
            {
                CancellationTokenSource quelle = _laufAbbruch;
                _laufAbbruch = null;
                if (quelle != null) quelle.Dispose();
            }

            string abbruch = SimulationLaufCtrl.Abbruchgrund(sim);
            if (abbruch != null) return new EPOS.UI.Seiten.Simulation.Rueckmeldung(false, abbruch);

            // Erst JETZT ist ein Ergebnis da, das gespeichert werden darf (Befund N1).
            _ergebnisGueltig = true;
            _autarkieGesetzt = false;

            return EPOS.UI.Seiten.Simulation.Rueckmeldung.Still;
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

            if (!_ergebnisGueltig)
                return new EPOS.UI.Seiten.Simulation.Rueckmeldung(
                    false, MyResource.Resource.SIM_MSG_KEIN_VOLLSTAENDIGES_ERGEBNIS);

            bool ok = SimulationLaufCtrl.ErgebnisSpeichern(
                m_ID_Projekt, _waermebedarf, _strombedarf, sim);

            if (ok)
            {
                // Die Kacheln der Startmaske auffrischen - wörtlich :3748-3749, in
                // try/catch, weil die Startmaske geschlossen sein kann.
                try
                {
                    projektCtrl.ReadSingle(m_ID_Projekt);
                    Program.mainfrm.SetSPControl(projektCtrl.m_szProjektname);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Die Startmaske konnte nicht aufgefrischt werden: " + ex.Message);
                }
            }

            return new EPOS.UI.Seiten.Simulation.Rueckmeldung(
                ok,
                ok ? MyResource.Resource.SIM_MSG_ERGEBNIS_GESPEICHERT
                   : MyResource.Resource.SIM_MSG_ERGEBNIS_NICHT_GESPEICHERT);
        }
    }
}
