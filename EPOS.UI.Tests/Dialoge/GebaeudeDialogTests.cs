using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using EPOS.UI.Standards;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Gebäude eines Projekts (iU9-W9.2). Soll ist die Feldkarte von <c>Form_Gebaeude</c>:
/// zwei Listen, der Detailblock und zehn Knöpfe. Der Katalog ist seit Stufe G3, Welle K die
/// Katalogliste des Hauses — dasselbe Profil und derselbe Filterstand wie die
/// Gebäudeverwaltung; die vier Vorfilter sind Suche und Trichter.
///
/// <para>Zwei Betriebsarten (Risiko R‑W9‑2): Projekt und Assistent. Die Verwaltung ist seit
/// Stufe 5 der Neuordnung der Administrationsdialoge eine eigene Komponente —
/// <c>GebaeudeAdminDialogTests</c>.</para>
///
/// <para>Die Kultur ist auf de-DE gepinnt — die Erwartungswerte sind deutsche
/// Beschriftungen.</para>
/// </summary>
public class GebaeudeDialogTests : EposBunitContext
{
    /// <summary>Das Profil, wie die Hülle es reicht — mit den Texten aus <c>MyResource</c>.</summary>
    private static Katalogfilterprofil Profil()
        => Katalogfilterprofil.FuerGebaeude(s => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(s) ?? s);

    /// <summary>Eine Katalogzeile mit den fünf Spalten von <c>FuerGebaeude</c>.</summary>
    private static Katalogfilterzeile Katalogsatz(int id, string name, string art, string verwendung,
                                                  string baujahr, double flaeche)
        => new Katalogfilterzeile(id, name)
            .MitText(Katalogfilterprofil.SpBezeichner, name)
            .MitText(Katalogfilterprofil.SpGebaeudeart, art)
            .MitText(Katalogfilterprofil.SpVerwendung, verwendung)
            .MitText(Katalogfilterprofil.SpBaualtersklasse, baujahr)
            .MitZahl(Katalogfilterprofil.SpFlaecheM2, flaeche, 0);

    /// <summary>Drei Sätze in der Reihenfolge des Controllers (<c>ORDER BY Bezeichner</c>).</summary>
    private static IReadOnlyList<Katalogfilterzeile> Katalog() => new[]
    {
        Katalogsatz(1, "Haus 1990", "Einfamilienhaus", "Wohngebäude", "1984 bis 1994", 150),
        Katalogsatz(2, "Haus 2010", "Mehrfamilienhaus", "Wohngebäude", "2010 bis 2015", 420),
        Katalogsatz(3, "Hotel Sonne", "Hotel", "Gewerbe+Sonstige", "1969 bis 1978", 1200)
    };

    public GebaeudeDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static GebaeudeProjektZeile Zeile(int idZ, string name = "Haus 1990") => new()
    {
        IdZ = idZ,
        IdGebaeude = 7,
        IdKatalog = 42,
        Name = name,
        Art = "Einfamilienhaus",
        Beschreibung = "Ein Haus",
        Baualtersklasse = "E",
        Wohnflaeche = 150,
        Einheit = "Wohnfläche [m²]",
        Jahresnutzungsgrad = 1,
        DezentralWarmwasser = false
    };

    private IRenderedComponent<GebaeudeDialog> Aufbauen(
        List<GebaeudeProjektZeile>? zeilen = null,
        bool wizard = false,
        Katalogfilterstand? filterstand = null,
        Func<string, bool>? katalogLoeschen = null,
        Func<string, IReadOnlyDictionary<string, object>>? katalogGaben = null,
        Func<string, string>? katalogLoeschsperre = null,
        Func<GebaeudeProjektZeile, IReadOnlyDictionary<string, object>>? wohnflaecheGaben = null,
        Func<IReadOnlyDictionary<string, object>>? gebaeudetypGaben = null,
        Func<GebaeudeProjektZeile, IReadOnlyDictionary<string, object>?>? bedarfGaben = null,
        Action? geaendert = null,
        Action<bool>? geschlossen = null)
    {
        return Render<GebaeudeDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<GebaeudeProjektZeile> { Zeile(1) })
            .Add(x => x.Wizard, wizard)
            .Add(x => x.Katalogzeilen, () => Katalog())
            .Add(x => x.Katalogprofil, Profil())
            .Add(x => x.Filterstandvorgabe, filterstand ?? new Katalogfilterstand())
            .Add(x => x.StammDetail, n => new GebaeudeStammDetail(n, "Einfamilienhaus",
                                                                  "Katalogtext", "150,00"))
            .Add(x => x.StammSatz, n => Zeile(100000, n))
            .Add(x => x.KatalogLoeschen, katalogLoeschen ?? (_ => true))
            .Add(x => x.KatalogLoeschsperre, katalogLoeschsperre)
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
        => cut.FindAll(".epos-zweispalten-uebernahme button")[0];

    /// <summary>Der Entfernenknopf, ebendort.</summary>
    private static IElement Entfernen(IRenderedComponent<GebaeudeDialog> cut)
        => cut.FindAll(".epos-zweispalten-uebernahme button")[1];

    /// <summary>Die gezeichneten Zeilen der Katalogliste.</summary>
    private static IReadOnlyList<IElement> Katalogzeilen(IRenderedComponent<GebaeudeDialog> cut)
        => cut.FindAll(".epos-katalogliste tbody tr");

