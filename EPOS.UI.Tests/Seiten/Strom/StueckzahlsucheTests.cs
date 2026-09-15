using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Strom;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Seiten.Strom;

/// <summary>
/// DIE ZWEITE SUCHMETHODE „Stückzahl suchen" in der Ansicht (Auftrag #247,
/// Anwenderentscheid SD‑E‑10 vom 12.09.2026, Konzept „Stromspeicher-Dialoge" 8.3/8.4).
///
/// <para><b>Was hier geprüft wird</b> — die drei Stellen, an denen sich die Methode in
/// der Oberfläche auswirkt: der Kasten „Bestes Ergebnis" nennt die BESTÜCKUNG statt
/// Kapazität · C-Rate, die Ergebnissicht zeigt die Stückzahlkurve statt der Rasterkarte,
/// und „Kandidat übernehmen" setzt die STÜCKZAHLEN in die Karten (AnzahlVon =
/// AnzahlBis = n) statt die Flotte auszutauschen.</para>
///
/// <para><b>Dazu Schritt 1</b>: das Auswahlkästchen je Einheitenkarte und der Knopf
/// „Ausgewählte Einheiten in Projekt übernehmen" (SD‑Q15) — der Weg, aus einer
/// gefundenen Bestückung Speicheranlagen des Projekts zu machen.</para>
/// </summary>
public sealed class StueckzahlsucheTests : EposBunitContext
{
    /// <summary>Die Ansicht trägt einen <c>InfoKnopf</c> und braucht deshalb den Hilfedienst.</summary>
    public StueckzahlsucheTests()
        => Services.AddSingleton<IHilfeDienst>(new KeineHilfe());

    // =====================================================================
    //  Der Kasten „Bestes Ergebnis" und die Ergebnissicht
    // =====================================================================

