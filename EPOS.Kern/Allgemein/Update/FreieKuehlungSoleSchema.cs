using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KU3-6a - FREIE KUEHLUNG UEBER DIE WAERMEQUELLE (Schritt 187; Konzept Kuehlung Stufe KU3, F1 und F4).
    //
    // WOZU. Eine Sole-Wasser- oder Wasser-Wasser-Waermepumpe im Kuehlbetrieb deckt je Stunde Kaelte vor dem
    // Verdichter direkt aus ihrer Waermequelle, solange Quellentemperatur plus Graedigkeit des Waermetauschers
    // unter dem Kaltwasser-Vorlauf liegen. Der Schritt legt dafuer drei Eingaben am Erzeuger und je zwei
    // Zaehler an den beiden Ergebnistabellen der Waermepumpe an:
    //
    //   Tab_Energieanlagen            Kuehl_Frei                 INTEGER  0/1, Vorgabe 0 = aus
    //                                 Kuehl_Frei_Graedigkeit_K   REAL     0..20; NULL = Festwert 3,0 K
    //                                 Kuehl_Frei_Leistung_kW     REAL     > 0;   NULL = Kaelteleistung der Kennlinie
    //   Tab_ErgebnisWaermepumpe       FreieKuehlung_MWh          REAL     >= 0
    //   Tab_ErgebnisWaermepumpeModul  FreieKuehlung_Stunden      INTEGER  0..8760
    //   (beide Zaehler an beiden Tabellen, wie Kaelteproduktion)
    //
    // Ergebnisspalten NULL = "nicht erhoben" (keine Kaelteerzeugung gerechnet). Die drei Eingaben sind
    // Betriebsvorgaben, kein Kostenstempel: Der Stempeltrigger der Anlagenzeile (KostenStempelSchema) bleibt.
    //
    // KEIN DML, KEIN SICHTNEUBAU, KEINE SAAT. Jede Bestandszeile steht danach auf Kuehl_Frei = 0 und NULL;
    // der Referenzlauf bleibt byte-gleich. Alles in EINEM Vorgang; der Schritt ist wiederholbar.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>KU3-6a</b> — freie Kühlung über die Wärmequelle: Schalter, Grädigkeit und Leistungsgrenze am Erzeuger,
    /// Kälte und Stunden der freien Kühlung im Ergebnis der Wärmepumpe. EINE Quelle für Migration, Werkzeug,
    /// Testkopie und Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class FreieKuehlungSoleSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht (angemeldet als 187).
        /// </summary>
        public const int SCHRITT = AnlagenfahrplanSchema.SCHRITT + 1;

        /// <summary>Die Anlagentabelle des Projekts (ohne Stammfassung).</summary>
        public const string TAB_ANLAGEN = "Tab_Energieanlagen";

        /// <summary>Die Ergebnistabelle aller Wärmepumpen eines Laufs.</summary>
        public const string TAB_ERGEBNIS_WP = SchemaKatalog.TAB_ERGEBNISWAERMEPUMPE;

        /// <summary>Die Ergebnistabelle je Wärmepumpe (Modulzeile).</summary>
        public const string TAB_ERGEBNIS_WP_MODUL = KuehlungSchema.TAB_ERGEBNIS_WP_MODUL;

        /// <summary>Der Schalter der freien Kühlung über die Wärmequelle: 1 = an, 0 = aus (Vorgabe).</summary>
        public const string SPALTE_KUEHL_FREI = "Kuehl_Frei";

        /// <summary>Die Grädigkeit des Wärmetauschers [K], 0 … 20; NULL = Festwert.</summary>
        public const string SPALTE_GRAEDIGKEIT = "Kuehl_Frei_Graedigkeit_K";

        /// <summary>Die Leistungsgrenze der freien Kühlung [kW], &gt; 0; NULL = Kälteleistung der Kennlinie.</summary>
        public const string SPALTE_LEISTUNG = "Kuehl_Frei_Leistung_kW";

        /// <summary>Die frei gedeckte Kälte [MWh/a], ≥ 0; NULL = nicht erhoben.</summary>
        public const string SPALTE_FREIE_KUEHLUNG_MWH = "FreieKuehlung_MWh";

        /// <summary>Die Stunden mit freier Kühlung [h], 0 … 8760; NULL = nicht erhoben.</summary>
        public const string SPALTE_FREIE_KUEHLUNG_STUNDEN = "FreieKuehlung_Stunden";

        /// <summary>Die drei Spalten an <c>Tab_Energieanlagen</c>, in Anlagereihenfolge.</summary>
        public static readonly IReadOnlyList<string> SPALTEN_ANLAGE = new[] { SPALTE_KUEHL_FREI, SPALTE_GRAEDIGKEIT, SPALTE_LEISTUNG };

        /// <summary>Die zwei Zähler, je an beiden Ergebnistabellen der Wärmepumpe.</summary>
        public static readonly IReadOnlyList<string> SPALTEN_ERGEBNIS = new[] { SPALTE_FREIE_KUEHLUNG_MWH, SPALTE_FREIE_KUEHLUNG_STUNDEN };

        /// <summary>Die beiden Ergebnistabellen der Wärmepumpe.</summary>
        public static readonly IReadOnlyList<string> TABELLEN_ERGEBNIS = new[] { TAB_ERGEBNIS_WP, TAB_ERGEBNIS_WP_MODUL };

        private static string Q(string s) => "\"" + s + "\"";

        private static string Stunden(string s) => "INTEGER CHECK (" + Q(s) + " IS NULL OR " + Q(s) + " BETWEEN 0 AND 8760)";

        private static string NichtNegativ(string s) => "REAL CHECK (" + Q(s) + " IS NULL OR " + Q(s) + " >= 0)";

        /// <summary>Die Spalten des Schritts in Anlagereihenfolge: Tabelle, Spalte, Typ samt Klausel.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN = new[]
        {
            (TAB_ANLAGEN, SPALTE_KUEHL_FREI,
                "INTEGER NOT NULL DEFAULT 0 CHECK (" + Q(SPALTE_KUEHL_FREI) + " IN (0,1))"),
            (TAB_ANLAGEN, SPALTE_GRAEDIGKEIT,
                "REAL CHECK (" + Q(SPALTE_GRAEDIGKEIT) + " IS NULL OR " + Q(SPALTE_GRAEDIGKEIT) + " BETWEEN 0 AND 20)"),
            (TAB_ANLAGEN, SPALTE_LEISTUNG,
                "REAL CHECK (" + Q(SPALTE_LEISTUNG) + " IS NULL OR " + Q(SPALTE_LEISTUNG) + " > 0)"),
            (TAB_ERGEBNIS_WP, SPALTE_FREIE_KUEHLUNG_MWH, NichtNegativ(SPALTE_FREIE_KUEHLUNG_MWH)),
            (TAB_ERGEBNIS_WP, SPALTE_FREIE_KUEHLUNG_STUNDEN, Stunden(SPALTE_FREIE_KUEHLUNG_STUNDEN)),
            (TAB_ERGEBNIS_WP_MODUL, SPALTE_FREIE_KUEHLUNG_MWH, NichtNegativ(SPALTE_FREIE_KUEHLUNG_MWH)),
            (TAB_ERGEBNIS_WP_MODUL, SPALTE_FREIE_KUEHLUNG_STUNDEN, Stunden(SPALTE_FREIE_KUEHLUNG_STUNDEN)),
        };

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { TAB_ANLAGEN, TAB_ERGEBNIS_WP, TAB_ERGEBNIS_WP_MODUL };

        /// <summary>Die Anweisung, die eine Spalte anlegt.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE " + Q(s.Tabelle) + " ADD COLUMN " + Q(s.Spalte) + " " + s.Typ;

        /// <summary>Stehen alle sieben Spalten?</summary>
        public static bool Vollstaendig() => SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte));

        /// <summary>Stehen die drei Spalten am Erzeuger? (Wächter der Anlagen-Anweisung, etwa für einen älteren Stand auf iOS.)</summary>
        public static bool AnlagenspaltenVorhanden()
            => SPALTEN_ANLAGE.All(s => DataRepository.SpalteVorhanden(TAB_ANLAGEN, s));

        /// <summary>Stehen die zwei Zähler an beiden Ergebnistabellen der Wärmepumpe?</summary>
        public static bool ErgebnisspaltenVorhanden()
            => TABELLEN_ERGEBNIS.All(t => SPALTEN_ERGEBNIS.All(s => DataRepository.SpalteVorhanden(t, s)));

        /// <summary>
        /// Führt den Schritt aus (wiederholbar); Rückgabe = Zahl der angelegten Spalten. Fehlt eine der
        /// drei Tabellen, wirft er benannt.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            var offen = SPALTEN.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)).ToList();
            if (offen.Count == 0)
            {
                bericht?.Add("steht bereits - Kuehl_Frei* an Tab_Energieanlagen, FreieKuehlung_* an den " +
                             "Ergebnistabellen der Waermepumpe; nichts zu tun");
                return 0;
            }

            int angelegt = 0;
            var zeilen = new List<string>();
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    foreach ((string Tabelle, string Spalte, string Typ) s in offen)
                    {
                        v.Ausfuehren(Anlegen(s));
                        zeilen.Add(s.Tabelle + "." + s.Spalte + " angelegt (" +
                                   (s.Spalte == SPALTE_KUEHL_FREI ? "Vorgabe 0" : "leer") + ")");
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
            bericht?.Add("KEIN DML an Bestandsdaten, der Referenzlauf bleibt byte-gleich");
            return angelegt;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
