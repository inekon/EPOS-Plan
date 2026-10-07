using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KP3 WELLE A - VERWENDETER AUFSCHLAG UND BEMESSENE AUFHEIZZEIT IM ERGEBNIS (Schritt 194,
    // Entscheid E99 vom 06.10.2026; Entwurf KP3 Festlegungen 35-39, F9; Teilkonzept
    // Konditionierungsprofile 9.8/9.9).
    //
    // WOZU. Der Bericht nannte den Aufschlag bisher als Projekteinstellung - also den Wert, der HEUTE
    // eingestellt ist, nicht den, mit dem der gespeicherte Lauf gerechnet hat. Die Ergebniszeile des
    // Gebaeudes nimmt deshalb zwei Werte des Laufs auf:
    //
    //   Tab_ErgebnisGebaeude  Aufheiz_Aufschlag_Verwendet_H  INTEGER 0..47  [h]  NULL = kein Aufschlag dieser Art
    //                         Aufheizzeit_Bemessen_H         INTEGER 0..47  [h]  NULL = keine bemessene Zeit
    //
    //   Aufheiz_Aufschlag_Verwendet_H: die Stunden, die der Aufschlag aus Aufheiz_Aufschlag_H und
    //     Aufheiz_Aufschlag_Prozent (es gilt das Groessere, Festlegung 35) der LAENGSTEN Rampe tatsaechlich
    //     hinzugefuegt hat, n' - n nach der Begrenzung auf D + 1; bei mehreren gleich langen Rampen der
    //     groesste; 0 bei "taeglich" und "fest" ohne Rampe mit mehr als einer Stufe oder ohne Aufschlag;
    //     NULL bei "manuell" (der Aufschlag wirkt nie auf den manuellen Wert), bei GEKOPPELT, UNBEHEIZT und
    //     mit Schalter aus. Im Mehrzonengebaeude der Wert der Zone mit der laengsten Rampe.
    //   Aufheizzeit_Bemessen_H: die Zeit, mit der das Gebaeude gerechnet wurde - bei "manuell" der manuelle
    //     Wert, sonst t_auf,max nach dem Aufschlag (n' - 1 mit n = t_auf,max + 1, Deckel 48, ohne D);
    //     NULL bei UNERREICHBAR (wie Aufheizzeit_Max_H), GEKOPPELT, UNBEHEIZT und mit Schalter aus. Im
    //     Mehrzonengebaeude das Groesste seiner Zonen.
    //
    // NUR AM GEBAEUDE. Tab_ErgebnisZone bekommt keine Spalte: Die Zonen erben Art und manuelle Zeit vom
    // Gebaeude, der Bericht liest nur die Gebaeudezeile (E99: zwei Ergebnisspalten). KEIN EXPORT (E99):
    // Die Zahlen des Ergebnisexports bleiben, wie sie sind.
    //
    // KEIN DML, KEIN NEUBAU, KEIN SICHTNEUBAU. Reines ADD COLUMN, nullbar, mit Pruefklausel; jede
    // Bestandszeile steht danach auf NULL - der Bericht nennt fuer sie den Aufschlag benannt als
    // Projekteinstellung. Der Referenzlauf liest Tab_ErgebnisGebaeude nicht; er bleibt byte-gleich. Alles
    // in EINEM Vorgang; der Schritt ist wiederholbar.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl); dazu ErgebnisCtrl (Spalten nach Vorhandensein).
    // ====================================================================================

    /// <summary>
    /// <b>KP3 Welle A</b> — verwendeter Aufschlag und bemessene Aufheizzeit in der Ergebniszeile des Gebäudes (E99):
    /// EINE Quelle für Migration, Werkzeug, Testkopie und Nachweis (ADR-001 Option C). Anlass und Bauform stehen im
    /// Kopf der Datei.
    /// </summary>
    public static class AufheizAufschlagErgebnisSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht (angemeldet als 194).
        /// </summary>
        public const int SCHRITT = StandardlastprofilSchema.SCHRITT + 1;

        /// <summary>Die Ergebnistabelle der Gebäude.</summary>
        public const string TAB_ERGEBNIS_GEBAEUDE = AufheizManuellSchema.TAB_ERGEBNIS_GEBAEUDE;

        /// <summary><c>Aufheiz_Aufschlag_Verwendet_H</c> [h]: der Aufschlag der längsten Rampe; NULL bei „manuell" und ohne Planung.</summary>
        public const string SPALTE_AUFSCHLAG_VERWENDET = "Aufheiz_Aufschlag_Verwendet_H";

        /// <summary><c>Aufheizzeit_Bemessen_H</c> [h]: t_auf,max nach dem Aufschlag bzw. der manuelle Wert; NULL ohne bemessene Zeit.</summary>
        public const string SPALTE_ZEIT_BEMESSEN = "Aufheizzeit_Bemessen_H";

        /// <summary>Der größte Wert beider Spalten [h] — n' ≤ 48.</summary>
        public const int MAX_H = AufheizManuellSchema.MANUELL_MAX_H;

        /// <summary>Spaltenzahl von <c>Tab_ErgebnisGebaeude</c> nach diesem Schritt (43 + 2).</summary>
        public const int SPALTENZAHL_ERGEBNIS_GEBAEUDE = AufheizManuellSchema.SPALTENZAHL_ERGEBNIS_GEBAEUDE + 2;

        /// <summary>Die Spalten des Schritts in Anlagereihenfolge: Tabelle, Spalte, Typ samt Klausel.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN = new[]
        {
            (TAB_ERGEBNIS_GEBAEUDE, SPALTE_AUFSCHLAG_VERWENDET, "INTEGER " + Pruefung(SPALTE_AUFSCHLAG_VERWENDET)),
            (TAB_ERGEBNIS_GEBAEUDE, SPALTE_ZEIT_BEMESSEN, "INTEGER " + Pruefung(SPALTE_ZEIT_BEMESSEN)),
        };

        private static string Pruefung(string spalte)
            => "CHECK (\"" + spalte + "\" IS NULL OR \"" + spalte + "\" BETWEEN 0 AND " +
               MAX_H.ToString(CultureInfo.InvariantCulture) + ")";

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { TAB_ERGEBNIS_GEBAEUDE };

        /// <summary>Die Anweisung, die eine Spalte anlegt.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE \"" + s.Tabelle + "\" ADD COLUMN \"" + s.Spalte + "\" " + s.Typ;

        /// <summary>Stehen beide Spalten?</summary>
        public static bool Vollstaendig() => SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte));

        /// <summary>
        /// Führt den Schritt aus (wiederholbar); Rückgabe = Zahl der angelegten Spalten. Ohne
        /// <c>Tab_ErgebnisGebaeude</c> wirft er benannt.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            var offen = SPALTEN.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)).ToList();
            if (offen.Count == 0)
            {
                bericht?.Add("steht bereits - verwendeter Aufschlag und bemessene Aufheizzeit an Tab_ErgebnisGebaeude; nichts zu tun");
                return 0;
            }

            int angelegt = 0;
            var zeilen = new List<string>();
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    foreach ((string Tabelle, string Spalte, string Typ) s in offen)
                    {
                        v.Ausfuehren(Anlegen(s));
                        zeilen.Add(s.Tabelle + "." + s.Spalte + " angelegt (leer)");
                        angelegt++;
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
                foreach (string z in zeilen) bericht.Add(z);
            bericht?.Add("KEIN DML an Bestandsdaten, der Referenzlauf bleibt byte-gleich");
            return angelegt;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
