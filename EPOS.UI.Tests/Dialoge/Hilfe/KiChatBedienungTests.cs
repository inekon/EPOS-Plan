using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Hilfe;
using EPOS.UI.Dienste;
using KiKern;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Hilfe;

/// <summary>
/// <see cref="KiChatDialog"/> — <b>JEDES Bedienelement des Hilfe-Assistenten</b>
/// (Auftrag #219, Anwenderbefunde <b>KI‑D‑B‑1</b> „die Eingabe funktioniert nicht" und
/// <b>KI‑D‑B‑2</b> „Online-Dokumentation öffnen tut nichts", beide 11.09.2026).
///
/// <para><b>Warum ein zweiter Zeuge neben <c>KiChatDialogTests</c>.</b> Jener prüft die
/// drei Betriebszustände, die Sperre und den Bestätigungsblock — also das VERHALTEN des
/// Chats. Hier wird eine andere Frage gestellt, und zwar für jeden Knopf und jeden
/// Verweis einzeln: <b>Kommt der Klick überhaupt irgendwo an?</b> Genau diese Frage hat
/// bis #219 niemand gestellt, und deshalb ist ein toter Fußleistenverweis ein halbes
/// Jahr lang niemandem aufgefallen.</para>
///
/// <para><b>Was hier NICHT geprüft werden kann.</b> Ob die Eingabe am GERÄT ankommt,
/// entscheidet zur Hälfte die Plattformhülle (das nicht-modale Fenster muss die Tastatur
/// des Betriebssystems bekommen). bunit kennt kein Fenster; geprüft wird die Hälfte, die
/// in dieser Bibliothek liegt — das Feld ist nicht gesperrt, es trägt
/// <c>autofocus</c>, und der Dialog holt den Schreibzeiger ausdrücklich. Die andere
/// Hälfte hält <c>KiChatOeffnerTests</c> am Quelltext der Hülle fest.</para>
///
/// <para>Die Klasse pinnt die Sprache selbst (Regel seit W8).</para>
/// </summary>
public class KiChatBedienungTests : EposBunitContext
{
    public KiChatBedienungTests()
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>Die Adresse der Online-Dokumentation, wie sie die Hülle liefert.</summary>
    private const string DOKU = "https://wiki.epos-plan.de";

    private static KiChatTexte Texte() => new()
    {
        Verlauf = "Hilfe-Assistent",
        VerlaufLeer = "Noch keine Frage gestellt.",
        ErklaerungMehr = "Was der Assistent tut und was übertragen wird",
        KontextFormat = "Bereich: {0}",
        KontextLeer = "Bereich: (nicht erkannt)",
        DialognameFormat = "Dialog: {0}",
        VerbrauchFormat = "Heute genutzt: {0} von {1}",
        Eingabe = "Ihre Frage",
        Fragen = "Fragen",
        Suchen = "Nur suchen",
        Aktionen = "Aktionen zulassen",
        AktionenEin = "Aktionen sind jetzt zugelassen.",
        AktionenAus = "Aktionen sind abgeschaltet.",
        Werkzeuge = "Werkzeuge...",
        WerkzeugeTitel = "Aktionen von Hand",
        HinweisVorn = "Es werden nur Hilfetexte übertragen. ",
        HinweisLink = "Rechtshinweis anzeigen",
        RechtshinweisTitel = "Rechtshinweis",
        Doku = "Online-Dokumentation öffnen",
        DokuAdresse = DOKU,
        Vorschau = "Was wird gesendet?",
        VorschauTitel = "Sendevorschau",
        Protokoll = "Protokoll anzeigen",
        ProtokollTitel = "Aktionsprotokoll",
        Einstellungen = "Einstellungen...",
        EinstellungenTitel = "KI-Assistent - Einstellungen",
        Gespeichert = "Die Angaben wurden gespeichert.",
        Schliessen = "Schließen",
        Kopieren = "Verlauf kopieren"
    };

