using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using SpeicherEngine;
using Xunit;

namespace SpeicherEngine.Tests;

/// <summary>
/// AUFTRAG #224 (Anwenderentscheid SD‑E‑9 / SD‑Q10, 11.09.2026): die ZWEITE PHASE der
/// Flotten-Rastersuche — das Feinraster um das Grob-Optimum.
///
/// <para><b>Woher die Regel kommt.</b> Die Mappe V7 („Optimierung Speicher") rechnet seit
/// jeher zwei Phasen: ein Grobraster über den ganzen Suchraum und danach ein enges Raster
/// im Fenster <c>[best − Schrittweite, best + Schrittweite]</c>, gekappt auf den Suchraum,
/// in Schritten von <c>Schrittweite / 9</c>. Der abgelöste Einzelspeicher-Optimierer
/// (<c>SpeicherOptimierer.FeinrasterBereich</c>, Mindestbreite 1 kWh, strenger
/// Größer-Vergleich Z. 143) bildet sie nach; der <c>FlottenOptimierer</c> rechnete bis
/// hierher NUR das Grobraster (Konzept „Stromspeicher-Dialoge" 7.2, Paket P6).</para>
///
/// <para><b>Das Prüfmuster ist dreiteilig</b> und steht so auch im Konzept:</para>
/// <list type="number">
/// <item>Das Grob-Optimum liegt IM Feinintervall — die zweite Phase sucht um es herum und
/// nicht irgendwo.</item>
/// <item>Das Feinraster gewinnt NUR bei strikt besserem Kapitalwert; bei Gleichstand
/// bleibt der Grobpunkt der Beste.</item>
/// <item>Jeder Kandidat trägt seine Phase, und <c>MaximaleKandidaten</c> zählt beide.</item>
/// </list>
///
/// <para><b>Der Prüfstand</b> ist der der Kennzahlenprobe (#193): acht Viertelstunden, die
/// Last springt zwischen 20 kW und 4 kW, Peak-Ziel 10 kW, Netzladung frei. Gerastert wird
/// EINE Achse über die Kapazität 10…20 kWh in Schritten von 10 kWh — das Feinraster läuft
/// damit in Schritten von 10/9 kWh.</para>
/// </summary>
public sealed class FlottenFeinrasterTests : IDisposable
{
    private readonly CultureInfo _vorher = CultureInfo.CurrentCulture;

    public FlottenFeinrasterTests()
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
    //  1. Der Bereich: das Grob-Optimum liegt IM Feinintervall
    // =====================================================================

    /// <summary>
    /// Ein Grob-Optimum MITTEN im Suchraum bekommt ein beidseitiges Fenster von je einer
    /// Schrittweite, gerastert mit Schrittweite/9.
    /// </summary>
    [Fact]
    public void Das_Feinintervall_liegt_um_das_Grob_Optimum_und_enthaelt_es()
    {
        var achse = Achse(von: 100, bis: 500, schritt: 100);

        List<double> werte = FlottenOptimierer.Feinrasterwerte(achse, 300);

        Assert.Equal(200.0, werte[0], 9);
        Assert.Equal(400.0, werte[^1], 9);
        Assert.Contains(werte, w => Math.Abs(w - 300.0) < 1e-9);      // das Optimum ist dabei
        Assert.Equal(100.0 / 9.0, werte[1] - werte[0], 9);            // Schrittweite / 9
        Assert.Equal(19, werte.Count);
    }

    /// <summary>
    /// Am RAND wird das Fenster gekappt — die Suche läuft nicht über die vom Anwender
    /// gesetzten Grenzen hinaus (dieselbe Regel wie <c>SpeicherOptimierer</c>).
    /// </summary>
    [Fact]
    public void Ein_Optimum_am_Rand_bekommt_ein_einseitiges_Feinintervall()
    {
        var achse = Achse(von: 500, bis: 5000, schritt: 500);

        List<double> werte = FlottenOptimierer.Feinrasterwerte(achse, 5000);

        Assert.Equal(4500.0, werte[0], 9);
        Assert.Equal(5000.0, werte[^1], 9);
        Assert.All(werte, w => Assert.InRange(w, 500.0, 5000.0));
    }

