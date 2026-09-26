using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Berichte;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>Die Sammlung der Prüfstände, die den statischen <see cref="Vorlagenfeldhalter"/> setzen.</summary>
[CollectionDefinition("Vorlagenfeldhalter", DisableParallelization = true)]
public sealed class VorlagenfeldhalterSammlung { }

/// <summary>Eine Zwischenablage, die mitschreibt (oder scheitert).</summary>
public sealed class TestZwischenablage : IZwischenablage
{
    public List<string> Texte { get; } = new();
    public bool Antwort { get; set; } = true;

    public Task<bool> TextSetzenAsync(string text)
    {
        Texte.Add(text);
        return Task.FromResult(Antwort);
    }
}

/// <summary>Die erfundenen Einträge der Prüfstände (Konzept 9.4) — je Art einer.</summary>
public static class Vorlagenfeldproben
{
    public static readonly Vorlagenfeldanzeige Kunde = new(
        "projekt.kunde", "Text", "Text", "Stamm", "Stamm", "Kunde des Stammprojekts. Mehr steht hier nicht.",
        Leerwert: "—", Excel: "EPOS.projekt.kunde");

    public static readonly Vorlagenfeldanzeige Jaz = new(
        "stamm.kennzahl.eff.jaz", "Zahl", "Zahl", "Stamm", "Stamm", "Jahresarbeitszahl der Wärmepumpe",
        Beispiel: "3,8", Einheit: "–", Leerwert: "—", Excel: "EPOS.stamm.kennzahl.eff.jaz");

    public static readonly Vorlagenfeldanzeige Tabelle = new(
        "tabelle.vergleich.effizienz", "Tabelle", "Tabelle", "Gruppe", "Gruppe", "Vergleich der Effizienz",
        Excel: "EPOS.tabelle.vergleich.effizienz");

    public static readonly Vorlagenfeldanzeige Bild = new(
        "bild.vergleich.balken.eff.jaz", "Bild", "Bild", "Gruppe", "Gruppe", "Balken der JAZ",
        Excel: "in Excel als Diagramm auf dem Tabellenbereich");

    public static readonly Vorlagenfeldanzeige Kapitel = new(
        "kapitel.wirtschaftlichkeit", "Kapitel", "Kapitel", "Bericht", "Bericht", "Das Kapitel Wirtschaftlichkeit");

    public static readonly Vorlagenfeldanzeige StandWert = new(
        "stand.kennzahl.eff.jaz", "Zahl", "Zahl", "je Stand", "Stand", "JAZ des Stands",
        Leerwert: "—", Excel: "in Excel nur als Listenzeile");

    public static readonly Vorlagenfeldanzeige StandBild = new(
        "stand.bild.deckung_waerme", "Bild", "Bild", "je Stand", "Stand", "Deckung Wärme",
        Excel: "in Excel als Diagramm auf dem Tabellenbereich");

    public static readonly Vorlagenfeldanzeige Schalter = new(
        "hat.tabelle.vergleich.effizienz", "Schalter", "Schalter", "Gruppe", "Gruppe", "Führt der Bericht die Tabelle?");

    public static readonly Vorlagenfeldanzeige GebaeudeName = new(
        "gebaeude.name", "Text", "Text", "je Gebäude", "Gebaeude", "Name des Gebäudes", Leerwert: "");

    public static IReadOnlyList<Vorlagenfeldanzeige> Alle => new[]
    {
        Kunde, Jaz, Tabelle, Bild, Kapitel, StandWert, StandBild, Schalter, GebaeudeName,
    };
}

/// <summary>Grundlage: Kultur de-DE, eigener Zustand und Halter, beide in Dispose zurück.</summary>
public abstract class VorlagenfeldBunitContext : EposBunitContext
{
    protected VorlagenfeldBunitContext(bool mitAblage = true)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        Ansicht = new Vorlagenfeldansicht { Kopiertdauer = TimeSpan.FromMinutes(5), Leuchtdauer = TimeSpan.FromMinutes(5) };
        Services.AddSingleton(Ansicht);
        if (mitAblage) Services.AddSingleton<IZwischenablage>(Ablage);
        Vorlagenfeldhalter.Setzen(Vorlagenfeldproben.Alle);
    }

    protected Vorlagenfeldansicht Ansicht { get; }
    protected TestZwischenablage Ablage { get; } = new();

    protected override void Dispose(bool disposing)
    {
        if (disposing) Vorlagenfeldhalter.Zuruecksetzen();
        base.Dispose(disposing);
    }
}

// =========================================================================
//  Die Marke
// =========================================================================

