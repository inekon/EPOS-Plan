using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die neun Ecodesign-Zapfprofile als Bedarfstage der Quelle (5)</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 4.5, N11 (e), N26; Stufe Z3; Anwenderentscheid „Abschnitt 1: Ecodesign
    /// - erweitere Profil"). Die Daten sind frei: Verordnung (EU) Nr. 814/2013 der Kommission,
    /// Anhang III, Tabelle 1 „Lastprofile von Warmwasserbereitern" (ABl. L 239 vom 6.9.2013,
    /// S. 162) — EU-Recht, die neun Profile XXS bis 4XL (3XS ist nicht Teil des Katalogs). Die
    /// Testdatenbank trägt sie als Katalogzeilen (Herkunftsart <c>FREI</c>, eingespielt von
    /// <c>Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py</c> aus
    /// <c>Referenzlaeufe/Skripte/ecodesign_profile_bauen.py</c>); die Probe liest je Profil die
    /// Ereignisse aus der Datenbank und hält ihre Summe gegen die Q_ref, die die Verordnung für
    /// das Profil nennt. Toleranz relativ 1e-9 (4.5).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class EcodesignTests
    {
        /// <summary>Q_ref je Lastprofil [kWh/d], Verordnung (EU) Nr. 814/2013, Anhang III, Tabelle 1.</summary>
        public static readonly TheoryData<string, double, int> PROFILE = new TheoryData<string, double, int>
        {
            { "XXS", 2.100, 20 },
            { "XS", 2.100, 3 },
            { "S", 2.100, 11 },
            { "M", 5.845, 23 },
            { "L", 11.655, 24 },
            { "XL", 19.07, 30 },
            { "XXL", 24.53, 30 },
            { "3XL", 46.76, 10 },
            { "4XL", 93.52, 10 },
        };

        /// <summary>Die Zapfungen der Tabelle 1 liegen zwischen 07:00 und 21:45 (Anhang III Nr. 2 b).</summary>
        private const int ERSTE_MINUTE = 7 * 60, LETZTE_MINUTE = 21 * 60 + 45;

        [Theory]
        [MemberData(nameof(PROFILE))]
        public void Die_Tagessummen_sind_exakt(string profil, double qRef, int anzahlEreignisse)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string bezeichner = "Ecodesign-Zapfprofil " + profil;
            BedarfstagKatalogzeile z = Assert.Single(ZapfprofilCtrl.Bedarfstage(),
                t => t.QuelleArt == ZapfBedarfstagquelle.Ecodesign && t.Bezeichner == bezeichner);
            Assert.Equal(Herkunftsart.Frei, z.Herkunft.Art);
            Assert.Equal("Verordnung (EU) Nr. 814/2013 Anhang III", z.Herkunft.Quelle);
            // Die Bezugsmenge: Profil L beschreibt eine Wohneinheit, jedes andere Q_ref / Q_ref(L),
            // kaufmännisch auf 0,01 (Folgeposten #546).
            Assert.Equal(ZapfBezugsart.Wohneinheiten, z.Bezugsart);
            Assert.Equal(Math.Round(qRef / Q_REF_L, 2, MidpointRounding.AwayFromZero), z.Bezugsmenge.Value, 12);

            Assert.Equal(anzahlEreignisse, z.Ereignisse.Count);
            Assert.All(z.Ereignisse, e =>
            {
                Assert.InRange(e.MinuteBeginn, ERSTE_MINUTE, LETZTE_MINUTE);
                Assert.True(e.DauerMin >= 1);
                Assert.True(e.EnergieKwh > 0);
            });
            Assert.Equal(z.Ereignisse.Select(e => e.MinuteBeginn).OrderBy(m => m), z.Ereignisse.Select(e => e.MinuteBeginn));
            Assert.InRange(Math.Abs(z.Ereignisse.Sum(e => e.EnergieKwh) / qRef - 1.0), 0.0, 1e-9);

            // Als Bedarfstag auf seine eigene Bezugsmenge: unskaliert, dieselbe Tagessumme über die
            // Minuten verteilt.
            Assert.Equal(1.0, Bedarfstag.Skalierung(z, z.Bezugsmenge));
            Bedarfstag tag = Bedarfstag.AusKatalog(z, Bedarfstag.Skalierung(z, z.Bezugsmenge));
            Assert.Equal(ZapfBedarfstagquelle.Ecodesign, tag.Quelle);
            Assert.InRange(Math.Abs(tag.TagessummeKwh / qRef - 1.0), 0.0, 1e-9);
            Assert.True(tag.GroessteMinutenleistungKw > 0);
        }

        /// <summary>Q_ref des Profils L [kWh/d] — das Profil, das genau eine Wohneinheit beschreibt.</summary>
        private const double Q_REF_L = 11.655;

        private static BedarfstagKatalogzeile Profil(string name)
            => Assert.Single(ZapfprofilCtrl.Bedarfstage(),
                             t => t.QuelleArt == ZapfBedarfstagquelle.Ecodesign && t.Bezeichner == "Ecodesign-Zapfprofil " + name);

        /// <summary>
        /// <b>Die Ecodesign-Profile skalieren nach Wohneinheiten</b> (Folgeposten #546): Profil L bei
        /// einer Wohneinheit unverändert, bei zehn zehnfach; Profil M (0,5 Wohneinheiten) bei einer
        /// Wohneinheit doppelt.
        /// </summary>
        [Fact]
        public void Die_Ecodesign_Profile_skalieren_nach_Wohneinheiten()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BedarfstagKatalogzeile l = Profil("L");
            Assert.Equal(1.0, l.Bezugsmenge.Value, 12);
            Assert.Equal(1.0, Bedarfstag.Skalierung(l, 1.0), 12);
            Assert.Equal(11.655, Bedarfstag.AusKatalog(l, Bedarfstag.Skalierung(l, 1.0)).TagessummeKwh, 9);
            Assert.Equal(10.0, Bedarfstag.Skalierung(l, 10.0), 12);
            Assert.Equal(116.55, Bedarfstag.AusKatalog(l, Bedarfstag.Skalierung(l, 10.0)).TagessummeKwh, 9);

            BedarfstagKatalogzeile m = Profil("M");
            Assert.Equal(0.5, m.Bezugsmenge.Value, 12);
            Assert.Equal(2.0, Bedarfstag.Skalierung(m, 1.0), 12);
            Assert.Equal(2.0 * 5.845, Bedarfstag.AusKatalog(m, Bedarfstag.Skalierung(m, 1.0)).TagessummeKwh, 9);
        }

        /// <summary>Genau neun Ecodesign-Profile, keines doppelt (Anwenderentscheid: 3XS bleibt aussen vor).</summary>
        [Fact]
        public void Genau_neun_Ecodesign_Profile_ohne_Dopplung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ecodesign = ZapfprofilCtrl.Bedarfstage().Where(t => t.QuelleArt == ZapfBedarfstagquelle.Ecodesign).ToList();
            Assert.Equal(9, ecodesign.Count);
            Assert.Equal(ecodesign.Count, ecodesign.Select(t => t.Bezeichner).Distinct().Count());
        }
    }
}
