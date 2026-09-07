using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
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
public class KataloglisteTests : BunitContext
{
    public KataloglisteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
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
        Assert.Equal("2 von 3 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);
        Assert.Equal(2, cut.FindAll("tbody tr").Count);

        cut.Find(".epos-katalog-suchzeile input").Input("vaillant erdgas");
        Assert.Equal("1 von 3 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);
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

        Assert.Equal("0 von 3 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);
        Assert.Single(cut.FindAll(".epos-katalog-leer"));
        Assert.Empty(cut.FindAll("tbody tr"));
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

        cut.Find(".epos-katalog-suchzeile input").Input("vaillant");
        Assert.Empty(cut.FindAll(".epos-katalog-ruecksetzer"));

        Filter(cut, Katalogfilterprofil.SpBrennstoff, "Gas");
        Assert.Single(cut.FindAll(".epos-katalog-ruecksetzer"));

        cut.Find(".epos-katalog-ruecksetzer").Click();
        Assert.Empty(cut.FindAll(".epos-katalog-ruecksetzer"));
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

        var gefuellt = cut.FindAll(".epos-trichter--gesetzt .epos-trichter-bild path");
        Assert.Single(gefuellt);
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
        Assert.Single(cut.FindAll(".epos-spaltenfilter"));
        Assert.Equal(Katalogfilterprofil.SpBrennstoff, cut.Instance.OffenesPopover);

        cut.FindAll(".epos-trichter")[3].Click();          // P_th
        Assert.Single(cut.FindAll(".epos-spaltenfilter"));
        Assert.Equal(Katalogfilterprofil.SpPtherm, cut.Instance.OffenesPopover);

        cut.FindAll(".epos-trichter")[3].Click();          // derselbe -> zu
        Assert.Empty(cut.FindAll(".epos-spaltenfilter"));
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
        Assert.Equal("enthält…", cut.Find(".epos-spaltenfilter input").GetAttribute("placeholder"));
        Assert.Empty(cut.FindAll(".epos-spaltenfilter-hinweis"));

        cut.FindAll(".epos-trichter")[3].Click();          // P_th (Zahl)
        Assert.Contains("10..60", cut.Find(".epos-spaltenfilter input").GetAttribute("placeholder"));
        Assert.Single(cut.FindAll(".epos-spaltenfilter-hinweis"));
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

        Assert.Equal("2 von 3 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);
        Assert.Equal(2, cut.FindAll("tbody tr").Count);

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
        Assert.Equal("2 von 3 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);

        Filter(cut, Katalogfilterprofil.SpEta, ">=0,975");
        Assert.Equal("1 von 3 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);
        Assert.Contains("Gamma", cut.Find("tbody").TextContent);

        // Unverstanden = kein Filter: die Zeilen bleiben, wie sie waren.
        Filter(cut, Katalogfilterprofil.SpEta, ">=");
        Assert.Equal("2 von 3 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);
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
        Assert.Contains("▲", cut.FindAll(".epos-sortierpfeil")[3].TextContent);
        Assert.Equal(new[] { "Alpha", "Gamma", "Beta" }, Namen(cut));

        cut.FindAll(".epos-spaltenkopf-titel")[3].Click();          // ab
        Assert.Contains("▼", cut.FindAll(".epos-sortierpfeil")[3].TextContent);
        Assert.Equal(new[] { "Beta", "Gamma", "Alpha" }, Namen(cut));

        cut.FindAll(".epos-spaltenkopf-titel")[3].Click();          // aus
        Assert.Contains("⇅", cut.FindAll(".epos-sortierpfeil")[3].TextContent);
        Assert.Equal(new[] { "Alpha", "Beta", "Gamma" }, Namen(cut));
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
        Assert.Equal("Beta", gewaehlt);

        cut.Render(p => p
            .Add(x => x.Profil, Profil())
            .Add(x => x.Zeilen, Zeilen())
            .Add(x => x.Filterstand, stand)
            .Add(x => x.Gewaehlt, gewaehlt));

        // "Gas" blendet Beta (Heizoel) aus - die Auswahl des Wirtes bleibt.
        Filter(cut, Katalogfilterprofil.SpBrennstoff, "Gas");
        Assert.Equal("Beta", gewaehlt);
        Assert.DoesNotContain("Beta", cut.Find("tbody").TextContent);

        // Und beim Zuruecknehmen steht sie wieder markiert da.
        cut.Find(".epos-katalog-ruecksetzer").Click();
        Assert.Contains("Beta", cut.Find("tbody").TextContent);
        Assert.Single(cut.FindAll("tbody button[aria-pressed=\"true\"]"));
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

        Assert.Equal("15 von 20.749 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);
        Assert.False(cut.Instance.Virtualisiert);

        // DIE ZEILEN SIND WIRKLICH DIE NEUEN - nicht der alte Zwischenspeicher.
        Assert.Equal(15, cut.FindAll("tbody tr").Count);
        Assert.DoesNotContain("Andere M-", cut.Find("tbody").TextContent);
        Assert.Contains("LONGi LR5-00000", cut.Find("tbody").TextContent);
    }

    // =====================================================================
    //  Hilfen
    // =====================================================================

    /// <summary>Öffnet den Trichter der Spalte, tippt den Ausdruck und übernimmt ihn.</summary>
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
        cut.Find(".epos-spaltenfilter input").Change(ausdruck);
    }

    private static string[] Namen(IRenderedComponent<Katalogliste> cut) =>
        cut.Instance.Angezeigt.Select(z => z.Bezeichner).ToArray();
}
