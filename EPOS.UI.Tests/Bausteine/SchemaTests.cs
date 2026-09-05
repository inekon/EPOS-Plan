using Bunit;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Der Baustein <see cref="Schema"/> (iU9-W10b.0c) — das Hydraulikschema als SVG,
/// der Ersatz fuer die GDI+-Zeichnung <c>Views/Simulation/SchemaAnsicht.cs</c>.
///
/// <para>Geprueft wird, was den Vorlaeufer ausmachte: vier Spaltenkoepfe, je Knoten ein
/// Kasten mit Rang, Titel, Zeilen, Badges und ggf. Warnzeile, je Kante ein Bezierbogen
/// mit Pfeilspitze (gestrichelt bei der Kaskade), der Prioritaetskreis auf der
/// Kurvenmitte, das Pillen-Band und die fuenf Legendeneintraege — dazu Klick,
/// Doppelklick und der Kurzhinweis.</para>
///
/// <para><c>JSRuntimeMode.Loose</c>, weil der gewaehlte Kasten ueber
/// <c>FocusAsync</c> in den Sichtbereich geholt wird (der Ersatz fuer
/// <c>SichtbarMachen</c>).</para>
/// </summary>
public class SchemaTests : BunitContext
{
    public SchemaTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // =====================================================================
    // Ein synthetisches Bild - zwei Erzeuger, eine Quelle, ein Speicher,
    // ein Abnehmer, vier Kanten, drei Bandglieder, fuenf Legendeneintraege.
    // =====================================================================

    private static SchemaBild Bild(bool mitWarnung = true, bool kaskade = true) => new SchemaBild(
        Knoten: new[]
        {
            new SchemaKnoten("QUELLE_1", SchemaKnotenart.Quelle, 18, 44, 150, 35,
                             "", "Außenluft", new string[0], new string[0],
                             "Quelle der Wärmepumpe", false, "", false, true),

            new SchemaKnoten("ERZEUGER_1", SchemaKnotenart.Erzeuger, 224, 44, 214, 65,
                             "1", "Wärmepumpe · WP 1", new[] { "55 / 45 °C", "Senke: Puffer A" },
                             new string[0], "Chips der Karte", false, "", false, false),

            new SchemaKnoten("ERZEUGER_2", SchemaKnotenart.Erzeuger, 224, 123, 214, 50,
                             "2", "Heizkessel · K1", new[] { "70 / 50 °C" }, new string[0],
                             "", mitWarnung, "Vorlauf unter dem Puffer-Sollwert", true, false),

            new SchemaKnoten("SPEICHER_10", SchemaKnotenart.Speicher, 494, 60, 190, 82,
                             "", "Puffer A", new[] { "800 l" }, new[] { "Heizung", "Warmwasser" },
                             "Detailzeilen der Karte", false, "", false, false),

            new SchemaKnoten("ABNEHMER_HEIZKREIS", SchemaKnotenart.Abnehmer, 740, 70, 132, 35,
                             "", "Heizkreis", new string[0], new string[0], "", false, "", false, false)
        },
        Kanten: new[]
        {
            // Streckenzuege in Spaltenbahnen (W10b-B-1) - waagerecht heraus, senkrecht
            // in der Gasse, waagerecht hinein; kein Bezierbogen mehr.
            new SchemaKante("QUELLE_1", "ERZEUGER_1", SchemaKantenart.Quelle, 0,
                            "M168,61 L196,61 L196,76 L224,76", 196, 68),
            new SchemaKante("ERZEUGER_1", "SPEICHER_10", SchemaKantenart.Ladung, 1,
                            "M438,76 L466,76 L466,101 L494,101", 466, 88),
            new SchemaKante("SPEICHER_10", "ABNEHMER_HEIZKREIS", SchemaKantenart.Versorgung, 0,
                            "M684,101 L712,101 L712,87 L740,87", 712, 94),
            new SchemaKante("SPEICHER_10", "ERZEUGER_2", SchemaKantenart.Kaskade, 0,
                            "M494,142 L466,142 L466,148 L438,148", 466, 145)
        },
        Band: new[]
        {
            new SchemaBandglied("ERZEUGER_1", "Wärmepumpe", SchemaKnotenart.Erzeuger,
                                SchemaKantenart.Quelle, 18, 240, 86, 22, true),
            new SchemaBandglied("SPEICHER_10", "Puffer A", SchemaKnotenart.Speicher,
                                SchemaKantenart.Ladung, 122, 240, 68, 22, false),
            new SchemaBandglied("ERZEUGER_2", "Heizkessel", SchemaKnotenart.Erzeuger,
                                SchemaKantenart.Kaskade, 208, 240, 80, 22, false)
        },
        Legende: new[]
        {
            new SchemaLegendeeintrag("Ladung", SchemaKantenart.Ladung, false),
            new SchemaLegendeeintrag("Versorgung", SchemaKantenart.Versorgung, false),
            new SchemaLegendeeintrag("Prozess", SchemaKantenart.Prozess, false),
            new SchemaLegendeeintrag("Quelle", SchemaKantenart.Quelle, false),
            new SchemaLegendeeintrag("Kaskade", SchemaKantenart.Kaskade, true)
        },
        Spaltenkoepfe: new[] { "Wärmequelle", "Erzeuger", "Speicher", "Abnehmer" },
        SpaltenX: new[] { 18, 224, 494, 740 },
        SpaltenBreite: new[] { 150, 214, 190, 132 },
        Breite: 890, Hoehe: 340, Rand: 18, KopfHoehe: 26,
        BandOben: 210, LegendeOben: 280,
        LinienBreite: 2, LinienBreiteHervor: 3, HatKaskade: kaskade, IstLeer: false);

