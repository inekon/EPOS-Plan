using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // IMPORTQUELLE UND IMPORTZUORDNUNG - Schemaschritt S-F der Gebaeudesimulation, Stufe G4c
    // (Datenaustauschkonzept gbXML/IFC 2.3 und 7.1 bis 7.5; Softwarearchitektur
    // Gebaeudesimulation 2.2, 2.4, 2.6, 2.7).
    //
    // WOZU. Die Paarung EPOS-Zeile <-> Quellentitaet muss den Zuordnungsdialog ueberleben:
    // Der Round-Trip (G7d) findet sonst nicht wieder, welcher IfcSpace zu welcher Zone gehoert,
    // ein zweiter Import derselben Datei erkennt nicht, was er schon zugeordnet hat, und die
    // Herkunft „aus Datei X vom Y" waere ohne die Quelldatei nur die halbe Auskunft.
    // Gespeichert wird je Lauf EINE Quelle (Tab_Importquelle) und je Paarung EINE Zeile
    // (Tab_Importzuordnung). Nur der Dateiname, nie der Pfad; SHA-256 des Inhalts,
    // hexadezimal klein, 64 Zeichen.
    //
    // BAUFORM. Kindliste am Projektgebaeude, STRICT, AUTOINCREMENT, CHECK (length(...)) statt
    // Typlaenge, ungeordnet (kein Rang). Keine der beiden Tabellen fuehrt ein ID_Projekt (W16):
    // Die Quelle haengt ueber ihr Gebaeude am Projekt, die Paarung ueber ihre Quelle. Aus fuenf
    // nullbaren Fremdschluesseln macht der CHECK „genau einer gesetzt" EINEN Zeiger - eine
    // polymorphe Kennung ohne Fremdschluessel waere ein Textverweis in Zahlenform.
    //
    // DIE KASKADE - ABWEICHUNG VON DEN PAPIEREN (Arbeitsentscheid des Orchestrators, G4c
    // Welle 3). Softwarearchitektur 2.2 und Datenaustauschkonzept 7.2 fuehren die fuenf
    // Zielverweise der Paarung (ID_Gebaeude, ID_Zone, ID_Bauteil, ID_Aufbau, ID_Baustoff)
    // „ohne Kaskade". Hier tragen sie ON DELETE CASCADE, und zwar aus drei Gruenden:
    //   1. Die Fremdschluessel sind im Betrieb SCHARF (PRAGMA foreign_keys = ON je Verbindung,
    //      SqliteDatenzugriff). Ohne Loeschregel (NO ACTION) scheiterte das Entfernen einer
    //      importierten Zone, eines Bauteils, Aufbaus oder Baustoffs durch den Anwender am
    //      Verweis der Paarung - der Zonendialog koennte nicht mehr speichern. SET NULL
    //      verletzte den CHECK „genau ein Ziel".
    //   2. Die Begruendung der Papiere - ein Speicherweg, der loescht und neu anlegt, raeumte
    //      die Paarung bei jedem gewoehnlichen Speichern ab (Kaskadenfalle, Kapitel 7) - traegt
    //      nicht mehr: GebaeudeZonenCtrl.SpeichernJeGebaeude gleicht ueber die Ids ab (Muster
    //      A6) und loescht nur, was der Anwender entfernt hat; die G3-Messung A1 hat ergeben,
    //      dass kein gewoehnlicher Speicherweg die Zeile in Tab_Gebaeude loescht und neu anlegt
    //      (Gebaeudeliste abgeglichen, Feld-Uebernahme zielgenau). Die G3-Sitzung hat das fuer
    //      SpeichernJeGebaeude bestaetigt.
    //   3. Eine Paarung ohne ihr Ziel hat keine Bedeutung - faellt das Ziel, faellt sie mit.
    // Die Quelle haengt wie in den Papieren mit Kaskade am Gebaeude, die Paarung mit Kaskade
    // an der Quelle. Gehalten von EPOS.Kern.Tests/GebaeudeImportCtrlTests (Probe 14 und die
    // drei Faelle des Arbeitsentscheids).
    //
    // ERGEBNISNEUTRAL. Kein Rechenweg liest die Tabellen (7.5), Import laeuft nur auf Zuruf;
    // keine Saat, kein Datenumbau, der Referenzlauf bleibt byte-gleich.
    // ====================================================================================

    /// <summary>
    /// <b>Die DDL der Herkunftsablage der Gebäudeimporte</b> — Schemaschritt S-F (Nummer
    /// <see cref="SCHRITT"/>). EINE Quelle für Migrationsschritt, <c>Werkzeuge/Testdatenbankschema</c>,
    /// Testvorrichtung, Kopierweg und Nachweis (ADR-001 Option C). Anlass, Bauform und die
    /// Abweichung bei der Kaskade stehen im Kopf der Datei.
    /// </summary>
    public static class ImportzuordnungSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht; Migration,
        /// Werkzeug, Zielstand und Tests verweisen hierher. Der Schritt folgt auf die
        /// Mehrzonenschritte S-A bis S-C (<see cref="ZonenSchema.SCHRITT"/>), auf deren Tabellen
        /// die Paarung zeigt, und auf die Kälteseite der Anlagenkopplung
        /// (<see cref="KuehluebergabeSchema.SCHRITT_ZONE"/>), mit der er keine Tabelle teilt. Wird er
        /// beim Zusammenführen umnummeriert, ändert sich nur diese Zahl.
        /// </summary>
        public const int SCHRITT = 138;

        // =================================================================
        //  Namen — sprachneutral und EINMAL
        // =================================================================

        /// <summary>Die Quellentabelle — eine Zeile je Importlauf.</summary>
        public const string TAB_QUELLE = SchemaKatalog.TAB_IMPORTQUELLE;

        /// <summary>Die Zuordnungstabelle — eine Zeile je Paarung.</summary>
        public const string TAB_ZUORDNUNG = SchemaKatalog.TAB_IMPORTZUORDNUNG;

        /// <summary>Index der Paarungen über <c>ID_Importquelle</c> — Leseweg je Quelle und Kaskade.</summary>
        public const string INDEX_ZUORDNUNG_QUELLE = "idx_Importzuordnung_Quelle";

        /// <summary>Index der Paarungen über <c>Quellkennung</c> — der Weg des Round-Trips zurück zur EPOS-Zeile.</summary>
        public const string INDEX_ZUORDNUNG_KENNUNG = "idx_Importzuordnung_Kennung";

        /// <summary>Größte Länge des Dateinamens (nur der Name, nie der Pfad).</summary>
        public const int DATEINAME_MAX = 260;

        /// <summary>Länge des SHA-256 hexadezimal.</summary>
        public const int HASH_LAENGE = 64;

        /// <summary>Größte Länge der Quellkennung (IFC-GlobalId 22 Zeichen, gbXML-<c>id</c> gekürzt, <see cref="Quellkennung"/>).</summary>
        public const int QUELLKENNUNG_MAX = 64;

        /// <summary>Größte Länge des Quelltyps (<c>IfcBuilding</c>, <c>Space</c>, <c>Surface</c> …).</summary>
        public const int QUELLTYP_MAX = 40;

        // ---- Tab_Importquelle -------------------------------------------------------

        /// <summary>Das Projektgebäude des Laufs, NOT NULL, <c>ON DELETE CASCADE</c>.</summary>
        public const string SPALTE_ID_GEBAEUDE = "ID_Gebaeude";

        /// <summary>Format (<see cref="DbWerte.IMPORT_FORMATE"/>), NOT NULL — Persistenzwert, kein Anzeigetext.</summary>
        public const string SPALTE_FORMAT = "Format";

        /// <summary>Nur der Dateiname, nie der Pfad; NOT NULL, höchstens <see cref="DATEINAME_MAX"/> Zeichen.</summary>
        public const string SPALTE_DATEINAME = "Dateiname";

        /// <summary>SHA-256 des Dateiinhalts, hexadezimal klein, genau <see cref="HASH_LAENGE"/> Zeichen, NOT NULL.</summary>
        public const string SPALTE_HASH = "Hash";

        /// <summary>Größe der Datei [Byte], NOT NULL.</summary>
        public const string SPALTE_GROESSE = "Groesse";

        /// <summary>
        /// Schema- bzw. Versionswert, wie gelesen (<c>IFC4</c>/<c>IFC2X3</c>/<c>IFC4X3</c> bzw. gbXML-<c>version</c>);
        /// NULL = keiner. Sperrkriterium des Round-Trips — nur <c>IFC4</c> lässt G7d zu.
        /// </summary>
        public const string SPALTE_SCHEMASTAND = "Schemastand";

        /// <summary>Zeitpunkt des Laufs, ISO 8601 invariant, NOT NULL.</summary>
        public const string SPALTE_ZEITPUNKT = "Zeitpunkt";

        /// <summary>EPOS-Programmfassung des Laufs; NULL = unbekannt.</summary>
        public const string SPALTE_PROGRAMMFASSUNG = "Programmfassung";

        /// <summary>Zonenregel des Laufs (<c>X1</c>…<c>X4</c> bzw. <c>Z1</c>…<c>Z5</c>); NULL = keine.</summary>
        public const string SPALTE_ZONENREGEL = "Zonenregel";

        /// <summary>Summe beider Verlustkanäle beim Lesen (IFC), NOT NULL DEFAULT 0; &gt; 0 sperrt den Round-Trip.</summary>
        public const string SPALTE_FEHLENDE_ENTITAETEN = "FehlendeEntitaeten";

        // ---- Tab_Importzuordnung ----------------------------------------------------

        /// <summary>Die Quelle der Paarung, NOT NULL, <c>ON DELETE CASCADE</c>.</summary>
        public const string SPALTE_ID_IMPORTQUELLE = "ID_Importquelle";

        /// <summary>Ziel Gebäude (→ <c>Tab_Gebaeude.ID</c>), nullbar, <c>ON DELETE CASCADE</c>.</summary>
        public const string SPALTE_ZIEL_GEBAEUDE = "ID_Gebaeude";

        /// <summary>Ziel Zone (→ <c>Tab_Zone.ID</c>), nullbar, <c>ON DELETE CASCADE</c>.</summary>
        public const string SPALTE_ZIEL_ZONE = "ID_Zone";

        /// <summary>Ziel Bauteil (→ <c>Tab_Bauteil.ID</c>), nullbar, <c>ON DELETE CASCADE</c>.</summary>
        public const string SPALTE_ZIEL_BAUTEIL = "ID_Bauteil";

        /// <summary>Ziel Aufbau (→ <c>Tab_Bauteilaufbau.ID</c>, Projektkopie), nullbar, <c>ON DELETE CASCADE</c>.</summary>
        public const string SPALTE_ZIEL_AUFBAU = "ID_Aufbau";

        /// <summary>Ziel Baustoff (→ <c>Tab_Baustoff.ID</c>, Projektkopie), nullbar, <c>ON DELETE CASCADE</c>.</summary>
        public const string SPALTE_ZIEL_BAUSTOFF = "ID_Baustoff";

        /// <summary><c>IfcGloballyUniqueId</c> bzw. gbXML-<c>id</c>, NOT NULL, höchstens <see cref="QUELLKENNUNG_MAX"/> Zeichen.</summary>
        public const string SPALTE_QUELLKENNUNG = "Quellkennung";

        /// <summary>Typ der Quellentität, NOT NULL, höchstens <see cref="QUELLTYP_MAX"/> Zeichen.</summary>
        public const string SPALTE_QUELLTYP = "Quelltyp";

        /// <summary>
        /// Die fünf Zielverweise in Schemareihenfolge samt Zieltabelle — die Reihenfolge der
        /// Aufzählung <c>ImportZiel</c> (Gebäude, Zone, Bauteil, Aufbau, Baustoff).
        /// </summary>
        public static readonly IReadOnlyList<KeyValuePair<string, string>> Zielverweise = new[]
        {
            new KeyValuePair<string, string>(SPALTE_ZIEL_GEBAEUDE, "Tab_Gebaeude"),
            new KeyValuePair<string, string>(SPALTE_ZIEL_ZONE, SchemaKatalog.TAB_ZONE),
            new KeyValuePair<string, string>(SPALTE_ZIEL_BAUTEIL, SchemaKatalog.TAB_BAUTEIL),
            new KeyValuePair<string, string>(SPALTE_ZIEL_AUFBAU, SchemaKatalog.TAB_BAUTEILAUFBAU),
            new KeyValuePair<string, string>(SPALTE_ZIEL_BAUSTOFF, SchemaKatalog.TAB_BAUSTOFF)
        };

        /// <summary>Die Wertliste des Formats als SQL-Literal (Quelle des <c>CHECK</c>).</summary>
        public const string WERTE_FORMAT = "'" + DbWerte.IMPORT_FORMAT_IFC + "','" + DbWerte.IMPORT_FORMAT_GBXML + "'";

        // =================================================================
        //  Die DDL
        // =================================================================

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Importquelle</c> — 11 Spalten, STRICT, Kaskade zum Gebäude.</summary>
        public const string SQL_CREATE_QUELLE =
            "CREATE TABLE IF NOT EXISTS \"Tab_Importquelle\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Gebaeude\" INTEGER NOT NULL,\n" +
            "    \"Format\" TEXT NOT NULL CHECK (\"Format\" IN (" + WERTE_FORMAT + ")),\n" +
            "    \"Dateiname\" TEXT NOT NULL CHECK (length(\"Dateiname\") <= 260),\n" +
            "    \"Hash\" TEXT NOT NULL CHECK (length(\"Hash\") = 64),\n" +
            "    \"Groesse\" INTEGER NOT NULL,\n" +
            "    \"Schemastand\" TEXT,\n" +
            "    \"Zeitpunkt\" TEXT NOT NULL,\n" +
            "    \"Programmfassung\" TEXT,\n" +
            "    \"Zonenregel\" TEXT,\n" +
            "    \"FehlendeEntitaeten\" INTEGER NOT NULL DEFAULT 0,\n" +
            "    FOREIGN KEY (\"ID_Gebaeude\") REFERENCES \"Tab_Gebaeude\" (\"ID\") ON DELETE CASCADE\n" +
            ") STRICT";

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_Importzuordnung</c> — 9 Spalten, STRICT, Kaskade zur
        /// Quelle und (abweichend von den Papieren, Kopf der Datei) zu jedem der fünf Ziele;
        /// der <c>CHECK</c> verlangt genau ein Ziel.
        /// </summary>
        public const string SQL_CREATE_ZUORDNUNG =
            "CREATE TABLE IF NOT EXISTS \"Tab_Importzuordnung\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Importquelle\" INTEGER NOT NULL,\n" +
            "    \"ID_Gebaeude\" INTEGER,\n" +
            "    \"ID_Zone\" INTEGER,\n" +
            "    \"ID_Bauteil\" INTEGER,\n" +
            "    \"ID_Aufbau\" INTEGER,\n" +
            "    \"ID_Baustoff\" INTEGER,\n" +
            "    \"Quellkennung\" TEXT NOT NULL CHECK (length(\"Quellkennung\") <= 64),\n" +
            "    \"Quelltyp\" TEXT NOT NULL CHECK (length(\"Quelltyp\") <= 40),\n" +
            "    CHECK ((\"ID_Gebaeude\" IS NOT NULL) + (\"ID_Zone\" IS NOT NULL) + (\"ID_Bauteil\" IS NOT NULL) +\n" +
            "           (\"ID_Aufbau\" IS NOT NULL) + (\"ID_Baustoff\" IS NOT NULL) = 1),\n" +
            "    FOREIGN KEY (\"ID_Importquelle\") REFERENCES \"Tab_Importquelle\" (\"ID\") ON DELETE CASCADE,\n" +
            "    FOREIGN KEY (\"ID_Gebaeude\") REFERENCES \"Tab_Gebaeude\" (\"ID\") ON DELETE CASCADE,\n" +
            "    FOREIGN KEY (\"ID_Zone\") REFERENCES \"Tab_Zone\" (\"ID\") ON DELETE CASCADE,\n" +
            "    FOREIGN KEY (\"ID_Bauteil\") REFERENCES \"Tab_Bauteil\" (\"ID\") ON DELETE CASCADE,\n" +
            "    FOREIGN KEY (\"ID_Aufbau\") REFERENCES \"Tab_Bauteilaufbau\" (\"ID\") ON DELETE CASCADE,\n" +
            "    FOREIGN KEY (\"ID_Baustoff\") REFERENCES \"Tab_Baustoff\" (\"ID\") ON DELETE CASCADE\n" +
            ") STRICT";

        /// <summary>Der Index der Paarungen je Quelle.</summary>
        public const string SQL_INDEX_ZUORDNUNG_QUELLE =
            "CREATE INDEX IF NOT EXISTS \"idx_Importzuordnung_Quelle\" ON \"Tab_Importzuordnung\" (\"ID_Importquelle\")";

        /// <summary>Der Index der Paarungen je Quellkennung (Round-Trip).</summary>
        public const string SQL_INDEX_ZUORDNUNG_KENNUNG =
            "CREATE INDEX IF NOT EXISTS \"idx_Importzuordnung_Kennung\" ON \"Tab_Importzuordnung\" (\"Quellkennung\")";

        /// <summary>Die zwei Tabellen in Anlegereihenfolge (Eltern vor Kind), je Tabellenname.</summary>
        public static IEnumerable<KeyValuePair<string, string>> Tabellenanweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(TAB_QUELLE, SQL_CREATE_QUELLE);
                yield return new KeyValuePair<string, string>(TAB_ZUORDNUNG, SQL_CREATE_ZUORDNUNG);
            }
        }

        /// <summary>Die zwei Indizes, je Indexname.</summary>
        public static IEnumerable<KeyValuePair<string, string>> Indexanweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(INDEX_ZUORDNUNG_QUELLE, SQL_INDEX_ZUORDNUNG_QUELLE);
                yield return new KeyValuePair<string, string>(INDEX_ZUORDNUNG_KENNUNG, SQL_INDEX_ZUORDNUNG_KENNUNG);
            }
        }

        /// <summary>Alle vier Anweisungen des Schritts S-F in der festen Handgriffreihenfolge (R2) — Tabellen, dann Indizes.</summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
            => Tabellenanweisungen.Concat(Indexanweisungen);

        /// <summary>
        /// Die Spalten der Quelle in Schemareihenfolge, ohne <c>ID</c> — die EINE Liste, an der
        /// Lesen und Schreiben des <c>GebaeudeImportCtrl</c> hängen.
        /// </summary>
        public static readonly IReadOnlyList<string> Quellspalten = new[]
        {
            SPALTE_ID_GEBAEUDE, SPALTE_FORMAT, SPALTE_DATEINAME, SPALTE_HASH, SPALTE_GROESSE,
            SPALTE_SCHEMASTAND, SPALTE_ZEITPUNKT, SPALTE_PROGRAMMFASSUNG, SPALTE_ZONENREGEL,
            SPALTE_FEHLENDE_ENTITAETEN
        };

        /// <summary>Die Spalten der Paarung in Schemareihenfolge, ohne <c>ID</c>.</summary>
        public static readonly IReadOnlyList<string> Zuordnungsspalten = new[]
        {
            SPALTE_ID_IMPORTQUELLE, SPALTE_ZIEL_GEBAEUDE, SPALTE_ZIEL_ZONE, SPALTE_ZIEL_BAUTEIL,
            SPALTE_ZIEL_AUFBAU, SPALTE_ZIEL_BAUSTOFF, SPALTE_QUELLKENNUNG, SPALTE_QUELLTYP
        };

        /// <summary>Stehen beide Tabellen und beide Indizes?</summary>
        public static bool Vollstaendig()
        {
            foreach (KeyValuePair<string, string> a in Tabellenanweisungen)
                if (!DataRepository.TabelleVorhanden(a.Key)) return false;
            foreach (KeyValuePair<string, string> a in Indexanweisungen)
            {
                object n = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = ?", new DbParam("@i", a.Key));
                if (n == null || System.Convert.ToInt64(n, System.Globalization.CultureInfo.InvariantCulture) == 0)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Führt den Schritt in EINEM Zug aus — für <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>. <b>Wiederholbar</b> (<c>IF NOT EXISTS</c>), <b>kein DML</b>.
        /// </summary>
        /// <returns>Zahl der in diesem Lauf angelegten Tabellen (0 bis 2).</returns>
        public static int Ausfuehren()
        {
            int angelegt = 0;
            foreach (KeyValuePair<string, string> a in Tabellenanweisungen)
            {
                bool vorher = DataRepository.TabelleVorhanden(a.Key);
                DataRepository.ExecuteNonQuery(a.Value);
                if (!vorher && DataRepository.TabelleVorhanden(a.Key)) angelegt++;
            }
            foreach (KeyValuePair<string, string> a in Indexanweisungen)
                DataRepository.ExecuteNonQuery(a.Value);
            return angelegt;
        }
    }
}
