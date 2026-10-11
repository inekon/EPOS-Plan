using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die kleine Kurve des Kesseleditors</b> (Konzept Kesselkennlinie 5, erster Punkt).
    ///
    /// <para><b>Die Punkte</b> (<see cref="Kesselkennlinie.Kurven"/>) kommen aus denselben Funktionen wie der Lauf: je
    /// Stützstelle genau <see cref="Kesselkennlinie.Eta"/> bzw. <see cref="Kesselkennlinie.EtaBrennwert"/> mit dem
    /// Nennwirkungsgrad nach Brennstoff und dem wirksamen η₃₀ — gepflegt, als Prozentangabe oder als Normvorgabe (7.1).
    /// Mit Brennwertkennlinie drei Kurven bei 30, 50 und 60 °C Rücklauf, die die Prüfpunkte des Katalogs treffen; ohne
    /// sie eine; der Elektrokessel flach.</para>
    ///
    /// <para><b>Das Bild</b> (<see cref="ChartRenderer.KesselkennlinieModell"/>): ein Pixelbild ohne Zeichenfläche mit
    /// einer Datenreihe je Linie (x = Last in Prozent), Werten an den Punktmarken, Legende je Linie; nicht endliche Werte
    /// fallen weg, ohne Linie steht der Leerhinweis.</para>
    /// </summary>
    public sealed class KesselkennlinieKurveTests : IDisposable
    {
        private const int ERDGAS = 3;
        private const int HEIZOEL = 9;
        private const int STROM = 13;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static IReadOnlyList<Kesselkurve> Brennwertkessel(double? eta30, bool kennlinie = true)
            => Kesselkennlinie.Kurven(0.97, 0.0, ERDGAS, true, "Brennwertkessel", eta30, kennlinie);

        private static double Bei(Kesselkurve k, double laststufe)
            => k.Punkte.Single(p => Math.Abs(p.Laststufe - laststufe) < 1e-12).Wirkungsgrad;

        // =================================================================
        //  Die Punkte
        // =================================================================

        [Fact]
        public void Mit_Brennwertkennlinie_drei_Kurven_bei_30_50_und_60_Grad()
        {
            IReadOnlyList<Kesselkurve> kurven = Brennwertkessel(1.07);

            Assert.Equal(new double?[] { 30, 50, 60 }, kurven.Select(k => k.RuecklaufC).ToArray());
            foreach (Kesselkurve k in kurven)
            {
                Assert.Equal(Kesselkennlinie.KURVE_STUETZSTELLEN, k.Punkte.Count);
                Assert.Equal(0.1, k.Punkte[0].Laststufe, 12);
                Assert.Equal(1.0, k.Punkte[^1].Laststufe);
            }
        }

        [Fact]
        public void Die_Brennwertkurven_treffen_die_Pruefpunkte_des_Katalogs()
        {
            IReadOnlyList<Kesselkurve> kurven = Brennwertkessel(1.07);

            Assert.Equal(1.07, Bei(kurven[0], 0.3), 12);   // 30 % Last, 30 °C Rücklauf → η₃₀
            Assert.Equal(0.97, Bei(kurven[2], 1.0), 12);   // Nennlast, über dem Taupunkt → η₁₀₀
            // Unter 30 % Last flach, darüber fällt die Kurve; der wärmere Rücklauf liegt tiefer.
            Assert.Equal(Bei(kurven[0], 0.3), Bei(kurven[0], 0.1));
            Assert.True(Bei(kurven[0], 1.0) < Bei(kurven[0], 0.3));
            Assert.True(Bei(kurven[1], 0.5) < Bei(kurven[0], 0.5));
            Assert.True(Bei(kurven[2], 0.5) < Bei(kurven[1], 0.5));
        }

        [Fact]
        public void Jeder_Punkt_ist_der_Wirkungsgrad_der_Laufstunde()
        {
            IReadOnlyList<Kesselkurve> brennwert = Brennwertkessel(1.07);
            foreach (Kesselkurve k in brennwert)
                foreach (Kesselkurvenpunkt p in k.Punkte)
                    Assert.Equal(Kesselkennlinie.EtaBrennwert(p.Laststufe, 0.97, 1.07, k.RuecklaufC!.Value, ERDGAS, out _),
                                 p.Wirkungsgrad);

            Kesselkurve teillast = Assert.Single(Brennwertkessel(1.07, kennlinie: false));
            Assert.Null(teillast.RuecklaufC);
            foreach (Kesselkurvenpunkt p in teillast.Punkte)
                Assert.Equal(Kesselkennlinie.Eta(p.Laststufe, 0.97, 1.07), p.Wirkungsgrad);
        }

        [Fact]
        public void Ohne_Brennwertkessel_wirkt_der_Schalter_Brennwertkennlinie_nicht()
        {
            // Der Controller schreibt den Schalter nur beim Brennwertkessel; die Kurve folgt dem Lauf.
            Kesselkurve k = Assert.Single(Kesselkennlinie.Kurven(0.92, 0.0, ERDGAS, false, "", null, true));
            Assert.Null(k.RuecklaufC);
            Assert.All(k.Punkte, p => Assert.Equal(0.92, p.Wirkungsgrad));   // Niedertemperaturkessel: flach
        }

        [Fact]
        public void Ein_leeres_Eta30_nimmt_die_Normvorgabe_nach_Bauart()
        {
            Kesselkurve brennwert = Assert.Single(Brennwertkessel(null, kennlinie: false));
            Assert.Equal(0.97 + Kesselkennlinie.VORGABE_ZUSCHLAG_BRENNWERT, Bei(brennwert, 0.3), 12);

            Kesselkurve standard = Assert.Single(Kesselkennlinie.Kurven(0.90, 0.0, ERDGAS, false, "Standardkessel", null, false));
            Assert.Equal(0.90 - Kesselkennlinie.VORGABE_ABSCHLAG_STANDARD, Bei(standard, 0.3), 12);
            Assert.Equal(0.90, Bei(standard, 1.0), 12);
        }

        [Fact]
        public void Prozentangaben_werden_wie_im_Lauf_als_Faktor_gelesen()
        {
            IReadOnlyList<Kesselkurve> prozent = Kesselkennlinie.Kurven(97, 0, ERDGAS, true, "", 107, true);
            IReadOnlyList<Kesselkurve> faktor = Brennwertkessel(1.07);
            for (int i = 0; i < faktor.Count; i++)
                for (int j = 0; j < faktor[i].Punkte.Count; j++)
                    Assert.Equal(faktor[i].Punkte[j].Wirkungsgrad, prozent[i].Punkte[j].Wirkungsgrad, 12);
            Assert.Equal(0.97, Kesselkennlinie.WirkungsgradAlsFaktor(97), 12);
            Assert.Equal(1.2, Kesselkennlinie.WirkungsgradAlsFaktor(1.2));
        }

        [Fact]
        public void Heizoel_nimmt_das_Oelfeld_und_ein_fehlender_Wert_den_Rueckfall()
        {
            Kesselkurve oel = Assert.Single(Kesselkennlinie.Kurven(0.99, 0.93, HEIZOEL, false, "", null, false));
            Assert.All(oel.Punkte, p => Assert.Equal(0.93, p.Wirkungsgrad));

            Kesselkurve ohne = Assert.Single(Kesselkennlinie.Kurven(0.0, 0.0, ERDGAS, false, "", null, false));
            Assert.All(ohne.Punkte, p => Assert.Equal(Kesselkennlinie.NENNWIRKUNGSGRAD_RUECKFALL, p.Wirkungsgrad));
            Assert.Equal(0.90, Kesselkennlinie.Nennwirkungsgrad(-1, 0.95, ERDGAS));
            Assert.Equal(0.95, Kesselkennlinie.Nennwirkungsgrad(-1, 0.95, HEIZOEL));
        }

        [Fact]
        public void Der_Elektrokessel_rechnet_flach_ohne_Kennlinie()
        {
            Kesselkurve k = Assert.Single(Kesselkennlinie.Kurven(0.99, 0.0, STROM, true, "", 1.07, true));
            Assert.Null(k.RuecklaufC);
            Assert.All(k.Punkte, p => Assert.Equal(0.99, p.Wirkungsgrad));
        }

        // =================================================================
        //  Das Bild
        // =================================================================

        private static List<ChartRenderer.KesselkennlinienReihe> Reihen(IReadOnlyList<Kesselkurve> kurven)
            => kurven.Select(k => new ChartRenderer.KesselkennlinienReihe(
                   k.RuecklaufC.HasValue ? "Rücklauf " + k.RuecklaufC.Value + " °C" : "Teillastkennlinie", k.Punkte)).ToList();

        private static List<string> Werte(Zeichenmodell m)
            => Alle(m.Befehle).Where(b => b.Wert != null).Select(b => b.Wert).ToList();

        private static IEnumerable<Zeichenbefehl> Alle(IEnumerable<Zeichenbefehl> befehle)
        {
            foreach (Zeichenbefehl b in befehle)
            {
                yield return b;
                if (b is Gruppe g)
                    foreach (Zeichenbefehl k in Alle(g.Befehle)) yield return k;
            }
        }

        [Fact]
        public void Das_Bild_fuehrt_je_Linie_eine_Datenreihe_mit_der_Last_als_x_Stelle()
        {
            Zeichenmodell m = ChartRenderer.KesselkennlinieModell("Wirkungsgrad über der Last", "Last [%]", "Wirkungsgrad",
                                                                  Reihen(Brennwertkessel(1.07)));

            Assert.Equal(ChartRenderer.KESSELKENNLINIE_BREITE, m.Breite);
            Assert.Equal(ChartRenderer.KESSELKENNLINIE_HOEHE, m.Hoehe);
            Assert.Null(m.Flaeche);   // ohne Zoom (DG-E3-7)
            Assert.Equal(new[] { "Rücklauf 30 °C", "Rücklauf 50 °C", "Rücklauf 60 °C" }, m.Reihen.Select(r => r.Name));
            foreach (Datenreihe r in m.Reihen)
            {
                Assert.Equal(new double[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 }, r.XWerte.Select(x => Math.Round(x, 9)));
                Assert.Equal(Kesselkennlinie.KURVE_STUETZSTELLEN, r.Werte.Length);
            }
            Assert.Equal(new[] { Farbrolle.SERIE_1, Farbrolle.SERIE_2, Farbrolle.SERIE_3 }, m.Reihen.Select(r => r.Ton.Rolle));
            foreach (string name in m.Reihen.Select(r => r.Name))
                Assert.Contains(m.Befehle, b => b.Marke == "legende:" + name);
        }

        [Fact]
        public void Jede_Punktmarke_nennt_Last_und_Wirkungsgrad()
        {
            Zeichenmodell m = ChartRenderer.KesselkennlinieModell("Wirkungsgrad über der Last", "Last [%]", "Wirkungsgrad",
                                                                  Reihen(Brennwertkessel(1.07)));
            List<string> werte = Werte(m);

            Assert.Contains("Last 30 % · Rücklauf 30 °C: 1,070", werte);
            Assert.Contains("Last 100 % · Rücklauf 60 °C: 0,970", werte);
        }

        [Fact]
        public void Nicht_endliche_Werte_fallen_weg_und_ohne_Linie_steht_der_Leerhinweis()
        {
            var punkte = new[]
            {
                new Kesselkurvenpunkt(0.1, double.NaN), new Kesselkurvenpunkt(0.5, 0.95), new Kesselkurvenpunkt(1.0, 0.93),
            };
            Zeichenmodell m = ChartRenderer.KesselkennlinieModell("T", "Last [%]", "Wirkungsgrad",
                new[] { new ChartRenderer.KesselkennlinienReihe("A", punkte),
                        new ChartRenderer.KesselkennlinienReihe("B", new[] { new Kesselkurvenpunkt(0.5, double.PositiveInfinity) }) });
            Datenreihe a = Assert.Single(m.Reihen);
            Assert.Equal(2, a.Werte.Length);

            Zeichenmodell leer = ChartRenderer.KesselkennlinieModell("T", "Last [%]", "Wirkungsgrad", null);
            Assert.Empty(leer.Reihen);
            Assert.Contains(leer.Befehle, b => b.Marke == "leerhinweis");
        }
    }
}
