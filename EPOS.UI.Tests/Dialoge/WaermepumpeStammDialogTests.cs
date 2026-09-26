using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Waermepumpe;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Datenbank Wärmepumpen (iU9-W7.3). Soll ist die Feldkarte von <c>Form_WP</c>:
/// 28 Zeilen — die Stammliste, neun Felder, zwei Kennlinienbilder in zwei
/// Reiterblättern, der Umschalter Wärme/Kühlung und sechs Knöpfe.
/// </summary>
public class WaermepumpeStammDialogTests : EposBunitContext
{

    /// <summary>
    /// Der Filterstand DIESES Prüfstands. Ohne ihn nähme der Dialog den aus dem
    /// <c>Katalogfilterregister</c> — der lebt prozessweit, und xunit fährt
    /// Testklassen nebeneinander. Dass das Register wirklich teilt, prüft
    /// <c>KatalogfilterstandTests</c>.
    /// </summary>
    private readonly Katalogfilterstand _filterstand = new();

    /// <summary>
    /// Die beiden Kennlinienbilder als ZEICHENMODELL — je EINE Instanz, EINMAL
    /// gebaut: Der Baustein <c>DiagrammSvg</c> vergleicht die MODELLREFERENZ, und ein
    /// je Zeichenlauf neu gebautes Modell setzte seinen Baum jedes Mal neu. Die
    /// Titel sind mit Absicht unverwechselbar — daran erkennt der Prüfstand, welches
    /// der beiden Blätter gerade im Baum steht.
    /// </summary>
    private static readonly Zeichenmodell BildCop = Kennlinienbild("BILD-COP");
    private static readonly Zeichenmodell BildLeistung = Kennlinienbild("BILD-LEISTUNG");

    private static Zeichenmodell Kennlinienbild(string titel)
    {
        var punkte = new List<(double Temperatur, double Wert)>();
        for (int t = -15; t <= 15; t += 5) punkte.Add((t, 4.0 + t * 0.05));

        return ChartRenderer.KennlinienModell(
            titel, titel, "Temperatur",
            new[] { new ChartRenderer.KennlinienReihe(35, punkte) },
            ChartRenderer.Kennlinienmarke.Kreis);
    }

    /// <summary>Der Text des gezeigten Kennlinienbildes (Titel, Achsen, Legende).</summary>
    private static string Bildtext(IRenderedComponent<WaermepumpeStammDialog> cut)
        => cut.Find(".epos-diagramm-svg").TextContent;

    /// <summary>
    /// Die Stammliste mit ihren NEUN Spalten (Anwenderentscheid W14a-E-10 vom
    /// 07.09.2026) - vorher eine Namensspalte.
    /// </summary>
    private static readonly Katalogfilterzeile[] Liste =
    {
        new Katalogfilterzeile(1, "WP Alpha")
            .MitText(Katalogfilterprofil.SpHersteller, "Alpha")
            .MitText(Katalogfilterprofil.SpBezeichner, "WP Alpha")
            .MitText(Katalogfilterprofil.SpQuelle, "Luft-Wasser")
            .MitZahl(Katalogfilterprofil.SpNennleistung, 12.0)
            .MitZahl(Katalogfilterprofil.SpVlMax, 55.0, 0)
            .MitZahl(Katalogfilterprofil.SpKuehlleistung, 6.5, 1),
        new Katalogfilterzeile(2, "WP Ausliefer") { Geschuetzt = true }
            .MitText(Katalogfilterprofil.SpHersteller, "Beta")
            .MitText(Katalogfilterprofil.SpBezeichner, "WP Ausliefer")
            .MitText(Katalogfilterprofil.SpQuelle, "Sole-Wasser")
            .MitZahl(Katalogfilterprofil.SpNennleistung, 20.0)
            .MitZahl(Katalogfilterprofil.SpVlMax, 65.0, 0)
            .MitZahl(Katalogfilterprofil.SpKuehlleistung, 0.0, 1)
    };

    public WaermepumpeStammDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static WaermepumpeStammDaten Satz(int id) => id switch
    {
        1 => new WaermepumpeStammDaten
        {
            Id = 1, Name = "WP Alpha", Firma = "Alpha", Beschreibung = "Testgerät",
            Typ = "Luft-Wasser", Baujahr = 2023, Aufstellung = "Innenaufstellung",
            Nennleistung = 12, Heizstab = 6, Regelung = "stetig",
            Kuehlleistung = 8.5, Modulkosten = 4000, MaxPtherm = 14, NurLesen = false
        },
        _ => new WaermepumpeStammDaten
        {
            Id = 2, Name = "WP Ausliefer", Firma = "Beta", Typ = "Sole-Wasser",
            Baujahr = 2020, Regelung = "einstufig", Nennleistung = 20, Heizstab = 9,
            NurLesen = true
        }
    };

