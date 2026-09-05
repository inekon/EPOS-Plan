using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SkiaSharp;
using WindowsFormsApplication1;

namespace ChartProben
{
    /// <summary>
    /// ChartProben (Umsetzungskonzept iOS, Paket iU7-3) — der Nachweis, dass
    /// <see cref="ChartRenderer"/> nach der Portierung auf SkiaSharp OHNE Windows
    /// zeichnet.
    ///
    /// <para><b>Synthetische Daten, keine Datenbank.</b> Die Reihen entstehen hier aus
    /// Sinus und Rampe — fest verdrahtet, ohne Zufall und ohne Datei. Die Probe soll
    /// den RENDERER pruefen und nicht den Rechenkern; und sie muss auf einem nackten
    /// CI-Abbild ohne Kenndaten.sqlite durchlaufen.</para>
    ///
    /// <para><b>Was geprueft wird</b>, je Bild: PNG-Signatur, Bildmasse (die Werte aus
    /// dem Bestand — sie duerfen sich durch die Portierung nicht geaendert haben), das
    /// Bild ist nicht einfarbig, jede erwartete Palettenfarbe kommt vor, und zweimal
    /// Rendern liefert byte-gleiche Dateien (Determinismus — der Bericht darf sich
    /// zwischen zwei Laeufen nicht unterscheiden).</para>
    ///
    /// <para>Rueckgabe 0, wenn alles gruen ist, sonst 1.</para>
    /// </summary>
    internal static class Program
    {
        private const int STUNDEN = 8760;

        /// <summary>
        /// Die Kostenprofil-Linie, wie sie im fertigen Bild ANKOMMT.
        /// <c>ChartRenderer.C_PROFIL</c> ist halbtransparent (Deckung 180 von 255,
        /// woertlich aus <c>Form_Kostenprofil</c>); ueber der weissen Flaeche
        /// entsteht daraus 75/146/75. Die Pixelpruefung vergleicht exakt, deshalb
        /// steht hier die gemischte Farbe und nicht die Palettenfarbe - dasselbe
        /// Thema wie bei den halbtransparenten Speichertemperaturen, dort ist die
        /// untere Schicht deshalb gar nicht geprueft.
        /// </summary>
        private static readonly SKColor PROFILLINIE_AUF_WEISS = new SKColor(75, 146, 75);

        /// <summary>
        /// Die Flaeche des Stundenprofils, wie sie im fertigen Bild ANKOMMT (iU9-W8.0c).
        /// <c>ChartRenderer.C_PROFILFLAECHE</c> ist Blau mit der Deckung 100 von 255
        /// (woertlich aus <c>Form_EingStromTyp</c>: <c>Color.FromArgb(100, Color.Blue)</c>);
        /// ueber der weissen Flaeche entsteht daraus 155/155/255 - derselbe Grund wie bei
        /// der Kostenprofil-Linie darueber.
        /// </summary>
        private static readonly SKColor PROFILFLAECHE_AUF_WEISS = new SKColor(155, 155, 255);

        /// <summary>
        /// Die QUELLTEMPERATUR-Linie des Jahresgangs, wie sie im fertigen Bild ANKOMMT
        /// (iU9-W10a.0d). <c>ChartRenderer.C_QUELLTEMPERATUR</c> ist SaddleBrown mit der
        /// Deckung 200 von 255 (woertlich aus <c>Form_QuelleErdreich</c>:
        /// <c>Color.FromArgb(200, Color.SaddleBrown)</c>); ueber der weissen Flaeche
        /// entsteht daraus 164/109/70 - derselbe Grund wie bei den beiden Farben darueber.
        ///
        /// <para>Die zweite Reihe (Aussentemperatur, SteelBlue mit Deckung 90 und
        /// Strichstaerke 1) wird NICHT geprueft: Eine ein Pixel breite, stark
        /// durchscheinende Linie geht in der Kantenglaettung auf - es gibt kein Pixel, das
        /// die Mischfarbe exakt traegt. Dasselbe Zugestaendnis macht die Pruefung bei den
        /// Speichertemperaturen.</para>
        /// </summary>
        private static readonly SKColor QUELLTEMPERATUR_AUF_WEISS = new SKColor(164, 109, 70);

        private static int _verstoesse;
        private static int _bilder;

        private static int Main(string[] args)
        {
            try { Console.OutputEncoding = new UTF8Encoding(false); } catch { }

            string ziel = Argument(args, "--ziel") ?? Path.Combine(Wurzel(), "artifacts", "chartproben");
            Directory.CreateDirectory(ziel);

            Console.WriteLine("ChartProben - plattformfreier Renderer-Nachweis (Paket iU7-3)");
            Console.WriteLine("Zielordner: " + ziel);
            Console.WriteLine("Schriftart: " + Schriftbefund());
            Console.WriteLine();
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "{0,-26} {1,-12} {2,10} {3,8} {4,7} {5}",
                "Bildtyp", "Masse", "Bytes", "Farben", "determ.", "Ergebnis"));
            Console.WriteLine(new string('-', 92));

            ZeitreihenSatz z = SyntheticherSatz();
            List<VerlaufSerie> serien = Beispielserien();

            // 1 - Kuchen
            var kuchen = new List<ChartRenderer.Segment>
            {
                new ChartRenderer.Segment("Solarthermie", 12.0, ChartRenderer.C_SOLAR),
                new ChartRenderer.Segment("Waermepumpe", 48.0, ChartRenderer.C_WP),
                new ChartRenderer.Segment("BHKW", 26.0, ChartRenderer.C_BHKW),
                new ChartRenderer.Segment("Spitzenkessel", 14.0, ChartRenderer.C_KESSEL)
            };
            Pruefe(ziel, "kuchen", 960, 600,
                   new[] { ChartRenderer.C_SOLAR, ChartRenderer.C_WP,
                           ChartRenderer.C_BHKW, ChartRenderer.C_KESSEL },
                   () => ChartRenderer.Kuchen("Waermedeckung", kuchen));

            // 2 - Balken (Hoehe = 150 + n * 64)
            var balken = new List<ChartRenderer.Balken>
            {
                new ChartRenderer.Balken("Stamm", 412.0, true),
                new ChartRenderer.Balken("Variante A", 355.0, false),
                new ChartRenderer.Balken("Variante B", 298.0, false),
                new ChartRenderer.Balken("Variante C", 181.0, false)
            };
            Pruefe(ziel, "balken_horizontal", 1240, 150 + balken.Count * 64,
                   new[] { ChartRenderer.C_STAMM, ChartRenderer.C_WP },
                   () => ChartRenderer.BalkenHorizontal("Brennstoffeinsatz", "MWh/a", balken));

            // 3 - Jahresverlauf Waerme (gestapelte Flaechen)
            Pruefe(ziel, "jahresverlauf_waerme", 1240, 560,
                   new[] { ChartRenderer.C_SOLAR, ChartRenderer.C_WP, ChartRenderer.C_NETZ,
                           ChartRenderer.C_BHKW, ChartRenderer.C_KESSEL, ChartRenderer.C_BEDARF },
                   () => ChartRenderer.JahresverlaufWaerme(z));

            // 4 - Jahresdauerlinie
            Pruefe(ziel, "dauerlinie_waerme", 1240, 560,
                   new[] { ChartRenderer.C_BEDARF, ChartRenderer.C_SOLAR, ChartRenderer.C_WP,
                           ChartRenderer.C_BHKW, ChartRenderer.C_KESSEL },
                   () => ChartRenderer.DauerlinieWaerme(z));

