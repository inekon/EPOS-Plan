using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Standards;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Baustein <c>PvModellFelder</c> (Paket A/B des PV-Ertragsmodells, Merge 5): die
/// Regeln von <c>Form_PV.ModellUmschalten</c> und <c>Form_PVModell</c> - Modell EINFACH
/// sperrt den Wechselrichterknopf und laesst den Wirkungsgrad frei, ERWEITERT umgekehrt;
/// leer heisst NULL; die Ueberlagerung schreibt erst mit OK.
/// </summary>
public class PvModellFelderTests : BunitContext
{
    public PvModellFelderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
    }

    private static ErzeugerZeile Zeile(bool erweitert = false)
        => new() { Schluessel = 1, Bezeichner = "Modul 400", GeraetId = 31,
                   Neigung = 30, Azimut = 180, AnzahlModule = 20, ModellErweitert = erweitert };

    private IRenderedComponent<PvModellFelder> Aufbauen(ErzeugerZeile zeile, Action? geaendert = null)
        => Render<PvModellFelder>(p => p
            .Add(x => x.Zeile, zeile)
            .Add(x => x.KwpAnlage, 8.0)
            .Add(x => x.Geaendert, () => geaendert?.Invoke()));

    [Fact]
    public void Modell_Einfach_laesst_den_Wirkungsgrad_frei_und_sperrt_den_Knopf()
    {
        var cut = Aufbauen(Zeile());

        var felder = cut.FindComponents<Zahlenfeld>();
        Assert.True(felder[0].Instance.Aktiv);                               // WR-Wirkungsgrad
        Assert.True(cut.Find(".epos-pvmodell-wechselrichter").HasAttribute("disabled"));
        Assert.False(cut.Instance.WechselrichterOffen);
    }

    [Fact]
    public async Task Umschalten_auf_Erweitert_sperrt_den_Wirkungsgrad_und_meldet()
    {
        int gemeldet = 0;
        var zeile = Zeile();
        var cut = Aufbauen(zeile, () => gemeldet++);

        var wahl = cut.FindComponent<Auswahlfeld>();
        await cut.InvokeAsync(() => wahl.Instance.AuswahlChanged.InvokeAsync(1));

        Assert.True(zeile.ModellErweitert);
        Assert.Equal(1, gemeldet);
        Assert.False(cut.FindComponents<Zahlenfeld>()[0].Instance.Aktiv);
        Assert.False(cut.Find(".epos-pvmodell-wechselrichter").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Leer_und_Null_heissen_NULL_ein_Wert_wird_uebernommen()
    {
        var zeile = Zeile();
        var cut = Aufbauen(zeile);
        var felder = cut.FindComponents<Zahlenfeld>();

        await cut.InvokeAsync(() => felder[0].Instance.WertChanged.InvokeAsync(0.97));
        await cut.InvokeAsync(() => felder[1].Instance.WertChanged.InvokeAsync(0));

        Assert.Equal(0.97, zeile.WrWirkungsgrad);
        Assert.Null(zeile.Systemverluste);
    }

    [Fact]
    public async Task Die_Ueberlagerung_schreibt_erst_mit_OK_und_zeigt_DC_zu_AC()
    {
        int gemeldet = 0;
        var zeile = Zeile(erweitert: true);
        var cut = Aufbauen(zeile, () => gemeldet++);

        cut.Find(".epos-pvmodell-wechselrichter").Click();
        Assert.True(cut.Instance.WechselrichterOffen);
        Assert.Contains("kein Clipping", cut.Instance.DcAcText, StringComparison.OrdinalIgnoreCase);

        // Die Felder der Ueberlagerung folgen den drei Anlagenfeldern: Nennleistung, eta10/50/100.
        var felder = cut.FindComponents<Zahlenfeld>();
        Assert.True(felder.Count >= 6);
        await cut.InvokeAsync(() => felder[2].Instance.WertChanged.InvokeAsync(10.0));
        await cut.InvokeAsync(() => felder[3].Instance.WertChanged.InvokeAsync(0.9));
        Assert.Contains("0,80", cut.Instance.DcAcText);                       // 8 kWp auf 10 kW
        Assert.Null(zeile.WrNennleistungKw);                                    // noch nicht geschrieben

        await cut.InvokeAsync(() => cut.FindComponent<EPOS.UI.Bausteine.SpeichernLeiste>()
                                       .Instance.Ergebnis.InvokeAsync(true));

        Assert.Equal(10.0, zeile.WrNennleistungKw);
        Assert.Equal(0.9, zeile.WrEta10);
        Assert.Null(zeile.WrEta50);
        Assert.Equal(1, gemeldet);
        Assert.False(cut.Instance.WechselrichterOffen);
    }
}
