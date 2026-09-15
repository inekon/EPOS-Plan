using System;
using System.Collections.Generic;
using System.Reflection;
using SpeicherEngine;
using Xunit;

namespace SpeicherEngine.Tests;

public sealed class FlottenWirtschaftlichkeitTests
{
    [Fact]
    public void Kapitalwert_VerwendetCapexCashflowsErsatzUndRestwertGenauEinmal()
    {
        var unit = new FlottenEinheit
        {
            Id = "A", Name = "A", KapazitaetKWh = 10, LadeleistungKw = 4, EntladeleistungKw = 4,
            InvestitionEuro = 100, InvestitionEuroProKWh = 50, InvestitionEuroProKw = 100,
            RestwertEuro = 40
        };
        var result = FlottenWirtschaftlichkeit.Bewerte(new FlottenWirtschaftlichkeitEingang
        {
            Einheiten = new List<FlottenEinheit> { unit },
            Kalkulationszins = 0,
            RestwertEuro = 60,
            Jahreskonten = new List<FlottenJahreskonto>
            {
                Konto(1, 300),
                Konto(2, 250)
            }
        });

        // CAPEX 100+500+400=1000; Cashflows 550; Restwert 100.
        Assert.Equal(-350, result.KapitalwertEuro, 8);
        Assert.Equal(1000, result.InvestitionEuro, 8);
        Assert.Equal(new[] { 300d, 250d }, result.JahresCashflowsEuro);
    }

    [Fact]
    public void Teiljahr_WirdNichtAlsJahreskontoAkzeptiert()
    {
        var x = new FlottenWirtschaftlichkeitEingang
        {
            Jahreskonten = new List<FlottenJahreskonto> { Konto(1, 10, false) }
        };
        Assert.Throws<ArgumentException>(() => FlottenWirtschaftlichkeit.Bewerte(x));
    }

    [Fact]
    public void Jahrescashflow_WirdAusEinerGemeinsamenRechnungsdifferenzNeuGebildet()
    {
        var account = Konto(1, 100);
        account.NettoCashflowEuro = 9999; // Darf nicht als zweite, unabhaengige Einsparung eingehen.
        account.OpexEuro = 30;
        var result = FlottenWirtschaftlichkeit.Bewerte(new FlottenWirtschaftlichkeitEingang
        {
            Jahreskonten = new List<FlottenJahreskonto> { account }
        });

        Assert.Equal(70, result.JahresCashflowsEuro[0], 8);
        Assert.Equal(70, result.KapitalwertEuro, 8);
    }

    [Fact]
    public void ReferenzjahrWiederholung_IstExplizitGekennzeichnet_UndBuchtErsatzImFaelligenJahr()
    {
        var unit = new FlottenEinheit { Id = "A", Name = "A", ErsatzintervallJahre = 2, ErsatzkostenEuro = 50 };
        var result = FlottenWirtschaftlichkeit.Bewerte(new FlottenWirtschaftlichkeitEingang
        {
            Einheiten = new List<FlottenEinheit> { unit },
            Jahreskonten = new List<FlottenJahreskonto> { Konto(1, 100) },
            ReferenzjahrExplizitWiederholen = true,
            ProjektjahreBeiWiederholung = 3
        });

        Assert.True(result.IstWiederholteReferenzjahrProjektion);
        Assert.Equal(50, result.Jahreskonten[1].ErsatzkostenEuro);
        Assert.Equal(50, result.Jahreskonten[1].NettoCashflowEuro);
        Assert.All(result.Jahreskonten, x => Assert.Contains("wiederholte", x.Projektionskennzeichnung));
    }

    [Fact]
    public void Rainflow_GeschlossenerHubErgibtEinenVollzyklusUndMinerSchaden()
    {
        var result = FlottenRainflow.Auswerten(new[] { 0d, 1d, 0d },
            new[] { new FlottenRainflowPunkt { Entladetiefe = 1, ZyklenBisEol = 1000 } });

        Assert.Equal(2, result.Zyklen.Count);
        Assert.Equal(1, result.Zyklen[0].Anzahl + result.Zyklen[1].Anzahl, 8);
        Assert.Equal(0.001, result.Schaden, 10);
    }

