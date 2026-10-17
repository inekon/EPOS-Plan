using System.Globalization;
using EPOS.UI.Dialoge.Bedarf;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <b>Der Farbmodus „Befund“ als Daten</b> (Abstimmung G5, B1; G5-3): die Farbtafel samt Kontrast der Schrift auf ihren
/// Farben und die Befundbytes im Körperfeld — gebaut wie die Stufenbytes des Farbmodus „Aufbau“ (BA-3). Die Razor-Seite
/// folgt in einem eigenen Schritt; hier gelten die Daten.
/// </summary>
public class GebaeudeBefundTests
{
    private const string ZONE = "A1|EG|B";

    private static IReadOnlyList<IReadOnlyList<GebaeudeAnsichtPunkt>> Polygon(params (double X, double Y)[] punkte)
        => new[] { (IReadOnlyList<GebaeudeAnsichtPunkt>)punkte.Select(p => new GebaeudeAnsichtPunkt(p.X, p.Y)).ToList() };

    /// <summary>Ein Raum mit drei Dreiecken (R1 Außenwand, R3 Boden, R0 innen) und ein Fensterkörper — wie in <see cref="GebaeudeAufbauTests"/>.</summary>
    private static GebaeudeAnsichtDaten Daten(bool mitBefund)
    {
        var raum = new GebaeudeAnsichtRaum("r-wohnen", "Wohnen", ZONE, true, false, "48 m²", Polygon((0, 0), (6, 0), (6, 8), (0, 8)));
        var datei = new GebaeudeAnsichtDateikoerper(new float[] { 0, 0, 0, 6, 0, 0, 6, 8, 0, 0, 8, 0 }, new[] { 0, 1, 2, 0, 2, 3, 0, 1, 3 },
                                                    new[] { 0, 1, 1, 2, 2, 3, 0, 3 }, 3, "FacetedBrep", Array.Empty<string>())
        { Gruppen = new byte[] { 1, 3, 0 } };
        var fenster = new GebaeudeAnsichtDateikoerper(new float[] { 1.5f, 0, 1, 3, 0, 1, 3, 0, 2 }, new[] { 0, 1, 2 }, new[] { 0, 1, 1, 2 }, 1,
                                                      "FacetedBrep", Array.Empty<string>());
        var d = new GebaeudeAnsichtDaten
        {
            Geschosse = new[] { new GebaeudeAnsichtGeschoss("g-eg", "Erdgeschoss", false, 0, 0, 6, 8, new[] { raum }) },
            Zonen = new[] { new GebaeudeAnsichtZone(ZONE, "Erdgeschoss", 0, true, false, false) },
            Koerperraeume = new[]
            {
                new GebaeudeAnsichtKoerperraum("r-wohnen", 2.75, new[] { (IReadOnlyList<string?>)new string?[4] }, null, null)
                {
                    Dateikoerper = datei, Herkunft = Koerperherkunft.Datei,
                    Dreiecksbauteile = new string?[] { "aw-sued", "bo-1", null },
                },
            },
            Flaechengruppen = Enumerable.Range(0, GebaeudeAnsichtRandgruppen.ZAHL)
                .Select(i => new GebaeudeAnsichtFlaechengruppe((Randgruppe)i, i is 1 or 3 ? 48 : 0, i is 1 or 3 ? 1 : 0)).ToList(),
            Bauteilkoerper = new[] { new GebaeudeAnsichtBauteilkoerper("f-1", Randgruppe.R7, fenster) },
        };
        if (!mitBefund) return d;
        return d with
        {
            Bauteilbefunde = new Dictionary<string, Bauteilbefundstufe>
            {
                ["aw-sued"] = Bauteilbefundstufe.KoerperUnlesbar, ["bo-1"] = Bauteilbefundstufe.OhneEigenschaften, ["f-1"] = Bauteilbefundstufe.Ohne,
            },
        };
    }

