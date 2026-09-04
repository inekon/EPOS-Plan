using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Hilfe;
using EPOS.UI.Dienste;
using KiKern;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Hilfe;

/// <summary>
/// <see cref="KiChatDialog"/> — Zeuge T-2 (iU9-W15b.7).
///
/// <para><b>Was hier bewiesen wird.</b> Der Solitär des Bestands hatte 1 704
/// Zeilen und keinen einzigen Test — keine der sechs Masken der Welle wurde von
/// einem Zeugen genannt (§ 13.1 der Vermessung). Geprüft werden die drei
/// Betriebszustände, Enter/Umschalt+Enter, die Sperre, der Bestätigungsblock mit
/// seinen Ausgängen, die Werkzeugliste samt Kulturregel und die Zusage, dass die
/// Komponente selbst NICHTS sendet.</para>
///
/// <para><b>Kein Netz.</b> Die Komponente kennt weder <c>KiChatService</c> noch
/// <c>KiAusfuehrer</c>; sie bekommt Delegaten. Was hier läuft, sind Prüflinge —
/// dieselbe Bauart wie der <c>Modellkanal</c> im Kern.</para>
///
/// <para>Die Klasse pinnt die Sprache selbst (Regel seit W8).</para>
/// </summary>
public class KiChatDialogTests : BunitContext
{
    public KiChatDialogTests()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;

        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static KiChatTexte Texte() => new()
    {
        Verlauf = "Hilfe-Assistent",
        KontextFormat = "Kontext: {0}",
        KontextLeer = "Kontext: (nicht erkannt)",
        Denkt = "Der Assistent denkt nach...",
        VerbrauchFormat = "Heute genutzt: {0} von {1}",
        Eingabe = "Ihre Frage",
        Fragen = "Fragen",
        Suchen = "Nur suchen",
        Aktionen = "Aktionen zulassen",
        AktionenEin = "Aktionen sind jetzt zugelassen.",
        AktionenAus = "Aktionen sind abgeschaltet.",
        EinwilligungFehlt = "Ohne Einwilligung wird nichts übertragen.",
        Werkzeuge = "Werkzeuge...",
        WerkzeugeTitel = "Aktionen von Hand",
        Beschreibung = "Beschreibung",
        Ausfuehren = "Ausführen",
        AktionWaehlen = "Bitte zuerst eine Aktion wählen.",
        HinweisVorn = "Es werden nur Hilfetexte übertragen. ",
        HinweisLink = "Rechtshinweis",
        RechtshinweisTitel = "Rechtshinweis",
        Doku = "Online-Dokumentation öffnen",
        DokuAdresse = "https://wiki.epos-plan.de",
        Vorschau = "Was wird gesendet?",
        VorschauTitel = "Sendevorschau",
        VorschauKopf = "Gesendet wird an gemini-2.5-flash-lite.",
        Protokoll = "Protokoll anzeigen",
        ProtokollTitel = "Aktionsprotokoll",
        Einstellungen = "Einstellungen...",
        Gespeichert = "Die Angaben wurden gespeichert.",
        Schliessen = "Schließen",
        Kopieren = "Verlauf kopieren",
        BestaetigungTitel = "Bitte bestätigen",
        BestaetigungAusfuehren = "Ausführen",
        BestaetigungAbbrechen = "Abbrechen"
    };

    private static KiAktion Aktion() => new(
        name: "projekt_umbenennen",
        zweck: "Benennt ein Projekt um.",
        stufe: Schutzstufe.Lesen,
        andockpunkt: "ProjektCtrl.Rename",
        parameter: new[]
        {
            new KiParameter("schwelle_kw", KiParameterTyp.Zahl, "Zielschwelle.",
                            pflicht: false, anzeigename: "Schwelle")
        },
        ausfuehren: _ => KiErgebnis.Ok("ok"));

