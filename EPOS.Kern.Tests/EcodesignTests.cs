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
            Assert.Null(z.Bezugsmenge);

            Assert.Equal(anzahlEreignisse, z.Ereignisse.Count);
            Assert.All(z.Ereignisse, e =>
            {
                Assert.InRange(e.MinuteBeginn, ERSTE_MINUTE, LETZTE_MINUTE);
                Assert.True(e.DauerMin >= 1);
                Assert.True(e.EnergieKwh > 0);
            });
            Assert.Equal(z.Ereignisse.Select(e => e.MinuteBeginn).OrderBy(m => m), z.Ereignisse.Select(e => e.MinuteBeginn));
            Assert.InRange(Math.Abs(z.Ereignisse.Sum(e => e.EnergieKwh) / qRef - 1.0), 0.0, 1e-9);

            // Als Bedarfstag: dieselbe Tagessumme über die Minuten verteilt; ohne Bezugsmenge wird
            // nicht skaliert (nur Einfamilienhaus, zur Plausibilisierung).
            Assert.Equal(1.0, Bedarfstag.Skalierung(z, 40.0));
            Bedarfstag tag = Bedarfstag.AusKatalog(z, Bedarfstag.Skalierung(z, 40.0));
            Assert.Equal(ZapfBedarfstagquelle.Ecodesign, tag.Quelle);
            Assert.InRange(Math.Abs(tag.TagessummeKwh / qRef - 1.0), 0.0, 1e-9);
            Assert.True(tag.GroessteMinutenleistungKw > 0);
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
