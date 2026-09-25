using System;
using EPOS.UI.Dialoge.Bedarf;
using KiKern;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Baujahr an der Oberfläche</b> (Stufe G4a, Schemaschritt <see cref="BaujahrSchema.SCHRITT"/>):
    /// Der Katalogeditor bildet das Feld NULL-erhaltend ab, die Prüfung steht im Arbeitsstand mit den
    /// Grenzen der Spalte, die Beschriftungen trennen Baualtersklasse und Baujahr (keine zweite
    /// Bedeutung eines Schlüssels), und der Assistent führt das Feld in beiden Gebäudemasken.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class BaujahrOberflaecheTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Katalogsatz → Feldsatz → Katalogsatz: das Jahr reist mit, leer bleibt leer — nie 0.</summary>
        [Fact]
        public void Der_Katalogeditor_bildet_das_Baujahr_NULL_erhaltend_ab()
        {
            var satz = new GebaeudeModel { Gebaeudename = "Baujahrprobe", Baujahr = 1965 };

            GebaeudeKatalogDaten d = GebaeudeKatalogHuelle.AusModell(satz);
            Assert.Equal(1965, d.Baujahr);
            Assert.Equal(1965, d.Kopie().Baujahr);
            Assert.Equal(1965, GebaeudeKatalogHuelle.NachModell(d, new GebaeudeModel()).Baujahr);

            // "Speichern unter" auf einen leeren Satz: das leere Feld überschreibt den alten Wert mit NULL.
            GebaeudeModel leer = GebaeudeKatalogHuelle.NachModell(GebaeudeKatalogHuelle.AusModell(new GebaeudeModel()), satz);
            Assert.Null(leer.Baujahr);
        }

        /// <summary>
        /// Die Regel steht einmal im Arbeitsstand: leer ist erlaubt, 1500 und 2100 auch, darunter und
        /// darüber meldet sie — mit den Grenzen der Spalte.
        /// </summary>
        [Theory]
        [InlineData(null, false)]
        [InlineData(1500, false)]
        [InlineData(2100, false)]
        [InlineData(1499, true)]
        [InlineData(2101, true)]
        public void Der_Arbeitsstand_prueft_das_Baujahr_gegen_die_Grenzen_der_Spalte(int? jahr, bool meldet)
        {
            var arbeit = new GebaeudeArbeitsstand();
            arbeit.Laden(new GebaeudeKatalogDaten { Name = "Probe", Baujahr = jahr }, neu: false);

            GebaeudePrueftexte p = GebaeudeKatalogHuelle.Prueftexte();
            GebaeudePruefbefund befund = arbeit.Pruefen(false, p, GebaeudeKatalogHuelle.Texte());

            string erwartet = "Das Baujahr muss zwischen 1500 und 2100 liegen.";
            if (meldet) Assert.Equal(erwartet, befund?.Meldung);
            else Assert.NotEqual(erwartet, befund?.Meldung);
        }

        /// <summary>
        /// Die Texte kommen aus den Ressourcen, in beiden Sprachen, und trennen die zwei Felder: Die
        /// Klappliste heißt „Baualtersklasse", „Baujahr" gehört allein dem Jahresfeld.
        /// </summary>
        [Fact]
        public void Baualtersklasse_und_Baujahr_haben_je_einen_eigenen_Schluessel()
        {
            Assert.Equal("Baualtersklasse :", R.GEBK_LBL_BAUALTERSKLASSE);
            Assert.Equal("Baujahr :", R.GEBK_LBL_BAUJAHR);
            Assert.Equal("Baualtersklasse", R.GEBA_LBL_BAUALTERSKLASSE);
            Assert.Equal("Baujahr", R.GEBA_LBL_BAUJAHR);

            GebaeudePrueftexte p = GebaeudeKatalogHuelle.Prueftexte();
            Assert.Equal("Baujahr", p.FeldBaujahr);
            Assert.Equal(R.GEBK_MSG_BAUJAHR, p.MeldungBaujahr);

            var en = System.Globalization.CultureInfo.GetCultureInfo("en-US");
            Assert.Equal("Building age class:", R.ResourceManager.GetString("GEBK_LBL_BAUALTERSKLASSE", en));
            Assert.Equal("Year of construction:", R.ResourceManager.GetString("GEBK_LBL_BAUJAHR", en));
            Assert.Equal("Building age class", R.ResourceManager.GetString("GEBA_LBL_BAUALTERSKLASSE", en));
            Assert.Equal("Year of construction", R.ResourceManager.GetString("GEBA_LBL_BAUJAHR", en));
            Assert.Contains("building age class", R.ResourceManager.GetString("KI_DLG_GEBK_BAUALTERSKLASSE_ERL", en));
        }

        /// <summary>
        /// Der Assistent führt das Baujahr in Katalogeditor und Verwaltung als Ganzzahl 1500 … 2100,
        /// leer erlaubt, unter der Beschriftung des Jahresfelds; die Baualtersklasse steht unter ihrer
        /// eigenen.
        /// </summary>
        [Theory]
        [InlineData(KiMaskennamen.GEBAEUDE_KATALOG)]
        [InlineData(KiMaskennamen.GEBAEUDE_ADMIN)]
        public void Der_Assistent_fuehrt_das_Baujahr_neben_der_Baualtersklasse(string maske)
        {
            KiDialog dialog = KiDialoge.Katalog.Finde(maske);
            Assert.NotNull(dialog);

            KiDialogFeld jahr = dialog.FindeFeld("baujahr");
            Assert.NotNull(jahr);
            Assert.Equal("GebaeudeKatalogKiSicht.Baujahr", jahr.Eigenschaftspfad);
            Assert.Equal(KiParameterTyp.Ganzzahl, jahr.Typ);
            Assert.True(jahr.LeerErlaubt);
            Assert.Equal(1500.0, jahr.Min);
            Assert.Equal(2100.0, jahr.Max);
            Assert.Equal(R.GEBK_LBL_BAUJAHR, jahr.Anzeigename);

            KiDialogFeld klasse = dialog.FindeFeld("baualtersklasse");
            Assert.Equal(R.GEBK_LBL_BAUALTERSKLASSE, klasse.Anzeigename);
            Assert.Equal(R.KI_DLG_GEBK_BAUALTERSKLASSE_ERL, klasse.Erlaeuterung);
        }
    }
}
