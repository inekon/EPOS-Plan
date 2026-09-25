using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Gebaeudevergleich.Tests
{
    /// <summary>
    /// Ruft das Werkzeug als PROGRAMM auf (Muster <c>Auslieferungsvorlage.Tests/Werkzeuglauf.cs</c>)
    /// und liest seine Ausgabe.
    /// </summary>
    internal static class Werkzeuglauf
    {
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

        /// <summary>Die Testdatenbank — <c>null</c>, wenn sie fehlt oder nur ein LFS-Zeiger ist.</summary>
        internal static string Testdatenbank
        {
            get
            {
                string p = Path.Combine(Repowurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
                if (!File.Exists(p) || new FileInfo(p).Length < 4096) return null;
                return p;
            }
        }

        /// <summary>Die Referenzbasis: der EINE Ordner <c>Referenzlaeufe/20*_R*</c>.</summary>
        internal static string Basis
        {
            get
            {
                string[] kandidaten = Directory.GetDirectories(Path.Combine(Repowurzel, "Referenzlaeufe"), "20*_R*");
                if (kandidaten.Length != 1)
                    throw new InvalidOperationException("Erwartet genau einen Basisordner Referenzlaeufe/20*_R*, gefunden " +
                                                        kandidaten.Length.ToString(CultureInfo.InvariantCulture));
                return kandidaten[0];
            }
        }

        private static (string Datei, string Vorspann) Programm()
        {
            string konfiguration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name ?? "Release";
            string ordner = Path.Combine(Repowurzel, "Werkzeuge", "Gebaeudevergleich", "bin", konfiguration, "net10.0");
            string starter = Path.Combine(ordner,
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Gebaeudevergleich.exe" : "Gebaeudevergleich");
            if (File.Exists(starter)) return (starter, null);
            string dll = Path.Combine(ordner, "Gebaeudevergleich.dll");
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
                WorkingDirectory = Path.GetTempPath(),
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            if (vorspann != null) start.ArgumentList.Add(vorspann);
            foreach (string a in argumente) start.ArgumentList.Add(a);
            start.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en";
            start.Environment["LANG"] = "C.UTF-8";

            using var p = Process.Start(start);
            var aus = p.StandardOutput.ReadToEndAsync();
            var fehler = p.StandardError.ReadToEndAsync();
            p.WaitForExit();
            return new Ergebnis { Code = p.ExitCode, Ausgabe = aus.Result, Fehlerausgabe = fehler.Result };
        }

        internal static string Pruefsumme(string datei)
        {
            using var s = new FileStream(datei, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return Convert.ToHexString(SHA256.HashData(s));
        }

        // ---- CSV ------------------------------------------------------------------------

        /// <summary>Eine CSV des Werkzeugs (Semikolon, Anführungszeichen) als Zeilen mit Spaltennamen.</summary>
        internal static List<Dictionary<string, string>> Csv(string datei)
        {
            string[] zeilen = File.ReadAllText(datei, Encoding.UTF8).Split('\n', StringSplitOptions.RemoveEmptyEntries);
            string[] kopf = Felder(zeilen[0]);
            var liste = new List<Dictionary<string, string>>();
            foreach (string z in zeilen.Skip(1))
            {
                string[] f = Felder(z);
                var d = new Dictionary<string, string>(StringComparer.Ordinal);
                for (int i = 0; i < kopf.Length; i++) d[kopf[i]] = i < f.Length ? f[i] : "";
                liste.Add(d);
            }
            return liste;
        }

        private static string[] Felder(string zeile)
        {
            var felder = new List<string>();
            var sb = new StringBuilder();
            bool inAnf = false;
            for (int i = 0; i < zeile.Length; i++)
            {
                char c = zeile[i];
                if (inAnf)
                {
                    if (c == '"' && i + 1 < zeile.Length && zeile[i + 1] == '"') { sb.Append('"'); i++; }
                    else if (c == '"') inAnf = false;
                    else sb.Append(c);
                }
                else if (c == '"') inAnf = true;
                else if (c == ';') { felder.Add(sb.ToString()); sb.Clear(); }
                else if (c != '\r') sb.Append(c);
            }
            felder.Add(sb.ToString());
            return felder.ToArray();
        }

        internal static double Zahl(string s) => double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);

        /// <summary>Die Schlüssel-Wert-Zeilen einer <c>aggregate.csv</c> der Referenzbasis.</summary>
        internal static Dictionary<string, string> Aggregat(string projektordner)
        {
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string z in File.ReadAllLines(Path.Combine(projektordner, "aggregate.csv"), Encoding.UTF8))
            {
                int i = z.IndexOf(';');
                if (i > 0) d[z.Substring(0, i).TrimStart('﻿')] = z.Substring(i + 1);
            }
            return d;
        }
    }
}
