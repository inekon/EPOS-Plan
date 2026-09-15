using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using SpeicherEngine;
using Xunit;

namespace SpeicherEngine.Tests;

/// <summary>
/// DIE GRÖSSENSUCHE RECHNET GERÄTE (Anwenderentscheid vom 15.09.2026: „Größensuch
/// Stromspeicher nur über Kapazität und Leistung im Katalog des Projektes oder wahlweise
/// aus Stammdaten").
///
/// <para><b>Was hier geprüft wird.</b> Die Auswahlregel
/// (<see cref="FlottenGeraetewahl.Waehle"/>) mit ihren drei Fällen — Treffer, kein
/// Treffer, gar kein Gerät —, die Bestimmtheit ihrer Reihenfolge, die benannte Umsetzung
/// eines Standes mit C-Rate-Kopplung, die neutralen Vorgaben eines Geräts ohne Kennwerte,
/// die Kandidatenzahl als Zahl der Geräte, der Wegfall der zweiten Suchphase — und zuletzt
/// am laufenden Optimierer, dass die Parameter des Geräts wirklich ankommen: zwei Geräte
/// gleicher Größe mit verschiedenen Wirkungsgraden liefern verschiedene Ergebnisse.</para>
/// </summary>
public sealed class FlottenGeraetesucheTests
{
    // =====================================================================
    //  1. Der Bereich trifft Geräte
    // =====================================================================

    /// <summary>
    /// <b>Gibt es Treffer, gibt es nur Treffer.</b> Wer einen Bereich vorgibt und Geräte
    /// darin findet, will nicht daneben rechnen — die Geräte außerhalb bleiben draußen,
    /// und jeder Treffer trägt die Abweichung 0.
    /// </summary>
    [Fact]
    public void Ein_Bereich_mit_Treffern_liefert_genau_die_Treffer()
    {
        FlottenAuslegungsAchse achse = Achse(100, 300, 50, 300, Bestand());

        var gewaehlt = FlottenGeraetewahl.Waehle(achse);

        Assert.Equal(new[] { "M100", "M200", "M300" },
                     gewaehlt.Select(g => g.Quellkennung).ToArray());
        Assert.All(gewaehlt, g => Assert.True(g.Treffer));
        Assert.All(gewaehlt, g => Assert.Equal(0.0, g.Abweichung, 12));
    }

    /// <summary>
    /// Die Grenzen zählen MIT: Ein Gerät genau auf „von" oder genau auf „bis" ist ein
    /// Treffer. Ein Bereich, der zu einem Punkt zusammenfällt, ist zulässig — dann hat der
    /// Anwender die Größe festgenagelt.
    /// </summary>
    [Fact]
    public void Die_Grenzen_zaehlen_mit_und_ein_Punktbereich_ist_zulaessig()
    {
        Assert.Equal(new[] { "M100", "M200" },
                     FlottenGeraetewahl.Waehle(Achse(100, 200, 100, 200, Bestand()))
                                       .Select(g => g.Quellkennung).ToArray());

        Assert.Equal(new[] { "M200" },
                     FlottenGeraetewahl.Waehle(Achse(200, 200, 200, 200, Bestand()))
                                       .Select(g => g.Quellkennung).ToArray());
    }

    // =====================================================================
    //  2. Der Bereich trifft keines — die nächstliegenden
    // =====================================================================

    /// <summary>
    /// <b>Trifft kein Gerät, kommen die nächstliegenden — mit sichtbarer Abweichung.</b>
    /// Der Bereich 400…500 kWh / 400…500 kW liegt über dem ganzen Bestand; am nächsten
    /// steht das größte Gerät (300 kWh / 300 kW).
    /// </summary>
    /// <remarks>
    /// Die Rechnung Stück für Stück: Bezug ist die Bereichsmitte, also 450 kWh und 450 kW.
    /// Das 300er Gerät liegt 100 kWh und 100 kW unter „von" — das macht
    /// 100/450 + 100/450 = 0,4444. Das 200er liegt 200 daneben: 0,8889.
    /// </remarks>
    [Fact]
    public void Ohne_Treffer_kommen_die_naechstliegenden_mit_ihrer_Abweichung()
    {
        var gewaehlt = FlottenGeraetewahl.Waehle(Achse(400, 500, 400, 500, Bestand()));

        Assert.Equal(4, gewaehlt.Count);                       // der ganze Bestand, geordnet
        Assert.Equal("M300", gewaehlt[0].Quellkennung);
        Assert.False(gewaehlt[0].Treffer);
        Assert.Equal(200.0 / 450.0, gewaehlt[0].Abweichung, 9);
        Assert.Equal(400.0 / 450.0, gewaehlt[1].Abweichung, 9);

        // Aufsteigend — und keine zwei gleich weit entfernten in wechselnder Ordnung.
        Assert.Equal(gewaehlt.Select(g => g.Abweichung).OrderBy(x => x).ToArray(),
                     gewaehlt.Select(g => g.Abweichung).ToArray());
    }

