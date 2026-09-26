using System;
using System.IO;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Ein gesperrter Ordner wird benannt (<see cref="OrdnerGesperrtException"/>): Zielordner des Berichts, Vorlagenordner und
    /// Muster nennen den Ordner und den Weg zur Freigabe (Überwachter Ordnerzugriff) statt der rohen Ausnahme — in beiden
    /// Sprachen. Den verweigerten Zugriff selbst stellt kein Test her (der Läufer darf überall schreiben).
    /// </summary>
    public class OrdnerGesperrtTests
    {
        [Theory]
        [InlineData("de-DE", "Überwachte Ordnerzugriff", "Einstellungen › Bericht")]
        [InlineData("en-US", "Controlled folder access", "Settings › Report")]
        public void Die_Meldungen_nennen_Ordner_und_Freigabe(string kultur, string freigabe, string einstellung)
        {
            using var k = new Kulturvorrichtung(kultur);
            string ordner = Path.Combine("C:", "Users", "Anwender", "Documents");
            foreach (string m in new[] { OrdnerGesperrtException.Zielordner(ordner), OrdnerGesperrtException.Vorlagenordner(ordner) })
            {
                Assert.Contains(ordner, m);
                Assert.Contains(freigabe, m);
                Assert.Contains("EPOS_Plan.exe", m);
                Assert.Contains(einstellung, m);
            }
        }

        [Fact]
        public void Nur_ein_verweigerter_Zugriff_gilt_als_gesperrt()
        {
            var innen = new UnauthorizedAccessException("Access to the path is denied.");
            var ex = new OrdnerGesperrtException("/x", "benannt", innen);
            Assert.True(OrdnerGesperrtException.IstGesperrt(innen));
            Assert.True(OrdnerGesperrtException.IstGesperrt(ex));
            Assert.Same(innen, ex.InnerException);
            Assert.Equal("/x", ex.Ordner);
            Assert.Equal("benannt", ex.Message);
            Assert.False(OrdnerGesperrtException.IstGesperrt(new IOException("gesperrt")));
            Assert.False(OrdnerGesperrtException.IstGesperrt(null));
        }

        [Fact]
        public void Eine_gesperrte_Musterdatei_ist_ein_Schreibfehler()
        {
            var d = new Musterdatei("a.docx", Musterzustand.Fehler, "a.docx: verweigert", gesperrt: true);
            var befund = new Musterbefund("/muster", new[] { d }, OrdnerGesperrtException.Vorlagenordner("/muster"));
            Assert.True(d.Gesperrt);
            Assert.True(befund.Schreibfehler);
            Assert.False(befund.Erfolg);
            Assert.Equal(2, befund.Meldungen.Count);
            Assert.Contains("/muster", befund.Meldungen[0]);
        }
    }
}
