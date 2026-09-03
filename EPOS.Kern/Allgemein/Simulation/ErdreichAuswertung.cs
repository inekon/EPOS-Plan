using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ergebnisanbindung der VDI-4640-Auslegungsprüfung (Paket 7, Stufe 1).
    ///
    /// Paket 3 hat die Rechenseite geliefert (<see cref="VDI4640Pruefung"/>), aber keine
    /// Eingangsgrößen: Der Quellendialog zeigte deshalb „(noch kein Simulationslauf)".
    /// Diese Klasse schließt die Lücke. Nach jedem Lauf (Aufruf am Ende von
    /// <c>SimulationControl.Do_Simulation</c>) wertet sie die Wärmepumpen-Ergebnisse
    /// aus und legt je Energieanlage mit <c>WQ_Typ = 'Erdreich'</c> ab:
    ///
    ///   Entzugsganglinie [kW]       = WP_Waermeproduktion_stuendlich − WP_Strombedarf_stuendlich
    ///   Jahresentzugsarbeit [kWh/a] = Summe der Entzugsganglinie
    ///   max. Entzugsleistung [W]    = Maximum der Entzugsganglinie × 1000
    ///   Volllaststunden     [h/a]   = Jahresentzugsarbeit / Spitzenentzugsleistung
    ///
    /// EINE BASIS FÜR ALLE DREI GRÖSSEN. Das ist der Kern der Nacharbeit: Zuvor kamen
    /// Jahresentzugsarbeit und Volllaststunden aus den MODUL-Jahressummen
    /// (<c>Modul_WP_Waermeproduktion</c>/<c>…Strombedarf</c>/<c>…Laufzeit</c>), die
    /// Spitzenleistung dagegen aus der GLOBALEN Stundenganglinie. Beide Zahlenwerke
    /// weichen systematisch voneinander ab, weil die Wärme, die die Wärmepumpe zum
    /// LADEN des Senkenspeichers erzeugt, nur in die Ganglinie einfließt
    /// (<c>SimulationWaermepumpe</c>, Block „Pufferspeicher laden aus WP-Überschuss")
    /// und nicht in die Modulsummen. Die Auslegungsprüfung setzte damit einen zu
    /// kleinen Jahresentzug gegen eine zu große Spitze - die Volllaststunden fielen
    /// entsprechend zu niedrig aus und die Prüfung wurde stillschweigend zu milde.
    /// Bis Paket 4 die Ganglinie je Modul liefert, ist die globale Ganglinie die
    /// belastbarere der beiden Basen; gibt es einen Senkenspeicher, ist die
    /// Speicherladung darin enthalten und der Kurztext sagt das an.
    ///
    /// GRENZE DER STUFE 1 (bewusst, siehe Protokoll): Wärmeproduktion und Strombedarf
    /// liegen als Stundenganglinie nur GLOBAL vor (Summe aller WP-Module). Die
    /// Zuordnung zum Modul ist deshalb gestuft:
    ///
    ///   • genau ein WP-Modul                → exakt (die globale Ganglinie IST die des Moduls)
    ///   • mehrere Module, alle mit Erdreich → die globale Ganglinie ist vollständig
    ///                                          Erdreich-Entzug; der Modulanteil wird
    ///                                          proportional zur Modul-Jahresentzugsarbeit
    ///                                          verteilt und als Näherung gekennzeichnet
    ///   • gemischte Quellen                 → nicht je Modul trennbar; die Prüfung
    ///                                          bleibt aus, der Hinweis sagt warum
    ///
    /// WIRKSAMKEITSREGEL DER ENGINE: Für Wärmepumpen mit <c>Tab_WP.Typ = 'Luft-Wasser'</c>
    /// liefern <c>WaermequelleClass.Quelltemperatur</c> und <c>…Quellspeicher</c>
    /// unabhängig von WQ_* immer die Außenluft bzw. null - eine dort gepflegte
    /// Erdreich-Konfiguration wird gar nicht gerechnet. Solche Anlagen werden NICHT
    /// geprüft; stattdessen weisen Kurztext und Dialog darauf hin, dass die
    /// Konfiguration wirkungslos ist. Andernfalls stünde eine VDI-4640-Aussage über
    /// ein Erdreich im Ergebnis, das die Simulation nie angefasst hat.
    ///
    /// Zweite Warnbedingung aus Konzept 13.1: Quelltemperatur minus Spreizung soll
    /// 0 °C nicht DAUERHAFT unterschreiten. Gezählt werden ausschließlich die
    /// BETRIEBSSTUNDEN (Stunden, in denen die Wärmepumpe läuft) - in den Stillstands-
    /// stunden entzieht niemand Wärme, eine Frostmeldung daraus wäre gegenstandslos.
    /// Schwelle: <see cref="FROST_ANTEIL_MAX"/> der Betriebsstunden.
    ///
    /// Die Ergebnisse liegen prozessweit je Projekt (Muster der übrigen Statics in
    /// <c>Program</c>), damit sie sowohl die Detailansicht als auch den später
    /// geöffneten Quellendialog erreichen, ohne dass eine Aufrufkette dafür nötig ist.
    /// Sie werden NICHT persistiert - sie gelten für den Lauf der laufenden Sitzung.
    /// </summary>
    public static class ErdreichAuswertung
    {
        /// <summary>
        /// Anteil der BETRIEBSSTUNDEN, ab dem „Quelltemperatur − Spreizung &lt; 0 °C"
        /// als dauerhaft gilt (5 %). Einzelne Frostspitzen sind normal und sollen
        /// nicht warnen. Bezug sind bewusst die Betriebsstunden und nicht die 8760
        /// Jahresstunden: eine Wärmepumpe mit 2 000 Betriebsstunden hätte sonst erst
        /// ab 438 h gewarnt - also erst, wenn schon rund ein Fünftel ihrer Laufzeit
        /// im Frost liegt.
        /// </summary>
        public const double FROST_ANTEIL_MAX = 0.05;

        /// <summary>
        /// Normbasis der Auslegungsprüfung, die im Frosthinweis genannt wird:
        /// VDI 4640 Blatt 2 bemisst Erdkollektor und Erdsonde gegen eine minimale
        /// Soleaustrittstemperatur von −5 °C. Eine rechnerisch unter 0 °C liegende
        /// Soletemperatur ist damit kein Widerspruch zu „Grenzwert eingehalten" -
        /// genau das musste der Meldungstext sagen.
        ///
        /// Reiner Anzeigetext, deshalb aus dem Ressourcenkatalog und nicht mehr
        /// als <c>const</c> (Paket 9: eine Konstante könnte nicht übersetzen).
        /// </summary>
        public static string FROST_NORMBASIS
        {
            get { return MyResource.Resource.SIMQ_FROST_NORMBASIS; }
        }

        /// <summary>Vorgabespreizung der Quelle [K], wenn WQ_Spreizung nicht gepflegt ist
        /// (gleicher Wert wie in <c>WaermequelleClass.Quellspeicher</c>).</summary>
        public const double SPREIZUNG_DEFAULT = 5.0;

        /// <summary>Ergebnisgrößen einer Erdreich-Wärmequelle nach einem Simulationslauf.</summary>
        public class AnlageErgebnis
        {
            /// <summary>Tab_Energieanlagen.ID der Wärmepumpe.</summary>
            public int ID_Anlage;

            /// <summary>Bezeichner des WP-Moduls (Anzeige).</summary>
            public string Modul = "";

            /// <summary>Jahresentzugsarbeit [kWh/a] aus der Entzugsganglinie (therm − el).</summary>
            public double JahresentzugKWh;

            /// <summary>
            /// Jahresvolllaststunden [h/a] = Jahresentzugsarbeit / Spitzenentzugsleistung.
            /// Aus derselben Ganglinie wie die beiden Bezugsgrößen, damit die drei Zahlen
            /// zueinander passen (siehe Klassenkommentar).
            /// </summary>
            public double VolllastStunden;

            /// <summary>Stunden mit laufender Wärmepumpe [h/a] (Bezug der Frostprüfung).</summary>
            public int BetriebsStunden;

            /// <summary>
            /// true, wenn dem Projekt ein Senkenspeicher zugeordnet ist: dann enthält
            /// die Entzugsganglinie auch die Wärme, mit der die Wärmepumpe den Speicher
            /// lädt. Wird im Kurztext als „inkl. Speicherladung" ausgewiesen.
            /// </summary>
            public bool InklSpeicherladung;

            /// <summary>
            /// true, wenn die Erdreich-Konfiguration in der Simulation gar nicht wirkt
            /// (Luft-Wasser-Wärmepumpe). Dann steht in <see cref="Grenze"/> der Grund
            /// und es wird nichts geprüft.
            /// </summary>
            public bool Unwirksam;

            /// <summary>Maximale Entzugsleistung [W]; 0, wenn nicht ermittelbar.</summary>
            public double MaxEntzugW;

            /// <summary>false = maximale Entzugsleistung nicht je Modul trennbar.</summary>
            public bool MaxEntzugBelastbar;

            /// <summary>true, wenn MaxEntzugW aus der Summenganglinie geschätzt wurde.</summary>
            public bool MaxEntzugGeschaetzt;

            /// <summary>Erläuterung zur Belastbarkeit (leer = ohne Vorbehalt).</summary>
            public string Grenze = "";

            /// <summary>Stunden mit Quelltemperatur − Spreizung &lt; 0 °C.</summary>
            public int FrostStunden;

            /// <summary>true, wenn die Frostgrenze dauerhaft unterschritten wird (13.1).</summary>
            public bool FrostWarnung;

            /// <summary>Ergebnis der Auslegungsprüfung nach VDI 4640 Bl. 2 (nie null).</summary>
            public VDI4640Pruefung.Ergebnis Pruefung = new VDI4640Pruefung.Ergebnis();

            /// <summary>
            /// Kompakte Textzeile für den Ergebnisbereich der Detailansicht.
            /// Reine Anzeige - der Text wird nirgends verglichen oder gespeichert.
            /// Die Zahlenformate ("N0") kommen aus dem Quelltext, der Katalog führt
            /// die Platzhalter normalisiert (Lesehinweis des Ressourcenkatalogs).
            /// </summary>
            public string Kurztext()
            {
                CultureInfo ci = CultureInfo.CurrentCulture;
                string kopf = string.Format(ci, MyResource.Resource.SIMQ_ERDREICH_KURZTEXT_KOPF, Modul);

                if (Unwirksam)
                    return kopf + Grenze;

                if (!MaxEntzugBelastbar)
                    return kopf + string.Format(ci,
                        MyResource.Resource.SIMQ_VDI4640_PRUEFUNG_NICHT_MOEGLICH, Grenze);

                string text = kopf + string.Format(ci,
                    MyResource.Resource.SIMQ_ERDREICH_ENTZUG_KURZTEXT,
                    JahresentzugKWh.ToString("N0", ci),
                    InklSpeicherladung ? MyResource.Resource.SIMQ_INKL_SPEICHERLADUNG : "",
                    MaxEntzugW.ToString("N0", ci),
                    VolllastStunden.ToString("N0", ci));

                if (!Pruefung.Moeglich) text += Pruefung.Hinweis;
                else if (Pruefung.Warnung) text += MyResource.Resource.SIMQ_VDI4640_GRENZWERT_UEBERSCHRITTEN;
                else text += MyResource.Resource.SIMQ_VDI4640_EINGEHALTEN;

                if (MaxEntzugGeschaetzt) text += MyResource.Resource.SIMQ_SPITZE_AUS_SUMMENGANGLINIE;
                if (FrostWarnung) text += " " + Frosttext();

                return text;
            }

            /// <summary>
            /// Meldungstext der zweiten Warnbedingung, mit Bezugsgröße und Normbasis.
            /// Ohne die Normbasis las sich „VDI 4640: eingehalten" direkt neben der
            /// Frostmeldung wie ein Widerspruch - tatsächlich prüft die Norm gegen
            /// −5 °C Soleaustritt, die Frostbedingung ist eine ZUSÄTZLICHE, strengere
            /// Betrachtung aus Konzept 13.1.
            /// </summary>
            public string Frosttext()
            {
                CultureInfo ci = CultureInfo.CurrentCulture;
                return string.Format(ci, MyResource.Resource.SIMQ_FROSTTEXT,
                    FrostStunden.ToString("N0", ci), BetriebsStunden.ToString("N0", ci),
                    FROST_NORMBASIS);
            }
        }

        // Prozessweiter Zwischenspeicher je Projekt (letzter Lauf gewinnt).
        private static readonly Dictionary<int, List<AnlageErgebnis>> _proProjekt =
            new Dictionary<int, List<AnlageErgebnis>>();

        /// <summary>Ergebnisse des letzten Laufs eines Projekts (nie null, ggf. leer).</summary>
        public static List<AnlageErgebnis> FuerProjekt(int idProjekt)
        {
            List<AnlageErgebnis> liste;
            lock (_proProjekt)
                if (_proProjekt.TryGetValue(idProjekt, out liste)) return liste;
            return new List<AnlageErgebnis>();
        }

        /// <summary>Ergebnis einer einzelnen Energieanlage oder null.</summary>
        public static AnlageErgebnis FuerAnlage(int idProjekt, int idAnlage)
        {
            foreach (AnlageErgebnis a in FuerProjekt(idProjekt))
                if (a.ID_Anlage == idAnlage) return a;
            return null;
        }

        // =================================================================================
        // W10a.0b - was der Erdreich-Dialog von einem Lauf braucht
        // =================================================================================

        /// <summary>
        /// Die sieben Größen, mit denen die Auslegungsprüfung des Erdreich-Dialogs
        /// arbeitet — das Ergebnis eines Laufs, übersetzt in die Sprache der Anzeige.
        /// </summary>
        /// <param name="Vorhanden">
        /// Es gibt überhaupt ein Ergebnis für diese Anlage. <c>false</c> heißt „noch kein
        /// Simulationslauf"; die übrigen Felder sind dann bedeutungslos.
        /// </param>
        /// <param name="ErgebnisseVorhanden">
        /// Die maximale Entzugsleistung ist je Modul belastbar — nur dann RECHNET die
        /// Prüfung, sonst steht nur <paramref name="HinweisErgebnis"/> da.
        /// </param>
        /// <param name="MaxEntzugW">Maximale Entzugsleistung [W].</param>
        /// <param name="JahresentzugKWh">Jahresentzugsarbeit [kWh/a].</param>
        /// <param name="VolllastStunden">Jahresvolllaststunden [h/a].</param>
        /// <param name="HinweisErgebnis">
        /// Der Text ANSTELLE der Prüfung (Luft-Wasser oder nicht belastbar); leer, wenn
        /// gerechnet werden kann.
        /// </param>
        /// <param name="HinweisVorbehalt">Vorbehalt ZUR Prüfung (geschätzt, inkl. Speicherladung).</param>
        /// <param name="HinweisFrost">Frostmeldung der zweiten Warnbedingung.</param>
        public sealed record ErdreichLaufErgebnis(
            bool Vorhanden,
            bool ErgebnisseVorhanden,
            double MaxEntzugW,
            double JahresentzugKWh,
            double VolllastStunden,
            string HinweisErgebnis,
            string HinweisVorbehalt,
            string HinweisFrost)
        {
            /// <summary>„Es gab keinen Lauf" — der Zustand beim Öffnen ohne Ergebnis.</summary>
            public static readonly ErdreichLaufErgebnis Keines =
                new ErdreichLaufErgebnis(false, false, 0, 0, 0, "", "", "");
        }

        /// <summary>
        /// Übersetzt ein <see cref="AnlageErgebnis"/> in die Anzeigegrößen des
        /// Erdreich-Dialogs.
        ///
        /// <para><b>iU9‑W10a.0b (Befund W10‑B8).</b> Diese Zuordnung stand ZWEIMAL
        /// wortgleich da: in <c>Form_QuelleErdreich.ErgebnisUebernehmen</c> :1155-1188 und
        /// in <c>Form_Simulation_Config.Uebersicht</c> :1130-1162, wo der Aufrufer denselben
        /// Satz Felder beim Öffnen des Dialogs setzt. Der Quelltext vermerkte die Doppelung
        /// selbst und nannte diese Klasse als richtigen Ort; dort steht sie jetzt.</para>
        ///
        /// <para><b>Die Reihenfolge der drei Fälle ist die Fachregel.</b> „Unwirksam"
        /// (Luft-Wasser) schlägt „nicht belastbar", und beide schließen die Prüfung
        /// aus — erst danach werden Vorbehalt und Frostmeldung überhaupt gefüllt.</para>
        /// </summary>
        /// <param name="erg">Das Laufergebnis; <c>null</c> ergibt <see cref="ErdreichLaufErgebnis.Keines"/>.</param>
        public static ErdreichLaufErgebnis ErgebnisZuordnen(AnlageErgebnis erg)
        {
            if (erg == null) return ErdreichLaufErgebnis.Keines;

            if (erg.Unwirksam)
            {
                // Luft-Wasser: die Konfiguration wird gar nicht gerechnet. Das muss im
                // Dialog stehen, sonst pflegt der Anwender Bodentyp und Sondenlaenge ins
                // Leere (Konzept 4.5). Umbrueche VOR dem Einsetzen normalisieren.
                return new ErdreichLaufErgebnis(
                    true, erg.MaxEntzugBelastbar, erg.MaxEntzugW, erg.JahresentzugKWh,
                    erg.VolllastStunden,
                    string.Format(CultureInfo.CurrentCulture,
                        Zeilenumbruch.Normalisieren(MyResource.Resource.SIMQ_ERDREICH_WIRKUNGSLOS),
                        erg.Grenze),
                    "", "");
            }

            if (!erg.MaxEntzugBelastbar)
            {
                return new ErdreichLaufErgebnis(
                    true, false, erg.MaxEntzugW, erg.JahresentzugKWh, erg.VolllastStunden,
                    string.Format(CultureInfo.CurrentCulture,
                        Zeilenumbruch.Normalisieren(MyResource.Resource.SIMQ_ERDREICH_KEINE_PRUEFUNG),
                        erg.Grenze),
                    "", "");
            }

            string vorbehalt = erg.MaxEntzugGeschaetzt ? erg.Grenze : "";
            if (erg.InklSpeicherladung)
                vorbehalt = (vorbehalt.Length > 0 ? vorbehalt + " " : "") +
                            MyResource.Resource.SIMQ_ERDREICH_SPEICHERLADUNG;

            return new ErdreichLaufErgebnis(
                true, true, erg.MaxEntzugW, erg.JahresentzugKWh, erg.VolllastStunden,
                "", vorbehalt, erg.FrostWarnung ? erg.Frosttext() : "");
        }

        /// <summary>
        /// Wertet einen abgeschlossenen Lauf aus und legt die Erdreich-Ergebnisse ab.
        /// Fehlertolerant: Bei fehlenden Daten bleibt die Liste des Projekts leer,
        /// der Lauf selbst wird nie beeinträchtigt (reine Auswertung).
        /// </summary>
        public static void AusLauf(SimulationControl sim)
        {
            if (sim == null || sim.m_ID_Projekt <= 0) return;

            var ergebnisse = new List<AnlageErgebnis>();
            try
            {
                // PAKET E1: Gefragt ist, ob die WÄRMEPUMPE einen Senkenspeicher lädt —
                // das steht am Modul selbst. Bis hierher stand hier der Alias
                // sim.puffer_wp; SimulationControl setzt simulation_wp.Pufferspeicher aus
                // genau diesem Alias, der Wert ist also unverändert (kein Ergebniseffekt).
                // Der Umweg über den Alias entfällt, weil er den ERSTEN Heizungspuffer
                // des Laufs meint und nicht den der Wärmepumpe (Konzept 6.3).
                if (sim.bSimulationWP && sim.simulation_wp != null)
                    ergebnisse = Auswerten(sim.m_ID_Projekt, sim.simulation_wp,
                                           sim.simulation_wp.Pufferspeicher != null);
            }
            catch (Exception ex)
            {
                // Protokollkanal-Nachzug: WARNUNG - die Erdreich-Kennwerte des Laufs
                // (Frostbilanz, VDI-4640-Prüfgrößen) bleiben leer, und der Anwender sähe
                // sonst nur leere Anzeigen ohne Grund. Der Kanal des Laufs steht hier
                // noch: AusLauf ist der letzte Schritt von Do_Simulation_Intern.
                SimulationProtokoll.Aktuell.Warnung("Erdreich-Auswertung fehlgeschlagen: " + ex.Message +
                                                    " - die Erdreich-Kennwerte dieses Laufs bleiben leer.");
                ergebnisse = new List<AnlageErgebnis>();
            }

            lock (_proProjekt) _proProjekt[sim.m_ID_Projekt] = ergebnisse;
        }

        // ------------------------------------------------------------------
        // Auswertung
        // ------------------------------------------------------------------

        private static List<AnlageErgebnis> Auswerten(int idProjekt, SimulationWaermepumpe wp,
                                                      bool mitSenkenspeicher)
        {
            var liste = new List<AnlageErgebnis>();
            int anzahlModule = wp.wp_list.Count;
            if (anzahlModule == 0) return liste;

            // Welche Module sind auf Erdreich konfiguriert - und bei welchen wirkt das
            // auch? Luft-Wasser rechnet immer mit der Außenluft (Wirksamkeitsregel,
            // siehe SimulationWaermepumpe.WPTypen).
            var konfiguriert = new bool[anzahlModule];
            var wirksam = new bool[anzahlModule];
            int anzahlWirksam = 0;
            for (int i = 0; i < anzahlModule; i++)
            {
                string typ = WaermequelleClass.WertLesen(wp.wp_list[i], "WQ_Typ") as string;
                konfiguriert[i] = (typ == WaermequelleClass.TYP_ERDREICH);
                wirksam[i] = konfiguriert[i] && !IstLuftWasser(wp, i);
                if (wirksam[i]) anzahlWirksam++;
            }

            // Entzugsganglinie (therm − el) der GESAMTEN Wärmepumpenkaskade [kW].
            // Jahresarbeit, Spitze und Betriebsstunden kommen aus derselben Reihe -
            // das ist der Kern der Basiskorrektur (siehe Klassenkommentar).
            double maxEntzugGesamtKW = 0;
            double entzugGesamtKWh = 0;
            int betriebsStunden = 0;
            var laeuft = new bool[8760];

            float[] therm = wp.WP_Waermeproduktion_stuendlich;
            float[] el = wp.WP_Strombedarf_stuendlich;
            if (therm != null && el != null)
            {
                int n = Math.Min(therm.Length, el.Length);
                if (n > laeuft.Length) n = laeuft.Length;
                for (int i = 0; i < n; i++)
                {
                    if (therm[i] > 0) { laeuft[i] = true; betriebsStunden++; }

                    double q = therm[i] - el[i];
                    if (q <= 0) continue;
                    entzugGesamtKWh += q;
                    if (q > maxEntzugGesamtKW) maxEntzugGesamtKW = q;
                }
            }

            // Verteilungsschlüssel bei mehreren Erdreich-Modulen: die Modul-Jahressummen.
            // Sie dienen NUR noch als Anteil, nicht mehr als Absolutwert - damit wandert
            // der Basisbruch nicht über die Hintertür wieder herein.
            var entzugModul = new double[anzahlModule];
            double entzugWirksamSumme = 0;
            for (int i = 0; i < anzahlModule; i++)
            {
                double e = wp.Modul_WP_Waermeproduktion[i] - wp.Modul_WP_Strombedarf[i];
                if (e < 0) e = 0;
                entzugModul[i] = e;
                if (wirksam[i]) entzugWirksamSumme += e;
            }

            // Eindeutigkeit der Zuordnung (siehe Klassenkommentar). Ein nicht wirksames
            // Modul (Luft-Wasser) speist die Ganglinie mit Luftwärme und macht den Fall
            // damit genauso gemischt wie eine andere Quelle.
            bool eindeutig = anzahlWirksam > 0 &&
                             (anzahlModule == 1 || anzahlWirksam == anzahlModule);
            bool geschaetzt = eindeutig && anzahlModule > 1;

            int klimazone = KlimazoneDesProjekts(idProjekt);

            for (int i = 0; i < anzahlModule; i++)
            {
                if (!konfiguriert[i]) continue;

                AnlageErgebnis a = new AnlageErgebnis();
                a.ID_Anlage = wp.wp_list[i];
                a.Modul = (i < wp.WP_Modul.Length && !string.IsNullOrEmpty(wp.WP_Modul[i]))
                    ? wp.WP_Modul[i]
                    : string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.SIMQ_ANLAGE_ERSATZNAME, a.ID_Anlage);

                // Luft-Wasser: die Konfiguration wird nicht gerechnet - nichts prüfen,
                // sondern sagen, warum hier keine Zahlen stehen.
                if (!wirksam[i])
                {
                    a.Unwirksam = true;
                    a.MaxEntzugBelastbar = false;
                    a.Grenze = MyResource.Resource.SIMQ_ERDREICH_UNWIRKSAM_LUFT_WASSER;
                    a.Pruefung = new VDI4640Pruefung.Ergebnis { Moeglich = false, Hinweis = a.Grenze };
                    liste.Add(a);
                    continue;
                }

                a.BetriebsStunden = betriebsStunden;
                a.InklSpeicherladung = mitSenkenspeicher;

                if (!eindeutig)
                {
                    a.MaxEntzugBelastbar = false;
                    a.Grenze = MyResource.Resource.SIMQ_ENTZUG_NICHT_JE_MODUL_TRENNBAR;
                }
                else
                {
                    double anteil = 1.0;
                    if (geschaetzt && entzugWirksamSumme > 0)
                        anteil = entzugModul[i] / entzugWirksamSumme;

                    a.JahresentzugKWh = entzugGesamtKWh * anteil;
                    a.MaxEntzugW = maxEntzugGesamtKW * anteil * 1000.0;   // kW -> W

                    // Volllaststunden aus DENSELBEN beiden Größen. Der Anteil kürzt sich
                    // heraus, das Ergebnis ist also modulunabhängig - richtig so, denn
                    // die zugrunde liegende Ganglinie ist es auch.
                    a.VolllastStunden = (maxEntzugGesamtKW > 0)
                        ? entzugGesamtKWh / maxEntzugGesamtKW
                        : betriebsStunden;

                    a.MaxEntzugBelastbar = true;
                    a.MaxEntzugGeschaetzt = geschaetzt;
                    if (geschaetzt)
                        a.Grenze = MyResource.Resource.SIMQ_ENTZUG_ANTEILIG_GESCHAETZT;
                }

                a.Pruefung = Pruefen(a, klimazone);
                FrostPruefen(a, wp, i, laeuft, betriebsStunden);
                liste.Add(a);
            }

            return liste;
        }

        /// <summary>
        /// Rechnet die Engine für dieses Modul mit der Außenluft? Dann bleibt die
        /// WQ_*-Konfiguration wirkungslos.
        ///
        /// Die Bedingung ist wortgleich zu <c>WaermequelleClass.Quelltemperatur</c> und
        /// <c>…Quellspeicher</c>: leerer Typ ODER „Luft-Wasser". Liegt die Typliste gar
        /// nicht vor (Lauf einer älteren Fassung), bleibt es beim bisherigen Verhalten
        /// und es wird geprüft.
        /// </summary>
        private static bool IstLuftWasser(SimulationWaermepumpe wp, int index)
        {
            var typen = wp.WPTypen;
            if (typen == null || index >= typen.Count) return false;

            string typ = typen[index];
            return string.IsNullOrEmpty(typ) ||
                   string.Equals(typ, DbWerte.WP_BAUART_LUFT_WASSER, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Auslegungsprüfung nach VDI 4640 Bl. 2 mit den Anlagendaten aus WQ_*.</summary>
        private static VDI4640Pruefung.Ergebnis Pruefen(AnlageErgebnis a, int klimazone)
        {
            if (!a.MaxEntzugBelastbar)
                return new VDI4640Pruefung.Ergebnis { Moeglich = false, Hinweis = a.Grenze };

            string quellsystem = WaermequelleClass.WertLesen(a.ID_Anlage, "WQ_Quellsystem") as string;
            string bodentyp = WaermequelleClass.WertLesen(a.ID_Anlage, "WQ_Bodentyp") as string;
            if (string.IsNullOrEmpty(bodentyp)) bodentyp = ErdreichTemperatur.BODENTYP_DEFAULT;

            double tiefe = Zahl(WaermequelleClass.WertLesen(a.ID_Anlage, "WQ_Tiefe"));
            double flaeche = Zahl(WaermequelleClass.WertLesen(a.ID_Anlage, "WQ_Flaeche"));
            int anzahl = (int)Zahl(WaermequelleClass.WertLesen(a.ID_Anlage, "WQ_Anzahl"));

            if (quellsystem == ErdreichTemperatur.QUELLSYSTEM_SONDE)
            {
                int sonden = Math.Max(1, anzahl);
                double meter = tiefe * sonden;
                double stunden = a.VolllastStunden > 0
                    ? a.VolllastStunden : VDI4640Pruefung.VolllaststundenZone(klimazone);
                return VDI4640Pruefung.PruefeSonde(
                    ErdreichTemperatur.Bodentyp(bodentyp).Lambda,
                    sonden, stunden, meter, a.MaxEntzugW, bodentyp);
            }

            return VDI4640Pruefung.PruefeKollektor(
                klimazone, VDI4640Pruefung.BodenartAusBodentyp(bodentyp),
                flaeche, a.MaxEntzugW, a.JahresentzugKWh, bodentyp);
        }

        /// <summary>
        /// Zweite Warnbedingung aus Konzept 13.1: Quelltemperatur minus Spreizung soll
        /// 0 °C nicht dauerhaft unterschreiten.
        ///
        /// Gezählt werden ausschließlich die BETRIEBSSTUNDEN - Stunden, in denen die
        /// Wärmepumpe läuft. In der Stillstandszeit wird der Quelle nichts entzogen, das
        /// Erdreich regeneriert dort; eine Frostmeldung aus Stillstandsstunden wäre
        /// gegenstandslos und hat die Warnung zuvor systematisch zu früh ausgelöst
        /// (Bezug waren alle 8760 Stunden). Gewarnt wird ab
        /// <see cref="FROST_ANTEIL_MAX"/> der Betriebsstunden.
        ///
        /// Die Betriebsstunden stammen aus der GLOBALEN Ganglinie und sind damit dieselbe
        /// Näherung wie beim übrigen Ausweis - je Modul entsteht die Ganglinie erst mit
        /// Paket 4.
        /// </summary>
        private static void FrostPruefen(AnlageErgebnis a, SimulationWaermepumpe wp, int index,
                                         bool[] laeuft, int betriebsStunden)
        {
            var profile = wp.Quelltemperaturen;
            if (profile == null || index >= profile.Count) return;

            float[] quelltemp = profile[index];
            if (quelltemp == null || quelltemp.Length == 0) return;
            if (laeuft == null || betriebsStunden <= 0) return;

            double spreizung = Zahl(WaermequelleClass.WertLesen(a.ID_Anlage, "WQ_Spreizung"));
            if (spreizung <= 0) spreizung = SPREIZUNG_DEFAULT;

            int n = Math.Min(quelltemp.Length, laeuft.Length);
            int unterNull = 0;
            for (int i = 0; i < n; i++)
                if (laeuft[i] && quelltemp[i] - spreizung < 0) unterNull++;

            a.FrostStunden = unterNull;
            a.FrostWarnung = unterNull > FROST_ANTEIL_MAX * betriebsStunden;
        }

        /// <summary>Klimazone (DIN 4710) der Klimaregion des Projekts; 0 = nicht zugeordnet.</summary>
        private static int KlimazoneDesProjekts(int idProjekt)
        {
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID_Klimaregion FROM Tab_Projekt WHERE ID = ?",
                    new DbParam("@p", idProjekt));
                if (dt == null || dt.Rows.Count == 0 || dt.Rows[0][0] == DBNull.Value) return 0;
                return KlimaregionCtrl.GetKlimazone(Convert.ToInt32(dt.Rows[0][0]));
            }
            catch { return 0; }
        }

        private static double Zahl(object o)
        {
            if (o == null || o == DBNull.Value) return 0;
            try { return Convert.ToDouble(o); }
            catch { return 0; }
        }
    }
}
