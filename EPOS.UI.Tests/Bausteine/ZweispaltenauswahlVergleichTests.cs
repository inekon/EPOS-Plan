using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// KA-E-24: „Vergleichen…" über beide Listen der Katalogauswahl — die Kreuze der Projekt- und der Katalogliste
/// zusammen (zwei bis drei), ohne Kreuze die gewählte Projektzeile gegen den gewählten Katalogsatz; die Herkunft
/// steht im Spaltenkopf vor dem Namen; sonst weich gesperrt mit Grund.
/// </summary>
public class ZweispaltenauswahlVergleichTests : EposBunitContext
{
    private static RenderFragment Absatz(string text) => b =>
    {
        b.OpenElement(0, "p");
        b.AddContent(1, text);
        b.CloseElement();
    };

    private static Vergleichssatz? Satz(Satzmarke herkunft, string schluessel)
        => new(herkunft, (herkunft == Satzmarke.Projektsatz ? "Kessel " : "Katalog ") + schluessel,
               new List<(string, string)> { ("Leistung [kW]", schluessel == "1" ? "100" : "200"), ("Brennstoff", "Gas") });

    private IRenderedComponent<Zweispaltenauswahl> Aufbauen(Bereichswahl projekt, Bereichswahl katalog,
                                                           string? projektGewaehlt = null, string? katalogGewaehlt = null,
                                                           List<string>? abgelehnt = null)
        => Render<Zweispaltenauswahl>(p => p
            .Add(x => x.Dialogname, "")
            .Add(x => x.LinksTitel, "Projekt").Add(x => x.RechtsTitel, "Katalog")
            .Add(x => x.Satz, Absatz("Felder"))
            .Add(x => x.SatzArt, Satzmarke.Katalogsatz)
            .Add(x => x.SatzName, "Katalog 1")
            .Add(x => x.ProjektWahl, projekt).Add(x => x.ProjektSichtbar, new[] { "1", "2", "3" })
            .Add(x => x.KatalogWahl, katalog).Add(x => x.KatalogSichtbar, new[] { "1", "2", "3" })
            .Add(x => x.ProjektGewaehlt, projektGewaehlt)
            .Add(x => x.KatalogGewaehlt, katalogGewaehlt)
            .Add(x => x.KnopfleisteAmSatz, true)
            .Add(x => x.VergleichProjekt, s => Satz(Satzmarke.Projektsatz, s))
            .Add(x => x.VergleichKatalog, s => Satz(Satzmarke.Katalogsatz, s))
            .Add(x => x.VergleichAbgelehnt, (string g) => abgelehnt?.Add(g)));

    private static AngleSharp.Dom.IElement Knopf(IRenderedComponent<Zweispaltenauswahl> cut)
        => cut.Find(".epos-zweispalten-satzknoepfe .epos-knopf--vergleichen");

    private static bool Gesperrt(IRenderedComponent<Zweispaltenauswahl> cut)
        => Knopf(cut).GetAttribute("aria-disabled") == "true";

    private static Bereichswahl Wahl(params string[] schluessel)
    {
        var w = new Bereichswahl();
        w.Setzen(schluessel);
        return w;
    }

    [Fact]
    public void KA_E_24_Die_Kreuze_beider_Listen_vergleichen_zusammen_mit_der_Herkunft_im_Kopf()
    {
        var cut = Aufbauen(Wahl("1"), Wahl("2"));

        Assert.False(Gesperrt(cut));
        Assert.Equal(Resource.AUSWAHL_VERGLEICH_HINWEIS, Knopf(cut).GetAttribute("title"));
        Knopf(cut).Click();

        Assert.True(cut.Instance.VergleichOffen);
        Assert.Equal(new[] { (Resource.AUSWAHL_VERGLEICH_HERKUNFT_PROJEKT, "Kessel 1"),
                             (Resource.AUSWAHL_VERGLEICH_HERKUNFT_KATALOG, "Katalog 2") },
                     cut.Instance.Vergleichskoepfe.Select(k => (k.Herkunft, k.Name)).ToArray());
        var koepfe = cut.FindAll(".epos-vergleich thead th").Skip(1).ToList();
        Assert.Equal(Resource.AUSWAHL_VERGLEICH_HERKUNFT_PROJEKT, koepfe[0].QuerySelector(".epos-vergleich-herkunft")!.TextContent);
        Assert.EndsWith("Kessel 1", koepfe[0].TextContent);
        Assert.Equal(Resource.AUSWAHL_VERGLEICH_HERKUNFT_KATALOG, koepfe[1].QuerySelector(".epos-vergleich-herkunft")!.TextContent);
        // Leistung weicht ab, Brennstoff ist stimmig.
        Assert.Equal(new[] { true, false }, cut.Instance.Vergleich.Select(v => v.Abweichend).ToArray());
        Assert.Single(cut.FindAll(".epos-vergleich--abweichend"));
    }

