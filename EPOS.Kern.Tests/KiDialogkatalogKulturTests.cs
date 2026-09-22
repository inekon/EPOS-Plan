using System.Linq;
using KiKern;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der ZEUGE über den Kulturschlüssel des Dialogkatalogs.
    ///
    /// <para><b>Was er hält.</b> <c>KiDialoge.Katalog</c> führt JE KULTUR einen eigenen
    /// Katalog. Die Anzeigenamen seiner Einträge kommen übersetzt aus
    /// <c>MyResource.Resource</c>; ein einziger Katalog je Prozess trüge deshalb auf
    /// Dauer die Sprache seines ERSTEN Zugriffs — im Programm folgte er keinem
    /// Sprachwechsel zur Laufzeit, im Testlauf entschiede die Reihenfolge der Fälle
    /// darüber, in welcher Sprache er dasteht.</para>
    ///
    /// <para><b>Woran es festgemacht ist.</b> An zwei Texten, die in den beiden
    /// <c>.resx</c> verschieden stehen: am Maskennamen <c>KI_DLG_MASKE_HEIZKESSEL</c>
    /// („Heizkessel bearbeiten" / „Edit boiler") und an der Feldbeschriftung
    /// <c>KI_DLG_HK_LEISTUNG_NAME</c> („Thermische Leistung" / „Thermal output"). Die
    /// Kultur setzt in jedem Fall die <see cref="Kulturvorrichtung"/> des Hauses — sie
    /// pinnt alle VIER Werte und stellt jeden aus seinem eigenen Merkwert zurück; roh
    /// gesetzt würde sie dem nächsten Fall die Sprache verstellen.</para>
    /// </summary>
    public class KiDialogkatalogKulturTests
    {
        /// <summary>Der Anzeigename der Heizkesselmaske im übergebenen Katalog.</summary>
        private static string Maskenname(KiDialogKatalog katalog)
        {
            return katalog.Finde(KiMaskennamen.HEIZKESSEL).Anzeigename;
        }

        /// <summary>Die Beschriftung des Feldes „th_leistung" im übergebenen Katalog.</summary>
        private static string Feldname(KiDialogKatalog katalog)
        {
            return katalog.Finde(KiMaskennamen.HEIZKESSEL)
                          .Felder.First(f => f.Name == "th_leistung").Anzeigename;
        }

        /// <summary>
        /// Unter <c>de-DE</c> und unter <c>en-US</c> steht je ein EIGENER Katalog, und
        /// jeder trägt die Namen seiner Sprache.
        /// </summary>
        [Fact]
        public void Je_Kultur_ein_Katalog_mit_den_Namen_seiner_Sprache()
        {
            KiDialogKatalog deutsch;
            using (new Kulturvorrichtung("de-DE")) deutsch = KiDialoge.Katalog;

            KiDialogKatalog englisch;
            using (new Kulturvorrichtung("en-US")) englisch = KiDialoge.Katalog;

            Assert.NotSame(deutsch, englisch);

            Assert.Equal("Heizkessel bearbeiten", Maskenname(deutsch));
            Assert.Equal("Edit boiler", Maskenname(englisch));

            Assert.Equal("Thermische Leistung", Feldname(deutsch));
            Assert.Equal("Thermal output", Feldname(englisch));
        }

        /// <summary>
        /// Dieselbe Kultur bekommt DENSELBEN Katalog — kein Neubau je Zugriff.
        /// </summary>
        [Fact]
        public void Dieselbe_Kultur_bekommt_denselben_Katalog()
        {
            using var _ = new Kulturvorrichtung("de-DE");

            KiDialogKatalog erster = KiDialoge.Katalog;
            KiDialogKatalog zweiter = KiDialoge.Katalog;

            Assert.Same(erster, zweiter);
        }

        /// <summary>
        /// Ein Wechsel nach <c>en-US</c> und zurück liefert wieder die deutschen Namen —
        /// und zwar denselben Katalog wie vorher.
        /// </summary>
        [Fact]
        public void Ein_Kulturwechsel_hin_und_zurueck_liefert_wieder_die_deutschen_Namen()
        {
            KiDialogKatalog vorher;
            using (new Kulturvorrichtung("de-DE")) vorher = KiDialoge.Katalog;

            using (new Kulturvorrichtung("en-US"))
            {
                Assert.Equal("Edit boiler", Maskenname(KiDialoge.Katalog));
            }

            using (new Kulturvorrichtung("de-DE"))
            {
                Assert.Same(vorher, KiDialoge.Katalog);
                Assert.Equal("Heizkessel bearbeiten", Maskenname(KiDialoge.Katalog));
            }
        }
    }
}
