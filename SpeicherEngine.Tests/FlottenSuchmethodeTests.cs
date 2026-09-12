using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using SpeicherEngine;
using Xunit;

namespace SpeicherEngine.Tests;

/// <summary>
/// AUFTRAG #247 (Anwenderentscheid SD‑E‑10, 12.09.2026): die ZWEI SUCHMETHODEN der
/// Rastersuche — „Größe suchen" (G) und „Stückzahl suchen" (S) — und die dritte Wahl
/// „Nur bewerten".
///
/// <para><b>Woher die Regel kommt</b> (Konzept „Stromspeicher-Dialoge" 8.1–8.3). Der
/// Anwender gab die Station „4 Optimierung" zurück: <i>„Es ergibt keinen Sinn, die
/// Variation der Leistung, Kapazität … bei einem vorgegebenen Speicher vorzunehmen"</i>
/// und <i>„Für mehrere Speicher ist die Variante die Anzahl der Speicher die Variation …
/// Das wäre eine zweite Methode."</i> Bis #247 lief <c>BildeAchse</c> Stückzahl × erste
/// Größe × zweite Größe in EINEM Lauf; ein Katalogspeicher bekam dabei Fantasiegrößen mit
/// den Kostensätzen seines Geräts.</para>
///
/// <para><b>Das Prüfmuster ist vierteilig</b> und steht so im Stufenplan 8.6:</para>
/// <list type="number">
/// <item>Die Kandidatenzahl je Methode — das Beispiel des Konzepts: 266 unter G, 4 unter
/// S, 1 unter „Bewerten".</item>
/// <item>Unter S bleibt die GRÖSSE die der Vorlage und die Stückzahl ist ganzzahlig;
/// unter G bleibt die STÜCKZAHL fest.</item>
/// <item>Ein Stand ohne das Feld lädt als <see cref="FlottenSuchmethode.Groesse"/> — der
/// Altbestand rechnet weiter wie bisher.</item>
/// <item>Die Kosten sind unter S linear in der Stückzahl: n Stück kosten n × Einheit.</item>
/// </list>
///
/// <para><b>Der Prüfstand</b> ist der der Feinrasterprobe (#224): acht Viertelstunden, die
/// Last springt zwischen 20 kW und 4 kW. Für die Kostenprobe steht das Peak-Ziel ÜBER der
/// Spitze — dann tut die Flotte nichts, und der Kapitalwert unterscheidet sich zwischen
/// zwei Kandidaten ausschliesslich um ihre Investition.</para>
/// </summary>
public sealed class FlottenSuchmethodeTests : IDisposable
{
    private readonly CultureInfo _vorher = CultureInfo.CurrentCulture;

    public FlottenSuchmethodeTests()
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
    //  1. Die Kandidatenzahl je Methode — das Beispiel des Konzepts (8.4)
    // =====================================================================

    /// <summary>
    /// Der Suchraum aus dem Bildschirmfoto des Anwenders (Kapazität 40–300 kWh in
    /// Schritten von 20, Leistung 40–400 kW in Schritten von 20, Stückzahl 1–4) ergibt
    /// unter „Größe suchen" <b>14 × 19 = 266</b> Kandidaten — die Stückzahl zählt als EIN
    /// Stützpunkt.
    /// </summary>
    [Fact]
    public void Unter_Groesse_zaehlt_die_Stueckzahl_als_ein_Stuetzpunkt()
    {
        FlottenKandidatenzahl zahl = FlottenOptimierer.Kandidatenzahl(
            Beispielraum(FlottenSuchmethode.Groesse));

        Assert.True(zahl.Gueltig);
        Assert.Equal(FlottenSuchbefund.Inordnung, zahl.Befund);
        Assert.Equal(266, zahl.Grob);
    }

