using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Der Baustein <c>Katalograhmen</c> — Anwenderwunsch vom 05.09.2026,
/// „Admin-Menüs sind nicht an Größe Bildschirm angepasst".
///
/// <para><b>Der Befund.</b> „Administration Solarkollektoren" zeigte
/// untereinander: Überschrift, Balken „Auswahl in DB:", eine Liste in ihrem
/// eigenen kleinen Rollrahmen und darunter — nur über den SEITENrollbalken
/// erreichbar — den Balken „Eingabe der Solarkollektoren" mit den Feldern.
/// Alle sechs Verwaltungsmasken des Bestands stellten beides NEBENEINANDER
/// (Form_Heizkessel_Admin 726 × 383, Form_BHKWAdmin 856 × 517,
/// Form_SolarKollektorenAdmin 825 × 494, Form_PufferSp_Admin 721 × 330,
/// Form_AdminPV 607 × 489, Form_AdminStromspeicher 614 × 367).</para>
///
/// <para><b>Zweierlei wird geprüft</b> (Lehre W6‑B‑1): das MARKUP über bunit
/// und die REGEL im Stilblatt — eine bunit-Probe rechnet kein CSS aus. Denselben
/// Weg gehen <c>ListenrahmenTests</c> und <c>ZweispaltenauswahlTests</c>.</para>
///
/// <para>Keine Sprachbindung: geprüft werden Klassennamen. Die Kultur wird
/// trotzdem gepinnt (Hausregel seit iU9‑W8).</para>
/// </summary>
public class KatalograhmenTests : EposBunitContext
{
    public KatalograhmenTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<Katalograhmen> Aufbauen(bool gestapelt = false) =>
        Render<Katalograhmen>(p => p
            .Add(x => x.Gestapelt, gestapelt)
            .Add(x => x.Liste, (RenderFragment)(b => b.AddMarkupContent(0, "<p id=\"liste\">Liste</p>")))
            .Add(x => x.Eingabe, (RenderFragment)(b => b.AddMarkupContent(0, "<p id=\"eingabe\">Eingabe</p>"))));

    // =====================================================================
    //  Das Markup
    // =====================================================================

    /// <summary>Zwei Blöcke, jeder mit seinem Inhalt — und der Rahmen füllt.</summary>
    [Fact]
    public void Der_Rahmen_stellt_Liste_und_Eingabe_untereinander()
    {
        var cut = Aufbauen();

        string wurzel = cut.Find("div").ClassName ?? "";
        Assert.Contains("epos-katalog-paar", wurzel);
        Assert.Contains("epos-katalog-fuellend", wurzel);
        Assert.DoesNotContain("epos-katalog-paar--gestapelt", wurzel);

        Assert.Equal("Liste", cut.Find(".epos-katalog-liste #liste").TextContent);
        Assert.Equal("Eingabe", cut.Find(".epos-katalog-eingabe #eingabe").TextContent);
    }

    /// <summary>
    /// <c>Gestapelt</c> ist die Ausnahme für die Masken, deren Vorbild schon
    /// gestapelt war (<c>Form_AdminWaermeeinlesen</c> 676 × 433 — die Liste ging
    /// dort über die volle Breite).
    /// </summary>
    [Fact]
    public void Gestapelt_haengt_die_Zusatzklasse_an()
    {
        Assert.Contains("epos-katalog-paar--gestapelt",
                        Aufbauen(gestapelt: true).Find("div").ClassName ?? "");
    }

    /// <summary>
    /// Die zwei Spalten stehen auch ohne Inhalt da — sonst fiele die
    /// Rasteraufteilung in sich zusammen, sobald ein Wirt nur eine Seite füllt.
    /// </summary>
    [Fact]
    public void Ohne_Gaben_stehen_beide_Spalten()
    {
        var cut = Render<Katalograhmen>();

        Assert.Single(cut.FindAll(".epos-katalog-liste"));
        Assert.Single(cut.FindAll(".epos-katalog-eingabe"));
    }

    // =====================================================================
    //  Die Regeln im Stilblatt
    // =====================================================================

