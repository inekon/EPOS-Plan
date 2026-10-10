using System.Linq;
using Bunit;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Auftrag KS: die KÄLTEBAHN im Baustein <see cref="Schema"/> — Trennlinie, Bahntitel und
/// vier Spaltenköpfe der Kälteseite, die Kästen mit der Kennzeichnung der Bahn, der Satz der
/// Kälte-Kaskade unter dem Band und der Legendeneintrag mit der Marke; ohne Kälte entsteht
/// keines dieser Elemente. Die Texte kommen als Daten herein, deshalb ohne Kulturpinnung.
/// </summary>
public class SchemaKaeltebahnTests : BunitContext
{
    public SchemaKaeltebahnTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static readonly string[] Leer = new string[0];

    private static SchemaBild Bild(bool kaelte)
    {
        var knoten = new System.Collections.Generic.List<SchemaKnoten>
        {
            new SchemaKnoten("ERZEUGER_1", SchemaKnotenart.Erzeuger, 224, 44, 214, 50,
                             "1", "Wärmepumpe", "Wärmepumpe", Leer, Leer, "", false, "", false, false),
            new SchemaKnoten("ABNEHMER_HEIZKREIS", SchemaKnotenart.Abnehmer, 740, 50, 132, 35,
                             "", "Heizkreis", "Heizkreis", Leer, Leer, "", false, "", false, false)
        };
        var kanten = new System.Collections.Generic.List<SchemaKante>
        {
            new SchemaKante("ERZEUGER_1", "ABNEHMER_HEIZKREIS", SchemaKantenart.Versorgung, 0,
                            "M438,69 L740,69", 590, 69)
        };
        var legende = new System.Collections.Generic.List<SchemaLegendeeintrag>
        {
            new SchemaLegendeeintrag("Ladung", SchemaKantenart.Ladung, false),
            new SchemaLegendeeintrag("Versorgung", SchemaKantenart.Versorgung, false)
        };

        if (kaelte)
        {
            knoten.Add(new SchemaKnoten("KQUELLE_7", SchemaKnotenart.Quelle, 18, 166, 150, 35,
                                        "", "Trockenkühler", "Trockenkühler", Leer, Leer,
                                        "Rückkühlung: Trockenkühler", false, "", false, false) { Kaelte = true });
            knoten.Add(new SchemaKnoten("KERZEUGER_7", SchemaKnotenart.Erzeuger, 224, 166, 214, 50,
                                        "", "Kältemaschine 10 kW", "Kältemaschine 10 kW",
                                        new[] { "Kältemaschine", "1 × 10 kW" }, Leer, "", false, "", false, false)
                                        { Kaelte = true });
            knoten.Add(new SchemaKnoten("KSPEICHER_9", SchemaKnotenart.Speicher, 494, 166, 190, 70,
                                        "", "Kaltwasserspeicher", "Kaltwasserspeicher",
                                        new[] { "2000 l", "6/12 °C" }, new[] { "Kälte" }, "", false, "", false, false)
                                        { Kaelte = true });
            knoten.Add(new SchemaKnoten("ABNEHMER_KAELTEKREIS", SchemaKnotenart.Abnehmer, 740, 180, 132, 35,
                                        "", "Kältekreis", "Kältekreis", Leer, Leer, "", false, "", false, false)
                                        { Kaelte = true });
            kanten.Add(new SchemaKante("KQUELLE_7", "KERZEUGER_7", SchemaKantenart.Quelle, 0,
                                       "M168,183 L224,183", 196, 183));
            kanten.Add(new SchemaKante("KERZEUGER_7", "KSPEICHER_9", SchemaKantenart.Ladung, 1,
                                       "M438,191 L494,191", 466, 191));
            kanten.Add(new SchemaKante("KSPEICHER_9", "ABNEHMER_KAELTEKREIS", SchemaKantenart.Versorgung, 0,
                                       "M684,198 L740,198", 712, 198));
            legende.Add(new SchemaLegendeeintrag("Kältebahn: …", SchemaKantenart.Versorgung, false)
            {
                Marke = true,
                MarkeText = "Kälte"
            });
        }

        var bild = new SchemaBild(
            Knoten: knoten, Kanten: kanten, Band: new SchemaBandglied[0], Legende: legende,
            Spaltenkoepfe: new[] { "Wärmequelle", "Erzeuger", "Speicher", "Abnehmer" },
            SpaltenX: new[] { 18, 224, 494, 740 },
            SpaltenBreite: new[] { 150, 214, 190, 132 },
            Breite: 890, Hoehe: kaelte ? 400 : 260, Rand: 18, KopfHoehe: 26,
            BandOben: kaelte ? 262 : 120, LegendeOben: kaelte ? 330 : 170,
            LinienBreite: 2, LinienBreiteHervor: 3, HatKaskade: false, IstLeer: false);

        return kaelte
            ? bild with
            {
                KaelteOben = 110,
                KaelteTitel = "Kälte",
                KaelteSpaltenkoepfe = new[] { "Rückkühlung / Quelle", "Kälteerzeuger", "Kältespeicher", "Abnehmer" },
                KaelteKetteOben = 300,
                KaelteKetteText = "Kälte-Kaskade: Kältemaschine 10 kW"
            }
            : bild;
    }

