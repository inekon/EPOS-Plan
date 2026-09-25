using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Ergebnistabelle je Gebäude</b> — Schemaschritt 107 (Entscheid E30 vom 23.09.2026,
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
    ///
    /// <para><b>Schritt 128</b> (<see cref="SCHRITT_HEIZKREIS"/>; Anlagenkopplung AK1, Welle 3) hängt vier nullbare Spalten des
    /// Heizkreises an (<see cref="SpaltenHeizkreis"/>): Übergabeart, mittlerer Vor- und
    /// Rücklauf und die Stunden mit begrenzter Übergabe — NULL heißt „nicht gekoppelt
    /// gerechnet". Die Tabelle aus <see cref="SQL_CREATE"/> bleibt die des Schritts 107; ihr
    /// Stand nach 128 hat <see cref="SPALTENZAHL_MIT_HEIZKREIS"/> Spalten.</para>
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

        // =====================================================================
        //  Schritt 128 — der Heizkreis je Gebäude (Anlagenkopplung AK1, Welle 3)
        // =====================================================================
        //
        // Die drei Größen der Projektzeile (Schritt 123, Tab_ErgebnisEnergiebedarf) JE GEBÄUDE
        // — nach dem Muster E30: Der Lauf schreibt, der Bericht liest und rechnet nichts nach.
        // Dazu die Übergabeart, mit der das Gebäude gekoppelt gerechnet hat: Sie ist zugleich
        // die Kennung „gekoppelt" der Zeile, und der Bericht nennt die Art des LAUFS, nicht die
        // einer später geänderten Eingabe.

        /// <summary>
        /// <c>Uebergabe_Art</c> — die Übergabeart des gekoppelt gerechneten Gebäudes
        /// (<c>DbWerte.UEBERGABE_*</c> ohne „ideal"); <b>NULL heißt „nicht gekoppelt gerechnet"</b>.
        /// </summary>
        public const string SPALTE_UEBERGABE_ART = "Uebergabe_Art";

        /// <summary><c>VorlaufMittel_C</c> — heizzeitgewichtetes Mittel des gefahrenen Vorlaufs [°C]; NULL ohne Kopplung oder ohne Heizstunde.</summary>
        public const string SPALTE_VORLAUF_MITTEL = "VorlaufMittel_C";

        /// <summary><c>RuecklaufMittel_C</c> — dasselbe für den Rücklauf [°C].</summary>
        public const string SPALTE_RUECKLAUF_MITTEL = "RuecklaufMittel_C";

        /// <summary><c>UebergabeBegrenzt_H</c> — Stunden, in denen die Übergabe die Grenze war [h], Summe der Zeitanteile; NULL ohne Kopplung.</summary>
        public const string SPALTE_UEBERGABE_BEGRENZT = "UebergabeBegrenzt_H";

        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht: Migration
        /// (<c>SchemaMigration.SCHRITT_128_ERGEBNIS_HEIZKREIS</c>), Werkzeug und Nachweis lesen sie
        /// hier. Vergeben beim Merge mit origin am 24.09.2026 (125 bis 127 waren belegt).
        /// </summary>
        public const int SCHRITT_HEIZKREIS = 128;

        /// <summary>Spaltenzahl der Tabelle nach Schritt 128 (Nachweis in den Tests).</summary>
        public const int SPALTENZAHL_MIT_HEIZKREIS = SPALTENZAHL + 4;

        /// <summary>
        /// Spaltenzahl der Tabelle nach KAK-S3 (<see cref="KuehluebergabeSchema.SCHRITT_ERGEBNIS"/>,
        /// E37): dazu die fünf Spalten des Kältekreises (<see cref="KuehluebergabeSchema.SpaltenKuehlkreis"/>).
        /// </summary>
        public const int SPALTENZAHL_MIT_KUEHLKREIS = SPALTENZAHL_MIT_HEIZKREIS + 5;

        /// <summary>
        /// <b>Die vier Spalten von Schritt 128</b> in Anlegereihenfolge: Name und SQLite-Definition
        /// (STRICT-Typ samt <c>CHECK</c>), <b>nullbar, ohne Vorgabe und ohne Nachtrag</b> — jede
        /// vorhandene Ergebniszeile ist eine Zeile ohne Kopplung. Die Wertliste der Übergabeart
        /// kommt aus <c>DbWerte</c>, die Stunden sind eine Summe von Zeitanteilen und deshalb
        /// <c>REAL</c> (0 … 8 760).
        /// </summary>
        public static readonly IReadOnlyList<KeyValuePair<string, string>> SpaltenHeizkreis = new[]
        {
            new KeyValuePair<string, string>(SPALTE_UEBERGABE_ART,
                "TEXT CHECK (\"" + SPALTE_UEBERGABE_ART + "\" IN ('" + DbWerte.UEBERGABE_RADIATOR + "','" +
                DbWerte.UEBERGABE_FLAECHE + "','" + DbWerte.UEBERGABE_KONVEKTOR + "'))"),
            new KeyValuePair<string, string>(SPALTE_VORLAUF_MITTEL, "REAL"),
            new KeyValuePair<string, string>(SPALTE_RUECKLAUF_MITTEL, "REAL"),
            new KeyValuePair<string, string>(SPALTE_UEBERGABE_BEGRENZT,
                "REAL CHECK (\"" + SPALTE_UEBERGABE_BEGRENZT + "\" BETWEEN 0 AND 8760)"),
        };

        /// <summary>Die Anweisung, die eine Spalte von <see cref="SpaltenHeizkreis"/> anlegt (<c>ALTER TABLE … ADD COLUMN</c>).</summary>
        public static string SpalteAnlegen(KeyValuePair<string, string> spalte)
            => "ALTER TABLE \"" + TAB + "\" ADD COLUMN \"" + spalte.Key + "\" " + spalte.Value;

        /// <summary>Steht Schritt 128? Die Tabelle steht und trägt alle vier Spalten des Heizkreises.</summary>
        public static bool HeizkreisVollstaendig()
            => Vorhanden() && SpaltenHeizkreis.All(s => DataRepository.SpalteVorhanden(TAB, s.Key));

        /// <summary>
        /// Führt Schritt 128 in EINEM Vorgang aus — für <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>; die Migration der Schale geht denselben Weg über ihre eigenen
        /// Helfer. <b>Wiederholbar</b>, <b>kein DML</b>; ohne die Tabelle (Stand vor 107) tut er nichts.
        /// </summary>
        /// <param name="bericht">Nimmt eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (höchstens vier).</returns>
        public static int HeizkreisAlle(IList<string> bericht)
        {
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen Verbindung
            // und saehe die offene Transaktion nicht.
            if (!Vorhanden())
            {
                bericht?.Add(TAB + " fehlt (Stand vor Schritt 107) - nichts angelegt");
                return 0;
            }
            var fehlend = SpaltenHeizkreis.Where(s => !DataRepository.SpalteVorhanden(TAB, s.Key)).ToList();

            int angelegt = 0;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach (KeyValuePair<string, string> s in fehlend)
                    {
                        v.Ausfuehren(SpalteAnlegen(s));
                        angelegt++;
                    }
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            bericht?.Add(angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                         SpaltenHeizkreis.Count.ToString(CultureInfo.InvariantCulture) +
                         " Spalte(n) des Heizkreises an " + TAB + " angelegt");
            return angelegt;
        }
    }
}
