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
/// <b>Die Gruppe „Teillast und Takten" der Verwaltung „Kältemaschinen"</b> (KM3-E3-a, Fachkonzept Teillast und Takten 7.1):
/// Felder und Platzhalter, Lesemodus des Auslieferungssatzes, Lesezeile, die Schnellwahlen „Kurve aus Typkennfeld…" und
/// „Typkennfeld auf Datenblatt skalieren…", die Auskunft „Teillastpunkte prüfen…" und die benannte Ablehnung — gegen den
/// Stub der Verwaltungstests mit den Wegen der Hülle (Kern ohne Datenbank). Kultur de-DE.
/// </summary>
public class KaeltemaschineKatalogTeillastTests : EposBunitContext
{
    public KaeltemaschineKatalogTeillastTests()
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
            })
            .Add(x => x.Typkennfeldnamen, KaeltemaschineTeillastDialogrechnung.Typkennfeldnamen())
            .Add(x => x.KurveAusTypkennfeld, KaeltemaschineKatalogHuelle.KurveAusTypkennfeld)
            .Add(x => x.Skalieren, KaeltemaschineKatalogHuelle.Skalieren)
            .Add(x => x.Auskunft, KaeltemaschineKatalogHuelle.Auskunft)
            .Add(x => x.Lesezeile, KaeltemaschineKatalogHuelle.Lesezeile)
            .Add(x => x.NennEerHinweis, d => KaeltemaschineStammCtrl.NennEerHinweis(KaeltemaschineKatalogHuelle.AlsModell(d)))
            .Add(x => x.Teillastbild, d => KaeltemaschineTeillastbild.Modell(KaeltemaschineKatalogHuelle.AlsModell(d))));

    /// <summary>Dupliziert den ersten Saatsatz: die Kopie ist gewählt und bearbeitbar.</summary>
    private IRenderedComponent<KaeltemaschineKatalogDialog> MitKopie(KaeltemaschineKatalogDialogTests.Katalog k)
    {
        var cut = Aufbauen(k);
        cut.FindAll(".epos-auswahlleiste button").First(b => b.TextContent.Trim().StartsWith(R.ADM_BTN_DUPLIZIEREN, StringComparison.Ordinal)).Click();
        cut.Find(".epos-ueberlagerung").QuerySelectorAll("button").First(b => b.TextContent.Trim() == R.ALLG_BTN_OK).Click();
        Assert.False(cut.Instance.Auslieferung);
        return cut;
    }

    private static IElement Gruppe(IRenderedComponent<KaeltemaschineKatalogDialog> cut)
        => cut.FindAll("section.epos-stammblattgruppe").First(s => s.GetAttribute("aria-label") == R.BHKWK_GRP_TEILLAST);

    private static IElement Knopf(IRenderedComponent<KaeltemaschineKatalogDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static int TypMitKurve()
    {
        IReadOnlyList<string> namen = KaeltemaschineTeillastDialogrechnung.Typkennfeldnamen();
        for (int i = 0; i < namen.Count; i++)
            if (KaeltemaschineTeillastDialogrechnung.KurveAusTypkennfeld(namen[i])?.TeillastWeg == KaeltemaschineTeillastSchema.WEG_KURVE)
                return i;
        throw new InvalidOperationException("kein Typkennfeld mit Kurve");
    }

    [Fact]
    public void Die_Gruppe_steht_zwischen_Kenndaten_und_Kennlinie_mit_acht_Feldern_und_Platzhaltern()
    {
        var cut = MitKopie(new KaeltemaschineKatalogDialogTests.Katalog());

        Assert.Equal(new[] { R.ADM_SB_KENNDATEN, R.BHKWK_GRP_TEILLAST, R.KM_GRUPPE_KENNLINIE },
                     cut.FindAll(".epos-stammblattgruppe-titel").Select(e => e.TextContent).ToArray());
        IElement g = Gruppe(cut);
        Assert.Equal(3, g.QuerySelectorAll("select").Length);
        IReadOnlyList<IElement> zahlen = g.QuerySelectorAll("input").ToList();
        Assert.Equal(5, zahlen.Count);
        Assert.Equal(new[] { "Vorgabe", "Vorgabe", "Vorgabe", "Vorgabe", "0,9 (Vorgabe)" },
                     zahlen.Select(i => i.GetAttribute("placeholder")).ToArray());
        Assert.Contains(R.KM_TEILLAST_WEG_BESTAND, g.QuerySelectorAll("select")[0].TextContent);
        Assert.Contains(R.KM_PH_RANDWEG_VORGABE, g.QuerySelectorAll("select")[2].TextContent);
        // Ohne Teillastrechnung: g = 1 und der Hinweis auf den Bestandsweg.
        Assert.Equal("g(0,25) = 1,00 · g(0,5) = 1,00 · g(0,75) = 1,00", g.QuerySelector(".epos-kaeltemaschine-lesezeile")!.TextContent);
        Assert.Equal(R.KM_TT_HINWEIS_BESTAND, g.QuerySelector(".epos-kaeltemaschine-teillasthinweis")!.TextContent);
        Assert.Contains(R.KM_TT_VERWEIS_MINDESTTEILLAST, g.TextContent);
        Assert.Contains(R.KM_BTN_KURVE_TYPKENNFELD, g.TextContent);
    }

    [Fact]
    public void Ein_Auslieferungssatz_zeigt_die_Gruppe_als_Text_mit_Lesezeile_und_Auskunft()
    {
        var cut = Aufbauen(new KaeltemaschineKatalogDialogTests.Katalog());
        Assert.True(cut.Instance.Auslieferung);

        IElement g = Gruppe(cut);
        Assert.Empty(g.QuerySelectorAll("input"));
        Assert.Empty(g.QuerySelectorAll("select"));
        Assert.Contains(R.KM_TEILLAST_WEG_BESTAND, g.TextContent);
        Assert.Contains("0,9 (Vorgabe)", g.TextContent);
        Assert.NotNull(g.QuerySelector(".epos-kaeltemaschine-lesezeile"));
        Assert.DoesNotContain(R.KM_BTN_KURVE_TYPKENNFELD, g.TextContent);
        Assert.Contains(R.KM_BTN_TEILLASTPUNKTE, g.TextContent);
    }

    [Fact]
    public void Kurve_aus_Typkennfeld_uebernimmt_die_Werte_und_Speichern_schreibt_sie()
    {
        var k = new KaeltemaschineKatalogDialogTests.Katalog();
        var cut = MitKopie(k);
        int platz = TypMitKurve();
        string name = KaeltemaschineTeillastDialogrechnung.Typkennfeldnamen()[platz];
        KaeltemaschineTeillastDialogrechnung.Typkurve erwartet = KaeltemaschineTeillastDialogrechnung.KurveAusTypkennfeld(name);

        Knopf(cut, R.KM_BTN_KURVE_TYPKENNFELD).Click();
        Assert.True(cut.Instance.TypwahlOffen);
        IElement wahl = cut.Find(".epos-kaeltemaschine-typwahl");
        wahl.QuerySelector("select")!.Change(platz.ToString(System.Globalization.CultureInfo.InvariantCulture));
        cut.Find(".epos-kaeltemaschine-typwahl").QuerySelectorAll("button").First(b => b.TextContent.Trim() == R.ALLG_BTN_OK).Click();

        Assert.False(cut.Instance.TypwahlOffen);
        KaeltemaschineDaten a = cut.Instance.Arbeitsstand;
        Assert.Equal(1, a.TeillastWegIndex);
        Assert.Equal(erwartet.A, a.KurveA);
        Assert.Equal(erwartet.C, a.KurveC);
        Assert.Equal(erwartet.LastgradMin, a.KurveLastgradMin);
        Assert.True(cut.Instance.Geaendert);
        Assert.Equal("", Gruppe(cut).QuerySelector(".epos-kaeltemaschine-teillasthinweis")?.TextContent ?? "");

        Knopf(cut, R.ADM_BTN_SPEICHERN).Click();
        KaeltemaschineModel m = k.Modelle.Last();
        Assert.Equal(KaeltemaschineTeillastSchema.WEG_KURVE, m.Teillast_Weg);
        Assert.Equal(erwartet.B, m.Teillastkurve_b);
        Assert.Equal(erwartet.Verdichterregelung, m.Verdichterregelung);
    }

    [Fact]
    public void Skalieren_fuehrt_mit_dem_skalierten_Satz_in_Neu_und_legt_einen_eigenen_Satz_an()
    {
        var k = new KaeltemaschineKatalogDialogTests.Katalog();
        var cut = Aufbauen(k);
        int vorher = k.Gespeichert.Count;

        Knopf(cut, R.KM_BTN_SKALIEREN).Click();
        IElement wahl = cut.Find(".epos-kaeltemaschine-typwahl");
        // Ohne Wahl: benannte Ablehnung.
        wahl.QuerySelectorAll("button").First(b => b.TextContent.Trim() == R.ALLG_BTN_OK).Click();
        Assert.Contains(R.KM_MSG_TYPKENNFELD_WAEHLEN, cut.Find(".epos-kaeltemaschine-typwahl").TextContent);

        cut.Find(".epos-kaeltemaschine-typwahl select").Change("0");
        cut.Find(".epos-kaeltemaschine-typwahl").QuerySelectorAll("input")[0].Input("150");
        cut.Find(".epos-kaeltemaschine-typwahl").QuerySelectorAll("input")[1].Input("3,5");
        cut.Find(".epos-kaeltemaschine-typwahl").QuerySelectorAll("button").First(b => b.TextContent.Trim() == R.ALLG_BTN_OK).Click();

        Assert.True(cut.Instance.NeuOffen);
        cut.Find(".epos-kaeltemaschine-neu").QuerySelectorAll("button").First(b => b.TextContent.Trim() == R.ALLG_BTN_OK).Click();

        Assert.Equal(vorher + 1, k.Gespeichert.Count);
        KaeltemaschineModel m = k.Modelle.Last();
        Assert.Equal(150, m.Nennkaelteleistung_kW);
        Assert.Equal(3.5, m.Nenn_EER);
        Assert.EndsWith("(skaliert)", m.Bezeichner, StringComparison.Ordinal);
        Assert.False(m.ReadOnly);
        Assert.NotEmpty(m.Kennlinie);
        (double q, double eer) = KaeltemaschineTeillastDialogrechnung.Nennpunkt(m);
        Assert.Equal(150, q, 1);
        Assert.Equal(3.5, eer, 2);
    }

    [Fact]
    public void Die_Auskunft_rechnet_vier_Punkte_und_lehnt_leere_Eingaben_benannt_ab()
    {
        var k = new KaeltemaschineKatalogDialogTests.Katalog();
        var cut = Aufbauen(k);
        int vorher = k.Gespeichert.Count;

        Knopf(cut, R.KM_BTN_TEILLASTPUNKTE).Click();
        Assert.True(cut.Instance.AuskunftOffen);
        // Keine voreingetragenen Punkte.
        Assert.All(cut.Find(".epos-kaeltemaschine-auskunft").QuerySelectorAll("input"), i => Assert.Equal("", i.GetAttribute("value") ?? ""));
        Knopf(cut, R.KM_AUSK_RECHNEN).Click();
        Assert.Contains(R.KM_MSG_AUSKUNFT_EINGABE, cut.Find(".epos-kaeltemaschine-auskunft").TextContent);

        string[] werte = { "35", "100", "30", "75", "25", "50", "20", "25" };
        for (int i = 0; i < werte.Length; i++)
            cut.Find(".epos-kaeltemaschine-auskunft").QuerySelectorAll("input")[i].Input(werte[i]);
        Knopf(cut, R.KM_AUSK_RECHNEN).Click();

        Assert.NotNull(cut.Instance.AuskunftZeilen);
        Assert.Equal(4, cut.Instance.AuskunftZeilen!.Count);
        Assert.Equal(new[] { "A", "B", "C", "D" }, cut.Instance.AuskunftZeilen.Select(z => z.Name).ToArray());
        Assert.Equal(4, cut.FindAll(".epos-kaeltemaschine-auskunfttafel tbody tr").Count);
        Assert.All(cut.Instance.AuskunftZeilen, z => Assert.True(z.Eer > 0 && z.LeistungsaufnahmeKw > 0));
        Assert.Equal(vorher, k.Gespeichert.Count);

        cut.Find(".epos-kaeltemaschine-auskunft").QuerySelectorAll("button").First(b => b.TextContent.Trim() == R.ALLG_BTN_OK).Click();
        Assert.False(cut.Instance.AuskunftOffen);
    }

    [Fact]
    public void Eine_unvollstaendige_Kurve_wird_benannt_abgelehnt_und_nicht_geschrieben()
    {
        var k = new KaeltemaschineKatalogDialogTests.Katalog();
        var cut = MitKopie(k);
        int vorher = k.Gespeichert.Count;

        Gruppe(cut).QuerySelectorAll("input")[0].Input("0,1");
        Assert.True(cut.Instance.Geaendert);
        Assert.Equal(R.KM_TT_HINWEIS_BESTAND, Gruppe(cut).QuerySelector(".epos-kaeltemaschine-teillasthinweis")!.TextContent);
        Gruppe(cut).QuerySelectorAll("select")[0].Change("1");
        Assert.Equal(R.KM_TT_HINWEIS_VERWORFEN, Gruppe(cut).QuerySelector(".epos-kaeltemaschine-teillasthinweis")!.TextContent);

        Knopf(cut, R.ADM_BTN_SPEICHERN).Click();
        Assert.Equal(R.KM_MSG_TEILLASTKURVE_BEIWERTE, cut.Instance.Meldung);
        Assert.Equal(vorher, k.Gespeichert.Count);
    }
}