    /// <summary>
    /// Derselbe Suchraum unter „Stückzahl suchen": <b>4</b> Kandidaten (1…4 Stück) — erste
    /// und zweite Größe zählen je EINEN Stützpunkt, nämlich die Vorlage.
    /// </summary>
    [Fact]
    public void Unter_Stueckzahl_zaehlen_die_zwei_Groessen_je_einen_Stuetzpunkt()
    {
        FlottenKandidatenzahl zahl = FlottenOptimierer.Kandidatenzahl(
            Beispielraum(FlottenSuchmethode.Stueckzahl));

        Assert.True(zahl.Gueltig);
        Assert.Equal(4, zahl.Grob);
        Assert.Equal(0, zahl.FeinHoechstens);        // ganze Zahlen — kein Feinraster
    }

    /// <summary>„Nur bewerten" rechnet genau EINEN Kandidaten: die eingestellte Flotte.</summary>
    [Fact]
    public void Unter_Bewerten_bleibt_genau_ein_Kandidat()
    {
        FlottenKandidatenzahl zahl = FlottenOptimierer.Kandidatenzahl(
            Beispielraum(FlottenSuchmethode.Bewerten));

        Assert.True(zahl.Gueltig);
        Assert.Equal(1, zahl.Grob);
        Assert.Equal(0, zahl.FeinHoechstens);
        Assert.Equal(FlottenSuchbefund.Inordnung, zahl.Befund);
    }

    /// <summary>
    /// Das Feinraster gibt es NUR unter „Größe suchen" — unter S ist zwischen zwei ganzen
    /// Stückzahlen nichts zu verfeinern, unter „Bewerten" wird gar nicht gerastert.
    /// </summary>
    [Fact]
    public void Das_Feinraster_gehoert_allein_der_Groessensuche()
    {
        Assert.True(FlottenOptimierer.Kandidatenzahl(
            Beispielraum(FlottenSuchmethode.Groesse, feinraster: true)).FeinHoechstens > 0);
        Assert.Equal(0, FlottenOptimierer.Kandidatenzahl(
            Beispielraum(FlottenSuchmethode.Stueckzahl, feinraster: true)).FeinHoechstens);
        Assert.Empty(FlottenOptimierer.Feinrasterwerte(Beispielachse(), 200,
                                                       FlottenSuchmethode.Stueckzahl));
        Assert.NotEmpty(FlottenOptimierer.Feinrasterwerte(Beispielachse(), 200,
                                                          FlottenSuchmethode.Groesse));
    }

    // =====================================================================
    //  2. Die Vorprüfung — benannte Ablehnungen (Konzept 8.3)
    // =====================================================================

    /// <summary>
    /// Eine Suchmethode ohne eine einzige Einheit mit „variieren" hat nichts zu suchen —
    /// die Zählregel nennt den Grund, und der Lauf lehnt ihn benannt ab.
    /// </summary>
    [Fact]
    public void Eine_Suchmethode_ohne_aktive_Achse_wird_benannt_abgelehnt()
    {
        FlottenStudieKonfiguration config = Beispielraum(FlottenSuchmethode.Stueckzahl);
        config.Auslegung.Achsen[0].Aktiv = false;

        Assert.Equal(FlottenSuchbefund.KeineAktiveAchse,
                     FlottenOptimierer.Kandidatenzahl(config).Befund);

        var ex = Assert.Throws<ArgumentException>(() => FlottenOptimierer.Rechne(Eingang(), config));
        Assert.Contains("variieren", ex.Message);

        // Die Gegenprobe: „Nur bewerten" braucht keine Achse und wird nicht abgelehnt.
        FlottenStudieKonfiguration bewerten = Beispielraum(FlottenSuchmethode.Bewerten);
        bewerten.Auslegung.Achsen[0].Aktiv = false;
        Assert.Equal(FlottenSuchbefund.Inordnung,
                     FlottenOptimierer.Kandidatenzahl(bewerten).Befund);
    }

    /// <summary>Ein Stückzahlbereich mit „bis" unter „von" ist ein EINGABEfehler.</summary>
    [Fact]
    public void Ein_leerer_Stueckzahlbereich_wird_benannt_abgelehnt()
    {
        FlottenStudieKonfiguration config = Beispielraum(FlottenSuchmethode.Stueckzahl);
        config.Auslegung.Achsen[0].AnzahlVon = 3;
        config.Auslegung.Achsen[0].AnzahlBis = 1;

        FlottenKandidatenzahl zahl = FlottenOptimierer.Kandidatenzahl(config);
        Assert.False(zahl.Gueltig);
        Assert.Equal(FlottenSuchbefund.StueckzahlbereichLeer, zahl.Befund);

        var ex = Assert.Throws<ArgumentException>(() => FlottenOptimierer.Rechne(Eingang(), config));
        Assert.Contains("Stueckzahlbereich", ex.Message);
    }

