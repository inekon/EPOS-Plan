using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using EPOS.UI.Standards;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Der Kopf des Reiters „Gebäude und Hülle“, zweigeteilt und mit Herkunft</b> (Entwurf GD 2.1, Welle GD-1; F1, F2,
/// F6): Block „Beschreibung und Einordnung“ mit dem Hinweis, dass der Rechenweg ihn nicht liest; Block „Rechengrößen“
/// mit einer Herkunftsmarke je Feld aus der Auskunft des Kerns. Mit Zonen stehen Raumhöhe und Luftwechselrate nur,
/// wo eine Zone sie erbt; g und Bauweise als Rückfall. Dazu die Rundung der Anzeige (Nebenwunsch): Watt ohne
/// Nachkommastellen, der gespeicherte Wert bleibt.
///
/// <para>Die Kultur ist auf de-DE gepinnt: Die Erwartungswerte sind deutsche Beschriftungen und Zahlen.</para>
/// </summary>
public class GebaeudeKopfHerkunftTests : EposBunitContext
{
    public GebaeudeKopfHerkunftTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static GebaeudeKatalogDaten Satz() => new()
    {
        Name = "Haus A", Typ = "Einfamilienhaus", Beschreibung = "Beschreibung Haus A", Gebaeudeart = "Hotel",
        Verwendung = "Wohngebaeude", Baualtersklasse = 4, Bauart = 1, WohnflaecheGesamt = 150, FlaecheNutzer = 35,
        Waermegewinne = 400, Fensterdurchlassgrad = 0.4, Raumhoehe = 2.5, FensterflaecheNord = 10, FensterflaecheSued = 20,
        FensterflaecheOstWest = 15, FlaecheAussenwand = 200, Dachflaeche = 120, Grundflaeche = 100, SonstigeFlaechen = 5,
        UWertAussenwand = 0.3, UWertFenster = 1.3, UWertDachflaeche = 0.2, UWertGrundflaeche = 0.35, UWertSonstiges = 0.5,
        WbvkFensterWand = 0.1, AnschlussFensterWand = 50, SollTag = 20, NachtAbsenkung = 17, MaxTemperatur = 24,
        WochenendAbsenkung = 0, SollFerien = 0, Luftwechselrate = 0.5
    };

    /// <summary>Eine Zone mit einer Außenwand ohne Aufbau und einem Fenster ohne g.</summary>
    private static ZoneDaten Zone(int id, double flaeche, double? hoehe = null, double? infiltration = null)
        => new()
        {
            Id = id, Bezeichner = "Zone " + id, Nutzflaeche = flaeche, Raumhoehe = hoehe,
            LuftwechselInfiltration = infiltration,
            Bauteile =
            {
                new BauteilDaten { Id = id * 10 + 1, Bezeichner = "Wand", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
                                   Flaeche = 100, UWert = 0.3, Azimut = 180 },
                new BauteilDaten { Id = id * 10 + 2, Bezeichner = "Fenster", Bauteilart = DbWerte.BAUTEILART_FENSTER,
                                   Flaeche = 10, UWert = 1.1, Azimut = 180 }
            }
        };

    private IRenderedComponent<GebaeudeKatalogDialog> Aufbauen(IReadOnlyList<ZoneDaten>? zonen = null)
        => Render<GebaeudeKatalogDialog>(p => p
            .Add(x => x.Daten, Satz())
            .Add(x => x.Modus, GebaeudeKatalogModus.Bearbeiten)
            .Add(x => x.Gebaeudetypen, () => new[] { "Einfamilienhaus" })
            .Add(x => x.Gebaeudearten, () => new[] { "Hotel" })
            .Add(x => x.Baualtersklassen, new[] { "a", "b", "c", "d", "e" })
            .Add(x => x.Speichern, (_, _, _) => new GebaeudeKatalogErgebnis(true, ""))
            .Add(x => x.Zonen, new GebaeudeZonenweg
            {
                Katalog = true,
                Zonen = zonen ?? Array.Empty<ZoneDaten>(),
                Speichern = _ => ZonenSchreibergebnis.Gut,
            }));

    private static IReadOnlyList<string> Beschriftungen(IRenderedComponent<GebaeudeKatalogDialog> cut)
        => cut.FindAll(".epos-feld-text").Select(e => e.TextContent.Trim()).ToList();

    private static IElement Marke(IRenderedComponent<GebaeudeKatalogDialog> cut, string feld)
        => cut.Find($"p.epos-gebk-herkunft[data-feld={feld}]");

    private static IElement Gruppe(IRenderedComponent<GebaeudeKatalogDialog> cut, string titel)
        => cut.FindAll(".epos-gruppe, section, fieldset, details").First(e => e.TextContent.TrimStart().StartsWith(titel, StringComparison.Ordinal));

    [Fact]
    public void Feldbestand_einzonig_zweigeteilt_mit_Marken()
    {
        var cut = Aufbauen();

        // Beide Blöcke, die Beschreibung zuerst; der Hinweis sagt, wofür sie dient.
        int einordnung = cut.Markup.IndexOf("Beschreibung und Einordnung", StringComparison.Ordinal);
        int rechen = cut.Markup.IndexOf("Rechengrößen", StringComparison.Ordinal);
        Assert.True(einordnung >= 0 && rechen > einordnung);
        IElement hinweis = cut.Find("p.epos-gebk-einordnung");
        Assert.Contains("Der Rechenweg liest sie nicht.", hinweis.TextContent);
        Assert.Contains("wirkungslos", hinweis.QuerySelector(".epos-gebk-marke--wirkungslos")!.TextContent);

        // Die Felder der Einordnung stehen vor dem Hinweis, die Rechengrößen danach.
        string markup = cut.Markup;
        int h = markup.IndexOf("epos-gebk-einordnung", StringComparison.Ordinal);
        foreach (string f in new[] { "Gebäudeart :", "Baualtersklasse :", "Baujahr :", "Verwendung :", "Energiestandard :" })
            Assert.InRange(markup.IndexOf(f, StringComparison.Ordinal), 0, h);
        foreach (string f in new[] { "Bauart :", "Nutzfläche :", "Fläche / Nutzer :", "Raumhöhe :", "Luftwechselrate :" })
            Assert.True(markup.IndexOf(f, StringComparison.Ordinal) > h, f);

        // Je Rechengröße eine Marke; einzonig wirkt alles von Hand.
        foreach (string feld in new[] { "Bauweise", "Nutzflaeche", "FlaecheJeNutzer", "Fensterdurchlassgrad", "Raumhoehe", "Luftwechselrate" })
        {
            IElement m = Marke(cut, feld);
            Assert.Equal("Hand", m.GetAttribute("data-herkunft"));
            Assert.Equal("von Hand", m.QuerySelector(".epos-gebk-marke")!.TextContent.Trim());
        }
        Assert.Empty(cut.FindAll("p.epos-gebk-jezone"));
    }

    [Fact]
    public void Feldbestand_mehrzonig_eigene_Werte_blenden_Raumhoehe_und_Luftwechsel_aus()
    {
        var cut = Aufbauen(new[] { Zone(1, 90, hoehe: 3.0, infiltration: 0.2), Zone(2, 60, hoehe: 2.8, infiltration: 0.2) });

        IReadOnlyList<string> felder = Beschriftungen(cut);
        Assert.DoesNotContain("Raumhöhe :", felder);
        Assert.DoesNotContain("Luftwechselrate :", felder);
        Assert.Contains("Fensterdurchlaßgrad :", felder);
        Assert.Contains(cut.FindAll("p.epos-gebk-jezone"), p => p.GetAttribute("data-feld") == "Raumhoehe");
        Assert.Contains(cut.FindAll("p.epos-gebk-jezone"), p => p.GetAttribute("data-feld") == "Luftwechselrate");

        // g und Bauweise stehen als Rückfall: zwei Fenster ohne g, je Zone Außen- und Innengruppe ohne Aufbau.
        IElement g = Marke(cut, "Fensterdurchlassgrad");
        Assert.Equal("Geerbt", g.GetAttribute("data-herkunft"));
        Assert.Contains("Rückfall für 2 Fenster ohne eigenes g", g.TextContent);
        Assert.Contains("Rückfall für 4 Bauteilgruppen ohne Aufbau", Marke(cut, "Bauweise").TextContent);
    }

    [Fact]
    public void Feldbestand_mehrzonig_erbende_Zonen_zeigen_Vorgabe_fuer_n_Zonen()
    {
        var cut = Aufbauen(new[] { Zone(1, 90, hoehe: 3.0), Zone(2, 60) });

        IReadOnlyList<string> felder = Beschriftungen(cut);
        Assert.Contains("Raumhöhe :", felder);
        Assert.Contains("Luftwechselrate :", felder);
        IElement hoehe = Marke(cut, "Raumhoehe");
        Assert.Equal("Geerbt", hoehe.GetAttribute("data-herkunft"));
        Assert.Contains("Vorgabe für 1 von 2 Zonen ohne eigene Höhe", hoehe.TextContent);
        Assert.Contains("Vorgabe für 2 von 2 Zonen ohne Lüftungswerte", Marke(cut, "Luftwechselrate").TextContent);
        // Die Herleitung des Luftwechsels auf Gebäudeebene gilt nur ohne Zonen.
        Assert.DoesNotContain("VDI 6007 rechnet mit", cut.Markup);
    }

    // =================================================================================
    // Rundung der Anzeige (Nebenwunsch)
    // =================================================================================

    [Fact]
    public void Rundung_der_Anzeige_Watt_ohne_Nachkommastellen()
    {
        Assert.Equal(0, KonditionierungMatrix.Stellen(KonditionierungGroesse.Geraete, KonditionierungZeile.Nennwert));
        Assert.Equal(0, KonditionierungMatrix.Stellen(KonditionierungGroesse.Personen, KonditionierungZeile.Nennwert));
        Assert.Null(KonditionierungMatrix.Stellen(KonditionierungGroesse.Lueftung, KonditionierungZeile.Nennwert));
        Assert.Null(KonditionierungMatrix.Stellen(KonditionierungGroesse.Geraete, KonditionierungZeile.Tag));

        // Das Feld zeigt gerundet, ohne Tausendertrennung (der Parser liest den Punkt als Dezimalzeichen).
        double? gemeldet = null;
        var feld = Render<Zahlenfeld>(p => p
            .Add(x => x.Wert, 3594.9644)
            .Add(x => x.Nachkommastellen, 0)
            .Add(x => x.WertChanged, w => gemeldet = w));
        Assert.Equal("3595", feld.Find("input").GetAttribute("value"));
        Assert.Null(gemeldet);

        // Die Textzeile der Jahresmittel: Watt mit Tausenderpunkt.
        Assert.Equal("3.595", Zahlen.Watt(3594.9644));
        Assert.Equal("—", Zahlen.Watt(null));
    }
}
