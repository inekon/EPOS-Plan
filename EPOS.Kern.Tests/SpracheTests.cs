using Xunit;
using WindowsFormsApplication1;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Berichtssprache haengt seit iU4-1 an <see cref="Sprache.Nummer"/> statt an
    /// <c>Program</c>. Geprueft wird, dass <c>BerichtTexte.T</c> dieser Nummer folgt.
    ///
    /// <para>Beide Faelle stehen in EINEM Test, weil die Nummer prozessweiter Zustand
    /// ist: So ist sichergestellt, dass sie am Ende wieder auf 0 (deutsch) steht,
    /// gleich in welcher Reihenfolge xunit die Tests abarbeitet.</para>
    /// </summary>
    public class SpracheTests
    {
        [Fact]
        public void BerichtTexte_folgen_der_Sprachnummer()
        {
            int vorher = Sprache.Nummer;
            try
            {
                Sprache.Nummer = 1;
                Assert.True(Sprache.Englisch);
                Assert.True(BerichtTexte.Englisch);
                Assert.Equal("Contents", BerichtTexte.T("Inhalt"));

                Sprache.Nummer = 0;
                Assert.False(Sprache.Englisch);
                Assert.False(BerichtTexte.Englisch);
                Assert.Equal("Inhalt", BerichtTexte.T("Inhalt"));
            }
            finally
            {
                Sprache.Nummer = vorher;
            }
        }

        [Fact]
        public void Unbekannter_Text_laeuft_unveraendert_durch()
        {
            int vorher = Sprache.Nummer;
            try
            {
                Sprache.Nummer = 1;
                Assert.Equal("Kesselhaus Nord", BerichtTexte.T("Kesselhaus Nord"));
            }
            finally
            {
                Sprache.Nummer = vorher;
            }
        }
    }
}
