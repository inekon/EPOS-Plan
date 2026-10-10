using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Strom;
using KiKern;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten.Strom;

/// <summary>
/// Der Assistent setzt ein Feld der GEZEICHNETEN Stromspeicher-Ansicht — und die Ansicht geht
/// danach denselben Weg wie nach einer Eingabe von Hand: Fassung, Ungespeichert-Marke und
/// beim Betriebsziel die Folgen des Betriebseditors (Netzladung, Ratsche, Peak-Vorschlag).
/// </summary>
public sealed class StromspeicherKiSetzenTests : EposBunitContext, IDisposable
{
    private readonly Func<bool> _schreibrechtVorher = Schreibnaht.Schreibrecht;

    public StromspeicherKiSetzenTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        Schreibnaht.Schreibrecht = Schreibnaht.ImmerErlaubt;
    }

    public new void Dispose()
    {
        Schreibnaht.Schreibrecht = _schreibrechtVorher;
        base.Dispose();
    }

    [Fact]
    public async Task Ein_neues_Betriebsziel_zieht_die_Folgen_wie_der_Betriebseditor()
    {
        FlottenStudieKonfiguration flotte = Flotte(FlottenBetriebsziel.PvGreedy);
        int vorschlaege = 0;
        var cut = Ansicht(new StromspeicherAuslegungDienste
        {
            Vorgaben = () => Vorgaben(flotte),
            PeakZielVorschlag = _ => { vorschlaege++; return null!; }
        });

        int fassung = cut.Instance.Fassung;
        int vorschlaegeVorher = vorschlaege;
        Assert.False(cut.Instance.Ungespeichert);

        KiErgebnis ergebnis = await cut.InvokeAsync(() => Setzen("betriebsziel",
            nameof(FlottenBetriebsziel.PeakShaving)));

        Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
        FlottenSimulationOptionen o = cut.Instance.Eingaben.Auslegung!.Flotte!.Optionen;
        Assert.Equal(FlottenBetriebsziel.PeakShaving, o.Betriebsziel);
        Assert.Equal(FlottenVorgaben.NetzladungFuer(FlottenBetriebsziel.PeakShaving), o.NetzladungErlaubt);
        Assert.Equal(FlottenVorgaben.PeakZielAdaptivFuer(FlottenBetriebsziel.PeakShaving), o.PeakZielAdaptiv);
        Assert.True(cut.Instance.Fassung > fassung);
        Assert.True(cut.Instance.Ungespeichert);
        Assert.True(vorschlaege > vorschlaegeVorher);
    }

    [Fact]
    public async Task Ein_Wert_ausserhalb_der_Betriebsfuehrung_meldet_wie_von_Hand()
    {
        FlottenStudieKonfiguration flotte = Flotte(FlottenBetriebsziel.PeakShaving);
        var cut = Ansicht(new StromspeicherAuslegungDienste { Vorgaben = () => Vorgaben(flotte) });

        int fassung = cut.Instance.Fassung;
        bool netzladung = cut.Instance.Eingaben.Auslegung!.Flotte!.Optionen.NetzladungErlaubt;

        KiErgebnis ergebnis = await cut.InvokeAsync(() => Setzen("netzbezug_grenze", "80"));

        Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
        FlottenSimulationOptionen o = cut.Instance.Eingaben.Auslegung!.Flotte!.Optionen;
        Assert.Equal(80, o.NetzbezugGrenzeKw);
        Assert.Equal(netzladung, o.NetzladungErlaubt);
        Assert.Equal(fassung + 1, cut.Instance.Fassung);
        Assert.True(cut.Instance.Ungespeichert);
    }

    // =====================================================================
    //  Hilfen
    // =====================================================================

    private static FlottenStudieKonfiguration Flotte(FlottenBetriebsziel ziel) => new()
    {
        Einheiten = new List<FlottenEinheit>
        {
            new()
            {
                Id = "a", Name = "A", KapazitaetKWh = 24, LadeleistungKw = 10, EntladeleistungKw = 12,
                Ladewirkungsgrad = 0.95, Entladewirkungsgrad = 0.95,
                SocMin = 0.1, SocMax = 0.9, SocStart = 0.1
            }
        },
        Optionen = new()
        {
            Betriebsziel = ziel,
            NetzladungErlaubt = FlottenVorgaben.NetzladungFuer(ziel),
            PeakZielAdaptiv = FlottenVorgaben.PeakZielAdaptivFuer(ziel),
            WirtschaftlicherPeakZielwertKw = 50
        },
        Wirtschaftlichkeit = new() { ProjektjahreBeiWiederholung = 1 }
    };

    private static SpeicherOptimierungVorgaben Vorgaben(FlottenStudieKonfiguration flotte) => new()
    {
        Eingaben = new SpeicherOptimierungEingaben
        { Auslegung = new SpeicherAuslegungKonfiguration { Flotte = flotte } }
    };

    private IRenderedComponent<StromspeicherAuslegungSeite> Ansicht(StromspeicherAuslegungDienste dienste)
        => Render<StromspeicherAuslegungSeite>(p => p
            .Add(x => x.Dienste, dienste)
            .Add(x => x.PlanerVerfuegbar, true));

    /// <summary>Vorbereiten, freigeben, ausführen — der ganze Weg einer Feldsetzung.</summary>
    private static async Task<KiErgebnis> Setzen(string feld, string wert)
    {
        var schicht = new KiAusfuehrung { Schreibrecht = () => true };
        var werte = new Dictionary<string, object?>
        {
            ["maske"] = KiMaskennamen.STROMSPEICHER_AUSLEGUNG, ["feld"] = feld, ["wert"] = wert
        };
        KiPruefErgebnis geprueft = KiPruefung.Pruefe(schicht.Register, "feld_setzen", werte);
        Assert.True(geprueft.Gueltig, geprueft.FehlerText());

        KiVorbereitung vorbereitung = await schicht.VorbereitenAsync(geprueft.Aufruf, CancellationToken.None);
        if (vorbereitung.Freigabe is null) return vorbereitung.Ablehnung;

        vorbereitung.Freigabe.Erteilen();
        return await schicht.AusfuehrenAsync(geprueft.Aufruf, vorbereitung.Freigabe, CancellationToken.None);
    }
}
