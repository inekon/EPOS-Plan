using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>FK-1 — die Projektkopie eines Katalogsatzes trägt ihren Projektbezug</b>
    /// (Anwenderentscheid vom 21.09.2026, „FK beheben, vor allen anderen Aufgaben").
    ///
    /// <para><b>Der Befund.</b> Das Übernehmen eines Standard-Stromprofils scheiterte
    /// mit „SQLite Error 19: FOREIGN KEY constraint failed" beim Einfügen in
    /// <c>Z_Projekt_Stromverbraucher</c>. Die Ursache lag eine Ebene tiefer:
    /// <c>StromverbraucherStammCtrl.CopyFromStamm</c> liess im Insert des Typprofils
    /// die Spalte <c>ID_Projekt</c> weg. Sie trug <c>DEFAULT 0</c>, und seit
    /// Schemaschritt 96 steht auf ihr ein Fremdschlüssel auf <c>Tab_Projekt(ID)</c> —
    /// ein Projekt 0 gibt es nicht. Die ganze Kopie wurde zurückgerollt,
    /// <c>CopyFromStamm</c> gab −1, und der Assistentenschritt schrieb daraufhin die
    /// KATALOG-Id in die Projektzuordnung.</para>
    ///
    /// <para><b>Was hier festgehalten wird.</b> Alle drei Katalogkopierer —
    /// Stromverbraucher, Brauchwasser, Prozesswärme — laufen mit einem Stammsatz
    /// SAMT Typprofil durch, und Kopf wie Typprofil tragen danach die richtige
    /// <c>ID_Projekt</c>. Keine Zeile bleibt bei 0 oder NULL stehen.</para>
    ///
    /// <para><b>Und der Rückfall ist abgeschafft.</b> Scheitert eine Kopie doch —
    /// weil es den Katalogsatz nicht gibt —, so bricht der Assistentenschritt
    /// (<c>WizardCtrl.Add_Projekt_Stromverbraucher</c>, <c>…_Prozess</c>,
    /// <c>…_Brauchwasser</c>) BENANNT ab und schreibt nichts. Eine Katalog-Id in
    /// einer Projektzuordnung wäre die nächste Fremdschlüsselmeldung an einer Stelle,
    /// die mit der Ursache nichts zu tun hat.</para>
    ///
    /// <para><b>Jeder Fall bekommt seine eigene Arbeitskopie</b> — die Fälle
    /// schreiben. Ohne Datenbank schweigen sie
    /// (<see cref="TestDatenbank.Vorhanden"/>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KatalogKopieProjektbezugTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        /// <summary>
        /// Das Zielprojekt. 1030 führt keinen der drei Sätze — jeder Kopierer legt
        /// also wirklich an, statt eine vorhandene Kopie wiederzufinden.
        /// </summary>
        private const int PROJEKT = 1030;

        /// <summary>Der Katalogsatz aus der Gerätemeldung (Typ <c>REH_1</c>).</summary>
        private const string SV_KATALOG = "EFH_3_Pers";

        /// <summary>Ein Brauchwasser-Stammsatz mit Typprofil (Typ „EFH Wohnen").</summary>
        private const string BW_KATALOG = "EFH Wohnen, 1 Person";

        /// <summary>Ein Prozesswärme-Stammsatz mit Typprofil (Typ „CONT").</summary>
        private const string PW_KATALOG = "CONT";

        /// <summary>
        /// Ein Name, den kein Katalog führt — damit scheitert jede der drei Kopien,
        /// und der Assistentenschritt muss abbrechen statt zurückzufallen.
        /// </summary>
        private const string OHNE_KATALOGSATZ = "FK-1 Probe ohne Katalogsatz";

        public void Dispose() => _db.Dispose();

        // =====================================================================
        //  Die drei Katalogkopierer
        // =====================================================================

        /// <summary>
        /// DER FALL AUS DER GERÄTEMELDUNG. Kopf und Typprofil des Stromverbrauchers
        /// stehen mit der Projektnummer.
        /// </summary>
        [Fact]
        public void Der_Stromverbraucher_kommt_samt_Typprofil_mit_Projektbezug_ins_Projekt()
        {
            using var _ = new Kulturvorrichtung();
            if (!_db.Vorhanden) return;

            int id = StromverbraucherStammCtrl.CopyFromStamm(SV_KATALOG, PROJEKT);
            Assert.True(id > 0, "CopyFromStamm hat keine Projektkopie geliefert (" + id + ").");

            Assert.Equal((long)PROJEKT, Zahl("SELECT ID_Projekt FROM Tab_Stromverbraucher WHERE ID = ?", id));

            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_Stromverbrauchertyp WHERE ID_Stromverbraucher = ?", id) > 0,
                        "Das Typprofil ist nicht mitgekommen.");
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Stromverbrauchertyp " +
                                 "WHERE ID_Stromverbraucher = ? AND (ID_Projekt IS NULL OR ID_Projekt <> ?)",
                                 id, PROJEKT));
        }

        /// <summary>Dasselbe für das Brauchwasser — das Vorbild des Kopierwegs.</summary>
        [Fact]
        public void Das_Brauchwasser_kommt_samt_Typprofil_mit_Projektbezug_ins_Projekt()
        {
            using var _ = new Kulturvorrichtung();
            if (!_db.Vorhanden) return;

            int id = BrauchwasserStammCtrl.CopyFromStamm(BW_KATALOG, PROJEKT);
            Assert.True(id > 0, "CopyFromStamm hat keine Projektkopie geliefert (" + id + ").");

            Assert.Equal((long)PROJEKT, Zahl("SELECT ID_Projekt FROM Tab_Brauchwasser WHERE ID = ?", id));

            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_Brauchwassertyp WHERE ID_Brauchwasser = ?", id) > 0,
                        "Das Typprofil ist nicht mitgekommen.");
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Brauchwassertyp " +
                                 "WHERE ID_Brauchwasser = ? AND (ID_Projekt IS NULL OR ID_Projekt <> ?)",
                                 id, PROJEKT));
        }

        /// <summary>Dasselbe für die Prozesswärme — das zweite Vorbild.</summary>
        [Fact]
        public void Die_Prozesswaerme_kommt_samt_Typprofil_mit_Projektbezug_ins_Projekt()
        {
            using var _ = new Kulturvorrichtung();
            if (!_db.Vorhanden) return;

            int id = ProzesswaermeStammCtrl.CopyFromStamm(PW_KATALOG, PROJEKT);
            Assert.True(id > 0, "CopyFromStamm hat keine Projektkopie geliefert (" + id + ").");

            Assert.Equal((long)PROJEKT, Zahl("SELECT ID_Projekt FROM Tab_Prozesswaerme WHERE ID = ?", id));

            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_Prozesstyp WHERE ID_Prozesswaerme = ?", id) > 0,
                        "Das Typprofil ist nicht mitgekommen.");
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Prozesstyp " +
                                 "WHERE ID_Prozesswaerme = ? AND (ID_Projekt IS NULL OR ID_Projekt <> ?)",
                                 id, PROJEKT));
        }

        /// <summary>
        /// DIE WACHE ÜBER DEN KOPIERWEG: In den drei Typ-Projekttabellen steht nach
        /// dem Kopieren keine Zeile mit einer Projektnummer, die es nicht gibt — und
        /// <c>foreign_key_check</c> bleibt leer. Ohne den Projektbezug im Insert wäre
        /// die Kopie gar nicht erst zustande gekommen.
        /// </summary>
        [Fact]
        public void Nach_allen_drei_Kopien_meldet_foreign_key_check_nichts()
        {
            using var _ = new Kulturvorrichtung();
            if (!_db.Vorhanden) return;

            Assert.True(StromverbraucherStammCtrl.CopyFromStamm(SV_KATALOG, PROJEKT) > 0);
            Assert.True(BrauchwasserStammCtrl.CopyFromStamm(BW_KATALOG, PROJEKT) > 0);
            Assert.True(ProzesswaermeStammCtrl.CopyFromStamm(PW_KATALOG, PROJEKT) > 0);

            Assert.Empty(DataRepository.GetDataTable("PRAGMA foreign_key_check").Rows);
        }

        // =====================================================================
        //  Der Assistentenschritt bricht ab, statt eine Katalog-Id zu schreiben
        // =====================================================================

        /// <summary>
        /// Scheitert die Kopie, MELDET der Schritt benannt und schreibt NICHTS.
        /// Bis FK-1 blieb die KATALOG-Id stehen und wanderte in die Projektzuordnung —
        /// deren Fremdschlüssel zeigt auf die PROJEKTtabelle, nicht auf den Katalog.
        /// </summary>
        [Fact]
        public void Ohne_Katalogsatz_bricht_der_Stromverbraucher_Schritt_benannt_ab()
        {
            using var _ = new Kulturvorrichtung();
            if (!_db.Vorhanden) return;

            long vorher = Zahl("SELECT COUNT(*) FROM Z_Projekt_Stromverbraucher");
            var liste = new List<Z_ProjektStromverbraucherModel>
            {
                new Z_ProjektStromverbraucherModel
                {
                    m_szVerbraucher = OHNE_KATALOGSATZ,
                    m_ID_Stromverbraucher = KatalogId("Tab_Stromverbraucher_STAMM")
                }
            };

            string[] meldungen;
            using (DataRepository.EngineModus())
            {
                Assert.False(new WizardCtrl().Add_Projekt_Stromverbraucher(PROJEKT, liste));
                meldungen = DataRepository.StilleFehlerAbholen();
            }

            Assert.Single(meldungen);
            Assert.Contains("Stromverbraucher", meldungen[0], StringComparison.Ordinal);
            Assert.Contains(OHNE_KATALOGSATZ, meldungen[0], StringComparison.Ordinal);
            Assert.Equal(vorher, Zahl("SELECT COUNT(*) FROM Z_Projekt_Stromverbraucher"));
        }

        /// <summary>Dasselbe für die Prozesswärme.</summary>
        [Fact]
        public void Ohne_Katalogsatz_bricht_der_Prozesswaerme_Schritt_benannt_ab()
        {
            using var _ = new Kulturvorrichtung();
            if (!_db.Vorhanden) return;

            long vorher = Zahl("SELECT COUNT(*) FROM Z_Projekt_Prozesswaerme");
            var liste = new List<Z_ProjektProzesswaermeModel>
            {
                new Z_ProjektProzesswaermeModel
                {
                    szProzessname = OHNE_KATALOGSATZ,
                    ID_Prozesswaerme = KatalogId("Tab_Prozesswaerme_STAMM")
                }
            };

            string[] meldungen;
            using (DataRepository.EngineModus())
            {
                Assert.False(new WizardCtrl().Add_Projekt_Prozess(PROJEKT, liste));
                meldungen = DataRepository.StilleFehlerAbholen();
            }

            Assert.Single(meldungen);
            Assert.Contains("Prozesswaerme", meldungen[0], StringComparison.Ordinal);
            Assert.Equal(vorher, Zahl("SELECT COUNT(*) FROM Z_Projekt_Prozesswaerme"));
        }

        /// <summary>Dasselbe für das Brauchwasser.</summary>
        [Fact]
        public void Ohne_Katalogsatz_bricht_der_Brauchwasser_Schritt_benannt_ab()
        {
            using var _ = new Kulturvorrichtung();
            if (!_db.Vorhanden) return;

            long vorher = Zahl("SELECT COUNT(*) FROM Z_Projekt_Brauchwasser");
            var liste = new List<Z_ProjektBrauchwasserModel>
            {
                new Z_ProjektBrauchwasserModel
                {
                    szBezeichner = OHNE_KATALOGSATZ,
                    ID_Brauchwasser = KatalogId("Tab_Brauchwasser_STAMM")
                }
            };

            string[] meldungen;
            using (DataRepository.EngineModus())
            {
                Assert.False(new WizardCtrl().Add_Projekt_Brauchwasser(PROJEKT, liste));
                meldungen = DataRepository.StilleFehlerAbholen();
            }

            Assert.Single(meldungen);
            Assert.Contains("Brauchwasser", meldungen[0], StringComparison.Ordinal);
            Assert.Equal(vorher, Zahl("SELECT COUNT(*) FROM Z_Projekt_Brauchwasser"));
        }

        /// <summary>
        /// Mit Katalogsatz geht derselbe Schritt durch — und die Zuordnung zeigt auf
        /// die PROJEKTkopie, nicht auf den Katalogsatz.
        /// </summary>
        [Fact]
        public void Mit_Katalogsatz_zeigt_die_Zuordnung_auf_die_Projektkopie()
        {
            using var _ = new Kulturvorrichtung();
            if (!_db.Vorhanden) return;

            object roh = DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Stromverbraucher_STAMM WHERE Bezeichner = ?",
                new DbParam("p1", SV_KATALOG));
            Assert.True(roh != null && roh != DBNull.Value,
                        "Der Katalogsatz " + SV_KATALOG + " fehlt in der Testdatenbank.");
            int katalogId = Convert.ToInt32(roh, CultureInfo.InvariantCulture);

            var liste = new List<Z_ProjektStromverbraucherModel>
            {
                new Z_ProjektStromverbraucherModel
                {
                    m_szVerbraucher = SV_KATALOG,
                    m_ID_Stromverbraucher = katalogId
                }
            };

            Assert.True(new WizardCtrl().Add_Projekt_Stromverbraucher(PROJEKT, liste));

            long zugeordnet = Zahl("SELECT ID_Stromverbraucher FROM Z_Projekt_Stromverbraucher " +
                                   "WHERE ID_Projekt = ? ORDER BY ID DESC LIMIT 1", PROJEKT);
            Assert.NotEqual(katalogId, (int)zugeordnet);
            Assert.Equal((long)PROJEKT, Zahl("SELECT ID_Projekt FROM Tab_Stromverbraucher WHERE ID = ?",
                                             (int)zugeordnet));
            Assert.Empty(DataRepository.GetDataTable("PRAGMA foreign_key_check").Rows);
        }

        // =====================================================================
        //  Kleinkram
        // =====================================================================

        /// <summary>Irgendeine Id aus einem Katalog — die Id, die vor FK-1 durchschlug.</summary>
        private static int KatalogId(string tabelle)
        {
            object wert = DataRepository.ExecuteScalar("SELECT MIN(ID) FROM " + tabelle);
            return wert == null || wert == DBNull.Value
                ? 1
                : Convert.ToInt32(wert, CultureInfo.InvariantCulture);
        }

        /// <summary>Eine Zählung; −1, wenn sie nichts liefert.</summary>
        private static long Zahl(string sql, params int[] werte)
        {
            var p = new DbParam[werte.Length];
            for (int i = 0; i < werte.Length; i++)
                p[i] = new DbParam("p" + i.ToString(CultureInfo.InvariantCulture), werte[i]);

            object wert = DataRepository.ExecuteScalar(sql, p);
            return wert == null || wert == DBNull.Value
                ? -1
                : Convert.ToInt64(wert, CultureInfo.InvariantCulture);
        }
    }
}
