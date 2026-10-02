using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // FELDER DES KOLLEKTORFELDS UND BEZUGSFLÄCHE DES KOLLEKTORKATALOGS - Welle M2 Solarthermie
    // der Entscheidungsvorlage „Modellgrenzen der Rechenwege" (ST1, ST2, ST3 Stufe 1, ST4, ST6).
    //
    // WO DIE FELDER STEHEN. Die Auslegung eines Kollektorfelds - Modulanzahl, Neigung, Azimut -
    // steht an seiner Anlagenzeile (Tab_Energieanlagen, ID_Type = Solarthermie); dorthin kommen
    // die fünf Felder des Solarkreises. Der Kollektorsatz (Tab_Solarkollektoren_STAMM und seine
    // Projektkopie Tab_Solarkollektoren) trägt die Kennwerte η₀, a₁, a₂ - und mit diesem Schritt
    // die Fläche, auf die sie bezogen sind.
    //
    // FÜNF SPALTEN an Tab_Energieanlagen, alle nullbar mit Prüfklausel (leer = Vorgabe):
    //   Pumpenleistung_W            REAL  0 … 10 000   leer = Hilfsenergie_Anteil, sonst kein Pumpenstrom
    //   Solarkreisverluste_Prozent  REAL  0 … 50       leer = 8 %
    //   Uebertrager_Graedigkeit_K   REAL  0 … 30       leer = 5 K
    //   Kollektor_Spreizung_K       REAL  0 … 30       leer = 10 K
    //   Arbeitstemperatur_Weg       TEXT  'fest' | 'speicher'   leer = 'fest'
    // EINE SPALTE an Tab_Solarkollektoren_STAMM und Tab_Solarkollektoren gleich (die Projektkopie
    // trägt den Katalogsatz Spalte für Spalte):
    //   Bezugsflaeche  TEXT NOT NULL DEFAULT 'apertur' CHECK (IN ('apertur','brutto'))
    // Alle drei Tabellen sind STRICT; REAL und TEXT sind dort zulässig.
    //
    // KEIN DML. Die Anlagenspalten entstehen leer, jeder Kollektorsatz bekommt 'apertur' - die
    // Fläche, mit der er heute rechnet. Die Vorgaben rechnen wie zuvor: Verluste 8 %, feste
    // Arbeitstemperatur, Aperturfläche, kein Pumpenstrom ohne gepflegten Wert. Der Referenzlauf
    // bleibt byte-gleich.
    //
    // DIE NAMEN STEHEN NUR ALS ARGUMENT (Muster KesselBereitschaftEinheitSchema): Die Anweisung
    // entsteht aus Tabelle, Spalte und Typ; Werkzeuge/SqlDialektPruefer sieht keinen fertigen
    // Text, der gegen eine migrierte Datenbank „duplicate column" wäre.
    //
    // VIER LESER: der Schemaschritt in WindowsFormsApplication1/Allgemein/Update/
    // SchemaMigration.cs, das Werkzeug Werkzeuge/Testdatenbankschema, die Testvorrichtung samt
    // Nachweis in EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// Die Spalten des Kollektorfelds an <c>Tab_Energieanlagen</c> und die Bezugsfläche am
    /// Kollektorkatalog — EINE Quelle für Migration, Werkzeug, Testkopie, Controller, Rechenweg
    /// und Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class SolarthermieFelderSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht: der nächste
        /// Schritt hinter der Einheit des Kessel-Bereitschaftsverlusts.
        /// </summary>
        public const int SCHRITT = KesselBereitschaftEinheitSchema.SCHRITT + 1;

        /// <summary>Die Anlagenzeilen, an denen das Kollektorfeld steht.</summary>
        public const string TAB_ANLAGEN = SchemaKatalog.TAB_ENERGIEANLAGEN;

        /// <summary>Der Kollektorkatalog der Auslieferung.</summary>
        public const string TAB_KATALOG_STAMM = "Tab_Solarkollektoren_STAMM";

        /// <summary>Die Projektkopien der Kollektorsätze.</summary>
        public const string TAB_KATALOG_PROJEKT = "Tab_Solarkollektoren";

        /// <summary>Elektrische Leistung der Solarkreispumpe [W] (ST1).</summary>
        public const string SPALTE_PUMPENLEISTUNG = "Pumpenleistung_W";

        /// <summary>Wärmeverluste des Solarkreises [% des Bruttoertrags] (ST3 Stufe 1).</summary>
        public const string SPALTE_VERLUSTE = "Solarkreisverluste_Prozent";

        /// <summary>Grädigkeit des Wärmeübertragers zum Speicher [K] (ST4).</summary>
        public const string SPALTE_GRAEDIGKEIT = "Uebertrager_Graedigkeit_K";

        /// <summary>Spreizung des Kollektorkreises, Austritt minus Eintritt [K] (ST2).</summary>
        public const string SPALTE_SPREIZUNG = "Kollektor_Spreizung_K";

        /// <summary>
        /// Weg der Arbeitstemperatur: <see cref="DbWerte.SOLAR_ARBEITSTEMPERATUR_FEST"/> oder
        /// <see cref="DbWerte.SOLAR_ARBEITSTEMPERATUR_SPEICHER"/> (ST2); leer = fest.
        /// </summary>
        public const string SPALTE_ARBEITSTEMPERATUR = "Arbeitstemperatur_Weg";

        /// <summary>
        /// Bezugsfläche der Kollektorkennwerte: <see cref="DbWerte.SOLAR_BEZUGSFLAECHE_APERTUR"/>
        /// oder <see cref="DbWerte.SOLAR_BEZUGSFLAECHE_BRUTTO"/> (ST6).
        /// </summary>
        public const string SPALTE_BEZUGSFLAECHE = "Bezugsflaeche";

        /// <summary>Obergrenze der Pumpenleistung [W].</summary>
        public const double PUMPENLEISTUNG_MAX_W = 10000;

        /// <summary>Obergrenze der Solarkreisverluste [%].</summary>
        public const double VERLUSTE_MAX_PROZENT = 50;

        /// <summary>Obergrenze von Grädigkeit und Spreizung [K].</summary>
        public const double TEMPERATURDIFFERENZ_MAX_K = 30;

        /// <summary>Eine Zahlenspalte mit Prüfklausel; leer bleibt zulässig.</summary>
        private static string Zahl(string spalte, double max)
            => "REAL CHECK (\"" + spalte + "\" BETWEEN 0 AND " +
               max.ToString(CultureInfo.InvariantCulture) + ")";

        /// <summary>Die fünf Spalten der Anlagenzeile in dieser Reihenfolge — Name und Typdefinition.</summary>
        public static readonly KeyValuePair<string, string>[] ANLAGENSPALTEN =
        {
            new KeyValuePair<string, string>(SPALTE_PUMPENLEISTUNG, Zahl(SPALTE_PUMPENLEISTUNG, PUMPENLEISTUNG_MAX_W)),
            new KeyValuePair<string, string>(SPALTE_VERLUSTE, Zahl(SPALTE_VERLUSTE, VERLUSTE_MAX_PROZENT)),
            new KeyValuePair<string, string>(SPALTE_GRAEDIGKEIT, Zahl(SPALTE_GRAEDIGKEIT, TEMPERATURDIFFERENZ_MAX_K)),
            new KeyValuePair<string, string>(SPALTE_SPREIZUNG, Zahl(SPALTE_SPREIZUNG, TEMPERATURDIFFERENZ_MAX_K)),
            new KeyValuePair<string, string>(SPALTE_ARBEITSTEMPERATUR,
                "TEXT CHECK (\"" + SPALTE_ARBEITSTEMPERATUR + "\" IN ('" + DbWerte.SOLAR_ARBEITSTEMPERATUR_FEST +
                "','" + DbWerte.SOLAR_ARBEITSTEMPERATUR_SPEICHER + "'))"),
        };

        /// <summary>Die Typdefinition der Bezugsfläche: Pflichtfeld mit Vorgabe Apertur und Prüfklausel.</summary>
        public const string TYP_BEZUGSFLAECHE =
            "TEXT NOT NULL DEFAULT '" + DbWerte.SOLAR_BEZUGSFLAECHE_APERTUR + "' CHECK (\"" + SPALTE_BEZUGSFLAECHE +
            "\" IN ('" + DbWerte.SOLAR_BEZUGSFLAECHE_APERTUR + "','" + DbWerte.SOLAR_BEZUGSFLAECHE_BRUTTO + "'))";

        /// <summary>Die beiden Katalogtabellen, in dieser Reihenfolge: Katalog, dann Projektkopien.</summary>
        public static readonly string[] KATALOGTABELLEN = { TAB_KATALOG_STAMM, TAB_KATALOG_PROJEKT };

        /// <summary>Die drei Tabellen des Schritts — für die Prüfung, dass sie vorhanden sind.</summary>
        public static readonly string[] TABELLEN = { TAB_ANLAGEN, TAB_KATALOG_STAMM, TAB_KATALOG_PROJEKT };

        /// <summary>Alle Spalten des Schritts als (Tabelle, Spalte, Typ), in Anweisungsfolge.</summary>
        private static IEnumerable<(string Tabelle, string Spalte, string Typ)> Spalten()
        {
            foreach (KeyValuePair<string, string> s in ANLAGENSPALTEN)
                yield return (TAB_ANLAGEN, s.Key, s.Value);
            foreach (string tabelle in KATALOGTABELLEN)
                yield return (tabelle, SPALTE_BEZUGSFLAECHE, TYP_BEZUGSFLAECHE);
        }

        /// <summary>Stehen alle sieben Spalten? Dann ist der Schritt gelaufen.</summary>
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
        /// <returns>Die Zahl der angelegten Spalten (0 bis 7).</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            int n = 0;
            foreach (KeyValuePair<string, string> a in new List<KeyValuePair<string, string>>(Anweisungen))
            {
                DataRepository.ExecuteNonQuery(a.Value);
                n++;
                bericht?.Add(a.Key);
            }
            if (n == 0) bericht?.Add(TAB_ANLAGEN + ", " + TAB_KATALOG_STAMM + " und " + TAB_KATALOG_PROJEKT +
                                     ": Spalten des Kollektorfelds vorhanden");
            return n;
        }
    }
}
