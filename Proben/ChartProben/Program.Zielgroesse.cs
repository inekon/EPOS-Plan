using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;

namespace ChartProben
{
    /// <summary>
    /// <b>BV-E5 — DIE BILDGRÖSSE STUFE 2</b> (Konzept Berichtsvorlagen 6.5): Die dreizehn Berichtsbilder
    /// zeichnen im ZIELMASS eines Bildrahmens der Berichtsvorlage (<see cref="Bildmass"/>), statt im festen
    /// Maß gezeichnet und danach skaliert zu werden.
    ///
    /// <para><b>Zwei neue Größen je Bild.</b> Die HALBE Satzspiegelbreite (622 × 400 Bildpunkte des Modells,
    /// im Bericht 311 × 200 px — zwei Bilder nebeneinander) und eine HOHE Form (622 × 800). Bilder, deren Höhe
    /// den Daten folgt (Balken je Stand, Spannenbild), nehmen nur die Breite.</para>
    ///
    /// <para><b>Die Gegenprobe</b> hält fest, was Maß und Farbe nicht sehen: Im Zielmaß bleibt die Schrift der
    /// Legende und der Achsen so groß wie im festen Maß — das Bild ist neu gezeichnet, nicht verkleinert.
    /// Die Bilder im festen Maß stehen unverändert weiter oben; ihre Hashes bleiben.</para>
    /// </summary>
    internal static partial class Program
    {
        /// <summary>Die halbe Satzspiegelbreite der Standardvorlage (9 355 DXA ≈ 623 px) im Modell (×2).</summary>
        private static readonly Bildmass HALB = new Bildmass(622, 400);

        /// <summary>Die hohe Form: halbe Breite, doppelte Höhe.</summary>
        private static readonly Bildmass HOCH = new Bildmass(622, 800);

        /// <summary>Die Proben der Stufe 2 — eine Zeile in <c>Program.cs</c> ruft sie.</summary>
        private static void ZielgroessenProben(string ziel, ZeitreihenSatz z, List<VerlaufSerie> serien)
        {
            var kuchen = new List<ChartRenderer.Segment>
            {
                new ChartRenderer.Segment("Solarthermie", 12.0, ChartRenderer.C_SOLAR),
                new ChartRenderer.Segment("Waermepumpe", 48.0, ChartRenderer.C_WP),
                new ChartRenderer.Segment("BHKW", 26.0, ChartRenderer.C_BHKW),
                new ChartRenderer.Segment("Spitzenkessel", 14.0, ChartRenderer.C_KESSEL)
            };
            var balken = new List<ChartRenderer.Balken>
            {
                new ChartRenderer.Balken("Stamm", 412.0, true),
                new ChartRenderer.Balken("Variante A", 355.0, false),
                new ChartRenderer.Balken("Variante B", 298.0, false),
                new ChartRenderer.Balken("Variante C", 181.0, false)
            };
            var szenTexte = new ChartRenderer.VerlaufSzenarienTexte();
            var spanTexte = new ChartRenderer.SpannenTexte();

            var bilder = new List<(string Name, SKColor[] Farben, bool NurBreite, Func<Bildmass?, Zeichenmodell> Bau)>
            {
                ("kuchen", new[] { ChartRenderer.C_SOLAR, ChartRenderer.C_WP, ChartRenderer.C_BHKW, ChartRenderer.C_KESSEL },
                    false, m => ChartRenderer.KuchenModell("Waermedeckung", kuchen, m)),
                ("balken_horizontal", new[] { ChartRenderer.C_STAMM, ChartRenderer.C_WP },
                    true, m => ChartRenderer.BalkenHorizontalModell("Brennstoffeinsatz", "MWh/a", balken, m)),
                ("jahresverlauf_waerme", new[] { ChartRenderer.C_SOLAR, ChartRenderer.C_WP, ChartRenderer.C_BEDARF },
                    false, m => ChartRenderer.JahresverlaufWaermeModell(z, m)),
                ("dauerlinie_waerme", new[] { ChartRenderer.C_BEDARF, ChartRenderer.C_WP },
                    false, m => ChartRenderer.DauerlinieWaermeModell(z, m)),
                ("strombilanz_monate", new[] { ChartRenderer.C_PV, ChartRenderer.C_BHKW, ChartRenderer.C_BEDARF },
                    false, m => ChartRenderer.StrombilanzMonateModell(z, m)),
                ("speicherverlauf", new[] { ChartRenderer.C_SPEICHER[0], ChartRenderer.C_PV },
                    false, m => ChartRenderer.SpeicherverlaufModell(z, m)),
                ("speichertemperaturen", new[] { ChartRenderer.C_SPEICHER[0], ChartRenderer.C_NETZ },
                    false, m => ChartRenderer.SpeichertemperaturenModell(z, m)),
                ("kapitalwert_absolut", new[] { ChartRenderer.C_STAMM, ChartRenderer.C_SERIEN[0] },
                    false, m => ChartRenderer.KapitalwertVerlaufModell("Kumulierte Barwerte je Projekt",
                                    ChartRenderer.VerlaufsReihen(serien, true), null, m)),
                ("kapitalwert_szenarien", new[] { ChartRenderer.C_SERIEN[0], ChartRenderer.C_SERIEN[1] },
                    false, m => ChartRenderer.KapitalwertSzenarienModell(
                                    "Kumulierter Barwert der Differenz zur Referenz — drei Szenarien",
                                    Szenarieninhalt(3, szenTexte), szenTexte, null, m)),
                ("kapitalwert_spanne", new[] { ChartRenderer.C_RASTER_GUT },
                    true, m => ChartRenderer.KapitalwertSpanneModell(Mockupbalken(), SPANNE_REFERENZ, spanTexte, m)),
                ("kapitalwert_bruecke", new[] { ChartRenderer.C_RASTER_GUT, ChartRenderer.C_RASTER_SCHLECHT, ChartRenderer.C_STAMM },
                    false, m => ChartRenderer.KapitalwertBrueckeModell(Mockupschritte(), Brueckentexte(), m)),
                ("zahlungsstrom", new[] { ChartRenderer.C_STAMM, ChartRenderer.C_RASTER_SCHLECHT },
                    false, m => ChartRenderer.ZahlungsstromModell(Bhkwreihen(), BHKW_ERSATZ, Zahlungsstromtexte(), m)),
            };

            foreach (var b in bilder)
            {
                Zeichenmodell vorgabe = b.Bau(null);
                foreach ((string Zusatz, Bildmass Mass) groesse in new[] { ("halb", HALB), ("hoch", HOCH) })
                {
                    if (b.NurBreite && groesse.Zusatz == "hoch") continue;
                    Bildmass m = groesse.Mass;
                    var bau = b.Bau;
                    // Die Breite ist das Zielmaß — außer beim Brücken-, Spannen- und Szenarienbild, die nicht
                    // schmaler als ihre Mindestbreite zeichnen. Die Höhe ist das Zielmaß oder mehr: Räumt die
                    // Zeichenfläche einer umbrechenden Legende Platz, wächst das Bild (STUFE2_MIN_FLAECHE).
                    int breite = Math.Max(m.Breite, Mindestbreite(b.Name));
                    Zeichenmodell probe = bau(m);
                    int hoehe = b.NurBreite ? vorgabe.Hoehe : Math.Max(m.Hoehe, probe.Hoehe);
                    Pruefe(ziel, "stufe2_" + b.Name + "_" + groesse.Zusatz, breite, hoehe, b.Farben,
                           () => SkiaMaler.Png(bau(m)));
                    SchriftBleibt("stufe2_" + b.Name + "_" + groesse.Zusatz + "_schrift", vorgabe, probe,
                                  b.NurBreite ? 0 : m.Hoehe);
                }
            }
        }

