using System.Globalization;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Admin;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Katalog-Dublettensuche (iU9-W14c.5).
///
/// <para><b>Warum dieser Feldbestandstest zählt</b> (R-W14c-10): Der Vorläufer
/// <c>Form_KatalogDubletten</c> hatte KEINEN Designer, keine Feldkarte, keinen
/// Eintrag im Erreichbarkeitsbefund und keine Zeile im Stapellauf (Befund
/// W14c-B61) — im Vollständigkeitsnetz war er unsichtbar. Die Sollangaben hier
/// stammen aus der von Hand geschriebenen Feldkarte
/// (<c>BaueOberflaeche:70–185</c>): Katalogwahl, Prüfknopf, Statuszeile, Baum,
/// Detailfeld, vier Aktionsknöpfe und das Protokollfeld — dazu der neue
/// Schließen-Knopf (A-14).</para>
///
/// <para>Die Kultur ist auf de-DE gepinnt (Regel seit W8).</para>
/// </summary>
public class KatalogDublettenDialogTests : BunitContext
{
    private static readonly (string, string)[] KATALOGE =
    {
        ("WP", "Wärmepumpen"),
        ("PV", "Photovoltaik")
    };

    /// <summary>
    /// Der Prüfbaum: zwei Kataloge, im ersten eine NAMENS- und eine INHALTSgruppe mit
    /// je zwei Sätzen — vier Ebenen, wie der Dublettenbefund sie führt.
    /// </summary>
    private static IReadOnlyList<DublettenKnoten> Baum() => new[]
    {
        new DublettenKnoten("K:WP", "Wärmepumpen (51 Sätze)", DublettenKnotenArt.Wurzel, "WP", new[]
        {
            new DublettenKnoten("K:WP/N", "Namensdubletten (1 Gruppen)", DublettenKnotenArt.Ast, "WP", new[]
            {
                new DublettenKnoten("K:WP/N/0", "Vaillant VKK 476", DublettenKnotenArt.Gruppe, "WP", new[]
                {
                    new DublettenKnoten("K:WP/N/0/7", "ID 7 — Vaillant VKK 476",
                        DublettenKnotenArt.Blatt, "WP", Array.Empty<DublettenKnoten>(), false,
                        "", 0, true, 7, false),
                    new DublettenKnoten("K:WP/N/0/9", "ID 9 — vaillant vkk 476",
                        DublettenKnotenArt.Blatt, "WP", Array.Empty<DublettenKnoten>(), false,
                        "[Auslieferung]", 0, true, 9, true)
                }, false, "", 0, true)
            }, true),

            new DublettenKnoten("K:WP/I", "Inhaltsdubletten (1 Gruppen)", DublettenKnotenArt.Ast, "WP", new[]
            {
                new DublettenKnoten("K:WP/I/0", "gleicher Inhalt: A / B", DublettenKnotenArt.Gruppe, "WP", new[]
                {
                    new DublettenKnoten("K:WP/I/0/3", "ID 3 — A", DublettenKnotenArt.Blatt, "WP",
                        Array.Empty<DublettenKnoten>(), false, "", 0, false, 3, false),
                    new DublettenKnoten("K:WP/I/0/4", "ID 4 — B", DublettenKnotenArt.Blatt, "WP",
                        Array.Empty<DublettenKnoten>(), false, "", 0, false, 4, false)
                }, false, "", 0, false)
            }, true)
        }, true),

        new DublettenKnoten("K:PV", "Photovoltaik (6 Sätze)", DublettenKnotenArt.Wurzel, "PV",
                            Array.Empty<DublettenKnoten>(), true)
    };