    [Fact]
    public void Rainflow_ExtrapoliertLebensdauerkurveNicht()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FlottenRainflow.Auswerten(
            new[] { 0d, 0.1, 0d },
            new[] { new FlottenRainflowPunkt { Entladetiefe = 0.2, ZyklenBisEol = 10000 } }));
    }

    // =====================================================================
    //  Die Gueltigkeit der Lebensdauerkurve (Auftrag #257)
    // =====================================================================

    /// <summary>
    /// EINE GUELTIGE KURVE und eine LEERE Kurve sind beide in Ordnung: Ohne Punkte
    /// zaehlt die Auswertung Zyklen und rechnet keinen Miner-Schaden.
    /// </summary>
    [Fact]
    public void PruefeKurve_NimmtEineGueltigeUndEineLeereKurveAn()
    {
        Assert.Null(FlottenRainflow.PruefeKurve(new[]
        {
            new FlottenRainflowPunkt { Entladetiefe = 1, ZyklenBisEol = 1000 },
            new FlottenRainflowPunkt { Entladetiefe = 0.5, ZyklenBisEol = 4000 }
        }));
        Assert.Null(FlottenRainflow.PruefeKurve(new FlottenRainflowPunkt[0]));
        Assert.Null(FlottenRainflow.PruefeKurve(null));
    }

    /// <summary>
    /// JEDER EINZELNE GRUND mit seinem eigenen Namen. Der frisch angelegte Punkt 0/0
    /// heisst NICHT AUSGEFUELLT und nicht Wertebereichsfehler - er ist ein leeres
    /// Feldpaar, kein falscher Wert.
    /// </summary>
    [Theory]
    [InlineData(0d, 0d, FlottenRainflowMangel.NichtAusgefuellt)]
    [InlineData(0d, 1000d, FlottenRainflowMangel.EntladetiefeAusserhalb)]
    [InlineData(1.5, 1000d, FlottenRainflowMangel.EntladetiefeAusserhalb)]
    [InlineData(double.NaN, 1000d, FlottenRainflowMangel.EntladetiefeAusserhalb)]
    [InlineData(0.5, 0d, FlottenRainflowMangel.ZyklenNichtPositiv)]
    [InlineData(0.5, -3d, FlottenRainflowMangel.ZyklenNichtPositiv)]
    [InlineData(0.5, double.NaN, FlottenRainflowMangel.ZyklenNichtPositiv)]
    public void PruefeKurve_NenntDenGrundJedesMangels(double tiefe, double zyklen,
        FlottenRainflowMangel erwartet)
    {
        FlottenRainflowBefund? befund = FlottenRainflow.PruefeKurve(new[]
        {
            new FlottenRainflowPunkt { Entladetiefe = tiefe, ZyklenBisEol = zyklen }
        });

        Assert.NotNull(befund);
        Assert.Equal(0, befund.Index);
        Assert.Equal(erwartet, befund.Mangel);
    }

    /// <summary>
    /// EINE ENTLADETIEFE NUR EINMAL: Gemeldet wird der ZWEITE Punkt - der erste stand
    /// schon da, der zweite ist der, den der Anwender wegnehmen oder aendern muss.
    /// </summary>
    [Fact]
    public void PruefeKurve_MeldetDenZweitenPunktMitDerselbenEntladetiefe()
    {
        FlottenRainflowBefund? befund = FlottenRainflow.PruefeKurve(new[]
        {
            new FlottenRainflowPunkt { Entladetiefe = 1, ZyklenBisEol = 1000 },
            new FlottenRainflowPunkt { Entladetiefe = 0.5, ZyklenBisEol = 4000 },
            new FlottenRainflowPunkt { Entladetiefe = 0.5, ZyklenBisEol = 5000 }
        });

        Assert.NotNull(befund);
        Assert.Equal(2, befund.Index);
        Assert.Equal(FlottenRainflowMangel.EntladetiefeDoppelt, befund.Mangel);
    }

    /// <summary>
    /// DER INDEX ZAEHLT DIE GELIEFERTE LISTE, nicht die sortierte: Der Editor zeigt
    /// genau diese Reihenfolge, und der Befund soll auf die Zeile zeigen, die der
    /// Anwender vor sich hat.
    /// </summary>
    [Fact]
    public void PruefeKurve_ZaehltDieGelieferteReihenfolge()
    {
        FlottenRainflowBefund? befund = FlottenRainflow.PruefeKurve(new[]
        {
            new FlottenRainflowPunkt { Entladetiefe = 1, ZyklenBisEol = 1000 },
            new FlottenRainflowPunkt()
        });

        Assert.NotNull(befund);
        Assert.Equal(1, befund.Index);
        Assert.Equal(FlottenRainflowMangel.NichtAusgefuellt, befund.Mangel);
    }

    /// <summary>
    /// AUSWERTEN WIRFT UNVERAENDERT - derselbe Ausnahmetyp, derselbe Satz. Die
    /// Bedingung ist nur herausgeloest, nicht geaendert.
    /// </summary>
    [Fact]
    public void Rainflow_WirftFuerEineUngueltigeKurveWieBisher()
    {
        ArgumentException leer = Assert.Throws<ArgumentException>(() => FlottenRainflow.Auswerten(
            new[] { 0d, 1d, 0d }, new[] { new FlottenRainflowPunkt() }));
        Assert.Contains("Rainflow-Kurve ist ungueltig.", leer.Message);

        ArgumentException doppelt = Assert.Throws<ArgumentException>(() => FlottenRainflow.Auswerten(
            new[] { 0d, 1d, 0d }, new[]
            {
                new FlottenRainflowPunkt { Entladetiefe = 1, ZyklenBisEol = 1000 },
                new FlottenRainflowPunkt { Entladetiefe = 1, ZyklenBisEol = 2000 }
            }));
        Assert.Contains("Rainflow-Kurve ist ungueltig.", doppelt.Message);
    }

    /// <summary>
    /// <b>Die Schranke bleibt stehen</b>, auch wenn die Kandidatenzahl nicht mehr
    /// explodieren kann: Vier Geräte mal zwei Betriebsziele sind acht Kandidaten und
    /// überschreiten eine Grenze von sieben — der Suchraum wird ABGEWIESEN und nicht
    /// gekürzt.
    /// </summary>
    [Fact]
    public void ZuGrosserSuchraum_WirdAbgelehntUndNichtAbgeschnitten()
    {
        FlottenGeraetekandidat Geraet(string id, double kWh, double kW) => new()
        {
            Quellkennung = id,
            Geraet = new FlottenEinheit
            {
                Id = id, Name = id, KapazitaetKWh = kWh,
                LadeleistungKw = kW, EntladeleistungKw = kW,
                Ladewirkungsgrad = 1, Entladewirkungsgrad = 1
            }
        };

        var axis = new FlottenAuslegungsAchse
        {
            Aktiv = true,
            AnzahlVon = 1,
            AnzahlBis = 1,
            KapazitaetVonKWh = 10,
            KapazitaetBisKWh = 20,
            LeistungVonKw = 5,
            LeistungBisKw = 10,
            Geraete = new List<FlottenGeraetekandidat>
            {
                Geraet("a", 10, 5), Geraet("b", 10, 10),
                Geraet("c", 20, 5), Geraet("d", 20, 10)
            },
            Vorlage = new FlottenEinheit { Id = "A", Name = "A", LadeleistungKw = 5,
                EntladeleistungKw = 5, Ladewirkungsgrad = 1, Entladewirkungsgrad = 1 }
        };
        var config = new FlottenStudieKonfiguration
        {
            Auslegung = new FlottenAuslegungEingang
            {
                Achsen = new List<FlottenAuslegungsAchse> { axis },
                Betriebsziele = new List<FlottenBetriebsziel>
                    { FlottenBetriebsziel.PvGreedy, FlottenBetriebsziel.PeakShaving },
                MaximaleKandidaten = 7
            }
        };

        var ex = Assert.Throws<ArgumentException>(() => FlottenOptimierer.Rechne(new FlottenEingang(), config));
        Assert.Contains("nicht gekuerzt", ex.Message);
    }

    [Fact]
    public void KandidatId_UnterscheidetAsymmetrischeEntladeleistung()
    {
        var methode = typeof(FlottenOptimierer).GetMethod("KandidatId",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var einheiten = new List<FlottenEinheit>
        {
            new() { Id = "A", KapazitaetKWh = 12, LadeleistungKw = 4, EntladeleistungKw = 7 }
        };

        var id = Assert.IsType<string>(methode.Invoke(null,
            new object[] { einheiten, FlottenBetriebsziel.MultiUse }));

        Assert.Equal("MultiUse:A:12kWh:4kWladen:7kWentladen", id);
    }

    private static FlottenJahreskonto Konto(int year, double cash, bool full = true) => new()
    {
        Jahr = year,
        NettoCashflowEuro = cash,
        IstVollstaendigesJahr = full,
        Referenzrechnung = new FlottenRechnung { GesamtEuro = cash },
        Variantenrechnung = new FlottenRechnung()
    };
}
