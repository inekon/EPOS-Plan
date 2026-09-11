using Bunit;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Assistent;
using EPOS.UI.Seiten.Strom;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Seiten.Strom;

/// <summary>
/// DIAGNOSEBANNER, VORPRÜFUNG, PEAK-ZIEL und die RÜCKFRAGE beim Verlassen — die vier
/// Stücke, die Paket P3 (Auftrag #192) neu an die Ansicht hängt.
///
/// <para><b>Woher sie kommen.</b> Der Anwender schickte am 11.09.2026 zwei
/// Bildschirmfotos, auf denen „Mit Flotte" und „Ohne Speicher" Zahl für Zahl gleich
/// waren. Das war kein Anzeigefehler: Die Flotte hatte im ganzen Jahr weder geladen
/// noch entladen — und niemand sagte es. P1 (#183) zählt seither die Gründe, P3
/// zeigt sie und bietet die Abhilfe an.</para>
/// </summary>
public sealed class StromspeicherAuslegungBannerTests : EposBunitContext
{
    /// <summary>Die Ansicht trägt einen <c>InfoKnopf</c> und braucht deshalb den Hilfedienst.</summary>
    public StromspeicherAuslegungBannerTests() => Services.AddSingleton<IHilfeDienst>(new KeineHilfe());

    // =====================================================================
    //  Gaben
    // =====================================================================

    private static FlottenStudieKonfiguration Flotte(bool netzladung = false) => new()
    {
        Einheiten = new()
        {
            new()
            {
                Id = "a", Name = "A", KapazitaetKWh = 24, LadeleistungKw = 10, EntladeleistungKw = 12,
                Ladewirkungsgrad = 0.95, Entladewirkungsgrad = 0.95,
                SocMin = 0.1, SocMax = 0.9, SocStart = 0.1
            }
        },
        Optionen = new()
        {
            Betriebsziel = FlottenBetriebsziel.PeakShaving,
            WirtschaftlicherPeakZielwertKw = 50,
            NetzladungErlaubt = netzladung,
            EnergieAusgleichEuroProKWh = 0.2
        },
        Wirtschaftlichkeit = new() { ProjektjahreBeiWiederholung = 1 }
    };

    private static SpeicherOptimierungVorgaben Vorgaben(FlottenStudieKonfiguration flotte) => new()
    {
        Eingaben = new SpeicherOptimierungEingaben
        { Auslegung = new SpeicherAuslegungKonfiguration { Flotte = flotte } }
    };

    /// <summary>
    /// Ein Laufergebnis mit ARBEITSLOSER Flotte — genau der Fall aus dem Bildschirmfoto:
    /// null geladen, null entladen, und drei gezählte Gründe.
    /// </summary>
    private static SpeicherFlottenErgebnis Arbeitslos(FlottenStudieKonfiguration flotte)
    {
        var diagnose = new FlottenDiagnose
        {
            IntervalleGesamt = 35040,
            Arbeitslos = true,
            IntervalleLastUeberPeakZiel = 35040,
            Gruende = new()
            {
                new FlottenDiagnoseBefund
                {
                    Grund = FlottenDiagnoseGrund.LastUeberPeakZiel,
                    Intervalle = 35040, Anteil = 1.0
                },
                new FlottenDiagnoseBefund
                {
                    Grund = FlottenDiagnoseGrund.LadedeckelDurchNetzladeverbot,
                    Intervalle = 35040, Anteil = 1.0
                }
            }
        };

        return new SpeicherFlottenErgebnis
        {
            Erfolg = true,
            Konfiguration = flotte,
            Studie = new FlottenStudienErgebnis
            {
                Variante = new FlottenSimulationErgebnis { Zulaessig = true, Diagnose = diagnose }
            },
            Pruefhinweise = new()
            {
                new FlottenHinweis
                {
                    Kennung = FlottenHinweisKennung.PeakZielUnterTagesminimum,
                    Stufe = FlottenHinweisStufe.Warnung,
                    Text = "Peak-Ziel 50 kW liegt unter dem Maximum der Tagesminima (68 kW)."
                },
                new FlottenHinweis
                {
                    Kennung = FlottenHinweisKennung.StartSoCAufMinimum,
                    Stufe = FlottenHinweisStufe.Hinweis,
                    Text = "Start-Ladezustand = SoC-Minimum (Produktivstandard)."
                }
            }
        };
    }

