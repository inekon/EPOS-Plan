using System.Linq;
using KiKern;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E10 — <b>Kennzeichnung A8</b> (Empfehlung E10‑Q4 a): Die geräteeigenen
    /// Nutzungsdauern <c>Tab_Heizkessel.Nutzungsdauer</c> und <c>Tab_BHKW.Nutzungsdauer</c>
    /// rechnen nicht; maßgeblich ist die Nutzungsdauertabelle.
    ///
    /// <para><b>Wo die Kennzeichnung steht.</b> In der Beschriftung des Katalogfeldes
    /// (Katalogbrowser und Aufklapper „Alle Daten" der Erzeugermasken), im Vermerk des
    /// Profils, den die Oberfläche als Tooltip zeigt, in der KI-Feldkarte der Verwaltung
    /// und des Aufklappers (Erläuterung mit Vermerk — das Feld bleibt setzbar wie im
    /// Katalog) und in der Parameterverwendung. Kein Schemaschritt, keine Spalte entfernt;
    /// eine neue Eingabestelle entsteht nicht, die Maskenwache bleibt unberührt.</para>
    /// </summary>
    public class NutzungsdauerKennzeichnungTests
    {
        private const string VERMERK =
            "Gerätedaten — nicht rechenwirksam; maßgeblich ist die Nutzungsdauertabelle (Nutzungsdauern (AfA)).";

        /// <summary>Beschriftung und Vermerk des Katalogfeldes; nur dieses Feld trägt einen.</summary>
        [Theory]
        [InlineData(KatalogBrowserArt.Heizkessel)]
        [InlineData(KatalogBrowserArt.Bhkw)]
        public void Das_Katalogfeld_nennt_sich_Geraetedaten_und_traegt_den_Vermerk(KatalogBrowserArt art)
        {
            using var _ = new Kulturvorrichtung();
            KatalogBrowserProfil profil = KatalogBrowserProfil.Finde(art, KiDialogTexte.Profiltext);

            BrowserDetailfeld nd = profil.Detailfelder.Single(f => f.Schluessel == KatalogBrowserProfil.FeldNutzungsdauer);
            Assert.Equal("Nutzungsdauer (Gerätedaten):", nd.Bezeichnung);
            Assert.Equal(VERMERK, nd.Hinweis);
            Assert.True(nd.Editierbar, "Die Spalte bleibt pflegbar — gekennzeichnet, nicht gesperrt.");

            Assert.All(profil.Detailfelder.Where(f => f.Schluessel != KatalogBrowserProfil.FeldNutzungsdauer),
                       f => Assert.Equal("", f.Hinweis));
        }

        /// <summary>Beide Sprachen tragen die Kennzeichnung.</summary>
        [Fact]
        public void Die_Kennzeichnung_steht_auch_englisch()
        {
            using var _ = new Kulturvorrichtung("en-US");
            BrowserDetailfeld nd = KatalogBrowserProfil.Finde(KatalogBrowserArt.Bhkw, KiDialogTexte.Profiltext)
                .Detailfelder.Single(f => f.Schluessel == KatalogBrowserProfil.FeldNutzungsdauer);

            Assert.Equal("Service life (device data):", nd.Bezeichnung);
            Assert.Contains("not used in the calculation", nd.Hinweis);
        }

        /// <summary>
        /// Die KI-Feldkarte der Verwaltung und des Aufklappers „Alle Daten": Erläuterung mit
        /// Vermerk, Anzeigename mit „Gerätedaten", setzbar wie der Katalog.
        /// </summary>
        [Theory]
        [InlineData(KatalogBrowserArt.Heizkessel, KiMaskennamen.HEIZKESSEL_PROJEKT)]
        [InlineData(KatalogBrowserArt.Bhkw, KiMaskennamen.BHKW_PROJEKT)]
        public void Die_KI_Feldkarte_traegt_den_Vermerk(KatalogBrowserArt art, string projektmaske)
        {
            using var _ = new Kulturvorrichtung();

            KiDialogFeld verwaltung = KiDialoge.Katalog.Finde(KiMaskennamen.KatalogBrowser(art))
                                              .Felder.Single(f => f.Name == "nutzungsdauer");
            Assert.Equal("Nutzungsdauer (Gerätedaten)", verwaltung.Anzeigename);
            Assert.Contains("nicht rechenwirksam", verwaltung.Erlaeuterung);
            Assert.Contains("Nutzungsdauertabelle", verwaltung.Erlaeuterung);
            Assert.False(verwaltung.NurLesen);

            KiDialogFeld aufklapper = KiDialoge.Katalog.Finde(projektmaske)
                                              .Felder.Single(f => f.Name == "katalog_nutzungsdauer");
            Assert.Equal(verwaltung.Erlaeuterung, aufklapper.Erlaeuterung);
        }

        /// <summary>Die Parameterverwendung führt die Spalte als Dialogfeld mit Vermerk — nicht gerechnet.</summary>
        [Theory]
        [InlineData(Anlagenart.Heizkessel)]
        [InlineData(Anlagenart.Bhkw)]
        public void Die_Parameterverwendung_nennt_die_Spalte_nicht_rechenwirksam(Anlagenart art)
        {
            ParameterEintrag e = ParameterVerwendung.Katalog(art).Single(x => x.Spalte == "Nutzungsdauer");

            Assert.True(e.Hat(Verwendung.Dialog));
            Assert.False(e.Gerechnet);
            Assert.Contains("nicht rechenwirksam", e.Fundstelle);
            Assert.Contains("Nutzungsdauertabelle", e.Fundstelle);
        }
    }
}
