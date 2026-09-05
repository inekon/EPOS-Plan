using System.Globalization;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Der Baustein <see cref="KiKnopf"/> — der Einstieg in den Assistenten aus einer
/// Maske heraus (iU9-W15b.5).
///
/// <para>Er löst zwei protokollierte Abweichungen ein: Die Wellen 6 und 7 haben
/// den fehlenden KI-Einstieg je als bewusste Abweichung vermerkt, beide mit
/// derselben Begründung — „Der KI-Einstieg hat in EPOS.UI noch keinen
/// Baustein".</para>
///
/// <para>Die Klasse pinnt die Sprache selbst (Regel seit W8).</para>
/// </summary>
public class KiKnopfTests : BunitContext
{
    public KiKnopfTests()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
    }

    [Fact]
    public void Er_traegt_Beschriftung_und_Kurztext()
    {
        var cut = Render<KiKnopf>(p => p
            .Add(x => x.Beschriftung, "Hilfe")
            .Add(x => x.Kurztext, "KI-Assistent"));

        var knopf = cut.Find("button.epos-kiknopf");

        Assert.Equal("Hilfe", knopf.TextContent.Trim());
        Assert.Equal("KI-Assistent", knopf.GetAttribute("title"));
        Assert.Equal("KI-Assistent", knopf.GetAttribute("aria-label"));
    }

    /// <summary>
    /// <b>Der Abschalter blendet ihn aus, er sperrt ihn nicht.</b> Ein grauer Knopf
    /// wäre ein Versprechen, das die Installation nicht einlöst.
    /// </summary>
    [Fact]
    public void Ohne_Sichtbar_steht_er_gar_nicht_da()
    {
        var cut = Render<KiKnopf>(p => p
            .Add(x => x.Beschriftung, "Hilfe")
            .Add(x => x.Sichtbar, false));

        Assert.Empty(cut.FindAll("button.epos-kiknopf"));
    }

    /// <summary>
    /// <b>Er zieht den Fokus nicht an sich.</b> Der Vorläufer war ein Knopf mit
    /// <c>TabStop = false</c> und einem überschriebenen <c>OnMouseDown</c>, damit ein
    /// Klick den Fokus nicht aus dem gerade bearbeiteten Feld holt.
    /// </summary>
    [Fact]
    public void Er_ist_nicht_tabulierbar()
    {
        var cut = Render<KiKnopf>(p => p.Add(x => x.Beschriftung, "Hilfe"));

        Assert.Equal("-1", cut.Find("button.epos-kiknopf").GetAttribute("tabindex"));
    }

    /// <summary>
    /// <b>Er öffnet nichts.</b> Er meldet „der Anwender möchte den Assistenten"; das
    /// Öffnen ist Sache der Hülle (Windows <c>KiChatHuelle</c>, iOS
    /// <c>Seitenschluessel.KiAssistent</c>).
    /// </summary>
    [Fact]
    public void Ein_Klick_meldet_sich_beim_Wirt()
    {
        int gemeldet = 0;

        var cut = Render<KiKnopf>(p => p
            .Add(x => x.Beschriftung, "Hilfe")
            .Add(x => x.Gewaehlt, EventCallback.Factory.Create(this, () => gemeldet++)));

        cut.Find("button.epos-kiknopf").Click();

        Assert.Equal(1, gemeldet);
    }
}
