using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KB-D - PFLEGBARE KAELTEFOLGE (Schritt 212; Entwurf Kaeltebereich 3.4, Entscheid E117 F1).
    //
    // WOZU. Die Reihenfolge der Kaelteerzeuger eines Projekts wird pflegbar. Die Stufen bleiben fest -
    // freie Kuehlung, Kaeltespeicher, Erzeuger (Entwurf 3.4: "Die freie Kuehlung bleibt vorn"; die Kaelte-
    // speicher ordnet ihre Entladeprioritaet). Gepflegt wird der Platz eines ERZEUGERS: Ein Rang stellt eine
    // Waermepumpe im Kuehlbetrieb vor oder hinter eine Kaeltemaschine und ordnet die Kaeltemaschinen
    // untereinander. Die Waermekaskade (Tool_1..4) bleibt, wie sie ist.
    //
    //   Tab_Energieanlagen   Kaelte_Rang   INTEGER   >= 1; NULL = Vorgabefolge (Waermepumpen nach Modulfolge,
    //                                                 dann Kaeltemaschinen nach Anlagen-ID)
    //
    // Der Rang steht an der ANLAGENZEILE, weil die Kaeltefolge Anlagen ordnet (Kaeltekaskade.Erzeuger je
    // Anlagenzeile) und eine Zeile mit ihrem Rang geloescht, kopiert und dupliziert wird - kein Kindsatz,
    // keine Waise. Eindeutig haelt ihn der Schreibweg (KaeltefolgeCtrl), nicht ein Index: Die Fachspalten-
    // Uebertragung (Assistent, Komponentenuebernahme) traegt die Zeile samt Rang, und ein gleicher Rang
    // ordnet sich stabil nach der Vorgabefolge.
    //
    // KEIN DML, KEIN SICHTNEUBAU, KEINE SAAT. Jede Bestandszeile steht danach auf NULL; der Referenzlauf
    // bleibt byte-gleich. Alles in EINEM Vorgang; der Schritt ist wiederholbar.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>KB-D</b> — die pflegbare Kältefolge: der Rang eines Kälteerzeugers an seiner Anlagenzeile. EINE Quelle für
    /// Migration, Werkzeug, Testkopie und Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class KaelteRangSchema
    {
        /// <summary><b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht (angemeldet als 212).</summary>
        public const int SCHRITT = KaelteKatalogfelderSchema.SCHRITT + 1;

        /// <summary>Die Anlagentabelle des Projekts (ohne Stammfassung).</summary>
        public const string TAB_ANLAGEN = "Tab_Energieanlagen";

        /// <summary>Der Rang in der Kältefolge, ≥ 1; NULL = Vorgabefolge.</summary>
        public const string SPALTE_RANG = "Kaelte_Rang";

        private static string Q(string s) => "\"" + s + "\"";

        /// <summary>Typ samt Klausel der Spalte.</summary>
        public static readonly string TYP =
            "INTEGER CHECK (" + Q(SPALTE_RANG) + " IS NULL OR " + Q(SPALTE_RANG) + " >= 1)";

        /// <summary>Die Spalten des Schritts: Tabelle, Spalte, Typ samt Klausel.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN = new[]
        {
            (TAB_ANLAGEN, SPALTE_RANG, TYP)
        };

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { TAB_ANLAGEN };

        /// <summary>Die Anweisung, die eine Spalte anlegt.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE " + Q(s.Tabelle) + " ADD COLUMN " + Q(s.Spalte) + " " + s.Typ;

        /// <summary>Steht die Spalte? (Wächter der Leser, etwa für einen älteren Stand auf iOS.)</summary>
        public static bool Vollstaendig() => SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte));

        /// <summary>Wie viele Anlagenzeilen tragen einen Rang? (Nachweis: nach dem Schritt 0.)</summary>
        public static int ZeilenMitRang()
        {
            if (!Vollstaendig()) return 0;
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + TAB_ANLAGEN + " WHERE " + SPALTE_RANG + " IS NOT NULL");
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Führt den Schritt aus (wiederholbar); Rückgabe = Zahl der angelegten Spalten. Fehlt die Anlagentabelle,
        /// wirft er benannt.
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
                bericht?.Add("steht bereits - Kaelte_Rang an Tab_Energieanlagen; nichts zu tun");
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
                        zeilen.Add(s.Tabelle + "." + s.Spalte + " angelegt (leer = Vorgabefolge)");
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
