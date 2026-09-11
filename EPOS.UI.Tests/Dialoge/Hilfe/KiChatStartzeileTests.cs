using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Hilfe;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Hilfe;

/// <summary>
/// Die STARTZEILE des Chats (Auftrag #227, Anwenderhinweis 11.09.2026: „Es
/// könnte im Hilfedialog ‚erkläre aktuellen Dialog' oder ähnliches stehen. Sonst
/// sind die beiden KI Buttons nicht einsichtig.").
///
/// <para>Was hier bewiesen wird: Öffnet sich der Assistent MIT einem
/// Aufrufkontext (Hilfeschlüssel gesetzt), zeigt der leere Verlauf eine
/// Startzeile mit dem Kontext links und zwei Knöpfen rechts; ein Klick sendet
/// die passende Frage als Anwendernachricht — die vorbereitete, wenn es eine
/// gibt, sonst eine allgemeine mit dem Bildschirmnamen. Ohne Hilfeschlüssel
/// (der Menüweg „Hilfe › Assistent") bleibt sie weg, und mit der ersten
/// Nachricht verschwindet sie. Ohne Einrichtung des Assistenten (KI‑D‑Q1)
/// bleiben die Knöpfe eine WEICHE Sperre mit Hinweis.</para>
/// </summary>
public class KiChatStartzeileTests : EposBunitContext
{
    public KiChatStartzeileTests()
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static KiChatTexte Texte() => new()
    {
        Verlauf = "Hilfe-Assistent",
        VerlaufLeer = "Noch keine Frage gestellt.",
        KontextFormat = "Kontext: {0}",
        KontextLeer = "Kontext: (nicht erkannt)",
        DialognameFormat = "Dialog: {0}",
        MeldungFormat = "Meldung: {0}",
        Eingabe = "Ihre Frage",
        Fragen = "Fragen",
        Suchen = "Nur suchen",
        Aktionen = "Aktionen zulassen",
        Werkzeuge = "Werkzeuge...",
        HinweisVorn = "Es werden nur Hilfetexte übertragen. ",
        Doku = "Online-Dokumentation öffnen",
        Schliessen = "Schließen",
        StartzeileErklaeren = "Aktuellen Dialog erklären",
        StartzeileMoeglich = "Was kann ich hier tun?",
        StartzeileFrageErklaerenFormat = "Erkläre mir „{0}“.",
        StartzeileFrageMoeglichFormat = "Was kann ich in „{0}“ tun?",
        StartzeileBildschirm = "diesen Bildschirm",
        StartzeileGesperrt = "Richten Sie zuerst den Assistenten ein."
    };

    private IRenderedComponent<KiChatDialog> Zeigen(
        string kontext = "Bereich: Simulation",
        string dialogname = "",
        string vorbelegung = "",
        string hilfeschluessel = "Form_Simulation_Detail.btn_Help",
        bool eingerichtet = true,
        Func<string, bool, bool, Task<IReadOnlyList<Gespraechszeile>>>? fragen = null)
        => Render<KiChatDialog>(p =>
        {
            p.Add(x => x.Texte, Texte())
             .Add(x => x.Kontext, kontext)
             .Add(x => x.Dialogname, dialogname)
             .Add(x => x.Vorbelegung, vorbelegung)
             .Add(x => x.Hilfeschluessel, hilfeschluessel)
             .Add(x => x.Eingerichtet, eingerichtet)
             .Add(x => x.AnfragenHeute, 3)
             .Add(x => x.Tageslimit, 50);
            if (fragen is not null) p.Add(x => x.Fragen, fragen);
        });

    private static Func<string, bool, bool, Task<IReadOnlyList<Gespraechszeile>>> Aufzeichner(
        List<string> gefragt)
        => (f, _, _) =>
        {
            gefragt.Add(f);
            return Task.FromResult<IReadOnlyList<Gespraechszeile>>(Array.Empty<Gespraechszeile>());
        };

    // ==================================================================
    //  Sichtbarkeit
    // ==================================================================

    [Fact]
    public void Mit_Aufrufkontext_und_leerem_Verlauf_steht_die_Startzeile()
    {
        var cut = Zeigen();

        var zeile = cut.Find(".epos-kichat-startzeile");
        Assert.Equal("Kontext: Bereich: Simulation",
                     zeile.QuerySelector(".epos-kichat-startzeile-kontext")!.TextContent.Trim());

        var knoepfe = zeile.QuerySelectorAll(".epos-kichat-startzeile-knoepfe button");
        Assert.Equal(new[] { "Aktuellen Dialog erklären", "Was kann ich hier tun?" },
                     knoepfe.Select(b => b.TextContent.Trim()));
    }

