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
/// <b>KT-3: Die Katalogdialoge bleiben bei einer werfenden Liste nicht still.</b> Wirft das
/// Lesen der Katalogliste, zeichnet der Dialog trotzdem, sein Warnbanner nennt den Grund
/// (<c>WURZEL_LISTE_FEHLER</c> mit der Meldung der Ausnahme), das
/// <see cref="Ausnahmeprotokoll"/> bekommt einen Vermerk, und Schließen bleibt erreichbar.
/// Die Fangstelle ist <see cref="Katalogladung"/>; je Dialog ein Fall, dazu die Gegenfälle.
/// </summary>
public class KatalogListeWirftTests : EposBunitContext
{
    private const string GRUND = "Katalogtabelle gesperrt";

    private readonly List<string> _vermerke = new();

    public KatalogListeWirftTests()
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

    private static IReadOnlyList<Katalogfilterzeile> Wirft() => throw new InvalidOperationException(GRUND);

    private static IReadOnlyList<Katalogfilterzeile> Eine() => new[] { new Katalogfilterzeile(1, "Gerät A") };

    /// <summary>Banner mit Grund sichtbar, Vermerk geschrieben.</summary>
    private void BannerMitGrund<T>(IRenderedComponent<T> cut) where T : IComponent
    {
        string vorspann = R.WURZEL_LISTE_FEHLER.Split('{')[0];
        List<string> texte = cut.FindAll(".epos-warnbanner--warnung .epos-warnbanner-text")
                                .Select(e => e.TextContent).ToList();
        Assert.Contains(texte, t => t.StartsWith(vorspann, StringComparison.Ordinal) && t.Contains(GRUND));
        lock (_vermerke)
            Assert.Contains(_vermerke, v => v.StartsWith("Katalogliste", StringComparison.Ordinal) && v.Contains(GRUND));
    }

    /// <summary>Kein Banner mit dem Listengrund, kein Vermerk.</summary>
    private void OhneBanner<T>(IRenderedComponent<T> cut) where T : IComponent
    {
        string vorspann = R.WURZEL_LISTE_FEHLER.Split('{')[0];
        Assert.DoesNotContain(cut.FindAll(".epos-warnbanner-text"),
                              e => e.TextContent.StartsWith(vorspann, StringComparison.Ordinal));
        lock (_vermerke) Assert.Empty(_vermerke);
    }

    // =================================================================================
    // Kältemaschine — zuerst, mit Schließen; dazu die Fangstelle selbst
    // =================================================================================

    [Fact]
    public void Kaeltemaschine_wirft_die_Liste_zeigt_der_Dialog_den_Grund_und_bleibt_bedienbar()
    {
        bool? geschlossen = null;
        var cut = Render<KaeltemaschineKatalogDialog>(b => b
            .Add(x => x.Katalogzeilen, Wirft)
            .Add(x => x.Geschlossen, (bool e) => geschlossen = e));

        Assert.Equal(R.KM_TITEL, cut.Find(".epos-dialog-titel").TextContent);
        BannerMitGrund(cut);

        cut.Find(".epos-dialog-zu").Click();
        Assert.NotNull(geschlossen);
    }

    [Fact]
    public void Kaeltemaschine_ohne_Ausnahme_zeigt_keinen_Listenbanner()
        => OhneBanner(Render<KaeltemaschineKatalogDialog>(b => b.Add(x => x.Katalogzeilen, Eine)));

    [Fact]
    public void Katalogladung_leert_den_Grund_beim_naechsten_gelungenen_Lesen()
    {
        IReadOnlyList<Katalogfilterzeile> erste = Katalogladung.Lesen(Wirft, "Liste", out string fehler);
        Assert.Empty(erste);
        Assert.Contains(GRUND, fehler);
        Assert.Contains("Liste", fehler);

        IReadOnlyList<Katalogfilterzeile> zweite = Katalogladung.Lesen(Eine, "Liste", out fehler);
        Assert.Single(zweite);
        Assert.Equal("", fehler);

        Assert.Empty(Katalogladung.Lesen<Katalogfilterzeile>(null, "Liste", out fehler));
        Assert.Equal("", fehler);
    }

    // =================================================================================
    // Die gleich gebauten Katalogdialoge (dieselbe Ladeform über Katalogladung)
    // =================================================================================

