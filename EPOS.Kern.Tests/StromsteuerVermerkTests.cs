using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Stromsteuer-Vermerk der Nachweiszeile (<see cref="WirtschaftlichkeitParameter.Nachweis"/>) folgt der Sprache der
    /// übergebenen Berichtskultur.
    ///
    /// <para><b>Rote Probe (KP3-A1b):</b> Der Vermerk stand rein deutsch im Bericht („Stromsteuer: hocheffizient ja,
    /// räumlicher Zusammenhang nein“) — auch im englischen. Er kommt jetzt aus einer Ressource beider Sprachen; die
    /// Wahrheitswerte aus den vorhandenen Ja/Nein-Texten. Ohne Projekt (<c>IdStamm</c> 0) ohne Datenbank.</para>
    /// </summary>
    public class StromsteuerVermerkTests
    {
        private static WirtschaftlichkeitParameter Parameter(bool hocheffizient, bool raeumlich) => new WirtschaftlichkeitParameter
        {
            Zinssatz = 3.0,
            Betrachtungszeitraum = 20,
            HocheffizienzNachweis = hocheffizient,
            RaeumlicherZusammenhang = raeumlich
        };

        [Theory]
        [InlineData("en-US", " · Electricity tax: high-efficiency yes, spatial connection no")]
        [InlineData("de-DE", " · Stromsteuer: hocheffizient ja, räumlicher Zusammenhang nein")]
        public void Vermerk_folgt_der_Berichtssprache(string kultur, string erwartet)
        {
            string zeile = Parameter(true, false).Nachweis(CultureInfo.GetCultureInfo(kultur));
            Assert.Contains(erwartet, zeile);
        }

        [Fact]
        public void Ohne_Angabe_kein_Vermerk()
        {
            Assert.DoesNotContain("Stromsteuer", Parameter(false, false).Nachweis(CultureInfo.GetCultureInfo("de-DE")));
            Assert.DoesNotContain("Electricity tax", Parameter(false, false).Nachweis(CultureInfo.GetCultureInfo("en-US")));
        }
    }
}
