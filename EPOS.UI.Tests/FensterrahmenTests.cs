using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bunit;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// Die Wache zu <b>„Kopf+Fuß fest"</b> (Anwenderentscheid 30.09.2026): In den Dialogen,
/// die unter Windows als eigenes Fenster laufen, haften Kopfzeile und Schlussleiste am
/// Fenster, nur der Inhalt dazwischen rollt.
///
/// <para><b>Wie es gebaut ist.</b> Die Fensterhülle <c>BlazorDialogForm</c> — und nur sie —
/// meldet an <c>#app</c> die <see cref="Fensterwurzel{TInhalt}"/> an; sie zeichnet den Dialog
/// und dahinter den Baustein <see cref="Fenstermarke"/> — EINE Wurzelkomponente, kein
/// Selektor wie <c>body::after</c>, den der BlazorWebView selbst belegt. Das Hausblatt lässt dann, und nur dann, das erste Kind der
/// Dialogwurzel (<c>.epos-dialog-kopf</c>) oben und die Leiste mit dem Primärknopf unten
/// haften (<c>epos-ui.css</c>, Abschnitt „Dialog im eigenen Fenster"). Die Katalogdialoge
/// (<c>.epos-katalog-dialog</c>) und die Projektdialoge mit Katalogauswahl (<c>.epos-zweispalten</c>) bleiben ausgenommen, Überlagerung und Blatt behalten ihre
/// eigenen Regeln.</para>
///
/// <para><b>Was hier gehalten wird</b> — bunit misst keine Lage, das tut die Fensterprobe
/// (<c>Proben/Rasterprobe/fensterprobe.mjs</c>); hier stehen die Voraussetzungen, auf die
/// die Regel baut:</para>
/// <list type="number">
///   <item><description>Die Marke zeichnet nichts als ein verborgenes Element.</description></item>
///   <item><description>Allein <c>BlazorDialogForm</c> setzt sie; Hauptfenster und
///     iOS-Hülle nicht (auf iOS stehen die Ansichten der <c>AppWurzel</c> ebenso
///     unmittelbar unter <c>#app</c> — ohne Marke hafteten sie mit).</description></item>
///   <item><description>Die Regeln stehen im Hausblatt, an die Marke gebunden, mit
///     <c>z-index</c> unter den Überlagerungen.</description></item>
///   <item><description>Jeder Fensterdialog mit Hauswurzel trägt den Kopf als erstes Kind,
///     und ein Primärknopf steht nur in der Schlussleiste: Leisten mit Primärknopf stehen
///     auf der Wurzelebene nur als Zweige EINER Stelle unmittelbar hintereinander, und nach
///     ihnen folgt keine weitere Knopfleiste. Sonst haftete eine Knopfzeile mitten im
///     Inhalt.</description></item>
/// </list>
/// </summary>
public sealed class FensterrahmenTests : EposBunitContext
{
    /// <summary>Der Wurzelanker der Regeln im Hausblatt.</summary>
    private const string ANKER = "#app:has(> .epos-fenstermarke) > .epos-dialog:not(.epos-katalog-dialog, :has(> .epos-zweispalten))";

    // =====================================================================
    //  1 - Die Marke
    // =====================================================================

    [Fact]
    public void Die_Fenstermarke_zeichnet_nur_ein_verborgenes_Element()
    {
        var cut = Render<Fenstermarke>();

        var elemente = cut.Nodes.OfType<AngleSharp.Dom.IElement>().ToList();
        Assert.Single(elemente);
        Assert.Equal("SPAN", elemente[0].TagName);
        Assert.Equal("epos-fenstermarke", elemente[0].ClassName);
        Assert.True(elemente[0].HasAttribute("hidden"));
        Assert.Equal("", elemente[0].TextContent);
    }

    /// <summary>
    /// Die Fensterwurzel zeichnet den Dialog (über <c>Wurzel</c> samt Fehlerschranke, ohne
    /// eigene Hülle) und DAHINTER die Marke; der Parametersatz kommt unverändert beim Dialog an.
    /// </summary>
    [Fact]
    public void Die_Fensterwurzel_zeichnet_den_Dialog_und_dahinter_die_Marke()
    {
        var cut = Render<Fensterwurzel<Probedialog>>(p => p.AddUnmatched("Titel", "Probe"));

        var elemente = cut.Nodes.OfType<AngleSharp.Dom.IElement>().ToList();
        Assert.Equal(2, elemente.Count);
        Assert.Equal("epos-dialog", elemente[0].ClassName);
        Assert.Equal("Probe", elemente[0].TextContent);
        Assert.Equal("epos-fenstermarke", elemente[1].ClassName);
    }

