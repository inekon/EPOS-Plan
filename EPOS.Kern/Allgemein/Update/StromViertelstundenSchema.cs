using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // EINSPEISEGRENZE DES PROJEKTS UND SELBSTENTLADUNG DES STROMSPEICHERS - Welle M5 „Strom in
    // Viertelstunden" der Entscheidungsvorlage „Modellgrenzen der Rechenwege" (PV3, SP1).
    //
    // WO DIE FELDER STEHEN. Die Einspeisegrenze gilt am Netzanschlusspunkt des Projekts, nicht je
    // Anlage; sie steht deshalb an der Projekteinstellung Tab_Einstellungen, neben den übrigen
    // Laufparametern. Die Selbstentladung ist ein Gerätekennwert des Stromspeichers; sie steht am
    // Katalog Tab_Stromspeicher_STAMM und an seiner Projektkopie Tab_Stromspeicher (die Kopie trägt
    // den Katalogsatz Spalte für Spalte). Der Standby-Verbrauch des Speichersystems hat seinen Ort
    // schon: Tab_Stromspeicher(_STAMM).Standby_Verbrauch [W] - er bekommt keine zweite Spalte.
    //
    // ZWEI SPALTEN an Tab_Einstellungen, beide nullbar mit Prüfklausel (leer = keine Grenze):
    //   Einspeisegrenze_Wert     REAL  >= 0          leer = keine Einspeisegrenze
    //   Einspeisegrenze_Einheit  TEXT  'kW' | '%'    leer = kW
    // EINE SPALTE an Tab_Stromspeicher_STAMM und Tab_Stromspeicher gleich:
    //   Selbstentladung_Prozent_Monat  REAL  0 … 20   leer = keine Selbstentladung
    // Alle drei Tabellen sind STRICT; REAL und TEXT sind dort zulässig.
    //
    // KEIN DML. Alle Spalten entstehen leer; leer rechnet wie zuvor (keine Abregelung, keine
    // Selbstentladung). Der Schritt ist ergebnisneutral.
    //
    // DIE NAMEN STEHEN NUR ALS ARGUMENT (Muster SolarthermieFelderSchema): Die Anweisung entsteht aus
    // Tabelle, Spalte und Typ; Werkzeuge/SqlDialektPruefer sieht keinen fertigen Text, der gegen
    // eine migrierte Datenbank „duplicate column" wäre.
    //
    // VIER LESER: der Schemaschritt in WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs,
    // das Werkzeug Werkzeuge/Testdatenbankschema, die Testvorrichtung samt Nachweis in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// Die Einspeisegrenze an <c>Tab_Einstellungen</c> und die Selbstentladung am
    /// Stromspeicherkatalog — EINE Quelle für Migration, Werkzeug, Testkopie, Controller, Rechenweg
    /// und Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class StromViertelstundenSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht: der nächste
        /// Schritt hinter den Feldern des Kollektorfelds.
        /// </summary>
        public const int SCHRITT = SolarthermieFelderSchema.SCHRITT + 1;

        /// <summary>Die Projekteinstellungen, an denen die Einspeisegrenze steht.</summary>
        public const string TAB_EINSTELLUNGEN = SchemaKatalog.TAB_EINSTELLUNGEN;

        /// <summary>Der Stromspeicherkatalog der Auslieferung.</summary>
        public const string TAB_KATALOG_STAMM = SchemaKatalog.TAB_STROMSPEICHER_STAMM;

        /// <summary>Die Projektkopien der Stromspeicher.</summary>
        public const string TAB_KATALOG_PROJEKT = SchemaKatalog.TAB_STROMSPEICHER;

        /// <summary>Zahlenwert der Einspeisegrenze, in der Einheit <see cref="SPALTE_EINSPEISEGRENZE_EINHEIT"/> (PV3).</summary>
        public const string SPALTE_EINSPEISEGRENZE_WERT = "Einspeisegrenze_Wert";

        /// <summary>
        /// Einheit der Einspeisegrenze: <see cref="DbWerte.EINSPEISEGRENZE_KW"/> oder
        /// <see cref="DbWerte.EINSPEISEGRENZE_PROZENT"/> (% der installierten PV-Leistung); leer = kW.
        /// </summary>
        public const string SPALTE_EINSPEISEGRENZE_EINHEIT = "Einspeisegrenze_Einheit";

        /// <summary>Selbstentladung des Speichers [% des Inhalts je Monat] (SP1).</summary>
        public const string SPALTE_SELBSTENTLADUNG = "Selbstentladung_Prozent_Monat";

        /// <summary>
        /// Der Standby-Verbrauch des Speichersystems [W] (SP1) — die BESTANDSSPALTE des Katalogs; der
        /// Schritt legt sie nicht an, er benennt sie nur für Controller und Rechenweg.
        /// </summary>
        public const string SPALTE_STANDBY = "Standby_Verbrauch";

        /// <summary>Obergrenze der Selbstentladung [%/Monat].</summary>
        public const double SELBSTENTLADUNG_MAX_PROZENT = 20;

        /// <summary>
        /// Obergrenze des Standby-Verbrauchs [W]. Die Bestandsspalte trägt keine Prüfklausel; der
        /// Rechenweg nimmt einen Wert außerhalb 0 … 1 000 nicht und meldet ihn.
        /// </summary>
        public const double STANDBY_MAX_W = 1000;

        /// <summary>Die zwei Spalten der Projekteinstellung in dieser Reihenfolge — Name und Typdefinition.</summary>
        public static readonly KeyValuePair<string, string>[] EINSTELLUNGSSPALTEN =
        {
            new KeyValuePair<string, string>(SPALTE_EINSPEISEGRENZE_WERT,
                "REAL CHECK (\"" + SPALTE_EINSPEISEGRENZE_WERT + "\" >= 0)"),
            new KeyValuePair<string, string>(SPALTE_EINSPEISEGRENZE_EINHEIT,
                "TEXT CHECK (\"" + SPALTE_EINSPEISEGRENZE_EINHEIT + "\" IN ('" + DbWerte.EINSPEISEGRENZE_KW +
                "','" + DbWerte.EINSPEISEGRENZE_PROZENT + "'))"),
        };

        /// <summary>Die Typdefinition der Selbstentladung: nullbar mit Prüfklausel.</summary>
        public static readonly string TYP_SELBSTENTLADUNG =
            "REAL CHECK (\"" + SPALTE_SELBSTENTLADUNG + "\" BETWEEN 0 AND " +
            SELBSTENTLADUNG_MAX_PROZENT.ToString(CultureInfo.InvariantCulture) + ")";

        /// <summary>Die beiden Katalogtabellen, in dieser Reihenfolge: Katalog, dann Projektkopien.</summary>
        public static readonly string[] KATALOGTABELLEN = { TAB_KATALOG_STAMM, TAB_KATALOG_PROJEKT };

        /// <summary>Die drei Tabellen des Schritts — für die Prüfung, dass sie vorhanden sind.</summary>
        public static readonly string[] TABELLEN = { TAB_EINSTELLUNGEN, TAB_KATALOG_STAMM, TAB_KATALOG_PROJEKT };

        /// <summary>Alle Spalten des Schritts als (Tabelle, Spalte, Typ), in Anweisungsfolge.</summary>
        private static IEnumerable<(string Tabelle, string Spalte, string Typ)> Spalten()
        {
            foreach (KeyValuePair<string, string> s in EINSTELLUNGSSPALTEN)
                yield return (TAB_EINSTELLUNGEN, s.Key, s.Value);
            foreach (string tabelle in KATALOGTABELLEN)
                yield return (tabelle, SPALTE_SELBSTENTLADUNG, TYP_SELBSTENTLADUNG);
        }

        /// <summary>Stehen alle vier Spalten? Dann ist der Schritt gelaufen.</summary>
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
        /// <returns>Die Zahl der angelegten Spalten (0 bis 4).</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            int n = 0;
            foreach (KeyValuePair<string, string> a in new List<KeyValuePair<string, string>>(Anweisungen))
            {
                DataRepository.ExecuteNonQuery(a.Value);
                n++;
                bericht?.Add(a.Key);
            }
            if (n == 0) bericht?.Add(TAB_EINSTELLUNGEN + ", " + TAB_KATALOG_STAMM + " und " + TAB_KATALOG_PROJEKT +
                                     ": Spalten der Einspeisegrenze und der Selbstentladung vorhanden");
            return n;
        }
    }
}
