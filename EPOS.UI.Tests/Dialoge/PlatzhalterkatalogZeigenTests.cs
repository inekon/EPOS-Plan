using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using EPOS.UI.Dialoge.Berichte;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>„In der App zeigen"</b> im Platzhalterkatalog (BV-E6, Konzept Berichtsvorlagen 9.3, 9.4): mit einem
/// lesend erreichbaren Ort springt der Katalog über <c>Dienste.Navigation</c> zur Ansicht, schaltet
/// „Marken" ein, lässt die Marke aufleuchten und schließt; ohne Ort, nur in einer Eingabemaske oder nur
/// im Projektassistenten ist der Knopf WEICH gesperrt und nennt den Grund; kennt die Oberfläche die
/// Ansicht nicht, bleibt der Katalog offen und die Anzeige, wie sie war.
///
/// <para>Die Orte sind meist Stubs (<c>OrteFinden</c>); ein Fall je Ansicht nimmt die echte Tabelle
/// <see cref="Vorlagenfeldorte"/>. <c>Dienste.Navigation</c>
/// ist prozessweit: Sammlung <c>KiDialogweg</c>, Rückstellung in <c>Dispose</c>.</para>
/// </summary>
[Collection("KiDialogweg")]
public sealed class PlatzhalterkatalogZeigenTests : EposBunitContext
{
    private readonly INavigation _vorher;
    private readonly TestNavigation _navigation = new();
    private readonly Vorlagenfeldansicht _ansicht = new() { Leuchtdauer = TimeSpan.FromMinutes(5) };

