using System.Globalization;
using WindowsFormsApplication1;

namespace Rasterprobe.Wirt.Seiten;

/// <summary>
/// Die synthetischen Zeilen der Rasterprobe (#235) — in Aufbau und
/// Textlaengen wie die Saetze der CEC-Speicherliste, damit das Raster
/// dieselben Spaltenbreiten und dieselbe Zeilenhoehe bekommt.
///
/// <para>Deterministisch (fester Startwert): Zwei Laeufe der Probe messen
/// dieselbe Seite.</para>
/// </summary>
public static class Zeilenbau
{
    private static readonly string[] Hersteller =
    {
        "SunPower Corporation", "LG Electronics Inc.", "Tesla, Inc.",
        "Enphase Energy Inc.", "Generac Power Systems", "BYD Company Limited",
        "sonnen GmbH", "Panasonic Corporation of North America",
        "FranklinWH Energy Storage Inc.", "SolarEdge Technologies Ltd.",
        "Pylontech", "Villara Energy Systems", "Electriq Power Inc.",
        "HomeGrid / Lion Energy", "Fortress Power LLC"
    };

    private static readonly string[] Chemie =
    { "LFP", "NMC", "LTO", "NCA", "Blei-Gel", "Natrium-Ionen" };

    private static readonly string[] Quellen = { "CEC", "bslib" };

    /// <summary>Baut <paramref name="anzahl"/> Zeilen mit den acht Spalten des Stromspeicherprofils.</summary>
    public static IReadOnlyList<Katalogfilterzeile> Bauen(int anzahl)
    {
        var zufall = new Random(20260912);
        var liste = new List<Katalogfilterzeile>(anzahl);

        for (int i = 0; i < anzahl; i++)
        {
            string firma = Hersteller[i % Hersteller.Length];
            string modell = string.Create(CultureInfo.InvariantCulture,
                $"ESS-{1000 + (i % 900)}{(char)('A' + (i % 26))} {(i % 7) + 1}-Phase");
            double energie = Math.Round(2.5 + zufall.NextDouble() * 60.0, 1);
            double leistung = Math.Round(1.0 + zufall.NextDouble() * 25.0, 1);
            double eta = Math.Round(0.82 + zufall.NextDouble() * 0.15, 3);

            liste.Add(new Katalogfilterzeile(i + 1, firma + " " + modell)
                .MitText("EINTRAG", firma + " " + modell)
                .MitText("QUELLE", Quellen[i % Quellen.Length])
                .MitText("FIRMA", firma)
                .MitText("MODELL", modell)
                .MitZahl("ENERGIE", energie)
                .MitZahl("LEISTUNG", leistung)
                .MitZahl("ETA", eta, 3)
                .MitText("TYP", Chemie[i % Chemie.Length]));
        }

        return liste;
    }
}
