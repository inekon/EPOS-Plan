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
/// <b>Das Kältemittel im Katalogdialog der Kältemaschine</b> (KM4-E3-a, Konzept Kältemittel als Stammdatum 7.1): die
/// Klappliste aus der Kältemitteltabelle samt Eintrag „nicht zugeordnet“, die Lesezeilen (GWP mit Herkunft nach KM4-Q17,
/// Abweichungshinweis, übrige Stammwerte), die Schnellwahl „GWP aus Kältemittel übernehmen“, der Lesemodus eines
/// Auslieferungssatzes, die Hülle ohne Oberfläche und die KI-Sicht. Kultur de-DE über die Hausvorrichtung.
/// </summary>
public class KaeltemaschineKatalogKaeltemittelTests : EposBunitContext
{
    public KaeltemaschineKatalogKaeltemittelTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private const string LESE_R32 = "F-Gas · Sicherheitsgruppe A2L";

    private static readonly IReadOnlyList<KaeltemittelListeneintrag> Liste = new[]
    {
        new KaeltemittelListeneintrag(1, "R32", "R32 — Difluormethan", 675, LESE_R32),
        new KaeltemittelListeneintrag(2, "R290", "R290 — Propan", 3, "kein F-Gas · natürlich"),
        new KaeltemittelListeneintrag(3, "SONDER", "SONDER — ohne Tabellen-GWP", null, "")
    };

    private static KaeltemaschineKatalogTexte Texte => new();

    /// <summary>Der Dialog mit dem ersten Saatsatz als Kältemittel <paramref name="text"/> und GWP <paramref name="gwp"/>.</summary>
    private IRenderedComponent<KaeltemaschineKatalogDialog> Aufbauen(KaeltemaschineKatalogDialogTests.Katalog k, string text,
                                                                     double? gwp, bool kopie = true, bool mitListe = true)
    {
        k.Saetze[0].Kaeltemittel = text;
        k.Saetze[0].Gwp = gwp;
        var cut = Render<KaeltemaschineKatalogDialog>(b => b
            .Add(x => x.Katalogzeilen, k.Zeilen)
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Rueckkuehlarten, KaeltemaschineKatalogHuelle.Rueckkuehlarten())
            .Add(x => x.Kaeltemittelliste, mitListe ? Liste : null)
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
        if (!kopie) return cut;
        cut.FindAll(".epos-auswahlleiste button")
           .First(b => b.TextContent.Trim().StartsWith(Texte.KnopfDuplizieren, StringComparison.Ordinal)).Click();
        cut.Find(".epos-ueberlagerung").QuerySelectorAll("button").First(b => b.TextContent.Trim() == Texte.Ok).Click();
        return cut;
    }

    private static IElement Kenndaten(IRenderedComponent<KaeltemaschineKatalogDialog> cut)
        => cut.FindAll("section.epos-stammblattgruppe").First(s => s.GetAttribute("aria-label") == R.ADM_SB_KENNDATEN);

    private static IElement? Klappliste(IRenderedComponent<KaeltemaschineKatalogDialog> cut)
        => Kenndaten(cut).QuerySelectorAll("select").FirstOrDefault(s => s.TextContent.Contains(R.KM_KMT_KEINS, StringComparison.Ordinal));

    private static IElement? Lesung(IRenderedComponent<KaeltemaschineKatalogDialog> cut)
        => Kenndaten(cut).QuerySelector(".epos-kaeltemittel-lesung");

    private static IElement? Schnellwahl(IRenderedComponent<KaeltemaschineKatalogDialog> cut)
        => Kenndaten(cut).QuerySelector(".epos-kaeltemittel-gwp-knopf");

