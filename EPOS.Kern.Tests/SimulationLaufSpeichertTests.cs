using System;
using System.Threading.Tasks;
using EPOS.UI.Seiten.Simulation;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis zum Anwenderbefund vom 22.09.2026</b>: „Die Kachel Simulation
    /// rechnet, aber sie speichert nicht."
    ///
    /// <para><b>Die Lage im Bestand.</b> Der Weg der Kachel
    /// (<c>SimulationErgebnisHuelle.Laufen</c>) endete mit
    /// <c>ZustandSetzen(Gueltig)</c> und einer stillen Rückmeldung; geschrieben wurde
    /// erst auf den eigenen Knopf „Ergebnis speichern". Der zweite Weg zum selben
    /// Rechenlauf — die Übersichtsseite über <c>SimulationRunner.SimuliereUndSpeichere</c>
    /// — rechnete und speicherte dagegen in EINEM Zug. Zwei Wege, dieselbe Rechnung,
    /// verschiedene Folgen: Wer über die Kachel lief und den Knopf nicht fand, sah in
    /// Übersicht, Bericht und Wirtschaftlichkeit weiter den ALTEN Lauf. Beim Anwender
    /// führte das dazu, dass ein längst entfernter Strombedarf im gespeicherten
    /// Ergebnis stehen blieb.</para>
    ///
    /// <para>Gemessen wird am Weg selbst: Hülle bauen, den Dienst <c>Laufen</c> fahren,
    /// danach die DATENBANK lesen. Der Fall legt seine eigene Arbeitskopie an — er
    /// rechnet und schreibt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SimulationLaufSpeichertTests : IDisposable
    {
        /// <summary>Der Erfolgssatz des Laufs kommt aus den Satellitenressourcen und
        /// folgt <c>CurrentUICulture</c>; ohne Pinnung liest ein Lauf unter en-US den
        /// englischen Text.</summary>
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>„Referenz BHKW-Kaskade" — das Projekt, an dem auch
        /// <see cref="SimulationLaufCtrlTests"/> seinen Lauf misst. Sein Strombedarf
        /// steht als GANGLINIE (<c>Z_ProjektStromganglinie</c>).</summary>
        private const int PROJEKT = 1030;

        /// <summary>
        /// DER BEFUND SELBST. Der Kachelweg schreibt sein Ergebnis — und was danach in
        /// der Datenbank steht, ist der FRISCHE Lauf: ohne den entfernten Strombedarf.
        /// </summary>
        [Fact]
        public async Task Der_Kachelweg_speichert_sein_Ergebnis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(StrombedarfGesamt() > 0, "Das gespeicherte Ergebnis führt keinen Strombedarf.");

            // Die Lage des Befunds: Der Anwender nimmt den Strombedarf heraus und
            // startet die Simulation neu.
            DataRepository.ExecuteSQL(
                "DELETE FROM Z_ProjektStromganglinie WHERE ID_Projekt = ?",
                new DbParam("@p", PROJEKT));

            Rueckmeldung antwort = await Kachelweg();

            Assert.True(antwort.Erfolg, antwort.Text);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SIM_MSG_ERGEBNIS_GESPEICHERT, antwort.Text);

            // GESCHRIEBEN, nicht nur gerechnet: Der Ergebniskopf behält seine Id
            // (ErgebnisCtrl.Save schreibt ihn neu, nicht dazu) — sein INHALT ist der
            // frische Lauf, und der führt keinen Strombedarf mehr.
            Assert.Equal(0.0, StrombedarfGesamt(), 6);
        }

        // =================================================================
        // Handgriffe
        // =================================================================

        /// <summary>Der Weg der Kachel: die Hülle bauen und ihren Dienst
        /// <c>Laufen</c> fahren — denselben, den die Razor-Seite ruft.</summary>
        private static Task<Rueckmeldung> Kachelweg()
        {
            SimulationErgebnisHuelle huelle =
                SimulationErgebnisHuelle.Erzeugen(null, PROJEKT, new BedarfsZustand());

            var dienste = (SimulationErgebnisDienste)huelle.Gaben()["Dienste"];
            Assert.NotNull(dienste.Laufen);

            return dienste.Laufen((anteil, text) => { });
        }

        /// <summary>Der GESPEICHERTE Strombedarf des Projekts [MWh/a].</summary>
        private static double StrombedarfGesamt()
        {
            ErgebnisModel erg = new ErgebnisCtrl().Load(PROJEKT);
            return (erg == null || erg.Energiebedarf == null)
                   ? 0.0 : erg.Energiebedarf.Strombedarf_Gesamt;
        }
    }
}
