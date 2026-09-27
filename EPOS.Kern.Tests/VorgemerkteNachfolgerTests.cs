using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Vorgemerkte Bildnamen, deren Bild inzwischen unter einem Katalogschlüssel besteht: Der Prüfer nennt den geltenden
    /// Schlüssel statt „erst in einer späteren Programmfassung“; die übrigen vorgemerkten Namen bleiben beim alten Hinweis.
    /// </summary>
    public class VorgemerkteNachfolgerTests
    {
        [Fact]
        public void Jeder_Nachfolger_ist_ein_Bildschluessel_und_der_alte_Name_bleibt_vorgemerkt()
        {
            Assert.NotEmpty(Vorlagenfeldkatalog.VorgemerkteNachfolger);
            foreach (var paar in Vorlagenfeldkatalog.VorgemerkteNachfolger)
            {
                Assert.Contains(paar.Key, Vorlagenfeldkatalog.VorgemerkteBilder);
                Vorlagenfeld f = Vorlagenfeldkatalog.Finde(paar.Value);
                Assert.True(f != null, paar.Value);
                Assert.Equal(Vorlagenfeldart.Bild, f.Art);
                Assert.Equal(Vorlagenfeldkontext.Stand, f.Kontext);
            }
        }

        [Fact]
        public void Der_Pruefer_nennt_den_Nachfolger()
        {
            using var k = new Kulturvorrichtung();
            Pruefbefund mit = Vorlagenpruefer.Pruefe(Probevorlagen.AusAbsaetzen("{{bild.ergebnis.streuwolke}}"), Pruefstufe.Schnell, new Pruefkontext());
            Assert.Contains(mit.Meldungen, m => (m.WasTun ?? "").Contains("{{stand.bild.waermepumpe_streuwolke}}")
                                             && (m.WasTun ?? "").Contains("{{#je stand}}"));

            Pruefbefund ohne = Vorlagenpruefer.Pruefe(Probevorlagen.AusAbsaetzen("{{bild.kosten.profil}}"), Pruefstufe.Schnell, new Pruefkontext());
            Assert.DoesNotContain(ohne.Meldungen, m => (m.WasTun ?? "").Contains("{{stand.bild."));
            Assert.Contains(ohne.Meldungen, m => m.Stufe == Befundstufe.Fehler);
        }
    }
}
