using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;

namespace ZapfprofilValidierung
{
    /// <summary>
    /// <b>Der Ablauf</b> — dieselbe Naht, die <c>Program.Main</c> und die Proben benutzen: Argumente,
    /// Katalog, Objekte, Berichte, Rückgabecode. Nichts wird geschrieben, bevor jeder Berichtstext
    /// die <see cref="Berichtswache"/> bestanden hat.
    /// </summary>
    internal static class Einstieg
    {
        internal const int OK = 0;
        internal const int UNERWARTET = 1;
        internal const int AUFRUF = 2;
        internal const int SCHREIBORT = 3;
        internal const int NICHT_ABGENOMMEN = 5;
        internal const int BERICHTSWACHE = 6;

        /// <summary>Der Name der Beschreibungsdatei in jedem Objektordner.</summary>
        internal const string OBJEKTDATEI = "objekt.json";

        internal const string SAMMEL_MD = "Sammelbericht.md";
        internal const string SAMMEL_CSV = "Sammelbericht.csv";

        /// <summary>
        /// Führt den Lauf aus. <paramref name="aus"/> nimmt die Fortschrittszeilen,
        /// <paramref name="fehler"/> die Gründe.
        /// </summary>
        internal static int Lauf(string[] args, TextWriter aus, TextWriter fehler)
        {
            if (args == null || args.Length == 0 || args[0] == "--hilfe" || args[0] == "-h" || args[0] == "--help")
            {
                Argumente.HilfeAusgeben(aus);
                return AUFRUF;
            }
            Argumente a = Argumente.Lesen(args, out string aufruffehler);
            if (a == null) { fehler.WriteLine(aufruffehler); return AUFRUF; }

            string katalogpfad = string.IsNullOrWhiteSpace(a.Katalog)
                ? Vorgabekatalog(a.Quelle) : a.Katalog;
            Katalog katalog = Katalogquelle.Lesen(katalogpfad, out string katalogfehler);
            if (katalog == null) { fehler.WriteLine(katalogfehler); return AUFRUF; }
            aus.WriteLine("Katalog: " + katalog.Herkunft + " — " + katalog.Arten.Count.ToString(CultureInfo.InvariantCulture)
                          + " Nutzungsarten, " + katalog.Parameter.Anzahl.ToString(CultureInfo.InvariantCulture)
                          + " Parameter, " + katalog.Kategorien.Count.ToString(CultureInfo.InvariantCulture)
                          + " Zapfkategorien.");
            foreach (string h in katalog.Hinweise) aus.WriteLine("  Hinweis: " + h);

            List<string> ordner = Objektordner(a.Quelle);
            if (ordner.Count == 0)
            {
                fehler.WriteLine("Unter \"" + a.Quelle + "\" liegt kein Objektordner mit " + OBJEKTDATEI + ".");
                return AUFRUF;
            }

            if (a.BeispielreiheSchreiben) return Beispielreihen(a, katalog, ordner, aus, fehler);

            var befunde = new List<Objektbefund>();
            foreach (string o in ordner)
            {
                Objektbeschreibung beschreibung = Objektbeschreibung.Lesen(Path.Combine(o, OBJEKTDATEI),
                                                                          out string lesefehler);
                if (beschreibung == null)
                {
                    fehler.WriteLine(Path.GetFileName(o) + "/" + OBJEKTDATEI + ": " + lesefehler);
                    befunde.Add(new Objektbefund
                    {
                        Kennung = Path.GetFileName(o), Ordner = Path.GetFileName(o),
                        Abbruch = OBJEKTDATEI + " ist nicht lesbar: " + lesefehler
                    });
                    continue;
                }
                Objektbefund b = Objektlauf.Rechnen(o, beschreibung, katalog, a.Realisierungen, a.Seed);
                befunde.Add(b);
                aus.WriteLine(b.Kennung + ": " + Bericht.Ampeltext(b.Gesamt)
                              + (b.Abbruch != null ? " — " + b.Abbruch : ""));
            }

            int code = befunde.All(b => b.Gesamt == Ampel.Gruen) ? OK : NICHT_ABGENOMMEN;
            if (a.Trocken)
            {
                aus.WriteLine("Trockenlauf: " + befunde.Count.ToString(CultureInfo.InvariantCulture)
                              + " Objekte gelesen und gerechnet, nichts geschrieben.");
                return code;
            }

            int schreibcode = Schreiben(a, katalog, befunde, aus, fehler);
            return schreibcode != OK ? schreibcode : code;
        }