    /// <summary>
    /// Der Chat, wie ihn die Hülle aus einer ANSICHT heraus aufmacht: mit Bereich und
    /// Dialognamen, ohne laufende Aktion. Genau die Lage des Bildschirmfotos zu
    /// KI‑D‑B‑1 („Bereich: Detaillierte Simulation | Dialog: Simulation").
    /// </summary>
    private IRenderedComponent<KiChatDialog> AusAnsicht(
        Action<ComponentParameterCollectionBuilder<KiChatDialog>>? mehr = null,
        bool hilfeBetrieb = false)
    {
        return Render<KiChatDialog>(p =>
        {
            p.Add(x => x.Texte, Texte())
             .Add(x => x.Kontext, "Detaillierte Simulation")
             .Add(x => x.Dialogname, "Simulation")
             .Add(x => x.HilfeBetrieb, hilfeBetrieb)
             .Add(x => x.Eingerichtet, true)
             .Add(x => x.AnfragenHeute, 0)
             .Add(x => x.Tageslimit, 50)
             .Add(x => x.Aktionen, Array.Empty<KiAktion>());
            mehr?.Invoke(p);
        });
    }

    private static IElement Feld(IRenderedComponent<KiChatDialog> cut)
        => cut.Find("textarea.epos-kieingabe-feld");

    private static IElement Verweis(IRenderedComponent<KiChatDialog> cut, string text)
        => cut.FindAll("button.epos-kichat-link").First(b => b.TextContent.Trim() == text);