    private static IElement Knopf(IRenderedComponent<KaeltemaschineKatalogDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // =================================================================================
    // Klappliste
    // =================================================================================

    [Fact]
    public void Die_Klappliste_traegt_die_Tabelle_und_speichert_den_Code()
    {
        var k = new KaeltemaschineKatalogDialogTests.Katalog();
        var cut = Aufbauen(k, "r290", null);

        IElement wahl = Klappliste(cut)!;
        Assert.Contains("R32 — Difluormethan", wahl.TextContent);
        Assert.Contains("R290 — Propan", wahl.TextContent);
        Assert.Equal("1", wahl.QuerySelectorAll("option").Single(o => o.HasAttribute("selected")).GetAttribute("value"));
        Assert.DoesNotContain(R.KM_KMT_NICHT_ZUGEORDNET.Replace("{0}", ""), wahl.TextContent);

        wahl.Change("0");
        Knopf(cut, R.ADM_BTN_SPEICHERN).Click();
        Assert.Equal("R32", k.Gespeichert.Last().Kaeltemittel);
        Assert.Equal("R32", k.Modelle.Last().Kaeltemittel);
    }

    [Fact]
    public void Ein_Bestandstext_bleibt_als_nicht_zugeordnet_waehlbar()
    {
        var k = new KaeltemaschineKatalogDialogTests.Katalog();
        var cut = Aufbauen(k, "Frigen X", 1500);

        IElement wahl = Klappliste(cut)!;
        IElement gewaehlt = wahl.QuerySelectorAll("option").Single(o => o.HasAttribute("selected"));
        Assert.Equal("-1", gewaehlt.GetAttribute("value"));
        Assert.Equal(string.Format(R.KM_KMT_NICHT_ZUGEORDNET, "Frigen X"), gewaehlt.TextContent.Trim());

        // Lesezeilen: GWP aus der Geraeteangabe, der Hinweis ohne Tabellenzeile; keine Schnellwahl.
        string lese = Lesung(cut)!.TextContent;
        Assert.Contains(string.Format(R.KM_KMT_GWP_GERAET, "1.500"), lese);
        Assert.Contains(R.KM_KMT_OHNE_ZUORDNUNG, lese);
        Assert.Null(Schnellwahl(cut));

        // Die Wahl desselben Eintrags laesst den Text stehen; Speichern schreibt ihn unveraendert.
        wahl.Change("-1");
        Knopf(cut, R.ADM_BTN_SPEICHERN).Click();
        Assert.Equal("Frigen X", k.Modelle.Last().Kaeltemittel);
    }

    [Fact]
    public void Leer_waehlen_loescht_den_Text()
    {
        var k = new KaeltemaschineKatalogDialogTests.Katalog();
        var cut = Aufbauen(k, "R32", null);
        Klappliste(cut)!.Change("");
        Assert.Null(Lesung(cut));
        Knopf(cut, R.ADM_BTN_SPEICHERN).Click();
        Assert.Null(k.Modelle.Last().Kaeltemittel);
    }

    // =================================================================================
    // Lesezeilen und Schnellwahl
    // =================================================================================

    [Fact]
    public void Die_Lesezeilen_zeigen_Tabellen_GWP_Abweichung_und_Stammwerte()
    {
        var k = new KaeltemaschineKatalogDialogTests.Katalog();
        var cut = Aufbauen(k, "R32", 700);

        IReadOnlyList<string> zeilen = Lesung(cut)!.QuerySelectorAll(".epos-herleitung").Select(z => z.TextContent.Trim()).ToList();
        Assert.Equal(new[]
        {
            string.Format(R.KM_KMT_GWP_TABELLE, "675"),
            string.Format(R.KM_KMT_GWP_ABWEICHUNG, "700", "675"),
            LESE_R32
        }, zeilen);
    }

    [Fact]
    public void Die_Schnellwahl_setzt_das_Tabellen_GWP_und_sperrt_danach()
    {
        var k = new KaeltemaschineKatalogDialogTests.Katalog();
        var cut = Aufbauen(k, "R32", 700);

        IElement knopf = Schnellwahl(cut)!;
        Assert.Equal(R.KM_KMT_GWP_UEBERNEHMEN, knopf.TextContent.Trim());
        Assert.False(knopf.HasAttribute("disabled"));
        knopf.Click();

        Assert.True(Schnellwahl(cut)!.HasAttribute("disabled"));
        Assert.DoesNotContain(string.Format(R.KM_KMT_GWP_ABWEICHUNG, "700", "675"), Lesung(cut)!.TextContent);
        Knopf(cut, R.ADM_BTN_SPEICHERN).Click();
        Assert.Equal(675, k.Gespeichert.Last().Gwp);
    }

    [Fact]
    public void Die_Schnellwahl_wirkt_bei_leerem_GWP_und_nicht_innerhalb_eines_Prozents()
    {
        var k = new KaeltemaschineKatalogDialogTests.Katalog();
        var leer = Aufbauen(k, "R32", null);
        Assert.False(Schnellwahl(leer)!.HasAttribute("disabled"));
        Assert.Contains(string.Format(R.KM_KMT_GWP_TABELLE, "675"), Lesung(leer)!.TextContent);

        Assert.False(KaeltemittelLesung.SchnellwahlAktiv(Liste, "R32", 681));   // 0,9 % daneben
        Assert.True(KaeltemittelLesung.SchnellwahlAktiv(Liste, "R32", 682));    // 1,04 % daneben
        Assert.False(KaeltemittelLesung.SchnellwahlAktiv(Liste, "SONDER", null)); // ohne Tabellenwert
        Assert.False(KaeltemittelLesung.SchnellwahlAktiv(Liste, "Frigen X", null));
        Assert.Empty(KaeltemittelLesung.Zeilen(Liste, "R32", 681, Texte)
                                       .Where(z => z.StartsWith(R.KM_KMT_GWP_ABWEICHUNG[..10], StringComparison.Ordinal)));
    }

    // =================================================================================
    // Lesemodus, ohne Gaben
    // =================================================================================

    [Fact]
    public void Ein_Auslieferungssatz_zeigt_Kaeltemittel_und_Lesezeilen_als_Text_ohne_Knopf()
    {
        var k = new KaeltemaschineKatalogDialogTests.Katalog();
        var cut = Aufbauen(k, "R32", 700, kopie: false);
        Assert.True(cut.Instance.Auslieferung);

        IElement g = Kenndaten(cut);
        Assert.Null(Klappliste(cut));
        Assert.Null(Schnellwahl(cut));
        Assert.Contains("R32 — Difluormethan", g.TextContent);
        Assert.Contains(string.Format(R.KM_KMT_GWP_TABELLE, "675"), g.TextContent);
        Assert.Contains(string.Format(R.KM_KMT_GWP_ABWEICHUNG, "700", "675"), g.TextContent);
        Assert.Contains(LESE_R32, g.TextContent);
    }

    [Fact]
    public void Ohne_Liste_bleibt_der_Bestandstext_ohne_Hinweis()
    {
        var k = new KaeltemaschineKatalogDialogTests.Katalog();
        var cut = Aufbauen(k, "R32", null, mitListe: false);

        IElement wahl = Klappliste(cut)!;
        Assert.Equal(2, wahl.QuerySelectorAll("option").Length);   // Platzhalter und Bestandstext
        Assert.Null(Lesung(cut));
    }

    // =================================================================================
    // Huelle ohne Oberflaeche, KI-Sicht
    // =================================================================================

    [Fact]
    public void Die_Huelle_bildet_die_Tabelle_auf_die_Eintraege_ab()
    {
        var saetze = new[]
        {
            new KaeltemittelCtrl.Kaeltemittelsatz(7, " R32 ", "Difluormethan", "HFKW", 675, "AR5", true, false, "A2L",
                                                  null, null, null, 1, true),
            new KaeltemittelCtrl.Kaeltemittelsatz(8, "R744", "", "natürlich", 1, "AR5", false, true, "A1",
                                                  null, null, null, 2, true),
            new KaeltemittelCtrl.Kaeltemittelsatz(9, "  ", "ohne Code", "", null, "", null, null, "", null, null, null, 3, false),
            null!
        };
        IReadOnlyList<KaeltemittelListeneintrag> l = KaeltemaschineKatalogHuelle.Kaeltemittelwahlen(saetze);

        Assert.Equal(2, l.Count);
        Assert.Equal(new KaeltemittelListeneintrag(7, "R32", "R32 — Difluormethan", 675, l[0].Lesewerte), l[0]);
        Assert.Equal("R744", l[1].Text);
        Assert.DoesNotContain("GWP", l[0].Lesewerte, StringComparison.Ordinal);
        Assert.Contains(R.KM_LESE_FGAS, l[0].Lesewerte);
        Assert.Contains(R.KM_LESE_NATUERLICH, l[1].Lesewerte);
        Assert.Empty(KaeltemaschineKatalogHuelle.Kaeltemittelwahlen(null!));
    }

    [Fact]
    public void Die_KI_Sicht_waehlt_ueber_den_Code()
    {
        var d = new KaeltemaschineDaten { Id = 5, Kaeltemittel = "Frigen X" };
        int geaendert = 0;
        var sicht = new KaeltemaschineKatalogKiSicht
        {
            ArbeitLesen = () => d,
            Kaeltemittelliste = () => Liste,
            Geaendert = () => geaendert++
        };

        Assert.Equal(new[] { "R32", "R290", "SONDER" }, sicht.KaeltemittelWahl.Select(e => e.Schluessel));
        Assert.Equal("R290 — Propan", sicht.KaeltemittelWahl[1].Text);
        Assert.Equal("Frigen X", sicht.Kaeltemittel);

        sicht.Kaeltemittel = "r290";
        Assert.Equal("R290", d.Kaeltemittel);
        sicht.Kaeltemittel = "";
        Assert.Equal("", d.Kaeltemittel);
        Assert.True(geaendert >= 2);
    }
}