/// <summary>
/// <b>Die Platzhaltermarke</b> (<see cref="Vorlagenfeldknopf"/>, Konzept Berichtsvorlagen 9.4, 9.7):
/// die drei Stellungen, die Aufklappung mit allen Angaben, Anheften und Lösen (Klick, Esc,
/// Schließfläche), Kopieren je Art mit und ohne Zwischenablage, „Platzhalter ausblenden", das
/// Aufleuchten und die Anmeldung für die Zahl der Zeile.
/// </summary>
[Collection("Vorlagenfeldhalter")]
public sealed class VorlagenfeldknopfTests : VorlagenfeldBunitContext
{
    private IRenderedComponent<Vorlagenfeldknopf> Marke(string schluessel,
        Action<ComponentParameterCollectionBuilder<Vorlagenfeldknopf>>? mehr = null)
        => Render<Vorlagenfeldknopf>(p => { p.Add(x => x.Vorlagenfeld, schluessel); mehr?.Invoke(p); });

    [Fact]
    public void Ausgeschaltet_zeichnet_die_Marke_nichts()
    {
        var cut = Marke("projekt.kunde", p => p.Add(x => x.Stufe, Vorlagenfeldstufe.Aehnlich));
        Assert.Equal(Vorlagenfeldstellung.Aus, Ansicht.Stellung);
        Assert.Empty(cut.FindAll(".epos-vorlagenfeld, button"));

        // Nur der verborgene Anker: Schlüssel und Stufe für Wachen, ohne Trefferfläche.
        var anker = cut.Find("[data-vorlagenfeld]");
        Assert.True(anker.HasAttribute("hidden"));
        Assert.Equal("projekt.kunde", anker.GetAttribute("data-vorlagenfeld"));
        Assert.Equal("aehnlich", anker.GetAttribute("data-vorlagenfeldstufe"));
        Assert.Equal("", anker.TextContent);
    }

    [Fact]
    public void Ohne_Gaben_und_mit_unbekanntem_Schluessel_zeichnet_sie_nichts()
    {
        Ansicht.Setzen(Vorlagenfeldstellung.Marken);
        Assert.Empty(Render<Vorlagenfeldknopf>().FindAll(".epos-vorlagenfeld, button"));
        var unbekannt = Marke("gibt.es.nicht");
        Assert.Empty(unbekannt.FindAll(".epos-vorlagenfeld, button"));
        Assert.Equal("gibt.es.nicht", unbekannt.Find("[data-vorlagenfeld][hidden]").GetAttribute("data-vorlagenfeld"));
        Assert.Equal(0, Ansicht.Markenzahl);
    }

    [Fact]
    public void In_der_Stellung_Marken_steht_das_Symbol_mit_Namen_und_Mouse_over()
    {
        Ansicht.Setzen(Vorlagenfeldstellung.Marken);
        var cut = Marke("projekt.kunde");

        var huelle = cut.Find(".epos-vorlagenfeld");
        Assert.Contains("epos-vorlagenfeld--marken", huelle.ClassName);
        Assert.Equal("projekt.kunde", huelle.GetAttribute("data-vorlagenfeld"));
        Assert.Equal("entspricht", huelle.GetAttribute("data-vorlagenfeldstufe"));
        Assert.False(huelle.HasAttribute("hidden"));

        var knopf = cut.Find("button.epos-vorlagenfeld-marke");
        Assert.Equal("button", knopf.GetAttribute("type"));
        Assert.Equal("Platzhalter projekt.kunde: Angaben zeigen und kopieren", knopf.GetAttribute("aria-label"));
        Assert.Equal("projekt.kunde – Kunde des Stammprojekts.", knopf.GetAttribute("title"));
        Assert.Equal("false", knopf.GetAttribute("aria-expanded"));
        Assert.Equal("{ }", cut.Find(".epos-vorlagenfeld-symbol").TextContent);
        Assert.Empty(cut.FindAll(".epos-vorlagenfeld-schluessel"));
    }

