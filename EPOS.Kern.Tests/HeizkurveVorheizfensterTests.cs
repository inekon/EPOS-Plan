using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Heizkurve im Vorheizfenster</b> (SA1b): Mit den neuen Verfahren „Vorheizzeit vorgeben“ und „berechnen“ fährt die
    /// Heizkurve einer gekoppelten Zone im Fenster am Fenstersollwert θ_T — die Vorlaufreihe wird nach dem Plan am Sollwert der
    /// Stunde neu gebildet, die Vorausschau rechnet ebenso. Die Sollwertrampe plant gekoppelte Zonen nicht; ihr Vorlauf bleibt
    /// der Bestand.
    /// </summary>
    [Collection("Testdatenbank")]
    public class HeizkurveVorheizfensterTests : IClassFixture<TestDatenbank>
    {
        private const int STUNDEN = 8760;

        private readonly TestDatenbank _db;
        private readonly ITestOutputHelper _aus;

        public HeizkurveVorheizfensterTests(TestDatenbank db, ITestOutputHelper aus)
        {
            _db = db;
            _aus = aus;
        }

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

        private static Aufheizvorgabe Neu(Aufheizverfahren verfahren)
            => new Aufheizvorgabe(true, null, null, null, null)
            {
                Vorheizen = new Vorheizvorgabe(verfahren, verfahren == Aufheizverfahren.Vorgabe ? 6 : null),
            };

        /// <summary>Die Vorlaufreihe ist die Heizkurve am Sollwert der Stunde — Zeichen für Zeichen.</summary>
        private static void KurveAmSollwert(GebaeudeModellEingang e)
        {
            for (int h = 0; h < STUNDEN; h++)
                Assert.Equal(Bits(e.Heizkurve.VorlaufC(e.ThetaSoll[h], e.ThetaOut[h])), Bits(e.VorlaufC[h]));
        }

        [Theory]
        [InlineData(Aufheizverfahren.Vorgabe)]
        [InlineData(Aufheizverfahren.Berechnet)]
        public void Die_Kurve_folgt_im_Fenster_dem_Fenstersollwert(Aufheizverfahren verfahren)
        {
            GebaeudeModellEingang e = VorheizAnkunftsbezugTests.Gekoppelt(1.0);
            double[] kalender = (double[])e.ThetaSoll.Clone();
            double[] vorher = (double[])e.VorlaufC.Clone();
            (Aufheizplan plan, GebaeudeModellErgebnis lauf) = VorheizAnkunftsbezugTests.Rechnen(e, Neu(verfahren));
            Assert.NotNull(plan.Vorheizen);
            Assert.True(plan.Geaendert, "Der Plan hebt keine Stunde an.");
            KurveAmSollwert(e);

            // In den Fensterstunden (Sollwert angehoben) liegt der Vorlauf über dem am abgesenkten Sollwert; sonst unverändert.
            int fenster = 0, hoeher = 0;
            for (int h = 0; h < STUNDEN; h++)
            {
                bool angehoben = !double.IsNaN(e.ThetaSoll[h]) && (double.IsNaN(kalender[h]) || e.ThetaSoll[h] > kalender[h]);
                if (!angehoben)
                {
                    Assert.Equal(Bits(vorher[h]), Bits(e.VorlaufC[h]));
                    continue;
                }
                fenster++;
                if (double.IsNaN(vorher[h]) ? !double.IsNaN(e.VorlaufC[h]) : e.VorlaufC[h] > vorher[h]) hoeher++;
            }
            Assert.True(fenster > 0);
            Assert.True(hoeher > 0, "Kein Fenstervorlauf über dem abgesenkten.");

            // Der Lauf fährt diesen Vorlauf (ohne Raumeinfluss und Fahrplan): im Fenster mit Heizung höchstens die Kurve am θ_T.
            int geprueft = 0;
            for (int h = 0; h < STUNDEN; h++)
            {
                double v = lauf.Heizkreis.VorlaufC[h];
                if (double.IsNaN(v) || double.IsNaN(e.VorlaufC[h])) continue;
                Assert.True(v <= e.VorlaufC[h] + 1e-9, "Stunde " + h + ": Lauf " + v + " über der Kurve " + e.VorlaufC[h]);
                geprueft++;
            }
            Assert.True(geprueft > 0);
            _aus.WriteLine(verfahren + ": Fensterstunden " + fenster + ", höherer Vorlauf " + hoeher);
        }

        [Fact]
        public void Die_Vorausschau_rechnet_die_Kurve_am_Fenstersollwert()
        {
            GebaeudeModellEingang e = VorheizAnkunftsbezugTests.Gekoppelt(1.0);
            ZonenEingang zone = ZonenEingang.Einzeln(e);
            int h = Enumerable.Range(1, STUNDEN - 1)
                              .First(x => e.ThetaOut[x] < 0.0 && e.ThetaSoll[x] < 17.0 && !double.IsNaN(e.ThetaSoll[x]));
            Stundenrand r = zone.Rand(h, false, ReadOnlySpan<double>.Empty, false).MitVorheizen(20.0, double.NaN);
            Stundenrand k = e.KurveAmSollwert(in r, h, 20.0);
            Assert.Equal(Bits(e.Heizkurve.VorlaufC(20.0, e.ThetaOut[h])), Bits(k.VorlaufC));
            Assert.True(k.VorlaufC > r.VorlaufC);
            // Am Sollwert des Randes derselbe Rand; mit Vorlaufangebot gekappt.
            Assert.Equal(Bits(r.VorlaufC), Bits(e.KurveAmSollwert(in r, h, e.ThetaSoll[h]).VorlaufC));
            double grenze = r.VorlaufC + 0.5;
            Stundenrand g = e.KurveAmSollwert(r.MitVerfuegbarkeit(double.NaN, Verfuegbarkeitsgrund.KeineBegrenzung, grenze), h, 20.0);
            Assert.Equal(grenze, g.VorlaufC);
            Assert.True(g.VorlaufAnlageGekappt);
        }

        // =====================================================================
        //  Sollwertrampe: gekoppelte Zonen bleiben unberührt
        // =====================================================================

        /// <summary>
        /// Die Sollwertrampe plant eine gekoppelte Zone nicht (Zustand GEKOPPELT, W5/F13): Ihr Sollwert und damit ihr Vorlauf bleiben
        /// Zeichen für Zeichen der Bestand — eine Nachführung der Kurve an der Rampe hätte deshalb keine Wirkung (SA3a).
        /// </summary>
        [Fact]
        public void Die_Sollwertrampe_laesst_den_Vorlauf_einer_gekoppelten_Zone_unberuehrt()
        {
            GebaeudeModellEingang e = VorheizAnkunftsbezugTests.Gekoppelt(1.0);
            double[] vorher = (double[])e.VorlaufC.Clone();
            Aufheizplan plan = Aufheizoptimierung.Anwenden(ZonenEingang.Einzeln(e), new Aufheizvorgabe(true, null, null, null, null),
                                                           0.5 * e.AuslegungsheizlastW);
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT, plan.Zustand);
            Assert.False(plan.Geaendert);
            Assert.Null(plan.Vorheizen);
            for (int h = 0; h < STUNDEN; h++) Assert.Equal(Bits(vorher[h]), Bits(e.VorlaufC[h]));
        }

        /// <summary>
        /// Die Nachbildung der Kurve folgt dem Sollwert gekoppelter Zonen: Eine angehobene Reihe an einer gekoppelten Zone zieht
        /// die Kurve erst mit <see cref="GebaeudeModellEingang.VorlaufNachSollwertBilden"/> nach.
        /// </summary>
        [Fact]
        public void Die_Nachbildung_der_Kurve_folgt_einer_angehobenen_Reihe()
        {
            GebaeudeModellEingang e = VorheizAnkunftsbezugTests.Gekoppelt(1.0);
            double[] vorher = (double[])e.VorlaufC.Clone();
            double[] reihe = (double[])e.ThetaSoll.Clone();
            for (int h = 1; h < STUNDEN; h++)
                if (reihe[h] < 17.0 && e.ThetaSoll[h] < (h + 1 < STUNDEN ? e.ThetaSoll[h + 1] : 0.0)) reihe[h] = 18.0;
            e.HeizsollwertMitRampeSetzen(reihe);
            for (int h = 0; h < STUNDEN; h++) Assert.Equal(Bits(vorher[h]), Bits(e.VorlaufC[h]));
            GebaeudeModellEingang.VorlaufNachSollwertBilden(new[] { ZonenEingang.Einzeln(e) });
            KurveAmSollwert(e);
            Assert.Contains(Enumerable.Range(0, STUNDEN), h => e.VorlaufC[h] > vorher[h]);
        }
    }
}
