using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KP-S2 - DIE PROJEKTEINSTELLUNG DER AUFHEIZOPTIMIERUNG (Entwurf KP3 Abschnitt 4,
    // Teilkonzept 5.3; Festlegungen 15, 23, 24).
    //
    // WOZU. Die Aufheizoptimierung zieht den Sprung vom Absenk- auf den Tagsollwert als Rampe
    // vor, so dass die Aufheizleistung P_auf nicht ueberschritten wird. Ob sie rechnet und wie
    // sie bemisst, ist eine Projekteinstellung - fuenf Spalten an Tab_Einstellungen:
    //
    //   Aufheizoptimierung  INTEGER NOT NULL DEFAULT 0 CHECK (IN (0,1))  - der Schalter
    //   Aufheiz_Bemessung   TEXT CHECK (IN ('STUNDE','STUNDE_ABZUG'))    - NULL = (a)
    //   Aufheiz_Abzug_K     REAL CHECK (BETWEEN 0 AND 10)                - NULL = 2 K
    //   Aufheiz_Reserve     REAL CHECK (> 0 AND <= 1)                    - NULL = 0,2
    //   Aufheiz_Art         TEXT CHECK (IN ('TAEGLICH','FEST'))          - NULL = taeglich
    //
    // Die Reserve ist ein Anteil, die Oberflaeche zeigt Prozent; rho = 0 ist ausgeschlossen
    // (Festlegung 15). Die Wertlisten kommen aus DbWerte.AUFHEIZ_*.
    //
    // KEIN DML, KEIN TABELLENNEUBAU. Der Schalter entsteht in jeder Zeile als 0, die uebrigen
    // Spalten als NULL - jedes Projekt rechnet danach wie vorher („aus"). SQLite prueft ein
    // CHECK beim ADD COLUMN gegen die vorhandenen Zeilen; NULL und die Vorgabe 0 bestehen ihn.
    // Tab_Einstellungen traegt keinen Kostenstempel (Schritt 159), die Trigger bleiben stehen.
    //
    // DER NAME STEHT NUR ALS ARGUMENT (Muster KesselKennlinieSchema): Die Anweisung entsteht in
    // einer Schleife aus Spalte und Typ; Werkzeuge/SqlDialektPruefer sieht keinen fertigen Text,
    // der gegen die bereits migrierte Testdatenbank „duplicate column" waere.
    //
    // DREI LESER: der Schemaschritt in WindowsFormsApplication1/Allgemein/Update/
    // SchemaMigration.cs, das Werkzeug Werkzeuge/Testdatenbankschema und die Testvorrichtung
    // samt Nachweis in EPOS.Kern.Tests; dazu KonfigurationCtrl.AufheizvorgabeLesen/-Schreiben.
    // ====================================================================================

    /// <summary>
    /// <b>KP-S2</b> — die fünf Spalten der Aufheizoptimierung an <c>Tab_Einstellungen</c> (Entwurf KP3
    /// Abschnitt 4) — EINE Quelle für Migration, Werkzeug, Testkopie, Controller und Nachweis
    /// (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class AufheizvorgabeSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht. Vergeben unmittelbar
        /// vor dem Schemacommit gegen <c>origin</c> (Festlegung 23, ADR-001 A11): der Schritt hinter den
        /// Änderungsstempeln.
        /// </summary>
        public const int SCHRITT = KostenStempelSchema.SCHRITT + 1;

        /// <summary>Die Tabelle der Projekteinstellungen.</summary>
        public const string TABELLE = SchemaKatalog.TAB_EINSTELLUNGEN;

        /// <summary><c>Aufheizoptimierung</c> (0/1, NOT NULL DEFAULT 0): der Projektschalter; 0 = aus.</summary>
        public const string SPALTE_SCHALTER = "Aufheizoptimierung";

        /// <summary><c>Aufheiz_Bemessung</c>: <see cref="DbWerte.AUFHEIZ_BEMESSUNGEN"/>; NULL = (a) kälteste Stunde.</summary>
        public const string SPALTE_BEMESSUNG = "Aufheiz_Bemessung";

        /// <summary><c>Aufheiz_Abzug_K</c> [K]: ΔT_K der Bemessung (b), 0 bis 10; NULL = <see cref="ABZUG_VORGABE_K"/>.</summary>
        public const string SPALTE_ABZUG = "Aufheiz_Abzug_K";

        /// <summary><c>Aufheiz_Reserve</c>: die Aufheizreserve ρ als Anteil, über 0 bis 1; NULL = <see cref="RESERVE_VORGABE"/>.</summary>
        public const string SPALTE_RESERVE = "Aufheiz_Reserve";

        /// <summary><c>Aufheiz_Art</c>: <see cref="DbWerte.AUFHEIZ_ARTEN"/>; NULL = täglich.</summary>
        public const string SPALTE_ART = "Aufheiz_Art";

        /// <summary>Die kleinste zulässige ΔT_K [K] (Prüfklausel).</summary>
        public const double ABZUG_MIN_K = 0;

        /// <summary>Die größte zulässige ΔT_K [K] (Prüfklausel).</summary>
        public const double ABZUG_MAX_K = 10;

        /// <summary>Die Vorgabe der ΔT_K [K], wenn die Spalte NULL ist.</summary>
        public const double ABZUG_VORGABE_K = 2;

        /// <summary>Die größte zulässige Reserve (100 %); die kleinste ist ausgeschlossen 0 (Festlegung 15).</summary>
        public const double RESERVE_MAX = 1;

        /// <summary>Die Vorgabe der Reserve ρ, wenn die Spalte NULL ist (20 %; E58 F7 (c)).</summary>
        public const double RESERVE_VORGABE = 0.2;

        /// <summary>Spaltenzahl von <c>Tab_Einstellungen</c> vor diesem Schritt (Nachweis in den Tests).</summary>
        public const int SPALTENZAHL_VORHER = 32;

        /// <summary>Spaltenzahl von <c>Tab_Einstellungen</c> nach diesem Schritt.</summary>
        public const int SPALTENZAHL = SPALTENZAHL_VORHER + 5;

        /// <summary>
        /// Die fünf Spalten mit ihrer Typdefinition, in der Reihenfolge des Entwurfs. Der Schalter ist
        /// <c>NOT NULL DEFAULT 0</c> mit Prüfklausel (Boolean-Regel), die übrigen sind nullbar ohne
        /// Vorgabe — leer heißt „die Vorgabe", kein DDL-DEFAULT auf einem Fachwert.
        /// </summary>
        public static readonly IReadOnlyList<KeyValuePair<string, string>> SPALTEN = new[]
        {
            new KeyValuePair<string, string>(SPALTE_SCHALTER,
                "INTEGER NOT NULL DEFAULT 0 CHECK (\"" + SPALTE_SCHALTER + "\" IN (0,1))"),
            new KeyValuePair<string, string>(SPALTE_BEMESSUNG,
                "TEXT CHECK (\"" + SPALTE_BEMESSUNG + "\" IN (" + Liste(DbWerte.AUFHEIZ_BEMESSUNGEN) + "))"),
            new KeyValuePair<string, string>(SPALTE_ABZUG,
                "REAL CHECK (\"" + SPALTE_ABZUG + "\" BETWEEN " + Zahl(ABZUG_MIN_K) + " AND " + Zahl(ABZUG_MAX_K) + ")"),
            new KeyValuePair<string, string>(SPALTE_RESERVE,
                "REAL CHECK (\"" + SPALTE_RESERVE + "\" > 0 AND \"" + SPALTE_RESERVE + "\" <= " + Zahl(RESERVE_MAX) + ")"),
            new KeyValuePair<string, string>(SPALTE_ART,
                "TEXT CHECK (\"" + SPALTE_ART + "\" IN (" + Liste(DbWerte.AUFHEIZ_ARTEN) + "))"),
        };

        /// <summary>Stehen alle fünf Spalten? Dann ist der Schritt gelaufen.</summary>
        public static bool Vollstaendig()
        {
            foreach (KeyValuePair<string, string> s in SPALTEN)
                if (!DataRepository.SpalteVorhanden(TABELLE, s.Key)) return false;
            return true;
        }

        /// <summary>
        /// Die Anweisungen des Schritts — Beschreibung und SQL, je fehlende Spalte eine; leer, wenn alles
        /// steht (<b>wiederholbar</b>). Fehlt die Tabelle ganz, meldet
        /// <see cref="DataRepository.SpalteVorhanden"/> <c>false</c>, und das <c>ALTER TABLE</c> scheitert
        /// benannt.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                foreach (KeyValuePair<string, string> s in SPALTEN)
                {
                    if (DataRepository.SpalteVorhanden(TABELLE, s.Key)) continue;
                    yield return new KeyValuePair<string, string>(
                        TABELLE + "." + s.Key + " anlegen",
                        "ALTER TABLE \"" + TABELLE + "\" ADD COLUMN \"" + s.Key + "\" " + s.Value);
                }
            }
        }

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang aus — für <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>; die Migration der Schale geht denselben Weg über ihre eigenen Helfer,
        /// aus derselben <see cref="Anweisungen"/>. <b>Kein DML.</b>
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (0 bis 5).</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen Verbindung
            // und saehe die offene Transaktion nicht.
            List<KeyValuePair<string, string>> offen = Anweisungen.ToList();
            if (offen.Count == 0)
            {
                bericht?.Add(TABELLE + ": Spalten der Aufheizoptimierung vorhanden");
                return 0;
            }
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach (KeyValuePair<string, string> a in offen) v.Ausfuehren(a.Value);
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            foreach (KeyValuePair<string, string> a in offen) bericht?.Add(a.Key);
            return offen.Count;
        }

        /// <summary>Eine Wertliste als SQL-Literal: <c>'A','B'</c>.</summary>
        internal static string Liste(IEnumerable<string> werte)
            => string.Join(",", werte.Select(w => "'" + w + "'"));

        /// <summary>Eine Zahl als SQL-Literal, kulturfest.</summary>
        internal static string Zahl(double wert) => wert.ToString("R", CultureInfo.InvariantCulture);
    }
}
