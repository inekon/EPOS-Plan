using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Gebäude eines Projekts (iU9-W9.2). Soll ist die Feldkarte von <c>Form_Gebaeude</c>:
/// 27 Zeilen — zwei Listen, vier Filter, der Detailblock und zehn Knöpfe.
///
/// <para>Drei Betriebsarten (Risiko R‑W9‑2): Projekt, Assistent, Verwaltung. Jede hat
/// ihren eigenen Feldbestandsfall.</para>
///
/// <para>Die Kultur ist auf de-DE gepinnt — die Erwartungswerte sind deutsche
/// Beschriftungen.</para>
/// </summary>
public class GebaeudeDialogTests : BunitContext
{
    private static readonly string[] ARTEN_WOHN = { "Einfamilienhaus", "Mehrfamilienhaus" };
    private static readonly string[] ARTEN_SONST = { "Hotel", "Kaufhaus", "Industriehalle" };
    private static readonly string[] KLASSEN =
    { "vor 1919", "1919 bis 1948", "1949 bis 1957", "1958 bis 1968", "1969 bis 1978",
      "1979 bis 1983", "1984 bis 1994", "1995 bis 2000", "Niedrigenergiebauweise",
      "Passivhaus", "EnEv 2007", "Eff. 70 (EnEV 2007)", "EnEV 2009",
      "Eff. 70 (EnEV 2009)", "Eff. 55 (EnEV 2009)", "EnEV 2014", "EnEV 2016",
      "Eff. 100 (EnEV 2016)", "Eff. 155 (EnEV 2016)", "BEG 55", "BEG 40" };

    private static readonly GebaeudeKatalogZeile[] KATALOG_WOHN =
    {
        new("Haus 1990", "Einfamilienhaus", "150,00 [m²]"),
        new("Haus 2010", "Mehrfamilienhaus", "420,00 [m²]")
    };

    private static readonly GebaeudeKatalogZeile[] KATALOG_SONST =
    {
        new("Hotel Sonne", "Hotel", "1.200,00 [m²]")
    };

    public GebaeudeDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static GebaeudeProjektZeile Zeile(int idZ, string name = "Haus 1990") => new()
    {
        IdZ = idZ,
        IdGebaeude = 7,
        Name = name,
        Art = "Einfamilienhaus",
        Beschreibung = "Ein Haus",
        Baualtersklasse = "E",
        Wohnflaeche = 150,
        Einheit = "Wohnfläche [m²]",
        Jahresnutzungsgrad = 1,
        DezentralWarmwasser = false
    };

    /// <summary>Merkt sich die letzte Filteranfrage, damit die Tests sie prüfen können.</summary>
    private sealed class Filterprotokoll
    {
        internal bool Wohngebaeude = true;
        internal string? Art;
        internal int? Klasse;
        internal bool AusBaujahrwahl;
    }

