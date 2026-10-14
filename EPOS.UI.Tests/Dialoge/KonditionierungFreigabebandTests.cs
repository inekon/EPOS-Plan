using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Das Jahresband der Freigabe im Reiter Konditionierung</b> (Entwurf AK3-K 3.3; Welle KZ): das Band je Ort über die
/// Hülle und den Kern (<c>Konditionierungsfreigabe</c>, dieselbe Regel wie der Lauf), die Zählzeile, die Legende und der
/// Hinweis; ohne Delegat steht nichts da.
/// </summary>
public class KonditionierungFreigabebandTests : EposBunitContext
{
    public KonditionierungFreigabebandTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<KonditionierungReiter> Reiter(KonditionierungWeg? weg, bool kuehlung)
    {
        GebaeudeKatalogDaten satz = KalenderkarteTests.Satz();
        satz.KuehlungAktiv = kuehlung;
        var arbeit = new GebaeudeArbeitsstand();
        arbeit.Laden(satz, neu: false);
        var bearbeitung = new KonditionierungBearbeitung(arbeit, () => weg);
        return Render<KonditionierungReiter>(p => p.Add(x => x.Bearbeitung, bearbeitung));
    }

    [Fact]
    public void Ohne_Kalender_gibt_der_Bestand_mit_Kuehlung_beide_Seiten_an_jedem_Tag_frei()
    {
        var cut = Reiter(KalenderkarteTests.Weg(null), kuehlung: true);
        var band = cut.Find("section.epos-kond-freigabe");
        var ort = Assert.Single(band.QuerySelectorAll("div.epos-kond-freigabe-ort"));
        Assert.Equal("Gebäude", ort.GetAttribute("data-ort"));
        Assert.Equal("365", ort.GetAttribute("data-beides"));
        var rect = Assert.Single(ort.QuerySelectorAll("rect"));
        Assert.Equal("epos-freigabe-3", rect.GetAttribute("class"));
        Assert.Equal("365", rect.GetAttribute("width"));
        Assert.Equal("Gebäude: Heizen frei 0 Tage, Kühlen frei 0 Tage, beides 365 Tage, keines 0 Tage",
                     ort.QuerySelector("p.epos-kond-freigabe-zeile")!.TextContent.Trim());
        Assert.Equal("Freigabe Heizen und Kühlen", band.QuerySelector("p.epos-kond-freigabe-titel")!.TextContent.Trim());
        Assert.Contains("nie beides am selben Tag", band.QuerySelector("p.epos-kond-freigabe-hinweis")!.TextContent);
        Assert.Equal(4, band.QuerySelectorAll("span.epos-kond-freigabe-marke").Length);
    }

    [Fact]
    public void Ohne_Kuehlung_ist_nur_Heizen_frei()
    {
        var cut = Reiter(KalenderkarteTests.Weg(null), kuehlung: false);
        var ort = Assert.Single(cut.FindAll("div.epos-kond-freigabe-ort"));
        Assert.Equal("0", ort.GetAttribute("data-beides"));
        Assert.Equal("epos-freigabe-1", Assert.Single(ort.QuerySelectorAll("rect")).GetAttribute("class"));
    }

    [Fact]
    public void Ohne_Delegat_steht_kein_Band()
    {
        var cut = Reiter(new KonditionierungWeg(), kuehlung: true);
        Assert.Empty(cut.FindAll("section.epos-kond-freigabe"));
    }

    [Fact]
    public void Das_Band_fasst_gleiche_Tage_zu_Abschnitten_und_zaehlt_je_Art()
    {
        var tage = new int[365];
        for (int d = 0; d < 365; d++) tage[d] = d < 100 ? 1 : d < 120 ? 3 : d < 270 ? 2 : d < 280 ? 0 : 1;
        var b = new KonditionierungFreigabeband("Zone A", tage);
        Assert.Equal(185, b.TageHeizen);
        Assert.Equal(150, b.TageKuehlen);
        Assert.Equal(20, b.TageBeides);
        Assert.Equal(10, b.TageKeine);

        var cut = Render<Freigabeband>(p => p.Add(x => x.Baender, new[] { b }).Add(x => x.Texte, new KonditionierungTexte()));
        var rects = cut.FindAll("rect");
        Assert.Equal(new[] { "epos-freigabe-1", "epos-freigabe-3", "epos-freigabe-2", "epos-freigabe-0", "epos-freigabe-1" },
                     rects.Select(r => r.GetAttribute("class")));
        Assert.Equal(new[] { "0", "100", "120", "270", "280" }, rects.Select(r => r.GetAttribute("x")));
        Assert.Equal("Zone A: Heizen frei 185 Tage, Kühlen frei 150 Tage, beides 20 Tage, keines 10 Tage",
                     cut.Find("p.epos-kond-freigabe-zeile").TextContent.Trim());
        Assert.Empty(Render<Freigabeband>(p => p.Add(x => x.Baender, null)).FindAll("section"));
    }
}
