using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests;

/// <summary>
/// Das Zeichenmodell der Versionsbalken (<see cref="ChartRenderer.VersionsbalkenModell"/>): ein Balken je Version mit Wert,
/// keiner ohne Wert, Randstrich an der Abweichung, die Einheit an der Achse, negative Werte links der Null, und das Bild
/// lässt sich malen.
/// </summary>
public class VersionsbalkenModellTests
{
    private static readonly Versionsbalkenserie[] s_serien =
    {
        new() { Name = "Stamm", Rolle = Farbrolle.STAMM },
        new() { Name = "V1", Rolle = Farbrolle.SERIE_1 },
        new() { Name = "V2", Rolle = Farbrolle.SERIE_2 }
    };

    private static Versionsbalkenzeile Zeile(string name, double? a, double? b, double? c) => new()
    {
        Bezeichnung = name,
        Werte = new[] { a, b, c },
        Texte = new[] { "a", "b", "c" },
        Zeigertexte = new[] { "Stamm · " + name, "V1 · " + name, "V2 · " + name },
        WeichtAb = new[] { false, true, false }
    };

    [Fact]
    public void Ein_Balken_je_Version_mit_Wert_und_Randstrich_an_der_Abweichung()
    {
        Zeichenmodell m = ChartRenderer.VersionsbalkenModell("MWh/a", s_serien,
            new[] { Zeile("Wärme", 100, 110, null), Zeile("Strom", 50, 40, null) });

        Rechteck[] balken(string v) => m.Befehle.OfType<Rechteck>().Where(r => r.Marke == "reihe:" + v && r.Fuellung is not null).ToArray();
        Assert.Equal(2, balken("Stamm").Length);
        Assert.Equal(2, balken("V1").Length);
        Assert.Empty(balken("V2"));
        Assert.All(balken("V1"), r => Assert.Equal(Farbrolle.SERIE_1, r.Fuellung.Ton.Rolle));
        Assert.Equal(2, m.Befehle.OfType<Linie>().Count(l => l.Marke == "reihe:V1"));
        Assert.DoesNotContain(m.Befehle.OfType<Linie>(), l => l.Marke == "reihe:Stamm");
        Assert.Contains(m.Befehle.OfType<Text>(), t => t.Inhalt == "[MWh/a]");
        Assert.Contains(m.Befehle, b => b.Wert == "V1 · Wärme");

        // Der längere Balken gehört zum größeren Wert.
        Assert.True(balken("V1")[0].Breite > balken("Stamm")[0].Breite);
        Assert.NotEmpty(SkiaMaler.Png(m));
    }

    [Fact]
    public void Negative_Werte_stehen_links_der_Null_und_das_Bild_ist_bestimmt()
    {
        Versionsbalkenzeile[] zeilen = { Zeile("Saldo", -20, 30, 10) };
        Zeichenmodell m = ChartRenderer.VersionsbalkenModell("", s_serien, zeilen);
        Rechteck stamm = m.Befehle.OfType<Rechteck>().First(r => r.Marke == "reihe:Stamm" && r.Fuellung is not null);
        Rechteck v1 = m.Befehle.OfType<Rechteck>().First(r => r.Marke == "reihe:V1" && r.Fuellung is not null);
        Assert.True(stamm.X + stamm.Breite <= v1.X + 0.01f);
        Assert.DoesNotContain(m.Befehle.OfType<Text>(), t => t.Inhalt.StartsWith("["));
        Assert.Equal(SkiaMaler.Png(m), SkiaMaler.Png(ChartRenderer.VersionsbalkenModell("", s_serien, zeilen)));
    }
}
