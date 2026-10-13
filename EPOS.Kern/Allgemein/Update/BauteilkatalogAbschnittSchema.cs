using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // SCHRITT 229 - INHOMOGENE SCHICHT UND FENSTER IM AUFBAUKATALOG (Entwurf Bauteilkatalog 6a, Wellen BK-5 und BK-6;
    // Entscheid E132 F7). Angemeldet als 229, haengt beim Merge an 228 GeraetekostenEntfallSchema; bis dahin haengt die Klasse
    // vorlaeufig an der letzten vorhandenen Klasse (KaeltebedarfSplitAbrechnungSchema, 223) - die Kette laeuft ueber + 1.
    //
    // BEFUND. Was BK-5 und BK-6 an Spalten brauchen, steht zum groessten Teil seit Schritt 221 (BauteilkatalogSchema):
    //   - Abschnitte: Tab_Bauteilabschnitt(_STAMM) (Nummer, Flaechenanteil 0..1, Text, Quellkennung) und an der Schicht der
    //     Verweis ID_Abschnitt. Der Flaechenanteil gehoert dem Abschnitt (Streifen), nicht jeder seiner Schichten; eine
    //     zweite Spalte Flaechenanteil an der Schicht wuerde dieselbe Aussage doppelt fuehren. Dieser Schritt legt deshalb
    //     KEINE Abschnittsspalte an.
    //   - Fenster: Ug, Uf, g_Wert, Rahmenanteil (0..1) am Aufbau - g_Wert und Rahmenanteil mit Name, Bedeutung und Einheit
    //     wie an Tab_Bauteil. Psi_Gesamt/Psi_System/Psi_Geometrie sind psi der Waermebruecke des Einbaus, nicht der Glasrand.
    //
    // WAS. Es fehlt allein der Waermebrueckenverlustkoeffizient des Glasrands psi_g [W/(m*K)] fuer U_w nach DIN EN ISO 10077-1:
    //   Tab_Bauteilaufbau_STAMM.Psi_Glasrand, Tab_Bauteilaufbau.Psi_Glasrand  REAL, nullbar, CHECK (NULL oder >= 0).
    //
    // ERGEBNISNEUTRAL. Kein Bestandssatz bekommt einen Wert; kein Referenzprojekt fuehrt einen Aufbau. Eine NULL-Spalte
    // aendert keine Pruefsumme eines Katalogsatzes (NULL steht nicht im Pruefsummentext).
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in EPOS.Kern.Tests und die
    // Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>Schemaschritt der inhomogenen Schicht und der Fenster im Aufbaukatalog</b> (BK-5/BK-6): ψ des Glasrands am Aufbau
    /// (Katalog und Projektkopie). Die Abschnitte selbst stehen seit <see cref="BauteilkatalogSchema"/>. Wiederholbar,
    /// ergebnisneutral, kein DML. Angemeldet als 229, hängt beim Merge an 228 <c>GeraetekostenEntfallSchema</c>.
    /// </summary>
    public static class BauteilkatalogAbschnittSchema
    {
        /// <summary>
        /// Die Nummer des Schritts — angemeldet als 229; vorläufig an <see cref="KaeltebedarfSplitAbrechnungSchema"/> (223)
        /// gehängt, beim Merge an 228 <c>GeraetekostenEntfallSchema</c>. Die Nummer steht allein hier.
        /// </summary>
        public const int SCHRITT = KaeltebedarfSplitAbrechnungSchema.SCHRITT + 1;

        /// <summary>ψ des Glasrands [W/(m·K)] (DIN EN ISO 10077-1, ψ_g); NULL = nicht angegeben.</summary>
        public const string SPALTE_PSI_GLASRAND = "Psi_Glasrand";

        /// <summary>Die neuen Spalten von <c>Tab_Bauteilaufbau(_STAMM)</c> in Anlegereihenfolge.</summary>
        public static readonly IReadOnlyList<BauteilkatalogSchema.Spalte> AUFBAU_SPALTEN = new[]
        {
            new BauteilkatalogSchema.Spalte(SPALTE_PSI_GLASRAND, BauteilkatalogSchema.Spaltenart.Zahl,
                "REAL CHECK (\"" + SPALTE_PSI_GLASRAND + "\" IS NULL OR (\"" + SPALTE_PSI_GLASRAND + "\" >= 0))")
        };

        /// <summary>Die beiden Aufbautabellen — Katalog vor Kopie.</summary>
        public static readonly IReadOnlyList<string> TABELLEN =
            new[] { BauteilkatalogSchema.TAB_AUFBAU_STAMM, BauteilkatalogSchema.TAB_AUFBAU };

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => TABELLEN;

        /// <summary>Die ADD-COLUMN-Anweisung einer Spalte an einer Tabelle.</summary>
        public static string Anlegen(string tabelle, BauteilkatalogSchema.Spalte s) =>
            "ALTER TABLE \"" + tabelle + "\" ADD COLUMN \"" + s.Name + "\" " + s.Typ;

        /// <summary>Was noch offen ist: je fehlende Spalte 1 (−1: eine Tabelle fehlt).</summary>
        public static int Offen()
        {
            if (!Voraussetzungen().All(DataRepository.TabelleVorhanden)) return -1;
            return TABELLEN.Sum(t => AUFBAU_SPALTEN.Count(s => !DataRepository.SpalteVorhanden(t, s.Name)));
        }

        /// <summary>Steht der Schritt?</summary>
        public static bool Vollstaendig() => Offen() == 0;

        /// <summary>
        /// Steht der Schritt? Gemerkt je Datenbank, sobald er steht (ein Schritt wird nie zurückgenommen) — die Leser des
        /// Bauteilwegs fragen je Projekt eine feste Zahl Abfragen.
        /// </summary>
        public static bool Lesbar()
        {
            string pfad;
            try { pfad = DataRepository.GetDBPath() ?? ""; }
            catch (Exception) { pfad = ""; }
            lock (_sperre)
                if (pfad.Length > 0 && string.Equals(pfad, _pfadLesbar, StringComparison.OrdinalIgnoreCase)) return true;
            bool da = DataRepository.TabelleVorhanden(BauteilkatalogSchema.TAB_AUFBAU) &&
                      DataRepository.SpalteVorhanden(BauteilkatalogSchema.TAB_AUFBAU, SPALTE_PSI_GLASRAND);
            if (da)
                lock (_sperre) _pfadLesbar = pfad;
            return da;
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
            var offen = new List<(string Tabelle, BauteilkatalogSchema.Spalte Spalte)>();
            foreach (string t in TABELLEN)
                foreach (BauteilkatalogSchema.Spalte s in AUFBAU_SPALTEN)
                    if (!DataRepository.SpalteVorhanden(t, s.Name)) offen.Add((t, s));
            if (offen.Count == 0)
            {
                bericht?.Add("steht bereits - Psi_Glasrand am Aufbaukatalog; nichts zu tun");
                return 0;
            }
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach ((string t, BauteilkatalogSchema.Spalte s) in offen) v.Ausfuehren(Anlegen(t, s));
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
                foreach ((string t, BauteilkatalogSchema.Spalte s) in offen) bericht.Add(t + "." + s.Name + " angelegt (leer)");
                bericht.Add("KEIN DML an Bestandsdaten - kein Referenzprojekt fuehrt einen Aufbau, der Referenzlauf bleibt gleich");
            }
            return offen.Count;
        }
    }
}
