using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Etappe DG-E3, Gruppe (d) — die vier reinen BERICHTSBILDER.</b>
    /// <c>JahresverlaufWaerme</c>, <c>DauerlinieWaerme</c>, <c>Speicherverlauf</c> und
    /// <c>Speichertemperaturen</c> haben seit diesem Auftrag je ein Zeichenmodell.
    ///
    /// <para><b>Entscheid DG-E3-7 — sie bleiben PIXELBILDER.</b> Sie tragen keine
    /// <see cref="Zeichenmodell.Flaeche"/> und keine <see cref="Datenreihe"/>: Im
    /// Bericht gibt es keine Bedienung, der Gewinn des SVG ist Schärfe und Text. Der
    /// <c>SvgSchreiber</c> schreibt deshalb jeden Befehl so, wie der
    /// <c>SkiaMaler</c> ihn malt — die Reihen als Pixelpfad, die Beschriftungen als
    /// <c>&lt;text&gt;</c>. Genau das prüfen diese Fälle; ein Rückfall auf den
    /// Datenkoordinaten-Weg fiele im PNG-Vergleich NICHT auf.</para>
    ///
    /// <para>Die Marken sind da, obwohl der Bericht sie nicht bedient: Sie sind die
    /// Struktur, an der ein Leser (und ein späterer Bildschirmweg) Titel, Achsen,
    /// Reihen und Legende auseinanderhält.</para>
    ///
    /// <para>Ohne Datenbank und ohne Oberfläche: Der Zeitreihensatz ist synthetisch,
    /// aber vollständig — Erzeugung, Bedarf, zwei Wärmespeicher mit beiden
    /// Schichttemperaturen, ein Stromspeicher und eine Quelltemperatur.</para>
    /// </summary>
    public class ChartRendererGruppeDTests
    {
        // =====================================================================
        //  1 — jede der vier Methoden liefert ihr Modell, und das PNG kommt daraus
        // =====================================================================

        /// <summary>
        /// Der <c>byte[]</c>-Weg ist <c>SkiaMaler.Png(…Modell(…))</c> — Byte für Byte.
        /// Das ist die Probe darauf, dass es nur EINE Beschreibung des Bildes gibt.
        /// </summary>
        [Theory]
        [InlineData("jahresverlauf")]
        [InlineData("dauerlinie")]
        [InlineData("speicherverlauf")]
        [InlineData("speichertemperaturen")]
        public void DasPngKommtAusDemselbenModell(string bild)
        {
            ZeitreihenSatz z = Satz();

            byte[] ausMethode = Png(bild, z);
            byte[] ausModell = SkiaMaler.Png(Modell(bild, z));

            Assert.NotNull(ausMethode);
            Assert.Equal(ausModell, ausMethode);
        }

        /// <summary>Zweimal erzeugt ist dasselbe Modell — kein Zufall, keine Uhrzeit.</summary>
        [Theory]
        [InlineData("jahresverlauf")]
        [InlineData("dauerlinie")]
        [InlineData("speicherverlauf")]
        [InlineData("speichertemperaturen")]
        public void ZweimalErzeugtIstDasselbeModell(string bild)
        {
            ZeitreihenSatz z = Satz();
            Assert.True(Modell(bild, z).Gleicht(Modell(bild, z)),
                        "Das Modell von „" + bild + "\" ist nicht deterministisch.");
        }

        // =====================================================================
        //  2 — DG-E3-7: reines Pixelbild
        // =====================================================================

        /// <summary>
        /// Keine Zeichenfläche, keine Datenreihe (DG-E3-7) — und deshalb im SVG kein
        /// inneres <c>&lt;svg class="epos-flaeche"&gt;</c> und kein
        /// <c>path.epos-reihe</c>. Die Reihen stehen als gewöhnlicher Pixelpfad da,
        /// genau wie im PNG.
        /// </summary>
        [Theory]
        [InlineData("jahresverlauf")]
        [InlineData("dauerlinie")]
        [InlineData("speicherverlauf")]
        [InlineData("speichertemperaturen")]
        public void DieVierSindReinePixelbilder(string bild)
        {
            Zeichenmodell m = Modell(bild, Satz());

            Assert.Null(m.Flaeche);
            Assert.Empty(m.Reihen);

            List<SvgKnoten> alle = SvgSchreiber.Baum(m).Alle().ToList();
            Assert.DoesNotContain(alle, k => k.Name == "svg" && Attribut(k, "class") == "epos-flaeche");
            Assert.DoesNotContain(alle, k => Attribut(k, "class") == "epos-reihe");
        }

        /// <summary>
        /// Jeder Text des Modells wird ein <c>&lt;text&gt;</c> — er bleibt also Text
        /// und wird nicht zum Pfad. Das ist der zweite Gewinn des SVG im Bericht
        /// (der erste ist die Schärfe): Titel, Achsen und Legende sind durchsuchbar.
        /// </summary>
        [Theory]
        [InlineData("jahresverlauf")]
        [InlineData("dauerlinie")]
        [InlineData("speicherverlauf")]
        [InlineData("speichertemperaturen")]
        public void JederTextDesModellsStehtAlsTextImSvg(string bild)
        {
            Zeichenmodell m = Modell(bild, Satz());

            int imModell = Textbefehle(m.Befehle);
            int imBaum = SvgSchreiber.Baum(m).Alle().Count(k => k.Name == "text");

            Assert.True(imModell > 0, "Das Bild führt gar keinen Text.");
            Assert.Equal(imModell, imBaum);
        }

        /// <summary>
        /// Der SVG-Text beginnt wohlgeformt mit <c>&lt;svg</c>, trägt den Namensraum
        /// und ist zweimal geschrieben byte-gleich — sonst wäre er als eigener Teil
        /// im Wortbericht nicht brauchbar (DG-E3-8).
        /// </summary>
        [Theory]
        [InlineData("jahresverlauf")]
        [InlineData("dauerlinie")]
        [InlineData("speicherverlauf")]
        [InlineData("speichertemperaturen")]
        public void DerSvgTextBeginntMitSvgUndIstDeterministisch(string bild)
        {
            ZeitreihenSatz z = Satz();
            Zeichenmodell m = Modell(bild, z);

            string a = SvgSchreiber.Text(m);
            Assert.StartsWith("<svg", a, StringComparison.Ordinal);
            Assert.Contains("xmlns=\"http://www.w3.org/2000/svg\"", a, StringComparison.Ordinal);
            Assert.Contains("<text", a, StringComparison.Ordinal);

            Assert.Equal(a, SvgSchreiber.Text(m));
            Assert.Equal(a, SvgSchreiber.Text(Modell(bild, Satz())));
        }

        // =====================================================================
        //  3 — die Marken
        // =====================================================================

        /// <summary>
        /// Titel, x-Achse, y-Achse, je Reihe eine Marke und je Legendeneintrag eine —
        /// und das ACHSENKREUZ ohne Marke, damit es beim Ausblenden einer
        /// Achsenteilung stehen bleibt.
        /// </summary>
        [Fact]
        public void JahresverlaufWaermeModell_traegtSeineMarken()
        {
            Zeichenmodell m = ChartRenderer.JahresverlaufWaermeModell(Satz());
            HashSet<string> marken = Marken(m);

            Assert.Contains("titel", marken);
            Assert.Contains("xachse", marken);
            Assert.Contains("yachse", marken);
            Assert.Contains("reihe:Wärmebedarf", marken);
            Assert.Contains("legende:Wärmebedarf", marken);

            // Jede gezeichnete Reihe steht auch in der Legende — und umgekehrt.
            Assert.Equal(Reihennamen(marken), Legendennamen(marken));

            Assert.Contains(m.Befehle, b => b.Marke == null);
        }

        /// <summary>Dieselben Marken bei der Dauerlinie; x zählt hier den RANG.</summary>
        [Fact]
        public void DauerlinieWaermeModell_traegtSeineMarken()
        {
            Zeichenmodell m = ChartRenderer.DauerlinieWaermeModell(Satz());
            HashSet<string> marken = Marken(m);

            Assert.Contains("titel", marken);
            Assert.Contains("xachse", marken);
            Assert.Contains("yachse", marken);
            Assert.Contains("reihe:Wärmebedarf", marken);
            Assert.Equal(Reihennamen(marken), Legendennamen(marken));
        }

        /// <summary>
        /// Die drei Wochenfelder: Jede Reihe wird DREIMAL gezeichnet und trägt jedes
        /// Mal dieselbe Marke. Die Feldüberschriften sind die Beschriftung der
        /// Zeitachse und stehen unter <c>xachse</c>, die zwei Randwerte links unter
        /// <c>yachse</c>.
        /// </summary>
        [Fact]
        public void SpeicherverlaufModell_traegtJedeReiheDreimalUnterDerselbenMarke()
        {
            Zeichenmodell m = ChartRenderer.SpeicherverlaufModell(Satz());
            HashSet<string> marken = Marken(m);

            Assert.Contains("titel", marken);
            Assert.Contains("xachse", marken);
            Assert.Contains("yachse", marken);
            Assert.Contains("reihe:Heizungspuffer (Senke)", marken);
            Assert.Contains("reihe:Stromspeicher (PV)", marken);
            Assert.Equal(Reihennamen(marken), Legendennamen(marken));

            // Drei Felder — also drei Pfade je Reihe.
            Assert.Equal(3, m.Befehle.Count(b => b.Marke == "reihe:Stromspeicher (PV)"));

            // Drei Feldüberschriften unter xachse.
            Assert.Equal(3, m.Befehle.OfType<WindowsFormsApplication1.Zeichnung.Text>()
                                     .Count(t => t.Marke == "xachse"));
        }

        /// <summary>
        /// Speichertemperaturen: zwei Reihen je Speicher, dazu die Quelltemperatur —
        /// jede mit Marke und Legendeneintrag.
        /// </summary>
        [Fact]
        public void SpeichertemperaturenModell_traegtObenUntenUndQuelle()
        {
            Zeichenmodell m = ChartRenderer.SpeichertemperaturenModell(Satz());
            HashSet<string> marken = Marken(m);

            Assert.Contains("titel", marken);
            Assert.Contains("xachse", marken);
            Assert.Contains("yachse", marken);
            Assert.Contains("reihe:Heizungspuffer (Senke) oben", marken);
            Assert.Contains("reihe:Heizungspuffer (Senke) unten", marken);
            Assert.Contains("reihe:Erdsonde (Quelle)", marken);
            Assert.Equal(Reihennamen(marken), Legendennamen(marken));
        }

        // =====================================================================
        //  4 — kein Bild, kein Modell
        // =====================================================================

        /// <summary>
        /// Ein leerer Zeitreihensatz gibt <c>null</c> — und zwar auf BEIDEN Wegen.
        /// Die vier zeichnen bewusst keinen Leerhinweis: Der Bericht lässt die Stelle
        /// samt Beschriftung aus, statt eine leere Fläche zu setzen.
        /// </summary>
        [Theory]
        [InlineData("jahresverlauf")]
        [InlineData("dauerlinie")]
        [InlineData("speicherverlauf")]
        [InlineData("speichertemperaturen")]
        public void OhneReihenGibtEsKeinModellUndKeinBild(string bild)
        {
            var leer = new ZeitreihenSatz();
            Assert.Null(Modell(bild, leer));
            Assert.Null(Png(bild, leer));
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        private static Zeichenmodell Modell(string bild, ZeitreihenSatz z)
        {
            switch (bild)
            {
                case "jahresverlauf": return ChartRenderer.JahresverlaufWaermeModell(z);
                case "dauerlinie": return ChartRenderer.DauerlinieWaermeModell(z);
                case "speicherverlauf": return ChartRenderer.SpeicherverlaufModell(z);
                case "speichertemperaturen": return ChartRenderer.SpeichertemperaturenModell(z);
                default: throw new ArgumentOutOfRangeException(nameof(bild), bild, "unbekanntes Bild");
            }
        }

        private static byte[] Png(string bild, ZeitreihenSatz z)
        {
            switch (bild)
            {
                case "jahresverlauf": return ChartRenderer.JahresverlaufWaerme(z);
                case "dauerlinie": return ChartRenderer.DauerlinieWaerme(z);
                case "speicherverlauf": return ChartRenderer.Speicherverlauf(z);
                case "speichertemperaturen": return ChartRenderer.Speichertemperaturen(z);
                default: throw new ArgumentOutOfRangeException(nameof(bild), bild, "unbekanntes Bild");
            }
        }

        /// <summary>Die Namen aus den <c>reihe:</c>-Marken.</summary>
        private static string[] Reihennamen(HashSet<string> marken)
            => Namen(marken, "reihe:");

        /// <summary>Die Namen aus den <c>legende:</c>-Marken.</summary>
        private static string[] Legendennamen(HashSet<string> marken)
            => Namen(marken, "legende:");

        private static string[] Namen(HashSet<string> marken, string vorsatz)
            => marken.Where(s => s.StartsWith(vorsatz, StringComparison.Ordinal))
                     .Select(s => s.Substring(vorsatz.Length))
                     .OrderBy(s => s, StringComparer.Ordinal)
                     .ToArray();

        private static HashSet<string> Marken(Zeichenmodell m)
        {
            var marken = new HashSet<string>(StringComparer.Ordinal);
            Sammle(m.Befehle, marken);
            return marken;
        }

        private static void Sammle(IReadOnlyList<Zeichenbefehl> befehle, HashSet<string> ziel)
        {
            foreach (Zeichenbefehl b in befehle)
            {
                if (b.Marke != null) ziel.Add(b.Marke);
                if (b is Gruppe g) Sammle(g.Befehle, ziel);
            }
        }

        private static int Textbefehle(IReadOnlyList<Zeichenbefehl> befehle)
        {
            int n = 0;
            foreach (Zeichenbefehl b in befehle)
            {
                if (b is WindowsFormsApplication1.Zeichnung.Text) n++;
                else if (b is Gruppe g) n += Textbefehle(g.Befehle);
            }
            return n;
        }

        private static string Attribut(SvgKnoten knoten, string name)
        {
            foreach (KeyValuePair<string, string> a in knoten.Attribute)
                if (a.Key == name) return a.Value;
            return null;
        }

        // ------------------------------------------------- der synthetische Satz

        /// <summary>
        /// Ein vollständiger Zeitreihensatz ohne Datenbank: Bedarf und fünf Erzeuger,
        /// zwei Wärmespeicher mit Füllstand und beiden Schichttemperaturen, ein
        /// Stromspeicher und eine Quelltemperatur. Dieselbe Bauform wie der Satz der
        /// <c>ChartProben</c>, nur kleiner geschrieben.
        /// </summary>
        internal static ZeitreihenSatz Satz()
        {
            var z = new ZeitreihenSatz();

            z.Reihen[ZeitreihenSatz.WAERMEBEDARF] = Reihe(180, 120, 25, Math.PI / 2);
            z.Reihen[ZeitreihenSatz.SOLAR_WAERME] = Reihe(18, 16, 8, -Math.PI / 2);
            z.Reihen[ZeitreihenSatz.WP_WAERME] = Reihe(70, 40, 12, Math.PI / 2);
            z.Reihen[ZeitreihenSatz.BHKW_WAERME] = Reihe(45, 25, 9, Math.PI / 2);
            z.Reihen[ZeitreihenSatz.KESSEL_WAERME] = Reihe(25, 35, 6, Math.PI / 2);

            z.Reihen[ZeitreihenSatz.STROMBEDARF] = Reihe(90, 20, 30, 0);
            z.Reihen[ZeitreihenSatz.PV_GENUTZT] = Reihe(28, 26, 18, -Math.PI / 2);
            z.Reihen[ZeitreihenSatz.BHKW_STROM] = Reihe(30, 18, 6, Math.PI / 2);
            z.Reihen[ZeitreihenSatz.NETZBEZUG] = Reihe(40, 10, 14, 0);
            z.Reihen[ZeitreihenSatz.PV_SPEICHER_SOC] = Reihe(160, 80, 60, -Math.PI / 2);

            Speicher(z, "PUFFER_11", "Heizungspuffer (Senke)", 520, 260, 180, 0.0, 68, 46);
            Speicher(z, "PUFFER_12", "Brauchwasserspeicher (Senke)", 300, 140, 110, 0.6, 60, 40);

            z.Reihen[ZeitreihenSatz.QUELLTEMP_PRAEFIX + "5"] = Temperatur(12, 9, 2, -Math.PI / 2);
            z.Beschriftungen[ZeitreihenSatz.QUELLTEMP_PRAEFIX + "5"] = "Erdsonde (Quelle)";

            return z;
        }

        private static void Speicher(ZeitreihenSatz z, string schluessel, string beschriftung,
                                     double grund, double jahreshub, double tageshub,
                                     double phase, double tOben, double tUnten)
        {
            z.Reihen[schluessel] = Reihe(grund, jahreshub, tageshub, phase);
            z.Beschriftungen[schluessel] = beschriftung;
            z.Speicherreihen.Add(schluessel);

            z.Reihen[schluessel + ZeitreihenSatz.SUFFIX_T_OBEN] = Temperatur(tOben, 6, 4, phase);
            z.Beschriftungen[schluessel + ZeitreihenSatz.SUFFIX_T_OBEN] = beschriftung + " oben";

            z.Reihen[schluessel + ZeitreihenSatz.SUFFIX_T_UNTEN] = Temperatur(tUnten, 5, 3, phase);
            z.Beschriftungen[schluessel + ZeitreihenSatz.SUFFIX_T_UNTEN] = beschriftung + " unten";
        }

        /// <summary>Eine Jahresreihe mit Jahres- und Tagesgang, nie unter null.</summary>
        private static double[] Reihe(double grund, double jahreshub, double tageshub, double phase)
        {
            var w = new double[ZeitreihenSatz.Stunden];
            for (int i = 0; i < w.Length; i++)
            {
                double jahr = Math.Cos(2.0 * Math.PI * i / w.Length + phase);
                double tag = Math.Sin(2.0 * Math.PI * (i % 24) / 24.0);
                w[i] = Math.Round(Math.Max(0.0, grund + jahreshub * jahr + tageshub * tag), 3);
            }
            return w;
        }

        /// <summary>Dieselbe Form, aber mit Vorzeichen — Temperaturen dürfen fallen.</summary>
        private static double[] Temperatur(double grund, double jahreshub, double tageshub,
                                           double phase)
        {
            var w = new double[ZeitreihenSatz.Stunden];
            for (int i = 0; i < w.Length; i++)
            {
                double jahr = Math.Cos(2.0 * Math.PI * i / w.Length + phase);
                double tag = Math.Sin(2.0 * Math.PI * (i % 24) / 24.0);
                w[i] = Math.Round(grund + jahreshub * jahr + tageshub * tag, 3);
            }
            return w;
        }
    }
}