    private IRenderedComponent<GebaeudeDialog> Aufbauen(
        List<GebaeudeProjektZeile>? zeilen = null,
        bool wizard = false,
        bool admin = false,
        Filterprotokoll? protokoll = null,
        Func<string, bool>? katalogLoeschen = null,
        Func<string, IReadOnlyDictionary<string, object>>? katalogGaben = null,
        Func<GebaeudeProjektZeile, IReadOnlyDictionary<string, object>>? wohnflaecheGaben = null,
        Func<IReadOnlyDictionary<string, object>>? gebaeudetypGaben = null,
        Func<GebaeudeProjektZeile, IReadOnlyDictionary<string, object>?>? bedarfGaben = null,
        Action? geaendert = null,
        Action<bool>? geschlossen = null)
    {
        Filterprotokoll p2 = protokoll ?? new Filterprotokoll();

        return Render<GebaeudeDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<GebaeudeProjektZeile> { Zeile(1) })
            .Add(x => x.Wizard, wizard)
            .Add(x => x.Admin, admin)
            .Add(x => x.Katalog, (wohn, art, klasse, ausBaujahr) =>
            {
                p2.Wohngebaeude = wohn;
                p2.Art = art;
                p2.Klasse = klasse;
                p2.AusBaujahrwahl = ausBaujahr;
                return wohn ? KATALOG_WOHN : KATALOG_SONST;
            })
            .Add(x => x.Gebaeudearten, wohn => wohn ? ARTEN_WOHN : ARTEN_SONST)
            .Add(x => x.Baualtersklassen, KLASSEN)
            .Add(x => x.StammDetail, n => new GebaeudeStammDetail(n, "Einfamilienhaus",
                                                                  "Katalogtext", "150,00"))
            .Add(x => x.StammSatz, n => Zeile(100000, n))
            .Add(x => x.KatalogLoeschen, katalogLoeschen ?? (_ => true))
            .Add(x => x.KatalogGaben, katalogGaben)
            .Add(x => x.WohnflaecheGaben, wohnflaecheGaben)
            .Add(x => x.GebaeudetypGaben, gebaeudetypGaben)
            .Add(x => x.BedarfGaben, bedarfGaben)
            .Add(x => x.Geaendert, geaendert)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));
    }

    private static IElement Knopf(IRenderedComponent<GebaeudeDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    /// <summary>
    /// Der Übernahmeknopf — seit Befund W9‑B‑3 mit Klartext, seit dem
    /// Anwenderentscheid #76 in der Mittelspalte zwischen den beiden Listen.
    /// </summary>
    private static IElement Uebernehmen(IRenderedComponent<GebaeudeDialog> cut)
        => cut.FindAll(".epos-zweispalten-mitte button")[0];

    /// <summary>Der Entfernenknopf, ebendort.</summary>
    private static IElement Entfernen(IRenderedComponent<GebaeudeDialog> cut)
        => cut.FindAll(".epos-zweispalten-mitte button")[1];

    // =================================================================================
    // Feldbestand je Betriebsart
    // =================================================================================

    [Fact]
    public void Der_Feldbestand_der_Karte_steht()
    {
        var cut = Aufbauen();

        Assert.Contains("Eingabe der Gebäudedaten", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Eingabe der Energiedaten", cut.Markup);
        Assert.Contains("ausgewählte Gebäude im Projekt:", cut.Markup);
        Assert.Contains("Gebäude in DB:", cut.Markup);
        Assert.Contains("Filter Gebäude DB", cut.Markup);
        Assert.Contains("Gebäude: Verbrauch", cut.Markup);
        Assert.Contains("Typ/Wohnfläche", cut.Markup);

        // Zwei Klapplisten (Gebaeudeart, Baujahr), eine Optionsgruppe mit zwei Optionen,
        // ein Suchfeld, fuenf gesperrte Detailfelder.
        Assert.Equal(2, cut.FindAll("select").Count);
        Assert.Equal(2, cut.FindAll("input[type=radio]").Count);
        Assert.Equal(4, cut.FindAll("input[type=text][readonly]").Count);
        Assert.Single(cut.FindAll("textarea[readonly]"));

        foreach (string t in new[] { "Ändern", "Gebäude in DB ändern...",
                                     "Gebäude in DB neu...", "Gebäude in DB löschen",
                                     "OK", "Abbrechen" })
            Assert.NotNull(Knopf(cut, t));

        // Die zwei Richtungsknoepfe tragen seit Entscheid #76 ihr Zeichen als eigenes
        // Element neben dem Text; sie werden deshalb ueber die Mittelspalte gesucht.
        Assert.Contains("In das Projekt übernehmen", Uebernehmen(cut).TextContent);
        Assert.Contains("Aus dem Projekt entfernen", Entfernen(cut).TextContent);
    }

    [Fact]
    public void Im_Assistenten_gibt_es_keine_Schlussleiste()
    {
        var cut = Aufbauen(wizard: true);

        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "OK");
        Assert.Contains("ausgewählte Gebäude im Projekt:", cut.Markup);
    }

    [Fact]
    public void In_der_Verwaltung_fehlt_der_ganze_Projektteil()
    {
        var cut = Aufbauen(admin: true);

        Assert.DoesNotContain("ausgewählte Gebäude im Projekt:", cut.Markup);
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Contains("übernehmen"));
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Contains("entfernen"));
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Ändern");
        Assert.Contains("Gebäude in DB:", cut.Markup);
    }

    /// <summary>Ohne Delegat kein Knopf — die Hausregel für alle Sprünge.</summary>
    [Fact]
    public void Ohne_Delegat_gibt_es_keinen_Gebaeudetyp_Knopf()
    {
        var cut = Aufbauen();
        Assert.DoesNotContain("Gebäudetyp in DB ändern...", cut.Markup);

        var cut2 = Aufbauen(gebaeudetypGaben: () => new Dictionary<string, object>());
        Assert.Contains("Gebäudetyp in DB ändern...", cut2.Markup);
    }

    // =================================================================================
    // Filter
    // =================================================================================

    [Fact]
    public void Der_Umschalter_laedt_Katalog_UND_Artenliste_neu()
    {
        var protokoll = new Filterprotokoll();
        var cut = Aufbauen(protokoll: protokoll);

        Assert.Contains("Einfamilienhaus", cut.Markup);
        cut.FindAll("input[type=radio]")[1].Change(true);      // Gewerbe+Sonstige

        Assert.False(protokoll.Wohngebaeude);
        Assert.Contains("Hotel Sonne", cut.Markup);
        Assert.Contains("Industriehalle", cut.Markup);
    }

    [Fact]
    public void Die_Gebaeudeartliste_beginnt_mit_Alle()
    {
        var cut = Aufbauen();

        IElement art = cut.FindAll("select")[0];
        Assert.Equal(3, art.QuerySelectorAll("option").Length);   // Alle + zwei Arten
        Assert.Equal("Alle", art.QuerySelectorAll("option")[0].TextContent);
    }

    [Fact]
    public void Die_Baujahrliste_beginnt_mit_Alle_und_fuehrt_21_Klassen()
    {
        var cut = Aufbauen();

        IElement baujahr = cut.FindAll("select")[1];
        Assert.Equal(22, baujahr.QuerySelectorAll("option").Length);
        Assert.Equal("Alle", baujahr.QuerySelectorAll("option")[0].TextContent);
    }

    /// <summary>
    /// <b>Befund W9‑B1.</b> Der Kern braucht die Herkunft der Auswahl: Der
    /// Gebäudeart-Handler filtert ohne, der Baujahr-Handler mit der Verwendung.
    /// </summary>
    [Fact]
    public void Die_Herkunft_der_Auswahl_geht_an_den_Kern()
    {
        var protokoll = new Filterprotokoll();
        var cut = Aufbauen(protokoll: protokoll);

        cut.FindAll("select")[0].Change("1");        // Gebaeudeart
        Assert.False(protokoll.AusBaujahrwahl);
        Assert.Equal("Mehrfamilienhaus", protokoll.Art);

        cut.FindAll("select")[1].Change("4");        // Baujahr
        Assert.True(protokoll.AusBaujahrwahl);
        Assert.Equal(4, protokoll.Klasse);
    }

    [Fact]
    public void Die_Wildcardsuche_filtert_die_Anzeige()
    {
        var cut = Aufbauen();

        Assert.Contains("Haus 2010", cut.Markup);
        cut.FindAll("input[type=text]").First(i => !i.HasAttribute("readonly")).Input("*1990");
        Assert.DoesNotContain("Haus 2010", cut.Markup);
        Assert.Contains("Haus 1990", cut.Markup);
    }

    // =================================================================================
    // Uebernehmen, Entfernen, Detailblock
    // =================================================================================

    [Fact]
    public void Uebernehmen_legt_eine_Zeile_mit_den_Vorgaben_an()
    {
        bool gemeldet = false;
        var zeilen = new List<GebaeudeProjektZeile>();
        var cut = Aufbauen(zeilen: zeilen, geaendert: () => gemeldet = true);

        cut.FindAll("button.epos-anlagenwahl").Last().Click();   // eine Katalogzeile
        Uebernehmen(cut).Click();

        Assert.Single(zeilen);
        Assert.Equal("Wohnfläche [m²]", zeilen[0].Einheit);
        Assert.Equal(1, zeilen[0].Jahresnutzungsgrad);
        Assert.False(zeilen[0].DezentralWarmwasser);
        Assert.True(gemeldet);
    }

    /// <summary>
    /// „Aus dem Projekt entfernen" trifft über die <c>IdZ</c>: Zwei gleiche Gebäude im
    /// Projekt teilen sich die Stamm-Id (<c>btn_Entfernen_Click</c>:283-287).
    /// </summary>
    [Fact]
    public void Entfernen_trifft_die_Zeile_und_nicht_die_Stamm_Id()
    {
        var zeilen = new List<GebaeudeProjektZeile> { Zeile(11), Zeile(12) };
        var cut = Aufbauen(zeilen: zeilen);

        // Die zweite Projektzeile waehlen und entfernen.
        cut.FindAll("button.epos-anlagenwahl")[1].Click();
        Entfernen(cut).Click();

        Assert.Single(zeilen);
        Assert.Equal(11, zeilen[0].IdZ);
    }

    [Fact]
    public void Eine_Projektzeile_fuellt_den_Detailblock_aus_der_Zeile()
    {
        var cut = Aufbauen();

        Assert.Contains("Haus 1990", cut.Markup);
        Assert.Contains("Ein Haus", cut.Markup);
        Assert.Contains("Wohnfläche [m²]", cut.Markup);
    }

    [Fact]
    public void Eine_Katalogzeile_fuellt_den_Detailblock_aus_dem_Stamm()
    {
        var cut = Aufbauen();

        cut.FindAll("button.epos-anlagenwahl").Last().Click();

        Assert.Contains("Katalogtext", cut.Markup);
        Assert.Null(cut.Instance.Gewaehlt);
        Assert.NotNull(cut.Instance.Katalogzeile);
    }

    // =================================================================================
    // Ueberlagerungen und Rueckfrage
    // =================================================================================

    [Fact]
    public void Aendern_oeffnet_die_Wohnflaechenangabe_als_Ueberlagerung()
    {
        var cut = Aufbauen(wohnflaecheGaben: z => new Dictionary<string, object>
        {
            ["Gebaeudename"] = z.Name,
            ["Wert"] = z.Wohnflaeche,
            ["Jahresnutzungsgrad"] = z.Jahresnutzungsgrad,
            ["Einheit"] = z.Einheit
        });

        Knopf(cut, "Ändern").Click();

        Assert.True(cut.Instance.WohnflaecheOffen);
        Assert.Single(cut.FindAll("[role=dialog]"));
    }

    // =================================================================================
    // „Simulation…" — Anwenderwunsch W9-E-2 (05.09.2026)
    // =================================================================================

    /// <summary>
    /// <b>Ohne Delegat kein Knopf</b> — die Hausregel für jeden Sprung. In der
    /// Katalogverwaltung gibt es kein Projekt und damit nichts zu rechnen.
    /// </summary>
    [Fact]
    public void Ohne_Delegat_gibt_es_keinen_Simulationsknopf()
    {
        Assert.DoesNotContain("Simulation...", Aufbauen().Markup);

        var cut = Aufbauen(bedarfGaben: _ => new Dictionary<string, object>());
        Assert.Contains("Simulation...", cut.Markup);
    }

    /// <summary>Der Knopf hängt an der MARKIERUNG der Projektliste, wie „Ändern".</summary>
    [Fact]
    public void Ohne_markiertes_Gebaeude_ist_der_Simulationsknopf_gesperrt()
    {
        var cut = Aufbauen(bedarfGaben: _ => new Dictionary<string, object>());

        // Der Sprung in den Katalog nimmt die Projektmarkierung weg
        // (KatalogzeileWaehlen setzt _gewaehlt auf null).
        cut.FindAll("button.epos-anlagenwahl").Last().Click();

        Assert.Null(cut.Instance.Gewaehlt);
        Assert.True(Knopf(cut, "Simulation...").HasAttribute("disabled"));
    }

    [Fact]
    public void Mit_markiertem_Gebaeude_ist_der_Simulationsknopf_frei()
    {
        var cut = Aufbauen(bedarfGaben: _ => new Dictionary<string, object>());

        Assert.NotNull(cut.Instance.Gewaehlt);
        Assert.False(Knopf(cut, "Simulation...").HasAttribute("disabled"));
    }

    /// <summary>
    /// Der Knopf öffnet die Überlagerung und gibt dem Kern GENAU DIE markierte Zeile
    /// mit — ihre <c>IdZ</c> ist der Schlüssel, nicht die Stamm-Id.
    /// </summary>
    [Fact]
    public void Simulation_oeffnet_den_Bedarf_als_Ueberlagerung()
    {
        int gefragt = 0;
        var cut = Aufbauen(zeilen: new List<GebaeudeProjektZeile> { Zeile(4711) },
                           bedarfGaben: z =>
                           {
                               gefragt = z.IdZ;
                               return new Dictionary<string, object>
                               {
                                   ["Daten"] = new GebaeudeBedarfDaten { Name = z.Name }
                               };
                           });

        Knopf(cut, "Simulation...").Click();

        Assert.Equal(4711, gefragt);
        Assert.True(cut.Instance.BedarfOffen);
        Assert.Single(cut.FindAll("[role=dialog]"));
    }

    /// <summary>
    /// Kommt keine Zahl heraus — die Zeile ist noch nicht gespeichert oder das Projekt
    /// führt keine Klimaregion —, MELDET der Dialog das, statt eine leere Überlagerung
    /// aufzumachen.
    /// </summary>
    [Fact]
    public void Ohne_Ergebnis_meldet_der_Dialog_statt_eine_leere_Flaeche_zu_zeigen()
    {
        var cut = Aufbauen(bedarfGaben: _ => null);

        Knopf(cut, "Simulation...").Click();

        Assert.False(cut.Instance.BedarfOffen);
        Assert.Contains("kein Wärmebedarf", cut.Instance.Meldung);
    }

    /// <summary>Esc schließt den Wirt nicht, solange der Bedarf steht.</summary>
    [Fact]
    public void Esc_schliesst_NICHT_wenn_der_Bedarf_offen_ist()
    {
        bool gerufen = false;
        var cut = Aufbauen(bedarfGaben: _ => new Dictionary<string, object>(),
                           geschlossen: _ => gerufen = true);

        Knopf(cut, "Simulation...").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.False(gerufen);
    }

    /// <summary>In der Katalogverwaltung gibt es den Knopf auch MIT Delegat nicht.</summary>
    [Fact]
    public void In_der_Verwaltung_fehlt_der_Simulationsknopf()
    {
        var cut = Aufbauen(admin: true, bedarfGaben: _ => new Dictionary<string, object>());

        Assert.DoesNotContain("Simulation...", cut.Markup);
    }

    [Fact]
    public void Der_Katalogeditor_meldet_ohne_markierten_Satz()
    {
        var cut = Aufbauen(katalogGaben: _ => new Dictionary<string, object>());

        Knopf(cut, "Gebäude in DB ändern...").Click();

        Assert.False(cut.Instance.KatalogeditorOffen);
        Assert.Contains("Gebäude in DB auswählen!", cut.Instance.Meldung);
    }

    [Fact]
    public void Gebaeude_in_DB_neu_oeffnet_den_Katalogeditor_ohne_Markierung()
    {
        string uebergeben = "x";
        var cut = Aufbauen(katalogGaben: name =>
        {
            uebergeben = name;
            return new Dictionary<string, object>();
        });

        Knopf(cut, "Gebäude in DB neu...").Click();

        Assert.True(cut.Instance.KatalogeditorOffen);
        Assert.Equal("", uebergeben);
    }

    [Fact]
    public void Loeschen_fragt_nach_und_meldet_danach()
    {
        string geloescht = "";
        var cut = Aufbauen(katalogLoeschen: n => { geloescht = n; return true; });

        cut.FindAll("button.epos-anlagenwahl").Last().Click();
        Knopf(cut, "Gebäude in DB löschen").Click();

        Assert.Contains("wirklich gelöscht", cut.Markup);
        Knopf(cut, "Ja").Click();

        Assert.Equal("Haus 2010", geloescht);
        Assert.Contains("Gebäude gelöscht!", cut.Instance.Meldung);
    }

    [Fact]
    public void Loeschen_mit_Nein_laesst_alles_stehen()
    {
        bool gerufen = false;
        var cut = Aufbauen(katalogLoeschen: _ => { gerufen = true; return true; });

        cut.FindAll("button.epos-anlagenwahl").Last().Click();
        Knopf(cut, "Gebäude in DB löschen").Click();
        Knopf(cut, "Nein").Click();

        Assert.False(gerufen);
    }

    // =================================================================================
    // Tastatur und Schlussleiste
    // =================================================================================

    [Fact]
    public void Esc_schliesst_mit_Abbruch()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.False(ergebnis);
    }

    [Fact]
    public void Esc_schliesst_NICHT_wenn_eine_Ueberlagerung_offen_ist()
    {
        bool gerufen = false;
        var cut = Aufbauen(wohnflaecheGaben: _ => new Dictionary<string, object>(),
                           geschlossen: _ => gerufen = true);

        Knopf(cut, "Ändern").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.False(gerufen);
    }

    [Fact]
    public void OK_meldet_true()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        Knopf(cut, "OK").Click();

        Assert.True(ergebnis);
    }

    // =================================================================================
    // Die zwei Richtungsknoepfe - Windows-Abnahme 05.09.2026, Befund W9-B-3
    // =================================================================================

    /// <summary>
    /// <b>Befund W9‑B‑3:</b> „nicht so recht klar, auf was sich die oberen 2 Buttons
    /// beziehen."
    ///
    /// <para>Der Vorläufer trug hier die blanken Zeichen „◀" und „▶". Beide Knöpfe
    /// tragen ihre Aufgabe seither im Klartext.</para>
    ///
    /// <para><b>Anwenderentscheid #76</b> vom selben Tag hat das Anordnungsschema
    /// nachgezogen: Die Listen stehen wieder nebeneinander und brechen erst auf
    /// schmalem Schirm untereinander um. Das Zeichen steht deshalb nicht mehr IM
    /// Text — es hängt an der Anordnung, und beide Zeichen stehen im Markup, damit
    /// das Stilblatt je Breite eines zeigen kann.</para>
    /// </summary>
    [Fact]
    public void Die_zwei_Richtungsknoepfe_sagen_was_sie_tun()
    {
        var cut = Aufbauen();

        IElement hinzu = Uebernehmen(cut);
        IElement weg = Entfernen(cut);

        Assert.Equal("In das Projekt übernehmen",
                     hinzu.QuerySelector(".epos-zweispalten-knopftext")!.TextContent);
        Assert.Equal("Aus dem Projekt entfernen",
                     weg.QuerySelector(".epos-zweispalten-knopftext")!.TextContent);

        // Nebeneinander wandert die Zeile nach links ins Projekt und nach rechts
        // heraus; untereinander nach oben und nach unten.
        Assert.Equal("◀", hinzu.QuerySelector(".epos-zweispalten-pfeil--breit")!.TextContent);
        Assert.Equal("▲", hinzu.QuerySelector(".epos-zweispalten-pfeil--schmal")!.TextContent);
        Assert.Equal("▶", weg.QuerySelector(".epos-zweispalten-pfeil--breit")!.TextContent);
        Assert.Equal("▼", weg.QuerySelector(".epos-zweispalten-pfeil--schmal")!.TextContent);

        // Das Zeichen ist Beiwerk: Eine Sprachausgabe liest den Satz, nicht das Dreieck.
        foreach (IElement p in hinzu.QuerySelectorAll(".epos-zweispalten-pfeil"))
            Assert.Equal("true", p.GetAttribute("aria-hidden"));
    }

    /// <summary>Beschriftung UND Kurztext — der Kurztext nennt die Herkunft der Zeile.</summary>
    [Fact]
    public void Die_zwei_Richtungsknoepfe_tragen_einen_Kurztext()
    {
        var cut = Aufbauen();

        Assert.Contains("Gebäude in DB", Uebernehmen(cut).GetAttribute("title") ?? "");
        Assert.Contains("Projektliste", Entfernen(cut).GetAttribute("title") ?? "");
    }

    // =================================================================================
    // Die Markierung der Projektliste - Windows-Abnahme 05.09.2026, Befund W9-B-1
    // =================================================================================

    /// <summary>
    /// <b>Befund W9‑B‑1:</b> „Im Projekt gespeichertes Gebäude wird nicht angezeigt
    /// bzw. in der Liste selektiert."
    ///
    /// <para>Der Wirt baut seine Anzeigeliste bei JEDEM <c>Gaben</c>-Aufruf neu aus
    /// der Fachliste auf (<c>GebaeudeHuelle.Gaben</c> :113‑114) — die Zeilenobjekte
    /// sind danach andere. Eine Markierung über die Objektgleichheit war damit beim
    /// ersten Neuzeichnen des Wirtes weg: Das gespeicherte Gebäude stand in der Liste,
    /// aber unmarkiert. Verglichen wird deshalb über die <c>IdZ</c>.</para>
    /// </summary>
    [Fact]
    public void Die_Markierung_ueberlebt_einen_Austausch_der_Zeilenliste()
    {
        var cut = Aufbauen(zeilen: new List<GebaeudeProjektZeile> { Zeile(11, "Haus A"),
                                                                    Zeile(12, "Haus B") });

        cut.FindAll("button.epos-anlagenwahl")[1].Click();
        Assert.Equal(12, cut.Instance.Gewaehlt?.IdZ);

        // Derselbe Bestand, NEUE Objekte - genau das, was die Huelle liefert.
        cut.Render(p => p.Add(x => x.Zeilen, new List<GebaeudeProjektZeile>
        {
            Zeile(11, "Haus A"), Zeile(12, "Haus B")
        }));

        Assert.Equal(12, cut.Instance.Gewaehlt?.IdZ);
        Assert.Equal("Haus B",
                     cut.Find(".epos-zeile--markiert td:last-child").TextContent.Trim());
    }

    /// <summary>
    /// Die zweite Hälfte desselben Befundes: Kommt die Projektliste erst NACH dem
    /// ersten Zeichnen (der Ladeweg des Assistenten läuft beim Verlassen der
    /// Projektkopfseite), stand bis hierher für immer keine Markierung.
    /// </summary>
    [Fact]
    public void Eine_spaeter_gefuellte_Projektliste_wird_markiert()
    {
        var cut = Aufbauen(zeilen: new List<GebaeudeProjektZeile>());

        Assert.Null(cut.Instance.Gewaehlt);
        Assert.Empty(cut.FindAll(".epos-zeile--markiert"));

        cut.Render(p => p.Add(x => x.Zeilen,
                              new List<GebaeudeProjektZeile> { Zeile(11, "Musterhaus") }));

        Assert.Equal(11, cut.Instance.Gewaehlt?.IdZ);
        Assert.Contains("Musterhaus", cut.Find(".epos-zeile--markiert").TextContent);
    }

    /// <summary>
    /// Steht der Anwender im KATALOG, bleibt seine Wahl stehen — das Nachziehen
    /// überschreibt sie nicht.
    /// </summary>
    [Fact]
    public void Eine_Katalogwahl_wird_vom_Nachziehen_nicht_ueberschrieben()
    {
        var cut = Aufbauen(zeilen: new List<GebaeudeProjektZeile> { Zeile(11, "Haus A") });

        // Die erste Katalogzeile waehlen - das nimmt die Projektmarkierung weg.
        cut.FindAll("button.epos-anlagenwahl")[1].Click();
        Assert.Null(cut.Instance.Gewaehlt);
        Assert.NotNull(cut.Instance.Katalogzeile);

        cut.Render(p => p.Add(x => x.Zeilen,
                              new List<GebaeudeProjektZeile> { Zeile(11, "Haus A") }));

        Assert.Null(cut.Instance.Gewaehlt);
        Assert.NotNull(cut.Instance.Katalogzeile);
    }

    // =================================================================================
    // Die Anordnung - Anwenderentscheid #76 vom 05.09.2026
    // =================================================================================

    /// <summary>
    /// Der Anwender hat nach der Windows-Abnahme entschieden, dass auch dieser Dialog
    /// dem BHKW-PLAN-Schema folgt: Projektliste LINKS, Katalog RECHTS, die zwei
    /// Pfeilknöpfe in einer schmalen Mittelspalte dazwischen. Bis dahin standen die
    /// beiden Listen untereinander und die Knöpfe unter der Projektliste.
    /// </summary>
    [Fact]
    public void Projektliste_links_Katalog_rechts_Pfeile_dazwischen()
    {
        var cut = Aufbauen();

        var bereiche = cut.FindAll(".epos-zweispalten > div")
                          .Select(e => e.ClassName ?? "").ToList();

        Assert.Equal(3, bereiche.Count);
        Assert.Contains("epos-zweispalten-spalte--links", bereiche[0]);
        Assert.Contains("epos-zweispalten-mitte", bereiche[1]);
        Assert.Contains("epos-zweispalten-spalte--rechts", bereiche[2]);

        // Beide Listen stehen weiterhin in ihrem Rahmen (Befund W9-B-2).
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-spalte .epos-raster-huelle").Count);
    }

}
