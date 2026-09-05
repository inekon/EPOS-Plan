using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// <b>Anwenderentscheid #76</b> vom 05.09.2026, nach der Windows-Abnahme:
/// „Alle Dialoge, in denen links ‚im Projekt ausgewählt‘ und rechts ‚aus der
/// Datenbank/Katalog‘ mit Pfeilknöpfen dazwischen stehen, folgen dem alten
/// BHKW-PLAN-Schema NEBENEINANDER." Auf schmalem Schirm bricht das Paar
/// untereinander um; dann gilt das Schema, das der Gebäudedialog seit W9 hatte.
///
/// <para>Geprüft wird dreierlei: der BAUSTEIN (Bereiche, Knöpfe, Sperrzustände,
/// Tastaturweg), die REGEL im Stilblatt (eine bunit-Probe sieht sie nicht —
/// Lehre W6‑B‑1) und der BESTAND (kein Dialog baut das Muster noch selbst).</para>
/// </summary>
public class ZweispaltenauswahlTests : BunitContext
{
    public ZweispaltenauswahlTests()
    {
        DeutscheOberflaeche();
    }

    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
    }

    /// <summary>Die kleinste tragfähige Probe: zwei Listen, zwei Knöpfe.</summary>
    private IRenderedComponent<Zweispaltenauswahl> Aufbauen(
        bool nurRechts = false,
        bool uebernehmenGesperrt = false,
        bool entfernenGesperrt = false,
        Action? uebernommen = null,
        Action? entfernt = null)
        => Render<Zweispaltenauswahl>(p => p
            .Add(x => x.LinksTitel, "ausgewählte Gebäude im Projekt:")
            .Add(x => x.RechtsTitel, "Gebäude in DB:")
            .Add(x => x.NurRechts, nurRechts)
            .Add(x => x.UebernehmenGesperrt, uebernehmenGesperrt)
            .Add(x => x.EntfernenGesperrt, entfernenGesperrt)
            .Add(x => x.Uebernehmen, () => uebernommen?.Invoke())
            .Add(x => x.Entfernen, () => entfernt?.Invoke())
            .Add(x => x.Links, (RenderFragment)(b =>
            {
                b.OpenElement(0, "p");
                b.AddAttribute(1, "class", "probe-links");
                b.AddContent(2, "Projektliste");
                b.CloseElement();
            }))
            .Add(x => x.Rechts, (RenderFragment)(b =>
            {
                b.OpenElement(0, "p");
                b.AddAttribute(1, "class", "probe-rechts");
                b.AddContent(2, "Katalogliste");
                b.CloseElement();
            })));

    // =====================================================================
    // Der Baustein
    // =====================================================================

    /// <summary>
    /// Drei Bereiche in der Reihenfolge links — Mitte — rechts. Die Reihenfolge
    /// IM MARKUP ist zugleich der Tastaturweg: Der Tabulator läuft von der
    /// Projektliste über die zwei Knöpfe in den Katalog.
    /// </summary>
    [Fact]
    public void Links_Mitte_Rechts_stehen_in_dieser_Reihenfolge()
    {
        var cut = Aufbauen();

        var bereiche = cut.FindAll(".epos-zweispalten > div")
                          .Select(e => e.ClassName ?? "").ToList();

        Assert.Equal(3, bereiche.Count);
        Assert.Contains("epos-zweispalten-spalte--links", bereiche[0]);
        Assert.Contains("epos-zweispalten-mitte", bereiche[1]);
        Assert.Contains("epos-zweispalten-spalte--rechts", bereiche[2]);

        Assert.Single(cut.FindAll(".epos-zweispalten-spalte--links .probe-links"));
        Assert.Single(cut.FindAll(".epos-zweispalten-spalte--rechts .probe-rechts"));
    }

    /// <summary>
    /// Jede Spalte trägt ihre Überschrift sichtbar UND als <c>aria-label</c> —
    /// eine Sprachausgabe sagt beim Wechsel, wo man ist.
    /// </summary>
    [Fact]
    public void Jede_Spalte_traegt_ihre_Ueberschrift_und_eine_aria_Beschriftung()
    {
        var cut = Aufbauen();

        IElement links = cut.Find(".epos-zweispalten-spalte--links");
        IElement rechts = cut.Find(".epos-zweispalten-spalte--rechts");

        Assert.Equal("group", links.GetAttribute("role"));
        Assert.Equal("ausgewählte Gebäude im Projekt:", links.GetAttribute("aria-label"));
        Assert.Equal("Gebäude in DB:", rechts.GetAttribute("aria-label"));

        Assert.Equal("ausgewählte Gebäude im Projekt:",
                     links.QuerySelector("h2.epos-untergruppe")!.TextContent);
        Assert.Equal("Gebäude in DB:",
                     rechts.QuerySelector("h2.epos-untergruppe")!.TextContent);

        // Auch die Knopfgruppe hat einen Namen.
        Assert.Equal("Zwischen Projekt und Datenbank verschieben",
                     cut.Find(".epos-zweispalten-mitte").GetAttribute("aria-label"));
    }

    /// <summary>
    /// <b>Der Kern des Entscheids.</b> Das Zeichen muss zur Anordnung passen, und
    /// eine Komponente weiß nicht, wie breit sie gezeichnet wird. Also stehen BEIDE
    /// Zeichen im Markup, und das Stilblatt zeigt je Breite eines — nebeneinander
    /// ◀/▶, untereinander ▲/▼. Kein JavaScript.
    /// </summary>
    [Fact]
    public void Jeder_Knopf_traegt_beide_Zeichen_und_seinen_Klartext()
    {
        var cut = Aufbauen();
        var knoepfe = cut.FindAll(".epos-zweispalten-mitte button");

        Assert.Equal(2, knoepfe.Count);

        Assert.Equal("◀", knoepfe[0].QuerySelector(".epos-zweispalten-pfeil--breit")!.TextContent);
        Assert.Equal("▲", knoepfe[0].QuerySelector(".epos-zweispalten-pfeil--schmal")!.TextContent);
        Assert.Equal("▶", knoepfe[1].QuerySelector(".epos-zweispalten-pfeil--breit")!.TextContent);
        Assert.Equal("▼", knoepfe[1].QuerySelector(".epos-zweispalten-pfeil--schmal")!.TextContent);

        Assert.Equal("In das Projekt übernehmen",
                     knoepfe[0].QuerySelector(".epos-zweispalten-knopftext")!.TextContent);
        Assert.Equal("Aus dem Projekt entfernen",
                     knoepfe[1].QuerySelector(".epos-zweispalten-knopftext")!.TextContent);

        // Das Zeichen ist Beiwerk - eine Sprachausgabe liest den Satz.
        foreach (IElement pfeil in cut.FindAll(".epos-zweispalten-pfeil"))
            Assert.Equal("true", pfeil.GetAttribute("aria-hidden"));
    }

    /// <summary>Der Kurztext nennt die HERKUNFT der Zeile, nicht nur die Aufgabe.</summary>
    [Fact]
    public void Beide_Knoepfe_tragen_einen_Kurztext()
    {
        var knoepfe = Aufbauen().FindAll(".epos-zweispalten-mitte button");

        Assert.Contains("Datenbankliste", knoepfe[0].GetAttribute("title") ?? "");
        Assert.Contains("Projektliste", knoepfe[1].GetAttribute("title") ?? "");
    }

    [Fact]
    public void Ohne_Markierung_ist_der_jeweilige_Knopf_gesperrt()
    {
        var cut = Aufbauen(uebernehmenGesperrt: true);
        var knoepfe = cut.FindAll(".epos-zweispalten-mitte button");

        Assert.True(knoepfe[0].HasAttribute("disabled"));
        Assert.False(knoepfe[1].HasAttribute("disabled"));

        cut = Aufbauen(entfernenGesperrt: true);
        knoepfe = cut.FindAll(".epos-zweispalten-mitte button");

        Assert.False(knoepfe[0].HasAttribute("disabled"));
        Assert.True(knoepfe[1].HasAttribute("disabled"));
    }

    [Fact]
    public void Jeder_Knopf_meldet_seinen_Klick()
    {
        int hin = 0, weg = 0;
        var cut = Aufbauen(uebernommen: () => hin++, entfernt: () => weg++);

        cut.FindAll(".epos-zweispalten-mitte button")[0].Click();
        cut.FindAll(".epos-zweispalten-mitte button")[1].Click();

        Assert.Equal(1, hin);
        Assert.Equal(1, weg);
    }

    /// <summary>
    /// Die Verwaltungsbetriebsart des Gebäudedialogs (Form_Gebaeude_Load:608‑620):
    /// kein Projekt, also keine Projektliste und keine Pfeile.
    /// </summary>
    [Fact]
    public void NurRechts_laesst_die_linke_Spalte_und_die_Pfeile_weg()
    {
        var cut = Aufbauen(nurRechts: true);

        Assert.Empty(cut.FindAll(".epos-zweispalten-spalte--links"));
        Assert.Empty(cut.FindAll(".epos-zweispalten-mitte"));
        Assert.Single(cut.FindAll(".epos-zweispalten-spalte--rechts .probe-rechts"));
    }

    // =====================================================================
    // Die Regel im Stilblatt - eine bunit-Probe sieht sie nicht (Lehre W6-B-1)
    // =====================================================================

    /// <summary>
    /// Nebeneinander ist die VORGABE, untereinander der Ausnahmefall auf schmalem
    /// Schirm. Vorher war es umgekehrt.
    /// </summary>
    [Fact]
    public void Breit_stehen_die_Spalten_nebeneinander_schmal_untereinander()
    {
        Assert.Contains("flex-direction: row", Stilblock(".epos-zweispalten {"));
        Assert.Contains("flex-direction: column", Umbruchblock(".epos-zweispalten {"));
    }

    /// <summary>
    /// Der Umbruch ist eine MEDIENABFRAGE und kein <c>flex-wrap</c>: Nur so weiß
    /// das Blatt, welches Pfeilzeichen gerade gilt. Bei <c>flex-wrap</c> käme die
    /// Reihe um, ohne dass eine Regel es merkt.
    /// </summary>
    [Fact]
    public void Der_Wirt_bricht_nicht_von_selbst_um()
    {
        Assert.DoesNotContain("flex-wrap", Stilblock(".epos-zweispalten {"));
    }

    /// <summary>
    /// Die Umbruchbreite steht als Token in <c>:root</c> (Hausregel „Eine Farbe
    /// steht als Token") und — weil eine Medienabfrage kein Token lesen kann —
    /// ein zweites Mal in der Abfrage selbst. Diese Probe hält beide gegeneinander.
    /// </summary>
    [Fact]
    public void Die_Umbruchbreite_steht_als_Token()
    {
        string wurzel = Stilblock(":root {");
        Match token = Regex.Match(wurzel, @"--epos-zweispalten-umbruch:\s*(\d+)px;");
        Assert.True(token.Success, "Das Token --epos-zweispalten-umbruch fehlt in :root");

        Match abfrage = Regex.Match(Stilblatt(),
            @"ZWEISPALTENAUSWAHL.*?@media \(max-width:\s*(\d+)px\)", RegexOptions.Singleline);
        Assert.True(abfrage.Success, "Die Medienabfrage des Blocks Zweispaltenauswahl fehlt");

        Assert.Equal(token.Groups[1].Value, abfrage.Groups[1].Value);
    }

    /// <summary>
    /// Die Mittelspalte bleibt SCHMAL — im Vorbild 63 px (Form_Gebaeude) bis 88 px
    /// (Form_Heizkessel). Ihre Breite steht als Token, nicht als Zahl in der Regel.
    /// </summary>
    [Fact]
    public void Die_Mittelspalte_nimmt_ihre_Breite_aus_einem_Token()
    {
        Assert.Contains("--epos-zweispalten-mitte:", Stilblock(":root {"));
        Assert.Contains("width: var(--epos-zweispalten-mitte)",
                        Stilblock(".epos-zweispalten-mitte {"));
    }

    /// <summary>
    /// Je Anordnung ist genau EIN Zeichen zu sehen: breit die waagerechten,
    /// schmal die senkrechten.
    /// </summary>
    [Fact]
    public void Je_Anordnung_ist_genau_ein_Zeichen_sichtbar()
    {
        Assert.Contains("display: none", Stilblock(".epos-zweispalten-pfeil--schmal {"));
        Assert.Contains("display: none", Umbruchblock(".epos-zweispalten-pfeil--breit {"));
        Assert.Contains("display: inline", Umbruchblock(".epos-zweispalten-pfeil--schmal {"));
    }

    // =====================================================================
    // Der Bestand - kein Dialog baut das Muster noch selbst
    // =====================================================================

    /// <summary>
    /// Die elf Projekt/Datenbank-Dialoge des Hauses. Wer einen zwölften baut,
    /// nimmt den Baustein — und trägt ihn hier ein.
    /// </summary>
    private static readonly string[] Dialoge =
    {
        "Dialoge/Bedarf/GebaeudeDialog.razor",
        "Dialoge/Bedarf/WaermebedarfExternDialog.razor",
        "Dialoge/Bedarf/BedarfsProfileDialog.razor",
        "Dialoge/Erzeuger/HeizkesselDialog.razor",
        "Dialoge/Erzeuger/BhkwDialog.razor",
        "Dialoge/Erzeuger/PhotovoltaikDialog.razor",
        "Dialoge/Erzeuger/PufferspeicherDialog.razor",
        "Dialoge/Erzeuger/StromspeicherDialog.razor",
        "Dialoge/Solarthermie/SolarkollektorenDialog.razor",
        "Dialoge/Solarthermie/SolarganglinieDialog.razor",
        "Dialoge/Strom/StromganglinieDialog.razor",
    };

    [Fact]
    public void Alle_elf_Projekt_DB_Dialoge_nehmen_den_Baustein()
    {
        foreach (string d in Dialoge)
        {
            string quelle = File.ReadAllText(Path.Combine(Wurzel(), "EPOS.UI", d));
            Assert.Contains("<Zweispaltenauswahl", quelle);
        }
    }

    /// <summary>
    /// Die alte Pfeilspalte ist weg — sonst gäbe es zwei Fassungen desselben
    /// Musters, und die eine bekäme den nächsten Anwenderwunsch nicht mit.
    /// </summary>
    [Fact]
    public void Keine_Komponente_baut_die_Pfeilspalte_noch_selbst()
    {
        var uebrig = Directory
            .EnumerateFiles(Path.Combine(Wurzel(), "EPOS.UI"), "*.razor", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f).Contains("epos-auswahlpfeile", StringComparison.Ordinal))
            .ToList();

        Assert.Empty(uebrig);
        Assert.DoesNotContain("epos-auswahlpfeile", Stilblatt());
    }

    // =====================================================================
    // Hilfen
    // =====================================================================

    private static string Wurzel()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        return d!.FullName;
    }

    private static string Stilblatt()
        => File.ReadAllText(Path.Combine(Wurzel(), "EPOS.UI", "wwwroot", "epos-ui.css"));

    /// <summary>Liest den Rumpf einer Regel aus dem Stilblatt.</summary>
    private static string Stilblock(string selektor) => Block(Stilblatt(), selektor);

    /// <summary>
    /// Liest den Rumpf einer Regel aus der Medienabfrage des Blocks
    /// „Zweispaltenauswahl" — also aus der Fassung für den schmalen Schirm.
    /// </summary>
    private static string Umbruchblock(string selektor)
    {
        string css = Stilblatt();
        int a = css.IndexOf("ZWEISPALTENAUSWAHL", StringComparison.Ordinal);
        Assert.True(a >= 0, "Der Block Zweispaltenauswahl steht nicht im Stilblatt");
        int m = css.IndexOf("@media (max-width:", a, StringComparison.Ordinal);
        Assert.True(m > a, "Die Medienabfrage des Blocks Zweispaltenauswahl fehlt");
        return Block(css.Substring(m), selektor);
    }

    private static string Block(string css, string selektor)
    {
        int a = css.IndexOf(selektor, StringComparison.Ordinal);
        Assert.True(a >= 0, $"Regel {selektor} steht nicht im Stilblatt");
        int e = css.IndexOf('}', a);
        Assert.True(e > a);
        return css.Substring(a + selektor.Length, e - a - selektor.Length);
    }
}
