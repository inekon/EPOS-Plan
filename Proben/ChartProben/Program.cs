using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SkiaSharp;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;

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
    internal static partial class Program
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

        /// <summary>
        /// Der Ordner des Schalters <c>--ablage</c>, sonst <c>null</c>: Dorthin legt jede
        /// Probe ihr Bild als PNG ab — auch die Gegenproben, die im Bestand gar nichts
        /// schreiben.
        /// </summary>
        private static string _ablage;

        /// <summary>Die Datei des Schalters <c>--hashes</c>, sonst <c>null</c>.</summary>
        private static string _hashdatei;

        /// <summary>
        /// Die Datei des Schalters <c>--svg</c>, sonst <c>null</c>: Dorthin schreibt die
        /// SVG-Gegenprobe den Jahresgang der Klimadaten EINMAL als Text — zum Ansehen im
        /// Browser und als Nachweis von Größe und Knotenzahl (Auftrag DG-E2).
        /// </summary>
        private static string _svgdatei;

        /// <summary>
        /// Der Ordner des Schalters <c>--svg-alle</c>, sonst <c>null</c>: Dorthin
        /// schreibt die Probe JEDES Modell der Gruppe (a) als <c>.svg</c> — zum Ansehen
        /// im Browser und für die Sichtprüfung gegen das Skia-Bild (Auftrag DG-E3a).
        /// <c>--svg</c> bleibt daneben, was es war: der Jahresgang in EINE Datei.
        /// </summary>
        private static string _svgordner;

        /// <summary>
        /// Die Messlatte: Dateiname → SHA-256 des PNG, nach Name geordnet
        /// (<see cref="StringComparer.Ordinal"/> — damit die Reihenfolge nicht von der
        /// Kultur des Laufs abhaengt). Gefuellt wird nur, wenn einer der beiden Schalter
        /// steht; ohne sie bleibt das Verhalten der Probe unveraendert.
        /// </summary>
        private static readonly SortedDictionary<string, string> _messlatte =
            new SortedDictionary<string, string>(StringComparer.Ordinal);

        private static int Main(string[] args)
        {
            try { Console.OutputEncoding = new UTF8Encoding(false); } catch { }

            // DIE MESSLATTE GILT FUER DIE VORGABE-PALETTE, AUSDRUECKLICH (Auftrag DF-1).
            // Die Farben der Diagramme sind seit DF-1 eine Anwendungseinstellung; ohne
            // diese Zeile haenge die eingefrorene Hashliste an dem, was zufaellig in
            // Farbpalette.Aktuell steht. Der Pruefstand setzt sie deshalb selbst -
            // Dienste.Einstellungen ist hier ohnehin die fluechtige Standardfassung.
            Farbpalette.Aktuell = Farbpalette.Vorgabe;

            string ziel = Argument(args, "--ziel") ?? Path.Combine(Wurzel(), "artifacts", "chartproben");
            Directory.CreateDirectory(ziel);

            _ablage = Argument(args, "--ablage");
            _hashdatei = Argument(args, "--hashes");
            _svgdatei = Argument(args, "--svg");
            _svgordner = Argument(args, "--svg-alle");
            if (_ablage != null) Directory.CreateDirectory(_ablage);

            Console.WriteLine("ChartProben - plattformfreier Renderer-Nachweis (Paket iU7-3)");
            Console.WriteLine("Zielordner: " + ziel);
            if (_ablage != null) Console.WriteLine("Ablage: " + _ablage);
            if (_hashdatei != null) Console.WriteLine("Hashliste: " + _hashdatei);
            if (_svgdatei != null) Console.WriteLine("SVG-Datei: " + _svgdatei);
            if (_svgordner != null) Console.WriteLine("SVG-Ordner: " + _svgordner);
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

            // 9b - AUFTRAG U18: dasselbe Absolutbild mit GESTRICHELTER Stammlinie.
            // Der Wortbericht ist der einzige Ort, an dem die Versionen nebeneinander
            // stehen; die Legende nennt deshalb Name, Farbe UND Strichart. Geprueft
            // werden Masse, Palette und Determinismus - dass die Strichart ueberhaupt
            // ankommt, sagt die Gegenprobe weiter unten.
            Pruefe(ziel, "kapitalwert_absolut_legende", 1240, 620,
                   new[] { ChartRenderer.C_STAMM, ChartRenderer.C_SERIEN[0], ChartRenderer.C_SERIEN[1] },
                   () => ChartRenderer.KapitalwertVerlauf("Kumulierte Barwerte je Version",
                            VerlaufMitGestricheltemStamm(serien), null));

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

            // 16d - DERSELBE JAHRESGANG MIT ZEITAUSSCHNITT (Auftrag KL-8). Der Anwender
            // zieht im Klimadialog ein Rechteck auf; die Huelle rechnet daraus ein
            // Achsenfenster, und der Kern zeichnet das Bild NEU - zugeschnitten, mit den
            // wirklichen Jahresstunden auf der x-Achse statt der Monatsteilung 0…12.
            // Geprueft wird, dass das Fenster das Bildmass NICHT antastet (1 304 x 440
            // wie in der Vollansicht) und dass es deterministisch bleibt.
            var fensterKlima = new ChartRenderer.Achsenfenster(2900, 3400);
            Pruefe(ziel, "klimadaten_temperatur_fenster", 1304, 440,
                   new SKColor[0],
                   () => ChartRenderer.Jahresgang("Jahrestemperatur Verlauf",
                            new List<ChartRenderer.Reihe>
                            {
                                new ChartRenderer.Reihe("Temperatur", klimaTemperatur,
                                                        ChartRenderer.C_AUSSENTEMPERATUR)
                            },
                            "Monat", "Temperatur [°C]", false, fensterKlima));

            // Die Gegenprobe: Ohne sie bestuende ein stillschweigend uebergangener
            // Fensterparameter jede Mass-, Farb- und Determinismuspruefung - und der
            // Ausschnitt zeigte weiter das ganze Jahr.
            Unterschiedlich("jahresgang_fenster",
                () => ChartRenderer.Jahresgang("Jahrestemperatur Verlauf",
                        new List<ChartRenderer.Reihe>
                        {
                            new ChartRenderer.Reihe("Temperatur", klimaTemperatur,
                                                    ChartRenderer.C_AUSSENTEMPERATUR)
                        },
                        "Monat", "Temperatur [°C]"),
                () => ChartRenderer.Jahresgang("Jahrestemperatur Verlauf",
                        new List<ChartRenderer.Reihe>
                        {
                            new ChartRenderer.Reihe("Temperatur", klimaTemperatur,
                                                    ChartRenderer.C_AUSSENTEMPERATUR)
                        },
                        "Monat", "Temperatur [°C]", false, fensterKlima));

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
                                                    ChartRenderer.Stapelart.Keine, ChartRenderer.Strichart.Durchgezogen,4f),
                            "Waermelast [kW]", ChartRenderer.Achse.Monate, false,
                            new List<ChartRenderer.Reihe>
                            { new ChartRenderer.Reihe("Waermebedarf", gesamtlast, SKColors.DarkCyan) },
                            "Bedarf [kW]"));

            // --- #234: ZWEI Speicher auf der zweiten Achse ----------------------------
            //
            // Anwenderrueckmeldung 12.09.2026: Auf der zweiten Achse des Waermegangs
            // steht seither der SPEICHERINHALT [kWh] - und ein Projekt fuehrt mehrere
            // Pufferspeicher. Die Bedarfslinie liegt dafuer auf der PRIMAERachse, wo
            // auch die Produktion steht (beides kW). Geprueft wird, dass beide
            // Speicherfarben UND die Bedarfsfarbe im Bild vorkommen; die Achse selbst
            // steht bei mehr als einer Reihe neutral in DimGray.
            double[] soc1 = Jahresreihe(900, 600, 250, 0, Math.PI / 2);
            double[] soc2 = Jahresreihe(500, 300, 150, 0, -Math.PI / 2);

            var b2Speicher = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("Pufferspeicher 1", soc1, SKColors.MediumVioletRed),
                new ChartRenderer.Reihe("Pufferspeicher 2", soc2, SKColors.Teal)
            };

            Pruefe(ziel, "erzeugerstapel_zwei_speicher", 1240, 560,
                   new[] { SKColors.Orange, SKColors.Yellow, SKColors.Blue, SKColors.Green,
                           SKColors.DarkCyan, SKColors.MediumVioletRed, SKColors.Teal,
                           SKColors.DimGray },
                   () => ChartRenderer.ErzeugerStapel("Waermeproduktion Jahresganglinie",
                            b2Stapel,
                            new List<ChartRenderer.Reihe>
                            { new ChartRenderer.Reihe("Waermebedarf", gesamtlast,
                                                      SKColors.DarkCyan,
                                                      ChartRenderer.Stapelart.Keine, ChartRenderer.Strichart.Durchgezogen,2f) },
                            new ChartRenderer.Reihe("Gesamt", gesamtlast, SKColors.Green,
                                                    ChartRenderer.Stapelart.Keine, ChartRenderer.Strichart.Durchgezogen,4f),
                            "Leistung [kW]", ChartRenderer.Achse.Monate, false,
                            b2Speicher, "Speicherinhalt [kWh]"));

            // --- #240: ERZEUGERSTAPEL MIT VIELEN REIHEN -------------------------------
            //
            // Neun Legendeneintraege mit langen Namen passen nicht in eine Zeile
            // (Anschlag 100 px, Umbruch bei 1 210 px). Die Legende bricht um, und bis
            // #240 lag die zweite Zeile auf dem y-Achsentitel, der 24 px ueber der
            // Zeichenflaeche steht. Seither beginnt die Zeichenflaeche UNTER der
            // gemessenen Legendenhoehe - dasselbe Muster wie in Verlaufsbild
            // (W11b-B-28).
            string[] LANGE_NAMEN =
            {
                "Waermepumpe Nord", "Waermepumpe Sued", "Heizstab Technikraum",
                "Heizkessel Altbau", "Heizkessel Neubau", "Solarthermie Dach",
                "BHKW Grundlast", "BHKW Spitzenlast", "Fernwaerme Uebergabe"
            };
            string[] KURZE_NAMEN = { "A", "B", "C", "D", "E", "F", "G", "H", "I" };

            Pruefe(ziel, "erzeugerstapel_neun_reihen", 1240, 560,
                   new[] { SKColors.Orange, SKColors.Yellow, SKColors.Blue, SKColors.Brown,
                           SKColors.Red, SKColors.SeaGreen, SKColors.DarkOrchid,
                           SKColors.SteelBlue, SKColors.Sienna },
                   () => NeunReihenBild(LANGE_NAMEN));

            // GEGENPROBE zum Versatz: DIESELBEN neun Reihen, nur mit kurzen Namen. Dann
            // passt die Legende in EINE Zeile, und die Zeichenflaeche beginnt wieder bei
            // 110 px. Gemessen wird die oberste RASTERLINIE (Gainsboro, ueber die volle
            // Breite) - ohne den Versatz staende sie in beiden Bildern gleich hoch, und
            // die zweite Legendenzeile laege im Titelbereich.
            Zeichenflaechenversatz("erzeugerstapel_neun_reihen",
                () => NeunReihenBild(KURZE_NAMEN),
                () => NeunReihenBild(LANGE_NAMEN),
                (int)ChartRenderer.LEGENDE_ZEILE);

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
                                                        ChartRenderer.Strichart.Durchgezogen, 2f)
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
                                                    ChartRenderer.Stapelart.Keine, ChartRenderer.Strichart.Durchgezogen,4f),
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

            // Der Ring des DASHBOARDS (#222): OHNE Legende, mit Unterzeile unter der
            // Mittelzahl - und deshalb quadratisch. Die Legende steht dort als HTML
            // neben dem Bild; im PNG waere sie eine Rastergrafik, die der Rahmen
            // abschneidet (Anwenderfoto „Heinestr 15", rechte Grafik).
            Pruefe(ziel, "ring_ohne_legende", 420, 420,
                   new[] { RING_WP, RING_REST_GRAU },
                   () => ChartRenderer.Ring("Waermebedarfsdeckung [%]",
                            new List<ChartRenderer.Ringsegment>
                            {
                                new ChartRenderer.Ringsegment("Waermepumpe", 340, RING_WP),
                                new ChartRenderer.Ringsegment("Rest", 60, RING_REST_GRAU)
                            },
                            85.0, "%", "gedeckt", false));

            // DER 0-%-FALL (#222, Anwenderbefund „Strombedarfsdeckung bei 0 ist das
            // Diagramm nicht gut"): Der ungedeckte Rest ist IMMER ein Segment, bei
            // 0 % also ein grauer VOLLRING. Vorher zeichnete der Stromring dort einen
            // vollen gelben Kreis - das sah aus wie eine Leistung.
            Pruefe(ziel, "ring_null_prozent", 420, 420,
                   new[] { RING_REST_GRAU },
                   () => ChartRenderer.Ring("Strombedarfsdeckung [%]",
                            new List<ChartRenderer.Ringsegment>
                            {
                                new ChartRenderer.Ringsegment("Netzbezug", 10322.36, RING_REST_GRAU)
                            },
                            0.0, "%", "Netzbezug 100 %", false));

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

            // B6b: die Waerme-Autarkie der Solarthermie - derselbe Monatsstapel mit den
            // Rollen der Waermeseite (Solar, Speicherladung, Rest), aus einer echten
            // Aggregation synthetischer Stundenreihen (SolarWaermeMonate).
            Pruefe(ziel, "waerme_autarkie_monate", 978, 542,
                   new[] { Rollenfarbe(Farbrolle.WAERME_SOLAR), Rollenfarbe(Farbrolle.SPEICHERLADUNG),
                           Rollenfarbe(Farbrolle.REST) },
                   () => WaermeAutarkieBild.Png(WaermeAutarkieSatz()));

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
                                                        ChartRenderer.Stapelart.Keine, ChartRenderer.Strichart.Gestrichelt),
                                new ChartRenderer.Reihe("Puffer 2 oben",
                                                        Temperaturreihe(55, 7, 1, 0), TEMP_BLAU),
                                new ChartRenderer.Reihe("Puffer 2 unten",
                                                        Temperaturreihe(41, 5, 1, 0), TEMP_BLAU,
                                                        ChartRenderer.Stapelart.Keine, ChartRenderer.Strichart.Gestrichelt),
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
                                                        ChartRenderer.Stapelart.Keine, ChartRenderer.Strichart.Gestrichelt)
                            },
                            minAuto: true));

            // --- Gebaeudesimulation G2: Raumtemperatur eines Gebaeudes -----------------
            // Raumluft und operative Temperatur mit dem Sollwertband (Heizsollwert als
            // Tag-/Nachtfahrplan, obere Raumtemperatur als Festwert), beide gestrichelt.
            double[] raumluft = Temperaturreihe(21.5, 2.5, 0.8, -Math.PI / 2);
            double[] operativ = Temperaturreihe(21.0, 3.0, 0.5, -Math.PI / 2);
            var sollwert = new double[STUNDEN];
            for (int i = 0; i < STUNDEN; i++) sollwert[i] = (i % 24) >= 6 && (i % 24) <= 21 ? 20.0 : 17.0;
            Pruefe(ziel, "raumtemperatur_gebaeude", 1240, 560,
                   new[] { Rollenfarbe(Farbrolle.SERIE_1), Rollenfarbe(Farbrolle.SERIE_2) },
                   () => ChartRenderer.Raumtemperatur("Raumtemperatur", raumluft, operativ, sollwert, 26.0,
                                                      new ChartRenderer.Raumtemperaturnamen()));

            // Gegenprobe: das Sollwertband muss im Bild stehen - ohne es waeren Masse und
            // Farben der beiden Temperaturreihen dieselben.
            Unterschiedlich("raumtemperatur_sollband_wirkt",
                () => ChartRenderer.Raumtemperatur("Raumtemperatur", raumluft, operativ, null, null,
                                                   new ChartRenderer.Raumtemperaturnamen()),
                () => ChartRenderer.Raumtemperatur("Raumtemperatur", raumluft, operativ, sollwert, 26.0,
                                                   new ChartRenderer.Raumtemperaturnamen()));

            // =========================================================================
            // 37/38 - die ZWEI BILDER DER AUSLEGUNGSOPTIMIERUNG (W11b-B-5)
            // =========================================================================
            //
            // Sie loesen ScottPlot ab - bis zur Windows-Abnahme V2 (07.09.2026) war
            // Form_SpeicherOptimierung der einzige Ort des Programms, an dem eine
            // zweite Zeichenbibliothek lief. Ihre Heatmap sammelte je Lauf eine
            // Farbskala an (Plot.Clear raeumt Plottables, keine Panels); nach sieben
            // Laeufen war die Zeichenflaeche auf null Bildpunkte geschrumpft. Hier gibt
            // es keinen Zeichenzustand mehr - jeder Aufruf liefert ein volles PNG.
            //
            // Das Raster laeuft bewusst ins Negative: Ein zu grosser Speicher traegt
            // seinen Kapitaldienst nicht mehr, und genau das soll die Skala zeigen.
            double[] rasterCRaten = { 0.5, 1.0, 1.5, 2.0, 2.5, 3.0 };
            double[] rasterKapazitaeten = new double[10];
            for (int i = 0; i < 10; i++) rasterKapazitaeten[i] = 500.0 + i * 500.0;
            double[][] rasterWerte = Rasterfeld(rasterKapazitaeten, rasterCRaten);

            Pruefe(ziel, "optimierungsraster", 860, 560,
                   new[] { ChartRenderer.C_RASTER_SCHLECHT, ChartRenderer.C_RASTER_MITTE,
                           ChartRenderer.C_RASTER_GUT, SKColors.Black },
                   () => ChartRenderer.Optimierungsraster("Jahresüberschuss ΔJ [€/a]",
                            "C-Rate [1/h]", "Kapazität [kWh]", "ΔJ [€/a]",
                            rasterCRaten, rasterKapazitaeten, rasterWerte,
                            RASTER_BESTE_ZEILE, RASTER_BESTE_SPALTE));

            Pruefe(ziel, "schnittkurve", 720, 460,
                   new[] { ChartRenderer.C_STAMM, ChartRenderer.C_RASTER_SCHLECHT },
                   () => ChartRenderer.Schnittkurve("Schnittkurve bei 1,5 C",
                            "Kapazität [kWh]", "ΔJ [€/a]",
                            rasterKapazitaeten, Rasterspalte(rasterWerte, RASTER_BESTE_SPALTE),
                            rasterKapazitaeten[RASTER_BESTE_ZEILE],
                            rasterWerte[RASTER_BESTE_ZEILE][RASTER_BESTE_SPALTE]));

            // =========================================================================
            // 39/40 - die GROESSEN-SICHT DER FLOTTE (#193, P4, Konzept 2.5)
            // =========================================================================
            //
            // Dieselbe Rasterkarte, nun mit den zwei Zutaten, die die Flotte braucht:
            // der SCHRAFFUR der unzulaessigen Kandidaten und der FUSSZEILE mit dem
            // SP-O-4-Hinweis. Beides sind optionale Parameter; die Probe 37 darueber
            // ruft die Funktion weiterhin ohne sie und muss byte-gleich bleiben.
            //
            // DIE KARTE IST DUENN BESETZT. Die Groessensuche waehlt unter GERAETEN, und
            // Geraete bilden kein Gitter: Zeilen sind die verschiedenen Kapazitaeten der
            // gefundenen Geraete, Spalten ihre verschiedenen Entladeleistungen, und nur
            // die Zellen, hinter denen wirklich ein Geraet steht, tragen einen Wert. Die
            // C-RATE IST KEINE ACHSE MEHR - sie ist die abgeleitete Kennzahl P/E jedes
            // Geraets. Genau das muss der Renderer koennen: Die uebrigen Zellen sind NaN
            // und duerfen nicht in der Minimumfarbe erscheinen.
            //
            // Unzulaessig ist hier der ganze obere Rand (die zwei groessten Kapazitaeten)
            // und die hoechste Leistung - so liegt Schraffur sowohl an einer Kante als
            // auch quer durch die Flaeche, und das Optimum bleibt frei.
            (double E, double P)[] geraete = Geraeteliste();
            double[] geraeteLeistungen = Geraeteleistungen(geraete);
            double[][] geraeteWerte = Geraetefeld(rasterKapazitaeten, geraeteLeistungen, geraete);
            bool[][] rasterSperre = Rastersperre(rasterKapazitaeten.Length, geraeteLeistungen.Length);

            Pruefe(ziel, "flottenraster_schraffur", 860, 560,
                   new[] { ChartRenderer.C_RASTER_SCHLECHT, ChartRenderer.C_RASTER_MITTE,
                           ChartRenderer.C_RASTER_GUT, SKColors.Black },
                   () => ChartRenderer.Optimierungsraster(
                            "Kapitalwert über Kapazität und Entladeleistung",
                            "Entladeleistung [kW]", "Kapazität [kWh]", "Kapitalwert [€]",
                            geraeteLeistungen, rasterKapazitaeten, geraeteWerte,
                            RASTER_BESTE_ZEILE, GERAETE_BESTE_SPALTE,
                            rasterSperre,
                            "SP-O-4: Die Aussage gilt nur für die geprüften Geräte. "
                            + "Zwischen zwei Geräten ist nichts gerechnet."));

            // Der SCHNITT UEBER DER LEISTUNG laeuft ueber die gefundenen GERAETE, nach
            // ihrer Entladeleistung geordnet - jeder Punkt ist ein Speicher, den es
            // wirklich gibt. Geprueft wird, dass die Kurve das auch mit einer Achse kann,
            // die nicht bei 500 anfaengt und ungleichmaessig geteilt ist.
            double[] geraeteAchse = geraete.OrderBy(g => g.P).Select(g => g.P).ToArray();
            double[] geraeteKurve = geraete.OrderBy(g => g.P).Select(g => Flaeche(g.E, g.P / g.E)).ToArray();
            int besterPunkt = Array.IndexOf(geraeteAchse, 1250.0);

            Pruefe(ziel, "flottenschnitt_leistung", 720, 460,
                   new[] { ChartRenderer.C_STAMM, ChartRenderer.C_RASTER_SCHLECHT },
                   () => ChartRenderer.Schnittkurve(
                            "Kapitalwert über der Entladeleistung der gefundenen Geräte",
                            "Entladeleistung [kW]", "Kapitalwert [€]",
                            geraeteAchse, geraeteKurve,
                            geraeteAchse[besterPunkt], geraeteKurve[besterPunkt]));

            // =========================================================================
            // 41 - die SCHNITTKURVE mit FEINRASTERPUNKTEN (#224)
            // =========================================================================
            //
            // Die zweite Phase der Rastersuche legt ihre Punkte ZWISCHEN die
            // Stuetzstellen des Grobrasters (Anwenderentscheid SD-E-9 / SD-Q10). Im
            // Bild muessen sie zu UNTERSCHEIDEN sein - die Mappe V7 zeichnet dafuer
            // zwei Punktreihen. Hier: neun Grobpunkte auf ganzen Kapazitaeten und acht
            // Feinpunkte im Fenster um das Optimum am rechten Rand, zusammen auf EINER
            // Kurve, die Feinpunkte in C_FEINRASTER und kleiner.
            double[] feinAchse = Feinachse();
            double[] feinWerte = Feinwerte(feinAchse);
            bool[] feinMarken = Feinmarken(feinAchse);

            Pruefe(ziel, "flottenschnitt_feinraster", 720, 460,
                   new[] { ChartRenderer.C_STAMM, ChartRenderer.C_FEINRASTER,
                           ChartRenderer.C_RASTER_SCHLECHT },
                   () => ChartRenderer.Schnittkurve(
                            "Kapitalwert über der Kapazität (Grob- und Feinraster)",
                            "Kapazität [kWh]", "Kapitalwert [€]",
                            feinAchse, feinWerte,
                            feinAchse[feinAchse.Length - 1], feinWerte[feinWerte.Length - 1],
                            feinMarken));

            // =========================================================================
            // 42 - die GROESSENKOPPLUNG "Kapazitaet und Leistung" (#226)
            // =========================================================================
            //
            // Der Fall des Anwenders vom 11.09.2026: 13 x 13 = 169 Kandidaten auf einem
            // Kapazitaet x Leistung-Gitter (20...500 in Schritten von 40). Beide Achsen
            // sind das eingegebene Raster, die Matrix ist LUECKENLOS - und genau das
            // unterscheidet dieses Bild von der C-Raten-Karte darueber, in der dieselben
            // Kandidaten auf 137 krumme Spalten mit ueberwiegend Loechern fielen.
            double[] kapLeistungAchse = new double[13];
            for (int i = 0; i < 13; i++) kapLeistungAchse[i] = 20.0 + 40.0 * i;
            double[][] kapLeistungWerte = Leistungsfeld(kapLeistungAchse, kapLeistungAchse);

            Pruefe(ziel, "flottenraster_leistung", 860, 560,
                   new[] { ChartRenderer.C_RASTER_SCHLECHT, ChartRenderer.C_RASTER_MITTE,
                           ChartRenderer.C_RASTER_GUT, SKColors.Black },
                   () => ChartRenderer.Optimierungsraster(
                            "Kapitalwert über Kapazität und Entladeleistung",
                            "Entladeleistung [kW]", "Kapazität [kWh]", "Kapitalwert [€]",
                            kapLeistungAchse, kapLeistungAchse, kapLeistungWerte,
                            LEISTUNGSFELD_BESTE_ZEILE, LEISTUNGSFELD_BESTE_SPALTE, null,
                            "SP-O-4: Die Aussage gilt nur für das geprüfte endliche Raster. "
                            + "Zwischen zwei Stützstellen ist nichts gerechnet."));

            // =========================================================================
            // 43/44 - die ZWEI BILDER DER STUECKZAHLSUCHE (#247, SD-E-10 / SD-Q17)
            // =========================================================================
            //
            // „Stueckzahl suchen" laesst die Groesse der Einheit stehen und variiert, wie
            // viele davon stehen. Das ist eine GANZE Zahl - deshalb Balken statt Kurve
            // und deshalb kein Feinraster: Zwischen zwei Geraeten gibt es nichts.
            //
            // Die Reihe laeuft bewusst ins Negative (vier Geraete tragen ihren
            // Kapitaldienst nicht mehr), damit die Nulllinie und die rote Saeule im Bild
            // stehen; das dritte Stueck ist unzulaessig und traegt die Schraffur.
            int[] stueckzahlen = { 1, 2, 3, 4, 5 };
            double[] stueckwerte = { 28400.0, 41280.0, 33700.0, 8900.0, -14500.0 };
            bool[] stuecksperre = { false, false, true, false, false };
            const int STUECK_BESTE = 1;                      // 2 Stueck

            Pruefe(ziel, "flotte_stueckzahlkurve", 720, 460,
                   new[] { ChartRenderer.C_STAMM, ChartRenderer.C_RASTER_GUT,
                           ChartRenderer.C_RASTER_SCHLECHT, SKColors.Black },
                   () => ChartRenderer.Stueckzahlkurve(
                            "Kapitalwert über Stückzahl — Growatt WIT-M+APX ESS",
                            "Stückzahl [Stück]", "Kapitalwert [€]",
                            stueckzahlen, stueckwerte, STUECK_BESTE, stuecksperre));

            // Die Karte n1 x n2 ist DIESELBE Zeichnung wie die Groessenkarte, nur mit
            // ganzzahligen Achsen - ein eigener Renderer waere eine zweite Wahrheit ueber
            // dieselbe Flaeche. Geprueft wird, dass sie mit kleinen ganzen Achsen (0…3)
            // dasselbe leistet wie mit Kapazitaeten in Tausendern.
            double[] stueckAchse1 = { 1.0, 2.0, 3.0 };
            double[] stueckAchse2 = { 0.0, 1.0, 2.0 };
            double[][] stueckfeld = Stueckfeld(stueckAchse1, stueckAchse2);

            Pruefe(ziel, "flotte_stueckzahlraster", 860, 560,
                   new[] { ChartRenderer.C_RASTER_SCHLECHT, ChartRenderer.C_RASTER_MITTE,
                           ChartRenderer.C_RASTER_GUT, SKColors.Black },
                   () => ChartRenderer.Optimierungsraster(
                            "Kapitalwert über die Stückzahlen zweier Einheiten",
                            "Stückzahl Einheit 2", "Stückzahl Einheit 1", "Kapitalwert [€]",
                            stueckAchse2, stueckAchse1, stueckfeld, 1, 1, null,
                            "SP-O-4: Die Aussage gilt nur für das geprüfte endliche Raster. "
                            + "Zwischen zwei Stützstellen ist nichts gerechnet."));

            // --- Die JAHRESPROJEKTION der Speicherflotte (#184, P2) ------------------
            //
            // Saeulen je Projektjahr, Linie kumuliert, Ersatzjahre markiert. Das Bild
            // ist der einzige Ort, an dem der Anwender sieht, WANN sich eine Flotte
            // traegt; geprueft werden Masse, die drei Farben und der Determinismus.
            //
            // Jahr 1 ist bewusst NEGATIV (Anlaufjahr) - damit steht die rote Saeule im
            // Bild und die Farbregel „negatives Jahr ist rot" wird mitgeprueft. Das
            // Ersatzjahr 10 traegt Band und Dreieck in derselben Farbe.
            int[] projektjahre = new int[20];
            for (int i = 0; i < 20; i++) projektjahre[i] = i + 1;
            double[] projektionNetto = Projektionsreihe();
            double[] projektionKumuliert = Kumuliert(projektionNetto, -15000.0);
            double[] projektionBetrieb = new double[20];
            for (int i = 0; i < 20; i++) projektionBetrieb[i] = -420.0 - i * 12.0;

            Pruefe(ziel, "jahresprojektion", 1240, 560,
                   new[] { ChartRenderer.C_PV, ChartRenderer.C_STAMM,
                           ChartRenderer.C_RASTER_SCHLECHT },
                   () => ChartRenderer.Jahresprojektion("Jahresprojektion [€]", projektjahre,
                            new ChartRenderer.Reihe("Netto-Cashflow", projektionNetto, ChartRenderer.C_PV),
                            new ChartRenderer.Reihe("kumuliert", projektionKumuliert, ChartRenderer.C_STAMM),
                            new[] { 10 },
                            new List<ChartRenderer.Reihe>
                            {
                                new ChartRenderer.Reihe("Betrieb", projektionBetrieb,
                                                        ChartRenderer.C_BHKW) { Strichart = ChartRenderer.Strichart.Gestrichelt }
                            },
                            "Zahlung [€]", "Projektjahr"));

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

            // #234: Die zweite Achse steht NUR mit gewaehltem Speicher. Ohne diese
            // Gegenprobe bestuende ein Bild, das die Speicherliste stillschweigend
            // uebergeht, jede Mass-, Farb- und Determinismuspruefung - und die
            // Zeichenflaeche bliebe breit, obwohl rechts eine Skala stehen muesste.
            Unterschiedlich("erzeugerstapel_zweite_achse",
                () => ChartRenderer.ErzeugerStapel("Waermeproduktion Jahresganglinie",
                        b2Stapel, new List<ChartRenderer.Reihe>(), null,
                        "Leistung [kW]", ChartRenderer.Achse.Monate, false),
                () => ChartRenderer.ErzeugerStapel("Waermeproduktion Jahresganglinie",
                        b2Stapel, new List<ChartRenderer.Reihe>(), null,
                        "Leistung [kW]", ChartRenderer.Achse.Monate, false,
                        b2Speicher, "Speicherinhalt [kWh]"));

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


            // Dasselbe fuer die ZWEI OPTIMIERUNGSBILDER (W11b-B-5): Beide tragen eine
            // MARKE fuer das Optimum, und beide bestuenden Mass-, Farb- und
            // Determinismuspruefung auch dann, wenn die Marke stillschweigend
            // wegfiele - das Optimum ist genau die Aussage dieser Bilder. Geprueft
            // wird deshalb, dass "mit Marke" und "ohne Marke" zwei verschiedene
            // Bilder sind.
            Unterschiedlich("optimierungsraster_marke",
                () => ChartRenderer.Optimierungsraster("Jahresüberschuss ΔJ [€/a]",
                        "C-Rate [1/h]", "Kapazität [kWh]", "ΔJ [€/a]",
                        rasterCRaten, rasterKapazitaeten, rasterWerte, -1, -1),
                () => ChartRenderer.Optimierungsraster("Jahresüberschuss ΔJ [€/a]",
                        "C-Rate [1/h]", "Kapazität [kWh]", "ΔJ [€/a]",
                        rasterCRaten, rasterKapazitaeten, rasterWerte,
                        RASTER_BESTE_ZEILE, RASTER_BESTE_SPALTE));

            Unterschiedlich("schnittkurve_marke",
                () => ChartRenderer.Schnittkurve("Schnittkurve bei 1,5 C",
                        "Kapazität [kWh]", "ΔJ [€/a]",
                        rasterKapazitaeten, Rasterspalte(rasterWerte, RASTER_BESTE_SPALTE),
                        double.NaN, double.NaN),
                () => ChartRenderer.Schnittkurve("Schnittkurve bei 1,5 C",
                        "Kapazität [kWh]", "ΔJ [€/a]",
                        rasterKapazitaeten, Rasterspalte(rasterWerte, RASTER_BESTE_SPALTE),
                        rasterKapazitaeten[RASTER_BESTE_ZEILE],
                        rasterWerte[RASTER_BESTE_ZEILE][RASTER_BESTE_SPALTE]));

            // Dasselbe fuer die SCHRAFFUR der Groessen-Sicht (#193): Masse, Farben und
            // Determinismus stimmen auch dann, wenn der Parameter "unzulaessig"
            // stillschweigend uebergangen wuerde - und dann stuende ein gesperrter
            // Kandidat als gruenes Feld im Bild. Geprueft wird deshalb, dass "mit
            // Schraffur" und "ohne Schraffur" zwei verschiedene Bilder sind. Der
            // erste Aufruf ist zugleich der Beleg, dass die Probe 37 (Aufruf OHNE die
            // zwei neuen Parameter) unberuehrt bleibt.
            Unterschiedlich("flottenraster_schraffur_wirkt",
                () => ChartRenderer.Optimierungsraster("Kapitalwert über Kapazität und C-Rate",
                        "C-Rate [1/h]", "Kapazität [kWh]", "Kapitalwert [€]",
                        rasterCRaten, rasterKapazitaeten, rasterWerte,
                        RASTER_BESTE_ZEILE, RASTER_BESTE_SPALTE),
                () => ChartRenderer.Optimierungsraster("Kapitalwert über Kapazität und C-Rate",
                        "C-Rate [1/h]", "Kapazität [kWh]", "Kapitalwert [€]",
                        rasterCRaten, rasterKapazitaeten, rasterWerte,
                        RASTER_BESTE_ZEILE, RASTER_BESTE_SPALTE, rasterSperre));

            // Und dasselbe fuer das LOCH (#226): Bis dahin bekam jede nicht gerechnete
            // Stelle die MINIMUMFARBE - eine Aussage ueber eine Variante, die es nie
            // gab. Die Gegenprobe stellt zwei Raster nebeneinander, die sich in genau
            // EINER Zelle unterscheiden: einmal NaN, einmal der kleinste Wert des
            // Feldes. Vor der Behebung waren beide Bilder byte-gleich (beide rot);
            // Masse, Farben und Determinismus haetten das nie bemerkt. Skalengrenzen
            // und damit alle uebrigen Zellen bleiben gleich, weil das Minimum an der
            // Ecke [12][12] stehen bleibt.
            double loch = kapLeistungWerte.SelectMany(z => z).Min();

            Unterschiedlich("flottenraster_loch_ist_kein_minimum",
                () => ChartRenderer.Optimierungsraster(
                        "Kapitalwert über Kapazität und Entladeleistung",
                        "Entladeleistung [kW]", "Kapazität [kWh]", "Kapitalwert [€]",
                        kapLeistungAchse, kapLeistungAchse,
                        MitStelle(kapLeistungWerte, 0, 0, double.NaN),
                        LEISTUNGSFELD_BESTE_ZEILE, LEISTUNGSFELD_BESTE_SPALTE),
                () => ChartRenderer.Optimierungsraster(
                        "Kapitalwert über Kapazität und Entladeleistung",
                        "Entladeleistung [kW]", "Kapazität [kWh]", "Kapitalwert [€]",
                        kapLeistungAchse, kapLeistungAchse,
                        MitStelle(kapLeistungWerte, 0, 0, loch),
                        LEISTUNGSFELD_BESTE_ZEILE, LEISTUNGSFELD_BESTE_SPALTE));

            // Und dasselbe fuer die ZWEITE FUSSZEILE (#255/#360): Hat ein FEINPUNKT den
            // Lauf gewonnen, traegt die Karte unter dem SP-O-4-Hinweis einen zweiten
            // Satz - die Karte markiert dann NICHT das beste Ergebnis, und ohne diesen
            // Satz laese sich die Marke als das Beste des Laufs.
            //
            // BIS #360 HING DER ZUSATZ HINTER DEM GRUNDTEXT und damit an dessen letzter
            // Zeile. Unter der Achsenbeschriftung bleiben bis zur Bildkante 560 aber nur
            // rund 28 Bildpunkte, und eine Zeile der 13-Punkt-Schrift ist je nach
            // Schriftart 17 bis 22 hoch: Auf einer Schrift mit hoher Zeile lag die
            // zweite Zeile GANZ unter dem Bildrand, der Zusatz war nirgends zu sehen
            // und zwei Karten mit und ohne ihn waren byte-gleich. Jetzt steht er auf
            // einer eigenen Zeile, und das Bild waechst so weit, dass der ganze Block
            // darin steht.
            //
            // Zwei Gegenproben: dass der Zusatz UEBERHAUPT etwas aendert, und dass seine
            // LETZTE Zeile im Bild steht - dafuer unterscheiden sich die zwei Texte nur
            // in ihren letzten Woertern.
            const string fussGrund =
                "SP-O-4: Die Aussage gilt nur für das geprüfte endliche Raster. Zwischen "
              + "zwei Stützstellen ist nichts gerechnet; das markierte Optimum ist das "
              + "beste geprüfte, nicht das global beste.";
            const string fussZusatz =
                "Das beste Ergebnis stammt aus dem Feinraster (12.345 € bei 233,3 kWh) und "
              + "liegt zwischen zwei Stützstellen; markiert ist hier das Grob-Optimum, "
              + "gezeigt wird es im Ausschnitt um das ";

            Unterschiedlich("flottenraster_zusatzzeile_wirkt",
                () => ChartRenderer.Optimierungsraster(
                        "Kapitalwert über Kapazität und Entladeleistung",
                        "Entladeleistung [kW]", "Kapazität [kWh]", "Kapitalwert [€]",
                        kapLeistungAchse, kapLeistungAchse, kapLeistungWerte,
                        LEISTUNGSFELD_BESTE_ZEILE, LEISTUNGSFELD_BESTE_SPALTE, null,
                        fussGrund),
                () => ChartRenderer.Optimierungsraster(
                        "Kapitalwert über Kapazität und Entladeleistung",
                        "Entladeleistung [kW]", "Kapazität [kWh]", "Kapitalwert [€]",
                        kapLeistungAchse, kapLeistungAchse, kapLeistungWerte,
                        LEISTUNGSFELD_BESTE_ZEILE, LEISTUNGSFELD_BESTE_SPALTE, null,
                        fussGrund, fussZusatz + "Optimum."));

            Unterschiedlich("flottenraster_zusatz_letzte_zeile_steht_im_bild",
                () => ChartRenderer.Optimierungsraster(
                        "Kapitalwert über Kapazität und Entladeleistung",
                        "Entladeleistung [kW]", "Kapazität [kWh]", "Kapitalwert [€]",
                        kapLeistungAchse, kapLeistungAchse, kapLeistungWerte,
                        LEISTUNGSFELD_BESTE_ZEILE, LEISTUNGSFELD_BESTE_SPALTE, null,
                        fussGrund, fussZusatz + "Optimum."),
                () => ChartRenderer.Optimierungsraster(
                        "Kapitalwert über Kapazität und Entladeleistung",
                        "Entladeleistung [kW]", "Kapazität [kWh]", "Kapitalwert [€]",
                        kapLeistungAchse, kapLeistungAchse, kapLeistungWerte,
                        LEISTUNGSFELD_BESTE_ZEILE, LEISTUNGSFELD_BESTE_SPALTE, null,
                        fussGrund, fussZusatz + "Grob-Optimum."));

            // Und dasselbe fuer die FEINRASTERPUNKTE (#224): Masse, Farben und
            // Determinismus stimmen auch dann, wenn der Parameter stillschweigend
            // ignoriert wuerde - alle Punkte staenden dann in C_STAMM. Die Gegenprobe
            // stellt DIESELBE Kurve zweimal nebeneinander, einmal ohne und einmal mit
            // der Phasenmarke; ohne Marke muss das Bild byte-gleich zu dem vor #224
            // sein, mit Marke anders.
            Unterschiedlich("flottenschnitt_feinpunkte_wirken",
                () => ChartRenderer.Schnittkurve(
                        "Kapitalwert über der Kapazität (Grob- und Feinraster)",
                        "Kapazität [kWh]", "Kapitalwert [€]",
                        feinAchse, feinWerte,
                        feinAchse[feinAchse.Length - 1], feinWerte[feinWerte.Length - 1]),
                () => ChartRenderer.Schnittkurve(
                        "Kapitalwert über der Kapazität (Grob- und Feinraster)",
                        "Kapazität [kWh]", "Kapitalwert [€]",
                        feinAchse, feinWerte,
                        feinAchse[feinAchse.Length - 1], feinWerte[feinWerte.Length - 1],
                        feinMarken));

            // Dasselbe fuer die ERSATZJAHR-MARKE der Jahresprojektion (#184): Masse,
            // Farben und Determinismus stimmen auch dann, wenn die Marke stillschweigend
            // an derselben Stelle bliebe. Geprueft wird deshalb, dass ein ANDERES
            // Ersatzjahr ein anderes Bild ergibt - Band und Dreieck stehen dann ueber
            // einer anderen Saeule.
            Unterschiedlich("jahresprojektion_ersatzjahr",
                () => ChartRenderer.Jahresprojektion("Jahresprojektion [€]", projektjahre,
                        new ChartRenderer.Reihe("Netto-Cashflow", projektionNetto, ChartRenderer.C_PV),
                        new ChartRenderer.Reihe("kumuliert", projektionKumuliert, ChartRenderer.C_STAMM),
                        new[] { 10 }, null, "Zahlung [€]", "Projektjahr"),
                () => ChartRenderer.Jahresprojektion("Jahresprojektion [€]", projektjahre,
                        new ChartRenderer.Reihe("Netto-Cashflow", projektionNetto, ChartRenderer.C_PV),
                        new ChartRenderer.Reihe("kumuliert", projektionKumuliert, ChartRenderer.C_STAMM),
                        new[] { 14 }, null, "Zahlung [€]", "Projektjahr"));

            // Dasselbe fuer die ZWEI Zusaetze des Rings (#222). Beide bestuenden Mass-,
            // Farb- und Determinismuspruefung auch dann, wenn der Renderer sie
            // stillschweigend uebergehen wuerde - und dann stuende die Legende weiter
            // im Bild bzw. die Mitte traege eine nackte Null ohne den Satz dazu.
            var ringDeckung = new List<ChartRenderer.Ringsegment>
            {
                new ChartRenderer.Ringsegment("Photovoltaik", 220, RING_WP),
                new ChartRenderer.Ringsegment("Netzbezug", 95, RING_REST_GRAU)
            };
            var ringNull = new List<ChartRenderer.Ringsegment>
            {
                new ChartRenderer.Ringsegment("Netzbezug", 315, RING_REST_GRAU)
            };

            Unterschiedlich("ring_legende_weglassen",
                () => ChartRenderer.Ring("Stromdeckung", ringDeckung, 69.8, "%"),
                () => ChartRenderer.Ring("Stromdeckung", ringDeckung, 69.8, "%", "gedeckt", false));

            // „0 % zeichnet den grauen Vollring": Derselbe Aufruf mit einem
            // Deckungssegment ergibt ein ANDERES Bild - waere der Vollring nur die
            // unveraenderte Zeichnung von irgendetwas, faellt es hier auf.
            Unterschiedlich("ring_null_prozent_vollring",
                () => ChartRenderer.Ring("Stromdeckung", ringDeckung, 69.8, "%", "gedeckt", false),
                () => ChartRenderer.Ring("Stromdeckung", ringNull, 0.0, "%", "Netzbezug 100 %", false));

            // Und dasselbe fuer die BESTWERTMARKE der Stueckzahlkurve (#247): Masse,
            // Farben und Determinismus stimmen auch dann, wenn die Marke stillschweigend
            // an einer anderen Saeule stuende - oder gar nicht. Die Gegenprobe stellt
            // DIESELBE Reihe zweimal nebeneinander, einmal ohne Marke und einmal mit ihr
            // auf dem Optimum; ohne Marke traegt keine Saeule die gruene Farbe und das
            // schwarze Quadrat fehlt.
            Unterschiedlich("flotte_stueckzahl_bestmarke_wirkt",
                () => ChartRenderer.Stueckzahlkurve(
                        "Kapitalwert über Stückzahl — Growatt WIT-M+APX ESS",
                        "Stückzahl [Stück]", "Kapitalwert [€]",
                        stueckzahlen, stueckwerte, -1, stuecksperre),
                () => ChartRenderer.Stueckzahlkurve(
                        "Kapitalwert über Stückzahl — Growatt WIT-M+APX ESS",
                        "Stückzahl [Stück]", "Kapitalwert [€]",
                        stueckzahlen, stueckwerte, STUECK_BESTE, stuecksperre));

            // AUFTRAG U18 - die zwei Gegenproben zur Legende des Kapitalwert-Verlaufs.
            //
            // Erstens die STRICHART: Masse, Farben und Determinismus stimmen auch dann,
            // wenn der Renderer das Merkmal Strichart einer Reihe stillschweigend
            // uebergeht - beide Bilder waeren dann byte-gleich. Genau so war es bis zu
            // diesem Auftrag: Das Feld stand an Reihe, dieses Bild las es nicht.
            Unterschiedlich("kapitalwert_verlauf_gestrichelt_wirkt",
                () => ChartRenderer.KapitalwertVerlauf("Kumulierte Barwerte je Version",
                        ChartRenderer.VerlaufsReihen(serien, true), null),
                () => ChartRenderer.KapitalwertVerlauf("Kumulierte Barwerte je Version",
                        VerlaufMitGestricheltemStamm(serien), null));

            // Zweitens die NAMEN: Ohne Legende stuende in beiden Bildern dasselbe. Der
            // Wortlaut der Namen ist der einzige Unterschied, die Reihen selbst sind
            // dieselben - dasselbe Muster wie bei den neun Legendeneintraegen des
            // Erzeugerstapels.
            Unterschiedlich("kapitalwert_verlauf_legende_nennt_die_version",
                () => ChartRenderer.KapitalwertVerlauf("Kumulierte Barwerte je Version",
                        VerlaufMitNamen(serien, "Stamm", "Variante A", "Variante B"), null),
                () => ChartRenderer.KapitalwertVerlauf("Kumulierte Barwerte je Version",
                        VerlaufMitNamen(serien, "Bestand", "Version 1", "Version 2"), null));

            // ETAPPE E6 - der Verlauf mit drei Szenarien und die dritte Strichart: Mass-,
            // Gegen- und SVG-Proben in Program.Szenarien.cs.
            SzenarienProben(ziel);

            // ETAPPE E6, Nachtrag E5b - das Spannenbild (Bandbreite je Version als Balken):
            // Mass-, Gegen- und SVG-Proben in Program.Spanne.cs.
            SpannenProben(ziel);

            // ETAPPE E8a (U41) - das Brueckenbild von der Investition zur
            // Kapitalwertdifferenz: Mass-, Gegen- und SVG-Proben in Program.Bruecke.cs.
            BrueckenProben(ziel);

            // ETAPPE E8a (U42) - das Zahlungsstrombild je Jahr (gestapelte Jahresbalken, Ausgaben
            // nach unten, Ersatzjahre markiert): Mass-, Gegen- und SVG-Proben in
            // Program.Zahlungsstrom.cs.
            ZahlungsstromProben(ziel);

            // ZAPFPROFILGENERATOR Z1 - die Vorschaubilder Tagesgang, Wochenprofil und Jahresgang:
            // Mass-, Gegen- und SVG-Proben in Program.Zapfprofil.cs.
            ZapfprofilProben(ziel);

            // ZAPFPROFILGENERATOR Z2 - die Bilder der Auslegung (Summenlinie, Wertepaarkurve,
            // maßgebende Woche): Mass-, Gegen- und SVG-Proben in Program.Auslegung.cs.
            AuslegungProben(ziel);

            // ANLAGENKOPPLUNG AK1 WELLE 3 - das Bild „Vorlauf und Rücklauf" mit Lücken ohne
            // Heizbetrieb: Mass-, Gegen- und SVG-Proben in Program.Anlagenkopplung.cs.
            AnlagenkopplungProben(ziel);

            // AUFTRAG DF-1 - die Gegenprobe zur einstellbaren Palette.
            //
            // Masse, Farben und Determinismus stimmen auch dann, wenn Farbpalette.Aktuell
            // beim Malen gar nicht gelesen wuerde: Beide Bilder waeren dann byte-gleich,
            // und der Einstellungsdialog haette keine Wirkung. Hier steht dasselbe Bild
            // zweimal, einmal mit den Hausfarben und einmal mit einer Palette, in der die
            // Waermepumpe ROT ist.
            //
            // OHNE MESSLATTE: Das zweite Bild haengt an einer Anwendereinstellung und
            // gehoert nicht in die eingefrorene Hashliste - sie misst die Vorgabe.
            var rot = new Farbpalette(
                new Dictionary<Farbrolle, Farbe> { { Farbrolle.WAERME_WP, new Farbe(0xFF, 0x00, 0x00) } },
                Farbpalette.Vorgabe);
            Unterschiedlich("palette_abweichend_wirkt",
                () => ChartRenderer.JahresverlaufWaerme(z),
                () => MitPalette(rot, () => ChartRenderer.JahresverlaufWaerme(z)),
                false);

            // =========================================================================
            // AUFTRAG DG-E2 - DIE SVG-GEGENPROBE.
            //
            // Derselbe Jahresgang, zweiter Ausgabeweg: SvgSchreiber statt SkiaMaler.
            // Geprueft wird, was ein PNG-Vergleich nicht sehen kann - Determinismus
            // des Textes, der Aufbau des Baums, die Wirkung der Palette und die
            // viewBox des inneren svg im Fenster.
            //
            // OHNE ABLAGE UND OHNE MESSLATTE: Hier entsteht kein PNG; die eingefrorene
            // Hashliste misst den PNG-Weg und bekommt hier keine Zeile.
            // =========================================================================
            var svgReihen = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("Temperatur", klimaTemperatur,
                                        ChartRenderer.C_AUSSENTEMPERATUR)
            };
            Func<ChartRenderer.Achsenfenster, Zeichenmodell> svgModell =
                f => ChartRenderer.JahresgangModell("Jahrestemperatur Verlauf", svgReihen,
                                                    "Monat", "Temperatur [°C]", false, f);

            // (a) Zweimal geschrieben - und zweimal ERZEUGT - ist byte-gleich. Ohne
            //     diese Probe koennte sich ein Zufall (Woerterbuchreihenfolge, Zeit,
            //     Kultur) in den Text schleichen, den kein Aufbau-Test bemerkt.
            SvgProbe("svg_jahresgang_byte_gleich", e =>
            {
                Zeichenmodell m = svgModell(null);
                string a = SvgSchreiber.Text(m);
                string b = SvgSchreiber.Text(m);
                e.Masse = m.Breite + "x" + m.Hoehe;
                e.Groesse = Encoding.UTF8.GetByteCount(a).ToString("N0", CultureInfo.InvariantCulture);

                if (!string.Equals(a, b, StringComparison.Ordinal))
                    e.Maengel.Add("zweimal geschrieben ist nicht byte-gleich");
                if (!string.Equals(a, SvgSchreiber.Text(svgModell(null)), StringComparison.Ordinal))
                    e.Maengel.Add("zweimal erzeugt ist nicht byte-gleich");
            });

            // (b) Der Aufbau: je Reihe GENAU EIN <path class="epos-reihe"> im inneren
            //     svg, dieses mit preserveAspectRatio="none", jeder Reihenpfad mit
            //     vector-effect und data-marke, und mindestens so viele <text> wie das
            //     Modell Beschriftungen fuehrt. Ohne das bestuende ein Schreiber, der
            //     die Reihen weglaesst, jede Determinismuspruefung.
            SvgProbe("svg_jahresgang_struktur", e =>
            {
                Zeichenmodell m = svgModell(null);
                SvgKnoten baum = SvgSchreiber.Baum(m);
                List<SvgKnoten> alle = baum.Alle().ToList();
                e.Masse = m.Breite + "x" + m.Hoehe;
                e.Knoten = alle.Count.ToString(CultureInfo.InvariantCulture);

                List<SvgKnoten> pfade = alle.Where(
                    k => k.Name == "path" && Attributwert(k, "class") == "epos-reihe").ToList();
                if (pfade.Count != m.Reihen.Count)
                    e.Maengel.Add("Reihenpfade: " + pfade.Count + " statt " + m.Reihen.Count);
                foreach (SvgKnoten pf in pfade)
                {
                    if (Attributwert(pf, "vector-effect") != "non-scaling-stroke")
                        e.Maengel.Add("Reihenpfad ohne vector-effect");
                    string marke = Attributwert(pf, "data-marke");
                    if (marke == null || !marke.StartsWith("reihe:", StringComparison.Ordinal))
                        e.Maengel.Add("Reihenpfad ohne data-marke");
                    if (string.IsNullOrEmpty(Attributwert(pf, "d")))
                        e.Maengel.Add("Reihenpfad ohne Punkte");
                }

                SvgKnoten flaeche = alle.FirstOrDefault(
                    k => k.Name == "svg" && Attributwert(k, "class") == "epos-flaeche");
                if (flaeche == null) e.Maengel.Add("kein inneres svg");
                else if (Attributwert(flaeche, "preserveAspectRatio") != "none")
                    e.Maengel.Add("inneres svg ohne preserveAspectRatio=none");

                int texte = alle.Count(k => k.Name == "text");
                int beschriftungen = Textbefehle(m.Befehle);
                if (texte < beschriftungen)
                    e.Maengel.Add("nur " + texte + " <text> zu " + beschriftungen + " Beschriftungen");

                if (!alle.Any(k => Attributwert(k, "data-marke") != null))
                    e.Maengel.Add("keine data-marke im Baum");
            });

            // (c) Die Palette wirkt beim SCHREIBEN - genau wie beim Malen. Ohne diese
            //     Probe bliebe unbemerkt, dass der Schreiber die Farbrolle gar nicht
            //     aufloest: Der Text saehe mit jeder Palette gleich aus.
            var svgRot = new Farbpalette(
                new Dictionary<Farbrolle, Farbe>
                { { Farbrolle.AUSSENTEMPERATUR, new Farbe(0xFF, 0x00, 0x00, 90) } },
                Farbpalette.Vorgabe);
            SvgProbe("svg_palette_wirkt", e =>
            {
                Zeichenmodell m = svgModell(null);
                string vorgabe = SvgSchreiber.Text(m, Farbpalette.Vorgabe);
                string getauscht = SvgSchreiber.Text(m, svgRot);
                e.Groesse = Encoding.UTF8.GetByteCount(vorgabe)
                                    .ToString("N0", CultureInfo.InvariantCulture);

                if (string.Equals(vorgabe, getauscht, StringComparison.Ordinal))
                    e.Maengel.Add("die getauschte Palette aendert den Text nicht");
                if (!string.Equals(vorgabe, SvgSchreiber.Text(m), StringComparison.Ordinal))
                    e.Maengel.Add("die Vorgabe-Palette aendert den Text");

                string hausfarbe = Reihenfarbe(SvgSchreiber.Baum(m, Farbpalette.Vorgabe));
                string getauschte = Reihenfarbe(SvgSchreiber.Baum(m, svgRot));
                if (hausfarbe != "#4682B4")
                    e.Maengel.Add("Reihenfarbe der Vorgabe ist " + (hausfarbe ?? "nicht da"));
                if (getauschte != "#FF0000")
                    e.Maengel.Add("Reihenfarbe der getauschten Palette ist " + (getauschte ?? "nicht da"));
            });

            // (d) Im Fenster steht die viewBox des inneren svg auf den FENSTERSTUNDEN.
            //     Ohne diese Probe bestuende ein Schreiber, der das Datenfenster
            //     stillschweigend uebergeht - der Ausschnitt zeigte weiter das Jahr.
            SvgProbe("svg_jahresgang_fenster", e =>
            {
                SvgKnoten voll = SvgSchreiber.Baum(svgModell(null));
                SvgKnoten teil = SvgSchreiber.Baum(svgModell(fensterKlima));
                string boxVoll = Flaechenbox(voll);
                string boxTeil = Flaechenbox(teil);
                e.Masse = "Fenster";
                e.Knoten = teil.Alle().Count().ToString(CultureInfo.InvariantCulture);

                // 2 900 bis 3 400 (ausschliesslich) sind 500 Stuetzstellen, also die
                // Stunden 2 900 bis 3 399 - eine Spanne von 499.
                string erwartet = "2900 0 499 ";
                if (boxTeil == null || !boxTeil.StartsWith(erwartet, StringComparison.Ordinal))
                    e.Maengel.Add("viewBox steht nicht auf den Fensterstunden: " +
                                  (boxTeil ?? "fehlt"));
                if (boxVoll == null || !boxVoll.StartsWith("0 0 8759 ", StringComparison.Ordinal))
                    e.Maengel.Add("viewBox der Vollansicht: " + (boxVoll ?? "fehlt"));
            });

            // =========================================================================
            // AUFTRAG DG-E3a - DIE SIEBEN ZEITREIHEN-MODELLE DER ERGEBNISREITER.
            //
            // Dieselben Gaben, aus denen die PNG-Proben weiter oben ihr Bild ziehen,
            // gehen hier durch den zweiten Ausgabeweg. Geprueft wird, was ein
            // PNG-Vergleich nicht sehen kann: Determinismus des Textes, EIN
            // path.epos-reihe je Datenreihe, Flaechen mit fill und geschlossenem Zug,
            // die viewBox-Hoehe in BILDPUNKTEN (DG-E3-1) und die zweite Achse.
            //
            // OHNE ABLAGE UND OHNE MESSLATTE: Hier entsteht kein PNG; die eingefrorene
            // Hashliste misst den PNG-Weg und bekommt hier keine Zeile.
            // =========================================================================
            var e3Temperaturen = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("Puffer 1 oben", Temperaturreihe(62, 8, 0, 0), TEMP_ROT),
                new ChartRenderer.Reihe("Puffer 1 unten", Temperaturreihe(48, 6, 0, 0), TEMP_ROT,
                                        ChartRenderer.Stapelart.Keine, ChartRenderer.Strichart.Gestrichelt),
                new ChartRenderer.Reihe("Puffer 2 oben", Temperaturreihe(55, 7, 1, 0), TEMP_BLAU),
                new ChartRenderer.Reihe("Puffer 2 unten", Temperaturreihe(41, 5, 1, 0), TEMP_BLAU,
                                        ChartRenderer.Stapelart.Keine, ChartRenderer.Strichart.Gestrichelt),
                new ChartRenderer.Reihe("Quelltemperatur Erdreich",
                                        Temperaturreihe(11, 4, 0, -Math.PI / 2), TEMP_QUELLE)
            };

            // Lastgang und Speicherbetrieb (B10): vier LEISTUNGEN links, der
            // Ladezustand als ENERGIE rechts - das Bild mit der zweiten Achse aus
            // Verlaufsbild. Es hat keine PNG-Probe; die Gegenprobe ist seine erste.
            double[] bezugOhne = Jahresreihe(140, 90, 30, 0, Math.PI / 2);
            double[] bezugMit = Jahresreihe(100, 80, 15, 0, Math.PI / 2);
            var speicherleistung = new double[bezugOhne.Length];
            for (int i = 0; i < speicherleistung.Length; i++)
                speicherleistung[i] = bezugOhne[i] - bezugMit[i];
            var kappung = new double[bezugOhne.Length];
            for (int i = 0; i < kappung.Length; i++) kappung[i] = 110.0;
            double[] ladezustand = Jahresreihe(400, 120, 180, 0.7, Math.PI / 2);

            var e3Bilder = new List<KeyValuePair<string, Func<Zeichenmodell>>>
            {
                Modellprobe("jahresgang", () => svgModell(null)),
                Modellprobe("kostenprofil",
                    () => ChartRenderer.KostenprofilModell("Kostenprofil", profil, "ct/kWh", "Monat")),
                Modellprobe("stundenprofil_woche",
                    () => ChartRenderer.StundenprofilModell("Wochenwerte", wochenprofil, 24,
                                                            "Wochenstunde (1..168)", "Verteilung")),
                Modellprobe("jahresverlauf_bedarf",
                    () => ChartRenderer.JahresverlaufModell("Jahresuebersicht", jahresverlauf,
                                                            "Waermebedarf [kW]", SKColors.SteelBlue)),
                Modellprobe("ganglinie_normiert",
                    () => ChartRenderer.GanglinieNormiertModell("Waermelast Jahresganglinie", b1Reihen,
                                                                "Anteil am Hoechstwert",
                                                                ChartRenderer.Achse.Monate, false)),
                Modellprobe("erzeugerstapel_waerme",
                    () => ChartRenderer.ErzeugerStapelModell("Waermeproduktion Jahresganglinie",
                            b2Stapel, new List<ChartRenderer.Reihe>(),
                            new ChartRenderer.Reihe("Gesamt", gesamtlast, SKColors.Green,
                                                    ChartRenderer.Stapelart.Keine, ChartRenderer.Strichart.Durchgezogen,4f),
                            "Waermelast [kW]", ChartRenderer.Achse.Monate, false,
                            new List<ChartRenderer.Reihe>
                            { new ChartRenderer.Reihe("Waermebedarf", gesamtlast, SKColors.DarkCyan) },
                            "Bedarf [kW]")),
                Modellprobe("erzeugerstapel_zwei_speicher",
                    () => ChartRenderer.ErzeugerStapelModell("Waermeproduktion Jahresganglinie",
                            b2Stapel,
                            new List<ChartRenderer.Reihe>
                            { new ChartRenderer.Reihe("Waermebedarf", gesamtlast, SKColors.DarkCyan,
                                                      ChartRenderer.Stapelart.Keine, ChartRenderer.Strichart.Durchgezogen,2f) },
                            new ChartRenderer.Reihe("Gesamt", gesamtlast, SKColors.Green,
                                                    ChartRenderer.Stapelart.Keine, ChartRenderer.Strichart.Durchgezogen,4f),
                            "Leistung [kW]", ChartRenderer.Achse.Monate, false,
                            b2Speicher, "Speicherinhalt [kWh]")),
                Modellprobe("temperaturverlauf",
                    () => ChartRenderer.TemperaturverlaufModell("Speichertemperaturen",
                                                                e3Temperaturen, true)),
                Modellprobe("speicherbetrieb",
                    () => ChartRenderer.SpeicherbetriebModell("Lastgang und Speicherbetrieb",
                            new List<ChartRenderer.Reihe>
                            {
                                new ChartRenderer.Reihe("Netzbezug ohne Speicher", bezugOhne,
                                                        SKColors.Gray),
                                new ChartRenderer.Reihe("Netzbezug mit Speicher", bezugMit,
                                                        SKColors.SteelBlue),
                                new ChartRenderer.Reihe("Kappungsschwelle", kappung, SKColors.Red,
                                                        ChartRenderer.Stapelart.Keine, ChartRenderer.Strichart.Gestrichelt),
                                new ChartRenderer.Reihe("Speicherleistung", speicherleistung,
                                                        SKColors.Green)
                            },
                            "Leistung [kW]",
                            new ChartRenderer.Reihe("Ladezustand", ladezustand,
                                                    SKColors.MediumVioletRed),
                            "Ladezustand [kWh]"))
            };

            // Die Gegenproben - der Jahresgang steht schon oben, er wird hier nur
            // abgelegt.
            foreach (KeyValuePair<string, Func<Zeichenmodell>> b in e3Bilder)
                if (b.Key != "jahresgang") SvgModellprobe(b.Key, b.Value);

            if (_svgdatei != null) SvgAblegen(svgModell(null));
            if (_svgordner != null) SvgOrdnerSchreiben(e3Bilder);

            // Die Gegenproben der GRUPPE (d) - die vier reinen Berichtsbilder
            // (Program.GruppeD.cs). Sie schreiben mit --svg-alle ihre eigenen Dateien.
            GruppeDProben(z);
            // Die Gruppe (b) bringt ihre Gegenproben, ihre Daten und ihre SVG-Dateien
            // selbst mit (Program.GruppeB.cs).
            GruppeBProben();
            // AUFTRAG DG-E3c - die sieben Bilder OHNE Zeitachse. Sie stehen in
            // Program.GruppeC.cs und bringen ihre Gaben selbst mit; die Registrierung
            // ist diese eine Zeile.
            GruppeCProben();
            // BV-E5 - die Bildgroesse Stufe 2: die dreizehn Berichtsbilder im Zielmass eines
            // Bildrahmens (halbe Satzspiegelbreite, hohe Form) in Program.Zielgroesse.cs.
            ZielgroessenProben(ziel, z, serien);

            Console.WriteLine(new string('-', 92));
            Console.WriteLine(_bilder + " Bilder geprueft, " + _verstoesse + " Verstoesse.");
            MesslatteSchreiben();
            if (_verstoesse == 0) Console.WriteLine("ERGEBNIS: alle gruen.");
            else Console.WriteLine("ERGEBNIS: FEHLGESCHLAGEN.");
            return _verstoesse == 0 ? 0 : 1;
        }

        /// <summary>
        /// Zeichnet EIN Bild mit einer anderen Palette und stellt die Vorgabe danach
        /// wieder her — auch wenn der Renderer wirft. <c>Farbpalette.Aktuell</c> ist
        /// prozessweiter Zustand; die Probe läuft einfädig.
        /// </summary>
        private static byte[] MitPalette(Farbpalette palette, Func<byte[]> zeichnen)
        {
            Farbpalette.Aktuell = palette;
            try { return zeichnen(); }
            finally { Farbpalette.Zuruecksetzen(); }
        }

        // =================================================================================
        // Ablage und Messlatte (Auftrag DG-E0)
        // =================================================================================

        /// <summary>
        /// Legt EIN gezeichnetes PNG in der Ablage ab und merkt sich seinen SHA-256.
        ///
        /// <para>Die Probe ruft das fuer jedes Bild, das sie erzeugt — also auch fuer die
        /// beiden Bilder einer Gegenprobe (<see cref="Unterschiedlich"/>), die im Bestand
        /// nur miteinander verglichen und nie geschrieben werden. Die Messlatte soll den
        /// GANZEN Zeichenweg abdecken: Was sie nicht nennt, kann sich beim Umbau auf das
        /// Zeichenmodell unbemerkt aendern.</para>
        ///
        /// <para>Ohne <c>--ablage</c> und ohne <c>--hashes</c> tut die Methode nichts —
        /// die Probe verhaelt sich dann Byte fuer Byte wie zuvor.</para>
        /// </summary>
        private static void Ablegen(string dateiname, byte[] daten)
        {
            if (daten == null) return;
            if (_ablage == null && _hashdatei == null) return;

            if (_ablage != null)
                File.WriteAllBytes(Path.Combine(_ablage, dateiname), daten);

            if (_hashdatei != null)
                _messlatte[dateiname] =
                    Convert.ToHexString(SHA256.HashData(daten)).ToLowerInvariant();
        }

        /// <summary>
        /// Schreibt die Hashliste: je Bild eine Zeile <c>&lt;sha256&gt;&#160;&#160;&lt;name&gt;.png</c>,
        /// nach Name geordnet, mit LF und OHNE BOM. Das Format ist das von
        /// <c>sha256sum</c>, damit die Liste auch ausserhalb der Probe nachgerechnet
        /// werden kann (<c>sha256sum -c</c> im Ablageordner).
        /// </summary>
        private static void MesslatteSchreiben()
        {
            if (_hashdatei == null) return;

            string ordner = Path.GetDirectoryName(Path.GetFullPath(_hashdatei));
            if (!string.IsNullOrEmpty(ordner)) Directory.CreateDirectory(ordner);

            var text = new StringBuilder();
            foreach (KeyValuePair<string, string> zeile in _messlatte)
                text.Append(zeile.Value).Append("  ").Append(zeile.Key).Append('\n');

            File.WriteAllText(_hashdatei, text.ToString(), new UTF8Encoding(false));
            Console.WriteLine(_messlatte.Count + " Hashes geschrieben: " + _hashdatei);
        }

        // =================================================================================
        // Die SVG-Gegenprobe (Auftrag DG-E2)
        // =================================================================================

        /// <summary>Was eine SVG-Gegenprobe meldet — dieselben Spalten wie ein Bild.</summary>
        private sealed class SvgErgebnis
        {
            public string Masse = "-";
            public string Groesse = "-";
            public string Knoten = "-";
            public readonly List<string> Maengel = new List<string>();
        }

        /// <summary>
        /// Eine Gegenprobe auf dem SVG-Weg: <b>kein PNG, keine Ablage, keine
        /// Messlatte</b>. Sie zählt als geprüftes Bild, damit sie in der Zusammenfassung
        /// steht; die eingefrorene Hashliste bekommt keine Zeile (sie misst den
        /// PNG-Weg, und der ändert sich in dieser Etappe nicht um ein Byte).
        /// </summary>
        private static void SvgProbe(string name, Action<SvgErgebnis> pruefung)
        {
            _bilder++;
            var e = new SvgErgebnis();
            try { pruefung(e); }
            catch (Exception ex)
            { e.Maengel.Add("Ausnahme: " + ex.GetType().Name + " - " + ex.Message); }
            Melde(name, e.Masse, e.Groesse, e.Knoten, "-", e.Maengel);
        }

        /// <summary>Der Wert eines Attributs oder <c>null</c>.</summary>
        private static string Attributwert(SvgKnoten knoten, string name)
        {
            foreach (KeyValuePair<string, string> a in knoten.Attribute)
                if (a.Key == name) return a.Value;
            return null;
        }

        /// <summary>Die <c>viewBox</c> des inneren svg in Datenkoordinaten.</summary>
        private static string Flaechenbox(SvgKnoten baum)
        {
            foreach (SvgKnoten k in baum.Alle())
                if (k.Name == "svg" && Attributwert(k, "class") == "epos-flaeche")
                    return Attributwert(k, "viewBox");
            return null;
        }

        /// <summary>Die Strichfarbe des ersten Reihenpfads.</summary>
        private static string Reihenfarbe(SvgKnoten baum)
        {
            foreach (SvgKnoten k in baum.Alle())
                if (k.Name == "path" && Attributwert(k, "class") == "epos-reihe")
                    return Attributwert(k, "stroke");
            return null;
        }

        /// <summary>Die Anzahl der Textbefehle des Modells, auch die in Gruppen.</summary>
        private static int Textbefehle(IReadOnlyList<Zeichenbefehl> befehle)
        {
            int n = 0;
            if (befehle == null) return 0;
            foreach (Zeichenbefehl b in befehle)
            {
                if (b is WindowsFormsApplication1.Zeichnung.Text) n++;
                else if (b is Gruppe g) n += Textbefehle(g.Befehle);
            }
            return n;
        }

        /// <summary>Ein Modell der Gruppe (a) unter seinem Namen — nur der Bequemlichkeit.</summary>
        private static KeyValuePair<string, Func<Zeichenmodell>> Modellprobe(
            string name, Func<Zeichenmodell> modell)
            => new KeyValuePair<string, Func<Zeichenmodell>>(name, modell);

        /// <summary>
        /// <b>Die SVG-Gegenprobe EINES Modells der Gruppe (a)</b> (Auftrag DG-E3a).
        /// Geprüft wird, was der PNG-Vergleich nicht sieht:
        ///
        /// <list type="number">
        ///   <item>Zweimal geschrieben UND zweimal erzeugt ist byte-gleich.</item>
        ///   <item>Je <c>Datenreihe</c> genau EIN <c>path.epos-reihe</c> im inneren
        ///   <c>&lt;svg&gt;</c>, jeder mit <c>data-marke</c>, <c>vector-effect</c> und
        ///   Punkten.</item>
        ///   <item>Eine FLÄCHE trägt <c>fill</c> und einen geschlossenen Zug
        ///   (DG-E3-2); eine Linie trägt <c>fill="none"</c>.</item>
        ///   <item>Die viewBox des inneren svg steht senkrecht auf den BILDPUNKTEN der
        ///   Zeichenfläche (DG-E3-1) — nicht mehr auf der Wertespanne.</item>
        ///   <item>Führt das Modell eine zweite Achse, steht sie als <c>yachse2</c> im
        ///   Baum.</item>
        /// </list>
        /// </summary>
        private static void SvgModellprobe(string name, Func<Zeichenmodell> bau)
        {
            SvgProbe("svg_" + name, e =>
            {
                Zeichenmodell m = bau();
                string a = SvgSchreiber.Text(m);
                e.Masse = m.Breite + "x" + m.Hoehe;
                e.Groesse = Encoding.UTF8.GetByteCount(a)
                                    .ToString("N0", CultureInfo.InvariantCulture);

                if (!string.Equals(a, SvgSchreiber.Text(m), StringComparison.Ordinal))
                    e.Maengel.Add("zweimal geschrieben ist nicht byte-gleich");
                if (!string.Equals(a, SvgSchreiber.Text(bau()), StringComparison.Ordinal))
                    e.Maengel.Add("zweimal erzeugt ist nicht byte-gleich");

                SvgKnoten baum = SvgSchreiber.Baum(m);
                List<SvgKnoten> alle = baum.Alle().ToList();
                e.Knoten = alle.Count.ToString(CultureInfo.InvariantCulture);

                if (m.Flaeche == null) { e.Maengel.Add("das Modell fuehrt keine Zeichenflaeche"); return; }
                if (m.Reihen.Count == 0) { e.Maengel.Add("das Modell fuehrt keine Datenreihe"); return; }

                SvgKnoten flaeche = alle.FirstOrDefault(
                    k => k.Name == "svg" && Attributwert(k, "class") == "epos-flaeche");
                if (flaeche == null) { e.Maengel.Add("kein inneres svg"); return; }
                if (Attributwert(flaeche, "preserveAspectRatio") != "none")
                    e.Maengel.Add("inneres svg ohne preserveAspectRatio=none");

                // DG-E3-1: die viewBox-Hoehe sind die Bildpunkte der Zeichenflaeche.
                string[] box = (Attributwert(flaeche, "viewBox") ?? "").Split(' ');
                string hoehe = m.Flaeche.Bild.Hoehe.ToString("0.##", CultureInfo.InvariantCulture);
                if (box.Length != 4 || box[1] != "0" || box[3] != hoehe)
                    e.Maengel.Add("viewBox steht nicht auf den Bildpunkten (" + hoehe + "): " +
                                  Attributwert(flaeche, "viewBox"));

                List<SvgKnoten> pfade = alle.Where(
                    k => k.Name == "path" && Attributwert(k, "class") == "epos-reihe").ToList();
                if (pfade.Count != m.Reihen.Count)
                    e.Maengel.Add("Reihenpfade: " + pfade.Count + " statt " + m.Reihen.Count);

                for (int i = 0; i < pfade.Count && i < m.Reihen.Count; i++)
                {
                    SvgKnoten pf = pfade[i];
                    Datenreihe r = m.Reihen[i];
                    string d = Attributwert(pf, "d");

                    if (Attributwert(pf, "vector-effect") != "non-scaling-stroke")
                        e.Maengel.Add("Reihenpfad ohne vector-effect: " + r.Name);
                    string marke = Attributwert(pf, "data-marke");
                    if (marke == null || !marke.StartsWith("reihe:", StringComparison.Ordinal))
                        e.Maengel.Add("Reihenpfad ohne data-marke: " + r.Name);
                    if (string.IsNullOrEmpty(d))
                        e.Maengel.Add("Reihenpfad ohne Punkte: " + r.Name);

                    bool gefuellt = Attributwert(pf, "fill") != "none";
                    if (r.Art == Reihenart.Flaeche)
                    {
                        if (!gefuellt) e.Maengel.Add("Flaeche ohne fill: " + r.Name);
                        if (d == null || !d.EndsWith(" Z", StringComparison.Ordinal))
                            e.Maengel.Add("Flaeche ohne geschlossenen Zug: " + r.Name);
                    }
                    else if (gefuellt) e.Maengel.Add("Linie mit fill: " + r.Name);
                }

                bool y2ImModell = m.Befehle.Any(b => b.Marke == "yachse2");
                bool y2ImBaum = alle.Any(k => Attributwert(k, "data-marke") == "yachse2");
                if (y2ImModell != y2ImBaum)
                    e.Maengel.Add("die zweite Achse steht " +
                                  (y2ImModell ? "nicht im Baum" : "im Baum, aber nicht im Modell"));
            });
        }

        /// <summary>
        /// Schreibt ALLE Modelle der Gruppe (a) als <c>.svg</c> in einen Ordner
        /// (Schalter <c>--svg-alle &lt;ordner&gt;</c>) — zum Ansehen im Browser und für
        /// die Sichtprüfung gegen das Skia-Bild. Kein Teil der Prüfung: Es entsteht
        /// kein PNG und keine Hashzeile.
        /// </summary>
        private static void SvgOrdnerSchreiben(
            IReadOnlyList<KeyValuePair<string, Func<Zeichenmodell>>> bilder)
        {
            Directory.CreateDirectory(_svgordner);
            foreach (KeyValuePair<string, Func<Zeichenmodell>> b in bilder)
            {
                string text = SvgSchreiber.Text(b.Value());
                string pfad = Path.Combine(_svgordner, b.Key + ".svg");
                File.WriteAllText(pfad, text, new UTF8Encoding(false));
                Console.WriteLine("SVG geschrieben: " + pfad + " - " +
                    Encoding.UTF8.GetByteCount(text).ToString("N0", CultureInfo.InvariantCulture) +
                    " Byte");
            }
        }

        /// <summary>
        /// Schreibt den Jahresgang EINMAL als SVG-Datei (Schalter <c>--svg</c>) und
        /// nennt Größe und Knotenzahl.
        /// </summary>
        private static void SvgAblegen(Zeichenmodell modell)
        {
            string text = SvgSchreiber.Text(modell);
            string ordner = Path.GetDirectoryName(Path.GetFullPath(_svgdatei));
            if (!string.IsNullOrEmpty(ordner)) Directory.CreateDirectory(ordner);
            File.WriteAllText(_svgdatei, text, new UTF8Encoding(false));

            Console.WriteLine("SVG geschrieben: " + _svgdatei + " - " +
                Encoding.UTF8.GetByteCount(text).ToString("N0", CultureInfo.InvariantCulture) +
                " Byte, " +
                SvgSchreiber.Baum(modell).Alle().Count().ToString("N0", CultureInfo.InvariantCulture) +
                " Knoten");
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

        /// <summary>
        /// Der UNGEDECKTE REST der zwei Deckungsringe (#222) — seit dem
        /// Anwenderentscheid vom 11.09.2026 in BEIDEN Ringen dasselbe Grau. Derselbe
        /// Wert steht als <c>R_REST_GRAU</c> im Bildbauer der Ergebnishuelle und als
        /// Token <c>--epos-ring-rest</c> im Stilblatt.
        /// </summary>
        private static readonly SKColor RING_REST_GRAU = new SKColor(0xD9, 0xDE, 0xE5);

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

        /// <summary>
        /// Der Netto-Cashflow einer Speicherflotte ueber 20 Projektjahre (#184) —
        /// Anlaufjahr negativ, danach ein langsam wachsender Ueberschuss, im
        /// Ersatzjahr 10 ein Einbruch. Streng deterministisch wie alle Reihen hier.
        /// </summary>
        private static double[] Projektionsreihe()
        {
            var r = new double[20];
            for (int i = 0; i < r.Length; i++)
            {
                double jahr = i + 1;
                r[i] = -900.0 + 260.0 * jahr - 4.0 * jahr * jahr;
                if (jahr == 10) r[i] -= 4200.0;   // Ersatzinvestition
            }
            return r;
        }

        /// <summary>Die laufende Summe einer Reihe ab einem Startwert (Investition, negativ).</summary>
        private static double[] Kumuliert(double[] werte, double start)
        {
            var r = new double[werte.Length];
            double summe = start;
            for (int i = 0; i < werte.Length; i++) { summe += werte[i]; r[i] = summe; }
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

        /// <summary>
        /// Das Optimum des synthetischen Rasters: Kapazitaetszeile 4 (2 500 kWh) und
        /// C-Raten-Spalte 2 (1,5 C) - der Scheitel der Flaeche in <see cref="Rasterfeld"/>.
        /// </summary>
        private const int RASTER_BESTE_ZEILE = 4;

        /// <summary>Siehe <see cref="RASTER_BESTE_ZEILE"/>.</summary>
        private const int RASTER_BESTE_SPALTE = 2;

        /// <summary>
        /// Ein synthetisches Optimierungsraster: eine nach unten geoeffnete Flaeche mit
        /// dem Scheitel bei 2 500 kWh und 1,5 C. Die Raender laufen ins Negative - ein
        /// zu grosser Speicher traegt seinen Kapitaldienst nicht mehr, und die
        /// Dreifarbskala soll genau das zeigen.
        /// </summary>
        private static double[][] Rasterfeld(double[] kapazitaeten, double[] cRaten)
        {
            var feld = new double[kapazitaeten.Length][];
            for (int i = 0; i < kapazitaeten.Length; i++)
            {
                feld[i] = new double[cRaten.Length];
                for (int s = 0; s < cRaten.Length; s++)
                {
                    feld[i][s] = Flaeche(kapazitaeten[i], cRaten[s]);
                }
            }
            return feld;
        }

        /// <summary>
        /// Die synthetische Flaeche, aus der beide Karten ihre Werte nehmen: ein Scheitel
        /// bei 2 500 kWh und 1,5 C, nach beiden Seiten abfallend und ins Negative laufend.
        /// </summary>
        private static double Flaeche(double kapazitaetKwh, double cRate)
            => 4000.0
             - 0.0009 * (kapazitaetKwh - 2500.0) * (kapazitaetKwh - 2500.0)
             - 900.0 * (cRate - 1.5) * (cRate - 1.5);

        /// <summary>Eine Spalte des Rasters - die Schnittkurve bei fester C-Rate.</summary>
        private static double[] Rasterspalte(double[][] feld, int spalte)
        {
            var w = new double[feld.Length];
            for (int i = 0; i < feld.Length; i++) w[i] = feld[i][spalte];
            return w;
        }

        /// <summary>
        /// Die Sperrmatrix der Groessen-Sicht (#193): die zwei GROESSTEN Kapazitaeten
        /// und die schnellste C-Rate sind unzulaessig. Damit liegt Schraffur an einer
        /// Kante und quer durch die Flaeche, und das Optimum (Zeile 4, Spalte 2) bleibt
        /// frei - sonst pruefte das Bild die Marke ueber der Schraffur statt beides.
        /// </summary>
        private static bool[][] Rastersperre(int zeilen, int spalten)
        {
            var sperre = new bool[zeilen][];
            for (int i = 0; i < zeilen; i++)
            {
                sperre[i] = new bool[spalten];
                for (int s = 0; s < spalten; s++)
                    sperre[i][s] = i >= zeilen - 2 || s == spalten - 1;
            }
            return sperre;
        }

        /// <summary>
        /// Die Leistungsachse einer Rasterzeile: P = E * C bei fester Kapazitaet - die
        /// zweite Lesart derselben Kurve (Konzept 2.5).
        /// </summary>
        /// <summary>
        /// Die Achse des Feinraster-Schnitts (Auftrag #224): neun GROBE Stuetzstellen
        /// 500…4 500 kWh in Schritten von 500 und danach acht FEINE im Fenster
        /// [4 500, 5 000] in Schritten von 500/9 — die Regel „Schrittweite / 9" der
        /// Mappe V7 um ein Grob-Optimum am oberen Rand.
        /// </summary>
        private static double[] Feinachse()
        {
            var werte = new List<double>();
            for (int i = 0; i < 9; i++) werte.Add(500.0 + 500.0 * i);
            for (int i = 1; i <= 9; i++) werte.Add(4500.0 + 500.0 * i / 9.0);
            return werte.Distinct().OrderBy(x => x).ToArray();
        }

        /// <summary>Eine flach auslaufende Kurve ueber dieser Achse — das Optimum liegt rechts.</summary>
        private static double[] Feinwerte(double[] achse)
        {
            var werte = new double[achse.Length];
            for (int i = 0; i < achse.Length; i++)
                werte[i] = 70000.0 * (1.0 - Math.Exp(-achse[i] / 2200.0)) - 12000.0;
            return werte;
        }

        /// <summary>
        /// Je Stuetzstelle: Sie stammt aus der ZWEITEN Phase. Grob sind genau die
        /// Vielfachen von 500 kWh.
        /// </summary>
        private static bool[] Feinmarken(double[] achse)
        {
            var marken = new bool[achse.Length];
            for (int i = 0; i < achse.Length; i++)
                marken[i] = Math.Abs(achse[i] / 500.0 - Math.Round(achse[i] / 500.0)) > 1e-9;
            return marken;
        }

        private static double[] Leistungsachse(double[] cRaten, double kapazitaetKwh)
        {
            var w = new double[cRaten.Length];
            for (int s = 0; s < cRaten.Length; s++) w[s] = cRaten[s] * kapazitaetKwh;
            return w;
        }

        /// <summary>Die Spalte des Optimums in der GERAETEkarte: 1 250 kW.</summary>
        private const int GERAETE_BESTE_SPALTE = 4;

        /// <summary>
        /// ZEHN Geraete, wie sie ein Katalog fuehrt: krumme Paare aus Kapazitaet und
        /// Entladeleistung, kein Gitter. Das Optimum liegt auf (2 500 kWh, 1 250 kW).
        /// </summary>
        private static (double E, double P)[] Geraeteliste() => new[]
        {
            (500.0, 250.0), (1000.0, 500.0), (1000.0, 1000.0), (1500.0, 750.0),
            (2000.0, 1000.0), (2500.0, 1250.0), (2500.0, 2500.0), (3000.0, 1500.0),
            (4000.0, 2000.0), (5000.0, 2500.0)
        };

        /// <summary>Die verschiedenen Entladeleistungen der Geraete, aufsteigend — die Spaltenachse.</summary>
        private static double[] Geraeteleistungen((double E, double P)[] geraete)
            => geraete.Select(g => g.P).Distinct().OrderBy(x => x).ToArray();

        /// <summary>
        /// Die DUENN besetzte Matrix: Nur eine Zelle, hinter der ein Geraet steht, traegt
        /// einen Wert; jede andere bleibt <c>NaN</c> und damit leer.
        /// </summary>
        private static double[][] Geraetefeld(double[] kapazitaeten, double[] leistungen,
                                              (double E, double P)[] geraete)
        {
            var feld = new double[kapazitaeten.Length][];
            for (int z = 0; z < kapazitaeten.Length; z++)
            {
                feld[z] = new double[leistungen.Length];
                for (int s = 0; s < leistungen.Length; s++) feld[z][s] = double.NaN;
            }
            foreach ((double E, double P) g in geraete)
            {
                int z = Array.IndexOf(kapazitaeten, g.E);
                int s = Array.IndexOf(leistungen, g.P);
                if (z >= 0 && s >= 0) feld[z][s] = Flaeche(g.E, g.P / g.E);
            }
            return feld;
        }

        /// <summary>Das Optimum des Kapazitaet-x-Leistung-Feldes: 220 kWh (Zeile 5).</summary>
        private const int LEISTUNGSFELD_BESTE_ZEILE = 5;

        /// <summary>Siehe <see cref="LEISTUNGSFELD_BESTE_ZEILE"/>: 140 kW (Spalte 3).</summary>
        private const int LEISTUNGSFELD_BESTE_SPALTE = 3;

        /// <summary>
        /// Das synthetische Feld der GROESSENKOPPLUNG "Kapazitaet und Leistung" (#226):
        /// eine nach unten geoeffnete Flaeche mit dem Scheitel bei 220 kWh und 140 kW.
        /// Das Minimum liegt an der Ecke [12][12] - dort bleibt es auch, wenn die
        /// Gegenprobe die Zelle [0][0] veraendert.
        /// </summary>
        private static double[][] Leistungsfeld(double[] kapazitaeten, double[] leistungen)
        {
            var feld = new double[kapazitaeten.Length][];
            for (int i = 0; i < kapazitaeten.Length; i++)
            {
                feld[i] = new double[leistungen.Length];
                for (int s = 0; s < leistungen.Length; s++)
                {
                    double c = kapazitaeten[i], p = leistungen[s];
                    feld[i][s] = 5000.0
                               - (c - 220.0) * (c - 220.0) / 100.0
                               - (p - 140.0) * (p - 140.0) / 50.0;
                }
            }
            return feld;
        }

        /// <summary>
        /// Das synthetische Feld der STUECKZAHLKARTE (#247): eine nach unten geoeffnete
        /// Flaeche mit dem Scheitel bei 2 Stueck der ersten und 1 Stueck der zweiten
        /// Einheit - so steht das Optimum in der Mitte und die Raender fallen ab.
        /// </summary>
        private static double[][] Stueckfeld(double[] erste, double[] zweite)
        {
            var feld = new double[erste.Length][];
            for (int i = 0; i < erste.Length; i++)
            {
                feld[i] = new double[zweite.Length];
                for (int s = 0; s < zweite.Length; s++)
                    feld[i][s] = 42000.0
                               - (erste[i] - 2.0) * (erste[i] - 2.0) * 9000.0
                               - (zweite[s] - 1.0) * (zweite[s] - 1.0) * 6000.0;
            }
            return feld;
        }

        /// <summary>Eine KOPIE des Feldes mit genau EINER veraenderten Stelle.</summary>
        private static double[][] MitStelle(double[][] feld, int zeile, int spalte, double wert)
        {
            var kopie = feld.Select(z => (double[])z.Clone()).ToArray();
            kopie[zeile][spalte] = wert;
            return kopie;
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
            Ablegen(name + ".png", a);

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
            Unterschiedlich(name, ganz, teil, true);
        }

        /// <summary>
        /// Dieselbe Gegenprobe, wahlweise OHNE Ablage und Messlatte
        /// (<paramref name="messlatte"/> = <c>false</c>).
        ///
        /// <para><b>Wofür.</b> Die Probe „Palette abweichend" zeichnet dasselbe Bild
        /// mit einer GEÄNDERTEN Farbpalette — ihr zweites Bild hängt damit an einer
        /// Anwendereinstellung und gehört nicht in die eingefrorene Messlatte. Die
        /// Messlatte bleibt die Aussage über die VORGABE-Palette; die Gegenprobe sagt
        /// nur, dass ein getauschtes Rot überhaupt ankommt.</para>
        /// </summary>
        private static void Unterschiedlich(string name, Func<byte[]> ganz, Func<byte[]> teil,
                                            bool messlatte)
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

            // Beide Seiten der Gegenprobe in die Ablage und die Messlatte: "_a" ist das
            // erste, "_b" das zweite Bild des Vergleichs.
            if (messlatte)
            {
                Ablegen(name + "_a.png", a);
                Ablegen(name + "_b.png", b);
            }

            Melde(name + " (wirkt)", "-", b == null ? "-" : b.Length.ToString("N0", CultureInfo.InvariantCulture),
                  "-", "-", maengel);
        }

        /// <summary>
        /// Das Bild des Erzeugerstapels mit NEUN Legendeneintraegen (#240) — der
        /// Wortlaut der Namen ist der einzige Unterschied zwischen Probe und
        /// Gegenprobe, die Reihen selbst sind dieselben.
        /// </summary>
        private static byte[] NeunReihenBild(string[] namen)
        {
            var farben = new[]
            {
                SKColors.Orange, SKColors.Yellow, SKColors.Blue, SKColors.Brown,
                SKColors.Red, SKColors.SeaGreen, SKColors.DarkOrchid,
                SKColors.SteelBlue, SKColors.Sienna
            };

            var stapel = new List<ChartRenderer.Reihe>();
            for (int i = 0; i < farben.Length; i++)
                stapel.Add(new ChartRenderer.Reihe(
                    namen[i],
                    Jahresreihe(8 + 2 * i, 6 + i, 3, 0.2 * i, Math.PI / 2),
                    farben[i], ChartRenderer.Stapelart.Saeule));

            return ChartRenderer.ErzeugerStapel("Waermeproduktion Jahresganglinie",
                        stapel, new List<ChartRenderer.Reihe>(), null,
                        "Waermelast [kW]", ChartRenderer.Achse.Monate, false);
        }

        /// <summary>
        /// <b>Die Zeichenflaeche macht der Legende Platz</b> (Auftrag #240): Bricht sie
        /// in eine zweite Zeile um, beginnt die Flaeche genau eine Legendenzeile tiefer.
        ///
        /// <para>Gemessen wird an den BILDPUNKTEN: Die oberste waagerechte Rasterlinie
        /// (Gainsboro, ueber die volle Breite) liegt auf der Oberkante der
        /// Zeichenflaeche. Ohne den Versatz staende sie in beiden Bildern gleich hoch —
        /// und die zweite Legendenzeile laege im Bereich des y-Achsentitels, der 24 px
        /// darueber steht. Mass-, Farb- und Determinismuspruefung bemerken das nicht:
        /// Das Bild ist deterministisch falsch.</para>
        ///
        /// <para>Geprueft wird ein VIELFACHES der Legendenzeile, keine feste Zahl: Wie
        /// viele Zeilen neun lange Namen belegen, haengt an der Schriftbreite (heute
        /// drei, also 60 px); dass es UEBERHAUPT eine ganze Zeilenhoehe ist und nicht
        /// null, ist die Aussage.</para>
        /// </summary>
        private static void Zeichenflaechenversatz(string name, Func<byte[]> wenige,
                                                   Func<byte[]> viele, int zeilenhoehe)
        {
            _bilder++;
            var maengel = new List<string>();
            int oben = -1, unten = -1;

            try
            {
                byte[] a = wenige(), b = viele();
                oben = ObersteRasterlinie(a);
                unten = ObersteRasterlinie(b);
                Ablegen(name + "_wenige.png", a);
                Ablegen(name + "_viele.png", b);
            }
            catch (Exception ex)
            {
                Melde(name + " (Versatz)", "-", "-", "-", "-",
                      new List<string> { "Ausnahme: " + ex.GetType().Name + " - " + ex.Message });
                return;
            }

            int versatz = oben < 0 || unten < 0 ? -1 : unten - oben;

            if (versatz < 0)
                maengel.Add("keine Rasterlinie gefunden (" + oben + " / " + unten + ")");
            else if (versatz == 0)
                maengel.Add("die Zeichenflaeche beginnt in beiden Bildern bei " + oben +
                            " px - die zweite Legendenzeile liegt also weiter auf dem " +
                            "y-Achsentitel (#240)");
            else if (versatz % zeilenhoehe != 0)
                maengel.Add("Versatz " + versatz + " px ist kein Vielfaches der " +
                            "Legendenzeile (" + zeilenhoehe + " px) - Rasterlinie bei " +
                            oben + " bzw. " + unten);

            Melde(name + " (Versatz " + versatz + " px)", "-", "-", "-", "-", maengel);
        }

        /// <summary>
        /// Die Bildzeile der obersten waagerechten Rasterlinie — sie ist die Oberkante
        /// der Zeichenflaeche. Gesucht wird in einem Streifen weit rechts vom
        /// Achsentitel, in dem sonst nichts Graues steht; die Linie ist Gainsboro
        /// (220) und kann durch Kantenglaettung bis gegen Weiss aufgehellt sein.
        /// </summary>
        private static int ObersteRasterlinie(byte[] png)
        {
            using (SKBitmap bild = SKBitmap.Decode(png))
            {
                if (bild == null) return -1;
                for (int y = 0; y < bild.Height; y++)
                {
                    int treffer = 0;
                    for (int x = 700; x < 1100 && x < bild.Width; x++)
                    {
                        SKColor c = bild.GetPixel(x, y);
                        if (c.Red == c.Green && c.Green == c.Blue &&
                            c.Red >= 200 && c.Red <= 245) treffer++;
                    }
                    if (treffer >= 300) return y;
                }
            }
            return -1;
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

        /// <summary>Die Farbe einer Rolle in der Vorgabe-Palette — die Erwartung einer Maßprobe.</summary>
        private static SKColor Rollenfarbe(Farbrolle rolle)
            => Farbpalette.Vorgabe.Loese(Farbton.Aus(rolle)).Skiafarbe();

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
        /// <summary>
        /// Ein Jahr Waermebedarf mit Winterspitze und ein Solarertrag mit Sommerspitze —
        /// fest verdrahtet, ohne Zufall. Ein Viertel der Solardeckung laeuft ueber den
        /// Speicher.
        /// </summary>
        private static SolarWaermeMonate WaermeAutarkieSatz()
        {
            var bedarf = new double[8760];
            var direkt = new double[8760];
            var speicher = new double[8760];
            for (int h = 0; h < 8760; h++)
            {
                double jahr = Math.Cos(2.0 * Math.PI * h / 8760.0);          // 1 im Januar
                double tag = Math.Max(0.0, Math.Sin(Math.PI * ((h % 24) - 6) / 12.0));
                bedarf[h] = Math.Round(8.0 + 6.0 * jahr, 6);
                double solar = Math.Min(bedarf[h], (4.0 - 2.5 * jahr) * tag);
                direkt[h] = Math.Round(0.75 * solar, 6);
                speicher[h] = Math.Round(0.25 * solar, 6);
            }
            return SolarWaermeMonate.Aggregieren(bedarf, direkt, speicher);
        }

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
        /// <summary>
        /// AUFTRAG U18 - die Reihen des Absolutbildes mit GESTRICHELTER Stammlinie.
        /// Die Stammlinie ist die Bezugsgroesse und keine Version; im
        /// Schwarz-Weiss-Ausdruck ist sie nur ueber die Strichart von ihr zu trennen.
        /// </summary>
        private static List<ChartRenderer.Reihe> VerlaufMitGestricheltemStamm(
            List<VerlaufSerie> serien)
        {
            return ChartRenderer.VerlaufsReihen(serien, true, true);
        }

        /// <summary>
        /// AUFTRAG U18 - dieselben Reihen unter anderen Namen. Der Wortlaut ist der
        /// einzige Unterschied zwischen Probe und Gegenprobe; die Werte, Farben und
        /// Masse bleiben gleich, damit allein die Legende das Bild veraendert.
        /// </summary>
        private static List<ChartRenderer.Reihe> VerlaufMitNamen(
            List<VerlaufSerie> serien, params string[] namen)
        {
            List<ChartRenderer.Reihe> reihen = ChartRenderer.VerlaufsReihen(serien, true);
            for (int i = 0; i < reihen.Count && i < namen.Length; i++)
                reihen[i].Name = namen[i];
            return reihen;
        }

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
