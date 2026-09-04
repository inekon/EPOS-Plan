using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der ERGEBNISZUSTAND der Schemamigration — losgelöst von der Migration selbst.
    ///
    /// <para><b>Warum diese Klasse existiert (Umsetzungskonzept iU3, Kante K1).</b> Der
    /// Rechenkern (<c>SimulationRunner</c>, <c>SimulationControl</c>) muss vor jedem Lauf
    /// wissen, ob die Datenbank auf dem benötigten Stand ist. Bisher fragte er das direkt
    /// bei <see cref="SchemaMigration"/> ab — und zog damit die vollständige Migration mit
    /// ihrem Access-Zweig (<c>System.Data.OleDb</c>) in den Kern hinein. Die Antwort auf
    /// „darf gerechnet werden?" sind aber nur vier Werte; die Migration, die sie erzeugt,
    /// gehört nicht dazu.</para>
    ///
    /// <para>Deshalb tragen die Werte jetzt hier. <see cref="SchemaMigration"/> behält
    /// seine öffentliche Fläche vollständig und LEITET WEITER — jeder bestehende Aufrufer
    /// bleibt gültig, der Rechenkern kommt ohne die Migration aus.</para>
    ///
    /// <para><b>Vorbelegung.</b> <see cref="MigrationOk"/> ist vor dem ersten Lauf
    /// <c>true</c>: Werkzeuge, die die Migration gar nicht anstoßen (Referenzlauf-Suite),
    /// sollen dadurch nicht blockiert werden. <see cref="Ausgefuehrt"/> bleibt bis zum
    /// ersten Lauf <c>false</c> — die Sperre greift erst, wenn tatsächlich migriert
    /// wurde und dabei etwas fehlschlug.</para>
    /// </summary>
    public static class SchemaStand
    {
        /// <summary>
        /// Der Schemastand, den ein vollständiger Migrationslauf dieser Programmfassung
        /// erreicht — die ZAHL, nicht die Migration.
        ///
        /// <para>Sie steht hier und nicht bei <c>SchemaMigration</c>, weil der
        /// PROJEKTTRANSFER sie braucht: <c>ProjektExportImportCtrl</c> schreibt sie in
        /// das Manifest eines <c>.wpx</c>-Pakets und lehnt beim Import ein Paket mit
        /// abweichendem Stand ab. Genau diese eine Konstante war bis iU9‑W15a die
        /// einzige Kante, die den Transfercontroller (1 278 Zeilen, ohne jede
        /// WinForms-Berührung) im Anwendungsprojekt festhielt — und damit den
        /// Projekttransfer auf iOS unmöglich machte (Befund W15a‑B30).</para>
        ///
        /// <para><c>SchemaMigration.ZIEL_VERSION</c> verweist seither HIERHER; die
        /// Fortschreibung der Nummer bleibt bei der Migration beschrieben (dort steht
        /// die Reihenfolge „erst Schrittkonstante, Methode und SCHRITTE-Eintrag, DANN
        /// das Ziel"). Wer eine neue Migrationsstufe anlegt, ändert die Zahl HIER.</para>
        /// </summary>
        public const int Zielversion = 61;

        /// <summary>
        /// Nummer der Vorbelegung von <c>Extrapolation_erlaubt</c> (Paket 8,
        /// Konzept 13.4). Die SPALTE entsteht bereits in Schritt 2; dieser Schritt setzt
        /// ihren WERT einmalig auf WAHR und ist damit das zweite DML des Vorhabens.
        ///
        /// <para>Steht hier und nicht bei <see cref="SchemaMigration"/>, weil
        /// <c>KonfigurationCtrl</c> die Nummer gegen den gespeicherten Schemastand prüft
        /// (Kante K2) — eine reine Zahl, für die der Kern die Migration nicht braucht.
        /// <see cref="SchemaMigration.SCHRITT_7_EXTRAPOLATION"/> verweist hierher.</para>
        /// </summary>
        public const int SCHRITT_7_EXTRAPOLATION = 7;

        /// <summary>
        /// DDL des Parallelverbunds (Kante K3). <c>AnlagePufferVerbundCtrl</c> legt die
        /// Tabelle bei Bedarf still selbst an, wenn die Migration sie noch nicht gebaut
        /// hat; dafür genügt der SQL-Text, nicht die Migration.
        ///
        /// <para>Keine DEFAULT-Werte auf den beiden FK-Spalten — dieselbe Regel wie im
        /// Spaltenkatalog: eine 0 verletzte die erzwungene Beziehung, „nicht gesetzt" wird
        /// durch NULL ausgedrückt.</para>
        /// </summary>
        public const string SQL_CREATE_ANLAGEPUFFERVERBUND =
            "CREATE TABLE Z_AnlagePufferVerbund (ID LONG NOT NULL PRIMARY KEY, " +
            "ID_Anlage LONG, ID_Puffer LONG)";

        /// <summary>
        /// Index über den Anlagenverweis — der Suchweg des Dialogs (Mitglieder EINER
        /// Anlage). Die Registry-Speisung liest projektweit über einen Verbund zu
        /// <c>Tab_Energieanlagen</c> und profitiert davon ebenfalls.
        /// </summary>
        public const string SQL_INDEX_ANLAGEPUFFERVERBUND =
            "CREATE INDEX idx_AnlagePufferVerbund ON Z_AnlagePufferVerbund (ID_Anlage)";

        /// <summary>
        /// Nummer des Migrationsschritts „Einheitenkonsistenz" (Kante iU4-2). Der
        /// Prüfer <c>EnergieEinheitenPruefung</c> nennt die Nummer nur in seiner
        /// Meldung „Migrationsschritt N steht aus" — eine reine Zahl, für die er die
        /// Migration mit ihrem Access-Zweig nicht braucht.
        ///
        /// <para><see cref="SchemaMigration.SCHRITT_25_EINHEITENKONSISTENZ"/> verweist
        /// hierher; die vollständige Begründung des Schritts steht dort.</para>
        /// </summary>
        public const int SCHRITT_25_EINHEITENKONSISTENZ = 25;

        /// <summary>
        /// DDL der Preisreihe (Kante iU4-2, Muster <see cref="SQL_CREATE_ANLAGEPUFFERVERBUND"/>).
        /// <c>PreisreiheCtrl.StelleTabellenSicher</c> legt Kopf- und Wertetabelle bei
        /// Bedarf still selbst an, wenn die Migration sie noch nicht gebaut hat; dafür
        /// genügt der SQL-Text, nicht die Migration.
        ///
        /// <para><c>ID_Energietraeger</c> ist seit Schritt 40 Teil des CREATE, damit auch
        /// diese tolerante Rückfallebene die Spalte mitbringt; Bestandstabellen rüstet
        /// Schritt 40 nach.</para>
        /// </summary>
        public const string SQL_CREATE_PREISREIHE =
            "CREATE TABLE Tab_Preisreihe (ID LONG NOT NULL PRIMARY KEY, " +
            "ID_Projekt LONG, Bezeichner TEXT(255), Jahr LONG, " +
            "Aufloesung TEXT(50), Einheit TEXT(50), ID_Energietraeger LONG)";

        /// <summary>Index über den Projektbezug - der Suchweg der Auswahllisten.</summary>
        public const string SQL_INDEX_PREISREIHE =
            "CREATE INDEX idx_Preisreihe ON Tab_Preisreihe (ID_Projekt)";

        /// <summary>Werte einer Preisreihe: eine Zeile je Intervall, Reihenfolge = ID-Reihenfolge.</summary>
        public const string SQL_CREATE_PREISREIHEDATEN =
            "CREATE TABLE Tab_PreisreiheDaten (ID LONG NOT NULL PRIMARY KEY, " +
            "ID_Preisreihe LONG, Wert DOUBLE)";

        /// <summary>Index über den Kopfverweis - der einzige Suchweg auf die Werte.</summary>
        public const string SQL_INDEX_PREISREIHEDATEN =
            "CREATE INDEX idx_PreisreiheDaten ON Tab_PreisreiheDaten (ID_Preisreihe)";

        /// <summary>
        /// Löschweitergabe vom Kopf auf die Werte - ohne sie blieben nach dem Löschen
        /// einer Reihe bis zu 35.040 Waisenzeilen stehen.
        ///
        /// <para><b>Nur der Access-Zweig.</b> <c>ALTER TABLE … ADD CONSTRAINT</c> ist
        /// Access-DDL; SQLite kann einer bestehenden Tabelle keinen Fremdschlüssel mehr
        /// anhängen — die Anweisung endet dort mit „near CONSTRAINT: syntax error"
        /// (SQL-Dialekt-Audit 03.09.2026). Verwendet wird sie deshalb ausschließlich von
        /// <c>SchemaMigration.PreisreiheTabellen</c>, dem Erststart-Weg über die
        /// ACE-Engine. Die SQLite-Rückfallebene nimmt
        /// <see cref="SQL_CREATE_PREISREIHEDATEN_MIT_FK"/>, das die Beziehung schon beim
        /// Anlegen mitbringt.</para>
        /// </summary>
        public const string SQL_FK_PREISREIHEDATEN =
            "ALTER TABLE Tab_PreisreiheDaten ADD CONSTRAINT FK_PreisreiheDaten " +
            "FOREIGN KEY (ID_Preisreihe) REFERENCES Tab_Preisreihe (ID) ON DELETE CASCADE";

        /// <summary>
        /// Dieselbe Tabelle wie <see cref="SQL_CREATE_PREISREIHEDATEN"/>, aber MIT der
        /// Löschweitergabe im CREATE - die SQLite-Schreibweise derselben Absicht.
        ///
        /// <para>Angelegt am 03.09.2026 (SQL-Dialekt-Audit). Die Rückfallebene
        /// <c>PreisreiheCtrl.StelleTabellenSicher</c> legte die Tabelle bisher ohne
        /// Beziehung an und schob den Fremdschlüssel per ALTER nach; unter SQLite
        /// scheiterte das lautlos (die Zeile steht in einem stillen catch), und
        /// gelöschte Reihen ließen ihre Werte stehen. Der Migrator erzeugt die Tabelle
        /// genau so, wie sie hier steht - beide Wege enden damit beim selben Schema.</para>
        /// </summary>
        public const string SQL_CREATE_PREISREIHEDATEN_MIT_FK =
            "CREATE TABLE Tab_PreisreiheDaten (ID LONG NOT NULL PRIMARY KEY, " +
            "ID_Preisreihe LONG, Wert DOUBLE, " +
            "FOREIGN KEY (ID_Preisreihe) REFERENCES Tab_Preisreihe (ID) ON DELETE CASCADE)";

        /// <summary>
        /// DDL des Kostenprofils (Kante iU4-2). <c>KostenprofilCtrl</c> legt die Tabelle
        /// bei Bedarf still selbst an - wie bei der Preisreihe genügt dafür der SQL-Text.
        /// </summary>
        public const string SQL_CREATE_KOSTENPROFIL =
            "CREATE TABLE Tab_Kostenprofil (ID LONG NOT NULL PRIMARY KEY, " +
            "ID_Projekt LONG, Bezeichner TEXT(255), Monatswerte TEXT(255), Wochenwerte MEMO)";

        /// <summary>Index über den Projektbezug.</summary>
        public const string SQL_INDEX_KOSTENPROFIL =
            "CREATE INDEX idx_Kostenprofil ON Tab_Kostenprofil (ID_Projekt)";

        /// <summary>
        /// false, sobald ein Lauf einen Schritt nicht abschließen konnte. Vor dem ersten
        /// Lauf true - Werkzeuge, die die Migration gar nicht anstoßen (Referenzlauf-Suite),
        /// sollen dadurch nicht blockiert werden.
        /// </summary>
        public static bool MigrationOk { get; set; } = true;

        /// <summary>Vollständiger Bericht des letzten Laufs; erste Zeile ist der DB-Pfad.</summary>
        public static string Fehlerbericht { get; set; } = "";

        /// <summary>true, sobald <c>SchemaMigration.Ausfuehren</c> mindestens einmal gelaufen ist.</summary>
        public static bool Ausgefuehrt { get; set; }

        /// <summary>
        /// Sperrt den Simulationsbereich, wenn ein Migrationslauf stattgefunden HAT und
        /// dabei etwas fehlschlug.
        ///
        /// <para><b>Unverändert seit ARBEITSPAKET S6 - und das ist geprüft, nicht
        /// unterlassen.</b> Verlangt ist die Semantik „Stand &lt;
        /// <c>SchemaMigration.ZIEL_VERSION</c> ⇒ gesperrt". Sie kommt im SQLite-Zweig
        /// genauso zustande wie vorher im Access-Zweig, nämlich über
        /// <see cref="MigrationOk"/>: <c>SchemaMigration.Ausfuehren</c> liefert
        /// <c>alleOk &amp;&amp; StandNachher &gt;= ZIEL_VERSION</c>, und
        /// <c>SchritteAbarbeitenSqlite</c> bricht bei Stand 0 und bei Stand &lt; 61 mit
        /// <c>false</c> ab. Ein Stand unter 61 kann daher gar nicht als „ok" durchgehen.
        /// Eine zweite Prüfung auf <c>StandNachher</c> stünde hier nur als Wiederholung -
        /// und würde die Sperre an einen Zähler koppeln, den auch
        /// <c>SchemaMigration.HebeAltbestand</c> beschreibt (siehe die Begründung
        /// dort).</para>
        ///
        /// <para><c>SchemaMigration.HebeAltbestand</c> rührt <see cref="Ausgefuehrt"/> und
        /// <see cref="MigrationOk"/> nicht an; eine Alt-Hebung kann diese Sperre also
        /// weder setzen noch aufheben.</para>
        /// </summary>
        public static bool SimulationGesperrt(out string grund)
        {
            if (!Ausgefuehrt || MigrationOk)
            {
                grund = null;
                return false;
            }

            grund = "Die Datenbank ist nicht auf dem für die Simulation benötigten Stand." +
                    Environment.NewLine + Environment.NewLine +
                    FehlerKopf() + Environment.NewLine + Environment.NewLine +
                    "Der Simulationsbereich bleibt gesperrt, bis die Aktualisierung der " +
                    "Datenbank erfolgreich war.";
            return true;
        }

        /// <summary>
        /// Die ersten Zeilen des Berichts - genug für eine verständliche Meldung,
        /// ohne den Anwender mit dem vollständigen Protokoll zu erschlagen.
        /// </summary>
        public static string FehlerKopf()
        {
            if (string.IsNullOrEmpty(Fehlerbericht)) return "(kein Bericht vorhanden)";

            string[] zeilen = Fehlerbericht.Replace("\r\n", "\n").Split('\n');
            var kopf = new List<string>();
            foreach (string z in zeilen)
            {
                kopf.Add(z);
                if (kopf.Count >= 12) break;
            }
            return string.Join(Environment.NewLine, kopf).TrimEnd();
        }
    }
}
