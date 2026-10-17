using System;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Seiten.Simulation;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Befund EB-2 (17.10.2026, Anwenderentscheid „Standby berücksichtigen"): Nach einem Lauf aus der
    /// Oberfläche war der Standby des Speichersystems immer 0. Der Lauf der Oberfläche rechnet den
    /// Speicher über die vorbelegte Flotte (<see cref="SimulationControl.SpeicherflotteAusLaufVorbelegen"/>,
    /// eine Einheit je Speicheranlage, #210); die Einheit entstand in
    /// <c>SpeicherFlottenStudieCtrl.Einheit</c> aus dem Parametersatz der Anlage — ohne dessen
    /// <c>StandbyKw</c> und <c>SelbstentladungProzentMonat</c>. Der Flottensimulator rechnet den
    /// Standby aus <see cref="FlottenEinheit.HilfsverbrauchKw"/>; der blieb 0.
    /// </summary>
    /// <remarks>
    /// <para>Projekt 1007 der Testdatenbank führt vier Speicheranlagen, drei davon mit demselben Gerät
    /// (BYD, Variante 1 aktiv), eine mit einem zweiten (Vaillant); kein Gerät trägt einen Standby.
    /// Die Fälle setzen ihn in der Arbeitskopie. Der Einzellauf (<see cref="SimulationRunner"/> ohne
    /// Vorbelegung, keine aktivierte Projektflotte) rechnet die Anlage der aktiven Variante; die
    /// vorbelegte Flotte rechnet alle vier Anlagen — drei Einheiten mit dem Standby des geteilten
    /// Geräts. Der Lauf der Oberfläche aktiviert seine Flotte als Projektflotte; der Runner rechnet
    /// danach dieselbe Flotte.</para>
    /// </remarks>
    [Collection("Testdatenbank")]
    public class SpeicherStandbyVorbelegungTests
    {
        private const int PROJEKT = 1007;
        private const int GERAET_BYD = 1007013;        // drei Anlagen: 10354 (aktive Variante), 10355, 10356
        private const int GERAET_VAILLANT = 1007001;   // eine Anlage: 10357
        private const int ANLAGE_AKTIV = 10354;
        private const int ANLAGE_VAILLANT = 10357;

        private const double STANDBY_W = 20.0;
        private const double SELBSTENTLADUNG = 2.0;
        private const double STUNDEN = 8760.0;

        private static void StandbySetzen(int geraet, double? standbyW, double? selbstentladung)
        {
            DataRepository.ExecuteSQL(
                "UPDATE Tab_Stromspeicher SET Standby_Verbrauch = ?, Selbstentladung_Prozent_Monat = ? WHERE ID = ?",
                new DbParam("@s", (object)standbyW ?? DBNull.Value),
                new DbParam("@e", (object)selbstentladung ?? DBNull.Value),
                new DbParam("@id", geraet));
        }

        private static SimulationRunner Lauf(bool vorbelegen)
        {
            var r = new SimulationRunner();
            r.sim.SpeicherflotteAusLaufVorbelegen = vorbelegen;
            Assert.True(r.Simuliere(PROJEKT, out string fehler), "Lauf gescheitert: " + fehler);
            return r;
        }

        /// <summary>Der Weg der Kachel: Hülle bauen und ihren Dienst <c>Laufen</c> fahren.</summary>
        private static Task<Rueckmeldung> Kachelweg()
        {
            SimulationErgebnisHuelle huelle =
                SimulationErgebnisHuelle.Erzeugen(null, PROJEKT, new BedarfsZustand());
            var dienste = (SimulationErgebnisDienste)huelle.Gaben()["Dienste"];
            Assert.NotNull(dienste.Laufen);
            return dienste.Laufen((anteil, text) => { });
        }

        /// <summary>Die Einheit der Vorbelegung trägt Standby und Selbstentladung ihrer Anlage — geprüft wie im Einzellauf.</summary>
        [Fact]
        public void Die_Einheit_der_Vorbelegung_traegt_Standby_und_Selbstentladung_der_Anlage()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            StandbySetzen(GERAET_BYD, STANDBY_W, SELBSTENTLADUNG);
            FlottenEinheit byd = SpeicherFlottenStudieCtrl.EinheitAusProjektanlage(PROJEKT, ANLAGE_AKTIV);
            Assert.NotNull(byd);
            Assert.Equal(STANDBY_W / 1000.0, byd.HilfsverbrauchKw, 12);
            Assert.Equal(SELBSTENTLADUNG, byd.SelbstentladungProzentProMonat, 12);

            // Ein Gerät ohne Pflege: beides 0 — wie bisher.
            FlottenEinheit vaillant = SpeicherFlottenStudieCtrl.EinheitAusProjektanlage(PROJEKT, ANLAGE_VAILLANT);
            Assert.NotNull(vaillant);
            Assert.Equal(0.0, vaillant.HilfsverbrauchKw);
            Assert.Equal(0.0, vaillant.SelbstentladungProzentProMonat);
        }

        /// <summary>
        /// Einzellauf und vorbelegte Flotte rechnen den Standby je Anlage gleich: die aktive Anlage 20 W
        /// (175,2 kWh/a), die Flotte aller vier Anlagen dreimal das Gerät (525,6 kWh/a) — der Standby
        /// jeder Einheit ist der ihrer Anlage.
        /// </summary>
        [Fact]
        public void Einzellauf_und_vorbelegte_Flotte_rechnen_den_Standby_je_Anlage()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            StandbySetzen(GERAET_BYD, STANDBY_W, null);

            SimulationRunner einzeln = Lauf(false);
            Assert.Null(einzeln.sim.SpeicherflottenEingaben);
            Assert.Equal(STANDBY_W / 1000.0 * STUNDEN, einzeln.sim.SpeichersystemEigenverbrauchKwh, 6);

            SimulationRunner flotte = Lauf(true);
            Assert.NotNull(flotte.sim.SpeicherflottenEingaben);
            var einheiten = flotte.sim.Speicherflottenkonfiguration.Einheiten;
            Assert.Equal(4, einheiten.Count);
            Assert.Equal(3, einheiten.Count(e => e.HilfsverbrauchKw == STANDBY_W / 1000.0));
            Assert.Equal(1, einheiten.Count(e => e.HilfsverbrauchKw == 0.0));
            Assert.Equal(3.0 * STANDBY_W / 1000.0 * STUNDEN, flotte.sim.SpeichersystemEigenverbrauchKwh, 6);
            Assert.Equal(3.0 * einzeln.sim.SpeichersystemEigenverbrauchKwh, flotte.sim.SpeichersystemEigenverbrauchKwh, 6);

            // Ohne Standby bleibt die Flotte, wie sie war: Eigenverbrauch 0.
            StandbySetzen(GERAET_BYD, null, null);
            Assert.Equal(0.0, Lauf(true).sim.SpeichersystemEigenverbrauchKwh);
        }

        /// <summary>
        /// Der Lauf der Oberfläche schreibt den Standby ins Ergebnis (<c>Tab_ErgebnisStromspeicher.Eigenverbrauch_System</c>)
        /// und aktiviert seine Flotte als Projektflotte; der Runner rechnet danach denselben Standby.
        /// </summary>
        [Fact]
        public async Task Der_Kachelweg_schreibt_den_Standby_und_der_Runner_rechnet_danach_denselben()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            StandbySetzen(GERAET_BYD, STANDBY_W, null);
            Assert.False(SpeicherFlottenProjektCtrl.IstAktiv(PROJEKT));

            Rueckmeldung antwort = await Kachelweg();
            Assert.True(antwort.Erfolg, antwort.Text);

            double erwartet = 3.0 * STANDBY_W / 1000.0 * STUNDEN;
            VariantenDaten v = EnergievergleichCtrl.Lade(PROJEKT, true);
            ErgebnisStromspeicherModel sp = Assert.Single(v.Ergebnis.Stromspeicher);
            Assert.True(sp.EigenverbrauchSystemKwh.HasValue, "Standby nicht geschrieben");
            Assert.Equal(Math.Round(erwartet, 2, MidpointRounding.AwayFromZero), sp.EigenverbrauchSystemKwh.Value, 6);

            Assert.True(SpeicherFlottenProjektCtrl.IstAktiv(PROJEKT));
            SimulationRunner danach = Lauf(false);
            Assert.Equal(erwartet, danach.sim.SpeichersystemEigenverbrauchKwh, 6);
            Assert.Equal(sp.EigenverbrauchSystemKwh.Value, Math.Round(danach.sim.SpeichersystemEigenverbrauchKwh, 2, MidpointRounding.AwayFromZero), 6);
        }
    }
}