            // 5 - Strombilanz im Monatsverlauf
            Pruefe(ziel, "strombilanz_monate", 1240, 560,
                   new[] { ChartRenderer.C_PV, ChartRenderer.C_BHKW, ChartRenderer.C_KESSEL,
                           ChartRenderer.C_NETZ, ChartRenderer.C_BEDARF },
                   () => ChartRenderer.StrombilanzMonate(z));

            // 6 - Speicherverlauf (drei Wochenfenster)
            Pruefe(ziel, "speicherverlauf", 1240, 520,
                   new[] { ChartRenderer.C_SPEICHER[0], ChartRenderer.C_SPEICHER[1],
                           ChartRenderer.C_PV },
                   () => ChartRenderer.Speicherverlauf(z));

            // 7 - Speichertemperaturen (untere Schicht halbtransparent, deshalb nicht geprueft)
            Pruefe(ziel, "speichertemperaturen", 1240, 560,
                   new[] { ChartRenderer.C_SPEICHER[0], ChartRenderer.C_SPEICHER[1],
                           ChartRenderer.C_NETZ },
                   () => ChartRenderer.Speichertemperaturen(z));

            // 8 - Kapitalwert-Verlauf, Differenzbild (ohne Stammlinie)
            Pruefe(ziel, "kapitalwert_differenz", 1240, 620,
                   new[] { ChartRenderer.C_SERIEN[0], ChartRenderer.C_SERIEN[1] },
                   () => ChartRenderer.KapitalwertVerlauf("Differenz zur Stamm-Referenz",
                            ChartRenderer.VerlaufsReihen(serien, false),
                            "Synthetische Probendaten, Paket iU7-3"));

            // 9 - Kapitalwert-Verlauf, Absolutbild (mit Stammlinie)
            Pruefe(ziel, "kapitalwert_absolut", 1240, 620,
                   new[] { ChartRenderer.C_STAMM, ChartRenderer.C_SERIEN[0], ChartRenderer.C_SERIEN[1] },
                   () => ChartRenderer.KapitalwertVerlauf("Kumulierte Barwerte je Projekt",
                            ChartRenderer.VerlaufsReihen(serien, true), null));

            // 10 - Kostenprofil (iU9-W3.4): 8 760 Stundenpreise ueber der Monatsachse.
            // Die Reihe laeuft ins Negative, damit die gestrichelte Nulllinie mitgeprueft
            // wird - ein Wochenwert ist eine Abweichung und darf den Monatswert
            // unter null ziehen.
            double[] profil = Preisprofil();
            Pruefe(ziel, "kostenprofil", 1296, 780,
                   new[] { PROFILLINIE_AUF_WEISS },
                   () => ChartRenderer.Kostenprofil("Kostenprofil", profil, "ct/kWh", "Monat"));

            // 11/12 - Kennlinien (iU9-W7.0c): drei Vorlaufstufen (35/45/55 °C) ueber
            // der Aussentemperatur -15…+20 °C. Zwei Bilder aus DENSELBEN Stuetzstellen,
            // wie die beiden Reiterblaetter "COP" und "Leistung" der Waermepumpenmasken:
            // Kreismarken fuer den COP, Kreuzmarken fuer die Leistung.
            var kennlinienCop = Kennlinien(cop: true);
            var kennlinienLeistung = Kennlinien(cop: false);

            Pruefe(ziel, "kennlinien_cop", 968, 520,
                   new[] { ChartRenderer.C_SERIEN[0], ChartRenderer.C_SERIEN[1], ChartRenderer.C_SERIEN[2] },
                   () => ChartRenderer.Kennlinien("Kennlinien COP", "COP", "Temperatur",
                            kennlinienCop, ChartRenderer.Kennlinienmarke.Kreis));

            Pruefe(ziel, "kennlinien_leistung", 968, 520,
                   new[] { ChartRenderer.C_SERIEN[0], ChartRenderer.C_SERIEN[1], ChartRenderer.C_SERIEN[2] },
                   () => ChartRenderer.Kennlinien("Kennlinien Leistung", "Leistung", "Temperatur",
                            kennlinienLeistung, ChartRenderer.Kennlinienmarke.Kreuz));

            // 13/14/15 - Bedarfsbilder (iU9-W8.0c): Monatssaeulen, Stundenprofil und
            // Jahresverlauf. Sie ersetzen die Charts der zehn Bedarfsmasken der Welle 8.
            double[] monatswerte = Monatsreihe();
            Pruefe(ziel, "monatssaeulen", 978, 542,
                   new[] { SKColors.YellowGreen },
                   () => ChartRenderer.MonatsSaeulen("Strombedarf Monatsuebersicht", monatswerte,
                            SKColors.YellowGreen, "MWh"));

            // Das Stundenprofil traegt eine HALBTRANSPARENTE Flaeche; geprueft wird die
            // Randlinie (deckend) und die Mischfarbe der Flaeche ueber Weiss.
            double[] wochenprofil = Wochenprofil();
            Pruefe(ziel, "stundenprofil_woche", 1244, 464,
                   new[] { ChartRenderer.C_PROFILLINIE, PROFILFLAECHE_AUF_WEISS },
                   () => ChartRenderer.Stundenprofil("Wochenwerte", wochenprofil, 24,
                            "Wochenstunde (1..168)", "Verteilung"));

            double[] jahresverlauf = Jahresreihe(140, 90, 30, 0, Math.PI / 2);
            Pruefe(ziel, "jahresverlauf_bedarf", 978, 542,
                   new[] { SKColors.SteelBlue },
                   () => ChartRenderer.Jahresverlauf("Jahresuebersicht", jahresverlauf,
                            "Waermebedarf [kW]", SKColors.SteelBlue));

            // 15b/15c - DIE ZWEI ZEITSTUFEN des Bedarfs-Ergebnisdialogs
            // (Anwenderwunsch W8-E-2, Windows-Abnahme 05.09.2026).
            //
            // "Grafik Strombedarf soll ausser dem Jahr auch Woche und Tag zeigen." Beides
            // ist DERSELBE Zeichenweg mit einem Achsenfenster - ein neues Renderer-Bild
            // braucht es nicht. Geprueft wird hier, dass die zwei Ausschnitte dieselben
            // Masse und Farben tragen wie die Vollansicht und deterministisch sind; DASS
            // sie ueberhaupt etwas anderes zeigen, sagt die Gegenprobe weiter unten.
            //
            // Woche 27 (Stunde 4 368 bis 4 536) und Tag 200 (Stunde 4 776 bis 4 800)
            // liegen mitten im Sommer - dort ist die Reihe weder am Rand noch auf ihrem
            // Jahreshoechstwert, und ein Zuschnittfehler faellt auf.
            var fensterWoche = new ChartRenderer.Achsenfenster(26 * 168, 27 * 168);
            var fensterTag = new ChartRenderer.Achsenfenster(199 * 24, 200 * 24);

            Pruefe(ziel, "jahresverlauf_woche", 978, 542,
                   new[] { SKColors.SteelBlue },
                   () => ChartRenderer.Jahresverlauf("Strombedarf Ganglinie", jahresverlauf,
                            "Strombedarf [kW]", SKColors.SteelBlue, fensterWoche));

            Pruefe(ziel, "jahresverlauf_tag", 978, 542,
                   new[] { SKColors.SteelBlue },
                   () => ChartRenderer.Jahresverlauf("Strombedarf Ganglinie", jahresverlauf,
                            "Strombedarf [kW]", SKColors.SteelBlue, fensterTag));

