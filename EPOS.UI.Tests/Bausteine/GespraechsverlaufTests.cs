using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Der Baustein <see cref="Gespraechsverlauf"/> — Zeuge T-1 (G-1 … G-12,
/// iU9-W15b.6).
///
/// <para><b>Was hier bewiesen wird.</b> Der Vorläufer <c>Form_KiChat</c> hatte
/// genau eine Ausgabestelle — <c>SchreibeZeile(text, farbe, fett)</c> — und über
/// die ganze Maske hinweg acht Farben und zwei Schriftschnitte. Diese zehn
/// Rollen sind der ganze Ausgabewortschatz des Assistenten, und sie müssen
/// vollständig ankommen; eine verlorene Rolle heißt, dass ein Fehler wie eine
/// Auskunft aussieht.</para>
///
/// <para><b>Und drei Zusagen, die es im Vorläufer nicht gab.</b> Fremdtext wird
/// nie als Markup ausgegeben (G-4), die Komponente rät nicht, was ein Link ist
/// (G-5), und sie öffnet nichts selbst (G-6). Alle drei zielen auf dieselbe
/// Stelle: In dieser Liste steht Text, den ein Modell geschrieben hat.</para>
///
/// <para>Die Klasse pinnt die Sprache selbst (Regel seit W8) — sie prüft
/// deutsche Texte, und xunit gibt keine Reihenfolge vor.</para>
/// </summary>
public class GespraechsverlaufTests : BunitContext
{
    public GespraechsverlaufTests()
    {
        DeutscheOberflaeche();

        // Der Baustein lädt sein Bildlaufmodul dynamisch. In Loose-Mode
        // beantwortet bunit den import und jeden Aufruf mit dem Standardwert;
        // die Prüfung von G-3 setzt das Modul darunter ausdrücklich auf.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>
    /// Kultur, UI-Kultur und die beiden Prozessvorgaben auf <c>de-DE</c> — sonst
    /// hängt das Ergebnis daran, welche Testklasse zuerst lief (Ursache der
    /// W12-Rotmeldung).
    /// </summary>
    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
    }

    private static Gespraechszeile Z(Gespraechsrolle rolle, string text,
                                     string? adresse = null, long kennung = 0)
        => new(rolle, text, adresse, kennung);

    // ==================================================================
    //  G-1  Zehn Rollen, zehn Klassen
    // ==================================================================

    [Theory]
    [InlineData(Gespraechsrolle.Anwender, "epos-verlauf-zeile--anwender")]
    [InlineData(Gespraechsrolle.Assistent, "epos-verlauf-zeile--assistent")]
    [InlineData(Gespraechsrolle.AssistentKopf, "epos-verlauf-zeile--assistentkopf")]
    [InlineData(Gespraechsrolle.Ueberschrift, "epos-verlauf-zeile--ueberschrift")]
    [InlineData(Gespraechsrolle.Leise, "epos-verlauf-zeile--leise")]
    [InlineData(Gespraechsrolle.Erfolg, "epos-verlauf-zeile--erfolg")]
    [InlineData(Gespraechsrolle.Warnung, "epos-verlauf-zeile--warnung")]
    [InlineData(Gespraechsrolle.Fehler, "epos-verlauf-zeile--fehler")]
    [InlineData(Gespraechsrolle.Bestaetigung, "epos-verlauf-zeile--bestaetigung")]
    [InlineData(Gespraechsrolle.Leerzeile, "epos-verlauf-zeile--leerzeile")]
    public void G1_Jede_Rolle_traegt_genau_ihre_Klasse(Gespraechsrolle rolle, string klasse)
    {
        var cut = Render<Gespraechsverlauf>(p => p
            .Add(x => x.Zeilen, new[] { Z(rolle, "Eine Zeile") }));

        var zeile = cut.Find("p.epos-verlauf-zeile");

        Assert.Contains(klasse, zeile.ClassName);
        // Genau EINE Rollenklasse - sonst überlagern sich zwei Farben.
        Assert.Single(zeile.ClassName!.Split(' '), k => k.StartsWith("epos-verlauf-zeile--"));
    }

    /// <summary>Die zehn Rollen des Aufzählungstyps sind vollständig belegt.</summary>
    [Fact]
    public void G1_Es_sind_genau_zehn_Rollen()
    {
        Assert.Equal(10, Enum.GetValues<Gespraechsrolle>().Length);
    }

    // ==================================================================
    //  G-2  Die Reihenfolge bleibt
    // ==================================================================

    /// <summary>
    /// Der Vorläufer hängte immer an (<c>AppendText</c>); es gibt keine Sortierung
    /// und darf keine geben — die Antwort steht unter der Frage, zu der sie gehört.
    /// </summary>
    [Fact]
    public void G2_Die_Reihenfolge_bleibt_wie_uebergeben()
    {
        var cut = Render<Gespraechsverlauf>(p => p.Add(x => x.Zeilen, new[]
        {
            Z(Gespraechsrolle.Anwender, "Sie: Wie lege ich ein Projekt an?", kennung: 1),
            Z(Gespraechsrolle.AssistentKopf, "Assistent:", kennung: 2),
            Z(Gespraechsrolle.Assistent, "Über Datei / Neu.", kennung: 3)
        }));

        var texte = cut.FindAll("p.epos-verlauf-zeile").Select(p => p.TextContent).ToArray();

        Assert.Equal(new[]
        {
            "Sie: Wie lege ich ein Projekt an?",
            "Assistent:",
            "Über Datei / Neu."
        }, texte);
    }

    /// <summary>
    /// Eine dazugekommene Zeile hängt hinten an; die vorhandenen bleiben stehen.
    /// (Die stabile Kennung sorgt dafür, dass Blazor die Liste nicht neu baut.)
    /// </summary>
    [Fact]
    public void G2_Neue_Zeilen_haengen_hinten_an()
    {
        var zeilen = new List<Gespraechszeile>
        {
            Z(Gespraechsrolle.Anwender, "Erste", kennung: 1)
        };

        var cut = Render<Gespraechsverlauf>(p => p.Add(x => x.Zeilen, zeilen));
        Assert.Single(cut.FindAll("p.epos-verlauf-zeile"));

        zeilen.Add(Z(Gespraechsrolle.Assistent, "Zweite", kennung: 2));
        cut.Render(p => p.Add(x => x.Zeilen, zeilen.ToArray()));

        var texte = cut.FindAll("p.epos-verlauf-zeile").Select(p => p.TextContent).ToArray();
        Assert.Equal(new[] { "Erste", "Zweite" }, texte);
    }

    // ==================================================================
    //  G-3  Nachfuehrung
    // ==================================================================

    /// <summary>
    /// Kommen Zeilen dazu, fragt der Baustein die Bildlaufstellung und springt ans
    /// Ende. Beides läuft über sein Modul <c>epos-verlauf.js</c> — das ist der
    /// einzige JavaScript-Anteil von <c>EPOS.UI</c>.
    /// </summary>
    [Fact]
    public void G3_Nachfuehren_springt_ans_Ende()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        var modul = JSInterop.SetupModule("./_content/EPOS.UI/epos-verlauf.js");
        modul.Setup<bool>("istUnten", _ => true).SetResult(true);
        var ansEnde = modul.SetupVoid("ansEnde", _ => true);

        Render<Gespraechsverlauf>(p => p
            .Add(x => x.Zeilen, new[] { Z(Gespraechsrolle.Anwender, "Erste", kennung: 1) }));

        Assert.NotEmpty(ansEnde.Invocations);
    }

    /// <summary>
    /// <b>Entscheid E-12.</b> Steht der Anwender NICHT unten, wird nicht gesprungen —
    /// sonst reißt ihm die nächste Antwort den Verlauf weg, den er gerade liest. Der
    /// Vorläufer kannte diese Rücksicht nicht (<c>ScrollToCaret</c> sprang immer).
    /// </summary>
    [Fact]
    public void G3_Wer_oben_nachliest_bleibt_dort()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        var modul = JSInterop.SetupModule("./_content/EPOS.UI/epos-verlauf.js");
        modul.Setup<bool>("istUnten", _ => true).SetResult(false);
        var ansEnde = modul.SetupVoid("ansEnde", _ => true);

        Render<Gespraechsverlauf>(p => p
            .Add(x => x.Zeilen, new[] { Z(Gespraechsrolle.Anwender, "Erste", kennung: 1) }));

        Assert.Empty(ansEnde.Invocations);
    }

    /// <summary>Ohne <c>Nachfuehren</c> wird gar nicht erst gemessen.</summary>
    [Fact]
    public void G3_Ohne_Nachfuehren_wird_nichts_gerufen()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        var modul = JSInterop.SetupModule("./_content/EPOS.UI/epos-verlauf.js");
        var ansEnde = modul.SetupVoid("ansEnde", _ => true);

        Render<Gespraechsverlauf>(p => p
            .Add(x => x.Nachfuehren, false)
            .Add(x => x.Zeilen, new[] { Z(Gespraechsrolle.Anwender, "Erste", kennung: 1) }));

        Assert.Empty(ansEnde.Invocations);
    }

    // ==================================================================
    //  G-4  Fremdtext ist niemals Markup
    // ==================================================================

    /// <summary>
    /// <b>Der wichtigste Fall des Bausteins.</b> Der Antworttext kommt vom Modell.
    /// Er wird als TEXT gesetzt; <c>&lt;b&gt;</c> erscheint wörtlich und erzeugt
    /// kein Element. Ein Markdown- oder HTML-Wandler wäre genau hier die
    /// Angriffsfläche (Entscheid E-7b: nein).
    /// </summary>
    [Fact]
    public void G4_Fremdtext_wird_nie_als_Markup_ausgegeben()
    {
        const string boes = "<b>fett</b> und <script>alert(1)</script>";

        var cut = Render<Gespraechsverlauf>(p => p
            .Add(x => x.Zeilen, new[] { Z(Gespraechsrolle.Assistent, boes) }));

        Assert.Equal(boes, cut.Find("p.epos-verlauf-zeile").TextContent);
        Assert.Empty(cut.FindAll("p.epos-verlauf-zeile b"));
        Assert.Empty(cut.FindAll("script"));
    }

    // ==================================================================
    //  G-5 / G-6  Verweise
    // ==================================================================

    /// <summary>
    /// Nur eine Zeile MIT Adresse ist ein Verweis. Ein Text, der wie eine URL
    /// aussieht, aber keine Adresse trägt, bleibt Text — der Vorläufer ließ das die
    /// <c>RichtextBox</c> erraten (<c>DetectUrls</c>), ein HTML-Baustein darf das
    /// nicht.
    /// </summary>
    [Fact]
    public void G5_Nur_Zeilen_mit_Adresse_sind_Verweise()
    {
        var cut = Render<Gespraechsverlauf>(p => p.Add(x => x.Zeilen, new[]
        {
            Z(Gespraechsrolle.Leise, "Siehe https://wiki.epos-plan.de/Projekt", kennung: 1),
            Z(Gespraechsrolle.Leise, "• Projekt anlegen", "https://wiki.epos-plan.de/Projekt", 2)
        }));

        var verweise = cut.FindAll("button.epos-verlauf-verweis");

        Assert.Single(verweise);
        Assert.Equal("• Projekt anlegen", verweise[0].TextContent);
    }

    /// <summary>
    /// Ein Klick meldet GENAU die Adresse und öffnet nichts. Das Öffnen ist Sache
    /// der Plattform — unter Windows <c>Dienste.Datei</c>, auf iOS der Launcher.
    /// </summary>
    [Fact]
    public void G6_Klick_meldet_die_Adresse_und_oeffnet_nichts()
    {
        string? gemeldet = null;

        var cut = Render<Gespraechsverlauf>(p => p
            .Add(x => x.Zeilen, new[]
            {
                Z(Gespraechsrolle.Leise, "• Projekt anlegen", "https://wiki.epos-plan.de/Projekt", 1)
            })
            .Add(x => x.AdresseGewaehlt, EventCallback.Factory.Create<string>(this, a => gemeldet = a)));

        cut.Find("button.epos-verlauf-verweis").Click();

        Assert.Equal("https://wiki.epos-plan.de/Projekt", gemeldet);
    }

    // ==================================================================
    //  G-7  Beschaeftigt
    // ==================================================================

    /// <summary>
    /// <c>Beschaeftigt</c> zeigt genau EINE zusätzliche Zeile mit
    /// <c>role="status"</c>; <c>false</c> entfernt sie. Der Vorläufer schrieb
    /// stattdessen „Der Assistent denkt nach…" in eine Statuszeile.
    /// </summary>
    [Fact]
    public void G7_Beschaeftigt_zeigt_genau_eine_Statuszeile()
    {
        var cut = Render<Gespraechsverlauf>(p => p
            .Add(x => x.Zeilen, new[] { Z(Gespraechsrolle.Anwender, "Sie: Frage") })
            .Add(x => x.Beschaeftigt, true)
            .Add(x => x.BeschaeftigtText, "Der Assistent denkt nach..."));

        var status = cut.FindAll("p[role=status]");
        Assert.Single(status);
        Assert.Contains("Der Assistent denkt nach...", status[0].TextContent);

        cut.Render(p => p
            .Add(x => x.Zeilen, new[] { Z(Gespraechsrolle.Anwender, "Sie: Frage") })
            .Add(x => x.Beschaeftigt, false)
            .Add(x => x.BeschaeftigtText, "Der Assistent denkt nach..."));

        Assert.Empty(cut.FindAll("p[role=status]"));
    }

    // ==================================================================
    //  G-8  Kopieren
    // ==================================================================

    /// <summary>
    /// Kopieren liefert alle Zeilen als Text, je Zeile eine Zeile — ohne Farben und
    /// ohne zusätzliche Rollenpräfixe: „Sie: " und „Assistent:" stehen bereits im
    /// Text. Die Komponente schreibt NICHT in die Zwischenablage; das kann
    /// <c>EPOS.UI</c> gar nicht (Entscheid E-11, Neuerung).
    /// </summary>
    [Fact]
    public void G8_Kopieren_liefert_den_ganzen_Verlauf_als_Text()
    {
        string? text = null;

        var cut = Render<Gespraechsverlauf>(p => p
            .Add(x => x.Zeilen, new[]
            {
                Z(Gespraechsrolle.Anwender, "Sie: Wie geht das?", kennung: 1),
                Z(Gespraechsrolle.AssistentKopf, "Assistent:", kennung: 2),
                Z(Gespraechsrolle.Assistent, "So geht das.", kennung: 3)
            })
            .Add(x => x.KopierenText, "Verlauf kopieren")
            .Add(x => x.Kopieren, EventCallback.Factory.Create<string>(this, t => text = t)));

        cut.Find("button.epos-verlauf-kopieren").Click();

        Assert.NotNull(text);
        var zeilen = text!.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
        Assert.Equal(new[] { "Sie: Wie geht das?", "Assistent:", "So geht das." }, zeilen);
    }

    /// <summary>Ohne Beschriftung gibt es keinen Kopierknopf.</summary>
    [Fact]
    public void G8_Ohne_Beschriftung_kein_Kopierknopf()
    {
        var cut = Render<Gespraechsverlauf>(p => p
            .Add(x => x.Zeilen, new[] { Z(Gespraechsrolle.Assistent, "Text") }));

        Assert.Empty(cut.FindAll("button.epos-verlauf-kopieren"));
    }

    // ==================================================================
    //  G-9  Die leere Liste
    // ==================================================================

    /// <summary>
    /// Eine leere Liste zeichnet den Rahmen, keine Zeile und keine Ausnahme — das
    /// ist der Zustand, bevor die Begrüßung geschrieben ist.
    /// </summary>
    [Fact]
    public void G9_Leere_Liste_zeichnet_den_Rahmen_ohne_Zeile()
    {
        var cut = Render<Gespraechsverlauf>();

        Assert.NotNull(cut.Find("div.epos-verlauf"));
        Assert.Empty(cut.FindAll("p.epos-verlauf-zeile"));
    }

    // ==================================================================
    //  G-10  Sehr langer Text
    // ==================================================================

    /// <summary>
    /// Ein überlanger Text bricht um und erzeugt keinen Waagerecht-Bildlauf. Die
    /// Zusage steckt in der Klasse (<c>overflow-wrap: anywhere</c>); hier wird
    /// geprüft, dass der Text ungekürzt ankommt und die Klasse trägt.
    /// </summary>
    [Fact]
    public void G10_Sehr_langer_Text_bleibt_ganz_und_bricht_um()
    {
        string lang = new string('W', 5000);

        var cut = Render<Gespraechsverlauf>(p => p
            .Add(x => x.Zeilen, new[] { Z(Gespraechsrolle.Assistent, lang) }));

        var zeile = cut.Find("p.epos-verlauf-zeile");
        Assert.Equal(5000, zeile.TextContent.Length);
        Assert.Contains("epos-verlauf-zeile", zeile.ClassName);
    }

    /// <summary>
    /// Zeilenumbrüche INNERHALB einer Zeile bleiben erhalten — die Antwort des
    /// Modells ist mehrzeilig, und der Rechtshinweis wäre sonst eine Textwurst.
    /// </summary>
    [Fact]
    public void G10_Umbrueche_innerhalb_einer_Zeile_bleiben()
    {
        var cut = Render<Gespraechsverlauf>(p => p
            .Add(x => x.Zeilen, new[] { Z(Gespraechsrolle.Assistent, "Erste Zeile\nZweite Zeile") }));

        Assert.Contains("\n", cut.Find("p.epos-verlauf-zeile").TextContent);
    }

    // ==================================================================
    //  G-11 / G-12  Tastatur und Sprachausgabe
    // ==================================================================

    /// <summary>
    /// Die Liste ist fokussierbar und damit mit ↑/↓/Bild↑/Bild↓/Pos1/Ende
    /// blätterbar — die <c>RichTextBox</c> des Vorläufers war es auch. Verweise
    /// sind eigene Tabstopps (sie sind Knöpfe).
    /// </summary>
    [Fact]
    public void G11_Die_Liste_ist_fokussierbar_und_Verweise_sind_eigene_Tabstopps()
    {
        var cut = Render<Gespraechsverlauf>(p => p.Add(x => x.Zeilen, new[]
        {
            Z(Gespraechsrolle.Leise, "• Projekt anlegen", "https://wiki.epos-plan.de/Projekt", 1)
        }));

        Assert.Equal("0", cut.Find("div.epos-verlauf").GetAttribute("tabindex"));
        Assert.NotNull(cut.Find("button.epos-verlauf-verweis"));
    }

    /// <summary>
    /// <c>role="log"</c> mit <c>aria-live="polite"</c> und
    /// <c>aria-relevant="additions"</c>: Eine NEUE Antwort wird vorgelesen, der
    /// ganze Verlauf nicht. Die modale MessageBox leistete das bisher nebenbei.
    /// </summary>
    [Fact]
    public void G12_Die_Liste_meldet_sich_als_Protokoll_mit_hoeflicher_Ansage()
    {
        var cut = Render<Gespraechsverlauf>(p => p
            .Add(x => x.Bezeichnung, "Gesprächsverlauf")
            .Add(x => x.Zeilen, new[] { Z(Gespraechsrolle.Assistent, "Antwort") }));

        var liste = cut.Find("div.epos-verlauf");

        Assert.Equal("log", liste.GetAttribute("role"));
        Assert.Equal("polite", liste.GetAttribute("aria-live"));
        Assert.Equal("additions", liste.GetAttribute("aria-relevant"));
        Assert.Equal("Gesprächsverlauf", liste.GetAttribute("aria-label"));
    }

    // ==================================================================
    //  E-3  Der Fussbereich liegt IM Bildlauf
    // ==================================================================

    /// <summary>
    /// Der Bestätigungsblock steht UNTEN im Verlauf, nach der letzten Zeile — nicht
    /// oben am Fenster wie im Vorläufer. Der Kommentar dort verlangte „neben dem,
    /// was zu ihr geführt hat"; in einer scrollenden Liste ist das unten.
    /// </summary>
    [Fact]
    public void E3_Der_Fussbereich_steht_im_Bildlauf_nach_der_letzten_Zeile()
    {
        var cut = Render<Gespraechsverlauf>(p => p
            .Add(x => x.Zeilen, new[] { Z(Gespraechsrolle.Anwender, "Sie: Frage") })
            .Add(x => x.Fussbereich, (RenderFragment)(b =>
            {
                b.OpenElement(0, "div");
                b.AddAttribute(1, "id", "block");
                b.AddContent(2, "Ausführen?");
                b.CloseElement();
            })));

        var liste = cut.Find("div.epos-verlauf");

        // Er liegt IM Bildlaufbereich ...
        var kinder = liste.Children.Select(k => k.ClassName ?? "").ToList();
        int fuss = kinder.FindIndex(k => k.Contains("epos-verlauf-fuss"));
        int letzteZeile = kinder.FindLastIndex(k => k.Contains("epos-verlauf-zeile"));

        Assert.True(fuss >= 0, "Der Fussbereich liegt nicht im Bildlaufbereich.");
        // ... und HINTER der letzten Zeile.
        Assert.True(fuss > letzteZeile, "Der Fussbereich steht vor der letzten Zeile.");
        Assert.Equal("Ausführen?", cut.Find("#block").TextContent);
    }

    /// <summary>Ohne Fußbereich gibt es kein leeres <c>div</c>.</summary>
    [Fact]
    public void E3_Ohne_Fussbereich_kein_leerer_Block()
    {
        var cut = Render<Gespraechsverlauf>(p => p
            .Add(x => x.Zeilen, new[] { Z(Gespraechsrolle.Anwender, "Sie: Frage") }));

        Assert.Empty(cut.FindAll("div.epos-verlauf-fuss"));
    }
}
