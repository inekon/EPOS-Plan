using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // HC-5 - GRUNDRISS JE RAUM AUS DEM DATEIKOERPER (Schritt 191; Entscheid E94; Konzept HottCAD-Verbund
    // Kapitel 11.3).
    //
    // WOZU. Ein importiertes Gebaeude fuehrt in der Datenbank keine Geometrie; Export und Exportmodell zeigen
    // jeden Raum als Rechteck. Der Schritt legt EINE Kindliste der Importquelle an, die je Raum der Datei den
    // Grundriss (Ringe in ganzen Millimetern, absolut im Modellsystem), die Hoehenlage des Bodens, die Hoehe
    // des Prismas, die Herleitung und die Vermerke traegt - wenige Kilobyte je Gebaeude:
    //
    //   Tab_Raumgrundriss  ID                INTEGER PK AUTOINCREMENT
    //                      ID_Importquelle   INTEGER NOT NULL  -> Tab_Importquelle.ID  ON DELETE CASCADE
    //                      ID_Zone           INTEGER NULL      -> Tab_Zone.ID          ON DELETE SET NULL
    //                      Quellkennung      TEXT NOT NULL     1..64 Zeichen (Wiedererkennung, keine Beziehung)
    //                      Raumname, Geschoss TEXT NULL        <= 200 Zeichen
    //                      Geschoss_Lage_m   REAL NULL
    //                      Boden_m           REAL NOT NULL
    //                      Hoehe_m           REAL NOT NULL     > 0
    //                      Ringe             TEXT NOT NULL     "x,y;x,y;...|..." ganze Millimeter
    //                      Ringflaeche_m2    REAL NOT NULL     > 0
    //                      Abweichung        REAL NULL         (Ringflaeche - Raumflaeche) / Raumflaeche
    //                      Herleitung        TEXT NOT NULL     KoerperBoden, KoerperDecke, KoerperHuelle, Boden, Decke
    //                      Vermerke          TEXT NULL         sprachneutrale Schluessel mit Komma, <= 200 Zeichen
    //   UNIQUE (ID_Importquelle, Quellkennung); Index idx_Raumgrundriss_Zone ueber ID_Zone.
    //
    // KEIN ID_Projekt (W16 wie die Herkunftsablage), keine Boolean-Spalte, keine Beziehung ueber Text.
    //
    // DIE KETTE. Schritt 190 (NP5, Sitzung Gebaeudesimulation) ist angemeldet, lag beim Bau aber nicht auf
    // origin. Die Nummer haengt deshalb VORLAEUFIG ueber RaumnutzungSchema.SCHRITT + 2 an 189; beim
    // Zusammenfuehren mit Schritt 190 wird diese EINE Zeile auf "<Klasse von 190>.SCHRITT + 1" umgehaengt.
    //
    // KEIN DML, KEINE SAAT. Kein Rechenweg liest die Tabelle (Konzept 11.5); die Testdatenbank bekommt sie
    // leer, der Referenzlauf bleibt byte-gleich. Wiederholbar (IF NOT EXISTS).
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>Die DDL der Grundrisse je importiertem Raum</b> — Schemaschritt <see cref="SCHRITT"/> (HC-5). EINE Quelle für
    /// Migrationsschritt, <c>Werkzeuge/Testdatenbankschema</c>, Testvorrichtung und Nachweis (ADR-001 Option C).
    /// Anlass, Bauform und die Kette stehen im Kopf der Datei.
    /// </summary>
    public static class RaumgrundrissSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht. Vorläufig über
        /// <see cref="RaumnutzungSchema.SCHRITT"/> + 2, weil Schritt 190 (NP5) angemeldet, aber noch nicht gebaut ist;
        /// beim Zusammenführen auf „Klasse von 190“.SCHRITT + 1 umhängen (Kopf der Datei).
        /// </summary>
        public const int SCHRITT = RaumnutzungSchema.SCHRITT + 2; // Kette: beim Merge auf <Klasse von 190>.SCHRITT + 1 umhängen

        // =================================================================
        //  Namen — sprachneutral und EINMAL
        // =================================================================

        /// <summary>Die Tabelle — eine Zeile je Raum und Importquelle.</summary>
        public const string TAB = SchemaKatalog.TAB_RAUMGRUNDRISS;

        /// <summary>Der Index über <c>ID_Zone</c> — Weg der Löschregel und des Lesens je Zone.</summary>
        public const string INDEX_ZONE = "idx_Raumgrundriss_Zone";

        /// <summary>Größte Länge der Quellkennung (wie <see cref="ImportzuordnungSchema.QUELLKENNUNG_MAX"/>).</summary>
        public const int QUELLKENNUNG_MAX = ImportzuordnungSchema.QUELLKENNUNG_MAX;

        /// <summary>Größte Länge von Raumname und Geschoss.</summary>
        public const int NAME_MAX = 200;

        /// <summary>Kleinste Länge der Ringe (drei Punkte „0,0;1,0;0,1“).</summary>
        public const int RINGE_MIN = 11;

        /// <summary>Größte Länge der Ringe.</summary>
        public const int RINGE_MAX = 65536;

        /// <summary>Größte Länge der Vermerke.</summary>
        public const int VERMERKE_MAX = 200;

        public const string SPALTE_ID_IMPORTQUELLE = "ID_Importquelle";
        public const string SPALTE_ID_ZONE = "ID_Zone";
        public const string SPALTE_QUELLKENNUNG = "Quellkennung";
        public const string SPALTE_RAUMNAME = "Raumname";
        public const string SPALTE_GESCHOSS = "Geschoss";
        public const string SPALTE_GESCHOSS_LAGE = "Geschoss_Lage_m";
        public const string SPALTE_BODEN = "Boden_m";
        public const string SPALTE_HOEHE = "Hoehe_m";
        public const string SPALTE_RINGE = "Ringe";
        public const string SPALTE_RINGFLAECHE = "Ringflaeche_m2";
        public const string SPALTE_ABWEICHUNG = "Abweichung";
        public const string SPALTE_HERLEITUNG = "Herleitung";
        public const string SPALTE_VERMERKE = "Vermerke";

        /// <summary>Die Spalten in Schemareihenfolge, ohne <c>ID</c> — die EINE Liste, an der Lesen und Schreiben hängen.</summary>
        public static readonly IReadOnlyList<string> Spalten = new[]
        {
            SPALTE_ID_IMPORTQUELLE, SPALTE_ID_ZONE, SPALTE_QUELLKENNUNG, SPALTE_RAUMNAME, SPALTE_GESCHOSS,
            SPALTE_GESCHOSS_LAGE, SPALTE_BODEN, SPALTE_HOEHE, SPALTE_RINGE, SPALTE_RINGFLAECHE, SPALTE_ABWEICHUNG,
            SPALTE_HERLEITUNG, SPALTE_VERMERKE
        };

        /// <summary>
        /// Die zulässigen Werte der Herleitung — die Namen von <see cref="Umrissherleitung"/>; <c>Boden</c> und
        /// <c>Decke</c> für Umrisse aus Raumgrenzen bzw. gbXML (F6).
        /// </summary>
        public static readonly IReadOnlyList<string> HERLEITUNGEN = new[]
        {
            nameof(Umrissherleitung.KoerperBoden), nameof(Umrissherleitung.KoerperDecke), nameof(Umrissherleitung.KoerperHuelle),
            nameof(Umrissherleitung.Boden), nameof(Umrissherleitung.Decke)
        };

        private static string WerteHerleitung => string.Join(",", HERLEITUNGEN.Select(h => "'" + h + "'"));

        // =================================================================
        //  Die DDL
        // =================================================================

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Raumgrundriss</c> — 14 Spalten, STRICT, Kaskade zur Quelle, SET NULL zur Zone.</summary>
        public static string SQL_CREATE =>
            "CREATE TABLE IF NOT EXISTS \"Tab_Raumgrundriss\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Importquelle\" INTEGER NOT NULL,\n" +
            "    \"ID_Zone\" INTEGER,\n" +
            "    \"Quellkennung\" TEXT NOT NULL CHECK (length(\"Quellkennung\") BETWEEN 1 AND 64),\n" +
            "    \"Raumname\" TEXT CHECK (\"Raumname\" IS NULL OR length(\"Raumname\") <= 200),\n" +
            "    \"Geschoss\" TEXT CHECK (\"Geschoss\" IS NULL OR length(\"Geschoss\") <= 200),\n" +
            "    \"Geschoss_Lage_m\" REAL,\n" +
            "    \"Boden_m\" REAL NOT NULL,\n" +
            "    \"Hoehe_m\" REAL NOT NULL CHECK (\"Hoehe_m\" > 0),\n" +
            "    \"Ringe\" TEXT NOT NULL CHECK (length(\"Ringe\") BETWEEN 11 AND 65536 AND \"Ringe\" NOT GLOB '*[^0-9,;|-]*'),\n" +
            "    \"Ringflaeche_m2\" REAL NOT NULL CHECK (\"Ringflaeche_m2\" > 0),\n" +
            "    \"Abweichung\" REAL,\n" +
            "    \"Herleitung\" TEXT NOT NULL CHECK (\"Herleitung\" IN (" + WerteHerleitung + ")),\n" +
            "    \"Vermerke\" TEXT CHECK (\"Vermerke\" IS NULL OR length(\"Vermerke\") <= 200),\n" +
            "    UNIQUE (\"ID_Importquelle\", \"Quellkennung\"),\n" +
            "    FOREIGN KEY (\"ID_Importquelle\") REFERENCES \"Tab_Importquelle\" (\"ID\") ON DELETE CASCADE,\n" +
            "    FOREIGN KEY (\"ID_Zone\") REFERENCES \"Tab_Zone\" (\"ID\") ON DELETE SET NULL\n" +
            ") STRICT";

        /// <summary>Der Index über <c>ID_Zone</c>.</summary>
        public const string SQL_INDEX_ZONE =
            "CREATE INDEX IF NOT EXISTS \"idx_Raumgrundriss_Zone\" ON \"Tab_Raumgrundriss\" (\"ID_Zone\")";

        /// <summary>Die Anweisungen in fester Reihenfolge — Tabelle, dann Index.</summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(TAB, SQL_CREATE);
                yield return new KeyValuePair<string, string>(INDEX_ZONE, SQL_INDEX_ZONE);
            }
        }

        /// <summary>Die Tabellen, die vorher stehen müssen (Eltern der Fremdschlüssel).</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { ImportzuordnungSchema.TAB_QUELLE, SchemaKatalog.TAB_ZONE };

        /// <summary>Stehen Tabelle und Index?</summary>
        public static bool Vollstaendig()
        {
            if (!DataRepository.TabelleVorhanden(TAB)) return false;
            object n = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = ?", new DbParam("@i", INDEX_ZONE));
            return n != null && n != DBNull.Value && Convert.ToInt64(n, CultureInfo.InvariantCulture) > 0;
        }

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang aus — für Migration, <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>. <b>Wiederholbar</b>, <b>kein DML</b>.
        /// </summary>
        /// <returns>Zahl der in diesem Lauf angelegten Tabellen (0 oder 1).</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            if (Vollstaendig())
            {
                bericht?.Add("steht bereits - Tab_Raumgrundriss samt Index; nichts zu tun");
                return 0;
            }

            bool vorher = DataRepository.TabelleVorhanden(TAB);
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach (KeyValuePair<string, string> a in Anweisungen) v.Ausfuehren(a.Value);
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            int angelegt = !vorher && DataRepository.TabelleVorhanden(TAB) ? 1 : 0;
            bericht?.Add(angelegt == 1 ? "Tab_Raumgrundriss angelegt (leer), Index idx_Raumgrundriss_Zone"
                                       : "Index idx_Raumgrundriss_Zone nachgezogen");
            bericht?.Add("KEIN DML an Bestandsdaten, der Referenzlauf bleibt byte-gleich");
            return angelegt;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
