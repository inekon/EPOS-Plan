using System.Linq;
using KiKern;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Energiestandard</b> (Entscheid E47, Konzept Baualtersklassen 3.2): elf sprachneutrale
    /// Codes, Texte aus den Ressourcen in beiden Sprachen, der Filter nach der Verwendung
    /// (Effizienzhaus 115/100 und 85 nur für Wohngebäude) und das Feld des Assistenten.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class EnergiestandardTests
    {
        [Fact]
        public void Die_elf_Codes_stehen_in_der_Reihenfolge_des_Konzepts()
        {
            Assert.Equal(new[]
            {
                "TEILSANIERT", "SANIERT", "NIEDRIGENERGIE", "EH115_100", "EH85", "EH70", "EH55", "EH40", "DENKMAL",
                "PASSIVHAUS", "NULLEMISSION"
            }, Energiestandard.CODES);
            Assert.All(Energiestandard.CODES, c => Assert.True(Energiestandard.Gueltig(c)));
            Assert.False(Energiestandard.Gueltig(""));
            Assert.False(Energiestandard.Gueltig(null));
            Assert.False(Energiestandard.Gueltig("eh55"));
            Assert.Equal(6, Energiestandard.Index(Energiestandard.EH55));
            Assert.Equal(Energiestandard.EH55, Energiestandard.Code(6));
            Assert.Null(Energiestandard.Code(null));
            Assert.Null(Energiestandard.Code(11));
        }

        /// <summary>Effizienzhaus 115/100 und 85 gibt es nur für Wohngebäude; leer passt immer.</summary>
        [Fact]
        public void Die_Liste_folgt_der_Verwendung()
        {
            Assert.Equal(11, Energiestandard.Codes(GebaeudeStammCtrl.FILTERWERT_WOHN).Count);
            Assert.Equal(11, Energiestandard.Codes("").Count);
            var nichtwohnen = Energiestandard.Codes(GebaeudeStammCtrl.FILTERWERT_SONSTIGE);
            Assert.Equal(9, nichtwohnen.Count);
            Assert.DoesNotContain(Energiestandard.EH85, nichtwohnen);
            Assert.DoesNotContain(Energiestandard.EH115_100, nichtwohnen);
            Assert.Contains(Energiestandard.EH70, nichtwohnen);
            Assert.True(Energiestandard.PasstZu(null, GebaeudeStammCtrl.FILTERWERT_SONSTIGE));
            Assert.False(Energiestandard.PasstZu(Energiestandard.EH85, GebaeudeStammCtrl.FILTERWERT_SONSTIGE));
            Assert.True(Energiestandard.PasstZu(Energiestandard.EH85, GebaeudeStammCtrl.FILTERWERT_WOHN));
        }

        [Fact]
        public void Die_Texte_kommen_aus_den_Ressourcen_in_beiden_Sprachen()
        {
            using (new Kulturvorrichtung())
            {
                Assert.Equal("Effizienzhaus/-gebäude 55", Energiestandard.Text(Energiestandard.EH55));
                Assert.Equal("Passivhaus bzw. EnerPHit", Energiestandard.Text(Energiestandard.PASSIVHAUS));
                Assert.Equal("wie Baualtersklasse (unsaniert)", Energiestandard.KeinerText());
                Assert.All(Energiestandard.CODES, c => Assert.Equal(Energiestandard.TextDeutsch(c), Energiestandard.Text(c)));
                Assert.Equal("", Energiestandard.Text(null));
                Assert.Equal("EH155", Energiestandard.Text("EH155"));   // ein Datenfehler bleibt sichtbar
            }
            using (new Kulturvorrichtung("en-US"))
            {
                Assert.Equal("Efficiency House/Building 55", Energiestandard.Text(Energiestandard.EH55));
                Assert.Equal("zero-emission building", Energiestandard.Text(Energiestandard.NULLEMISSION));
                Assert.Equal("as building age class (unrefurbished)", Energiestandard.KeinerText());
                Assert.Equal("Energy standard:", R.GEBK_LBL_ENERGIESTANDARD);
            }
        }

        /// <summary>Katalogeditor und Gebäudeverwaltung führen das Feld im Assistenten als Wahl, leer erlaubt.</summary>
        [Theory]
        [InlineData(KiMaskennamen.GEBAEUDE_KATALOG)]
        [InlineData(KiMaskennamen.GEBAEUDE_ADMIN)]
        public void Der_Assistent_fuehrt_den_Energiestandard_als_Wahl(string maske)
        {
            using var kultur = new Kulturvorrichtung();
            KiDialog dialog = KiDialoge.Katalog.Finde(maske);
            KiDialogFeld feld = dialog.FindeFeld("energiestandard");
            Assert.NotNull(feld);
            Assert.Equal("GebaeudeKatalogKiSicht.Energiestandard", feld.Eigenschaftspfad);
            Assert.Equal(KiParameterTyp.Wahl, feld.Typ);
            Assert.True(feld.LeerErlaubt);
            Assert.Equal(R.GEBK_LBL_ENERGIESTANDARD, feld.Anzeigename);
            Assert.True(dialog.Felder.ToList().FindIndex(f => f.Name == "energiestandard") >
                        dialog.Felder.ToList().FindIndex(f => f.Name == "verwendung"));
        }
    }
}
