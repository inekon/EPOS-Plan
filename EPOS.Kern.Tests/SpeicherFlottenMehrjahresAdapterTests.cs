using System;
using System.Collections.Generic;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests;

public sealed class SpeicherFlottenMehrjahresAdapterTests
{
    [Fact]
    public void Einzelstudie_Reicht_Endenergie_Chronologisch_Ins_Folgejahr_Weiter()
    {
        var config = Konfiguration(projektjahre: 2);
        config.Einheiten[0].SocStart = 1;
        var jahr1 = Jahr(2025, lastKwImErstenIntervall: 20);
        var jahr2 = Jahr(2026, lastKwImErstenIntervall: 4);

        SpeicherFlottenErgebnis ergebnis = SpeicherFlottenStudieCtrl.Rechnen(
            Vorbereitung(config, jahr1, jahr2), null, null, default);

        Assert.Equal(0, ergebnis.Studie.Variante.SpeicherKennzahlen[0].EndenergieKWh, 10);
        FlottenJahreskonto folgejahr = ergebnis.Studie.Wirtschaftlichkeit.Jahreskonten[1];
        Assert.Equal(folgejahr.Referenzrechnung.GesamtEuro, folgejahr.Variantenrechnung.GesamtEuro, 10);
        Assert.Equal(ergebnis.Studie.ReferenzOhneSpeicher.DatenId, ergebnis.Studie.Variante.DatenId);
        Assert.EndsWith(":2025", ergebnis.Studie.Variante.DatenId, StringComparison.Ordinal);
        Assert.Equal(ergebnis.Studie.ReferenzOhneSpeicher.KonfigurationId,
            ergebnis.Studie.Variante.KonfigurationId);
    }

    [Fact]
    public void Einzelstudie_Verwendet_Verfuegbarkeit_Des_Jeweiligen_Projektjahrs()
    {
        var config = Konfiguration(projektjahre: 2);
        config.Einheiten[0].SocStart = 1;
        var jahr1 = Jahr(2025, lastKwImErstenIntervall: 4);
        jahr1.VerfuegbarkeitsfaktorNachEinheitId["s1"] =
            new List<double>(new double[jahr1.Istwerte.Count]);
        var jahr2 = Jahr(2026);

        SpeicherFlottenErgebnis ergebnis = SpeicherFlottenStudieCtrl.Rechnen(
            Vorbereitung(config, jahr1, jahr2), null, null, default);

        Assert.Equal(5, ergebnis.Studie.Variante.SpeicherKennzahlen[0].EndenergieKWh, 10);
        Assert.Equal(ergebnis.Studie.Referenzrechnung.GesamtEuro,
            ergebnis.Studie.Variantenrechnung.GesamtEuro, 10);
    }

    [Fact]
    public void Einzelstudie_Lehnt_Luecken_Und_Fehlende_Finanzjahre_Klar_Ab()
    {
        var config = Konfiguration(projektjahre: 2);
        StromspeicherOptimierungVorbereitung luecke = Vorbereitung(config,
            new FlottenProjektjahr { Jahr = 2025 }, new FlottenProjektjahr { Jahr = 2027 });
        Assert.Contains("fehlt das Jahr 2026", Assert.Throws<ArgumentException>(() =>
            SpeicherFlottenStudieCtrl.Rechnen(luecke, null, null, default)).Message);

        StromspeicherOptimierungVorbereitung zuKurz = Vorbereitung(config,
            new FlottenProjektjahr { Jahr = 2025 });
        Assert.Contains("aber 1 vollständige Projektjahre", Assert.Throws<ArgumentException>(() =>
            SpeicherFlottenStudieCtrl.Rechnen(zuKurz, null, null, default)).Message);

        StromspeicherOptimierungVorbereitung ohneJahresdaten = Vorbereitung(config);
        Assert.Contains("nur ein Referenzjahr", Assert.Throws<ArgumentException>(() =>
            SpeicherFlottenStudieCtrl.Rechnen(ohneJahresdaten, null, null, default)).Message);
    }

    private static StromspeicherOptimierungVorbereitung Vorbereitung(
        FlottenStudieKonfiguration config, params FlottenProjektjahr[] jahre)
    {
        var zeit = DateTimeOffset.Parse("2025-01-01T00:00:00Z");
        return new StromspeicherOptimierungVorbereitung
        {
            Basis = new SpeicherParameter { CNomKwh = 5, PKw = 20, SoCMaxKwh = 5 },
            Eingang = new SpeicherEingang(new[] { 0d }, new[] { 0d }, new[] { 20d }),
            ZeitstempelUtc = new[] { zeit },
            Eingaben = new SpeicherOptimierungEingaben
            {
                Auslegung = new SpeicherAuslegungKonfiguration
                {
                    Flotte = config,
                    FlottenProjektjahre = new List<FlottenProjektjahr>(jahre)
                }
            }
        };
    }

    private static FlottenStudieKonfiguration Konfiguration(int projektjahre) => new()
    {
        Einheiten = new List<FlottenEinheit>
        {
            new()
            {
                Id = "s1", Name = "Speicher 1", KapazitaetKWh = 5,
                LadeleistungKw = 20, EntladeleistungKw = 20,
                Ladewirkungsgrad = 1, Entladewirkungsgrad = 1,
                SocMin = 0, SocMax = 1
            }
        },
        Optionen = new FlottenSimulationOptionen
        {
            Betriebsziel = FlottenBetriebsziel.PvGreedy,
            EnergieAusgleichEuroProKWh = 0
        },
        Wirtschaftlichkeit = new FlottenWirtschaftlichkeitEingang
        {
            ProjektjahreBeiWiederholung = projektjahre
        }
    };

    private static FlottenProjektjahr Jahr(int jahr, double lastKwImErstenIntervall = 0)
    {
        DateTimeOffset[] achse = SpeicherFlottenStudieCtrl.ModellZeitachse(jahr, 35040);
        var werte = new List<FlottenNetzintervall>(achse.Length);
        for (int i = 0; i < achse.Length; i++)
            werte.Add(new FlottenNetzintervall
            {
                Zeitstempel = achse[i],
                LastKw = i == 0 ? lastKwImErstenIntervall : 0,
                BezugspreisEuroProKWh = .20
            });
        return new FlottenProjektjahr { Jahr = jahr, Istwerte = werte };
    }
}