    /// <summary>Wählt einen Katalogsatz über den Wahlknopf seiner Zeile — wie die Hand.</summary>
    private static void KatalogWaehlen(IRenderedComponent<GebaeudeDialog> cut, string name)
        => Katalogzeilen(cut).First(tr => tr.TextContent.Contains(name, StringComparison.Ordinal))
                             .QuerySelector("button.epos-anlagenwahl")!.Click();

    /// <summary>Die Namen der gezeichneten Katalogzeilen.</summary>
    private static string[] Katalognamen(IRenderedComponent<GebaeudeDialog> cut)
        => Katalogzeilen(cut).Select(tr => tr.QuerySelectorAll("td")[1].TextContent.Trim()).ToArray();

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
        Assert.Contains("Gebäude: Verbrauch", cut.Markup);

        // Welle K: KEINE Vorfilter mehr ueber dem Katalog - weder Klapplisten noch
        // Optionsgruppe; die Katalogliste traegt das eine Suchfeld. Dazu fuenf gesperrte
        // Detailfelder und seit Stufe G1 zwei leise Kennzahlen (H_ges, Rechenweg).
        Assert.DoesNotContain("Filter Gebäude DB", cut.Markup);
        Assert.Empty(cut.FindAll("select"));
        Assert.Empty(cut.FindAll("input[type=radio]"));
        Assert.Single(cut.FindAll(".epos-katalogliste"));
        Assert.Single(cut.FindAll(".epos-katalogliste input[type=search]"));
        Assert.Equal(6, cut.FindAll("input[type=text][readonly]").Count);
        Assert.Single(cut.FindAll("textarea[readonly]"));

        foreach (string t in new[] { "Fläche und Verbrauch…", "Gebäude in DB ändern...",
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
    // Der Katalog - die Katalogliste des Hauses (Stufe G3, Welle K)
    // =================================================================================

    /// <summary>
    /// <b>Der Katalog ist die Katalogliste der Gebäudeverwaltung</b> — dieselben fünf Spalten
    /// (Profil <c>FuerGebaeude</c>), jede mit Trichter und Sortierpfeil, alle Sätze des
    /// Katalogs ohne Vorauswahl. Als PROJEKTdialog behält die Liste die Wahlspalte und das
    /// Zeilenmaß 53 px (Hausregel: nur in den Verwaltungen ist die Zeile die Wahl), und einen
    /// Vergleichsknopf bietet sie nicht an.
    /// </summary>
    [Fact]
    public void Der_Katalog_ist_die_Katalogliste_der_Verwaltung_mit_Wahlspalte()
    {
        var cut = Aufbauen();

        var koepfe = cut.FindAll(".epos-katalogliste thead .epos-spaltenkopf-text")
                        .Select(e => e.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "Name", "Gebäudeart", "Verwendung", "Baualtersklasse", "Fläche [m²]" }, koepfe);
        Assert.Equal(5, cut.FindAll(".epos-katalogliste thead .epos-trichter").Count);

        Assert.Equal(new[] { "Haus 1990", "Haus 2010", "Hotel Sonne" }, Katalognamen(cut));
        Assert.Equal(3, cut.FindAll(".epos-katalogliste tbody button.epos-anlagenwahl").Count);
        Assert.Equal("Wahl", cut.Find(".epos-katalogliste thead th.epos-spalte-wahl").TextContent.Trim());

        Katalogliste liste = cut.FindComponent<Katalogliste>().Instance;
        Assert.Equal(53f, liste.Zeilenhoehe);
        Assert.Equal(Raster<Katalogfilterzeile>.ZEILENHOEHE, liste.Zeilenhoehe);
        Assert.False(liste.ZeileIstWahl);
        Assert.Empty(cut.FindAll(".epos-katalog-vergleichknopf"));
        Assert.Contains("3 von 3", cut.Find(".epos-katalog-treffer").TextContent);
    }

    /// <summary>
    /// <b>Die Suche geht über alle Spalten</b> (der frühere Textfilter mit Platzhaltern) —
    /// sie steht im Filterstand, und die Liste zeigt nur die Treffer.
    /// </summary>
    [Fact]
    public void Die_Suche_ueber_alle_Spalten_filtert_die_Liste()
    {
        var stand = new Katalogfilterstand();
        var cut = Aufbauen(filterstand: stand);

        cut.Find(".epos-katalog-suchzeile input").Input("*1990");

        cut.WaitForAssertion(() => Assert.Equal(new[] { "Haus 1990" }, Katalognamen(cut)));
        Assert.Equal("*1990", stand.Suche);
    }

    /// <summary>
    /// <b>Die Verwendung ist ein Trichter</b> (bis Welle K die Optionsgruppe
    /// Wohngebäude/Gewerbe+Sonstige): „Gewerbe" im Trichter der Spalte lässt nur die
    /// Gewerbegebäude stehen — ohne einen Weg über die Datenbank.
    /// </summary>
    [Fact]
    public void Der_Trichter_Verwendung_filtert_die_Liste()
    {
        var stand = new Katalogfilterstand();
        var cut = Aufbauen(filterstand: stand);

        cut.FindAll(".epos-katalogliste thead .epos-trichter")[2].Click();     // Verwendung
        cut.WaitForElement(".epos-spaltenfilter input").Change("Gewerbe");

        cut.WaitForAssertion(() => Assert.Equal(new[] { "Hotel Sonne" }, Katalognamen(cut)));
        Assert.Equal("Gewerbe", stand.Ausdruck(Katalogfilterprofil.SpVerwendung));
    }

    /// <summary>
    /// <b>Gebäudeart und Baujahr sind Trichter</b> (bis Welle K zwei Klapplisten mit
    /// „Alle"); sie wirken UND-verknüpft. Die frühere Weiche, deren Ergebnis davon abhing,
    /// welche Klappliste zuletzt angefasst wurde (Befund W9‑B1), gibt es nicht mehr.
    /// </summary>
    [Fact]
    public void Gebaeudeart_und_Baujahr_filtern_als_Trichter()
    {
        var stand = new Katalogfilterstand();
        stand.Setzen(Katalogfilterprofil.SpVerwendung, "Wohngebäude");
        var cut = Aufbauen(filterstand: stand);
        Assert.Equal(new[] { "Haus 1990", "Haus 2010" }, Katalognamen(cut));

        stand.Setzen(Katalogfilterprofil.SpGebaeudeart, "Mehrfamilienhaus");
        cut.Render();
        cut.WaitForAssertion(() => Assert.Equal(new[] { "Haus 2010" }, Katalognamen(cut)));

        stand.Setzen(Katalogfilterprofil.SpGebaeudeart, "");
        stand.Setzen(Katalogfilterprofil.SpBaualtersklasse, "1984 bis 1994");
        cut.Render();
        cut.WaitForAssertion(() => Assert.Equal(new[] { "Haus 1990" }, Katalognamen(cut)));
    }

    /// <summary>
    /// <b>Derselbe Filterstand wie in der Verwaltung</b>: Ohne eigene Vorgabe holt der
    /// Dialog seinen Stand aus dem Register — unter dem Schlüssel der Gebäudeverwaltung.
    /// Wer dort filtert, findet den Filter hier wieder (je Katalog einer, für die Sitzung).
    /// </summary>
    [Fact]
    public void Ohne_Vorgabe_teilt_der_Dialog_den_Filterstand_der_Verwaltung()
    {
        var cut = Render<GebaeudeDialog>(p => p
            .Add(x => x.Zeilen, new List<GebaeudeProjektZeile>())
            .Add(x => x.Katalogzeilen, () => Katalog())
            .Add(x => x.Katalogprofil, Profil()));

        Assert.Same(Katalogfilterregister.Stand(Katalogfilterprofil.SCHLUESSEL_GEBAEUDE),
                    cut.Instance.Filterstand);
    }

    /// <summary>
    /// <b>Ohne Gaben zeichnet der Dialog</b> — die Katalogliste steht mit dem Profil
    /// <c>FuerGebaeude</c> da, leer und mit „Kein Treffer.", statt zu fehlen.
    /// </summary>
    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog_samt_leerer_Katalogliste()
    {
        var cut = Render<GebaeudeDialog>(p => p
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand()));

        Assert.Single(cut.FindAll(".epos-katalogliste"));
        Assert.Equal(5, cut.FindAll(".epos-katalogliste thead .epos-spaltenkopf-text").Count);
        Assert.Single(cut.FindAll(".epos-katalog-leer"));
        Assert.Null(cut.Instance.Katalogzeile);
    }

