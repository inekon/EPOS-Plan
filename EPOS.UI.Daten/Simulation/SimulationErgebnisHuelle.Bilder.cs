using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Seiten.Simulation;
using SkiaSharp;
using SpeicherEngine;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die BILDER, die CSV-Exporte und die Überlagerungen der Ergebnishülle
    /// (iU9-W11b.13).
    ///
    /// <para><b>Siebzehn Zeichenflächen, sieben Renderer-Bilder.</b> Was der Vorläufer
    /// mit neun <c>Chart</c>-Steuerelementen, fünf <c>ChartManager</c> und zwei
    /// GDI-Donuts zeichnete, kommt hier als PNG aus dem Kern-Renderer (iU9-W11a.6).
    /// Gerendert wird erst auf Anforderung: Die Seite fragt je Reiter und
    /// Schalterstellung, und ihr Zwischenspeicher hält das Ergebnis.</para>
    ///
    /// <para><b>Zwölf davon sind ZEICHENMODELLE</b> (Etappe DG-E3, Gruppe (a)): Jede
    /// Stelle mit Zeitachse liefert ein <c>Zeichenmodell</c> (<see cref="Modell"/>) und
    /// steht in der Oberfläche im Baustein <c>DiagrammSvg</c> — Zoom auf der Zeitachse,
    /// Werte am Mauszeiger, Legende und Farbwahl ohne einen Rundlauf in den Kern.
    /// Pixelbild bleiben allein die vier Bilder OHNE Zeitachse
    /// (<see cref="Bild"/>).</para>
    /// </summary>
    internal sealed partial class SimulationErgebnisHuelle
    {
        // Die Farben der Reihen — wörtlich die des Vorläufers, nur als SKColor.
        private static readonly SKColor F_BEDARF = SKColors.Red;
        private static readonly SKColor F_PRODUKTION = SKColors.Blue;
        private static readonly SKColor F_HEIZSTAB = SKColors.Yellow;
        private static readonly SKColor F_REST = SKColors.Green;
        private static readonly SKColor F_WARMWASSER = SKColors.DeepSkyBlue;
        private static readonly SKColor F_SPEICHERLADUNG = SKColors.DarkOrange;
        private static readonly SKColor F_SPEICHER = new SKColor(120, 130, 140);
        private static readonly SKColor F_UEBERSCHUSS = SKColors.Yellow;
        private static readonly SKColor F_PV = SKColors.BlueViolet;
        private static readonly SKColor F_WAERMEPUMPE = SKColors.Orange;
        private static readonly SKColor F_KESSEL = SKColors.Blue;
        private static readonly SKColor F_SOLAR = SKColors.Brown;
        private static readonly SKColor F_BHKW = SKColors.Red;
        private static readonly SKColor F_GESAMT = SKColors.Green;
        private static readonly SKColor F_LASTGANG = SKColors.Brown;

        /// <summary>
        /// Befund W11-B40 (A-Zeile): Lastgangprofil und BHKW-Strom trugen im Vorläufer
        /// BEIDE <c>Color.Brown</c> — im Stapel unten und als Linie darüber nicht zu
        /// unterscheiden. Das BHKW bekommt hier eine eigene Farbe.
        /// </summary>
        private static readonly SKColor F_BHKW_STROM = SKColors.SaddleBrown;

        private static readonly SKColor[] F_KANAL =
        {
            SKColors.Red, SKColors.DeepSkyBlue, new SKColor(0x7E, 0x57, 0xA6)
        };

        private static readonly SKColor[] F_SPEICHERREIHEN =
        {
            SKColors.MediumVioletRed, SKColors.DarkViolet, SKColors.Teal,
            SKColors.SaddleBrown, SKColors.DarkSlateGray, SKColors.Crimson
        };

        // Die Ringfarben der zwei GDI-Donuts (NavigatorUebersicht :310-388).
        private static readonly SKColor R_WP = SKColor.Parse("#2ECC71");
        private static readonly SKColor R_SOLAR = SKColor.Parse("#E67E22");
        private static readonly SKColor R_HEIZSTAB = SKColor.Parse("#F1C40F");
        private static readonly SKColor R_KESSEL = SKColor.Parse("#95A5A6");
        private static readonly SKColor R_BHKW = SKColor.Parse("#75A5A6");
        private static readonly SKColor R_REST = SKColor.Parse("#3498DB");
        private static readonly SKColor R_PV = SKColor.Parse("#2ECC71");
        private static readonly SKColor R_BHKW_STROM = SKColor.Parse("#E67E22");
        private static readonly SKColor R_SPEICHER = SKColor.Parse("#9B59B6");

        /// <summary>
        /// Der UNGEDECKTE REST — seit #222 in BEIDEN Ringen dasselbe Grau
        /// (Anwenderentscheid 11.09.2026 „Empfehlung", Ringvariante A).
        ///
        /// <para>Vorher war er im Wärmering Blau (<c>#3498DB</c>) und im Stromring Gelb
        /// (<c>#F1C40F</c>) — zwei kräftige Farben für dieselbe Aussage „hier fehlt
        /// etwas", und im Stromring ohne Erzeuger sah ein voller gelber Kreis aus wie
        /// eine Leistung. Grau ist die Abwesenheit; das Bild sagt damit dasselbe wie
        /// die Legende daneben. Der Wert ist derselbe wie das Stilblatt-Token
        /// <c>--epos-ring-rest</c>, damit das Legendenkästchen die Segmentfarbe
        /// trifft.</para>
        /// </summary>
        private static readonly SKColor R_REST_GRAU = SKColor.Parse("#D9DEE5");

        // =================================================================
        // Ein Bild
        // =================================================================

        /// <summary>
        /// <b>Die vier Bilder, die PIXELBILDER BLEIBEN</b> (Etappe DG-E3, Gruppe (a),
        /// Oberflächenteil): die Streuwolke „Leistung über Außentemperatur", die zwei
        /// Deckungsringe der Übersicht und die Monatssäulen der Autarkie.
        ///
        /// <para>Sie tragen keine ZEITachse — x ist dort die Außentemperatur, ein
        /// Kreissegment oder ein Monat —, und ihre Zeichenmodelle entstehen in den
        /// Gruppen (b) und (c) des Rollouts. Bis dahin geht ihr Weg unverändert über
        /// <c>ChartBild</c>.</para>
        ///
        /// <para>Jede übrige Stelle liefert ein <see cref="Modell"/>.</para>
        /// </summary>
        private byte[] Bild(Bildauftrag a)
        {
            if (a == null) return null;

            try
            {
                switch (a.Bild)
                {
                    case Bilder.RingWaerme: return ErgebnisIstGueltig ? BildRingWaerme() : null;
                    case Bilder.RingStrom: return ErgebnisIstGueltig ? BildRingStrom() : null;
                    case Bilder.WpLeistungTemperatur:
                        return ErgebnisIstGueltig ? BildStreuwolke(a) : null;
                    case Bilder.AutarkieMonate:
                        return ErgebnisIstGueltig ? BildAutarkie(a.Zahl) : null;
                    default: return null;
                }
            }
            catch (Exception ex)
            {
                // Ein Bild, das nicht entsteht, darf die Seite nicht mitreißen; der
                // Platzhalter des Bausteins sagt, dass keines da ist.
                Console.WriteLine("Das Ergebnisbild konnte nicht gezeichnet werden: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// <b>Die zwölf ZEITREIHEN-Bilder als Zeichenmodell</b> (Etappe DG-E3,
        /// Gruppe (a)). Die Seite zeigt sie im Baustein <c>DiagrammSvg</c>: jede Linie
        /// ein Vektor, der Zoom eine Attributänderung an EINER <c>viewBox</c>, der Wert
        /// am Mauszeiger eine Lesestelle im Modell.
        ///
        /// <para><b>Der Rundlauf-Datenzoom ist damit entfallen</b> (Entscheid DG-E3-9,
        /// Konzept Diagramme § 6): Das Modell trägt die Werte ohnehin, also braucht es
        /// weder ein gemeldetes Rechteck noch einen zweiten Renderlauf. Der
        /// <c>Bildauftrag</c> führt deshalb keinen <c>Bereich</c> mehr, und die Hülle
        /// keinen Umrechner dafür.</para>
        /// </summary>
        private Zeichenmodell Modell(Bildauftrag a)
        {
            if (a == null) return null;
            if (!ErgebnisIstGueltig && a.Bild != Bilder.BedarfWaerme && a.Bild != Bilder.BedarfStrom)
                return null;

            try
            {
                switch (a.Bild)
                {
                    case Bilder.BedarfWaerme: return ModellBedarfWaerme(a);
                    case Bilder.BedarfStrom: return ModellBedarfStrom(a);
                    case Bilder.WpProduktion: return ModellWpProduktion(a);
                    case Bilder.WpStromverbrauch: return ModellWpStrom(a);
                    case Bilder.Speichertemperaturen: return ModellTemperaturen(a);
                    case Bilder.Heizkessel: return ModellKessel(a);
                    case Bilder.Solarthermie: return ModellSolar(a);
                    case Bilder.Bhkw: return ModellBhkw(a);
                    case Bilder.Photovoltaik: return ModellPv(a);
                    case Bilder.SpeicherBetrieb: return ModellSpeicherBetrieb(a);
                    case Bilder.Waermegang: return ModellWaermegang(a);
                    case Bilder.Stromgang: return ModellStromgang(a);
                    default: return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Das Ergebnisbild konnte nicht gezeichnet werden: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// W11b‑B‑17 — WAEHLBARE REIHEN. Traegt der Auftrag KEINE Reihenliste
        /// (<c>null</c>), sind alle Reihen gemeint: So rufen die Bilder, die keine
        /// Auswahl kennen, und so rief die Waermepumpenseite vor dem Anwenderwunsch
        /// vom 08.09.2026.
        ///
        /// <para>Eine LEERE Liste ist etwas anderes als keine: Sie heisst „keine
        /// Reihe" — der Anwender hat alle abgewaehlt. Der Renderer zeichnet dann
        /// seinen Leerhinweis; das ist kein Sonderfall und keine Ausnahme.</para>
        /// </summary>
        private static bool Alle(Bildauftrag a) => a == null || a.Reihen == null;

        /// <summary>Gehoert diese Reihe zur Wahl? (Siehe <see cref="Alle"/>.)</summary>
        private static bool Gewaehlt(Bildauftrag a, bool alle, string schluessel)
            => alle || a.Reihen.Contains(schluessel);

        /// <summary>Null-sichere Kopie einer Reihe (bis W8-O-5d die Weitung float -&gt; double).</summary>
        private static double[] Kopie(double[] werte)
            => werte == null ? new double[0] : (double[])werte.Clone();

        // breite ist eine STRICHSTAERKE in Bildpunkten - SkiaSharp rechnet in float,
        // deshalb bleibt der Parameter float (W8-O-5d, Grenze 2a).
        private static ChartRenderer.Reihe Reihe(string name, double[] werte, SKColor farbe,
                                                 ChartRenderer.Stapelart art = ChartRenderer.Stapelart.Keine,
                                                 float breite = 0f)
            => new ChartRenderer.Reihe(name, Kopie(werte), farbe, art, false, breite);

        // ---- B1: die zwei normierten Ganglinien des Bedarfsreiters ------

        private Zeichenmodell ModellBedarfWaerme(Bildauftrag a)
        {
            var reihen = new List<ChartRenderer.Reihe>();
            IReadOnlyList<string> wahl = a.Reihen ?? new List<string>();

            if (wahl.Count == 0 || wahl.Contains("GESAMT"))
                reihen.Add(Reihe(MyResource.Resource.CHART_LEGENDE_SUMME_WAERMEBEDARF,
                                 _waermebedarf.Waermebedarf, F_BEDARF));

            for (int k = 0; k < Kanal.ANZAHL; k++)
            {
                if (!wahl.Contains("KANAL_" + k)) continue;
                reihen.Add(Reihe(KANALNAMEN[k],
                                 SimulationControl.BedarfKanalStuendlich(_waermebedarf, k),
                                 F_KANAL[k % F_KANAL.Length]));
            }

            return ChartRenderer.GanglinieNormiertModell(
                MyResource.Resource.CHART_TITEL_WAERMELAST_JAHRESGANGLINIE, reihen,
                MyResource.Resource.CHART_ACHSE_WAERMELAST,
                a.Sortiert ? ChartRenderer.Achse.Jahresstunden : ChartRenderer.Achse.Monate,
                a.Sortiert);
        }

        private Zeichenmodell ModellBedarfStrom(Bildauftrag a)
        {
            double[] werte = _strombedarf.Strombedarf_viertelStundenwerte;
            var reihen = new List<ChartRenderer.Reihe>
            {
                Reihe(MyResource.Resource.CHART_ACHSE_STROMBEDARF, werte, F_BEDARF)
            };

            return ChartRenderer.GanglinieNormiertModell(
                MyResource.Resource.CHART_TITEL_STROMBEDARF_JAHRESGANGLINIE, reihen,
                MyResource.Resource.CHART_ACHSE_STROMBEDARF,
                a.Sortiert ? ChartRenderer.Achse.Jahresstunden : ChartRenderer.Achse.Monate,
                a.Sortiert);
        }

        // ---- Die zwei Ringe ---------------------------------------------
        //
        // DIE TORTE IST MIT AUFTRAG #240 GEFALLEN. Sie zeichnete die
        // Waermebedarfsdeckung als Kuchen (woertlich FuelleUebersicht :3959-3969)
        // und hatte seit #222 keinen Anforderer mehr: Die Uebersicht des
        // Simulationsergebnisses zeigt dieselbe Aussage seither als RING mit
        // HTML-Legende daneben (BildRingWaerme), und kein Reiter und keine
        // Berichtsseite fragte den Schluessel UEBERSICHT_KUCHEN noch an - nur ein
        // Testfall tat es. Der RENDERER ChartRenderer.Kuchen bleibt: Der
        // Variantenbericht zeichnet damit seine zwei Deckungsbilder
        // (BausteineVergleich :213/:218).

        /// <summary>
        /// Der Ring „Wärmedeckung" (B5) — Segmente NUR für vorhandene Erzeuger, Werte
        /// und Farben gemeinsam gefiltert (<c>NavigatorUebersicht</c> :304-333).
        /// </summary>
        /// <summary>
        /// Die Segmente des WÄRMERINGS — EINE Liste für das Bild UND für die Legende
        /// daneben (#222).
        ///
        /// <para><b>W11b‑B‑11 ist damit anders erfüllt.</b> Bis #222 hängte
        /// <c>MitZahlen</c> Menge und Prozent an den Segmentnamen, damit sie in der
        /// gezeichneten Legende standen. Die Legende steht jetzt als HTML neben dem
        /// Bild und trägt dieselben zwei Zahlen in eigenen Spalten — kopierbar,
        /// mitwachsend und nicht abschneidbar. Der Name bleibt deshalb ein Name.</para>
        ///
        /// <para>Der ungedeckte Rest ist IMMER das letzte Segment (:325-326) — auch
        /// mit dem Wert 0; der Renderer lässt Nullsegmente ohnehin aus.</para>
        /// </summary>
        private List<ChartRenderer.Ringsegment> SegmenteWaerme(
            ErgebnisPraesenz p, SimulationErgebnisCtrl.UebersichtKennzahlen k)
        {
            var segmente = new List<ChartRenderer.Ringsegment>();
            if (p.Waermepumpe)
                segmente.Add(new ChartRenderer.Ringsegment(
                    MyResource.Resource.SIM_ERZEUGERNAME_WAERMEPUMPE, k.WaermeWpMwh, R_WP));
            if (p.Solarthermie)
                segmente.Add(new ChartRenderer.Ringsegment(
                    MyResource.Resource.SIM_ERZEUGERNAME_SOLARTHERMIE, k.WaermeSolarMwh, R_SOLAR));
            if (p.Heizstab)
                segmente.Add(new ChartRenderer.Ringsegment(
                    MyResource.Resource.CHART_SEGMENT_HEIZSTAB, k.WaermeHeizstabMwh, R_HEIZSTAB));
            if (p.Heizkessel)
                segmente.Add(new ChartRenderer.Ringsegment(
                    MyResource.Resource.SIM_ERZEUGERNAME_HEIZKESSEL, k.WaermeKesselMwh, R_KESSEL));
            if (p.BHKW)
                segmente.Add(new ChartRenderer.Ringsegment(
                    MyResource.Resource.SIM_ERZEUGERNAME_BHKW, k.WaermeBhkwMwh, R_BHKW));

            segmente.Add(new ChartRenderer.Ringsegment(
                MyResource.Resource.SIMUEB_LEGENDE_REST, k.RestwaermebedarfMwh, R_REST_GRAU));

            return segmente;
        }

        /// <summary>Die Segmente des STROMRINGS — dieselbe Regel wie bei der Wärme.</summary>
        private List<ChartRenderer.Ringsegment> SegmenteStrom(
            ErgebnisPraesenz p, SimulationErgebnisCtrl.UebersichtKennzahlen k)
        {
            var segmente = new List<ChartRenderer.Ringsegment>();
            if (p.Photovoltaik)
                segmente.Add(new ChartRenderer.Ringsegment(
                    MyResource.Resource.SIM_PHOTOVOLTAIK, k.PvStromproduktionMwh, R_PV));
            if (p.BHKW)
                segmente.Add(new ChartRenderer.Ringsegment(
                    MyResource.Resource.SIM_ERZEUGERNAME_BHKW,
                    k.BhkwStromproduktionMwh, R_BHKW_STROM));
            if (p.Stromspeicher && sim.Speicherergebnis != null)
                segmente.Add(new ChartRenderer.Ringsegment(
                    MyResource.Resource.SIM_STROMSPEICHER,
                    k.StromspeicherEntladungMwh, R_SPEICHER));

            segmente.Add(new ChartRenderer.Ringsegment(
                MyResource.Resource.SIMUEB_LEGENDE_NETZBEZUG, k.ReststromMwh, R_REST_GRAU));

            return segmente;
        }

        private byte[] BildRingWaerme()
        {
            ErgebnisPraesenz p = ErgebnisPraesenz.Ermitteln(sim);
            var k = Kennzahlen();
            double wbGesamt = _waermebedarf.Waermebedarf_Gesamt;

            double mitte = wbGesamt > 0 ? k.WaermeGesamtMwh * 100.0 / wbGesamt : 0.0;

            return ChartRenderer.Ring(MyResource.Resource.CHART_KACHEL_WAERMEBEDARFSDECKUNG,
                                      SegmenteWaerme(p, k), mitte, "%",
                                      MyResource.Resource.SIMUEB_RING_GEDECKT, false);
        }

        /// <summary>
        /// Der Ring „Stromdeckung" (B6). Er liest seit W8‑O‑5c / S1.2 dieselben
        /// <c>…Mwh</c>-Felder wie der Wärmering (Befund U5/U6); vorher rechnete er
        /// Photovoltaik und Speicherentladung selbst auf MWh und nahm BHKW und
        /// Reststrom fertig — drei Konventionen in EINEM Bild.
        /// </summary>
        private byte[] BildRingStrom()
        {
            ErgebnisPraesenz p = ErgebnisPraesenz.Ermitteln(sim);
            var k = Kennzahlen();
            double sbGesamt = k.StrombedarfMitEigenverbrauchMwh;

            // Befund W11-B36: Ohne Bedarf steht hier 0 und nicht 100.
            double mitte = sbGesamt > 0 ? k.StromGesamtMwh * 100.0 / sbGesamt : 0.0;

            // #222: Bei 0 % sagt die Unterzeile, WAS null ist — sonst steht dort ein
            // grauer Vollring mit einer nackten Null.
            string unterzeile = mitte > 0 ? MyResource.Resource.SIMUEB_RING_GEDECKT
                                          : MyResource.Resource.SIMUEB_RING_NETZBEZUG;

            return ChartRenderer.Ring(MyResource.Resource.CHART_KACHEL_STROMBEDARFSDECKUNG,
                                      SegmenteStrom(p, k), mitte, "%", unterzeile, false);
        }

        // ---- Die Wärmepumpenseite ---------------------------------------

        /// <summary>
        /// B2 auf der Wärmepumpenseite: BEDARF als Fläche (Heizwärme, Warmwasser),
        /// PRODUKTION als Säule (WP, Heizstab) — zwei Stapelgruppen in EINEM Bild.
        ///
        /// <para><b>Befund W11-B18 (A-Zeile):</b> Der Heizstab ist in BEIDEN Zweigen
        /// derselbe Anteil. Der Vorläufer zeichnete ihn sortiert als KUMULIERTE Kurve
        /// „WP-Produktion + Heizstab", chronologisch als eigenen Anteil — zwei Größen
        /// unter demselben Serienschlüssel.</para>
        /// </summary>
        private Zeichenmodell ModellWpProduktion(Bildauftrag a)
        {
            double[] bedarf = sim.simulation_wp.Waermebedarf_stuendlich;
            double[] ww = SimulationErgebnisCtrl.WarmwasserAnteil(_waermebedarf, bedarf);
            double[] heizung = new double[Kanalsatz.STUNDEN_JAHR];
            for (int n = 0; n < Kanalsatz.STUNDEN_JAHR && n < bedarf.Length; n++)
                heizung[n] = bedarf[n] - ww[n];

            bool alle = Alle(a);
            var stapel = new List<ChartRenderer.Reihe>();

            if (Gewaehlt(a, alle, "HEIZWAERMEBEDARF"))
                stapel.Add(Reihe(MyResource.Resource.CHART_LEGENDE_HEIZWAERMEBEDARF, heizung,
                                 F_BEDARF, ChartRenderer.Stapelart.Flaeche));
            if (Gewaehlt(a, alle, "WARMWASSERBEDARF"))
                stapel.Add(Reihe(MyResource.Resource.CHART_LEGENDE_WARMWASSERBEDARF, ww,
                                 F_WARMWASSER, ChartRenderer.Stapelart.Flaeche));
            if (Gewaehlt(a, alle, "WAERMEPRODUKTION"))
                stapel.Add(Reihe(MyResource.Resource.CHART_LEGENDE_WAERMEPRODUKTION,
                                 sim.simulation_wp.WP_Waermeproduktion_stuendlich, F_PRODUKTION,
                                 ChartRenderer.Stapelart.Saeule));
            if (Gewaehlt(a, alle, "HEIZSTAB"))
                stapel.Add(Reihe(MyResource.Resource.CHART_SEGMENT_HEIZSTAB,
                                 sim.simulation_wp.Heizstab_stuendlich, F_HEIZSTAB,
                                 ChartRenderer.Stapelart.Saeule));

            return ChartRenderer.ErzeugerStapelModell(
                MyResource.Resource.CHART_TITEL_WAERMELAST_JAHRESGANGLINIE,
                stapel, new List<ChartRenderer.Reihe>(), null,
                MyResource.Resource.CHART_ACHSE_WAERMELAST,
                ChartRenderer.Achse.Jahresstunden, a != null && a.Sortiert);
        }

        /// <summary>
        /// B3 auf der Wärmepumpenseite: der Stromverbrauch als EINE Linie über dem Jahr.
        /// </summary>
        private Zeichenmodell ModellWpStrom(Bildauftrag a)
        {
            double[] gesamt = _strombedarf.AddVectors(sim.simulation_wp.WP_Strombedarf_stuendlich,
                                                     sim.simulation_wp.Heizstab_stuendlich);

            return ChartRenderer.JahresverlaufModell(
                MyResource.Resource.CHART_TITEL_STROMBEDARF_JAHRESGANGLINIE,
                Kopie(gesamt), MyResource.Resource.CHART_ACHSE_STROMBEDARF, F_BEDARF);
        }

        /// <summary>
        /// B4 — die Streuwolke „Leistung über Außentemperatur". Die drei Reihen sind
        /// halbtransparent wie im Vorläufer (<c>ARGB(120, …)</c>).
        ///
        /// <para><b>Befund W11-B17 entfällt:</b> Die im Kommentar angekündigte Filterung
        /// „ein Wert je Temperatur" war auskommentiert, und die drei Kopierschleifen
        /// kopierten Array in Array gleicher Länge — 40 Zeilen totes Programm.</para>
        /// </summary>
        private byte[] BildStreuwolke(Bildauftrag a)
        {
            bool alle = Alle(a);
            bool mitBedarf = Gewaehlt(a, alle, "WAERMEBEDARF");
            bool mitHeizstab = Gewaehlt(a, alle, "HEIZSTAB");
            bool mitProduktion = Gewaehlt(a, alle, "WAERMEPRODUKTION");

            var bedarf = new List<(double, double)>();
            var produktion = new List<(double, double)>();
            var heizstab = new List<(double, double)>();

            double[] t = sim.simulation_wp.Temperatur;
            double[] prod = sim.simulation_wp.WP_Waermeproduktion_stuendlich;
            double[] bed = sim.simulation_wp.Waermebedarf_stuendlich;
            double[] hs = sim.simulation_wp.Heizstab_stuendlich;

            for (int n = 0; n < Kanalsatz.STUNDEN_JAHR && n < t.Length; n++)
            {
                double x = Math.Round(t[n], 1);
                if (mitBedarf) bedarf.Add((x, bed[n]));
                if (mitProduktion) produktion.Add((x, prod[n]));
                if (mitHeizstab) heizstab.Add((x, hs[n] > 0 ? prod[n] + hs[n] : 0.0));
            }

            var reihen = new List<ChartRenderer.Punktreihe>();
            if (mitBedarf)
                reihen.Add(new ChartRenderer.Punktreihe(MyResource.Resource.CHART_LEGENDE_WAERMEBEDARF,
                                                        bedarf, F_BEDARF.WithAlpha(120)));
            if (mitHeizstab)
                reihen.Add(new ChartRenderer.Punktreihe(MyResource.Resource.CHART_SEGMENT_HEIZSTAB,
                                                        heizstab, F_HEIZSTAB.WithAlpha(120)));
            if (mitProduktion)
                reihen.Add(new ChartRenderer.Punktreihe(MyResource.Resource.CHART_LEGENDE_WAERMEPRODUKTION,
                                                        produktion, F_PRODUKTION.WithAlpha(120)));

            return ChartRenderer.Streuwolke(
                MyResource.Resource.CHART_TITEL_LEISTUNG_UEBER_AUSSENTEMPERATUR,
                MyResource.Resource.CHART_ACHSE_TEMPERATUR,
                MyResource.Resource.SIM_SPALTE_LEISTUNG, reihen);
        }

        /// <summary>
        /// B7 — die Speichertemperaturen, Y-Achse ohne Nullpunkt.
        ///
        /// <para>Der Ausschnitt entsteht seit der Etappe DG-E3 im SVG: Der Zoom
        /// verschiebt die <c>viewBox</c> der Zeichenfläche, und die Temperaturachse
        /// bleibt dabei vollständig stehen (DG-E2-3, „Zoom nur auf der
        /// Zeitachse").</para>
        /// </summary>
        private Zeichenmodell ModellTemperaturen(Bildauftrag a)
        {
            var reihen = new List<ChartRenderer.Reihe>();
            foreach (Temperaturreihe r in Temperaturreihen())
                reihen.Add(new ChartRenderer.Reihe(r.Legende, Kopie(r.Werte), r.Farbe,
                                                   ChartRenderer.Stapelart.Keine, r.Gestrichelt));

            return ChartRenderer.TemperaturverlaufModell(
                MyResource.Resource.CHART_TITEL_SPEICHERTEMPERATUR, reihen, true);
        }

        // ---- Kessel, Solarthermie, BHKW, Photovoltaik -------------------

        /// <summary>
        /// Die drei Reihen des Kesselbildes. Seit dem Anwenderwunsch 09.09.2026
        /// (W11b‑B‑21) ist jede abwählbar; <c>null</c> als Reihenliste heißt
        /// weiter „alle“ (<see cref="Alle"/>), eine LEERE Liste heißt „keine“ —
        /// der Renderer zeichnet dann seinen Leerhinweis.
        /// </summary>
        private Zeichenmodell ModellKessel(Bildauftrag a)
        {
            bool sortiert = a != null && a.Sortiert;
            bool alle = Alle(a);

            var stapel = new List<ChartRenderer.Reihe>();
            if (Gewaehlt(a, alle, "WAERMEPRODUKTION"))
                stapel.Add(Reihe(MyResource.Resource.CHART_LEGENDE_WAERMEPRODUKTION_HEIZKESSEL,
                                 sim.simulation_spk.Kesselleistung_stuendlich, F_PRODUKTION,
                                 ChartRenderer.Stapelart.Saeule, sortiert ? 4f : 0f));

            var linien = new List<ChartRenderer.Reihe>();
            if (Gewaehlt(a, alle, "RESTWAERME"))
                linien.Add(Reihe(MyResource.Resource.CHART_SEGMENT_RESTWAERME,
                                 sim.simulation_spk.Restwaerme, F_REST));
            // Der Bedarf ZULETZT und damit ganz oben - er ist die Bezugsgröße
            // (Begründung im Blockkommentar :970-980). Hier der PROJEKTbedarf.
            if (Gewaehlt(a, alle, "WAERMEBEDARF"))
                linien.Add(Reihe(MyResource.Resource.CHART_LEGENDE_WAERMEBEDARF_GESAMT,
                                 _waermebedarf.Waermebedarf, F_BEDARF));

            return ChartRenderer.ErzeugerStapelModell(
                MyResource.Resource.CHART_TITEL_WAERMELAST_JAHRESGANGLINIE,
                stapel, linien, null, MyResource.Resource.CHART_ACHSE_WAERMELAST,
                sortiert ? ChartRenderer.Achse.Jahresstunden : ChartRenderer.Achse.Monate,
                sortiert);
        }

        /// <summary>
        /// Die zwei Linien der Solarthermie. Seit dem Anwenderwunsch 09.09.2026
        /// (W11b‑B‑19) ist jede abwählbar; <c>null</c> als Reihenliste heißt weiter
        /// „alle“ (<see cref="Alle"/>), eine LEERE Liste heißt „keine“ — der
        /// Renderer zeichnet dann seinen Leerhinweis.
        /// </summary>
        private Zeichenmodell ModellSolar(Bildauftrag a)
        {
            bool alle = Alle(a);
            var linien = new List<ChartRenderer.Reihe>();

            if (Gewaehlt(a, alle, "WAERMEBEDARF"))
                linien.Add(new ChartRenderer.Reihe(MyResource.Resource.CHART_LEGENDE_WAERMEBEDARF,
                                                   sim.simulation_solarthermie.Waermebedarf, F_BEDARF));
            if (Gewaehlt(a, alle, "WAERMEPRODUKTION"))
                linien.Add(new ChartRenderer.Reihe(MyResource.Resource.CHART_LEGENDE_WAERMEPRODUKTION,
                                                   sim.simulation_solarthermie.Waermeproduktion, F_PRODUKTION));

            return ChartRenderer.ErzeugerStapelModell(
                MyResource.Resource.CHART_TITEL_WAERMELAST_JAHRESGANGLINIE,
                new List<ChartRenderer.Reihe>(), linien, null,
                MyResource.Resource.CHART_ACHSE_WAERMELAST,
                ChartRenderer.Achse.Jahresstunden, false);
        }

        /// <summary>
        /// Die vier Reihen des BHKW-Bildes. Seit W11b‑B‑23 (09.09.2026) ist jede
        /// abwählbar — dieselbe Regel wie überall: <c>null</c> heißt „alle“
        /// (<see cref="Alle"/>), eine LEERE Liste heißt „keine“.
        /// </summary>
        private Zeichenmodell ModellBhkw(Bildauftrag a)
        {
            SimulationBHKW b = sim.simulation_bhkw;
            bool sortiert = a != null && a.Sortiert;
            bool alle = Alle(a);

            var stapel = new List<ChartRenderer.Reihe>();
            if (Gewaehlt(a, alle, "WAERMEPRODUKTION"))
                stapel.Add(Reihe(MyResource.Resource.CHART_LEGENDE_WAERMEPRODUKTION, b.waermeproduktion,
                                 F_PRODUKTION, ChartRenderer.Stapelart.Saeule, sortiert ? 4f : 0f));

            double[] ladung = Array.ConvertAll(b.Speicherladung_stuendlich, x => (double)x);

            var linien = new List<ChartRenderer.Reihe>();
            if (Gewaehlt(a, alle, "SPEICHERLADUNG"))
                linien.Add(Reihe(MyResource.Resource.SIMDET_BHKW_SERIE_SPEICHERLADUNG, ladung,
                                 F_SPEICHERLADUNG));
            if (Gewaehlt(a, alle, "RESTWAERME"))
                linien.Add(Reihe(MyResource.Resource.CHART_SEGMENT_RESTWAERME, b.waermerestbedarf, F_REST));
            // Der STUFENEINGANG zuletzt und damit oben - nicht der Projektbedarf
            // (Begründung im Blockkommentar :2140-2147).
            if (Gewaehlt(a, alle, "WAERMEBEDARF"))
                linien.Add(Reihe(MyResource.Resource.CHART_LEGENDE_WAERMEBEDARF, b.waermebedarf, F_BEDARF));

            return ChartRenderer.ErzeugerStapelModell(
                MyResource.Resource.CHART_TITEL_WAERMELAST_JAHRESGANGLINIE,
                stapel, linien, null, MyResource.Resource.CHART_ACHSE_WAERMELAST,
                sortiert ? ChartRenderer.Achse.Jahresstunden : ChartRenderer.Achse.Monate,
                sortiert);
        }

        /// <summary>
        /// B2 + B3 auf der PV-Seite: vier Reihen im Viertelstundenraster; der
        /// Speicherfüllstand geht in kWh auf die ZWEITE Y-Achse.
        ///
        /// <para><b>W11b‑B‑19 (09.09.2026):</b> Bis dahin waren Strombedarf und
        /// Photovoltaik FEST an — „keine Wahl“ (leere Liste) zeichnete sie
        /// trotzdem. Jetzt gilt hier dieselbe Regel wie in jedem anderen Bild mit
        /// wählbaren Reihen: <c>null</c> heißt „alle“ (<see cref="Alle"/>), eine
        /// LEERE Liste heißt „keine“, und der Renderer zeichnet dann seinen
        /// Leerhinweis. Die Reihenfolge der Reihen bleibt unverändert.</para>
        /// </summary>
        private Zeichenmodell ModellPv(Bildauftrag a)
        {
            bool alle = Alle(a);

            var linien = new List<ChartRenderer.Reihe>();
            if (Gewaehlt(a, alle, "UEBERSCHUSS"))
                linien.Add(Reihe(MyResource.Resource.CHART_LEGENDE_UEBERSCHUSS,
                                 sim.simulation_pv.Ueberschuss_viertelstunde, F_UEBERSCHUSS));
            if (Gewaehlt(a, alle, "STROMBEDARF"))
                linien.Add(Reihe(MyResource.Resource.CHART_ACHSE_STROMBEDARF,
                                 sim.simulation_pv.Strombedarf, F_BEDARF));
            // W11b-B-6: die ERZEUGUNG der Module (Stromproduktion_Theoretisch), nicht der
            // genutzte Anteil - der lag ohne Strombedarf auf 0, die Kurve war leer, und
            // die Tabelle darunter wies 13 MWh aus. Der Vorlaeufer (:4574) zeichnete
            // dieselbe genutzte Reihe; das war seine Schwaeche, nicht die des Ports.
            // Die Viertelstunden kommen aus derselben Umrechnung wie die Bestandsreihen.
            if (Gewaehlt(a, alle, "PHOTOVOLTAIK"))
                linien.Add(Reihe(MyResource.Resource.SIM_PHOTOVOLTAIK,
                                 sim.simulation_pv.Stundenwerte_zu_viertelstunden(
                                     sim.simulation_pv.Stromproduktion_Theoretisch), F_PV));

            // #234: Die zweite Achse nimmt seither eine LISTE; hier steht genau eine
            // Reihe darauf — der eine Stromspeicher des Projekts.
            List<ChartRenderer.Reihe> zweite = Gewaehlt(a, alle, "SPEICHERFUELLSTAND")
                ? new List<ChartRenderer.Reihe>
                  { Reihe(MyResource.Resource.PSP_CHECKBOX_SPEICHERFUELLSTAND,
                          sim.Speicherfuellstand_viertelstuendlich, F_SPEICHER) }
                : null;

            return ChartRenderer.ErzeugerStapelModell(
                MyResource.Resource.CHART_TITEL_STROMBEDARF_PV_JAHRESGANGLINIE,
                new List<ChartRenderer.Reihe>(), linien, null,
                MyResource.Resource.CHART_ACHSE_LEISTUNG,
                ChartRenderer.Achse.Monate, false,
                zweite, MyResource.Resource.CHART_ACHSE_SPEICHER_KWH);
        }

        /// <summary>
        /// <b>Lastgang und Speicherbetrieb in EINEM Bild</b> (Anwenderwunsch W11b‑B‑26,
        /// 10.09.2026): Netzbezug ohne Speicher, Netzbezug mit Speicher, die
        /// Speicherleistung mit Vorzeichen — und der LADEZUSTAND auf einer zweiten Achse
        /// rechts.
        ///
        /// <para><b>Es löst das SoC-Bild ab</b> und zeigt es nicht daneben noch einmal:
        /// Der Ladezustand allein beantwortet die Frage des Anwenders nicht („um wie viel
        /// senkt der Speicher den Bezug, und wann tut er es"), und zweimal dieselbe Kurve
        /// auf einem Reiter ist eine Kurve zu viel. Der Datenzoom aus W11b‑B‑24 bleibt —
        /// ein Speicher lädt und entlädt im TAGESrhythmus, und in der Jahresansicht liegen
        /// rund 40 Viertelstunden auf einem Bildpunkt.</para>
        ///
        /// <para><b>Gezeichnet wird im Kern</b> (<see cref="SpeicherBetriebsbild"/>) — die
        /// Hülle sucht nur die zwei Bestandteile des Laufs zusammen: den EINGANG (Lastgang
        /// und Erzeugung, seit W11b‑B‑26 im Lauf-Kontext) und das ERGEBNIS (SoC, Ladung,
        /// Entladung). Ohne einen von beiden gibt es kein Bild, und der Baustein sagt, dass
        /// keines da ist.</para>
        /// </summary>
        private Zeichenmodell ModellSpeicherBetrieb(Bildauftrag a)
        {
            SpeicherErgebnis erg = sim.Speicherergebnis;
            StromspeicherLaufKontext kontext = sim.Speicherkontext;
            SpeicherEingang eingang = kontext == null ? null : kontext.Eingang;
            if (erg == null || eingang == null) return null;

            SpeicherParameter p = kontext.Parameter;
            double dt = p != null && p.DtH > 0.0 ? p.DtH : StromspeicherSimCtrl.INTERVALL_H;

            // DIE REIHEN KOMMEN AUS DERSELBEN STELLE WIE FUER DAS PNG
            // (SpeicherBetriebsbild) - nur der Ausgabeweg ist ein anderer: Das
            // Modell geht in den SVG-Baustein, die Bytes weiter in den Bericht.
            return ChartRenderer.SpeicherbetriebModell(
                MyResource.Resource.SP_CHART_TITEL_BETRIEB,
                SpeicherBetriebsbild.Leistungsreihen(eingang, erg, dt, a.Reihen),
                MyResource.Resource.PEAK_CHART_Y,
                SpeicherBetriebsbild.Ladezustand(erg, a.Reihen),
                MyResource.Resource.PEAK_CHART_Y2,
                a.Sortiert);
        }

        // ---- B6: der Monatsstapel der Autarkie-Analyse -------------------

        /// <summary>
        /// Die zwölf Monatssäulen — wörtlich <c>FillMonthlyChart</c> :431-474: feste
        /// 730-h-Monate, im Viertelstundenraster also 2 920 Intervalle je Monat.
        /// </summary>
        private byte[] BildAutarkie(double kwh)
        {
            if (_autarkieSpeicher == null || _autarkieLast == null) AutarkieRechnen(kwh);
            if (_autarkieSpeicher == null) return null;

            double[] direkt = new double[12];
            double[] ausSpeicher = new double[12];
            double[] luecke = new double[12];

            for (int m = 0; m < 12; m++)
            {
                for (int v = 0; v < 2920; v++)
                {
                    int i = m * 2920 + v;
                    if (i >= _autarkieLast.Length) break;

                    IntervallEnergien e = Vorverarbeitung.Berechne(
                        _autarkieLast[i], _autarkiePv[i], 0.0,
                        StromspeicherSimCtrl.INTERVALL_H, true, false);

                    direkt[m] += e.EDirektKwh;
                    double entnahme = _autarkieSpeicher.EntladungAcKwh[i];
                    ausSpeicher[m] += entnahme;
                    luecke[m] += e.EDefizitKwh - entnahme;
                }
            }

            var reihen = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe(MyResource.Resource.CHART_LEGENDE_EIGENVERBRAUCH_DIREKT,
                                        direkt, SKColors.Gold),
                new ChartRenderer.Reihe(MyResource.Resource.CHART_LEGENDE_EIGENVERBRAUCH_SPEICHER,
                                        ausSpeicher, SKColors.LightGreen),
                new ChartRenderer.Reihe(MyResource.Resource.CHART_LEGENDE_AUTARKIELUECKE,
                                        luecke, SKColors.Red)
            };

            return ChartRenderer.MonatsStapel(
                MyResource.Resource.CHART_ACHSE_ENERGIEBEDARF_DECKUNG, "kWh", reihen);
        }

        // ---- Die beiden Ganglinien-Reiter -------------------------------

        /// <summary>
        /// Der Wärmegang (B2 + B3). <c>kanal &lt; 0</c> = Produktion je Erzeuger, sonst
        /// die Deckung des Kanals — die Kernachse der E2-Umschaltung
        /// (<c>VektorenSetzen</c> :424-453).
        /// </summary>
        private Zeichenmodell ModellWaermegang(Bildauftrag a)
        {
            int kanal = a.Kanal;
            IReadOnlyList<string> wahl = a.Reihen ?? new List<string>();

            double[] Vektor(string schluessel)
            {
                switch (schluessel)
                {
                    case "WAERMEPUMPE":
                        return kanal < 0 ? sim.simulation_wp.WP_Waermeproduktion_stuendlich
                                         : sim.DeckungKanalStuendlich(ProjektPuffer.TYP_WP, kanal);
                    case "HEIZSTAB":
                        return kanal < 0 ? sim.simulation_wp.Heizstab_stuendlich
                                         : sim.HeizstabKanalStuendlich(kanal);
                    case "HEIZKESSEL":
                        return kanal < 0 ? sim.simulation_spk.Kesselleistung_stuendlich
                                         : sim.DeckungKanalStuendlich(ProjektPuffer.TYP_KESSEL, kanal);
                    case "SOLARTHERMIE":
                        return kanal < 0
                            ? Array.ConvertAll(sim.simulation_solarthermie.Waermeproduktion, x => (double)x)
                            : sim.DeckungKanalStuendlich(ProjektPuffer.TYP_SOLARTHERMIE, kanal);
                    case "BHKW_WAERME":
                        return kanal < 0 ? sim.simulation_bhkw.waermeproduktion
                                         : sim.DeckungKanalStuendlich(ProjektPuffer.TYP_BHKW, kanal);
                    default:
                        return null;
                }
            }

            var farben = new Dictionary<string, SKColor>
            {
                { "WAERMEPUMPE", F_WAERMEPUMPE }, { "HEIZSTAB", F_HEIZSTAB },
                { "HEIZKESSEL", F_KESSEL }, { "SOLARTHERMIE", F_SOLAR },
                { "BHKW_WAERME", F_BHKW }
            };

            var stapel = new List<ChartRenderer.Reihe>();
            foreach (Ganglinienreihe r in WaermegangDaten(ErgebnisPraesenz.Ermitteln(sim)).Erzeuger)
            {
                if (!r.Vorhanden || !wahl.Contains(r.Schluessel)) continue;
                double[] werte = Vektor(r.Schluessel);
                if (werte == null) continue;
                stapel.Add(Reihe(r.Text, werte, farben[r.Schluessel],
                                 ChartRenderer.Stapelart.Saeule, a.Sortiert ? 4f : 0f));
            }

            // Die Speicherfüllstände auf die ZWEITE Achse (Anwenderrückmeldung
            // 12.09.2026, #234): Sie führen kWh, alles andere in diesem Bild kW —
            // und eine Achse, die beides trägt, sagt bei keiner der zwei Größen die
            // Wahrheit. Bis dahin lagen sie auf der PRIMÄRachse und der Wärmebedarf
            // (kW!) auf der zweiten; es war genau verkehrt herum.
            var speicherreihen = new List<ChartRenderer.Reihe>();
            List<SimulationPufferspeicher> speicher = sim.AlleSpeicher();
            int nummer = 0;
            for (int i = 0; i < speicher.Count; i++)
            {
                SimulationPufferspeicher sp = speicher[i];
                if (sp == null) continue;
                string schluessel = sp.Schluessel(i);
                if (wahl.Contains(schluessel))
                    speicherreihen.Add(Reihe(sp.BezeichnerAnzeige(), sp.SOC_stuendlich,
                                             F_SPEICHERREIHEN[nummer % F_SPEICHERREIHEN.Length]));
                nummer++;
            }

            // Die KONTUR „Summe Wärmeerzeugung" liegt UNTER dem Stapel - sie ist die
            // Summe und darf ihn nicht überdecken (NavigatorWaerme :631-635).
            //
            // SIE IST ABSCHALTBAR WIE IHRE SUMMANDEN (Anwenderwunsch 15.09.2026): Bis
            // hierher entstand sie allein daraus, dass der Stapel eine Reihe trug - ein
            // Schalter dafür fehlte. Jetzt hängt sie am Schlüssel „GESAMT" wie im
            // Stromgang. Der Zeichenweg bleibt Zeile für Zeile derselbe; bei gewählter
            // Summe ist das Bild dasselbe wie zuvor.
            ChartRenderer.Reihe kontur = null;
            if (stapel.Count > 0 && wahl.Contains("GESAMT"))
            {
                double[] gesamt = new double[Kanalsatz.STUNDEN_JAHR];
                foreach (ChartRenderer.Reihe r in stapel)
                    for (int h = 0; h < gesamt.Length && h < r.Werte.Length; h++) gesamt[h] += r.Werte[h];

                kontur = new ChartRenderer.Reihe(MyResource.Resource.CHART_LEGENDE_SUMME_WAERMEERZEUGUNG,
                                                 gesamt, F_GESAMT,
                                                 ChartRenderer.Stapelart.Keine, false, 4f);
            }

            // Die Bedarfslinie liegt auf der PRIMÄRACHSE (#234): Wärmelast und
            // Produktion sind beide eine Leistung in kW und gehören auf EINE Skala —
            // nur so ist abzulesen, ob die Erzeuger den Bedarf decken. Sie steht als
            // letzte Linie und damit ganz oben (dieselbe Zeichenlage wie im Bestand).
            var linien = new List<ChartRenderer.Reihe>();
            if (wahl.Contains("WAERMEBEDARF"))
            {
                double[] bedarf = kanal < 0
                    ? _waermebedarf.Waermebedarf
                    : SimulationControl.BedarfKanalStuendlich(_waermebedarf, kanal);
                linien.Add(Reihe(MyResource.Resource.CHART_LEGENDE_WAERMEBEDARF, bedarf,
                                 SKColors.DarkCyan, ChartRenderer.Stapelart.Keine, 2f));
            }

            string titel = kanal < 0
                ? MyResource.Resource.CHART_TITEL_WAERMEPRODUKTION_JAHRESGANGLINIE
                : string.Format(MyResource.Resource.CHART_TITEL_DECKUNG_JE_BEDARFSART, KANALNAMEN[kanal]);

            // Die zweite Achse steht NUR, wenn ein Speicher gewählt ist — sonst nimmt
            // die Zeichenfläche die vollen 1 100 Bildpunkte wie jedes Bild ohne sie.
            return ChartRenderer.ErzeugerStapelModell(
                titel, stapel, linien, kontur,
                MyResource.Resource.CHART_ACHSE_LEISTUNG,
                a.Sortiert ? ChartRenderer.Achse.Jahresstunden : ChartRenderer.Achse.Monate,
                a.Sortiert,
                speicherreihen.Count > 0 ? speicherreihen : null,
                MyResource.Resource.CHART_ACHSE_SPEICHERINHALT_KWH);
        }

        /// <summary>
        /// Der Stromgang (B2): der Verbrauchsstapel, die Erzeugungslinien darüber und
        /// die Kontrolllinie „Summe Stromverbrauch" — im Viertelstundenraster.
        /// </summary>
        private Zeichenmodell ModellStromgang(Bildauftrag a)
        {
            IReadOnlyList<string> wahl = a.Reihen ?? new List<string>();

            double[] Viertel(double[] stunden) => sim.Stundenwerte_zu_viertelstunden(stunden);

            var stapel = new List<ChartRenderer.Reihe>();
            void Stapel(string schluessel, string name, double[] werte, SKColor farbe)
            {
                if (wahl.Contains(schluessel) && werte != null)
                    stapel.Add(Reihe(name, werte, farbe, ChartRenderer.Stapelart.Saeule,
                                     a.Sortiert ? 4f : 0f));
            }

            Stapel("PROFIL_LASTGANG", MyResource.Resource.CHART_LEGENDE_PROFIL_LASTGANG,
                   _strombedarf.Strombedarf_viertelStundenwerte, F_LASTGANG);
            Stapel("WAERMEPUMPE", MyResource.Resource.SIM_ERZEUGERNAME_WAERMEPUMPE,
                   Viertel(sim.simulation_wp.WP_Strombedarf_stuendlich), F_WAERMEPUMPE);
            Stapel("HEIZSTAB", MyResource.Resource.CHART_SEGMENT_HEIZSTAB,
                   Viertel(sim.simulation_wp.Heizstab_stuendlich), F_HEIZSTAB);
            Stapel("HEIZKESSEL", MyResource.Resource.SIM_ERZEUGERNAME_HEIZKESSEL,
                   Viertel(sim.simulation_spk.Strombedarf_stuendlich), F_KESSEL);

            var linien = new List<ChartRenderer.Reihe>();
            if (wahl.Contains("BHKW_STROM"))
                linien.Add(Reihe(MyResource.Resource.SIM_ERZEUGERNAME_BHKW,
                                 Viertel(sim.simulation_bhkw.stromproduktion), F_BHKW_STROM));
            if (wahl.Contains("PV"))
                linien.Add(Reihe(MyResource.Resource.SIM_PHOTOVOLTAIK,
                                 sim.simulation_pv.Stromproduktion_viertelstunde, F_PV));

            // „GESAMT" ist die Kontrolllinie über allem (:220-221). Sie addiert
            // Lastgang, Wärmepumpe, Heizstab und Heizkessel - das ist der
            // STROMVERBRAUCH und nicht die Stromerzeugung; BHKW und Photovoltaik
            // stehen als Erzeugungslinien daneben und gehen gerade nicht ein.
            ChartRenderer.Reihe kontur = null;
            if (wahl.Contains("GESAMT"))
            {
                double[] gesamt = _strombedarf.AddVectors(
                    _strombedarf.AddVectors(_strombedarf.Strombedarf_viertelStundenwerte,
                                            Viertel(sim.simulation_wp.WP_Strombedarf_stuendlich)),
                    _strombedarf.AddVectors(Viertel(sim.simulation_wp.Heizstab_stuendlich),
                                            Viertel(sim.simulation_spk.Strombedarf_stuendlich)));

                kontur = new ChartRenderer.Reihe(MyResource.Resource.CHART_LEGENDE_SUMME_STROMVERBRAUCH,
                                                 Kopie(gesamt), F_GESAMT,
                                                 ChartRenderer.Stapelart.Keine, false, 2f);
            }

            return ChartRenderer.ErzeugerStapelModell(
                MyResource.Resource.CHART_TITEL_STROMBEDARF_STROMVERBRAUCH_JAHRESGANGLINIE,
                stapel, linien, kontur, MyResource.Resource.CHART_ACHSE_LEISTUNG,
                a.Sortiert ? ChartRenderer.Achse.Jahresstunden : ChartRenderer.Achse.Monate,
                a.Sortiert);
        }

        // =================================================================
        // Die Farbe einer Reihe (Farbrollen, Bedienung Teil 2)
        // =================================================================

        /// <summary>
        /// Der Klick auf das Farbfeld eines Legendeneintrags landet hier: Die Rolle
        /// bekommt anwendungsweit diese Farbe (<c>Diagrammfarben.Setze</c> schreibt
        /// die Einstellung und speist <c>Farbpalette.Aktuell</c>). Danach trägt sie
        /// jedes Diagramm und jeder Bericht — beide malen über dieselbe Palette.
        /// </summary>
        private static Task FarbeSetzen(Farbrolle rolle, Farbe farbe)
        {
            Diagrammfarben.Setze(rolle, farbe);
            return Task.CompletedTask;
        }

        /// <summary>„Hausfarbe": Der Eintrag fällt aus der Einstellung.</summary>
        private static Task FarbeZuruecksetzen(Farbrolle rolle)
        {
            Diagrammfarben.Zuruecksetzen(rolle);
            return Task.CompletedTask;
        }
    }
}