        /// <summary>
        /// Schreibt die Berichte. Jeder Text geht vorher durch die <see cref="Berichtswache"/>; ein
        /// Fund lässt nichts entstehen.
        /// </summary>
        private static int Schreiben(Argumente a, Katalog katalog, List<Objektbefund> befunde,
                                     TextWriter aus, TextWriter fehler)
        {
            var texte = new List<(string Datei, string Text)>();
            foreach (Objektbefund b in befunde)
            {
                texte.Add((Dateiname(b.Kennung) + ".md", Bericht.ObjektMarkdown(b)));
                texte.Add((Dateiname(b.Kennung) + ".csv", Bericht.ObjektCsv(b)));
            }
            texte.Add((SAMMEL_MD, Bericht.SammelMarkdown(befunde, katalog.Herkunft, null)));
            texte.Add((SAMMEL_CSV, Bericht.SammelCsv(befunde)));

            // Die Wache prueft JEDEN Text gegen die Einheitenregel UND gegen die Kennzahlen ALLER
            // Objekte: Ein Sammelbericht koennte die Zahl eines anderen Objekts tragen als das, dessen
            // Datei gerade entsteht.
            List<string> verboten = befunde.SelectMany(b => b.Verbotene).Distinct().ToList();
            foreach ((string datei, string text) in texte)
            {
                List<string> funde = Berichtswache.Pruefen(text, verboten);
                if (funde.Count == 0) continue;
                fehler.WriteLine(Berichtswache.Meldung(datei, funde));
                return BERICHTSWACHE;
            }

            try
            {
                Directory.CreateDirectory(a.Ziel);
                foreach ((string datei, string text) in texte)
                    File.WriteAllText(Path.Combine(a.Ziel, datei), text, new UTF8Encoding(false));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException
                                      || ex is ArgumentException || ex is NotSupportedException)
            {
                fehler.WriteLine("Der Zielordner \"" + a.Ziel + "\" ist nicht beschreibbar: " + ex.Message);
                return SCHREIBORT;
            }
            aus.WriteLine("Berichte in \"" + a.Ziel + "\": "
                          + texte.Count.ToString(CultureInfo.InvariantCulture) + " Dateien.");
            return OK;
        }

        private static int Beispielreihen(Argumente a, Katalog katalog, List<string> ordner,
                                          TextWriter aus, TextWriter fehler)
        {
            int rot = 0;
            foreach (string o in ordner)
            {
                Objektbeschreibung beschreibung = Objektbeschreibung.Lesen(Path.Combine(o, OBJEKTDATEI),
                                                                          out string lesefehler);
                if (beschreibung == null)
                {
                    fehler.WriteLine(Path.GetFileName(o) + "/" + OBJEKTDATEI + ": " + lesefehler);
                    rot++;
                    continue;
                }
                string grund = Beispielreihe.Schreiben(o, beschreibung, katalog, a.Trocken, out string vermerk);
                if (grund != null) { fehler.WriteLine(beschreibung.Kennung + ": " + grund); rot++; continue; }
                aus.WriteLine(beschreibung.Kennung + ": " + (a.Trocken ? "geprueft" : "geschrieben")
                              + " — " + vermerk);
            }
            return rot == 0 ? OK : NICHT_ABGENOMMEN;
        }

        /// <summary>Die Objektordner: jeder Unterordner mit einer <c>objekt.json</c>, nach Namen geordnet.</summary>
        internal static List<string> Objektordner(string quelle)
            => Directory.EnumerateDirectories(quelle)
                        .Where(d => File.Exists(Path.Combine(d, OBJEKTDATEI)))
                        .OrderBy(d => Path.GetFileName(d), StringComparer.Ordinal)
                        .ToList();

        /// <summary>
        /// Die Vorgabe der Katalogquelle: <c>Referenzlaeufe/Kenndaten_Test.sqlite</c>, gesucht vom
        /// Quellordner und vom Laufordner aufwärts (Muster <c>EPOS.Kern.Tests/TestDatenbank</c>).
        /// </summary>
        internal static string Vorgabekatalog(string quelle)
        {
            foreach (string start in new[] { quelle, AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            {
                if (string.IsNullOrWhiteSpace(start)) continue;
                var d = new DirectoryInfo(Path.GetFullPath(start));
                for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
                {
                    string kandidat = Path.Combine(d.FullName,
                        Argumente.KATALOG_VORGABE.Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(kandidat)) return kandidat;
                }
            }
            return Argumente.KATALOG_VORGABE;
        }

        /// <summary>Ein Dateiname aus der Kennung — nur Buchstaben, Ziffern, Strich und Unterstrich.</summary>
        internal static string Dateiname(string kennung)
        {
            var s = new StringBuilder();
            foreach (char c in kennung ?? "")
                s.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
            return s.Length == 0 ? "objekt" : s.ToString();
        }
    }
}
