using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Interpolation über den Vorlauf</b> (AK3-I, Festlegungen I-1 bis I-5, <see cref="VorlaufInterpolation"/>,
    /// gilt ohne Schalter): (a) die Lage zwischen den Stützstellen; (b) O1p — der Produktweg mit
    /// einer echten Kennlinie aus der Testdatenbank, Leistung und COP an Zwischenvorläufen gegen eine Handrechnung;
    /// (c) Randwerte und Stützstellen bitgleich zur Stützstellenwahl; (d) Zählung der Vorlaufwahl (I-4);
    /// (e) Kälteseite zwischen 7 und 18 °C (I-3); (f) das gekoppelte Referenzprojekt 1047 interpoliert im Lauf.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class VorlaufInterpolationTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        // =============================================================================
        //  (a) Lage zwischen den Stützstellen
        // =============================================================================

        [Theory]
        [InlineData(38.5, 0, 0.35)]
        [InlineData(45.0, -1, 0.0)]     // auf der Stützstelle: keine Interpolation (I-2)
        [InlineData(52.5, 1, 0.75)]
        [InlineData(30.0, -1, 0.0)]     // darunter: Randregel
        [InlineData(60.0, -1, 0.0)]     // darüber: Randregel
        [InlineData(double.NaN, -1, 0.0)]
        public void Einschliessend_liefert_untere_Stuetzstelle_und_Anteil_der_oberen(double v, int unten, double gewicht)
        {
            bool zwischen = VorlaufInterpolation.Einschliessend(new[] { 35, 45, 55 }, v, out int u, out double g);
            Assert.Equal(unten >= 0, zwischen);
            Assert.Equal(unten, u);
            Assert.Equal(gewicht, g, 12);
        }

        [Fact]
        public void Gewichtung_nimmt_Leistung_und_COP_je_fuer_sich_und_Pel_als_Quotient()
        {
            double[] u = { 1, 4.0, 10.0, 2.5 };
            double[] o = { 1, 3.0, 9.0, 3.0 };
            double[] r = SimulationWaermepumpe.VorlaufGewichten(u, o, 0.25);
            Assert.Equal(1.0, r[0]);
            Assert.Equal(3.75, r[1], 12);
            Assert.Equal(9.75, r[2], 12);
            Assert.Equal(9.75 / 3.75, r[3], 12);
        }

        // =============================================================================
        //  (b), (c) O1p: Produktweg mit echter Kennlinie
        // =============================================================================

        /// <summary>Ein Gerät der Testdatenbank mit den Vorlauf-Stützstellen 35, 45 und 55 °C.</summary>
        private static int GeraetMitDreiStuetzstellen()
        {
            object id = DataRepository.ExecuteScalar(
                "SELECT ID_WP FROM Tab_Kenndaten GROUP BY ID_WP " +
                "HAVING SUM(IIF(Vorlauf = 35, 1, 0)) > 1 AND SUM(IIF(Vorlauf = 45, 1, 0)) > 1 AND SUM(IIF(Vorlauf = 55, 1, 0)) > 1 " +
                "ORDER BY ID_WP LIMIT 1");
            Assert.NotNull(id);
            return Convert.ToInt32(id, CultureInfo.InvariantCulture);
        }

        /// <summary>Handrechnung: die Kennlinie EINES Vorlaufs linear über der Quelltemperatur (innen).</summary>
        private static (double cop, double ptherm) VonHand(int idWp, int vorlauf, double temperatur)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT Temperatur, COP, Ptherm FROM Tab_Kenndaten WHERE ID_WP = ? AND Vorlauf = ? ORDER BY Temperatur",
                new DbParam("@wp", idWp), new DbParam("@vorlauf", vorlauf));
            var p = dt.Rows.Cast<DataRow>()
                      .Select(r => (t: Convert.ToDouble(r["Temperatur"], CultureInfo.InvariantCulture),
                                    c: Convert.ToDouble(r["COP"], CultureInfo.InvariantCulture),
                                    q: Convert.ToDouble(r["Ptherm"], CultureInfo.InvariantCulture)))
                      .ToList();
            for (int i = 1; i < p.Count; i++)
            {
                if (temperatur > p[i].t) continue;
                double a = (temperatur - p[i - 1].t) / (p[i].t - p[i - 1].t);
                return (p[i - 1].c + a * (p[i].c - p[i - 1].c), p[i - 1].q + a * (p[i].q - p[i - 1].q));
            }
            throw new InvalidOperationException("Temperatur außerhalb der Kennlinie.");
        }

        /// <summary>Eine Quelltemperatur, die in beiden Kennlinien innen und auf keiner Stützstelle liegt.</summary>
        private static double InnereQuelltemperatur(SimulationWaermepumpe._Kenndaten a, SimulationWaermepumpe._Kenndaten b)
        {
            double unten = Math.Max(a.dat.Min(d => d.Temperatur), b.dat.Min(d => d.Temperatur));
            double oben = Math.Min(a.dat.Max(d => d.Temperatur), b.dat.Max(d => d.Temperatur));
            Assert.True(oben > unten);
            return unten + 0.37 * (oben - unten) + 0.123;
        }

        [Theory]
        [InlineData(38.5)]
        [InlineData(41.0)]
        [InlineData(47.25)]
        [InlineData(53.9)]
        public void O1p_Leistung_und_COP_am_Zwischenvorlauf_treffen_die_Handrechnung(double vorlauf)
        {
            if (!_db.Vorhanden) return;
            int id = GeraetMitDreiStuetzstellen();
            SimulationWaermepumpe.Kennlinienwahl wahl;
            wahl = SimulationWaermepumpe.KennlinienwahlLaden(id, null);
            Assert.True(wahl.Interpolieren);

            Assert.Equal(SimulationWaermepumpe.Vorlauflage.Innerhalb,
                         wahl.Abfragen(vorlauf, true, out _, out int unten, out double gewicht));
            Assert.True(unten >= 0);
            SimulationWaermepumpe._Kenndaten ku = wahl.Kurven[unten], ko = wahl.Kurven[unten + 1];
            Assert.True(ku.Vorlauf < vorlauf && vorlauf < ko.Vorlauf);
            Assert.Equal((vorlauf - ku.Vorlauf) / (ko.Vorlauf - ku.Vorlauf), gewicht, 12);

            double t = InnereQuelltemperatur(ku, ko);
            double[] r = new SimulationWaermepumpe().KennlinieInterpoliertAuswerten(t, null, ku, ko, gewicht, -1);

            (double cu, double qu) = VonHand(id, ku.Vorlauf, t);
            (double co, double qo) = VonHand(id, ko.Vorlauf, t);
            double cop = (1 - gewicht) * cu + gewicht * co;
            double ptherm = (1 - gewicht) * qu + gewicht * qo;
            Assert.Equal(1.0, r[0]);
            Assert.Equal(cop, r[1], 9);
            Assert.Equal(ptherm, r[2], 9);
            Assert.Equal(ptherm / cop, r[3], 9);

            // Stetig und zwischen den beiden Kennlinien: der Wert liegt im Band der Stützwerte.
            Assert.InRange(r[1], Math.Min(cu, co) - 1e-12, Math.Max(cu, co) + 1e-12);
            Assert.InRange(r[2], Math.Min(qu, qo) - 1e-12, Math.Max(qu, qo) + 1e-12);

            // Zwei Auswertungen sind bitgleich.
            double[] r2 = new SimulationWaermepumpe().KennlinieInterpoliertAuswerten(t, null, ku, ko, gewicht, -1);
            Assert.Equal(r, r2);
        }

        [Theory]
        [InlineData(45.0)]   // auf der Stützstelle (I-2)
        [InlineData(30.0)]   // darunter: unterste Kennlinie (F-A8)
        [InlineData(70.0)]   // darüber, Extrapolation erlaubt: oberste Kennlinie (F-A8)
        public void Auf_der_Stuetzstelle_und_ausserhalb_rechnet_die_Stuetzstelle_wie_die_Stuetzstellenwahl(double vorlauf)
        {
            if (!_db.Vorhanden) return;
            int id = GeraetMitDreiStuetzstellen();
            SimulationWaermepumpe.Kennlinienwahl ein, aus;
            ein = SimulationWaermepumpe.KennlinienwahlLaden(id, null);
            aus = SimulationWaermepumpe.KennlinienwahlLaden(id, null);
            aus.Interpolieren = false;   // Stützstellenwahl als Gegenprobe
            Assert.True(ein.Interpolieren);

            SimulationWaermepumpe.Vorlauflage lageEin = ein.Abfragen(vorlauf, true, out int stelleEin, out int unten, out _);
            SimulationWaermepumpe.Vorlauflage lageAus = aus.Abfragen(vorlauf, true, out int stelleAus);
            Assert.Equal(-1, unten);
            Assert.Equal(lageAus, lageEin);
            Assert.Equal(stelleAus, stelleEin);

            var sim = new SimulationWaermepumpe();
            double t = InnereQuelltemperatur(ein.Kurven[0], ein.Kurven[ein.Kurven.Length - 1]);
            Assert.Equal(sim.berechne_wptherm(t, null, aus.Kurven[stelleAus], -1),
                         sim.berechne_wptherm(t, null, ein.Kurven[stelleEin], -1));
        }

        // =============================================================================
        //  (d) Zählung der Vorlaufwahl (I-4)
        // =============================================================================

        [Fact]
        public void Die_Vorlaufwahl_zaehlt_weiter_die_naechste_Stuetzstelle_und_dazu_die_Intervalle()
        {
            var wahl = SimulationWaermepumpe.Kennlinienwahl.FuerVorlaeufe(35, 45, 55);
            wahl.Interpolieren = true;
            double[] reihe = { 30.0, 36.0, 40.0, 41.0, 45.0, 47.0, 52.0, 55.0, 60.0 };
            foreach (double v in reihe) wahl.Zaehlen(v, true, out _);

            Assert.Equal(new[] { 2, 4, 3 }, wahl.Stunden);   // Zählung: nächste Stützstelle
            Assert.Equal(1, wahl.Darueber);
            Assert.Equal(1, wahl.Darunter);
            Assert.Equal(new[] { 3, 2 }, wahl.Interpoliert);  // 36, 40, 41 | 47, 52
            Assert.Equal("35:1;45:4;55:2", wahl.Ausweis().StundenText);
        }

        // =============================================================================
        //  (e) Kälteseite (I-3, K21 neu)
        // =============================================================================

        private static readonly int[] TEMPERATUREN = { 20, 30, 40 };
        private static readonly double[] EER_7 = { 4.0, 3.2, 2.4 };
        private static readonly double[] PK_7 = { 50.0, 44.0, 38.0 };
        private static readonly double[] EER_18 = { 6.0, 4.8, 3.6 };
        private static readonly double[] PK_18 = { 70.0, 62.0, 54.0 };

        private static List<KuehlkennlinienZeile> Kuehlzeilen()
        {
            var z = new List<KuehlkennlinienZeile>();
            int id = 1;
            for (int i = 0; i < TEMPERATUREN.Length; i++)
            {
                z.Add(new KuehlkennlinienZeile(id++, 7, TEMPERATUREN[i], EER_7[i], PK_7[i], 100));
                z.Add(new KuehlkennlinienZeile(id++, 18, TEMPERATUREN[i], EER_18[i], PK_18[i], 100));
                z.Add(new KuehlkennlinienZeile(id++, 18, TEMPERATUREN[i], 1.0, 1.0, 50));   // Teillast gilt nicht
            }
            return z;
        }

        [Fact]
        public void Kaelte_zwischen_7_und_18_Grad_interpoliert_Leistung_und_EER()
        {
            Kuehlkennlinie k = Kuehlkennlinie.Bilden(Kuehlzeilen(), 12, true);
            Assert.True(k.Interpoliert);
            Assert.False(k.VorlaufAusgewichen);
            Assert.Equal(7, k.Vorlauf);
            Assert.Equal(18, k.Oben.Vorlauf);
            Assert.Equal(100, k.Oben.Laststufe);
            double w = 5.0 / 11.0;
            Assert.Equal(w, k.GewichtOben, 12);

            KennlinienPunkt p = k.Auswerten(25.0, true);
            double eer7 = 3.6, eer18 = 5.4, pk7 = 47.0, pk18 = 66.0;   // je bei 25 °C, Mitte 20/30
            Assert.Equal(eer7 + w * (eer18 - eer7), p.Eer, 12);
            Assert.Equal(pk7 + w * (pk18 - pk7), p.Pkuehl, 12);
            Assert.Equal(KennlinienLage.Innen, p.Lage);

            // Außerhalb der Temperaturachse gilt je Vorlauf die Randregel, die Lage wird weitergegeben.
            KennlinienPunkt kalt = k.Auswerten(10.0, true);
            Assert.Equal(KennlinienLage.KappungUnten, kalt.Lage);
            Assert.Equal(PK_7[0] + w * (PK_18[0] - PK_7[0]), kalt.Pkuehl, 12);
        }

        [Theory]
        [InlineData(7)]
        [InlineData(18)]
        [InlineData(22)]    // darüber: Randwert 18 wie bisher
        [InlineData(3)]     // darunter: Randwert 7 wie bisher
        [InlineData(null)]  // NULL: kleinster Stützwert
        public void Kaelte_auf_der_Stuetzstelle_und_ausserhalb_wie_die_Stuetzstellenwahl(int? kuehlVorlauf)
        {
            Kuehlkennlinie ein = Kuehlkennlinie.Bilden(Kuehlzeilen(), kuehlVorlauf, true);
            Kuehlkennlinie aus = Kuehlkennlinie.Bilden(Kuehlzeilen(), kuehlVorlauf);
            Assert.False(ein.Interpoliert);
            Assert.Equal(aus.Vorlauf, ein.Vorlauf);
            Assert.Equal(aus.VorlaufAusgewichen, ein.VorlaufAusgewichen);
            foreach (double t in new[] { 10.0, 25.0, 40.0, 45.0 })
            {
                Assert.Equal(aus.Auswerten(t, true).Eer, ein.Auswerten(t, true).Eer);
                Assert.Equal(aus.Auswerten(t, true).Pkuehl, ein.Auswerten(t, true).Pkuehl);
            }
        }

        // =============================================================================
        //  (f) Gekoppeltes Referenzprojekt 1047 interpoliert im Lauf
        // =============================================================================

        private static ErgebnisWaermepumpeModel LaufUndLesen(int projekt)
        {
            SimulationProtokoll.NeuStarten();
            int kopf = new SimulationRunner().SimuliereUndSpeichere(projekt, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);
            ErgebnisWaermepumpeModel w = new ErgebnisCtrl().Load(projekt).Waermepumpe;
            Assert.NotNull(w);
            return w;
        }

        [Fact]
        public void Referenzprojekt_1047_interpoliert_im_Lauf_und_weist_die_Vorlaufwahl_aus()
        {
            if (!_db.Vorhanden) return;
            string kopf = WindowsFormsApplication1.MyResource.Resource.SIMENG_WP_VORLAUF_INTERPOLIERT;
            string kennung = kopf.Substring(kopf.IndexOf("{0}", StringComparison.Ordinal) + 3);
            kennung = kennung.Substring(0, kennung.IndexOf("{1}", StringComparison.Ordinal));

            ErgebnisWaermepumpeModel w = LaufUndLesen(1047);
            Assert.Contains(SimulationProtokoll.Aktuell.Hinweise, h => h.Contains(kennung, StringComparison.Ordinal));

            // Die Ausweisspalten bleiben die Zählung der nächsten Stützstelle (I-4).
            ErgebnisWaermepumpeModulModel m = Assert.Single(w.Module, x => x.Vorlaufwahl_Stunden != null);
            Assert.False(string.IsNullOrEmpty(m.Vorlaufwahl_Stunden));
            Assert.True(w.Stromverbrauch_WP > 0 && w.Waermeproduktion_WP > 0);
        }
    }
}
