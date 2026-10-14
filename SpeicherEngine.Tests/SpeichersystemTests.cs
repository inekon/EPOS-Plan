using System;
using System.Collections.Generic;
using System.Linq;
using SpeicherEngine;
using Xunit;

namespace SpeicherEngine.Tests;

/// <summary>
/// <b>Welle M5 „Strom in Viertelstunden"</b>: der Eigenverbrauch des Speichersystems (SP1 — Standby
/// und Selbstentladung) und die weiche Einspeisegrenze der Flotte (PV3, Laden vor Abregeln).
/// </summary>
public sealed class SpeichersystemTests
{
    private const double Dt = 0.25;

    // =====================================================================
    //  Standby: aus PV-Überschuss, sonst aus dem Netz
    // =====================================================================

    [Fact]
    public void Standby_wird_aus_PV_Ueberschuss_gedeckt_sonst_aus_dem_Netz()
    {
        // Intervall 0: 2 kW Überschuss deckt 0,05 kW ganz. Intervall 1: kein Überschuss - Netz.
        // Intervall 2: 0,02 kW Überschuss deckt einen Teil. Intervall 3: Überschuss ganz geladen.
        double[] last = { 1, 3, 1, 1 };
        double[] pv = { 3, 1, 1.02, 3 };
        double[] ladung = { 0, 0, 0, 2 * Dt };
        StandbyBilanz b = Speichersystem.Standby(last, pv, ladung, 0.05, Dt);

        Assert.Equal(0.05, b.AusPvKw[0], 12);
        Assert.Equal(0.0, b.AusNetzKw[0], 12);
        Assert.Equal(0.0, b.AusPvKw[1], 12);
        Assert.Equal(0.05, b.AusNetzKw[1], 12);
        Assert.Equal(0.02, b.AusPvKw[2], 9);
        Assert.Equal(0.03, b.AusNetzKw[2], 9);
        Assert.Equal(0.0, b.AusPvKw[3], 12);
        Assert.Equal(0.05, b.AusNetzKw[3], 12);

        // Jahressummen: 4 Intervalle × 0,05 kW × 0,25 h.
        Assert.Equal(4 * 0.05 * Dt, b.GesamtKwh, 12);
        Assert.Equal(b.AusPvKwh + b.AusNetzKwh, b.GesamtKwh, 12);
    }

    [Fact]
    public void Ohne_Standby_ist_die_Bilanz_leer()
    {
        StandbyBilanz b = Speichersystem.Standby(new double[3], new double[] { 5, 5, 5 }, null, 0.0, Dt);
        Assert.All(b.AusPvKw, w => Assert.Equal(0.0, w));
        Assert.All(b.AusNetzKw, w => Assert.Equal(0.0, w));
        Assert.Equal(0.0, b.GesamtKwh);
    }

    [Fact]
    public void Negativer_Standby_wird_benannt_abgelehnt()
    {
        Assert.Throws<ArgumentException>(() => Speichersystem.Standby(new double[1], new double[1], null, -1, Dt));
    }

    // =====================================================================
    //  Selbstentladung
    // =====================================================================

    [Fact]
    public void Selbstentladung_zehrt_den_Inhalt_hoechstens_bis_zur_Untergrenze()
    {
        Assert.Equal(0.0, Speichersystem.Selbstentladung(10, 1, 0.0));
        Assert.Equal(10 * 0.001, Speichersystem.Selbstentladung(10, 1, 0.001), 15);
        // Knapp über der Untergrenze: nur bis zu ihr.
        Assert.Equal(0.005, Speichersystem.Selbstentladung(1.005, 1, 0.5), 15);
        Assert.Equal(0.0, Speichersystem.Selbstentladung(1, 1, 0.5));
    }

    [Fact]
    public void Ein_ruhender_Speicher_verliert_einen_Monat_lang_die_gepflegte_Selbstentladung()
    {
        // 730 h = 2 920 Viertelstunden ohne Last und ohne PV: der Speicher steht still und
        // verliert nur durch Selbstentladung (3 %/Monat).
        int n = 2920;
        var eingang = new SpeicherEingang(new double[n], new double[n], SpeicherEingang.KonstanteReihe(20, n));
        var p = Parameter(10, 0.0) with { StartSoCKwh = 10, SelbstentladungProzentMonat = 3 };

        SpeicherErgebnis e = new Dauernutzung().Berechne(eingang, p);
        double erwartet = 10 * Math.Pow(1 - 0.03 / n, n);
        Assert.Equal(erwartet, e.SoCKwh[n - 1], 9);
        Assert.Equal(10 - erwartet, e.Kennzahlen.SelbstentladungKwh, 9);
        Assert.Equal(e.Kennzahlen.SelbstentladungKwh, e.Kennzahlen.SpeicherverlusteKwh, 9);
    }

    [Fact]
    public void Ohne_Selbstentladung_rechnet_die_Dauernutzung_bitgleich()
    {
        int n = 96;
        var last = Enumerable.Range(0, n).Select(i => 2.0 + (i % 7)).ToArray();
        var pv = Enumerable.Range(0, n).Select(i => i % 4 == 0 ? 12.0 : 0.5).ToArray();
        var eingang = new SpeicherEingang(last, pv, SpeicherEingang.KonstanteReihe(25, n));
        var p = Parameter(20, 5);

        SpeicherErgebnis a = new Dauernutzung().Berechne(eingang, p);
        SpeicherErgebnis b = new Dauernutzung().Berechne(eingang, p with { SelbstentladungProzentMonat = 0, StandbyKw = 0 });
        Assert.Equal(a.SoCKwh, b.SoCKwh);
        Assert.Equal(0.0, b.Kennzahlen.SelbstentladungKwh);
    }

