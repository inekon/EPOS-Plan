using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using SpeicherEngine;
using Xunit;

namespace SpeicherEngine.Tests;

/// <summary>
/// AUFTRAG #193 (Paket P4 des Konzepts „Stromspeicher-Dialoge", Abschnitt 2.5): Die
/// Kandidaten der Rastersuche tragen seither die BETRIEBSkennzahlen — Durchsatz,
/// Vollzyklen, Bezugsspitze, Betriebsersparnis und die Aussage „arbeitslos" — sowie
/// ihre Achsenwerte und ihre Stelle im Raster.
///
/// <para><b>Der Befund dahinter</b> (Konzept 1.5): Bis hierher stand je Kandidat nur
/// der Kapitalwert da. Ein arbeitsloser Kandidat — die Flotte lädt und entlädt im
/// ganzen Jahr nicht, Befund SP‑O‑10 — war von einem arbeitenden nicht zu
/// unterscheiden.</para>
///
/// <para><b>Wie geprüft wird: gegen den EINZELLAUF.</b> Die Zahlen sollen die des
/// Kandidatenlaufs sein und keine zweite Rechnung. Der Prüfstand rechnet deshalb
/// dieselbe Hardware ein zweites Mal mit <see cref="FlottenSimulator.Simuliere"/> und
/// hält die Kennzahlen des Kandidaten Bit für Bit dagegen. Eine zusätzliche Simulation
/// im Optimierer fiele damit auf.</para>
///
/// <para><b>Der Prüfstand</b> ist der der Diagnoseprobe (#183): acht Viertelstunden,
/// die Last springt zwischen 20 kW und 4 kW, das Peak-Ziel steht auf 10 kW, der
/// Speicher startet auf seinem SoC-Minimum. Mit freigegebener Netzladung arbeitet die
/// Flotte, ohne sie ist sie arbeitslos. Gerastert werden zwei Kapazitäten und zwei
/// C-Raten — vier Kandidaten, ein 2 × 2-Raster.</para>
/// </summary>
public sealed class FlottenKandidatKennzahlenTests : IDisposable
{
    private readonly CultureInfo _vorher = CultureInfo.CurrentCulture;

    public FlottenKandidatKennzahlenTests()
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

    // =====================================================================
    //  Achsenwerte und Rasterstellen
    // =====================================================================

    /// <summary>
    /// Das 2 × 2-Raster liefert vier Kandidaten, jeden genau einmal — und die
    /// Nullvariante davor, die keine Stelle im Raster hat.
    /// </summary>
    [Fact]
    public void Das_Raster_liefert_jede_Stelle_genau_einmal()
    {
        FlottenAuslegungErgebnis ergebnis = Suche(netzladung: true);

        Assert.Equal(5, ergebnis.Kandidaten.Count);
        FlottenKandidatZusammenfassung null1 = ergebnis.Kandidaten[0];
        Assert.Empty(null1.Einheiten);
        Assert.Equal(-1, null1.Rasterzeile);
        Assert.Equal(-1, null1.Rasterspalte);

        var stellen = Gerastert(ergebnis).Select(k => (k.Rasterzeile, k.Rasterspalte)).ToArray();
        Assert.Equal(4, stellen.Distinct().Count());
        Assert.All(stellen, s => Assert.InRange(s.Rasterzeile, 0, 1));
        Assert.All(stellen, s => Assert.InRange(s.Rasterspalte, 0, 1));
    }

    /// <summary>
    /// Die Zeile ist die KAPAZITÄT, die Spalte die C-RATE — in der Reihenfolge, in der
    /// die Achse sie führt. Ohne diese Zuordnung stünde die Rasterkarte gespiegelt.
    /// </summary>
    [Fact]
    public void Zeile_ist_die_Kapazitaet_und_Spalte_die_CRate()
    {
        foreach (FlottenKandidatZusammenfassung k in Gerastert(Suche(netzladung: true)))
        {
            Assert.Equal(k.Rasterzeile == 0 ? 10.0 : 20.0, k.KapazitaetKWh, 9);
            Assert.Equal(k.Rasterspalte == 0 ? 0.5 : 1.0, k.CRate, 9);
        }
    }

