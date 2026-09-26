using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SkiaSharp;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;

namespace ChartProben
{
    /// <summary>
    /// <b>ETAPPE E6 — DER VERLAUF MIT DREI SZENARIEN UND DIE DRITTE STRICHART</b>
    /// (Konzept Wirtschaftlichkeit § 2.13 (5), Mockup Kategorie 8 „Wie sicher ist das?").
    ///
    /// <para><b>Was hier gezeichnet wird.</b> Der kumulierte Barwert der Differenz zur
    /// Referenz je Jahr, je Stand in drei Stricharten (<c>ChartRenderer.KapitalwertSzenarien</c>):
    /// Farbe = Stand, Strichart = Szenario, zweigeteilte Legende, Nulldurchgang je Linie als
    /// Marke. Die Linien sind SYNTHETISCH (Investition, abgezinster Jahresnutzen und ein Ersatz
    /// im Jahr 13/15/17 — wie im Mockup) und gehen durch die REIHENBILDUNG des Renderers
    /// (<c>ChartRenderer.VerlaufsReihenSzenarien</c>), nicht an ihr vorbei.</para>
    ///
    /// <para><b>Die Texte sind die deutsche Vorgabe</b> (<c>VerlaufSzenarienTexte</c>) — kein
    /// Probebild hängt an der Oberflächensprache des Rechners.</para>
    ///
    /// <para><b>Die Gegenproben</b> halten fest, was Maß-, Farb- und Determinismusprüfung
    /// nicht sehen: dass die DRITTE Strichart gezeichnet wird und nicht auf eine der zwei
    /// alten fällt, dass der zweite Teil der Legende aus den Szenarien stammt und dass die
    /// Nulldurchgänge im Bild stehen.</para>
    /// </summary>
    internal static partial class Program
    {
        /// <summary>Die Probenamen der Stände — kurz, damit die Legende auf jedem System
        /// gleich umbricht (acht Namen: zwei Zeilen im ersten Legendenteil).</summary>
        private static readonly string[] STAENDE =
        {
            "Variante 1", "Variante 2", "Variante 3", "Variante 4",
            "Variante 5", "Variante 6", "Variante 7", "Variante 8", "Variante 9"
        };