    /// <summary>Die Grenzen aus <c>SchemaLayout.LINIE_BREITE_MIN/MAX</c>.</summary>
    private const double BREITE_MIN = 2;
    private const double BREITE_MAX = 6;

    // ================================================================== Aufbau

    [Fact]
    public void Vier_Spaltenkoepfe_stehen_ueber_ihren_Spalten()
    {
        var cut = Render<Schema>(p => p.Add(x => x.Layout, Bild()));

        var koepfe = cut.FindAll("text.epos-schema-spaltenkopf");
        Assert.Equal(4, koepfe.Count);
        Assert.Equal("Wärmequelle", koepfe[0].TextContent);
        Assert.Equal("Abnehmer", koepfe[3].TextContent);

        // Mitte der ersten Spalte: 18 + 150/2.
        Assert.Equal("93", koepfe[0].GetAttribute("x"));
    }

    [Fact]
    public void Je_Knoten_ein_Kasten_mit_Rang_Titel_und_Zeilen()
    {
        var cut = Render<Schema>(p => p.Add(x => x.Layout, Bild()));

        Assert.Equal(5, cut.FindAll("g.epos-schema-knoten").Count);
        Assert.Equal(5, cut.FindAll("rect.epos-schema-kasten").Count);

        var raenge = cut.FindAll("text.epos-schema-rang");
        Assert.Equal(2, raenge.Count);            // nur die beiden Erzeuger
        Assert.Equal("1", raenge[0].TextContent);

        var titel = cut.FindAll("text.epos-schema-titel");
        Assert.Equal(5, titel.Count);
        Assert.Contains(titel, t => t.TextContent == "Wärmepumpe · WP 1");

        // Drei Zusatzzeilen: zwei am Erzeuger 1, eine am Erzeuger 2, eine am Speicher.
        Assert.Equal(4, cut.FindAll("text.epos-schema-zeile").Count);

        // Badges nur am Speicher.
        Assert.Equal(2, cut.FindAll("rect.epos-schema-badge").Count);
    }

    /// <summary>
    /// Die Warnung ueberschreibt jede Rahmenfarbe und legt eine Warnzeile IN den
    /// Kasten (SchemaAnsicht:595 und :663-669).
    /// </summary>
    [Fact]
    public void Ein_Warnknoten_traegt_Warnklasse_und_Warnzeile()
    {
        var cut = Render<Schema>(p => p
            .Add(x => x.Layout, Bild())
            .Add(x => x.WarnungText, "Vorlauf unter dem Puffer-Sollwert"));

        var warn = cut.FindAll("g.epos-schema-knoten--warnung");
        Assert.Single(warn);

        Assert.Single(cut.FindAll("rect.epos-schema-warnflaeche"));
        Assert.Equal("Vorlauf unter dem Puffer-Sollwert",
                     cut.Find("text.epos-schema-warntext").TextContent);

        // OHNE Warnung bleibt beides weg.
        var ohne = Render<Schema>(p => p.Add(x => x.Layout, Bild(mitWarnung: false)));
        Assert.Empty(ohne.FindAll("g.epos-schema-knoten--warnung"));
        Assert.Empty(ohne.FindAll("rect.epos-schema-warnflaeche"));
    }

    // ================================================================== Kanten

