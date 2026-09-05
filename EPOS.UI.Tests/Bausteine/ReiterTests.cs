using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Reiter und Reiterblatt (iU9-W5.0) - der Ersatz fuer TabControl/TabPage.
/// Geprueft wird, was die Wellen 1 bis 4 vermisst haben: dass die Blaetter sich
/// selbst anmelden, dass nur das gewaehlte gezeichnet wird und dass die
/// Pfeiltasten wandern.
/// </summary>
public class ReiterTests : BunitContext
{
    /// <summary>Zwei Blaetter als Kindinhalt - der uebliche Aufbau.</summary>
    private static RenderFragment ZweiBlaetter(bool zweitesBedienbar = true,
                                               string sperrgrund = "") => b =>
    {
        b.OpenComponent<Reiterblatt>(0);
        b.AddAttribute(1, "Schluessel", "EINS");
        b.AddAttribute(2, "Titel", "Erstes");
        b.AddAttribute(3, "KindInhalt",
            (RenderFragment)(x => x.AddMarkupContent(0, "<p id=\"i1\">Inhalt eins</p>")));
        b.CloseComponent();

        b.OpenComponent<Reiterblatt>(4);
        b.AddAttribute(5, "Schluessel", "ZWEI");
        b.AddAttribute(6, "Titel", "Zweites");
        b.AddAttribute(7, "Bedienbar", zweitesBedienbar);
        b.AddAttribute(8, "Sperrgrund", sperrgrund);
        b.AddAttribute(9, "KindInhalt",
            (RenderFragment)(x => x.AddMarkupContent(0, "<p id=\"i2\">Inhalt zwei</p>")));
        b.CloseComponent();
    };

    [Fact]
    public void Die_Blaetter_melden_sich_selbst_an_und_stehen_in_der_Leiste()
    {
        var cut = Render<Reiter>(p => p.Add(x => x.KindInhalt, ZweiBlaetter()));

        var knoepfe = cut.FindAll(".epos-reiter-knopf");
        Assert.Equal(2, knoepfe.Count);
        Assert.Equal("Erstes", knoepfe[0].TextContent.Trim());
        Assert.Equal("Zweites", knoepfe[1].TextContent.Trim());
    }

