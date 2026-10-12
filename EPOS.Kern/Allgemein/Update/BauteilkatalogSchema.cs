using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // BK-2 - SCHEMA UND FELDER DES BAUSTOFF- UND BAUTEILKATALOGS (Entscheid E132, Anwender 12.10.2026;
    // Entwurf Bauteilkatalog 2.1, 2.3, 5.1, 6a; Feldinventar "Folgerung fuer BK-2").
    //
    // WAS. Der Baustoff- und der Aufbaukatalog nehmen die fachlich belegten Felder einer Fremdquelle auf (F2), werden
    // zweistufig in Gruppe und Klasse gegliedert (F3), und ein Aufbau traegt das Kennzeichen "fuer die Gebaeudesimulation
    // meist nicht geeignet" (F3). Im Einzelnen:
    //
    //   1. HERKUNFT. Die Wertliste von Herkunft an Tab_Baustoff(_STAMM) und Tab_Bauteilaufbau(_STAMM) bekommt EIGEN und
    //      IMPORT; die Quelle darf 400 Zeichen lang werden (eine zusammengefuehrte Dublette nennt alle Teilkataloge, F4).
    //      SQLite kann einem CHECK nichts nachtragen -> Neubau der vier Tabellen nach dem Rezept des Hauses
    //      (TwwBezugsartSchema.Neubau: IDs, AUTOINCREMENT-Stand, Indizes, Fremdschluessel der Kinder bleiben). Tab_Zone und
    //      Tab_Bauteil behalten ihre Liste - sie sind kein Katalog.
    //   2. QUELLKENNUNG. Eindeutig je Herkunft IMPORT (Teilindex; in der Projektkopie je Projekt) - der Wiederimport
    //      erkennt seinen Satz an der GUID des Quellsatzes.
    //   3. GLIEDERUNG. Gruppe_Code und Klasse_Code an Baustoff und Aufbau: FESTE CODES ALS SPALTEN MIT CHECK, keine
    //      Texttabelle. Gruende: (a) die Codes sind ein Wertebereich wie Bauteilart, Typaufbau und Herkunft derselben
    //      Tabellen, keine Beziehung auf einen gepflegten Satz; (b) die Anzeigetexte gehoeren in beide Ressourcensprachen,
    //      eine Texttabelle waere einsprachig; (c) die Abbildung der Fremdcodes (Entwurf 2.3, Regeln 5 und 6) liegt fest im
    //      Kern (Bauteilkatalog), die Liste aendert sich nur mit einem Schemaschritt - wie jede Wertliste des Hauses.
    //   4. KENNZEICHEN. Simulation_Ungeeignet (0/1, NOT NULL, vom Kern gesetzt): eine Schicht ohne lambda oder ein Aufbau
    //      ohne Schicht (ein Fenster, eine Vorhangfassade oder eine Tuer mit Quell-U ausgenommen).
    //   5. ABSCHNITTE. Eigene Tabelle Tab_Bauteilabschnitt(_STAMM) (Nummer, Flaechenanteil 0..1, Text, Quellkennung; FK zum
    //      Aufbau mit Kaskade) und an der Schicht ID_Abschnitt (NULL = homogen). Grund: Flaechenanteil und Text gehoeren
    //      dem Abschnitt, nicht jeder seiner Schichten (die Quelle fuehrt sie ebenso an einer eigenen Zeile), und die
    //      Beziehung laeuft ueber die ID (Hausregel). Die Rechnung nach DIN EN ISO 6946 6.7 baut BK-5.
    //   6. FELDER DER QUELLE als nullbare Spalten mit Bereichs-CHECK (Listen BAUSTOFF_SPALTEN, AUFBAU_SPALTEN,
    //      SCHICHT_SPALTEN). Schalter sind 0/1 mit CHECK (spalte IN (0,1)) und nullbar: NULL heisst "nicht angegeben".
    //      Codes der Quelle (Baustoff-, Oberflaechen-, Schicht-, Estrich-, Belueftungs-, Dickenart) stehen als ganze Zahl.
    //
    // BESTAND. Katalog: ReadOnly = 1 -> Herkunft VORGABE; ReadOnly = 0 mit Herkunft leer, MANUELL oder VORGABE -> EIGEN
    // (eine Importherkunft IFC/GBXML/SQPROJ/KATALOG bleibt, sie sagt, woher der Satz kam). Projektkopien behalten ihre
    // Herkunft. Gruppe aus der Stoffgruppe (Baustoff) bzw. Gruppe und Klasse aus Bauteilart und Typaufbau (Aufbau), der
    // Rest bleibt leer. Das Kennzeichen rechnet der Schritt mit derselben Regel wie der Kern. Die Pruefsumme jedes
    // Katalogsatzes, dessen Summe vorher stimmte, wird neu gestempelt (Gliederung und Kennzeichen sind Fachspalten).
    //
    // ERGEBNISNEUTRAL. Der Bauteilweg liest weiterhin nur lambda, rho, c, Dicke, Luftschicht, U und die Fensterwerte am
    // Bauteil; keine der neuen Spalten.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in EPOS.Kern.Tests und die
    // Paketanhebung (Art Ddl: ein aelteres Paket bringt seine Projektkopien ohne die neuen Felder).
    // ====================================================================================

    /// <summary>
    /// <b>BK-2</b> — Schema und Felder des Baustoff- und Bauteilkatalogs (E132). EINE Quelle für Migration, Werkzeug,
    /// Testkopie und Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class BauteilkatalogSchema
    {
        /// <summary><b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht (221, hängt an 220).</summary>
        public const int SCHRITT = RaumnutzungNormwerteSchema.SCHRITT + 1;

        // ---- Tabellen ------------------------------------------------------------------

        /// <summary>Baustoffkatalog.</summary>
        public const string TAB_BAUSTOFF_STAMM = SchemaKatalog.TAB_BAUSTOFF_STAMM;
        /// <summary>Baustoffe der Projekte.</summary>
        public const string TAB_BAUSTOFF = SchemaKatalog.TAB_BAUSTOFF;
        /// <summary>Aufbaukatalog.</summary>
        public const string TAB_AUFBAU_STAMM = SchemaKatalog.TAB_BAUTEILAUFBAU_STAMM;
        /// <summary>Aufbauten der Projekte.</summary>
        public const string TAB_AUFBAU = SchemaKatalog.TAB_BAUTEILAUFBAU;
        /// <summary>Schichten des Katalogs.</summary>
        public const string TAB_SCHICHT_STAMM = SchemaKatalog.TAB_BAUTEILSCHICHT_STAMM;
        /// <summary>Schichten der Projekte.</summary>
        public const string TAB_SCHICHT = SchemaKatalog.TAB_BAUTEILSCHICHT;
        /// <summary>Abschnitte inhomogener Schichten des Katalogs (neu).</summary>
        public const string TAB_ABSCHNITT_STAMM = "Tab_Bauteilabschnitt_STAMM";
        /// <summary>Abschnitte inhomogener Schichten der Projekte (neu).</summary>
        public const string TAB_ABSCHNITT = "Tab_Bauteilabschnitt";

        // ---- Herkunft --------------------------------------------------------------------

        /// <summary>Die Herkunftsklausel vor dem Schritt (sechs Werte, Schritt 200).</summary>
        public const string CHECK_HERKUNFT_ALT = "CHECK (\"Herkunft\" IN ('MANUELL','KATALOG','IFC','GBXML','VORGABE','SQPROJ'))";

        /// <summary>Die Herkunftsklausel der vier Katalogtabellen nach dem Schritt (plus EIGEN und IMPORT).</summary>
        public const string CHECK_HERKUNFT_NEU =
            "CHECK (\"Herkunft\" IN ('MANUELL','KATALOG','IFC','GBXML','VORGABE','SQPROJ','" + HERKUNFT_EIGEN + "','" + HERKUNFT_IMPORT + "'))";

        /// <summary>Die Längenklausel der Quelle vor dem Schritt.</summary>
        public const string CHECK_QUELLE_ALT = "CHECK (length(\"Quelle\") <= 120)";

        /// <summary>Die Höchstlänge der Quelle nach dem Schritt.</summary>
        public const int QUELLE_MAX_ZEICHEN = 400;

        /// <summary>Die Längenklausel der Quelle nach dem Schritt.</summary>
        public const string CHECK_QUELLE_NEU = "CHECK (length(\"Quelle\") <= 400)";

        /// <summary>Herkunft „eigener Satz des Anwenders“ (Persistenzwert, eingefroren).</summary>
        public const string HERKUNFT_EIGEN = "EIGEN";

        /// <summary>Herkunft „aus einer Fremdquelle eingelesen“ (Persistenzwert, eingefroren).</summary>
        public const string HERKUNFT_IMPORT = "IMPORT";

        /// <summary>Herkunft „ausgeliefert“ (vorhandener Wert).</summary>
        public const string HERKUNFT_VORGABE = DbWerte.HERKUNFT_VORGABE;

        // ---- Spaltennamen, die der Kern nennt --------------------------------------------

        /// <summary>Gruppe (Code).</summary>
        public const string SPALTE_GRUPPE = "Gruppe_Code";
        /// <summary>Klasse (Code).</summary>
        public const string SPALTE_KLASSE = "Klasse_Code";
        /// <summary>Kennzeichen „für die Gebäudesimulation meist nicht geeignet“ (0/1).</summary>
        public const string SPALTE_UNGEEIGNET = "Simulation_Ungeeignet";
        /// <summary>U-Wert der Quelle [W/(m²K)].</summary>
        public const string SPALTE_U_QUELLE = "U_Quelle";
        /// <summary>Abschnitt der Schicht (ID; NULL = homogen).</summary>
        public const string SPALTE_ID_ABSCHNITT = "ID_Abschnitt";

        // ---- Gliederung (Codes, eingefroren) -------------------------------------------

        /// <summary>Die Gruppen der Baustoffe (Code-Suffix der Ressourcen <c>BST_GRUPPE_*</c>).</summary>
        public static readonly IReadOnlyList<string> BAUSTOFF_GRUPPEN = new[]
        {
            "MAUERWERK", "BETON", "ESTRICHE", "PUTZE", "DAEMMSTOFFE", "HOLZ", "PLATTEN", "BODENBELAEGE",
            "ABDICHTUNGEN", "METALLE", "ERDREICH", "SONSTIGE"
        };

        /// <summary>Die Klassen der Baustoffe je Gruppe.</summary>
        public static readonly IReadOnlyList<(string Gruppe, string Klasse)> BAUSTOFF_KLASSEN = new[]
        {
            ("MAUERWERK", "ZIEGEL"), ("MAUERWERK", "KALKSANDSTEIN"), ("MAUERWERK", "PORENBETON"),
            ("MAUERWERK", "LEICHTBETONSTEIN"), ("MAUERWERK", "NATURSTEIN"), ("MAUERWERK", "MAUERWERK_SONSTIGE"),
            ("BETON", "NORMALBETON"), ("BETON", "STAHLBETON"), ("BETON", "LEICHTBETON"),
            ("ESTRICHE", "ZEMENTESTRICH"), ("ESTRICHE", "CALCIUMSULFATESTRICH"), ("ESTRICHE", "GUSSASPHALT"),
            ("ESTRICHE", "TROCKENESTRICH"),
            ("PUTZE", "KALKPUTZ"), ("PUTZE", "ZEMENTPUTZ"), ("PUTZE", "GIPSPUTZ"), ("PUTZE", "LEHMPUTZ"),
            ("PUTZE", "DAEMMPUTZ"), ("PUTZE", "MOERTEL"),
            ("DAEMMSTOFFE", "MINERALWOLLE"), ("DAEMMSTOFFE", "POLYSTYROL"), ("DAEMMSTOFFE", "POLYURETHAN"),
            ("DAEMMSTOFFE", "HOLZFASER"), ("DAEMMSTOFFE", "NATURFASER"), ("DAEMMSTOFFE", "SCHAUMGLAS"),
            ("DAEMMSTOFFE", "VAKUUMDAEMMUNG"), ("DAEMMSTOFFE", "DAEMMSTOFF_SONSTIGE"),
            ("HOLZ", "VOLLHOLZ"), ("HOLZ", "HOLZWERKSTOFF"),
            ("PLATTEN", "GIPSPLATTE"), ("PLATTEN", "ZEMENTPLATTE"), ("PLATTEN", "PLATTE_SONSTIGE"),
            ("BODENBELAEGE", "KERAMIK"), ("BODENBELAEGE", "PARKETT"), ("BODENBELAEGE", "TEXTILBELAG"),
            ("BODENBELAEGE", "KUNSTSTOFFBELAG"),
            ("ABDICHTUNGEN", "BITUMEN"), ("ABDICHTUNGEN", "KUNSTSTOFFBAHN"), ("ABDICHTUNGEN", "DAMPFBREMSE"),
            ("METALLE", "STAHL"), ("METALLE", "ALUMINIUM"), ("METALLE", "KUPFER"), ("METALLE", "GLAS"),
            ("ERDREICH", "BINDIG"), ("ERDREICH", "NICHTBINDIG"), ("ERDREICH", "FELS"),
            ("SONSTIGE", "LUFT"), ("SONSTIGE", "STOFF_SONSTIGE")
        };

        /// <summary>Die Gruppen der Bauteilaufbauten.</summary>
        public static readonly IReadOnlyList<string> AUFBAU_GRUPPEN = new[]
        {
            "WAENDE", "DECKEN", "DAECHER", "OEFFNUNGEN", "SONSTIGE"
        };

        /// <summary>Die Klassen der Bauteilaufbauten je Gruppe.</summary>
        public static readonly IReadOnlyList<(string Gruppe, string Klasse)> AUFBAU_KLASSEN = new[]
        {
            ("WAENDE", "AUSSENWAND"), ("WAENDE", "AUSSENWAND_ERDREICH"), ("WAENDE", "INNENWAND"),
            ("DECKEN", "GESCHOSSDECKE"), ("DECKEN", "DECKE_OBEN"), ("DECKEN", "DECKE_UNTEN"), ("DECKEN", "BODENPLATTE"),
            ("DAECHER", "FLACHDACH"), ("DAECHER", "STEILDACH"),
            ("OEFFNUNGEN", "FENSTER"), ("OEFFNUNGEN", "DACHFENSTER"), ("OEFFNUNGEN", "VORHANGFASSADE"), ("OEFFNUNGEN", "TUER"),
            ("SONSTIGE", "SONSTIGES")
        };

        // ---- Spalten ---------------------------------------------------------------------

        /// <summary>Art einer neuen Spalte: Zahl, ganze Zahl, Schalter (0/1), Text oder Code.</summary>
        public enum Spaltenart { Zahl, Ganz, Schalter, Text, Code, Kennzeichen, Verweis }

        /// <summary>Eine neue Spalte: Name, Art und Typ samt Prüfklausel.</summary>
        public sealed class Spalte
        {
            internal Spalte(string name, Spaltenart art, string typ)
            {
                Name = name;
                Art = art;
                Typ = typ;
            }

            /// <summary>Der Spaltenname.</summary>
            public string Name { get; }

            /// <summary>Die Art (für die Leser des Kerns).</summary>
            public Spaltenart Art { get; }

            /// <summary>Typ und Prüfklausel, wie <c>ADD COLUMN</c> sie anlegt.</summary>
            public string Typ { get; }
        }

        private static string Q(string s) => "\"" + s + "\"";

        private static string Liste(IEnumerable<string> werte) => string.Join(",", werte.Select(w => "'" + w + "'"));

        private static Spalte Zahl(string n, string bedingung = null) =>
            new Spalte(n, Spaltenart.Zahl, "REAL" + (bedingung == null ? "" : " CHECK (" + Q(n) + " IS NULL OR (" + bedingung.Replace("@", Q(n)) + "))"));

        private static Spalte NichtNegativ(string n) => Zahl(n, "@ >= 0");

        private static Spalte Positiv(string n) => Zahl(n, "@ > 0");

        private static Spalte Anteil(string n) => Zahl(n, "@ >= 0 AND @ <= 1");

        private static Spalte Code(string n) =>
            new Spalte(n, Spaltenart.Ganz, "INTEGER CHECK (" + Q(n) + " IS NULL OR " + Q(n) + " >= 0)");

        private static Spalte Schalter(string n) =>
            new Spalte(n, Spaltenart.Schalter, "INTEGER CHECK (" + Q(n) + " IN (0,1))");

        private static Spalte Text(string n, int max) =>
            new Spalte(n, Spaltenart.Text, "TEXT CHECK (" + Q(n) + " IS NULL OR length(" + Q(n) + ") <= " +
                                           max.ToString(CultureInfo.InvariantCulture) + ")");

        private static Spalte Gliederung(string n, IEnumerable<string> werte) =>
            new Spalte(n, Spaltenart.Code, "TEXT CHECK (" + Q(n) + " IS NULL OR " + Q(n) + " IN (" + Liste(werte) + "))");

        /// <summary>Die Höchstlänge eines Infotexts.</summary>
        public const int INFOTEXT_MAX_ZEICHEN = 4000;

        /// <summary>Die Höchstlänge eines Langtexts.</summary>
        public const int LANGTEXT_MAX_ZEICHEN = 1000;

        /// <summary>Die neuen Spalten von <c>Tab_Baustoff(_STAMM)</c> in Anlegereihenfolge.</summary>
        public static readonly IReadOnlyList<Spalte> BAUSTOFF_SPALTEN = new[]
        {
            Gliederung(SPALTE_GRUPPE, BAUSTOFF_GRUPPEN),
            Gliederung(SPALTE_KLASSE, BAUSTOFF_KLASSEN.Select(k => k.Klasse)),
            Text("Langtext", LANGTEXT_MAX_ZEICHEN),
            Text("Infotext", INFOTEXT_MAX_ZEICHEN),
            Positiv("Lambda_Bemessung"),
            NichtNegativ("Mue_Min"),
            NichtNegativ("Mue_Max"),
            Code("Baustoffart_Code"),
            Code("Oberflaechenart_Code"),
            Schalter("Dampfdicht"),
            Schalter("Waermereflektierend"),
            Schalter("Kapillaraktiv"),
            NichtNegativ("Feuchtegehalt"),
            Zahl("Temperatur_Max"),
            NichtNegativ("Rauheit"),
            Zahl("Laengenausdehnung"),
            Zahl("OI3_AP"),
            Zahl("OI3_PEI"),
            Zahl("OI3_GWP")
        };

        /// <summary>Die neuen Spalten von <c>Tab_Bauteilaufbau(_STAMM)</c> in Anlegereihenfolge.</summary>
        public static readonly IReadOnlyList<Spalte> AUFBAU_SPALTEN = new[]
        {
            Gliederung(SPALTE_GRUPPE, AUFBAU_GRUPPEN),
            Gliederung(SPALTE_KLASSE, AUFBAU_KLASSEN.Select(k => k.Klasse)),
            new Spalte(SPALTE_UNGEEIGNET, Spaltenart.Kennzeichen,
                       "INTEGER NOT NULL DEFAULT 0 CHECK (" + Q(SPALTE_UNGEEIGNET) + " IN (0,1))"),
            Text("Infotext", INFOTEXT_MAX_ZEICHEN),
            NichtNegativ("Dicke_Quelle"),
            Positiv(SPALTE_U_QUELLE),
            NichtNegativ("R_Quelle"),
            NichtNegativ("Rsi"),
            NichtNegativ("Rse"),
            Schalter("Hinterlueftet"),
            Anteil("Rahmenanteil"),
            Anteil("g_Wert"),
            Positiv("Ug"),
            Positiv("Uf"),
            NichtNegativ("Rahmenbreite"),
            Anteil("Verschattungsfaktor"),
            Zahl("Psi_Gesamt"),
            Zahl("Psi_System"),
            Zahl("Psi_Geometrie"),
            Zahl("OI3_Rahmen_AP"),
            Zahl("OI3_Rahmen_PEI"),
            Zahl("OI3_Rahmen_GWP"),
            Text("Gueltigkeit", 120)
        };

        /// <summary>Die neuen Fachspalten von <c>Tab_Bauteilschicht(_STAMM)</c> in Anlegereihenfolge (ohne den Verweis).</summary>
        public static readonly IReadOnlyList<Spalte> SCHICHT_SPALTEN = new[]
        {
            Text("Kurztext", 80),
            Text("Langtext", LANGTEXT_MAX_ZEICHEN),
            Gliederung(SPALTE_GRUPPE, BAUSTOFF_GRUPPEN),
            Code("Schichtart_Code"),
            Code("Estrichart_Code"),
            Code("Belueftungsart_Code"),
            Code("Dickenart_Code"),
            NichtNegativ("R_Quelle"),
            Anteil("Emissionsgrad"),
            NichtNegativ("Mue_Min"),
            NichtNegativ("Mue_Max"),
            Positiv("Lambda_Bemessung"),
            NichtNegativ("Rsi"),
            NichtNegativ("Rse"),
            NichtNegativ("Rauheit"),
            Zahl("Elastizitaet"),
            Zahl("Laengenausdehnung"),
            Schalter("Hinterlueftet"),
            Schalter("Kapillaraktiv"),
            Schalter("Dampfdicht"),
            NichtNegativ("Feuchtegehalt"),
            Zahl("OI3_AP"),
            Zahl("OI3_PEI"),
            Zahl("OI3_GWP"),
            NichtNegativ("EN6946_Balkenbreite"),
            NichtNegativ("EN6946_Gefachbreite"),
            NichtNegativ("EN6946_Versatz"),
            Zahl("EN6946_Drehung"),
            Text("Quellkennung", 64)
        };

        /// <summary>Der Verweis der Schicht auf ihren Abschnitt (je Seite eine andere Zieltabelle).</summary>
        public static Spalte AbschnittVerweis(bool katalog) =>
            new Spalte(SPALTE_ID_ABSCHNITT, Spaltenart.Verweis,
                       "INTEGER REFERENCES " + Q(katalog ? TAB_ABSCHNITT_STAMM : TAB_ABSCHNITT) + " (\"ID\") ON DELETE CASCADE");

        /// <summary>Der CREATE-Text einer Abschnittstabelle.</summary>
        public static string AbschnittAnlegen(bool katalog) =>
            "CREATE TABLE IF NOT EXISTS " + Q(katalog ? TAB_ABSCHNITT_STAMM : TAB_ABSCHNITT) + " (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Aufbau\" INTEGER NOT NULL,\n" +
            "    \"Nummer\" INTEGER NOT NULL CHECK (\"Nummer\" >= 1),\n" +
            "    \"Flaechenanteil\" REAL CHECK (\"Flaechenanteil\" IS NULL OR (\"Flaechenanteil\" >= 0 AND \"Flaechenanteil\" <= 1)),\n" +
            "    \"Text\" TEXT CHECK (\"Text\" IS NULL OR length(\"Text\") <= 250),\n" +
            "    \"Quellkennung\" TEXT CHECK (\"Quellkennung\" IS NULL OR length(\"Quellkennung\") <= 64),\n" +
            "    UNIQUE (\"ID_Aufbau\", \"Nummer\"),\n" +
            "    FOREIGN KEY (\"ID_Aufbau\") REFERENCES " + Q(katalog ? TAB_AUFBAU_STAMM : TAB_AUFBAU) + " (\"ID\") ON DELETE CASCADE\n" +
            ") STRICT";

        /// <summary>Die Namen der Teilindizes „Quellkennung eindeutig je Herkunft IMPORT“ je Kopftabelle.</summary>
        public static string Importindex(string tabelle) => "UX_" + tabelle + "_Importkennung";

        /// <summary>Der Teilindex „Quellkennung eindeutig je Herkunft IMPORT“ (Projektkopie: je Projekt).</summary>
        public static string ImportindexAnlegen(string tabelle)
        {
            bool projekt = tabelle == TAB_BAUSTOFF || tabelle == TAB_AUFBAU;
            return "CREATE UNIQUE INDEX IF NOT EXISTS " + Q(Importindex(tabelle)) + " ON " + Q(tabelle) + " (" +
                   (projekt ? "\"ID_Projekt\", " : "") + "\"Quellkennung\") WHERE \"Herkunft\" = '" + HERKUNFT_IMPORT +
                   "' AND \"Quellkennung\" IS NOT NULL";
        }

        /// <summary>Der Index der Schichten auf ihren Abschnitt.</summary>
        private static string AbschnittindexAnlegen(string schicht) =>
            "CREATE INDEX IF NOT EXISTS " + Q("idx_" + schicht.Substring(4) + "_Abschnitt") + " ON " + Q(schicht) + " (\"ID_Abschnitt\")";

        /// <summary>Die neuen Spalten einer Tabelle (Schichten samt Verweis).</summary>
        public static IReadOnlyList<Spalte> SpaltenVon(string tabelle)
        {
            switch (tabelle)
            {
                case TAB_BAUSTOFF_STAMM:
                case TAB_BAUSTOFF: return BAUSTOFF_SPALTEN;
                case TAB_AUFBAU_STAMM:
                case TAB_AUFBAU: return AUFBAU_SPALTEN;
                case TAB_SCHICHT_STAMM: return SCHICHT_SPALTEN.Concat(new[] { AbschnittVerweis(true) }).ToList();
                case TAB_SCHICHT: return SCHICHT_SPALTEN.Concat(new[] { AbschnittVerweis(false) }).ToList();
                default: return Array.Empty<Spalte>();
            }
        }

        /// <summary>Die vier Kopftabellen mit neuer Herkunftsklausel — Katalog vor Kopie, Stoffe vor Aufbauten.</summary>
        public static readonly IReadOnlyList<string> KOPFTABELLEN = new[] { TAB_BAUSTOFF_STAMM, TAB_BAUSTOFF, TAB_AUFBAU_STAMM, TAB_AUFBAU };

        /// <summary>Die sechs Tabellen, die neue Spalten bekommen.</summary>
        public static readonly IReadOnlyList<string> SPALTENTABELLEN =
            new[] { TAB_BAUSTOFF_STAMM, TAB_BAUSTOFF, TAB_AUFBAU_STAMM, TAB_AUFBAU, TAB_SCHICHT_STAMM, TAB_SCHICHT };

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => SPALTENTABELLEN;

        /// <summary>Der Hilfsname, unter den die alte Tabelle für die Dauer des Neubaus ausweicht.</summary>
        public static string Hilfsname(string tabelle) => tabelle + "_vor_Bauteilkatalog";

        /// <summary>
        /// <b>Der Zieltext einer Kopftabelle</b>: der GELTENDE <c>sqlite_master</c>-Text mit getauschter Herkunfts- und
        /// Quellklausel. <c>null</c>, wenn die Tabelle die neue Herkunftsklausel schon trägt. Trägt sie eine unbekannte
        /// Klausel oder ist sie nicht <c>STRICT</c>, bricht der Aufruf ab, statt eine falsche Tabelle anzulegen.
        /// </summary>
        public static string Zieltext(string tabelle, string bestand)
        {
            if (string.IsNullOrEmpty(bestand))
                throw new InvalidOperationException("Zu " + tabelle + " gibt es keinen CREATE-Text in sqlite_master.");
            if (bestand.Contains(CHECK_HERKUNFT_NEU, StringComparison.Ordinal)) return null;
            if (!bestand.TrimEnd().EndsWith(") STRICT", StringComparison.Ordinal))
                throw new InvalidOperationException(tabelle + " ist keine STRICT-Tabelle - der Schritt " + Nr + " baut sie nicht um.");
            string ziel = Tauschen(bestand, tabelle, CHECK_HERKUNFT_ALT, CHECK_HERKUNFT_NEU);
            // Die Quellklausel nur, wo sie noch die alte ist (ein Rückbau der Herkunft allein lässt sie stehen).
            return ziel.Contains(CHECK_QUELLE_NEU, StringComparison.Ordinal) ? ziel : Tauschen(ziel, tabelle, CHECK_QUELLE_ALT, CHECK_QUELLE_NEU);
        }

        private static string Tauschen(string text, string tabelle, string alt, string neu)
        {
            int stelle = text.IndexOf(alt, StringComparison.Ordinal);
            if (stelle < 0)
                throw new InvalidOperationException(tabelle + " traegt eine unbekannte Pruefklausel (erwartet: " + alt +
                                                    ") - der Schritt " + Nr + " baut die Tabelle deshalb NICHT um.");
            if (text.IndexOf(alt, stelle + alt.Length, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException(tabelle + " traegt die Pruefklausel " + alt + " zweimal.");
            return text.Substring(0, stelle) + neu + text.Substring(stelle + alt.Length);
        }

        private static string CreateText(Func<string, DbParam[], DataTable> lese, string tabelle)
        {
            DataTable dt = lese("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new[] { new DbParam("p1", tabelle) });
            return dt == null || dt.Rows.Count == 0 || dt.Rows[0][0] == DBNull.Value
                ? null : Convert.ToString(dt.Rows[0][0], CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Steht der Schritt? (Leser fragen das, bevor sie die Spalten nennen.) Der Schritt läuft in EINEM Vorgang; deshalb
        /// genügt die Marke, die er zuletzt setzt: die Projekt-Abschnittstabelle und der Abschnittsverweis der Projektschicht.
        /// </summary>
        public static bool Lesbar() =>
            DataRepository.TabelleVorhanden(TAB_ABSCHNITT) && DataRepository.SpalteVorhanden(TAB_SCHICHT, SPALTE_ID_ABSCHNITT);

        /// <summary>Was noch offen ist: je umzubauende Tabelle, fehlende Spalte, Tabelle oder Index 1 (−1: Tabelle fehlt).</summary>
        public static int Offen()
        {
            if (!Voraussetzungen().All(DataRepository.TabelleVorhanden)) return -1;
            int offen = 0;
            foreach (string t in KOPFTABELLEN)
                if (Zieltext(t, CreateText((sql, p) => DataRepository.GetDataTable(sql, p), t)) != null) offen++;
            foreach (string t in SPALTENTABELLEN)
                offen += SpaltenVon(t).Count(s => !DataRepository.SpalteVorhanden(t, s.Name));
            if (!DataRepository.TabelleVorhanden(TAB_ABSCHNITT_STAMM)) offen++;
            if (!DataRepository.TabelleVorhanden(TAB_ABSCHNITT)) offen++;
            foreach (string t in KOPFTABELLEN)
                if (Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = ?",
                                                                 new DbParam("p1", Importindex(t))) ?? 0L,
                                    CultureInfo.InvariantCulture) == 0) offen++;
            return offen;
        }

        /// <summary>Steht der Schritt?</summary>
        public static bool Vollstaendig() => Offen() == 0;

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang mit abgeschalteten Fremdschlüsseln aus (wiederholbar); Rückgabe = Zahl der
        /// Handgriffe (Neubauten, Spalten, Tabellen, Indizes). Steht alles, geschieht nichts. Fehlt eine vorausgesetzte
        /// Tabelle, wirft er benannt.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            if (Offen() == 0)
            {
                bericht?.Add("steht bereits - Herkunft, Gliederung, Kennzeichen und Felder des Bauteilkatalogs; nichts zu tun");
                return 0;
            }
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen Verbindung.
            var offeneSpalten = SPALTENTABELLEN.ToDictionary(t => t, t => SpaltenVon(t).Where(s => !DataRepository.SpalteVorhanden(t, s.Name)).ToList());
            int handgriffe = 0;
            var zeilen = new List<string>();
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    Func<string, DbParam[], DataTable> lese = (sql, p) => v.Lese(sql, p);
                    // 1. Neubau der Kopftabellen (Herkunft + Quelle).
                    foreach (string t in KOPFTABELLEN)
                    {
                        string ziel = Zieltext(t, CreateText(lese, t));
                        if (ziel == null) continue;
                        long vorher = Zahl(v, "SELECT COUNT(*) FROM " + Q(t));
                        int objekte = TwwBezugsartSchema.Neubau(v, t, ziel, Hilfsname(t));
                        long nachher = Zahl(v, "SELECT COUNT(*) FROM " + Q(t));
                        if (vorher != nachher)
                            throw new InvalidOperationException(t + ": " + vorher.ToString(CultureInfo.InvariantCulture) +
                                " Zeile(n) vor dem Neubau, " + nachher.ToString(CultureInfo.InvariantCulture) + " danach.");
                        handgriffe++;
                        zeilen.Add(t + ": neu gebaut (Herkunft + EIGEN/IMPORT, Quelle <= " +
                                   QUELLE_MAX_ZEICHEN.ToString(CultureInfo.InvariantCulture) + "); Zeilen " +
                                   nachher.ToString(CultureInfo.InvariantCulture) + ", Index/Trigger wieder " +
                                   objekte.ToString(CultureInfo.InvariantCulture));
                    }
                    // 2. Abschnittstabellen (vor dem Verweis der Schichten).
                    foreach (bool katalog in new[] { true, false })
                    {
                        string t = katalog ? TAB_ABSCHNITT_STAMM : TAB_ABSCHNITT;
                        if (Zahl(v, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '" + t + "'") > 0) continue;
                        v.Ausfuehren(AbschnittAnlegen(katalog));
                        handgriffe++;
                        zeilen.Add(t + " angelegt (leer)");
                    }
                    // 3. Neue Spalten.
                    foreach (KeyValuePair<string, List<Spalte>> e in offeneSpalten)
                    {
                        foreach (Spalte s in e.Value)
                            v.Ausfuehren("ALTER TABLE " + Q(e.Key) + " ADD COLUMN " + Q(s.Name) + " " + s.Typ);
                        handgriffe += e.Value.Count;
                        if (e.Value.Count > 0)
                            zeilen.Add(e.Key + ": " + e.Value.Count.ToString(CultureInfo.InvariantCulture) + " Spalte(n) angelegt");
                    }
                    // 4. Indizes.
                    foreach (string t in KOPFTABELLEN)
                        v.Ausfuehren(ImportindexAnlegen(t));
                    v.Ausfuehren(AbschnittindexAnlegen(TAB_SCHICHT_STAMM));
                    v.Ausfuehren(AbschnittindexAnlegen(TAB_SCHICHT));

                    // 5. Bestand - Pruefsummen merken, ableiten, neu stempeln.
                    List<long> stimmigStoff = Stimmig(v, TAB_BAUSTOFF_STAMM, lese);
                    List<long> stimmigAufbau = Stimmig(v, TAB_AUFBAU_STAMM, lese);
                    int herkunft = HerkunftAbleiten(v);
                    int gliederung = 0, kennzeichen = 0;
                    foreach (string t in new[] { TAB_BAUSTOFF_STAMM, TAB_BAUSTOFF }) gliederung += BaustoffGliederungAbleiten(v, t);
                    foreach (string t in new[] { TAB_AUFBAU_STAMM, TAB_AUFBAU }) gliederung += AufbauGliederungAbleiten(v, t);
                    kennzeichen += KennzeichenSetzen(v, TAB_AUFBAU_STAMM, TAB_SCHICHT_STAMM, null);
                    kennzeichen += KennzeichenSetzen(v, TAB_AUFBAU, TAB_SCHICHT, null);
                    int summen = Stempeln(v, TAB_BAUSTOFF_STAMM, stimmigStoff, lese) + Stempeln(v, TAB_AUFBAU_STAMM, stimmigAufbau, lese);
                    zeilen.Add("Bestand: Herkunft " + herkunft.ToString(CultureInfo.InvariantCulture) + ", Gliederung " +
                               gliederung.ToString(CultureInfo.InvariantCulture) + ", Kennzeichen " +
                               kennzeichen.ToString(CultureInfo.InvariantCulture) + " Zeile(n); Pruefsummen neu " +
                               summen.ToString(CultureInfo.InvariantCulture));

                    foreach (string t in KOPFTABELLEN.Concat(new[] { TAB_SCHICHT_STAMM, TAB_SCHICHT }))
                    {
                        long verletzt = TwwBezugsartSchema.Verletzt(v, t);
                        if (verletzt > 0)
                            throw new InvalidOperationException("Nach dem Schritt meldet foreign_key_check fuer " + t +
                                " oder eine Kindtabelle " + verletzt.ToString(CultureInfo.InvariantCulture) + " verletzte Zeile(n).");
                    }
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
                finally
                {
                    // DER LEGACY-MODUS DARF DIE VERBINDUNG NICHT UEBERLEBEN (Muster aus Schritt 96).
                    try { if (v.Offen) v.Ausfuehren("PRAGMA legacy_alter_table = OFF"); }
                    catch (Exception) { /* die Verbindung ist dann ohnehin am Ende */ }
                }
            }
            if (bericht != null)
            {
                foreach (string z in zeilen) bericht.Add(z);
                bericht.Add("foreign_key_check leer; IDs und Fremdschluessel erhalten; der Bauteilweg liest keine neue Spalte, " +
                            "der Referenzlauf bleibt gleich");
            }
            return handgriffe;
        }

        // ---- Bestand -----------------------------------------------------------------------

        /// <summary>Katalog: ReadOnly = 1 → VORGABE; ReadOnly = 0 mit Herkunft leer, MANUELL oder VORGABE → EIGEN.</summary>
        internal static int HerkunftAbleiten(DbVorgang v)
        {
            int n = 0;
            foreach (string t in new[] { TAB_BAUSTOFF_STAMM, TAB_AUFBAU_STAMM })
            {
                n += v.Ausfuehren("UPDATE " + Q(t) + " SET \"Herkunft\" = ? WHERE \"ReadOnly\" = 1 AND (\"Herkunft\" IS NULL OR \"Herkunft\" <> ?)",
                                  new DbParam("p1", HERKUNFT_VORGABE), new DbParam("p2", HERKUNFT_VORGABE));
                n += v.Ausfuehren("UPDATE " + Q(t) + " SET \"Herkunft\" = ? WHERE \"ReadOnly\" = 0 AND " +
                                  "(\"Herkunft\" IS NULL OR \"Herkunft\" IN (?, ?))",
                                  new DbParam("p1", HERKUNFT_EIGEN), new DbParam("p2", DbWerte.HERKUNFT_MANUELL),
                                  new DbParam("p3", HERKUNFT_VORGABE));
            }
            return n;
        }

        /// <summary>Baustoff: die Gruppe aus der Stoffgruppe (Text), die Klasse bleibt leer.</summary>
        internal static int BaustoffGliederungAbleiten(DbVorgang v, string tabelle)
        {
            int n = 0;
            foreach (Baustoffgruppe g in Baustoffgruppen.Alle)
                n += v.Ausfuehren("UPDATE " + Q(tabelle) + " SET " + Q(SPALTE_GRUPPE) + " = ? WHERE " + Q(SPALTE_GRUPPE) +
                                  " IS NULL AND \"Gruppe\" = ?",
                                  new DbParam("p1", Bauteilkatalog.GruppeAusStoffgruppe(g.Wert)), new DbParam("p2", g.Wert));
            return n;
        }

        /// <summary>Aufbau: Gruppe und Klasse aus Bauteilart und Typaufbau (eine unklare Klasse bleibt leer).</summary>
        internal static int AufbauGliederungAbleiten(DbVorgang v, string tabelle)
        {
            int n = 0;
            bool mitTyp = Zahl(v, "SELECT COUNT(*) FROM pragma_table_info('" + tabelle + "') WHERE name = '" + TypaufbauSchema.SPALTE + "'") > 0;
            if (mitTyp)
                foreach ((string art, string typ) in Bauteilkatalog.KLASSE_AUS_TYPAUFBAU.Keys)
                {
                    (string gruppe, string klasse) = Bauteilkatalog.GliederungAusBauteilart(art, typ);
                    n += v.Ausfuehren("UPDATE " + Q(tabelle) + " SET " + Q(SPALTE_GRUPPE) + " = ?, " + Q(SPALTE_KLASSE) + " = ? WHERE " +
                                      Q(SPALTE_GRUPPE) + " IS NULL AND \"Bauteilart\" = ? AND \"Typaufbau\" = ?",
                                      new DbParam("p1", gruppe), Wert("p2", klasse), new DbParam("p3", art), new DbParam("p4", typ));
                }
            foreach (string art in Bauteilkatalog.BAUTEILARTEN)
            {
                (string gruppe, string klasse) = Bauteilkatalog.GliederungAusBauteilart(art, null);
                if (gruppe == null) continue;
                n += v.Ausfuehren("UPDATE " + Q(tabelle) + " SET " + Q(SPALTE_GRUPPE) + " = ?, " + Q(SPALTE_KLASSE) + " = ? WHERE " +
                                  Q(SPALTE_GRUPPE) + " IS NULL AND \"Bauteilart\" = ?",
                                  new DbParam("p1", gruppe), Wert("p2", klasse), new DbParam("p3", art));
            }
            return n;
        }

        /// <summary>
        /// Das Kennzeichen nach der Regel des Kerns (<see cref="Bauteilkatalog.MeistUngeeignet"/>) in SQL: eine Schicht ohne
        /// Luftschicht und ohne λ > 0, oder keine Schicht (ein Fenster, eine Vorhangfassade oder eine Tür mit Quell-U
        /// ausgenommen). <paramref name="idAufbau"/> <c>null</c> = alle Aufbauten der Tabelle. Liefert die Zahl der
        /// geänderten Zeilen.
        /// </summary>
        internal static int KennzeichenSetzen(DbVorgang v, string aufbau, string schicht, long? idAufbau)
        {
            var p = new List<DbParam>();
            foreach (string art in Bauteilkatalog.OEFFNUNGSARTEN) p.Add(new DbParam("p" + p.Count, art));
            string ausdruck =
                "CASE WHEN EXISTS (SELECT 1 FROM " + Q(schicht) + " s WHERE s.\"ID_Aufbau\" = a.\"ID\" AND s.\"IstLuftschicht\" = 0 " +
                "AND (s.\"Lambda\" IS NULL OR s.\"Lambda\" <= 0)) THEN 1 " +
                "WHEN EXISTS (SELECT 1 FROM " + Q(schicht) + " s WHERE s.\"ID_Aufbau\" = a.\"ID\") THEN 0 " +
                "WHEN a.\"Bauteilart\" IN (" + string.Join(", ", p.Select(_ => "?")) + ") AND a." + Q(SPALTE_U_QUELLE) + " IS NOT NULL THEN 0 " +
                "ELSE 1 END";
            string sql = "UPDATE " + Q(aufbau) + " AS a SET " + Q(SPALTE_UNGEEIGNET) + " = " + ausdruck +
                         " WHERE " + Q(SPALTE_UNGEEIGNET) + " IS NOT " + ausdruck;
            var alle = new List<DbParam>(p);
            alle.AddRange(p.Select(x => new DbParam(x.Name + "b", x.Wert)));
            if (idAufbau.HasValue)
            {
                sql += " AND a.\"ID\" = ?";
                alle.Add(new DbParam("pid", idAufbau.Value));
            }
            return v.Ausfuehren(sql, alle.ToArray());
        }

        private static List<long> Stimmig(DbVorgang v, string tabelle, Func<string, DbParam[], DataTable> lese)
        {
            var ids = new List<long>();
            Katalogtabelle t = Katalogfassung.Tabelle(tabelle);
            if (t == null || !Katalogfassung.SpaltenVorhanden(tabelle)) return ids;
            List<string> spalten = Katalogfassung.VorhandeneFachspalten(t);
            DataTable dt = v.Lese("SELECT ID, " + Q(Katalogfassung.SPALTE_PRUEFSUMME) + ", " + Katalogfassung.Spaltentext(spalten) +
                                  " FROM " + Q(tabelle) + " WHERE " + Q(Katalogfassung.SPALTE_PRUEFSUMME) + " IS NOT NULL");
            foreach (DataRow r in dt.Rows)
                if (string.Equals(Convert.ToString(r[Katalogfassung.SPALTE_PRUEFSUMME], CultureInfo.InvariantCulture),
                                  Katalogfassung.PruefsummeDerZeile(t, spalten, r, lese), StringComparison.Ordinal))
                    ids.Add(Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture));
            return ids;
        }

        private static int Stempeln(DbVorgang v, string tabelle, List<long> ids, Func<string, DbParam[], DataTable> lese)
        {
            if (ids.Count == 0) return 0;
            Katalogtabelle t = Katalogfassung.Tabelle(tabelle);
            // Die volle Liste: im Vorgang stehen die neuen Spalten schon (die eigene Verbindung von SpalteVorhanden sieht sie nicht).
            var spalten = t.Fachspalten.ToList();
            int n = 0;
            foreach (long id in ids)
            {
                DataTable dt = v.Lese("SELECT ID, " + Katalogfassung.Spaltentext(spalten) + " FROM " + Q(tabelle) + " WHERE ID = ?",
                                      new DbParam("p1", id));
                if (dt.Rows.Count == 0) continue;
                n += v.Ausfuehren("UPDATE " + Q(tabelle) + " SET " + Q(Katalogfassung.SPALTE_PRUEFSUMME) + " = ? WHERE ID = ? AND " +
                                  Q(Katalogfassung.SPALTE_PRUEFSUMME) + " IS NOT ?",
                                  new DbParam("p1", Katalogfassung.PruefsummeDerZeile(t, spalten, dt.Rows[0], lese)),
                                  new DbParam("p2", id),
                                  new DbParam("p3", Katalogfassung.PruefsummeDerZeile(t, spalten, dt.Rows[0], lese)));
            }
            return n;
        }

        private static DbParam Wert(string name, string wert) =>
            new DbParam(name, DbParamTyp.VarWChar) { Wert = (object)wert ?? DBNull.Value };

        private static long Zahl(DbVorgang v, string sql)
        {
            object wert = v.Skalar(sql);
            if (wert == null || wert == DBNull.Value) return -1;
            return Convert.ToInt64(wert, CultureInfo.InvariantCulture);
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
