using System;
using System.Collections.Generic;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests;

public sealed class SpeicherFlottenProjektCtrlTests
{
    [Fact]
    public void Flottenkontext_BrauchtKeineEinzelanlage_und_aggregiert_physischeEinheiten()
    {
        FlottenStudieKonfiguration flotte = Konfiguration();
        flotte.Einheiten.Add(new FlottenEinheit
        {
            Id = "s2", Name = "Speicher 2", KapazitaetKWh = 20,
            LadeleistungKw = 3, EntladeleistungKw = 6,
            Ladewirkungsgrad = .9, Entladewirkungsgrad = .8,
            SocMin = .1, SocStart = .4, SocMax = .9
        });

        StromspeicherOptimierungVorbereitung v = new StromspeicherSimCtrl()
            .BereiteProjektflotteVor(null, 987654321, flotte);

        Assert.Equal(30, v.Basis.CNomKwh, 10);
        Assert.Equal(10, v.Basis.PKw, 10);
        Assert.Equal(2, v.Basis.SoCMinKwh, 10);
        Assert.Equal(8, v.Basis.StartSoCKwh.GetValueOrDefault(), 10);
        Assert.Equal(28, v.Basis.SoCMaxKwh, 10);
        Assert.Equal((10 + 20 * .9 * .8) / 30, v.Basis.RoundTripWirkungsgrad, 10);
        Assert.Equal(0, v.Kontext.ID_Energieanlage);
        Assert.Equal("Speicherflotte", v.Kontext.Bezeichner);
        Assert.Null(v.Eingang);
    }

    [Fact]
    public void Projektlauf_UebernimmtVollstaendigenVorzeichenbehaftetenNetzfluss()
    {
        FlottenEingang input = Eingang(
            new FlottenNetzintervall { LastKw = 0, PvKw = 8, BezugspreisEuroProKWh = .20, PvVerkaufspreisEuroProKWh = .05 },
            new FlottenNetzintervall { LastKw = 8, PvKw = 0, BezugspreisEuroProKWh = .20, PvVerkaufspreisEuroProKWh = .05 });
        FlottenStudieKonfiguration config = Konfiguration();

        SpeicherFlottenProjektLauf lauf = SpeicherFlottenProjektCtrl.Rechnen(input, config, null);

        Assert.Equal(new[] { -4d, 4d }, lauf.NetzleistungKw);
        Assert.Equal(new[] { 1d, 0d }, lauf.Kompatibilitaetsergebnis.LadungAcKwh);
        Assert.Equal(new[] { 0d, 1d }, lauf.Kompatibilitaetsergebnis.EntladungAcKwh);
        Assert.Equal(1d, lauf.Kompatibilitaetsergebnis.Kennzahlen.LadeenergiePvKwh, 10);
        Assert.Equal(.15d, lauf.Kompatibilitaetsergebnis.SummeGeldwertEur, 10);
        Assert.Same(lauf.Studie, lauf.Kontext.Flottenergebnis);
        Assert.NotSame(config, lauf.Konfiguration);
    }

    [Fact]
    public void Kompatibilitaetswert_ZaehltBatterieexportNurEinmal()
    {
        FlottenEingang input = Eingang(new FlottenNetzintervall
        {
            BatterieVerkaufspreisEuroProKWh = 1.50
        });
        FlottenStudienErgebnis studie = new()
        {
            ReferenzOhneSpeicher = Simulation(new FlottenIntervallErgebnis()),
            Variante = Simulation(new FlottenIntervallErgebnis
            {
                NetzleistungKw = -2, NetzeinspeisungKw = 2,
                BatterieNetzeinspeisungKw = 2,
                IstleistungKwJeSpeicher = new List<double> { 2 },
                EnergieEndeKWhJeSpeicher = new List<double> { 0 }
            }),
            Referenzrechnung = new FlottenRechnung(),
            Variantenrechnung = new FlottenRechnung { EnergiekostenEuro = -.75, GesamtEuro = -.75 }
        };
        studie.Variante.SpeicherKennzahlen.Add(new FlottenSpeicherKennzahlen
            { SpeicherId = "s1", EntladeenergieAcKWh = .5 });

        SpeicherErgebnis legacy = StromspeicherSimCtrl.AlsKompatibilitaetsergebnis(
            studie, input, Konfiguration());

        Assert.Equal(.75, legacy.GeldwertEur[0], 10);
        Assert.Equal(.75, legacy.SummeGeldwertEur, 10);
        Assert.Equal(.5, legacy.EntladeenergieKwh, 10);
    }

    [Fact]
    public void Projektquellen_LehnenExterneLastPvUndProjektjahreAb()
    {
        var a = new SpeicherAuslegungKonfiguration
            { Lastquelle = SpeicherAuslegungQuelle.Datei, PvQuelle = SpeicherAuslegungQuelle.Epos };
        Assert.Contains("externe Lastdatei", Assert.Throws<InvalidOperationException>(
            () => SpeicherFlottenProjektCtrl.PruefeProjektquellen(a)).Message);

        a.Lastquelle = SpeicherAuslegungQuelle.Epos;
        a.PvQuelle = SpeicherAuslegungQuelle.Datei;
        Assert.Contains("externe PV-Datei", Assert.Throws<InvalidOperationException>(
            () => SpeicherFlottenProjektCtrl.PruefeProjektquellen(a)).Message);

        a.PvQuelle = SpeicherAuslegungQuelle.Keine;
        a.FlottenProjektjahre.Add(new FlottenProjektjahr());
        Assert.Contains("eigenständigen Studie", Assert.Throws<InvalidOperationException>(
            () => SpeicherFlottenProjektCtrl.PruefeProjektquellen(a)).Message);
    }