        /// <summary>
        /// Die Proben dieser Etappe — eine Zeile in <c>Program.cs</c> ruft sie.
        /// </summary>
        private static void SzenarienProben(string ziel)
        {
            var texte = new ChartRenderer.VerlaufSzenarienTexte();
            const string TITEL = "Kumulierter Barwert der Differenz zur Referenz — drei Szenarien";
            const string FUSS = "Synthetische Probendaten, Etappe E6";

            // ---- Maßproben -------------------------------------------------------

            // Drei Stände, alle drei Szenarien: zwei Legendenzeilen, das feste Maß.
            Pruefe(ziel, "kapitalwert_szenarien", 1240, 620,
                   new[] { ChartRenderer.C_SERIEN[0], ChartRenderer.C_SERIEN[1], ChartRenderer.C_SERIEN[2] },
                   () => ChartRenderer.KapitalwertSzenarien(TITEL, Szenarieninhalt(3, texte), texte, FUSS));

            // EIN Stand (der Fall der Sicht 2, B − A): die Marken nennen ihr Szenario.
            Pruefe(ziel, "kapitalwert_szenarien_eine_variante", 1240, 620,
                   new[] { ChartRenderer.C_SERIEN[0] },
                   () => ChartRenderer.KapitalwertSzenarien(TITEL, Szenarieninhalt(1, texte), texte, null));

            // ACHT Stände — die Grenze der Palette: Der erste Legendenteil bricht in eine
            // zweite Zeile um, das Bild wird um eine Legendenzeile (30 px) länger, die
            // Zeichenfläche bleibt, wie sie ist. Die acht Farbfelder der Legende tragen jede
            // Farbe der Palette voll.
            Pruefe(ziel, "kapitalwert_szenarien_acht_varianten", 1240, 650,
                   ChartRenderer.C_SERIEN.ToArray(),
                   () => ChartRenderer.KapitalwertSzenarien(TITEL, Szenarieninhalt(8, texte), texte, FUSS));

            // NEUN Stände — die BENANNTE Ablehnung: kein Bild mit doppelt vergebener Farbe,
            // sondern der Grund an der Stelle des Bildes.
            Pruefe(ziel, "kapitalwert_szenarien_neun_varianten", 1240, 620,
                   new SKColor[0],
                   () => ChartRenderer.KapitalwertSzenarien(TITEL, Szenarieninhalt(9, texte), texte, null));

            // ---- Gegenproben -----------------------------------------------------

            // Erstens die DRITTE STRICHART: Ein Renderer, der „gepunktet" wie „gestrichelt"
            // zeichnete, bestuende jede Mass- und Farbpruefung. Dasselbe Bild je Version,
            // einmal mit gestrichelter, einmal mit gepunkteter Stammlinie.
            List<VerlaufSerie> serien = Beispielserien();
            Unterschiedlich("strichart_gepunktet_wirkt",
                () => ChartRenderer.KapitalwertVerlauf("Kumulierte Barwerte je Version",
                        MitStammStrichart(serien, ChartRenderer.Strichart.Gestrichelt), null),
                () => ChartRenderer.KapitalwertVerlauf("Kumulierte Barwerte je Version",
                        MitStammStrichart(serien, ChartRenderer.Strichart.Gepunktet), null));

            // Zweitens der ZWEITE TEIL DER LEGENDE: Dieselben Linien, nur die Namen der
            // Szenarien in der Legende sind andere. Bei drei Staenden tragen die Marken keinen
            // Szenarionamen — der Unterschied kann also nur aus der Legende kommen.
            Unterschiedlich("kapitalwert_szenarien_legende_zweigeteilt_wirkt",
                () => ChartRenderer.KapitalwertSzenarien(TITEL, Szenarieninhalt(3, texte), texte, null),
                () =>
                {
                    ChartRenderer.Szenarienreihen inhalt = Szenarieninhalt(3, texte);
                    var andere = new List<(string, ChartRenderer.Strichart)>(inhalt.Szenarien);
                    inhalt.Szenarien.Clear();
                    foreach ((string Name, ChartRenderer.Strichart Art) s in andere)
                        inhalt.Szenarien.Add((s.Name + " (Satz)", s.Art));
                    return ChartRenderer.KapitalwertSzenarien(TITEL, inhalt, texte, null);
                });

            // Drittens die NULLDURCHGAENGE: dasselbe Bild mit und ohne Marken.
            Unterschiedlich("kapitalwert_szenarien_nulldurchgang_wirkt",
                () => ChartRenderer.KapitalwertSzenarien(TITEL, Szenarieninhalt(3, texte), texte, null),
                () =>
                {
                    ChartRenderer.Szenarienreihen inhalt = Szenarieninhalt(3, texte);
                    inhalt.Marken.Clear();
                    return ChartRenderer.KapitalwertSzenarien(TITEL, inhalt, texte, null);
                });

            // ---- SVG: dasselbe Modell auf dem Bildschirmweg ----------------------

            SvgModellprobe("kapitalwert_szenarien",
                () => ChartRenderer.KapitalwertSzenarienModell(TITEL, Szenarieninhalt(3, texte), texte, FUSS));
            SzenarienStrichprobe(texte);

            // Sichtprüfung (--svg-alle): dasselbe Modell als Datei neben dem Skia-PNG.
            if (_svgordner != null)
                SvgOrdnerSchreiben(new List<KeyValuePair<string, Func<Zeichenmodell>>>
                {
                    new KeyValuePair<string, Func<Zeichenmodell>>("kapitalwert_szenarien",
                        () => ChartRenderer.KapitalwertSzenarienModell(TITEL, Szenarieninhalt(3, texte), texte, FUSS))
                });
        }