    public PlatzhalterkatalogZeigenTests()
    {
        _vorher = WindowsFormsApplication1.Dienste.Navigation;
        WindowsFormsApplication1.Dienste.Navigation = _navigation;
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        Services.AddSingleton(_ansicht);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) WindowsFormsApplication1.Dienste.Navigation = _vorher;
        base.Dispose(disposing);
    }

    private static readonly Vorlagenfeldort Kachel =
        new(Seitenschluessel.BerichteKosten, "WIRTSCHAFT", "Kachel „Kapitalwert ggü. Stamm“", true);

    private static IReadOnlyList<Katalogzeile> Katalog() => new[]
    {
        new Katalogzeile("wirtschaft.beste.kapitalwert", "Zahl", "je Gruppe", "Kapitalwert der besten Variante"),
        new Katalogzeile("projekt.kunde", "Text", "Stamm", "Kunde des Projekts"),
    };

    private IRenderedComponent<PlatzhalterkatalogDialog> Zeige(Func<string, IReadOnlyList<Vorlagenfeldort>>? orte,
                                                               Action? geschlossen = null)
        => Render<PlatzhalterkatalogDialog>(p => p
            .Add(x => x.Eintraege, Katalog())
            .Add(x => x.OrteFinden, orte)
            .Add(x => x.Geschlossen, () => geschlossen?.Invoke()));

    private static void Waehle(IRenderedComponent<PlatzhalterkatalogDialog> cut, string schluessel)
        => cut.FindAll(".epos-vorlage-katalogwahl").First(k => k.TextContent.Trim() == schluessel).Click();

    [Fact]
    public void Ohne_Wahl_steht_kein_Knopf()
    {
        var cut = Zeige(_ => new[] { Kachel });
        Assert.Empty(cut.FindAll(".epos-vorlage-katalogzeigen-knopf"));
    }

    [Fact]
    public void Mit_Ort_springt_der_Katalog_schaltet_Marken_ein_und_schliesst()
    {
        bool zu = false;
        var cut = Zeige(s => s == "wirtschaft.beste.kapitalwert" ? new[] { Kachel } : Array.Empty<Vorlagenfeldort>(),
                        () => zu = true);
        Waehle(cut, "wirtschaft.beste.kapitalwert");

        var knopf = cut.Find(".epos-vorlage-katalogzeigen-knopf");
        Assert.Equal("In der App zeigen", knopf.TextContent);
        Assert.False(knopf.HasAttribute("aria-disabled"));
        Assert.False(knopf.HasAttribute("disabled"));
        Assert.Contains("In der App: Kachel „Kapitalwert ggü. Stamm“", cut.Find(".epos-vorlage-katalogzeigen").TextContent);

        knopf.Click();

        Assert.Equal(new[] { Seitenschluessel.BerichteKosten }, _navigation.Masken);
        Assert.Equal(new object[] { "WIRTSCHAFT" }, _navigation.LetzteArgumente);
        Assert.Equal(Vorlagenfeldstellung.Marken, _ansicht.Stellung);
        Assert.Equal("wirtschaft.beste.kapitalwert", _ansicht.Leuchtschluessel);
        Assert.True(zu);
    }

    [Fact]
    public void Ohne_Reiter_geht_die_Ansicht_ohne_Argument_auf_und_Schluessel_bleibt_Schluessel()
    {
        _ansicht.Setzen(Vorlagenfeldstellung.Schluessel);
        var cut = Zeige(_ => new[] { new Vorlagenfeldort(Seitenschluessel.Simulation, "", "Ring „Deckung Wärme“", true) });
        Waehle(cut, "projekt.kunde");

        cut.Find(".epos-vorlage-katalogzeigen-knopf").Click();

        Assert.Equal(new[] { Seitenschluessel.Simulation }, _navigation.Masken);
        Assert.Empty(_navigation.LetzteArgumente);
        Assert.Equal(Vorlagenfeldstellung.Schluessel, _ansicht.Stellung);
    }

    public static TheoryData<string, string> Sperren() => new()
    {
        { "kein", "Kein Gegenstück in der App – dieser Platzhalter entsteht nur im Bericht." },
        { "eingabe", "Nur in einer Eingabemaske zu sehen – dorthin führt der Katalog nicht." },
        { "assistent", "Nur im Projektassistenten zu sehen – dorthin führt der Katalog nicht." },
    };

    [Theory]
    [MemberData(nameof(Sperren))]
    public void Ohne_erreichbaren_Ort_ist_der_Knopf_weich_gesperrt_und_meldet_den_Grund(string fall, string grund)
    {
        IReadOnlyList<Vorlagenfeldort> orte = fall switch
        {
            "eingabe" => new[] { new Vorlagenfeldort(Seitenschluessel.Startseite, "ERZEUGER", "Feld Kunde", false) },
            "assistent" => new[]
            {
                new Vorlagenfeldort(Seitenschluessel.Assistent, "", "Projektkopf", true),
                new Vorlagenfeldort(Seitenschluessel.Startseite, "", "Feld", false),
            },
            _ => Array.Empty<Vorlagenfeldort>(),
        };
        bool zu = false;
        var cut = Zeige(_ => orte, () => zu = true);
        Waehle(cut, "projekt.kunde");

        var knopf = cut.Find(".epos-vorlage-katalogzeigen-knopf");
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.False(knopf.HasAttribute("disabled"));   // weich: der title erscheint
        Assert.Equal(grund, knopf.GetAttribute("title"));
        Assert.Contains(grund, cut.Find(".epos-vorlage-katalogzeigen").TextContent);

        knopf.Click();

        Assert.Empty(_navigation.Masken);
        Assert.Equal(grund, cut.Find(".epos-leiste .epos-status").TextContent);
        Assert.Equal(Vorlagenfeldstellung.Aus, _ansicht.Stellung);
        Assert.False(zu);
    }

    [Fact]
    public void Kennt_die_Oberflaeche_die_Ansicht_nicht_bleibt_alles_wie_es_war()
    {
        _navigation.Antwort = false;
        bool zu = false;
        var cut = Zeige(_ => new[] { Kachel }, () => zu = true);
        Waehle(cut, "wirtschaft.beste.kapitalwert");

        cut.Find(".epos-vorlage-katalogzeigen-knopf").Click();

        Assert.Equal("Die Ansicht ist hier nicht erreichbar.", cut.Find(".epos-leiste .epos-status").TextContent);
        Assert.Equal(Vorlagenfeldstellung.Aus, _ansicht.Stellung);
        Assert.Null(_ansicht.Leuchtschluessel);
        Assert.False(zu);
    }

    /// <summary>
    /// Je ein Ort jeder Ansicht der ECHTEN Ortstabelle springt so, wie beide Schalen es verstehen:
    /// „Berichte &amp; Kosten“ mit der Seite als Argument, die Ergebnisblätter der Simulation über die
    /// Ansicht <c>SIMULATION</c> mit der Marke <c>schritt=3;blatt=…</c> — die Windows-Navigation
    /// leitet <c>SIMULATION_ERGEBNIS</c> nicht weiter.
    /// </summary>
    [Theory]
    [InlineData("wirtschaft.beste.kapitalwert", Ansichten.BerichteKosten, "WIRTSCHAFT")]
    [InlineData("stamm.wirtschaft.investition", Ansichten.BerichteKosten, "KOSTEN")]
    [InlineData("tabelle.komponenten.matrix", Ansichten.BerichteKosten, "UEBERSICHT")]
    [InlineData("stand.bild.deckung_waerme", Masken.Simulation, "schritt=3;blatt=UEBERSICHT")]
    [InlineData("stamm.bild.speichertemperaturen", Masken.Simulation, "schritt=3;blatt=WAERMEPUMPE")]
    [InlineData("stand.bild.speicherverlauf", Masken.Simulation, "schritt=3;blatt=STROMSPEICHER")]
    public void Ein_Ort_jeder_Ansicht_der_Tabelle_springt_auf_beiden_Schalen(string schluessel, string maske, string argument)
    {
        bool zu = false;
        var cut = Render<PlatzhalterkatalogDialog>(p => p
            .Add(x => x.Eintraege, new[] { new Katalogzeile(schluessel, "Zahl", "Stamm", "Probe") })
            .Add(x => x.Geschlossen, () => zu = true));
        Waehle(cut, schluessel);

        cut.Find(".epos-vorlage-katalogzeigen-knopf").Click();

        Assert.Equal(new[] { maske }, _navigation.Masken);
        Assert.Equal(new object[] { argument }, _navigation.LetzteArgumente);
        if (maske == Masken.Simulation)
        {
            (int schritt, string blatt) = EPOS.UI.Seiten.Simulation.SimulationMarke.Lesen(argument);
            Assert.Equal(3, schritt);
            Assert.False(string.IsNullOrEmpty(blatt));
        }
        Assert.Equal(schluessel, _ansicht.Leuchtschluessel);
        Assert.True(zu);
    }

    [Fact]
    public void Ohne_OrteFinden_gilt_die_Tabelle_der_App()
    {
        var cut = Render<PlatzhalterkatalogDialog>(p => p.Add(x => x.Eintraege, Katalog()));
        Waehle(cut, "projekt.kunde");

        // Dieselbe Antwort wie die Regel über der Tabelle der App (Projektkopf: nur im Assistenten).
        bool erreichbar = Vorlagenfeldzeige.Waehle(Vorlagenfeldorte.Finde("projekt.kunde")).Ort is not null;
        Assert.Equal(erreichbar ? null : "true",
                     cut.Find(".epos-vorlage-katalogzeigen-knopf").GetAttribute("aria-disabled"));
    }
}

