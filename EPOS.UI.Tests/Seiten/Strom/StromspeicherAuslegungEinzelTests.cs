using System.Globalization;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Seiten.Simulation;
using EPOS.UI.Seiten.Strom;
using EPOS.UI.Dienste;
using EPOS.UI.Bausteine;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten.Strom;

/// <summary>
/// Der MODUS „EINZELSPEICHER" der Ansicht „Stromspeicher-Auslegung" (Paket P3,
/// Auftrag #192) — die übernommenen Prüffälle des gefallenen
/// <c>SpeicherOptimierungDialog</c> (W11b‑B‑5, Windows-Abnahme V2 vom 07.09.2026),
/// der seinerseits <c>Form_SpeicherOptimierung</c> ablöste.
///
/// <para><b>Was der Umzug ändert und was nicht.</b> Die Fachaussagen sind dieselben;
/// nur stehen die Bedienelemente jetzt auf den Blättern der Ablaufleiste — Suchraum
/// auf Schritt 1, Berechnungsart und Leistungspreis auf Schritt 3, das Ergebnis auf
/// Schritt 5 —, und der Startknopf ist Schritt 4 der Leiste.</para>
///
/// <para><b>Was hier geprüft wird, sind die zwei Befunde des Anwenders.</b></para>
/// <list type="number">
///   <item><description><b>„Texte überschneiden sich."</b> In der abgelösten Maske
///     lagen die aktuelle Auslegung (x = 14…414) und der Erklärtext zur Zielfunktion
///     (x = 310…1140) in derselben Zeile übereinander, und der Erklärtext ragte über
///     seine GroupBox hinaus. Hier steht er als <c>Herleitungszeile</c> UNTER der
///     Formulargruppe — geprüft wird die Reihenfolge im Markup, denn genau die war
///     der Fehler.</description></item>
///   <item><description><b>„Dialog stürzt nach kurzer Zeit ab."</b> Jeder Lauf hängte
///     eine weitere ScottPlot-Farbskala an denselben Plot. Hier hat der Dialog keinen
///     Zeichenzustand: Fünf Läufe hintereinander zeigen fünfmal dieselben zwei Bilder,
///     und nichts sammelt sich an.</description></item>
/// </list>
///
/// <para>Die Kultur ist auf de-DE gepinnt — die Punktzahl und die Kennzahlen sind
/// Text.</para>
/// </summary>
public class StromspeicherAuslegungEinzelTests : EposBunitContext
{
    public StromspeicherAuslegungEinzelTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // =================================================================================
    //  Gaben
    // =================================================================================

    private static SpeicherOptimierungVorgaben Vorgaben() => new()
    {
        Eingaben = new SpeicherOptimierungEingaben(),
        AktuelleAuslegung = "Aktuelle Auslegung: 250 kWh / 125 kW (0,5 C)"
    };

    /// <summary>
    /// Dieselben Gaben MIT zwei Leistungspreis-Quellen (Anwenderentscheid W11b‑E‑3,
    /// 10.09.2026) — Tarifstruktur und Energieträger Strom.
    /// </summary>
    private static SpeicherOptimierungVorgaben VorgabenMitQuellen() => new()
    {
        Eingaben = new SpeicherOptimierungEingaben(),
        AktuelleAuslegung = "Aktuelle Auslegung: 250 kWh / 125 kW (0,5 C)",
        Leistungspreisquellen = new[]
        {
            new SpeicherOptimierungLeistungspreisQuelle
            {
                Bezeichnung = "Tarifstruktur (Wirtschaftlichkeit): 120 €/(kW·a) — Stufe 2",
                WertEurProKwA = 120.0
            },
            new SpeicherOptimierungLeistungspreisQuelle
            {
                Bezeichnung = "Energieträger Strom: 95 €/(kW·a)",
                WertEurProKwA = 95.0
            }
        }
    };

    /// <summary>Ein fertiges Ergebnis — zwei Bilder, drei Kennzahlen, ein Hinweis.</summary>
    private static SpeicherOptimierungErgebnis Ergebnis(bool randlage = true)
    {
        var e = new SpeicherOptimierungErgebnis
        {
            Erfolg = true,
            Statuszeile = "120 Rasterpunkte in 0,3 s — Optimum 2.500 kWh / 1,5 C",
            KapazitaetKwh = 2500.0,
            CRate = 1.5,
            LeistungKw = 3750.0,
            ZielfunktionEur = 4000.0,
            PunkteGerechnet = 120,
            DauerSekunden = 0.3,
            Randlage = randlage,
            Hinweise = randlage
                ? new[] { "Optimum am Rand — Suchbereich erweitern (Kapazität an der Obergrenze)." }
                : System.Array.Empty<string>(),
            SchnittTitel = "Schnittkurve bei 1,5 C",
            RasterBild = Bild(1),
            SchnittBild = Bild(2),
            RasterCsv = "Phase;Kapazität [kWh]\r\nGrobraster;2500\r\n",
            Kennzahlen = new[]
            {
                new SpeicherOptimierungKennzahl
                {
                    Gruppe = SpeicherOptimierungCtrl.GRUPPE_AUSLEGUNG,
                    Bezeichnung = "Nennkapazität C_nom", Wert = "2500", Einheit = "kWh"
                },
                new SpeicherOptimierungKennzahl
                {
                    Gruppe = SpeicherOptimierungCtrl.GRUPPE_WIRTSCHAFT,
                    Bezeichnung = "Zielfunktion ΔJ", Wert = "4000,00", Einheit = "€/a"
                },
                new SpeicherOptimierungKennzahl
                {
                    Gruppe = SpeicherOptimierungCtrl.GRUPPE_SPEICHER,
                    Bezeichnung = "Speicherverluste", Wert = "-12", Einheit = "kWh/a", Negativ = true
                }
            }
        };
        return e;
    }