    /// <summary>
    /// <b>Die Wahl hängt am Bezeichner</b> (Hausregel der Katalogliste): Blendet die Suche
    /// den gewählten Satz aus, bleibt er gewählt, und „In das Projekt übernehmen" nimmt ihn.
    /// </summary>
    [Fact]
    public void Die_Katalogwahl_bleibt_wenn_der_Filter_sie_ausblendet()
    {
        var zeilen = new List<GebaeudeProjektZeile>();
        var cut = Aufbauen(zeilen: zeilen);

        KatalogWaehlen(cut, "Haus 2010");
        cut.Find(".epos-katalog-suchzeile input").Input("Hotel");
        cut.WaitForAssertion(() => Assert.Equal(new[] { "Hotel Sonne" }, Katalognamen(cut)));

        Assert.Equal("Haus 2010", cut.Instance.Katalogzeile?.Bezeichner);
        Uebernehmen(cut).Click();

        Assert.Equal("Haus 2010", Assert.Single(zeilen).Name);
    }

    /// <summary>
    /// Entfernt der Anwender die letzte Projektzeile, steht die erste SICHTBARE Katalogzeile
    /// im Detailblock — die erste der gefilterten Liste, nicht die erste des Katalogs.
    /// </summary>
    [Fact]
    public void Nach_dem_Entfernen_der_letzten_Zeile_steht_die_erste_sichtbare_Katalogzeile()
    {
        var stand = new Katalogfilterstand { Suche = "Hotel" };
        var zeilen = new List<GebaeudeProjektZeile> { Zeile(11) };
        var cut = Aufbauen(zeilen: zeilen, filterstand: stand);

        Entfernen(cut).Click();

        Assert.Empty(zeilen);
        Assert.Equal("Hotel Sonne", cut.Instance.Katalogzeile?.Bezeichner);
    }

