using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // NP7 - NORMWERTE DER DIN EINLESEN: AUSSENLUFT JE PERSON UND HERKUNFT DER WERTE
    // (Entscheid E131, Anwender 12.10.2026; Konzept Nutzungsprofile 4.1, 5.2 und 6.5).
    //
    // WAS. Tab_Raumnutzungsprofil bekommt vier nullbare Spalten:
    //
    //   Aussenluft_Person   REAL  m3/(h*Person), >= 0 - die Aussenluft je Person; die Tabelle der Norm fuehrt sie NEBEN der
    //                             flaechenbezogenen Aussenluft. Deshalb eine eigene Spalte und keine dritte Einheit von
    //                             Aussenluft_Einheit: Ein Profil traegt beide Werte zugleich, und der Tabellenneubau unterbleibt.
    //   Werte_Herkunft      TEXT  NULL oder 'EINGELESEN' - die Werte eines ausgelieferten Profils stammen aus "Normwerte einlesen"
    //   Werte_Quelle        TEXT  <= 400 Zeichen - Dateiname der Quelle (CSV des Anwenders oder Projektdatei)
    //   Werte_Datum         TEXT  JJJJ-MM-TT - Tag des Einlesens
    //
    // ADD COLUMN mit CHECK; keine Bestandszeile aendert sich (alle vier Spalten leer). Der CHECK der Herkunft prueft allein
    // die Spalte; dass Quelle und Datum zur Herkunft gehoeren, haelt der Schreibweg (RaumnutzungNormwerte).
    //
    // ERGEBNISNEUTRAL. Kein Referenzprojekt fuehrt ein Nutzungsprofil mit Aussenluft je Person; der Generator liest die
    // Spalte nur, wo die flaechen- oder raumbezogene Aussenluft fehlt. Wiederholbar: ein zweiter Lauf findet nichts.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in EPOS.Kern.Tests und die
    // Paketanhebung (Art Katalog: ein Paket fuehrt keinen Katalog der Nutzungsprofile).
    // ====================================================================================

    /// <summary>
    /// <b>NP7</b> — Außenluft je Person und Herkunft der Werte am Profil des Katalogs der Nutzungsprofile (E131). EINE Quelle
    /// für Migration, Werkzeug, Testkopie und Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class RaumnutzungNormwerteSchema
    {
        /// <summary><b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht (220, hängt an 219).</summary>
        public const int SCHRITT = RaumnutzungNormkategorienSchema.SCHRITT + 1;

        /// <summary>Die Profile.</summary>
        public const string TAB_PROFIL = RaumnutzungSchema.TAB_PROFIL;

        /// <summary>Außenluft je Person [m³/(h·Person)].</summary>
        public const string SPALTE_AUSSENLUFT_PERSON = "Aussenluft_Person";

        /// <summary>Herkunft der Werte (<c>NULL</c> oder <see cref="HERKUNFT_EINGELESEN"/>).</summary>
        public const string SPALTE_HERKUNFT = "Werte_Herkunft";

        /// <summary>Die Quelle der eingelesenen Werte (Dateiname).</summary>
        public const string SPALTE_QUELLE = "Werte_Quelle";

        /// <summary>Der Tag des Einlesens (JJJJ-MM-TT).</summary>
        public const string SPALTE_DATUM = "Werte_Datum";

        /// <summary>Die Herkunft „aus Normwerte einlesen“ (Persistenzwert, eingefroren).</summary>
        public const string HERKUNFT_EINGELESEN = "EINGELESEN";

        /// <summary>Die Höchstlänge der Quelle.</summary>
        public const int QUELLE_MAX_ZEICHEN = RaumnutzungSchema.BESCHREIBUNG_MAX_ZEICHEN;

        private static string Q(string s) => "\"" + s + "\"";

        /// <summary>Die vier Spalten mit Typ und Prüfklausel in Anlegereihenfolge.</summary>
        public static readonly IReadOnlyList<(string Spalte, string Typ)> SPALTEN = new[]
        {
            (SPALTE_AUSSENLUFT_PERSON, "REAL CHECK (" + Q(SPALTE_AUSSENLUFT_PERSON) + " IS NULL OR " + Q(SPALTE_AUSSENLUFT_PERSON) + " >= 0)"),
            (SPALTE_HERKUNFT, "TEXT CHECK (" + Q(SPALTE_HERKUNFT) + " IS NULL OR " + Q(SPALTE_HERKUNFT) + " IN ('" + HERKUNFT_EINGELESEN + "'))"),
            (SPALTE_QUELLE, "TEXT CHECK (" + Q(SPALTE_QUELLE) + " IS NULL OR length(" + Q(SPALTE_QUELLE) + ") <= " +
                            QUELLE_MAX_ZEICHEN.ToString(CultureInfo.InvariantCulture) + ")"),
            (SPALTE_DATUM, "TEXT CHECK (" + Q(SPALTE_DATUM) + " IS NULL OR " + Q(SPALTE_DATUM) +
                           " GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]')"),
        };

        /// <summary>Die <c>ALTER TABLE … ADD COLUMN</c>-Anweisung einer Spalte.</summary>
        public static string Anlegen((string Spalte, string Typ) s)
            => "ALTER TABLE " + Q(TAB_PROFIL) + " ADD COLUMN " + Q(s.Spalte) + " " + s.Typ;

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { RaumnutzungSchema.TAB_KATALOG, TAB_PROFIL };

        /// <summary>Stehen die vier Spalten? (Leser fragen das, bevor sie die Spalten nennen.)</summary>
        public static bool Lesbar() => DataRepository.TabelleVorhanden(TAB_PROFIL) &&
                                       SPALTEN.All(s => DataRepository.SpalteVorhanden(TAB_PROFIL, s.Spalte));

        /// <summary>Was noch offen ist: je fehlende Spalte 1 (−1: Tabelle fehlt).</summary>
        public static int Offen()
        {
            if (!Voraussetzungen().All(DataRepository.TabelleVorhanden)) return -1;
            return SPALTEN.Count(s => !DataRepository.SpalteVorhanden(TAB_PROFIL, s.Spalte));
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
            var offen = SPALTEN.Where(s => !DataRepository.SpalteVorhanden(TAB_PROFIL, s.Spalte)).ToList();
            if (offen.Count == 0)
            {
                bericht?.Add("steht bereits - Aussenluft je Person und Herkunft der Werte am Nutzungsprofil; nichts zu tun");
                return 0;
            }
            var zeilen = new List<string>();
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach ((string Spalte, string Typ) s in offen)
                    {
                        v.Ausfuehren(Anlegen(s));
                        zeilen.Add(TAB_PROFIL + "." + s.Spalte + " angelegt (leer)");
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
                bericht.Add("KEIN DML an Bestandsdaten; kein Referenzprojekt fuehrt Aussenluft je Person, der Referenzlauf bleibt byte-gleich");
            }
            return offen.Count;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
