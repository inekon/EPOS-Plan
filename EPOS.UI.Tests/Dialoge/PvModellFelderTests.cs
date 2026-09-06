using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Standards;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Baustein <c>PvModellFelder</c> (Paket A/B des PV-Ertragsmodells, Merge 5): das
/// Rechenmodell, der Wechselrichter-Wirkungsgrad und die Systemverluste.
///
/// <para><b>Was mit Stufe S2 des Wechselrichterkonzepts hier WEGGEFALLEN ist</b>
/// (W6‑E‑2, Entscheidungsfrage Q5): der gesperrte Knopf „Wechselrichter…" und die
/// Überlagerung dahinter. Die Sperrregel <c>disabled="@(!Zeile.ModellErweitert)"</c>
/// war der Anlass des Anwenderwunsches — ein gesperrter Knopf ohne sichtbaren Grund
/// liest sich als Fehler. Die Überlagerung selbst ist nicht weg, sie steht jetzt im
/// Abschnitt „Wechselrichter und Stränge" (<see cref="PvStraengeFelderTests"/>), wo
/// alles zum Wechselrichter steht.</para>
///
/// <para>Was BLEIBT und hier geprüft wird: Der Wirkungsgrad ist im Modell EINFACH frei
/// und in ERWEITERT gesperrt (dort rechnet die Kennlinie), leer heisst NULL, und die
/// Zeile unter dem Rechenmodell sagt, was die Modellwahl unterscheidet — und was
/// nicht.</para>
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
            .Add(x => x.Geaendert, () => geaendert?.Invoke()));

    [Fact]
    public void Modell_Einfach_laesst_den_Wirkungsgrad_frei()
    {
        var cut = Aufbauen(Zeile());

        var felder = cut.FindComponents<Zahlenfeld>();
        Assert.True(felder[0].Instance.Aktiv);                               // WR-Wirkungsgrad
    }

    /// <summary>
    /// <b>Der gesperrte Knopf ist fort</b> (Q5) — und mit ihm die Sperrregel. Der
    /// Baustein zeichnet keinen Knopf mehr; der Wechselrichter steht im eigenen
    /// Abschnitt und ist in BEIDEN Modellen bedienbar.
    /// </summary>
    [Fact]
    public void Der_gesperrte_Wechselrichterknopf_ist_fort()
    {
        var cut = Aufbauen(Zeile());

        Assert.Empty(cut.FindAll(".epos-pvmodell-wechselrichter"));
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

    /// <summary>
    /// <b>Die Modellzeile</b> (Konzept 7): Sie nennt, was sich zwischen den zwei
    /// Modellen unterscheidet — und sagt im SELBEN Satz, dass der Wechselrichter davon
    /// nicht betroffen ist. Ohne diese Zeile bliebe die Wahl eine Ratefrage, und die
    /// Aussage von Q5 stünde nirgends in der Maske.
    /// </summary>
    [Fact]
    public async Task Die_Modellzeile_nennt_beide_Modelle_und_den_Wechselrichter()
    {
        var zeile = Zeile();
        var cut = Aufbauen(zeile);

        Assert.Contains("Einfach", cut.Instance.Modellzeile, StringComparison.Ordinal);
        Assert.Contains("in beiden Modellen", cut.Instance.Modellzeile, StringComparison.Ordinal);
        Assert.Contains("in beiden Modellen", cut.Find(".epos-pvmodell-zeile").TextContent,
                        StringComparison.Ordinal);

        var wahl = cut.FindComponent<Auswahlfeld>();
        await cut.InvokeAsync(() => wahl.Instance.AuswahlChanged.InvokeAsync(1));

        Assert.Contains("Hay-Davies", cut.Instance.Modellzeile, StringComparison.Ordinal);
        Assert.Contains("in beiden Modellen", cut.Instance.Modellzeile, StringComparison.Ordinal);
    }
}
