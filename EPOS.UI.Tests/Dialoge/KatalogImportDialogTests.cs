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

    /// <summary>
    /// Klickt „VDI 3805 Datei…". <b>Ohne Warten</b> — nur für die zwei Fälle, die den
    /// Dateiwähler ABSICHTLICH offen halten; überall sonst die Fassung mit der
    /// erwarteten Zeilenzahl.
    /// </summary>
    private static void Einlesen(IRenderedComponent<KatalogImportDialog> cut)
    {
        cut.FindAll("button").First(b => b.TextContent.Contains("VDI 3805")).Click();
    }

    /// <summary>
    /// Klickt „VDI 3805 Datei…" und wartet auf den GEZEICHNETEN Abschluss des
    /// Lesegangs: die Auswahlzeile unter der Liste meldet <paramref name="zeilen"/>
    /// Einträge.
    /// </summary>
    private static void Einlesen(IRenderedComponent<KatalogImportDialog> cut, int zeilen)
    {
        Einlesen(cut);
        Gezeichnet(cut, zeilen);
    }

    /// <summary>Die GEZEICHNETE Auswahlzeile unter der Liste („n von m Einträgen geladen.").</summary>
    private static string Auswahlzeile(IRenderedComponent<KatalogImportDialog> cut)
        => cut.FindAll(".epos-katalogimport > .epos-herleitung")[0].TextContent;

    /// <summary>
    /// Wartet auf den GEZEICHNETEN Stand der Liste: <paramref name="zeilen"/> sichtbare
    /// Zeilen, und die Auswahlzeile trägt dieselbe Zahl.
    ///
    /// <para><b>W6‑B‑2‑O‑1: bunits synchrone Ereignisse warten NICHT.</b> <c>Click()</c>,
    /// <c>Change()</c> und <c>Input()</c> geben das Ereignis nur beim Zeichner ab; nur
    /// die <c>…Async</c>-Fassungen liefern laut bunit-Dokumentation „a task that
    /// completes when the event handler is done". Der Zeichnerfaden
    /// (<c>RendererSynchronizationContext</c>) führt das Ereignis auf dem Prüffaden
    /// aus, SOLANGE seine Warteschlange frei ist; liegt dort schon ein Werkstück —
    /// nach einem Lesegang regelmäßig das <c>OnAfterRenderAsync</c> von QuickGrid —,
    /// wird das Ereignis EINGEREIHT, und die nächste Zeile des Falls liest den Stand
    /// VOR dem Ereignis. Verloren ging der Wettlauf im Kern-Lauf <b>216</b>, im
    /// Zwilling dieser Klasse
    /// (<c>ModulImportDialogTests.Der_Herstellerfilter_zeigt_nur_noch_die_Zeilen_des_Herstellers</c>,
    /// „Expected 5, Actual 155"); derselbe Commit war im Lauf 215 grün. Gemessen mit
    /// dem wörtlichen Prüfstand unter Rechenlast: altes Muster 68 bis 95 von 400 Läufen
    /// rot (zwei Messreihen), neues 0 von 400.</para>
    ///
    /// <para>Wo ein Lesegang NULL sichtbare Zeilen ergibt — die Solarvorbelegung
    /// filtert die Probezeilen weg —, trägt erst der nächste Filterschritt den
    /// Nachweis; dort steht dann dieser Aufruf mit der Zahl, die er erwartet.</para>
    /// </summary>
    private static void Gezeichnet(IRenderedComponent<KatalogImportDialog> cut, int zeilen)
        => cut.WaitForAssertion(() =>
        {
            Assert.Equal(zeilen, cut.Instance.SichtbareZeilen);
            Assert.Contains(" von " + zeilen + " ", Auswahlzeile(cut));
        });

    /// <summary>
    /// Wartet, bis genau diese Zeilen markiert sind. Begründung wie bei
    /// <see cref="Gezeichnet"/> — auch ein Zeilenklick ist ein Ereignis, hinter dem
    /// <c>Click()</c> nicht wartet.
    /// </summary>
    private static void Markiert(IRenderedComponent<KatalogImportDialog> cut, params int[] zeilen)
        => cut.WaitForAssertion(() => Assert.Equal(zeilen, cut.Instance.Markiert));

    /// <summary>
    /// Wartet auf den GEZEICHNETEN Abschluss eines Schreibgangs: Der Wirt hat gemeldet.
    /// <c>Schreibgang</c> ist <c>async</c> und läuft über <c>Vorpruefen</c> und
    /// <c>Ausfuehren</c> — dieselbe Regel und dieselbe Begründung wie bei
    /// <see cref="Gezeichnet"/>.
    /// </summary>
    private static void Gemeldet(IRenderedComponent<KatalogImportDialog> cut, string text)
        => cut.WaitForAssertion(() => Assert.Contains(text, cut.Instance.Meldung));

    /// <summary>
    /// Setzt den Filter einer Spalte über ihren TRICHTER (Stufe S3.4). Er ersetzt
    /// das von/bis-Paar und die Herstellerklappliste: <paramref name="trichter"/>
    /// zählt die filterbaren Spalten von links, <paramref name="ausdruck"/> ist ein
    /// <c>Zahlenausdruck</c> („10..200") bzw. „enthält…".
    /// </summary>
    private static void Spaltenfilter(IRenderedComponent<KatalogImportDialog> cut,
                                      int trichter, string ausdruck)
    {
        cut.FindAll(".epos-trichter")[trichter].Click();
        cut.Find(".epos-spaltenfilter input").Change(ausdruck);
    }

    /// <summary>Tippt in das EINE Suchfeld über allen Spalten (Stufe S3.4).</summary>
    private static void Suchen(IRenderedComponent<KatalogImportDialog> cut, string text)
        => cut.Find(".epos-katalog-suchzeile input").Input(text);

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

        // S3.4: Die gefilterte Groesse ist eine SPALTE geworden - der Kopf traegt
        // sie ohne "von:", denn ein Spaltenkopf ist keine Feldbeschriftung.
        Assert.Contains("Th. Leistung [kW]", cut.Find("thead").TextContent);
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

        Assert.Contains("Volumen [l]", cut.Find("thead").TextContent);
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

        Assert.Contains("Aperturfläche [m²]", cut.Find("thead").TextContent);
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
    /// <b>Die Filtervorbelegung des Designers ist mit Stufe S3.4 entfallen</b>
    /// (10…200 kW, 0…1000 l, 0…5 m², 0…100 kW). Sie war die Vorbelegung von ZWEI
    /// Zahlenfeldern; im Spaltenmodell gibt es kein Feld, das etwas vorbelegen
    /// könnte — und eine Vorbelegung, die beim Aufmachen Zeilen verschwinden ließe,
    /// war schon beim Stromspeicherimport als unerklärlich verworfen worden
    /// (Entscheid W13‑E‑2). <b>Nach dem Lesen steht jetzt alles da</b>; wer
    /// eingrenzen will, schreibt „10..200" in den Trichter der Spalte.
    /// </summary>
    [Theory]
    [InlineData(KatalogImportArt.Heizkessel)]
    [InlineData(KatalogImportArt.Pufferspeicher)]
    [InlineData(KatalogImportArt.Solarkollektoren)]
    [InlineData(KatalogImportArt.Waermepumpe)]
    public void Ohne_Filter_steht_nach_dem_Lesen_die_ganze_Datei(KatalogImportArt art)
    {
        var cut = Bauen(art, DreiZeilen());

        Einlesen(cut, 3);

        // Kein Zahlenfeld mehr in der Maske - die zwei Filterleisten sind gefallen.
        Assert.Empty(cut.FindAll(".epos-katalogimport-filter"));
        Assert.Contains("Kessel gross", cut.Find("tbody").TextContent);
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

        cut.WaitForAssertion(() => Assert.Equal(3, cut.Instance.SichtbareZeilen));
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

        // S3.4: ohne Vorbelegung stehen ALLE drei da.
        Einlesen(cut, 3);

        Assert.Contains("Kessel klein", cut.Find("tbody").TextContent);
        Assert.Contains("Kessel gross", cut.Find("tbody").TextContent);

        // Der Zahlenausdruck der SPALTE laesst den 250-kW-Kessel draussen -
        // dasselbe, was vorher die Vorbelegung 10..200 tat.
        Spaltenfilter(cut, 2, "10..200");
        Gezeichnet(cut, 2);
        Assert.DoesNotContain("Kessel gross", cut.Find("tbody").TextContent);
    }

    [Fact]
    public void Der_Zahlenfilter_und_der_Suchtext_wirken_zusammen()
    {
        var cut = Bauen(KatalogImportArt.Heizkessel, DreiZeilen());
        Einlesen(cut, 3);

        // Die Suche steht als EIN Feld ueber allen Spalten - hier trifft sie die FIRMA.
        Suchen(cut, "buderus");
        Gezeichnet(cut, 1);
        Assert.Contains("Kessel gross", cut.Find("tbody").TextContent);

        // Zwei Begriffe wirken als UND ueber alle Spalten
        Suchen(cut, "kessel buderus");
        Gezeichnet(cut, 1);

        Suchen(cut, "kessel wolf");
        Gezeichnet(cut, 0);

        // Und der Spaltenfilter wirkt ZUSAETZLICH (UND) zur Suche.
        Suchen(cut, "vaillant");
        Gezeichnet(cut, 2);
        Spaltenfilter(cut, 2, ">50");
        Gezeichnet(cut, 1);
        Assert.Contains("Kessel mittel", cut.Find("tbody").TextContent);
    }

    /// <summary>
    /// Die Mehrfachmarkierung. <b>Seit dem Anwenderentscheid W6‑E‑5 (07.09.2026)
    /// schaltet der EINFACHE Klick um</b> — bis dahin ersetzte er die Wahl (Semantik
    /// der <c>ListBox</c> mit <c>MultiExtended</c>), obwohl die Spalte ein
    /// Kontrollkästchen zeigt; wer zwei Zeilen anklickte, hatte am Ende eine.
    /// <c>Strg</c> tut dasselbe, <c>Umschalt</c> nimmt den Bereich ab dem Anker DAZU.
    /// </summary>
    /// <summary>Der Alle-Schalter der Suchzeile markiert alle sichtbaren Zeilen (W13-B-5).</summary>
    [Fact]
    public void Der_Alle_Schalter_markiert_alle_sichtbaren_Zeilen()
    {
        var cut = Bauen(KatalogImportArt.Heizkessel, DreiZeilen());
        Einlesen(cut, 3);

        cut.Find(".epos-wahl-alle input").Change(true);
        Markiert(cut, 0, 1, 2);

        cut.Find(".epos-wahl-alle input").Change(false);
        Markiert(cut);
    }

    [Fact]
    public void Mehrere_Zeilen_lassen_sich_markieren()
    {
        var cut = Bauen(KatalogImportArt.Heizkessel, DreiZeilen());
        Einlesen(cut, 3);

        var wahl = cut.FindAll("tbody .epos-anlagenwahl");
        Assert.Equal(3, wahl.Count);

        // Zwei EINFACHE Klicks - und beide Zeilen stehen.
        wahl[0].Click();
        Markiert(cut, 0);

        cut.FindAll("tbody .epos-anlagenwahl")[2].Click();
        Markiert(cut, 0, 2);

        // Noch ein Klick auf dieselbe Zeile nimmt sie wieder weg.
        cut.FindAll("tbody .epos-anlagenwahl")[2].Click();
        Markiert(cut, 0);

        // Strg tut dasselbe, Umschalt nimmt den Bereich ab dem Anker dazu.
        cut.FindAll("tbody .epos-anlagenwahl")[2].Click(new MouseEventArgs { CtrlKey = true });
        Markiert(cut, 0, 2);

        cut.FindAll("tbody .epos-anlagenwahl")[1].Click(new MouseEventArgs { ShiftKey = true });
        Markiert(cut, 0, 1, 2);
    }

    /// <summary>
    /// Eine Markierung übersteht das Umfiltern, solange die Zeile sichtbar
    /// bleibt — und fällt heraus, wenn sie es nicht tut.
    /// </summary>
    [Fact]
    public void Die_Markierung_uebersteht_das_Umfiltern()
    {
        var cut = Bauen(KatalogImportArt.Heizkessel, DreiZeilen());
        Einlesen(cut, 3);

        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        cut.FindAll("tbody .epos-anlagenwahl")[2].Click(new MouseEventArgs { CtrlKey = true });
        Markiert(cut, 0, 2);

        // Spaltenfilter "<=200": der 250-kW-Kessel faellt aus Liste UND Markierung.
        Spaltenfilter(cut, 2, "<=200");
        Gezeichnet(cut, 2);
        Markiert(cut, 0);
    }

    /// <summary>
    /// Die Detailfelder zeigen den angeklickten Satz — <c>ZeigeDetails</c> des
    /// Vorläufers.
    /// </summary>
    [Fact]
    public void Ein_Klick_zieht_die_Detailfelder_nach()
    {
        var cut = Bauen(KatalogImportArt.Heizkessel, DreiZeilen());
        Einlesen(cut, 3);

        var felder = cut.FindAll(".epos-katalogimport-details input, .epos-katalogimport-details textarea");
        Assert.Equal("", felder[0].GetAttribute("value"));

        cut.FindAll("tbody .epos-anlagenwahl")[1].Click();
        cut.WaitForAssertion(() => Assert.Equal("Kessel mittel",
            cut.FindAll(".epos-katalogimport-details input")[0].GetAttribute("value")));

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
        Einlesen(cut, 3);
        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();

        cut.FindAll(".epos-katalogimport-details input")[0].Input("Kessel umbenannt");

        cut.WaitForAssertion(() =>
            Assert.Contains("Kessel umbenannt", cut.Find("tbody").TextContent));
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
        Einlesen(cut, 3);

        cut.FindAll("button").First(b => b.TextContent.Contains("Speichern")).Click();

        Gemeldet(cut, "Bitte einen Eintrag wählen.");
    }

    /// <summary>
    /// Der konfliktfreie Weg: Vorprüfung, kein Dialog, Ausführung, EINE
    /// Sammelmeldung — und ANWENDERENTSCHEID W13-E-3 (09.09.2026): der Dialog
    /// bleibt offen, obwohl etwas geschrieben wurde; nur die Markierung wird
    /// geleert.
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

        Einlesen(cut, 3);
        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        cut.FindAll("tbody .epos-anlagenwahl")[1].Click(new MouseEventArgs { CtrlKey = true });
        Markiert(cut, 0, 1);

        cut.FindAll("button").First(b => b.TextContent.Contains("Speichern")).Click();

        Gemeldet(cut, "2 von 2 Einträgen geladen.");
        Assert.Equal(new[] { 0, 1 }, gesehen);

        // W13-E-3: Der Dialog bleibt offen - Geschlossen wird NICHT gerufen -,
        // und die Markierung ist geleert.
        Assert.Null(ergebnis);
        Assert.True(cut.Instance.Geschrieben);
        Markiert(cut);
    }

    /// <summary>
    /// <b>ANWENDERENTSCHEID W13-E-3</b> (09.09.2026): Nach dem Schreiben bleibt
    /// der Dialog offen — Erfolgsbanner und Liste stehen weiter, nur die
    /// Markierung wird geleert, damit dieselben Sätze nicht versehentlich ein
    /// zweites Mal geschrieben werden.
    /// </summary>
    [Fact]
    public void Nach_dem_Schreiben_bleibt_der_Dialog_offen_und_die_Markierung_ist_leer()
    {
        var cut = Bauen(KatalogImportArt.Heizkessel, DreiZeilen(),
            vorpruefen: (_, __) => Task.FromResult(Vorpruefung(false)),
            ausfuehren: (anzahl, _, __, ___, ____) => Task.FromResult(
                new ImportBilanz { Markiert = anzahl, Gespeichert = anzahl }));

        Einlesen(cut, 3);
        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        Markiert(cut, 0);

        cut.FindAll("button").First(b => b.TextContent.Contains("Speichern")).Click();

        // Das Erfolgsbanner steht ...
        Gemeldet(cut, "1 von 1 Einträgen geladen.");

        // ... die Liste auch (kein Unmount, kein neues Einlesen noetig) ...
        Assert.Equal(3, cut.Instance.SichtbareZeilen);

        // ... nur die Markierung ist geleert.
        Markiert(cut);
        Assert.True(cut.Instance.Geschrieben);
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

        Einlesen(cut, 3);
        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        cut.FindAll("button").First(b => b.TextContent.Contains("Speichern")).Click();

        Gemeldet(cut, "Bereits eingelesen (übersprungen): 1");
        Assert.Null(ergebnis);
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

        Einlesen(cut, 3);
        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        cut.FindAll("tbody .epos-anlagenwahl")[1].Click(new MouseEventArgs { CtrlKey = true });
        Markiert(cut, 0, 1);

        cut.FindAll("button").First(b => b.TextContent.Contains("Speichern")).Click();

        // Genau EINE Ueberlagerung, im SELBEN Fenster.
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[role='dialog']")));
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

        // S3.4: Es gibt keine Filtervorbelegung mehr - nach dem Lesen stehen bei
        // allen vier Auspraegungen alle drei Probezeilen da.
        Einlesen(cut, 3);

        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        cut.FindAll("tbody .epos-anlagenwahl")[2].Click();

        Markiert(cut, 0, 2);

        cut.FindAll("button").First(b => b.TextContent.Contains("Speichern")).Click();

        cut.WaitForAssertion(() => Assert.Equal(2, ausgefuehrt));
        Assert.Equal(new[] { 0, 2 }, gesehen);

        // W13-E-3: Der Dialog bleibt offen, die Markierung ist geleert.
        Assert.True(cut.Instance.Geschrieben);
        Markiert(cut);
    }

    /// <summary>
    /// <b>Der Doppelklick</b> (W6‑E‑5): Er nimmt die Zeile in die Markierung und
    /// übernimmt SIE sofort — die übrige Markierung bleibt zunächst stehen.
    ///
    /// <para>Der Fall schickt die Ereignisfolge des Browsers: <c>click</c>,
    /// <c>click</c>, <c>dblclick</c>. Die zwei Klicks heben sich mit der Umschaltregel
    /// auf, deshalb muss der Wirt die Zeile ausdrücklich hinzufügen.</para>
    ///
    /// <para><b>ANWENDERENTSCHEID W13-E-3</b> (09.09.2026): Der Dialog bleibt nach
    /// dem Schreiben offen, aber die Markierung wird geleert - dieselbe Regel
    /// wie beim Fussknopf „Speichern DB".</para>
    /// </summary>
    [Fact]
    public void Ein_Doppelklick_uebernimmt_genau_diese_Zeile()
    {
        List<int>? gesehen = null;

        var cut = Bauen(KatalogImportArt.Heizkessel, DreiZeilen(),
            vorpruefen: (markiert, _) => { gesehen = markiert.ToList(); return Task.FromResult(Vorpruefung(false)); },
            ausfuehren: (anzahl, _, __, ___, ____) => Task.FromResult(
                new ImportBilanz { Markiert = anzahl, Gespeichert = anzahl }));

        Einlesen(cut, 3);

        // Zeile 0 ist schon markiert - der Doppelklick darf sie nicht verlieren.
        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        Markiert(cut, 0);

        cut.FindAll("tbody .epos-anlagenwahl")[2].Click();
        cut.FindAll("tbody .epos-anlagenwahl")[2].Click();
        cut.FindAll("tbody .epos-anlagenwahl")[2].DoubleClick();

        // Geschrieben wurde NUR die doppelt geklickte Zeile ...
        cut.WaitForAssertion(() => Assert.Equal(new[] { 2 }, gesehen));
        // ... W13-E-3: nach dem Schreiben ist die Markierung geleert (der
        // Dialog bleibt offen).
        Assert.True(cut.Instance.Geschrieben);
        Markiert(cut);
    }

    /// <summary>
    /// <b>ANWENDERENTSCHEID W13-E-3</b> (09.09.2026): Der eigentliche Zweck der
    /// Änderung — zwei Übernahmen HINTEREINANDER, ohne den Dialog
    /// zwischendurch zu schliessen und neu zu öffnen (bei 6 654 Stromspeichern
    /// der Unterschied zwischen einem und zwei Netzabrufen).
    /// </summary>
    [Fact]
    public void Zwei_Uebernahmen_nacheinander_schreiben_zweimal()
    {
        int ausgefuehrt = 0;
        bool? ergebnis = null;

        var cut = Bauen(KatalogImportArt.Heizkessel, DreiZeilen(),
            vorpruefen: (_, __) => Task.FromResult(Vorpruefung(false)),
            ausfuehren: (anzahl, _, __, ___, ____) =>
            {
                ausgefuehrt++;
                return Task.FromResult(new ImportBilanz { Markiert = anzahl, Gespeichert = anzahl });
            },
            geschlossen: EventCallback.Factory.Create<bool>(this, b => ergebnis = b));

        Einlesen(cut, 3);

        // Erste Uebernahme.
        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        Markiert(cut, 0);
        cut.FindAll("button").First(b => b.TextContent.Contains("Speichern")).Click();
        cut.WaitForAssertion(() => Assert.Equal(1, ausgefuehrt));
        Markiert(cut);

        // Der Dialog ist weiter offen - eine zweite Wahl ist moeglich, und
        // Geschlossen wurde nicht gerufen.
        Assert.Null(ergebnis);

        // Zweite Wahl, zweite Uebernahme.
        cut.FindAll("tbody .epos-anlagenwahl")[1].Click();
        Markiert(cut, 1);
        cut.FindAll("button").First(b => b.TextContent.Contains("Speichern")).Click();

        cut.WaitForAssertion(() => Assert.Equal(2, ausgefuehrt));
        Markiert(cut);
        Assert.Null(ergebnis);
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

        Einlesen(cut, 3);
        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        Markiert(cut, 0);
        cut.FindAll("button").First(b => b.TextContent.Contains("Speichern")).Click();

        cut.WaitForAssertion(() => Assert.True(gefragt, "Auch der Solarimport muss vorpruefen."));
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

        // Der Lesegang endet hier im WARNBANNER - darauf wartet der Fall.
        cut.WaitForAssertion(() =>
            Assert.Contains("Unbekannter Aufstellungsindex", cut.Find("[role='alert']").TextContent));
        Assert.Contains("\"7\"", cut.Instance.Meldung);
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

        // Drei Lesegaenge, drei Ereignisse - und keines davon wartet von sich aus
        // (W6-B-2-O-1, Begruendung bei Gezeichnet).
        cut.WaitForAssertion(() => Assert.Equal(new[] { "CEC_NETZ", "CEC_DATEI", "BSLIB" },
                                                gelesen.Select(g => g.Quelle).ToArray()));
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
        Gezeichnet(cut, 4);

        // S3.4: Der Kopf traegt jetzt Sortierpfeil und Trichter; der NAME steht im
        // eigenen Element - die Wahlspalte hat keinen Sortierknopf und traegt ihn
        // unmittelbar.
        string[] koepfe = cut.FindAll(".epos-raster thead th")
                             .Select(t => (t.QuerySelector(".epos-spaltenkopf-text")
                                           ?? t).TextContent.Trim()).ToArray();

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
        Gezeichnet(cut, 4);

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
    /// <b>Aus zwei Zahlenbereichen und einer Herstellerklappliste sind drei
    /// SPALTEN geworden</b> (Stufe S3.4). Die Aussage bleibt: Eine Liste von 1 bis
    /// 10 032 kWh und 0,4 bis 4 904 kW ist mit EINEM Filter nicht einzugrenzen —
    /// beide Größen tragen deshalb einen eigenen Trichter, und der Hersteller
    /// ebenso.
    ///
    /// <para><b>Die Klappliste mit „(alle)" ist damit entfallen</b>: Sie zählte die
    /// 130 Hersteller der CEC-Liste auf, weil es keinen anderen Weg gab, einen
    /// davon zu treffen. Ein Feld „enthält…" trifft ihn mit vier Zeichen und
    /// braucht die Aufzählung nicht — und es kann etwas, das die Klappliste nie
    /// konnte: einen Namensteil.</para>
    /// </summary>
    [Fact]
    public void Die_drei_Filtergroessen_sind_Spalten_mit_Trichter()
    {
        var gelesen = new List<(string, string)>();
        var cut = BauenSpeicher(gelesen);
        Quelle(cut, "bslib laden");
        Gezeichnet(cut, 4);

        // Keine Filterleiste und keine Klappliste mehr.
        Assert.Empty(cut.FindAll(".epos-katalogimport-filter"));
        Assert.Empty(cut.FindAll(".epos-katalogimport select"));

        // Die drei Groessen stehen als Spalte da - jede mit ihrem Trichter.
        string kopf = cut.Find(".epos-raster thead").TextContent;
        Assert.Contains("Hersteller", kopf);
        Assert.Contains("kWh", kopf);
        Assert.Contains("kW", kopf);

        // Acht Spalten sind filterbar (alle ausser der Wahlspalte).
        Assert.Equal(8, cut.FindAll(".epos-raster thead .epos-trichter").Count);
    }

    /// <summary>
    /// <b>Der Herstellerfilter zeigt nur noch die Zeilen des Herstellers</b> —
    /// jetzt als Spaltenfilter „enthält…" statt als Klappliste — und die zwei
    /// Zahlenspalten greifen auf ihre je eigene Größe: kWh auf die Kapazität,
    /// kW auf die Leistung.
    /// </summary>
    [Fact]
    public void Die_drei_Filter_greifen_auf_ihre_je_eigene_Groesse()
    {
        var gelesen = new List<(string, string)>();
        var cut = BauenSpeicher(gelesen);
        Quelle(cut, "bslib laden");
        Gezeichnet(cut, 4);

        // Trichter 2 ist die Spalte "Hersteller" (nach Eintrag und Quelle).
        Spaltenfilter(cut, 2, "KOSTAL");
        Gezeichnet(cut, 1);

        Spaltenfilter(cut, 2, "");
        Gezeichnet(cut, 4);

        // Trichter 4 ist "kWh", Trichter 5 ist "kW" - zwei Groessen, zwei Filter.
        Spaltenfilter(cut, 4, ">100");
        Gezeichnet(cut, 0);

        Spaltenfilter(cut, 4, "");
        Gezeichnet(cut, 4);
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
        Gezeichnet(cut, 4);

        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        cut.FindAll("tbody .epos-anlagenwahl")[1].Click();

        Markiert(cut, 0, 1);

        await cut.InvokeAsync(() =>
            cut.FindAll("button").First(b => b.TextContent.Contains("Speichern")).Click());

        Gemeldet(cut, "2");
        Assert.Equal(new[] { 2 }, uebernommen);
    }

    // =====================================================================
    // 6 — Tastatur
    // =====================================================================

    /// <summary>Ohne dass in dieser Sitzung geschrieben wurde, meldet Esc <c>false</c>.</summary>
    [Fact]
    public void Esc_schliesst_den_Dialog()
    {
        bool? ergebnis = null;
        var cut = Bauen(KatalogImportArt.Heizkessel, DreiZeilen(),
            geschlossen: EventCallback.Factory.Create<bool>(this, b => ergebnis = b));

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        cut.WaitForAssertion(() => Assert.False(ergebnis));
    }

    /// <summary>
    /// <b>ANWENDERENTSCHEID W13-E-3</b> (09.09.2026): Wurde in dieser Sitzung
    /// schon geschrieben, meldet Esc beim wirklichen Schliessen <c>true</c> -
    /// analog zum Fussknopf „OK".
    /// </summary>
    [Fact]
    public void Nach_dem_Schreiben_meldet_Esc_true()
    {
        bool? ergebnis = null;
        var cut = Bauen(KatalogImportArt.Heizkessel, DreiZeilen(),
            vorpruefen: (_, __) => Task.FromResult(Vorpruefung(false)),
            ausfuehren: (anzahl, _, __, ___, ____) => Task.FromResult(
                new ImportBilanz { Markiert = anzahl, Gespeichert = anzahl }),
            geschlossen: EventCallback.Factory.Create<bool>(this, b => ergebnis = b));

        Einlesen(cut, 3);
        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        cut.FindAll("button").First(b => b.TextContent.Contains("Speichern")).Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.Geschrieben));
        Assert.Null(ergebnis);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        cut.WaitForAssertion(() => Assert.True(ergebnis));
    }

    /// <summary>Ohne dass in dieser Sitzung geschrieben wurde, meldet der Fussknopf „OK" <c>false</c>.</summary>
    [Fact]
    public void Der_Fussknopf_OK_schliesst_ohne_Ergebnis()
    {
        bool? ergebnis = null;
        var cut = Bauen(KatalogImportArt.Heizkessel, DreiZeilen(),
            geschlossen: EventCallback.Factory.Create<bool>(this, b => ergebnis = b));

        cut.FindAll("button").First(b => b.TextContent.Trim() == "OK").Click();

        cut.WaitForAssertion(() => Assert.False(ergebnis));
    }

    /// <summary>
    /// <b>ANWENDERENTSCHEID W13-E-3</b> (09.09.2026): Wurde in dieser Sitzung
    /// schon geschrieben, meldet der Fussknopf „OK" beim wirklichen
    /// Schliessen <c>true</c> - die Hülle muss ihren Aufruferkatalog nachladen.
    /// </summary>
    [Fact]
    public void Nach_dem_Schreiben_meldet_OK_true()
    {
        bool? ergebnis = null;
        var cut = Bauen(KatalogImportArt.Heizkessel, DreiZeilen(),
            vorpruefen: (_, __) => Task.FromResult(Vorpruefung(false)),
            ausfuehren: (anzahl, _, __, ___, ____) => Task.FromResult(
                new ImportBilanz { Markiert = anzahl, Gespeichert = anzahl }),
            geschlossen: EventCallback.Factory.Create<bool>(this, b => ergebnis = b));

        Einlesen(cut, 3);
        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        cut.FindAll("button").First(b => b.TextContent.Contains("Speichern")).Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.Geschrieben));

        // Der Dialog blieb bislang offen - Geschlossen wurde noch NICHT gerufen.
        Assert.Null(ergebnis);

        cut.FindAll("button").First(b => b.TextContent.Trim() == "OK").Click();

        cut.WaitForAssertion(() => Assert.True(ergebnis));
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
