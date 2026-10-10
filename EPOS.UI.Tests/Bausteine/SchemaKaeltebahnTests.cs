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
}
