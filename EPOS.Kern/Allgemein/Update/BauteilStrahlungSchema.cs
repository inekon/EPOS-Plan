using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // SCHRITT 237 - ABSORPTIONS- UND EMISSIONSGRAD JE BAUTEIL (Welle DA-8, Konzept Datenaustausch Abschnitt 6 F8 und 7).
    // Angemeldet als 237. VORLAEUFIGE KETTE: haengt ueber + 1 an 233 BauteilUHerkunftSchema, weil 234-236 der anderen
    // Sitzungen noch nicht auf origin liegen; nach Schritt 236 auf <Klasse von 236>.SCHRITT + 1 umhaengen.
    //
    // ANLASS. Der Rechenweg rechnet den kurzwelligen und den langwelligen Term der Aussenflaechen (VDI 6007-1 Gl. (33),
    // (38)) mit zwei Festwerten (GebaeudeFestwerte.ABSORPTIONSGRAD_OPAK, EMISSIONSGRAD_AUSSEN); gbXML und IFC fuehren beide
    // Werte je Konstruktion, EPOS konnte sie weder halten noch rechnen.
    //
    // WAS. Absorptionsgrad_Solar und Emissionsgrad  REAL, nullbar, CHECK (x IS NULL OR (x >= 0 AND x <= 1)), an
    // Tab_Bauteilaufbau(_STAMM) und Tab_Bauteil(_STAMM). NULL = Vorgabe: das Bauteil erbt den Wert seines Aufbaus, ohne
    // ihn gilt der Festwert.
    //
    // ERGEBNISNEUTRAL. Kein DML an Bestandsdaten; kein Referenzprojekt fuehrt einen Wert.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in EPOS.Kern.Tests und die
    // Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>Schemaschritt der Strahlungswerte je Bauteil</b> (DA-8): <c>Absorptionsgrad_Solar</c> und <c>Emissionsgrad</c> an
    /// <c>Tab_Bauteilaufbau(_STAMM)</c> und <c>Tab_Bauteil(_STAMM)</c>. Wiederholbar, ergebnisneutral, kein DML.
    /// </summary>
    public static class BauteilStrahlungSchema
    {
        /// <summary>
        /// Die Nummer des Schritts (237); hängt über <c>+ 1</c> an 236 (<see cref="KaeltetyptagSchema"/>, KBP-4a). Die Nummer steht
        /// allein hier.
        /// </summary>
        public const int SCHRITT = KaeltetyptagSchema.SCHRITT + 1;

        /// <summary>Absorptionsgrad der Außenfläche für Solarstrahlung a_F [–]; NULL = Vorgabe.</summary>
        public const string SPALTE_ABSORPTION = "Absorptionsgrad_Solar";

        /// <summary>Langwelliger Emissionsgrad der Außenfläche ε_F [–]; NULL = Vorgabe.</summary>
        public const string SPALTE_EMISSION = "Emissionsgrad";

        /// <summary>Die Vorgabe des Absorptionsgrads, wenn Bauteil und Aufbau leer sind (der Festwert des Rechenwegs).</summary>
        public const double VORGABE_ABSORPTION = GebaeudeFestwerte.ABSORPTIONSGRAD_OPAK;

        /// <summary>Die Vorgabe des Emissionsgrads, wenn Bauteil und Aufbau leer sind (der Festwert des Rechenwegs).</summary>
        public const double VORGABE_EMISSION = GebaeudeFestwerte.EMISSIONSGRAD_AUSSEN;

        private static string Q(string s) => "\"" + s + "\"";

        /// <summary>Der Typ einer Spalte samt <c>CHECK</c> (nullbar, Bereich 0 … 1).</summary>
        public static string Typ(string spalte) =>
            "REAL CHECK (" + Q(spalte) + " IS NULL OR (" + Q(spalte) + " >= 0 AND " + Q(spalte) + " <= 1))";

        /// <summary>Die beiden Spalten in Anlegereihenfolge — als Katalogspalten (Zusatzfelder des Aufbaus).</summary>
        public static readonly IReadOnlyList<BauteilkatalogSchema.Spalte> SPALTEN = new[]
        {
            new BauteilkatalogSchema.Spalte(SPALTE_ABSORPTION, BauteilkatalogSchema.Spaltenart.Zahl, Typ(SPALTE_ABSORPTION)),
            new BauteilkatalogSchema.Spalte(SPALTE_EMISSION, BauteilkatalogSchema.Spaltenart.Zahl, Typ(SPALTE_EMISSION)),
        };

        /// <summary>Die beiden Aufbautabellen — Katalog vor Kopie.</summary>
        public static readonly IReadOnlyList<string> TABELLEN_AUFBAU =
            new[] { BauteilkatalogSchema.TAB_AUFBAU_STAMM, BauteilkatalogSchema.TAB_AUFBAU };

        /// <summary>Die beiden Bauteiltabellen — Projekt vor Katalog.</summary>
        public static readonly IReadOnlyList<string> TABELLEN_BAUTEIL =
            new[] { SchemaKatalog.TAB_BAUTEIL, SchemaKatalog.TAB_BAUTEIL_STAMM };

        /// <summary>Alle vier Tabellen.</summary>
        public static IReadOnlyList<string> TABELLEN => TABELLEN_AUFBAU.Concat(TABELLEN_BAUTEIL).ToList();

        /// <summary>Die ADD-COLUMN-Anweisung einer Spalte an einer Tabelle.</summary>
        public static string Anlegen(string tabelle, BauteilkatalogSchema.Spalte s) =>
            "ALTER TABLE " + Q(tabelle) + " ADD COLUMN " + Q(s.Name) + " " + s.Typ;

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => TABELLEN;

        /// <summary>Was noch offen ist: je fehlende Spalte 1 (−1: eine Tabelle fehlt).</summary>
        public static int Offen()
        {
            if (!Voraussetzungen().All(DataRepository.TabelleVorhanden)) return -1;
            return TABELLEN.Sum(t => SPALTEN.Count(s => !DataRepository.SpalteVorhanden(t, s.Name)));
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
            bool da = TABELLEN.All(t => DataRepository.TabelleVorhanden(t) && DataRepository.SpalteVorhanden(t, SPALTE_EMISSION));
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
            var offen = new List<(string Tabelle, BauteilkatalogSchema.Spalte Spalte)>();
            foreach (string t in TABELLEN)
                foreach (BauteilkatalogSchema.Spalte s in SPALTEN)
                    if (!DataRepository.SpalteVorhanden(t, s.Name)) offen.Add((t, s));
            if (offen.Count == 0)
            {
                bericht?.Add("steht bereits - Absorptionsgrad_Solar und Emissionsgrad an Aufbau und Bauteil; nichts zu tun");
                return 0;
            }
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
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
            ProbeVerwerfen();
            GebaeudeZonenanschluss.ProbeVerwerfen();
            if (bericht != null)
            {
                foreach ((string t, BauteilkatalogSchema.Spalte s) in offen) bericht.Add(t + "." + s.Name + " angelegt (leer)");
                bericht.Add("KEIN DML an Bestandsdaten - leere Spalte heisst Vorgabe, der Referenzlauf bleibt gleich");
            }
            return offen.Count;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
