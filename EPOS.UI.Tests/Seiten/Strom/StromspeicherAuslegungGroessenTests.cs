using System;
using System.Collections.Generic;
using System.Linq;
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
/// DIE GRÖSSEN-SICHT IN SCHRITT 5 (Auftrag #196 — die Einbindung von Paket P4 in die
/// Ansicht aus Paket P3; Konzept „Stromspeicher-Dialoge" 2.5).
///
/// <para><b>Was hier geprüft wird.</b> Nicht der Baustein selbst — den prüft
/// <c>EPOS.UI.Tests/Dialoge/SpeicherFlottenGroessenAnsichtTests</c> —, sondern seine
/// NAHT zur Seite: wann er erscheint, dass die einfache Kandidatentabelle daneben
/// nicht mehr steht, und was „Kandidat übernehmen" mit Schritt 1 und Schritt 5
/// macht.</para>
///
/// <para><b>Die Rückfrage ist der Teil, der schiefgehen kann.</b> Der Kandidat ersetzt
/// die Speicher in Schritt 1; trägt der Anwender dort ungespeicherte Eingaben, sind sie
/// danach weg. Deshalb steht davor dieselbe Dreifachfrage wie beim Verlassen
/// (Muster 62b‑E‑1) — und „Bleiben" muss wirklich nichts tun.</para>
/// </summary>
public sealed class StromspeicherAuslegungGroessenTests : EposBunitContext
{
    /// <summary>Die Ansicht trägt einen <c>InfoKnopf</c> und braucht deshalb den Hilfedienst.</summary>
    public StromspeicherAuslegungGroessenTests()
        => Services.AddSingleton<IHilfeDienst>(new KeineHilfe());

    // =====================================================================
    //  Wann der Baustein steht
    // =====================================================================

    /// <summary>
    /// Nach einem Suchlauf steht die Größen-Sicht in Schritt 5 — und die einfache
    /// Kandidatentabelle der Ergebnisansicht steht NICHT mehr daneben (#196: keine zwei
    /// Tabellen derselben Kandidaten).
    /// </summary>
    [Fact]
    public void Mit_Rastersuchergebnis_steht_die_Groessensicht_vor_der_Ergebnisansicht()
    {
        var cut = Gerechnet(MitAuslegung());

        Assert.Equal(AuslegungSchritt.Ergebnis, cut.Instance.Schritt);
        Assert.Single(cut.FindComponents<SpeicherFlottenGroessenAnsicht>());
        Assert.Single(cut.FindComponents<SpeicherFlottenErgebnisAnsicht>());

        // Die Aussage des Laufs steht GENAU EINMAL da.
        Assert.Equal(1, Vorkommen(cut.Markup, "Beste technisch zulässige Flotte"));
        Assert.DoesNotContain("Varianten geprüft", cut.Markup, StringComparison.Ordinal);

        // Und die Größen-Sicht steht VOR der Ergebnisansicht (Konzept 2.5).
        Assert.True(cut.Markup.IndexOf("epos-flotte-groessen", StringComparison.Ordinal)
                    < cut.Markup.IndexOf("epos-flotte-ergebnis", StringComparison.Ordinal));
    }

    /// <summary>
    /// Ohne Rastersuche gibt es keine Kandidaten — dann steht auch keine Karte da.
    /// </summary>
    [Fact]
    public void Ohne_Rastersuchergebnis_steht_keine_Groessensicht()
    {
        var cut = Gerechnet(OhneAuslegung(), groessenOptimieren: false);

        Assert.Equal(AuslegungSchritt.Ergebnis, cut.Instance.Schritt);
        Assert.Single(cut.FindComponents<SpeicherFlottenErgebnisAnsicht>());
        Assert.Empty(cut.FindComponents<SpeicherFlottenGroessenAnsicht>());
    }

    // =====================================================================
    //  „Kandidat uebernehmen"
    // =====================================================================

