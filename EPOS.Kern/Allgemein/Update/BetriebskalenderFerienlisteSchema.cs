using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // FERIENLISTE DES BETRIEBSKALENDERS (Anwenderentscheid 17.10.2026: "Kalender bekommt eine Ferienliste wie das
    // Gebaeude").
    //
    // WOZU. Ein Betriebskalender (Tab_Betriebskalender, BedarfNetzKalenderSchema) traegt vier feste Ferienpaare
    // Ferien1_Von/_Bis ... Ferien4_Von/_Bis. Das Gebaeude fuehrt vier Spalten PLUS eine Ferienliste; "Aus Gebaeude
    // uebernehmen..." lieferte deshalb mehr Zeitraeume, als der Kalender aufnahm. Die neue Tabelle ergaenzt die vier
    // Spaltenpaare um beliebig viele weitere Zeitraeume - die Spalten bleiben (Bestand), wie beim Gebaeude:
    //
    //   Tab_Betriebskalenderferien (STRICT)
    //     ID                   INTEGER PRIMARY KEY AUTOINCREMENT
    //     ID_Betriebskalender  INTEGER NOT NULL -> Tab_Betriebskalender (ID) ON DELETE CASCADE   Beziehung ueber die ID
    //     Reihenfolge          INTEGER NOT NULL >= 1                                            Position in der Liste
    //     Name                 TEXT NULL, hoechstens 100 Zeichen                                wahlfrei
    //     Beginn               INTEGER NOT NULL, 1 .. 365                                       Jahrestag im Gemeinjahr
    //     Ende                 INTEGER NOT NULL, 1 .. 365                                       Beginn > Ende = ueber den
    //                                                                                           Jahreswechsel
    //     UNIQUE (ID_Betriebskalender, Reihenfolge)
    //   Index Tab_Betriebskalenderferien_Kalender auf ID_Betriebskalender.
    //
    // DAS FORMAT folgt den Spalten des Kalenders (Jahrestag 1 ... 365, beide Grenzen gesetzt), nicht der Zonenliste des
    // Zapfprofils (dort darf der Beginn fehlen). Keine Datenuebernahme: Die Liste entsteht leer.
    //
    // ERGEBNISNEUTRAL. Die Tabelle entsteht leer; kein Referenzprojekt traegt einen Betriebskalender an einer Zuordnung.
    // Wiederholbar.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in EPOS.Kern.Tests und
    // die Paketanhebung (Art Ddl); dazu BetriebskalenderCtrl (Lesen, Speichern, Loeschen) nach Vorhandensein.
    // ====================================================================================

    /// <summary>
    /// <b>Ferienliste des Betriebskalenders</b> — Schemaschritt (Nummer <see cref="SCHRITT"/>): die Tabelle
    /// <see cref="TAB_FERIEN"/> mit beliebig vielen weiteren Betriebsferien je Kalender neben den vier Spaltenpaaren.
    /// EINE Quelle für Migration, Werkzeug, Testkopie, Paketanhebung und Nachweis (ADR-001 Option C).
    /// </summary>
    public static class BetriebskalenderFerienlisteSchema
    {
        /// <summary><b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht.</summary>
        // KETTE: 241 haengt ueber + 1 an 240 (ErgebnisWirtschaftlichkeitSpaltenSchema, KB-8) — vorlaeufig + 3 an 238 (KlimaRohwerteSchema), bis 239 und 240 auf origin liegen; dann umhaengen.
        public const int SCHRITT = KlimaRohwerteSchema.SCHRITT + 3;

        /// <summary>Die Tabelle der weiteren Betriebsferien.</summary>
        public const string TAB_FERIEN = "Tab_Betriebskalenderferien";

        /// <summary>Der Kalender, an dem die Liste hängt.</summary>
        public const string TAB_KALENDER = BedarfNetzKalenderSchema.TAB_KALENDER;

        public const string SPALTE_KALENDER = "ID_Betriebskalender";
        public const string SPALTE_REIHENFOLGE = "Reihenfolge";
        public const string SPALTE_NAME = "Name";
        public const string SPALTE_BEGINN = "Beginn";
        public const string SPALTE_ENDE = "Ende";

        /// <summary>Der Index auf der Kalenderspalte.</summary>
        public const string INDEX_KALENDER = "Tab_Betriebskalenderferien_Kalender";

        /// <summary>Höchstlänge des Namens eines Zeitraums.</summary>
        public const int NAME_MAX = 100;

        private static string Q(string s) => "\"" + s + "\"";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Betriebskalenderferien</c> — 6 Spalten, STRICT.</summary>
        public static readonly string SQL_CREATE =
            "CREATE TABLE IF NOT EXISTS " + Q(TAB_FERIEN) + " (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    " + Q(SPALTE_KALENDER) + " INTEGER NOT NULL REFERENCES " + Q(TAB_KALENDER) + " (\"ID\") ON DELETE CASCADE,\n" +
            "    " + Q(SPALTE_REIHENFOLGE) + " INTEGER NOT NULL CHECK (" + Q(SPALTE_REIHENFOLGE) + " >= 1),\n" +
            "    " + Q(SPALTE_NAME) + " TEXT CHECK (" + Q(SPALTE_NAME) + " IS NULL OR length(" + Q(SPALTE_NAME) + ") <= " +
            NAME_MAX.ToString(CultureInfo.InvariantCulture) + "),\n" +
            "    " + Q(SPALTE_BEGINN) + " INTEGER NOT NULL CHECK (" + Q(SPALTE_BEGINN) + " BETWEEN 1 AND 365),\n" +
            "    " + Q(SPALTE_ENDE) + " INTEGER NOT NULL CHECK (" + Q(SPALTE_ENDE) + " BETWEEN 1 AND 365),\n" +
            "    UNIQUE (" + Q(SPALTE_KALENDER) + ", " + Q(SPALTE_REIHENFOLGE) + ")\n" +
            ") STRICT";

        /// <summary>Der Index auf <see cref="SPALTE_KALENDER"/>.</summary>
        public static readonly string SQL_INDEX =
            "CREATE INDEX IF NOT EXISTS " + Q(INDEX_KALENDER) + " ON " + Q(TAB_FERIEN) + " (" + Q(SPALTE_KALENDER) + ")";

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { TAB_KALENDER };

        /// <summary>Steht die Tabelle?</summary>
        public static bool TabelleVorhanden() => DataRepository.TabelleVorhanden(TAB_FERIEN);

        /// <summary>Steht der Schritt? (Tabelle samt Index.)</summary>
        public static bool Vollstaendig()
        {
            if (!TabelleVorhanden()) return false;
            object n = DataRepository.ExecuteScalar("SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = ?",
                                                    new DbParam("?", INDEX_KALENDER));
            return n != null && n != DBNull.Value && Convert.ToInt64(n, CultureInfo.InvariantCulture) > 0;
        }

        /// <summary>
        /// Führt den Schritt aus (wiederholbar): Tabelle und Index in EINEM Vorgang. Rückgabe = Zahl der angelegten
        /// Objekte (Tabelle, Index). Fehlt eine Voraussetzung, wirft er benannt. Keine Datenübernahme.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            // Die Auskunft VOR dem Vorgang - TabelleVorhanden arbeitet auf einer eigenen Verbindung.
            bool tabelleFehlt = !TabelleVorhanden();
            bool indexFehlt = !Vollstaendig();
            int angelegt = 0;
            if (tabelleFehlt || indexFehlt)
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        if (tabelleFehlt) v.Ausfuehren(SQL_CREATE);
                        v.Ausfuehren(SQL_INDEX);
                        v.Commit();
                    }
                    catch
                    {
                        v.Rollback();
                        throw;
                    }
                }
                if (tabelleFehlt) { angelegt++; bericht?.Add(TAB_FERIEN + " angelegt"); }
                angelegt++;
                bericht?.Add(INDEX_KALENDER + " angelegt");
            }
            else
            {
                bericht?.Add("steht bereits - " + TAB_FERIEN);
            }
            bericht?.Add("Die Liste entsteht leer; kein Referenzprojekt traegt einen Betriebskalender, der Referenzlauf " +
                         "bleibt gleich");
            return angelegt;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
