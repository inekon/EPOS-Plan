using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// BV-E2 (Konzept Berichtsvorlagen 10.2, „Häkchen (BV-Q1 c)") — <b>die Häkchen der Berichtsseite
/// folgen der gewählten Vorlage</b>: Ein Eintrag, dessen Kapitel weder die Word-Vorlage noch die
/// Excel-Mappe führt, steht weich gesperrt da (sichtbar, nicht wählbar, Grund am Element, ein Klick
/// nennt ihn), sein Häkchen bleibt gespeichert und geht mit dem Auftrag hinaus; mit
/// <c>{{bericht.inhalt}}</c> bleibt die Liste frei; führt die Vorlage kein Kapitel, steht bei reiner
/// Word-Ausgabe statt der Liste die leise Zeile „Den Inhalt bestimmt die Vorlage"; ein
/// Vorlagenwechsel holt den neuen Stand und bringt die Häkchen zurück.
///
/// <para>Kultur de-DE (deutsche Text-Asserts); alle Werte erfunden.</para>
/// </summary>
public class BerichtSeiteHaekchenTests : EposBunitContext
{
    public BerichtSeiteHaekchenTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // =====================================================================
    // Prüfstand
    // =====================================================================

    private const string DECKBLATT = "deckblatt";
    private const string PROJEKT = "projektbeschreibung";
    private const string WIRTSCHAFT = "wirtschaftlichkeit";
    private const string ANHANG = "anhang";

    private const string GRUND = "in dieser Vorlage nicht enthalten";
    private const string INHALT = "Den Inhalt bestimmt die Vorlage – sie führt einzelne Platzhalter, aber kein Kapitel.";

    private static readonly string[] ALLE = { DECKBLATT, PROJEKT, WIRTSCHAFT, ANHANG };

    /// <summary>Vier Bausteine: Deckblatt und Anhang nur im Word-Bericht, die übrigen auch in der Mappe.</summary>
    private static BerichtStand Stand(int ausgabe = 0) => new()
    {
        Varianten = new[]
        {
            new VarianteZeile { IdProjekt = 1030, Art = "Stamm", Bezeichner = "(Stammprojekt)",
                                Projektname = "Musterhaus", SimStand = "02.09.2026 10:00", IstStamm = true },
            new VarianteZeile { IdProjekt = 1031, Art = "Variante", Bezeichner = "Kessel groß",
                                Projektname = "Musterhaus", SimStand = "02.09.2026 10:00" }
        },
        GewaehlteVarianten = new[] { 1030, 1031 },
        Bausteine = new[]
        {
            new BausteinZeile { Schluessel = DECKBLATT, Titel = "Deckblatt" },
            new BausteinZeile { Schluessel = PROJEKT, Titel = "Projektbeschreibung", InExcel = true },
            new BausteinZeile { Schluessel = WIRTSCHAFT, Titel = "Wirtschaftlichkeit", InExcel = true },
            new BausteinZeile { Schluessel = ANHANG, Titel = "Anhang" }
        },
        AktiveBausteine = new[] { DECKBLATT, PROJEKT, ANHANG },
        AusgabeId = ausgabe,
        Zielordner = @"C:\Berichte"
    };

    private BerichtAuftrag? _auftrag;

    private IRenderedComponent<BerichtSeite> Zeige(
        Kapitelstand? kapitel, int ausgabe = 0,
        Action<ComponentParameterCollectionBuilder<BerichtSeite>>? mehr = null)
        => Render<BerichtSeite>(p =>
        {
            p.Add(x => x.Laden, () => Stand(ausgabe));
            p.Add(x => x.Erstellen, (BerichtAuftrag a, Action<Laufschritt> _) =>
            {
                _auftrag = a;
                return Task.FromResult(new LaufErgebnis { Erfolg = true, Statuszeile = "fertig" });
            });
            if (kapitel is not null) p.Add(x => x.Kapitelstand, kapitel);
            mehr?.Invoke(p);
        });

    /// <summary>Der Eintrag der Häkchenliste mit diesem Titel (das Label samt Kästchen).</summary>
    private static IElement Eintrag(IRenderedComponent<BerichtSeite> cut, string titel)
        => cut.FindAll(".epos-mehrfachauswahl-liste label.epos-schalter")
              .First(l => l.QuerySelector(".epos-feld-text")!.TextContent.Trim() == titel);

    private static IElement Kasten(IRenderedComponent<BerichtSeite> cut, string titel)
        => Eintrag(cut, titel).QuerySelector("input[type=checkbox]")!;