    /// <summary>
    /// <b>Höchstens fünf Ersatzvorschläge.</b> Wer mehr sehen will, weitet seinen Bereich
    /// — dann sind es Treffer und keine Ersatzvorschläge.
    /// </summary>
    [Fact]
    public void Es_kommen_hoechstens_fuenf_naechstliegende()
    {
        var bestand = Enumerable.Range(1, 12)
            .Select(i => Geraet("G" + i.ToString("00"), 10.0 * i, 10.0 * i)).ToList();

        var gewaehlt = FlottenGeraetewahl.Waehle(Achse(1000, 1100, 1000, 1100, bestand));

        Assert.Equal(FlottenGeraetewahl.NAECHSTLIEGENDE_HOECHSTENS, gewaehlt.Count);
        Assert.Equal("G12", gewaehlt[0].Quellkennung);         // das größte steht am nächsten
    }

    /// <summary>
    /// <b>Die Reihenfolge ist bestimmt.</b> Derselbe Bestand in anderer Eingangsreihenfolge
    /// liefert dieselbe Ausgabe: erst Abweichung, dann Kapazität, dann Leistung, zuletzt
    /// die Quellkennung. Zwei Läufe auf derselben Datenbank liefern damit dieselbe Ordnung.
    /// </summary>
    [Fact]
    public void Die_Reihenfolge_ist_bestimmt()
    {
        // Vier Geräte, die sich paarweise in Kapazität, Leistung und Kennung gleichen —
        // jedes Sortiermerkmal wird gebraucht.
        var vorwaerts = new List<FlottenGeraetekandidat>
        {
            Geraet("a", 100, 100), Geraet("b", 100, 100),
            Geraet("c", 100, 200), Geraet("d", 200, 100)
        };
        var rueckwaerts = Enumerable.Reverse(vorwaerts).ToList();

        string[] Kennungen(List<FlottenGeraetekandidat> b)
            => FlottenGeraetewahl.Waehle(Achse(100, 200, 100, 200, b))
                                 .Select(g => g.Quellkennung).ToArray();

        Assert.Equal(new[] { "a", "b", "c", "d" }, Kennungen(vorwaerts));
        Assert.Equal(new[] { "a", "b", "c", "d" }, Kennungen(rueckwaerts));
    }

    /// <summary>Ein unbrauchbarer Bereich und ein leerer Bestand liefern nichts — ohne Ausnahme.</summary>
    [Fact]
    public void Ein_unbrauchbarer_Bereich_liefert_nichts()
    {
        Assert.Empty(FlottenGeraetewahl.Waehle(Achse(300, 100, 100, 200, Bestand())));   // bis < von
        Assert.Empty(FlottenGeraetewahl.Waehle(Achse(0, 100, 100, 200, Bestand())));     // von = 0
        Assert.Empty(FlottenGeraetewahl.Waehle(Achse(100, 300, 50, 200,
                                                     new List<FlottenGeraetekandidat>())));
        Assert.Empty(FlottenGeraetewahl.Waehle(null!));
    }

    // =====================================================================
    //  3. Der Altbestand: eine C-Rate-Kopplung lädt und wird umgesetzt
    // =====================================================================

