using System.IO;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Klappgruppe - der allgemeine Klappabschnitt (KAL1): Vorgabe zugeklappt, Klappknopf in der Überschrift mit
/// aria-expanded und aria-controls, Inhalt nur aufgeklappt im Baum, bindbare Stellung, Kopfhandlungen, Hochkontrast.
/// </summary>
public class KlappgruppeTests : EposBunitContext
{
    private IRenderedComponent<Klappgruppe> Zeige(bool offen = false, int ebene = 4)
        => Render<Klappgruppe>(p => p
            .Add(x => x.Titel, "Gruppe")
            .Add(x => x.Zusatz, "3 Tage")
            .Add(x => x.Ebene, ebene)
            .Add(x => x.Offen, offen)
            .Add(x => x.Kopfhandlungen, "<button type=\"button\" class=\"neu\">Neu</button>")
            .AddChildContent("<p class=\"inhalt\">x</p>"));

    [Fact]
    public void Vorgabe_ist_zugeklappt_der_Inhalt_fehlt_der_Kopf_steht()
    {
        IRenderedComponent<Klappgruppe> cut = Zeige();
        IElement knopf = cut.Find("h4.epos-klappgruppe-titel > button.epos-klappgruppe-knopf[type=button]");
        Assert.Equal("false", knopf.GetAttribute("aria-expanded"));
        Assert.Equal("▸", cut.Find(".epos-klappgruppe-zeichen").TextContent);
        Assert.Equal("true", cut.Find(".epos-klappgruppe-zeichen").GetAttribute("aria-hidden"));
        Assert.Equal("3 Tage", cut.Find(".epos-klappgruppe-zusatz").TextContent);
        Assert.Empty(cut.FindAll(".inhalt"));
        Assert.NotNull(cut.Find(".epos-klappgruppe-handlungen button.neu"));
        Assert.Equal("Gruppe", cut.Find("section.epos-klappgruppe").GetAttribute("aria-label"));
    }

    [Fact]
    public void Klick_klappt_auf_und_zu_und_meldet_die_Stellung()
    {
        bool? gemeldet = null;
        IRenderedComponent<Klappgruppe> cut = Render<Klappgruppe>(p => p
            .Add(x => x.Titel, "Gruppe")
            .Add(x => x.OffenChanged, (bool o) => gemeldet = o)
            .AddChildContent("<p class=\"inhalt\">x</p>"));

        cut.Find(".epos-klappgruppe-knopf").Click();
        Assert.True(gemeldet);
        Assert.True(cut.Instance.IstOffen);
        IElement knopf = cut.Find(".epos-klappgruppe-knopf");
        Assert.Equal("true", knopf.GetAttribute("aria-expanded"));
        Assert.Equal(knopf.GetAttribute("aria-controls"), cut.Find(".epos-klappgruppe-inhalt").Id);
        Assert.NotNull(cut.Find(".epos-klappgruppe-inhalt .inhalt"));
        Assert.Contains("epos-klappgruppe--offen", cut.Find("section").ClassList);

        cut.Find(".epos-klappgruppe-knopf").Click();
        Assert.False(gemeldet);
        Assert.Empty(cut.FindAll(".inhalt"));
        Assert.Empty(cut.FindAll(".epos-klappgruppe-zusatz"));   // ohne Zusatz kein Zusatzfeld
    }

    [Fact]
    public void Der_Wirt_setzt_die_Stellung_und_die_Ebene()
    {
        IRenderedComponent<Klappgruppe> cut = Zeige(offen: true, ebene: 3);
        Assert.NotNull(cut.Find("h3.epos-klappgruppe-titel > .epos-klappgruppe-knopf"));
        Assert.NotNull(cut.Find(".inhalt"));
        Assert.Equal("▾", cut.Find(".epos-klappgruppe-zeichen").TextContent);

        cut.Render(p => p.Add(x => x.Offen, false));
        Assert.Empty(cut.FindAll(".inhalt"));
    }

    [Fact]
    public void Das_Hausblatt_hat_Touchziel_Fokus_und_Hochkontrast_fuer_die_Klappgruppe()
    {
        DirectoryInfo? d = new(System.AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css"))) d = d.Parent;
        Assert.NotNull(d);
        string css = File.ReadAllText(Path.Combine(d!.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")).Replace("\r\n", "\n");
        int knopf = css.IndexOf(".epos-klappgruppe-knopf {", System.StringComparison.Ordinal);
        Assert.True(knopf > 0);
        Assert.Contains("min-height: var(--epos-touchziel)", css.Substring(knopf, 400));
        Assert.Contains(".epos-klappgruppe-knopf:focus-visible", css);
        int hk = css.IndexOf("@media (forced-colors: active) {\n    .epos-klappgruppe-knopf", System.StringComparison.Ordinal);
        Assert.True(hk > 0, "Hochkontrastblock der Klappgruppe fehlt");
    }
}