    /// <summary>
    /// Die Größen JE EINHEIT stehen neben den Summen: Bei einer Einheit sind sie
    /// dieselben Zahlen, und die C-Rate der Einheit ist die der Flotte.
    /// </summary>
    [Fact]
    public void Jeder_Kandidat_nennt_die_Groessen_seiner_Einheit()
    {
        foreach (FlottenKandidatZusammenfassung k in Gerastert(Suche(netzladung: true)))
        {
            FlottenKandidatEinheit einheit = Assert.Single(k.Einheiten);
            Assert.Equal(k.KapazitaetKWh, einheit.KapazitaetKWh, 9);
            Assert.Equal(k.LadeleistungKw, einheit.LadeleistungKw, 9);
            Assert.Equal(k.EntladeleistungKw, einheit.EntladeleistungKw, 9);
            Assert.Equal(k.CRate, einheit.CRate, 9);
            Assert.Equal(k.KapazitaetKWh * k.CRate, k.EntladeleistungKw, 9);
        }
    }

    // =====================================================================
    //  Die Betriebskennzahlen
    // =====================================================================

    /// <summary>
    /// Durchsatz, Vollzyklen, Bezugsspitze und Ersparnis des Kandidaten sind die Zahlen
    /// DESSELBEN Laufs — geprüft gegen eine zweite, unabhängige Simulation derselben
    /// Hardware.
    /// </summary>
    [Fact]
    public void Die_Kennzahlen_sind_die_des_Kandidatenlaufs()
    {
        FlottenKandidatZusammenfassung k = Einer(Suche(netzladung: true), kapazitaet: 10.0, cRate: 1.0);
        FlottenStudienErgebnis einzeln = Einzellauf(netzladung: true, kapazitaet: 10.0, leistung: 10.0);

        double durchsatz = einzeln.Variante.SpeicherKennzahlen.Sum(x => x.EntladeenergieAcKWh);
        Assert.True(durchsatz > 0.0, "Der Prüfstand soll eine arbeitende Flotte zeigen.");

        Assert.Equal(durchsatz, k.DurchsatzKWh, 9);
        Assert.Equal(durchsatz / 10.0, k.Vollzyklen, 9);
        Assert.Equal(einzeln.Variante.MaximalerNetzbezugKw, k.BezugsspitzeKw, 9);
        Assert.False(k.Arbeitslos);
    }

    /// <summary>
    /// Die ERSPARNIS ist die Rechnungsdifferenz OHNE Kapitaldienst: abzüglich
    /// Betriebsaufwand und Durchsatzkosten, aber ohne Investition und Ersatz. Der
    /// Prüfstand trägt beide Kostenarten, damit ein Weglassen auffiele.
    /// </summary>
    [Fact]
    public void Die_Ersparnis_zieht_Betrieb_und_Durchsatz_ab_und_keinen_Kapitaldienst()
    {
        FlottenKandidatZusammenfassung k = Einer(Suche(netzladung: true), kapazitaet: 10.0, cRate: 1.0);
        FlottenStudienErgebnis einzeln = Einzellauf(netzladung: true, kapazitaet: 10.0, leistung: 10.0);

        double durchsatz = einzeln.Variante.SpeicherKennzahlen.Sum(x => x.EntladeenergieAcKWh);
        double opex = OPEX_FEST;
        double durchsatzkosten = durchsatz * DURCHSATZKOSTEN;
        double erwartet = einzeln.Referenzrechnung.GesamtEuro - einzeln.Variantenrechnung.GesamtEuro
                          - opex - durchsatzkosten;

        Assert.Equal(erwartet, k.ErsparnisEuroJahr, 9);

        // Gegenprobe: Die Investition steckt NICHT darin - sonst wäre die Ersparnis um
        // die 1 000 EUR Festbetrag kleiner als der reine Betriebsvorteil.
        Assert.True(k.KapitalwertEuro < k.ErsparnisEuroJahr,
            "Der Kapitalwert trägt den Kapitaldienst, die Ersparnis nicht.");
    }

