using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // TEILLAST- UND BRENNWERTKENNLINIE DES HEIZKESSELS - Etappe E1 „Daten + Import" des
    // Konzepts Dokumentation/aktuell/Konzept_Kessel_Kennlinie_EPOS-Plan.md (Abschnitte 3.1
    // und 3.2, Statusnummer #569; Anwenderentscheide 29.09.2026 F1 bis F4).
    //
    // WOZU. Der Heizkessel rechnet mit einem festen Wirkungsgrad. Die fuenf Spalten tragen,
    // was eine optionale Kennlinie spaeter braucht: den Wirkungsgrad bei 30 % Last, den
    // Schalter der Brennwertkennlinie und die drei Groessen des Taktmodells. In E1 sind sie
    // DATEN - kein Rechenweg liest sie; die Rechenwirkung kommt mit E2 bis E4.
    //
    // FUENF SPALTEN, an Tab_Heizkessel_STAMM UND Tab_Heizkessel gleich (die Projektkopie
    // traegt den Katalogsatz Spalte fuer Spalte):
    //   Wirkungsgrad_Teillast30  REAL, nullbar     - eta bei 30 % Last, Hi, Faktor
    //   Kennlinie_Brennwert      INTEGER NOT NULL DEFAULT 0 CHECK (IN (0,1))
    //   Mindestleistung          REAL, nullbar     - untere Modulationsgrenze [kW]
    //   Anfahrverlust_kWh        REAL, nullbar     - Brennstoff je Start [kWh]
    //   Mindestlaufzeit_min      INTEGER, nullbar  - Mindestlaufzeit je Start [min]
    // Leer heisst jeweils „nicht gepflegt"; was dann gilt, legen E2 und E4 fest (Normvorgaben
    // nach Entscheid F1, Nachtrag im Konzept). Beide Tabellen sind STRICT; REAL und INTEGER
    // sind dort zulaessig. Der Schalter traegt die Boolean-Regel aus BETRIEB_SQLITE.md § 6.
    //
    // KEIN DML. Die nullbaren Spalten entstehen in jeder Zeile als NULL, der Schalter als 0 -
    // der Kessel rechnet damit genau wie vorher. Die Nachpflege des Katalogs aus VDI 3805
    // (Entscheid F2) ist ein WERKZEUGWEG (KesselkatalogNachpflege), kein Schemaschritt: Er
    // braucht die Herstellerdateien, und er beruehrt nie eine Projektkopie.
    //
    // DER NAME STEHT NUR ALS ARGUMENT. Dasselbe Muster wie bei SolarkollektorTemperaturen und
    // KesselHeizgrenzeSchema: Die Anweisung entsteht in einer Schleife aus Tabelle, Spalte und
    // Typ, Werkzeuge/SqlDialektPruefer sieht deshalb keinen fertigen Text, der gegen die bereits
    // migrierte Testdatenbank „duplicate column" waere.
    //
    // DREI LESER: der Schemaschritt in WindowsFormsApplication1/Allgemein/Update/
    // SchemaMigration.cs, das Werkzeug Werkzeuge/Testdatenbankschema und die Testvorrichtung
    // samt Nachweis in EPOS.Kern.Tests.
    // ====================================================================================

    /// <summary>
    /// Die Kennlinienspalten des Heizkessels (Konzept Kesselkennlinie 3.1, Etappe E1) — EINE
    /// Quelle für Migration, Werkzeug, Testkopie, Controller und Nachweis (ADR-001 Option C).
    /// Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class KesselKennlinieSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht. Vergeben
        /// unmittelbar vor dem Schemacommit gegen <c>origin</c> (Regel „lückenlos"): Sie folgt
        /// auf die Heizgrenze der Kesselbereitschaft (<see cref="KesselHeizgrenzeSchema"/>).
        /// </summary>
        public const int SCHRITT = KesselHeizgrenzeSchema.SCHRITT + 1;

        /// <summary>Der Katalog der Auslieferung.</summary>
        public const string TAB_STAMM = SchemaKatalog.TAB_HEIZKESSEL_STAMM;

        /// <summary>Die Projektkopien.</summary>
        public const string TAB_PROJEKT = SchemaKatalog.TAB_HEIZKESSEL;

        /// <summary>
        /// <c>Wirkungsgrad_Teillast30</c>: Wirkungsgrad bei 30 % Last, heizwertbezogen, als
        /// Faktor. Ein Wert über 1,5 gilt als Prozentangabe (dieselbe Regel wie beim
        /// Nennwirkungsgrad). NULL = keine Teillastkennlinie gepflegt.
        /// </summary>
        public const string SPALTE_TEILLAST30 = "Wirkungsgrad_Teillast30";

        /// <summary>
        /// <c>Kennlinie_Brennwert</c>: die Brennwertkennlinie rechnen (0/1). Zulässig nur bei
        /// <c>Brennwert</c> = 1 — der Controller hält die Regel.
        /// </summary>
        public const string SPALTE_KENNLINIE_BRENNWERT = "Kennlinie_Brennwert";

        /// <summary><c>Mindestleistung</c> [kW]: die untere Modulationsgrenze. NULL = nicht gepflegt.</summary>
        public const string SPALTE_MINDESTLEISTUNG = "Mindestleistung";

        /// <summary><c>Anfahrverlust_kWh</c> [kWh]: Brennstoff je Start. NULL = nicht gepflegt.</summary>
        public const string SPALTE_ANFAHRVERLUST = "Anfahrverlust_kWh";

        /// <summary><c>Mindestlaufzeit_min</c> [min]: Laufzeit je Start im Takten. NULL = nicht gepflegt.</summary>
        public const string SPALTE_MINDESTLAUFZEIT = "Mindestlaufzeit_min";

        /// <summary>Die beiden Tabellen, in dieser Reihenfolge: Katalog, dann Projektkopien.</summary>
        public static readonly string[] TABELLEN = { TAB_STAMM, TAB_PROJEKT };

        /// <summary>
        /// Die fünf Spalten mit ihrer Typdefinition, in der Reihenfolge des Konzepts. Der
        /// Schalter ist <c>NOT NULL DEFAULT 0</c> mit Prüfklausel (Boolean-Regel), die übrigen
        /// sind nullbar ohne Vorgabe — leer heißt „nicht gepflegt", nicht 0.
        /// </summary>
        public static readonly IReadOnlyList<KeyValuePair<string, string>> SPALTEN = new[]
        {
            new KeyValuePair<string, string>(SPALTE_TEILLAST30, "REAL"),
            new KeyValuePair<string, string>(SPALTE_KENNLINIE_BRENNWERT,
                "INTEGER NOT NULL DEFAULT 0 CHECK (\"" + SPALTE_KENNLINIE_BRENNWERT + "\" IN (0,1))"),
            new KeyValuePair<string, string>(SPALTE_MINDESTLEISTUNG, "REAL"),
            new KeyValuePair<string, string>(SPALTE_ANFAHRVERLUST, "REAL"),
            new KeyValuePair<string, string>(SPALTE_MINDESTLAUFZEIT, "INTEGER")
        };

        /// <summary>Stehen alle zehn Spalten (fünf je Tabelle)? Dann ist der Schritt gelaufen.</summary>
        public static bool Vollstaendig()
        {
            foreach (string tabelle in TABELLEN)
                foreach (KeyValuePair<string, string> s in SPALTEN)
                    if (!DataRepository.SpalteVorhanden(tabelle, s.Key)) return false;
            return true;
        }

        /// <summary>
        /// Stehen die fünf Spalten an <paramref name="tabelle"/>? Die Controller fragen das,
        /// bevor sie die Spalten lesen oder schreiben — eine nicht migrierte Datenbank führt
        /// sie noch nicht, und ein Schreibweg darf daran nicht scheitern.
        /// </summary>
        public static bool SpaltenVorhanden(string tabelle)
        {
            foreach (KeyValuePair<string, string> s in SPALTEN)
                if (!DataRepository.SpalteVorhanden(tabelle, s.Key)) return false;
            return true;
        }

        /// <summary>
        /// Die Anweisungen des Schritts — Beschreibung und SQL, je fehlende Spalte eine; leer,
        /// wenn alles steht (<b>wiederholbar</b>). Fehlt eine Tabelle ganz, meldet
        /// <see cref="DataRepository.SpalteVorhanden"/> <c>false</c>, und das
        /// <c>ALTER TABLE</c> scheitert benannt.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                foreach (string tabelle in TABELLEN)
                    foreach (KeyValuePair<string, string> s in SPALTEN)
                    {
                        if (DataRepository.SpalteVorhanden(tabelle, s.Key)) continue;
                        yield return new KeyValuePair<string, string>(
                            tabelle + "." + s.Key + " anlegen",
                            "ALTER TABLE \"" + tabelle + "\" ADD COLUMN \"" + s.Key + "\" " + s.Value);
                    }
            }
        }

        /// <summary>
        /// Führt den Schritt aus — für <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>; die Migration der Schale geht denselben Weg über ihre
        /// eigenen Helfer, aus derselben <see cref="Anweisungen"/>. <b>Kein DML.</b>
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (0 bis 10).</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            int n = 0;
            foreach (KeyValuePair<string, string> a in new List<KeyValuePair<string, string>>(Anweisungen))
            {
                DataRepository.ExecuteNonQuery(a.Value);
                n++;
                bericht?.Add(a.Key);
            }
            if (n == 0) bericht?.Add(TAB_STAMM + " und " + TAB_PROJEKT + ": Kennlinienspalten vorhanden");
            return n;
        }
    }
}
