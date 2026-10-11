using System;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Lizenzbedingung der Fernwartung (Konzept Administration und Support 4.3, AS-Q8
    /// Lesart b): bezahltes Token (<c>person</c>, <c>firma</c>) bei jedem Status ausser
    /// <c>NichtAktiviert</c> und <c>UhrManipuliert</c>; die Testversion und ein fehlendes Token
    /// bleiben gesperrt. Der Status kommt aus <see cref="LizenzManager.Bewerten"/> mit festem
    /// Datum — ohne Ablage, ohne Zeitanker.
    /// </summary>
    public sealed class FernwartungFreigabeTests : IDisposable
    {
        private const string GERAET = "SHA256:probe-geraet";
        private static readonly DateTime HEUTE = new DateTime(2026, 10, 11);

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static Fernwartungsfreigabe Frage(LizenzToken token, DateTime? anker = null)
        {
            LizenzStatus status = LizenzManager.Bewerten(token, GERAET, HEUTE, anker ?? DateTime.MinValue);
            return LizenzCtrl.FernwartungErlaubt(token, status);
        }

        private static void Frei(Fernwartungsfreigabe f)
        {
            Assert.True(f.Erlaubt);
            Assert.Equal("", f.Grund);
        }

        [Theory]
        [InlineData("person")]
        [InlineData("firma")]
        public void Gueltige_bezahlte_Lizenz_ist_erlaubt(string typ)
        {
            var token = LizenzToken.FuerPruefstand(GERAET, gueltigBis: HEUTE.AddDays(100), typ: typ);
            Assert.Equal(LizenzStatus.Gueltig, LizenzManager.Bewerten(token, GERAET, HEUTE, DateTime.MinValue));
            Frei(Frage(token));
        }

        [Fact]
        public void Abgelaufene_Firmenlizenz_im_Lesemodus_ist_erlaubt()
        {
            var token = LizenzToken.FuerPruefstand(GERAET, gueltigBis: HEUTE.AddDays(-60), kulanzTage: 14, typ: "firma");
            Assert.Equal(LizenzStatus.Lesemodus, LizenzManager.Bewerten(token, GERAET, HEUTE, DateTime.MinValue));
            Frei(Frage(token));
        }

        [Fact]
        public void Kulanz_ist_erlaubt()
        {
            var token = LizenzToken.FuerPruefstand(GERAET, gueltigBis: HEUTE.AddDays(-3), kulanzTage: 14, typ: "person");
            Assert.Equal(LizenzStatus.Kulanz, LizenzManager.Bewerten(token, GERAET, HEUTE, DateTime.MinValue));
            Frei(Frage(token));
        }

        [Fact]
        public void Faellige_Nachpruefung_ist_erlaubt()
        {
            var token = LizenzToken.FuerPruefstand(GERAET, gueltigBis: HEUTE.AddDays(100), tokenBis: HEUTE.AddDays(-2), typ: "firma");
            Assert.Equal(LizenzStatus.NachpruefungFaellig, LizenzManager.Bewerten(token, GERAET, HEUTE, DateTime.MinValue));
            Frei(Frage(token));
        }

        [Fact]
        public void Testversion_ist_gesperrt_mit_dem_Testgrund()
        {
            var token = LizenzToken.FuerPruefstand(GERAET, gueltigBis: HEUTE.AddDays(20), typ: "demo");
            Fernwartungsfreigabe f = Frage(token);
            Assert.False(f.Erlaubt);
            Assert.Equal(R.FW_SPERRE_TEST, f.Grund);
        }

        [Fact]
        public void Ohne_Token_ist_gesperrt_mit_dem_Lizenzgrund()
        {
            Fernwartungsfreigabe f = Frage(null);
            Assert.False(f.Erlaubt);
            Assert.Equal(R.FW_SPERRE_LIZENZ, f.Grund);
        }

        [Fact]
        public void Fremdes_Geraet_ist_nicht_aktiviert_und_gesperrt()
        {
            var token = LizenzToken.FuerPruefstand("SHA256:anderes-geraet", gueltigBis: HEUTE.AddDays(100), typ: "firma");
            Fernwartungsfreigabe f = Frage(token);
            Assert.False(f.Erlaubt);
            Assert.Equal(R.FW_SPERRE_LIZENZ, f.Grund);
        }

        [Fact]
        public void Manipulierte_Uhr_ist_gesperrt()
        {
            var token = LizenzToken.FuerPruefstand(GERAET, gueltigBis: HEUTE.AddDays(100), typ: "firma");
            Assert.Equal(LizenzStatus.UhrManipuliert,
                         LizenzManager.Bewerten(token, GERAET, HEUTE, HEUTE.AddDays(30)));
            Fernwartungsfreigabe f = Frage(token, HEUTE.AddDays(30));
            Assert.False(f.Erlaubt);
            Assert.Equal(R.FW_SPERRE_LIZENZ, f.Grund);
        }

        [Fact]
        public void Unbekannter_Typ_ist_gesperrt()
        {
            var token = LizenzToken.FuerPruefstand(GERAET, gueltigBis: HEUTE.AddDays(100), typ: "schule");
            Assert.False(Frage(token).Erlaubt);
        }

        [Fact]
        public void Die_Frage_ohne_Argumente_wirft_nie_und_liefert_einen_Grund_wenn_gesperrt()
        {
            Fernwartungsfreigabe f = LizenzCtrl.FernwartungErlaubt();
            Assert.NotNull(f);
            Assert.Equal(f.Erlaubt, f.Grund.Length == 0);
        }

        [Fact]
        public void Die_Seite_ist_die_vereinbarte_Adresse()
        {
            Assert.Equal("https://epos-plan.de/support/fernwartung", Fernwartung.SEITE);
        }

        [Fact]
        public void Die_Kontaktdaten_stehen_in_beiden_Sprachen()
        {
            Assert.Equal("0711 9959 826 50", R.SUP_KONTAKT_TELEFON);
            Assert.Equal("info@inekon.de", R.SUP_KONTAKT_MAIL);
            Assert.Equal("werktags 9 bis 16 Uhr", R.SUP_KONTAKT_ZEITEN);
            var en = new System.Globalization.CultureInfo("en-US");
            Assert.Equal("weekdays 9 am to 4 pm", R.ResourceManager.GetString("SUP_KONTAKT_ZEITEN", en));
            Assert.NotEqual(R.FW_SPERRE_TEST, R.ResourceManager.GetString("FW_SPERRE_TEST", en));
        }
    }
}
