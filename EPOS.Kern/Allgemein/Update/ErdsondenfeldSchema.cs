using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // ERDSONDENFELD JE ANLAGE PFLEGBAR (Schritt 195; Konzept Simulationsablauf 23.3).
    //
    // WOZU. Das Sondenfeld rechnet mit Geometrie und Bohrlochkennwerten (Sondenfeldgeometrie). Die
    // Anlagenzeile nimmt sie als Kennzahlen am Feld auf - keine neue Beziehung:
    //
    //   Tab_Energieanlagen  WQ_Sondenabstand        REAL     > 0   [m]      NULL = Vorgabe 6,0
    //                       WQ_Bohrlochdurchmesser  REAL     > 0   [mm]     NULL = Vorgabe 150
    //                       WQ_Bohrlochwiderstand   REAL     > 0   [m*K/W]  NULL = Vorgabe 0,10
    //                       WQ_Kopfueberdeckung     REAL     >= 0  [m]      NULL = Vorgabe 2,0
    //                       WQ_Betrachtungsjahr     INTEGER  >= 1           NULL = Vorgabe 10
    //                       WQ_Sondenanordnung      TEXT     Quadratisch | Reihe   NULL = Quadratisch
    //
    // NULL heisst Normvorgabe (Sondenfeldgeometrie.Norm, VDI 4640 Blatt 2 Tabelle B2). Die Spalten sind
    // FACHSPALTEN im Sinn der Rettung im Assistenten (WizardCtrl, FS1): ErdsondenfeldCtrl schreibt sie
    // zielgenau, die Rettung haelt sie ueber Loeschen und Neuanlegen von selbst.
    //
    // KEIN DML, KEIN NEUBAU. Reines ADD COLUMN, nullbar, mit Pruefklausel; jede Bestandszeile steht
    // danach auf NULL und rechnet unveraendert mit der Norm. Alles in EINEM Vorgang; wiederholbar.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl); dazu WaermequelleClass.SondenfeldgeometrieDerAnlage.
    // ====================================================================================

    /// <summary>
    /// <b>Erdsondenfeld je Anlage</b> — Geometrie und Bohrlochkennwerte des Sondenfeldes an
    /// <c>Tab_Energieanlagen</c>: EINE Quelle für Migration, Werkzeug, Testkopie und Nachweis (ADR-001 Option C).
    /// Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class ErdsondenfeldSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht (angemeldet als 195).
        /// </summary>
        public const int SCHRITT = AufheizAufschlagErgebnisSchema.SCHRITT + 1;

        /// <summary>Die Anlagentabelle.</summary>
        public const string TAB_ANLAGEN = "Tab_Energieanlagen";

        /// <summary><c>WQ_Sondenabstand</c> [m], &gt; 0; NULL = Vorgabe.</summary>
        public const string SPALTE_ABSTAND = "WQ_Sondenabstand";

        /// <summary><c>WQ_Bohrlochdurchmesser</c> [mm], &gt; 0; NULL = Vorgabe.</summary>
        public const string SPALTE_BOHRLOCHDURCHMESSER = "WQ_Bohrlochdurchmesser";

        /// <summary><c>WQ_Bohrlochwiderstand</c> [m·K/W], &gt; 0; NULL = Vorgabe.</summary>
        public const string SPALTE_BOHRLOCHWIDERSTAND = "WQ_Bohrlochwiderstand";

        /// <summary><c>WQ_Kopfueberdeckung</c> [m], ≥ 0; NULL = Vorgabe.</summary>
        public const string SPALTE_KOPFUEBERDECKUNG = "WQ_Kopfueberdeckung";

        /// <summary><c>WQ_Betrachtungsjahr</c>, ≥ 1; NULL = Vorgabe.</summary>
        public const string SPALTE_BETRACHTUNGSJAHR = "WQ_Betrachtungsjahr";

        /// <summary><c>WQ_Sondenanordnung</c> (<see cref="ANORDNUNGEN"/>); NULL = Quadratisch.</summary>
        public const string SPALTE_ANORDNUNG = "WQ_Sondenanordnung";

        /// <summary>Die Schreibweisen der Anordnung in der Datenbank — die Namen von <see cref="Sondenanordnung"/>.</summary>
        public static readonly IReadOnlyList<string> ANORDNUNGEN = Enum.GetNames(typeof(Sondenanordnung));

        private static string Q(string s) => "\"" + s + "\"";

        private static string Positiv(string s) => "REAL CHECK (" + Q(s) + " IS NULL OR " + Q(s) + " > 0)";

        /// <summary>Die Spalten des Schritts in Anlagereihenfolge: Tabelle, Spalte, Typ samt Klausel.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN = new[]
        {
            (TAB_ANLAGEN, SPALTE_ABSTAND, Positiv(SPALTE_ABSTAND)),
            (TAB_ANLAGEN, SPALTE_BOHRLOCHDURCHMESSER, Positiv(SPALTE_BOHRLOCHDURCHMESSER)),
            (TAB_ANLAGEN, SPALTE_BOHRLOCHWIDERSTAND, Positiv(SPALTE_BOHRLOCHWIDERSTAND)),
            (TAB_ANLAGEN, SPALTE_KOPFUEBERDECKUNG,
                "REAL CHECK (" + Q(SPALTE_KOPFUEBERDECKUNG) + " IS NULL OR " + Q(SPALTE_KOPFUEBERDECKUNG) + " >= 0)"),
            (TAB_ANLAGEN, SPALTE_BETRACHTUNGSJAHR,
                "INTEGER CHECK (" + Q(SPALTE_BETRACHTUNGSJAHR) + " IS NULL OR " + Q(SPALTE_BETRACHTUNGSJAHR) + " >= 1)"),
            (TAB_ANLAGEN, SPALTE_ANORDNUNG,
                "TEXT CHECK (" + Q(SPALTE_ANORDNUNG) + " IS NULL OR " + Q(SPALTE_ANORDNUNG) + " IN (" +
                string.Join(",", ANORDNUNGEN.Select(a => "'" + a + "'")) + "))"),
        };

        /// <summary>Die Spaltennamen in Anlagereihenfolge.</summary>
        public static IReadOnlyList<string> Spaltennamen() => SPALTEN.Select(s => s.Spalte).ToList();

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { TAB_ANLAGEN };

        /// <summary>Die Anweisung, die eine Spalte anlegt.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE " + Q(s.Tabelle) + " ADD COLUMN " + Q(s.Spalte) + " " + s.Typ;

        /// <summary>Stehen alle sechs Spalten?</summary>
        public static bool Vollstaendig() => SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte));

        /// <summary>
        /// Führt den Schritt aus (wiederholbar); Rückgabe = Zahl der angelegten Spalten. Ohne
        /// <c>Tab_Energieanlagen</c> wirft er benannt.
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
                bericht?.Add("steht bereits - Sondenfeldspalten an Tab_Energieanlagen; nichts zu tun");
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
            bericht?.Add("KEIN DML an Bestandsdaten - leere Spalten heissen Normvorgabe, der Referenzlauf bleibt gleich");
            return angelegt;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
