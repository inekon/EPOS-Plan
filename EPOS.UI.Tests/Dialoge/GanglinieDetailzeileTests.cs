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

        // Die Namens- und Kennwertfelder stehen mit der Grafik in der Satzflaeche.
        Assert.NotEmpty(cut.FindAll(".epos-zweispalten-satz .epos-berechnungshilfe"));
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
}