    [Fact]
    public void Ohne_Hilfeschluessel_steht_keine_Startzeile()
    {
        // Der Menueweg "Hilfe > Assistent" meldet einen Bereich, aber keinen
        // Hilfeschluessel - genau der Unterschied zum Aufruf aus einem Dialog.
        var cut = Zeigen(hilfeschluessel: "");

        Assert.Empty(cut.FindAll(".epos-kichat-startzeile"));
    }

    [Fact]
    public void Nach_der_ersten_Nachricht_verschwindet_die_Startzeile()
    {
        var gefragt = new List<string>();
        var cut = Zeigen(fragen: Aufzeichner(gefragt));

        cut.Find(".epos-kichat-startzeile-knoepfe button").Click();

        Assert.Single(gefragt);
        Assert.Empty(cut.FindAll(".epos-kichat-startzeile"));
    }

    // ==================================================================
    //  Die zwei Fragen
    // ==================================================================

    [Fact]
    public void Klick_auf_Erklaeren_sendet_die_vorbereitete_Frage_des_Kontexts()
    {
        var gefragt = new List<string>();
        var cut = Zeigen(vorbelegung: "Warum deckt die Kaskade den Bedarf nur teilweise?",
                         fragen: Aufzeichner(gefragt));

        cut.FindAll(".epos-kichat-startzeile-knoepfe button")[0].Click();

        Assert.Equal(new[] { "Warum deckt die Kaskade den Bedarf nur teilweise?" }, gefragt);
    }

    [Fact]
    public void Ohne_vorbereitete_Frage_sendet_Erklaeren_die_allgemeine_Frage_mit_dem_Bildschirmnamen()
    {
        var gefragt = new List<string>();
        var cut = Zeigen(dialogname: "Simulation · 3 Ergebnis · Stromspeicher",
                         fragen: Aufzeichner(gefragt));

        cut.FindAll(".epos-kichat-startzeile-knoepfe button")[0].Click();

        Assert.Equal(new[] { "Erkläre mir „Simulation · 3 Ergebnis · Stromspeicher“." }, gefragt);
    }

    [Fact]
    public void Klick_auf_MoeglichKnopf_sendet_eine_ANDERE_neue_Frage()
    {
        var gefragt = new List<string>();
        var cut = Zeigen(dialogname: "Heizkessel bearbeiten", vorbelegung: "Vorbereitete Frage",
                         fragen: Aufzeichner(gefragt));

        cut.FindAll(".epos-kichat-startzeile-knoepfe button")[1].Click();

        Assert.Equal(new[] { "Was kann ich in „Heizkessel bearbeiten“ tun?" }, gefragt);
    }

    [Fact]
    public void Ohne_Dialogname_und_Bereich_greift_der_Bildschirm_Rueckfall()
    {
        var gefragt = new List<string>();
        var cut = Zeigen(kontext: "", dialogname: "", fragen: Aufzeichner(gefragt));

        cut.FindAll(".epos-kichat-startzeile-knoepfe button")[0].Click();

        Assert.Equal(new[] { "Erkläre mir „diesen Bildschirm“." }, gefragt);
    }

    // ==================================================================
    //  Ohne Einrichtung (KI-D-Q1)
    // ==================================================================

    [Fact]
    public void Ohne_Einrichtung_tragen_die_Knoepfe_die_weiche_Sperre_und_senden_nichts()
    {
        var gefragt = new List<string>();
        var cut = Zeigen(eingerichtet: false, fragen: Aufzeichner(gefragt));

        var knoepfe = cut.FindAll(".epos-kichat-startzeile-knoepfe button");
        Assert.All(knoepfe, b =>
        {
            Assert.Equal("true", b.GetAttribute("aria-disabled"));
            Assert.Equal("Richten Sie zuerst den Assistenten ein.", b.GetAttribute("title"));
        });

        knoepfe[0].Click();
        knoepfe[1].Click();

        Assert.Empty(gefragt);
        // Die Startzeile bleibt stehen - es wurde ja nichts gesendet.
        Assert.NotEmpty(cut.FindAll(".epos-kichat-startzeile"));
    }

    [Fact]
    public void Mit_Einrichtung_tragen_die_Knoepfe_keine_Sperre()
    {
        var cut = Zeigen(eingerichtet: true);

        var knoepfe = cut.FindAll(".epos-kichat-startzeile-knoepfe button");
        Assert.All(knoepfe, b =>
        {
            Assert.Null(b.GetAttribute("aria-disabled"));
            Assert.Null(b.GetAttribute("title"));
        });
    }
}
