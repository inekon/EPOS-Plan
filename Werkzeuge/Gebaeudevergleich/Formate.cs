using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;

namespace Gebaeudevergleich
{
    /// <summary>
    /// Die Zahl- und Textformate der Ausgabe. <b>CSV</b>: Semikolon, invariante Zahlen ohne
    /// Exponent, leeres Feld für „keine Zahl". <b>Bericht und Zusammenfassung</b>: de-DE.
    /// </summary>
    internal static class Formate
    {
        private static readonly CultureInfo DE = new CultureInfo("de-DE");

        /// <summary>Eine Zahl für die CSV; NaN, ∞ und <c>null</c> werden leer.</summary>
        internal static string Csv(double? w)
        {
            if (!w.HasValue || !Kennzahlen.Endlich(w.Value)) return "";
            double v = w.Value == 0.0 ? 0.0 : w.Value;   // keine „-0"
            return v.ToString("0.#########", CultureInfo.InvariantCulture);
        }

        internal static string Csv(int? w) => w.HasValue ? w.Value.ToString(CultureInfo.InvariantCulture) : "";

        internal static string Csv(bool w) => w ? "1" : "0";

        /// <summary>Ein Textfeld für die CSV — in Anführungszeichen, sobald es Trenner, Anführungszeichen oder Umbrüche trägt.</summary>
        internal static string CsvText(string s)
        {
            s ??= "";
            s = s.Replace("\r\n", " | ").Replace("\n", " | ").Replace("\r", " | ");
            return s.IndexOfAny(new[] { ';', '"' }) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
        }

        /// <summary>Eine Zahl für den Bericht (de-DE) mit fester Stellenzahl; „—" ohne Zahl.</summary>
        internal static string De(double? w, int stellen = 1)
        {
            if (!w.HasValue || !Kennzahlen.Endlich(w.Value)) return "—";
            double v = Math.Round(w.Value, stellen) == 0.0 ? 0.0 : w.Value;
            return v.ToString("N" + stellen.ToString(CultureInfo.InvariantCulture), DE);
        }

        /// <summary>Eine ganze Zahl für den Bericht (de-DE).</summary>
        internal static string De(int w) => w.ToString("N0", DE);

        /// <summary>HTML-Maskierung.</summary>
        internal static string Html(string s) => WebUtility.HtmlEncode(s ?? "");

        /// <summary>
        /// Quantil nach linearer Interpolation zwischen den Ordnungsstatistiken (Typ 7 nach
        /// Hyndman/Fan, die Vorgabe von R und Excel QUARTILE.INC).
        /// </summary>
        internal static double Quantil(IReadOnlyList<double> sortiert, double p)
        {
            if (sortiert == null || sortiert.Count == 0) return double.NaN;
            if (sortiert.Count == 1) return sortiert[0];
            double h = (sortiert.Count - 1) * p;
            int u = (int)Math.Floor(h);
            int o = Math.Min(u + 1, sortiert.Count - 1);
            return sortiert[u] + (h - u) * (sortiert[o] - sortiert[u]);
        }

        internal static List<double> Sortiert(IEnumerable<double> werte)
            => werte.Where(Kennzahlen.Endlich).OrderBy(w => w).ToList();
    }
}
