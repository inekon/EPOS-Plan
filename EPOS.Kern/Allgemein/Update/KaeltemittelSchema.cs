using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KM4-E1 - DAS KAELTEMITTEL ALS STAMMDATUM: TABELLE, VERWEIS, INBETRIEBNAHMEJAHR, ZUORDNUNG
    // (Konzept Kaeltemittel als Stammdatum 3.1, 3.2, 4.1, 4.2, 4.4, 4.6, 6; Entscheid E123, KM4-Q1 bis Q17).
    //
    // WAS.
    //   Tab_Kaeltemittel_STAMM      neu, STRICT: eine Zeile je Code (ISO 817 mit Isomer), GWP mit Quelle und Bezug,
    //                               F-Gas, natuerlich, Sicherheitsgruppe, PFAS-Status, Status; Katalogspalten nach
    //                               Hausmuster (Register "KMT", Stufe 3, vor "KM"). KEINE Projektkopie.
    //   Tab_Kaeltemaschine_STAMM    ID_Kaeltemittel (nullbar, ohne Fremdschluesselzwang); die Textspalte bleibt
    //   Tab_Kaeltemaschine          ID_Kaeltemittel ebenso
    //   Tab_Projekt                 Inbetriebnahmejahr (nullbar, 2000 bis 2100; leer = keine Pruefung, KM4-Q5 a)
    //
    // SAAT. Achtzehn Codes mit GWP, Quelle, F-Gas, natuerlich und PFAS-Status, alle VORLAEUFIG bis zur Pruefung am
    // Volltext der Verordnung (KM4-Q2 a); Sicherheitsgruppe leer (KM4-Q3 b). Wiederholbar: nur fehlende Codes.
    //
    // ZUORDNUNG DES TEXTBESTANDS (3.2, KM4-Q8 a). Je Zeile mit leerer ID und gesetztem Text: genau ein Code-Treffer
    // (Trim, ohne Gross/Klein) setzt die ID, der Text bleibt; ohne Treffer bleibt sie leer (nie raten). Der
    // Auslieferungssatz „R1234ze“ (Beispielgeraet) bekommt ueber seinen Katalogschluessel R1234ze(E). Jeder
    // Katalogsatz, dessen Pruefsumme vorher stimmte und der eine ID bekommt, erhaelt die neue Pruefsumme (der Verweis
    // geht ueber den Code in die Summe ein).
    //
    // ERGEBNISNEUTRAL. Kein Rechenweg liest Tabelle, Verweis oder Jahr (die Pruefung ist KM4-E2). Drei Vorgaenge:
    // DDL mit abgeschalteten Fremdschluesseln, Saat, Zuordnung. Ein zweiter Lauf aendert nichts.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in EPOS.Kern.Tests und
    // die Paketanhebung (Art Import: der Verweis reist nicht, der Import setzt ihn aus dem Text).
    // ====================================================================================

    /// <summary>
    /// <b>KM4-E1</b> — das Kältemittel als Stammdatum: <c>Tab_Kaeltemittel_STAMM</c> samt Saat, der Verweis
    /// <c>ID_Kaeltemittel</c> an Katalog und Projektkopie der Kältemaschine, <c>Tab_Projekt.Inbetriebnahmejahr</c> und die
    /// Zuordnung des Textbestands. EINE Quelle für Migration, Werkzeug, Testkopie und Nachweis (ADR-001 Option C).
    /// </summary>
    public static class KaeltemittelSchema
    {
        /// <summary><b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht (216).</summary>
        // Hängt an Schritt 215 (AH-V4, VorheizenSchema).
        public const int SCHRITT = VorheizenSchema.SCHRITT + 1;

        /// <summary>Die Kältemitteltabelle (Katalog, keine Projektkopie).</summary>
        public const string TAB_STAMM = "Tab_Kaeltemittel_STAMM";

        /// <summary>Das Kürzel im Katalogregister.</summary>
        public const string KUERZEL = "KMT";

        /// <summary>Die Projekte.</summary>
        public const string TAB_PROJEKT = "Tab_Projekt";

        // ---- Spalten der Kältemitteltabelle (Konzept 4.1) ----

        public const string SPALTE_CODE = "Code";
        public const string SPALTE_BEZEICHNUNG = "Bezeichnung";
        public const string SPALTE_GRUPPE = "Gruppe";
        public const string SPALTE_GWP = "GWP";
        public const string SPALTE_GWP_QUELLE = "GWP_Quelle";
        public const string SPALTE_F_GAS = "F_Gas";
        public const string SPALTE_NATUERLICH = "Natuerlich";
        public const string SPALTE_SICHERHEITSGRUPPE = "Sicherheitsgruppe";
        public const string SPALTE_PFAS_STATUS = "PFAS_Status";
        public const string SPALTE_STATUS = "Status";
        public const string SPALTE_QUELLE = "Quelle";
        public const string SPALTE_SORTIERUNG = "Sortierung";

        /// <summary>Der Verweis der Kältemaschine auf ihr Kältemittel (Katalog und Projektkopie).</summary>
        public const string SPALTE_ID_KAELTEMITTEL = "ID_Kaeltemittel";

        /// <summary>Das geplante Jahr der Inbetriebnahme des Projekts.</summary>
        public const string SPALTE_INBETRIEBNAHMEJAHR = "Inbetriebnahmejahr";

        /// <summary>Kleinstes zulässiges Inbetriebnahmejahr.</summary>
        public const int INBETRIEBNAHMEJAHR_MIN = 2000;

        /// <summary>Größtes zulässiges Inbetriebnahmejahr.</summary>
        public const int INBETRIEBNAHMEJAHR_MAX = 2100;

        /// <summary>Höchstlänge des Codes.</summary>
        public const int CODE_MAX = 20;

        /// <summary>Höchstlänge der Textfelder Bezeichnung, GWP-Quelle und Quelle.</summary>
        public const int TEXT_MAX = 120;

        /// <summary>Der Code, auf den der Auslieferungssatz „R1234ze“ über seinen Katalogschlüssel zeigt (KM4-Q8 a).</summary>
        public const string BEISPIEL_CODE = "R1234ze(E)";

        /// <summary>Der Text des Auslieferungssatzes ohne Isomerangabe.</summary>
        public const string BEISPIEL_TEXT = "R1234ze";

        /// <summary>Der Katalogschlüssel des Beispielgeräts mit „R1234ze“ (Auslieferungssatz 3).</summary>
        public const string BEISPIEL_SCHLUESSEL = "KM:KAELTEMASCHINE_500_KW_WASSERGEKUEHLT_MIT_NASSKUEHLER";

        /// <summary>
        /// Die Fachspalten in Schemareihenfolge — alle außer <c>ID</c>, <c>ReadOnly</c> und den Katalogspalten. Die EINE
        /// Liste, an der Katalogfassung und Prüfsumme hängen.
        /// </summary>
        public static readonly string[] Fachspalten =
        {
            SPALTE_CODE, SPALTE_BEZEICHNUNG, SPALTE_GRUPPE, SPALTE_GWP, SPALTE_GWP_QUELLE, SPALTE_F_GAS, SPALTE_NATUERLICH,
            SPALTE_SICHERHEITSGRUPPE, SPALTE_PFAS_STATUS, SPALTE_STATUS, SPALTE_QUELLE, SPALTE_SORTIERUNG
        };

        private static string Q(string s) => "\"" + s + "\"";

        private static string Liste(IEnumerable<string> werte) => string.Join(",", werte.Select(w => "'" + w + "'"));

        private static string Zahl(int i) => i.ToString(CultureInfo.InvariantCulture);

        private static string TextBis(string s, int max) =>
            "TEXT CHECK (" + Q(s) + " IS NULL OR length(" + Q(s) + ") <= " + Zahl(max) + ")";

        private static string TextAus(string s, IEnumerable<string> werte) =>
            "TEXT CHECK (" + Q(s) + " IS NULL OR " + Q(s) + " IN (" + Liste(werte) + "))";

        private static string Schalter(string s) => "INTEGER CHECK (" + Q(s) + " IS NULL OR " + Q(s) + " IN (0,1))";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Kaeltemittel_STAMM</c> (die Katalogspalten legt der Schritt danach an).</summary>
        public static string SqlCreateStamm() =>
            "CREATE TABLE IF NOT EXISTS " + Q(TAB_STAMM) + " (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    " + Q(SPALTE_CODE) + " TEXT NOT NULL UNIQUE COLLATE NOCASE CHECK (length(" + Q(SPALTE_CODE) + ") BETWEEN 1 AND " +
                Zahl(CODE_MAX) + "),\n" +
            "    " + Q(SPALTE_BEZEICHNUNG) + " " + TextBis(SPALTE_BEZEICHNUNG, TEXT_MAX) + ",\n" +
            "    " + Q(SPALTE_GRUPPE) + " " + TextAus(SPALTE_GRUPPE, DbWerte.KAELTEMITTEL_GRUPPEN) + ",\n" +
            "    " + Q(SPALTE_GWP) + " REAL CHECK (" + Q(SPALTE_GWP) + " IS NULL OR " + Q(SPALTE_GWP) + " >= 0),\n" +
            "    " + Q(SPALTE_GWP_QUELLE) + " " + TextBis(SPALTE_GWP_QUELLE, TEXT_MAX) + ",\n" +
            "    " + Q(SPALTE_F_GAS) + " " + Schalter(SPALTE_F_GAS) + ",\n" +
            "    " + Q(SPALTE_NATUERLICH) + " " + Schalter(SPALTE_NATUERLICH) + ",\n" +
            "    " + Q(SPALTE_SICHERHEITSGRUPPE) + " " + TextAus(SPALTE_SICHERHEITSGRUPPE, DbWerte.KAELTEMITTEL_SICHERHEITSGRUPPEN) + ",\n" +
            "    " + Q(SPALTE_PFAS_STATUS) + " " + TextAus(SPALTE_PFAS_STATUS, DbWerte.KAELTEMITTEL_PFAS_STATUS) + ",\n" +
            "    " + Q(SPALTE_STATUS) + " " + TextAus(SPALTE_STATUS, DbWerte.KAELTEMITTEL_STATUS) + ",\n" +
            "    " + Q(SPALTE_QUELLE) + " " + TextBis(SPALTE_QUELLE, TEXT_MAX) + ",\n" +
            "    " + Q(SPALTE_SORTIERUNG) + " INTEGER,\n" +
            "    \"ReadOnly\" INTEGER NOT NULL DEFAULT 0 CHECK (\"ReadOnly\" IN (0,1))\n" +
            ") STRICT";

        /// <summary>Die Spalten, die der Schritt an bestehende Tabellen anhängt — Tabelle, Name, Typ samt Klausel.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN = new[]
        {
            (KaeltemaschineSchema.TAB_STAMM, SPALTE_ID_KAELTEMITTEL, "INTEGER"),
            (KaeltemaschineSchema.TAB_PROJEKT, SPALTE_ID_KAELTEMITTEL, "INTEGER"),
            (TAB_PROJEKT, SPALTE_INBETRIEBNAHMEJAHR,
                "INTEGER CHECK (" + Q(SPALTE_INBETRIEBNAHMEJAHR) + " IS NULL OR " + Q(SPALTE_INBETRIEBNAHMEJAHR) + " BETWEEN " +
                Zahl(INBETRIEBNAHMEJAHR_MIN) + " AND " + Zahl(INBETRIEBNAHMEJAHR_MAX) + ")"),
        };

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() =>
            new[] { TAB_PROJEKT, KaeltemaschineSchema.TAB_STAMM, KaeltemaschineSchema.TAB_PROJEKT };

        /// <summary>Die Anweisung, die eine Spalte anlegt.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE " + Q(s.Tabelle) + " ADD COLUMN " + Q(s.Spalte) + " " + s.Typ;

        // =================================================================
        //  Die Saat (KM4-Q2 a, Q3 b, Q10 a, Q11 a)
        // =================================================================

        /// <summary>Eine Kältemittelzeile der Saat.</summary>
        public sealed record Kaeltemittel(string Code, string Bezeichnung, string Gruppe, double Gwp, string GwpQuelle,
                                          bool FGas, bool Natuerlich, string PfasStatus, string Vertrauen);

        private const string AR4 = "Anhang I AR4";
        private const string QUELLE_RECHERCHE = "Rechtsrecherche Kälteanlagen 10.10.2026; Vertrauensgrad ";
        private const string PFAS_HERLEITUNG = "; PFAS nach Strukturformel (OECD 2021)";

        /// <summary>
        /// Die achtzehn Codes — die zehn der Wärmepumpen-Klappliste eingeschlossen. GWP₁₀₀ nach der Verordnung (EU)
        /// 2024/573 mit Quelle und Bezug je Zeile; Gemische als massengewichtete Summe ihrer Bestandteile (eigene Rechnung).
        /// Alle Zeilen <c>VORLAEUFIG</c>, die Sicherheitsgruppe leer.
        /// </summary>
        public static readonly IReadOnlyList<Kaeltemittel> SAAT = new[]
        {
            new Kaeltemittel("R32", "Difluormethan", DbWerte.KAELTEMITTEL_GRUPPE_HFKW, 675,
                "VO 2024/573 Anhang I, GWP100 AR4", true, false, DbWerte.KAELTEMITTEL_PFAS_KEIN, "S+P"),
            new Kaeltemittel("R134a", "1,1,1,2-Tetrafluorethan", DbWerte.KAELTEMITTEL_GRUPPE_HFKW, 1430,
                "VO 2024/573 Anhang I, GWP100 AR4", true, false, DbWerte.KAELTEMITTEL_PFAS, "S"),
            new Kaeltemittel("R410A", "Gemisch R32/R125", DbWerte.KAELTEMITTEL_GRUPPE_GEMISCH, 2087.5,
                "Gemisch R32/R125 50/50 (" + AR4 + "; R125 = 3500), Abl.", true, false, DbWerte.KAELTEMITTEL_PFAS_BESTANDTEIL, "Abl."),
            new Kaeltemittel("R407C", "Gemisch R32/R125/R134a", DbWerte.KAELTEMITTEL_GRUPPE_GEMISCH, 1773.85,
                "Gemisch R32/R125/R134a 23/25/52 (" + AR4 + "), Abl.", true, false, DbWerte.KAELTEMITTEL_PFAS_BESTANDTEIL, "Abl."),
            new Kaeltemittel("R404A", "Gemisch R125/R143a/R134a", DbWerte.KAELTEMITTEL_GRUPPE_GEMISCH, 3921.6,
                "Gemisch R125/R143a/R134a 44/52/4 (" + AR4 + "; R143a = 4470), Abl.", true, false,
                DbWerte.KAELTEMITTEL_PFAS_BESTANDTEIL, "Abl."),
            new Kaeltemittel("R454B", "Gemisch R32/R1234yf", DbWerte.KAELTEMITTEL_GRUPPE_GEMISCH, 465.2,
                "Gemisch R32/R1234yf 68,9/31,1 (" + AR4 + ", Anhang II AR6), Abl.", true, false,
                DbWerte.KAELTEMITTEL_PFAS_BESTANDTEIL, "Abl."),
            new Kaeltemittel("R454C", "Gemisch R32/R1234yf", DbWerte.KAELTEMITTEL_GRUPPE_GEMISCH, 145.5,
                "Gemisch R32/R1234yf 21,5/78,5 (" + AR4 + ", Anhang II AR6), Abl.", true, false,
                DbWerte.KAELTEMITTEL_PFAS_BESTANDTEIL, "Abl./S"),
            new Kaeltemittel("R452B", "Gemisch R32/R125/R1234yf", DbWerte.KAELTEMITTEL_GRUPPE_GEMISCH, 697.4,
                "Gemisch R32/R125/R1234yf 67/7/26 (" + AR4 + ", Anhang II AR6), Abl.", true, false,
                DbWerte.KAELTEMITTEL_PFAS_BESTANDTEIL, "Abl."),
            new Kaeltemittel("R455A", "Gemisch R744/R32/R1234yf", DbWerte.KAELTEMITTEL_GRUPPE_GEMISCH, 145.5,
                "Gemisch R744/R32/R1234yf 3/21,5/75,5 (" + AR4 + ", Anhang II/VI AR6), Abl.", true, false,
                DbWerte.KAELTEMITTEL_PFAS_BESTANDTEIL, "Abl."),
            new Kaeltemittel("R513A", "Gemisch R1234yf/R134a", DbWerte.KAELTEMITTEL_GRUPPE_GEMISCH, 629.5,
                "Gemisch R1234yf/R134a 56/44 (Anhang II AR6, " + AR4 + "), Abl.", true, false,
                DbWerte.KAELTEMITTEL_PFAS_BESTANDTEIL, "Abl./S"),
            new Kaeltemittel("R1234ze(E)", "trans-1,3,3,3-Tetrafluorpropen", DbWerte.KAELTEMITTEL_GRUPPE_HFO, 1.37,
                "VO 2024/573 Anhang II Abschnitt 1, GWP100 AR6", true, false, DbWerte.KAELTEMITTEL_PFAS, "S"),
            new Kaeltemittel("R1234yf", "2,3,3,3-Tetrafluorpropen", DbWerte.KAELTEMITTEL_GRUPPE_HFO, 0.501,
                "VO 2024/573 Anhang II Abschnitt 1, GWP100 AR6", true, false, DbWerte.KAELTEMITTEL_PFAS, "S"),
            new Kaeltemittel("R1233zd(E)", "trans-1-Chlor-3,3,3-trifluorpropen", DbWerte.KAELTEMITTEL_GRUPPE_HFO, 3.88,
                "VO 2024/573 Anhang II Abschnitt 1, GWP100 AR6", true, false, DbWerte.KAELTEMITTEL_PFAS, "S"),
            new Kaeltemittel("R290", "Propan", DbWerte.KAELTEMITTEL_GRUPPE_NATUERLICH, 0.02,
                "VO 2024/573 Anhang VI, GWP100 AR6", false, true, DbWerte.KAELTEMITTEL_PFAS_KEIN, "S-amtl"),
            new Kaeltemittel("R600a", "Isobutan", DbWerte.KAELTEMITTEL_GRUPPE_NATUERLICH, 0,
                "VO 2024/573 Anhang VI (Fußnote nicht gelesen)", false, true, DbWerte.KAELTEMITTEL_PFAS_KEIN, "S-amtl"),
            new Kaeltemittel("R1270", "Propen", DbWerte.KAELTEMITTEL_GRUPPE_NATUERLICH, 0,
                "VO 2024/573 Anhang VI (Fußnote nicht gelesen)", false, true, DbWerte.KAELTEMITTEL_PFAS_KEIN, "S-amtl"),
            new Kaeltemittel("R744", "Kohlendioxid", DbWerte.KAELTEMITTEL_GRUPPE_NATUERLICH, 1,
                "Bezugsgas (GWP100 = 1)", false, true, DbWerte.KAELTEMITTEL_PFAS_KEIN, "S"),
            new Kaeltemittel("R717", "Ammoniak", DbWerte.KAELTEMITTEL_GRUPPE_NATUERLICH, 0,
                "VO 2024/573 Anhang VI", false, true, DbWerte.KAELTEMITTEL_PFAS_KEIN, "S-amtl"),
        };

        /// <summary>Die Quelle der übrigen Kennzeichen einer Saatzeile.</summary>
        public static string QuelleDer(Kaeltemittel k) => QUELLE_RECHERCHE + k.Vertrauen + PFAS_HERLEITUNG;

        /// <summary>Die Sortierung einer Saatzeile: ihre Stelle in <see cref="SAAT"/> mal zehn.</summary>
        public static int SortierungDer(Kaeltemittel k)
        {
            for (int i = 0; i < SAAT.Count; i++)
                if (ReferenceEquals(SAAT[i], k)) return (i + 1) * 10;
            return 0;
        }

        internal static readonly string SQL_SAAT =
            "INSERT INTO " + Q(TAB_STAMM) + " (" + string.Join(", ", Fachspalten.Select(Q)) + ", \"ReadOnly\") " +
            "SELECT ?, ?, ?, ?, ?, ?, ?, NULL, ?, ?, ?, ?, 1 WHERE NOT EXISTS (SELECT 1 FROM " + Q(TAB_STAMM) +
            " WHERE " + Q(SPALTE_CODE) + " = ?)";

        /// <summary>Legt die fehlenden Codes der Saat an (ausgelieferte Zeilen, <c>VORLAEUFIG</c>); liefert ihre Zahl.</summary>
        public static int Saat(IList<string> bericht)
        {
            int n = 0;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach (Kaeltemittel k in SAAT)
                        n += v.Ausfuehren(SQL_SAAT,
                            new DbParam("?", k.Code), new DbParam("?", k.Bezeichnung), new DbParam("?", k.Gruppe),
                            new DbParam("?", k.Gwp), new DbParam("?", k.GwpQuelle), new DbParam("?", k.FGas ? 1 : 0),
                            new DbParam("?", k.Natuerlich ? 1 : 0), new DbParam("?", k.PfasStatus),
                            new DbParam("?", DbWerte.GESETZ_STATUS_VORLAEUFIG), new DbParam("?", QuelleDer(k)),
                            new DbParam("?", SortierungDer(k)), new DbParam("?", k.Code));
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            bericht?.Add(TAB_STAMM + ": " + n.ToString(CultureInfo.InvariantCulture) + " Kaeltemittel angelegt (VORLAEUFIG)");
            return n;
        }

        // =================================================================
        //  Die Zuordnung des Textbestands (Konzept 3.2, KM4-Q8 a)
        // =================================================================

        // Trim wie string.Trim fuer die Zeichen, die ein Code-Text tragen kann (Leerzeichen, Tab, Zeilenwechsel).
        private const string TRIMZEICHEN = "' ' || char(9) || char(10) || char(13)";

        private static string Getrimmt(string ausdruck) => "trim(" + ausdruck + ", " + TRIMZEICHEN + ")";

        /// <summary>
        /// Setzt <c>ID_Kaeltemittel</c> einer Tabelle bei genau einem Code-Treffer (Trim, <c>COLLATE NOCASE</c> der
        /// Codespalte); nur Zeilen mit leerer ID und gesetztem Text. Ohne Parameter.
        /// </summary>
        public static string SqlZuordnung(string tabelle)
        {
            string text = Getrimmt(Q(tabelle) + "." + Q(KaeltemaschineSchema.SPALTE_KAELTEMITTEL));
            return "UPDATE " + Q(tabelle) + " SET " + Q(SPALTE_ID_KAELTEMITTEL) + " = (SELECT k.\"ID\" FROM " + Q(TAB_STAMM) +
                   " k WHERE k." + Q(SPALTE_CODE) + " = " + text + ") " +
                   "WHERE " + Q(SPALTE_ID_KAELTEMITTEL) + " IS NULL AND " + text + " <> '' " +
                   "AND (SELECT COUNT(*) FROM " + Q(TAB_STAMM) + " k2 WHERE k2." + Q(SPALTE_CODE) + " = " + text + ") = 1";
        }

        /// <summary>
        /// Setzt den Auslieferungssatz „R1234ze“ über seinen Katalogschlüssel auf <see cref="BEISPIEL_CODE"/> — nur mit
        /// leerer ID und dem Text ohne Isomer. Parameter: Code, Katalogschlüssel, Text, Code.
        /// </summary>
        internal static readonly string SQL_BEISPIEL =
            "UPDATE " + Q(KaeltemaschineSchema.TAB_STAMM) + " SET " + Q(SPALTE_ID_KAELTEMITTEL) + " = (SELECT k.\"ID\" FROM " +
            Q(TAB_STAMM) + " k WHERE k." + Q(SPALTE_CODE) + " = ?) " +
            "WHERE " + Q(Katalogfassung.SPALTE_SCHLUESSEL) + " = ? AND \"ReadOnly\" = 1 AND " + Q(SPALTE_ID_KAELTEMITTEL) + " IS NULL " +
            "AND " + Getrimmt(Q(KaeltemaschineSchema.SPALTE_KAELTEMITTEL)) + " = ? COLLATE NOCASE " +
            "AND EXISTS (SELECT 1 FROM " + Q(TAB_STAMM) + " k2 WHERE k2." + Q(SPALTE_CODE) + " = ?)";

        /// <summary>Das Ergebnis eines Laufs.</summary>
        public readonly record struct Laufergebnis(int Angelegt, int Saat, int KatalogZugeordnet, int ProjektZugeordnet,
                                                   int PruefsummenNeu);

        /// <summary>
        /// Die Zuordnung in EINEM Vorgang: erst der Auslieferungssatz über seinen Schlüssel, dann die exakten Treffer an
        /// Katalog und Projektkopie, zuletzt die neue Prüfsumme jedes Katalogsatzes, der eine ID bekommen hat und dessen
        /// Summe vorher stimmte. Liefert (Katalogsätze, Projektkopien, neue Prüfsummen).
        /// </summary>
        public static (int Katalog, int Projekt, int Summen) Zuordnen()
        {
            Katalogtabelle t = Katalogfassung.Tabelle(KaeltemaschineSchema.TAB_STAMM);
            bool mitSumme = t != null && Katalogfassung.SpaltenVorhanden(KaeltemaschineSchema.TAB_STAMM);
            List<string> spalten = mitSumme ? Katalogfassung.VorhandeneFachspalten(t) : null;
            string km = Q(KaeltemaschineSchema.TAB_STAMM);
            int katalog = 0, projekt, summen = 0;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    Func<string, DbParam[], DataTable> lese = (sql, p) => v.Lese(sql, p);
                    var stimmig = new List<long>();
                    if (mitSumme)
                    {
                        DataTable dt = v.Lese("SELECT ID, " + Q(Katalogfassung.SPALTE_PRUEFSUMME) + ", " + Katalogfassung.Spaltentext(spalten) +
                                              " FROM " + km + " WHERE " + Q(SPALTE_ID_KAELTEMITTEL) + " IS NULL AND " +
                                              Q(Katalogfassung.SPALTE_PRUEFSUMME) + " IS NOT NULL");
                        if (dt != null)
                            foreach (DataRow r in dt.Rows)
                                if (string.Equals(Convert.ToString(r[Katalogfassung.SPALTE_PRUEFSUMME], CultureInfo.InvariantCulture),
                                                  Katalogfassung.PruefsummeDerZeile(t, spalten, r, lese), StringComparison.Ordinal))
                                    stimmig.Add(Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture));
                        katalog += v.Ausfuehren(SQL_BEISPIEL, new DbParam("?", BEISPIEL_CODE), new DbParam("?", BEISPIEL_SCHLUESSEL),
                                                new DbParam("?", BEISPIEL_TEXT), new DbParam("?", BEISPIEL_CODE));
                    }
                    katalog += v.Ausfuehren(SqlZuordnung(KaeltemaschineSchema.TAB_STAMM));
                    projekt = v.Ausfuehren(SqlZuordnung(KaeltemaschineSchema.TAB_PROJEKT));
                    foreach (long id in stimmig)
                    {
                        DataTable dt = v.Lese("SELECT ID, " + Katalogfassung.Spaltentext(spalten) + " FROM " + km +
                                              " WHERE ID = ? AND " + Q(SPALTE_ID_KAELTEMITTEL) + " IS NOT NULL",
                                              new[] { new DbParam("?", id) });
                        if (dt == null || dt.Rows.Count == 0) continue;
                        v.Ausfuehren("UPDATE " + km + " SET " + Q(Katalogfassung.SPALTE_PRUEFSUMME) + " = ? WHERE ID = ?",
                                     new DbParam("?", Katalogfassung.PruefsummeDerZeile(t, spalten, dt.Rows[0], lese)),
                                     new DbParam("?", id));
                        summen++;
                    }
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            return (katalog, projekt, summen);
        }

        // =================================================================
        //  Stand und Ausführung
        // =================================================================

        /// <summary>Die Katalogtabelle des Registers (Kürzel <see cref="KUERZEL"/>).</summary>
        private static IReadOnlyList<Katalogtabelle> Register() =>
            Katalogfassung.Stufe3.Where(k => k.Tabelle == TAB_STAMM).ToArray();

        /// <summary>Stehen Tabelle, Katalogspalten und die angehängten Spalten?</summary>
        public static bool SchemaVollstaendig() =>
            DataRepository.TabelleVorhanden(TAB_STAMM)
            && SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte))
            && KatalogfassungSchema.KatalogspaltenVollstaendig(Register());

        /// <summary>Steht das Schema, und trägt die Tabelle jeden Code der Saat samt Katalogschlüssel und Prüfsumme?</summary>
        public static bool Vollstaendig()
        {
            if (!SchemaVollstaendig()) return false;
            foreach (Kaeltemittel k in SAAT)
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM " + Q(TAB_STAMM) + " WHERE " + Q(SPALTE_CODE) + " = ?", new DbParam("?", k.Code));
                if (o == null || o == DBNull.Value || Convert.ToInt32(o, CultureInfo.InvariantCulture) != 1) return false;
            }
            return KatalogSchluesselSaat.OffeneSaetze(Register()) == 0;
        }

        /// <summary>Wie viele Zeilen einer Tabelle tragen eine Kältemittel-ID? (Nachweis; −1 ohne Spalte.)</summary>
        public static int ZeilenMitId(string tabelle)
        {
            if (!DataRepository.SpalteVorhanden(tabelle, SPALTE_ID_KAELTEMITTEL)) return -1;
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + Q(tabelle) + " WHERE " + Q(SPALTE_ID_KAELTEMITTEL) + " IS NOT NULL");
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Führt den Schritt aus (wiederholbar): das DDL in EINEM Vorgang mit abgeschalteten Fremdschlüsseln, danach
        /// Katalogspalten und Teilindex, die Saat, Katalogschlüssel und Prüfsumme der neuen Zeilen und die Zuordnung des
        /// Textbestands in eigenen Vorgängen. Fehlt eine vorausgesetzte Tabelle, wirft er benannt.
        /// </summary>
        public static Laufergebnis Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            // Die Auskunft VOR dem Vorgang - TabelleVorhanden und SpalteVorhanden arbeiten auf einer eigenen Verbindung.
            bool tabelleFehlt = !DataRepository.TabelleVorhanden(TAB_STAMM);
            var offen = SPALTEN.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)).ToList();
            int angelegt = 0;
            if (tabelleFehlt || offen.Count > 0)
            {
                var zeilen = new List<string>();
                using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
                {
                    try
                    {
                        if (tabelleFehlt)
                        {
                            v.Ausfuehren(SqlCreateStamm());
                            zeilen.Add(TAB_STAMM + " angelegt");
                            angelegt++;
                        }
                        foreach ((string Tabelle, string Spalte, string Typ) s in offen)
                        {
                            v.Ausfuehren(Anlegen(s));
                            zeilen.Add(s.Tabelle + "." + s.Spalte + " angelegt (leer)");
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
                if (bericht != null)
                    foreach (string z in zeilen) bericht.Add(z);
            }
            foreach (KeyValuePair<string, string> a in KatalogfassungSchema.KatalogspaltenAnweisungen(Register()).ToList())
            {
                DataRepository.ExecuteNonQuery(a.Value);
                bericht?.Add(a.Key);
                angelegt++;
            }
            if (angelegt == 0) bericht?.Add("steht bereits - " + TAB_STAMM + ", Verweis der Kaeltemaschine, Inbetriebnahmejahr");

            int saat = Saat(bericht);
            KatalogSchluesselSaat.Ausfuehren(bericht, Register());
            (int katalog, int projekt, int summen) = Zuordnen();
            bericht?.Add("Kaeltemittel zugeordnet: " + katalog.ToString(CultureInfo.InvariantCulture) + " Katalogsatz/-saetze, " +
                         projekt.ToString(CultureInfo.InvariantCulture) + " Projektkopie(n), " +
                         summen.ToString(CultureInfo.InvariantCulture) + " Pruefsumme(n) neu; kein Rechenweg liest Tabelle, " +
                         "Verweis oder Jahr, der Referenzlauf bleibt byte-gleich");
            return new Laufergebnis(angelegt, saat, katalog, projekt, summen);
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
