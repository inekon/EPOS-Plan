using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Solarthermie;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Detailzeile der Ganglinien-Dialoge</b> (DZ1, Konzept Projektdialoge mit
/// Katalogauswahl 4.4 und 4.9): Alle Dialoge mit CSV-Import nennen den gewählten Satz samt
/// Marke und zeigen aufgeklappt die Ganglinie mit Kennzahlen — derselbe Baustein
/// <see cref="GanglinienGrafik"/> wie im Wärmebedarf extern.
/// </summary>
public class GanglinieDetailzeileTests : EposBunitContext
{
    private static IReadOnlyList<Katalogfilterzeile> Katalog => new[]
    {
        Zeitreihenproben.Zeile(21, "Ganglinie Nord", beschreibung: "Nord", jahresarbeitMwh: 3.9, spitzeKw: 5.4),
        Zeitreihenproben.Zeile(22, "Ganglinie Süd", beschreibung: "Süd", jahresarbeitMwh: 4.2, spitzeKw: 6.0)
    };

    private readonly List<GanglinienWahl> _gefragt = new();

    public GanglinieDetailzeileTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private Task<GanglinienKennzahlen?> Kennzahlen(GanglinienWahl w)
    {
        _gefragt.Add(w);
        return Task.FromResult<GanglinienKennzahlen?>(new GanglinienKennzahlen(4.2, 6.0, 700));
    }

    private static Zeichenmodell? Bild(GanglinienWahl w, bool sortiert) => null;

    private static ErzeugerZeile Zeile(int schluessel, string name, int id)
        => new() { Schluessel = schluessel, Bezeichner = name, GeraetId = id };

    private static string Satzname<T>(IRenderedComponent<T> cut) where T : Microsoft.AspNetCore.Components.IComponent
        => cut.Find(".epos-zweispalten-satzname").TextContent.Trim();

    private static string Satzmarke<T>(IRenderedComponent<T> cut) where T : Microsoft.AspNetCore.Components.IComponent
        => cut.Find(".epos-zweispalten-marke--satz").TextContent.Trim();

    /// <summary>Wählt die Zeile <paramref name="index"/> der Liste <paramref name="liste"/> (0 = Projekt, 1 = Katalog).</summary>
    private static void Waehlen<T>(IRenderedComponent<T> cut, int liste, int index) where T : Microsoft.AspNetCore.Components.IComponent
        => cut.FindAll(".epos-raster")[liste].QuerySelectorAll("tbody tr")[index].QuerySelector("button")!.Click();

    /// <summary>Klappt die Detailzeile auf und liefert, ob sie die Ganglinie trägt.</summary>
    private static bool AufgeklapptMitGanglinie<T>(IRenderedComponent<T> cut) where T : Microsoft.AspNetCore.Components.IComponent
    {
        cut.Find(".epos-zweispalten-satzzeile").Click();
        Assert.Equal("true", cut.Find(".epos-zweispalten-satzzeile").GetAttribute("aria-expanded"));
        return cut.FindAll(".epos-zweispalten-satz .epos-ganglinie-grafik").Count == 1;
    }

    // =================================================================================
    // Solarthermieganglinie — der Befund des Anwenders ("Kein Satz gewählt")
    // =================================================================================

    private IRenderedComponent<SolarganglinieDialog> Solar(bool mitGrafik = true)
        => Render<SolarganglinieDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Ganglinie Nord", 501) })
            .Add(x => x.Katalogwege, new GanglinienKatalogwege { Katalogzeilen = () => Task.FromResult(Katalog) })
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.Solarganglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Aufnehmen, id => Zeile(100000, "Ganglinie Süd", id))
            .Add(x => x.Kennzahlen, mitGrafik ? Kennzahlen : null)
            .Add(x => x.Bildauftrag, mitGrafik ? Bild : null));

    [Fact]
    public void Solarganglinie_Detailzeile_nennt_den_Katalogsatz_und_zeigt_die_Ganglinie()
    {
        var cut = Solar();
        Assert.Equal(Resource.AUSWAHL_SATZ_LEER, Satzname(cut));

        Waehlen(cut, 1, 1);

        Assert.Equal("Ganglinie Süd", Satzname(cut));
        Assert.Equal(Resource.AUSWAHL_MARKE_KATALOGSATZ, Satzmarke(cut));
        Assert.Equal(new GanglinienWahl(true, 22, "Ganglinie Süd"), cut.Instance.Grafikwahl);
        Assert.True(AufgeklapptMitGanglinie(cut));
        Assert.Contains("4", cut.Find(".epos-zweispalten-satz .epos-ganglinie-kennzahlen").TextContent);
    }

    /// <summary>
    /// DZ1‑N2: Mit <c>BildauftragMass</c> entsteht das Modell zuerst in Vorgabegröße (0 × 0) und nach
    /// der Meldung des Behältermaßes in genau dieser Größe neu — die Kurve nimmt die volle Breite.
    /// </summary>
    [Fact]
    public void Solarganglinie_baut_das_Modell_im_gemeldeten_Behaeltermass()
    {
        var masse = new List<(bool Sortiert, int Breite, int Hoehe)>();
        var cut = Render<SolarganglinieDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Ganglinie Nord", 501) })
            .Add(x => x.Katalogwege, new GanglinienKatalogwege { Katalogzeilen = () => Task.FromResult(Katalog) })
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.Solarganglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Kennzahlen, Kennzahlen)
            .Add(x => x.Bildauftrag, Bild)
            .Add(x => x.BildauftragMass, (w, s, b, h) => { masse.Add((s, b, h)); return null; }));

        Waehlen(cut, 1, 1);
        Assert.True(AufgeklapptMitGanglinie(cut));
        Assert.Contains((false, 0, 0), masse);

        var svg = cut.FindComponent<DiagrammSvg>();
        cut.InvokeAsync(() => svg.Instance.MassGemeldet(900, 180));

        Assert.Equal((false, 900, 180), masse[^1]);
        Assert.Equal(new Flaechenmass(900, 180), cut.FindComponent<GanglinienGrafik>().Instance.Zeichenmass);
    }

    [Fact]
    public void Solarganglinie_Projektzeile_fragt_ihre_Projektkopie_eine_neue_den_Katalog()
    {
        var cut = Solar();
        Waehlen(cut, 0, 0);
        Assert.Equal("Ganglinie Nord", Satzname(cut));
        Assert.Equal(Resource.AUSWAHL_MARKE_PROJEKTSATZ, Satzmarke(cut));
        Assert.Equal(new GanglinienWahl(false, 501, "Ganglinie Nord"), cut.Instance.Grafikwahl);

        // Eine eben aufgenommene Zeile traegt die Katalog-Id: Id 0, der Kern faellt auf den Namen zurueck.
        Waehlen(cut, 1, 1);
        cut.Find(".epos-zweispalten-knopf--uebernehmen").Click();
        Assert.Equal(new GanglinienWahl(false, 0, "Ganglinie Süd"), cut.Instance.Grafikwahl);
        Assert.True(AufgeklapptMitGanglinie(cut));
    }

    [Fact]
    public void Solarganglinie_ohne_Kennzahlenweg_nennt_den_Satz_ohne_Grafik()
    {
        var cut = Solar(mitGrafik: false);
        Waehlen(cut, 1, 0);
        Assert.Equal("Ganglinie Nord", Satzname(cut));
        Assert.False(AufgeklapptMitGanglinie(cut));
    }

    // =================================================================================
    // PV-Ganglinie
    // =================================================================================

    [Fact]
    public void PvGanglinie_Detailzeile_nennt_den_Satz_und_zeigt_die_Ganglinie()
    {
        var neu = Zeile(100000, "Ganglinie Süd", 22);
        var cut = Render<PvGanglinieDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Ganglinie Nord", 601) })
            .Add(x => x.Katalogwege, new GanglinienKatalogwege { Katalogzeilen = () => Task.FromResult(Katalog) })
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.PvGanglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Aufnehmen, _ => neu)
            .Add(x => x.Kennzahlen, Kennzahlen)
            .Add(x => x.Bildauftrag, Bild));

        Waehlen(cut, 0, 0);
        Assert.Equal("Ganglinie Nord", Satzname(cut));
        Assert.Equal(Resource.AUSWAHL_MARKE_PROJEKTSATZ, Satzmarke(cut));
        Assert.Equal(new GanglinienWahl(false, 601, "Ganglinie Nord"), cut.Instance.Grafikwahl);

        Waehlen(cut, 1, 1);
        Assert.Equal(Resource.AUSWAHL_MARKE_KATALOGSATZ, Satzmarke(cut));
        Assert.Equal(new GanglinienWahl(true, 22, "Ganglinie Süd"), cut.Instance.Grafikwahl);
        Assert.True(AufgeklapptMitGanglinie(cut));

        // Die Namens- und Kennwertfelder stehen mit der Grafik in der Satzflaeche - verdichtet (DZ1-N1):
        // alle fuenf in der Kopfzeile, die Infoknoepfe in der Kennzahlenzeile, keine eigene Knopfzeile.
        Assert.Equal(5, cut.FindAll(".epos-zweispalten-satz .epos-ganglinie-kopf > .epos-feld").Count);
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-satz .epos-ganglinie-leiste .epos-ganglinie-knoepfe .epos-infoknopf").Count);
        // Die Huelle .epos-berechnungshilfe steht genau einmal - in der Kennzahlenzeile.
        Assert.Single(cut.FindAll(".epos-zweispalten-satz .epos-berechnungshilfe"));
        Assert.Single(cut.FindAll(".epos-zweispalten-satz .epos-ganglinie-leiste .epos-ganglinie-knoepfe > .epos-berechnungshilfe"));
    }

    // =================================================================================
    // Verdichteter Kopf der Satzflaeche (DZ1-N1)
    // =================================================================================

    [Fact]
    public void Solarganglinie_Kopf_traegt_Name_und_Beschreibung_in_einer_Zeile_und_die_Infoknoepfe_in_der_Kennzahlenzeile()
    {
        var cut = Solar();
        Waehlen(cut, 0, 0);
        Assert.True(AufgeklapptMitGanglinie(cut));

        // EINE Kopfzeile mit Name und Beschreibung, beide als einzeilige Lesefelder (kein textarea).
        var kopf = Assert.Single(cut.FindAll(".epos-zweispalten-satz .epos-ganglinie-grafik > .epos-ganglinie-kopf"));
        var felder = kopf.QuerySelectorAll(":scope > .epos-feld");
        Assert.Equal(2, felder.Length);
        Assert.Equal(new[] { "Name:", "Beschreibung:" },
                     felder.Select(f => f.QuerySelector(".epos-feld-text")!.TextContent.Trim()));
        Assert.All(felder, f => Assert.NotNull(f.QuerySelector("input[readonly]")));
        Assert.Empty(kopf.QuerySelectorAll("textarea"));
        Assert.Equal("Ganglinie Nord", felder[0].QuerySelector("input")!.GetAttribute("value"));

        // Die Kennzahlenzeile traegt Schalter, Einheitenwahl und die zwei Infoknoepfe; keine eigene Knopfzeile.
        var leiste = cut.Find(".epos-zweispalten-satz .epos-ganglinie-grafik > .epos-ganglinie-leiste");
        Assert.NotNull(leiste.QuerySelector("input[type=checkbox]"));
        Assert.NotNull(leiste.QuerySelector("select"));
        Assert.Equal(2, leiste.QuerySelectorAll(".epos-ganglinie-knoepfe .epos-infoknopf").Length);
        // Die Huelle .epos-berechnungshilfe steht genau einmal - in der Kennzahlenzeile.
        Assert.Single(cut.FindAll(".epos-zweispalten-satz .epos-berechnungshilfe"));
        Assert.Single(cut.FindAll(".epos-zweispalten-satz .epos-ganglinie-leiste .epos-ganglinie-knoepfe > .epos-berechnungshilfe"));
    }

    [Fact]
    public void Solarganglinie_ohne_Grafik_behaelt_Knopfzeile_und_Felder_untereinander()
    {
        var cut = Solar(mitGrafik: false);
        Waehlen(cut, 1, 0);
        Assert.Empty(cut.FindAll(".epos-ganglinie-kopf"));
        Assert.Single(cut.FindAll(".epos-zweispalten-satz .epos-berechnungshilfe"));
        Assert.Single(cut.FindAll(".epos-zweispalten-satz textarea[readonly]"));
    }

    // =================================================================================
    // Stromganglinie und Wärmebedarf extern (das Muster)
    // =================================================================================

    [Fact]
    public void Stromganglinie_Detailzeile_nennt_den_Satz_und_zeigt_die_Ganglinie()
    {
        var cut = Render<StromganglinieDialog>(p => p
            .Add(x => x.Zeilen, new List<GanglinienProjektZeile> { new(1, 701, "Ganglinie Nord") })
            .Add(x => x.Katalogzeilen, () => Task.FromResult(Katalog))
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.Stromganglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Kennzahlen, Kennzahlen)
            .Add(x => x.Bildauftrag, Bild));

        Waehlen(cut, 1, 1);
        Assert.Equal("Ganglinie Süd", Satzname(cut));
        Assert.Equal(Resource.AUSWAHL_MARKE_KATALOGSATZ, Satzmarke(cut));
        Waehlen(cut, 0, 0);
        Assert.Equal("Ganglinie Nord", Satzname(cut));
        Assert.Equal(Resource.AUSWAHL_MARKE_PROJEKTSATZ, Satzmarke(cut));
        Assert.True(AufgeklapptMitGanglinie(cut));
    }

    [Fact]
    public void WaermebedarfExtern_Detailzeile_nennt_den_Satz_und_zeigt_die_Ganglinie()
    {
        var cut = Render<WaermebedarfExternDialog>(p => p
            .Add(x => x.Zeilen, new List<WaermebedarfExternZeile>
                 { new() { IdZ = 1, IdGanglinie = 801, Bezeichner = "Ganglinie Nord", Kanal = "HEIZUNG" } })
            .Add(x => x.Katalogzeilen, () => Task.FromResult(Katalog))
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.Waermebedarf))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Kennzahlen, Kennzahlen)
            .Add(x => x.Bildauftrag, Bild));

        // Beim Öffnen ist die erste Zuordnung gewählt.
        Assert.Equal("Ganglinie Nord", Satzname(cut));
        Assert.Equal(Resource.AUSWAHL_MARKE_PROJEKTSATZ, Satzmarke(cut));
        Waehlen(cut, 1, 1);
        Assert.Equal("Ganglinie Süd", Satzname(cut));
        Assert.Equal(Resource.AUSWAHL_MARKE_KATALOGSATZ, Satzmarke(cut));
        Assert.True(AufgeklapptMitGanglinie(cut));
    }

    // =================================================================================
    // UeS1c: die Ganglinie in der Satz-Ueberlagerung
    // =================================================================================

    /// <summary>
    /// Prüft „Vergrößern": Die Überlagerung trägt die Grafik genau einmal im Körper, die Fußleiste nur
    /// „Schließen" ohne Schlosshinweis (reine Ansicht), und das gemeldete Behältermaß baut das Modell in
    /// genau dieser Größe. Schließen stellt die Grafik zurück in die Satzfläche.
    /// </summary>
    private static void UeberlagerungPruefen<T>(IRenderedComponent<T> cut, List<(bool Sortiert, int Breite, int Hoehe)> masse,
                                                Func<IRenderedComponent<DiagrammSvg>> grafik)
        where T : Microsoft.AspNetCore.Components.IComponent
    {
        cut.Find(".epos-zweispalten-vergroessern").Click();

        Assert.Single(cut.FindAll(".epos-ganglinie-grafik"));
        Assert.Single(cut.FindAll(".epos-satzueberlagerung-koerper .epos-ganglinie-grafik"));
        Assert.Empty(cut.FindAll(".epos-satzueberlagerung-ok"));
        Assert.Empty(cut.FindAll(".epos-satzueberlagerung-abbrechen"));
        Assert.Empty(cut.FindAll(".epos-satzueberlagerung-hinweis"));
        Assert.Single(cut.FindAll(".epos-satzueberlagerung-schliessen"));

        var svg = grafik();
        cut.InvokeAsync(() => svg.Instance.MassGemeldet(1240, 560));
        Assert.Equal((false, 1240, 560), masse[^1]);

        cut.Find(".epos-satzueberlagerung-schliessen").Click();
        Assert.Empty(cut.FindAll(".epos-ueberlagerung--satz"));
        Assert.Single(cut.FindAll(".epos-zweispalten-satz .epos-ganglinie-grafik"));
    }

    [Fact]
    public void UeS1c_Solarganglinie_Vergroessern_zeichnet_die_Kurve_im_Mass_der_Ueberlagerung()
    {
        var masse = new List<(bool Sortiert, int Breite, int Hoehe)>();
        var cut = Render<SolarganglinieDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Ganglinie Nord", 501) })
            .Add(x => x.Katalogwege, new GanglinienKatalogwege { Katalogzeilen = () => Task.FromResult(Katalog) })
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.Solarganglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Kennzahlen, Kennzahlen)
            .Add(x => x.Bildauftrag, Bild)
            .Add(x => x.BildauftragMass, (w, s, b, h) => { masse.Add((s, b, h)); return null; }));
        Waehlen(cut, 1, 1);
        UeberlagerungPruefen(cut, masse, () => cut.FindComponent<DiagrammSvg>());
    }

    [Fact]
    public void UeS1c_PvGanglinie_Vergroessern_zeichnet_die_Kurve_im_Mass_der_Ueberlagerung()
    {
        var masse = new List<(bool Sortiert, int Breite, int Hoehe)>();
        var cut = Render<PvGanglinieDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Ganglinie Nord", 601) })
            .Add(x => x.Katalogwege, new GanglinienKatalogwege { Katalogzeilen = () => Task.FromResult(Katalog) })
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.PvGanglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Kennzahlen, Kennzahlen)
            .Add(x => x.Bildauftrag, Bild)
            .Add(x => x.BildauftragMass, (w, s, b, h) => { masse.Add((s, b, h)); return null; }));
        Waehlen(cut, 0, 0);
        UeberlagerungPruefen(cut, masse, () => cut.FindComponent<DiagrammSvg>());
    }

    [Fact]
    public void UeS1c_Stromganglinie_Vergroessern_zeichnet_die_Kurve_im_Mass_der_Ueberlagerung()
    {
        var masse = new List<(bool Sortiert, int Breite, int Hoehe)>();
        var cut = Render<StromganglinieDialog>(p => p
            .Add(x => x.Zeilen, new List<GanglinienProjektZeile> { new(1, 701, "Ganglinie Nord") })
            .Add(x => x.Katalogzeilen, () => Task.FromResult(Katalog))
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.Stromganglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Kennzahlen, Kennzahlen)
            .Add(x => x.Bildauftrag, Bild)
            .Add(x => x.BildauftragMass, (w, s, b, h) => { masse.Add((s, b, h)); return null; }));
        Waehlen(cut, 0, 0);
        UeberlagerungPruefen(cut, masse, () => cut.FindComponent<DiagrammSvg>());
    }

    [Fact]
    public void UeS1c_WaermebedarfExtern_Vergroessern_zeichnet_die_Kurve_im_Mass_der_Ueberlagerung()
    {
        var masse = new List<(bool Sortiert, int Breite, int Hoehe)>();
        var cut = Render<WaermebedarfExternDialog>(p => p
            .Add(x => x.Zeilen, new List<WaermebedarfExternZeile>
                 { new() { IdZ = 1, IdGanglinie = 801, Bezeichner = "Ganglinie Nord", Kanal = "HEIZUNG" } })
            .Add(x => x.Katalogzeilen, () => Task.FromResult(Katalog))
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.Waermebedarf))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Kennzahlen, Kennzahlen)
            .Add(x => x.Bildauftrag, Bild)
            .Add(x => x.BildauftragMass, (w, s, b, h) => { masse.Add((s, b, h)); return null; }));
        UeberlagerungPruefen(cut, masse, () => cut.FindComponent<DiagrammSvg>());
    }
}
