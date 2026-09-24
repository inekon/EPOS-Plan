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
            double restA = a.sim.RestwaermeMwh;

            var b = new SimulationRunner();
            Assert.True(b.Simuliere(PROJEKT, out fehler), fehler);
            b.sim.Do_Simulation(PROJEKT, null, CancellationToken.None);

            Assert.Equal(restA, b.sim.RestwaermeMwh, 6);
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
            double restEigen = a.sim.RestwaermeMwh;

            var b = new SimulationRunner();
            Assert.True(b.Simuliere(PROJEKT, out fehler), fehler);
            await Task.Run(() => SimulationLaufCtrl.Laufen(b.sim, PROJEKT));

            Assert.Equal(restEigen, b.sim.RestwaermeMwh, 6);
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
            Assert.True(strom.StrombedarfGesamtMwh > 0);
        }

        // ------------------------------------------- Erzeuger ohne Kaskadenplatz (#190)

        /// <summary>Das Projekt des Abnahmebefunds: Kessel angelegt, kein Kaskadenplatz.</summary>
        private const int PROJEKT_OHNE_PLATZ = 1007;

        /// <summary>Die Gegenprobe: derselbe Aufbau, aber <c>Tool_2 = Heizkessel</c>.</summary>
        private const int PROJEKT_MIT_PLATZ = 1040;

        /// <summary>
        /// Die Meldung selbst — gemessen an einer PLATZBELEGUNG, nicht an einem Projekt.
        ///
        /// <para>Sie steht so hier, weil der Heizkessel seinen Platz inzwischen
        /// automatisch bekommt (HK-E-1, <c>KonfigurationCtrl.HeizkesselNachziehen</c>,
        /// gemessen in <c>HeizkesselKaskadeTests</c>) — eine gelesene Konfiguration
        /// traegt ihn danach. Der Kessel bleibt trotzdem der beste Prueffall fuer den
        /// TEXT der Meldung: Er nennt die Erzeugerart, die Anlage beim Namen und den Weg
        /// zurueck.</para>
        /// </summary>
        [Fact]
        public void Ein_Kessel_ohne_Kaskadenplatz_wird_mit_Kennung_gemeldet()
        {
            if (!_db.Vorhanden) return;
            using var _ = new DeutscheOberflaeche();

            string[] ohne = { "", DbWerte.ERZEUGER_SOLARTHERMIE, DbWerte.ERZEUGER_WAERMEPUMPE, "",
                              DbWerte.ERZEUGER_PHOTOVOLTAIK, DbWerte.ERZEUGER_STROMSPEICHER };
            var befunde = SimulationLaufCtrl.ErzeugerOhneKaskadenplatz(PROJEKT_OHNE_PLATZ, ohne);

            Warnbefund b = Assert.Single(befunde);
            Assert.Equal(SimulationLaufCtrl.KRIT_ERZEUGER_OHNE_KASKADENPLATZ, b.Kriterium);
            Assert.Equal(DbWerte.ERZEUGER_HEIZKESSEL, b.Steuerwert);
            Assert.False(b.Hart);
            Assert.True(b.ID_Anlage > 0);

            // Der Text nennt die Anlage beim Namen und den Weg zurueck.
            string bezeichner = WErzeugerCtrl.AnlagenBezeichner(b.ID_Anlage);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.KONFIG_HEIZKESSEL, b.Text);
            Assert.Contains(bezeichner, b.Text);
            Assert.Contains("aufnehmen", b.Text);
        }

        /// <summary>
        /// Die Gegenprobe: Projekt 1040 fuehrt denselben Bestand, hat den Kessel aber auf
        /// <c>Tool_2</c> — kein Hinweis. Ohne sie wuerde eine Pruefung, die IMMER meldet,
        /// den Fall darueber ebenso bestehen.
        /// </summary>
        [Fact]
        public void Ein_Projekt_mit_belegten_Plaetzen_meldet_nichts()
        {
            if (!_db.Vorhanden) return;

            KonfigurationModel konfig = KonfigurationCtrl.LiesProjekt(PROJEKT_MIT_PLATZ);
            Assert.NotNull(konfig);

            Assert.Empty(SimulationLaufCtrl.ErzeugerOhneKaskadenplatz(PROJEKT_MIT_PLATZ, konfig));
        }

        /// <summary>
        /// Dieselbe Pruefung gegen die PLATZBELEGUNG EINES LAUFS: Derselbe Bestand
        /// meldet den Kessel, sobald sein Platz fehlt — und schweigt, sobald er da ist.
        /// Das ist der Weg, den <c>SimulationControl</c> nimmt (<c>tool[]</c>).
        /// </summary>
        [Fact]
        public void Die_Pruefung_gegen_die_Platzbelegung_eines_Laufs_folgt_dem_Feld_tool()
        {
            if (!_db.Vorhanden) return;

            string[] ohne = { "", DbWerte.ERZEUGER_SOLARTHERMIE, DbWerte.ERZEUGER_WAERMEPUMPE, "",
                              DbWerte.ERZEUGER_PHOTOVOLTAIK, DbWerte.ERZEUGER_STROMSPEICHER };
            var gemeldet = SimulationLaufCtrl.ErzeugerOhneKaskadenplatz(PROJEKT_OHNE_PLATZ, ohne);
            Assert.Equal(DbWerte.ERZEUGER_HEIZKESSEL, Assert.Single(gemeldet).Steuerwert);

            string[] mit = { DbWerte.ERZEUGER_HEIZKESSEL, DbWerte.ERZEUGER_SOLARTHERMIE,
                             DbWerte.ERZEUGER_WAERMEPUMPE, "",
                             DbWerte.ERZEUGER_PHOTOVOLTAIK, DbWerte.ERZEUGER_STROMSPEICHER };
            Assert.Empty(SimulationLaufCtrl.ErzeugerOhneKaskadenplatz(PROJEKT_OHNE_PLATZ, mit));
        }

        /// <summary>
        /// Die Stromseite hat ihre eigenen Plaetze (<c>Tool_5</c>/<c>Tool_6</c>) und
        /// dieselbe Luecke — sie bekommt deshalb eine eigene Kennung und einen eigenen
        /// Text („nicht aufgenommen" statt „nicht in der Kaskade").
        /// </summary>
        [Fact]
        public void Die_Stromplaetze_melden_mit_eigener_Kennung()
        {
            if (!_db.Vorhanden) return;

            // Alles belegt AUSSER Tool_5/Tool_6: 1007 fuehrt zwei PV-Anlagen und vier
            // Stromspeicher.
            string[] plaetze = { DbWerte.ERZEUGER_HEIZKESSEL, DbWerte.ERZEUGER_SOLARTHERMIE,
                                 DbWerte.ERZEUGER_WAERMEPUMPE, "", "", "" };

            var befunde = SimulationLaufCtrl.ErzeugerOhneKaskadenplatz(PROJEKT_OHNE_PLATZ, plaetze);

            Assert.NotEmpty(befunde);
            foreach (Warnbefund b in befunde)
            {
                Assert.Equal(SimulationLaufCtrl.KRIT_ERZEUGER_OHNE_STROMPLATZ, b.Kriterium);
                Assert.True(b.Steuerwert == DbWerte.ERZEUGER_PHOTOVOLTAIK ||
                            b.Steuerwert == DbWerte.ERZEUGER_STROMSPEICHER);
            }
        }

        /// <summary>
        /// Der LAUF sagt es auch — sonst saehe der unbeaufsichtigte Referenz- und
        /// CI-Lauf die Luecke nie (die Vorpruefung erreicht nur die Maske). Gemessen an
        /// einer Waermepumpe ohne Platz: Der Heizkessel bekommt seinen Platz seit HK-E-1
        /// automatisch, die uebrigen Erzeugerarten werden weiterhin nur gemeldet.
        /// </summary>
        [Fact]
        public void Der_Lauf_meldet_einen_nicht_platzierten_Erzeuger_im_Protokoll()
        {
            if (!_db.Vorhanden) return;
            using var _ = new DeutscheOberflaeche();

            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(PROJEKT_OHNE_PLATZ, out fehler), fehler);

            // Der Kessel rechnet jetzt mit - das ist die Wirkung des Entscheids.
            Assert.True(laeufer.sim.bSimulationKessel);

            // Die Meldung selbst lebt weiter: eine Platzbelegung ohne Waermepumpe
            // meldet die Waermepumpe des Projekts.
            string[] ohneWp = { DbWerte.ERZEUGER_HEIZKESSEL, DbWerte.ERZEUGER_SOLARTHERMIE, "", "",
                                DbWerte.ERZEUGER_PHOTOVOLTAIK, DbWerte.ERZEUGER_STROMSPEICHER };
            var befunde = SimulationLaufCtrl.ErzeugerOhneKaskadenplatz(PROJEKT_OHNE_PLATZ, ohneWp);

            Warnbefund b = Assert.Single(befunde);
            Assert.Equal(DbWerte.ERZEUGER_WAERMEPUMPE, b.Steuerwert);
            Assert.Contains(WErzeugerCtrl.AnlagenBezeichner(b.ID_Anlage), b.Text);
        }

        // =================================================================
        // DIE MELDUNG WIRD HANDLUNGSFAEHIG (Anwenderentscheid vom 16.09.2026,
        // Punkt c): Die Vorpruefung sagt jetzt auch, OB ein Platz frei waere.
        // =================================================================

        /// <summary>
        /// Ein freier Waermeplatz reicht — gleich welcher. Die Kaskade verdichtet nicht,
        /// eine Luecke vorn ist so gut wie ein freier Platz hinten.
        /// </summary>
        [Fact]
        public void Ein_freier_Waermeplatz_macht_das_Aufnehmen_moeglich()
        {
            string[] mitLuecke = { "", DbWerte.ERZEUGER_SOLARTHERMIE,
                                   DbWerte.ERZEUGER_WAERMEPUMPE, DbWerte.ERZEUGER_BHKW,
                                   DbWerte.ERZEUGER_PHOTOVOLTAIK, DbWerte.ERZEUGER_STROMSPEICHER };

            Assert.True(SimulationLaufCtrl.AufnahmeMoeglich(mitLuecke, DbWerte.ERZEUGER_HEIZKESSEL));
        }

        /// <summary>
        /// Sind alle vier Waermeplaetze belegt, ist das Aufnehmen NICHT moeglich — die
        /// Meldung sagt das dann, statt einen Knopf zu zeigen, der nichts tut.
        /// </summary>
        [Fact]
        public void Eine_volle_Kaskade_laesst_keinen_Waermeerzeuger_mehr_zu()
        {
            string[] voll = { DbWerte.ERZEUGER_HEIZKESSEL, DbWerte.ERZEUGER_SOLARTHERMIE,
                              DbWerte.ERZEUGER_WAERMEPUMPE, DbWerte.ERZEUGER_BHKW, "", "" };

            Assert.False(SimulationLaufCtrl.AufnahmeMoeglich(voll, DbWerte.ERZEUGER_HEIZKESSEL));
        }

        /// <summary>
        /// Photovoltaik und Stromspeicher haengen an IHREM Platz (<c>Tool_5</c> bzw.
        /// <c>Tool_6</c>) — ein freier Waermeplatz hilft ihnen nicht, und ein belegter
        /// Stromplatz sperrt nur die eigene Erzeugerart.
        /// </summary>
        [Fact]
        public void Die_Stromseite_fragt_ihren_eigenen_Platz()
        {
            string[] pvBelegt = { "", "", "", "", DbWerte.ERZEUGER_PHOTOVOLTAIK, "" };

            Assert.False(SimulationLaufCtrl.AufnahmeMoeglich(pvBelegt, DbWerte.ERZEUGER_PHOTOVOLTAIK));
            Assert.True(SimulationLaufCtrl.AufnahmeMoeglich(pvBelegt, DbWerte.ERZEUGER_STROMSPEICHER));
            Assert.True(SimulationLaufCtrl.AufnahmeMoeglich(pvBelegt, DbWerte.ERZEUGER_WAERMEPUMPE));
        }

        /// <summary>
        /// Eine kuerzere Liste laesst die fehlenden Plaetze als FREI gelten, und ein
        /// leerer Steuerwert ist nie aufnehmbar.
        /// </summary>
        [Fact]
        public void Fehlende_Plaetze_gelten_als_frei_und_ein_leerer_Steuerwert_nie()
        {
            string[] nurWaerme = { DbWerte.ERZEUGER_HEIZKESSEL, DbWerte.ERZEUGER_SOLARTHERMIE,
                                   DbWerte.ERZEUGER_WAERMEPUMPE, DbWerte.ERZEUGER_BHKW };

            Assert.True(SimulationLaufCtrl.AufnahmeMoeglich(nurWaerme, DbWerte.ERZEUGER_PHOTOVOLTAIK));
            Assert.False(SimulationLaufCtrl.AufnahmeMoeglich(nurWaerme, ""));
            Assert.False(SimulationLaufCtrl.AufnahmeMoeglich(null, null));
        }

        /// <summary>
        /// <b>Das gemessene Beispiel: Projekt 1017.</b> Seine Waermepumpe steht auf Platz 3 — sie
        /// ist der Kaelteerzeuger des Referenzprojekts (Einfrierregel „gesaete Kaeltedaten",
        /// <c>Referenzlaeufe/Skripte/kaelteerzeuger_1017_referenzprojekt.py</c>) und wird nicht
        /// gemeldet. Die Probe nimmt sie im MODELL wieder heraus, ohne die Datenbank anzufassen:
        /// Dann steht die Waermepumpe ohne Platz, <c>Tool_3/4</c> sind frei — das Aufnehmen waere
        /// also moeglich, und die Meldung traegt den Knopf. Sie geschieht aber NICHT von selbst:
        /// Keine Automatik fuer andere Erzeugerarten als den Heizkessel.
        /// </summary>
        [Fact]
        public void Projekt_1017_meldet_die_Waermepumpe_und_haette_einen_freien_Platz()
        {
            if (!_db.Vorhanden) return;
            using var _ = new DeutscheOberflaeche();

            KonfigurationModel konfig = KonfigurationCtrl.LiesProjekt(1017);
            Assert.NotNull(konfig);
            Assert.Equal(DbWerte.ERZEUGER_WAERMEPUMPE, konfig.m_Tool_3);
            Assert.DoesNotContain(SimulationLaufCtrl.ErzeugerOhneKaskadenplatz(1017, konfig),
                                  b => b.Steuerwert == DbWerte.ERZEUGER_WAERMEPUMPE);
            konfig.m_Tool_3 = "";

            var gemeldet = new List<string>();
            foreach (Warnbefund b in SimulationLaufCtrl.ErzeugerOhneKaskadenplatz(1017, konfig))
                gemeldet.Add(b.Steuerwert);

            Assert.Contains(DbWerte.ERZEUGER_WAERMEPUMPE, gemeldet);

            // Der Platz waere da - genommen wird er nur von einem Menschen.
            List<string> plaetze = Kaskade.Lesen(konfig);
            plaetze.Add(Kaskade.StromWert(konfig, Kaskade.PLATZ_STROMERZEUGER));
            plaetze.Add(Kaskade.StromWert(konfig, Kaskade.PLATZ_ENERGIESPEICHER));

            Assert.True(SimulationLaufCtrl.AufnahmeMoeglich(plaetze, DbWerte.ERZEUGER_WAERMEPUMPE));
            Assert.DoesNotContain(DbWerte.ERZEUGER_WAERMEPUMPE, Kaskade.Lesen(konfig));
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