    /// <summary>
    /// OHNE Netzladung tut die Flotte im ganzen Zeitraum nichts: Jeder Kandidat ist
    /// arbeitslos, sein Durchsatz ist null — und die Bezugsspitze bleibt die der
    /// Referenz. Genau diesen Fall konnte die Kandidatentabelle bis #193 nicht zeigen.
    /// </summary>
    [Fact]
    public void Ohne_Netzladung_ist_jeder_Kandidat_arbeitslos()
    {
        FlottenAuslegungErgebnis ergebnis = Suche(netzladung: false);
        FlottenStudienErgebnis einzeln = Einzellauf(netzladung: false, kapazitaet: 10.0, leistung: 10.0);

        foreach (FlottenKandidatZusammenfassung k in Gerastert(ergebnis))
        {
            Assert.True(k.Arbeitslos);
            Assert.Equal(0.0, k.DurchsatzKWh, 9);
            Assert.Equal(0.0, k.Vollzyklen, 9);
        }

        Assert.Equal(einzeln.Variante.MaximalerNetzbezugKw,
                     Einer(ergebnis, kapazitaet: 10.0, cRate: 1.0).BezugsspitzeKw, 9);
    }

    /// <summary>
    /// DAS ERGEBNIS TRÄGT DIE GRÖSSENKOPPLUNG der Suchachse (Auftrag #226,
    /// Anwenderbefund 11.09.2026). Ohne sie weiß die Größen-Sicht nicht, was ihre
    /// Achsen bedeuten, und legte die Kandidaten eines Kapazität × Leistung-Gitters
    /// auf eine C-Raten-Achse — dort sind sie kein Gitter mehr, sondern Löcher.
    /// </summary>
    /// <param name="modus">Die eingestellte Kopplung der einzigen aktiven Achse.</param>
    [Theory]
    [InlineData(FlottenAuslegungsmodus.KapazitaetUndCRate)]
    [InlineData(FlottenAuslegungsmodus.KapazitaetUndLeistung)]
    [InlineData(FlottenAuslegungsmodus.LeistungUndCRate)]
    public void Das_Ergebnis_traegt_die_Groessenkopplung_der_Suchachse(FlottenAuslegungsmodus modus)
        => Assert.Equal(modus, Suche(netzladung: true, modus).Achsenmodus);

    /// <summary>
    /// OHNE aktive Suchachse gibt es kein Raster — dann bleibt die Vorbelegung stehen,
    /// und ein Ergebnis aus fremder Quelle wird gelesen wie vor #226.
    /// </summary>
    [Fact]
    public void Ohne_Suchachse_bleibt_die_Vorbelegung_stehen()
        => Assert.Equal(FlottenAuslegungsmodus.KapazitaetUndCRate,
                        new FlottenAuslegungErgebnis().Achsenmodus);

    // ================================================================= Prüfstand

    private const double OPEX_FEST = 12.0;
    private const double DURCHSATZKOSTEN = 0.01;

    /// <summary>Die vier gerasterten Kandidaten — ohne die Nullvariante.</summary>
    private static IEnumerable<FlottenKandidatZusammenfassung> Gerastert(FlottenAuslegungErgebnis e)
        => e.Kandidaten.Where(k => k.Einheiten.Count > 0);

    private static FlottenKandidatZusammenfassung Einer(FlottenAuslegungErgebnis e,
                                                        double kapazitaet, double cRate)
        => Gerastert(e).Single(k => Math.Abs(k.KapazitaetKWh - kapazitaet) < 1e-9
                                 && Math.Abs(k.CRate - cRate) < 1e-9);

    /// <summary>
    /// Die Rastersuche über zwei Kapazitäten und zwei C-Raten — und, seit Auftrag #226,
    /// wahlweise über eine andere Größenkopplung. Die Leistungsgrenzen stehen deshalb
    /// mit da; in den zwei C-Raten-Modi liest sie niemand.
    /// </summary>
    /// <param name="netzladung">Darf die Flotte aus dem Netz laden?</param>
    /// <param name="modus">Die Größenkopplung der einzigen aktiven Achse.</param>
    private static FlottenAuslegungErgebnis Suche(
        bool netzladung,
        FlottenAuslegungsmodus modus = FlottenAuslegungsmodus.KapazitaetUndCRate)
    {
        FlottenStudieKonfiguration config = Config(netzladung);
        config.Einheiten.Clear();
        config.Wirtschaftlichkeit.Einheiten.Clear();
        config.Auslegung = new FlottenAuslegungEingang
        {
            MaximaleKandidaten = 100,
            Achsen = new List<FlottenAuslegungsAchse>
            {
                new()
                {
                    Aktiv = true,
                    Modus = modus,
                    AnzahlVon = 1,
                    AnzahlBis = 1,
                    KapazitaetVonKWh = 10,
                    KapazitaetBisKWh = 20,
                    KapazitaetSchrittKWh = 10,
                    LeistungVonKw = 5,
                    LeistungBisKw = 10,
                    LeistungSchrittKw = 5,
                    CRateVon = 0.5,
                    CRateBis = 1.0,
                    CRateSchritt = 0.5,
                    Vorlage = Einheit(10, 10)
                }
            }
        };
        return FlottenOptimierer.Rechne(Eingang(), config);
    }