    /// <summary>
    /// Der gewählte Kandidat WIRD die Flotte in Schritt 1: Kapazität und Leistungen je
    /// Einheit aus dem Kandidaten, alles Übrige aus dem Arbeitsstand. Der Suchlauf ist
    /// danach abgeschaltet, Schritt 5 veraltet, die Ansicht steht auf Schritt 1 und sagt
    /// im Banner, was geschehen ist.
    /// </summary>
    [Fact]
    public void Kandidat_uebernehmen_setzt_die_Flotte_in_Schritt_eins()
    {
        var cut = Gerechnet(MitAuslegung());

        Uebernehmen(cut, "K-30");

        FlottenStudieKonfiguration flotte = cut.Instance.Eingaben.Auslegung!.Flotte!;
        FlottenEinheit einheit = Assert.Single(flotte.Einheiten);
        Assert.Equal(30.0, einheit.KapazitaetKWh, 9);
        Assert.Equal(15.0, einheit.EntladeleistungKw, 9);
        Assert.Equal(0.95, einheit.Ladewirkungsgrad, 9);      // aus dem Arbeitsstand
        Assert.Equal(30.0, Assert.Single(flotte.Wirtschaftlichkeit.Einheiten).KapazitaetKWh, 9);

        Assert.False(cut.Instance.Eingaben.Auslegung!.FlottenGroessenOptimieren);
        Assert.True(cut.Instance.Veraltet);
        Assert.Equal(AuslegungSchritt.Speicher, cut.Instance.Schritt);
        Assert.Contains("K-30", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Flotte neu bewerten", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// Der BESTE Kandidat nimmt den Weg über die Konfiguration des Optimierers — genau
    /// den, den „Beste Flotte übernehmen" seit jeher geht. Keine zweite Wahrheit.
    /// </summary>
    [Fact]
    public void Der_beste_Kandidat_nimmt_die_Konfiguration_des_Optimierers()
    {
        SpeicherFlottenErgebnis ergebnis = MitAuslegung();
        ergebnis.Auslegung!.BesteKonfiguration = new FlottenStudieKonfiguration
        {
            Einheiten = { new FlottenEinheit { Id = "vom-Optimierer", KapazitaetKWh = 42 } }
        };

        var cut = Gerechnet(ergebnis);
        Uebernehmen(cut, "K-20");

        FlottenEinheit einheit =
            Assert.Single(cut.Instance.Eingaben.Auslegung!.Flotte!.Einheiten);
        Assert.Equal("vom-Optimierer", einheit.Id);
        Assert.Equal(42.0, einheit.KapazitaetKWh, 9);
    }

    /// <summary>
    /// Das BETRIEBSZIEL des Kandidaten landet in Schritt 3 — die Rastersuche fährt je
    /// Ziel eigene Kandidaten.
    /// </summary>
    [Fact]
    public void Das_Betriebsziel_des_Kandidaten_landet_in_Schritt_drei()
    {
        var cut = Gerechnet(MitAuslegung());

        Uebernehmen(cut, "K-10-PV");

        Assert.Equal(FlottenBetriebsziel.PvGreedy,
                     cut.Instance.Eingaben.Auslegung!.Flotte!.Optionen.Betriebsziel);
    }

    // =====================================================================
    //  Die Rueckfrage bei ungespeicherten Eingaben (Muster 62b-E-1)
    // =====================================================================

    /// <summary>
    /// Trägt Schritt 1 ungespeicherte Eingaben, kommt erst die Dreifachfrage — und
    /// „Bleiben" lässt die Flotte, wie sie ist.
    /// </summary>
    [Fact]
    public async Task Bei_ungespeicherten_Eingaben_kommt_die_Rueckfrage_und_Bleiben_haelt_den_Stand()
    {
        var cut = Gerechnet(MitAuslegung());
        await AendernUndZurueckZumErgebnis(cut);

        Uebernehmen(cut, "K-30");

        Assert.Contains(Resource.FLOTTE_SEITE_KANDIDAT_FRAGE, cut.Markup, StringComparison.Ordinal);
        Assert.Equal(24.0, cut.Instance.Eingaben.Auslegung!.Flotte!.Einheiten[0].KapazitaetKWh, 9);

        await cut.FindAll("button")
                 .Single(b => b.TextContent.Trim() == Resource.FLOTTE_SEITE_BTN_BLEIBEN)
                 .ClickAsync(new());

        Assert.DoesNotContain(Resource.FLOTTE_SEITE_KANDIDAT_FRAGE, cut.Markup, StringComparison.Ordinal);
        Assert.Equal(24.0, cut.Instance.Eingaben.Auslegung!.Flotte!.Einheiten[0].KapazitaetKWh, 9);
        Assert.Equal(AuslegungSchritt.Ergebnis, cut.Instance.Schritt);
    }

    /// <summary>„Verwerfen" übernimmt den Kandidaten, ohne vorher zu schreiben.</summary>
    [Fact]
    public async Task Verwerfen_in_der_Rueckfrage_uebernimmt_ohne_zu_speichern()
    {
        int geschrieben = 0;
        var cut = Gerechnet(MitAuslegung(), _ => { geschrieben++; return Task.FromResult(""); });
        await AendernUndZurueckZumErgebnis(cut);

        Uebernehmen(cut, "K-30");
        await cut.FindAll("button")
                 .Single(b => b.TextContent.Trim() == Resource.FLOTTE_SEITE_BTN_VERWERFEN)
                 .ClickAsync(new());

        Assert.Equal(0, geschrieben);
        Assert.Equal(30.0, cut.Instance.Eingaben.Auslegung!.Flotte!.Einheiten[0].KapazitaetKWh, 9);
        Assert.Equal(AuslegungSchritt.Speicher, cut.Instance.Schritt);
    }

    /// <summary>
    /// „Speichern" schreibt den bisherigen Stand, BEVOR der Kandidat ihn ersetzt —
    /// derselbe Weg wie der Knopf des Auslegungseditors.
    /// </summary>
    [Fact]
    public async Task Speichern_in_der_Rueckfrage_schreibt_den_bisherigen_Stand()
    {
        double gespeichert = 0;
        var cut = Gerechnet(MitAuslegung(), e =>
        {
            gespeichert = e.Auslegung!.Flotte!.Einheiten[0].KapazitaetKWh;
            return Task.FromResult("");
        });
        await AendernUndZurueckZumErgebnis(cut);

        Uebernehmen(cut, "K-30");
        await cut.FindAll("button")
                 .Single(b => b.TextContent.Trim() == Resource.FLOTTE_SEITE_BTN_SPEICHERN)
                 .ClickAsync(new());

        Assert.Equal(24.0, gespeichert, 9);
        Assert.Equal(30.0, cut.Instance.Eingaben.Auslegung!.Flotte!.Einheiten[0].KapazitaetKWh, 9);
    }

    // =====================================================================
    //  Hilfen
    // =====================================================================

    /// <summary>Die Ansicht nach einem gelungenen Flottenlauf — sie steht auf Schritt 5.</summary>
    private IRenderedComponent<StromspeicherAuslegungSeite> Gerechnet(
        SpeicherFlottenErgebnis ergebnis,
        Func<SpeicherOptimierungEingaben, Task<string>>? speichern = null,
        bool groessenOptimieren = true)
    {
        var vorgaben = new SpeicherOptimierungVorgaben
        {
            Eingaben = new SpeicherOptimierungEingaben
            {
                Auslegung = new SpeicherAuslegungKonfiguration
                {
                    Flotte = Flotte(),
                    FlottenGroessenOptimieren = groessenOptimieren
                }
            }
        };

        var cut = Render<StromspeicherAuslegungSeite>(p => p
            .Add(x => x.Dienste, new StromspeicherAuslegungDienste
            {
                Vorgaben = () => vorgaben,
                FlotteRechnen = (_, _) => Task.FromResult(ergebnis),
                EinstellungenSpeichern = speichern
            })
            .Add(x => x.PlanerVerfuegbar, true));

        Auslegungshilfe.Rechenknopf(cut).Click();
        return cut;
    }

    /// <summary>Klickt „übernehmen" in der Kandidatenzeile mit dieser Kennung.</summary>
    private static void Uebernehmen(IRenderedComponent<StromspeicherAuslegungSeite> cut, string kandidat)
    {
        IElement zeile = cut.FindAll(".epos-flotte-groessen-tabelle tbody tr")
            .Single(x => x.TextContent.Contains(kandidat, StringComparison.Ordinal));
        zeile.QuerySelector(".epos-zellenaktionen button")!.Click();
    }

    /// <summary>
    /// Ändert die Flotte in Schritt 1 (damit ist der Stand ungespeichert) und geht
    /// zurück auf das Ergebnisblatt.
    /// </summary>
    private static async Task AendernUndZurueckZumErgebnis(
        IRenderedComponent<StromspeicherAuslegungSeite> cut)
    {
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Speicher);
        await cut.InvokeAsync(() => cut.FindComponent<SpeicherFlottenEditor>()
            .Instance.WertChanged.InvokeAsync(Flotte()));
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Ergebnis);
        Assert.True(cut.Instance.Ungespeichert);
    }

