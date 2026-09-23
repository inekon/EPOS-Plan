using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Lastspitzenkappung (iU9-W12.6), Vorbild <c>Views/Stromspeicher/Form_PeakShaving</c> —
/// seit Stufe 5 der Neuordnung der Administrationsdialoge (V16, Bestand A11) im Gerüst der
/// Verwaltungen: die Liste der Lastgänge (Zeile ist die Wahl), rechts das Stammblatt mit den
/// Gruppen Speicher, Schwelle, Kosten, „Berechnen" dazwischen und dem Ergebnis; die Datei
/// kommt über „Lastgang aus Datei…" als Überlagerung.
///
/// <para>Gerechnet wird mit der echten Engine über einen synthetischen Lastgang — keine
/// Datenbank, keine Oberfläche des Bestands.</para>
/// </summary>
public class PeakShavingDialogTests : EposBunitContext
{
    public PeakShavingDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // ---------------------------------------------------------------- Daten

    private static double[] Lastgang()
    {
        double[] w = new double[35040];
        for (int i = 0; i < w.Length; i++)
            w[i] = 100.0 + 40.0 * Math.Sin(2.0 * Math.PI * i / 96.0);
        for (int i = 1000; i < 1040; i++) w[i] = 400.0;
        return w;
    }

    private static IReadOnlyList<(int Id, string Text)> Zwei() => new[]
    {
        (0, "Werk Nord  [Projekt]"),
        (1, "Auslieferung  [Stamm]")
    };

    /// <summary>Die Zeilen, wie die Hülle sie liefert (<c>PeakShavingCtrl.Katalogfilterzeilen</c>).</summary>
    private static IReadOnlyList<Katalogfilterzeile> Zeilen() => new[]
    {
        new Katalogfilterzeile(11, "Werk Nord") { Schluessel = "G0" }
            .MitText(Katalogfilterprofil.SpBezeichner, "Werk Nord")
            .MitText(Katalogfilterprofil.SpQuelle, "Projekt")
            .MitZahl(Katalogfilterprofil.SpIntervallMin, 15, 0)
            .MitZahl(Katalogfilterprofil.SpJahresmaximumKw, 400, 1),
        new Katalogfilterzeile(16, "Auslieferung") { Schluessel = "G1" }
            .MitText(Katalogfilterprofil.SpBezeichner, "Auslieferung")
            .MitText(Katalogfilterprofil.SpQuelle, "Stamm")
            .MitZahl(Katalogfilterprofil.SpIntervallMin, 60, 0)
            .MitZahl(Katalogfilterprofil.SpJahresmaximumKw, 2070, 1)
    };

    private static PeakShavingVorbelegung Vorgaben(bool ausProjekt = false) =>
        new PeakShavingVorbelegung
        {
            AusProjekt = ausProjekt,
            Bezeichner = "Speicher A",
            LeistungspreisEurProKwA = 120.0,
            BezugspreisMittelCtKwh = 25.0,
            CCapEurProKwh = 400.0,
            CPowEurProKw = 200.0,
            IFixEur = 1000.0
        };

    /// <summary>Ein Zeichenmodell ohne Inhalt — die Prüfstände fragen nur, OB der Delegat gerufen wurde.</summary>
    private static Zeichenmodell Leermodell()
        => new Zeichenmodell(200, 100, Farbton.Aus(Farbrolle.HINTERGRUND));

    /// <summary>Der echte Rechenweg — dieselbe Engine wie in der Hülle.</summary>
    private static Task<PeakShavingErgebnis> Rechnen(double[] reihe, PeakShavingEingaben e)
        => Task.FromResult(new PeakShaving(e.AlsPeakShavingParameter(), e.Modus)
                               .BerechnePeakShaving(reihe, e.AlsSpeicherParameter()));

