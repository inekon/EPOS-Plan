using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // SCHRITT 240 - DIE VIER ERGEBNISSPALTEN DER WIRTSCHAFTLICHKEIT (Welle KB-8 "Vorsorgewege der Wirtschaftlichkeit").
    // Angemeldet als 240. VORLAEUFIGE KETTE: 239 ist von einer anderen Sitzung angemeldet und liegt noch nicht auf origin;
    // die Nummer steht deshalb ueber + 2 an 238 (KlimaRohwerteSchema). Bis zum Umhaengen ist der Schritt NICHT in der
    // Schrittliste der Migration, nicht im Register der Paketanhebung und nicht im Zielstand - beide Listen muessen
    // lueckenlos bleiben, und ein Lauf auf 240 vor 239 ueberginge 239 fuer immer (Marker "Nr <= Version").
    //
    // ANLASS. Tab_ErgebnisWirtschaftlichkeit fuehrt vier Spalten, die weder das Grundschema noch ein Schritt anlegte:
    // StromsteuerBefreiungModus (Etappe B6), ErsatzBarwert (W5-B-10), Nachweis_Json (B7P) und Lauf_Staende (EZ-19). Sie
    // entstanden zur Laufzeit ueber eine Spaltenvorsorge im Controller (ALTER TABLE beim Laden und Speichern). Dieser
    // Schritt legt sie an ihrer Stelle an; die Vorsorge im Controller entfaellt.
    //
    // WAS. Je fehlende Spalte ein ALTER TABLE ... ADD COLUMN mit genau der Typuebersetzung des bisherigen Laufzeitwegs
    // (StilleDb.AlterTableAddColumn aus der Access-Typangabe): TEXT(20) -> TEXT CHECK (length(...) <= 20), DOUBLE -> REAL,
    // LONGTEXT -> TEXT, TEXT -> TEXT; alle nullbar, ohne Vorgabe. Eine vorhandene Spalte bleibt unberuehrt - viele
    // Anwenderdatenbanken tragen die Spalten schon aus dem Laufzeitweg. Die Tabelle ist STRICT; ADD COLUMN haelt das.
    //
    // ERGEBNISNEUTRAL. Kein DML; Bestandszeilen bleiben NULL, wie nach dem Laufzeitweg. Wiederholbar.
    //
    // LESER: Werkzeuge/Testdatenbankschema und die Testvorrichtung in EPOS.Kern.Tests; nach dem Umhaengen dazu
    // SchemaMigration (Schale) und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>Schemaschritt der Ergebnisspalten der Wirtschaftlichkeit</b> (KB-8): <c>StromsteuerBefreiungModus</c>,
    /// <c>ErsatzBarwert</c>, <c>Nachweis_Json</c> und <c>Lauf_Staende</c> an <c>Tab_ErgebnisWirtschaftlichkeit</c>, jede
    /// nur, wenn sie fehlt. Wiederholbar, ergebnisneutral, kein DML.
    /// </summary>
    public static class ErgebnisWirtschaftlichkeitSpaltenSchema
    {
        /// <summary>Die Nummer des Schritts (240); die Nummer steht allein hier.</summary>
        public const int SCHRITT = ErgebnisreihenSchema.SCHRITT + 1;

        /// <summary>Die Tabelle des Schritts.</summary>
        public const string TABELLE = WirtschaftlichkeitCtrl.TAB_ERGEBNIS;

        /// <summary>Eine Spalte des Schritts: Name und die Access-Typangabe des bisherigen Laufzeitwegs.</summary>
        public sealed class Spalte
        {
            /// <summary>Spaltenname.</summary>
            public string Name { get; }

            /// <summary>Die Access-Typangabe, aus der <see cref="StilleDb.SqliteSpaltenTyp"/> den SQLite-Typ übersetzt.</summary>
            public string AccessTyp { get; }

            /// <summary>Der SQLite-Typ samt Nebenbedingung, wie ihn der Laufzeitweg angelegt hat.</summary>
            public string Typ => StilleDb.SqliteSpaltenTyp(Name, AccessTyp);

            /// <summary>Legt die Beschreibung an.</summary>
            public Spalte(string name, string accessTyp)
            {
                Name = name;
                AccessTyp = accessTyp;
            }
        }

        /// <summary>Die vier Spalten in Anlegereihenfolge.</summary>
        public static readonly IReadOnlyList<Spalte> SPALTEN = new[]
        {
            new Spalte(WirtschaftlichkeitCtrl.SPALTE_STROMST_MODUS, "TEXT(20)"),   // Etappe B6
            new Spalte(WirtschaftlichkeitCtrl.SPALTE_ERSATZ_BARWERT, "DOUBLE"),    // W5-B-10
            new Spalte(WirtschaftlichkeitCtrl.SPALTE_NACHWEIS_JSON, "LONGTEXT"),   // B7P
            new Spalte(WirtschaftlichkeitCtrl.SPALTE_LAUF_STAENDE, "TEXT"),        // EZ-19
        };

        /// <summary>Die ADD-COLUMN-Anweisung einer Spalte — dieselbe Schreibweise wie der bisherige Laufzeitweg.</summary>
        public static string Anlegen(Spalte s) => StilleDb.AlterTableAddColumn(TABELLE, s.Name, s.AccessTyp);

        /// <summary>Die Tabellen, die ein früherer Schritt (das Grundschema) angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { TABELLE };

        /// <summary>Was noch offen ist: je fehlende Spalte 1 (−1: die Tabelle fehlt).</summary>
        public static int Offen()
        {
            if (!DataRepository.TabelleVorhanden(TABELLE)) return -1;
            return SPALTEN.Count(s => !DataRepository.SpalteVorhanden(TABELLE, s.Name));
        }

        /// <summary>Steht der Schritt?</summary>
        public static bool Vollstaendig() => Offen() == 0;

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang aus (wiederholbar); Rückgabe = Zahl der angelegten Spalten. Fehlt die
        /// Tabelle, wirft er benannt.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            List<Spalte> offen = SPALTEN.Where(s => !DataRepository.SpalteVorhanden(TABELLE, s.Name)).ToList();
            if (offen.Count == 0)
            {
                bericht?.Add("steht bereits - die vier Ergebnisspalten an " + TABELLE + "; nichts zu tun");
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
            if (bericht != null)
            {
                foreach (Spalte s in offen) bericht.Add(TABELLE + "." + s.Name + " angelegt (" + s.Typ + ", leer)");
                bericht.Add("KEIN DML an Bestandsdaten - Bestandszeilen bleiben NULL, der Referenzlauf bleibt gleich");
            }
            return offen.Count;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
