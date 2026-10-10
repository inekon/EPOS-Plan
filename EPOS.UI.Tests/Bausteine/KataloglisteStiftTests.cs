using System.Collections.Generic;
using System.Linq;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <b>Der Stift je Zeile der Katalogliste</b> (UeS2, Anwenderentscheid 10.10.2026: „Füge
/// jeweils in die Auswahl (DB und Projekt) ein Bearbeiten Symbol ein."): eine schmale Spalte am
/// Ende, nur mit Rückruf; ihr Klick wählt die Zeile und ruft den Rückruf, ohne Kästchen oder
/// Markierung; die Spaltenwahl kennt sie nicht.
/// </summary>
public class KataloglisteStiftTests : EposBunitContext
{
    public KataloglisteStiftTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static Katalogfilterprofil Profil() =>
        Katalogfilterprofil.Finde(Anlagenart.Heizkessel, s => s);

    private static List<Katalogfilterzeile> Zeilen() => new()
    {
        new Katalogfilterzeile(1, "Alpha").MitText(Katalogfilterprofil.SpBezeichner, "Alpha")
            .MitText(Katalogfilterprofil.SpHersteller, "Werk 1").MitZahl(Katalogfilterprofil.SpPtherm, 15.0),
        new Katalogfilterzeile(2, "Beta").MitText(Katalogfilterprofil.SpBezeichner, "Beta")
            .MitText(Katalogfilterprofil.SpHersteller, "Werk 2").MitZahl(Katalogfilterprofil.SpPtherm, 80.0)
    };

    private IRenderedComponent<Katalogliste> Aufbauen(List<Katalogfilterzeile>? bearbeitet,
                                                      List<string>? gewaehlt = null,
                                                      bool kaestchen = false,
                                                      List<IReadOnlyList<string>>? kaestchenGemeldet = null,
                                                      string? spaltenwahlname = null)
    {
        return Render<Katalogliste>(p =>
        {
            p.Add(x => x.Profil, Profil())
             .Add(x => x.Zeilen, Zeilen())
             .Add(x => x.Filterstand, new Katalogfilterstand())
             .Add(x => x.GewaehltChanged, EventCallback.Factory.Create<string>(this, w => gewaehlt?.Add(w)));
            if (kaestchen)
                p.Add(x => x.ZeileIstWahl, true).Add(x => x.Kaestchen, true)
                 .Add(x => x.GewaehlteChanged, EventCallback.Factory.Create<IReadOnlyList<string>>(this, l => kaestchenGemeldet?.Add(l)));
            if (spaltenwahlname is not null) p.Add(x => x.Spaltenwahlname, spaltenwahlname);
            if (bearbeitet is not null)
                p.Add(x => x.Bearbeiten, EventCallback.Factory.Create<Katalogfilterzeile>(this, z => bearbeitet.Add(z)));
        });
    }

    [Fact]
    public void Ohne_Rueckruf_gibt_es_keine_Stiftspalte()
    {
        var cut = Aufbauen(bearbeitet: null);

        Assert.Empty(cut.FindAll(".epos-zeilenstift"));
        Assert.Empty(cut.FindAll("th.epos-spalte-stift"));
    }

    [Fact]
    public void Mit_Rueckruf_traegt_jede_Zeile_am_Ende_einen_Stift_mit_Beschriftung()
    {
        var cut = Aufbauen(bearbeitet: new());

        var stifte = cut.FindAll(".epos-zeilenstift");
        Assert.Equal(2, stifte.Count);
        Assert.All(stifte, s =>
        {
            Assert.Equal("Bearbeiten", s.GetAttribute("aria-label"));
            Assert.Equal("button", s.GetAttribute("type"));
            Assert.NotNull(s.QuerySelector("svg[aria-hidden=true]"));
        });
        // Die Spalte steht am Ende jeder Zeile, ihr Kopf traegt die Beschriftung fuer die Sprachausgabe.
        Assert.All(cut.FindAll("tbody tr"), tr => Assert.Contains("epos-spalte-stift", tr.Children.Last().ClassName));
        Assert.Equal("Bearbeiten", cut.Find("thead th.epos-spalte-stift .epos-zeilenstift-kopf").TextContent);
    }

    [Fact]
    public void Der_Stift_waehlt_die_Zeile_und_ruft_den_Rueckruf_mit_ihr()
    {
        var bearbeitet = new List<Katalogfilterzeile>();
        var gewaehlt = new List<string>();
        var cut = Aufbauen(bearbeitet, gewaehlt);

        cut.FindAll(".epos-zeilenstift")[1].Click();

        Assert.Equal(new[] { "Beta" }, gewaehlt);
        var zeile = Assert.Single(bearbeitet);
        Assert.Equal(2, zeile.Id);
    }

    [Fact]
    public void Der_Stift_setzt_kein_Kaestchen_der_Mehrfachwahl()
    {
        var bearbeitet = new List<Katalogfilterzeile>();
        var gewaehlt = new List<string>();
        var kaestchen = new List<IReadOnlyList<string>>();
        var cut = Aufbauen(bearbeitet, gewaehlt, kaestchen: true, kaestchenGemeldet: kaestchen);

        cut.FindAll(".epos-zeilenstift")[0].Click();

        Assert.Equal(new[] { "Alpha" }, gewaehlt);
        Assert.Single(bearbeitet);
        Assert.Empty(kaestchen);
        Assert.All(cut.FindAll("td.epos-spalte-kaestchen input[type=checkbox]"), e => Assert.False(e.HasAttribute("checked")));
    }

    [Fact]
    public void Die_Stiftspalte_zaehlt_nicht_zur_Spaltenwahl()
    {
        var ohne = Aufbauen(bearbeitet: null, spaltenwahlname: "Probe.Stift");
        ohne.Find(".epos-katalog-spaltenknopf").Click();
        int erwartet = ohne.Find(".epos-spaltenwahl-auswahl").QuerySelectorAll("input").Length;

        var mit = Aufbauen(bearbeitet: new(), spaltenwahlname: "Probe.Stift");
        mit.Find(".epos-katalog-spaltenknopf").Click();
        var auswahl = mit.Find(".epos-spaltenwahl-auswahl");

        Assert.True(erwartet > 0);
        Assert.Equal(erwartet, auswahl.QuerySelectorAll("input").Length);
        Assert.DoesNotContain("Bearbeiten", auswahl.TextContent);
    }

    [Fact]
    public void Die_Stiftregel_haelt_das_Beruehrungsziel_und_die_Zeilenhoehe()
    {
        string css = System.IO.File.ReadAllText(System.IO.Path.Combine(Wurzel(), "EPOS.UI", "wwwroot", "epos-ui.css"));
        int i = css.IndexOf(".epos-zeilenstift {", System.StringComparison.Ordinal);
        Assert.True(i > 0);
        string regel = css.Substring(i, css.IndexOf('}', i) - i);
        Assert.Contains("width: var(--epos-touchziel);", regel);
        Assert.Contains("height: var(--epos-touchziel);", regel);
        Assert.Contains("td.epos-spalte-stift", css);
        Assert.Contains("@media (forced-colors: active) {\n    .epos-zeilenstift", css);
    }

    private static string Wurzel()
    {
        var d = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
        while (d is not null && !System.IO.File.Exists(System.IO.Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css"))) d = d.Parent;
        return d!.FullName;
    }
}
