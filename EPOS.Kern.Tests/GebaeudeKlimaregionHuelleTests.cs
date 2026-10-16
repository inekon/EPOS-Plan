using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>N5: „Klimaregion wählen…“ im Gebäudedialog</b> — der Weg der Hülle (<c>GebaeudeHuelle.KlimaregionWeg</c>) zeigt die
    /// Liste der Kopfleiste, ist mit der Stammregion des Projekts vorbelegt und schreibt über den Speicherweg der Kopfleiste;
    /// ein ungültiger Wunsch kommt mit benanntem Grund zurück. Gerechnet auf der Kopie der Testdatenbank.
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeKlimaregionHuelleTests
    {
        private const int PROJEKT = 1030;

        [Fact]
        public void Liste_der_Kopfleiste_vorbelegt_mit_der_Region_des_Projekts_und_Speichern_schreibt()
        {
            using (TestDatenbank db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                GebaeudeKlimaregionWeg weg = GebaeudeHuelle.KlimaregionWeg(PROJEKT);
                Assert.NotNull(weg);
                Assert.Equal(StartseiteCtrl.KlimaregionenMitId().ToList(), weg.Eintraege.ToList());
                int vorher = StartseiteCtrl.ProjektKlimaregionStammId(PROJEKT);
                Assert.True(vorher > 0);
                Assert.Equal(vorher, weg.Gewaehlt);
                // Nachzug N6: die Herkunftszeile der Region ist derselbe Satz wie im Projektassistenten.
                Assert.Equal(EPOS.UI.Seiten.Start.KlimaHerkunftGaben.Zeile(ProjektKopfHuelle.Herkunft(vorher),
                                 MyResource.Resource.START_KLIMA_HERKUNFT, MyResource.Resource.START_KLIMA_HERKUNFT_KURZ),
                             weg.Herkunftszeile(vorher));
                Assert.NotEqual("", weg.Herkunftszeile(vorher));

                int andere = weg.Eintraege.First(e => e.Id != vorher).Id;
                Assert.Null(weg.Speichern(andere));
                Assert.Equal(andere, StartseiteCtrl.ProjektKlimaregionStammId(PROJEKT));
                Assert.Equal(andere, GebaeudeHuelle.KlimaregionWeg(PROJEKT).Gewaehlt);
            }
        }

        [Fact]
        public void Gegenprobe_ohne_Region_benannter_Grund_und_ohne_Projekt_kein_Weg()
        {
            using (TestDatenbank db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                int vorher = StartseiteCtrl.ProjektKlimaregionStammId(PROJEKT);
                GebaeudeKlimaregionWeg weg = GebaeudeHuelle.KlimaregionWeg(PROJEKT);
                Assert.Equal(WindowsFormsApplication1.MyResource.Resource.Text_Form_Start_KlimaregionAuswaehlen, weg.Speichern(0));
                Assert.Equal(vorher, StartseiteCtrl.ProjektKlimaregionStammId(PROJEKT));
                Assert.Null(GebaeudeHuelle.KlimaregionWeg(0));
            }
        }
    }
}
