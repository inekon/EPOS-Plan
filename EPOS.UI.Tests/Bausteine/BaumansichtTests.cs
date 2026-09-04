using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Der Baustein <c>Baumansicht</c> (iU9-W14c.4) — der Ersatz für den EINZIGEN
/// <c>TreeView</c> des Bestands (<c>Form_KatalogDubletten._tree</c>).
///
/// <para>Der Baustein ist rein datengetrieben und ohne JS; er ist damit prüfbar wie
/// <c>Reiter</c> und <c>Zeilenwahl</c>. Die dreizehn Fälle hier sind die
/// Testliste aus der Vermessung § 11.4 g.</para>
///
/// <para><b>Der Prüfbaum ist der Dublettenfall:</b> zwei Kataloge, im ersten ein Ast
/// mit einer Gruppe und zwei Sätzen — vier Ebenen, Wurzel und Ast von vorn offen,
/// die Gruppe zu.</para>
/// </summary>
public class BaumansichtTests : BunitContext
{
    private static IReadOnlyList<Baumknoten> Baum() => new[]
    {
        new Baumknoten("K:WP", "Wärmepumpen (51 Sätze)", new[]
        {
            new Baumknoten("K:WP/N", "Namensdubletten (1 Gruppen)", new[]
            {
                new Baumknoten("K:WP/N/0", "Vaillant VKK 476", new[]
                {
                    Baumknoten.Blatt("K:WP/N/0/7", "ID 7 — Vaillant VKK 476"),
                    Baumknoten.Blatt("K:WP/N/0/9", "ID 9 — vaillant vkk 476", "[Auslieferung]")
                }, VonVornOffen: false)
            }, VonVornOffen: true)
        }, VonVornOffen: true),

        new Baumknoten("K:PV", "Photovoltaik (6 Sätze)", Array.Empty<Baumknoten>(),
                       VonVornOffen: true)
    };

    private IRenderedComponent<Baumansicht> Zeige(
        IReadOnlyList<Baumknoten>? baum = null,
        string? gewaehlt = null,
        Action<string?>? gewaehltChanged = null,
        string leerText = "Keine Dubletten gefunden.")
    {
        return Render<Baumansicht>(p => p
            .Add(x => x.Wurzeln, baum ?? Baum())
            .Add(x => x.Gewaehlt, gewaehlt)
            .Add(x => x.GewaehltChanged, gewaehltChanged ?? (_ => { }))
            .Add(x => x.Bezeichnung, "Dublettenbefund")
            .Add(x => x.LeerText, leerText));
    }

    private static IElement Knoten(IRenderedComponent<Baumansicht> cut, string schluessel)
        => cut.Find("li[data-schluessel=\"" + schluessel + "\"]");

    // =====================================================================
    //  1 — Rollen, Ebenen, Geschwister
    // =====================================================================

    [Fact]
    public void ZeichnetJedenKnotenMitEbeneUndRolle()
    {
        var cut = Zeige();

        Assert.Single(cut.FindAll("ul[role=tree]"));
        Assert.Equal("Dublettenbefund", cut.Find("ul[role=tree]").GetAttribute("aria-label"));

        // Wurzel und Ast sind offen, die Gruppe zu -> sichtbar sind
        // 2 Wurzeln + 1 Ast + 1 Gruppe = 4 treeitem.
        var knoten = cut.FindAll("li[role=treeitem]");
        Assert.Equal(4, knoten.Count);

        Assert.Equal("1", Knoten(cut, "K:WP").GetAttribute("aria-level"));
        Assert.Equal("2", Knoten(cut, "K:WP/N").GetAttribute("aria-level"));
        Assert.Equal("3", Knoten(cut, "K:WP/N/0").GetAttribute("aria-level"));

        // aria-setsize / aria-posinset je Ebene.
        Assert.Equal("2", Knoten(cut, "K:WP").GetAttribute("aria-setsize"));
        Assert.Equal("1", Knoten(cut, "K:WP").GetAttribute("aria-posinset"));
        Assert.Equal("2", Knoten(cut, "K:PV").GetAttribute("aria-posinset"));

        // Die vierte Ebene erscheint, sobald die Gruppe aufgeklappt ist.
        Knoten(cut, "K:WP/N/0").QuerySelector("button.epos-baum-schalter")!.Click();
        Assert.Equal("4", Knoten(cut, "K:WP/N/0/7").GetAttribute("aria-level"));
    }

