using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // HERKUNFT DES NORDWINKELS (Schritt 199; Abstimmungspapier G5, Abschnitt 7, N6).
    //
    // WOZU. Die Importquelle traegt den Nordwinkel, um den die Bauteilazimute gedreht sind
    // (Tab_Importquelle.Nordwinkel_Grad, RaumgrundrissSchema). Ob er aus der Datei stammt, vom Anwender
    // eingegeben oder als Annahme "Planoberseite = Nord" gesetzt ist, entscheidet ueber das Neulesen
    // (nur eine Eingabe ersetzt den Dateiwert) und ueber die Anzeige - deshalb wird sie gespeichert:
    //
    //   Tab_Importquelle  Nordwinkel_Herkunft  TEXT  ANNAHME | DATEI | EINGABE
    //                                               NULL = nicht bestimmt (Lesen: Wert -> DATEI, NULL -> ANNAHME)
    //
    // Die Wertliste steht EINMAL: NordwinkelherkunftWerte (Import/Gebaeude/Nordrichtung.cs), je Wert
    // der Aufzaehlung Nordwinkelherkunft einer.
    //
    // NACHFUELLEN. Nach dem ADD COLUMN bekommt jede Bestandszeile ohne Herkunft die, die der Leseweg
    // ohne Spalte annimmt: Nordwinkel_Grad vorhanden -> DATEI, NULL -> ANNAHME. Ergebnisneutral: Der
    // Rechenweg liest die Spalte nicht, kein Referenzprojekt hat eine Importquelle. Wiederholbar.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl); geschrieben und gelesen von GebaeudeImportCtrl.
    // ====================================================================================

    /// <summary>
    /// <b>Herkunft des Nordwinkels</b> — die Spalte <c>Tab_Importquelle.Nordwinkel_Herkunft</c>: EINE Quelle für Migration,
    /// Werkzeug, Testkopie und Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class NordrichtungSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht (angemeldet als 199): der Schritt
        /// hinter <see cref="Ak3Schema.SCHRITT"/>.
        /// </summary>
        public const int SCHRITT = Ak3Schema.SCHRITT + 1;

        /// <summary>Die Quelltabelle des Imports.</summary>
        public const string TAB_QUELLE = RaumgrundrissSchema.TAB_QUELLE;

        /// <summary>Der Nordwinkel der Quelle, nach dem die Bestandszeilen nachgefüllt werden.</summary>
        public const string SPALTE_NORDWINKEL = RaumgrundrissSchema.SPALTE_NORDWINKEL;

        /// <summary><c>Nordwinkel_Herkunft</c> (<see cref="WERTE"/>).</summary>
        public const string SPALTE = "Nordwinkel_Herkunft";

        /// <summary>Die zulässigen Werte — die von <see cref="NordwinkelherkunftWerte"/>, in der Reihenfolge der Aufzählung.</summary>
        public static readonly IReadOnlyList<string> WERTE = Enum.GetValues(typeof(Nordwinkelherkunft))
            .Cast<Nordwinkelherkunft>().Select(NordwinkelherkunftWerte.Wert).ToList();

        private static string Q(string s) => "\"" + s + "\"";

        /// <summary>Typ samt Prüfklausel der Spalte.</summary>
        public static string Typ => "TEXT CHECK (" + Q(SPALTE) + " IS NULL OR " + Q(SPALTE) + " IN (" +
                                    string.Join(",", WERTE.Select(w => "'" + w + "'")) + "))";

        /// <summary>Die Anweisung, die die Spalte anlegt.</summary>
        public static string Anlegen() => "ALTER TABLE " + Q(TAB_QUELLE) + " ADD COLUMN " + Q(SPALTE) + " " + Typ;

        /// <summary>
        /// Die Anweisung, die Bestandszeilen ohne Herkunft nachfüllt: Nordwinkel vorhanden → <c>DATEI</c>, NULL → <c>ANNAHME</c>;
        /// eine gesetzte Herkunft bleibt.
        /// </summary>
        public static string Nachfuellen() =>
            "UPDATE " + Q(TAB_QUELLE) + " SET " + Q(SPALTE) + " = CASE WHEN " + Q(SPALTE_NORDWINKEL) + " IS NULL THEN '" +
            NordwinkelherkunftWerte.ANNAHME + "' ELSE '" + NordwinkelherkunftWerte.DATEI + "' END WHERE " + Q(SPALTE) + " IS NULL";

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { TAB_QUELLE };

        /// <summary>Steht die Spalte?</summary>
        public static bool Vollstaendig() => DataRepository.SpalteVorhanden(TAB_QUELLE, SPALTE);

        /// <summary>
        /// Führt den Schritt aus (wiederholbar): legt die Spalte an, falls sie fehlt, und füllt jede Zeile ohne Herkunft nach.
        /// Rückgabe = Zahl der angelegten Spalten (0 oder 1). Ohne <c>Tab_Importquelle</c> oder ohne deren Nordwinkel wirft er
        /// benannt.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            if (!RaumgrundrissSchema.NordwinkelVorhanden())
                throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Spalte " + TAB_QUELLE + "." + SPALTE_NORDWINKEL +
                                                    " fehlt; Schritt " + RaumgrundrissSchema.SCHRITT.ToString(CultureInfo.InvariantCulture) +
                                                    " ist nicht gelaufen.");
            bool steht = Vollstaendig();
            int gefuellt;
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    if (!steht) v.Ausfuehren(Anlegen());
                    gefuellt = v.Ausfuehren(Nachfuellen());
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            GebaeudeZonenanschluss.ProbeVerwerfen();
            bericht?.Add(steht
                ? "steht bereits - " + TAB_QUELLE + "." + SPALTE
                : TAB_QUELLE + "." + SPALTE + " angelegt");
            bericht?.Add(gefuellt.ToString(CultureInfo.InvariantCulture) + " Quelle(n) nachgefuellt (Nordwinkel vorhanden -> " +
                         NordwinkelherkunftWerte.DATEI + ", sonst " + NordwinkelherkunftWerte.ANNAHME +
                         "); der Rechenweg liest die Spalte nicht, der Referenzlauf bleibt gleich");
            return steht ? 0 : 1;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