    [Fact]
    public void Mit_Kaelte_zeichnet_der_Baustein_die_Kaeltebahn()
    {
        var cut = Render<Schema>(p => p.Add(x => x.Layout, Bild(true)));

        // Trennlinie und Bahntitel.
        var trenner = cut.Find("line.epos-schema-bahntrenner");
        Assert.Equal("110", trenner.GetAttribute("y1"));
        Assert.Equal("Kälte", cut.Find("text.epos-schema-bahnkopf").TextContent);

        // Vier Spaltenköpfe oben, vier der Kältebahn darunter.
        Assert.Equal(8, cut.FindAll("text.epos-schema-spaltenkopf").Count);
        var unten = cut.FindAll("text.epos-schema-spaltenkopf--kaelte");
        Assert.Equal(new[] { "Rückkühlung / Quelle", "Kälteerzeuger", "Kältespeicher", "Abnehmer" },
                     unten.Select(t => t.TextContent).ToArray());

        // Die Kästen der Kältebahn tragen ihre Kennzeichnung, die der Wärme nicht.
        Assert.Equal(4, cut.FindAll("g.epos-schema-knoten--kaelte").Count);
        Assert.Equal(6, cut.FindAll("g.epos-schema-knoten").Count);

        // Dieselbe Kantensprache: Ladung mit Prioritätskreis, keine neue Kantenklasse.
        Assert.Equal(4, cut.FindAll("path.epos-schema-kante").Count);
        Assert.Single(cut.FindAll("circle.epos-schema-priokreis"));
        Assert.Contains("epos-schema--ladung",
                        cut.FindAll("path.epos-schema-kante").Single(k => k.GetAttribute("d") == "M438,191 L494,191")
                           .GetAttribute("class"));

        // Satz der Kälte-Kaskade und die Marke in der Legende.
        Assert.Equal("Kälte-Kaskade: Kältemaschine 10 kW", cut.Find("text.epos-schema-kaeltekette").TextContent);
        cut.Find("rect.epos-schema-legendemarke");
        Assert.Contains(cut.FindAll("text.epos-schema-legendetext"), t => t.TextContent == "Kältebahn: …");
        Assert.Equal(2, cut.FindAll("line.epos-schema-legendelinie").Count);
    }

    [Fact]
    public void Ohne_Kaelte_entsteht_kein_Element_der_Kaeltebahn()
    {
        var cut = Render<Schema>(p => p.Add(x => x.Layout, Bild(false)));

        Assert.Empty(cut.FindAll("line.epos-schema-bahntrenner"));
        Assert.Empty(cut.FindAll("text.epos-schema-bahnkopf"));
        Assert.Empty(cut.FindAll("text.epos-schema-kaeltekette"));
        Assert.Empty(cut.FindAll("rect.epos-schema-legendemarke"));
        Assert.Empty(cut.FindAll("g.epos-schema-knoten--kaelte"));
        Assert.Equal(4, cut.FindAll("text.epos-schema-spaltenkopf").Count);
    }

    [Fact]
    public void Ein_Klick_auf_den_Kaeltespeicher_meldet_seinen_Schluessel()
    {
        string gemeldet = "";
        var cut = Render<Schema>(p => p
            .Add(x => x.Layout, Bild(true))
            .Add(x => x.SichtbarMachen, false)
            .Add(x => x.Ausgewaehlt, s => gemeldet = s));

        cut.FindAll("g.epos-schema-knoten--kaelte")
           .Single(g => g.GetAttribute("aria-label") == "Kaltwasserspeicher")
           .Click();

        Assert.Equal("KSPEICHER_9", gemeldet);
    }

    // ================================================================== KS-2: Doppelklick

    /// <summary>KS-2: Der Wirt nennt je Schlüssel den Weg — hier wie die Simulationskonfiguration.</summary>
    private static string? Wege(string schluessel) => schluessel switch
    {
        "KQUELLE_7" => "Doppelklick öffnet die Kältemaschinen des Projekts; dort steht die Rückkühlart.",
        "KERZEUGER_7" => "Doppelklick öffnet die Kältemaschinen des Projekts.",
        "KSPEICHER_9" => "Doppelklick öffnet die Pufferverwaltung.",
        "ERZEUGER_1" => "",
        _ => null
    };