    [Fact]
    public void Je_Kante_ein_Streckenzug_mit_Pfeilspitze_und_eigener_Farbklasse()
    {
        var cut = Render<Schema>(p => p.Add(x => x.Layout, Bild()));

        var kanten = cut.FindAll("path.epos-schema-kante");
        Assert.Equal(4, kanten.Count);

        Assert.All(kanten, k => Assert.StartsWith("url(#", k.GetAttribute("marker-end")));
        Assert.All(kanten, k => Assert.StartsWith("M", k.GetAttribute("d")));

        Assert.Single(cut.FindAll("path.epos-schema--ladung.epos-schema-kante"));
        Assert.Single(cut.FindAll("path.epos-schema--versorgung.epos-schema-kante"));
        Assert.Single(cut.FindAll("path.epos-schema--kaskade.epos-schema-kante"));
        Assert.Single(cut.FindAll("path.epos-schema--quelle.epos-schema-kante"));

        // Fuenf Pfeilspitzen, eine je Kantenart - und ihre Kennungen sind
        // instanzeigen (zwei Schemata auf einer Seite duerfen sich nicht stoeren).
        Assert.Equal(5, cut.FindAll("marker").Count);
    }

    /// <summary>
    /// <b>Anwenderbefund W10b-B-1, Punkt 1</b> (Windows-Abnahme 05.09.2026): „Die Pfeile
    /// sind teilweise zu dick und falsch dargestellt."
    ///
    /// <para>Eine Leitung ist eine LINIE gleichmaessiger, gedeckelter Breite mit EINER
    /// Pfeilspitze am Ende — keine Flaeche. Zwei Dinge machten daraus vorher eine:
    /// die Farbregel des Stilblatts setzte neben dem Strich auch die FUELLUNG (und
    /// schlug damit das <c>fill="none"</c> am Element), und die Spitze bemass sich mit
    /// <c>markerUnits="strokeWidth"</c> an der Strichstaerke.</para>
    /// </summary>
    [Fact]
    public void Jede_Leitung_ist_eine_Linie_gedeckelter_Breite_ohne_Flaeche()
    {
        var cut = Render<Schema>(p => p.Add(x => x.Layout, Bild()));

        var kanten = cut.FindAll("path.epos-schema-kante");
        Assert.Equal(4, kanten.Count);

        foreach (var k in kanten)
        {
            // Die Breite steht AM Element und liegt in den Grenzen des Kerns.
            double breite = double.Parse(k.GetAttribute("stroke-width")!,
                                         System.Globalization.CultureInfo.InvariantCulture);
            Assert.InRange(breite, BREITE_MIN, BREITE_MAX);

            // Keine Flaeche, EINE Spitze - und die sitzt am ENDE.
            Assert.Equal("none", k.GetAttribute("fill"));
            Assert.StartsWith("url(#", k.GetAttribute("marker-end"));
            Assert.Null(k.GetAttribute("marker-start"));
            Assert.Null(k.GetAttribute("marker-mid"));

            // Ein Streckenzug aus geraden Stuecken, kein Bezierbogen.
            string d = k.GetAttribute("d")!;
            Assert.DoesNotContain("C", d);
            Assert.DoesNotContain("Q", d);
        }

        // Kein Vieleck traegt eine Verbindung - Flaechen gibt es nur in der Pfeilspitze.
        Assert.Empty(cut.FindAll("polygon"));

        // Die Spitze misst feste Nutzereinheiten, nicht Vielfache der Strichstaerke.
        foreach (var m in cut.FindAll("marker"))
            Assert.Equal("userSpaceOnUse", m.GetAttribute("markerUnits"));
    }

    [Fact]
    public void Die_Kaskade_ist_gestrichelt_und_traegt_ihre_eigene_Klasse()
    {
        var cut = Render<Schema>(p => p.Add(x => x.Layout, Bild()));

        // Die Strichelung steht im Stilblatt an genau dieser Klassenpaarung.
        Assert.Single(cut.FindAll("path.epos-schema-kante.epos-schema--kaskade"));

        string blatt = System.IO.File.ReadAllText(Stilblatt());
        Assert.Contains(".epos-schema-kante.epos-schema--kaskade { stroke-dasharray:", blatt);

        // Und die Farbregeln setzen KEINE Flaeche mehr an der Leitung (W10b-B-1).
        Assert.DoesNotContain(".epos-schema--versorgung { stroke: var(--epos-schema-versorgung); fill:",
                              blatt);
        Assert.Contains(".epos-schema-pfeil.epos-schema--versorgung { fill:", blatt);
    }

