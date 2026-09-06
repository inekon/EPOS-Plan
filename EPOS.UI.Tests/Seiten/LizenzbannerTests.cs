using System.Globalization;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// <b>Das Lizenzbanner der <see cref="AppWurzel"/></b> (Welle iF30, Anwenderentscheid
/// vom 04.09.2026).
///
/// <para>Es steht über JEDER Ansicht und auf BEIDEN Plattformen — die Wurzel ist der
/// einzige Ort, an dem das geht. Geprüft wird hier, was die Komponente daraus macht:
/// der Lesemodus als DAUERHAFTES Banner in Warnfarbe, die Warnstufen als verfallender
/// Hinweis, und ohne Lage gar nichts.</para>
///
/// <para><b>Die Komponente kennt den Lizenzkern nicht</b> (Regel S-2 aus W15c): Sie
/// bekommt ein fertiges <see cref="LizenzLage"/> herein — unter Windows als Parameter
/// aus <c>HauptfensterHuelle</c>, auf iOS über <c>IProjektQuelle.Lizenzlage()</c>.
/// Beide Wege stehen hier als eigene Fälle.</para>
/// </summary>
public class LizenzbannerTests : BunitContext
{
    private readonly CultureInfo _kulturVorher = CultureInfo.CurrentUICulture;

