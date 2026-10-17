using System;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Modell des Bivalenzdiagramms</b> (Fachkonzept Übergabegrenze 7.2; Umsetzungskonzept UB‑E4‑a) — ohne
    /// Datenbank, am Zahlenbeispiel: Heizkörper 75/60/20 °C, n = 1,3, 10 kW; Gebäude 10 kW bei −12 °C; Kennfeld bei
    /// 55 °C 7 kW bei −7 °C bis 9 kW bei +7 °C; σ_min 3 K; teilparallel, Abschaltpunkt −10 °C, Kessel 10 kW. Das Bild
    /// selbst halten die ChartProben (Maße, Farben, Determinismus, Gegenproben, Messlatte).
    /// </summary>
    public class BivalenzdiagrammTests
    {
        private const double TOL_PUNKT_K = 0.05;

        private static Bivalenzherleitung Herleitung(bool vorwaermbetrieb = true, double phiN = 10.0,
                                                     Bivalenzbetriebsart art = Bivalenzbetriebsart.Teilparallel,
                                                     double? abschaltpunkt = -10.0, bool mitUebergabe = true)
            => Bivalenzherleitung.Rechnen(new BivalenzGebaeudedaten
            {
                Gebaeude = mitUebergabe ? new Uebergabezone(phiN, 75.0, 60.0, 20.0, 1.3) : null,
                HeizlastN = 10.0,
                AuslegungAussenC = -12.0,
                AuslegungRaumC = 20.0,
            }, new BivalenzGeraetedaten
            {
                HoechstvorlaufC = 55.0,
                Kennfeld = new[] { new Kennfeldpunkt(-7.0, 7.0), new Kennfeldpunkt(7.0, 9.0) },
                SpreizungMinK = 3.0,
                Betriebsart = art,
                AbschaltpunktC = abschaltpunkt,
                Vorwaermbetrieb = vorwaermbetrieb,
                Kesselleistung = 10.0,
                Einbindung = "DIREKT",
            });

        [Fact]
        public void Reihen_und_Achsen_des_Zahlenbeispiels()
        {
            BivalenzdiagrammModell m = BivalenzdiagrammModell.Aus(Herleitung());
            Assert.True(m.MitUebergabe);
            Assert.Equal(55.0, m.HoechstvorlaufC);
            Assert.Equal(33, m.Heizlast.Count);                     // −12 … +20 °C in 1 K
            Assert.Equal(new BivalenzKurvenpunkt(-12.0, 10.0), m.Heizlast[0]);
            Assert.Equal(0.0, m.Heizlast[32].Leistung, 12);
            Assert.Equal(7.0, m.Kennfeld[0].Leistung, 12);
            Assert.Equal(9.0, m.Kennfeld.Single(p => p.AussenC == 7.0).Leistung, 12);
            Assert.Equal(5.680, m.Uebergabegrenze, 3);

            // Achsen: x −15 … 20 in 5 K (Spanne 32 K / 8 → Stufe 5), y 0 … 12,5 in 2,5 (10 kW · 1,08 / 5 → Stufe 2,5).
            Assert.Equal(-15.0, m.XVon);
            Assert.Equal(20.0, m.XBis);
            Assert.Equal(5.0, m.XSchritt);
            Assert.Equal(12.5, m.YBis);
            Assert.Equal(2.5, m.YSchritt);
        }

        [Fact]
        public void Flaechen_und_Marken_mit_Vorwaermbetrieb()
        {
            BivalenzdiagrammModell m = BivalenzdiagrammModell.Aus(Herleitung());
            Assert.Equal(new[] { BivalenzFlaechenart.NurKessel, BivalenzFlaechenart.Vorwaermung,
                                 BivalenzFlaechenart.WaermepumpeAllein },
                         m.Flaechen.Select(f => f.Art).ToArray());
            Assert.Equal(-15.0, m.Flaechen[0].VonC);
            Assert.Equal(-3.55, m.Flaechen[0].BisC, TOL_PUNKT_K);
            Assert.Equal(m.Flaechen[0].BisC, m.Flaechen[1].VonC);
            Assert.Equal(1.83, m.Flaechen[1].BisC, TOL_PUNKT_K);
            Assert.Equal(20.0, m.Flaechen[2].BisC);

            Assert.Equal(new[] { BivalenzMarkenart.ErsterBivalenzpunkt, BivalenzMarkenart.ZweiterBivalenzpunkt,
                                 BivalenzMarkenart.Abschaltpunkt, BivalenzMarkenart.NachKennfeld },
                         m.Marken.Select(k => k.Art).ToArray());
            Assert.Equal(1.83, m.Marken[0].AussenC, TOL_PUNKT_K);
            Assert.Equal(-3.55, m.Marken[1].AussenC, TOL_PUNKT_K);
            Assert.Equal(-10.0, m.Marken[2].AussenC);
            Assert.Equal(-3.84, m.Marken[3].AussenC, TOL_PUNKT_K);
            // Die Flächengrenzen stehen auf den Marken (Bisektion bis weit unter 1e-6 K).
            Assert.Equal(m.Marken[1].AussenC, m.Flaechen[0].BisC, 6);
            Assert.Equal(m.Marken[0].AussenC, m.Flaechen[1].BisC, 6);
        }

        [Fact]
        public void Leistung_der_Waermepumpe_nach_Betriebsbereich()
        {
            BivalenzdiagrammModell m = BivalenzdiagrammModell.Aus(Herleitung());
            var wp = m.Waermepumpe;
            for (int i = 1; i < wp.Count; i++) Assert.True(wp[i].AussenC >= wp[i - 1].AussenC);

            // Unter θ_biv,2 nur der Kessel; über θ_biv,1 die Heizlast; an θ_biv,1 die Übergabegrenze.
            Assert.All(wp.Where(p => p.AussenC < -3.6), p => Assert.Equal(0.0, p.Leistung));
            Assert.All(wp.Where(p => p.AussenC > 1.9), p => Assert.Equal(10.0 * (20.0 - p.AussenC) / 32.0, p.Leistung, 9));
            Assert.Equal(5.680, wp.Where(p => Math.Abs(p.AussenC - m.Marken[0].AussenC) < 1e-6).Max(p => p.Leistung), 3);

            // An θ_biv,2 springt die Vorwärmung von W_H·σ_min = 10/15 · 3 = 2 kW auf null - senkrecht.
            var sprung = wp.Where(p => Math.Abs(p.AussenC - m.Marken[1].AussenC) < 1e-6).ToArray();
            Assert.Equal(2, sprung.Length);
            Assert.Equal(0.0, sprung.Min(p => p.Leistung));
            Assert.Equal(2.0, sprung.Max(p => p.Leistung), 6);
            Assert.Equal(0.0, wp[wp.Count - 1].Leistung, 12);
        }

        [Fact]
        public void Ohne_Vorwaermbetrieb_zwei_Flaechen()
        {
            BivalenzdiagrammModell m = BivalenzdiagrammModell.Aus(Herleitung(vorwaermbetrieb: false));
            Assert.Equal(new[] { BivalenzFlaechenart.NurKessel, BivalenzFlaechenart.WaermepumpeAllein },
                         m.Flaechen.Select(f => f.Art).ToArray());
            Assert.Equal(1.83, m.Flaechen[0].BisC, TOL_PUNKT_K);
            Assert.DoesNotContain(m.Marken, k => k.Art == BivalenzMarkenart.ZweiterBivalenzpunkt);
        }

        [Fact]
        public void Parallel_begrenzt_das_Kennfeld()
        {
            // Große Übergabe (30 kW Nennleistung): Das Kennfeld begrenzt, unter θ_biv,1 = Kennfeldpunkt deckt die
            // Wärmepumpe ihr Kennfeld parallel zum Kessel; parallel kennt keinen Abschaltpunkt.
            Bivalenzherleitung h = Herleitung(phiN: 30.0, art: Bivalenzbetriebsart.Parallel);
            BivalenzdiagrammModell m = BivalenzdiagrammModell.Aus(h);
            Assert.Equal(new[] { BivalenzFlaechenart.Parallel, BivalenzFlaechenart.WaermepumpeAllein },
                         m.Flaechen.Select(f => f.Art).ToArray());
            Assert.Equal(-3.84, m.Flaechen[0].BisC, TOL_PUNKT_K);
            Assert.Equal(new[] { BivalenzMarkenart.ErsterBivalenzpunkt }, m.Marken.Select(k => k.Art).ToArray());
            Assert.Equal(7.0, m.Waermepumpe[0].Leistung, 12);
            Assert.Null(h.Punkte.AbschaltpunktC);
            Assert.Equal(-15.0, m.XVon);                            // ohne Abschaltpunkt dieselbe Achse: −12 … 20 → 5 K
            Assert.Equal(5.0, m.XSchritt);
        }

        [Fact]
        public void Stundenpunkte_erweitern_die_Achsen()
        {
            Bivalenzherleitung h = Herleitung();
            BivalenzdiagrammModell m = BivalenzdiagrammModell.Aus(h, new[]
            {
                new BivalenzStundenpunkt(-18.0, 0.0),
                new BivalenzStundenpunkt(5.0, 13.0),
                new BivalenzStundenpunkt(double.NaN, 3.0),
                new BivalenzStundenpunkt(0.0, double.PositiveInfinity),
            });
            Assert.Equal(2, m.Stundenpunkte.Count);
            Assert.Equal(-20.0, m.XVon);
            Assert.Equal(15.0, m.YBis);                             // 13 · 1,08 = 14,04 → Stufe 2,5 → 15
            Assert.Equal(-20.0, m.Flaechen[0].VonC);
        }

        [Fact]
        public void Platzhalter_ohne_Uebergabedaten()
        {
            BivalenzdiagrammModell m = BivalenzdiagrammModell.Aus(Herleitung(mitUebergabe: false));
            Assert.False(m.MitUebergabe);
            Assert.Empty(m.Heizlast);
            Assert.Empty(m.Flaechen);
            Assert.Empty(m.Marken);

            Zeichenmodell z = ChartRenderer.BivalenzdiagrammZeichnung(m);
            Assert.Equal(ChartRenderer.BIVALENZ_BREITE, z.Breite);
            Assert.Equal(ChartRenderer.BIVALENZ_HOEHE, z.Hoehe);
            Assert.Contains(z.Befehle.OfType<Text>(), t => t.Inhalt == "kein Bivalenzdiagramm — Kopplung aus");
            Assert.DoesNotContain(z.Befehle, b => (b.Marke ?? "").StartsWith("reihe:", StringComparison.Ordinal));
        }

        [Fact]
        public void Zeichnung_traegt_Reihen_und_Marken()
        {
            Zeichenmodell z = ChartRenderer.BivalenzdiagrammZeichnung(BivalenzdiagrammModell.Aus(Herleitung()));
            var marken = z.Befehle.Select(b => b.Marke ?? "").Distinct().ToList();
            foreach (string soll in new[] { "reihe:Heizlast", "reihe:Kennfeld bei 55 °C", "reihe:Übergabe bei 55 °C",
                                            "reihe:Wärmepumpe", "marke:ErsterBivalenzpunkt", "marke:ZweiterBivalenzpunkt",
                                            "marke:Abschaltpunkt", "marke:NachKennfeld", "bereiche" })
                Assert.Contains(soll, marken);
        }

        [Fact]
        public void Texte_aus_den_Ressourcen()
        {
            BivalenzdiagrammTexte t = BivalenzdiagrammTexte.AusRessourcen();
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.BER_BILD_BIVALENZ_TITEL, t.Titel);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.BER_BILD_BIVALENZ_PLATZHALTER, t.Platzhalter);
            using (new Kulturvorrichtung())
            {
                BivalenzdiagrammTexte de = BivalenzdiagrammTexte.AusRessourcen();
                var vorgabe = new BivalenzdiagrammTexte();
                Assert.Equal(vorgabe.Titel, de.Titel);
                Assert.Equal(vorgabe.Kennfeld, de.Kennfeld);
                Assert.Equal(vorgabe.ErsterBivalenzpunkt, de.ErsterBivalenzpunkt);
                Assert.Equal(vorgabe.Platzhalter, de.Platzhalter);
            }
        }
    }
}
