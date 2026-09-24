using Bunit;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// ETAPPE E9b (Konzept § 2.11.5 „Pflege"; Entscheid E9b‑Q1, Lesart a) — der
/// <b>±-Knopf eines Szenariopaars</b>. Derselbe Baustein steht an den Trägerpreisen der
/// Trägerkarte, an den Einspeisevergütungen (Parameterdialog, BHKW-Wirtschaftlichkeit)
/// und an DV-Entgelt und PPA-Preis des PV-Vergütungsdialogs.
///
/// <para>Geprüft wird, was der Knopf SAGT (Zeichen, Gegenstand, Kennzeichen, Warnzeichen,
/// Kurztext, aria-label), wann er gesperrt ist, dass er meldet — und dass es ohne
/// Delegat keinen Knopf gibt.</para>
/// </summary>
public class SzenarioKnopfTests : EposBunitContext
{
    private int _angefordert;

    private IRenderedComponent<SzenarioKnopf> Zeige(bool gepflegt = false, bool warnung = false,
                                                    bool aktiv = true, string kurztext = "",
                                                    bool mitDelegat = true)
        => Render<SzenarioKnopf>(p =>
        {
            p.Add(x => x.Text, "Arbeitspreis");
            p.Add(x => x.Gepflegt, gepflegt);
            p.Add(x => x.Warnung, warnung);
            p.Add(x => x.Aktiv, aktiv);
            p.Add(x => x.Kurztext, kurztext);
            if (mitDelegat) p.Add(x => x.Angefordert, () => _angefordert++);
        });

    [Fact]
    public void Ohne_Pflege_zeigt_der_Knopf_Zeichen_und_Gegenstand_ohne_Kennzeichen()
    {
        var cut = Zeige(kurztext: "Szenariowerte Best/Worst pflegen");
        var knopf = cut.Find("button");

        Assert.Equal("epos-knopf epos-szenarioknopf", knopf.ClassName);
        Assert.Equal("±", cut.Find(".epos-szenarioknopf-zeichen").TextContent);
        Assert.Equal("Arbeitspreis", cut.Find(".epos-szenarioknopf-text").TextContent);
        Assert.Empty(cut.FindAll(".epos-szenarioknopf-kennzeichen"));
        Assert.Empty(cut.FindAll(".epos-szenarioknopf-warnung"));
        Assert.Equal("± Arbeitspreis", knopf.GetAttribute("aria-label"));
        Assert.Equal("Szenariowerte Best/Worst pflegen", knopf.GetAttribute("title"));
        Assert.False(knopf.HasAttribute("disabled"));
    }

    [Fact]
    public void Ein_gepflegtes_Paar_traegt_das_Kennzeichen_und_sagt_es_der_Sprachausgabe()
    {
        var cut = Zeige(gepflegt: true);
        var knopf = cut.Find("button");

        Assert.Contains("epos-szenarioknopf--gepflegt", knopf.ClassName);
        Assert.Equal("●", cut.Find(".epos-szenarioknopf-kennzeichen").TextContent);
        Assert.Equal("true", cut.Find(".epos-szenarioknopf-kennzeichen").GetAttribute("aria-hidden"));
        Assert.Equal("± Arbeitspreis (gepflegt)", knopf.GetAttribute("aria-label"));
        Assert.Equal("± Arbeitspreis (gepflegt)", cut.Instance.AriaText);
    }

    /// <summary>E9b‑Q4 (warnen, nicht verweigern): ein gepflegtes Paar ohne Erwartet-Wert.</summary>
    [Fact]
    public void Die_Warnung_zeigt_das_Warnzeichen_und_nennt_sich_im_aria_label()
    {
        var cut = Zeige(gepflegt: true, warnung: true);

        Assert.Equal("⚠", cut.Find(".epos-szenarioknopf-warnung").TextContent);
        Assert.Equal("± Arbeitspreis (gepflegt) (ohne Erwartet-Wert)",
                     cut.Find("button").GetAttribute("aria-label"));
    }

    /// <summary>Ohne Kurztext steht der Gegenstand im <c>title</c>.</summary>
    [Fact]
    public void Ohne_Kurztext_traegt_der_title_den_Gegenstand()
    {
        var cut = Zeige();

        Assert.Equal("Arbeitspreis", cut.Find("button").GetAttribute("title"));
    }

    [Fact]
    public void Ein_Klick_meldet_und_ein_gesperrter_Knopf_ist_gesperrt()
    {
        var cut = Zeige();
        cut.Find("button").Click();
        Assert.Equal(1, _angefordert);

        var gesperrt = Zeige(aktiv: false);
        Assert.True(gesperrt.Find("button").HasAttribute("disabled"));
    }

    /// <summary>Hausregel „Kein Delegat, kein Knopf".</summary>
    [Fact]
    public void Ohne_Delegat_gibt_es_keinen_Knopf()
    {
        var cut = Zeige(mitDelegat: false);

        Assert.Empty(cut.FindAll("button"));
        Assert.Equal("", cut.Markup.Trim());
    }

    /// <summary>
    /// Die Wörter des aria-labels kommen vom Wirt (<c>SZP_GEPFLEGT</c>,
    /// <c>SZP_OHNE_ERWARTET_KURZ</c>) — in jeder Sprache.
    /// </summary>
    [Fact]
    public void Die_Woerter_des_aria_labels_kommen_vom_Wirt()
    {
        var cut = Render<SzenarioKnopf>(p => p
            .Add(x => x.Text, "energy price")
            .Add(x => x.Gepflegt, true)
            .Add(x => x.Warnung, true)
            .Add(x => x.GepflegtText, "maintained")
            .Add(x => x.WarnungText, "without expected value")
            .Add(x => x.Angefordert, () => { }));

        Assert.Equal("± energy price (maintained) (without expected value)",
                     cut.Find("button").GetAttribute("aria-label"));
    }
}