    /// <summary>
    /// Die MINDESTBREITE von 1 kWh greift nur bei einem Suchraum, der schmaler ist —
    /// wörtlich die Vorlagentreue aus der Mappe.
    /// </summary>
    [Fact]
    public void Ein_sehr_schmaler_Suchraum_bekommt_die_Mindestbreite()
    {
        var achse = Achse(von: 10, bis: 10.2, schritt: 0.1);

        List<double> werte = FlottenOptimierer.Feinrasterwerte(achse, 10.1);

        Assert.True(werte[^1] - werte[0] >= FlottenOptimierer.FEINRASTER_MINDESTBREITE - 1e-9);
    }

    /// <summary>Ein Suchraum aus EINEM Stützwert hat nichts zu verfeinern.</summary>
    [Fact]
    public void Ein_einziger_Stuetzwert_bekommt_kein_Feinraster()
        => Assert.Empty(FlottenOptimierer.Feinrasterwerte(Achse(von: 42, bis: 42, schritt: 1), 42));

    // =====================================================================
    //  2. Die Zählregel — dieselbe Zahl für Kandidatenzeile und Lauf
    // =====================================================================

    /// <summary>
    /// Die Zählregel nennt Grobraster und Feinraster-Obergrenze getrennt und prüft die
    /// Grenze gegen BEIDE Phasen.
    /// </summary>
    [Fact]
    public void Die_Zaehlregel_nennt_beide_Phasen()
    {
        FlottenStudieKonfiguration config = Konfiguration(feinraster: true, grenze: 1000);

        FlottenKandidatenzahl zahl = FlottenOptimierer.Kandidatenzahl(config);

        Assert.True(zahl.Gueltig);
        Assert.Equal(2, zahl.Grob);                  // 10 und 20 kWh, ein Betriebsziel
        Assert.True(zahl.FeinHoechstens > 0);
        Assert.Equal(zahl.Grob + zahl.FeinHoechstens, zahl.Gesamt);
        Assert.True(zahl.Zulaessig);
    }

    /// <summary>Ohne zweite Phase zählt nur das Grobraster.</summary>
    [Fact]
    public void Ohne_Feinraster_zaehlt_nur_das_Grobraster()
    {
        FlottenKandidatenzahl zahl =
            FlottenOptimierer.Kandidatenzahl(Konfiguration(feinraster: false, grenze: 1000));

        Assert.Equal(0, zahl.FeinHoechstens);
        Assert.Equal(zahl.Grob, zahl.Gesamt);
    }

    /// <summary>
    /// Die Grenze reisst am GESAMTRASTER: Ein Grobraster, das allein noch hineinpasst,
    /// wird abgewiesen, sobald das Feinraster dazukommt — und der Lauf sagt es, BEVOR er
    /// die erste Phase verrechnet hat.
    /// </summary>
    [Fact]
    public void Die_Kandidatengrenze_zaehlt_beide_Phasen()
    {
        FlottenStudieKonfiguration config = Konfiguration(feinraster: true, grenze: 3);

        Assert.False(FlottenOptimierer.Kandidatenzahl(config).Zulaessig);
        var ex = Assert.Throws<ArgumentException>(() => FlottenOptimierer.Rechne(Eingang(), config));
        Assert.Contains("nicht gekuerzt", ex.Message);
        Assert.Contains("Feinraster", ex.Message);

        // Dasselbe Grobraster OHNE zweite Phase bleibt zulaessig — die Gegenprobe.
        FlottenStudieKonfiguration ohne = Konfiguration(feinraster: false, grenze: 3);
        Assert.True(FlottenOptimierer.Kandidatenzahl(ohne).Zulaessig);
    }

    /// <summary>Ein unbrauchbarer Suchbereich liefert keine erfundene Zahl.</summary>
    [Fact]
    public void Ein_unbrauchbarer_Bereich_meldet_sich_als_ungueltig()
    {
        FlottenStudieKonfiguration config = Konfiguration(feinraster: true, grenze: 1000);
        config.Auslegung.Achsen[0].KapazitaetBisKWh = 1;       // bis < von

        FlottenKandidatenzahl zahl = FlottenOptimierer.Kandidatenzahl(config);

        Assert.False(zahl.Gueltig);
        Assert.False(zahl.Zulaessig);
    }

    // =====================================================================
    //  3. Der Lauf: Phasenmarke, Gewinnregel, Abbruch
    // =====================================================================