        /// <summary>
        /// <b>Die Stricharten im SVG</b>: Jede Linie trägt im Bildschirmweg dieselbe
        /// Strichfolge wie im PNG — Ungünstig 8/5, Günstig 2,5/3,5, Erwartet keine —, und
        /// jede Marke eines Nulldurchgangs nennt ihren Wert am Element (<c>data-wert</c>).
        /// </summary>
        private static void SzenarienStrichprobe(ChartRenderer.VerlaufSzenarienTexte texte)
        {
            SvgProbe("svg_kapitalwert_szenarien_stricharten", e =>
            {
                ChartRenderer.Szenarienreihen inhalt = Szenarieninhalt(3, texte);
                Zeichenmodell m = ChartRenderer.KapitalwertSzenarienModell("K", inhalt, texte, null);
                List<SvgKnoten> alle = SvgSchreiber.Baum(m).Alle().ToList();
                e.Masse = m.Breite + "x" + m.Hoehe;
                e.Knoten = alle.Count.ToString(CultureInfo.InvariantCulture);

                List<SvgKnoten> pfade = alle.Where(
                    k => k.Name == "path" && Attributwert(k, "class") == "epos-reihe").ToList();
                if (pfade.Count != 9) e.Maengel.Add("Reihenpfade: " + pfade.Count + " statt 9");

                foreach (SvgKnoten pf in pfade)
                {
                    string marke = Attributwert(pf, "data-marke") ?? "";
                    string strich = Attributwert(pf, "stroke-dasharray");
                    string soll = marke.EndsWith(texte.Worst, StringComparison.Ordinal) ? "8 5"
                                : marke.EndsWith(texte.Best, StringComparison.Ordinal) ? "2.5 3.5"
                                : null;
                    if (!string.Equals(strich, soll, StringComparison.Ordinal))
                        e.Maengel.Add("Strichfolge von " + marke + ": " + (strich ?? "keine") +
                                      " statt " + (soll ?? "keine"));
                }

                List<SvgKnoten> marken = alle.Where(
                    k => Attributwert(k, "data-marke") == "nulldurchgang").ToList();
                if (marken.Count == 0) e.Maengel.Add("keine Marke eines Nulldurchgangs im Baum");
                foreach (SvgKnoten k in marken)
                    if (string.IsNullOrEmpty(Attributwert(k, "data-wert")))
                        e.Maengel.Add("Marke eines Nulldurchgangs ohne data-wert");
                if (inhalt.Marken.Count == 0) e.Maengel.Add("die Reihenbildung meldet keinen Nulldurchgang");
            });

            // Der DRUCK (Wortbericht): kein inneres svg, kein vector-effect — Word kennt die
            // Eigenschaft nicht und dehnte Strich und Strichfolge zu Baendern. Die neun Linien
            // stehen als Pixelpfade ohne Fuellung, in derselben Strichfolge wie das PNG.
            SvgProbe("svg_kapitalwert_szenarien_druck", e =>
            {
                ChartRenderer.Szenarienreihen inhalt = Szenarieninhalt(3, texte);
                Zeichenmodell m = ChartRenderer.KapitalwertSzenarienModell("K", inhalt, texte, null);
                List<SvgKnoten> alle = SvgSchreiber.Druckbaum(m).Alle().ToList();
                e.Masse = m.Breite + "x" + m.Hoehe;
                e.Knoten = alle.Count.ToString(CultureInfo.InvariantCulture);

                string text = SvgSchreiber.Drucktext(m);
                if (text.Contains(SvgSchreiber.KLASSE_FLAECHE)) e.Maengel.Add("inneres svg im Druck");
                if (text.Contains("vector-effect")) e.Maengel.Add("vector-effect im Druck");

                List<SvgKnoten> pfade = alle.Where(k => k.Name == "path" &&
                    (Attributwert(k, "data-marke") ?? "").StartsWith("reihe:", StringComparison.Ordinal)).ToList();
                if (pfade.Count != 9) e.Maengel.Add("Reihenpfade im Druck: " + pfade.Count + " statt 9");
                foreach (SvgKnoten pf in pfade)
                {
                    if (Attributwert(pf, "fill") != "none")
                        e.Maengel.Add("Reihenpfad mit Fuellung: " + Attributwert(pf, "data-marke"));
                    string marke = Attributwert(pf, "data-marke") ?? "";
                    string strich = Attributwert(pf, "stroke-dasharray");
                    bool soll = marke.EndsWith(texte.Worst, StringComparison.Ordinal)
                             || marke.EndsWith(texte.Best, StringComparison.Ordinal);
                    if (soll != (strich != null))
                        e.Maengel.Add("Strichfolge im Druck von " + marke + ": " + (strich ?? "keine"));
                }
            });
        }

