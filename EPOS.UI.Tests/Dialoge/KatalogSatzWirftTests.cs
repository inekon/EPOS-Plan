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
    // KT-7: Bedarfsverwaltung, Gebäudetypen, Baustoffe, Bauteilaufbauten
    // =================================================================================

    private IRenderedComponent<BedarfAdminDialog> BedarfAdmin(Func<string, string>? jahressumme = null,
                                                              Func<string, (string, string)?>? kopf = null,
                                                              Func<string, double[]?>? monate = null)
        => Render<BedarfAdminDialog>(b => b
            .Add(x => x.Art, BedarfsArt.Stromverbraucher)
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerBedarf(BedarfsArt.Stromverbraucher, s => s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Jahressumme, jahressumme ?? (_ => "1"))
            .Add(x => x.Kopf, kopf ?? (_ => ("B", "T")))
            .Add(x => x.Monatswerte, monate ?? (_ => new double[12])));

    [Fact]
    public void BedarfAdmin_wirft_die_Jahressumme_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(BedarfAdmin(jahressumme: _ => Wirft<string>()));

    [Fact]
    public void BedarfAdmin_wirft_der_Kopf_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(BedarfAdmin(kopf: _ => Wirft<(string, string)?>()));

    [Fact]
    public void BedarfAdmin_wirft_die_Monatswerte_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(BedarfAdmin(monate: _ => Wirft<double[]?>()));

    [Fact]
    public void BedarfAdmin_ohne_Ausnahme_zeigt_keinen_Satzbanner()
        => OhneBanner(BedarfAdmin());

    private IRenderedComponent<GebaeudetypDialog> Gebaeudetyp(Func<string, GebaeudetypDaten?> lies)
        => Render<GebaeudetypDialog>(b => b
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerGebaeudetyp(s => R.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Lies, lies));

    [Fact]
    public void Gebaeudetyp_wirft_der_Satz_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Gebaeudetyp(_ => Wirft<GebaeudetypDaten?>()));

    [Fact]
    public void Gebaeudetyp_ohne_Ausnahme_zeigt_keinen_Satzbanner()
        => OhneBanner(Gebaeudetyp(_ => null));

    private IRenderedComponent<BaustoffKatalogDialog> Baustoff(Func<int, BaustoffDaten?> lies)
        => Render<BaustoffKatalogDialog>(b => b
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerBaustoff(s => R.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Lies, lies));

    [Fact]
    public void Baustoff_wirft_der_Satz_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Baustoff(_ => Wirft<BaustoffDaten?>()));

    [Fact]
    public void Baustoff_ohne_Ausnahme_zeigt_keinen_Satzbanner()
        => OhneBanner(Baustoff(_ => null));

    private IRenderedComponent<BauteilaufbauDialog> Bauteilaufbau(Func<int, BauteilaufbauDaten?> lies)
        => Render<BauteilaufbauDialog>(b => b
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerBauteilaufbau(s => R.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Lies, lies));

    [Fact]
    public void Bauteilaufbau_wirft_der_Satz_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Bauteilaufbau(_ => Wirft<BauteilaufbauDaten?>()));

    [Fact]
    public void Bauteilaufbau_ohne_Ausnahme_zeigt_keinen_Satzbanner()
        => OhneBanner(Bauteilaufbau(_ => null));

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
    public void Kaeltemaschinenanlage_wirft_der_Satz_zeigt_der_Dialog_den_Grund()
    {
        var cut = Render<KaeltemaschineAnlageDialog>(b => b
            .Add(x => x.Katalogprofil, Profil(Anlagenart.Kaeltemaschine))
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.Katalogwerte, _ => Wirft<KaeltemaschineGeraetwerte?>()));
        ZeileWaehlen(cut);
        BannerMitGrund(cut);
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

    // =================================================================================
    // Die Ganglinien-Wirte (Kennzahlen der gewählten Ganglinie)
    // =================================================================================

    [Fact]
    public void WaermebedarfExtern_wirft_der_Satz_zeigt_der_Dialog_den_Grund()
    {
        var cut = Render<WaermebedarfExternDialog>(b => b
            .Add(x => x.Zeilen, new List<WaermebedarfExternZeile>())
            .Add(x => x.Katalogzeilen, () => Task.FromResult(Eine()))
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.Waermebedarf))
            .Add(x => x.Kennzahlen, _ => Task.FromException<GanglinienKennzahlen?>(new InvalidOperationException(GRUND))));
        ZeileWaehlen(cut);
        BannerMitGrund(cut);
    }

    [Fact]
    public void Stromganglinie_wirft_der_Satz_zeigt_der_Dialog_den_Grund()
    {
        var cut = Render<EPOS.UI.Dialoge.Strom.StromganglinieDialog>(b => b
            .Add(x => x.Zeilen, new List<EPOS.UI.Dialoge.Strom.GanglinienProjektZeile>())
            .Add(x => x.Katalogzeilen, () => Task.FromResult(Eine()))
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.Stromganglinie))
            .Add(x => x.Kennzahlen, _ => Task.FromException<GanglinienKennzahlen?>(new InvalidOperationException(GRUND))));
        ZeileWaehlen(cut);
        BannerMitGrund(cut);
    }

    [Fact]
    public void WaermebedarfExtern_ohne_Ausnahme_zeigt_keinen_Satzbanner()
    {
        var cut = Render<WaermebedarfExternDialog>(b => b
            .Add(x => x.Zeilen, new List<WaermebedarfExternZeile>())
            .Add(x => x.Katalogzeilen, () => Task.FromResult(Eine()))
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.Waermebedarf))
            .Add(x => x.Kennzahlen, _ => Task.FromResult<GanglinienKennzahlen?>(null)));
        ZeileWaehlen(cut);
        OhneBanner(cut);
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

    // =================================================================================
    // Die Ganglinien-Dialoge (Kennzahlen des gewählten Satzes, Katalogauswahl V1 Stufe 4)
    // =================================================================================

    [Fact]
    public void Solarganglinie_wirft_der_Satz_zeigt_der_Dialog_den_Grund()
    {
        var cut = Render<SolarganglinieDialog>(b => b
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.Solarganglinie))
            .Add(x => x.Katalogwege, new GanglinienKatalogwege { Katalogzeilen = () => Task.FromResult(Eine()) })
            .Add(x => x.Kennzahlen, _ => Wirft<Task<GanglinienKennzahlen?>>()));
        ZeileWaehlen(cut);
        BannerMitGrund(cut);
    }

    [Fact]
    public void PvGanglinie_wirft_der_Satz_zeigt_der_Dialog_den_Grund()
    {
        var cut = Render<PvGanglinieDialog>(b => b
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.PvGanglinie))
            .Add(x => x.Katalogwege, new GanglinienKatalogwege { Katalogzeilen = () => Task.FromResult(Eine()) })
            .Add(x => x.Kennzahlen, _ => Wirft<Task<GanglinienKennzahlen?>>()));
        ZeileWaehlen(cut);
        BannerMitGrund(cut);
    }

    [Fact]
    public void Solarganglinie_ohne_Ausnahme_zeigt_keinen_Satzbanner()
    {
        var cut = Render<SolarganglinieDialog>(b => b
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.Solarganglinie))
            .Add(x => x.Katalogwege, new GanglinienKatalogwege { Katalogzeilen = () => Task.FromResult(Eine()) })
            .Add(x => x.Kennzahlen, _ => Task.FromResult<GanglinienKennzahlen?>(null)));
        ZeileWaehlen(cut);
        OhneBanner(cut);
    }

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

    // =================================================================================
    // KT-9: die Vergleichsgabe der Katalogliste in den Projektdialogen
    // =================================================================================

    private const string ZWEITER = "Gerät B";

    private static IReadOnlyList<Katalogfilterzeile> Zwei()
        => new[] { new Katalogfilterzeile(1, NAME), new Katalogfilterzeile(2, ZWEITER) };

    /// <summary>Die Gabe: <see cref="ZWEITER"/> liefert einen Wert, <see cref="NAME"/> wirft, wenn gewünscht.</summary>
    private static Func<string, IReadOnlyList<Parameterwert>> Gabe(bool wirft)
        => name => wirft && name == NAME
            ? throw new InvalidOperationException(GRUND)
            : new[] { new Parameterwert(new ParameterEintrag("Leistung", "Leistung", "kW",
                                                                Array.Empty<Verwendung>(), ""), "42") };

    /// <summary>Vergleicht beide Geräte über die Katalogliste des Dialogs (der Weg des Knopfs „Vergleichen…").</summary>
    private static void Vergleichen(IRenderedComponent<Katalogliste> liste)
        => liste.InvokeAsync(() => liste.Instance.Vergleichen(new[] { ZWEITER, NAME }));

    /// <summary>Die Werte der einen Vergleichszeile (je Gerät eine Zelle).</summary>
    private static List<string> Vergleichswerte<T>(IRenderedComponent<T> cut) where T : IComponent
    {
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-vergleich tbody tr")));
        return cut.Find(".epos-vergleich tbody tr").QuerySelectorAll("td")
                  .Select(e => e.TextContent.Trim()).ToList();
    }

    /// <summary>
    /// Wirft die Abfrage eines Geräts: Das Gerät bleibt im Vergleich leer, das andere trägt seinen Wert,
    /// der Banner nennt Gerät und Grund, das Protokoll hat einen Vermerk, der Dialog steht weiter.
    /// </summary>
    private void VergleichWirft<T>(IRenderedComponent<T> cut, IRenderedComponent<Katalogliste> liste)
        where T : IComponent
    {
        Vergleichen(liste);
        Assert.Equal(new[] { "42", ParameterVerwendung.LEER }, Vergleichswerte(cut));

        string vorspann = R.WURZEL_SATZ_FEHLER.Split('{')[0];
        cut.WaitForAssertion(() => Assert.Contains(
            cut.FindAll(".epos-warnbanner--warnung .epos-warnbanner-text").Select(e => e.TextContent),
            t => t.StartsWith(vorspann, StringComparison.Ordinal) && t.Contains(NAME) && t.Contains(GRUND)));
        lock (_vermerke)
            Assert.Contains(_vermerke, v => v.StartsWith("Katalogsatz", StringComparison.Ordinal)
                                            && v.Contains(NAME) && v.Contains(GRUND));
        // Der Dialog ist offen: die Katalogliste führt ihre beiden Zeilen weiter.
        Assert.Equal(2, liste.Instance.Zeilen.Count);
        Assert.NotEmpty(cut.FindAll(".epos-vergleich"));
    }

    private void VergleichOhneAusnahme<T>(IRenderedComponent<T> cut, IRenderedComponent<Katalogliste> liste)
        where T : IComponent
    {
        Vergleichen(liste);
        Assert.Equal(new[] { "42", "42" }, Vergleichswerte(cut));
        OhneBanner(cut);
    }

    private IRenderedComponent<WaermepumpenDialog> WpVergleich(bool wirft)
        => Render<WaermepumpenDialog>(b => b
            .Add(x => x.Zeilen, new List<WaermepumpeAnlageDaten>())
            .Add(x => x.Katalog, Zwei)
            .Add(x => x.Katalogprofil, Profil(Anlagenart.Waermepumpe))
            .Add(x => x.Vergleichsparameter, Gabe(wirft)));

    [Fact]
    public void Waermepumpen_wirft_die_Vergleichsgabe_zeigt_der_Dialog_den_Grund()
    {
        var cut = WpVergleich(true);
        VergleichWirft(cut, cut.FindComponent<Katalogliste>());
    }

    [Fact]
    public void Waermepumpen_Vergleich_ohne_Ausnahme_zeigt_keinen_Banner()
    {
        var cut = WpVergleich(false);
        VergleichOhneAusnahme(cut, cut.FindComponent<Katalogliste>());
    }

    private IRenderedComponent<HeizkesselDialog> KesselVergleich(bool wirft)
        => Render<HeizkesselDialog>(b => b
            .Add(x => x.Katalogprofil, Profil(Anlagenart.Heizkessel))
            .Add(x => x.Katalogzeilen, Zwei)
            .Add(x => x.KatalogDetail, Detail)
            .Add(x => x.Vergleichsparameter, Gabe(wirft)));

    [Fact]
    public void Heizkessel_wirft_die_Vergleichsgabe_zeigt_der_Dialog_den_Grund()
    {
        var cut = KesselVergleich(true);
        VergleichWirft(cut, cut.FindComponent<Katalogliste>());
    }

    [Fact]
    public void Heizkessel_Vergleich_ohne_Ausnahme_zeigt_keinen_Banner()
    {
        var cut = KesselVergleich(false);
        VergleichOhneAusnahme(cut, cut.FindComponent<Katalogliste>());
    }

    private IRenderedComponent<BhkwDialog> BhkwVergleich(bool wirft)
        => Render<BhkwDialog>(b => b
            .Add(x => x.Katalogprofil, Profil(Anlagenart.Bhkw))
            .Add(x => x.Katalogzeilen, Zwei)
            .Add(x => x.KatalogDetail, Detail)
            .Add(x => x.Vergleichsparameter, Gabe(wirft)));

    [Fact]
    public void Bhkw_wirft_die_Vergleichsgabe_zeigt_der_Dialog_den_Grund()
    {
        var cut = BhkwVergleich(true);
        VergleichWirft(cut, cut.FindComponent<Katalogliste>());
    }

    [Fact]
    public void Bhkw_Vergleich_ohne_Ausnahme_zeigt_keinen_Banner()
    {
        var cut = BhkwVergleich(false);
        VergleichOhneAusnahme(cut, cut.FindComponent<Katalogliste>());
    }

    private IRenderedComponent<PhotovoltaikDialog> PvVergleich(bool wirft)
        => Render<PhotovoltaikDialog>(b => b
            .Add(x => x.Katalogprofil, Profil(Anlagenart.Photovoltaik))
            .Add(x => x.Katalogzeilen, Zwei)
            .Add(x => x.Detail, Detail)
            .Add(x => x.Vergleichsparameter, Gabe(wirft)));

    [Fact]
    public void Photovoltaik_wirft_die_Vergleichsgabe_zeigt_der_Dialog_den_Grund()
    {
        var cut = PvVergleich(true);
        VergleichWirft(cut, cut.FindComponent<Katalogliste>());
    }

    [Fact]
    public void Photovoltaik_Vergleich_ohne_Ausnahme_zeigt_keinen_Banner()
    {
        var cut = PvVergleich(false);
        VergleichOhneAusnahme(cut, cut.FindComponent<Katalogliste>());
    }
}