/// <summary>Die Wahl des Ortes (<see cref="Vorlagenfeldzeige.Waehle"/>) ohne Oberfläche.</summary>
public sealed class VorlagenfeldzeigeTests
{
    [Fact]
    public void Der_erste_lesende_Ort_gewinnt_und_der_Grund_ist_der_schwerste()
    {
        var lesend = new Vorlagenfeldort(Seitenschluessel.BerichteKosten, "", "a", true);
        var eingabe = new Vorlagenfeldort(Seitenschluessel.Startseite, "", "b", false);
        var assistent = new Vorlagenfeldort(Seitenschluessel.ProjektBearbeiten, "", "c", true);

        Assert.Equal((null, Zeigegrund.KeinOrt), Vorlagenfeldzeige.Waehle(null));
        Assert.Equal((lesend, Zeigegrund.Moeglich), Vorlagenfeldzeige.Waehle(new[] { eingabe, assistent, lesend }));
        Assert.Equal((null, Zeigegrund.NichtLesend), Vorlagenfeldzeige.Waehle(new[] { eingabe }));
        Assert.Equal((null, Zeigegrund.Assistent), Vorlagenfeldzeige.Waehle(new[] { eingabe, assistent }));
        Assert.Equal((null, Zeigegrund.Assistent), Vorlagenfeldzeige.Waehle(new[] { assistent, eingabe }));
        Assert.Equal((null, Zeigegrund.KeinOrt),
                     Vorlagenfeldzeige.Waehle(new[] { new Vorlagenfeldort("", "", "leer", true) }));
    }

    [Fact]
    public void Zeigen_verweigert_Assistent_und_Eingabemaske_ohne_zu_springen()
    {
        var ansicht = new Vorlagenfeldansicht();
        Assert.False(Vorlagenfeldzeige.Zeigen(ansicht, new Vorlagenfeldort(Seitenschluessel.Assistent, "", "", true), "x"));
        Assert.False(Vorlagenfeldzeige.Zeigen(ansicht, new Vorlagenfeldort(Seitenschluessel.Startseite, "", "", false), "x"));
        Assert.Equal(Vorlagenfeldstellung.Aus, ansicht.Stellung);
        Assert.Null(ansicht.Leuchtschluessel);
    }
}