    [Fact]
    public void KA_E_24_Drei_Kreuze_sind_frei_vier_sind_weich_gesperrt_mit_der_Grenze()
    {
        var abgelehnt = new List<string>();
        var cut = Aufbauen(Wahl("1", "2"), Wahl("1"), abgelehnt: abgelehnt);
        Assert.False(Gesperrt(cut));

        cut = Aufbauen(Wahl("1", "2"), Wahl("1", "2"), abgelehnt: abgelehnt);
        Assert.True(Gesperrt(cut));
        Assert.False(Knopf(cut).HasAttribute("disabled"));          // fokussierbar, nennt den Grund
        Assert.Equal(Resource.AUSWAHL_VERGLEICH_GRENZE, Knopf(cut).GetAttribute("title"));
        Knopf(cut).Click();
        Assert.False(cut.Instance.VergleichOffen);
        Assert.Equal(new[] { Resource.AUSWAHL_VERGLEICH_GRENZE }, abgelehnt);
    }

    [Fact]
    public void KA_E_24_Ein_Kreuz_allein_ist_weich_gesperrt_mit_dem_Grund()
    {
        var abgelehnt = new List<string>();
        var cut = Aufbauen(Wahl("1"), Wahl(), abgelehnt: abgelehnt);

        Assert.True(Gesperrt(cut));
        Assert.Equal(Resource.AUSWAHL_VERGLEICH_SPERRE, Knopf(cut).GetAttribute("title"));
        Knopf(cut).Click();
        Assert.Equal(new[] { Resource.AUSWAHL_VERGLEICH_SPERRE }, abgelehnt);
    }

    [Fact]
    public void KA_E_24_Ohne_Kreuze_vergleicht_es_die_gewaehlte_Projektzeile_mit_dem_gewaehlten_Katalogsatz()
    {
        var projekt = Wahl();
        var katalog = Wahl();
        // nur eine Projektzeile gewaehlt: gesperrt
        var cut = Aufbauen(projekt, katalog, projektGewaehlt: "2");
        Assert.True(Gesperrt(cut));

        // dann der Katalogsatz (der Wirt fuehrt EINEN gewaehlten Satz) - die letzte Wahl beider Listen zaehlt
        cut.Render(p => p.Add(x => x.ProjektGewaehlt, (string?)null).Add(x => x.KatalogGewaehlt, "1"));
        Assert.False(Gesperrt(cut));
        Assert.Equal(Resource.AUSWAHL_VERGLEICH_PAAR_HINWEIS, Knopf(cut).GetAttribute("title"));
        Knopf(cut).Click();
        Assert.Equal(new[] { "Kessel 2", "Katalog 1" }, cut.Instance.Vergleichskoepfe.Select(k => k.Name).ToArray());

        // die Projektzeile verlaesst die Liste: wieder gesperrt
        cut.Render(p => p.Add(x => x.ProjektSichtbar, new[] { "1", "3" }));
        Assert.True(Gesperrt(cut));
    }

    [Fact]
    public void KA_E_24_Ohne_Gabe_fuehrt_der_Baustein_keinen_Vergleichsknopf()
    {
        var cut = Render<Zweispaltenauswahl>(p => p
            .Add(x => x.Dialogname, "")
            .Add(x => x.Satz, Absatz("Felder"))
            .Add(x => x.KnopfleisteAmSatz, true));
        Assert.False(cut.Instance.VergleichAmSatz);
        Assert.Empty(cut.FindAll(".epos-knopf--vergleichen"));
    }
}
