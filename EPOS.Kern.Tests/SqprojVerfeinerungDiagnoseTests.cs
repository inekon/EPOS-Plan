using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Diagnose der Projektdatei-Geometrie an der Anwenderdatei</b> (Übergabepapier „IFC / Gebäudeimport“, Abschnitt 4,
    /// technische Verfeinerungen): Anteil der Raumdreiecke mit Ersatzkennung, Raumpolygone mit mehreren Quellflächen an Boden
    /// oder Decke, Anteil der Bauteilkörper mit angenommener Bezugsebene. Nur Anteile und Zählungen, keine Werte und keine
    /// Namen aus der Datei; fehlt die Datei, schweigt der Fall.
    /// </summary>
    public sealed class SqprojVerfeinerungDiagnoseTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();
        private readonly ITestOutputHelper _aus;

        public SqprojVerfeinerungDiagnoseTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose() => _kultur.Dispose();

        private static string Wurzel([System.Runtime.CompilerServices.CallerFilePath] string eigeneDatei = null)
        {
            foreach (string start in new[] { Path.GetDirectoryName(eigeneDatei), AppContext.BaseDirectory })
            {
                string o = start;
                while (o != null && !File.Exists(Path.Combine(o, "WP-Plan.Kern.slnf")))
                    o = Path.GetDirectoryName(o);
                if (o != null) return o;
            }
            return null;
        }

        internal static GebaeudeAbbild Anwenderdatei()
        {
            string wurzel = Wurzel();
            string pfad = wurzel == null ? null : Path.Combine(wurzel, "Quellen", "Sportheim_1970_unsaniert.sqproj");
            if (pfad == null || !File.Exists(pfad) || new FileInfo(pfad).Length < 10_000) return null;
            var profil = new SqprojImportProfil();
            using FileStream f = File.OpenRead(pfad);
            return profil.LeserErzeugen().Lesen(f, profil, null, CancellationToken.None);
        }

        private static string P(double x) => (100.0 * x).ToString("0.0", CultureInfo.InvariantCulture) + " %";

        private static double Dreiecksflaeche(Dateikoerper k, int t)
        {
            double[] a = k.PunkteM[k.Dreiecke[t][0]], b = k.PunkteM[k.Dreiecke[t][1]], c = k.PunkteM[k.Dreiecke[t][2]];
            double[] n = Polygonnetz.Kreuz(Polygonnetz.Minus(b, a), Polygonnetz.Minus(c, a));
            return Math.Sqrt(Polygonnetz.Punkt(n, n)) / 2.0;
        }

        /// <summary>Die Art einer Ersatzkennung (<c>Mantel</c>, <c>Boden</c>, <c>Decke</c>); <c>null</c> = Kennung eines Bauteils.</summary>
        internal static string Ersatzart(string quelle)
        {
            int i = quelle?.LastIndexOf(':') ?? -1;
            if (i < 0) return null;
            string rest = quelle.Substring(i + 1);
            foreach (string art in new[] { "Mantel", "Boden", "Decke", "Kante" })
                if (rest.StartsWith(art, StringComparison.Ordinal)) return art;
            return "sonst";
        }

        private static double Dicke(Dateikoerper k, double[] n)
            => k.PunkteM.Max(p => Polygonnetz.Punkt(p, n)) - k.PunkteM.Min(p => Polygonnetz.Punkt(p, n));

        /// <summary>Liegt der Punkt (in der Ebene) im Ring? Projektion auf die zwei Achsen mit der größten Spanne der Normale.</summary>
        internal static bool ImRing(IReadOnlyList<double[]> ring, double[] n, double[] q)
        {
            int weg = Math.Abs(n[0]) >= Math.Abs(n[1]) && Math.Abs(n[0]) >= Math.Abs(n[2]) ? 0 : Math.Abs(n[1]) >= Math.Abs(n[2]) ? 1 : 2;
            int i0 = weg == 0 ? 1 : 0, i1 = weg == 2 ? 1 : 2;
            bool innen = false;
            for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
            {
                double[] a = ring[i], b = ring[j];
                if ((a[i1] > q[i1]) != (b[i1] > q[i1]) && q[i0] < (b[i0] - a[i0]) * (q[i1] - a[i1]) / (b[i1] - a[i1]) + a[i0]) innen = !innen;
            }
            return innen;
        }

        [Fact]
        public void Diagnose_Ersatzkennung_Quellflaechen_und_Bezugsebene()
        {
            GebaeudeAbbild a = Anwenderdatei();
            if (a == null) return;
            List<AbbildRaum> raeume = a.Gebaeude.SelectMany(g => g.Raeume).Where(r => r.Koerper != null).ToList();
            int dreiecke = 0;
            double flaeche = 0.0;
            var ersatz = new SortedDictionary<string, (int Zahl, double Flaeche)>(StringComparer.Ordinal);
            foreach (AbbildRaum r in raeume)
                for (int t = 0; t < r.Koerper.Dreiecke.Count; t++)
                {
                    double f = Dreiecksflaeche(r.Koerper, t);
                    dreiecke++;
                    flaeche += f;
                    string art = Ersatzart(r.Koerper.Quellflaechen[t]);
                    if (art == null) continue;
                    ersatz.TryGetValue(art, out var w);
                    ersatz[art] = (w.Zahl + 1, w.Flaeche + f);
                }
            int ersatzZahl = ersatz.Values.Sum(v => v.Zahl);
            _aus.WriteLine("Raumkörper " + raeume.Count + ", Dreiecke " + dreiecke + ", mit Ersatzkennung " + ersatzZahl + " (" + P((double)ersatzZahl / dreiecke)
                           + ", Fläche " + P(ersatz.Values.Sum(v => v.Flaeche) / flaeche) + ")");
            foreach (var g in a.Meldungen.Where(m => m.Schluessel == SqprojGeometrie.KOERPER_RAUM || m.Schluessel == SqprojGeometrie.RAUM_OHNE_POLYGON)
                                  .GroupBy(m => m.Schluessel + " " + m.Werte[1]))
                _aus.WriteLine("  Raumkörper nicht gebildet: " + g.Key + " = " + g.Count());
            foreach (var e in ersatz)
                _aus.WriteLine("  " + e.Key + ": " + e.Value.Zahl + " Dreiecke (" + P((double)e.Value.Zahl / dreiecke) + "), Fläche " + P(e.Value.Flaeche / flaeche));

            // Ursachen: je Ersatzdreieck die nächste Bauteilfläche, parallel, deren Ring den Schwerpunkt trägt.
            List<AbbildBauteil> flaechig = a.Gebaeude.SelectMany(g => g.Bauteile).Concat(a.BauteileOhneGebaeude).Distinct()
                                            .Where(b => b.RandpunkteM != null && b.RandpunkteM.Count >= 3).ToList();
            var ursachen = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (AbbildRaum r in raeume)
                for (int t = 0; t < r.Koerper.Dreiecke.Count; t++)
                {
                    string art = Ersatzart(r.Koerper.Quellflaechen[t]);
                    if (art == null) continue;
                    Dateikoerper k = r.Koerper;
                    double[] c = Polygonnetz.Mal(Polygonnetz.Plus(Polygonnetz.Plus(k.PunkteM[k.Dreiecke[t][0]], k.PunkteM[k.Dreiecke[t][1]]), k.PunkteM[k.Dreiecke[t][2]]), 1.0 / 3.0);
                    double[] tn = k.Normalen[t];
                    string beste = "keine";
                    double besterAbstand = double.MaxValue;
                    foreach (AbbildBauteil b in flaechig)
                    {
                        double[] n = Polygonnetz.Normiert(Polygonnetz.Newell(b.RandpunkteM));
                        if (n == null) continue;
                        double cos = Math.Abs(Polygonnetz.Punkt(n, tn));
                        double abstand = Math.Abs(Polygonnetz.Punkt(Polygonnetz.Minus(c, b.RandpunkteM[0]), n));
                        if (abstand > 1.0) continue;
                        double[] q = Polygonnetz.Minus(c, Polygonnetz.Mal(n, Polygonnetz.Punkt(Polygonnetz.Minus(c, b.RandpunkteM[0]), n)));
                        if (!ImRing(b.RandpunkteM, n, q)) continue;
                        bool eigen = b.Nachbarn.Any(x => string.Equals(x.Kennung, r.Kennung, StringComparison.OrdinalIgnoreCase));
                        string klasse = (eigen ? "eigen" : "fremd") + (cos >= Math.Cos(Math.PI / 180.0) ? ",parallel" : cos >= Math.Cos(Math.PI / 18.0) ? ",bis10Grad" : ",schraeg")
                                        + "," + (b.Koerper == null ? "ohne Körper" : abstand <= Dicke(b.Koerper, n) + 0.02 ? "bis Dicke" : abstand <= Dicke(b.Koerper, n) + 0.32 ? "bis Dicke+30cm" : "weiter")
                                        + "," + b.Art;
                        double rang = abstand + (eigen ? 0 : 10) + (cos >= Math.Cos(Math.PI / 180.0) ? 0 : 5);
                        if (rang < besterAbstand) { besterAbstand = rang; beste = klasse; }
                    }
                    string s = art + ": " + beste;
                    ursachen[s] = ursachen.TryGetValue(s, out int z) ? z + 1 : 1;
                }
            foreach (var u in ursachen) _aus.WriteLine("  Ursache " + u.Key + " = " + u.Value);
            // Punkt 2: höchstens ein Zehntel der Raumdreiecke mit Ersatzkennung (Ausgang 40,7 %), alle Raumkörper gebildet.
            Assert.True(ersatzZahl <= 0.10 * dreiecke, "Ersatzkennung " + P((double)ersatzZahl / dreiecke));
            Assert.DoesNotContain(a.Meldungen, m => m.Schluessel == SqprojGeometrie.KOERPER_RAUM);

            List<AbbildBauteil> alle = a.Gebaeude.SelectMany(g => g.Bauteile).Concat(a.BauteileOhneGebaeude).Distinct().ToList();
            alle = alle.Concat(alle.SelectMany(b => b.Oeffnungen)).Distinct().ToList();
            List<AbbildBauteil> koerper = alle.Where(b => b.Koerper != null).ToList();
            int angenommen = koerper.Count(b => b.Koerper.Vermerke.Contains(Koerpervermerk.Bezugsebene_angenommen));
            _aus.WriteLine("Bauteilkörper " + koerper.Count + ", Bezugsebene angenommen " + angenommen + " (" + P((double)angenommen / Math.Max(1, koerper.Count)) + ")");
            // Punkt 1: Raumpolygone, deren Boden bzw. Decke mehrere eigene, nicht senkrechte Bauteilflächen überdecken.
            var sa = (SqprojGebaeudeAbbild)a;
            int polygone = 0;
            var mehrfach = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (AbbildRaum r in raeume)
            {
                if (!sa.Raumpolygone.TryGetValue(r.Kennung, out IReadOnlyList<Raumpolygon> pgs)) continue;
                List<AbbildBauteil> eigene = flaechig.Where(b => b.Nachbarn.Any(x => string.Equals(x.Kennung, r.Kennung, StringComparison.OrdinalIgnoreCase))).ToList();
                foreach (Raumpolygon pg in pgs)
                {
                    polygone++;
                    List<double[]> ring = pg.Aussen.Punkte.ToList();
                    double xmin = ring.Min(q => q[0]), xmax = ring.Max(q => q[0]), ymin = ring.Min(q => q[1]), ymax = ring.Max(q => q[1]);
                    var stich = new List<double[]>();
                    for (int i = 0; i < 10; i++)
                        for (int j = 0; j < 10; j++)
                        {
                            double[] q = { xmin + (xmax - xmin) * (i + 0.5) / 10.0, ymin + (ymax - ymin) * (j + 0.5) / 10.0 };
                            if (SqprojGeometrie.Innen2D(ring, q)) stich.Add(q);
                        }
                    foreach (bool boden in new[] { true, false })
                    {
                        double hoehe = boden ? pg.Aussen.Boden.Average() : pg.Aussen.Decke.Average();
                        var treffer = new List<AbbildBauteil>();
                        foreach (AbbildBauteil b in eigene)
                        {
                            double[] n = Polygonnetz.Normiert(Polygonnetz.Newell(b.RandpunkteM));
                            if (n == null || Math.Abs(n[2]) < 0.017) continue;
                            double[] o = b.RandpunkteM[0];
                            int zahl = stich.Count(q => SqprojGeometrie.Innen2D(b.RandpunkteM, q)
                                                        && Math.Abs(o[2] - (n[0] * (q[0] - o[0]) + n[1] * (q[1] - o[1])) / n[2] - hoehe) <= 0.8);
                            if (zahl > 0) treffer.Add(b);
                        }
                        string schluessel = (boden ? "Boden" : "Decke") + " mit " + Math.Min(treffer.Count, 3) + (treffer.Count >= 3 ? "+" : "") + " Quellflächen"
                                            + (treffer.Count >= 2 ? ", Arten " + treffer.Select(b => b.Art).Distinct().Count() + ", Randbedingungen "
                                                                    + treffer.Select(b => b.Randbedingung).Distinct().Count() : "");
                        mehrfach[schluessel] = mehrfach.TryGetValue(schluessel, out int z) ? z + 1 : 1;
                    }
                }
            }
            _aus.WriteLine("Raumpolygone " + polygone + ", Teilung zurückgenommen " + sa.DeckelteilungZurueckgenommen + ", Räume mit engem Mantel " + sa.MantelEng + ", ganz eng " + sa.ZuordnungEng);
            foreach (var m in mehrfach) _aus.WriteLine("  " + m.Key + " = " + m.Value);

            var raumKoerper = raeume.ToDictionary(r => r.Kennung, r => r.Koerper, StringComparer.OrdinalIgnoreCase);
            var befunde = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (AbbildBauteil b in koerper.Where(b => b.Koerper.Vermerke.Contains(Koerpervermerk.Bezugsebene_angenommen)))
            {
                double[] n = Polygonnetz.Normiert(Polygonnetz.Newell(b.RandpunkteM));
                double D = Dicke(b.Koerper, n);
                double[] c = b.RandpunkteM.Aggregate(new double[3], Polygonnetz.Plus);
                c = Polygonnetz.Mal(c, 1.0 / b.RandpunkteM.Count);
                var stich = new List<double[]> { c };
                stich.AddRange(b.RandpunkteM.Select(q => Polygonnetz.Mal(Polygonnetz.Plus(q, c), 0.5)));
                var abstaende = new List<(double Weg, int Raum)>();
                int raumZahl = 0;
                foreach (AbbildNachbar nb in b.Nachbarn)
                {
                    if (nb.Kennung == null || !raumKoerper.TryGetValue(nb.Kennung, out Dateikoerper k)) continue;
                    raumZahl++;
                    double best = double.MaxValue, bestWeg = 0;
                    for (int t = 0; t < k.Dreiecke.Count; t++)
                    {
                        double[] tn = k.Normalen[t];
                        double cos = Polygonnetz.Punkt(tn, n);
                        if (Math.Abs(cos) < Math.Cos(Math.PI / 180.0)) continue;
                        double[] p0 = k.PunkteM[k.Dreiecke[t][0]];
                        var tri = k.Dreiecke[t].Select(i => k.PunkteM[i]).ToList();
                        foreach (double[] st in stich)
                        {
                            double weg = Polygonnetz.Punkt(Polygonnetz.Minus(p0, st), tn) / cos;
                            if (Math.Abs(weg) >= best) continue;
                            if (!ImRing(tri, tn, Polygonnetz.Plus(st, Polygonnetz.Mal(n, weg)))) continue;
                            best = Math.Abs(weg);
                            bestWeg = weg;
                        }
                    }
                    if (best < double.MaxValue) abstaende.Add((bestWeg, raumZahl));
                    else
                    {
                        // Ohne Treffer: die nächste parallele Ebene, deren Dreieck mit dem Ring überlappt (Ecke oder Schwerpunkt im Ring).
                        double frei = double.MaxValue;
                        bool ueberlapp = false;
                        for (int t = 0; t < k.Dreiecke.Count; t++)
                        {
                            double[] tn = k.Normalen[t];
                            double cos = Polygonnetz.Punkt(tn, n);
                            if (Math.Abs(cos) < Math.Cos(Math.PI / 180.0)) continue;
                            var tri = k.Dreiecke[t].Select(i => k.PunkteM[i]).ToList();
                            double[] ts = Polygonnetz.Mal(tri.Aggregate(new double[3], Polygonnetz.Plus), 1.0 / 3.0);
                            double weg = Math.Abs(Polygonnetz.Punkt(Polygonnetz.Minus(ts, c), n));
                            bool ue = tri.Concat(new[] { ts }).Any(q => ImRing(b.RandpunkteM, n, q));
                            if (ue && weg < frei) { frei = weg; ueberlapp = true; }
                        }
                        abstaende.Add((ueberlapp ? 1000 + frei : 2000, raumZahl));
                    }
                }
                string text = b.Art + ", Räume mit Körper " + raumZahl + ", "
                              + (abstaende.Any(w => w.Weg < 1000) ? "Stichpunkt trifft, Lage passt zu keinem Fall"
                                 : abstaende.Any(w => w.Weg < 2000) ? "Raumfläche überlappt, Lage passt zu keinem Fall" : "keine Raumfläche überlappt");
                befunde[text] = befunde.TryGetValue(text, out int z) ? z + 1 : 1;
            }
            foreach (var bf in befunde) _aus.WriteLine("  Angenommen " + bf.Key + " = " + bf.Value);
            // Punkt 3: höchstens 3 % der Bauteilkörper mit angenommener Bezugsebene (Ausgang 5,9 %).
            Assert.True(angenommen <= 0.03 * koerper.Count, "Bezugsebene angenommen " + P((double)angenommen / koerper.Count));
            foreach (var g in koerper.Where(b => b.Koerper.Vermerke.Contains(Koerpervermerk.Bezugsebene_angenommen)).GroupBy(b => b.Art).OrderBy(g => g.Key))
                _aus.WriteLine("  " + g.Key + ": " + g.Count() + ", davon Vorgabedicke " + g.Count(b => b.Koerper.Vermerke.Contains(Koerpervermerk.Vorgabedicke)));
        }
    }
}
