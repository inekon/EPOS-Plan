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

    /// <summary>
    /// Der Tooltip der Semantikzeile, so wie ihn die Hülle baut: der Text von
    /// <c>KI_SEMANTIK_HERKUNFT</c> (wortgleich aus dem Vorläufer,
    /// <c>Form_KiChat:936-938</c>) mit den drei Angaben aus
    /// <c>SemantikModell</c>. Die drei Konstanten stehen im Kern und sind hier
    /// mitgeprüft — steht dort ein anderes Modell, fällt dieser Zeuge auf.
    /// </summary>
    private const string HERKUNFT =
        "Die semantische Suche arbeitet ausschließlich auf diesem Rechner. Modell: " +
        MODELL + " (" + LIZENZ + "), einmalig bezogen von " + QUELLE + ".";

    private const string MODELL = WindowsFormsApplication1.SemantikModell.NAME;
    private const string LIZENZ = WindowsFormsApplication1.SemantikModell.LIZENZ;
    private const string QUELLE = WindowsFormsApplication1.SemantikModell.QUELLE;

    private static KiChatTexte Texte() => new()
    {
        Verlauf = "Hilfe-Assistent",
        ErklaerungMehr = "Was der Assistent tut",
        VerlaufLeer = "Noch keine Frage gestellt.",
        EinstellungenTitel = "KI-Assistent - Einstellungen",
        KontextFormat = "Kontext: {0}",
        KontextLeer = "Kontext: (nicht erkannt)",
        Denkt = "Der Assistent denkt nach...",
        VerbrauchFormat = "Heute genutzt: {0} von {1}",
        SemantikHerkunft = HERKUNFT,
        Eingabe = "Ihre Frage",
        Fragen = "Fragen",
        Suchen = "Nur suchen",
        Aktionen = "Aktionen zulassen",
        AktionenEin = "Aktionen sind jetzt zugelassen.",
        AktionenAus = "Aktionen sind abgeschaltet.",
        EinwilligungFehlt = "Ohne Einwilligung wird nichts übertragen.",
        Werkzeuge = "Werkzeuge...",
        WerkzeugeTitel = "Aktionen von Hand",
        Werkzeugliste = Werkzeugtexte(),
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

    /// <summary>Die sechzehn Texte der Werkzeugliste (W15b-E-4).</summary>
    private static KiWerkzeugTexte Werkzeugtexte() => new()
    {
        Liste = "Aktionen von Hand",
        Hinweis = "Hier führen Sie eine Aktion von Hand aus.",
        Suche = "Aktion suchen",
        KeinTreffer = "Keine Aktion passt zu diesem Suchtext.",
        GruppeLesend = "Aktionen, die nur lesen",
        GruppeAendernd = "Aktionen, die Daten ändern",
        MerkmalLesend = "Liest nur",
        MerkmalAendernd = "Ändert Daten",
        Beispiel = "So fragen Sie:",
        Angaben = "Angaben",
        KeineAngaben = "Diese Aktion braucht keine Angaben.",
        Wirkung = "Danach:",
        Pflicht = "Pflichtangabe",
        LeerKopf = "Bitte links eine Aktion wählen.",
        LeerText = "Ein Beispiel: Wählen Sie links Variante anlegen.",
        Bestaetigungspflicht = "Verändernde Aktionen laufen erst nach Ihrer Bestätigung.",
        AndockpunktFormat = "Technischer Andockpunkt: {0}"
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
        bool hilfeBetrieb = false,
        IReadOnlyList<KiAktion>? aktionen = null)
    {
        return Render<KiChatDialog>(p =>
        {
            p.Add(x => x.Texte, texte ?? Texte())
             .Add(x => x.Kontext, kontext)
             .Add(x => x.HilfeBetrieb, hilfeBetrieb)
             .Add(x => x.Eingerichtet, true)
             .Add(x => x.AnfragenHeute, 3)
             .Add(x => x.Tageslimit, 50)
             .Add(x => x.Aktionen, aktionen ?? new[] { Aktion() });
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

    /// <summary>
    /// <b>Die Begrüßung steht im KOPF, nicht im Verlauf</b> (Anwenderbefund
    /// <b>W15b‑E‑3</b> der Windows-Abnahme vom 05.09.2026). Der Kopfkasten trug vier
    /// Zeilen Erklärung, und der Gesprächsverlauf darunter war „nicht zu sehen (leer,
    /// ohne Rahmen)". Sichtbar bleibt der ERSTE Satz; der Verlauf beginnt leer.
    /// </summary>
    [Fact]
    public void Die_Begruessung_steht_im_Kopf_und_nicht_im_Verlauf()
    {
        var cut = Zeigen(p => p.Add(x => x.Begruessung, new[]
        {
            new Gespraechszeile(Gespraechsrolle.AssistentKopf, "Hilfe-Assistent"),
            new Gespraechszeile(Gespraechsrolle.Warnung, "Es ist noch kein API-Schlüssel hinterlegt.")
        }));

        Assert.Contains("Es ist noch kein API-Schlüssel hinterlegt.",
                        cut.Find("p.epos-kichat-einleitung").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Es ist noch kein API-Schlüssel hinterlegt.",
                              cut.Find("div.epos-verlauf").TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    /// Die WEITEREN Erklärzeilen wandern hinter die Klappe — genau das, was der
    /// Anwender an den vier Zeilen des Kopfkastens beanstandet hat.
    /// </summary>
    [Fact]
    public void Die_lange_Erklaerung_steht_hinter_der_Klappe()
    {
        var cut = Zeigen(p => p.Add(x => x.Begruessung, new[]
        {
            new Gespraechszeile(Gespraechsrolle.AssistentKopf, "Hilfe-Assistent"),
            new Gespraechszeile(Gespraechsrolle.Assistent, "Stellen Sie Ihre Frage."),
            new Gespraechszeile(Gespraechsrolle.Leise, "Es werden nur Hilfetexte übertragen."),
            new Gespraechszeile(Gespraechsrolle.Leise, "Lesende Aktionen laufen sofort.")
        }));

        Assert.Equal("Stellen Sie Ihre Frage.", cut.Find("p.epos-kichat-einleitung").TextContent);

        var klappe = cut.Find("details.epos-kichat-erklaerung");
        Assert.Contains("Es werden nur Hilfetexte übertragen.", klappe.TextContent,
                        StringComparison.Ordinal);
        Assert.Contains("Lesende Aktionen laufen sofort.", klappe.TextContent,
                        StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Der Verlauf ist sichtbar und sagt, dass er leer ist</b> (W15b‑E‑3). Ohne
    /// diese Zeile stand dort eine Fläche, die der Anwender nicht als Liste erkannte.
    /// </summary>
    [Fact]
    public void Der_leere_Verlauf_sagt_dass_noch_keine_Frage_gestellt_wurde()
    {
        var cut = Zeigen();

        Assert.Contains("Noch keine Frage gestellt.",
                        cut.Find("div.epos-verlauf").TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Der Zähler steht GENAU EINMAL</b> (W15b‑E‑3). Auf dem Bildschirmfoto stand
    /// „Heute genutzt: 0 von 50" zweimal — in der Begrüßung und in der Fußleiste. Die
    /// Begrüßung kommt seither ohne ihn (<c>KiVerlaufstexte.Begruessung
    /// mitZaehler: false</c>); geprüft wird hier die ANSICHT: Was die Hülle nicht mehr
    /// hineingibt, darf die Komponente auch nicht selbst erzeugen.
    /// </summary>
    [Fact]
    public void Der_Tagesverbrauch_steht_genau_einmal()
    {
        var cut = Zeigen(p => p.Add(x => x.Begruessung, new[]
        {
            new Gespraechszeile(Gespraechsrolle.AssistentKopf, "Hilfe-Assistent"),
            new Gespraechszeile(Gespraechsrolle.Assistent, "Stellen Sie Ihre Frage.")
        }));

        string ganz = cut.Find("div.epos-kichat").TextContent;
        int erste = ganz.IndexOf("Heute genutzt:", StringComparison.Ordinal);

        Assert.True(erste >= 0, "Der Zähler fehlt ganz.");
        Assert.Equal(-1, ganz.IndexOf("Heute genutzt:", erste + 1, StringComparison.Ordinal));
    }

    /// <summary>
    /// <b>Die drei Knöpfe stehen in EINER Reihe</b> (W15b‑E‑3): Fragen, Nur suchen und
    /// „Werkzeuge…" liegen im selben Elternelement, der Aktionsschalter daneben. Auf
    /// dem Bildschirmfoto standen sie rechts gestapelt.
    /// </summary>
    [Fact]
    public void Fragen_Suchen_und_Werkzeuge_stehen_in_einer_Reihe()
    {
        var cut = Zeigen();

        var reihe = cut.Find("div.epos-kieingabe-knoepfe");
        string[] beschriftungen = reihe.QuerySelectorAll("button")
                                       .Select(b => b.TextContent.Trim())
                                       .ToArray();

        Assert.Contains("Fragen", beschriftungen);
        Assert.Contains("Nur suchen", beschriftungen);
        Assert.Contains("Werkzeuge...", beschriftungen);
        // Der Aktionsschalter steht im selben Kasten, links vor den Knoepfen.
        Assert.NotNull(reihe.QuerySelector("span.epos-kieingabe-vorspann input[type=checkbox]"));
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
    public async Task Der_Bestaetigungsblock_steht_im_Verlauf()
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

        await steuerung.Beenden(false);
        Assert.False(await antwort);
    }

    /// <summary>„Ausführen" liefert ja, „Abbrechen" nein.</summary>
    [Theory]
    [InlineData("Ausführen", true)]
    [InlineData("Abbrechen", false)]
    public async Task Der_Block_liefert_die_Entscheidung(string knopf, bool erwartet)
    {
        KiChatSteuerung? steuerung = null;
        var cut = Zeigen(p => p.Add(x => x.Anmelden, (Action<KiChatSteuerung>)(s => steuerung = s)));

        Task<bool> antwort = steuerung!.Zeigen("Vorschau", "60 s");
        cut.Render();

        cut.FindAll("div.epos-kibest button")
           .First(b => b.TextContent.Trim() == knopf).Click();

        Assert.Equal(erwartet, await antwort);
    }

    /// <summary>
    /// <b>Nur EINE Vorschau gleichzeitig</b> (Fachkonzept 3.5, Punkt 4): Es gibt
    /// keine Sammelbestätigung — eine zweite wird sofort abgelehnt.
    /// </summary>
    [Fact]
    public async Task Eine_zweite_Vorschau_wird_sofort_abgelehnt()
    {
        KiChatSteuerung? steuerung = null;
        var cut = Zeigen(p => p.Add(x => x.Anmelden, (Action<KiChatSteuerung>)(s => steuerung = s)));

        Task<bool> erste = steuerung!.Zeigen("Erste", "60 s");
        cut.Render();

        Assert.False(await steuerung.Zeigen("Zweite", "60 s"));
        Assert.False(erste.IsCompleted);

        await steuerung.Beenden(false);
        await erste;
    }

    /// <summary>
    /// Verfall und Abbruch kommen von AUSSEN — der Wirt zählt die Frist, nicht die
    /// Anzeige. Ein Fenster, dessen Uhr steht, dürfte sonst beliebig lange
    /// bestätigen.
    /// </summary>
    [Fact]
    public async Task Der_Wirt_kann_die_Vorschau_von_aussen_beenden()
    {
        KiChatSteuerung? steuerung = null;
        var cut = Zeigen(p => p.Add(x => x.Anmelden, (Action<KiChatSteuerung>)(s => steuerung = s)));

        Task<bool> antwort = steuerung!.Zeigen("Vorschau", "60 s");
        cut.Render();
        Assert.NotEmpty(cut.FindAll("div.epos-kibest"));

        await steuerung.Beenden(false);
        cut.Render();

        Assert.False(await antwort);
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

    // ==================================================================
    //  Die Semantikzeile und ihr Tooltip (Entscheid W15b-O-2, 04.09.2026)
    // ==================================================================

    /// <summary>
    /// <b>Mit Semantikzustand:</b> Die Statuszeile trägt ein <c>title</c>-Merkmal
    /// mit Modellname, Lizenz und Herkunft — der Tooltip, der im Bestand am
    /// Statuslabel hing (<c>Form_KiChat:935-938</c>) und mit dem Label entfiel
    /// (Anpassung A‑10).
    /// </summary>
    [Fact]
    public void Die_Semantikzeile_traegt_Modell_Lizenz_und_Herkunft_als_Tooltip()
    {
        var cut = Zeigen(p => p.Add(x => x.SemantikZeile,
            (Func<string>)(() => "Semantische Suche aktiv")));

        var zeile = cut.Find("span.epos-kichat-status");
        string tipp = zeile.GetAttribute("title") ?? "";

        Assert.Contains("Semantische Suche aktiv", zeile.TextContent, StringComparison.Ordinal);
        Assert.Contains(MODELL, tipp, StringComparison.Ordinal);
        Assert.Contains(LIZENZ, tipp, StringComparison.Ordinal);
        Assert.Contains(QUELLE, tipp, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Ohne Semantikzustand:</b> gar kein <c>title</c>. Der Tagesverbrauch steht
    /// zwar in derselben Zeile, aber über ihn sagt der Tooltip nichts — und ein
    /// leerer Kasten unter dem Mauszeiger wäre schlimmer als keiner.
    /// </summary>
    [Fact]
    public void Ohne_Semantikzustand_gibt_es_keinen_Tooltip()
    {
        var cut = Zeigen();

        var zeile = cut.Find("span.epos-kichat-status");

        Assert.Contains("Heute genutzt: 3 von 50", zeile.TextContent, StringComparison.Ordinal);
        Assert.False(zeile.HasAttribute("title"));
    }

    // ==================================================================
    //  Die Werkzeugliste in Anwendersprache (Befund W15b-E-4)
    // ==================================================================

    /// <summary>Eine ändernde Aktion mit zwei Pflichtfeldern — der Fall des Befunds.</summary>
    private static KiAktion Schreibaktion() => new(
        name: "variante_anlegen",
        zweck: "Legt zu einem Stammprojekt eine neue Variante an.",
        stufe: Schutzstufe.Schreiben,
        andockpunkt: "VariantenCtrl.AnlegenAusStamm",
        parameter: new[]
        {
            new KiParameter("stammprojekt", KiParameterTyp.Text, "Name des Stammprojekts.",
                            pflicht: true, anzeigename: "Stammprojekt"),
            new KiParameter("bezeichner", KiParameterTyp.Text, "Name der neuen Variante.",
                            pflicht: true, anzeigename: "Bezeichner", maxLaenge: 200)
        },
        ausfuehren: _ => KiErgebnis.Ok("ok"),
        vorschau: _ => "Ich würde eine Variante anlegen.",
        wirkung: "Danach gibt es ein zusätzliches Projekt in der Vergleichsgruppe.",
        titel: "Variante anlegen",
        beispiel: "Lege zum Projekt Musterhaus eine Variante Wärmepumpe statt Kessel an.");

    private IRenderedComponent<KiChatDialog> MitWerkzeugliste(params KiAktion[] aktionen)
    {
        var cut = Zeigen(aktionen: aktionen);
        cut.FindAll("button.epos-knopf").First(b => b.TextContent.Trim() == "Werkzeuge...").Click();
        return cut;
    }

    /// <summary>
    /// <b>Der Befund im Kern:</b> Links standen rohe Bezeichner
    /// (<c>speichervariante_aktiv_setzen</c>). Jetzt steht dort der Titel, und der
    /// Bezeichner steht NICHT mehr im sichtbaren Text.
    /// </summary>
    [Fact]
    public void Die_Werkzeugliste_zeigt_Titel_statt_Bezeichner()
    {
        var cut = MitWerkzeugliste(Schreibaktion());

        var eintrag = cut.Find("button.epos-kiwerkzeuge-eintrag");
        Assert.Equal("Variante anlegen", eintrag.TextContent.Trim());
        Assert.DoesNotContain("variante_anlegen",
                              cut.Find("div.epos-kiwerkzeuge").TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Der Andockpunkt verschwindet aus der Anwendersicht</b> und bleibt nur als
    /// Kurztext stehen — er gehört ins Protokoll, nicht in die Maske.
    /// </summary>
    [Fact]
    public void Der_Andockpunkt_steht_nur_noch_im_Kurztext()
    {
        var cut = MitWerkzeugliste(Schreibaktion());
        cut.Find("button.epos-kiwerkzeuge-eintrag").Click();

        Assert.DoesNotContain("VariantenCtrl.AnlegenAusStamm",
                              cut.Find("div.epos-kiwerkzeuge").TextContent, StringComparison.Ordinal);
        Assert.Contains("VariantenCtrl.AnlegenAusStamm",
                        cut.Find("h3.epos-kiwerkzeuge-titel").GetAttribute("title") ?? "",
                        StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Lesend oder ändernd</b> — das Kennzeichen sagt es in zwei Wörtern, und die
    /// Liste gruppiert danach. Eine ändernde Aktion nennt außerdem ihre Wirkung und
    /// die Bestätigungspflicht.
    /// </summary>
    [Fact]
    public void Die_Werkzeugliste_kennzeichnet_lesend_und_aendernd()
    {
        var cut = MitWerkzeugliste(Aktion(), Schreibaktion());

        string liste = cut.Find("ul.epos-kiwerkzeuge-liste").TextContent;
        Assert.Contains("Aktionen, die nur lesen", liste, StringComparison.Ordinal);
        Assert.Contains("Aktionen, die Daten ändern", liste, StringComparison.Ordinal);

        cut.FindAll("button.epos-kiwerkzeuge-eintrag")
           .First(b => b.TextContent.Trim() == "Variante anlegen").Click();

        Assert.Equal("Ändert Daten", cut.Find("span.epos-kiwerkzeuge-merkmal").TextContent.Trim());
        Assert.Contains("Danach gibt es ein zusätzliches Projekt",
                        cut.Find("p.epos-kiwerkzeuge-wirkung").TextContent, StringComparison.Ordinal);
        Assert.Contains("Verändernde Aktionen laufen erst nach Ihrer Bestätigung.",
                        cut.Find("p.epos-kiwerkzeuge-bestaetigung").TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Mindestens ein Beispiel — direkt im Dialog.</b> Im Leerzustand steht der
    /// vollständige Fall, an einer gewählten Aktion ihr eigener Satz.
    /// </summary>
    [Fact]
    public void Die_Werkzeugliste_zeigt_ein_Beispiel_im_Leerzustand_und_je_Aktion()
    {
        var cut = MitWerkzeugliste(Schreibaktion());

        Assert.Contains("Ein Beispiel: Wählen Sie links Variante anlegen.",
                        cut.Find("div.epos-kiwerkzeuge-leer").TextContent, StringComparison.Ordinal);

        cut.Find("button.epos-kiwerkzeuge-eintrag").Click();

        Assert.Contains("Lege zum Projekt Musterhaus eine Variante Wärmepumpe statt Kessel an.",
                        cut.Find("p.epos-kiwerkzeuge-beispiel").TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Pflichtangaben sind gekennzeichnet</b> — Stern in der Beschriftung und der
    /// Grund als Kurztext; die Erläuterung steht als Platzhalter im Feld.
    /// </summary>
    [Fact]
    public void Pflichtangaben_tragen_ein_Kennzeichen_und_einen_Platzhalter()
    {
        var cut = MitWerkzeugliste(Schreibaktion());
        cut.Find("button.epos-kiwerkzeuge-eintrag").Click();

        var beschriftungen = cut.FindAll("div.epos-kiwerkzeuge-felder span.epos-feld-text");
        Assert.Equal("Stammprojekt *", beschriftungen[0].TextContent.Trim());
        Assert.Equal("Pflichtangabe", beschriftungen[0].GetAttribute("title"));

        var felder = cut.FindAll("div.epos-kiwerkzeuge-felder input");
        Assert.Equal("Name des Stammprojekts.", felder[0].GetAttribute("placeholder"));
    }

    /// <summary>
    /// Bis zu acht Einträgen gibt es kein Suchfeld — bei neun schon. Der Bestand
    /// führt 24 Aktionen.
    /// </summary>
    [Theory]
    [InlineData(8, false)]
    [InlineData(9, true)]
    public void Ab_neun_Aktionen_bekommt_die_Liste_ein_Suchfeld(int anzahl, bool mitSuche)
    {
        KiAktion[] viele = Enumerable.Range(1, anzahl)
            .Select(i => new KiAktion(
                name: "aktion_" + i,
                zweck: "Zweck " + i,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "Ctrl.Weg",
                titel: "Aktion " + i))
            .ToArray();

        var cut = MitWerkzeugliste(viele);

        Assert.Equal(mitSuche, cut.FindAll("label.epos-kiwerkzeuge-suche").Count > 0);
    }

    /// <summary>Das Suchfeld filtert über Titel, Zweck und Beispiel.</summary>
    [Fact]
    public void Das_Suchfeld_filtert_die_Liste()
    {
        KiAktion[] viele = Enumerable.Range(1, 8)
            .Select(i => new KiAktion(
                name: "aktion_" + i, zweck: "Zweck " + i, stufe: Schutzstufe.Lesen,
                andockpunkt: "Ctrl.Weg", titel: "Aktion " + i))
            .Append(Schreibaktion())
            .ToArray();

        var cut = MitWerkzeugliste(viele);
        cut.Find("label.epos-kiwerkzeuge-suche input").Input("Variante");

        var eintraege = cut.FindAll("button.epos-kiwerkzeuge-eintrag");
        Assert.Single(eintraege);
        Assert.Equal("Variante anlegen", eintraege[0].TextContent.Trim());
    }

    /// <summary>Der Verlauf nennt nach dem Ausführen den TITEL, nicht den Bezeichner.</summary>
    [Fact]
    public void Der_Verlauf_nennt_den_Titel_der_ausgefuehrten_Aktion()
    {
        var cut = Zeigen(aktionen: new[] { Schreibaktion() }, mehr: p => p
            .Add(x => x.Ausfuehren,
                 (Func<string, IReadOnlyDictionary<string, object>, Task<IReadOnlyList<Gespraechszeile>>>)
                 ((_, _) => Task.FromResult<IReadOnlyList<Gespraechszeile>>(Array.Empty<Gespraechszeile>()))));

        cut.FindAll("button.epos-knopf").First(b => b.TextContent.Trim() == "Werkzeuge...").Click();
        cut.Find("button.epos-kiwerkzeuge-eintrag").Click();
        cut.Find("div.epos-kiwerkzeuge-fuss button.epos-knopf--primaer").Click();

        string verlauf = cut.Find("div.epos-verlauf").TextContent;
        Assert.Contains("Variante anlegen", verlauf, StringComparison.Ordinal);
        Assert.DoesNotContain("variante_anlegen", verlauf, StringComparison.Ordinal);
    }

    // ==================================================================
    //  Die Einstellungen als Ueberlagerung (Befund W15b-B-1)
    // ==================================================================

    /// <summary>
    /// <b>„Einstellungen…" öffnet KEIN zweites Fenster mehr.</b> Anwenderbefund
    /// <b>W15b‑B‑1</b> der Windows-Abnahme vom 05.09.2026: Der Knopf öffnete ein
    /// leeres Fenster, dann stürzte die Anwendung ab — die Hülle gab
    /// <c>Task.FromResult(KiEinstellungenHuelle.Oeffnen(…))</c> heraus und zog damit
    /// eine zweite WebView2 synchron im Rückruf der ersten hoch. Liefert die Hülle
    /// einen Inhalt, erscheint er als Überlagerung, und der Delegat bleibt
    /// UNGERUFEN.
    /// </summary>
    [Fact]
    public void Einstellungen_erscheinen_als_Ueberlagerung_und_rufen_den_Delegaten_nicht()
    {
        int delegatGerufen = 0;
        EventCallback<bool> fertig = default;

        RenderFragment<EventCallback<bool>> inhalt = rueckweg =>
        {
            fertig = rueckweg;
            return b =>
            {
                b.OpenElement(0, "p");
                b.AddAttribute(1, "class", "pruef-einstellungen");
                b.AddContent(2, "Einstellungen");
                b.CloseElement();
            };
        };

        var cut = Zeigen(p => p
            .Add(x => x.Einstellungen, (Func<Task<bool>>)(() =>
            {
                delegatGerufen++;
                return Task.FromResult(true);
            }))
            .Add(x => x.Einstellungsinhalt, inhalt));

        cut.FindAll("button.epos-knopf").First(b => b.TextContent.Trim() == "Einstellungen...").Click();

        Assert.Equal(0, delegatGerufen);
        Assert.NotEmpty(cut.FindAll("p.pruef-einstellungen"));
        Assert.Contains("KI-Assistent - Einstellungen",
                        cut.Find("div.epos-ueberlagerung").TextContent, StringComparison.Ordinal);

        // Der Rueckweg schliesst die Ueberlagerung und vermerkt das Speichern.
        cut.InvokeAsync(() => fertig.InvokeAsync(true));

        Assert.Empty(cut.FindAll("p.pruef-einstellungen"));
        Assert.Contains("Die Angaben wurden gespeichert.",
                        cut.Find("div.epos-verlauf").TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    /// Dasselbe für den Rechtshinweis: Er erscheint als Überlagerung, sein
    /// Delegat — das zweite Fenster — bleibt ungerufen.
    /// </summary>
    [Fact]
    public void Der_Rechtshinweis_erscheint_als_Ueberlagerung()
    {
        int delegatGerufen = 0;

        RenderFragment<EventCallback<bool>> inhalt = _ => b =>
        {
            b.OpenElement(0, "p");
            b.AddAttribute(1, "class", "pruef-hinweis");
            b.AddContent(2, "Rechtshinweis");
            b.CloseElement();
        };

        var cut = Zeigen(p => p
            .Add(x => x.Rechtshinweis, (Func<Task>)(() =>
            {
                delegatGerufen++;
                return Task.CompletedTask;
            }))
            .Add(x => x.Rechtshinweisinhalt, inhalt));

        cut.FindAll("button.epos-kichat-link").First(b => b.TextContent.Trim() == "Rechtshinweis").Click();

        Assert.Equal(0, delegatGerufen);
        Assert.NotEmpty(cut.FindAll("p.pruef-hinweis"));
    }

    /// <summary>
    /// <b>Ohne Inhalt bleibt der alte Weg</b> — eine Hülle, die keine Überlagerung
    /// liefern kann (iOS vor iU11), ruft weiterhin den Delegaten.
    /// </summary>
    [Fact]
    public void Ohne_Inhalt_gehen_die_Einstellungen_weiter_ueber_den_Delegaten()
    {
        int gerufen = 0;

        var cut = Zeigen(p => p.Add(x => x.Einstellungen, (Func<Task<bool>>)(() =>
        {
            gerufen++;
            return Task.FromResult(true);
        })));

        cut.FindAll("button.epos-knopf").First(b => b.TextContent.Trim() == "Einstellungen...").Click();

        Assert.Equal(1, gerufen);
        Assert.Contains("Die Angaben wurden gespeichert.",
                        cut.Find("div.epos-verlauf").TextContent, StringComparison.Ordinal);
    }

    // ==================================================================
    //  Enter sendet EINMAL (Befund W15b-B-2, zweiter Teil)
    // ==================================================================

    /// <summary>
    /// <b>Der doppelte Block.</b> Auf dem Bildschirmfoto zu <b>W15b‑B‑2</b> stand der
    /// ganze Block — Frage, Hinweis, Abschnitte, Quellen — ZWEIMAL untereinander im
    /// Verlauf. Die Ursache liegt in der Eingabezeile: <c>@@onkeydown:preventDefault</c>
    /// wird beim ZEICHNEN ausgewertet, nicht beim Ereignis. Beim sendenden Enter stand
    /// die Marke deshalb noch auf <c>false</c>, der Browser trug seinen Zeilenumbruch
    /// ein, und das nachfolgende <c>oninput</c> schrieb die Frage zurück ins Feld, das
    /// <c>Senden</c> gerade geleert hatte. Der Anwender sah eine unveränderte Eingabe
    /// und drückte noch einmal Enter.
    /// </summary>
    [Fact]
    public void Enter_sendet_einmal_und_das_Feld_bleibt_danach_leer()
    {
        var fragen = new List<string>();

        var cut = Zeigen(p => p.Add(x => x.Fragen,
            (Func<string, bool, Task<IReadOnlyList<Gespraechszeile>>>)((f, _) =>
            {
                fragen.Add(f);
                return Task.FromResult<IReadOnlyList<Gespraechszeile>>(
                    new[] { new Gespraechszeile(Gespraechsrolle.Assistent, "Antwort auf " + f) });
            })));

        var feld = cut.Find("textarea.epos-kieingabe-feld");
        feld.Input("wieviel varianten hat dieses projekt");
        feld.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        // Der Browser traegt seinen Zeilenumbruch nach: dasselbe Ereignis, das die
        // Frage bisher zurueckschrieb.
        cut.Find("textarea.epos-kieingabe-feld").Input("wieviel varianten hat dieses projekt\n");

        Assert.Equal("", cut.Find("textarea.epos-kieingabe-feld").GetAttribute("value"));

        // Ein zweites Enter auf dem nun leeren Feld sendet nichts.
        cut.Find("textarea.epos-kieingabe-feld").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Single(fragen);
    }

    /// <summary>Umschalt+Enter sendet nicht — es bleibt der Zeilenumbruch.</summary>
    [Fact]
    public void Umschalt_und_Enter_sendet_nicht()
    {
        var fragen = new List<string>();

        var cut = Zeigen(p => p.Add(x => x.Fragen,
            (Func<string, bool, Task<IReadOnlyList<Gespraechszeile>>>)((f, _) =>
            {
                fragen.Add(f);
                return Task.FromResult<IReadOnlyList<Gespraechszeile>>(Array.Empty<Gespraechszeile>());
            })));

        var feld = cut.Find("textarea.epos-kieingabe-feld");
        feld.Input("erste Zeile");
        feld.KeyDown(new KeyboardEventArgs { Key = "Enter", ShiftKey = true });

        Assert.Empty(fragen);
        Assert.Equal("erste Zeile", cut.Find("textarea.epos-kieingabe-feld").GetAttribute("value"));
    }
}
