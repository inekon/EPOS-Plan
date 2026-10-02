using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // BODENALBEDO JE ANLAGE - Welle M1a der Entscheidungsvorlage Modellgrenzen, Punkt PV4:
    // Die Albedo des Bodens vor einer Photovoltaik- oder Solarthermie-Anlage ist einstellbar.
    //
    // WOZU. Der bodenreflektierte Anteil der Einstrahlung auf die geneigte Ebene rechnet mit
    // GHI · Albedo · (1 − cos β) / 2. Für flache Dachanlagen über Gras ist 0,2 eine gute
    // Annahme; vor einer Fassade, über hellem Dach oder Schnee und bei bifazialen Modulen
    // wirkt der Bodenreflex deutlich stärker. Der Wert gehört deshalb der Anlage, nicht dem
    // Rechner.
    //
    // EINE SPALTE an Tab_Energieanlagen (STRICT):
    //   Albedo  REAL  CHECK (Albedo IS NULL OR (Albedo >= 0 AND Albedo <= 1))
    // Nullbar und ohne DDL-Vorgabe: NULL heißt „nicht gepflegt, es gilt die Vorgabe"
    // (Bodenalbedo.VORGABE = SolarCalculator.ALBEDO_BODEN = 0,2) - die Fachannahme steht im
    // Code, nicht im Schema (Hausregel „kein DDL-DEFAULT auf Fachwerten").
    //
    // KEIN DML. Jede Bestandszeile bleibt NULL und rechnet damit wie zuvor mit 0,2; der
    // Referenzlauf bleibt byte-gleich.
    //
    // DER NAME STEHT NUR ALS ARGUMENT (Muster KesselBereitschaftEinheitSchema): Die Anweisung
    // entsteht aus Tabelle, Spalte und Typ; Werkzeuge/SqlDialektPruefer sieht keinen fertigen
    // Text, der gegen die bereits migrierte Testdatenbank „duplicate column" wäre.
    //
    // VIER LESER: der Schemaschritt in WindowsFormsApplication1/Allgemein/Update/
    // SchemaMigration.cs, das Werkzeug Werkzeuge/Testdatenbankschema, die Testvorrichtung
    // in EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// Die Albedospalte der Anlagenzeile — EINE Quelle für Migration, Werkzeug, Testkopie,
    /// Controller und Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei;
    /// die Leseregel steht bei <see cref="Bodenalbedo"/>.
    /// </summary>
    public static class AlbedoSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht: der nächste
        /// Schritt hinter der Einheit des Bereitschaftsverlusts (<see cref="KesselBereitschaftEinheitSchema"/>).
        /// </summary>
        public const int SCHRITT = KesselBereitschaftEinheitSchema.SCHRITT + 1;

        /// <summary>Die Anlagentabelle.</summary>
        public const string TABELLE = SchemaKatalog.TAB_ENERGIEANLAGEN;

        /// <summary><c>Albedo</c>: die Bodenalbedo vor der Anlage (0 … 1); NULL = Vorgabe 0,2.</summary>
        public const string SPALTE = "Albedo";

        /// <summary>Die Typdefinition: nullbar, ohne Vorgabe, mit Prüfklausel 0 … 1.</summary>
        public const string TYP =
            "REAL CHECK (\"" + SPALTE + "\" IS NULL OR (\"" + SPALTE + "\" >= 0 AND \"" + SPALTE + "\" <= 1))";

        /// <summary>Steht die Spalte? Dann ist der Schritt gelaufen.</summary>
        public static bool Vollstaendig() => DataRepository.SpalteVorhanden(TABELLE, SPALTE);

        /// <summary>
        /// Die Anweisung des Schritts — Beschreibung und SQL; leer, wenn die Spalte steht
        /// (<b>wiederholbar</b>).
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                if (DataRepository.SpalteVorhanden(TABELLE, SPALTE)) yield break;
                yield return new KeyValuePair<string, string>(
                    TABELLE + "." + SPALTE + " anlegen",
                    "ALTER TABLE \"" + TABELLE + "\" ADD COLUMN \"" + SPALTE + "\" " + TYP);
            }
        }

        /// <summary>
        /// Führt den Schritt aus — für <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>; die Migration der Schale geht denselben Weg über ihre eigenen
        /// Helfer, aus derselben <see cref="Anweisungen"/>. <b>Kein DML.</b>
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
            if (n == 0) bericht?.Add(TABELLE + ": Albedospalte vorhanden");
            return n;
        }
    }

    /// <summary>
    /// <b>Die Leseregel der Bodenalbedo</b> — die eine Stelle, an der aus dem gepflegten Wert einer
    /// Anlagenzeile der Rechenwert wird. Photovoltaik und Solarthermie fragen hier, nicht selbst.
    /// </summary>
    public static class Bodenalbedo
    {
        /// <summary>Die Vorgabe ohne gepflegten Wert: 0,2 (Gras, offenes Gelände).</summary>
        public const double VORGABE = SolarCalculator.ALBEDO_BODEN;

        /// <summary>Kleinster zulässiger Wert.</summary>
        public const double MIN = 0.0;

        /// <summary>Größter zulässiger Wert.</summary>
        public const double MAX = 1.0;

        /// <summary>
        /// Der Rechenwert: der gepflegte Wert, wenn er zwischen 0 und 1 liegt, sonst
        /// <see cref="VORGABE"/> (leer, NaN oder außerhalb der Grenzen — die Prüfklausel der
        /// Spalte lässt Letzteres gar nicht zu).
        /// </summary>
        public static double Wert(double? gepflegt)
        {
            if (!gepflegt.HasValue) return VORGABE;
            double w = gepflegt.Value;
            if (double.IsNaN(w) || w < MIN || w > MAX) return VORGABE;
            return w;
        }

        /// <summary>Der Rechenwert einer Anlagenzeile; <c>null</c> = Vorgabe.</summary>
        public static double Wert(WErzeugerModel anlage) => Wert(anlage?.Albedo);

        /// <summary>Ist der Wert als Eingabe zulässig (leer oder 0 … 1)?</summary>
        public static bool Zulaessig(double? wert)
            => !wert.HasValue || (!double.IsNaN(wert.Value) && wert.Value >= MIN && wert.Value <= MAX);
    }
}
