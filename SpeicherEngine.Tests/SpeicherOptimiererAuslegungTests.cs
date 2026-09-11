using System;
using Xunit;

namespace SpeicherEngine.Tests
{
    public sealed class SpeicherOptimiererAuslegungTests
    {
        private static SpeicherEingang Eingang()
        {
            return new SpeicherEingang(
                new[] { 0.0, 0.0, 40.0, 40.0 },
                new[] { 40.0, 40.0, 0.0, 0.0 },
                new[] { 20.0, 20.0, 20.0, 20.0 });
        }

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
            DegradationProA = 0.0,
            CVerEurProKwhZyklus = 0.025
        };

        private static OptimiererOptionen Fest(double cRate = 1.0) => new OptimiererOptionen
        {
            CMinKwh = 10.0,
            CMaxKwh = 10.0,
            RMin = cRate,
            RMax = cRate,
            RSchritt = 1.0,
            Feinraster = false
        };

        [Fact]
        public void Schrittachsen_Nehmen_Die_Exakte_Obergrenze_Auf()
        {
            OptimiererOptionen opt = new OptimiererOptionen
            {
                CMinKwh = 10.0,
                CMaxKwh = 25.0,
                CSchrittKwh = 10.0,
                RMin = 0.5,
                RMax = 1.2,
                RSchritt = 0.5,
                Feinraster = false
            };

            Assert.Equal(new[] { 10.0, 20.0, 25.0 }, opt.Groessenwerte());
            Assert.Equal(new[] { 0.5, 1.0, 1.2 }, opt.CRaten());
            Assert.Equal(9, opt.PunkteGesamt);
        }

        [Fact]
        public void Leistungsachse_Haelt_Leistung_Je_Zeile_Fest_Und_Leitet_Kapazitaet_Ab()
        {
            OptimiererOptionen opt = new OptimiererOptionen
            {
                Groessenachse = OptimiererGroessenachse.LeistungKw,
                PMinKw = 10.0,
                PMaxKw = 20.0,
                PSchrittKw = 10.0,
                RMin = 0.5,
                RMax = 1.0,
                RSchritt = 0.5,
                Feinraster = false
            };

            OptimiererRaster raster = new SpeicherOptimierer().Optimiere(Eingang(), Basis(), opt).Grobraster;

            Assert.Equal(OptimiererGroessenachse.LeistungKw, raster.Groessenachse);
            Assert.Equal(new[] { 10.0, 20.0 }, raster.Groessenwerte);
            Assert.Equal(new[] { 10.0, 20.0 }, raster.LeistungenKw);
            Assert.Empty(raster.KapazitaetenKwh);
            Assert.Equal(10.0, raster.Punkte[0][0].PKw, 12);
            Assert.Equal(20.0, raster.Punkte[0][0].CNomKwh, 12);
            Assert.Equal(10.0, raster.Punkte[0][1].PKw, 12);
            Assert.Equal(10.0, raster.Punkte[0][1].CNomKwh, 12);
            Assert.Equal(20.0, raster.Punkte[1][0].PKw, 12);
            Assert.Equal(40.0, raster.Punkte[1][0].CNomKwh, 12);
        }

        [Fact]
        public void Leistungsachse_Uebernimmt_Den_Achsenwert_Bitgenau()
        {
            OptimiererOptionen opt = new OptimiererOptionen
            {
                Groessenachse = OptimiererGroessenachse.LeistungKw,
                PMinKw = 7.3,
                PMaxKw = 7.3,
                RMin = 0.3,
                RMax = 0.3,
                RSchritt = 0.1,
                Feinraster = false,
                BetriebEurProKwJahr = 11.0
            };

            OptimiererErgebnis erg = new SpeicherOptimierer().Optimiere(Eingang(), Basis(), opt);

            Assert.Equal(BitConverter.DoubleToInt64Bits(7.3),
                         BitConverter.DoubleToInt64Bits(erg.BestPunkt.PKw));
            Assert.Equal(7.3 * 11.0, erg.BestPunkt.BetriebskostenLeistungEurProA, 12);
            Assert.Equal(BitConverter.DoubleToInt64Bits(7.3),
                         BitConverter.DoubleToInt64Bits(erg.BestParameter.PKw));
        }