    private static bool Gesperrt(IRenderedComponent<BerichtSeite> cut, string titel)
        => Kasten(cut, titel).GetAttribute("aria-disabled") == "true";

    /// <summary>„Erstellen", dann „Ja" auf die heutige Startrückfrage — der Auftrag geht hinaus.</summary>
    private BerichtAuftrag Erstellen(IRenderedComponent<BerichtSeite> cut)
    {
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Erstellen").Click();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();
        Assert.NotNull(_auftrag);
        return _auftrag!;
    }

    // =====================================================================
    // Ohne Kapitelstand und mit {{bericht.inhalt}}: die Liste wie bisher
    // =====================================================================

    [Fact]
    public void Ohne_Kapitelstand_sind_alle_Haekchen_frei()
    {
        var cut = Zeige(null);

        Assert.Equal(4, cut.FindAll(".epos-mehrfachauswahl-liste input[type=checkbox]").Count);
        Assert.Empty(cut.FindAll(".epos-mehrfachauswahl-liste input[aria-disabled]"));
        Assert.Empty(cut.FindAll(".epos-bericht-inhaltszeile"));
        Assert.False(cut.Instance.InhaltBestimmtDieVorlage);
    }

    /// <summary>
    /// Die Standardvorlage und jede Vorlage mit <c>{{bericht.inhalt}}</c> führen alle Kapitel — der
    /// Kapitelstand der Hülle nennt dann keinen Baustein, und die Liste bleibt frei.
    /// </summary>
    [Fact]
    public void Mit_bericht_inhalt_bleibt_die_Liste_frei()
    {
        var cut = Zeige(new Kapitelstand(Array.Empty<string>()));

        Assert.Single(cut.FindAll(".epos-mehrfachauswahl"));
        Assert.Empty(cut.FindAll(".epos-mehrfachauswahl-liste input[aria-disabled]"));
        Assert.All(ALLE, s => Assert.False(cut.Instance.BausteinGesperrt(s)));
    }

    // =====================================================================
    // Weiche Sperre mit Grund
    // =====================================================================

    /// <summary>
    /// Ein Eintrag, dessen Kapitel die Vorlage nicht führt, bleibt SICHTBAR, trägt
    /// <c>aria-disabled</c> und seinen Grund als <c>title</c> — und ist nicht <c>disabled</c>, sonst
    /// erschiene der Grund nie (Hausregel „Bedienung"). Sein Häkchen zeigt den gespeicherten Stand.
    /// </summary>
    [Fact]
    public void Ein_Kapitel_das_die_Vorlage_nicht_fuehrt_ist_weich_gesperrt_mit_Grund()
    {
        var cut = Zeige(new Kapitelstand(new[] { WIRTSCHAFT, ANHANG }));

        Assert.Equal(4, cut.FindAll(".epos-mehrfachauswahl-liste input[type=checkbox]").Count);

        foreach (string titel in new[] { "Wirtschaftlichkeit", "Anhang" })
        {
            IElement kasten = Kasten(cut, titel);
            Assert.Equal("true", kasten.GetAttribute("aria-disabled"));
            Assert.False(kasten.HasAttribute("disabled"));
            Assert.Equal(GRUND, Eintrag(cut, titel).GetAttribute("title"));
            Assert.Contains("epos-schalter", Eintrag(cut, titel).ClassList);   // 44 px wie jeder Schalter
        }
        Assert.True(Kasten(cut, "Anhang").HasAttribute("checked"));            // gespeichert: an
        Assert.False(Kasten(cut, "Wirtschaftlichkeit").HasAttribute("checked"));

        Assert.False(Gesperrt(cut, "Deckblatt"));
        Assert.False(Gesperrt(cut, "Projektbeschreibung"));
        Assert.Null(Eintrag(cut, "Deckblatt").GetAttribute("title"));
    }

