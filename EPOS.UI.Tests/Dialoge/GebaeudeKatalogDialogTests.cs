using System.Globalization;
using System.Threading;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Gebäude-Katalogeditor in VDI-6007-Struktur (Stufe G1 der Gebäudesimulation;
/// Umsetzungskonzept Gebäudesimulation 2.3–2.6, 2.10; Entscheide E2, E13, E20, E27/U1, U3).
///
/// <para><b>Was geprüft wird:</b> die Hülltabelle (acht Zeilen, U·A gerechnet, Fensterzeile
/// nur lesbar, Randbedingung nur an der Bodenplatte), die Wärmeleitwerte (H_T, H_ve, H_ges,
/// der gewichtete Wert nur auf dem Tagesbilanz-Weg), die Fenster nach Orientierung, die
/// Modellparameter (immer sichtbar, leer = Vorgabe als Platzhalter, gespeichert wird NULL),
/// der Schalter „Rechenweg" (Anzeige = Rechnung, NULL bleibt NULL), der Bestandswegabschnitt,
/// der EINE Schreibweg (OK prüft, speichert, schließt; Abbrechen schreibt nichts; der
/// hereingereichte Satz bleibt unberührt) — und die Bestandsfälle der drei Modi, der
/// Bauweise, des zweiten Reiters und des Assistenten.</para>
///
/// <para>Die Kultur ist auf de-DE gepinnt: Die Erwartungswerte sind deutsche
/// Beschriftungen und Zahlen.</para>
/// </summary>
public class GebaeudeKatalogDialogTests : EposBunitContext
{
    private static readonly string[] TYPEN = { "Einfamilienhaus", "Hotel" };
    private static readonly string[] ARTEN = { "Einfamilienhaus", "Hotel", "Kaufhaus" };
    private static readonly string[] KLASSEN =
    { "vor 1919", "1919 bis 1948", "1949 bis 1957", "1958 bis 1968", "1969 bis 1978",
      "1979 bis 1983", "1984 bis 1994", "1995 bis 2000", "Niedrigenergiebauweise",
      "Passivhaus", "EnEv 2007", "Eff. 70 (EnEV 2007)", "EnEV 2009",
      "Eff. 70 (EnEV 2009)", "Eff. 55 (EnEV 2009)", "EnEV 2014", "EnEV 2016",
      "Eff. 100 (EnEV 2016)", "Eff. 155 (EnEV 2016)", "BEG 55", "BEG 40" };
    private static readonly string[] NAMEN = { "Haus A", "Haus B", "Hotel C" };
    private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

    private const string REITER2 = "Temperaturen und Ferien";

    public GebaeudeKatalogDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>
    /// Ein vollständig belegter Satz. Hülle: AW 0,3·200 = 60; Fenster 1,3·45 = 58,5;
    /// Dach 0,2·120 = 24; Boden 0,35·100 = 35; Sonstiges 0,5·5 = 2,5; Wärmebrücke
    /// Fenster–Wand 0,1·50 = 5 → H_T = 185. H_ve = 0,5·150·2,5·0,34 = 63,75.
    /// </summary>
    private static GebaeudeKatalogDaten Satz(string name = "Haus A") => new()
    {
        Name = name,
        Typ = "Einfamilienhaus",
        Beschreibung = "Beschreibung " + name,
        Gebaeudeart = "Hotel",
        Verwendung = "Wohngebaeude",
        Baualtersklasse = 4,
        Bauart = 1,
        WohnflaecheGesamt = 150,
        FlaecheNutzer = 35,
        Waermegewinne = 400,
        Fensterdurchlassgrad = 0.4,
        Raumhoehe = 2.5,
        FensterflaecheNord = 10,
        FensterflaecheSued = 20,
        FensterflaecheOstWest = 15,
        FlaecheAussenwand = 200,
        Dachflaeche = 120,
        Grundflaeche = 100,
        SonstigeFlaechen = 5,
        UWertAussenwand = 0.3,
        UWertFenster = 1.3,
        UWertDachflaeche = 0.2,
        UWertGrundflaeche = 0.35,
        UWertSonstiges = 0.5,
        WbvkFensterWand = 0.1,
        AnschlussFensterWand = 50,
        SollTag = 20,
        NachtAbsenkung = 17,
        MaxTemperatur = 24,
        WochenendAbsenkung = 0,
        SollFerien = 0,
        Luftwechselrate = 0.5
    };

    private IRenderedComponent<GebaeudeKatalogDialog> Aufbauen(
        GebaeudeKatalogDaten? daten = null,
        GebaeudeKatalogModus modus = GebaeudeKatalogModus.Bearbeiten,
        Func<GebaeudeKatalogDaten, bool, string, GebaeudeKatalogErgebnis>? speichern = null,
        Func<string, GebaeudeKatalogDaten?>? lies = null,
        Func<IReadOnlyDictionary<string, object>>? brauchwasser = null,
        Action<bool>? geschlossen = null,
        bool titelAnzeigen = true)
        => Render<GebaeudeKatalogDialog>(p => p
            .Add(x => x.Daten, daten ?? Satz())
            .Add(x => x.TitelAnzeigen, titelAnzeigen)
            .Add(x => x.Modus, modus)
            .Add(x => x.Gebaeudetypen, () => TYPEN)
            .Add(x => x.Gebaeudearten, () => ARTEN)
            .Add(x => x.Baualtersklassen, KLASSEN)
            .Add(x => x.Katalognamen, () => NAMEN)
            .Add(x => x.Lies, lies ?? (n => Satz(n)))
            .Add(x => x.Speichern, speichern ?? ((_, _, _) => new GebaeudeKatalogErgebnis(true, "")))
            .Add(x => x.BrauchwasserGaben, brauchwasser)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IElement Knopf(IRenderedComponent<GebaeudeKatalogDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static void ReiterWaehlen(IRenderedComponent<GebaeudeKatalogDialog> cut, string titel)
        => cut.FindAll("button[role=tab]").First(b => b.TextContent.Trim() == titel).Click();

    /// <summary>Das Feld (label.epos-feld) mit dieser Beschriftung.</summary>
    private static IElement Feld(IRenderedComponent<GebaeudeKatalogDialog> cut, string beschriftung)
        => cut.FindAll("label.epos-feld")
              .First(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == beschriftung);

    private static IElement Eingabe(IRenderedComponent<GebaeudeKatalogDialog> cut, string beschriftung)
        => Feld(cut, beschriftung).QuerySelector("input")!;

    private static IElement Klappliste(IRenderedComponent<GebaeudeKatalogDialog> cut, string beschriftung)
        => Feld(cut, beschriftung).QuerySelector("select")!;

    private static IElement Zeile(IRenderedComponent<GebaeudeKatalogDialog> cut, string bauteil)
        => cut.Find($"table.epos-huelltabelle tr[data-bauteil={bauteil}]");

    private static string Wk(double wert) => wert.ToString("N1", DE) + " W/K";

    private static void Ok(IRenderedComponent<GebaeudeKatalogDialog> cut)
        => cut.Find(".epos-leiste button.epos-knopf--primaer").Click();

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Der_erste_Reiter_traegt_die_Gruppen_der_VDI_Struktur()
    {
        var cut = Aufbauen();

        foreach (string gruppe in new[] { "Kenngrößen", "Hülle: Transmission je Bauteil",
                                          "Wärmeleitwerte", "Fenster nach Orientierung",
                                          "Modellparameter (VDI 6007)", "Rechenweg" })
            Assert.Contains(gruppe, cut.Markup);

        // Die alten Gruppen sind aufgeloest (Konzept 2.3).
        Assert.DoesNotContain("U-Werte [W/m²K]", cut.Markup);
        Assert.DoesNotContain("Flächen [m²]", cut.Markup);
        Assert.Contains("Nutzfläche :", cut.Markup);
        Assert.DoesNotContain("Wohn-/Nutzfläche", cut.Markup);
    }

    [Fact]
    public void Die_Luftwechselrate_steht_bei_den_Kenngroessen()
    {
        var cut = Aufbauen();

        Assert.Equal("0,5", Eingabe(cut, "Luftwechselrate :").GetAttribute("value"));
    }

    /// <summary>Der Designer schreibt „Fläschen"; gemeint sind Flächen (A-4).</summary>
    [Fact]
    public void Der_Titel_ist_berichtigt()
    {
        var cut = Aufbauen();

        Assert.Contains("Flächen", cut.Find(".epos-dialog-titel").TextContent);
        Assert.DoesNotContain("Fläschen", cut.Markup);
    }

    [Fact]
    public void Der_zweite_Reiter_traegt_Raumtemperaturen_und_Ferien()
    {
        var cut = Aufbauen();
        ReiterWaehlen(cut, REITER2);

        // 5 Raumtemperaturen, dazu 16 Ganzzahlfelder fuer die vier Ferienzeitraeume.
        Assert.Equal(5, cut.FindAll("input[inputmode=decimal]").Count);
        Assert.Equal(16, cut.FindAll("input[inputmode=numeric]").Count);

        Assert.Contains("Raumtemperaturen", cut.Markup);
        Assert.Contains("Ferien Anfang", cut.Markup);
        Assert.Contains("Ferien Ende", cut.Markup);
        Assert.Contains("Winter :", cut.Markup);
        // Waermebruecken und Anschlussmasse stehen jetzt in der Huelltabelle, und der
        // Uebernahmeknopf ist entfallen (M-2).
        Assert.DoesNotContain("Wärmebrückenverlustkoeffizienten", cut.Markup);
        Assert.DoesNotContain("Abmessung Anschluß", cut.Markup);
        Assert.DoesNotContain("Werte übernehmen", cut.Markup);
    }

    /// <summary>Ein nicht gewähltes Blatt wird GAR NICHT gezeichnet (Baustein `Reiterblatt`).</summary>
    [Fact]
    public void Der_zweite_Reiter_erscheint_erst_beim_Betreten()
    {
        var cut = Aufbauen();

        Assert.DoesNotContain("Ferien Anfang", cut.Markup);
        ReiterWaehlen(cut, REITER2);
        Assert.Contains("Ferien Anfang", cut.Markup);
    }

    // =================================================================================
    // Die U*A-Tabelle und die Waermeleitwerte (Konzept 2.5)
    // =================================================================================

    [Fact]
    public void Die_Huelltabelle_fuehrt_acht_Zeilen()
    {
        var cut = Aufbauen();

        var zeilen = cut.FindAll("table.epos-huelltabelle tbody tr");
        Assert.Equal(8, zeilen.Count);
        Assert.Equal(new[] { "Außenwand", "Fenster", "Dach", "Bodenplatte", "Sonstiges",
                             "Wärmebrücke Fenster–Wand", "Wärmebrücke Außenwand–Keller",
                             "Wärmebrücke Wand–Dach" },
                     zeilen.Select(z => z.QuerySelector("th")!.TextContent.Trim()));
    }

    [Fact]
    public void Jede_Huellzeile_zeigt_U_mal_A()
    {
        var cut = Aufbauen();

        Assert.Equal("60,0", Zeile(cut, "Aussenwand").QuerySelector("td:last-child")!.TextContent.Trim());
        Assert.Equal("58,5", Zeile(cut, "Fenster").QuerySelector("td:last-child")!.TextContent.Trim());
        Assert.Equal("5,0", Zeile(cut, "WaermebrueckeFensterWand").QuerySelector("td:last-child")!.TextContent.Trim());

        // Eine Eingabe rechnet die Zeile sofort nach.
        Zeile(cut, "Dach").QuerySelectorAll("input")[0].Input("0,5");
        Assert.Equal("60,0", Zeile(cut, "Dach").QuerySelector("td:last-child")!.TextContent.Trim());
    }

    /// <summary>Die Fensterfläche ist gerechnet (Summe der vier Orientierungen), nicht eingegeben.</summary>
    [Fact]
    public void Die_Fensterzeile_ist_nur_lesbar()
    {
        var cut = Aufbauen();

        IElement fenster = Zeile(cut, "Fenster");
        Assert.Single(fenster.QuerySelectorAll("input"));      // nur U
        Assert.Contains("45,00 m²", fenster.TextContent);        // 20 + 15 + 10
    }

    [Fact]
    public void Nur_die_Bodenplatte_hat_eine_Randbedingung()
    {
        var cut = Aufbauen();

        Assert.Single(cut.FindAll("table.epos-huelltabelle select"));
        Assert.Single(Zeile(cut, "Bodenplatte").QuerySelectorAll("select"));
        Assert.Contains("Außenluft", Zeile(cut, "Aussenwand").TextContent);
    }

    [Fact]
    public void Die_Randbedingung_Keller_zeigt_die_Kellertemperatur_mit_Vorgabe()
    {
        GebaeudeKatalogDaten geschrieben = null!;
        var cut = Aufbauen(speichern: (d, _, _) => { geschrieben = d; return new(true, ""); });

        Assert.DoesNotContain("Kellertemperatur", cut.Markup);
        Zeile(cut, "Bodenplatte").QuerySelector("select")!.Change("1");   // Keller

        IElement keller = Eingabe(cut, "Kellertemperatur :");
        Assert.Equal("Vorgabe 10", keller.GetAttribute("placeholder"));

        Ok(cut);
        Assert.Equal(DbWerte.GRUND_KELLER, geschrieben.GrundflaecheRandbedingung);
        Assert.Null(geschrieben.Kellertemperatur);
    }

    [Fact]
    public void Erdreich_bleibt_NULL_wenn_die_Randbedingung_nicht_angefasst_wird()
    {
        GebaeudeKatalogDaten geschrieben = null!;
        var cut = Aufbauen(speichern: (d, _, _) => { geschrieben = d; return new(true, ""); });

        Zeile(cut, "Bodenplatte").QuerySelector("select")!.Change("2");   // Aussenluft
        Zeile(cut, "Bodenplatte").QuerySelector("select")!.Change("0");   // zurueck: Erdreich
        Ok(cut);

        Assert.Null(geschrieben.GrundflaecheRandbedingung);
    }

    [Fact]
    public void H_T_ist_die_Summe_der_acht_Zeilen()
    {
        var cut = Aufbauen();

        Assert.Equal(Wk(185.0), Eingabe(cut, "H_T Transmission :").GetAttribute("value"));
    }

    [Fact]
    public void H_ve_kommt_aus_Luftwechsel_Nutzflaeche_und_Raumhoehe()
    {
        var cut = Aufbauen();

        Assert.Equal(Wk(0.5 * 150 * 2.5 * 0.34), Eingabe(cut, "H_ve Lüftung :").GetAttribute("value"));
    }

    [Fact]
    public void H_ges_ist_H_T_plus_H_ve()
    {
        var cut = Aufbauen();

        Assert.Equal(Wk(185.0 + 0.5 * 150 * 2.5 * 0.34), Eingabe(cut, "H_ges gesamt :").GetAttribute("value"));
    }

    /// <summary>
    /// Die gewichtete Zeile steht nur auf dem Tagesbilanz-Weg und trägt die Gewichte des
    /// Bestandswegs (0,83 / 0,95 / 0,45); die Kernprobe hält sie gegen
    /// <c>SpezWaermeverlusteC</c> (<c>GebaeudehuellbilanzTests</c>).
    /// </summary>
    [Fact]
    public void Der_gewichtete_Wert_steht_nur_im_Tagesbilanz_Weg()
    {
        GebaeudeKatalogDaten daten = Satz();
        daten.Modell = DbWerte.GEBAEUDE_MODELL_TAGESBILANZ;
        var cut = Aufbauen(daten);

        double gewichtet = 0.83 * 60 + 58.5 + 0.95 * 24 + 0.45 * 35 + 2.5 + 0.83 * 5;
        Assert.Equal(Wk(gewichtet), Eingabe(cut, "H_T gewichtet (Tagesbilanz) :").GetAttribute("value"));
        Assert.Contains("wichtet Außenwand und Wärmebrücken mit 0,83", cut.Markup);

        Klappliste(cut, "Rechenweg :").Change("0");   // VDI 6007
        Assert.DoesNotContain("H_T gewichtet", cut.Markup);
        Assert.DoesNotContain("wichtet Außenwand", cut.Markup);
    }

    // =================================================================================
    // Fenster nach Orientierung (Konzept 2.6)
    // =================================================================================

    [Fact]
    public void Ost_und_West_stehen_getrennt_und_zeigen_die_Haelfte_als_Vorgabe()
    {
        var cut = Aufbauen();

        Assert.Equal("Vorgabe 7,5", Eingabe(cut, "Fensterfläche Ost :").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe 7,5", Eingabe(cut, "Fensterfläche West :").GetAttribute("placeholder"));
        Assert.Equal("15,00", Eingabe(cut, "Summe Ost + West :").GetAttribute("value"));
        Assert.Equal("45,00", Eingabe(cut, "gesamte Fensterfläche :").GetAttribute("value"));
    }

    [Fact]
    public void Die_Summe_Ost_West_wird_gerechnet_und_mitgeschrieben()
    {
        GebaeudeKatalogDaten geschrieben = null!;
        var cut = Aufbauen(speichern: (d, _, _) => { geschrieben = d; return new(true, ""); });

        Eingabe(cut, "Fensterfläche Ost :").Input("6");
        Eingabe(cut, "Fensterfläche West :").Input("12");
        Assert.Equal("18,00", Eingabe(cut, "Summe Ost + West :").GetAttribute("value"));
        Ok(cut);

        Assert.Equal(6, geschrieben.FensterflaecheOst);
        Assert.Equal(12, geschrieben.FensterflaecheWest);
        Assert.Equal(18, geschrieben.FensterflaecheOstWest);
    }

    /// <summary>Beide leer heißt NULL — die Hälfte rechnet der Kern, nicht der Dialog.</summary>
    [Fact]
    public void Beide_leer_heisst_NULL_und_das_Bestandsfeld_bleibt()
    {
        GebaeudeKatalogDaten geschrieben = null!;
        var cut = Aufbauen(speichern: (d, _, _) => { geschrieben = d; return new(true, ""); });

        Ok(cut);

        Assert.Null(geschrieben.FensterflaecheOst);
        Assert.Null(geschrieben.FensterflaecheWest);
        Assert.Equal(15, geschrieben.FensterflaecheOstWest);
    }

    [Fact]
    public void Ost_ohne_West_meldet_beim_OK()
    {
        bool geschrieben = false;
        var cut = Aufbauen(speichern: (_, _, _) => { geschrieben = true; return new(true, ""); });

        Eingabe(cut, "Fensterfläche Ost :").Input("6");
        Ok(cut);

        Assert.False(geschrieben);
        Assert.Contains("Ost und West bitte beide", cut.Instance.Meldung);
    }

    // =================================================================================
    // Modellparameter und Rechenweg (Konzept 2.4, E20, ADR-006)
    // =================================================================================

    [Fact]
    public void Ein_leeres_Parameterfeld_zeigt_seine_Vorgabe()
    {
        var cut = Aufbauen();

        Assert.Equal("Vorgabe 0,3", Eingabe(cut, "Rahmenanteil :").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe 0,9", Eingabe(cut, "Verschattungsfaktor :").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe 2,5", Eingabe(cut, "Innenflächenfaktor :").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe: unbegrenzt", Eingabe(cut, "Heizleistungsgrenze :").GetAttribute("placeholder"));
        Assert.Equal("", Eingabe(cut, "Rahmenanteil :").GetAttribute("value") ?? "");
    }

    [Fact]
    public void Ein_leeres_Parameterfeld_speichert_NULL_nicht_die_Vorgabe()
    {
        GebaeudeKatalogDaten geschrieben = null!;
        var cut = Aufbauen(speichern: (d, _, _) => { geschrieben = d; return new(true, ""); });

        Eingabe(cut, "Masseanteil außen :").Input("0,4");
        Ok(cut);

        Assert.Null(geschrieben.Rahmenanteil);
        Assert.Null(geschrieben.Verschattungsfaktor);
        Assert.Null(geschrieben.Innenflaechenfaktor);
        Assert.Null(geschrieben.HeizungStrahlungsanteil);
        Assert.Null(geschrieben.HeizleistungMax);
        Assert.Equal(0.4, geschrieben.MasseanteilAussen);
        Assert.False(geschrieben.AussenbauteileStrahlung);
    }

    // =================================================================================
    // Stufe G2: Infiltration, Nutzerlüftung, Sommerlüftung (Rechenschritte A7, 7.2)
    // =================================================================================

    private static IElement Kaestchen(IRenderedComponent<GebaeudeKatalogDialog> cut, string beschriftung)
        => cut.FindAll("label.epos-schalter")
              .First(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == beschriftung)
              .QuerySelector("input")!;

    [Fact]
    public void Die_Lueftungsfelder_stehen_bei_den_Modellparametern()
    {
        var cut = Aufbauen();

        Assert.NotNull(Eingabe(cut, "Infiltration :"));
        Assert.NotNull(Eingabe(cut, "Nutzerlüftung :"));
        Assert.NotNull(Kaestchen(cut, "Sommerlüftung"));
        // Beide leer: der VDI-Weg rechnet mit der Luftwechselrate - sie steht in der Herleitung.
        Assert.Contains("VDI 6007 rechnet mit 0,50 1/h (Luftwechselrate des Gebäudes).", cut.Markup);
        Assert.Equal("", Eingabe(cut, "Infiltration :").GetAttribute("placeholder") ?? "");
    }

    [Fact]
    public void Ein_gesetztes_Lueftungsfeld_zeigt_die_Vorgabe_des_anderen_und_die_Summe()
    {
        GebaeudeKatalogDaten daten = Satz();
        daten.Modell = DbWerte.GEBAEUDE_MODELL_VDI6007;
        var cut = Aufbauen(daten);

        Eingabe(cut, "Infiltration :").Input("0,2");

        Assert.Equal("Vorgabe 0,4", Eingabe(cut, "Nutzerlüftung :").GetAttribute("placeholder"));
        Assert.Contains("VDI 6007 rechnet mit 0,60 1/h (Infiltration + Nutzerlüftung).", cut.Markup);
        // H_ve folgt auf dem VDI-Weg dem wirksamen Luftwechsel.
        Assert.Equal(Wk(0.6 * 150 * 2.5 * 0.34), Eingabe(cut, "H_ve Lüftung :").GetAttribute("value"));
    }

    [Fact]
    public void Die_Lueftungsfelder_speichern_leer_als_NULL_und_gesetzt_mit_Wert()
    {
        GebaeudeKatalogDaten geschrieben = null!;
        var cut = Aufbauen(speichern: (d, _, _) => { geschrieben = d; return new(true, ""); });
        Ok(cut);
        Assert.Null(geschrieben.LuftwechselInfiltration);
        Assert.Null(geschrieben.LuftwechselNutzer);
        Assert.False(geschrieben.Sommerlueftung);

        var cut2 = Aufbauen(speichern: (d, _, _) => { geschrieben = d; return new(true, ""); });
        Eingabe(cut2, "Nutzerlüftung :").Input("0,8");
        Kaestchen(cut2, "Sommerlüftung").Change(true);
        Assert.Contains("steigt der Luftwechsel auf 2,0 1/h", cut2.Markup);
        Ok(cut2);
        Assert.Null(geschrieben.LuftwechselInfiltration);
        Assert.Equal(0.8, geschrieben.LuftwechselNutzer);
        Assert.True(geschrieben.Sommerlueftung);
    }

    [Fact]
    public void Eine_Infiltration_von_0_faerbt_das_Feld()
    {
        var cut = Aufbauen();
        Eingabe(cut, "Infiltration :").Input("0");

        Assert.Contains("epos-fehleingabe", Eingabe(cut, "Infiltration :").ClassName ?? "");
    }

    [Fact]
    public void Die_Rechenwegliste_fuehrt_zwei_Eintraege()
    {
        var cut = Aufbauen();

        var optionen = Klappliste(cut, "Rechenweg :").QuerySelectorAll("option")
                                                       .Select(o => o.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "VDI 6007", "Tagesbilanz" }, optionen);
    }

    /// <summary>
    /// <b>Die Anzeige folgt der Rechnung</b> (ADR-006): Ein Gebäude ohne Angabe steht auf dem
    /// Weg, den die Weiche für NULL nimmt — VDI 6007 —, und die Herleitungszeile sagt, dass die Vorgabe gilt.
    /// </summary>
    [Fact]
    public void Ohne_Angabe_zeigt_der_Schalter_den_Weg_der_Vorgabe()
    {
        var cut = Aufbauen();

        Assert.Equal(Gebaeuderechenweg.IstVdi6007(null), cut.Instance.IstVdi6007);
        string erwartet = Gebaeuderechenweg.IstVdi6007(null) ? "0" : "1";
        Assert.Equal(erwartet, Klappliste(cut, "Rechenweg :").QuerySelector("option[selected]")!
                                                             .GetAttribute("value"));
        Assert.Contains("es gilt die Vorgabe des Programms", cut.Instance.Rechenwegzeile);
    }

    [Fact]
    public void Wer_den_Schalter_nicht_anfasst_behaelt_NULL()
    {
        GebaeudeKatalogDaten geschrieben = null!;
        var cut = Aufbauen(speichern: (d, _, _) => { geschrieben = d; return new(true, ""); });

        // Hin und zurueck: die Wahl kehrt auf den Weg der Vorgabe zurueck.
        string vorgabe = Gebaeuderechenweg.IstVdi6007(null) ? "0" : "1";
        string anderer = vorgabe == "0" ? "1" : "0";
        Klappliste(cut, "Rechenweg :").Change(anderer);
        Klappliste(cut, "Rechenweg :").Change(vorgabe);
        Ok(cut);

        Assert.Null(geschrieben.Modell);
    }

    [Fact]
    public void Der_Schalter_schreibt_den_gewaehlten_Rechenweg()
    {
        GebaeudeKatalogDaten geschrieben = null!;
        GebaeudeKatalogDaten daten = Satz();
        daten.Modell = DbWerte.GEBAEUDE_MODELL_TAGESBILANZ;
        var cut = Aufbauen(daten, speichern: (d, _, _) => { geschrieben = d; return new(true, ""); });

        Klappliste(cut, "Rechenweg :").Change("0");
        Assert.True(cut.Instance.IstVdi6007);
        Ok(cut);

        Assert.Equal(DbWerte.GEBAEUDE_MODELL_VDI6007, geschrieben.Modell);
    }

    [Fact]
    public void Die_Rechenwegzeile_wechselt_mit_der_Wahl()
    {
        GebaeudeKatalogDaten daten = Satz();
        daten.Modell = DbWerte.GEBAEUDE_MODELL_VDI6007;
        var cut = Aufbauen(daten);

        Assert.StartsWith("VDI 6007: Raumtemperatur", cut.Instance.Rechenwegzeile);
        Assert.DoesNotContain("Vorgabe des Programms", cut.Instance.Rechenwegzeile);

        Klappliste(cut, "Rechenweg :").Change("1");
        Assert.StartsWith("Tagesbilanz: der eingefrorene Bestandsweg", cut.Instance.Rechenwegzeile);
    }

    /// <summary>
    /// Ersatzfall 1 (E20, Konzept 2.10): Der Abschnitt „Tagesbilanz (Bestandsweg)" erscheint
    /// allein auf dem Tagesbilanz-Weg — mit Gebäudetyp und der schreibgesperrten Fensterfläche
    /// Ost + West; auf VDI 6007 gibt es ihn gar nicht.
    /// </summary>
    [Fact]
    public void Der_Bestandswegabschnitt_erscheint_nur_auf_dem_Tagesbilanz_Weg()
    {
        GebaeudeKatalogDaten daten = Satz();
        daten.Modell = DbWerte.GEBAEUDE_MODELL_TAGESBILANZ;
        var cut = Aufbauen(daten);

        IElement abschnitt = cut.Find("details.epos-tagesbilanz");
        Assert.Contains("Tagesbilanz (Bestandsweg)", abschnitt.QuerySelector("summary")!.TextContent);
        Assert.Contains("Gebäudetyp :", abschnitt.TextContent);
        IElement ostWest = Feld(cut, "Fensterfläche Ost + West :").QuerySelector("input")!;
        Assert.True(ostWest.HasAttribute("readonly"));

        Klappliste(cut, "Rechenweg :").Change("0");
        Assert.Empty(cut.FindAll("details.epos-tagesbilanz"));
        Assert.DoesNotContain("Gebäudetyp :", cut.Markup);
    }

    /// <summary>
    /// Ersatzfall 2 (E20): Die Modellparameter stehen in BEIDEN Stellungen, bleiben beim
    /// Umschalten stehen und werden auch auf dem Tagesbilanz-Weg gespeichert.
    /// </summary>
    [Fact]
    public void Beim_Umschalten_bleiben_die_Modellparameter_stehen_und_werden_gespeichert()
    {
        GebaeudeKatalogDaten geschrieben = null!;
        GebaeudeKatalogDaten daten = Satz();
        daten.Modell = DbWerte.GEBAEUDE_MODELL_VDI6007;
        var cut = Aufbauen(daten, speichern: (d, _, _) => { geschrieben = d; return new(true, ""); });

        Eingabe(cut, "Rahmenanteil :").Input("0,25");
        Klappliste(cut, "Rechenweg :").Change("1");      // Tagesbilanz

        foreach (string feld in new[] { "Rahmenanteil :", "Verschattungsfaktor :", "Masseanteil außen :",
                                        "Innenflächenfaktor :", "Strahlungsanteil Heizung :",
                                        "Heizleistungsgrenze :" })
            Assert.NotNull(Eingabe(cut, feld));
        Assert.Contains("Außenbauteile mit Strahlung", cut.Markup);
        Assert.Equal("0,25", Eingabe(cut, "Rahmenanteil :").GetAttribute("value"));

        Ok(cut);
        Assert.Equal(DbWerte.GEBAEUDE_MODELL_TAGESBILANZ, geschrieben.Modell);
        Assert.Equal(0.25, geschrieben.Rahmenanteil);
    }

    // =================================================================================
    // Pruefregeln (Konzept 4.8) - eine Stelle, im OK-Weg
    // =================================================================================

    [Fact]
    public void Ein_U_Wert_ausserhalb_0_1_bis_6_faerbt_und_wird_nicht_uebernommen()
    {
        var cut = Aufbauen();

        IElement u = Zeile(cut, "Aussenwand").QuerySelectorAll("input")[0];
        u.Input("7");

        Assert.Contains("epos-fehleingabe", Zeile(cut, "Aussenwand").QuerySelectorAll("input")[0].ClassName);
        Assert.Equal(0.3, cut.Instance.Arbeitsstand.UWertAussenwand);

        Ok(cut);
        Assert.Contains("U-Wert Außenwand", cut.Instance.Meldung);
    }

    [Fact]
    public void Ein_gespeicherter_U_Wert_ausserhalb_des_Bereichs_meldet_beim_OK()
    {
        bool geschrieben = false;
        GebaeudeKatalogDaten daten = Satz();
        daten.UWertDachflaeche = 8;
        var cut = Aufbauen(daten, speichern: (_, _, _) => { geschrieben = true; return new(true, ""); });

        Ok(cut);

        Assert.False(geschrieben);
        Assert.Contains("Der U-Wert Dach muss zwischen 0,1 und 6", cut.Instance.Meldung);
    }

    [Fact]
    public void Ein_g_Wert_ueber_1_meldet_beim_OK()
    {
        GebaeudeKatalogDaten daten = Satz();
        daten.Fensterdurchlassgrad = 1.5;
        var cut = Aufbauen(daten);

        Ok(cut);

        Assert.Contains("Fensterdurchlaßgrad muss größer als 0 und höchstens 1", cut.Instance.Meldung);
    }

    [Fact]
    public void Eine_Luftwechselrate_von_0_meldet_beim_OK()
    {
        GebaeudeKatalogDaten daten = Satz();
        daten.Luftwechselrate = 0;
        var cut = Aufbauen(daten);

        Ok(cut);

        Assert.Contains("Luftwechselrate muss größer als 0", cut.Instance.Meldung);
    }

    [Fact]
    public void Eine_Bauweise_ausserhalb_5_bis_200_je_m2_meldet_beim_OK()
    {
        GebaeudeKatalogDaten daten = Satz();
        daten.Bauweise = 50;               // absolut 50 auf 150 m² = 0,33 Wh/(m²K)
        var cut = Aufbauen(daten);

        Ok(cut);

        Assert.Contains("Die Bauweise muss zwischen 5 und 200", cut.Instance.Meldung);
    }

    [Fact]
    public void Eine_fehlende_Pflichtzahl_meldet_mit_ihrem_Feldnamen()
    {
        bool geschrieben = false;
        GebaeudeKatalogDaten daten = Satz();
        daten.Raumhoehe = null;

        var cut = Aufbauen(daten, speichern: (_, _, _) => { geschrieben = true; return new(true, ""); });

        Ok(cut);

        Assert.False(geschrieben);
        Assert.Contains("Raumhöhe", cut.Instance.Meldung);
    }

    [Fact]
    public void Ein_leerer_Name_meldet_beim_OK_im_Modus_Neu()
    {
        bool geschrieben = false;
        GebaeudeKatalogDaten daten = Satz("");

        var cut = Aufbauen(daten, modus: GebaeudeKatalogModus.Neu,
                           speichern: (_, _, _) => { geschrieben = true; return new(true, ""); });

        Ok(cut);

        Assert.False(geschrieben);
        Assert.Contains("Gebäudenamen", cut.Instance.Meldung);
    }

    [Fact]
    public void Eine_verletzte_Ferienregel_springt_auf_den_zweiten_Reiter()
    {
        var cut = Aufbauen();
        ReiterWaehlen(cut, REITER2);

        var ganzzahl = cut.FindAll("input[inputmode=numeric]");
        ganzzahl[0].Input("1");    // Winter Beginn: 1.2.  -> Jahrestag 32
        ganzzahl[1].Input("2");
        var ende = cut.FindAll("input[inputmode=numeric]");
        ende[8].Input("1");        // Winter Ende:   1.3.  -> Jahrestag 60 > 32
        ende[9].Input("3");

        ReiterWaehlen(cut, "Gebäude und Hülle");
        Ok(cut);

        Assert.Contains("Jahresgrenze", cut.Instance.Meldung);
        Assert.Equal("TEMPERATUREN", cut.Instance.AktiverReiter);
    }

    // =================================================================================
    // Der EINE Schreibweg (E27/U1)
    // =================================================================================

    [Fact]
    public void OK_prueft_speichert_und_schliesst()
    {
        int laeufe = 0;
        bool? geschlossen = null;
        string bezeichner = "";
        bool? neu = null;
        var cut = Aufbauen(
            speichern: (_, istNeu, bez) => { laeufe++; neu = istNeu; bezeichner = bez; return new(true, ""); },
            geschlossen: b => geschlossen = b);

        Ok(cut);

        Assert.Equal(1, laeufe);
        Assert.False(neu);
        Assert.Equal("Haus A", bezeichner);
        Assert.True(geschlossen);
    }

    [Fact]
    public void Abbrechen_schreibt_nichts()
    {
        int laeufe = 0;
        bool? geschlossen = null;
        var cut = Aufbauen(speichern: (_, _, _) => { laeufe++; return new(true, ""); },
                           geschlossen: b => geschlossen = b);

        Eingabe(cut, "Raumhöhe :").Input("3");
        Knopf(cut, "Abbrechen").Click();

        Assert.Equal(0, laeufe);
        Assert.False(geschlossen);
    }

    [Fact]
    public void Der_hereingereichte_Satz_bleibt_unberuehrt()
    {
        GebaeudeKatalogDaten daten = Satz();
        var cut = Aufbauen(daten);

        Eingabe(cut, "Raumhöhe :").Input("3");
        Klappliste(cut, "Bauart :").Change("2");
        Ok(cut);

        Assert.Equal(2.5, daten.Raumhoehe);
        Assert.Equal(0, daten.Bauweise);
        Assert.Equal(3, cut.Instance.Arbeitsstand.Raumhoehe);
    }

    [Fact]
    public void Eine_abgelehnte_Schreibung_haelt_den_Dialog_offen()
    {
        bool? geschlossen = null;
        var cut = Aufbauen(speichern: (_, _, _) =>
            new GebaeudeKatalogErgebnis(false, "Dieser Stammdatensatz ist schreibgeschützt"),
            geschlossen: b => geschlossen = b);

        Ok(cut);

        Assert.Contains("schreibgeschützt", cut.Instance.Meldung);
        Assert.Single(cut.FindAll("[role=alert]"));
        Assert.Null(geschlossen);
    }

    [Fact]
    public void Ueberschreiben_trifft_den_Ursprungsnamen()
    {
        string bezeichner = "";
        var cut = Aufbauen(speichern: (_, _, bez) => { bezeichner = bez; return new(true, ""); });

        cut.FindAll("input[type=text]").First(i => i.GetAttribute("value") == "Haus A").Input("Haus NEU");
        Ok(cut);

        Assert.Equal("Haus A", bezeichner);
    }

    [Fact]
    public void Speichern_unter_legt_unter_dem_neuen_Namen_an_und_schliesst()
    {
        string bezeichner = "";
        bool? neu = null;
        bool? geschlossen = null;
        var cut = Aufbauen(speichern: (_, istNeu, bez) => { neu = istNeu; bezeichner = bez; return new(true, ""); },
                           geschlossen: b => geschlossen = b);

        cut.FindAll("input[type=text]").First(i => i.GetAttribute("value") == "Haus A").Input("Haus Kopie");
        Knopf(cut, "Speichern unter").Click();

        Assert.True(neu);
        Assert.Equal("Haus Kopie", bezeichner);
        Assert.True(geschlossen);
    }

    // =================================================================================
    // Die drei Modi
    // =================================================================================

    [Fact]
    public void Die_Fussleiste_ist_die_SpeichernLeiste()
    {
        var cut = Aufbauen();
        var leisten = cut.FindAll(".epos-dialog > .epos-leiste");
        IElement fuss = leisten[leisten.Count - 1];

        var knoepfe = fuss.QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Speichern unter", "Abbrechen", "OK" }, knoepfe);

        var primaer = fuss.QuerySelectorAll("button.epos-knopf--primaer");
        Assert.Single(primaer);
        Assert.Equal("OK", primaer[0].TextContent.Trim());
        Assert.Contains("legt einen neuen Katalogsatz", cut.Markup);
    }

    [Fact]
    public void Im_Modus_Neu_gibt_es_kein_Speichern_unter()
    {
        var cut = Aufbauen(daten: new GebaeudeKatalogDaten(), modus: GebaeudeKatalogModus.Neu);

        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Speichern unter");
        Assert.NotNull(Knopf(cut, "OK"));
    }

    [Fact]
    public void Im_Modus_Neu_legt_OK_an()
    {
        bool? neu = null;
        string bezeichner = "";
        var cut = Aufbauen(daten: Satz("Neubau"), modus: GebaeudeKatalogModus.Neu,
                           speichern: (_, istNeu, bez) => { neu = istNeu; bezeichner = bez; return new(true, ""); });

        Ok(cut);

        Assert.True(neu);
        Assert.Equal("Neubau", bezeichner);
    }

    [Fact]
    public void Im_Modus_Admin_ist_der_Name_eine_Klappliste_ohne_Speichern_unter()
    {
        var cut = Aufbauen(modus: GebaeudeKatalogModus.Admin);

        Assert.NotNull(Klappliste(cut, "Name :"));
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Speichern unter");
        Assert.Contains("Haus A", cut.Markup);
        Assert.Contains("Hotel C", cut.Markup);
    }

    [Fact]
    public void Im_Modus_Admin_laedt_der_Namenswechsel_den_gewaehlten_Satz()
    {
        var cut = Aufbauen(modus: GebaeudeKatalogModus.Admin);

        Klappliste(cut, "Name :").Change("2");   // Hotel C

        Assert.Equal("Hotel C", cut.Instance.Ursprungsname);
    }

    // =================================================================================
    // Baujahr, Bauart, Verwendung
    // =================================================================================

    [Fact]
    public void Die_Baujahrliste_fuehrt_21_Klassen()
    {
        var cut = Aufbauen();

        Assert.Equal(21, Klappliste(cut, "Baujahr :").QuerySelectorAll("option").Length);
        Assert.Contains("vor 1919", cut.Markup);
        Assert.Contains("BEG 40", cut.Markup);
    }

    /// <summary>Befund W9‑B8: Der Steuerwert der Verwendung ist getrennt vom Anzeigetext.</summary>
    [Fact]
    public void Die_Verwendung_traegt_den_Steuerwert_getrennt_vom_Anzeigetext()
    {
        var cut = Aufbauen();

        Assert.Contains("Wohngebäude", cut.Markup);
        Klappliste(cut, "Verwendung :").Change("1");
        Assert.Equal("Nicht Wohngebaeude", cut.Instance.Arbeitsstand.Verwendung);
    }

    [Fact]
    public void Die_Bauartliste_fuehrt_die_drei_Stufen()
    {
        var cut = Aufbauen();

        Assert.Contains("Leichte Bauart", cut.Markup);
        Assert.Contains("Schwere Bauart", cut.Markup);
        Assert.Contains("Sehr schwere Bauart", cut.Markup);
    }

    [Fact]
    public void Die_Bauartwahl_bildet_die_Bauweise_aus_der_Nutzflaeche()
    {
        var cut = Aufbauen();                       // Nutzfläche 150 m²

        Klappliste(cut, "Bauart :").Change("2");     // Sehr schwere Bauart
        Assert.Equal(2, cut.Instance.Arbeitsstand.Bauart);
        Assert.Equal(15000, cut.Instance.Arbeitsstand.Bauweise);

        Klappliste(cut, "Bauart :").Change("0");     // Leichte Bauart
        Assert.Equal(3000, cut.Instance.Arbeitsstand.Bauweise);
    }

    [Fact]
    public void Die_Gebaeudeartwahl_laesst_die_Bauweise_stehen()
    {
        var cut = Aufbauen();
        Klappliste(cut, "Bauart :").Change("1");     // Schwere Bauart -> 150 × 50
        double vorher = cut.Instance.Arbeitsstand.Bauweise;

        Klappliste(cut, "Gebäudeart :").Change("2"); // Kaufhaus

        Assert.Equal("Kaufhaus", cut.Instance.Arbeitsstand.Gebaeudeart);
        Assert.Equal(7500, vorher);
        Assert.Equal(vorher, cut.Instance.Arbeitsstand.Bauweise);
    }

    [Fact]
    public void Beim_Schreiben_kommt_die_Bauweise_aus_Bauart_und_Nutzflaeche()
    {
        double geschrieben = -1;
        var cut = Aufbauen(speichern: (d, _, _) => { geschrieben = d.Bauweise; return new(true, ""); });

        Klappliste(cut, "Bauart :").Change("1");     // Schwere Bauart
        Eingabe(cut, "Nutzfläche :").Input("200");   // Nutzfläche danach

        Ok(cut);

        Assert.Equal(10000, geschrieben);            // 200 × 50, nicht 150 × 50
    }

    /// <summary>
    /// Der freie Zahlenweg (Konzept 2.11, M-e): Eine gespeicherte Bauweise, die nicht aus
    /// der Bauart stammt, bleibt stehen, solange Bauart und Nutzfläche unberührt sind.
    /// </summary>
    [Fact]
    public void Eine_freie_Bauweise_bleibt_ohne_Bauartwahl_stehen()
    {
        double geschrieben = -1;
        GebaeudeKatalogDaten daten = Satz();
        daten.Bauweise = 9876;
        var cut = Aufbauen(daten, speichern: (d, _, _) => { geschrieben = d.Bauweise; return new(true, ""); });

        Ok(cut);

        Assert.Equal(9876, geschrieben);
    }

    [Fact]
    public void Das_Laden_leitet_die_Bauart_aus_der_gespeicherten_Bauweise_ab()
    {
        GebaeudeKatalogDaten geladen = Satz("Hotel C");
        geladen.WohnflaecheGesamt = 100;
        geladen.Bauweise = 10000;      // spez. 100 -> sehr schwer
        geladen.Bauart = 0;            // absichtlich unpassend

        var cut = Aufbauen(modus: GebaeudeKatalogModus.Admin, lies: _ => geladen);

        Klappliste(cut, "Name :").Change("2");     // Hotel C

        Assert.Equal("Hotel C", cut.Instance.Ursprungsname);
        Assert.Equal(2, cut.Instance.Arbeitsstand.Bauart);
        Assert.Equal(10000, cut.Instance.Arbeitsstand.Bauweise);
    }

    // =================================================================================
    // Die Ableitungen des zweiten Reiters - jetzt im OK-Weg
    // =================================================================================

    [Fact]
    public void OK_hebt_eine_Maximaltemperatur_unter_1_auf_24()
    {
        GebaeudeKatalogDaten geschrieben = null!;
        var cut = Aufbauen(speichern: (d, _, _) => { geschrieben = d; return new(true, ""); });
        ReiterWaehlen(cut, REITER2);

        cut.FindAll("input[inputmode=decimal]")[2].Input("0");
        Ok(cut);

        Assert.Equal(24, geschrieben.MaxTemperatur);
    }

    [Fact]
    public void OK_setzt_die_Flags_Wochenende_und_Ferien_aus_den_Absenkungen()
    {
        GebaeudeKatalogDaten geschrieben = null!;
        var cut = Aufbauen(speichern: (d, _, _) => { geschrieben = d; return new(true, ""); });
        ReiterWaehlen(cut, REITER2);

        cut.FindAll("input[inputmode=decimal]")[3].Input("16");   // Wochenendabsenkung
        cut.FindAll("input[inputmode=decimal]")[4].Input("15");   // Soll in Ferien
        Ok(cut);

        Assert.Equal(1, geschrieben.Wochenende);
        Assert.Equal(1, geschrieben.Ferien);
        Assert.Equal(0, geschrieben.WwBedarf);
    }

    [Fact]
    public void OK_hebt_einen_leeren_Winterferienbeginn_auf_366()
    {
        GebaeudeKatalogDaten geschrieben = null!;
        var cut = Aufbauen(speichern: (d, _, _) => { geschrieben = d; return new(true, ""); });

        Ok(cut);

        Assert.Equal(366, geschrieben.Ferienbeginn[0]);
    }

    // =================================================================================
    // Brauchwasser und Tastatur
    // =================================================================================

    [Fact]
    public void Ohne_Delegat_gibt_es_keinen_Brauchwasserknopf()
    {
        var cut = Aufbauen();
        ReiterWaehlen(cut, REITER2);

        Assert.DoesNotContain("Brauchwasser...", cut.Markup);
    }

    [Fact]
    public void Mit_Delegat_oeffnet_der_Brauchwasserknopf_die_Ueberlagerung()
    {
        bool gerufen = false;
        var cut = Aufbauen(brauchwasser: () => { gerufen = true; return new Dictionary<string, object>(); });
        ReiterWaehlen(cut, REITER2);

        Knopf(cut, "Brauchwasser...").Click();

        Assert.True(gerufen);
        Assert.True(cut.Instance.BrauchwasserOffen);
    }

    [Fact]
    public void Esc_schliesst_ohne_zu_schreiben()
    {
        bool? ergebnis = null;
        int laeufe = 0;
        var cut = Aufbauen(speichern: (_, _, _) => { laeufe++; return new(true, ""); },
                           geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.False(ergebnis);
        Assert.Equal(0, laeufe);
    }

    /// <summary>Das Kreuz im Dialogkopf wirkt wie Esc: schließt ohne zu schreiben.</summary>
    [Fact]
    public void Kreuz_schliesst_wie_Esc()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog-zu").Click();

        Assert.False(ergebnis);
    }

    [Fact]
    public void Ueberlagerungskreuz_schliesst_den_Brauchwasserdialog()
    {
        var cut = Aufbauen(brauchwasser: () => new Dictionary<string, object>());
        ReiterWaehlen(cut, REITER2);
        Knopf(cut, "Brauchwasser...").Click();
        Assert.True(cut.Instance.BrauchwasserOffen);

        cut.Find(".epos-ueberlagerung-zu").Click();

        Assert.False(cut.Instance.BrauchwasserOffen);
    }

    [Fact]
    public void Ohne_Titel_zeigt_der_Kopf_weder_Titel_noch_Kreuz()
    {
        var cut = Aufbauen(titelAnzeigen: false);

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
        Assert.NotEmpty(cut.FindAll(".epos-dialog-kopf"));
    }

    [Fact]
    public void Die_Ueberlagerung_Brauchwasser_zeigt_nur_ein_Kreuz()
    {
        var cut = Aufbauen(brauchwasser: () => new Dictionary<string, object>());
        ReiterWaehlen(cut, REITER2);
        Knopf(cut, "Brauchwasser...").Click();

        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
    }

    /// <summary>Die Parameterblöcke des ersten Reiters stehen im Formularraster.</summary>
    [Fact]
    public void Die_Bloecke_des_Gebaeudekatalogs_stehen_im_Formularraster()
    {
        var cut = Aufbauen();

        Assert.True(cut.FindAll(".epos-formularraster").Count >= 4,
                    "der erste Reiter traegt weniger als vier Raster");
        Assert.NotEmpty(cut.FindAll(
            ".epos-formularraster .epos-feld--kurz .epos-feld-zeile .epos-einheit"));
    }

    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F3)
    // =====================================================================

    /// <summary>
    /// Die Maske meldet sich an und deckt BEIDE Reiterblätter über den Arbeitsstand ab.
    /// </summary>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_setzt_beide_Blaetter()
    {
        var cut = Aufbauen();

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.GEBAEUDE_KATALOG));

        WindowsFormsApplication1.KiFeldzugang flaeche =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.GEBAEUDE_KATALOG, "wohnflaeche");
        Assert.NotNull(flaeche);
        Assert.Equal(150.0, flaeche.Lesen());

        flaeche.Setzen(180.0);
        cut.Render();
        Assert.Equal(180.0, cut.Instance.Arbeitsstand.WohnflaecheGesamt);

        WindowsFormsApplication1.KiFeldzugang soll =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.GEBAEUDE_KATALOG, "soll_tag");
        Assert.NotNull(soll);
        Assert.Equal(20.0, soll.Lesen());

        soll.Setzen(21.5);
        cut.Render();
        Assert.Equal(21.5, cut.Instance.Arbeitsstand.SollTag);
    }

    /// <summary>Der Gebäudetyp ist ein WAHLFELD (KI-D-Q6).</summary>
    [Fact]
    public void Der_Assistent_waehlt_den_Gebaeudetyp_ueber_seinen_Text()
    {
        var cut = Aufbauen();

        WindowsFormsApplication1.KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.GEBAEUDE_KATALOG, "gebaeudetyp");
        Assert.NotNull(zugang);

        KiFeldumsetzung umsetzung = KiFeldwandler.Wandle(zugang, TYPEN[1]);
        Assert.True(umsetzung.Ok, umsetzung.Grund);
        zugang.Setzen(umsetzung.Wert);
        cut.Render();

        Assert.Equal(TYPEN[1], cut.Instance.Arbeitsstand.Typ);
    }
}
