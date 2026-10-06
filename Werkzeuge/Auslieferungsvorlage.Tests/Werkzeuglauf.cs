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
                // Ein GEHOBENER Stand der Testdatenbank (Werkzeuge/Testdatenbankschema auf einer Kopie),
                // solange ein neuer Schemaschritt die Datei im Repository noch nicht erreicht hat: Die
                // Abnahme des Werkzeugs verlangt den Zielstand. Die Datei im Repository bleibt unberuehrt.
                string umweg = Environment.GetEnvironmentVariable("EPOS_TESTDATENBANK");
                if (!string.IsNullOrEmpty(umweg) && File.Exists(umweg)) return umweg;

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

        /// <summary>
        /// <b>Die Registerkataloge der Testdatenbank mit leerem Paketteil</b>: Sie fuehren Zeilen, aber keinen
        /// Satz mit <c>ReadOnly = 1</c>, und brechen ohne benannte Ausnahme mit Code 6 ab (Konzept Setup 6.5.4
        /// E2). Dieselbe Liste nimmt der CI-Setup-Lauf (<c>windows.yml</c>, Job <c>installer</c>); die Wache
        /// <c>KatalogpaketSetupWacheTests</c> haelt beide gleich. Waechst die Pflege der Testdatenbank, wird
        /// die Liste kuerzer - nie laenger, ohne dass ein Katalog dazukommt. Gebraucht wird sie nur im Modus
        /// <c>alle</c>: Im Modus <c>readonly</c> leert die ReadOnly-Regel jeden dieser Kataloge, ein Lauf dort
        /// kommt ohne Ausnahme aus (<c>KatalogregelTests.K2</c>).
        /// </summary>
        internal static readonly string[] LEERE_PAKETTEILE_DER_TESTDATENBANK =
        {
            "Tab_Heizkessel_STAMM", "Tab_PV_STAMM", "Tab_Brennstoff_Stamm", "Tab_DBTagV_STAMM",
            "Tab_Pufferspeicher_STAMM", "Tab_Solarkollektoren_STAMM", "Tab_Solarganglinie_STAMM",
            "Tab_Stromspeicher_STAMM", "Tab_Stromverbrauchertyp_STAMM", "Tab_Stromverbraucher_STAMM",
            "Tab_Stromganglinie_STAMM"
        };

        /// <summary>Die Argumente samt je einem <c>--ohne-paket</c> fuer jeden leeren Paketteil der Testdatenbank.</summary>
        internal static string[] MitAusnahmen(params string[] argumente)
        {
            var alle = new List<string>(argumente);
            foreach (string t in LEERE_PAKETTEILE_DER_TESTDATENBANK) { alle.Add("--ohne-paket"); alle.Add(t); }
            return alle.ToArray();
        }

        /// <summary>
        /// Ein Lauf mit den benannten Ausnahmen der Testdatenbank: fuer jeden Fall im Modus <c>alle</c>, der
        /// nicht den Abbruch bei leerem Paketteil selbst prueft.
        /// </summary>
        internal static Ergebnis StartenMitAusnahmen(params string[] argumente) => Starten(MitAusnahmen(argumente));

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
