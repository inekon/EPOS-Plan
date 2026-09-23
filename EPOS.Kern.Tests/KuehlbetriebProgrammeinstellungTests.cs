using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using EPOS.UI.Seiten.Simulation;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>K10 in der Fassung von E27</b> (Kühlkonzept 7.2, 8.3, 10.3; F-K20): die
    /// Programmeinstellung „Neue Projekte mit Kühlung anlegen" und die Projekteinstellung
    /// <c>Tab_Einstellungen.Kuehlbetrieb</c> (Schemaschritt 109, KU-S2).
    ///
    /// <para><b>Geprüft wird:</b> die Ablage (ein <c>Dienste.Einstellungen</c>-Schlüssel, kein
    /// <c>Properties.Settings</c>-Schlüssel, Vorgabe aus); der Anfangswert in beiden
    /// Anlagewegen mit Einstellung an und aus; Bestandsprojekte bleiben aus, auch mit
    /// eingeschalteter Programmeinstellung; ein Lauf und das Lesen der Konfiguration fragen
    /// die Programmeinstellung NIE (Probe mit mitzählender Ablage und Wächter über den
    /// Quelltext); das Speichern der Kaskade erhält den Wert; ein Projektduplikat übernimmt
    /// den Wert der Quelle.</para>
    ///
    /// <para><b>Die Programmeinstellung wird über <see cref="FluechtigeEinstellungen"/>
    /// gesetzt</b>, ohne Registry (10.3) — deshalb in der seriellen Sammlung, und jeder Fall
    /// stellt die Ablage zurück. Eigene Arbeitskopie je Fall — die Fälle schreiben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KuehlbetriebProgrammeinstellungTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly IEinstellungen _vorher = Dienste.Einstellungen;

        public KuehlbetriebProgrammeinstellungTests()
        {
            Dienste.Einstellungen = new FluechtigeEinstellungen();
        }

        public void Dispose()
        {
            Dienste.Einstellungen = _vorher;
            _db.Dispose();
        }

        /// <summary>Das Referenzprojekt mit BHKW-Kaskade und Einstellungssatz.</summary>
        private const int REFERENZ = 1030;

        /// <summary>Quelle der Duplikatprobe (mit Einstellungssatz).</summary>
        private const string QUELLNAME = "Beispiel WP WG 1";

        // =============================================================================
        //  1 — Die Ablage
        // =============================================================================

        /// <summary>
        /// Der Schlüssel ist ASCII, eingefroren und <b>kein</b> <c>Properties.Settings</c>-Schlüssel;
        /// die Werksvorgabe ist aus.
        /// </summary>
        [Fact]
        public void Der_Schluessel_ist_ASCII_und_keine_Settings_Eigenschaft()
        {
            Assert.Equal("NeueProjekteMitKuehlung", EinstellungenCtrl.SCHLUESSEL_NEUE_PROJEKTE_MIT_KUEHLUNG);
            Assert.True(EinstellungenCtrl.SCHLUESSEL_NEUE_PROJEKTE_MIT_KUEHLUNG.All(c => c < 128));
            Assert.False(EinstellungenCtrl.NEUE_PROJEKTE_MIT_KUEHLUNG_VORGABE);
            Assert.Null(WindowsFormsApplication1.Properties.Settings.Default.Properties[EinstellungenCtrl.SCHLUESSEL_NEUE_PROJEKTE_MIT_KUEHLUNG]);
        }

        /// <summary>
        /// F-K20: Nichts hinterlegt heißt „aus" — im Kern wie im Wertesatz des Dialogs.
        /// Geschrieben wird über <c>Dienste.Einstellungen</c> (<c>SchreibZahl</c>), und der Satz
        /// liest den Wert von dort zurück.
        /// </summary>
        [Fact]
        public void Ohne_Eintrag_aus_und_geschrieben_ueber_Dienste_Einstellungen()
        {
            Assert.False(EinstellungenCtrl.NeueProjekteMitKuehlungLesen());
            Assert.False(EinstellungenCtrl.Lesen().NeueProjekteMitKuehlung);

            EinstellungenCtrl.NeueProjekteMitKuehlungSchreiben(true);
            Assert.Equal(1, Dienste.Einstellungen.LiesZahl(EinstellungenCtrl.SCHLUESSEL_NEUE_PROJEKTE_MIT_KUEHLUNG, -1));
            Assert.True(EinstellungenCtrl.NeueProjekteMitKuehlungLesen());
            Assert.True(EinstellungenCtrl.Lesen().NeueProjekteMitKuehlung);

            EinstellungenCtrl.NeueProjekteMitKuehlungSchreiben(false);
            Assert.Equal(0, Dienste.Einstellungen.LiesZahl(EinstellungenCtrl.SCHLUESSEL_NEUE_PROJEKTE_MIT_KUEHLUNG, -1));
            Assert.False(EinstellungenCtrl.Lesen().NeueProjekteMitKuehlung);
        }

        // =============================================================================
        //  2 — Der Anfangswert in beiden Anlagewegen (10.3, erster Fall)
        // =============================================================================

        /// <summary>
        /// Assistent, Programmeinstellung AUS (die Vorgabe): Die Anlage bleibt, wie sie war —
        /// kein Einstellungssatz vor dem ersten Speichern der Kaskade —, und das Projekt ist
        /// „aus".
        /// </summary>
        [Fact]
        public void Assistent_mit_Einstellung_aus_legt_ohne_Satz_an_und_das_Projekt_ist_aus()
        {
            if (!_db.Vorhanden) return;

            int id = PerAssistentAnlegen("Kuehlprobe Assistent aus");

            Assert.Equal(0L, Saetze(id));
            Assert.False(KonfigurationCtrl.KuehlbetriebLesen(id));
            Assert.Null(KonfigurationCtrl.LiesProjekt(id));
        }

        /// <summary>
        /// Assistent, Programmeinstellung AN: Das Projekt bekommt seinen Einstellungssatz beim
        /// Anlegen — mit denselben Vorbelegungen wie beim ersten Speichern der Kaskade — und
        /// <c>Kuehlbetrieb = 1</c>.
        /// </summary>
        [Fact]
        public void Assistent_mit_Einstellung_an_legt_den_Satz_mit_Kuehlbetrieb_1_an()
        {
            if (!_db.Vorhanden) return;
            EinstellungenCtrl.NeueProjekteMitKuehlungSchreiben(true);

            int id = PerAssistentAnlegen("Kuehlprobe Assistent an");

            Assert.Equal(1L, Saetze(id));
            Assert.True(KonfigurationCtrl.KuehlbetriebLesen(id));
            DataRow satz = Satz(id);
            Assert.Equal(1L, Convert.ToInt64(satz["Kuehlbetrieb"], CultureInfo.InvariantCulture));
            Assert.Equal(1L, Convert.ToInt64(satz[SchemaKatalog.SPALTE_EXTRAPOLATION_ERLAUBT], CultureInfo.InvariantCulture));
            Assert.Equal(DbWerte.KNAPPHEIT_DEFAULT, Convert.ToString(satz[SchemaKatalog.SPALTE_KANAL_KNAPPHEITSREIHENFOLGE], CultureInfo.InvariantCulture));
            Assert.Equal(0L, Convert.ToInt64(satz[SchemaKatalog.SPALTE_KASKADE_GEPFLEGT], CultureInfo.InvariantCulture));
            Assert.True(KonfigurationCtrl.LiesProjekt(id).Kuehlbetrieb);
        }

        /// <summary>
        /// Der zweite Anlageweg (<see cref="ProjektCtrl.Insert"/>) geht über dieselbe Stelle:
        /// aus → kein Satz, an → Satz mit 1. Seine Kennung ist die, die die Datenbank vergeben hat.
        /// </summary>
        [Fact]
        public void ProjektCtrl_Insert_uebernimmt_die_Programmeinstellung_ebenso()
        {
            if (!_db.Vorhanden) return;

            int aus = PerProjektCtrlAnlegen("Kuehlprobe Insert aus");
            Assert.Equal(0L, Saetze(aus));
            Assert.False(KonfigurationCtrl.KuehlbetriebLesen(aus));

            EinstellungenCtrl.NeueProjekteMitKuehlungSchreiben(true);
            int an = PerProjektCtrlAnlegen("Kuehlprobe Insert an");
            Assert.Equal(1L, Saetze(an));
            Assert.True(KonfigurationCtrl.KuehlbetriebLesen(an));
        }

        // =============================================================================
        //  3 — Bestandsprojekte bleiben aus; die Laufzeit fragt nie (10.3, zweiter Fall)
        // =============================================================================

        /// <summary>
        /// Nach KU-S2 trägt jedes vorhandene Projekt 0 — auch wenn die Programmeinstellung an
        /// ist, auch ein Projekt ohne Einstellungssatz, und auch nach dem Lesen der
        /// Konfiguration.
        /// </summary>
        [Fact]
        public void Bestandsprojekte_bleiben_aus_auch_mit_eingeschalteter_Programmeinstellung()
        {
            if (!_db.Vorhanden) return;
            EinstellungenCtrl.NeueProjekteMitKuehlungSchreiben(true);

            DataTable projekte = DataRepository.GetDataTable("SELECT ID FROM Tab_Projekt ORDER BY ID");
            int ohneSatz = 0;
            foreach (DataRow p in projekte.Rows)
            {
                int id = Convert.ToInt32(p["ID"], CultureInfo.InvariantCulture);
                Assert.False(KonfigurationCtrl.KuehlbetriebLesen(id), "Projekt " + id);
                KonfigurationModel m = KonfigurationCtrl.LiesProjekt(id);
                if (m == null) { ohneSatz++; continue; }
                Assert.False(m.Kuehlbetrieb, "Projekt " + id);
            }
            Assert.True(ohneSatz > 0, "Die Testdatenbank fuehrt kein Projekt ohne Einstellungssatz mehr.");
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Einstellungen WHERE Kuehlbetrieb <> 0"));
        }

        /// <summary>
        /// DIE PROBE: Ein vollständiger Lauf und das Lesen der Konfiguration eines Projekts fragen
        /// die Programmeinstellung kein einziges Mal — die Ablage zählt jeden Lesezugriff mit.
        /// </summary>
        [Fact]
        public void Lauf_und_Konfiguration_lesen_die_Programmeinstellung_nie()
        {
            if (!_db.Vorhanden) return;
            var ablage = new MitzaehlendeEinstellungen();
            Dienste.Einstellungen = ablage;
            ablage.SchreibZahl(EinstellungenCtrl.SCHLUESSEL_NEUE_PROJEKTE_MIT_KUEHLUNG, 1);

            Assert.NotNull(KonfigurationCtrl.LiesProjekt(REFERENZ));
            Assert.False(KonfigurationCtrl.KuehlbetriebLesen(REFERENZ));

            var laeufer = new SimulationRunner();
            Assert.True(laeufer.Simuliere(REFERENZ, out string fehler), fehler);

            Assert.Equal(0, ablage.Lesezugriffe(EinstellungenCtrl.SCHLUESSEL_NEUE_PROJEKTE_MIT_KUEHLUNG));
            Assert.False(KonfigurationCtrl.KuehlbetriebLesen(REFERENZ));
        }

        /// <summary>
        /// DER WÄCHTER: Die Programmeinstellung wird an genau ZWEI Stellen gelesen — im
        /// Einstellungsdialog (<c>EinstellungenCtrl</c>) und beim Anlegen eines Projekts
        /// (<c>KonfigurationCtrl.KuehlbetriebAnfangswertSetzen</c>) —, und den Anfangswert setzen
        /// allein die beiden Anlagewege. Wer den Wert anderswo liest, schaltet
        /// Bestandsprojekte mit (Kühlkonzept 7.2).
        /// </summary>
        [Fact]
        public void Waechter_die_Programmeinstellung_hat_zwei_Leser_und_zwei_Anlagewege()
        {
            string wurzel = Arbeitsbaum();
            string[] ordner = { "EPOS.Kern", "EPOS.UI", "EPOS.UI.Daten", "WindowsFormsApplication1", "EPOS.iOS",
                                "KiKern", "SpeicherEngine", "SpeicherPlanung", "Referenzlauf", "EPOS.Referenzlauf" };
            List<string> dateien = ordner.Select(o => Path.Combine(wurzel, o))
                                         .Where(Directory.Exists)
                                         .SelectMany(Quelltexte)
                                         .ToList();
            Assert.NotEmpty(dateien);

            Assert.Equal(new[] { "EinstellungenCtrl.cs", "KonfigurationCtrl.cs" },
                         Fundorte(dateien, "NeueProjekteMitKuehlungLesen("));
            Assert.Equal(new[] { "EinstellungenCtrl.cs" },
                         Fundorte(dateien, "SCHLUESSEL_NEUE_PROJEKTE_MIT_KUEHLUNG"));
            Assert.Equal(new[] { "EinstellungenCtrl.cs" },
                         Fundorte(dateien, "\"NeueProjekteMitKuehlung\""));
            Assert.Equal(new[] { "KonfigurationCtrl.cs", "ProjektCtrl.cs", "WizardCtrl.cs" },
                         Fundorte(dateien, "KuehlbetriebAnfangswertSetzen("));
        }

        // =============================================================================
        //  4 — Das Speichern der Kaskade erhält den Wert (10.3, dritter Fall)
        // =============================================================================

        /// <summary>
        /// Die Konfigurationsseite speichert die Kaskade als Löschen und Neuanlegen der ganzen
        /// Zeile. Ein Projekt mit <c>Kuehlbetrieb = 1</c> trägt danach weiter 1 — und eines
        /// mit 0 bleibt 0, auch bei eingeschalteter Programmeinstellung.
        /// </summary>
        [Fact]
        public void Das_Speichern_der_Kaskade_erhaelt_den_Kuehlbetrieb()
        {
            if (!_db.Vorhanden) return;

            Assert.True(KonfigurationCtrl.KuehlbetriebSchreiben(REFERENZ, true));
            long idVorher = Zahl("SELECT ID FROM Tab_Einstellungen WHERE ID_Projekt = " + REFERENZ);

            Assert.True(Kaskadendienste(REFERENZ).Speichern());

            Assert.NotEqual(idVorher, Zahl("SELECT ID FROM Tab_Einstellungen WHERE ID_Projekt = " + REFERENZ));
            Assert.Equal(1L, Saetze(REFERENZ));
            Assert.True(KonfigurationCtrl.KuehlbetriebLesen(REFERENZ));

            EinstellungenCtrl.NeueProjekteMitKuehlungSchreiben(true);
            Assert.True(KonfigurationCtrl.KuehlbetriebSchreiben(REFERENZ, false));
            Assert.True(Kaskadendienste(REFERENZ).Speichern());
            Assert.False(KonfigurationCtrl.KuehlbetriebLesen(REFERENZ));
        }

        // =============================================================================
        //  5 — Das Projektduplikat übernimmt den Wert der Quelle (10.3, vierter Fall)
        // =============================================================================

        /// <summary>
        /// Ein Duplikat ist kopiert, nicht neu angelegt: Es trägt den Wert seiner Quelle — 1 bei
        /// ausgeschalteter und 0 bei eingeschalteter Programmeinstellung.
        /// </summary>
        [Fact]
        public void Das_Projektduplikat_uebernimmt_den_Wert_der_Quelle()
        {
            if (!_db.Vorhanden) return;
            var kopierer = new ProjektDuplizierenCtrl();
            int quelle = kopierer.GetProjektId(QUELLNAME);
            Assert.True(quelle > 0);
            Assert.Equal(1L, Saetze(quelle));

            Assert.True(KonfigurationCtrl.KuehlbetriebSchreiben(quelle, true));
            int kopieAn = kopierer.Duplizieren(QUELLNAME, "Kuehlprobe Kopie an");
            Assert.True(kopieAn > 0, "Duplizieren fehlgeschlagen.");
            Assert.True(KonfigurationCtrl.KuehlbetriebLesen(kopieAn));

            EinstellungenCtrl.NeueProjekteMitKuehlungSchreiben(true);
            Assert.True(KonfigurationCtrl.KuehlbetriebSchreiben(quelle, false));
            int kopieAus = kopierer.Duplizieren(QUELLNAME, "Kuehlprobe Kopie aus");
            Assert.True(kopieAus > 0, "Duplizieren fehlgeschlagen.");
            Assert.False(KonfigurationCtrl.KuehlbetriebLesen(kopieAus));
        }

        // -----------------------------------------------------------------------------
        //  Handgriffe
        // -----------------------------------------------------------------------------

        private static int PerAssistentAnlegen(string name)
        {
            var modell = new ProjektModel
            {
                m_szProjektname = name,
                m_szBearbeiter = "Probe",
                m_szBeschreibung = "",
                m_szKunde = "",
                m_szKlimaregion = "",
                m_Aenderungsdatum = new DateTime(2026, 9, 23),
                m_Erstelldatum = new DateTime(2026, 9, 23),
            };
            int id = 0;
            Assert.True(new WizardCtrl().Add_Projekt(ref id, modell));
            Assert.True(id > 0);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = " + id));
            return id;
        }

        private static int PerProjektCtrlAnlegen(string name)
        {
            var ctrl = new ProjektCtrl
            {
                m_szProjektname = name,
                m_szBearbeiter = "Probe",
                m_Aenderungsdatum = new DateTime(2026, 9, 23),
                m_Erstelldatum = new DateTime(2026, 9, 23),
            };
            Assert.True(ctrl.Insert());
            Assert.Equal(Zahl("SELECT ID FROM Tab_Projekt WHERE Projektname = '" + name + "'"), ctrl.m_ID);
            return ctrl.m_ID;
        }

        private static long Saetze(int idProjekt)
            => Zahl("SELECT COUNT(*) FROM Tab_Einstellungen WHERE ID_Projekt = " + idProjekt.ToString(CultureInfo.InvariantCulture));

        private static DataRow Satz(int idProjekt)
            => DataRepository.GetDataTable("SELECT * FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                                           new DbParam("?", idProjekt)).Rows[0];

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        /// <summary>Die Datenseite der Simulationskonfiguration zu einem Projekt (Muster <c>HeizkesselKaskadeTests</c>).</summary>
        private static SimulationKonfigDienste Kaskadendienste(int idProjekt)
            => (SimulationKonfigDienste)SimulationKonfigHuelle.Erzeugen(idProjekt).Gaben()["Dienste"];

        /// <summary>Die Dateinamen, deren Quelltext (ohne Kommentarzeilen) das Muster enthält — sortiert.</summary>
        private static string[] Fundorte(IEnumerable<string> dateien, string muster)
        {
            return dateien.Where(d => File.ReadAllLines(d)
                                          .Select(z => z.TrimStart())
                                          .Where(z => !z.StartsWith("//", StringComparison.Ordinal) &&
                                                      !z.StartsWith("*", StringComparison.Ordinal))
                                          .Any(z => z.Contains(muster)))
                          .Select(Path.GetFileName)
                          .Distinct(StringComparer.Ordinal)
                          .OrderBy(n => n, StringComparer.Ordinal)
                          .ToArray();
        }

        private static IEnumerable<string> Quelltexte(string ordner)
        {
            char t = Path.DirectorySeparatorChar;
            return Directory.GetFiles(ordner, "*.*", SearchOption.AllDirectories)
                .Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                            p.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
                .Where(p => p.IndexOf(t + "bin" + t, StringComparison.Ordinal) < 0 &&
                            p.IndexOf(t + "obj" + t, StringComparison.Ordinal) < 0);
        }

        /// <summary>Die Wurzel des Arbeitsbaums — derselbe Weg wie in <c>ModultrennungswacheTests</c>.</summary>
        private static string Arbeitsbaum([CallerFilePath] string eigeneDatei = null)
        {
            if (!string.IsNullOrEmpty(eigeneDatei))
            {
                string ordner = Path.GetDirectoryName(eigeneDatei);
                string wurzel = ordner == null ? null : Path.GetDirectoryName(ordner);
                if (wurzel != null && File.Exists(Path.Combine(wurzel, "WP-Plan.sln"))) return wurzel;
            }

            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null)
            {
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
                d = d.Parent;
            }

            Assert.Fail("Die Wurzel des Arbeitsbaums (WP-Plan.sln) ist nicht zu finden.");
            return null;
        }

        /// <summary>Eine flüchtige Ablage, die jeden Lesezugriff je Schlüssel mitzählt.</summary>
        private sealed class MitzaehlendeEinstellungen : IEinstellungen
        {
            private readonly FluechtigeEinstellungen _innen = new FluechtigeEinstellungen();
            private readonly Dictionary<string, int> _gelesen = new Dictionary<string, int>(StringComparer.Ordinal);

            public int Lesezugriffe(string schluessel)
            {
                lock (_gelesen) return _gelesen.TryGetValue(schluessel, out int n) ? n : 0;
            }

            private void Zaehle(string schluessel)
            {
                if (schluessel == null) return;
                lock (_gelesen) _gelesen[schluessel] = Lesezugriffe(schluessel) + 1;
            }

            public string Lies(string schluessel, string vorgabe = null) { Zaehle(schluessel); return _innen.Lies(schluessel, vorgabe); }
            public int LiesZahl(string schluessel, int vorgabe = 0) { Zaehle(schluessel); return _innen.LiesZahl(schluessel, vorgabe); }
            public void Schreib(string schluessel, string wert) => _innen.Schreib(schluessel, wert);
            public void SchreibZahl(string schluessel, int wert) => _innen.SchreibZahl(schluessel, wert);
            public void Loesche(string schluessel) => _innen.Loesche(schluessel);
            public string LiesMaschine(string schluessel, string vorgabe = null) { Zaehle(schluessel); return _innen.LiesMaschine(schluessel, vorgabe); }
        }
    }
}
