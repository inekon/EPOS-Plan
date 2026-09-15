using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using SpeicherEngine;
using Xunit;

namespace SpeicherEngine.Tests;

/// <summary>
/// GERAET ODER EIGENE PARAMETER (Anwenderentscheid 15.09.2026): Unter „Groesse suchen"
/// ersetzte die Geraetewahl die Vorlage VOLLSTAENDIG — die Werte, die der Anwender in
/// Schritt 1 gesetzt hatte (Wirkungsgrade, SoC-Band, Hilfsverbrauch, Kostensaetze),
/// fielen dabei weg. Unter „Stueckzahl suchen" blieben sie stehen.
///
/// <para><b>Die eine Regel</b> steht in <see cref="FlottenGeraeteuebernahme"/>: Was der
/// Geraetesatz FUEHRT, kommt vom Geraet; alles Uebrige bleibt, wie der Anwender es
/// gesetzt hat. Die GROESSE kommt immer vom Geraet — sie ist der Gegenstand der Suche.
/// Was ein Geraetesatz nie fuehrt (Betriebs- und Durchsatzkosten, Grenzverschleiss,
/// Peak-Reserve, Ersatz, Restwert, Alterungskurve), bleibt damit ausnahmslos beim
/// Anwender.</para>
///
/// <para><b>Der Pruefstand</b> ist der der Suchmethodenprobe: acht Viertelstunden, die
/// Last springt zwischen 20 kW und 4 kW; zwei Geraete im Bereich, damit die Suche etwas
/// zu waehlen hat.</para>
/// </summary>
public sealed class FlottenGeraeteuebernahmeTests : IDisposable
{
    private readonly CultureInfo _vorher = CultureInfo.CurrentCulture;

    public FlottenGeraeteuebernahmeTests()
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
    //  1. Die Werte aus Schritt 1 kommen im Kandidaten an
    // =====================================================================

    /// <summary>
    /// <b>Der Befund selbst.</b> Ein Geraet, dessen Satz nur die Groesse fuehrt, bringt
    /// GENAU die Groesse mit; Wirkungsgrade, SoC-Band, Hilfsverbrauch, Kostensaetze,
    /// Grenzverschleiss und Betriebskosten des Anwenders stehen unveraendert im
    /// Kandidaten.
    /// </summary>
    [Fact]
    public void Ein_in_Schritt_1_gesetzter_Wert_kommt_im_Kandidaten_an()
    {
        FlottenStudieKonfiguration config = Laufraum();
        FlottenAuslegungErgebnis ergebnis = FlottenOptimierer.Rechne(Eingang(), config);

        Assert.NotNull(ergebnis.BesteKonfiguration);
        FlottenEinheit e = ergebnis.BesteKonfiguration!.Einheiten.Single();

        // Die GROESSE kommt vom Geraet — eines der beiden, nicht die der Vorlage.
        Assert.Contains(e.KapazitaetKWh, new[] { 10.0, 20.0 });
        Assert.NotEqual(VORLAGE_KAPAZITAET, e.KapazitaetKWh);

        // Alles Uebrige ist das des Anwenders.
        Assert.Equal(0.90, e.Ladewirkungsgrad, 9);
        Assert.Equal(0.88, e.Entladewirkungsgrad, 9);
        Assert.Equal(0.20, e.SocMin, 9);
        Assert.Equal(1.00, e.SocMax, 9);
        Assert.Equal(0.70, e.HilfsverbrauchKw, 9);
        Assert.Equal(0.05, e.GrenzverschleissEuroProKWhEntladung, 9);
        Assert.Equal(3.0, e.JaehrlicheOpexEuroProKw, 9);
        Assert.Equal(VORLAGE_INVESTITION, e.InvestitionEuro, 9);
    }

    /// <summary>
    /// <b>Dieselbe Zusage fuer JEDEN Kandidaten</b>, nicht nur fuer den besten: Die
    /// Herleitung gilt fuer den ganzen Lauf.
    /// </summary>
    [Fact]
    public void Jeder_Kandidat_traegt_die_Herleitung()
    {
        FlottenAuslegungErgebnis ergebnis = FlottenOptimierer.Rechne(Eingang(), Laufraum());

        var gerastert = ergebnis.Kandidaten.Where(k => k.Rasterzeile >= 0).ToList();
        Assert.NotEmpty(gerastert);
        Assert.All(gerastert, k => Assert.Equal(FlottenKennwertherkunft.Groesse, k.Kennwertherkunft));
    }

