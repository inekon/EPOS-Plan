using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace SvgProbe
{
    /// <summary>
    /// SvgProbe (Konzept DG-1) — misst, was ein SVG aus C# fuer einen Jahresgang
    /// KOSTET: Punkte, Bytes (roh und gzip), Erzeugungszeit, Determinismus.
    ///
    /// <para>Aufruf: <c>dotnet run --project Proben/SvgProbe -c Release -- --ziel &lt;ordner&gt;</c>.
    /// Der Ordner bekommt je Variante eine .svg zum Ansehen; ohne <c>--ziel</c> wird
    /// nur gemessen. Rueckgabe 0, wenn jede Variante zweimal byte-gleich erzeugt
    /// wird, sonst 1.</para>
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try { Console.OutputEncoding = new UTF8Encoding(false); } catch { }

            string? ziel = Arg(args, "--ziel");
            int wiederholungen = int.Parse(Arg(args, "--wiederholungen") ?? "7", CultureInfo.InvariantCulture);
            if (ziel != null) Directory.CreateDirectory(ziel);

            Console.WriteLine("SvgProbe DG-1 — Jahresgang 1304x440, Zeichenflaeche 1154 px breit, 8 760 Stunden je Reihe");
            Console.WriteLine();
            Console.WriteLine("Variante     Reihen  Punkte/Reihe  Bytes      KB     gzip KB  Erzeugung ms (Median)  byte-gleich");

            int verstoesse = 0;
            foreach (int anzahl in new[] { 1, 3, 6 })
            {
                double[][] reihen = SvgZeichner.Jahresreihen(anzahl);
                foreach (Pfadart art in new[] { Pfadart.Roh, Pfadart.JederNte, Pfadart.Gebuendelt })
                {
                    var zeiten = new List<double>();
                    string svg = "";
                    for (int k = 0; k < wiederholungen; k++)
                    {
                        var uhr = Stopwatch.StartNew();
                        svg = SvgZeichner.Svg(reihen, art);
                        uhr.Stop();
                        zeiten.Add(uhr.Elapsed.TotalMilliseconds);
                    }
                    zeiten.Sort();
                    double median = zeiten[zeiten.Count / 2];

                    string zweitens = SvgZeichner.Svg(reihen, art);
                    bool gleich = string.Equals(svg, zweitens, StringComparison.Ordinal);
                    if (!gleich) verstoesse++;

                    byte[] bytes = Encoding.UTF8.GetBytes(svg);
                    int punkte = SvgZeichner.Punktzahl(reihen[0], art, SvgZeichner.FL_B);

                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "{0,-12} {1,6}  {2,12}  {3,9}  {4,6:0.0}  {5,7:0.0}  {6,21:0.00}  {7}",
                        SvgZeichner.Name(art), anzahl, punkte, bytes.Length, bytes.Length / 1024.0,
                        Gzip(bytes) / 1024.0, median, gleich ? "ja" : "NEIN"));

                    if (ziel != null)
                    {
                        string name = string.Format(CultureInfo.InvariantCulture, "jahresgang_{0}_{1}reihen.svg",
                            SvgZeichner.Name(art).Replace(" ", "").Replace("-", ""), anzahl);
                        File.WriteAllBytes(Path.Combine(ziel, name), bytes);
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine(verstoesse == 0
                ? "ERGEBNIS: jede Variante zweimal byte-gleich."
                : "ERGEBNIS: " + verstoesse + " Variante(n) NICHT deterministisch.");
            return verstoesse == 0 ? 0 : 1;
        }

        private static long Gzip(byte[] bytes)
        {
            using (var aus = new MemoryStream())
            {
                using (var gz = new GZipStream(aus, CompressionLevel.Optimal, true))
                    gz.Write(bytes, 0, bytes.Length);
                return aus.Length;
            }
        }

        private static string? Arg(string[] args, string name)
        {
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }
}