    /// <summary>
    /// Die Wurzel eines Katalogdialogs nimmt <c>.epos-dialog</c> die
    /// Breitenbremse (1160 px) und gibt ihm die volle Höhe des Fensters.
    ///
    /// <para><c>overflow: auto</c> und nicht <c>hidden</c>: Im Normalfall rollt
    /// dort nichts — die Abschnitte darin schrumpfen. Wird das Fenster aber bis
    /// auf das Kleinstmaß zusammengezogen, ist ein Rollbalken die ehrliche
    /// Antwort; <c>hidden</c> schnitte die Schlussleiste ab.</para>
    /// </summary>
    [Fact]
    public void Die_Dialogwurzel_nimmt_Breite_und_Hoehe()
    {
        string block = Stilblock(".epos-katalog-dialog {");

        Assert.Contains("max-width: none", block);
        Assert.Contains("height: 100dvh", block);
        Assert.Contains("overflow: auto", block);
    }

    /// <summary>
    /// Der füllende Abschnitt braucht <c>min-height: 0</c>. Ohne sie wächst ein
    /// Flex-Kind nie unter seinen Inhalt, und der Rollbalken landet an der Seite
    /// statt in der Liste — genau der Befund.
    /// </summary>
    [Fact]
    public void Der_fuellende_Abschnitt_traegt_min_height_null()
    {
        string block = Stilblock(".epos-katalog-fuellend {");

        Assert.Contains("flex: 1 1 auto", block);
        Assert.Contains("min-height: 0", block);
    }

    /// <summary>
    /// <b>Seit W14a‑E‑10 (07.09.2026) trägt die Liste im Katalograhmen wieder eine
    /// Höchsthöhe</b> — die Ausnahme zu W9‑B‑2 fällt. Steht die Eingabe UNTER der
    /// Liste statt daneben, schöbe eine lange Liste sie beliebig weit nach unten;
    /// das Maß ist <b>1,3 × <c>--epos-listenhoehe</c></b> = 458 px = elf Zeilen
    /// (Konzept_Katalogfilter 5.6.5, im Mockup gemessen).
    /// </summary>
    [Fact]
    public void Die_Liste_im_Rahmen_traegt_die_Hoechsthoehe_von_elf_Zeilen()
    {
        string block = Stilblock(".epos-katalog-liste .epos-raster-huelle {");

        Assert.Contains("flex: 0 1 auto", block);
        Assert.Contains("max-height: calc(var(--epos-listenhoehe) * 1.3)", block);
        Assert.DoesNotContain("max-height: none", block);
        Assert.Contains("min-height:", block);
    }

    /// <summary>
    /// <b>Der Eingabeblock rollt NICHT mehr selbst.</b> Er steht unter der Liste und
    /// ist so hoch wie sein Inhalt; was über die Fensterhöhe hinausgeht, nimmt der
    /// Rollbalken der MASKE (<c>.epos-katalog-dialog</c>, <c>overflow: auto</c>) —
    /// ein zweiter Rollbereich mitten im Dialog verbärge die Hälfte der Felder
    /// hinter einem Balken, den niemand sucht (offener Punkt O‑7).
    /// </summary>
    [Fact]
    public void Der_Eingabeblock_rollt_nicht_mehr_selbst()
    {
        Assert.DoesNotContain("overflow-y: auto", Stilblock(".epos-katalog-eingabe {"));
        Assert.Contains("overflow: auto", Stilblock(".epos-katalog-dialog {"));
    }

    /// <summary>
    /// <b>Es gibt nur noch EINE Anordnung: untereinander</b> (Anwenderentscheid
    /// W14a‑E‑10 — „Liste wie zuvor über ganze Breite, sonst zu schmale Liste").
    /// Die Medienabfrage bei 900 px entfällt damit ersatzlos; untereinander war
    /// schon vorher der schmale Fall. Das Token <c>--epos-zweispalten-umbruch</c>
    /// bleibt — <c>Zweispaltenauswahl</c> und <c>Formularraster</c> benutzen es
    /// weiter.
    /// </summary>
    [Fact]
    public void Es_gibt_nur_noch_eine_Anordnung_untereinander()
    {
        string block = Stilblock(".epos-katalog-paar {");

        Assert.Contains("grid-template-columns: 1fr", block);
        Assert.DoesNotContain("minmax(280px", block);
        Assert.Contains("grid-template-rows: auto auto", block);

        // Der Umbruch war die zweite Anordnung; ohne sie gibt es ihn nicht mehr.
        string css = Stilblatt();
        int a = css.IndexOf(".epos-katalog-paar {", StringComparison.Ordinal);
        int e = css.IndexOf(".epos-katalog-suchzeile {", a, StringComparison.Ordinal);
        Assert.True(e > a);
        Assert.DoesNotContain("@media (max-width: 900px)", css.Substring(a, e - a));

        // Das Token bleibt - es traegt weiter die Zweispaltenauswahl.
        Assert.Contains("--epos-zweispalten-umbruch: 900px", Stilblock(":root {"));
    }

