using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // VW1a - AUSWEIS DER VORLAUFWAHL DER WAERMEPUMPE (Schritt 188; Entscheid E88).
    //
    // WOZU. Unter Anlagenkopplung waehlt die Waermepumpe je Stunde die Kennlinienstuetzstelle am gerechneten
    // Heizkreisvorlauf. Die Zaehler dieser Wahl (Stunden je Stuetzstelle, darueber, darunter) stehen in der
    // Protokollmeldung; der Schritt legt sie zusaetzlich als drei Spalten an der Modulzeile des Ergebnisses an:
    //
    //   Tab_ErgebnisWaermepumpeModul  Vorlaufwahl_Stunden        TEXT     "Vorlauf:Stunden;..." aufsteigend
    //                                 Vorlauf_Darueber_Stunden   INTEGER  0..8760
    //                                 Vorlauf_Darunter_Stunden   INTEGER  0..8760
    //
    // NULL = keine Kennlinienwahl am Vorlauf (keine Kopplung, reines Warmwassermodul).
    //
    // KEIN DML, KEIN SICHTNEUBAU, KEINE SAAT. Kein Rechenwert aendert sich. Alles in EINEM Vorgang; der
    // Schritt ist wiederholbar.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>VW1a</b> — Ausweis der Vorlaufwahl der Wärmepumpe: Stunden je Kennlinienstützstelle sowie über der
    /// obersten und unter der untersten Stützstelle an der Modulzeile des Ergebnisses. EINE Quelle für
    /// Migration, Werkzeug, Testkopie und Nachweis (ADR-001 Option C).
    /// </summary>
    public static class VorlaufwahlSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht (angemeldet als 188).
        /// </summary>
        public const int SCHRITT = FreieKuehlungSoleSchema.SCHRITT + 1;

        /// <summary>Die Ergebnistabelle je Wärmepumpe (Modulzeile).</summary>
        public const string TAB_ERGEBNIS_WP_MODUL = KuehlungSchema.TAB_ERGEBNIS_WP_MODUL;

        /// <summary>Die Stunden je Stützstelle als Text „Vorlauf:Stunden“ aufsteigend, mit „;“ getrennt; NULL = keine Wahl.</summary>
        public const string SPALTE_VORLAUFWAHL_STUNDEN = "Vorlaufwahl_Stunden";

        /// <summary>Die Stunden über der obersten Stützstelle [h], 0 … 8760; NULL = keine Wahl.</summary>
        public const string SPALTE_DARUEBER_STUNDEN = "Vorlauf_Darueber_Stunden";

        /// <summary>Die Stunden unter der untersten Stützstelle [h], 0 … 8760; NULL = keine Wahl.</summary>
        public const string SPALTE_DARUNTER_STUNDEN = "Vorlauf_Darunter_Stunden";

        /// <summary>Die drei Spalten an der Modulzeile, in Anlagereihenfolge.</summary>
        public static readonly IReadOnlyList<string> SPALTEN_ERGEBNIS =
            new[] { SPALTE_VORLAUFWAHL_STUNDEN, SPALTE_DARUEBER_STUNDEN, SPALTE_DARUNTER_STUNDEN };

        private static string Q(string s) => "\"" + s + "\"";

        private static string Stunden(string s) => "INTEGER CHECK (" + Q(s) + " IS NULL OR " + Q(s) + " BETWEEN 0 AND 8760)";

        /// <summary>Die Spalten des Schritts in Anlagereihenfolge: Tabelle, Spalte, Typ samt Klausel.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN = new[]
        {
            (TAB_ERGEBNIS_WP_MODUL, SPALTE_VORLAUFWAHL_STUNDEN, "TEXT"),
            (TAB_ERGEBNIS_WP_MODUL, SPALTE_DARUEBER_STUNDEN, Stunden(SPALTE_DARUEBER_STUNDEN)),
            (TAB_ERGEBNIS_WP_MODUL, SPALTE_DARUNTER_STUNDEN, Stunden(SPALTE_DARUNTER_STUNDEN)),
        };

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { TAB_ERGEBNIS_WP_MODUL };

        /// <summary>Die Anweisung, die eine Spalte anlegt.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE " + Q(s.Tabelle) + " ADD COLUMN " + Q(s.Spalte) + " " + s.Typ;

        /// <summary>Stehen alle drei Spalten?</summary>
        public static bool Vollstaendig() => SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte));

        /// <summary>Stehen die drei Spalten an der Modulzeile? (Wächter des Ergebnisschreibers, etwa für einen älteren Stand auf iOS.)</summary>
        public static bool ErgebnisspaltenVorhanden() => Vollstaendig();

        /// <summary>
        /// Baut den Spaltentext aus den Stunden je Stützstelle: „Vorlauf:Stunden“ aufsteigend nach Vorlauf, mit „;“
        /// getrennt, Vorlauf ganzzahlig gerundet in °C. Leere Liste ergibt NULL.
        /// </summary>
        public static string StundenText(IEnumerable<KeyValuePair<double, int>> stundenJeStuetzstelle)
        {
            if (stundenJeStuetzstelle == null) return null;
            var paare = stundenJeStuetzstelle.OrderBy(p => p.Key).ToList();
            if (paare.Count == 0) return null;
            return string.Join(";", paare.Select(p =>
                ((long)Math.Round(p.Key, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture) + ":" +
                p.Value.ToString(CultureInfo.InvariantCulture)));
        }

        /// <summary>
        /// Liest den Spaltentext zurück in Paare (Vorlauf °C, Stunden), in der Reihenfolge des Textes. NULL oder leer
        /// ergibt eine leere Liste; ein fehlerhaftes Paar wirft benannt.
        /// </summary>
        public static IReadOnlyList<KeyValuePair<int, int>> StundenLesen(string text)
        {
            var liste = new List<KeyValuePair<int, int>>();
            if (string.IsNullOrWhiteSpace(text)) return liste;
            foreach (string teil in text.Split(';'))
            {
                string[] p = teil.Split(':');
                if (p.Length != 2
                    || !int.TryParse(p[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int vorlauf)
                    || !int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int stunden))
                    throw new FormatException("Vorlaufwahl: Das Paar '" + teil + "' ist nicht von der Form Vorlauf:Stunden.");
                liste.Add(new KeyValuePair<int, int>(vorlauf, stunden));
            }
            return liste;
        }

        /// <summary>
        /// Führt den Schritt aus (wiederholbar); Rückgabe = Zahl der angelegten Spalten. Fehlt die Tabelle, wirft
        /// er benannt.
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
                bericht?.Add("steht bereits - Vorlaufwahl_Stunden, Vorlauf_Darueber_Stunden und " +
                             "Vorlauf_Darunter_Stunden an Tab_ErgebnisWaermepumpeModul; nichts zu tun");
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
