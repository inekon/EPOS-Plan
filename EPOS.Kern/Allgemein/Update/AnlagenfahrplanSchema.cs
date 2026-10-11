using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // AK2-1 - DER ANLAGENFAHRPLAN (Schritt 186; Anlagenkopplung 8.2 AK-S2, 8.3 AK-S3 Komfortteil).
    //
    // WOZU. Mit AK2 begrenzt der Fahrplan des Erzeugers (Sperrzeit, Zeitprogramm, Vorlaufangebot) die
    // Leistung, die ein gekoppeltes Gebaeude bekommt; der Komfort zeigt, was davon im Raum ankommt. Der
    // Schritt legt dafuer zwei Eingaben am Erzeuger und sechs Kennzahlen im Ergebnis an:
    //
    //   Tab_Energieanlagen         Zeitprogramm                     TEXT     NULL = immer verfuegbar
    //                              Vorlauf_Max                      REAL     NULL = Vorlauf der Anlage
    //   Tab_ErgebnisEnergiebedarf  Komfort_Unterschreitungsstunden  INTEGER  0..8760
    //                              Komfort_Kelvinstunden            REAL     >= 0
    //                              Komfort_Laengste_Strecke         INTEGER  0..8760
    //                              Fahrplan_Begrenzt_Stunden        INTEGER  0..8760
    //                              Komfort_Ueberschreitungsstunden  INTEGER  0..8760
    //                              Komfort_Kelvinstunden_Kuehlung   REAL     >= 0
    //
    // Das Zeitprogramm sind 168 Faktoren 0..1, Montag 00:00 bis Sonntag 23:00, im Format des
    // Sollwertprofils (AnlagenkopplungSchema.WochenprofilLesen, Trennzeichen ';'); gelesen von
    // Anlagenzeitprogramm. Ergebnisspalten NULL = "nicht erhoben" (kein gekoppeltes Gebaeude).
    // Sperrung, Sperrzeit_von/_bis und Nutzungszeit bleiben unberuehrt (F7).
    //
    // KEIN DML, KEIN SICHTNEUBAU, KEINE SAAT. Jede Bestandszeile steht danach auf NULL; der Referenzlauf
    // bleibt byte-gleich. Alles in EINEM Vorgang; der Schritt ist wiederholbar.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>AK2-1</b> — der Anlagenfahrplan: Zeitprogramm und Vorlaufangebot am Erzeuger, Komfort und
    /// Fahrplanbegrenzung im Ergebnis. EINE Quelle für Migration, Werkzeug, Testkopie und Nachweis
    /// (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class AnlagenfahrplanSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht (angemeldet als 186).
        /// </summary>
        public const int SCHRITT = ZonenKaeltespitzeSchema.SCHRITT + 1;

        /// <summary>Die Anlagentabelle des Projekts (ohne Stammfassung).</summary>
        public const string TAB_ANLAGEN = "Tab_Energieanlagen";

        /// <summary>Die Ergebnistabelle des Energiebedarfs.</summary>
        public const string TAB_ERGEBNIS = SchemaKatalog.TAB_ERGEBNISENERGIEBEDARF;

        /// <summary>168 Verfügbarkeitsfaktoren 0 … 1, Montag 00:00 bis Sonntag 23:00; NULL = immer verfügbar.</summary>
        public const string SPALTE_ZEITPROGRAMM = "Zeitprogramm";

        /// <summary>Das Vorlaufangebot des Erzeugers [°C]; NULL = <c>Vorlauf</c> der Anlage.</summary>
        public const string SPALTE_VORLAUF_MAX = "Vorlauf_Max";

        /// <summary>Stunden der Nutzungszeit unter dem Sollwert [h].</summary>
        public const string SPALTE_UNTERSCHREITUNGSSTUNDEN = "Komfort_Unterschreitungsstunden";

        /// <summary>Summe der Unterschreitungen [Kh].</summary>
        public const string SPALTE_KELVINSTUNDEN = "Komfort_Kelvinstunden";

        /// <summary>Längste zusammenhängende Unterschreitung [h].</summary>
        public const string SPALTE_LAENGSTE_STRECKE = "Komfort_Laengste_Strecke";

        /// <summary>Stunden, in denen der Fahrplan die Grenze war [h].</summary>
        public const string SPALTE_FAHRPLAN_BEGRENZT = "Fahrplan_Begrenzt_Stunden";

        /// <summary>Stunden der Nutzungszeit über dem Kühlsollwert [h] (F9).</summary>
        public const string SPALTE_UEBERSCHREITUNGSSTUNDEN = "Komfort_Ueberschreitungsstunden";

        /// <summary>Summe der Überschreitungen des Kühlsollwerts [Kh] (F9).</summary>
        public const string SPALTE_KELVINSTUNDEN_KUEHLUNG = "Komfort_Kelvinstunden_Kuehlung";

        /// <summary>Die zwei Spalten an <c>Tab_Energieanlagen</c>.</summary>
        public static readonly IReadOnlyList<string> SPALTEN_ANLAGE = new[] { SPALTE_ZEITPROGRAMM, SPALTE_VORLAUF_MAX };

        /// <summary>Die sechs Spalten an <c>Tab_ErgebnisEnergiebedarf</c>, in Anlagereihenfolge.</summary>
        public static readonly IReadOnlyList<string> SPALTEN_ERGEBNIS = new[]
        {
            SPALTE_UNTERSCHREITUNGSSTUNDEN, SPALTE_KELVINSTUNDEN, SPALTE_LAENGSTE_STRECKE,
            SPALTE_FAHRPLAN_BEGRENZT, SPALTE_UEBERSCHREITUNGSSTUNDEN, SPALTE_KELVINSTUNDEN_KUEHLUNG,
        };

        internal static string Stunden(string s) => "INTEGER CHECK (\"" + s + "\" IS NULL OR \"" + s + "\" BETWEEN 0 AND 8760)";

        internal static string NichtNegativ(string s) => "REAL CHECK (\"" + s + "\" IS NULL OR \"" + s + "\" >= 0)";

        /// <summary>Die Spalten des Schritts in Anlagereihenfolge: Tabelle, Spalte, Typ samt Klausel.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN = new[]
        {
            (TAB_ANLAGEN, SPALTE_ZEITPROGRAMM, "TEXT"),
            (TAB_ANLAGEN, SPALTE_VORLAUF_MAX, "REAL"),
            (TAB_ERGEBNIS, SPALTE_UNTERSCHREITUNGSSTUNDEN, Stunden(SPALTE_UNTERSCHREITUNGSSTUNDEN)),
            (TAB_ERGEBNIS, SPALTE_KELVINSTUNDEN, NichtNegativ(SPALTE_KELVINSTUNDEN)),
            (TAB_ERGEBNIS, SPALTE_LAENGSTE_STRECKE, Stunden(SPALTE_LAENGSTE_STRECKE)),
            (TAB_ERGEBNIS, SPALTE_FAHRPLAN_BEGRENZT, Stunden(SPALTE_FAHRPLAN_BEGRENZT)),
            (TAB_ERGEBNIS, SPALTE_UEBERSCHREITUNGSSTUNDEN, Stunden(SPALTE_UEBERSCHREITUNGSSTUNDEN)),
            (TAB_ERGEBNIS, SPALTE_KELVINSTUNDEN_KUEHLUNG, NichtNegativ(SPALTE_KELVINSTUNDEN_KUEHLUNG)),
        };

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { TAB_ANLAGEN, TAB_ERGEBNIS };

        /// <summary>Die Anweisung, die eine Spalte anlegt.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE \"" + s.Tabelle + "\" ADD COLUMN \"" + s.Spalte + "\" " + s.Typ;

        /// <summary>Stehen alle acht Spalten?</summary>
        public static bool Vollstaendig() => SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte));

        /// <summary>Stehen die zwei Spalten am Erzeuger? (Wächter der Anlagen-Anweisung, etwa für einen älteren Stand auf iOS.)</summary>
        public static bool AnlagenspaltenVorhanden()
            => SPALTEN_ANLAGE.All(s => DataRepository.SpalteVorhanden(TAB_ANLAGEN, s));

        /// <summary>Stehen die sechs Ergebnisspalten?</summary>
        public static bool ErgebnisspaltenVorhanden()
            => SPALTEN_ERGEBNIS.All(s => DataRepository.SpalteVorhanden(TAB_ERGEBNIS, s));

        /// <summary>
        /// Führt den Schritt aus (wiederholbar); Rückgabe = Zahl der angelegten Spalten. Fehlt eine der
        /// beiden Tabellen, wirft er benannt.
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
                bericht?.Add("steht bereits - Zeitprogramm/Vorlauf_Max an Tab_Energieanlagen, Komfortspalten an " +
                             "Tab_ErgebnisEnergiebedarf; nichts zu tun");
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
            bericht?.Add("KEIN DML an Bestandsdaten, der Referenzlauf bleibt byte-gleich");
            return angelegt;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