    /// <summary>
    /// Kein Wurzelselektor mit <c>::after</c>/<c>::before</c> in der Windows-Schale: Der
    /// BlazorWebView belegt <c>body::after</c> selbst (Nachladen der Stilblätter mit den
    /// Entwicklerwerkzeugen); eine zweite Anmeldung wirft in der Fensterprozedur und beendet
    /// den Prozess ohne Meldung (<c>0xc000041d</c>).
    /// </summary>
    [Fact]
    public void Kein_Wurzelselektor_der_Schale_belegt_einen_Pseudoort()
    {
        string schale = Path.Combine(Wurzel(), "WindowsFormsApplication1");
        string[] treffer = Directory.GetFiles(schale, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                     && !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .Where(p => Regex.IsMatch(Code(File.ReadAllText(p)), @"RootComponents\.Add<[^;]*""[^""]*::(after|before)"""))
            .ToArray();
        Assert.Empty(treffer);
    }

    /// <summary>Ein kleinster Dialog für die Fensterwurzel.</summary>
    private sealed class Probedialog : Microsoft.AspNetCore.Components.ComponentBase
    {
        [Microsoft.AspNetCore.Components.Parameter] public string Titel { get; set; } = "";

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder b)
        {
            b.OpenElement(0, "div");
            b.AddAttribute(1, "class", "epos-dialog");
            b.AddContent(2, Titel);
            b.CloseElement();
        }
    }

    // =====================================================================
    //  1b - Das Fensterkreuz führt erst offene Blätter zurück
    // =====================================================================

    /// <summary>
    /// Fensterkreuz und Alt+F4 eines Dialogs im eigenen Fenster führen erst ein offenes Blatt zurück
    /// (Anwenderwunsch „Gleiches Verhalten mit anderen Dialogen“): Die Fensterwurzel reicht den Stapel
    /// ihres Schließwegs an den Dialog, ein Blatt darin meldet sich an, und der Schließweg fängt das
    /// Schließen ab, bis kein Blatt mehr steht — Blatt im Blatt zuerst.
    /// </summary>
    [Fact]
    public void Das_Fensterkreuz_fuehrt_erst_offene_Blaetter_zurueck()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var weg = new Fensterschliessweg();
        var cut = Render<Fensterwurzel<Blattprobedialog>>(p => p.Add(w => w.Schliessweg, weg));
        var dialog = cut.FindComponent<Blattprobedialog>().Instance;

        // Wurzelblatt: nichts abzufangen, das Fenster schließt.
        Assert.False(weg.SchliessenAbfangen());

        cut.InvokeAsync(() => dialog.Oeffne(a: true, b: true));
        Assert.Equal(2, weg.Stapel.Anzahl);

        // Erstes Kreuz: das innere Blatt geht zurück, das Fenster bleibt.
        Assert.True(weg.SchliessenAbfangen());
        weg.LetzterRueckweg.GetAwaiter().GetResult();
        cut.WaitForAssertion(() => Assert.False(dialog.BlattB));
        Assert.True(dialog.BlattA);
        Assert.Equal(1, weg.Stapel.Anzahl);

