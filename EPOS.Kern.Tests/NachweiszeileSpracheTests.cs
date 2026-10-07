using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Festtexte der Nachweiszeile (<see cref="WirtschaftlichkeitParameter.Nachweis"/>) folgen der Sprache der
    /// übergebenen Berichtskultur — Zahlen und Datum ebenso.
    ///
    /// <para><b>Rote Probe (KP3-A1d):</b> Außer dem Stromsteuer-Vermerk (KP3-A1b) stand die Zeile deutsch im englischen
    /// Bericht („Preissteigerung Energie“, „wie Betrieb“, „Einspeisevergütung“, „Energiesteuer“, „Unternehmensart“) und
    /// nannte die Steuerwerte der Datenbank (<c>PARAGRAF_53</c>, <c>PROD_GEWERBE</c>) statt ihrer Anzeigenamen. Ohne
    /// Projekt (<c>IdStamm</c> 0) ohne Datenbank — der KWKG-Teil braucht eine aktive Anlage und steht hier nicht.</para>
    /// </summary>
    public class NachweiszeileSpracheTests
    {
        private static WirtschaftlichkeitParameter Parameter() => new WirtschaftlichkeitParameter
        {
            Zinssatz = 3.0,
            Betrachtungszeitraum = 20,
            PreissteigerungEnergie = 2.5,
            PreissteigerungBetrieb = 1.5,
            Einspeiseverguetung = 0.082,
            EinspeiseverguetungKWK = 0.04,
            EnergiesteuerWahl = DbWerte.ENERGIESTEUER_WAHL_53,
            AufteilungMethode = DbWerte.AUFTEILUNG_ENERGETISCH,
            Jahresnutzungsgrad = 75.5,
            Unternehmensart = DbWerte.UNTERNEHMENSART_PROD_GEWERBE
        };

        [Fact]
        public void Englische_Zeile_ohne_deutsche_Festtexte()
        {
            string zeile = Parameter().Nachweis(CultureInfo.GetCultureInfo("en-US"));
            Assert.StartsWith("i = 3.0 % · T = 20 a · Energy price increase 2.5 %/a, operation 1.5 %/a, " +
                              "investment/replacement 1.5 %/a (as operating) · Feed-in tariff 0.082 €/kWh", zeile);
            Assert.Contains(" · Energy tax sec. 53 EnergieStG (form 1131) (energy-based (conservative)), annual utilisation rate 75.5 %", zeile);
            Assert.Contains(" · Type of business manufacturing business", zeile);
            Assert.Contains(" · CHP feed-in tariff 0.040 €/kWh", zeile);
            foreach (string deutsch in new[] { "Preissteigerung", "wie Betrieb", "Einspeisevergütung", "Energiesteuer",
                                               "Unternehmensart", "Nutzungsgrad", "PARAGRAF_53", "PROD_GEWERBE" })
                Assert.DoesNotContain(deutsch, zeile);
        }

        [Fact]
        public void Deutsche_Zeile_bleibt_im_Wortlaut()
        {
            string zeile = Parameter().Nachweis(CultureInfo.GetCultureInfo("de-DE"));
            Assert.StartsWith("i = 3,0 % · T = 20 a · Preissteigerung Energie 2,5 %/a, Betrieb 1,5 %/a, " +
                              "Investition/Ersatz 1,5 %/a (wie Betrieb) · Einspeisevergütung 0,082 €/kWh", zeile);
            Assert.Contains(" · Energiesteuer § 53 EnergieStG (Formular 1131) (energetisch (konservativ)), Nutzungsgrad 75,5 %", zeile);
            Assert.Contains(" · Unternehmensart produzierendes Gewerbe", zeile);
            Assert.Contains(" · Einspeisevergütung KWK 0,040 €/kWh", zeile);
        }

        [Theory]
        [InlineData("en-US", "maintained")]
        [InlineData("de-DE", "gepflegt")]
        public void Gepflegtes_p_I_nennt_seine_Herkunft_in_der_Berichtssprache(string kultur, string herkunft)
        {
            WirtschaftlichkeitParameter p = Parameter();
            p.PreissteigerungInvestition = 2.0;
            Assert.Contains("(" + herkunft + ")", p.Nachweis(CultureInfo.GetCultureInfo(kultur)));
        }
    }
}
