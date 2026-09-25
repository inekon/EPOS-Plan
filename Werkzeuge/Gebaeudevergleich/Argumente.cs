using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;

namespace Gebaeudevergleich
{
    /// <summary>
    /// Die Kommandozeile: der Unterbefehl vorn, danach Schalter mit zwei Bindestrichen.
    /// Jeder Unterbefehl nimmt nur seine eigenen Schalter; ein fremder oder unbekannter
    /// Schalter ist ein Abbruch mit Grund, nie ein still übergangenes Wort.
    /// </summary>
    internal sealed class Argumente
    {
        internal const string AUFNAHME = "aufnahme";
        internal const string VERGLEICH = "vergleich";
        internal const string VARIANTE = "variante";

        /// <summary>Der Dateiname der Momentaufnahme im Zielordner von <c>aufnahme</c>.</summary>
        internal const string AUFNAHMEDATEI = "aufnahme.sqlite";

        /// <summary>Der Dateiname des Protokolls im Zielordner.</summary>
        internal const string PROTOKOLLDATEI = "protokoll.txt";

        internal string Befehl { get; private set; }
        internal string Quelle { get; private set; }
        internal string Db { get; private set; }
        internal string Ziel { get; private set; }
        internal string Modell { get; private set; }

        /// <summary>Die gewählten Projekte; <c>null</c> = alle.</summary>
        internal IReadOnlyList<int> Projekte { get; private set; }

        internal bool MitNamen { get; private set; }
        internal bool Stundenreihen { get; private set; }

        /// <summary>Der Grund, warum die Zeile nicht taugt; <c>null</c> = in Ordnung.</summary>
        internal string Fehler { get; private set; }

        /// <summary>
        /// Das Protokoll: bei <c>aufnahme</c> und <c>vergleich</c> im Zielordner, bei
        /// <c>variante</c> (Ziel ist eine Datei) daneben als <c>&lt;ziel&gt;.protokoll.txt</c>.
        /// </summary>
        internal string Protokolldatei
            => Befehl == VARIANTE ? Ziel + ".protokoll.txt" : Path.Combine(Ziel, PROTOKOLLDATEI);

        /// <summary>Der Ordner, in den das Werkzeug schreibt.</summary>
        internal string Zielordner
            => Befehl == VARIANTE ? Path.GetDirectoryName(Ziel) : Ziel;

        internal static void HilfeAusgeben(TextWriter aus)
        {
            aus.WriteLine("Gebaeudevergleich — Tagesbilanz-Weg (alt) gegen VDI-6007-Weg (neu) je Gebäude");
            aus.WriteLine("an einer Momentaufnahme der Arbeitsdatenbank (Ablösekriterium Q24/E27, Bedingung 1).");
            aus.WriteLine();
            aus.WriteLine("Aufruf:");
            aus.WriteLine("  Gebaeudevergleich aufnahme  --quelle <sqlite> --ziel <ordner>");
            aus.WriteLine("  Gebaeudevergleich vergleich --db <sqlite> --ziel <ordner> [--projekte a,b]");
            aus.WriteLine("                              [--mit-namen] [--stundenreihen]");
            aus.WriteLine("  Gebaeudevergleich variante  --db <sqlite> --modell TAGESBILANZ|VDI6007 --ziel <datei>");
            aus.WriteLine();
            aus.WriteLine("aufnahme   Zieht eine Momentaufnahme der Quelle nach <ordner>/" + AUFNAHMEDATEI + ":");
            aus.WriteLine("           immutable-Verbindung, VACUUM INTO, SHA-256 der Quelle davor und danach,");
            aus.WriteLine("           Bestand der Nebendateien (-wal, -shm, -journal). Die Quelle bleibt");
            aus.WriteLine("           byte-gleich; neben ihr entsteht keine Datei. Eine Quelle unter");
            aus.WriteLine("           %ProgramData%\\EPOS_PLAN wird nur aufgenommen, wenn EPOS_Plan nicht läuft.");
            aus.WriteLine("vergleich  Rechnet jedes Gebäude der --db einmal auf jedem Weg und schreibt nach");
            aus.WriteLine("           <ordner>: gebaeude.csv, gebaeude_monate.csv, projekte.csv, bericht.html,");
            aus.WriteLine("           zusammenfassung.md, protokoll.txt (mit --stundenreihen dazu stunden/).");
            aus.WriteLine("           Ohne --mit-namen steht in keiner Ausgabe ein Projekt-, Kunden-,");
            aus.WriteLine("           Bearbeiter- oder Gebäudename, nur die ID.");
            aus.WriteLine("variante   Kopiert die --db nach <datei> (VACUUM INTO) und setzt dort an jedem");
            aus.WriteLine("           Gebäude den Rechenweg (Gebaeude_Modell) - für die Projektwirkung über");
            aus.WriteLine("           EPOS.Referenzlauf. Die --db bleibt unverändert.");
            aus.WriteLine();
            aus.WriteLine("Schreibort: Kein --ziel im Repository (Wurzel mit WP-Plan.sln) und keines unter");
            aus.WriteLine("%ProgramData%\\EPOS_PLAN; --db nie unter %ProgramData%\\EPOS_PLAN.");
            aus.WriteLine();
            aus.WriteLine("Schemastand: vergleich und variante verlangen den Zielstand dieses Werkzeugs");
            aus.WriteLine("(SchemaStand.Zielversion = " + SchemaStand.Zielversion.ToString(CultureInfo.InvariantCulture) + "). Migrationsweg:");
            aus.WriteLine("  Regelweg  EPOS-Plan in derselben Fassung starten; es migriert die Arbeitsdatenbank");
            aus.WriteLine("            mit eigener Sicherung. Danach die Aufnahme wiederholen.");
            aus.WriteLine("  Ersatzweg (nur wenn die Programm-Migration scheitert) - die KOPIE über den");
            aus.WriteLine("            Windows-Referenzlauf migrieren, siehe Werkzeuge/Gebaeudevergleich/LIESMICH.md:");
            aus.WriteLine("            Referenzlauf.exe lauf --quelle <aufnahme.sqlite> --projekte 999999 --ziel <ordner>");
            aus.WriteLine("            (Konsole in eine Datei), dann aufnahme --quelle");
            aus.WriteLine("            <worktree>/Referenzlaeufe/Arbeitskopie/Kenndaten.sqlite und die Arbeitskopie leeren.");
            aus.WriteLine();
            aus.WriteLine("Rückgabe: 0 = keine rote Zeile, 1 = mindestens eine rote Zeile, 2 = Abbruch.");
        }

