using Bunit;
using EPOS.UI.Dialoge.Import;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// MA-6: die Vorschau des Wiederimports — Zahlen je Gruppe, Stand und Felder je Zeile, die Wahl „entfallende entfernen“ nur,
/// wenn etwas entfällt. Probewerte ohne Datenbank.
/// </summary>
public sealed class WiederimportVorschauTests : EposBunitContext
{
    public WiederimportVorschauTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private static GebaeudeWiederimportDaten Daten(bool mitEntfallend, Action<bool>? setzen = null) => new()
    {
        Zeilen = new List<WiederimportZeile>
        {
            new(true, "Zone 1", "Zone 1", Wiederimportstand.Geaendert, new[] { "Fläche" }, new[] { "Nutzung und Kalender" }),
            new(true, "Zone 2", "Zone 2", Wiederimportstand.Neu, Array.Empty<string>(), Array.Empty<string>()),
            new(false, "Zone 1", "Wand 1", Wiederimportstand.Unveraendert, Array.Empty<string>(), new[] { "Aufbau und U-Wert" }),
            new(true, "Handzone", "Handzone", Wiederimportstand.Bleibt, Array.Empty<string>(), Array.Empty<string>()),
        }.Concat(mitEntfallend
            ? new[] { new WiederimportZeile(false, "Zone 1", "Dach 1", Wiederimportstand.Entfaellt, Array.Empty<string>(), Array.Empty<string>()) }
            : Array.Empty<WiederimportZeile>()).ToList(),
        EntfernenSetzen = setzen
    };

    [Fact]
    public void Zahlen_je_Gruppe_und_Zeilen_mit_Stand_Feldern_und_Hand()
    {
        using var kultur = new Kulturvorrichtung("de-DE");
        var cut = Render<WiederimportVorschau>(c => c.Add(x => x.Daten, Daten(true)));
        var zahlen = cut.FindAll(".epos-wiederimport-zahlen").Select(e => e.TextContent).ToList();
        Assert.Equal("Zonen: 1 neu · 1 geändert · 0 unverändert · 0 entfällt · 1 bleibt (Hand)", zahlen[0]);
        Assert.Equal("Bauteile: 0 neu · 0 geändert · 1 unverändert · 1 entfällt · 0 bleibt (Hand)", zahlen[1]);
        var zeilen = cut.FindAll(".epos-wiederimport-zeile");
        Assert.Equal(5, zeilen.Count);
        Assert.Contains("Fläche", zeilen[0].TextContent);
        Assert.Contains("Nutzung und Kalender", zeilen[0].TextContent);
        Assert.Equal(R.GEBWI_STAND_ENTFAELLT, zeilen[4].QuerySelector(".epos-wiederimport-stand")!.TextContent);
        Assert.Empty(cut.FindAll(".epos-wiederimport-fassung"));
    }

    [Fact]
    public void Die_Wahl_entfernen_steht_nur_mit_Entfallendem_und_meldet_sich()
    {
        bool? wahl = null;
        GebaeudeWiederimportDaten d = Daten(true, w => wahl = w);
        var cut = Render<WiederimportVorschau>(c => c.Add(x => x.Daten, d));
        var haken = cut.Find(".epos-wiederimport-entfernen input");
        Assert.False(haken.HasAttribute("checked"));
        haken.Change(true);
        Assert.True(wahl);
        Assert.True(d.Entfernen);

        var ohne = Render<WiederimportVorschau>(c => c.Add(x => x.Daten, Daten(false)));
        Assert.Empty(ohne.FindAll(".epos-wiederimport-entfernen"));
        Assert.Single(ohne.FindAll(".epos-wiederimport-ohne"));
    }
}
