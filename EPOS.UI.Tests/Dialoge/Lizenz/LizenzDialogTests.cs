using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Lizenz;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Lizenz;

/// <summary>
/// <see cref="LizenzDialog"/> — Zeuge der Welle iU9-W15c.11.
///
/// <para><b>Zwei Gesichter, eine Komponente</b>, genau wie im Vorläufer, dessen
/// Konstruktor einen <c>bool</c> nahm: der Menüpunkt „Hilfe → Lizenz" und die
/// EULA-Abfrage beim ersten Start. Der Unterschied sind vier Zeilen Text und zwei
/// Knöpfe — und genau das prüft dieser Zeuge.</para>
///
/// <para><b>Drei Zusagen tragen Gewicht.</b> (1) Die DREI Registerkarten stehen in
/// ihrer Reihenfolge, und verbindlich ist die erste. (2) Der Zustimmungsmodus zeigt
/// „Zustimmen"/„Ablehnen" statt „Schließen", und „Zustimmen" meldet BEIDES —
/// <c>Zugestimmt</c> und <c>Geschlossen(true)</c>. (3) Aus Text wird nie
/// Auszeichnung: Verweise entstehen als Elemente, und nur bei <c>https://</c>.</para>
///
/// <para>Die Klasse pinnt die Sprache selbst (Regel seit W8).</para>
/// </summary>
public class LizenzDialogTests : EposBunitContext
{
    public LizenzDialogTests()
    {
        Services.AddSingleton<EPOS.UI.Dienste.IHilfeDienst>(new EPOS.UI.Dienste.KeineHilfe());

        // window.print() ist der Druckweg (E-2); im Pruefstand gibt es keinen
        // Browser, der Aufruf soll aber nicht werfen.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static readonly List<RechtsAbschnitt> HINWEISE = new()
    {
        new RechtsAbschnitt(true, "Anbieter"),
        new RechtsAbschnitt(false, "INEKON - Intelligente Energiekonzepte. Kontakt: https://epos-plan.de/impressum/"),
        new RechtsAbschnitt(true, "Verbindliche Grundlage"),
        new RechtsAbschnitt(false, "Für die Nutzung gilt ausschließlich die Lizenzvereinbarung."),
    };

    private static readonly List<RechtsAbschnitt> KOMPONENTEN = new()
    {
        new RechtsAbschnitt(true, "Laufzeit und Bibliotheken"),
        new RechtsAbschnitt(false, "Microsoft .NET 10 mit Windows Forms."),
    };

    /// <summary>
    /// Der Textsatz des Prüfstands — seit W15c-O-2 EIN Parameter statt achtzehn.
    /// Die Werte stehen hier weiterhin wörtlich, damit jede Zusage dieses Zeugen an
    /// einem Literal hängt und nicht am Katalog; dass sich das Bündel OHNE Angabe
    /// selbst aus <c>MyResource</c> füllt, prüft <see cref="LizenzTexteTests"/>.
    /// </summary>
    private static LizenzTexte Texte(string sprachHinweis = "") => new()
    {
        KopfTitel = "Lizenz und rechtliche Hinweise",
        KopfUntertitel = "EPOS-Plan - Energieplanungs-Software - INEKON",
        ReiterStatus = "Status & Aktivierung",
        ReiterVertrag = "Lizenzvereinbarung",
        ReiterHinweise = "Rechtliche Hinweise",
        ReiterKomponenten = "Komponenten",
        KnopfDrucken = "Drucken...",
        KnopfSpeichern = "Speichern unter...",
        KnopfAktivieren = "Lizenz aktivieren...",
        KnopfSchliessen = "Schließen",
        KnopfZustimmen = "Zustimmen",
        KnopfAblehnen = "Ablehnen",
        ZustimmungHinweis = "Bitte lesen Sie die Vereinbarung und bestätigen Sie sie, um das Programm zu nutzen.",
        FussLizenz = "Lizenz: {0}",
        FussQuelle = "Quelle: {0}",
        FussStand = "   ·   Stand {0}",
        MsgGespeichert = "Gespeichert:\n{0}",
        SprachHinweis = sprachHinweis,
    };

    private IRenderedComponent<LizenzDialog> Zeigen(
        bool zustimmungsmodus = false,
        LizenzTextGaben? text = null,
        Func<Task<LizenzTextGaben?>>? onlineNachladen = null,
        Func<string, string, Task<string?>>? speichern = null,
        IReadOnlyDictionary<string, object>? verwaltung = null,
        EventCallback<bool>? geschlossen = null,
        EventCallback? zugestimmt = null,
        string sprachHinweis = "",
        string startReiter = "")
    {
        return Render<LizenzDialog>(p =>
        {
            p.Add(x => x.Zustimmungsmodus, zustimmungsmodus)
             .Add(x => x.StartReiter, startReiter)
             .Add(x => x.Text, text ?? new LizenzTextGaben("Der Vertragstext.", "https://epos-plan.de/agb/", "13.08.2026"))
             .Add(x => x.Lizenzstatus, "Firmenlizenz · gültig bis 31.12.2026")
             .Add(x => x.Hinweise, HINWEISE)
             .Add(x => x.Komponenten, KOMPONENTEN)
             .Add(x => x.Texte, Texte(sprachHinweis));

            if (onlineNachladen is not null) p.Add(x => x.OnlineNachladen, onlineNachladen);
            if (speichern is not null) p.Add(x => x.Speichern, speichern);
            if (verwaltung is not null) p.Add(x => x.VerwaltungGaben, verwaltung);
            if (geschlossen is not null) p.Add(x => x.Geschlossen, geschlossen.Value);
            if (zugestimmt is not null) p.Add(x => x.Zugestimmt, zugestimmt.Value);
        });
    }

    /// <summary>
    /// Der Parametersatz der Verwaltung, wie ihn die Hülle splattet (MN-1: er
    /// entscheidet über das Reiterblatt „Status &amp; Aktivierung").
    /// </summary>
    private static Dictionary<string, object> Verwaltungsgaben() => new()
    {
        ["Lage"] = new LizenzGaben("GUELTIG", "Firmenlizenz", "Lizenz EPOS-2026-00001", true, ""),
        ["Texte"] = new LizenzTexte
        {
            Verwaltung =
            {
                GruppeStatus = "Lizenzstatus auf diesem Arbeitsplatz",
                KnopfAktivieren = "Jetzt aktivieren",
            }
        },
    };

    private static IElement Knopf(IRenderedComponent<LizenzDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static bool GibtKnopf(IRenderedComponent<LizenzDialog> cut, string text)
        => cut.FindAll("button").Any(b => b.TextContent.Trim() == text);

    // ==================================================================
    //  Die drei Registerkarten
    // ==================================================================

    /// <summary>
    /// OHNE Parametersatz der Verwaltung sind es die drei LESEkarten — der
    /// Wellenplan sprach von vier, gemessen waren es drei (Befund W15c-B3).
    /// </summary>
    [Fact]
    public void Es_sind_genau_drei_Registerkarten()
    {
        var cut = Zeigen();
        var reiter = cut.FindAll("[role=tab]");

        Assert.Equal(3, reiter.Count);
        Assert.Equal("Lizenzvereinbarung", reiter[0].TextContent.Trim());
        Assert.Equal("Rechtliche Hinweise", reiter[1].TextContent.Trim());
        Assert.Equal("Komponenten", reiter[2].TextContent.Trim());
    }

    /// <summary>
    /// ANWENDERENTSCHEID MN-1 (19.09.2026): Mit dem Parametersatz der Verwaltung
    /// sind es VIER Karten, und „Status &amp; Aktivierung" steht als ERSTE da —
    /// sie hat den Knopf „Lizenz aktivieren…" samt seiner Überlagerung abgelöst.
    /// </summary>
    [Fact]
    public void Mit_der_Verwaltung_sind_es_vier_Registerkarten()
    {
        var cut = Zeigen(verwaltung: Verwaltungsgaben());
        var reiter = cut.FindAll("[role=tab]");

        Assert.Equal(4, reiter.Count);
        Assert.Equal("Status & Aktivierung", reiter[0].TextContent.Trim());
        Assert.Equal("Lizenzvereinbarung", reiter[1].TextContent.Trim());
        Assert.Equal("Rechtliche Hinweise", reiter[2].TextContent.Trim());
        Assert.Equal("Komponenten", reiter[3].TextContent.Trim());
    }

    /// <summary>Beim Öffnen steht die verbindliche erste Karte vorn.</summary>
    [Fact]
    public void Die_verbindliche_Karte_steht_vorn()
    {
        var cut = Zeigen();

        Assert.Contains("Der Vertragstext.", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Anbieter", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// MN-1: Auch MIT der Verwaltung bleibt die Lizenzvereinbarung vorn, solange
    /// die Hülle nichts anderes sagt — die verbindliche Karte ist die Vorgabe, und
    /// der Erststart (Zustimmungsmodus) hängt daran.
    /// </summary>
    [Fact]
    public void Ohne_Vorwahl_steht_die_Vereinbarung_auch_mit_der_Verwaltung_vorn()
    {
        var cut = Zeigen(verwaltung: Verwaltungsgaben());

        Assert.Contains("Der Vertragstext.", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindComponents<LizenzVerwaltungDialog>());
        Assert.Equal("true", cut.FindAll("[role=tab]")[1].GetAttribute("aria-selected"));
    }

    /// <summary>
    /// MN-1: Die Hülle wählt das Blatt vor — „STATUS", wenn die Lizenz nicht
    /// aktiviert ist. Ein unbekannter Schlüssel und ein „STATUS" OHNE
    /// Parametersatz ändern nichts: Dann stünde sonst je nach Gaben ein anderes
    /// Blatt vorn.
    /// </summary>
    [Fact]
    public void Die_Huelle_waehlt_das_Startblatt_vor()
    {
        var cut = Zeigen(verwaltung: Verwaltungsgaben(), startReiter: "STATUS");
        Assert.Single(cut.FindComponents<LizenzVerwaltungDialog>());
        Assert.Equal("true", cut.FindAll("[role=tab]")[0].GetAttribute("aria-selected"));

        var hinweise = Zeigen(verwaltung: Verwaltungsgaben(), startReiter: "HINWEISE");
        Assert.Contains("Anbieter", hinweise.Markup, StringComparison.Ordinal);

        // STATUS ohne Parametersatz: das Blatt gibt es gar nicht.
        var ohne = Zeigen(startReiter: "STATUS");
        Assert.Contains("Der Vertragstext.", ohne.Markup, StringComparison.Ordinal);

        // Unbekannter Schluessel: die Vereinbarung.
        var unbekannt = Zeigen(verwaltung: Verwaltungsgaben(), startReiter: "GIBTSNICHT");
        Assert.Contains("Der Vertragstext.", unbekannt.Markup, StringComparison.Ordinal);
    }

    /// <summary>Ein Reiterwechsel zeigt die erzeugten Abschnitte.</summary>
    [Fact]
    public void Der_Reiterwechsel_zeigt_die_Rechtstexte()
    {
        var cut = Zeigen();

        cut.FindAll("[role=tab]")[1].Click();

        Assert.Equal(2, cut.FindAll("h3.epos-lizenz-ueberschrift").Count);
        Assert.Equal(2, cut.FindAll("p.epos-lizenz-absatz").Count);
        Assert.Contains("Anbieter", cut.Markup, StringComparison.Ordinal);

        cut.FindAll("[role=tab]")[2].Click();
        Assert.Contains("Laufzeit und Bibliotheken", cut.Markup, StringComparison.Ordinal);
    }

    // ==================================================================
    //  Verweise
    // ==================================================================

    /// <summary>
    /// Ein <c>https://</c>-Lauf im Rechtstext wird ein echter Verweis, der sich in
    /// einem neuen Fenster öffnet — <c>Process.Start</c> gibt es hier nicht.
    /// </summary>
    [Fact]
    public void Aus_einer_Adresse_wird_ein_Verweis()
    {
        var cut = Zeigen();
        cut.FindAll("[role=tab]")[1].Click();

        var verweis = cut.FindAll("p.epos-lizenz-absatz a").Single();

        Assert.Equal("https://epos-plan.de/impressum/", verweis.GetAttribute("href"));
        Assert.Equal("_blank", verweis.GetAttribute("target"));
        Assert.Equal("noopener noreferrer", verweis.GetAttribute("rel"));
    }

    /// <summary>
    /// <b>Aus Text wird nie Auszeichnung.</b> Was wie HTML aussieht, bleibt Text —
    /// die Abschnitte werden als Elemente gebaut, nicht als <c>MarkupString</c>.
    /// </summary>
    [Fact]
    public void Aus_Text_wird_nie_Auszeichnung()
    {
        var cut = Render<LizenzDialog>(p =>
        {
            p.Add(x => x.Hinweise, new List<RechtsAbschnitt>
                  {
                      new RechtsAbschnitt(false, "<script>alert('x')</script> und <b>fett</b>")
                  })
             .Add(x => x.Texte, Texte());
        });

        cut.FindAll("[role=tab]")[1].Click();

        Assert.Empty(cut.FindAll("script"));
        Assert.Empty(cut.FindAll("p.epos-lizenz-absatz b"));
        Assert.Contains("<b>fett</b>", cut.Find("p.epos-lizenz-absatz").TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    /// Die Zerlegung erkennt <c>https://</c> und sonst nichts — kein <c>http://</c>,
    /// keine nackte Domäne, und ein Satzzeichen gehört nicht zur Adresse.
    /// </summary>
    [Theory]
    [InlineData("ohne Adresse", 0)]
    [InlineData("siehe http://alt.example/", 0)]
    [InlineData("siehe epos-plan.de", 0)]
    [InlineData("siehe https://epos-plan.de/agb/.", 1)]
    [InlineData("https://a.example und https://b.example", 2)]
    public void Die_Zerlegung_erkennt_nur_https(string text, int erwartet)
    {
        int verweise = LizenzDialog.Zerlegen(text).Count(s => s.IstVerweis);
        Assert.Equal(erwartet, verweise);
    }

    /// <summary>Das Satzzeichen am Ende bleibt Text.</summary>
    [Fact]
    public void Ein_Satzzeichen_gehoert_nicht_zur_Adresse()
    {
        var stuecke = LizenzDialog.Zerlegen("Kontakt: https://epos-plan.de/impressum/.").ToList();

        Assert.Equal("https://epos-plan.de/impressum/", stuecke.Single(s => s.IstVerweis).Stueck);
        Assert.Contains(stuecke, s => !s.IstVerweis && s.Stueck == ".");
    }

    // ==================================================================
    //  Die zwei Gesichter
    // ==================================================================

    /// <summary>
    /// Der Normalmodus zeigt „Schließen" und die Lizenzzeile; „Zustimmen" und
    /// „Ablehnen" gibt es nicht.
    /// </summary>
    [Fact]
    public void Der_Normalmodus_zeigt_Schliessen_und_den_Lizenzstand()
    {
        var cut = Zeigen();

        Assert.True(GibtKnopf(cut, "Schließen"));
        Assert.False(GibtKnopf(cut, "Zustimmen"));
        Assert.False(GibtKnopf(cut, "Ablehnen"));
        Assert.Contains("Lizenz: Firmenlizenz · gültig bis 31.12.2026", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// Der Zustimmungsmodus zeigt genau ZWEI zusätzliche Knöpfe statt „Schließen" —
    /// und in der Fußzeile den Bestätigungshinweis.
    /// </summary>
    [Fact]
    public void Der_Zustimmungsmodus_zeigt_zwei_Knoepfe_statt_einem()
    {
        var cut = Zeigen(zustimmungsmodus: true);

        Assert.True(GibtKnopf(cut, "Zustimmen"));
        Assert.True(GibtKnopf(cut, "Ablehnen"));
        Assert.False(GibtKnopf(cut, "Schließen"));
        Assert.Contains("Bitte lesen Sie die Vereinbarung", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Lizenz: Firmenlizenz", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// „Zustimmen" meldet BEIDES: die Zustimmung (die die Hülle sich merkt) und das
    /// Schließen mit <c>true</c> — der Startweg wertet genau das aus.
    /// </summary>
    [Fact]
    public void Zustimmen_meldet_die_Zustimmung_und_schliesst_mit_true()
    {
        int gemerkt = 0;
        var ergebnisse = new List<bool>();
        var cut = Zeigen(zustimmungsmodus: true,
                         zugestimmt: EventCallback.Factory.Create(new object(), () => gemerkt++),
                         geschlossen: EventCallback.Factory.Create<bool>(new object(), ergebnisse.Add));

        Knopf(cut, "Zustimmen").Click();

        Assert.Equal(1, gemerkt);
        Assert.Equal(new[] { true }, ergebnisse);
    }

    /// <summary>„Ablehnen" schließt mit <c>false</c> und merkt NICHTS.</summary>
    [Fact]
    public void Ablehnen_schliesst_mit_false_und_merkt_nichts()
    {
        int gemerkt = 0;
        var ergebnisse = new List<bool>();
        var cut = Zeigen(zustimmungsmodus: true,
                         zugestimmt: EventCallback.Factory.Create(new object(), () => gemerkt++),
                         geschlossen: EventCallback.Factory.Create<bool>(new object(), ergebnisse.Add));

        Knopf(cut, "Ablehnen").Click();

        Assert.Equal(0, gemerkt);
        Assert.Equal(new[] { false }, ergebnisse);
    }

    /// <summary>„Schließen" im Normalmodus meldet <c>true</c>.</summary>
    [Fact]
    public void Schliessen_meldet_true()
    {
        var ergebnisse = new List<bool>();
        var cut = Zeigen(geschlossen: EventCallback.Factory.Create<bool>(new object(), ergebnisse.Add));

        Knopf(cut, "Schließen").Click();

        Assert.Equal(new[] { true }, ergebnisse);
    }

    // ==================================================================
    //  Das Kreuz im Kopf (Anwenderentscheid 15.09.2026)
    // ==================================================================

    /// <summary>
    /// Im Zustimmungsmodus gibt es kein „Schließen" — das Kreuz geht denselben
    /// Weg wie „Ablehnen" (<c>false</c>, nichts gemerkt). Das entspricht dem
    /// Windows-Kreuz des besitzerlosen Fensters von heute: Ohne ausdrückliches
    /// Zustimmen bleibt das Ergebnis <c>false</c>.
    /// </summary>
    [Fact]
    public void Das_Kreuz_lehnt_im_Zustimmungsmodus_ab()
    {
        int gemerkt = 0;
        var ergebnisse = new List<bool>();
        var cut = Zeigen(zustimmungsmodus: true,
                         zugestimmt: EventCallback.Factory.Create(new object(), () => gemerkt++),
                         geschlossen: EventCallback.Factory.Create<bool>(new object(), ergebnisse.Add));

        cut.Find(".epos-dialog-zu").Click();

        Assert.Equal(0, gemerkt);
        Assert.Equal(new[] { false }, ergebnisse);
    }

    /// <summary>Im Normalmodus geht das Kreuz denselben Weg wie „Schließen" (<c>true</c>).</summary>
    [Fact]
    public void Das_Kreuz_schliesst_im_Normalmodus_mit_true()
    {
        var ergebnisse = new List<bool>();
        var cut = Zeigen(geschlossen: EventCallback.Factory.Create<bool>(new object(), ergebnisse.Add));

        cut.Find(".epos-dialog-zu").Click();

        Assert.Equal(new[] { true }, ergebnisse);
    }

    // ==================================================================
    //  Die Fußzeile und ihre Knopfreihenfolge
    // ==================================================================

    /// <summary>
    /// Die Herkunft steht in der Fußzeile, mit Stand, wenn er bekannt ist — und ohne,
    /// wenn nicht.
    /// </summary>
    [Fact]
    public void Die_Fusszeile_nennt_Quelle_und_Stand()
    {
        Assert.Contains("Quelle: https://epos-plan.de/agb/   ·   Stand 13.08.2026",
                        Zeigen().Find("div.epos-lizenz-herkunft").TextContent, StringComparison.Ordinal);

        string ohneStand = Zeigen(text: new LizenzTextGaben("Text.", @"C:\LIZENZ-INEKON.rtf", ""))
                           .Find("div.epos-lizenz-herkunft").TextContent;
        Assert.Contains(@"Quelle: C:\LIZENZ-INEKON.rtf", ohneStand, StringComparison.Ordinal);
        Assert.DoesNotContain("Stand", ohneStand, StringComparison.Ordinal);
    }

    /// <summary>
    /// Die Knopfreihenfolge ist bitgleich (Entscheid E-14): von RECHTS gelesen
    /// [Schließen] [Drucken] [Speichern]. <b>MN-1 (19.09.2026):</b> „Lizenz
    /// aktivieren…" ist aus der Leiste gefallen — er öffnete die Verwaltung als
    /// Überlagerung, und die ist jetzt das erste Reiterblatt.
    /// </summary>
    [Fact]
    public void Die_Knopfreihenfolge_ist_bitgleich()
    {
        var cut = Zeigen(speichern: (r, i) => Task.FromResult<string?>(null),
                         verwaltung: Verwaltungsgaben());

        var texte = cut.FindAll("div.epos-lizenz-knoepfe button")
                       .Select(b => b.TextContent.Trim())
                       .ToList();

        // GENAU drei: der Infoknopf stand bis zur Abnahme als letztes Element
        // hinter "Schliessen" und musste aus der Liste gefiltert werden.
        Assert.Equal(new[] { "Speichern unter...", "Drucken...", "Schließen" }, texte);
    }

    /// <summary>
    /// Ohne Delegat kein Knopf — die Hausregel gilt auch hier: „Speichern unter…"
    /// verschwindet, wenn die Hülle sie nicht bedient. „Lizenz aktivieren…" gibt es
    /// seit MN-1 gar nicht mehr.
    /// </summary>
    [Fact]
    public void Ohne_Delegat_kein_Knopf()
    {
        var cut = Zeigen();

        Assert.False(GibtKnopf(cut, "Speichern unter..."));
        Assert.False(GibtKnopf(cut, "Lizenz aktivieren..."));
        Assert.True(GibtKnopf(cut, "Drucken..."));

        // Und auch MIT Parametersatz der Verwaltung kommt er nicht wieder.
        var mit = Zeigen(verwaltung: Verwaltungsgaben());
        Assert.False(GibtKnopf(mit, "Lizenz aktivieren..."));
    }

    // ==================================================================
    //  Speichern und nachladen
    // ==================================================================

    /// <summary>
    /// Die Online-Fassung wird nachgeliefert und ersetzt den Ladehinweis — und zwar
    /// GENAU EINMAL. Der Vorläufer schrieb dafür aus einem <c>async void</c> in die
    /// Anzeige (Befund W15c-B28).
    /// </summary>
    [Fact]
    public void Die_Onlinefassung_wird_genau_einmal_nachgeliefert()
    {
        int rufe = 0;
        var cut = Zeigen(
            text: new LizenzTextGaben("Die Lizenzvereinbarung wird geladen...", "https://epos-plan.de/agb/", ""),
            onlineNachladen: () =>
            {
                rufe++;
                return Task.FromResult<LizenzTextGaben?>(
                    new LizenzTextGaben("Die geholte Fassung.", "https://epos-plan.de/agb/", "13.08.2026"));
            });

        cut.WaitForAssertion(() => Assert.Contains("Die geholte Fassung.", cut.Markup, StringComparison.Ordinal));
        Assert.Equal(1, rufe);
        Assert.Contains("Stand 13.08.2026", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// „Speichern unter…" reicht den Inhalt der AKTIVEN Karte hinaus und meldet den
    /// Erfolg an Ort und Stelle statt in einer MessageBox.
    /// </summary>
    [Fact]
    public void Gespeichert_wird_die_aktive_Karte()
    {
        string? gesehenerReiter = null, gesehenerInhalt = null;
        var cut = Zeigen(speichern: (r, i) =>
        {
            gesehenerReiter = r; gesehenerInhalt = i;
            return Task.FromResult<string?>(@"C:\Temp\EPOS-Plan_Lizenz.txt");
        });

        Knopf(cut, "Speichern unter...").Click();
        cut.WaitForAssertion(() => Assert.Contains("Gespeichert:", cut.Markup, StringComparison.Ordinal));
        Assert.Equal("VERTRAG", gesehenerReiter);
        Assert.Equal("Der Vertragstext.", gesehenerInhalt);

        cut.FindAll("[role=tab]")[2].Click();
        Knopf(cut, "Speichern unter...").Click();
        cut.WaitForAssertion(() => Assert.Equal("KOMPONENTEN", gesehenerReiter));
        Assert.Contains("Microsoft .NET 10 mit Windows Forms.", gesehenerInhalt!, StringComparison.Ordinal);
    }

    // ==================================================================
    //  MN-1 (19.09.2026) — die Lizenzverwaltung als erstes Reiterblatt
    // ==================================================================

    /// <summary>
    /// Der Reiter „Status &amp; Aktivierung" trägt die Verwaltung — IM selben
    /// Fenster, ohne zweites modales Fenster (Risiko R2) und seit MN-1 auch ohne
    /// Überlagerung (bis dahin Entscheid E-11): Ein Klick auf den Reiter, nicht
    /// zwei auf Knopf und Kreuz.
    /// </summary>
    [Fact]
    public void Die_Verwaltung_steht_im_ersten_Reiterblatt()
    {
        var cut = Zeigen(verwaltung: Verwaltungsgaben());

        // Solange die Lesekarte vorn steht, ist die Maske gar nicht gezeichnet.
        Assert.Empty(cut.FindComponents<LizenzVerwaltungDialog>());

        cut.FindAll("[role=tab]")[0].Click();

        Assert.Single(cut.FindComponents<LizenzVerwaltungDialog>());
        Assert.Contains("Lizenzstatus auf diesem Arbeitsplatz", cut.Markup, StringComparison.Ordinal);

        // Es gibt KEINE Überlagerung mehr und keinen Knopf, der eine öffnete.
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
        Assert.False(GibtKnopf(cut, "Lizenz aktivieren..."));
    }

    /// <summary>
    /// Anwenderbefund 15.09.2026 („Doppeltes Kreuz dürfen nicht sein!"): Auch mit
    /// der Verwaltung im Reiter trägt das Fenster GENAU EIN ✕ — das des
    /// Dialogkopfs. Das Blatt bringt keins mit, und es gibt nur EINE Fußzeile mit
    /// dem Lizenzstand und nur EINEN Knopf „Schließen".
    /// </summary>
    [Fact]
    public void Der_Lizenzdialog_zeigt_ein_Kreuz_und_eine_Fusszeile()
    {
        var cut = Zeigen(verwaltung: Verwaltungsgaben());
        cut.FindAll("[role=tab]")[0].Click();

        Assert.Single(cut.FindComponents<EPOS.UI.Bausteine.Schliesskreuz>());
        Assert.Empty(cut.FindAll(".epos-lizverw-fuss .epos-dialog-zu"));

        Assert.Single(cut.FindAll("footer.epos-lizenz-fuss"));
        Assert.Contains("Lizenz: Firmenlizenz · gültig bis 31.12.2026", cut.Markup, StringComparison.Ordinal);
        Assert.Single(cut.FindAll("button"), b => b.TextContent.Trim() == "Schließen");
    }

    /// <summary>
    /// MN-1: Die zwei Sprünge unter dem Übertragungshinweis schalten den Reiter des
    /// WIRTS um — aus der Aktivierungsmaske heraus auf die Lizenzvereinbarung und
    /// auf die rechtlichen Hinweise, ohne das Fenster zu verlassen.
    /// </summary>
    [Fact]
    public void Die_Spruenge_aus_der_Verwaltung_wechseln_den_Reiter()
    {
        var cut = Zeigen(verwaltung: Verwaltungsgaben());
        cut.FindAll("[role=tab]")[0].Click();

        cut.FindAll(".epos-lizverw-spruenge button")[0].Click();

        Assert.Equal("true", cut.FindAll("[role=tab]")[1].GetAttribute("aria-selected"));
        Assert.Contains("Der Vertragstext.", cut.Markup, StringComparison.Ordinal);

        // Zurueck auf die Maske und diesmal auf die rechtlichen Hinweise.
        cut.FindAll("[role=tab]")[0].Click();
        cut.FindAll(".epos-lizverw-spruenge button")[1].Click();

        Assert.Equal("true", cut.FindAll("[role=tab]")[2].GetAttribute("aria-selected"));
        Assert.Contains("Anbieter", cut.Markup, StringComparison.Ordinal);
    }

    // ==================================================================
    //  E-7: der Sprachhinweis
    // ==================================================================

    /// <summary>
    /// Auf Englisch steht über den Rechtstexten „Binding version in German." — auf
    /// Deutsch ist der Schlüssel leer, und dann steht dort nichts (Entscheid E-7).
    /// </summary>
    [Fact]
    public void Der_Sprachhinweis_erscheint_nur_wenn_er_gesetzt_ist()
    {
        var ohne = Zeigen();
        ohne.FindAll("[role=tab]")[1].Click();
        Assert.Empty(ohne.FindAll("p.epos-lizenz-sprachhinweis"));

        var mit = Zeigen(sprachHinweis: "Binding version in German.");
        mit.FindAll("[role=tab]")[1].Click();
        Assert.Equal("Binding version in German.",
                     mit.Find("p.epos-lizenz-sprachhinweis").TextContent.Trim());
    }

    /// <summary>Der Infoknopf trägt den Schlüssel aus <c>help_mapping.txt</c>.</summary>
    [Fact]
    public void Der_Infoknopf_traegt_seinen_Schluessel()
    {
        var cut = Zeigen();
        Assert.Equal("Form_Lizenz.btn_Help",
                     cut.FindComponent<EPOS.UI.Bausteine.InfoKnopf>().Instance.Schluessel);
    }

    // =====================================================================
    //  Windows-Abnahme 05.09.2026 — W15c-B-1 und W15c-E-1
    // =====================================================================

    /// <summary>
    /// <b>Befund W15c‑E‑1 — LESEBEREICH statt Formularfeld.</b> Der Vertragstext
    /// stand in einem <c>&lt;textarea&gt;</c>: ein Formularfeld mit
    /// Größenanfasser, eigener Schriftfläche und eigenem Rollbalken für einen
    /// Text, den niemand bearbeitet. Jetzt trägt <b>jede</b> der drei Karten
    /// denselben Lesebereich.
    /// </summary>
    [Fact]
    public void Der_Vertragstext_steht_in_einem_Lesebereich_und_nicht_im_Formularfeld()
    {
        var cut = Zeigen();

        Assert.Empty(cut.FindAll("textarea"));
        Assert.Single(cut.FindAll("div.epos-lizenz-lesebereich"));

        var bereich = cut.Find("div.epos-lizenz-lesebereich");
        Assert.Equal("region", bereich.GetAttribute("role"));
        Assert.Equal("Lizenzvereinbarung", bereich.GetAttribute("aria-label"));
        Assert.Equal("0", bereich.GetAttribute("tabindex"));

        // Auch die zwei erzeugten Karten lesen sich im selben Rahmen.
        cut.FindAll("[role=tab]")[1].Click();
        Assert.Equal("Rechtliche Hinweise",
                     cut.Find("div.epos-lizenz-lesebereich").GetAttribute("aria-label"));
        Assert.Empty(cut.FindAll("textarea"));
    }

    /// <summary>
    /// Der Vertragstext wird GESETZT: Eine Zeile, die mit „§" beginnt, ist eine
    /// Überschrift, alles andere ein Absatz. <b>Am Wortlaut ändert sich nichts</b>.
    /// </summary>
    [Fact]
    public void Der_Vertragstext_wird_in_Absaetze_und_Paragrafen_gesetzt()
    {
        var cut = Zeigen(text: new LizenzTextGaben(
            "Vorbemerkung.\n\n§ 1 Geltungsbereich\nDiese Vereinbarung gilt.\n\n§ 2 Nutzung\nSie dürfen.",
            "https://epos-plan.de/agb/", "13.08.2026"));

        var koepfe = cut.FindAll("div.epos-lizenz-lesebereich h3.epos-lizenz-ueberschrift")
                        .Select(h => h.TextContent.Trim()).ToList();
        var absaetze = cut.FindAll("div.epos-lizenz-lesebereich p.epos-lizenz-absatz")
                          .Select(a => a.TextContent.Trim()).ToList();

        Assert.Equal(new[] { "§ 1 Geltungsbereich", "§ 2 Nutzung" }, koepfe);
        Assert.Equal(new[] { "Vorbemerkung.", "Diese Vereinbarung gilt.", "Sie dürfen." }, absaetze);
    }

    /// <summary>Die Zerlegung des Vertragstextes — die REGEL, ohne den Dialog.</summary>
    [Theory]
    [InlineData("", 0, 0)]
    [InlineData("   \n\n  ", 0, 0)]
    [InlineData("Ein Satz.", 0, 1)]
    [InlineData("Zeile eins\nZeile zwei", 0, 1)]
    [InlineData("Eins.\n\nZwei.", 0, 2)]
    [InlineData("§ 1 Zweck\nText.", 1, 1)]
    [InlineData("Kopf.\n§ 1 Zweck\nText.\n§ 2 Ende\nSchluss.", 2, 3)]
    public void Vertragsabschnitte_trennt_Paragrafen_von_Absaetzen(string text, int koepfe, int absaetze)
    {
        var abschnitte = LizenzDialog.Vertragsabschnitte(text);

        Assert.Equal(koepfe, abschnitte.Count(a => a.IstUeberschrift));
        Assert.Equal(absaetze, abschnitte.Count(a => !a.IstUeberschrift));
    }

    /// <summary>
    /// <b>Im Vertragstext wird nichts verlinkt.</b> Er kommt von auswärts — von
    /// <c>epos-plan.de</c> oder aus einer Datei —, und dort wird nichts geraten;
    /// im ERZEUGTEN Rechtstext (unser eigener Ressourcentext) dagegen schon.
    /// </summary>
    [Fact]
    public void Der_Vertragstext_bekommt_keine_Verweise()
    {
        var cut = Zeigen(text: new LizenzTextGaben(
            "Die Fassung steht unter https://epos-plan.de/agb/ bereit.",
            "https://epos-plan.de/agb/", ""));

        Assert.Empty(cut.FindAll("div.epos-lizenz-lesebereich a"));
        Assert.Contains("https://epos-plan.de/agb/",
                        cut.Find("p.epos-lizenz-absatz").TextContent, StringComparison.Ordinal);

        cut.FindAll("[role=tab]")[1].Click();
        Assert.Single(cut.FindAll("div.epos-lizenz-lesebereich a"));
    }

    /// <summary>
    /// <b>Anwender, 05.09.2026: „Wozu gibt es ‚Datei wählen'? löschen."</b> Den
    /// Knopf gibt es nicht mehr — auch nicht als Parameter, den eine Hülle noch
    /// setzen könnte.
    /// </summary>
    [Fact]
    public void Es_gibt_keinen_Knopf_Datei_waehlen_mehr()
    {
        var cut = Zeigen(speichern: (r, i) => Task.FromResult<string?>(null),
                         verwaltung: new Dictionary<string, object>());

        Assert.False(GibtKnopf(cut, "Datei wählen..."));
        Assert.DoesNotContain("Datei wählen", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("div.epos-lizenz-werkzeuge"));

        Assert.Null(typeof(LizenzDialog).GetProperty("DateiWaehlen"));
    }

    /// <summary>
    /// Der Hilfeknopf steht im KOPF und nicht hinter „Schließen" (W15c‑E‑1) —
    /// dieselbe Lage wie in jedem anderen Dialog des Hauses.
    /// </summary>
    [Fact]
    public void Der_Infoknopf_steht_im_Kopf_und_nicht_in_der_Knopfleiste()
    {
        var cut = Zeigen();

        Assert.NotNull(cut.Find("header.epos-lizenz-kopf").QuerySelector("button.epos-infoknopf"));
        Assert.Null(cut.Find("div.epos-lizenz-knoepfe").QuerySelector("button.epos-infoknopf"));
    }

    /// <summary>Die Knopfleiste ist die des Hauses — <c>epos-leiste</c>.</summary>
    [Fact]
    public void Die_Knopfleiste_ist_die_des_Hauses()
    {
        var leiste = Zeigen().Find("div.epos-lizenz-knoepfe");

        Assert.Contains("epos-leiste", leiste.ClassName ?? "", StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Befund W15c‑B‑1 — die Seite rollt nie waagerecht, und sie hat einen
    /// Rand.</b> Der Anwender fand jede Zeile links angeschnitten: Die drei
    /// Wurzeln der Welle 15c hängen nicht unter <c>.epos-dialog</c> und trugen
    /// deshalb keinen Seitenrand; ohne <c>overflow-x</c> verschob zudem jeder
    /// Waagerecht-Überlauf die ganze Seite. Eine bunit-Probe sieht ein Stilblatt
    /// nicht (Lehre W6‑B‑1) — geprüft wird die REGEL, wie in
    /// <c>StartseiteTests</c>.
    /// </summary>
    [Fact]
    public void Die_Wurzeln_der_Welle_tragen_Rand_und_rollen_nicht_waagerecht()
    {
        string block = Stilblock(".epos-lizenz,\n.epos-lizverw,\n.epos-erststart {");

        Assert.Contains("box-sizing: border-box", block, StringComparison.Ordinal);
        Assert.Contains("padding: var(--epos-karte-rand)", block, StringComparison.Ordinal);
        Assert.Contains("max-width: 100%", block, StringComparison.Ordinal);
        Assert.Contains("overflow-x: clip", block, StringComparison.Ordinal);

        // Keine feste Breite - sie waere genau der Ueberlauf, den der Befund meldet.
        Assert.DoesNotContain("width: 9", block, StringComparison.Ordinal);

        // MN-1 (19.09.2026): Das REITERBLATT bringt seinen Rand mit; er darf sich
        // nicht addieren (bis dahin stand hier die Ueberlagerung, E-11).
        Assert.Contains("padding: 0",
                        Stilblock(".epos-lizenz-verwaltungsblatt > .epos-lizverw {"),
                        StringComparison.Ordinal);

        // Und das Blatt rollt wie die drei Lesebereiche daneben - senkrecht ja,
        // waagerecht nie; sonst spraenge das Bild beim Reiterwechsel.
        string blatt = Stilblock(".epos-lizenz-verwaltungsblatt {");
        Assert.Contains("overflow-y: auto", blatt, StringComparison.Ordinal);
        Assert.Contains("overflow-x: hidden", blatt, StringComparison.Ordinal);
    }

    /// <summary>
    /// Breiter Inhalt rollt in SEINEM Rahmen: Der Lesebereich rollt senkrecht,
    /// waagerecht gar nicht — dieselbe Bauart wie <c>.epos-verlauf</c> (W15b).
    /// </summary>
    [Fact]
    public void Der_Lesebereich_rollt_senkrecht_und_nie_waagerecht()
    {
        string block = Stilblock(".epos-lizenz-lesebereich {");

        Assert.Contains("overflow-y: auto", block, StringComparison.Ordinal);
        Assert.Contains("overflow-x: hidden", block, StringComparison.Ordinal);
        Assert.DoesNotContain("resize", block, StringComparison.Ordinal);

        // Die Zeilenlaenge bleibt angenehm, und die Schriftstufen kommen aus den
        // Token statt aus Pixeln (W15c-E-1).
        Assert.Contains("max-width: 78ch", Stilblock(".epos-lizenz-absatz {"), StringComparison.Ordinal);
        Assert.Contains("var(--epos-schriftgroesse-kartentitel)",
                        Stilblock(".epos-lizenz-titel {"), StringComparison.Ordinal);
        Assert.Contains("var(--epos-schriftgroesse-gruppenkopf)",
                        Stilblock(".epos-lizenz-ueberschrift {"), StringComparison.Ordinal);
    }

    /// <summary>Liest den Rumpf einer Regel aus <c>EPOS.UI/wwwroot/epos-ui.css</c>.</summary>
    private static string Stilblock(string selektor)
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null &&
               !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);   // das Stilblatt muss im Baum stehen

        // Zeilenenden angleichen: Auf Windows liegt das Blatt nach dem Auschecken
        // mit CRLF, ein mehrzeiliger Selektor traegt hier aber "\n" - dieselbe
        // Angleichung wie in StartseiteTests und StilblattTests.
        string css = File.ReadAllText(Path.Combine(d!.FullName, "EPOS.UI", "wwwroot", "epos-ui.css"))
                         .Replace("\r\n", "\n");
        selektor = selektor.Replace("\r\n", "\n");

        int a = css.IndexOf(selektor, StringComparison.Ordinal);
        Assert.True(a >= 0, "Regel " + selektor + " steht nicht im Stilblatt");
        int e = css.IndexOf('}', a);
        Assert.True(e > a);
        return css.Substring(a + selektor.Length, e - a - selektor.Length);
    }
}