    /// <summary>
    /// <b>Ein vor der Umstellung gespeicherter Stand lädt.</b> System.Text.Json schreibt
    /// die Aufzählung als Zahl; die 1 ist die frühere Kopplung „Kapazität und C-Rate".
    /// Sie wird BENANNT umgesetzt: Der Kapazitätsbereich bleibt, der Leistungsbereich
    /// entsteht aus den Ecken <c>P = E · C</c> — 100 kWh · 0,5 C = 50 kW bis
    /// 300 kWh · 2 C = 600 kW.
    /// </summary>
    [Fact]
    public void Ein_Stand_mit_Kapazitaet_und_CRate_wird_benannt_umgesetzt()
    {
        const string alt = """
            {"Achsen":[{"Aktiv":true,"Modus":1,"AnzahlVon":1,"AnzahlBis":1,
             "KapazitaetVonKWh":100,"KapazitaetBisKWh":300,"KapazitaetSchrittKWh":50,
             "CRateVon":0.5,"CRateBis":2.0,"CRateSchritt":0.5}]}
            """;

        FlottenAuslegungEingang stand = JsonSerializer.Deserialize<FlottenAuslegungEingang>(
            alt, new JsonSerializerOptions { IncludeFields = true })!;
        Assert.Equal(FlottenAuslegungsmodus.KapazitaetUndCRate, stand.Achsen[0].Modus);

        Assert.True(FlottenAltstand.Normalisiere(stand));

        FlottenAuslegungsAchse a = stand.Achsen[0];
        Assert.Equal(FlottenAuslegungsmodus.KapazitaetUndLeistung, a.Modus);
        Assert.Equal(100.0, a.KapazitaetVonKWh, 9);
        Assert.Equal(300.0, a.KapazitaetBisKWh, 9);
        Assert.Equal(50.0, a.LeistungVonKw, 9);
        Assert.Equal(600.0, a.LeistungBisKw, 9);

        // Ein zweiter Aufruf ändert nichts mehr.
        Assert.False(FlottenAltstand.Normalisiere(stand));
    }

    /// <summary>
    /// Dieselbe Umsetzung für „Leistung und C-Rate": Der Leistungsbereich bleibt, die
    /// Kapazität entsteht als <c>E = P / C</c> — die SCHNELLSTE C-Rate ergibt die
    /// KLEINSTE Kapazität.
    /// </summary>
    [Fact]
    public void Ein_Stand_mit_Leistung_und_CRate_wird_benannt_umgesetzt()
    {
        var achse = new FlottenAuslegungsAchse
        {
            Modus = FlottenAuslegungsmodus.LeistungUndCRate,
            LeistungVonKw = 100, LeistungBisKw = 400,
            CRateVon = 0.5, CRateBis = 2.0
        };

        Assert.True(FlottenAltstand.Normalisiere(achse));

        Assert.Equal(FlottenAuslegungsmodus.KapazitaetUndLeistung, achse.Modus);
        Assert.Equal(100.0, achse.LeistungVonKw, 9);
        Assert.Equal(400.0, achse.LeistungBisKw, 9);
        Assert.Equal(50.0, achse.KapazitaetVonKWh, 9);     // 100 / 2,0
        Assert.Equal(800.0, achse.KapazitaetBisKWh, 9);    // 400 / 0,5
    }

    /// <summary>
    /// <b>Ohne brauchbare C-Raten bleibt der vorhandene Bereich stehen.</b> Eine C-Rate
    /// von 0 ergäbe einen Leistungsbereich von 0 bis 0; dann ist der gespeicherte Bereich
    /// die bessere Auskunft als eine gerechnete Null — und die Vorprüfung sagt, was fehlt.
    /// </summary>
    [Fact]
    public void Ohne_brauchbare_CRate_bleibt_der_Bereich_stehen()
    {
        var achse = new FlottenAuslegungsAchse
        {
            Modus = FlottenAuslegungsmodus.KapazitaetUndCRate,
            KapazitaetVonKWh = 100, KapazitaetBisKWh = 300,
            LeistungVonKw = 40, LeistungBisKw = 80,
            CRateVon = 0, CRateBis = 0
        };

        Assert.True(FlottenAltstand.Normalisiere(achse));
        Assert.Equal(FlottenAuslegungsmodus.KapazitaetUndLeistung, achse.Modus);
        Assert.Equal(40.0, achse.LeistungVonKw, 9);
        Assert.Equal(80.0, achse.LeistungBisKw, 9);
    }

    /// <summary>
    /// <b>Der Lauf selbst setzt um.</b> Ein Stand mit C-Rate-Kopplung stürzt nicht ab und
    /// rechnet nicht ins Leere: Er bekommt seinen Leistungsbereich und findet darin seine
    /// Geräte.
    /// </summary>
    [Fact]
    public void Der_Lauf_setzt_einen_Altstand_um_und_rechnet_weiter()
    {
        FlottenStudieKonfiguration config = Laufraum();
        FlottenAuslegungsAchse a = config.Auslegung.Achsen[0];
        a.Modus = FlottenAuslegungsmodus.KapazitaetUndCRate;
        a.LeistungVonKw = 0;                      // wie in einem alten Stand: nie gefüllt
        a.LeistungBisKw = 0;
        a.CRateVon = 1.0;
        a.CRateBis = 1.0;

        FlottenAuslegungErgebnis ergebnis = FlottenOptimierer.Rechne(Eingang(), config);

        Assert.Equal(FlottenAuslegungsmodus.KapazitaetUndLeistung, ergebnis.Achsenmodus);
        Assert.Equal(2, ergebnis.Kandidaten.Count(k => k.Einheiten.Count > 0));
    }

