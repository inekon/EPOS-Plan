using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>„Aufheizzeit manuell (h)" im Reiter „Konditionierung"</b> (Entwurf KP3, Welle O2; E59, Festlegungen 37, 38, 40):
/// Vorschlag übernehmen, innerhalb und außerhalb der Spanne (Hinweis, gespeichert wird trotzdem), leer = NULL, harte
/// Grenze 1–47, ohne Schalter, unerreichbar, Zonen erben, Lesemodus; im Gebäudeeditor nur in der Betriebsart Projekt,
/// geschrieben im OK-Weg. Runde Phantasiewerte.
/// </summary>
public sealed class AufheizzeitManuellFeldTests : EposBunitContext
{
    private readonly Kulturvorrichtung _kultur = new();

    public AufheizzeitManuellFeldTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _kultur.Dispose();
        base.Dispose(disposing);
    }

    private static readonly AufheizzeitManuellDaten VORSCHLAG = new()
    {
        SchalterAn = true, BemessenH = 6, VonH = 6, BisH = 12, Tau2H = 5.0,
    };

    private IRenderedComponent<AufheizzeitManuellFeld> Zeichne(int? wert, AufheizzeitManuellDaten? vorschlag,
                                                                List<int?>? gesetzt = null, IReadOnlyList<string>? zonen = null,
                                                                bool gesperrt = false, List<bool>? fehler = null)
        => Render<AufheizzeitManuellFeld>(p => p
            .Add(x => x.Wert, wert)
            .Add(x => x.Vorschlag, vorschlag)
            .Add(x => x.Zonen, zonen ?? Array.Empty<string>())
            .Add(x => x.Gesperrt, gesperrt)
            .Add(x => x.WertChanged, w => gesetzt?.Add(w))
            .Add(x => x.FehlerZustand, e => fehler?.Add(e.Fehlerhaft)));

    [Fact]
    public void Die_bemessene_Zeit_wird_uebernommen()
    {
        var gesetzt = new List<int?>();
        var cut = Zeichne(null, VORSCHLAG, gesetzt);
        Assert.Contains("bemessen: 6 h", cut.Find(".epos-aufheizzeit-bemessen").TextContent);
        Assert.Contains("sinnvolle Spanne: 6–12 h (aus der Zeitkonstante τ₂ = 5,0 h)", cut.Markup);
        Assert.Contains("Leer: Es gilt die Art der Projekteinstellung", cut.Markup);
        cut.Find("button.epos-aufheizzeit-uebernehmen").Click();
        Assert.Equal(new int?[] { 6 }, gesetzt);
        Assert.Equal("6", cut.Find(".epos-aufheizzeit-manuell").GetAttribute("data-wert"));
    }

    [Fact]
    public void Innerhalb_der_Spanne_kein_Hinweis_ausserhalb_ein_Hinweis_und_der_Wert_bleibt()
    {
        Assert.Empty(Zeichne(8, VORSCHLAG).FindAll(".epos-aufheizzeit-ausserhalb"));
        var gesetzt = new List<int?>();
        var cut = Zeichne(null, VORSCHLAG, gesetzt);
        cut.Find(".epos-aufheizzeit-manuell input").Input("20");
        Assert.Equal(new int?[] { 20 }, gesetzt);
        Assert.Equal("20 h liegt außerhalb der Spanne 6–12 h — der Wert wird trotzdem gespeichert.",
                     cut.Find(".epos-aufheizzeit-ausserhalb").TextContent);
    }

    [Fact]
    public void Leer_schreibt_NULL()
    {
        var gesetzt = new List<int?>();
        var cut = Zeichne(9, VORSCHLAG, gesetzt);
        cut.Find(".epos-aufheizzeit-manuell input").Input("");
        Assert.Equal(new int?[] { null }, gesetzt);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("48")]
    public void Ausserhalb_von_1_bis_47_faerbt_das_Feld_und_meldet_den_Fehler(string eingabe)
    {
        var fehler = new List<bool>();
        var cut = Zeichne(null, VORSCHLAG, fehler: fehler);
        cut.Find(".epos-aufheizzeit-manuell input").Input(eingabe);
        Assert.Contains("epos-fehleingabe", cut.Find(".epos-aufheizzeit-manuell input").ClassName);
        Assert.Contains(true, fehler);
    }

    [Fact]
    public void Ohne_Schalter_sagt_das_Feld_dass_es_erst_mit_der_Optimierung_wirkt()
    {
        var cut = Zeichne(6, new AufheizzeitManuellDaten { SchalterAn = false, BemessenH = 6, VonH = 6, BisH = 12, Tau2H = 5.0 });
        Assert.Contains("Wirkt erst mit eingeschalteter Aufheizoptimierung", cut.Markup);
        Assert.Single(cut.FindAll("button.epos-aufheizzeit-uebernehmen"));
    }

    [Fact]
    public void Unerreichbar_gibt_es_nur_die_Spanne_ab_einer_Stunde()
    {
        var cut = Zeichne(null, new AufheizzeitManuellDaten { SchalterAn = true, Unerreichbar = true, VonH = 1, BisH = 10, Tau2H = 4.0 });
        Assert.Empty(cut.FindAll("button.epos-aufheizzeit-uebernehmen"));
        Assert.Contains("sinnvolle Spanne: 1–10 h", cut.Markup);
        Assert.Contains("Die Bemessung ist unerreichbar (W1)", cut.Markup);
    }

    [Fact]
    public void Ohne_Bemessung_nennt_das_Feld_den_Grund_und_ohne_Gabe_steht_nur_das_Feld()
    {
        Assert.Contains("Ohne Bemessung kein Vorschlag", Zeichne(null, new AufheizzeitManuellDaten { SchalterAn = true }).Markup);
        var ohne = Zeichne(null, null);
        Assert.Single(ohne.FindAll(".epos-aufheizzeit-manuell input"));
        Assert.Empty(ohne.FindAll("button"));
        Assert.DoesNotContain("Spanne", ohne.Markup);
    }

    [Fact]
    public void Die_Zonen_zeigen_den_geerbten_Wert()
    {
        Assert.Contains("Die 2 Zonen erben diesen Wert: 6 h.", Zeichne(6, VORSCHLAG, zonen: new[] { "Zone 1", "Zone 2" }).Markup);
        Assert.Contains("Die 2 Zonen erben diesen Wert: Art des Projekts.", Zeichne(null, VORSCHLAG, zonen: new[] { "Zone 1", "Zone 2" }).Markup);
    }

    [Fact]
    public void Im_Lesemodus_ist_das_Feld_gesperrt_und_es_gibt_kein_Uebernehmen()
    {
        var cut = Zeichne(6, VORSCHLAG, gesperrt: true);
        Assert.Empty(cut.FindAll("button.epos-aufheizzeit-uebernehmen"));
        Assert.True(cut.Find(".epos-aufheizzeit-manuell input").HasAttribute("disabled"));
    }

    // =============================================================================
    //  Im Gebäudeeditor: nur im Projekt, geschrieben im OK-Weg
    // =============================================================================

    private IRenderedComponent<GebaeudeKatalogDialog> Editor(GebaeudeKatalogModus modus,
                                                             Func<GebaeudeKatalogDaten, bool, string, GebaeudeKatalogErgebnis> speichern,
                                                             int? wert = null)
    {
        var cut = Render<GebaeudeKatalogDialog>(p => p
            .Add(x => x.Daten, Satz(wert))
            .Add(x => x.Modus, modus)
            .Add(x => x.Aufheizzeit, VORSCHLAG)
            .Add(x => x.Speichern, speichern)
            .Add(x => x.Geschlossen, _ => { }));
        cut.FindAll("button[role=tab]").First(b => b.TextContent.Trim() == "Konditionierung").Click();
        return cut;
    }

    /// <summary>Ein vollständiger Satz mit runden Phantasiewerten, der die Prüfregeln des OK-Wegs hält.</summary>
    private static GebaeudeKatalogDaten Satz(int? wert) => new()
    {
        Name = "Haus A", Typ = "Einfamilienhaus", Gebaeudeart = "Hotel", Verwendung = "Wohngebaeude",
        Baualtersklasse = 4, Bauart = 1, WohnflaecheGesamt = 150, FlaecheNutzer = 35, Waermegewinne = 400,
        Fensterdurchlassgrad = 0.4, Raumhoehe = 2.5, FensterflaecheNord = 10, FensterflaecheSued = 20,
        FensterflaecheOstWest = 15, FlaecheAussenwand = 200, Dachflaeche = 120, Grundflaeche = 100, SonstigeFlaechen = 5,
        UWertAussenwand = 0.3, UWertFenster = 1.3, UWertDachflaeche = 0.2, UWertGrundflaeche = 0.35, UWertSonstiges = 0.5,
        WbvkFensterWand = 0.1, AnschlussFensterWand = 50, SollTag = 20, NachtAbsenkung = 17, MaxTemperatur = 24,
        WochenendAbsenkung = 0, SollFerien = 0, Luftwechselrate = 0.5, WwBedarf = 700, AufheizzeitManuellH = wert,
    };

    [Fact]
    public void Im_Katalogmodus_gibt_es_kein_Feld()
    {
        var cut = Editor(GebaeudeKatalogModus.Bearbeiten, (_, _, _) => new GebaeudeKatalogErgebnis(true, ""));
        Assert.Empty(cut.FindAll(".epos-aufheizzeit-manuell"));
    }

    [Fact]
    public void Im_Projekt_schreibt_erst_OK_den_Wert_der_Abbruch_nicht()
    {
        var geschrieben = new List<int?>();
        var cut = Editor(GebaeudeKatalogModus.Projekt, (d, istNeu, _) =>
        {
            if (!istNeu) geschrieben.Add(d.AufheizzeitManuellH);
            return new GebaeudeKatalogErgebnis(true, "");
        }, wert: 4);
        Assert.Equal("4", cut.Find(".epos-aufheizzeit-manuell").GetAttribute("data-wert"));
        cut.Find("button.epos-aufheizzeit-uebernehmen").Click();
        Assert.Empty(geschrieben);
        cut.Find(".epos-leiste button.epos-knopf--primaer").Click();
        Assert.Equal(new int?[] { 6 }, geschrieben);
    }
}
