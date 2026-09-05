using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Der Baustein <see cref="Diagramm"/> — der Rahmen, in dem JEDES Renderer-Bild
/// steht (Windows-Abnahme 05.09.2026, Befund A-1: „Allgemein bei Charts: das
/// Zoomen funktioniert nicht").
///
/// <para><b>Was hier bewiesen wird.</b> Im WinForms-Vorbild waren die Diagramme
/// <c>Chart</c>-Steuerelemente mit Achsenzoom; seit iU7 zeichnet der Kern ein
/// PNG, und W11b hat den Verlust ausdrücklich festgehalten (A-7, Risiko
/// R-W11-5). Der Baustein gibt den Zoom zurück, und zwar in zwei Stufen: den
/// BILDZOOM (Rad, Kneifgeste, Ziehen, Tasten — er läuft ganz im Browser) und den
/// DATENZOOM (ein aufgezogenes Rechteck, das der Kern neu zeichnet). Geprüft
/// wird hier die C#-Seite: Rahmen, Knöpfe, Sprachausgabe, Zoomanzeige und die
/// vier Interop-Aufrufe. Was im Browser passiert, prüft kein bunit-Fall — dafür
/// steht der Abnahmepunkt am Gerät.</para>
///
/// <para><b>Und die Zusage, dass nichts bricht, wenn das Modul fehlt.</b> Eine
/// alte WebView oder eine Prüfumgebung ohne JavaScript darf kein leeres
/// Diagramm ergeben, sondern ein starres — genau den Zustand von vorher.</para>
///
/// <para>Die Klasse pinnt die Sprache selbst (Regel seit W8): Sie prüft die
/// Zoomanzeige „×2,5" und damit ein Dezimalkomma.</para>
/// </summary>
public class DiagrammTests : BunitContext
{
    private const string MODUL = "./_content/EPOS.UI/epos-diagramm.js";

    public DiagrammTests()
    {
        DeutscheOberflaeche();

        // Der Baustein lädt sein Modul dynamisch. In Loose-Mode beantwortet bunit
        // den import und jeden Aufruf mit dem Standardwert; die Fälle, die auf
        // einen bestimmten Aufruf zielen, setzen das Modul ausdrücklich auf.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>
    /// Kultur, UI-Kultur und die beiden Prozessvorgaben auf <c>de-DE</c> — sonst
    /// hängt das Ergebnis daran, welche Testklasse zuerst lief.
    /// </summary>
    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
    }

    private static RenderFragment Inhalt(string text) => builder =>
    {
        builder.OpenElement(0, "img");
        builder.AddAttribute(1, "alt", text);
        builder.CloseElement();
    };

    // ==================================================================
    //  D-1  Der Rahmen
    // ==================================================================

    /// <summary>
    /// Drei Teile machen den Rahmen aus: die Leiste, die zuschneidende Fläche
    /// (<c>overflow: hidden</c> im Stilblatt) und der verschiebbare Inhalt. Das
    /// Rechteck für den Datenzoom liegt darüber und ist zunächst verborgen.
    /// </summary>
    [Fact]
    public void D1_Der_Rahmen_traegt_Leiste_Flaeche_Inhalt_und_Rechteck()
    {
        var cut = Render<Diagramm>(p => p.Add(x => x.ChildContent, Inhalt("Bild")));

        Assert.Single(cut.FindAll(".epos-diagramm"));
        Assert.Single(cut.FindAll(".epos-diagramm-leiste"));
        Assert.Single(cut.FindAll(".epos-diagramm-flaeche"));
        Assert.Single(cut.FindAll(".epos-diagramm-inhalt"));
        Assert.True(cut.Find(".epos-diagramm-gummi").HasAttribute("hidden"));
    }

    /// <summary>Das Bild steht IM verschiebbaren Teil — sonst zoomt es nicht mit.</summary>
    [Fact]
    public void D1_Das_Kind_steht_im_verschiebbaren_Teil()
    {
        var cut = Render<Diagramm>(p => p.Add(x => x.ChildContent, Inhalt("Jahresgang")));

        Assert.Single(cut.FindAll(".epos-diagramm-inhalt img"));
    }

    // ==================================================================
    //  D-2  Sprachausgabe
    // ==================================================================