    // =====================================================================
    //  2 — Ein Blatt traegt kein aria-expanded
    // =====================================================================

    [Fact]
    public void EinBlattTraegtKeinAriaExpanded()
    {
        var cut = Zeige();
        Knoten(cut, "K:WP/N/0").QuerySelector("button.epos-baum-schalter")!.Click();

        Assert.False(Knoten(cut, "K:WP/N/0/7").HasAttribute("aria-expanded"));
        Assert.True(Knoten(cut, "K:WP/N/0").HasAttribute("aria-expanded"));

        // Auch eine Wurzel OHNE Kinder ist ein Blatt im Sinne der Auszeichnung.
        Assert.False(Knoten(cut, "K:PV").HasAttribute("aria-expanded"));
        Assert.Empty(Knoten(cut, "K:PV").QuerySelectorAll("button.epos-baum-schalter"));
        Assert.Single(Knoten(cut, "K:PV").QuerySelectorAll("span.epos-baum-schalter--leer"));
    }

    // =====================================================================
    //  3 — Die Vorgabe kommt aus den Daten
    // =====================================================================

    /// <summary>
    /// Der Dublettenfall: Wurzel und Ast offen, die Gruppe zu — bitgleich zu
    /// <c>BaumFuellen</c> des Vorläufers (<c>wurzel.Expand</c> und
    /// <c>foreach (ast) ast.Expand</c>).
    /// </summary>
    [Fact]
    public void VonVornOffenBestimmtDenStartzustand()
    {
        var cut = Zeige();

        Assert.Equal("true", Knoten(cut, "K:WP").GetAttribute("aria-expanded"));
        Assert.Equal("true", Knoten(cut, "K:WP/N").GetAttribute("aria-expanded"));
        Assert.Equal("false", Knoten(cut, "K:WP/N/0").GetAttribute("aria-expanded"));

        // Die Blaetter sind damit noch nicht gezeichnet.
        Assert.Empty(cut.FindAll("li[data-schluessel=\"K:WP/N/0/7\"]"));
    }

    // =====================================================================
    //  4 — Der Schalter klappt um und waehlt NICHT
    // =====================================================================

    [Fact]
    public void DerSchalterKlapptUmUndWaehltNicht()
    {
        var gemeldet = new List<string?>();
        var cut = Zeige(gewaehltChanged: s => gemeldet.Add(s));

        Knoten(cut, "K:WP/N/0").QuerySelector("button.epos-baum-schalter")!.Click();

        Assert.Equal("true", Knoten(cut, "K:WP/N/0").GetAttribute("aria-expanded"));
        Assert.Empty(gemeldet);                                     // KEINE Auswahl
        Assert.Equal("false", Knoten(cut, "K:WP/N/0").GetAttribute("aria-selected"));

        // Noch einmal klappt wieder zu.
        Knoten(cut, "K:WP/N/0").QuerySelector("button.epos-baum-schalter")!.Click();
        Assert.Equal("false", Knoten(cut, "K:WP/N/0").GetAttribute("aria-expanded"));
        Assert.Empty(gemeldet);
    }

    // =====================================================================
    //  5 — Ein Klick auf den Text meldet den Schluessel
    // =====================================================================

    [Fact]
    public void EinKlickAufDenTextMeldetDenSchluessel()
    {
        var gemeldet = new List<string?>();
        var cut = Zeige(gewaehltChanged: s => gemeldet.Add(s));

        Knoten(cut, "K:WP/N").QuerySelector("span.epos-baum-text")!.Click();

        Assert.Equal(new[] { "K:WP/N" }, gemeldet);
        Assert.Equal("true", Knoten(cut, "K:WP/N").GetAttribute("aria-selected"));

        // Ein zweiter Klick auf denselben Knoten waehlt AB - der Wirt bekommt null
        // und schaltet seine Knoepfe zurueck.
        Knoten(cut, "K:WP/N").QuerySelector("span.epos-baum-text")!.Click();
        Assert.Equal(new string?[] { "K:WP/N", null }, gemeldet);
    }

