using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // ERZEUGER IN TEILLAST: WÄRMEPUMPE UND BHKW - Welle M4 der Entscheidungsvorlage
    // „Modellgrenzen der Rechenwege" (WP1 Taktverlust nach EN 14825, BH1 Teillastkennlinie des
    // BHKW nach EN 15316-4-4, BH2 Takten des BHKW mit Mindestlaufzeit und Anfahrverlust).
    //
    // WO DIE FELDER STEHEN. Die Kennwerte gehören zum Gerät, nicht zur Anlage: Sie stehen im
    // Katalog (Tab_WP_STAMM, Tab_BHKW_STAMM) und in der Projektkopie (Tab_WP, Tab_BHKW), die den
    // Katalogsatz Spalte für Spalte trägt.
    //
    // ZWEI SPALTEN an Tab_WP_STAMM und Tab_WP, nullbar mit Prüfklausel (leer = Vorgabe):
    //   Mindestleistung_kW     REAL  0 … 1 000   kleinste Modulationsleistung; leer = keine Taktrechnung
    //   Taktverlustfaktor_Cd   REAL  0 … 1       Teillastkoeffizient C_d; leer = 0,9 (EN 14825)
    // VIER SPALTEN an Tab_BHKW_STAMM und Tab_BHKW, nullbar mit Prüfklausel:
    //   Wirkungsgrad_el_Teillast50  REAL     0 … 1    η_el bei 50 % elektrischer Last (Faktor wie
    //                                                  Wirkungsgrad_el); leer = wie Volllast
    //   Wirkungsgrad_th_Teillast50  REAL     0 … 1    η_th bei 50 % elektrischer Last; leer = wie Volllast
    //   Anfahrverlust_kWh           REAL     0 … 100  Brennstoff je Start [kWh]
    //   Mindestlaufzeit_min         INTEGER  0 … 60   Laufzeit je Start im Takten [min]
    // Ohne Anfahrverlust und Mindestlaufzeit taktet das Modul nicht - es bleibt unter seiner
    // Untergrenze in der Stunde aus. Alle vier Tabellen sind STRICT; REAL und INTEGER sind dort
    // zulässig.
    //
    // DIE WIRKUNGSGRADE SIND FAKTOREN wie ihre Nachbarspalten Wirkungsgrad_el und Wirkungsgrad_th
    // (Schemaschritte 98 und 99): 0,34 heißt 34 %. Die Oberfläche zeigt Prozent.
    //
    // KEIN DML. Alle Spalten entstehen leer, und leer rechnet jeder Erzeuger wie zuvor: keine
    // Taktrechnung der Wärmepumpe, Volllastwirkungsgrade für jede Auslastung des BHKW, kein Takten.
    // Der Referenzlauf bleibt byte-gleich.
    //
    // DIE NAMEN STEHEN NUR ALS ARGUMENT (Muster SolarthermieFelderSchema): Die Anweisung entsteht
    // aus Tabelle, Spalte und Typ; Werkzeuge/SqlDialektPruefer sieht keinen fertigen Text, der
    // gegen eine migrierte Datenbank „duplicate column" wäre.
    //
    // VIER LESER: der Schemaschritt in WindowsFormsApplication1/Allgemein/Update/
    // SchemaMigration.cs, das Werkzeug Werkzeuge/Testdatenbankschema, die Testvorrichtung samt
    // Nachweis in EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// Die Teillastfelder von Wärmepumpe und BHKW an Katalog und Projektkopie — EINE Quelle für
    /// Migration, Werkzeug, Testkopie, Controller, Rechenweg und Nachweis (ADR-001 Option C).
    /// Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class ErzeugerTeillastSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht: der nächste
        /// Schritt hinter den Feldern des Kollektorfelds.
        /// </summary>
        public const int SCHRITT = SolarthermieFelderSchema.SCHRITT + 1;

        /// <summary>Der Wärmepumpenkatalog der Auslieferung.</summary>
        public const string TAB_WP_STAMM = "Tab_WP_STAMM";

        /// <summary>Die Projektkopien der Wärmepumpen.</summary>
        public const string TAB_WP = "Tab_WP";

        /// <summary>Der BHKW-Katalog der Auslieferung.</summary>
        public const string TAB_BHKW_STAMM = SchemaKatalog.TAB_BHKW_STAMM;

        /// <summary>Die Projektkopien der BHKW-Module.</summary>
        public const string TAB_BHKW = SchemaKatalog.TAB_BHKW;

        /// <summary>Kleinste Modulationsleistung der Wärmepumpe [kW] (WP1).</summary>
        public const string SPALTE_WP_MINDESTLEISTUNG = "Mindestleistung_kW";

        /// <summary>Teillastkoeffizient C_d der Wärmepumpe nach EN 14825 [-] (WP1).</summary>
        public const string SPALTE_WP_CD = "Taktverlustfaktor_Cd";

        /// <summary>Elektrischer Wirkungsgrad des BHKW bei 50 % elektrischer Last [Faktor] (BH1).</summary>
        public const string SPALTE_BHKW_ETA_EL50 = "Wirkungsgrad_el_Teillast50";

        /// <summary>Thermischer Wirkungsgrad des BHKW bei 50 % elektrischer Last [Faktor] (BH1).</summary>
        public const string SPALTE_BHKW_ETA_TH50 = "Wirkungsgrad_th_Teillast50";

        /// <summary>Anfahrverlust des BHKW je Start [kWh Brennstoff] (BH2).</summary>
        public const string SPALTE_BHKW_ANFAHRVERLUST = "Anfahrverlust_kWh";

        /// <summary>Mindestlaufzeit des BHKW je Start [min] (BH2).</summary>
        public const string SPALTE_BHKW_MINDESTLAUFZEIT = "Mindestlaufzeit_min";

        /// <summary>Obergrenze der Mindestleistung der Wärmepumpe [kW].</summary>
        public const double WP_MINDESTLEISTUNG_MAX_KW = 1000;

        /// <summary>Obergrenze des Teillastkoeffizienten C_d.</summary>
        public const double CD_MAX = 1;

        /// <summary>Obergrenze der Teillastwirkungsgrade des BHKW (Faktor).</summary>
        public const double ETA50_MAX = 1;

        /// <summary>Obergrenze des Anfahrverlusts je Start [kWh].</summary>
        public const double ANFAHRVERLUST_MAX_KWH = 100;

        /// <summary>Obergrenze der Mindestlaufzeit [min] — eine Stunde, das Raster des Laufs.</summary>
        public const int MINDESTLAUFZEIT_MAX_MIN = 60;

        /// <summary>Eine Zahlenspalte mit Prüfklausel; leer bleibt zulässig.</summary>
        private static string Zahl(string spalte, double max)
            => "REAL CHECK (\"" + spalte + "\" BETWEEN 0 AND " +
               max.ToString(CultureInfo.InvariantCulture) + ")";

        /// <summary>Eine ganzzahlige Spalte mit Prüfklausel; leer bleibt zulässig.</summary>
        private static string Ganz(string spalte, int max)
            => "INTEGER CHECK (\"" + spalte + "\" BETWEEN 0 AND " +
               max.ToString(CultureInfo.InvariantCulture) + ")";

        /// <summary>Die zwei Spalten der Wärmepumpe in dieser Reihenfolge — Name und Typdefinition.</summary>
        public static readonly KeyValuePair<string, string>[] WP_SPALTEN =
        {
            new KeyValuePair<string, string>(SPALTE_WP_MINDESTLEISTUNG, Zahl(SPALTE_WP_MINDESTLEISTUNG, WP_MINDESTLEISTUNG_MAX_KW)),
            new KeyValuePair<string, string>(SPALTE_WP_CD, Zahl(SPALTE_WP_CD, CD_MAX)),
        };

        /// <summary>Die vier Spalten des BHKW in dieser Reihenfolge — Name und Typdefinition.</summary>
        public static readonly KeyValuePair<string, string>[] BHKW_SPALTEN =
        {
            new KeyValuePair<string, string>(SPALTE_BHKW_ETA_EL50, Zahl(SPALTE_BHKW_ETA_EL50, ETA50_MAX)),
            new KeyValuePair<string, string>(SPALTE_BHKW_ETA_TH50, Zahl(SPALTE_BHKW_ETA_TH50, ETA50_MAX)),
            new KeyValuePair<string, string>(SPALTE_BHKW_ANFAHRVERLUST, Zahl(SPALTE_BHKW_ANFAHRVERLUST, ANFAHRVERLUST_MAX_KWH)),
            new KeyValuePair<string, string>(SPALTE_BHKW_MINDESTLAUFZEIT, Ganz(SPALTE_BHKW_MINDESTLAUFZEIT, MINDESTLAUFZEIT_MAX_MIN)),
        };

        /// <summary>Die Tabellen der Wärmepumpe, in dieser Reihenfolge: Katalog, dann Projektkopien.</summary>
        public static readonly string[] WP_TABELLEN = { TAB_WP_STAMM, TAB_WP };

        /// <summary>Die Tabellen des BHKW, in dieser Reihenfolge: Katalog, dann Projektkopien.</summary>
        public static readonly string[] BHKW_TABELLEN = { TAB_BHKW_STAMM, TAB_BHKW };

        /// <summary>Die vier Tabellen des Schritts — für die Prüfung, dass sie vorhanden sind.</summary>
        public static readonly string[] TABELLEN = { TAB_WP_STAMM, TAB_WP, TAB_BHKW_STAMM, TAB_BHKW };

        /// <summary>Alle Spalten des Schritts als (Tabelle, Spalte, Typ), in Anweisungsfolge.</summary>
        private static IEnumerable<(string Tabelle, string Spalte, string Typ)> Spalten()
        {
            foreach (string tabelle in WP_TABELLEN)
                foreach (KeyValuePair<string, string> s in WP_SPALTEN)
                    yield return (tabelle, s.Key, s.Value);
            foreach (string tabelle in BHKW_TABELLEN)
                foreach (KeyValuePair<string, string> s in BHKW_SPALTEN)
                    yield return (tabelle, s.Key, s.Value);
        }

        /// <summary>Stehen alle zwölf Spalten? Dann ist der Schritt gelaufen.</summary>
        public static bool Vollstaendig()
        {
            foreach (var s in Spalten())
                if (!DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)) return false;
            return true;
        }

        /// <summary>
        /// Stehen die Spalten der Wärmepumpe an <paramref name="tabelle"/>? Controller und Rechenweg
        /// fragen das, bevor sie lesen oder schreiben — eine nicht migrierte Datenbank führt sie
        /// noch nicht, und ein Schreibweg darf daran nicht scheitern.
        /// </summary>
        public static bool WpSpaltenVorhanden(string tabelle)
        {
            foreach (KeyValuePair<string, string> s in WP_SPALTEN)
                if (!DataRepository.SpalteVorhanden(tabelle, s.Key)) return false;
            return true;
        }

        /// <summary>Stehen die Spalten des BHKW an <paramref name="tabelle"/>? Wie <see cref="WpSpaltenVorhanden"/>.</summary>
        public static bool BhkwSpaltenVorhanden(string tabelle)
        {
            foreach (KeyValuePair<string, string> s in BHKW_SPALTEN)
                if (!DataRepository.SpalteVorhanden(tabelle, s.Key)) return false;
            return true;
        }

        /// <summary>
        /// Die Anweisungen des Schritts — Beschreibung und SQL, je fehlende Spalte eine; leer,
        /// wenn alles steht (<b>wiederholbar</b>).
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                foreach (var s in Spalten())
                {
                    if (DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)) continue;
                    yield return new KeyValuePair<string, string>(
                        s.Tabelle + "." + s.Spalte + " anlegen",
                        "ALTER TABLE \"" + s.Tabelle + "\" ADD COLUMN \"" + s.Spalte + "\" " + s.Typ);
                }
            }
        }

        /// <summary>
        /// Führt den Schritt aus — für <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>; die Migration der Schale geht denselben Weg über ihre eigenen
        /// Helfer, aus derselben <see cref="Anweisungen"/>. <b>Kein DML.</b>
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (0 bis 12).</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            int n = 0;
            foreach (KeyValuePair<string, string> a in new List<KeyValuePair<string, string>>(Anweisungen))
            {
                DataRepository.ExecuteNonQuery(a.Value);
                n++;
                bericht?.Add(a.Key);
            }
            if (n == 0) bericht?.Add(TAB_WP_STAMM + ", " + TAB_WP + ", " + TAB_BHKW_STAMM + " und " + TAB_BHKW +
                                     ": Teillastspalten vorhanden");
            return n;
        }
    }
}
