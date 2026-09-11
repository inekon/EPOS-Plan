using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using SpeicherEngine;
using Xunit;

namespace SpeicherEngine.Tests;

/// <summary>
/// BEFUND SP‑O‑10 (Anwenderrückmeldung 11.09.2026): „Mit Flotte" war byte-gleich
/// „Ohne Speicher" — die Flotte hatte im ganzen Jahr weder geladen noch entladen, und
/// niemand sagte es. Die Diagnose aus Aufgabe #183 zählt die Sperren mit.
///
/// <para><b>Der Prüfstand.</b> Acht Viertelstunden, die Last springt zwischen 20 kW und
/// 4 kW, das Peak-Ziel steht auf 10 kW, der Speicher startet auf seinem SoC-Minimum.
/// Ohne freigegebene Netzladung ist beides gesperrt: Entladen, weil der Speicher leer
/// ist; Laden, weil es keinen Überschuss gibt (n &gt; 0 in jedem Intervall). Mit
/// freigegebener Netzladung lädt dieselbe Reihe in den Tälern und entlädt in den
/// Spitzen.</para>
/// </summary>
public sealed class FlottenDiagnoseTests : IDisposable
{
    private readonly CultureInfo _vorher = CultureInfo.CurrentCulture;

    public FlottenDiagnoseTests()
    {
        CultureInfo de = new CultureInfo("de-DE");
        CultureInfo.CurrentCulture = de;
        Thread.CurrentThread.CurrentCulture = de;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        CultureInfo.CurrentCulture = _vorher;
        Thread.CurrentThread.CurrentCulture = _vorher;
    }

    [Fact]
    public void OhneNetzladung_MeldetDieDiagnoseEineArbeitsloseFlotteMitAllenZaehlern()
    {
        var config = Config(netzladung: false);

        var diagnose = FlottenSimulator.Simuliere(Reihe(), config).Variante.Diagnose;

        Assert.True(diagnose.Arbeitslos);
        Assert.Equal(0, diagnose.LadeenergieAcKWh, 10);
        Assert.Equal(0, diagnose.EntladeenergieAcKWh, 10);
        Assert.Equal(8, diagnose.IntervalleGesamt);
        Assert.Equal(4, diagnose.IntervalleLastUeberPeakZiel);
        Assert.Equal(4, diagnose.IntervalleMitEntladeanforderung);
        Assert.Equal(4, diagnose.IntervalleMitLadeanforderung);
        Assert.Equal(4, diagnose.IntervalleLadedeckelNullPeakregel);
        Assert.Equal(8, diagnose.IntervalleLadedeckelNullNetzladeverbot);
        Assert.Equal(4, diagnose.IntervalleEntladeanforderungOhneEnergie);
    }

    [Fact]
    public void OhneNetzladung_NenntDieDiagnoseVierGruendeMitIhrenZahlen()
    {
        var diagnose = FlottenSimulator.Simuliere(Reihe(), Config(netzladung: false)).Variante.Diagnose;

        Assert.Equal(new[]
        {
            FlottenDiagnoseGrund.LastUeberPeakZiel,
            FlottenDiagnoseGrund.LadedeckelDurchPeakregel,
            FlottenDiagnoseGrund.LadedeckelDurchNetzladeverbot,
            FlottenDiagnoseGrund.EntladeanforderungOhneEnergie
        }, diagnose.Gruende.Select(x => x.Grund));
        Assert.Equal(8, diagnose.Gruende.Single(x =>
            x.Grund == FlottenDiagnoseGrund.LadedeckelDurchNetzladeverbot).Intervalle);
        Assert.Equal(1.0, diagnose.Gruende.Single(x =>
            x.Grund == FlottenDiagnoseGrund.LadedeckelDurchNetzladeverbot).Anteil, 10);
        Assert.Equal(0.5, diagnose.Gruende.Single(x =>
            x.Grund == FlottenDiagnoseGrund.LastUeberPeakZiel).Anteil, 10);
    }

    [Fact]
    public void OhneNetzladung_IstAuchDieEinheitArbeitslos()
    {
        var diagnose = FlottenSimulator.Simuliere(Reihe(), Config(netzladung: false)).Variante.Diagnose;

        FlottenEinheitDiagnose einheit = Assert.Single(diagnose.Einheiten);
        Assert.Equal("A", einheit.SpeicherId);
        Assert.True(einheit.Arbeitslos);
        Assert.Equal(4, einheit.IntervalleEntladeanforderungOhneEnergie);
        Assert.Equal(0, einheit.IntervalleMitEntladeanforderung);
        // Die Verteilung weist der Einheit sehr wohl eine Ladung zu; erst der Ladedeckel
        // des Netzladeverbots streicht sie wieder.
        Assert.Equal(4, einheit.IntervalleMitLadeanforderung);
    }