    /// <summary>Das Stilblatt der Bibliothek — vom Testlaufordner aus aufwaerts gesucht.</summary>
    private static string Stilblatt()
    {
        var d = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
        for (int i = 0; i < 8 && d is not null; i++, d = d.Parent)
        {
            string kandidat = System.IO.Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css");
            if (System.IO.File.Exists(kandidat)) return kandidat;
        }

        Assert.Fail("epos-ui.css nicht gefunden");
        return "";
    }

    /// <summary>
    /// <b>W10b-B-1, Punkt 3.</b> Der Satz „Keine Kaskade im Projekt" haengt an der
    /// TATSACHE des Modells, nicht am leeren Band: Im Bildschirmfoto trugen zwei
    /// Erzeugerkaesten „Quelle: Puffer 3000Ltr · Kaskade", und darunter stand, es gebe
    /// keine Kaskade.
    /// </summary>
    [Fact]
    public void Ohne_Kette_aber_MIT_Kaskade_steht_kein_Widerspruch_im_Bild()
    {
        SchemaBild mitKaskade = Bild(kaskade: true) with { Band = new SchemaBandglied[0] };

        var cut = Render<Schema>(p => p
            .Add(x => x.Layout, mitKaskade)
            .Add(x => x.KeineKetteText, "Keine Kaskade im Projekt"));

        Assert.DoesNotContain("Keine Kaskade im Projekt", cut.Markup);

        // Ohne Kaskade steht der Satz sehr wohl da.
        SchemaBild ohne = Bild(kaskade: false) with { Band = new SchemaBandglied[0] };

        var leer = Render<Schema>(p => p
            .Add(x => x.Layout, ohne)
            .Add(x => x.KeineKetteText, "Keine Kaskade im Projekt"));

        Assert.Contains("Keine Kaskade im Projekt", leer.Markup);
    }

    [Fact]
    public void Nur_eine_Kante_mit_Prioritaet_traegt_den_Kreis()
    {
        var cut = Render<Schema>(p => p.Add(x => x.Layout, Bild()));

        var kreise = cut.FindAll("circle.epos-schema-priokreis");
        Assert.Single(kreise);
        Assert.Equal("466", kreise[0].GetAttribute("cx"));
        Assert.Equal("9", kreise[0].GetAttribute("r"));

        Assert.Equal("1", cut.Find("text.epos-schema-prioziffer").TextContent);
    }

    // ================================================================== Band und Legende

    [Fact]
    public void Das_Kaskadenband_zeigt_Pillen_und_Pfeile_dazwischen()
    {
        var cut = Render<Schema>(p => p
            .Add(x => x.Layout, Bild())
            .Add(x => x.KetteKopfText, "Kaskadenkette"));

        Assert.Equal("Kaskadenkette", cut.Find("text.epos-schema-bandkopf").TextContent);
        Assert.Equal(3, cut.FindAll("rect.epos-schema-pille").Count);

        // Zwei Pfeile: vor dem zweiten und vor dem dritten Glied.
        Assert.Equal(2, cut.FindAll("line.epos-schema-bandpfeil").Count);

        // Die Pille des Speichers traegt die Speicherfarbe.
        Assert.Single(cut.FindAll("g.epos-schema-band--speicher"));
    }

    [Fact]
    public void Die_Legende_hat_fuenf_Eintraege_und_eine_gestrichelte_Linie()
    {
        var cut = Render<Schema>(p => p.Add(x => x.Layout, Bild()));

        Assert.Equal(5, cut.FindAll("line.epos-schema-legendelinie").Count);
        Assert.Equal(5, cut.FindAll("text.epos-schema-legendetext").Count);
        Assert.Single(cut.FindAll("line.epos-schema-legendelinie--gestrichelt"));
    }

    // ================================================================== Auswahl

    [Fact]
    public void Der_gewaehlte_Knoten_traegt_Halo_und_Auswahlklasse()
    {
        var cut = Render<Schema>(p => p
            .Add(x => x.Layout, Bild())
            .Add(x => x.Gewaehlt, "SPEICHER_10")
            .Add(x => x.SichtbarMachen, false));

        var gewaehlt = cut.FindAll("g.epos-schema-knoten--gewaehlt");
        Assert.Single(gewaehlt);
        Assert.Equal("true", gewaehlt[0].GetAttribute("aria-pressed"));
        Assert.Single(cut.FindAll("rect.epos-schema-halo"));

        // Beide Kanten AM gewaehlten Speicher sind hervorgehoben.
        Assert.Equal(3, cut.FindAll("path.epos-schema-kante--hervor").Count);
    }