    private IRenderedComponent<WaermepumpeStammDialog> Aufbauen(
        Func<WaermepumpeStammDaten, bool, KatalogSpeicherErgebnis>? speichern = null,
        Func<string, string?>? gesperrtDurch = null,
        Func<string, bool>? loeschen = null,
        Func<int, bool>? hatKuehlung = null,
        Func<int, IReadOnlyList<KennlinienZeile>>? kennlinien = null,
        Func<int, IReadOnlyList<KennlinienZeile>, bool>? abgleichen = null,
        Func<IReadOnlyList<Katalogfilterzeile>>? liste = null,
        Action<bool>? geschlossen = null,
        Func<Farbrolle, Farbe, Task>? farbeSetzen = null,
        Func<int, WaermepumpeStammDaten>? satz = null,
        EPOS.UI.Bausteine.Schlossweg? schloss = null)
        => Render<WaermepumpeStammDialog>(p => p
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.FarbeSetzen, farbeSetzen)
            .Add(x => x.Liste, liste ?? (() => Liste))
            .Add(x => x.Satz, satz ?? Satz)
            .Add(x => x.Schloss, schloss)
            .Add(x => x.Bilder, (id, kuehl) => new KennlinienBilder(
                kuehl ? BildLeistung : BildCop, kuehl ? BildCop : BildLeistung))
            .Add(x => x.HatKuehlung, hatKuehlung ?? (id => id == 1))
            .Add(x => x.Speichern, speichern ?? ((d, _) => new KatalogSpeicherErgebnis(true, "Gespeichert", d.Name)))
            .Add(x => x.GesperrtDurch, gesperrtDurch ?? (_ => null))
            .Add(x => x.Loeschen, loeschen ?? (_ => true))
            .Add(x => x.Kennlinien, kennlinien ?? (_ => new List<KennlinienZeile>
            {
                new() { Id = 1, Vorlauf = 35, Temperatur = -7, Cop = 2.8, Ptherm = 6.1 }
            }))
            .Add(x => x.KennlinienAbgleichen, abgleichen ?? ((_, _) => true))
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IElement Knopf(IRenderedComponent<WaermepumpeStammDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    /// <summary>Die Gruppe „Kenndaten" des Stammblatts (Stufe 3) — dort stehen die Stammfelder.</summary>
    private static IElement Kenndaten(IRenderedComponent<WaermepumpeStammDialog> cut)
        => cut.FindAll(".epos-stammblattgruppe")
              .First(g => g.QuerySelector(".epos-stammblattgruppe-titel")?.TextContent == "Kenndaten");

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Der_Feldbestand_der_Karte_steht()
    {
        var cut = Aufbauen();

        var gruppe = Kenndaten(cut);
        // Name, Hersteller (Text), Nennleistung, Heizstab (Ganzzahl), Kuehlleistung (Zahl).
        Assert.Equal(5, gruppe.QuerySelectorAll("input").Length);
        Assert.Single(gruppe.QuerySelectorAll("textarea"));
        // Vier Klapplisten: Typ, Leistungsstufen, Aufstellung, Baujahr.
        Assert.Equal(4, gruppe.QuerySelectorAll("select").Length);

        // Stufe 3: Die Knoepfe stehen an drei Orten - Fussleiste (Speichern,
        // Verwerfen, Neu, Beenden), Auswahlleiste (Vergleichen, Duplizieren...,
        // Loeschen) und der Kopf der Gruppe Kennlinie (Kennliniendaten...).
        var knopftexte = cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList();
        // W14a-E-10 / S2.2: Der Knopf "Modul-Katalog..." ist GEFALLEN - siehe
        // Der_Modulkatalog_ist_mit_S2_2_gefallen.
        Assert.DoesNotContain("📋  Modul-Katalog...", knopftexte);
        Assert.Contains("Kennliniendaten...", knopftexte);
        Assert.Contains("Speichern", knopftexte);
        Assert.Contains("Neu", knopftexte);
        Assert.Contains("Löschen", knopftexte);
        Assert.Contains("Beenden", knopftexte);
        Assert.Contains("Kennliniendaten...",
                        cut.Find(".epos-stammblattgruppe-kopf").TextContent);
    }

    // =================================================================================
    // Das Geruest (Konzept Administrationsdialoge, Stufe 1, V1)
    // =================================================================================

    /// <summary>
    /// <b>Die Kennlinien stehen im Eingabeblock</b>, nicht mehr zwischen Rahmen und
    /// Fußleiste: Dort waren sie ein dritter Bereich, dessen Reiterblatt in sich rollte
    /// (Katalogprobe, 1 088 × 624: 41 px sichtbar von 412). Im Eingabeblock rollen sie
    /// mit den Feldern — Titel und Fußleiste stehen, und zwischen ihnen stehen nur
    /// Liste und Eingabeblock.
    /// </summary>
    [Fact]
    public void Die_Kennlinien_stehen_im_Eingabeblock()
    {
        var cut = Aufbauen();

        Assert.Single(cut.FindAll(".epos-katalog-eingabe .epos-reiter"));
        Assert.Single(cut.FindAll(".epos-katalog-eingabe .epos-optionsgruppe"));

        // Zwischen Rahmen und Fussleiste steht kein weiterer Block.
        var kinder = cut.Find(".epos-katalog-dialog").Children
            .Select(e => e.ClassName ?? "").ToList();
        int rahmen = kinder.FindIndex(k => k.Contains("epos-katalograhmen"));
        int fuss = kinder.FindIndex(k => k.Contains("epos-leiste"));
        Assert.True(rahmen >= 0 && fuss == rahmen + 1, string.Join(" | ", kinder));
    }

    // =================================================================================
    // Die Fussleiste nach der Hausregel (DL-2 Nr. 4, Konzept Abschnitt 2 Zeile 4)
    // =================================================================================

    /// <summary>
    /// Die Fußleiste der Verwaltung (Stufe 3): <b>Speichern · Verwerfen · Füller · Neu ·
    /// Beenden</b>. „Kennliniendaten…" steht im Kopf der Gruppe Kennlinie, Duplizieren…
    /// und Löschen in der Auswahlleiste (V8: keine Handlung an zwei Orten); „Beenden"
    /// ist der einzige primäre Knopf und steht zuletzt.
    /// </summary>
    [Fact]
    public void Die_Fussleiste_traegt_das_Katalogmuster()
    {
        var cut = Aufbauen();
        var leiste = cut.FindAll(".epos-leiste").Last();

        var knoepfe = leiste.QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Speichern", "Verwerfen", "Neu", "Beenden" }, knoepfe);

        // Der Fueller steht zwischen "Verwerfen" und "Neu".
        var kinder = leiste.Children.Select(e => e.ClassName ?? "").ToList();
        Assert.Single(leiste.QuerySelectorAll(".epos-leiste-fueller"));
        Assert.Equal(2, kinder.FindIndex(k => k.Contains("epos-leiste-fueller")));

        var primaer = leiste.QuerySelectorAll("button.epos-knopf--primaer");
        Assert.Single(primaer);
        Assert.Equal("Beenden", primaer[0].TextContent.Trim());
    }

    /// <summary>
    /// Der Knopf trägt den kurzen Text, die Überlagerung den vollen Wortlaut — und
    /// hinter dem kurzen Knopf steht derselbe Handler: Er öffnet den Kennlinieneditor.
    /// </summary>
    [Fact]
    public void Der_kurze_Knopf_oeffnet_die_Ueberlagerung_mit_dem_vollen_Titel()
    {
        var cut = Aufbauen();

        Knopf(cut, "Kennliniendaten...").Click();

        Assert.True(cut.Instance.KennlinieneditorOffen);
        Assert.Contains("Kennliniendaten Ansicht/Bearbeiten...",
                        cut.Find(".epos-ueberlagerung").TextContent);
    }

    [Fact]
    public void Die_Beschriftungen_stehen_wie_im_Designer()
    {
        var cut = Aufbauen();
        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();

        foreach (string soll in new[]
                 {
                     "Name", "Hersteller", "Beschreibung", "Wärmepumpentyp", "Leistungsstufen",
                     "Aufstellung", "Baujahr", "Nennleistung", "Heizstab", "Kühlleistung"
                 })
            Assert.Contains(soll, texte);

        // Das Kopfband steht seit Stufe 3 als Kurztext am Titel (Zone 1 des Schemas).
        Assert.Equal("Verwaltung Daten zu Wärmepumpen und deren Kennlinien",
                     cut.Find(".epos-dialog-titel").GetAttribute("title"));
        Assert.Empty(cut.FindAll(".epos-kontextzeile"));
    }

    [Fact]
    public void Die_beiden_Reiterblaetter_heissen_COP_und_Leistung()
    {
        var cut = Aufbauen();
        var reiter = cut.FindAll(".epos-reiter-knopf").Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "COP", "Leistung" }, reiter);
    }

    /// <summary>Die Maske ist lokalisiert (21 englische Texte, W7.9).</summary>
    [Fact]
    public void Die_englischen_Texte_lassen_sich_setzen()
    {
        var cut = Render<WaermepumpeStammDialog>(p => p
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Liste, () => Liste)
            .Add(x => x.Satz, Satz)
            .Add(x => x.TitelText, "Heat pump database")
            .Add(x => x.LabelBaujahr, "Year of construction")
            .Add(x => x.BtnNeuText, "New"));

        Assert.Equal("Heat pump database", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Year of construction", cut.FindAll(".epos-feld-text").Select(e => e.TextContent));
        Assert.Contains("New", cut.FindAll("button").Select(b => b.TextContent.Trim()));
    }

    /// <summary>
    /// Ein Titel, eine Stelle (W11b-B-9): Eingebettet in die Ueberlagerung „Parameter
    /// Bearbeiten..." der Detailansicht trägt DIE den Titel — <c>TitelAnzeigen="false"</c>
    /// lässt Titel und Kreuz hier aus (Anwenderentscheid 15.09.2026: das Kreuz steht beim
    /// Titel). <c>TitelText</c> bleibt gesetzt; er nennt die Maske in der Assistentenmeldung.
    /// </summary>
    [Fact]
    public void Ohne_TitelAnzeigen_zeigt_der_Dialog_weder_Titel_noch_Kreuz()
    {
        var cut = Render<WaermepumpeStammDialog>(p => p
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Liste, () => Liste)
            .Add(x => x.Satz, Satz)
            .Add(x => x.TitelAnzeigen, false));

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
        // Der Hilfeknopf bleibt - er haengt nicht am Titel.
        Assert.NotEmpty(cut.FindAll(".epos-dialog-kopf"));
        Assert.Single(cut.FindAll(".epos-dialog-kopf--ohnetitel"));
    }

    // =================================================================================
    // Liste und Auswahl
    // =================================================================================

    [Fact]
    public void Vorbelegt_ist_die_erste_Zeile()
    {
        var cut = Aufbauen();
        Assert.Equal(1, cut.Instance.GewaehlteId);
        Assert.Equal("WP Alpha", Kenndaten(cut).QuerySelector("input")!.GetAttribute("value"));
    }

    /// <summary>
    /// <b>Ein Auslieferungssatz trägt das Schloss</b> (Konzept Administrationsdialoge,
    /// V10) — nur das Zeichen hinter dem Modell, das Wort im Kurztext. Der Vorläufer
    /// zeichnete ihn grau (listBox_WP_DrawItem:187); das Schloss ersetzt das Dimmen.
    /// </summary>
    [Fact]
    public void Ein_Auslieferungssatz_traegt_das_Schloss()
    {
        var cut = Aufbauen();
        var zeilen = cut.FindAll(".epos-raster tbody tr");

        Assert.Null(zeilen[0].QuerySelector(".epos-schloss"));
        var schloss = zeilen[1].QuerySelector(".epos-schloss");
        Assert.NotNull(schloss);
        Assert.StartsWith("Auslieferungssatz", schloss!.GetAttribute("title"));
    }

    /// <summary>
    /// <b>Die Stammliste trägt seit W14a‑E‑10 die NEUN Spalten des Profils</b>
    /// (Konzept 4.3) — bis dahin eine Namensspalte, während der einzige
    /// vollständige Filter des Hauses in einer Überlagerung saß (Befund 1.2/4).
    /// Seit dem 16.09.2026 tragen ALLE NEUN einen Trichter: Aus dem Kennzeichen
    /// „Kühlen" (Ja/Nein, nur Sortierpfeil) ist die Zahlenspalte „Kühlleistung [kW]"
    /// geworden.
    /// </summary>
    [Fact]
    public void Die_Stammliste_zeigt_die_neun_Spalten_und_neun_Trichter()
    {
        var cut = Aufbauen();

        Assert.Equal(9, cut.FindAll(".epos-spaltenkopf").Count);
        Assert.Equal(9, cut.FindAll(".epos-trichter").Count);
        Assert.Single(cut.FindAll(".epos-katalog-suchzeile"));
    }

    /// <summary>
    /// Der Spaltenfilter engt die Stammliste ein — dieselbe Bedienung wie beim
    /// Heizkessel und bei den PV-Modulen (der Kern des Entscheids W14a‑E‑10).
    /// </summary>
    [Fact]
    public void Der_Spaltenfilter_engt_die_Stammliste_ein()
    {
        var cut = Aufbauen();
        Assert.Equal(2, cut.FindAll(".epos-raster tbody tr").Count);

        // Quelle ist die dritte Spalte und damit der dritte Trichter.
        cut.FindAll(".epos-trichter")[2].Click();
        cut.Find(".epos-spaltenfilter input").Change("Luft");

        Assert.Single(cut.FindAll(".epos-raster tbody tr"));
        Assert.Equal("1 von 2 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);
    }

    [Fact]
    public void Die_Wahl_fuellt_die_Felder_und_die_Bilder()
    {
        var cut = Aufbauen();
        Assert.Contains("BILD-COP", Bildtext(cut));

        Zeilenklick.Zeile(cut, 1);

        Assert.Equal(2, cut.Instance.GewaehlteId);
        // WP Ausliefer ist ein Auslieferungssatz: Seit Stufe 4 stehen seine Kenndaten
        // als TEXT da (V13), nicht als gesperrte Felder.
        Assert.Equal("WP Ausliefer", Kenndaten(cut).QuerySelector(".epos-stammblattwert dd")!.TextContent);
    }

    // =================================================================================
    // Waerme / Kuehlung
    // =================================================================================

    [Fact]
    public void Der_Umschalter_erscheint_nur_mit_Kuehl_Kenndaten()
    {
        var cut = Aufbauen();
        Assert.Single(cut.FindAll(".epos-optionsgruppe"));      // WP Alpha hat Kuehlung

        Zeilenklick.Zeile(cut, 1); // WP Ausliefer hat keine
        Assert.Empty(cut.FindAll(".epos-optionsgruppe"));
    }

    [Fact]
    public void Der_Umschalter_zeichnet_die_Bilder_neu()
    {
        var cut = Aufbauen();
        Assert.Contains("BILD-COP", Bildtext(cut));

        cut.FindAll(".epos-optionsgruppe input[type=radio]")[1].Change(true);   // Kühlung
        Assert.Contains("BILD-LEISTUNG", Bildtext(cut));
    }

    /// <summary>
    /// <b>Die Kennlinien stehen als SVG im Baum</b> (Etappe DG-E3, Gruppe (b)) —
    /// unter zwei verschiedenen Kennungen. Ohne Zeichenfläche gibt es dort auch
    /// keine Bedienleiste: Ein Zoom auf eine Handvoll Stützstellen hätte keine
    /// Aussage (Entscheid DG-E3-7/-10).
    /// </summary>
    [Fact]
    public void Die_Kennlinien_stehen_als_DiagrammSvg()
    {
        var cut = Aufbauen();

        Assert.Single(cut.FindComponents<EPOS.UI.Bausteine.DiagrammSvg>());
        Assert.Equal("wp-kennlinie-cop",
                     cut.FindComponent<EPOS.UI.Bausteine.DiagrammSvg>().Instance.Kennung);
        Assert.Empty(cut.FindAll(".epos-diagramm-leiste"));

        // Das zweite Blatt traegt die ANDERE Kennung - zwei gleiche schnitten das eine
        // Bild am clipPath-Rechteck des anderen.
        cut.FindAll(".epos-reiter-knopf")[1].Click();
        Assert.Equal("wp-kennlinie-leistung",
                     cut.FindComponent<EPOS.UI.Bausteine.DiagrammSvg>().Instance.Kennung);
    }

    [Fact]
    public void Ein_Zeilenwechsel_faellt_auf_Waerme_zurueck()
    {
        // listBox_WP_SelectedIndexChanged:338 rief radioButton_Waerme.PerformClick().
        var cut = Aufbauen(hatKuehlung: _ => true);

        cut.FindAll(".epos-optionsgruppe input[type=radio]")[1].Change(true);
        Zeilenklick.Zeile(cut, 1);

        Assert.True(cut.FindAll(".epos-optionsgruppe input[type=radio]")[0].IsChecked());
    }

    // =================================================================================
    // Speichern, Neu, Loeschen
    // =================================================================================

    [Fact]
    public void Speichern_auf_einem_Auslieferungssatz_wird_abgelehnt()
    {
        bool geschrieben = false;
        var cut = Aufbauen(speichern: (d, _) =>
        {
            geschrieben = true;
            return new KatalogSpeicherErgebnis(true, "", d.Name);
        });

        Zeilenklick.Zeile(cut, 1);   // WP Ausliefer

        // AD-Q11: weich gesperrt, der Grund im Kurztext; die Felder sind nur lesbar.
        var speichern = Knopf(cut, "Speichern");
        Assert.Equal("true", speichern.GetAttribute("aria-disabled"));
        Assert.Contains("Duplizieren", speichern.GetAttribute("title") ?? "");
        // Stufe 4 (V13): Die Felder eines Auslieferungssatzes stehen als TEXT da.
        Assert.Contains("epos-stammblattgruppe--lesen", Kenndaten(cut).ClassName ?? "");
        Assert.Empty(Kenndaten(cut).QuerySelectorAll("input, select, textarea"));

        speichern.Click();

        Assert.False(geschrieben);
        Assert.Contains("Duplizieren", cut.Find(".epos-warnbanner").TextContent);
    }

    /// <summary>
    /// <b>„Duplizieren…" legt die eigene Wärmepumpe an</b> (AD-Q11): vorbelegt mit
    /// „Name (Kopie)", der Weg bekommt die ID des Auslieferungssatzes, danach ist die
    /// Kopie gewählt, und die Statuszeile nennt beide.
    /// </summary>
    [Fact]
    public void Duplizieren_legt_die_eigene_Waermepumpe_an_und_waehlt_sie()
    {
        var liste = Liste.ToList();
        (int, string)? gerufen = null;
        var cut = Render<WaermepumpeStammDialog>(p => p
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Liste, () => liste)
            .Add(x => x.Satz, id => id == 3
                ? new WaermepumpeStammDaten { Id = 3, Name = "WP Ausliefer (Kopie)", NurLesen = false }
                : Satz(id))
            .Add(x => x.Duplizieren, (id, name) =>
            {
                gerufen = (id, name);
                liste.Add(new Katalogfilterzeile(3, name).MitText(Katalogfilterprofil.SpBezeichner, name));
                return new KatalogSpeicherErgebnis(true, "", name);
            }));

        Zeilenklick.Zeile(cut, 1);   // WP Ausliefer
        Knopf(cut, "Duplizieren...").Click();

        Assert.True(cut.Instance.Duplizierfrage);
        Assert.Equal("WP Ausliefer (Kopie)", cut.Find(".epos-ueberlagerung input[type=text]").GetAttribute("value"));
        cut.Find(".epos-ueberlagerung").QuerySelectorAll("button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.Equal((2, "WP Ausliefer (Kopie)"), gerufen);
        Assert.Equal(3, cut.Instance.GewaehlteId);
        Assert.Contains("dupliziert", cut.Instance.Status);
        Assert.Null(Knopf(cut, "Speichern").GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Neu_leert_die_Felder_und_legt_beim_Speichern_an()
    {
        bool? alsNeu = null;
        var cut = Aufbauen(speichern: (d, neu) =>
        {
            alsNeu = neu;
            return new KatalogSpeicherErgebnis(true, "Gespeichert", d.Name);
        });

        Knopf(cut, "Neu").Click();
        Assert.Equal(0, cut.Instance.GewaehlteId);
        Assert.Equal("", Kenndaten(cut).QuerySelector("input")!.GetAttribute("value"));
        Assert.Empty(cut.FindAll(".epos-optionsgruppe"));        // keine Kuehlung ohne Geraet

        Knopf(cut, "Speichern").Click();
        Assert.True(alsNeu);
    }

    [Fact]
    public void Ein_abgelehntes_Speichern_bleibt_als_Banner_stehen()
    {
        var cut = Aufbauen(speichern: (_, _) =>
            new KatalogSpeicherErgebnis(false, "Speicherung nicht möglich, Fehler aufgetreten!", ""));

        Knopf(cut, "Speichern").Click();
        Assert.Contains("Fehler aufgetreten", cut.Find(".epos-warnbanner").TextContent);

        // Der Grund steht auch ROT in der Statuszeile neben dem Knopf.
        var status = cut.Find(".epos-leiste-fueller.epos-status");
        Assert.Contains("Fehler aufgetreten", status.TextContent);
        Assert.Contains("epos-status--fehler", status.ClassName);
    }

    [Fact]
    public void Loeschen_fragt_erst_nach()
    {
        bool geloescht = false;
        var cut = Aufbauen(loeschen: _ => { geloescht = true; return true; });

        Knopf(cut, "Löschen").Click();
        Assert.Single(cut.FindAll(".epos-rueckfrage"));
        Assert.False(geloescht);

        cut.Find(".epos-rueckfrage").QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == "Nein").Click();
        Assert.False(geloescht);
    }

    [Fact]
    public void Eine_Projektzuordnung_sperrt_das_Loeschen_und_nennt_das_Projekt()
    {
        bool geloescht = false;
        var cut = Aufbauen(gesperrtDurch: _ => "Musterprojekt",
                           loeschen: _ => { geloescht = true; return true; });

        // Stufe 3: Die Projektsperre steht schon am Knopf - weich gesperrt, der Kurztext
        // nennt das Projekt (Konzept 3.4); ein Klick meldet es, ohne Rueckfrage.
        var loeschen = Knopf(cut, "Löschen");
        Assert.Equal("true", loeschen.GetAttribute("aria-disabled"));
        Assert.Contains("Musterprojekt", loeschen.GetAttribute("title") ?? "");
        loeschen.Click();

        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
        Assert.False(geloescht);
        Assert.Contains("Musterprojekt", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Loeschen_eines_Auslieferungssatzes_wird_abgelehnt()
    {
        bool geloescht = false;
        var cut = Aufbauen(loeschen: _ => { geloescht = true; return true; });

        Zeilenklick.Zeile(cut, 1);   // WP Ausliefer

        // V13: weich gesperrt mit Grund, keine Rueckfrage.
        var loeschen = Knopf(cut, "Löschen");
        Assert.Equal("true", loeschen.GetAttribute("aria-disabled"));
        loeschen.Click();

        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
        Assert.False(geloescht);
        Assert.Contains("Löschen gesperrt", cut.Find(".epos-warnbanner").TextContent);
    }

    // =================================================================================
    // Die beiden Ueberlagerungen
    // =================================================================================

    [Fact]
    public void Kennliniendaten_oeffnen_den_Editor_in_der_Ueberlagerung()
    {
        var cut = Aufbauen();
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));

        Knopf(cut, "Kennliniendaten...").Click();

        Assert.True(cut.Instance.KennlinieneditorOffen);
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
    }

    [Fact]
    public void Der_Editor_schreibt_bei_OK_zurueck_und_zeichnet_neu()
    {
        int geschrieben = 0;
        var cut = Aufbauen(abgleichen: (_, _) => { geschrieben++; return true; });

        Knopf(cut, "Kennliniendaten...").Click();
        cut.Find(".epos-ueberlagerung").QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == "OK").Click();

        Assert.Equal(1, geschrieben);
        Assert.False(cut.Instance.KennlinieneditorOffen);
    }

    [Fact]
    public void Ein_Abbruch_im_Editor_schreibt_nichts()
    {
        int geschrieben = 0;
        var cut = Aufbauen(abgleichen: (_, _) => { geschrieben++; return true; });

        Knopf(cut, "Kennliniendaten...").Click();
        cut.Find(".epos-ueberlagerung").QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == "Abbruch").Click();

        Assert.Equal(0, geschrieben);
    }

    [Fact]
    public void Bei_einem_Auslieferungssatz_sagt_der_Editor_warum_und_schreibt_nicht()
    {
        int geschrieben = 0;
        var cut = Aufbauen(abgleichen: (_, _) => { geschrieben++; return true; });

        Zeilenklick.Zeile(cut, 1);   // WP Ausliefer
        Knopf(cut, "Kennliniendaten...").Click();

        Assert.Contains("nur angesehen", cut.FindAll(".epos-warnbanner")[0].TextContent);

        cut.Find(".epos-ueberlagerung").QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == "OK").Click();
        Assert.Equal(0, geschrieben);
    }

    /// <summary>
    /// <b>W14a‑E‑10 / S2.2 — der Knopf „Modul-Katalog…" ist gefallen.</b>
    ///
    /// <para>Er öffnete eine Überlagerung mit dem <c>WaermepumpenKatalogDialog</c>
    /// darin. Solange der seine eigene Filterleiste mit elf Bedienelementen hatte,
    /// war das ein zweiter Weg mit einer zweiten Bedienung; seit S2.2 zeigt er
    /// DIESELBE Liste mit DENSELBEN neun Spalten aus DEMSELBEN Weg
    /// (<c>WPStammCtrl.Katalogfilterzeilen</c>) — und DIESE Maske ist der Katalog,
    /// <c>Tab_WP_STAMM</c> steht schon da. Zwei Fassungen derselben Liste
    /// übereinander verbietet die Hausregel seit iZ5.</para>
    ///
    /// <para>Die zwei anderen Wirte des Katalogdialogs — Wärmepumpenverwaltung und
    /// Anlagenmaske — haben keine eigene Katalogliste und behalten ihn.</para>
    /// </summary>
    [Fact]
    public void Der_Modulkatalog_ist_mit_S2_2_gefallen()
    {
        var cut = Aufbauen();

        Assert.DoesNotContain(cut.FindAll("button"),
                              b => b.TextContent.Contains("Modul-Katalog"));

        // Die Zeile wird in der Liste DIESER Maske gewählt - der Weg, den der
        // Knopf umständlich nachgebaut hat.
        Zeilenklick.Zeile(cut, 1);
        Assert.Equal(2, cut.Instance.GewaehlteId);              // "WP Ausliefer"
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
    }

    // =================================================================================
    // Klapplisten mit Bestandswert, Tastatur, Abschluss
    // =================================================================================

    [Fact]
    public void Ein_Bestandswert_ausserhalb_der_festen_Liste_bleibt_stehen()
    {
        // A-16: Der Vorlaeufer hatte frei beschreibbare ComboBoxen; ein select wuerde
        // einen unbekannten Wert still verwerfen.
        var cut = Render<WaermepumpeStammDialog>(p => p
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Liste, () => new[] { new Katalogfilterzeile(9, "Sonder")
                                                 .MitText(Katalogfilterprofil.SpBezeichner, "Sonder") })
            .Add(x => x.Satz, _ => new WaermepumpeStammDaten
            {
                Id = 9, Name = "Sonder", Typ = "Abwasser-Wasser", Regelung = "stetig"
            }));

        var typen = Kenndaten(cut).QuerySelectorAll("select")[0]
                       .QuerySelectorAll("option").Select(o => o.TextContent).ToList();
        Assert.Equal("Abwasser-Wasser", typen[0]);
        Assert.Equal(5, typen.Count);                            // 4 feste + der Bestandswert
    }

    [Fact]
    public void Die_Baujahrliste_ist_lueckenlos_und_ohne_Dublette()
    {
        // A-15 / Befund W7-O-2: Der Vorlaeufer trug "2024" zweimal und "2022" nie.
        var cut = Aufbauen();
        var jahre = Kenndaten(cut).QuerySelectorAll("select")[3]
                       .QuerySelectorAll("option").Select(o => o.TextContent).ToList();

        Assert.Equal(new[] { "2025", "2024", "2023", "2022", "2021",
                             "2020", "2019", "2018", "2017", "2016" }, jahre);
    }

    [Fact]
    public void Beenden_und_Esc_melden_das_Ergebnis()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);
        Knopf(cut, "Beenden").Click();
        Assert.True(ergebnis);

        // Esc wirkt wie "Beenden" - EIN Schlussweg (Konzept Administrationsdialoge, V15).
        ergebnis = null;
        var cut2 = Aufbauen(geschlossen: b => ergebnis = b);
        cut2.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.True(ergebnis);
    }

    [Fact]
    public void Esc_bei_offener_Ueberlagerung_schliesst_den_Dialog_nicht()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        // Seit S2.2 ist der Kennlinien-Editor die verbliebene Ueberlagerung.
        Knopf(cut, "Kennliniendaten...").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Null(ergebnis);
    }

    /// <summary>
    /// Anwenderentscheid 15.09.2026: das Kreuz der Kopfzeile wirkt wie Esc — und beide
    /// wie „Beenden" (Konzept Administrationsdialoge, V15).
    /// </summary>
    [Fact]
    public void Kreuz_meldet_wie_Beenden()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog-zu").Click();

        Assert.True(ergebnis);
    }

    /// <summary>Das ✕ der Ueberlagerung "Kennliniendaten..." bricht NUR die Ebene ab.</summary>
    [Fact]
    public void Ueberlagerungskreuz_des_Kennlinieneditors_schliesst_nur_die_Ebene()
    {
        var cut = Aufbauen();

        Knopf(cut, "Kennliniendaten...").Click();
        Assert.True(cut.Instance.KennlinieneditorOffen);

        cut.Find(".epos-ueberlagerung-zu").Click();

        Assert.False(cut.Instance.KennlinieneditorOffen);
    }
    // =====================================================================
    //  Formularraster — Anwenderwunsch iU8‑E‑2, Paket P1 (05.09.2026)
    // =====================================================================

    /// <summary>
    /// <b>iU8‑E‑2, Paket P1:</b> „Darstellung der Dialoge kompakter und
    /// übersichtlicher — Parameterblöcke rechts."
    ///
    /// <para>Der Stammdatenblock steht seither im <c>Formularraster</c>: Die Beschriftung
    /// fällt NEBEN das Feld, die Felder ordnen sich in eine oder zwei Spalten,
    /// und ein Zahlenfeld ist kurz mit der Einheit unmittelbar dahinter. Zuvor
    /// nahm jedes Feld die volle Breite und die Beschriftung stand darüber.</para>
    ///
    /// <para>Die Regeln dahinter hält <c>Bausteine/FormularrasterTests</c>;
    /// hier steht nur, dass der Block ihn TRÄGT.</para>
    /// </summary>
    [Fact]
    public void Der_Stammdatenblock_steht_im_Formularraster()
    {
        var cut = Aufbauen();

        var raster = cut.FindAll(".epos-formularraster");
        Assert.NotEmpty(raster);
        Assert.Contains(raster, r => r.QuerySelectorAll(".epos-feld").Length > 0);

        // Ein Zahlenfeld meldet sich als KURZES Feld, und seine Einheit steht in
        // derselben Feldzeile — im Vorbild 4 px hinter dem Feld, im Befund am
        // rechten Rand des Blocks.
        var kurz = cut.FindAll(".epos-formularraster .epos-feld--kurz");
        Assert.NotEmpty(kurz);
        Assert.Contains(kurz, f => f.QuerySelector(".epos-feld-zeile .epos-einheit") is not null);
    }

    // =====================================================================
    //  Modulkosten — Anwenderentscheid W14a‑O‑1 (06.09.2026)
    // =====================================================================
    //
    //  Ä19 der Welle 7 nahm das Feld ganz aus der Maske; seither füllte es
    //  allein der VDI‑3805‑Import, und niemand sah, warum die Kostenübernahme
    //  keinen Planwert vorschlug. Der Entscheid holt den Wert zurück — NUR
    //  LESEND, mit Herleitungszeile. Die Fälle halten beides fest: dass er
    //  DASTEHT und dass er kein Eingabefeld ist.

    /// <summary>
    /// Die Zeile der Modulkosten — seit Stufe 4 der Neuordnung in der eigenen Gruppe
    /// „Kosten" des Stammblatts (nicht mehr zwischen den Kenndaten), als Lesewert.
    /// </summary>
    private static IElement Modulkostenfeld(IRenderedComponent<WaermepumpeStammDialog> cut)
        => Kostengruppe(cut).QuerySelector(".epos-stammblattwert")!;

    /// <summary>Die Gruppe „Kosten" des Stammblatts.</summary>
    private static IElement Kostengruppe(IRenderedComponent<WaermepumpeStammDialog> cut)
        => cut.FindAll(".epos-stammblattgruppe").Single(g => g.GetAttribute("aria-label") == "Kosten");

    /// <summary>Die Herleitungszeilen unter den Modulkosten.</summary>
    private static IEnumerable<string> Herleitungen(IRenderedComponent<WaermepumpeStammDialog> cut)
        => Kostengruppe(cut).QuerySelectorAll(".epos-herleitung").Select(e => e.TextContent.Trim()).ToList();

    [Fact]
    public void Die_Modulkosten_stehen_als_Lesewert_mit_Einheit_im_Raster()
    {
        var cut = Aufbauen();                                   // WP Alpha: 4000 €
        var feld = Modulkostenfeld(cut);

        Assert.Equal("Modulkosten", feld.QuerySelector("dt")!.TextContent.Trim());
        Assert.Equal("4000 €", feld.QuerySelector("dd")!.TextContent.Trim());
    }

    /// <summary>
    /// <b>Ä19 bleibt gewahrt.</b> Der Wert steht als Text da, nicht als gesperrtes
    /// Eingabefeld: Der Feldbestand des Blocks zählt weiterhin fünf <c>input</c>
    /// (Name, Hersteller, Nennleistung, Heizstab, Kühlleistung), und die Zelle der
    /// Modulkosten führt weder <c>input</c> noch <c>textarea</c> noch <c>select</c>.
    /// </summary>
    [Fact]
    public void Die_Modulkosten_sind_kein_Eingabefeld()
    {
        var cut = Aufbauen();

        Assert.Equal(5, Kenndaten(cut).QuerySelectorAll("input").Length);

        var feld = Kostengruppe(cut);
        Assert.Empty(feld.QuerySelectorAll("input"));
        Assert.Empty(feld.QuerySelectorAll("textarea"));
        Assert.Empty(feld.QuerySelectorAll("select"));
        Assert.Empty(feld.QuerySelectorAll("button"));
    }

    [Fact]
    public void Die_Herleitungszeile_steht_unter_dem_Wert()
    {
        var cut = Aufbauen();
        var zeilen = Herleitungen(cut);

        // Ein Wert ist da: nur die Herkunft, kein zweiter Hinweis.
        Assert.Equal(new[]
        {
            "aus dem Datenbestand; Gerätekosten werden in der Kostenverwaltung gepflegt"
        }, zeilen);
    }

    /// <summary>
    /// <b>0 heißt „kein Planwert".</b> <c>Tab_WP_STAMM.Modulkosten</c> ist eine
    /// INTEGER-Spalte, ein NULL kommt als 0 im Datensatz an, und
    /// <c>TechnikPlanwertCtrl.Basis</c> verwirft Beträge ≤ 0 — die Kostenübernahme
    /// schlägt dann nichts vor. Genau das sagt die Maske: der Halbgeviertstrich statt
    /// einer 0, keine Einheit dahinter und eine zweite leise Zeile mit dem Grund.
    /// </summary>
    [Fact]
    public void Ohne_Planwert_steht_ein_Strich_und_der_Grund_darunter()
    {
        var cut = Aufbauen();
        Zeilenklick.Zeile(cut, 1);   // WP Ausliefer: 0

        var feld = Modulkostenfeld(cut);
        Assert.Equal("–", feld.QuerySelector("dd")!.TextContent.Trim());   // ohne Einheit

        var zeilen = Herleitungen(cut);
        Assert.Equal(new[]
        {
            "aus dem Datenbestand; Gerätekosten werden in der Kostenverwaltung gepflegt",
            "kein Planwert im Datenbestand"
        }, zeilen);
    }

    /// <summary>Auch die neuen Zeilen sind lokalisiert (W7.9).</summary>
    [Fact]
    public void Die_Modulkostenzeilen_stehen_auch_englisch()
    {
        var cut = Render<WaermepumpeStammDialog>(p => p
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Liste, () => Liste)
            .Add(x => x.Satz, Satz)
            .Add(x => x.LabelModulkosten, "Module costs")
            .Add(x => x.HerleitungModulkosten,
                 "from the stored catalogue; equipment costs are maintained in cost management")
            .Add(x => x.HinweisModulkostenLeer, "no planned value in the stored catalogue"));

        var feld = Modulkostenfeld(cut);
        Assert.Equal("Module costs", feld.QuerySelector("dt")!.TextContent.Trim());
        Assert.Equal("4000 €", feld.QuerySelector("dd")!.TextContent.Trim());
        Assert.Equal(new[]
        {
            "from the stored catalogue; equipment costs are maintained in cost management"
        }, Herleitungen(cut));

        Zeilenklick.Zeile(cut, 1);   // WP Ausliefer: 0
        Assert.Equal("–", Modulkostenfeld(cut).QuerySelector("dd")!.TextContent.Trim());
        Assert.Equal(new[]
        {
            "from the stored catalogue; equipment costs are maintained in cost management",
            "no planned value in the stored catalogue"
        }, Herleitungen(cut));
    }

    /// <summary>
    /// Eine Neuanlage steht auf 0 — der Fall, um den es dem Anwender ging: Wer eine
    /// Wärmepumpe von Hand anlegt, sieht sofort, dass die Kostenübernahme von hier
    /// keinen Planwert bekommt.
    /// </summary>
    [Fact]
    public void Eine_Neuanlage_zeigt_den_Strich()
    {
        var cut = Aufbauen();
        Knopf(cut, "Neu").Click();

        Assert.Equal("–", Modulkostenfeld(cut).QuerySelector("dd")!.TextContent.Trim());
        Assert.Contains("kein Planwert im Datenbestand", Herleitungen(cut));
    }

    /// <summary>
    /// <b>Raster und Aufklapper zeigen denselben Wert.</b> Die
    /// <c>Parameteruebersicht</c> liest die Spalte roh aus der Datenbank, das Raster
    /// den Feldsatz des Dialogs — zwei Wege zu einer Zahl. Der Fall legt sie
    /// nebeneinander: Bei einem gepflegten Wert stehen dieselben Ziffern da.
    /// </summary>
    [Fact]
    public void Der_Aufklapper_zeigt_dieselbe_Zahl_wie_das_Raster()
    {
        var eintrag = new ParameterEintrag("Modulkosten", "Modulkosten", "€",
                                           new[] { Verwendung.Wirtschaftlichkeit },
                                           "TechnikPlanwertCtrl.cs:345");
        var cut = Render<WaermepumpeStammDialog>(p => p
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Liste, () => Liste)
            .Add(x => x.Satz, Satz)
            .Add(x => x.Uebersicht, _ => new[] { new Parameterwert(eintrag, "4000") }));

        Assert.Equal("4000 €", Modulkostenfeld(cut).QuerySelector("dd")!.TextContent.Trim());

        cut.Find(".epos-modulparameter-knopf").Click();
        Assert.Contains("4000",
                        cut.Find(".epos-parameteruebersicht-wert").TextContent.Trim());
    }

    // =================================================================================
    //  Die Farbwahl am Bild (Farbrollen, Bedienung Teil 2)
    // =================================================================================

    /// <summary>
    /// <b>Mit Schreibweg trägt das Farbfeld des Legendeneintrags die Klasse
    /// <c>epos-legende-farbfeld</c>, und ein Klick öffnet den Wähler.</b> Jede
    /// Vorlaufkennlinie führt eine Farbrolle; ohne <c>FarbeSetzen</c> gäbe es dort
    /// nichts zu klicken — kein Delegat, kein Wähler.
    /// </summary>
    [Fact]
    public void Das_Farbfeld_der_Kennlinie_oeffnet_den_Farbwaehler()
    {
        var cut = Aufbauen(farbeSetzen: (rolle, farbe) => Task.CompletedTask);

        Assert.NotEmpty(cut.FindAll("rect.epos-legende-farbfeld"));
        Assert.Empty(cut.FindAll(".epos-farbwahl"));

        cut.FindAll("rect.epos-legende-farbfeld")[0].Click();
        Assert.Single(cut.FindAll(".epos-farbwahl"));
    }

    /// <summary>Ohne Schreibweg bleibt das Farbfeld ein gemaltes Rechteck.</summary>
    [Fact]
    public void Ohne_Schreibweg_traegt_die_Kennlinie_kein_Farbfeld()
    {
        Assert.Empty(Aufbauen().FindAll("rect.epos-legende-farbfeld"));
    }

    // =================================================================================
    //  Stufe 4 (Konzept Administrationsdialoge): "nur mit Kuehlfunktion" in der
    //  Werkzeugleiste
    // =================================================================================

    /// <summary>
    /// <b>„nur mit Kühlfunktion"</b> steht in der Werkzeugleiste der Liste, neben dem
    /// Suchfeld — derselbe Schalter wie im Anlagendialog. Er setzt den Trichter
    /// „&gt;0" auf die Spalte Kühlleistung (kein zweiter Filterweg), und „Filter
    /// zurücksetzen" nimmt ihn mit.
    /// </summary>
    [Fact]
    public void Der_Kuehlschalter_setzt_den_Trichter_der_Kuehlleistung()
    {
        var cut = Aufbauen();
        Assert.Equal(2, cut.FindAll(".epos-raster tbody tr").Count);

        var schalter = cut.Find(".epos-katalog-suchzeile .epos-katalog-werkzeug input[type=checkbox]");
        Assert.Contains("nur mit Kühlfunktion", cut.Find(".epos-katalog-werkzeug").TextContent);
        Assert.False(cut.Instance.NurMitKuehlung);

        schalter.Change(true);

        Assert.True(cut.Instance.NurMitKuehlung);
        Assert.Equal(">0", _filterstand.Ausdruck(Katalogfilterprofil.SpKuehlleistung));

        cut.Find(".epos-katalog-ruecksetzer").Click();
        Assert.False(cut.Instance.NurMitKuehlung);
    }

    // =================================================================================
    //  Stufe 3 (Konzept Administrationsdialoge): Stammblatt mit der Gruppe Kennlinie,
    //  Aenderungserkennung, Auswahlleiste, Vergleich (V3 V6 V8 V9 V12 V13)
    // =================================================================================

    /// <summary>
    /// <b>Das Stammblatt der Wärmepumpe</b> (V9): vorn die dialogspezifische Gruppe
    /// „Kennlinie" mit „Kennliniendaten…" im Kopf und den zwei Reiterblättern, dann die
    /// Kenndaten; Herkunft und Kennzahlen im Kopf kommen aus der Zeile der Liste.
    /// </summary>
    [Fact]
    public void Das_Stammblatt_traegt_Kennlinie_und_Kenndaten()
    {
        var cut = Aufbauen();

        var titel = cut.FindAll(".epos-stammblattgruppe-titel").Select(e => e.TextContent).ToList();
        // Stufe 4: die eigene Gruppe Kosten (die Modulkosten, aus den Kenndaten heraus).
        Assert.Equal(new[] { "Kennlinie", "Kenndaten", "Kosten" }, titel);
        var kennlinie = cut.FindAll(".epos-stammblattgruppe")[0];
        Assert.NotNull(kennlinie.QuerySelector(".epos-reiter"));
        Assert.Contains("Kennliniendaten...", kennlinie.QuerySelector(".epos-stammblattgruppe-kopf")!.TextContent);

        Assert.Equal("WP Alpha", cut.Find(".epos-stammblatt-nametext").TextContent);
        Assert.Equal("Alpha · Luft-Wasser · eigener Satz", cut.Find(".epos-stammblatt-unter").TextContent);
        Assert.Equal(3, cut.FindAll(".epos-stammblatt-kennzahl").Count);
    }

    /// <summary>
    /// <b>Die Änderungserkennung</b> (zurückgestellt aus „Nach #445"): Ein geändertes
    /// Feld hält den Zeilenwechsel und Beenden an — „Speichern oder Verwerfen" im
    /// Warnband —, der Fuß des Stammblatts sagt es, und „Verwerfen" lädt den Satz neu.
    /// Bis Stufe 3 verwarf ein Zeilenwechsel die Eingabe still.
    /// </summary>
    [Fact]
    public void Ein_geaendertes_Feld_haelt_Zeilenwechsel_und_Beenden_an()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);
        Assert.True(Knopf(cut, "Verwerfen").HasAttribute("disabled"));

        Kenndaten(cut).QuerySelectorAll("input")[1].Input("Neuer Hersteller");
        Assert.True(cut.Instance.Geaendert);
        Assert.False(Knopf(cut, "Verwerfen").HasAttribute("disabled"));
        Assert.Contains("Speichern oder Verwerfen", cut.Find(".epos-stammblatt-hinweis").TextContent);

        Zeilenklick.Zeile(cut, 1);
        Assert.Equal(1, cut.Instance.GewaehlteId);
        Assert.Contains("Verwerfen", cut.Instance.Meldung);

        Knopf(cut, "Beenden").Click();
        Assert.Null(ergebnis);

        Knopf(cut, "Verwerfen").Click();
        Assert.False(cut.Instance.Geaendert);
        Assert.Equal("Alpha", Kenndaten(cut).QuerySelectorAll("input")[1].GetAttribute("value"));

        Zeilenklick.Zeile(cut, 1);
        Assert.Equal(2, cut.Instance.GewaehlteId);

        Knopf(cut, "Beenden").Click();
        Assert.True(ergebnis);
    }

    /// <summary>
    /// <b>Löschen mehrerer Wärmepumpen</b> (AD-Q9): Der Auslieferungssatz bleibt stehen
    /// (V13), eine einem Projekt zugeordnete bleibt stehen und nennt ihr Projekt; die
    /// Statuszeile nennt beide Zahlen.
    /// </summary>
    [Fact]
    public void Loeschen_mehrerer_laesst_Auslieferung_und_Projektgeraete_stehen()
    {
        var liste = Liste.Append(new Katalogfilterzeile(3, "WP Gamma")
                                     .MitText(Katalogfilterprofil.SpBezeichner, "WP Gamma")).ToList();
        var geloescht = new List<string>();
        var cut = Aufbauen(liste: () => liste,
                           gesperrtDurch: n => n == "WP Gamma" ? "Musterprojekt" : null,
                           loeschen: n => { geloescht.Add(n); liste.RemoveAll(z => z.Bezeichner == n); return true; });

        foreach (int i in new[] { 0, 1, 2 })
            cut.FindAll("td.epos-spalte-kaestchen input")[i].Change(true);
        Assert.Equal(3, cut.Instance.Kaestchen.Count);

        Knopf(cut, "Löschen").Click();
        Assert.Contains("Stehen bleiben (Auslieferungssatz): WP Ausliefer", cut.Find(".epos-rueckfrage").TextContent);
        cut.Find(".epos-rueckfrage").QuerySelectorAll("button").First(b => b.TextContent.Trim() == "Ja").Click();

        Assert.Equal(new[] { "WP Alpha" }, geloescht);
        Assert.Contains("Musterprojekt", cut.Find(".epos-warnbanner").TextContent);
        Assert.Equal("1 gelöscht, 2 stehen geblieben", cut.Instance.Status);
    }

    /// <summary>
    /// <b>Vergleichen</b> (V12): zwei Kästchen, der Vergleich im Stammblatt über die
    /// Felder des Stammsatzes samt Herkunft — die Herkunft weicht hier ab.
    /// </summary>
    [Fact]
    public void Zwei_Waermepumpen_vergleichen_im_Stammblatt()
    {
        var cut = Aufbauen();

        cut.FindAll("td.epos-spalte-kaestchen input")[0].Change(true);
        cut.FindAll("td.epos-spalte-kaestchen input")[1].Change(true);
        Knopf(cut, "Vergleichen").Click();

        Assert.True(cut.Instance.Vergleicht);
        Assert.Equal(new[] { "Parameter", "WP Alpha", "WP Ausliefer" },
                     cut.FindAll(".epos-stammblatt .epos-vergleich thead th").Select(th => th.TextContent.Trim()));
        Assert.Contains(cut.Instance.Vergleichszeilen, z => z.Name == "Herkunft" && z.Abweichend);
        Assert.Contains(cut.Instance.Vergleichszeilen, z => z.Name.StartsWith("Nennleistung", StringComparison.Ordinal) && z.Abweichend);
    }

    // =================================================================================
    // „Import…" als Zweitweg (Konzept Administrationsdialoge 7.1 d)
    // =================================================================================

    /// <summary>Der kleinste Parametersatz des Wärmepumpenimports (VDI 3805 Blatt 22).</summary>
    private static IReadOnlyDictionary<string, object> WpImportGaben() => new Dictionary<string, object>
    {
        ["Art"] = KatalogImportArt.Waermepumpe,
        ["ProfilVorgabe"] = KatalogImportProfil.Finde(KatalogImportArt.Waermepumpe,
                                                      EPOS.UI.Dialoge.Import.Texte.Zu),
        ["Meldungstext"] = new Func<SpeicherEngine.PruefMeldung, string>(EPOS.UI.Dialoge.Import.Texte.Zu),
        ["Fortschrittstext"] = new Func<ImportFortschritt, string>(EPOS.UI.Dialoge.Import.Texte.Zu)
    };

    [Fact]
    public void Ohne_Importweg_steht_kein_Importknopf()
    {
        // So in der Ueberlagerung des Anlagendialogs: dort reicht niemand den Weg herein.
        var cut = Aufbauen();

        Assert.Empty(cut.FindAll(".epos-importknopf"));
    }

    [Fact]
    public async Task Import_oeffnet_den_Waermepumpenimport_und_waehlt_danach_die_neue_Waermepumpe()
    {
        bool zu = false;
        var zeilen = Liste.ToList();
        var cut = Aufbauen(liste: () => zeilen.ToArray(), geschlossen: _ => zu = true);
        cut.Render(p => p.Add(x => x.ImportGaben, WpImportGaben));

        var knopf = cut.Find(".epos-importknopf");
        Assert.Equal("Import…", knopf.TextContent.Trim());
        knopf.Click();

        Assert.True(cut.Instance.ImportOffen);
        var ueberlagerung = cut.Find(".epos-ueberlagerung");
        Assert.Null(ueberlagerung.QuerySelector(".epos-ueberlagerung-kopf"));
        Assert.NotNull(ueberlagerung.QuerySelector(".epos-katalogimport .epos-dialog-titel"));
        Assert.Single(ueberlagerung.QuerySelectorAll(".epos-dialog-zu"));

        // Esc im Import: die Stammverwaltung bleibt offen.
        cut.Find(".epos-katalogimport").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(cut.Instance.ImportOffen);
        Assert.False(zu);

        // Zweiter Anlauf: eine Waermepumpe geschrieben.
        cut.Find(".epos-importknopf").Click();
        zeilen.Add(new Katalogfilterzeile(3, "WP Gamma")
            .MitText(Katalogfilterprofil.SpBezeichner, "WP Gamma"));
        var import = cut.FindComponent<EPOS.UI.Dialoge.Import.KatalogImportDialog>();
        await cut.InvokeAsync(() => import.Instance.Geschlossen.InvokeAsync(true));

        Assert.False(cut.Instance.ImportOffen);
        Assert.Equal(3, cut.Instance.GewaehlteId);
        Assert.Empty(cut.Instance.Kaestchen);
        Assert.Equal("„WP Gamma“ eingelesen.", cut.Instance.Status);
    }

    /// <summary>
    /// <b>Nach dem Wärmepumpenimport sind alle neuen Geräte gewählt</b> (V14, Konzept
    /// 7.1 d): Das Kästchen von vorher fällt, die zwei neuen stehen gewählt, die
    /// Auswahlleiste sagt „2 gewählt", die erste neue Wärmepumpe ist Fokuszeile im
    /// Stammblatt, die Statuszeile nennt die Zahl.
    /// </summary>
    [Fact]
    public async Task Nach_dem_Waermepumpenimport_sind_alle_neuen_Geraete_gewaehlt()
    {
        var zeilen = Liste.ToList();
        var cut = Aufbauen(liste: () => zeilen.ToArray(),
                           satz: id => id <= 2 ? Satz(id) : new WaermepumpeStammDaten
                           {
                               Id = id, Name = id == 3 ? "WP Gamma" : "WP Delta", Firma = "Gamma",
                               Typ = "Luft-Wasser", Nennleistung = 9, NurLesen = false
                           });
        cut.Render(p => p.Add(x => x.ImportGaben, WpImportGaben));
        cut.FindAll("td.epos-spalte-kaestchen input")[0].Change(true);
        Assert.Single(cut.Instance.Kaestchen);

        cut.Find(".epos-importknopf").Click();
        zeilen.Add(new Katalogfilterzeile(3, "WP Gamma").MitText(Katalogfilterprofil.SpBezeichner, "WP Gamma"));
        zeilen.Add(new Katalogfilterzeile(4, "WP Delta").MitText(Katalogfilterprofil.SpBezeichner, "WP Delta"));
        var liste = cut.FindComponent<EPOS.UI.Bausteine.Katalogliste>();
        Assert.Equal(0, liste.Instance.Zeigeanlass);
        var import = cut.FindComponent<EPOS.UI.Dialoge.Import.KatalogImportDialog>();
        await cut.InvokeAsync(() => import.Instance.Geschlossen.InvokeAsync(true));

        // Die Übernahme ist der Liste ein Anlass, die neue Fokuszeile ins Bild zu rollen.
        Assert.Equal(1, liste.Instance.Zeigeanlass);
        Assert.Equal(new[] { "WP Gamma", "WP Delta" }, cut.Instance.Kaestchen);
        Assert.Equal(3, cut.Instance.GewaehlteId);
        Assert.Equal("2 gewählt", cut.Find(".epos-auswahlleiste-was").TextContent.Trim());
        Assert.Equal("WP Gamma", cut.Find(".epos-stammblatt-nametext").TextContent);
        Assert.Equal(2, cut.FindAll("tbody td.epos-spalte-kaestchen input").Count(k => k.HasAttribute("checked")));
        Assert.Equal("2 Sätze übernommen und gewählt.", cut.Instance.Status);
    }
    // =================================================================================
    // „Schloss setzen…" / „Schloss aufheben…" (Entscheid AD-Q15)
    // =================================================================================

    /// <summary>
    /// <b>Das Schloss aufheben</b> (AD-Q15) in der Wärmepumpenverwaltung: Nach dem „Ja"
    /// liest der Dialog Liste und Satz neu — <c>NurLesen</c> fällt, die Kenndaten sind
    /// Eingabefelder, „Speichern" ist frei, das Stammblatt trägt das Band.
    /// </summary>
    [Fact]
    public void Schloss_aufheben_nach_Rueckfrage_gibt_Speichern_frei()
    {
        var schloss = new Schlosspruefung(2);
        IReadOnlyList<Katalogfilterzeile> Frisch() => schloss.Markieren(new[]
        {
            new Katalogfilterzeile(1, "WP Alpha").MitText(Katalogfilterprofil.SpBezeichner, "WP Alpha"),
            new Katalogfilterzeile(2, "WP Ausliefer").MitText(Katalogfilterprofil.SpBezeichner, "WP Ausliefer")
        });
        WaermepumpeStammDaten SatzMitSchloss(int id)
        {
            WaermepumpeStammDaten d = Satz(id);
            d.NurLesen = schloss.Gesperrt.Contains(id);
            return d;
        }
        var cut = Aufbauen(liste: Frisch, satz: SatzMitSchloss, schloss: schloss.Weg());

        Zeilenklick.Zeile(cut, 1);   // WP Ausliefer
        Assert.Equal("true", Knopf(cut, "Speichern").GetAttribute("aria-disabled"));
        Assert.Equal("Schloss aufheben...", Schlosspruefung.Beschriftung(cut));

        Schlosspruefung.Knopf(cut).Click();
        Assert.StartsWith("Schloss von „WP Ausliefer“ aufheben?", Schlosspruefung.Frage(cut));
        Schlosspruefung.Ja(cut);

        Assert.Equal(new[] { 2 }, schloss.Aufrufe.Single().Ids);
        Assert.Equal("WP Ausliefer", cut.Find(".epos-stammblatt-nametext").TextContent);
        Assert.True(Schlosspruefung.Band(cut));
        Assert.Null(Knopf(cut, "Speichern").GetAttribute("aria-disabled"));
        Assert.Equal("Schloss von „WP Ausliefer“ aufgehoben.", cut.Instance.Status);
    }
}
