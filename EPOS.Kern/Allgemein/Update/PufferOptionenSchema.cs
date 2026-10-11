using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // OPTIONEN DES PUFFERSPEICHERS UND THERMISCHE DESINFEKTION - Welle M7 „Speicher" der
    // Entscheidungsvorlage „Modellgrenzen der Rechenwege" (PS1 (c), PS1 (a), PS5 (a), BW5).
    //
    // WO DIE FELDER STEHEN. Bereitschaftsweg, Aufstellraum, Zonenanteile und Frischwassermodul sind
    // Eigenschaften der ANLAGE, in der ein Speicher steht - sie stehen deshalb an der Projektkopie
    // Tab_Pufferspeicher, neben der Schichtzahl und den Entnahmehöhen (Schritt 53). Der Katalog
    // Tab_Pufferspeicher_STAMM führt nur Gerätewerte und bleibt, wie er ist. Die thermische
    // Desinfektion ist ein Betrieb des ganzen Projekts; sie steht an der Projekteinstellung
    // Tab_Einstellungen neben den übrigen Laufparametern.
    //
    // FÜNF SPALTEN an Tab_Pufferspeicher, alle nullbar mit Prüfklausel (leer = Rechnung wie zuvor):
    //   Bereitschaft_Weg           TEXT     'tag' | 'temperatur'   leer = tag
    //   Aufstellraum_Temperatur_C  REAL     0 … 35                  leer = 20 °C
    //   Schicht_Anteile            TEXT     1 … 200 Zeichen         leer = gleich große Zonen
    //   Frischwassermodul          INTEGER  0 | 1                   leer = 0 = aus
    //   FWM_Graedigkeit_K          REAL     0 … 20                  leer = 5 K
    // FÜNF SPALTEN an Tab_Einstellungen:
    //   Desinfektion_Aktiv             INTEGER  0 | 1         leer = 0 = aus
    //   Desinfektion_Intervall_Tage    INTEGER  1 … 31        leer = 7
    //   Desinfektion_Stunde            INTEGER  0 … 23        leer = 2
    //   Desinfektion_Zieltemperatur_C  REAL     55 … 90       leer = 70 °C
    //   Desinfektion_Volumen_l         REAL     0 … 100 000   leer = Volumen der Brauchwasserspeicher
    // Beide Tabellen sind STRICT; TEXT, REAL und INTEGER sind dort zulässig.
    //
    // KEIN DML. Alle Spalten entstehen leer; leer rechnet wie zuvor. Der Schritt ist ergebnisneutral.
    //
    // DIE NAMEN STEHEN NUR ALS ARGUMENT (Muster StromViertelstundenSchema): Die Anweisung entsteht aus
    // Tabelle, Spalte und Typ; Werkzeuge/SqlDialektPruefer sieht keinen fertigen Text, der gegen eine
    // migrierte Datenbank „duplicate column" wäre.
    //
    // VIER LESER: der Schemaschritt in WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs,
    // das Werkzeug Werkzeuge/Testdatenbankschema, die Testvorrichtung samt Nachweis in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// Die Optionen des Pufferspeichers an <c>Tab_Pufferspeicher</c> und die thermische Desinfektion an
    /// <c>Tab_Einstellungen</c> — EINE Quelle für Migration, Werkzeug, Testkopie, Controller, Rechenweg
    /// und Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class PufferOptionenSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht: der nächste
        /// Schritt hinter der Katalogempfehlung der Hilfsenergie (Weg B).
        /// </summary>
        public const int SCHRITT = HilfsenergieEmpfehlungNachzug.SCHRITT + 1;

        /// <summary>Die Projektkopien der Pufferspeicher.</summary>
        public const string TAB_PUFFER = SchemaKatalog.TAB_PUFFERSPEICHER;

        /// <summary>Die Projekteinstellungen, an denen die Desinfektion steht.</summary>
        public const string TAB_EINSTELLUNGEN = SchemaKatalog.TAB_EINSTELLUNGEN;

        // --- Pufferspeicher ------------------------------------------------------------------

        /// <summary>Rechenweg des Bereitschaftsverlusts: <see cref="DbWerte.PSP_BEREITSCHAFT_TAG"/> oder <see cref="DbWerte.PSP_BEREITSCHAFT_TEMPERATUR"/> (PS1 (c)).</summary>
        public const string SPALTE_BEREITSCHAFT_WEG = "Bereitschaft_Weg";

        /// <summary>Temperatur des Aufstellraums [°C] für den Weg „temperatur" (PS1 (c)).</summary>
        public const string SPALTE_AUFSTELLRAUM = "Aufstellraum_Temperatur_C";

        /// <summary>Volumenanteile der Zonen von oben, durch Semikolon getrennt (PS1 (a)).</summary>
        public const string SPALTE_SCHICHT_ANTEILE = "Schicht_Anteile";

        /// <summary>Frischwassermodul am Speicher, 0 oder 1 (PS5 (a)).</summary>
        public const string SPALTE_FRISCHWASSERMODUL = "Frischwassermodul";

        /// <summary>Grädigkeit des Frischwassermoduls [K] (PS5 (a)).</summary>
        public const string SPALTE_FWM_GRAEDIGKEIT = "FWM_Graedigkeit_K";

        // --- Projekteinstellung ---------------------------------------------------------------

        /// <summary>Schalter der thermischen Desinfektion, 0 oder 1 (BW5).</summary>
        public const string SPALTE_DESINFEKTION_AKTIV = "Desinfektion_Aktiv";

        /// <summary>Abstand zweier Desinfektionen [Tage] (BW5).</summary>
        public const string SPALTE_DESINFEKTION_INTERVALL = "Desinfektion_Intervall_Tage";

        /// <summary>Stunde des Tages, in der die Desinfektion läuft, 0 … 23 (BW5).</summary>
        public const string SPALTE_DESINFEKTION_STUNDE = "Desinfektion_Stunde";

        /// <summary>Zieltemperatur der Desinfektion [°C] (BW5).</summary>
        public const string SPALTE_DESINFEKTION_ZIEL = "Desinfektion_Zieltemperatur_C";

        /// <summary>Aufgeheiztes Volumen [l] (BW5).</summary>
        public const string SPALTE_DESINFEKTION_VOLUMEN = "Desinfektion_Volumen_l";

        // --- Grenzen der Prüfklauseln ---------------------------------------------------------

        /// <summary>Kleinste und größte Temperatur des Aufstellraums [°C].</summary>
        public const double AUFSTELLRAUM_MIN = 0, AUFSTELLRAUM_MAX = 35;

        /// <summary>Größte Grädigkeit des Frischwassermoduls [K].</summary>
        public const double FWM_GRAEDIGKEIT_MAX = 20;

        /// <summary>Größte Länge des Anteiltexts.</summary>
        public const int ANTEILE_LAENGE_MAX = 200;

        /// <summary>Grenzen des Intervalls [Tage].</summary>
        public const int INTERVALL_MIN = 1, INTERVALL_MAX = 31;

        /// <summary>Grenzen der Stunde.</summary>
        public const int STUNDE_MIN = 0, STUNDE_MAX = 23;

        /// <summary>Grenzen der Zieltemperatur [°C].</summary>
        public const double ZIEL_MIN = 55, ZIEL_MAX = 90;

        /// <summary>Größtes Volumen [l].</summary>
        public const double VOLUMEN_MAX = 100000;

        private static string Inv(double d) => d.ToString(CultureInfo.InvariantCulture);

        private static string Zwischen(string spalte, double min, double max)
            => "CHECK (\"" + spalte + "\" BETWEEN " + Inv(min) + " AND " + Inv(max) + ")";

        private static string Schalter(string spalte) => "CHECK (\"" + spalte + "\" IN (0,1))";

        /// <summary>Die fünf Spalten des Pufferspeichers in dieser Reihenfolge — Name und Typdefinition.</summary>
        public static readonly KeyValuePair<string, string>[] PUFFERSPALTEN =
        {
            new KeyValuePair<string, string>(SPALTE_BEREITSCHAFT_WEG,
                "TEXT CHECK (\"" + SPALTE_BEREITSCHAFT_WEG + "\" IN ('" + DbWerte.PSP_BEREITSCHAFT_TAG + "','" +
                DbWerte.PSP_BEREITSCHAFT_TEMPERATUR + "'))"),
            new KeyValuePair<string, string>(SPALTE_AUFSTELLRAUM,
                "REAL " + Zwischen(SPALTE_AUFSTELLRAUM, AUFSTELLRAUM_MIN, AUFSTELLRAUM_MAX)),
            new KeyValuePair<string, string>(SPALTE_SCHICHT_ANTEILE,
                "TEXT CHECK (length(\"" + SPALTE_SCHICHT_ANTEILE + "\") BETWEEN 1 AND " +
                ANTEILE_LAENGE_MAX.ToString(CultureInfo.InvariantCulture) + ")"),
            new KeyValuePair<string, string>(SPALTE_FRISCHWASSERMODUL,
                "INTEGER " + Schalter(SPALTE_FRISCHWASSERMODUL)),
            new KeyValuePair<string, string>(SPALTE_FWM_GRAEDIGKEIT,
                "REAL " + Zwischen(SPALTE_FWM_GRAEDIGKEIT, 0, FWM_GRAEDIGKEIT_MAX)),
        };

        /// <summary>Die fünf Spalten der Projekteinstellung in dieser Reihenfolge — Name und Typdefinition.</summary>
        public static readonly KeyValuePair<string, string>[] EINSTELLUNGSSPALTEN =
        {
            new KeyValuePair<string, string>(SPALTE_DESINFEKTION_AKTIV,
                "INTEGER " + Schalter(SPALTE_DESINFEKTION_AKTIV)),
            new KeyValuePair<string, string>(SPALTE_DESINFEKTION_INTERVALL,
                "INTEGER " + Zwischen(SPALTE_DESINFEKTION_INTERVALL, INTERVALL_MIN, INTERVALL_MAX)),
            new KeyValuePair<string, string>(SPALTE_DESINFEKTION_STUNDE,
                "INTEGER " + Zwischen(SPALTE_DESINFEKTION_STUNDE, STUNDE_MIN, STUNDE_MAX)),
            new KeyValuePair<string, string>(SPALTE_DESINFEKTION_ZIEL,
                "REAL " + Zwischen(SPALTE_DESINFEKTION_ZIEL, ZIEL_MIN, ZIEL_MAX)),
            new KeyValuePair<string, string>(SPALTE_DESINFEKTION_VOLUMEN,
                "REAL " + Zwischen(SPALTE_DESINFEKTION_VOLUMEN, 0, VOLUMEN_MAX)),
        };

        /// <summary>Die zwei Tabellen des Schritts — für die Prüfung, dass sie vorhanden sind.</summary>
        public static readonly string[] TABELLEN = { TAB_PUFFER, TAB_EINSTELLUNGEN };

        /// <summary>Alle Spalten des Schritts als (Tabelle, Spalte, Typ), in Anweisungsfolge.</summary>
        private static IEnumerable<(string Tabelle, string Spalte, string Typ)> Spalten()
        {
            foreach (KeyValuePair<string, string> s in PUFFERSPALTEN)
                yield return (TAB_PUFFER, s.Key, s.Value);
            foreach (KeyValuePair<string, string> s in EINSTELLUNGSSPALTEN)
                yield return (TAB_EINSTELLUNGEN, s.Key, s.Value);
        }

        /// <summary>Stehen alle zehn Spalten? Dann ist der Schritt gelaufen.</summary>
        public static bool Vollstaendig()
        {
            foreach (var s in Spalten())
                if (!DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)) return false;
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
            if (n == 0) bericht?.Add(TAB_PUFFER + " und " + TAB_EINSTELLUNGEN +
                                     ": Spalten der Pufferoptionen und der Desinfektion vorhanden");
            return n;
        }
    }
}
