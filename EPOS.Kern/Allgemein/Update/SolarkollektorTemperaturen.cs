using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // VOR- UND RUECKLAUF DES SOLARKOLLEKTORS FALLEN WEG
    // - Anwenderentscheid 26.09.2026: „Solarthermie: Katalogspalten VL/RL entfernen —
    //   keine Funktion."
    //
    // WOZU. Tab_Solarkollektoren_STAMM und seine Projektkopie Tab_Solarkollektoren
    // fuehrten je eine Spalte Vorlauf und Ruecklauf. Kein Rechenweg liest sie: Der Ertrag
    // (SimulationSolarthermie.Kollektorfelder_Lesen) rechnet mit einer festen
    // Speichertemperatur, und die Parameteruebersicht stufte beide schon als „nur Dialog"
    // ein. Ihr einziger Weg in die Welt war die Vorbelegung der Anlagenzeile
    // (AnlagenTemperaturen.AusStammsatz) - und von dort zog das Paar ueber die
    // Systemvorgabe die Temperaturen neuer Puffer herunter. Eine Spalte, die niemand
    // braucht und die nur Schaden anrichten kann, ist eine zweite, tote Wahrheit.
    //
    // ERGEBNISNEUTRAL, UND ZWAR OHNE ZUTUN. Keine der vier Spalten traegt eine
    // Rechengroesse; der Schritt schreibt KEIN DML, er entfernt nur. Kein Referenzprojekt
    // fuehrt Solarthermie in der Kaskade; der Referenzlauf bleibt gleich.
    //
    // KEIN TABELLENNEUBAU NOETIG. SQLite kann DROP COLUMN seit 3.35. Keine der Spalten
    // steht unter einem Index (die Indizes tragen Bezeichner und ID_Projekt), in einem
    // Fremdschluessel, einer generierten Spalte, einem Trigger oder einer Sicht; eine
    // Spalten-CHECK-Bedingung haben sie nicht. Beide Tabellen sind STRICT; das aendert
    // daran nichts.
    //
    // DIE NAMEN STEHEN NUR HIER, UND NUR ALS ARGUMENT. Dasselbe Muster wie bei
    // StrompreisAltspalten (Schritt 85): Gegen die bereits migrierte
    // Referenzlaeufe/Kenndaten_Test.sqlite gehalten, waere eine ausgeschriebene Anweisung
    // zwangslaeufig „no such column" - und genau so haelt sie Werkzeuge/SqlDialektPruefer.
    //
    // DREI LESER: der Schemaschritt in WindowsFormsApplication1/Allgemein/Update/
    // SchemaMigration.cs, das Werkzeug Werkzeuge/Testdatenbankschema und die
    // Testvorrichtung samt Nachweis in EPOS.Kern.Tests.
    // ====================================================================================

    /// <summary>
    /// Vor- und Rücklauf des Solarkollektors in Katalog und Projektkopie (Anwenderentscheid
    /// 26.09.2026) — EINE Quelle für Migration, Werkzeug und Nachweis.
    /// </summary>
    public static class SolarkollektorTemperaturen
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht. Sie folgt
        /// lückenlos auf die Katalogsätze M und A (<see cref="GebaeudeSaatSchema.SCHRITT"/>);
        /// wird der Schritt beim Zusammenführen umnummeriert, ändert sich nur diese Zeile.
        /// </summary>
        public const int SCHRITT = GebaeudeSaatSchema.SCHRITT + 1;

        /// <summary>Der Auslieferungskatalog der Kollektoren.</summary>
        public const string TABELLE_STAMM = "Tab_Solarkollektoren_STAMM";

        /// <summary>Die Projektkopie der Kollektoren.</summary>
        public const string TABELLE_PROJEKT = "Tab_Solarkollektoren";

        /// <summary>
        /// Die vier Spalten in fester Reihenfolge: Tabelle und Name. Jeder der drei Leser
        /// geht dieselbe Liste durch - aus ihr entstehen Anweisung, Zählung und Nachweis.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Spalten
        {
            get
            {
                yield return Paar(TABELLE_STAMM, "Vorlauf");
                yield return Paar(TABELLE_STAMM, "Ruecklauf");
                yield return Paar(TABELLE_PROJEKT, "Vorlauf");
                yield return Paar(TABELLE_PROJEKT, "Ruecklauf");
            }
        }

        /// <summary>
        /// Die Anweisungen in derselben festen Reihenfolge - Beschreibung und SQL.
        ///
        /// <para><b>Wiederholbar:</b> Ein zweiter Lauf findet keine der Spalten mehr
        /// (<see cref="Offen"/> = 0) und fasst nichts an; jede Anweisung entsteht nur für
        /// eine Spalte, die noch steht (<see cref="Vorhanden"/>). Fehlt eine Tabelle
        /// ganz, gibt es für sie nichts zu tun.</para>
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                foreach (KeyValuePair<string, string> s in Spalten)
                    if (Vorhanden(s.Key, s.Value))
                        yield return new KeyValuePair<string, string>(
                            s.Key + "." + s.Value + " entfernen",
                            SqlSpalteEntfernen(s.Key, s.Value));
            }
        }

        /// <summary>Steht diese Spalte noch?</summary>
        public static bool Vorhanden(string tabelle, string spalte)
        {
            return DataRepository.SpalteVorhanden(tabelle, spalte);
        }

        /// <summary>
        /// Wie viele der vier Spalten stehen noch? Genau so viele entfernt der Schritt.
        /// 0 = nichts zu tun, er ist gelaufen.
        /// </summary>
        public static int Offen()
        {
            int n = 0;
            foreach (KeyValuePair<string, string> s in Spalten)
                if (Vorhanden(s.Key, s.Value)) n++;
            return n;
        }

        /// <summary>
        /// Führt den Schritt aus: jede noch stehende Spalte fällt, jede Anweisung mit einer
        /// Zeile im <paramref name="bericht"/> (darf <c>null</c> sein). Rückgabe: Zahl der
        /// entfernten Spalten. Wiederholbar.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            int n = 0;
            foreach (KeyValuePair<string, string> a in new List<KeyValuePair<string, string>>(Anweisungen))
            {
                DataRepository.ExecuteNonQuery(a.Value);
                n++;
                if (bericht != null) bericht.Add(a.Key);
            }
            return n;
        }

        // =================================================================
        //  Der Baukasten - der Name der entfallenden Spalte steht nur als
        //  Argument (Begruendung im Kopfblock)
        // =================================================================

        private static KeyValuePair<string, string> Paar(string tabelle, string spalte)
        {
            return new KeyValuePair<string, string>(tabelle, spalte);
        }

        /// <summary>
        /// <c>ALTER TABLE … DROP COLUMN</c> — SQLite kann das seit 3.35 ohne Tabellenneubau.
        /// </summary>
        private static string SqlSpalteEntfernen(string tabelle, string spalte)
        {
            return "ALTER TABLE \"" + tabelle + "\" DROP COLUMN \"" + spalte + "\"";
        }
    }
}