    /// <summary>Dieselbe Hardware EINZELN gerechnet — die unabhängige Gegenrechnung.</summary>
    private static FlottenStudienErgebnis Einzellauf(bool netzladung, double kapazitaet, double leistung)
    {
        FlottenStudieKonfiguration config = Config(netzladung);
        config.Einheiten = new List<FlottenEinheit> { Einheit(kapazitaet, leistung) };
        FlottenStudienErgebnis studie = FlottenSimulator.Simuliere(Reihe(), config);
        return studie;
    }

    private static FlottenStudieKonfiguration Config(bool netzladung) => new()
    {
        Einheiten = new List<FlottenEinheit>(),
        Optionen = new FlottenSimulationOptionen
        {
            Betriebsziel = FlottenBetriebsziel.PeakShaving,
            WirtschaftlicherPeakZielwertKw = 10,
            NetzladungErlaubt = netzladung,
            EnergieAusgleichEuroProKWh = 0
        },
        Tarif = new FlottenTarif { LeistungspreisEuroProKw = 100 },
        Wirtschaftlichkeit = new FlottenWirtschaftlichkeitEingang { Kalkulationszins = 0 }
    };

    /// <summary>
    /// Die Vorlage: ein verlustfreier Speicher mit festem Betriebsaufwand, Durchsatzkosten
    /// und einer Investition von 1 000 EUR — die drei Größen, an denen sich Ersparnis und
    /// Kapitalwert unterscheiden müssen.
    /// </summary>
    private static FlottenEinheit Einheit(double kapazitaet, double leistung) => new()
    {
        Id = "A",
        Name = "Speicher A",
        KapazitaetKWh = kapazitaet,
        LadeleistungKw = leistung,
        EntladeleistungKw = leistung,
        Ladewirkungsgrad = 1,
        Entladewirkungsgrad = 1,
        SocMin = 0.2,
        SocMax = 1.0,
        SocStart = 0.2,
        JaehrlicheFixeOpexEuro = OPEX_FEST,
        DurchsatzkostenEuroProKWhEntladung = DURCHSATZKOSTEN,
        InvestitionEuro = 1000
    };

    /// <summary>
    /// EIN Projektjahr, ausdrücklich als vollständig gekennzeichnet. Die Rastersuche
    /// verlangt sonst die 35 040 Zeilen eines echten Jahres; geprüft werden hier die
    /// Kennzahlen, nicht die Jahreslänge.
    /// </summary>
    private static FlottenEingang Eingang() => new()
    {
        KonfigurationId = "C",
        DatenId = "D",
        Projektjahre = new List<FlottenProjektjahr>
        {
            new()
            {
                Jahr = 2026,
                IstVollstaendigesJahr = true,
                Istwerte = Zeilen()
            }
        }
    };

    private static FlottenEingang Reihe() => new()
    {
        KonfigurationId = "C",
        DatenId = "D",
        Istwerte = Zeilen()
    };

    /// <summary>Acht Viertelstunden, abwechselnd 20 kW und 4 kW Last, keine Erzeugung.</summary>
    private static List<FlottenNetzintervall> Zeilen() => Enumerable.Range(0, 8)
        .Select(t => new FlottenNetzintervall
        {
            Zeitstempel = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMinutes(15 * t),
            LastKw = t % 2 == 0 ? 20.0 : 4.0,
            BezugspreisEuroProKWh = 0.3,
            PvVerkaufspreisEuroProKWh = 0.08,
            BhkwVerkaufspreisEuroProKWh = 0.12,
            BatterieVerkaufspreisEuroProKWh = 0.05
        }).ToList();
}
