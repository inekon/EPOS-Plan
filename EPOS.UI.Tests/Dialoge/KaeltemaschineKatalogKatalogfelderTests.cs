using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Katalogfelder der Kälteerzeuger im Katalogdialog</b> (Stufe K-A): Geräteart und Art der saisonalen Kennzahl als
/// Auswahl, GWP, Füllmenge und Wert als Zahlen in der Gruppe Kenndaten; Speichern schreibt die Persistenzwerte; ein
/// Auslieferungssatz zeigt sie als Text.
/// </summary>
public class KaeltemaschineKatalogKatalogfelderTests : EposBunitContext
{
    public KaeltemaschineKatalogKatalogfelderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<KaeltemaschineKatalogDialog> Aufbauen(KaeltemaschineKatalogDialogTests.Katalog k)
        => Render<KaeltemaschineKatalogDialog>(b => b
            .Add(x => x.Katalogzeilen, k.Zeilen)
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Rueckkuehlarten, KaeltemaschineKatalogHuelle.Rueckkuehlarten())
            .Add(x => x.Lies, id => k.Saetze.FirstOrDefault(s => s.Id == id)?.Kopie())
            .Add(x => x.Pruefen, KaeltemaschineKatalogHuelle.Pruefen)
            .Add(x => x.Speichern, k.Speichern)
            .Add(x => x.Duplizieren, (id, name) =>
            {
                KaeltemaschineDaten neu = k.Saetze.First(s => s.Id == id).Kopie();
                neu.Id = 0;
                neu.Bezeichner = name;
                neu.Auslieferung = false;
                return k.Speichern(neu);
            }));

    private static IElement Kenndaten(IRenderedComponent<KaeltemaschineKatalogDialog> cut)
        => cut.FindAll("section.epos-stammblattgruppe").First(s => s.GetAttribute("aria-label") == R.ADM_SB_KENNDATEN);

    private static IElement Auswahl(IElement gruppe, string platzhalter)
        => gruppe.QuerySelectorAll("select").First(s => s.TextContent.Contains(platzhalter, StringComparison.Ordinal));

    [Fact]
    public void Die_Kenndaten_fuehren_die_Felder_und_Speichern_schreibt_sie()
    {
        var k = new KaeltemaschineKatalogDialogTests.Katalog();
        var cut = Aufbauen(k);
        cut.FindAll(".epos-auswahlleiste button").First(b => b.TextContent.Trim().StartsWith(R.ADM_BTN_DUPLIZIEREN, StringComparison.Ordinal)).Click();
        cut.Find(".epos-ueberlagerung").QuerySelectorAll("button").First(b => b.TextContent.Trim() == R.ALLG_BTN_OK).Click();
        Assert.False(cut.Instance.Auslieferung);

        IElement g = Kenndaten(cut);
        IElement art = Auswahl(g, R.KM_GERAETEART_SPLIT);
        Assert.Contains(R.KM_GERAETEART_AUS_RUECKKUEHLUNG, art.TextContent);
        foreach (string text in new[] { R.KM_GERAETEART_KWS_LUFT, R.KM_GERAETEART_SPLIT, R.KM_GERAETEART_MULTISPLIT, R.KM_GERAETEART_VRF })
            Assert.Contains(text, art.TextContent);
        Assert.Contains(R.KM_SAISON_SEER, Auswahl(g, R.KM_SAISON_ETA_S_C).TextContent);
        Assert.Contains(R.KM_LBL_GWP, g.TextContent);
        Assert.Contains(R.KM_LBL_FUELLMENGE, g.TextContent);

        // Platz 3 = SPLIT (KaelteKatalogfelderSchema.GERAETEARTEN), Platz 0 = SEER.
        Auswahl(Kenndaten(cut), R.KM_GERAETEART_SPLIT).Change("3");
        Auswahl(Kenndaten(cut), R.KM_SAISON_ETA_S_C).Change("0");
        Assert.Equal(3, cut.Instance.Arbeitsstand.GeraeteartIndex);
        cut.Instance.Arbeitsstand.Saisonkennzahl = 6.5;
        cut.Instance.Arbeitsstand.Gwp = 675;
        cut.Instance.Arbeitsstand.Fuellmenge = 1.5;
        Assert.True(cut.Instance.Geaendert);

        cut.FindAll("button").First(b => b.TextContent.Trim() == R.ADM_BTN_SPEICHERN).Click();
        KaeltemaschineModel m = k.Modelle.Last();
        Assert.Equal(KaelteKatalogfelderSchema.GERAETEART_SPLIT, m.Geraeteart);
        Assert.Equal(KaelteKatalogfelderSchema.SAISON_SEER, m.Saisonkennzahl_Art);
        Assert.Equal(6.5, m.Saisonkennzahl);
        Assert.Equal(675, m.Kaeltemittel_GWP);
        Assert.Equal(1.5, m.Kaeltemittel_Fuellmenge_kg);
    }

    [Fact]
    public void Ein_Auslieferungssatz_zeigt_die_Felder_als_Text()
    {
        var cut = Aufbauen(new KaeltemaschineKatalogDialogTests.Katalog());
        Assert.True(cut.Instance.Auslieferung);
        IElement g = Kenndaten(cut);
        Assert.Empty(g.QuerySelectorAll("select"));
        Assert.Contains(R.KM_LBL_GERAETEART, g.TextContent);
        Assert.Contains(R.KM_LBL_SAISON_ART, g.TextContent);
        Assert.Contains(R.KM_LBL_GWP, g.TextContent);
    }

    /// <summary>Die Hülle bildet Persistenzwert und Listenplatz verlustfrei aufeinander ab.</summary>
    [Fact]
    public void Die_Huelle_bildet_die_Felder_rund()
    {
        var m = new KaeltemaschineModel
        {
            Bezeichner = "H", Rueckkuehlart = KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER,
            Geraeteart = KaelteKatalogfelderSchema.GERAETEART_VRF, Kaeltemittel_GWP = 2088, Kaeltemittel_Fuellmenge_kg = 12,
            Saisonkennzahl_Art = KaelteKatalogfelderSchema.SAISON_ETA_S_C, Saisonkennzahl = 260
        };
        KaeltemaschineDaten d = KaeltemaschineKatalogHuelle.AlsDaten(m);
        Assert.Equal(5, d.GeraeteartIndex);
        Assert.Equal(1, d.SaisonArtIndex);
        KaeltemaschineModel z = KaeltemaschineKatalogHuelle.AlsModell(d);
        Assert.Equal("VRF", z.Geraeteart);
        Assert.Equal(2088, z.Kaeltemittel_GWP);
        Assert.Equal(12, z.Kaeltemittel_Fuellmenge_kg);
        Assert.Equal("ETA_S_C", z.Saisonkennzahl_Art);
        Assert.Equal(260, z.Saisonkennzahl);
        Assert.Equal(0, d.Abweichungen(d.Kopie()));
        KaeltemaschineDaten anders = d.Kopie();
        anders.GeraeteartIndex = 0; anders.Gwp = 1;
        Assert.Equal(2, d.Abweichungen(anders));
    }
}