            // 16 - Jahresgang zweireihig (iU9-W10a.0d): Quelltemperatur und
            // Aussentemperatur ueber der Monatsachse 0…12, Legende oben. Er loest das
            // Chart des Erdreich-Dialogs ab. Beide Reihen sind TEMPERATUREN und laufen
            // deshalb ins Negative - damit wird die gestrichelte Nulllinie mitgeprueft.
            double[] quelltemperatur = Temperaturreihe(9, 4, 0, -Math.PI / 2);
            double[] aussentemperatur = Temperaturreihe(9, 14, 3, -Math.PI / 2);
            Pruefe(ziel, "jahresgang_erdreich", 1304, 440,
                   new[] { QUELLTEMPERATUR_AUF_WEISS },
                   () => ChartRenderer.Jahresgang("Jahresgang der Quelltemperatur",
                            new List<ChartRenderer.Reihe>
                            {
                                new ChartRenderer.Reihe("Quelltemperatur", quelltemperatur,
                                                        ChartRenderer.C_QUELLTEMPERATUR),
                                new ChartRenderer.Reihe("Aussentemperatur", aussentemperatur,
                                                        ChartRenderer.C_AUSSENTEMPERATUR)
                            },
                            "Monat", "Temperatur [°C]"));

            // =========================================================================
            // 16b/16c - die ZWEI KLIMABILDER der Welle 14c (iU9-W14c.7)
            // =========================================================================
            //
            // Sie halten die beiden Faelle GETRENNT fest, die der Erdreich-Jahresgang
            // nicht abdeckt:
            //
            //  * EINE Reihe mit VORZEICHEN (Jahrestemperatur): Die Achse muss ins
            //    Negative reichen und die gestrichelte Nulllinie zeichnen. Der
            //    Vorlaeufer nahm hier yAxis.Min() - denselben Wert liefert Jahresgang
            //    ohne minimumNull.
            //  * EINE Reihe mit NULLPUNKTBINDUNG (Sonnenwinkel): Der Vorlaeufer setzte
            //    YMinValue = 0 fest (Form_Klimadaten:119). Ohne den Schalter aus
            //    W14c.0j begaenne die Achse am kleinsten Wert - das Bild saehe sichtbar
            //    anders aus. Genau das prueft die zweite Probe: minimumNull = true.
            //
            // Der Sonnenwinkel laeuft im Jahresgang zwischen rund 15 und 62 Grad
            // (Stuttgart); die Reihe bildet das nach, ohne je unter null zu gehen.
            double[] klimaTemperatur = Temperaturreihe(9.5, 11, 4, -Math.PI / 2);
            Pruefe(ziel, "klimadaten_temperatur", 1304, 440,
                   new SKColor[0],
                   () => ChartRenderer.Jahresgang("Jahrestemperatur Verlauf",
                            new List<ChartRenderer.Reihe>
                            {
                                new ChartRenderer.Reihe("Temperatur", klimaTemperatur,
                                                        ChartRenderer.C_AUSSENTEMPERATUR)
                            },
                            "Monat", "Temperatur [°C]"));

            double[] sonnenwinkel = Sonnenwinkelreihe();
            Pruefe(ziel, "klimadaten_sonnenwinkel", 1304, 440,
                   new[] { SKColors.Orange },
                   () => ChartRenderer.Jahresgang("Sonnenwinkel Verlauf",
                            new List<ChartRenderer.Reihe>
                            {
                                new ChartRenderer.Reihe("Sonnenwinkel", sonnenwinkel,
                                                        SKColors.Orange)
                            },
                            "Monat", "Sonnenwinkel [°]", minimumNull: true));

            // =========================================================================
            // 17-30 - die sieben ERGEBNISBILDER der Welle 11 (iU9-W11a.6), je zwei Proben
            // =========================================================================
            //
            // Je Bild ein "voller" und ein "magerer" Fall: der volle mit der Reihenzahl
            // des Bestands, der magere mit einer einzigen Reihe. Der magere Fall ist der
            // wichtigere - er trifft die Praesenzfilterung, und genau dort brachen die
            // Vorlaeufer (drei parallele Listen, die gemeinsam gefiltert werden mussten,
            // NavigatorUebersicht :304-306).

            double[] gesamtlast = Jahresreihe(180, 120, 40, 0, Math.PI / 2);
            double[] heizung = Jahresreihe(120, 90, 25, 0, Math.PI / 2);
            double[] brauchwasser = Jahresreihe(40, 5, 10, 1.0, Math.PI / 2);
            double[] prozess = Jahresreihe(20, 2, 5, 2.0, Math.PI / 2);

