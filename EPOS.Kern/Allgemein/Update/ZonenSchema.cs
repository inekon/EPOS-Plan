using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // ZONEN UND BAUTEILE - Schemaschritt S-C der Gebaeudesimulation, Stufe G3
    // (Softwarearchitektur Gebaeudesimulation 2.2, 2.4, W1, W4, W6 bis W10, A1;
    // Mehrzonenkonzept 4.1 und 4.2).
    //
    // WOZU. Das Bauteil haengt an der ZONE (W4), und Tab_Bauteil.ID_Zone ist NOT NULL - also
    // entsteht Tab_Zone mit G3, nicht erst mit G6 (W1). Es entsteht KEINE implizite Zone:
    // Tab_Zone bleibt leer, bis der Anwender „Gebaeude als eine Zone uebernehmen" drueckt oder
    // ein Import Zonen schreibt; eine leere Tab_Zone heisst Klassenweg.
    //
    // BAUFORM B (Mehrzonenkonzept 4.1). Zone und Bauteil sind geordnete Kindlisten mit Rang,
    // STRICT, AUTOINCREMENT, und die Kaskade zeigt NUR zum Eltern: Zone -> Gebaeude und
    // Bauteil -> Zone mit ON DELETE CASCADE, Bauteil -> Aufbau OHNE Kaskade. Kein Kind traegt
    // ein eigenes ID_Projekt (W16) - die Zone haengt ueber ihr Gebaeude am Projekt, das Bauteil
    // ueber seine Zone. Geschrieben wird als Aggregat je Gebaeude mit Abgleich ueber die Ids
    // (Muster A6, GebaeudeZonenCtrl.SpeichernJeGebaeude), damit die Ids stehen bleiben, auf die
    // der Import zeigt.
    //
    // DIE ZONENSPALTEN. Die neun Sollwert- und Lueftungsspalten spiegeln die Namen des
    // Gebaeudes buchstabengetreu (einschliesslich Maximaleraumtemperatur); NULL heisst „Wert des
    // Gebaeudes". Dazu GENAU die vier Kuehleingaben aus KU-S1 und die drei Uebergabespalten aus
    // AK-S1 - mit denselben Namen und Typen wie am Gebaeude, alle nullbar (NULL = Wert des
    // Gebaeudes), der Schalter Kuehlung_Aktiv also ohne DEFAULT. Keine Spalte der Kuehluebergabe:
    // Die legt die Stufe AK1 selbst an (Entscheid E37).
    //
    // DAS BAUTEIL. Kein ID_Nachbarzone (kommt mit S-G/G6b), kein IstAussen (W6 - die Aussage
    // traegt allein Randbedingung). Bauteilart mit neun Werten (W7), Randbedingung mit vier
    // (W8, kein KELLER), Herkunft mit fuenf (W9), Quellkennung bis 64 Zeichen (W10).
    //
    // ERGEBNISNEUTRAL. Der Schritt legt nur die Tabellen an. Gelesen werden sie vom Bauteilweg
    // des Rechenkerns (GebaeudeZonenanschluss, genau eine Zone je Gebaeude; mehrere ab Stufe G6b);
    // kein Referenzprojekt fuehrt eine Zone, der Referenzlauf bleibt byte-gleich.
    // ====================================================================================

    /// <summary>
    /// <b>Die DDL der Zonen und Bauteile</b> — Schemaschritt S-C (Nummer <see cref="SCHRITT"/>).
    /// Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class ZonenSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht. Er folgt auf
        /// S-B (<see cref="BauteilaufbauSchema.SCHRITT"/>), auf dessen Aufbauten das Bauteil zeigt.
        /// </summary>
        public const int SCHRITT = BauteilaufbauSchema.SCHRITT + 1;

        // =================================================================
        //  Namen — sprachneutral und EINMAL
        // =================================================================

        /// <summary>Die Zonentabelle.</summary>
        public const string TAB_ZONE = SchemaKatalog.TAB_ZONE;

        /// <summary>Die Bauteiltabelle.</summary>
        public const string TAB_BAUTEIL = SchemaKatalog.TAB_BAUTEIL;

        /// <summary>Index der Zonen über (<c>ID_Gebaeude</c>, <c>Rang</c>) — Lesewege und Kaskade.</summary>
        public const string INDEX_ZONE = "idx_Zone_Gebaeude";

        /// <summary>Index der Bauteile über (<c>ID_Zone</c>, <c>Rang</c>).</summary>
        public const string INDEX_BAUTEIL = "idx_Bauteil_Zone";

        // ---- Zone -------------------------------------------------------------------

        /// <summary>Das Gebäude der Zone, NOT NULL, <c>ON DELETE CASCADE</c> (A1).</summary>
        public const string SPALTE_ID_GEBAEUDE = "ID_Gebaeude";

        /// <summary>Reihenfolge, lückenlos ab 1; zugleich die Iterationsreihenfolge der Kopplung.</summary>
        public const string SPALTE_RANG = "Rang";

        /// <summary>Name der Zone bzw. des Bauteils, NOT NULL, höchstens 80 Zeichen (W2).</summary>
        public const string SPALTE_BEZEICHNER = "Bezeichner";

        /// <summary>Nutzfläche der Zone [m²] (E13); NULL = aus den Bauteilen.</summary>
        public const string SPALTE_NUTZFLAECHE = "Nutzflaeche";

        /// <summary>Raumhöhe [m]; NULL = <c>Tab_Gebaeude.Raumhoehe</c>.</summary>
        public const string SPALTE_RAUMHOEHE = "Raumhoehe";

        /// <summary>Volumen [m³]; NULL = Fläche × Höhe.</summary>
        public const string SPALTE_VOLUMEN = "Volumen";

        /// <summary>Schalter (0/1, NOT NULL DEFAULT 1): die Zone wird beheizt.</summary>
        public const string SPALTE_IST_BEHEIZT = "IstBeheizt";

        /// <summary>Innere Wärmegewinne; NULL = anteilig aus dem Gebäude über den Flächenschlüssel.</summary>
        public const string SPALTE_INTERNE_WAERMEGEWINNE = "Interne_Waermegewinne";

        /// <summary>Bewohner; NULL = anteilig aus dem Gebäude über den Flächenschlüssel.</summary>
        public const string SPALTE_BEWOHNER = "Bewohner";

        /// <summary>Herkunft (<see cref="DbWerte.HERKUENFTE"/>); NULL = nicht angegeben (W9).</summary>
        public const string SPALTE_HERKUNFT = "Herkunft";

        /// <summary>IFC-GUID oder gbXML-id der Quellentität; NULL = keine (W10).</summary>
        public const string SPALTE_QUELLKENNUNG = "Quellkennung";

        /// <summary>
        /// <b>Die neun Sollwert- und Lüftungsspalten</b> — die Namen des Gebäudes
        /// buchstabengetreu, damit der Rückfall auf den Gebäudewert eine Namensgleichheit
        /// bleibt und keine Übersetzungstabelle braucht. NULL heißt „Wert des Gebäudes".
        /// </summary>
        public static readonly IReadOnlyList<string> GebaeudewertSpalten = new[]
        {
            "Raumsolltemperatur_Tag", "Raumsolltemperatur_Nachtabsenkung", "Raumsolltemperatur_Wochenende",
            "Raumsolltemperatur_Ferien", "Maximaleraumtemperatur", GebaeudeSchema.SPALTE_HEIZUNG_STRAHLUNGSANTEIL,
            GebaeudeSchema.SPALTE_HEIZLEISTUNG_MAX, GebaeudeSchema.SPALTE_LUFTWECHSEL_INFILTRATION,
            GebaeudeSchema.SPALTE_LUFTWECHSEL_NUTZER
        };

        /// <summary>
        /// <b>Der Block aus KU-S1</b> — dieselben vier Namen und Typen wie am Gebäude
        /// (<see cref="GebaeudeSchema.KUEHL_SPALTEN"/>), hier alle nullbar: NULL heißt „Wert des
        /// Gebäudes", auch beim Schalter <c>Kuehlung_Aktiv</c>.
        /// </summary>
        public static readonly IReadOnlyList<string> KuehlSpalten = new[]
        {
            GebaeudeSchema.SPALTE_KUEHL_SOLLWERT, GebaeudeSchema.SPALTE_KUEHLLEISTUNG_MAX,
            GebaeudeSchema.SPALTE_KUEHLUNG_AKTIV, GebaeudeSchema.SPALTE_KUEHL_SOLLWERT_NACHT
        };

        /// <summary>
        /// <b>Der Block aus AK-S1</b> — Übergabeart, Exponent und Nennleistung wie am Gebäude;
        /// NULL heißt „Wert des Gebäudes". Mehr trägt die Zone von AK-S1 nicht (Mehrzonenkonzept 4.2).
        /// </summary>
        public static readonly IReadOnlyList<string> UebergabeSpalten = new[]
        {
            GebaeudeSchema.SPALTE_UEBERGABE_ART, GebaeudeSchema.SPALTE_UEBERGABE_EXPONENT,
            GebaeudeSchema.SPALTE_UEBERGABE_LEISTUNG_NENN
        };

        /// <summary>
        /// Die Wertliste der Übergabeart an der Zone — die vier Arten des Gebäudes
        /// (<c>DbWerte.UEBERGABE_*</c>) EINSCHLIESSLICH <c>IDEAL</c>: An der Zone heißt NULL
        /// „Wert des Gebäudes", eine ideal geregelte Zone in einem gekoppelten Gebäude braucht
        /// deshalb den ausdrücklichen Wert. <c>Tab_ErgebnisGebaeude</c> führt ihn nicht, weil dort
        /// NULL „nicht gekoppelt" heißt.
        /// </summary>
        public const string WERTE_UEBERGABE_ART =
            "'" + DbWerte.UEBERGABE_IDEAL + "','" + DbWerte.UEBERGABE_RADIATOR + "','" +
            DbWerte.UEBERGABE_FLAECHE + "','" + DbWerte.UEBERGABE_KONVEKTOR + "'";

        // ---- Bauteil ----------------------------------------------------------------

        /// <summary>Die Zone des Bauteils, NOT NULL, <c>ON DELETE CASCADE</c> (W4).</summary>
        public const string SPALTE_ID_ZONE = "ID_Zone";

        /// <summary>Bauteilart (<see cref="DbWerte.BAUTEILARTEN"/>), NOT NULL (W7).</summary>
        public const string SPALTE_BAUTEILART = "Bauteilart";

        /// <summary>Aufbau des Bauteils (→ <c>Tab_Bauteilaufbau.ID</c>, ohne Kaskade); <b>NULL = nur U-Wert</b>.</summary>
        public const string SPALTE_ID_AUFBAU = "ID_Aufbau";

        /// <summary>Fläche [m²], NOT NULL.</summary>
        public const string SPALTE_FLAECHE = "Flaeche";

        /// <summary>U-Wert [W/(m²·K)]; NULL = aus dem Aufbau gerechnet.</summary>
        public const string SPALTE_U_WERT = "U_Wert";

        /// <summary>g-Wert (Fenster, Vorhangfassade); NULL = Vorgabe.</summary>
        public const string SPALTE_G_WERT = "g_Wert";

        /// <summary>Rahmenanteil (Fenster, Vorhangfassade); NULL = Vorgabe.</summary>
        public const string SPALTE_RAHMENANTEIL = "Rahmenanteil";

        /// <summary>Verschattungsfaktor (Fenster, Vorhangfassade); NULL = Vorgabe.</summary>
        public const string SPALTE_VERSCHATTUNGSFAKTOR = "Verschattungsfaktor";

        /// <summary>Neigung [°]; NULL = nach Bauteilart (Dach/Decke 0°, Boden 180°, sonst 90°).</summary>
        public const string SPALTE_NEIGUNG = "Neigung";

        /// <summary>
        /// Azimut [°], 0° = Nord; <b>NULL nur bei Neigung 0° oder 180°</b> — eine Außenwand ohne
        /// Azimut wird benannt abgelehnt (<c>GebaeudeZonenCtrl.Pruefen</c>), nicht auf Nord vorbelegt.
        /// </summary>
        public const string SPALTE_AZIMUT = "Azimut";

        /// <summary>Randbedingung (<see cref="DbWerte.RANDBEDINGUNGEN"/>); <b>NULL = Außenluft</b>, kein KELLER (W8).</summary>
        public const string SPALTE_RANDBEDINGUNG = "Randbedingung";

        /// <summary>Wärmebrücke ψ·L [W/K]; NULL = keine.</summary>
        public const string SPALTE_PSI_L = "Psi_L";

        /// <summary>Die Wertliste der Randbedingung als SQL-Literal (Quelle des <c>CHECK</c>).</summary>
        public const string WERTE_RANDBEDINGUNG =
            "'" + DbWerte.RANDBEDINGUNG_AUSSENLUFT + "','" + DbWerte.RANDBEDINGUNG_ERDREICH + "','" +
            DbWerte.RANDBEDINGUNG_ZONE + "','" + DbWerte.RANDBEDINGUNG_UNBEHEIZT + "'";

        // =================================================================
        //  Die DDL
        // =================================================================

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_Zone</c> — 28 Spalten, STRICT, Kaskade zum Gebäude.
        /// </summary>
        public const string SQL_CREATE_ZONE =
            "CREATE TABLE IF NOT EXISTS \"Tab_Zone\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Gebaeude\" INTEGER NOT NULL,\n" +
            "    \"Rang\" INTEGER NOT NULL,\n" +
            "    \"Bezeichner\" TEXT NOT NULL CHECK (length(\"Bezeichner\") <= 80),\n" +
            "    \"Nutzflaeche\" REAL,\n" +
            "    \"Raumhoehe\" REAL,\n" +
            "    \"Volumen\" REAL,\n" +
            "    \"IstBeheizt\" INTEGER NOT NULL DEFAULT 1 CHECK (\"IstBeheizt\" IN (0,1)),\n" +
            "    \"Raumsolltemperatur_Tag\" REAL,\n" +
            "    \"Raumsolltemperatur_Nachtabsenkung\" REAL,\n" +
            "    \"Raumsolltemperatur_Wochenende\" REAL,\n" +
            "    \"Raumsolltemperatur_Ferien\" REAL,\n" +
            "    \"Maximaleraumtemperatur\" REAL,\n" +
            "    \"Heizung_Strahlungsanteil\" REAL,\n" +
            "    \"Heizleistung_Max\" REAL,\n" +
            "    \"Luftwechsel_Infiltration\" REAL,\n" +
            "    \"Luftwechsel_Nutzer\" REAL,\n" +
            "    \"Interne_Waermegewinne\" REAL,\n" +
            "    \"Bewohner\" REAL,\n" +
            "    \"Kuehl_Sollwert\" REAL,\n" +
            "    \"Kuehlleistung_Max\" REAL,\n" +
            "    \"Kuehlung_Aktiv\" INTEGER CHECK (\"Kuehlung_Aktiv\" IN (0,1)),\n" +
            "    \"Kuehl_Sollwert_Nacht\" REAL,\n" +
            "    \"Uebergabe_Art\" TEXT CHECK (\"Uebergabe_Art\" IN (" + WERTE_UEBERGABE_ART + ")),\n" +
            "    \"Uebergabe_Exponent\" REAL,\n" +
            "    \"Uebergabe_Leistung_Nenn\" REAL,\n" +
            "    \"Herkunft\" TEXT CHECK (\"Herkunft\" IN (" + BaustoffSchema.WERTE_HERKUNFT + ")),\n" +
            "    \"Quellkennung\" TEXT CHECK (length(\"Quellkennung\") <= 64),\n" +
            "    FOREIGN KEY (\"ID_Gebaeude\") REFERENCES \"Tab_Gebaeude\" (\"ID\") ON DELETE CASCADE\n" +
            ") STRICT";

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_Bauteil</c> — 17 Spalten, STRICT, Kaskade zur Zone,
        /// der Aufbau ohne Kaskade. Ohne <c>ID_Nachbarzone</c> (S-G) und ohne <c>IstAussen</c> (W6).
        /// </summary>
        public const string SQL_CREATE_BAUTEIL =
            "CREATE TABLE IF NOT EXISTS \"Tab_Bauteil\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Zone\" INTEGER NOT NULL,\n" +
            "    \"Rang\" INTEGER NOT NULL,\n" +
            "    \"Bezeichner\" TEXT NOT NULL CHECK (length(\"Bezeichner\") <= 80),\n" +
            "    \"Bauteilart\" TEXT NOT NULL CHECK (\"Bauteilart\" IN (" + BauteilaufbauSchema.WERTE_BAUTEILART + ")),\n" +
            "    \"ID_Aufbau\" INTEGER,\n" +
            "    \"Flaeche\" REAL NOT NULL,\n" +
            "    \"U_Wert\" REAL,\n" +
            "    \"g_Wert\" REAL,\n" +
            "    \"Rahmenanteil\" REAL,\n" +
            "    \"Verschattungsfaktor\" REAL,\n" +
            "    \"Neigung\" REAL,\n" +
            "    \"Azimut\" REAL,\n" +
            "    \"Randbedingung\" TEXT CHECK (\"Randbedingung\" IN (" + WERTE_RANDBEDINGUNG + ")),\n" +
            "    \"Psi_L\" REAL,\n" +
            "    \"Herkunft\" TEXT CHECK (\"Herkunft\" IN (" + BaustoffSchema.WERTE_HERKUNFT + ")),\n" +
            "    \"Quellkennung\" TEXT CHECK (length(\"Quellkennung\") <= 64),\n" +
            "    FOREIGN KEY (\"ID_Zone\") REFERENCES \"Tab_Zone\" (\"ID\") ON DELETE CASCADE,\n" +
            "    FOREIGN KEY (\"ID_Aufbau\") REFERENCES \"Tab_Bauteilaufbau\" (\"ID\")\n" +
            ") STRICT";

        /// <summary>Der Index der Zonen.</summary>
        public const string SQL_INDEX_ZONE =
            "CREATE INDEX IF NOT EXISTS \"idx_Zone_Gebaeude\" ON \"Tab_Zone\" (\"ID_Gebaeude\", \"Rang\")";

        /// <summary>Der Index der Bauteile.</summary>
        public const string SQL_INDEX_BAUTEIL =
            "CREATE INDEX IF NOT EXISTS \"idx_Bauteil_Zone\" ON \"Tab_Bauteil\" (\"ID_Zone\", \"Rang\")";

        /// <summary>Die zwei Tabellen in Anlegereihenfolge (Eltern vor Kind), je Tabellenname.</summary>
        public static IEnumerable<KeyValuePair<string, string>> Tabellenanweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(TAB_ZONE, SQL_CREATE_ZONE);
                yield return new KeyValuePair<string, string>(TAB_BAUTEIL, SQL_CREATE_BAUTEIL);
            }
        }

        /// <summary>Die zwei Indizes, je Indexname.</summary>
        public static IEnumerable<KeyValuePair<string, string>> Indexanweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(INDEX_ZONE, SQL_INDEX_ZONE);
                yield return new KeyValuePair<string, string>(INDEX_BAUTEIL, SQL_INDEX_BAUTEIL);
            }
        }

        /// <summary>
        /// Alle vier Anweisungen des Schritts S-C in der festen Handgriffreihenfolge (R2) —
        /// Tabellen, dann Indizes. Der Kopplungsschritt S-G bringt später
        /// <c>AnweisungenKopplung</c> dazu (<c>Tab_Zonenluftstrom</c>, <c>ID_Nachbarzone</c>).
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
            => Tabellenanweisungen.Concat(Indexanweisungen);

        /// <summary>
        /// Die Spalten der Zone in Schemareihenfolge, ohne <c>ID</c> — die EINE Liste, an der
        /// Lesen und Schreiben des <c>GebaeudeZonenCtrl</c> hängen.
        /// </summary>
        public static readonly IReadOnlyList<string> Zonenspalten = new[]
        {
            SPALTE_ID_GEBAEUDE, SPALTE_RANG, SPALTE_BEZEICHNER, SPALTE_NUTZFLAECHE, SPALTE_RAUMHOEHE,
            SPALTE_VOLUMEN, SPALTE_IST_BEHEIZT
        }.Concat(GebaeudewertSpalten)
         .Concat(new[] { SPALTE_INTERNE_WAERMEGEWINNE, SPALTE_BEWOHNER })
         .Concat(KuehlSpalten)
         .Concat(UebergabeSpalten)
         .Concat(new[] { SPALTE_HERKUNFT, SPALTE_QUELLKENNUNG })
         .ToArray();

        /// <summary>Die Spalten des Bauteils in Schemareihenfolge, ohne <c>ID</c>.</summary>
        public static readonly IReadOnlyList<string> Bauteilspalten = new[]
        {
            SPALTE_ID_ZONE, SPALTE_RANG, SPALTE_BEZEICHNER, SPALTE_BAUTEILART, SPALTE_ID_AUFBAU,
            SPALTE_FLAECHE, SPALTE_U_WERT, SPALTE_G_WERT, SPALTE_RAHMENANTEIL, SPALTE_VERSCHATTUNGSFAKTOR,
            SPALTE_NEIGUNG, SPALTE_AZIMUT, SPALTE_RANDBEDINGUNG, SPALTE_PSI_L, SPALTE_HERKUNFT,
            SPALTE_QUELLKENNUNG
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
            // Der Zonenleser des Laufs hat sich „keine Tabelle" gemerkt, falls er vorher fragte.
            GebaeudeZonenanschluss.ProbeVerwerfen();
            return angelegt;
        }
    }
}
