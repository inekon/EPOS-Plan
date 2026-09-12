using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Zeilenwahl (iU9-W3.0) - der runde Wahlknopf einer Rasterzeile. Vorher stand
/// er wortgleich im Markup von BhkwWirtschaftlichkeitDialog und
/// KostenfaktorKatalogDialog.
/// </summary>
public class ZeilenwahlTests : BunitContext
{
    [Fact]
    public void Ungewaehlt_zeigt_der_Knopf_den_leeren_Kreis()
    {
        var cut = Render<Zeilenwahl>();

        Assert.Equal("○", cut.Find("button").TextContent.Trim());
        Assert.Equal("false", cut.Find("button").GetAttribute("aria-pressed"));
        Assert.Contains("epos-anlagenwahl", cut.Find("button").ClassName);
        Assert.DoesNotContain("epos-knopf--primaer", cut.Find("button").ClassName);
    }

    [Fact]
    public void Gewaehlt_zeigt_er_den_vollen_Kreis_und_meldet_es_der_Sprachausgabe()
    {
        var cut = Render<Zeilenwahl>(p => p.Add(x => x.Gewaehlt, true));

        Assert.Equal("●", cut.Find("button").TextContent.Trim());
        Assert.Equal("true", cut.Find("button").GetAttribute("aria-pressed"));
        Assert.Contains("epos-knopf--primaer", cut.Find("button").ClassName);
    }

    [Fact]
    public void Der_Klick_wird_gemeldet()
    {
        int mal = 0;
        var cut = Render<Zeilenwahl>(p => p.Add(x => x.Gewaehltwerden, () => mal++));

        cut.Find("button").Click();

        Assert.Equal(1, mal);
    }

    [Fact]
    public void Der_Kurztext_steht_am_Knopf()
    {
        var cut = Render<Zeilenwahl>(p => p.Add(x => x.Kurztext, "Emissionsart wählen"));

        Assert.Equal("Emissionsart wählen", cut.Find("button").GetAttribute("title"));
    }

    [Fact]
    public void Gesperrt_bleibt_der_Knopf_sichtbar_und_meldet_nicht()
    {
        int mal = 0;
        var cut = Render<Zeilenwahl>(p => p
            .Add(x => x.Aktiv, false)
            .Add(x => x.Gewaehltwerden, () => mal++));

        Assert.True(cut.Find("button").HasAttribute("disabled"));
        Assert.Equal(0, mal);
    }

    // =====================================================================
    // Mehrfachmodus (iU9-W13.0l)
    // =====================================================================

    [Fact]
    public void Im_Mehrfachmodus_ist_der_Knopf_ein_Kontrollkaestchen()
    {
        var cut = Render<Zeilenwahl>(p => p.Add(x => x.Mehrfach, true));

        Assert.Equal("\u2610", cut.Find("button").TextContent.Trim());
        Assert.Equal("checkbox", cut.Find("button").GetAttribute("role"));
        Assert.Equal("false", cut.Find("button").GetAttribute("aria-checked"));
        Assert.False(cut.Find("button").HasAttribute("aria-pressed"));
    }

    [Fact]
    public void Ein_gesetztes_Kaestchen_zeigt_den_Haken_und_meldet_es_der_Sprachausgabe()
    {
        var cut = Render<Zeilenwahl>(p => p
            .Add(x => x.Mehrfach, true)
            .Add(x => x.Gewaehlt, true));

        Assert.Equal("\u2611", cut.Find("button").TextContent.Trim());
        Assert.Equal("true", cut.Find("button").GetAttribute("aria-checked"));
        Assert.Contains("epos-knopf--primaer", cut.Find("button").ClassName);
    }

    /// <summary>
    /// Der Klick meldet die Zusatztasten mit - Strg und Umschalt sind der
    /// Unterschied zwischen "nur diese Zeile", "diese dazu" und "der Bereich
    /// bis hierher" (Vorbild SelectionMode.MultiExtended).
    /// </summary>
    [Fact]
    public void Der_Klick_meldet_Strg_und_Umschalt_mit()
    {
        Zeilenwahl.Zeilenklick letzter = default;
        var cut = Render<Zeilenwahl>(p => p
            .Add(x => x.Mehrfach, true)
            .Add(x => x.Tastenwahl, (Zeilenwahl.Zeilenklick k) => letzter = k));

        cut.Find("button").Click(new MouseEventArgs { CtrlKey = true });
        Assert.True(letzter.Strg);
        Assert.False(letzter.Umschalt);

        cut.Find("button").Click(new MouseEventArgs { ShiftKey = true });
        Assert.False(letzter.Strg);
        Assert.True(letzter.Umschalt);

        cut.Find("button").Click(new MouseEventArgs());
        Assert.False(letzter.Strg);
        Assert.False(letzter.Umschalt);
    }

    /// <summary>Beide Rueckrufe laufen, wenn beide belegt sind.</summary>
    [Fact]
    public void Tastenwahl_und_Gewaehltwerden_schliessen_einander_nicht_aus()
    {
        int einfach = 0;
        int mitTasten = 0;
        var cut = Render<Zeilenwahl>(p => p
            .Add(x => x.Mehrfach, true)
            .Add(x => x.Gewaehltwerden, () => einfach++)
            .Add(x => x.Tastenwahl, (Zeilenwahl.Zeilenklick _) => mitTasten++));

        cut.Find("button").Click();

        Assert.Equal(1, einfach);
        Assert.Equal(1, mitTasten);
    }
}
