using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Berichtsvorlage
{
    /// <summary>
    /// Pflegt die Word-Vorlage des Berichts (Konzept Berichtsvorlagen, Etappe BV-E0,
    /// Abschnitt 6.3, Anhang B.3).
    ///
    /// <para><b>bereinigen &lt;docx&gt;</b> — führt doppelte Stildefinitionen (gleiche
    /// <c>w:styleId</c>) so zusammen, wie Word sie heute auflöst, lässt je ID genau eine
    /// stehen und ergänzt das Absatzformat „EPOS Kapitelkopf“, wenn es fehlt
    /// (<see cref="Stilbereinigung"/>). Vorher gibt es den Vergleich der Definitionen aus,
    /// danach den Validatorbefund. Ein zweiter Lauf findet nichts mehr und schreibt nichts —
    /// die Datei bleibt byte-gleich.</para>
    ///
    /// <para><b>beispiel &lt;quelle.docx&gt; &lt;ziel.docx&gt; [--sammelanker]</b> — baut aus
    /// der bereinigten Vorlage die Beispielvorlage mit Platzhaltern
    /// (<see cref="Beispielvorlage"/>); mit <c>--sammelanker</c> die Stufe für BV-E1, deren
    /// Rumpf nur aus <c>{{bericht.inhalt}}</c> besteht.</para>
    ///
    /// <para><b>Rückgabe.</b> 0 = geschrieben bzw. nichts zu tun; 2 Aufruf, 3 Datei,
    /// 4 Prüfung rot (Validator, fehlende Stile, doppelte Stile in der Quelle),
    /// 1 unerwartet. Bei jedem Wert außer 0 bleibt die Zieldatei unberührt: Beide Modi
    /// arbeiten auf einer Arbeitskopie und ersetzen erst nach grüner Prüfung.</para>
    /// </summary>
    internal static class Program
    {
        internal const int OK = 0;
        internal const int UNERWARTET = 1;
        internal const int AUFRUF = 2;
        internal const int DATEI = 3;
        internal const int PRUEFUNG = 4;

        private static int Main(string[] args)
        {
            // Die Ausgabe führt Umlaute und Gedankenstriche: UTF-8 ohne Vorspann, auf jeder
            // Plattform (Muster: Werkzeuge/Auslieferungsvorlage/Program.cs).
            try { Console.OutputEncoding = new UTF8Encoding(false); } catch { }

            if (args.Length == 0 || args[0] == "--hilfe" || args[0] == "-h" || args[0] == "--help")
            {
                Hilfe();
                return AUFRUF;
            }

            List<string> stellen = args.Skip(1).Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToList();
            List<string> schalter = args.Skip(1).Where(a => a.StartsWith("--", StringComparison.Ordinal)).ToList();

            try
            {
                switch (args[0])
                {
                    case "bereinigen":
                        if (stellen.Count != 1 || schalter.Count != 0)
                            return Aufruffehler("bereinigen erwartet genau eine Datei und keinen Schalter.");
                        return Stilbereinigung.Ausfuehren(Path.GetFullPath(stellen[0]), Console.Out);

                    case "beispiel":
                        if (stellen.Count != 2 || schalter.Any(s => s != "--sammelanker"))
                            return Aufruffehler("beispiel erwartet Quelle und Ziel, als Schalter nur --sammelanker.");
                        return Beispielvorlage.Ausfuehren(Path.GetFullPath(stellen[0]), Path.GetFullPath(stellen[1]),
                                                          schalter.Contains("--sammelanker"), Console.Out);

                    default:
                        return Aufruffehler("Unbekannter Modus „" + args[0] + "“.");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Unerwarteter Fehler: " + ex);
                return UNERWARTET;
            }
        }

        private static int Aufruffehler(string text)
        {
            Console.Error.WriteLine(text);
            Hilfe();
            return AUFRUF;
        }

        private static void Hilfe()
        {
            Console.WriteLine("Berichtsvorlage — pflegt die Word-Vorlage des Berichts (Konzept Berichtsvorlagen, BV-E0)");
            Console.WriteLine();
            Console.WriteLine("  bereinigen <docx>                                   doppelte Stile zusammenführen, „EPOS Kapitelkopf“ ergänzen");
            Console.WriteLine("  beispiel <quelle.docx> <ziel.docx> [--sammelanker]  Beispielvorlage mit Platzhaltern aus der bereinigten Vorlage");
            Console.WriteLine();
            Console.WriteLine("Beispiel:");
            Console.WriteLine("  dotnet run --project Werkzeuge/Berichtsvorlage -c Release -- bereinigen WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage.docx");
            Console.WriteLine("  dotnet run --project Werkzeuge/Berichtsvorlage -c Release -- beispiel WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage.docx WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage_Beispiel.docx");
        }

        /// <summary>
        /// Legt eine Arbeitskopie im Temp-Ordner des Systems an — nie im Arbeitsbaum:
        /// Bräche das Werkzeug ab, läge sonst eine halbe Datei im Repository, und der
        /// nächste Sync-Commit nähme sie mit. Der Aufrufer löscht sie in einem finally.
        /// </summary>
        internal static string Arbeitskopie(string quelle)
        {
            string kopie = Path.Combine(Path.GetTempPath(), "berichtsvorlage_" + Guid.NewGuid().ToString("N") + ".docx");
            File.Copy(quelle, kopie, false);
            return kopie;
        }

        internal static void Loeschen(string pfad)
        {
            try { if (pfad != null && File.Exists(pfad)) File.Delete(pfad); } catch { }
        }
    }
}