    /// <summary>Ein unbrauchbarer GRÖSSENbereich meldet sich unter G als solcher.</summary>
    [Fact]
    public void Ein_unbrauchbarer_Groessenbereich_meldet_sich_als_solcher()
    {
        FlottenStudieKonfiguration config = Beispielraum(FlottenSuchmethode.Groesse);
        config.Auslegung.Achsen[0].KapazitaetBisKWh = 10;      // bis < von

        FlottenKandidatenzahl zahl = FlottenOptimierer.Kandidatenzahl(config);
        Assert.False(zahl.Gueltig);
        Assert.Equal(FlottenSuchbefund.GroessenbereichUnbrauchbar, zahl.Befund);

        // Unter S liest die Zählregel den Größenbereich gar nicht — er ist dort fest.
        FlottenStudieKonfiguration stueck = Beispielraum(FlottenSuchmethode.Stueckzahl);
        stueck.Auslegung.Achsen[0].KapazitaetBisKWh = 10;
        Assert.True(FlottenOptimierer.Kandidatenzahl(stueck).Gueltig);
    }

    // =====================================================================
    //  3. Der Lauf: was unter S und was unter G variiert
    // =====================================================================

    /// <summary>
    /// Unter „Stückzahl suchen" trägt jeder Kandidat GANZZAHLIG viele Einheiten der
    /// Vorlagengröße — die Kapazität einer Einheit bleibt Zeichen für Zeichen die der
    /// Karte, variiert wird allein, wie viele davon stehen.
    /// </summary>
    [Fact]
    public void Unter_Stueckzahl_bleibt_die_Groesse_die_der_Vorlage()
    {
        FlottenAuslegungErgebnis ergebnis = FlottenOptimierer.Rechne(
            Eingang(), Laufraum(FlottenSuchmethode.Stueckzahl, von: 1, bis: 3));

        var kandidaten = Gerastert(ergebnis).ToList();
        Assert.Equal(3, kandidaten.Count);
        Assert.Equal(new[] { 1, 2, 3 }, kandidaten.Select(k => k.Einheiten.Count).OrderBy(x => x));

        foreach (FlottenKandidatZusammenfassung k in kandidaten)
        {
            Assert.All(k.Einheiten, e => Assert.Equal(VORLAGE_KAPAZITAET, e.KapazitaetKWh, 9));
            Assert.All(k.Einheiten, e => Assert.Equal(VORLAGE_LEISTUNG, e.EntladeleistungKw, 9));
            Assert.Equal(k.Einheiten.Count * VORLAGE_KAPAZITAET, k.KapazitaetKWh, 9);
            // Die Stückzahl steht im Kandidaten und muss nicht aus Kennungen gelesen werden.
            Assert.Equal(new[] { k.Einheiten.Count }, k.Stueckzahlen);
            // Kein Grobgitter: Die Karte der Stückzahlsuche spannen die Stückzahlen auf.
            Assert.Equal(-1, k.Rasterzeile);
            Assert.Equal(-1, k.Rasterspalte);
        }
    }

    /// <summary>
    /// Unter „Größe suchen" steht die Stückzahl fest auf dem Von-Wert der Karte — auch
    /// wenn der Stand einen Bereich führt (das Mischraster gibt es seit #247 nicht mehr).
    /// </summary>
    [Fact]
    public void Unter_Groesse_bleibt_die_Stueckzahl_fest()
    {
        FlottenAuslegungErgebnis ergebnis = FlottenOptimierer.Rechne(
            Eingang(), Laufraum(FlottenSuchmethode.Groesse, von: 2, bis: 4));

        var kandidaten = Gerastert(ergebnis).ToList();
        Assert.NotEmpty(kandidaten);
        Assert.All(kandidaten, k => Assert.Equal(2, k.Einheiten.Count));
        Assert.All(kandidaten, k => Assert.Equal(new[] { 2 }, k.Stueckzahlen));

        // Und die Größen laufen wirklich über das Raster: zwei Kapazitätsstufen.
        Assert.Equal(2, kandidaten.Select(k => Math.Round(k.Einheiten[0].KapazitaetKWh, 6))
                                  .Distinct().Count());
    }