    [Fact]
    public void Die_Aufklappung_nennt_alle_Angaben_aus_9_4()
    {
        Ansicht.Setzen(Vorlagenfeldstellung.Marken);
        var cut = Marke("stamm.kennzahl.eff.jaz", p => p
            .Add(x => x.Stufe, Vorlagenfeldstufe.Aehnlich)
            .Add(x => x.StufenHinweis, "im Bericht als Kuchendiagramm"));

        var auf = cut.Find(".epos-vorlagenfeld-aufklappung");
        Assert.Equal("group", auf.GetAttribute("role"));
        Assert.Equal("{{stamm.kennzahl.eff.jaz}}", cut.Find(".epos-vorlagenfeld-auf-schluessel").TextContent);
        Assert.Equal("Jahresarbeitszahl der Wärmepumpe", cut.Find(".epos-vorlagenfeld-auf-text").TextContent);

        string daten = string.Join(" | ", cut.FindAll(".epos-vorlagenfeld-auf-daten").Select(e => e.TextContent.Trim()));
        Assert.Contains("Art Zahl · Kontext Stamm", daten);
        Assert.Contains("Beispiel 3,8 –", daten);
        Assert.Contains("ohne Wert —", daten);
        Assert.Contains("Excel EPOS.stamm.kennzahl.eff.jaz", daten);
        Assert.Contains("ähnlich im Bericht", daten);
        Assert.Contains("im Bericht als Kuchendiagramm", daten);
        Assert.Contains("epos-vorlagenfeld-auf-stufe--aehnlich", cut.Find(".epos-vorlagenfeld-auf-stufe").ClassName);

        Assert.Equal("Kopieren", cut.Find(".epos-vorlagenfeld-kopieren").TextContent);
        Assert.Equal("Platzhalter ausblenden", cut.Find(".epos-vorlagenfeld-ausblenden").TextContent);
        Assert.Equal("Esc schließt", cut.Find(".epos-vorlagenfeld-auf-esc").TextContent);
        // Keine Überlagerung — also kein Schließkreuz.
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
    }

    [Fact]
    public void Entspricht_ist_die_Vorgabe_Excel_fehlt_bei_Kapiteln_und_der_Zusatz_steht_mehrzeilig()
    {
        Ansicht.Setzen(Vorlagenfeldstellung.Marken);
        var kapitel = Marke("kapitel.wirtschaftlichkeit");
        string daten = string.Join(" | ", kapitel.FindAll(".epos-vorlagenfeld-auf-daten").Select(e => e.TextContent.Trim()));
        Assert.Contains("entspricht dem Bericht", daten);
        Assert.Contains("Excel nur im Word-Bericht", daten);
        Assert.DoesNotContain("ohne Wert", daten);   // Kapitel haben keinen Leerwert

        var tabelle = Marke("tabelle.vergleich.effizienz", p => p.Add(x => x.Zusatz, "Zeilen: eff.jaz\nSpalten: stand.*"));
        Assert.Equal("Zeilen: eff.jaz\nSpalten: stand.*", tabelle.Find(".epos-vorlagenfeld-auf-zusatz").TextContent);
    }

    [Fact]
    public void In_der_Stellung_Schluessel_steht_der_Schluessel_sichtbar_und_ein_Klick_kopiert()
    {
        Ansicht.Setzen(Vorlagenfeldstellung.Schluessel);
        var cut = Marke("projekt.kunde");

        Assert.Contains("epos-vorlagenfeld--schluessel", cut.Find(".epos-vorlagenfeld").ClassName);
        Assert.Equal("projekt.kunde", cut.Find(".epos-vorlagenfeld-schluessel").TextContent);
        var knopf = cut.Find("button.epos-vorlagenfeld-marke");
        Assert.Equal("Platzhalter projekt.kunde kopieren", knopf.GetAttribute("aria-label"));
        Assert.False(knopf.HasAttribute("aria-expanded"));

        knopf.Click();

        Assert.Equal(new[] { "{{projekt.kunde}}" }, Ablage.Texte);
        Assert.Contains("epos-vorlagenfeld-marke--kopiert", cut.Find("button.epos-vorlagenfeld-marke").ClassName);
        Assert.Equal("kopiert", cut.Find(".epos-vorlagenfeld-kopiert").TextContent);
        Assert.False(cut.Instance.Angeheftet);
    }

