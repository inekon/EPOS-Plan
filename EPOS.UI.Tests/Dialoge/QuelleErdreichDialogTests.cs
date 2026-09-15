using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Simulation;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// QuelleErdreichDialog (iU9-W10a.3) - der Ersatz fuer Form_QuelleErdreich.
///
/// <para>FELDBESTAND laut Feldkarte: 30 Steuerelemente plus das Diagramm - zwei
/// Wahlknoepfe (Kollektor/Sonde), vier Zahlenfelder je Zweig, zwei Klapplisten
/// (Bodentyp, Klimazone), das Spreizungsfeld, der Kartenknopf, drei
/// Herleitungszeilen, die Kennwertzeile und der Simulationsknopf.</para>
///
/// <para>OHNE KNOPFLEISTE (Auftrag #275, Anwenderwunsch 14.09.2026): OK und
/// Abbrechen sind gefallen. Der EINE Ausgang ist das Schliessen der Ueberlagerung
/// (Kreuz oder Esc), und es UEBERNIMMT - durch dieselben acht Pruefregeln, die
/// vorher an OK hingen. Eine verletzte Regel meldet und haelt den Dialog offen.</para>
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
        Func<int, Task<(ErdreichAuswertung.ErdreichLaufErgebnis?, string?)>>? simulieren = null)
    {
        return Render<QuelleErdreichDialog>(p =>
        {
            p.Add(x => x.Daten, daten);
            p.Add(x => x.Lauf, lauf ?? ErdreichAuswertung.ErdreichLaufErgebnis.Keines);
            if (geschlossen is not null) p.Add(x => x.Geschlossen, geschlossen);
            if (simulieren is not null) p.Add(x => x.Simulieren, simulieren);
        });
    }

    /// <summary>Ein belastbares Laufergebnis — die Prüfung rechnet damit.</summary>
    private static ErdreichAuswertung.ErdreichLaufErgebnis MitLauf(double maxEntzug = 9000)
        => new(true, true, maxEntzug, 18000, 1800, "", "", "");

    /// <summary>
    /// Der EINE Ausgang (#275): das Schliessen der Ueberlagerung. Der Wirt ruft
    /// genau diese Methode aus dem <c>Geschlossen</c>-Rueckruf seiner
    /// <c>Ueberlagerung</c> (das Kreuz); Esc kommt im Dialog selbst hier an.
    /// </summary>
    private static async Task Schliessen(IRenderedComponent<QuelleErdreichDialog> cut)
        => await cut.InvokeAsync(() => cut.Instance.UebernehmenUndSchliessen());

    // ================================================================== Feldbestand

    [Fact]
    public void Der_Feldbestand_steht_vollstaendig()
    {
        var cut = Zeige(Kollektor());

        Assert.Equal("Wärmequelle Erdreich — WP Erdgeschoss",
                     cut.Find("h1.epos-dialog-titel").TextContent);

        // Zwei Wahlknoepfe (Kollektor/Sonde), je einer in seiner Rubrik.
        Assert.Equal(2, cut.FindAll("input[type=radio]").Count);

        // Vier Zweigfelder + Spreizung = fuenf Zahlenfelder (davon eins ganzzahlig).
        Assert.Equal(5, cut.FindAll("input.epos-eingabe").Count);

        // Zwei Klapplisten: Bodentyp und Klimazone.
        Assert.Equal(2, cut.FindAll("select").Count);

        Assert.NotNull(cut.Find("button.epos-infoknopf"));

        // #275: Die Knopfleiste ist gefallen - weder OK noch Abbrechen stehen noch da.
        Assert.Empty(cut.FindAll(".epos-leiste"));
        Assert.Empty(cut.FindAll("button.epos-knopf--primaer"));
    }

    /// <summary>
    /// Die Klimazonenliste traegt Zone 0 („nicht zugeordnet") plus 1…15 - genau
    /// KatalogeFuellen:669-679.
    /// </summary>
    [Fact]
    public void Die_Klimazonenliste_traegt_die_Null_und_fuenfzehn_Zonen()
    {
        var cut = Zeige(Kollektor());
        var zonen = cut.FindAll("select")[1].QuerySelectorAll("option");

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
        Assert.Equal(new[] { "Länge je Sonde:", "Anzahl Sonden:" }, sonde);
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
        Assert.Equal(2, zweige[1].QuerySelectorAll("input.epos-eingabe").Length);
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
        cut.FindAll("select")[0].Change(anderer.ToString());
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
        cut.FindAll("select")[0].Change((index == 0 ? 1 : 0).ToString());
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

    /// <summary>Nach dem Lauf AUS DIESEM DIALOG steht der andere Aenderungshinweis.</summary>
    [Fact]
    public void Nach_einem_Lauf_aus_dem_Dialog_steht_der_zweite_Hinweis()
    {
        var cut = Zeige(Kollektor(),
            simulieren: _ => Task.FromResult<(ErdreichAuswertung.ErdreichLaufErgebnis?, string?)>(
                (MitLauf(), null)));

        cut.FindAll("button").First(b => b.TextContent.Contains("Simulation")).Click();
        cut.FindAll("input.epos-eingabe")[0].Input("2,5");

        Assert.Contains("GESPEICHERTEN", cut.Instance.Aenderungshinweis);
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

    // =========================================== Das Schliessen uebernimmt (#275)

    /// <summary>
    /// Die ACHT Pruefregeln (<c>btnOk_Click</c>:1194-1264) haengen seit #275 am
    /// SCHLIESSEN statt am OK-Knopf - Wortlaut und Reihenfolge unveraendert. Jede
    /// meldet, und der Dialog bleibt stehen.
    /// </summary>
    [Fact]
    public async Task Die_acht_Pruefregeln_melden_woertlich()
    {
        // Kollektor: keine Zahl
        var cut = Zeige(Kollektor());
        cut.FindAll("input.epos-eingabe")[0].Input("");
        await Schliessen(cut);
        cut.WaitForAssertion(() => Assert.Contains(
            "gültige Zahlenwerte für Verlegetiefe und Fläche", cut.Instance.Meldung));

        // Kollektor: Tiefe 0
        cut = Zeige(Kollektor());
        cut.FindAll("input.epos-eingabe")[0].Input("0");
        await Schliessen(cut);
        cut.WaitForAssertion(() => Assert.Contains(
            "Verlegetiefe muss größer als 0 m sein", cut.Instance.Meldung));

        // Kollektor: Tiefe > 10
        cut = Zeige(Kollektor());
        cut.FindAll("input.epos-eingabe")[0].Input("12");
        await Schliessen(cut);
        cut.WaitForAssertion(() => Assert.Contains("nicht tiefer als 10 m", cut.Instance.Meldung));

        // Kollektor: Flaeche 0
        cut = Zeige(Kollektor());
        cut.FindAll("input.epos-eingabe")[1].Input("0");
        await Schliessen(cut);
        cut.WaitForAssertion(() => Assert.Contains("Kollektorfläche eintragen", cut.Instance.Meldung));

        // Sonde: keine Zahl
        cut = Zeige(Sonde());
        cut.FindAll("input.epos-eingabe")[2].Input("");
        await Schliessen(cut);
        cut.WaitForAssertion(() => Assert.Contains(
            "gültige Zahlenwerte für Sondenlänge und Anzahl", cut.Instance.Meldung));

        // Sonde: Laenge 0
        cut = Zeige(Sonde());
        cut.FindAll("input.epos-eingabe")[2].Input("0");
        await Schliessen(cut);
        cut.WaitForAssertion(() => Assert.Contains(
            "Sondenlänge muss größer als 0 m sein", cut.Instance.Meldung));

        // Sonde: Anzahl 0
        cut = Zeige(Sonde());
        cut.FindAll("input.epos-eingabe")[3].Input("0");
        await Schliessen(cut);
        cut.WaitForAssertion(() => Assert.Contains("mindestens eine Sonde", cut.Instance.Meldung));

        // Beide: Spreizung 0
        cut = Zeige(Kollektor());
        cut.FindAll("input.epos-eingabe")[4].Input("0");
        await Schliessen(cut);
        cut.WaitForAssertion(() => Assert.Contains(
            "nutzbare Spreizung größer als 0 K", cut.Instance.Meldung));
    }

    /// <summary>
    /// Rueckschreiben KOLLEKTOR (:1224-1227): Quellsystem, Tiefe, Flaeche und
    /// ausdruecklich Anzahl = 0.
    /// </summary>
    [Fact]
    public async Task Das_Schliessen_schreibt_den_Kollektorzweig_zurueck()
    {
        QuelleErdreichDaten? ergebnis = null;
        var cut = Zeige(Kollektor(), d => ergebnis = d);

        await Schliessen(cut);

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
    public async Task Das_Schliessen_schreibt_den_Sondenzweig_zurueck()
    {
        QuelleErdreichDaten? ergebnis = null;
        var cut = Zeige(Sonde(), d => ergebnis = d);

        await Schliessen(cut);

        Assert.NotNull(ergebnis);
        Assert.Equal(ErdreichTemperatur.QUELLSYSTEM_SONDE, ergebnis!.Quellsystem);
        Assert.Equal(120.0, ergebnis.Tiefe);
        Assert.Equal(0.0, ergebnis.Flaeche);
        Assert.Equal(4, ergebnis.Anzahl);
    }

    /// <summary>
    /// #275, der Kern des Wunsches: Das Kreuz darf die Eingaben NICHT still
    /// verwerfen. Ist eine Regel verletzt, meldet der Dialog sichtbar, gibt NICHTS
    /// zurueck - auch kein <c>null</c> - und bleibt mit allen Eingaben stehen.
    /// </summary>
    [Fact]
    public async Task Eine_verletzte_Regel_haelt_den_Dialog_offen()
    {
        int gemeldet = 0;
        QuelleErdreichDaten? ergebnis = null;
        var cut = Zeige(Kollektor(), d => { gemeldet++; ergebnis = d; });

        cut.FindAll("input.epos-eingabe")[0].Input("0");        // Verlegetiefe 0
        await Schliessen(cut);

        cut.WaitForAssertion(() => Assert.Contains(
            "Verlegetiefe muss größer als 0 m sein",
            cut.Find(".epos-warnbanner").TextContent));

        Assert.Equal(0, gemeldet);
        Assert.Null(ergebnis);

        // Der Dialog steht noch, samt der eingetippten Flaeche.
        Assert.NotNull(cut.Find("div.epos-dialog"));
        Assert.Equal("250", cut.FindAll("input.epos-eingabe")[1].GetAttribute("value"));
    }

    /// <summary>
    /// Ist die Regel erfuellt, meldet dasselbe Schliessen die Daten - ein zweiter
    /// Anlauf nach der Berichtigung kommt also heraus.
    /// </summary>
    [Fact]
    public async Task Nach_der_Berichtigung_kommt_der_Anwender_heraus()
    {
        QuelleErdreichDaten? ergebnis = null;
        var cut = Zeige(Kollektor(), d => ergebnis = d);

        cut.FindAll("input.epos-eingabe")[0].Input("0");
        await Schliessen(cut);
        Assert.Null(ergebnis);

        cut.FindAll("input.epos-eingabe")[0].Input("2,5");
        await Schliessen(cut);

        Assert.NotNull(ergebnis);
        Assert.Equal(2.5, ergebnis!.Tiefe);
        Assert.Equal("", cut.Instance.Meldung);
    }

    /// <summary>
    /// ESC ist derselbe Weg, keine zweite Kopie der Regeln: Mit gueltigen Werten
    /// uebernimmt er, mit einer verletzten Regel meldet er und der Dialog bleibt.
    /// </summary>
    [Fact]
    public void Esc_uebernimmt_und_meldet_wie_das_Kreuz()
    {
        QuelleErdreichDaten? ergebnis = null;
        var cut = Zeige(Kollektor(), d => ergebnis = d);

        cut.FindAll("input.epos-eingabe")[0].Input("0");
        cut.Find("div.epos-dialog").KeyDown("Escape");
        cut.WaitForAssertion(() => Assert.Contains(
            "Verlegetiefe muss größer als 0 m sein", cut.Instance.Meldung));
        Assert.Null(ergebnis);

        cut.FindAll("input.epos-eingabe")[0].Input("1,8");
        cut.Find("div.epos-dialog").KeyDown("Escape");
        cut.WaitForAssertion(() => Assert.NotNull(ergebnis));
        Assert.Equal(1.8, ergebnis!.Tiefe);
    }

    /// <summary>Esc schliesst zuerst die KARTE, nicht den Dialog (Hausregel).</summary>
    [Fact]
    public void Esc_schliesst_zuerst_die_Karte()
    {
        QuelleErdreichDaten? ergebnis = null;
        var cut = Zeige(Kollektor(), d => ergebnis = d);

        cut.FindAll("button").First(b => b.TextContent.Contains("…")).Click();
        Assert.True(cut.Instance.KarteOffen);

        cut.Find("div.epos-dialog").KeyDown("Escape");
        Assert.False(cut.Instance.KarteOffen);
        Assert.Null(ergebnis);             // der Dialog steht noch, er hat nichts gemeldet
    }

    /// <summary>
    /// Waehrend des Simulationslaufs schliesst der Dialog NICHT (Abweichung A-5):
    /// Der Wartezustand haelt ihn, Kreuz und Esc uebernehmen nichts.
    /// </summary>
    [Fact]
    public async Task Der_Wartezustand_verhindert_das_Uebernehmen()
    {
        var haenger = new TaskCompletionSource<(ErdreichAuswertung.ErdreichLaufErgebnis?, string?)>();
        QuelleErdreichDaten? ergebnis = null;

        var cut = Zeige(Kollektor(), d => ergebnis = d, simulieren: _ => haenger.Task);

        cut.FindAll("button").First(b => b.TextContent.Contains("Simulation")).Click();
        cut.WaitForState(() => cut.Instance.Laeuft);

        await Schliessen(cut);                              // das Kreuz
        cut.Find("div.epos-dialog").KeyDown("Escape");      // und Esc

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
}