    // =====================================================================
    //  4. Die neutralen Vorgaben
    // =====================================================================

    /// <summary>
    /// <b>Ein Gerät ohne Kennwerte bekommt die neutralen Vorgaben und ist gekennzeichnet.</b>
    /// Sie stehen an EINER Stelle (<see cref="FlottenGeraetevorgaben"/>): Wirkungsgrade
    /// 95 %, SoC-Fenster 10…90 %, keine Alterung.
    /// </summary>
    [Fact]
    public void Ein_Geraet_ohne_Kennwerte_bekommt_die_neutralen_Vorgaben()
    {
        var nackt = new FlottenEinheit
        {
            Id = "N", Name = "ohne Kennwerte",
            KapazitaetKWh = 100, LadeleistungKw = 100, EntladeleistungKw = 100,
            Ladewirkungsgrad = 0, Entladewirkungsgrad = 0,
            SocMin = 0, SocMax = 0
        };

        Assert.True(FlottenGeraetevorgaben.LueckenFuellen(nackt));

        Assert.Equal(0.95, nackt.Ladewirkungsgrad, 12);
        Assert.Equal(0.95, nackt.Entladewirkungsgrad, 12);
        Assert.Equal(0.10, nackt.SocMin, 12);
        Assert.Equal(0.90, nackt.SocMax, 12);
        Assert.Empty(nackt.RainflowKurve);                              // keine Alterung
        Assert.Equal(0.0, nackt.GrenzverschleissEuroProKWhEntladung, 12);

        // Ein zweiter Aufruf füllt nichts mehr — und ein gepflegtes Gerät nie.
        Assert.False(FlottenGeraetevorgaben.LueckenFuellen(nackt));
    }

    /// <summary>
    /// <b>Die Kennzeichnung kommt beim Kandidaten an.</b> Ein Bestandssatz, den der Kern
    /// als „mit neutralen Vorgaben" gemeldet hat, trägt die Marke bis in die
    /// Zusammenfassung — sonst hielte der Anwender ihn für ein vollständig gepflegtes
    /// Gerät.
    /// </summary>
    [Fact]
    public void Die_Kennzeichnung_erreicht_die_Zusammenfassung()
    {
        FlottenStudieKonfiguration config = Laufraum();
        config.Auslegung.Achsen[0].Geraete = new List<FlottenGeraetekandidat>
        {
            Geraet("rein", 10, 10),
            new()
            {
                Quellkennung = "neutral",
                NeutraleKennwerte = true,
                Geraet = new FlottenEinheit
                {
                    Id = "X", Name = "neutral", KapazitaetKWh = 20,
                    LadeleistungKw = 20, EntladeleistungKw = 20
                }
            }
        };

        FlottenAuslegungErgebnis ergebnis = FlottenOptimierer.Rechne(Eingang(), config);

        var gerechnet = ergebnis.Kandidaten.Where(k => k.Einheiten.Count > 0).ToList();
        Assert.Equal(2, gerechnet.Count);
        Assert.Contains(gerechnet, k => k.Quellkennung == "neutral" && k.NeutraleKennwerte);
        Assert.Contains(gerechnet, k => k.Quellkennung == "rein" && !k.NeutraleKennwerte);
    }

    // =====================================================================
    //  5. Der Lauf rechnet wirklich die Geräte
    // =====================================================================

    /// <summary>
    /// <b>Die Parameter kommen vom Gerät an.</b> Zwei Geräte GLEICHER Größe, die sich
    /// allein im Wirkungsgrad unterscheiden, liefern verschiedene Ergebnisse — käme der
    /// Wirkungsgrad aus einer gemeinsamen Vorlage, wären beide Kapitalwerte gleich.
    /// </summary>
    [Fact]
    public void Verschiedene_Geraete_liefern_verschiedene_Ergebnisse()
    {
        FlottenStudieKonfiguration config = Laufraum();
        FlottenGeraetekandidat gut = Geraet("gut", 20, 20);
        FlottenGeraetekandidat schlecht = Geraet("schlecht", 20, 20);
        schlecht.Geraet.Ladewirkungsgrad = 0.5;
        schlecht.Geraet.Entladewirkungsgrad = 0.5;
        config.Auslegung.Achsen[0].Geraete = new List<FlottenGeraetekandidat> { gut, schlecht };

        FlottenAuslegungErgebnis ergebnis = FlottenOptimierer.Rechne(Eingang(), config);

        double Wert(string kennung) => ergebnis.Kandidaten
            .Single(k => k.Quellkennung == kennung).KapitalwertEuro;

        Assert.NotEqual(Wert("gut"), Wert("schlecht"), 6);
        Assert.True(Wert("gut") > Wert("schlecht"),
                    "Das Geraet mit dem besseren Wirkungsgrad muss besser abschneiden.");
    }

