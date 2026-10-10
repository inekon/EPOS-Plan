using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Komponente <see cref="KaeltemaschineKonfiguration"/></b> (Welle KB-B, Entwurf Kältebereich 3.3): die
/// Betriebseingaben einer Kältemaschine, herausgelöst aus dem Dialog „Kältemaschinen im Projekt“ und dort wie im
/// Bereich „Kälte“ der Simulationskonfiguration eingebunden. Sie schreibt in den Feldsatz des Wirts und meldet jede
/// Eingabe; geprüft und gespeichert wird im OK-Weg des Wirts.
/// </summary>
public class KaeltemaschineKonfigurationTests : EposBunitContext
{
    private const int STROM = 1;
    private const int OEKOSTROM = 2;
    private int _gemeldet;

    public KaeltemaschineKonfigurationTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static KaeltemaschineAnlageDaten Satz(bool teillast = true, int? traeger = STROM) => new()
    {
        AnlagenId = 7, Bezeichner = "KM Bestand", Anzahl = 1, KuehlCarrierId = traeger,
        Geraet = new KaeltemaschineGeraetwerte
        {
            Bezeichner = "KM-Typ 150", Nennkaelteleistung = 150, NennEer = 3.25, Rueckkuehlart = "Trockenkühler",
            KaltwasserMin = 5, TeillastGesetzt = teillast, Kaltwasserstuetzstellen = new[] { 6.0, 8.0 }
        }
    };

    private IRenderedComponent<KaeltemaschineKonfiguration> Zeige(KaeltemaschineAnlageDaten d, bool aktiv = true,
                                                                  int anlagenJeKopie = 1, bool geraet = false)
        => Render<KaeltemaschineKonfiguration>(b => b
            .Add(x => x.Daten, d)
            .Add(x => x.Stromtraeger, new List<(int, string)> { (STROM, "Strom"), (OEKOSTROM, "Ökostrom") })
            .Add(x => x.ProjektStromtraeger, STROM)
            .Add(x => x.Aktiv, aktiv)
            .Add(x => x.AnlagenJeKopie, anlagenJeKopie)
            .Add(x => x.GeraetZeigen, geraet)
            .Add(x => x.Geaendert, () => _gemeldet++));

    private static IElement Feld(IRenderedComponent<KaeltemaschineKonfiguration> cut, string bezeichnung)
        => cut.FindAll("label").First(l => l.TextContent.Trim().StartsWith(bezeichnung, StringComparison.Ordinal))
              .QuerySelector("input, select")
           ?? cut.FindAll(".epos-feld").First(f => f.TextContent.Trim().StartsWith(bezeichnung, StringComparison.Ordinal))
                 .QuerySelector("input, select")!;

    [Fact]
    public void Die_Eingaben_schreiben_in_den_Feldsatz_und_melden_sich()
    {
        KaeltemaschineAnlageDaten d = Satz();
        var cut = Zeige(d);

        Feld(cut, R.KMA_LBL_NAME).Input("KM Nord");
        Feld(cut, R.KMA_LBL_ANZAHL).Input("3");
        Feld(cut, R.KMA_LBL_VORLAUF).Input("9");
        Feld(cut, R.KMA_LBL_HILFSSTROM).Input("4");

        Assert.Equal("KM Nord", d.Bezeichner);
        Assert.Equal(3, d.Anzahl);
        Assert.Equal(9.0, d.KuehlVorlauf);
        Assert.Equal(0.04, d.KuehlHilfsstromanteil!.Value, 10);
        Assert.Equal(4, _gemeldet);
    }

    [Fact]
    public void Die_Folgeschaltung_steht_erst_ab_zwei_Maschinen_mit_Teillastrechnung()
    {
        KaeltemaschineAnlageDaten d = Satz();
        var cut = Zeige(d);
        string folge = string.Format(R.KMA_HINWEIS_FOLGESCHALTUNG, 2);
        Assert.DoesNotContain(folge, cut.Markup);

        Feld(cut, R.KMA_LBL_ANZAHL).Input("2");
        Assert.Contains(folge, cut.Markup);

        var ohne = Zeige(new KaeltemaschineAnlageDaten { Anzahl = 2, Geraet = new KaeltemaschineGeraetwerte() });
        Assert.DoesNotContain(folge, ohne.Markup);
    }

    [Fact]
    public void Der_Zaehlerhinweis_steht_nur_ohne_abweichenden_Kuehltraeger()
    {
        var gleich = Zeige(Satz(traeger: STROM));
        Assert.Contains(R.KMA_HINWEIS_ZAEHLER, gleich.Markup);

        var abweichend = Zeige(Satz(traeger: OEKOSTROM));
        Assert.DoesNotContain(R.KMA_HINWEIS_ZAEHLER, abweichend.Markup);
    }

    [Fact]
    public void Eine_geteilte_Projektkopie_und_die_Geraetezeile_stehen_nur_auf_Wunsch()
    {
        var schlicht = Zeige(Satz());
        Assert.DoesNotContain("KM-Typ 150", schlicht.Markup);
        Assert.DoesNotContain(string.Format(R.SIMKONF_KAELTE_GILT_FUER, 2), schlicht.Markup);

        var voll = Zeige(Satz(), anlagenJeKopie: 2, geraet: true);
        Assert.Contains(string.Format(R.SIMKONF_KAELTE_GILT_FUER, 2), voll.Markup);
        string geraet = voll.Instance.GeraetZeile!;
        Assert.Contains("KM-Typ 150", geraet);
        Assert.Contains("Trockenkühler", geraet);
        Assert.Contains(geraet, voll.Markup);
    }

    [Fact]
    public void Gesperrt_ist_jedes_Feld_bedienungslos()
    {
        var cut = Zeige(Satz(), aktiv: false);

        Assert.True(Feld(cut, R.KMA_LBL_NAME).HasAttribute("readonly"));
        foreach (IElement e in cut.FindAll("input[type=number], select, input[type=checkbox]"))
            Assert.True(e.HasAttribute("disabled"), e.OuterHtml);
    }

    [Fact]
    public void Prozent_und_Anteil_rechnen_hin_und_zurueck()
    {
        Assert.Equal(5.0, KaeltemaschineKonfiguration.AnteilAlsProzent(0.05));
        Assert.Equal(0.05, KaeltemaschineKonfiguration.ProzentAlsAnteil(5.0));
        Assert.Null(KaeltemaschineKonfiguration.AnteilAlsProzent(null));
        Assert.Equal(KaeltemaschineKonfiguration.AnteilAlsProzent(0.123), KaeltemaschineAnlageDialog.AnteilAlsProzent(0.123));
    }
}
