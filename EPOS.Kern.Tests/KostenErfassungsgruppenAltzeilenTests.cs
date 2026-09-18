using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Nullzeilen der drei Erfassungsgruppen fallen weg</b> — Schemaschritt 90
    /// (DML-Teil), der Nachweis zu <see cref="KostenErfassungsgruppenAltzeilen"/>
    /// (Anwenderentscheid K-WZ-1 (a)).
    ///
    /// <para><b>Der Befund.</b> In <c>Tab_ProjektWerte</c> standen
    /// Hauptkomponentenzeilen der frueheren Kostenmaske zu den drei nicht
    /// anlagenfaehigen Erfassungsgruppen — Gruppe „Allgemein", Wert 0,00. Die Kostenseite
    /// zeigte sie unter „Anlagenkomponenten" ohne Kennzeichnung und ohne Papierkorb; kein
    /// heutiger Rechenweg legt sie an.</para>
    ///
    /// <para><b>Die Grenze des Schrittes ist der eigentliche Prueffall:</b> Traegt
    /// IRGENDEINE Position derselben Gruppe einen Wert, bleibt die Gruppe vollstaendig
    /// stehen — samt ihrer Nullzeilen. Dafuer steht die Gegenprobe unten.</para>
    ///
    /// <para><b>Diese Klasse SCHREIBT</b> und braucht je Fall eine unberuehrte
    /// Arbeitskopie. Fehlt die Testdatenbank, schweigen die Faelle.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KostenErfassungsgruppenAltzeilenTests
    {
        /// <summary>Die elf Nullzeilen, die der Schritt in der Testdatenbank vorfand —
        /// Projekte 1018, 1019, 1031 und 1032, Kategorien 1 und 2.</summary>
        private const int ZEILEN_IN_DER_TESTDATENBANK = 11;

        /// <summary>Ein Projekt, das eine solche Zeile trug.</summary>
        private const int PROJEKT = 1019;

        /// <summary>„Wärmezentrale" im Komponentenkatalog.</summary>
        private const int KOMPONENTE_WAERMEZENTRALE = 8;

        /// <summary><c>Tab_Kostenfaktor.StammID</c> der Hauptkomponente
        /// „Wärmezentrale".</summary>
        private const int STAMM_WAERMEZENTRALE = 95;

        /// <summary>
        /// <b>Der ganze Schritt in EINEM Fall.</b> Der Fall stellt die elf Zeilen selbst
        /// wieder her — die Arbeitskopie steht bereits auf dem Zielstand — und faehrt dann
        /// DIESELBE Anweisung, die Migration und Werkzeug fahren: 11 → 0, und ein zweiter
        /// Lauf fasst nichts an.
        /// </summary>
        [Fact]
        public void Der_Schritt_entfernt_die_Nullzeilen_und_wiederholt_sich_folgenlos()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(0, KostenErfassungsgruppenAltzeilen.Offen());   // Zielstand
            NullzeilenWiederherstellen();

            Assert.Equal(ZEILEN_IN_DER_TESTDATENBANK, KostenErfassungsgruppenAltzeilen.Offen());
            long vorher = Zahl("SELECT COUNT(*) FROM " + KostenErfassungsgruppenAltzeilen.TABELLE);

            foreach (KeyValuePair<string, string> a in KostenErfassungsgruppenAltzeilen.Anweisungen)
                DataRepository.ExecuteNonQuery(a.Value);

            Assert.Equal(0, KostenErfassungsgruppenAltzeilen.Offen());
            Assert.Equal(vorher - ZEILEN_IN_DER_TESTDATENBANK,
                         Zahl("SELECT COUNT(*) FROM " + KostenErfassungsgruppenAltzeilen.TABELLE));

            // Wiederholbar: Der zweite Lauf gibt keine Anweisung mehr heraus.
            Assert.Empty(KostenErfassungsgruppenAltzeilen.Anweisungen);
        }

        /// <summary>
        /// <b>Ergebnisneutral, und zwar messbar:</b> Jede entfernte Zeile traegt 0,00 in
        /// jedem Wertfeld — die Summe je Projekt und Kategorie ist vor und nach dem
        /// Schritt dieselbe.
        /// </summary>
        [Fact]
        public void Die_Summe_je_Projekt_und_Kategorie_bleibt_gleich()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            NullzeilenWiederherstellen();

            double vorher = Summe(PROJEKT);
            foreach (KeyValuePair<string, string> a in KostenErfassungsgruppenAltzeilen.Anweisungen)
                DataRepository.ExecuteNonQuery(a.Value);
            double nachher = Summe(PROJEKT);

            Assert.Equal(vorher, nachher, 9);
        }

        /// <summary>
        /// <b>Die Gegenprobe — die Grenze des Schrittes.</b> Bekommt EINE Position der
        /// Gruppe einen Wert, bleibt die Gruppe vollstaendig stehen: die Zeile mit Wert
        /// UND die Nullzeile daneben. Eine Gruppe, in der der Anwender etwas erfasst hat,
        /// wird nicht angetastet.
        /// </summary>
        [Fact]
        public void Eine_Gruppe_mit_einer_Position_mit_Wert_bleibt_vollstaendig_stehen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            NullzeilenWiederherstellen();
            int offenVorher = KostenErfassungsgruppenAltzeilen.Offen();
            Assert.True(offenVorher > 0);

            // Eine zweite Position in DERSELBEN Gruppe (Projekt, Kategorie, Komponente),
            // diesmal mit einem Wert - und keine Hauptkomponente.
            DataRepository.ExecuteNonQuery(
                "INSERT INTO " + KostenErfassungsgruppenAltzeilen.TABELLE +
                " (ProjektID, StammID, KomponentenID, KategorieID, EingegebenerWert, " +
                "Worstcase, Bestcase, Gruppe) VALUES (?, ?, ?, 1, 1234.0, 0, 0, 'Allgemein')",
                new DbParam("@p", PROJEKT), new DbParam("@s", 0),
                new DbParam("@k", KOMPONENTE_WAERMEZENTRALE));

            // Die Nullzeile dieser Gruppe zaehlt jetzt nicht mehr mit.
            Assert.Equal(offenVorher - 1, KostenErfassungsgruppenAltzeilen.Offen());

            foreach (KeyValuePair<string, string> a in KostenErfassungsgruppenAltzeilen.Anweisungen)
                DataRepository.ExecuteNonQuery(a.Value);

            // Die Gruppe steht vollstaendig: beide Zeilen.
            Assert.Equal(2, Zahl(
                "SELECT COUNT(*) FROM " + KostenErfassungsgruppenAltzeilen.TABELLE +
                " WHERE ProjektID = " + PROJEKT + " AND KategorieID = 1 AND KomponentenID = " +
                KOMPONENTE_WAERMEZENTRALE));
        }

        /// <summary>Die drei Namen kommen aus <see cref="DbWerte"/> — nicht aus einem
        /// zweiten, abgeschriebenen Satz.</summary>
        [Fact]
        public void Der_Schritt_kennt_genau_die_drei_Erfassungsgruppen()
        {
            var gruppen = new List<string>(KostenErfassungsgruppenAltzeilen.Erfassungsgruppen);
            Assert.Equal(3, gruppen.Count);
            Assert.Contains(DbWerte.KOSTEN_KOMPONENTE_WAERMEZENTRALE, gruppen);
            Assert.Contains(DbWerte.KOSTEN_KOMPONENTE_BAULICHE_ANLAGEN, gruppen);
            Assert.Contains(DbWerte.KOSTEN_KOMPONENTE_STROMEINSPEISUNG, gruppen);

            // Keine von ihnen ist anlagenfaehig - genau das ist der Grund des Befundes.
            foreach (string g in gruppen)
            {
                Assert.False(KostenVorlagenCtrl.IstWaehlbar(g), g);
                Assert.True(KostenVorlagenCtrl.IstErfassungsgruppe(g), g);
            }

            // Umgekehrt ist keine anlagenfaehige Komponente eine Erfassungsgruppe -
            // die Kostenseite kennzeichnet sie deshalb nicht als solche.
            foreach (string k in KostenVorlagenCtrl.WaehlbareKomponenten)
                Assert.False(KostenVorlagenCtrl.IstErfassungsgruppe(k), k);

            // Ein leerer Name ist keine Gruppe.
            Assert.False(KostenVorlagenCtrl.IstErfassungsgruppe(""));
            Assert.False(KostenVorlagenCtrl.IstErfassungsgruppe(null));
        }

        // =================================================================
        //  Werkzeug
        // =================================================================

        /// <summary>
        /// Legt die elf Zeilen wieder an, die der Schritt in der Testdatenbank entfernt
        /// hat — je Projekt und Kategorie eine Hauptkomponentenzeile ohne Wert, so wie
        /// sie die fruehere Kostenmaske hinterlassen hat.
        /// </summary>
        private static void NullzeilenWiederherstellen()
        {
            Anlegen(1018, 1, KOMPONENTE_WAERMEZENTRALE, STAMM_WAERMEZENTRALE);
            Anlegen(1019, 1, KOMPONENTE_WAERMEZENTRALE, STAMM_WAERMEZENTRALE);
            Anlegen(1019, 1, 9, 99);
            Anlegen(1019, 1, 10, 104);
            Anlegen(1019, 2, KOMPONENTE_WAERMEZENTRALE, STAMM_WAERMEZENTRALE);
            Anlegen(1019, 2, 9, 99);
            Anlegen(1019, 2, 10, 104);
            Anlegen(1031, 1, KOMPONENTE_WAERMEZENTRALE, STAMM_WAERMEZENTRALE);
            Anlegen(1032, 1, KOMPONENTE_WAERMEZENTRALE, STAMM_WAERMEZENTRALE);
            Anlegen(1032, 1, 9, 99);
            Anlegen(1032, 1, 10, 104);
        }

        private static void Anlegen(int projekt, int kategorie, int komponente, int stamm)
        {
            DataRepository.ExecuteNonQuery(
                "INSERT INTO " + KostenErfassungsgruppenAltzeilen.TABELLE +
                " (ProjektID, StammID, KomponentenID, KategorieID, EingegebenerWert, " +
                "Worstcase, Bestcase, Gruppe) VALUES (?, ?, ?, ?, 0, 0, 0, 'Allgemein')",
                new DbParam("@p", projekt), new DbParam("@s", stamm),
                new DbParam("@k", komponente), new DbParam("@kat", kategorie));
        }

        private static double Summe(int projekt)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT IFNULL(SUM(IFNULL(EingegebenerWert, 0)), 0) FROM " +
                KostenErfassungsgruppenAltzeilen.TABELLE + " WHERE ProjektID = ?",
                new DbParam("@p", projekt));
            return o == null || o == DBNull.Value ? 0 : Convert.ToDouble(o);
        }

        private static long Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o);
        }
    }
}