    private static int Vorkommen(string markup, string text)
    {
        int zahl = 0, stelle = markup.IndexOf(text, StringComparison.Ordinal);
        while (stelle >= 0)
        {
            zahl++;
            stelle = markup.IndexOf(text, stelle + text.Length, StringComparison.Ordinal);
        }
        return zahl;
    }

    private static FlottenStudieKonfiguration Flotte() => new()
    {
        Einheiten =
        {
            new FlottenEinheit
            {
                Id = "a", Name = "A", KapazitaetKWh = 24, LadeleistungKw = 10, EntladeleistungKw = 12,
                Ladewirkungsgrad = 0.95, Entladewirkungsgrad = 0.95,
                SocMin = 0.1, SocMax = 0.9, SocStart = 0.5
            }
        },
        Optionen = new FlottenSimulationOptionen
        {
            Betriebsziel = FlottenBetriebsziel.PeakShaving,
            WirtschaftlicherPeakZielwertKw = 8,
            EnergieAusgleichEuroProKWh = 0.2
        },
        Wirtschaftlichkeit = new FlottenWirtschaftlichkeitEingang
        { ProjektjahreBeiWiederholung = 1 }
    };

    /// <summary>Ein gerechneter Flottenlauf OHNE Rastersuche.</summary>
    private static SpeicherFlottenErgebnis OhneAuslegung()
    {
        FlottenStudieKonfiguration config = Flotte();
        var eingang = new FlottenEingang
        {
            DatenId = "Test",
            Istwerte =
            {
                new FlottenNetzintervall
                { Zeitstempel = DateTimeOffset.Parse("2026-01-01T00:00:00Z"), LastKw = 10 },
                new FlottenNetzintervall
                { Zeitstempel = DateTimeOffset.Parse("2026-01-01T00:15:00Z"), LastKw = 10 }
            }
        };
        return new SpeicherFlottenErgebnis
        {
            Erfolg = true,
            Konfiguration = config,
            Studie = FlottenSimulator.Simuliere(eingang, config)
        };
    }

