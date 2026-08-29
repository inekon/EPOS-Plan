using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // Zugriff auf Z_AnlagePufferVerbund - die ZUSAETZLICHEN Mitglieder eines
    // Pufferverbunds je Waermeerzeuger-Anlage (Paket Parallelverbund, Entscheidung
    // des Anwenders 17.08.2026; angelegt von SchemaMigration Schritt 14).
    //
    // FACHLICH: Ein Verbund ist EIN gemeinsamer Waermevorrat aus mehreren parallel
    // verschalteten Behaeltern - Kapazitaeten addiert, ein Fuellstand, eine
    // Schaltschwelle. Der LEITSPEICHER steht unveraendert in
    // Tab_Energieanlagen.WS_ID_Puffer und ist auch die ID, unter der das
    // Rechenobjekt und die Ergebniszeile laufen; diese Tabelle traegt nur die
    // Mitglieder ab dem zweiten Behaelter. Eine leere Tabelle bedeutet deshalb
    // exakt das Verhalten vor dem Paket.
    //
    // DIALOGFREI ueber StilleDb (Konzept 13.4), nicht ueber DataRepository: Gelesen
    // wird dieselbe Tabelle aus dem ENGINE-Pfad heraus
    // (SimulationControl.SpeicherRegistryAufbauen) und aus der Referenzlauf-Suite,
    // und dort darf keine MessageBox stehen. Geschrieben wird nur aus der
    // Oberflaeche - derselbe Zugriffsweg schadet dort nicht, und eine zweite
    // SQL-Wahrheit auf derselben Tabelle waere schlechter als ein stiller Fehler,
    // den der Aufrufer ohnehin am Rueckgabewert erkennt.
    //
    // IDs ueber MAX(ID)+1 (Hausmuster ADR-001), kein RecordSet.
    // ---------------------------------------------------------------------------
    public static class AnlagePufferVerbundCtrl
    {
        public const string TABLE = "Z_AnlagePufferVerbund";

        // =====================================================================
        // Konfliktbefunde - DATEN, kein Anzeigetext
        // =====================================================================

        /// <summary>Grundcode „das Mitglied ist Hauptsenke (Leitspeicher) einer Anlage".</summary>
        public const string GRUND_HAUPTSENKE = "Hauptsenke";

        /// <summary>Grundcode „das Mitglied ist Zweitsenke einer Anlage".</summary>
        public const string GRUND_ZWEITSENKE = "Zweitsenke";

        /// <summary>Grundcode „das Mitglied gehoert schon zu einem Verbund mit anderem Leitspeicher".</summary>
        public const string GRUND_ANDERER_VERBUND = "AndererVerbund";

        /// <summary>Grundcode „der gewaehlte Leitspeicher ist selbst Mitglied eines fremden Verbunds".</summary>
        public const string GRUND_LEIT_IST_MITGLIED = "LeitIstMitglied";

        /// <summary>Grundcode „das Mitglied ist die Waermequelle derselben Anlage" (Kurzschluss).</summary>
        public const string GRUND_QUELLE = "Quelle";

        /// <summary>Grundcode „das Mitglied gehoert nicht zum Projekt oder hat die falsche Verwendung".</summary>
        public const string GRUND_PASST_NICHT = "PasstNicht";

        /// <summary>
        /// Grundcode „der gewaehlte Leitspeicher fuehrt eine SCHICHTUNG (N &gt; 1)" —
        /// Kriterium W6 des Warnkriterienkatalogs, HART (Konzept 6.2/6.3, Paket P1).
        ///
        /// Verbund und Schichtung schliessen sich je Rechenspeicher aus (Entscheidung
        /// F8): Ein Verbund ist EIN Vorrat aus mehreren Behaeltern, sein <c>Q_max</c> ist
        /// die AUFSUMMIERTE Kapazitaet aller Mitglieder. Eine Schichtebene, die aus dem
        /// Volumen des Leitspeichers abgeleitet waere, beschriebe damit einen Behaelter,
        /// den es so nicht gibt - die Schicht-Invariante aus Konzept 7.3 waere verletzt.
        /// Ein Verbund-Leitspeicher rechnet deshalb stets mit N = 1.
        ///
        /// Dies ist die GEGENRICHTUNG des Guards: Der Speicherdialog weist N &gt; 1 an
        /// einem bereits bestehenden Leitspeicher ab, diese Pruefung weist einen Verbund
        /// ab, dessen Leitspeicher schon geschichtet ist.
        /// </summary>
        public const string GRUND_LEIT_GESCHICHTET = "LeitGeschichtet";

        /// <summary>
        /// Ein einzelner Konflikt aus <see cref="KonfliktPruefen"/>.
        ///
        /// Traegt ausschliesslich DATEN (IDs und einen Grundcode). Den Anzeigetext baut
        /// <c>WaermesenkeClass.Pruefen</c> daraus - dort steht die Fachregel, und dort
        /// liegen die Ressourcentexte (Drei-Schichten-Regel).
        /// </summary>
        public sealed class Konfliktbefund
        {
            /// <summary>Der Puffer, an dem der Konflikt haengt.</summary>
            public int ID_Puffer;

            /// <summary>Einer der GRUND_*-Codes dieser Klasse.</summary>
            public string Grund = "";

            /// <summary>Die ANDERE Anlage, die den Puffer bereits belegt; 0, wenn es keine gibt.</summary>
            public int ID_AndereAnlage;

            /// <summary>Der Leitspeicher des fremden Verbunds; 0, wenn es keinen gibt.</summary>
            public int ID_FremderLeit;
        }

        // =====================================================================
        // Vorsorge
        // =====================================================================

        /// <summary>
        /// Legt Tabelle und Index an, falls Migrationsschritt 14 noch nicht gelaufen ist -
        /// dieselbe tolerante Rueckfallebene wie <c>KostenprofilCtrl.StelleTabelleSicher</c>.
        ///
        /// Die BEZIEHUNGEN kommen hier ausdruecklich NICHT mit: Sie sind schon in der
        /// Migration weich (siehe <c>SchemaMigration.Schritt_14_Parallelverbund</c>), und
        /// eine Rueckfallebene, die stillschweigend Beziehungen anlegt, waere die zweite
        /// Wahrheit ueber das Schema. Die Ablage funktioniert ohne sie.
        /// </summary>
        public static void StelleTabelleSicher()
        {
            if (StilleDb.Scalar("SELECT COUNT(*) FROM [" + TABLE + "]") != null) return;

            // Kein Ergebnis heisst hier "Tabelle fehlt" (StilleDb schluckt den Fehler und
            // liefert null). Ein leerer Tabelleninhalt liefert dagegen 0, nicht null -
            // COUNT(*) hat immer eine Zeile.
            StilleDb.NonQuery(SchemaMigration.SQL_CREATE_ANLAGEPUFFERVERBUND);
            StilleDb.NonQuery(SchemaMigration.SQL_INDEX_ANLAGEPUFFERVERBUND);
        }

        // =====================================================================
        // Lesen
        // =====================================================================

        /// <summary>
        /// Die zusaetzlichen Verbundmitglieder EINER Anlage, in ID-Reihenfolge; nie
        /// <c>null</c>. Der Leitspeicher ist NICHT enthalten (er steht in
        /// <c>WS_ID_Puffer</c>).
        ///
        /// Eine fehlende Tabelle liefert eine leere Liste - genau wie ein Projekt ohne
        /// Verbund. Der Aufrufer braucht deshalb keine Schemapruefung.
        /// </summary>
        public static List<int> MitgliederLesen(int idAnlage)
        {
            List<int> liste = new List<int>();
            if (idAnlage <= 0) return liste;

            DataTable dt = StilleDb.Tabelle(
                "SELECT ID_Puffer FROM [" + TABLE + "] WHERE ID_Anlage = ? ORDER BY ID",
                StilleDb.Par("@anlage", OleDbType.Integer, idAnlage));
            if (dt == null) return liste;

            foreach (DataRow r in dt.Rows)
            {
                int id = StilleDb.Zahl(StilleDb.Feld(r, "ID_Puffer"));
                if (id > 0 && !liste.Contains(id)) liste.Add(id);
            }

            return liste;
        }

        // ENTFALLEN MIT PAKET L (Aufraeumen, A1-O3): ProjektHatVerbund - die Frage „hat
        // dieses Projekt ueberhaupt einen Verbund?". Sie hatte genau zwei Aufrufer, und
        // beide sind mit Paket A1 gefallen: die Weiche des Rechenwegs
        // (SimulationControl._verbundErzwingtSpeicherstufe) und die Kaskaden-Automatik
        // (KonfigurationCtrl.KaskadeNotwendig). Seit A1 rechnet JEDER Lauf ueber die
        // Speicherstufe - ein Verbund erzwingt nichts mehr, weil es nichts mehr zu
        // erzwingen gibt. Wer die Frage je wieder braucht: eine COUNT(*)-Abfrage ueber
        // TABLE mit INNER JOIN auf Tab_Energieanlagen (die Anlagen tragen den
        // Projektbezug, die Zuordnungszeile nicht - Invariante S-1).

        /// <summary>
        /// Die Verbuende EINES PROJEKTS in der Form, die die Speicher-Registry braucht:
        /// <c>Leitspeicher-ID -&gt; Liste der zusaetzlichen Mitglieder</c>.
        ///
        /// Der Leitspeicher kommt aus <c>Tab_Energieanlagen.WS_ID_Puffer</c> DER ANLAGE,
        /// zu der die Verbundzeile gehoert - deshalb der Verbund ueber beide Tabellen in
        /// EINER Abfrage. Zeilen einer Anlage ohne Leitspeicher (Senke auf Heizkreis
        /// gestellt, Verbundzeilen aber noch nicht aufgeraeumt) haben keinen Vorrat, dem
        /// sie zugerechnet werden koennten, und entfallen still.
        ///
        /// MEHRERE ERZEUGER AM SELBEN VERBUND sind ausdruecklich erlaubt (Konfliktregel:
        /// „derselbe Verbund darf von mehreren Erzeugern geladen werden"). Nennen sie
        /// UNTERSCHIEDLICHE Mitgliedermengen, wird die VEREINIGUNG gebildet: Ein Behaelter
        /// ist hydraulisch entweder Teil des Vorrats oder nicht - eine erzeugerabhaengige
        /// Kapazitaet desselben Speichers gibt es physikalisch nicht. Der Aufrufer erfaehrt
        /// das ueber <paramref name="abweichendeZuschnitte"/> und meldet es ins
        /// Lauf-Protokoll.
        /// </summary>
        /// <param name="abweichendeZuschnitte">
        /// Leitspeicher-IDs, bei denen zwei Anlagen unterschiedliche Mitgliedermengen
        /// nennen. Nie <c>null</c>.
        /// </param>
        public static Dictionary<int, List<int>> VerbuendeDesProjekts(
            int idProjekt, out List<int> abweichendeZuschnitte)
        {
            Dictionary<int, List<int>> verbuende = new Dictionary<int, List<int>>();
            abweichendeZuschnitte = new List<int>();
            if (idProjekt <= 0) return verbuende;

            DataTable dt = StilleDb.Tabelle(
                "SELECT a.ID AS ID_Anlage, a.WS_ID_Puffer AS ID_Leit, v.ID_Puffer AS ID_Mitglied " +
                "FROM [" + TABLE + "] v INNER JOIN Tab_Energieanlagen a ON v.ID_Anlage = a.ID " +
                "WHERE a.ID_Projekt = ? ORDER BY a.ID, v.ID",
                StilleDb.Par("@proj", OleDbType.Integer, idProjekt));
            if (dt == null) return verbuende;

            // Je Anlage die genannte Menge, damit die Abweichung erkennbar bleibt.
            Dictionary<int, List<int>> jeAnlage = new Dictionary<int, List<int>>();
            Dictionary<int, int> leitJeAnlage = new Dictionary<int, int>();

            foreach (DataRow r in dt.Rows)
            {
                int idAnlage = StilleDb.Zahl(StilleDb.Feld(r, "ID_Anlage"));
                int idLeit = StilleDb.Zahl(StilleDb.Feld(r, "ID_Leit"));
                int idMitglied = StilleDb.Zahl(StilleDb.Feld(r, "ID_Mitglied"));

                if (idLeit <= 0 || idMitglied <= 0) continue;
                if (idMitglied == idLeit) continue;      // ein Speicher ist nicht sein eigenes Mitglied

                if (!jeAnlage.ContainsKey(idAnlage)) jeAnlage[idAnlage] = new List<int>();
                if (!jeAnlage[idAnlage].Contains(idMitglied)) jeAnlage[idAnlage].Add(idMitglied);
                leitJeAnlage[idAnlage] = idLeit;
            }

            foreach (KeyValuePair<int, List<int>> paar in jeAnlage)
            {
                int idLeit = leitJeAnlage[paar.Key];

                if (!verbuende.ContainsKey(idLeit))
                {
                    verbuende[idLeit] = new List<int>(paar.Value);
                    continue;
                }

                List<int> bestand = verbuende[idLeit];
                bool gleich = bestand.Count == paar.Value.Count;
                foreach (int m in paar.Value) if (!bestand.Contains(m)) gleich = false;

                if (!gleich && !abweichendeZuschnitte.Contains(idLeit))
                    abweichendeZuschnitte.Add(idLeit);

                foreach (int m in paar.Value) if (!bestand.Contains(m)) bestand.Add(m);
            }

            return verbuende;
        }

        /// <summary>
        /// Alle Puffer, die im Projekt als Verbund-MITGLIED gefuehrt werden (ohne die
        /// Leitspeicher). Das Sicherheitsnetz der Engine: Ein solcher Puffer darf niemals
        /// zusaetzlich als eigenstaendiges Rechenobjekt in den Rechenpfad, sonst zaehlte
        /// seine Kapazitaet doppelt.
        /// </summary>
        public static List<int> MitgliederDesProjekts(int idProjekt)
        {
            List<int> liste = new List<int>();
            if (idProjekt <= 0) return liste;

            DataTable dt = StilleDb.Tabelle(
                "SELECT v.ID_Puffer FROM [" + TABLE + "] v " +
                "INNER JOIN Tab_Energieanlagen a ON v.ID_Anlage = a.ID " +
                "WHERE a.ID_Projekt = ?",
                StilleDb.Par("@proj", OleDbType.Integer, idProjekt));
            if (dt == null) return liste;

            foreach (DataRow r in dt.Rows)
            {
                int id = StilleDb.Zahl(StilleDb.Feld(r, "ID_Puffer"));
                if (id > 0 && !liste.Contains(id)) liste.Add(id);
            }

            return liste;
        }

        /// <summary>
        /// Alle Puffer, die im Projekt als WAERMEQUELLE einer Anlage gefuehrt werden
        /// (<c>WQ_ID_Puffer</c>); nie <c>null</c>.
        ///
        /// <b>Warum der Verbund das braucht.</b> Ein Quellspeicher rechnet in der Engine
        /// auf einem EIGENEN Weg: Seine nutzbare Kapazitaet folgt nicht dem Temperaturpaar
        /// der Speicherzeile, sondern der Spreizung <c>WQ_Spreizung</c> der ANLAGE
        /// (<c>SimulationControl.QuellspeicherUebernehmen</c>). Er ist damit bereits ein
        /// vollwertiges Rechenobjekt - wuerde derselbe Behaelter zusaetzlich in den
        /// Senkenvorrat eines Verbunds aufaddiert, stuende seine Kapazitaet zweimal im
        /// Lauf: einmal als Quelle, einmal als Teil des Vorrats. Fachlich ist das ein
        /// Kurzschluss - ein Behaelter kann nicht gleichzeitig die Waerme liefern UND den
        /// Vorrat bilden, in den sie geladen wird.
        ///
        /// PROJEKTWEIT, nicht je Anlage: <c>WaermesenkeClass.QuellPufferDerAnlage</c>
        /// beantwortet die Frage fuer EINE Anlage (Pruefpunkt 4 in
        /// <c>WaermesenkeClass.Pruefen</c>). Hier geht es um jede Anlage des Projekts -
        /// auch die Quelle einer ANDEREN Waermepumpe darf nicht in einen fremden Verbund
        /// wandern. Gefunden wurde die Luecke im Wirkungsnachweis des Pakets: In Projekt
        /// 1021 war der zweite Heizungspuffer zugleich Quellspeicher der zweiten
        /// Waermepumpe, und seine Kapazitaet erschien sowohl im Verbund als auch als
        /// eigenes Quellobjekt.
        /// </summary>
        public static List<int> QuellPufferDesProjekts(int idProjekt)
        {
            List<int> liste = new List<int>();
            if (idProjekt <= 0) return liste;

            DataTable dt = StilleDb.Tabelle(
                "SELECT WQ_ID_Puffer FROM Tab_Energieanlagen " +
                "WHERE ID_Projekt = ? AND WQ_ID_Puffer IS NOT NULL",
                StilleDb.Par("@proj", OleDbType.Integer, idProjekt));
            if (dt == null) return liste;

            foreach (DataRow r in dt.Rows)
            {
                int id = StilleDb.Zahl(StilleDb.Feld(r, "WQ_ID_Puffer"));
                if (id > 0 && !liste.Contains(id)) liste.Add(id);
            }

            return liste;
        }

        /// <summary>
        /// Der LEITSPEICHER des Verbunds, in dem dieser Puffer Mitglied ist; 0, wenn er in
        /// keinem Verbund steht. Grundlage der Anzeige „im Verbund mit …" an der
        /// Speicherkarte und im Pufferdialog.
        ///
        /// Bei mehreren Anlagen mit demselben Verbund liefert die Abfrage denselben Leit
        /// mehrfach - genommen wird der erste Treffer. Ein Mitglied mit ZWEI verschiedenen
        /// Leitspeichern ist ein Konflikt, den <see cref="KonfliktPruefen"/> beim
        /// Speichern verhindert und den die Engine meldet.
        /// </summary>
        public static int LeitspeicherFuerMitglied(int idPuffer)
        {
            if (idPuffer <= 0) return 0;

            return StilleDb.Zahl(StilleDb.Scalar(
                "SELECT MIN(a.WS_ID_Puffer) FROM [" + TABLE + "] v " +
                "INNER JOIN Tab_Energieanlagen a ON v.ID_Anlage = a.ID " +
                "WHERE v.ID_Puffer = ? AND a.WS_ID_Puffer IS NOT NULL",
                StilleDb.Par("@id", OleDbType.Integer, idPuffer)));
        }

        /// <summary>
        /// true, wenn dieser Puffer der LEITSPEICHER eines Parallelverbunds ist — also
        /// mindestens eine Anlage ihn als Hauptsenke fuehrt UND fuer diese Anlage
        /// zusaetzliche Verbundmitglieder eingetragen sind (PAKET P1, Kriterium W6).
        ///
        /// <para>Das Gegenstueck zu <see cref="LeitspeicherFuerMitglied"/>: Dort ist der
        /// Puffer das MITGLIED und gesucht wird sein Leitspeicher, hier ist er selbst der
        /// Leitspeicher. Der Guard des Speicherdialogs braucht genau diese Richtung.</para>
        ///
        /// <para>Die Bedingung <c>v.ID_Puffer &lt;&gt; a.WS_ID_Puffer</c> haelt dieselbe
        /// Regel wie <see cref="VerbuendeDesProjekts"/> ein: Ein Speicher ist nicht sein
        /// eigenes Mitglied, und eine solche Altzeile macht aus ihm keinen Verbund.</para>
        ///
        /// <para>Still ueber <see cref="StilleDb"/>; eine fehlende Tabelle bedeutet
        /// „kein Verbund" und damit <c>false</c>.</para>
        /// </summary>
        public static bool IstLeitspeicher(int idPuffer)
        {
            if (idPuffer <= 0) return false;

            return StilleDb.Zahl(StilleDb.Scalar(
                "SELECT COUNT(*) FROM [" + TABLE + "] v " +
                "INNER JOIN Tab_Energieanlagen a ON v.ID_Anlage = a.ID " +
                "WHERE a.WS_ID_Puffer = ? AND v.ID_Puffer <> a.WS_ID_Puffer",
                StilleDb.Par("@id", OleDbType.Integer, idPuffer))) > 0;
        }

        /// <summary>
        /// Anlagen, die diesen Puffer als Verbundmitglied fuehren - je Treffer
        /// <c>Anlagen-ID</c> und <c>Bezeichner</c>. Grundlage des Loeschschutzes in
        /// <c>PufferSpCtrl.ReferenzenAufPuffer</c>.
        /// </summary>
        public static List<KeyValuePair<int, string>> MitgliedschaftenAufPuffer(int idPuffer)
        {
            List<KeyValuePair<int, string>> treffer = new List<KeyValuePair<int, string>>();
            if (idPuffer <= 0) return treffer;

            DataTable dt = StilleDb.Tabelle(
                "SELECT a.ID, a.Bezeichner, a.ID_Type FROM [" + TABLE + "] v " +
                "INNER JOIN Tab_Energieanlagen a ON v.ID_Anlage = a.ID " +
                "WHERE v.ID_Puffer = ? ORDER BY a.Bezeichner",
                StilleDb.Par("@id", OleDbType.Integer, idPuffer));
            if (dt == null) return treffer;

            foreach (DataRow r in dt.Rows)
                treffer.Add(new KeyValuePair<int, string>(
                    StilleDb.Zahl(StilleDb.Feld(r, "ID")),
                    StilleDb.Text(StilleDb.Feld(r, "Bezeichner"))));

            return treffer;
        }

        // =====================================================================
        // Schreiben
        // =====================================================================

        /// <summary>
        /// Setzt die Mitgliederliste einer Anlage auf genau <paramref name="idsPuffer"/> -
        /// erst DELETE aller Zeilen dieser Anlage, dann INSERT der uebergebenen.
        ///
        /// <b>Warum Delete/Insert und kein Abgleich.</b> Es sind Handvoll Zeilen ohne
        /// eigene Eigenschaften; ein Differenzabgleich waere mehr Code fuer dasselbe
        /// Ergebnis. Dasselbe Muster nutzt der Konfigurationsdialog fuer
        /// <c>Z_ProjektPufferSp</c>.
        ///
        /// <b>Eine leere Liste ist ein gueltiger Aufruf</b> und bedeutet „kein Verbund
        /// mehr" - genau der Weg, auf dem der Anwender den Verbund im Senkendialog wieder
        /// aufloest. Deshalb laeuft das DELETE auch ohne Eintraege.
        ///
        /// Rueckgabe false nur, wenn ein INSERT scheitert; das DELETE gilt als gelungen,
        /// wenn es keine Zeile fand (0 ist kein Fehler).
        /// </summary>
        public static bool Schreiben(int idAnlage, IList<int> idsPuffer)
        {
            if (idAnlage <= 0) return false;

            StelleTabelleSicher();

            if (StilleDb.NonQuery("DELETE FROM [" + TABLE + "] WHERE ID_Anlage = ?",
                                  StilleDb.Par("@anlage", OleDbType.Integer, idAnlage)) < 0)
                return false;

            if (idsPuffer == null || idsPuffer.Count == 0) return true;

            // MAX(ID)+1 EINMAL holen und selbst weiterzaehlen: Ein zweiter Aufruf je Zeile
            // waere eine Abfrage pro Mitglied, und die IDs vergibt hier ohnehin nur dieser
            // eine Vorgang (Hausmuster ADR-001, wie KostenprofilCtrl.Insert).
            int naechsteId = StilleDb.Zahl(StilleDb.Scalar("SELECT MAX(ID) FROM [" + TABLE + "]")) + 1;
            if (naechsteId < 1) naechsteId = 1;

            bool ok = true;
            List<int> geschrieben = new List<int>();

            foreach (int idPuffer in idsPuffer)
            {
                if (idPuffer <= 0 || geschrieben.Contains(idPuffer)) continue;

                if (StilleDb.NonQuery(
                        "INSERT INTO [" + TABLE + "] (ID, ID_Anlage, ID_Puffer) VALUES (?, ?, ?)",
                        StilleDb.Par("@id", OleDbType.Integer, naechsteId),
                        StilleDb.Par("@anlage", OleDbType.Integer, idAnlage),
                        StilleDb.Par("@puffer", OleDbType.Integer, idPuffer)) <= 0)
                {
                    ok = false;
                    continue;
                }

                geschrieben.Add(idPuffer);
                naechsteId++;
            }

            return ok;
        }

        /// <summary>
        /// Entfernt alle Verbundzeilen, die auf einen dieser Puffer zeigen -
        /// die Verbund-Haelfte von <c>PufferSpCtrl.ReferenzenLoesen</c>.
        ///
        /// Ohne diesen Schritt scheiterte das <c>DELETE FROM Tab_Pufferspeicher</c> an der
        /// restriktiven Beziehung <c>FK_Verbund_Puffer</c> (Jet-Fehler 3200) - genau wie es
        /// ohne das Nullen von <c>WS_ID_Puffer</c> scheitern wuerde.
        /// </summary>
        public static void ReferenzenEntfernen(IList<int> pufferIds)
        {
            if (pufferIds == null || pufferIds.Count == 0) return;

            foreach (int idPuffer in pufferIds)
            {
                if (idPuffer <= 0) continue;
                StilleDb.NonQuery("DELETE FROM [" + TABLE + "] WHERE ID_Puffer = ?",
                                  StilleDb.Par("@puffer", OleDbType.Integer, idPuffer));
            }
        }

        // =====================================================================
        // Konfliktregel (Entscheidung des Anwenders 17.08.2026)
        // =====================================================================

        /// <summary>
        /// Prueft, ob die gewuenschte Verbundzuordnung fachlich moeglich ist, und liefert
        /// je Beanstandung einen <see cref="Konfliktbefund"/>; leere Liste = in Ordnung.
        ///
        /// <b>Die Regel.</b> Ein Puffer darf nicht gleichzeitig Verbundmitglied und
        /// anderweitig eigenstaendiges Lade-/Senkenziel sein. Denn ein Mitglied hat im
        /// Verbund keinen eigenen Fuellstand mehr - seine Kapazitaet ist Teil des
        /// gemeinsamen Vorrats unter der Leit-ID. Wuerde ein zweiter Erzeuger ihn
        /// zusaetzlich als eigenes Ziel laden, zaehlte dieselbe Kapazitaet zweimal, und die
        /// Bilanz des Laufs waere falsch, ohne dass es irgendwo auffiele.
        ///
        /// Im Einzelnen beanstandet werden:
        /// <list type="number">
        ///   <item><description>Mitglied ist Hauptsenke (Leitspeicher) EINER Anlage des
        ///     Projekts - auch der eigenen.</description></item>
        ///   <item><description>Mitglied ist Zweitsenke einer Anlage des
        ///     Projekts.</description></item>
        ///   <item><description>Mitglied gehoert schon zu einem Verbund mit ANDEREM
        ///     Leitspeicher.</description></item>
        ///   <item><description>Der gewaehlte Leitspeicher ist selbst Mitglied eines
        ///     fremden Verbunds.</description></item>
        ///   <item><description>Mitglied ist die Waermequelle IRGENDEINER Anlage des
        ///     Projekts (Kurzschluss). Fuer die eigene Anlage ist das dieselbe Regel wie
        ///     Punkt 4 in <c>WaermesenkeClass.Pruefen</c>; die Ausweitung auf das ganze
        ///     Projekt kam aus dem Wirkungsnachweis dieses Pakets - ein Quellspeicher
        ///     rechnet auf einem eigenen Weg und wuerde im Verbund doppelt zaehlen
        ///     (Begruendung bei <see cref="QuellPufferDesProjekts"/>).</description></item>
        ///   <item><description>Mitglied gehoert nicht zum Projekt oder traegt eine andere
        ///     Verwendung als der Leitspeicher.</description></item>
        ///   <item><description><b>PAKET P1:</b> Der Leitspeicher fuehrt eine SCHICHTUNG
        ///     (<c>Schichten_Anzahl &gt; 1</c>). Kriterium W6, HART - Verbund und
        ///     Schichtung schliessen sich je Rechenspeicher aus (Konzept 6.3,
        ///     Entscheidung F8).</description></item>
        /// </list>
        ///
        /// <b>Ausdrueckliche AUSNAHME zu 3.</b> Derselbe Verbund darf von MEHREREN
        /// Erzeugern geladen werden. Das ist genau der Fall „gleicher Leitspeicher,
        /// gleiche Mitglieder" - und der faellt hier nicht auf, weil dann kein FREMDER
        /// Leitspeicher gefunden wird.
        ///
        /// <b>Dialogfrei.</b> Die Meldungstexte baut der Aufrufer
        /// (<c>WaermesenkeClass.Pruefen</c>); hier entstehen nur Befunde.
        /// </summary>
        /// <param name="idProjekt">Projekt der Anlage.</param>
        /// <param name="idAnlage">Die Anlage, deren Verbund gespeichert werden soll.</param>
        /// <param name="idLeit">Ihr Leitspeicher (<c>WS_ID_Puffer</c> nach dem Speichern).</param>
        /// <param name="idPuffer2">Ihre Zweitsenke (<c>WS_ID_Puffer2</c>), 0 wenn keine.</param>
        /// <param name="mitglieder">Die gewuenschten zusaetzlichen Mitglieder.</param>
        /// <param name="verwendungLeit">
        /// Wirksame Verwendung des Leitspeichers; ein Mitglied muss dieselbe tragen.
        /// Leer = Pruefung entfaellt.
        /// </param>
        public static List<Konfliktbefund> KonfliktPruefen(int idProjekt, int idAnlage,
                                                          int idLeit, int idPuffer2,
                                                          IList<int> mitglieder,
                                                          string verwendungLeit)
        {
            List<Konfliktbefund> befunde = new List<Konfliktbefund>();
            if (mitglieder == null || mitglieder.Count == 0) return befunde;

            // --- 7. W6, GEGENRICHTUNG (PAKET P1): Der Leitspeicher darf keine
            //        SCHICHTUNG fuehren. Verbund und Schichtung schliessen sich je
            //        Rechenspeicher aus (Konzept 6.3, Entscheidung F8) - Begruendung
            //        bei GRUND_LEIT_GESCHICHTET. Der Speicherdialog haelt dieselbe
            //        Regel von der anderen Seite: Dort wird N > 1 an einem bereits
            //        bestehenden Leitspeicher abgewiesen.
            //
            //        Spaltentolerant: Ohne Migrationsschritt 53 liefert
            //        SchichtdatenLesen die Vorbelegung N = 1, und der Guard schweigt.
            //        Zuerst geprueft, weil er die GANZE Zuordnung betrifft und nicht
            //        ein einzelnes Mitglied - der Dialog nennt ohnehin nur den ersten
            //        Befund.
            if (idLeit > 0 && PufferSpCtrl.SchichtdatenLesen(idLeit).Geschichtet)
                befunde.Add(new Konfliktbefund
                {
                    ID_Puffer = idLeit,
                    Grund = GRUND_LEIT_GESCHICHTET
                });

            // --- 4. Der Leitspeicher selbst darf nicht Mitglied eines fremden Verbunds
            //        sein. Er ist der Vorratsbehaelter DIESES Verbunds; gehoerte er
            //        zugleich zu einem anderen, gaebe es zwei Vorraete mit demselben
            //        Behaelter.
            int fremderLeitDesLeit = FremderLeit(idLeit, idAnlage, idLeit);
            if (fremderLeitDesLeit > 0)
                befunde.Add(new Konfliktbefund
                {
                    ID_Puffer = idLeit,
                    Grund = GRUND_LEIT_IST_MITGLIED,
                    ID_FremderLeit = fremderLeitDesLeit
                });

            // Quellspeicher des GANZEN Projekts, nicht nur der eigenen Anlage: Auch die
            // Waermequelle einer anderen Waermepumpe darf nicht in diesen Verbund - sie
            // rechnet auf einem eigenen Weg und zaehlte sonst doppelt (Begruendung bei
            // QuellPufferDesProjekts; die Engine wehrt denselben Fall in
            // SimulationControl.VerbundAufaddieren ab).
            List<int> quellPuffer = QuellPufferDesProjekts(idProjekt);

            foreach (int idMitglied in mitglieder)
            {
                if (idMitglied <= 0) continue;

                // --- 6. Projekt und Verwendung ---------------------------------------
                WaermesenkeClass.PufferInfo p = WaermesenkeClass.PufferLesen(idMitglied);
                if (p == null || p.ID_Projekt != idProjekt ||
                    (verwendungLeit != null && verwendungLeit.Length > 0 &&
                     !string.Equals(WaermesenkeClass.WirksameVerwendung(p), verwendungLeit,
                                    StringComparison.OrdinalIgnoreCase)))
                {
                    befunde.Add(new Konfliktbefund { ID_Puffer = idMitglied, Grund = GRUND_PASST_NICHT });
                    continue;
                }

                // --- 5. Waermequelle einer Anlage des Projekts ------------------------
                if (quellPuffer.Contains(idMitglied))
                {
                    befunde.Add(new Konfliktbefund { ID_Puffer = idMitglied, Grund = GRUND_QUELLE });
                    continue;
                }

                // --- 1./2. eigenstaendiges Senkenziel einer Anlage des Projekts -------
                //
                // Die eigene Zweitsenke wird MITGEPRUEFT (idPuffer2 kommt aus der
                // Oberflaeche und steht noch nicht in der Datenbank), die eigene
                // Hauptsenke ist der Leitspeicher und deshalb schon durch
                // Normalisieren ausgeschlossen.
                if (idPuffer2 > 0 && idPuffer2 == idMitglied)
                {
                    befunde.Add(new Konfliktbefund
                    {
                        ID_Puffer = idMitglied,
                        Grund = GRUND_ZWEITSENKE,
                        ID_AndereAnlage = idAnlage
                    });
                    continue;
                }

                Konfliktbefund senke = SenkenbelegungPruefen(idProjekt, idAnlage, idMitglied);
                if (senke != null) { befunde.Add(senke); continue; }

                // --- 3. fremder Verbund ----------------------------------------------
                int fremd = FremderLeit(idMitglied, idAnlage, idLeit);
                if (fremd > 0)
                    befunde.Add(new Konfliktbefund
                    {
                        ID_Puffer = idMitglied,
                        Grund = GRUND_ANDERER_VERBUND,
                        ID_FremderLeit = fremd
                    });
            }

            return befunde;
        }

        /// <summary>
        /// Belegt eine Anlage des Projekts diesen Puffer als Haupt- oder Zweitsenke?
        /// <c>null</c> = frei.
        ///
        /// Die eigene Anlage wird UEBERGANGEN: Ihre Hauptsenke ist der Leitspeicher (den
        /// die Mitgliederliste nicht enthalten darf, dafuer sorgt
        /// <c>WaermesenkeClass.Normalisieren</c>), und ihre Zweitsenke prueft der Aufrufer
        /// selbst gegen den NOCH NICHT gespeicherten Dialogstand.
        /// </summary>
        private static Konfliktbefund SenkenbelegungPruefen(int idProjekt, int idAnlage, int idPuffer)
        {
            DataTable dt = StilleDb.Tabelle(
                "SELECT ID, WS_ID_Puffer, WS_ID_Puffer2 FROM Tab_Energieanlagen " +
                "WHERE ID_Projekt = ? AND ID <> ? AND (WS_ID_Puffer = ? OR WS_ID_Puffer2 = ?)",
                StilleDb.Par("@proj", OleDbType.Integer, idProjekt),
                StilleDb.Par("@selbst", OleDbType.Integer, idAnlage),
                StilleDb.Par("@a", OleDbType.Integer, idPuffer),
                StilleDb.Par("@b", OleDbType.Integer, idPuffer));
            if (dt == null || dt.Rows.Count == 0) return null;

            DataRow r = dt.Rows[0];
            bool haupt = StilleDb.Zahl(StilleDb.Feld(r, "WS_ID_Puffer")) == idPuffer;

            return new Konfliktbefund
            {
                ID_Puffer = idPuffer,
                Grund = haupt ? GRUND_HAUPTSENKE : GRUND_ZWEITSENKE,
                ID_AndereAnlage = StilleDb.Zahl(StilleDb.Feld(r, "ID"))
            };
        }

        /// <summary>
        /// Der Leitspeicher eines FREMDEN Verbunds, in dem <paramref name="idPuffer"/>
        /// Mitglied ist; 0, wenn es keinen gibt.
        ///
        /// „Fremd" heisst: eine ANDERE Anlage als <paramref name="idAnlage"/> UND ein
        /// anderer Leitspeicher als <paramref name="idLeitEigen"/>. Damit bleibt die
        /// ausdrueckliche Ausnahme der Konfliktregel offen - mehrere Erzeuger duerfen
        /// denselben Verbund laden, solange Leitspeicher und Mitglieder uebereinstimmen.
        /// </summary>
        private static int FremderLeit(int idPuffer, int idAnlage, int idLeitEigen)
        {
            if (idPuffer <= 0) return 0;

            DataTable dt = StilleDb.Tabelle(
                "SELECT a.WS_ID_Puffer FROM [" + TABLE + "] v " +
                "INNER JOIN Tab_Energieanlagen a ON v.ID_Anlage = a.ID " +
                "WHERE v.ID_Puffer = ? AND a.ID <> ?",
                StilleDb.Par("@puffer", OleDbType.Integer, idPuffer),
                StilleDb.Par("@anlage", OleDbType.Integer, idAnlage));
            if (dt == null) return 0;

            foreach (DataRow r in dt.Rows)
            {
                int leit = StilleDb.Zahl(StilleDb.Feld(r, "WS_ID_Puffer"));
                if (leit > 0 && leit != idLeitEigen) return leit;
            }

            return 0;
        }
    }
}
