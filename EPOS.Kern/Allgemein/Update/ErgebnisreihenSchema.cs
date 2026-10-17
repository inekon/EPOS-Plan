using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // SCHRITT 239 - ERGEBNISREIHEN: BHKW-EINSPEISUNG, STANDBY DES SPEICHERSYSTEMS, PV-ABREGELUNG (Welle EB-1,
    // Anwenderentscheide vom 17.10.2026, Konzept Ergebnisreihen). Angemeldet als 239, haengt ueber + 1 an 238
    // KlimaRohwerteSchema (KD-4).
    //
    // ANLASS. Drei Groessen rechnet der Lauf, der Ergebnisbaum fuehrt sie nicht: die Einspeisung des BHKW stand allein in
    // Tab_ErgebnisStromMatrix.KwkEinspMWh (geschrieben erst von der Wirtschaftlichkeitsrechnung) und als Laufreihe, der
    // Standby des Speichersystems (SimulationControl.SpeichersystemEigenverbrauchKwh) nur im Referenzlauf-Export, die
    // PV-Abregelung an der Einspeisegrenze nur als Laufreihe PV_ABREGELUNG. Das Energieflussbild bilanzierte die
    // Einspeisung am Stromknoten und zeigte Standby und Abregelung als "nicht zugeordnet".
    //
    // WAS. Drei Spalten, REAL, nullbar, CHECK (x IS NULL OR x >= 0):
    //   Tab_ErgebnisBHKW.Stromeinspeisung            [MWh/a] wie die Nachbarspalten der Zeile,
    //   Tab_ErgebnisPhotovoltaik.Abregelung           [MWh/a] wie die Nachbarspalten der Zeile,
    //   Tab_ErgebnisStromspeicher.Eigenverbrauch_System [kWh/a] wie die Nachbarspalten der Zeile (eine Zeile je Lauf).
    // NULL heisst "nicht erhoben" (Ergebnis vor diesem Schritt): das Energieflussbild faellt dann auf die Bilanzierung
    // zurueck, die Kennzahl auf die Laufreihe.
    //
    // ERGEBNISNEUTRAL. Kein DML an Bestandsdaten; der Rechenweg bleibt unveraendert, der Lauf schreibt nur zusaetzlich,
    // was er schon rechnet. Der Referenzlauf bekommt drei neue Schluessel in aggregate.csv (Basis R59).
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in EPOS.Kern.Tests und die
    // Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>Schemaschritt der Ergebnisreihen</b> (EB-1): <c>Stromeinspeisung</c> an <c>Tab_ErgebnisBHKW</c>, <c>Abregelung</c>
    /// an <c>Tab_ErgebnisPhotovoltaik</c>, <c>Eigenverbrauch_System</c> an <c>Tab_ErgebnisStromspeicher</c>. Wiederholbar,
    /// ergebnisneutral, kein DML.
    /// </summary>
    public static class ErgebnisreihenSchema
    {
        /// <summary>
        /// Die Nummer des Schritts (239); hängt über <c>+ 1</c> an 238 (<see cref="KlimaRohwerteSchema"/>, KD-4). Die Nummer
        /// steht allein hier.
        /// </summary>
        public const int SCHRITT = KlimaRohwerteSchema.SCHRITT + 1;

        /// <summary>Die Einspeisung des BHKW-Stroms ins Netz [MWh/a] an <c>Tab_ErgebnisBHKW</c>; NULL = nicht erhoben.</summary>
        public const string SPALTE_BHKW_STROMEINSPEISUNG = "Stromeinspeisung";

        /// <summary>Die abgeregelte PV-Energie an der Einspeisegrenze [MWh/a] an <c>Tab_ErgebnisPhotovoltaik</c>; NULL = nicht erhoben.</summary>
        public const string SPALTE_PV_ABREGELUNG = "Abregelung";

        /// <summary>Der Standby (Eigenverbrauch) des Speichersystems [kWh/a] an <c>Tab_ErgebnisStromspeicher</c>; NULL = nicht erhoben.</summary>
        public const string SPALTE_SPEICHER_EIGENVERBRAUCH = "Eigenverbrauch_System";

        /// <summary>Eine Spalte an ihrer Tabelle.</summary>
        public sealed class Spalte
        {
            public string Tabelle { get; }
            public string Name { get; }
            public string Typ { get; }
            public Spalte(string tabelle, string name) { Tabelle = tabelle; Name = name; Typ = ErgebnisreihenSchema.Typ(name); }
        }

        private static string Q(string s) => "\"" + s + "\"";

        /// <summary>Der Typ einer Spalte samt <c>CHECK</c> (nullbar, nie negativ).</summary>
        public static string Typ(string spalte) => "REAL CHECK (" + Q(spalte) + " IS NULL OR " + Q(spalte) + " >= 0)";

        /// <summary>Die drei Spalten in Anlegereihenfolge: BHKW, Photovoltaik, Stromspeicher.</summary>
        public static readonly IReadOnlyList<Spalte> SPALTEN = new[]
        {
            new Spalte(SchemaKatalog.TAB_ERGEBNISBHKW, SPALTE_BHKW_STROMEINSPEISUNG),
            new Spalte(ErgebnisCtrl.TAB_PV, SPALTE_PV_ABREGELUNG),
            new Spalte(SchemaKatalog.TAB_ERGEBNISSTROMSPEICHER, SPALTE_SPEICHER_EIGENVERBRAUCH),
        };

        /// <summary>Die drei Tabellen (jede muss ein früherer Schritt angelegt haben).</summary>
        public static IReadOnlyList<string> TABELLEN => SPALTEN.Select(s => s.Tabelle).ToList();

        /// <summary>Die ADD-COLUMN-Anweisung einer Spalte.</summary>
        public static string Anlegen(Spalte s) => "ALTER TABLE " + Q(s.Tabelle) + " ADD COLUMN " + Q(s.Name) + " " + s.Typ;

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => TABELLEN;

        /// <summary>Was noch offen ist: je fehlende Spalte 1 (−1: eine Tabelle fehlt).</summary>
        public static int Offen()
        {
            if (!Voraussetzungen().All(DataRepository.TabelleVorhanden)) return -1;
            return SPALTEN.Count(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Name));
        }

        /// <summary>Steht der Schritt?</summary>
        public static bool Vollstaendig() => Offen() == 0;

        /// <summary>
        /// Steht der Schritt? Gemerkt je Datenbank, sobald er steht (ein Schritt wird nie zurückgenommen) — der Schreibweg
        /// des Ergebnisses fragt je Lauf eine feste Zahl Abfragen.
        /// </summary>
        public static bool Lesbar()
        {
            string pfad;
            try { pfad = DataRepository.GetDBPath() ?? ""; }
            catch (Exception) { pfad = ""; }
            lock (_sperre)
                if (pfad.Length > 0 && string.Equals(pfad, _pfadLesbar, StringComparison.OrdinalIgnoreCase)) return true;
            bool da = SPALTEN.All(s => DataRepository.TabelleVorhanden(s.Tabelle) && DataRepository.SpalteVorhanden(s.Tabelle, s.Name));
            if (da)
                lock (_sperre) _pfadLesbar = pfad;
            return da;
        }

        /// <summary>Vergisst die gemerkte Antwort von <see cref="Lesbar"/> (nach dem Anlegen und für Tests).</summary>
        public static void ProbeVerwerfen()
        {
            lock (_sperre) _pfadLesbar = null;
        }

        private static readonly object _sperre = new object();
        private static string _pfadLesbar;

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
            List<Spalte> offen = SPALTEN.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Name)).ToList();
            if (offen.Count == 0)
            {
                bericht?.Add("steht bereits - Stromeinspeisung, Abregelung und Eigenverbrauch_System an den Ergebniszeilen; nichts zu tun");
                return 0;
            }
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    foreach (Spalte s in offen) v.Ausfuehren(Anlegen(s));
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            ProbeVerwerfen();
            if (bericht != null)
            {
                foreach (Spalte s in offen) bericht.Add(s.Tabelle + "." + s.Name + " angelegt (leer)");
                bericht.Add("KEIN DML an Bestandsdaten - leere Spalte heisst nicht erhoben, der Rechenweg bleibt gleich");
            }
            return offen.Count;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
