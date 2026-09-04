using System;
using System.Collections.Generic;
using System.Globalization;
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
public class LizenzDialogTests : BunitContext
{
    public LizenzDialogTests()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;

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
        ReiterVertrag = "Lizenzvereinbarung",
        ReiterHinweise = "Rechtliche Hinweise",
        ReiterKomponenten = "Komponenten",
        KnopfDatei = "Datei wählen...",
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
        Func<Task<LizenzTextGaben?>>? dateiWaehlen = null,
        Func<Task<LizenzTextGaben?>>? onlineNachladen = null,
        Func<string, string, Task<string?>>? speichern = null,
        IReadOnlyDictionary<string, object>? verwaltung = null,
        EventCallback<bool>? geschlossen = null,
        EventCallback? zugestimmt = null,
        string sprachHinweis = "")
    {
        return Render<LizenzDialog>(p =>
        {
            p.Add(x => x.Zustimmungsmodus, zustimmungsmodus)
             .Add(x => x.Text, text ?? new LizenzTextGaben("Der Vertragstext.", "https://epos-plan.de/agb/", "13.08.2026"))
             .Add(x => x.Lizenzstatus, "Firmenlizenz · gültig bis 31.12.2026")
             .Add(x => x.Hinweise, HINWEISE)
             .Add(x => x.Komponenten, KOMPONENTEN)
             .Add(x => x.Texte, Texte(sprachHinweis));

            if (dateiWaehlen is not null) p.Add(x => x.DateiWaehlen, dateiWaehlen);
            if (onlineNachladen is not null) p.Add(x => x.OnlineNachladen, onlineNachladen);
            if (speichern is not null) p.Add(x => x.Speichern, speichern);
            if (verwaltung is not null) p.Add(x => x.VerwaltungGaben, verwaltung);
            if (geschlossen is not null) p.Add(x => x.Geschlossen, geschlossen.Value);
            if (zugestimmt is not null) p.Add(x => x.Zugestimmt, zugestimmt.Value);
        });
    }

    private static IElement Knopf(IRenderedComponent<LizenzDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static bool GibtKnopf(IRenderedComponent<LizenzDialog> cut, string text)
        => cut.FindAll("button").Any(b => b.TextContent.Trim() == text);

    // ==================================================================
    //  Die drei Registerkarten
    // ==================================================================

    /// <summary>
    /// Drei Registerkarten in ihrer Reihenfolge — der Wellenplan sprach von vier,
    /// gemessen sind es drei (Befund W15c-B3).
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

    /// <summary>Beim Öffnen steht die verbindliche erste Karte vorn.</summary>
    [Fact]
    public void Die_verbindliche_Karte_steht_vorn()
    {
        var cut = Zeigen();

        Assert.Contains("Der Vertragstext.", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Anbieter", cut.Markup, StringComparison.Ordinal);
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
    /// [Schließen] [Drucken] [Speichern] [Aktivieren].
    /// </summary>
    [Fact]
    public void Die_Knopfreihenfolge_ist_bitgleich()
    {
        var cut = Zeigen(speichern: (r, i) => Task.FromResult<string?>(null),
                         verwaltung: new Dictionary<string, object>());

        var texte = cut.FindAll("div.epos-lizenz-knoepfe button")
                       .Select(b => b.TextContent.Trim())
                       .Where(t => t.Length > 0)
                       .ToList();

        Assert.Equal(new[] { "Lizenz aktivieren...", "Speichern unter...", "Drucken...", "Schließen" },
                     texte.Take(4));
    }

    /// <summary>
    /// Ohne Delegat kein Knopf — die Hausregel gilt auch hier: „Speichern unter…"
    /// und „Lizenz aktivieren…" verschwinden, wenn die Hülle sie nicht bedient.
    /// </summary>
    [Fact]
    public void Ohne_Delegat_kein_Knopf()
    {
        var cut = Zeigen();

        Assert.False(GibtKnopf(cut, "Speichern unter..."));
        Assert.False(GibtKnopf(cut, "Lizenz aktivieren..."));
        Assert.False(GibtKnopf(cut, "Datei wählen..."));
        Assert.True(GibtKnopf(cut, "Drucken..."));
    }

    // ==================================================================
    //  Datei wählen, speichern, nachladen
    // ==================================================================

    /// <summary>Eine gewählte Datei ersetzt Text und Herkunft; ein Abbruch nicht.</summary>
    [Fact]
    public void Eine_gewaehlte_Datei_ersetzt_den_Text()
    {
        var cut = Zeigen(dateiWaehlen: () => Task.FromResult<LizenzTextGaben?>(
            new LizenzTextGaben("Die Datei liegt vor.", @"C:\LIZENZ-INEKON.rtf", "")));

        Knopf(cut, "Datei wählen...").Click();

        cut.WaitForAssertion(() => Assert.Contains("Die Datei liegt vor.", cut.Markup, StringComparison.Ordinal));
        Assert.Contains(@"Quelle: C:\LIZENZ-INEKON.rtf", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>Ein abgebrochener Wähler lässt alles, wie es war.</summary>
    [Fact]
    public void Ein_abgebrochener_Waehler_aendert_nichts()
    {
        var cut = Zeigen(dateiWaehlen: () => Task.FromResult<LizenzTextGaben?>(null));

        Knopf(cut, "Datei wählen...").Click();

        cut.WaitForAssertion(() => Assert.Contains("Der Vertragstext.", cut.Markup, StringComparison.Ordinal));
    }

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
    //  Die Lizenzverwaltung als Überlagerung (E-11)
    // ==================================================================

    /// <summary>
    /// „Lizenz aktivieren…" öffnet die Verwaltung IM selben Fenster — kein zweites
    /// modales Fenster (Risiko R2).
    /// </summary>
    [Fact]
    public void Die_Verwaltung_erscheint_als_Ueberlagerung()
    {
        var gaben = new Dictionary<string, object>
        {
            ["Lage"] = new LizenzGaben("GUELTIG", "Firmenlizenz", "Lizenz EPOS-2026-00001", true, ""),
            ["Texte"] = new LizenzTexte
            {
                Verwaltung =
                {
                    GruppeStatus = "Lizenzstatus auf diesem Arbeitsplatz",
                    KnopfSchliessen = "Schließen",
                }
            },
        };
        var cut = Zeigen(verwaltung: gaben);

        Assert.Empty(cut.FindComponents<LizenzVerwaltungDialog>());

        Knopf(cut, "Lizenz aktivieren...").Click();

        Assert.Single(cut.FindComponents<LizenzVerwaltungDialog>());
        Assert.Contains("Lizenzstatus auf diesem Arbeitsplatz", cut.Markup, StringComparison.Ordinal);
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
}
