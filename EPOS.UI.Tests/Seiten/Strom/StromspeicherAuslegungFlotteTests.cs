using Bunit;
using Microsoft.Extensions.DependencyInjection;
using EPOS.UI.Dienste;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Seiten.Simulation;
using EPOS.UI.Seiten.Strom;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Seiten.Strom;

/// <summary>
/// Der MODUS „FLOTTE" der Ansicht „Stromspeicher-Auslegung" (Paket P3, Auftrag #192)
/// — die übernommenen Prüffälle des gefallenen <c>SpeicherFlottenDialog</c>.
///
/// <para><b>Was der Umzug ändert.</b> Aus vier Reitern werden vier Blätter EINER
/// Ablaufleiste, aus dem Fußknopf „Speichervergleich berechnen" wird Schritt 4. Die
/// Fachaussagen bleiben: Der Rechenlauf bekommt einen unabhängigen Stand, die
/// Projektübernahme ist ausdrücklich und nach einer Eingabeänderung gesperrt, und aus
/// dem Ergebnis führt ein Weg zurück zur Betriebsführung.</para>
/// </summary>
public sealed class StromspeicherAuslegungFlotteTests : EposBunitContext
{
    /// <summary>
    /// Die Ansicht traegt einen <c>InfoKnopf</c>; ohne Hilfedienst wirft der
    /// Blazor-Verteiler schon beim ersten Zeichnen.
    /// </summary>
    public StromspeicherAuslegungFlotteTests() => Services.AddSingleton<IHilfeDienst>(new KeineHilfe());

    /// <summary>Die Ansicht im Vorgabemodus „Flotte", Blatt 1.</summary>
    private IRenderedComponent<StromspeicherAuslegungSeite> Ansicht(StromspeicherAuslegungDienste dienste)
        => Render<StromspeicherAuslegungSeite>(p => p
            .Add(x => x.Dienste, dienste)
            .Add(x => x.PlanerVerfuegbar, true));

    private static void Rechnen(IRenderedComponent<StromspeicherAuslegungSeite> cut)
        => Auslegungshilfe.Rechenknopf(cut).Click();

    [Fact]
    public void Der_Einstieg_zeigt_den_Flotteneditor_statt_des_Einzelspeicherrasters()
    {
        var cut = Ansicht(new StromspeicherAuslegungDienste
        {
            Vorgaben = () => new SpeicherOptimierungVorgaben(),
            FlotteRechnen = (_, _) => Task.FromResult(new SpeicherFlottenErgebnis())
        });

        Assert.Single(cut.FindComponents<SpeicherFlottenEditor>());
        Assert.Empty(cut.FindComponents<EinzelspeicherSuchraum>());
        Assert.DoesNotContain("Vorbelegung: 500", cut.Markup);
    }

    /// <summary>
    /// Der Modus-Umschalter (SD‑Q1) führt auf den ANDEREN Pfad — und nur dorthin: Die
    /// zwei Rechenwege bleiben getrennt (SD‑Q2).
    /// </summary>
    [Fact]
    public void Der_Modusumschalter_wechselt_auf_den_Einzelspeicher()
    {
        var cut = Ansicht(new StromspeicherAuslegungDienste
        {
            Vorgaben = () => new SpeicherOptimierungVorgaben(),
            FlotteRechnen = (_, _) => Task.FromResult(new SpeicherFlottenErgebnis()),
            EinzelRechnen = (_, _) => Task.FromResult(new SpeicherOptimierungErgebnis())
        });

        Assert.Equal(AuslegungModus.Flotte, cut.Instance.Modus);
        Assert.Equal(Resource.FLOTTE_SEITE_BTN_FLOTTE,
                     Auslegungshilfe.Rechenknopf(cut).TextContent.Trim());

        Auslegungshilfe.Modus(cut, AuslegungModus.Einzelspeicher);

        Assert.Equal(AuslegungModus.Einzelspeicher, cut.Instance.Modus);
        Assert.Single(cut.FindComponents<EinzelspeicherSuchraum>());
        Assert.Empty(cut.FindComponents<SpeicherFlottenEditor>());
        Assert.Equal(Resource.FLOTTE_SEITE_BTN_EINZEL,
                     Auslegungshilfe.Rechenknopf(cut).TextContent.Trim());
    }

