using System;
using System.Data;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Sitzt die Schreibnaht wirklich in der Zugriffsschicht?</b> (Welle iF30)
    ///
    /// <para><see cref="SchreibnahtTests"/> prüft die REGEL ohne Datenbank. Hier steht die
    /// andere Hälfte: dass jeder der Wege — die sechs Zugriffsmethoden der Fassade, der
    /// Datenbankvorgang, <c>StilleDb</c> und <c>RecordSet</c> — tatsächlich an
    /// <c>SqliteDatenzugriff.ErzeugeKommando</c> vorbeikommt und dort abgewiesen wird.
    /// Ohne diese Fälle wäre „die eine Naht" eine Behauptung.</para>
    ///
    /// <para><b>Der Aufbau ist die Umkehrung der Testvorrichtung.</b>
    /// <see cref="TestDatenbank"/> hebt die Sperre für jede Testklasse
    /// (<c>Schreibnaht.WerkzeugFreigabe</c>) — sonst liefe kein einziger schreibender
    /// Kern-Test mehr. Diese Fälle stellen sie danach für sich selbst wieder her und
    /// nehmen das am Ende zurück; nur so lässt sich der gesperrte Zustand überhaupt
    /// herstellen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SchreibnahtDatenbankTests
    {
        /// <summary>Das Regressionsprojekt der Referenzlaeufe (Id 1030).</summary>
        private const int PROJEKT = 1030;

        /// <summary>
        /// Setzt die Schreibsperre INNERHALB einer offenen Testvorrichtung wieder in Kraft
        /// und nimmt das beim Verlassen zurück.
        /// </summary>
        private sealed class Lesemodus : IDisposable
        {
            private readonly Func<bool> _vorher;

            public Lesemodus()
            {
                _vorher = Schreibnaht.Schreibrecht;
                Schreibnaht.Schreibrecht = () => false;
            }

            public void Dispose() => Schreibnaht.Schreibrecht = _vorher;
        }

        // =====================================================================
        //  S1 — Lesen bleibt frei
        // =====================================================================

        /// <summary>
        /// S1: Im Lesemodus liest die Anwendung unverändert — Projekte öffnen, Ergebnisse
        /// ansehen, Berichte. Genau das verlangt § 6 des Lizenzierungskonzepts.
        /// </summary>
        [Fact]
        public void S1_Lesen_bleibt_im_Lesemodus_frei()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var sperre = new Lesemodus();

            DataTable dt = DataRepository.GetDataTable(
                "SELECT Projektname FROM Tab_Projekt WHERE ID = ?", new DbParam("?", PROJEKT));

            Assert.NotNull(dt);
            Assert.Equal(1, dt.Rows.Count);

            // Auch die Schemaauskunft und der stille Weg lesen weiter.
            Assert.True(DataRepository.TabelleVorhanden("Tab_Projekt"));
            Assert.NotNull(StilleDb.Tabelle("SELECT COUNT(*) AS n FROM Tab_Projekt"));
        }

        // =====================================================================
        //  S2 — Die vier Zugriffsmethoden der Fassade
        // =====================================================================

        /// <summary>
        /// S2: <c>ExecuteSQL</c>, <c>ExecuteNonQuery</c>, <c>ExecuteInsertAndGetId</c> und
        /// <c>ExecuteScalar</c> schreiben im Lesemodus NICHT und melden den Fehlausgang
        /// ihres jeweiligen Vertrags (<c>false</c>, <c>-1</c>, <c>0</c>, <c>null</c>).
        /// </summary>
        [Fact]
        public void S2_Die_Fassade_schreibt_im_Lesemodus_nicht()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string vorher = Projektname();
            using (new Lesemodus())
            {
                Assert.False(DataRepository.ExecuteSQL(
                    "UPDATE Tab_Projekt SET Projektname = ? WHERE ID = ?",
                    new DbParam("?", "iF30"), new DbParam("?", PROJEKT)));

                Assert.Equal(-1, DataRepository.ExecuteNonQuery(
                    "UPDATE Tab_Projekt SET Projektname = ? WHERE ID = ?",
                    new DbParam("?", "iF30"), new DbParam("?", PROJEKT)));

                Assert.Equal(0, DataRepository.ExecuteInsertAndGetId(
                    "INSERT INTO Tab_Projekt (Projektname) VALUES (?)",
                    new[] { new DbParam("?", "iF30") }));

                Assert.Null(DataRepository.ExecuteScalar(
                    "DELETE FROM Tab_Projekt WHERE ID = ?", new DbParam("?", PROJEKT)));
            }

            Assert.Equal(vorher, Projektname());
        }

        // =====================================================================
        //  S3 — Der Datenbankvorgang reicht die Ausnahme durch
        // =====================================================================

        /// <summary>
        /// S3: <c>DbVorgang</c> fängt bewusst nichts ab (so war es schon immer) — die
        /// <see cref="LesemodusException"/> kommt beim Aufrufer an, und ohne
        /// <c>Commit</c> wird zurückgerollt. Der Vorgang selbst darf sich öffnen: Ein
        /// Lesen in einer Transaktion ist im Lesemodus erlaubt.
        /// </summary>
        [Fact]
        public void S3_Der_Vorgang_reicht_die_Ausnahme_durch()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string vorher = Projektname();
            using (new Lesemodus())
            using (DbVorgang vorgang = DataRepository.Vorgang())
            {
                // Lesen in der Transaktion geht.
                Assert.NotNull(vorgang.Lese("SELECT ID FROM Tab_Projekt WHERE ID = ?",
                                            new DbParam("?", PROJEKT)));

                Assert.Throws<LesemodusException>(() => vorgang.Ausfuehren(
                    "UPDATE Tab_Projekt SET Projektname = ? WHERE ID = ?",
                    new DbParam("?", "iF30"), new DbParam("?", PROJEKT)));

                Assert.Throws<LesemodusException>(() => vorgang.EinfuegenUndId(
                    "INSERT INTO Tab_Projekt (Projektname) VALUES (?)",
                    new[] { new DbParam("?", "iF30") }));
            }

            Assert.Equal(vorher, Projektname());
        }

        // =====================================================================
        //  S4 — Die stillen Wege
        // =====================================================================

        /// <summary>
        /// S4: <c>StilleDb</c> und <c>RecordSet</c> gehen an derselben Naht vorbei — sie
        /// melden ihren Fehlausgang (<c>-1</c> bzw. <c>false</c>), und geschrieben wird
        /// nichts.
        /// </summary>
        [Fact]
        public void S4_Auch_die_stillen_Wege_schreiben_nicht()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string vorher = Projektname();
            using (new Lesemodus())
            {
                Assert.Equal(-1, StilleDb.NonQuery(
                    "UPDATE Tab_Projekt SET Projektname = ? WHERE ID = ?",
                    new DbParam("?", "iF30"), new DbParam("?", PROJEKT)));

                var satz = new RecordSet();
                Assert.False(satz.Insert(
                    "UPDATE Tab_Projekt SET Projektname = 'iF30' WHERE ID = " + PROJEKT));
            }

            Assert.Equal(vorher, Projektname());
        }

        // =====================================================================
        //  S5 — Die benannte Ausnahme wirkt AN DER DATENBANK
        // =====================================================================

        /// <summary>
        /// S5: Eine <c>Schreibnaht.Freigabe</c> mit Grund lässt denselben Schreibweg im
        /// Lesemodus durch. Das ist der Nachweis für die Ausnahmen des Programms —
        /// Erststart- und Schemamigration, Programmzustand, Sicherung.
        /// </summary>
        [Fact]
        public void S5_Eine_benannte_Freigabe_schreibt_auch_im_Lesemodus()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string vorher = Projektname();
            using (new Lesemodus())
            using (Schreibnaht.Freigabe(Schreibnaht.GRUND_MIGRATION))
            {
                Assert.True(DataRepository.ExecuteSQL(
                    "UPDATE Tab_Projekt SET Projektname = ? WHERE ID = ?",
                    new DbParam("?", "iF30 Freigabe"), new DbParam("?", PROJEKT)));
            }

            Assert.Equal("iF30 Freigabe", Projektname());

            // Zurueckschreiben - die Arbeitskopie faellt zwar ohnehin mit der Vorrichtung,
            // aber ein Fall raeumt hinter sich auf.
            DataRepository.ExecuteSQL("UPDATE Tab_Projekt SET Projektname = ? WHERE ID = ?",
                                      new DbParam("?", vorher), new DbParam("?", PROJEKT));
        }

        // =====================================================================
        //  S6 — Der Programmzustand bleibt schreibbar
        // =====================================================================

        /// <summary>
        /// S6: <c>Tab_Applikation</c> — „welches Projekt ist zuletzt geöffnet worden" —
        /// wird auch im Lesemodus fortgeschrieben. Ohne diese Ausnahme könnte ein
        /// Anwender mit abgelaufener Lizenz kein Projekt mehr ÖFFNEN, und genau das
        /// erlaubt § 6 ausdrücklich.
        /// </summary>
        [Fact]
        public void S6_Der_Programmzustand_bleibt_im_Lesemodus_schreibbar()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            using (new Lesemodus())
            {
                var app = new ApplikationCtrl();
                app.ReadSingle();
                app.m_szProjektname = "iF30 zuletzt";
                Assert.True(app.Update());

                Assert.Equal(SchemaStand.Zielversion, ApplikationCtrl.GetSchemaVersion());
                Assert.True(ApplikationCtrl.SetSchemaVersion(SchemaStand.Zielversion));
            }

            Assert.Equal("iF30 zuletzt",
                         Text(DataRepository.ExecuteScalar(
                             "SELECT Projektname FROM Tab_Applikation WHERE ID = 1")));
        }

        // =====================================================================
        //  S7 — Der Simulationslauf verweigert VOR dem Start
        // =====================================================================

        /// <summary>
        /// S7: <c>SimulationLaufCtrl.Vorpruefen</c> meldet den Lesemodus als ERSTEN
        /// Grund — vor allen fachlichen Prüfungen und lange vor dem Speichern. Ein Lauf,
        /// der erst nach Minuten an der Naht aufliefe, wäre für den Anwender ein Rätsel.
        /// </summary>
        [Fact]
        public void S7_Der_Simulationslauf_verweigert_vor_dem_Start()
        {
            using (new Lesemodus())
            {
                // Auch mit vollstaendig unbrauchbaren Fachangaben (konfig == null) steht
                // der Lizenzgrund vorn - er sticht SIM_MSG_KONFIGURATION_FEHLT.
                Assert.Equal(Resource.SIM_MSG_LESEMODUS,
                             SimulationLaufCtrl.Vorpruefen(PROJEKT, null, 0));
                Assert.Equal(Resource.SIM_MSG_LESEMODUS,
                             SimulationLaufCtrl.LesemodusGrund());
            }

            // Mit Schreibrecht meldet dieselbe Pruefung wieder ihre Fachgruende.
            Schreibnaht.WerkzeugFreigabe("Prüfstand iF30 S7");
            try
            {
                Assert.Null(SimulationLaufCtrl.LesemodusGrund());
                Assert.Equal(Resource.SIM_MSG_KONFIGURATION_FEHLT,
                             SimulationLaufCtrl.Vorpruefen(PROJEKT, null, 0));
            }
            finally
            {
                Schreibnaht.WerkzeugFreigabeZuruecknehmen();
            }
        }

        // =====================================================================

        private static string Projektname()
        {
            return Text(DataRepository.ExecuteScalar(
                "SELECT Projektname FROM Tab_Projekt WHERE ID = ?", new DbParam("?", PROJEKT)));
        }

        private static string Text(object wert)
        {
            return wert == null || wert == DBNull.Value ? "" : (Convert.ToString(wert) ?? "");
        }
    }
}
