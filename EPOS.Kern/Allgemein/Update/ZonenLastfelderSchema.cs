using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DA-2 - LASTFELDER DER ZONE FUER DEN RUNDLAUF DES DATENAUSTAUSCHS (Entscheid E138 F5, Anwender 13.10.2026;
    // Konzept Datenaustausch der Simulationsdaten, Abschnitte 2, 4.4 und 7; Schemabedarf aus dem Protokoll DA-1).
    //
    // WAS. Tab_Zone und ihr Katalogzwilling Tab_Zone_STAMM bekommen drei nullbare Spalten:
    //
    //   Aussenluft_Person   REAL  m3/(h*Person), >= 0 - die Aussenluft je Person (Inventar B14). gbXML OAFlowPerPerson
    //                             traegt sie; bisher ging sie beim Import im Nutzerluftwechsel auf.
    //   Licht_W_m2          REAL  W/m2, >= 0 - die Beleuchtungsleistung je Flaeche (B18). Sie ist ein TEIL der inneren
    //                             Lasten: Interne_Waermegewinne bzw. der Nennwert des Kalenders GERAETE bleiben die Summe,
    //                             die der Lauf liest. Ein Anteil (Licht_Anteil) ist nicht noetig: Summe und Lichtleistung
    //                             je Flaeche geben die Geraeteleistung ohne Verlust zurueck (Geraete = Summe - Licht x Flaeche).
    //   Personen_Waerme_W   REAL  W je Person, >= 0 - die Waermeabgabe je Person (B16). gbXML PeopleHeatGain traegt sie;
    //                             der Nennwert des Kalenders PERSONEN bleibt Personen x Waerme und ist, was der Lauf liest.
    //
    // ADD COLUMN mit CHECK; keine Bestandszeile aendert sich (alle drei Spalten leer).
    //
    // ERGEBNISNEUTRAL. Der Rechenweg liest keine der drei Spalten; keine Referenzzone traegt einen Wert. Der Referenzlauf
    // bleibt byte-gleich. Wiederholbar: ein zweiter Lauf findet nichts.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in EPOS.Kern.Tests und die
    // Paketanhebung (Art Ddl: ein aelteres Paket bringt seine Zonen ohne die Spalten, sie bleiben leer).
    // ====================================================================================

    /// <summary>
    /// <b>DA-2</b> — Lastfelder der Zone (Außenluft je Person, Lichtleistung je Fläche, Wärme je Person) an <c>Tab_Zone</c> und
    /// <c>Tab_Zone_STAMM</c> für den Rundlauf des Datenaustauschs (E138 F5). EINE Quelle für Migration, Werkzeug, Testkopie und
    /// Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class ZonenLastfelderSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht. Angemeldet ist 225; die Kette hängt über
        /// <c>+ 1</c> an der Vorgängerklasse (beim Zusammenführen an die Klasse des Schritts 224).
        /// </summary>
        public const int SCHRITT = BauteilkatalogHaustechnikSchema.SCHRITT + 1;

        /// <summary>Die Zonen des Projekts.</summary>
        public const string TAB_ZONE = SchemaKatalog.TAB_ZONE;

        /// <summary>Die Zonen eines Katalogsatzes (Zwilling).</summary>
        public const string TAB_ZONE_STAMM = SchemaKatalog.TAB_ZONE_STAMM;

        /// <summary>Außenluft je Person [m³/(h·Person)].</summary>
        public const string SPALTE_AUSSENLUFT_PERSON = "Aussenluft_Person";

        /// <summary>Beleuchtungsleistung je Fläche [W/m²] — Teil der inneren Lasten.</summary>
        public const string SPALTE_LICHT = "Licht_W_m2";

        /// <summary>Wärmeabgabe je Person [W].</summary>
        public const string SPALTE_PERSONEN_WAERME = "Personen_Waerme_W";

        /// <summary>Die Tabellen, an denen die Spalten stehen.</summary>
        public static readonly IReadOnlyList<string> TABELLEN = new[] { TAB_ZONE, TAB_ZONE_STAMM };

        private static string Q(string s) => "\"" + s + "\"";

        private static string Nichtnegativ(string spalte) => "REAL CHECK (" + Q(spalte) + " IS NULL OR " + Q(spalte) + " >= 0)";

        /// <summary>Die drei Spalten mit Typ und Prüfklausel in Anlegereihenfolge.</summary>
        public static readonly IReadOnlyList<(string Spalte, string Typ)> SPALTEN = new[]
        {
            (SPALTE_AUSSENLUFT_PERSON, Nichtnegativ(SPALTE_AUSSENLUFT_PERSON)),
            (SPALTE_LICHT, Nichtnegativ(SPALTE_LICHT)),
            (SPALTE_PERSONEN_WAERME, Nichtnegativ(SPALTE_PERSONEN_WAERME)),
        };

        /// <summary>Die <c>ALTER TABLE … ADD COLUMN</c>-Anweisung einer Spalte.</summary>
        public static string Anlegen(string tabelle, (string Spalte, string Typ) s)
            => "ALTER TABLE " + Q(tabelle) + " ADD COLUMN " + Q(s.Spalte) + " " + s.Typ;

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => TABELLEN;

        /// <summary>Stehen die drei Spalten an <c>Tab_Zone</c>? (Leser fragen das, bevor sie die Spalten nennen.)</summary>
        public static bool Lesbar() => DataRepository.TabelleVorhanden(TAB_ZONE) &&
                                       SPALTEN.All(s => DataRepository.SpalteVorhanden(TAB_ZONE, s.Spalte));

        /// <summary>Was noch offen ist: je fehlende Spalte 1 (−1: Tabelle fehlt).</summary>
        public static int Offen()
        {
            if (!Voraussetzungen().All(DataRepository.TabelleVorhanden)) return -1;
            return TABELLEN.Sum(t => SPALTEN.Count(s => !DataRepository.SpalteVorhanden(t, s.Spalte)));
        }

        /// <summary>Steht der Schritt?</summary>
        public static bool Vollstaendig() => Offen() == 0;

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang aus (wiederholbar); Rückgabe = Zahl der angelegten Spalten. Fehlt eine
        /// vorausgesetzte Tabelle, wirft er benannt.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen Verbindung.
            var offen = TABELLEN.SelectMany(t => SPALTEN.Where(s => !DataRepository.SpalteVorhanden(t, s.Spalte))
                                                         .Select(s => (Tabelle: t, Spalte: s))).ToList();
            if (offen.Count == 0)
            {
                bericht?.Add("steht bereits - Lastfelder der Zone (Aussenluft je Person, Licht, Waerme je Person); nichts zu tun");
                return 0;
            }
            var zeilen = new List<string>();
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach (var o in offen)
                    {
                        v.Ausfuehren(Anlegen(o.Tabelle, o.Spalte));
                        zeilen.Add(o.Tabelle + "." + o.Spalte.Spalte + " angelegt (leer)");
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
            {
                foreach (string z in zeilen) bericht.Add(z);
                bericht.Add("KEIN DML an Bestandsdaten; der Rechenweg liest die Spalten nicht, der Referenzlauf bleibt byte-gleich");
            }
            return offen.Count;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