        [Fact]
        public void Leistungsachse_Skaliert_Das_Initiale_Soc_Mit_Der_Abgeleiteten_Kapazitaet()
        {
            SpeicherParameter basis = Basis() with { StartSoCKwh = 5.0 };
            SpeicherParameter punkt = SpeicherOptimierer.RasterpunktNachLeistung(basis, 10.0, 0.5);

            Assert.Equal(20.0, punkt.CNomKwh, 12);
            Assert.Equal(10.0, punkt.StartSoCKwh!.Value, 12);
            Assert.Equal(0.5, punkt.StartSoCKwh.Value / punkt.CNomKwh, 12);
        }

        [Theory]
        [InlineData(OptimiererGroessenachse.KapazitaetKwh)]
        [InlineData(OptimiererGroessenachse.LeistungKw)]
        public void Gleiche_Grenzen_Erzeugen_Einen_Gueltigen_Einpunktbereich(OptimiererGroessenachse achse)
        {
            OptimiererOptionen opt = new OptimiererOptionen
            {
                Groessenachse = achse,
                CMinKwh = 10.0,
                CMaxKwh = 10.0,
                PMinKw = 10.0,
                PMaxKw = 10.0,
                RMin = 1.0,
                RMax = 1.0,
                RSchritt = 0.5,
                Feinraster = true
            };

            OptimiererErgebnis erg = new SpeicherOptimierer().Optimiere(Eingang(), Basis(), opt);

            Assert.Single(erg.Grobraster.Groessenwerte);
            Assert.NotNull(erg.Feinraster);
            Assert.Single(erg.Feinraster!.Groessenwerte);
            Assert.Equal(10.0, erg.Feinraster.GroessenMin, 12);
            Assert.Equal(10.0, erg.Feinraster.GroessenMax, 12);
            Assert.Equal(2, erg.PunkteGerechnet);
        }

        [Fact]
        public void Feinraster_Bleibt_Auch_Bei_Sehr_Engem_Bereich_Innerhalb_Der_Grenzen()
        {
            OptimiererOptionen opt = new OptimiererOptionen
            {
                CMinKwh = 100.0,
                CMaxKwh = 100.4,
                Stuetzstellen = 2,
                RMin = 1.0,
                RMax = 1.0,
                RSchritt = 1.0,
                Feinraster = true
            };

            OptimiererErgebnis erg = new SpeicherOptimierer().Optimiere(Eingang(), Basis(), opt);

            Assert.NotNull(erg.Feinraster);
            Assert.InRange(erg.Feinraster!.GroessenMin, opt.CMinKwh, opt.CMaxKwh);
            Assert.InRange(erg.Feinraster.GroessenMax, opt.CMinKwh, opt.CMaxKwh);
        }

        [Fact]
        public void Betriebskosten_Werden_Aus_Installierter_Groesse_Und_Entladung_Berechnet()
        {
            OptimiererOptionen opt = Fest() with
            {
                BetriebEurProKwJahr = 2.0,
                BetriebEurProKwhJahr = 3.0,
                BetriebEurProKwhEntladen = 4.0
            };

            OptimiererPunkt punkt = new SpeicherOptimierer().Optimiere(Eingang(), Basis(), opt).BestPunkt;

            Assert.Equal(20.0, punkt.BetriebskostenLeistungEurProA, 12);   // 10 kW * 2
            Assert.Equal(30.0, punkt.BetriebskostenKapazitaetEurProA, 12); // 10 kWh * 3
            Assert.Equal(20.0, punkt.BetriebskostenEntladungEurProA, 12);  // 5 kWh AC * 4
            Assert.Equal(70.0, punkt.BetriebskostenEurProA, 12);
            Assert.Equal(-249.25, punkt.JahresueberschussVorBetriebskostenEur, 12);
            Assert.Equal(-319.25, punkt.JahresueberschussEur, 12);
            Assert.Equal(punkt.JahresueberschussEur, punkt.ZielfunktionEur, 12);
            Assert.Equal(punkt.ErtragAequivalentEur - 70.0,
                         punkt.ErtragAequivalentNachBetriebskostenEur, 12);
            Assert.Equal(-2492.5, punkt.KapitalwertVorBetriebskostenEur, 12);
            Assert.Equal(-3192.5, punkt.KapitalwertEur, 12);
        }

        [Fact]
        public void Verschleiss_Und_Betriebskosten_Werden_Getrennt_Einmal_Abgezogen()
        {
            OptimiererOptionen opt = Fest() with
            {
                BetriebEurProKwhEntladen = 4.0,
                KVerInZielfunktion = true
            };

            OptimiererPunkt punkt = new SpeicherOptimierer().Optimiere(Eingang(), Basis(), opt).BestPunkt;

            Assert.Equal(20.0, punkt.BetriebskostenEurProA, 12);
            Assert.Equal(0.125, punkt.VerschleisskostenEurProA, 12);
            Assert.Equal(punkt.JahresueberschussVorBetriebskostenEur - 20.0,
                         punkt.JahresueberschussEur, 12);
            Assert.Equal(punkt.JahresueberschussEur - 0.125, punkt.ZielfunktionEur, 12);
        }

