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
/// Katalogimport (iU9-W13.1) — EINE Komponente, FÜNF Ausprägungen.
///
/// <para>Soll sind die Feldkarten von <c>Form_Heizkessel_einlesen</c> (17 Zeilen),
/// <c>Form_PufferSp_einlesen</c> (14), <c>Form_SolarKollektoren_einlesen</c> (11 + 11
/// im Gruppenrahmen) und <c>Form_WP_einlesen</c> (34). Der Abgleich läuft je
/// AUSPRÄGUNG, nicht je Komponente (Muster W8) — deshalb vier Feldbestandsfälle.</para>
///
/// <para>Die FÜNFTE Ausprägung — der <b>Stromspeicher</b> aus W13‑E‑2
/// (07.09.2026, Stufe S1) — hat keine Feldkarte, weil sie keinen Vorläufer hat:
/// Der Bestand kannte für Stromspeicher gar keinen Import. Geprüft wird deshalb
/// gegen das PROFIL — die drei Quellknöpfe, die sieben Listenspalten, der zweite
/// Zahlenbereich, die Herstellerklappliste und die Herleitungszeile zu den
/// fehlenden Kosten (Entscheid Q3).</para>
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
            p.Add(x => x.Lesen, (Func<string, string, IProgress<ImportFortschritt>, CancellationToken,
                                      Task<KatalogLeseErgebnis>>)((_, __, ___, ____) =>
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
            p.Add(x => x.Lesen, (Func<string, string, IProgress<ImportFortschritt>, CancellationToken,
                                      Task<KatalogLeseErgebnis>>)((_, __, ___, ____) =>
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
            p.Add(x => x.Lesen, (Func<string, string, IProgress<ImportFortschritt>, CancellationToken,
                                      Task<KatalogLeseErgebnis>>)((_, __, ___, ____) =>
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
    /// Die Mehrfachmarkierung. <b>Seit dem Anwenderentscheid W6‑E‑5 (07.09.2026)
    /// schaltet der EINFACHE Klick um</b> — bis dahin ersetzte er die Wahl (Semantik
    /// der <c>ListBox</c> mit <c>MultiExtended</c>), obwohl die Spalte ein
    /// Kontrollkästchen zeigt; wer zwei Zeilen anklickte, hatte am Ende eine.
    /// <c>Strg</c> tut dasselbe, <c>Umschalt</c> nimmt den Bereich ab dem Anker DAZU.
    /// </summary>
    [Fact]
    public void Mehrere_Zeilen_lassen_sich_markieren()
    {
        var cut = Bauen(KatalogImportArt.Heizkessel, DreiZeilen());
        Einlesen(cut);
        cut.FindAll(".epos-katalogimport-filter input")[1].Input("100000");

        var wahl = cut.FindAll("tbody .epos-anlagenwahl");
        Assert.Equal(3, wahl.Count);

        // Zwei EINFACHE Klicks - und beide Zeilen stehen.
        wahl[0].Click();
        Assert.Equal(new[] { 0 }, cut.Instance.Markiert);

        cut.FindAll("tbody .epos-anlagenwahl")[2].Click();
        Assert.Equal(new[] { 0, 2 }, cut.Instance.Markiert);

        // Noch ein Klick auf dieselbe Zeile nimmt sie wieder weg.
        cut.FindAll("tbody .epos-anlagenwahl")[2].Click();
        Assert.Equal(new[] { 0 }, cut.Instance.Markiert);

        // Strg tut dasselbe, Umschalt nimmt den Bereich ab dem Anker dazu.
        cut.FindAll("tbody .epos-anlagenwahl")[2].Click(new MouseEventArgs { CtrlKey = true });
        Assert.Equal(new[] { 0, 2 }, cut.Instance.Markiert);

        cut.FindAll("tbody .epos-anlagenwahl")[1].Click(new MouseEventArgs { ShiftKey = true });
        Assert.Equal(new[] { 0, 1, 2 }, cut.Instance.Markiert);
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
    /// <b>Der Nachweis zu W6‑E‑5 für alle vier Ausprägungen</b>: Zwei EINFACHE Klicks
    /// wählen zwei Sätze, und „Speichern" schreibt beide in einem Zug. Vor dem
    /// Entscheid ersetzte der zweite Klick die Wahl des ersten — der Anwender
    /// beschrieb das als „die Mehrfachauswahl funktioniert nicht".
    /// </summary>
    [Theory]
    [InlineData(KatalogImportArt.Heizkessel)]
    [InlineData(KatalogImportArt.Pufferspeicher)]
    [InlineData(KatalogImportArt.Solarkollektoren)]
    [InlineData(KatalogImportArt.Waermepumpe)]
    public void Zwei_einfache_Klicks_uebernehmen_zwei_Saetze(KatalogImportArt art)
    {
        List<int>? gesehen = null;
        int ausgefuehrt = 0;

        var cut = Bauen(art, DreiZeilen(),
            vorpruefen: (markiert, _) => { gesehen = markiert.ToList(); return Task.FromResult(Vorpruefung(false)); },
            ausfuehren: (anzahl, _, __, ___, ____) =>
            {
                ausgefuehrt = anzahl;
                return Task.FromResult(new ImportBilanz { Markiert = anzahl, Gespeichert = anzahl });
            });

        Einlesen(cut);
        // Die Vorbelegungen der vier Filter sind verschieden - die Obergrenze weit
        // aufmachen, damit alle drei Probezeilen stehen.
        cut.FindAll(".epos-katalogimport-filter input")[1].Input("100000");
        Assert.Equal(3, cut.Instance.SichtbareZeilen);

        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        cut.FindAll("tbody .epos-anlagenwahl")[2].Click();

        Assert.Equal(new[] { 0, 2 }, cut.Instance.Markiert);

        cut.FindAll("button").First(b => b.TextContent.Contains("Speichern")).Click();

        Assert.Equal(new[] { 0, 2 }, gesehen);
        Assert.Equal(2, ausgefuehrt);
    }

    /// <summary>
    /// <b>Der Doppelklick</b> (W6‑E‑5): Er nimmt die Zeile in die Markierung und
    /// übernimmt SIE sofort — die übrige Markierung bleibt stehen.
    ///
    /// <para>Der Fall schickt die Ereignisfolge des Browsers: <c>click</c>,
    /// <c>click</c>, <c>dblclick</c>. Die zwei Klicks heben sich mit der Umschaltregel
    /// auf, deshalb muss der Wirt die Zeile ausdrücklich hinzufügen.</para>
    /// </summary>
    [Fact]
    public void Ein_Doppelklick_uebernimmt_genau_diese_Zeile()
    {
        List<int>? gesehen = null;

        var cut = Bauen(KatalogImportArt.Heizkessel, DreiZeilen(),
            vorpruefen: (markiert, _) => { gesehen = markiert.ToList(); return Task.FromResult(Vorpruefung(false)); },
            ausfuehren: (anzahl, _, __, ___, ____) => Task.FromResult(
                new ImportBilanz { Markiert = anzahl, Gespeichert = anzahl }));

        Einlesen(cut);
        cut.FindAll(".epos-katalogimport-filter input")[1].Input("100000");

        // Zeile 0 ist schon markiert - der Doppelklick darf sie nicht verlieren.
        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();

        cut.FindAll("tbody .epos-anlagenwahl")[2].Click();
        cut.FindAll("tbody .epos-anlagenwahl")[2].Click();
        cut.FindAll("tbody .epos-anlagenwahl")[2].DoubleClick();

        // Geschrieben wurde NUR die doppelt geklickte Zeile ...
        Assert.Equal(new[] { 2 }, gesehen);
        // ... und die uebrige Markierung steht noch, samt der neuen Zeile.
        Assert.Equal(new[] { 0, 2 }, cut.Instance.Markiert);
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
    // 5b — Die fuenfte Auspraegung: Stromspeicher (W13-E-2, Stufe S1)
    // =====================================================================

    /// <summary>
    /// <b>Drei Quellknöpfe statt eines Dateiwählers.</b> Der Stromspeicher liest
    /// aus zwei Listen und auf drei Wegen; die vier VDI-Ausprägungen kennen nur
    /// eine <c>.vdi</c>-Datei und behalten ihren Wähler.
    /// </summary>
    [Fact]
    public void Stromspeicher_zeigt_drei_Quellknoepfe_statt_des_Dateiwaehlers()
    {
        var gelesen = new List<(string, string)>();
        var cut = BauenSpeicher(gelesen);

        Assert.Equal("Stromspeicher Einlesen", cut.Find(".epos-dialog-titel").TextContent);

        string[] knoepfe = cut.Find(".epos-katalogimport-quellen")
                              .QuerySelectorAll("button")
                              .Select(b => b.TextContent.Trim()).ToArray();

        Assert.Equal(new[] { "CEC-Liste abrufen", "CEC-Datei laden", "bslib laden" }, knoepfe);

        // Der Dateiwaehler der vier VDI-Auspraegungen steht NICHT daneben - es
        // gaebe sonst zwei Wege zu derselben Sache.
        Assert.Empty(cut.FindAll(".epos-dateiwahl"));
    }

    /// <summary>
    /// <b>Jeder Knopf meldet SEINE Quelle.</b> Daran hängt im Kern die Wahl des
    /// Zerlegers — <c>bslib_database.csv</c> und eine ausgeleitete CEC-CSV sind
    /// an der Endung nicht zu unterscheiden. Und nur „CEC-Datei laden" öffnet
    /// den Wähler; die zwei anderen kommen mit leerem Pfad, weil die Hülle die
    /// Datei selbst beschafft (Netzabruf, Auslieferungsdatei).
    /// </summary>
    [Fact]
    public void Jeder_Quellknopf_meldet_seinen_Schluessel_und_nur_einer_waehlt_eine_Datei()
    {
        var gelesen = new List<(string Quelle, string Pfad)>();
        var cut = BauenSpeicher(gelesen);

        Quelle(cut, "CEC-Liste abrufen");
        Quelle(cut, "CEC-Datei laden");
        Quelle(cut, "bslib laden");

        Assert.Equal(new[] { "CEC_NETZ", "CEC_DATEI", "BSLIB" },
                     gelesen.Select(g => g.Quelle).ToArray());
        Assert.Equal(new[] { "", "cec.xlsx", "" },
                     gelesen.Select(g => g.Pfad).ToArray());
    }

    /// <summary>
    /// Ein ABGEBROCHENER Wähler liest nichts — der Anwender hat gerade selbst
    /// entschieden, es zu lassen (dieselbe Regel wie bei den vier VDI-Masken).
    /// </summary>
    [Fact]
    public void Ein_abgebrochener_Waehler_liest_auch_beim_Stromspeicher_nichts()
    {
        var gelesen = new List<(string, string)>();
        var cut = BauenSpeicher(gelesen, waehlerPfad: "");

        Quelle(cut, "CEC-Datei laden");

        Assert.Empty(gelesen);
        Assert.Equal(0, cut.Instance.SichtbareZeilen);
    }

    /// <summary>
    /// <b>Die Liste zeigt die Kennwerte in der ZEILE.</b> Sieben Spalten neben
    /// dem Bezeichner — Quelle, Hersteller, Modell, kWh, kW, η_RT und Chemie;
    /// die zwei Bestandsspalten der VDI-Masken reichen für 6 654 Geräte nicht.
    /// </summary>
    [Fact]
    public void Stromspeicher_zeigt_seine_sieben_Listenspalten()
    {
        var gelesen = new List<(string, string)>();
        var cut = BauenSpeicher(gelesen);
        Quelle(cut, "bslib laden");

        string[] koepfe = cut.FindAll(".epos-raster thead th")
                             .Select(t => t.TextContent.Trim()).ToArray();

        Assert.Equal(new[] { "Wahl", "Eintrag", "Quelle", "Hersteller", "Modell",
                             "kWh", "kW", "η_RT", "Zellchemie" }, koepfe);

        // Und die WERTE stehen darin - die Zeile aus bslib traegt ihren
        // gemessenen Wirkungsgrad, die aus der CEC-Liste ohne Angabe nichts.
        string liste = cut.Find(".epos-raster").TextContent;
        Assert.Contains("Junelight Smart Battery 9,9", liste);
        Assert.Contains("0.9687", liste);
        Assert.Contains("Lithium-Eisen-Phosphat", liste);
    }

    /// <summary>
    /// <b>Neun Detailfelder, und nur der Bezeichner ist änderbar</b> — dieselbe
    /// Regel wie bei den vier VDI-Ausprägungen (Befund W13‑B26).
    /// </summary>
    [Fact]
    public void Stromspeicher_zeigt_neun_Detailfelder_und_nur_der_Bezeichner_ist_aenderbar()
    {
        var gelesen = new List<(string, string)>();
        var cut = BauenSpeicher(gelesen);
        Quelle(cut, "bslib laden");

        Assert.Equal(9, cut.FindAll(".epos-katalogimport-details label").Count);

        string felder = cut.Find(".epos-katalogimport-details").TextContent;
        Assert.Contains("Modell:", felder);
        Assert.Contains("Zellchemie:", felder);
        Assert.Contains("Kapazität: [kWh]", felder);
        Assert.Contains("Leistung: [kW]", felder);
        Assert.Contains("Round-Trip-Wirkungsgrad:", felder);
        Assert.Contains("Standby-Verbrauch: [W]", felder);
        Assert.Contains("Quelle:", felder);

        // Acht der neun Felder sind gesperrt; das eine offene ist der Bezeichner.
        var eingaben = cut.Find(".epos-katalogimport-details").QuerySelectorAll("input");
        Assert.Equal(8, eingaben.Count(e => e.HasAttribute("readonly")));
    }

    /// <summary>
    /// <b>Die Maske sagt, was die Quelle NICHT liefert</b> (Entscheid
    /// W13‑E‑2‑Q3): Kosten, Degradation und Zyklenzusage bleiben leer. Eine
    /// erfundene Zahl in einer Wirtschaftlichkeitsrechnung wäre schlimmer als
    /// eine fehlende — ein Speicher mit 0 EUR/kWh ist gratis und damit immer die
    /// beste Lösung.
    /// </summary>
    [Fact]
    public void Stromspeicher_traegt_die_Herleitungszeile_zu_den_fehlenden_Kosten()
    {
        var gelesen = new List<(string, string)>();
        var cut = BauenSpeicher(gelesen);

        Assert.Contains("Die Quelle liefert keine Kosten", cut.Markup);
        Assert.Contains("Stromspeicher", cut.Markup);
    }

    /// <summary>
    /// <b>Zwei Zahlenbereiche und eine Herstellerklappliste.</b> Eine Liste von
    /// 1 bis 10 032 kWh und 0,4 bis 4 904 kW ist mit EINEM Filter nicht
    /// einzugrenzen; und die Klappliste entsteht aus den gelesenen Sätzen —
    /// „(alle)" voran, jeder Hersteller einmal, alphabetisch.
    /// </summary>
    [Fact]
    public void Die_Filterleiste_fuehrt_zwei_Zahlenbereiche_und_die_Hersteller()
    {
        var gelesen = new List<(string, string)>();
        var cut = BauenSpeicher(gelesen);
        Quelle(cut, "bslib laden");

        string leiste = cut.Find(".epos-katalogimport-filter").TextContent;
        Assert.Contains("Kapazität [kWh] von:", leiste);
        Assert.Contains("Leistung [kW] von:", leiste);
        Assert.Contains("Hersteller:", leiste);

        string[] hersteller = cut.Find(".epos-katalogimport-filter")
                                 .QuerySelector("select")!
                                 .QuerySelectorAll("option")
                                 .Select(o => o.TextContent.Trim()).ToArray();

        Assert.Equal(new[] { "(alle)", "Alpha ESS Co., Ltd.", "BYD", "KOSTAL", "Siemens" },
                     hersteller);
    }

    /// <summary>
    /// <b>Der Herstellerfilter zeigt nur noch die Zeilen des Herstellers</b> —
    /// und der zweite Zahlenbereich wirkt auf die LEISTUNG, nicht auf die
    /// Kapazität.
    /// </summary>
    [Fact]
    public void Die_zwei_neuen_Filter_greifen_auf_ihre_je_eigene_Groesse()
    {
        var gelesen = new List<(string, string)>();
        var cut = BauenSpeicher(gelesen);
        Quelle(cut, "bslib laden");
        Assert.Equal(4, cut.Instance.SichtbareZeilen);

        // Hersteller "KOSTAL" (Zeile 3 der Klappliste, hinter "(alle)").
        cut.Find(".epos-katalogimport-filter select").Change("3");
        Assert.Equal(1, cut.Instance.SichtbareZeilen);

        cut.Find(".epos-katalogimport-filter select").Change("0");
        Assert.Equal(4, cut.Instance.SichtbareZeilen);
    }

    /// <summary>
    /// <b>Mehrfachwahl kommt aus dem WIRT</b> (W6‑E‑5) — hier wird nur belegt,
    /// dass die fünfte Ausprägung sie erbt: zwei Klicks, zwei Sätze in EINEM
    /// Schreibgang mit EINER Sammelmeldung.
    /// </summary>
    [Fact]
    public async Task Zwei_Klicks_uebernehmen_zwei_Speicher_in_einem_Schreibgang()
    {
        var gelesen = new List<(string, string)>();
        var uebernommen = new List<int>();

        var cut = BauenSpeicher(gelesen,
            vorpruefen: (markiert, _) =>
            {
                uebernommen.Add(markiert.Count);
                return Task.FromResult(SpeicherVorpruefung(markiert.ToArray()));
            },
            ausfuehren: (anzahl, entscheidungen, _, __, ___) =>
                Task.FromResult(new ImportBilanz { Markiert = anzahl, Gespeichert = entscheidungen.Count }));

        Quelle(cut, "bslib laden");

        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        cut.FindAll("tbody .epos-anlagenwahl")[1].Click();

        Assert.Equal(new[] { 0, 1 }, cut.Instance.Markiert);

        await cut.InvokeAsync(() =>
            cut.FindAll("button").First(b => b.TextContent.Contains("Speichern")).Click());

        Assert.Equal(new[] { 2 }, uebernommen);
        Assert.Contains("2", cut.Instance.Meldung);
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

    /// <summary>
    /// Vier Speicherzeilen, wie sie aus den zwei Quellen kämen — zwei aus der
    /// CEC-Liste (mit und ohne Wirkungsgrad) und zwei aus bslib (mit Standby).
    /// Die Zahlen sind ECHTE Werte der zwei Importproben.
    /// </summary>
    private static List<KatalogZeile> VierSpeicher() => new()
    {
        Speicher("Alpha ESS Co., Ltd.: SMILE-SP7.6", "Alpha ESS Co., Ltd.",
                 "SMILE-SP7.6", "Lithium-Eisen-Phosphat", 8.2, 7.6, "", "", "CEC_DATEI"),
        Speicher("BYD: Battery-Box Premium HVS 10.2", "BYD",
                 "Battery-Box Premium HVS 10.2", "Lithium-Eisen-Phosphat", 10.2, 9.0,
                 "0.93", "", "CEC_DATEI"),
        Speicher("Siemens: Junelight Smart Battery 9,9", "Siemens",
                 "Junelight Smart Battery 9,9", "", 8.85, 3.507, "0.9687", "15", "BSLIB"),
        Speicher("KOSTAL: PLENTICORE plus 10 / BYD Battery-Box H11.5", "KOSTAL",
                 "PLENTICORE plus 10 / BYD Battery-Box H11.5", "", 10.51, 5.776,
                 "0.9528", "9.18", "BSLIB")
    };

    private static KatalogZeile Speicher(string bezeichner, string firma, string modell,
                                         string chemie, double kwh, double kw,
                                         string eta, string standby, string quelle) =>
        new(bezeichner, firma, kwh, new Dictionary<string, string>
        {
            { KatalogImportProfil.FeldName, bezeichner },
            { KatalogImportProfil.FeldFirma, firma },
            { "MODELL", modell },
            { "TYP", chemie },
            { "ENERGIE", kwh.ToString(CultureInfo.InvariantCulture) },
            { "LEISTUNG", kw.ToString(CultureInfo.InvariantCulture) },
            { "ETA", eta },
            { "STANDBY", standby },
            { KatalogImportProfil.FeldQuelle, quelle }
        }, kw);

    /// <summary>
    /// Baut die Stromspeicher-Ausprägung. Anders als bei den vier VDI-Masken
    /// merkt sich der Prüfstand, mit welchem QUELLSCHLÜSSEL und mit welchem PFAD
    /// gelesen wurde — genau das unterscheidet die drei Knöpfe.
    /// </summary>
    private IRenderedComponent<KatalogImportDialog> BauenSpeicher(
        List<(string Quelle, string Pfad)> gelesen,
        List<KatalogZeile>? zeilen = null,
        IReadOnlyList<PruefMeldung>? meldungen = null,
        string waehlerPfad = "cec.xlsx",
        Func<IReadOnlyList<int>, IReadOnlyDictionary<int, string>, Task<KatalogVorpruefung>>? vorpruefen = null,
        Func<int, List<KonfliktEntscheidung>, IReadOnlyDictionary<int, string>,
             IProgress<ImportFortschritt>, CancellationToken, Task<ImportBilanz>>? ausfuehren = null,
        EventCallback<bool>? geschlossen = null)
    {
        return Render<KatalogImportDialog>(p =>
        {
            p.Add(x => x.Art, KatalogImportArt.Stromspeicher);
            p.Add(x => x.ProfilVorgabe,
                  KatalogImportProfil.Finde(KatalogImportArt.Stromspeicher, Texte.Zu));
            p.Add(x => x.DateiWaehlen,
                  (Func<string, Task<string?>>)(_ => Task.FromResult<string?>(waehlerPfad)));
            p.Add(x => x.Lesen, (Func<string, string, IProgress<ImportFortschritt>, CancellationToken,
                                      Task<KatalogLeseErgebnis>>)((quelle, pfad, __, ___) =>
            {
                gelesen.Add((quelle, pfad));
                return Task.FromResult(new KatalogLeseErgebnis(
                    zeilen ?? VierSpeicher(), meldungen ?? Array.Empty<PruefMeldung>()));
            }));
            p.Add(x => x.Vorpruefen, vorpruefen);
            p.Add(x => x.Ausfuehren, ausfuehren);
            p.Add(x => x.Sammelmeldung, (Func<ImportBilanz, string>)VdiAuswahlFilter.LadeMeldung);
            p.Add(x => x.Meldungstext, (Func<PruefMeldung, string>)Texte.Zu);
            p.Add(x => x.Fortschrittstext, (Func<ImportFortschritt, string>)Texte.Zu);
            if (geschlossen.HasValue) p.Add(x => x.Geschlossen, geschlossen.Value);
        });
    }

    /// <summary>Klickt einen der drei Quellknöpfe an seiner Beschriftung.</summary>
    private static void Quelle(IRenderedComponent<KatalogImportDialog> cut, string beschriftung) =>
        cut.Find(".epos-katalogimport-quellen").QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == beschriftung).Click();

    /// <summary>Die Vorprüfung einer Speicherauswahl — n Kandidaten, alle neu.</summary>
    private static KatalogVorpruefung SpeicherVorpruefung(params int[] indizes)
    {
        var pruefungen = indizes.Select(i => new ImportPruefung
        {
            Kandidat = new ImportKandidat { Name = "Satz " + i, Tag = i },
            Befund = ImportBefund.Neu
        }).ToList();

        return new KatalogVorpruefung(pruefungen, Array.Empty<string>(), false,
                                      KatalogImportAblauf.AllesImportieren(pruefungen));
    }

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
