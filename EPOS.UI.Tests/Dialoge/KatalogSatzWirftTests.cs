using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Solarthermie;
using EPOS.UI.Dialoge.Waermepumpe;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>KT-4: Wirft das Lesen des gewählten Satzes, bleiben die Katalogdialoge nicht still.</b>
/// Der Dialog zeichnet weiter, sein Warnbanner nennt den Grund (<c>WURZEL_SATZ_FEHLER</c> mit
/// Satzname und Meldung der Ausnahme), das <see cref="Ausnahmeprotokoll"/> hat einen Vermerk,
/// und die Liste bleibt bedienbar. Die Fangstelle ist <see cref="Katalogladung.Satz{T}"/>; je
/// Dialog ein Fall, dazu Gegenfälle ohne Ausnahme.
/// </summary>
public class KatalogSatzWirftTests : EposBunitContext
{
    private const string GRUND = "Satz gesperrt";
    private const string NAME = "Gerät A";

    private readonly List<string> _vermerke = new();

    public KatalogSatzWirftTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        Ausnahmeprotokoll.Vermerkt += Mitschreiben;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Ausnahmeprotokoll.Vermerkt -= Mitschreiben;
        base.Dispose(disposing);
    }

    private void Mitschreiben(string zeile)
    {
        lock (_vermerke) _vermerke.Add(zeile);
    }

    private static IReadOnlyList<Katalogfilterzeile> Eine() => new[] { new Katalogfilterzeile(1, NAME) };

    private static T Wirft<T>() => throw new InvalidOperationException(GRUND);

    /// <summary>Wählt die Zeile in der Katalogliste (der letzten Liste des Dialogs).</summary>
    private static void ZeileWaehlen<T>(IRenderedComponent<T> cut) where T : IComponent
        => cut.FindAll(".epos-raster").Last().QuerySelectorAll(".epos-zeilenzelle--name, .epos-anlagenwahl")[0].Click();

    /// <summary>Banner mit Satzname und Grund, Vermerk, die Liste steht noch.</summary>
    private void BannerMitGrund<T>(IRenderedComponent<T> cut) where T : IComponent
    {
        string vorspann = R.WURZEL_SATZ_FEHLER.Split('{')[0];
        List<string> texte = cut.FindAll(".epos-warnbanner--warnung .epos-warnbanner-text")
                                .Select(e => e.TextContent).ToList();
        Assert.Contains(texte, t => t.StartsWith(vorspann, StringComparison.Ordinal)
                                    && t.Contains(NAME) && t.Contains(GRUND));
        lock (_vermerke)
            Assert.Contains(_vermerke, v => v.StartsWith("Katalogsatz", StringComparison.Ordinal) && v.Contains(GRUND));

        // Die Liste bleibt bedienbar: Die Zeile steht und lässt sich wieder wählen.
        Assert.NotEmpty(cut.FindAll(".epos-raster").Last().QuerySelectorAll(".epos-zeilenzelle--name, .epos-anlagenwahl"));
        ZeileWaehlen(cut);
    }

    private void OhneBanner<T>(IRenderedComponent<T> cut) where T : IComponent
    {
        string vorspann = R.WURZEL_SATZ_FEHLER.Split('{')[0];
        Assert.DoesNotContain(cut.FindAll(".epos-warnbanner-text"),
                              e => e.TextContent.StartsWith(vorspann, StringComparison.Ordinal));
        lock (_vermerke) Assert.Empty(_vermerke);
    }

    private static Katalogfilterprofil Profil(Anlagenart art)
        => Katalogfilterprofil.MitVerwendung(art, s => R.ResourceManager.GetString(s) ?? s);

    private static ErzeugerDetail Detail(string n) => new(n, "", Array.Empty<(string, string)>());

    // =================================================================================
    // Kältemaschine und Wärmepumpe (Stammblatt aus dem Satz)
    // =================================================================================

    [Fact]
    public void Kaeltemaschine_wirft_der_Satz_zeigt_der_Dialog_den_Grund_und_ein_leeres_Stammblatt()
    {
        var cut = Render<KaeltemaschineKatalogDialog>(b => b
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.Lies, _ => Wirft<KaeltemaschineDaten?>()));
        ZeileWaehlen(cut);
        BannerMitGrund(cut);
    }

    [Fact]
    public void Kaeltemaschine_ohne_Ausnahme_zeigt_keinen_Satzbanner()
    {
        var cut = Render<KaeltemaschineKatalogDialog>(b => b
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.Lies, id => new KaeltemaschineDaten { Id = id, Bezeichner = NAME }));
        ZeileWaehlen(cut);
        OhneBanner(cut);
    }

    [Fact]
    public void Waermepumpe_wirft_der_Satz_zeigt_der_Dialog_den_Grund()
    {
        var cut = Render<WaermepumpeStammDialog>(b => b
            .Add(x => x.Liste, Eine)
            .Add(x => x.Satz, _ => Wirft<WaermepumpeStammDaten?>()));
        ZeileWaehlen(cut);
        BannerMitGrund(cut);
    }

    [Fact]
    public void Waermepumpe_ohne_Ausnahme_zeigt_keinen_Satzbanner()
    {
        var cut = Render<WaermepumpeStammDialog>(b => b
            .Add(x => x.Liste, Eine)
            .Add(x => x.Satz, _ => new WaermepumpeStammDaten()));
        ZeileWaehlen(cut);
        OhneBanner(cut);
    }

    // =================================================================================
    // Die Erzeugerdialoge (Detailblock des Katalogsatzes)
    // =================================================================================

    [Fact]
    public void Heizkessel_wirft_der_Satz_zeigt_der_Dialog_den_Grund()
    {
        var cut = Render<HeizkesselDialog>(b => b
            .Add(x => x.Katalogprofil, Profil(Anlagenart.Heizkessel))
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.KatalogDetail, _ => Wirft<ErzeugerDetail?>()));
        ZeileWaehlen(cut);
        BannerMitGrund(cut);
    }

    [Fact]
    public void Heizkessel_ohne_Ausnahme_zeigt_keinen_Satzbanner()
    {
        var cut = Render<HeizkesselDialog>(b => b
            .Add(x => x.Katalogprofil, Profil(Anlagenart.Heizkessel))
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.KatalogDetail, Detail));
        ZeileWaehlen(cut);
        OhneBanner(cut);
    }

    [Fact]
    public void Bhkw_wirft_der_Satz_zeigt_der_Dialog_den_Grund()
    {
        var cut = Render<BhkwDialog>(b => b
            .Add(x => x.Katalogprofil, Profil(Anlagenart.Bhkw))
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.KatalogDetail, _ => Wirft<ErzeugerDetail?>()));
        ZeileWaehlen(cut);
        BannerMitGrund(cut);
    }

    [Fact]
    public void Pufferspeicher_wirft_der_Satz_zeigt_der_Dialog_den_Grund()
    {
        var cut = Render<PufferspeicherDialog>(b => b
            .Add(x => x.Katalogprofil, Profil(Anlagenart.Pufferspeicher))
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.KatalogDetail, _ => Wirft<ErzeugerDetail?>()));
        ZeileWaehlen(cut);
        BannerMitGrund(cut);
    }

    [Fact]
    public void Stromspeicher_wirft_der_Satz_zeigt_der_Dialog_den_Grund()
    {
        var cut = Render<StromspeicherDialog>(b => b
            .Add(x => x.Katalogprofil, Profil(Anlagenart.Stromspeicher))
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.KatalogDetail, _ => Wirft<ErzeugerDetail?>()));
        ZeileWaehlen(cut);
        BannerMitGrund(cut);
    }

    [Fact]
    public void Photovoltaik_wirft_der_Satz_zeigt_der_Dialog_den_Grund()
    {
        var cut = Render<PhotovoltaikDialog>(b => b
            .Add(x => x.Katalogprofil, Profil(Anlagenart.Photovoltaik))
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.Detail, _ => Wirft<ErzeugerDetail?>()));
        ZeileWaehlen(cut);
        BannerMitGrund(cut);
    }

    [Fact]
    public void Solarkollektoren_wirft_der_Satz_zeigt_der_Dialog_den_Grund()
    {
        var cut = Render<SolarkollektorenDialog>(b => b
            .Add(x => x.Katalogprofil, Profil(Anlagenart.Solarkollektoren))
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.Detail, _ => Wirft<ErzeugerDetail?>()));
        ZeileWaehlen(cut);
        BannerMitGrund(cut);
    }

    // =================================================================================
    // KatalogBrowser und ModulKatalog (Felder des Satzes über die Wege)
    // =================================================================================

    [Fact]
    public void KatalogBrowser_wirft_der_Satz_zeigt_der_Dialog_den_Grund()
    {
        var cut = Render<KatalogBrowserDialog>(b => b
            .Add(x => x.Art, KatalogBrowserArt.Heizkessel)
            .Add(x => x.ProfilVorgabe, KatalogBrowserProfil.Finde(KatalogBrowserArt.Heizkessel,
                     s => R.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Wege, new KatalogBrowserWege
            {
                Katalogzeilen = Eine,
                Detail = _ => Wirft<IReadOnlyList<BrowserFeldwert>?>()
            }));
        ZeileWaehlen(cut);
        BannerMitGrund(cut);
    }

    [Fact]
    public void ModulKatalog_wirft_der_Satz_zeigt_der_Dialog_den_Grund()
    {
        var cut = Render<ModulKatalogDialog>(b => b
            .Add(x => x.Art, ModulKatalogArt.Stromspeicher)
            .Add(x => x.ProfilVorgabe, ModulKatalogProfil.Finde(ModulKatalogArt.Stromspeicher,
                     s => R.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Wege, new ModulKatalogWege
            {
                Katalogzeilen = Eine,
                Detail = _ => Wirft<IReadOnlyList<ModulFeldwert>?>()
            }));
        ZeileWaehlen(cut);
        BannerMitGrund(cut);
    }

    [Fact]
    public void ModulKatalog_ohne_Ausnahme_zeigt_keinen_Satzbanner()
    {
        var cut = Render<ModulKatalogDialog>(b => b
            .Add(x => x.Art, ModulKatalogArt.Stromspeicher)
            .Add(x => x.ProfilVorgabe, ModulKatalogProfil.Finde(ModulKatalogArt.Stromspeicher,
                     s => R.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Wege, new ModulKatalogWege
            {
                Katalogzeilen = Eine,
                Detail = _ => Array.Empty<ModulFeldwert>()
            }));
        ZeileWaehlen(cut);
        OhneBanner(cut);
    }

    // =================================================================================
    // Die vier Bedarfsdialoge (Stammblatt aus dem Satz)
    // =================================================================================

    [Fact]
    public void GebaeudeAdmin_wirft_der_Satz_zeigt_der_Dialog_den_Grund()
    {
        var cut = Render<GebaeudeAdminDialog>(b => b
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerGebaeude(s => R.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.Satz, _ => Wirft<GebaeudeStammblattDaten?>()));
        BannerMitGrund(cut);
    }

    [Fact]
    public void Gebaeude_wirft_der_Katalogsatz_zeigt_der_Dialog_den_Grund()
    {
        var cut = Render<GebaeudeDialog>(b => b
            .Add(x => x.Zeilen, new List<GebaeudeProjektZeile>())
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerGebaeude(s => R.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.StammDetail, _ => Wirft<GebaeudeStammDetail?>()));
        ZeileWaehlen(cut);
        BannerMitGrund(cut);
    }

    [Fact]
    public void BedarfsProfile_wirft_der_Satz_zeigt_der_Dialog_den_Grund()
    {
        var cut = Render<BedarfsProfileDialog>(b => b
            .Add(x => x.Zeilen, new List<BedarfsProfilZeile>())
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerBedarf(BedarfsArt.Prozesswaerme, s => s).MitVerwendungsspalte(s => s))
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.Info, _ => Wirft<BedarfsProfilInfo?>()));
        ZeileWaehlen(cut);
        BannerMitGrund(cut);
    }

    [Fact]
    public void TwwNutzungsart_wirft_der_Satz_zeigt_der_Dialog_den_Grund()
    {
        var cut = Render<TwwNutzungsartAdminDialog>(b => b
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerTwwNutzungsart(s => R.ResourceManager.GetString(s) ?? s))
            // Die Nutzungsarten wählen über die Id im Schlüssel.
            .Add(x => x.Katalogzeilen, () => new[] { new Katalogfilterzeile(1, NAME) { Schluessel = "1" } })
            .Add(x => x.Detail, _ => Wirft<TwwNutzungsartDetailDaten?>()));
        BannerMitGrund(cut);
    }

    // =================================================================================
    // Die Fangstelle selbst
    // =================================================================================

    [Fact]
    public void Katalogladung_Satz_und_SatzDazu_benennen_und_halten_den_Grund()
    {
        Assert.Null(Katalogladung.Satz(() => Wirft<string?>(), NAME, out string fehler));
        Assert.Contains(GRUND, fehler);
        Assert.Contains(NAME, fehler);

        Assert.Equal("x", Katalogladung.Satz<string>(() => "x", NAME, out fehler));
        Assert.Equal("", fehler);

        string gehalten = "früher";
        Assert.Equal("x", Katalogladung.SatzDazu<string>(() => "x", NAME, ref gehalten));
        Assert.Equal("früher", gehalten);
        Assert.Null(Katalogladung.SatzDazu(() => Wirft<string?>(), NAME, ref gehalten));
        Assert.Contains(GRUND, gehalten);
    }

    // =================================================================================
    // KT-6: PV-Ganglinie und Solarganglinie — die Kennzahlen des gewählten Satzes und die Abweichung
    // der Projektkopie vom Katalog
    // =================================================================================

    private static GanglinienKatalogwege GanglinienWege() => new()
    {
        Katalogzeilen = () => Task.FromResult<IReadOnlyList<Katalogfilterzeile>>(new[] { Zeitreihenproben.Zeile(31, NAME) })
    };

    private IRenderedComponent<PvGanglinieDialog> PvGanglinie(Func<GanglinienWahl, Task<GanglinienKennzahlen?>>? kennzahlen,
                                                             Func<string, string>? abweichung,
                                                             bool mitProjektzeile = false)
        => Render<PvGanglinieDialog>(b => b
            .Add(x => x.Zeilen, mitProjektzeile
                ? new List<ErzeugerZeile> { new() { Schluessel = 1, Bezeichner = NAME, GeraetId = 31 } }
                : new List<ErzeugerZeile>())
            .Add(x => x.Katalogwege, GanglinienWege())
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.PvGanglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Kennzahlen, kennzahlen)
            .Add(x => x.KatalogAbweichung, abweichung));

    private IRenderedComponent<SolarganglinieDialog> Solarganglinie(Func<GanglinienWahl, Task<GanglinienKennzahlen?>>? kennzahlen)
        => Render<SolarganglinieDialog>(b => b
            .Add(x => x.Zeilen, new List<ErzeugerZeile>())
            .Add(x => x.Katalogwege, GanglinienWege())
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.Solarganglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Kennzahlen, kennzahlen));

    [Fact]
    public void PvGanglinie_wirft_die_Kennzahlen_zeigt_der_Dialog_den_Grund()
    {
        var cut = PvGanglinie(_ => Wirft<Task<GanglinienKennzahlen?>>(), null);
        ZeileWaehlen(cut);
        cut.WaitForAssertion(() => Assert.NotEqual("", cut.Instance.SatzFehler));
        BannerMitGrund(cut);
        Assert.Null(cut.Instance.Grafikkennzahlen);
    }

    [Fact]
    public void PvGanglinie_wirft_die_Abweichung_zeigt_der_Dialog_den_Grund_einmal()
    {
        int gefragt = 0;
        var cut = PvGanglinie(null, _ => { gefragt++; return Wirft<string>(); }, mitProjektzeile: true);
        cut.WaitForAssertion(() => Assert.NotEqual("", cut.Instance.SatzFehler));
        BannerMitGrund(cut);
        Assert.Equal(1, gefragt);
        Assert.Empty(cut.FindAll(".epos-pvg-abweichung"));
    }

    [Fact]
    public void PvGanglinie_ohne_Ausnahme_zeigt_keinen_Satzbanner()
    {
        var cut = PvGanglinie(_ => Task.FromResult<GanglinienKennzahlen?>(null), _ => "", mitProjektzeile: true);
        ZeileWaehlen(cut);
        OhneBanner(cut);
    }

    [Fact]
    public void Solarganglinie_wirft_die_Kennzahlen_zeigt_der_Dialog_den_Grund()
    {
        var cut = Solarganglinie(_ => Wirft<Task<GanglinienKennzahlen?>>());
        ZeileWaehlen(cut);
        cut.WaitForAssertion(() => Assert.NotEqual("", cut.Instance.SatzFehler));
        BannerMitGrund(cut);
    }

    [Fact]
    public void Solarganglinie_ohne_Ausnahme_zeigt_keinen_Satzbanner()
    {
        var cut = Solarganglinie(_ => Task.FromResult<GanglinienKennzahlen?>(null));
        ZeileWaehlen(cut);
        OhneBanner(cut);
    }

    [Fact]
    public async Task Die_asynchronen_Wege_der_Fangstelle_nennen_den_Grund_und_lassen_das_Ergebnis_durch()
    {
        (IReadOnlyList<int> liste, string fehler) = await Katalogladung.LesenAsync<int>(() => Wirft<Task<IReadOnlyList<int>>>(), NAME);
        Assert.Empty(liste);
        Assert.Contains(GRUND, fehler);
        (liste, fehler) = await Katalogladung.LesenAsync<int>(() => Task.FromResult<IReadOnlyList<int>>(new[] { 1 }), NAME);
        Assert.Single(liste);
        Assert.Equal("", fehler);

        (string? satz, fehler) = await Katalogladung.SatzAsync<string>(() => Wirft<Task<string?>>(), NAME);
        Assert.Null(satz);
        Assert.Contains(GRUND, fehler);
        (satz, fehler) = await Katalogladung.SatzAsync<string>(() => Task.FromResult<string?>("x"), NAME);
        Assert.Equal("x", satz);
        Assert.Equal("", fehler);

        Assert.Equal("abgelehnt", await Katalogladung.SchreibenAsync(() => Wirft<Task<string>>(), NAME, _ => "abgelehnt"));
        Assert.Equal("ok", await Katalogladung.SchreibenAsync(() => Task.FromResult("ok"), NAME, _ => "abgelehnt"));
        Assert.False(await Katalogladung.LoeschenAsync(() => Wirft<Task<bool>>(), NAME, _ => false));
        Assert.True(await Katalogladung.LoeschenAsync(() => Task.FromResult(true), NAME, _ => false));
        (int? wert, fehler) = await Katalogladung.ZurListeAsync<int?>(() => Wirft<Task<int?>>(), NAME);
        Assert.Null(wert);
        Assert.Contains(GRUND, fehler);
    }
}