        [Fact]
        public void Unendliche_Negative_Und_Zu_Grosse_Suchraeume_Werden_Abgelehnt()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                (Fest() with { BetriebEurProKwJahr = double.PositiveInfinity }).Pruefe());
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                (Fest() with { BetriebEurProKwhEntladen = -0.01 }).Pruefe());
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                (Fest() with { CMaxKwh = double.NaN }).Pruefe());
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                (Fest() with { CMaxKwh = 100000.0, CSchrittKwh = 0.001 }).Pruefe());
        }

        [Fact]
        public void Netzanschluss_Pv_Ueberschuss_Laedt_Fuer_Die_Folgende_Spitze()
        {
            SpeicherEingang eingang = new SpeicherEingang(
                new[] { 0.0, 10.0 }, new[] { 10.0, 0.0 }, new[] { 20.0, 20.0 });
            SpeicherParameter basis = Basis() with { DtH = 1.0, IFixEur = 0.0 };
            OptimiererOptionen opt = Fest() with
            {
                Strategie = OptimiererStrategie.Lastspitzenkappung,
                LeistungspreisEurProKwA = 100.0,
                NetzanschlussAuslegung = true
            };

            OptimiererPunkt punkt = new SpeicherOptimierer().Optimiere(eingang, basis, opt).BestPunkt;

            Assert.Equal(10.0, punkt.SpitzeOhneSpeicherKw, 12);
            Assert.Equal(0.0, punkt.SpitzeMitSpeicherKw, 12);
            Assert.Equal(10.0, punkt.LadeenergieKwh, 12);
            Assert.Equal(10.0, punkt.EntladeenergieKwh, 12);
        }

        [Fact]
        public void Netzanschluss_Bewertet_Importaenderung_Mit_Dem_Jeweiligen_Preis()
        {
            // Der erste Peak setzt die Schwelle, im zweiten Intervall lädt der Speicher
            // bei negativem Preis aus dem Netz und entlädt beim hohen Preis.
            SpeicherEingang eingang = new SpeicherEingang(
                new[] { 10.0, 0.0, 20.0 }, new[] { 0.0, 0.0, 0.0 },
                new[] { 0.0, -5.0, 20.0 });
            SpeicherParameter basis = Basis() with
            {
                DtH = 1.0,
                CCapEurProKwh = 0.0,
                CPowEurProKw = 0.0,
                IFixEur = 0.0
            };
            OptimiererOptionen opt = Fest() with
            {
                Strategie = OptimiererStrategie.Lastspitzenkappung,
                LeistungspreisEurProKwA = 1.0,
                NetzanschlussAuslegung = true
            };

            OptimiererPunkt punkt = new SpeicherOptimierer().Optimiere(eingang, basis, opt).BestPunkt;

            Assert.Equal(10.0, punkt.LeistungspreisersparnisEur, 12);
            Assert.Equal(2.5, punkt.ErtragReferenzjahrEur - punkt.LeistungspreisersparnisEur, 12);
            Assert.Equal(12.5, punkt.JahresueberschussEur, 12);
        }

        [Fact]
        public void Peakshaving_Default_Bleibt_Auf_Dem_Reinen_Lastgang()
        {
            SpeicherEingang eingang = new SpeicherEingang(
                new[] { 10.0, 20.0 }, new[] { 9.0, 0.0 }, new[] { 5.0, 25.0 });
            PeakShavingParameter parameter = PeakShavingParameter.Nachziehend(100.0, 15.0);
            SpeicherParameter basis = Basis() with { DtH = 1.0 };

            PeakShavingErgebnis ueberEingang = new PeakShaving(parameter).BerechnePeakShaving(eingang, basis);
            PeakShavingErgebnis ueberLast = new PeakShaving(parameter).BerechnePeakShaving(eingang.LastKw, basis);

            Assert.Equal(ueberLast.PAltKw, ueberEingang.PAltKw);
            Assert.Equal(ueberLast.PNeuKw, ueberEingang.PNeuKw);
            Assert.Equal(BitConverter.DoubleToInt64Bits(ueberLast.ErtragPsEur),
                         BitConverter.DoubleToInt64Bits(ueberEingang.ErtragPsEur));
        }
    }
}