    // ================================================================== Bedienung

    [Fact]
    public void Klick_meldet_die_Auswahl_Doppelklick_den_Editorwunsch()
    {
        string gewaehlt = "";
        string bearbeitet = "";

        var cut = Render<Schema>(p => p
            .Add(x => x.Layout, Bild())
            .Add(x => x.SichtbarMachen, false)
            .Add(x => x.Ausgewaehlt, (string s) => gewaehlt = s)
            .Add(x => x.BearbeitenGewuenscht, (string s) => bearbeitet = s));

        // Nach jedem Ereignis neu suchen: Der Rueckruf zeichnet die Komponente neu,
        // und ein vorher gemerktes Element traegt dann keinen Ereignisverweis mehr.
        cut.FindAll("g.epos-schema-knoten")[1].Click();
        Assert.Equal("ERZEUGER_1", gewaehlt);

        cut.FindAll("g.epos-schema-knoten")[3].DoubleClick();
        Assert.Equal("SPEICHER_10", bearbeitet);
    }

    /// <summary>
    /// Auch ein BANDGLIED meldet Auswahl und Editorwunsch — im Vorlaeufer waren die
    /// Bandflaechen Teil desselben Hit-Tests (Treffer:167-168).
    /// </summary>
    [Fact]
    public void Ein_Bandglied_meldet_dieselben_zwei_Ereignisse()
    {
        string gewaehlt = "";
        string bearbeitet = "";

        var cut = Render<Schema>(p => p
            .Add(x => x.Layout, Bild())
            .Add(x => x.SichtbarMachen, false)
            .Add(x => x.Ausgewaehlt, (string s) => gewaehlt = s)
            .Add(x => x.BearbeitenGewuenscht, (string s) => bearbeitet = s));

        cut.FindAll("g.epos-schema-band")[2].Click();
        Assert.Equal("ERZEUGER_2", gewaehlt);

        cut.FindAll("g.epos-schema-band")[0].DoubleClick();
        Assert.Equal("ERZEUGER_1", bearbeitet);
    }

    /// <summary>
    /// Der Tastaturweg: Eingabe waehlt, Eingabe auf dem BEREITS gewaehlten Element
    /// oeffnet den Editor. Der Vorlaeufer war ein reines Mausziel.
    /// </summary>
    [Fact]
    public void Die_Eingabetaste_waehlt_und_oeffnet()
    {
        string gewaehlt = "";
        string bearbeitet = "";

        var cut = Render<Schema>(p => p
            .Add(x => x.Layout, Bild())
            .Add(x => x.Gewaehlt, "ERZEUGER_1")
            .Add(x => x.SichtbarMachen, false)
            .Add(x => x.Ausgewaehlt, (string s) => gewaehlt = s)
            .Add(x => x.BearbeitenGewuenscht, (string s) => bearbeitet = s));

        cut.FindAll("g.epos-schema-knoten")[3]
           .KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });
        Assert.Equal("SPEICHER_10", gewaehlt);

        cut.FindAll("g.epos-schema-knoten")[1]
           .KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });
        Assert.Equal("ERZEUGER_1", bearbeitet);
    }

    // ================================================================== Kurzhinweis

    /// <summary>
    /// Der Kurzhinweis ist Titel + Hinweis + (bei Warnung) Warntext — woertlich wie
    /// <c>OnMouseMove</c>:381-389, nur als <c>title</c> statt als ToolTip.
    /// </summary>
    [Fact]
    public void Jeder_Knoten_traegt_seinen_Kurzhinweis()
    {
        var cut = Render<Schema>(p => p.Add(x => x.Layout, Bild()));

        var titel = cut.FindAll("g.epos-schema-knoten title");
        Assert.Equal(5, titel.Count);

        Assert.Equal("Außenluft\nQuelle der Wärmepumpe", titel[0].TextContent);
        Assert.Contains("Vorlauf unter dem Puffer-Sollwert", titel[2].TextContent);
    }

    // ================================================================== Leerbild

    [Fact]
    public void Ohne_Layout_steht_der_Leertext()
    {
        var cut = Render<Schema>(p => p.Add(x => x.LeerText, "Noch keine Hydraulik."));

        Assert.Equal("Noch keine Hydraulik.", cut.Find("p.epos-schema-leer").TextContent);
        Assert.Empty(cut.FindAll("svg.epos-schema-bild"));
    }
}
