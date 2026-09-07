using System;
using System.Collections.Generic;
using System.IO;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Befund W7‑B‑3</b> der Windows-Abnahme V2 vom 07.09.2026: „Energieerzeuger →
    /// Wärmepumpe (Projekt ‚Stromspeicher mit Wärmepumpe'): Hier im Beispiel T800-2,
    /// im Projekt-Wärmepumpen-Dialog keine Kennlinie."
    ///
    /// <para><b>Die Ursache war die TABELLE, nicht die Datenlage</b> — Fall (a) der
    /// drei Kandidaten. Der Delegat <c>Bilder</c> der Anlagenhülle rief
    /// <c>WaermepumpeStammHuelle.BilderZu</c> und damit <c>KenndatenCtrl.Reihen</c>;
    /// jene Abfrage geht auf <c>Tab_Kenndaten_STAMM</c>, übergeben wird ihr aber
    /// <c>Daten.IdWp</c> — bei einer gespeicherten Anlage die Id der PROJEKTKOPIE.
    /// Fall 1 hält beide Abfragen gegen dasselbe Gerät und zeigt den Unterschied
    /// unmittelbar: 16 Stützstellen in <c>Tab_Kenndaten</c>, keine einzige in
    /// <c>Tab_Kenndaten_STAMM</c>.</para>
    ///
    /// <para><b>Was NICHT die Ursache war.</b> (b) Die Projektkopie fehlt nicht — sie
    /// ist vollständig vorhanden; das gilt für alle 38 Gerätekopien der
    /// Testdatenbank. (c) Die Kennlinienart stimmt: T 800-2 führt Wärmekennlinien und
    /// gar keine Kühlkennlinien, und der Reiter fragt die Wärme.</para>
    ///
    /// <para><b>Der RÜCKFALL</b> (Fälle 3 bis 7) ist die zweite Hälfte des Auftrags:
    /// Fehlen die Projektkennlinien wirklich, zeigt der Dialog den Katalogsatz
    /// gleichen Namens — als Herleitung, nicht als Projektwahrheit — und bietet an,
    /// ihn nachzuholen.</para>
    ///
    /// <para>Der Rechenweg bleibt unberührt: Er liest unverändert
    /// <c>Tab_Kenndaten</c>, und die Referenzprojekte haben ihre Kennlinien. Der
    /// Referenzlauf bleibt byte-gleich.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class WaermepumpeKennlinienTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public WaermepumpeKennlinienTests(TestDatenbank db) { _db = db; }

        /// <summary>Das Projekt, in dem die Wegwerf-Gerätekopien entstehen.</summary>
        private const int TESTPROJEKT = 1030;

        /// <summary>Der Katalogsatz, an dem der Befund gemeldet wurde — mit 16 Stützstellen.</summary>
        private const string KATALOGGERAET = "T 800-2";

        // =================================================================================
        // 1-2 — Der Befund selbst
        // =================================================================================

        /// <summary>
        /// <b>Fall 1 — die belegte Ursache.</b> Dieselbe Id, zwei Tabellen: Die
        /// Projektkopie von T 800-2 im Projekt 1006 führt ihre Kennlinien in
        /// <c>Tab_Kenndaten</c>; unter derselben Id steht in
        /// <c>Tab_Kenndaten_STAMM</c> nichts. Der alte Weg konnte deshalb nur „Keine
        /// Kennlinien vorhanden" zeigen.
        /// </summary>
        [Fact]
        public void Der_alte_Weg_findet_zur_Projektkopie_keine_einzige_Zeile()
        {
            if (!_db.Vorhanden) return;

            int idWp = ProjektgeraetSuchen(1006, KATALOGGERAET);
            Assert.True(idWp > 0, "Die Testdatenbank führt die Projektkopie T 800-2 im Projekt 1006 nicht mehr.");

            // Der alte Weg: Tab_Kenndaten_STAMM mit einer Tab_WP-Id.
            Assert.Empty(KenndatenCtrl.Reihen(idWp).Vorlaeufe);

            // Der neue: die Projektkopie - und die hat vier Vorlaufstufen.
            KennlinienSatz projekt = KenndatenCtrl.ReihenProjekt(idWp);
            Assert.Equal(new List<int> { 35, 45, 55, 65 }, projekt.Vorlaeufe);
        }

        /// <summary>
        /// <b>Fall 2</b>: Der Dialogweg liefert für dieselbe Anlage die Bilder der
        /// PROJEKTKOPIE — und sagt es auch. Ein Nachholen gibt es hier nicht: Es fehlt
        /// nichts.
        /// </summary>
        [Fact]
        public void Eine_gespeicherte_Anlage_zeigt_ihre_Projektkennlinien()
        {
            if (!_db.Vorhanden) return;

            int idWp = ProjektgeraetSuchen(1006, KATALOGGERAET);
            Assert.True(idWp > 0);

            WaermepumpeKennlinienCtrl.Quelle q = WaermepumpeKennlinienCtrl.FuerAnlage(idWp);

            Assert.Equal(WaermepumpeKennlinienCtrl.Herkunft.Projekt, q.Woher);
            Assert.False(q.Nachholbar);
            Assert.Equal(4, q.Satz.Cop.Count);
            Assert.Equal(4, q.Satz.Leistung.Count);

            // Und die Vorlaufliste des Dialogs kommt aus derselben Quelle - sie hing
            // an derselben falschen Tabelle und blieb leer.
            Assert.Equal(new List<int> { 35, 45, 55, 65 },
                         WaermepumpeKennlinienCtrl.VorlaeufeFuerAnlage(idWp));
        }

        // =================================================================================
        // 3-5 — Der Rückfall auf den Katalog und das Nachholen
        // =================================================================================

        /// <summary>
        /// <b>Fall 3</b>: Eine Gerätekopie OHNE Kennlinien zeigt den Katalogsatz
        /// gleichen Namens — ausgewiesen als Herkunft „Katalog", mit dem Katalogsatz
        /// benannt und als nachholbar gekennzeichnet.
        /// </summary>
        [Fact]
        public void Eine_Kopie_ohne_Kennlinien_faellt_auf_den_Katalog_zurueck()
        {
            if (!_db.Vorhanden) return;

            int kopie = KopieOhneKennlinienAnlegen(KATALOGGERAET);
            try
            {
                WaermepumpeKennlinienCtrl.Quelle q = WaermepumpeKennlinienCtrl.FuerAnlage(kopie);

                Assert.Equal(WaermepumpeKennlinienCtrl.Herkunft.Katalog, q.Woher);
                Assert.True(q.Nachholbar);
                Assert.True(q.KatalogId > 0);
                Assert.Equal(new List<int> { 35, 45, 55, 65 }, q.Satz.Vorlaeufe);
            }
            finally { KopieLoeschen(kopie); }
        }

        /// <summary>
        /// <b>Fall 4 — der Knopf.</b> „Kennlinien aus dem Katalog übernehmen" schreibt
        /// die Stützstellen in die Projektkopie; danach ist die Herkunft „Projekt",
        /// und der Rückfall wird nicht mehr gebraucht. Das ist der Zustand, in dem
        /// auch der LAUF wieder rechnen kann — er liest dieselbe Tabelle.
        /// </summary>
        [Fact]
        public void Der_Knopfweg_holt_die_Kennlinien_in_das_Projekt()
        {
            if (!_db.Vorhanden) return;

            int kopie = KopieOhneKennlinienAnlegen(KATALOGGERAET);
            try
            {
                Assert.Equal(0, Zeilen("SELECT COUNT(*) FROM Tab_Kenndaten WHERE ID_WP = ?", kopie));

                int geschrieben = new WPCtrl().KennlinienAusKatalog(kopie);

                Assert.Equal(16, geschrieben);
                Assert.Equal(16, Zeilen("SELECT COUNT(*) FROM Tab_Kenndaten WHERE ID_WP = ?", kopie));

                WaermepumpeKennlinienCtrl.Quelle q = WaermepumpeKennlinienCtrl.FuerAnlage(kopie);
                Assert.Equal(WaermepumpeKennlinienCtrl.Herkunft.Projekt, q.Woher);
                Assert.Equal(new List<int> { 35, 45, 55, 65 }, q.Satz.Vorlaeufe);
            }
            finally { KopieLoeschen(kopie); }
        }

        /// <summary>
        /// <b>Fall 5</b>: Ein zweiter Druck auf denselben Knopf verdoppelt nichts —
        /// geschrieben wird nur in eine Tabelle, die für dieses Gerät KEINE Zeile
        /// führt. Was der Anwender gepflegt hat, kann der Knopf damit nicht
        /// überschreiben.
        /// </summary>
        [Fact]
        public void Was_schon_da_ist_bleibt_unangetastet()
        {
            if (!_db.Vorhanden) return;

            int kopie = KopieOhneKennlinienAnlegen(KATALOGGERAET);
            try
            {
                Assert.Equal(16, new WPCtrl().KennlinienAusKatalog(kopie));
                Assert.Equal(0, new WPCtrl().KennlinienAusKatalog(kopie));
                Assert.Equal(16, Zeilen("SELECT COUNT(*) FROM Tab_Kenndaten WHERE ID_WP = ?", kopie));
            }
            finally { KopieLoeschen(kopie); }
        }

        // =================================================================================
        // 6-8 — Die Ränder
        // =================================================================================

        /// <summary>
        /// <b>Fall 6</b>: Solange der Anwender eine Wärmepumpe erst AUSWÄHLT, trägt
        /// <c>Daten.IdWp</c> die Katalog-Id — die Projektkopie legt <c>WizardCtrl</c>
        /// erst beim Speichern an. Dann sind die Katalogkennlinien die einzige Quelle,
        /// und nachzuholen gibt es nichts: Der Knopf erscheint nicht.
        /// </summary>
        [Fact]
        public void Eine_noch_nicht_gespeicherte_Wahl_zeigt_den_Katalog_ohne_Knopf()
        {
            if (!_db.Vorhanden) return;

            int stammId = DataRepository.GetIdByName(WPStammCtrl.TABLE, "Bezeichner", KATALOGGERAET);
            Assert.True(stammId > 0);

            WaermepumpeKennlinienCtrl.Quelle q = WaermepumpeKennlinienCtrl.FuerAnlage(stammId);

            Assert.Equal(WaermepumpeKennlinienCtrl.Herkunft.Katalog, q.Woher);
            Assert.False(q.Nachholbar);
            Assert.Equal(stammId, q.KatalogId);
        }

        /// <summary>
        /// <b>Fall 7</b>: Eine Gerätekopie, deren Bezeichner im Katalog nicht (mehr)
        /// steht — umbenannt oder frei angelegt —, hat keinen Rückfall. Dann bleibt es
        /// beim Platzhalter, und der Knopf verspricht nichts. Die Testdatenbank führt
        /// diesen Fall selbst: „CS6800iAW MB + AW 12 OR-T (2)" im Projekt 1011.
        /// </summary>
        [Fact]
        public void Ohne_Katalogsatz_gleichen_Namens_bleibt_es_beim_Platzhalter()
        {
            if (!_db.Vorhanden) return;

            int kopie = KopieOhneKennlinienAnlegen("W7B3 Gerät ohne Katalogsatz");
            try
            {
                WaermepumpeKennlinienCtrl.Quelle q = WaermepumpeKennlinienCtrl.FuerAnlage(kopie);

                Assert.Equal(WaermepumpeKennlinienCtrl.Herkunft.Ohne, q.Woher);
                Assert.False(q.Nachholbar);
                Assert.Empty(q.Satz.Vorlaeufe);

                // Und der Knopfweg schreibt nichts, statt zu scheitern.
                Assert.Equal(0, new WPCtrl().KennlinienAusKatalog(kopie));
            }
            finally { KopieLoeschen(kopie); }
        }

        /// <summary>
        /// <b>Fall 8</b>: Eine Id, die es gar nicht gibt, ist kein Fehler, sondern ein
        /// leerer Satz — der Dialog zeigt seinen Platzhalter.
        /// </summary>
        [Fact]
        public void Eine_unbekannte_Id_liefert_einen_leeren_Satz()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(WaermepumpeKennlinienCtrl.Herkunft.Ohne,
                         WaermepumpeKennlinienCtrl.FuerAnlage(0).Woher);
            Assert.Equal(WaermepumpeKennlinienCtrl.Herkunft.Ohne,
                         WaermepumpeKennlinienCtrl.FuerAnlage(987654321).Woher);
            Assert.Equal(-1, new WPCtrl().KennlinienAusKatalog(987654321));
        }

        // =================================================================================
        // 9 — Der HINWEIS im Laufprotokoll
        // =================================================================================

        /// <summary>
        /// <b>Fall 9 — der Hinweis im Laufprotokoll.</b>
        ///
        /// <para><b>Erst der Befund:</b> Eine Anlage ohne Projektkennlinien rechnet im
        /// Lauf NICHT still mit 0. <c>SimulationWaermepumpe.ModuleAufbauen</c> zählt
        /// die Stützstellen des eingestellten Vorlaufs und bricht bei 0 mit
        /// <c>SIMENG_WP_KEINE_KENNDATEN</c> über den Fehlerkanal ab (Paket 8, Konzept
        /// 13.4). Zwei verschiedene Lagen führen aber dorthin, und die Meldung nannte
        /// nur die eine: „keine Kenndaten für Vorlauf X" liest sich wie eine Frage der
        /// Vorlaufeinstellung, auch wenn das Gerät ÜBERHAUPT keine Kennlinien hat.</para>
        ///
        /// <para>Deshalb schreibt der Lauf in der zweiten Lage zusätzlich einen
        /// HINWEIS ins Protokoll, der auf den Knopf im Dialog zeigt. Geprüft wird die
        /// REGEL an der Quelle — den Lauf selbst kann diese Probe nicht führen, und
        /// die vier Referenzprojekte erreichen den Zweig nie (sie haben ihre
        /// Kennlinien), weshalb der Referenzlauf byte-gleich bleibt.</para>
        /// </summary>
        [Fact]
        public void Der_Lauf_bricht_ab_und_nennt_den_Weg_zur_Behebung()
        {
            string quelle = Quelltext("EPOS.Kern", "Allgemein", "Simulation", "SimulationWaermepumpe.cs");

            // Der Abbruch steht unveraendert da - es wird nicht still mit 0 gerechnet.
            Assert.Contains("SIMENG_WP_KEINE_KENNDATEN", quelle);

            // ... und daneben der neue Hinweis fuer die zweite Lage.
            Assert.Contains("SIMENG_WP_KENNLINIEN_FEHLEN", quelle);
            Assert.Contains("SimulationProtokoll.Aktuell.Hinweis", quelle);

            // Der Text selbst steht in BEIDEN Sprachen und traegt den Anlagennamen.
            Assert.Contains("{0}", WindowsFormsApplication1.MyResource.Resource.SIMENG_WP_KENNLINIEN_FEHLEN);
            Assert.Contains("SIMENG_WP_KENNLINIEN_FEHLEN",
                            Quelltext("EPOS.Kern", "MyResource", "Resource.en-US.resx"));
        }

        // =================================================================================
        // Hilfsmittel
        // =================================================================================

        /// <summary>Die Id einer Gerätekopie in einem Projekt; 0, wenn es sie nicht gibt.</summary>
        private static int ProjektgeraetSuchen(int idProjekt, string bezeichner)
        {
            return new WPCtrl().GetProjektId(bezeichner, idProjekt);
        }

        /// <summary>
        /// Eine Wegwerf-Gerätekopie OHNE Kennlinienzeilen — genau die Lage, die der
        /// Befund beschreibt. <c>CopyFromStamm</c> kann sie nicht herstellen: Jene
        /// Methode legt die Kennlinien immer mit an.
        /// </summary>
        private static int KopieOhneKennlinienAnlegen(string bezeichner)
        {
            int id = DataRepository.GetMaxID("Tab_WP") + 1;
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO Tab_WP (ID, ID_Projekt, Bezeichner, Nennleistung) VALUES (?,?,?,?)",
                new DbParam("@id", id), new DbParam("@p", TESTPROJEKT),
                new DbParam("@b", bezeichner), new DbParam("@n", 12)));
            return id;
        }

        /// <summary>
        /// Räumt die Wegwerfkopie weg. Die Kennlinienzeilen gehen über die
        /// Löschweitergabe des Fremdschlüssels mit (<c>Tab_Kenndaten.ID_WP</c> →
        /// <c>Tab_WP.ID</c>, <c>ON DELETE CASCADE</c>); sicherheitshalber werden sie
        /// vorher genannt, denn der Referenzlauf darf keine Waise erben.
        /// </summary>
        private static void KopieLoeschen(int id)
        {
            if (id <= 0) return;
            DataRepository.ExecuteSQL("DELETE FROM Tab_Kenndaten WHERE ID_WP = ?", new DbParam("@id", id));
            DataRepository.ExecuteSQL("DELETE FROM Tab_Kenndaten_Kuehlung WHERE ID_WP = ?", new DbParam("@id", id));
            DataRepository.ExecuteSQL("DELETE FROM Tab_WP WHERE ID = ?", new DbParam("@id", id));
        }

        private static int Zeilen(string sql, int id)
        {
            object v = DataRepository.ExecuteScalar(sql, new DbParam("@id", id));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        /// <summary>Eine Quelldatei des Bestands, von der Projektwurzel aus gesucht.</summary>
        private static string Quelltext(params string[] teile)
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, Path.Combine(teile))))
                d = d.Parent;

            Assert.True(d != null, "Quelldatei " + Path.Combine(teile) + " nicht gefunden");
            return File.ReadAllText(Path.Combine(d.FullName, Path.Combine(teile)));
        }
    }
}
