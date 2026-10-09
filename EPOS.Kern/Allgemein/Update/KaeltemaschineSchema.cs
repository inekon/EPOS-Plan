using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KU3-1 - DIE KAELTEMASCHINE ALS EIGENER ERZEUGERTYP: SCHEMA UND KATALOG
    // (Konzept Kuehlung 5.3, Plan G7b bis KU3 Abschnitt 6, Entscheide E67/E68).
    //
    // WAS. Vier Tabellen nach dem Muster der Waermepumpe:
    //
    //   Tab_Kaeltemaschine_STAMM               Katalog der Auslieferung (ReadOnly, Katalogspalten)
    //   Tab_Kaeltemaschine                     Projektkopie: ID_Projekt (Kaskade), ID_Stamm (SET NULL)
    //   Tab_Kenndaten_Kaeltemaschine_STAMM     Kennlinie ueber Rueckkuehl- und Kaltwassertemperatur
    //   Tab_Kenndaten_Kaeltemaschine           Kennlinie der Projektkopie
    //
    // ZUORDNUNG PROJEKT <-> KATALOG wie bei der Waermepumpe: Die Projektkopie traegt ID_Projekt und
    // ID_Stamm (Verweis auf den Katalogsatz, ON DELETE SET NULL); eine Z_*-Tabelle braucht es nicht,
    // weil die Kopie selbst die Zuordnung ist (Tab_WP.ID_Stamm, Schritt WaermepumpeKatalogverweis).
    //
    // RUECKKUEHLART als Text mit CHECK: LUFT (luftgekuehlter Verfluessiger), WASSER (wassergekuehlt
    // mit fremdem Kuehlwasser, etwa Brunnen), TROCKENKUEHLER, NASSKUEHLER (Konzept 5.3/5.4: die
    // Rueckkuehlung ist Bestandteil der Maschine, kein eigener Erzeuger).
    //
    // SAAT. Drei neutrale Beispielgeraete mit je sechs Kennlinienpunkten (Rueckkuehlung 25/35/45 Grad C
    // x Kaltwasser 6/12 Grad C), ReadOnly = 1, samt Katalogschluessel und Pruefsumme. Wiederholbar:
    // Ein Geraet wird ueber seinen Bezeichner erkannt, ein Kennlinienpunkt ueber den eindeutigen
    // Schluessel (Geraet, Rueckkuehltemperatur, Kaltwassertemperatur).
    //
    // ERGEBNISNEUTRAL. Kein Projekt fuehrt eine Kaeltemaschine; der Rechenweg liest die Tabellen nicht.
    // Der Referenzlauf bleibt byte-gleich.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>Schemaschritt der Kältemaschine</b> (KU3-1): Katalog, Projektkopie, Kennlinien und die Saat
    /// dreier Beispielgeräte. Wiederholbar und ergebnisneutral.
    /// </summary>
    public static class KaeltemaschineSchema
    {
        /// <summary>Die Nummer des Schritts — hängt an der Vorgängerklasse.</summary>
        public const int SCHRITT = ZonenUebergabeSchema.SCHRITT + 1;

        /// <summary>Der Katalog der Auslieferung.</summary>
        public const string TAB_STAMM = "Tab_Kaeltemaschine_STAMM";

        /// <summary>Die Projektkopie.</summary>
        public const string TAB_PROJEKT = "Tab_Kaeltemaschine";

        /// <summary>Die Kennlinie des Katalogs.</summary>
        public const string TAB_KENNDATEN_STAMM = "Tab_Kenndaten_Kaeltemaschine_STAMM";

        /// <summary>Die Kennlinie der Projektkopie.</summary>
        public const string TAB_KENNDATEN = "Tab_Kenndaten_Kaeltemaschine";

        /// <summary>Der Fremdschlüssel der Kennlinie auf ihr Gerät.</summary>
        public const string SPALTE_ID_KAELTEMASCHINE = "ID_Kaeltemaschine";

        /// <summary>Der Verweis der Projektkopie auf ihren Katalogsatz.</summary>
        public const string SPALTE_ID_STAMM = "ID_Stamm";

        /// <summary>Nennkälteleistung [kW].</summary>
        public const string SPALTE_NENNKAELTELEISTUNG = "Nennkaelteleistung_kW";

        /// <summary>EER im Nennpunkt [—].</summary>
        public const string SPALTE_NENN_EER = "Nenn_EER";

        /// <summary>Kältemittel als Text (etwa „R32“).</summary>
        public const string SPALTE_KAELTEMITTEL = "Kaeltemittel";

        /// <summary>Rückkühlart, einer der Werte aus <see cref="RUECKKUEHLARTEN"/>.</summary>
        public const string SPALTE_RUECKKUEHLART = "Rueckkuehlart";

        /// <summary>Kleinste Teillast [%] der Nennkälteleistung.</summary>
        public const string SPALTE_MINDESTTEILLAST = "Mindestteillast_Prozent";

        /// <summary>Elektrische Leistung der Rückkühlung (Ventilatoren, Pumpen) im Nennpunkt [kW]; NULL = im EER enthalten.</summary>
        public const string SPALTE_HILFSSTROM_RUECKKUEHLUNG = "Hilfsstrom_Rueckkuehlung_kW";

        /// <summary>Kleinster zulässiger Kaltwasservorlauf [°C].</summary>
        public const string SPALTE_KALTWASSER_VORLAUF_MIN = "Kaltwasser_Vorlauf_Min";

        /// <summary>Gerätepreis [€] — Anwenderfeld wie <c>Tab_WP_STAMM.Modulkosten</c>.</summary>
        public const string SPALTE_MODULKOSTEN = "Modulkosten";

        /// <summary>Kennlinie: Eintrittstemperatur des Rückkühlmediums am Verflüssiger [°C] (luftgekühlt: Außenluft).</summary>
        public const string SPALTE_RUECKKUEHLTEMPERATUR = "Rueckkuehltemperatur";

        /// <summary>Kennlinie: Kaltwasservorlauf [°C].</summary>
        public const string SPALTE_KALTWASSERTEMPERATUR = "Kaltwassertemperatur";

        /// <summary>Kennlinie: EER im Punkt [—].</summary>
        public const string SPALTE_EER = "EER";

        /// <summary>Kennlinie: Kälteleistung im Punkt [kW].</summary>
        public const string SPALTE_KAELTELEISTUNG = "Kaelteleistung_kW";

        /// <summary>Rückkühlart luftgekühlter Verflüssiger. Persistenzwert, eingefroren.</summary>
        public const string RUECKKUEHLART_LUFT = "LUFT";

        /// <summary>Rückkühlart wassergekühlt mit fremdem Kühlwasser (Brunnen, Fluss). Persistenzwert.</summary>
        public const string RUECKKUEHLART_WASSER = "WASSER";

        /// <summary>Rückkühlart Trockenkühler. Persistenzwert.</summary>
        public const string RUECKKUEHLART_TROCKENKUEHLER = "TROCKENKUEHLER";

        /// <summary>Rückkühlart Nasskühler (Verdunstungskühlturm). Persistenzwert.</summary>
        public const string RUECKKUEHLART_NASSKUEHLER = "NASSKUEHLER";

        /// <summary>Die zulässigen Rückkühlarten in fester Folge.</summary>
        public static readonly IReadOnlyList<string> RUECKKUEHLARTEN = new[]
        {
            RUECKKUEHLART_LUFT, RUECKKUEHLART_WASSER, RUECKKUEHLART_TROCKENKUEHLER, RUECKKUEHLART_NASSKUEHLER
        };

        /// <summary>
        /// Die Fachspalten von Katalog und Projektkopie in Schemareihenfolge — die EINE Liste, an der
        /// Projektkopie, Schreibwege und Katalogfassung hängen (ohne <c>ID</c>, <c>ID_Projekt</c>,
        /// <c>ID_Stamm</c>, <c>ReadOnly</c> und die Katalogspalten).
        /// </summary>
        public static readonly string[] Fachspalten;

        /// <summary>
        /// Die zwölf Grundspalten des Kopfs, die seit diesem Schritt stehen — der Kopf, den die Schreibwege in EINER
        /// Anweisung schreiben. Die acht Spalten von Teillast und Takten (<see cref="KaeltemaschineTeillastSchema"/>,
        /// Schritt 209) schreiben sie in einem eigenen Schritt, nur wenn die Spalten stehen.
        /// </summary>
        public static readonly string[] Grundspalten =
        {
            "Bezeichner", "Firma", "Typ", "Beschreibung",
            SPALTE_NENNKAELTELEISTUNG, SPALTE_NENN_EER, SPALTE_KAELTEMITTEL, SPALTE_RUECKKUEHLART,
            SPALTE_MINDESTTEILLAST, SPALTE_HILFSSTROM_RUECKKUEHLUNG, SPALTE_KALTWASSER_VORLAUF_MIN,
            SPALTE_MODULKOSTEN
        };

        static KaeltemaschineSchema()
        {
            // KaeltemaschineTeillastSchema (Schritt 209): die acht Eingabespalten haengen hinten an; leer tragen sie nichts
            // zur Pruefsumme bei, eine Datenbank vor dem Schritt fuehrt sie nicht (Katalogfassung.VorhandeneFachspalten).
            Fachspalten = Grundspalten.Concat(KaeltemaschineTeillastSchema.EINGABESPALTEN).ToArray();
        }

        /// <summary>Die Fachspalten der Kennlinie (ohne <c>ID</c>, Fremdschlüssel, <c>ID_Projekt</c>, <c>ReadOnly</c>).</summary>
        public static readonly string[] KennlinienSpalten =
        {
            SPALTE_RUECKKUEHLTEMPERATUR, SPALTE_KALTWASSERTEMPERATUR, SPALTE_EER, SPALTE_KAELTELEISTUNG
        };

        private static string Q(string s) => "\"" + s + "\"";

        private static string Fachspaltentext() =>
            "    \"Bezeichner\" TEXT NOT NULL CHECK (length(\"Bezeichner\") BETWEEN 1 AND 255),\n" +
            "    \"Firma\" TEXT,\n" +
            "    \"Typ\" TEXT,\n" +
            "    \"Beschreibung\" TEXT,\n" +
            "    " + Q(SPALTE_NENNKAELTELEISTUNG) + " REAL CHECK (" + Q(SPALTE_NENNKAELTELEISTUNG) + " IS NULL OR " + Q(SPALTE_NENNKAELTELEISTUNG) + " > 0),\n" +
            "    " + Q(SPALTE_NENN_EER) + " REAL CHECK (" + Q(SPALTE_NENN_EER) + " IS NULL OR " + Q(SPALTE_NENN_EER) + " > 0),\n" +
            "    " + Q(SPALTE_KAELTEMITTEL) + " TEXT,\n" +
            "    " + Q(SPALTE_RUECKKUEHLART) + " TEXT CHECK (" + Q(SPALTE_RUECKKUEHLART) + " IS NULL OR " + Q(SPALTE_RUECKKUEHLART) + " IN (" +
                string.Join(",", RUECKKUEHLARTEN.Select(a => "'" + a + "'")) + ")),\n" +
            "    " + Q(SPALTE_MINDESTTEILLAST) + " REAL CHECK (" + Q(SPALTE_MINDESTTEILLAST) + " IS NULL OR " + Q(SPALTE_MINDESTTEILLAST) + " BETWEEN 0 AND 100),\n" +
            "    " + Q(SPALTE_HILFSSTROM_RUECKKUEHLUNG) + " REAL CHECK (" + Q(SPALTE_HILFSSTROM_RUECKKUEHLUNG) + " IS NULL OR " + Q(SPALTE_HILFSSTROM_RUECKKUEHLUNG) + " >= 0),\n" +
            "    " + Q(SPALTE_KALTWASSER_VORLAUF_MIN) + " REAL,\n" +
            "    " + Q(SPALTE_MODULKOSTEN) + " REAL CHECK (" + Q(SPALTE_MODULKOSTEN) + " IS NULL OR " + Q(SPALTE_MODULKOSTEN) + " >= 0)";

        private static string Kennlinientext() =>
            "    " + Q(SPALTE_RUECKKUEHLTEMPERATUR) + " REAL NOT NULL,\n" +
            "    " + Q(SPALTE_KALTWASSERTEMPERATUR) + " REAL NOT NULL,\n" +
            "    " + Q(SPALTE_EER) + " REAL CHECK (" + Q(SPALTE_EER) + " IS NULL OR " + Q(SPALTE_EER) + " > 0),\n" +
            "    " + Q(SPALTE_KAELTELEISTUNG) + " REAL CHECK (" + Q(SPALTE_KAELTELEISTUNG) + " IS NULL OR " + Q(SPALTE_KAELTELEISTUNG) + " >= 0)";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Kaeltemaschine_STAMM</c> (die Katalogspalten legt der Schritt danach an).</summary>
        public static string SqlCreateStamm() =>
            "CREATE TABLE IF NOT EXISTS " + Q(TAB_STAMM) + " (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            Fachspaltentext() + ",\n" +
            "    \"ReadOnly\" INTEGER NOT NULL DEFAULT 0 CHECK (\"ReadOnly\" IN (0,1))\n" +
            ") STRICT";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Kaeltemaschine</c> — die Projektkopie.</summary>
        public static string SqlCreateProjekt() =>
            "CREATE TABLE IF NOT EXISTS " + Q(TAB_PROJEKT) + " (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Projekt\" INTEGER NOT NULL REFERENCES \"Tab_Projekt\" (\"ID\") ON DELETE CASCADE ON UPDATE CASCADE,\n" +
            "    " + Q(SPALTE_ID_STAMM) + " INTEGER REFERENCES " + Q(TAB_STAMM) + " (\"ID\") ON DELETE SET NULL,\n" +
            Fachspaltentext() + "\n" +
            ") STRICT";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Kenndaten_Kaeltemaschine_STAMM</c>.</summary>
        public static string SqlCreateKenndatenStamm() =>
            "CREATE TABLE IF NOT EXISTS " + Q(TAB_KENNDATEN_STAMM) + " (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    " + Q(SPALTE_ID_KAELTEMASCHINE) + " INTEGER NOT NULL REFERENCES " + Q(TAB_STAMM) + " (\"ID\") ON DELETE CASCADE ON UPDATE CASCADE,\n" +
            Kennlinientext() + ",\n" +
            "    \"ReadOnly\" INTEGER NOT NULL DEFAULT 0 CHECK (\"ReadOnly\" IN (0,1)),\n" +
            "    UNIQUE (" + Q(SPALTE_ID_KAELTEMASCHINE) + ", " + Q(SPALTE_RUECKKUEHLTEMPERATUR) + ", " + Q(SPALTE_KALTWASSERTEMPERATUR) + ")\n" +
            ") STRICT";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Kenndaten_Kaeltemaschine</c>.</summary>
        public static string SqlCreateKenndaten() =>
            "CREATE TABLE IF NOT EXISTS " + Q(TAB_KENNDATEN) + " (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Projekt\" INTEGER NOT NULL REFERENCES \"Tab_Projekt\" (\"ID\") ON DELETE CASCADE ON UPDATE CASCADE,\n" +
            "    " + Q(SPALTE_ID_KAELTEMASCHINE) + " INTEGER NOT NULL REFERENCES " + Q(TAB_PROJEKT) + " (\"ID\") ON DELETE CASCADE ON UPDATE CASCADE,\n" +
            Kennlinientext() + ",\n" +
            "    UNIQUE (" + Q(SPALTE_ID_KAELTEMASCHINE) + ", " + Q(SPALTE_RUECKKUEHLTEMPERATUR) + ", " + Q(SPALTE_KALTWASSERTEMPERATUR) + ")\n" +
            ") STRICT";

        /// <summary>Die vier Tabellen in Anlegereihenfolge (Eltern vor Kindern).</summary>
        public static IReadOnlyList<KeyValuePair<string, string>> Tabellen() => new[]
        {
            new KeyValuePair<string, string>(TAB_STAMM, SqlCreateStamm()),
            new KeyValuePair<string, string>(TAB_PROJEKT, SqlCreateProjekt()),
            new KeyValuePair<string, string>(TAB_KENNDATEN_STAMM, SqlCreateKenndatenStamm()),
            new KeyValuePair<string, string>(TAB_KENNDATEN, SqlCreateKenndaten()),
        };

        /// <summary>Was der Schritt voraussetzt.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { "Tab_Projekt" };

        // =================================================================
        //  Die Saat
        // =================================================================

        /// <summary>Ein Beispielgerät der Saat.</summary>
        public sealed record Beispielgeraet(string Bezeichner, double Nennkaelteleistung, double NennEer,
                                            string Kaeltemittel, string Rueckkuehlart, double Mindestteillast,
                                            double? HilfsstromRueckkuehlung, double KaltwasserVorlaufMin,
                                            IReadOnlyList<(double Rueckkuehl, double Kaltwasser, double Eer, double Leistung)> Kennlinie);

        /// <summary>Die drei Beispielgeräte — neutrale Namen, runde Werte, je sechs Kennlinienpunkte.</summary>
        public static readonly IReadOnlyList<Beispielgeraet> SAAT = new[]
        {
            new Beispielgeraet("Kältemaschine 50 kW luftgekühlt", 50, 3.0, "R32", RUECKKUEHLART_LUFT, 25, null, 5,
                new[]
                {
                    (25.0, 6.0, 3.8, 55.0), (35.0, 6.0, 3.0, 50.0), (45.0, 6.0, 2.5, 44.0),
                    (25.0, 12.0, 4.5, 62.0), (35.0, 12.0, 3.6, 57.0), (45.0, 12.0, 2.9, 50.0),
                }),
            new Beispielgeraet("Kältemaschine 200 kW wassergekühlt mit Trockenkühler", 200, 4.0, "R513A",
                RUECKKUEHLART_TROCKENKUEHLER, 20, 6, 5,
                new[]
                {
                    (25.0, 6.0, 5.0, 215.0), (35.0, 6.0, 4.0, 200.0), (45.0, 6.0, 3.1, 180.0),
                    (25.0, 12.0, 5.8, 240.0), (35.0, 12.0, 4.7, 225.0), (45.0, 12.0, 3.6, 205.0),
                }),
            new Beispielgeraet("Kältemaschine 500 kW wassergekühlt mit Nasskühler", 500, 4.3, "R1234ze",
                RUECKKUEHLART_NASSKUEHLER, 15, 10, 5,
                new[]
                {
                    (25.0, 6.0, 5.4, 535.0), (35.0, 6.0, 4.3, 500.0), (45.0, 6.0, 3.3, 455.0),
                    (25.0, 12.0, 6.0, 600.0), (35.0, 12.0, 5.0, 565.0), (45.0, 12.0, 3.9, 515.0),
                }),
        };

        internal static readonly string SQL_SAAT_GERAET =
            "INSERT INTO " + Q(TAB_STAMM) + " (\"Bezeichner\", " + Q(SPALTE_NENNKAELTELEISTUNG) + ", " + Q(SPALTE_NENN_EER) + ", " +
            Q(SPALTE_KAELTEMITTEL) + ", " + Q(SPALTE_RUECKKUEHLART) + ", " + Q(SPALTE_MINDESTTEILLAST) + ", " +
            Q(SPALTE_HILFSSTROM_RUECKKUEHLUNG) + ", " + Q(SPALTE_KALTWASSER_VORLAUF_MIN) + ", \"ReadOnly\") " +
            "SELECT ?, ?, ?, ?, ?, ?, ?, ?, 1 WHERE NOT EXISTS (SELECT 1 FROM " + Q(TAB_STAMM) + " WHERE \"Bezeichner\" = ?)";

        internal static readonly string SQL_SAAT_PUNKT =
            "INSERT OR IGNORE INTO " + Q(TAB_KENNDATEN_STAMM) + " (" + Q(SPALTE_ID_KAELTEMASCHINE) + ", " +
            Q(SPALTE_RUECKKUEHLTEMPERATUR) + ", " + Q(SPALTE_KALTWASSERTEMPERATUR) + ", " + Q(SPALTE_EER) + ", " +
            Q(SPALTE_KAELTELEISTUNG) + ", \"ReadOnly\") " +
            "SELECT ID, ?, ?, ?, ?, 1 FROM " + Q(TAB_STAMM) + " WHERE \"Bezeichner\" = ? AND \"ReadOnly\" = 1";

        /// <summary>Legt die Beispielgeräte samt Kennlinie an, die noch fehlen; liefert die Zahl neuer Zeilen.</summary>
        public static int Saat(IList<string> bericht)
        {
            int g = 0, p = 0;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach (Beispielgeraet b in SAAT)
                    {
                        g += v.Ausfuehren(SQL_SAAT_GERAET,
                            new DbParam("?", b.Bezeichner), new DbParam("?", b.Nennkaelteleistung),
                            new DbParam("?", b.NennEer), new DbParam("?", b.Kaeltemittel),
                            new DbParam("?", b.Rueckkuehlart), new DbParam("?", b.Mindestteillast),
                            new DbParam("?", (object)b.HilfsstromRueckkuehlung ?? DBNull.Value),
                            new DbParam("?", b.KaltwasserVorlaufMin), new DbParam("?", b.Bezeichner));
                        foreach (var k in b.Kennlinie)
                            p += v.Ausfuehren(SQL_SAAT_PUNKT,
                                new DbParam("?", k.Rueckkuehl), new DbParam("?", k.Kaltwasser),
                                new DbParam("?", k.Eer), new DbParam("?", k.Leistung),
                                new DbParam("?", b.Bezeichner));
                    }
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            bericht?.Add(TAB_STAMM + ": " + g.ToString(CultureInfo.InvariantCulture) + " Beispielgeraet(e) angelegt");
            bericht?.Add(TAB_KENNDATEN_STAMM + ": " + p.ToString(CultureInfo.InvariantCulture) + " Kennlinienpunkt(e) angelegt");
            return g + p;
        }

        // =================================================================
        //  Stand und Ausführung
        // =================================================================

        /// <summary>Stehen die vier Tabellen samt Katalogspalten?</summary>
        public static bool SchemaVollstaendig() =>
            Tabellen().All(t => DataRepository.TabelleVorhanden(t.Key))
            && KatalogfassungSchema.KatalogspaltenVollstaendig(Katalogfassung.Stufe3);

        /// <summary>Steht das Schema, und trägt der Katalog die Saat samt Katalogschlüssel?</summary>
        public static bool Vollstaendig()
        {
            if (!SchemaVollstaendig()) return false;
            foreach (Beispielgeraet b in SAAT)
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM " + Q(TAB_KENNDATEN_STAMM) + " k JOIN " + Q(TAB_STAMM) +
                    " s ON s.ID = k." + Q(SPALTE_ID_KAELTEMASCHINE) + " WHERE s.\"Bezeichner\" = ?",
                    new DbParam("?", b.Bezeichner));
                if (o == null || o == DBNull.Value || Convert.ToInt32(o, CultureInfo.InvariantCulture) < b.Kennlinie.Count) return false;
            }
            return KatalogSchluesselSaat.OffeneSaetze(Katalogfassung.Stufe3) == 0;
        }

        /// <summary>
        /// Legt fehlende Tabellen, die Katalogspalten und die Saat an und stempelt die ausgelieferten Sätze
        /// (Katalogschlüssel und Prüfsumme). Wiederholbar; liefert die Zahl der Schemaanweisungen.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            int n = 0;
            foreach (KeyValuePair<string, string> t in Tabellen())
            {
                if (DataRepository.TabelleVorhanden(t.Key)) continue;
                DataRepository.ExecuteNonQuery(t.Value);
                bericht?.Add(t.Key + " angelegt");
                n++;
            }
            foreach (KeyValuePair<string, string> a in KatalogfassungSchema.KatalogspaltenAnweisungen(Katalogfassung.Stufe3).ToList())
            {
                DataRepository.ExecuteNonQuery(a.Value);
                bericht?.Add(a.Key);
                n++;
            }
            if (n == 0) bericht?.Add("Tabellen der Kaeltemaschine vorhanden");
            Saat(bericht);
            KatalogSchluesselSaat.Ausfuehren(bericht, Katalogfassung.Stufe3);
            bericht?.Add("KEIN Rechenergebnis aendert sich, der Referenzlauf bleibt byte-gleich");
            return n;
        }

        /// <summary>
        /// Trägt <c>ID_Stamm</c> der Projektkopien eines Projekts über den Bezeichner nach — nur bei genau einem
        /// Treffer im Katalog (Projektpaket: der Verweis reist nicht, Muster <c>WaermepumpeKatalogverweis</c>).
        /// Ein Parameter: die Projekt-ID.
        /// </summary>
        public static string SqlNachtragProjekt() =>
            "UPDATE " + Q(TAB_PROJEKT) + " SET " + Q(SPALTE_ID_STAMM) + " = (SELECT s.\"ID\" FROM " + Q(TAB_STAMM) + " s " +
            "WHERE s.\"Bezeichner\" = " + Q(TAB_PROJEKT) + ".\"Bezeichner\") " +
            "WHERE \"ID_Projekt\" = ? AND " + Q(SPALTE_ID_STAMM) + " IS NULL " +
            "AND (SELECT COUNT(*) FROM " + Q(TAB_STAMM) + " s2 WHERE s2.\"Bezeichner\" = " + Q(TAB_PROJEKT) + ".\"Bezeichner\") = 1";

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