    [Fact]
    public void Befundbytes_stehen_hinter_den_Gruppen_und_fehlen_ohne_Befund()
    {
        GebaeudeAnsichtKoerperfeld f0 = Daten(mitBefund: false).Koerperfeld();
        Assert.Equal(-1, f0.Verzeichnis[0].BefundAb);
        Assert.Equal(-1, f0.Bauteile[0].Befund);

        GebaeudeAnsichtDaten d = Daten(mitBefund: true);
        Assert.True(d.HatBefunde);
        GebaeudeAnsichtKoerperfeld f = d.Koerperfeld();
        // Drei Befundbytes, auf vier Byte aufgefüllt; die Teile davor bleiben byteweise gleich.
        Assert.Equal(f0.Bytes.Length + 4, f.Bytes.Length);
        Assert.Equal(f0.Bytes.Take(f0.Verzeichnis[0].GruppenAb + 3), f.Bytes.Take(f0.Verzeichnis[0].GruppenAb + 3));
        int ab = f.Verzeichnis[0].BefundAb;
        Assert.True(ab > f.Verzeichnis[0].GruppenAb);
        // R1 mit unlesbarem Körper → 1, R3 ohne Eigenschaften → 2, R0 → neutrale Innenfläche.
        Assert.Equal(new byte[] { 1, 2, GebaeudeAnsichtBefundstufen.INNEN_NEUTRAL }, f.Bytes.Skip(ab).Take(3));
        Assert.Equal((int)Bauteilbefundstufe.Ohne, f.Bauteile[0].Befund);
        Assert.Equal(Bauteilbefundstufe.OhneBauteil, d.BefundVon("unbekannt"));
        Assert.Equal(GebaeudeAnsichtBefundstufen.KEINE, d.Befundbyte(255, "aw-sued"));
    }

    [Fact]
    public void Farbtafel_rot_orange_grau_mit_den_Toenen_der_Tafel_Aufbau()
    {
        Assert.Equal(GebaeudeAnsichtBefundstufen.ZAHL + 1, GebaeudeAnsichtBefundstufen.FARBEN.Count);
        Assert.Equal("#d62728", GebaeudeAnsichtBefundstufen.Farbe(Bauteilbefundstufe.OhneEigenschaften));
        Assert.Equal("#f07f13", GebaeudeAnsichtBefundstufen.Farbe(Bauteilbefundstufe.KoerperUnlesbar));
        Assert.Equal("#9e9e9e", GebaeudeAnsichtBefundstufen.Farbe(Bauteilbefundstufe.Ohne));
        // Orange = Stufe C, Hellgrau = ohne Bauteil, Grau = neutrale Innenfläche der Tafel „Aufbau“.
        Assert.Equal(GebaeudeAnsichtAufbaustufen.Farbe(Aufbaustufe.C), GebaeudeAnsichtBefundstufen.Farbe(Bauteilbefundstufe.KoerperUnlesbar));
        Assert.Equal(GebaeudeAnsichtAufbaustufen.Farbe(Aufbaustufe.OhneBauteil), GebaeudeAnsichtBefundstufen.Farbe(Bauteilbefundstufe.OhneBauteil));
        Assert.Equal(GebaeudeAnsichtAufbaustufen.FARBEN[GebaeudeAnsichtAufbaustufen.INNEN_NEUTRAL],
                     GebaeudeAnsichtBefundstufen.FARBEN[GebaeudeAnsichtBefundstufen.INNEN_NEUTRAL]);
        // Die Stufen der Legende sind verschieden, die Schlüssel eindeutig.
        var stufen = Enum.GetValues<Bauteilbefundstufe>();
        Assert.Equal(stufen.Length, stufen.Select(GebaeudeAnsichtBefundstufen.Farbe).Distinct().Count());
        Assert.Equal(stufen.Length, stufen.Select(GebaeudeAnsichtBefundstufen.Schluessel).Distinct().Count());
        Assert.True(GebaeudeAnsichtBefundstufen.MitBefund(Bauteilbefundstufe.KoerperUnlesbar));
        Assert.False(GebaeudeAnsichtBefundstufen.MitBefund(Bauteilbefundstufe.Ohne));
    }

    /// <summary>Hausblatt: Schrift auf einer Fläche hält 4,5 : 1 (WCAG-Kontrast aus der relativen Leuchtdichte).</summary>
    [Fact]
    public void Schrift_auf_jeder_Befundfarbe_haelt_4_5_zu_1()
    {
        foreach (Bauteilbefundstufe s in Enum.GetValues<Bauteilbefundstufe>())
        {
            double k = Kontrast(GebaeudeAnsichtBefundstufen.Farbe(s), GebaeudeAnsichtBefundstufen.Schriftfarbe(s));
            Assert.True(k >= 4.5, s + ": " + k.ToString("0.00", CultureInfo.InvariantCulture));
        }
        // Rot und Orange unterscheiden sich auch in der Helligkeit (Graustufendruck).
        Assert.True(Kontrast(GebaeudeAnsichtBefundstufen.Farbe(Bauteilbefundstufe.OhneEigenschaften),
                             GebaeudeAnsichtBefundstufen.Farbe(Bauteilbefundstufe.KoerperUnlesbar)) >= 1.5);
    }

    private static double Kontrast(string a, string b)
    {
        double la = Leuchtdichte(a), lb = Leuchtdichte(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static double Leuchtdichte(string farbe)
    {
        double Kanal(int stelle)
        {
            double c = int.Parse(farbe.Substring(stelle, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Kanal(1) + 0.7152 * Kanal(3) + 0.0722 * Kanal(5);
    }
}
