using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KONDITIONIERUNGSVORLAGEN - Schemaschritt KP-S1v, Stufe KP1b Welle W1 (Konzept
    // Konditionierungsprofile 5.1, 5.4, 5.6 und 5.7; Entwurf KP1b Abschnitt 3 und
    // Festlegungen 1 und 2).
    //
    // WOZU. Schritt 151 (KP-S1) hat Kalender, Perioden und Vorgabezellen angelegt und die
    // Spalte ID_Vorlage schon in beide Eigentuemerregeln genommen - aber OHNE
    // Fremdschluessel, weil es die Vorlagentabelle noch nicht gab. Dieser Schritt holt vier
    // Dinge nach, die zusammengehoeren:
    //
    //   1. Tab_Konditionierungsvorlage_STAMM (STRICT, Konzept 5.7): eine Vorlage gehoert
    //      genau EINER Groesse; der Bezeichner ist 1..80 Zeichen lang, ohne Rand-
    //      Leerzeichen, und je Groesse eindeutig ohne Unterschied von Gross- und
    //      Kleinschreibung (benannter Index, NOCASE - die Rueckfallsperre; der Controller
    //      prueft getrimmt mit OrdinalIgnoreCase, NOCASE faltet nur ASCII).
    //   2. Der FREMDSCHLUESSEL ID_Vorlage -> Tab_Konditionierungsvorlage_STAMM(ID) ON DELETE
    //      CASCADE an Kalender- und Vorgabetabelle, per TABELLENNEUBAU nach dem Rezept von
    //      Schritt 96. Erst mit ihm nimmt eine geloeschte Vorlage (auch die Katalogbereinigung
    //      der Auslieferungsvorlage) Kalender, Perioden und Vorgaben mit; ohne ihn blieben
    //      Waisen, die kein foreign_key_check meldet (Festlegung 1).
    //   3. ACHT TEILINDIZES der Eindeutigkeit - die ersten im Schema: ein Kalender je
    //      Eigentuemer und Groesse (Gebaeude ohne Zone, Zone, Katalogbau) bzw. hoechstens
    //      einer je Vorlage; eine Vorgabezeile je Eigentuemer, Groesse und Zeile bzw. je
    //      Vorlage und Zeile (Festlegung 2: Teilindizes jetzt, keine Trigger).
    //   4. Die Ergebnisspalte Nachtauskuehlstunden_H an Tab_ErgebnisGebaeude und
    //      Tab_ErgebnisZone, nullbar, CHECK wie die Nachbarspalten (Konzept 5.3). Nur die
    //      Spalten - geschrieben werden sie erst mit der Nachtauskuehlung (Welle R2).
    //
    // DER ZIELTEXT WIRD NICHT ABGESCHRIEBEN - dieselbe Regel wie in den Schritten 96 und
    // 100. Er entsteht aus dem GELTENDEN sqlite_master.sql der Tabelle: hinter die EINE
    // Stelle "ID_Vorlage" INTEGER tritt die Klausel, sonst Zeichen fuer Zeichen der Bestand.
    // Steht die Stelle nicht genau einmal da, bricht der Schritt BENANNT ab.
    //
    // DIE FALLE DES UMBENENNENS. Die Kalendertabelle ist ELTERNtabelle: Die Periodentabelle
    // verweist mit ON DELETE CASCADE auf sie. Seit SQLite 3.26 schreibt
    // ALTER TABLE ... RENAME die Verweise ANDERER Tabellen mit um - auch bei
    // ausgeschalteten Fremdschluesseln -, wenn legacy_alter_table nicht eingeschaltet ist.
    // Ohne den Legacy-Modus zeigte Tab_Konditionierungsperiode danach auf
    // "Tab_Konditionierungskalender_alt", und mit dem DROP der Hilfstabelle auf NICHTS. Der
    // Schritt benennt deshalb unter "PRAGMA legacy_alter_table = ON" um und prueft vor dem
    // Commit ausdruecklich, dass die Periodentabelle namentlich auf die Kalendertabelle
    // verweist (EPOS.Kern.Tests/KonditionierungVorlagenSchemaTests haelt die Falle samt
    // Gegenprobe).
    //
    // DER DROP LOESCHT KEINE PERIODE. Bei eingeschalteten Fremdschluesseln fuehrt DROP
    // TABLE ein implizites DELETE FROM aus, und das loest die Kaskade aus. Der Umbau laeuft
    // deshalb in DataRepository.VorgangOhneFremdschluessel() - EIN Vorgang fuer den ganzen
    // Schritt: Scheitert ein Teil, bleibt die Datei, wie sie war, und der Schritt ist
    // wiederholbar.
    //
    // KEIN DML AN BESTANDSDATEN - bis auf die benannte Waisenloeschung: Eine Kalender- oder
    // Vorgabezeile, deren ID_Vorlage auf keine Vorlage zeigt, scheiterte am neuen
    // Fremdschluessel. Sie wird gezaehlt, samt ihren Perioden geloescht und im Bericht
    // genannt (erwartet 0: In KP1a schreibt kein Controller die Spalte). DUBLETTEN dagegen
    // werden NIE still geloescht: Gibt es eine, bricht der Schritt benannt ab.
    //
    // ERGEBNISNEUTRAL. Die Vorlagentabelle entsteht leer, der Neubau kopiert Zeilen mit
    // ihren IDs und Zaehlerstaenden, und die Ergebnisspalten liest der Referenzlauf nicht
    // (Referenzlauf/Ergebnisexport.cs liest Tab_ErgebnisGebaeude und Tab_ErgebnisZone
    // nicht). Der Referenzlauf bleibt byte-gleich.
    // ====================================================================================

    /// <summary>
    /// <b>Vorlagen, Fremdschlüssel und Eindeutigkeit der Konditionierung</b> — Schemaschritt
    /// KP-S1v (Nummer <see cref="SCHRITT"/>). EINE Quelle für Migrationsschritt,
    /// <c>Werkzeuge/Testdatenbankschema</c>, Testvorrichtung und Nachweis (ADR-001 Option C).
    /// Anlass, Rezept und die Falle des Umbenennens stehen im Kopf der Datei.
    /// </summary>
    public static class KonditionierungVorlagenSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht; Migration,
        /// Werkzeug, Zielstand und Tests verweisen hierher. Sie folgt lückenlos auf
        /// <see cref="KonditionierungSchema.SCHRITT"/> (Regel „lückenlos", ADR-001).
        /// </summary>
        public const int SCHRITT = KonditionierungSchema.SCHRITT + 1;

        // =================================================================
        //  Namen — sprachneutral und EINMAL
        // =================================================================

        /// <summary>Die Vorlagentabelle (Konzept 5.7).</summary>
        public const string TAB_VORLAGE = SchemaKatalog.TAB_KONDITIONIERUNGSVORLAGE_STAMM;

        /// <summary>Die Spalte, die in Kalender- und Vorgabetabelle auf die Vorlage zeigt.</summary>
        public const string SPALTE_ID_VORLAGE = KonditionierungSchema.SPALTE_ID_VORLAGE;

        /// <summary>Die Größe einer Vorlage (<see cref="DbWerte.KOND_GROESSEN"/>).</summary>
        public const string SPALTE_GROESSE = "Groesse";

        /// <summary>Der Name einer Vorlage, eindeutig je Größe.</summary>
        public const string SPALTE_BEZEICHNER = "Bezeichner";

        /// <summary>Die Beschreibung einer Vorlage.</summary>
        public const string SPALTE_BESCHREIBUNG = "Beschreibung";

        /// <summary>Die Nutzung einer Vorlage (<see cref="DbWerte.KOND_NUTZUNGEN"/>); NULL heißt „ohne".</summary>
        public const string SPALTE_NUTZUNG = "Nutzung";

        /// <summary>Das Schloss: 1 = ausgeliefert, weder löschen noch umbenennen.</summary>
        public const string SPALTE_READONLY = "ReadOnly";

        /// <summary>Die Namensregel als benannter Index: eindeutig je Größe, <c>NOCASE</c>.</summary>
        public const string INDEX_VORLAGE_NAME = "idx_KondVorlage_Name";

        /// <summary>Die Höchstzahl der Zeichen eines Vorlagennamens (Konzept 5.7).</summary>
        public const int BEZEICHNER_MAX_ZEICHEN = 80;

        /// <summary>Die Höchstzahl der Zeichen einer Beschreibung (Konzept 5.7).</summary>
        public const int BESCHREIBUNG_MAX_ZEICHEN = 400;

        /// <summary>Die Ergebnisspalte der Nachtauskühlung [h] (Konzept 3.7 und 5.3).</summary>
        public const string SPALTE_NACHTAUSKUEHLSTUNDEN = "Nachtauskuehlstunden_H";

        /// <summary>Der Zusatz, unter dem eine Tabelle für die Dauer ihres Neubaus ausweicht.</summary>
        public const string HILFSZUSATZ = "_alt";

        /// <summary>Das Ende, das jede STRICT-Tabelle trägt — die STRICT-Wache des Neubaus.</summary>
        public const string ENDE = ") STRICT";

        /// <summary>
        /// <b>Die Stelle</b> im geltenden CREATE-Text, hinter die <see cref="KLAUSEL"/> tritt — sie
        /// muss GENAU EINMAL dastehen.
        /// </summary>
        public const string STELLE = "\"" + SPALTE_ID_VORLAGE + "\" INTEGER";

        /// <summary>Die Fremdschlüsselklausel, die der Neubau hinter <see cref="STELLE"/> einsetzt.</summary>
        public const string KLAUSEL = " REFERENCES \"" + TAB_VORLAGE + "\" (\"ID\") ON DELETE CASCADE";

        /// <summary>Spaltenzahl von <c>Tab_Konditionierungsvorlage_STAMM</c> (Nachweis in den Tests).</summary>
        public const int SPALTENZAHL_VORLAGE = 6;

        /// <summary>Spaltenzahl von <c>Tab_ErgebnisGebaeude</c> nach diesem Schritt.</summary>
        public const int SPALTENZAHL_ERGEBNIS_GEBAEUDE = ErgebnisGebaeudeSchema.SPALTENZAHL_MIT_KUEHLKREIS + 1;

        /// <summary>Spaltenzahl von <c>Tab_ErgebnisZone</c> nach diesem Schritt.</summary>
        public const int SPALTENZAHL_ERGEBNIS_ZONE = ZonenkopplungSchema.SPALTENZAHL_ERGEBNIS + 1;

        // =================================================================
        //  Die DDL
        // =================================================================

        /// <summary>Die vier Nutzungen als SQL-Literal.</summary>
        public static readonly string WERTE_NUTZUNG =
            string.Join(",", DbWerte.KOND_NUTZUNGEN.Select(w => "'" + w + "'"));

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_Konditionierungsvorlage_STAMM</c> — sechs Spalten,
        /// STRICT (Konzept 5.7). Der Bezeichner ist 1 … 80 Zeichen lang und trägt keine
        /// Rand-Leerzeichen (<c>trim</c>) — sonst stünden „Büro" und „Büro " als zwei Namen in
        /// einer Liste, die kein Index auseinanderhält.
        /// </summary>
        public static readonly string SQL_CREATE_VORLAGE =
            "CREATE TABLE IF NOT EXISTS \"" + TAB_VORLAGE + "\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"Groesse\" TEXT NOT NULL CHECK (\"Groesse\" IN (" + KonditionierungSchema.WERTE_GROESSE + ")),\n" +
            "    \"Bezeichner\" TEXT NOT NULL CHECK (length(\"Bezeichner\") BETWEEN 1 AND " +
                BEZEICHNER_MAX_ZEICHEN.ToString(CultureInfo.InvariantCulture) +
                " AND \"Bezeichner\" = trim(\"Bezeichner\")),\n" +
            "    \"Beschreibung\" TEXT CHECK (\"Beschreibung\" IS NULL OR length(\"Beschreibung\") <= " +
                BESCHREIBUNG_MAX_ZEICHEN.ToString(CultureInfo.InvariantCulture) + "),\n" +
            "    \"Nutzung\" TEXT CHECK (\"Nutzung\" IS NULL OR \"Nutzung\" IN (" + WERTE_NUTZUNG + ")),\n" +
            "    \"ReadOnly\" INTEGER NOT NULL DEFAULT 0 CHECK (\"ReadOnly\" IN (0,1))\n" +
            ") STRICT";

        /// <summary>
        /// Die Namensregel: <c>UNIQUE (Groesse, Bezeichner COLLATE NOCASE)</c> als benannter Index —
        /// „Büro" darf in jeder der fünf Listen stehen, aber nur einmal je Liste.
        /// </summary>
        public static readonly string SQL_INDEX_VORLAGE_NAME =
            "CREATE UNIQUE INDEX IF NOT EXISTS \"" + INDEX_VORLAGE_NAME + "\" ON \"" + TAB_VORLAGE +
            "\" (\"" + SPALTE_GROESSE + "\", \"" + SPALTE_BEZEICHNER + "\" COLLATE NOCASE)";

        /// <summary>Die Definition der Ergebnisspalte, <b>nullbar</b>, CHECK wie <c>Sommerlueftungsstunden_H</c>.</summary>
        public const string DEFINITION_NACHTAUSKUEHLSTUNDEN =
            "INTEGER CHECK (\"" + SPALTE_NACHTAUSKUEHLSTUNDEN + "\" BETWEEN 0 AND 8760)";

        /// <summary>Die zwei Ergebnistabellen, die <see cref="SPALTE_NACHTAUSKUEHLSTUNDEN"/> bekommen.</summary>
        public static readonly IReadOnlyList<string> Ergebnistabellen = new[]
        {
            ErgebnisGebaeudeSchema.TAB, ZonenkopplungSchema.TAB_ERGEBNIS
        };

        /// <summary>Die Anweisung, die die Ergebnisspalte an einer der <see cref="Ergebnistabellen"/> anlegt.</summary>
        public static string SpalteAnlegen(string tabelle)
            => "ALTER TABLE \"" + tabelle + "\" ADD COLUMN \"" + SPALTE_NACHTAUSKUEHLSTUNDEN + "\" " +
               DEFINITION_NACHTAUSKUEHLSTUNDEN;

        /// <summary>Die zwei Tabellen, deren <c>ID_Vorlage</c> den Fremdschlüssel bekommt — in Umbaureihenfolge.</summary>
        public static readonly IReadOnlyList<string> TabellenMitVorlage = new[]
        {
            KonditionierungSchema.TAB_KALENDER, KonditionierungSchema.TAB_VORGABE
        };

        // =================================================================
        //  Die acht Teilindizes der Eindeutigkeit
        // =================================================================

        /// <summary>
        /// Ein eindeutiger TEILINDEX: Name, Tabelle, Spalten und die Bedingung, unter der er gilt.
        /// Er trägt zugleich die Dublettenzählung, die VOR seinem Anlegen läuft.
        /// </summary>
        public sealed class Teilindex
        {
            internal Teilindex(string name, string tabelle, string eigentuemer, string bedingung,
                               params string[] spalten)
            {
                Name = name;
                Tabelle = tabelle;
                Eigentuemer = eigentuemer;
                Bedingung = bedingung;
                Spalten = spalten;
            }

            /// <summary>Der Indexname.</summary>
            public string Name { get; }

            /// <summary>Die Tabelle.</summary>
            public string Tabelle { get; }

            /// <summary>Die Eigentümerart im Klartext des Berichts (ASCII, keine Anzeige).</summary>
            public string Eigentuemer { get; }

            /// <summary>Die Bedingung des Teilindex (<c>WHERE</c> ohne das Wort).</summary>
            public string Bedingung { get; }

            /// <summary>Die Spalten in Indexreihenfolge.</summary>
            public IReadOnlyList<string> Spalten { get; }

            private string Spaltenliste => string.Join(", ", Spalten.Select(s => "\"" + s + "\""));

            /// <summary><c>CREATE UNIQUE INDEX IF NOT EXISTS … WHERE …</c></summary>
            public string Anweisung
                => "CREATE UNIQUE INDEX IF NOT EXISTS \"" + Name + "\" ON \"" + Tabelle + "\" (" + Spaltenliste +
                   ") WHERE " + Bedingung;

            /// <summary>Die Zahl der Gruppen, die den Index verletzen würden (0 = er lässt sich anlegen).</summary>
            public string SqlDubletten
                => "SELECT COUNT(*) FROM (SELECT 1 FROM \"" + Tabelle + "\" WHERE " + Bedingung +
                   " GROUP BY " + Spaltenliste + " HAVING COUNT(*) > 1)";
        }

        private const string WO_GEBAEUDE = "\"ID_Gebaeude\" IS NOT NULL AND \"ID_Zone\" IS NULL";
        private const string WO_ZONE = "\"ID_Zone\" IS NOT NULL";
        private const string WO_STAMM = "\"ID_Gebaeude_Stamm\" IS NOT NULL";
        private const string WO_VORLAGE = "\"ID_Vorlage\" IS NOT NULL";

        /// <summary>
        /// <b>Die acht Teilindizes</b> in Anlegereihenfolge: je Tabelle die Eigentümerarten Gebäude
        /// ohne Zone, Zone, Katalogbau und Vorlage. Ein Zonenkalender neben einem Gebäudekalender
        /// derselben Größe ist erlaubt (verschiedene Eigentümer); eine Vorlage trägt höchstens EINEN
        /// Kalender — in ihrer Größe, die der Controller hält.
        /// </summary>
        public static readonly IReadOnlyList<Teilindex> Teilindizes = new[]
        {
            new Teilindex("idx_KondKalender_EindeutigGebaeude", KonditionierungSchema.TAB_KALENDER,
                          "Gebaeude ohne Zone", WO_GEBAEUDE, "ID_Gebaeude", "Groesse"),
            new Teilindex("idx_KondKalender_EindeutigZone", KonditionierungSchema.TAB_KALENDER,
                          "Zone", WO_ZONE, "ID_Zone", "Groesse"),
            new Teilindex("idx_KondKalender_EindeutigGebaeudeStamm", KonditionierungSchema.TAB_KALENDER,
                          "Katalogbau", WO_STAMM, "ID_Gebaeude_Stamm", "Groesse"),
            new Teilindex("idx_KondKalender_EindeutigVorlage", KonditionierungSchema.TAB_KALENDER,
                          "Vorlage", WO_VORLAGE, "ID_Vorlage"),
            new Teilindex("idx_KondVorgabe_EindeutigGebaeude", KonditionierungSchema.TAB_VORGABE,
                          "Gebaeude ohne Zone", WO_GEBAEUDE, "ID_Gebaeude", "Groesse", "Zeile"),
            new Teilindex("idx_KondVorgabe_EindeutigZone", KonditionierungSchema.TAB_VORGABE,
                          "Zone", WO_ZONE, "ID_Zone", "Groesse", "Zeile"),
            new Teilindex("idx_KondVorgabe_EindeutigGebaeudeStamm", KonditionierungSchema.TAB_VORGABE,
                          "Katalogbau", WO_STAMM, "ID_Gebaeude_Stamm", "Groesse", "Zeile"),
            new Teilindex("idx_KondVorgabe_EindeutigVorlage", KonditionierungSchema.TAB_VORGABE,
                          "Vorlage", WO_VORLAGE, "ID_Vorlage", "Zeile"),
        };

        // =================================================================
        //  Stand
        // =================================================================

        /// <summary>
        /// Kann der Datenweg den Schritt nutzen? Die Vorlagentabelle steht und beide
        /// Ergebnistabellen tragen <see cref="SPALTE_NACHTAUSKUEHLSTUNDEN"/> — die Probe der
        /// Controller und des Ergebnisschreibers; Fremdschlüssel und Indizes ändern kein Lesen.
        /// </summary>
        public static bool Lesbar()
        {
            if (!DataRepository.TabelleVorhanden(TAB_VORLAGE)) return false;
            foreach (string t in Ergebnistabellen)
                if (!DataRepository.SpalteVorhanden(t, SPALTE_NACHTAUSKUEHLSTUNDEN)) return false;
            return true;
        }

        /// <summary>
        /// Trägt <c>ID_Vorlage</c> dieser Tabelle den Fremdschlüssel auf die Vorlagentabelle?
        /// Gefragt wird <c>pragma_foreign_key_list</c> — die BEZIEHUNG ist die Frage, nicht ihre
        /// Schreibweise im CREATE-Text.
        /// </summary>
        public static bool FremdschluesselSteht(string tabelle)
            => Zahl(DataRepository.ExecuteScalar(SQL_FREMDSCHLUESSEL, ParameterFremdschluessel(tabelle))) > 0;

        /// <summary>
        /// Steht der Schritt? Die Vorlagentabelle samt Namensindex, beide Ergebnisspalten, beide
        /// Fremdschlüssel, alle acht Teilindizes und — weil der Neubau sie neu anlegt — die neun
        /// Indizes von Schritt 151.
        /// </summary>
        public static bool Vollstaendig()
        {
            if (!Lesbar()) return false;
            if (!KonditionierungSchema.Vollstaendig()) return false;
            if (!IndexSteht(INDEX_VORLAGE_NAME)) return false;
            foreach (string t in TabellenMitVorlage)
                if (!FremdschluesselSteht(t)) return false;
            foreach (Teilindex i in Teilindizes)
                if (!IndexSteht(i.Name)) return false;
            return true;
        }

        private const string SQL_FREMDSCHLUESSEL =
            "SELECT COUNT(*) FROM pragma_foreign_key_list(?) WHERE \"table\" = ? AND \"from\" = ?";

        private static DbParam[] ParameterFremdschluessel(string tabelle)
            => new[]
            {
                new DbParam("@t", tabelle),
                new DbParam("@z", TAB_VORLAGE),
                new DbParam("@s", SPALTE_ID_VORLAGE),
            };

        private const string SQL_INDEX_STEHT =
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = ?";

        private static bool IndexSteht(string name)
            => Zahl(DataRepository.ExecuteScalar(SQL_INDEX_STEHT, new DbParam("@i", name))) > 0;

        // =================================================================
        //  Der Zieltext des Neubaus
        // =================================================================

        /// <summary>
        /// Der Zieltext einer Tabelle aus <see cref="TabellenMitVorlage"/>: ihr GELTENDER
        /// <c>sqlite_master.sql</c>, hinter der EINEN <see cref="STELLE"/> um <see cref="KLAUSEL"/>
        /// ergänzt — sonst Zeichen für Zeichen der Bestand.
        ///
        /// <para>Greift die Stelle nicht — keine STRICT-Tabelle, ein anderer Kopf, die Stelle fehlt,
        /// steht zweimal da oder trägt schon eine <c>REFERENCES</c>-Klausel —, bricht der Aufruf
        /// BENANNT ab, statt eine falsche Tabelle anzulegen.</para>
        /// </summary>
        public static string Zieltext(string tabelle, string bestand)
        {
            if (string.IsNullOrEmpty(bestand))
                throw new InvalidOperationException(
                    "Zu " + tabelle + " gibt es keinen CREATE-Text in sqlite_master.");

            string text = bestand.TrimEnd();
            if (!text.EndsWith(ENDE, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Die Tabelle " + tabelle + " endet nicht auf \"" + ENDE + "\" - Schemaschritt " +
                    SCHRITT.ToString(CultureInfo.InvariantCulture) + " baut sie deshalb NICHT um.");

            string kopf = "CREATE TABLE \"" + tabelle + "\" (";
            if (!text.StartsWith(kopf, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Der CREATE-Text von " + tabelle + " hat eine andere Bauform als erwartet " +
                    "(erwartet wurde der Beginn " + kopf + ").");

            int erste = text.IndexOf(STELLE, StringComparison.Ordinal);
            int zweite = erste < 0 ? -1 : text.IndexOf(STELLE, erste + STELLE.Length, StringComparison.Ordinal);
            if (erste < 0 || zweite >= 0)
                throw new InvalidOperationException(
                    "Im CREATE-Text von " + tabelle + " steht die Stelle " + STELLE + " " +
                    (erste < 0 ? "nicht" : "mehr als einmal") + " - Schemaschritt " +
                    SCHRITT.ToString(CultureInfo.InvariantCulture) + " setzt den Fremdschluessel " +
                    "nur an GENAU EINER Stelle ein und baut die Tabelle deshalb NICHT um.");

            int hinter = erste + STELLE.Length;
            string rest = text.Substring(hinter);
            if (rest.Length == 0 || (rest[0] != ',' && !char.IsWhiteSpace(rest[0])) ||
                rest.TrimStart().StartsWith("REFERENCES", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Hinter " + STELLE + " steht im CREATE-Text von " + tabelle + " nicht das Ende der " +
                    "Spaltendefinition, oder die Spalte traegt schon einen Verweis - Schemaschritt " +
                    SCHRITT.ToString(CultureInfo.InvariantCulture) + " baut die Tabelle NICHT um.");

            return text.Substring(0, hinter) + KLAUSEL + rest;
        }

        // =================================================================
        //  Ausführung
        // =================================================================

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang mit abgeschalteten Fremdschlüsseln aus — für die
        /// Migration der Schale, <c>Werkzeuge/Testdatenbankschema</c> und <c>EPOS.Kern.Tests</c>.
        /// <b>Wiederholbar:</b> Jeder Teil prüft seinen Stand; steht alles, öffnet er keinen Vorgang.
        ///
        /// <para><b>Benannter Abbruch</b> (<see cref="InvalidOperationException"/>, nichts geändert):
        /// Dubletten unter einem der Teilindizes, eine Stelle, die nicht genau einmal dasteht, ein
        /// nicht leerer <c>foreign_key_check</c> nach dem Neubau oder eine Periodentabelle, die nicht
        /// mehr namentlich auf die Kalendertabelle verweist. Fehlen die Tabellen der Schritte 151,
        /// 107 oder 147, tut er nichts und sagt es.</para>
        /// </summary>
        /// <param name="bericht">Nimmt Zeilen auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der Änderungen am Schema (Tabelle, Neubauten, Indizes, Spalten); 0, wenn alles stand.</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            string nr = SCHRITT.ToString(CultureInfo.InvariantCulture);

            // Die Auskunft VOR dem Vorgang - TabelleVorhanden arbeitet auf einer eigenen
            // Verbindung und saehe die offene Transaktion nicht.
            if (!KonditionierungSchema.Lesbar())
            {
                bericht?.Add("Die Tabellen des Schritts " +
                             KonditionierungSchema.SCHRITT.ToString(CultureInfo.InvariantCulture) +
                             " fehlen - nichts angelegt");
                return 0;
            }
            foreach (string t in Ergebnistabellen)
                if (!DataRepository.TabelleVorhanden(t))
                {
                    bericht?.Add("Die Tabelle " + t + " fehlt - nichts angelegt");
                    return 0;
                }

            if (Vollstaendig())
            {
                bericht?.Add("steht bereits - Vorlagentabelle, beide Fremdschluessel, acht Teilindizes " +
                             "und beide Ergebnisspalten; nichts zu tun");
                return 0;
            }

            int aenderungen = 0;
            var zeilen = new List<string>();

            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    // ---- 1. Die Vorlagentabelle und ihre Namensregel.
                    bool vorlageNeu = !ObjektSteht(v, "table", TAB_VORLAGE);
                    v.Ausfuehren(SQL_CREATE_VORLAGE);
                    bool nameNeu = !ObjektSteht(v, "index", INDEX_VORLAGE_NAME);
                    v.Ausfuehren(SQL_INDEX_VORLAGE_NAME);
                    if (vorlageNeu) aenderungen++;
                    if (nameNeu) aenderungen++;
                    zeilen.Add(TAB_VORLAGE + (vorlageNeu ? " angelegt (leer)" : " stand bereits") +
                               ", Namensregel " + INDEX_VORLAGE_NAME + (nameNeu ? " angelegt" : " stand bereits"));

                    // ---- 2a. Die Waisen an ID_Vorlage - nur, wo der Fremdschluessel noch fehlt.
                    var offen = TabellenMitVorlage.Where(t => !FremdschluesselImVorgang(v, t)).ToList();
                    foreach (string t in offen)
                    {
                        string waise = "\"" + SPALTE_ID_VORLAGE + "\" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM \"" +
                                       TAB_VORLAGE + "\" w WHERE w.\"ID\" = \"" + t + "\".\"" + SPALTE_ID_VORLAGE + "\")";
                        long perioden = 0;
                        if (string.Equals(t, KonditionierungSchema.TAB_KALENDER, StringComparison.Ordinal))
                            // Die Kaskade von Hand - die Fremdschluessel sind ja aus.
                            perioden = v.Ausfuehren("DELETE FROM \"" + KonditionierungSchema.TAB_PERIODE +
                                                    "\" WHERE \"" + KonditionierungSchema.SPALTE_ID_KALENDER +
                                                    "\" IN (SELECT \"ID\" FROM \"" + t + "\" WHERE " + waise + ")");
                        long geloescht = v.Ausfuehren("DELETE FROM \"" + t + "\" WHERE " + waise);
                        zeilen.Add(t + ": Zeilen mit ID_Vorlage ohne Vorlage " + Text(geloescht) + " geloescht" +
                                   (string.Equals(t, KonditionierungSchema.TAB_KALENDER, StringComparison.Ordinal)
                                       ? ", ihre Perioden " + Text(perioden) : "") + " (erwartet 0)");
                    }

                    // ---- 2b. Die Dubletten - VOR jedem Umbau; nie still geloescht.
                    var dubletten = new List<string>();
                    foreach (Teilindex i in Teilindizes)
                    {
                        if (ObjektSteht(v, "index", i.Name)) continue;
                        long gruppen = Zahl(v.Skalar(i.SqlDubletten));
                        if (gruppen > 0)
                            dubletten.Add(i.Tabelle + " je " + i.Eigentuemer + " (" + i.Name + "): " +
                                          Text(gruppen) + " Gruppe(n)");
                    }
                    if (dubletten.Count > 0)
                        throw new InvalidOperationException(
                            "Schemaschritt " + nr + " bricht ab: Dubletten unter den Teilindizes der " +
                            "Eindeutigkeit - " + string.Join("; ", dubletten) + ". Der Schritt loescht keine " +
                            "Dublette still; nichts wurde geaendert, und er ist wiederholbar, sobald sie " +
                            "geklaert sind.");

                    // ---- 2c. Der Neubau je offener Tabelle.
                    foreach (string t in offen)
                    {
                        zeilen.Add(Neubau(v, t));
                        aenderungen++;
                    }

                    // ---- 3. Die neun Indizes von Schritt 151 (der Neubau legt sie schon an; hier fuer
                    //      eine Datei, deren Fremdschluessel stand, der aber ein Index fehlt) und die
                    //      acht Teilindizes.
                    foreach (KeyValuePair<string, string> a in KonditionierungSchema.Indexanweisungen)
                        v.Ausfuehren(a.Value);
                    int indizes = 0;
                    foreach (Teilindex i in Teilindizes)
                    {
                        bool neu = !ObjektSteht(v, "index", i.Name);
                        v.Ausfuehren(i.Anweisung);
                        if (neu) indizes++;
                    }
                    aenderungen += indizes;
                    zeilen.Add(Text(indizes) + " von " + Text(Teilindizes.Count) +
                               " Teilindizes der Eindeutigkeit angelegt");

                    // ---- 4. Die Ergebnisspalten.
                    int spalten = 0;
                    foreach (string t in Ergebnistabellen)
                    {
                        if (SpalteImVorgang(v, t, SPALTE_NACHTAUSKUEHLSTUNDEN)) continue;
                        v.Ausfuehren(SpalteAnlegen(t));
                        spalten++;
                    }
                    aenderungen += spalten;
                    zeilen.Add(Text(spalten) + " von " + Text(Ergebnistabellen.Count) + " Spalte(n) " +
                               SPALTE_NACHTAUSKUEHLSTUNDEN + " angelegt (nullbar)");

                    // ---- Die Zeugen NACHHER, noch INNERHALB der Transaktion.
                    Nachpruefen(v);

                    v.Commit();
                }
                finally
                {
                    // DER LEGACY-MODUS DARF DIE VERBINDUNG NICHT UEBERLEBEN - die Klammer fuer
                    // einen Abbruch zwischen den beiden PRAGMAs (Muster aus Schritt 96).
                    try { v.Ausfuehren("PRAGMA legacy_alter_table = OFF"); }
                    catch (Exception) { /* nach Commit abgeschlossen - dann stand er schon aus */ }
                }
            }

            if (bericht != null)
                foreach (string z in zeilen) bericht.Add(z);
            bericht?.Add("foreign_key_check leer, " + KonditionierungSchema.TAB_PERIODE + " verweist namentlich auf " +
                         KonditionierungSchema.TAB_KALENDER + "; KEIN DML an Bestandsdaten, der Referenzlauf " +
                         "bleibt byte-gleich");
            return aenderungen;
        }

        /// <summary>
        /// Das Tabellenneubau-Rezept aus Schritt 96: Die alte Tabelle weicht UNTER
        /// <c>legacy_alter_table = ON</c> auf den Hilfsnamen aus, die neue entsteht unter dem
        /// echten Namen, die Zeilen ziehen namentlich mit ihren IDs um, der AUTOINCREMENT-Stand
        /// reist mit, die alte fällt (Fremdschlüssel aus — keine Kaskade), die Indizes stehen
        /// wieder: die des Bestands Wort für Wort, dazu die neun von Schritt 151.
        /// </summary>
        /// <returns>Die Berichtszeile.</returns>
        private static string Neubau(DbVorgang v, string tabelle)
        {
            string alt = tabelle + HILFSZUSATZ;

            string bestand = TextVon(v.Skalar(
                "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("@t", tabelle)));
            string ziel = Zieltext(tabelle, bestand);

            // Spalten NAMENTLICH - "SELECT *" haengt an der Reihenfolge zweier Schemastaende.
            var namen = new List<string>();
            DataTable info = v.Lese("SELECT name FROM pragma_table_info(?)", new DbParam("@t", tabelle));
            foreach (DataRow zeile in info.Rows)
                namen.Add("\"" + Convert.ToString(zeile["name"], CultureInfo.InvariantCulture) + "\"");
            string spaltenliste = string.Join(", ", namen);

            // Die Indizes der Tabelle, Wort fuer Wort der Bestand. Sie wandern beim Umbenennen
            // mit und fallen mit der Hilfstabelle; danach entstehen sie neu.
            var indizes = new List<string>();
            DataTable idx = v.Lese(
                "SELECT sql FROM sqlite_master WHERE type = 'index' AND tbl_name = ? AND sql IS NOT NULL",
                new DbParam("@t", tabelle));
            foreach (DataRow zeile in idx.Rows)
                indizes.Add(Convert.ToString(zeile["sql"], CultureInfo.InvariantCulture));

            object standWert = v.Skalar("SELECT seq FROM sqlite_sequence WHERE name = ?", new DbParam("@t", tabelle));
            long zeilenVorher = Zahl(v.Skalar("SELECT COUNT(*) FROM \"" + tabelle + "\""));

            v.Ausfuehren("DROP TABLE IF EXISTS \"" + alt + "\"");

            // DIE FALLE: Ohne den Legacy-Modus schriebe SQLite den Verweis der Periodentabelle
            // auf den Hilfsnamen um (Kopf der Datei).
            v.Ausfuehren("PRAGMA legacy_alter_table = ON");
            v.Ausfuehren("ALTER TABLE \"" + tabelle + "\" RENAME TO \"" + alt + "\"");
            v.Ausfuehren("PRAGMA legacy_alter_table = OFF");

            v.Ausfuehren(ziel);
            v.Ausfuehren("INSERT INTO \"" + tabelle + "\" (" + spaltenliste + ") SELECT " +
                         spaltenliste + " FROM \"" + alt + "\"");
            v.Ausfuehren("DROP TABLE \"" + alt + "\"");

            // DER ZAEHLER. Ohne ihn fiele der AUTOINCREMENT-Stand auf die groesste kopierte ID
            // zurueck, und eine vergebene Id kaeme ein zweites Mal heraus.
            v.Ausfuehren("DELETE FROM sqlite_sequence WHERE name = ?", new DbParam("@t", alt));
            v.Ausfuehren("DELETE FROM sqlite_sequence WHERE name = ?", new DbParam("@t", tabelle));
            if (standWert != null && standWert != DBNull.Value)
                v.Ausfuehren("INSERT INTO sqlite_sequence (name, seq) VALUES (?, ?)",
                             new DbParam("@t", tabelle),
                             new DbParam("@s", Convert.ToInt64(standWert, CultureInfo.InvariantCulture)));

            foreach (string anweisung in indizes)
                v.Ausfuehren(ProjektFremdschluessel.MitIfNotExists(anweisung));
            foreach (KeyValuePair<string, string> a in KonditionierungSchema.Indexanweisungen)
                if (a.Value.Contains("ON \"" + tabelle + "\"", StringComparison.Ordinal))
                    v.Ausfuehren(a.Value);

            long zeilenNachher = Zahl(v.Skalar("SELECT COUNT(*) FROM \"" + tabelle + "\""));
            if (zeilenNachher != zeilenVorher)
                throw new InvalidOperationException(
                    "Der Neubau von " + tabelle + " traegt " + Text(zeilenNachher) + " statt " +
                    Text(zeilenVorher) + " Zeilen - Schemaschritt " + SCHRITT.ToString(CultureInfo.InvariantCulture) +
                    " nimmt ihn zurueck.");

            return tabelle + ": Fremdschluessel " + SPALTE_ID_VORLAGE + " -> " + TAB_VORLAGE +
                   " ON DELETE CASCADE per Neubau gesetzt; Zeilen " + Text(zeilenVorher) + " -> " +
                   Text(zeilenNachher) + ", IDs und Zaehlerstand erhalten, " + Text(indizes.Count) +
                   " Index(e) des Bestands wieder angelegt";
        }

        /// <summary>
        /// Die Zeugen vor dem Commit: <c>foreign_key_check</c> leer für die drei Tabellen der
        /// Konditionierung und die Vorlagentabelle, und die Periodentabelle verweist NAMENTLICH auf
        /// die Kalendertabelle — nicht auf deren Hilfsnamen (die Falle aus dem Kopf der Datei).
        /// </summary>
        private static void Nachpruefen(DbVorgang v)
        {
            foreach (string t in new[]
                     {
                         KonditionierungSchema.TAB_KALENDER, KonditionierungSchema.TAB_PERIODE,
                         KonditionierungSchema.TAB_VORGABE, TAB_VORLAGE
                     })
            {
                long verletzt = Zahl(v.Skalar("SELECT COUNT(*) FROM pragma_foreign_key_check(?)", new DbParam("@t", t)));
                if (verletzt > 0)
                    throw new InvalidOperationException(
                        "Nach dem Umbau meldet foreign_key_check fuer " + t + " " + Text(verletzt) +
                        " verletzte Zeile(n) - Schemaschritt " + SCHRITT.ToString(CultureInfo.InvariantCulture) +
                        " nimmt alles zurueck.");
            }

            long namentlich = Zahl(v.Skalar(
                "SELECT COUNT(*) FROM pragma_foreign_key_list(?) WHERE \"table\" = ? AND \"from\" = ?",
                new DbParam("@t", KonditionierungSchema.TAB_PERIODE),
                new DbParam("@z", KonditionierungSchema.TAB_KALENDER),
                new DbParam("@s", KonditionierungSchema.SPALTE_ID_KALENDER)));
            string text = TextVon(v.Skalar(
                "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                new DbParam("@t", KonditionierungSchema.TAB_PERIODE))) ?? "";
            if (namentlich != 1 || text.Contains(HILFSZUSATZ + "\"", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    KonditionierungSchema.TAB_PERIODE + " verweist nach dem Neubau nicht mehr namentlich auf " +
                    KonditionierungSchema.TAB_KALENDER + " (legacy_alter_table) - Schemaschritt " +
                    SCHRITT.ToString(CultureInfo.InvariantCulture) + " nimmt alles zurueck.");
        }

        // =================================================================
        //  Kleinkram - Auskunft INNERHALB des Vorgangs
        // =================================================================

        private static bool ObjektSteht(DbVorgang v, string art, string name)
            => Zahl(v.Skalar("SELECT COUNT(*) FROM sqlite_master WHERE type = ? AND name = ?",
                             new DbParam("@a", art), new DbParam("@n", name))) > 0;

        private static bool FremdschluesselImVorgang(DbVorgang v, string tabelle)
            => Zahl(v.Skalar(SQL_FREMDSCHLUESSEL, ParameterFremdschluessel(tabelle))) > 0;

        private static bool SpalteImVorgang(DbVorgang v, string tabelle, string spalte)
            => Zahl(v.Skalar("SELECT COUNT(*) FROM pragma_table_info(?) WHERE name = ?",
                             new DbParam("@t", tabelle), new DbParam("@s", spalte))) > 0;

        private static long Zahl(object wert)
            => wert == null || wert == DBNull.Value ? 0 : Convert.ToInt64(wert, CultureInfo.InvariantCulture);

        private static string Text(long zahl) => zahl.ToString(CultureInfo.InvariantCulture);

        private static string TextVon(object wert)
            => wert == null || wert == DBNull.Value ? null : Convert.ToString(wert, CultureInfo.InvariantCulture);
    }
}
