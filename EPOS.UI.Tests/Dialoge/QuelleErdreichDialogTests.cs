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
/// Herleitungszeilen, die Kennwertzeile, der Simulationsknopf, OK und Abbrechen.</para>
///
/// <para>KEINE DATENBANK: Die Fachrechnung liegt in ErdreichTemperatur,
/// VDI4640Pruefung und ErdreichAuswertung; alle drei rechnen aus dem uebergebenen
/// Aussentemperaturvektor bzw. dem Prozessspeicher.</para>
/// </summary>
public class QuelleErdreichDialogTests : BunitContext
{
    public QuelleErdreichDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Ein Aussentemperaturvektor, damit die Vorschau etwas zu rechnen hat.</summary>
    private static float[] Aussen()
    {
        var w = new float[8760];
        for (int i = 0; i < w.Length; i++)
            w[i] = (float)(9 + 12 * System.Math.Sin(2 * System.Math.PI * i / 8760.0 - System.Math.PI / 2));
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

    // ================================================================== Feldbestand

    [Fact]
    public void Der_Feldbestand_steht_vollstaendig()
    {
        var cut = Zeige(Kollektor());

        Assert.Equal("Wärmequelle Erdreich — WP Erdgeschoss",
                     cut.Find("h1.epos-dialog-titel").TextContent);

        // Zwei Wahlknoepfe (Kollektor/Sonde).
        Assert.Equal(2, cut.FindAll("input[type=radio]").Count);

        // Vier Zweigfelder + Spreizung = fuenf Zahlenfelder (davon eins ganzzahlig).
        Assert.Equal(5, cut.FindAll("input.epos-eingabe").Count);

        // Zwei Klapplisten: Bodentyp und Klimazone.
        Assert.Equal(2, cut.FindAll("select").Count);

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
        cut.FindAll("input.epos-eingabe")[4].Input("0");
        cut.Find("button.epos-knopf--primaer").Click();
        Assert.Contains("nutzbare Spreizung größer als 0 K", cut.Instance.Meldung);
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
}