    /// <summary>
    /// Ein Diagramm ohne Text ist für blinde Anwender nicht vorhanden. Die Fläche
    /// trägt deshalb <c>role="img"</c> mit der Bezeichnung — und
    /// <c>tabindex="0"</c>, damit die Tasten + − 0 sie überhaupt erreichen.
    /// </summary>
    [Fact]
    public void D2_Die_Flaeche_ist_benannt_und_tastaturerreichbar()
    {
        var cut = Render<Diagramm>(p => p
            .Add(x => x.Bezeichnung, "Wärmelast Jahresganglinie")
            .Add(x => x.ChildContent, Inhalt("Bild")));

        var flaeche = cut.Find(".epos-diagramm-flaeche");
        Assert.Equal("img", flaeche.GetAttribute("role"));
        Assert.Equal("Wärmelast Jahresganglinie", flaeche.GetAttribute("aria-label"));
        Assert.Equal("0", flaeche.GetAttribute("tabindex"));
    }

    /// <summary>Die Zoomanzeige meldet sich selbst — sonst bliebe die Stufe stumm.</summary>
    [Fact]
    public void D2_Die_Zoomanzeige_ist_eine_Meldezone()
    {
        var cut = Render<Diagramm>(p => p.Add(x => x.ChildContent, Inhalt("Bild")));

        Assert.Equal("polite", cut.Find(".epos-diagramm-stufe").GetAttribute("aria-live"));
    }

    // ==================================================================
    //  D-3  Die Knöpfe
    // ==================================================================

    /// <summary>
    /// Ohne Datenzoom gibt es genau EINEN Knopf. Der Umschalter „Bereich" wäre
    /// dort ein Versprechen, das niemand einlöst — dieselbe Hausregel wie überall:
    /// kein Rückruf, kein Knopf.
    /// </summary>
    [Fact]
    public void D3_Ohne_Datenzoom_bleibt_es_bei_einem_Knopf()
    {
        var cut = Render<Diagramm>(p => p.Add(x => x.ChildContent, Inhalt("Bild")));

        var knoepfe = cut.FindAll("button.epos-diagramm-knopf");
        Assert.Single(knoepfe);
        Assert.Equal("1:1", knoepfe[0].TextContent.Trim());
    }

    /// <summary>Mit Datenzoom kommt der Umschalter „Bereich" dazu, zunächst aus.</summary>
    [Fact]
    public void D3_Mit_Datenzoom_kommt_der_Bereichsknopf_dazu()
    {
        var cut = Render<Diagramm>(p => p
            .Add(x => x.ChildContent, Inhalt("Bild"))
            .Add(x => x.BereichGewaehlt, (Diagrammbereich _) => { }));

        Assert.Equal(2, cut.FindAll("button.epos-diagramm-knopf").Count);
        Assert.Equal("false", cut.Find("button[aria-pressed]").GetAttribute("aria-pressed"));
        Assert.False(cut.Instance.Bereichsmodus);
    }

    /// <summary>
    /// Der Umschalter bleibt gedrückt, solange er an ist — auf dem iPad gibt es
    /// keine Umschalttaste, dort ist er der einzige Weg zum Rechteck.
    /// </summary>
    [Fact]
    public void D3_Der_Bereichsknopf_schaltet_um()
    {
        var cut = Render<Diagramm>(p => p
            .Add(x => x.ChildContent, Inhalt("Bild"))
            .Add(x => x.BereichGewaehlt, (Diagrammbereich _) => { }));

        cut.Find("button[aria-pressed]").Click();

        Assert.True(cut.Instance.Bereichsmodus);
        Assert.Equal("true", cut.Find("button[aria-pressed]").GetAttribute("aria-pressed"));
    }

    // ==================================================================
    //  D-4  Die Zoomanzeige
    // ==================================================================

    /// <summary>Ohne Zoom steht dort „×1" — nicht „×1,0".</summary>
    [Fact]
    public void D4_Am_Anfang_steht_die_Stufe_eins()
    {
        var cut = Render<Diagramm>(p => p.Add(x => x.ChildContent, Inhalt("Bild")));

        Assert.Equal("×1", cut.Find(".epos-diagramm-stufe").TextContent.Trim());
        Assert.Equal(1.0, cut.Instance.Stufe);
    }