    [Fact]
    public void MitNetzladung_ArbeitetDieselbeFlotte()
    {
        var variante = FlottenSimulator.Simuliere(Reihe(), Config(netzladung: true)).Variante;
        var diagnose = variante.Diagnose;

        Assert.False(diagnose.Arbeitslos);
        Assert.Equal(6.0, diagnose.LadeenergieAcKWh, 8);
        Assert.Equal(4.5, diagnose.EntladeenergieAcKWh, 8);
        Assert.Equal(0, diagnose.IntervalleLadedeckelNullNetzladeverbot);
        // Nur das ERSTE Intervall trifft den noch leeren Speicher.
        Assert.Equal(1, diagnose.IntervalleEntladeanforderungOhneEnergie);
        Assert.Equal(4, diagnose.IntervalleMitLadeanforderung);
        Assert.False(Assert.Single(diagnose.Einheiten).Arbeitslos);
        Assert.DoesNotContain(diagnose.Gruende,
            x => x.Grund == FlottenDiagnoseGrund.LadedeckelDurchNetzladeverbot);
    }

    [Fact]
    public void DerReferenzlaufOhneSpeicher_TraegtEineLeereDiagnose()
    {
        var diagnose = FlottenSimulator.Simuliere(Reihe(), Config(netzladung: false))
            .ReferenzOhneSpeicher.Diagnose;

        Assert.Equal(0, diagnose.IntervalleGesamt);
        Assert.False(diagnose.Arbeitslos);
        Assert.Empty(diagnose.Gruende);
        Assert.Empty(diagnose.Einheiten);
    }

    [Fact]
    public void OhneAnforderung_MeldetDieDiagnoseBeideLeerlaufgruende()
    {
        // PV-Eigenverbrauch ohne Last und ohne Erzeugung: n = 0, also weder Lade- noch
        // Entladeanforderung.
        var einheit = Einheit();
        var config = new FlottenStudieKonfiguration
        {
            Einheiten = new List<FlottenEinheit> { einheit },
            Optionen = new FlottenSimulationOptionen
            {
                Betriebsziel = FlottenBetriebsziel.PvGreedy,
                EnergieAusgleichEuroProKWh = 0
            }
        };

        var diagnose = FlottenSimulator.Simuliere(
            Eingang(new[] { Zeile(0, 0.0), Zeile(1, 0.0) }), config).Variante.Diagnose;

        Assert.True(diagnose.Arbeitslos);
        Assert.Equal(new[]
        {
            // n = 0 ist kein Ueberschuss: Das Netzladeverbot deckelt auch hier auf 0.
            FlottenDiagnoseGrund.LadedeckelDurchNetzladeverbot,
            FlottenDiagnoseGrund.KeineEntladeanforderung,
            FlottenDiagnoseGrund.KeineLadeanforderung
        }, diagnose.Gruende.Select(x => x.Grund));
        Assert.All(diagnose.Gruende, x => Assert.Equal(2, x.Intervalle));
    }

    // ================================================================= Prüfstand

    /// <summary>Acht Viertelstunden, abwechselnd 20 kW und 4 kW Last, keine Erzeugung.</summary>
    private static FlottenEingang Reihe() => Eingang(Enumerable.Range(0, 8)
        .Select(t => Zeile(t, t % 2 == 0 ? 20.0 : 4.0)).ToArray());

    private static FlottenEingang Eingang(FlottenNetzintervall[] zeilen) => new()
    {
        KonfigurationId = "C",
        DatenId = "D",
        Istwerte = new List<FlottenNetzintervall>(zeilen)
    };

    private static FlottenNetzintervall Zeile(int viertelstunde, double last) => new()
    {
        Zeitstempel = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMinutes(15 * viertelstunde),
        LastKw = last,
        BezugspreisEuroProKWh = 0.3,
        PvVerkaufspreisEuroProKWh = 0.08,
        BhkwVerkaufspreisEuroProKWh = 0.12,
        BatterieVerkaufspreisEuroProKWh = 0.05
    };

    /// <summary>Lastspitzenkappung gegen 10 kW; der Speicher startet auf seinem SoC-Minimum.</summary>
    private static FlottenStudieKonfiguration Config(bool netzladung) => new()
    {
        Einheiten = new List<FlottenEinheit> { Einheit() },
        Optionen = new FlottenSimulationOptionen
        {
            Betriebsziel = FlottenBetriebsziel.PeakShaving,
            WirtschaftlicherPeakZielwertKw = 10,
            NetzladungErlaubt = netzladung,
            EnergieAusgleichEuroProKWh = 0
        }
    };

    private static FlottenEinheit Einheit() => new()
    {
        Id = "A",
        Name = "Speicher A",
        KapazitaetKWh = 10,
        LadeleistungKw = 10,
        EntladeleistungKw = 10,
        Ladewirkungsgrad = 1,
        Entladewirkungsgrad = 1,
        SocMin = 0.2,
        SocMax = 1.0,
        SocStart = 0.2
    };
}
