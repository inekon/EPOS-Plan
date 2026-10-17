using System.Data;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die eine Leseklasse der Katalogkosten</b> (<see cref="KatalogkostensatzCtrl"/>): Satz je
    /// Bemessungsgröße und Betrag für ein Stück lesen dieselbe Investitions-Satzvorlage. Hier die
    /// Leerfälle, die ohne Datenbank entschieden sind — kein Gewerk, keine Zeile, kein Verweis:
    /// lieber keine Zahl als eine geratene.
    /// </summary>
    public class KatalogkostensatzCtrlTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Tab_Unbekannt")]
        public void Ohne_Gewerk_gibt_es_weder_Satz_noch_Betrag_noch_Einheit(string tabelle)
        {
            Assert.Null(KatalogkostensatzCtrl.Satz(tabelle, 1));
            Assert.Equal("", KatalogkostensatzCtrl.SatzText(tabelle, 1));
            Assert.Equal("", KatalogkostensatzCtrl.Einheit(tabelle));
            Assert.Equal(0, KatalogkostensatzCtrl.Betrag(tabelle, Zeile(5)));
            Assert.Equal("", KatalogkostensatzCtrl.BetragText(tabelle, Zeile(5)));
        }

        [Fact]
        public void Ohne_Zeile_oder_ohne_Verweis_bleibt_der_Betrag_leer()
        {
            string tabelle = GeraetekostenEntfallSchema.GEWERKE[0].Katalog;
            Assert.Equal(0, KatalogkostensatzCtrl.Betrag(tabelle, null));
            Assert.Equal(0, KatalogkostensatzCtrl.Betrag(tabelle, Zeile(null)));
            Assert.Equal(0, KatalogkostensatzCtrl.Betrag(tabelle, Zeile(0)));
            Assert.Equal("", KatalogkostensatzCtrl.BetragText(tabelle, Zeile(0)));
        }

        [Fact]
        public void Ohne_Verweis_oder_ohne_Vorlage_gibt_es_keinen_Satz()
        {
            string tabelle = GeraetekostenEntfallSchema.GEWERKE[0].Katalog;
            Assert.Null(KatalogkostensatzCtrl.Satz(tabelle, 0));
            Assert.Null(KatalogkostensatzCtrl.SatzDerVorlage(0));
            Assert.Equal("", KatalogkostensatzCtrl.SatzTextDerVorlage(-1));
        }

        private static DataRow Zeile(int? idVorlage)
        {
            var t = new DataTable();
            t.Columns.Add(KatalogkostenInvestitionSchema.SPALTE_ID_KOSTENVORLAGE_INVESTITION, typeof(int));
            DataRow r = t.NewRow();
            if (idVorlage.HasValue) r[0] = idVorlage.Value;
            t.Rows.Add(r);
            return r;
        }
    }
}