        /// <summary>
        /// <b>Die Gegenprobe der Stufe 2:</b> Die Legenden und Achsen des Bildes im Zielmaß tragen dieselben
        /// Schriftgrößen wie im festen Maß — neu gezeichnet, nicht skaliert. Dazu ist das Bild wirklich
        /// schmaler als das feste Maß.
        /// </summary>
        private static void SchriftBleibt(string name, Zeichenmodell vorgabe, Zeichenmodell ziel, int mindesthoehe)
        {
            _bilder++;
            var maengel = new List<string>();
            if (vorgabe == null || ziel == null) maengel.Add("kein Modell");
            else
            {
                if (ziel.Breite >= vorgabe.Breite) maengel.Add("das Zielmaß ist nicht schmaler: " + ziel.Breite);
                if (ziel.Hoehe < mindesthoehe) maengel.Add("das Bild ist niedriger als das Zielmaß: " + ziel.Hoehe);
                HashSet<float> soll = Schriftgroessen(vorgabe.Befehle);
                HashSet<float> ist = Schriftgroessen(ziel.Befehle);
                if (soll.Count == 0) maengel.Add("das Bild im festen Maß trägt keine Achsen- oder Legendenschrift");
                else if (!ist.SetEquals(soll))
                    maengel.Add("Schriftgrößen [" + string.Join(", ", ist.OrderBy(x => x)) + "] statt [" +
                                string.Join(", ", soll.OrderBy(x => x)) + "]");
            }
            Melde(name + " (wirkt)", ziel == null ? "-" : ziel.Breite + "x" + ziel.Hoehe, "-", "-", "-", maengel);
        }

        /// <summary>Die Mindestbreite der Stufe 2 je Bild (sonst <see cref="Bildmass.MIN_BREITE"/>).</summary>
        private static int Mindestbreite(string name)
        {
            switch (name)
            {
                case "kapitalwert_bruecke": return ChartRenderer.BRUECKE_MIN_BREITE;
                case "kapitalwert_spanne": return ChartRenderer.SPANNE_MIN_BREITE;
                case "kapitalwert_szenarien": return ChartRenderer.SZENARIEN_MIN_BREITE;
                default: return Bildmass.MIN_BREITE;
            }
        }

        /// <summary>Die Schriftgrößen der Texte außer dem Titel (der im Zielmaß einpassen darf).</summary>
        private static HashSet<float> Schriftgroessen(IReadOnlyList<Zeichenbefehl> befehle)
        {
            var groessen = new HashSet<float>();
            foreach (Zeichenbefehl b in befehle)
            {
                if (b is Gruppe g) groessen.UnionWith(Schriftgroessen(g.Befehle));
                else if (b is Text t && t.Marke != "titel") groessen.Add(t.Schrift.Punkt);
            }
            return groessen;
        }
    }
}