    /// <summary>Ein winziges, aber gueltiges PNG — der Dialog zeigt es nur an.</summary>
    private static byte[] Bild(byte kennung) => new byte[]
    {
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, kennung
    };

    /// <summary>
    /// Die Ansicht im Modus „Einzelspeicher", Blatt 1. Jeder Prüffall beginnt hier —
    /// so wie er im abgelösten Dialog auf dessen einzigem Blatt begann.
    /// </summary>
    private IRenderedComponent<StromspeicherAuslegungSeite> Aufbauen(
        Func<SpeicherOptimierungEingaben, Action<double?, string>,
             Task<SpeicherOptimierungErgebnis>>? rechnen = null,
        Action? abbrechen = null,
        Func<double, double, Rueckmeldung>? uebernehmen = null,
        Func<string, Task<Rueckmeldung>>? csv = null,
        Action? geschlossen = null,
        Func<SpeicherOptimierungVorgaben>? vorgaben = null,
        Action<double>? leistungspreis = null,
        Func<bool, IReadOnlyList<string>, Diagrammbereich?,
             SpeicherOptimierungBetriebsbild>? betrieb = null)
    {
        var dienste = new StromspeicherAuslegungDienste
        {
            Vorgaben = vorgaben ?? Vorgaben,
            EinzelRechnen = rechnen ?? ((_, _) => Task.FromResult(Ergebnis())),
            Abbrechen = abbrechen,
            AuslegungUebernehmen = uebernehmen,
            Csv = csv,
            LeistungspreisSchreiben = leistungspreis,
            Betriebsbild = betrieb
        };

        var cut = Render<StromspeicherAuslegungSeite>(p => p
            .Add(x => x.Dienste, dienste)
            .Add(x => x.PlanerVerfuegbar, false)
            .Add(x => x.Geschlossen,
                 geschlossen is null ? default : EventCallbackVon(geschlossen)));

        Auslegungshilfe.Modus(cut, AuslegungModus.Einzelspeicher);
        return cut;
    }

    /// <summary>Startet den Rasterlauf — Schritt 4 der Ablaufleiste.</summary>
    private static Task Starten(IRenderedComponent<StromspeicherAuslegungSeite> cut)
        => Auslegungshilfe.Rechenknopf(cut).ClickAsync(new());

    /// <summary>Das Ergebnisblatt (Schritt 5) — dort stehen Bilder und Kennzahlen.</summary>
    private static void ZumErgebnis(IRenderedComponent<StromspeicherAuslegungSeite> cut)
        => Auslegungshilfe.Schritt(cut, AuslegungSchritt.Ergebnis);

    /// <summary>Blatt 3 — Berechnungsart, Zielfunktion, Leistungspreis.</summary>
    private static void ZumBetrieb(IRenderedComponent<StromspeicherAuslegungSeite> cut)
        => Auslegungshilfe.Schritt(cut, AuslegungSchritt.Betrieb);

    /// <summary>Blatt 1 — der Suchraum.</summary>
    private static void ZumSuchraum(IRenderedComponent<StromspeicherAuslegungSeite> cut)
        => Auslegungshilfe.Schritt(cut, AuslegungSchritt.Speicher);

    /// <summary>Die Ergebniskomponente — sie hält die Schalterstellungen des Betriebsbildes.</summary>
    private static EinzelspeicherErgebnis Ergebnisblatt(
        IRenderedComponent<StromspeicherAuslegungSeite> cut)
        => cut.FindComponent<EinzelspeicherErgebnis>().Instance;

    private Microsoft.AspNetCore.Components.EventCallback EventCallbackVon(Action a)
        => Microsoft.AspNetCore.Components.EventCallback.Factory.Create(this, a);

    private Microsoft.AspNetCore.Components.EventCallback<T> EventCallbackVon<T>(Action<T> a)
        => Microsoft.AspNetCore.Components.EventCallback.Factory.Create(this, a);

    private static IElement Knopf(IRenderedComponent<StromspeicherAuslegungSeite> cut, string text)
        => Auslegungshilfe.Knopf(cut, text);

    // =================================================================================
    //  Befund 1 — die Ueberschneidung
    // =================================================================================

    /// <summary>
    /// Der Erklärtext zur Zielfunktion steht UNTER der Formulargruppe, nicht neben der
    /// Klappliste. Geprüft wird die Reihenfolge im Markup: Die Herleitungszeilen folgen
    /// dem Raster, sie stehen nicht darin.
    /// </summary>
    [Fact]
    public void Die_Erklaerung_steht_unter_der_Gruppe()
    {
        var cut = Aufbauen();
        string html = cut.Markup;

        int raster = html.IndexOf("epos-formularraster", StringComparison.Ordinal);
        int herleitung = html.IndexOf("epos-herleitung", StringComparison.Ordinal);
        int rasterEnde = html.LastIndexOf("epos-feld", StringComparison.Ordinal);

        Assert.True(raster >= 0, "Der Suchraum steht nicht im Formularraster.");
        Assert.True(herleitung > rasterEnde,
            "Die Herleitungszeile steht NICHT unter der Gruppe.");
    }

