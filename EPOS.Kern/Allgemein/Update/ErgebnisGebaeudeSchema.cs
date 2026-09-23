using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Ergebnistabelle je Gebäude</b> — Schemaschritt 106 (Entscheid E30 vom 23.09.2026,
    /// Konzept Gebäudesimulation N1.35): Der Simulationslauf schreibt je Gebäude eine Zeile mit
    /// Rechenweg und Kennzahlen, der Bericht liest sie und rechnet nichts nach.
    ///
    /// <para><b>Eine Quelle für Migration, Testdatenbank und Nachweis</b> — Muster
    /// <see cref="TwwSchema"/>: <c>SchemaMigration</c> legt die Tabelle beim Programmstart an,
    /// <c>Werkzeuge/Testdatenbankschema</c> beim Nachziehen der Messlatte, und
    /// <c>ErgebnisGebaeudeSchemaTests</c> prüft genau diese Anweisungen.</para>
    ///
    /// <para><b>Bauform der Ergebnistabellen</b> (wie <c>Tab_ErgebnisStromspeicher</c>):
    /// <c>ID INTEGER PRIMARY KEY</c> mit Vergabe MAX+1 durch <c>ErgebnisCtrl</c>, der Kopf über
    /// <c>ID_Ergebnis</c> mit Löschweitergabe — der nächste Lauf ersetzt die Zeilen, wie alle
    /// Ergebnistabellen. Dazu die Beziehung auf die Gebäudezeile des Projekts
    /// (<c>Tab_Gebaeude.ID</c>), ebenfalls mit Löschweitergabe: Ein gelöschtes Gebäude hat kein
    /// Ergebnis mehr. STRICT; die Einheit steht im Spaltennamen (Einheitenregel 3 des Kerns).</para>
    ///
    /// <para><b>NULL heißt „auf diesem Rechenweg nicht gerechnet".</b> Kühlenergie, Kühlstunden,
    /// Raumtemperatur, Überhitzungs- und Sommerlüftungsstunden und die obere Raumtemperatur gibt
    /// es nur auf dem VDI-Weg; ein Gebäude auf dem Tagesbilanz-Weg trägt dort NULL. Wärmebedarf
    /// und die drei Spitzenwerte haben beide Wege.</para>
    ///
    /// <para><b>Ergebnisneutral.</b> Reines DDL; kein Rechenweg liest die Tabelle, und der
    /// Referenzlauf exportiert sie nicht — die Kennzahlen stehen dort schon als Skalare
    /// <c>Geb[i].*</c> (<see cref="GebaeudeErgebnisexport"/>).</para>
    /// </summary>
    public static class ErgebnisGebaeudeSchema
    {
        /// <summary>Der Tabellenname.</summary>
        public const string TAB = "Tab_ErgebnisGebaeude";

        /// <summary>Spaltenzahl der Tabelle (Nachweis in den Tests).</summary>
        public const int SPALTENZAHL = 16;

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_ErgebnisGebaeude</c> — 16 Spalten.</summary>
        public const string SQL_CREATE =
            "CREATE TABLE IF NOT EXISTS \"Tab_ErgebnisGebaeude\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY,\n" +
            "    \"ID_Ergebnis\" INTEGER NOT NULL REFERENCES \"Tab_Ergebnis\" (\"ID\") ON DELETE CASCADE,\n" +
            "    \"ID_Gebaeude\" INTEGER NOT NULL REFERENCES \"Tab_Gebaeude\" (\"ID\") ON DELETE CASCADE ON UPDATE CASCADE,\n" +
            "    \"Merkplatz\" INTEGER NOT NULL CHECK (\"Merkplatz\" >= 0),\n" +
            "    \"Gebaeudename\" TEXT,\n" +
            "    \"Rechenweg\" TEXT NOT NULL CHECK (\"Rechenweg\" IN ('VDI6007','TAGESBILANZ')),\n" +
            "    \"Heizwaerme_Mwh\" REAL NOT NULL,\n" +
            "    \"Spitze_Kw\" REAL NOT NULL,\n" +
            "    \"SpitzeTagesmittel_Kw\" REAL NOT NULL,\n" +
            "    \"Spitze95_Kw\" REAL NOT NULL,\n" +
            "    \"Kuehlenergie_Mwh\" REAL,\n" +
            "    \"Kuehlstunden_H\" INTEGER CHECK (\"Kuehlstunden_H\" BETWEEN 0 AND 8760),\n" +
            "    \"MittlereRaumtemperatur_C\" REAL,\n" +
            "    \"Ueberhitzungsstunden_H\" INTEGER CHECK (\"Ueberhitzungsstunden_H\" BETWEEN 0 AND 8760),\n" +
            "    \"Sommerlueftungsstunden_H\" INTEGER CHECK (\"Sommerlueftungsstunden_H\" BETWEEN 0 AND 8760),\n" +
            "    \"ObereRaumtemperatur_C\" REAL\n" +
            ") STRICT";

        /// <summary>Index auf dem Kopfverweis — trägt das Lesen je Lauf und die Löschweitergabe.</summary>
        public const string INDEX_ERGEBNIS = "Tab_ErgebnisGebaeude_ID_Ergebnis";

        /// <summary>Index auf dem Gebäudeverweis — trägt die Löschweitergabe beim Löschen eines Gebäudes.</summary>
        public const string INDEX_GEBAEUDE = "Tab_ErgebnisGebaeude_ID_Gebaeude";

        /// <summary>Die Anweisungen in Anlegereihenfolge (erst die Tabelle, dann die zwei Indizes), je Name.</summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(TAB, SQL_CREATE);
                yield return new KeyValuePair<string, string>(INDEX_ERGEBNIS,
                    "CREATE INDEX IF NOT EXISTS \"" + INDEX_ERGEBNIS + "\" ON \"" + TAB + "\" (\"ID_Ergebnis\")");
                yield return new KeyValuePair<string, string>(INDEX_GEBAEUDE,
                    "CREATE INDEX IF NOT EXISTS \"" + INDEX_GEBAEUDE + "\" ON \"" + TAB + "\" (\"ID_Gebaeude\")");
            }
        }

        /// <summary>Steht die Tabelle auf der aktuellen Datenbank? (Nachprobe des Schritts, Leseweg des Ergebnisses.)</summary>
        public static bool Vorhanden()
        {
            return DataRepository.TabelleVorhanden(TAB);
        }
    }
}
