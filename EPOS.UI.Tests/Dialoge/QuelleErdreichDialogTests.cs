using System.Globalization;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Simulation;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// QuelleErdreichDialog (iU9-W10a.3) - der Ersatz fuer Form_QuelleErdreich.
///
/// <para>FELDBESTAND laut Feldkarte: 30 Steuerelemente plus das Diagramm - zwei
/// Wahlknoepfe (Kollektor/Sonde), vier Zahlenfelder je Zweig, zwei Klapplisten
/// (Bodentyp, Klimazone), das Spreizungsfeld, der Kartenknopf, drei
/// Herleitungszeilen, die Kennwertzeile, der Simulationsknopf, OK und Abbrechen.</para>
///
/// <para>KEINE DATENBANK: Die Fachrechnung liegt in ErdreichTemperatur,
/// VDI4640Pruefung und ErdreichAuswertung; alle drei rechnen aus dem uebergebenen
/// Aussentemperaturvektor bzw. dem Prozessspeicher.</para>
/// </summary>
public class QuelleErdreichDialogTests : EposBunitContext
{
    public QuelleErdreichDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Ein Aussentemperaturvektor, damit die Vorschau etwas zu rechnen hat.</summary>
    private static double[] Aussen()
    {
        var w = new double[8760];
        for (int i = 0; i < w.Length; i++)
            w[i] = (double)(9 + 12 * System.Math.Sin(2 * System.Math.PI * i / 8760.0 - System.Math.PI / 2));
        return w;
    }

    private static QuelleErdreichDaten Kollektor() => new()
    {
        WPName = "WP Erdgeschoss",
        IdProjekt = 1030,
        IdAnlage = 77,
        Quellsystem = ErdreichTemperatur.QUELLSYSTEM_KOLLEKTOR,
        Tiefe = 1.8,
        Flaeche = 250,
        Anzahl = 0,
        Bodentyp = ErdreichTemperatur.BODENTYP_DEFAULT,
        Klimazone = 6,
        Spreizung = 4,
        Aussentemperatur = Aussen()
    };

    private static QuelleErdreichDaten Sonde() => Kollektor() with
    {
        Quellsystem = ErdreichTemperatur.QUELLSYSTEM_SONDE,
        Tiefe = 120,
        Flaeche = 0,
        Anzahl = 4
    };

    private IRenderedComponent<QuelleErdreichDialog> Zeige(
        QuelleErdreichDaten daten,
        Action<QuelleErdreichDaten?>? geschlossen = null,
        ErdreichAuswertung.ErdreichLaufErgebnis? lauf = null,
        Func<QuelleErdreichDaten, Task<(ErdreichAuswertung.ErdreichLaufErgebnis?, string?)>>? simulieren = null,
        bool titelAnzeigen = true,
        Func<double[], double[]?, double[]?, Task<Zeichenmodell?>>? modell = null)
    {
        return Render<QuelleErdreichDialog>(p =>
        {
            p.Add(x => x.Daten, daten);
            p.Add(x => x.Lauf, lauf ?? ErdreichAuswertung.ErdreichLaufErgebnis.Keines);
            if (!titelAnzeigen) p.Add(x => x.TitelAnzeigen, false);
            if (geschlossen is not null) p.Add(x => x.Geschlossen, geschlossen);
            if (simulieren is not null) p.Add(x => x.Simulieren, simulieren);
            if (modell is not null) p.Add(x => x.Jahresgangmodell, modell);
        });
    }

    /// <summary>
    /// Die VORSCHAU als Zeichenmodell — EINE Instanz, EINMAL gebaut: Der Baustein
    /// <c>DiagrammSvg</c> vergleicht die Modellreferenz, und ein je Zeichenlauf neu
    /// gebautes Modell setzte seinen Baum jedes Mal neu.
    /// </summary>
    private readonly Zeichenmodell _vorschau = Vorschau();

    private static Zeichenmodell Vorschau()
    {
        var quelle = new double[168];
        for (int i = 0; i < quelle.Length; i++)
            quelle[i] = 8.0 + 2.0 * Math.Sin(2 * Math.PI * i / 24.0);

        return ChartRenderer.JahresgangModell(
            "Jahresgang der Quelltemperatur",
            new[]
            {
                new ChartRenderer.Reihe("Quelltemperatur", quelle,
                                        ChartRenderer.C_QUELLTEMPERATUR)
            },
            "Monat", "Quelltemperatur [°C]");
    }

    /// <summary>Ein belastbares Laufergebnis — die Prüfung rechnet damit.</summary>
    private static ErdreichAuswertung.ErdreichLaufErgebnis MitLauf(double maxEntzug = 9000)
        => new(true, true, maxEntzug, 18000, 1800, "", "", "");

    // ================================================================== Feldbestand

    [Fact]
    public void Der_Feldbestand_steht_vollstaendig()
    {
        var cut = Zeige(Kollektor());

        Assert.Equal("Wärmequelle Erdreich — WP Erdgeschoss",
                     cut.Find("h1.epos-dialog-titel").TextContent);

        // Zwei Wahlknoepfe (Kollektor/Sonde), je einer in seiner Rubrik.
        Assert.Equal(2, cut.FindAll("input[type=radio]").Count);

        // Vier Zweigfelder + fuenf Felder des Sondenfeldes + Spreizung = zehn Zahlenfelder.
        Assert.Equal(10, cut.FindAll("input.epos-eingabe").Count);

        // Drei Klapplisten: Anordnung des Sondenfeldes, Bodentyp und Klimazone.
        Assert.Equal(3, cut.FindAll("select").Count);

        Assert.NotNull(cut.Find("button.epos-infoknopf"));
        Assert.Equal(2, cut.FindAll(".epos-leiste button").Count);
    }

    /// <summary>
    /// Die Klimazonenliste traegt Zone 0 („nicht zugeordnet") plus 1…15 - genau
    /// KatalogeFuellen:669-679.
    /// </summary>
    [Fact]
    public void Die_Klimazonenliste_traegt_die_Null_und_fuenfzehn_Zonen()
    {
        var cut = Zeige(Kollektor());
        var zonen = cut.FindAll("select")[2].QuerySelectorAll("option");

        Assert.Equal(1 + VDI4640Pruefung.KLIMAZONEN, zonen.Length);
        Assert.Equal("0 — nicht zugeordnet", zonen[0].TextContent);
        Assert.Contains("h/a", zonen[1].TextContent);
    }

    // ================================================================== Vorbelegung

    /// <summary>
    /// SetControls:736-780, woertlich: Beim KOLLEKTOR steht die gespeicherte Tiefe im
    /// Tiefenfeld und die feste 90 im Laengenfeld; bei der SONDE ist es umgekehrt -
    /// die gespeicherte Tiefe IST dort die Sondenlaenge.
    /// </summary>
    [Fact]
    public void Die_Vorbelegung_folgt_dem_Zweig()
    {
        var kollektor = Zeige(Kollektor()).Instance;
        Assert.False(kollektor.IstSonde);
        Assert.Equal(1.8, kollektor.Zweigfelder.Tiefe);
        Assert.Equal(250.0, kollektor.Zweigfelder.Flaeche);
        Assert.Equal(90.0, kollektor.Zweigfelder.Laenge);
        Assert.Equal(1, kollektor.Zweigfelder.Anzahl);

        var sonde = Zeige(Sonde()).Instance;
        Assert.True(sonde.IstSonde);
        Assert.Equal(120.0, sonde.Zweigfelder.Laenge);
        Assert.Equal(4, sonde.Zweigfelder.Anzahl);
        Assert.Equal(ErdreichTemperatur.TIEFE_DEFAULT, sonde.Zweigfelder.Tiefe);
    }

    /// <summary>
    /// Ohne gespeicherte Werte gelten die Vorgaben aus VorgabenSetzen:688-695 -
    /// Tiefe TIEFE_DEFAULT, Laenge 90, Anzahl 1, Spreizung SPREIZUNG_DEFAULT.
    /// </summary>
    [Fact]
    public void Ohne_gespeicherte_Werte_gelten_die_Vorgaben()
    {
        var cut = Zeige(new QuelleErdreichDaten { Aussentemperatur = Aussen() });

        Assert.Equal(ErdreichTemperatur.TIEFE_DEFAULT, cut.Instance.Zweigfelder.Tiefe);
        Assert.Equal(90.0, cut.Instance.Zweigfelder.Laenge);
        Assert.Equal(1, cut.Instance.Zweigfelder.Anzahl);
        Assert.Equal(ErdreichTemperatur.BODENTYP_DEFAULT, cut.Instance.Bodentyp);
        Assert.Equal(0, cut.Instance.Klimazone);
    }

