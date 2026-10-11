using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Wache über die Bilddateien des Wikis unter <c>Projekte/Wiki/Dateien/</c> (Konzept
    /// Hilfesystem, Abschnitt 14.3): nur SVG und PNG mit ASCII-Namen, je Datei genau eine Zeile in
    /// <c>Werkzeuge/WikiUpload/dateien.tsv</c> und jede Zeile mit vorhandener Datei, jede Datei in
    /// mindestens einer Wiki-Quelle eingebunden, PNG höchstens 1400 px breit und 300 KB groß, SVG
    /// ohne die Muster, die auch <c>wiki_upload.py</c> vor jedem Upload abweist.
    /// Nicht geprüft wird, ob jedes <c>[[Datei:…]]</c> der Quellen im Repository liegt — ältere
    /// Seiten verweisen auf Schemata, die nur im Wiki liegen.
    /// </summary>
    public sealed class WikiBilderWacheTests
    {
        private const int PNG_BREITE_MAX = 1400;
        private const long PNG_BYTES_MAX = 300 * 1024;

        private static readonly string[] Endungen = { ".svg", ".png" };

        /// <summary>Dieselben Muster wie <c>SVG_VERBOTENE_MUSTER</c> in <c>wiki_upload.py</c>.</summary>
        private static readonly (Regex Muster, string Meldung)[] SvgVerbote =
        {
            (new Regex(@"<\s*script", RegexOptions.IgnoreCase), "<script>"),
            (new Regex(@"\bon[a-zA-Z]+\s*=", RegexOptions.IgnoreCase), "on…=-Attribut"),
            (new Regex(@"<\s*foreignObject", RegexOptions.IgnoreCase), "<foreignObject>"),
            (new Regex(@"(?:xlink:href|href)\s*=\s*[""']\s*https?://", RegexOptions.IgnoreCase), "externe href/xlink:href"),
            (new Regex(@"<\s*title", RegexOptions.IgnoreCase), "<title>"),
        };

        private static readonly Regex DateiVerweis = new Regex(@"\[\[\s*Datei\s*:\s*([^|\]]+)", RegexOptions.IgnoreCase);

        [Fact]
        public void Nur_Svg_und_Png_mit_Ascii_Namen()
        {
            var fehler = new List<string>();
            foreach (string pfad in Bilddateien())
            {
                string name = Path.GetFileName(pfad);
                string endung = Path.GetExtension(name).ToLowerInvariant();
                if (!Endungen.Contains(endung) || Path.GetExtension(name) != endung)
                    fehler.Add(name + ": Endung nicht zugelassen (nur .svg und .png, klein geschrieben)");
                if (name.Any(z => z > 127 || char.IsWhiteSpace(z)))
                    fehler.Add(name + ": Dateiname ist nicht reines ASCII ohne Leerzeichen");
            }

            Assert.True(fehler.Count == 0, string.Join(Environment.NewLine, fehler));
        }

        [Fact]
        public void Jede_Datei_hat_genau_eine_Zeile_in_dateien_tsv_und_jede_Zeile_eine_Datei()
        {
            string wurzel = Arbeitsbaum();
            var zeilen = TsvZeilen(wurzel);
            var fehler = new List<string>();

            foreach (string pfad in Bilddateien())
            {
                string name = Path.GetFileName(pfad);
                int anzahl = zeilen.Count(z => string.Equals(z.Name, name, StringComparison.Ordinal));
                if (anzahl != 1)
                    fehler.Add(name + ": " + anzahl + " Zeilen in dateien.tsv statt genau einer");
            }

            foreach (var z in zeilen)
            {
                string voll = Path.Combine(wurzel, z.RepoPfad.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(voll))
                    fehler.Add("dateien.tsv Zeile " + z.Nummer + ": Datei fehlt (" + z.RepoPfad + ")");
                else if (!string.Equals(Path.GetFileName(z.RepoPfad), z.Name, StringComparison.Ordinal))
                    fehler.Add("dateien.tsv Zeile " + z.Nummer + ": Dateiname und Repo-Pfad passen nicht zusammen");
            }

            Assert.True(fehler.Count == 0, string.Join(Environment.NewLine, fehler));
        }

        [Fact]
        public void Jede_Datei_ist_in_einer_Wiki_Quelle_eingebunden()
        {
            string wurzel = Arbeitsbaum();
            var benutzt = new HashSet<string>(StringComparer.Ordinal);
            var quellen = Directory.GetFiles(Path.Combine(wurzel, "Projekte", "Wiki"), "*.wiki")
                .Concat(Directory.GetFiles(Path.Combine(wurzel, "EPOS.Kern", "Allgemein", "Hilfe", "Berechnung"), "*.wiki"));
            foreach (string quelle in quellen)
                foreach (Match m in DateiVerweis.Matches(File.ReadAllText(quelle, Encoding.UTF8)))
                    benutzt.Add(Normalform(m.Groups[1].Value));

            var fehler = Bilddateien()
                .Select(Path.GetFileName)
                .Where(name => !benutzt.Contains(Normalform(name)))
                .Select(name => name + ": in keiner Wiki-Quelle mit [[Datei: eingebunden")
                .ToList();

            Assert.True(fehler.Count == 0, string.Join(Environment.NewLine, fehler));
        }

        [Fact]
        public void Png_halten_Breite_und_Groesse()
        {
            var fehler = new List<string>();
            foreach (string pfad in Bilddateien().Where(p => p.EndsWith(".png", StringComparison.OrdinalIgnoreCase)))
            {
                string name = Path.GetFileName(pfad);
                long bytes = new FileInfo(pfad).Length;
                if (bytes > PNG_BYTES_MAX)
                    fehler.Add(name + ": " + bytes + " Byte, erlaubt " + PNG_BYTES_MAX);

                int breite = PngBreite(pfad);
                if (breite < 0)
                    fehler.Add(name + ": kein gültiger PNG-Kopf (IHDR)");
                else if (breite > PNG_BREITE_MAX)
                    fehler.Add(name + ": " + breite + " px breit, erlaubt " + PNG_BREITE_MAX);
            }

            Assert.True(fehler.Count == 0, string.Join(Environment.NewLine, fehler));
        }

        [Fact]
        public void Svg_ohne_verbotene_Inhalte()
        {
            var fehler = new List<string>();
            foreach (string pfad in Bilddateien().Where(p => p.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)))
            {
                string text = File.ReadAllText(pfad, Encoding.UTF8);
                foreach (var (muster, meldung) in SvgVerbote)
                    if (muster.IsMatch(text))
                        fehler.Add(Path.GetFileName(pfad) + ": " + meldung);
            }

            Assert.True(fehler.Count == 0, string.Join(Environment.NewLine, fehler));
        }

        // ------------------------------------------------------------------------------------

        private static IEnumerable<string> Bilddateien()
        {
            string ordner = Path.Combine(Arbeitsbaum(), "Projekte", "Wiki", "Dateien");
            Assert.True(Directory.Exists(ordner), "Ordner fehlt: " + ordner);
            return Directory.GetFiles(ordner).OrderBy(p => p, StringComparer.Ordinal).ToList();
        }

        private static List<(int Nummer, string Name, string RepoPfad)> TsvZeilen(string wurzel)
        {
            string datei = Path.Combine(wurzel, "Werkzeuge", "WikiUpload", "dateien.tsv");
            Assert.True(File.Exists(datei), "dateien.tsv fehlt: " + datei);
            var zeilen = new List<(int, string, string)>();
            string[] alle = File.ReadAllLines(datei, Encoding.UTF8);
            for (int i = 0; i < alle.Length; i++)
            {
                string z = alle[i].TrimEnd('\r');
                if (z.Trim().Length == 0 || z.TrimStart().StartsWith("#", StringComparison.Ordinal)) continue;
                string[] teile = z.Split('\t');
                Assert.True(teile.Length >= 2, "dateien.tsv Zeile " + (i + 1) + ": weniger als zwei Spalten");
                zeilen.Add((i + 1, teile[0].Trim(), teile[1].Trim()));
            }

            return zeilen;
        }

        /// <summary>Breite aus dem IHDR-Block; −1, wenn Signatur oder Block fehlen.</summary>
        private static int PngBreite(string pfad)
        {
            byte[] kopf = new byte[24];
            using (var s = File.OpenRead(pfad))
            {
                if (s.Read(kopf, 0, kopf.Length) != kopf.Length) return -1;
            }

            byte[] signatur = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            if (!kopf.Take(8).SequenceEqual(signatur)) return -1;
            if (Encoding.ASCII.GetString(kopf, 12, 4) != "IHDR") return -1;
            return (kopf[16] << 24) | (kopf[17] << 16) | (kopf[18] << 8) | kopf[19];
        }

        /// <summary>MediaWiki-Dateinamen: Leerzeichen und Unterstrich gleich, erster Buchstabe groß.</summary>
        private static string Normalform(string name)
        {
            string n = name.Trim().Replace(' ', '_');
            return n.Length == 0 ? n : char.ToUpperInvariant(n[0]) + n.Substring(1);
        }

        /// <summary>Die Wurzel des Arbeitsbaums — derselbe Weg wie in <c>RepositoryOrdnungWacheTests</c>.</summary>
        private static string Arbeitsbaum([CallerFilePath] string eigeneDatei = null)
        {
            if (!string.IsNullOrEmpty(eigeneDatei))
            {
                string ordner = Path.GetDirectoryName(eigeneDatei);
                string wurzel = ordner == null ? null : Path.GetDirectoryName(ordner);
                if (wurzel != null && File.Exists(Path.Combine(wurzel, "WP-Plan.sln"))) return wurzel;
            }

            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null)
            {
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
                d = d.Parent;
            }

            Assert.Fail("Die Wurzel des Arbeitsbaums (WP-Plan.sln) ist nicht zu finden.");
            return null;
        }
    }
}
