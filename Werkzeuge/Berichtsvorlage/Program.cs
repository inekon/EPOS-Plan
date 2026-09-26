using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Berichtsvorlage
{
    /// <summary>
    /// Pflegt die Word-Vorlage des Berichts (Konzept Berichtsvorlagen, Etappen BV-E0 bis BV-E2,
    /// Abschnitt 6.3, Anhang B.3).
    ///
    /// <para><b>bereinigen &lt;docx&gt;</b> — führt doppelte Stildefinitionen (gleiche
    /// <c>w:styleId</c>) so zusammen, wie Word sie heute auflöst, lässt je ID genau eine
    /// stehen und ergänzt das Absatzformat „EPOS Kapitelkopf“, wenn es fehlt
    /// (<see cref="Stilbereinigung"/>). Vorher gibt es den Vergleich der Definitionen aus,
    /// danach den Validatorbefund. Ein zweiter Lauf findet nichts mehr und schreibt nichts —
    /// die Datei bleibt byte-gleich.</para>
    ///
    /// <para><b>beispiel &lt;quelle.docx&gt; &lt;ziel.docx&gt; [--standard | --sammelanker]
    /// [--katalogfassung &lt;n&gt;]</b> — baut aus der bereinigten Vorlage die Beispielvorlage
    /// mit Platzhaltern (<see cref="Beispielvorlage"/>); mit <c>--standard</c> die Standardvorlage
    /// im selben Aufbau ohne Kommentare, mit <c>--sammelanker</c> die Stufe für BV-E1, deren Rumpf
    /// nur aus <c>{{bericht.inhalt}}</c> besteht. <c>--katalogfassung</c> setzt
    /// <c>EPOS.Katalogfassung</c> in <c>custom.xml</c> (Vorgabe
    /// <see cref="Beispielvorlage.KATALOGFASSUNG_VORGABE"/>).</para>
    ///
    /// <para><b>kurzbericht &lt;quelle.docx&gt; &lt;ziel.docx&gt; --sprache de|en [--katalogfassung &lt;n&gt;]</b> —
    /// baut aus der bereinigten Vorlage den Kurzbericht der Sprache (<see cref="Kurzbericht"/>, Konzept 6.3 Nr. 2,
    /// Anhang B.1): Lehrvorlage aus Einzelwerten, Blöcken, Tabellen und Bildern, erläutert in Kommentaren.</para>
    ///
    /// <para><b>ausfuehrlich &lt;quelle.docx&gt; &lt;ziel.docx&gt; --sprache de|en [--katalogfassung &lt;n&gt;]</b> —
    /// baut die ausführliche Vorlage der Sprache (<see cref="Ausfuehrlich"/>, Entscheid BV-E8-4): der volle Bericht in der
    /// Folge des Standardberichts, jeder Abschnitt aus Einzelelementen, erläutert in Kommentaren.</para>
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

            try
            {
                switch (args[0])
                {
                    case "bereinigen":
                        List<string> stellen = args.Skip(1).Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToList();
                        if (stellen.Count != 1 || args.Length != 2)
                            return Aufruffehler("bereinigen erwartet genau eine Datei und keinen Schalter.");
                        return Stilbereinigung.Ausfuehren(Path.GetFullPath(stellen[0]), Console.Out);

                    case "beispiel":
                        var dateien = new List<string>();
                        string fehler = LiesBeispiel(args.Skip(1).ToList(), dateien, out Vorlagenart art, out int katalogfassung);
                        if (fehler != null) return Aufruffehler(fehler);
                        return Beispielvorlage.Ausfuehren(Path.GetFullPath(dateien[0]), Path.GetFullPath(dateien[1]),
                                                          art, katalogfassung, Console.Out);

                    case "kurzbericht":
                        var ziele = new List<string>();
                        string fehlerKurz = LiesKurzbericht(args.Skip(1).ToList(), ziele, out bool englisch, out int fassung);
                        if (fehlerKurz != null) return Aufruffehler(fehlerKurz);
                        return Kurzbericht.Ausfuehren(Path.GetFullPath(ziele[0]), Path.GetFullPath(ziele[1]),
                                                      englisch, fassung, Console.Out);

                    case "ausfuehrlich":
                        var zieleAus = new List<string>();
                        string fehlerAus = LiesKurzbericht(args.Skip(1).ToList(), zieleAus, out bool englischAus, out int fassungAus, "ausfuehrlich");
                        if (fehlerAus != null) return Aufruffehler(fehlerAus);
                        return Ausfuehrlich.Ausfuehren(Path.GetFullPath(zieleAus[0]), Path.GetFullPath(zieleAus[1]),
                                                       englischAus, fassungAus, Console.Out);

                    case "excel-ausfuehrlich":
                        {
                            var zielXl = new List<string>();
                            string fehlerXl = LiesExcel(args.Skip(1).ToList(), zielXl, out bool englischXl, out int fassungXl);
                            if (fehlerXl != null) return Aufruffehler(fehlerXl);
                            return ExcelVorlage.Ausfuehren(Path.GetFullPath(zielXl[0]), englischXl, fassungXl, Console.Out);
                        }

                    case "alle":
                        List<string> ordner = args.Skip(1).Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToList();
                        if (ordner.Count != 1 || args.Length != 2)
                            return Aufruffehler("alle erwartet genau den Vorlagenordner und keinen Schalter.");
                        return Sammellauf.Ausfuehren(Path.GetFullPath(ordner[0]), Console.Out);

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

        /// <summary>
        /// Liest die Angaben von <c>beispiel</c>: Quelle und Ziel, wahlweise <c>--standard</c> oder
        /// <c>--sammelanker</c> und <c>--katalogfassung &lt;n&gt;</c> (ganze Zahl ab 1). Rückgabe:
        /// der Aufruffehler oder null.
        /// </summary>
        internal static string LiesBeispiel(IReadOnlyList<string> angaben, List<string> dateien,
                                            out Vorlagenart art, out int katalogfassung)
        {
            art = Vorlagenart.Beispiel;
            katalogfassung = Beispielvorlage.KATALOGFASSUNG_VORGABE;
            bool standard = false, sammelanker = false, fassungGesetzt = false;
            for (int i = 0; i < angaben.Count; i++)
            {
                string angabe = angaben[i];
                switch (angabe)
                {
                    case "--standard":
                        if (standard) return "--standard steht doppelt.";
                        standard = true;
                        break;
                    case "--sammelanker":
                        if (sammelanker) return "--sammelanker steht doppelt.";
                        sammelanker = true;
                        break;
                    case "--katalogfassung":
                        if (fassungGesetzt) return "--katalogfassung steht doppelt.";
                        if (i + 1 >= angaben.Count
                            || !int.TryParse(angaben[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out katalogfassung)
                            || katalogfassung < 1)
                            return "--katalogfassung erwartet eine ganze Zahl ab 1.";
                        fassungGesetzt = true;
                        i++;
                        break;
                    default:
                        if (angabe.StartsWith("--", StringComparison.Ordinal))
                            return "Unbekannter Schalter „" + angabe + "“.";
                        dateien.Add(angabe);
                        break;
                }
            }
            if (dateien.Count != 2) return "beispiel erwartet Quelle und Ziel.";
            if (standard && sammelanker) return "--standard und --sammelanker schließen einander aus.";
            art = sammelanker ? Vorlagenart.StandardSammelanker : standard ? Vorlagenart.Standard : Vorlagenart.Beispiel;
            return null;
        }

        /// <summary>
        /// Liest die Angaben von <c>kurzbericht</c> und <c>ausfuehrlich</c> (<paramref name="modus"/>): Quelle und Ziel, <c>--sprache de|en</c> (Pflicht) und
        /// <c>--katalogfassung &lt;n&gt;</c>. Rückgabe: der Aufruffehler oder null.
        /// </summary>
        internal static string LiesKurzbericht(IReadOnlyList<string> angaben, List<string> dateien,
                                               out bool englisch, out int katalogfassung, string modus = "kurzbericht")
        {
            englisch = false;
            katalogfassung = Beispielvorlage.KATALOGFASSUNG_VORGABE;
            string sprache = null;
            bool fassungGesetzt = false;
            for (int i = 0; i < angaben.Count; i++)
            {
                string angabe = angaben[i];
                switch (angabe)
                {
                    case "--sprache":
                        if (sprache != null) return "--sprache steht doppelt.";
                        if (i + 1 >= angaben.Count || (angaben[i + 1] != "de" && angaben[i + 1] != "en"))
                            return "--sprache erwartet de oder en.";
                        sprache = angaben[++i];
                        break;
                    case "--katalogfassung":
                        if (fassungGesetzt) return "--katalogfassung steht doppelt.";
                        if (i + 1 >= angaben.Count
                            || !int.TryParse(angaben[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out katalogfassung)
                            || katalogfassung < 1)
                            return "--katalogfassung erwartet eine ganze Zahl ab 1.";
                        fassungGesetzt = true;
                        i++;
                        break;
                    default:
                        if (angabe.StartsWith("--", StringComparison.Ordinal))
                            return "Unbekannter Schalter „" + angabe + "“.";
                        dateien.Add(angabe);
                        break;
                }
            }
            if (dateien.Count != 2) return modus + " erwartet Quelle und Ziel.";
            if (sprache == null) return modus + " erwartet --sprache de oder --sprache en.";
            englisch = sprache == "en";
            return null;
        }

        /// <summary>
        /// Liest die Angaben von <c>excel-ausfuehrlich</c>: das Ziel, <c>--sprache de|en</c> (Pflicht) und
        /// <c>--katalogfassung &lt;n&gt;</c> (Vorgabe: die laufende Fassung des Katalogs). Rückgabe: der Aufruffehler oder null.
        /// </summary>
        internal static string LiesExcel(IReadOnlyList<string> angaben, List<string> ziele, out bool englisch, out int katalogfassung)
        {
            var mitQuelle = new List<string> { "-" };
            mitQuelle.AddRange(angaben);
            var dateien = new List<string>();
            string fehler = LiesKurzbericht(mitQuelle, dateien, out englisch, out katalogfassung, "excel-ausfuehrlich");
            if (fehler != null) return fehler.Replace("erwartet Quelle und Ziel", "erwartet genau ein Ziel");
            if (!angaben.Contains("--katalogfassung")) katalogfassung = WindowsFormsApplication1.Vorlagenfeldkatalog.KATALOGFASSUNG;
            ziele.Add(dateien[1]);
            return null;
        }

        private static int Aufruffehler(string text)
        {
            Console.Error.WriteLine(text);
            Hilfe();
            return AUFRUF;
        }

        private static void Hilfe()
        {
            Console.WriteLine("Berichtsvorlage — pflegt die Word-Vorlagen des Berichts (Konzept Berichtsvorlagen, BV-E0 bis BV-E5)");
            Console.WriteLine();
            Console.WriteLine("  bereinigen <docx>                    doppelte Stile zusammenführen, „EPOS Kapitelkopf“ ergänzen");
            Console.WriteLine("  beispiel <quelle.docx> <ziel.docx>   Vorlage mit Platzhaltern aus der bereinigten Stilvorlage:");
            Console.WriteLine("      (ohne Schalter)                  Beispielvorlage, voller Aufbau mit Kommentaren     EPOS.Vorlage = beispiel");
            Console.WriteLine("      --standard                       Standardvorlage, voller Aufbau ohne Kommentare     EPOS.Vorlage = standard");
            Console.WriteLine("      --sammelanker                    Standardvorlage, Rumpf nur {{bericht.inhalt}}      EPOS.Vorlage = standard-sammelanker");
            Console.WriteLine("      --katalogfassung <n>             EPOS.Katalogfassung in custom.xml (Vorgabe " + Beispielvorlage.KATALOGFASSUNG_VORGABE + ")");
            Console.WriteLine("  kurzbericht <quelle.docx> <ziel.docx> --sprache de|en [--katalogfassung <n>]");
            Console.WriteLine("                                       Kurzbericht je Sprache (Lehrvorlage mit Kommentaren)  EPOS.Vorlage = kurzbericht");
            Console.WriteLine("  ausfuehrlich <quelle.docx> <ziel.docx> --sprache de|en [--katalogfassung <n>]");
            Console.WriteLine("                                       ausführliche Vorlage je Sprache (voller Bericht aus Einzelelementen)  EPOS.Vorlage = ausfuehrlich");
            Console.WriteLine("  excel-ausfuehrlich <ziel.xlsx> --sprache de|en [--katalogfassung <n>]");
            Console.WriteLine("                                       ausführliche Excel-Vorlage je Sprache (alle Konfigurationselemente)  EPOS.Vorlage = ausfuehrlich-excel");
            Console.WriteLine("  alle <vorlagenordner>                Sammelbefehl: jede mitgelieferte Vorlage aus dem aktuellen Katalog neu erzeugen");
            Console.WriteLine();
            Console.WriteLine("Beispiel:");
            Console.WriteLine("  dotnet run --project Werkzeuge/Berichtsvorlage -c Release -- bereinigen WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage.docx");
            Console.WriteLine("  dotnet run --project Werkzeuge/Berichtsvorlage -c Release -- beispiel WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage.docx WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage_Beispiel.docx");
            Console.WriteLine("  dotnet run --project Werkzeuge/Berichtsvorlage -c Release -- kurzbericht WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage.docx WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage_Kurzbericht.docx --sprache de");
            Console.WriteLine("  dotnet run --project Werkzeuge/Berichtsvorlage -c Release -- ausfuehrlich WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage.docx WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage_Ausfuehrlich.docx --sprache de");
            Console.WriteLine("  dotnet run --project Werkzeuge/Berichtsvorlage -c Release -- alle WindowsFormsApplication1/Allgemein/Bericht/Vorlagen");
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
