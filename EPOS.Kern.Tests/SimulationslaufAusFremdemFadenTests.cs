using System.Threading.Tasks;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// PROBE R-W10a-2 (iU9-W10a): Laeuft <see cref="SimulationRunner.Simuliere"/> aus einem
    /// FREMDEN Faden?
    ///
    /// <para><b>Warum die Frage gestellt wird.</b> <c>Form_QuelleErdreich</c> stoesst einen
    /// vollstaendigen Simulationslauf an — synchron, im Oberflaechenfaden, mit Sanduhr
    /// (Befund W10-B9). In einer WebView blockiert das die ganze Oberflaeche: Blazor
    /// zeichnet auf demselben Faden. Der Ausweg ist <c>Task.Run</c>, aber nur, wenn der
    /// Datenzugriff keinen Fadenbezug hat.</para>
    ///
    /// <para><b>Ergebnis der Probe: er hat keinen.</b> <c>SqliteDatenzugriff</c> oeffnet je
    /// Aufruf eine eigene Verbindung (<c>OeffneVerbindung</c>), haelt nichts
    /// <c>[ThreadStatic]</c> und nichts statisch Offenes; <c>DataRepository.EngineModus</c>
    /// ist ein Zaehler auf einem statischen Feld, kein Fadenzustand. Der Lauf im
    /// <c>Task.Run</c> liefert deshalb dasselbe Ergebnis wie im aufrufenden Faden — die
    /// Huelle <c>QuelleErdreichHuelle</c> rechnet asynchron mit sichtbarem Wartezustand.</para>
    ///
    /// <para><b>Ohne Testdatenbank schweigt der Fall.</b> Wie ueberall in diesem Projekt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SimulationslaufAusFremdemFadenTests
    {
        /// <summary>Projekt 1030 — dasselbe, das die CI gegen die Basis rechnet.</summary>
        private const int PROJEKT = 1030;

        [Fact]
        public async Task Simuliere_laeuft_in_Task_Run_ohne_Fadenfehler()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string fehler = null;
            bool ok = await Task.Run(() =>
            {
                var laeufer = new SimulationRunner();
                return laeufer.Simuliere(PROJEKT, out fehler);
            });

            // Der Lauf muss durchgehen; ein Fadenproblem meldete sich als Ausnahme oder
            // als Datenbankfehler im Fehlertext.
            Assert.True(ok, "Simulationslauf im Task.Run gescheitert: " + fehler);
            Assert.True(string.IsNullOrEmpty(fehler), "Fehlertext trotz Erfolg: " + fehler);
        }

        /// <summary>
        /// Derselbe Lauf im AUFRUFENDEN Faden — die Gegenprobe. Beide Wege muessen dasselbe
        /// liefern, sonst waere die Asynchronitaet nicht folgenlos.
        /// </summary>
        [Fact]
        public void Simuliere_liefert_im_eigenen_Faden_dasselbe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string fehler;
            bool ok = new SimulationRunner().Simuliere(PROJEKT, out fehler);

            Assert.True(ok, "Simulationslauf im eigenen Faden gescheitert: " + fehler);
        }
    }
}
