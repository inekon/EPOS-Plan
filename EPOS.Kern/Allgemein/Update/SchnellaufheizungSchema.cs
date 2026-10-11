using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // SA2 - DIE SCHNELLAUFHEIZUNG DES VORHEIZENS: EINSTELLUNG UND ERGEBNISSE
    // (Entwurf Vorheizrampe Fassung 2; Entscheid E128, Kern SA1).
    //
    // WAS.
    //   Tab_Einstellungen     zwei Spalten: Schalter der Schnellaufheizung und Raumsollwert-Ueberhoehung Δθ
    //   Tab_ErgebnisGebaeude  sechs Ergebnisgroessen der Ueberhoehung (Vorheizgebaeude des Kerns)
    //   Tab_ErgebnisZone      dieselben sechs je Zone
    //
    // ALLE NULLBAR, mit Pruefklausel. NULL heisst Vorgabe: Schalter aus (Bestand), Ueberhoehung = Kern-Konstante
    // Vorheizvorgabe.UEBERHOEHUNG_VORGABE_K. Die Ergebnisspalten stehen auf NULL, solange die Schnellaufheizung an keiner Zone
    // wirkt (Schalter aus, Sollwertrampe oder keine Zone mit wirksamem Heizkreis und Heizkurve).
    //
    // ERGEBNISNEUTRAL. KEIN DML, KEIN TABELLENNEUBAU: Jede Bestandszeile steht danach auf NULL, und NULL rechnet bitgleich
    // den Bestand. Der Schritt ist wiederholbar.
    //
    // DREI LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema und die Testvorrichtung in EPOS.Kern.Tests;
    // dazu KonfigurationCtrl (Einstellung) und ErgebnisCtrl (Ergebnis) nach Vorhandensein.
    // ====================================================================================

    /// <summary>
    /// <b>SA2</b> — die Schnellaufheizung des Vorheizens (E128): Schalter und Überhöhung an <c>Tab_Einstellungen</c> und sechs
    /// Ergebnisspalten je Gebäude und je Zone. EINE Quelle für Migration, Werkzeug, Testkopie und Nachweis (ADR-001 Option C).
    /// Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class SchnellaufheizungSchema
    {
        /// <summary><b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht (217).</summary>
        // Hängt an Schritt 216 (KM4-E1, KaeltemittelSchema).
        public const int SCHRITT = KaeltemittelSchema.SCHRITT + 1;

        /// <summary>Die Projekteinstellungen.</summary>
        public const string TAB_EINSTELLUNGEN = VorheizenSchema.TAB_EINSTELLUNGEN;

        /// <summary>Das Ergebnis je Gebäude.</summary>
        public const string TAB_ERGEBNIS_GEBAEUDE = VorheizenSchema.TAB_ERGEBNIS_GEBAEUDE;

        /// <summary>Das Ergebnis je Zone.</summary>
        public const string TAB_ERGEBNIS_ZONE = VorheizenSchema.TAB_ERGEBNIS_ZONE;

        // ---- Einstellung (Tab_Einstellungen) ----

        /// <summary>Der Schalter der Schnellaufheizung (0/1, NULL = aus).</summary>
        public const string SPALTE_SCHALTER = "Aufheiz_Schnellaufheizung";

        /// <summary>Die Raumsollwert-Überhöhung Δθ [K] (&gt; 0 … 10, NULL = Kern-Vorgabe).</summary>
        public const string SPALTE_UEBERHOEHUNG = "Aufheiz_Ueberhoehung_K";

        /// <summary>Die größte Überhöhung [K] — dieselbe Grenze wie im Kern.</summary>
        public const double UEBERHOEHUNG_MAX_K = Vorheizvorgabe.UEBERHOEHUNG_MAX_K;

        // ---- Ergebnis (Tab_ErgebnisGebaeude, Tab_ErgebnisZone) ----

        /// <summary>Die wirksame Überhöhung [K]; am Gebäude die größte der Zonen.</summary>
        public const string SPALTE_ERG_UEBERHOEHUNG = "Vorheiz_Ueberhoehung_K";

        /// <summary>Die Stunden, in denen die Überhöhung den Vorlauf angehoben hat (am Gebäude Summe der Zonen).</summary>
        public const string SPALTE_ERG_STUNDEN = "Vorheiz_Ueberhoehung_H";

        /// <summary>Der höchste Vorlauf in diesen Stunden [°C]; am Gebäude der höchste der Zonen.</summary>
        public const string SPALTE_ERG_VORLAUF_MAX = "Vorheiz_Ueberhoehung_Vorlauf_Max_C";

        /// <summary>Die Stunden, in denen die Vorlaufgrenze der Anlage die Überhöhung begrenzt hat.</summary>
        public const string SPALTE_ERG_VORLAUFGRENZE = "Vorheiz_Vorlaufgrenze_H";

        /// <summary>Die Stunden mit wirkungsloser Überhöhung (Vorlauf schon an der Vorlaufgrenze).</summary>
        public const string SPALTE_ERG_WIRKUNGSLOS = "Vorheiz_Ueberhoehung_Wirkungslos_H";

        /// <summary>Die Fensterstunden mit Überhöhung in einer Sperrzeit.</summary>
        public const string SPALTE_ERG_SPERRZEIT = "Vorheiz_Ueberhoehung_Sperrzeit_H";

        private static string Q(string s) => "\"" + s + "\"";

        private static string Zahl(double d) => d.ToString(CultureInfo.InvariantCulture);

        private static string Ueberhoehung(string s)
            => "REAL CHECK (" + Q(s) + " IS NULL OR (" + Q(s) + " > 0 AND " + Q(s) + " <= " + Zahl(UEBERHOEHUNG_MAX_K) + "))";

        private static string GanzNichtNegativ(string s) => "INTEGER CHECK (" + Q(s) + " IS NULL OR " + Q(s) + " >= 0)";

        /// <summary>Die zwei Spalten der Projekteinstellung in Tabellenfolge — Name und Typ samt Klausel.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN_EINSTELLUNG = new[]
        {
            (TAB_EINSTELLUNGEN, SPALTE_SCHALTER,
                "INTEGER CHECK (" + Q(SPALTE_SCHALTER) + " IS NULL OR " + Q(SPALTE_SCHALTER) + " IN (0,1))"),
            (TAB_EINSTELLUNGEN, SPALTE_UEBERHOEHUNG, Ueberhoehung(SPALTE_UEBERHOEHUNG)),
        };

        /// <summary>
        /// Die sechs Ergebnisgrößen in Tabellenfolge — dieselben an Gebäude und Zone. Stunden am Gebäude sind Summen über die
        /// Zonen und deshalb nur nicht negativ; der Vorlauf ohne Bereich (eine Temperatur).
        /// </summary>
        public static readonly IReadOnlyList<(string Spalte, string Typ)> ERGEBNISSPALTEN = new[]
        {
            (SPALTE_ERG_UEBERHOEHUNG, Ueberhoehung(SPALTE_ERG_UEBERHOEHUNG)),
            (SPALTE_ERG_STUNDEN, GanzNichtNegativ(SPALTE_ERG_STUNDEN)),
            (SPALTE_ERG_VORLAUF_MAX, "REAL"),
            (SPALTE_ERG_VORLAUFGRENZE, GanzNichtNegativ(SPALTE_ERG_VORLAUFGRENZE)),
            (SPALTE_ERG_WIRKUNGSLOS, GanzNichtNegativ(SPALTE_ERG_WIRKUNGSLOS)),
            (SPALTE_ERG_SPERRZEIT, GanzNichtNegativ(SPALTE_ERG_SPERRZEIT)),
        };

        /// <summary>Die Ergebnisspalten an beiden Tabellen (Gebäude vor Zone).</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN_ERGEBNIS =
            ERGEBNISSPALTEN.Select(s => (TAB_ERGEBNIS_GEBAEUDE, s.Spalte, s.Typ))
                           .Concat(ERGEBNISSPALTEN.Select(s => (TAB_ERGEBNIS_ZONE, s.Spalte, s.Typ))).ToArray();

        /// <summary>Alle Spalten, die der Schritt an bestehende Tabellen anhängt.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN =
            SPALTEN_EINSTELLUNG.Concat(SPALTEN_ERGEBNIS).ToArray();

        /// <summary>Spalten von <c>Tab_ErgebnisGebaeude</c> nach dem Schritt (Vorgänger <see cref="VorheizenSchema"/>).</summary>
        public const int SPALTENZAHL_ERGEBNIS_GEBAEUDE = VorheizenSchema.SPALTENZAHL_ERGEBNIS_GEBAEUDE + 6;

        /// <summary>Spalten von <c>Tab_ErgebnisZone</c> nach dem Schritt (Vorgänger <see cref="VorheizenSchema"/>).</summary>
        public const int SPALTENZAHL_ERGEBNIS_ZONE = VorheizenSchema.SPALTENZAHL_ERGEBNIS_ZONE + 6;

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() =>
            new[] { TAB_EINSTELLUNGEN, TAB_ERGEBNIS_GEBAEUDE, TAB_ERGEBNIS_ZONE };

        /// <summary>Die Anweisung, die eine Spalte anlegt.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE " + Q(s.Tabelle) + " ADD COLUMN " + Q(s.Spalte) + " " + s.Typ;

        /// <summary>Steht der Schritt — alle Spalten? (Wächter der Leser, etwa für einen älteren Stand auf iOS.)</summary>
        public static bool Vollstaendig() => SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte));

        /// <summary>Wie viele Einstellungszeilen tragen den Schalter „an“? (Nachweis: nach dem Schritt 0.)</summary>
        public static int ZeilenMitSchalter()
        {
            if (!DataRepository.SpalteVorhanden(TAB_EINSTELLUNGEN, SPALTE_SCHALTER)) return 0;
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + Q(TAB_EINSTELLUNGEN) + " WHERE " + Q(SPALTE_SCHALTER) + " = 1");
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang aus (wiederholbar); Rückgabe = Zahl der angelegten Spalten. Fehlt eine
        /// vorausgesetzte Tabelle, wirft er benannt. <b>Kein DML.</b>
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen Verbindung.
            var offen = SPALTEN.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)).ToList();
            if (offen.Count == 0)
            {
                bericht?.Add("steht bereits - Schnellaufheizung (Einstellung, Ergebnis); nichts zu tun");
                return 0;
            }
            var zeilen = new List<string>();
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach ((string Tabelle, string Spalte, string Typ) s in offen)
                    {
                        v.Ausfuehren(Anlegen(s));
                        zeilen.Add(s.Tabelle + "." + s.Spalte + " angelegt (leer)");
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
                bericht.Add("KEIN DML an Bestandsdaten, der Referenzlauf bleibt byte-gleich");
            }
            return offen.Count;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
