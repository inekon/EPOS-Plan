using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // EINHEIT DES BEREITSCHAFTSVERLUSTS DES HEIZKESSELS - Anwenderentscheid vom 02.10.2026:
    // Der Bereitschaftsverlust wird im Kesseldialog wahlweise in kW oder in Prozent der
    // Nennleistung erfasst.
    //
    // WOZU. Tab_Heizkessel(_STAMM).Betriebsbereitschaftverlust ist eine Leistung in kW (der
    // Import liest die Bereitschaftsleistung aus VDI 3805). Viele Datenblätter nennen den
    // Bereitschaftsverlust aber als Anteil der Nennleistung. Die neue Spalte hält fest, in
    // welcher Einheit der gespeicherte Wert steht; gerechnet wird weiter in kW - die eine
    // Umrechnung steht in KesselBereitschaft.LeistungKw.
    //
    // EINE SPALTE, an Tab_Heizkessel_STAMM UND Tab_Heizkessel gleich (die Projektkopie trägt
    // den Katalogsatz Spalte für Spalte):
    //   Bereitschaft_Einheit  TEXT NOT NULL DEFAULT 'kW' CHECK (IN ('kW','%'))
    // Beide Tabellen sind STRICT; TEXT ist dort zulässig.
    //
    // KEIN DML. Jede Bestandszeile bekommt die Vorgabe 'kW' - genau die Einheit, in der ihr
    // Wert heute gerechnet wird. Kein Wert wird umgedeutet, der Referenzlauf bleibt byte-gleich.
    //
    // DER NAME STEHT NUR ALS ARGUMENT (Muster KesselKennlinieSchema): Die Anweisung entsteht aus
    // Tabelle, Spalte und Typ, Werkzeuge/SqlDialektPruefer sieht keinen fertigen Text, der gegen
    // die bereits migrierte Testdatenbank „duplicate column" wäre.
    //
    // VIER LESER: der Schemaschritt in WindowsFormsApplication1/Allgemein/Update/
    // SchemaMigration.cs, das Werkzeug Werkzeuge/Testdatenbankschema, die Testvorrichtung samt
    // Nachweis in EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// Die Einheitenspalte des Bereitschaftsverlusts am Heizkessel — EINE Quelle für Migration,
    /// Werkzeug, Testkopie, Controller und Nachweis (ADR-001 Option C). Anlass und Bauform
    /// stehen im Kopf der Datei; die Rechenregel steht bei <see cref="KesselBereitschaft"/>.
    /// </summary>
    public static class KesselBereitschaftEinheitSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht: der nächste
        /// Schritt hinter den Änderungsstempeln (<see cref="KostenStempelSchema"/>).
        /// </summary>
        public const int SCHRITT = KostenStempelSchema.SCHRITT + 1;

        /// <summary>Der Katalog der Auslieferung.</summary>
        public const string TAB_STAMM = SchemaKatalog.TAB_HEIZKESSEL_STAMM;

        /// <summary>Die Projektkopien.</summary>
        public const string TAB_PROJEKT = SchemaKatalog.TAB_HEIZKESSEL;

        /// <summary>
        /// <c>Bereitschaft_Einheit</c>: die Einheit von <c>Betriebsbereitschaftverlust</c> —
        /// <see cref="DbWerte.KESSEL_BEREITSCHAFT_EINHEIT_KW"/> oder
        /// <see cref="DbWerte.KESSEL_BEREITSCHAFT_EINHEIT_PROZENT"/>.
        /// </summary>
        public const string SPALTE = "Bereitschaft_Einheit";

        /// <summary>Die Typdefinition: Pflichtfeld mit Vorgabe kW und Prüfklausel.</summary>
        public const string TYP =
            "TEXT NOT NULL DEFAULT '" + DbWerte.KESSEL_BEREITSCHAFT_EINHEIT_KW + "' CHECK (\"" + SPALTE +
            "\" IN ('" + DbWerte.KESSEL_BEREITSCHAFT_EINHEIT_KW + "','" +
            DbWerte.KESSEL_BEREITSCHAFT_EINHEIT_PROZENT + "'))";

        /// <summary>Die beiden Tabellen, in dieser Reihenfolge: Katalog, dann Projektkopien.</summary>
        public static readonly string[] TABELLEN = { TAB_STAMM, TAB_PROJEKT };

        /// <summary>Steht die Spalte an beiden Tabellen? Dann ist der Schritt gelaufen.</summary>
        public static bool Vollstaendig()
        {
            foreach (string tabelle in TABELLEN)
                if (!DataRepository.SpalteVorhanden(tabelle, SPALTE)) return false;
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
                foreach (string tabelle in TABELLEN)
                {
                    if (DataRepository.SpalteVorhanden(tabelle, SPALTE)) continue;
                    yield return new KeyValuePair<string, string>(
                        tabelle + "." + SPALTE + " anlegen",
                        "ALTER TABLE \"" + tabelle + "\" ADD COLUMN \"" + SPALTE + "\" " + TYP);
                }
            }
        }

        /// <summary>
        /// Führt den Schritt aus — für <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>; die Migration der Schale geht denselben Weg über ihre eigenen
        /// Helfer, aus derselben <see cref="Anweisungen"/>. <b>Kein DML.</b>
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (0 bis 2).</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            int n = 0;
            foreach (KeyValuePair<string, string> a in new List<KeyValuePair<string, string>>(Anweisungen))
            {
                DataRepository.ExecuteNonQuery(a.Value);
                n++;
                bericht?.Add(a.Key);
            }
            if (n == 0) bericht?.Add(TAB_STAMM + " und " + TAB_PROJEKT + ": Einheitenspalte vorhanden");
            return n;
        }
    }
}