    /// <summary>
    /// Der Lauf rechnet beide Phasen: Die Kandidatenliste führt Grob- UND Feinpunkte,
    /// jeder mit seiner Phase, und ein Feinpunkt hat keine Stelle im Grobgitter.
    /// </summary>
    [Fact]
    public void Der_Lauf_traegt_seine_Kandidaten_mit_Phasenmarke()
    {
        FlottenAuslegungErgebnis ergebnis =
            FlottenOptimierer.Rechne(Eingang(), Konfiguration(feinraster: true, grenze: 1000));

        Assert.True(ergebnis.FeinrasterGerechnet);
        var grob = Gerastert(ergebnis).Where(k => k.Phase == FlottenKandidatPhase.Grob).ToList();
        var fein = Gerastert(ergebnis).Where(k => k.Phase == FlottenKandidatPhase.Fein).ToList();

        Assert.Equal(2, grob.Count);
        Assert.NotEmpty(fein);
        Assert.All(fein, k => Assert.Equal(-1, k.Rasterzeile));
        Assert.All(fein, k => Assert.Equal(-1, k.Rasterspalte));
        Assert.All(grob, k => Assert.True(k.Rasterzeile >= 0));

        // Die zweite Phase haelt sich an ihr Fenster — und an die zweite Achse des
        // Grob-Optimums: Ihre Kandidaten tragen DIESELBE C-Rate.
        double cRateGrobOptimum = ergebnis.Kandidaten
            .Where(k => k.Phase == FlottenKandidatPhase.Grob && k.Einheiten.Count > 0)
            .OrderByDescending(k => k.KapitalwertEuro).First().CRate;
        Assert.All(fein, k => Assert.Equal(cRateGrobOptimum, k.CRate, 6));
    }

    /// <summary>
    /// Der ABGESCHALTETE Schalter ist die Gegenprobe: dieselbe Suche ohne zweite Phase
    /// führt ausschliesslich Grobpunkte.
    /// </summary>
    [Fact]
    public void Ohne_Feinraster_bleibt_der_Lauf_beim_Grobraster()
    {
        FlottenAuslegungErgebnis ergebnis =
            FlottenOptimierer.Rechne(Eingang(), Konfiguration(feinraster: false, grenze: 1000));

        Assert.False(ergebnis.FeinrasterGerechnet);
        Assert.Equal(2, Gerastert(ergebnis).Count());
        Assert.All(Gerastert(ergebnis), k => Assert.Equal(FlottenKandidatPhase.Grob, k.Phase));
    }

    /// <summary>
    /// <b>Das Feinraster gewinnt nur STRIKT.</b> Das Fenster um das Grob-Optimum enthält
    /// dessen Größe selbst, der Feinpunkt dort ist Zeichen für Zeichen dieselbe Hardware —
    /// und trägt deshalb denselben Kapitalwert. Weil Phase 2 NACH Phase 1 läuft und der
    /// Vergleich strikt ist, bleibt der GROBPUNKT der Beste.
    /// </summary>
    [Fact]
    public void Bei_Gleichstand_bleibt_das_Grobraster_massgeblich()
    {
        FlottenAuslegungErgebnis ergebnis =
            FlottenOptimierer.Rechne(Eingang(), Konfiguration(feinraster: true, grenze: 1000));

        FlottenKandidatZusammenfassung bester = Assert.IsType<FlottenKandidatZusammenfassung>(
            ergebnis.BesterKandidat);
        Assert.Equal(FlottenKandidatPhase.Grob, bester.Phase);

        // Der Zwilling aus Phase 2 steht daneben, mit demselben Wert — der Gleichstand,
        // um den es geht.
        FlottenKandidatZusammenfassung zwilling = Assert.Single(Gerastert(ergebnis),
            k => k.Phase == FlottenKandidatPhase.Fein
                 && Math.Abs(k.KapazitaetKWh - bester.KapazitaetKWh) < 1e-9);
        Assert.Equal(bester.KapitalwertEuro, zwilling.KapitalwertEuro, 9);
    }

    /// <summary>
    /// Ein Abbruch in der ZWEITEN Phase wirkt wie in der ersten: Der Lauf endet mit
    /// <see cref="OperationCanceledException"/> und nicht mit einem halben Ergebnis.
    /// </summary>
    [Fact]
    public void Der_Abbruch_wirkt_auch_in_der_zweiten_Phase()
    {
        FlottenStudieKonfiguration config = Konfiguration(feinraster: true, grenze: 1000);
        long grob = FlottenOptimierer.Kandidatenzahl(config).Grob;

        using var quelle = new CancellationTokenSource();
        int gezaehlt = 0;
        var melder = new Fortschrittsmelder(_ =>
        {
            gezaehlt++;
            if (gezaehlt == grob) quelle.Cancel();     // genau nach der ersten Phase
        });

        Assert.Throws<OperationCanceledException>(
            () => FlottenOptimierer.Rechne(Eingang(), config, null, melder, quelle.Token));
        Assert.Equal(grob, gezaehlt);
    }

