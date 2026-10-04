using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KU3-4 - DIE KAELTEMASCHINE ALS ANLAGE (Konzept Kuehlung 5.3, 5.5, 6.2, 7.3; Plan G7b bis KU3
    // Abschnitt 6, Zeile KU3-4; Entscheide E67/E68).
    //
    // WAS.
    //   Tab_Typ_Energieanlagen      Zeile 13 "Kältemaschine" (feste Nummer nach WizardItemClass)
    //   Tab_Energieanlagen          ID_Kaeltemaschine (Verweis auf die Projektkopie, SET NULL) und
    //                               Kaeltemaschine_Anzahl (NOT NULL DEFAULT 1); Kuehl_ID_Carrier und
    //                               Kuehl_EigenerZaehler stehen schon (KU-S3) und gelten wie bei der
    //                               Waermepumpe
    //   Tab_Kaeltemaschine          Kuehl_Vorlauf und Kuehl_Hilfsstromanteil an der Projektkopie - wie
    //                               Tab_WP.Kuehl_Vorlauf/Kuehl_Hilfsstromanteil, nur im Projekt
    //   Tab_KostenKomponente        Zeile 11 "Kältemaschine"
    //   Tab_Nutzungsdauer           Standardzeile "Gerät" der Komponente 11 (VDI 2067: 15 a)
    //   Tab_KostenVorlage(Position) Standardvorlagen Investition und Betrieb der Komponente 11
    //   Tab_ErgebnisKaeltemaschine  das Ergebnis je Maschine (Muster Tab_ErgebnisWaermepumpe)
    //
    // SAAT nur hier, wiederholbar (INSERT ... WHERE NOT EXISTS). Neutrale, runde Werte.
    //
    // ERGEBNISNEUTRAL. Kein Referenzprojekt fuehrt eine Anlagenzeile der Kaeltemaschine; die neuen
    // Spalten entstehen leer bzw. mit dem Vorgabewert 1. Der Referenzlauf bleibt byte-gleich.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>Schemaschritt der Kältemaschine als Anlage</b> (KU3-4): Anlagentyp, Verweis und Anzahl an der
    /// Anlagenzeile, Kühleingaben an der Projektkopie, Kostenkomponente samt Vorlagen und Nutzungsdauer,
    /// Ergebnistabelle je Maschine. Wiederholbar und ergebnisneutral.
    /// </summary>
    public static class KaeltemaschineAnlageSchema
    {
        /// <summary>Die Nummer des Schritts — hängt an der Vorgängerklasse.</summary>
        public const int SCHRITT = KaeltemaschineSchema.SCHRITT + 1;

        /// <summary><c>Tab_Typ_Energieanlagen.ID</c> der Kältemaschine — feste Nummer.</summary>
        public const int TYP_KAELTEMASCHINE = WizardItemClass.KM_TYP;

        /// <summary><c>Tab_KostenKomponente.ID</c> der Kältemaschine — feste Nummer.</summary>
        public const int KOMPONENTE_KAELTEMASCHINE = 11;

        /// <summary>Die Anlagentabelle.</summary>
        public const string TAB_ANLAGEN = "Tab_Energieanlagen";

        /// <summary>Verweis der Anlagenzeile auf die Projektkopie.</summary>
        public const string SPALTE_ID_KAELTEMASCHINE = KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE;

        /// <summary>Anzahl gleicher Maschinen der Anlagenzeile (Vorgabe 1).</summary>
        public const string SPALTE_ANZAHL = "Kaeltemaschine_Anzahl";

        /// <summary>Kaltwasservorlauf der Projektkopie [°C]; NULL = kleinste Stützstelle der Kennlinie.</summary>
        public const string SPALTE_KUEHL_VORLAUF = "Kuehl_Vorlauf";

        /// <summary>Hilfsstromanteil der Projektkopie [0..1); NULL = kein Zuschlag.</summary>
        public const string SPALTE_KUEHL_HILFSSTROMANTEIL = "Kuehl_Hilfsstromanteil";

        /// <summary>Das Ergebnis je Maschine.</summary>
        public const string TAB_ERGEBNIS = "Tab_ErgebnisKaeltemaschine";

        /// <summary>Die Ergebnisspalten in Schemareihenfolge (ohne ID, ID_Ergebnis, ID_Kaeltemaschine, Bezeichner).</summary>
        public static readonly string[] ErgebnisSpalten =
        {
            "Anzahl", "Kaelteproduktion_MWh", "Stromverbrauch_MWh", "Hilfsstrom_MWh", "FreieKuehlung_MWh",
            "FreieKuehlung_Stunden", "Taktstunden", "Unterdeckung_MWh", "Stunden_Leistungsgrenze"
        };

        /// <summary>Die Spalten des Schritts an bestehenden Tabellen.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN = new[]
        {
            (TAB_ANLAGEN, SPALTE_ID_KAELTEMASCHINE,
             "INTEGER REFERENCES \"" + KaeltemaschineSchema.TAB_PROJEKT + "\" (\"ID\") ON DELETE SET NULL"),
            (TAB_ANLAGEN, SPALTE_ANZAHL,
             "INTEGER NOT NULL DEFAULT 1 CHECK (\"" + SPALTE_ANZAHL + "\" >= 1)"),
            (KaeltemaschineSchema.TAB_PROJEKT, SPALTE_KUEHL_VORLAUF,
             "REAL CHECK (\"" + SPALTE_KUEHL_VORLAUF + "\" IS NULL OR \"" + SPALTE_KUEHL_VORLAUF + "\" BETWEEN -20 AND 30)"),
            (KaeltemaschineSchema.TAB_PROJEKT, SPALTE_KUEHL_HILFSSTROMANTEIL,
             "REAL CHECK (\"" + SPALTE_KUEHL_HILFSSTROMANTEIL + "\" IS NULL OR (\"" + SPALTE_KUEHL_HILFSSTROMANTEIL +
             "\" >= 0 AND \"" + SPALTE_KUEHL_HILFSSTROMANTEIL + "\" < 1))"),
        };

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_ErgebnisKaeltemaschine</c>.</summary>
        public static string SqlCreateErgebnis() =>
            "CREATE TABLE IF NOT EXISTS \"" + TAB_ERGEBNIS + "\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Ergebnis\" INTEGER NOT NULL REFERENCES \"Tab_Ergebnis\" (\"ID\") ON DELETE CASCADE,\n" +
            "    \"" + SPALTE_ID_KAELTEMASCHINE + "\" INTEGER REFERENCES \"" + KaeltemaschineSchema.TAB_PROJEKT + "\" (\"ID\") ON DELETE SET NULL,\n" +
            "    \"Bezeichner\" TEXT,\n" +
            "    \"Anzahl\" INTEGER,\n" +
            "    \"Kaelteproduktion_MWh\" REAL,\n" +
            "    \"Stromverbrauch_MWh\" REAL,\n" +
            "    \"Hilfsstrom_MWh\" REAL,\n" +
            "    \"FreieKuehlung_MWh\" REAL,\n" +
            "    \"FreieKuehlung_Stunden\" INTEGER,\n" +
            "    \"Taktstunden\" INTEGER,\n" +
            "    \"Unterdeckung_MWh\" REAL,\n" +
            "    \"Stunden_Leistungsgrenze\" INTEGER\n" +
            ") STRICT";

        /// <summary>Was der Schritt voraussetzt.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[]
        {
            "Tab_Projekt", "Tab_Ergebnis", TAB_ANLAGEN, "Tab_Typ_Energieanlagen", "Tab_KostenKomponente",
            "Tab_Nutzungsdauer", "Tab_KostenVorlage", "Tab_KostenVorlagePosition", KaeltemaschineSchema.TAB_PROJEKT
        };

        /// <summary>Die <c>ALTER TABLE … ADD COLUMN</c>-Anweisung einer Spalte.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE \"" + s.Tabelle + "\" ADD COLUMN \"" + s.Spalte + "\" " + s.Typ;

        // =================================================================
        //  Die Saat
        // =================================================================

        /// <summary>Die Nutzungsdauer des Geräts nach VDI 2067 Blatt 1 [a].</summary>
        public const double NUTZUNGSDAUER_JAHRE = 15;

        /// <summary>Die Position der Investitionsvorlage, die mit der Nennkälteleistung rechnet.</summary>
        public const string POSITION_AGGREGAT = "Kältemaschine (Aggregat)";

        /// <summary>Vorgabesatz der Aggregatposition [€/kW Kälteleistung] — runder, neutraler Wert.</summary>
        public const double SATZ_AGGREGAT_EUR_JE_KW = 300;

        /// <summary>Wartung [% der Investition je Jahr] — runder Wert nach VDI 2067.</summary>
        public const double WARTUNG_PROZENT = 1.5;

        /// <summary>Instandsetzung [% der Investition je Jahr] — runder Wert nach VDI 2067.</summary>
        public const double INSTANDSETZUNG_PROZENT = 1.5;

        /// <summary>Eine Position einer Standardvorlage.</summary>
        public sealed record Position(string Bezeichnung, string Kostenart, string Bemessung, double? Satz, int Sortierung, bool Pflicht);

        /// <summary>Die Investitionsvorlage.</summary>
        public static readonly IReadOnlyList<Position> VORLAGE_INVESTITION = new[]
        {
            new Position(POSITION_AGGREGAT, DbWerte.KOSTENART_KAPITALGEBUNDEN, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, SATZ_AGGREGAT_EUR_JE_KW, 10, false),
            new Position("Rückkühlung / Zubehör", DbWerte.KOSTENART_KAPITALGEBUNDEN, DbWerte.BEMESSUNG_BETRAG, null, 20, false),
            new Position("MSR-Technik / Automation", DbWerte.KOSTENART_KAPITALGEBUNDEN, DbWerte.BEMESSUNG_PROZENT_ERZEUGERKOSTEN, null, 30, false),
            new Position("Montage, Installation & Kältetechnik", DbWerte.KOSTENART_KAPITALGEBUNDEN, DbWerte.BEMESSUNG_PROZENT_INVESTITION, null, 40, false),
            new Position("Planung / Baunebenkosten", DbWerte.KOSTENART_KAPITALGEBUNDEN, DbWerte.BEMESSUNG_PROZENT_INVESTITION, null, 50, false),
        };

        /// <summary>Die Betriebsvorlage.</summary>
        public static readonly IReadOnlyList<Position> VORLAGE_BETRIEB = new[]
        {
            new Position("Wartung Kältemaschine", DbWerte.KOSTENART_BETRIEBSGEBUNDEN, DbWerte.BEMESSUNG_PROZENT_INVESTITION, WARTUNG_PROZENT, 10, true),
            new Position("Instandhaltung Kältemaschine", DbWerte.KOSTENART_BETRIEBSGEBUNDEN, DbWerte.BEMESSUNG_PROZENT_INVESTITION, INSTANDSETZUNG_PROZENT, 20, true),
        };

        /// <summary>Der Katalogstempel der Kosten an <c>Tab_Applikation</c>.</summary>
        private const string SPALTE_KATALOGSTEMPEL = "Kostenkatalog_Geaendert";

        internal const string SQL_SAAT_TYP =
            "INSERT INTO \"Tab_Typ_Energieanlagen\" (\"ID\", \"Bezeichner\") SELECT ?, ? " +
            "WHERE NOT EXISTS (SELECT 1 FROM \"Tab_Typ_Energieanlagen\" WHERE \"ID\" = ?)";

        internal const string SQL_SAAT_KOMPONENTE =
            "INSERT INTO \"Tab_KostenKomponente\" (\"ID\", \"Komponente\") SELECT ?, ? " +
            "WHERE NOT EXISTS (SELECT 1 FROM \"Tab_KostenKomponente\" WHERE \"ID\" = ?)";

        internal const string SQL_SAAT_NUTZUNGSDAUER =
            "INSERT INTO \"Tab_Nutzungsdauer\" (\"KomponentenID\", \"Positionsart\", \"IstStandard\", \"Nutzungsdauer_a\", " +
            "\"Instandsetzung_Prozent\", \"Wartung_Prozent\", \"Quelle\", \"ReadOnly\", \"Sortierung\") " +
            "SELECT ?, ?, 1, ?, ?, ?, ?, 1, 10 " +
            "WHERE NOT EXISTS (SELECT 1 FROM \"Tab_Nutzungsdauer\" WHERE \"KomponentenID\" = ? AND \"Positionsart\" = ?)";

        internal const string SQL_SAAT_VORLAGE =
            "INSERT INTO \"Tab_KostenVorlage\" (\"KomponentenID\", \"KategorieID\", \"Name\", \"IstStandard\", \"ReadOnly\") " +
            "SELECT ?, ?, 'Standard', 1, 1 " +
            "WHERE NOT EXISTS (SELECT 1 FROM \"Tab_KostenVorlage\" WHERE \"KomponentenID\" = ? AND \"KategorieID\" = ? AND \"IstStandard\" = 1)";

        internal const string SQL_SAAT_POSITION =
            "INSERT INTO \"Tab_KostenVorlagePosition\" (\"VorlageID\", \"Bezeichnung\", \"Kostenart\", \"Bemessung\", \"Satz\", " +
            "\"IstErloes\", \"Sortierung\", \"IstPflicht\", \"NutzungsdauerID\") " +
            "SELECT v.\"ID\", ?, ?, ?, ?, 0, ?, ?, " +
            "(SELECT n.\"ID\" FROM \"Tab_Nutzungsdauer\" n WHERE n.\"KomponentenID\" = ? AND n.\"IstStandard\" = 1 LIMIT 1) " +
            "FROM \"Tab_KostenVorlage\" v WHERE v.\"KomponentenID\" = ? AND v.\"KategorieID\" = ? AND v.\"IstStandard\" = 1 " +
            "AND NOT EXISTS (SELECT 1 FROM \"Tab_KostenVorlagePosition\" p WHERE p.\"VorlageID\" = v.\"ID\" AND p.\"Bezeichnung\" = ?)";

        /// <summary>Legt Typ, Komponente, Nutzungsdauer und Vorlagen an, die noch fehlen; liefert die Zahl neuer Zeilen.</summary>
        public static int Saat(IList<string> bericht)
        {
            int n = 0;
            // Die Saat ist Auslieferung, keine Anwenderänderung: Der Katalogstempel der Kosten (Trigger an
            // Tab_KostenKomponente und Tab_Nutzungsdauer) bleibt, wie er war - kein Projekt rechnet anders.
            bool stempel = DataRepository.SpalteVorhanden("Tab_Applikation", SPALTE_KATALOGSTEMPEL);
            object stempelVorher = stempel ? DataRepository.ExecuteScalar("SELECT " + SPALTE_KATALOGSTEMPEL + " FROM Tab_Applikation LIMIT 1") : null;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    n += v.Ausfuehren(SQL_SAAT_TYP, new DbParam("?", TYP_KAELTEMASCHINE),
                        new DbParam("?", DbWerte.KOSTEN_KOMPONENTE_KAELTEMASCHINE), new DbParam("?", TYP_KAELTEMASCHINE));
                    n += v.Ausfuehren(SQL_SAAT_KOMPONENTE, new DbParam("?", KOMPONENTE_KAELTEMASCHINE),
                        new DbParam("?", DbWerte.KOSTEN_KOMPONENTE_KAELTEMASCHINE), new DbParam("?", KOMPONENTE_KAELTEMASCHINE));
                    n += v.Ausfuehren(SQL_SAAT_NUTZUNGSDAUER, new DbParam("?", KOMPONENTE_KAELTEMASCHINE),
                        new DbParam("?", "Gerät"), new DbParam("?", NUTZUNGSDAUER_JAHRE),
                        new DbParam("?", INSTANDSETZUNG_PROZENT), new DbParam("?", WARTUNG_PROZENT),
                        new DbParam("?", NutzungsdauerSchema.QUELLE_VDI),
                        new DbParam("?", KOMPONENTE_KAELTEMASCHINE), new DbParam("?", "Gerät"));
                    foreach ((int kategorie, IReadOnlyList<Position> positionen) in new[]
                             {
                                 (DbWerte.KOSTEN_KATEGORIE_INVESTITION, VORLAGE_INVESTITION),
                                 (DbWerte.KOSTEN_KATEGORIE_BETRIEB, VORLAGE_BETRIEB)
                             })
                    {
                        n += v.Ausfuehren(SQL_SAAT_VORLAGE, new DbParam("?", KOMPONENTE_KAELTEMASCHINE), new DbParam("?", kategorie),
                            new DbParam("?", KOMPONENTE_KAELTEMASCHINE), new DbParam("?", kategorie));
                        foreach (Position p in positionen)
                            n += v.Ausfuehren(SQL_SAAT_POSITION, new DbParam("?", p.Bezeichnung), new DbParam("?", p.Kostenart),
                                new DbParam("?", p.Bemessung), new DbParam("?", (object)p.Satz ?? DBNull.Value),
                                new DbParam("?", p.Sortierung), new DbParam("?", p.Pflicht ? 1 : 0),
                                new DbParam("?", KOMPONENTE_KAELTEMASCHINE),
                                new DbParam("?", KOMPONENTE_KAELTEMASCHINE), new DbParam("?", kategorie),
                                new DbParam("?", p.Bezeichnung));
                    }
                    if (stempel && n > 0)
                        v.Ausfuehren("UPDATE Tab_Applikation SET " + SPALTE_KATALOGSTEMPEL + " = ?",
                                     new DbParam("?", stempelVorher ?? DBNull.Value));
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            bericht?.Add("Saat der Kaeltemaschine als Anlage: " + n.ToString(CultureInfo.InvariantCulture) + " Zeile(n) angelegt");
            return n;
        }

        // =================================================================
        //  Stand und Ausführung
        // =================================================================

        /// <summary>Stehen Spalten und Ergebnistabelle?</summary>
        public static bool SchemaVollstaendig() =>
            DataRepository.TabelleVorhanden(TAB_ERGEBNIS)
            && SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte));

        /// <summary>Steht das Schema samt Typ, Komponente, Nutzungsdauer und beiden Vorlagen?</summary>
        public static bool Vollstaendig()
        {
            if (!SchemaVollstaendig()) return false;
            int Zahl(string sql, params object[] werte)
            {
                object o = DataRepository.ExecuteScalar(sql, werte.Select(w => new DbParam("?", w)).ToArray());
                return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
            return Zahl("SELECT COUNT(*) FROM Tab_Typ_Energieanlagen WHERE ID = ?", TYP_KAELTEMASCHINE) == 1
                && Zahl("SELECT COUNT(*) FROM Tab_KostenKomponente WHERE ID = ?", KOMPONENTE_KAELTEMASCHINE) == 1
                && Zahl("SELECT COUNT(*) FROM Tab_Nutzungsdauer WHERE KomponentenID = ? AND IstStandard = 1", KOMPONENTE_KAELTEMASCHINE) >= 1
                && Zahl("SELECT COUNT(*) FROM Tab_KostenVorlagePosition p JOIN Tab_KostenVorlage v ON v.ID = p.VorlageID " +
                        "WHERE v.KomponentenID = ? AND v.IstStandard = 1", KOMPONENTE_KAELTEMASCHINE)
                   >= VORLAGE_INVESTITION.Count + VORLAGE_BETRIEB.Count;
        }

        /// <summary>
        /// Legt fehlende Spalten und die Ergebnistabelle in EINEM Vorgang mit abgeschalteten Fremdschlüsseln an,
        /// dann die Saat. Wiederholbar; liefert die Zahl der Schemaanweisungen.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen Verbindung.
            var offen = SPALTEN.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)).ToList();
            bool ergebnisFehlt = !DataRepository.TabelleVorhanden(TAB_ERGEBNIS);
            int n = 0;
            if (offen.Count > 0 || ergebnisFehlt)
            {
                using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
                {
                    try
                    {
                        foreach ((string Tabelle, string Spalte, string Typ) s in offen)
                        {
                            v.Ausfuehren(Anlegen(s));
                            bericht?.Add(s.Tabelle + "." + s.Spalte + " angelegt");
                            n++;
                        }
                        if (ergebnisFehlt)
                        {
                            v.Ausfuehren(SqlCreateErgebnis());
                            bericht?.Add(TAB_ERGEBNIS + " angelegt");
                            n++;
                        }
                        v.Commit();
                    }
                    catch
                    {
                        v.Rollback();
                        throw;
                    }
                }
            }
            else bericht?.Add("Spalten und Ergebnistabelle der Kaeltemaschine als Anlage vorhanden");
            Saat(bericht);
            bericht?.Add("KEIN Rechenergebnis aendert sich, der Referenzlauf bleibt byte-gleich");
            return n;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
