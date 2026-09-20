using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Wächter über die ETAPPE E1 des Diagrammkonzepts: Der <c>ChartRenderer</c>
    /// zeichnet nicht mehr, er BESCHREIBT.
    ///
    /// <para><b>Die Regel.</b> Jede der 26 Zeichenmethoden füllt ein
    /// <c>Zeichenmodell</c> — eine Liste unveränderlicher Befehle mit fertigen
    /// Koordinaten — und gibt es an <c>SkiaMaler.Png</c>. Im Renderer steht deshalb
    /// KEIN Zeichenaufruf einer Grafikbibliothek mehr: kein <c>SKCanvas</c>, kein
    /// <c>SKPaint</c>, kein <c>SKPath</c>, keine Zeichenfläche, kein
    /// <c>SKColors</c>-Wert. Nur so kann ein zweiter Ausgabeweg (SVG, Etappe E2)
    /// dasselbe Modell lesen, ohne dass der Renderer ein zweites Mal geschrieben
    /// wird.</para>
    ///
    /// <para><b>Warum ein Wächter und nicht nur ein Aufräumen.</b> Die Umstellung
    /// ändert kein Bild — die Hash-Messlatte der ChartProben ist der Nachweis. Genau
    /// deshalb fällt ein Rückfall NICHT auf: Ein neuer Zeichenaufruf auf einer
    /// Leinwand liefert dasselbe PNG wie der Modellweg und bliebe im Bildvergleich
    /// unsichtbar. Erst der SVG-Weg stieße darauf, und dann wäre die Ursache ein
    /// Jahr alt.</para>
    ///
    /// <para><b>Was ausdrücklich BLEIBEN darf.</b> Drei Skia-Typen sind reine
    /// WERTTYPEN und zeichnen nichts: <c>SKColor</c> (die Farbfelder der öffentlichen
    /// Fläche und <c>Reihe.Farbe</c>), <c>SKRect</c> (das Zeichenrechteck des Layouts)
    /// und <c>SKPoint</c> (eine gerechnete Bildkoordinate). Ihr Ersatz durch die
    /// Modelltypen berührt jede Hülle und gehört in eine eigene Etappe. Die
    /// TEXTVERMESSUNG bleibt ebenfalls eine Kern-Funktion — sie steht aber in
    /// <c>Zeichnung/SkiaMaler.cs</c> (<c>Schriftkette</c>, <c>Schriftmass</c>) und
    /// nicht mehr im Renderer.</para>
    /// </summary>
    public sealed class ZeichenmodellWacheTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Die Skia-Bezeichner, die im Renderer stehen dürfen — und nur sie.</summary>
        private static readonly string[] ErlaubteSkiaTypen = { "SKColor", "SKRect", "SKPoint" };

        /// <summary>
        /// Im <c>ChartRenderer</c> steht kein Skia-Zeichenbefehl mehr — weder als
        /// Aufruf noch als Typ, noch im Kommentar.
        /// </summary>
        [Fact]
        public void DerRendererRuftKeineSkiaZeichenbefehle()
        {
            string text = RendererQuelle();

            var treffer = Regex.Matches(text, @"\bSK[A-Z][A-Za-z]*")
                               .Select(m => m.Value)
                               .Where(name => !ErlaubteSkiaTypen.Contains(name))
                               .Distinct()
                               .OrderBy(n => n, StringComparer.Ordinal)
                               .ToArray();

            Assert.True(treffer.Length == 0,
                "Der ChartRenderer nennt wieder Skia-Zeichentypen: " + string.Join(", ", treffer) +
                ". Jede Zeichenmethode füllt ein Zeichenmodell; gemalt wird in SkiaMaler.");
        }

        /// <summary>
        /// Auch ohne Skia-Typ im Namen: Es gibt im Renderer keine Leinwand, keinen
        /// Pinsel und keinen <c>Draw…</c>-Aufruf mehr.
        /// </summary>
        [Fact]
        public void DerRendererHaeltKeineLeinwandUndKeinenPinsel()
        {
            string text = RendererQuelle();

            foreach (string verboten in new[] { ".DrawLine(", ".DrawRect(", ".DrawCircle(",
                                                ".DrawOval(", ".DrawPath(", ".DrawText(",
                                                "ClipRect(", "Canvas", "PathEffect" })
                Assert.False(text.Contains(verboten, StringComparison.Ordinal),
                             "Der ChartRenderer enthält wieder „" + verboten + "\" — " +
                             "gezeichnet wird über das Zeichenmodell, nicht unmittelbar.");
        }

        /// <summary>
        /// Jede Zeichenmethode gibt ihr Modell an den Maler: 26 öffentliche
        /// Bildmethoden, und keine eigene Zeichenfläche mehr.
        /// </summary>
        [Fact]
        public void JedeZeichenmethodeGibtIhrModellAnDenMaler()
        {
            string text = RendererQuelle();

            int bildmethoden = Regex.Matches(text, @"public static byte\[\] ").Count;
            Assert.Equal(26, bildmethoden);

            // Kein Bild entsteht mehr auf einer eigenen Flaeche; jedes geht durch
            // SkiaMaler.Png - auch die vier gemeinsamen Rumpfmethoden.
            Assert.DoesNotContain("Start(W, H)", text, StringComparison.Ordinal);
            Assert.True(Regex.Matches(text, @"SkiaMaler\.Png\(").Count >= bildmethoden,
                        "Es gibt weniger Übergaben an SkiaMaler.Png als Bildmethoden.");
        }

        /// <summary>
        /// Der ÜBERGANGSWEG der Etappe E1 ist weg: Es gibt kein Zeichenziel mehr, das
        /// einen Befehl unmittelbar auf eine Leinwand malt.
        /// </summary>
        [Fact]
        public void DerUebergangswegIstEntfernt()
        {
            string wurzel = Arbeitsbaum();
            string[] quellen = Directory
                .EnumerateFiles(Path.Combine(wurzel, "EPOS.Kern"), "*.cs", SearchOption.AllDirectories)
                .Where(p => p.Contains("SkiaZiel", StringComparison.Ordinal) ||
                            File.ReadAllText(p).Contains("SkiaZiel", StringComparison.Ordinal))
                .ToArray();

            Assert.True(quellen.Length == 0,
                "Das Übergangsziel SkiaZiel lebt noch in: " +
                string.Join(", ", quellen.Select(p => Path.GetFileName(p))));
        }

        private static string RendererQuelle()
        {
            string pfad = Path.Combine(Arbeitsbaum(), "EPOS.Kern", "Allgemein", "Bericht",
                                       "ChartRenderer.cs");
            Assert.True(File.Exists(pfad), "ChartRenderer.cs ist nicht zu finden: " + pfad);
            return File.ReadAllText(pfad);
        }

        /// <summary>Die Wurzel des Arbeitsbaums — derselbe Weg wie in den übrigen Wächtern.</summary>
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
