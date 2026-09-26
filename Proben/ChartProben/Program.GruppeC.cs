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
    /// <b>AUFTRAG DG-E3c — die sieben Bilder OHNE ZEITACHSE.</b> Kuchen, horizontale
    /// Balken, Strombilanz im Monatsverlauf, Monatssäulen, Monatsstapel, Ring und die
    /// Rasterkarte der Auslegungsoptimierung gehen hier durch den zweiten Ausgabeweg.
    ///
    /// <para><b>Was diese Gegenprobe von der der Gruppe (a) unterscheidet.</b> Dort
    /// war das innere <c>&lt;svg&gt;</c> der Prüfgegenstand; hier ist sein FEHLEN es.
    /// Diese sieben Bilder sind reine Pixelbilder (Entscheid DG-E3-7): keine
    /// Zeichenfläche, keine <c>Datenreihe</c>, kein Zoom. Geprüft wird deshalb, dass
    /// jedes Datenelement seine Marke UND seinen fertig formatierten Wert trägt
    /// (DG-E3-6) — die Zahl, die die Oberfläche beim Zeigen darauf anzeigt —, dass
    /// jeder Legendeneintrag denselben Schlüssel führt wie die Elemente, die er
    /// schaltet, und dass zweimal Schreiben byte-gleich ist.</para>
    ///
    /// <para><b>Dieselben Gaben wie die PNG-Proben.</b> Jedes Modell hier entsteht aus
    /// den Zahlen, aus denen die gleichnamige Probe in <c>Program.cs</c> ihr Bild
    /// zieht — nur so ist die Sichtprüfung <c>--svg-alle</c> gegen <c>--ablage</c>
    /// ein Vergleich und nicht ein Nebeneinander zweier Bilder.</para>
    ///
    /// <para>OHNE ABLAGE UND OHNE MESSLATTE: Hier entsteht kein PNG; die eingefrorene
    /// Hashliste misst den PNG-Weg und bekommt hier keine Zeile.</para>
    /// </summary>
    internal static partial class Program
    {
        /// <summary>Der Trenner, den der Renderer zwischen zwei Teilen einer Kennung setzt.</summary>
        private const string C_TRENNER = " · ";

        /// <summary>
        /// Die sieben Modelle der Gruppe (c) unter den Namen ihrer PNG-Proben — die
        /// Reihenfolge ist die von <c>Program.cs</c>.
        /// </summary>
        private static List<KeyValuePair<string, Func<Zeichenmodell>>> GruppeCBilder()
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

            ZeitreihenSatz satz = SyntheticherSatz();
            double[] monatswerte = Monatsreihe();

            double[] cRaten = { 0.5, 1.0, 1.5, 2.0, 2.5, 3.0 };
            var kapazitaeten = new double[10];
            for (int i = 0; i < 10; i++) kapazitaeten[i] = 500.0 + i * 500.0;
            double[][] rasterWerte = Rasterfeld(kapazitaeten, cRaten);

            return new List<KeyValuePair<string, Func<Zeichenmodell>>>
            {
                Modellprobe("kuchen",
                    () => ChartRenderer.KuchenModell("Waermedeckung", kuchen)),
                Modellprobe("balken_horizontal",
                    () => ChartRenderer.BalkenHorizontalModell("Brennstoffeinsatz", "MWh/a", balken)),
                Modellprobe("strombilanz_monate",
                    () => ChartRenderer.StrombilanzMonateModell(satz)),
                Modellprobe("monatssaeulen",
                    () => ChartRenderer.MonatsSaeulenModell("Strombedarf Monatsuebersicht",
                            monatswerte, SKColors.YellowGreen, "MWh")),
                Modellprobe("monatsstapel_drei_reihen",
                    () => ChartRenderer.MonatsStapelModell("Energie-Bedarf & Deckung", "kWh",
                            new List<ChartRenderer.Reihe>
                            {
                                new ChartRenderer.Reihe("Eigenverbrauch (Direkt)",
                                                        Monatsreihe(), SKColors.Gold),
                                new ChartRenderer.Reihe("Eigenverbrauch (Speicher)",
                                                        Monatsreihe(0.4), SKColors.LightGreen),
                                new ChartRenderer.Reihe("Autarkie-Luecke (Netz)",
                                                        Monatsreihe(0.9), SKColors.Red)
                            })),
                // Die Ueberladung OHNE Unterzeile und MIT Legende - sie geht auf
                // dieselbe RingModell-Fassung wie die vierteilige.
                Modellprobe("ring_waermedeckung",
                    () => ChartRenderer.RingModell("Waermedeckung",
                            new List<ChartRenderer.Ringsegment>
                            {
                                new ChartRenderer.Ringsegment("Waermepumpe", 340, RING_WP),
                                new ChartRenderer.Ringsegment("Solarthermie", 90, RING_SOLAR),
                                new ChartRenderer.Ringsegment("Heizstab", 45, RING_HEIZSTAB),
                                new ChartRenderer.Ringsegment("Spitzenkessel", 120, RING_KESSEL),
                                new ChartRenderer.Ringsegment("Rest", 60, RING_REST)
                            },
                            89.4, "%")),
                Modellprobe("optimierungsraster",
                    () => ChartRenderer.OptimierungsrasterModell("Jahresüberschuss ΔJ [€/a]",
                            "C-Rate [1/h]", "Kapazität [kWh]", "ΔJ [€/a]",
                            cRaten, kapazitaeten, rasterWerte,
                            RASTER_BESTE_ZEILE, RASTER_BESTE_SPALTE))
            };
        }

        /// <summary>
        /// Die Gegenproben der Gruppe (c) und — mit <c>--svg-alle</c> — ihre Dateien.
        /// Das ist die EINE Zeile, die <c>Program.cs</c> ruft.
        /// </summary>
        private static void GruppeCProben()
        {
            List<KeyValuePair<string, Func<Zeichenmodell>>> bilder = GruppeCBilder();

            foreach (KeyValuePair<string, Func<Zeichenmodell>> b in bilder)
                SvgPixelbildprobe(b.Key, b.Value);

            // Der Zellwert der Rasterkarte im Wortlaut: Er setzt sich aus BEIDEN
            // Achsenbeschriftungen und der Einheit der Farbskala zusammen, und das
            // sieht kein Aufbau-Test.
            SvgProbe("svg_c_rasterzelle_nennt_beide_achsen", e =>
            {
                Zeichenmodell m = bilder.First(b => b.Key == "optimierungsraster").Value();
                e.Masse = m.Breite + "x" + m.Hoehe;

                SvgKnoten baum = SvgSchreiber.Baum(m);
                List<string> werte = Reihenwerte(baum);
                e.Knoten = werte.Count.ToString(CultureInfo.InvariantCulture);
                if (werte.Count == 0) { e.Maengel.Add("keine Rasterzelle mit Wert"); return; }

                // 6 C-Raten x 10 Kapazitaeten = 60 Zellen; keine ist schraffiert.
                if (werte.Count != 60)
                    e.Maengel.Add("Zellen mit Wert: " + werte.Count + " statt 60");

                // Die erste Zelle ist die linke UNTERE: 500 kWh bei 0,5 C.
                string erste = werte[0];
                foreach (string teil in new[] { "Kapazität 500 kWh", C_TRENNER, "C-Rate 0,5 1/h", ": " })
                    if (!erste.Contains(teil, StringComparison.Ordinal))
                        e.Maengel.Add("der Zellwert nennt " + teil + " nicht: " + erste);

                // Die Zahl traegt die Einheit der FARBSKALA, nicht die einer Achse.
                foreach (string w in werte)
                    if (!w.EndsWith(" €/a", StringComparison.Ordinal))
                        e.Maengel.Add("Zellwert ohne die Einheit der Skala: " + w);

                // Die Bestmarke und die Farbskala sind eigene Marken - und die
                // Bestmarke nennt DIESELBE Zelle wie das Raster darunter.
                List<SvgKnoten> alle = baum.Alle().ToList();
                if (alle.Count(k => Attributwert(k, "data-marke") == "marke") != 1)
                    e.Maengel.Add("die Bestmarke steht nicht genau einmal im Baum");
                if (!alle.Any(k => Attributwert(k, "data-marke") == "skala"))
                    e.Maengel.Add("die Farbskala traegt die Marke skala nicht");

                string beste = alle.Where(k => Attributwert(k, "data-marke") == "marke")
                                   .Select(k => Attributwert(k, "data-wert"))
                                   .FirstOrDefault();
                if (beste == null || !werte.Contains(beste, StringComparer.Ordinal))
                    e.Maengel.Add("die Bestmarke nennt keine Zelle des Rasters: " +
                                  (beste ?? "ohne Wert"));
            });

            // Der Wert einer Monatssaeule nennt seinen Monat und seine Einheit - und
            // zwar mit den Nachkommastellen der eigenen y-Achse.
            SvgProbe("svg_c_monatswert_nennt_monat_und_einheit", e =>
            {
                Zeichenmodell m = bilder.First(b => b.Key == "monatssaeulen").Value();
                e.Masse = m.Breite + "x" + m.Hoehe;

                List<string> werte = Reihenwerte(SvgSchreiber.Baum(m));
                e.Knoten = werte.Count.ToString(CultureInfo.InvariantCulture);

                // Elf Saeulen: der Nullmonat der synthetischen Reihe wird nicht gezeichnet.
                if (werte.Count != 11)
                    e.Maengel.Add("Saeulen mit Wert: " + werte.Count + " statt 11");
                if (!werte.Any(w => w.StartsWith("Jan: ", StringComparison.Ordinal)))
                    e.Maengel.Add("keine Saeule nennt den Januar: " + Erste(werte));
                foreach (string w in werte)
                    if (!w.EndsWith(" MWh", StringComparison.Ordinal))
                        e.Maengel.Add("Saeulenwert ohne Einheit: " + w);
            });

            // Kuchen und Ring zeigen ANTEILE - dieselbe Stufung wie im Bildtext.
            SvgProbe("svg_c_anteil_steht_in_prozent", e =>
            {
                foreach (string name in new[] { "kuchen", "ring_waermedeckung" })
                {
                    Zeichenmodell m = bilder.First(b => b.Key == name).Value();
                    List<string> werte = Reihenwerte(SvgSchreiber.Baum(m));
                    e.Masse = "Anteile";
                    e.Knoten = werte.Count.ToString(CultureInfo.InvariantCulture);

                    foreach (string w in werte)
                        if (!w.EndsWith(" %", StringComparison.Ordinal))
                            e.Maengel.Add(name + ": Segment ohne Prozentwert: " + w);
                }

                // Der Kuchen der PNG-Probe traegt 48 von 100 - woertlich der Satz aus
                // dem Entscheid DG-E3-6.
                Zeichenmodell k = bilder.First(b => b.Key == "kuchen").Value();
                if (!Reihenwerte(SvgSchreiber.Baum(k))
                        .Any(w => w == "Waermepumpe: 48,0 %"))
                    e.Maengel.Add("der Kuchen nennt Waermepumpe: 48,0 % nicht");
            });

            if (_svgordner != null) SvgOrdnerSchreiben(bilder);
        }

        /// <summary>
        /// <b>Die SVG-Gegenprobe EINES reinen Pixelbildes</b> (Auftrag DG-E3c).
        ///
        /// <list type="number">
        ///   <item>Zweimal geschrieben UND zweimal erzeugt ist byte-gleich.</item>
        ///   <item>Das Modell führt KEINE Zeichenfläche und KEINE Datenreihe, und im
        ///   Baum steht deshalb weder ein inneres <c>&lt;svg&gt;</c> noch ein
        ///   <c>path.epos-reihe</c> (DG-E3-7).</item>
        ///   <item>Jedes Datenelement — jeder Knoten mit <c>data-marke="reihe:…"</c> —
        ///   trägt einen nicht leeren <c>data-wert</c> (DG-E3-6), und kein Knoten
        ///   trägt einen Wert ohne Marke.</item>
        ///   <item>Der Bildtitel steht als <c>titel</c> im Baum.</item>
        ///   <item>Jeder Legendeneintrag <c>legende:&lt;N&gt;</c> schaltet Elemente,
        ///   die es gibt: Es gibt mindestens ein <c>reihe:&lt;N&gt;</c> dazu.</item>
        /// </list>
        /// </summary>
        private static void SvgPixelbildprobe(string name, Func<Zeichenmodell> bau)
        {
            SvgProbe("svg_c_" + name, e =>
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

                // (2) DG-E3-7: ein reines Pixelbild - kein Zoom, keine Datenreihe.
                if (m.Flaeche != null) e.Maengel.Add("das Modell fuehrt eine Zeichenflaeche");
                if (m.Reihen.Count != 0)
                    e.Maengel.Add("das Modell fuehrt " + m.Reihen.Count + " Datenreihen");
                if (alle.Any(k => k.Name == "svg" && Attributwert(k, "class") == "epos-flaeche"))
                    e.Maengel.Add("es gibt ein inneres svg");
                if (alle.Any(k => Attributwert(k, "class") == "epos-reihe"))
                    e.Maengel.Add("es gibt einen Reihenpfad in Datenkoordinaten");

                // (3) Marke UND Wert an jedem Datenelement.
                var elemente = alle.Where(k => Istreihe(Attributwert(k, "data-marke"))).ToList();
                if (elemente.Count == 0) { e.Maengel.Add("kein Datenelement mit reihe:-Marke"); return; }

                foreach (SvgKnoten k in elemente)
                    if (string.IsNullOrEmpty(Attributwert(k, "data-wert")))
                        e.Maengel.Add("Datenelement ohne data-wert: " +
                                      Attributwert(k, "data-marke") + " (" + k.Name + ")");

                foreach (SvgKnoten k in alle)
                    if (Attributwert(k, "data-wert") != null &&
                        Attributwert(k, "data-marke") == null)
                        e.Maengel.Add("data-wert ohne data-marke: " + k.Name);

                // (4) Der Titel ist markiert.
                if (!alle.Any(k => Attributwert(k, "data-marke") == "titel"))
                    e.Maengel.Add("kein Element mit der Marke titel");

                // (5) Die Legende schaltet Elemente, die es gibt.
                var reihen = new HashSet<string>(
                    elemente.Select(k => Attributwert(k, "data-marke").Substring("reihe:".Length)),
                    StringComparer.Ordinal);
                foreach (string legende in alle
                             .Select(k => Attributwert(k, "data-marke"))
                             .Where(mk => mk != null &&
                                          mk.StartsWith("legende:", StringComparison.Ordinal))
                             .Select(mk => mk.Substring("legende:".Length))
                             .Distinct(StringComparer.Ordinal))
                    if (!reihen.Contains(legende))
                        e.Maengel.Add("Legendeneintrag ohne Elemente: " + legende);
            });
        }

        /// <summary>Gehört die Marke zu einem Datenelement?</summary>
        private static bool Istreihe(string marke)
            => marke != null && marke.StartsWith("reihe:", StringComparison.Ordinal);

        /// <summary>
        /// Die <c>data-wert</c>-Texte aller Datenelemente eines Baums, in
        /// Zeichenreihenfolge und ohne Wiederholung. Eine Zeile des horizontalen
        /// Balkens besteht aus vier Befehlen mit DEMSELBEN Wert; gezählt werden
        /// Elemente, nicht Befehle.
        /// </summary>
        private static List<string> Reihenwerte(SvgKnoten baum)
        {
            var werte = new List<string>();
            string letzte = null;
            foreach (SvgKnoten k in baum.Alle())
            {
                string marke = Attributwert(k, "data-marke");
                string wert = Attributwert(k, "data-wert");
                if (!Istreihe(marke) || string.IsNullOrEmpty(wert)) continue;
                if (string.Equals(wert, letzte, StringComparison.Ordinal)) continue;
                werte.Add(wert);
                letzte = wert;
            }
            return werte;
        }

        /// <summary>Der erste Wert einer Liste — für eine Mängelmeldung.</summary>
        private static string Erste(List<string> werte)
            => werte.Count == 0 ? "(keiner)" : werte[0];
    }
}