        internal static Argumente Lesen(string[] args)
        {
            var a = new Argumente();
            if (args == null || args.Length == 0) return a.Mit("Kein Unterbefehl.");

            a.Befehl = args[0];
            if (a.Befehl != AUFNAHME && a.Befehl != VERGLEICH && a.Befehl != VARIANTE)
                return a.Mit("Unbekannter Unterbefehl: " + args[0] + " (aufnahme, vergleich, variante).");

            HashSet<string> erlaubt = a.Befehl switch
            {
                AUFNAHME => new HashSet<string> { "--quelle", "--ziel" },
                VERGLEICH => new HashSet<string> { "--db", "--ziel", "--projekte", "--mit-namen", "--stundenreihen" },
                _ => new HashSet<string> { "--db", "--modell", "--ziel" },
            };

            for (int i = 1; i < args.Length; i++)
            {
                string s = args[i];
                if (!erlaubt.Contains(s))
                    return a.Mit(s.StartsWith("--", StringComparison.Ordinal)
                        ? "Der Schalter " + s + " gehört nicht zu '" + a.Befehl + "'."
                        : "Unerwartetes Wort: " + s);

                switch (s)
                {
                    case "--mit-namen": a.MitNamen = true; continue;
                    case "--stundenreihen": a.Stundenreihen = true; continue;
                }

                if (++i >= args.Length) return a.Mit(s + " braucht einen Wert.");
                string wert = args[i];
                switch (s)
                {
                    case "--quelle": a.Quelle = Path.GetFullPath(wert); break;
                    case "--db": a.Db = Path.GetFullPath(wert); break;
                    case "--ziel": a.Ziel = Path.GetFullPath(wert); break;
                    case "--modell": a.Modell = wert; break;
                    case "--projekte":
                        string grund = a.ProjekteLesen(wert);
                        if (grund != null) return a.Mit(grund);
                        break;
                }
            }

            if (a.Ziel == null) return a.Mit("--ziel fehlt.");
            switch (a.Befehl)
            {
                case AUFNAHME:
                    if (a.Quelle == null) return a.Mit("--quelle fehlt.");
                    return a;
                case VERGLEICH:
                    if (a.Db == null) return a.Mit("--db fehlt.");
                    return a;
                default:
                    if (a.Db == null) return a.Mit("--db fehlt.");
                    if (a.Modell != DbWerte.GEBAEUDE_MODELL_TAGESBILANZ && a.Modell != DbWerte.GEBAEUDE_MODELL_VDI6007)
                        return a.Mit("--modell kennt nur " + DbWerte.GEBAEUDE_MODELL_TAGESBILANZ + " und " +
                                     DbWerte.GEBAEUDE_MODELL_VDI6007 + ".");
                    return a;
            }
        }

        /// <summary>
        /// Gibt es die Eingabedatei, und ist sie kein Git-LFS-Zeiger? Rückgabe <c>null</c> = ja.
        /// Bewusst ERST NACH dem Schreibort gerufen: Eine <c>--db</c> unter
        /// <c>%ProgramData%\EPOS_PLAN</c> wird verweigert, ohne dass die Datei auch nur geöffnet wird.
        /// </summary>
        internal string DateienPruefen()
            => Befehl == AUFNAHME ? DateiPruefen(Quelle, "Quelle") : DateiPruefen(Db, "Datenbank");

        private string ProjekteLesen(string wert)
        {
            var liste = new List<int>();
            foreach (string teil in wert.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!int.TryParse(teil.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int id) || id <= 0)
                    return "--projekte erwartet positive Projekt-IDs, getrennt mit Komma: " + teil;
                if (!liste.Contains(id)) liste.Add(id);
            }
            if (liste.Count == 0) return "--projekte nennt kein Projekt.";
            Projekte = liste.OrderBy(x => x).ToList();
            return null;
        }

        private static string DateiPruefen(string pfad, string was)
        {
            if (!File.Exists(pfad)) return was + " nicht gefunden: " + pfad;
            if (IstLfsZeiger(pfad))
                return was + " ist ein Git-LFS-Zeiger, keine Datenbank: " + pfad +
                       " - einmal je Rechner \"git lfs install\", dann \"git lfs checkout\".";
            return null;
        }

        /// <summary>Die erste Zeile jeder Git-LFS-Zeigerdatei (Spezifikation v1).</summary>
        private const string LFS_KENNUNG = "version https://git-lfs";

        private static bool IstLfsZeiger(string pfad)
        {
            try
            {
                using var s = new FileStream(pfad, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                byte[] puffer = new byte[LFS_KENNUNG.Length];
                int gelesen = s.Read(puffer, 0, puffer.Length);
                return gelesen == puffer.Length && System.Text.Encoding.ASCII.GetString(puffer) == LFS_KENNUNG;
            }
            catch (IOException)
            {
                return false;
            }
        }

        private Argumente Mit(string fehler) { Fehler = fehler; return this; }
    }
}
