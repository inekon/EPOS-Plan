using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Wächter über die FARBEN der Diagramme (DG-E5, Anwenderentscheid DG-Q8:
    /// „Jede Reihe bekommt einen Wähler").
    ///
    /// <para><b>Die Regel.</b> Die Farbe einer Größe ist eine <c>Farbrolle</c> und
    /// steht in der Palette. Wer sie stattdessen als festen Wert schreibt, nimmt dem
    /// Legendeneintrag seinen Farbwähler und der Einstellung ihre Wirkung: Das Bild
    /// zeigt dann eine Farbe, auf die keine Rolle zeigt — und wo der Wert zufällig
    /// eine Hausfarbe trifft, färbt der Anwender beim Wählen die falsche Größe.</para>
    ///
    /// <para><b>Der Geltungsbereich</b> ist alles, was ein Bild BESTELLT, aber nicht
    /// malt: die Hüllen (<c>EPOS.UI.Daten</c>), die Oberfläche (<c>EPOS.UI</c>), die
    /// Hüllen der Windows-Schale (<c>WindowsFormsApplication1/Views</c>), die
    /// Fachcontroller (<c>EPOS.Kern/Controller</c>) und die zwei Bildbauer des Kerns,
    /// die ihre Reihen selbst zusammenstellen (<c>PeakShavingBild</c>,
    /// <c>SpeicherBetriebsbild</c>).</para>
    ///
    /// <para><b>Was ausdrücklich AUSGENOMMEN ist</b> — dort gehört der Farbwert hin:
    /// der Renderer <c>ChartRenderer</c> mit seinen <c>C_*</c>-Hausfarben, die Brücke
    /// <c>SkiaMaler</c>/<c>SkiaBruecke</c> zwischen Modell und SkiaSharp, die
    /// <c>Farbpalette</c> samt <c>Diagrammfarben</c> und die Prüfstände
    /// (<c>Proben/</c>), die den Renderer gegen feste Werte messen. Sie liegen alle
    /// ausserhalb des Geltungsbereichs; eine Ausnahmeliste INNERHALB gibt es nicht,
    /// und sie soll leer bleiben.</para>
    ///
    /// <para><b>Kommentarzeilen zählen nicht:</b> Ein Verweis auf eine frühere Farbe in
    /// einer Erläuterung ist kein Zeichenbefehl. Geprüft wird der Quelltext ohne
    /// <c>//</c>- und <c>///</c>-Zeilen.</para>
    /// </summary>
    public sealed class DiagrammfarbenWacheTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Die Ordner, in denen keine feste Farbe stehen darf (repo-relativ).</summary>
        private static readonly string[] Ordner =
        {
            "EPOS.UI.Daten",
            "EPOS.UI",
            "WindowsFormsApplication1/Views",
            "EPOS.Kern/Controller"
        };

        /// <summary>Die zwei Bildbauer des Kerns, die ihre Reihen selbst zusammenstellen.</summary>
        private static readonly string[] Einzeldateien =
        {
            "EPOS.Kern/Allgemein/Bericht/PeakShavingBild.cs",
            "EPOS.Kern/Allgemein/Bericht/SpeicherBetriebsbild.cs"
        };

        /// <summary>Bauordner — dort steht erzeugter Quelltext, kein Bestand.</summary>
        private static readonly string[] AusgenommeneOrdnernamen = { "bin", "obj", "node_modules" };

        /// <summary>Die verbotenen Schreibweisen, je mit ihrem Namen für die Meldung.</summary>
        private static readonly (string Name, Regex Muster)[] Verboten =
        {
            ("SKColors.",    new Regex(@"\bSKColors\s*\.",  RegexOptions.Compiled)),
            ("new SKColor(", new Regex(@"\bnew\s+SKColor\s*\(", RegexOptions.Compiled))
        };

        // =====================================================================
        //  Der Wächter
        // =====================================================================

        /// <summary>
        /// In den Hüllen, in der Oberfläche, in den Views der Windows-Schale, in den
        /// Fachcontrollern und in den zwei Bildbauern des Kerns steht keine feste
        /// Skia-Farbe mehr — jede Reihe nennt ihre Farbrolle.
        /// </summary>
        [Fact]
        public void Keine_festen_Skia_Farben_ausserhalb_von_Renderer_und_Palette()
        {
            var funde = new List<string>();

            foreach (string datei in Quelldateien())
                foreach ((string name, Regex muster) in Verboten)
                    Treffer(datei, name, muster, funde);

            Assert.True(funde.Count == 0,
                "Die Farbe einer Groesse ist eine Farbrolle und steht in der Palette " +
                "(DG-E5, Anwenderentscheid DG-Q8). Ein fester Farbwert nimmt dem " +
                "Legendeneintrag seinen Waehler; die Reihe bekommt statt dessen einen " +
                "Konstruktor mit Farbrolle:\n" + string.Join("\n", funde));
        }

        /// <summary>
        /// <b>Die Wache misst wirklich etwas.</b> Sie findet über den geprüften Bestand
        /// Dateien — läuft sie an einer falschen Wurzel ins Leere, wäre sie still grün.
        /// </summary>
        [Fact]
        public void Die_Wache_prueft_einen_belegten_Bestand()
        {
            List<string> dateien = Quelldateien();

            Assert.True(dateien.Count > 100,
                "Die Wache findet nur " + dateien.Count + " Dateien - die Wurzel stimmt nicht.");

            foreach (string einzeln in Einzeldateien)
                Assert.Contains(dateien, d => d.Replace(Path.DirectorySeparatorChar, '/')
                                               .EndsWith(einzeln, StringComparison.Ordinal));
        }

        // =====================================================================
        //  Die Hilfsmittel
        // =====================================================================

        /// <summary>Jede Zeile ohne Kommentar, die ein verbotenes Muster trägt.</summary>
        private static void Treffer(string datei, string name, Regex muster, List<string> funde)
        {
            string[] zeilen = File.ReadAllLines(datei);
            for (int i = 0; i < zeilen.Length; i++)
            {
                string zeile = zeilen[i];
                string ohneRand = zeile.TrimStart();
                if (ohneRand.StartsWith("//", StringComparison.Ordinal)) continue;
                if (ohneRand.StartsWith("*", StringComparison.Ordinal)) continue;
                if (!muster.IsMatch(zeile)) continue;

                funde.Add(Relativ(datei) + ":" + (i + 1) + "  " + name + "  " + ohneRand.Trim());
            }
        }

        /// <summary>Alle geprüften Quelldateien: <c>.cs</c> und <c>.razor</c>.</summary>
        private static List<string> Quelldateien()
        {
            string wurzel = Arbeitsbaum();
            var dateien = new List<string>();

            foreach (string ordner in Ordner)
            {
                string voll = Path.Combine(wurzel, ordner.Replace('/', Path.DirectorySeparatorChar));
                if (Directory.Exists(voll)) Sammle(voll, dateien);
            }

            foreach (string einzeln in Einzeldateien)
            {
                string voll = Path.Combine(wurzel, einzeln.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(voll)) dateien.Add(voll);
            }

            return dateien;
        }

        private static void Sammle(string ordner, List<string> dateien)
        {
            foreach (string datei in Directory.EnumerateFiles(ordner))
            {
                string endung = Path.GetExtension(datei);
                if (string.Equals(endung, ".cs", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(endung, ".razor", StringComparison.OrdinalIgnoreCase))
                    dateien.Add(datei);
            }

            foreach (string unter in Directory.EnumerateDirectories(ordner))
            {
                string name = Path.GetFileName(unter);
                if (AusgenommeneOrdnernamen.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;
                Sammle(unter, dateien);
            }
        }

        private static string Relativ(string voll)
        {
            string wurzel = Arbeitsbaum();
            return voll.StartsWith(wurzel, StringComparison.OrdinalIgnoreCase)
                 ? voll.Substring(wurzel.Length).TrimStart(Path.DirectorySeparatorChar, '/')
                       .Replace(Path.DirectorySeparatorChar, '/')
                 : voll;
        }

        /// <summary>Die Wurzel des Arbeitsbaums — derselbe Weg wie in den übrigen Wachen.</summary>
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
