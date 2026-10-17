using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using static EPOS.Kern.Tests.ZapfprofilTestbau;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>ZC4 (a) — der Kalender wirkt</b>: die Zählung der Tagtypen eines Zapfkalenders und die Verteilung der
    /// Zapfwärme einer Jahresreihe auf die Tagtypen. Keine neue Rechnung — nur Zählen und Summieren über den
    /// Kalender, mit dem der Bereich zapft. Erfundene Kalender und Reihen.
    /// </summary>
    public sealed class ZapfauswertungTagtypTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        [Fact]
        public void Die_Zaehlung_traegt_365_Tage_in_vier_Tagtypen()
        {
            // Jahr beginnt am Mittwoch (2): 261 Werktage, 52 Samstage, 52 Sonntage, keine Ferien.
            int[] t = Zapfkalender.Zaehlen(Zapfkalender.Bilden(2, We(2), null));
            Assert.Equal(new[] { 261, 52, 52, 0 }, t);
            Assert.Equal(365, t.Sum());
            Assert.Throws<ArgumentException>(() => Zapfkalender.Zaehlen(new ZapfTagtyp[364]));
            Assert.Throws<ArgumentException>(() => Zapfkalender.Zaehlen(null));
        }

        [Fact]
        public void Ein_Feiertag_zaehlt_als_Sonntag_auch_am_Samstag()
        {
            // Tag 4 ist ein Samstag, Tag 10 ein Freitag — beide Feiertag.
            ZapfTagtyp[] k = Zapfkalender.Bilden(2, We(2, 4, 10), null, feiertage: new[] { 4, 10 });
            Assert.Equal(new[] { 260, 51, 54, 0 }, Zapfkalender.Zaehlen(k));
        }

        [Fact]
        public void Die_Ferien_zaehlen_je_Bereich()
        {
            ZapfTagtyp[] mitFerien = Zapfkalender.Bilden(2, We(2), new[] { new Ferienfenster(182, 195) });
            ZapfTagtyp[] ohne = Zapfkalender.Bilden(2, We(2), null);
            int[] a = Zapfkalender.Zaehlen(mitFerien), b = Zapfkalender.Zaehlen(ohne);
            Assert.Equal(14, a[3]);
            Assert.Equal(0, b[3]);
            Assert.Equal(365, a.Sum());
            // Die vierzehn Ferientage fehlen den übrigen Tagtypen: 10 Werktage, 2 Samstage, 2 Sonntage.
            Assert.Equal(new[] { 251, 50, 50, 14 }, a);
        }

        [Fact]
        public void Die_Verteilung_summiert_die_Tage_je_Tagtyp_zur_Jahressumme()
        {
            ZapfTagtyp[] k = Zapfkalender.Bilden(2, We(2, 121), new[] { new Ferienfenster(182, 195) }, feiertage: new[] { 121 });
            var stunden = new double[Zapfkalender.STUNDEN_JAHR];
            for (int h = 0; h < stunden.Length; h++) stunden[h] = 0.5 + (h % 24) * 0.1 + (h / 24 % 7) * 0.03;
            var reihe = new Bilanzreihe(stunden);

            Tagtypverteilung v = Zapfauswertung.Verteilung(reihe, k);
            Assert.Equal(Zapfkalender.Zaehlen(k), v.Tage.ToArray());
            Assert.Equal(reihe.JahressummeKwh, v.SummeKwh, 6);
            Assert.Equal(reihe.JahressummeKwh, v.ZapfungKwh.Sum(), 6);
            Assert.True(v.ZapfungKwh.All(x => x > 0));

            // Eine gleichmäßige Reihe: jeder Tag 24 kWh, die Energie je Tagtyp ist 24 kWh je Tag.
            var gleich = new Bilanzreihe(Enumerable.Repeat(1.0, Zapfkalender.STUNDEN_JAHR).ToArray());
            Tagtypverteilung g = Zapfauswertung.Verteilung(gleich, k);
            for (int i = 0; i < 4; i++) Assert.Equal(24.0 * g.Tage[i], g.ZapfungKwh[i], 9);
            Assert.Throws<ArgumentNullException>(() => Zapfauswertung.Verteilung(null, k));
        }
    }
}