    [Fact]
    public void Kopiert_verschwindet_nach_der_Kopiertdauer()
    {
        Ansicht.Kopiertdauer = TimeSpan.FromMilliseconds(30);
        Ansicht.Setzen(Vorlagenfeldstellung.Schluessel);
        var cut = Marke("projekt.kunde");

        cut.Find("button.epos-vorlagenfeld-marke").Click();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".epos-vorlagenfeld-kopiert")), TimeSpan.FromSeconds(5));
        Assert.Equal("projekt.kunde", cut.Find(".epos-vorlagenfeld-schluessel").TextContent);
    }

    [Fact]
    public void Klick_heftet_an_Esc_und_die_Schliessflaeche_loesen()
    {
        Ansicht.Setzen(Vorlagenfeldstellung.Marken);
        var cut = Marke("projekt.kunde");

        Assert.Empty(cut.FindAll(".epos-vorlagenfeld-schliessflaeche"));
        cut.Find("button.epos-vorlagenfeld-marke").Click();
        Assert.True(cut.Instance.Angeheftet);
        Assert.Contains("epos-vorlagenfeld--offen", cut.Find(".epos-vorlagenfeld").ClassName);
        Assert.Equal("true", cut.Find("button.epos-vorlagenfeld-marke").GetAttribute("aria-expanded"));
        Assert.Single(cut.FindAll(".epos-vorlagenfeld-schliessflaeche"));

        cut.Find(".epos-vorlagenfeld").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(cut.Instance.Angeheftet);

        cut.Find("button.epos-vorlagenfeld-marke").Click();
        cut.Find(".epos-vorlagenfeld-schliessflaeche").Click();
        Assert.False(cut.Instance.Angeheftet);
        Assert.Empty(cut.FindAll(".epos-vorlagenfeld-schliessflaeche"));

        // Ein zweiter Klick auf die Marke löst ebenso.
        cut.Find("button.epos-vorlagenfeld-marke").Click();
        cut.Find("button.epos-vorlagenfeld-marke").Click();
        Assert.False(cut.Instance.Angeheftet);
    }

    [Fact]
    public void Esc_schliesst_die_Aufklappung_im_Fokus_und_gibt_das_naechste_Esc_frei()
    {
        Ansicht.Setzen(Vorlagenfeldstellung.Marken);
        var cut = Marke("projekt.kunde");

        cut.Find(".epos-vorlagenfeld").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Contains("epos-vorlagenfeld--unterdrueckt", cut.Find(".epos-vorlagenfeld").ClassName);

        cut.Find(".epos-vorlagenfeld").MouseLeave();
        Assert.DoesNotContain("epos-vorlagenfeld--unterdrueckt", cut.Find(".epos-vorlagenfeld").ClassName);
    }

    public static TheoryData<string, string, string> Kopien() => new()
    {
        { "projekt.kunde", "{{projekt.kunde}}", "In Word tippen oder einfügen" },
        { "stamm.kennzahl.eff.jaz", "{{stamm.kennzahl.eff.jaz}}", "In Word tippen oder einfügen" },
        { "tabelle.vergleich.effizienz", "{{tabelle.vergleich.effizienz}}", "In einen eigenen Absatz einfügen" },
        { "kapitel.wirtschaftlichkeit", "{{kapitel.wirtschaftlichkeit}}", "In einen eigenen Absatz einfügen" },
        { "bild.vergleich.balken.eff.jaz", "bild.vergleich.balken.eff.jaz", "Bild einfügen, Alternativtext = Schlüssel." },
        { "stand.kennzahl.eff.jaz", "{{#je stand}}\n{{stand.kennzahl.eff.jaz}}\n{{/je}}", "Wert je Stand" },
        { "stand.bild.deckung_waerme", "stand.bild.deckung_waerme", "Bild je Stand" },
        { "hat.tabelle.vergleich.effizienz", "{{#wenn hat.tabelle.vergleich.effizienz}}\n…\n{{/wenn}}", "Nur als Bedingung" },
        { "gebaeude.name", "{{#je gebaeude}}\n{{gebaeude.name}}\n{{/je}}", "Wert je Gebäude" },
    };

    [Theory]
    [MemberData(nameof(Kopien))]
    public void Kopieren_je_Art_legt_den_Text_der_Vorlage_in_die_Zwischenablage(string schluessel, string text, string hinweis)
    {
        Ansicht.Setzen(Vorlagenfeldstellung.Marken);
        var cut = Marke(schluessel);

        Assert.Contains(hinweis, cut.Find(".epos-vorlagenfeld-auf-hinweis").TextContent);
        cut.Find(".epos-vorlagenfeld-kopieren").Click();

        Assert.Equal(new[] { text }, Ablage.Texte);
        Assert.Equal("✓ kopiert", cut.Find(".epos-vorlagenfeld-kopieren").TextContent);
        Assert.Contains("epos-vorlagenfeld-kopieren--kopiert", cut.Find(".epos-vorlagenfeld-kopieren").ClassName);
        Assert.Empty(cut.FindAll(".epos-vorlagenfeld-auf-textfeld"));
    }

    [Fact]
    public void Scheitert_die_Zwischenablage_steht_der_Text_markiert_in_der_Aufklappung()
    {
        Ablage.Antwort = false;
        Ansicht.Setzen(Vorlagenfeldstellung.Schluessel);
        var cut = Marke("stand.kennzahl.eff.jaz");

        cut.Find("button.epos-vorlagenfeld-marke").Click();

        Assert.True(cut.Instance.Angeheftet);
        Assert.Contains("epos-vorlagenfeld--offen", cut.Find(".epos-vorlagenfeld").ClassName);
        var feld = cut.Find("textarea.epos-vorlagenfeld-auf-textfeld");
        Assert.True(feld.HasAttribute("readonly"));
        Assert.Equal("{{#je stand}}\n{{stand.kennzahl.eff.jaz}}\n{{/je}}", feld.TextContent);
        Assert.Equal("3", feld.GetAttribute("rows"));
        Assert.Contains("Zwischenablage ist hier nicht erreichbar", cut.Markup);
        Assert.Empty(cut.FindAll(".epos-vorlagenfeld-kopiert"));

        // Markiert über die JS-Brücke: querySelector auf das Feld der Marke.
        Assert.Contains(JSInterop.Invocations, i => i.Identifier == "document.querySelector"
                                                    && ((string)i.Arguments[0]!).EndsWith(" textarea", StringComparison.Ordinal));
    }

    [Fact]
    public void Ausblenden_schaltet_die_Anzeige_fuer_alle_Marken_aus()
    {
        Ansicht.Setzen(Vorlagenfeldstellung.Marken);
        var eins = Marke("projekt.kunde");
        var zwei = Marke("stamm.kennzahl.eff.jaz");

        eins.Find(".epos-vorlagenfeld-ausblenden").Click();

        Assert.Equal(Vorlagenfeldstellung.Aus, Ansicht.Stellung);
        eins.WaitForAssertion(() => Assert.Empty(eins.FindAll(".epos-vorlagenfeld")));
        zwei.WaitForAssertion(() => Assert.Empty(zwei.FindAll(".epos-vorlagenfeld")));
    }

    [Fact]
    public void Jede_Marke_folgt_dem_geteilten_Zustand()
    {
        var cut = Marke("projekt.kunde");
        Assert.Empty(cut.FindAll(".epos-vorlagenfeld"));

        cut.InvokeAsync(() => Ansicht.Setzen(Vorlagenfeldstellung.Marken));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("button.epos-vorlagenfeld-marke")));

        cut.InvokeAsync(() => Ansicht.Setzen(Vorlagenfeldstellung.Schluessel));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-vorlagenfeld-schluessel")));
    }

    [Fact]
    public void In_der_App_zeigen_laesst_die_Marke_aufleuchten_und_dann_erloeschen()
    {
        var cut = Marke("projekt.kunde");
        var andere = Marke("stamm.kennzahl.eff.jaz");

        cut.InvokeAsync(() => Ansicht.Leuchten("projekt.kunde"));

        Assert.Equal(Vorlagenfeldstellung.Marken, Ansicht.Stellung);
        cut.WaitForAssertion(() => Assert.Contains("epos-vorlagenfeld--leuchten", cut.Find(".epos-vorlagenfeld").ClassName));
        Assert.DoesNotContain("epos-vorlagenfeld--leuchten", andere.Find(".epos-vorlagenfeld").ClassName);
        Assert.Contains(JSInterop.Invocations, i => i.Identifier == "document.querySelector"
                                                    && (string)i.Arguments[0]! == "#" + cut.Find(".epos-vorlagenfeld").Id);

        cut.InvokeAsync(() => Ansicht.LeuchtenBeenden("projekt.kunde"));
        cut.WaitForAssertion(() => Assert.DoesNotContain("epos-vorlagenfeld--leuchten", cut.Find(".epos-vorlagenfeld").ClassName));
    }

    [Fact]
    public void Das_Leuchten_erlischt_nach_der_Leuchtdauer()
    {
        Ansicht.Leuchtdauer = TimeSpan.FromMilliseconds(30);
        var cut = Marke("projekt.kunde");

        cut.InvokeAsync(() => Ansicht.Leuchten("projekt.kunde"));

        cut.WaitForAssertion(() => Assert.Null(Ansicht.Leuchtschluessel), TimeSpan.FromSeconds(5));
        cut.WaitForAssertion(() => Assert.DoesNotContain("epos-vorlagenfeld--leuchten", cut.Find(".epos-vorlagenfeld").ClassName));
        Assert.Equal(Vorlagenfeldstellung.Marken, Ansicht.Stellung);
    }

    [Fact]
    public void Jede_Marke_meldet_sich_an_und_ab_je_Schluessel_einmal()
    {
        var a = Marke("projekt.kunde");
        var b = Marke("projekt.kunde");
        var c = Marke("stamm.kennzahl.eff.jaz");
        Assert.Equal(2, Ansicht.Markenzahl);

        // Ein Schlüsselwechsel meldet um, ein leerer ab.
        c.Render(p => p.Add(x => x.Vorlagenfeld, ""));
        Assert.Equal(1, Ansicht.Markenzahl);
        c.Render(p => p.Add(x => x.Vorlagenfeld, "tabelle.vergleich.effizienz"));
        Assert.Equal(2, Ansicht.Markenzahl);

        // Verworfen meldet jede Marke sich ab.
        a.Instance.Dispose();
        b.Instance.Dispose();
        c.Instance.Dispose();
        Assert.Equal(0, Ansicht.Markenzahl);
    }
}