    // =====================================================================
    //  2. Was das Geraet fuehrt, gewinnt
    // =====================================================================

    /// <summary>
    /// <b>Die andere Haelfte der Regel.</b> Fuehrt der Satz Wirkungsgrade, SoC-Band,
    /// Hilfsverbrauch und Kosten, so gelten SEINE Werte — dafuer sind es Geraete.
    /// </summary>
    [Fact]
    public void Was_das_Geraet_fuehrt_ueberschreibt_die_Vorlage()
    {
        var ziel = Vorlage();
        var kandidat = new FlottenGeraetekandidat
        {
            Quellkennung = "K",
            Gefuehrt = FlottenKennwertherkunft.Groesse | FlottenKennwertherkunft.Wirkungsgrade
                     | FlottenKennwertherkunft.SocBand | FlottenKennwertherkunft.Hilfsverbrauch
                     | FlottenKennwertherkunft.Kosten,
            Geraet = new FlottenEinheit
            {
                KapazitaetKWh = 30, LadeleistungKw = 15, EntladeleistungKw = 15,
                Ladewirkungsgrad = 0.97, Entladewirkungsgrad = 0.96,
                SocMin = 0.05, SocMax = 0.95, SocStart = 0.5,
                HilfsverbrauchKw = 0.2,
                EigeneKosten = true, InvestitionEuro = 5000,
                InvestitionEuroProKWh = 300, InvestitionEuroProKw = 120
            }
        };

        FlottenKennwertherkunft herkunft = FlottenGeraeteuebernahme.Uebernehmen(ziel, kandidat);

        Assert.Equal(30, ziel.KapazitaetKWh, 9);
        Assert.Equal(0.97, ziel.Ladewirkungsgrad, 9);
        Assert.Equal(0.05, ziel.SocMin, 9);
        Assert.Equal(0.2, ziel.HilfsverbrauchKw, 9);
        Assert.Equal(5000, ziel.InvestitionEuro, 9);
        Assert.Equal(300, ziel.InvestitionEuroProKWh, 9);

        // Und was der Satz NIE fuehrt, bleibt auch hier beim Anwender.
        Assert.Equal(0.05, ziel.GrenzverschleissEuroProKWhEntladung, 9);
        Assert.Equal(3.0, ziel.JaehrlicheOpexEuroProKw, 9);

        Assert.Equal(kandidat.Gefuehrt, herkunft);
    }

    /// <summary>
    /// <b>Der Start-Ladezustand liegt immer im geltenden Band</b> — gleichgueltig, aus
    /// welcher der beiden Quellen Band und Start stammen. Hier bringt das Geraet kein
    /// Band; der Start der Vorlage liegt unter deren eigener Untergrenze und wird
    /// hochgezogen.
    /// </summary>
    [Fact]
    public void Der_Start_wird_ins_geltende_Band_geklemmt()
    {
        var ziel = Vorlage();
        ziel.SocStart = 0.0;

        FlottenGeraeteuebernahme.Uebernehmen(ziel, new FlottenGeraetekandidat
        {
            Gefuehrt = FlottenKennwertherkunft.Groesse,
            Geraet = new FlottenEinheit { KapazitaetKWh = 30, LadeleistungKw = 15, EntladeleistungKw = 15 }
        });

        Assert.Equal(0.20, ziel.SocStart, 9);
    }

    // =====================================================================
    //  3. Was ein Satz fuehrt, liest der Kern an der rohen Einheit
    // =====================================================================