    /// <summary>
    /// Stufe G6a (Anwenderentscheid A2): Ein Gebäude mit Zonen geht erst nach der Rückfrage aus dem
    /// Projekt — sie nennt die Zahl der Zonen und der Bauteile; „Nein" lässt alles stehen. Ohne Zonen
    /// entfernt der Knopf wie bisher sofort.
    /// </summary>
    [Fact]
    public void Aus_dem_Projekt_entfernen_fragt_bei_einem_Gebaeude_mit_Zonen()
    {
        GebaeudeProjektZeile mitZonen = Zeile(11, "Haus mit Zonen");
        mitZonen.Zonenzahl = 2;
        mitZonen.Bauteilzahl = 7;
        var zeilen = new List<GebaeudeProjektZeile> { mitZonen, Zeile(12) };
        var cut = Aufbauen(zeilen: zeilen);

        Entfernen(cut).Click();
        Assert.True(cut.Instance.AusProjektFrageOffen);
        Assert.Equal(2, zeilen.Count);
        Assert.Equal("„Haus mit Zonen“ trägt 2 Zonen mit 7 Bauteilen. Mit dem Speichern der Liste werden sie samt dem " +
                     "Gebäude aus dem Projekt gelöscht. Trotzdem aus dem Projekt entfernen?", cut.Instance.AusProjektFrage);
        cut.FindAll(".epos-rueckfrage button").First(b => b.TextContent.Trim() == "Nein").Click();
        Assert.False(cut.Instance.AusProjektFrageOffen);
        Assert.Equal(2, zeilen.Count);

        Entfernen(cut).Click();
        cut.FindAll(".epos-rueckfrage button").First(b => b.TextContent.Trim() == "Ja").Click();
        Assert.Equal(12, Assert.Single(zeilen).IdZ);

        // Ohne Zonen keine Rückfrage.
        Entfernen(cut).Click();
        Assert.False(cut.Instance.AusProjektFrageOffen);
        Assert.Empty(zeilen);
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
    /// <b>Der Katalogverweis reist mit der Zeile</b> (Konzept Administrationsdialoge
    /// 7.1 (a)): Die übernommene Zeile ist genau die, die der Kern liefert — samt
    /// <c>IdKatalog</c> —, und eine Änderung der Wohnflächenangabe oder das Entfernen einer
    /// anderen Zeile lässt ihn stehen. Assistent und Startseite schreiben die Liste danach
    /// über diesen Verweis neu, nicht über den Namen.
    /// </summary>
    [Fact]
    public void Uebernehmen_und_Entfernen_lassen_den_Katalogverweis_stehen()
    {
        var zeilen = new List<GebaeudeProjektZeile> { Zeile(11) };
        zeilen[0].IdKatalog = 17;
        var cut = Aufbauen(zeilen: zeilen);

        cut.FindAll("button.epos-anlagenwahl").Last().Click();   // eine Katalogzeile
        Uebernehmen(cut).Click();

        Assert.Equal(2, zeilen.Count);
        Assert.Equal(42, zeilen[1].IdKatalog);                    // aus StammSatz

        cut.FindAll("button.epos-anlagenwahl")[1].Click();        // die neue Projektzeile
        Entfernen(cut).Click();

        GebaeudeProjektZeile bleibt = Assert.Single(zeilen);
        Assert.Equal(11, bleibt.IdZ);
        Assert.Equal(17, bleibt.IdKatalog);
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

        Knopf(cut, "Fläche und Verbrauch…").Click();

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

    /// <summary>
    /// Stufe G6a: Lehnt die Fassade das Gebäude benannt ab (etwa mit zwei Zonen), nennt die Meldung
    /// diesen Grund statt der allgemeinen Bitte um Klimaregion.
    /// </summary>
    [Fact]
    public void Ohne_Ergebnis_nennt_der_Dialog_den_benannten_Grund()
    {
        var cut = Aufbauen(bedarfGaben: _ => null);
        cut.Render(p => p.Add(x => x.BedarfBefund, () => "Haus: Das Gebäude trägt 2 Zonen."));

        Knopf(cut, "Simulation...").Click();

        Assert.False(cut.Instance.BedarfOffen);
        Assert.Equal("Für dieses Gebäude lässt sich kein Wärmebedarf berechnen: Haus: Das Gebäude trägt 2 Zonen.",
                     cut.Instance.Meldung);
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

        KatalogWaehlen(cut, "Haus 2010");
        Knopf(cut, "Gebäude in DB löschen").Click();

        Assert.Contains("wirklich gelöscht", cut.Markup);
        Knopf(cut, "Ja").Click();

        Assert.Equal("Haus 2010", geloescht);
        Assert.Contains("Gebäude gelöscht!", cut.Instance.Meldung);
        Assert.Null(cut.Instance.Katalogzeile);
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

    /// <summary>
    /// <b>Die Löschsperre der Verwaltung gilt auch hier</b> (#487): Führt ein Projekt den
    /// Satz (oder ist er ein Auslieferungssatz), steht der benannte Grund als Warnung — ohne
    /// Rückfrage, ohne Löschversuch.
    /// </summary>
    [Fact]
    public void Loeschen_eines_gesperrten_Satzes_nennt_den_Grund()
    {
        const string GRUND = "In Projekten verwendet (Projekt A) – Löschen gesperrt; dort zuerst entfernen.";
        bool gerufen = false;
        string gefragt = "";
        var cut = Aufbauen(katalogLoeschen: _ => { gerufen = true; return true; },
                           katalogLoeschsperre: n => { gefragt = n; return GRUND; });

        KatalogWaehlen(cut, "Haus 2010");
        Knopf(cut, "Gebäude in DB löschen").Click();

        Assert.Equal("Haus 2010", gefragt);
        Assert.Equal(GRUND, cut.Instance.Meldung);
        Assert.DoesNotContain("wirklich gelöscht", cut.Markup);
        Assert.False(gerufen);
    }

    /// <summary>Ohne markierten Katalogsatz: die Bitte um eine Wahl statt einer leeren Rückfrage.</summary>
    [Fact]
    public void Loeschen_ohne_Wahl_bittet_um_eine_Wahl()
    {
        var cut = Aufbauen(katalogLoeschsperre: _ => "");

        Knopf(cut, "Gebäude in DB löschen").Click();

        Assert.Equal("Gebäude in DB auswählen!", cut.Instance.Meldung);
        Assert.DoesNotContain("wirklich gelöscht", cut.Markup);
    }

    /// <summary>Lehnt der Schreibweg nach dem „Ja" ab, steht die Absage da, statt still nichts zu tun.</summary>
    [Fact]
    public void Ein_abgelehntes_Loeschen_meldet_den_Fehlschlag()
    {
        var cut = Aufbauen(katalogLoeschen: _ => false, katalogLoeschsperre: _ => "");

        cut.FindAll("button.epos-anlagenwahl").Last().Click();
        Knopf(cut, "Gebäude in DB löschen").Click();
        Knopf(cut, "Ja").Click();

        Assert.Equal("Der Datensatz konnte nicht aus der Datenbank gelöscht werden.", cut.Instance.Meldung);
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

        Knopf(cut, "Fläche und Verbrauch…").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.False(gerufen);
    }

    /// <summary>Das Kreuz im Dialogkopf wirkt wie Esc: Abbrechen ohne zu speichern.</summary>
    [Fact]
    public void Kreuz_schliesst_wie_Esc()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog-zu").Click();

        Assert.False(ergebnis);
    }

    /// <summary>
    /// Die vier Ueberlagerungen (Katalogeditor, Wohnfläche, Gebäudetypen, Bedarf)
    /// trugen bislang kein ✕ (<c>Schliessbar="false"</c>) — jetzt schließt ihr Kreuz
    /// wie „Abbrechen": Die Ueberlagerung geht wieder zu.
    /// </summary>
    [Fact]
    public void Ueberlagerungskreuz_schliesst_den_Bedarfsdialog()
    {
        var cut = Aufbauen(zeilen: new List<GebaeudeProjektZeile> { Zeile(4711) },
                           bedarfGaben: z => new Dictionary<string, object>
                           {
                               ["Daten"] = new GebaeudeBedarfDaten { Name = z.Name }
                           });

        Knopf(cut, "Simulation...").Click();
        Assert.True(cut.Instance.BedarfOffen);

        cut.Find(".epos-ueberlagerung-zu").Click();

        Assert.False(cut.Instance.BedarfOffen);
    }

    /// <summary>
    /// „Das Kreuz steht beim Titel": Die Katalogeditor-Überlagerung trägt Titel und ✕,
    /// der eingebettete <c>GebaeudeKatalogDialog</c> (<c>TitelAnzeigen="false"</c>) keins
    /// von beidem — sonst stünden zwei Kreuze und zwei Titel übereinander.
    /// </summary>
    [Fact]
    public void Die_Ueberlagerung_Katalogeditor_zeigt_nur_ein_Kreuz()
    {
        var cut = Aufbauen(katalogGaben: _ => new Dictionary<string, object>());

        Knopf(cut, "Gebäude in DB neu...").Click();
        Assert.True(cut.Instance.KatalogeditorOffen);

        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
    }

    /// <summary>
    /// „Das Kreuz steht beim Titel": Die Wohnflächen-Überlagerung trägt Titel und ✕, der
    /// eingebettete <c>GebaeudeWohnflaecheDialog</c> (<c>TitelText=""</c>) keins von beidem.
    /// </summary>
    [Fact]
    public void Die_Ueberlagerung_Wohnflaeche_zeigt_nur_ein_Kreuz()
    {
        var cut = Aufbauen(wohnflaecheGaben: _ => new Dictionary<string, object>());

        Knopf(cut, "Fläche und Verbrauch…").Click();
        Assert.True(cut.Instance.WohnflaecheOffen);

        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
    }

    /// <summary>
    /// „Das Kreuz steht beim Titel": Die Gebäudetypen-Überlagerung trägt Titel und ✕, der
    /// eingebettete <c>GebaeudetypDialog</c> (<c>TitelText=""</c>) keins von beidem.
    /// </summary>
    [Fact]
    public void Die_Ueberlagerung_Gebaeudetypen_zeigt_nur_ein_Kreuz()
    {
        var cut = Aufbauen(gebaeudetypGaben: () => new Dictionary<string, object>());

        Knopf(cut, "Gebäudetyp in DB ändern...").Click();
        Assert.True(cut.Instance.GebaeudetypOffen);

        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
    }

    /// <summary>
    /// „Das Kreuz steht beim Titel": Die Bedarfs-Überlagerung trägt Titel und ✕, der
    /// eingebettete <c>GebaeudeBedarfDialog</c> (<c>TitelText=""</c>) keins von beidem.
    /// </summary>
    [Fact]
    public void Die_Ueberlagerung_Bedarf_zeigt_nur_ein_Kreuz()
    {
        var cut = Aufbauen(zeilen: new List<GebaeudeProjektZeile> { Zeile(4711) },
                           bedarfGaben: z => new Dictionary<string, object>
                           {
                               ["Daten"] = new GebaeudeBedarfDaten { Name = z.Name }
                           });

        Knopf(cut, "Simulation...").Click();
        Assert.True(cut.Instance.BedarfOffen);

        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
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
    // Die eine Fussleiste je Betriebsart - DL-2, Schritt 7 (Entscheid DL-Q1 b)
    // =================================================================================

    /// <summary>Die LETZTE Knopfleiste unter dem Dialoginhalt — der Fuß.</summary>
    private static IElement Fuss(IRenderedComponent<GebaeudeDialog> cut)
    {
        var leisten = cut.FindAll(".epos-dialog > .epos-leiste");
        return leisten[leisten.Count - 1];
    }

    /// <summary>Die Listenleiste unter dem Katalog — sie steht IN der Zweispaltenauswahl.</summary>
    private static IElement Katalogleiste(IRenderedComponent<GebaeudeDialog> cut)
        => cut.FindAll(".epos-zweispalten .epos-leiste")[0];

    /// <summary>
    /// <b>Projektbetrieb.</b> Der Fuß läuft
    /// <b>Ändern · Simulation… · Gebäudetyp in DB ändern… · Füller · Abbrechen · OK</b>
    /// — eine einzige Leiste (<c>SpeichernLeiste</c> mit Aktionsschlitz), OK als
    /// letzter und einziger primärer Knopf.
    /// </summary>
    [Fact]
    public void Im_Projekt_stehen_die_drei_Aktionen_im_Fuss_vor_Abbrechen_und_OK()
    {
        var cut = Aufbauen(bedarfGaben: _ => new Dictionary<string, object>(),
                           gebaeudetypGaben: () => new Dictionary<string, object>());

        IElement fuss = Fuss(cut);

        Assert.Equal(
            new[] { "Fläche und Verbrauch…", "Simulation...", "Gebäudetyp in DB ändern...", "Abbrechen", "OK" },
            fuss.QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray());

        // Der Fueller der SpeichernLeiste ist ihre Statusspanne (flex: 1 1 auto):
        // Sie steht zwischen den Aktionen und den zwei Schlussknoepfen.
        Assert.Single(fuss.QuerySelectorAll(".epos-status"));

        var primaer = fuss.QuerySelectorAll("button.epos-knopf--primaer");
        Assert.Single(primaer);
        Assert.Equal("OK", primaer[0].TextContent.Trim());
        Assert.Same(fuss.QuerySelectorAll("button").Last(), primaer[0]);

        // Keine zweite Leiste im Detailblock mehr - die drei Knoepfe sind gewandert.
        Assert.Equal(2, cut.FindAll(".epos-leiste").Count);      // Katalogliste + Fuss
    }

    /// <summary>
    /// Die Listenleiste der Katalogspalte läuft <b>Neu… · Ändern… · Löschen</b>; den
    /// Gebäudetyp-Knopf trägt im Projekt der Fuß (Entscheid DL-Q1 b) — nie die
    /// Listenleiste.
    /// </summary>
    [Fact]
    public void Der_Gebaeudetyp_Knopf_steht_im_Projekt_nur_im_Fuss()
    {
        var projekt = Aufbauen(gebaeudetypGaben: () => new Dictionary<string, object>());

        Assert.Equal(new[] { "Gebäude in DB neu...", "Gebäude in DB ändern...",
                             "Gebäude in DB löschen" },
                     Katalogleiste(projekt).QuerySelectorAll("button")
                         .Select(b => b.TextContent.Trim()).ToArray());
        Assert.Contains(Fuss(projekt).QuerySelectorAll("button"),
                        b => b.TextContent.Trim() == "Gebäudetyp in DB ändern...");
    }

    /// <summary>
    /// Ein gewanderter Knopf behält seinen Handler: „Ändern" im Fuß öffnet weiter die
    /// Wohnflächenangabe, „Simulation…" den Wärmebedarf, „Gebäudetyp in DB ändern…"
    /// die Typenverwaltung — und alle drei hängen weiter an der Markierung bzw. am
    /// Delegaten.
    /// </summary>
    [Fact]
    public void Die_gewanderten_Knoepfe_rufen_denselben_Weg()
    {
        var cut = Aufbauen(wohnflaecheGaben: _ => new Dictionary<string, object>(),
                           bedarfGaben: _ => new Dictionary<string, object>(),
                           gebaeudetypGaben: () => new Dictionary<string, object>());

        IElement fuss = Fuss(cut);

        fuss.QuerySelectorAll("button").First(b => b.TextContent.Trim() == "Fläche und Verbrauch…").Click();
        Assert.True(cut.Instance.WohnflaecheOffen);
        cut.Find(".epos-ueberlagerung-zu").Click();

        Fuss(cut).QuerySelectorAll("button")
            .First(b => b.TextContent.Trim() == "Simulation...").Click();
        Assert.True(cut.Instance.BedarfOffen);
        cut.Find(".epos-ueberlagerung-zu").Click();

        Fuss(cut).QuerySelectorAll("button")
            .First(b => b.TextContent.Trim() == "Gebäudetyp in DB ändern...").Click();
        Assert.True(cut.Instance.GebaeudetypOffen);
    }

    /// <summary>
    /// <b>Der Arbeitsstand.</b> „Abbrechen" schreibt nichts: Es meldet <c>false</c>,
    /// auch nachdem die Projektliste im Dialog verändert wurde — geschrieben wird
    /// erst im OK-Weg des Wirtes.
    /// </summary>
    [Fact]
    public void Abbrechen_schreibt_nichts()
    {
        bool? ergebnis = null;
        int geloescht = 0;
        var zeilen = new List<GebaeudeProjektZeile> { Zeile(1) };

        var cut = Aufbauen(zeilen: zeilen,
                           katalogLoeschen: _ => { geloescht++; return true; },
                           geschlossen: b => ergebnis = b);

        // Eine Aufnahme aus dem Katalog - der Arbeitsstand des Dialogs aendert sich.
        cut.FindAll("button.epos-anlagenwahl").Last().Click();
        Uebernehmen(cut).Click();
        Assert.Equal(2, zeilen.Count);

        Knopf(cut, "Abbrechen").Click();

        Assert.False(ergebnis);
        Assert.Equal(0, geloescht);
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
        Assert.Equal("▲", hinzu.QuerySelector(".epos-zweispalten-pfeil")!.TextContent);
        Assert.Equal("▼", weg.QuerySelector(".epos-zweispalten-pfeil")!.TextContent);

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
                     cut.Find(".epos-zeile--markiert td:nth-child(2)").TextContent.Trim());
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
        Assert.Contains("epos-zweispalten-spalte--oben", bereiche[0]);
        Assert.Contains("epos-zweispalten-uebernahme", bereiche[1]);
        Assert.Contains("epos-zweispalten-spalte--unten", bereiche[2]);

        // Beide Listen stehen weiterhin in ihrem Rahmen (Befund W9-B-2).
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-spalte .epos-raster-huelle").Count);
    }

    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F3)
    // =====================================================================

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Sie bindet über die
    /// Sichtklasse <c>GebaeudeKiSicht</c> und steht deshalb nicht in der Markup-Probe
    /// des Dialogkatalogs — dieser Fall ist ihr Ersatz: Die Maske steht gezeichnet da,
    /// die Brücke liest den Namen des markierten Satzes, und ein Setzen des Suchmusters
    /// landet in der Suche der Katalogliste (Welle K) — die Liste filtert danach.
    /// </summary>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_setzt_die_Suche()
    {
        var stand = new Katalogfilterstand();
        var cut = Aufbauen(filterstand: stand);

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.GEBAEUDE));

