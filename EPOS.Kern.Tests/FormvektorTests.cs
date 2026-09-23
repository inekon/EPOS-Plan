using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using static EPOS.Kern.Tests.ZapfprofilTestbau;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Schicht S2 — Formvektor, Tagtypgewicht und Ferienregel</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 2.4, 4.2): Energieerhaltung, Formvektor-Summe 1, Normierung,
    /// Nullgewichte. Erfundene Formen; Toleranz relativ 1e-12.
    /// </summary>
    public sealed class FormvektorTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        private static readonly double[] Flach = Enumerable.Repeat(1.0, 12).ToArray();

        private static (Zeitstruktur S, ZapfTagtyp[] K, double[] Kw) Aufbau(Nutzungsart n, ZonenStand z = null,
                                                                            List<ZapfHinweis> h = null, int jan1 = 0,
                                                                            bool[] we = null)
        {
            z ??= Zone();
            Zeitstruktur s = Formvektor.Bilden(z, n, n.Tagesgaenge, Parameter(), null, h);
            ZapfTagtyp[] k = Zapfkalender.Bilden(jan1, we ?? We(jan1), Zapfkalender.FensterDerZone(z));
            double[] kw = Kaltwassergang.Monatsfaktoren(50.0, Kaltwassergang.Monatswerte(11.0, 2.0, 8), 11.0);
            return (s, k, kw);
        }

        [Fact]
        public void Die_Stundenreihe_erhaelt_die_Jahresmenge()
        {
            Nutzungsart n = Art(monate: new[] { 1.5, 1.5, 1.2, 1.0, 0.8, 0.6, 0.6, 0.6, 0.8, 1.0, 1.2, 1.2 },
                                ferienfaktor: 0.5);
            ZonenStand z = Zone() with { Ferienbeginn = new int?[] { 200, null, null, null }, Ferienende = new int?[] { 220, null, null, null } };
            var (s, k, kw) = Aufbau(n, z);
            double[] tage = Formvektor.Tagesmengen(12345.0, s, k, 0, kw);
            Assert.True(Relativ(tage.Sum(), 12345.0) < 1e-12);
            double[] reihe = Formvektor.Stundenreihe(tage, s, k);
            Assert.Equal(8760, reihe.Length);
            Assert.True(Relativ(reihe.Sum(), 12345.0) < 1e-12);
            for (int d = 0; d < 365; d++)
                Assert.True(Relativ(reihe.Skip(d * 24).Take(24).Sum(), tage[d]) < 1e-12);
        }

        [Fact]
        public void Der_Formvektor_summiert_zu_eins()
        {
            var (s, k, kw) = Aufbau(Art());
            double[] form = Formvektor.Form(s, k, 0, kw);
            Assert.True(Math.Abs(form.Sum() - 1.0) < 1e-12);
            Assert.All(form, x => Assert.True(x >= 0));
        }

        [Fact]
        public void Tagesgang_und_Woche_summieren_zu_eins()
        {
            // Wochenfaktoren Σ 2 und ein Tagesgang Σ 2: normiert, mit Hinweis.
            Tagesgangsatz satz = Satz(5, Gang((6, 1.0), (18, 1.0)), Gang((9, 1.0)), Gang((10, 1.0)), Gang((11, 1.0)));
            Nutzungsart n = Art(woche: new[] { 0.3, 0.3, 0.3, 0.3, 0.3, 0.25, 0.25 }, satz: satz);
            var h = new List<ZapfHinweis>();
            var (s, _, _) = Aufbau(n, h: h);
            Assert.True(Math.Abs(s.Wochenfaktoren.Sum() - 1.0) < 1e-15);
            for (int t = 0; t < 4; t++)
            {
                double summe = 0.0;
                for (int x = 0; x < 24; x++) summe += s.Tagesgaenge[t, x];
                Assert.Equal(1.0, summe, 15);
            }
            Assert.Contains(h, x => x.Code == "WOCHENFAKTOREN_SUMME");
            Assert.Contains(h, x => x.Code == "TAGESGANG_SUMME");

            h.Clear();
            Aufbau(Art(), h: h);
            Assert.Empty(h);
        }

        [Fact]
        public void Ein_Wochenfaktor_null_gibt_dem_Tag_nichts_und_die_Menge_bleibt()
        {
            Nutzungsart n = Art(woche: new[] { 0.2, 0.2, 0.2, 0.2, 0.2, 0.0, 0.0 });
            var (s, k, kw) = Aufbau(n);
            double[] tage = Formvektor.Tagesmengen(1000.0, s, k, 0, kw);
            for (int d = 1; d <= 365; d++)
                if (k[d - 1] != ZapfTagtyp.Werktag) Assert.Equal(0.0, tage[d - 1]);
            Assert.True(Relativ(tage.Sum(), 1000.0) < 1e-12);
        }

        [Fact]
        public void Ein_leerer_Tagesgang_ergibt_null_mit_Hinweis()
        {
            Tagesgangsatz satz = Satz(6, Gang((7, 1.0)), Gang((9, 1.0)), Gang((10, 1.0)), new double[24]);
            Nutzungsart n = Art(satz: satz);
            ZonenStand z = Zone() with { Ferienbeginn = new int?[] { 100, null, null, null }, Ferienende = new int?[] { 110, null, null, null } };
            var h = new List<ZapfHinweis>();
            var (s, k, kw) = Aufbau(n, z, h);
            Assert.Contains(h, x => x.Code == "TAGESGANG_LEER");
            double[] tage = Formvektor.Tagesmengen(1000.0, s, k, 0, kw);
            for (int d = 100; d <= 110; d++) Assert.Equal(0.0, tage[d - 1]);
            Assert.True(Relativ(Formvektor.Stundenreihe(tage, s, k).Sum(), 1000.0) < 1e-12);
        }

        [Fact]
        public void Der_Ruhetag_traegt_Ferienfaktor_mal_Wochenmittel_sonst_die_Sonntagsmenge()
        {
            // Monate und Kaltwasser flach, damit nur das Tagesgewicht zählt.
            ZonenStand z = Zone() with { Ferienbeginn = new int?[] { 15, null, null, null }, Ferienende = new int?[] { 15, null, null, null } };
            double[] woche = { 0.2, 0.2, 0.2, 0.1, 0.1, 0.1, 0.1 };
            ZapfTagtyp[] k = Zapfkalender.Bilden(0, We(0), Zapfkalender.FensterDerZone(z));

            Nutzungsart mit = Art(woche: woche, ferienfaktor: 0.7);
            Zeitstruktur s = Formvektor.Bilden(z, mit, mit.Tagesgaenge, Parameter(), null, null);
            double[] t = Formvektor.Tagesmengen(1000.0, s, k, 0, Flach);
            // Tag 15 Montag (Ruhetag), Tag 8 Montag (Werktag): Verhältnis 0,7 · (1/7) / 0,2.
            Assert.Equal(0.7 * (1.0 / 7.0) / 0.2, t[14] / t[7], 12);

            Nutzungsart ohne = Art(woche: woche, ferienfaktor: null);
            Zeitstruktur s2 = Formvektor.Bilden(z, ohne, ohne.Tagesgaenge, Parameter(), null, null);
            double[] t2 = Formvektor.Tagesmengen(1000.0, s2, k, 0, Flach);
            Assert.Equal(t2[13], t2[14], 12);    // Tag 14 Sonntag
            Assert.NotEqual(ZapfTagtyp.SonnFeiertag, k[14]);
        }

        [Fact]
        public void Der_Auslastungsgang_ueberschreibt_den_Monat()
        {
            var p = new Herkunftsprotokoll();
            double?[] ausl = new double?[12];
            ausl[0] = 2.0;
            ZonenStand z = Zone() with { Auslastung = ausl };
            Nutzungsart n = Art();
            Zeitstruktur s = Formvektor.Bilden(z, n, n.Tagesgaenge, Parameter(), p, null);
            Assert.Equal(2.0, s.Monatsfaktoren[0]);
            Assert.Equal(1.0, s.Monatsfaktoren[1]);
            Assert.Equal(Wertstatus.Ueberschrieben, p.Letzter("Zone A", ZapfFeld.MONATSFAKTOREN).Status);

            ZapfTagtyp[] k = Zapfkalender.Bilden(0, We(0), null);
            double[] t = Formvektor.Tagesmengen(1000.0, s, k, 0, Flach);
            // 1. und 29. Januar sind Montage, ebenso der 5. Februar (Tag 36).
            Assert.Equal(2.0, t[0] / t[35], 12);
        }

        [Fact]
        public void Keine_Verteilung_und_falsche_Raster_werden_benannt_abgelehnt()
        {
            Nutzungsart null7 = Art(woche: new double[7]);
            Assert.Equal(ZapfEingabefehler.KeineVerteilung,
                Assert.Throws<ZapfprofilEingabeException>(() => Formvektor.Bilden(Zone(), null7, null7.Tagesgaenge, Parameter(), null, null)).Fehler);

            Nutzungsart kurz = Art(monate: new double[11]);
            Assert.Equal(ZapfEingabefehler.RasterUngueltig,
                Assert.Throws<ZapfprofilEingabeException>(() => Formvektor.Bilden(Zone(), kurz, kurz.Tagesgaenge, Parameter(), null, null)).Fehler);

            Nutzungsart negativ = Art(woche: new[] { 0.5, 0.5, 0.5, -0.5, 0.0, 0.0, 0.0 });
            Assert.Equal(ZapfEingabefehler.RasterUngueltig,
                Assert.Throws<ZapfprofilEingabeException>(() => Formvektor.Bilden(Zone(), negativ, negativ.Tagesgaenge, Parameter(), null, null)).Fehler);

            // Nur Werktage gewichtet, aber alle Tage Ferien -> nichts zu verteilen.
            Nutzungsart werktags = Art(woche: new[] { 0.2, 0.2, 0.2, 0.2, 0.2, 0.0, 0.0 }, ferienfaktor: 0.0);
            ZonenStand ferien = Zone() with { Ferienbeginn = new int?[] { 1, null, null, null }, Ferienende = new int?[] { 365, null, null, null } };
            var (s, k, kw) = Aufbau(werktags, ferien);
            Assert.Equal(ZapfEingabefehler.KeineVerteilung,
                Assert.Throws<ZapfprofilEingabeException>(() => Formvektor.Tagesmengen(10.0, s, k, 0, kw)).Fehler);
        }
    }
}
