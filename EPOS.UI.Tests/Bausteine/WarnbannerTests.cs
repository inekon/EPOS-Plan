using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Warnbanner - die Meldung im Dialog statt als MessageBox.
///
/// <para>Seit iU9-W15b.1 zusaetzlich der SELBSTVERFALL (Zeuge T-6): der Ersatz
/// fuer <c>Form_Hinweis</c>, den Kurzhinweis, der sich nach drei Sekunden selbst
/// schliesst. Geprueft wird mit einer GESTEUERTEN UHR - ein Test, der drei
/// Sekunden schlaeft, wird irgendwann uebersprungen.</para>
///
/// <para>Die Klasse pinnt die Sprache selbst (Regel seit W8).</para>
/// </summary>
public class WarnbannerTests : EposBunitContext
{
    public WarnbannerTests()
    {
    }

    /// <summary>
    /// Eine Uhr, die auf Zuruf ablaeuft. Sie meldet zurueck, mit WELCHER Frist sie
    /// gerufen wurde - der Vorlaeufer wartete genau drei Sekunden, und das ist Teil
    /// der Zusage.
    /// </summary>
    private sealed class Handuhr
    {
        private readonly TaskCompletionSource _laeuft = new();

        internal TimeSpan? Frist { get; private set; }

        internal Task Warten(TimeSpan frist, CancellationToken marke)
        {
            Frist = frist;
            marke.Register(() => _laeuft.TrySetCanceled());
            return _laeuft.Task;
        }

        /// <summary>Laesst die Frist ablaufen.</summary>
        internal void Ablaufen() => _laeuft.TrySetResult();
    }

    [Theory]
    [InlineData(WarnStufe.Hinweis, "epos-warnbanner--hinweis")]
    [InlineData(WarnStufe.Warnung, "epos-warnbanner--warnung")]
    [InlineData(WarnStufe.Fehler, "epos-warnbanner--fehler")]
    public void Jede_Stufe_hat_ihre_Zustandsklasse(WarnStufe stufe, string klasse)
    {
        var cut = Render<Warnbanner>(p => p
            .Add(x => x.Stufe, stufe)
            .Add(x => x.Text, "Bitte einen Variantennamen (Code) eingeben."));

        Assert.Contains(klasse, cut.Find("div").ClassName);
    }

    [Fact]
    public void Der_Text_wird_gezeigt_und_als_alert_gemeldet()
    {
        var cut = Render<Warnbanner>(p => p.Add(x => x.Text, "Bitte einen Variantennamen (Code) eingeben."));

        Assert.Equal("alert", cut.Find("div").GetAttribute("role"));
        Assert.Equal("Bitte einen Variantennamen (Code) eingeben.",
                     cut.Find(".epos-warnbanner-text").TextContent);
    }

    [Fact]
    public void Ohne_Angabe_ist_die_Stufe_Warnung()
    {
        var cut = Render<Warnbanner>(p => p.Add(x => x.Text, "Hinweis"));

        Assert.Contains("epos-warnbanner--warnung", cut.Find("div").ClassName);
    }

    // ==================================================================
    //  T-6  Der Selbstverfall (iU9-W15b.1)
    // ==================================================================

    /// <summary>
    /// Ohne <c>Verfaellt</c> bleibt das Banner stehen — das ist die Vorgabe und das
    /// Verhalten aller bisherigen Wirte.
    /// </summary>
    [Fact]
    public void Ohne_Frist_bleibt_das_Banner_stehen()
    {
        var uhr = new Handuhr();

        var cut = Render<Warnbanner>(p => p
            .Add(x => x.Text, "Projekt Muster GmbH geöffnet!")
            .Add(x => x.Uhr, uhr.Warten));

        Assert.Null(uhr.Frist);
        Assert.NotNull(cut.Find("div.epos-warnbanner"));
    }