    /// <summary>
    /// Der Klick auf ein gesperrtes Häkchen schaltet nicht um — er nennt den Grund in einem Banner
    /// unter der Liste; das Häkchen bleibt gespeichert und geht mit dem Auftrag hinaus, damit ein
    /// Vorlagenwechsel es zurückbringt.
    /// </summary>
    [Fact]
    public void Der_Klick_nennt_den_Grund_und_das_Haekchen_bleibt_gespeichert()
    {
        var cut = Zeige(new Kapitelstand(new[] { ANHANG }));

        Kasten(cut, "Anhang").Click();

        IElement banner = cut.Find(".epos-warnbanner");
        Assert.Contains("„Anhang“ ist in dieser Vorlage nicht enthalten", banner.TextContent);
        Assert.Contains("Häkchen bleibt gespeichert", banner.TextContent);
        Assert.True(Kasten(cut, "Anhang").HasAttribute("checked"));

        // Die Liste steht VOR dem Banner - sie springt nicht, wenn es erscheint.
        var kinder = banner.ParentElement!.Children.ToList();
        int liste = kinder.FindIndex(k => k.ClassList.Contains("epos-mehrfachauswahl"));
        Assert.True(liste >= 0 && liste < kinder.FindIndex(k => k.ClassList.Contains("epos-warnbanner")));

        BerichtAuftrag auftrag = Erstellen(cut);
        Assert.Equal(new[] { DECKBLATT, PROJEKT, ANHANG }, auftrag.Bausteine);
    }

    /// <summary>Die freien Häkchen bleiben bedienbar, auch neben gesperrten.</summary>
    [Fact]
    public void Die_freien_Haekchen_bleiben_bedienbar()
    {
        var cut = Zeige(new Kapitelstand(new[] { ANHANG }));

        Kasten(cut, "Projektbeschreibung").Change(false);
        Kasten(cut, "Wirtschaftlichkeit").Change(true);

        Assert.Empty(cut.FindAll(".epos-warnbanner"));
        BerichtAuftrag auftrag = Erstellen(cut);
        Assert.Equal(new[] { DECKBLATT, ANHANG, WIRTSCHAFT }, auftrag.Bausteine);
    }

    // =====================================================================
    // Ohne Kapitel: „Den Inhalt bestimmt die Vorlage"
    // =====================================================================

    /// <summary>
    /// Führt die Vorlage nur Einzelplatzhalter und entsteht nur Word, steht statt der Liste eine
    /// leise Zeile; die gespeicherten Häkchen gehen unverändert mit.
    /// </summary>
    [Fact]
    public void Ohne_Kapitel_steht_statt_der_Liste_die_leise_Zeile()
    {
        var cut = Zeige(new Kapitelstand(ALLE, InhaltAusVorlage: true));

        Assert.Empty(cut.FindAll(".epos-mehrfachauswahl"));
        IElement zeile = cut.Find(".epos-bericht-inhaltszeile");
        Assert.Equal("Berichtsbausteine:", zeile.QuerySelector(".epos-untergruppe")!.TextContent.Trim());
        Assert.Equal(INHALT, zeile.QuerySelector(".epos-herleitung-text")!.TextContent.Trim());
        Assert.True(cut.Instance.InhaltBestimmtDieVorlage);
        Assert.Equal(INHALT, cut.Instance.Assistentensicht.Bausteine);

        BerichtAuftrag auftrag = Erstellen(cut);
        Assert.Equal(new[] { DECKBLATT, PROJEKT, ANHANG }, auftrag.Bausteine);
    }

    /// <summary>
    /// Mit Excel bleibt die Liste, denn die Mappe folgt den Häkchen wie heute (BV-Q2): Bei „Beide" ist
    /// nur gesperrt, was weder die Word-Vorlage noch die Mappe führt, bei „Excel" nichts. Zurück auf
    /// „Word" steht wieder die leise Zeile.
    /// </summary>
    [Fact]
    public void Mit_Excel_bleibt_die_Liste_und_gesperrt_ist_nur_was_auch_die_Mappe_nicht_fuehrt()
    {
        var cut = Zeige(new Kapitelstand(ALLE, InhaltAusVorlage: true), ausgabe: 2);

        Assert.Single(cut.FindAll(".epos-mehrfachauswahl"));
        Assert.True(Gesperrt(cut, "Deckblatt"));
        Assert.True(Gesperrt(cut, "Anhang"));
        Assert.False(Gesperrt(cut, "Projektbeschreibung"));
        Assert.False(Gesperrt(cut, "Wirtschaftlichkeit"));

        cut.FindAll(".epos-optionsgruppe input[type=radio]")[1].Change(true);   // „Excel"
        Assert.Empty(cut.FindAll(".epos-mehrfachauswahl-liste input[aria-disabled]"));

        cut.FindAll(".epos-optionsgruppe input[type=radio]")[0].Change(true);   // „Word"
        Assert.Empty(cut.FindAll(".epos-mehrfachauswahl"));
        Assert.Single(cut.FindAll(".epos-bericht-inhaltszeile"));
    }

    // =====================================================================
    // Vorlagenwechsel
    // =====================================================================

