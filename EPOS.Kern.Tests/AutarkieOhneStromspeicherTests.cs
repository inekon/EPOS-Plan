using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Autarkie-Analyse ohne Stromspeicher rechnet mit 0 kWh</b> (Anwenderentscheid
    /// „Ohne Speicher = 0 kWh!", Papier „Verbesserungen 29.09.2026"). Bis dahin nahm sie einen
    /// 5-kWh-Speicher an, den das Projekt nicht hat — die Kachel zeigte dann eine Autarkie,
    /// die das Projekt nicht erreicht.
    ///
    /// <para><b>Der Nachweis am Lauf:</b> Projekt 1045 führt Photovoltaik und keinen
    /// Stromspeicher. Die Seite belegt das Feld mit 0 kWh und sagt „ohne Stromspeicher";
    /// die Autarkie entspricht dem Direktverbrauch allein. Die alte Vorbelegung (5 kWh) bleibt
    /// als Was-wäre-wenn-Eingabe rechenbar und liegt darüber. Projekt 1046 führt Speicher —
    /// dort bleibt die Summe der Speicher die Vorbelegung.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class AutarkieOhneStromspeicherTests : IDisposable
    {
        private readonly ITestOutputHelper _ausgabe;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public AutarkieOhneStromspeicherTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        public void Dispose() => _kultur.Dispose();

        /// <summary>PV ohne Stromspeicher.</summary>
        private const int OHNE_SPEICHER = 1045;

        /// <summary>PV mit vier Speicheranlagen (zusammen 38,5 kWh).</summary>
        private const int MIT_SPEICHER = 1046;

        private const double ALTE_VORBELEGUNG_KWH = 5.0;

        [Fact]
        public async Task Ohne_Stromspeicher_rechnet_die_Analyse_mit_null_kWh()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SimulationErgebnisDienste dienste = Ergebnisdienste(OHNE_SPEICHER);
            Rueckmeldung lauf = await dienste.Laufen((anteil, text) => { });
            Assert.True(lauf.Erfolg, lauf.Text);
            SimulationErgebnisDaten d = dienste.Laden(OHNE_SPEICHER);
            Assert.Equal(ErgebnisZustand.Gueltig, d.Zustand);

            AutarkieDaten neu = d.Autarkie;
            Assert.True(neu.HatPv);
            Assert.Equal(0.0, neu.SpeicherKwh);
            Assert.True(neu.OhneStromspeicher);
            Assert.Equal(0.0, neu.SpeichernutzenKwh);
            Assert.True(double.IsFinite(neu.AutarkiePvProzent));
            Assert.True(neu.AutarkiePvProzent > 0.0);

            // Die alte Vorbelegung als Was-wäre-wenn-Eingabe: mehr Autarkie als ohne Speicher.
            AutarkieDaten alt = dienste.AutarkieRechnen!(ALTE_VORBELEGUNG_KWH);
            Assert.False(alt.OhneStromspeicher);
            Assert.True(alt.AutarkiePvProzent > neu.AutarkiePvProzent);
            Assert.True(alt.SpeichernutzenKwh > 0.0);

            _ausgabe.WriteLine($"Projekt {OHNE_SPEICHER}: Autarkie PV alt (5 kWh) {alt.AutarkiePvProzent:F4} % " +
                               $"-> neu (0 kWh) {neu.AutarkiePvProzent:F4} %; Speichernutzen alt {alt.SpeichernutzenKwh:F2} kWh " +
                               $"-> neu {neu.SpeichernutzenKwh:F2} kWh");
        }

        [Fact]
        public async Task Mit_Stromspeicher_bleibt_die_Summe_der_Speicher_die_Vorbelegung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SimulationErgebnisDienste dienste = Ergebnisdienste(MIT_SPEICHER);
            Rueckmeldung lauf = await dienste.Laufen((anteil, text) => { });
            Assert.True(lauf.Erfolg, lauf.Text);
            SimulationErgebnisDaten d = dienste.Laden(MIT_SPEICHER);
            Assert.Equal(ErgebnisZustand.Gueltig, d.Zustand);

            Assert.Equal(StromspeicherStammCtrl.KapazitaetJeProjekt(MIT_SPEICHER), d.Autarkie.SpeicherKwh, 9);
            Assert.True(d.Autarkie.SpeicherKwh > 0.0);
            Assert.False(d.Autarkie.OhneStromspeicher);
        }

        private static SimulationErgebnisDienste Ergebnisdienste(int idProjekt)
        {
            var quelle = new SimulationAnsichtQuelle(new BedarfsZustand(), null);
            IReadOnlyDictionary<string, object> gaben = quelle.AnsichtGaben(idProjekt, "Prüfprojekt");
            var dienste = (SimulationAnsichtDienste)gaben["Dienste"];
            return (SimulationErgebnisDienste)dienste.Ergebnis["Dienste"];
        }
    }
}