    // =====================================================================
    //  6 — Roving tabindex
    // =====================================================================

    [Fact]
    public void NurEinKnotenStehtImTabulatorzyklus()
    {
        var cut = Zeige();

        Assert.Single(cut.FindAll("li[tabindex=\"0\"]"));
        Assert.Equal("K:WP", cut.Find("li[tabindex=\"0\"]").GetAttribute("data-schluessel"));

        // Nach der Auswahl wandert er mit.
        Knoten(cut, "K:WP/N").QuerySelector("span.epos-baum-text")!.Click();
        Assert.Single(cut.FindAll("li[tabindex=\"0\"]"));
        Assert.Equal("K:WP/N", cut.Find("li[tabindex=\"0\"]").GetAttribute("data-schluessel"));
    }

    // =====================================================================
    //  7-10 — Die Tastenkarte
    // =====================================================================

    private static void Taste(IRenderedComponent<Baumansicht> cut, string taste)
        => cut.Find("ul[role=tree]").KeyDown(new KeyboardEventArgs { Key = taste });

    [Fact]
    public void PfeilRechtsKlapptAufUndSteigtAb()
    {
        var cut = Zeige();

        // Der Fokus steht auf der Wurzel, sie ist OFFEN -> erstes Kind.
        Taste(cut, "ArrowRight");
        Assert.Equal("K:WP/N", cut.Find("li[tabindex=\"0\"]").GetAttribute("data-schluessel"));

        // Weiter zur Gruppe, die ist ZU -> aufklappen statt absteigen.
        Taste(cut, "ArrowRight");
        Assert.Equal("K:WP/N/0", cut.Find("li[tabindex=\"0\"]").GetAttribute("data-schluessel"));
        Taste(cut, "ArrowRight");
        Assert.Equal("true", Knoten(cut, "K:WP/N/0").GetAttribute("aria-expanded"));
        Assert.Equal("K:WP/N/0", cut.Find("li[tabindex=\"0\"]").GetAttribute("data-schluessel"));

        // Jetzt steigt sie ab.
        Taste(cut, "ArrowRight");
        Assert.Equal("K:WP/N/0/7", cut.Find("li[tabindex=\"0\"]").GetAttribute("data-schluessel"));

        // Am Blatt tut Pfeil rechts nichts.
        Taste(cut, "ArrowRight");
        Assert.Equal("K:WP/N/0/7", cut.Find("li[tabindex=\"0\"]").GetAttribute("data-schluessel"));
    }

    [Fact]
    public void PfeilLinksKlapptZuUndSteigtAuf()
    {
        var cut = Zeige();

        Taste(cut, "ArrowDown");            // Ast
        Assert.Equal("K:WP/N", cut.Find("li[tabindex=\"0\"]").GetAttribute("data-schluessel"));

        Taste(cut, "ArrowLeft");            // offen -> zuklappen
        Assert.Equal("false", Knoten(cut, "K:WP/N").GetAttribute("aria-expanded"));

        Taste(cut, "ArrowLeft");            // zu -> zum Elternknoten
        Assert.Equal("K:WP", cut.Find("li[tabindex=\"0\"]").GetAttribute("data-schluessel"));
    }

    [Fact]
    public void PfeilRunterUeberspringtZugeklappteKinder()
    {
        var cut = Zeige();

        // Sichtliste: K:WP, K:WP/N, K:WP/N/0, K:PV - die Blaetter der zugeklappten
        // Gruppe kommen NICHT vor.
        Taste(cut, "ArrowDown");
        Taste(cut, "ArrowDown");
        Assert.Equal("K:WP/N/0", cut.Find("li[tabindex=\"0\"]").GetAttribute("data-schluessel"));

        Taste(cut, "ArrowDown");
        Assert.Equal("K:PV", cut.Find("li[tabindex=\"0\"]").GetAttribute("data-schluessel"));

        // Am Ende bleibt er stehen.
        Taste(cut, "ArrowDown");
        Assert.Equal("K:PV", cut.Find("li[tabindex=\"0\"]").GetAttribute("data-schluessel"));

        Taste(cut, "ArrowUp");
        Assert.Equal("K:WP/N/0", cut.Find("li[tabindex=\"0\"]").GetAttribute("data-schluessel"));
    }

