using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KWN-0 - DIE ART EINER KAELTEBEDARFSZEILE: RAUMKUEHLUNG ODER PROZESSKAELTE
    // (Konzept Kuehlung 5.6.9 KWN-S1 teilweise; Entscheid E127, KWN-Q7).
    //
    // WAS.
    //   Z_Projekt_Kaeltebedarf  Kaelte_Art  (Kaeltebedarfsprofile)
    //   Z_ProjektWaermebedarf   Kaelte_Art  (externe Lastgaenge; wirksam nur fuer den Kanal Kuehlung)
    //
    // TEXT, Vorgabe 'RAUM', Pruefklausel IN ('RAUM','PROZESS'); NULL heisst ebenfalls RAUM. Die Prozesskaelte bleibt im
    // EINEN Kuehlkanal und wird in SimulationKaeltebedarf als Unterposten Kaeltebedarf_Prozess gefuehrt (kein fuenfter
    // Kanal). Die Bindung an einen Kaeltespeicher (ID_Pufferspeicher, 5.6.7) und die Kopplungsspalten an
    // Tab_Energieanlagen gehoeren zu spaeteren Wellen (KWN-2S, KWN-1).
    //
    // ERGEBNISNEUTRAL. KEIN DML, KEIN TABELLENNEUBAU: ADD COLUMN mit Vorgabe setzt jede Bestandszeile auf 'RAUM', und
    // ohne PROZESS-Zeile aendert sich keine Zahl. Der Schritt ist wiederholbar.
    //
    // DREI LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema und die Testvorrichtung in EPOS.Kern.Tests;
    // dazu Z_ProjektKaeltebedarfCtrl und WizardCtrl nach Vorhandensein.
    // ====================================================================================

    /// <summary>
    /// <b>KWN-0</b> — die Art einer Kältebedarfszeile (Raum oder Prozess) an beiden Zuordnungen. EINE Quelle für Migration,
    /// Werkzeug, Testkopie und Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class KaelteArtSchema
    {
        /// <summary><b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht (218).</summary>
        // Hängt an Schritt 217 (SA2, SchnellaufheizungSchema).
        public const int SCHRITT = SchnellaufheizungSchema.SCHRITT + 1;

        /// <summary>Die Zuordnung der Kältebedarfsprofile.</summary>
        public const string TAB_PROFIL = KaeltebedarfSchema.TAB_ZUORDNUNG;

        /// <summary>Die Zuordnung der externen Lastgänge.</summary>
        public const string TAB_LASTGANG = KaeltebedarfSchema.TAB_LASTGANG;

        /// <summary>Die Spalte der Art.</summary>
        public const string SPALTE = "Kaelte_Art";

        /// <summary>Raumkühlung — die Vorgabe.</summary>
        public const string RAUM = DbWerte.KAELTE_ART_RAUM;

        /// <summary>Prozesskälte.</summary>
        public const string PROZESS = DbWerte.KAELTE_ART_PROZESS;

        private static string Q(string s) => "\"" + s + "\"";

        /// <summary>Der Spaltentyp samt Vorgabe und Prüfklausel — gleich an beiden Tabellen.</summary>
        public static readonly string TYP =
            "TEXT DEFAULT '" + RAUM + "' CHECK (" + Q(SPALTE) + " IS NULL OR " + Q(SPALTE) + " IN ('" + RAUM + "','" +
            PROZESS + "'))";

        /// <summary>Die zwei Spalten in Tabellenfolge (Profil vor Lastgang).</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN = new[]
        {
            (TAB_PROFIL, SPALTE, TYP),
            (TAB_LASTGANG, SPALTE, TYP),
        };

        /// <summary>Ist der Wert Prozesskälte? Leer, NULL und jeder andere Wert heißen Raum.</summary>
        public static bool IstProzess(string wert) =>
            wert != null && string.Equals(wert.Trim(), PROZESS, StringComparison.OrdinalIgnoreCase);

        /// <summary>Der Persistenzwert einer gelesenen Zelle: <see cref="PROZESS"/> oder <see cref="RAUM"/>.</summary>
        public static string Normal(object wert) =>
            wert != null && wert != DBNull.Value && IstProzess(wert.ToString()) ? PROZESS : RAUM;

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { TAB_PROFIL, TAB_LASTGANG };

        /// <summary>Die Anweisung, die eine Spalte anlegt.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE " + Q(s.Tabelle) + " ADD COLUMN " + Q(s.Spalte) + " " + s.Typ;

        /// <summary>Steht die Spalte an der Profilzuordnung?</summary>
        public static bool ProfilLesbar() => DataRepository.SpalteVorhanden(TAB_PROFIL, SPALTE);

        /// <summary>Steht die Spalte an der Lastgangzuordnung?</summary>
        public static bool LastgangLesbar() => DataRepository.SpalteVorhanden(TAB_LASTGANG, SPALTE);

        /// <summary>Steht der Schritt — beide Spalten? (Wächter der Leser, etwa für einen älteren Stand auf iOS.)</summary>
        public static bool Vollstaendig() => SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte));

        /// <summary>Wie viele Zeilen beider Zuordnungen tragen Prozesskälte? (Nachweis: nach dem Schritt 0.)</summary>
        public static int ZeilenMitProzess()
        {
            int n = 0;
            foreach ((string Tabelle, string Spalte, string Typ) s in SPALTEN)
            {
                if (!DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)) continue;
                object o = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM " + Q(s.Tabelle) + " WHERE " + Q(s.Spalte) + " = ?", new DbParam("@art", PROZESS));
                if (o != null && o != DBNull.Value) n += Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
            return n;
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
                bericht?.Add("steht bereits - Art der Kaeltebedarfszeilen (Raum/Prozess); nichts zu tun");
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
                        zeilen.Add(s.Tabelle + "." + s.Spalte + " angelegt (Vorgabe " + RAUM + ")");
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
