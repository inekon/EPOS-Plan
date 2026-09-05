using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Dialoge.Import;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Katalogimport VDI 3805 (iU9-W13.1) — EINE Komponente, VIER Ausprägungen.
///
/// <para>Soll sind die Feldkarten von <c>Form_Heizkessel_einlesen</c> (17 Zeilen),
/// <c>Form_PufferSp_einlesen</c> (14), <c>Form_SolarKollektoren_einlesen</c> (11 + 11
/// im Gruppenrahmen) und <c>Form_WP_einlesen</c> (34). Der Abgleich läuft je
/// AUSPRÄGUNG, nicht je Komponente (Muster W8) — deshalb vier Feldbestandsfälle.</para>
///
/// <para>Die Kultur ist auf de-DE gepinnt: Die Erwartungswerte sind deutsche
/// Beschriftungen, und der Windows-Läufer läuft mit englischer Oberfläche.</para>
/// </summary>
public class KatalogImportDialogTests : BunitContext
{
    public KatalogImportDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>
    /// Die Sprache der Oberfläche wird auf de-DE gepinnt (Muster
    /// <c>DeutscheOberflaeche</c> aus <c>EPOS.Kern.Tests</c>) — Kultur UND
    /// Thread-Kultur, damit ein Lauf unter <c>LANG=en_US.UTF-8</c> dieselben
    /// deutschen Beschriftungen sieht.
    /// </summary>
    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
    }

    // =====================================================================
    // Prüfstand
    // =====================================================================

    /// <summary>Drei Zeilen mit steigendem Filterwert — genug für jede Filterprobe.</summary>
    private static List<KatalogZeile> DreiZeilen() => new()
    {
        Zeile("Kessel klein",  "Vaillant", 19.3),
        Zeile("Kessel mittel", "Vaillant", 84.1),
        Zeile("Kessel gross",  "Buderus", 250.0)
    };

    private static KatalogZeile Zeile(string name, string firma, double wert) =>
        new(name, firma, wert, new Dictionary<string, string>
        {
            { KatalogImportProfil.FeldName, name },
            { KatalogImportProfil.FeldFirma, firma },
            { "BAUART", "Brennwert-Kessel" },
            { "THLEISTUNG", wert.ToString(CultureInfo.InvariantCulture) },
            { "BRENNSTOFF", "Erdgas E" },
            { "WIRKUNGSGRAD", "87.4" },
            { "VERLUSTE", "0.030" },
            { "SPEICHERTYP", "Pufferspeicher" },
            { "VOLUMEN", "303" },
            { "BESCHREIBUNG", "Antireflexglas" },
            { "APERTUR", "2.35" },
            { "LEISTUNG", "0" },
            { "H0", "0.73" }, { "A1", "3.54" }, { "A2", "0.015" },
            { "KDIR", "0.98" }, { "KDIFF", "0" },
            { "TYP", "Sole-Wasser" }, { "AUFSTELLUNG", "innen" },
            { "ZUSATZHEIZUNG", "6" }, { "STUFEN", "0" },
            { "MAXVORLAUF", "75" }, { "KUEHLLEISTUNG", "" }
        });

    /// <summary>
    /// Baut den Dialog mit einer Ausprägung und optional schon gelesenen Zeilen.
    /// Der Lesevorgang läuft über den Dateiwähler, damit der echte Weg geprüft wird.
    /// </summary>
    private IRenderedComponent<KatalogImportDialog> Bauen(
        KatalogImportArt art,
        List<KatalogZeile>? zeilen = null,
        IReadOnlyList<PruefMeldung>? meldungen = null,
        Func<IReadOnlyList<int>, IReadOnlyDictionary<int, string>, Task<KatalogVorpruefung>>? vorpruefen = null,
        Func<int, List<KonfliktEntscheidung>, IReadOnlyDictionary<int, string>,
             IProgress<ImportFortschritt>, CancellationToken, Task<ImportBilanz>>? ausfuehren = null,
        EventCallback<bool>? geschlossen = null)
    {
        return Render<KatalogImportDialog>(p =>
        {
            p.Add(x => x.Art, art);
            p.Add(x => x.ProfilVorgabe, KatalogImportProfil.Finde(art, Texte.Zu));
            p.Add(x => x.DateiWaehlen, (Func<string, Task<string?>>)(_ => Task.FromResult<string?>("probe.vdi")));
            p.Add(x => x.Lesen, (Func<string, IProgress<ImportFortschritt>, CancellationToken,
                                      Task<KatalogLeseErgebnis>>)((_, __, ___) =>
                Task.FromResult(new KatalogLeseErgebnis(
                    zeilen ?? new List<KatalogZeile>(),
                    meldungen ?? Array.Empty<PruefMeldung>()))));
            p.Add(x => x.Vorpruefen, vorpruefen);
            p.Add(x => x.Ausfuehren, ausfuehren);
            p.Add(x => x.Sammelmeldung, (Func<ImportBilanz, string>)VdiAuswahlFilter.LadeMeldung);
            p.Add(x => x.Meldungstext, (Func<PruefMeldung, string>)Texte.Zu);
            p.Add(x => x.Fortschrittstext, (Func<ImportFortschritt, string>)Texte.Zu);
            if (geschlossen.HasValue) p.Add(x => x.Geschlossen, geschlossen.Value);
        });
    }

    /// <summary>Klickt „Durchsuchen…" und wartet, bis die Liste steht.</summary>
    private static void Einlesen(IRenderedComponent<KatalogImportDialog> cut)
    {
        cut.FindAll("button").First(b => b.TextContent.Contains("VDI 3805")).Click();
    }

    // =====================================================================
    // 1 — Feldbestand je Ausprägung
    // =====================================================================

    /// <summary>
    /// <b>Heizkessel</b> (Blatt 3, die Referenzausprägung): sieben Detailfelder,
    /// Titel und Filterbeschriftung aus dem Profil.
    /// </summary>
    [Fact]
    public void Heizkessel_zeigt_seine_sieben_Detailfelder()
    {
        var cut = Bauen(KatalogImportArt.Heizkessel);

        Assert.Equal("Heizkessel Einlesen", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal(7, cut.FindAll(".epos-katalogimport-details label").Count);

        string felder = cut.Find(".epos-katalogimport-details").TextContent;
        Assert.Contains("Name:", felder);
        Assert.Contains("Firma:", felder);
        Assert.Contains("Bauart:", felder);
        Assert.Contains("thermische Leistung: [kWth]", felder);
        Assert.Contains("Brennstoff:", felder);
        Assert.Contains("Wirkungsgrad: [%]", felder);
        Assert.Contains("Bereitschaftsverluste: [kW]", felder);

        Assert.Contains("Th. Leistung [kW] von:", cut.Find(".epos-katalogimport-filter").TextContent);
    }

    /// <summary><b>Pufferspeicher</b> (Blatt 20): fünf Detailfelder, Volumenfilter.</summary>
    [Fact]
    public void Pufferspeicher_zeigt_seine_fuenf_Detailfelder()
    {
        var cut = Bauen(KatalogImportArt.Pufferspeicher);

        Assert.Equal("Pufferspeicher Einlesen", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal(5, cut.FindAll(".epos-katalogimport-details label").Count);

        string felder = cut.Find(".epos-katalogimport-details").TextContent;
        Assert.Contains("Speichertyp:", felder);
        Assert.Contains("Gesamtvolumen: [l]", felder);
        Assert.Contains("Bereitschaftsverluste: [kWh/d]", felder);
        Assert.DoesNotContain("Brennstoff:", felder);

        Assert.Contains("Volumen [l] von:", cut.Find(".epos-katalogimport-filter").TextContent);
    }

    /// <summary>
    /// <b>Solarkollektoren</b> (Blatt 19): elf Detailfelder — zehn wie im
    /// Vorläufer, dazu die BESCHREIBUNG, die es im Designer gab und die
    /// <c>ZeigeDetails</c> nie befüllte (Befund W13-B25, Abweichung A-5).
    /// </summary>
    [Fact]
    public void Solarkollektoren_zeigen_elf_Detailfelder_samt_der_Beschreibung()
    {
        var cut = Bauen(KatalogImportArt.Solarkollektoren);

        Assert.Equal("Solarkollektoren Einlesen", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal(11, cut.FindAll(".epos-katalogimport-details label").Count);

        string felder = cut.Find(".epos-katalogimport-details").TextContent;
        Assert.Contains("Beschreibung:", felder);
        Assert.Contains("Aperturfläche: [m²]", felder);
        Assert.Contains("Spitzenleistung: [W/m²]", felder);
        Assert.Contains("Optischer Wirkungsgrad:", felder);
        Assert.Contains("Linearer Verlustkoeffizient: [W/(m²K)]", felder);
        Assert.Contains("Quadratischer Verlustkoeffizient: [W/(m²K)]", felder);
        Assert.Contains("Einfallswinkel-Korrekturfaktor für die Direktstrahlung:", felder);
        Assert.Contains("Korrekturfaktor für diffuse Strahlung:", felder);

        Assert.Contains("Aperturfläche [m²] von:", cut.Find(".epos-katalogimport-filter").TextContent);
    }

    /// <summary>
    /// <b>Wärmepumpe</b> (Blatt 22): zehn Detailfelder und als einzige der
    /// Hinweis „* 0=modulierend" unter den Feldern.
    /// </summary>
    [Fact]
    public void Waermepumpe_zeigt_zehn_Detailfelder_und_den_Stufenhinweis()
    {
        var cut = Bauen(KatalogImportArt.Waermepumpe);

        Assert.Equal("Wärmepumpen Einlesen", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal(10, cut.FindAll(".epos-katalogimport-details label").Count);

        string felder = cut.Find(".epos-katalogimport-details").TextContent;
        Assert.Contains("Aufstellung:", felder);
        Assert.Contains("elektrische Zuheizung: [kW]", felder);
        Assert.Contains("Stufen:", felder);
        Assert.Contains("max. Vorlauf:", felder);
        Assert.Contains("Kühlleistung: [kWcool]", felder);

        Assert.Contains("* 0=modulierend", cut.Markup);
    }

    /// <summary>Nur die Wärmepumpe trägt den Stufenhinweis.</summary>
    [Theory]
    [InlineData(KatalogImportArt.Heizkessel)]
    [InlineData(KatalogImportArt.Pufferspeicher)]
    [InlineData(KatalogImportArt.Solarkollektoren)]
    public void Die_drei_anderen_tragen_den_Stufenhinweis_nicht(KatalogImportArt art)
    {
        Assert.DoesNotContain("0=modulierend", Bauen(art).Markup);
    }

    // =====================================================================
    // 2 — Filtervorbelegung
    // =====================================================================

    /// <summary>
    /// Die Vorbelegung der beiden Filterfelder ist die des Designers und muss
    /// bitgleich bleiben — sie ist das, was der Anwender beim Öffnen sieht.
    /// </summary>
    [Theory]
    [InlineData(KatalogImportArt.Heizkessel, "10,0", "200,0")]
    [InlineData(KatalogImportArt.Pufferspeicher, "0", "1000")]
    [InlineData(KatalogImportArt.Solarkollektoren, "0,00", "5,00")]
    [InlineData(KatalogImportArt.Waermepumpe, "0", "100")]
    public void Die_Filtervorbelegung_ist_die_des_Designers(
        KatalogImportArt art, string von, string bis)
    {
        var cut = Bauen(art);
        var felder = cut.FindAll(".epos-katalogimport-filter input");

        Assert.Equal(von, felder[0].GetAttribute("value"));
        Assert.Equal(bis, felder[1].GetAttribute("value"));
    }

    // =====================================================================
    // 3 — Lesen, Filtern, Markieren
    // =====================================================================

    /// <summary>
    /// <b>Der Dateiwähler darf WARTEN</b> (Befund W13‑B‑1, Windows-Abnahme
    /// 05.09.2026).
    ///
    /// <para>Bis dahin gaben alle Hüllen ihren Wähler als
    /// <c>Task.FromResult(Dienste.Datei.DateiOeffnen(…))</c> herein — der
    /// <c>OpenFileDialog</c> ging also SYNCHRON im Blazor-Ereignis auf, mitten
    /// im <c>WebMessageReceived</c>-Rückruf der WebView2. Seither liefert
    /// <c>DateiOeffnenAsync</c> einen Task, der erst eine geposteten Nachricht
    /// später erfüllt wird.</para>
    ///
    /// <para>Der Fall hält fest, dass die Komponente das aushält: Solange der
    /// Wähler offen ist, steht die alte Liste; erst wenn er antwortet, läuft das
    /// Lesen an. Der Wähler wird hier von Hand aufgelöst — genau die Rolle, die
    /// am Gerät der Bedienfaden hinter dem Ereignis spielt.</para>
    /// </summary>
    [Fact]
    public async Task Der_Dateiwaehler_darf_warten_und_die_Liste_kommt_danach()
    {
        var waehler = new TaskCompletionSource<string?>();

        var cut = Render<KatalogImportDialog>(p =>
        {
            p.Add(x => x.Art, KatalogImportArt.Heizkessel);
            p.Add(x => x.ProfilVorgabe, KatalogImportProfil.Finde(KatalogImportArt.Heizkessel, Texte.Zu));
            p.Add(x => x.DateiWaehlen, (Func<string, Task<string?>>)(_ => waehler.Task));
            p.Add(x => x.Lesen, (Func<string, IProgress<ImportFortschritt>, CancellationToken,
                                      Task<KatalogLeseErgebnis>>)((_, __, ___) =>
                Task.FromResult(new KatalogLeseErgebnis(
                    DreiZeilen(), Array.Empty<PruefMeldung>()))));
            p.Add(x => x.Meldungstext, (Func<PruefMeldung, string>)Texte.Zu);
            p.Add(x => x.Fortschrittstext, (Func<ImportFortschritt, string>)Texte.Zu);
        });

        Einlesen(cut);

        // Der Waehler steht noch offen: nichts gelesen, nichts in der Liste.
        Assert.Equal(0, cut.Instance.SichtbareZeilen);

        // Jetzt antwortet er - so wie der Bedienfaden hinter dem Ereignis.
        await cut.InvokeAsync(() => waehler.SetResult("probe.vdi"));

        cut.WaitForAssertion(() => Assert.Equal(2, cut.Instance.SichtbareZeilen));
        Assert.Contains("Kessel klein", cut.Find("tbody").TextContent);
    }

    /// <summary>
    /// Ein ABGEBROCHENER Wähler (leerer Pfad) lässt alles, wie es war — kein
    /// Lesen, keine Meldung. Dieselbe Zusage wie im Baustein <c>Dateiwahl</c>,
    /// hier auf dem wartenden Weg.
    /// </summary>
    [Fact]
    public async Task Ein_abgebrochener_Waehler_liest_nichts()
    {
        var waehler = new TaskCompletionSource<string?>();
        bool gelesen = false;

        var cut = Render<KatalogImportDialog>(p =>
        {
            p.Add(x => x.Art, KatalogImportArt.Heizkessel);
            p.Add(x => x.ProfilVorgabe, KatalogImportProfil.Finde(KatalogImportArt.Heizkessel, Texte.Zu));
            p.Add(x => x.DateiWaehlen, (Func<string, Task<string?>>)(_ => waehler.Task));
            p.Add(x => x.Lesen, (Func<string, IProgress<ImportFortschritt>, CancellationToken,
                                      Task<KatalogLeseErgebnis>>)((_, __, ___) =>
            {
                gelesen = true;
                return Task.FromResult(new KatalogLeseErgebnis(
                    DreiZeilen(), Array.Empty<PruefMeldung>()));
            }));
            p.Add(x => x.Meldungstext, (Func<PruefMeldung, string>)Texte.Zu);
            p.Add(x => x.Fortschrittstext, (Func<ImportFortschritt, string>)Texte.Zu);
        });

        Einlesen(cut);
        await cut.InvokeAsync(() => waehler.SetResult(""));

        Assert.False(gelesen);
        Assert.Equal(0, cut.Instance.SichtbareZeilen);
        Assert.Equal("", cut.Instance.Meldung);
    }

    [Fact]
    public void Nach_dem_Lesen_stehen_die_Saetze_in_der_Liste()
    {
        var cut = Bauen(KatalogImportArt.Heizkessel, DreiZeilen());

        Einlesen(cut);

        // Die Vorbelegung 10..200 laesst den 250-kW-Kessel draussen.
        Assert.Equal(2, cut.Instance.SichtbareZeilen);
        Assert.Contains("Kessel klein", cut.Find("tbody").TextContent);
        Assert.DoesNotContain("Kessel gross", cut.Find("tbody").TextContent);
    }

    [Fact]
    public void Der_Zahlenfilter_und_der_Suchtext_wirken_zusammen()
    {
        var cut = Bauen(KatalogImportArt.Heizkessel, DreiZeilen());
        Einlesen(cut);

        // Obergrenze hochsetzen: alle drei
        cut.FindAll(".epos-katalogimport-filter input")[1].Input("100000");
        Assert.Equal(3, cut.Instance.SichtbareZeilen);

        // Suchtext ueber die FIRMA
        cut.FindAll(".epos-katalogimport-filter input")[2].Input("buderus");
        Assert.Equal(1, cut.Instance.SichtbareZeilen);
        Assert.Contains("Kessel gross", cut.Find("tbody").TextContent);

        // Zwei Begriffe wirken als UND ueber beide Spalten
        cut.FindAll(".epos-katalogimport-filter input")[2].Input("kessel buderus");
        Assert.Equal(1, cut.Instance.SichtbareZeilen);

        cut.FindAll(".epos-katalogimport-filter input")[2].Input("kessel wolf");
        Assert.Equal(0, cut.Instance.SichtbareZeilen);
    }

    /// <summary>
    /// Die Mehrfachmarkierung: Klick wählt eine, <c>Strg</c> nimmt dazu — der
    /// Ersatz für <c>SelectionMode.MultiExtended</c> der ListBox.
    /// </summary>
    [Fact]
    public void Mehrere_Zeilen_lassen_sich_markieren()
    {
        var cut = Bauen(KatalogImportArt.Heizkessel, DreiZeilen());
        Einlesen(cut);
        cut.FindAll(".epos-katalogimport-filter input")[1].Input("100000");

        var wahl = cut.FindAll("tbody .epos-anlagenwahl");
        Assert.Equal(3, wahl.Count);

        wahl[0].Click();
        Assert.Equal(new[] { 0 }, cut.Instance.Markiert);

        cut.FindAll("tbody .epos-anlagenwahl")[2].Click(new MouseEventArgs { CtrlKey = true });
        Assert.Equal(new[] { 0, 2 }, cut.Instance.Markiert);

        cut.FindAll("tbody .epos-anlagenwahl")[1].Click(new MouseEventArgs { ShiftKey = true });
        Assert.Equal(new[] { 1, 2 }, cut.Instance.Markiert);
    }

    /// <summary>
    /// Eine Markierung übersteht das Umfiltern, solange die Zeile sichtbar
    /// bleibt — und fällt heraus, wenn sie es nicht tut.
    /// </summary>
    [Fact]
    public void Die_Markierung_uebersteht_das_Umfiltern()
    {
        var cut = Bauen(KatalogImportArt.Heizkessel, DreiZeilen());
        Einlesen(cut);
        cut.FindAll(".epos-katalogimport-filter input")[1].Input("100000");

        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        cut.FindAll("tbody .epos-anlagenwahl")[2].Click(new MouseEventArgs { CtrlKey = true });
        Assert.Equal(new[] { 0, 2 }, cut.Instance.Markiert);

        // Obergrenze zurueck auf 200: der 250-kW-Kessel faellt aus Liste UND Markierung.
        cut.FindAll(".epos-katalogimport-filter input")[1].Input("200");
        Assert.Equal(2, cut.Instance.SichtbareZeilen);
        Assert.Equal(new[] { 0 }, cut.Instance.Markiert);
    }

    /// <summary>
    /// Die Detailfelder zeigen den angeklickten Satz — <c>ZeigeDetails</c> des
    /// Vorläufers.
    /// </summary>
    [Fact]
    public void Ein_Klick_zieht_die_Detailfelder_nach()
    {
        var cut = Bauen(KatalogImportArt.Heizkessel, DreiZeilen());
        Einlesen(cut);

        var felder = cut.FindAll(".epos-katalogimport-details input, .epos-katalogimport-details textarea");
        Assert.Equal("", felder[0].GetAttribute("value"));

        cut.FindAll("tbody .epos-anlagenwahl")[1].Click();

        felder = cut.FindAll(".epos-katalogimport-details input, .epos-katalogimport-details textarea");
        Assert.Equal("Kessel mittel", felder[0].GetAttribute("value"));
        Assert.Equal("Vaillant", felder[1].GetAttribute("value"));
        Assert.Equal("84.1", felder[3].GetAttribute("value"));
    }

    /// <summary>
    /// <b>Nur der Bezeichner ist änderbar</b> — in allen vier Designern trägt
    /// jedes andere Detailfeld ein <c>Enabled = false</c>.
    /// </summary>
    [Theory]
    [InlineData(KatalogImportArt.Heizkessel)]
    [InlineData(KatalogImportArt.Pufferspeicher)]
    [InlineData(KatalogImportArt.Solarkollektoren)]
    [InlineData(KatalogImportArt.Waermepumpe)]
    public void Nur_der_Bezeichner_ist_aenderbar(KatalogImportArt art)
    {
        var cut = Bauen(art, DreiZeilen());

        var felder = cut.FindAll(".epos-katalogimport-details input, .epos-katalogimport-details textarea");
        Assert.False(felder[0].HasAttribute("readonly"));
        for (int i = 1; i < felder.Count; i++)
            Assert.True(felder[i].HasAttribute("readonly"), "Feld " + i + " muesste gesperrt sein.");
    }

    /// <summary>
    /// Eine Handkorrektur am Bezeichner erreicht die Liste — und damit auch die
    /// Vorprüfung und das Schreiben (Abweichung A-4 zu Befund W13-B26).
    /// </summary>
    [Fact]
    public void Eine_Handkorrektur_am_Bezeichner_erreicht_die_Liste()
    {
        var cut = Bauen(KatalogImportArt.Heizkessel, DreiZeilen());
        Einlesen(cut);
        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();

        cut.FindAll(".epos-katalogimport-details input")[0].Input("Kessel umbenannt");

        Assert.Contains("Kessel umbenannt", cut.Find("tbody").TextContent);
        Assert.DoesNotContain("Kessel klein", cut.Find("tbody").TextContent);
    }

    // =====================================================================
    // 4 — Übernehmen
    // =====================================================================

    /// <summary>
    /// <b>Die leere Auswahl meldet sich</b> — ein Text für alle vier
    /// (Abweichung A-3; die Wärmepumpe brach wortlos ab, Befund W13-B29).
    /// </summary>
    [Fact]
    public void Ohne_Markierung_meldet_die_Uebernahme_sich()
    {
        var cut = Bauen(KatalogImportArt.Waermepumpe, DreiZeilen(),
            vorpruefen: (_, __) => Task.FromResult(Vorpruefung(false)),
            ausfuehren: (_, __, ___, ____, _____) => Task.FromResult(new ImportBilanz()));
        Einlesen(cut);

        cut.FindAll("button").First(b => b.TextContent.Contains("Speichern")).Click();

        Assert.Equal("Bitte einen Eintrag wählen.", cut.Instance.Meldung);
    }

    /// <summary>
    /// Der konfliktfreie Weg: Vorprüfung, kein Dialog, Ausführung, EINE
    /// Sammelmeldung — und der Dialog ist zu Ende, weil etwas geschrieben wurde.
    /// </summary>
    [Fact]
    public void Ein_konfliktfreier_Lauf_schreibt_und_meldet_einmal()
    {
        bool? ergebnis = null;
        List<int>? gesehen = null;

        var cut = Bauen(KatalogImportArt.Heizkessel, DreiZeilen(),
            vorpruefen: (markiert, _) => { gesehen = markiert.ToList(); return Task.FromResult(Vorpruefung(false)); },
            ausfuehren: (anzahl, _, __, ___, ____) => Task.FromResult(
                new ImportBilanz { Markiert = anzahl, Gespeichert = anzahl }),
            geschlossen: EventCallback.Factory.Create<bool>(this, b => ergebnis = b));

        Einlesen(cut);
        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        cut.FindAll("tbody .epos-anlagenwahl")[1].Click(new MouseEventArgs { CtrlKey = true });

        cut.FindAll("button").First(b => b.TextContent.Contains("Speichern")).Click();

        Assert.Equal(new[] { 0, 1 }, gesehen);
        Assert.Equal("2 von 2 Einträgen geladen.", cut.Instance.Meldung);
        Assert.True(ergebnis);
    }

    /// <summary>
    /// Ohne einen einzigen Treffer bleibt der Dialog offen, damit der Anwender
    /// Filter und Auswahl korrigieren kann.
    /// </summary>
    [Fact]
    public void Ohne_Treffer_bleibt_der_Dialog_offen()
    {
        bool? ergebnis = null;

        var cut = Bauen(KatalogImportArt.Heizkessel, DreiZeilen(),
            vorpruefen: (_, __) => Task.FromResult(Vorpruefung(false)),
            ausfuehren: (anzahl, _, __, ___, ____) => Task.FromResult(
                new ImportBilanz { Markiert = anzahl, Duplikat = anzahl }),
            geschlossen: EventCallback.Factory.Create<bool>(this, b => ergebnis = b));

        Einlesen(cut);
        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        cut.FindAll("button").First(b => b.TextContent.Contains("Speichern")).Click();

        Assert.Null(ergebnis);
        Assert.Contains("Bereits eingelesen (übersprungen): 1", cut.Instance.Meldung);
    }

    /// <summary>
    /// <b>Der Konfliktdialog ist eine ÜBERLAGERUNG</b>, kein zweites Fenster
    /// (Risiko R2) — und er erscheint EINMAL für die ganze Auswahl, nicht je Satz.
    /// </summary>
    [Fact]
    public void Bei_Konflikten_erscheint_EIN_Dialog_als_Ueberlagerung()
    {
        var cut = Bauen(KatalogImportArt.Solarkollektoren, DreiZeilen(),
            vorpruefen: (_, __) => Task.FromResult(Vorpruefung(true)),
            ausfuehren: (anzahl, _, __, ___, ____) => Task.FromResult(
                new ImportBilanz { Markiert = anzahl, Ueberschrieben = anzahl }));

        Einlesen(cut);
        cut.FindAll(".epos-katalogimport-filter input")[1].Input("100000");
        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        cut.FindAll("tbody .epos-anlagenwahl")[1].Click(new MouseEventArgs { CtrlKey = true });

        cut.FindAll("button").First(b => b.TextContent.Contains("Speichern")).Click();

        // Genau EINE Ueberlagerung, im SELBEN Fenster.
        Assert.Single(cut.FindAll("[role='dialog']"));
        Assert.Contains("Import: Konflikte prüfen", cut.Markup);
    }

    /// <summary>
    /// <b>Solar bekommt die Vorprüfung</b> (Abweichung A-6 zu Befund W13-B24):
    /// Es war die einzige der vier ohne Dublettenprüfung und ohne Konfliktdialog.
    /// </summary>
    [Fact]
    public void Auch_Solar_laeuft_ueber_die_Vorpruefung()
    {
        bool gefragt = false;

        var cut = Bauen(KatalogImportArt.Solarkollektoren, DreiZeilen(),
            vorpruefen: (_, __) => { gefragt = true; return Task.FromResult(Vorpruefung(false)); },
            ausfuehren: (anzahl, _, __, ___, ____) => Task.FromResult(
                new ImportBilanz { Markiert = anzahl, Gespeichert = anzahl }));

        Einlesen(cut);
        // Die Solarvorbelegung filtert bis 5 m² - die Probezeilen liegen darueber.
        cut.FindAll(".epos-katalogimport-filter input")[1].Input("100000");
        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        cut.FindAll("button").First(b => b.TextContent.Contains("Speichern")).Click();

        Assert.True(gefragt, "Auch der Solarimport muss vorpruefen.");
    }

    // =====================================================================
    // 5 — Meldungen des Lesens
    // =====================================================================

    /// <summary>
    /// Was der Parser gemeldet hat, steht als Warnbanner — im Bestand riss es die
    /// ganze Datei mit (Befund W13-B35, Abweichung A-2).
    /// </summary>
    [Fact]
    public void Eine_Lesemeldung_erscheint_als_Warnbanner()
    {
        var cut = Bauen(KatalogImportArt.Waermepumpe, DreiZeilen(), new[]
        {
            new PruefMeldung(PruefStufe.Warnung, "IMP_KAT_PROT_AUFSTELLUNG", "7")
        });

        Einlesen(cut);

        Assert.Contains("Unbekannter Aufstellungsindex", cut.Instance.Meldung);
        Assert.Contains("\"7\"", cut.Instance.Meldung);
        Assert.NotEmpty(cut.FindAll("[role='alert']"));
    }

    // =====================================================================
    // 6 — Tastatur
    // =====================================================================

    [Fact]
    public void Esc_schliesst_den_Dialog()
    {
        bool? ergebnis = null;
        var cut = Bauen(KatalogImportArt.Heizkessel, DreiZeilen(),
            geschlossen: EventCallback.Factory.Create<bool>(this, b => ergebnis = b));

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.False(ergebnis);
    }

    [Fact]
    public void Der_Fussknopf_OK_schliesst_ohne_Ergebnis()
    {
        bool? ergebnis = null;
        var cut = Bauen(KatalogImportArt.Heizkessel, DreiZeilen(),
            geschlossen: EventCallback.Factory.Create<bool>(this, b => ergebnis = b));

        cut.FindAll("button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.False(ergebnis);
    }

    // =====================================================================
    // Hilfen
    // =====================================================================

    private static KatalogVorpruefung Vorpruefung(bool konflikt)
    {
        var pruefungen = new List<ImportPruefung>
        {
            new() { Kandidat = new ImportKandidat { Name = "Kessel klein", Tag = 0 },
                    Befund = konflikt ? ImportBefund.NameVorhanden : ImportBefund.Neu,
                    Vorhanden = konflikt ? new KatalogSatz { Id = 7, Name = "Kessel klein" } : null }
        };
        return new KatalogVorpruefung(pruefungen, new[] { "kessel klein" }, konflikt,
                                      KatalogImportAblauf.AllesImportieren(pruefungen));
    }
}