    /// <summary>
    /// ABWEICHUNG A-4 (Befund W10-B11): Das Umschalten SPERRT nur; beide Zweige
    /// behalten ihre Werte. Im Vorlaeufer ueberschrieb SetControls den jeweils anderen
    /// Zweig mit seiner Vorgabe, und wer umschaltete, verlor still, was er dort gerade
    /// eingetippt hatte.
    /// </summary>
    [Fact]
    public void Das_Umschalten_erhaelt_beide_Zweige()
    {
        var cut = Zeige(Kollektor());

        // Im Sondenzweig etwas eintragen …
        cut.FindAll("input[type=radio]")[1].Change("1");
        Assert.True(cut.Instance.IstSonde);
        cut.FindAll("input.epos-eingabe")[2].Input("150");
        Assert.Equal(150.0, cut.Instance.Zweigfelder.Laenge);

        // … zurueckschalten: die Kollektorwerte stehen unveraendert da …
        cut.FindAll("input[type=radio]")[0].Change("0");
        Assert.False(cut.Instance.IstSonde);
        Assert.Equal(1.8, cut.Instance.Zweigfelder.Tiefe);
        Assert.Equal(250.0, cut.Instance.Zweigfelder.Flaeche);

        // … und die Sondenlaenge ebenfalls.
        Assert.Equal(150.0, cut.Instance.Zweigfelder.Laenge);
    }

    /// <summary>Das Umschalten sperrt die Felder des anderen Zweigs, ohne sie zu verbergen.</summary>
    [Fact]
    public void Das_Umschalten_sperrt_den_anderen_Zweig()
    {
        var cut = Zeige(Kollektor());
        var felder = cut.FindAll("input.epos-eingabe");

        Assert.False(felder[0].HasAttribute("disabled"));   // Tiefe
        Assert.False(felder[1].HasAttribute("disabled"));   // Flaeche
        Assert.True(felder[2].HasAttribute("disabled"));    // Laenge
        Assert.True(felder[3].HasAttribute("disabled"));    // Anzahl

        cut.FindAll("input[type=radio]")[1].Change("1");
        felder = cut.FindAll("input.epos-eingabe");
        Assert.True(felder[0].HasAttribute("disabled"));
        Assert.False(felder[2].HasAttribute("disabled"));
    }

    // ===================================================================== Rubriken

    /// <summary>
    /// Anwenderwunsch 14.09.2026: Erdkollektor und Erdsonde sind ZWEI Rubriken mit je
    /// ihren Parametern - nicht alle vier Felder gemischt unter einer Ueberschrift.
    /// Jede Rubrik traegt als erste Zeile ihr Optionsfeld.
    /// </summary>
    [Fact]
    public void Erdkollektor_und_Erdsonde_sind_zwei_Rubriken_mit_ihren_Feldern()
    {
        var cut = Zeige(Kollektor());
        var rubriken = cut.FindAll("section.epos-gruppenkopf");

        Assert.Equal("Erdkollektor", rubriken[0].QuerySelector(".epos-gruppenkopf-titel")!.TextContent);
        Assert.Equal("Erdsonde", rubriken[1].QuerySelector(".epos-gruppenkopf-titel")!.TextContent);

        // Rubrik 1: ein Optionsfeld, dann Verlegetiefe und Flaeche - sonst nichts.
        Assert.Single(rubriken[0].QuerySelectorAll("input[type=radio]"));
        string[] kollektor = Feldnamen(rubriken[0]);
        Assert.Equal(new[] { "Verlegetiefe:", "Fläche:" }, kollektor);

        // Rubrik 2: ein Optionsfeld, dann Laenge je Sonde und Anzahl Sonden.
        Assert.Single(rubriken[1].QuerySelectorAll("input[type=radio]"));
        string[] sonde = Feldnamen(rubriken[1]);
        Assert.Equal(new[]
            {
                "Länge je Sonde:", "Anzahl Sonden:", "Sondenabstand:", "Anordnung:", "Bohrlochdurchmesser:",
                "Bohrlochwiderstand:", "Kopfüberdeckung:", "Betrachtungsjahr:",
            }, sonde);
    }

    /// <summary>Die Beschriftungen der Felder einer Rubrik, in Anzeigereihenfolge.</summary>
    private static string[] Feldnamen(AngleSharp.Dom.IElement rubrik)
    {
        var namen = new List<string>();
        foreach (var feld in rubrik.QuerySelectorAll(".epos-feld"))
        {
            var text = feld.QuerySelector(".epos-feld-text");
            if (text is not null) namen.Add(text.TextContent);
        }
        return namen.ToArray();
    }

    /// <summary>
    /// Die zwei Optionsfelder sind EINE Wahl: gleicher HTML-Name (Browser, Tastatur und
    /// Sprachausgabe lesen sie als Alternative) und immer GENAU EINES gewaehlt.
    /// </summary>
    [Fact]
    public void Die_zwei_Optionsfelder_sind_eine_einzige_Wahl()
    {
        var cut = Zeige(Kollektor());
        var wahl = cut.FindAll("input[type=radio]");

        string name = wahl[0].GetAttribute("name")!;
        Assert.False(string.IsNullOrEmpty(name));
        Assert.Equal(name, wahl[1].GetAttribute("name"));

        Assert.True(wahl[0].HasAttribute("checked"));
        Assert.False(wahl[1].HasAttribute("checked"));

        wahl[1].Change("1");
        cut.WaitForAssertion(() =>
        {
            var neu = cut.FindAll("input[type=radio]");
            Assert.False(neu[0].HasAttribute("checked"));
            Assert.True(neu[1].HasAttribute("checked"));
        });
    }

    /// <summary>
    /// Die nicht gewaehlte Rubrik bleibt STEHEN: ihre Felder sind da, gesperrt und
    /// leise gestellt (epos-erdreich-zweig--ruht). Umschalten wandert die Kennzeichnung
    /// mit, die Werte bleiben (A-4).
    /// </summary>
    [Fact]
    public void Die_ruhende_Rubrik_bleibt_stehen_und_wird_leise()
    {
        var cut = Zeige(Kollektor());
        var zweige = cut.FindAll(".epos-erdreich-zweig");

        Assert.Equal(2, zweige.Count);
        Assert.False(zweige[0].ClassList.Contains("epos-erdreich-zweig--ruht"));
        Assert.True(zweige[1].ClassList.Contains("epos-erdreich-zweig--ruht"));

        // Die Felder der ruhenden Rubrik stehen weiter da - gesperrt, nicht verborgen.
        // Laenge, Anzahl und die fuenf Zahlenfelder des Sondenfeldes.
        Assert.Equal(7, zweige[1].QuerySelectorAll("input.epos-eingabe").Length);
        foreach (var feld in zweige[1].QuerySelectorAll("input.epos-eingabe"))
            Assert.True(feld.HasAttribute("disabled"));

        cut.FindAll("input[type=radio]")[1].Change("1");
        cut.WaitForAssertion(() =>
        {
            var neu = cut.FindAll(".epos-erdreich-zweig");
            Assert.True(neu[0].ClassList.Contains("epos-erdreich-zweig--ruht"));
            Assert.False(neu[1].ClassList.Contains("epos-erdreich-zweig--ruht"));
        });

        // Die Werte des ruhenden Zweigs bleiben unangetastet.
        Assert.Equal(1.8, cut.Instance.Zweigfelder.Tiefe);
        Assert.Equal(250.0, cut.Instance.Zweigfelder.Flaeche);
    }

    // ================================================================== Bodenkennwerte

    /// <summary>
    /// ABWEICHUNG A-3 (Befund W10-B6): Der Bodentyp ist SCHLUESSELGEKOPPELT. Der
    /// Vorlaeufer las ihn ueber den Listenindex; wird der Katalog umsortiert, zeigen
    /// Bestandsprojekte danach auf den falschen Boden.
    /// </summary>
    [Fact]
    public void Der_Bodentyp_kommt_als_Katalogschluessel_zurueck()
    {
        var cut = Zeige(Kollektor());

        int index = ErdreichTemperatur.KatalogIndex(ErdreichTemperatur.BODENTYP_DEFAULT);
        Assert.Equal(ErdreichTemperatur.Katalog[index].Schluessel, cut.Instance.Bodentyp);

        // Ein anderer Katalogeintrag - der Schluessel folgt der Auswahl, nicht der
        // Anzeigeposition.
        int anderer = index == 0 ? 1 : 0;
        cut.FindAll("select")[1].Change(anderer.ToString());
        Assert.Equal(ErdreichTemperatur.Katalog[anderer].Schluessel, cut.Instance.Bodentyp);
    }

