using System;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    public sealed class SpeicherOptimierungAuslegungIntegrationTests
    {
        private static SpeicherEingang Eingang() => new SpeicherEingang(
            new[] { 0.0, 0.0, 40.0, 40.0 },
            new[] { 40.0, 40.0, 0.0, 0.0 },
            new[] { 20.0, 20.0, 20.0, 20.0 });

        private static SpeicherParameter Basis() => new SpeicherParameter
        {
            CNomKwh = 10.0,
            PKw = 10.0,
            SoCMinKwh = 0.0,
            SoCMaxKwh = 10.0,
            RoundTripWirkungsgrad = 1.0,
            DtH = 0.25,
            VerguetungCtKwh = 5.0,
            CCapEurProKwh = 100.0,
            CPowEurProKw = 50.0,
            IFixEur = 1000.0,
            Kapitalzins = 0.0,
            NutzungsdauerA = 10.0,
            DegradationProA = 0.0
        };

        private static SpeicherOptimierungEingaben Neu() => new SpeicherOptimierungEingaben
        {
            Groessenachse = OptimiererGroessenachse.LeistungKw,
            PMinKw = 10.0,
            PMaxKw = 20.0,
            PSchrittKw = 10.0,
            CSchrittKwh = 5.0,
            CMinKwh = 10.0,
            CMaxKwh = 10.0,
            RMin = 1.0,
            RMax = 1.0,
            RSchritt = 1.0,
            Feinraster = false,
            Auslegung = new SpeicherAuslegungKonfiguration
            {
                VerwendeteKosten = new SpeicherKostensaetze
                {
                    BetriebEurProKwJahr = 2.0,
                    BetriebEurProKwhJahr = 3.0,
                    BetriebEurProKwhEntladen = 4.0,
                    BetriebVorhanden = true
                }
            }
        };

        [Fact]
        public void Optionen_Uebernehmen_Achse_Schritte_Und_Aufgeloeste_Laufkosten()
        {
            SpeicherOptimierungEingaben eingaben = Neu();
            OptimiererOptionen opt = SpeicherOptimierungCtrl.Optionen(eingaben);

            Assert.Equal(OptimiererGroessenachse.LeistungKw, opt.Groessenachse);
            Assert.Equal(10.0, opt.PMinKw);
            Assert.Equal(20.0, opt.PMaxKw);
            Assert.Equal(10.0, opt.PSchrittKw);
            Assert.Equal(5.0, opt.CSchrittKwh);
            Assert.Equal(2.0, opt.BetriebEurProKwJahr);
            Assert.Equal(3.0, opt.BetriebEurProKwhJahr);
            Assert.Equal(4.0, opt.BetriebEurProKwhEntladen);
            Assert.True(opt.NetzanschlussAuslegung);
        }

        [Fact]
        public void Legacy_Ohne_Auslegung_Behält_Nullkosten_Und_Alten_Peakpfad()
        {
            OptimiererOptionen opt = SpeicherOptimierungCtrl.Optionen(new SpeicherOptimierungEingaben());

            Assert.False(opt.NetzanschlussAuslegung);
            Assert.Equal(0.0, opt.BetriebEurProKwJahr);
            Assert.Equal(0.0, opt.BetriebEurProKwhJahr);
            Assert.Equal(0.0, opt.BetriebEurProKwhEntladen);
        }

        [Theory]
        [InlineData(OptimiererGroessenachse.KapazitaetKwh)]
        [InlineData(OptimiererGroessenachse.LeistungKw)]
        public void Gleiche_Grenzen_Sind_In_Beiden_Achsen_Gueltig(OptimiererGroessenachse achse)
        {
            SpeicherOptimierungEingaben e = Neu();
            e.Groessenachse = achse;
            e.CMaxKwh = e.CMinKwh;
            e.PMaxKw = e.PMinKw;

            Assert.Empty(SpeicherOptimierungCtrl.Pruefe(e));
            Assert.Equal(1, SpeicherOptimierungCtrl.Optionen(e).GroessenAnzahl);
        }

        [Fact]
        public void Leistung_Raster_Schnitt_Kpi_Und_Csv_Verwenden_Die_Neue_Achse_Und_Kosten()
        {
            OptimiererOptionen opt = SpeicherOptimierungCtrl.Optionen(Neu());
            OptimiererErgebnis roh = new SpeicherOptimierer().Optimiere(Eingang(), Basis(), opt);

            SpeicherOptimierungErgebnis dto = SpeicherOptimierungCtrl.Auswerten(roh);

            Assert.True(dto.Erfolg);
            Assert.Equal(OptimiererGroessenachse.LeistungKw, dto.Groessenachse);
            Assert.Equal(dto.LeistungKw, dto.Groessenwert);
            Assert.NotNull(dto.RasterBild);
            Assert.NotNull(dto.SchnittBild);
            Assert.Contains("[kW]", SpeicherOptimierungCtrl.GroessenachsenTitel(dto.Groessenachse), StringComparison.Ordinal);
            string betriebName = WindowsFormsApplication1.MyResource.Resource.WIRT_ZEILE_BETRIEBSKOSTEN
                .Replace(" [€/a]", "");
            Assert.Contains(dto.Kennzahlen, z =>
                z.Bezeichnung == betriebName);

            string[] zeilen = dto.RasterCsv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Contains("Größenachse", zeilen[0], StringComparison.Ordinal);
            Assert.Contains("LeistungKw", zeilen[1], StringComparison.Ordinal);
            Assert.All(zeilen, z => Assert.Equal(31, z.Count(c => c == ';')));
        }

        [Fact]
        public void Legacy_Csv_Behaelt_Die_Bisherige_Spaltenzahl()
        {
            OptimiererOptionen opt = SpeicherOptimierungCtrl.Optionen(new SpeicherOptimierungEingaben
            {
                CMinKwh = 10.0,
                CMaxKwh = 20.0,
                Stuetzstellen = 2,
                RMin = 1.0,
                RMax = 1.0,
                RSchritt = 1.0,
                Feinraster = false
            });
            OptimiererErgebnis roh = new SpeicherOptimierer().Optimiere(Eingang(), Basis(), opt);

            string[] zeilen = SpeicherOptimierungCtrl.RasterCsvText(roh)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries);

            Assert.All(zeilen, z => Assert.Equal(25, z.Count(c => c == ';')));
        }

        [Fact]
        public void Betriebsbild_Bewahrt_Tatsaechliche_Zeitstempel_Des_Ausschnitts()
        {
            OptimiererOptionen opt = SpeicherOptimierungCtrl.Optionen(Neu());
            OptimiererErgebnis roh = new SpeicherOptimierer().Optimiere(Eingang(), Basis(), opt);
            DateTimeOffset start = new DateTimeOffset(2025, 3, 30, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset[] zeit = Enumerable.Range(0, 4)
                .Select(i => start.AddMinutes(15 * i)).ToArray();
            var vorbereitung = new StromspeicherOptimierungVorbereitung
            {
                Eingang = Eingang(),
                Basis = Basis(),
                ZeitstempelUtc = zeit,
                ZeitachsenHinweis = "Tatsächliche UTC-Zeitachse"
            };

            SpeicherOptimierungBetriebsbild bild = SpeicherOptimierungCtrl.Betriebsbild(
                vorbereitung, roh, false, null, null);

            Assert.Equal(zeit, bild.ZeitstempelUtc);
            Assert.Equal("Tatsächliche UTC-Zeitachse", bild.ZeitachsenHinweis);
        }
    }
}