    // =====================================================================
    //  KL-5 — der Rahmen staucht seine Reihen nicht mehr
    // =====================================================================

    /// <summary>
    /// <b>Der Befund KL‑5.</b> Im Dialog „Klimadaten" (1 180 × 780, nach einem
    /// Regionalimport mit 35 Regionen) malten Reiterleiste und Diagrammkasten über
    /// die Listenzeilen, der Eingabeblock über die Fußleiste; dasselbe Bild kam aus
    /// der „Stromverbraucher Verwaltung". Gemessen im Chromium
    /// (<c>Proben/Rasterprobe/katalogprobe.mjs</c>): Der Rahmen wurde von der
    /// Flexbox auf 601,8 px gestaucht und verteilte diese Höhe zu GLEICHEN Teilen
    /// auf seine zwei auto‑Reihen (295,906 px | 295,906 px) — weil beide Kinder
    /// <c>min-height: 0</c> trugen und die Mindestgröße einer Reihe damit null war.
    /// Die zweite Reihe war 618 px zu kurz, ihr Inhalt zeichnete darüber hinaus.
    ///
    /// <para>Die Behebung steht auf zwei Beinen, und beide werden hier geprüft:
    /// die Kinder bekommen ihre selbsttätige Mindestgröße zurück
    /// (<c>min-height: auto</c>), und der Rahmen rollt in sich
    /// (<c>overflow: auto</c>), statt die Maske länger zu machen — sonst stünde
    /// „Beenden" weit unter dem Fensterrand.</para>
    ///
    /// <para>Die MASSE prüft nur der Browser: <c>katalogprobe.mjs</c>, zwölf Fälle
    /// über alle sieben Katalograhmen‑Masken samt Gegenprobe <c>--vorher</c>. bunit
    /// hat kein Layout und sieht eine Überlagerung grundsätzlich nicht.</para>
    /// </summary>
    [Fact]
    public void Der_Rahmen_rollt_in_sich_und_laesst_seinen_Reihen_ihre_Hoehe()
    {
        Assert.Contains("overflow: auto",
                        Stilblock(".epos-katalog-paar.epos-katalog-fuellend {"));

        // Der zweite Selektor der Regel genügt als Anker — so hängt der Fall nicht
        // an der Zeilenendung des Stilblattes.
        Assert.Contains("min-height: auto",
                        Stilblock(".epos-katalog-paar > .epos-katalog-eingabe {"));
    }

    /// <summary>
    /// Die Regel gilt nur für das RASTERpaar. Zwei Masken setzen
    /// <c>epos-katalog-fuellend</c> auf eine bloße Liste
    /// (<c>GesetzeskatalogDialog</c>, <c>WaermepumpenKatalogDialog</c>); sie haben
    /// keine zweite Reihe und sollen ihre Liste weiter mitschrumpfen lassen.
    /// Deshalb trägt die gemeinsame Klasse <c>epos-katalog-fuellend</c> die zwei
    /// neuen Regeln NICHT.
    /// </summary>
    [Fact]
    public void Die_gemeinsame_Fuellklasse_bleibt_unberuehrt()
    {
        string block = Stilblock(".epos-katalog-fuellend {");

        Assert.Contains("flex: 1 1 auto", block);
        Assert.Contains("min-height: 0", block);
        Assert.DoesNotContain("overflow", block);
    }

    // =====================================================================
    //  Hilfen
    // =====================================================================

    private static string Stilblatt()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        return File.ReadAllText(Path.Combine(d!.FullName, "EPOS.UI", "wwwroot", "epos-ui.css"));
    }

    /// <summary>Liest den Rumpf einer Regel aus <c>EPOS.UI/wwwroot/epos-ui.css</c>.</summary>
    private static string Stilblock(string selektor)
    {
        string css = Stilblatt();

        int a = css.IndexOf(selektor, StringComparison.Ordinal);
        Assert.True(a >= 0, $"Regel {selektor} steht nicht im Stilblatt");
        int e = css.IndexOf('}', a);
        Assert.True(e > a);
        return css.Substring(a + selektor.Length, e - a - selektor.Length);
    }
}
