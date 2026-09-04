using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <see cref="ErgebnisPraesenz"/> — mit iU9-W11a aus
    /// <c>Views/Simulation/ErgebnisPraesenz.cs</c> in den Kern gezogen und dabei von
    /// <c>internal</c> auf <c>public</c> gehoben.
    ///
    /// <para><b>Warum das eine Kernprobe braucht.</b> Die Regel steuert, welche Zeilen,
    /// Schalter, Chartserien und Tortensegmente in FUENF der sechs Ergebnismasken
    /// erscheinen. Sie hat vier ODER-verknuepfte Quellen, davon eine mit
    /// Datenbankzugriff (Punkt 4, der Anlagenbestand) — und genau die war bisher nur am
    /// Geraet nachweisbar.</para>
    ///
    /// <para>Geprueft wird gegen einen echten Lauf von Projekt 1030 (dasselbe, das die
    /// CI gegen die Basis rechnet). Ohne Testdatenbank schweigen die Faelle.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ErgebnisPraesenzTests : IClassFixture<TestDatenbank>
    {
        /// <summary>
        /// EINE Arbeitskopie fuer die ganze Klasse (iU9-W11a.6). Die Faelle hier lesen
        /// nur; eine Kopie je Testfall waere 77 MB Datei-Ein-/Ausgabe fuer nichts.
        /// </summary>
        private readonly TestDatenbank _db;

        public ErgebnisPraesenzTests(TestDatenbank db) { _db = db; }

        private const int PROJEKT = 1030;

        /// <summary>
        /// Die Rueckfallebene macht alles sichtbar — auch fuer <c>null</c>. Vor dem
        /// ersten Lauf darf nichts verschwinden, was danach erscheinen soll.
        /// </summary>
        [Fact]
        public void Alles_zeigt_jede_Komponente()
        {
            ErgebnisPraesenz p = ErgebnisPraesenz.Alles();

            Assert.True(p.Waermepumpe);
            Assert.True(p.Heizstab);
            Assert.True(p.Heizkessel);
            Assert.True(p.Solarthermie);
            Assert.True(p.BHKW);
            Assert.True(p.Photovoltaik);
            Assert.True(p.Speicher);
            Assert.True(p.Stromspeicher);
        }

        [Fact]
        public void Ermitteln_ohne_Lauf_faellt_auf_Alles_zurueck()
        {
            ErgebnisPraesenz p = ErgebnisPraesenz.Ermitteln(null);

            Assert.True(p.Waermepumpe);
            Assert.True(p.Photovoltaik);
            Assert.True(p.Stromspeicher);
        }

        /// <summary>
        /// Ein leerer <see cref="SimulationControl"/> ohne Projekt: kein Lauf, kein
        /// Anlagenbestand — nichts ist vorhanden. Das ist die Gegenprobe zu
        /// <see cref="Alles_zeigt_jede_Komponente"/> und zeigt, dass die Regel ueberhaupt
        /// etwas ausblendet.
        /// </summary>
        [Fact]
        public void Ermitteln_ohne_Projekt_und_ohne_Lauf_zeigt_nichts()
        {
            ErgebnisPraesenz p = ErgebnisPraesenz.Ermitteln(new SimulationControl());

            Assert.False(p.Waermepumpe);
            Assert.False(p.Heizstab);
            Assert.False(p.Heizkessel);
            Assert.False(p.Solarthermie);
            Assert.False(p.BHKW);
            Assert.False(p.Photovoltaik);
            Assert.False(p.Stromspeicher);
        }

        /// <summary>
        /// Nach einem vollstaendigen Lauf von Projekt 1030 muessen die Komponenten
        /// vorhanden sein, die der Lauf gerechnet hat. Geprueft wird die INVARIANTE, nicht
        /// die Bestueckung des Projekts: Wer im Lauf ein Ergebnis hat, ist praesent.
        /// </summary>
        [Fact]
        public void Ermitteln_nach_Lauf_1030_meldet_jede_gerechnete_Stufe()
        {
            if (!_db.Vorhanden) return;

            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(PROJEKT, out fehler), "Lauf gescheitert: " + fehler);

            ErgebnisPraesenz p = ErgebnisPraesenz.Ermitteln(laeufer.sim);

            if (laeufer.sim.bSimulationWP) Assert.True(p.Waermepumpe);
            if (laeufer.sim.bSimulationKessel) Assert.True(p.Heizkessel);
            if (laeufer.sim.bSimulationSolarthermie) Assert.True(p.Solarthermie);
            if (laeufer.sim.bSimulationBHKW) Assert.True(p.BHKW);
            if (laeufer.sim.bSimulationPV) Assert.True(p.Photovoltaik);

            // Der Heizstab ist Teil der Waermepumpe und hat keine eigene Anlagenzeile.
            Assert.Equal(p.Waermepumpe, p.Heizstab);

            // Die Fuellstandsserien entstehen aus der Speicherliste des Laufs.
            Assert.Equal(laeufer.sim.AlleSpeicher() != null && laeufer.sim.AlleSpeicher().Count > 0,
                         p.Speicher);

            // Der Stromspeicher zaehlt allein ueber das Engine-Ergebnis (AP3b).
            Assert.Equal(laeufer.sim.bSimulationSSP && laeufer.sim.Speicherergebnis != null,
                         p.Stromspeicher);
        }

        /// <summary>
        /// Punkt 4 der Regel: Der Anlagenbestand allein macht eine Komponente praesent —
        /// auch ohne Lauf. Geprueft mit einem <see cref="SimulationControl"/>, der nur
        /// seine Projekt-ID kennt: 1030 fuehrt Anlagen, also muss mindestens eine
        /// Komponente anspringen, obwohl kein Lauf stattgefunden hat.
        /// </summary>
        [Fact]
        public void Ermitteln_zieht_den_Anlagenbestand_auch_ohne_Lauf_heran()
        {
            if (!_db.Vorhanden) return;

            var sim = new SimulationControl();
            sim.m_ID_Projekt = PROJEKT;

            ErgebnisPraesenz p = ErgebnisPraesenz.Ermitteln(sim);

            Assert.True(p.Waermepumpe || p.Heizkessel || p.Solarthermie ||
                        p.BHKW || p.Photovoltaik,
                        "Projekt " + PROJEKT + " fuehrt Anlagen — Punkt 4 der Regel hat nicht gegriffen.");
        }
    }
}
