using System.Globalization;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die AUSLEGUNGSOPTIMIERUNG des Stromspeichers (W11b‑B‑5, Windows-Abnahme V2 vom
/// 07.09.2026) — der Nachfolger von <c>Form_SpeicherOptimierung</c>, der letzten
/// WinForms-Fachmaske.
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
public class SpeicherOptimierungDialogTests : BunitContext
{
    public SpeicherOptimierungDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
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

    private IRenderedComponent<SpeicherOptimierungDialog> Aufbauen(
        Func<SpeicherOptimierungEingaben, Action<double?, string>,
             Task<SpeicherOptimierungErgebnis>>? rechnen = null,
        Action? abbrechen = null,
        Action<(double Kwh, double Kw)>? uebernehmen = null,
        Action<string>? csv = null,
        Action? geschlossen = null,
        string meldung = "",
        bool meldungErfolg = false)
    {
        return Render<SpeicherOptimierungDialog>(p => p
            .Add(x => x.Vorgaben, Vorgaben)
            .Add(x => x.Rechnen, rechnen ?? ((_, _) => Task.FromResult(Ergebnis())))
            .Add(x => x.Abbrechen, abbrechen is null ? default : EventCallbackVon(abbrechen))
            .Add(x => x.Uebernehmen, uebernehmen is null ? default : EventCallbackVon(uebernehmen))
            .Add(x => x.Csv, csv is null ? default : EventCallbackVon(csv))
            .Add(x => x.Geschlossen, geschlossen is null ? default : EventCallbackVon(geschlossen))
            .Add(x => x.Meldung, meldung)
            .Add(x => x.MeldungErfolg, meldungErfolg));
    }

    private Microsoft.AspNetCore.Components.EventCallback EventCallbackVon(Action a)
        => Microsoft.AspNetCore.Components.EventCallback.Factory.Create(this, a);

    private Microsoft.AspNetCore.Components.EventCallback<T> EventCallbackVon<T>(Action<T> a)
        => Microsoft.AspNetCore.Components.EventCallback.Factory.Create(this, a);

    private static IElement Knopf(IRenderedComponent<SpeicherOptimierungDialog> cut, string text)
        => cut.FindAll("button").Single(b => b.TextContent.Trim() == text);

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
        // Maske ragte er 8 Bildpunkte ueber seine GroupBox hinaus.
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.OPT_HINWEIS_ZIELFUNKTION,
                        cut.Markup, StringComparison.Ordinal);
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.OPT_HINWEIS_SUCHRAUM,
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
        string html = cut.Markup;

        foreach (string label in new[]
        {
            WindowsFormsApplication1.MyResource.Resource.OPT_LBL_CMIN,
            WindowsFormsApplication1.MyResource.Resource.OPT_LBL_CMAX,
            WindowsFormsApplication1.MyResource.Resource.OPT_LBL_STUETZSTELLEN,
            WindowsFormsApplication1.MyResource.Resource.OPT_LBL_RMIN,
            WindowsFormsApplication1.MyResource.Resource.OPT_LBL_RMAX,
            WindowsFormsApplication1.MyResource.Resource.OPT_LBL_RSCHRITT,
            WindowsFormsApplication1.MyResource.Resource.OPT_LBL_STRATEGIE,
            WindowsFormsApplication1.MyResource.Resource.OPT_CHK_FEINRASTER,
            WindowsFormsApplication1.MyResource.Resource.OPT_CHK_KVER
        })
            Assert.Contains(label, html, StringComparison.Ordinal);
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

        cut.FindAll("input[type=checkbox]")[0].Change(false);   // Feinraster aus
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

        await cut.Find("button.epos-simerg-knopf").ClickAsync(new());

        Assert.NotNull(cut.Instance.Ergebnis);
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
        await cut.Find("button.epos-simerg-knopf").ClickAsync(new());

        Assert.Equal(2, cut.FindAll("div.epos-diagramm").Count);
        Assert.All(cut.FindAll("img.epos-chartbild"),
                   b => Assert.NotNull(b.ParentElement?.ParentElement));
    }

    [Fact]
    public async Task Der_Rand_Hinweis_erscheint_als_Warnband()
    {
        var cut = Aufbauen();
        await cut.Find("button.epos-simerg-knopf").ClickAsync(new());

        Assert.Contains("Optimum am Rand", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Ohne_Randlage_steht_kein_Hinweis()
    {
        var cut = Aufbauen(rechnen: (_, _) => Task.FromResult(Ergebnis(randlage: false)));
        await cut.Find("button.epos-simerg-knopf").ClickAsync(new());

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
        Task klick = cut.Find("button.epos-simerg-knopf").ClickAsync(new());

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

        Task klick = cut.Find("button.epos-simerg-knopf").ClickAsync(new());

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

        Task klick = cut.Find("button.epos-simerg-knopf").ClickAsync(new());
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
        await cut.Find("button.epos-simerg-knopf").ClickAsync(new());

        Assert.Equal(0, laeufe);
        Assert.Null(cut.Instance.Ergebnis);
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

        await cut.Find("button.epos-simerg-knopf").ClickAsync(new());

        Assert.Null(cut.Instance.Ergebnis);
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
        var cut = Aufbauen(uebernehmen: _ => { });

        Assert.True(Knopf(cut, WindowsFormsApplication1.MyResource.Resource.OPT_BTN_UEBERNEHMEN)
                        .HasAttribute("disabled"));
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
        var cut = Aufbauen(uebernehmen: a => gerufen.Add(a));

        await cut.Find("button.epos-simerg-knopf").ClickAsync(new());
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
        var cut = Aufbauen(uebernehmen: a => gerufen.Add(a));

        await cut.Find("button.epos-simerg-knopf").ClickAsync(new());
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
    public void Die_Meldung_der_Huelle_steht_in_der_richtigen_Stufe()
    {
        var gut = Aufbauen(meldung: "Die Auslegung wurde übernommen.", meldungErfolg: true);
        Assert.Contains("Die Auslegung wurde übernommen.", gut.Markup, StringComparison.Ordinal);
        Assert.NotNull(gut.Find(".epos-warnbanner--erfolg"));

        var schlecht = Aufbauen(meldung: "Die Auslegung konnte nicht geschrieben werden.");
        Assert.Empty(schlecht.FindAll(".epos-warnbanner--erfolg"));
    }

    // =================================================================================
    //  CSV und Schliessen
    // =================================================================================

    [Fact]
    public async Task Ohne_Rueckruf_kein_CSV_Knopf()
    {
        var cut = Aufbauen();
        await cut.Find("button.epos-simerg-knopf").ClickAsync(new());

        Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.OPT_BTN_CSV,
                              cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Der_CSV_Weg_bekommt_den_fertigen_Text()
    {
        string? bekommen = null;
        var cut = Aufbauen(csv: t => bekommen = t);

        await cut.Find("button.epos-simerg-knopf").ClickAsync(new());
        await Knopf(cut, WindowsFormsApplication1.MyResource.Resource.OPT_BTN_CSV).ClickAsync(new());

        Assert.NotNull(bekommen);
        Assert.StartsWith("Phase;", bekommen, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Schliessen_meldet_sich_beim_Wirt()
    {
        int zu = 0;
        var cut = Aufbauen(geschlossen: () => zu++);

        await Knopf(cut, WindowsFormsApplication1.MyResource.Resource.OPT_BTN_SCHLIESSEN)
                  .ClickAsync(new());

        Assert.Equal(1, zu);
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
            await cut.Find("button.epos-simerg-knopf").ClickAsync(new());

            Assert.Equal(2, cut.FindAll("img.epos-chartbild").Count);
            Assert.Equal(2, cut.FindAll("div.epos-diagramm").Count);
            Assert.Equal(i, lauf);

            // Das Bild ist DAS DES LAUFS - kein Rest des vorigen.
            Assert.Same(cut.Instance.Ergebnis!.RasterBild, cut.Instance.Ergebnis.RasterBild);
            Assert.Equal((byte)i, cut.Instance.Ergebnis.RasterBild[8]);
        }

        // Auch die Kennzahlen bleiben drei - nichts staut sich an.
        Assert.Equal(3, cut.Instance.Ergebnis!.Kennzahlen.Count);
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

        await cut.Find("button.epos-simerg-knopf").ClickAsync(new());
        Assert.Equal(2, cut.FindAll("img.epos-chartbild").Count);

        await cut.Find("button.epos-simerg-knopf").ClickAsync(new());
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
        var cut = Render<SpeicherOptimierungDialog>();

        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.OPT_GRP_SUCHRAUM,
                        cut.Markup, StringComparison.Ordinal);
        Assert.True(cut.Find("button.epos-simerg-knopf").HasAttribute("disabled"));
    }
}
