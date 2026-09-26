using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Dialog „Luftaustausch zwischen Zonen" (Gebäudesimulation G6b, Welle W2): das Zeilenraster
/// Zone A | Zone B | V̇ mit dem Pflichtsatz, Anlegen und Entfernen im Arbeitsstand (die Vorlage bleibt
/// unberührt), die Prüfregeln genau einmal im Rückruf der Leiste — Zonen gewählt, V̇ eingegeben, dann die
/// Regel des Kerns über die Paare —, der Rückweg, der Dialog ohne Gaben und der Assistent.
///
/// <para>Die Kultur ist auf de-DE gepinnt: Die Erwartungswerte sind deutsche Beschriftungen und Zahlen.</para>
/// </summary>
public class LuftaustauschDialogTests : EposBunitContext
{
    public LuftaustauschDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Drei Zonen, eine davon vorläufig (negative Id).</summary>
    private static readonly IReadOnlyList<NachbarzoneWahl> ZONEN = new[]
    {
        new NachbarzoneWahl(1, "Erdgeschoss"),
        new NachbarzoneWahl(2, "Obergeschoss"),
        new NachbarzoneWahl(-3, "Treppenhaus")
    };

    private static List<ZonenluftstromDaten> Stroeme() => new()
    {
        new ZonenluftstromDaten { Id = 11, IdZoneA = 1, IdZoneB = 2, Volumenstrom = 80 }
    };

    private IRenderedComponent<LuftaustauschDialog> Aufbauen(IReadOnlyList<ZonenluftstromDaten>? stroeme = null,
                                                             Action<IReadOnlyList<ZonenluftstromDaten>?>? geschlossen = null)
        => Render<LuftaustauschDialog>(p => p
            .Add(x => x.Zonen, ZONEN)
            .Add(x => x.Luftstroeme, stroeme ?? Stroeme())
            .Add(x => x.Geschlossen, l => geschlossen?.Invoke(l)));

    private static IReadOnlyList<IElement> Zeilen(IRenderedComponent<LuftaustauschDialog> cut)
        => cut.FindAll(".epos-luftstrom").ToList();

    private static void Ok(IRenderedComponent<LuftaustauschDialog> cut)
        => cut.Find(".epos-luftaustauschdialog > .epos-leiste button.epos-knopf--primaer").Click();

