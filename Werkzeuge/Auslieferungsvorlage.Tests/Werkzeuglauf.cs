using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Auslieferungsvorlage.Tests
{
    /// <summary>
    /// Ruft das Werkzeug so auf, wie es die Freigabekette aufruft: als PROGRAMM, mit
    /// Kommandozeile und Rueckgabecode.
    ///
    /// <para><b>Warum nicht die Klassen direkt.</b> <c>Setup/build-setup.ps1</c> und
    /// <c>Setup/EPOS-Plan.iss</c> werden gegen genau diese Naht verdrahtet (Auftrag #162).
    /// Ein Test, der <c>Vorlagenbau</c> von innen anspricht, pruefte alles ausser der
    /// Stelle, an der es im Ernstfall bricht — ein vertippter Schalter, ein
    /// Rueckgabecode, der 0 meldet, obwohl nichts entstanden ist.</para>
    /// </summary>
    internal static class Werkzeuglauf
    {
        /// <summary>Das Ergebnis eines Laufs.</summary>
        internal sealed class Ergebnis
        {
            internal int Code;
            internal string Ausgabe;
            internal string Fehlerausgabe;
            internal string Alles => Ausgabe + Environment.NewLine + Fehlerausgabe;
        }

        /// <summary>Die Repowurzel, erkennbar an <c>WP-Plan.sln</c>.</summary>
        internal static string Repowurzel
        {
            get
            {
                var d = new DirectoryInfo(AppContext.BaseDirectory);
                while (d != null)
                {
                    if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
                    d = d.Parent;
                }
                throw new InvalidOperationException("Repowurzel nicht gefunden über " + AppContext.BaseDirectory);
            }
        }

        /// <summary>
        /// Die Testdatenbank — oder <c>null</c>, wenn sie fehlt. Dann ueberspringen die
        /// Faelle still (dieselbe Regel wie in <c>EPOS.Kern.Tests.TestDatenbank</c>: ein
        /// Lauf ohne die 67-MB-Datei soll schweigen, nicht rot werden).
        /// </summary>
        internal static string Testdatenbank
        {
            get
            {
                string p = Path.Combine(Repowurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
                return File.Exists(p) ? p : null;
            }
        }

        /// <summary>
        /// Das gebaute Werkzeug. Es liegt in DERSELBEN Konfiguration wie dieses
        /// Testprojekt — der <c>ProjectReference</c> sorgt dafuer, dass es vor den Proben
        /// gebaut ist. Auf Windows traegt es <c>.exe</c>, sonst keine Endung; fehlt der
        /// Anwendungsstarter (etwa bei <c>PublishSingleFile</c>-Bauarten), tut es die
        /// Bibliothek ueber <c>dotnet</c>.
        /// </summary>
        private static (string Datei, string Vorspann) Programm()
        {
            string konfiguration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name ?? "Release";
            string ordner = Path.Combine(Repowurzel, "Werkzeuge", "Auslieferungsvorlage",
                                         "bin", konfiguration, "net10.0");
            string starter = Path.Combine(ordner,
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Auslieferungsvorlage.exe" : "Auslieferungsvorlage");
            if (File.Exists(starter)) return (starter, null);

            string dll = Path.Combine(ordner, "Auslieferungsvorlage.dll");
            if (File.Exists(dll)) return ("dotnet", dll);

            throw new FileNotFoundException("Das gebaute Werkzeug wurde nicht gefunden", starter);
        }

        internal static Ergebnis Starten(params string[] argumente)
        {
            (string datei, string vorspann) = Programm();

            var start = new ProcessStartInfo
            {
                FileName = datei,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = Repowurzel,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            if (vorspann != null) start.ArgumentList.Add(vorspann);
            foreach (string a in argumente) start.ArgumentList.Add(a);

            // Die Kultur festnageln: Das Werkzeug schreibt Zahlen in den Prueflauf, und
            // ein Testlauf soll nicht davon abhaengen, welche Sprache der Laeufer eingestellt hat.
            start.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en";
            start.Environment["LANG"] = "C.UTF-8";

            using var p = Process.Start(start);
            string aus = p.StandardOutput.ReadToEnd();
            string fehler = p.StandardError.ReadToEnd();
            p.WaitForExit();
            return new Ergebnis { Code = p.ExitCode, Ausgabe = aus, Fehlerausgabe = fehler };
        }
    }

    /// <summary>Ein Wegwerfordner fuer die Dauer einer Probe.</summary>
    internal sealed class Arbeitsordner : IDisposable
    {
        internal Arbeitsordner()
        {
            Pfad = Path.Combine(Path.GetTempPath(), "vorlagenprobe-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(Pfad);
        }

        internal string Pfad { get; }

        internal string Datei(string name) => Path.Combine(Pfad, name);

        public void Dispose()
        {
            try { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); } catch { }
            try { Directory.Delete(Pfad, true); } catch { }
        }
    }
}
