using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Standards;
using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// „Wechselrichter vorschlagen" im Abschnitt <c>PvStraengeFelder</c>: Knopf neben der
/// Katalogwahl (kein Delegat, kein Knopf; weich gesperrt mit Grund), die Überlagerung mit
/// der bewerteten Liste, „Übernehmen" setzt Katalogwahl und Herstellerfilter und gibt
/// „Auslegung vorschlagen" frei, „Abbrechen" lässt alles. Die Bewertung selbst rechnet der
/// Kern (<c>WechselrichterVorschlagTests</c>); hier kommt sie über den Delegaten herein.
/// </summary>
public class PvWechselrichterVorschlagTests : EposBunitContext
{
    public PvWechselrichterVorschlagTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static readonly (int Id, string Text, string Firma)[] KATALOG =
    {
        (7, "Muster 2500TL", "Muster"),
        (8, "Muster 5000TL-2M", "Muster"),
        (9, "Fremd 3000X", "Fremd")
    };

    private static readonly string[] HERSTELLER = { "Alle", "Fremd", "Muster" };

    private static IReadOnlyList<(int Id, string Text)> Filtern(string firma)
    {
        var liste = new List<(int, string)>();
        foreach (var z in KATALOG)
            if (firma.Length == 0 || firma == "Alle" || z.Firma == firma) liste.Add((z.Id, z.Text));
        return liste;
    }

    /// <summary>Die Liste der Hülle: ein ungeeignetes Gerät steht absichtlich NICHT vorn.</summary>
    private static readonly WechselrichterVorschlagZeile[] LISTE =
    {
        new(9, "Fremd 3000X", "Fremd", Wechselrichtereignung.Geeignet, "geeignet", "",
            "1,15", 1, "1 × (1 × 10)", "425 ≤ 600 V", "261…355 V in 80…500 V"),
        new(8, "Muster 5000TL-2M", "Muster", Wechselrichtereignung.Bedingt, "bedingt",
            "DC/AC 1,45 über 1,30", "1,45", 1, "1 × (2 × 5)", "213 ≤ 600 V", "130…178 V in 80…500 V"),
        new(7, "Muster 2500TL", "Muster", Wechselrichtereignung.Ungeeignet, "ungeeignet",
            "keine Reihenlänge passt ins Spannungsfenster", "", 0, "", "", "")
    };

    private static ErzeugerZeile Zeile(double? anzahl = 10, string modul = "Modul 275")
        => new ErzeugerZeile
        {
            Schluessel = 1, Bezeichner = modul, Neigung = 30, Azimut = 0,
            AnzahlModule = anzahl, MitWechselrichter = true
        };

    private IRenderedComponent<PvStraengeFelder> Aufbauen(
        ErzeugerZeile zeile,
        Func<ErzeugerZeile, string, IReadOnlyList<WechselrichterVorschlagZeile>>? wrVorschlagen,
        Func<ErzeugerZeile, int, StrangVorschlag>? auslegen = null,
        Action? geaendert = null)
        => Render<PvStraengeFelder>(p => p
            .Add(x => x.Zeile, zeile)
            .Add(x => x.NeigungAnlage, zeile.Neigung)
            .Add(x => x.AzimutAnlage, zeile.Azimut)
            .Add(x => x.Geraete, Filtern(""))
            .Add(x => x.GeraetUebernehmen, id => new GeraetWahl(1000 + id, "Gerät " + id))
            .Add(x => x.Hersteller, HERSTELLER)
            .Add(x => x.GeraeteFiltern, Filtern)
            .Add(x => x.AuslegungVorschlagen, auslegen)
            .Add(x => x.WechselrichterVorschlagen, wrVorschlagen)
            .Add(x => x.Module, Array.Empty<(int, string)>())
            .Add(x => x.Geaendert, () => geaendert?.Invoke()));

    private static StrangVorschlag Vorschlag(ErzeugerZeile z, int id)
        => new(true, new[] { new StrangVorgabe(1, 1, 10, 1) }, "Vorschlag.");