    [Fact]
    public void Pos1UndEndeSpringenAnDieEnden()
    {
        var cut = Zeige();

        Taste(cut, "End");
        Assert.Equal("K:PV", cut.Find("li[tabindex=\"0\"]").GetAttribute("data-schluessel"));

        Taste(cut, "Home");
        Assert.Equal("K:WP", cut.Find("li[tabindex=\"0\"]").GetAttribute("data-schluessel"));

        // Mit aufgeklappter Gruppe ist das letzte sichtbare Blatt NICHT das Ende -
        // K:PV steht danach.
        Knoten(cut, "K:WP/N/0").QuerySelector("button.epos-baum-schalter")!.Click();
        Taste(cut, "End");
        Assert.Equal("K:PV", cut.Find("li[tabindex=\"0\"]").GetAttribute("data-schluessel"));
    }

    // =====================================================================
    //  11 — Enter waehlt und loest KEINE Aktion aus
    // =====================================================================

    [Fact]
    public void EnterWaehltUndLoestKeineAktionAus()
    {
        var gemeldet = new List<string?>();
        var cut = Zeige(gewaehltChanged: s => gemeldet.Add(s));

        Taste(cut, "ArrowDown");
        Taste(cut, "Enter");

        Assert.Equal(new[] { "K:WP/N" }, gemeldet);       // GENAU EIN Rueckruf
        Assert.Equal("true", Knoten(cut, "K:WP/N").GetAttribute("aria-selected"));

        // Die Leertaste tut dasselbe (und waehlt hier wieder ab).
        Taste(cut, " ");
        Assert.Equal(new string?[] { "K:WP/N", null }, gemeldet);
    }

    // =====================================================================
    //  12 — Die Auswahl ueberlebt einen Neuaufbau
    // =====================================================================

    [Fact]
    public void DerGewaehlteKnotenBleibtNachEinemNeuaufbau()
    {
        var cut = Zeige(gewaehlt: "K:WP/N");
        Assert.Equal("true", Knoten(cut, "K:WP/N").GetAttribute("aria-selected"));

        // Ein Neuscan baut den Baum NEU auf - mit denselben Schluesseln.
        cut.Render(p => p.Add(x => x.Wurzeln, Baum()));

        Assert.Equal("true", Knoten(cut, "K:WP/N").GetAttribute("aria-selected"));
        Assert.Equal("true", Knoten(cut, "K:WP").GetAttribute("aria-expanded"));
    }

    // =====================================================================
    //  13 — Leertext und das getrennte Kennzeichen
    // =====================================================================

    [Fact]
    public void OhneWurzelnStehtDerLeertext()
    {
        var cut = Zeige(baum: Array.Empty<Baumknoten>());

        Assert.Empty(cut.FindAll("ul[role=tree]"));
        Assert.Equal("Keine Dubletten gefunden.", cut.Find("p.epos-baum-leer").TextContent.Trim());
    }

    [Fact]
    public void DasKennzeichenStehtGetrenntVomText()
    {
        var cut = Zeige();
        Knoten(cut, "K:WP/N/0").QuerySelector("button.epos-baum-schalter")!.Click();

        IElement mit = Knoten(cut, "K:WP/N/0/9");
        Assert.Equal("[Auslieferung]",
                     mit.QuerySelector("span.epos-baum-kennzeichen")!.TextContent.Trim());

        // Der Text selbst traegt es NICHT - zwei span, nicht eine verkettete Zeichenkette.
        Assert.DoesNotContain("[Auslieferung]",
            mit.QuerySelector("span.epos-baum-text")!.ChildNodes[0].TextContent);

        // Ein Satz ohne Kennzeichen bekommt auch keines.
        Assert.Empty(Knoten(cut, "K:WP/N/0/7").QuerySelectorAll("span.epos-baum-kennzeichen"));
    }
}