    /// <summary>
    /// Der Schalter „Speicheranzahl und Größenbereiche optimieren" benennt den
    /// Rechenknopf um — das ist die halbe Antwort auf Punkt 3 der Anwenderrückmeldung
    /// („Wie wird die Lastspitzenkappung gestartet?").
    /// </summary>
    [Fact]
    public void Der_Suchlaufschalter_benennt_den_Rechenknopf_um()
    {
        var cut = Ansicht(new StromspeicherAuslegungDienste
        {
            Vorgaben = () => new SpeicherOptimierungVorgaben(),
            FlotteRechnen = (_, _) => Task.FromResult(new SpeicherFlottenErgebnis())
        });

        Assert.Equal(Resource.FLOTTE_SEITE_BTN_FLOTTE,
                     Auslegungshilfe.Rechenknopf(cut).TextContent.Trim());

        cut.FindAll("input[type=checkbox]")
           .Single(x => x.ParentElement!.TextContent.Contains(Resource.FLOTTE_DLG_CHK_OPTIMIEREN))
           .Change(true);

        Assert.Equal(Resource.FLOTTE_SEITE_BTN_GROESSEN,
                     Auslegungshilfe.Rechenknopf(cut).TextContent.Trim());
    }

    [Fact]
    public void Berechnung_bekommt_alle_physischen_Einheiten_als_unabhaengigen_Stand()
    {
        SpeicherOptimierungEingaben? erhalten = null;
        var vorgaben = new SpeicherOptimierungVorgaben
        {
            Eingaben = new SpeicherOptimierungEingaben
            {
                Auslegung = new SpeicherAuslegungKonfiguration
                {
                    Flotte = new FlottenStudieKonfiguration
                    {
                        Einheiten = new()
                        {
                            new() { Id = "a", Name = "A", KapazitaetKWh = 20 },
                            new() { Id = "b", Name = "B", KapazitaetKWh = 80 }
                        },
                        Optionen = new() { EnergieAusgleichEuroProKWh = 0.2 },
                        Wirtschaftlichkeit = new() { ProjektjahreBeiWiederholung = 1 }
                    }
                }
            }
        };

        var cut = Ansicht(new StromspeicherAuslegungDienste
        {
            Vorgaben = () => vorgaben,
            FlotteRechnen = (e, _) =>
            {
                erhalten = e;
                return Task.FromResult(new SpeicherFlottenErgebnis { Erfolg = true });
            }
        });

        Rechnen(cut);

        Assert.Equal(2, erhalten!.Auslegung.Flotte!.Einheiten.Count);
        erhalten.Auslegung.Flotte.Einheiten[0].KapazitaetKWh = 999;
        Assert.Equal(20, vorgaben.Eingaben.Auslegung.Flotte!.Einheiten[0].KapazitaetKWh);

        // Ein gelungener Lauf fuehrt von selbst auf das Ergebnisblatt.
        Assert.Equal(AuslegungSchritt.Ergebnis, cut.Instance.Schritt);
        Assert.Single(cut.FindComponents<SpeicherFlottenErgebnisAnsicht>());
    }