    [Fact]
    public void Kein_Delegat_ist_kein_Knopf()
    {
        var cut = Aufbauen(Zeile(), null);
        Assert.Empty(cut.FindAll(".epos-straenge-wrvorschlag"));
        Assert.DoesNotContain("Wechselrichter vorschlagen", cut.Markup, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null, "Modul 275", "Die Modulzahl der Anlage fehlt.")]
    [InlineData(10.0, "", "Zuerst das Modul der Anlage wählen.")]
    public void Die_weiche_Sperre_nennt_den_Grund(double? anzahl, string modul, string grund)
    {
        int gefragt = 0;
        var cut = Aufbauen(Zeile(anzahl, modul), (z, h) => { gefragt++; return LISTE; });

        var knopf = cut.Find(".epos-straenge-wrvorschlag");
        Assert.False(knopf.HasAttribute("disabled"));
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Equal(grund, knopf.GetAttribute("title"));

        knopf.Click();

        Assert.Equal(0, gefragt);
        Assert.False(cut.Instance.WechselrichterVorschlagOffen);
        Assert.Equal(grund, cut.Find(".epos-straenge-wrsperre").TextContent);
    }

    [Fact]
    public void Die_Ueberlagerung_zeigt_die_bewertete_Liste()
    {
        string? filter = null;
        var cut = Aufbauen(Zeile(), (z, h) => { filter = h; return LISTE; });

        var knopf = cut.Find(".epos-straenge-wrvorschlag");
        Assert.False(knopf.HasAttribute("aria-disabled"));
        knopf.Click();

        Assert.True(cut.Instance.WechselrichterVorschlagOffen);
        Assert.Equal("Alle", filter);
        Assert.Equal("Wechselrichter vorschlagen", cut.Find(".epos-ueberlagerung-titel").TextContent);

        var zeilen = cut.FindAll(".epos-wrvorschlag-tabelle tbody tr");
        Assert.Equal(3, zeilen.Count);
        Assert.Contains("epos-wrv-stufe--geeignet", zeilen[0].QuerySelector(".epos-wrv-stufe")!.ClassName);
        Assert.Contains("epos-wrv-stufe--bedingt", zeilen[1].QuerySelector(".epos-wrv-stufe")!.ClassName);
        Assert.Contains("epos-wrv-stufe--ungeeignet", zeilen[2].QuerySelector(".epos-wrv-stufe")!.ClassName);
        Assert.Contains("1 × (1 × 10)", zeilen[0].TextContent, StringComparison.Ordinal);

        // Vorgewählt ist die erste Zeile, die nicht „ungeeignet" ist.
        Assert.Equal(9, cut.Instance.WechselrichterVorschlagWahl);
        Assert.Equal("true", zeilen[0].GetAttribute("aria-selected"));

        Assert.StartsWith("3 Kandidaten geprüft, 1 geeignet (Herstellerfilter: Alle)",
                          cut.Find(".epos-wrvorschlag-hinweis").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Uebernehmen_setzt_Geraet_und_Filter_und_gibt_die_Auslegung_frei()
    {
        int gemeldet = 0;
        var zeile = Zeile();
        var cut = Aufbauen(zeile, (z, h) => LISTE, Vorschlag, () => gemeldet++);

        // Vorher: „Auslegung vorschlagen" weich gesperrt, der Grund nennt den neuen Knopf.
        var auslegung = cut.Find(".epos-straenge-vorschlag");
        Assert.Equal("true", auslegung.GetAttribute("aria-disabled"));
        Assert.Contains("Wechselrichter vorschlagen", auslegung.GetAttribute("title"), StringComparison.Ordinal);

        cut.Find(".epos-straenge-wrvorschlag").Click();
        // Die Zeile ist die Wahl: das bedingte Gerät eines anderen Herstellers.
        cut.FindAll(".epos-wrvorschlag-tabelle tbody tr")[1].Click();
        Assert.Equal(8, cut.Instance.WechselrichterVorschlagWahl);

        cut.Find(".epos-wrvorschlag .epos-knopf--primaer").Click();

        Assert.False(cut.Instance.WechselrichterVorschlagOffen);
        Assert.Equal(8, cut.Instance.Katalogwahl);
        Assert.Equal(2, cut.Instance.Herstellerfilter);          // „Muster"
        Assert.True(cut.Instance.VorschlagFrei);
        Assert.False(cut.Find(".epos-straenge-vorschlag").HasAttribute("aria-disabled"));
        Assert.Contains("„Muster 5000TL-2M“ ist gewählt", cut.Instance.Vorschlagsatz, StringComparison.Ordinal);

        // Nichts geschrieben, nichts gemeldet: der Wirt bleibt im Arbeitsstand.
        Assert.Empty(zeile.Straenge);
        Assert.Equal(0, gemeldet);

        // Danach füllt „Auslegung vorschlagen" die Tabelle.
        cut.Find(".epos-straenge-vorschlag").Click();
        Assert.Single(zeile.Straenge);
        Assert.Equal(1008, zeile.Straenge[0].WechselrichterId);
    }

    [Fact]
    public void Abbrechen_und_Esc_aendern_nichts()
    {
        var cut = Aufbauen(Zeile(), (z, h) => LISTE, Vorschlag);
        int? filterVorher = cut.Instance.Herstellerfilter;

        cut.Find(".epos-straenge-wrvorschlag").Click();
        cut.FindAll(".epos-wrvorschlag-tabelle tbody tr")[1].Click();
        var abbrechen = cut.FindAll(".epos-wrvorschlag .epos-leiste .epos-knopf")
                           .First(k => k.TextContent == "Abbrechen");
        abbrechen.Click();

        Assert.False(cut.Instance.WechselrichterVorschlagOffen);
        Assert.Equal(0, cut.Instance.Katalogwahl ?? 0);
        Assert.Equal(filterVorher, cut.Instance.Herstellerfilter);
        Assert.False(cut.Instance.VorschlagFrei);

        // Esc wirkt wie Abbrechen.
        cut.Find(".epos-straenge-wrvorschlag").Click();
        cut.Find(".epos-wrvorschlag").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(cut.Instance.WechselrichterVorschlagOffen);
        Assert.Equal(0, cut.Instance.Katalogwahl ?? 0);
    }

    [Fact]
    public void Der_Versuch_an_der_gesperrten_Auslegung_nennt_den_Grund()
    {
        int gefragt = 0;
        var zeile = Zeile();
        var cut = Aufbauen(zeile, (z, h) => LISTE, (z, id) => { gefragt++; return Vorschlag(z, id); });

        cut.Find(".epos-straenge-vorschlag").Click();

        Assert.Equal(0, gefragt);
        Assert.Empty(zeile.Straenge);
        var banner = cut.FindComponent<Warnbanner>();
        Assert.Equal(WarnStufe.Hinweis, banner.Instance.Stufe);
        Assert.StartsWith("Zuerst einen Wechselrichter aus dem Katalog wählen", banner.Instance.Text,
                          StringComparison.Ordinal);
    }

    [Fact]
    public void Ohne_Wahl_meldet_Uebernehmen_und_bleibt_offen()
    {
        var nurUngeeignet = new[] { LISTE[2] };
        var cut = Aufbauen(Zeile(), (z, h) => nurUngeeignet, Vorschlag);

        cut.Find(".epos-straenge-wrvorschlag").Click();
        Assert.Null(cut.Instance.WechselrichterVorschlagWahl);

        var ok = cut.Find(".epos-wrvorschlag .epos-knopf--primaer");
        Assert.Equal("true", ok.GetAttribute("aria-disabled"));
        ok.Click();

        Assert.True(cut.Instance.WechselrichterVorschlagOffen);
        Assert.Contains("Zuerst eine Zeile wählen.", cut.Find(".epos-wrvorschlag").TextContent, StringComparison.Ordinal);
        Assert.Equal(0, cut.Instance.Katalogwahl ?? 0);
    }
}
