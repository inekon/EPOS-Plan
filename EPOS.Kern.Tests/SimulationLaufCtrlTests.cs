using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <see cref="SimulationLaufCtrl"/> und der Fortschritt/Abbruch von
    /// <c>SimulationControl.Do_Simulation</c> (iU9-W11a.4, Befund W11-B48).
    ///
    /// <para>Die Faelle ohne Datenbank pruefen die Vorpruefungen und die
    /// Abbruchauswertung; die Faelle mit Datenbank pruefen, dass der Lauf im fremden
    /// Faden dasselbe liefert, dass er seine Phasen der Reihe nach meldet und dass eine
    /// gesetzte Abbruchmarke ihn stoppt.</para>
    ///
    /// <para>Die Texte kommen aus <c>MyResource.Resource</c>; wo einer geprueft wird,
    /// ist die Sprache gepinnt (Regel seit iU9-W8).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SimulationLaufCtrlTests : IClassFixture<TestDatenbank>
    {
        /// <summary>
        /// EINE Arbeitskopie fuer die ganze Klasse (iU9-W11a.6). Die Faelle hier lesen
        /// nur; eine Kopie je Testfall waere 77 MB Datei-Ein-/Ausgabe fuer nichts.
        /// </summary>
        private readonly TestDatenbank _db;

        public SimulationLaufCtrlTests(TestDatenbank db) { _db = db; }

        private const int PROJEKT = 1030;

        // ---------------------------------------------------------------- Vorpruefen

        [Fact]
        public void Vorpruefen_ohne_Konfiguration_meldet_die_fehlende_Konfiguration()
        {
            using var _ = new DeutscheOberflaeche();

            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SIM_MSG_KONFIGURATION_FEHLT,
                         SimulationLaufCtrl.Vorpruefen(PROJEKT, null, 1));
        }

        /// <summary>
        /// Die Netzverlustpruefung greift NUR bei der Einheit „%" und NUR ueber 100 —
        /// woertlich aus <c>Energiebedarf</c> :3953.
        /// </summary>
        [Fact]
        public void Vorpruefen_meldet_Netzverluste_ueber_hundert_nur_in_Prozent()
        {
            using var _ = new DeutscheOberflaeche();

            var prozent = new KonfigurationModel { m_szNetzverlusteEinheit = "%", m_Netzverluste = 101 };
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SIM_MSG_NETZVERLUSTE_ZU_GROSS,
                         SimulationLaufCtrl.Vorpruefen(PROJEKT, prozent, 1));

            // Genau 100 ist erlaubt.
            var grenze = new KonfigurationModel { m_szNetzverlusteEinheit = "%", m_Netzverluste = 100 };
            Assert.Null(SimulationLaufCtrl.Vorpruefen(PROJEKT, grenze, 1));

            // Dieselbe Zahl in einer ABSOLUTEN Einheit ist kein Fehler.
            var absolut = new KonfigurationModel { m_szNetzverlusteEinheit = "MWh", m_Netzverluste = 101 };
            Assert.Null(SimulationLaufCtrl.Vorpruefen(PROJEKT, absolut, 1));
        }

        [Fact]
        public void Vorpruefen_meldet_die_fehlende_Klimaregion()
        {
            using var _ = new DeutscheOberflaeche();

            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SIM_MSG_KLIMAREGION_WAEHLEN,
                         SimulationLaufCtrl.Vorpruefen(PROJEKT, new KonfigurationModel(), 0));
        }

        [Fact]
        public void Vorpruefen_meldet_nichts_wenn_alles_steht()
        {
            Assert.Null(SimulationLaufCtrl.Vorpruefen(PROJEKT, new KonfigurationModel(), 7));
        }

        /// <summary>
        /// Die Reihenfolge der Pruefungen ist die des Vorlaeufers: erst Konfiguration,
        /// dann Netzverluste, dann Klimaregion.
        /// </summary>
        [Fact]
        public void Vorpruefen_haelt_die_Reihenfolge_der_Pruefungen()
        {
            using var _ = new DeutscheOberflaeche();

            var kaputt = new KonfigurationModel { m_szNetzverlusteEinheit = "%", m_Netzverluste = 200 };
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SIM_MSG_NETZVERLUSTE_ZU_GROSS,
                         SimulationLaufCtrl.Vorpruefen(PROJEKT, kaputt, 0));
        }

        // ---------------------------------------------------------------- Abbruchgrund

        [Fact]
        public void Abbruchgrund_eines_gelungenen_Laufs_ist_null()
        {
            Assert.Null(SimulationLaufCtrl.Abbruchgrund(null));
            Assert.Null(SimulationLaufCtrl.Abbruchgrund(new SimulationControl()));
        }

        /// <summary>
        /// Zwei Quellen in fester Reihenfolge: der Sperrgrund (der Lauf ist gar nicht
        /// erst angelaufen) schlaegt den Fehlertext (ein Modul hat abgebrochen).
        /// </summary>
        [Fact]
        public void Abbruchgrund_nimmt_den_Sperrgrund_vor_dem_Fehlertext()
        {
            var sim = new SimulationControl();
            sim.Sperrgrund = "Schema nicht migriert";
            sim.Fehlertext = "Kennlinie fehlt";

            Assert.StartsWith("Schema nicht migriert", SimulationLaufCtrl.Abbruchgrund(sim));

            sim.Sperrgrund = "";
            Assert.StartsWith("Kennlinie fehlt", SimulationLaufCtrl.Abbruchgrund(sim));
        }

        // ---------------------------------------------------------------- Fortschritt

        /// <summary>
        /// Der Lauf meldet seine fuenf Phasen in der Reihenfolge des Rechenwegs, mit
        /// nicht fallendem Anteil.
        /// </summary>
        [Fact]
        public void Do_Simulation_meldet_die_Phasen_in_Reihenfolge()
        {
            if (!_db.Vorhanden) return;

            var gemeldet = new List<LaufFortschritt>();
            var melder = new SofortMelder(gemeldet.Add);

            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(PROJEKT, out fehler), "Vorlauf gescheitert: " + fehler);

            // Denselben Lauf noch einmal, diesmal mit Fortschritt.
            laeufer.sim.Do_Simulation(PROJEKT, melder);

            Assert.NotEmpty(gemeldet);
            Assert.Equal(Laufphase.Start, gemeldet[0].Phase);
            Assert.Equal(Laufphase.Abschluss, gemeldet[gemeldet.Count - 1].Phase);

            for (int i = 1; i < gemeldet.Count; i++)
            {
                Assert.True((int)gemeldet[i].Phase > (int)gemeldet[i - 1].Phase,
                            "Phase " + gemeldet[i].Phase + " nach " + gemeldet[i - 1].Phase);
                Assert.True(gemeldet[i].Anteil >= gemeldet[i - 1].Anteil);
            }
            Assert.All(gemeldet, f => Assert.InRange(f.Anteil, 0.0, 1.0));
        }

        /// <summary>
        /// Ohne Fortschrittsempfaenger und ohne Abbruchmarke verhaelt sich der Lauf wie
        /// bisher — das ist die Bedingung des Referenzlaufs.
        /// </summary>
        [Fact]
        public void Do_Simulation_ohne_Zusatzangaben_laeuft_wie_bisher()
        {
            if (!_db.Vorhanden) return;

            var a = new SimulationRunner();
            string fehler;
            Assert.True(a.Simuliere(PROJEKT, out fehler), fehler);
            float restA = a.sim.Restwaerme;

            var b = new SimulationRunner();
            Assert.True(b.Simuliere(PROJEKT, out fehler), fehler);
            b.sim.Do_Simulation(PROJEKT, null, CancellationToken.None);

            Assert.Equal(restA, b.sim.Restwaerme, 6);
        }

        // ---------------------------------------------------------------- Abbruch

        /// <summary>
        /// Eine bereits gesetzte Abbruchmarke stoppt den Lauf an der ERSTEN Phasengrenze
        /// — er rechnet dann gar nicht.
        /// </summary>
        [Fact]
        public void Do_Simulation_bricht_an_der_ersten_Phasengrenze_ab()
        {
            if (!_db.Vorhanden) return;

            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(PROJEKT, out fehler), fehler);

            using var quelle = new CancellationTokenSource();
            quelle.Cancel();

            Assert.Throws<OperationCanceledException>(
                () => laeufer.sim.Do_Simulation(PROJEKT, null, quelle.Token));
        }

        /// <summary>
        /// Derselbe Abbruch aus einem <c>Task.Run</c> — der Weg, den die Detailansicht
        /// seit iU9-W11a.4 geht.
        /// </summary>
        [Fact]
        public async Task Laufen_im_Task_laesst_sich_abbrechen()
        {
            if (!_db.Vorhanden) return;

            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(PROJEKT, out fehler), fehler);

            using var quelle = new CancellationTokenSource();
            quelle.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => Task.Run(() => SimulationLaufCtrl.Laufen(laeufer.sim, PROJEKT, null, quelle.Token),
                               quelle.Token));
        }

        /// <summary>
        /// Der Lauf im fremden Faden liefert dasselbe wie im eigenen — die Gegenprobe zu
        /// R-W10a-2 fuer den Weg ueber <see cref="SimulationLaufCtrl.Laufen"/>.
        /// </summary>
        [Fact]
        public async Task Laufen_im_Task_liefert_dasselbe_Ergebnis()
        {
            if (!_db.Vorhanden) return;

            var a = new SimulationRunner();
            string fehler;
            Assert.True(a.Simuliere(PROJEKT, out fehler), fehler);
            float restEigen = a.sim.Restwaerme;

            var b = new SimulationRunner();
            Assert.True(b.Simuliere(PROJEKT, out fehler), fehler);
            await Task.Run(() => SimulationLaufCtrl.Laufen(b.sim, PROJEKT));

            Assert.Equal(restEigen, b.sim.Restwaerme, 6);
        }

        // ---------------------------------------------------------------- Bedarf

        /// <summary>
        /// <see cref="SimulationLaufCtrl.Bedarf"/> fuellt die HEREINGEREICHTEN Objekte —
        /// sie gehoeren dem Aufrufer (Befund W11-B3) und werden dort weiterverwendet.
        /// </summary>
        [Fact]
        public void Bedarf_fuellt_die_hereingereichten_Objekte()
        {
            if (!_db.Vorhanden) return;

            var projekt = new ProjektCtrl();
            projekt.ReadSingle(PROJEKT);
            if (projekt.m_ID_Klimaregion == 0) return;

            var waerme = new SimulationWaermebedarf();
            var strom = new SimulationStrombedarf();

            string fehler = SimulationLaufCtrl.Bedarf(PROJEKT, projekt.m_ID_Klimaregion,
                                                      0, "", waerme, strom);

            Assert.Null(fehler);
            Assert.True(waerme.Waermebedarf_Gesamt > 0);
            Assert.True(strom.Strombedarf_gesamt > 0);
        }

        /// <summary>Ein <c>IProgress&lt;T&gt;</c> ohne Marshalling — fuer den Prueffall.</summary>
        private sealed class SofortMelder : IProgress<LaufFortschritt>
        {
            private readonly Action<LaufFortschritt> _ziel;
            public SofortMelder(Action<LaufFortschritt> ziel) { _ziel = ziel; }
            public void Report(LaufFortschritt wert) { _ziel(wert); }
        }

        private sealed class DeutscheOberflaeche : IDisposable
        {
            private readonly System.Globalization.CultureInfo _vorher =
                System.Threading.Thread.CurrentThread.CurrentUICulture;

            public DeutscheOberflaeche()
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture =
                    new System.Globalization.CultureInfo("de-DE");
            }

            public void Dispose()
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = _vorher;
            }
        }
    }
}
