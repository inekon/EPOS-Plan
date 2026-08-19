using System;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// Schutzstufen-Riegel (Fachkonzept 4.1) und Rundendeckel (Fachkonzept 3.3,
    /// Festlegung 5).
    /// </summary>
    public class KiRiegelTests
    {
        private static KiRegister R() => Registerabbild.MitHoeherenStufen();

        private static KiAufruf Aufruf(string aktion, string argumente)
        {
            KiPruefErgebnis p = KiPruefung.PruefeJson(R(), aktion, argumente);
            Assert.True(p.Gueltig, p.FehlerText());
            return p.Aufruf!;
        }

        // ===================================================== Schutzstufen-Riegel

        [Fact]
        public void BisEtappeDreiIstNurLesenFreigegeben()
            => Assert.Equal(Schutzstufe.Lesen, KiRiegel.OhneBestaetigung);

        [Fact]
        public void LesendeAktionDarfDirektLaufen()
        {
            KiAufruf a = Aufruf("projekt_lesen", "{\"projekt_id\":7}");

            Assert.Null(KiRiegel.Pruefe(a));
            Assert.True(KiRiegel.DarfDirektLaufen(a));
        }

        [Fact]
        public void SchreibendeAktionWirdAngehalten()
        {
            KiAufruf a = Aufruf("variante_anlegen", "{\"projekt_id\":7,\"bezeichner\":\"Variante B\"}");

            string? grund = KiRiegel.Pruefe(a);

            Assert.NotNull(grund);
            Assert.False(KiRiegel.DarfDirektLaufen(a));
            Assert.Contains("variante_anlegen", grund!);
            Assert.Contains("Bestätigungsschicht", grund!);
        }

        [Fact]
        public void RechnendeAktionWirdAngehalten()
        {
            KiAufruf a = Aufruf("simulation_rechnen", "{\"projekt_id\":7}");

            string? grund = KiRiegel.Pruefe(a);

            Assert.NotNull(grund);
            Assert.Contains("simulation_rechnen", grund!);
        }

        [Fact]
        public void JedeStufeOberhalbDerGrenzeWirdAngehalten()
        {
            // Der Riegel haengt an der Stufe, nicht an einer Namensliste - eine neue
            // Schreibaktion ist damit ohne Zutun mit erfasst.
            foreach (KiAktion a in R().Alle)
            {
                bool darf = KiRiegel.DarfDirektLaufen(a);
                Assert.Equal(a.Stufe == Schutzstufe.Lesen, darf);
            }
        }

        [Fact]
        public void MitAngehobenerGrenzeLaeuftAuchStufeZwei()
        {
            // So sieht es mit der Bestaetigungsschicht der Etappe 3 aus: die Grenze wird
            // NUR zusammen mit dem Klick angehoben, nie allein.
            KiAufruf a = Aufruf("variante_anlegen", "{\"projekt_id\":7,\"bezeichner\":\"Variante B\"}");

            Assert.Null(KiRiegel.Pruefe(a, Schutzstufe.Schreiben));
            Assert.NotNull(KiRiegel.Pruefe(a, Schutzstufe.Lesen));
        }

        [Fact]
        public void OhneAufrufIstNichtsZuBeanstanden()
        {
            Assert.Null(KiRiegel.Pruefe((KiAufruf?)null));
            Assert.Null(KiRiegel.Pruefe((KiAktion?)null));
        }

        [Fact]
        public void DerGrundNenntDieStufeImKlartext()
        {
            string? grund = KiRiegel.Pruefe(R().Finde("simulation_rechnen"));

            Assert.NotNull(grund);
            Assert.Contains(KiTexte.StufeRechnen, grund!);
        }

        // ============================================================ Rundendeckel

        [Fact]
        public void DerRegeldeckelIstDrei()
        {
            Assert.Equal(3, KiWerkzeuge.Rundendeckel);
            Assert.Equal(3, new KiRunden().Deckel);
        }

        [Fact]
        public void DreiRundenGehenDannIstSchluss()
        {
            var runden = new KiRunden();

            Assert.True(runden.Beginne());
            Assert.True(runden.Beginne());
            Assert.True(runden.Beginne());
            Assert.False(runden.Beginne());

            Assert.Equal(3, runden.Verbraucht);
            Assert.False(runden.DarfWeiter);
        }

        [Fact]
        public void EineAbgelehnteRundeVerbrauchtNichts()
        {
            var runden = new KiRunden(1);

            Assert.True(runden.Beginne());
            Assert.False(runden.Beginne());
            Assert.False(runden.Beginne());

            Assert.Equal(1, runden.Verbraucht);
        }

        [Fact]
        public void DerAbbruchtextNenntDenDeckel()
        {
            var runden = new KiRunden(3);
            while (runden.Beginne()) { }

            string text = runden.Abbruchtext();

            Assert.Contains("3", text);
            Assert.NotEqual(KiTexte.RundendeckelErreicht, text);   // {0} ist ersetzt
        }

        [Fact]
        public void EigenerDeckelIstMoeglichAberNichtNull()
        {
            Assert.Equal(5, new KiRunden(5).Deckel);
            Assert.Throws<ArgumentOutOfRangeException>(() => new KiRunden(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new KiRunden(-1));
        }

        [Fact]
        public void DerZaehlerZeigtSeinenStand()
        {
            var runden = new KiRunden();
            runden.Beginne();

            Assert.Equal("1/3", runden.ToString());
        }
    }
}
