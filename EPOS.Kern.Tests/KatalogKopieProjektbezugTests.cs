using System;
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
        //  Kleinkram
        // =====================================================================

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