    /// <summary>
    /// Unter „Stückzahl suchen" nennt die zweite Karte die BESTÜCKUNG („2 × Growatt")
    /// und darunter, was daraus wird („= 258 kWh · 200 kW") — Kapazität und C-Rate wären
    /// dort die Antwort auf eine Frage, die der Lauf nicht gestellt hat (Konzept 8.4).
    /// </summary>
    [Fact]
    public void Der_Kasten_nennt_unter_Stueckzahl_die_Bestueckung()
    {
        var cut = Gerechnet();

        IElement kasten = cut.Find(".epos-flotte-bestes");
        Assert.Contains(Resource.FLOTTE_OPT_BEST_BESTUECKUNG, kasten.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain(Resource.FLOTTE_OPT_BEST_GROESSE, kasten.TextContent, StringComparison.Ordinal);
        Assert.Contains("2 × Growatt", kasten.TextContent, StringComparison.Ordinal);
        Assert.Contains("258", kasten.TextContent, StringComparison.Ordinal);

        // Und die Phasenmarke fehlt: Ein Feinraster gibt es unter S nicht.
        Assert.Empty(cut.FindAll(".epos-flotte-phasenmarke"));
    }

    /// <summary>
    /// Die Ergebnissicht zeigt unter S die KURVE „Kapitalwert über Stückzahl" — und
    /// weder Rasterkarte noch Schnitte noch ihre zwei Schieber (SD‑Q17).
    /// </summary>
    [Fact]
    public void Die_Ergebnissicht_zeigt_unter_Stueckzahl_die_Kurve()
    {
        var cut = Gerechnet();

        IRenderedComponent<SpeicherFlottenGroessenAnsicht> sicht =
            cut.FindComponent<SpeicherFlottenGroessenAnsicht>();

        Assert.Equal(FlottenSuchmethode.Stueckzahl, sicht.Instance.Methode);
        Assert.Contains(Resource.FLOTTE_STUECK_BILD_ALT, cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("input[type=range]"));

        // Die Kandidatentabelle bleibt — sie ist die einzige Stelle, an der man eine
        // Variante UEBERNIMMT.
        Assert.Single(cut.FindAll("table.epos-flotte-groessen-tabelle"));
    }

    /// <summary>
    /// Die Gegenprobe: Derselbe Lauf unter „Größe suchen" zeigt die Rasterkarte samt
    /// Schiebern und NICHT die Stückzahlkurve.
    /// </summary>
    [Fact]
    public void Unter_Groesse_steht_weiterhin_die_Rasterkarte()
    {
        var cut = Gerechnet(FlottenSuchmethode.Groesse);

        Assert.Equal(FlottenSuchmethode.Groesse,
                     cut.FindComponent<SpeicherFlottenGroessenAnsicht>().Instance.Methode);
        Assert.DoesNotContain(Resource.FLOTTE_STUECK_BILD_ALT, cut.Markup, StringComparison.Ordinal);
        Assert.NotEmpty(cut.FindAll("input[type=range]"));
    }

    // =====================================================================
    //  „Kandidat übernehmen" unter S
    // =====================================================================

    /// <summary>
    /// <b>Übernommen wird die STÜCKZAHL</b> (Auftrag #247): Der gewählte Kandidat setzt
    /// <c>AnzahlVon = AnzahlBis = n</c> in der Karte der variierten Einheit; die Einheit
    /// selbst bleibt unverändert — ihre Größe hat der Lauf gar nicht variiert.
    /// </summary>
    [Fact]
    public void Kandidat_uebernehmen_setzt_unter_Stueckzahl_die_Stueckzahl()
    {
        var cut = Gerechnet();

        Uebernehmen(cut, "n=3");

        FlottenStudieKonfiguration flotte = cut.Instance.Eingaben.Auslegung!.Flotte!;
        FlottenAuslegungsAchse achse = Assert.Single(flotte.Auslegung.Achsen);
        Assert.Equal(3, achse.AnzahlVon);
        Assert.Equal(3, achse.AnzahlBis);

        // Die Einheit bleibt, wie sie war — eine Groesse hat niemand gesucht.
        FlottenEinheit einheit = Assert.Single(flotte.Einheiten);
        Assert.Equal(129.0, einheit.KapazitaetKWh, 9);

        // Und die Suche bleibt eingeschaltet: Anders als unter G ist die Karte weiter
        // der Suchraum, nur mit festgesetzter Stueckzahl.
        Assert.True(cut.Instance.Eingaben.Auslegung!.FlottenGroessenOptimieren);
        Assert.Equal(AuslegungSchritt.Speicher, cut.Instance.Schritt);
        Assert.True(cut.Instance.Veraltet);
    }

    // =====================================================================
    //  Schritt 1: Auswahl und Übernahmeknopf (SD‑Q15)
    // =====================================================================

    /// <summary>
    /// Der Knopf ruft den Delegaten mit den GEWÄHLTEN Einheiten und ihren Stückzahlen —
    /// und erst, nachdem die Rückfrage bejaht ist.
    /// </summary>
    [Fact]
    public void Der_Uebernahmeknopf_ruft_den_Delegaten_mit_den_gewaehlten_Einheiten()
    {
        IReadOnlyList<FlottenEinheit>? erhalten = null;
        IReadOnlyList<int>? stueckzahlen = null;

        var cut = Schritt1((einheiten, zahlen) =>
        {
            erhalten = einheiten;
            stueckzahlen = zahlen;
            return new FlottenUebernahmeErgebnis(true, "", new[]
            {
                new FlottenUebernahmeAnlage(einheiten[0].Id, 4711, 99, "Growatt WIT", true, 1)
            });
        });

        // Ohne Auswahl passiert nichts — der Knopf nennt seinen Grund.
        Uebernahmeknopf(cut).Click();
        Assert.Null(erhalten);

        Auswahlkaestchen(cut).Change(true);
        Uebernahmeknopf(cut).Click();

        // Die Rueckfrage steht und nennt die zwei Zahlen; geschrieben ist noch nichts.
        Assert.Null(erhalten);
        Assert.Contains(Resource.FLOTTE_ED_UEBERNAHME_TITEL, cut.Markup, StringComparison.Ordinal);

        Bejahen(cut);

        Assert.NotNull(erhalten);
        Assert.Equal("Growatt WIT", Assert.Single(erhalten!).Name);
        Assert.Equal(new[] { 2 }, stueckzahlen);          // AnzahlVon der Achse

        // Danach trägt die Einheit ihren Anlagenbezug — Badge und Nachzugszeile stimmen.
        Assert.Equal("4711",
                     Assert.Single(cut.Instance.Eingaben.Auslegung!.Flotte!.Einheiten).AnlageId);

        // Und die Seite meldet, was geschehen ist (die EINE Meldezeile über der
        // Ablaufleiste, nicht eine zweite im Editor).
        Assert.Contains(string.Format(Kultur, Resource.FLOTTE_ED_UEBERNAHME_ERFOLG, 1, 0),
                        cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Im LESEMODUS ist der Knopf gesperrt und nennt seinen Grund</b> — weich
    /// gesperrt (<c>aria-disabled</c>), damit der Grund überhaupt ankommt (W16b‑E‑6).
    /// </summary>
    [Fact]
    public void Im_Lesemodus_bleibt_der_Uebernahmeknopf_wirkungslos_und_nennt_den_Grund()
    {
        bool gerufen = false;
        var cut = Schritt1((_, _) =>
        {
            gerufen = true;
            return new FlottenUebernahmeErgebnis(true, "", Array.Empty<FlottenUebernahmeAnlage>());
        }, schreibgeschuetzt: true);

        Auswahlkaestchen(cut).Change(true);
        IElement knopf = Uebernahmeknopf(cut);
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Equal(Resource.FLOTTE_ED_UEBERNAHME_GESPERRT, knopf.GetAttribute("title"));

        knopf.Click();

        Assert.False(gerufen);
        Assert.DoesNotContain(Resource.FLOTTE_ED_UEBERNAHME_TITEL, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_ED_UEBERNAHME_GESPERRT, cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Ohne Delegat kein Knopf</b> (Hausregel „kein Delegat, kein Bedienelement") —
    /// dann steht auch kein Auswahlkästchen da. So ist es im Reiter „Stromspeicher", der
    /// dasselbe Blatt ohne Projektwege führt.
    /// </summary>
    [Fact]
    public void Ohne_Delegat_gibt_es_weder_Knopf_noch_Kaestchen()
    {
        var cut = Schritt1(null);

        Assert.Empty(cut.FindAll("input.epos-flotte-einheit__wahl"));
        Assert.DoesNotContain(Resource.FLOTTE_ED_UEBERNAHME, cut.Markup, StringComparison.Ordinal);
    }

    // ================================================================= Prüfstand

    private static CultureInfo Kultur => CultureInfo.CurrentCulture;

    /// <summary>Die Ansicht nach einem Stückzahllauf mit drei Kandidaten (1, 2, 3 Stück).</summary>
    private IRenderedComponent<StromspeicherAuslegungSeite> Gerechnet(
        FlottenSuchmethode methode = FlottenSuchmethode.Stueckzahl)
    {
        FlottenKandidatZusammenfassung bester = Kandidat(2, 41280);
        var ergebnis = new SpeicherFlottenErgebnis
        {
            Erfolg = true,
            Konfiguration = Flotte(methode),
            Auslegung = new FlottenAuslegungErgebnis
            {
                Aussage = "Beste Variante im geprueften endlichen Raster",
                Kandidaten = new List<FlottenKandidatZusammenfassung>
                {
                    Kandidat(1, 28400), bester, Kandidat(3, 33700)
                },
                BesterKandidat = bester,
                Rechendauer = TimeSpan.FromSeconds(1.2)
            }
        };

        var vorgaben = new SpeicherOptimierungVorgaben
        {
            Eingaben = new SpeicherOptimierungEingaben
            {
                Auslegung = new SpeicherAuslegungKonfiguration
                {
                    Flotte = Flotte(methode),
                    FlottenGroessenOptimieren = true
                }
            }
        };

        var cut = Render<StromspeicherAuslegungSeite>(p => p
            .Add(x => x.Dienste, new StromspeicherAuslegungDienste
            {
                Vorgaben = () => vorgaben,
                FlotteRechnen = (_, _) => Task.FromResult(ergebnis)
            })
            .Add(x => x.PlanerVerfuegbar, true));

        // Nach dem Lauf steht Schritt 5 vorn; dort sitzen seit #273 der Kasten „Bestes
        // Ergebnis" und die Groessen-Sicht.
        Auslegungshilfe.Rechenknopf(cut).Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Flottenergebnis));
        return cut;
    }

    /// <summary>Die Ansicht auf Schritt 1, mit oder ohne Übernahmeweg.</summary>
    private IRenderedComponent<StromspeicherAuslegungSeite> Schritt1(
        Func<IReadOnlyList<FlottenEinheit>, IReadOnlyList<int>, FlottenUebernahmeErgebnis>? uebernehmen,
        bool schreibgeschuetzt = false)
    {
        var vorgaben = new SpeicherOptimierungVorgaben
        {
            Eingaben = new SpeicherOptimierungEingaben
            {
                Auslegung = new SpeicherAuslegungKonfiguration
                {
                    Flotte = Flotte(FlottenSuchmethode.Stueckzahl),
                    FlottenGroessenOptimieren = true
                }
            }
        };

        return Render<StromspeicherAuslegungSeite>(p => p
            .Add(x => x.Dienste, new StromspeicherAuslegungDienste
            {
                Vorgaben = () => vorgaben,
                FlotteRechnen = (_, _) => Task.FromResult(new SpeicherFlottenErgebnis()),
                EinheitenUebernehmen = uebernehmen,
                Schreibgeschuetzt = () => schreibgeschuetzt
            })
            .Add(x => x.PlanerVerfuegbar, true));
    }

    private static IElement Uebernahmeknopf(IRenderedComponent<StromspeicherAuslegungSeite> cut)
        => cut.FindAll("button")
              .Single(b => b.TextContent.Trim() == Resource.FLOTTE_ED_UEBERNAHME);

    private static IElement Auswahlkaestchen(IRenderedComponent<StromspeicherAuslegungSeite> cut)
        => cut.Find("input.epos-flotte-einheit__wahl");

    /// <summary>Das „Ja" der Rückfrage — der erste Knopf ihrer Leiste.</summary>
    private static void Bejahen(IRenderedComponent<StromspeicherAuslegungSeite> cut)
        => cut.Find(".epos-rueckfrage .epos-leiste button").Click();

    /// <summary>Klickt „übernehmen" in der Kandidatenzeile mit dieser Kennung.</summary>
    private static void Uebernehmen(IRenderedComponent<StromspeicherAuslegungSeite> cut, string kandidat)
    {
        IElement zeile = cut.FindAll(".epos-flotte-groessen-tabelle tbody tr")
            .Single(x => x.TextContent.Contains(kandidat, StringComparison.Ordinal));
        zeile.QuerySelector(".epos-zellenaktionen button")!.Click();
    }

    private static FlottenKandidatZusammenfassung Kandidat(int stueck, double kapitalwert) => new()
    {
        KandidatId = "n=" + stueck.ToString(CultureInfo.InvariantCulture),
        Betriebsziel = FlottenBetriebsziel.PeakShaving,
        Zulaessig = true,
        KapitalwertEuro = kapitalwert,
        KapazitaetKWh = 129.0 * stueck,
        LadeleistungKw = 100.0 * stueck,
        EntladeleistungKw = 100.0 * stueck,
        ErsparnisEuroJahr = 9640,
        Stueckzahlen = new List<int> { stueck },
        Einheiten = Enumerable.Range(0, stueck).Select(i => new FlottenKandidatEinheit
        {
            Id = "a-A1-N" + (i + 1).ToString(CultureInfo.InvariantCulture),
            KapazitaetKWh = 129.0, LadeleistungKw = 100.0, EntladeleistungKw = 100.0
        }).ToList()
    };

    /// <summary>EIN Gerät des Prüfbestands der Größensuche.</summary>
    private static FlottenGeraetekandidat Geraet(double kWh, double kW) => new()
    {
        Quellkennung = kWh.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
        Geraet = new FlottenEinheit
        {
            Id = "G" + kWh.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
            Name = "Speicher " + kWh.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
            KapazitaetKWh = kWh, LadeleistungKw = kW, EntladeleistungKw = kW,
            Ladewirkungsgrad = 0.95, Entladewirkungsgrad = 0.95,
            SocMin = 0.1, SocMax = 0.9, SocStart = 0.5
        }
    };

    private static FlottenStudieKonfiguration Flotte(FlottenSuchmethode methode)
    {
        var einheit = new FlottenEinheit
        {
            Id = "a", Name = "Growatt WIT", KapazitaetKWh = 129,
            LadeleistungKw = 100, EntladeleistungKw = 100,
            Ladewirkungsgrad = 0.95, Entladewirkungsgrad = 0.95,
            SocMin = 0.1, SocMax = 0.9, SocStart = 0.5
        };
        return new FlottenStudieKonfiguration
        {
            Einheiten = { einheit },
            Optionen = new FlottenSimulationOptionen
            {
                Betriebsziel = FlottenBetriebsziel.PeakShaving,
                EnergieAusgleichEuroProKWh = 0.3
            },
            Wirtschaftlichkeit = new FlottenWirtschaftlichkeitEingang
            { Kalkulationszins = 0.03, ProjektjahreBeiWiederholung = 20 },
            Auslegung = new FlottenAuslegungEingang
            {
                MaximaleKandidaten = 10000,
                Feinraster = false,
                Suchmethode = methode,
                Achsen =
                {
                    new FlottenAuslegungsAchse
                    {
                        Aktiv = true, Modus = FlottenAuslegungsmodus.KapazitaetUndLeistung,
                        AnzahlVon = 2, AnzahlBis = 4,
                        KapazitaetVonKWh = 100, KapazitaetBisKWh = 200,
                        LeistungVonKw = 80, LeistungBisKw = 120,
                        Geraete = { Geraet(100, 80), Geraet(150, 100), Geraet(200, 120) },
                        Vorlage = einheit
                    }
                }
            }
        };
    }
}