    /// <summary>
    /// Meldet das Modul eine neue Stufe, steht sie in der Leiste — mit
    /// DEZIMALKOMMA, weil die Oberfläche deutsch ist.
    /// </summary>
    [Fact]
    public void D4_Die_gemeldete_Stufe_erscheint_mit_Dezimalkomma()
    {
        var cut = Render<Diagramm>(p => p.Add(x => x.ChildContent, Inhalt("Bild")));

        cut.InvokeAsync(() => cut.Instance.ZoomGemeldet(2.5));

        Assert.Equal("×2,5", cut.Find(".epos-diagramm-stufe").TextContent.Trim());
    }

    // ==================================================================
    //  D-5  Der Weg zum Modul
    // ==================================================================

    /// <summary>
    /// Beim ersten Zeichnen wird das Modul geladen und an die Fläche gehängt.
    /// Mitgegeben wird die Komponente selbst — sie ist es, die das Modul
    /// zurückruft.
    /// </summary>
    [Fact]
    public void D5_Beim_ersten_Zeichnen_wird_gebunden()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        var modul = JSInterop.SetupModule(MODUL);
        var binden = modul.SetupVoid("binden", _ => true);

        Render<Diagramm>(p => p.Add(x => x.ChildContent, Inhalt("Bild")));