/// <summary>Ohne Zwischenablage im Dienstverzeichnis: der Text steht markiert in der Aufklappung.</summary>
[Collection("Vorlagenfeldhalter")]
public sealed class VorlagenfeldknopfOhneAblageTests : VorlagenfeldBunitContext
{
    public VorlagenfeldknopfOhneAblageTests() : base(mitAblage: false) { }

    [Fact]
    public void Ohne_Adapter_zeigt_die_Aufklappung_den_Text_markiert()
    {
        Ansicht.Setzen(Vorlagenfeldstellung.Marken);
        var cut = Render<Vorlagenfeldknopf>(p => p.Add(x => x.Vorlagenfeld, "projekt.kunde"));

        cut.Find(".epos-vorlagenfeld-kopieren").Click();

        Assert.Equal("{{projekt.kunde}}", cut.Instance.TextOhneAblage);
        Assert.Equal("{{projekt.kunde}}", cut.Find("textarea.epos-vorlagenfeld-auf-textfeld").TextContent);
        Assert.Equal("Text für die Vorlage", cut.Find(".epos-vorlagenfeld-auf-feld > span").TextContent);
        Assert.True(cut.Instance.Angeheftet);
        Assert.Empty(cut.FindAll(".epos-vorlagenfeld-kopiert"));

        // Lösen räumt das Feld weg.
        cut.Find(".epos-vorlagenfeld-schliessflaeche").Click();
        Assert.Null(cut.Instance.TextOhneAblage);
    }
}