    private static IElement Knopf(IRenderedComponent<KiChatDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // =====================================================================
    //  KI-D-B-1: die Eingabe
    // =====================================================================

    /// <summary>
    /// <b>Das Eingabefeld ist beim Öffnen aus einer Ansicht NICHT gesperrt.</b>
    ///
    /// <para>Das ist die Gegenprobe zur Hypothese (b) des Befundes: Der Anwender sah
    /// aktiv gezeichnete Knöpfe und konnte trotzdem nicht tippen — käme das aus
    /// <c>Gesperrt</c>, trüge das Feld ein <c>disabled</c>, und die Knöpfe wären
    /// mitgesperrt. Hier steht die Lage des Bildschirmfotos nachgestellt: Kontext aus
    /// einer Ansicht, keine laufende Aktion, kein <c>Belegt</c>.</para>
    /// </summary>
    [Fact]
    public void Das_Eingabefeld_ist_aus_einem_Ansichtskontext_nicht_gesperrt()
    {
        var cut = AusAnsicht();

        Assert.False(Feld(cut).HasAttribute("disabled"));
        Assert.False(Knopf(cut, "Fragen").HasAttribute("disabled"));
        Assert.False(Knopf(cut, "Nur suchen").HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>Das Eingabefeld trägt <c>autofocus</c></b> — die halbe Behebung von KI‑D‑B‑1.
    /// </summary>
    /// <remarks>
    /// <c>autofocus</c> wirkt beim ERSTEN Einfügen in das Dokument; die andere Hälfte
    /// (das zweite Öffnen desselben Fensters) trägt der ausdrückliche Griff, siehe
    /// <see cref="Ein_neuer_Aufrufkontext_holt_den_Schreibzeiger_zurueck"/>.
    /// </remarks>
    [Fact]
    public void Das_Eingabefeld_traegt_autofocus()
    {
        Assert.True(Feld(AusAnsicht()).HasAttribute("autofocus"));
    }

    /// <summary>
    /// <b>Der Dialog holt den Schreibzeiger ausdrücklich</b> (<c>FocusAsync</c> auf dem
    /// Textfeld) — nicht nur über das Merkmal.
    /// </summary>
    /// <remarks>
    /// Gemessen wird der JS-Aufruf, den <c>ElementReference.FocusAsync</c> absetzt;
    /// bunit zeichnet ihn im lockeren Modus auf. Ein Merkmal allein genügt nicht: Wird
    /// das Fenster ein zweites Mal nach vorn geholt, gibt es kein erstes Einfügen mehr.
    /// </remarks>
    [Fact]
    public void Der_Dialog_setzt_den_Schreibzeiger_in_das_Eingabefeld()
    {
        AusAnsicht();

        Assert.Contains(JSInterop.Invocations,
                        i => i.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// <b>Gegenprobe:</b> Läuft eine Aktion (<c>Belegt</c>), ist das Feld gesperrt, es
    /// trägt kein <c>autofocus</c>, und der Zeiger wird nicht gesetzt — ein gesperrtes
    /// Feld nimmt ihn nicht an, und der Griff bliebe wirkungslos.
    /// </summary>
    [Fact]
    public void Bei_laufender_Aktion_bleibt_das_Feld_gesperrt_und_ohne_Zeiger()
    {
        var cut = AusAnsicht(p => p.Add(x => x.Belegt, (Func<bool>)(() => true)));

        Assert.True(Feld(cut).HasAttribute("disabled"));
        Assert.False(Feld(cut).HasAttribute("autofocus"));
        Assert.DoesNotContain(JSInterop.Invocations,
                              i => i.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// <b>Im Hilfe-Betrieb ist die Eingabe genauso frei.</b> Der Abschalter nimmt den
    /// Modellweg, nicht die Tastatur — der Chat sucht dann lokal (Fachkonzept 11.9).
    /// </summary>
    [Fact]
    public void Im_Hilfebetrieb_ist_die_Eingabe_ebenso_frei()
    {
        var cut = AusAnsicht(hilfeBetrieb: true);

        Assert.False(Feld(cut).HasAttribute("disabled"));
        Assert.True(Feld(cut).HasAttribute("autofocus"));
    }

    /// <summary>
    /// <b>Ein NEUER Aufrufkontext holt den Schreibzeiger zurück</b> — der Fall „ein
    /// zweites Mal aus einer anderen Maske gefragt": Windows öffnet nur EIN Chatfenster
    /// und holt das stehende nach vorn (<c>KiChatHuelle.Oeffnen</c>), ein erstes
    /// Zeichnen gibt es dafür nicht mehr.
    /// </summary>
    [Fact]
    public async Task Ein_neuer_Aufrufkontext_holt_den_Schreibzeiger_zurueck()
    {
        KiChatSteuerung? steuerung = null;
        var cut = AusAnsicht(p => p.Add(x => x.Anmelden, (Action<KiChatSteuerung>)(s => steuerung = s)));

        Assert.NotNull(steuerung);
        int vorher = JSInterop.Invocations
            .Count(i => i.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));

        await cut.InvokeAsync(() => steuerung!.Kontext(
            new KiKontextangabe("Stromspeicher", "Auslegung", "", "")));

        int nachher = JSInterop.Invocations
            .Count(i => i.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));

        Assert.True(nachher > vorher,
                    "Nach einem neuen Aufrufkontext wurde der Schreibzeiger nicht erneut gesetzt.");
    }

    // =====================================================================
    //  KI-D-B-2: die Fussleiste und alles, was daneben steht
    // =====================================================================

    /// <summary>
    /// <b>„Online-Dokumentation öffnen" meldet die Adresse nach außen</b> — der Befund
    /// KI‑D‑B‑2, von der Komponentenseite gesehen.
    /// </summary>
    /// <remarks>
    /// Dass der Klick ankommt, war nie das Problem; die Hülle warf die Adresse weg
    /// (<c>Dienste.Datei.MitSystemOeffnen</c> prüft <c>File.Exists</c>). Dieser Fall
    /// hält die Komponentenhälfte fest, <c>KiChatOeffnerTests</c> die Hüllenhälfte.
    /// </remarks>
    [Fact]
    public void Der_Dokumentationsverweis_meldet_die_Adresse()
    {
        string? adresse = null;
        var cut = AusAnsicht(p => p.Add(x => x.AdresseGewaehlt,
            EventCallback.Factory.Create<string>(this, a => adresse = a)));

        Verweis(cut, "Online-Dokumentation öffnen").Click();

        Assert.Equal(DOKU, adresse);
    }

    /// <summary>
    /// <b>Gegenprobe:</b> Ohne Adresse meldet der Verweis nichts — und wirft auch
    /// nicht. Der Fall tritt auf, wenn eine Hülle den Text ohne die Basis-URL liefert.
    /// </summary>
    [Fact]
    public void Ohne_Adresse_meldet_der_Dokumentationsverweis_nichts()
    {
        int gerufen = 0;
        KiChatTexte ohne = Texte();
        ohne.DokuAdresse = "";

        var cut = Render<KiChatDialog>(p => p
            .Add(x => x.Texte, ohne)
            .Add(x => x.Kontext, "Detaillierte Simulation")
            .Add(x => x.Eingerichtet, true)
            .Add(x => x.Aktionen, Array.Empty<KiAktion>())
            .Add(x => x.AdresseGewaehlt,
                 EventCallback.Factory.Create<string>(this, _ => gerufen++)));

        Verweis(cut, "Online-Dokumentation öffnen").Click();

        Assert.Equal(0, gerufen);
    }

    /// <summary>„Was wird gesendet?" ruft den Vorschauweg und zeigt dessen Text.</summary>
    [Fact]
    public void Die_Sendevorschau_ruft_ihren_Weg()
    {
        int gerufen = 0;
        var cut = AusAnsicht(p => p.Add(x => x.Vorschau, (Func<bool, Task<string>>)(_ =>
        {
            gerufen++;
            return Task.FromResult("Der Anfragerumpf.");
        })));

        Verweis(cut, "Was wird gesendet?").Click();

        Assert.Equal(1, gerufen);
        Assert.Contains("Der Anfragerumpf.", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>„Protokoll anzeigen" ruft den Protokollweg und zeigt dessen Text.</summary>
    [Fact]
    public void Das_Protokoll_ruft_seinen_Weg()
    {
        int gerufen = 0;
        var cut = AusAnsicht(p => p.Add(x => x.Protokoll, (Func<Task<string>>)(() =>
        {
            gerufen++;
            return Task.FromResult("Zeile eins des Protokolls.");
        })));

        Verweis(cut, "Protokoll anzeigen").Click();

        Assert.Equal(1, gerufen);
        Assert.Contains("Zeile eins des Protokolls.", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>„Verlauf kopieren" gibt den TEXT heraus</b> — die Zwischenablage schreibt die
    /// Hülle. In der WebView2 ist <c>navigator.clipboard</c> nicht der Weg (er braucht
    /// einen sicheren Kontext); hier geht nichts durch den Browser.
    /// </summary>
    [Fact]
    public void Verlauf_kopieren_gibt_den_Text_an_die_Huelle()
    {
        string? text = null;
        var cut = AusAnsicht(p => p
            .Add(x => x.Suchen, (Func<string, Task<IReadOnlyList<Gespraechszeile>>>)(_ =>
                Task.FromResult<IReadOnlyList<Gespraechszeile>>(new[]
                {
                    new Gespraechszeile(Gespraechsrolle.Assistent, "Ein Suchtreffer.")
                })))
            .Add(x => x.Kopieren, EventCallback.Factory.Create<string>(this, t => text = t)));

        Feld(cut).Input("Pufferspeicher");
        Knopf(cut, "Nur suchen").Click();
        Verweis(cut, "Verlauf kopieren").Click();

        Assert.NotNull(text);
        Assert.Contains("Pufferspeicher", text!, StringComparison.Ordinal);
        Assert.Contains("Ein Suchtreffer.", text!, StringComparison.Ordinal);
    }

    /// <summary>
    /// „Rechtshinweis anzeigen" öffnet die ÜBERLAGERUNG, wenn die Hülle einen Inhalt
    /// mitgibt (Entscheid E‑5) — und nicht den Rückweg über den Delegaten.
    /// </summary>
    [Fact]
    public void Der_Rechtshinweis_erscheint_als_Ueberlagerung()
    {
        int delegatweg = 0;
        var cut = AusAnsicht(p => p
            .Add(x => x.Rechtshinweis, (Func<Task>)(() => { delegatweg++; return Task.CompletedTask; }))
            .Add(x => x.Rechtshinweisinhalt, (RenderFragment<EventCallback<bool>>)(
                 _ => b => b.AddMarkupContent(0, "<p>Der Rechtshinweis im Wortlaut.</p>"))));

        Verweis(cut, "Rechtshinweis anzeigen").Click();

        Assert.Equal(0, delegatweg);
        Assert.Contains("Der Rechtshinweis im Wortlaut.", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// „Einstellungen…" erscheint ebenfalls als Überlagerung — der Befund W15b‑B‑1,
    /// hier als bleibender Zeuge.
    /// </summary>
    [Fact]
    public void Die_Einstellungen_erscheinen_als_Ueberlagerung()
    {
        int delegatweg = 0;
        var cut = AusAnsicht(p => p
            .Add(x => x.Einstellungen, (Func<Task<bool>>)(() => { delegatweg++; return Task.FromResult(false); }))
            .Add(x => x.Einstellungsinhalt, (RenderFragment<EventCallback<bool>>)(
                 _ => b => b.AddMarkupContent(0, "<p>Der Schlüsselblock.</p>"))));

        Knopf(cut, "Einstellungen...").Click();

        Assert.Equal(0, delegatweg);
        Assert.Contains("Der Schlüsselblock.", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>„Werkzeuge…" öffnet die Werkzeugliste als Überlagerung.</summary>
    [Fact]
    public void Werkzeuge_oeffnet_die_Liste()
    {
        var cut = AusAnsicht();

        Knopf(cut, "Werkzeuge...").Click();

        Assert.Contains("Aktionen von Hand", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>„Schließen" meldet sich bei der Hülle — sie macht das Fenster zu.</summary>
    [Fact]
    public void Schliessen_meldet_sich_bei_der_Huelle()
    {
        int gerufen = 0;
        var cut = AusAnsicht(p => p.Add(x => x.Geschlossen,
            EventCallback.Factory.Create(this, () => gerufen++)));

        Knopf(cut, "Schließen").Click();

        Assert.Equal(1, gerufen);
    }

    /// <summary>
    /// „Aktionen zulassen" fragt die Einwilligung — und bleibt aus, wenn sie ausbleibt
    /// (der Riegel liegt im Kern, der Schalter nimmt ihn nicht vorweg).
    /// </summary>
    [Fact]
    public void Aktionen_zulassen_fragt_die_Einwilligung()
    {
        int gefragt = 0;
        var cut = AusAnsicht(p => p.Add(x => x.Einwilligen, (Func<Task<bool>>)(() =>
        {
            gefragt++;
            return Task.FromResult(false);
        })));

        cut.Find("span.epos-kieingabe-vorspann input[type=checkbox]").Change(true);

        Assert.Equal(1, gefragt);
        Assert.False(cut.Find("span.epos-kieingabe-vorspann input[type=checkbox]").HasAttribute("checked"));
    }

    /// <summary>
    /// Der KONTEXTLINK „Was der Assistent tut und was übertragen wird" ist eine
    /// <c>&lt;details&gt;</c>-Klappe und braucht weder Delegat noch JavaScript — er
    /// steht hier, damit die Elementtabelle des Berichts vollständig geprüft ist.
    /// </summary>
    [Fact]
    public void Der_Kontextlink_ist_eine_Klappe_ohne_Delegat()
    {
        var cut = AusAnsicht(p => p.Add(x => x.Begruessung, new[]
        {
            new Gespraechszeile(Gespraechsrolle.Assistent, "Stellen Sie Ihre Frage."),
            new Gespraechszeile(Gespraechsrolle.Leise, "Es werden nur Hilfetexte übertragen.")
        }));

        var klappe = cut.Find("details.epos-kichat-erklaerung");
        Assert.Equal("Was der Assistent tut und was übertragen wird",
                     klappe.QuerySelector("summary")!.TextContent.Trim());
    }

    /// <summary>
    /// Der <b>i-Knopf</b> oben rechts trägt den Hilfeschlüssel des Chats — und
    /// ausdrücklich KEINEN zweiten Assistentenknopf (ein Assistent im Assistenten
    /// führte im Kreis, Auftrag #199).
    /// </summary>
    [Fact]
    public void Der_i_Knopf_steht_im_Kopf_und_ohne_zweiten_Assistenten()
    {
        var cut = AusAnsicht();
        var kopf = cut.Find("span.epos-kichat-kopfknopf");

        Assert.NotNull(kopf.QuerySelector("button"));
        var knopf = cut.FindComponent<InfoKnopf>();
        Assert.False(knopf.Instance.AssistentSichtbar);
    }

    /// <summary>
    /// Der ZÄHLER „Heute genutzt" steht in der Fußleiste und nennt die Zahlen, die die
    /// Hülle hereingibt.
    /// </summary>
    [Fact]
    public void Der_Zaehler_steht_in_der_Fussleiste()
    {
        var cut = Render<KiChatDialog>(p => p
            .Add(x => x.Texte, Texte())
            .Add(x => x.Kontext, "Detaillierte Simulation")
            .Add(x => x.Eingerichtet, true)
            .Add(x => x.AnfragenHeute, 3)
            .Add(x => x.Tageslimit, 50)
            .Add(x => x.Aktionen, Array.Empty<KiAktion>()));

        Assert.Contains("Heute genutzt: 3 von 50",
                        cut.Find("span.epos-kichat-status").TextContent, StringComparison.Ordinal);
    }
}
