using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Hilfe;
using EPOS.UI.Dienste;
using KiKern;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Hilfe;

/// <summary>
/// „FELDWERTE MITSENDEN" — der Weg 4 des Konzepts „Der Hilfe-Assistent im Dialog"
/// (Auftrag #200, Stufe S2).
///
/// <para><b>Was hier bewiesen wird.</b> Ein Dialog meldet seine Felder beim Öffnen an und
/// beim Schließen ab; der Schalter im Chat erscheint nur mit angemeldeter Maske und
/// bleibt ohne Einwilligungsweg gesperrt — mit sichtbarem Grund; die Vorschau zeigt die
/// WERTE; und ohne Einwilligung geht kein Feldwert hinaus.</para>
///
/// <para><b>Warum die Anmeldeprobe OHNE Komponente läuft.</b> Die Maskenbrücke ist
/// prozessweiter Zustand, und xunit fährt Testklassen nebeneinander: Jede andere Klasse,
/// die <c>PufferSpKatalogDialog</c> zeichnet, meldet DIESELBE Maske an und löst eine
/// fremde Anmeldung ab. Was der Brücke gegenüber deterministisch ist, sind deshalb (a)
/// die Anmeldung über <see cref="KiMaskenanmeldung"/> selbst — sie läuft ohne Renderer
/// und ohne Wartepunkt — und (b) am gezeichneten Dialog die MONOTONEN Aussagen:
/// angemeldet ist angemeldet, und das Feld des Katalogs steht in der Liste. Eine
/// Wertprobe am gezeichneten Dialog wäre ein flatterhafter Zeuge, und ein flatterhafter
/// Zeuge ist schlimmer als keiner.</para>
///
/// <para>Der Chat bekommt seine drei Feldwert-Einstiege als DELEGATEN (wie jeden anderen
/// Weg nach draußen, § 15.3): Diese Fälle setzen sie selbst und rühren die Brücke gar
/// nicht an.</para>
/// </summary>
public class KiFeldwerteTests : EposBunitContext
{
    public KiFeldwerteTests()
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // =====================================================================
    //  (1) Der Dialog meldet an und ab
    // =====================================================================

    [Fact]
    public void Die_Anmeldung_haelt_die_Getter_auf_den_LEBENDEN_Dialogzustand()
    {
        var daten = new PufferSpKatalogDaten { Gesamtvolumen = 750 };

        using (KiMaskenanmeldung anmeldung =
                   KiMaskenanmeldung.Fuer(KiMaskennamen.PUFFERSPEICHER, () => daten))
        {
            Assert.True(anmeldung.Angemeldet);

            KiFeldzugang zugang =
                KiMaskenbruecke.Feldzugang(KiMaskennamen.PUFFERSPEICHER, "gesamtvolumen")!;

            Assert.NotNull(zugang);
            Assert.Equal(750, zugang.Lesen());

            // Der Getter zeigt auf den DIALOGZUSTAND, nicht auf eine Kopie vom Anmelden:
            // Was der Anwender gerade getippt und noch nicht gespeichert hat, steht drin.
            daten.Gesamtvolumen = 1500;
            Assert.Equal(1500, zugang.Lesen());
        }
    }

    [Fact]
    public void Dispose_meldet_ab_und_ist_mehrfach_erlaubt()
    {
        var daten = new PufferSpKatalogDaten();
        KiMaskenanmeldung anmeldung =
            KiMaskenanmeldung.Fuer(KiMaskennamen.PUFFERSPEICHER, () => daten);

        Assert.True(anmeldung.Angemeldet);

        anmeldung.Dispose();
        anmeldung.Dispose();          // ein zweites Mal ist kein Fehler

        Assert.False(anmeldung.Angemeldet);
    }

    [Fact]
    public void Eine_leere_Quelle_meldet_trotzdem_an_und_liest_leer()
    {
        // Der Photovoltaik-Dialog meldet die GEWAEHLTE Zeile an - und solange keine
        // gewaehlt ist, gibt es keine. Die Maske steht trotzdem offen.
        ErzeugerZeile? keine = null;

        using KiMaskenanmeldung anmeldung =
            KiMaskenanmeldung.Fuer(KiMaskennamen.PHOTOVOLTAIK, () => keine);

        Assert.True(anmeldung.Angemeldet);

        KiFeldzugang zugang = KiMaskenbruecke.Feldzugang(KiMaskennamen.PHOTOVOLTAIK, "neigung")!;
        Assert.Null(zugang.Lesen());
    }