    public KatalogDublettenDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Die Sprache der Oberfläche wird auf de-DE gepinnt (Regel seit W8).</summary>
    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
    }

    private IRenderedComponent<KatalogDublettenDialog> Zeige(
        Func<string, IProgress<Scanmeldung>, Task<Scanergebnis>>? scannen = null,
        Func<string, Task<string>>? detailtext = null,
        Func<string, Task<int>>? umfang = null,
        Func<string, Task<Aktionsergebnis>>? bereinigen = null,
        Func<string, Task<Loeschpruefung>>? loeschPruefen = null,
        Func<string, Task<Aktionsergebnis>>? loeschen = null,
        Func<string, Task<Umbenennung>>? umbenennenVorbereiten = null,
        Func<string, string, string?>? namePruefen = null,
        Func<string, string, Task<Aktionsergebnis>>? umbenennen = null,
        Func<IReadOnlyList<string>, Task<string>>? protokollSpeichern = null,
        Action<bool>? geschlossen = null)
    {
        return Render<KatalogDublettenDialog>(p => p
            .Add(x => x.Kataloge, KATALOGE.ToList())
            .Add(x => x.Scannen, scannen ?? ((_, _) =>
                Task.FromResult(new Scanergebnis(Baum(), "", Array.Empty<string>()))))
            .Add(x => x.Detailtext, detailtext ?? (s => Task.FromResult("Bezeichner = " + s)))
            .Add(x => x.Bereinigungsumfang, umfang ?? (_ => Task.FromResult(1)))
            .Add(x => x.Bereinigen, bereinigen ??
                (_ => Task.FromResult(new Aktionsergebnis(true, new[] { "bereinigt" }))))
            .Add(x => x.LoeschPruefen, loeschPruefen ??
                (_ => Task.FromResult(new Loeschpruefung(false, false, "", "", "Vaillant VKK 476", 7))))
            .Add(x => x.Loeschen, loeschen ??
                (_ => Task.FromResult(new Aktionsergebnis(true, new[] { "geloescht" }))))
            .Add(x => x.UmbenennenVorbereiten, umbenennenVorbereiten ??
                (_ => Task.FromResult(new Umbenennung(false, "Vaillant VKK 476"))))
            .Add(x => x.NamePruefen, namePruefen ?? ((_, _) => (string?)null))
            .Add(x => x.Umbenennen, umbenennen ??
                ((_, _) => Task.FromResult(new Aktionsergebnis(true, new[] { "umbenannt" }))))
            .Add(x => x.ProtokollSpeichern, protokollSpeichern ?? (_ => Task.FromResult("gespeichert")))
            .Add(x => x.Geschlossen, geschlossen ?? (_ => { })));
    }

    private static IRenderedComponent<KatalogDublettenDialog> Gescannt(
        IRenderedComponent<KatalogDublettenDialog> cut)
    {
        cut.FindAll(".epos-kontextleiste button")[0].Click();
        return cut;
    }

    // =====================================================================
    //  Feldbestand (Feldkarte von Hand, BaueOberflaeche:70-185)
    // =====================================================================

    [Fact]
    public void Der_Dialog_zeigt_die_zehn_Bausteine_der_Handkarte()
    {
        var cut = Zeige();

        // _cbKatalog: "(alle Kataloge)" plus die 19 - hier zwei.
        var eintraege = cut.FindAll("select option").Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(3, eintraege.Count);
        Assert.Equal("(alle Kataloge)", eintraege[0]);
        Assert.Equal("Wärmepumpen", eintraege[1]);

        // _btnPruefen und _lblStatus
        Assert.Equal("Prüfen", cut.FindAll(".epos-kontextleiste button")[0].TextContent.Trim());
        Assert.Single(cut.FindAll(".epos-kontextleiste .epos-status"));

        // _tbDetails und _tbProtokoll: zwei mehrzeilige, nur lesende Festbreitenfelder.
        var felder = cut.FindAll("textarea");
        Assert.Equal(2, felder.Count);
        Assert.All(felder, f => Assert.True(f.HasAttribute("readonly")));

        // Die vier Aktionsknoepfe plus der NEUE Schliessen-Knopf (A-14).
        var knoepfe = cut.FindAll(".epos-leiste button").Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(5, knoepfe.Count);
        Assert.StartsWith("Leere Kopien bereinigen", knoepfe[0]);
        Assert.StartsWith("Satz löschen", knoepfe[1]);
        Assert.StartsWith("Satz umbenennen", knoepfe[2]);
        Assert.StartsWith("Protokoll speichern", knoepfe[3]);
        Assert.Equal("Schließen", knoepfe[4]);
    }

    /// <summary>
    /// Vor dem Scan steht kein Baum, sondern der Leertext — und drei der vier
    /// Aktionsknöpfe sind gesperrt.
    /// </summary>
    [Fact]
    public void Vor_dem_Scan_steht_der_Leertext_und_die_Knoepfe_sind_gesperrt()
    {
        var cut = Zeige();

        Assert.Empty(cut.FindAll("ul[role=tree]"));
        Assert.Single(cut.FindAll("p.epos-baum-leer"));

        var knoepfe = cut.FindAll(".epos-leiste button");
        Assert.True(knoepfe[0].HasAttribute("disabled"));    // Bereinigen
        Assert.True(knoepfe[1].HasAttribute("disabled"));    // Loeschen
        Assert.True(knoepfe[2].HasAttribute("disabled"));    // Umbenennen
        Assert.False(knoepfe[3].HasAttribute("disabled"));   // Protokoll - immer aktiv
        Assert.False(knoepfe[4].HasAttribute("disabled"));   // Schliessen
    }

    // =====================================================================
    //  Der Scan (A-15) und der vierstufige Baum
    // =====================================================================

    [Fact]
    public void Der_Scan_baut_den_vierstufigen_Baum_mit_zwei_Katalogen()
    {
        string? gefragt = null;
        var cut = Zeige(scannen: (s, _) =>
        {
            gefragt = s;
            return Task.FromResult(new Scanergebnis(Baum(), "", Array.Empty<string>()));
        });

        Gescannt(cut);

        Assert.Equal("", gefragt);                            // "(alle Kataloge)"
        Assert.Single(cut.FindAll("ul[role=tree]"));

        // Wurzel und Ast sind offen, die Gruppen zu: 2 Wurzeln + 2 Aeste + 2 Gruppen.
        Assert.Equal(6, cut.FindAll("li[role=treeitem]").Count);
        Assert.Equal("1", cut.Find("li[data-schluessel=\"K:WP\"]").GetAttribute("aria-level"));
        Assert.Equal("2", cut.Find("li[data-schluessel=\"K:WP/N\"]").GetAttribute("aria-level"));
        Assert.Equal("3", cut.Find("li[data-schluessel=\"K:WP/N/0\"]").GetAttribute("aria-level"));

        // Die vierte Ebene erscheint beim Aufklappen der Gruppe.
        cut.Find("li[data-schluessel=\"K:WP/N/0\"]")
           .QuerySelector("button.epos-baum-schalter")!.Click();
        Assert.Equal("4", cut.Find("li[data-schluessel=\"K:WP/N/0/7\"]").GetAttribute("aria-level"));
        Assert.Equal("[Auslieferung]",
            cut.Find("li[data-schluessel=\"K:WP/N/0/9\"]")
               .QuerySelector("span.epos-baum-kennzeichen")!.TextContent.Trim());
    }

    [Fact]
    public void Ein_einzelner_Katalog_wird_namentlich_gescannt()
    {
        string? gefragt = null;
        var cut = Zeige(scannen: (s, _) =>
        {
            gefragt = s;
            return Task.FromResult(new Scanergebnis(Baum(), "", Array.Empty<string>()));
        });

        cut.Find("select").Change("1");                       // "Wärmepumpen"
        Assert.Equal("WP", cut.Instance.GewaehlterSchluessel);

        Gescannt(cut);
        Assert.Equal("WP", gefragt);
    }

    [Fact]
    public void Die_Statuszeile_meldet_wenn_es_nichts_zu_melden_gibt()
    {
        var cut = Zeige(scannen: (_, _) => Task.FromResult(
            new Scanergebnis(Array.Empty<DublettenKnoten>(), "Keine Dubletten gefunden.",
                             Array.Empty<string>())));

        Gescannt(cut);

        Assert.Equal("Keine Dubletten gefunden.", cut.Instance.Status);
    }

    /// <summary>Ein Lesefehler des Scans landet im Sitzungsprotokoll.</summary>
    [Fact]
    public void Ein_Lesefehler_landet_im_Protokoll()
    {
        var cut = Zeige(scannen: (_, _) => Task.FromResult(
            new Scanergebnis(Baum(), "", new[] { "Tab_WP_STAMM: Tabelle fehlt" })));

        Gescannt(cut);

        Assert.Contains("Tab_WP_STAMM: Tabelle fehlt", cut.Instance.Protokollzeilen);
        Assert.Contains("Tab_WP_STAMM: Tabelle fehlt", cut.FindAll("textarea")[1].TextContent);
    }

    // =====================================================================
    //  Die drei Knopfzustaende (tree_AfterSelect:432-439)
    // =====================================================================

    [Fact]
    public void Die_Knopfzustaende_folgen_der_Auswahl()
    {
        var cut = Zeige();
        Gescannt(cut);

        // Ohne Auswahl: "Bereinigen" ist frei, weil GENAU EIN Katalog gescannt
        // waere - hier sind es zwei, also gesperrt.
        var knoepfe = cut.FindAll(".epos-leiste button");
        Assert.True(knoepfe[0].HasAttribute("disabled"));
        Assert.True(knoepfe[1].HasAttribute("disabled"));
        Assert.True(knoepfe[2].HasAttribute("disabled"));

        // Ein GRUPPENknoten: Bereinigen frei, Loeschen und Umbenennen gesperrt.
        cut.Find("li[data-schluessel=\"K:WP/N/0\"]").QuerySelector("span.epos-baum-text")!.Click();
        knoepfe = cut.FindAll(".epos-leiste button");
        Assert.False(knoepfe[0].HasAttribute("disabled"));
        Assert.True(knoepfe[1].HasAttribute("disabled"));
        Assert.True(knoepfe[2].HasAttribute("disabled"));

        // Ein SATZ: alle drei frei.
        cut.Find("li[data-schluessel=\"K:WP/N/0\"]")
           .QuerySelector("button.epos-baum-schalter")!.Click();
        cut.Find("li[data-schluessel=\"K:WP/N/0/7\"]").QuerySelector("span.epos-baum-text")!.Click();
        knoepfe = cut.FindAll(".epos-leiste button");
        Assert.False(knoepfe[0].HasAttribute("disabled"));
        Assert.False(knoepfe[1].HasAttribute("disabled"));
        Assert.False(knoepfe[2].HasAttribute("disabled"));
    }

    [Fact]
    public void Die_Auswahl_zeigt_den_Detailtext()
    {
        var cut = Zeige(detailtext: s => Task.FromResult("Bezeichner = " + s));
        Gescannt(cut);

        cut.Find("li[data-schluessel=\"K:WP/N/0\"]").QuerySelector("span.epos-baum-text")!.Click();

        Assert.Equal("Bezeichner = K:WP/N/0", cut.Instance.Detailinhalt);
        Assert.Contains("Bezeichner = K:WP/N/0", cut.FindAll("textarea")[0].TextContent);
    }

    // =====================================================================
    //  Bereinigen
    // =====================================================================

    [Fact]
    public void Ohne_Gruppen_meldet_Bereinigen_statt_zu_fragen()
    {
        var cut = Zeige(umfang: _ => Task.FromResult(0));
        Gescannt(cut);
        cut.Find("li[data-schluessel=\"K:WP\"]").QuerySelector("span.epos-baum-text")!.Click();

        cut.FindAll(".epos-leiste button")[0].Click();

        Assert.Equal("Keine Dubletten gefunden.", cut.Instance.Meldung);
        Assert.All(cut.FindComponents<Rueckfrage>(), f => Assert.False(f.Instance.Offen));
    }

    [Fact]
    public void Bereinigen_fragt_mit_der_Gruppenzahl_und_betont_Nein()
    {
        int bereinigt = 0;
        var cut = Zeige(umfang: _ => Task.FromResult(3),
                        bereinigen: _ => { bereinigt++; return Task.FromResult(
                            new Aktionsergebnis(true, new[] { "3 Gruppen bereinigt" })); });
        Gescannt(cut);
        cut.Find("li[data-schluessel=\"K:WP\"]").QuerySelector("span.epos-baum-text")!.Click();

        cut.FindAll(".epos-leiste button")[0].Click();

        var frage = cut.FindComponents<Rueckfrage>().First(f => f.Instance.Offen);
        Assert.Contains("3", frage.Instance.Frage);
        Assert.True(frage.Instance.VorgabeNein);              // A-1

        frage.FindAll("button").First(b => b.TextContent.Trim() == "Nein").Click();
        Assert.Equal(0, bereinigt);

        cut.FindAll(".epos-leiste button")[0].Click();
        cut.FindComponents<Rueckfrage>().First(f => f.Instance.Offen)
           .FindAll("button").First(b => b.TextContent.Trim() == "Ja").Click();

        Assert.Equal(1, bereinigt);
        Assert.Contains("3 Gruppen bereinigt", cut.Instance.Protokollzeilen);
    }

    // =====================================================================
    //  Loeschen - die DREI Schranken hintereinander
    // =====================================================================

    private void SatzWaehlen(IRenderedComponent<KatalogDublettenDialog> cut, string schluessel)
    {
        cut.Find("li[data-schluessel=\"K:WP/N/0\"]")
           .QuerySelector("button.epos-baum-schalter")!.Click();
        cut.Find("li[data-schluessel=\"" + schluessel + "\"]")
           .QuerySelector("span.epos-baum-text")!.Click();
    }

    [Fact]
    public void Schranke_1_Ein_Auslieferungssatz_bleibt_stehen()
    {
        int geloescht = 0;
        var cut = Zeige(
            loeschPruefen: _ => Task.FromResult(new Loeschpruefung(true, false, "", "", "x", 9)),
            loeschen: _ => { geloescht++; return Task.FromResult(Aktionsergebnis.Nichts()); });
        Gescannt(cut);
        SatzWaehlen(cut, "K:WP/N/0/9");

        cut.FindAll(".epos-leiste button")[1].Click();

        Assert.Contains("Auslieferungssätze", cut.Instance.Meldung);
        Assert.Equal(0, geloescht);
        Assert.All(cut.FindComponents<Rueckfrage>(), f => Assert.False(f.Instance.Offen));
    }

    /// <summary>
    /// Befund W14c-B44: Ein FEHLSCHLAG der Verwendungsprüfung ist nicht „nicht
    /// verwendet" — er meldet sich und hält an.
    /// </summary>
    [Fact]
    public void Eine_gescheiterte_Verwendungspruefung_haelt_an()
    {
        int geloescht = 0;
        var cut = Zeige(
            loeschPruefen: _ => Task.FromResult(
                new Loeschpruefung(false, false, "", "Tabelle Tab_X fehlt", "A", 7)),
            loeschen: _ => { geloescht++; return Task.FromResult(Aktionsergebnis.Nichts()); });
        Gescannt(cut);
        SatzWaehlen(cut, "K:WP/N/0/7");

        cut.FindAll(".epos-leiste button")[1].Click();

        Assert.Contains("Tab_X", cut.Instance.Meldung);
        Assert.Equal(0, geloescht);
        Assert.All(cut.FindComponents<Rueckfrage>(), f => Assert.False(f.Instance.Offen));
    }

    /// <summary>
    /// Schranke 2 MELDET und lässt weiterlaufen — wörtlich wie der Vorläufer: „bitte
    /// nur löschen, wenn der Satz sicher nicht verwendet wird."
    /// </summary>
    [Fact]
    public void Schranke_2_Ohne_Verwendungspruefung_wird_gemeldet_und_weitergemacht()
    {
        var cut = Zeige(loeschPruefen: _ => Task.FromResult(
            new Loeschpruefung(false, true, "", "", "A", 7)));
        Gescannt(cut);
        SatzWaehlen(cut, "K:WP/N/0/7");

        cut.FindAll(".epos-leiste button")[1].Click();

        Assert.Contains("keine Verwendungsprüfung", cut.Instance.Meldung);
        Assert.Single(cut.FindComponents<Rueckfrage>().Where(f => f.Instance.Offen));
    }

    [Fact]
    public void Schranke_3_Erst_die_Verwendungsfrage_dann_die_Endbestaetigung()
    {
        var geloescht = new List<string>();
        var cut = Zeige(
            loeschPruefen: _ => Task.FromResult(
                new Loeschpruefung(false, false, "Tab_WErzeuger (2)", "", "Vaillant VKK 476", 7)),
            loeschen: s => { geloescht.Add(s); return Task.FromResult(
                new Aktionsergebnis(true, new[] { "geloescht." })); });
        Gescannt(cut);
        SatzWaehlen(cut, "K:WP/N/0/7");

        cut.FindAll(".epos-leiste button")[1].Click();

        // Frage 1: der Satz wird verwendet.
        var frage = cut.FindComponents<Rueckfrage>().First(f => f.Instance.Offen);
        Assert.Contains("Tab_WErzeuger (2)", frage.Instance.Frage);
        frage.FindAll("button").First(b => b.TextContent.Trim() == "Ja").Click();

        // Frage 2: die Endbestaetigung mit Name UND Id.
        frage = cut.FindComponents<Rueckfrage>().First(f => f.Instance.Offen);
        Assert.Contains("Vaillant VKK 476", frage.Instance.Frage);
        Assert.Contains("7", frage.Instance.Frage);
        frage.FindAll("button").First(b => b.TextContent.Trim() == "Ja").Click();

        Assert.Equal(new[] { "K:WP/N/0/7" }, geloescht);
        Assert.Contains("geloescht.", cut.Instance.Protokollzeilen);
    }

    [Fact]
    public void Nein_bei_der_Verwendungsfrage_loescht_nicht()
    {
        int geloescht = 0;
        var cut = Zeige(
            loeschPruefen: _ => Task.FromResult(
                new Loeschpruefung(false, false, "Tab_WErzeuger (2)", "", "A", 7)),
            loeschen: _ => { geloescht++; return Task.FromResult(Aktionsergebnis.Nichts()); });
        Gescannt(cut);
        SatzWaehlen(cut, "K:WP/N/0/7");

        cut.FindAll(".epos-leiste button")[1].Click();
        cut.FindComponents<Rueckfrage>().First(f => f.Instance.Offen)
           .FindAll("button").First(b => b.TextContent.Trim() == "Nein").Click();

        Assert.Equal(0, geloescht);
        Assert.All(cut.FindComponents<Rueckfrage>(), f => Assert.False(f.Instance.Offen));
    }

    // =====================================================================
    //  Umbenennen - der NamensDialog MIT Pruefung (Befund W14c-B46)
    // =====================================================================

    [Fact]
    public void Umbenennen_oeffnet_den_Namensdialog_mit_dem_heutigen_Namen()
    {
        var cut = Zeige();
        Gescannt(cut);
        SatzWaehlen(cut, "K:WP/N/0/7");

        cut.FindAll(".epos-leiste button")[2].Click();

        var namensdialog = cut.FindComponent<EPOS.UI.Dialoge.Allgemein.NamensDialog>();
        Assert.Equal("Vaillant VKK 476", namensdialog.Instance.Name);
    }

    [Fact]
    public void Ein_Auslieferungssatz_wird_nicht_umbenannt()
    {
        var cut = Zeige(umbenennenVorbereiten: _ => Task.FromResult(new Umbenennung(true, "x")));
        Gescannt(cut);
        SatzWaehlen(cut, "K:WP/N/0/9");

        cut.FindAll(".epos-leiste button")[2].Click();

        Assert.Contains("Auslieferungssätze", cut.Instance.Meldung);
        Assert.Empty(cut.FindComponents<EPOS.UI.Dialoge.Allgemein.NamensDialog>());
    }

    /// <summary>
    /// Die Prüfung, die der Baustein bis W14c nicht kannte: „normalisiert schon
    /// vergeben" hält den Dialog OFFEN und sagt, warum.
    /// </summary>
    [Fact]
    public void Ein_vergebener_Name_haelt_den_Namensdialog_offen()
    {
        int umbenannt = 0;
        var cut = Zeige(
            namePruefen: (_, name) => name == "belegt" ? "Der Name ist bereits vergeben und damit ungültig." : null,
            umbenennen: (_, _) => { umbenannt++; return Task.FromResult(Aktionsergebnis.Nichts()); });
        Gescannt(cut);
        SatzWaehlen(cut, "K:WP/N/0/7");
        cut.FindAll(".epos-leiste button")[2].Click();

        var namensdialog = cut.FindComponent<EPOS.UI.Dialoge.Allgemein.NamensDialog>();
        namensdialog.Find("input").Input("belegt");
        namensdialog.FindAll("button").First(b => b.TextContent.Trim() == "Übernehmen").Click();

        Assert.Equal(0, umbenannt);
        Assert.Single(cut.FindComponents<EPOS.UI.Dialoge.Allgemein.NamensDialog>());
        Assert.Contains("ungültig", namensdialog.Markup);
    }

    [Fact]
    public void Ein_freier_Name_wird_geschrieben()
    {
        var geschrieben = new List<string>();
        var cut = Zeige(umbenennen: (_, name) =>
        {
            geschrieben.Add(name);
            return Task.FromResult(new Aktionsergebnis(true, new[] { "umbenannt" }));
        });
        Gescannt(cut);
        SatzWaehlen(cut, "K:WP/N/0/7");
        cut.FindAll(".epos-leiste button")[2].Click();

        var namensdialog = cut.FindComponent<EPOS.UI.Dialoge.Allgemein.NamensDialog>();
        namensdialog.Find("input").Input("Vaillant VKK 476 (alt)");
        namensdialog.FindAll("button").First(b => b.TextContent.Trim() == "Übernehmen").Click();

        Assert.Equal(new[] { "Vaillant VKK 476 (alt)" }, geschrieben);
        Assert.Contains("umbenannt", cut.Instance.Protokollzeilen);
    }

    // =====================================================================
    //  Protokoll (Befund W14c-B47) und Schluss
    // =====================================================================

    [Fact]
    public void Ein_leeres_Protokoll_meldet_sich_statt_still_zurueckzukehren()
    {
        int gespeichert = 0;
        var cut = Zeige(protokollSpeichern: _ => { gespeichert++; return Task.FromResult("x"); });

        cut.FindAll(".epos-leiste button")[3].Click();

        Assert.Equal(0, gespeichert);
        Assert.Contains("Protokoll ist leer", cut.Instance.Meldung);
    }

    [Fact]
    public void Ein_gefuelltes_Protokoll_wird_gespeichert()
    {
        IReadOnlyList<string>? uebergeben = null;
        var cut = Zeige(
            scannen: (_, _) => Task.FromResult(new Scanergebnis(Baum(), "", new[] { "Zeile 1" })),
            protokollSpeichern: z => { uebergeben = z; return Task.FromResult("gespeichert"); });
        Gescannt(cut);

        cut.FindAll(".epos-leiste button")[3].Click();

        Assert.NotNull(uebergeben);
        Assert.Contains("Zeile 1", uebergeben!);
        Assert.Equal("gespeichert", cut.Instance.Meldung);
    }

    /// <summary>„Schließen" liefert OK (A-14 — der Vorläufer hatte keinen Knopf).</summary>
    [Fact]
    public void Schliessen_liefert_OK()
    {
        bool? antwort = null;
        var cut = Zeige(geschlossen: b => antwort = b);

        cut.FindAll(".epos-leiste button")[4].Click();

        Assert.True(antwort);
    }

    [Fact]
    public void Esc_schliesst_nur_wenn_keine_Ebene_offen_ist()
    {
        bool? antwort = null;
        var cut = Zeige(geschlossen: b => antwort = b);
        Gescannt(cut);
        SatzWaehlen(cut, "K:WP/N/0/7");

        cut.FindAll(".epos-leiste button")[2].Click();        // Umbenennen steht
        cut.Find("div.epos-katalogdubletten").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(antwort);

        cut.FindComponent<EPOS.UI.Dialoge.Allgemein.NamensDialog>()
           .FindAll("button").First(b => b.TextContent.Trim() == "Abbrechen").Click();
        cut.Find("div.epos-katalogdubletten").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(antwort);
    }
}