        /// <summary>
        /// Die Linien des Bildes durch die Reihenbildung des Renderers — dieselbe, die
        /// Seite und Bericht nehmen.
        /// </summary>
        private static ChartRenderer.Szenarienreihen Szenarieninhalt(
            int staende, ChartRenderer.VerlaufSzenarienTexte texte)
            => ChartRenderer.VerlaufsReihenSzenarien(Szenarienmodell(staende), texte);

        /// <summary>
        /// Ein Sammelmodell aus synthetischen Läufen: ein Stamm als Referenz und
        /// <paramref name="staende"/> Stände mit je einer Differenzlinie in allen drei
        /// Szenarien, über 20 Jahre.
        /// </summary>
        private static WirtschaftlichkeitVerlaufSzenarien Szenarienmodell(int staende)
        {
            const int JAHRE = 20;
            var modell = new WirtschaftlichkeitVerlaufSzenarien { Jahre = JAHRE };
            foreach (string s in WirtschaftlichkeitVerlaufSzenarien.Reihenfolge)
            {
                var lauf = new WirtschaftlichkeitVerlauf { Jahre = JAHRE, Szenario = s };
                lauf.Absolut.Add(new VerlaufSerie
                {
                    IdProjekt = 900, Anzeige = "Stamm", IstStamm = true, Kumuliert = new double[JAHRE + 1]
                });
                for (int i = 0; i < staende; i++)
                {
                    int id = 901 + i;
                    double[] d = Differenzlinie(i, s, JAHRE);
                    lauf.Absolut.Add(new VerlaufSerie { IdProjekt = id, Anzeige = STAENDE[i], Kumuliert = d });
                    lauf.Differenz.Add(new VerlaufSerie
                    {
                        IdProjekt = id, Anzeige = STAENDE[i], Kumuliert = d,
                        RestwertBarwert = 20000.0 + 5000.0 * i
                    });
                }
                modell.Laeufe[s] = lauf;
            }
            return modell;
        }

        /// <summary>
        /// Eine synthetische Differenzlinie: Mehrinvestition, abgezinster Jahresnutzen und
        /// ein Ersatz — im ungünstigen Fall früher (Jahr 13) und teurer, im günstigen später
        /// (Jahr 17) und billiger. Der achte Stand erreicht die Nulllinie nicht: Auch den
        /// Fall ohne Marke zeigt das Bild.
        /// </summary>
        private static double[] Differenzlinie(int stand, string szenario, int jahre)
        {
            bool worst = szenario == WirtschaftlichkeitSzenario.WORST;
            bool best = szenario == WirtschaftlichkeitSzenario.BEST;
            double invest = (400000.0 + 150000.0 * stand) * (worst ? 1.1 : best ? 0.9 : 1.0);
            double nutzen = (160000.0 - 12000.0 * stand) * (worst ? 0.9 : best ? 1.1 : 1.0);
            int ersatz = worst ? 13 : best ? 17 : 15;

            var d = new double[jahre + 1];
            d[0] = -invest;
            for (int t = 1; t <= jahre; t++)
            {
                double abzins = Math.Pow(0.97, t);
                d[t] = d[t - 1] + nutzen * abzins - (t == ersatz ? 0.25 * invest * abzins : 0.0);
            }
            return d;
        }

        /// <summary>Die Reihen des Absolutbildes mit der Stammlinie in der gegebenen Strichart.</summary>
        private static List<ChartRenderer.Reihe> MitStammStrichart(List<VerlaufSerie> serien,
                                                                  ChartRenderer.Strichart art)
        {
            List<ChartRenderer.Reihe> reihen = ChartRenderer.VerlaufsReihen(serien, true);
            foreach (ChartRenderer.Reihe r in reihen)
                if (r.Farbe == ChartRenderer.C_STAMM) r.Strichart = art;
            return reihen;
        }
    }
}