    private IRenderedComponent<StromspeicherAuslegungSeite> Ansicht(StromspeicherAuslegungDienste dienste)
        => Render<StromspeicherAuslegungSeite>(p => p
            .Add(x => x.Dienste, dienste)
            .Add(x => x.PlanerVerfuegbar, true));

    // =====================================================================
    //  Das Diagnosebanner (Konzept 2.2 Punkt 2)
    // =====================================================================

    [Fact]
    public void Eine_arbeitslose_Flotte_bekommt_ein_Warnbanner_mit_Gruenden_und_Abhilfe()
    {
        FlottenStudieKonfiguration flotte = Flotte();
        var cut = Ansicht(new StromspeicherAuslegungDienste
        {
            Vorgaben = () => Vorgaben(flotte),
            FlotteRechnen = (_, _) => Task.FromResult(Arbeitslos(flotte)),
            PeakZielBestimmen = (_, _) => Task.FromResult(new FlottenPeakZielErgebnis())
        });

        Auslegungshilfe.Rechenknopf(cut).Click();

        var banner = cut.FindComponent<FlottenDiagnosebanner>().Instance;
        Assert.True(banner.Arbeitslos);
        Assert.Contains(Resource.FLOTTE_BANNER_ARBEITSLOS, cut.Markup);

        // Die GRUENDE stehen im Klartext - sie kommen aus dem Kern, nicht aus der Seite.
        Assert.Contains("Last über dem Peak-Ziel", cut.Markup);
        Assert.Contains("Ladedeckel 0 durch das Netzladeverbot", cut.Markup);

        // Und die beiden Pruefhinweise des Laufs.
        Assert.Contains("Maximum der Tagesminima", cut.Markup);
        Assert.Contains("Start-Ladezustand", cut.Markup);

        // Drei Abhilfeknoepfe.
        Assert.Contains(Resource.FLOTTE_BANNER_BTN_PEAKZIEL, cut.Markup);
        Assert.Contains(Resource.FLOTTE_BANNER_BTN_NETZLADUNG, cut.Markup);
        Assert.Contains(Resource.FLOTTE_BANNER_BTN_BETRIEB, cut.Markup);
    }

    /// <summary>
    /// Das Banner steht ÜBER den Kacheln (Konzept 2.2): Wer die Kacheln liest, bevor er
    /// weiß, dass die Flotte nichts getan hat, liest zwölf Nullen als Ergebnis.
    /// </summary>
    [Fact]
    public void Das_Diagnosebanner_steht_vor_der_Ergebnisansicht()
    {
        FlottenStudieKonfiguration flotte = Flotte();
        var cut = Ansicht(new StromspeicherAuslegungDienste
        {
            Vorgaben = () => Vorgaben(flotte),
            FlotteRechnen = (_, _) => Task.FromResult(Arbeitslos(flotte))
        });

        Auslegungshilfe.Rechenknopf(cut).Click();

        int banner = cut.Markup.IndexOf("epos-flotte-diagnose", StringComparison.Ordinal);
        int ergebnis = cut.Markup.IndexOf("epos-flotte-ergebnis", StringComparison.Ordinal);
        Assert.True(banner >= 0 && ergebnis > banner,
                    "Das Diagnosebanner steht NICHT vor der Ergebnisansicht.");
    }

