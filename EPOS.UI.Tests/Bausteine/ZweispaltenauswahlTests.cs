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
/// <b>Anwenderentscheid #76</b> vom 05.09.2026, nach der Windows-Abnahme — zwei
/// Listen mit Pfeilknöpfen dazwischen, nach dem alten BHKW-PLAN-Schema —,
/// <b>in seiner ANORDNUNG geändert durch W14a-E-10-Q2</b> vom 07.09.2026: Die
/// zwei Listen stehen seither <b>untereinander</b>, oben die kurze Projektliste,
/// darunter die Übernahmeleiste, darunter der Katalog über die ganze Breite
/// („Liste wie zuvor über ganze Breite, sonst zu schmale Liste"). #76 bleibt in
/// seiner Aussage: zwei Listen, Übernahmeknöpfe dazwischen, der Filter über der
/// Liste, auf die er wirkt.
///
/// <para>Geprüft wird dreierlei: der BAUSTEIN (Bereiche, Knöpfe, Sperrzustände,
/// Tastaturweg), die REGEL im Stilblatt (eine bunit-Probe sieht sie nicht —
/// Lehre W6-B-1) und der BESTAND (kein Dialog baut das Muster noch selbst, und
/// alle elf Wirte nehmen ihn).</para>
/// </summary>
public class ZweispaltenauswahlTests : EposBunitContext
{
    public ZweispaltenauswahlTests()
    {
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
    /// Drei Bereiche in der Reihenfolge oben — Übernahmeleiste — unten. Die
    /// Reihenfolge IM MARKUP ist zugleich der Tastaturweg: Der Tabulator läuft
    /// von der Projektliste über die zwei Knöpfe in den Katalog.
    /// </summary>
    [Fact]
    public void Oben_Uebernahmeleiste_Unten_stehen_in_dieser_Reihenfolge()
    {
        var cut = Aufbauen();

        var bereiche = cut.FindAll(".epos-zweispalten > div")
                          .Select(e => e.ClassName ?? "").ToList();

        Assert.Equal(3, bereiche.Count);
        Assert.Contains("epos-zweispalten-spalte--oben", bereiche[0]);
        Assert.Contains("epos-zweispalten-uebernahme", bereiche[1]);
        Assert.Contains("epos-zweispalten-spalte--unten", bereiche[2]);

        Assert.Single(cut.FindAll(".epos-zweispalten-spalte--oben .probe-links"));
        Assert.Single(cut.FindAll(".epos-zweispalten-spalte--unten .probe-rechts"));
    }

    /// <summary>
    /// Jede Spalte trägt ihre Überschrift sichtbar UND als <c>aria-label</c> —
    /// eine Sprachausgabe sagt beim Wechsel, wo man ist.
    /// </summary>
    [Fact]
    public void Jede_Spalte_traegt_ihre_Ueberschrift_und_eine_aria_Beschriftung()
    {
        var cut = Aufbauen();

        IElement links = cut.Find(".epos-zweispalten-spalte--oben");
        IElement rechts = cut.Find(".epos-zweispalten-spalte--unten");

        Assert.Equal("group", links.GetAttribute("role"));
        Assert.Equal("ausgewählte Gebäude im Projekt:", links.GetAttribute("aria-label"));
        Assert.Equal("Gebäude in DB:", rechts.GetAttribute("aria-label"));

        Assert.Equal("ausgewählte Gebäude im Projekt:",
                     links.QuerySelector("h2.epos-untergruppe")!.TextContent);
        Assert.Equal("Gebäude in DB:",
                     rechts.QuerySelector("h2.epos-untergruppe")!.TextContent);

        // Auch die Knopfgruppe hat einen Namen.
        Assert.Equal("Zwischen Projekt und Datenbank verschieben",
                     cut.Find(".epos-zweispalten-uebernahme").GetAttribute("aria-label"));
    }

    /// <summary>
    /// <b>EIN Zeichen je Knopf</b> — und das ist der Gewinn aus Q2. Bis dahin trug
    /// jeder Knopf BEIDE Paare im Markup (◀▶ nebeneinander, ▲▼ untereinander),
    /// weil eine Komponente nicht weiß, wie breit sie gezeichnet wird, und das
    /// Stilblatt je Breite eines zeigte. Bei EINER Anordnung braucht es das nicht
    /// mehr: ▲ nach oben in das Projekt, ▼ nach unten in den Katalog.
    /// </summary>
    [Fact]
    public void Jeder_Knopf_traegt_ein_Zeichen_und_seinen_Klartext()
    {
        var cut = Aufbauen();
        var knoepfe = cut.FindAll(".epos-zweispalten-uebernahme button");

        Assert.Equal(2, knoepfe.Count);

        Assert.Equal("▲", knoepfe[0].QuerySelector(".epos-zweispalten-pfeil")!.TextContent);
        Assert.Equal("▼", knoepfe[1].QuerySelector(".epos-zweispalten-pfeil")!.TextContent);

        // Je Knopf GENAU EIN Zeichen - das waagerechte Paar ist mit Q2 gefallen.
        Assert.Single(knoepfe[0].QuerySelectorAll(".epos-zweispalten-pfeil"));
        Assert.Single(knoepfe[1].QuerySelectorAll(".epos-zweispalten-pfeil"));
        Assert.Empty(cut.FindAll(".epos-zweispalten-pfeil--breit"));
        Assert.Empty(cut.FindAll(".epos-zweispalten-pfeil--schmal"));

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
        var knoepfe = Aufbauen().FindAll(".epos-zweispalten-uebernahme button");

        Assert.Contains("Datenbankliste", knoepfe[0].GetAttribute("title") ?? "");
        Assert.Contains("Projektliste", knoepfe[1].GetAttribute("title") ?? "");
    }

    [Fact]
    public void Ohne_Markierung_ist_der_jeweilige_Knopf_gesperrt()
    {
        var cut = Aufbauen(uebernehmenGesperrt: true);
        var knoepfe = cut.FindAll(".epos-zweispalten-uebernahme button");

        Assert.True(knoepfe[0].HasAttribute("disabled"));
        Assert.False(knoepfe[1].HasAttribute("disabled"));

        cut = Aufbauen(entfernenGesperrt: true);
        knoepfe = cut.FindAll(".epos-zweispalten-uebernahme button");

        Assert.False(knoepfe[0].HasAttribute("disabled"));
        Assert.True(knoepfe[1].HasAttribute("disabled"));
    }

    [Fact]
    public void Jeder_Knopf_meldet_seinen_Klick()
    {
        int hin = 0, weg = 0;
        var cut = Aufbauen(uebernommen: () => hin++, entfernt: () => weg++);

        cut.FindAll(".epos-zweispalten-uebernahme button")[0].Click();
        cut.FindAll(".epos-zweispalten-uebernahme button")[1].Click();

        Assert.Equal(1, hin);
        Assert.Equal(1, weg);
    }

    /// <summary>
    /// Die Verwaltungsbetriebsart des Gebäudedialogs (Form_Gebaeude_Load:608‑620):
    /// kein Projekt, also keine Projektliste und keine Pfeile.
    /// </summary>
    [Fact]
    public void NurRechts_laesst_den_oberen_Block_und_die_Uebernahmeleiste_weg()
    {
        var cut = Aufbauen(nurRechts: true);

        Assert.Empty(cut.FindAll(".epos-zweispalten-spalte--oben"));
        Assert.Empty(cut.FindAll(".epos-zweispalten-uebernahme"));
        Assert.Single(cut.FindAll(".epos-zweispalten-spalte--unten .probe-rechts"));
    }

    // =====================================================================
    // Die Regel im Stilblatt - eine bunit-Probe sieht sie nicht (Lehre W6-B-1)
    // =====================================================================

    /// <summary>
    /// <b>Untereinander ist die EINZIGE Anordnung</b> (Q2). Bis dahin war
    /// nebeneinander die Vorgabe und untereinander der Ausnahmefall auf schmalem
    /// Schirm; die Ausnahme ist die Regel geworden — dieselbe Bewegung, die
    /// S1.7 am <c>Katalograhmen</c> gemacht hat.
    /// </summary>
    [Fact]
    public void Die_zwei_Listen_stehen_untereinander()
    {
        Assert.Contains("flex-direction: column", Stilblock(".epos-zweispalten {"));
        Assert.DoesNotContain("flex-direction: row", Stilblock(".epos-zweispalten {"));
    }

    /// <summary>
    /// <b>Die Medienabfrage ist gefallen.</b> Es gibt nur noch EINE Anordnung —
    /// eine zweite wäre ein zweites Bild derselben Maske, und der Anwender hat
    /// gerade verlangt, dass jeder Katalogdialog gleich aussieht.
    /// </summary>
    [Fact]
    public void Der_Block_traegt_keine_Medienabfrage_mehr()
    {
        string css = Stilblatt();
        int a = css.IndexOf("ZWEISPALTENAUSWAHL", StringComparison.Ordinal);
        Assert.True(a >= 0, "Der Block Zweispaltenauswahl steht nicht im Stilblatt");

        // Bis zum naechsten Blockkopf darf keine Medienabfrage stehen.
        int e = css.IndexOf("/* ====", a + 20, StringComparison.Ordinal);
        string block = e > a ? css.Substring(a, e - a) : css.Substring(a);

        Assert.DoesNotContain("@media", block);
        Assert.DoesNotContain("flex-wrap", Stilblock(".epos-zweispalten {"));
    }

    /// <summary>
    /// <b>Die Mittelspalte ist gefallen.</b> Mit ihr das Token
    /// <c>--epos-zweispalten-mitte</c> und die zwei waagerechten Pfeilklassen —
    /// der Baustein ist mit Q2 kleiner geworden, nicht größer. Das Token
    /// <c>--epos-zweispalten-umbruch</c> BLEIBT: Formularraster, Dublettenbaum
    /// und Kennzahlzeile benutzen es weiter.
    /// </summary>
    [Fact]
    public void Die_Mittelspalte_und_ihre_Klassen_sind_gefallen()
    {
        string css = Stilblatt();

        Assert.DoesNotContain(".epos-zweispalten-mitte {", css);
        Assert.DoesNotContain(".epos-zweispalten-pfeil--breit", css);
        Assert.DoesNotContain(".epos-zweispalten-pfeil--schmal", css);

        Assert.Contains("--epos-zweispalten-umbruch:", Stilblock(":root {"));
    }

    /// <summary>
    /// <b>Die Projektliste ist höhenbegrenzt</b> (Konzept_Katalogfilter 5.6.5):
    /// 12 rem = 192 px, bei 45 px Kopfzelle und 37 px Zeilenhöhe VIER Zeilen.
    /// Ohne die Grenze schöbe eine lange Projektliste den Katalog beliebig weit
    /// nach unten — genau der Grund, aus dem die Katalogliste ihre eigene Grenze
    /// aus dem <c>Katalograhmen</c> mitbringt. Die Zahl steht als Token, nicht
    /// als Zahl in der Regel.
    /// </summary>
    [Fact]
    public void Die_Projektliste_ist_hoehenbegrenzt()
    {
        Assert.Contains("--epos-projektlistenhoehe: 12rem;", Stilblock(":root {"));
        Assert.Contains("max-height: var(--epos-projektlistenhoehe)",
                        Stilblock(".epos-zweispalten-spalte--oben .epos-raster-huelle {"));
    }

    /// <summary>
    /// Die Übernahmeleiste ist eine ZEILE und bricht um wie jede Knopfleiste des
    /// Hauses (Befund W12‑B‑1) — sie war bis Q2 eine schmale Spalte.
    /// </summary>
    [Fact]
    public void Die_Uebernahmeleiste_ist_eine_Zeile_und_bricht_um()
    {
        string leiste = Stilblock(".epos-zweispalten-uebernahme {");

        Assert.Contains("flex-direction: row", leiste);
        Assert.Contains("flex-wrap: wrap", leiste);
        Assert.DoesNotContain("width:", leiste);
    }

    // =====================================================================
    // Die Knopfleiste unter der Katalogliste (Befund W12-B-1)
    // =====================================================================

    /// <summary>
    /// <b>Die Regel zu Befund W12‑B‑1.</b> Die Knopfleiste bricht um, und ein
    /// Knopf bemisst sich an seiner Beschriftung — beides steht EINMAL im Blatt
    /// (<c>.epos-leiste</c> / <c>.epos-knopf</c>) und gilt damit für alle elf
    /// Projekt/DB-Dialoge wie für die Katalogverwaltungen.
    ///
    /// <para>Vier Dinge dürfen NICHT wiederkommen: eine Reihe ohne Umbruch
    /// (dann schrumpfen die Knöpfe unter ihren Text), <c>white-space: nowrap</c>
    /// am Knopf (dann kann der Text im Knopf nicht umbrechen),
    /// <c>overflow: hidden</c> (das schneidet die Beschriftung ab, statt sie zu
    /// zeigen) und eine feste <c>width</c> (dann bemisst sich der Knopf nicht
    /// mehr an seiner Beschriftung — <c>min-width</c> bleibt erlaubt, es ist
    /// ein Mindestmaß).</para>
    /// </summary>
    [Fact]
    public void Die_Knopfleiste_bricht_um_statt_die_Beschriftung_abzuschneiden()
    {
        string leiste = Stilblock(".epos-leiste {");
        Assert.Contains("flex-wrap: wrap", leiste);
        Assert.Contains("gap:", leiste);            // Abstand ueber gap, nicht margin

        string knopf = Stilblock(".epos-knopf {");
        Assert.Contains("flex: 0 1 auto", knopf);   // bemisst sich am Text
        Assert.Contains("white-space: normal", knopf);
        Assert.Contains("overflow-wrap: break-word", knopf);

        Assert.DoesNotContain("white-space: nowrap", knopf);
        Assert.DoesNotContain("overflow: hidden", knopf);

        // min-width und max-width bleiben erlaubt; eine blanke width nicht.
        Assert.False(Regex.IsMatch(knopf, @"(?<![\w-])width\s*:"),
                     "Der Hausknopf traegt eine feste Breite - er soll sich an "
                     + "seiner Beschriftung bemessen (Befund W12-B-1).");
    }

    /// <summary>
    /// <b>Befund W12‑B‑1</b> vom 05.09.2026 (Windows-Abnahme, Dialog „Standard
    /// Stromprofil"): „Beschriftung der Buttons nicht zur Umrandung passen."
    /// Unter der rechten Liste stehen dort VIER Knöpfe — „Stromverbraucher
    /// ändern…", „… neu…", „… löschen", „Typ in DB ändern…" — in einer Spalte,
    /// die nur die halbe Dialogbreite hat. Der Text lief über den Rahmen in den
    /// Nachbarknopf, die Umrandung lag mitten im Wort.
    ///
    /// <para>Diese Probe hält das MARKUP fest: Die Knöpfe, die ein Aufrufer
    /// unter seine Katalogliste setzt, landen in der rechten Spalte des
    /// Bausteins und stehen dort in <c>.epos-leiste</c> — genau der Klasse, an
    /// der die Umbruchregel hängt. Die Regel selbst prüft
    /// <see cref="Die_Knopfleiste_bricht_um_statt_die_Beschriftung_abzuschneiden"/>;
    /// eine bunit-Probe sieht ein Stilblatt nicht (Lehre W6‑B‑1).</para>
    /// </summary>
    [Fact]
    public void Die_Knopfleiste_unter_der_Katalogliste_traegt_die_Leistenklasse()
    {
        string[] beschriftungen =
        {
            "Stromverbraucher ändern...",
            "Stromverbraucher neu...",
            "Stromverbraucher löschen",
            "Typ in DB ändern...",
        };

        var cut = Render<Zweispaltenauswahl>(p => p
            .Add(x => x.LinksTitel, "Projekt Strombedarf:")
            .Add(x => x.RechtsTitel, "Datenbank Strombedarf:")
            .Add(x => x.Rechts, (RenderFragment)(b =>
            {
                b.OpenElement(0, "div");
                b.AddAttribute(1, "class", "epos-leiste");
                int schluessel = 2;
                foreach (string text in beschriftungen)
                {
                    b.OpenElement(schluessel++, "button");
                    b.AddAttribute(schluessel++, "type", "button");
                    b.AddAttribute(schluessel++, "class", "epos-knopf");
                    b.AddContent(schluessel++, text);
                    b.CloseElement();
                }
                b.CloseElement();
            })));

        // Die Leiste steht IM UNTEREN Block - nicht daneben, nicht im Wirt.
        IElement leiste = cut.Find(".epos-zweispalten-spalte--unten > .epos-leiste");

        // ... und alle vier Knöpfe stehen in dieser einen Leiste.
        var knoepfe = leiste.QuerySelectorAll("button.epos-knopf");
        Assert.Equal(beschriftungen.Length, knoepfe.Length);
        Assert.Equal(beschriftungen, knoepfe.Select(k => k.TextContent).ToArray());

        // Kein Knopf trägt eine Breite am Element - die Bemessung gehört ins
        // Stilblatt, und ein Inline-Stil wäre gegen die Hausregel.
        Assert.All(knoepfe, k => Assert.Null(k.GetAttribute("style")));
    }

    /// <summary>
    /// Dieselbe Leiste in allen elf Projekt/DB-Dialogen: Wer Katalogknöpfe unter
    /// seine rechte Liste setzt, nimmt <c>.epos-leiste</c>. Sonst gäbe es eine
    /// zweite Knopfleiste, und die bekäme den Umbruch aus W12‑B‑1 nicht mit.
    /// </summary>
    [Fact]
    public void Jeder_Dialog_setzt_seine_Katalogknoepfe_in_eine_epos_leiste()
    {
        foreach (string d in Dialoge)
        {
            string quelle = File.ReadAllText(Path.Combine(Wurzel(), "EPOS.UI", d))
                                .Replace("\r\n", "\n");

            int a = quelle.IndexOf("<Rechts>", StringComparison.Ordinal);
            int e = quelle.IndexOf("</Rechts>", StringComparison.Ordinal);
            Assert.True(a >= 0 && e > a, d + ": kein <Rechts>-Block");

            string rechts = quelle.Substring(a, e - a);
            if (!rechts.Contains("epos-knopf", StringComparison.Ordinal))
                continue;                       // Dialog ohne Katalogknoepfe

            Assert.Contains("class=\"epos-leiste\"", rechts, StringComparison.Ordinal);
        }
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

    /// <summary>
    /// Das Hausblatt mit angeglichenen Zeilenenden: Auf Windows liegt es nach
    /// dem Auschecken mit CRLF (.gitattributes: text=auto), ein zweizeiliger
    /// Selektor traegt hier aber "\n" - dieselbe Angleichung wie in
    /// StartseiteTests und StilblattTests.
    /// </summary>
    private static string Stilblatt()
        => File.ReadAllText(Path.Combine(Wurzel(), "EPOS.UI", "wwwroot", "epos-ui.css"))
               .Replace("\r\n", "\n");

    /// <summary>Liest den Rumpf einer Regel aus dem Stilblatt.</summary>
    private static string Stilblock(string selektor)
        => Block(Stilblatt(), selektor.Replace("\r\n", "\n"));

    private static string Block(string css, string selektor)
    {
        int a = css.IndexOf(selektor, StringComparison.Ordinal);
        Assert.True(a >= 0, $"Regel {selektor} steht nicht im Stilblatt");
        int e = css.IndexOf('}', a);
        Assert.True(e > a);
        return css.Substring(a + selektor.Length, e - a - selektor.Length);
    }
}