    [Fact]
    public void Der_gezeichnete_Dialog_meldet_seine_Maske_an()
    {
        // Die MONOTONE Aussage (siehe Klassenkopf): Nach dem Zeichnen steht die Maske
        // in der Bruecke, und ihre Feldliste ist die des Katalogs.
        IRenderedComponent<PufferSpKatalogDialog> cut =
            Render<PufferSpKatalogDialog>(p => p.Add(x => x.Daten,
                                                     new PufferSpKatalogDaten { Gesamtvolumen = 750 }));

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.PUFFERSPEICHER));

        IReadOnlyList<KiFeldwert> werte = KiMaskenbruecke.Lesen(KiMaskennamen.PUFFERSPEICHER);
        Assert.Single(werte);
        Assert.Equal("gesamtvolumen", werte[0].Name);

        Assert.NotNull(cut.Instance);
    }

    [Theory]
    [InlineData(typeof(PufferSpKatalogDialog))]
    [InlineData(typeof(HeizkesselKatalogDialog))]
    [InlineData(typeof(PhotovoltaikDialog))]
    [InlineData(typeof(EPOS.UI.Dialoge.Waermepumpe.WaermepumpeStammDialog))]
    [InlineData(typeof(EPOS.UI.Seiten.Strom.StromspeicherAuslegungSeite))]
    public void Jede_der_fuenf_Komponenten_kann_sich_abmelden(Type komponente)
    {
        // Ohne IDisposable käme das Abmelden nie — die Anmeldung überlebte den Dialog,
        // und der Assistent läse die Maske von vorhin.
        Assert.True(typeof(IDisposable).IsAssignableFrom(komponente), komponente.Name);
    }

    // =====================================================================
    //  (2) Der Schalter im Chat
    // =====================================================================

    [Fact]
    public void Ohne_angemeldete_Maske_gibt_es_den_Schalter_nicht()
    {
        IRenderedComponent<KiChatDialog> cut = Chat();

        Assert.False(cut.Instance.FeldwerteMoeglich);
        Assert.DoesNotContain(SCHALTER, cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mit_angemeldeter_Maske_steht_der_Schalter_da()
    {
        IRenderedComponent<KiChatDialog> cut = Chat(felder: () => Block());

        Assert.True(cut.Instance.FeldwerteMoeglich);
        Assert.Contains(SCHALTER, cut.Markup, StringComparison.Ordinal);
        Assert.False(cut.Instance.FeldwerteStehen);      // aus, bis der Anwender ihn setzt
    }

    [Fact]
    public void Im_Hilfe_Betrieb_gibt_es_ihn_nicht()
    {
        // Abgeschaltete KI: kein "Fragen", kein Aktionsschalter - und erst recht kein
        // Weg, Feldwerte an einen Anbieter zu senden.
        IRenderedComponent<KiChatDialog> cut = Chat(felder: () => Block(), hilfeBetrieb: true);

        Assert.False(cut.Instance.FeldwerteMoeglich);
        Assert.DoesNotContain(SCHALTER, cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Eine_Maske_ohne_Felder_laesst_den_Schalter_weg()
    {
        // Ein Schalter ohne etwas zu senden waere wirkungslos.
        IRenderedComponent<KiChatDialog> cut = Chat(felder: () => new KiDialogdaten());

        Assert.False(cut.Instance.FeldwerteMoeglich);
    }

    [Fact]
    public void Ohne_Einwilligungsweg_ist_der_Schalter_gesperrt_UND_der_Grund_steht_da()
    {
        IRenderedComponent<KiChatDialog> cut = Chat(felder: () => Block(), gesperrt: true);

        Assert.Contains(SCHALTER, cut.Markup, StringComparison.Ordinal);
        Assert.Contains("disabled", Kaestchen(cut).OuterHtml, StringComparison.Ordinal);

        // Der Grund steht SICHTBAR daneben: Ein gesperrtes Kaestchen nimmt keine
        // Zeigerereignisse an, sein Tooltip erschiene nie (Hausregel EPOS.UI/CLAUDE.md).
        Assert.Contains(GESPERRT, cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Ohne_Sperre_ist_das_Kaestchen_bedienbar()
    {
        IRenderedComponent<KiChatDialog> cut = Chat(felder: () => Block());

        Assert.DoesNotContain("disabled", Kaestchen(cut).OuterHtml, StringComparison.Ordinal);
        Assert.DoesNotContain(GESPERRT, cut.Markup, StringComparison.Ordinal);
    }

    // =====================================================================
    //  (3) Die Einwilligung
    // =====================================================================

    [Fact]
    public void Das_Einschalten_holt_die_Einwilligung_EINMAL_ein()
    {
        int gefragt = 0;
        IRenderedComponent<KiChatDialog> cut = Chat(
            felder: () => Block(),
            einwilligen: () => { gefragt++; return Task.FromResult(true); });

        Umschalten(cut, true);
        Assert.True(cut.Instance.FeldwerteStehen);
        Assert.Equal(1, gefragt);
        Assert.Contains("Feldwerte gehen mit.", Texte(cut));

        // Aus und wieder ein: Das AUSschalten fragt nicht, das Einschalten schon - der
        // Merker liegt im Kern und liefert dann sofort "ja".
        Umschalten(cut, false);
        Assert.False(cut.Instance.FeldwerteStehen);
        Assert.Equal(1, gefragt);
        Assert.Contains("Feldwerte bleiben hier.", Texte(cut));
    }

    [Fact]
    public void Ohne_Einwilligung_bleibt_der_Schalter_aus_und_der_Verlauf_sagt_warum()
    {
        IRenderedComponent<KiChatDialog> cut = Chat(
            felder: () => Block(),
            einwilligen: () => Task.FromResult(false));

        Umschalten(cut, true);

        Assert.False(cut.Instance.FeldwerteStehen);
        Assert.Contains("Ohne Einwilligung kein Feldwert.", Texte(cut));
    }

    [Fact]
    public void Ohne_Einwilligungsweg_bleibt_der_Schalter_ebenfalls_aus()
    {
        // Kein Delegat heisst: Die Einwilligung laesst sich hier nicht einholen. Der
        // Schalter darf dann nicht stillschweigend wirken.
        IRenderedComponent<KiChatDialog> cut = Chat(felder: () => Block(), gesperrt: true);

        Umschalten(cut, true);

        Assert.False(cut.Instance.FeldwerteStehen);
    }

    // =====================================================================
    //  (4) Was gesendet wird
    // =====================================================================

    [Fact]
    public void Ohne_Einwilligung_wird_KEIN_Feldwert_mitgeschickt()
    {
        var mit = new List<bool>();

        IRenderedComponent<KiChatDialog> cut = Chat(
            felder: () => Block(),
            einwilligen: () => Task.FromResult(false),
            fragen: (_, _, feldwerte) =>
            {
                mit.Add(feldwerte);
                return Task.FromResult<IReadOnlyList<Gespraechszeile>>(Array.Empty<Gespraechszeile>());
            });

        Umschalten(cut, true);       // abgelehnt
        Fragen(cut, "Warum ist die Flotte arbeitslos?");

        Assert.Equal(new[] { false }, mit);
    }

    [Fact]
    public void Mit_Einwilligung_und_Schalter_gehen_sie_mit()
    {
        var mit = new List<bool>();

        IRenderedComponent<KiChatDialog> cut = Chat(
            felder: () => Block(),
            einwilligen: () => Task.FromResult(true),
            fragen: (_, _, feldwerte) =>
            {
                mit.Add(feldwerte);
                return Task.FromResult<IReadOnlyList<Gespraechszeile>>(Array.Empty<Gespraechszeile>());
            });

        Fragen(cut, "Erste Frage ohne Schalter");
        Umschalten(cut, true);
        Fragen(cut, "Zweite Frage mit Schalter");

        Assert.Equal(new[] { false, true }, mit);
    }

    [Fact]
    public void Faellt_die_Maske_weg_geht_trotz_stehendem_Schalter_nichts_mit()
    {
        // Der Schalter kann aus einem Dialog heraus angehakt worden sein, der inzwischen
        // geschlossen ist - das Chatfenster steht unter Windows nicht-modal daneben.
        KiDialogdaten? offen = Block();
        var mit = new List<bool>();

        IRenderedComponent<KiChatDialog> cut = Chat(
            felder: () => offen,
            einwilligen: () => Task.FromResult(true),
            fragen: (_, _, feldwerte) =>
            {
                mit.Add(feldwerte);
                return Task.FromResult<IReadOnlyList<Gespraechszeile>>(Array.Empty<Gespraechszeile>());
            });

        Umschalten(cut, true);
        offen = null;                                  // der Dialog ist zu
        cut.Render();

        Fragen(cut, "Und jetzt?");

        Assert.Equal(new[] { false }, mit);
        Assert.False(cut.Instance.FeldwerteMoeglich);
    }

    // =====================================================================
    //  (5) Die Vorschau zeigt die Werte
    // =====================================================================

    [Fact]
    public void Die_Vorschau_zeigt_den_Feldblock_WOERTLICH_wenn_der_Schalter_steht()
    {
        IRenderedComponent<KiChatDialog> cut = Chat(
            felder: () => Block(),
            einwilligen: () => Task.FromResult(true),
            vorschau: mitFeldwerten => Task.FromResult(
                "POST …\n" + (mitFeldwerten ? Block().Text : "")));

        Umschalten(cut, true);
        cut.FindAll("button").First(b => b.TextContent.Contains("Was wird gesendet?",
                                                               StringComparison.Ordinal)).Click();

        Assert.Contains("Pufferspeicher bearbeiten", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Gesamtvolumen [l]: 750", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Ohne_Schalter_zeigt_die_Vorschau_den_Block_NICHT()
    {
        // Vorschau und Wirklichkeit sind derselbe Weg mit demselben Schalterstand; eine
        // Vorschau, die mehr zeigt als hinausgeht, waere so falsch wie eine, die weniger
        // zeigt.
        IRenderedComponent<KiChatDialog> cut = Chat(
            felder: () => Block(),
            vorschau: mitFeldwerten => Task.FromResult(
                "POST …\n" + (mitFeldwerten ? Block().Text : "")));

        cut.FindAll("button").First(b => b.TextContent.Contains("Was wird gesendet?",
                                                               StringComparison.Ordinal)).Click();

        Assert.DoesNotContain("Gesamtvolumen", cut.Markup, StringComparison.Ordinal);
    }

    // =====================================================================
    //  Vorrichtung
    // =====================================================================

    private const string SCHALTER = "Feldwerte mitsenden";
    private const string GESPERRT = "Einwilligung nicht einholbar.";

    /// <summary>Ein Feldblock, wie ihn <c>KiMaskenbruecke.Dialogdaten</c> baut.</summary>
    private static KiDialogdaten Block() => new()
    {
        Maskenname = KiMaskennamen.PUFFERSPEICHER,
        Anzeigename = "Pufferspeicher bearbeiten",
        Feldzahl = 1,
        Text = "Werte der offenen Maske „Pufferspeicher bearbeiten“ (1 Felder):\n"
             + "- Gesamtvolumen [l]: 750\n"
    };

    private IRenderedComponent<KiChatDialog> Chat(
        Func<KiDialogdaten?>? felder = null,
        Func<Task<bool>>? einwilligen = null,
        bool gesperrt = false,
        bool hilfeBetrieb = false,
        Func<string, bool, bool, Task<IReadOnlyList<Gespraechszeile>>>? fragen = null,
        Func<bool, Task<string>>? vorschau = null)
        => Render<KiChatDialog>(p =>
        {
            p.Add(x => x.Texte, Texte());
            p.Add(x => x.HilfeBetrieb, hilfeBetrieb);
            p.Add(x => x.Eingerichtet, true);
            p.Add(x => x.FeldwerteGesperrt, gesperrt);
            if (felder is not null) p.Add(x => x.Feldwerte, felder);
            if (einwilligen is not null) p.Add(x => x.FeldwerteEinwilligen, einwilligen);
            if (fragen is not null) p.Add(x => x.Fragen, fragen);
            if (vorschau is not null) p.Add(x => x.Vorschau, vorschau);
        });

    /// <summary>Das Kästchen des Feldwerteschalters — das ZWEITE im Vorspann.</summary>
    private static AngleSharp.Dom.IElement Kaestchen(IRenderedComponent<KiChatDialog> cut)
        => cut.FindAll("input[type=checkbox]")[1];

    private static void Umschalten(IRenderedComponent<KiChatDialog> cut, bool an)
        => Kaestchen(cut).Change(an);

    private static void Fragen(IRenderedComponent<KiChatDialog> cut, string frage)
    {
        // Die Eingabezeile haengt an oninput, nicht an onchange: Enter sendet, und das
        // Feld muss den getippten Stand schon kennen, bevor der Knopf kommt.
        cut.Find("textarea").Input(frage);
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Fragen").Click();
    }

    private static IReadOnlyList<string> Texte(IRenderedComponent<KiChatDialog> cut)
        => cut.Instance.Zeilen.Select(z => z.Text).ToList();

    /// <summary>Nur die Texte, die diese Fälle lesen — der Rest bleibt leer.</summary>
    private static KiChatTexte Texte() => new()
    {
        Verlauf = "Hilfe-Assistent",
        VerlaufLeer = "Noch keine Frage gestellt.",
        Eingabe = "Ihre Frage",
        Fragen = "Fragen",
        Suchen = "Nur suchen",
        Aktionen = "Aktionen zulassen",
        Feldwerte = SCHALTER,
        FeldwerteEin = "Feldwerte gehen mit.",
        FeldwerteAus = "Feldwerte bleiben hier.",
        FeldwerteFehlt = "Ohne Einwilligung kein Feldwert.",
        FeldwerteGesperrt = GESPERRT,
        Vorschau = "Was wird gesendet?",
        VorschauTitel = "Sendevorschau",
        VorschauKopf = "Gesendet wird an gemini-2.5-flash-lite.",
        Schliessen = "Schließen",
        HinweisVorn = "Es werden nur Hilfetexte übertragen. "
    };
}