        WindowsFormsApplication1.KiFeldzugang name =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.GEBAEUDE, "name");
        Assert.NotNull(name);
        Assert.Equal("Haus 1990", name.Lesen());
        Assert.False(name.Setzbar);

        WindowsFormsApplication1.KiFeldzugang suche =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.GEBAEUDE, "suche");
        Assert.True(suche.Setzbar);

        suche.Setzen("Haus*");
        Assert.Equal("Haus*", suche.Lesen());
        Assert.Equal("Haus*", stand.Suche);

        cut.Render();
        cut.WaitForAssertion(() => Assert.Equal(new[] { "Haus 1990", "Haus 2010" }, Katalognamen(cut)));
    }

    /// <summary>
    /// <b>Das Baujahr ist ein WAHLFELD</b> (KI-D-Q6) über die Werte, die in der Spalte
    /// „Baujahr" stehen: Gesetzt wird über den Anzeigetext, und der landet als Ausdruck im
    /// Trichter der Spalte — die Liste zeigt danach nur die Sätze dieser Klasse.
    /// </summary>
    [Fact]
    public void Der_Assistent_waehlt_die_Baualtersklasse_ueber_ihren_Text()
    {
        var stand = new Katalogfilterstand();
        var cut = Aufbauen(filterstand: stand);

        WindowsFormsApplication1.KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.GEBAEUDE, "filter_baujahr");
        Assert.NotNull(zugang);
        Assert.Equal(new[] { "1969 bis 1978", "1984 bis 1994", "2010 bis 2015" },
                     zugang.Wahleintraege().Select(e => e.Text).ToArray());

        KiFeldumsetzung umsetzung = KiFeldwandler.Wandle(zugang, "2010 bis 2015");
        Assert.True(umsetzung.Ok, umsetzung.Grund);
        zugang.Setzen(umsetzung.Wert);
        cut.Render();

        KiFeldwert wert = KiMaskenbruecke.Lesen(KiMaskennamen.GEBAEUDE)
                                         .Single(f => f.Name == "filter_baujahr");
        Assert.Equal("2010 bis 2015", wert.Text);
        Assert.Equal("2010 bis 2015", stand.Ausdruck(Katalogfilterprofil.SpBaualtersklasse));
        cut.WaitForAssertion(() => Assert.Equal(new[] { "Haus 2010" }, Katalognamen(cut)));
    }

    /// <summary>
    /// <b>Die Verwendung ist ein Trichter, den der Assistent setzt und leert</b>: Die Wahl
    /// trägt die zwei Verwendungen, die in der Spalte stehen; „leer" nimmt den Filter zurück
    /// (früher war das Feld Pflicht — die Optionsgruppe hatte immer einen Wert).
    /// </summary>
    [Fact]
    public void Der_Assistent_setzt_und_leert_den_Trichter_Verwendung()
    {
        var stand = new Katalogfilterstand();
        var cut = Aufbauen(filterstand: stand);

        WindowsFormsApplication1.KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.GEBAEUDE, "verwendung");
        Assert.Equal(new[] { "Gewerbe+Sonstige", "Wohngebäude" },
                     zugang.Wahleintraege().Select(e => e.Schluessel).ToArray());

        KiFeldumsetzung gewerbe = KiFeldwandler.Wandle(zugang, "gewerbe");
        Assert.True(gewerbe.Ok, gewerbe.Grund);
        zugang.Setzen(gewerbe.Wert);
        cut.Render();
        Assert.Equal("Gewerbe+Sonstige", stand.Ausdruck(Katalogfilterprofil.SpVerwendung));
        cut.WaitForAssertion(() => Assert.Equal(new[] { "Hotel Sonne" }, Katalognamen(cut)));

        KiFeldumsetzung leer = KiFeldwandler.Wandle(zugang, "");
        Assert.True(leer.Ok, leer.Grund);
        zugang.Setzen(leer.Wert);
        cut.Render();
        Assert.False(stand.Gefiltert(Katalogfilterprofil.SpVerwendung));
        cut.WaitForAssertion(() => Assert.Equal(3, Katalognamen(cut).Length));
    }

    // =================================================================================
    // Stufe G1 (Umsetzungskonzept Gebaeudesimulation 2.7): Rechenweg und H_ges
    // =================================================================================

    /// <summary>
    /// Die Spalte „Rechenweg" der Projektliste — ohne sie wäre der Rechenweg (E1) für den
    /// Anwender unsichtbar; den Text setzt die Hülle so, wie die Weiche rechnet.
    /// </summary>
    [Fact]
    public void Die_Projektliste_fuehrt_die_Spalte_Rechenweg()
    {
        GebaeudeProjektZeile z = Zeile(1);
        z.Rechenweg = "Tagesbilanz (Bestandsweg) (Vorgabe)";
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { z });

        var koepfe = cut.FindAll(".epos-zweispalten table.epos-raster")[0]
                        .QuerySelectorAll("thead th").Select(t => t.TextContent.Trim()).ToList();
        Assert.Contains("Rechenweg", koepfe);
        Assert.Contains("Tagesbilanz (Bestandsweg) (Vorgabe)", cut.Markup);
    }

    [Fact]
    public void Der_Detailblock_zeigt_H_ges_und_den_Rechenweg_nur_lesend()
    {
        GebaeudeProjektZeile z = Zeile(1);
        z.Rechenweg = "VDI 6007";
        z.HgesWK = 2046.25;
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { z });

        var felder = cut.FindAll("label.epos-feld");
        IElement hges = felder.First(l => l.TextContent.Contains("Wärmeleitwert H_ges:")).QuerySelector("input")!;
        IElement weg = felder.First(l => l.TextContent.Contains("Rechenweg:")).QuerySelector("input")!;

        Assert.Equal(2046.25.ToString("N1", System.Globalization.CultureInfo.GetCultureInfo("de-DE")) + " W/K", hges.GetAttribute("value"));
        Assert.True(hges.HasAttribute("readonly"));
        Assert.Equal("VDI 6007", weg.GetAttribute("value"));
    }

    /// <summary>Ohne Wert steht „—", nie eine erfundene Zahl — auch bei einem Katalogsatz.</summary>
    [Fact]
    public void Ohne_Wert_steht_ein_Strich_und_der_Katalogsatz_bringt_seine_Kennwerte()
    {
        var cut = Render<GebaeudeDialog>(p => p
            .Add(x => x.Zeilen, new List<GebaeudeProjektZeile> { Zeile(1) })
            .Add(x => x.Katalogzeilen, () => Katalog())
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.StammDetail, n => new GebaeudeStammDetail(n, "Einfamilienhaus", "Katalogtext",
                                                                  "150,00", "VDI 6007", 500.0)));

        IElement hges = cut.FindAll("label.epos-feld")
                           .First(l => l.TextContent.Contains("Wärmeleitwert H_ges:")).QuerySelector("input")!;
        Assert.Equal("—", hges.GetAttribute("value"));

        KatalogWaehlen(cut, "Haus 1990");

        Assert.Equal(500.0.ToString("N1", System.Globalization.CultureInfo.GetCultureInfo("de-DE")) + " W/K",
                     cut.FindAll("label.epos-feld")
                        .First(l => l.TextContent.Contains("Wärmeleitwert H_ges:")).QuerySelector("input")!
                        .GetAttribute("value"));
    }
}