/// <summary>Ohne Eintrag im Dienstverzeichnis nimmt die Marke den Rückfall — sie wirft nicht.</summary>
[Collection("Vorlagenfeldhalter")]
public sealed class VorlagenfeldknopfOhneDienstverzeichnisTests : EposBunitContext
{
    public VorlagenfeldknopfOhneDienstverzeichnisTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Vorlagenfeldhalter.Setzen(Vorlagenfeldproben.Alle);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Vorlagenfeldansicht.Rueckfall.Setzen(Vorlagenfeldstellung.Aus);
            Vorlagenfeldhalter.Zuruecksetzen();
        }
        base.Dispose(disposing);
    }

    [Fact]
    public void Die_Marke_zeichnet_mit_dem_Rueckfall()
    {
        var cut = Render<Vorlagenfeldknopf>(p => p.Add(x => x.Vorlagenfeld, "projekt.kunde"));
        Assert.Empty(cut.FindAll(".epos-vorlagenfeld"));

        cut.InvokeAsync(() => Vorlagenfeldansicht.Rueckfall.Setzen(Vorlagenfeldstellung.Marken));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("button.epos-vorlagenfeld-marke")));
    }
}

// =========================================================================
//  Kopieren je Art (ohne Oberfläche)
// =========================================================================

/// <summary>Die Regel „Kopieren je Art" (Konzept 9.4) an EINER Stelle: <see cref="Vorlagenfeldkopie.Fuer"/>.</summary>
public sealed class VorlagenfeldkopieTests
{
    [Fact]
    public void Ohne_Eintrag_ist_der_Text_leer()
    {
        Assert.Equal("", Vorlagenfeldkopie.Fuer(null).Text);
        Assert.Equal("", Vorlagenfeldkopie.Fuer(Vorlagenfeldproben.Kunde with { Schluessel = " " }).Text);
    }

    [Fact]
    public void Die_Hinweise_folgen_der_Art_und_der_Block_dem_Kontext()
    {
        Assert.Equal(Kopierhinweis.Einzeln, Vorlagenfeldkopie.Fuer(Vorlagenfeldproben.Kunde).Hinweis);
        Assert.Equal(Kopierhinweis.EigenerAbsatz, Vorlagenfeldkopie.Fuer(Vorlagenfeldproben.Tabelle).Hinweis);
        Assert.Equal(Kopierhinweis.EigenerAbsatz, Vorlagenfeldkopie.Fuer(Vorlagenfeldproben.Kapitel).Hinweis);
        Assert.Equal(Kopierhinweis.Bild, Vorlagenfeldkopie.Fuer(Vorlagenfeldproben.Bild).Hinweis);
        Assert.Equal(Kopierhinweis.Schalter, Vorlagenfeldkopie.Fuer(Vorlagenfeldproben.Schalter).Hinweis);
        Assert.Equal("", Vorlagenfeldkopie.Fuer(Vorlagenfeldproben.Kunde).Block);
        Assert.Equal("stand", Vorlagenfeldkopie.Fuer(Vorlagenfeldproben.StandWert).Block);
        Assert.Equal("stand", Vorlagenfeldkopie.Fuer(Vorlagenfeldproben.StandBild).Block);
        Assert.Equal("gebaeude", Vorlagenfeldkopie.Fuer(Vorlagenfeldproben.GebaeudeName).Block);
        Assert.Equal("{{liste.x}}", Vorlagenfeldkopie.Fuer(
            new Vorlagenfeldanzeige("liste.x", "Liste", "Liste", "Bericht", "Bericht", "")).Text);
    }
}