    [Fact]
    public async Task Projektuebernahme_ist_explizit_und_nach_Eingabeaenderung_gesperrt()
    {
        int uebernahmen = 0;
        FlottenStudieKonfiguration config = Einheitenflotte();
        SpeicherFlottenErgebnis ergebnis = Gerechnet(config);

        var cut = Ansicht(new StromspeicherAuslegungDienste
        {
            Vorgaben = () => new SpeicherOptimierungVorgaben
            { Eingaben = new() { Auslegung = new() { Flotte = config } } },
            FlotteRechnen = (_, _) => Task.FromResult(ergebnis),
            ProjektflotteAktivieren = e =>
            {
                Assert.Same(ergebnis, e);
                uebernahmen++;
                return Task.FromResult("");
            }
        });

        Rechnen(cut);
        Assert.Equal(0, uebernahmen);

        cut.FindAll("button").Single(b => b.TextContent.Contains("für die Projektsimulation aktivieren")).Click();
        Assert.Equal(1, uebernahmen);
        Assert.Contains("Bitte die Projektsimulation neu berechnen", cut.Markup);

        // Zurueck auf Blatt 1, dort etwas aendern - das Ergebnis ist danach veraltet.
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Speicher);
        await cut.InvokeAsync(() =>
            cut.FindComponent<SpeicherFlottenEditor>().Instance.WertChanged.InvokeAsync(config));
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Ergebnis);

        Assert.True(cut.Instance.Veraltet);
        Assert.True(cut.FindAll("button")
            .Single(b => b.TextContent.Contains("für die Projektsimulation aktivieren"))
            .HasAttribute("disabled"));
    }

    /// <summary>
    /// AUFTRAG #184 (Konzept 2.2): Der Betriebseditor steht nicht mehr im Ergebnis. Das
    /// Ergebnis nennt die berechnete Betriebsführung und meldet den Wunsch, sie zu
    /// ändern — die Ansicht wechselt darauf auf Schritt 3, wo der Editor steht.
    /// </summary>
    [Fact]
    public async Task Aus_dem_Ergebnis_fuehrt_ein_Weg_zurueck_zur_Betriebsfuehrung()
    {
        FlottenStudieKonfiguration config = Einheitenflotte();
        config.Optionen.Betriebsziel = FlottenBetriebsziel.PeakShaving;
        config.Optionen.WirtschaftlicherPeakZielwertKw = 8;
        SpeicherFlottenErgebnis ergebnis = Gerechnet(config);

        var cut = Ansicht(new StromspeicherAuslegungDienste
        {
            Vorgaben = () => new SpeicherOptimierungVorgaben
            { Eingaben = new() { Auslegung = new() { Flotte = config } } },
            FlotteRechnen = (_, _) => Task.FromResult(ergebnis)
        });

        Rechnen(cut);

        // Auf dem Ergebnisblatt steht KEIN Betriebseditor mehr - nur der Weg dorthin.
        Assert.Empty(cut.FindComponents<SpeicherFlottenBetriebEditor>());

        await cut.FindAll("button")
                 .Single(b => b.TextContent.Trim() == Resource.FLOTTE_ERG_BTN_BETRIEB_AENDERN)
                 .ClickAsync(new());

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(AuslegungSchritt.Betrieb, cut.Instance.Schritt);
            Assert.Single(cut.FindComponents<SpeicherFlottenBetriebEditor>());
        });
    }

    /// <summary>
    /// Der Betriebseditor steht GENAU EINMAL in der Ansicht — auf Schritt 3. Das Blatt
    /// „Speicher" führt ihn ausdrücklich nicht (<c>BetriebZeigen="false"</c>): Zweimal
    /// dieselben sechs Felder wären die Doppelung, die Konzept 2.2 beseitigt hat.
    /// </summary>
    [Fact]
    public void Der_Betriebseditor_steht_genau_auf_Schritt_drei()
    {
        var cut = Ansicht(new StromspeicherAuslegungDienste
        {
            Vorgaben = () => new SpeicherOptimierungVorgaben(),
            FlotteRechnen = (_, _) => Task.FromResult(new SpeicherFlottenErgebnis())
        });

        Assert.Empty(cut.FindComponents<SpeicherFlottenBetriebEditor>());

        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Betrieb);
        Assert.Single(cut.FindComponents<SpeicherFlottenBetriebEditor>());
    }

    // =====================================================================
    //  Hilfen
    // =====================================================================

    private static FlottenStudieKonfiguration Einheitenflotte() => new()
    {
        Einheiten = new()
        {
            new()
            {
                Id = "a", Name = "A", KapazitaetKWh = 20, LadeleistungKw = 5, EntladeleistungKw = 5,
                Ladewirkungsgrad = 0.95, Entladewirkungsgrad = 0.95,
                SocMin = 0.1, SocMax = 0.9, SocStart = 0.5
            }
        },
        Optionen = new() { EnergieAusgleichEuroProKWh = 0.2 },
        Wirtschaftlichkeit = new() { ProjektjahreBeiWiederholung = 1 }
    };

    private static SpeicherFlottenErgebnis Gerechnet(FlottenStudieKonfiguration config)
    {
        var input = new FlottenEingang
        {
            DatenId = "Test",
            Istwerte = new()
            {
                new FlottenNetzintervall { Zeitstempel = DateTimeOffset.Parse("2026-01-01T00:00:00Z"), LastKw = 10 },
                new FlottenNetzintervall { Zeitstempel = DateTimeOffset.Parse("2026-01-01T00:15:00Z"), LastKw = 10 }
            }
        };
        return new SpeicherFlottenErgebnis
        {
            Erfolg = true,
            Konfiguration = config,
            Studie = FlottenSimulator.Simuliere(input, config)
        };
    }
}
