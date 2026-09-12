using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <b>Die EINE Katalogliste des Hauses</b> (Anwenderentscheid <b>W14a‑E‑10</b> vom
/// 07.09.2026, Konzept_Katalogfilter 5.6, Schritte S1.2, S1.3 und S1.8).
///
/// <para>Geprüft wird das ganze Schema: Zone A (Suche links, Trefferzahl rechts,
/// Rücksetzer nur wenn gesetzt), der Spaltenkopf (Sortierzyklus auf → ab → aus,
/// Trichter gefüllt gegen Umriss, Popover), die Verknüpfung von Suche und
/// Spaltenfilter, „Kein Treffer." statt einer leeren Liste, das Bleiben der
/// Markierung — und der Übergang <b>20 749 → 15</b> unter <c>Virtualisiert</c>,
/// an dem der <c>@key</c>-Fix W6‑B‑2 hängt.</para>
///
/// <para>Kultur gepinnt (Hausregel seit iU9‑W8).</para>
/// </summary>
public class KataloglisteTests : EposBunitContext
{
    public KataloglisteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // =====================================================================
    //  Probedaten - der Heizkesselkatalog in klein
    // =====================================================================

    private static Katalogfilterprofil Profil() =>
        Katalogfilterprofil.Finde(Anlagenart.Heizkessel, s => s);

    private static Katalogfilterzeile Zeile(int id, string name, string firma, string brennstoff,
                                            double? pth, double? eta, bool brennwert)
    {
        return new Katalogfilterzeile(id, name)
            .MitText(Katalogfilterprofil.SpBezeichner, name)
            .MitText(Katalogfilterprofil.SpHersteller, firma)
            .MitText(Katalogfilterprofil.SpBrennstoff, brennstoff)
            .MitZahl(Katalogfilterprofil.SpPtherm, pth)
            .MitZahl(Katalogfilterprofil.SpEta, eta, 3)
            .MitKennzeichen(Katalogfilterprofil.SpBrennwert, brennwert);
    }

    private static List<Katalogfilterzeile> Zeilen() => new()
    {
        Zeile(1, "Alpha", "Vaillant", "Erdgas E", 15.0, 0.97, true),
        Zeile(2, "Beta",  "Buderus",  "Heizöl EL", 80.0, 0.90, false),
        Zeile(3, "Gamma", "Vaillant", "Stadtgas", 40.0, 0.98, false)
    };

    private IRenderedComponent<Katalogliste> Aufbauen(
        IReadOnlyList<Katalogfilterzeile>? zeilen = null,
        Katalogfilterstand? stand = null,
        string gewaehlt = "",
        Action<string>? gewaehltGeaendert = null)
    {
        return Render<Katalogliste>(p => p
            .Add(x => x.Profil, Profil())
            .Add(x => x.Zeilen, zeilen ?? Zeilen())
            .Add(x => x.Filterstand, stand ?? new Katalogfilterstand())
            .Add(x => x.Gewaehlt, gewaehlt)
            .Add(x => x.GewaehltChanged,
                 EventCallback.Factory.Create<string>(this, w => gewaehltGeaendert?.Invoke(w))));
    }

    // =====================================================================
    //  Zone A - die EINE Suchzeile (5.6.4)
    // =====================================================================

    /// <summary>
    /// Über der Liste steht genau EINE Zeile: das Suchfeld links, die Trefferzahl
    /// rechts. <b>Keine Filterzeile, keine Chips, keine Aufzählung.</b>
    /// </summary>
    [Fact]
    public void Zone_A_ist_EINE_Zeile_mit_Suche_und_Trefferzahl()
    {
        var cut = Aufbauen();

        Assert.Single(cut.FindAll(".epos-katalog-suchzeile"));
        Assert.Single(cut.FindAll(".epos-katalog-suchzeile input"));
        Assert.Equal("3 von 3 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);

        // Es gibt keine Chips und keine Filterzeile - beides hat der Anwender
        // abgewaehlt ("nicht separat, ausser 'Suche' ueber alle Felder").
        Assert.Empty(cut.FindAll(".epos-chip"));
    }

    /// <summary>
    /// Die Suche wirkt ODER über alle Spalten und UND über die Begriffe; die
    /// Trefferzahl folgt.
    /// </summary>
    [Fact]
    public void Die_Suche_engt_ein_und_die_Trefferzahl_folgt()
    {
        var cut = Aufbauen();

        cut.Find(".epos-katalog-suchzeile input").Input("vaillant");
        Gezeichnet(cut, "2 von 3 Sätzen", 2);

        cut.Find(".epos-katalog-suchzeile input").Input("vaillant erdgas");
        Gezeichnet(cut, "1 von 3 Sätzen", 1);
    }

    /// <summary>
    /// <b>„Kein Treffer." statt einer leeren Liste</b> — derselbe Satz wie im
    /// <c>EnergietraegerDialog</c> (<c>ETV_SUCHE_LEER</c>).
    /// </summary>
    [Fact]
    public void Statt_einer_leeren_Liste_steht_Kein_Treffer()
    {
        var cut = Aufbauen();
        Assert.Empty(cut.FindAll(".epos-katalog-leer"));

        cut.Find(".epos-katalog-suchzeile input").Input("gibtesnicht");

        Gezeichnet(cut, "0 von 3 Sätzen", 0);
        Assert.Single(cut.FindAll(".epos-katalog-leer"));
    }

    /// <summary>
    /// <b>Der Rücksetzer erscheint NUR, wenn ein Spaltenfilter gesetzt ist</b>
    /// (5.6.4) — eine Suche allein zeigt ihn nicht, denn sie hat ihr eigenes Feld.
    /// </summary>
    [Fact]
    public void Der_Ruecksetzer_steht_nur_bei_gesetztem_Spaltenfilter()
    {
        var stand = new Katalogfilterstand();
        var cut = Aufbauen(stand: stand);
        Assert.Empty(cut.FindAll(".epos-katalog-ruecksetzer"));

        // Erst den gezeichneten Sucherfolg abwarten - eine Prüfung auf ABWESENHEIT
        // wäre sonst auch dann grün, wenn das Ereignis noch in der Warteschlange
        // liegt (W6-B-2-O-1, siehe Gezeichnet).
        cut.Find(".epos-katalog-suchzeile input").Input("vaillant");
        Gezeichnet(cut, "2 von 3 Sätzen", 2);
        Assert.Empty(cut.FindAll(".epos-katalog-ruecksetzer"));

        Filter(cut, Katalogfilterprofil.SpBrennstoff, "Gas");
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-katalog-ruecksetzer")));

        cut.Find(".epos-katalog-ruecksetzer").Click();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".epos-katalog-ruecksetzer")));
        Assert.False(stand.Gesetzt);

        // Die SUCHE bleibt - der Knopf ist der Ruecksetzer der Trichter.
        Assert.Equal("vaillant", stand.Suche);
    }

    // =====================================================================
    //  Der Spaltenkopf (5.6.2)
    // =====================================================================

    /// <summary>
    /// Die Liste zeigt die SECHS Parameterspalten des Profils plus die Wahlspalte —
    /// statt der einen Namensspalte des Vorläufers (Befund 1.2/2).
    /// </summary>
    [Fact]
    public void Die_Liste_zeigt_die_Spalten_des_Profils()
    {
        var cut = Aufbauen();

        Assert.Equal(7, cut.FindAll("thead th").Count);
        Assert.Equal(6, cut.FindAll(".epos-spaltenkopf").Count);
        Assert.Contains("Erdgas E", cut.Find("tbody").TextContent);
        Assert.Contains("0,970", cut.Find("tbody").TextContent);
    }

    /// <summary>
    /// <b>Eine Kennzeichenspalte trägt NUR den Sortierpfeil</b> (5.6.2): fünf
    /// Trichter für sechs Spalten — „Brennwert" hat keinen.
    /// </summary>
    [Fact]
    public void Die_Kennzeichenspalte_traegt_keinen_Trichter()
    {
        var cut = Aufbauen();

        Assert.Equal(5, cut.FindAll(".epos-trichter").Count);
        Assert.Equal(6, cut.FindAll(".epos-spaltenkopf-titel").Count);
    }

    /// <summary>
    /// <b>Gefüllt gegen Umriss</b>, und zwar im MARKUP (Auflage des Anwenders zu
    /// Rev. 3): ohne Filter <c>fill="none"</c> und Strichstärke 1,3, mit Filter
    /// <c>fill="currentColor"</c> und Strichstärke 2. In Graustufen bleibt ein
    /// voller Trichter ein voller Trichter.
    /// </summary>
    [Fact]
    public void Der_gefilterte_Trichter_ist_gefuellt_und_nicht_nur_bunt()
    {
        var cut = Aufbauen();

        Assert.All(cut.FindAll(".epos-trichter-bild path"),
                   p => Assert.Equal("none", p.GetAttribute("fill")));
        Assert.Empty(cut.FindAll(".epos-trichter--gesetzt"));

        Filter(cut, Katalogfilterprofil.SpBrennstoff, "Gas");
        cut.WaitForAssertion(() =>
            Assert.Single(cut.FindAll(".epos-trichter--gesetzt .epos-trichter-bild path")));

        var gefuellt = cut.FindAll(".epos-trichter--gesetzt .epos-trichter-bild path");
        Assert.Equal("currentColor", gefuellt[0].GetAttribute("fill"));
        Assert.Equal("2", gefuellt[0].GetAttribute("stroke-width"));
    }

    /// <summary>
    /// Der Trichter öffnet und schließt das Popover; ein Klick auf einen ANDEREN
    /// Trichter wechselt es.
    /// </summary>
    [Fact]
    public void Der_Trichter_oeffnet_und_schliesst_das_Popover()
    {
        var cut = Aufbauen();
        Assert.Empty(cut.FindAll(".epos-spaltenfilter"));

        cut.FindAll(".epos-trichter")[2].Click();          // Brennstoff
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll(".epos-spaltenfilter"));
            Assert.Equal(Katalogfilterprofil.SpBrennstoff, cut.Instance.OffenesPopover);
        });

        cut.FindAll(".epos-trichter")[3].Click();          // P_th
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll(".epos-spaltenfilter"));
            Assert.Equal(Katalogfilterprofil.SpPtherm, cut.Instance.OffenesPopover);
        });

        cut.FindAll(".epos-trichter")[3].Click();          // derselbe -> zu
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".epos-spaltenfilter")));
    }

    /// <summary>
    /// Eine ZAHLENSPALTE bekommt den Formenhinweis, eine Textspalte den Platzhalter
    /// „enthält…" (5.6.3).
    /// </summary>
    [Fact]
    public void Zahlenspalte_und_Textspalte_bekommen_verschiedene_Platzhalter()
    {
        var cut = Aufbauen();

        cut.FindAll(".epos-trichter")[0].Click();          // Bezeichner (Text)
        Assert.Equal("enthält…",
                     cut.WaitForElement(".epos-spaltenfilter input").GetAttribute("placeholder"));
        Assert.Empty(cut.FindAll(".epos-spaltenfilter-hinweis"));

        cut.FindAll(".epos-trichter")[3].Click();          // P_th (Zahl)
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-spaltenfilter-hinweis")));
        Assert.Contains("10..60", cut.Find(".epos-spaltenfilter input").GetAttribute("placeholder"));
    }

    /// <summary>
    /// <b>Der Spaltenfilter engt ein, und die gefilterte Spalte wird getönt</b> —
    /// die Klasse steht über <c>ColumnBase.Class</c> an Kopf UND Körperzellen
    /// (5.6.2).
    /// </summary>
    [Fact]
    public void Der_Spaltenfilter_engt_ein_und_toent_die_Spalte()
    {
        var cut = Aufbauen();

        Filter(cut, Katalogfilterprofil.SpBrennstoff, "Gas");

        Gezeichnet(cut, "2 von 3 Sätzen", 2);

        Assert.Single(cut.FindAll("thead th.epos-spalte--gefiltert"));
        Assert.Equal(2, cut.FindAll("tbody td.epos-spalte--gefiltert").Count);
    }

    /// <summary>
    /// <b>Der Zahlenausdruck im Popover</b> (Frage Q1 = ja): <c>10..50</c> und
    /// <c>&gt;=0,95</c> wirken; ein unverstandener Ausdruck ist KEIN Filter.
    /// </summary>
    [Fact]
    public void Das_eine_Feld_versteht_den_Zahlenausdruck()
    {
        var cut = Aufbauen();

        Filter(cut, Katalogfilterprofil.SpPtherm, "10..50");
        Gezeichnet(cut, "2 von 3 Sätzen", 2);

        Filter(cut, Katalogfilterprofil.SpEta, ">=0,975");
        Gezeichnet(cut, "1 von 3 Sätzen", 1);
        Assert.Contains("Gamma", cut.Find("tbody").TextContent);

        // Unverstanden = kein Filter: die Zeilen bleiben, wie sie waren.
        Filter(cut, Katalogfilterprofil.SpEta, ">=");
        Gezeichnet(cut, "2 von 3 Sätzen", 2);
    }

    /// <summary>
    /// <b>Der Sortierzyklus auf → ab → aus</b> (5.6.2), immer höchstens EINE Spalte.
    /// Der dritte Klick stellt die Reihenfolge des Controllers wieder her.
    /// </summary>
    [Fact]
    public void Der_Sortierzyklus_geht_auf_ab_aus()
    {
        var cut = Aufbauen();
        Assert.Contains("⇅", cut.FindAll(".epos-sortierpfeil")[3].TextContent);

        cut.FindAll(".epos-spaltenkopf-titel")[3].Click();          // P_th auf
        Sortiert(cut, "▲", "Alpha", "Gamma", "Beta");

        cut.FindAll(".epos-spaltenkopf-titel")[3].Click();          // ab
        Sortiert(cut, "▼", "Beta", "Gamma", "Alpha");

        cut.FindAll(".epos-spaltenkopf-titel")[3].Click();          // aus
        Sortiert(cut, "⇅", "Alpha", "Beta", "Gamma");
    }

    // =====================================================================
    //  Die Markierung (S1.8)
    // =====================================================================

    /// <summary>
    /// <b>Die gewählte Zeile BLEIBT gewählt</b>, auch wenn der Filter sie ausblendet
    /// (Hausregel aus dem <c>EnergietraegerDialog</c>, W4): Der Filter wirkt auf die
    /// ANZEIGE, nie auf die Auswahl. Beim Zurücknehmen steht sie wieder markiert da.
    /// </summary>
    [Fact]
    public void Die_Markierung_bleibt_ueber_einen_Filterwechsel()
    {
        string gewaehlt = "";
        var stand = new Katalogfilterstand();
        var cut = Aufbauen(stand: stand, gewaehltGeaendert: w => gewaehlt = w);

        cut.FindAll("tbody tr")[1].QuerySelector("button")!.Click();     // Beta
        cut.WaitForAssertion(() => Assert.Equal("Beta", gewaehlt));

        cut.Render(p => p
            .Add(x => x.Profil, Profil())
            .Add(x => x.Zeilen, Zeilen())
            .Add(x => x.Filterstand, stand)
            .Add(x => x.Gewaehlt, gewaehlt));

        // "Gas" blendet Beta (Heizoel) aus - die Auswahl des Wirtes bleibt.
        Filter(cut, Katalogfilterprofil.SpBrennstoff, "Gas");
        cut.WaitForAssertion(() => Assert.DoesNotContain("Beta", cut.Find("tbody").TextContent));
        Assert.Equal("Beta", gewaehlt);

        // Und beim Zuruecknehmen steht sie wieder markiert da.
        cut.Find(".epos-katalog-ruecksetzer").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Beta", cut.Find("tbody").TextContent);
            Assert.Single(cut.FindAll("tbody button[aria-pressed=\"true\"]"));
        });
    }

    // =====================================================================
    //  20 749 -> 15 unter Virtualisiert (5.6.6, Fix W6-B-2)
    // =====================================================================

    /// <summary>
    /// <b>Der Übergang, für den es den <c>@key</c>-Fix W6‑B‑2 gibt.</b> Der Wirt
    /// schaltet <c>Virtualisiert</c> an der GEFILTERTEN Zeilenzahl (≥ 120), und ein
    /// Spaltenfilter führt regelmäßig über diese Schwelle hinweg: 20 749 → 15. Ohne
    /// den <c>@key</c> zeigte QuickGrid danach seinen alten Zwischenspeicher — die
    /// ungefilterte Liste vom Anfang (der belegte Fehler aus dem Geräteimport).
    ///
    /// <para>Geprüft wird beides: dass der Schalter wirklich wechselt UND dass die
    /// Tabelle danach die FÜNFZEHN zeigt und nicht die alte Liste.</para>
    /// </summary>
    [Fact]
    public void Zwanzigtausend_Zeilen_werden_zu_fuenfzehn_und_das_Raster_zeigt_sie()
    {
        var viele = new List<Katalogfilterzeile>(20749);
        for (int i = 0; i < 20749; i++)
        {
            bool longi = i < 15;
            viele.Add(Zeile(i + 1,
                            (longi ? "LONGi LR5-" : "Andere M-") + i.ToString("D5", CultureInfo.InvariantCulture),
                            longi ? "LONGi Green Energy" : "Ablytek",
                            "Erdgas E", 500 + (i % 100), 0.9, false));
        }

        var stand = new Katalogfilterstand();
        var cut = Render<Katalogliste>(p => p
            .Add(x => x.Profil, Profil())
            .Add(x => x.Zeilen, viele)
            .Add(x => x.Filterstand, stand));

        Assert.True(cut.Instance.Virtualisiert);
        Assert.Equal("20.749 von 20.749 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);

        Filter(cut, Katalogfilterprofil.SpHersteller, "LONGi");

        // Erst der GEZEICHNETE Stand, dann die Prüfungen (W6-B-2-O-1, siehe
        // Gezeichnet): Trefferzeile UND fuenfzehn Zeilen im Koerper.
        Gezeichnet(cut, "15 von 20.749 Sätzen", 15);
        Assert.False(cut.Instance.Virtualisiert);

        // DIE ZEILEN SIND WIRKLICH DIE NEUEN - nicht der alte Zwischenspeicher.
        Assert.DoesNotContain("Andere M-", cut.Find("tbody").TextContent);
        Assert.Contains("LONGi LR5-00000", cut.Find("tbody").TextContent);
    }

    // =====================================================================
    //  6 654 Stromspeicher: die Liste blinkt (Befund W13-B-6, 09.09.2026)
    // =====================================================================

    /// <summary>Ein Katalog beliebiger Größe — die kW-Spalte trägt echte Zahlen.</summary>
    private static List<Katalogfilterzeile> Grosser_Katalog(int anzahl) =>
        Enumerable.Range(0, anzahl)
                  .Select(i => Zeile(i + 1,
                                     "Speicher " + i.ToString("D5", CultureInfo.InvariantCulture),
                                     i % 2 == 0 ? "Sonnen" : "BYD",
                                     "Erdgas E", 5.0 + i % 90, 0.9, false))
                  .ToList();

    /// <summary>
    /// <b>Der Befund W13‑B‑6</b> (Windows-Abnahme 09.09.2026, „Stromspeicher
    /// Einlesen" mit 6 654 Sätzen): „die Liste blinkt und ist nicht sichtbar" —
    /// statt Zeilen standen Platzhalter mit „…" in jeder Zelle, während die
    /// Trefferzeile richtig „6.654 von 6.654 Sätzen" meldete.
    ///
    /// <para><b>Die Ursache stand hier</b>, in einer einzigen Stelle des Markups:
    /// <c>Zeilen="@_gefiltert.AsQueryable()"</c> — bei JEDEM Zeichenlauf ein frisches
    /// <c>EnumerableQuery</c>. QuickGrid vergleicht seine Datenquelle nach REFERENZ
    /// (<c>OnParametersSetAsync</c>: <c>dataSourceHasChanged =
    /// _newItemsOrItemsProvider != _lastAssignedItemsOrProvider</c>), hielt jeden
    /// Zeichenlauf für eine neue Menge, brach die laufende Ladung ab und stellte
    /// hinter <c>await Task.Delay(100)</c> eine neue an. Kam der nächste Zeichenlauf
    /// schneller, war auch sie hinfällig: <c>Virtualize</c> behielt sein leeres
    /// <c>_loadedItems</c> und zeichnete nur Platzhalter.</para>
    ///
    /// <para>Geprüft wird deshalb GENAU das, worauf QuickGrid schaut: die Identität
    /// der Datenquelle über mehrere Zeichenläufe — und mit ihr, dass die Tabelle
    /// nicht dauernd lädt (die Klasse <c>loading</c> blendet den Körper auf
    /// <c>opacity: .25</c> ab: das gemeldete Blinken).</para>
    /// </summary>
    [Fact]
    public void Die_Datenquelle_des_Rasters_bleibt_ueber_Zeichenlaeufe_dieselbe()
    {
        var viele = Grosser_Katalog(6654);

        var cut = Render<Katalogliste>(p => p
            .Add(x => x.Profil, Profil())
            .Add(x => x.Zeilen, viele)
            .Add(x => x.Filterstand, new Katalogfilterstand()));

        Assert.True(cut.Instance.Virtualisiert);
        cut.WaitForAssertion(() => Assert.DoesNotContain("loading", cut.Find("table").ClassName));

        object? quelle = cut.FindComponent<QuickGrid<Katalogfilterzeile>>().Instance.Items;
        object gefiltert = cut.Instance.Angezeigt;

        for (int i = 0; i < 10; i++)
        {
            cut.Render();
            Assert.Same(quelle, cut.FindComponent<QuickGrid<Katalogfilterzeile>>().Instance.Items);
            Assert.Same(gefiltert, cut.Instance.Angezeigt);
            Assert.DoesNotContain("loading", cut.Find("table").ClassName);
        }
    }

    /// <summary>
    /// <b>Und die Gegenprobe:</b> Ändert sich der Filter WIRKLICH, bekommt QuickGrid
    /// eine andere Datenquelle. Eine festgehaltene Menge, die nach einem Filterwechsel
    /// stehen bliebe, wäre der Fehler W6‑B‑2 von der anderen Seite.
    /// </summary>
    [Fact]
    public void Ein_Filterwechsel_gibt_dem_Raster_eine_neue_Datenquelle()
    {
        var viele = Grosser_Katalog(500);

        var cut = Render<Katalogliste>(p => p
            .Add(x => x.Profil, Profil())
            .Add(x => x.Zeilen, viele)
            .Add(x => x.Filterstand, new Katalogfilterstand()));

        object? vorher = cut.FindComponent<QuickGrid<Katalogfilterzeile>>().Instance.Items;

        Filter(cut, Katalogfilterprofil.SpHersteller, "Sonnen");
        Gefiltert(cut, "250 von 500 Sätzen", 250);

        Assert.NotSame(vorher, cut.FindComponent<QuickGrid<Katalogfilterzeile>>().Instance.Items);
    }

    /// <summary>
    /// <b>Und die zweite Gegenprobe — die teuer erkaufte:</b> Mehrere Wirte ändern ihre
    /// Zeilenliste AN ORT UND STELLE. Der Ganglinienverwalter löscht mit
    /// <c>RemoveAll</c> aus derselben Liste, die er auch hereinreicht; ihre Referenz
    /// bleibt dabei dieselbe.
    ///
    /// <para>Der erste Anlauf zu W13‑B‑6 hängte das Neurechnen an genau diese
    /// Referenz (dazu an <c>Profil</c> und einen Zähler im Filterstand) und sparte
    /// damit die Filterrechnung je Zeichenlauf. Er fiel über diesen Fall:
    /// <c>SolarganglinieAdminDialogTests.Ja_loescht_und_meldet</c> und drei weitere
    /// zeigten nach dem Löschen weiter drei Zeilen. Deshalb rechnet
    /// <c>Neuberechnen</c> weiter bei jedem Zeichenlauf und hält statt dessen das
    /// ERGEBNIS fest — was hier geprüft wird.</para>
    /// </summary>
    [Fact]
    public void Eine_an_Ort_und_Stelle_geaenderte_Liste_wird_bemerkt()
    {
        var liste = Grosser_Katalog(500);

        var cut = Render<Katalogliste>(p => p
            .Add(x => x.Profil, Profil())
            .Add(x => x.Zeilen, liste)
            .Add(x => x.Filterstand, new Katalogfilterstand()));

        object? vorher = cut.FindComponent<QuickGrid<Katalogfilterzeile>>().Instance.Items;
        Assert.Equal(500, cut.Instance.Angezeigt.Count);

        // DIESELBE Instanz, anderer Inhalt - wie beim Loeschen im Ganglinienverwalter.
        liste.RemoveRange(0, 100);
        cut.Render();

        Assert.Equal(400, cut.Instance.Angezeigt.Count);
        Assert.Equal("400 von 400 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);
        Assert.NotSame(vorher, cut.FindComponent<QuickGrid<Katalogfilterzeile>>().Instance.Items);
    }

    /// <summary>
    /// <b>„Der Filter funktioniert nicht"</b> — der zweite Teil des Befundes W13‑B‑6.
    /// Der Anwender tippte in den Trichter der kW-Spalte, die Trefferzeile rechnete
    /// richtig, und die Liste zeigte trotzdem nichts.
    ///
    /// <para>Der Fall aus W6‑B‑2 (20 749 → 15) deckt den Übergang auf den FLACHEN
    /// Zweig ab; dieser hier bleibt beidseits der Schwelle VIRTUALISIERT — 500 → 250,
    /// und genau dort lebte der Fehler. Geprüft wird, dass die gezeichneten Zeilen
    /// wirklich die gefilterten sind.</para>
    /// </summary>
    [Fact]
    public void Der_Zahlenfilter_greift_auch_wenn_die_Liste_virtualisiert_bleibt()
    {
        var viele = Grosser_Katalog(500);
        var stand = new Katalogfilterstand();

        var cut = Render<Katalogliste>(p => p
            .Add(x => x.Profil, Profil())
            .Add(x => x.Zeilen, viele)
            .Add(x => x.Filterstand, stand));

        Assert.True(cut.Instance.Virtualisiert);

        // 5,0 .. 94,0 kW; ">50" laesst 224 Saetze stehen - weiter ueber der Schwelle.
        Filter(cut, Katalogfilterprofil.SpPtherm, ">50");
        cut.WaitForAssertion(() =>
            Assert.Equal("224 von 500 Sätzen", cut.Find(".epos-katalog-treffer").TextContent));

        Assert.True(cut.Instance.Virtualisiert);
        Assert.Equal(224, cut.Instance.Angezeigt.Count);
        Assert.All(cut.Instance.Angezeigt,
                   z => Assert.True(z.Zahl(Katalogfilterprofil.SpPtherm) > 50));

        // Und im KOERPER stehen die gefilterten Zeilen, keine Platzhalter.
        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll("td.grid-cell-placeholder"));
            Assert.DoesNotContain("loading", cut.Find("table").ClassName);
            Assert.NotEmpty(cut.FindAll("tbody tr"));
        });
    }

    // =====================================================================
    //  Hilfen
    // =====================================================================

    /// <summary>
    /// Öffnet den Trichter der Spalte, tippt den Ausdruck und übernimmt ihn.
    ///
    /// <para><b>Auf das GEZEICHNETE Popover warten, nicht auf den Klick</b>
    /// (W6‑B‑2‑O‑1): Zwischen dem <c>Click()</c> auf den Trichter und dem
    /// <c>Change()</c> im Feld liegt ein Zeichenlauf, den bunits synchrones
    /// <c>Click()</c> nicht abwartet — die Begründung steht bei
    /// <see cref="Gezeichnet"/>. <c>Find</c> würfe hier sofort
    /// <c>ElementNotFound</c>; <c>WaitForElement</c> wartet auf den nächsten
    /// Zeichenlauf.</para>
    /// </summary>
    private static void Filter(IRenderedComponent<Katalogliste> cut, string schluessel,
                               string ausdruck)
    {
        Katalogfilterprofil profil = Profil();
        int index = 0;
        for (int i = 0; i < profil.Spalten.Count; i++)
        {
            if (profil.Spalten[i].Schluessel == schluessel) break;
            if (profil.Spalten[i].Filterbar) index++;
        }

        cut.FindAll(".epos-trichter")[index].Click();
        cut.WaitForElement(".epos-spaltenfilter input").Change(ausdruck);
    }

    /// <summary>
    /// Wartet auf den GEZEICHNETEN Stand der Liste: Die Trefferzeile trägt
    /// <paramref name="trefferzeile"/>, und im Körper stehen <paramref name="zeilen"/>
    /// Zeilen.
    ///
    /// <para><b>W6‑B‑2‑O‑1: bunits synchrone Ereignisse warten NICHT.</b>
    /// <c>Click()</c>, <c>Change()</c> und <c>Input()</c> geben das Ereignis nur beim
    /// Zeichner ab; nur die <c>…Async</c>-Fassungen liefern laut bunit-Dokumentation
    /// „a task that completes when the event handler is done". Der Zeichnerfaden
    /// (<c>RendererSynchronizationContext</c>) arbeitet ein Werkstück auf dem
    /// AUFRUFENDEN Faden ab, solange seine Warteschlange frei ist; liegt dort schon
    /// eines, wird das Ereignis EINGEREIHT, der Aufruf kehrt sofort zurück, und die
    /// nächste Zeile des Falls liest den Stand VOR dem Ereignis.</para>
    ///
    /// <para><b>In dieser Liste legt der <c>@key</c>-Fix W6‑B‑2 das Werkstück
    /// selbst hin.</b> Der Schlüssel des Rasters ist <c>(Virtualisiert,
    /// Zeilenzahl)</c> (<c>Raster.razor</c>); beim Übergang 20 749 → 15 ändern sich
    /// beide, Blazor verwirft das alte QuickGrid und baut ein NEUES — mitsamt
    /// dessen <c>OnAfterRenderAsync</c> und dem asynchronen Datenabruf, aus dem die
    /// fünfzehn Zeilen erst in einem SPÄTEREN Zeichenlauf fallen als die
    /// Trefferzeile. Deshalb wartet dieser Helfer auf BEIDES.</para>
    ///
    /// <para>GEMESSEN am wörtlichen Prüfstand (temporäre Klasse, nach der Messung
    /// gelöscht): ohne Fremdlast 0 von 60 rot; mit 8 Rechenfäden auf 4 Kernen
    /// 1 von 60 rot, und zwar mit genau dem gemeldeten Bild — Trefferzeile
    /// „20.749 von 20.749 Sätzen" statt „15 von 20.749 Sätzen". Mit einer von einem
    /// fremden Faden BELEGTEN Warteschlange fällt das alte Muster in 15 von 15
    /// Läufen um, das neue in 0 von 15. Das ist der Befund aus der
    /// Windows-Sandbox vom 07.09.2026: „flackert unter Last und ist allein
    /// grün".</para>
    /// </summary>
    private static void Gezeichnet(IRenderedComponent<Katalogliste> cut, string trefferzeile,
                                   int zeilen)
        => cut.WaitForAssertion(() =>
        {
            Assert.Equal(trefferzeile, cut.Find(".epos-katalog-treffer").TextContent);
            Assert.Equal(zeilen, cut.FindAll("tbody tr").Count);
        });

    /// <summary>
    /// Wie <see cref="Gezeichnet"/>, aber für eine Liste, die VIRTUALISIERT bleibt:
    /// Dort stehen die gefilterten Zeilen nicht alle im Baum — QuickGrid hält nur den
    /// sichtbaren Ausschnitt —, und <c>tbody tr</c> zu zählen hieße, die
    /// Virtualisierung zu prüfen statt den Filter. Gewartet wird deshalb auf die
    /// Trefferzeile und auf die gefilterte MENGE.
    /// </summary>
    private static void Gefiltert(IRenderedComponent<Katalogliste> cut, string trefferzeile,
                                  int zeilen)
        => cut.WaitForAssertion(() =>
        {
            Assert.Equal(trefferzeile, cut.Find(".epos-katalog-treffer").TextContent);
            Assert.Equal(zeilen, cut.Instance.Angezeigt.Count);
        });

    /// <summary>
    /// Wartet auf den GEZEICHNETEN Sortierpfeil der Spalte P_th und auf die
    /// zugehörige Reihenfolge. Begründung wie bei <see cref="Gezeichnet"/> — auch ein
    /// Klick auf den Spaltenkopf ist ein Ereignis, hinter dem <c>Click()</c> nicht
    /// wartet.
    /// </summary>
    private static void Sortiert(IRenderedComponent<Katalogliste> cut, string pfeil,
                                 params string[] namen)
        => cut.WaitForAssertion(() =>
        {
            Assert.Contains(pfeil, cut.FindAll(".epos-sortierpfeil")[3].TextContent);
            Assert.Equal(namen, Namen(cut));
        });

    private static string[] Namen(IRenderedComponent<Katalogliste> cut) =>
        cut.Instance.Angezeigt.Select(z => z.Bezeichner).ToArray();
}