// =========================================================================
//  Umschalter und Zeile
// =========================================================================

/// <summary>
/// <b>Der Umschalter</b> (<see cref="Vorlagenfeldumschalter"/>, Konzept 9.4): { } · Aus · Marken ·
/// Schlüssel als Optionsgruppe mit 44-px-Stellungen, Pfeiltasten, ein Zustand für alle.
/// </summary>
[Collection("Vorlagenfeldhalter")]
public sealed class VorlagenfeldumschalterTests : VorlagenfeldBunitContext
{
    [Fact]
    public void Der_Umschalter_fuehrt_drei_Stellungen_und_steht_auf_Aus()
    {
        var cut = Render<Vorlagenfeldumschalter>();

        var gruppe = cut.Find(".epos-vorlagenfeld-umschalter");
        Assert.Equal("radiogroup", gruppe.GetAttribute("role"));
        Assert.Equal("Platzhalter zeigen", gruppe.GetAttribute("aria-label"));
        Assert.Equal("Platzhalter zeigen", gruppe.GetAttribute("title"));
        Assert.Equal("{ }", cut.Find(".epos-vorlagenfeld-umschalter-titel").TextContent);

        var stellungen = cut.FindAll("button.epos-vorlagenfeld-stellung");
        Assert.Equal(new[] { "Aus", "Marken", "Schlüssel" }, stellungen.Select(s => s.TextContent));
        Assert.All(stellungen, s => Assert.Equal("radio", s.GetAttribute("role")));
        Assert.Equal(new[] { "true", "false", "false" }, stellungen.Select(s => s.GetAttribute("aria-checked")));
        Assert.Equal(new[] { "0", "-1", "-1" }, stellungen.Select(s => s.GetAttribute("tabindex")));
        Assert.Empty(cut.FindAll("input"));   // kein nacktes <input type="radio">
    }

    [Fact]
    public void Ein_Klick_stellt_um_und_jeder_Umschalter_folgt()
    {
        var eins = Render<Vorlagenfeldumschalter>(p => p.Add(x => x.Ort, "BERICHTE_KOSTEN"));
        var zwei = Render<Vorlagenfeldumschalter>(p => p.Add(x => x.Ort, "SIMULATION_ERGEBNIS"));

        eins.FindAll("button.epos-vorlagenfeld-stellung")[2].Click();

        Assert.Equal(Vorlagenfeldstellung.Schluessel, Ansicht.Stellung);
        zwei.WaitForAssertion(() =>
            Assert.Contains("epos-vorlagenfeld-stellung--an", zwei.FindAll("button.epos-vorlagenfeld-stellung")[2].ClassName));
    }

    [Theory]
    [InlineData("ArrowRight", Vorlagenfeldstellung.Marken)]
    [InlineData("ArrowLeft", Vorlagenfeldstellung.Schluessel)]
    [InlineData("End", Vorlagenfeldstellung.Schluessel)]
    [InlineData("Home", Vorlagenfeldstellung.Aus)]
    [InlineData("Enter", Vorlagenfeldstellung.Aus)]
    public void Pfeile_und_Pos1_Ende_wandern_wie_in_einer_Optionsgruppe(string taste, Vorlagenfeldstellung erwartet)
    {
        var cut = Render<Vorlagenfeldumschalter>();
        cut.Find(".epos-vorlagenfeld-umschalter").KeyDown(new KeyboardEventArgs { Key = taste });
        Assert.Equal(erwartet, Ansicht.Stellung);
    }
}

