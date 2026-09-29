using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // HEIZGRENZE DER KESSELBEREITSCHAFT - Anwenderentscheid 27.09.2026 zu #568, Punkt d:
    // „Heiztag = Tagesmittel der Aussentemperatur unter der Heizgrenze; Vorgabe 15 °C, je
    // Projekt individuell vorgebbar."
    //
    // WOZU. Ein stillstehender Heizkessel traegt seinen Bereitschaftsverlust, wenn er
    // betriebsbereit ist: an einem Heiztag oder im Nachlauf von 24 Stunden nach seiner
    // letzten Laufstunde (SimulationSPK.IstBetriebsbereit). Der Heiztag haengt an der
    // Heizgrenze des Projekts; diese Spalte traegt sie.
    //
    // EINE SPALTE. Tab_Einstellungen.Kessel_Heizgrenze REAL, nullbar, OHNE DEFAULT und OHNE
    // CHECK: NULL heisst „die Vorgabe" (SimulationSPK.HEIZGRENZE_VORGABE_C), kein DDL-DEFAULT
    // auf einem Fachwert; die Plausibilitaetsgrenzen haelt die Oberflaeche. Tab_Einstellungen
    // ist STRICT; REAL ist dort zulaessig.
    //
    // KEIN DML. Die Spalte entsteht in jeder Zeile als NULL - jedes Projekt rechnet danach
    // mit der Vorgabe. Die Rechenwirkung kommt mit der Regel, nicht mit der Spalte.
    //
    // DER NAME STEHT NUR ALS ARGUMENT. Dasselbe Muster wie bei SolarkollektorTemperaturen:
    // Gegen die bereits migrierte Referenzlaeufe/Kenndaten_Test.sqlite gehalten, waere eine
    // ausgeschriebene Anweisung zwangslaeufig „duplicate column" - Werkzeuge/SqlDialektPruefer
    // sieht deshalb keinen fertigen Text.
    //
    // DREI LESER: der Schemaschritt in WindowsFormsApplication1/Allgemein/Update/
    // SchemaMigration.cs, das Werkzeug Werkzeuge/Testdatenbankschema und die Testvorrichtung
    // samt Nachweis in EPOS.Kern.Tests.
    // ====================================================================================

    /// <summary>
    /// Die Heizgrenze der Kesselbereitschaft je Projekt (Anwenderentscheid 27.09.2026 zu #568)
    /// — EINE Quelle für Migration, Werkzeug, Testkopie und Nachweis (ADR-001 Option C).
    /// Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class KesselHeizgrenzeSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht. Vergeben
        /// unmittelbar vor dem Schemacommit gegen <c>origin</c> (Regel „lückenlos"); beim Zusammenführen
        /// mit KP1b (Schritt 152) und der Bezugsart Zimmer (Schritt 153, <see cref="TwwBezugsartSchema"/>) auf 154 umnummeriert.
        /// </summary>
        public const int SCHRITT = TwwBezugsartSchema.SCHRITT + 1;

        /// <summary>Die Tabelle der Projekteinstellungen.</summary>
        public const string TABELLE = SchemaKatalog.TAB_EINSTELLUNGEN;

        /// <summary>
        /// <c>Tab_Einstellungen.Kessel_Heizgrenze</c> [°C]: Ein Tag ist Heiztag, wenn das
        /// Tagesmittel der Außentemperatur darunter liegt. NULL = Vorgabe.
        /// </summary>
        public const string SPALTE = "Kessel_Heizgrenze";

        /// <summary>Die Typdefinition hinter dem Spaltennamen: nullbar, ohne Vorgabe, ohne Prüfung.</summary>
        public const string TYP = "REAL";

        /// <summary>Steht die Spalte? Dann ist der Schritt gelaufen.</summary>
        public static bool Vollstaendig()
        {
            return DataRepository.SpalteVorhanden(TABELLE, SPALTE);
        }

        /// <summary>
        /// Die Anweisung des Schritts — Beschreibung und SQL; leer, wenn die Spalte schon
        /// steht (<b>wiederholbar</b>). Fehlt die Tabelle ganz, meldet
        /// <see cref="DataRepository.SpalteVorhanden"/> <c>false</c>, und das
        /// <c>ALTER TABLE</c> scheitert benannt.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                if (!Vollstaendig())
                    yield return new KeyValuePair<string, string>(
                        TABELLE + "." + SPALTE + " anlegen",
                        "ALTER TABLE \"" + TABELLE + "\" ADD COLUMN \"" + SPALTE + "\" " + TYP);
            }
        }

        /// <summary>
        /// Führt den Schritt aus — für <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>; die Migration der Schale geht denselben Weg über ihre
        /// eigenen Helfer, aus derselben <see cref="Anweisungen"/>. <b>Kein DML.</b>
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (0 oder 1).</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            int n = 0;
            foreach (KeyValuePair<string, string> a in new List<KeyValuePair<string, string>>(Anweisungen))
            {
                DataRepository.ExecuteNonQuery(a.Value);
                n++;
                bericht?.Add(a.Key);
            }
            if (n == 0) bericht?.Add(TABELLE + "." + SPALTE + ": vorhanden");
            return n;
        }
    }
}
