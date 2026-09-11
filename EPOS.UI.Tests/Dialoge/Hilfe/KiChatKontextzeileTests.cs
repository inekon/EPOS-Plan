using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Hilfe;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Hilfe;

/// <summary>
/// Die KONTEXTZEILE und die VORBELEGTE FRAGE des Chats (Auftrag #199, Stufe S1).
///
/// <para>Was hier bewiesen wird: Der Chat sagt, WOHER er gerufen wurde — Bereich,
/// Dialogname und, wenn es eine gab, die Meldungskennung —, und er stellt die
/// vorbelegte Frage NICHT von selbst (Konzept 5: „kein Assistent ohne
/// Anwenderfrage").</para>
/// </summary>
public class KiChatKontextzeileTests : EposBunitContext
{
    public KiChatKontextzeileTests()
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static KiChatTexte Texte() => new()
    {
        Verlauf = "Hilfe-Assistent",
        VerlaufLeer = "Noch keine Frage gestellt.",
        KontextFormat = "Kontext: {0}",
        KontextLeer = "Kontext: (nicht erkannt)",
        DialognameFormat = "Dialog: {0}",
        MeldungFormat = "Meldung: {0}",
        Eingabe = "Ihre Frage",
        Fragen = "Fragen",
        Suchen = "Nur suchen",
        Aktionen = "Aktionen zulassen",
        Werkzeuge = "Werkzeuge...",
        HinweisVorn = "Es werden nur Hilfetexte übertragen. ",
        Doku = "Online-Dokumentation öffnen",
        Schliessen = "Schließen"
    };

    private IRenderedComponent<KiChatDialog> Zeigen(
        string kontext = "Bereich: Heizkessel",
        string dialogname = "",
        string kennung = "",
        string vorbelegung = "",
        Func<string, bool, bool, Task<IReadOnlyList<Gespraechszeile>>>? fragen = null)
        => Render<KiChatDialog>(p =>
        {
            p.Add(x => x.Texte, Texte())
             .Add(x => x.Kontext, kontext)
             .Add(x => x.Dialogname, dialogname)
             .Add(x => x.Kennung, kennung)
             .Add(x => x.Vorbelegung, vorbelegung)
             .Add(x => x.Eingerichtet, true)
             .Add(x => x.AnfragenHeute, 3)
             .Add(x => x.Tageslimit, 50);
            if (fragen is not null) p.Add(x => x.Fragen, fragen);
        });

    private static string Kontextzeile(IRenderedComponent<KiChatDialog> cut)
        => cut.Find(".epos-kichat-kontext").TextContent.Trim();

    // ==================================================================
    //  Die Kontextzeile
    // ==================================================================

    /// <summary>Ohne Dialogbezug bleibt es beim Bereich — der Menüweg ändert sich nicht.</summary>
    [Fact]
    public void Ohne_Dialog_nennt_die_Zeile_nur_den_Bereich()
    {
        var cut = Zeigen();

        Assert.Equal("Kontext: Bereich: Heizkessel", Kontextzeile(cut));
    }

    [Fact]
    public void Mit_Dialog_nennt_die_Zeile_den_Dialognamen()
    {
        var cut = Zeigen(dialogname: "Heizkessel bearbeiten");

        Assert.Equal("Kontext: Bereich: Heizkessel | Dialog: Heizkessel bearbeiten",
                     Kontextzeile(cut));
    }

    [Fact]
    public void Mit_Kennung_nennt_die_Zeile_auch_die_Meldung()
    {
        var cut = Zeigen(dialogname: "Stromspeicher-Auslegung",
                         kennung: KiMeldungskennung.FLOTTE_ARBEITSLOS);

        Assert.Equal("Kontext: Bereich: Heizkessel | Dialog: Stromspeicher-Auslegung"
                     + " | Meldung: " + KiMeldungskennung.FLOTTE_ARBEITSLOS,
                     Kontextzeile(cut));
    }

    /// <summary>Ohne erkannten Bereich bleibt der Ersatzsatz — samt Dialogname.</summary>
    [Fact]
    public void Ohne_Bereich_steht_der_Ersatzsatz()
    {
        var cut = Zeigen(kontext: "", dialogname: "Heizkessel bearbeiten");

        Assert.Equal("Kontext: (nicht erkannt) | Dialog: Heizkessel bearbeiten",
                     Kontextzeile(cut));
    }

    // ==================================================================
    //  Die vorbelegte Frage
    // ==================================================================

