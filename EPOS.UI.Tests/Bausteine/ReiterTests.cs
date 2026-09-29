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

    // =====================================================================
    //  Zweite Ebene: Kennung, Statuszeile, Leistenende
    //  (Konzept Navigation Berichte & Kosten, Etappen A1/A2)
    // =====================================================================

    /// <summary>Zwei Blätter mit Statuszeile: das erste leise ohne Kurzform, das zweite Warnung mit Kurzform.</summary>
    private static RenderFragment BlaetterMitStatus(string erstesStatus = "3 Versionen") => b =>
    {
        b.OpenComponent<Reiterblatt>(0);
        b.AddAttribute(1, "Schluessel", "EINS");
        b.AddAttribute(2, "Titel", "Erstes");
        b.AddAttribute(3, "Status", erstesStatus);
        b.AddAttribute(4, "KindInhalt",
            (RenderFragment)(x => x.AddMarkupContent(0, "<p id=\"i1\">Inhalt eins</p>")));
        b.CloseComponent();

        b.OpenComponent<Reiterblatt>(5);
        b.AddAttribute(6, "Schluessel", "ZWEI");
        b.AddAttribute(7, "Titel", "Zweites");
        b.AddAttribute(8, "Status", "3 Träger · 2 Warnungen");
        b.AddAttribute(9, "StatusKurz", "2");
        b.AddAttribute(10, "Statusstufe", Statusstufe.Warnung);
        b.CloseComponent();
    };

    [Fact]
    public void Ohne_Kennung_bleiben_die_Kennungen_wie_sie_waren()
    {
        var cut = Render<Reiter>(p => p.Add(x => x.KindInhalt, ZweiBlaetter()));

        Assert.Equal("reiter-EINS", cut.FindAll(".epos-reiter-knopf")[0].Id);
        Assert.Equal("blatt-EINS", cut.Find(".epos-reiter-blatt").Id);
    }

    [Fact]
    public void Die_Kennung_setzt_ihren_Vorsatz_vor_Knopf_und_Blatt()
    {
        var cut = Render<Reiter>(p => p
            .Add(x => x.Kennung, "bk")
            .Add(x => x.KindInhalt, ZweiBlaetter()));

        IElement knopf = cut.FindAll(".epos-reiter-knopf")[0];
        Assert.Equal("bk-reiter-EINS", knopf.Id);
        Assert.Equal("bk-blatt-EINS", knopf.GetAttribute("aria-controls"));
        Assert.Equal("bk-reiter-ZWEI", cut.FindAll(".epos-reiter-knopf")[1].Id);

        IElement blatt = cut.Find(".epos-reiter-blatt");
        Assert.Equal("bk-blatt-EINS", blatt.Id);
        Assert.Equal("bk-reiter-EINS", blatt.GetAttribute("aria-labelledby"));
    }

    /// <summary>Ein Blatt ohne Status zeichnet seinen Knopf wie immer: nur der Titel, keine Spannen.</summary>
    [Fact]
    public void Ohne_Status_traegt_der_Knopf_nur_den_Titel()
    {
        var cut = Render<Reiter>(p => p.Add(x => x.KindInhalt, ZweiBlaetter()));

        IElement knopf = cut.FindAll(".epos-reiter-knopf")[0];
        Assert.Equal("Erstes", knopf.TextContent.Trim());
        Assert.Empty(knopf.Children);
        Assert.DoesNotContain("epos-reiter-knopf--status", knopf.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Die_Statuszeile_steht_leise_unter_dem_Titel()
    {
        var cut = Render<Reiter>(p => p.Add(x => x.KindInhalt, BlaetterMitStatus()));

        IElement knopf = cut.FindAll(".epos-reiter-knopf")[0];
        Assert.Contains("epos-reiter-knopf--status", knopf.ClassName, StringComparison.Ordinal);
        Assert.Equal("Erstes", knopf.QuerySelector(".epos-reiter-titel")!.TextContent);

        IElement zeile = knopf.QuerySelector(".epos-reiter-status")!;
        Assert.Equal("epos-reiter-status", zeile.ClassName);
        Assert.Null(zeile.QuerySelector(".epos-reiter-warnzeichen"));

        // Ohne Kurzform EIN Text — keine lange und kurze Fassung nebeneinander.
        Assert.Equal("3 Versionen", Assert.Single(zeile.QuerySelectorAll(".epos-reiter-status-text")).TextContent);
        Assert.Empty(zeile.QuerySelectorAll(".epos-reiter-status-kurz"));
    }

    [Fact]
    public void Die_Warnung_traegt_Zeichen_Warnklasse_und_Kurzform()
    {
        var cut = Render<Reiter>(p => p.Add(x => x.KindInhalt, BlaetterMitStatus()));

        IElement zeile = cut.FindAll(".epos-reiter-knopf")[1].QuerySelector(".epos-reiter-status")!;
        Assert.Contains("epos-reiter-status--warnung", zeile.ClassName, StringComparison.Ordinal);

        IElement zeichen = zeile.QuerySelector(".epos-reiter-warnzeichen")!;
        Assert.Equal("▲", zeichen.TextContent);
        Assert.Equal("true", zeichen.GetAttribute("aria-hidden"));

        Assert.Equal("3 Träger · 2 Warnungen", zeile.QuerySelector(".epos-reiter-status-lang")!.TextContent);
        Assert.Equal("2", zeile.QuerySelector(".epos-reiter-status-kurz")!.TextContent);
    }

    /// <summary>Ein neuer Status des Wirts erreicht die Leiste — der Reiter zeichnet sie nach.</summary>
    [Fact]
    public void Ein_neuer_Status_erreicht_die_Leiste()
    {
        var cut = Render<Reiter>(p => p.Add(x => x.KindInhalt, BlaetterMitStatus("3 Versionen")));

        cut.Render(p => p.Add(x => x.KindInhalt, BlaetterMitStatus("4 Versionen")));

        cut.WaitForAssertion(() => Assert.Equal(
            "4 Versionen", cut.FindAll(".epos-reiter-knopf")[0].QuerySelector(".epos-reiter-status-text")!.TextContent));
    }

    [Fact]
    public void Ohne_Leistenende_bleibt_die_Leiste_allein()
    {
        var cut = Render<Reiter>(p => p.Add(x => x.KindInhalt, ZweiBlaetter()));

        Assert.Empty(cut.FindAll(".epos-reiter-kopfzeile"));
        Assert.Empty(cut.FindAll(".epos-reiter-leistenende"));
        Assert.Equal("epos-reiter-leiste", cut.Find(".epos-reiter > :first-child").ClassName);
    }

    /// <summary>
    /// Das Leistenende steht NEBEN der tablist in derselben Zeile — eine tablist trägt nur
    /// Reiter, und die Pfeiltasten im Ende wandern nicht durch die Reiter.
    /// </summary>
    [Fact]
    public void Das_Leistenende_steht_neben_der_Leiste_nicht_in_ihr()
    {
        RenderFragment ende = b => b.AddMarkupContent(0, "<span id=\"ende\">Stamm: Musterhaus</span>");
        var cut = Render<Reiter>(p => p
            .Add(x => x.Leistenende, ende)
            .Add(x => x.KindInhalt, ZweiBlaetter()));

        IElement kopfzeile = cut.Find(".epos-reiter > .epos-reiter-kopfzeile");
        Assert.Equal(2, kopfzeile.Children.Length);
        Assert.Equal("tablist", kopfzeile.Children[0].GetAttribute("role"));
        Assert.Equal("epos-reiter-leistenende", kopfzeile.Children[1].ClassName);
        Assert.Equal("Stamm: Musterhaus", kopfzeile.Children[1].QuerySelector("#ende")!.TextContent);
        Assert.Null(cut.Find(".epos-reiter-leiste").QuerySelector("#ende"));

        // Die Leiste bedient sich wie immer.
        cut.Find(".epos-reiter-leiste").KeyDown("ArrowRight");
        Assert.Equal("Inhalt zwei", cut.Find("#i2").TextContent);
    }
}