    [Fact]
    public void Beide_Erklaerungen_stehen_vollstaendig_da()
    {
        var cut = Aufbauen();

        // Der Text wird nicht gekuerzt und nicht abgeschnitten - in der abgeloesten
        // Maske ragte er 8 Bildpunkte ueber seine GroupBox hinaus. Seit P3 steht die
        // SUCHRAUMregel auf Blatt 1 und die ZIELFUNKTION auf Blatt 3: Sie gehoeren zu
        // verschiedenen Schritten und stehen deshalb nicht mehr untereinander.
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.OPT_HINWEIS_SUCHRAUM,
                        cut.Markup, StringComparison.Ordinal);

        ZumBetrieb(cut);
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.OPT_HINWEIS_ZIELFUNKTION,
                        cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Die_aktuelle_Auslegung_steht_in_einer_eigenen_Zeile()
    {
        var cut = Aufbauen();

        var zeile = cut.Find("p.epos-speicheropt-zeile");
        Assert.Contains("250 kWh", zeile.TextContent, StringComparison.Ordinal);
        Assert.Contains("120", zeile.TextContent, StringComparison.Ordinal);   // Rasterpunkte
    }

    // =================================================================================
    //  Der Feldbestand des Suchraums
    // =================================================================================

    [Fact]
    public void Der_Suchraum_traegt_die_sechs_Felder_und_die_drei_Schalter()
    {
        var cut = Aufbauen();

        // Blatt 1 „Speicherparameter": die sechs Zahlen und das Feinraster - alles,
        // was den RASTER beschreibt.
        foreach (string label in new[]
        {
            WindowsFormsApplication1.MyResource.Resource.OPT_LBL_CMIN,
            WindowsFormsApplication1.MyResource.Resource.OPT_LBL_CMAX,
            WindowsFormsApplication1.MyResource.Resource.OPT_LBL_STUETZSTELLEN,
            WindowsFormsApplication1.MyResource.Resource.OPT_LBL_RMIN,
            WindowsFormsApplication1.MyResource.Resource.OPT_LBL_RMAX,
            WindowsFormsApplication1.MyResource.Resource.OPT_LBL_RSCHRITT,
            WindowsFormsApplication1.MyResource.Resource.OPT_CHK_FEINRASTER
        })
            Assert.Contains(label, cut.Markup, StringComparison.Ordinal);

        // Blatt 3 „Betriebsfuehrung": Berechnungsart und Zielfunktionsschalter.
        ZumBetrieb(cut);
        foreach (string label in new[]
        {
            WindowsFormsApplication1.MyResource.Resource.OPT_LBL_STRATEGIE,
            WindowsFormsApplication1.MyResource.Resource.OPT_CHK_KVER
        })
            Assert.Contains(label, cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Die_Vorbelegung_kommt_aus_den_Gaben()
    {
        var cut = Aufbauen();

        Assert.Equal(500.0, cut.Instance.Eingaben.CMinKwh);
        Assert.Equal(5000.0, cut.Instance.Eingaben.CMaxKwh);
        Assert.Equal(10, cut.Instance.Eingaben.Stuetzstellen);
        Assert.True(cut.Instance.Eingaben.Feinraster);
    }

    /// <summary>Die Punktzahl folgt der Eingabe — sie kostet je Punkt einen Jahreslauf.</summary>
    [Fact]
    public void Die_Punktzahl_zieht_mit()
    {
        var cut = Aufbauen();
        Assert.Contains("120", cut.Find("span.epos-speicheropt-punkte").TextContent,
                        StringComparison.Ordinal);

        cut.FindAll("input[type=checkbox]")[0].Change(false);   // Feinraster aus (Blatt 1)
        Assert.Contains("60", cut.Find("span.epos-speicheropt-punkte").TextContent,
                        StringComparison.Ordinal);
    }

    // =================================================================================
    //  Der Lauf
    // =================================================================================

    [Fact]
    public async Task Ein_Lauf_zeigt_Bilder_Kennzahlen_und_Statuszeile()
    {
        var cut = Aufbauen();

        await Starten(cut);

        Assert.NotNull(cut.Instance.Einzelergebnis);
        Assert.Contains("120 Rasterpunkte", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(2, cut.FindAll("img.epos-chartbild").Count);
        Assert.Contains("Nennkapazität C_nom", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Zielfunktion ΔJ", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Speicherverluste", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>Beide Bilder stehen im Baustein <c>Diagramm</c> und sind damit zoombar.</summary>
    [Fact]
    public async Task Beide_Bilder_sind_zoombar()
    {
        var cut = Aufbauen();
        await Starten(cut);

        Assert.Equal(2, cut.FindAll("div.epos-diagramm").Count);
        Assert.All(cut.FindAll("img.epos-chartbild"),
                   b => Assert.NotNull(b.ParentElement?.ParentElement));
    }

    [Fact]
    public async Task Der_Rand_Hinweis_erscheint_als_Warnband()
    {
        var cut = Aufbauen();
        await Starten(cut);

        Assert.Contains("Optimum am Rand", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Ohne_Randlage_steht_kein_Hinweis()
    {
        var cut = Aufbauen(rechnen: (_, _) => Task.FromResult(Ergebnis(randlage: false)));
        await Starten(cut);

        Assert.DoesNotContain("Optimum am Rand", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// Der Fortschritt kommt aus dem Hintergrund an, solange der Lauf läuft — und
    /// verschwindet danach.
    /// </summary>
    [Fact]
    public async Task Der_Fortschritt_erscheint_waehrend_des_Laufs()
    {
        var quelle = new TaskCompletionSource<SpeicherOptimierungErgebnis>();
        Action<double?, string>? melder = null;

        var cut = Aufbauen(rechnen: (_, m) => { melder = m; return quelle.Task; });

        // Der Klick wird NICHT abgewartet: Sein Task endet erst, wenn der Behandler
        // fertig ist - und der wartet auf den Lauf. Das ist der Sinn der Uebung.
        Task klick = Starten(cut);

        Assert.True(cut.Instance.Laeuft);
        Assert.NotNull(cut.Find("progress"));

        melder!(0.5, "Rasterpunkt 60 von 120");
        cut.WaitForAssertion(() =>
            Assert.Contains("Rasterpunkt 60 von 120", cut.Markup, StringComparison.Ordinal));

        quelle.SetResult(Ergebnis());
        await klick;

        Assert.False(cut.Instance.Laeuft);
        Assert.Empty(cut.FindAll("progress"));
    }

    /// <summary>Ohne Abbruchrückruf gibt es keinen Abbrechen-Knopf (Hausregel).</summary>
    [Fact]
    public async Task Ohne_Rueckruf_kein_Abbrechen_Knopf()
    {
        var quelle = new TaskCompletionSource<SpeicherOptimierungErgebnis>();
        var cut = Aufbauen(rechnen: (_, _) => quelle.Task);

        Task klick = Starten(cut);

        Assert.Empty(cut.FindAll("button.epos-fortschritt-abbruch"));

        quelle.SetResult(Ergebnis());
        await klick;
    }

    [Fact]
    public async Task Abbrechen_ruft_den_Weg_genau_einmal()
    {
        int gerufen = 0;
        var quelle = new TaskCompletionSource<SpeicherOptimierungErgebnis>();

        var cut = Aufbauen(rechnen: (_, _) => quelle.Task, abbrechen: () => gerufen++);

        Task klick = Starten(cut);
        await cut.Find("button.epos-fortschritt-abbruch").ClickAsync(new());

        Assert.Equal(1, gerufen);

        // Ein zweiter Druck beschleunigt nichts - der Knopf bleibt stehen, aber gesperrt.
        Assert.True(cut.Find("button.epos-fortschritt-abbruch").HasAttribute("disabled"));

        quelle.SetResult(Ergebnis());
        await klick;
    }

    // =================================================================================
    //  Eingabefehler
    // =================================================================================

    /// <summary>
    /// Ein unbrauchbarer Suchraum SPERRT den Lauf — er beginnt gar nicht erst, und die
    /// Meldung steht im Klartext da.
    /// </summary>
    [Fact]
    public async Task Ein_Eingabefehler_startet_keinen_Lauf()
    {
        int laeufe = 0;
        var cut = Aufbauen(rechnen: (_, _) => { laeufe++; return Task.FromResult(Ergebnis()); });

        // "Kapazität bis" unter "von" - dieselbe Bedingung, die die abgeloeste Maske
        // mit einer MessageBox meldete.
        cut.FindAll("input[type=text]")[1].Input("100");
        await Starten(cut);

        Assert.Equal(0, laeufe);
        Assert.Null(cut.Instance.Einzelergebnis);
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.OPT_MSG_CMAX,
                        cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Eine_Fehlermeldung_des_Kerns_steht_im_Band()
    {
        var cut = Aufbauen(rechnen: (_, _) => Task.FromResult(new SpeicherOptimierungErgebnis
        {
            Erfolg = false,
            Meldung = "Die Simulation ist noch nicht gerechnet."
        }));

        await Starten(cut);

        Assert.Null(cut.Instance.Einzelergebnis);
        Assert.Contains("Die Simulation ist noch nicht gerechnet.", cut.Markup,
                        StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("img.epos-chartbild"));
    }

    // =================================================================================
    //  Uebernehmen
    // =================================================================================

    [Fact]
    public async Task Ohne_Ergebnis_ist_Uebernehmen_gesperrt()
    {
        var cut = Aufbauen(uebernehmen: (_, _) => new Rueckmeldung(true, ""));

        // Ohne Lauf gibt es das Ergebnisblatt gar nicht - der Schritt ist weich
        // gesperrt und nennt seinen Grund (Hausregel W16b-E-6).
        AngleSharp.Dom.IElement schritt =
            Auslegungshilfe.Schrittknopf(cut, AuslegungSchritt.Ergebnis);
        Assert.Equal("true", schritt.GetAttribute("aria-disabled"));
        Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.OPT_BTN_UEBERNEHMEN,
                              cut.Markup, StringComparison.Ordinal);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Übernehmen fragt zurück und ruft den Weg dann GENAU EINMAL — mit den Zahlen des
    /// Bestpunkts.
    /// </summary>
    [Fact]
    public async Task Uebernehmen_fragt_zurueck_und_ruft_den_Weg_genau_einmal()
    {
        var gerufen = new List<(double Kwh, double Kw)>();
        var cut = Aufbauen(uebernehmen: (kwh, kw) =>
        {
            gerufen.Add((kwh, kw));
            return new Rueckmeldung(true, "Die Auslegung wurde übernommen.");
        });

        await Starten(cut);
        await Knopf(cut, WindowsFormsApplication1.MyResource.Resource.OPT_BTN_UEBERNEHMEN)
                  .ClickAsync(new());

        // Die Rueckfrage steht - noch ist nichts geschrieben.
        Assert.Contains("2500", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(gerufen);

        await Knopf(cut, "Ja").ClickAsync(new());

        Assert.Single(gerufen);
        Assert.Equal(2500.0, gerufen[0].Kwh);
        Assert.Equal(3750.0, gerufen[0].Kw);
    }

    [Fact]
    public async Task Ein_Nein_schreibt_nichts()
    {
        var gerufen = new List<(double Kwh, double Kw)>();
        var cut = Aufbauen(uebernehmen: (kwh, kw) =>
        {
            gerufen.Add((kwh, kw));
            return new Rueckmeldung(true, "");
        });

        await Starten(cut);
        await Knopf(cut, WindowsFormsApplication1.MyResource.Resource.OPT_BTN_UEBERNEHMEN)
                  .ClickAsync(new());
        await Knopf(cut, "Nein").ClickAsync(new());

        Assert.Empty(gerufen);
    }

    /// <summary>
    /// „Die Auslegung wurde übernommen" ist ein ERFOLG und gehört nicht in ein rotes
    /// Band; „konnte nicht geschrieben werden" schon.
    /// </summary>
    [Fact]
    public async Task Die_Meldung_der_Huelle_steht_in_der_richtigen_Stufe()
    {
        var gut = Aufbauen(uebernehmen: (_, _) =>
            new Rueckmeldung(true, "Die Auslegung wurde übernommen."));
        await Uebernehmen(gut);

        Assert.Contains("Die Auslegung wurde übernommen.", gut.Markup, StringComparison.Ordinal);
        Assert.NotNull(gut.Find(".epos-warnbanner--erfolg"));

        var schlecht = Aufbauen(uebernehmen: (_, _) =>
            new Rueckmeldung(false, "Die Auslegung konnte nicht geschrieben werden."));
        await Uebernehmen(schlecht);

        Assert.Contains("Die Auslegung konnte nicht geschrieben werden.", schlecht.Markup,
                        StringComparison.Ordinal);
        Assert.Empty(schlecht.FindAll(".epos-warnbanner--erfolg"));
    }

    /// <summary>Rechnen, „Bestpunkt übernehmen" drücken und die Rückfrage bejahen.</summary>
    private static async Task Uebernehmen(IRenderedComponent<StromspeicherAuslegungSeite> cut)
    {
        await Starten(cut);
        await Knopf(cut, WindowsFormsApplication1.MyResource.Resource.OPT_BTN_UEBERNEHMEN)
                  .ClickAsync(new());
        await Knopf(cut, "Ja").ClickAsync(new());
    }

    // =================================================================================
    //  CSV und Schliessen
    // =================================================================================

    [Fact]
    public async Task Ohne_Rueckruf_kein_CSV_Knopf()
    {
        var cut = Aufbauen();
        await Starten(cut);

        Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.OPT_BTN_CSV,
                              cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Der_CSV_Weg_bekommt_den_fertigen_Text()
    {
        string? bekommen = null;
        var cut = Aufbauen(csv: t => { bekommen = t; return Task.FromResult(Rueckmeldung.Still); });

        await Starten(cut);
        await Knopf(cut, WindowsFormsApplication1.MyResource.Resource.OPT_BTN_CSV).ClickAsync(new());

        Assert.NotNull(bekommen);
        Assert.StartsWith("Phase;", bekommen, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Schliessen_meldet_sich_beim_Wirt()
    {
        int zu = 0;
        var cut = Aufbauen(geschlossen: () => zu++);

        // Der Dialog hatte einen Schliessknopf, die Ansicht hat den RUECKWEG. Ohne
        // ungespeicherte Eingaben gibt es dabei keine Rueckfrage (62b-E-1).
        await Knopf(cut, WindowsFormsApplication1.MyResource.Resource.FLOTTE_SEITE_ZURUECK)
                  .ClickAsync(new());

        cut.WaitForAssertion(() => Assert.Equal(1, zu));
    }

    // =================================================================================
    //  Befund 2 — kein Zeichenzustand, der sich ansammelt
    // =================================================================================

    /// <summary>
    /// <b>Fünf Läufe hintereinander.</b> In der abgelösten Maske hängte jeder Lauf eine
    /// weitere Farbskala an denselben ScottPlot-Plot (<c>Plot.Clear()</c> räumt
    /// Plottables, aber keine Panels); die Zeichenfläche schrumpfte je Lauf um rund 78
    /// Bildpunkte und war ab dem achten Lauf null. Hier zeigt jeder Lauf GENAU zwei
    /// Bilder, und keines stammt aus einem früheren.
    /// </summary>
    [Fact]
    public async Task Fuenf_Laeufe_hintereinander_zeigen_immer_genau_zwei_Bilder()
    {
        int lauf = 0;
        var cut = Aufbauen(rechnen: (_, _) =>
        {
            lauf++;
            var e = Ergebnis();
            e.RasterBild = Bild((byte)lauf);
            e.SchnittBild = Bild((byte)(100 + lauf));
            return Task.FromResult(e);
        });

        for (int i = 1; i <= 5; i++)
        {
            await Starten(cut);

            Assert.Equal(2, cut.FindAll("img.epos-chartbild").Count);
            Assert.Equal(2, cut.FindAll("div.epos-diagramm").Count);
            Assert.Equal(i, lauf);

            // Das Bild ist DAS DES LAUFS - kein Rest des vorigen.
            Assert.Same(cut.Instance.Einzelergebnis!.RasterBild, cut.Instance.Einzelergebnis.RasterBild);
            Assert.Equal((byte)i, cut.Instance.Einzelergebnis.RasterBild[8]);
        }

        // Auch die Kennzahlen bleiben drei - nichts staut sich an.
        Assert.Equal(3, cut.Instance.Einzelergebnis!.Kennzahlen.Count);
    }

    /// <summary>
    /// Ein zweiter Lauf, der scheitert, LÄSST DAS ALTE ERGEBNIS NICHT STEHEN. Sonst
    /// stünde eine Fehlermeldung über einem Bild, das dazu nicht passt.
    /// </summary>
    [Fact]
    public async Task Ein_gescheiterter_zweiter_Lauf_raeumt_das_erste_Ergebnis_weg()
    {
        int lauf = 0;
        var cut = Aufbauen(rechnen: (_, _) =>
        {
            lauf++;
            return Task.FromResult(lauf == 1
                ? Ergebnis()
                : new SpeicherOptimierungErgebnis { Erfolg = false, Meldung = "Fehlgeschlagen." });
        });

        await Starten(cut);
        Assert.Equal(2, cut.FindAll("img.epos-chartbild").Count);

        await Starten(cut);
        Assert.Empty(cut.FindAll("img.epos-chartbild"));
        Assert.Contains("Fehlgeschlagen.", cut.Markup, StringComparison.Ordinal);
    }

    // =================================================================================
    //  Ohne Gaben
    // =================================================================================

    /// <summary>
    /// Ohne Rechendelegat zeichnet der Dialog, aber der Startknopf bleibt gesperrt —
    /// die Rastersuche braucht einen gelaufenen Simulationsdurchgang.
    /// </summary>
    [Fact]
    public void Ohne_Gaben_zeichnet_er_und_sperrt_den_Start()
    {
        var cut = Render<StromspeicherAuslegungSeite>(p => p.Add(x => x.PlanerVerfuegbar, false));
        Auslegungshilfe.Modus(cut, AuslegungModus.Einzelspeicher);

        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.OPT_GRP_SUCHRAUM,
                        cut.Markup, StringComparison.Ordinal);
        Assert.True(Auslegungshilfe.Rechenknopf(cut).HasAttribute("disabled"));
    }

    // =================================================================================
    //  ANWENDERENTSCHEID W11b‑E‑3 (10.09.2026) — die Lastspitzenkappung
    // =================================================================================

    /// <summary>
    /// Die Klappliste „Berechnungsart" auf Blatt 3. <b>Die ERSTE Auswahl der Seite ist
    /// der MODUS-Umschalter</b> der Ablaufleiste — er steht vor jedem Blatt.
    /// </summary>
    private static IElement Berechnungsart(IRenderedComponent<StromspeicherAuslegungSeite> cut)
        => cut.FindAll("select")[1];

    /// <summary>Geht auf Blatt 3 und stellt die Klappliste auf die Lastspitzenkappung.</summary>
    private static void KappungWaehlen(IRenderedComponent<StromspeicherAuslegungSeite> cut)
    {
        ZumBetrieb(cut);
        Berechnungsart(cut).Change("2");
    }

    [Fact]
    public void Die_Klappliste_bietet_drei_Berechnungsarten()
    {
        var cut = Aufbauen();
        ZumBetrieb(cut);
        var eintraege = Berechnungsart(cut).QuerySelectorAll("option");

        Assert.Equal(3, eintraege.Length);
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_BERECHNUNG_ANZEIGE_LASTSPITZENKAPPUNG,
                     eintraege[2].TextContent.Trim());
    }

    /// <summary>
    /// Der Leistungspreis erscheint NUR bei der Lastspitzenkappung — bei Dauer- und
    /// Nachtnutzung wäre er ein Feld ohne Wirkung.
    /// </summary>
    [Fact]
    public void Das_Leistungspreisfeld_erscheint_erst_mit_der_Kappung()
    {
        var cut = Aufbauen(vorgaben: VorgabenMitQuellen, leistungspreis: _ => { });
        ZumBetrieb(cut);

        Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.OPT_LBL_LEISTUNGSPREIS,
                              cut.Markup, StringComparison.Ordinal);

        KappungWaehlen(cut);

        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.OPT_LBL_LEISTUNGSPREIS,
                        cut.Markup, StringComparison.Ordinal);
        Assert.Equal(OptimiererStrategie.Lastspitzenkappung, cut.Instance.Eingaben.Strategie);
    }

    /// <summary>Die Zielfunktionszeile wechselt mit der Berechnungsart.</summary>
    [Fact]
    public void Die_Herleitung_nennt_die_Zielfunktion_der_Kappung()
    {
        var cut = Aufbauen(vorgaben: VorgabenMitQuellen, leistungspreis: _ => { });
        KappungWaehlen(cut);

        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.OPT_HINWEIS_ZIEL_LASTSPITZE,
                        cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.OPT_HINWEIS_ZIELFUNKTION,
                              cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// Die Übernahme aus einer Quelle SETZT das Feld und SCHREIBT — sie ist kein
    /// Vorschlag (Anwenderentscheid W11b‑E‑3).
    /// </summary>
    [Fact]
    public void Die_Uebernahme_aus_einer_Quelle_setzt_das_Feld_und_schreibt()
    {
        var geschrieben = new List<double>();
        var cut = Aufbauen(vorgaben: VorgabenMitQuellen, leistungspreis: geschrieben.Add);

        KappungWaehlen(cut);

        // Nach Modus-Umschalter und Berechnungsart folgt die Quellenliste; Id = Listenindex.
        var quellen = cut.FindAll("select")[2];
        Assert.Equal(3, quellen.QuerySelectorAll("option").Length);   // Platzhalter + zwei Quellen

        quellen.Change("1");                                          // Energietraeger Strom

        Assert.Equal(95.0, cut.Instance.Eingaben.LeistungspreisEurProKwA);
        Assert.Equal(new[] { 95.0 }, geschrieben);
    }

    /// <summary>Jedes Feld schreibt sofort — auch die Zahl von Hand.</summary>
    [Fact]
    public void Das_Leistungspreisfeld_schreibt_sofort()
    {
        var geschrieben = new List<double>();
        var cut = Aufbauen(vorgaben: VorgabenMitQuellen, leistungspreis: geschrieben.Add);

        KappungWaehlen(cut);

        // Auf Blatt 3 steht NUR der Leistungspreis - der Suchraum ist Blatt 1.
        cut.FindAll("input[type=text]")[0].Input("142,5");

        Assert.Equal(142.5, cut.Instance.Eingaben.LeistungspreisEurProKwA);
        Assert.Equal(new[] { 142.5 }, geschrieben);
    }

    /// <summary>Ohne gepflegte Quelle gibt es keine Klappliste — keine Liste mit Nullen.</summary>
    [Fact]
    public void Ohne_Quellen_bleibt_die_Uebernahme_weg()
    {
        var cut = Aufbauen(leistungspreis: _ => { });
        KappungWaehlen(cut);

        // Modus-Umschalter und Berechnungsart - keine dritte Liste.
        Assert.Equal(2, cut.FindAll("select").Count);
        Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.OPT_LBL_LP_QUELLE,
                              cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>Ohne Schreibdelegat bleibt das Feld Anzeige („Kein Delegat ist kein Knopf").</summary>
    [Fact]
    public void Ohne_Schreibweg_ist_das_Feld_nur_Anzeige()
    {
        var cut = Aufbauen(vorgaben: VorgabenMitQuellen);
        KappungWaehlen(cut);

        Assert.True(cut.FindAll("input[type=text]")[0].HasAttribute("disabled"));
        Assert.True(cut.FindAll("select")[2].HasAttribute("disabled"));
    }

    // =================================================================================
    //  BEFUND W11b‑B‑25 (09.09.2026) — „Grafik zu gross", „Dialog uebersichtlicher"
    // =================================================================================

    /// <summary>
    /// Rasterkarte und Schnittkurve stehen in EINER Zeile nebeneinander; untereinander
    /// schob die Karte die Kurve aus dem sichtbaren Bereich.
    /// </summary>
    [Fact]
    public async Task Beide_Bilder_stehen_nebeneinander_in_einer_Zeile()
    {
        var cut = Aufbauen();
        await Starten(cut);

        var zeile = cut.Find("div.epos-speicheropt-diagramme");
        Assert.Equal(2, zeile.QuerySelectorAll("img.epos-chartbild").Length);
        Assert.Empty(cut.FindAll("section.epos-simerg-diagrammzeile"));
    }

    /// <summary>
    /// Die Kennzahlen stehen als Wertelisten nebeneinander statt als eine lange
    /// Tabelle — je Gruppe eine Liste, leere Gruppen entfallen.
    /// </summary>
    [Fact]
    public async Task Die_Kennzahlen_stehen_als_Wertelisten()
    {
        var cut = Aufbauen();
        await Starten(cut);

        Assert.Empty(cut.FindAll("table.epos-speicheropt-kennzahlen"));
        Assert.Equal(3, cut.FindAll("dl.epos-simerg-werte").Count);   // drei belegte Gruppen
        Assert.Contains("Nennkapazität C_nom", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>Die Hinweise des Kerns stehen als Warnbänder, nicht als Fließtext.</summary>
    [Fact]
    public async Task Die_Hinweise_stehen_als_Warnbaender()
    {
        var cut = Aufbauen();
        await Starten(cut);

        var baender = cut.FindAll(".epos-warnbanner-text");
        Assert.Contains(baender, b => b.TextContent.Contains("Optimum am Rand", StringComparison.Ordinal));
    }

    // =================================================================================
    //  BEFUND W11b‑B‑25 — „Lastgang und Speicherung in einer Grafik"
    // =================================================================================

    /// <summary>Dasselbe Ergebnis, aber MIT dem Bild des Bestpunkts.</summary>
    private static SpeicherOptimierungErgebnis ErgebnisMitBetrieb()
    {
        var e = Ergebnis();
        e.BetriebBild = Bild(7);
        e.BetriebTitel = "Lastgang und Speicherbetrieb [kW] — Woche der Jahresspitze";
        return e;
    }

    /// <summary>Ein Neuzeichnen-Weg, der jede Anfrage mitschreibt.</summary>
    private sealed class Zeichner
    {
        internal readonly List<(bool Jahr, string[] Reihen, bool Bereich)> Anfragen = new();
        private byte kennung = 20;

        internal SpeicherOptimierungBetriebsbild Zeichne(
            bool jahr, IReadOnlyList<string> reihen, Diagrammbereich? bereich)
        {
            Anfragen.Add((jahr, reihen is null ? Array.Empty<string>() : reihen.ToArray(),
                          bereich is not null));
            kennung++;
            return new SpeicherOptimierungBetriebsbild
            {
                Png = Bild(kennung),
                Titel = jahr ? "ganzes Jahr" : "Woche der Jahresspitze",
                Stuetzstellen = jahr ? 8760 : 168
            };
        }
    }

    /// <summary>
    /// Ohne Betriebsbild bleibt es bei den zwei Rasterbildern — der Dialog erfindet
    /// keines („Bild nur mit BetriebBild").
    /// </summary>
    [Fact]
    public async Task Ohne_Betriebsbild_bleibt_es_bei_zwei_Bildern()
    {
        var cut = Aufbauen();
        await Starten(cut);

        Assert.Null(Ergebnisblatt(cut).BetriebsbildPng);
        Assert.Equal(2, cut.FindAll("img.epos-chartbild").Count);
        Assert.Empty(cut.FindAll("section.epos-speicheropt-betrieb"));
    }

    [Fact]
    public async Task Mit_Betriebsbild_erscheint_das_dritte_Bild()
    {
        var cut = Aufbauen(rechnen: (_, _) => Task.FromResult(ErgebnisMitBetrieb()));
        await Starten(cut);

        Assert.Equal(3, cut.FindAll("img.epos-chartbild").Count);
        var abschnitt = cut.Find("section.epos-speicheropt-betrieb");
        Assert.Single(abschnitt.QuerySelectorAll("img.epos-chartbild"));
        Assert.False(Ergebnisblatt(cut).BetriebGanzesJahr);
    }

    /// <summary>Ohne Neuzeichnen-Weg gibt es keine Umschalter („Kein Delegat ist kein Knopf").</summary>
    [Fact]
    public async Task Ohne_Neuzeichnen_gibt_es_keine_Umschalter()
    {
        var cut = Aufbauen(rechnen: (_, _) => Task.FromResult(ErgebnisMitBetrieb()));
        await Starten(cut);

        // Auf dem Ergebnisblatt steht ohne Neuzeichnen-Weg KEIN Schalter.
        Assert.Empty(cut.FindAll("input[type=checkbox]"));
    }

    /// <summary>
    /// Der Umschalter „Ganzes Jahr" zeichnet das Bild neu — mit EINEM Jahreslauf des
    /// Bestpunkts, nicht mit einer neuen Rastersuche.
    /// </summary>
    [Fact]
    public async Task Der_Umschalter_auf_das_ganze_Jahr_zeichnet_neu()
    {
        var zeichner = new Zeichner();
        var cut = Aufbauen(rechnen: (_, _) => Task.FromResult(ErgebnisMitBetrieb()),
                           betrieb: zeichner.Zeichne);

        await Starten(cut);

        byte[] vorher = Ergebnisblatt(cut).BetriebsbildPng!;
        cut.FindAll("input[type=checkbox]")[0].Change(true);   // „Ganzes Jahr"

        Assert.Single(zeichner.Anfragen);
        Assert.True(zeichner.Anfragen[0].Jahr);
        Assert.True(Ergebnisblatt(cut).BetriebGanzesJahr);
        Assert.NotEqual(vorher, Ergebnisblatt(cut).BetriebsbildPng);
    }

    /// <summary>Eine abgewählte Reihe fehlt in der nächsten Anfrage.</summary>
    [Fact]
    public async Task Eine_abgewaehlte_Reihe_faellt_aus_der_Anfrage()
    {
        var zeichner = new Zeichner();
        var cut = Aufbauen(rechnen: (_, _) => Task.FromResult(ErgebnisMitBetrieb()),
                           betrieb: zeichner.Zeichne);

        await Starten(cut);

        // Schalter 0 = „Ganzes Jahr", 1 = Netzbezug ohne Speicher.
        cut.FindAll("input[type=checkbox]")[1].Change(false);

        Assert.Single(zeichner.Anfragen);
        Assert.DoesNotContain(SpeicherOptimierungCtrl.REIHE_OHNE, zeichner.Anfragen[0].Reihen);
        Assert.Contains(SpeicherOptimierungCtrl.REIHE_MIT, zeichner.Anfragen[0].Reihen);
    }

    /// <summary>
    /// Alles abgewählt heißt KEINE Reihe — die Hausregel der Ergebnisseite
    /// (<c>Doku_Simulationsergebnis_Darstellung.md</c>, § 5): eine leere Liste fällt
    /// NICHT auf „alle" zurück, das Bild zeigt den Leerhinweis des Zeichners.
    /// </summary>
    [Fact]
    public async Task Alles_abgewaehlt_liefert_eine_leere_Reihenwahl()
    {
        var zeichner = new Zeichner();
        var cut = Aufbauen(rechnen: (_, _) => Task.FromResult(ErgebnisMitBetrieb()),
                           betrieb: zeichner.Zeichne);

        await Starten(cut);

        // Der Lauf stand auf Dauernutzung: drei Reihenschalter (ohne die Schwelle).
        Assert.Equal(4, cut.FindAll("input[type=checkbox]").Count);   // Jahr + 3 Reihen

        cut.FindAll("input[type=checkbox]")[1].Change(false);
        cut.FindAll("input[type=checkbox]")[2].Change(false);
        cut.FindAll("input[type=checkbox]")[3].Change(false);

        Assert.Equal(3, zeichner.Anfragen.Count);

        // Die SCHWELLE steht noch in der Wahl - sie hat ohne Kappung keinen Schalter.
        Assert.Equal(new[] { SpeicherOptimierungCtrl.REIHE_SCHWELLE },
                     zeichner.Anfragen[^1].Reihen);
    }
}
