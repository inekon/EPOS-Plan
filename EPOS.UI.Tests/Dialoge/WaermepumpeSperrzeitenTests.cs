using Bunit;
using EPOS.UI.Dialoge.Waermepumpe;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Gruppe „Sperrzeiten“ im Wärmepumpen-Dialog</b> (Welle V14): Liste, Vorlagen,
/// Hinzufügen/Entfernen, Wochentage; ohne Liste der Hülle bleibt das Altfenster stehen. Dazu der
/// Text der KI-Sicht.
/// </summary>
public class WaermepumpeSperrzeitenTests : EposBunitContext
{
    public WaermepumpeSperrzeitenTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static WaermepumpeAnlageDaten Daten(List<SperrfensterZeile>? fenster) => new()
    {
        Bezeichner = "WP Alpha",
        SperrzeitVon = 14,
        SperrzeitBis = 17,
        Abschaltpunkt = -5,
        CarrierId = 60,
        Sperrfenster = fenster
    };

    private IRenderedComponent<WaermepumpeKonfiguration> Aufbauen(WaermepumpeAnlageDaten d, Action? geaendert = null)
        => Render<WaermepumpeKonfiguration>(p => p
            .Add(x => x.Daten, d)
            .Add(x => x.Traegerkatalog, Array.Empty<EPOS.UI.Bausteine.EnergietraegerWahl.Eintrag>())
            .Add(x => x.Aktiv, true)
            .Add(x => x.Geaendert, () => geaendert?.Invoke()));

    [Fact]
    public void Die_Liste_zeigt_je_Fenster_eine_Zeile_und_das_Altfenster_steht_nicht_doppelt()
    {
        var d = Daten(new() { new() { VonH = 14, DauerH = 3, HeizstabGesperrt = false } });
        var c = Aufbauen(d);
        Assert.Single(c.FindAll(".epos-sperrfenster-zeile"));
        // Mit Liste gibt es den alten Sperrzeit-Schalter nicht mehr.
        Assert.DoesNotContain(c.FindAll("label"), l => l.TextContent.Contains(new WaermepumpeKonfigurationTexte().LabelSperrzeitSchalter));
    }

    [Fact]
    public void Ohne_Liste_bleibt_das_Altfenster()
    {
        var c = Aufbauen(Daten(null));
        Assert.Empty(c.FindAll(".epos-sperrfenster-zeile"));
        Assert.Empty(c.FindAll("[data-vorlage]"));
    }

    [Fact]
    public void Vorlagen_Hinzufuegen_und_Entfernen()
    {
        int gemeldet = 0;
        var d = Daten(new());
        var c = Aufbauen(d, () => gemeldet++);
        c.Find("[data-vorlage=ZWEI_MAL_ZWEI]").Click();
        Assert.Equal(new double?[] { 11, 17 }, d.Sperrfenster!.Select(z => z.VonH));
        Assert.All(d.Sperrfenster!, z => Assert.True(z.HeizstabGesperrt && z.Wochentage == SperrfensterZeile.ALLE_TAGE));

        c.Find("[data-vorlage=DREI_MAL_ZWEI]").Click();
        Assert.Equal(3, d.Sperrfenster!.Count);
        c.Find("[data-aktion=sperrfenster-hinzu]").Click();
        Assert.Equal(4, d.Sperrfenster!.Count);
        Assert.Equal(4, c.FindAll(".epos-sperrfenster-zeile").Count);
        c.FindAll("[data-aktion=sperrfenster-entfernen]")[0].Click();
        Assert.Equal(new double?[] { 11, 17, 12 }, d.Sperrfenster!.Select(z => z.VonH));
        c.Find("[data-vorlage=KEINE]").Click();
        Assert.Empty(d.Sperrfenster!);
        Assert.True(gemeldet >= 5);
    }

    [Fact]
    public void Ein_Wochentag_laesst_sich_abwaehlen_aber_nicht_der_letzte()
    {
        var z = new SperrfensterZeile { VonH = 11, DauerH = 2, Wochentage = 1 | 2 };
        var c = Aufbauen(Daten(new() { z }));
        // Die Schalter der Zeile: sieben Wochentage, dann der Heizstab.
        var schalter = c.Find(".epos-sperrfenster-zeile").QuerySelectorAll("input[type=checkbox]");
        Assert.Equal(8, schalter.Length);
        schalter[1].Change(false);
        Assert.Equal(1, z.Wochentage);
        c.Find(".epos-sperrfenster-zeile").QuerySelectorAll("input[type=checkbox]")[0].Change(false);
        Assert.Equal(1, z.Wochentage);
        c.Find(".epos-sperrfenster-zeile").QuerySelectorAll("input[type=checkbox]")[6].Change(true);
        Assert.Equal(1 | 64, z.Wochentage);
    }

    [Theory]
    [InlineData("11-13; 17-19", 2)]
    [InlineData("22-2", 1)]
    [InlineData("", 0)]
    public void Die_KI_Sicht_liest_und_schreibt_den_Text(string text, int anzahl)
    {
        List<SperrfensterZeile>? l = WaermepumpeAnlageKiSicht.SperrfensterLesen(text);
        Assert.NotNull(l);
        Assert.Equal(anzahl, l!.Count);
        Assert.Equal(text, WaermepumpeAnlageKiSicht.SperrfensterText(l));
        Assert.Null(WaermepumpeAnlageKiSicht.SperrfensterLesen("11-xx"));
        Assert.Null(WaermepumpeAnlageKiSicht.SperrfensterLesen("25-2"));
    }
}
