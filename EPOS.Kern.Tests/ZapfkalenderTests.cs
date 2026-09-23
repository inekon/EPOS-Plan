using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using static EPOS.Kern.Tests.ZapfprofilTestbau;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Schicht S2 — Kalender und Kaltwassergang</b> (Umsetzungskonzept Zapfprofilgenerator
    /// 4.2, A6, K4): 365 Tage, Tagtyp aus Wochentag, Kennzeichen der Klimaregion und
    /// Ferienfenstern; fester Kaltwasser-Jahresgang. Erfundene Kalender und Temperaturen.
    /// </summary>
    public sealed class ZapfkalenderTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        [Fact]
        public void Das_Jahr_hat_365_Tage_und_beginnt_am_Wochentag_des_Januars()
        {
            // Jahr beginnt am Mittwoch (2): Tag 1 Werktag, Tag 4 Samstag, Tag 5 Sonntag.
            ZapfTagtyp[] k = Zapfkalender.Bilden(2, We(2), null);
            Assert.Equal(365, k.Length);
            Assert.Equal(ZapfTagtyp.Werktag, k[0]);
            Assert.Equal(ZapfTagtyp.Samstag, k[3]);
            Assert.Equal(ZapfTagtyp.SonnFeiertag, k[4]);
            Assert.Equal(ZapfTagtyp.Werktag, k[5]);
            Assert.Equal(52, k.Count(t => t == ZapfTagtyp.Samstag));
            Assert.Equal(52, k.Count(t => t == ZapfTagtyp.SonnFeiertag));
            Assert.Equal(261, k.Count(t => t == ZapfTagtyp.Werktag));
            Assert.Equal(2, Zapfkalender.Wochentag(2, 1));
            Assert.Equal(1, Zapfkalender.Monat(31));
            Assert.Equal(2, Zapfkalender.Monat(32));
            Assert.Equal(12, Zapfkalender.Monat(365));
            Assert.Equal(365, Zapfkalender.TageJeMonat.Sum());
        }

        [Fact]
        public void Ein_Feiertag_am_Montag_ist_ein_Sonntag_und_ein_Feiertag_am_Samstag_bleibt_Samstag()
        {
            // Jahr beginnt am Montag: Tag 8 ist Montag, Tag 13 Samstag.
            ZapfTagtyp[] k = Zapfkalender.Bilden(0, We(0, 8, 13), null);
            Assert.Equal(ZapfTagtyp.SonnFeiertag, k[7]);
            Assert.Equal(ZapfTagtyp.Samstag, k[12]);
        }

        [Fact]
        public void Ein_Feiertag_am_Montag_erhaelt_die_Sonntagsmenge()
        {
            // Werktagsgewicht Montag 0,3 ≠ Sonntag 0,05; Monat und Kaltwasser flach.
            Nutzungsart n = Art(woche: new[] { 0.3, 0.15, 0.15, 0.15, 0.15, 0.05, 0.05 });
            ZonenStand z = Zone();
            Zeitstruktur s = Formvektor.Bilden(z, n, n.Tagesgaenge, Parameter(), null, null);
            ZapfTagtyp[] k = Zapfkalender.Bilden(0, We(0, 8), null);
            double[] kw = Enumerable.Repeat(1.0, 12).ToArray();
            double[] tage = Formvektor.Tagesmengen(3650.0, s, k, 0, kw);
            double[] reihe = Formvektor.Stundenreihe(tage, s, k);

            Assert.Equal(tage[6], tage[7]);          // Tag 7 Sonntag, Tag 8 Feiertag am Montag
            Assert.NotEqual(tage[14], tage[7]);      // Tag 15 gewöhnlicher Montag
            for (int h = 0; h < 24; h++)
                Assert.Equal(reihe[6 * 24 + h], reihe[7 * 24 + h]);   // Sonntagsgang
        }

        [Fact]
        public void Jahrestag_366_ist_keine_Angabe()
        {
            Assert.Equal(new[] { new Ferienfenster(1, 6) }, Zapfkalender.AusJahrestagen(366, 6));
            Assert.Equal(new[] { new Ferienfenster(1, 6) }, Zapfkalender.AusJahrestagen(0, 6));
            Assert.Equal(new[] { new Ferienfenster(1, 6) }, Zapfkalender.AusJahrestagen(null, 6));
            Assert.Empty(Zapfkalender.AusJahrestagen(200, null));
            Assert.Empty(Zapfkalender.AusJahrestagen(200, 366));
            Assert.Equal(new[] { new Ferienfenster(200, 213) }, Zapfkalender.AusJahrestagen(200, 213));
            Assert.Equal(new[] { new Ferienfenster(300, 365) }, Zapfkalender.AusJahrestagen(300, 400));
        }

        [Fact]
        public void Ferien_ueber_den_Jahreswechsel_werden_Ruhetage()
        {
            IReadOnlyList<Ferienfenster> f = Zapfkalender.AusJahrestagen(360, 5);
            Assert.Equal(2, f.Count);
            ZapfTagtyp[] k = Zapfkalender.Bilden(0, We(0), f);
            for (int d = 1; d <= 5; d++) Assert.Equal(ZapfTagtyp.Ruhetag, k[d - 1]);
            for (int d = 360; d <= 365; d++) Assert.Equal(ZapfTagtyp.Ruhetag, k[d - 1]);
            Assert.NotEqual(ZapfTagtyp.Ruhetag, k[5]);
            Assert.NotEqual(ZapfTagtyp.Ruhetag, k[358]);

            ZonenStand z = Zone() with { Ferienbeginn = new int?[] { 366, 100, null, null }, Ferienende = new int?[] { 3, 102, null, null } };
            ZapfTagtyp[] kz = Zapfkalender.Bilden(0, We(0), Zapfkalender.FensterDerZone(z));
            Assert.Equal(6, kz.Count(t => t == ZapfTagtyp.Ruhetag));
        }

        [Fact]
        public void Ein_falscher_Kalender_wird_benannt_abgelehnt()
        {
            Assert.Equal(ZapfEingabefehler.KalenderUngueltig,
                Assert.Throws<ZapfprofilEingabeException>(() => Zapfkalender.Bilden(0, new bool[364], null)).Fehler);
            Assert.Equal(ZapfEingabefehler.KalenderUngueltig,
                Assert.Throws<ZapfprofilEingabeException>(() => Zapfkalender.Bilden(7, We(0), null)).Fehler);
            Assert.Throws<ZapfprofilEingabeException>(() => Zapfkalender.Bilden(0, null, null));
        }

        // =================================================================================
        // Kaltwassergang (K4)
        // =================================================================================

        [Fact]
        public void Das_Jahresmittel_des_Faktors_ist_eins()
        {
            double[] w = Kaltwassergang.Monatswerte(11.0, 2.0, 8);
            double[] f = Kaltwassergang.Monatsfaktoren(50.0, w, 11.0);
            Assert.True(Math.Abs(f.Average() - 1.0) < 1e-9);
            Assert.True(Math.Abs(w.Average() - 11.0) < 1e-9);
            // Im wärmsten Monat ist die Spreizung am kleinsten.
            Assert.Equal(f.Min(), f[7]);
            Assert.Equal(13.0, w[7], 12);
            Assert.Equal(9.0, w[1], 12);

            double[] flach = Kaltwassergang.Monatsfaktoren(50.0, Kaltwassergang.Monatswerte(11.0, 0.0, 1), 11.0);
            Assert.All(flach, x => Assert.Equal(1.0, x));
        }

        [Fact]
        public void Die_Monatswerte_sind_gerundet()
        {
            double[] w = Kaltwassergang.Monatswerte(11.0, 2.5, 7);
            foreach (double x in w) Assert.Equal(Math.Round(x, 9), x);
            Assert.Equal(12, w.Length);
        }

        [Fact]
        public void Kaltwasser_ueber_der_Zapftemperatur_wird_abgelehnt()
        {
            double[] w = Kaltwassergang.Monatswerte(40.0, 12.0, 8);
            Assert.Equal(ZapfEingabefehler.TemperaturUngueltig,
                Assert.Throws<ZapfprofilEingabeException>(() => Kaltwassergang.Monatsfaktoren(50.0, w, 40.0)).Fehler);
        }
    }
}
