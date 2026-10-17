using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Konvention des Gemeinjahrs</b> (E114, <see cref="Gemeinjahrkalender"/>): Ohne Jahr liegt Ostern auf dem Sonntag
    /// des Wochentagsrasters am nächsten zum 8. April (Jahrestag 98), Buß- und Bettag auf dem letzten Mittwoch des Rasters
    /// vor dem 23. November (Jahrestag 327); mit dem Jahr einer Preisreihe auf den echten Daten dieses Jahres. Feste
    /// Feiertage hängen an keinem der beiden Teile.
    /// </summary>
    public sealed class FeiertageTests
    {
        private const int MONTAG = 0, MITTWOCH = 2, DONNERSTAG = 3, FREITAG = 4, SONNTAG = 6;

        private static int Tag(string regel, Gemeinjahrkalender k) => Feiertage.Jahrestag(regel, k);

        [Fact]
        public void Im_Donnerstagsraster_liegt_Ostern_am_5_April()
        {
            var k = new Gemeinjahrkalender(DONNERSTAG);
            Assert.False(k.MitJahr);
            Assert.Equal(95, k.Ostersonntag);                                   // 5. April: 3 Tage vor dem Anker, 4 danach
            Assert.Equal(SONNTAG, k.Wochentag(k.Ostersonntag));
            Assert.Equal(93, Tag(DbWerte.KOND_FEIERTAG_KARFREITAG, k));
            Assert.Equal(FREITAG, k.Wochentag(93));
            Assert.Equal(96, Tag(DbWerte.KOND_FEIERTAG_OSTERMONTAG, k));
            Assert.Equal(134, Tag(DbWerte.KOND_FEIERTAG_HIMMELFAHRT, k));
            Assert.Equal(DONNERSTAG, k.Wochentag(134));
            Assert.Equal(145, Tag(DbWerte.KOND_FEIERTAG_PFINGSTMONTAG, k));
            Assert.Equal(MONTAG, k.Wochentag(145));
            Assert.Equal(155, Tag(DbWerte.KOND_FEIERTAG_FRONLEICHNAM, k));
            Assert.Equal(322, Tag(DbWerte.KOND_FEIERTAG_BUSS_UND_BETTAG, k));   // 18. November
            Assert.Equal(MITTWOCH, k.Wochentag(322));
        }

        [Fact]
        public void Im_Montagsraster_ist_der_Anker_selbst_Ostersonntag()
        {
            var k = new Gemeinjahrkalender(MONTAG);
            Assert.Equal(Gemeinjahrkalender.OSTERANKER, k.Ostersonntag);
            Assert.Equal(SONNTAG, k.Wochentag(98));
            Assert.Equal(96, Tag(DbWerte.KOND_FEIERTAG_KARFREITAG, k));
            Assert.Equal(158, Tag(DbWerte.KOND_FEIERTAG_FRONLEICHNAM, k));
            Assert.Equal(325, Tag(DbWerte.KOND_FEIERTAG_BUSS_UND_BETTAG, k));   // 21. November
        }

        /// <summary>
        /// Gleichstand kann nicht eintreten — die Woche hat sieben Tage, die Abstände zum Sonntag davor und danach ergänzen
        /// sich zu 7. Darum liegt Ostern in jedem der sieben Raster höchstens drei Tage vom Anker, Buß- und Bettag in der
        /// Woche vor dem 23. November.
        /// </summary>
        [Fact]
        public void In_jedem_Raster_liegt_Ostern_hoechstens_drei_Tage_vom_Anker()
        {
            for (int w0 = 0; w0 < 7; w0++)
            {
                var k = new Gemeinjahrkalender(w0);
                Assert.Equal(SONNTAG, k.Wochentag(k.Ostersonntag));
                Assert.InRange(k.Ostersonntag, 95, 101);
                int b = Tag(DbWerte.KOND_FEIERTAG_BUSS_UND_BETTAG, k);
                Assert.Equal(MITTWOCH, k.Wochentag(b));
                Assert.InRange(b, 320, 326);
            }
            Assert.Equal(7, Enumerable.Range(0, 7).Select(w => new Gemeinjahrkalender(w).Ostersonntag).Distinct().Count());
        }

        [Fact]
        public void Feste_Feiertage_haengen_an_keinem_Teil_der_Konvention()
        {
            foreach (var k in new[] { new Gemeinjahrkalender(DONNERSTAG), Gemeinjahrkalender.Kalenderjahr(2024), Gemeinjahrkalender.Kalenderjahr(2026) })
            {
                Assert.Equal(1, Tag(DbWerte.KOND_FEIERTAG_NEUJAHR, k));
                Assert.Equal(121, Tag(DbWerte.KOND_FEIERTAG_ERSTER_MAI, k));
                Assert.Equal(276, Tag(DbWerte.KOND_FEIERTAG_EINHEIT, k));
                Assert.Equal(359, Tag(DbWerte.KOND_FEIERTAG_WEIHNACHTEN_1, k));
                Assert.Equal(304, Tag(DbWerte.KOND_FEIERTAG_REFORMATIONSTAG, k));
            }
        }

        [Fact]
        public void Landesregeln_folgen_derselben_Konvention()
        {
            var k = new Gemeinjahrkalender(DONNERSTAG);
            var by = Landesfeiertage.Jahrestage("BY", k);
            Assert.Contains(155, by);                                           // Fronleichnam
            Assert.Contains(Feiertage.Gemeinjahrestag(1, 6), by);
            Assert.Contains(322, Landesfeiertage.Jahrestage("SN", k));          // Buß- und Bettag
            Assert.DoesNotContain(322, Landesfeiertage.Jahrestage("BY", k));
            Assert.Equal(9, Landesfeiertage.Jahrestage(null, k).Count);
            Assert.Equal(new[] { 1, 93, 96, 121, 134, 145, 276, 359, 360 }, Landesfeiertage.Jahrestage(null, k));
        }

        [Fact]
        public void Mit_Jahr_gelten_Raster_und_echte_Daten_des_Jahres()
        {
            {
                var k = Gemeinjahrkalender.Kalenderjahr(2025);
                Assert.True(k.MitJahr);
                Assert.Equal(2, k.W0);   // 1. Januar 2025 = Mittwoch
                Assert.Equal(SONNTAG, k.Wochentag(k.Ostersonntag));
                Assert.Equal(2, k.Wochentag(k.BussUndBettag));
                Assert.Equal(Feiertage.Gemeinjahrestag(4, 20), k.Ostersonntag);
                Assert.Equal(Feiertage.Gemeinjahrestag(4, 18), Tag(DbWerte.KOND_FEIERTAG_KARFREITAG, k));
                Assert.Equal(Feiertage.Gemeinjahrestag(6, 19), Tag(DbWerte.KOND_FEIERTAG_FRONLEICHNAM, k));
                Assert.Equal(Feiertage.Gemeinjahrestag(11, 19), Tag(DbWerte.KOND_FEIERTAG_BUSS_UND_BETTAG, k));
            }
            // Schaltjahr: dasselbe Datum, nicht derselbe Jahrestag des Schaltjahrs.
            Assert.Equal(Feiertage.Gemeinjahrestag(3, 29), Tag(DbWerte.KOND_FEIERTAG_KARFREITAG, Gemeinjahrkalender.Kalenderjahr(2024)));
            Assert.Equal(Feiertage.Gemeinjahrestag(11, 22), Tag(DbWerte.KOND_FEIERTAG_BUSS_UND_BETTAG, Gemeinjahrkalender.Kalenderjahr(2023)));
        }

        [Fact]
        public void Die_Konvention_prueft_Raster_und_Jahr()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Gemeinjahrkalender(7));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Gemeinjahrkalender(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Gemeinjahrkalender.Kalenderjahr(1500));
            Assert.Equal(new Gemeinjahrkalender(3), Gemeinjahrkalender.Aus(3, 0));
            Assert.Equal(Gemeinjahrkalender.Kalenderjahr(2026), Gemeinjahrkalender.Aus(3, 2026));
            // Ein Jahr mit fremdem Raster ist nicht baubar (E115): 2027 beginnt an einem Freitag.
            Assert.Throws<ArgumentException>(() => Gemeinjahrkalender.Aus(3, 2027));
            Assert.Throws<ArgumentException>(() => new Konditionierungssatz(3, 2027));
            Assert.Equal(DONNERSTAG, Gemeinjahrkalender.Kalenderjahr(2026).W0);
            Assert.Equal(-1, Tag("ROSENMONTAG", new Gemeinjahrkalender(0)));
        }

        /// <summary>Ein Konditionierungskalender legt seine Feiertagsregel nach der Konvention des Satzes, nicht nach einem Jahr.</summary>
        [Fact]
        public void Der_Kalender_legt_Karfreitag_nach_dem_Raster()
        {
            var k = new Gemeinjahrkalender(MITTWOCH);                          // das Raster der Vorgabe 2025 im Gebäudelauf
            Assert.Equal(96, k.Ostersonntag);
            Assert.Equal(94, Tag(DbWerte.KOND_FEIERTAG_KARFREITAG, k));
            Assert.Equal(FREITAG, k.Wochentag(94));
            Assert.Equal(323, Tag(DbWerte.KOND_FEIERTAG_BUSS_UND_BETTAG, k));   // 19. November — wie 2025
        }
    }
}