            // --- B1: normierte Ganglinie ---------------------------------------------
            var b1Reihen = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("Gesamt", gesamtlast, SKColors.Red),
                new ChartRenderer.Reihe("Heizung", heizung, SKColors.DeepSkyBlue),
                new ChartRenderer.Reihe("Brauchwasser", brauchwasser, B1_VIOLETT),
                new ChartRenderer.Reihe("Prozesswaerme", prozess, SKColors.Gray)
            };
            Pruefe(ziel, "ganglinie_normiert_chronologisch", 1240, 560,
                   new[] { SKColors.Red, SKColors.DeepSkyBlue, B1_VIOLETT, SKColors.Gray },
                   () => ChartRenderer.GanglinieNormiert("Waermelast Jahresganglinie", b1Reihen,
                            "Anteil am Hoechstwert", ChartRenderer.Achse.Monate, false));

            Pruefe(ziel, "ganglinie_normiert_sortiert", 1240, 560,
                   new[] { SKColors.Red },
                   () => ChartRenderer.GanglinieNormiert("Waermelast Jahresdauerlinie",
                            new List<ChartRenderer.Reihe>
                            { new ChartRenderer.Reihe("Gesamt", gesamtlast, SKColors.Red) },
                            "Anteil am Hoechstwert", ChartRenderer.Achse.Jahresstunden, true));

            // --- B2/B3: Erzeugerstapel ------------------------------------------------
            var b2Stapel = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("Waermepumpe", Jahresreihe(60, 45, 12, 0, Math.PI / 2),
                                        SKColors.Orange, ChartRenderer.Stapelart.Saeule),
                new ChartRenderer.Reihe("Heizstab", Jahresreihe(12, 9, 3, 0.4, Math.PI / 2),
                                        SKColors.Yellow, ChartRenderer.Stapelart.Saeule),
                new ChartRenderer.Reihe("Heizkessel", Jahresreihe(50, 40, 10, 0.2, Math.PI / 2),
                                        SKColors.Blue, ChartRenderer.Stapelart.Saeule),
                new ChartRenderer.Reihe("Solarthermie", Jahresreihe(18, 2, 6, 3.1, Math.PI / 2),
                                        SKColors.Brown, ChartRenderer.Stapelart.Flaeche),
                new ChartRenderer.Reihe("BHKW", Jahresreihe(35, 25, 8, 0.8, Math.PI / 2),
                                        SKColors.Red, ChartRenderer.Stapelart.Flaeche)
            };
            Pruefe(ziel, "erzeugerstapel_waerme", 1240, 560,
                   new[] { SKColors.Orange, SKColors.Yellow, SKColors.Blue,
                           SKColors.Brown, SKColors.Red, SKColors.Green, SKColors.DarkCyan },
                   () => ChartRenderer.ErzeugerStapel("Waermeproduktion Jahresganglinie",
                            b2Stapel,
                            new List<ChartRenderer.Reihe>(),
                            new ChartRenderer.Reihe("Gesamt", gesamtlast, SKColors.Green,
                                                    ChartRenderer.Stapelart.Keine, false, 4f),
                            "Waermelast [kW]", ChartRenderer.Achse.Monate, false,
                            new ChartRenderer.Reihe("Waermebedarf", gesamtlast, SKColors.DarkCyan),
                            "Bedarf [kW]"));

            // Viertelstundenraster (Stromseite) mit vier Stapelreihen und zwei Linien.
            var b2Strom = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("Lastgangprofil", Viertelstundenreihe(90, 60, 20, 0),
                                        SKColors.Brown, ChartRenderer.Stapelart.Saeule),
                new ChartRenderer.Reihe("Waermepumpe", Viertelstundenreihe(30, 20, 8, 0.5),
                                        SKColors.Orange, ChartRenderer.Stapelart.Saeule),
                new ChartRenderer.Reihe("Heizstab", Viertelstundenreihe(8, 5, 2, 1.0),
                                        SKColors.Yellow, ChartRenderer.Stapelart.Saeule),
                new ChartRenderer.Reihe("Heizkessel", Viertelstundenreihe(12, 8, 3, 1.5),
                                        SKColors.Blue, ChartRenderer.Stapelart.Saeule)
            };
            Pruefe(ziel, "erzeugerstapel_strom_viertelstunden", 1240, 560,
                   new[] { SKColors.Brown, SKColors.Orange, SKColors.Yellow, SKColors.Blue,
                           SKColors.BlueViolet, SKColors.Green },
                   () => ChartRenderer.ErzeugerStapel("Stromverbrauch Jahresganglinie",
                            b2Strom,
                            new List<ChartRenderer.Reihe>
                            {
                                new ChartRenderer.Reihe("Photovoltaik",
                                                        Viertelstundenreihe(40, 5, 15, 2.0),
                                                        SKColors.BlueViolet),
                                new ChartRenderer.Reihe("Gesamt",
                                                        Viertelstundenreihe(140, 95, 30, 0),
                                                        SKColors.Green, ChartRenderer.Stapelart.Keine,
                                                        false, 2f)
                            },
                            null, "Leistung [kW]", ChartRenderer.Achse.Monate, false));

            // Sortiert - ohne Stapel, mit dickeren Dauerlinien (BorderWidth 4).
            Pruefe(ziel, "erzeugerstapel_kessel_sortiert", 1240, 560,
                   new[] { SKColors.Blue, SKColors.Green, SKColors.Red },
                   () => ChartRenderer.ErzeugerStapel("Waermelast Jahresdauerlinie",
                            new List<ChartRenderer.Reihe>
                            {
                                new ChartRenderer.Reihe("Waermeproduktion",
                                                        Jahresreihe(50, 40, 10, 0.2, Math.PI / 2),
                                                        SKColors.Blue, ChartRenderer.Stapelart.Saeule)
                            },
                            new List<ChartRenderer.Reihe>
                            {
                                new ChartRenderer.Reihe("Restwaerme",
                                                        Jahresreihe(30, 20, 8, 0.6, Math.PI / 2),
                                                        SKColors.Green),
                                new ChartRenderer.Reihe("Waermebedarf", gesamtlast, SKColors.Red)
                            },
                            null, "Waermelast [kW]", ChartRenderer.Achse.Jahresstunden, true));

            // Der MAGERE Fall von B2: nur zwei Linien, kein Stapel und keine Kontur -
            // die Solarthermieseite (chart8) hat genau das.
            Pruefe(ziel, "erzeugerstapel_solar_zwei_linien", 1240, 560,
                   new[] { SKColors.Red, SKColors.Blue },
                   () => ChartRenderer.ErzeugerStapel("Waermelast Jahresganglinie",
                            new List<ChartRenderer.Reihe>(),
                            new List<ChartRenderer.Reihe>
                            {
                                new ChartRenderer.Reihe("Waermebedarf", gesamtlast, SKColors.Red),
                                new ChartRenderer.Reihe("Waermeproduktion",
                                                        Jahresreihe(18, 2, 6, 3.1, Math.PI / 2),
                                                        SKColors.Blue)
                            },
                            null, "Waermelast [kW]", ChartRenderer.Achse.Jahresstunden, false));

            // --- DATENZOOM: dieselben zwei Bilder mit Achsenbereich --------------------
            //
            // Windows-Abnahme 05.09.2026, Befund A-1. Der Anwender zieht im Bild ein
            // Rechteck auf; der Kern zeichnet DIESEN Ausschnitt neu, statt das fertige
            // Bild zu vergroessern. Geprueft wird dasselbe wie bei jedem anderen Bild -
            // Masse, Farben, Determinismus -, und zusaetzlich, dass der Ausschnitt ein
            // ANDERES Bild ergibt als die Vollansicht (weiter unten, Unterschiedlich).
            //
            // Das Fenster liegt auf den Stunden 3 000 bis 3 500: mitten im Jahr, damit
            // im Bild wirklich ein Ausschnitt steht und nicht zufaellig der Rand.
            var fenster = new ChartRenderer.Achsenfenster(3000, 3500);

            Pruefe(ziel, "ganglinie_normiert_fenster", 1240, 560,
                   new[] { SKColors.Red },
                   () => ChartRenderer.GanglinieNormiert("Waermelast Jahresganglinie",
                            new List<ChartRenderer.Reihe>
                            { new ChartRenderer.Reihe("Gesamt", gesamtlast, SKColors.Red) },
                            "Anteil am Hoechstwert", ChartRenderer.Achse.Jahresstunden, false,
                            fenster));

            // Mit senkrechtem Anteil: die obere Kante des Rechtecks halbiert die Achse.
            Pruefe(ziel, "erzeugerstapel_fenster", 1240, 560,
                   new[] { SKColors.Orange, SKColors.Yellow, SKColors.Blue, SKColors.Green },
                   () => ChartRenderer.ErzeugerStapel("Waermeproduktion Jahresganglinie",
                            b2Stapel,
                            new List<ChartRenderer.Reihe>(),
                            new ChartRenderer.Reihe("Gesamt", gesamtlast, SKColors.Green,
                                                    ChartRenderer.Stapelart.Keine, false, 4f),
                            "Waermelast [kW]", ChartRenderer.Achse.Jahresstunden, false,
                            null, null,
                            new ChartRenderer.Achsenfenster(3000, 3500, 0.6)));

            // --- B4: Streuwolke -------------------------------------------------------
            Pruefe(ziel, "streuwolke_drei_reihen", 1240, 560,
                   new[] { STREU_ROT_AUF_WEISS, STREU_GELB_AUF_WEISS, STREU_BLAU_AUF_WEISS },
                   () => ChartRenderer.Streuwolke("Leistung ueber Aussentemperatur",
                            "Temperatur [°C]", "Leistung [kW]",
                            new List<ChartRenderer.Punktreihe>
                            {
                                new ChartRenderer.Punktreihe("Waermebedarf", Wolke(0), STREU_ROT),
                                new ChartRenderer.Punktreihe("Heizstab", Wolke(1), STREU_GELB),
                                new ChartRenderer.Punktreihe("Waermeproduktion", Wolke(2), STREU_BLAU)
                            }));

            Pruefe(ziel, "streuwolke_eine_reihe", 1240, 560,
                   new[] { STREU_BLAU_AUF_WEISS },
                   () => ChartRenderer.Streuwolke("Leistung ueber Aussentemperatur",
                            "Temperatur [°C]", "Leistung [kW]",
                            new List<ChartRenderer.Punktreihe>
                            { new ChartRenderer.Punktreihe("Waermeproduktion", Wolke(2), STREU_BLAU) }));

            // --- B5: Ring -------------------------------------------------------------
            Pruefe(ziel, "ring_waermedeckung", 720, 560,
                   new[] { RING_WP, RING_SOLAR, RING_HEIZSTAB, RING_KESSEL, RING_REST },
                   () => ChartRenderer.Ring("Waermedeckung",
                            new List<ChartRenderer.Ringsegment>
                            {
                                new ChartRenderer.Ringsegment("Waermepumpe", 340, RING_WP),
                                new ChartRenderer.Ringsegment("Solarthermie", 90, RING_SOLAR),
                                new ChartRenderer.Ringsegment("Heizstab", 45, RING_HEIZSTAB),
                                new ChartRenderer.Ringsegment("Spitzenkessel", 120, RING_KESSEL),
                                new ChartRenderer.Ringsegment("Rest", 60, RING_REST)
                            },
                            89.4, "%"));

            // Der MAGERE Fall: drei Segmente, davon eines mit Wert 0 - es darf weder
            // gezeichnet noch in der Legende genannt werden (dynamische Legende).
            Pruefe(ziel, "ring_stromdeckung", 720, 560,
                   new[] { RING_WP, RING_SOLAR, RING_HEIZSTAB },
                   () => ChartRenderer.Ring("Stromdeckung",
                            new List<ChartRenderer.Ringsegment>
                            {
                                new ChartRenderer.Ringsegment("Photovoltaik", 220, RING_WP),
                                new ChartRenderer.Ringsegment("BHKW", 130, RING_SOLAR),
                                new ChartRenderer.Ringsegment("Speicherentladung", 0, RING_VIOLETT),
                                new ChartRenderer.Ringsegment("Reststrom", 95, RING_HEIZSTAB)
                            },
                            78.6, "%"));

            // --- B6: Monatsstapel -----------------------------------------------------
            Pruefe(ziel, "monatsstapel_drei_reihen", 978, 542,
                   new[] { SKColors.Gold, SKColors.LightGreen, SKColors.Red },
                   () => ChartRenderer.MonatsStapel("Energie-Bedarf & Deckung", "kWh",
                            new List<ChartRenderer.Reihe>
                            {
                                new ChartRenderer.Reihe("Eigenverbrauch (Direkt)",
                                                        Monatsreihe(), SKColors.Gold),
                                new ChartRenderer.Reihe("Eigenverbrauch (Speicher)",
                                                        Monatsreihe(0.4), SKColors.LightGreen),
                                new ChartRenderer.Reihe("Autarkie-Luecke (Netz)",
                                                        Monatsreihe(0.9), SKColors.Red)
                            }));

            Pruefe(ziel, "monatsstapel_eine_reihe", 978, 542,
                   new[] { SKColors.Gold },
                   () => ChartRenderer.MonatsStapel("Eigenverbrauch", "kWh",
                            new List<ChartRenderer.Reihe>
                            { new ChartRenderer.Reihe("Direkt", Monatsreihe(), SKColors.Gold) }));

            // --- B7: Temperaturverlauf ------------------------------------------------
            Pruefe(ziel, "temperaturverlauf_zwei_speicher", 1240, 560,
                   new[] { TEMP_ROT, TEMP_BLAU, TEMP_QUELLE },
                   () => ChartRenderer.Temperaturverlauf("Speichertemperaturen",
                            new List<ChartRenderer.Reihe>
                            {
                                new ChartRenderer.Reihe("Puffer 1 oben",
                                                        Temperaturreihe(62, 8, 0, 0), TEMP_ROT),
                                new ChartRenderer.Reihe("Puffer 1 unten",
                                                        Temperaturreihe(48, 6, 0, 0), TEMP_ROT,
                                                        ChartRenderer.Stapelart.Keine, true),
                                new ChartRenderer.Reihe("Puffer 2 oben",
                                                        Temperaturreihe(55, 7, 1, 0), TEMP_BLAU),
                                new ChartRenderer.Reihe("Puffer 2 unten",
                                                        Temperaturreihe(41, 5, 1, 0), TEMP_BLAU,
                                                        ChartRenderer.Stapelart.Keine, true),
                                new ChartRenderer.Reihe("Quelltemperatur Erdreich",
                                                        Temperaturreihe(11, 4, 0, -Math.PI / 2),
                                                        TEMP_QUELLE)
                            },
                            minAuto: true));

            // Der MAGERE Fall: EIN Speicher, und seine beiden Schichten liegen dicht
            // beieinander - hier greift die Mindestspanne von 5 K.
            Pruefe(ziel, "temperaturverlauf_ein_speicher", 1240, 560,
                   new[] { TEMP_ROT },
                   () => ChartRenderer.Temperaturverlauf("Speichertemperaturen",
                            new List<ChartRenderer.Reihe>
                            {
                                new ChartRenderer.Reihe("Puffer 1 oben",
                                                        Temperaturreihe(60, 0.4, 0, 0), TEMP_ROT),
                                new ChartRenderer.Reihe("Puffer 1 unten",
                                                        Temperaturreihe(59, 0.4, 0, 0), TEMP_ROT,
                                                        ChartRenderer.Stapelart.Keine, true)
                            },
                            minAuto: true));

            // --- Der Ausschnitt muss auch WIRKEN --------------------------------------
            //
            // Masse, Farben und Determinismus stimmen auch dann, wenn der
            // Fensterparameter stillschweigend ignoriert wird. Diese zwei Faelle
            // pruefen deshalb das Gegenstueck: derselbe Aufruf mit und ohne Fenster
            // muss zwei verschiedene Bilder liefern - und OHNE Fenster genau das der
            // Vollansicht, Byte fuer Byte.
            Unterschiedlich("ganglinie_normiert_fenster",
                () => ChartRenderer.GanglinieNormiert("Waermelast Jahresganglinie",
                        new List<ChartRenderer.Reihe>
                        { new ChartRenderer.Reihe("Gesamt", gesamtlast, SKColors.Red) },
                        "Anteil am Hoechstwert", ChartRenderer.Achse.Jahresstunden, false),
                () => ChartRenderer.GanglinieNormiert("Waermelast Jahresganglinie",
                        new List<ChartRenderer.Reihe>
                        { new ChartRenderer.Reihe("Gesamt", gesamtlast, SKColors.Red) },
                        "Anteil am Hoechstwert", ChartRenderer.Achse.Jahresstunden, false,
                        fenster));

            Unterschiedlich("erzeugerstapel_fenster",
                () => ChartRenderer.ErzeugerStapel("Waermeproduktion Jahresganglinie",
                        b2Stapel, new List<ChartRenderer.Reihe>(), null,
                        "Waermelast [kW]", ChartRenderer.Achse.Jahresstunden, false),
                () => ChartRenderer.ErzeugerStapel("Waermeproduktion Jahresganglinie",
                        b2Stapel, new List<ChartRenderer.Reihe>(), null,
                        "Waermelast [kW]", ChartRenderer.Achse.Jahresstunden, false,
                        null, null, fenster));

            // Dasselbe fuer die ZEITSTUFEN des Bedarfsdialogs (W8-E-2): Ohne diese zwei
            // Gegenproben bestuende ein stillschweigend uebergangener Fensterparameter
            // jede Mass-, Farb- und Determinismuspruefung - und "Woche" zeigte das Jahr.
            // Geprueft wird BEIDES: Woche gegen Jahr und Tag gegen Woche; sonst waere ein
            // Fenster, das immer denselben Ausschnitt nimmt, nicht zu bemerken.
            Unterschiedlich("jahresverlauf_woche_fenster",
                () => ChartRenderer.Jahresverlauf("Strombedarf Ganglinie", jahresverlauf,
                        "Strombedarf [kW]", SKColors.SteelBlue),
                () => ChartRenderer.Jahresverlauf("Strombedarf Ganglinie", jahresverlauf,
                        "Strombedarf [kW]", SKColors.SteelBlue, fensterWoche));

            Unterschiedlich("jahresverlauf_tag_fenster",
                () => ChartRenderer.Jahresverlauf("Strombedarf Ganglinie", jahresverlauf,
                        "Strombedarf [kW]", SKColors.SteelBlue, fensterWoche),
                () => ChartRenderer.Jahresverlauf("Strombedarf Ganglinie", jahresverlauf,
                        "Strombedarf [kW]", SKColors.SteelBlue, fensterTag));

            Console.WriteLine(new string('-', 92));
            Console.WriteLine(_bilder + " Bilder geprueft, " + _verstoesse + " Verstoesse.");
            if (_verstoesse == 0) Console.WriteLine("ERGEBNIS: alle gruen.");
            else Console.WriteLine("ERGEBNIS: FEHLGESCHLAGEN.");
            return _verstoesse == 0 ? 0 : 1;
        }


        // ------------------------------------------------- Farben der Welle-11-Bilder

        /// <summary>Die dritte Kanalfarbe der Bedarfsseite: <c>ARGB(126, 87, 166)</c>.</summary>
        private static readonly SKColor B1_VIOLETT = new SKColor(126, 87, 166);

        /// <summary>
        /// Die drei HALBTRANSPARENTEN Farben der Streuwolke — woertlich
        /// <c>Color.FromArgb(120, Red|Yellow|Blue)</c> aus <c>chart4</c>.
        /// </summary>
        private static readonly SKColor STREU_ROT = new SKColor(255, 0, 0, 120);
        private static readonly SKColor STREU_GELB = new SKColor(255, 255, 0, 120);
        private static readonly SKColor STREU_BLAU = new SKColor(0, 0, 255, 120);

        /// <summary>
        /// Dieselben drei Farben, wie sie ueber Weiss ANKOMMEN — die Pixelpruefung
        /// vergleicht exakt (derselbe Grund wie bei der Kostenprofil-Linie).
        /// </summary>
        private static readonly SKColor STREU_ROT_AUF_WEISS = new SKColor(255, 135, 135);
        private static readonly SKColor STREU_GELB_AUF_WEISS = new SKColor(255, 255, 135);
        private static readonly SKColor STREU_BLAU_AUF_WEISS = new SKColor(135, 135, 255);

        /// <summary>Die Segmentfarben der beiden Donuts der <c>NavigatorUebersicht</c>.</summary>
        private static readonly SKColor RING_WP = new SKColor(0x2E, 0xCC, 0x71);
        private static readonly SKColor RING_SOLAR = new SKColor(0xE6, 0x7E, 0x22);
        private static readonly SKColor RING_HEIZSTAB = new SKColor(0xF1, 0xC4, 0x0F);
        private static readonly SKColor RING_KESSEL = new SKColor(0x95, 0xA5, 0xA6);
        private static readonly SKColor RING_REST = new SKColor(0x34, 0x98, 0xDB);
        private static readonly SKColor RING_VIOLETT = new SKColor(0x9B, 0x59, 0xB6);

        /// <summary>Die Farbfolge der Speichertemperaturen (<c>TEMP_FARBEN</c>) und die Quellfarbe.</summary>
        private static readonly SKColor TEMP_ROT = new SKColor(0xC0, 0x39, 0x2B);
        private static readonly SKColor TEMP_BLAU = new SKColor(0x28, 0x80, 0xB9);
        private static readonly SKColor TEMP_QUELLE = new SKColor(0xD8, 0x5A, 0x30);

        /// <summary>Eine Viertelstundenreihe (35 040 Werte) — dieselbe Bauform wie <c>Jahresreihe</c>.</summary>
        private static double[] Viertelstundenreihe(double mitte, double amplitude,
                                                    double tagesamplitude, double phase)
        {
            var r = new double[STUNDEN * 4];
            for (int i = 0; i < r.Length; i++)
            {
                double jahr = 2.0 * Math.PI * i / r.Length;
                double tag = 2.0 * Math.PI * (i % 96) / 96.0;
                r[i] = Math.Max(0, mitte + amplitude * Math.Sin(jahr + phase)
                                         + tagesamplitude * Math.Sin(tag));
            }
            return r;
        }

        /// <summary>Eine zweite Monatsreihe mit VERSCHOBENER Phase — fuer den Stapel.</summary>
        private static double[] Monatsreihe(double phase)
        {
            var w = new double[12];
            for (int m = 0; m < 12; m++)
                w[m] = Math.Round(24.0 + 11.0 * Math.Cos(2.0 * Math.PI * m / 12.0 + phase), 3);
            return w;
        }

        /// <summary>Eine Punktwolke Temperatur/Leistung — fest verdrahtet, ohne Zufall.</summary>
        private static List<(double X, double Y)> Wolke(int reihe)
        {
            var p = new List<(double X, double Y)>();
            for (int i = 0; i < 2000; i++)
            {
                double t = -15.0 + 35.0 * i / 2000.0;
                double grund = Math.Max(0, 60.0 - 2.2 * t);
                double streuung = 6.0 * Math.Sin(i * 0.37 + reihe);
                p.Add((t, Math.Max(0, grund * (1.0 - 0.25 * reihe) + streuung)));
            }
            return p;
        }

        // =================================================================================
        // Pruefung eines Bildes
        // =================================================================================

        private static void Pruefe(string ziel, string name, int breite, int hoehe,
                                   SKColor[] erwartet, Func<byte[]> erzeuge)
        {
            _bilder++;
            var maengel = new List<string>();
            string masse = "-", bytes = "-", farbzahl = "-", determ = "-";

            byte[] a;
            try { a = erzeuge(); }
            catch (Exception ex)
            {
                Melde(name, masse, bytes, farbzahl, determ,
                      new List<string> { "Ausnahme: " + ex.GetType().Name + " - " + ex.Message });
                return;
            }

            if (a == null)
            {
                Melde(name, masse, bytes, farbzahl, determ,
                      new List<string> { "Renderer liefert null" });
                return;
            }

            bytes = a.Length.ToString("N0", CultureInfo.InvariantCulture);
            File.WriteAllBytes(Path.Combine(ziel, name + ".png"), a);

            // --- PNG-Signatur -----------------------------------------------------------
            byte[] sig = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            if (a.Length < sig.Length || !a.Take(sig.Length).SequenceEqual(sig))
                maengel.Add("keine PNG-Signatur");

            // --- Determinismus: zweimal rendern muss byte-gleich sein --------------------
            byte[] b = erzeuge();
            bool gleich = b != null && a.Length == b.Length && a.SequenceEqual(b);
            determ = gleich ? "ja" : "NEIN";
            if (!gleich) maengel.Add("zweiter Lauf liefert andere Bytes");

            using (SKBitmap bild = SKBitmap.Decode(a))
            {
                if (bild == null)
                {
                    maengel.Add("PNG nicht dekodierbar");
                    Melde(name, masse, bytes, farbzahl, determ, maengel);
                    return;
                }

                masse = bild.Width + "x" + bild.Height;
                if (bild.Width != breite || bild.Height != hoehe)
                    maengel.Add("Masse " + masse + " statt " + breite + "x" + hoehe);

                SKColor[] pixel = bild.Pixels;
                var vorhanden = new HashSet<uint>();
                foreach (SKColor p in pixel) vorhanden.Add((uint)p);
                farbzahl = vorhanden.Count.ToString(CultureInfo.InvariantCulture);

                if (vorhanden.Count < 2) maengel.Add("Bild ist einfarbig");

                foreach (SKColor soll in erwartet)
                    if (!vorhanden.Contains((uint)soll))
                        maengel.Add("Farbe #" + soll.Red.ToString("X2") + soll.Green.ToString("X2") +
                                    soll.Blue.ToString("X2") + " fehlt");
            }

            Melde(name, masse, bytes, farbzahl, determ, maengel);
        }

        /// <summary>
        /// Zwei Zeichenwege muessen VERSCHIEDENE Bilder liefern (Datenzoom, Befund A-1
        /// der Windows-Abnahme 05.09.2026). Das ist die Gegenprobe zu
        /// <see cref="Pruefe"/>: Ein Fensterparameter, den der Renderer uebergeht,
        /// bestuende jede Mass-, Farb- und Determinismuspruefung und wuerde trotzdem
        /// nichts tun.
        /// </summary>
        private static void Unterschiedlich(string name, Func<byte[]> ganz, Func<byte[]> teil)
        {
            _bilder++;
            var maengel = new List<string>();

            byte[] a, b;
            try { a = ganz(); b = teil(); }
            catch (Exception ex)
            {
                Melde(name + " (wirkt)", "-", "-", "-", "-",
                      new List<string> { "Ausnahme: " + ex.GetType().Name + " - " + ex.Message });
                return;
            }

            if (a == null || b == null) maengel.Add("Renderer liefert null");
            else if (a.SequenceEqual(b)) maengel.Add("Ausschnitt aendert das Bild nicht");

            Melde(name + " (wirkt)", "-", b == null ? "-" : b.Length.ToString("N0", CultureInfo.InvariantCulture),
                  "-", "-", maengel);
        }

        private static void Melde(string name, string masse, string bytes, string farbzahl,
                                  string determ, List<string> maengel)
        {
            if (maengel.Count > 0) _verstoesse++;
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "{0,-26} {1,-12} {2,10} {3,8} {4,7} {5}",
                name, masse, bytes, farbzahl, determ,
                maengel.Count == 0 ? "OK" : "FEHLER: " + string.Join("; ", maengel)));
        }

        // =================================================================================
        // Synthetische Daten - Sinus und Rampe, streng deterministisch
        // =================================================================================

        /// <summary>
        /// Jahresreihe aus Grundlast, Jahresschwingung, Tagesschwingung und einer
        /// linearen Rampe. Negative Werte werden abgeschnitten — die Diagramme des
        /// Berichts rechnen mit Energiemengen.
        /// </summary>
        private static double[] Jahresreihe(double grund, double jahresHub, double tagesHub,
                                            double rampe, double phase)
        {
            var w = new double[STUNDEN];
            for (int i = 0; i < STUNDEN; i++)
            {
                double jahr = 2.0 * Math.PI * i / STUNDEN;
                double tag = 2.0 * Math.PI * (i % 24) / 24.0;
                double v = grund
                         + jahresHub * Math.Sin(jahr + phase)
                         + tagesHub * Math.Sin(tag)
                         + rampe * i / STUNDEN;
                w[i] = v > 0 ? v : 0;
            }
            return w;
        }

        /// <summary>Reihe, die NICHT bei 0 anfangen soll (Temperaturen) — ohne Abschnitt.</summary>
        private static double[] Temperaturreihe(double mitte, double jahresHub, double tagesHub,
                                                double phase)
        {
            var w = new double[STUNDEN];
            for (int i = 0; i < STUNDEN; i++)
            {
                double jahr = 2.0 * Math.PI * i / STUNDEN;
                double tag = 2.0 * Math.PI * (i % 24) / 24.0;
                w[i] = mitte + jahresHub * Math.Sin(jahr + phase) + tagesHub * Math.Sin(tag);
            }
            return w;
        }

        /// <summary>
        /// Der TAGESHOECHSTSTAND der Sonne ueber das Jahr (iU9-W14c.7) - dieselbe Form,
        /// die <c>SolarCalculator.GetDailyAverages</c> als Maximum je Tag liefert:
        /// Sinusbogen zwischen rund 15 Grad im Winter und 62 Grad im Sommer, NIE
        /// negativ. Genau daran haengt die Nullpunktbindung der zweiten Probe.
        /// </summary>
        private static double[] Sonnenwinkelreihe()
        {
            var w = new double[STUNDEN];
            for (int i = 0; i < STUNDEN; i++)
            {
                double jahr = 2.0 * Math.PI * i / STUNDEN;
                w[i] = 38.5 + 23.5 * Math.Sin(jahr - Math.PI / 2);
            }
            return w;
        }

        private static ZeitreihenSatz SyntheticherSatz()
        {
            var z = new ZeitreihenSatz();

            // Waerme: Bedarf im Winter hoch (Phase so gelegt, dass Stunde 0 = Januar).
            z.Reihen[ZeitreihenSatz.WAERMEBEDARF] = Jahresreihe(180, 120, 25, 0, Math.PI / 2);
            z.Reihen[ZeitreihenSatz.TEMPERATUR] = Temperaturreihe(10, 12, 3, -Math.PI / 2);

            z.Reihen[ZeitreihenSatz.SOLAR_WAERME] = Jahresreihe(18, 16, 8, 0, -Math.PI / 2);
            z.Reihen[ZeitreihenSatz.WP_WAERME] = Jahresreihe(70, 40, 12, 10, Math.PI / 2);
            z.Reihen[ZeitreihenSatz.HEIZSTAB] = Jahresreihe(4, 6, 2, 0, Math.PI / 2);
            z.Reihen[ZeitreihenSatz.BHKW_WAERME] = Jahresreihe(45, 25, 9, 0, Math.PI / 2);
            z.Reihen[ZeitreihenSatz.KESSEL_WAERME] = Jahresreihe(25, 35, 6, 0, Math.PI / 2);

            // Strom: Bedarf gleichmaessiger, PV im Sommer hoch.
            z.Reihen[ZeitreihenSatz.STROMBEDARF] = Jahresreihe(90, 20, 30, 0, 0);
            z.Reihen[ZeitreihenSatz.PV_GENUTZT] = Jahresreihe(28, 26, 18, 0, -Math.PI / 2);
            z.Reihen[ZeitreihenSatz.PV_UEBERSCHUSS] = Jahresreihe(12, 14, 10, 0, -Math.PI / 2);
            z.Reihen[ZeitreihenSatz.BHKW_STROM] = Jahresreihe(30, 18, 6, 0, Math.PI / 2);
            z.Reihen[ZeitreihenSatz.NETZBEZUG] = Jahresreihe(40, 10, 14, 0, 0);
            z.Reihen[ZeitreihenSatz.PV_SPEICHER_SOC] = Jahresreihe(160, 80, 60, 0, -Math.PI / 2);

            // Zwei Waermespeicher mit Fuellstand und beiden Schichttemperaturen.
            Speicher(z, "PUFFER_11", "Heizungspuffer (Senke)", 520, 260, 180, 0.0, 68, 46);
            Speicher(z, "PUFFER_12", "Brauchwasserspeicher (Senke)", 300, 140, 110, 0.6, 60, 40);

            // Eine Quelltemperatur (temperaturgekoppelter Erzeuger, Paket B1).
            z.Reihen[ZeitreihenSatz.QUELLTEMP_PRAEFIX + "5"] = Temperaturreihe(12, 9, 2, -Math.PI / 2);
            z.Beschriftungen[ZeitreihenSatz.QUELLTEMP_PRAEFIX + "5"] = "Erdsonde (Quelle)";

            return z;
        }

        private static void Speicher(ZeitreihenSatz z, string schluessel, string beschriftung,
                                     double socGrund, double socJahresHub, double socTagesHub,
                                     double phase, double tOben, double tUnten)
        {
            z.Reihen[schluessel] = Jahresreihe(socGrund, socJahresHub, socTagesHub, 0, phase);
            z.Beschriftungen[schluessel] = beschriftung;
            z.Speicherreihen.Add(schluessel);

            z.Reihen[schluessel + ZeitreihenSatz.SUFFIX_T_OBEN] =
                Temperaturreihe(tOben, 6, 4, phase);
            z.Beschriftungen[schluessel + ZeitreihenSatz.SUFFIX_T_OBEN] = beschriftung + " oben";

            z.Reihen[schluessel + ZeitreihenSatz.SUFFIX_T_UNTEN] =
                Temperaturreihe(tUnten, 5, 3, phase);
            z.Beschriftungen[schluessel + ZeitreihenSatz.SUFFIX_T_UNTEN] = beschriftung + " unten";
        }

        /// <summary>
        /// Ein Kostenprofil, wie es <c>PreisModell.AusMonatsUndWochenwerten</c>
        /// baut (iU9-W3.4): Monatsniveau plus Wochenwert je Wochentag und Stunde.
        /// Der Dezember liegt hier UNTER null - damit prueft das Bild auch die
        /// gestrichelte Nulllinie und die vorzeichenfaehige Skala.
        /// </summary>
        /// <summary>
        /// Zwoelf Monatswerte (iU9-W8.0c) - Winterberg, Sommertal, dazu ein Monat auf
        /// genau 0. Der Nullmonat gehoert dazu: Die Achsenrechnung der Bedarfsmasken hat
        /// einen eigenen Rueckfall fuer "alles null", und eine EINZELNE Null darf ihn
        /// gerade nicht ausloesen.
        /// </summary>
        private static double[] Monatsreihe()
        {
            var w = new double[12];
            for (int m = 0; m < 12; m++)
                w[m] = Math.Round(42.0 + 18.0 * Math.Cos(2.0 * Math.PI * m / 12.0), 3);
            w[6] = 0.0;
            return w;
        }

        /// <summary>
        /// 168 Wochenwerte (iU9-W8.0c) - fuenf Werktage mit Tagesgang, zwei ruhigere
        /// Wochenendtage. Genau die Form, die ein Verbrauchertyp-Profil hat.
        /// </summary>
        private static double[] Wochenprofil()
        {
            var w = new double[168];
            for (int t = 0; t < 7; t++)
                for (int h = 0; h < 24; h++)
                {
                    double grund = t < 5 ? 0.55 : 0.25;
                    double tagesgang = 0.45 * Math.Sin(2.0 * Math.PI * (h - 6) / 24.0);
                    double v = grund + (t < 5 ? tagesgang : 0.5 * tagesgang);
                    w[t * 24 + h] = Math.Round(v > 0 ? v : 0, 4);
                }
            return w;
        }

        private static double[] Preisprofil()
        {
            var monat = new double[12];
            for (int m = 0; m < 12; m++) monat[m] = 25.0 + 6.0 * Math.Sin(2.0 * Math.PI * m / 12.0);
            monat[11] = -4.0;

            var woche = new double[168];
            for (int t = 0; t < 7; t++)
                for (int h = 0; h < 24; h++)
                    woche[t * 24 + h] = (t < 5 ? 3.0 : -2.0) * Math.Sin(2.0 * Math.PI * h / 24.0);

            // Dieselbe Zuordnung wie die Engine: Stunde -> Monat, Stunde -> Wochenstunde.
            var profil = new double[STUNDEN];
            int[] tageJeMonat = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            int stunde = 0;
            for (int m = 0; m < 12; m++)
                for (int d = 0; d < tageJeMonat[m]; d++)
                    for (int h = 0; h < 24; h++)
                    {
                        if (stunde >= STUNDEN) break;
                        profil[stunde] = monat[m] + woche[(stunde / 24 % 7) * 24 + h];
                        stunde++;
                    }
            return profil;
        }

        /// <summary>
        /// Drei Waermepumpen-Kennlinien (iU9-W7.0c) — je Vorlaufstufe acht Stuetzstellen
        /// von -15 bis +20 °C in 5-K-Schritten.
        ///
        /// <para>Die Physik ist nachgebildet, nicht gerechnet: Der COP steigt mit der
        /// Aussentemperatur und faellt mit dem Vorlauf (kleinerer Temperaturhub), die
        /// Waermeleistung steigt mit beidem. Es geht um den RENDERER — dass drei Reihen
        /// mit unterschiedlichen Werten in drei Farben mit ihren Marken erscheinen und
        /// zweimal Zeichnen dasselbe Bild liefert.</para>
        /// </summary>
        private static List<ChartRenderer.KennlinienReihe> Kennlinien(bool cop)
        {
            var reihen = new List<ChartRenderer.KennlinienReihe>();
            foreach (int vorlauf in new[] { 35, 45, 55 })
            {
                var punkte = new List<(double Temperatur, double Wert)>();
                for (int t = -15; t <= 20; t += 5)
                {
                    double hub = vorlauf - t;                       // Temperaturhub [K]
                    double wert = cop
                        ? 0.45 * (vorlauf + 273.15) / hub           // guetegradbehafteter Carnot-COP
                        : 6.0 + 0.18 * t + 0.04 * (55 - vorlauf) * 3.0;
                    punkte.Add((t, Math.Round(wert, 3)));
                }
                reihen.Add(new ChartRenderer.KennlinienReihe(vorlauf, punkte));
            }
            return reihen;
        }

        /// <summary>Drei Kapitalwertlinien ueber 21 Stuetzstellen; sie laufen durch die Null.</summary>
        private static List<VerlaufSerie> Beispielserien()
        {
            return new List<VerlaufSerie>
            {
                Serie("Stamm", true, -180000.0, 21000.0),
                Serie("Variante A", false, -260000.0, 32000.0),
                Serie("Variante B", false, -95000.0, 9000.0)
            };
        }

        private static VerlaufSerie Serie(string name, bool stamm, double invest, double jahresnutzen)
        {
            var k = new double[21];
            k[0] = invest;
            for (int t = 1; t < k.Length; t++)
                k[t] = k[t - 1] + jahresnutzen * Math.Pow(0.97, t);
            return new VerlaufSerie { Anzeige = name, IstStamm = stamm, Kumuliert = k };
        }

        // =================================================================================
        // Umgebung
        // =================================================================================

        /// <summary>
        /// Welche Schriftart die Rueckfallkette des Renderers auf DIESEM System liefert.
        /// Der Renderer haelt sie privat; hier wird dieselbe Kette noch einmal gefragt,
        /// damit im Protokoll steht, womit gezeichnet wurde.
        /// </summary>
        private static string Schriftbefund()
        {
            var stil = new SKFontStyle(SKFontStyleWeight.Normal, SKFontStyleWidth.Normal,
                                       SKFontStyleSlant.Upright);
            SKFontManager fm = SKFontManager.Default;
            SKTypeface t = null;
            string weg = null;
            foreach (string familie in new[]
                     { "Calibri", "Carlito", "Liberation Sans", "DejaVu Sans", "Helvetica", "Arial" })
            {
                try { t = fm.MatchFamily(familie, stil); } catch { }
                if (t != null) { weg = "MatchFamily(\"" + familie + "\")"; break; }
            }
            if (t == null) { try { t = fm.MatchFamily(null, stil); } catch { } weg = weg ?? "Systemschrift"; }
            if (t == null) { try { t = fm.MatchCharacter(null, stil, null, 'A'); } catch { } weg = weg ?? "MatchCharacter('A')"; }
            if (t == null) { t = SKTypeface.Default; weg = weg ?? "SKTypeface.Default"; }
            return (t.FamilyName ?? "?") + "  (ueber " + weg + ")";
        }

        /// <summary>Sucht vom Programmverzeichnis aufwaerts das Verzeichnis mit WP-Plan.sln.</summary>
        private static string Wurzel()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "WP-Plan.sln"))) return dir.FullName;
                dir = dir.Parent;
            }
            return Directory.GetCurrentDirectory();
        }

        private static string Argument(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            return null;
        }
    }
}
