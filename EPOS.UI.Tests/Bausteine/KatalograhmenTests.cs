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

    // =====================================================================
    //  Stufe 1 der Neuordnung (Konzept Administrationsdialoge, V1): Kopf und
    //  Fuß stehen, Liste und Eingabeblock rollen je für sich, nie einer im
    //  anderen. Die MASSE misst Proben/Rasterprobe/katalogprobe.mjs (Fälle N,
    //  1 088 × 624 und 400 × 624, Gegenprobe G2); hier stehen die REGELN.
    // =====================================================================

    /// <summary>
    /// <b>Der Befund vor Stufe 1</b> (Katalogprobe, Heizkessel 1 088 × 624): Der Rahmen
    /// rollte in sich (474 px sichtbar von 1 261), in ihm die Liste mit ihrer
    /// Höchsthöhe von 457,6 px — ein Rollbereich im Rollbereich —, und der
    /// Eingabeblock lag mit 0 px sichtbar unter dem Rand. <b>Jetzt rollt der Rahmen
    /// nicht mehr, er verteilt</b>; nur unter seinem Kleinstmaß rollt als Notnagel die
    /// Maske.
    /// </summary>
    [Fact]
    public void Der_Rahmen_rollt_nicht_er_verteilt()
    {
        string rahmen = Stilblock(".epos-katalog-paar.epos-katalog-fuellend {");
        Assert.Contains("overflow: hidden", rahmen);
        Assert.DoesNotContain("overflow: auto", rahmen);
        Assert.Contains("min-height: 22rem", rahmen);

        // Der Listenbereich malt nie über den Eingabeblock (KL-5), er schneidet ab.
        Assert.Contains("overflow: hidden", Stilblock("\n.epos-katalog-liste {"));

        // Der Notnagel bleibt an der Maske.
        Assert.Contains("overflow: auto", Stilblock(".epos-katalog-dialog {"));
    }

    /// <summary>
    /// <b>Die Liste nimmt die Resthöhe</b> (V1) — keine Höchsthöhe mehr (die
    /// 1,3 × <c>--epos-listenhoehe</c> fallen). Die Hülle füllt den Listenbereich und
    /// ist der Behälter fester Höhe, den <c>Virtualize</c> braucht (Rasterprobe, Fälle
    /// J und K: Rollbehälter = Hülle, Zeile 53 px).
    /// </summary>
    [Fact]
    public void Die_Liste_im_Rahmen_nimmt_die_Resthoehe()
    {
        Assert.Contains("flex: 1 1 0", Stilblock(".epos-katalog-paar > .epos-katalog-liste {"));
        Assert.Contains("flex: 1 1 0", Stilblock(".epos-katalog-liste > .epos-katalogliste {"));

        string huelle = Stilblock(".epos-katalog-liste .epos-katalogliste > .epos-raster-huelle {");
        Assert.Contains("flex: 1 1 0", huelle);
        Assert.Contains("max-height: none", huelle);
        Assert.Contains("min-height:", huelle);
        Assert.DoesNotContain("--epos-listenhoehe", huelle);
    }

    /// <summary>
    /// <b>Der Eingabeblock ist der zweite Bereich</b>: so hoch wie sein Inhalt,
    /// höchstens 34 % des Rahmens, er rollt selbst und ist oben sichtbar abgesetzt.
    /// Die Grenze steht als <c>max-height</c> eines Flexkindes — als Rasterreihe
    /// (<c>fit-content(38%)</c>) löste Chromium die Prozenthöhe nicht auf, und der
    /// Liste blieben 59 px (gemessen).
    /// </summary>
    [Fact]
    public void Der_Eingabeblock_rollt_fuer_sich_und_ist_gedeckelt()
    {
        // Der Zeilenumbruch davor grenzt die EIGENE Regel von der des Paares ab.
        string block = Stilblock("\n.epos-katalog-eingabe {");
        Assert.Contains("overflow: auto", block);
        Assert.Contains("border-top:", block);

        string kind = Stilblock(".epos-katalog-paar > .epos-katalog-eingabe {");
        Assert.Contains("flex: 0 1 auto", kind);
        Assert.Contains("max-height: 34%", kind);
    }

    /// <summary>
    /// <b>Eine Anordnung, untereinander</b> — als Flexspalte. Die Medienabfrage bei
    /// 900 px gibt es im Rahmen nicht; das Token <c>--epos-zweispalten-umbruch</c>
    /// bleibt — <c>Zweispaltenauswahl</c> und <c>Formularraster</c> benutzen es weiter.
    /// </summary>
    [Fact]
    public void Es_gibt_nur_noch_eine_Anordnung_untereinander()
    {
        string block = Stilblock(".epos-katalog-paar {");

        Assert.Contains("display: flex", block);
        Assert.Contains("flex-direction: column", block);
        Assert.DoesNotContain("grid-template-rows", block);

        string css = Stilblatt();
        int a = css.IndexOf(".epos-katalog-paar {", StringComparison.Ordinal);
        int e = css.IndexOf(".epos-katalog-suchzeile {", a, StringComparison.Ordinal);
        Assert.True(e > a);
        Assert.DoesNotContain("@media (max-width: 900px)", css.Substring(a, e - a));

        Assert.Contains("--epos-zweispalten-umbruch: 900px", Stilblock(":root {"));
    }

    /// <summary>
    /// <b>Eingebettet in eine Überlagerung</b> („Katalog verwalten…" aus einem
    /// Projektdialog) nimmt die Maske die Höhe der Überlagerung statt der des
    /// Fensters — sonst rollte die Überlagerung um die zwei Bereiche der Maske.
    /// </summary>
    [Fact]
    public void Eingebettet_nimmt_die_Maske_die_Hoehe_der_Ueberlagerung()
    {
        Assert.Contains("height: calc(90vh",
                        Stilblock(".epos-ueberlagerung-inhalt > .epos-katalog-dialog {"));
    }

    /// <summary>
    /// Die Regeln gelten nur für das Paar. Zwei Masken setzen
    /// <c>epos-katalog-fuellend</c> auf eine bloße Liste
    /// (<c>GesetzeskatalogDialog</c>, <c>WaermepumpenKatalogDialog</c>); die gemeinsame
    /// Klasse trägt deshalb kein <c>overflow</c>.
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