    public LizenzbannerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentUICulture = _kulturVorher;
        Navigationsziel.Aktuell = null;
        base.Dispose(disposing);
    }

    /// <summary>Die Lage „Lesemodus" — ohne Token, wie sie der Kern ohne Lizenz bildet.</summary>
    private static LizenzLage Lesemodus()
        => LizenzLage.Bilden(LizenzStatus.Lesemodus, null, new DateTime(2026, 9, 6));

    /// <summary>Eine Warnstufe mit fertigem Satz — sie kommt so aus dem Kern.</summary>
    private static LizenzLage Warnstufe(int tage, LizenzDringlichkeit dringlichkeit, string text)
        => new LizenzLage(LizenzStatus.Gueltig, false, tage, tage, dringlichkeit, text, "");

    private IRenderedComponent<AppWurzel> Aufbauen(LizenzLage? ueberParameter,
                                                   LizenzLage? ueberQuelle = null)
    {
        Services.AddSingleton<IProjektQuelle>(new TestProjektquelle { Lizenz = ueberQuelle });
        return Render<AppWurzel>(p => p.Add(w => w.Lizenzlage, ueberParameter));
    }

    // =====================================================================
    //  1-2  Ohne Lage kein Banner
    // =====================================================================

    /// <summary>
    /// Fall 1: Ohne Lizenzlage steht KEIN Banner. Das ist der Regelfall einer gültigen
    /// Lizenz — und der Zustand jedes bestehenden Prüfstands, der von iF30 nichts weiß.
    /// </summary>
    [Fact]
    public void Ohne_Lizenzlage_steht_kein_Banner()
    {
        var cut = Aufbauen(null);

        Assert.Empty(cut.FindAll(".epos-lizenzbanner"));
    }

    /// <summary>
    /// Fall 2: Eine ruhige Lage (gültig, fern vom Ablauf) zeigt ebenfalls nichts — ihr
    /// Text ist leer, und ein leeres Banner wäre ein leerer Kasten.
    /// </summary>
    [Fact]
    public void Eine_ruhige_Lage_zeigt_nichts()
    {
        var cut = Aufbauen(LizenzLage.Ruhig);

        Assert.Empty(cut.FindAll(".epos-lizenzbanner"));
    }

    // =====================================================================
    //  3-5  Der Lesemodus
    // =====================================================================

    /// <summary>
    /// Fall 3: Im Lesemodus steht das Banner in WARNFARBE und mit dem Satz aus dem
    /// Katalog — er nennt den Grund und den Weg zur Lizenz.
    /// </summary>
    [Fact]
    public void Im_Lesemodus_steht_das_Banner_in_Warnfarbe()
    {
        var cut = Aufbauen(Lesemodus());

        var banner = cut.Find(".epos-lizenzbanner .epos-warnbanner");
        Assert.Contains("epos-warnbanner--warnung", banner.ClassList);
        Assert.Equal("alert", banner.GetAttribute("role"));
        Assert.Equal(Resource.LIZ_BANNER_LESEMODUS,
                     cut.Find(".epos-lizenzbanner .epos-warnbanner-text").TextContent);
    }

    /// <summary>
    /// Fall 4: <b>Es bleibt stehen.</b> Der Lesemodus ist der Zustand, den der Anwender
    /// beheben MUSS und sonst nicht sieht — die einzige Begründung, die die Hausregel
    /// W16b-E-6 für ein Dauerbanner gelten lässt. Nachgewiesen an der Frist: Sie ist
    /// <c>null</c>, das Banner verfällt also nicht.
    /// </summary>
    [Fact]
    public void Das_Lesemodus_Banner_verfaellt_nicht()
    {
        var cut = Aufbauen(Lesemodus());

        var banner = cut.FindComponent<EPOS.UI.Bausteine.Warnbanner>();
        Assert.Null(banner.Instance.Verfaellt);
    }

    /// <summary>
    /// Fall 5: Es steht ÜBER der Ansicht, nicht darin — die Projektliste ist unverändert
    /// da. Ein Banner, das eine Ansicht verdrängte, nähme dem Anwender genau das, was der
    /// Lesemodus ihm ausdrücklich lässt: das Ansehen.
    /// </summary>
    [Fact]
    public void Das_Banner_steht_ueber_der_Ansicht_und_nicht_statt_ihrer()
    {
        var cut = Aufbauen(Lesemodus());

        Assert.Single(cut.FindAll(".epos-lizenzbanner"));
        Assert.Single(cut.FindAll(".epos-seite"));
    }

    // =====================================================================
    //  6-7  Die drei Warnstufen
    // =====================================================================

    /// <summary>
    /// Fall 6: Die Warnstufen 30 und 14 sind ein HINWEIS, die Stufe 7 eine WARNUNG — und
    /// alle drei VERFALLEN. Sie sind der „dezente Hinweis beim Start" aus § 6 des
    /// Konzepts; die Lizenz trägt noch, nichts ist gesperrt.
    /// </summary>
    [Theory]
    [InlineData(30, "hinweis")]
    [InlineData(14, "hinweis")]
    [InlineData(7, "warnung")]
    public void Eine_Warnstufe_steht_als_verfallender_Hinweis(int tage, string klasse)
    {
        LizenzDringlichkeit stufe = klasse == "warnung"
            ? LizenzDringlichkeit.Warnung
            : LizenzDringlichkeit.Hinweis;

        var cut = Aufbauen(Warnstufe(tage, stufe, "Die Lizenz läuft in " + tage + " Tagen ab."));

        var banner = cut.Find(".epos-lizenzbanner .epos-warnbanner");
        Assert.Contains("epos-warnbanner--" + klasse, banner.ClassList);
        Assert.Contains(tage.ToString(CultureInfo.InvariantCulture),
                        cut.Find(".epos-lizenzbanner .epos-warnbanner-text").TextContent);

        var baustein = cut.FindComponent<EPOS.UI.Bausteine.Warnbanner>();
        Assert.NotNull(baustein.Instance.Verfaellt);
    }

    /// <summary>
    /// Fall 7: Die Frist ist einstellbar — die Hülle bestimmt, wie lange ein Hinweis
    /// steht, nicht die Komponente.
    /// </summary>
    [Fact]
    public void Die_Hinweisdauer_kommt_von_aussen()
    {
        Services.AddSingleton<IProjektQuelle>(new TestProjektquelle());
        var cut = Render<AppWurzel>(p => p
            .Add(w => w.Lizenzlage, Warnstufe(30, LizenzDringlichkeit.Hinweis, "Bald abgelaufen."))
            .Add(w => w.HinweisDauer, TimeSpan.FromSeconds(5)));

        var baustein = cut.FindComponent<EPOS.UI.Bausteine.Warnbanner>();
        Assert.Equal(TimeSpan.FromSeconds(5), baustein.Instance.Verfaellt);
    }

    // =====================================================================
    //  8  Der iOS-Weg
    // =====================================================================

    /// <summary>
    /// Fall 8: Ohne Parameter fragt die Wurzel die PROJEKTQUELLE — das ist der iOS-Weg,
    /// wo es keine Seitenhülle gibt. Der Parameter hat Vorrang, wenn beide da sind.
    /// </summary>
    [Fact]
    public void Ohne_Parameter_fragt_die_Wurzel_die_Projektquelle()
    {
        var ueberQuelle = Aufbauen(null, Lesemodus());
        Assert.Equal(Resource.LIZ_BANNER_LESEMODUS,
                     ueberQuelle.Find(".epos-lizenzbanner .epos-warnbanner-text").TextContent);
    }

    /// <summary>
    /// Fall 9: Liegt beides vor, gewinnt der Parameter der Hülle — sie kennt den
    /// Arbeitsplatz, die Quelle ist der Rückfall.
    /// </summary>
    [Fact]
    public void Der_Parameter_der_Huelle_schlaegt_die_Quelle()
    {
        var cut = Aufbauen(Warnstufe(7, LizenzDringlichkeit.Warnung, "Sieben Tage."),
                           Lesemodus());

        Assert.Equal("Sieben Tage.", cut.Find(".epos-lizenzbanner .epos-warnbanner-text").TextContent);
    }

    // =====================================================================
    //  10  Beide Sprachen
    // =====================================================================

    /// <summary>
    /// Fall 10: Derselbe Zustand, andere Oberflächensprache — der Satz kommt aus dem
    /// Katalog und ist auf Englisch ein anderer. <b>Die Lage wird IN der Sprache
    /// gebildet</b>, in der sie angezeigt wird; deshalb steht der Kulturwechsel vor
    /// <c>Bilden</c> und nicht erst vor dem Zeichnen.
    /// </summary>
    [Fact]
    public void Das_Banner_spricht_beide_Sprachen()
    {
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
        string deutsch = Lesemodus().Text;

        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
        LizenzLage englisch = Lesemodus();

        Assert.NotEqual(deutsch, englisch.Text);

        var cut = Aufbauen(englisch);
        Assert.Equal(englisch.Text, cut.Find(".epos-lizenzbanner .epos-warnbanner-text").TextContent);
        Assert.Contains("Read-only", cut.Find(".epos-lizenzbanner .epos-warnbanner-text").TextContent);
    }

    // =====================================================================
    //  11-12  Einmal je Kalendertag (Anwenderentscheid iF30-O-2, 06.09.2026)
    // =====================================================================

    /// <summary>
    /// Fall 11: <b>Eine heute schon gezeigte Warnstufe bringt KEIN Banner.</b> Der
    /// Anwenderentscheid iF30‑O‑2 vom 06.09.2026 lautet „einmal täglich reicht" —
    /// gebaut war bis dahin „einmal je Programmstart".
    ///
    /// <para><b>Die Wurzel entscheidet das nicht, sie fragt.</b> Ob heute schon gewarnt
    /// wurde, steht im Kern (<c>LizenzLage.MitTagesmerker</c> über
    /// <c>LizenzWarnungMerker</c>); hier kommt es als fertiges
    /// <c>LizenzLage.WarnungZeigen</c> an. Deshalb prüft dieser Fall genau das: eine
    /// vollständige Lage mit Text und Dringlichkeit — und trotzdem kein Kasten.</para>
    /// </summary>
    [Theory]
    [InlineData(30, LizenzDringlichkeit.Hinweis)]
    [InlineData(14, LizenzDringlichkeit.Hinweis)]
    [InlineData(7, LizenzDringlichkeit.Warnung)]
    public void Eine_heute_schon_gezeigte_Warnstufe_bringt_kein_Banner(
        int tage, LizenzDringlichkeit dringlichkeit)
    {
        var stumm = new LizenzLage(LizenzStatus.Gueltig, false, tage, tage, dringlichkeit,
                                   "Die Lizenz läuft in " + tage + " Tagen ab.",
                                   "", warnungZeigen: false);

        var cut = Aufbauen(stumm);

        Assert.Empty(cut.FindAll(".epos-lizenzbanner"));
        // Die Ansicht selbst steht unveraendert da - stumm heisst „kein Hinweis",
        // nicht „keine Seite".
        Assert.Single(cut.FindAll(".epos-seite"));
    }

    /// <summary>
    /// Fall 12: <b>Das Lesemodus-Banner ist vom Tagesmerker unabhängig.</b> Es ist keine
    /// Warnstufe, sondern der Zustand, den der Anwender beheben MUSS und sonst nicht
    /// sieht (Hausregel W16b‑E‑6) — es steht bei jedem Start.
    ///
    /// <para>Nachgewiesen am echten Weg: Die Lage läuft dreimal durch
    /// <c>MitTagesmerker</c>, so wie <c>LizenzLage.Ermitteln</c> es bei drei
    /// Programmstarts täte. Der Merker wird dabei nicht einmal angefasst — deshalb
    /// braucht dieser Fall auch keine Einstellungsattrappe.</para>
    /// </summary>
    [Fact]
    public void Das_Lesemodus_Banner_ueberlebt_den_Tagesmerker()
    {
        var tag = new DateTime(2026, 9, 6);
        LizenzLage lage = Lesemodus();

        // Die Quelle wird EINMAL eingetragen: bunit baut den Dienstbehaelter beim
        // ersten Zeichnen, danach nimmt er keinen weiteren Eintrag mehr an.
        Services.AddSingleton<IProjektQuelle>(new TestProjektquelle());

        for (int start = 0; start < 3; start++)
        {
            LizenzLage nachMerker = lage.MitTagesmerker(tag);
            Assert.True(nachMerker.WarnungZeigen);

            var cut = Render<AppWurzel>(p => p.Add(w => w.Lizenzlage, nachMerker));
            Assert.Equal(Resource.LIZ_BANNER_LESEMODUS,
                         cut.Find(".epos-lizenzbanner .epos-warnbanner-text").TextContent);
        }
    }
}