    /// <summary>„Netzladung erlauben" setzt die Option und entwertet das Ergebnis.</summary>
    [Fact]
    public void Der_Abhilfeknopf_Netzladung_setzt_die_Option_und_markiert_veraltet()
    {
        FlottenStudieKonfiguration flotte = Flotte();
        var cut = Ansicht(new StromspeicherAuslegungDienste
        {
            Vorgaben = () => Vorgaben(flotte),
            FlotteRechnen = (_, _) => Task.FromResult(Arbeitslos(flotte))
        });

        Auslegungshilfe.Rechenknopf(cut).Click();
        Assert.False(cut.Instance.Eingaben.Auslegung!.Flotte!.Optionen.NetzladungErlaubt);
        Assert.False(cut.Instance.Veraltet);

        cut.FindAll("button").Single(b => b.TextContent.Trim() == Resource.FLOTTE_BANNER_BTN_NETZLADUNG).Click();

        Assert.True(cut.Instance.Eingaben.Auslegung!.Flotte!.Optionen.NetzladungErlaubt);
        Assert.True(cut.Instance.Veraltet);
    }

    /// <summary>Steht die Netzladung schon an, fehlt der Knopf — er hätte nichts zu tun.</summary>
    [Fact]
    public void Mit_erlaubter_Netzladung_gibt_es_den_Knopf_nicht()
    {
        FlottenStudieKonfiguration flotte = Flotte(netzladung: true);
        var cut = Ansicht(new StromspeicherAuslegungDienste
        {
            Vorgaben = () => Vorgaben(flotte),
            FlotteRechnen = (_, _) => Task.FromResult(Arbeitslos(flotte))
        });

        Auslegungshilfe.Rechenknopf(cut).Click();

        Assert.DoesNotContain(Resource.FLOTTE_BANNER_BTN_NETZLADUNG, cut.Markup);
    }

    /// <summary>„Zu Schritt 3 Betriebsführung" wechselt das Blatt.</summary>
    [Fact]
    public void Der_Abhilfeknopf_fuehrt_auf_Schritt_drei()
    {
        FlottenStudieKonfiguration flotte = Flotte();
        var cut = Ansicht(new StromspeicherAuslegungDienste
        {
            Vorgaben = () => Vorgaben(flotte),
            FlotteRechnen = (_, _) => Task.FromResult(Arbeitslos(flotte))
        });

        Auslegungshilfe.Rechenknopf(cut).Click();
        cut.FindAll("button").Single(b => b.TextContent.Trim() == Resource.FLOTTE_BANNER_BTN_BETRIEB).Click();

        Assert.Equal(AuslegungSchritt.Betrieb, cut.Instance.Schritt);
        Assert.Single(cut.FindComponents<SpeicherFlottenBetriebEditor>());
    }

    // =====================================================================
    //  Die Vorpruefung (Konzept 2.4 Punkt 3)
    // =====================================================================

    /// <summary>
    /// Die Vorprüfung steht ÜBER der Ablaufleiste und SPERRT NICHT: Der Lauf startet
    /// trotzdem — sie warnt, sie verbietet nicht.
    /// </summary>
    [Fact]
    public void Die_Vorpruefung_warnt_ueber_der_Leiste_und_sperrt_den_Lauf_nicht()
    {
        int laeufe = 0;
        FlottenStudieKonfiguration flotte = Flotte();
        var cut = Ansicht(new StromspeicherAuslegungDienste
        {
            Vorgaben = () => Vorgaben(flotte),
            Vorpruefen = _ => new[]
            {
                new FlottenHinweis
                {
                    Kennung = FlottenHinweisKennung.PeakZielUnterTagesminimum,
                    Stufe = FlottenHinweisStufe.Warnung,
                    Text = "Unter dem Ziel fällt die Last nie."
                }
            },
            FlotteRechnen = (_, _) =>
            {
                laeufe++;
                return Task.FromResult(new SpeicherFlottenErgebnis { Erfolg = true, Konfiguration = flotte });
            }
        });

        Assert.Single(cut.Instance.Vorpruefung);
        Assert.Contains("Unter dem Ziel fällt die Last nie.", cut.Markup);

        int warnung = cut.Markup.IndexOf("Unter dem Ziel fällt die Last nie.", StringComparison.Ordinal);
        int leiste = cut.Markup.IndexOf("epos-ablaufleiste", StringComparison.Ordinal);
        Assert.True(warnung < leiste, "Die Vorprüfung steht NICHT über der Ablaufleiste.");

        Assert.False(Auslegungshilfe.Rechenknopf(cut).HasAttribute("disabled"));
        Auslegungshilfe.Rechenknopf(cut).Click();
        Assert.Equal(1, laeufe);
    }

