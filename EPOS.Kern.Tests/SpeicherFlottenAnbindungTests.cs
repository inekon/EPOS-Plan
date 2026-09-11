using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests;

public sealed class SpeicherFlottenAnbindungTests
{
    private static StromspeicherOptimierungVorbereitung Vorbereitung()
    {
        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        return new StromspeicherOptimierungVorbereitung
        {
            Basis = new SpeicherParameter { CNomKwh = 10, PKw = 5, SoCMaxKwh = 10, VerguetungCtKwh = 7 },
            Eingaben = new SpeicherOptimierungEingaben { Auslegung = new SpeicherAuslegungKonfiguration
            { Flotte = new FlottenStudieKonfiguration() } },
            ZeitstempelUtc = new[] { start, start.AddMinutes(15) },
            Eingang = new SpeicherEingang(new[] { 10d, 20d }, new[] { 3d, 5d }, new[] { -10d, 30d },
                new[] { 1d, 2d }, new[] { 8d, 9d }, new[] { 11d, 12d })
        };
    }

    [Fact]
    public void Adapter_erhaelt_Bruttolast_und_quellenspezifische_Preispfade()
    {
        var v = Vorbereitung();
        var e = SpeicherFlottenStudieCtrl.Eingang(v);
        Assert.Equal(10, e.Istwerte[0].LastKw);
        Assert.Equal(3, e.Istwerte[0].PvKw);
        Assert.Equal(1, e.Istwerte[0].BhkwKw);
        Assert.Equal(-0.10, e.Istwerte[0].BezugspreisEuroProKWh, 10);
        Assert.Equal(0.08, e.Istwerte[0].PvVerkaufspreisEuroProKWh, 10);
        Assert.Equal(0.11, e.Istwerte[0].BhkwVerkaufspreisEuroProKWh, 10);
        Assert.Empty(e.Prognosen);
        Assert.Equal(v.ZeitstempelUtc, e.Istwerte.Select(x => x.Zeitstempel));
    }

    [Fact]
    public void Nur_explizites_Idealwissen_erzeugt_einen_Oracle_Snapshot()
    {
        var v = Vorbereitung();
        v.Eingaben.Auslegung.Flotte.Optionen.PrognoseArt = PrognoseArt.Oracle;
        var e = SpeicherFlottenStudieCtrl.Eingang(v);
        Assert.Single(e.Prognosen);
        Assert.Equal(PrognoseArt.Oracle, e.Prognosen[0].Art);
        e.Istwerte[0].LastKw = 999;
        Assert.Equal(10, e.Prognosen[0].Intervalle[0].LastKw);
    }

    [Fact]
    public void Batterieexport_verlangt_einen_eigenen_effektiven_Verkaufspreis()
    {
        var v = Vorbereitung();
        v.Eingaben.Auslegung.Flotte.Optionen.BatterieexportErlaubt = true;
        Assert.Throws<ArgumentException>(() => SpeicherFlottenStudieCtrl.Eingang(v));
        v.Eingaben.Auslegung.Flotte.Tarif.BatterieVerkaufspreisEuroProKWh = -0.02;
        Assert.Equal(-0.02, SpeicherFlottenStudieCtrl.Eingang(v).Istwerte[0].BatterieVerkaufspreisEuroProKWh);
    }

    [Fact]
    public void Kostenprofil_skalierbare_Saetze_und_eigene_Kosten_bleiben_getrennt()
    {
        var e = Vorbereitung().Eingaben;
        e.Auslegung.VerwendeteKosten = new SpeicherKostensaetze { InvestEurProKwh = 200, InvestEurProKw = 90, BetriebEurProKwJahr = 3 };
        e.Auslegung.Flotte.Einheiten = new()
        {
            new() { Name = "Geerbt", KapazitaetKWh = 10, InvestitionEuro = 123, ErsatzkostenEuro = 45 },
            new() { Name = "Eigen", EigeneKosten = true, InvestitionEuroProKWh = 250 }
        };
        var f = SpeicherFlottenStudieCtrl.Konfiguration(e);
        Assert.Equal(200, f.Einheiten[0].InvestitionEuroProKWh);
        Assert.Equal(90, f.Einheiten[0].InvestitionEuroProKw);
        Assert.Equal(0, f.Einheiten[0].InvestitionEuro);
        Assert.Equal(0, f.Einheiten[0].ErsatzkostenEuro);
        Assert.Equal(123, e.Auslegung.Flotte.Einheiten[0].InvestitionEuro);
        Assert.Equal(250, f.Einheiten[1].InvestitionEuroProKWh);
        Assert.Equal(0, e.Auslegung.Flotte.Einheiten[0].InvestitionEuroProKWh);
    }

    [Fact]
    public void Gespeichertes_Profil_traegt_Flotte_und_verifizierte_Prognosen_als_tiefe_Kopie()
    {
        var e = Vorbereitung().Eingaben;
        var t = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        e.Auslegung.Flotte.Einheiten.Add(new() { Id = "eins", Name = "Speicher 1", KapazitaetKWh = 20 });
        e.Auslegung.FlottenPrognosen.Add(new FlottenPrognoseSnapshot("p1", t, t, PrognoseArt.VerifiziertBekannt,
            new[] { new FlottenNetzintervall { Zeitstempel = t, LastKw = 1 } }));
        var kopie = e.Kopie();
        Assert.Single(kopie.Auslegung.Flotte.Einheiten);
        Assert.Single(kopie.Auslegung.FlottenPrognosen);
        kopie.Auslegung.Flotte.Einheiten[0].KapazitaetKWh = 40;
        Assert.Equal(20, e.Auslegung.Flotte.Einheiten[0].KapazitaetKWh);
        Assert.Equal("p1", kopie.Auslegung.FlottenPrognosen[0].Id);
    }

    [Fact]
    public void Modellkalender_erhaelt_DST_und_Schaltjahr()
    {
        var zeiten = SpeicherFlottenStudieCtrl.ModellZeitachse(2028, 35136);
        Assert.Equal(35136, zeiten.Length);
        Assert.All(zeiten.Zip(zeiten.Skip(1)), p => Assert.Equal(TimeSpan.FromMinutes(15), p.Second - p.First));
        Assert.Throws<ArgumentException>(() => SpeicherFlottenStudieCtrl.ModellZeitachse(2026, 35136));
    }

    [Theory]
    [InlineData(35040)]
    [InlineData(35136)]
    public void Epos_Modelljahr_erhaelt_jedes_Intervall_und_Energie_ohne_Jahreseingabe(int anzahl)
    {
        var v = Vorbereitung();
        var werte = Enumerable.Range(0, anzahl).Select(i => (double)i).ToArray();
        v.ZeitstempelUtc = null;
        v.Eingaben.Auslegung.FlottenModelljahr = 0; // Altes Profilfeld ist kein Eingabezwang.
        v.Eingang = new SpeicherEingang(werte, werte, werte);
        var e = SpeicherFlottenStudieCtrl.Eingang(v);
        Assert.Equal(werte, e.Istwerte.Select(x => x.LastKw));
        Assert.Equal(werte, e.Istwerte.Select(x => x.PvKw));
        Assert.Equal(werte.Sum() * .25, e.Istwerte.Sum(x => x.LastKw) * .25);
        Assert.All(e.Istwerte.Zip(e.Istwerte.Skip(1)),
            p => Assert.Equal(TimeSpan.FromMinutes(15), p.Second.Zeitstempel - p.First.Zeitstempel));
    }
}
