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

            Console.WriteLine(new string('-', 92));
            Console.WriteLine(_bilder + " Bilder geprueft, " + _verstoesse + " Verstoesse.");
            if (_verstoesse == 0) Console.WriteLine("ERGEBNIS: alle gruen.");
            else Console.WriteLine("ERGEBNIS: FEHLGESCHLAGEN.");
            return _verstoesse == 0 ? 0 : 1;
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
