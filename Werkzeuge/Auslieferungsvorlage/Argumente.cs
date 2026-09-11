using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Auslieferungsvorlage
{
    /// <summary>
    /// Die Kommandozeile — bewusst in derselben Form wie
    /// <c>Werkzeuge/Testdatenbankschema</c>: Stellungsargumente vorn, Schalter mit
    /// zwei Bindestrichen hinten, <c>--trocken</c> als Probelauf.
    ///
    /// <para><b>Diese Form ist verabredet.</b> <c>Setup/build-setup.ps1</c> und
    /// <c>Setup/EPOS-Plan.iss</c> werden gegen genau sie verdrahtet (Auftrag #162);
    /// wer sie aendert, bricht die Freigabekette.</para>
    /// </summary>
    internal sealed class Argumente
    {
        internal string Quelle { get; private set; }
        internal string Ziel { get; private set; }
        internal List<string> Beispiele { get; } = new List<string>();
        internal bool Trocken { get; private set; }

        /// <summary>
        /// Vorgabe <c>true</c> seit Anwenderentscheid <b>#160‑E‑1a</b> (11.09.2026,
        /// „a"): Der Katalog wird vollstaendig ausgeliefert, ohne dass <c>--kataloge
        /// alle</c> bei jedem Lauf eigens angegeben werden muesste. Der Schalter
        /// <c>--kataloge readonly</c> bleibt ausdruecklich waehlbar und setzt diesen
        /// Wert auf <c>false</c> — Weg b („Marke nachpflegen") wurde verworfen.
        /// </summary>
        internal bool KatalogeVollstaendig { get; private set; } = true;

        internal bool KatalogleerungZulassen { get; private set; }

        /// <summary>Der Grund, warum die Zeile nicht taugt; <c>null</c> = in Ordnung.</summary>
        internal string Fehler { get; private set; }

        internal static void HilfeAusgeben()
        {
            Console.WriteLine("Auslieferungsvorlage — erzeugt aus einer produktiven Kenndaten.sqlite die");
            Console.WriteLine("bereinigte Auslieferungsdatenbank samt Beispielprojekten.");
            Console.WriteLine();
            Console.WriteLine("Aufruf:");
            Console.WriteLine("  dotnet run --project Werkzeuge/Auslieferungsvorlage -c Release -- \\");
            Console.WriteLine("      <quelle.sqlite> <ziel.sqlite> [--beispiele <ordner-oder-liste>] [--trocken]");
            Console.WriteLine();
            Console.WriteLine("  <quelle.sqlite>   Die produktive Datenbank. Wird NIE veraendert; es wird eine");
            Console.WriteLine("                    Arbeitskopie ueber VACUUM INTO gezogen.");
            Console.WriteLine("  <ziel.sqlite>     Die zu erzeugende Vorlage. Muss ausserhalb des Repositorys");
            Console.WriteLine("                    liegen oder unter Setup/Vorlage/.");
            Console.WriteLine();
            Console.WriteLine("  --beispiele <x>   Projektpakete (.wpx), die nach der Bereinigung eingespielt");
            Console.WriteLine("                    werden. <x> ist ein ORDNER (alle *.wpx darin, nach Namen) oder");
            Console.WriteLine("                    eine mit ',' oder ';' getrennte Liste von Dateien.");
            Console.WriteLine("  --trocken         Alles rechnen und berichten, nichts schreiben: die Zieldatei");
            Console.WriteLine("                    entsteht nicht, der Bericht geht nur auf die Konsole.");
            Console.WriteLine("  --kataloge alle   Jede Katalogzeile behalten. VORGABE seit Anwenderentscheid");
            Console.WriteLine("                    #160-E-1a (11.09.2026) — ohne diesen Schalter geschieht");
            Console.WriteLine("                    dasselbe.");
            Console.WriteLine("  --kataloge readonly");
            Console.WriteLine("                    Nur behalten, was in *_STAMM ReadOnly=TRUE traegt");
            Console.WriteLine("                    (Setup-Konzept 6.1, Schritt 3) — ausdruecklich zu waehlen;");
            Console.WriteLine("                    bricht mit Code 4 ab, wenn das eine Katalogtabelle leert.");
            Console.WriteLine("  --katalogleerung-zulassen");
            Console.WriteLine("                    Nur mit --kataloge readonly wirksam: nicht abbrechen, wenn");
            Console.WriteLine("                    die ReadOnly-Regel eine Katalogtabelle vollstaendig leert.");
            Console.WriteLine();
            Console.WriteLine("Rueckgabe:");
            Console.WriteLine("  0  Vorlage erzeugt und abgenommen.");
            Console.WriteLine("  2  Aufruf falsch oder Quelle nicht lesbar.");
            Console.WriteLine("  3  Das Ziel liegt im Repository ausserhalb von Setup/Vorlage/.");
            Console.WriteLine("  4  Nur bei --kataloge readonly: Die ReadOnly-Regel wuerde eine Katalogtabelle");
            Console.WriteLine("     leeren.");
            Console.WriteLine("  5  Fachlicher Abbruch (Beispielimport oder Abnahme fehlgeschlagen).");
            Console.WriteLine("  1  Unerwarteter Fehler; die Ausnahme steht auf stderr.");
            Console.WriteLine();
            Console.WriteLine("Jeder Abbruch ungleich 0 nennt seinen Grund auf stderr.");
        }

        internal static Argumente Lesen(string[] args)
        {
            var a = new Argumente();
            var frei = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--trocken":
                        a.Trocken = true;
                        break;
                    case "--katalogleerung-zulassen":
                        a.KatalogleerungZulassen = true;
                        break;
                    case "--kataloge":
                        if (++i >= args.Length) return a.Mit("--kataloge braucht einen Wert (readonly oder alle).");
                        if (args[i] == "alle") a.KatalogeVollstaendig = true;
                        else if (args[i] == "readonly") a.KatalogeVollstaendig = false;
                        else return a.Mit("--kataloge kennt nur 'readonly' und 'alle'.");
                        break;
                    case "--beispiele":
                        if (++i >= args.Length) return a.Mit("--beispiele braucht einen Ordner oder eine Dateiliste.");
                        string grund = a.BeispieleAufloesen(args[i]);
                        if (grund != null) return a.Mit(grund);
                        break;
                    default:
                        if (args[i].StartsWith("--", StringComparison.Ordinal))
                            return a.Mit("Unbekannter Schalter: " + args[i]);
                        frei.Add(args[i]);
                        break;
                }
            }

            if (frei.Count != 2)
                return a.Mit("Erwartet werden genau zwei Pfade: <quelle.sqlite> <ziel.sqlite>.");

            a.Quelle = Path.GetFullPath(frei[0]);
            a.Ziel = Path.GetFullPath(frei[1]);

            if (!File.Exists(a.Quelle)) return a.Mit("Quelldatenbank nicht gefunden: " + a.Quelle);
            if (string.Equals(a.Quelle, a.Ziel, StringComparison.Ordinal))
                return a.Mit("Quelle und Ziel sind dieselbe Datei.");

            return a;
        }

        private Argumente Mit(string fehler) { Fehler = fehler; return this; }

        /// <summary>
        /// Ordner oder Liste zu Dateipfaden aufloesen. Ein Ordner wird nach <c>*.wpx</c>
        /// durchsucht und ORDINAL nach Namen sortiert — damit dieselbe Ablage immer
        /// dieselbe Reihenfolge ergibt und die vergebenen Projekt-Ids reproduzierbar sind.
        /// </summary>
        private string BeispieleAufloesen(string wert)
        {
            if (Directory.Exists(wert))
            {
                foreach (string d in Directory.GetFiles(wert, "*.wpx")
                                              .OrderBy(x => Path.GetFileName(x), StringComparer.Ordinal))
                    Beispiele.Add(Path.GetFullPath(d));
                return Beispiele.Count == 0 ? "Im Ordner " + wert + " liegt kein Projektpaket (*.wpx)." : null;
            }

            foreach (string teil in wert.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string p = Path.GetFullPath(teil.Trim());
                if (!File.Exists(p)) return "Projektpaket nicht gefunden: " + p;
                Beispiele.Add(p);
            }
            return Beispiele.Count == 0 ? "--beispiele nennt weder einen Ordner noch eine Datei." : null;
        }
    }
}
