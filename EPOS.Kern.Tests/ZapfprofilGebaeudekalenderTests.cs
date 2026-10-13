using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Kalender eines Gebäudes, wie eine gebundene Zone ihn liest</b> (Konzept Zapfprofil-Benutzerführung ZB5,
    /// Karte „Kalender"): der Leser des Kerns (<see cref="ZapfprofilCtrl.GebaeudekalenderDesProjekts"/>, dieselbe Lesung
    /// wie der Lauf) und die Texte der Hülle — Wochenende als Kürzel, Feiertagsland mit Namen, Ferien „Name: von – bis".
    /// </summary>
    [Collection("Testdatenbank")]
    public class ZapfprofilGebaeudekalenderTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public ZapfprofilGebaeudekalenderTests(TestDatenbank db) { _db = db; }

        [Fact]
        public void Der_Leser_liefert_je_Gebaeude_des_Projekts_einen_Kalender()
        {
            if (!_db.Vorhanden) return;

            IReadOnlyList<(int Id, string Name)> gebaeude = ZapfprofilCtrl.GebaeudeDesProjekts(ZapfprofilReferenzprojektWacheTests.PROJEKT);
            IReadOnlyList<ZapfprofilCtrl.ZapfGebaeudekalender> kalender =
                ZapfprofilCtrl.GebaeudekalenderDesProjekts(ZapfprofilReferenzprojektWacheTests.PROJEKT);

            Assert.Equal(gebaeude.Select(g => g.Id), kalender.Select(k => k.Id));
            Assert.Contains(kalender, k => k.Id == ZapfprofilReferenzprojektWacheTests.GEBAEUDE);
            // Ferien zeigt der Leser nur, wenn das Gebäude sie führt — und dann jeden Zeitraum mit einem Ende.
            foreach (ZapfprofilCtrl.ZapfGebaeudekalender k in kalender)
                Assert.All(k.Ferien, f => Assert.True(f.Ende.HasValue || f.Beginn.HasValue));
            Assert.Empty(ZapfprofilCtrl.GebaeudekalenderDesProjekts(0));
        }

        [Fact]
        public void Die_Huelle_schreibt_Wochenende_Land_und_Ferien_als_Text()
        {
            using var kultur = new Kulturvorrichtung();
            var g = new ZapfprofilCtrl.ZapfGebaeudekalender
            {
                Id = 7,
                Name = " Haus A ",
                Wochenendtage = null,
                Feiertagsland = "BW",
                Ferien = new[]
                {
                    new ZonenferienStand { Name = "Sommer", Beginn = 182, Ende = 226 },
                    new ZonenferienStand { Beginn = null, Ende = 6 },
                    new ZonenferienStand { Name = "ohne Ende", Beginn = 300, Ende = 366 }
                }
            };

            ZapfprofilGebaeudekalenderDaten d = ZapfprofilHuelle.AlsGebaeudekalender(g);
            Assert.Equal("Haus A", d.Name);
            Assert.Equal("Sa + So", d.Wochenende);
            Assert.Equal("Baden-Württemberg", d.Feiertagsland);
            // Ohne Namen „Ferien n", ohne Beginn ab dem 1. Januar; ein Zeitraum ohne Ende wirkt nicht und fehlt.
            Assert.Equal(new[] { "Sommer: 01.07. – 14.08.", "Ferien 2: 01.01. – 06.01." }, d.Ferien);

            Assert.Equal("So", ZapfprofilHuelle.Wochenendtext(1 << 6));
            Assert.Equal("Fr + Sa", ZapfprofilHuelle.Wochenendtext(1 << 4 | 1 << 5));
            Assert.Equal("", ZapfprofilHuelle.Landname(null));
            Assert.Equal("XX", ZapfprofilHuelle.Landname("XX"));
        }
    }
}
