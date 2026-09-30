using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
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

    private static IElement Inhalt(IRenderedComponent<KonditionierungReiter> cut, KonditionierungGroesse g)
        => Karte(cut, g).QuerySelector(".epos-kond-inhalt") ?? throw new InvalidOperationException("Die Karte ist nicht aufgeklappt.");

    private static IElement Rasterzelle(IElement wurzel, string name)
        => wurzel.QuerySelector($".epos-wochenraster label.epos-feld[title='{name}'] input")
           ?? throw new InvalidOperationException("Keine Zelle " + name);

    private KonditionierungKalender Heizkalender => _bearbeitung.Kalender(KonditionierungGroesse.Heizen)!;

    // =================================================================================
    // Teilschritt 1: Aufklappen, Grundangabe, Wochenraster
    // =================================================================================

    [Fact]
    public void Die_Karte_klappt_ueber_Kalender_im_Einzelnen_auf_und_zu()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        IElement karte = Karte(cut, KonditionierungGroesse.Heizen);
        IElement knopf = Knopf(karte, "epos-kond-einzelheiten");
        Assert.Equal("Kalender im Einzelnen", knopf.TextContent.Trim());
        Assert.Equal("false", knopf.GetAttribute("aria-expanded"));
        Assert.Null(karte.QuerySelector(".epos-kond-inhalt"));

        knopf.Click();
        karte = Karte(cut, KonditionierungGroesse.Heizen);
        Assert.Equal("true", Knopf(karte, "epos-kond-einzelheiten").GetAttribute("aria-expanded"));
        Assert.NotNull(karte.QuerySelector(".epos-kond-inhalt"));
        Assert.Contains("epos-kond-karte--offen", karte.ClassList);

        Knopf(karte, "epos-kond-einzelheiten").Click();
        Assert.Null(Karte(cut, KonditionierungGroesse.Heizen).QuerySelector(".epos-kond-inhalt"));
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
