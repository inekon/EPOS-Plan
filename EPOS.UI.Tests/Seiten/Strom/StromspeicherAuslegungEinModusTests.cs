using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using EPOS.UI.Seiten.Strom;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Seiten.Strom;

/// <summary>
/// EIN WEG STATT ZWEI — der Anwenderentscheid <b>SD‑E‑8</b> vom 11.09.2026 (Auftrag #206):
/// „Es ist nicht sinnvoll, einen Unterschied zwischen Einzelspeicher und Flotte zu machen.
/// Für Einzelspeicher sollen auch die Betriebsziele wählbar sein — die bisherigen
/// Berechnungsarten für Einzelspeicher sind nicht mehr nötig."
///
/// <para>Geprüft wird, was daraus folgt: kein Umschalter, die Einheitenliste für jede
/// Einheitenzahl, fünf Betriebsziele auch bei einer Einheit, die Verteilung erst ab zwei,
/// die Größen-Sicht mit einer Einheit — und das, was der Einzelweg HIERLIESS: das
/// Rückschreiben in die Projektanlage und der Leistungspreis als EINE Eingabe.</para>
///
/// <para>Die vier Fälle am Ende sind aus dem gelöschten <c>StromspeicherAuslegungEinzelTests</c>
/// übernommen: Fortschritt, Abbruch und Rückweg hingen dort am Einzellauf und gelten
/// unverändert für den einen verbliebenen Weg.</para>
/// </summary>
public sealed class StromspeicherAuslegungEinModusTests : EposBunitContext
{
    public StromspeicherAuslegungEinModusTests()
        => Services.AddSingleton<IHilfeDienst>(new KeineHilfe());

    // =================================================================================
    //  Der Umschalter ist weg
    // =================================================================================

    /// <summary>
    /// Es gibt keinen Modus-Umschalter mehr — weder in der Ablaufleiste noch sonst wo.
    /// </summary>
    [Fact]
    public void Die_Ablaufleiste_traegt_keinen_Modusumschalter_mehr()
    {
        var cut = Ansicht(Dienste());

        Assert.Empty(cut.FindAll(".epos-ablaufleiste-modus"));
        Assert.DoesNotContain(Resource.FLOTTE_SEITE_SCHRITT1, "Speicherparameter");
        Assert.Equal(Resource.FLOTTE_SEITE_SCHRITT1,
                     Auslegungshilfe.Schrittknopf(cut, AuslegungSchritt.Speicher)
                                    .TextContent.Trim());
    }

    /// <summary>
    /// Die WACHE nach Hausmuster: <c>AuslegungModus</c> kommt in <c>EPOS.UI</c> nicht mehr
    /// vor. Ein Aufzählungstyp, der gelöscht ist, lässt sich nicht mehr benennen — geprüft
    /// wird trotzdem der QUELLTEXT, damit ein späterer Wiedereinbau auffällt und nicht
    /// still als „Sonderfall" zurückkommt.
    /// </summary>
    [Fact]
    public void In_EPOS_UI_steht_kein_AuslegungModus_mehr()
    {
        List<string> treffer = Quelldateien()
            .Where(d => File.ReadAllText(d).Contains("AuslegungModus", StringComparison.Ordinal))
            .Select(d => Path.GetFileName(d))
            .ToList();

        Assert.True(treffer.Count == 0,
                    "AuslegungModus steht noch in: " + string.Join(", ", treffer));
    }

    /// <summary>Die Wache darf nicht ins Leere greifen.</summary>
    [Fact]
    public void Die_Wache_liest_tatsaechlich_Quelldateien()
    {
        string[] dateien = Quelldateien();

        Assert.True(dateien.Length > 100);
        Assert.Contains(dateien, d => Path.GetFileName(d) == "StromspeicherAuslegungSeite.razor");
    }

    // =================================================================================
    //  Eine Einheit ist der Einzelspeicher
    // =================================================================================