    private IRenderedComponent<Schema> MitWegen(System.Action<string> bearbeitet, string gewaehlt = "")
        => Render<Schema>(p => p
            .Add(x => x.Layout, Bild(true))
            .Add(x => x.SichtbarMachen, false)
            .Add(x => x.Gewaehlt, gewaehlt)
            .Add(x => x.Editorhinweis, Wege)
            .Add(x => x.BearbeitenGewuenscht, bearbeitet));

    private static AngleSharp.Dom.IElement Kasten(IRenderedComponent<Schema> cut, string titel)
        => cut.FindAll("g.epos-schema-knoten").Single(g => g.GetAttribute("aria-label") == titel);

    /// <summary>
    /// KS-2: Kältemaschine, Rückkühlung und Kältespeicher melden ihren Doppelklick mit IHREM
    /// Schlüssel — der Wirt entscheidet daraus über den Dialog.
    /// </summary>
    [Fact]
    public void Der_Doppelklick_auf_die_Kaeltebahn_meldet_den_Schluessel_des_Elements()
    {
        var gemeldet = new System.Collections.Generic.List<string>();
        var cut = MitWegen(gemeldet.Add);

        Kasten(cut, "Kältemaschine 10 kW").DoubleClick();
        Kasten(cut, "Trockenkühler").DoubleClick();
        Kasten(cut, "Kaltwasserspeicher").DoubleClick();

        Assert.Equal(new[] { "KERZEUGER_7", "KQUELLE_7", "KSPEICHER_9" }, gemeldet);
    }

    /// <summary>
    /// KS-2: Ein Element ohne Editor (der Kältekreis, der Heizkreis) trägt KEINEN Doppelklick —
    /// statt eines Handlers, der nichts tut; die Elemente mit Editor tragen ihn.
    /// </summary>
    [Fact]
    public void Ein_Element_ohne_Editor_traegt_keinen_Doppelklick()
    {
        var cut = MitWegen(_ => { });

        Assert.False(Kasten(cut, "Kältekreis").HasAttribute("blazor:ondblclick"));
        Assert.False(Kasten(cut, "Heizkreis").HasAttribute("blazor:ondblclick"));
        Assert.True(Kasten(cut, "Kältemaschine 10 kW").HasAttribute("blazor:ondblclick"));
        Assert.True(Kasten(cut, "Trockenkühler").HasAttribute("blazor:ondblclick"));
        Assert.True(Kasten(cut, "Wärmepumpe").HasAttribute("blazor:ondblclick"));
    }

    /// <summary>
    /// KS-2: Ohne den Delegat bleibt alles wie vor KS-2 — jedes Element trägt den Doppelklick.
    /// </summary>
    [Fact]
    public void Ohne_Editorhinweis_traegt_jedes_Element_den_Doppelklick()
    {
        var cut = Render<Schema>(p => p.Add(x => x.Layout, Bild(true)).Add(x => x.SichtbarMachen, false));

        Assert.All(cut.FindAll("g.epos-schema-knoten"), g => Assert.True(g.HasAttribute("blazor:ondblclick")));
    }

    /// <summary>KS-2: Der Tooltipp nennt den Weg als letzte Zeile; ein Element ohne Editor nennt keinen.</summary>
    [Fact]
    public void Der_Tooltipp_nennt_den_Weg_des_Doppelklicks()
    {
        var cut = MitWegen(_ => { });

        string km = Kasten(cut, "Kältemaschine 10 kW").QuerySelector("title")!.TextContent;
        Assert.EndsWith("Doppelklick öffnet die Kältemaschinen des Projekts.", km);

        string rk = Kasten(cut, "Trockenkühler").QuerySelector("title")!.TextContent;
        Assert.StartsWith("Trockenkühler\nRückkühlung: Trockenkühler\n", rk);
        Assert.EndsWith("dort steht die Rückkühlart.", rk);

        Assert.DoesNotContain("Doppelklick", Kasten(cut, "Kältekreis").QuerySelector("title")!.TextContent);
        Assert.DoesNotContain("Doppelklick", Kasten(cut, "Wärmepumpe").QuerySelector("title")!.TextContent);
    }

    /// <summary>
    /// KS-2: Die Eingabetaste auf dem GEWÄHLTEN Element ist der Tastaturweg zum Doppelklick —
    /// auf einem Element ohne Editor öffnet sie nichts.
    /// </summary>
    [Fact]
    public void Die_Eingabetaste_oeffnet_nur_ein_Element_mit_Editor()
    {
        var gemeldet = new System.Collections.Generic.List<string>();

        var ohne = MitWegen(gemeldet.Add, gewaehlt: "ABNEHMER_KAELTEKREIS");
        Kasten(ohne, "Kältekreis").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });
        Assert.Empty(gemeldet);

        var mit = MitWegen(gemeldet.Add, gewaehlt: "KERZEUGER_7");
        Kasten(mit, "Kältemaschine 10 kW").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(new[] { "KERZEUGER_7" }, gemeldet);
    }
}