    /// <summary>
    /// Ein Stückzahlbereich, der bei 0 beginnt, führt den Kandidaten OHNE diese Einheit
    /// mit — „0 = die Einheit entfällt" (Konzept 8.3).
    /// </summary>
    [Fact]
    public void Die_Stueckzahl_null_laesst_die_Einheit_entfallen()
    {
        FlottenAuslegungErgebnis ergebnis = FlottenOptimierer.Rechne(
            Eingang(), Laufraum(FlottenSuchmethode.Stueckzahl, von: 0, bis: 2));

        // Drei Kandidaten (0, 1, 2 Stück); der mit 0 Stück trägt keine Einheit und steht
        // deshalb — wie die vorangestellte Nullvariante — nicht in der gerasterten Menge.
        Assert.Equal(2, Gerastert(ergebnis).Count());
        Assert.Equal(4, ergebnis.Kandidaten.Count);
    }

    // =====================================================================
    //  4. Die Kosten unter S sind linear in der Stückzahl
    // =====================================================================

    /// <summary>
    /// <b>n Stück kosten n × Einheit</b> (Konzept 8.3). Gemessen an einem Prüfstand, in
    /// dem die Flotte NICHTS tut (das Peak-Ziel liegt über der Spitze): Dann unterscheiden
    /// sich zwei Kandidaten ausschliesslich um ihre Investition, und der Abstand zweier
    /// benachbarter Stückzahlen ist genau die Investition EINER Einheit.
    /// </summary>
    [Fact]
    public void Unter_Stueckzahl_sind_die_Kosten_linear()
    {
        FlottenStudieKonfiguration config = Laufraum(FlottenSuchmethode.Stueckzahl, von: 1, bis: 3);
        config.Optionen.WirtschaftlicherPeakZielwertKw = 1000;   // die Spitze wird nie erreicht
        config.Optionen.NetzladungErlaubt = false;               // und geladen wird auch nicht
        config.Wirtschaftlichkeit.RestwertEuro = 0;

        FlottenAuslegungErgebnis ergebnis = FlottenOptimierer.Rechne(Eingang(), config);

        double Kapitalwert(int stueck) => Gerastert(ergebnis)
            .Single(k => k.Einheiten.Count == stueck).KapitalwertEuro;

        double eins = Kapitalwert(1), zwei = Kapitalwert(2), drei = Kapitalwert(3);
        Assert.Equal(VORLAGE_INVESTITION, eins - zwei, 6);
        Assert.Equal(VORLAGE_INVESTITION, zwei - drei, 6);
    }

    // =====================================================================
    //  5. Ein Stand OHNE das Feld lädt als „Größe suchen"
    // =====================================================================

