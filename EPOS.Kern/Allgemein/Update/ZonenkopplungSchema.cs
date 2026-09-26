using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // ZONENKOPPLUNG - Schemaschritt S-G der Gebaeudesimulation, Stufe G6b (Mehrzonenkonzept
    // 4.2 und 4.4; Auftrag G6b, Welle W1; Anwenderentscheide A1 = M3 (b) und A6 vom 26.09.2026).
    //
    // WOZU. Ab zwei Zonen rechnet EPOS die Zonen gekoppelt (ADR-005): Eine Trennflaeche zwischen
    // zwei Zonen ist GENAU EIN Bauteil mit Randbedingung ZONE und einem Verweis auf die
    // Nachbarzone (keine Tab_Zonenkopplung - eine zweite Tabelle waere eine zweite Wahrheit
    // ueber dieselbe Flaeche, Mehrzonenkonzept 4.2); der Luftaustausch zwischen zwei Zonen ist
    // ein PAAR mit einem Volumenstrom. Dazu das Ergebnis je Zone als Skalare (A6), damit der
    // Bericht nach E30 nur Gespeichertes liest.
    //
    // EIN SCHRITT (Mehrzonenkonzept 4.2/4.4): Die Testdatenbank wird nur einmal angefasst.
    //   - Tab_Bauteil.ID_Nachbarzone INTEGER REFERENCES Tab_Zone(ID), OHNE Loeschregel (NO
    //     ACTION). RESTRICT scheiterte an der Kaskade beim Loeschen eines Gebaeudes (Gebaeude ->
    //     Zone -> Bauteil faellt in EINER Anweisung; NO ACTION prueft erst an deren Ende).
    //     Dass keine Trennflaeche auf eine entfernte Zone zeigt, haelt die Regelklasse
    //     (Zonenkopplungsregeln) vor dem Schreiben.
    //   - Tab_Bauteil.Trennflaeche_Zuordnung TEXT NULL CHECK IN ('IW','AW') - die Uebersteuerung
    //     der 4-K-Regel je Trennflaeche (M3 = (b)); NULL heisst 4-K-Regel.
    //   - idx_Bauteil_Nachbarzone (der Index auf ID_Zone steht seit S-C, ZonenSchema).
    //   - Tab_Zonenluftstrom STRICT: ID_ZoneA und ID_ZoneB mit Kaskade, Volumenstrom > 0,
    //     CHECK (ID_ZoneA < ID_ZoneB) und EIN eindeutiger Index auf (A, B): der CHECK normiert die
    //     Richtung, der Index erzwingt eine Zeile je Paar - erst beides zusammen traegt die
    //     Massenbilanz von selbst (Mehrzonenkonzept 4.2). Dazu ein Index auf B fuer die Kaskade.
    //   - Tab_ErgebnisZone STRICT, nur Skalare, nach dem Muster Tab_ErgebnisGebaeude (Schritt
    //     107): der Kopf ueber ID_ErgebnisGebaeude mit Kaskade, die Zone mit ON DELETE SET NULL
    //     (das Ergebnis eines Laufs bleibt, auch wenn die Zone danach geloescht wird), dazu Rang,
    //     Name und IstBeheizt zum Zeitpunkt des Laufs. Die Einheit steht im Spaltennamen.
    //
    // ERGEBNISNEUTRAL, solange kein Rechenweg liest: Die Spalten bleiben NULL, die Tabellen
    // leer; die Testdatenbank fuehrt keine Zone (Referenzlauf byte-gleich, keine neue Basis).
    // ====================================================================================

    /// <summary>
    /// <b>Die DDL der Zonenkopplung</b> — Schemaschritt S-G (Nummer <see cref="SCHRITT"/>). EINE
    /// Quelle für Migrationsschritt, <c>Werkzeuge/Testdatenbankschema</c>, Testvorrichtung, Kopierweg
    /// und Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class ZonenkopplungSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht; Migration,
        /// Werkzeug, Zielstand und Tests verweisen hierher. Vergeben beim Merge mit origin am
        /// 26.09.2026 (146 trägt der Namensabgleich der Baustoffe). Wird er umnummeriert, ändert
        /// sich nur diese Zeile.
        /// </summary>
        public const int SCHRITT = 147;

        // =================================================================
        //  Namen — sprachneutral und EINMAL
        // =================================================================

        /// <summary>Die Tabelle der Luftströme zwischen zwei Zonen.</summary>
        public const string TAB_LUFTSTROM = SchemaKatalog.TAB_ZONENLUFTSTROM;

        /// <summary>Die Ergebnistabelle je Zone.</summary>
        public const string TAB_ERGEBNIS = SchemaKatalog.TAB_ERGEBNISZONE;

        /// <summary>Die Nachbarzone einer Trennfläche (→ <c>Tab_Zone.ID</c>, ohne Löschregel); nur bei <c>Randbedingung = 'ZONE'</c>.</summary>
        public const string SPALTE_ID_NACHBARZONE = "ID_Nachbarzone";

        /// <summary>
        /// Die Zuordnung einer Trennfläche zur Gruppe (<see cref="DbWerte.TRENNFLAECHE_IW"/>,
        /// <see cref="DbWerte.TRENNFLAECHE_AW"/>); <b>NULL heißt 4-K-Regel</b> (A1 = M3 (b)).
        /// </summary>
        public const string SPALTE_TRENNFLAECHE_ZUORDNUNG = "Trennflaeche_Zuordnung";

        /// <summary>Zone A des Paares (die kleinere Id), NOT NULL, <c>ON DELETE CASCADE</c>.</summary>
        public const string SPALTE_ID_ZONE_A = "ID_ZoneA";

        /// <summary>Zone B des Paares (die größere Id), NOT NULL, <c>ON DELETE CASCADE</c>.</summary>
        public const string SPALTE_ID_ZONE_B = "ID_ZoneB";

        /// <summary>Der Volumenstrom des Paares [m³/h], NOT NULL, größer null; der Gegenstrom ist gleich groß.</summary>
        public const string SPALTE_VOLUMENSTROM = "Volumenstrom";

        /// <summary>Das Gebäudeergebnis des Laufs (→ <c>Tab_ErgebnisGebaeude.ID</c>), <c>ON DELETE CASCADE</c>.</summary>
        public const string SPALTE_ID_ERGEBNIS_GEBAEUDE = "ID_ErgebnisGebaeude";

        /// <summary>Index auf <c>Tab_Bauteil.ID_Nachbarzone</c> — trägt die Prüfung beim Löschen einer Zone.</summary>
        public const string INDEX_NACHBARZONE = "idx_Bauteil_Nachbarzone";

        /// <summary>Der eindeutige Index auf (<c>ID_ZoneA</c>, <c>ID_ZoneB</c>) — eine Zeile je Paar.</summary>
        public const string INDEX_LUFTSTROM = "idx_Zonenluftstrom";

        /// <summary>Index auf <c>ID_ZoneB</c> — trägt die Kaskade der zweiten Zone.</summary>
        public const string INDEX_LUFTSTROM_B = "idx_Zonenluftstrom_ZoneB";

        /// <summary>Index auf dem Kopfverweis des Zonenergebnisses.</summary>
        public const string INDEX_ERGEBNIS_GEBAEUDE = "idx_ErgebnisZone_ErgebnisGebaeude";

        /// <summary>Index auf dem Zonenverweis des Zonenergebnisses — trägt das SET NULL beim Löschen einer Zone.</summary>
        public const string INDEX_ERGEBNIS_ZONE = "idx_ErgebnisZone_Zone";

        /// <summary>Die Wertliste der Trennflächenzuordnung als SQL-Literal (Quelle des <c>CHECK</c>).</summary>
        public const string WERTE_TRENNFLAECHE =
            "'" + DbWerte.TRENNFLAECHE_IW + "','" + DbWerte.TRENNFLAECHE_AW + "'";

        // =================================================================
        //  Die DDL
        // =================================================================

        /// <summary>
        /// <b>Die zwei Spalten an <c>Tab_Bauteil</c></b> in Anlegereihenfolge: Name und
        /// STRICT-Definition, nullbar, ohne Vorgabe und ohne Nachtrag (<c>ALTER TABLE … ADD COLUMN</c>
        /// mit <c>REFERENCES</c> verlangt die Vorgabe NULL).
        /// </summary>
        public static readonly IReadOnlyList<KeyValuePair<string, string>> SpaltenBauteil = new[]
        {
            new KeyValuePair<string, string>(SPALTE_ID_NACHBARZONE,
                "INTEGER REFERENCES \"" + SchemaKatalog.TAB_ZONE + "\" (\"ID\")"),
            new KeyValuePair<string, string>(SPALTE_TRENNFLAECHE_ZUORDNUNG,
                "TEXT CHECK (\"" + SPALTE_TRENNFLAECHE_ZUORDNUNG + "\" IN (" + WERTE_TRENNFLAECHE + "))"),
        };

        /// <summary>Die Anweisung, die eine Spalte von <see cref="SpaltenBauteil"/> anlegt.</summary>
        public static string SpalteAnlegen(KeyValuePair<string, string> spalte)
            => "ALTER TABLE \"" + SchemaKatalog.TAB_BAUTEIL + "\" ADD COLUMN \"" + spalte.Key + "\" " + spalte.Value;

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Zonenluftstrom</c> — vier Spalten, STRICT, Kaskade zu beiden Zonen.</summary>
        public const string SQL_CREATE_LUFTSTROM =
            "CREATE TABLE IF NOT EXISTS \"Tab_Zonenluftstrom\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_ZoneA\" INTEGER NOT NULL,\n" +
            "    \"ID_ZoneB\" INTEGER NOT NULL,\n" +
            "    \"Volumenstrom\" REAL NOT NULL CHECK (\"Volumenstrom\" > 0),\n" +
            "    CHECK (\"ID_ZoneA\" < \"ID_ZoneB\"),\n" +
            "    FOREIGN KEY (\"ID_ZoneA\") REFERENCES \"Tab_Zone\" (\"ID\") ON DELETE CASCADE,\n" +
            "    FOREIGN KEY (\"ID_ZoneB\") REFERENCES \"Tab_Zone\" (\"ID\") ON DELETE CASCADE\n" +
            ") STRICT";

        /// <summary>Spaltenzahl von <c>Tab_Zonenluftstrom</c> (Nachweis in den Tests).</summary>
        public const int SPALTENZAHL_LUFTSTROM = 4;

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_ErgebnisZone</c> — 14 Spalten, STRICT, nur Skalare.
        /// <b>NULL heißt „nicht gerechnet"</b>: die Energiespalten einer unbeheizten Zone (A2), die
        /// Kühlenergie ohne wirksame Kühlung, Δϑ_max einer Zone ohne Nachbarzone.
        /// </summary>
        public const string SQL_CREATE_ERGEBNIS =
            "CREATE TABLE IF NOT EXISTS \"Tab_ErgebnisZone\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY,\n" +
            "    \"ID_ErgebnisGebaeude\" INTEGER NOT NULL REFERENCES \"Tab_ErgebnisGebaeude\" (\"ID\") ON DELETE CASCADE,\n" +
            "    \"ID_Zone\" INTEGER REFERENCES \"Tab_Zone\" (\"ID\") ON DELETE SET NULL,\n" +
            "    \"Rang\" INTEGER NOT NULL CHECK (\"Rang\" >= 1),\n" +
            "    \"Bezeichner\" TEXT NOT NULL CHECK (length(\"Bezeichner\") <= 80),\n" +
            "    \"IstBeheizt\" INTEGER NOT NULL CHECK (\"IstBeheizt\" IN (0,1)),\n" +
            "    \"Heizwaerme_Mwh\" REAL,\n" +
            "    \"Spitze_Kw\" REAL,\n" +
            "    \"Kuehlenergie_Mwh\" REAL,\n" +
            "    \"MittlereRaumtemperatur_C\" REAL,\n" +
            "    \"Ueberhitzungsstunden_H\" INTEGER CHECK (\"Ueberhitzungsstunden_H\" BETWEEN 0 AND 8760),\n" +
            "    \"DeltaThetaMax_K\" REAL,\n" +
            "    \"DurchlaeufeMax\" INTEGER CHECK (\"DurchlaeufeMax\" >= 1),\n" +
            "    \"Musterwechsel_H\" INTEGER CHECK (\"Musterwechsel_H\" BETWEEN 0 AND 8760)\n" +
            ") STRICT";

        /// <summary>Spaltenzahl von <c>Tab_ErgebnisZone</c> (Nachweis in den Tests).</summary>
        public const int SPALTENZAHL_ERGEBNIS = 14;

        /// <summary>Die Tabellen des Schritts in Anlegereihenfolge, je Tabellenname.</summary>
        public static IEnumerable<KeyValuePair<string, string>> Tabellenanweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(TAB_LUFTSTROM, SQL_CREATE_LUFTSTROM);
                yield return new KeyValuePair<string, string>(TAB_ERGEBNIS, SQL_CREATE_ERGEBNIS);
            }
        }

        /// <summary>Die fünf Indizes des Schritts, je Indexname — nach den Spalten und Tabellen anzulegen.</summary>
        public static IEnumerable<KeyValuePair<string, string>> Indexanweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(INDEX_NACHBARZONE,
                    "CREATE INDEX IF NOT EXISTS \"" + INDEX_NACHBARZONE + "\" ON \"" + SchemaKatalog.TAB_BAUTEIL +
                    "\" (\"" + SPALTE_ID_NACHBARZONE + "\")");
                yield return new KeyValuePair<string, string>(INDEX_LUFTSTROM,
                    "CREATE UNIQUE INDEX IF NOT EXISTS \"" + INDEX_LUFTSTROM + "\" ON \"" + TAB_LUFTSTROM +
                    "\" (\"" + SPALTE_ID_ZONE_A + "\", \"" + SPALTE_ID_ZONE_B + "\")");
                yield return new KeyValuePair<string, string>(INDEX_LUFTSTROM_B,
                    "CREATE INDEX IF NOT EXISTS \"" + INDEX_LUFTSTROM_B + "\" ON \"" + TAB_LUFTSTROM +
                    "\" (\"" + SPALTE_ID_ZONE_B + "\")");
                yield return new KeyValuePair<string, string>(INDEX_ERGEBNIS_GEBAEUDE,
                    "CREATE INDEX IF NOT EXISTS \"" + INDEX_ERGEBNIS_GEBAEUDE + "\" ON \"" + TAB_ERGEBNIS +
                    "\" (\"" + SPALTE_ID_ERGEBNIS_GEBAEUDE + "\")");
                yield return new KeyValuePair<string, string>(INDEX_ERGEBNIS_ZONE,
                    "CREATE INDEX IF NOT EXISTS \"" + INDEX_ERGEBNIS_ZONE + "\" ON \"" + TAB_ERGEBNIS +
                    "\" (\"ID_Zone\")");
            }
        }

        // =================================================================
        //  Stand und Ausführung
        // =================================================================

        /// <summary>
        /// Kann der Datenweg den Schritt nutzen? Beide Spalten an <c>Tab_Bauteil</c> und beide
        /// Tabellen — die Probe des Zonenlesers (<see cref="GebaeudeZonenanschluss.KopplungVorhanden"/>);
        /// die Indizes ändern kein Lesen und Schreiben.
        /// </summary>
        public static bool Lesbar()
        {
            if (!DataRepository.TabelleVorhanden(SchemaKatalog.TAB_BAUTEIL)) return false;
            foreach (KeyValuePair<string, string> s in SpaltenBauteil)
                if (!DataRepository.SpalteVorhanden(SchemaKatalog.TAB_BAUTEIL, s.Key)) return false;
            foreach (KeyValuePair<string, string> a in Tabellenanweisungen)
                if (!DataRepository.TabelleVorhanden(a.Key)) return false;
            return true;
        }

        /// <summary>
        /// Steht der Schritt? Beide Spalten an <c>Tab_Bauteil</c>, beide Tabellen und alle fünf
        /// Indizes.
        /// </summary>
        public static bool Vollstaendig()
        {
            if (!Lesbar()) return false;
            foreach (KeyValuePair<string, string> a in Indexanweisungen)
            {
                object n = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = ?", new DbParam("@i", a.Key));
                if (n == null || System.Convert.ToInt64(n, CultureInfo.InvariantCulture) == 0) return false;
            }
            return true;
        }

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang aus — für <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>; die Migration der Schale geht denselben Weg über ihre eigenen
        /// Helfer. <b>Wiederholbar</b> (vorhandene Spalte = nichts zu tun, <c>IF NOT EXISTS</c>),
        /// <b>kein DML</b>. Ohne <c>Tab_Zone</c>/<c>Tab_Bauteil</c> (Stand vor S-C) oder ohne
        /// <c>Tab_ErgebnisGebaeude</c> (Stand vor 107) tut er nichts und sagt es.
        /// </summary>
        /// <param name="bericht">Nimmt Zeilen auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten und Tabellen (höchstens vier).</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen Verbindung
            // und saehe die offene Transaktion nicht.
            if (!DataRepository.TabelleVorhanden(SchemaKatalog.TAB_ZONE) || !DataRepository.TabelleVorhanden(SchemaKatalog.TAB_BAUTEIL))
            {
                bericht?.Add(SchemaKatalog.TAB_ZONE + "/" + SchemaKatalog.TAB_BAUTEIL + " fehlen (Stand vor Schritt " +
                             ZonenSchema.SCHRITT.ToString(CultureInfo.InvariantCulture) + ") - nichts angelegt");
                return 0;
            }
            if (!DataRepository.TabelleVorhanden(ErgebnisGebaeudeSchema.TAB))
            {
                bericht?.Add(ErgebnisGebaeudeSchema.TAB + " fehlt (Stand vor Schritt 107) - nichts angelegt");
                return 0;
            }
            List<KeyValuePair<string, string>> spalten = SpaltenBauteil
                .Where(s => !DataRepository.SpalteVorhanden(SchemaKatalog.TAB_BAUTEIL, s.Key)).ToList();
            List<KeyValuePair<string, string>> tabellen = Tabellenanweisungen
                .Where(a => !DataRepository.TabelleVorhanden(a.Key)).ToList();

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach (KeyValuePair<string, string> s in spalten) v.Ausfuehren(SpalteAnlegen(s));
                    foreach (KeyValuePair<string, string> a in Tabellenanweisungen) v.Ausfuehren(a.Value);
                    foreach (KeyValuePair<string, string> a in Indexanweisungen) v.Ausfuehren(a.Value);
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            // Der Zonenleser hat sich „keine Kopplung" gemerkt, falls er vorher fragte.
            GebaeudeZonenanschluss.ProbeVerwerfen();

            bericht?.Add(spalten.Count.ToString(CultureInfo.InvariantCulture) + " von " +
                         SpaltenBauteil.Count.ToString(CultureInfo.InvariantCulture) + " Spalte(n) an " +
                         SchemaKatalog.TAB_BAUTEIL + " angelegt (" + SPALTE_ID_NACHBARZONE + ", " +
                         SPALTE_TRENNFLAECHE_ZUORDNUNG + "), " + tabellen.Count.ToString(CultureInfo.InvariantCulture) +
                         " von 2 Tabelle(n) angelegt (" + TAB_LUFTSTROM + ", " + TAB_ERGEBNIS + "), fünf Indizes");
            return spalten.Count + tabellen.Count;
        }
    }
}