        Assert.Single(binden.Invocations);
        Assert.Equal(2, binden.Invocations.Single().Arguments.Count);
    }

    /// <summary>Der Knopf „1:1" stellt das Bild zurück.</summary>
    [Fact]
    public void D5_Der_Knopf_stellt_das_Bild_zurueck()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        var modul = JSInterop.SetupModule(MODUL);
        modul.SetupVoid("binden", _ => true);
        var zurueck = modul.SetupVoid("zuruecksetzen", _ => true);

        var cut = Render<Diagramm>(p => p.Add(x => x.ChildContent, Inhalt("Bild")));
        cut.Find("button.epos-diagramm-knopf").Click();

        Assert.Single(zurueck.Invocations);
    }

    /// <summary>
    /// Und er meldet es nach oben: Wer einen Achsenbereich führt, verwirft ihn
    /// hier. EIN Knopf für beide Zoomstufen — zwei wären zwei Fragen an denselben
    /// Anwender.
    /// </summary>
    [Fact]
    public void D5_Der_Knopf_meldet_das_Zuruecksetzen()
    {
        int gerufen = 0;
        var cut = Render<Diagramm>(p => p
            .Add(x => x.ChildContent, Inhalt("Bild"))
            .Add(x => x.Zurueckgesetzt, () => gerufen++));

        cut.Find("button.epos-diagramm-knopf").Click();

        Assert.Equal(1, gerufen);
        Assert.Equal(1.0, cut.Instance.Stufe);
    }

    /// <summary>Der Umschalter „Bereich" reicht seinen Zustand ans Modul weiter.</summary>
    [Fact]
    public void D5_Der_Bereichsmodus_geht_ans_Modul()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        var modul = JSInterop.SetupModule(MODUL);
        modul.SetupVoid("binden", _ => true);
        var modus = modul.SetupVoid("bereichsmodus", _ => true);

        var cut = Render<Diagramm>(p => p
            .Add(x => x.ChildContent, Inhalt("Bild"))
            .Add(x => x.BereichGewaehlt, (Diagrammbereich _) => { }));
        cut.Find("button[aria-pressed]").Click();

        Assert.Single(modus.Invocations);
    }

    // ==================================================================
    //  D-6  Das aufgezogene Rechteck
    // ==================================================================

    /// <summary>
    /// Die vier Anteile kommen unverändert oben an — der Baustein rechnet NICHTS
    /// um. Was an dieser Stelle des Bildes steht, weiß nur der Renderer.
    /// </summary>
    [Fact]
    public void D6_Der_Bereich_wird_unveraendert_gemeldet()
    {
        Diagrammbereich? gemeldet = null;
        var cut = Render<Diagramm>(p => p
            .Add(x => x.ChildContent, Inhalt("Bild"))
            .Add(x => x.BereichGewaehlt, (Diagrammbereich b) => gemeldet = b));

        cut.InvokeAsync(() => cut.Instance.BereichGemeldet(0.25, 0.5, 0.1, 0.9));

        Assert.NotNull(gemeldet);
        Assert.Equal(0.25, gemeldet!.XVon);
        Assert.Equal(0.5, gemeldet.XBis);
        Assert.Equal(0.1, gemeldet.YVon);
        Assert.Equal(0.9, gemeldet.YBis);
    }

    /// <summary>
    /// Ohne Rückruf verpufft das Rechteck — kein Zeichenlauf, kein Zurücksetzen.
    /// Ein Bild ohne Datenzoom soll sich beim Ziehen nicht heimlich bewegen.
    /// </summary>
    [Fact]
    public async Task D6_Ohne_Rueckruf_geschieht_nichts()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        var modul = JSInterop.SetupModule(MODUL);
        modul.SetupVoid("binden", _ => true);
        var zurueck = modul.SetupVoid("zuruecksetzen", _ => true);

        var cut = Render<Diagramm>(p => p.Add(x => x.ChildContent, Inhalt("Bild")));
        await cut.InvokeAsync(() => cut.Instance.BereichGemeldet(0.2, 0.4, 0.1, 0.9));

        Assert.Empty(zurueck.Invocations);
    }

    /// <summary>
    /// Nach einem Datenzoom steht das Bild wieder auf 1:1 — der Kern zeichnet es
    /// ja mit dem neuen Ausschnitt, ein zusätzlicher Bildzoom wäre doppelt.
    /// </summary>
    [Fact]
    public async Task D6_Nach_dem_Bereich_steht_das_Bild_auf_eins()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        var modul = JSInterop.SetupModule(MODUL);
        modul.SetupVoid("binden", _ => true);
        var zurueck = modul.SetupVoid("zuruecksetzen", _ => true);

        var cut = Render<Diagramm>(p => p
            .Add(x => x.ChildContent, Inhalt("Bild"))
            .Add(x => x.BereichGewaehlt, (Diagrammbereich _) => { }));
        await cut.InvokeAsync(() => cut.Instance.ZoomGemeldet(4));
        await cut.InvokeAsync(() => cut.Instance.BereichGemeldet(0.2, 0.4, 0.1, 0.9));

        Assert.Equal(1.0, cut.Instance.Stufe);
        Assert.Single(zurueck.Invocations);
    }

    // ==================================================================
    //  D-7  Ohne JavaScript
    // ==================================================================

    /// <summary>
    /// Lädt das Modul nicht, steht das Bild da wie vorher — starr, aber
    /// vollständig. Ein Schönheitsfehler, kein Fehlschlag: In Strict-Mode OHNE
    /// aufgesetztes Modul wirft bunit bei jedem Aufruf, und genau das simuliert
    /// eine WebView, die die Datei nicht laden kann.
    /// </summary>
    [Fact]
    public void D7_Ohne_Modul_bleibt_das_Bild_stehen()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;

        var cut = Render<Diagramm>(p => p
            .Add(x => x.Bezeichnung, "Jahresgang")
            .Add(x => x.ChildContent, Inhalt("Bild")));

        Assert.Single(cut.FindAll(".epos-diagramm-inhalt img"));
        Assert.Equal("×1", cut.Find(".epos-diagramm-stufe").TextContent.Trim());

        // Auch der Knopf darf dann nicht durchschlagen.
        cut.Find("button.epos-diagramm-knopf").Click();
        Assert.Equal(1.0, cut.Instance.Stufe);
    }

    /// <summary>
    /// Und der Baustein lässt sich abräumen, ohne dass etwas nachhallt: Er nimmt
    /// die Handler wieder ab. Ohne das hinge an jedem geschlossenen Reiter ein
    /// Satz Ereignisbehandler samt Rückverweis auf eine tote Komponente.
    /// </summary>
    [Fact]
    public async Task D7_Das_Abraeumen_loest_die_Handler()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        var modul = JSInterop.SetupModule(MODUL);
        modul.SetupVoid("binden", _ => true);
        var loesen = modul.SetupVoid("loesen", _ => true);

        var cut = Render<Diagramm>(p => p.Add(x => x.ChildContent, Inhalt("Bild")));
        await cut.Instance.DisposeAsync();

        Assert.Single(loesen.Invocations);
    }
}