    private static IElement Knopf(IRenderedComponent<LuftaustauschDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    [Fact]
    public void Das_Raster_traegt_Zone_A_Zone_B_und_den_Volumenstrom_samt_Pflichtsatz()
    {
        var cut = Aufbauen();

        Assert.Contains("Gerechnet wird nur der eingegebene Strom.", cut.Markup);
        Assert.Contains("Zone A", cut.Markup);
        Assert.Contains("Zone B", cut.Markup);
        Assert.Contains("V̇ [m³/h]", cut.Markup);

        IElement zeile = Assert.Single(Zeilen(cut));
        IReadOnlyList<IElement> wahl = zeile.QuerySelectorAll("select").ToList();
        Assert.Equal(2, wahl.Count);
        Assert.Equal("1", wahl[0].QuerySelector("option[selected]")?.GetAttribute("value"));
        Assert.Equal("2", wahl[1].QuerySelector("option[selected]")?.GetAttribute("value"));
        Assert.Contains("Treppenhaus", wahl[0].TextContent);
        Assert.Equal("80", zeile.QuerySelector("input")!.GetAttribute("value"));
        Assert.Single(zeile.QuerySelectorAll("button"));
    }

    [Fact]
    public void OK_gibt_den_Arbeitsstand_zurueck_und_laesst_die_Vorlage_unberuehrt()
    {
        List<ZonenluftstromDaten> vorlage = Stroeme();
        IReadOnlyList<ZonenluftstromDaten>? zurueck = null;
        var cut = Aufbauen(vorlage, l => zurueck = l);

        Knopf(cut, "+ Neuer Luftstrom").Click();
        IElement neu = Zeilen(cut)[1];
        neu.QuerySelectorAll("select")[0].Change("-3");
        neu = Zeilen(cut)[1];
        neu.QuerySelectorAll("select")[1].Change("2");
        Zeilen(cut)[1].QuerySelector("input")!.Input("45,5");
        Zeilen(cut)[0].QuerySelector("input")!.Input("100");
        Ok(cut);

        Assert.NotNull(zurueck);
        Assert.Equal(2, zurueck!.Count);
        Assert.Equal(100.0, zurueck[0].Volumenstrom);
        Assert.True(zurueck[1].Id < 0);
        Assert.Equal(-3, zurueck[1].IdZoneA);
        Assert.Equal(2, zurueck[1].IdZoneB);
        Assert.Equal(45.5, zurueck[1].Volumenstrom);
        Assert.Equal(80.0, vorlage[0].Volumenstrom);
        Assert.Single(vorlage);
    }

    [Fact]
    public void Entfernen_nimmt_die_Zeile_nur_aus_dem_Arbeitsstand()
    {
        List<ZonenluftstromDaten> vorlage = Stroeme();
        IReadOnlyList<ZonenluftstromDaten>? zurueck = null;
        var cut = Aufbauen(vorlage, l => zurueck = l);

        Zeilen(cut)[0].QuerySelector("button")!.Click();
        Assert.Empty(Zeilen(cut));
        Assert.Contains("Kein Luftaustausch zwischen den Zonen.", cut.Markup);
        Ok(cut);

        Assert.NotNull(zurueck);
        Assert.Empty(zurueck!);
        Assert.Single(vorlage);
    }

    [Theory]
    [InlineData(null, "2", "50", "Luftstrom 2: Bitte beide Zonen wählen.")]
    [InlineData("1", "2", null, "Luftstrom 2: Bitte den Volumenstrom eingeben.")]
    [InlineData("2", "1", "50", "steht zweimal")]
    [InlineData("-3", "-3", "50", "Treppenhaus")]
    [InlineData("-3", "1", "0", "Treppenhaus")]
    public void Die_Pruefregeln_halten_den_Dialog_offen(string? a, string? b, string? v, string teil)
    {
        bool gerufen = false;
        var cut = Aufbauen(geschlossen: _ => gerufen = true);

        Knopf(cut, "+ Neuer Luftstrom").Click();
        if (a is not null) Zeilen(cut)[1].QuerySelectorAll("select")[0].Change(a);
        if (b is not null) Zeilen(cut)[1].QuerySelectorAll("select")[1].Change(b);
        if (v is not null) Zeilen(cut)[1].QuerySelector("input")!.Input(v);
        Ok(cut);

        Assert.False(gerufen);
        Assert.Contains(teil, cut.Instance.Meldung);
        Assert.Contains(teil, cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Abbrechen_und_Esc_geben_null()
    {
        var antworten = new List<IReadOnlyList<ZonenluftstromDaten>?>();
        var cut = Aufbauen(geschlossen: l => antworten.Add(l));

        Knopf(cut, "Abbrechen").Click();
        cut.Find(".epos-luftaustauschdialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal(2, antworten.Count);
        Assert.All(antworten, Assert.Null);
    }

    [Fact]
    public void Ohne_Gaben_steht_ein_leeres_Raster_und_OK_gibt_eine_leere_Liste()
    {
        var cut = Render<LuftaustauschDialog>();

        Assert.Empty(Zeilen(cut));
        Assert.Contains("Luftaustausch zwischen Zonen", cut.Find(".epos-dialog-titel").TextContent);
        Ok(cut);
        Assert.Equal("", cut.Instance.Meldung);
    }

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Sie bindet über die Sichtklasse
    /// <c>LuftaustauschKiSicht</c>: die Luftströme als Raster, die Zonen zum Lesen, der Volumenstrom setzbar.
    /// </summary>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an()
    {
        var cut = Aufbauen();

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.LUFTAUSTAUSCH));
        KiFeldzugang zoneA = KiMaskenbruecke.Feldzugang(KiMaskennamen.LUFTAUSTAUSCH, "zone_a_1");
        Assert.NotNull(zoneA);
        Assert.Equal("Erdgeschoss", zoneA.Lesen());
        Assert.False(zoneA.Setzbar);
        Assert.Equal("Obergeschoss", KiMaskenbruecke.Feldzugang(KiMaskennamen.LUFTAUSTAUSCH, "zone_b_1")!.Lesen());

        KiFeldzugang strom = KiMaskenbruecke.Feldzugang(KiMaskennamen.LUFTAUSTAUSCH, "volumenstrom_1");
        Assert.NotNull(strom);
        Assert.True(strom.Setzbar);
        strom.Setzen(95.0);
        cut.Render();
        Assert.Equal(95.0, cut.Instance.Arbeitsstand[0].Volumenstrom);
    }
}