    [Fact]
    public void Die_Bodenkennwerte_stehen_zum_gewaehlten_Katalogeintrag()
    {
        var cut = Zeige(Kollektor());

        Assert.Contains("λ = ", cut.Instance.Bodenkennwerte);
        Assert.Contains("Dämpfungstiefe", cut.Instance.Bodenkennwerte);

        string vorher = cut.Instance.Bodenkennwerte;
        int index = ErdreichTemperatur.KatalogIndex(ErdreichTemperatur.BODENTYP_DEFAULT);
        cut.FindAll("select")[1].Change((index == 0 ? 1 : 0).ToString());
        Assert.NotEqual(vorher, cut.Instance.Bodenkennwerte);
    }

    /// <summary>
    /// Ohne Klimadaten haengt die Kennwertzeile den Ersatzwert-Hinweis an
    /// (SIMQ_ERDREICH_OHNE_KLIMADATEN, Aktualisieren:849-850).
    /// </summary>
    [Fact]
    public void Ohne_Klimadaten_sagt_es_die_Kennwertzeile()
    {
        var mit = Zeige(Kollektor()).Instance;
        var ohne = Zeige(Kollektor() with { Aussentemperatur = null }).Instance;

        Assert.DoesNotContain("ohne Klimadaten", mit.Kennwertzeile);
        Assert.Contains("ohne Klimadaten", ohne.Kennwertzeile);
    }

    // ================================================================== Prüfung

    /// <summary>
    /// OHNE Lauf steht der Hinweis „(noch kein Simulationslauf)" da, und es wird
    /// NICHTS gerechnet (PruefungAktualisieren:863-877).
    /// </summary>
    [Fact]
    public void Ohne_Lauf_steht_der_Hinweis_statt_der_Pruefung()
    {
        var cut = Zeige(Kollektor());

        Assert.Contains("noch kein Simulationslauf", cut.Instance.Pruefungstext);
        Assert.False(cut.Instance.PruefungWarnt);
    }

    /// <summary>Mit Lauf rechnet die Pruefung — beide Zweige, mit ihrer je eigenen Regel.</summary>
    [Fact]
    public void Mit_Lauf_rechnet_die_Pruefung_in_beiden_Zweigen()
    {
        var kollektor = Zeige(Kollektor(), lauf: MitLauf()).Instance;
        Assert.DoesNotContain("noch kein Simulationslauf", kollektor.Pruefungstext);
        Assert.NotEqual("", kollektor.Pruefungstext);

        var sonde = Zeige(Sonde(), lauf: MitLauf()).Instance;
        Assert.DoesNotContain("noch kein Simulationslauf", sonde.Pruefungstext);
        Assert.NotEqual("", sonde.Pruefungstext);
        Assert.NotEqual(kollektor.Pruefungstext, sonde.Pruefungstext);
    }

    /// <summary>
    /// Eine ueberschrittene Grenze WARNT (im Vorlaeufer: Firebrick). Eine winzige
    /// Kollektorflaeche bei hoher Entzugsleistung ist der sichere Weg dorthin.
    /// </summary>
    [Fact]
    public void Eine_ueberschrittene_Grenze_warnt()
    {
        var eng = Kollektor() with { Flaeche = 5 };
        var cut = Zeige(eng, lauf: MitLauf(40000));

        Assert.True(cut.Instance.PruefungWarnt);
        Assert.Single(cut.FindAll(".epos-warnbanner"));
    }

    /// <summary>
    /// Ein Hinweis ANSTELLE der Pruefung (Luft-Wasser oder nicht belastbar) steht
    /// woertlich da, und es wird nicht gerechnet.
    /// </summary>
    [Fact]
    public void Ein_Ergebnishinweis_ersetzt_die_Pruefung()
    {
        var lauf = new ErdreichAuswertung.ErdreichLaufErgebnis(
            true, false, 0, 0, 0, "Luft-Wasser: wird nicht gerechnet", "", "");
        var cut = Zeige(Kollektor(), lauf: lauf);

        Assert.Equal("Luft-Wasser: wird nicht gerechnet", cut.Instance.Pruefungstext);
        Assert.False(cut.Instance.PruefungWarnt);
    }

    /// <summary>
    /// EQ1: Ein GESPEICHERTES Ergebnis (aus Tab_ErgebnisErdreich, kein Lauf in dieser Sitzung)
    /// nennt seinen Lauf; ein Lauf der Sitzung (ohne Laufstempel) nicht.
    /// </summary>
    [Fact]
    public void Ein_gespeichertes_Ergebnis_nennt_den_Stand_des_Laufs()
    {
        var gespeichert = MitLauf() with { Laufstempel = "2026-10-03 09:30:00" };
        var cut = Zeige(Kollektor(), lauf: gespeichert);
        Assert.Equal("Stand des Laufs vom 2026-10-03 09:30:00.", cut.Find(".epos-erdreich-laufstand").TextContent.Trim());

        Assert.Empty(Zeige(Kollektor(), lauf: MitLauf()).FindAll(".epos-erdreich-laufstand"));
    }

    // ================================================================== Vorprüfung und Sondenhinweis

    /// <summary>Auslegungswerte eines Sole-Geräts: 10 kW, COP 4,5 bei B0/W35.</summary>
    private static WpAuslegung[] EinGeraet()
        => new[] { new WpAuslegung("WP Sole", 10, 4.5, "B0/W35") };

    /// <summary>
    /// Bei der ERDSONDE sagt eine leise Zeile unter der Vorschau, dass sie die ungestörte
    /// Temperatur zeigt und die Soletemperatur im Lauf mit dem Entzug sinkt; beim Kollektor
    /// steht sie nicht.
    /// </summary>
    [Fact]
    public void Bei_der_Sonde_steht_der_Hinweis_zur_Soletemperatur_im_Lauf()
    {
        const string kern = "sinkt die Soletemperatur mit dem Entzug";

        var sonde = Zeige(Sonde());
        Assert.Contains(sonde.FindAll(".epos-herleitung-text"), e => e.TextContent.Contains(kern));

        var kollektor = Zeige(Kollektor());
        Assert.DoesNotContain(kollektor.FindAll(".epos-herleitung-text"), e => e.TextContent.Contains(kern));
    }

    /// <summary>
    /// OHNE Lauf, aber mit Auslegungswerten rechnet die VORPRÜFUNG — gekennzeichnet, mit
    /// der Herleitung und der Prüfung nach Tabelle B2.
    /// </summary>
    [Fact]
    public void Ohne_Lauf_steht_die_gekennzeichnete_Vorpruefung()
    {
        var cut = Zeige(Sonde() with { Auslegung = EinGeraet() });

        Assert.True(cut.Instance.IstVorpruefung);
        Assert.Equal("Vorprüfung aus Auslegungswerten (noch kein Simulationslauf)",
                     cut.Find(".epos-erdreich-vorpruefung").TextContent.Trim());
        Assert.DoesNotContain("noch kein Simulationslauf", cut.Instance.Pruefungstext);
        // 10 kW · (1 − 1/4,5) = 7 778 W
        Assert.Contains("7.778 W", cut.Instance.Pruefungstext);
        Assert.Contains("W/m", cut.Instance.Pruefungstext);
    }

    /// <summary>Ein Laufergebnis ERSETZT die Vorprüfung samt Kennzeichnung.</summary>
    [Fact]
    public void Das_Laufergebnis_ersetzt_die_Vorpruefung()
    {
        var cut = Zeige(Sonde() with { Auslegung = EinGeraet() }, lauf: MitLauf(9000));

        Assert.False(cut.Instance.IstVorpruefung);
        Assert.Empty(cut.FindAll(".epos-erdreich-vorpruefung"));
        Assert.Contains("9.000 W", cut.Instance.Pruefungstext);
        Assert.DoesNotContain("7.778", cut.Instance.Pruefungstext);
    }

    /// <summary>Ein Lauf aus dem Dialog ersetzt die Vorprüfung, die vorher stand.</summary>
    [Fact]
    public void Der_Lauf_aus_dem_Dialog_ersetzt_die_Vorpruefung()
    {
        var cut = Zeige(Sonde() with { Auslegung = EinGeraet() },
            simulieren: _ => Task.FromResult<(ErdreichAuswertung.ErdreichLaufErgebnis?, string?)>(
                (MitLauf(), null)));
        Assert.True(cut.Instance.IstVorpruefung);

        cut.FindAll("button").First(b => b.TextContent.Contains("Simulation")).Click();

        Assert.False(cut.Instance.IstVorpruefung);
        Assert.Empty(cut.FindAll(".epos-erdreich-vorpruefung"));
    }

