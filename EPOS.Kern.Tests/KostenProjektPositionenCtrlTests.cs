using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// W5‑B‑6 (Windows-Abnahme 08.09.2026): „Positionen ohne Anlagenzuordnung löschen" nimmt
    /// auch verwaiste PFLICHTPOSITIONEN weg — die Pflichtzeilen einer LEBENDEN Anlage bleiben
    /// geschützt. Projekt 1026 der Testdatenbank führt an seiner Wärmepumpe drei
    /// Pflichtpositionen (Betrieb, H3).
    /// </summary>
    [Collection("Testdatenbank")]
    public class KostenProjektPositionenCtrlTests
    {
        private const int PROJEKT = 1026;
        private const int WAERMEPUMPE = 1;   // Tab_KostenKomponente.ID

        private static int Anzahl(string zusatz)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_ProjektWerte AS w INNER JOIN Tab_Kostenfaktor AS k " +
                "ON w.StammID = k.StammID WHERE w.ProjektID = " + PROJEKT +
                " AND w.KomponentenID = " + WAERMEPUMPE + " " + zusatz);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }

        [Fact]
        public void Verwaiste_Pflichtpositionen_lassen_sich_als_ohne_Anlagenzuordnung_loeschen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Assert.True(Anzahl("AND w.IstPflicht = 1") > 0);

            // Der Anlagentausch: Die Anlagenzeile der Waermepumpe faellt weg, alle ihre
            // Positionen verwaisen - genau der Zustand des Anwenderprojekts 1026.
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET ID_Anlage = NULL WHERE ProjektID = ? AND KomponentenID = ?",
                new DbParam("@p", PROJEKT), new DbParam("@k", WAERMEPUMPE));
            int alle = Anzahl("");
            Assert.True(alle > 0);

            Assert.Equal(alle, KostenProjektPositionenCtrl.LoseLoeschen(PROJEKT, WAERMEPUMPE));
            Assert.Equal(0, Anzahl(""));
        }

        [Fact]
        public void Eine_Pflichtposition_einer_lebenden_Anlage_bleibt_geschuetzt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            object o = DataRepository.ExecuteScalar(
                "SELECT MIN(ID) FROM Tab_ProjektWerte WHERE ProjektID = " + PROJEKT +
                " AND KomponentenID = " + WAERMEPUMPE + " AND IstPflicht = 1 AND ID_Anlage IN " +
                "(SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = " + PROJEKT + ")");
            if (o == null || o == DBNull.Value) return;   // in dieser Datenbank keine lebende Pflichtzeile
            int id = Convert.ToInt32(o);

            Assert.False(KostenProjektPositionenCtrl.Loeschen(id));
            Assert.Equal(0, KostenProjektPositionenCtrl.LoseLoeschen(PROJEKT, WAERMEPUMPE));
            Assert.Equal(1, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ID = " + id)));
        }

        // =====================================================================
        //  Der Papierkorb nennt, was er wegnimmt (Auftrag Kostenbereich, Punkt 1)
        // =====================================================================

        /// <summary>Projekt 1044 „… - Schichtspeicher" fuehrt drei lose Positionen,
        /// je eine in drei verschiedenen Gewerken.</summary>
        private const int VARIANTE = 1044;
        private const int HEIZKESSEL = 2;
        private const int SOLARTHERMIE = 4;
        private const int PUFFERSPEICHER = 6;

        /// <summary>
        /// Die Zaehlung nennt Anzahl UND Summe — und zwar je Gewerk getrennt, so wie die
        /// gelbe Zeile der Kosten-Seite je Gewerk eine ist. Die Summe ist der ANGEZEIGTE
        /// Betrag, nicht der rohe EingegebenerWert; bei diesen drei Zeilen (Bemessung
        /// BETRAG) fallen beide zusammen.
        /// </summary>
        [Fact]
        public void Die_Zaehlung_der_losen_Positionen_nennt_Anzahl_und_Summe_je_Gewerk()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            KostenProjektPositionenCtrl.LoseBefund kessel =
                KostenProjektPositionenCtrl.LoseZaehlen(VARIANTE, HEIZKESSEL, 0);
            KostenProjektPositionenCtrl.LoseBefund solar =
                KostenProjektPositionenCtrl.LoseZaehlen(VARIANTE, SOLARTHERMIE, 0);
            KostenProjektPositionenCtrl.LoseBefund puffer =
                KostenProjektPositionenCtrl.LoseZaehlen(VARIANTE, PUFFERSPEICHER, 0);

            Assert.Equal(1, kessel.Anzahl);
            Assert.Equal(0.0, kessel.Summe, 6);
            Assert.Equal(1, solar.Anzahl);
            Assert.Equal(3775.00, solar.Summe, 2);
            Assert.Equal(1, puffer.Anzahl);
            Assert.Equal(3000.50, puffer.Summe, 2);
        }

        /// <summary>
        /// Gezaehlt wird GENAU die Menge, die <c>LoseLoeschen</c> anschliessend wegnimmt —
        /// sonst naennte die Rueckfrage eine andere Zahl, als hinterher verschwindet.
        /// </summary>
        [Fact]
        public void Die_Zaehlung_trifft_dieselbe_Menge_wie_das_Loeschen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            KostenProjektPositionenCtrl.LoseBefund vorher =
                KostenProjektPositionenCtrl.LoseZaehlen(VARIANTE, SOLARTHERMIE, 0);
            Assert.Equal(1, vorher.Anzahl);

            Assert.Equal(vorher.Anzahl,
                         KostenProjektPositionenCtrl.LoseLoeschen(VARIANTE, SOLARTHERMIE));

            // Danach ist nichts mehr zu zaehlen - und der Papierkorb bleibt ohne Wirkung.
            Assert.Equal(0, KostenProjektPositionenCtrl.LoseZaehlen(VARIANTE, SOLARTHERMIE, 0).Anzahl);
        }

        /// <summary>
        /// Der Kategoriefilter: Diese drei Zeilen sind Investitionen; unter „Betrieb"
        /// zaehlt der Papierkorb nichts. Ohne Kategorie (0) zaehlen BEIDE — das ist die
        /// Menge, die <c>LoseLoeschen</c> nimmt.
        /// </summary>
        [Fact]
        public void Die_Zaehlung_trennt_die_beiden_Kategorien()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(1, KostenProjektPositionenCtrl.LoseZaehlen(
                VARIANTE, PUFFERSPEICHER, DbWerte.KOSTEN_KATEGORIE_INVESTITION).Anzahl);
            Assert.Equal(0, KostenProjektPositionenCtrl.LoseZaehlen(
                VARIANTE, PUFFERSPEICHER, DbWerte.KOSTEN_KATEGORIE_BETRIEB).Anzahl);
            Assert.Equal(1, KostenProjektPositionenCtrl.LoseZaehlen(
                VARIANTE, PUFFERSPEICHER, 0).Anzahl);
        }

        /// <summary>Die Oberflaeche kennt das Gewerk als NAMEN — die Id dazu holt der Kern.</summary>
        [Fact]
        public void Die_Komponenten_Id_kommt_zum_Gewerkenamen_aus_dem_Kern()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(PUFFERSPEICHER,
                         KostenProjektPositionenCtrl.KomponentenId(DbWerte.KOSTEN_KOMPONENTE_PUFFERSPEICHER));
            Assert.Equal(0, KostenProjektPositionenCtrl.KomponentenId("gibt es nicht"));
            Assert.Equal(0, KostenProjektPositionenCtrl.KomponentenId(null));
        }

        // =====================================================================
        //  Der Geraeteanker loser Positionen (Auftrag Kostenbereich, Punkt 2)
        // =====================================================================

        /// <summary>Zahl der Positionen eines Projekts, deren Geraeteanker steht, obwohl
        /// die Position keine gueltige Anlage des Projekts (mehr) hat — ein toter
        /// Verweis.</summary>
        private static int ToteAnker(int projekt)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = " + projekt +
                " AND ID_AnlageGeraet IS NOT NULL AND (ID_Anlage IS NULL OR ID_Anlage NOT IN " +
                "(SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = " + projekt + "))");
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }

        /// <summary>
        /// Eine Kopie erbt keine toten Geraeteanker.
        ///
        /// <para>Der Duplizierer versetzt <c>ID_Anlage</c> ueber die FK-Landkarte, den
        /// komponentenabhaengigen Geraeteanker zieht <c>AnkerNachziehen</c> nach. Dessen
        /// EXISTS-Klausel traf aber nur Zeilen MIT gueltiger Anlage — eine schon in der
        /// Quelle lose Position behielt den Anker des QUELLprojekts, und der zeigt in der
        /// Kopie auf ein Geraet, das es dort nicht gibt.</para>
        ///
        /// <para>Die GEGENPROBE steht in derselben Datenbank: Die Quelle 1044 fuehrt drei
        /// solcher toten Anker (Bestand, von einer frueheren Kopie geerbt) — sie bleiben
        /// unangetastet, denn <c>AnkerNachziehen</c> laeuft nur auf der KOPIE.</para>
        /// </summary>
        [Fact]
        public void Eine_Kopie_erbt_keinen_Geraeteanker_ohne_Anlage()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(3, ToteAnker(VARIANTE));          // die Gegenprobe: so sah es aus

            string quelle = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Projektname FROM Tab_Projekt WHERE ID = " + VARIANTE));
            Assert.False(string.IsNullOrEmpty(quelle));

            int kopie = new ProjektDuplizierenCtrl().Duplizieren(quelle, "Ankerprobe Kopie");
            Assert.True(kopie > 0);

            // In der Kopie zeigt kein Anker mehr ins Leere.
            Assert.Equal(0, ToteAnker(kopie));

            // Die losen Positionen sind trotzdem da - geloest, nicht geloescht.
            Assert.Equal(3, KostenProjektPositionenCtrl.LoseZaehlen(kopie, HEIZKESSEL, 0).Anzahl +
                            KostenProjektPositionenCtrl.LoseZaehlen(kopie, SOLARTHERMIE, 0).Anzahl +
                            KostenProjektPositionenCtrl.LoseZaehlen(kopie, PUFFERSPEICHER, 0).Anzahl);

            // Und die GUELTIG zugeordneten Positionen tragen weiterhin einen Anker - auf
            // ein Geraet der KOPIE, nicht der Quelle.
            object mitAnker = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = " + kopie +
                " AND ID_AnlageGeraet IS NOT NULL AND ID_Anlage IN " +
                "(SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = " + kopie + ")");
            Assert.True(Convert.ToInt32(mitAnker) > 0);

            // Die Quelle ist unberuehrt geblieben.
            Assert.Equal(3, ToteAnker(VARIANTE));
        }
    }
}