    /// <summary>
    /// <b>Der Altbestand rechnet weiter.</b> Ein vor #247 gespeicherter Stand führt kein
    /// Feld <c>Suchmethode</c>; System.Text.Json lässt den Eigenschaftsinitialisierer
    /// stehen, und der lautet <see cref="FlottenSuchmethode.Groesse"/>. Der Wert 0 wäre
    /// „Bewerten" gewesen und hätte jedem Altbestand still die Suche abgeschaltet.
    /// </summary>
    [Fact]
    public void Ein_Stand_ohne_Feld_laedt_als_Groessensuche()
    {
        var optionen = new JsonSerializerOptions { IncludeFields = true };

        FlottenAuslegungEingang leer =
            JsonSerializer.Deserialize<FlottenAuslegungEingang>("{}", optionen)!;
        Assert.Equal(FlottenSuchmethode.Groesse, leer.Suchmethode);

        // Derselbe Weg, den der Kern geht (SpeicherAuslegungKopie.Von): ganze
        // Studienkonfiguration, Auslegungsteil ohne das Feld.
        const string alt = """{"Auslegung":{"MaximaleKandidaten":500,"Feinraster":false}}""";
        FlottenStudieKonfiguration stand =
            JsonSerializer.Deserialize<FlottenStudieKonfiguration>(alt, optionen)!;
        Assert.Equal(FlottenSuchmethode.Groesse, stand.Auslegung.Suchmethode);
        Assert.Equal(500, stand.Auslegung.MaximaleKandidaten);
        Assert.False(stand.Auslegung.Feinraster);

        // Und ein NEUER Stand trägt sie durch die Kopie (FlottenKopie.Konfiguration).
        stand.Auslegung.Suchmethode = FlottenSuchmethode.Stueckzahl;
        string neu = JsonSerializer.Serialize(stand, optionen);
        Assert.Equal(FlottenSuchmethode.Stueckzahl,
                     JsonSerializer.Deserialize<FlottenStudieKonfiguration>(neu, optionen)!
                                   .Auslegung.Suchmethode);
    }

    // ================================================================= Prüfstand

    private const double VORLAGE_KAPAZITAET = 12.0;
    private const double VORLAGE_LEISTUNG = 6.0;
    private const double VORLAGE_INVESTITION = 1000.0;

    /// <summary>Die gerasterten Kandidaten — ohne die Nullvariante und ohne den 0-Stück-Fall.</summary>
    private static IEnumerable<FlottenKandidatZusammenfassung> Gerastert(FlottenAuslegungErgebnis e)
        => e.Kandidaten.Where(k => k.Einheiten.Count > 0);

    /// <summary>Die Achse des Bildschirmfotos: 40–300 kWh / 20, 40–400 kW / 20, 1–4 Stück.</summary>
    private static FlottenAuslegungsAchse Beispielachse() => new()
    {
        Aktiv = true,
        Modus = FlottenAuslegungsmodus.KapazitaetUndLeistung,
        AnzahlVon = 1,
        AnzahlBis = 4,
        KapazitaetVonKWh = 40, KapazitaetBisKWh = 300, KapazitaetSchrittKWh = 20,
        LeistungVonKw = 40, LeistungBisKw = 400, LeistungSchrittKw = 20,
        CRateVon = 0.5, CRateBis = 2.0, CRateSchritt = 0.5,
        Vorlage = Vorlage(129, 100)
    };

    private static FlottenStudieKonfiguration Beispielraum(FlottenSuchmethode methode,
                                                           bool feinraster = false) => new()
    {
        Einheiten = new List<FlottenEinheit>(),
        Optionen = new FlottenSimulationOptionen(),
        Auslegung = new FlottenAuslegungEingang
        {
            MaximaleKandidaten = 100000,
            Feinraster = feinraster,
            Suchmethode = methode,
            Achsen = new List<FlottenAuslegungsAchse> { Beispielachse() }
        }
    };

    /// <summary>Der rechenbare Prüfstand — kleine Zahlen, damit der Lauf kurz bleibt.</summary>
    private static FlottenStudieKonfiguration Laufraum(FlottenSuchmethode methode, int von, int bis)
        => new()
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
                Suchmethode = methode,
                Achsen = new List<FlottenAuslegungsAchse>
                {
                    new()
                    {
                        Aktiv = true,
                        Modus = FlottenAuslegungsmodus.KapazitaetUndCRate,
                        AnzahlVon = von,
                        AnzahlBis = bis,
                        KapazitaetVonKWh = 10, KapazitaetBisKWh = 20, KapazitaetSchrittKWh = 10,
                        CRateVon = 1.0, CRateBis = 1.0, CRateSchritt = 0.5,
                        Vorlage = Vorlage(VORLAGE_KAPAZITAET, VORLAGE_LEISTUNG)
                    }
                }
            }
        };

    private static FlottenEinheit Vorlage(double kapazitaet, double leistung) => new()
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
        EigeneKosten = true,
        InvestitionEuro = VORLAGE_INVESTITION
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