    [Fact]
    public void Ohne_Vorgabe_steht_das_erste_Blatt_vorn()
    {
        var cut = Render<Reiter>(p => p.Add(x => x.KindInhalt, ZweiBlaetter()));

        Assert.Equal("Inhalt eins", cut.Find("#i1").TextContent);
        Assert.Empty(cut.FindAll("#i2"));
        Assert.Equal("true", cut.FindAll(".epos-reiter-knopf")[0].GetAttribute("aria-selected"));
        Assert.Equal("false", cut.FindAll(".epos-reiter-knopf")[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public void Die_Vorgabe_des_Wirts_entscheidet()
    {
        var cut = Render<Reiter>(p => p
            .Add(x => x.Aktiv, "ZWEI")
            .Add(x => x.KindInhalt, ZweiBlaetter()));

        Assert.Equal("Inhalt zwei", cut.Find("#i2").TextContent);
        Assert.Empty(cut.FindAll("#i1"));
    }

    [Fact]
    public void Ein_unbekannter_Schluessel_faellt_auf_das_erste_Blatt_zurueck()
    {
        var cut = Render<Reiter>(p => p
            .Add(x => x.Aktiv, "GIBTESNICHT")
            .Add(x => x.KindInhalt, ZweiBlaetter()));

        Assert.Equal("Inhalt eins", cut.Find("#i1").TextContent);
    }

    [Fact]
    public void Der_Klick_meldet_den_neuen_Schluessel()
    {
        string gemeldet = "";
        var cut = Render<Reiter>(p => p
            .Add(x => x.AktivChanged, (string s) => gemeldet = s)
            .Add(x => x.KindInhalt, ZweiBlaetter()));

        cut.FindAll(".epos-reiter-knopf")[1].Click();

        Assert.Equal("ZWEI", gemeldet);
        Assert.Equal("Inhalt zwei", cut.Find("#i2").TextContent);
    }

    [Fact]
    public void Ein_zweiter_Klick_auf_den_aktiven_Reiter_meldet_nichts()
    {
        int mal = 0;
        var cut = Render<Reiter>(p => p
            .Add(x => x.AktivChanged, (string _) => mal++)
            .Add(x => x.KindInhalt, ZweiBlaetter()));

        cut.FindAll(".epos-reiter-knopf")[0].Click();

        Assert.Equal(0, mal);
    }

    [Fact]
    public void Die_Leiste_und_die_Knoepfe_tragen_die_Rollen()
    {
        var cut = Render<Reiter>(p => p
            .Add(x => x.Bezeichnung, "Seiten")
            .Add(x => x.KindInhalt, ZweiBlaetter()));

        var leiste = cut.Find(".epos-reiter-leiste");
        Assert.Equal("tablist", leiste.GetAttribute("role"));
        Assert.Equal("Seiten", leiste.GetAttribute("aria-label"));

        var knopf = cut.FindAll(".epos-reiter-knopf")[0];
        Assert.Equal("tab", knopf.GetAttribute("role"));
        Assert.Equal("blatt-EINS", knopf.GetAttribute("aria-controls"));

        var blatt = cut.Find(".epos-reiter-blatt");
        Assert.Equal("tabpanel", blatt.GetAttribute("role"));
        Assert.Equal("blatt-EINS", blatt.GetAttribute("id"));
        Assert.Equal("reiter-EINS", blatt.GetAttribute("aria-labelledby"));
    }

    [Fact]
    public void Nur_der_aktive_Knopf_steht_im_Tabulatorzyklus()
    {
        var cut = Render<Reiter>(p => p.Add(x => x.KindInhalt, ZweiBlaetter()));

        Assert.Equal("0", cut.FindAll(".epos-reiter-knopf")[0].GetAttribute("tabindex"));
        Assert.Equal("-1", cut.FindAll(".epos-reiter-knopf")[1].GetAttribute("tabindex"));
    }

    [Fact]
    public void Pfeil_rechts_wandert_zum_naechsten_Blatt()
    {
        string gemeldet = "";
        var cut = Render<Reiter>(p => p
            .Add(x => x.AktivChanged, (string s) => gemeldet = s)
            .Add(x => x.KindInhalt, ZweiBlaetter()));

        cut.Find(".epos-reiter-leiste").KeyDown("ArrowRight");

        Assert.Equal("ZWEI", gemeldet);
    }

    [Fact]
    public void Pfeil_links_laeuft_um_und_landet_auf_dem_letzten_Blatt()
    {
        string gemeldet = "";
        var cut = Render<Reiter>(p => p
            .Add(x => x.AktivChanged, (string s) => gemeldet = s)
            .Add(x => x.KindInhalt, ZweiBlaetter()));

        cut.Find(".epos-reiter-leiste").KeyDown("ArrowLeft");

        Assert.Equal("ZWEI", gemeldet);
    }

    [Fact]
    public void Pos1_und_Ende_springen_an_die_Enden()
    {
        string gemeldet = "";
        var cut = Render<Reiter>(p => p
            .Add(x => x.AktivChanged, (string s) => gemeldet = s)
            .Add(x => x.KindInhalt, ZweiBlaetter()));

        cut.Find(".epos-reiter-leiste").KeyDown("End");
        Assert.Equal("ZWEI", gemeldet);
    }

    [Fact]
    public void Ein_gesperrtes_Blatt_bleibt_sichtbar_und_wird_uebersprungen()
    {
        string gemeldet = "";
        var cut = Render<Reiter>(p => p
            .Add(x => x.AktivChanged, (string s) => gemeldet = s)
            .Add(x => x.KindInhalt, ZweiBlaetter(zweitesBedienbar: false)));

        Assert.True(cut.FindAll(".epos-reiter-knopf")[1].HasAttribute("disabled"));

        cut.Find(".epos-reiter-leiste").KeyDown("ArrowRight");

        // Nur EIN bedienbares Blatt: der Umlauf landet wieder dort, es wird
        // nichts gemeldet.
        Assert.Equal("", gemeldet);

        // Ohne Sperrgrund ist die Sperre HART - kein ARIA-Zustand, kein
        // Tooltip, kein Ereignis (die Bauart seit W5.0).
        IElement knopf = cut.FindAll(".epos-reiter-knopf")[1];
        Assert.False(knopf.HasAttribute("aria-disabled"));
        Assert.False(knopf.HasAttribute("title"));
    }

    /// <summary>
    /// <b>Die WEICHE Sperre</b> (Anwenderwunsch <b>W16b‑E‑6</b>, 05.09.2026).
    ///
    /// <para>Nennt ein Blatt seinen <c>Sperrgrund</c>, bleibt sein Knopf ein
    /// Knopf: <c>aria-disabled</c> statt <c>disabled</c>, der Grund als
    /// <c>title</c> — und der Versuch meldet sich als <c>Verweigert</c>, ohne
    /// dass das Blatt gewechselt würde. Genau das kann ein <c>disabled</c>-Knopf
    /// nicht: Er nimmt keine Zeigerereignisse an, zeigt deshalb keinen Tooltip
    /// und feuert nicht.</para>
    /// </summary>
    [Fact]
    public void Ein_weich_gesperrtes_Blatt_nennt_seinen_Grund_und_meldet_den_Versuch()
    {
        string gewechselt = "";
        string verweigert = "";

        var cut = Render<Reiter>(p => p
            .Add(x => x.AktivChanged, (string s) => gewechselt = s)
            .Add(x => x.Verweigert, (string s) => verweigert = s)
            .Add(x => x.KindInhalt,
                 ZweiBlaetter(zweitesBedienbar: false, sperrgrund: "Erst nach der Projektwahl")));

        IElement knopf = cut.FindAll(".epos-reiter-knopf")[1];

        Assert.False(knopf.HasAttribute("disabled"));          // sonst gaebe es kein Ereignis
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Equal("Erst nach der Projektwahl", knopf.GetAttribute("title"));

        knopf.Click();

        Assert.Equal("ZWEI", verweigert);
        Assert.Equal("", gewechselt);                          // gewechselt wird NICHT
        Assert.Equal("EINS", cut.Instance.AktiverSchluessel);
    }

    /// <summary>
    /// Die Pfeiltasten überspringen auch ein WEICH gesperrtes Blatt — die
    /// Sperre ist dieselbe, nur ihre Auskunft ist anders.
    /// </summary>
    [Fact]
    public void Auch_ein_weich_gesperrtes_Blatt_wird_von_den_Pfeiltasten_uebersprungen()
    {
        string gemeldet = "";
        var cut = Render<Reiter>(p => p
            .Add(x => x.AktivChanged, (string s) => gemeldet = s)
            .Add(x => x.KindInhalt,
                 ZweiBlaetter(zweitesBedienbar: false, sperrgrund: "Erst nach der Projektwahl")));

        cut.Find(".epos-reiter-leiste").KeyDown("ArrowRight");

        Assert.Equal("", gemeldet);
    }

    [Fact]
    public void Das_Betreten_eines_Blattes_wird_gemeldet()
    {
        int betreten = 0;
        RenderFragment inhalt = b =>
        {
            b.OpenComponent<Reiterblatt>(0);
            b.AddAttribute(1, "Schluessel", "EINS");
            b.AddAttribute(2, "Titel", "Erstes");
            b.CloseComponent();

            b.OpenComponent<Reiterblatt>(3);
            b.AddAttribute(4, "Schluessel", "ZWEI");
            b.AddAttribute(5, "Titel", "Zweites");
            b.AddAttribute(6, "Betreten", EventCallback.Factory.Create(this, () => betreten++));
            b.CloseComponent();
        };

        var cut = Render<Reiter>(p => p.Add(x => x.KindInhalt, inhalt));
        Assert.Equal(0, betreten);

        cut.FindAll(".epos-reiter-knopf")[1].Click();

        Assert.Equal(1, betreten);
    }
}