    /// <summary>
    /// Der Wechsel der Vorlage holt über <c>VorlagenNeuLaden</c> den neuen Kapitelstand: Was die neue
    /// Vorlage führt, wird frei — mit dem gespeicherten Häkchen —, was sie nicht führt, gesperrt. Der
    /// Wechsel zurück bringt den alten Stand wieder.
    /// </summary>
    [Fact]
    public void Der_Vorlagenwechsel_holt_den_neuen_Stand_und_bringt_die_Haekchen_zurueck()
    {
        var vorlagen = new[] { new Vorlagenzeile(1, "Standard (EPOS-Plan)", Mitgeliefert: true), new Vorlagenzeile(2, "Kurzbericht") };
        var kapitel = new Dictionary<int, Kapitelstand>
        {
            [1] = new(Array.Empty<string>()),
            [2] = new(new[] { DECKBLATT, ANHANG })
        };
        int gewaehlt = 1;
        int geladen = 0;

        var cut = Zeige(kapitel[1], mehr: p => p
            .Add(x => x.Vorlagen, vorlagen)
            .Add(x => x.VorlageId, 1)
            .Add(x => x.VorlageIdChanged, (int? id) => gewaehlt = id ?? 1)
            .Add(x => x.VorlagenNeuLaden, () =>
            {
                geladen++;
                return new Vorlagenstand { Vorlagen = vorlagen, VorlageId = gewaehlt, Kapitelstand = kapitel[gewaehlt] };
            }));

        Assert.All(new[] { "Deckblatt", "Projektbeschreibung", "Wirtschaftlichkeit", "Anhang" },
                   t => Assert.False(Gesperrt(cut, t)));

        cut.Find(".epos-vorlage select").Change("2");

        Assert.Equal(1, geladen);
        Assert.True(Gesperrt(cut, "Deckblatt"));
        Assert.True(Gesperrt(cut, "Anhang"));
        Assert.True(Kasten(cut, "Anhang").HasAttribute("checked"));
        Assert.False(Gesperrt(cut, "Projektbeschreibung"));
        Assert.Contains("Anhang (in dieser Vorlage nicht enthalten)", cut.Instance.Assistentensicht.Bausteine);

        cut.Find(".epos-vorlage select").Change("1");

        Assert.Equal(2, geladen);
        Assert.False(Gesperrt(cut, "Anhang"));
        Assert.True(Kasten(cut, "Anhang").HasAttribute("checked"));   // zurück, wie gespeichert
        Assert.True(Kasten(cut, "Deckblatt").HasAttribute("checked"));
    }

    /// <summary>
    /// Liefert das Nachladen keinen Kapitelstand (die neue Vorlage ist nicht lesbar), ist jeder
    /// Eintrag wieder frei; eine stehende Meldung verschwindet mit dem Wechsel.
    /// </summary>
    [Fact]
    public void Ohne_Kapitelstand_nach_dem_Wechsel_ist_jeder_Eintrag_frei()
    {
        var vorlagen = new[] { new Vorlagenzeile(1, "Kurzbericht"), new Vorlagenzeile(2, "Kaputt") };
        var cut = Zeige(new Kapitelstand(new[] { ANHANG }), mehr: p => p
            .Add(x => x.Vorlagen, vorlagen)
            .Add(x => x.VorlageId, 1)
            .Add(x => x.VorlageIdChanged, (int? _) => { })
            .Add(x => x.VorlagenNeuLaden, () => new Vorlagenstand { Vorlagen = vorlagen, VorlageId = 2 }));

        Kasten(cut, "Anhang").Click();
        Assert.Single(cut.FindAll(".epos-warnbanner"));

        cut.Find(".epos-vorlage select").Change("2");

        Assert.Empty(cut.FindAll(".epos-mehrfachauswahl-liste input[aria-disabled]"));
        Assert.Empty(cut.FindAll(".epos-warnbanner"));
    }

    // =====================================================================
    // Assistent
    // =====================================================================

    [Fact]
    public void Die_Sicht_des_Assistenten_nennt_gesperrte_Bausteine_mit_Grund()
    {
        var ohne = Zeige(null);
        Assert.Equal("Deckblatt; Projektbeschreibung; Anhang", ohne.Instance.Assistentensicht.Bausteine);

        var mit = Zeige(new Kapitelstand(new[] { ANHANG }));
        Assert.Equal("Deckblatt; Projektbeschreibung; Anhang (" + GRUND + ")", mit.Instance.Assistentensicht.Bausteine);
    }
}
