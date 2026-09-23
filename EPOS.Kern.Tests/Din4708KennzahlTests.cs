using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using static EPOS.Kern.Tests.AuslegungTestbau;
using static EPOS.Kern.Tests.ZapfprofilTestbau;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Normvergleich — die Kennzahl nach DIN 4708</b> (Umsetzungskonzept Zapfprofilgenerator
    /// 4.5 c): Formel gegen eine Handrechnung mit ERFUNDENEN Parametern und einer erfundenen
    /// Wohnungstabelle, Gültigkeit je Zone und Topologie, „nicht rechenbar" ohne Wohnungstabelle.
    /// Keine Normzahl: p_b, w_b, W_b, a_i, z, Kappung, Belegung und Ausstattung sind erfunden.
    /// </summary>
    public sealed class Din4708KennzahlTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        /// <summary>Erfundener Katalog: Raumzahl 2 → 2 Personen, 3 → 2,5; Klasse „Klasse B" 4000 Wh.</summary>
        private static Din4708Katalog Katalog() => new Din4708Katalog(
            new Dictionary<string, double> { ["2"] = 2.0, ["3"] = 2.5 },
            new[] { new Din4708Ausstattung(11, "Klasse B", 4000.0), new Din4708Ausstattung(12, "Klasse C", 9000.0) });

        private static ZonenStand Wohnzone(string name = "Wohnen", params WohnungstypStand[] w)
            => Zone(name) with { Wohnungen = w };

        /// <summary>Unabhängige Fehlerfunktion: Simpson-Integral von 2/√π · e^(−t²) über [0; x].</summary>
        private static double ErfSimpson(double x)
        {
            const int n = 20000;
            double h = x / n, s = 0.0;
            for (int i = 0; i <= n; i++)
            {
                double f = Math.Exp(-(i * h) * (i * h));
                s += (i == 0 || i == n ? 1 : i % 2 == 1 ? 4 : 2) * f;
            }
            return 2.0 / Math.Sqrt(Math.PI) * s * h / 3.0;
        }

        [Fact]
        public void Die_Fehlerfunktion_stimmt_mit_einer_unabhaengigen_Quadratur()
        {
            Assert.Equal(0.0, Din4708Kennzahl.Erf(0.0));
            foreach (double x in new[] { 0.05, 0.3, 0.8, 1.2, 1.9, 2.7, 4.0 })
            {
                Assert.True(Math.Abs(Din4708Kennzahl.Erf(x) - ErfSimpson(x)) < 1e-11, "erf(" + x + ")");
                Assert.Equal(-Din4708Kennzahl.Erf(x), Din4708Kennzahl.Erf(-x));
            }
            Assert.Equal(1.0, Din4708Kennzahl.Erf(7.0));
            // Kappung: ab der Grenze gilt K = 1.
            Assert.Equal(Din4708Kennzahl.Erf(1.2), Din4708Kennzahl.K(1.2, 1.5));
            Assert.Equal(1.0, Din4708Kennzahl.K(1.5, 1.5));
            Assert.Equal(1.0, Din4708Kennzahl.K(2.0, 1.5));
        }

        [Fact]
        public void Die_Kennzahl_folgt_der_Handrechnung()
        {
            // 10 WE mit Raumzahl 2 (Katalog: 2 Personen), Klasse B (4000 Wh);
            // 5 WE mit 3 Personen, ohne Klasse (Einheitswohnung: w_b = 6000 Wh).
            // N = (10·2·4000 + 5·3·6000) / (p_b · w_b) = 170000 / 24000.
            ZonenStand z = Wohnzone("Wohnen",
                new WohnungstypStand { Anzahl = 10, Raumzahl = 2, IdAusstattung = 11 },
                new WohnungstypStand { Anzahl = 5, Personen = 3 });
            Parametersatz ps = Auslegungssatz();
            Din4708Ergebnis e = Din4708Kennzahl.Rechnen(new[] { (z, Art()) }, Katalog(), ps, 50.0, 0.75);

            Assert.True(e.Gueltig);
            Assert.True(e.Vollstaendig);
            double n = 170000.0 / 24000.0;
            Assert.True(Relativ(e.KennzahlN.Value, n) < 1e-12);
            Assert.Equal(15, e.Wohnungen);
            Assert.True(Relativ(e.Personen.Value, 35.0) < 1e-12);

            // u_i = a_i · z · (1 + √N) / √N mit a_1 0,3, a_2 3, z 0,2; Kappung 1,5 (beide darunter).
            double w = Math.Sqrt(n);
            double u1 = 0.3 * 0.2 * (1 + w) / w, u2 = 3.0 * 0.2 * (1 + w) / w;
            Assert.True(u2 < 1.5);
            double wz = 5000.0 * (n * ErfSimpson(u1) + w * ErfSimpson(u2)) / 1000.0;
            Assert.True(Relativ(e.WzKwh.Value, wz) < 1e-9);
            Assert.True(Relativ(e.VolumenL.Value, wz * 1000.0 / (1.163 * 50.0) / 0.75) < 1e-9);
            Assert.Contains(e.Hinweise, h => h.Code == Din4708Kennzahl.HINWEIS_WAERMEPUMPE);
            Assert.True(e.Zeilen[1].AusstattungVorgabe);
            Assert.Equal(6000.0, e.Zeilen[1].AusstattungWh);
        }

        [Fact]
        public void Die_Kappung_setzt_K_auf_eins()
        {
            // N = 1: u_2 = 3 · 0,2 · 2 = 1,2; mit Kappung 1,0 gilt K(u_2) = 1.
            Parametersatz ps = Auslegungssatz(new Dictionary<string, double> { [ZapfAuslegungParameter.DIN4708_KAPPUNG] = 1.0 });
            double u1 = 0.3 * 0.2 * 2.0;
            double erwartet = 5000.0 * (1.0 * ErfSimpson(u1) + 1.0 * 1.0) / 1000.0;
            Assert.True(Relativ(Din4708Kennzahl.WzKwh(1.0, ps), erwartet) < 1e-9);
            Assert.Equal(0.0, Din4708Kennzahl.WzKwh(0.0, ps));
            // W_z wächst mit N, W_z / N fällt (Gleichzeitigkeit).
            Parametersatz std = Auslegungssatz();
            double vor = 0.0, jeEinheitVor = double.MaxValue;
            foreach (double nn in new[] { 1.0, 2.0, 5.0, 20.0, 100.0 })
            {
                double wz = Din4708Kennzahl.WzKwh(nn, std);
                Assert.True(wz > vor);
                Assert.True(wz / nn < jeEinheitVor);
                vor = wz;
                jeEinheitVor = wz / nn;
            }
        }

        [Fact]
        public void Die_Gueltigkeit_gilt_je_Zone_und_Topologie()
        {
            Parametersatz ps = Auslegungssatz();
            ZonenStand wohnen = Wohnzone("Wohnen", new WohnungstypStand { Anzahl = 4, Personen = 2 });
            ZonenStand buero = Zone("Büro", 2);
            Nutzungsart nichtwohnen = Art(2, kalender: ZapfKalenderart.Arbeitstage);

            // Nichtwohnen allein: außerhalb des Gültigkeitsbereichs.
            Din4708Ergebnis e = Din4708Kennzahl.Rechnen(new[] { (buero, nichtwohnen) }, Katalog(), ps, 50.0, 0.75);
            Assert.False(e.Gueltig);
            Assert.Equal(ZapfAuslegungsfehler.NichtGueltig, e.Fehler);
            Assert.Contains(Din4708Kennzahl.AUSSERHALB, e.Grund);
            Assert.Equal(new[] { "Büro" }, e.ZonenAusserhalb);

            // Wohnen mit Durchfluss: außerhalb.
            e = Din4708Kennzahl.Rechnen(new[] { (wohnen with { Topologie = ZapfTopologie.Durchfluss }, Art()) },
                                        Katalog(), ps, 50.0, 0.75);
            Assert.False(e.Gueltig);

            // Gemischte Gruppe: gültig, aber nur für die Wohnzone, mit Hinweis.
            e = Din4708Kennzahl.Rechnen(new[] { (wohnen, Art()), (buero, nichtwohnen) }, Katalog(), ps, 50.0, 0.75);
            Assert.True(e.Gueltig);
            Assert.False(e.Vollstaendig);
            Assert.Contains(e.Hinweise, h => h.Code == Din4708Kennzahl.HINWEIS_TEILGUELTIG);
            Assert.True(Relativ(e.KennzahlN.Value, 4 * 2 * 6000.0 / 24000.0) < 1e-12);
        }

        [Fact]
        public void Ohne_Wohnungstabelle_nicht_rechenbar()
        {
            Parametersatz ps = Auslegungssatz();
            Din4708Ergebnis e = Din4708Kennzahl.Rechnen(new[] { (Zone("Wohnen"), Art()) }, Katalog(), ps, 50.0, 0.75);
            Assert.False(e.Gueltig);
            Assert.Equal(ZapfAuslegungsfehler.WohnungstabelleFehlt, e.Fehler);
            Assert.Contains("nicht rechenbar", e.Grund);
            Assert.Null(e.KennzahlN);

            // Unbekannte Ausstattungsklasse und fehlende Belegung: benannt, nie geschätzt.
            e = Din4708Kennzahl.Rechnen(new[] { (Wohnzone("W", new WohnungstypStand { Anzahl = 2, Personen = 2, IdAusstattung = 99 }), Art()) },
                                        Katalog(), ps, 50.0, 0.75);
            Assert.Equal(ZapfAuslegungsfehler.WohnungstypUngueltig, e.Fehler);
            e = Din4708Kennzahl.Rechnen(new[] { (Wohnzone("W", new WohnungstypStand { Anzahl = 2, Raumzahl = 7 }), Art()) },
                                        Katalog(), ps, 50.0, 0.75);
            Assert.Equal(ZapfAuslegungsfehler.WohnungstypUngueltig, e.Fehler);
            // Personen je WE der Zone als Belegung, wie im Mengengerüst.
            e = Din4708Kennzahl.Rechnen(new[] { (Wohnzone("W", new WohnungstypStand { Anzahl = 2, Raumzahl = 7 }) with { PersonenJeWe = 1.5 }, Art()) },
                                        Katalog(), ps, 50.0, 0.75);
            Assert.True(e.Gueltig);
            Assert.True(Relativ(e.Personen.Value, 3.0) < 1e-12);
            // Fehlt ein Parameter der Formel, lehnt der Parametersatz benannt ab.
            Assert.Throws<ParametersatzException>(() => Din4708Kennzahl.WzKwh(2.0, Auslegungssatz(null, ZapfAuslegungParameter.DIN4708_A2)));
        }
    }
}
