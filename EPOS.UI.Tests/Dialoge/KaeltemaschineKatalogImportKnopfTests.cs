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
/// <b>KM1 — „Import…" und „Typkennfelder laden…" in der Verwaltung „Kältemaschinen"</b>: Ohne Importweg
/// (iOS) lehnt „Import…" benannt ab; mit Weg steht der Katalogimport als Überlagerung. „Typkennfelder
/// laden…" fragt nach, ruft den Weg der Hülle erst nach „Ja" und nennt neu und übersprungen.
/// </summary>
public class KaeltemaschineKatalogImportKnopfTests : EposBunitContext
{
    public KaeltemaschineKatalogImportKnopfTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private int _geladen;

    private IRenderedComponent<KaeltemaschineKatalogDialog> Aufbauen(
        Func<IReadOnlyDictionary<string, object>?>? import, bool typkennfelder = true)
    {
        var k = new KaeltemaschineKatalogDialogTests.Katalog();
        return Render<KaeltemaschineKatalogDialog>(b =>
        {
            b.Add(x => x.Katalogzeilen, k.Zeilen)
             .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
             .Add(x => x.Rueckkuehlarten, KaeltemaschineKatalogHuelle.Rueckkuehlarten())
             .Add(x => x.Lies, id => k.Saetze.First(s => s.Id == id).Kopie())
             .Add(x => x.Pruefen, KaeltemaschineKatalogHuelle.Pruefen)
             .Add(x => x.Speichern, k.Speichern)
             .Add(x => x.ImportGaben, import)
             .Add(x => x.TypkennfelderAnzahl, 34);
            if (typkennfelder)
                b.Add(x => x.TypkennfelderLaden, () =>
                {
                    _geladen++;
                    return new KaeltemaschineTypkennfelderErgebnis(true, 30, 4, "");
                });
        });
    }

    private static IElement Knopf(IRenderedComponent<KaeltemaschineKatalogDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    [Fact]
    public void Ohne_Importweg_lehnt_der_Knopf_benannt_ab()
    {
        var cut = Aufbauen(null);

        Knopf(cut, R.ADM_BTN_IMPORT).Click();

        Assert.False(cut.Instance.ImportOffen);
        Assert.Equal(R.KM_MSG_IMPORT_PLATTFORM, cut.Instance.Meldung);
    }

    [Fact]
    public void Mit_Importweg_steht_der_Katalogimport_der_Kaeltemaschine()
    {
        int gerufen = 0;
        var cut = Aufbauen(() =>
        {
            gerufen++;
            return new Dictionary<string, object>
            {
                ["Art"] = KatalogImportArt.Kaeltemaschine,
                ["ProfilVorgabe"] = KatalogImportProfil.Finde(KatalogImportArt.Kaeltemaschine, s => R.ResourceManager.GetString(s) ?? s)
            };
        });

        Knopf(cut, R.ADM_BTN_IMPORT).Click();

        Assert.Equal(1, gerufen);
        Assert.True(cut.Instance.ImportOffen);
        Assert.Contains(R.IMP_KAT_TITEL_KAELTEMASCHINE, string.Concat(cut.Nodes.Select(n => n.TextContent)));
    }

    [Fact]
    public void Typkennfelder_laden_fragt_nach_und_meldet_neu_und_uebersprungen()
    {
        var cut = Aufbauen(null);

        Knopf(cut, R.KM_BTN_TYPKENNFELDER).Click();
        Assert.True(cut.Instance.Typfrage);
        Assert.Equal(0, _geladen);
        Assert.Contains(string.Format(System.Globalization.CultureInfo.CurrentCulture, R.KM_FRAGE_TYPKENNFELDER, 34), string.Concat(cut.Nodes.Select(n => n.TextContent)));

        Knopf(cut, R.ALLG_BTN_NEIN).Click();
        Assert.False(cut.Instance.Typfrage);
        Assert.Equal(0, _geladen);

        Knopf(cut, R.KM_BTN_TYPKENNFELDER).Click();
        Knopf(cut, R.ALLG_BTN_JA).Click();
        Assert.Equal(1, _geladen);
        Assert.Equal(string.Format(System.Globalization.CultureInfo.CurrentCulture, R.KM_MSG_TYPKENNFELDER, 30, 4), cut.Instance.Status);
    }

    [Fact]
    public void Ohne_Ladeweg_fehlt_der_Knopf_Typkennfelder()
    {
        var cut = Aufbauen(null, typkennfelder: false);

        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == R.KM_BTN_TYPKENNFELDER);
        Assert.Contains(cut.FindAll("button"), b => b.TextContent.Trim() == R.ADM_BTN_IMPORT);
    }
}