    /// <summary>
    /// Der FORTSCHRITT läuft über beide Phasen: Die gemeldete Gesamtzahl ist am Ende die
    /// wirklich gerechnete, und der letzte Bericht erreicht sie.
    /// </summary>
    [Fact]
    public void Der_Fortschritt_laeuft_ueber_beide_Phasen()
    {
        var berichte = new List<FlottenFortschritt>();
        var melder = new Fortschrittsmelder(berichte.Add);

        FlottenAuslegungErgebnis ergebnis = FlottenOptimierer.Rechne(
            Eingang(), Konfiguration(feinraster: true, grenze: 1000), null, melder);

        Assert.NotEmpty(berichte);
        FlottenFortschritt letzter = berichte[^1];
        Assert.Equal(letzter.Gesamt, letzter.Abgeschlossen);
        Assert.Equal(Gerastert(ergebnis).Count(), letzter.Abgeschlossen);
        Assert.True(ergebnis.Rechendauer >= TimeSpan.Zero);
    }

    // ================================================================= Prüfstand

    private sealed class Fortschrittsmelder : IProgress<FlottenFortschritt>
    {
        private readonly Action<FlottenFortschritt> _nimm;
        public Fortschrittsmelder(Action<FlottenFortschritt> nimm) => _nimm = nimm;
        public void Report(FlottenFortschritt wert) => _nimm(wert);
    }

    /// <summary>Die gerasterten Kandidaten — ohne die Nullvariante.</summary>
    private static IEnumerable<FlottenKandidatZusammenfassung> Gerastert(FlottenAuslegungErgebnis e)
        => e.Kandidaten.Where(k => k.Einheiten.Count > 0);

    private static FlottenAuslegungsAchse Achse(double von, double bis, double schritt) => new()
    {
        Aktiv = true,
        Modus = FlottenAuslegungsmodus.KapazitaetUndCRate,
        AnzahlVon = 1,
        AnzahlBis = 1,
        KapazitaetVonKWh = von,
        KapazitaetBisKWh = bis,
        KapazitaetSchrittKWh = schritt,
        CRateVon = 1.0,
        CRateBis = 1.0,
        CRateSchritt = 0.5,
        Vorlage = Einheit(von, von)
    };

    private static FlottenStudieKonfiguration Konfiguration(bool feinraster, int grenze) => new()
    {
        Einheiten = new List<FlottenEinheit>(),
        Optionen = new FlottenSimulationOptionen
        {
            Betriebsziel = FlottenBetriebsziel.PeakShaving,
            WirtschaftlicherPeakZielwertKw = 10,
            NetzladungErlaubt = true,
            EnergieAusgleichEuroProKWh = 0
        },
        Tarif = new FlottenTarif { LeistungspreisEuroProKw = 100 },
        // EIN Restwert der Studie, der jeden Kandidaten gleichermassen hebt: Damit liegt
        // der Kapitalwert ueberall UEBER null (sonst gewaenne die Nullvariante und es gaebe
        // keinen „besten Kandidaten", an dem sich die Gewinnregel zeigen liesse) — und er
        // ist bei allen GLEICH, was genau den Gleichstand herstellt, um den es geht.
        Wirtschaftlichkeit = new FlottenWirtschaftlichkeitEingang
        {
            Kalkulationszins = 0,
            RestwertEuro = 1000
        },
        Auslegung = new FlottenAuslegungEingang
        {
            MaximaleKandidaten = grenze,
            Feinraster = feinraster,
            Achsen = new List<FlottenAuslegungsAchse> { Achse(von: 10, bis: 20, schritt: 10) }
        }
    };

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
        JaehrlicheFixeOpexEuro = 0.0,
        DurchsatzkostenEuroProKWhEntladung = 0.01,
        InvestitionEuro = 0
    };

    private static FlottenEingang Eingang() => new()
    {
        KonfigurationId = "C",
        DatenId = "D",
        Projektjahre = new List<FlottenProjektjahr>
        {
            new() { Jahr = 2026, IstVollstaendigesJahr = true, Istwerte = Zeilen() }
        }
    };

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