    [Fact]
    public void Die_vorbelegte_Frage_steht_im_Feld()
    {
        var cut = Zeigen(vorbelegung: "Warum hat die Flotte nichts getan?");

        Assert.Equal("Warum hat die Flotte nichts getan?",
                     cut.Find("textarea").GetAttribute("value"));
    }

    /// <summary>
    /// Und sie geht NICHT von selbst hinaus: Der Verlauf bleibt leer, und der
    /// Frageweg wurde nicht gerufen.
    /// </summary>
    [Fact]
    public void Die_vorbelegte_Frage_wird_nicht_abgeschickt()
    {
        var gefragt = new List<string>();
        var cut = Zeigen(vorbelegung: "Warum hat die Flotte nichts getan?",
                         fragen: (f, _, _) =>
                         {
                             gefragt.Add(f);
                             return Task.FromResult<IReadOnlyList<Gespraechszeile>>(
                                 Array.Empty<Gespraechszeile>());
                         });

        Assert.Empty(gefragt);
        Assert.Empty(cut.Instance.Zeilen);
        Assert.Contains("Noch keine Frage gestellt.", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>Erst der Klick schickt sie ab — unverändert, wie getippt.</summary>
    [Fact]
    public void Erst_der_Klick_schickt_sie_ab()
    {
        var gefragt = new List<string>();
        var cut = Zeigen(vorbelegung: "Warum hat die Flotte nichts getan?",
                         fragen: (f, _, _) =>
                         {
                             gefragt.Add(f);
                             return Task.FromResult<IReadOnlyList<Gespraechszeile>>(
                                 Array.Empty<Gespraechszeile>());
                         });

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Fragen").Click();

        Assert.Equal(new[] { "Warum hat die Flotte nichts getan?" }, gefragt);
    }

    /// <summary>Ohne Vorbelegung bleibt das Feld leer — der Regelfall.</summary>
    [Fact]
    public void Ohne_Vorbelegung_bleibt_das_Feld_leer()
    {
        var cut = Zeigen();

        Assert.Equal("", cut.Find("textarea").GetAttribute("value"));
    }
}

/// <summary>
/// Der HIN- und RÜCKWEG der <see cref="AppWurzel"/> zum Hilfe-Assistenten
/// (Auftrag #199, Muster #62b).
/// </summary>
/// <remarks>
/// Serielle Sammlung: Der Fall meldet einen Aufruf im Kern
/// (<c>KiChatKontext.AufrufMelden</c>) — prozessweiter Zustand.
/// </remarks>
[Collection("KiDialogweg")]
public class AppWurzelKiAssistentTests : EPOS.UI.Tests.Bausteine.KiDialogwegBasis
{
    private static readonly ProjektZeile[] ZweiProjekte =
    {
        new ProjektZeile(1030, "B3-Kaskade", "Region 12", "WP+BHKW"),
        new ProjektZeile(1007, "Speichervariante A", "Region 12", "WP+Speicher")
    };

    private static KiChatTexte Texte() => new()
    {
        Verlauf = "Hilfe-Assistent",
        VerlaufLeer = "Noch keine Frage gestellt.",
        KontextFormat = "Kontext: {0}",
        KontextLeer = "Kontext: (nicht erkannt)",
        DialognameFormat = "Dialog: {0}",
        MeldungFormat = "Meldung: {0}",
        Eingabe = "Ihre Frage",
        Fragen = "Fragen",
        Suchen = "Nur suchen",
        HinweisVorn = "Es werden nur Hilfetexte übertragen. ",
        Doku = "Online-Dokumentation öffnen",
        Schliessen = "Schließen"
    };

    private static Dictionary<string, object> ChatGaben() => new(StringComparer.Ordinal)
    {
        ["Texte"] = Texte(),
        ["Kontext"] = "",
        ["Eingerichtet"] = true,
        ["AnfragenHeute"] = 0,
        ["Tageslimit"] = 50
    };

    private IRenderedComponent<AppWurzel> Aufbauen(TestProjektquelle quelle)
    {
        Services.AddSingleton<IProjektQuelle>(quelle);
        return Render<AppWurzel>();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Navigationsziel.Aktuell = null;
        base.Dispose(disposing);
    }

    /// <summary>Der Kern öffnet den Assistenten, die Wurzel wechselt die Ansicht.</summary>
    [Fact]
    public void Der_Maskenschluessel_wechselt_auf_den_Assistenten()
    {
        var quelle = new TestProjektquelle(ZweiProjekte) { KiAssistent = ChatGaben() };
        var cut = Aufbauen(quelle);

        Assert.Empty(cut.FindAll(".epos-kichat"));

        cut.InvokeAsync(() => Navigationsziel.Aktuell!.OeffneMaske(
            Masken.KiAssistent,
            KiAufrufkontext.AusHilfeschluessel("Form_Heizkessel.btn_Help", "Heizkessel bearbeiten")));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-kichat")));

        // Der Aufrufkontext legt sich UEBER den Parametersatz der Quelle.
        string zeile = cut.Find(".epos-kichat-kontext").TextContent;
        Assert.Contains(KiChatKontext.B_HEIZKESSEL, zeile, StringComparison.Ordinal);
        Assert.Contains("Dialog: Heizkessel bearbeiten", zeile, StringComparison.Ordinal);
    }

    /// <summary>Die vorbelegte Frage kommt mit — und wird nicht abgeschickt.</summary>
    [Fact]
    public void Die_Kennung_und_die_Frage_kommen_mit()
    {
        var quelle = new TestProjektquelle(ZweiProjekte) { KiAssistent = ChatGaben() };
        var cut = Aufbauen(quelle);

        cut.InvokeAsync(() => Navigationsziel.Aktuell!.OeffneMaske(
            Masken.KiAssistent,
            KiAufrufkontext.AusHilfeschluessel("Form_SpeicherOptimierung.btn_Help",
                                               "Stromspeicher-Auslegung",
                                               "Warum hat die Flotte nichts getan?",
                                               KiMeldungskennung.FLOTTE_ARBEITSLOS)));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-kichat")));

        Assert.Contains("Meldung: " + KiMeldungskennung.FLOTTE_ARBEITSLOS,
                        cut.Find(".epos-kichat-kontext").TextContent, StringComparison.Ordinal);
        Assert.Equal("Warum hat die Flotte nichts getan?",
                     cut.Find("textarea").GetAttribute("value"));
    }

    /// <summary>
    /// Der RÜCKWEG führt dorthin, woher man kam (Muster #62b) — nicht stur auf die
    /// Startansicht.
    /// </summary>
    [Fact]
    public void Der_Rueckweg_fuehrt_in_die_vorherige_Ansicht()
    {
        var quelle = new TestProjektquelle(ZweiProjekte) { KiAssistent = ChatGaben() };
        var cut = Aufbauen(quelle);

        // Erst in den Energietraeger-Dialog, von dort in den Assistenten.
        cut.FindAll(".epos-projekt-energie")[0].Click();
        Assert.Single(cut.FindAll(".epos-dialog"));

        cut.InvokeAsync(() => Navigationsziel.Aktuell!.OeffneMaske(
            Masken.KiAssistent, KiAufrufkontext.AusHilfeschluessel("Form_Kosten_Auswahl.btn_Help")));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-kichat")));

        // „Schliessen" des Chats fuehrt ZURUECK in den Dialog.
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Schließen").Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".epos-kichat")));
        Assert.Single(cut.FindAll(".epos-dialog"));
    }

    /// <summary>Der Rückweg meldet den Aufruf im Kern ab.</summary>
    [Fact]
    public void Der_Rueckweg_meldet_den_Aufruf_ab()
    {
        var quelle = new TestProjektquelle(ZweiProjekte) { KiAssistent = ChatGaben() };
        var cut = Aufbauen(quelle);

        cut.InvokeAsync(() => Navigationsziel.Aktuell!.OeffneMaske(
            Masken.KiAssistent, KiAufrufkontext.AusHilfeschluessel("Form_PV.btn_Help")));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-kichat")));
        Assert.NotNull(KiChatKontext.Aufruf);

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Schließen").Click();

        cut.WaitForAssertion(() => Assert.Null(KiChatKontext.Aufruf));
    }

    /// <summary>
    /// Ohne Parametersatz bleibt die Liste stehen und sagt warum — der Zustand der
    /// iOS-Hülle vor iU11, unverändert.
    /// </summary>
    [Fact]
    public void Ohne_Gaben_bleibt_die_Liste_stehen()
    {
        var cut = Aufbauen(new TestProjektquelle(ZweiProjekte));

        cut.InvokeAsync(() => Navigationsziel.Aktuell!.OeffneMaske(
            Masken.KiAssistent, KiAufrufkontext.AusHilfeschluessel("Form_PV.btn_Help")));

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".epos-kichat")));
        Assert.Contains("steht auf diesem Gerät noch nicht zur Verfügung",
                        cut.Markup, StringComparison.Ordinal);
    }
}