    /// <summary>
    /// Eine Luft-Wasser-Wärmepumpe: keine Vorprüfung, allein der Hinweis, dass die
    /// Erdreichquelle in der Simulation nicht gerechnet wird.
    /// </summary>
    [Fact]
    public void Bei_Luft_Wasser_steht_der_Hinweis_statt_der_Vorpruefung()
    {
        var cut = Zeige(Sonde() with { Auslegung = new[] { new WpAuslegung("WP Luft", 10, 4, "A2/W35", true) } });

        Assert.False(cut.Instance.IstVorpruefung);
        Assert.Empty(cut.FindAll(".epos-erdreich-vorpruefung"));
        Assert.Contains("Luft-Wasser-Anlage", cut.Instance.Pruefungstext);
        Assert.DoesNotContain("noch kein Simulationslauf", cut.Instance.Pruefungstext);
        Assert.DoesNotContain("W/m", cut.Instance.Pruefungstext);
    }

    /// <summary>Fehlt ein Auslegungswert, gibt es keine Vorprüfung — der Bereich nennt ihn.</summary>
    [Fact]
    public void Fehlt_ein_Wert_nennt_der_Bereich_ihn()
    {
        var ohneCop = Zeige(Sonde() with { Auslegung = new[] { new WpAuslegung("WP Sole", 10, 0, "") } });
        Assert.False(ohneCop.Instance.IstVorpruefung);
        Assert.Contains("noch kein Simulationslauf", ohneCop.Instance.Pruefungstext);
        Assert.Contains("Keine Vorprüfung aus Auslegungswerten möglich", ohneCop.Instance.Pruefungstext);
        Assert.Contains("„WP Sole“", ohneCop.Instance.Pruefungstext);

        var ohneWp = Zeige(Sonde());
        Assert.Contains("eine Wärmepumpe an der Anlage", ohneWp.Instance.Pruefungstext);

        var ohneZone = Zeige(Sonde() with { Auslegung = EinGeraet(), Klimazone = 0 });
        Assert.Contains("die Klimazone", ohneZone.Instance.Pruefungstext);
    }

    // ================================================================== Änderungshinweis

    /// <summary>
    /// Die drei Zustaende von AenderungshinweisAktualisieren:961-974. Ohne Lauf steht
    /// nie einer - die Pruefung sagt dann schon selbst, dass nichts gerechnet wurde.
    /// </summary>
    [Fact]
    public void Der_Aenderungshinweis_bleibt_ohne_Lauf_weg()
    {
        var cut = Zeige(Kollektor());
        Assert.Equal("", cut.Instance.Aenderungshinweis);

        cut.FindAll("input.epos-eingabe")[0].Input("2,5");
        Assert.Equal("", cut.Instance.Aenderungshinweis);
    }

    [Fact]
    public void Mit_Lauf_und_ohne_Aenderung_steht_kein_Hinweis()
    {
        Assert.Equal("", Zeige(Kollektor(), lauf: MitLauf()).Instance.Aenderungshinweis);
    }

    /// <summary>
    /// Mit Lauf UND geaenderten Eingaben steht der Hinweis "Bitte die Simulation neu
    /// starten" - der Lauf kam von aussen, nicht aus diesem Dialog.
    /// </summary>
    [Fact]
    public void Mit_Lauf_und_Aenderung_verlangt_der_Hinweis_einen_neuen_Lauf()
    {
        var cut = Zeige(Kollektor(), lauf: MitLauf());

        cut.FindAll("input.epos-eingabe")[0].Input("2,5");

        Assert.Contains("Simulation neu starten", cut.Instance.Aenderungshinweis);
    }

    // ================================================================== Simulation

    [Fact]
    public void Ohne_Delegat_gibt_es_keinen_Simulationsknopf()
    {
        var cut = Zeige(Kollektor());
        Assert.DoesNotContain("Simulation", KnopftexteOhneLeiste(cut));
    }

    private static string KnopftexteOhneLeiste(IRenderedComponent<QuelleErdreichDialog> cut)
    {
        var s = new System.Text.StringBuilder();
        foreach (var b in cut.FindAll("button"))
            if (!b.ClassList.Contains("epos-infoknopf")) s.Append(b.TextContent).Append('|');
        return s.ToString();
    }

    /// <summary>
    /// OHNE Projektbezug meldet der Knopf und startet nichts
    /// (SIMQ_ERDREICH_MSG_SIM_OHNE_PROJEKT, btnSimulation_Click:1016-1023).
    /// </summary>
    [Fact]
    public void Ohne_Projekt_meldet_der_Simulationsknopf()
    {
        bool gerufen = false;
        var cut = Zeige(Kollektor() with { IdProjekt = 0 },
            simulieren: _ =>
            {
                gerufen = true;
                return Task.FromResult<(ErdreichAuswertung.ErdreichLaufErgebnis?, string?)>((null, null));
            });

        cut.FindAll("button").First(b => b.TextContent.Contains("Simulation")).Click();

        Assert.False(gerufen);
        Assert.Contains("ohne Projektbezug", cut.Instance.Meldung);
    }

    [Fact]
    public void Der_Lauf_uebernimmt_sein_Ergebnis()
    {
        var cut = Zeige(Kollektor(),
            simulieren: _ => Task.FromResult<(ErdreichAuswertung.ErdreichLaufErgebnis?, string?)>(
                (MitLauf(), null)));

        Assert.Contains("noch kein Simulationslauf", cut.Instance.Pruefungstext);

        cut.FindAll("button").First(b => b.TextContent.Contains("Simulation")).Click();

        Assert.DoesNotContain("noch kein Simulationslauf", cut.Instance.Pruefungstext);
        Assert.False(cut.Instance.Laeuft);
    }

    /// <summary>
    /// Ein FEHLER des Laufs laesst die Pruefung unveraendert und meldet woertlich
    /// (SIMQ_ERDREICH_MSG_SIM_FEHLER mit dem Fehlertext).
    /// </summary>
    [Fact]
    public void Ein_Fehler_des_Laufs_laesst_die_Pruefung_stehen()
    {
        var cut = Zeige(Kollektor(),
            simulieren: _ => Task.FromResult<(ErdreichAuswertung.ErdreichLaufErgebnis?, string?)>(
                (null, "Kennlinie unterschritten")));

        cut.FindAll("button").First(b => b.TextContent.Contains("Simulation")).Click();

        Assert.Contains("Kennlinie unterschritten", cut.Instance.Meldung);
        Assert.Contains("noch kein Simulationslauf", cut.Instance.Pruefungstext);
    }

    /// <summary>
    /// Der Lauf AUS DIESEM DIALOG rechnet mit den ANGEZEIGTEN Eingaben (Anwendermeldung 10.10.2026): Der
    /// Rückruf bekommt den Satz, den OK zurückgäbe. Ohne Änderung danach steht kein Hinweis; eine Eingabe
    /// nach dem Lauf verlangt einen neuen.
    /// </summary>
    [Fact]
    public void Der_Lauf_aus_dem_Dialog_rechnet_mit_den_angezeigten_Eingaben()
    {
        QuelleErdreichDaten? gerechnet = null;
        var cut = Zeige(Sonde(),
            simulieren: satz =>
            {
                gerechnet = satz;
                return Task.FromResult<(ErdreichAuswertung.ErdreichLaufErgebnis?, string?)>((MitLauf(), null));
            });

        cut.FindAll("input.epos-eingabe")[2].Input("90");   // Länge je Sonde
        cut.FindAll("input.epos-eingabe")[3].Input("8");    // Anzahl Sonden
        cut.FindAll("button").First(b => b.TextContent.Contains("Simulation")).Click();

        Assert.NotNull(gerechnet);
        Assert.Equal(ErdreichTemperatur.QUELLSYSTEM_SONDE, gerechnet!.Quellsystem);
        Assert.Equal(90, gerechnet.Tiefe);
        Assert.Equal(8, gerechnet.Anzahl);
        Assert.Equal("", cut.Instance.Aenderungshinweis);

        cut.FindAll("input.epos-eingabe")[3].Input("9");
        Assert.Contains("neu starten", cut.Instance.Aenderungshinweis);
    }