    /// <summary>
    /// <see cref="FlottenGeraeteuebernahme.Gefuehrt"/> nimmt denselben Test, mit dem die
    /// neutralen Vorgaben entscheiden, ob sie greifen muessen — es gibt keine zweite
    /// Regel dafuer, was „gefuehrt" heisst.
    /// </summary>
    [Fact]
    public void Gefuehrt_liest_den_rohen_Satz()
    {
        var roh = new FlottenEinheit
        {
            KapazitaetKWh = 10, LadeleistungKw = 5, EntladeleistungKw = 5,
            Ladewirkungsgrad = FlottenGeraetevorgaben.LADEWIRKUNGSGRAD,
            Entladewirkungsgrad = FlottenGeraetevorgaben.ENTLADEWIRKUNGSGRAD,
            SocMin = FlottenGeraetevorgaben.SOC_MIN,
            SocMax = FlottenGeraetevorgaben.SOC_MAX
        };

        // Ein Satz auf lauter neutralen Vorgaben fuehrt nur seine Groesse.
        Assert.Equal(FlottenKennwertherkunft.Groesse, FlottenGeraeteuebernahme.Gefuehrt(roh));

        roh.Ladewirkungsgrad = 0.99;
        roh.Entladewirkungsgrad = 0.98;
        roh.HilfsverbrauchKw = 0.1;
        roh.EigeneKosten = true;
        Assert.Equal(FlottenKennwertherkunft.Groesse | FlottenKennwertherkunft.Wirkungsgrade
                   | FlottenKennwertherkunft.Hilfsverbrauch | FlottenKennwertherkunft.Kosten,
                     FlottenGeraeteuebernahme.Gefuehrt(roh));

        roh.SocMin = 0.15;
        roh.SocMax = 0.85;
        Assert.True(FlottenGeraeteuebernahme.Gefuehrt(roh).HasFlag(FlottenKennwertherkunft.SocBand));
    }

    // =====================================================================
    //  Pruefstand
    // =====================================================================

    private const double VORLAGE_KAPAZITAET = 12.0;
    private const double VORLAGE_LEISTUNG = 6.0;
    private const double VORLAGE_INVESTITION = 1000.0;

    /// <summary>
    /// Die Vorlage des Anwenders — Schritt 1. Jeder Wert weicht von der neutralen
    /// Vorgabe ab, damit sichtbar wird, wer ihn gesetzt hat.
    /// </summary>
    private static FlottenEinheit Vorlage() => new()
    {
        Id = "A",
        Name = "Speicher A",
        KapazitaetKWh = VORLAGE_KAPAZITAET,
        LadeleistungKw = VORLAGE_LEISTUNG,
        EntladeleistungKw = VORLAGE_LEISTUNG,
        Ladewirkungsgrad = 0.90,
        Entladewirkungsgrad = 0.88,
        SocMin = 0.20,
        SocMax = 1.00,
        SocStart = 0.20,
        HilfsverbrauchKw = 0.70,
        GrenzverschleissEuroProKWhEntladung = 0.05,
        JaehrlicheOpexEuroProKw = 3.0,
        EigeneKosten = true,
        InvestitionEuro = VORLAGE_INVESTITION
    };

    /// <summary>Ein Geraet, dessen Satz NUR die Groesse fuehrt — der haeufige Katalogfall.</summary>
    private static FlottenGeraetekandidat Geraet(string id, double kWh, double kW) => new()
    {
        Quellkennung = id,
        Gefuehrt = FlottenKennwertherkunft.Groesse,
        Geraet = new FlottenEinheit
        {
            Id = "G" + id, Name = "Speicher " + id,
            KapazitaetKWh = kWh, LadeleistungKw = kW, EntladeleistungKw = kW
        }
    };

    private static FlottenStudieKonfiguration Laufraum() => new()
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
        Wirtschaftlichkeit = new FlottenWirtschaftlichkeitEingang
        {
            Kalkulationszins = 0,
            RestwertEuro = 20000
        },
        Auslegung = new FlottenAuslegungEingang
        {
            MaximaleKandidaten = 1000,
            Feinraster = false,
            Suchmethode = FlottenSuchmethode.Groesse,
            Achsen = new List<FlottenAuslegungsAchse>
            {
                new()
                {
                    Aktiv = true,
                    Modus = FlottenAuslegungsmodus.KapazitaetUndLeistung,
                    AnzahlVon = 1,
                    AnzahlBis = 1,
                    KapazitaetVonKWh = 10, KapazitaetBisKWh = 20,
                    LeistungVonKw = 10, LeistungBisKw = 20,
                    Geraete = new List<FlottenGeraetekandidat>
                    {
                        Geraet("L1", 10, 10), Geraet("L2", 20, 20)
                    },
                    Vorlage = Vorlage()
                }
            }
        }
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

    private static List<FlottenNetzintervall> Zeilen()
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var rows = new List<FlottenNetzintervall>();
        for (var i = 0; i < 8; i++)
        {
            rows.Add(new FlottenNetzintervall
            {
                Zeitstempel = start.AddMinutes(15 * i),
                LastKw = i % 2 == 0 ? 20 : 4,
                BezugspreisEuroProKWh = 0.30
            });
        }
        return rows;
    }
}