    [Fact]
    public void Heizkessel_wirft_die_Liste_zeigt_der_Dialog_den_Grund_und_schliesst()
    {
        bool? geschlossen = null;
        var cut = Render<HeizkesselDialog>(b => b
            .Add(x => x.Katalogzeilen, Wirft)
            .Add(x => x.Geschlossen, (bool e) => geschlossen = e));
        BannerMitGrund(cut);
        cut.Find(".epos-dialog-zu").Click();
        Assert.NotNull(geschlossen);
    }

    [Fact]
    public void Heizkessel_ohne_Ausnahme_zeigt_keinen_Listenbanner()
        => OhneBanner(Render<HeizkesselDialog>(b => b.Add(x => x.Katalogzeilen, Eine)));

    [Fact]
    public void Waermepumpe_wirft_die_Liste_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Render<WaermepumpeStammDialog>(b => b.Add(x => x.Liste, Wirft)));

    [Fact]
    public void Waermepumpe_ohne_Ausnahme_zeigt_keinen_Listenbanner()
        => OhneBanner(Render<WaermepumpeStammDialog>(b => b.Add(x => x.Liste, Eine)));

    [Fact]
    public void Kaeltemaschinenanlage_wirft_die_Liste_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Render<KaeltemaschineAnlageDialog>(b => b.Add(x => x.Katalogzeilen, Wirft)));

    [Fact]
    public void Bhkw_wirft_die_Liste_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Render<BhkwDialog>(b => b.Add(x => x.Katalogzeilen, Wirft)));

    [Fact]
    public void Pufferspeicher_wirft_die_Liste_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Render<PufferspeicherDialog>(b => b.Add(x => x.Katalogzeilen, Wirft)));

    [Fact]
    public void Stromspeicher_wirft_die_Liste_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Render<StromspeicherDialog>(b => b.Add(x => x.Katalogzeilen, Wirft)));

    [Fact]
    public void Photovoltaik_wirft_die_Liste_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Render<PhotovoltaikDialog>(b => b.Add(x => x.Katalogzeilen, Wirft)));

    [Fact]
    public void WaermebedarfExtern_wirft_die_Liste_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Render<WaermebedarfExternDialog>(b => b
            .Add(x => x.Katalogzeilen, () => Task.FromResult(Wirft()))));

    [Fact]
    public void WaermebedarfExtern_wirft_die_Aufgabe_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Render<WaermebedarfExternDialog>(b => b
            .Add(x => x.Katalogzeilen, () => Task.FromException<IReadOnlyList<Katalogfilterzeile>>(
                new InvalidOperationException(GRUND)))));

    [Fact]
    public void Stromganglinie_wirft_die_Liste_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Render<EPOS.UI.Dialoge.Strom.StromganglinieDialog>(b => b
            .Add(x => x.Katalogzeilen, () => Task.FromResult(Wirft()))));

    [Fact]
    public void Stromganglinie_ohne_Ausnahme_zeigt_keinen_Listenbanner()
        => OhneBanner(Render<EPOS.UI.Dialoge.Strom.StromganglinieDialog>(b => b
            .Add(x => x.Katalogzeilen, () => Task.FromResult(Eine()))));

    [Fact]
    public void WaermebedarfExtern_ohne_Ausnahme_zeigt_keinen_Listenbanner()
        => OhneBanner(Render<WaermebedarfExternDialog>(b => b
            .Add(x => x.Katalogzeilen, () => Task.FromResult(Eine()))));

    [Fact]
    public void Solarkollektoren_wirft_die_Liste_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Render<SolarkollektorenDialog>(b => b.Add(x => x.Katalogzeilen, Wirft)));

    [Fact]
    public void Solarganglinie_wirft_die_Liste_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Render<SolarganglinieDialog>(b => b
            .Add(x => x.Katalogwege, new GanglinienKatalogwege { Katalogzeilen = () => Task.FromResult(Wirft()) })));

    [Fact]
    public void PvGanglinie_wirft_die_Liste_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Render<PvGanglinieDialog>(b => b
            .Add(x => x.Katalogwege, new GanglinienKatalogwege { Katalogzeilen = () => Task.FromResult(Wirft()) })));

    [Fact]
    public void Solarganglinie_im_Katalogbetrieb_wirft_die_Liste_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Render<SolarganglinieDialog>(b => b
            .Add(x => x.Katalogbetrieb, true)
            .Add(x => x.Katalogwege, new GanglinienKatalogwege { Katalogzeilen = () => Task.FromResult(Wirft()) })));

    [Fact]
    public void KatalogBrowser_wirft_die_Liste_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Render<KatalogBrowserDialog>(b => b
            .Add(x => x.Art, KatalogBrowserArt.Heizkessel)
            .Add(x => x.ProfilVorgabe, KatalogBrowserProfil.Finde(KatalogBrowserArt.Heizkessel,
                     s => R.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Wege, new KatalogBrowserWege { Katalogzeilen = Wirft })));

    [Fact]
    public void ModulKatalog_wirft_die_Liste_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Render<ModulKatalogDialog>(b => b
            .Add(x => x.Art, ModulKatalogArt.Stromspeicher)
            .Add(x => x.ProfilVorgabe, ModulKatalogProfil.Finde(ModulKatalogArt.Stromspeicher,
                     s => R.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Wege, new ModulKatalogWege { Katalogzeilen = Wirft })));

    // =================================================================================
    // KT-4: die vier Dialoge mit eigener Ladeform (nur die Ladezeile über Katalogladung)
    // =================================================================================

    [Fact]
    public void GebaeudeAdmin_wirft_die_Liste_zeigt_der_Dialog_den_Grund_und_schliesst()
    {
        bool? geschlossen = null;
        var cut = Render<GebaeudeAdminDialog>(b => b
            .Add(x => x.Katalogzeilen, Wirft)
            .Add(x => x.Geschlossen, (bool e) => geschlossen = e));
        BannerMitGrund(cut);
        cut.Find(".epos-dialog-zu").Click();
        Assert.NotNull(geschlossen);
    }

    [Fact]
    public void GebaeudeAdmin_ohne_Ausnahme_zeigt_keinen_Listenbanner()
        => OhneBanner(Render<GebaeudeAdminDialog>(b => b.Add(x => x.Katalogzeilen, Eine)));

    [Fact]
    public void Gebaeude_wirft_die_Liste_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Render<GebaeudeDialog>(b => b
            .Add(x => x.Zeilen, new List<GebaeudeProjektZeile>())
            .Add(x => x.Katalogzeilen, Wirft)));

    [Fact]
    public void Gebaeude_ohne_Ausnahme_zeigt_keinen_Listenbanner()
        => OhneBanner(Render<GebaeudeDialog>(b => b
            .Add(x => x.Zeilen, new List<GebaeudeProjektZeile>())
            .Add(x => x.Katalogzeilen, Eine)));

    [Fact]
    public void BedarfsProfile_wirft_die_Liste_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Render<BedarfsProfileDialog>(b => b
            .Add(x => x.Zeilen, new List<BedarfsProfilZeile>())
            .Add(x => x.Katalogzeilen, Wirft)));

    [Fact]
    public void BedarfsProfile_ohne_Ausnahme_zeigt_keinen_Listenbanner()
        => OhneBanner(Render<BedarfsProfileDialog>(b => b
            .Add(x => x.Zeilen, new List<BedarfsProfilZeile>())
            .Add(x => x.Katalogzeilen, Eine)));

    [Fact]
    public void TwwNutzungsart_wirft_die_Liste_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Render<TwwNutzungsartAdminDialog>(b => b.Add(x => x.Katalogzeilen, Wirft)));

    [Fact]
    public void TwwNutzungsart_ohne_Ausnahme_zeigt_keinen_Listenbanner()
        => OhneBanner(Render<TwwNutzungsartAdminDialog>(b => b.Add(x => x.Katalogzeilen, Eine)));

    // =================================================================================
    // KT-6: PV-Ganglinie und Solarganglinie — die Liste der Katalogseite, ihre Verwendung und das
    // Neuladen nach dem Import
    // =================================================================================

    private static Task<IReadOnlyList<Katalogfilterzeile>> Ganglinien()
        => Task.FromResult<IReadOnlyList<Katalogfilterzeile>>(new[] { Zeitreihenproben.Zeile(31, "Gerät A") });

    private IRenderedComponent<PvGanglinieDialog> PvGanglinie(GanglinienKatalogwege wege)
        => Render<PvGanglinieDialog>(b => b
            .Add(x => x.Zeilen, new List<ErzeugerZeile>())
            .Add(x => x.Katalogwege, wege)
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.PvGanglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand()));

    private IRenderedComponent<SolarganglinieDialog> Solarganglinie(GanglinienKatalogwege wege)
        => Render<SolarganglinieDialog>(b => b
            .Add(x => x.Zeilen, new List<ErzeugerZeile>())
            .Add(x => x.Katalogwege, wege)
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.Solarganglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand()));

    [Fact]
    public void PvGanglinie_wirft_die_Liste_zeigt_der_Dialog_den_Grund_und_bleibt_bedienbar()
    {
        bool? geschlossen = null;
        var cut = Render<PvGanglinieDialog>(b => b
            .Add(x => x.Zeilen, new List<ErzeugerZeile>())
            .Add(x => x.Katalogwege, new GanglinienKatalogwege { Katalogzeilen = () => Task.FromResult(Wirft()) })
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.PvGanglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Geschlossen, v => geschlossen = v));
        BannerMitGrund(cut);
        Assert.Contains("Photovoltaik Ganglinie", cut.Instance.Katalogseite!.Ladefehler);
        Assert.Empty(cut.Instance.Katalog);
        cut.FindAll("button").First(k => k.TextContent.Trim() == "Abbrechen").Click();
        Assert.False(geschlossen);
    }

    [Fact]
    public void PvGanglinie_wirft_die_Verwendung_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(PvGanglinie(new GanglinienKatalogwege
        {
            Katalogzeilen = Ganglinien,
            Verwendung = () => throw new InvalidOperationException(GRUND)
        }));

    [Fact]
    public void PvGanglinie_wirft_das_Neuladen_nach_dem_Import_zeigt_der_Dialog_den_Grund()
    {
        int gelesen = 0;
        var cut = PvGanglinie(new GanglinienKatalogwege
        {
            Katalogzeilen = () => ++gelesen == 1 ? Ganglinien() : Task.FromResult(Wirft()),
            DateiWaehlen = _ => Task.FromResult<string?>("D:/Daten/Ganglinie.csv"),
            Einlesen = (_, _) => Task.FromResult(new GanglinienKatalogimport(true, false, "Gerät B", "", ""))
        });
        OhneBanner(cut);
        cut.Find("button.epos-importknopf").Click();
        cut.FindAll("button").First(k => k.TextContent.Trim() == R.SGAD_BTN_DATEI).Click();
        cut.FindAll("button").First(k => k.TextContent.Trim() == R.SGAD_BTN_EINLESEN).Click();
        BannerMitGrund(cut);
        Assert.False(cut.Instance.Katalogseite!.ImportOffen);
    }

    [Fact]
    public void PvGanglinie_ohne_Ausnahme_zeigt_keinen_Listenbanner()
        => OhneBanner(PvGanglinie(new GanglinienKatalogwege
        {
            Katalogzeilen = Ganglinien,
            Verwendung = () => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>(
                new Dictionary<string, IReadOnlyList<string>>())
        }));

    [Fact]
    public void Solarganglinie_ohne_Ausnahme_zeigt_keinen_Listenbanner()
        => OhneBanner(Solarganglinie(new GanglinienKatalogwege { Katalogzeilen = Ganglinien }));

    // =================================================================================
    // KT-7: Bedarfsverwaltung, Gebäudetypen, Baustoffe, Bauteilaufbauten, Gebäudekatalog
    // =================================================================================

    private static IReadOnlyList<string> WirftNamen() => throw new InvalidOperationException(GRUND);

    private IRenderedComponent<BedarfAdminDialog> BedarfAdmin(Func<IReadOnlyList<Katalogfilterzeile>> zeilen,
                                                              Func<IReadOnlyList<string>>? typen = null)
        => Render<BedarfAdminDialog>(b => b
            .Add(x => x.Art, BedarfsArt.Stromverbraucher)
            .Add(x => x.TitelText, "Stromverbraucher")
            .Add(x => x.Katalogzeilen, zeilen)
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerBedarf(BedarfsArt.Stromverbraucher, s => s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Typen, typen));

    [Fact]
    public void BedarfAdmin_wirft_die_Liste_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(BedarfAdmin(Wirft));

    [Fact]
    public void BedarfAdmin_wirft_die_Typliste_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(BedarfAdmin(Eine, WirftNamen));

    [Fact]
    public void BedarfAdmin_ohne_Ausnahme_zeigt_keinen_Listenbanner()
        => OhneBanner(BedarfAdmin(Eine, () => new[] { "Typ" }));

    private IRenderedComponent<GebaeudetypDialog> Gebaeudetyp(Func<IReadOnlyList<Katalogfilterzeile>> zeilen,
                                                              Func<IReadOnlyDictionary<string, int>>? verwendung = null)
        => Render<GebaeudetypDialog>(b => b
            .Add(x => x.Katalogzeilen, zeilen)
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerGebaeudetyp(s => R.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Verwendung, verwendung));

    [Fact]
    public void Gebaeudetyp_wirft_die_Liste_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Gebaeudetyp(Wirft));

    [Fact]
    public void Gebaeudetyp_wirft_die_Verwendung_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Gebaeudetyp(Eine, () => throw new InvalidOperationException(GRUND)));

    [Fact]
    public void Gebaeudetyp_wirft_die_Namensliste_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Render<GebaeudetypDialog>(b => b.Add(x => x.Typen, WirftNamen)));

    [Fact]
    public void Gebaeudetyp_ohne_Ausnahme_zeigt_keinen_Listenbanner()
        => OhneBanner(Gebaeudetyp(Eine, () => new Dictionary<string, int>()));

    private IRenderedComponent<BaustoffKatalogDialog> Baustoff(Func<IReadOnlyList<Katalogfilterzeile>> zeilen,
                                                               Func<IReadOnlyDictionary<int, int>>? verwendung = null)
        => Render<BaustoffKatalogDialog>(b => b
            .Add(x => x.Katalogzeilen, zeilen)
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerBaustoff(s => R.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Verwendung, verwendung));

    [Fact]
    public void Baustoff_wirft_die_Liste_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Baustoff(Wirft));

    [Fact]
    public void Baustoff_wirft_die_Verwendung_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Baustoff(Eine, () => throw new InvalidOperationException(GRUND)));

    [Fact]
    public void Baustoff_ohne_Ausnahme_zeigt_keinen_Listenbanner()
        => OhneBanner(Baustoff(Eine, () => new Dictionary<int, int>()));

    private IRenderedComponent<BauteilaufbauDialog> Bauteilaufbau(Func<IReadOnlyList<Katalogfilterzeile>> zeilen,
                                                                  Func<IReadOnlyList<BaustoffWahl>>? baustoffe = null)
        => Render<BauteilaufbauDialog>(b => b
            .Add(x => x.Katalogzeilen, zeilen)
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerBauteilaufbau(s => R.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Baustoffe, baustoffe));

    [Fact]
    public void Bauteilaufbau_wirft_die_Liste_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Bauteilaufbau(Wirft));

    [Fact]
    public void Bauteilaufbau_wirft_die_Baustoffwahl_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Bauteilaufbau(Eine, () => throw new InvalidOperationException(GRUND)));

    [Fact]
    public void Bauteilaufbau_ohne_Ausnahme_zeigt_keinen_Listenbanner()
        => OhneBanner(Bauteilaufbau(Eine, () => Array.Empty<BaustoffWahl>()));

    [Fact]
    public void GebaeudeKatalog_wirft_die_Typliste_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Render<GebaeudeKatalogDialog>(b => b
            .Add(x => x.Gebaeudetypen, WirftNamen)
            .Add(x => x.Gebaeudearten, () => new[] { "Wohnen" })));

    [Fact]
    public void GebaeudeKatalog_wirft_die_Artenliste_zeigt_der_Dialog_den_Grund()
        => BannerMitGrund(Render<GebaeudeKatalogDialog>(b => b
            .Add(x => x.Gebaeudetypen, () => new[] { "EFH" })
            .Add(x => x.Gebaeudearten, WirftNamen)));

    [Fact]
    public void GebaeudeKatalog_ohne_Ausnahme_zeigt_keinen_Listenbanner()
        => OhneBanner(Render<GebaeudeKatalogDialog>(b => b
            .Add(x => x.Gebaeudetypen, () => new[] { "EFH" })
            .Add(x => x.Gebaeudearten, () => new[] { "Wohnen" })));
}
