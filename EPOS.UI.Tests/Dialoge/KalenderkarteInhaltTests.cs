using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Kalenderkarte im Einzelnen</b> (Stufe KP2, Welle U3; Teilkonzept Konditionierungsprofile 3.2,
/// 3.5, 7.5; Entwurf KP2 Festlegungen 7, 8, 11, 14, 15) — der aufgeklappte Inhalt einer Karte über dem
/// echten Weg der Hülle OHNE Datenbank (<c>KonditionierungHuelle.ReinerWeg</c>): Grundangabe und
/// Wochenraster, Zeitfenster, Periodenliste, Werkzeuge, Teppichbild.
/// </summary>
/// <remarks>
/// <para>Jede Handlung geht über den Weg in den Arbeitsstand; geschrieben wird erst mit dem OK des
/// Editors, „Zurücknehmen" nimmt je Handlung eine Stufe zurück.</para>
/// <para>Die Kultur ist auf de-DE gepinnt (deutsche Rückfalltexte).</para>
/// </remarks>
public class KalenderkarteInhaltTests : EposBunitContext
{
    public KalenderkarteInhaltTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private KonditionierungBearbeitung _bearbeitung = default!;
    private GebaeudeArbeitsstand _arbeit = default!;
    private readonly List<string> _meldungen = new();

    private IRenderedComponent<KonditionierungReiter> Aufbauen(KonditionierungWeg? weg = null, GebaeudeKatalogDaten? satz = null,
                                                               bool lesemodus = false, int entprellungMs = 0)
    {
        _arbeit = new GebaeudeArbeitsstand();
        _arbeit.Laden(satz ?? KalenderkarteTests.Satz(), neu: false);
        KonditionierungWeg w = weg ?? KalenderkarteTests.Weg(null);
        _bearbeitung = new KonditionierungBearbeitung(_arbeit, () => w) { Melden = (m, _) => _meldungen.Add(m) };
        return Render<KonditionierungReiter>(p => p
            .Add(x => x.Bearbeitung, _bearbeitung)
            .Add(x => x.Lesemodus, lesemodus)
            .Add(x => x.EntprellungMs, entprellungMs));
    }

    private static IElement Karte(IRenderedComponent<KonditionierungReiter> cut, KonditionierungGroesse g)
        => KalenderkarteTests.Karte(cut, g);

    private static IElement Knopf(IElement wurzel, string klasse)
        => wurzel.QuerySelector("button." + klasse) ?? throw new InvalidOperationException("Kein Knopf ." + klasse);

    /// <summary>Legt den Kalender der Größe an und klappt ihre Karte auf.</summary>
    private void AnlegenUndAufklappen(IRenderedComponent<KonditionierungReiter> cut, KonditionierungGroesse g)
    {
        Knopf(Karte(cut, g), "epos-kond-anlegen").Click();
        Knopf(Karte(cut, g), "epos-kond-einzelheiten").Click();
    }

    /// <summary>Der Abschnitt der Einzelheiten einer Größe — ein Geschwister der Karte, nicht ihr Kind.</summary>
    private static IElement? Einzelheiten(IRenderedComponent<KonditionierungReiter> cut, KonditionierungGroesse g)
        => cut.FindAll($"section.epos-kond-karte-einzelheiten[data-groesse='{(int)g}']").SingleOrDefault();

    private static IElement Inhalt(IRenderedComponent<KonditionierungReiter> cut, KonditionierungGroesse g)
        => Einzelheiten(cut, g)?.QuerySelector(".epos-kond-inhalt") ?? throw new InvalidOperationException("Die Karte ist nicht aufgeklappt.");

    private static IElement Rasterzelle(IElement wurzel, string name)
        => wurzel.QuerySelector($".epos-wochenraster label.epos-feld[title='{name}'] input")
           ?? throw new InvalidOperationException("Keine Zelle " + name);

    private KonditionierungKalender Heizkalender => _bearbeitung.Kalender(KonditionierungGroesse.Heizen)!;

    // =================================================================================
    // Teilschritt 1: Aufklappen, Grundangabe, Wochenraster
    // =================================================================================

    /// <summary>
    /// Anwendermeldung 08.10.2026 („bei Button anklicken wird Bereich entfernt"): Die aufgeklappte Karte nahm die
    /// ganze Rasterzeile und sprang aus ihrer Reihe. Jetzt bleibt die Karte samt Kopf, Vorlagenwahl, Knöpfen und
    /// Vorschau stehen; die Einzelheiten sind ein eigener Abschnitt neben ihr, der Knopf sagt, was er tut.
    /// </summary>
    [Fact]
    public void Kalender_bearbeiten_zeigt_die_Einzelheiten_neben_der_stehenden_Karte_und_klappt_sie_wieder_ein()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        IElement karte = Karte(cut, KonditionierungGroesse.Heizen);
        IElement knopf = Knopf(karte, "epos-kond-einzelheiten");
        Assert.Equal("Kalender bearbeiten…", knopf.TextContent.Trim());
        Assert.Contains("Wochenraster", knopf.GetAttribute("title"));
        Assert.Contains("Perioden", knopf.GetAttribute("title"));
        Assert.Contains("Jahresvorschau", knopf.GetAttribute("title"));
        Assert.Equal("false", knopf.GetAttribute("aria-expanded"));
        Assert.Null(Einzelheiten(cut, KonditionierungGroesse.Heizen));

        knopf.Click();
        karte = Karte(cut, KonditionierungGroesse.Heizen);
        knopf = Knopf(karte, "epos-kond-einzelheiten");
        Assert.Equal("Einzelheiten einklappen", knopf.TextContent.Trim());
        Assert.Equal("true", knopf.GetAttribute("aria-expanded"));
        Assert.Contains("epos-kond-karte--offen", karte.ClassList);
        // Die Karte behält ihre kompakten Bedienelemente und trägt die Einzelheiten nicht in sich.
        Assert.NotNull(karte.QuerySelector(".epos-kond-karte-zustand"));
        Assert.NotNull(karte.QuerySelector("button.epos-kond-anlegen"));
        Assert.Null(karte.QuerySelector(".epos-kond-inhalt"));
        IElement einzelheiten = Einzelheiten(cut, KonditionierungGroesse.Heizen)!;
        Assert.NotNull(einzelheiten.QuerySelector(".epos-kond-inhalt"));
        Assert.Equal(einzelheiten.Id, knopf.GetAttribute("aria-controls"));
        Assert.Equal("Kalender Heizen im Einzelnen", einzelheiten.QuerySelector("h3")!.TextContent.Trim());
        Assert.Contains("epos-kond--aktiv", einzelheiten.ClassList);
        Assert.Same(karte.ParentElement, einzelheiten.ParentElement);

        // Der Knopf im Abschnitt klappt ebenso ein.
        Knopf(einzelheiten, "epos-kond-einzelheiten-zu").Click();
        Assert.Null(Einzelheiten(cut, KonditionierungGroesse.Heizen));
        Assert.Equal("Kalender bearbeiten…", Knopf(Karte(cut, KonditionierungGroesse.Heizen), "epos-kond-einzelheiten").TextContent.Trim());
    }

    [Fact]
    public void Ohne_angelegten_Kalender_nennt_der_Inhalt_den_Grund_und_bietet_nichts_an()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        Knopf(Karte(cut, KonditionierungGroesse.Heizen), "epos-kond-einzelheiten").Click();
        IElement inhalt = Inhalt(cut, KonditionierungGroesse.Heizen);
        Assert.Contains("Erst „Kalender anlegen“", inhalt.QuerySelector(".epos-kond-inhalt-grund")!.TextContent);
        Assert.Null(inhalt.QuerySelector(".epos-wochenraster"));
        Assert.Null(inhalt.QuerySelector(".epos-kond-grundangabe"));
    }

    [Fact]
    public void Im_Lesemodus_und_ohne_Delegaten_steht_kein_Knopf_Kalender_im_Einzelnen()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen(lesemodus: true);
        Assert.Empty(cut.FindAll("button.epos-kond-einzelheiten"));

        KonditionierungWeg ohne = KalenderkarteTests.Weg(null);
        var gekuerzt = new KonditionierungWeg { ZelleSetzen = ohne.ZelleSetzen, Anlegen = ohne.Anlegen };
        cut = Aufbauen(gekuerzt);
        Assert.Empty(cut.FindAll("button.epos-kond-einzelheiten"));
    }

    [Fact]
    public void Am_angelegten_Kalender_zeigt_das_Wochenraster_die_Standardwoche_und_eine_Zelle_wirkt_je_Handlung()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        AnlegenUndAufklappen(cut, KonditionierungGroesse.Heizen);
        Assert.Equal(KonditionierungAngabe.Woche, Heizkalender.Angabe);
        IElement inhalt = Inhalt(cut, KonditionierungGroesse.Heizen);
        Assert.NotNull(inhalt.QuerySelector(".epos-kond-grundangabe-woche"));
        Assert.Contains("epos-wochenraster--umbrechend", inhalt.QuerySelector(".epos-wochenraster")!.ClassList);

        // Die Zellen heißen nach der Größe — die fünf Karten tragen verschiedene Feldnamen.
        IElement zelle = Rasterzelle(inhalt, "Heizen · Mo 07 Uhr");
        Assert.Equal("20", zelle.GetAttribute("value"));
        zelle.Input("22");
        Assert.Equal(22.0, Heizkalender.Woche![7]);

        Rasterzelle(Inhalt(cut, KonditionierungGroesse.Heizen), "Heizen · Di 03 Uhr").Input("aus");
        Assert.True(double.IsNaN(Heizkalender.Woche![24 + 3]));

        // „Zurücknehmen" nimmt eine Stufe: die zweite Zelle, nicht die erste.
        cut.Find("button.epos-kond-zuruecknehmen").Click();
        Assert.Equal(17.0, Heizkalender.Woche![24 + 3]);
        Assert.Equal(22.0, Heizkalender.Woche![7]);
    }

    [Fact]
    public void Standardwoche_verwerfen_fuehrt_zur_Grundangabe_mit_dem_haeufigsten_Wert_und_aus_wirkt_dort()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        AnlegenUndAufklappen(cut, KonditionierungGroesse.Heizen);
        Inhalt(cut, KonditionierungGroesse.Heizen).QuerySelectorAll(".epos-wochenraster-knoepfe button")
            .Single(b => b.TextContent.Trim() == "Standardwoche verwerfen").Click();

        Assert.Equal(KonditionierungAngabe.Wert, Heizkalender.Angabe);
        Assert.Equal(20.0, Heizkalender.Wert);
        IElement inhalt = Inhalt(cut, KonditionierungGroesse.Heizen);
        IElement feld = inhalt.QuerySelector(".epos-kond-grundangabe input")!;
        Assert.Equal("20", feld.GetAttribute("value"));
        // Ohne Woche zeigt das Raster die Grundangabe gesperrt, mit dem Knopf, der daraus eine Woche macht.
        Assert.Contains("Noch keine Standardwoche", inhalt.QuerySelector(".epos-wochenraster-hinweis")!.TextContent);

        feld.Input("aus");
        Assert.Equal(KonditionierungAngabe.Aus, Heizkalender.Angabe);

        Inhalt(cut, KonditionierungGroesse.Heizen).QuerySelectorAll(".epos-wochenraster-knoepfe button")
            .Single(b => b.TextContent.Trim() == "Standardwoche anlegen").Click();
        Assert.Equal(KonditionierungAngabe.Woche, Heizkalender.Angabe);
        Assert.All(Heizkalender.Woche!, v => Assert.True(double.IsNaN(v)));
    }

    // =================================================================================
    // Teilschritt 2: das Zeitfenster
    // =================================================================================

    private static IElement Fensterfeld(IElement inhalt, string beschriftung)
        => inhalt.QuerySelectorAll(".epos-kond-zeitfenster label.epos-feld")
                 .Single(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == beschriftung)
                 .QuerySelector("input")!;

    [Fact]
    public void Das_Zeitfenster_Mo_bis_Fr_6_bis_8_Uhr_22_Grad_ersetzt_nur_seine_Stunden_und_vermerkt_es()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        AnlegenUndAufklappen(cut, KonditionierungGroesse.Heizen);
        double[] vor = (double[])Heizkalender.Woche!.Clone();
        IElement inhalt = Inhalt(cut, KonditionierungGroesse.Heizen);

        // Vorgabe Mo–Fr; die Tagesknöpfe melden ihren Zustand über aria-pressed.
        IElement[] tage = inhalt.QuerySelectorAll("button.epos-kond-tag").ToArray();
        Assert.Equal(7, tage.Length);
        Assert.Equal(new[] { "true", "true", "true", "true", "true", "false", "false" },
                     tage.Select(t => t.GetAttribute("aria-pressed")).ToArray());

        Fensterfeld(inhalt, "Von").Input("6");
        Fensterfeld(Inhalt(cut, KonditionierungGroesse.Heizen), "Bis").Input("8");
        Fensterfeld(Inhalt(cut, KonditionierungGroesse.Heizen), "Wert").Input("22");
        Knopf(Inhalt(cut, KonditionierungGroesse.Heizen), "epos-kond-zeitfenster-eintragen").Click();

        double[] nach = Heizkalender.Woche!;
        for (int t = 0; t < 7; t++)
            for (int h = 0; h < 24; h++)
            {
                int i = t * 24 + h;
                if (t < 5 && h is 6 or 7) Assert.Equal(22.0, nach[i]);
                else Assert.Equal(vor[i], nach[i]);
            }
        Assert.False(string.IsNullOrEmpty(Heizkalender.Vermerk));
        Assert.Contains("Zuletzt angewandt:", Inhalt(cut, KonditionierungGroesse.Heizen).QuerySelector(".epos-kond-vermerk")!.TextContent);

        // Ein Schritt für „Zurücknehmen".
        cut.Find("button.epos-kond-zuruecknehmen").Click();
        Assert.Equal(vor, Heizkalender.Woche!);
    }

    [Fact]
    public void Das_Zeitfenster_ist_weich_gesperrt_bis_Tage_Zeiten_und_Wert_stehen_und_nennt_den_Grund()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        AnlegenUndAufklappen(cut, KonditionierungGroesse.Heizen);
        IElement knopf = Knopf(Inhalt(cut, KonditionierungGroesse.Heizen), "epos-kond-zeitfenster-eintragen");
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Null(knopf.GetAttribute("disabled"));
        knopf.Click();
        Assert.Contains("„Von“ und „Bis“", _meldungen.Last());

        // Alle Tage abgewählt: der Grund nennt die Tage.
        for (int t = 0; t < 5; t++)
            Inhalt(cut, KonditionierungGroesse.Heizen).QuerySelector($"button.epos-kond-tag[data-tag='{t}']")!.Click();
        Assert.Contains("Tag", Knopf(Inhalt(cut, KonditionierungGroesse.Heizen), "epos-kond-zeitfenster-eintragen").GetAttribute("title"));
        double[] vor = (double[])Heizkalender.Woche!.Clone();
        Knopf(Inhalt(cut, KonditionierungGroesse.Heizen), "epos-kond-zeitfenster-eintragen").Click();
        Assert.Equal(vor, Heizkalender.Woche!);
    }

    [Fact]
    public void Das_Zeitfenster_setzt_aus_ueber_Mitternacht_am_Wochenende()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        AnlegenUndAufklappen(cut, KonditionierungGroesse.Heizen);
        IElement inhalt = Inhalt(cut, KonditionierungGroesse.Heizen);
        // Mo–Fr ab, Sa und So an.
        for (int t = 0; t < 7; t++)
            Inhalt(cut, KonditionierungGroesse.Heizen).QuerySelector($"button.epos-kond-tag[data-tag='{t}']")!.Click();
        Fensterfeld(Inhalt(cut, KonditionierungGroesse.Heizen), "Von").Input("22");
        Fensterfeld(Inhalt(cut, KonditionierungGroesse.Heizen), "Bis").Input("6");
        Fensterfeld(Inhalt(cut, KonditionierungGroesse.Heizen), "Wert").Input("aus");
        Knopf(Inhalt(cut, KonditionierungGroesse.Heizen), "epos-kond-zeitfenster-eintragen").Click();

        double[] w = Heizkalender.Woche!;
        Assert.True(double.IsNaN(w[5 * 24 + 23]));   // Sa 23 Uhr
        Assert.True(double.IsNaN(w[6 * 24 + 2]));    // So 2 Uhr
        Assert.False(double.IsNaN(w[4 * 24 + 23]));  // Fr 23 Uhr bleibt
    }

    // =================================================================================
    // Teilschritt 4: Feiertage und Zeitstruktur
    // =================================================================================

    [Fact]
    public void Feiertage_als_Regel_legt_die_neun_Regeln_wie_Sonntag_an_einmal_und_mit_Vermerk()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        AnlegenUndAufklappen(cut, KonditionierungGroesse.Heizen);
        Knopf(Inhalt(cut, KonditionierungGroesse.Heizen), "epos-kond-feiertage-anlegen").Click();

        List<KonditionierungPeriode> regeln = Heizkalender.Perioden.Where(p => p.Art == KonditionierungPeriodenart.Feiertag).ToList();
        Assert.Equal(9, regeln.Count);
        Assert.All(regeln, p => Assert.Equal((KonditionierungAngabe.WieWochentag, (int?)7), (p.Angabe, p.WieWochentag)));
        Assert.All(regeln, p => Assert.InRange(p.Rang, 100, 108));
        Assert.False(string.IsNullOrEmpty(Heizkalender.Vermerk));
        // In der Periodenliste: eigene Zeilen, ▲▼ weich gesperrt (Feiertagsband).
        IElement zeile = Inhalt(cut, KonditionierungGroesse.Heizen).QuerySelector("tr[data-rang='100']")!;
        Assert.Equal("true", zeile.QuerySelector("button.epos-kond-rang-hoeher")!.GetAttribute("aria-disabled"));

        // Wiederholbar: keine Regel doppelt.
        Knopf(Inhalt(cut, KonditionierungGroesse.Heizen), "epos-kond-feiertage-anlegen").Click();
        Assert.Equal(9, Heizkalender.Perioden.Count(p => p.Art == KonditionierungPeriodenart.Feiertag));
    }

    [Fact]
    public void Zeitstruktur_wie_Heizung_setzt_die_Geraete_in_den_Heizstunden_auf_den_Tagwert_sonst_auf_den_Nachtwert()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        Assert.True(_bearbeitung.WertSetzen(KonditionierungGroesse.Geraete, KonditionierungZeile.Nacht, 10));
        AnlegenUndAufklappen(cut, KonditionierungGroesse.Geraete);
        // Heizen trägt die Zeitstruktur nicht selbst — dort gibt es das Werkzeug nicht.
        Knopf(Karte(cut, KonditionierungGroesse.Heizen), "epos-kond-einzelheiten").Click();
        Assert.Null(Inhalt(cut, KonditionierungGroesse.Heizen).QuerySelector("button.epos-kond-wie-heizung"));

        Knopf(Inhalt(cut, KonditionierungGroesse.Geraete), "epos-kond-wie-heizung").Click();
        KonditionierungKalender k = _bearbeitung.Kalender(KonditionierungGroesse.Geraete)!;
        Assert.Equal(KonditionierungAngabe.Woche, k.Angabe);
        // Heizen: Tag 20 °C von 6 bis 22 Uhr, sonst 17 °C — die Heizstunden bekommen 100 %, die übrigen 10 %.
        Assert.Equal(100.0, k.Woche![0 * 24 + 7]);
        Assert.Equal(10.0, k.Woche![0 * 24 + 23]);
        Assert.Equal(100.0, k.Woche![6 * 24 + 12]);
        Assert.Contains("Zuletzt angewandt:", Inhalt(cut, KonditionierungGroesse.Geraete).QuerySelector(".epos-kond-vermerk")!.TextContent);
    }

    [Fact]
    public void Zeitstruktur_wie_Anwesenheit_ohne_Personenkalender_meldet_den_Grund_und_aendert_nichts()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        AnlegenUndAufklappen(cut, KonditionierungGroesse.Lueftung);
        KonditionierungKalender vor = _bearbeitung.Kalender(KonditionierungGroesse.Lueftung)!.Kopie();
        Knopf(Inhalt(cut, KonditionierungGroesse.Lueftung), "epos-kond-wie-anwesenheit").Click();
        Assert.Single(_meldungen);
        Assert.Equal(vor.Angabe, _bearbeitung.Kalender(KonditionierungGroesse.Lueftung)!.Angabe);
        Assert.Null(_bearbeitung.Kalender(KonditionierungGroesse.Lueftung)!.Vermerk);
    }

    // =================================================================================
    // Teilschritt 5: das Teppichbild
    // =================================================================================

    [Fact]
    public void Das_Teppichbild_zeigt_den_Kalender_wie_er_gilt_mit_Bezugsjahr_und_Wert_samt_Quelle_am_Element()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        Knopf(Karte(cut, KonditionierungGroesse.Heizen), "epos-kond-einzelheiten").Click();
        IElement teppich = Inhalt(cut, KonditionierungGroesse.Heizen).QuerySelector(".epos-kond-teppich")!;
        // Auch vor dem Anlegen: der Kalender aus der Matrix.
        IElement[] felder = teppich.QuerySelectorAll("[data-wert]").ToArray();
        Assert.NotEmpty(felder);
        Assert.True(felder.Length <= 2000);
        Assert.Contains(felder, f => f.GetAttribute("data-wert")!.Contains("20 °C") && f.GetAttribute("data-wert")!.Contains("·"));
        // Im Katalog das Rückfallraster ohne Jahr (E115): Bildunterschrift und Titel nennen das Raster, kein Jahr.
        Assert.Contains("Gemeinjahr, 1. Januar = Sonntag", teppich.QuerySelector(".epos-kond-teppich-zeile")!.TextContent);
        Assert.DoesNotContain("Bezugsjahr", teppich.TextContent);
        Assert.Contains("1. Januar = So", teppich.TextContent);

        // Nach dem Anlegen und einem Zeitfenster mit „aus": das Bild zeigt die eigene Fläche „aus".
        Knopf(Karte(cut, KonditionierungGroesse.Heizen), "epos-kond-anlegen").Click();
        Fensterfeld(Inhalt(cut, KonditionierungGroesse.Heizen), "Von").Input("0");
        Fensterfeld(Inhalt(cut, KonditionierungGroesse.Heizen), "Bis").Input("6");
        Fensterfeld(Inhalt(cut, KonditionierungGroesse.Heizen), "Wert").Input("aus");
        Knopf(Inhalt(cut, KonditionierungGroesse.Heizen), "epos-kond-zeitfenster-eintragen").Click();
        Assert.NotEmpty(Inhalt(cut, KonditionierungGroesse.Heizen).QuerySelectorAll(".epos-kond-teppich [data-reihe='aus'], .epos-kond-teppich [data-rolle='aus'], .epos-kond-teppich [data-wert*='aus']"));
    }

    [Fact]
    public void Ohne_Kalender_der_Groesse_nennt_das_Teppichbild_seinen_Leerzustand()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        Knopf(Karte(cut, KonditionierungGroesse.Personen), "epos-kond-einzelheiten").Click();
        IElement teppich = Inhalt(cut, KonditionierungGroesse.Personen).QuerySelector(".epos-kond-teppich")!;
        Assert.Contains("Kein Jahresbild", teppich.QuerySelector(".epos-kond-teppich-leer")!.TextContent);
        Assert.Empty(teppich.QuerySelectorAll("svg"));
    }

    [Fact]
    public void Das_Teppichbild_rechnet_beim_Oeffnen_sofort_und_nach_Eingaben_entprellt_einmal()
    {
        int bilder = 0;
        KonditionierungWeg basis = KalenderkarteTests.Weg(null);
        var weg = new KonditionierungWeg
        {
            ZelleSetzen = basis.ZelleSetzen, Anlegen = basis.Anlegen, Standardwoche = basis.Standardwoche,
            Grundangabe = basis.Grundangabe, Bezugsjahr = basis.Bezugsjahr,
            Teppichbild = (s, o) =>
            {
                if (o.Groesse == KonditionierungGroesse.Heizen) bilder++;
                return basis.Teppichbild!(s, o);
            }
        };
        // Befund WT-b: Mit 150 ms Ruhezeit feuerte unter Last eine Entprellung ZWISCHEN zwei Eingaben (der Prüfstand
        // braucht dann mehr als 150 ms je Eingabe), und der Fall war rot, ohne dass sich an der Karte etwas änderte. Die
        // Ruhezeit steht deshalb während der Eingaben auf einer Minute, die kein Prüfstand überschreitet, und erst vor der
        // letzten Eingabe auf 1 ms (Muster VorpruefungEntprelltTests).
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen(weg, entprellungMs: 60_000);
        AnlegenUndAufklappen(cut, KonditionierungGroesse.Heizen);
        Assert.Equal(1, bilder);                          // beim Öffnen sofort

        foreach (string eingabe in new[] { "21", "22" })
        {
            Rasterzelle(Inhalt(cut, KonditionierungGroesse.Heizen), "Heizen · Mo 07 Uhr").Input(eingabe);
            cut.Render();
        }
        Assert.Equal(1, bilder);                          // in der Ruhezeit kein Bild

        cut.Render(p => p.Add(x => x.EntprellungMs, 1));
        Assert.Equal(1, bilder);                          // derselbe Stand rechnet nicht neu
        Rasterzelle(Inhalt(cut, KonditionierungGroesse.Heizen), "Heizen · Mo 07 Uhr").Input("23");
        cut.Render();

        // Auf den gezeichneten Zustand warten, nicht auf die Uhr.
        cut.WaitForAssertion(() => Assert.Equal(2, bilder), TimeSpan.FromSeconds(5));
        Assert.Equal(23.0, Heizkalender.Woche![7]);
        // Die überholten Entprellungen sind abgebrochen — kämen sie noch, stünde der Zähler hier höher.
        cut.Render();
        Assert.Equal(2, bilder);                          // und kein zweites
    }

    /// <summary>
    /// E115: Die Bildunterschrift nennt im Regelfall das Raster des Wegs („Gemeinjahr, 1. Januar = Donnerstag") und kein
    /// Jahr, im Sonderfall mit Preisreihenjahr das Bezugsjahr.
    /// </summary>
    [Theory]
    [InlineData(null, "Gemeinjahr, 1. Januar = Donnerstag", "Bezugsjahr")]
    [InlineData(2027, "Bezugsjahr 2027", "Gemeinjahr")]
    public void Die_Bildunterschrift_nennt_das_Raster_und_nur_mit_Preisreihe_das_Jahr(int? jahr, string erwartet, string fremd)
    {
        KonditionierungWeg basis = KalenderkarteTests.Weg(null);
        var weg = new KonditionierungWeg
        {
            ZelleSetzen = basis.ZelleSetzen, Anlegen = basis.Anlegen, Standardwoche = basis.Standardwoche,
            Grundangabe = basis.Grundangabe, Teppichbild = basis.Teppichbild,
            Bezugsjahr = jahr, WochentagJan1 = 3,
        };
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen(weg);
        Knopf(Karte(cut, KonditionierungGroesse.Heizen), "epos-kond-einzelheiten").Click();
        string zeile = Inhalt(cut, KonditionierungGroesse.Heizen).QuerySelector(".epos-kond-teppich-zeile")!.TextContent;
        Assert.Contains(erwartet, zeile);
        Assert.DoesNotContain(fremd, zeile);
        if (jahr is null) Assert.DoesNotContain("20", zeile.Split(':')[0]);
    }

    /// <summary>E115: Die Datumsanzeige trägt im Regelfall den Wochentag des Rasters, mit Preisreihe das volle Datum.</summary>
    [Fact]
    public void Die_Datumsanzeige_folgt_dem_Raster()
    {
        Assert.Equal("Do 15.01.", Kalendertage.Anzeige(15, 3, null));     // Raster Donnerstag: der 15. Januar ist ein Donnerstag
        Assert.Equal("Do 01.01.", Kalendertage.Anzeige(1, 3, null));
        Assert.Equal("15.01.2027", Kalendertage.Anzeige(15, 3, 2027));
        Assert.Equal("", Kalendertage.Anzeige(0, 3, null));
        Assert.Equal(2009, Kalendertage.Darstellungsjahr(3, null));       // erstes Gemeinjahr ab 2001 mit 1. Januar Donnerstag
        Assert.Equal(2027, Kalendertage.Darstellungsjahr(3, 2027));
        Assert.Equal("Donnerstag", Kalendertage.Wochentagname(3));
    }

    // =================================================================================
    // Teilschritt 6: „In den Kalender übernehmen" an der Wärmeübergabe (Teilkonzept 5.5)
    // =================================================================================

    /// <summary>Ein Katalogbau mit Heizkreis und einem Sollwert-Zeitprogramm: werktags 7–17 Uhr 21 °C, sonst 16 °C.</summary>
    private static GebaeudeKatalogDaten MitZeitprogramm()
    {
        GebaeudeKatalogDaten d = KalenderkarteTests.Satz();
        d.HeizkreisAktiv = true;
        d.UebergabeArt = DbWerte.UEBERGABE_RADIATOR;
        var w = new double[168];
        for (int t = 0; t < 7; t++)
            for (int h = 0; h < 24; h++)
                w[t * 24 + h] = t < 5 && h >= 7 && h < 17 ? 21 : 16;
        d.Sollwertprofil = AnlagenkopplungSchema.WochenprofilSchreiben(w);
        return d;
    }

    private IRenderedComponent<GebaeudeKatalogDialog> Dialog(GebaeudeKatalogDaten d)
        => Render<GebaeudeKatalogDialog>(p => p
            .Add(x => x.Daten, d)
            .Add(x => x.Modus, GebaeudeKatalogModus.Bearbeiten)
            .Add(x => x.Gebaeudetypen, () => new[] { "Einfamilienhaus" })
            .Add(x => x.Gebaeudearten, () => new[] { "Hotel" })
            .Add(x => x.Baualtersklassen, new[] { "bis 1859" })
            .Add(x => x.Speichern, (_, _, _) => new GebaeudeKatalogErgebnis(true, ""))
            .Add(x => x.Konditionierung, KalenderkarteTests.Weg(null))
            .Add(x => x.EntprellungMs, 0));

    [Fact]
    public void In_den_Kalender_uebernehmen_legt_den_Heizkalender_mit_der_Woche_des_Zeitprogramms_an_und_zeigt_ihn()
    {
        IRenderedComponent<GebaeudeKatalogDialog> cut = Dialog(MitZeitprogramm());
        IElement knopf = cut.Find("div.gebk-waermeuebergabe button.epos-uebergabe-in-den-kalender");
        Assert.Equal("In den Kalender übernehmen", knopf.TextContent.Trim());
        knopf.Click();

        KonditionierungBearbeitung b = cut.Instance.Konditionierungsbearbeitung;
        KonditionierungKalender k = b.Kalender(KonditionierungGroesse.Heizen)!;
        Assert.Equal(KonditionierungZustand.Angelegt, k.Zustand);
        Assert.Equal(21.0, k.Woche![7]);
        Assert.Equal(16.0, k.Woche![5 * 24 + 10]);
        Assert.False(string.IsNullOrEmpty(k.Vermerk));
        // Das Zeitprogramm bleibt stehen (AK1); der Dialog steht im Reiter „Konditionierung", Heizen aufgeklappt.
        Assert.NotNull(cut.Instance.Arbeitsstand.Sollwertprofil);
        Assert.Equal("KONDITIONIERUNG", cut.Instance.AktiverReiter);
        Assert.NotNull(cut.Find("section.epos-kond-karte-einzelheiten[data-groesse='0'] .epos-kond-inhalt"));

        // „Zurücknehmen" nimmt die Übernahme als EINEN Schritt zurück.
        cut.Find("button.epos-kond-zuruecknehmen").Click();
        Assert.False(b.Angelegt(KonditionierungGroesse.Heizen));
    }

    [Fact]
    public void Vor_einem_angelegten_Heizkalender_fragt_In_den_Kalender_uebernehmen_mit_Vorgabe_Nein()
    {
        IRenderedComponent<GebaeudeKatalogDialog> cut = Dialog(MitZeitprogramm());
        KonditionierungBearbeitung b = cut.Instance.Konditionierungsbearbeitung;
        Assert.True(b.Anlegen(KonditionierungGroesse.Heizen));
        double vor = b.Kalender(KonditionierungGroesse.Heizen)!.Woche![7];

        cut.Find("div.gebk-waermeuebergabe button.epos-uebergabe-in-den-kalender").Click();
        Assert.NotNull(b.OffeneFrage);
        Assert.True(b.OffeneFrage!.VorgabeNein);
        Assert.Contains("ersetzt die Standardwoche", b.OffeneFrage.Text);
        Assert.Equal(vor, b.Kalender(KonditionierungGroesse.Heizen)!.Woche![7]);

        b.Beantworten(true);
        Assert.Equal(21.0, b.Kalender(KonditionierungGroesse.Heizen)!.Woche![7]);
    }

    [Fact]
    public void Ohne_Zeitprogramm_oder_ohne_Weg_steht_kein_Knopf_In_den_Kalender_uebernehmen()
    {
        GebaeudeKatalogDaten d = MitZeitprogramm();
        d.Sollwertprofil = null;
        Assert.Empty(Dialog(d).FindAll("button.epos-uebergabe-in-den-kalender"));

        IRenderedComponent<GebaeudeKatalogDialog> ohneWeg = Render<GebaeudeKatalogDialog>(p => p
            .Add(x => x.Daten, MitZeitprogramm())
            .Add(x => x.Modus, GebaeudeKatalogModus.Bearbeiten)
            .Add(x => x.Speichern, (_, _, _) => new GebaeudeKatalogErgebnis(true, "")));
        Assert.Empty(ohneWeg.FindAll("button.epos-uebergabe-in-den-kalender"));
    }

    // =================================================================================
    // Teilschritt 7: offene Punkte aus U2
    // =================================================================================

    [Fact]
    public void Ein_Klick_auf_die_Zeile_Vorlage_bringt_die_Groesse_nach_vorn_und_fokussiert_die_Auswahlliste_ihrer_Karte()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen(KalenderkarteTests.Weg(Konditionierungsvorlagenablage.AusSaat()));
        Assert.Equal(KonditionierungGroesse.Heizen, cut.Instance.AktiveGroesse);
        int vor = JSInterop.Invocations.Count(i => i.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));

        IElement zelle = cut.Find("tr[data-zeile='vorlage'] button.epos-kond-herkunft-knopf[data-groesse='3']");
        Assert.Equal("Öffnet die Auswahlliste der Vorlagen dieser Größe in ihrer Karte.", zelle.GetAttribute("title"));
        zelle.Click();

        Assert.Equal(KonditionierungGroesse.Geraete, cut.Instance.AktiveGroesse);
        Assert.Contains("epos-kond--aktiv", Karte(cut, KonditionierungGroesse.Geraete).ClassList);
        cut.WaitForAssertion(() => Assert.Equal(vor + 1,
            JSInterop.Invocations.Count(i => i.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase))));
    }

    [Fact]
    public void Ohne_Vorlagen_im_Weg_und_im_Lesemodus_bleibt_die_Zeile_Vorlage_eine_Anzeige()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        Assert.Empty(cut.FindAll("button.epos-kond-herkunft-knopf"));
        cut = Aufbauen(KalenderkarteTests.Weg(Konditionierungsvorlagenablage.AusSaat()), lesemodus: true);
        Assert.Empty(cut.FindAll("button.epos-kond-herkunft-knopf"));
    }

    [Fact]
    public void Geraete_und_Personen_ohne_Anteile_zeigen_einen_benannten_Leerzustand_statt_Kein_Diagramm()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        foreach (KonditionierungGroesse g in new[] { KonditionierungGroesse.Geraete, KonditionierungGroesse.Personen })
        {
            IElement vorschau = Karte(cut, g).QuerySelector(".epos-kond-vorschau")!;
            Assert.DoesNotContain("Kein Diagramm vorhanden", vorschau.TextContent);
            string grund = vorschau.QuerySelector(".epos-kond-vorschau-leer")!.TextContent;
            Assert.Contains("trägt keinen Anteil", grund);
            Assert.Contains(_bearbeitung.Groessenname(g), grund);
        }
        // Heizen hat seine Woche.
        Assert.Null(Karte(cut, KonditionierungGroesse.Heizen).QuerySelector(".epos-kond-vorschau-leer"));

        // Mit einem Anteil ergibt sich die Woche.
        Assert.True(_bearbeitung.WertSetzen(KonditionierungGroesse.Geraete, KonditionierungZeile.Nacht, 20));
        cut.Render();
        Assert.Null(Karte(cut, KonditionierungGroesse.Geraete).QuerySelector(".epos-kond-vorschau-leer"));
    }

    // =================================================================================
    // Teilschritt 8: der Assistent — die Woche als Text
    // =================================================================================

    [Fact]
    public void Der_Assistent_liest_und_setzt_die_Woche_als_Text_und_die_Grundangabe_als_einen_Wert()
    {
        Aufbauen();
        var tafel = new KonditionierungKiTafel(KiKonditionierungsfelder.Finde);
        Assert.Null(tafel.Lesen(_bearbeitung, "kond_heizen_woche"));        // nicht angelegt

        // Nicht angelegt: benannt abgelehnt, statt still einen Kalender anzulegen.
        Assert.Throws<InvalidOperationException>(() => tafel.Setzen(_bearbeitung, "kond_heizen_woche", "20"));

        Assert.True(_bearbeitung.Anlegen(KonditionierungGroesse.Heizen));
        string text = (string)tafel.Lesen(_bearbeitung, "kond_heizen_woche")!;
        string[] werte = text.Split(';');
        Assert.Equal(168, werte.Length);
        Assert.Equal("20", werte[7]);
        Assert.Equal("17", werte[23]);

        werte[7] = "aus";
        werte[8] = "21,5";
        tafel.Setzen(_bearbeitung, "kond_heizen_woche", string.Join(";", werte));
        Assert.True(double.IsNaN(Heizkalender.Woche![7]));
        Assert.Equal(21.5, Heizkalender.Woche![8]);

        tafel.Setzen(_bearbeitung, "kond_heizen_woche", "19");
        Assert.Equal((KonditionierungAngabe.Wert, (double?)19), (Heizkalender.Angabe, Heizkalender.Wert));
        Assert.Equal("19", tafel.Lesen(_bearbeitung, "kond_heizen_woche"));

        InvalidOperationException f = Assert.Throws<InvalidOperationException>(
            () => tafel.Setzen(_bearbeitung, "kond_heizen_woche", "20;21;22"));
        Assert.Contains("168", f.Message);
        Assert.Equal(19.0, Heizkalender.Wert);
    }

    [Fact]
    public void Eine_Grundangabe_ohne_Rundlauf_meldet_den_Kern_und_aendert_nichts()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        AnlegenUndAufklappen(cut, KonditionierungGroesse.Heizen);
        Inhalt(cut, KonditionierungGroesse.Heizen).QuerySelectorAll(".epos-wochenraster-knoepfe button")
            .Single(b => b.TextContent.Trim() == "Standardwoche verwerfen").Click();
        Assert.Empty(_meldungen);

        Inhalt(cut, KonditionierungGroesse.Heizen).QuerySelector(".epos-kond-grundangabe input")!.Input("20,123456");
        Assert.Single(_meldungen);
        Assert.Equal(20.0, Heizkalender.Wert);
    }
}
