using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die DDL der Strangzuordnung</b> — Migrationsschritt 66
    /// (Konzept <c>Konzept_Wechselrichter_EPOS-Plan.md</c> 3.4 und 3.6,
    /// Anwenderentscheid <b>W6‑E‑2</b> vom 06.09.2026, Stufe S2).
    ///
    /// <para><b>Warum eine EIGENE Klasse neben <see cref="WechselrichterSchema"/>.</b>
    /// Die Schwesterklasse trägt im Kopf ihre Nummer: „Die DDL des
    /// Wechselrichterkatalogs — Migrationsschritt 65". Ihre Eigenschaft
    /// <c>Anweisungen</c> ist genau die Liste, die <c>SchemaMigration</c> und
    /// <c>Werkzeuge/Testdatenbankschema</c> für DIESEN Schritt abarbeiten und zählen
    /// („0 von 2 Tabelle(n) angelegt"). Ein dritter Eintrag darin machte die Zählung
    /// beider Schritte falsch, und ein Zweitlauf des Schritts 65 legte plötzlich eine
    /// Tabelle des Schritts 66 an. <b>Eine Klasse je Schritt</b> hält die zwei
    /// Zählungen und die zwei Idempotenzzusagen auseinander — dieselbe Trennung, mit
    /// der <c>KlimaWaisenBereinigung</c> nur die zwei <c>DELETE</c> des Schritts 62
    /// trägt und nichts sonst.</para>
    ///
    /// <para><b>Was hier NICHT steht: die Spalte des Schalters.</b>
    /// <c>Tab_Energieanlagen.PV_Wechselrichterweg</c> (Anwenderwunsch <b>W6‑E‑3</b>)
    /// gehört zum selben Schritt 66, ist aber eine ADDITIVE Spalte an einer
    /// vorhandenen Tabelle und steht deshalb dort, wo alle solchen Spalten stehen:
    /// <see cref="SchemaKatalog.Schritt66_PvWechselrichterweg"/>. Nur so erreicht sie
    /// die Rückfallebene <c>WaermequelleClass.SchemaSicherstellen</c>
    /// (<see cref="SchemaKatalog.Alle"/>) — und die braucht sie, weil
    /// <c>AnlagenSql.SQL_ANLAGE_INSERT</c> sie namentlich nennt.</para>
    ///
    /// <para><b>Nur SQLite, kein Access-Zweig</b> — wörtlich die Begründung des
    /// Schritts 65: Der Anwender hat am 06.09.2026 festgehalten, dass die
    /// Access-Datenbank nicht mehr relevant ist. <c>sql/schema/001_grundschema.sql</c>
    /// bleibt der eingefrorene Access-Zielstand 61 („NICHT VON HAND AENDERN"),
    /// eingebettete Ressource des <c>EposSqliteMigrator</c> und über
    /// <c>inventar.json</c> auf 114 Tabellen gezählt.</para>
    ///
    /// <para><b>STRICT und <c>IF NOT EXISTS</c>.</b> Alle Tabellen des Zielschemas sind
    /// <c>STRICT</c>; erlaubt sind dort nur INT/INTEGER/REAL/TEXT/BLOB/ANY. Die
    /// Idempotenz trägt <c>IF NOT EXISTS</c> — SQLite kann das selbst, und der
    /// SQLite-Zweig der Migration deutet bewusst keine Fehlertexte
    /// (<c>SchemaMigration.SqliteDdl</c>).</para>
    ///
    /// <para><b>Zwei Fremdschlüssel, und nur zwei</b> (Konzept 3.6, Zeile zu Schritt 66)
    /// — genau die Bauart des Vorbilds <c>Z_AnlageSenke</c>:</para>
    /// <list type="bullet">
    ///   <item><description><c>ID_Anlage</c> mit <b><c>ON DELETE CASCADE</c></b>. Die
    ///     Löschweitergabe ist hier nicht wahlweise, sondern zwingend: Der Speicherweg
    ///     jeder Anlage ist Löschen + Neuanlegen
    ///     (<c>WizardCtrl.Del_Projekt_Waermeerzeuger</c>), und restriktiv scheiterte
    ///     schon das <c>DELETE</c> — es liesse sich kein Projekt mehr speichern
    ///     (gemessen am 27.08.2026 für <c>Z_AnlageSenke</c>, Begründung bei
    ///     <c>SchemaMigration.SQL_FK_SENKE_ANLAGE</c>). Der Preis ist derselbe: Die
    ///     Strangzeilen müssen über den Speicherweg GERETTET werden
    ///     (<c>WizardCtrl.StraengeSichern</c> / <c>…Wiederherstellen</c>, Block ST1) —
    ///     genau die Falle N3.3 des Konzepts.</description></item>
    ///   <item><description><c>ID_Wechselrichter</c> RESTRIKTIV auf die PROJEKTKOPIE
    ///     <c>Tab_Wechselrichter</c> — wörtlich das Verhältnis
    ///     <c>Z_AnlageSenke.ID_Puffer</c> → <c>Tab_Pufferspeicher</c>. Restriktiv ist
    ///     hier gefahrlos, weil der Projekt-Löschweg zuerst die Anlagenzeilen entfernt
    ///     (<c>WErzeugerCtrl.Delete</c>) und die Strangzeilen damit vor jeder
    ///     Gerätezeile fallen. Eine <c>0</c> wird NIE geschrieben; „kein Gerät" ist
    ///     NULL.</description></item>
    /// </list>
    ///
    /// <para><b>Kein dritter Fremdschlüssel auf <c>ID_PV</c>.</b> Die Spalte steht
    /// bereit (Konzept 3.4, Entwurfsentscheidung 3: ein abweichender Modultyp je
    /// Strang), wird in Stufe S2 aber weder in der Oberfläche gezeigt noch geschrieben.
    /// Eine erzwungene Beziehung auf <c>Tab_PV</c> wäre eine stille Verhaltensänderung
    /// am Löschweg der Modul-Projektkopien (<c>GeraeteWaisen.Aufraeumen</c> räumt
    /// unreferenzierte Gerätezeilen ab und kennt diese Tabelle nicht) — dieselbe
    /// Zurückhaltung, mit der Schritt 65 auf den Fremdschlüssel über
    /// <c>ID_Projekt</c> verzichtet hat.</para>
    ///
    /// <para><b>Kein DDL-DEFAULT auf Fachwerten</b> (Hausregel, PV-Ertragsmodell N2.2):
    /// NULL ist der Vorgabewert, und der Vorgabewert ist der, der nichts ändert —
    /// <c>Geraetenummer</c> und <c>Mppt</c> NULL heissen 1, <c>Straenge_Parallel</c>
    /// NULL heisst 1, <c>Neigung</c>/<c>Azimut</c> NULL heissen „der Anlagenwert".
    /// <c>ID_Anlage</c> und <c>Rang</c> sind die einzigen <c>NOT NULL</c>-Spalten,
    /// genau wie im Vorbild.</para>
    ///
    /// <para><b>Ergebnisneutral.</b> Die Anweisung ist reines DDL ohne DML: Nach der
    /// Migration führt kein Projekt eine Strangzeile, und kein Rechenweg liest die
    /// Tabelle — S2 fasst <c>SimulationPV</c> nicht an. Der Referenzlauf bleibt
    /// byte-gleich.</para>
    /// </summary>
    public static class AnlageStrangSchema
    {
        // =================================================================
        //  Die Spaltennamen — sprachneutral und EINMAL
        // =================================================================

        /// <summary>FK auf <c>Tab_Energieanlagen.ID</c>, <c>ON DELETE CASCADE</c>. Pflicht.</summary>
        public const string SPALTE_ID_ANLAGE = "ID_Anlage";

        /// <summary>Reihenfolge in der Strangtabelle der Oberfläche, 1…n. Pflicht.</summary>
        public const string SPALTE_RANG = "Rang";

        /// <summary>Freitext („Dach Süd", „Ostseite"); NULL = der Rang als Anzeige.</summary>
        public const string SPALTE_BEZEICHNER = "Bezeichner";

        /// <summary>
        /// FK auf die PROJEKTKOPIE <c>Tab_Wechselrichter.ID</c>; NULL = kein Gerät
        /// zugeordnet.
        /// <para>Auf die Projektkopie und NIE auf den Katalog: „Projekte KOPIEREN
        /// Katalogsätze, alle persistierten Verweise zeigen auf die Projektkopie"
        /// (<c>KatalogRegistry</c>, Konzept 3.2).</para>
        /// </summary>
        public const string SPALTE_ID_WECHSELRICHTER = "ID_Wechselrichter";

        /// <summary>
        /// Welches PHYSISCHE Gerät dieses Typs (1…n); NULL = 1.
        /// <para>Das Gruppierungsmerkmal des Clippings: Es rechnet je
        /// (<c>ID_Anlage</c>, <c>ID_Wechselrichter</c>, <c>Geraetenummer</c>), und die
        /// Gerätezahl für die Kosten ist <c>COUNT(DISTINCT …)</c> (Konzept 3.4,
        /// Entscheidungsfrage Q6).</para>
        /// </summary>
        public const string SPALTE_GERAETENUMMER = "Geraetenummer";

        /// <summary>MPPT-Eingang dieses Geräts (1…n); NULL = 1.</summary>
        public const string SPALTE_MPPT = "Mppt";

        /// <summary>Module in Reihe — die Größe, an der P1 bis P3 hängen. Pflichtangabe der Maske.</summary>
        public const string SPALTE_MODULE_REIHE = "Module_Reihe";

        /// <summary>Parallel geschaltete Stränge; NULL = 1.</summary>
        public const string SPALTE_STRAENGE_PARALLEL = "Straenge_Parallel";

        /// <summary>Neigung dieses Teilfelds [°]; <b>NULL = der Anlagenwert</b> (Konzept 3.4).</summary>
        public const string SPALTE_NEIGUNG = "Neigung";

        /// <summary>Azimut dieses Teilfelds [°]; <b>NULL = der Anlagenwert</b>.</summary>
        public const string SPALTE_AZIMUT = "Azimut";

        /// <summary>
        /// Abweichender Modultyp (→ <c>Tab_PV.ID</c>); NULL = das Modul der Anlage.
        /// <para>In Stufe S2 nicht in der Oberfläche und ohne Fremdschlüssel (siehe
        /// Klassenkopf) — sie steht bereit, sobald sie gebraucht wird. Wer sie jetzt
        /// wegliesse, braucht später einen Migrationsschritt für ein Feld, das ohnehin
        /// absehbar ist.</para>
        /// </summary>
        public const string SPALTE_ID_PV = "ID_PV";

        // =================================================================
        //  Die DDL
        // =================================================================

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Z_AnlageStrang</c> — 12 Spalten und zwei
        /// Fremdschlüssel (Konzept 3.4).
        /// </summary>
        public const string SQL_CREATE =
            "CREATE TABLE IF NOT EXISTS \"Z_AnlageStrang\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Anlage\" INTEGER NOT NULL,\n" +
            "    \"Rang\" INTEGER NOT NULL,\n" +
            "    \"Bezeichner\" TEXT CHECK (length(\"Bezeichner\") <= 50),\n" +
            "    \"ID_Wechselrichter\" INTEGER,\n" +
            "    \"Geraetenummer\" INTEGER,\n" +
            "    \"Mppt\" INTEGER,\n" +
            "    \"Module_Reihe\" INTEGER,\n" +
            "    \"Straenge_Parallel\" INTEGER,\n" +
            "    \"Neigung\" INTEGER,\n" +
            "    \"Azimut\" INTEGER,\n" +
            "    \"ID_PV\" INTEGER,\n" +
            "    FOREIGN KEY (\"ID_Anlage\") REFERENCES \"Tab_Energieanlagen\" (\"ID\") ON DELETE CASCADE,\n" +
            "    FOREIGN KEY (\"ID_Wechselrichter\") REFERENCES \"Tab_Wechselrichter\" (\"ID\")\n" +
            ") STRICT";

        /// <summary>
        /// Die Anweisung(en) des Schritts, je Tabellenname — so, wie Migration und
        /// Werkzeug sie abarbeiten. Bauform wörtlich
        /// <see cref="WechselrichterSchema.Anweisungen"/>, damit beide Stellen
        /// dieselbe Schleife fahren.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(
                    SchemaKatalog.Z_ANLAGESTRANG, SQL_CREATE);
            }
        }

        /// <summary>
        /// Die Spalten der Strangzeile in Schemareihenfolge, ohne <c>ID</c> — die EINE
        /// Liste, an der Leseabfrage und <c>INSERT</c> des
        /// <c>AnlageStrangCtrl</c> hängen.
        /// </summary>
        public static readonly string[] Spalten =
        {
            SPALTE_ID_ANLAGE, SPALTE_RANG, SPALTE_BEZEICHNER, SPALTE_ID_WECHSELRICHTER,
            SPALTE_GERAETENUMMER, SPALTE_MPPT, SPALTE_MODULE_REIHE,
            SPALTE_STRAENGE_PARALLEL, SPALTE_NEIGUNG, SPALTE_AZIMUT, SPALTE_ID_PV
        };
    }
}