    // =====================================================================
    //  Das Peak-Ziel (SD-Q3) und die Vorgabe „Netzladung" (SD-Q5)
    // =====================================================================

    [Fact]
    public void Der_Vorschlag_steht_als_Herleitung_unter_dem_Feld_und_laesst_sich_uebernehmen()
    {
        FlottenStudieKonfiguration flotte = Flotte();
        var cut = Ansicht(new StromspeicherAuslegungDienste
        {
            Vorgaben = () => Vorgaben(flotte),
            FlotteRechnen = (_, _) => Task.FromResult(new SpeicherFlottenErgebnis()),
            PeakZielVorschlag = _ => new FlottenPeakZielVorschlag
            {
                PeakZielKw = 777.4,
                AusReihe = true,
                Herleitung = "H₀ = max(Referenzspitze 789,4 kW − Σ Entladeleistung 12 kW; 68 kW) = 777,4 kW."
            }
        });

        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Betrieb);
        Assert.Contains("H₀ = max(Referenzspitze", cut.Markup);

        cut.FindAll("button").Single(b => b.TextContent.Trim() == Resource.FLOTTE_PEAK_BTN_VORSCHLAG).Click();

        Assert.Equal(777.4, cut.Instance.Eingaben.Auslegung!.Flotte!.Optionen.WirtschaftlicherPeakZielwertKw);
    }

    /// <summary>
    /// Der RÜCKFALL (ohne Zeitreihe) trägt keine Herleitung — er ist ein benannter
    /// Ersatzwert und keine Herleitung aus der Referenz.
    /// </summary>
    [Fact]
    public void Ohne_Zeitreihe_steht_keine_Herleitungszeile()
    {
        FlottenStudieKonfiguration flotte = Flotte();
        var cut = Ansicht(new StromspeicherAuslegungDienste
        {
            Vorgaben = () => Vorgaben(flotte),
            PeakZielVorschlag = _ => new FlottenPeakZielVorschlag
            { PeakZielKw = 50, AusReihe = false, Herleitung = "Ohne Lastgang: Rückfallwert 50 kW." }
        });

        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Betrieb);

        Assert.DoesNotContain("Ohne Lastgang: Rückfallwert 50 kW.", cut.Markup);
        Assert.DoesNotContain(Resource.FLOTTE_PEAK_BTN_VORSCHLAG, cut.Markup);
    }

    /// <summary>
    /// „Peak-Ziel bestimmen…" fährt die Bisektion und fragt das Ergebnis ab, bevor es
    /// übernommen wird.
    /// </summary>
    [Fact]
    public async Task Peak_Ziel_bestimmen_fragt_zurueck_und_uebernimmt_erst_dann()
    {
        FlottenStudieKonfiguration flotte = Flotte();
        var cut = Ansicht(new StromspeicherAuslegungDienste
        {
            Vorgaben = () => Vorgaben(flotte),
            FlotteRechnen = (_, _) => Task.FromResult(new SpeicherFlottenErgebnis()),
            PeakZielBestimmen = (_, _) => Task.FromResult(new FlottenPeakZielErgebnis
            {
                PeakZielKw = 16.7,
                VerbleibendeSpitzeKw = 16.7,
                Laeufe = 9,
                Konvergiert = true,
                Herleitung = "Bisektion zwischen Grundlast und Bezugsspitze."
            })
        });

        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Betrieb);
        await cut.FindAll("button")
                 .Single(b => b.TextContent.Trim() == Resource.FLOTTE_PEAK_BTN_BESTIMMEN)
                 .ClickAsync(new());

        // Die Rueckfrage steht - noch ist nichts uebernommen.
        Assert.Contains("16,7", cut.Markup);
        Assert.Equal(50, cut.Instance.Eingaben.Auslegung!.Flotte!.Optionen.WirtschaftlicherPeakZielwertKw);

        await cut.FindAll("button").Single(b => b.TextContent.Trim() == "Ja").ClickAsync(new());

        Assert.Equal(16.7, cut.Instance.Eingaben.Auslegung!.Flotte!.Optionen.WirtschaftlicherPeakZielwertKw);
        Assert.Contains("Bisektion zwischen Grundlast", cut.Markup);
    }

    /// <summary>
    /// Bei einem PLANENDEN Betriebsziel wird der Knopf gar nicht erst gefahren: Zwölf
    /// geplante Jahresläufe sind keine Bedienhandlung. Der Grund steht im Klartext da —
    /// die Ausnahme des Kerns bekommt der Anwender nicht zu sehen.
    /// </summary>
    [Fact]
    public async Task Bei_einem_planenden_Ziel_meldet_der_Knopf_seinen_Grund()
    {
        int gerufen = 0;
        FlottenStudieKonfiguration flotte = Flotte();
        flotte.Optionen.Betriebsziel = FlottenBetriebsziel.MultiUse;

        var cut = Ansicht(new StromspeicherAuslegungDienste
        {
            Vorgaben = () => Vorgaben(flotte),
            PeakZielBestimmen = (_, _) =>
            {
                gerufen++;
                return Task.FromResult(new FlottenPeakZielErgebnis());
            }
        });

        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Betrieb);
        await cut.FindAll("button")
                 .Single(b => b.TextContent.Trim() == Resource.FLOTTE_PEAK_BTN_BESTIMMEN)
                 .ClickAsync(new());

        Assert.Equal(0, gerufen);
        Assert.Contains(Resource.FLOTTE_PEAK_GESPERRT, cut.Markup);
    }

    /// <summary>
    /// SD‑Q5: Ein Zielwechsel zieht die Vorgabe „Netzladung erlaubt" mit — die
    /// Lastspitzenkappung braucht sie, der PV-Eigenverbrauch ausdrücklich nicht.
    /// </summary>
    [Fact]
    public async Task Ein_Zielwechsel_zieht_die_Vorgabe_Netzladung_mit()
    {
        FlottenStudieKonfiguration flotte = Flotte(netzladung: true);
        flotte.Optionen.Betriebsziel = FlottenBetriebsziel.PeakShaving;

        var cut = Ansicht(new StromspeicherAuslegungDienste { Vorgaben = () => Vorgaben(flotte) });
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Betrieb);

        // PvGreedy (Id 0) - dort ist Netzladung ausdruecklich NICHT vorgesehen.
        cut.FindAll("label").Single(x => x.TextContent.Contains(Resource.FLOTTE_BETRIEB_LBL_ZIEL))
           .QuerySelector("select")!.Change("0");

        Assert.Equal(FlottenBetriebsziel.PvGreedy,
                     cut.Instance.Eingaben.Auslegung!.Flotte!.Optionen.Betriebsziel);
        Assert.False(cut.Instance.Eingaben.Auslegung!.Flotte!.Optionen.NetzladungErlaubt);
        Assert.Contains(Resource.FLOTTE_PEAK_NETZLADUNG_NEIN, cut.Markup);

        await Task.CompletedTask;
    }

    // =====================================================================
    //  Die Rueckfrage beim Verlassen (Muster 62b-E-1)
    // =====================================================================

    /// <summary>Ohne Änderung gibt es keine Rückfrage — es ist nichts zu verlieren.</summary>
    [Fact]
    public async Task Ohne_Aenderung_wird_beim_Verlassen_nicht_gefragt()
    {
        var cut = Ansicht(new StromspeicherAuslegungDienste
        { Vorgaben = () => Vorgaben(Flotte()) });

        Assert.False(cut.Instance.Ungespeichert);
        Assert.Equal(AssistentVerlassen.Verwerfen, await cut.Instance.FrageVerlassen());
    }

    /// <summary>
    /// Mit ungespeicherten Eingaben kommt die Rückfrage mit DREI Wegen —
    /// Speichern / Verwerfen / Bleiben. „Bleiben" hält die Ansicht fest.
    /// </summary>
    [Fact]
    public async Task Mit_Aenderung_kommt_die_Rueckfrage_und_Bleiben_haelt_die_Ansicht()
    {
        int zu = 0;
        FlottenStudieKonfiguration flotte = Flotte();
        var cut = Render<StromspeicherAuslegungSeite>(p => p
            .Add(x => x.Dienste, new StromspeicherAuslegungDienste { Vorgaben = () => Vorgaben(flotte) })
            .Add(x => x.PlanerVerfuegbar, true)
            .Add(x => x.Geschlossen, Microsoft.AspNetCore.Components.EventCallback.Factory.Create(
                 this, () => zu++)));

        // Eine Aenderung auf Blatt 3 macht den Stand ungespeichert.
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Betrieb);
        cut.FindAll("label").Single(x => x.TextContent.Contains(Resource.FLOTTE_BETRIEB_LBL_ZIEL))
           .QuerySelector("select")!.Change("0");
        Assert.True(cut.Instance.Ungespeichert);

        Task<AssistentVerlassen> frage = cut.Instance.FrageVerlassen();
        cut.WaitForAssertion(() => Assert.Contains(Resource.FLOTTE_SEITE_VERLASSEN_FRAGE, cut.Markup));

        await cut.FindAll("button")
                 .Single(b => b.TextContent.Trim() == Resource.FLOTTE_SEITE_BTN_BLEIBEN)
                 .ClickAsync(new());

        Assert.Equal(AssistentVerlassen.Bleiben, await frage);
        Assert.Equal(0, zu);
    }

    /// <summary>„Speichern" geht denselben Weg wie der Knopf des Auslegungseditors.</summary>
    [Fact]
    public async Task Speichern_in_der_Rueckfrage_schreibt_und_gibt_den_Weg_frei()
    {
        int geschrieben = 0;
        FlottenStudieKonfiguration flotte = Flotte();
        var cut = Ansicht(new StromspeicherAuslegungDienste
        {
            Vorgaben = () => Vorgaben(flotte),
            EinstellungenSpeichern = _ => { geschrieben++; return Task.FromResult(""); }
        });

        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Betrieb);
        cut.FindAll("label").Single(x => x.TextContent.Contains(Resource.FLOTTE_BETRIEB_LBL_ZIEL))
           .QuerySelector("select")!.Change("0");

        Task<AssistentVerlassen> frage = cut.Instance.FrageVerlassen();
        cut.WaitForAssertion(() => Assert.Contains(Resource.FLOTTE_SEITE_VERLASSEN_FRAGE, cut.Markup));

        await cut.FindAll("button")
                 .Single(b => b.TextContent.Trim() == Resource.FLOTTE_SEITE_BTN_SPEICHERN)
                 .ClickAsync(new());

        Assert.Equal(AssistentVerlassen.Gespeichert, await frage);
        Assert.Equal(1, geschrieben);
        Assert.False(cut.Instance.Ungespeichert);
    }

    // =====================================================================
    //  Der fehlende Simulationslauf (Muster iU9-W11a)
    // =====================================================================

    /// <summary>
    /// Ohne gerechneten Lauf sagt die Ansicht es und BIETET ihn an, statt ihn heimlich
    /// zu starten.
    /// </summary>
    [Fact]
    public async Task Ohne_Simulationslauf_bietet_die_Ansicht_ihn_an()
    {
        int laeufe = 0;
        var cut = Ansicht(new StromspeicherAuslegungDienste
        {
            Vorgaben = () => Vorgaben(Flotte()),
            LaufVorhanden = () => laeufe > 0,
            Simulationslauf = _ =>
            {
                laeufe++;
                return Task.FromResult(EPOS.UI.Seiten.Simulation.Rueckmeldung.Still);
            }
        });

        Assert.Contains(Resource.FLOTTE_SEITE_KEIN_LAUF, cut.Markup);

        await cut.FindAll("button")
                 .Single(b => b.TextContent.Trim() == Resource.FLOTTE_SEITE_BTN_LAUF)
                 .ClickAsync(new());

        Assert.Equal(1, laeufe);
        cut.WaitForAssertion(() => Assert.DoesNotContain(Resource.FLOTTE_SEITE_KEIN_LAUF, cut.Markup));
    }
}
