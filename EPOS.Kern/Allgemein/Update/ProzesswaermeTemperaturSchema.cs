using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // TEMPERATURPAAR JE PROZESS - Welle M3a, Entscheidungsvorlage Modellgrenzen PW1 Stufe 1
    // (Wellenplan vom Anwender freigegeben).
    //
    // WOZU. Ein Prozesswärmesatz war eine reine Wärmemenge; welches Temperaturniveau er
    // verlangt, kam im Rechenweg nicht vor. Die beiden Spalten halten fest, mit welchem
    // Vorlauf und Rücklauf ein Prozess versorgt werden muss. Der Lauf bildet daraus je Stunde
    // die HÖCHSTE geforderte Vorlauftemperatur des Prozesskanals und den mengengewichteten
    // Rücklauf (Prozesstemperatur); die Regeln stehen dort.
    //
    // ZWEI SPALTEN, an Tab_Prozesswaerme_STAMM UND Tab_Prozesswaerme gleich (die Projektkopie
    // trägt den Katalogsatz Spalte für Spalte):
    //   Vorlauf    REAL, nullbar, 0 … 250 °C
    //   Ruecklauf  REAL, nullbar, 0 … 250 °C, beide leer oder beide gesetzt, Vorlauf ≥ Rücklauf
    // Die paarweisen Prüfklauseln stehen an der ZWEITEN Spalte - ALTER TABLE legt die Spalten
    // nacheinander an, und eine Klausel darf nur Spalten nennen, die schon stehen.
    //
    // KEIN DML AN BESTANDSZEILEN. Jede Bestandszeile bleibt ohne Temperaturpaar - und ohne Paar
    // rechnet der Lauf Zeichen für Zeichen wie zuvor. Der Referenzlauf bleibt byte-gleich.
    //
    // DIE SAAT (PW5) gehört zum selben Schritt: acht ausgelieferte Betriebsweisen in
    // Tab_Prozesswaerme_STAMM und Tab_Prozesstyp_STAMM, ReadOnly = 1, mit Temperaturpaar als
    // Vorbelegung (ProzesstypSaat). Wiederholbar, nie überschreibend; kein Referenzprojekt ordnet
    // einen Satz zu.
    //
    // DER NAME STEHT NUR ALS ARGUMENT (Muster KesselBereitschaftEinheitSchema): Die Anweisung
    // entsteht aus Tabelle, Spalte und Typ; Werkzeuge/SqlDialektPruefer sieht keinen fertigen
    // Text, der gegen die bereits migrierte Testdatenbank „duplicate column" wäre.
    //
    // VIER LESER: der Schemaschritt in WindowsFormsApplication1/Allgemein/Update/
    // SchemaMigration.cs, das Werkzeug Werkzeuge/Testdatenbankschema, die Testvorrichtung samt
    // Nachweis in EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// Das Temperaturpaar je Prozesswärmesatz (Entscheidungsvorlage Modellgrenzen, PW1 Stufe 1) —
    /// EINE Quelle für Migration, Werkzeug, Testkopie und Nachweis (ADR-001 Option C). Anlass und
    /// Bauform stehen im Kopf der Datei; die Rechenregel steht bei <c>Prozesstemperatur</c>.
    /// </summary>
    public static class ProzesswaermeTemperaturSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht: der nächste
        /// Schritt hinter der Bodenalbedo je Anlage (<see cref="AlbedoSchema"/>).
        /// Wird der Schritt beim Zusammenführen umnummeriert, ändert sich nur diese Zeile.
        /// </summary>
        public const int SCHRITT = AlbedoSchema.SCHRITT + 1;

        /// <summary>Der Katalog der Auslieferung.</summary>
        public const string TAB_STAMM = "Tab_Prozesswaerme_STAMM";

        /// <summary>Die Projektkopien.</summary>
        public const string TAB_PROJEKT = "Tab_Prozesswaerme";

        /// <summary><c>Vorlauf</c>: die Vorlauftemperatur, die der Prozess verlangt [°C].</summary>
        public const string SPALTE_VORLAUF = "Vorlauf";

        /// <summary><c>Ruecklauf</c>: die Rücklauftemperatur, mit der der Prozess zurückgibt [°C].</summary>
        public const string SPALTE_RUECKLAUF = "Ruecklauf";

        /// <summary>Die untere Grenze beider Temperaturen [°C].</summary>
        public const double MIN_GRAD = 0;

        /// <summary>Die obere Grenze beider Temperaturen [°C].</summary>
        public const double MAX_GRAD = 250;

        /// <summary>Die Typdefinition des Vorlaufs: nullbar, mit Wertebereich.</summary>
        public const string TYP_VORLAUF =
            "REAL CHECK (\"" + SPALTE_VORLAUF + "\" IS NULL OR (\"" + SPALTE_VORLAUF + "\" >= 0 AND \"" +
            SPALTE_VORLAUF + "\" <= 250))";

        /// <summary>
        /// Die Typdefinition des Rücklaufs: nullbar, mit Wertebereich, dazu die beiden
        /// Paarklauseln (beide leer oder beide gesetzt; Vorlauf nicht unter dem Rücklauf).
        /// </summary>
        public const string TYP_RUECKLAUF =
            "REAL CHECK (\"" + SPALTE_RUECKLAUF + "\" IS NULL OR (\"" + SPALTE_RUECKLAUF + "\" >= 0 AND \"" +
            SPALTE_RUECKLAUF + "\" <= 250)) " +
            "CHECK ((\"" + SPALTE_VORLAUF + "\" IS NULL) = (\"" + SPALTE_RUECKLAUF + "\" IS NULL)) " +
            "CHECK (\"" + SPALTE_VORLAUF + "\" IS NULL OR \"" + SPALTE_VORLAUF + "\" >= \"" + SPALTE_RUECKLAUF + "\")";

        /// <summary>Die beiden Tabellen, in dieser Reihenfolge: Katalog, dann Projektkopien.</summary>
        public static readonly string[] TABELLEN = { TAB_STAMM, TAB_PROJEKT };

        /// <summary>Die beiden Spalten samt Typ, in dieser Reihenfolge (die zweite nennt die erste).</summary>
        public static readonly KeyValuePair<string, string>[] SPALTEN =
        {
            new KeyValuePair<string, string>(SPALTE_VORLAUF, TYP_VORLAUF),
            new KeyValuePair<string, string>(SPALTE_RUECKLAUF, TYP_RUECKLAUF),
        };

        /// <summary>Stehen beide Spalten an beiden Tabellen? Dann ist der DDL-Teil des Schritts gelaufen.</summary>
        public static bool SpaltenVollstaendig()
        {
            foreach (string tabelle in TABELLEN)
                foreach (KeyValuePair<string, string> s in SPALTEN)
                    if (!DataRepository.SpalteVorhanden(tabelle, s.Key)) return false;
            return true;
        }

        /// <summary>
        /// Ist der Schritt gelaufen? Die Spalten stehen, und die acht ausgelieferten Betriebsweisen
        /// (<see cref="ProzesstypSaat"/>, PW5) stehen unter ihren Namen in beiden Katalogen.
        /// </summary>
        public static bool Vollstaendig() => SpaltenVollstaendig() && ProzesstypSaat.Vollstaendig();

        /// <summary>
        /// Die DDL-Anweisungen des Schritts — Beschreibung und SQL, je fehlende Spalte eine; leer,
        /// wenn alles steht (<b>wiederholbar</b>).
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
        /// <c>EPOS.Kern.Tests</c>; die Migration der Schale geht denselben Weg über ihre eigenen
        /// Helfer, aus derselben <see cref="Anweisungen"/>.
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (0 bis 4); die Saat meldet in <paramref name="bericht"/>.</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            int n = 0;
            foreach (KeyValuePair<string, string> a in new List<KeyValuePair<string, string>>(Anweisungen))
            {
                DataRepository.ExecuteNonQuery(a.Value);
                n++;
                bericht?.Add(a.Key);
            }
            if (n == 0) bericht?.Add(TAB_STAMM + " und " + TAB_PROJEKT + ": Temperaturspalten vorhanden");

            // PW5: die acht typischen Betriebsweisen - wiederholbar, nie überschreibend.
            ProzesstypSaat.Ausfuehren(bericht);
            return n;
        }
    }
}