    /// <summary>Derselbe Lauf MIT einem synthetischen 2 × 2-Raster.</summary>
    private static SpeicherFlottenErgebnis MitAuslegung()
    {
        SpeicherFlottenErgebnis e = OhneAuslegung();
        FlottenKandidatZusammenfassung bester = Kandidat("K-20", 20, 20, 2000, true);
        e.Auslegung = new FlottenAuslegungErgebnis
        {
            Aussage = "Beste Variante im geprueften endlichen Raster",
            Kandidaten = new List<FlottenKandidatZusammenfassung>
            {
                Kandidat("K-10", 10, 5, 1000, true),
                Kandidat("K-10-PV", 10, 10, 900, true, FlottenBetriebsziel.PvGreedy),
                bester,
                Kandidat("K-30", 30, 15, 1500, true)
            },
            BesterKandidat = bester
        };
        return e;
    }

    private static FlottenKandidatZusammenfassung Kandidat(
        string id, double kapazitaet, double entladen, double kapitalwert, bool zulaessig,
        FlottenBetriebsziel ziel = FlottenBetriebsziel.PeakShaving) => new()
    {
        KandidatId = id,
        Betriebsziel = ziel,
        Zulaessig = zulaessig,
        KapitalwertEuro = kapitalwert,
        KapazitaetKWh = kapazitaet,
        LadeleistungKw = entladen,
        EntladeleistungKw = entladen,
        DurchsatzKWh = 100,
        Vollzyklen = 5,
        BezugsspitzeKw = 16.74,
        ErsparnisEuroJahr = 180,
        Einheiten = new List<FlottenKandidatEinheit>
        {
            new()
            {
                Id = "a",
                KapazitaetKWh = kapazitaet,
                LadeleistungKw = entladen,
                EntladeleistungKw = entladen
            }
        }
    };
}