        // Zweites Kreuz: das äußere Blatt.
        Assert.True(weg.SchliessenAbfangen());
        weg.LetzterRueckweg.GetAwaiter().GetResult();
        cut.WaitForAssertion(() => Assert.False(dialog.BlattA));
        Assert.Equal(0, weg.Stapel.Anzahl);
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".wurzelblatt")));

        // Drittes Kreuz: Wurzelblatt — das Fenster schließt.
        Assert.False(weg.SchliessenAbfangen());
    }

    /// <summary>Ohne Schließweg der Hülle legt die Wurzel einen eigenen an; der Dialog zeichnet wie bisher.</summary>
    [Fact]
    public void Ohne_Schliessweg_zeichnet_die_Wurzel_wie_bisher()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<Fensterwurzel<Blattprobedialog>>();
        cut.InvokeAsync(() => cut.FindComponent<Blattprobedialog>().Instance.Oeffne(a: true, b: false));
        Assert.Single(cut.FindAll(".epos-blatt"));
    }

    /// <summary>
    /// Die Windows-Hülle fragt den Schließweg beim Systembefehl „Schließen“ (Kreuz, Alt+F4) und reicht ihn
    /// der Fensterwurzel als eigenen Parameter herein — nach der Parametersatzwache.
    /// </summary>
    [Fact]
    public void Die_Fensterhuelle_fragt_den_Schliessweg_beim_Fensterkreuz()
    {
        string form = Code(Lies("WindowsFormsApplication1", "Allgemein", "Blazor", "BlazorDialogForm.cs"));
        Assert.Contains("_schliessweg.SchliessenAbfangen()", form);
        Assert.Contains("SC_CLOSE", form);
        Assert.Contains("Fensterwurzel<TKomponente>.Schliessweg", form);
        Assert.True(form.IndexOf("Parametersatzwache.Pruefen", StringComparison.Ordinal)
                    < form.IndexOf("Fensterwurzel<TKomponente>.Schliessweg", StringComparison.Ordinal));
    }

    /// <summary>Ein Fensterdialog mit Blatt A und darin Blatt B, Zustand beim Wirt wie im echten Dialog.</summary>
    private sealed class Blattprobedialog : Microsoft.AspNetCore.Components.ComponentBase
    {
        public bool BlattA;
        public bool BlattB;

        public void Oeffne(bool a, bool b) { BlattA = a; BlattB = b; StateHasChanged(); }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder b)
        {
            b.OpenElement(0, "div");
            b.AddAttribute(1, "class", "epos-dialog");
            if (!BlattA) b.AddMarkupContent(2, "<p class=\"wurzelblatt\">Dialog</p>");
            b.OpenComponent<Blattwechsel>(3);
            b.AddAttribute(4, nameof(Blattwechsel.Offen), BlattA);
            b.AddAttribute(5, nameof(Blattwechsel.Titel), "Blatt A");
            b.AddAttribute(6, nameof(Blattwechsel.Zurueck),
                Microsoft.AspNetCore.Components.EventCallback.Factory.Create(this, () => { BlattA = false; }));
            b.AddAttribute(7, nameof(Blattwechsel.KindInhalt), (Microsoft.AspNetCore.Components.RenderFragment)(a =>
            {
                if (!BlattA) return;
                a.OpenComponent<Blattwechsel>(0);
                a.AddAttribute(1, nameof(Blattwechsel.Offen), BlattB);
                a.AddAttribute(2, nameof(Blattwechsel.Titel), "Blatt B");
                a.AddAttribute(3, nameof(Blattwechsel.Zurueck),
                    Microsoft.AspNetCore.Components.EventCallback.Factory.Create(this, () => { BlattB = false; }));
                a.CloseComponent();
            }));
            b.CloseComponent();
            b.CloseElement();
        }
    }

    // =====================================================================
    //  2 - Wer sie setzt
    // =====================================================================

    [Fact]
    public void Allein_die_Fensterhuelle_setzt_die_Marke_in_app()
    {
        string form = Code(Lies("WindowsFormsApplication1", "Allgemein", "Blazor", "BlazorDialogForm.cs"));
        // EINE Wurzelkomponente: die Fensterwurzel an #app - sie zeichnet Dialog und Marke.
        Assert.Equal(1, Vorkommen(form, "RootComponents.Add<EPOS.UI.Bausteine.Fensterwurzel<TKomponente>>(\"#app\", parameter)"));
        Assert.Equal(1, Vorkommen(form, "RootComponents.Add"));

        Assert.DoesNotContain("Fenstermarke", Code(Lies("WindowsFormsApplication1", "Allgemein", "Blazor", "BlazorSeite.cs")));
        Assert.DoesNotContain("Fensterwurzel", Code(Lies("WindowsFormsApplication1", "Allgemein", "Blazor", "BlazorSeite.cs")));
        string ios = Path.Combine(Wurzel(), "EPOS.iOS", "HauptSeite.cs");
        if (File.Exists(ios))
            Assert.DoesNotContain("Fenstermarke", Code(File.ReadAllText(ios)));

        // Keine Komponente setzt sie selbst - sonst haftete ein Dialog auch als Seite.
        string[] setzer = Directory.GetFiles(Path.Combine(Wurzel(), "EPOS.UI"), "*.razor", SearchOption.AllDirectories)
            .Where(p => !p.EndsWith("Fenstermarke.razor", StringComparison.Ordinal))
            .Where(p => Regex.IsMatch(Markup(File.ReadAllText(p)), @"<Fenstermarke\b"))
            .ToArray();
        Assert.Empty(setzer);
    }

    // =====================================================================
    //  3 - Die Regeln im Hausblatt
    // =====================================================================

    [Fact]
    public void Kopf_und_Schlussleiste_haften_nur_mit_der_Marke()
    {
        string kopf = Regelblock(ANKER + " > .epos-dialog-kopf:first-child {");
        Assert.Contains("position: sticky;", kopf);
        Assert.Contains("top: 0;", kopf);
        Assert.Contains("background: var(--epos-karte-flaeche);", kopf);
        Assert.Contains("border-bottom: 1px solid var(--epos-rahmen-leise);", kopf);
        Assert.InRange(ZIndex(kopf), 1, 39);

        // Die Schlussleiste: die Leiste mit dem Primaerknopf als eigenem Kind, dazu der
        // Fussblock mit Warnband (W7-B-2) - beide in EINER Regel.
        string fuss = Regelblock(ANKER + " > .epos-leiste:has(> .epos-knopf--primaer),\n"
                                 + ANKER + " > .epos-dialog-fuss {");
        Assert.Contains("position: sticky;", fuss);
        Assert.Contains("bottom: 0;", fuss);
        Assert.Contains("background: var(--epos-karte-flaeche);", fuss);
        Assert.Contains("border-top: 1px solid var(--epos-rahmen-leise);", fuss);
        Assert.InRange(ZIndex(fuss), 1, 39);

        // Das angesprungene Feld: scroll-padding am Dokument, nur mit der Marke - und
        // keins, solange der Fokus in Kopf oder Fuss steht.
        string polster = Regelblock("html:has(> body > #app > .epos-fenstermarke) {");
        Assert.Contains("scroll-padding-top: var(--epos-fenster-polster-oben);", polster);
        Assert.Contains("scroll-padding-bottom: var(--epos-fenster-polster-unten);", polster);
        Assert.Contains("scroll-padding: 0;", Regelblock(
            "html:has(> body > #app > .epos-fenstermarke):has(> body > #app > .epos-dialog:not(.epos-katalog-dialog) > .epos-dialog-kopf:first-child :focus),"));

        // Kein anderer Ort im Blatt laesst den Dialogkopf haften.
        string css = Hausblatt();
        foreach (Match m in Regex.Matches(css, @"(?<sel>[^{}]*\.epos-dialog-kopf[^{}]*)\{(?<rumpf>[^}]*)\}"))
            if (m.Groups["rumpf"].Value.Contains("sticky", StringComparison.Ordinal))
                Assert.StartsWith(ANKER, m.Groups["sel"].Value.Trim().Split('\n').Last().Trim());
    }

    /// <summary>
    /// Die Meldung steht im Bild (Anwenderentscheid 06.10.2026): Ein Warnbanner als
    /// unmittelbares Kind der Fensterwurzel haftet unter dem Kopf und über der
    /// Schlussleiste, trägt dort sein Kreuz zum Ausblenden, und das Polster oben rechnet
    /// seine Höhe mit. Nirgends sonst haftet ein Banner, und das Kreuz ist außerhalb
    /// verborgen - Überlagerung, Blatt und Seite bleiben, wie sie sind. Die Lage misst die
    /// Bannerprobe (Proben/Rasterprobe/bannerprobe.mjs).
    /// </summary>
    [Fact]
    public void Ein_Banner_der_Fensterwurzel_haftet_unter_dem_Kopf()
    {
        string banner = Regelblock(ANKER + " > .epos-warnbanner {");
        Assert.Contains("position: sticky;", banner);
        Assert.Contains("top: var(--epos-fenster-kopf);", banner);
        Assert.Contains("bottom: var(--epos-fenster-fuss);", banner);
        Assert.InRange(ZIndex(banner), 1, 39);

        Assert.Contains("display: inline-flex;",
            Regelblock(ANKER + " > .epos-warnbanner > .epos-warnbanner-schliessen {"));
        Assert.Contains("display: none;", Regelblock(".epos-warnbanner-schliessen {"));
        Assert.Contains("--epos-fenster-polster-oben:", Regelblock(
            "html:has(> body > #app > .epos-fenstermarke):has(> body > #app > .epos-dialog:not(.epos-katalog-dialog) > .epos-warnbanner) {"));

        string css = Hausblatt();
        foreach (Match m in Regex.Matches(css, @"(?<sel>[^{}]*\.epos-warnbanner[^{}]*)\{(?<rumpf>[^}]*)\}"))
        {
            string rumpf = m.Groups["rumpf"].Value;
            string sel = m.Groups["sel"].Value.Trim().Split('\n').Last().Trim();
            if (rumpf.Contains("sticky", StringComparison.Ordinal))
                Assert.StartsWith(ANKER, sel);
            if (sel.Contains("epos-warnbanner-schliessen", StringComparison.Ordinal)
                && Regex.IsMatch(rumpf, @"display:\s*(inline-)?(flex|block)"))
                Assert.StartsWith(ANKER, sel);
        }
    }

    /// <summary>
    /// Die Bannerregel wirkt nur, wenn jede Meldung der Baustein ist: Kein Markup außer
    /// <c>Warnbanner.razor</c> zeichnet selbst ein <c>epos-warnbanner</c>-Element (die
    /// Teilklassen wie <c>epos-warnbanner-erklaeren</c> dürfen andere tragen).
    /// </summary>
    [Fact]
    public void Jede_Meldung_ist_der_Baustein_und_kein_eigenes_Banner()
    {
        var fremd = Directory.EnumerateFiles(Path.Combine(Wurzel(), "EPOS.UI"), "*.razor", SearchOption.AllDirectories)
            .Where(p => !p.Replace('\\', '/').Contains("/obj/", StringComparison.Ordinal))
            .Where(p => Path.GetFileName(p) != "Warnbanner.razor")
            .Where(p => Regex.IsMatch(Markup(File.ReadAllText(p)), @"class=""[^""]*\bepos-warnbanner(?![-\w])"))
            .Select(p => Path.GetRelativePath(Wurzel(), p))
            .ToList();

        Assert.Empty(fremd);
    }

    // =====================================================================
    //  4 - Die Fensterdialoge
    // =====================================================================

    [Fact]
    public void Jeder_Fensterdialog_traegt_den_Kopf_zuerst_und_den_Primaerknopf_nur_in_der_Schlussleiste()
    {
        List<(string Typ, string Datei)> dialoge = Fensterdialoge();
        var funde = new List<string>();
        int gedeckt = 0;
        foreach ((string typ, string datei) in dialoge)
        {
            Wurzelbild? bild = Wurzelbild.Lesen(File.ReadAllText(datei));
            if (bild is null || !bild.Hauswurzel) continue;   // eigene Wurzel oder Katalogdialog
            gedeckt++;
            funde.AddRange(Pruefen(bild).Select(f => typ + ": " + f));
        }

        Assert.True(funde.Count == 0, "Diese Fensterdialoge brechen die Regel \"Kopf+Fuss fest\":\n  "
                                      + string.Join("\n  ", funde));
        // 47 Fensterdialoge; 13 Katalogdialoge und 4 eigene Wurzeln (Lizenz, KI-Chat,
        // KI-Einstellungen, KI-Hinweis) bleiben draussen.
        Assert.True(gedeckt >= 25, "Nur " + gedeckt + " von " + dialoge.Count + " Fensterdialogen gedeckt.");
    }

    /// <summary>
    /// Die Gegenprobe (Lehre W6-B-1: eine Wache, die nie rot werden kann, prueft nichts).
    /// </summary>
    [Fact]
    public void Gegenprobe_die_Pruefung_greift_an_gebauten_Beispielen()
    {
        const string gut = """
            <div class="epos-dialog" tabindex="-1">
                <div class="epos-dialog-kopf"><h1>T</h1></div>
                <div class="epos-leiste"><button class="epos-knopf">Kosten</button></div>
                <p>Inhalt</p>
                <SpeichernLeiste Ergebnis="X" />
                <Ueberlagerung Offen="@a"><KindInhalt><div class="epos-leiste"><button class="epos-knopf epos-knopf--primaer">OK</button></div></KindInhalt></Ueberlagerung>
                <Rueckfrage Offen="@b" />
            </div>
            """;
        Assert.Empty(Pruefen(Wurzelbild.Lesen(gut)!));

        const string mittelleiste = """
            <div class="epos-dialog">
                <div class="epos-dialog-kopf"></div>
                <div class="epos-leiste"><button class="epos-knopf epos-knopf--primaer">Rechnen</button></div>
                <p>Inhalt</p>
                <SpeichernLeiste Ergebnis="X" />
            </div>
            """;
        Assert.Single(Pruefen(Wurzelbild.Lesen(mittelleiste)!));

        const string kopfNichtZuerst = """
            <div class="epos-dialog">
                <p class="epos-kontextzeile">x</p>
                <div class="epos-dialog-kopf"></div>
                <SpeichernLeiste Ergebnis="X" />
            </div>
            """;
        Assert.Single(Pruefen(Wurzelbild.Lesen(kopfNichtZuerst)!));

        const string ohneLeiste = """
            <div class="epos-dialog"><div class="epos-dialog-kopf"></div><p>x</p></div>
            """;
        Assert.Single(Pruefen(Wurzelbild.Lesen(ohneLeiste)!));

        const string leisteNachDemFuss = """
            <div class="epos-dialog">
                <div class="epos-dialog-kopf"></div>
                <SpeichernLeiste Ergebnis="X" />
                <div class="epos-leiste"><button class="epos-knopf">Mehr</button></div>
            </div>
            """;
        Assert.Single(Pruefen(Wurzelbild.Lesen(leisteNachDemFuss)!));

        // Zweige EINER Stelle und Inhalt nach der Schlussleiste sind erlaubt.
        const string zweigeUndNachlauf = """
            <div class="epos-dialog">
                <div class="epos-dialog-kopf"></div>
                @if (NurLesen) { <div class="epos-leiste"><button class="epos-knopf epos-knopf--primaer">Schliessen</button></div> }
                else { <SpeichernLeiste Ergebnis="X" /> }
                <Textfeld Bezeichnung="Protokoll" />
                <Rueckfrage Offen="@b" />
            </div>
            """;
        Assert.Empty(Pruefen(Wurzelbild.Lesen(zweigeUndNachlauf)!));

        Assert.False(Wurzelbild.Lesen("""<div class="epos-dialog epos-katalog-dialog"><div class="epos-dialog-kopf"></div></div>""")!.Hauswurzel);
        Assert.False(Wurzelbild.Lesen("""<div class="epos-lizenz"><header></header></div>""")!.Hauswurzel);
    }

    // =====================================================================
    //  Pruefung
    // =====================================================================

    private static List<string> Pruefen(Wurzelbild bild)
    {
        var funde = new List<string>();
        if (bild.Kinder.Count == 0 || !bild.Kinder[0].Klassen.Contains("epos-dialog-kopf"))
            funde.Add("das erste Kind der Wurzel ist nicht .epos-dialog-kopf (" +
                      (bild.Kinder.Count > 0 ? bild.Kinder[0].Name + "." + string.Join(".", bild.Kinder[0].Klassen) : "leer") + ")");

        int erste = bild.Kinder.FindIndex(k => k.TraegtPrimaer);
        if (erste < 0) { funde.Add("keine Schlussleiste mit Primaerknopf auf der Wurzelebene"); return funde; }
        int letzte = bild.Kinder.FindLastIndex(k => k.TraegtPrimaer);

        // Mehrere Leisten mit Primaerknopf nur als Zweige EINER Stelle (@if ... else), also
        // unmittelbar hintereinander - dazwischen Inhalt hiesse: eine haftet mitten im Dialog.
        for (int i = erste; i <= letzte; i++)
            if (!bild.Kinder[i].TraegtPrimaer)
            {
                funde.Add("zwischen zwei Leisten mit Primaerknopf steht " + Beschrieben(bild.Kinder[i])
                          + " - die erste haftete mitten im Inhalt");
                break;
            }

        // Nach der Schlussleiste keine weitere Knopfleiste: Die Schlussleiste ist die LETZTE
        // (KnopfleistenWacheTests). Inhalt darf folgen (das Protokoll der Dublettenpruefung) -
        // der Fuss ragt dann nicht in den Rand, er ueberdeckt nichts.
        foreach (Kind k in bild.Kinder.Skip(letzte + 1))
            if (IstLeiste(k))
            {
                funde.Add("nach der Schlussleiste steht noch die Leiste " + Beschrieben(k));
                break;
            }
        return funde;
    }

    private static bool IstLeiste(Kind k)
        => k.Name == "SpeichernLeiste" || (k.Name == "div" && (k.Klassen.Contains("epos-leiste") || k.Klassen.Contains("epos-dialog-fuss")));

    private static string Beschrieben(Kind k)
        => k.Name + (k.Klassen.Count > 0 ? "." + string.Join(".", k.Klassen) : "");

    /// <summary>Ein Kind der Dialogwurzel: Name, Klassen, ob es eine Schlussleiste ist.</summary>
    private sealed record Kind(string Name, IReadOnlyList<string> Klassen, bool TraegtPrimaer);

    /// <summary>
    /// Die Wurzel einer Razor-Komponente und ihre Kinder, aus dem Markup gelesen (ohne
    /// Kommentare und ohne @code). Razor-Verzweigungen zeichnen kein Element und stoeren nicht.
    /// </summary>
    private sealed class Wurzelbild
    {
        private static readonly HashSet<string> LEER = new(StringComparer.OrdinalIgnoreCase)
        { "input", "br", "img", "hr", "col", "meta", "link", "source", "area", "wbr", "path", "rect",
          "circle", "line", "polyline", "polygon", "stop", "use" };

        private static readonly Regex TAG = new(
            @"<(?<zu>/?)(?<name>[A-Za-z][\w.:-]*)(?<attr>(?:[^<>""']|""[^""]*""|'[^']*')*?)(?<leer>/?)>",
            RegexOptions.Singleline);

        public List<string> WurzelKlassen { get; } = new();
        public List<Kind> Kinder { get; } = new();

        /// <summary>Hauswurzel: <c>.epos-dialog</c>, kein Katalogdialog.</summary>
        public bool Hauswurzel => WurzelKlassen.Contains("epos-dialog") && !WurzelKlassen.Contains("epos-katalog-dialog");

        /// <summary>Ein offenes Element beim Lesen.</summary>
        private sealed class Knoten
        {
            public Knoten(string name, List<string> klassen) { Name = name; Klassen = klassen; }
            public string Name { get; }
            public List<string> Klassen { get; }
            /// <summary>Ein Primaerknopf ist unmittelbares Kind.</summary>
            public bool Unmittelbar { get; set; }
            /// <summary>Ein Primaerknopf steht tiefer darin (oder eine SpeichernLeiste).</summary>
            public bool Tiefer { get; set; }
        }

        public static Wurzelbild? Lesen(string razor)
        {
            string t = Markup(razor);
            var bild = new Wurzelbild();
            var stapel = new List<Knoten>();
            bool wurzelGefunden = false;
            foreach (Match m in TAG.Matches(t))
            {
                string name = m.Groups["name"].Value;
                if (m.Groups["zu"].Value == "/")
                {
                    int i = stapel.FindLastIndex(s => s.Name == name);
                    if (i < 0) continue;
                    if (i == 1) Abschliessen(bild, stapel[1]);   // ein Kind der Wurzel schliesst
                    stapel.RemoveRange(i, stapel.Count - i);
                    if (i == 0) break;                            // die Wurzel schliesst
                    continue;
                }

                var knoten = new Knoten(name, Klassen(m.Groups["attr"].Value));
                bool primaer = knoten.Klassen.Contains("epos-knopf--primaer") || name == "SpeichernLeiste";
                if (!wurzelGefunden)
                {
                    wurzelGefunden = true;
                    bild.WurzelKlassen.AddRange(knoten.Klassen);
                }
                else if (primaer && stapel.Count == 2) stapel[1].Unmittelbar = true;
                else if (primaer && stapel.Count > 2) stapel[1].Tiefer = true;

                bool offen = m.Groups["leer"].Value != "/" && !LEER.Contains(name);
                if (stapel.Count == 1 && !offen) Abschliessen(bild, knoten);
                if (offen) stapel.Add(knoten);
                else if (stapel.Count == 0) break;   // die Wurzel selbst ist leer
            }
            return wurzelGefunden ? bild : null;
        }

        private static void Abschliessen(Wurzelbild bild, Knoten k)
        {
            bool leiste = k.Name == "div" && k.Klassen.Contains("epos-leiste");
            bool fussblock = k.Name == "div" && k.Klassen.Contains("epos-dialog-fuss");
            bool schluss = k.Name == "SpeichernLeiste"
                           || (leiste && k.Unmittelbar)
                           || (fussblock && (k.Unmittelbar || k.Tiefer));
            bild.Kinder.Add(new Kind(k.Name, k.Klassen, schluss));
        }

        private static List<string> Klassen(string attribute)
        {
            Match m = Regex.Match(attribute, @"\bclass=""(?<k>[^""]*)""");
            if (!m.Success) return new List<string>();
            // Razor-Ausdruecke in der Klasse (@(x ? "a" : "b")) zaehlen mit ihren Literalen.
            return Regex.Matches(m.Groups["k"].Value, @"[A-Za-z][\w-]*").Select(x => x.Value).ToList();
        }
    }

    // =====================================================================
    //  Hilfen
    // =====================================================================

    /// <summary>Die Komponenten, die eine Hülle als eigenes Fenster öffnet, mit ihrer Datei.</summary>
    private static List<(string Typ, string Datei)> Fensterdialoge()
    {
        var typen = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string datei in Directory.GetFiles(Path.Combine(Wurzel(), "WindowsFormsApplication1"), "*.cs", SearchOption.AllDirectories))
        {
            if (datei.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                || datei.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)) continue;
            foreach (Match m in Regex.Matches(Code(File.ReadAllText(datei)), @"new\s+BlazorDialogForm<\s*(?<t>[\w.]+)\s*>"))
                typen.Add(m.Groups["t"].Value.Split('.').Last());
        }
        var razor = Directory.GetFiles(Path.Combine(Wurzel(), "EPOS.UI"), "*.razor", SearchOption.AllDirectories)
            .ToLookup(p => Path.GetFileNameWithoutExtension(p), StringComparer.Ordinal);
        var liste = typen.Where(t => razor.Contains(t)).Select(t => (t, razor[t].First())).ToList();
        Assert.True(liste.Count >= 40, "Nur " + liste.Count + " Fensterdialoge gefunden.");
        return liste;
    }

    /// <summary>Markup ohne Razor-Kommentare, HTML-Kommentare und den @code-Block.</summary>
    private static string Markup(string razor)
    {
        string t = Regex.Replace(razor, @"@\*.*?\*@", "", RegexOptions.Singleline);
        t = Regex.Replace(t, @"<!--.*?-->", "", RegexOptions.Singleline);
        int code = t.LastIndexOf("@code", StringComparison.Ordinal);
        return code >= 0 ? t[..code] : t;
    }

    /// <summary>C#-Quelltext ohne Kommentare.</summary>
    private static string Code(string cs)
    {
        string t = Regex.Replace(cs, @"/\*.*?\*/", "", RegexOptions.Singleline);
        return Regex.Replace(t, @"//[^\n]*", "");
    }

    private static int Vorkommen(string text, string teil)
    {
        int n = 0;
        for (int i = text.IndexOf(teil, StringComparison.Ordinal); i >= 0; i = text.IndexOf(teil, i + teil.Length, StringComparison.Ordinal))
            n++;
        return n;
    }

    private static int ZIndex(string rumpf)
    {
        Match m = Regex.Match(rumpf, @"z-index:\s*(?<z>\d+)");
        Assert.True(m.Success, "kein z-index im Rumpf");
        return int.Parse(m.Groups["z"].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string Hausblatt() => File.ReadAllText(Path.Combine(Wurzel(), "EPOS.UI", "wwwroot", "epos-ui.css"));

    /// <summary>Der Rumpf der Regel, deren Selektor mit <paramref name="selektor"/> beginnt (am Zeilenanfang).</summary>
    private static string Regelblock(string selektor)
    {
        string css = Hausblatt().Replace("\r\n", "\n", StringComparison.Ordinal);
        int a = css.IndexOf("\n" + selektor, StringComparison.Ordinal);
        Assert.True(a >= 0, "Die Regel \"" + selektor + "\" steht nicht im Hausblatt");
        int auf = css.IndexOf('{', a + selektor.Length - 1);
        int zu = css.IndexOf('}', auf);
        return css.Substring(auf + 1, zu - auf - 1);
    }

    private static string Lies(params string[] teile)
    {
        string pfad = Path.Combine(new[] { Wurzel() }.Concat(teile).ToArray());
        Assert.True(File.Exists(pfad), "Die Datei fehlt: " + pfad);
        return File.ReadAllText(pfad);
    }

    /// <summary>Die Wurzel des Arbeitsbaums, vom Testausgabeordner aus gesucht.</summary>
    private static string Wurzel()
    {
        var ordner = new DirectoryInfo(AppContext.BaseDirectory);
        while (ordner is not null && !Directory.Exists(Path.Combine(ordner.FullName, "WindowsFormsApplication1", "Views")))
            ordner = ordner.Parent;
        Assert.True(ordner is not null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
        return ordner!.FullName;
    }
}
