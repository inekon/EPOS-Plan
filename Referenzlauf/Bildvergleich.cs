using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using SkiaSharp;

namespace WindowsFormsApplication1.Referenzlauf
{
    /// <summary>
    /// Modus "bildvergleich" (Umsetzungskonzept iOS, Paket iU7-1) — die WINDOWS-ABNAHME
    /// der Renderer-Portierung.
    ///
    /// <para>Er rechnet dieselben Projekte wie der Modus "lauf", holt sich aus dem
    /// frischen Lauf den <see cref="ZeitreihenSatz"/> (genau so, wie es der
    /// BerichtsDatenSammler tut) und rendert daraus JEDEN Bildtyp des Berichts ZWEIMAL:
    /// einmal mit dem eingefrorenen GDI+-Stand <see cref="ChartRendererGdi"/> und einmal
    /// mit dem portierten <see cref="ChartRenderer"/>. Beide PNG werden abgelegt und
    /// Pixel fuer Pixel gegeneinander gehalten.</para>
    ///
    /// <para><b>Warum das nur unter Windows laeuft.</b> Die GDI+-Seite braucht
    /// System.Drawing mit einer echten Windows-Grafikbibliothek; auf Linux gibt es sie
    /// nicht. Der Modus ist deshalb ein reines Abnahmewerkzeug fuer den Entwicklerrechner
    /// und gehoert nicht in die CI. Der Vergleich SELBST laeuft ueber SkiaSharp
    /// (<see cref="SKBitmap.Decode(byte[])"/>) und nicht ueber System.Drawing — sonst
    /// haette die Messung dieselbe Bibliothek benutzt, die sie beurteilen soll.</para>
    ///
    /// <para><b>Was gemessen wird.</b> Erstens die BILDMASSE: Sie muessen gleich sein,
    /// denn der Word- und der Excel-Bericht betten die PNG mit fest verdrahteten
    /// Zielgroessen ein — eine andere Pixelzahl waere ein anderer Bildausschnitt.
    /// Zweitens der Anteil abweichender Pixel bei einer Toleranz von
    /// <see cref="KANALTOLERANZ"/> je Kanal: Kantenglaettung und Schriftrasterung
    /// unterscheiden sich zwischen GDI+ und Skia zwangslaeufig, die FLAECHEN duerfen es
    /// nicht. Drittens ein Farbhistogramm ueber die Palette des Berichts — es zeigt, ob
    /// jede Serie noch in ihrer Farbe und in etwa gleichem Umfang im Bild steht.</para>
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static class Bildvergleich
    {
        /// <summary>Zulaessiger Unterschied je Farbkanal, bis zu dem ein Pixel als gleich gilt.</summary>
        private const int KANALTOLERANZ = 24;

        /// <summary>Ab hier ist ein Bildpaar zu PRUEFEN statt PASS.</summary>
        private const double GRENZE_PROZENT = 3.0;

        /// <summary>Toleranz des Farbhistogramms — Flaechen sind flach gefuellt, die
        /// Randpixel der Kantenglaettung sollen NICHT mitgezaehlt werden.</summary>
        private const int HISTOGRAMM_TOLERANZ = 4;

        private const string ORDNER_ARBEITSKOPIE = "Arbeitskopie";

        // =================================================================================
        // Einstieg
        // =================================================================================

        /// <summary>
        /// bildvergleich --quelle &lt;sqlite&gt; --projekte 1030,1007,1017 --ziel &lt;ordner&gt;
        /// </summary>
        public static int Ausfuehren(string[] args)
        {
            var log = new Protokoll();

            string ziel = Argument(args, "--ziel");
            if (string.IsNullOrWhiteSpace(ziel))
            {
                log.FehlerZeile("--ziel <ordner> fehlt.");
                return 2;
            }
            ziel = Path.GetFullPath(ziel);
            Directory.CreateDirectory(ziel);

            string vorgabe = Argument(args, "--projekte");
            if (string.IsNullOrWhiteSpace(vorgabe))
            {
                log.FehlerZeile("--projekte <id,id,...> fehlt.");
                return 2;
            }
            List<int> ids;
            try
            {
                ids = vorgabe.Split(',')
                             .Select(s => int.Parse(s.Trim(), CultureInfo.InvariantCulture))
                             .ToList();
            }
            catch (FormatException)
            {
                log.FehlerZeile("--projekte erwartet Zahlen, getrennt durch Komma.");
                return 2;
            }

            log.Zeile("Bildvergleich GDI+ <-> SkiaSharp (Paket iU7-1).");
            log.Zeile("Zielordner: " + ziel);
            log.Leerzeile();

            // --- Arbeitskopie -------------------------------------------------------------
            // Sie liegt BEWUSST unter dem Zielordner und nicht unter Referenzlaeufe\: Der
            // Bildvergleich soll die Arbeitskopie eines nebenher laufenden "lauf" nicht
            // ueberschreiben.
            string quelle = DbUmgebung.ProduktivQuelleFinden(log, Argument(args, "--quelle"));
            if (quelle == null)
            {
                log.FehlerZeile("Keine Datenbank gefunden - Abbruch.");
                return 2;
            }
            string arbeitskopie = Path.Combine(ziel, ORDNER_ARBEITSKOPIE);
            DbUmgebung.ArbeitskopieAnlegen(quelle, arbeitskopie, log);
            DbUmgebung.AufArbeitskopieUmschaltenUndPruefen(arbeitskopie, log);
            log.Leerzeile();

            var zeilen = new List<string>();
            bool allesGruen = true;

            using (new DialogWaechter())
            {
                MigrationAusfuehren(log);
                log.Leerzeile();

                foreach (int id in ids)
                {
                    log.Zeile("--- Projekt " + id + " ---");
                    string projektOrdner = Path.Combine(ziel,
                        "Projekt_" + id.ToString(CultureInfo.InvariantCulture));
                    Directory.CreateDirectory(projektOrdner);

                    zeilen.Add("");
                    zeilen.Add("### Projekt " + id);
                    zeilen.Add("");

                    ZeitreihenSatz z = ZeitreihenAusFrischemLauf(id, log);
                    if (z == null)
                    {
                        allesGruen = false;
                        zeilen.Add("Kein Lauf - keine Bilder.");
                        continue;
                    }

                    List<Befund> befunde = ProjektVergleichen(z, projektOrdner, log);
                    if (befunde.Any(b => !b.Pass)) allesGruen = false;

                    zeilen.AddRange(Tabelle(befunde));
                    zeilen.Add("");
                    zeilen.AddRange(Histogrammbloecke(befunde));
                }
            }

            SchreibeBericht(Path.Combine(ziel, "bildvergleich.md"), quelle, ids, zeilen, allesGruen);
            log.Leerzeile();
            log.Zeile("Bericht geschrieben: " + Path.Combine(ziel, "bildvergleich.md"));
            log.Zeile(allesGruen ? "Gesamtergebnis: PASS" : "Gesamtergebnis: PRUEFEN");
            return allesGruen ? 0 : 1;
        }

        /// <summary>Bringt die Arbeitskopie auf den Zielstand — wie im Modus "lauf".</summary>
        private static void MigrationAusfuehren(Protokoll log)
        {
            log.Zeile("Schema-Migration der Arbeitskopie ...");
            try
            {
                string bericht;
                bool ok = SchemaMigration.Ausfuehren(out bericht);
                foreach (string z in (bericht ?? "").Replace("\r\n", "\n").Split('\n'))
                    if (z.Trim().Length > 0) log.Roh("  " + z);

                if (ok) log.Zeile("Migration: ERFOLG (Zielstand " + SchemaMigration.ZIEL_VERSION + ").");
                else log.Warnung("Migration FEHLGESCHLAGEN - der Lauf rechnet auf einem unvollstaendigen Schema.");
            }
            catch (Exception ex)
            {
                log.Warnung("Migration nicht ausfuehrbar: " + ex.Message);
            }
        }

        /// <summary>
        /// Rechnet EIN Projekt frisch und holt den Zeitreihensatz ab — derselbe Weg, den
        /// der BerichtsDatenSammler geht (SimuliereUndSpeichere, danach
        /// ZeitreihenExtraktor.AusLauf auf DERSELBEN Runner-Instanz). Die Bausteine des
        /// Berichts werden dafuer bewusst nicht angefasst.
        /// </summary>
        private static ZeitreihenSatz ZeitreihenAusFrischemLauf(int idProjekt, Protokoll log)
        {
            var runner = new SimulationRunner();
            string fehler;
            log.Zeile("Simulation startet fuer Projekt " + idProjekt + " ...");
            int kopf = runner.SimuliereUndSpeichere(idProjekt, out fehler);
            if (!runner.LaufOk)
            {
                log.FehlerZeile("Projekt " + idProjekt + ": " + (fehler ?? "unbekannter Fehler"));
                return null;
            }
            log.Zeile("Simulation beendet, Ergebnis-Kopf-ID " + kopf + ".");

            try { return ZeitreihenExtraktor.AusLauf(runner); }
            catch (Exception ex)
            {
                log.FehlerZeile("Zeitreihen nicht abholbar: " + ex.Message);
                return null;
            }
        }

        // =================================================================================
        // Die neun Bilder
        // =================================================================================

        private sealed class Bildpaar
        {
            public string Typ;
            public byte[] Gdi;
            public byte[] Skia;
        }

        /// <summary>
        /// Erzeugt jedes Bild zweimal, legt beide PNG ab und misst das Paar.
        ///
        /// <para>Die acht oeffentlichen Bilderzeuger ergeben NEUN Bilder: der
        /// Kapitalwert-Verlauf laeuft — wie im Bericht und im Verlaufsdialog — einmal als
        /// Differenzbild (ohne Stammlinie) und einmal als Absolutbild (mit Stammlinie),
        /// und das sind zwei verschiedene Zeichenwege.</para>
        /// </summary>
        private static List<Befund> ProjektVergleichen(ZeitreihenSatz z, string ordner, Protokoll log)
        {
            var paare = new List<Bildpaar>();

            // 1. Kuchen — fester Beispieldatensatz aus den Ergebnissen: die
            //    Jahressummen der Waermeerzeuger als Deckungsanteile.
            paare.Add(new Bildpaar
            {
                Typ = "kuchen_waermedeckung",
                Gdi = Sicher(() => ChartRendererGdi.Kuchen("Waermedeckung", KuchenGdi(z))),
                Skia = Sicher(() => ChartRenderer.Kuchen("Waermedeckung", KuchenSkia(z)))
            });

            // 2. Balken — derselbe Datensatz als Jahressummen in MWh, groesster Balken
            //    hervorgehoben (im Bericht ist das der Stamm).
            paare.Add(new Bildpaar
            {
                Typ = "balken_erzeugung",
                Gdi = Sicher(() => ChartRendererGdi.BalkenHorizontal("Waermeerzeugung", "MWh/a", BalkenGdi(z))),
                Skia = Sicher(() => ChartRenderer.BalkenHorizontal("Waermeerzeugung", "MWh/a", BalkenSkia(z)))
            });

            // 3.-7. Die fuenf Ganglinienbilder direkt aus dem Zeitreihensatz.
            paare.Add(new Bildpaar
            {
                Typ = "jahresverlauf_waerme",
                Gdi = Sicher(() => ChartRendererGdi.JahresverlaufWaerme(z)),
                Skia = Sicher(() => ChartRenderer.JahresverlaufWaerme(z))
            });
            paare.Add(new Bildpaar
            {
                Typ = "dauerlinie_waerme",
                Gdi = Sicher(() => ChartRendererGdi.DauerlinieWaerme(z)),
                Skia = Sicher(() => ChartRenderer.DauerlinieWaerme(z))
            });
            paare.Add(new Bildpaar
            {
                Typ = "strombilanz_monate",
                Gdi = Sicher(() => ChartRendererGdi.StrombilanzMonate(z)),
                Skia = Sicher(() => ChartRenderer.StrombilanzMonate(z))
            });
            paare.Add(new Bildpaar
            {
                Typ = "speicherverlauf",
                Gdi = Sicher(() => ChartRendererGdi.Speicherverlauf(z)),
                Skia = Sicher(() => ChartRenderer.Speicherverlauf(z))
            });
            paare.Add(new Bildpaar
            {
                Typ = "speichertemperaturen",
                Gdi = Sicher(() => ChartRendererGdi.Speichertemperaturen(z)),
                Skia = Sicher(() => ChartRenderer.Speichertemperaturen(z))
            });

            // 8./9. Kapitalwert-Verlauf aus einem FESTEN Beispiel — er haengt nicht am
            //       Zeitreihensatz, sondern an der Wirtschaftlichkeitsrechnung. Feste
            //       Zahlen halten das Bild ueber alle Projekte hinweg vergleichbar.
            List<VerlaufSerie> serien = Beispielserien();
            paare.Add(new Bildpaar
            {
                Typ = "kapitalwert_differenz",
                Gdi = Sicher(() => ChartRendererGdi.KapitalwertVerlauf(
                        "Differenz zur Stamm-Referenz",
                        ChartRendererGdi.VerlaufsReihen(serien, false), "Beispieldaten iU7")),
                Skia = Sicher(() => ChartRenderer.KapitalwertVerlauf(
                        "Differenz zur Stamm-Referenz",
                        ChartRenderer.VerlaufsReihen(serien, false), "Beispieldaten iU7"))
            });
            paare.Add(new Bildpaar
            {
                Typ = "kapitalwert_absolut",
                Gdi = Sicher(() => ChartRendererGdi.KapitalwertVerlauf(
                        "Kumulierte Barwerte je Projekt",
                        ChartRendererGdi.VerlaufsReihen(serien, true), null)),
                Skia = Sicher(() => ChartRenderer.KapitalwertVerlauf(
                        "Kumulierte Barwerte je Projekt",
                        ChartRenderer.VerlaufsReihen(serien, true), null))
            });

            var befunde = new List<Befund>();
            foreach (Bildpaar p in paare)
            {
                if (p.Gdi != null) File.WriteAllBytes(Path.Combine(ordner, p.Typ + "_gdi.png"), p.Gdi);
                if (p.Skia != null) File.WriteAllBytes(Path.Combine(ordner, p.Typ + "_skia.png"), p.Skia);

                Befund b = Vergleiche(p);
                befunde.Add(b);
                log.Roh("  " + (b.Pass ? "PASS   " : "PRUEFEN") + " " + p.Typ + " - " + b.Kurz);
            }
            return befunde;
        }

        private static byte[] Sicher(Func<byte[]> f)
        {
            try { return f(); } catch { return null; }
        }

        // --- die beiden Beispieldatensaetze, je einmal je Renderer -------------------------
        //
        // Sie sind ZWEIMAL da, weil Segment/Balken geschachtelte Typen der jeweiligen
        // Renderer-Klasse sind und ihre Farbe im Alt-Renderer System.Drawing.Color, im
        // neuen SKColor ist. Die ZAHLEN kommen beide Male aus derselben Quelle.

        private static readonly string[] ERZEUGER_SCHLUESSEL =
        {
            ZeitreihenSatz.SOLAR_WAERME, ZeitreihenSatz.WP_WAERME,
            ZeitreihenSatz.BHKW_WAERME, ZeitreihenSatz.KESSEL_WAERME
        };

        private static readonly string[] ERZEUGER_NAMEN =
        { "Solarthermie", "Waermepumpe", "BHKW", "Spitzenkessel" };

        /// <summary>Jahressumme einer Reihe in MWh; 0, wenn die Reihe fehlt.</summary>
        private static double JahresSummeMWh(ZeitreihenSatz z, string schluessel)
        {
            double[] w = z.Hole(schluessel);
            if (w == null) return 0;
            double s = 0;
            for (int i = 0; i < w.Length; i++) s += w[i];
            return s / 1000.0;
        }

        private static List<ChartRendererGdi.Segment> KuchenGdi(ZeitreihenSatz z)
        {
            var farben = new[] { ChartRendererGdi.C_SOLAR, ChartRendererGdi.C_WP,
                                 ChartRendererGdi.C_BHKW, ChartRendererGdi.C_KESSEL };
            var l = new List<ChartRendererGdi.Segment>();
            for (int i = 0; i < ERZEUGER_SCHLUESSEL.Length; i++)
            {
                double v = JahresSummeMWh(z, ERZEUGER_SCHLUESSEL[i]);
                if (v > 0) l.Add(new ChartRendererGdi.Segment(ERZEUGER_NAMEN[i], v, farben[i]));
            }
            if (l.Count == 0) l.Add(new ChartRendererGdi.Segment("Rest/ungedeckt", 1, ChartRendererGdi.C_REST));
            return l;
        }

        private static List<ChartRenderer.Segment> KuchenSkia(ZeitreihenSatz z)
        {
            var farben = new[] { ChartRenderer.C_SOLAR, ChartRenderer.C_WP,
                                 ChartRenderer.C_BHKW, ChartRenderer.C_KESSEL };
            var l = new List<ChartRenderer.Segment>();
            for (int i = 0; i < ERZEUGER_SCHLUESSEL.Length; i++)
            {
                double v = JahresSummeMWh(z, ERZEUGER_SCHLUESSEL[i]);
                if (v > 0) l.Add(new ChartRenderer.Segment(ERZEUGER_NAMEN[i], v, farben[i]));
            }
            if (l.Count == 0) l.Add(new ChartRenderer.Segment("Rest/ungedeckt", 1, ChartRenderer.C_REST));
            return l;
        }

        private static List<ChartRendererGdi.Balken> BalkenGdi(ZeitreihenSatz z)
        {
            var l = new List<ChartRendererGdi.Balken>();
            double max = ErzeugerMax(z);
            for (int i = 0; i < ERZEUGER_SCHLUESSEL.Length; i++)
            {
                double v = JahresSummeMWh(z, ERZEUGER_SCHLUESSEL[i]);
                if (v > 0) l.Add(new ChartRendererGdi.Balken(ERZEUGER_NAMEN[i], v, v >= max));
            }
            if (l.Count == 0) l.Add(new ChartRendererGdi.Balken("ohne Erzeugung", 1, true));
            return l;
        }

        private static List<ChartRenderer.Balken> BalkenSkia(ZeitreihenSatz z)
        {
            var l = new List<ChartRenderer.Balken>();
            double max = ErzeugerMax(z);
            for (int i = 0; i < ERZEUGER_SCHLUESSEL.Length; i++)
            {
                double v = JahresSummeMWh(z, ERZEUGER_SCHLUESSEL[i]);
                if (v > 0) l.Add(new ChartRenderer.Balken(ERZEUGER_NAMEN[i], v, v >= max));
            }
            if (l.Count == 0) l.Add(new ChartRenderer.Balken("ohne Erzeugung", 1, true));
            return l;
        }

        private static double ErzeugerMax(ZeitreihenSatz z)
        {
            double max = 0;
            foreach (string s in ERZEUGER_SCHLUESSEL) max = Math.Max(max, JahresSummeMWh(z, s));
            return max;
        }

        /// <summary>
        /// FESTES Beispiel fuer den Kapitalwert-Verlauf: eine Stammlinie und zwei
        /// Varianten ueber 21 Stuetzstellen (Jahr 0…20), rein rechnerisch erzeugt. Der
        /// Verlauf laeuft durch die Nulllinie, damit auch der gestrichelte Nullstrich und
        /// der negative Achsenbereich im Bild vorkommen.
        /// </summary>
        private static List<VerlaufSerie> Beispielserien()
        {
            return new List<VerlaufSerie>
            {
                Beispielserie("Stamm", true, -180000.0, 21000.0),
                Beispielserie("Variante A", false, -260000.0, 32000.0),
                Beispielserie("Variante B", false, -95000.0, 9000.0)
            };
        }

        private static VerlaufSerie Beispielserie(string name, bool stamm, double invest, double jahresnutzen)
        {
            var k = new double[21];
            k[0] = invest;
            for (int t = 1; t < k.Length; t++)
                k[t] = k[t - 1] + jahresnutzen * Math.Pow(0.97, t);
            return new VerlaufSerie { Anzeige = name, IstStamm = stamm, Kumuliert = k };
        }

        // =================================================================================
        // Messung
        // =================================================================================

        private sealed class Palettenwert
        {
            public string Name;
            public double ProzentGdi;
            public double ProzentSkia;
            public double Abweichung { get { return Math.Abs(ProzentGdi - ProzentSkia); } }
        }

        private sealed class Befund
        {
            public string Typ;
            public string MasseGdi = "-";
            public string MasseSkia = "-";
            public bool MasseGleich;
            public double AbweichendeProzent;
            public bool Pass;
            public string Anmerkung = "";
            public readonly List<Palettenwert> Palette = new List<Palettenwert>();

            public string Kurz
            {
                get
                {
                    if (!string.IsNullOrEmpty(Anmerkung)) return Anmerkung;
                    return MasseGdi + ", " + AbweichendeProzent.ToString("N2", CultureInfo.InvariantCulture) +
                           " % abweichende Pixel";
                }
            }
        }

        /// <summary>
        /// Die Palette des Berichts — jede Farbe steht fuer eine Serie.
        ///
        /// <para>Die RGB-Werte stehen hier AUSGESCHRIEBEN und werden bewusst nicht aus
        /// <c>ChartRenderer.C_*</c> gelesen: Das Messwerkzeug soll unabhaengig davon
        /// arbeiten, welchen Farbtyp der jeweilige Renderer gerade fuehrt (der
        /// Alt-Renderer System.Drawing.Color, der neue SKColor). Aendert sich eine
        /// Palettenfarbe im Renderer, faellt das hier als leeres Histogramm auf — das ist
        /// gewollt, denn eine stillschweigend andere Serienfarbe waere genau der Befund,
        /// den dieser Modus finden soll.</para>
        /// </summary>
        private static readonly KeyValuePair<string, SKColor>[] PALETTE =
        {
            new KeyValuePair<string, SKColor>("C_WP",     new SKColor(0x41, 0x72, 0xC4)),
            new KeyValuePair<string, SKColor>("C_BHKW",   new SKColor(0xED, 0x7D, 0x31)),
            new KeyValuePair<string, SKColor>("C_KESSEL", new SKColor(0x80, 0x80, 0x80)),
            new KeyValuePair<string, SKColor>("C_SOLAR",  new SKColor(0xFF, 0xC0, 0x00)),
            new KeyValuePair<string, SKColor>("C_PV",     new SKColor(0x70, 0xAD, 0x47)),
            new KeyValuePair<string, SKColor>("C_NETZ",   new SKColor(0x9E, 0x48, 0x0E)),
            new KeyValuePair<string, SKColor>("C_REST",   new SKColor(0xBF, 0xBF, 0xBF)),
            new KeyValuePair<string, SKColor>("C_BEDARF", new SKColor(0x33, 0x33, 0x33)),
            new KeyValuePair<string, SKColor>("C_STAMM",  new SKColor(0x1F, 0x4E, 0x79)),
            // C_SPEICHER[0..5] - die Farbfolge der Speicherlinien (Konzept 6.3).
            new KeyValuePair<string, SKColor>("C_SPEICHER[0] MediumVioletRed", new SKColor(0xC7, 0x15, 0x85)),
            new KeyValuePair<string, SKColor>("C_SPEICHER[1] DarkViolet",      new SKColor(0x94, 0x00, 0xD3)),
            new KeyValuePair<string, SKColor>("C_SPEICHER[2] Teal",            new SKColor(0x00, 0x80, 0x80)),
            new KeyValuePair<string, SKColor>("C_SPEICHER[3] SaddleBrown",     new SKColor(0x8B, 0x45, 0x13)),
            new KeyValuePair<string, SKColor>("C_SPEICHER[4] DarkSlateGray",   new SKColor(0x2F, 0x4F, 0x4F)),
            new KeyValuePair<string, SKColor>("C_SPEICHER[5] Crimson",         new SKColor(0xDC, 0x14, 0x3C))
        };

        private static Befund Vergleiche(Bildpaar p)
        {
            var b = new Befund { Typ = p.Typ };

            if (p.Gdi == null && p.Skia == null)
            {
                // Beide Renderer sagen "dieses Bild gibt es fuer dieses Projekt nicht" -
                // dieselbe Aussage auf beiden Seiten ist ein bestandener Vergleich.
                b.Anmerkung = "entfaellt (beide Renderer liefern kein Bild)";
                b.Pass = true;
                return b;
            }
            if (p.Gdi == null || p.Skia == null)
            {
                b.Anmerkung = "NUR " + (p.Gdi == null ? "SkiaSharp" : "GDI+") + " liefert ein Bild";
                return b;
            }

            using (SKBitmap a = SKBitmap.Decode(p.Gdi))
            using (SKBitmap c = SKBitmap.Decode(p.Skia))
            {
                if (a == null || c == null)
                {
                    b.Anmerkung = "PNG nicht dekodierbar";
                    return b;
                }

                b.MasseGdi = a.Width + "x" + a.Height;
                b.MasseSkia = c.Width + "x" + c.Height;
                b.MasseGleich = (a.Width == c.Width && a.Height == c.Height);
                if (!b.MasseGleich)
                {
                    b.Anmerkung = "MASSE UNGLEICH";
                    return b;
                }

                SKColor[] pa = a.Pixels;
                SKColor[] pc = c.Pixels;
                long gesamt = pa.Length;
                long abweichend = 0;
                var trefferA = new long[PALETTE.Length];
                var trefferC = new long[PALETTE.Length];

                for (long i = 0; i < gesamt; i++)
                {
                    SKColor x = pa[i], y = pc[i];
                    if (Math.Abs(x.Red - y.Red) > KANALTOLERANZ ||
                        Math.Abs(x.Green - y.Green) > KANALTOLERANZ ||
                        Math.Abs(x.Blue - y.Blue) > KANALTOLERANZ)
                        abweichend++;

                    for (int k = 0; k < PALETTE.Length; k++)
                    {
                        if (Nah(x, PALETTE[k].Value)) trefferA[k]++;
                        if (Nah(y, PALETTE[k].Value)) trefferC[k]++;
                    }
                }

                b.AbweichendeProzent = gesamt > 0 ? abweichend * 100.0 / gesamt : 0;
                for (int k = 0; k < PALETTE.Length; k++)
                {
                    if (trefferA[k] == 0 && trefferC[k] == 0) continue;   // Farbe kommt im Bild nicht vor
                    b.Palette.Add(new Palettenwert
                    {
                        Name = PALETTE[k].Key,
                        ProzentGdi = trefferA[k] * 100.0 / gesamt,
                        ProzentSkia = trefferC[k] * 100.0 / gesamt
                    });
                }

                b.Pass = b.MasseGleich && b.AbweichendeProzent < GRENZE_PROZENT;
                return b;
            }
        }

        private static bool Nah(SKColor a, SKColor b)
        {
            return Math.Abs(a.Red - b.Red) <= HISTOGRAMM_TOLERANZ
                && Math.Abs(a.Green - b.Green) <= HISTOGRAMM_TOLERANZ
                && Math.Abs(a.Blue - b.Blue) <= HISTOGRAMM_TOLERANZ;
        }

        // =================================================================================
        // Bericht
        // =================================================================================

        private static IEnumerable<string> Tabelle(List<Befund> befunde)
        {
            var l = new List<string>
            {
                "| Bildtyp | Masse GDI+ | Masse Skia | Masse gleich | abweichende Pixel | Palette max. Abw. | Ergebnis |",
                "|---|---|---|---|---|---|---|"
            };
            foreach (Befund b in befunde)
            {
                double maxAbw = b.Palette.Count > 0 ? b.Palette.Max(x => x.Abweichung) : 0;
                l.Add("| " + b.Typ +
                      " | " + b.MasseGdi +
                      " | " + b.MasseSkia +
                      " | " + (string.IsNullOrEmpty(b.Anmerkung) ? (b.MasseGleich ? "ja" : "NEIN") : "-") +
                      " | " + (string.IsNullOrEmpty(b.Anmerkung)
                               ? b.AbweichendeProzent.ToString("N2", CultureInfo.InvariantCulture) + " %" : "-") +
                      " | " + (b.Palette.Count > 0
                               ? maxAbw.ToString("N2", CultureInfo.InvariantCulture) + " %-Pkt." : "-") +
                      " | " + (b.Pass ? "PASS" : "PRUEFEN") +
                      (string.IsNullOrEmpty(b.Anmerkung) ? "" : " (" + b.Anmerkung + ")") + " |");
            }
            return l;
        }

        private static IEnumerable<string> Histogrammbloecke(List<Befund> befunde)
        {
            var l = new List<string>();
            foreach (Befund b in befunde)
            {
                if (b.Palette.Count == 0) continue;
                l.Add("<details><summary>Farbhistogramm " + b.Typ + "</summary>");
                l.Add("");
                l.Add("| Palettenfarbe | Anteil GDI+ | Anteil Skia | Abweichung |");
                l.Add("|---|---|---|---|");
                foreach (Palettenwert w in b.Palette)
                    l.Add("| " + w.Name +
                          " | " + w.ProzentGdi.ToString("N3", CultureInfo.InvariantCulture) + " %" +
                          " | " + w.ProzentSkia.ToString("N3", CultureInfo.InvariantCulture) + " %" +
                          " | " + w.Abweichung.ToString("N3", CultureInfo.InvariantCulture) + " %-Pkt. |");
                l.Add("");
                l.Add("</details>");
                l.Add("");
            }
            return l;
        }

        private static void SchreibeBericht(string datei, string quelle, List<int> ids,
                                            List<string> zeilen, bool allesGruen)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Bildvergleich GDI+ <-> SkiaSharp (Paket iU7-1)");
            sb.AppendLine();
            sb.AppendLine("**Zeitpunkt:** " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture));
            sb.AppendLine();
            sb.AppendLine("**Quelle:** `" + quelle + "`");
            sb.AppendLine();
            sb.AppendLine("**Projekte:** " + string.Join(", ",
                ids.Select(i => i.ToString(CultureInfo.InvariantCulture))));
            sb.AppendLine();
            sb.AppendLine("**Kriterium PASS:** Bildmasse gleich UND weniger als " +
                          GRENZE_PROZENT.ToString("N0", CultureInfo.InvariantCulture) +
                          " % abweichende Pixel bei einer Toleranz von " + KANALTOLERANZ +
                          "/255 je Farbkanal. Das Farbhistogramm zaehlt Pixel, die einer Palettenfarbe");
            sb.AppendLine("bis auf " + HISTOGRAMM_TOLERANZ +
                          "/255 je Kanal entsprechen; es ist eine Beobachtungsgroesse und geht nicht");
            sb.AppendLine("in PASS/PRUEFEN ein.");
            sb.AppendLine();
            sb.AppendLine("**Gesamtergebnis:** " + (allesGruen ? "PASS" : "PRUEFEN"));
            foreach (string z in zeilen) sb.AppendLine(z);

            Directory.CreateDirectory(Path.GetDirectoryName(datei));
            File.WriteAllText(datei, sb.ToString(), new UTF8Encoding(true));
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
