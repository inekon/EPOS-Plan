using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Ecodesign-Zapfprofil als Bedarfstag der Quelle (5)</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 4.5, N11 (e); Stufe Z3). Die Daten sind frei: Verordnung (EU)
    /// Nr. 814/2013 der Kommission, Anhang III, Tabelle 1 „Lastprofile von Warmwasserbereitern",
    /// Lastprofil L (ABl. L 239 vom 6.9.2013, S. 162) — EU-Recht. Die Testdatenbank trägt sie
    /// als Katalogzeile (Herkunftsart <c>FREI</c>, eingespielt von
    /// <c>Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py</c>); die Probe liest die Ereignisse
    /// aus der Datenbank und hält ihre Summe gegen die Q_ref, die die Verordnung für das Profil
    /// nennt. Toleranz relativ 1e-9 (4.5).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class EcodesignTests
    {
        /// <summary>Q_ref des Lastprofils L [kWh/d], Verordnung (EU) Nr. 814/2013, Anhang III, Tabelle 1.</summary>
        private const double Q_REF_L = 11.655;

        /// <summary>Die Zapfungen der Tabelle 1 liegen zwischen 07:00 und 21:45 (Anhang III Nr. 2 b).</summary>
        private const int ERSTE_MINUTE = 7 * 60, LETZTE_MINUTE = 21 * 60 + 45;

        [Fact]
        public void Die_Tagessummen_sind_exakt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BedarfstagKatalogzeile z = Assert.Single(ZapfprofilCtrl.Bedarfstage(), t => t.QuelleArt == ZapfBedarfstagquelle.Ecodesign);
            Assert.Equal(Herkunftsart.Frei, z.Herkunft.Art);
            Assert.Equal("Verordnung (EU) Nr. 814/2013 Anhang III", z.Herkunft.Quelle);
            Assert.Null(z.Bezugsmenge);

            // 24 Zapfungen im Messzyklus, jede mit Dauer; die Summe ist die Q_ref des Profils.
            Assert.Equal(24, z.Ereignisse.Count);
            Assert.All(z.Ereignisse, e =>
            {
                Assert.InRange(e.MinuteBeginn, ERSTE_MINUTE, LETZTE_MINUTE);
                Assert.True(e.DauerMin >= 1);
                Assert.True(e.EnergieKwh > 0);
            });
            Assert.Equal(z.Ereignisse.Select(e => e.MinuteBeginn).OrderBy(m => m), z.Ereignisse.Select(e => e.MinuteBeginn));
            Assert.InRange(Math.Abs(z.Ereignisse.Sum(e => e.EnergieKwh) / Q_REF_L - 1.0), 0.0, 1e-9);

            // Als Bedarfstag: dieselbe Tagessumme über die Minuten verteilt; ohne Bezugsmenge wird
            // nicht skaliert (nur Einfamilienhaus, zur Plausibilisierung).
            Assert.Equal(1.0, Bedarfstag.Skalierung(z, 40.0));
            Bedarfstag tag = Bedarfstag.AusKatalog(z, Bedarfstag.Skalierung(z, 40.0));
            Assert.Equal(ZapfBedarfstagquelle.Ecodesign, tag.Quelle);
            Assert.InRange(Math.Abs(tag.TagessummeKwh / Q_REF_L - 1.0), 0.0, 1e-9);
            Assert.True(tag.GroessteMinutenleistungKw > 0);
        }
    }
}
