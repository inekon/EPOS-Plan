using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Namen <c>EPOS.reihe.*</c></b> (Konzept Berichtsvorlagen 4.4, 7.4, Anhang A; Etappe BV-E8): Rasterreihen des
    /// Stammprojekts, auf die ein Diagramm der Anwendervorlage zeigen kann. EPOS schreibt die Zahlen ins Blatt
    /// „Diagrammdaten“ und setzt das <c>RefersTo</c> des Namens auf ihren Bereich.
    /// <list type="bullet">
    /// <item><c>EPOS.reihe.&lt;reihe&gt;</c> — die Stundenreihe (8760) des Zeitreihensatzes, etwa <c>EPOS.reihe.waermebedarf</c>;</item>
    /// <item><c>EPOS.reihe.&lt;reihe&gt;.tage</c> — die Tagesmittel (365);</item>
    /// <item><c>EPOS.reihe.&lt;reihe&gt;.monate</c> — die Monatssummen in MWh (12), bei Temperatur und Füllstand die Monatsmittel;</item>
    /// <item><c>EPOS.reihe.stunden</c>, <c>.tage</c>, <c>.monate</c> — die Achsen dazu (1 … 8760, 1 … 365, Jan … Dez).</item>
    /// </list>
    /// Die Reihennamen sind die Schlüssel des Zeitreihensatzes in Kleinbuchstaben (<see cref="ZeitreihenSatz"/>).
    /// </summary>
    internal static class Excelreihen
    {
        /// <summary>Die Vorsilbe des Schlüssels (ohne <c>EPOS.</c>).</summary>
        internal const string PRAEFIX = "reihe.";

        /// <summary>Die Raster.</summary>
        internal enum Raster { Stunden, Tage, Monate }

        /// <summary>Die Reihen des Zeitreihensatzes, die ein Name nennen kann.</summary>
        internal static readonly IReadOnlyList<string> Reihen = new[]
        {
            ZeitreihenSatz.WAERMEBEDARF, ZeitreihenSatz.TEMPERATUR, ZeitreihenSatz.STROMBEDARF, ZeitreihenSatz.STROMBEDARF_GESAMT,
            ZeitreihenSatz.WP_WAERME, ZeitreihenSatz.WP_STROM, ZeitreihenSatz.HEIZSTAB, ZeitreihenSatz.BHKW_WAERME,
            ZeitreihenSatz.BHKW_STROM, ZeitreihenSatz.BHKW_UEBERSCHUSS, ZeitreihenSatz.KESSEL_WAERME, ZeitreihenSatz.SOLAR_WAERME,
            ZeitreihenSatz.PV_GENUTZT, ZeitreihenSatz.PV_UEBERSCHUSS, ZeitreihenSatz.NETZEINSPEISUNG, ZeitreihenSatz.BATTERIE_EINSPEISUNG,
            ZeitreihenSatz.PV_ABREGELUNG, ZeitreihenSatz.NETZBEZUG, ZeitreihenSatz.WAERMEREST, ZeitreihenSatz.PV_SPEICHER_SOC,
        };

        /// <summary>Ist der Schlüssel ein Reihenname (<c>reihe.…</c>)?</summary>
        internal static bool IstReihe(string schluessel)
        {
            return schluessel != null && schluessel.StartsWith(PRAEFIX, StringComparison.Ordinal);
        }

        /// <summary>
        /// Zerlegt <c>reihe.&lt;reihe&gt;[.tage|.monate]</c>; <paramref name="reihe"/> ist der Schlüssel des Zeitreihensatzes, leer
        /// für eine Achse (<c>reihe.stunden</c> …). <c>false</c> für einen unbekannten Namen.
        /// </summary>
        internal static bool Lies(string schluessel, out string reihe, out Raster raster)
        {
            reihe = null;
            raster = Raster.Stunden;
            if (!IstReihe(schluessel)) return false;
            string rest = schluessel.Substring(PRAEFIX.Length);
            switch (rest)
            {
                case "stunden": reihe = ""; raster = Raster.Stunden; return true;
                case "tage": reihe = ""; raster = Raster.Tage; return true;
                case "monate": reihe = ""; raster = Raster.Monate; return true;
            }
            if (rest.EndsWith(".tage", StringComparison.Ordinal)) { raster = Raster.Tage; rest = rest.Substring(0, rest.Length - 5); }
            else if (rest.EndsWith(".monate", StringComparison.Ordinal)) { raster = Raster.Monate; rest = rest.Substring(0, rest.Length - 7); }
            string gefunden = Reihen.FirstOrDefault(r => string.Equals(r, rest, StringComparison.OrdinalIgnoreCase));
            if (gefunden == null) return false;
            reihe = gefunden;
            return true;
        }

        /// <summary>Die erlaubten Namen als Liste für Meldungen.</summary>
        internal static string Liste()
        {
            return "EPOS.reihe.stunden|tage|monate, EPOS.reihe.<" +
                   string.Join("|", Reihen.Select(r => r.ToLowerInvariant())) + ">[.tage|.monate]";
        }

        /// <summary>
        /// Die Zahlen eines Namens als Datenbereich (eine Reihe, Kategorien je Raster); <c>null</c>, wenn das Stammprojekt die
        /// Reihe nicht trägt.
        /// </summary>
        internal static Exceldiagramm Daten(BerichtsDaten daten, string schluessel, bool englisch)
        {
            if (!Lies(schluessel, out string reihe, out Raster raster)) return null;
            var d = new Exceldiagramm(schluessel, Vorlagenfeldkatalog.PRAEFIX_EXCEL + schluessel)
            {
                Bezug = "Stamm",
                Zahlformat = raster == Raster.Stunden ? "#,##0.0" : "#,##0.00",
                Kategorienkopf = ExcelVorlagentexte.T(englisch, raster == Raster.Monate ? nameof(MyResource.Resource.BV_XL_DG_MONAT)
                    : raster == Raster.Tage ? nameof(MyResource.Resource.BV_XL_DG_TAG) : nameof(MyResource.Resource.BV_XL_DG_STUNDE)),
            };
            int n = raster == Raster.Monate ? 12 : raster == Raster.Tage ? 365 : ZeitreihenSatz.Stunden;
            for (int i = 0; i < n; i++)
                d.Kategorien.Add(raster == Raster.Monate ? (object)ChartRenderer.MONATE[i] : (double)(i + 1));

            if (reihe.Length == 0)
            {
                // Die Achse selbst: die Kategorien als Reihe.
                if (raster == Raster.Monate) return d.Kategorien.Count == 12 ? MitText(d) : null;
                d.Reihe(schluessel, d.Kategorien.Select(k => (double?)(double)k), Excelreihenart.Linie, null);
                return d;
            }

            ZeitreihenSatz z = daten?.Varianten?.FirstOrDefault(v => v.IstStamm)?.Zeitreihen;
            double[] stunden = z?.Hole(reihe);
            if (stunden == null) return null;
            bool mittel = reihe == ZeitreihenSatz.TEMPERATUR || reihe == ZeitreihenSatz.PV_SPEICHER_SOC;
            double[] werte = raster == Raster.Stunden ? stunden
                : raster == Raster.Tage ? ChartRenderer.TagesMittel(stunden)
                : mittel ? Monatsmittel(stunden) : ChartRenderer.MonatsSummenMWh(stunden);
            d.Reihe(reihe.ToLowerInvariant(), Exceldiagramm.Endlich(werte), Excelreihenart.Linie, null);
            return d;
        }

        /// <summary>Die Monatsnamen als Reihe: die Kategorien stehen in der ersten Spalte, der Name zeigt auf sie.</summary>
        private static Exceldiagramm MitText(Exceldiagramm d)
        {
            d.Reihe("monate", new double?[12], Excelreihenart.Linie, null).NurDaten = true;
            return d;
        }

        private static double[] Monatsmittel(double[] stunden)
        {
            int[] tage = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            var r = new double[12];
            int h0 = 0;
            for (int m = 0; m < 12; m++)
            {
                int hn = tage[m] * 24;
                double s = 0;
                int k = 0;
                for (int h = h0; h < h0 + hn && h < stunden.Length; h++) { s += stunden[h]; k++; }
                r[m] = k == 0 ? double.NaN : s / k;
                h0 += hn;
            }
            return r;
        }

        /// <summary>Der Bereich, auf den ein Name zeigt: die Werte der Reihe, bei <c>reihe.monate</c> die Monatsnamen.</summary>
        internal static string Bezug(Diagrammplan.Block block)
        {
            Excelreihe r = block.Diagramm.Reihen[0];
            if (r.NurDaten) return block.Bereich.Kategorien;
            return block.Bereich.Werte(r);
        }

        /// <summary>Die Kennung des Datenbereichs eines Namens im Plan.</summary>
        internal static string Kennung(string schluessel)
        {
            return "name|" + schluessel.ToString(CultureInfo.InvariantCulture);
        }
    }
}
