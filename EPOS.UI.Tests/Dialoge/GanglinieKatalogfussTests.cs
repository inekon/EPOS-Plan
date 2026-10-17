using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Solarthermie;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Katalogauswahl V1, Stufe 4: Solarthermie- und PV-Ganglinie.</b> Die Knöpfe der
/// <c>GanglinieKatalogseite</c> stehen in den Slots der <c>Zweispaltenauswahl</c> (links Vergleichen…,
/// Schloss…, Löschen; rechts Import…), der Formathinweis als Infoknopf im Katalogkopf; beide Listen
/// tragen Kästchen und Stift, der Stift öffnet die Ganglinie als Ansicht in der Satz-Überlagerung;
/// „Löschen" wirkt auf die Auswahl mit EINER Rückfrage; die Projekt-Kopfleiste übernimmt und
/// entfernt mehrere; die Abweichung der PV-Projektkopie (E113) steht in der Überlagerung genau einmal.
/// </summary>
public class GanglinieKatalogfussTests : EposBunitContext
{
    public GanglinieKatalogfussTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static IReadOnlyList<Katalogfilterzeile> Katalog => new[]
    {
        Zeitreihenproben.Zeile(21, "Ganglinie Nord", beschreibung: "Nord", jahresarbeitMwh: 3.9, spitzeKw: 5.4),
        Zeitreihenproben.Zeile(22, "Ganglinie Süd", beschreibung: "Süd", jahresarbeitMwh: 4.2, spitzeKw: 6.0),
        Zeitreihenproben.Zeile(23, "Ganglinie West", beschreibung: "West", jahresarbeitMwh: 4.0, spitzeKw: 5.8, geschuetzt: true)
    };

    private static ErzeugerZeile Zeile(int schluessel, string name, int id)
        => new() { Schluessel = schluessel, Bezeichner = name, GeraetId = id };

    private readonly List<string> _geloescht = new();

    private GanglinienKatalogwege Wege() => new()
    {
        Katalogzeilen = () => Task.FromResult(Katalog.Where(z => !_geloescht.Contains(z.Bezeichner)).ToList()
                                                     as IReadOnlyList<Katalogfilterzeile>),
        Loeschen = n => { _geloescht.Add(n); return Task.FromResult(true); },
        Einlesen = (_, _) => Task.FromResult(new GanglinienKatalogimport(false, false, "", "", ""))
    };

