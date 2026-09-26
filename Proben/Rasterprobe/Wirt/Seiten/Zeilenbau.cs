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

    // ================================================================
    //  DIE VOLLEN KATALOGZEILEN (Neuordnung der Administrationsdialoge,
    //  Stufe 1): JEDE Spalte eines Profils belegt, in den TEXTLAENGEN der
    //  Testdatenbank - Heizkessel-Bezeichner bis 76 Zeichen, Firmen bis 34
    //  Zeichen ("... GmbH & Co. KG"), Zahlen mit Tausendertrennzeichen.
    //  Gemessen wird die BREITE der Liste; ein Profil mit leeren Spalten
    //  (Halbgeviertstrich) waere schmaler als die Wirklichkeit und liesse
    //  die Ueberbreite nie sehen. Die Namen sind neutral (Regel
    //  "keine Hersteller- und Produktdaten").
    // ================================================================

    private static readonly string[] Firmen =
    {
        "Nordwerk Heiztechnik GmbH & Co. KG",           // 34 Zeichen - die laengste Firma der Testdatenbank
        "Suedland Energiesysteme GmbH",
        "Westhaus Waermetechnik AG",
        "Ostkamp Kessel- und Anlagenbau GmbH",
        "Mittelwerk GmbH",
        "Alpha"
    };

    private static readonly string[] Namen =
    {
        // 76 Zeichen - der laengste Heizkessel-Bezeichner der Testdatenbank
        "Brennwertkessel Baureihe 2026 Plus mit modulierendem Brenner 3,8-24 kW Typ S",
        "Niedertemperaturkessel Standard 40 kW",
        "Kompaktgeraet 15",
        "Gas-Brennwerttherme Wandgeraet mit Warmwasserbereitung 26 kW",
        "Pelletkessel Automatik 32",
        "Oelkessel Unit 3-stufig 45 kW mit Edelstahlwaermetauscher"
    };

    /// <summary>
    /// Ein Anzeigetext je Spaltenschluessel in realistischer Laenge. Zahlen- und
    /// Kennzeichenspalten fuellt <see cref="Voll"/> selbst.
    /// </summary>
    private static string Textwert(string schluessel, int i) => schluessel switch
    {
        Katalogfilterprofil.SpHersteller => Firmen[i % Firmen.Length],
        Katalogfilterprofil.SpBrennstoff => new[] { "Erdgas H", "Erdgas L", "Fluessiggas", "Heizoel EL", "Holzpellets" }[i % 5],
        Katalogfilterprofil.SpMotortyp => new[] { "Gas-Otto-Motor", "Zuendstrahlmotor", "Mikrogasturbine" }[i % 3],
        Katalogfilterprofil.SpKollektortyp => new[] { "Flachkollektor", "Vakuumroehrenkollektor" }[i % 2],
        Katalogfilterprofil.SpSpeichertyp => new[] { "Pufferspeicher", "Kombispeicher", "Schichtenspeicher" }[i % 3],
        Katalogfilterprofil.SpTechnologie => new[] { "Mono-c-Si", "Multi-c-Si", "CdTe", "CIGS" }[i % 4],
        Katalogfilterprofil.SpHerkunft => new[] { "CEC", "PAN/OND", "Handeingabe" }[i % 3],
        Katalogfilterprofil.SpChemie => new[] { "LFP", "NMC", "Natrium-Ionen" }[i % 3],
        Katalogfilterprofil.SpQuelle => new[] { "Luft-Wasser", "Sole-Wasser", "Wasser-Wasser" }[i % 3],
        Katalogfilterprofil.SpTyp => "Produktion Schicht " + (i % 3 + 1),
        Katalogfilterprofil.SpBeschreibung => "Beschreibung des Satzes mit einigen Worten " + (i + 1),
        // Stufe 5 (V16): die Gebaeudeverwaltung - die laengsten Werte der Testdatenbank.
        Katalogfilterprofil.SpGebaeudeart => new[] { "grosses Mehrfamilienhaus", "Verwaltungsgebaeude", "Einfamilienhaus", "Hotel" }[i % 4],
        Katalogfilterprofil.SpVerwendung => new[] { "Wohngebäude", "Gewerbe+Sonstige" }[i % 2],
        Katalogfilterprofil.SpBaualtersklasse => new[] { "2016 bis 2020", "2002 bis 2009", "1919 bis 1948" }[i % 3],
        _ => "Wert " + (i + 1)
    };

    /// <summary>
    /// <b>Volle Zeilen fuer ein Profil</b>: jede Spalte belegt, Bezeichner und Firmen in
    /// der Laenge der Testdatenbank, jede siebte Zeile geschuetzt (Auslieferungssatz).
    /// </summary>
    public static IReadOnlyList<Katalogfilterzeile> Voll(Katalogfilterprofil profil, int anzahl)
    {
        var liste = new List<Katalogfilterzeile>(anzahl);
        for (int i = 0; i < anzahl; i++)
        {
            // Die laufende Nummer steht MIT im Namen: Die Dialoge halten ihre Wahl
            // am Bezeichner, zwei gleiche Namen brechen den Zeichenlauf ab.
            string basis = Namen[i % Namen.Length];
            string name = basis.Length > 70
                ? basis.Substring(0, 70) + " " + (i + 1).ToString("D5", CultureInfo.InvariantCulture)
                : basis + " (" + (i + 1).ToString("D3", CultureInfo.InvariantCulture) + ")";

            var zeile = new Katalogfilterzeile(i + 1, name) { Geschuetzt = i % 7 == 0 };
            foreach (Katalogspalte spalte in profil.Spalten)
            {
                if (spalte.Schluessel == Katalogfilterprofil.SpBezeichner)
                    zeile.MitText(spalte.Schluessel, name);
                else if (spalte.Art == Katalogspaltenart.Zahl)
                    zeile.MitZahl(spalte.Schluessel, 12.5 + (i * 137) % 2400, 1);
                else if (spalte.Art == Katalogspaltenart.JaNein)
                    zeile.MitKennzeichen(spalte.Schluessel, i % 3 == 0);
                else
                    zeile.MitText(spalte.Schluessel, Textwert(spalte.Schluessel, i));
            }
            liste.Add(zeile);
        }
        return liste;
    }
}
