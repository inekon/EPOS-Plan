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
public class WarnbannerTests : BunitContext
{
    public WarnbannerTests()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
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
}
