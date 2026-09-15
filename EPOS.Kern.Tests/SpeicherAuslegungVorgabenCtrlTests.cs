using System;
using System.Globalization;
using System.Threading;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// DIE LEISTUNGSPREIS-QUELLEN der Stromspeicher-Auslegung
    /// (Anwenderentscheid W11b-E-3, 10.09.2026).
    ///
    /// <para><b>Was geprueft wird.</b> Die eine Fachentscheidung, die der Controller
    /// selbst trifft: welche Stufe der zweistufigen Leistungspreis-Staffel an der
    /// BEZUGSSPITZE greift. Eine Kappung nimmt die Leistung immer von oben weg — die
    /// erste eingesparte Kilowatt ist deshalb die teuerste, und angeboten wird der
    /// Preis der Stufe, in der die Spitze liegt. Ohne bekannte Spitze sagt der Text
    /// es.</para>
    ///
    /// <para><b>Ohne Datenbank.</b> <c>TarifQuelle</c> bekommt den Tarifsatz als
    /// Parameter; der Leseweg zu Variante, Tarifsatz und Energietraeger ist
    /// Datenbanksache und steckt in <c>Leistungspreisquellen</c>, von dem hier nur der
    /// Fall "kein Projekt" geprueft ist.</para>
    /// </summary>
    public sealed class SpeicherAuslegungVorgabenCtrlTests : IDisposable
    {
        private readonly CultureInfo _vorher = CultureInfo.CurrentCulture;
        private readonly CultureInfo _vorherUi = CultureInfo.CurrentUICulture;

        public SpeicherAuslegungVorgabenCtrlTests()
        {
            CultureInfo de = new CultureInfo("de-DE");
            CultureInfo.CurrentCulture = de;
            Thread.CurrentThread.CurrentCulture = de;
            CultureInfo.CurrentUICulture = de;
            Thread.CurrentThread.CurrentUICulture = de;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            CultureInfo.CurrentCulture = _vorher;
            Thread.CurrentThread.CurrentCulture = _vorher;
            CultureInfo.CurrentUICulture = _vorherUi;
            Thread.CurrentThread.CurrentUICulture = _vorherUi;
        }

        private static TarifParameter Tarif(double grenze, double preis1, double preis2)
            => new TarifParameter
            {
                StaffelGrenzeKW = grenze,
                StaffelPreis1EurKW = preis1,
                StaffelPreis2EurKW = preis2
            };

        [Fact]
        public void Eine_Spitze_Ueber_Der_Grenze_Nimmt_Die_Zweite_Stufe()
        {
            SpeicherOptimierungLeistungspreisQuelle q =
                SpeicherAuslegungVorgabenCtrl.TarifQuelle(Tarif(500.0, 90.0, 120.0), 751.0);

            Assert.NotNull(q);
            Assert.Equal(120.0, q.WertEurProKwA, 9);
            Assert.Contains("751", q.Bezeichnung, StringComparison.Ordinal);
            Assert.Contains("500", q.Bezeichnung, StringComparison.Ordinal);
        }

        [Fact]
        public void Eine_Spitze_Innerhalb_Der_Grenze_Nimmt_Die_Erste_Stufe()
        {
            SpeicherOptimierungLeistungspreisQuelle q =
                SpeicherAuslegungVorgabenCtrl.TarifQuelle(Tarif(1000.0, 90.0, 120.0), 751.0);

            Assert.NotNull(q);
            Assert.Equal(90.0, q.WertEurProKwA, 9);
        }

        [Fact]
        public void Ohne_Zweite_Stufe_Gilt_Die_Erste_Auch_Oberhalb()
        {
            SpeicherOptimierungLeistungspreisQuelle q =
                SpeicherAuslegungVorgabenCtrl.TarifQuelle(Tarif(500.0, 90.0, 0.0), 751.0);

            Assert.NotNull(q);
            Assert.Equal(90.0, q.WertEurProKwA, 9);
        }

        [Fact]
        public void Ohne_Bekannte_Spitze_Sagt_Der_Text_Es()
        {
            SpeicherOptimierungLeistungspreisQuelle q =
                SpeicherAuslegungVorgabenCtrl.TarifQuelle(Tarif(500.0, 90.0, 120.0), 0.0);

            Assert.NotNull(q);
            Assert.Equal(90.0, q.WertEurProKwA, 9);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.OPT_QUELLE_TARIF_OHNE_SPITZE, q.Bezeichnung,
                            StringComparison.Ordinal);
        }

        [Fact]
        public void Ohne_Gepflegte_Staffel_Gibt_Es_Keine_Quelle()
        {
            Assert.Null(SpeicherAuslegungVorgabenCtrl.TarifQuelle(Tarif(500.0, 0.0, 0.0), 751.0));
            Assert.Null(SpeicherAuslegungVorgabenCtrl.TarifQuelle(null, 751.0));
        }

        [Fact]
        public void Ohne_Projekt_Gibt_Es_Keine_Quellen()
        {
            Assert.Empty(SpeicherAuslegungVorgabenCtrl.Leistungspreisquellen(0, 751.0));
        }
    }
}