    private IRenderedComponent<PeakShavingDialog> Zeige(
        IReadOnlyList<(int Id, string Text)>? ganglinien = null,
        bool mitZeilen = true,
        PeakShavingVorbelegung? vorgaben = null,
        Func<int, Task<double[]>>? werte = null,
        Func<string, Task<string?>>? waehlen = null,
        Func<string, GanglinienImportRueckrufe, Task<GanglinienImportErgebnis>>? einlesen = null,
        Func<double[], PeakShavingEingaben, Task<PeakShavingErgebnis>>? rechnen = null,
        Func<double[], PeakShavingEingaben, Task<double>>? minimal = null,
        Func<PeakShavingErgebnis, bool, Zeichenmodell?>? modell = null,
        Func<PeakShavingErgebnis, Task<bool>>? csv = null,
        Func<double, bool, Task<bool>>? variante = null,
        Action<bool>? geschlossen = null)
    {
        IReadOnlyList<(int Id, string Text)> g = ganglinien ?? Zwei();
        return Render<PeakShavingDialog>(p => p
            .Add(x => x.VarianteUebernehmen, variante)
            .Add(x => x.Ganglinien, g)
            .Add(x => x.Lastgangzeilen, mitZeilen && ganglinien is null ? Zeilen() : null)
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Vorgaben, vorgaben ?? Vorgaben())
            .Add(x => x.Werte, werte ?? (i => Task.FromResult(Lastgang())))
            .Add(x => x.DateiWaehlen, waehlen)
            .Add(x => x.Einlesen, einlesen)
            .Add(x => x.Rechnen, rechnen ?? Rechnen)
            .Add(x => x.MinimaleSchwelle, minimal)
            .Add(x => x.Modell, modell)
            .Add(x => x.CsvSpeichern, csv)
            .Add(x => x.Geschlossen, (bool ok) => geschlossen?.Invoke(ok)));
    }

    /// <summary>Ein Zahlenfeld des Stammblatts — die Suchzeile der Liste zählt nicht mit.</summary>
    private static IElement Feld(IRenderedComponent<PeakShavingDialog> cut, int i)
        => cut.FindAll(".epos-stammblatt input.epos-eingabe")[i];

    private static IElement Schalter(IRenderedComponent<PeakShavingDialog> cut, int i)
        => cut.FindAll(".epos-stammblatt input[type=checkbox]")[i];

    private static IElement Rechenknopf(IRenderedComponent<PeakShavingDialog> cut)
        => cut.Find(".epos-peakshaving-rechnen button");

    private static IElement Fussleiste(IRenderedComponent<PeakShavingDialog> cut)
        => cut.FindAll(".epos-dialog > .epos-leiste").Last();

    // =====================================================================
    // Gerüst und Feldbestand
    // =====================================================================

    /// <summary>
    /// <b>Das Gerüst</b> (V16): die Liste der Lastgänge mit vier Spalten statt Optionsgruppe
    /// und Klappliste, kein Kästchen, kein Schloss; das Stammblatt mit Speicher, Schwelle,
    /// Kosten und Ergebnis; 14 Zahlenfelder, zwei Schalter, drei Reiter.
    /// </summary>
    [Fact]
    public void Das_Geruest_zeigt_Liste_und_Stammblatt()
    {
        var cut = Zeige();

        Assert.Contains("Lastspitzenkappung", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("epos-katalog-dialog", cut.Find(".epos-dialog").ClassName);
        Assert.Empty(cut.FindAll("input[type=radio]"));
        Assert.Equal(new[] { "Lastgang", "Quelle", "Intervall [min]", "Jahresmaximum [kW]" },
                     cut.FindAll(".epos-katalogliste thead .epos-spaltenkopf-text").Select(e => e.TextContent.Trim()).ToArray());
        Assert.Equal(2, cut.FindAll(".epos-katalogliste tbody tr").Count);
        Assert.Empty(cut.FindAll(".epos-katalogliste .epos-spalte-kaestchen"));
        Assert.Empty(cut.FindAll(".epos-schloss"));

        Assert.Equal(new[] { "Speicher", "Schwelle", "Kosten", "Ergebnis" },
                     cut.FindAll(".epos-stammblattgruppe-titel").Select(e => e.TextContent).ToArray());
        Assert.Equal(14, cut.FindAll(".epos-stammblatt input.epos-eingabe").Count);
        Assert.Equal(2, cut.FindAll(".epos-stammblatt input[type=checkbox]").Count);
        Assert.Equal(3, cut.FindAll("[role=tab]").Count);
        Assert.NotNull(Rechenknopf(cut));
    }

    /// <summary>
    /// <b>Keine Auswahlleiste im breiten Fenster</b>: Die Leiste steht nur im schmalen
    /// Fenster (<c>epos-nur-schmal</c>) und trägt dort keine Handlung — nur den Namen und
    /// „Stammblatt ›".
    /// </summary>
    [Fact]
    public void Die_Auswahlleiste_steht_nur_schmal_und_ohne_Handlung()
    {
        var cut = Zeige();

        IElement huelle = cut.Find(".epos-katalog-auswahl > div");
        Assert.Contains("epos-nur-schmal", huelle.ClassName);
        Assert.Equal(new[] { "Stammblatt ›" },
                     cut.FindAll(".epos-auswahlleiste button").Select(b => b.TextContent.Trim()).ToArray());
    }

    /// <summary>
    /// Ohne Katalogzeilen baut der Dialog die Liste aus den beschrifteten Ganglinien — der Weg
    /// eines Wirts, der nur sie liefert.
    /// </summary>
    [Fact]
    public void Ohne_Katalogzeilen_traegt_die_Ganglinienliste()
    {
        var cut = Zeige(mitZeilen: false);

        Assert.Equal(2, cut.Instance.Zeilen.Count);
        Assert.Equal("G0", cut.Instance.Gewaehlt);
    }

    /// <summary>Ohne Wähler kein „Lastgang aus Datei…", ohne CSV-Delegat kein CSV-Knopf.</summary>
    [Fact]
    public void Ohne_Delegat_bleibt_der_Knopf_weg()
    {
        var cut = Zeige();
        Assert.Empty(cut.FindAll("button.epos-importknopf"));
        Assert.Single(Fussleiste(cut).QuerySelectorAll("button"));   // nur "Beenden"

        var mit = Zeige(waehlen: p => Task.FromResult<string?>(""),
                        einlesen: (p, r) => Task.FromResult(new GanglinienImportErgebnis()),
                        csv: r => Task.FromResult(true));
        Assert.Single(mit.FindAll("button.epos-importknopf"));
        Assert.Equal(3, Fussleiste(mit).QuerySelectorAll("button").Length);
    }

    /// <summary>Die Vorbelegung steht in den Feldern, die Herkunftszeile nennt den Speicher.</summary>
    [Fact]
    public void Die_Vorbelegung_steht_in_den_Feldern()
    {
        var cut = Zeige(vorgaben: Vorgaben(ausProjekt: true));

        Assert.Equal("100", Feld(cut, 0).GetAttribute("value"));      // P [kW]
        Assert.Equal("200", Feld(cut, 1).GetAttribute("value"));      // Kapazitaet
        Assert.Contains("Speicher A", cut.Markup);
    }

    /// <summary>
    /// Die erste Zeile ist beim Aufbau geladen — der Vorläufer rief <c>QuelleGeaendert</c>
    /// am Ende des Konstruktors; der Kopf des Stammblatts nennt Quelle, Wertezahl und die
    /// Spitze.
    /// </summary>
    [Fact]
    public void Der_erste_Lastgang_ist_beim_Aufbau_geladen()
    {
        var cut = Zeige();

        Assert.NotNull(cut.Instance.Lastgang);
        Assert.Equal(35040, cut.Instance.Lastgang!.Count);
        Assert.Equal("Werk Nord", cut.Find(".epos-stammblatt-nametext").TextContent);
        Assert.Equal("Projekt · 35.040 Werte · noch nicht berechnet",
                     cut.Find(".epos-stammblatt-unter").TextContent);
        Assert.Contains("400 kW", cut.Find(".epos-stammblatt-kennzahlen").TextContent);
    }

    /// <summary>Die Zeile ist die Wahl: ein Klick lädt den Lastgang ihres Platzes.</summary>
    [Fact]
    public void Die_Zeile_waehlt_den_Lastgang()
    {
        var geladen = new List<int>();
        var cut = Zeige(werte: i => { geladen.Add(i); return Task.FromResult(Lastgang()); });

        Zeilenklick.Zeile(cut, 1);

        Assert.Equal("G1", cut.Instance.Gewaehlt);
        Assert.Equal(new[] { 0, 1 }, geladen);
        Assert.Equal("Auslieferung", cut.Find(".epos-stammblatt-nametext").TextContent);
    }

    // =====================================================================
    // Der Schalter „adaptiv"
    // =====================================================================

    /// <summary>Solange nachgezogen wird, ist die Zielschwelle gesperrt — sie bleibt SICHTBAR.</summary>
    [Fact]
    public void Die_Zielschwelle_ist_nur_im_festen_Betrieb_bedienbar()
    {
        var cut = Zeige();

        Assert.True(Feld(cut, 6).HasAttribute("disabled"));           // tb_Ziel
        Schalter(cut, 0).Change(false);                               // adaptiv aus
        Assert.False(Feld(cut, 6).HasAttribute("disabled"));
    }

    // =====================================================================
    // Rechnen
    // =====================================================================

    /// <summary>Ohne Reihe meldet der Dialog und rechnet nicht.</summary>
    [Fact]
    public void Ohne_Lastgang_meldet_der_Rechenknopf()
    {
        var cut = Zeige(werte: i => Task.FromResult(Array.Empty<double>()));

        Rechenknopf(cut).Click();

        Assert.Contains("Bitte zuerst einen Lastgang", cut.Instance.Meldung);
        Assert.Empty(cut.Instance.Kennzahlen);
    }

    /// <summary>
    /// Der Lauf füllt die Kennzahlzeilen und die Monatsspitzen; der Kopf des Stammblatts
    /// zeigt danach Spitze ohne und mit Speicher und den Ertrag.
    /// </summary>
    [Fact]
    public void Der_Lauf_fuellt_Kennzahlen_Monate_und_Kopf()
    {
        bool socGefragt = false;
        var cut = Zeige(modell: (r, soc) => { socGefragt = soc; return Leermodell(); });

        Rechenknopf(cut).Click();

        Assert.Equal(21, cut.Instance.Kennzahlen.Count);          // 18 Zeilen + 3 Trenner
        Assert.Contains(cut.Instance.Kennzahlen, z => z.Bezeichnung.Contains("Lastspitze"));
        Assert.False(socGefragt);
        Assert.True(cut.FindAll(".epos-stammblatt .epos-raster tbody tr").Count >= 21);
        Assert.Equal(new[] { "Spitze ohne Speicher", "Spitze mit Speicher", "Ertrag je Jahr" },
                     cut.FindAll(".epos-stammblatt-kennzahl dt").Select(e => e.TextContent).ToArray());
        Assert.EndsWith("berechnet", cut.Find(".epos-stammblatt-unter").TextContent);
    }

    /// <summary>Eine verletzte Fachregel blockiert den Lauf und meldet wörtlich — hier die Kapazität.</summary>
    [Fact]
    public void Eine_verletzte_Fachregel_blockiert_den_Lauf()
    {
        var cut = Zeige();

        Feld(cut, 1).Input("0");            // Kapazitaet
        Rechenknopf(cut).Click();

        Assert.Contains("Kapazität", cut.Instance.Meldung);
        Assert.Empty(cut.Instance.Kennzahlen);
    }

    /// <summary>
    /// Ein leeres PFLICHTfeld blockiert und nennt seinen Namen (Befund W12-B9); Leistungspreis
    /// und die Investitionsanteile dürfen leer bleiben.
    /// </summary>
    [Fact]
    public void Ein_leeres_Pflichtfeld_blockiert_ein_leerer_Preis_nicht()
    {
        var cut = Zeige();

        Feld(cut, 7).Input("");             // Leistungspreis - leer erlaubt
        Feld(cut, 9).Input("");             // c_cap - leer erlaubt
        Rechenknopf(cut).Click();
        Assert.NotEmpty(cut.Instance.Kennzahlen);

        Feld(cut, 0).Input("");             // Leistung P - Pflicht
        Rechenknopf(cut).Click();
        Assert.Contains("Leistung", cut.Instance.Meldung);
    }

    /// <summary>„Minimale haltbare Schwelle ermitteln" setzt das Schwellenfeld und schaltet adaptiv aus.</summary>
    [Fact]
    public void Die_minimale_Schwelle_landet_im_Feld_und_schaltet_adaptiv_aus()
    {
        var cut = Zeige(minimal: (r, e) => Task.FromResult(321.5));

        cut.Find(".epos-peakshaving-minimal").Click();

        Assert.False(Schalter(cut, 0).HasAttribute("checked"));
        Assert.Equal("321,5", Feld(cut, 6).GetAttribute("value"));
        Assert.Contains("321,5", cut.Instance.Meldung);
    }

    /// <summary>Reißt der Speicher die Schwelle, meldet der Dialog es als Warnung.</summary>
    [Fact]
    public void Eine_gerissene_Schwelle_wird_gemeldet()
    {
        var cut = Zeige();

        Schalter(cut, 0).Change(false);     // adaptiv aus
        Feld(cut, 6).Input("120");          // unhaltbar niedrig
        Rechenknopf(cut).Click();

        Assert.Contains("hält die Zielschwelle nicht", cut.Instance.Meldung);
    }

    /// <summary>Der Engine-Text geht ungefiltert in die Meldung (:533-538).</summary>
    [Fact]
    public void Ein_Engine_Einwand_steht_ungefiltert_im_Banner()
    {
        var cut = Zeige(rechnen: (r, e) => throw new ArgumentException("Reihe zu kurz"));

        Rechenknopf(cut).Click();

        Assert.Contains("Reihe zu kurz", cut.Instance.Meldung);
        Assert.Empty(cut.Instance.Kennzahlen);
    }

    // =====================================================================
    // Ergebnisanzeige
    // =====================================================================

    /// <summary>Der SoC-Schalter holt ein neues Zeichenmodell mit dem Ladezustand.</summary>
    [Fact]
    public void Der_SoC_Schalter_zeichnet_das_Bild_neu()
    {
        bool? letzterSoc = null;
        var cut = Zeige(modell: (r, soc) => { letzterSoc = soc; return Leermodell(); });

        Rechenknopf(cut).Click();
        Assert.False(letzterSoc);

        cut.FindAll("[role=tab]")[1].Click();                     // Reiter "Chart"
        cut.Find("[role=tabpanel] input[type=checkbox]").Change(true);

        Assert.True(letzterSoc);
    }

    /// <summary>Ein Zeilenwechsel verwirft das Ergebnis — wörtlich <c>QuelleGeaendert</c> (:275-277).</summary>
    [Fact]
    public void Ein_Zeilenwechsel_verwirft_das_Ergebnis()
    {
        var cut = Zeige();

        Rechenknopf(cut).Click();
        Assert.NotEmpty(cut.Instance.Kennzahlen);

        Zeilenklick.Zeile(cut, 1);
        Assert.Empty(cut.Instance.Kennzahlen);
    }

    /// <summary>Ohne Ergebnis meldet der CSV-Knopf „Bitte zuerst rechnen."</summary>
    [Fact]
    public void Der_CSV_Knopf_meldet_ohne_Ergebnis()
    {
        bool geschrieben = false;
        var cut = Zeige(csv: r => { geschrieben = true; return Task.FromResult(true); });

        Fussleiste(cut).QuerySelector("button")!.Click();

        Assert.False(geschrieben);
        Assert.Contains("Bitte zuerst rechnen", cut.Instance.Meldung);
    }

    // =====================================================================
    // "Lastgang aus Datei..." (V14)
    // =====================================================================

    /// <summary>
    /// <b>„Lastgang aus Datei…"</b> öffnet EINE Überlagerung mit Titel und genau einem Kreuz,
    /// darin die Dateiwahl (<c>.epos-einlesen</c>); die Datei wird eingelesen, erscheint als
    /// Zeile mit der Quelle „Datei", ist gewählt und geladen — und nichts wird abgelegt.
    /// </summary>
    [Fact]
    public void Lastgang_aus_Datei_liest_ein_und_waehlt_die_Zeile()
    {
        var reihe = Lastgang();
        var cut = Zeige(waehlen: f => Task.FromResult<string?>(@"C:\daten\Messung_3.csv"),
                        einlesen: (p, r) => Task.FromResult(new GanglinienImportErgebnis
                        {
                            Ausgang = ImportAusgang.Erfolg,
                            Bezeichner = "Messung_3.csv",
                            Werte = reihe
                        }));

        cut.Find("button.epos-importknopf").Click();

        Assert.True(cut.Instance.DateiOffen);
        Assert.Equal("Lastgang aus Datei", cut.Find(".epos-ueberlagerung-titel").TextContent);
        Assert.Single(cut.FindAll(".epos-ueberlagerung .epos-ueberlagerung-zu"));
        Assert.Single(cut.FindAll(".epos-ueberlagerung .epos-einlesen"));

        cut.Find(".epos-ueberlagerung .epos-dateiwahl button").Click();

        cut.WaitForAssertion(() => Assert.False(cut.Instance.DateiOffen));
        Assert.Equal(3, cut.Instance.Zeilen.Count);
        Assert.Equal("D1", cut.Instance.Gewaehlt);
        Assert.Same(reihe, cut.Instance.Lastgang);
        Assert.Equal("Datei", cut.Instance.Zeilen[2].Text(Katalogfilterprofil.SpQuelle));
        Assert.Contains("nicht abgelegt", cut.Instance.Status);
        Assert.Equal("Messung_3.csv", cut.Find(".epos-stammblatt-nametext").TextContent);
    }

    /// <summary>Das Kreuz der Überlagerung schließt sie, ohne einzulesen.</summary>
    [Fact]
    public void Das_Kreuz_schliesst_die_Dateiwahl()
    {
        var cut = Zeige(waehlen: f => Task.FromResult<string?>(null),
                        einlesen: (p, r) => Task.FromResult(new GanglinienImportErgebnis()));

        cut.Find("button.epos-importknopf").Click();
        cut.Find(".epos-ueberlagerung-zu").Click();

        Assert.False(cut.Instance.DateiOffen);
        Assert.Equal(2, cut.Instance.Zeilen.Count);
    }

    // =====================================================================
    // Schluss
    // =====================================================================

    /// <summary>
    /// Die Fußleiste läuft <b>Lastgang aus Datei… · CSV-Export · In Variante übernehmen ·
    /// Füller · Beenden</b>; „Beenden" ist der letzte und der EINZIGE primäre Knopf der Maske.
    /// </summary>
    [Fact]
    public void Die_Fussleiste_laeuft_Datei_CSV_Variante_Fueller_Beenden()
    {
        var cut = Zeige(waehlen: f => Task.FromResult<string?>(null),
                        einlesen: (p, r) => Task.FromResult(new GanglinienImportErgebnis()),
                        csv: r => Task.FromResult(true),
                        variante: (ziel, adaptiv) => Task.FromResult(true));

        IElement fuss = Fussleiste(cut);

        Assert.Equal(new[] { "Lastgang aus Datei…", "CSV-Export", "In Variante übernehmen", "Beenden" },
                     fuss.QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray());
        Assert.Single(fuss.QuerySelectorAll(".epos-leiste-fueller"));

        var primaer = fuss.QuerySelectorAll("button.epos-knopf--primaer");
        Assert.Single(primaer);
        Assert.Equal("Beenden", primaer[0].TextContent.Trim());
        Assert.Same(fuss.QuerySelectorAll("button").Last(), primaer[0]);
        Assert.Single(cut.FindAll(".epos-dialog button.epos-knopf--primaer"));
    }

    /// <summary>
    /// „Berechnen" steht im Stammblatt zwischen Parametern und Ergebnis — ohne Primärfarbe
    /// (DL-Q2 a) — und rechnet.
    /// </summary>
    [Fact]
    public void Der_Rechenknopf_steht_im_Stammblatt_zwischen_Parametern_und_Ergebnis()
    {
        var cut = Zeige();

        IElement rechnen = Rechenknopf(cut);
        Assert.Equal("Berechnen", rechnen.TextContent.Trim());
        Assert.DoesNotContain("epos-knopf--primaer", rechnen.ClassName ?? "");
        Assert.NotNull(rechnen.Closest(".epos-stammblatt"));

        // Die Reihenfolge im Blatt: Kosten, dann die Rechenleiste, dann das Ergebnis.
        string inhalt = cut.Find(".epos-stammblatt-inhalt").InnerHtml;
        int kosten = inhalt.IndexOf(">Kosten<", StringComparison.Ordinal);
        int knopf = inhalt.IndexOf("epos-peakshaving-rechnen", StringComparison.Ordinal);
        int ergebnis = inhalt.IndexOf(">Ergebnis<", StringComparison.Ordinal);
        Assert.True(kosten < knopf && knopf < ergebnis, $"{kosten} {knopf} {ergebnis}");

        rechnen.Click();
        Assert.NotEmpty(cut.Instance.Kennzahlen);
    }

    /// <summary><b>Befund W12-B24, wörtlich:</b> Fußknopf und Esc melden beide <c>false</c>.</summary>
    [Fact]
    public void Der_Fussknopf_und_Esc_melden_beide_false()
    {
        bool? ergebnis = null;
        var cut = Zeige(geschlossen: b => ergebnis = b);

        Fussleiste(cut).QuerySelectorAll("button").Last().Click();
        Assert.False(ergebnis);

        ergebnis = null;
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(ergebnis);
    }

    /// <summary>Das Schließkreuz wirkt wie Esc: es kommt immer <c>false</c> heraus.</summary>
    [Fact]
    public void Das_Kreuz_meldet_ebenfalls_false()
    {
        bool? ergebnis = null;
        var cut = Zeige(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog-kopf .epos-dialog-zu").Click();
        Assert.False(ergebnis);
    }

    /// <summary>Die Parameter stehen im Formularraster — kurze Felder mit Einheit dahinter.</summary>
    [Fact]
    public void Die_Parameter_stehen_im_Formularraster()
    {
        var cut = Zeige();

        Assert.Empty(cut.FindAll(".epos-feldpaar"));
        Assert.True(cut.FindAll(".epos-stammblatt .epos-formularraster").Count >= 3,
                    "weniger Raster als Gruppen");
        Assert.NotEmpty(cut.FindAll(
            ".epos-formularraster .epos-feld--kurz .epos-feld-zeile .epos-einheit"));
    }

    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F6)
    // =====================================================================

    /// <summary>Der Zeuge an der Maskenbrücke: gelesen wird die Vorbelegung, gesetzt der Leistungspreis.</summary>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_setzt_eine_Zahl()
    {
        var cut = Zeige();

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.PEAK_SHAVING));

        KiFeldzugang lp = KiMaskenbruecke.Feldzugang(KiMaskennamen.PEAK_SHAVING, "leistungspreis");
        Assert.NotNull(lp);
        Assert.True(lp.Setzbar);
        Assert.Equal(120.0, Convert.ToDouble(lp.Lesen(), CultureInfo.InvariantCulture));

        KiFeldumsetzung neu = KiFeldwandler.Wandle(lp, "150,5");
        Assert.True(neu.Ok, neu.Grund);
        lp.Setzen(neu.Wert);
        cut.Render();

        Assert.Equal(150.5, Convert.ToDouble(lp.Lesen(), CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// <b>Das Wahlfeld „Lastgang" trifft über den angezeigten Text</b> (KI‑D‑Q6): Der
    /// Assistent nennt eine Ganglinie beim Namen, und die Maske wählt ihre Zeile.
    /// </summary>
    [Fact]
    public void Die_Ganglinie_laesst_sich_ueber_ihren_Namen_setzen()
    {
        var cut = Zeige();

        KiFeldzugang gang = KiMaskenbruecke.Feldzugang(KiMaskennamen.PEAK_SHAVING, "ganglinie");
        Assert.NotNull(gang);
        Assert.NotEmpty(gang.Wahleintraege());

        KiFeldumsetzung wahl = KiFeldwandler.Wandle(gang, "Auslieferung");
        Assert.True(wahl.Ok, wahl.Grund);
        gang.Setzen(wahl.Wert);
        cut.Render();

        Assert.Equal(1, Convert.ToInt32(gang.Lesen(), CultureInfo.InvariantCulture));
        Assert.Equal("G1", cut.Instance.Gewaehlt);
    }

    /// <summary>Reihenzeile, Herkunft und das offene Blatt sind NICHT setzbar.</summary>
    [Fact]
    public void Reihe_Herkunft_und_Reiter_bleiben_lesbar()
    {
        Zeige();

        foreach (string name in new[] { "reihe", "herkunft", "reiter" })
        {
            KiFeldzugang f = KiMaskenbruecke.Feldzugang(KiMaskennamen.PEAK_SHAVING, name);
            Assert.NotNull(f);
            Assert.False(f.Setzbar, name);
        }
    }

    /// <summary>Der Prüfhaken zieht dieselben Regeln wie der Rechenknopf.</summary>
    [Fact]
    public void Der_Pruefhaken_meldet_ein_verkehrtes_SoC_Band()
    {
        var cut = Zeige();

        KiMaskenhaken haken = KiMaskenbruecke.Haken(KiMaskennamen.PEAK_SHAVING);
        Assert.NotNull(haken);
        Assert.NotNull(haken.Pruefen);
        Assert.True(string.IsNullOrEmpty(haken.Pruefen()));

        KiFeldzugang min = KiMaskenbruecke.Feldzugang(KiMaskennamen.PEAK_SHAVING, "soc_min");
        KiFeldzugang max = KiMaskenbruecke.Feldzugang(KiMaskennamen.PEAK_SHAVING, "soc_max");
        min.Setzen(90.0);
        max.Setzen(10.0);
        cut.Render();

        Assert.False(string.IsNullOrEmpty(haken.Pruefen()));
    }
}