    private IRenderedComponent<SolarganglinieDialog> Solar(GanglinienKatalogwege? wege = null, List<ErzeugerZeile>? zeilen = null)
        => Render<SolarganglinieDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<ErzeugerZeile> { Zeile(1, "Ganglinie Nord", 21) })
            .Add(x => x.Katalogwege, wege ?? Wege())
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.Solarganglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Aufnehmen, id => Zeile(100000 + id, "Neu " + id, id)));

    private IRenderedComponent<PvGanglinieDialog> Pv(GanglinienKatalogwege? wege = null, Func<string, string>? abweichung = null)
        => Render<PvGanglinieDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Ganglinie Nord", 21), Zeile(2, "Ganglinie Süd", 22) })
            .Add(x => x.Katalogwege, wege ?? Wege())
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.PvGanglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.KatalogAbweichung, abweichung)
            .Add(x => x.AusKatalogErneuern, _ => Task.FromResult(new PvGanglinieErneuerung(true, "")))
            .Add(x => x.Aufnehmen, id => Zeile(100000 + id, "Neu " + id, id)));

    private static IElement Katalogliste<T>(IRenderedComponent<T> cut) where T : IComponent
        => cut.FindAll(".epos-raster").Last();

    private static void KatalogKlick<T>(IRenderedComponent<T> cut, int index, bool strg = false) where T : IComponent
        => Katalogliste(cut).QuerySelectorAll("tbody tr .epos-zeilenzelle--name")[index]
                            .Click(new MouseEventArgs { CtrlKey = strg });

    private static IElement Fussleiste<T>(IRenderedComponent<T> cut) where T : IComponent
        => cut.Find(".epos-zweispalten-satzknoepfe");   // AW3b: die Knopfleiste steht am gewaehlten Satz

    // =================================================================================
    // Die Fussleiste in den Slots
    // =================================================================================

    [Fact]
    public void Solar_Fussleiste_traegt_Vergleichen_Schloss_Loeschen_und_rechts_Import()
    {
        var cut = Solar();
        IElement fuss = Fussleiste(cut);
        Assert.Single(fuss.QuerySelectorAll("button.epos-knopf--vergleichen"));
        Assert.Single(fuss.QuerySelectorAll("button.epos-katalog-loeschen"));
        Assert.Single(fuss.QuerySelectorAll("button.epos-importknopf"));
        // Import steht rechts vom Fueller, Vergleichen links davon.
        List<string> folge = fuss.Children.Select(e => e.ClassName ?? "").ToList();
        // AW3b: am Satz stehen sie in einer Zeile - Vergleichen vorn, Import hinten.
        Assert.True(folge.FindIndex(k => k.Contains("epos-knopf--vergleichen")) < folge.FindIndex(k => k.Contains("epos-importknopf")));
        // Keine eigene Leiste der Seite mehr, der Formathinweis als Infoknopf im Katalogkopf.
        Assert.Empty(cut.FindAll(".epos-ganglinie-katalogfuss"));
        Assert.Single(cut.FindAll(".epos-ganglinie-formathinweis .epos-infoknopf"));
        Assert.Empty(fuss.QuerySelectorAll(".epos-ganglinie-formathinweis"));
    }

    [Fact]
    public void Pv_Fussleiste_traegt_zusaetzlich_Nennleistung()
    {
        var wege = new GanglinienKatalogwege
        {
            Katalogzeilen = () => Task.FromResult(Katalog),
            NennleistungSchreiben = (_, _) => Task.FromResult(new GanglinienNennleistungsschrieb(true, "", ""))
        };
        var cut = Pv(wege);
        Assert.Single(Fussleiste(cut).QuerySelectorAll("button.epos-nennleistungknopf"));
        Assert.Single(Fussleiste(cut).QuerySelectorAll("button.epos-knopf--vergleichen"));
    }

    [Fact]
    public void Vergleichen_braucht_zwei_angekreuzte_Saetze_und_der_Fuss_nennt_die_Zahl()
    {
        var cut = Solar();
        KatalogKlick(cut, 0);
        Assert.True(Fussleiste(cut).QuerySelector("button.epos-knopf--vergleichen")!.HasAttribute("disabled"));
        KatalogKlick(cut, 0, strg: true);
        KatalogKlick(cut, 1, strg: true);
        Assert.Equal(2, cut.FindComponent<GanglinieKatalogseite>().Instance.Ziele().Count);   // AW3b: die Zahl stand im Fuss
        IElement knopf = Fussleiste(cut).QuerySelector("button.epos-knopf--vergleichen")!;
        Assert.False(knopf.HasAttribute("disabled"));
        knopf.Click();
        Assert.True(cut.FindComponent<Katalogliste>().Instance.VergleichOffen);
    }

    // =================================================================================
    // Loeschen auf der Auswahl - eine Rueckfrage
    // =================================================================================

    [Fact]
    public void Mehrfachloeschen_fragt_einmal_nennt_den_gesperrten_und_loescht_die_freien()
    {
        var cut = Solar(zeilen: new List<ErzeugerZeile>());
        KatalogKlick(cut, 0, strg: true);
        KatalogKlick(cut, 1, strg: true);
        KatalogKlick(cut, 2, strg: true);
        Fussleiste(cut).QuerySelector("button.epos-katalog-loeschen")!.Click();

        var seite = cut.FindComponent<GanglinieKatalogseite>().Instance;
        Assert.True(seite.Loeschfrage);
        Assert.Equal(new[] { "Ganglinie Nord", "Ganglinie Süd" }, seite.Loeschziele);
        string frage = cut.Find(".epos-rueckfrage").TextContent;
        Assert.Contains("Ganglinie West", frage);

        cut.FindAll("button").First(b => b.TextContent.Trim() == R.ALLG_BTN_JA).Click();
        Assert.Equal(new[] { "Ganglinie Nord", "Ganglinie Süd" }, _geloescht);
        Assert.Single(Katalogliste(cut).QuerySelectorAll("tbody tr .epos-zeilenzelle--name"));
        Assert.Equal(0, seite.Wahl.Anzahl);
    }

    [Fact]
    public void Ein_gesperrter_Satz_allein_nennt_den_Grund_ohne_Rueckfrage()
    {
        var cut = Solar(zeilen: new List<ErzeugerZeile>());
        KatalogKlick(cut, 2);
        IElement knopf = Fussleiste(cut).QuerySelector("button.epos-katalog-loeschen")!;
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        knopf.Click();
        Assert.False(cut.FindComponent<GanglinieKatalogseite>().Instance.Loeschfrage);
        Assert.Empty(_geloescht);
    }

    // =================================================================================
    // Stift je Zeile
    // =================================================================================

    [Fact]
    public void Solar_Stift_der_Katalogzeile_oeffnet_die_Ansicht_in_der_Ueberlagerung()
    {
        var cut = Solar();
        Katalogliste(cut).QuerySelectorAll("tbody tr .epos-zeilenstift")[2].Click();
        Assert.True(cut.FindComponent<Zweispaltenauswahl>().Instance.SatzUeberlagerungOffen);
        Assert.Equal("Ganglinie West", cut.Instance.Katalogzeile?.Bezeichner);
        // Reine Ansicht: nur „Schließen", kein OK.
        var knoepfe = cut.Find(".epos-satzueberlagerung").QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.DoesNotContain(R.ALLG_BTN_OK, knoepfe);
    }

    [Fact]
    public void Solar_Stift_der_Projektzeile_waehlt_sie_und_oeffnet_die_Ueberlagerung()
    {
        var cut = Solar();
        cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr .epos-spalte-stift button")[0].Click();
        Assert.True(cut.FindComponent<Zweispaltenauswahl>().Instance.SatzUeberlagerungOffen);
        Assert.Equal("Ganglinie Nord", cut.Instance.Projektzeile?.Bezeichner);
    }

    [Fact]
    public void Pv_Stift_der_abweichenden_Projektzeile_zeigt_die_Abweichung_genau_einmal()
    {
        var cut = Pv(abweichung: n => n == "Ganglinie Nord" ? "weicht vom Katalog ab: Reihe" : "");
        cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr .epos-spalte-stift button")[0].Click();
        Assert.True(cut.FindComponent<Zweispaltenauswahl>().Instance.SatzUeberlagerungOffen);
        Assert.Single(cut.FindAll(".epos-pvg-abweichungszeile"));
        Assert.Single(cut.FindAll(".epos-satzueberlagerung .epos-pvg-abweichungszeile"));
        Assert.Single(cut.FindAll(".epos-satzueberlagerung .epos-pvg-erneuernknopf"));
    }

    // =================================================================================
    // Mehrfach uebernehmen und entfernen
    // =================================================================================

    [Fact]
    public void Uebernehmen_und_Entfernen_wirken_auf_die_Auswahl()
    {
        var zeilen = new List<ErzeugerZeile>();
        var cut = Solar(zeilen: zeilen);
        KatalogKlick(cut, 0, strg: true);
        KatalogKlick(cut, 1, strg: true);
        cut.Find(".epos-zweispalten-knopf--uebernehmen").Click();
        Assert.Equal(2, zeilen.Count);
        Assert.Equal(2, cut.Instance.ProjektWahl.Anzahl);

        cut.Find(".epos-zweispalten-knopf--entfernen").Click();
        Assert.Empty(zeilen);
        Assert.Null(cut.Instance.Projektzeile);
    }

    // =================================================================================
    // KT-4: das Lesen der Kennzahlen wirft
    // =================================================================================

    [Fact]
    public void Pv_wirft_das_Lesen_der_Kennzahlen_nennt_der_Banner_den_Grund()
    {
        var cut = Render<PvGanglinieDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile>())
            .Add(x => x.Katalogwege, Wege())
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.PvGanglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Kennzahlen, _ => throw new InvalidOperationException("Reihe gesperrt")));
        KatalogKlick(cut, 0);
        Assert.Contains("Reihe gesperrt", cut.Instance.SatzFehler);
        Assert.Contains(cut.FindAll(".epos-warnbanner-text"), e => e.TextContent.Contains("Reihe gesperrt"));
    }

    // =================================================================================
    // AW3b: eine Wahl je Zeile, Knopfleiste am gewaehlten Satz (Schema AW1)
    // =================================================================================

    private GanglinienKatalogwege WegeMitSchloss(Schlosspruefung schloss) => new()
    {
        Katalogzeilen = () => Task.FromResult(Katalog),
        Loeschen = n => { _geloescht.Add(n); return Task.FromResult(true); },
        Schloss = schloss.Weg()
    };

    private static List<ErzeugerZeile> DreiZeilen()
        => new() { Zeile(1, "Projekt Nord", 21), Zeile(2, "Projekt Süd", 22), Zeile(3, "Projekt West", 23) };

    [Fact]
    public void AW3b_Solar_Die_Zeile_ist_die_Wahl_mit_Hervorhebung_und_Tasten()
    {
        var cut = Solar(zeilen: DreiZeilen());
        KnopfleisteAmSatzPruefung.ZeileIstWahl(cut, 3);
        KnopfleisteAmSatzPruefung.KlickWaehlt(cut, 1, "Projekt Süd");
        KnopfleisteAmSatzPruefung.PfeiltastenBewegen(cut, "Projekt Nord", "Projekt Süd", "Projekt West");
        KnopfleisteAmSatzPruefung.Schlussleiste(cut, "OK", "Abbrechen");
    }

    [Fact]
    public void AW3b_Solar_Beim_Projektsatz_weich_gesperrt_beim_Katalogsatz_frei()
    {
        var schloss = new Schlosspruefung();
        var cut = Solar(WegeMitSchloss(schloss), DreiZeilen());
        KnopfleisteAmSatzPruefung.Projektzeilen(cut)[0].Click();
        Assert.True(KnopfleisteAmSatzPruefung.AmSatz(cut, "epos-knopf--vergleichen").HasAttribute("disabled"));
        KnopfleisteAmSatzPruefung.WeichGesperrt(cut, schloss, "epos-katalog-loeschen");
        Assert.Empty(_geloescht);

        KnopfleisteAmSatzPruefung.AmSatz(cut, "epos-knopf--bearbeiten-projekt").Click();   // = der Stift: Ansicht
        Assert.NotEmpty(cut.FindAll(".epos-satzueberlagerung-koerper"));
        cut.FindAll(".epos-satzueberlagerung-schliessen, .epos-satzueberlagerung-abbrechen")[0].Click();

        KatalogKlick(cut, 0);
        KnopfleisteAmSatzPruefung.KatalogsatzFrei(cut, "epos-katalog-loeschen");
        KatalogKlick(cut, 0, strg: true);
        KatalogKlick(cut, 1, strg: true);
        Assert.False(KnopfleisteAmSatzPruefung.AmSatz(cut, "epos-knopf--vergleichen").HasAttribute("disabled"));
    }

    [Fact]
    public void AW3b_Pv_Die_Zeile_ist_die_Wahl_und_der_Projektsatz_sperrt_weich()
    {
        var schloss = new Schlosspruefung();
        var wege = WegeMitSchloss(schloss);
        var cut = Pv(wege);
        KnopfleisteAmSatzPruefung.ZeileIstWahl(cut, 2);
        KnopfleisteAmSatzPruefung.KlickWaehlt(cut, 1, "Ganglinie Süd");
        KnopfleisteAmSatzPruefung.PfeiltastenBewegen(cut, "Ganglinie Nord", "Ganglinie Süd", "Ganglinie Süd");
        KnopfleisteAmSatzPruefung.WeichGesperrt(cut, schloss, "epos-katalog-loeschen");
        KnopfleisteAmSatzPruefung.Schlussleiste(cut, "OK", "Abbrechen");

        KatalogKlick(cut, 0);   // im Projekt verwendet: Loeschen nennt diesen Grund, nicht „Katalogsatz wählen"
        Assert.False(KnopfleisteAmSatzPruefung.AmSatz(cut, "epos-katalogschloss").HasAttribute("aria-disabled"));
        Assert.NotEqual(R.AUSWAHL_SPERRE_KATALOGSATZ, KnopfleisteAmSatzPruefung.AmSatz(cut, "epos-katalog-loeschen").GetAttribute("title"));
        KnopfleisteAmSatzPruefung.AmSatz(cut, "epos-knopf--bearbeiten-katalog").Click();   // = der Stift: Ansicht
        Assert.NotEmpty(cut.FindAll(".epos-satzueberlagerung-koerper"));
    }
}
