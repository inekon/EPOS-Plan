using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace ZapfprofilValidierung
{
    /// <summary>
    /// <b>Die Kommandozeile</b> des Werkzeugs, zerlegt und geprüft — ein Aufruffehler ist ein Satz
    /// auf stderr und Rückgabe 2, nie eine halbe Auswertung.
    ///
    /// <code>
    /// ZapfprofilValidierung &lt;ordner&gt; --ziel &lt;berichtordner&gt;
    ///                       [--katalog &lt;sqlite|paketordner&gt;]
    ///                       [--realisierungen N] [--seed S] [--trocken] [--beispielreihe]
    /// </code>
    /// </summary>
    internal sealed class Argumente
    {
        /// <summary>Der Ordner mit je einem Unterordner pro Messobjekt.</summary>
        internal string Quelle { get; private set; }

        /// <summary>Der Ordner, in den die Berichte geschrieben werden.</summary>
        internal string Ziel { get; private set; }

        /// <summary>Die Katalogquelle: eine <c>.sqlite</c>-Datei oder ein Paketordner; leer = Vorgabe.</summary>
        internal string Katalog { get; private set; }

        /// <summary>Zahl der Realisierungen des Ensembles; <c>null</c> = die Angabe des Objekts gilt.</summary>
        internal int? Realisierungen { get; private set; }

        /// <summary>Der Seed der Stochastik; <c>null</c> = die Angabe des Objekts gilt.</summary>
        internal int? Seed { get; private set; }

        /// <summary>Nur lesen und prüfen, nichts schreiben.</summary>
        internal bool Trocken { get; private set; }

        /// <summary>
        /// Statt der Auswertung die <b>synthetische</b> Messreihe jedes Objekts neu erzeugen
        /// (<see cref="Beispielreihe"/>) — der wiederholbare Weg, mit dem die Reihe unter
        /// <c>Beispiel/</c> entstanden ist.
        /// </summary>
        internal bool BeispielreiheSchreiben { get; private set; }

        /// <summary>Die Vorgabe der Katalogquelle, repo-relativ (Kapitel 6: der freie Paketteil liegt hier).</summary>
        internal const string KATALOG_VORGABE = "Referenzlaeufe/Kenndaten_Test.sqlite";

        /// <summary>
        /// Zerlegt die Argumente. <paramref name="fehler"/> trägt den Grund, wenn <c>null</c>
        /// zurückkommt.
        /// </summary>
        internal static Argumente Lesen(string[] args, out string fehler)
        {
            fehler = null;
            var a = new Argumente();
            var frei = new List<string>();
            for (int i = 0; i < (args?.Length ?? 0); i++)
            {
                string s = args[i];
                switch (s)
                {
                    case "--ziel":
                        if (!Folgt(args, i, out string z, out fehler, "--ziel")) return null;
                        a.Ziel = z; i++; break;
                    case "--katalog":
                        if (!Folgt(args, i, out string k, out fehler, "--katalog")) return null;
                        a.Katalog = k; i++; break;
                    case "--realisierungen":
                        if (!Folgt(args, i, out string r, out fehler, "--realisierungen")) return null;
                        if (!int.TryParse(r, NumberStyles.Integer, CultureInfo.InvariantCulture, out int rz) || rz < 0)
                        {
                            fehler = "--realisierungen erwartet eine ganze Zahl ab 0, nicht \"" + r + "\".";
                            return null;
                        }
                        a.Realisierungen = rz; i++; break;
                    case "--seed":
                        if (!Folgt(args, i, out string sd, out fehler, "--seed")) return null;
                        if (!int.TryParse(sd, NumberStyles.Integer, CultureInfo.InvariantCulture, out int sz))
                        {
                            fehler = "--seed erwartet eine ganze Zahl, nicht \"" + sd + "\".";
                            return null;
                        }
                        a.Seed = sz; i++; break;
                    case "--trocken": a.Trocken = true; break;
                    case "--beispielreihe": a.BeispielreiheSchreiben = true; break;
                    default:
                        if (s != null && s.StartsWith("--", StringComparison.Ordinal))
                        {
                            fehler = "Unbekannter Schalter \"" + s + "\".";
                            return null;
                        }
                        frei.Add(s);
                        break;
                }
            }

            if (frei.Count == 0) { fehler = "Der Quellordner fehlt."; return null; }
            if (frei.Count > 1) { fehler = "Zu viele Angaben ohne Schalter: " + string.Join(", ", frei); return null; }
            a.Quelle = frei[0];
            if (!Directory.Exists(a.Quelle)) { fehler = "Der Quellordner \"" + a.Quelle + "\" gibt es nicht."; return null; }
            if (!a.BeispielreiheSchreiben && string.IsNullOrWhiteSpace(a.Ziel))
            {
                fehler = "--ziel <berichtordner> fehlt.";
                return null;
            }
            return a;
        }

        private static bool Folgt(string[] args, int i, out string wert, out string fehler, string schalter)
        {
            wert = null; fehler = null;
            if (i + 1 >= args.Length) { fehler = schalter + " erwartet eine Angabe."; return false; }
            wert = args[i + 1];
            return true;
        }

        /// <summary>Die Hilfe auf stdout — dieselben Zeilen wie in <c>LIESMICH.md</c>.</summary>
        internal static void HilfeAusgeben(TextWriter aus)
        {
            aus.WriteLine("ZapfprofilValidierung - Rechennachweis des Zapfprofilgenerators gegen Messreihen (Stufe Z5, K5).");
            aus.WriteLine();
            aus.WriteLine("  ZapfprofilValidierung <ordner> --ziel <berichtordner>");
            aus.WriteLine("                        [--katalog <sqlite|paketordner>]");
            aus.WriteLine("                        [--realisierungen N] [--seed S] [--trocken] [--beispielreihe]");
            aus.WriteLine();
            aus.WriteLine("  <ordner>           je Messobjekt ein Unterordner mit objekt.json und messreihe.csv");
            aus.WriteLine("  --ziel             Ordner der Berichte (Markdown und CSV je Objekt, dazu der Sammelbericht)");
            aus.WriteLine("  --katalog          Katalogquelle; Vorgabe " + KATALOG_VORGABE);
            aus.WriteLine("  --realisierungen   ueberschreibt die Zahl der Ensemble-Realisierungen jedes Objekts");
            aus.WriteLine("  --seed             ueberschreibt den Seed jedes Objekts");
            aus.WriteLine("  --trocken          nur lesen und pruefen, nichts schreiben");
            aus.WriteLine("  --beispielreihe    schreibt statt der Berichte die synthetische messreihe.csv jedes Objekts");
            aus.WriteLine();
            aus.WriteLine("Rueckgabe: 0 abgenommen, 2 Aufruf, 3 Schreibort, 5 nicht abgenommen, 6 Berichtswache, 1 unerwartet.");
            aus.WriteLine("Echte Messreihen gehoeren NIE ins Repositorium; der Bericht traegt nur Verhaeltniszahlen.");
        }
    }
}