    /// <summary>
    /// <b>Der Fall des Vorläufers.</b> Mit einer Frist verschwindet das Banner, sobald
    /// sie abgelaufen ist — und die Frist ist genau die übergebene (drei Sekunden bei
    /// <c>Form_Hinweis</c>). Vorher steht es.
    /// </summary>
    [Fact]
    public void Nach_Ablauf_der_Frist_verschwindet_das_Banner()
    {
        var uhr = new Handuhr();

        var cut = Render<Warnbanner>(p => p
            .Add(x => x.Stufe, WarnStufe.Hinweis)
            .Add(x => x.Text, "Projekt Muster GmbH geöffnet!")
            .Add(x => x.Verfaellt, TimeSpan.FromSeconds(3))
            .Add(x => x.Uhr, uhr.Warten));

        Assert.Equal(TimeSpan.FromSeconds(3), uhr.Frist);
        Assert.NotEmpty(cut.FindAll("div.epos-warnbanner"));

        uhr.Ablaufen();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("div.epos-warnbanner")),
                             TimeSpan.FromSeconds(10));
    }

    /// <summary>Der Wirt erfährt vom Verfall und darf seine Meldung vergessen.</summary>
    [Fact]
    public void Der_Verfall_wird_gemeldet()
    {
        var uhr = new Handuhr();
        int gemeldet = 0;

        var cut = Render<Warnbanner>(p => p
            .Add(x => x.Text, "Projekt Muster GmbH geöffnet!")
            .Add(x => x.Verfaellt, TimeSpan.FromSeconds(3))
            .Add(x => x.Uhr, uhr.Warten)
            .Add(x => x.Verfallen, EventCallback.Factory.Create(this, () => gemeldet++)));

        uhr.Ablaufen();

        cut.WaitForAssertion(() => Assert.Equal(1, gemeldet), TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// <b>Eine NEUE Meldung setzt den Verfall zurück.</b> Sonst bliebe das Banner
    /// nach dem ersten Hinweis für immer verschwunden — und der zweite „Projekt
    /// geöffnet" käme nie an. Der Vorläufer hatte das Problem nicht: Er legte jedes
    /// Mal ein neues Fenster an.
    /// </summary>
    [Fact]
    public void Eine_neue_Meldung_setzt_den_Verfall_zurueck()
    {
        var uhr = new Handuhr();

        var cut = Render<Warnbanner>(p => p
            .Add(x => x.Text, "Erste Meldung")
            .Add(x => x.Verfaellt, TimeSpan.FromSeconds(3))
            .Add(x => x.Uhr, uhr.Warten));

        uhr.Ablaufen();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("div.epos-warnbanner")),
                             TimeSpan.FromSeconds(10));

        var zweite = new Handuhr();
        cut.Render(p => p
            .Add(x => x.Text, "Zweite Meldung")
            .Add(x => x.Verfaellt, TimeSpan.FromSeconds(3))
            .Add(x => x.Uhr, zweite.Warten));

        Assert.Equal("Zweite Meldung", cut.Find(".epos-warnbanner-text").TextContent);
    }

    /// <summary>
    /// Eine Frist von null oder darunter heißt „kein Verfall" — nicht „sofort weg".
    /// Ein Banner, das nie erscheint, wäre eine verlorene Meldung.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Eine_Frist_von_null_laesst_das_Banner_stehen(int sekunden)
    {
        var uhr = new Handuhr();

        var cut = Render<Warnbanner>(p => p
            .Add(x => x.Text, "Bleibt")
            .Add(x => x.Verfaellt, TimeSpan.FromSeconds(sekunden))
            .Add(x => x.Uhr, uhr.Warten));

        Assert.Null(uhr.Frist);
        Assert.NotNull(cut.Find("div.epos-warnbanner"));
    }

    /// <summary>
    /// Das Kreuz zum Ausblenden (Anwenderentscheid 06.10.2026): Es steht in jedem Banner,
    /// sichtbar macht es das Hausblatt nur dort, wo das Banner haftet. Ein Klick blendet
    /// aus und meldet <see cref="Warnbanner.Verfallen"/>; ein neuer Text zeigt es wieder.
    /// </summary>
    [Fact]
    public void Das_Kreuz_blendet_aus_und_ein_neuer_Text_zeigt_wieder()
    {
        using var kultur = new Kulturvorrichtung("de-DE");
        int verfallen = 0;
        var cut = Render<Warnbanner>(p => p
            .Add(x => x.Stufe, WarnStufe.Fehler)
            .Add(x => x.Text, "Gebäude kann nicht gelöscht werden.")
            .Add(x => x.Verfallen, () => verfallen++));

        var kreuz = cut.Find("div.epos-warnbanner > button.epos-warnbanner-schliessen");
        Assert.Equal("Meldung ausblenden", kreuz.GetAttribute("title"));
        Assert.Equal("Meldung ausblenden", kreuz.GetAttribute("aria-label"));
        Assert.Equal("button", kreuz.GetAttribute("type"));

        kreuz.Click();

        Assert.Empty(cut.FindAll("div.epos-warnbanner"));
        Assert.Equal(1, verfallen);

        cut.Render(p => p.Add(x => x.Text, "Zweite Meldung"));
        Assert.Equal("Zweite Meldung", cut.Find(".epos-warnbanner-text").TextContent);
    }

    /// <summary>Das Kreuz bricht einen laufenden Selbstverfall ab - er meldet nicht doppelt.</summary>
    [Fact]
    public void Das_Kreuz_beendet_den_Selbstverfall()
    {
        int verfallen = 0;
        var uhr = new Handuhr();
        var cut = Render<Warnbanner>(p => p
            .Add(x => x.Text, "Projekt geöffnet")
            .Add(x => x.Verfaellt, TimeSpan.FromSeconds(3))
            .Add(x => x.Uhr, uhr.Warten)
            .Add(x => x.Verfallen, () => verfallen++));

        cut.Find(".epos-warnbanner-schliessen").Click();
        uhr.Ablaufen();

        Assert.Empty(cut.FindAll("div.epos-warnbanner"));
        Assert.Equal(1, verfallen);
    }
    // =====================================================================
    //  Die Hinweisliste (Anwenderbefund 10.10.2026)
    // =====================================================================

    private static readonly string[] DreiHinweise =
        { "Erster Hinweis", "Zweiter Hinweis", "Dritter Hinweis" };

    /// <summary>
    /// Ein Hinweis mit Liste steht zugeklappt als EINE Zeile: Kopf, Zahl, Klapper - die
    /// Liste selbst ist nicht im Markup.
    /// </summary>
    [Fact]
    public void Ein_Hinweis_mit_Liste_steht_zugeklappt_als_eine_Zeile_mit_Zahl()
    {
        var cut = Render<Warnbanner>(p => p
            .Add(x => x.Stufe, WarnStufe.Hinweis)
            .Add(x => x.Text, "Stamm: Haus: ok (Ergebnis-ID 357)")
            .Add(x => x.Hinweise, DreiHinweise));

        Assert.Contains("epos-warnbanner--liste", cut.Find("div.epos-warnbanner").ClassName);
        Assert.Equal("Stamm: Haus: ok (Ergebnis-ID 357)", cut.Find(".epos-warnbanner-text").TextContent);
        Assert.Equal("· 3 Hinweise", cut.Find(".epos-warnbanner-zahl").TextContent);

        var klapper = cut.Find("button.epos-warnbanner-klapper");
        Assert.Equal("false", klapper.GetAttribute("aria-expanded"));
        Assert.Contains("Hinweise anzeigen", klapper.TextContent);
        Assert.Empty(cut.FindAll(".epos-warnbanner-liste"));
        Assert.DoesNotContain("Erster Hinweis", cut.Markup);
    }

    /// <summary>
    /// Der Klapper ist ein echter Knopf (Enter und Leertaste bedient der Browser): Der
    /// Klick klappt auf - ein Eintrag je Hinweis, aria-expanded und aria-controls auf
    /// die Liste -, ein zweiter Klick wieder zu.
    /// </summary>
    [Fact]
    public void Der_Klapper_klappt_die_Liste_auf_und_wieder_zu()
    {
        var cut = Render<Warnbanner>(p => p
            .Add(x => x.Stufe, WarnStufe.Hinweis)
            .Add(x => x.Text, "Kopf")
            .Add(x => x.Hinweise, DreiHinweise));

        cut.Find("button.epos-warnbanner-klapper").Click();

        var klapper = cut.Find("button.epos-warnbanner-klapper");
        Assert.Equal("button", klapper.GetAttribute("type"));
        Assert.Equal("true", klapper.GetAttribute("aria-expanded"));
        Assert.Contains("Hinweise ausblenden", klapper.TextContent);
        var liste = cut.Find("ul.epos-warnbanner-liste");
        Assert.Equal(liste.Id, klapper.GetAttribute("aria-controls"));
        Assert.Equal(3, liste.QuerySelectorAll("li").Length);
        Assert.Equal("Zweiter Hinweis", liste.QuerySelectorAll("li")[1].TextContent);

        cut.Find("button.epos-warnbanner-klapper").Click();
        Assert.Empty(cut.FindAll(".epos-warnbanner-liste"));
        Assert.False(cut.Instance.IstOffen);
    }

    /// <summary>Die gewählte Stellung bleibt über einen neuen Text stehen.</summary>
    [Fact]
    public void Die_Stellung_bleibt_ueber_einen_neuen_Text()
    {
        var cut = Render<Warnbanner>(p => p
            .Add(x => x.Stufe, WarnStufe.Hinweis)
            .Add(x => x.Text, "Kopf")
            .Add(x => x.Hinweise, DreiHinweise));
        cut.Find("button.epos-warnbanner-klapper").Click();

        cut.Render(p => p.Add(x => x.Text, "Neuer Kopf"));

        Assert.True(cut.Instance.IstOffen);
        Assert.NotEmpty(cut.FindAll(".epos-warnbanner-liste"));
    }

    /// <summary>Ein Hinweis zählt in der Einzahl.</summary>
    [Fact]
    public void Ein_einzelner_Hinweis_zaehlt_in_der_Einzahl()
    {
        var cut = Render<Warnbanner>(p => p
            .Add(x => x.Stufe, WarnStufe.Hinweis)
            .Add(x => x.Text, "Kopf")
            .Add(x => x.Hinweise, new[] { "Nur einer" }));

        Assert.Equal("· 1 Hinweis", cut.Find(".epos-warnbanner-zahl").TextContent);
    }

    /// <summary>Fehler und Warnung mit Liste stehen OFFEN - ein Fehler wird nicht versteckt.</summary>
    [Theory]
    [InlineData(WarnStufe.Fehler)]
    [InlineData(WarnStufe.Warnung)]
    public void Fehler_und_Warnung_mit_Liste_stehen_aufgeklappt(WarnStufe stufe)
    {
        var cut = Render<Warnbanner>(p => p
            .Add(x => x.Stufe, stufe)
            .Add(x => x.Text, "Kopf")
            .Add(x => x.Hinweise, DreiHinweise));

        Assert.Equal("true", cut.Find("button.epos-warnbanner-klapper").GetAttribute("aria-expanded"));
        Assert.Equal(3, cut.FindAll("ul.epos-warnbanner-liste > li").Count);
    }

    /// <summary>Ohne Liste (null oder leer) bleibt das Banner, wie es war: kein Klapper, keine Zahl.</summary>
    [Fact]
    public void Ohne_Liste_bleibt_das_Banner_unveraendert()
    {
        var ohne = Render<Warnbanner>(p => p
            .Add(x => x.Stufe, WarnStufe.Hinweis)
            .Add(x => x.Text, "Nur Text"));
        var leer = Render<Warnbanner>(p => p
            .Add(x => x.Stufe, WarnStufe.Hinweis)
            .Add(x => x.Text, "Nur Text")
            .Add(x => x.Hinweise, Array.Empty<string>()));

        foreach (var cut in new[] { ohne, leer })
        {
            Assert.Equal("epos-warnbanner epos-warnbanner--hinweis", cut.Find("div.epos-warnbanner").ClassName);
            Assert.Empty(cut.FindAll(".epos-warnbanner-klapper"));
            Assert.Empty(cut.FindAll(".epos-warnbanner-zahl"));
            Assert.Empty(cut.FindAll(".epos-warnbanner-liste"));
            Assert.Equal("Nur Text", cut.Find(".epos-warnbanner-text").TextContent);
        }
    }

    /// <summary>
    /// Die Stilregel der Liste: Rollbereich mit begrenzter Höhe, der innen rollt
    /// (bunit misst keine Höhe - geprüft wird die Regel).
    /// </summary>
    [Fact]
    public void Die_Liste_rollt_in_begrenzter_Hoehe()
    {
        string css = StilblattLesen();
        int i = css.IndexOf(".epos-warnbanner-liste {", StringComparison.Ordinal);
        Assert.True(i >= 0, "Regel .epos-warnbanner-liste fehlt");
        string regel = css.Substring(i, css.IndexOf('}', i) - i);
        Assert.Contains("max-height: 40vh", regel);
        Assert.Contains("overflow-y: auto", regel);
        Assert.Contains("flex-wrap: wrap", css.Substring(css.IndexOf(".epos-warnbanner--liste {", StringComparison.Ordinal), 60));
    }

    private static string StilblattLesen()
    {
        string? ordner = AppContext.BaseDirectory;
        while (ordner != null && !System.IO.File.Exists(System.IO.Path.Combine(ordner, "EPOS.UI", "wwwroot", "epos-ui.css")))
            ordner = System.IO.Path.GetDirectoryName(ordner);
        Assert.NotNull(ordner);
        return System.IO.File.ReadAllText(System.IO.Path.Combine(ordner!, "EPOS.UI", "wwwroot", "epos-ui.css"));
    }
}
