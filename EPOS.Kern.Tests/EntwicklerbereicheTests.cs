using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>Das Register der Entwicklerbereiche (Entwicklerhandbuch Abschnitt 2.1).</summary>
    public sealed class EntwicklerbereicheTests
    {
        [Fact]
        public void Acht_Eintraege_mit_eindeutigem_Schluessel_und_Zweck()
        {
            Assert.Equal(8, Entwicklerbereiche.Alle.Count);
            Assert.Equal(8, Entwicklerbereiche.Alle.Select(b => b.Schluessel).Distinct().Count());
            Assert.All(Entwicklerbereiche.Alle, b =>
            {
                Assert.False(string.IsNullOrWhiteSpace(b.Schluessel));
                Assert.False(string.IsNullOrWhiteSpace(b.Zweck));
            });
        }

        [Fact]
        public void Jede_Konstante_steht_im_Register()
        {
            string[] konstanten =
            {
                Entwicklerbereiche.PROJEKTDATEI_IMPORT, Entwicklerbereiche.PROJEKTDATEI_PROFILE,
                Entwicklerbereiche.PROJEKTDATEI_IOS, Entwicklerbereiche.PROJEKTDATEI_KI,
                Entwicklerbereiche.KATALOG_WIEDERHERSTELLEN, Entwicklerbereiche.KI_WERKZEUGE,
                Entwicklerbereiche.EXPORT_IDS,
                Entwicklerbereiche.WEBVIEW_WERKZEUGE,
            };
            Assert.Equal(konstanten, Entwicklerbereiche.Alle.Select(b => b.Schluessel).ToArray());

            // Wert = Name.
            foreach (var feld in typeof(Entwicklerbereiche).GetFields().Where(f => f.IsLiteral))
                Assert.Equal(feld.Name, (string)feld.GetRawConstantValue());
        }
    }
}
