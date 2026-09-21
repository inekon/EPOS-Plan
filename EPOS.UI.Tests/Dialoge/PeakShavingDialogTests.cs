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
/// Lastspitzenkappung (iU9-W12.6), Vorbild
/// <c>Views/Stromspeicher/Form_PeakShaving</c>.
///
/// <para>Soll ist die Feldkarte: Quellwahl mit zwei Optionen, Ganglinienliste,
/// Dateiwahl, 14 Zahlenfelder, drei Schalter, „Minimale haltbare Schwelle
/// ermitteln", „Berechnen", drei Reiter, „CSV-Export" und „Beenden".</para>
///
/// <para>Gerechnet wird mit der echten Engine über einen synthetischen Lastgang —
/// keine Datenbank, keine Oberfläche des Bestands.</para>
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

    /// <summary>
    /// Ein Zeichenmodell ohne Inhalt — die Prüfstände fragen nur, OB und mit welcher
    /// Schalterstellung der Delegat gerufen wurde; was er zeichnet, misst
    /// <c>PeakShavingBildTests</c> im Kern.
    /// </summary>
    private static Zeichenmodell Leermodell()
        => new Zeichenmodell(200, 100, Farbton.Aus(Farbrolle.HINTERGRUND));

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
        Func<PeakShavingErgebnis, bool, Zeichenmodell?>? modell = null,
        Func<PeakShavingErgebnis, Task<bool>>? csv = null,
        Func<double, bool, Task<bool>>? variante = null,
        Action<bool>? geschlossen = null)
    {
        return Render<PeakShavingDialog>(p => p
            .Add(x => x.VarianteUebernehmen, variante)
            .Add(x => x.Ganglinien, ganglinien ?? Zwei())
            .Add(x => x.Vorgaben, vorgaben ?? Vorgaben())
            .Add(x => x.Werte, werte ?? (i => Task.FromResult(Lastgang())))
            .Add(x => x.DateiWaehlen, waehlen)
            .Add(x => x.Rechnen, rechnen ?? Rechnen)
            .Add(x => x.MinimaleSchwelle, minimal)
            .Add(x => x.Modell, modell)
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
        Assert.Single(leisten[leisten.Count - 1].QuerySelectorAll("button"));   // nur "Beenden"

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
    /// Monatsspitzen; das Zeichenmodell kommt über den Delegaten.
    /// </summary>
    [Fact]
    public void Der_Lauf_fuellt_Kennzahlen_Monate_und_Bild()
    {
        bool socGefragt = false;

        var cut = Zeige(modell: (r, soc) => { socGefragt = soc; return Leermodell(); });

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

        // Der Knopf ist seit iU8-E-2 (Paket P3) ein DIREKTES Rasterkind und
        // steht damit neben der Zielschwelle; das Kindzeichen grenzt ihn gegen
        // den Knopf der Dateiwahl ab, der in seinem Feld steckt.
        cut.FindAll(".epos-formularraster > button")[0].Click();   // btn_Minimal

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
    /// Der SoC-Schalter holt ein neues Zeichenmodell und fragt den Ladezustand
    /// ausdrücklich an — die Sekundärachse des Bildes.
    /// </summary>
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
    /// <b>DL-2, Schritt 6 (Entscheid DL-Q2 a).</b> Die Fußleiste läuft
    /// <b>CSV-Export · In Variante übernehmen · Füller · Beenden</b>; „Beenden" ist
    /// der letzte und der EINZIGE primäre Knopf der Maske.
    /// </summary>
    [Fact]
    public void Die_Fussleiste_laeuft_CSV_Variante_Fueller_Beenden()
    {
        var cut = Zeige(csv: r => Task.FromResult(true),
                        variante: (ziel, adaptiv) => Task.FromResult(true));

        var leisten = cut.FindAll(".epos-dialog > .epos-leiste");
        IElement fuss = leisten[leisten.Count - 1];

        Assert.Equal(new[] { "CSV-Export", "In Variante übernehmen", "Beenden" },
                     fuss.QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray());

        Assert.Single(fuss.QuerySelectorAll(".epos-leiste-fueller"));

        var primaer = fuss.QuerySelectorAll("button.epos-knopf--primaer");
        Assert.Single(primaer);
        Assert.Equal("Beenden", primaer[0].TextContent.Trim());
        Assert.Same(fuss.QuerySelectorAll("button").Last(), primaer[0]);

        // Und in der GANZEN Maske traegt kein zweiter Knopf die Primaerfarbe.
        Assert.Single(cut.FindAll(".epos-dialog button.epos-knopf--primaer"));
    }

    /// <summary>
    /// „Berechnen" bleibt im Blatt zwischen Parametern und Ergebnis — aber ohne
    /// Primärfarbe (DL-Q2 a). Derselbe Knopf, derselbe Handler: Er rechnet weiter.
    /// </summary>
    [Fact]
    public void Der_Rechenknopf_bleibt_im_Blatt_und_traegt_keine_Primaerfarbe()
    {
        var cut = Zeige();

        IElement rechnen = Rechenknopf(cut);
        Assert.Equal("Berechnen", rechnen.TextContent.Trim());
        Assert.DoesNotContain("epos-knopf--primaer", rechnen.ClassName ?? "");

        rechnen.Click();
        Assert.NotEmpty(cut.Instance.Kennzahlen);
    }

    /// <summary>
    /// <b>Befund W12-B24, wörtlich:</b> Der einzige Fußknopf schließt mit
    /// „Abbrechen" — es kommt immer <c>false</c> heraus, auch bei Esc. Der Knopf
    /// heißt seit DL-2 „Beenden"; sein Handler ist derselbe geblieben.
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

    /// <summary>Das Schliesskreuz im Kopf wirkt wie Esc/Fussknopf: es kommt immer false heraus.</summary>
    [Fact]
    public void Das_Kreuz_meldet_ebenfalls_false()
    {
        bool? ergebnis = null;
        var cut = Zeige(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog-zu").Click();
        Assert.False(ergebnis);
    }

    // =====================================================================
    //  Formularraster (Anwenderwunsch iU8-E-2, Paket P3, 05.09.2026)
    // =====================================================================

    /// <summary>
    /// Quelle und Parameter stehen im Formularraster; die sieben handgebauten <c>epos-feldpaar</c>-Kaesten sind fort - der Raster stellt die zwei Feldpaare je Zeile selbst.
    ///
    /// <para>Geprueft wird das MARKUP: Der Block traegt
    /// <c>epos-formularraster</c>, und darin stehen Felder. Was der Raster
    /// daraus MACHT (Beschriftungsspalte, kurzes Feld, zwei Spalten), steht
    /// als Stilblattprobe in <c>FormularrasterTests</c> - eine bunit-Probe
    /// rechnet kein CSS aus (Lehre W6-B-1).</para>
    /// </summary>
    [Fact]
    public void Die_Parameter_stehen_im_Formularraster()
    {
        var cut = Zeige();

        Assert.Empty(cut.FindAll(".epos-feldpaar"));
        Assert.True(cut.FindAll(".epos-formularraster").Count >= 4,
                    "weniger Raster als Bloecke");

        // Leistung [kW], Kapazitaet [kWh], SoC [%]: kurzes Feld, Einheit dahinter.
        Assert.NotEmpty(cut.FindAll(
            ".epos-formularraster .epos-feld--kurz .epos-feld-zeile .epos-einheit"));
    }

    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F6)
    // =====================================================================

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Gelesen wird die
    /// Vorbelegung, gesetzt wird der Leistungspreis — beides über denselben Weg,
    /// den auch eine Hand am Eingabefeld nimmt.
    /// </summary>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_setzt_eine_Zahl()
    {
        var cut = Zeige();

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.PEAK_SHAVING));

        KiFeldzugang lp = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.PEAK_SHAVING, "leistungspreis");
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
    /// <b>Das WAHLFELD „Lastgang" trifft über den angezeigten Text</b> (KI‑D‑Q6):
    /// Der Assistent nennt eine Ganglinie beim Namen, und die Maske lädt sie.
    /// </summary>
    [Fact]
    public void Die_Ganglinie_laesst_sich_ueber_ihren_Namen_setzen()
    {
        var cut = Zeige();

        KiFeldzugang gang = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.PEAK_SHAVING, "ganglinie");
        Assert.NotNull(gang);
        Assert.NotEmpty(gang.Wahleintraege());

        KiFeldumsetzung wahl = KiFeldwandler.Wandle(gang, "Auslieferung");
        Assert.True(wahl.Ok, wahl.Grund);
        gang.Setzen(wahl.Wert);
        cut.Render();

        Assert.Equal(1, Convert.ToInt32(gang.Lesen(), CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// <b>Die drei abgeleiteten Felder sind NICHT setzbar</b> — Reihenzeile,
    /// Herkunft und das offene Blatt. Ein Blattwechsel ist eine Bedienhandlung.
    /// </summary>
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

    /// <summary>
    /// <b>Der Prüfhaken zieht dieselben Regeln wie der Rechenknopf.</b> Ein SoC-Band
    /// verkehrt herum ist ein Befund — und zwar derselbe, den die Maske zeigt.
    /// </summary>
    [Fact]
    public void Der_Pruefhaken_meldet_ein_verkehrtes_SoC_Band()
    {
        var cut = Zeige();

        KiMaskenhaken haken = KiMaskenbruecke.Haken(KiMaskennamen.PEAK_SHAVING);
        Assert.NotNull(haken);
        Assert.NotNull(haken.Pruefen);

        // Mit der Vorbelegung steht alles.
        Assert.True(string.IsNullOrEmpty(haken.Pruefen()));

        KiFeldzugang min = KiMaskenbruecke.Feldzugang(KiMaskennamen.PEAK_SHAVING, "soc_min");
        KiFeldzugang max = KiMaskenbruecke.Feldzugang(KiMaskennamen.PEAK_SHAVING, "soc_max");
        min.Setzen(90.0);
        max.Setzen(10.0);
        cut.Render();

        Assert.False(string.IsNullOrEmpty(haken.Pruefen()));
    }
}