    [Fact]
    public void Preisdatei_BrauchtExplizitePassendeModellachse()
    {
        var a = new SpeicherAuslegungKonfiguration
        {
            Lastquelle = SpeicherAuslegungQuelle.Epos,
            PvQuelle = SpeicherAuslegungQuelle.Epos,
            Preisquelle = SpeicherAuslegungQuelle.Datei
        };
        Assert.Contains("Modelljahrzuordnung", Assert.Throws<InvalidOperationException>(
            () => SpeicherFlottenProjektCtrl.PruefeProjektquellen(a)).Message);

        a.EposModelljahrZuordnen = true;
        a.PreisDatei = new SpeicherZeitreihe
        {
            Werte = new double[4], ZeitstempelUtc = new DateTimeOffset[4]
        };
        Assert.Contains("35.040", Assert.Throws<InvalidOperationException>(
            () => SpeicherFlottenProjektCtrl.PruefeProjektquellen(a)).Message);
    }

    [Fact]
    public void ExplizitAktiveUnzulaessigeFlotte_WirdNichtZumAltpfadAbgeschwaecht()
    {
        FlottenEingang input = Eingang(new FlottenNetzintervall { LastKw = 8 });
        FlottenStudieKonfiguration config = Konfiguration();
        config.Optionen.NetzbezugGrenzeKw = 1;

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => SpeicherFlottenProjektCtrl.Rechnen(input, config, null));

        Assert.Contains("unzulässig", ex.Message);
    }

    [Fact]
    public void OptimierteUebernahme_VerlangtEinheitlicheKandidatId()
    {
        const string kandidatId = "kandidat-17";
        FlottenStudieKonfiguration config = Konfiguration();
        FlottenStudienErgebnis studie = new()
        {
            ReferenzOhneSpeicher = Simulation(new FlottenIntervallErgebnis
            {
                NetzleistungKw = 2
            }),
            Variante = Simulation(new FlottenIntervallErgebnis
            {
                NetzleistungKw = 1
            })
        };
        studie.ReferenzOhneSpeicher.KonfigurationId = kandidatId;
        studie.ReferenzOhneSpeicher.DatenId = "daten-4";
        studie.Variante.KonfigurationId = kandidatId;
        studie.Variante.DatenId = "daten-4";

        FlottenAuslegungErgebnis auslegung = new()
        {
            BesterKandidat = new FlottenKandidatZusammenfassung
            {
                KandidatId = kandidatId,
                Zulaessig = true
            },
            BesteZeitreihe = studie.Variante,
            BesteKonfiguration = SpeicherAuslegungKopie.Von(config),
            BesteStudie = studie
        };
        SpeicherFlottenErgebnis ergebnis = new()
        {
            Erfolg = true,
            Eingaben = new SpeicherOptimierungEingaben
            {
                Auslegung = new SpeicherAuslegungKonfiguration
                {
                    Lastquelle = SpeicherAuslegungQuelle.Epos,
                    PvQuelle = SpeicherAuslegungQuelle.Epos,
                    Flotte = SpeicherAuslegungKopie.Von(config)
                }
            },
            Konfiguration = SpeicherAuslegungKopie.Von(config),
            Studie = studie,
            Auslegung = auslegung
        };

        SpeicherFlottenProjektCtrl.PruefeUebernahme(ergebnis);

        auslegung.BesterKandidat.KandidatId = "veralteter-kandidat";
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => SpeicherFlottenProjektCtrl.PruefeUebernahme(ergebnis));
        Assert.Contains("selben Auslegungsstand", ex.Message);
    }

    private static FlottenEingang Eingang(params FlottenNetzintervall[] intervalle)
    {
        DateTimeOffset start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        for (int i = 0; i < intervalle.Length; i++)
        {
            intervalle[i].Zeitstempel = start.AddMinutes(i * 15);
            intervalle[i].BhkwVerkaufspreisEuroProKWh = 0;
        }
        return new FlottenEingang
        {
            KonfigurationId = "config", DatenId = "data",
            Istwerte = new List<FlottenNetzintervall>(intervalle)
        };
    }

    private static FlottenStudieKonfiguration Konfiguration() => new()
    {
        Einheiten = new List<FlottenEinheit>
        {
            new()
            {
                Id = "s1", Name = "Speicher 1", KapazitaetKWh = 10,
                LadeleistungKw = 4, EntladeleistungKw = 4,
                Ladewirkungsgrad = 1, Entladewirkungsgrad = 1,
                SocMin = 0, SocStart = 0, SocMax = 1
            }
        },
        Optionen = new FlottenSimulationOptionen
        {
            Betriebsziel = FlottenBetriebsziel.PvGreedy,
            EnergieAusgleichEuroProKWh = 0
        },
        Wirtschaftlichkeit = new FlottenWirtschaftlichkeitEingang
            { ProjektjahreBeiWiederholung = 1 }
    };

    private static FlottenSimulationErgebnis Simulation(FlottenIntervallErgebnis intervall) => new()
    {
        Zulaessig = true,
        Intervalle = new List<FlottenIntervallErgebnis> { intervall }
    };
}