    /// <summary>
    /// „CSV…“ nach dem Lauf aus dem Dialog: Der Export trägt das gezeigte Modell — also auch die
    /// gerechnete Reihe des Laufs neben der ungestörten.
    /// </summary>
    [Fact]
    public void Nach_dem_Lauf_traegt_der_CSV_Export_die_gerechnete_Reihe()
    {
        var reihe = new double[8760];
        for (int i = 0; i < reihe.Length; i++) reihe[i] = 3.0;
        var lauf = MitLauf() with { QuelltemperaturStuendlich = reihe };
        Func<double[], double[]?, double[]?, Task<Zeichenmodell?>> zeichner = (quelle, _, gerechnet) =>
        {
            var reihen = new List<ChartRenderer.Reihe> { new("ungestört", quelle, ChartRenderer.C_QUELLTEMPERATUR) };
            if (gerechnet is not null)
                reihen.Add(new ChartRenderer.Reihe("gerechnet (letzter Lauf)", gerechnet, ChartRenderer.C_QUELLTEMPERATUR));
            return Task.FromResult<Zeichenmodell?>(ChartRenderer.JahresgangModell("Jahresgang", reihen, "Monat", "°C"));
        };

        var exporte = new List<Zeichenmodell>();
        var cut = Render<QuelleErdreichDialog>(p =>
        {
            p.Add(x => x.Daten, Sonde());
            p.Add(x => x.Lauf, ErdreichAuswertung.ErdreichLaufErgebnis.Keines);
            p.Add(x => x.Jahresgangmodell, zeichner);
            p.Add(x => x.Simulieren, _ => Task.FromResult<(ErdreichAuswertung.ErdreichLaufErgebnis?, string?)>((lauf, null)));
            p.Add(x => x.CsvSpeichern, (m, t, r) => { exporte.Add(m); return Task.CompletedTask; });
        });
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Bild));

        cut.FindAll("button").First(b => b.TextContent.Contains("Simulation")).Click();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.Instance.Bild!.Reihen.Count));
        cut.Find("div.epos-diagramm-leiste button.epos-diagramm-csv").Click();

        var spalten = ZeitreihenCsv.AusModell(Assert.Single(exporte));
        Assert.Equal(2, spalten.Count);
        Assert.Equal(reihe, spalten[1].Werte);
    }

    /// <summary>Eine verletzte Regel hält den Lauf an und meldet wie OK.</summary>
    [Fact]
    public void Eine_verletzte_Regel_haelt_den_Lauf_an()
    {
        bool gerufen = false;
        var cut = Zeige(Sonde(),
            simulieren: _ =>
            {
                gerufen = true;
                return Task.FromResult<(ErdreichAuswertung.ErdreichLaufErgebnis?, string?)>((MitLauf(), null));
            });

        cut.FindAll("input.epos-eingabe")[3].Input("0");
        cut.FindAll("button").First(b => b.TextContent.Contains("Simulation")).Click();

        Assert.False(gerufen);
        Assert.Contains("mindestens eine Sonde", cut.Instance.Meldung);
    }

    // ================================================================== Karte

    [Fact]
    public void Der_Kartenknopf_oeffnet_die_Ueberlagerung()
    {
        var cut = Zeige(Kollektor());
        Assert.False(cut.Instance.KarteOffen);

        cut.FindAll("button").First(b => b.TextContent.Contains("…")).Click();

        Assert.True(cut.Instance.KarteOffen);
        Assert.NotNull(cut.Find(".epos-ueberlagerung"));
    }

    /// <summary>
    /// Eine auf der Karte gewaehlte Zone geht in die AUSWAHLLISTE - genau wie im
    /// Vorlaeufer (btnKarte_Click:1079-1087).
    /// </summary>
    [Fact]
    public void Die_Karte_uebernimmt_ihre_Zone_in_die_Liste()
    {
        var cut = Zeige(Kollektor());
        Assert.Equal(6, cut.Instance.Klimazone);

        cut.FindAll("button").First(b => b.TextContent.Contains("…")).Click();
        cut.FindAll("path.epos-bildkarte-flaeche")[7].DoubleClick();   // Zone 8

        Assert.Equal(8, cut.Instance.Klimazone);
        Assert.False(cut.Instance.KarteOffen);
    }

    // ================================================================== OK-Regeln

    [Fact]
    public void Die_acht_Pruefregeln_melden_woertlich()
    {
        // Kollektor: keine Zahl
        var cut = Zeige(Kollektor());
        cut.FindAll("input.epos-eingabe")[0].Input("");
        cut.Find("button.epos-knopf--primaer").Click();
        Assert.Contains("gültige Zahlenwerte für Verlegetiefe und Fläche", cut.Instance.Meldung);

        // Kollektor: Tiefe 0
        cut = Zeige(Kollektor());
        cut.FindAll("input.epos-eingabe")[0].Input("0");
        cut.Find("button.epos-knopf--primaer").Click();
        Assert.Contains("Verlegetiefe muss größer als 0 m sein", cut.Instance.Meldung);

        // Kollektor: Tiefe > 10
        cut = Zeige(Kollektor());
        cut.FindAll("input.epos-eingabe")[0].Input("12");
        cut.Find("button.epos-knopf--primaer").Click();
        Assert.Contains("nicht tiefer als 10 m", cut.Instance.Meldung);

        // Kollektor: Flaeche 0
        cut = Zeige(Kollektor());
        cut.FindAll("input.epos-eingabe")[1].Input("0");
        cut.Find("button.epos-knopf--primaer").Click();
        Assert.Contains("Kollektorfläche eintragen", cut.Instance.Meldung);

        // Sonde: keine Zahl
        cut = Zeige(Sonde());
        cut.FindAll("input.epos-eingabe")[2].Input("");
        cut.Find("button.epos-knopf--primaer").Click();
        Assert.Contains("gültige Zahlenwerte für Sondenlänge und Anzahl", cut.Instance.Meldung);

        // Sonde: Laenge 0
        cut = Zeige(Sonde());
        cut.FindAll("input.epos-eingabe")[2].Input("0");
        cut.Find("button.epos-knopf--primaer").Click();
        Assert.Contains("Sondenlänge muss größer als 0 m sein", cut.Instance.Meldung);

        // Sonde: Anzahl 0
        cut = Zeige(Sonde());
        cut.FindAll("input.epos-eingabe")[3].Input("0");
        cut.Find("button.epos-knopf--primaer").Click();
        Assert.Contains("mindestens eine Sonde", cut.Instance.Meldung);

        // Beide: Spreizung 0
        cut = Zeige(Kollektor());
        cut.FindAll("input.epos-eingabe")[9].Input("0");
        cut.Find("button.epos-knopf--primaer").Click();
        Assert.Contains("nutzbare Spreizung größer als 0 K", cut.Instance.Meldung);

        // Sonde: ein Wert des Sondenfeldes ausserhalb seiner Pruefklausel
        cut = Zeige(Sonde());
        cut.FindAll("input.epos-eingabe")[4].Input("0");
        cut.Find("button.epos-knopf--primaer").Click();
        Assert.Contains("Sondenabstand, Bohrlochdurchmesser und Bohrlochwiderstand", cut.Instance.Meldung);
    }

    // ---------------------------------------------------------------------
    //  Das Sondenfeld je Anlage (Schemaschritt 195): leer = Vorgabe
    // ---------------------------------------------------------------------

    /// <summary>Die sechs Felder des Sondenfeldes sind nur beim Quellsystem Sonde frei; leer zeigen sie die Vorgabe.</summary>
    [Fact]
    public void Die_Sondenfeldfelder_gelten_nur_bei_der_Sonde_und_zeigen_die_Vorgabe()
    {
        var kollektor = Zeige(Kollektor());
        var felder = kollektor.FindAll("input.epos-eingabe");
        for (int i = 4; i <= 8; i++)
            Assert.True(felder[i].HasAttribute("disabled"), "Feld " + i);
        Assert.True(kollektor.FindAll("select")[0].HasAttribute("disabled"));

        var sonde = Zeige(Sonde());
        felder = sonde.FindAll("input.epos-eingabe");
        for (int i = 4; i <= 8; i++)
        {
            Assert.False(felder[i].HasAttribute("disabled"), "Feld " + i);
            Assert.Equal("", felder[i].GetAttribute("value") ?? "");
        }
        Assert.False(sonde.FindAll("select")[0].HasAttribute("disabled"));
        Assert.Equal("Vorgabe 6", felder[4].GetAttribute("placeholder"));
        Assert.Equal("Vorgabe 150", felder[5].GetAttribute("placeholder"));
        Assert.Equal("Vorgabe 0,1", felder[6].GetAttribute("placeholder"));
        Assert.Equal("Vorgabe 2", felder[7].GetAttribute("placeholder"));
        Assert.Equal("Vorgabe 10", felder[8].GetAttribute("placeholder"));
        Assert.Contains("Vorgabe Quadratisch", sonde.FindAll("select")[0].TextContent);
    }

    /// <summary>Leere Felder kommen als null (Vorgabe) zurück, gepflegte als Wert.</summary>
    [Fact]
    public void OK_schreibt_das_Sondenfeld_leer_als_Vorgabe_zurueck()
    {
        QuelleErdreichDaten? ergebnis = null;
        var cut = Zeige(Sonde(), d => ergebnis = d);
        cut.Find("button.epos-knopf--primaer").Click();
        Assert.NotNull(ergebnis);
        Assert.Null(ergebnis!.Sondenabstand);
        Assert.Null(ergebnis.Bohrlochdurchmesser);
        Assert.Null(ergebnis.Bohrlochwiderstand);
        Assert.Null(ergebnis.Kopfueberdeckung);
        Assert.Null(ergebnis.Betrachtungsjahr);
        Assert.Null(ergebnis.Sondenanordnung);

        ergebnis = null;
        cut = Zeige(Sonde(), d => ergebnis = d);
        var felder = cut.FindAll("input.epos-eingabe");
        felder[4].Input("8");
        cut.FindAll("input.epos-eingabe")[5].Input("180");
        cut.FindAll("input.epos-eingabe")[6].Input("0,08");
        cut.FindAll("input.epos-eingabe")[7].Input("0");
        cut.FindAll("input.epos-eingabe")[8].Input("25");
        cut.FindAll("select")[0].Change(((int)Sondenanordnung.Reihe).ToString(CultureInfo.InvariantCulture));
        cut.Find("button.epos-knopf--primaer").Click();
        Assert.NotNull(ergebnis);
        Assert.Equal(8.0, ergebnis!.Sondenabstand);
        Assert.Equal(180.0, ergebnis.Bohrlochdurchmesser);
        Assert.Equal(0.08, ergebnis.Bohrlochwiderstand);
        Assert.Equal(0.0, ergebnis.Kopfueberdeckung);
        Assert.Equal(25, ergebnis.Betrachtungsjahr);
        Assert.Equal("Reihe", ergebnis.Sondenanordnung);
    }

    /// <summary>Gespeicherte Werte stehen beim Öffnen in den Feldern; der Assistent setzt sie über die Sichtklasse.</summary>
    [Fact]
    public void Gespeicherte_Sondenfeldwerte_stehen_im_Feld_und_der_Assistent_setzt_sie()
    {
        var cut = Zeige(Sonde() with { Sondenabstand = 7.5, Betrachtungsjahr = 20, Sondenanordnung = "Reihe" });
        var felder = cut.FindAll("input.epos-eingabe");
        Assert.Equal("7,5", felder[4].GetAttribute("value"));
        Assert.Equal("20", felder[8].GetAttribute("value"));

        WindowsFormsApplication1.KiFeldzugang abstand =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.QUELLE_ERDREICH, "sondenabstand");
        Assert.NotNull(abstand);
        Assert.Equal(7.5, abstand.Lesen());
        abstand.Setzen(9.0);
        Assert.Equal(9.0, abstand.Lesen());

        WindowsFormsApplication1.KiFeldzugang anordnung =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.QUELLE_ERDREICH, "sondenanordnung");
        Assert.NotNull(anordnung);
        Assert.Equal("Reihe", anordnung.Lesen());
        anordnung.Setzen("Quadratisch");
        Assert.Equal("Quadratisch", anordnung.Lesen());
    }

    /// <summary>
    /// Rueckschreiben KOLLEKTOR (:1224-1227): Quellsystem, Tiefe, Flaeche und
    /// ausdruecklich Anzahl = 0.
    /// </summary>
    [Fact]
    public void OK_schreibt_den_Kollektorzweig_zurueck()
    {
        QuelleErdreichDaten? ergebnis = null;
        var cut = Zeige(Kollektor(), d => ergebnis = d);

        cut.Find("button.epos-knopf--primaer").Click();

        Assert.NotNull(ergebnis);
        Assert.Equal(ErdreichTemperatur.QUELLSYSTEM_KOLLEKTOR, ergebnis!.Quellsystem);
        Assert.Equal(1.8, ergebnis.Tiefe);
        Assert.Equal(250.0, ergebnis.Flaeche);
        Assert.Equal(0, ergebnis.Anzahl);
        Assert.Equal(4.0, ergebnis.Spreizung);
        Assert.Equal(6, ergebnis.Klimazone);
    }

    /// <summary>
    /// Rueckschreiben SONDE (:1248-1251): Tiefe = die SONDENLAENGE, Flaeche = 0,
    /// Anzahl gerundet.
    /// </summary>
    [Fact]
    public void OK_schreibt_den_Sondenzweig_zurueck()
    {
        QuelleErdreichDaten? ergebnis = null;
        var cut = Zeige(Sonde(), d => ergebnis = d);

        cut.Find("button.epos-knopf--primaer").Click();

        Assert.NotNull(ergebnis);
        Assert.Equal(ErdreichTemperatur.QUELLSYSTEM_SONDE, ergebnis!.Quellsystem);
        Assert.Equal(120.0, ergebnis.Tiefe);
        Assert.Equal(0.0, ergebnis.Flaeche);
        Assert.Equal(4, ergebnis.Anzahl);
    }

    [Fact]
    public void Abbrechen_und_Esc_liefern_null()
    {
        QuelleErdreichDaten? ergebnis = Kollektor();
        var cut = Zeige(Kollektor(), d => ergebnis = d);

        cut.FindAll(".epos-leiste button")[0].Click();
        Assert.Null(ergebnis);

        ergebnis = Kollektor();
        cut.Find("div.epos-dialog").KeyDown("Escape");
        Assert.Null(ergebnis);
    }

    /// <summary>Das Schliesskreuz im Kopf wirkt wie Esc/Abbrechen.</summary>
    [Fact]
    public void Kreuz_liefert_null()
    {
        QuelleErdreichDaten? ergebnis = Kollektor();
        var cut = Zeige(Kollektor(), d => ergebnis = d);

        cut.Find(".epos-dialog-zu").Click();
        Assert.Null(ergebnis);
    }

    /// <summary>Titel-bedingter Kopf: ohne Titel zeigt der Kopf weder Titel noch Kreuz.</summary>
    [Fact]
    public void Ohne_Titel_zeigt_der_Kopf_weder_Titel_noch_Kreuz()
    {
        var cut = Zeige(Kollektor(), titelAnzeigen: false);

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
        // Der Hilfeknopf bleibt - er haengt nicht am Titel.
        Assert.NotEmpty(cut.FindAll(".epos-dialog-kopf"));
    }

    /// <summary>Esc schliesst zuerst die KARTE, nicht den Dialog (Hausregel).</summary>
    [Fact]
    public void Esc_schliesst_zuerst_die_Karte()
    {
        QuelleErdreichDaten? ergebnis = Kollektor();
        var cut = Zeige(Kollektor(), d => ergebnis = d);

        cut.FindAll("button").First(b => b.TextContent.Contains("…")).Click();
        Assert.True(cut.Instance.KarteOffen);

        cut.Find("div.epos-dialog").KeyDown("Escape");
        Assert.False(cut.Instance.KarteOffen);
        Assert.NotNull(ergebnis);          // der Dialog steht noch
    }

    /// <summary>
    /// Das ✕ der Karte wirkt wie Abbrechen (Hausmuster): Es schliesst die
    /// Ueberlagerung, OHNE eine Zone zu uebernehmen - die gewaehlte Zone bleibt, was
    /// sie war, und der Dialog darunter steht weiter.
    /// </summary>
    [Fact]
    public void Das_Kreuz_der_Karte_verwirft_die_Zone()
    {
        QuelleErdreichDaten? ergebnis = null;
        var cut = Zeige(Kollektor(), d => ergebnis = d);

        cut.FindAll("button").First(b => b.TextContent.Contains("…")).Click();
        cut.FindAll("path.epos-bildkarte-flaeche")[7].Click();      // Zone 8 nur MARKIERT

        cut.Find("button.epos-ueberlagerung-zu").Click();

        Assert.False(cut.Instance.KarteOffen);
        Assert.Equal(6, cut.Instance.Klimazone);                    // unveraendert
        Assert.Null(ergebnis);                                      // der Dialog steht noch
    }

    /// <summary>
    /// Waehrend des Simulationslaufs uebernimmt nichts (Abweichung A-5): Der
    /// OK-Knopf ist gesperrt, und Esc drueckt den Wartezustand nicht weg.
    /// </summary>
    [Fact]
    public void Der_Wartezustand_sperrt_das_Uebernehmen()
    {
        var haenger = new TaskCompletionSource<(ErdreichAuswertung.ErdreichLaufErgebnis?, string?)>();
        QuelleErdreichDaten? ergebnis = null;

        var cut = Zeige(Kollektor(), d => ergebnis = d, simulieren: _ => haenger.Task);

        cut.FindAll("button").First(b => b.TextContent.Contains("Simulation")).Click();
        cut.WaitForState(() => cut.Instance.Laeuft);

        Assert.True(cut.Find("button.epos-knopf--primaer").HasAttribute("disabled"));

        cut.Find("div.epos-dialog").KeyDown("Escape");
        Assert.Null(ergebnis);
        Assert.True(cut.Instance.Laeuft);

        haenger.SetResult((MitLauf(), null));               // den Lauf sauber beenden
        cut.WaitForState(() => !cut.Instance.Laeuft);
    }

    // =====================================================================
    //  Formularraster (Anwenderwunsch iU8-E-2, Paket P3, 05.09.2026)
    // =====================================================================

    /// <summary>
    /// Die zwei Quellsystem-Rubriken und der Standort stehen im Formularraster - der
    /// Standort einspaltig, weil unter jedem Wert seine Herleitungszeile steht.
    ///
    /// <para>Geprueft wird das MARKUP: Der Block traegt
    /// <c>epos-formularraster</c>, und darin stehen Felder. Was der Raster
    /// daraus MACHT (Beschriftungsspalte, kurzes Feld, zwei Spalten), steht
    /// als Stilblattprobe in <c>FormularrasterTests</c> - eine bunit-Probe
    /// rechnet kein CSS aus (Lehre W6-B-1).</para>
    /// </summary>
    [Fact]
    public void Quellsystem_und_Standort_stehen_im_Formularraster()
    {
        var cut = Zeige(Kollektor());

        Assert.Equal(3, cut.FindAll(".epos-formularraster").Count);
        Assert.Single(cut.FindAll(".epos-formularraster--einspaltig"));
        Assert.NotEmpty(cut.FindAll(
            ".epos-formularraster .epos-feld--kurz .epos-feld-zeile .epos-einheit"));
    }

    // =====================================================================
    //  Die Vorschau als SVG (Etappe DG-E3)
    // =====================================================================

    /// <summary>
    /// <b>Der Jahresgang der Quelltemperatur steht im Baustein <c>DiagrammSvg</c></b>
    /// — unter der Kennung <c>quelle-erdreich</c>, mit Zoomleiste: Er trägt eine
    /// Zeitachse, und ein Zeitausschnitt ist die <c>viewBox</c> seiner Zeichenfläche.
    ///
    /// <para>Das Modell kommt AUS DER HÜLLE und bleibt im Dialog liegen, bis eine
    /// Eingabe ein neues verlangt — der Baustein baut seinen Knotenbaum nur neu, wenn
    /// die REFERENZ wechselt.</para>
    /// </summary>
    [Fact]
    public void Die_Vorschau_steht_als_DiagrammSvg()
    {
        var cut = Zeige(Kollektor(), modell: (_, _, _) => Task.FromResult<Zeichenmodell?>(_vorschau));

        // Das Bild entsteht NACH dem ersten Zeichenlauf (OnAfterRenderAsync) - der
        // Zeichner laeuft auf einem eigenen Faden. Also auf den Stand warten, statt
        // ihn sofort zu lesen.
        cut.WaitForAssertion(
            () => Assert.Same(_vorschau, cut.FindComponent<DiagrammSvg>().Instance.Modell));

        Assert.Equal("quelle-erdreich", cut.FindComponent<DiagrammSvg>().Instance.Kennung);
        Assert.Single(cut.FindAll(".epos-diagramm-leiste"));
        Assert.Empty(cut.FindAll("img"));
    }

    /// <summary>
    /// <b>Die Farbwahl am Bild geht durch</b> (Farbrollen, Bedienung Teil 2): Die zwei
    /// Reihen der Vorschau tragen die Rollen <c>QUELLTEMPERATUR</c> und
    /// <c>AUSSENTEMPERATUR</c>, also gibt es dort etwas zu wählen. Ohne Delegat bietet
    /// der Baustein den Wähler NICHT an — kein Empfänger, kein Versprechen.
    /// </summary>
    [Fact]
    public void Ohne_Farbdelegat_bietet_die_Vorschau_keinen_Waehler()
    {
        var ohne = Zeige(Kollektor(), modell: (_, _, _) => Task.FromResult<Zeichenmodell?>(_vorschau));
        Assert.False(ohne.FindComponent<DiagrammSvg>().Instance.FarbwahlErlaubt);

        var mit = Render<QuelleErdreichDialog>(p =>
        {
            p.Add(x => x.Daten, Kollektor());
            p.Add(x => x.Lauf, ErdreichAuswertung.ErdreichLaufErgebnis.Keines);
            p.Add(x => x.Jahresgangmodell,
                  (_, _, _) => Task.FromResult<Zeichenmodell?>(_vorschau));
            p.Add(x => x.FarbeSetzen, (_, _) => Task.CompletedTask);
        });

        Assert.True(mit.FindComponent<DiagrammSvg>().Instance.FarbwahlErlaubt);
    }

    /// <summary>
    /// Nach einem Lauf (Anwenderwunsch 08.10.2026) bekommt die Vorschau die gerechnete Quelltemperatur als
    /// dritte Reihe, und eine eigene Kennwertzeile nennt min/max/Mittel; ohne Lauf bleibt beides aus.
    /// </summary>
    [Fact]
    public void Nach_dem_Lauf_zeigt_die_Vorschau_die_gerechnete_Quelltemperatur()
    {
        var reihe = new double[8760];
        for (int i = 0; i < reihe.Length; i++) reihe[i] = i < 4380 ? 2.0 : 6.0;
        var lauf = MitLauf() with { QuelltemperaturStuendlich = reihe };

        double[]? erhalten = null;
        int aufrufe = 0;
        var cut = Zeige(Sonde(), lauf: lauf, modell: (_, _, gerechnet) =>
        {
            aufrufe++;
            erhalten = gerechnet;
            return Task.FromResult<Zeichenmodell?>(_vorschau);
        });

        cut.WaitForAssertion(() => Assert.True(aufrufe > 0));
        Assert.NotNull(erhalten);
        Assert.Equal(reihe, erhalten);
        Assert.Contains("gerechnet (letzter Lauf)", cut.Instance.KennwertzeileLauf, StringComparison.Ordinal);
        Assert.Contains("6", cut.Instance.KennwertzeileLauf, StringComparison.Ordinal);
        Assert.NotEmpty(cut.FindAll("[data-zeile=kennwerte-lauf]"));

        double[]? ohneLauf = new double[1];
        int aufrufeOhne = 0;
        var ohne = Zeige(Sonde(), modell: (_, _, gerechnet) =>
        {
            aufrufeOhne++;
            ohneLauf = gerechnet;
            return Task.FromResult<Zeichenmodell?>(_vorschau);
        });
        ohne.WaitForAssertion(() => Assert.True(aufrufeOhne > 0));
        Assert.Null(ohneLauf);
        Assert.Equal("", ohne.Instance.KennwertzeileLauf);
        Assert.Empty(ohne.FindAll("[data-zeile=kennwerte-lauf]"));
    }

    /// <summary>
    /// Anwendermeldung 10.10.2026 („Referenzprojekt AK3-K“): Der Lauf AUS DEM DIALOG zeigt sein Ergebnis
    /// sofort — Kennwertzeile und zweite Reihe im Bild —, nicht erst nach OK und Wiederöffnen. Lauf und
    /// Zeichenmodell kommen wie in der Hülle von einem fremden Faden.
    /// </summary>
    [Fact]
    public void Der_Lauf_aus_dem_Dialog_zeigt_Kennwerte_und_Reihe_sofort()
    {
        var reihe = new double[8760];
        for (int i = 0; i < reihe.Length; i++) reihe[i] = i < 4380 ? 1.0 : 7.0;
        var lauf = MitLauf() with { QuelltemperaturStuendlich = reihe };

        double[]? erhalten = null;
        var cut = Zeige(Sonde(),
            simulieren: async _ =>
            {
                await Task.Run(() => Thread.Sleep(30));
                return (lauf, null);
            },
            modell: async (quelle, _, gerechnet) =>
            {
                await Task.Run(() => Thread.Sleep(10));
                if (gerechnet is not null) erhalten = gerechnet;
                var reihen = new List<ChartRenderer.Reihe>
                {
                    new("ungestört", quelle, ChartRenderer.C_QUELLTEMPERATUR)
                };
                if (gerechnet is not null)
                    reihen.Add(new ChartRenderer.Reihe("gerechnet (letzter Lauf)", gerechnet, ChartRenderer.C_QUELLTEMPERATUR));
                return ChartRenderer.JahresgangModell("Jahresgang", reihen, "Monat", "°C");
            });

        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Bild));
        Assert.Empty(cut.FindAll("[data-zeile=kennwerte-lauf]"));

        cut.FindAll("button").First(b => b.TextContent.Contains("Simulation")).Click();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-zeile=kennwerte-lauf]")), TimeSpan.FromSeconds(5));
        cut.WaitForAssertion(() => Assert.Equal(reihe, erhalten), TimeSpan.FromSeconds(5));
        cut.WaitForAssertion(() => Assert.Equal(2, cut.Instance.Bild!.Reihen.Count), TimeSpan.FromSeconds(5));
        Assert.Contains("gerechnet (letzter Lauf)", cut.Markup, StringComparison.Ordinal);

        // Die Kennwerte charakterisieren den Lauf: Jahresmittel, Tiefstwert mit Zeitpunkt, Höchstwert.
        string zeile = cut.Find("[data-zeile=kennwerte-lauf]").TextContent;
        Assert.Contains("Jahresmittel 4,0 °C", zeile, StringComparison.Ordinal);
        Assert.Contains("Tiefstwert 1,0 °C am 01.01., 00:00 Uhr", zeile, StringComparison.Ordinal);
        Assert.Contains("Höchstwert 7,0 °C am 02.07., 12:00 Uhr", zeile, StringComparison.Ordinal);
    }

    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F2)
    // =====================================================================

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Sie bindet über die
    /// Sichtklasse <c>QuelleErdreichKiSicht</c> und steht deshalb nicht in der
    /// Markup-Probe des Dialogkatalogs — dieser Fall ist ihr Ersatz: Die Maske steht
    /// gezeichnet da, die Brücke liest die Spreizung, die der Anwender sieht, und ein
    /// Setzen landet im Eingabefeld.
    /// </summary>
    /// <remarks>
    /// <b>Monotone Aussage</b> (Muster <c>KiMaskenhakenTests</c>): Geprüft wird, was
    /// nach dem Zeichnen DA ist — die Brücke ist prozessweiter Zustand.
    /// </remarks>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_setzt_die_Spreizung()
    {
        Zeige(Kollektor());

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.QUELLE_ERDREICH));

        WindowsFormsApplication1.KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.QUELLE_ERDREICH, "spreizung");
        Assert.NotNull(zugang);
        Assert.Equal(4.0, zugang.Lesen());

        Assert.True(zugang.Setzbar);
        zugang.Setzen(6.5);

        Assert.Equal(6.5, zugang.Lesen());
    }

    /// <summary>
    /// Das Quellsystem ist ein Wahrheitswert, und ein Setzen geht den Weg des
    /// Optionsfeldes: Danach steht die Maske im Sondenzweig — die Kollektorfelder
    /// bleiben stehen und behalten ihre Werte (Abweichung A‑4).
    /// </summary>
    [Fact]
    public void Der_Assistent_schaltet_auf_die_Erdsonde_um()
    {
        var cut = Zeige(Kollektor());

        WindowsFormsApplication1.KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.QUELLE_ERDREICH, "erdsonde");

        Assert.Equal(false, zugang.Lesen());

        zugang.Setzen(true);
        cut.Render();

        Assert.Equal(true, zugang.Lesen());

        // Die Verlegetiefe des Kollektors steht unveraendert da.
        WindowsFormsApplication1.KiFeldzugang tiefe =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.QUELLE_ERDREICH, "verlegetiefe");
        Assert.Equal(1.8, tiefe.Lesen());
    }

    /// <summary>
    /// <b>Der Bodentyp ist ein WAHLFELD</b> (KI-F1b): Gesetzt wird über den
    /// ANZEIGETEXT der Klappliste, in der Sicht steht danach der KATALOGSCHLÜSSEL —
    /// nicht der Listenplatz (Abweichung A‑3).
    /// </summary>
    [Fact]
    public void Der_Assistent_waehlt_den_Bodentyp_ueber_seinen_Anzeigetext()
    {
        var cut = Zeige(Kollektor());

        WindowsFormsApplication1.KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.QUELLE_ERDREICH, "bodentyp");
        Assert.NotNull(zugang);
        Assert.Equal(ErdreichTemperatur.BODENTYP_DEFAULT, zugang.Lesen());

        string granit = ErdreichTemperatur.Bodentyp(DbWerte.BODENTYP_GRANIT).Untergrund;

        KiFeldumsetzung umsetzung = KiFeldwandler.Wandle(zugang, granit);
        Assert.True(umsetzung.Ok, umsetzung.Grund);
        zugang.Setzen(umsetzung.Wert);
        cut.Render();

        // In der Maske steht der Schluessel …
        Assert.Equal(DbWerte.BODENTYP_GRANIT, cut.Instance.Bodentyp);

        // … der Assistent liest den TEXT, den der Anwender sieht.
        KiFeldwert wert = KiMaskenbruecke.Lesen(KiMaskennamen.QUELLE_ERDREICH)
                                         .Single(f => f.Name == "bodentyp");
        Assert.Equal(granit, wert.Text);
        Assert.Equal(DbWerte.BODENTYP_GRANIT, wert.Schluessel);
    }

    /// <summary>
    /// Die Klimazone ist seit KI-F1b ebenfalls eine Wahl: Ihr Schlüssel ist die
    /// Zonennummer, ihr Text die Zeile „z — n h/a" der Klappliste.
    /// </summary>
    [Fact]
    public void Der_Assistent_waehlt_die_Klimazone_ueber_ihre_Nummer()
    {
        var cut = Zeige(Kollektor());

        WindowsFormsApplication1.KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.QUELLE_ERDREICH, "klimazone");
        Assert.NotNull(zugang);

        KiFeldumsetzung umsetzung = KiFeldwandler.Wandle(zugang, "11");
        Assert.True(umsetzung.Ok, umsetzung.Grund);
        zugang.Setzen(umsetzung.Wert);
        cut.Render();

        Assert.Equal(11, cut.Instance.Klimazone);

        KiFeldwert wert = KiMaskenbruecke.Lesen(KiMaskennamen.QUELLE_ERDREICH)
                                         .Single(f => f.Name == "klimazone");
        Assert.StartsWith("11 ", wert.Text);
        Assert.Equal("11", wert.Schluessel);
    }

    /// <summary>
    /// <b>„CSV…“ am Jahresgang</b> (CSV-3): Mit dem Delegat der Hülle trägt die Vorschau den
    /// Knopf; der Klick gibt das gezeigte Modell an die Naht — 8 760 Stundenwerte, Raster Stunde.
    /// Ohne Delegat kein Knopf.
    /// </summary>
    [Fact]
    public void Der_Jahresgang_schreibt_CSV_ueber_die_Naht()
    {
        Func<double[], double[]?, double[]?, Task<Zeichenmodell?>> zeichner = (quelle, _, _) =>
            Task.FromResult<Zeichenmodell?>(ChartRenderer.JahresgangModell("Jahresgang",
                new[] { new ChartRenderer.Reihe("Quelltemperatur", quelle, ChartRenderer.C_QUELLTEMPERATUR) },
                "Monat", "Quelltemperatur [°C]"));

        var ohne = Zeige(Kollektor(), modell: zeichner);
        ohne.WaitForAssertion(() => Assert.NotNull(ohne.FindComponent<DiagrammSvg>().Instance.Modell));
        Assert.Empty(ohne.FindAll("button.epos-diagramm-csv"));

        var exporte = new List<(Zeichenmodell M, string T, Zeitraster R)>();
        var cut = Render<QuelleErdreichDialog>(p =>
        {
            p.Add(x => x.Daten, Kollektor());
            p.Add(x => x.Lauf, ErdreichAuswertung.ErdreichLaufErgebnis.Keines);
            p.Add(x => x.Jahresgangmodell, zeichner);
            p.Add(x => x.CsvSpeichern, (m, t, r) => { exporte.Add((m, t, r)); return Task.CompletedTask; });
        });
        cut.WaitForAssertion(() => Assert.NotNull(cut.FindComponent<DiagrammSvg>().Instance.Modell));
        cut.Find("div.epos-diagramm-leiste button.epos-diagramm-csv").Click();

        var (m, _, r) = Assert.Single(exporte);
        Assert.Equal(8760, ZeitreihenCsv.AusModell(m)[0].Werte.Length);
        Assert.Equal(Zeitraster.Stunde, r);
    }
}
