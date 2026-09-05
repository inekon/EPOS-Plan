using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Lastspitzenkappung (iU9-W12.6), Vorbild
/// <c>Views/Stromspeicher/Form_PeakShaving</c>.
///
/// <para>Soll ist die Feldkarte: Quellwahl mit zwei Optionen, Ganglinienliste,
/// Dateiwahl, 14 Zahlenfelder, drei Schalter, „Minimale haltbare Schwelle
/// ermitteln", „Berechnen", drei Reiter, „CSV-Export" und „Schließen".</para>
///
/// <para>Gerechnet wird mit der echten Engine über einen synthetischen Lastgang —
/// keine Datenbank, keine Oberfläche des Bestands.</para>
/// </summary>
public class PeakShavingDialogTests : BunitContext
{
    private readonly CultureInfo _kulturVorher = CultureInfo.CurrentUICulture;
    private readonly CultureInfo _zahlenVorher = CultureInfo.CurrentCulture;

    public PeakShavingDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
    }

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentUICulture = _kulturVorher;
        CultureInfo.CurrentCulture = _zahlenVorher;
        base.Dispose(disposing);
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

    /// <summary>Der echte Rechenweg — dieselbe Engine wie in der Hülle.</summary>
    private static Task<PeakShavingErgebnis> Rechnen(double[] reihe, PeakShavingEingaben e)
        => Task.FromResult(new PeakShaving(e.AlsPeakShavingParameter(), e.Modus)
                               .BerechnePeakShaving(reihe, e.AlsSpeicherParameter()));

    private IRenderedComponent<PeakShavingDialog> Zeige(
        IReadOnlyList<(int Id, string Text)>? ganglinien = null,
        PeakShavingVorbelegung? vorgaben = null,
        Func<int, Task<double[]>>? werte = null,
        Func<string, Task<string?>>? waehlen = null,
        Func<double[], PeakShavingEingaben, Task<PeakShavingErgebnis>>? rechnen = null,
        Func<double[], PeakShavingEingaben, Task<double>>? minimal = null,
        Func<PeakShavingErgebnis, bool, Task<byte[]?>>? bild = null,
        Func<PeakShavingErgebnis, Task<bool>>? csv = null,
        Action<bool>? geschlossen = null)
    {
        return Render<PeakShavingDialog>(p => p
            .Add(x => x.Ganglinien, ganglinien ?? Zwei())
            .Add(x => x.Vorgaben, vorgaben ?? Vorgaben())
            .Add(x => x.Werte, werte ?? (i => Task.FromResult(Lastgang())))
            .Add(x => x.DateiWaehlen, waehlen)
            .Add(x => x.Rechnen, rechnen ?? Rechnen)
            .Add(x => x.MinimaleSchwelle, minimal)
            .Add(x => x.Bild, bild)
            .Add(x => x.CsvSpeichern, csv)
            .Add(x => x.Geschlossen, (bool ok) => geschlossen?.Invoke(ok)));
    }

    private static IElement Feld(IRenderedComponent<PeakShavingDialog> cut, int i)
        => cut.FindAll("input.epos-eingabe")[i];

    private static IElement Rechenknopf(IRenderedComponent<PeakShavingDialog> cut)
        => cut.Find(".epos-peakshaving-rechnen button");

    // =====================================================================
    // Feldbestand
    // =====================================================================

    /// <summary>
    /// Die Feldkarte: zwei Quelloptionen, eine Ganglinienliste, 14 Zahlenfelder,
    /// zwei Schalter im Parameterblock, „Minimal" und „Berechnen", drei Reiter.
    /// </summary>
    [Fact]
    public void Der_Dialog_zeigt_Quelle_vierzehn_Zahlenfelder_und_drei_Reiter()
    {
        var cut = Zeige();

        Assert.Contains("Lastspitzenkappung", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal(2, cut.FindAll("input[type=radio]").Count);
        Assert.Equal(14, cut.FindAll("input.epos-eingabe").Count);
        Assert.Equal(2, cut.FindAll("input[type=checkbox]").Count);   // adaptiv, kompatibel
        Assert.Equal(3, cut.FindAll("[role=tab]").Count);
        Assert.NotNull(Rechenknopf(cut));
    }

    /// <summary>Ohne Wähler bleibt die Dateiwahl weg, ohne CSV-Delegat der CSV-Knopf.</summary>
    [Fact]
    public void Ohne_Delegat_bleibt_der_Knopf_weg()
    {
        var cut = Zeige();
        Assert.Empty(cut.FindAll(".epos-dateiwahl"));

        var leisten = cut.FindAll(".epos-dialog > .epos-leiste");
        Assert.Single(leisten[leisten.Count - 1].QuerySelectorAll("button"));   // nur "Schliessen"

        var mit = Zeige(waehlen: p => Task.FromResult<string?>(""),
                        csv: r => Task.FromResult(true));
        Assert.Single(mit.FindAll(".epos-dateiwahl"));
        var leistenMit = mit.FindAll(".epos-dialog > .epos-leiste");
        Assert.Equal(2, leistenMit[leistenMit.Count - 1].QuerySelectorAll("button").Length);
    }

    /// <summary>
    /// Die Vorbelegung steht in den Feldern, und die Herkunftszeile nennt den
    /// Speicher — wörtlich <c>PEAK_HERKUNFT_PROJEKT</c>.
    /// </summary>
    [Fact]
    public void Die_Vorbelegung_steht_in_den_Feldern()
    {
        var cut = Zeige(vorgaben: Vorgaben(ausProjekt: true));

        Assert.Equal("100", Feld(cut, 0).GetAttribute("value"));      // P [kW]
        Assert.Equal("200", Feld(cut, 1).GetAttribute("value"));      // Kapazitaet
        Assert.Contains("Speicher A", cut.Markup);
    }

    /// <summary>
    /// Wörtlich (:227-228): Gibt es keine Ganglinie, steht die Quelle von vornherein
    /// auf „Datei importieren".
    /// </summary>
    [Fact]
    public void Ohne_Ganglinie_steht_die_Quelle_auf_Datei()
    {
        var leer = Zeige(ganglinien: Array.Empty<(int, string)>());
        Assert.True(leer.FindAll("input[type=radio]")[1].HasAttribute("checked"));

        var voll = Zeige();
        Assert.True(voll.FindAll("input[type=radio]")[0].HasAttribute("checked"));
    }

    /// <summary>
    /// Die erste Ganglinie ist beim Aufbau geladen — der Vorläufer rief
    /// <c>QuelleGeaendert</c> am Ende des Konstruktors.
    /// </summary>
    [Fact]
    public void Die_erste_Ganglinie_ist_beim_Aufbau_geladen()
    {
        var cut = Zeige();

        Assert.NotNull(cut.Instance.Lastgang);
        Assert.Equal(35040, cut.Instance.Lastgang!.Count);
        Assert.Contains("35.040", cut.Markup);           // "… 35.040 Werte, Jahresmaximum 400 kW"
        Assert.Contains("400", cut.Markup);
    }

    // =====================================================================
    // Der Schalter „adaptiv"
    // =====================================================================

    /// <summary>
    /// Solange nachgezogen wird, ist die Zielschwelle gesperrt — sie bleibt dabei
    /// SICHTBAR und lesbar (Hausregel `Aktiv`).
    /// </summary>
    [Fact]
    public void Die_Zielschwelle_ist_nur_im_festen_Betrieb_bedienbar()
    {
        var cut = Zeige();

        Assert.True(Feld(cut, 6).HasAttribute("disabled"));           // tb_Ziel
        cut.FindAll("input[type=checkbox]")[0].Change(false);         // adaptiv aus
        Assert.False(Feld(cut, 6).HasAttribute("disabled"));
    }

    // =====================================================================
    // Rechnen
    // =====================================================================

    /// <summary>Ohne Reihe meldet der Dialog und rechnet nicht.</summary>
    [Fact]
    public void Ohne_Lastgang_meldet_der_Rechenknopf()
    {
        var cut = Zeige(ganglinien: Array.Empty<(int, string)>());

        Rechenknopf(cut).Click();

        Assert.Contains("Bitte zuerst einen Lastgang", cut.Instance.Meldung);
        Assert.Empty(cut.Instance.Kennzahlen);
    }

    /// <summary>
    /// Der Lauf füllt die 17 Kennzahlzeilen (plus drei Trenner) und die
    /// Monatsspitzen; das Bild kommt über den Delegaten.
    /// </summary>
    [Fact]
    public void Der_Lauf_fuellt_Kennzahlen_Monate_und_Bild()
    {
        byte[] gemalt = new byte[] { 1, 2, 3 };
        bool socGefragt = false;

        var cut = Zeige(bild: (r, soc) => { socGefragt = soc; return Task.FromResult<byte[]?>(gemalt); });

        Rechenknopf(cut).Click();

        Assert.Equal(21, cut.Instance.Kennzahlen.Count);          // 18 Zeilen + 3 Trenner
        Assert.Contains(cut.Instance.Kennzahlen, z => z.Bezeichnung.Contains("Lastspitze"));
        Assert.False(socGefragt);                                  // der Schalter steht aus

        // Der Kennzahlenreiter ist der erste und zeigt seine Zeilen.
        Assert.True(cut.FindAll(".epos-raster tbody tr").Count >= 21);
    }

    /// <summary>
    /// Eine verletzte Fachregel blockiert den Lauf und meldet wörtlich — hier die
    /// Kapazität (<c>PEAK_MSG_KAPAZITAET</c>).
    /// </summary>
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
    /// Ein leeres PFLICHTfeld blockiert ebenfalls und nennt seinen Namen — der
    /// Ersatz für die 13 <c>Program.ZahlPruefen</c>-Meldungen (Befund W12-B9).
    /// Leistungspreis und die drei Investitionsanteile dürfen dagegen leer bleiben.
    /// </summary>
    [Fact]
    public void Ein_leeres_Pflichtfeld_blockiert_ein_leerer_Preis_nicht()
    {
        var cut = Zeige();

        Feld(cut, 7).Input("");             // Leistungspreis - leer erlaubt
        Feld(cut, 10).Input("");            // c_cap - leer erlaubt
        Rechenknopf(cut).Click();
        Assert.NotEmpty(cut.Instance.Kennzahlen);

        Feld(cut, 0).Input("");             // Leistung P - Pflicht
        Rechenknopf(cut).Click();
        Assert.Contains("Leistung", cut.Instance.Meldung);
    }

    /// <summary>
    /// „Minimale haltbare Schwelle ermitteln" übernimmt den Wert in das
    /// Schwellenfeld UND schaltet die adaptive Suche aus — wörtlich (:511-512).
    /// </summary>
    [Fact]
    public void Die_minimale_Schwelle_landet_im_Feld_und_schaltet_adaptiv_aus()
    {
        var cut = Zeige(minimal: (r, e) => Task.FromResult(321.5));

        cut.FindAll(".epos-feldpaar button")[0].Click();     // btn_Minimal

        Assert.False(cut.FindAll("input[type=checkbox]")[0].HasAttribute("checked"));
        Assert.Equal("321,5", Feld(cut, 6).GetAttribute("value"));
        Assert.Contains("321,5", cut.Instance.Meldung);
    }

    /// <summary>
    /// Reißt der Speicher die Schwelle, meldet der Dialog es als Warnung —
    /// <c>PEAK_MSG_GERISSEN</c> mit der neuen Spitze.
    /// </summary>
    [Fact]
    public void Eine_gerissene_Schwelle_wird_gemeldet()
    {
        var cut = Zeige();

        cut.FindAll("input[type=checkbox]")[0].Change(false);   // adaptiv aus
        Feld(cut, 6).Input("120");                              // unhaltbar niedrig
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

    /// <summary>
    /// Der SoC-Schalter zeichnet neu und fragt den Ladezustand ausdrücklich an —
    /// die Sekundärachse des Bildes.
    /// </summary>
    [Fact]
    public void Der_SoC_Schalter_zeichnet_das_Bild_neu()
    {
        bool? letzterSoc = null;
        var cut = Zeige(bild: (r, soc) => { letzterSoc = soc; return Task.FromResult<byte[]?>(new byte[] { 9 }); });

        Rechenknopf(cut).Click();
        Assert.False(letzterSoc);

        cut.FindAll("[role=tab]")[1].Click();                     // Reiter "Chart"
        cut.Find("[role=tabpanel] input[type=checkbox]").Change(true);

        Assert.True(letzterSoc);
    }

    /// <summary>
    /// Eine neue Quelle verwirft das Ergebnis — wörtlich <c>QuelleGeaendert</c>
    /// (:275-277).
    /// </summary>
    [Fact]
    public void Ein_Quellwechsel_verwirft_das_Ergebnis()
    {
        var cut = Zeige();

        Rechenknopf(cut).Click();
        Assert.NotEmpty(cut.Instance.Kennzahlen);

        cut.FindAll("input[type=radio]")[1].Change("1");           // auf "Datei"
        Assert.Empty(cut.Instance.Kennzahlen);
    }

    /// <summary>Ohne Ergebnis meldet der CSV-Knopf „Bitte zuerst rechnen."</summary>
    [Fact]
    public void Der_CSV_Knopf_meldet_ohne_Ergebnis()
    {
        bool geschrieben = false;
        var cut = Zeige(csv: r => { geschrieben = true; return Task.FromResult(true); });

        cut.FindAll(".epos-dialog > .epos-leiste")[1].QuerySelector("button")!.Click();

        Assert.False(geschrieben);
        Assert.Contains("Bitte zuerst rechnen", cut.Instance.Meldung);
    }

    // =====================================================================
    // Schluss
    // =====================================================================

    /// <summary>
    /// <b>Befund W12-B24, wörtlich:</b> Der einzige Fußknopf schließt mit
    /// „Abbrechen" — es kommt immer <c>false</c> heraus, auch bei Esc.
    /// </summary>
    [Fact]
    public void Der_Fussknopf_und_Esc_melden_beide_false()
    {
        bool? ergebnis = null;
        var cut = Zeige(geschlossen: b => ergebnis = b);

        var leisten = cut.FindAll(".epos-dialog > .epos-leiste");
        leisten[leisten.Count - 1].QuerySelectorAll("button").Last().Click();
        Assert.False(ergebnis);

        ergebnis = null;
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(ergebnis);
    }
}