    /// <summary>
    /// <b>Es wird nichts skaliert.</b> Jede Einheit eines Kandidaten trägt Zeichen für
    /// Zeichen die Kapazität und die Leistung ihres Geräts — eine gerechnete
    /// Zwischengröße wäre ein Speicher, den es nicht gibt.
    /// </summary>
    [Fact]
    public void Der_Lauf_skaliert_nichts()
    {
        FlottenAuslegungErgebnis ergebnis = FlottenOptimierer.Rechne(Eingang(), Laufraum());

        var groessen = ergebnis.Kandidaten
            .Where(k => k.Einheiten.Count > 0)
            .Select(k => (k.Einheiten[0].KapazitaetKWh, k.Einheiten[0].EntladeleistungKw))
            .OrderBy(x => x.Item1)
            .ToArray();

        Assert.Equal(new[] { (10.0, 10.0), (20.0, 20.0) }, groessen);
    }

    /// <summary>
    /// <b>Die Kandidatenzahl ist die Zahl der Geräte</b> — und sie kann nicht mehr
    /// explodieren: Sie ist höchstens so groß wie der Bestand der Quelle mal der Zahl der
    /// Betriebsziele.
    /// </summary>
    [Fact]
    public void Die_Kandidatenzahl_ist_die_Zahl_der_Geraete()
    {
        FlottenStudieKonfiguration config = Laufraum();
        FlottenKandidatenzahl zahl = FlottenOptimierer.Kandidatenzahl(config);

        Assert.True(zahl.Gueltig);
        Assert.Equal(2, zahl.Grob);
        Assert.Equal(0, zahl.FeinHoechstens);
        Assert.Equal(2, zahl.Gesamt);
        Assert.True(zahl.Zulaessig);
    }

    // =====================================================================
    //  Der Prüfstand
    // =====================================================================

    /// <summary>Vier Geräte von 100 bis 300 kWh, Leistung gleich Kapazität.</summary>
    private static List<FlottenGeraetekandidat> Bestand() => new()
    {
        Geraet("M100", 100, 100), Geraet("M200", 200, 200),
        Geraet("M300", 300, 300), Geraet("M050", 50, 50)
    };

    private static FlottenGeraetekandidat Geraet(string id, double kWh, double kW) => new()
    {
        Quellkennung = id,
        Geraet = new FlottenEinheit
        {
            Id = "G" + id, Name = "Speicher " + id,
            KapazitaetKWh = kWh, LadeleistungKw = kW, EntladeleistungKw = kW,
            Ladewirkungsgrad = 1, Entladewirkungsgrad = 1,
            SocMin = 0.2, SocMax = 1.0, SocStart = 0.2,
            EigeneKosten = true, InvestitionEuro = 1000
        }
    };

    private static FlottenAuslegungsAchse Achse(double kapVon, double kapBis,
                                                double leiVon, double leiBis,
                                                List<FlottenGeraetekandidat> bestand) => new()
    {
        Aktiv = true,
        Modus = FlottenAuslegungsmodus.KapazitaetUndLeistung,
        AnzahlVon = 1, AnzahlBis = 1,
        KapazitaetVonKWh = kapVon, KapazitaetBisKWh = kapBis,
        LeistungVonKw = leiVon, LeistungBisKw = leiBis,
        Geraete = bestand,
        Vorlage = new FlottenEinheit { Id = "V", Name = "Vorlage" }
    };

    /// <summary>Ein rechenbarer Suchraum mit ZWEI Geräten im Bereich.</summary>
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
            Suchmethode = FlottenSuchmethode.Groesse,
            Achsen = new List<FlottenAuslegungsAchse>
            {
                Achse(10, 20, 10, 20,
                      new List<FlottenGeraetekandidat> { Geraet("klein", 10, 10), Geraet("gross", 20, 20) })
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