    /// <summary>
    /// Ohne gespeicherten Flottenstand belegt der KERN eine Einheit aus der aktiven
    /// Speichervariante vor (<c>SpeicherFlottenStudieCtrl.Vorbelegung</c>); die Ansicht
    /// zeigt sie im Flotteneditor und rechnet mit ihr.
    /// </summary>
    [Fact]
    public void Die_Ansicht_startet_mit_der_einen_vorbelegten_Einheit()
    {
        var cut = Ansicht(Dienste(Eine()));

        SpeicherFlottenEditor editor = cut.FindComponent<SpeicherFlottenEditor>().Instance;
        Assert.Single(editor.Wert.Einheiten);
        Assert.Equal("Speicher 1", editor.Wert.Einheiten[0].Name);
        Assert.Equal("42", editor.Wert.Einheiten[0].AnlageId);
        Assert.Contains("Speicher 1", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// Die fünf Betriebsziele stehen auch bei EINER Einheit zur Wahl — das ist der Kern
    /// des Entscheids („Für Einzelspeicher sollen auch die Betriebsziele wählbar sein").
    /// </summary>
    [Fact]
    public void Bei_einer_Einheit_stehen_fuenf_Betriebsziele_zur_Wahl()
    {
        var cut = Ansicht(Dienste(Eine()));
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Betrieb);

        IElement ziel = Klappliste(cut, Resource.FLOTTE_BETRIEB_LBL_ZIEL);
        List<string> eintraege = ziel.QuerySelectorAll("option")
                                     .Select(o => o.TextContent.Trim()).ToList();

        Assert.Equal(5, eintraege.Count);
        Assert.Contains(Resource.FLOTTE_ZIEL_PVGREEDY, eintraege);
        Assert.Contains(Resource.FLOTTE_ZIEL_PEAKSHAVING, eintraege);
        Assert.Contains(Resource.FLOTTE_ZIEL_PVPLANUNG, eintraege);
        Assert.Contains(Resource.FLOTTE_ZIEL_ARBITRAGE, eintraege);
        Assert.Contains(Resource.FLOTTE_ZIEL_MULTIUSE, eintraege);
    }

    /// <summary>
    /// Die VERTEILUNG erscheint erst ab zwei Einheiten — ausgeblendet, nicht gesperrt,
    /// und mit einer Zeile Erklärung.
    /// </summary>
    [Fact]
    public void Die_Verteilung_erscheint_erst_ab_zwei_Einheiten()
    {
        var cut = Ansicht(Dienste(Eine()));
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Betrieb);

        Assert.DoesNotContain(Resource.FLOTTE_BETRIEB_LBL_VERTEILUNG, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_BETRIEB_VERTEILUNG_EINE, cut.Markup, StringComparison.Ordinal);

        var zwei = Ansicht(Dienste(Zwei()));
        Auslegungshilfe.Schritt(zwei, AuslegungSchritt.Betrieb);

        Assert.Contains(Resource.FLOTTE_BETRIEB_LBL_VERTEILUNG, zwei.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain(Resource.FLOTTE_BETRIEB_VERTEILUNG_EINE, zwei.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// Die GRÖSSEN-SICHT (Paket P4) gilt für jede Einheitenzahl: Ein Rastersuchergebnis
    /// mit einer Einheit zeigt sie genauso wie eines mit zweien.
    /// </summary>
    [Fact]
    public void Die_Groessen_Sicht_steht_auch_bei_einer_Einheit()
    {
        FlottenStudieKonfiguration flotte = Eine();
        SpeicherFlottenErgebnis ergebnis = Gerechnet(flotte);
        ergebnis.Auslegung = new FlottenAuslegungErgebnis
        {
            Kandidaten = new()
            {
                new FlottenKandidatZusammenfassung
                {
                    KandidatId = "K1", Zulaessig = true,
                    KapazitaetKWh = 20, LadeleistungKw = 5, EntladeleistungKw = 5,
                    KapitalwertEuro = 1234,
                    Einheiten = new()
                    {
                        new FlottenKandidatEinheit
                        { KapazitaetKWh = 20, LadeleistungKw = 5, EntladeleistungKw = 5 }
                    }
                }
            }
        };

        var cut = Ansicht(Dienste(flotte, ergebnis));
        Auslegungshilfe.Rechenknopf(cut).Click();

        Assert.Single(cut.FindComponents<SpeicherFlottenGroessenAnsicht>());
        Assert.Equal(1, cut.FindComponent<SpeicherFlottenGroessenAnsicht>().Instance.Einheiten);
    }

    // =================================================================================
    //  Was der Einzelweg hierliess
    // =================================================================================

    /// <summary>
    /// Das Rückschreiben in die Projektanlage bleibt (Auftrag #206, Punkt 2): Der Knopf
    /// steht in Schritt 5, fragt zurück und schreibt Kapazität und Leistung der EINEN
    /// Einheit mit Anlagenbezug.
    /// </summary>
    [Fact]
    public async Task Die_Groesse_der_einen_Einheit_geht_mit_Rueckfrage_in_die_Projektanlage()
    {
        var geschrieben = new List<(double Kwh, double Kw)>();
        FlottenStudieKonfiguration flotte = Eine();

        var cut = Ansicht(Dienste(flotte, Gerechnet(flotte), d =>
            d.AuslegungUebernehmen = (kwh, kw) =>
            {
                geschrieben.Add((kwh, kw));
                return new Rueckmeldung(true, "geschrieben");
            }));

        Auslegungshilfe.Rechenknopf(cut).Click();

        await Auslegungshilfe.Knopf(cut, Resource.FLOTTE_SEITE_BTN_ANLAGE).ClickAsync(new());

        // Erst die Rueckfrage, dann das Schreiben — vorher steht nichts in der Anlage.
        Assert.Empty(geschrieben);
        Assert.Contains("20", cut.Markup, StringComparison.Ordinal);

        await Auslegungshilfe.Knopf(cut, "Ja").ClickAsync(new());

        Assert.Single(geschrieben);
        Assert.Equal(20, geschrieben[0].Kwh);
        Assert.Equal(5, geschrieben[0].Kw);
        Assert.Contains("geschrieben", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// Ein NEIN auf die Rückfrage schreibt nichts — dieselbe Aussage wie im gefallenen
    /// Einzelspeicher-Ergebnis.
    /// </summary>
    [Fact]
    public async Task Ein_Nein_schreibt_nichts_in_die_Projektanlage()
    {
        int gerufen = 0;
        FlottenStudieKonfiguration flotte = Eine();

        var cut = Ansicht(Dienste(flotte, Gerechnet(flotte), d =>
            d.AuslegungUebernehmen = (_, _) => { gerufen++; return new Rueckmeldung(true, ""); }));

        Auslegungshilfe.Rechenknopf(cut).Click();
        await Auslegungshilfe.Knopf(cut, Resource.FLOTTE_SEITE_BTN_ANLAGE).ClickAsync(new());
        await Auslegungshilfe.Knopf(cut, "Nein").ClickAsync(new());

        Assert.Equal(0, gerufen);
    }

    /// <summary>
    /// Tragen ZWEI Einheiten einen Anlagenbezug, gibt es den Knopf nicht: Die
    /// Projektanlage trägt EINE Kapazität und EINE Leistung, und welche der beiden
    /// gemeint wäre, ist nicht zu sagen.
    /// </summary>
    [Fact]
    public void Bei_zwei_Anlagenbezuegen_gibt_es_den_Anlagenknopf_nicht()
    {
        FlottenStudieKonfiguration flotte = Zwei();
        flotte.Einheiten[1].AnlageId = "43";

        var cut = Ansicht(Dienste(flotte, Gerechnet(flotte), d =>
            d.AuslegungUebernehmen = (_, _) => new Rueckmeldung(true, "")));

        Auslegungshilfe.Rechenknopf(cut).Click();

        Assert.DoesNotContain(Resource.FLOTTE_SEITE_BTN_ANLAGE, cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// Steht neben der Anlage des Projekts eine ZWEITE, frei angelegte Einheit, ist die
    /// Frage eindeutig: Geschrieben wird die Einheit MIT Anlagenbezug — nicht die Summe
    /// der Flotte und nicht die erste der Liste.
    /// </summary>
    [Fact]
    public async Task Neben_einer_freien_Einheit_wird_die_Anlageneinheit_geschrieben()
    {
        var geschrieben = new List<(double Kwh, double Kw)>();
        FlottenStudieKonfiguration flotte = Zwei();
        flotte.Einheiten[0].AnlageId = "";      // die freie steht vorn
        flotte.Einheiten[1].AnlageId = "42";

        var cut = Ansicht(Dienste(flotte, Gerechnet(flotte), d =>
            d.AuslegungUebernehmen = (kwh, kw) =>
            {
                geschrieben.Add((kwh, kw));
                return new Rueckmeldung(true, "");
            }));

        Auslegungshilfe.Rechenknopf(cut).Click();
        await Auslegungshilfe.Knopf(cut, Resource.FLOTTE_SEITE_BTN_ANLAGE).ClickAsync(new());
        await Auslegungshilfe.Knopf(cut, "Ja").ClickAsync(new());

        Assert.Single(geschrieben);
        Assert.Equal(16, geschrieben[0].Kwh);
        Assert.Equal(4, geschrieben[0].Kw);
    }

    /// <summary>
    /// Ohne Schreibweg kein Knopf (Hausregel „Kein Delegat ist kein Knopf").
    /// </summary>
    [Fact]
    public void Ohne_Rueckruf_kein_Anlagenknopf()
    {
        FlottenStudieKonfiguration flotte = Eine();
        var cut = Ansicht(Dienste(flotte, Gerechnet(flotte)));

        Auslegungshilfe.Rechenknopf(cut).Click();

        Assert.DoesNotContain(Resource.FLOTTE_SEITE_BTN_ANLAGE, cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// Der Leistungspreis steht in Schritt 2 und ist EINE Eingabe: Er schreibt in den
    /// Suchraum, in den Flottentarif UND sofort in die Projektvariante.
    /// </summary>
    [Fact]
    public void Der_Leistungspreis_ist_eine_Eingabe_fuer_Flotte_und_Variante()
    {
        var geschrieben = new List<double>();
        FlottenStudieKonfiguration flotte = Eine();

        var cut = Ansicht(Dienste(flotte, null, d =>
            d.LeistungspreisSchreiben = w => geschrieben.Add(w)));

        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Daten);
        Assert.Single(cut.FindComponents<LeistungspreisBlock>());

        Feld(cut, Resource.OPT_LBL_LEISTUNGSPREIS).Input("133,5");

        Assert.Equal(new[] { 133.5 }, geschrieben);
        Assert.Equal(133.5, cut.Instance.Eingaben.LeistungspreisEurProKwA);
        Assert.Equal(133.5, cut.Instance.Eingaben.Auslegung.Flotte!.Tarif.LeistungspreisEuroProKw);
    }

    /// <summary>
    /// Eine gepflegte QUELLE setzt das Feld und schreibt — die Übernahme ist ein
    /// Schreibvorgang, kein Vorschlag (W11b‑E‑3, unverändert übernommen).
    /// </summary>
    [Fact]
    public void Eine_Leistungspreisquelle_setzt_das_Feld_und_schreibt()
    {
        var geschrieben = new List<double>();
        FlottenStudieKonfiguration flotte = Eine();

        var cut = Ansicht(Dienste(flotte, null, d =>
            d.LeistungspreisSchreiben = w => geschrieben.Add(w),
            quellen: new[]
            {
                new SpeicherOptimierungLeistungspreisQuelle
                { Bezeichnung = "Tarif Netzbezug", WertEurProKwA = 88.5 }
            }));

        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Daten);
        Klappliste(cut, Resource.OPT_LBL_LP_QUELLE).Change("0");

        Assert.Equal(new[] { 88.5 }, geschrieben);
        Assert.Equal(88.5, cut.Instance.Eingaben.Auslegung.Flotte!.Tarif.LeistungspreisEuroProKw);
    }

    // =================================================================================
    //  Uebernommen aus dem geloeschten Einzelpruefstand
    // =================================================================================

    /// <summary>
    /// Der Fortschritt kommt aus dem Hintergrund an, solange der Lauf läuft — und
    /// verschwindet danach.
    /// </summary>
    [Fact]
    public async Task Der_Fortschritt_erscheint_waehrend_des_Laufs()
    {
        var quelle = new TaskCompletionSource<SpeicherFlottenErgebnis>();
        Action<double?, string>? melder = null;
        FlottenStudieKonfiguration flotte = Eine();

        var cut = Ansicht(Dienste(flotte, null, d =>
            d.FlotteRechnen = (_, m) => { melder = m; return quelle.Task; }));

        // Der Klick wird NICHT abgewartet: Sein Task endet erst, wenn der Behandler
        // fertig ist - und der wartet auf den Lauf. Das ist der Sinn der Uebung.
        Task klick = Auslegungshilfe.Rechenknopf(cut).ClickAsync(new());

        Assert.True(cut.Instance.Laeuft);
        Assert.NotNull(cut.Find("progress"));

        melder!(0.5, "Variante 3 von 6");
        cut.WaitForAssertion(() =>
            Assert.Contains("Variante 3 von 6", cut.Markup, StringComparison.Ordinal));

        quelle.SetResult(Gerechnet(flotte));
        await klick;

        Assert.False(cut.Instance.Laeuft);
        Assert.Empty(cut.FindAll("progress"));
    }

    /// <summary>Ohne Abbruchrückruf gibt es keinen Abbrechen-Knopf (Hausregel).</summary>
    [Fact]
    public async Task Ohne_Rueckruf_kein_Abbrechen_Knopf()
    {
        var quelle = new TaskCompletionSource<SpeicherFlottenErgebnis>();
        FlottenStudieKonfiguration flotte = Eine();

        var cut = Ansicht(Dienste(flotte, null, d => d.FlotteRechnen = (_, _) => quelle.Task));

        Task klick = Auslegungshilfe.Rechenknopf(cut).ClickAsync(new());

        Assert.Empty(cut.FindAll("button.epos-fortschritt-abbruch"));

        quelle.SetResult(Gerechnet(flotte));
        await klick;
    }

    [Fact]
    public async Task Abbrechen_ruft_den_Weg_genau_einmal()
    {
        int gerufen = 0;
        var quelle = new TaskCompletionSource<SpeicherFlottenErgebnis>();
        FlottenStudieKonfiguration flotte = Eine();

        var cut = Ansicht(Dienste(flotte, null, d =>
        {
            d.FlotteRechnen = (_, _) => quelle.Task;
            d.Abbrechen = () => gerufen++;
        }));

        Task klick = Auslegungshilfe.Rechenknopf(cut).ClickAsync(new());
        await cut.Find("button.epos-fortschritt-abbruch").ClickAsync(new());

        Assert.Equal(1, gerufen);

        // Ein zweiter Druck beschleunigt nichts - der Knopf bleibt stehen, aber gesperrt.
        Assert.True(cut.Find("button.epos-fortschritt-abbruch").HasAttribute("disabled"));

        quelle.SetResult(Gerechnet(flotte));
        await klick;
    }

    /// <summary>
    /// Der Dialog hatte einen Schließknopf, die Ansicht hat den RÜCKWEG. Ohne
    /// ungespeicherte Eingaben gibt es dabei keine Rückfrage (62b‑E‑1).
    /// </summary>
    [Fact]
    public async Task Schliessen_meldet_sich_beim_Wirt()
    {
        int zu = 0;
        var cut = Render<StromspeicherAuslegungSeite>(p => p
            .Add(x => x.Dienste, Dienste(Eine()))
            .Add(x => x.PlanerVerfuegbar, true)
            .Add(x => x.Geschlossen, EventCallback.Factory.Create(this, () => zu++)));

        await Auslegungshilfe.Knopf(cut, Resource.FLOTTE_SEITE_ZURUECK).ClickAsync(new());

        Assert.Equal(1, zu);
    }

    // =================================================================================
    //  Hilfen
    // =================================================================================

    private IRenderedComponent<StromspeicherAuslegungSeite> Ansicht(StromspeicherAuslegungDienste dienste)
        => Render<StromspeicherAuslegungSeite>(p => p
            .Add(x => x.Dienste, dienste)
            .Add(x => x.PlanerVerfuegbar, true));

    private static StromspeicherAuslegungDienste Dienste(
        FlottenStudieKonfiguration? flotte = null,
        SpeicherFlottenErgebnis? ergebnis = null,
        Action<StromspeicherAuslegungDienste>? zusatz = null,
        IReadOnlyList<SpeicherOptimierungLeistungspreisQuelle>? quellen = null)
    {
        var vorgaben = new SpeicherOptimierungVorgaben
        {
            Eingaben = new SpeicherOptimierungEingaben
            {
                Auslegung = new SpeicherAuslegungKonfiguration { Flotte = flotte }
            },
            Leistungspreisquellen = quellen ?? Array.Empty<SpeicherOptimierungLeistungspreisQuelle>()
        };

        var dienste = new StromspeicherAuslegungDienste
        {
            Vorgaben = () => vorgaben,
            EinstellungenSpeichern = _ => Task.FromResult(""),
            FlotteRechnen = (_, _) => Task.FromResult(
                ergebnis ?? new SpeicherFlottenErgebnis { Erfolg = true })
        };
        zusatz?.Invoke(dienste);
        return dienste;
    }

    /// <summary>Die EINE Einheit, wie der Kern sie aus der aktiven Variante vorbelegt.</summary>
    private static FlottenStudieKonfiguration Eine() => new()
    {
        Einheiten = new()
        {
            new()
            {
                Id = "a", Name = "Speicher 1", AnlageId = "42",
                KapazitaetKWh = 20, LadeleistungKw = 5, EntladeleistungKw = 5,
                Ladewirkungsgrad = 0.95, Entladewirkungsgrad = 0.95,
                SocMin = 0.1, SocMax = 0.9, SocStart = 0.5
            }
        },
        Optionen = new() { EnergieAusgleichEuroProKWh = 0.2 },
        Wirtschaftlichkeit = new() { ProjektjahreBeiWiederholung = 1 }
    };

    private static FlottenStudieKonfiguration Zwei()
    {
        FlottenStudieKonfiguration f = Eine();
        f.Einheiten.Add(new FlottenEinheit
        {
            Id = "b", Name = "Speicher 2",
            KapazitaetKWh = 16, LadeleistungKw = 4, EntladeleistungKw = 4,
            Ladewirkungsgrad = 0.95, Entladewirkungsgrad = 0.95,
            SocMin = 0.1, SocMax = 0.9, SocStart = 0.5
        });
        return f;
    }

    private static SpeicherFlottenErgebnis Gerechnet(FlottenStudieKonfiguration config)
    {
        var eingang = new FlottenEingang
        {
            DatenId = "Test",
            Istwerte = new()
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

    private static IElement Klappliste(IRenderedComponent<StromspeicherAuslegungSeite> cut, string beschriftung)
        => cut.FindAll("label").Single(x => x.TextContent.Contains(beschriftung, StringComparison.Ordinal))
              .QuerySelector("select")!;

    private static IElement Feld(IRenderedComponent<StromspeicherAuslegungSeite> cut, string beschriftung)
        => cut.FindAll("label").Single(x => x.TextContent.Contains(beschriftung, StringComparison.Ordinal))
              .QuerySelector("input")!;

    // ---------------------------------------------------------------------
    //  Der Weg zu den Quelldateien (dasselbe Verfahren wie UeberlagerungstitelTests)
    // ---------------------------------------------------------------------

    private static string[] Quelldateien()
    {
        string ui = Path.Combine(Wurzel(), "EPOS.UI");
        return Directory.GetFiles(ui, "*.razor", SearchOption.AllDirectories)
                        .Concat(Directory.GetFiles(ui, "*.cs", SearchOption.AllDirectories))
                        .Where(d => !d.Contains(Path.Combine("obj", ""), StringComparison.Ordinal)
                                 && !d.Contains(Path.Combine("bin", ""), StringComparison.Ordinal))
                        .ToArray();
    }

    private static string Wurzel()
    {
        DirectoryInfo? d = new(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        return d!.FullName;
    }
}