    /// <summary>
    /// Zeichnet den Chat. bunit laesst einen Parameter nur EINMAL setzen — Texte,
    /// Kontext und Betriebszustand kommen deshalb als eigene Argumente herein, der
    /// Rest ueber den Bauhelfer.
    /// </summary>
    private IRenderedComponent<KiChatDialog> Zeigen(
        Action<ComponentParameterCollectionBuilder<KiChatDialog>>? mehr = null,
        KiChatTexte? texte = null,
        string kontext = "Bereich: Projektverwaltung",
        bool hilfeBetrieb = false)
    {
        return Render<KiChatDialog>(p =>
        {
            p.Add(x => x.Texte, texte ?? Texte())
             .Add(x => x.Kontext, kontext)
             .Add(x => x.HilfeBetrieb, hilfeBetrieb)
             .Add(x => x.Eingerichtet, true)
             .Add(x => x.AnfragenHeute, 3)
             .Add(x => x.Tageslimit, 50)
             .Add(x => x.Aktionen, new[] { Aktion() })
             .Add(x => x.Beschreiben, a => "Beschreibung von " + a.Name);
            mehr?.Invoke(p);
        });
    }

    // ==================================================================
    //  Die drei Betriebszustaende
    // ==================================================================

    /// <summary>
    /// <b>Regelbetrieb:</b> „Fragen" und „Nur suchen", Aktionsschalter, Werkzeuge,
    /// Vorschau, Protokoll, Einstellungen und der Verweis auf den Rechtshinweis.
    /// </summary>
    [Fact]
    public void Regelbetrieb_zeigt_alles()
    {
        var cut = Zeigen();
        string text = cut.Markup;

        Assert.Contains("Fragen", text, StringComparison.Ordinal);
        Assert.Contains("Nur suchen", text, StringComparison.Ordinal);
        Assert.Contains("Aktionen zulassen", text, StringComparison.Ordinal);
        Assert.Contains("Werkzeuge...", text, StringComparison.Ordinal);
        Assert.Contains("Was wird gesendet?", text, StringComparison.Ordinal);
        Assert.Contains("Protokoll anzeigen", text, StringComparison.Ordinal);
        Assert.Contains("Einstellungen...", text, StringComparison.Ordinal);
        Assert.Contains("Rechtshinweis", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Hilfe-Betrieb</b> (KI abgeschaltet, Fachkonzept 11.9): Der Chat geht auf
    /// und arbeitet als reine Hilfesuche. Kein „Fragen", kein Aktionsschalter, keine
    /// Werkzeuge, keine Vorschau, kein Protokoll, keine Einstellungen — und der
    /// Verweis auf den Rechtshinweis fällt weg, weil es nichts einzuwilligen gibt.
    /// </summary>
    [Fact]
    public void Hilfebetrieb_zeigt_nur_die_Suche()
    {
        var cut = Zeigen(hilfeBetrieb: true, texte: new KiChatTexte
        {
            Suchen = "Suchen",
            HinweisVorn = "Die Hilfe wird lokal durchsucht.",
            Schliessen = "Schließen",
            Doku = "Online-Dokumentation öffnen"
        });

        string text = cut.Markup;

        Assert.Contains("Suchen", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Aktionen zulassen", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Werkzeuge...", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Was wird gesendet?", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Protokoll anzeigen", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Einstellungen...", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Rechtshinweis", text, StringComparison.Ordinal);
    }

    /// <summary>Die Begrüßung kommt fertig aus dem Kern und steht im Verlauf.</summary>
    [Fact]
    public void Die_Begruessung_steht_im_Verlauf()
    {
        var cut = Zeigen(p => p.Add(x => x.Begruessung, new[]
        {
            new Gespraechszeile(Gespraechsrolle.AssistentKopf, "Hilfe-Assistent"),
            new Gespraechszeile(Gespraechsrolle.Warnung, "Es ist noch kein API-Schlüssel hinterlegt.")
        }));

        Assert.Contains("Es ist noch kein API-Schlüssel hinterlegt.",
                        cut.Find("div.epos-verlauf").TextContent, StringComparison.Ordinal);
    }

    /// <summary>Der Kontext steht oben; ohne Kontext der Ersatztext.</summary>
    [Theory]
    [InlineData("Bereich: Projektverwaltung", "Kontext: Bereich: Projektverwaltung")]
    [InlineData("", "Kontext: (nicht erkannt)")]
    public void Die_Kontextzeile_steht_oben(string kontext, string erwartet)
    {
        var cut = Zeigen(kontext: kontext);

        Assert.Equal(erwartet, cut.Find("p.epos-kichat-kontext").TextContent);
    }

    // ==================================================================
    //  Fragen, suchen und die Sperre
    // ==================================================================

    /// <summary>„Fragen" ruft den Modellweg und hängt dessen Zeilen an.</summary>
    [Fact]
    public void Fragen_ruft_den_Modellweg()
    {
        string? gefragt = null;

        var cut = Zeigen(p => p.Add(x => x.Fragen,
            (Func<string, bool, Task<IReadOnlyList<Gespraechszeile>>>)((f, _) =>
            {
                gefragt = f;
                return Task.FromResult<IReadOnlyList<Gespraechszeile>>(new[]
                {
                    new Gespraechszeile(Gespraechsrolle.Assistent, "Die Antwort.")
                });
            })));

        cut.Find("textarea.epos-kieingabe-feld").Input("Wie lege ich ein Projekt an?");
        cut.FindAll("button.epos-knopf").First(b => b.TextContent.Trim() == "Fragen").Click();

        Assert.Equal("Wie lege ich ein Projekt an?", gefragt);
        Assert.Contains("Die Antwort.", cut.Find("div.epos-verlauf").TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    /// „Nur suchen" ruft den SUCHweg — <b>ohne jeden Modellaufruf</b> (Entscheid
    /// 7.4). Das ist der Weg, der auch ohne Schlüssel und ohne Kontingent geht.
    /// </summary>
    [Fact]
    public void Nur_suchen_ruft_niemals_den_Modellweg()
    {
        int modell = 0, suche = 0;

        var cut = Zeigen(p => p
            .Add(x => x.Fragen, (Func<string, bool, Task<IReadOnlyList<Gespraechszeile>>>)((_, _) =>
            {
                modell++;
                return Task.FromResult<IReadOnlyList<Gespraechszeile>>(Array.Empty<Gespraechszeile>());
            }))
            .Add(x => x.Suchen, (Func<string, Task<IReadOnlyList<Gespraechszeile>>>)(_ =>
            {
                suche++;
                return Task.FromResult<IReadOnlyList<Gespraechszeile>>(Array.Empty<Gespraechszeile>());
            })));

        cut.Find("textarea.epos-kieingabe-feld").Input("Stichwort");
        cut.FindAll("button.epos-knopf").First(b => b.TextContent.Trim() == "Nur suchen").Click();

        Assert.Equal(0, modell);
        Assert.Equal(1, suche);
    }

    /// <summary>
    /// <b>Enter sendet, Umschalt+Enter macht eine neue Zeile</b> (Bestand
    /// <c>:954-966</c>) — die Bedienregel des Chatfensters.
    /// </summary>
    [Fact]
    public void Enter_sendet_und_Umschalt_Enter_nicht()
    {
        int gefragt = 0;

        var cut = Zeigen(p => p.Add(x => x.Fragen,
            (Func<string, bool, Task<IReadOnlyList<Gespraechszeile>>>)((_, _) =>
            {
                gefragt++;
                return Task.FromResult<IReadOnlyList<Gespraechszeile>>(Array.Empty<Gespraechszeile>());
            })));

        var feld = cut.Find("textarea.epos-kieingabe-feld");

        feld.Input("Erste Frage");
        feld.KeyDown(new KeyboardEventArgs { Key = "Enter", ShiftKey = true });
        Assert.Equal(0, gefragt);

        feld.KeyDown(new KeyboardEventArgs { Key = "Enter", ShiftKey = false });
        Assert.Equal(1, gefragt);
    }

    /// <summary>Eine leere Frage geht gar nicht erst hinaus.</summary>
    [Fact]
    public void Eine_leere_Frage_geht_nicht_hinaus()
    {
        int gefragt = 0;

        var cut = Zeigen(p => p.Add(x => x.Fragen,
            (Func<string, bool, Task<IReadOnlyList<Gespraechszeile>>>)((_, _) =>
            {
                gefragt++;
                return Task.FromResult<IReadOnlyList<Gespraechszeile>>(Array.Empty<Gespraechszeile>());
            })));

        cut.Find("textarea.epos-kieingabe-feld").Input("   ");
        cut.FindAll("button.epos-knopf").First(b => b.TextContent.Trim() == "Fragen").Click();

        Assert.Equal(0, gefragt);
    }

    /// <summary>
    /// Läuft eine Assistentenaktion, sind Feld und Knöpfe gesperrt — der Bestand
    /// fragte das alle 400 ms über <c>KiAusfuehrer.Belegt</c> ab.
    /// </summary>
    [Fact]
    public void Eine_laufende_Aktion_sperrt_die_Eingabe()
    {
        var cut = Zeigen(p => p.Add(x => x.Belegt, (Func<bool>)(() => true)));

        Assert.True(cut.Find("textarea.epos-kieingabe-feld").HasAttribute("disabled"));
    }

    // ==================================================================
    //  Der Aktionsschalter und die Einwilligung
    // ==================================================================

    /// <summary>
    /// <b>Ohne Einwilligung bleibt der Schalter aus.</b> Der Riegel selbst liegt im
    /// Kern; die Komponente fragt ihn über den Delegaten und dreht den Schalter
    /// zurück, wenn die Antwort nein lautet.
    /// </summary>
    [Fact]
    public void Ohne_Einwilligung_bleibt_der_Aktionsschalter_aus()
    {
        var cut = Zeigen(p => p.Add(x => x.Einwilligen,
            (Func<Task<bool>>)(() => Task.FromResult(false))));

        cut.Find("input[type=checkbox]").Change(true);

        Assert.False(cut.Find("input[type=checkbox]").HasAttribute("checked"));
        Assert.Contains("Ohne Einwilligung wird nichts übertragen.",
                        cut.Find("div.epos-verlauf").TextContent, StringComparison.Ordinal);
    }

    /// <summary>Mit Einwilligung geht der Schalter an und sagt es im Verlauf.</summary>
    [Fact]
    public void Mit_Einwilligung_geht_der_Aktionsschalter_an()
    {
        var cut = Zeigen(p => p.Add(x => x.Einwilligen,
            (Func<Task<bool>>)(() => Task.FromResult(true))));

        cut.Find("input[type=checkbox]").Change(true);

        Assert.True(cut.Find("input[type=checkbox]").HasAttribute("checked"));
        Assert.Contains("Aktionen sind jetzt zugelassen.",
                        cut.Find("div.epos-verlauf").TextContent, StringComparison.Ordinal);
    }

    // ==================================================================
    //  Der Bestaetigungsblock
    // ==================================================================

    /// <summary>
    /// <b>Der Block steht im Verlauf, nicht über ihm</b> (Entscheid E-3) — und sein
    /// Text steht ZUSÄTZLICH als Zeile darin, damit er nachlesbar bleibt, wenn der
    /// Block verschwindet.
    /// </summary>
    [Fact]
    public void Der_Bestaetigungsblock_steht_im_Verlauf()
    {
        KiChatSteuerung? steuerung = null;
        var cut = Zeigen(p => p.Add(x => x.Anmelden, (Action<KiChatSteuerung>)(s => steuerung = s)));

        Assert.NotNull(steuerung);
        Task<bool> antwort = steuerung!.Zeigen("Ich würde Projekt 42 umbenennen.", "60 s");
        cut.Render();

        var block = cut.Find("div.epos-kibest");
        Assert.Contains("Ich würde Projekt 42 umbenennen.", block.TextContent, StringComparison.Ordinal);
        // Er liegt im FUSSBEREICH des Verlaufs, und der liegt im Bildlaufbereich (E-3).
        Assert.Contains(cut.Find("div.epos-verlauf").Children,
                        k => (k.ClassName ?? "").Contains("epos-verlauf-fuss"));
        Assert.Contains(cut.Find("div.epos-verlauf-fuss").Children,
                        k => (k.ClassName ?? "").Contains("epos-kibest"));
        // ... und zusaetzlich als Zeile im Verlauf.
        Assert.Contains("Ich würde Projekt 42 umbenennen.",
                        cut.Find("div.epos-verlauf").TextContent, StringComparison.Ordinal);

        steuerung.Beenden(false);
        Assert.False(antwort.Result);
    }

    /// <summary>„Ausführen" liefert ja, „Abbrechen" nein.</summary>
    [Theory]
    [InlineData("Ausführen", true)]
    [InlineData("Abbrechen", false)]
    public void Der_Block_liefert_die_Entscheidung(string knopf, bool erwartet)
    {
        KiChatSteuerung? steuerung = null;
        var cut = Zeigen(p => p.Add(x => x.Anmelden, (Action<KiChatSteuerung>)(s => steuerung = s)));

        Task<bool> antwort = steuerung!.Zeigen("Vorschau", "60 s");
        cut.Render();

        cut.FindAll("div.epos-kibest button")
           .First(b => b.TextContent.Trim() == knopf).Click();

        Assert.Equal(erwartet, antwort.Result);
    }

    /// <summary>
    /// <b>Nur EINE Vorschau gleichzeitig</b> (Fachkonzept 3.5, Punkt 4): Es gibt
    /// keine Sammelbestätigung — eine zweite wird sofort abgelehnt.
    /// </summary>
    [Fact]
    public void Eine_zweite_Vorschau_wird_sofort_abgelehnt()
    {
        KiChatSteuerung? steuerung = null;
        var cut = Zeigen(p => p.Add(x => x.Anmelden, (Action<KiChatSteuerung>)(s => steuerung = s)));

        Task<bool> erste = steuerung!.Zeigen("Erste", "60 s");
        cut.Render();

        Assert.False(steuerung.Zeigen("Zweite", "60 s").Result);
        Assert.False(erste.IsCompleted);

        steuerung.Beenden(false);
    }

    /// <summary>
    /// Verfall und Abbruch kommen von AUSSEN — der Wirt zählt die Frist, nicht die
    /// Anzeige. Ein Fenster, dessen Uhr steht, dürfte sonst beliebig lange
    /// bestätigen.
    /// </summary>
    [Fact]
    public void Der_Wirt_kann_die_Vorschau_von_aussen_beenden()
    {
        KiChatSteuerung? steuerung = null;
        var cut = Zeigen(p => p.Add(x => x.Anmelden, (Action<KiChatSteuerung>)(s => steuerung = s)));

        Task<bool> antwort = steuerung!.Zeigen("Vorschau", "60 s");
        cut.Render();
        Assert.NotEmpty(cut.FindAll("div.epos-kibest"));

        steuerung.Beenden(false);
        cut.Render();

        Assert.False(antwort.Result);
        Assert.Empty(cut.FindAll("div.epos-kibest"));
    }

    // ==================================================================
    //  Die Werkzeugliste
    // ==================================================================

    /// <summary>
    /// <b>Die einzige MessageBox der Welle</b> ist jetzt ein Warnbanner, und der
    /// Bereich bleibt offen — genau wie der Wegwerf-Dialog, der nach der Meldung
    /// sein <c>DialogResult</c> auf <c>None</c> zurücksetzte.
    /// </summary>
    [Fact]
    public void Ohne_gewaehlte_Aktion_meldet_die_Werkzeugliste_und_bleibt_offen()
    {
        var cut = Zeigen();

        cut.FindAll("button.epos-knopf").First(b => b.TextContent.Trim() == "Werkzeuge...").Click();
        cut.Find("div.epos-kiwerkzeuge-fuss button.epos-knopf--primaer").Click();

        Assert.Contains("Bitte zuerst eine Aktion wählen.",
                        cut.Find("div.epos-warnbanner").TextContent, StringComparison.Ordinal);
        Assert.NotEmpty(cut.FindAll("div.epos-kiwerkzeuge"));
    }

    /// <summary>
    /// <b>Die Kulturregel</b> (Risiko R-W15b-6): „12,5" geht als „12.5" hinaus —
    /// invariant, wie die Aktion es parst.
    /// </summary>
    [Fact]
    public void Die_Werkzeugliste_haelt_die_Kulturregel()
    {
        IReadOnlyDictionary<string, object>? werte = null;

        var cut = Zeigen(p => p.Add(x => x.Ausfuehren,
            (Func<string, IReadOnlyDictionary<string, object>, Task<IReadOnlyList<Gespraechszeile>>>)
            ((_, w) =>
            {
                werte = w;
                return Task.FromResult<IReadOnlyList<Gespraechszeile>>(Array.Empty<Gespraechszeile>());
            })));

        cut.FindAll("button.epos-knopf").First(b => b.TextContent.Trim() == "Werkzeuge...").Click();
        cut.Find("button.epos-kiwerkzeuge-eintrag").Click();
        cut.Find("div.epos-kiwerkzeuge-felder input").Input("12,5");
        cut.Find("div.epos-kiwerkzeuge-fuss button.epos-knopf--primaer").Click();

        Assert.NotNull(werte);
        Assert.Equal("12.5", werte!["schwelle_kw"]);
    }

    /// <summary>
    /// <b>Erst schließen, dann ausführen</b> (Bestand <c>:1295-1296</c>): Der
    /// Ausführer weist Aktionen ab, solange eine Überlagerung offen ist. Die
    /// Komponente meldet den Zustand deshalb an den Wirt.
    /// </summary>
    [Fact]
    public void Die_Ueberlagerung_meldet_sich_beim_Wirt()
    {
        var meldungen = new List<bool>();

        var cut = Zeigen(p => p
            .Add(x => x.UeberlagerungGeaendert,
                 EventCallback.Factory.Create<bool>(this, o => meldungen.Add(o)))
            .Add(x => x.Ausfuehren,
                 (Func<string, IReadOnlyDictionary<string, object>, Task<IReadOnlyList<Gespraechszeile>>>)
                 ((_, _) => Task.FromResult<IReadOnlyList<Gespraechszeile>>(Array.Empty<Gespraechszeile>()))));

        cut.FindAll("button.epos-knopf").First(b => b.TextContent.Trim() == "Werkzeuge...").Click();
        cut.Find("button.epos-kiwerkzeuge-eintrag").Click();
        cut.Find("div.epos-kiwerkzeuge-fuss button.epos-knopf--primaer").Click();

        // Erst "offen", dann "zu" - und das Schliessen kommt VOR dem Ausfuehren.
        Assert.Equal(new[] { true, false }, meldungen);
        Assert.Empty(cut.FindAll("div.epos-kiwerkzeuge"));
    }

    // ==================================================================
    //  Die Nebenwege
    // ==================================================================

    /// <summary>Protokoll und Vorschau erscheinen als Überlagerung, nicht als Fenster.</summary>
    [Fact]
    public void Protokoll_erscheint_als_Ueberlagerung()
    {
        var cut = Zeigen(p => p.Add(x => x.Protokoll,
            (Func<Task<string>>)(() => Task.FromResult("2026-09-04 10:00 projekt_lesen OK"))));

        cut.FindAll("button.epos-kichat-link").First(b => b.TextContent.Trim() == "Protokoll anzeigen").Click();

        Assert.Contains("2026-09-04 10:00 projekt_lesen OK",
                        cut.Find("div.epos-textanzeige textarea").TextContent,
                        StringComparison.Ordinal);
    }

    /// <summary>
    /// „Online-Dokumentation öffnen" meldet die Adresse — die Komponente öffnet
    /// nichts selbst.
    /// </summary>
    [Fact]
    public void Die_Dokumentation_wird_gemeldet_nicht_geoeffnet()
    {
        string? adresse = null;

        var cut = Zeigen(p => p.Add(x => x.AdresseGewaehlt,
            EventCallback.Factory.Create<string>(this, a => adresse = a)));

        cut.FindAll("button.epos-kichat-link")
           .First(b => b.TextContent.Trim() == "Online-Dokumentation öffnen").Click();

        Assert.Equal("https://wiki.epos-plan.de", adresse);
    }

    /// <summary>„Schließen" meldet sich beim Wirt — die Komponente schließt nichts.</summary>
    [Fact]
    public void Schliessen_meldet_sich_beim_Wirt()
    {
        int gemeldet = 0;

        var cut = Zeigen(p => p.Add(x => x.Geschlossen,
            EventCallback.Factory.Create(this, () => gemeldet++)));

        cut.FindAll("button.epos-knopf").First(b => b.TextContent.Trim() == "Schließen").Click();

        Assert.Equal(1, gemeldet);
    }
}