/// <summary>
/// <b>Die leise Zeile</b> der Stellung „Schlüssel" (<see cref="Vorlagenfeldzeile"/>, Konzept 9.4): Zahl
/// der Platzhalter auf der Seite und „Katalog…" mit dem Katalog aus dem Halter.
/// </summary>
[Collection("Vorlagenfeldhalter")]
public sealed class VorlagenfeldzeileTests : VorlagenfeldBunitContext
{
    [Fact]
    public void Aus_und_Marken_zeichnen_keine_Zeile()
    {
        var cut = Render<Vorlagenfeldzeile>();
        Assert.Empty(cut.FindAll(".epos-vorlagenfeld-zeile"));

        cut.InvokeAsync(() => Ansicht.Setzen(Vorlagenfeldstellung.Marken));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".epos-vorlagenfeld-zeile")));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));   // keine Leiste, kein Kreuz
    }

    [Fact]
    public void In_der_Stellung_Schluessel_nennt_sie_die_Zahl_der_Platzhalter()
    {
        Ansicht.Setzen(Vorlagenfeldstellung.Schluessel);
        var cut = Render<Vorlagenfeldzeile>();
        Render<Vorlagenfeldknopf>(p => p.Add(x => x.Vorlagenfeld, "projekt.kunde"));
        Render<Vorlagenfeldknopf>(p => p.Add(x => x.Vorlagenfeld, "stamm.kennzahl.eff.jaz"));

        cut.WaitForAssertion(() => Assert.Contains("2 Platzhalter auf dieser Seite · ein Klick auf einen Schlüssel kopiert ihn",
                                                   cut.Find(".epos-vorlagenfeld-zeile-text").TextContent));
        Assert.Equal("Katalog…", cut.Find(".epos-vorlagenfeld-katalog").TextContent);
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
    }

    [Fact]
    public void Katalog_oeffnet_den_Platzhalterkatalog_aus_dem_Halter_und_Kreuz_schliesst()
    {
        Ansicht.Setzen(Vorlagenfeldstellung.Schluessel);
        var cut = Render<Vorlagenfeldzeile>();

        cut.Find(".epos-vorlagenfeld-katalog").Click();

        var katalog = cut.FindComponent<PlatzhalterkatalogDialog>();
        Assert.Equal(Vorlagenfeldproben.Alle.Count, katalog.Instance.Eintraege.Count);
        Assert.False(katalog.Instance.TitelAnzeigen);
        Assert.Null(katalog.Instance.BaukastenSpeichern);
        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));   // das Kreuz der Überlagerung
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));           // keins im eingebetteten Dialog

        cut.Find(".epos-ueberlagerung-zu").Click();
        Assert.Empty(cut.FindComponents<PlatzhalterkatalogDialog>());

        // Esc schließt ebenso.
        cut.Find(".epos-vorlagenfeld-katalog").Click();
        cut.Find(".epos-ueberlagerung").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(cut.FindComponents<PlatzhalterkatalogDialog>());
    }
}

// =========================================================================
//  Der Halter
// =========================================================================

/// <summary>Der Halter lädt über die Quelle, einmal je Sprache, und setzt sich zurück.</summary>
[Collection("Vorlagenfeldhalter")]
public sealed class VorlagenfeldhalterTests : IDisposable
{
    public void Dispose() => Vorlagenfeldhalter.Zuruecksetzen();

    [Fact]
    public void Ohne_Quelle_ist_der_Halter_leer()
    {
        Vorlagenfeldhalter.Zuruecksetzen();
        Assert.Empty(Vorlagenfeldhalter.Alle);
        Assert.Null(Vorlagenfeldhalter.Finde("projekt.kunde"));
        Assert.Null(Vorlagenfeldhalter.Finde(null));
    }

    [Fact]
    public void Die_Quelle_wird_einmal_gelesen_und_eine_werfende_Quelle_ergibt_leer()
    {
        int gelesen = 0;
        Vorlagenfeldhalter.Quelle = () => { gelesen++; return Vorlagenfeldproben.Alle; };
        Assert.Same(Vorlagenfeldproben.Kunde, Vorlagenfeldhalter.Finde("projekt.kunde"));
        Assert.Same(Vorlagenfeldproben.Jaz, Vorlagenfeldhalter.Finde(" stamm.kennzahl.eff.jaz "));
        Assert.Equal(9, Vorlagenfeldhalter.Alle.Count);
        Assert.Equal(1, gelesen);

        Vorlagenfeldhalter.Quelle = () => throw new InvalidOperationException("kaputt");
        Assert.Empty(Vorlagenfeldhalter.Alle);
    }

    [Fact]
    public void Kurzbeschreibung_und_Beispiel_samt_Einheit()
    {
        Assert.Equal("Kunde des Stammprojekts.", Vorlagenfeldproben.Kunde.Kurzbeschreibung);
        Assert.Equal("3,8 –", Vorlagenfeldproben.Jaz.BeispielMitEinheit);
        Assert.Equal("", Vorlagenfeldproben.Kunde.BeispielMitEinheit);
        var lang = Vorlagenfeldproben.Kunde with { Beschreibung = new string('x', 200) };
        Assert.Equal(120, lang.Kurzbeschreibung.Length);
    }
}