    [Fact]
    public void Selbstentladung_ausserhalb_des_Bereichs_wird_benannt_abgelehnt()
    {
        SpeicherParameter negativ = Parameter(10, 5) with { SelbstentladungProzentMonat = -1 };
        SpeicherParameter hundert = Parameter(10, 5) with { SelbstentladungProzentMonat = 100 };
        SpeicherParameter standby = Parameter(10, 5) with { StandbyKw = -0.1 };
        Assert.Throws<ArgumentOutOfRangeException>(() => negativ.Pruefe());
        Assert.Throws<ArgumentOutOfRangeException>(() => hundert.Pruefe());
        Assert.Throws<ArgumentOutOfRangeException>(() => standby.Pruefe());
    }

    [Fact]
    public void Die_Flotte_kennt_die_Selbstentladung_je_Einheit()
    {
        int n = 2920;
        var rows = Enumerable.Range(0, n).Select(i => Row(i)).ToArray();
        var b = Einheit("A", 10, 5);
        b.SocStart = 1.0;
        b.SelbstentladungProzentProMonat = 3;
        var config = Config(b);

        FlottenSimulationErgebnis v = FlottenSimulator.Simuliere(Input(rows), config).Variante;
        double erwartet = 10 * Math.Pow(1 - 0.03 / n, n);
        Assert.Equal(erwartet, v.Intervalle[n - 1].EnergieEndeKWhJeSpeicher[0], 9);
        Assert.Equal(10 - erwartet, v.SelbstentladungKWh, 9);
        Assert.True(v.Zulaessig);
    }

    // =====================================================================
    //  PV3 — die weiche Einspeisegrenze der Flotte: Laden vor Abregeln
    // =====================================================================

    [Fact]
    public void Weiche_Einspeisegrenze_regelt_erst_nach_dem_Laden_ab_und_bleibt_zulaessig()
    {
        // 20 kW PV, 5 kW Last: 15 kW Überschuss. Die Einheit lädt 4 kW, 11 kW blieben für das Netz;
        // die weiche Grenze 6 kW regelt 5 kW ab.
        var b = Einheit("A", 100, 4);
        var config = Config(b);
        config.Optionen.PvEinspeisegrenzeWeichKw = 6;
        FlottenIntervallErgebnis x = FlottenSimulator.Simuliere(Input(Row(0, load: 5, pv: 20)), config)
            .Variante.Intervalle[0];

        Assert.Equal(-4, x.IstleistungKwJeSpeicher[0], 8);
        Assert.Equal(6, x.NetzeinspeisungKw, 8);
        Assert.Equal(5, x.PvAbregelungKw, 8);
        Assert.Equal(0, x.TechnischeExportverletzungKw, 8);
    }

    [Fact]
    public void Ohne_weiche_Grenze_bleibt_die_Flotte_bitgleich()
    {
        var config = Config(Einheit("A", 100, 4));
        FlottenIntervallErgebnis x = FlottenSimulator.Simuliere(Input(Row(0, load: 5, pv: 20)), config)
            .Variante.Intervalle[0];
        Assert.Equal(11, x.NetzeinspeisungKw, 8);
        Assert.Equal(0, x.PvAbregelungKw, 8);
    }

    [Fact]
    public void Die_weiche_Grenze_bleibt_ausserhalb_der_gespeicherten_Konfiguration()
    {
        var o = new FlottenSimulationOptionen { PvEinspeisegrenzeWeichKw = 7 };
        string json = System.Text.Json.JsonSerializer.Serialize(o);
        Assert.DoesNotContain(nameof(FlottenSimulationOptionen.PvEinspeisegrenzeWeichKw), json);
    }

    // -----------------------------------------------------------------------------
    //  Hilfen
    // -----------------------------------------------------------------------------

    private static SpeicherParameter Parameter(double kapazitaet, double leistung) => new()
    {
        CNomKwh = kapazitaet,
        PKw = leistung,
        SoCMinKwh = 0.0,
        SoCMaxKwh = kapazitaet,
        RoundTripWirkungsgrad = 1.0,
        StartSoCKwh = 0.0
    };

    private static FlottenStudieKonfiguration Config(params FlottenEinheit[] units) => new()
    {
        Einheiten = new List<FlottenEinheit>(units),
        Optionen = new FlottenSimulationOptionen
        {
            Betriebsziel = FlottenBetriebsziel.PvGreedy,
            EnergieAusgleichEuroProKWh = 0
        }
    };

    private static FlottenEinheit Einheit(string id, double capacity, double power) => new()
    {
        Id = id,
        Name = id,
        KapazitaetKWh = capacity,
        LadeleistungKw = power,
        EntladeleistungKw = power,
        Ladewirkungsgrad = 1,
        Entladewirkungsgrad = 1,
        SocMin = 0,
        SocMax = 1,
        SocStart = 0.5
    };

    private static FlottenEingang Input(params FlottenNetzintervall[] rows) => new()
    {
        KonfigurationId = "C",
        DatenId = "D",
        Istwerte = new List<FlottenNetzintervall>(rows)
    };

    private static FlottenNetzintervall Row(int quarter, double load = 0, double pv = 0) => new()
    {
        Zeitstempel = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMinutes(15 * quarter),
        LastKw = load,
        PvKw = pv,
        BezugspreisEuroProKWh = 0.3,
        PvVerkaufspreisEuroProKWh = 0.08,
        BhkwVerkaufspreisEuroProKWh = 0.12,
        BatterieVerkaufspreisEuroProKWh = 0.05
    };
}
