using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Allgemein;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Wirtschaftlichkeits-Parameterdialog (iU9-W2.5). Soll ist die Handkarte
/// der gelöschten Maske <c>Form_WirtschaftlichkeitParameter</c> (K4, ohne
/// Designer):
///
/// <list type="bullet">
/// <item>Allgemein (immer): Zins, T, Preissteigerung Energie, Betrieb und — seit
///       W5‑B‑12 — Investition/Ersatz p_I (5, davon 4 Dezimalfelder)</item>
/// <item>Szenarien (immer, ETAPPE W5‑B‑9 vom 09.09.2026, seit W5‑B‑12 mit p_I,
///       seit ETAPPE E9b mit Betrachtungszeitraum und Mengenänderung): neun Größen ×
///       Best und Worst = 18 Felder (16 Dezimalfelder, 2 Ganzzahlfelder für den
///       Zeitraum), dazu der Knopf „Vorgaben“. Die Erwartet-Spalte ist Anzeige. Die
///       Felder stehen im Feldbestand ZWISCHEN Allgemein und Strom — daher die
///       verschobenen Indizes der Zahlenfelder unten.</item>
/// <item>Strom (immer): Einspeisung PV (1) und — ETAPPE E9b — ihr ±-Knopf, der das
///       Best/Worst-Paar in einer Überlagerung pflegt. AUFTRAG #325 (17.09.2026): Der
///       KWK-Satz steht im Dialog „BHKW-Wirtschaftlichkeit", der Freitextblock
///       „Bewertung nach DIN EN 17463" auf der Seite „Wirtschaftlichkeit“ — beide
///       sind hier ausgezogen. Der PV-Satz BLEIBT: Zwei Rechenwege brauchen ihn
///       auch ohne PV-Anlage im Projekt (v_bhkw-Rückfall und Arbitrage-Verkauf in
///       <c>StromPreisCtrl</c>), und der PV-Vergütungsdialog wäre dann nicht
///       erreichbar.</item>
/// <item>BHKW (nur mit BHKW): Verweis + Sprungknopf</item>
/// <item>Brennstoff (nur mit Brennstoff-Erzeuger): CO₂ + Katalogknopf + Park +
///       Referenzkessel + Bilanzjahr, Methode, Biomasse, Nachweis (7)</item>
/// </list>
/// </summary>
public class WirtschaftlichkeitParameterDialogTests : EposBunitContext
{
    public WirtschaftlichkeitParameterDialogTests()
    {
        // Seit iU9-W14c.3 steht der Gesetzeskatalog als Ueberlagerung in diesem
        // Dialog, und der bringt ein Raster (QuickGrid) mit - das laedt sein
        // JS-Modul beim Zeichnen.
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // ETAPPE W5-B-9 (09.09.2026): Der Szenarioblock steht ZWISCHEN Allgemein und
    // Strom und bringt seine Zahlenfelder mit. Die Indizes der uebrigen Felder
    // haben sich dadurch verschoben - sie stehen hier als Konstanten, damit der
    // naechste Umbau nur eine Zeile trifft statt sieben.
    // ETAPPE W5-B-12 (09.09.2026): p_I bringt ein viertes Dezimalfeld nach
    // "Allgemein" und eine siebte Zeile in die Szenariotabelle - genau diese zwei
    // Zahlen aendern sich hier, der Rest rechnet sich daraus.
    // AUFTRAG #325 (17.09.2026): Der KWK-Satz ist ausgezogen - die Stromgruppe
    // fuehrt nur noch EIN Zahlenfeld, und die Indizes ruecken um eins nach.
    // ETAPPE E9b (24.09.2026): Die Tafel traegt neun Zeilen - der Betrachtungszeitraum
    // als Ganzzahlfeld (inputmode numeric), die Mengenaenderung als Zahlenfeld. Alle
    // Felder der Tafel sind damit 18, ihre Dezimalfelder 16; die Indizes der
    // Dezimalfelder dahinter ruecken um zwei.
    private const int ALLGEMEIN_FELDER = 4;
    private const int SZENARIO_FELDER = 18;
    private const int SZENARIO_DEZIMAL = 16;
    // ETAPPE E15 (V-G7): Die Gruppe „Risiko" steht zwischen den Szenarien und Strom und
    // bringt drei Dezimalfelder (Zinszuschlag, R_loss, p_loss) und eine Klappliste (Art).
    private const int RISIKO_DEZIMAL = 3;
    private const int RISIKO_ZUSCHLAG = ALLGEMEIN_FELDER + SZENARIO_DEZIMAL;
    private const int RISIKO_VERLUST = RISIKO_ZUSCHLAG + 1;
    private const int RISIKO_P = RISIKO_ZUSCHLAG + 2;
    private const int EINSPEISUNG_PV = ALLGEMEIN_FELDER + SZENARIO_DEZIMAL + RISIKO_DEZIMAL;
    private const int CO2 = EINSPEISUNG_PV + 1;
    private const int FELDER_OHNE_ERZEUGER = ALLGEMEIN_FELDER + SZENARIO_DEZIMAL + RISIKO_DEZIMAL + 1;

    /// <summary>ETAPPE E9b: die Zeilen 8 und 9 der Tafel (Index ab 0).</summary>
    private const int ZEILE_ZEITRAUM = 7;
    private const int ZEILE_MENGE = 8;

    private static WirtschaftlichkeitParameter Satz() => new WirtschaftlichkeitParameter
    {
        IdStamm = 1030,
        Zinssatz = 3.5,
        Betrachtungszeitraum = 20,
        PreissteigerungEnergie = 2.5,
        PreissteigerungBetrieb = 1.5,
        Einspeiseverguetung = 0.0820,
        EinspeiseverguetungKWK = null,
        CO2Preis = 0,
        IdKraftwerkspark = 0,
        BilanzJahr = 0,
        EmissionsMethode = DbWerte.EMISSIONSMETHODE_KATALOG,
        BiomasseKonvention = DbWerte.BIOMASSE_KONVENTION_NULL,
        NachhaltigkeitsnachweisBiomasse = true
        // BHKW-Angaben: der Dialog zeigt sie nicht mehr (Auszug B5/BW9) - und seit
        // Schemaschritt 90 führt der Parametersatz Satz und Kontingent gar nicht
        // mehr; beide gehören der Anlage.
    };

    private IRenderedComponent<WirtschaftlichkeitParameterDialog> Aufbauen(
        WirtschaftlichkeitParameter satz,
        bool bhkw = false, bool brennstoff = false,
        Func<bool>? speichern = null,
        Action<WirtParameterErgebnis>? geschlossen = null,
        Func<IReadOnlyDictionary<string, object>>? gesetzeGaben = null,
        bool titelAnzeigen = true,
        IReadOnlyList<string>? einspeisungHinweise = null)
    {
        return Render<WirtschaftlichkeitParameterDialog>(p => p
            .Add(x => x.TitelAnzeigen, titelAnzeigen)
            .Add(x => x.Parameter, satz)
            .Add(x => x.HatBhkw, bhkw)
            .Add(x => x.HatBrennstoff, brennstoff)
            .Add(x => x.Kraftwerksparks, new[] { (0, "(keine Emissionsbilanz)"), (3, "Netzmix 2030") })
            .Add(x => x.ReferenzkesselZeile, "Referenzkessel (aus Projekt): Kessel A — η 92 %, Erdgas")
            .Add(x => x.Co2PrognoseAb, 2028)
            .Add(x => x.GesetzeGaben, gesetzeGaben)
            .Add(x => x.EinspeisungSzenarioHinweise, einspeisungHinweise ?? Array.Empty<string>())
            .Add(x => x.Speichern, speichern ?? (() => true))
            .Add(x => x.Geschlossen, geschlossen ?? (_ => { })));
    }

    /// <summary>
    /// Ein Titel, eine Stelle (W11b‑B‑9): Zeigt der Wirt schon einen — die
    /// Überlagerung der Wirtschaftlichkeitsseite tut es —, bleibt der eigene Kopf
    /// weg; der Hilfeknopf bleibt.
    /// </summary>
    [Fact]
    public void Ohne_TitelAnzeigen_bleibt_der_eigene_Kopf_weg_und_der_Hilfeknopf_steht()
    {
        var cut = Aufbauen(Satz(), titelAnzeigen: false);

        Assert.Empty(cut.FindAll("h1.epos-dialog-titel"));
        Assert.Contains("epos-dialog-kopf--ohnetitel", cut.Find("div.epos-dialog-kopf").ClassName);
        Assert.NotNull(cut.Find(".epos-infoknopf"));
    }

    /// <summary>Im eigenen Fenster (Vorgabe) steht der Kopf wie bisher.</summary>
    [Fact]
    public void Mit_TitelAnzeigen_steht_der_eigene_Kopf()
    {
        var cut = Aufbauen(Satz());

        Assert.Single(cut.FindAll("h1.epos-dialog-titel"));
    }

    // =====================================================================
    // Feldbestand und Sichtbarkeit
    // =====================================================================

    [Fact]
    public void Ohne_Erzeuger_stehen_nur_Allgemein_und_Strom()
    {
        var cut = Aufbauen(Satz());

        Assert.Equal(new[] { "Allgemein",
                             "Szenarien — Best und Worst gegen den Erwartungsfall",
                             "Risiko (DIN EN 17463, 6.5)",
                             "Strom — Einspeisung und Bezug" },
                     cut.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent).ToArray());

        // Zins, PreisE, PreisB, PreisI (4) + Szenarien 8×2 (16) + Risiko (3, E15) + Einspeisung PV (1)
        Assert.Equal(FELDER_OHNE_ERZEUGER, cut.FindAll("input[inputmode=decimal]").Count);
        // T + ETAPPE E9b: der Betrachtungszeitraum je Szenario (Best, Worst)
        Assert.Equal(3, cut.FindAll("input[inputmode=numeric]").Count);
        // SP-E-2: Der Anzeigehaken „Aufschlaege beruecksichtigen" ist entfallen -
        // die Preisanteile zerlegen den Arbeitspreis, statt auf ihn zu kommen.
        Assert.Empty(cut.FindAll("input[type=checkbox]"));
        // ETAPPE E15: die einzige Klappliste ist die Art des Risikos.
        Assert.Single(cut.FindAll("select"));
        // AUFTRAG #325: kein Freitextfeld mehr - der Bewertungsblock steht auf der Seite.
        Assert.Empty(cut.FindAll("textarea"));
    }

    [Fact]
    public void Mit_BHKW_erscheint_der_Verweis_statt_der_beiden_alten_Gruppen()
    {
        // Etappe B5/BW9: KWKG- und Steuerangaben stehen im eigenen Dialog.
        var cut = Aufbauen(Satz(), bhkw: true);
        var titel = cut.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent).ToList();

        Assert.Contains("BHKW — KWKG, Energie- und Stromsteuer", titel);
        Assert.Contains(cut.FindAll(".epos-herleitung-text"),
                        e => e.TextContent.Contains("BHKW-Wirtschaftlichkeit"));
        // Die Gruppe ist reiner Verweis - sie fuehrt selbst nicht dorthin.
        Assert.Empty(cut.FindAll("button.epos-sprung"));
        // Kein einziges Eingabefeld mehr aus den ausgezogenen Gruppen.
        Assert.Equal(FELDER_OHNE_ERZEUGER, cut.FindAll("input[inputmode=decimal]").Count);
    }

    [Fact]
    public void Mit_Brennstoff_erscheint_die_Emissionsgruppe_vollstaendig()
    {
        var cut = Aufbauen(Satz(), brennstoff: true,
                           gesetzeGaben: () => new Dictionary<string, object>());
        var titel = cut.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent).ToList();

        Assert.Contains("Brennstoff — BEHG und Emissionsbilanz (BHKW/Kessel)", titel);
        Assert.Equal(FELDER_OHNE_ERZEUGER + 1,
                     cut.FindAll("input[inputmode=decimal]").Count);       // + CO2
        Assert.Equal(4, cut.FindAll("input[inputmode=numeric]").Count);   // + Bilanzjahr
        Assert.Equal(4, cut.FindAll("select").Count);                     // Risiko (E15), Park, Methode, Biomasse
        Assert.Single(cut.FindAll("input[type=checkbox]"));               // Nachweis
        Assert.Single(cut.FindAll("button.epos-sprung"));                 // Katalogknopf
    }

    [Fact]
    public void Die_Referenzkesselzeile_wird_nur_angezeigt()
    {
        var cut = Aufbauen(Satz(), brennstoff: true);

        Assert.Contains(cut.FindAll(".epos-herleitung-text"),
                        e => e.TextContent.StartsWith("Referenzkessel (aus Projekt): Kessel A"));
    }

    [Fact]
    public void Die_Vorbelegung_kommt_aus_dem_geladenen_Satz()
    {
        var cut = Aufbauen(Satz());
        var zahlen = cut.FindAll("input[inputmode=decimal]");

        Assert.Equal("3,50", zahlen[0].GetAttribute("value"));
        Assert.Equal("0,0820", zahlen[EINSPEISUNG_PV].GetAttribute("value"));
        Assert.Equal("20", cut.Find("input[inputmode=numeric]").GetAttribute("value"));
    }

    // =====================================================================
    // Szenarien (ETAPPE W5-B-9, Anwenderentscheid 09.09.2026)
    // =====================================================================

    /// <summary>
    /// Die Tabelle trägt drei Wertspalten über neun Zeilen (seit W5‑B‑12 mit p_I,
    /// unmittelbar hinter den beiden anderen Preissteigerungen; seit ETAPPE E9b mit
    /// Betrachtungszeitraum und Mengenänderung als Zeilen 8 und 9). Die Erwartet-Spalte
    /// ist ANZEIGE — sie wiederholt die Projektparameter und trägt kein Eingabefeld
    /// („Kein Delegat ist kein Knopf“).
    /// </summary>
    [Fact]
    public void Die_Szenariotabelle_zeigt_drei_Spalten_und_neun_Groessen()
    {
        var cut = Aufbauen(Satz());
        var tabelle = cut.FindAll("table.epos-matrix")[0];

        Assert.Equal(new[] { "Größe", "Erwartet", "Best", "Worst" },
                     tabelle.QuerySelectorAll("thead th").Select(e => e.TextContent).ToArray());

        var zeilen = tabelle.QuerySelectorAll("tbody tr");
        Assert.Equal(9, zeilen.Length);
        Assert.Equal(new[] { "Kalkulationszins", "Preissteigerung Energie",
                             "Preissteigerung Betrieb", "Preissteigerung Investition",
                             "Investition", "Erträge", "Nutzungsdauer",
                             "Betrachtungszeitraum", "Mengenänderung" },
                     zeilen.Select(z => z.QuerySelector(".epos-matrix-titel")!.TextContent).ToArray());

        // Je Zeile genau ZWEI Eingabefelder - Best und Worst.
        foreach (var z in zeilen) Assert.Equal(2, z.QuerySelectorAll("input").Length);
        Assert.Equal(SZENARIO_FELDER, tabelle.QuerySelectorAll("input").Length);
        // E9b: Der Zeitraum ist eine ganze Zahl von Jahren, die Mengenänderung ein Prozentsatz.
        Assert.Equal(2, zeilen[ZEILE_ZEITRAUM].QuerySelectorAll("input[inputmode=numeric]").Length);
        Assert.Equal(2, zeilen[ZEILE_MENGE].QuerySelectorAll("input[inputmode=decimal]").Length);
    }

    /// <summary>
    /// ETAPPE E9b (E9a‑Q5): Die Zeilen 8 und 9 haben KEINE Vorgabe — leer heißt „wie
    /// Erwartet". Die Felder zeigen deshalb den GEPFLEGTEN Wert und bleiben ohne Pflege
    /// leer; der Platzhalter nennt den Wert, mit dem dann gerechnet wird. Die
    /// Erwartet-Spalte trägt T und 0 %. Wer tippt, pflegt genau dieses Szenario.
    /// </summary>
    [Fact]
    public void Die_Zeilen_Zeitraum_und_Menge_bleiben_ohne_Pflege_leer_und_schreiben_ihr_Szenario()
    {
        WirtschaftlichkeitParameter satz = Satz();               // T = 20 a
        var cut = Aufbauen(satz);
        var zeilen = cut.FindAll("table.epos-matrix tbody tr");

        Assert.Equal("20 a", Zelle(zeilen[ZEILE_ZEITRAUM], 0).TextContent.Trim());
        Assert.Equal("0 %", Zelle(zeilen[ZEILE_MENGE], 0).TextContent.Trim());

        Assert.Equal("", Feld(zeilen[ZEILE_ZEITRAUM], 0).GetAttribute("value") ?? "");
        Assert.Equal("", Feld(zeilen[ZEILE_MENGE], 1).GetAttribute("value") ?? "");
        Assert.Equal("20", Feld(zeilen[ZEILE_ZEITRAUM], 0).GetAttribute("placeholder"));
        Assert.Equal("20", Feld(zeilen[ZEILE_ZEITRAUM], 1).GetAttribute("placeholder"));
        Assert.Equal("0", Feld(zeilen[ZEILE_MENGE], 0).GetAttribute("placeholder"));

        Feld(zeilen[ZEILE_ZEITRAUM], 0).Input("25");
        Feld(cut.FindAll("table.epos-matrix tbody tr")[ZEILE_MENGE], 1).Input("-10");

        Assert.Equal(25, satz.SatzBest!.Zeitraum);
        Assert.Null(satz.SatzWorst!.Zeitraum);
        Assert.Equal(-10.0, satz.SatzWorst.Menge);
        Assert.Null(satz.SatzBest.Menge);
        Assert.True(satz.SatzBest.ZeitraumGepflegt(satz.Betrachtungszeitraum));
        Assert.True(satz.SatzWorst.MengeGepflegt);
    }

    /// <summary>
    /// Die Felder zeigen den WIRKSAMEN Wert, nicht den gepflegten: Ohne jede Pflege sind
    /// das die Vorgaben (Zins ∓ 1 %-Punkt, Investition ∓ 10 %, Erträge ± 10 %,
    /// Nutzungsdauer ± 2 a) — ein leeres Feld liesse den Anwender im Unklaren, womit
    /// gerechnet wird. Die Erwartet-Spalte trägt den Projektwert.
    /// </summary>
    [Fact]
    public void Die_Szenariofelder_zeigen_die_wirksamen_Vorgaben()
    {
        var cut = Aufbauen(Satz());               // i = 3,5 %, p_E = 2,5 %, p_B = 1,5 %
        var zeilen = cut.FindAll("table.epos-matrix tbody tr");

        Assert.Equal("3,50", Zelle(zeilen[0], 0).TextContent.Trim().Split(' ')[0]);
        Assert.Equal("2,50", Feld(zeilen[0], 0).GetAttribute("value"));   // Best  = 3,5 - 1
        Assert.Equal("4,50", Feld(zeilen[0], 1).GetAttribute("value"));   // Worst = 3,5 + 1
        Assert.Equal("1,50", Feld(zeilen[1], 0).GetAttribute("value"));   // p_E Best
        // W5-B-12: p_I ohne eigene Pflege ist p_B = 1,5 -> Best 0,50 / Worst 2,50,
        // also genau das wirksame p_B des jeweiligen Szenarios (Zeile 2).
        Assert.Equal("0,50", Feld(zeilen[3], 0).GetAttribute("value"));   // p_I Best
        Assert.Equal("2,50", Feld(zeilen[3], 1).GetAttribute("value"));   // p_I Worst
        Assert.Equal("-10,0", Feld(zeilen[4], 0).GetAttribute("value"));  // Investition Best
        Assert.Equal("10,0", Feld(zeilen[4], 1).GetAttribute("value"));   // Investition Worst
        Assert.Equal("10,0", Feld(zeilen[5], 0).GetAttribute("value"));   // Erträge Best
        Assert.Equal("2,0", Feld(zeilen[6], 0).GetAttribute("value"));    // Nutzungsdauer Best
    }

    /// <summary>
    /// Wer tippt, pflegt — und die Herleitungszeile sagt es. „Vorgaben“ setzt alle
    /// achtzehn Felder der Tafel wieder auf <c>null</c> (seit W5‑B‑12 mit p_I, seit
    /// ETAPPE E9b mit Betrachtungszeitraum und Mengenänderung); das ist NICHT dasselbe
    /// wie „auf die heutigen Vorgabezahlen setzen“, denn ein leeres Feld zieht bei einer
    /// geänderten Projektangabe mit.
    /// </summary>
    [Fact]
    public void Vorgaben_setzt_die_achtzehn_Felder_zurueck()
    {
        WirtschaftlichkeitParameter satz = Satz();
        var cut = Aufbauen(satz);

        Assert.True(satz.SatzBest.NurVorgaben);
        Assert.Contains("Best: Vorgaben", cut.Instance.SzenarioZeileBest);

        Feld(cut.FindAll("table.epos-matrix tbody tr")[0], 0).Input("1,25");
        Assert.Equal(1.25, satz.SatzBest.Zinssatz);
        Assert.False(satz.SatzBest.NurVorgaben);
        Assert.Contains("Best: gepflegte Werte", cut.Instance.SzenarioZeileBest);

        // W5-B-12: auch das siebte Feld zaehlt - NurVorgaben kennt es.
        Feld(cut.FindAll("table.epos-matrix tbody tr")[3], 1).Input("4,00");
        Assert.Equal(4.0, satz.SatzWorst.PreissteigerungInvestition);
        Assert.False(satz.SatzWorst.NurVorgaben);

        // E9b: die Zeilen 8 und 9.
        Feld(cut.FindAll("table.epos-matrix tbody tr")[ZEILE_ZEITRAUM], 1).Input("30");
        Feld(cut.FindAll("table.epos-matrix tbody tr")[ZEILE_MENGE], 0).Input("5");
        Assert.Equal(30, satz.SatzWorst.Zeitraum);
        Assert.Equal(5.0, satz.SatzBest.Menge);

        cut.FindAll("button.epos-knopf").First(b => b.TextContent.Trim() == "Vorgaben").Click();

        Assert.Null(satz.SatzBest.Zinssatz);
        Assert.Null(satz.SatzWorst.PreissteigerungInvestition);
        Assert.Null(satz.SatzWorst.Zeitraum);
        Assert.Null(satz.SatzBest.Menge);
        Assert.True(satz.SatzBest.NurVorgaben);
        Assert.True(satz.SatzWorst.NurVorgaben);

        // Die Felder der Zeilen 8 und 9 sind wieder leer.
        var zeilen = cut.FindAll("table.epos-matrix tbody tr");
        Assert.Equal("", Feld(zeilen[ZEILE_ZEITRAUM], 1).GetAttribute("value") ?? "");
        Assert.Equal("", Feld(zeilen[ZEILE_MENGE], 0).GetAttribute("value") ?? "");
    }

    /// <summary>
    /// ETAPPE E9b: „Vorgaben“ leert die TAFEL — die Einspeisevergütungen je Szenario
    /// stehen nicht in ihr, ihr Paar pflegt der ±-Knopf an ihrem Feld (PV hier, KWK im
    /// Dialog „BHKW-Wirtschaftlichkeit"). Ein Knopf der Tafel, der sie mitleerte, löschte
    /// eine Pflege, die an dieser Stelle niemand sieht.
    /// </summary>
    [Fact]
    public void Vorgaben_laesst_die_Einspeiseverguetungen_je_Szenario_stehen()
    {
        WirtschaftlichkeitParameter satz = Satz();
        satz.SatzBest = SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.BEST);
        satz.SatzWorst = SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.WORST);
        satz.SatzBest.Einspeiseverguetung = 0.10;
        satz.SatzWorst.EinspeiseverguetungKwk = 0.06;
        satz.SatzBest.Zinssatz = 2.0;
        var cut = Aufbauen(satz);

        cut.FindAll("button.epos-knopf").First(b => b.TextContent.Trim() == "Vorgaben").Click();

        Assert.Null(satz.SatzBest.Zinssatz);
        Assert.Equal(0.10, satz.SatzBest.Einspeiseverguetung);
        Assert.Equal(0.06, satz.SatzWorst.EinspeiseverguetungKwk);
    }

    /// <summary>Die Herleitungszeilen nennen beide Sätze mit ihren wirksamen Zahlen.</summary>
    [Fact]
    public void Die_Herleitungszeilen_nennen_beide_Saetze()
    {
        var cut = Aufbauen(Satz());

        Assert.Contains("i = 2,5 %", cut.Instance.SzenarioZeileBest);
        Assert.Contains("Investition -10 %", cut.Instance.SzenarioZeileBest);
        Assert.Contains("i = 4,5 %", cut.Instance.SzenarioZeileWorst);
        Assert.Contains("Nutzungsdauer -2 a", cut.Instance.SzenarioZeileWorst);
        // W5-B-12: p_I steht mit in der Annahmenzeile - p_B = 1,5 ∓ 1.
        Assert.Contains("p_I = 0,5 %/a", cut.Instance.SzenarioZeileBest);
        Assert.Contains("p_I = 2,5 %/a", cut.Instance.SzenarioZeileWorst);
    }

    /// <summary>
    /// ETAPPE E9b: Die Herleitungszeilen nennen Betrachtungszeitraum, Mengenänderung und
    /// Einspeisevergütung NUR, wenn dieses Szenario sie gepflegt hat — „wie Erwartet"
    /// steht schon in der Erwartet-Spalte daneben. Ein Zeitraum gleich dem
    /// Erwartungswert ist keine Pflege (dieselbe Regel wie im Kern).
    /// </summary>
    [Fact]
    public void Die_Herleitungszeilen_nennen_die_neuen_Groessen_nur_wenn_gepflegt()
    {
        WirtschaftlichkeitParameter satz = Satz();               // T = 20 a
        var cut = Aufbauen(satz);

        Assert.DoesNotContain("T = ", cut.Instance.SzenarioZeileBest);
        Assert.DoesNotContain("Mengen", cut.Instance.SzenarioZeileBest);
        Assert.DoesNotContain("Einspeisevergütung", cut.Instance.SzenarioZeileBest);

        Feld(cut.FindAll("table.epos-matrix tbody tr")[ZEILE_ZEITRAUM], 0).Input("25");
        Feld(cut.FindAll("table.epos-matrix tbody tr")[ZEILE_MENGE], 0).Input("5");

        Assert.Contains("T = 25 a", cut.Instance.SzenarioZeileBest);
        Assert.Contains("Mengen +5 %", cut.Instance.SzenarioZeileBest);
        Assert.DoesNotContain("T = ", cut.Instance.SzenarioZeileWorst);

        Feld(cut.FindAll("table.epos-matrix tbody tr")[ZEILE_ZEITRAUM], 1).Input("20");
        Assert.Equal(20, satz.SatzWorst!.Zeitraum);
        Assert.DoesNotContain("T = ", cut.Instance.SzenarioZeileWorst);
    }

    // =====================================================================
    // p_I und die nicht monetären Wirkungen (ETAPPE W5-B-12, 09.09.2026)
    // =====================================================================

    /// <summary>
    /// Ein LEERES p_I-Feld ist keine 0, sondern die Aussage „wie Betrieb“ — und die
    /// Herleitungszeile sagt beides: den wirksamen Wert und seine Herkunft. Wer eine Zahl
    /// einträgt, pflegt; wer sie wieder löscht, ist zurück bei p_B.
    /// </summary>
    [Fact]
    public void Ein_leeres_p_I_rechnet_wie_die_Preissteigerung_Betrieb()
    {
        WirtschaftlichkeitParameter satz = Satz();          // p_B = 1,5 %/a
        var cut = Aufbauen(satz);

        Assert.Null(satz.PreissteigerungInvestition);
        Assert.Equal(1.5, satz.PreisInvestWirksam, 9);
        Assert.Contains("1,50 %/a (wie Betrieb)", cut.Instance.PreisInvestZeile);

        // Das vierte Dezimalfeld des Blocks "Allgemein" ist p_I.
        cut.FindAll("input[inputmode=decimal]")[ALLGEMEIN_FELDER - 1].Input("3,00");
        Assert.Equal(3.0, satz.PreissteigerungInvestition);
        Assert.Equal(3.0, satz.PreisInvestWirksam, 9);
        Assert.Contains("3,00 %/a (gepflegt)", cut.Instance.PreisInvestZeile);

        cut.FindAll("input[inputmode=decimal]")[ALLGEMEIN_FELDER - 1].Input("");
        Assert.Null(satz.PreissteigerungInvestition);
        Assert.Contains("1,50 %/a (wie Betrieb)", cut.Instance.PreisInvestZeile);
    }

    /// <summary>
    /// Ist p_I gepflegt, spannt sich die Szenario-Vorgabe um DIESEN Wert und nicht mehr
    /// um p_B — dieselbe ∓1-%-Punkt-Regel wie bei p_E und p_B, angewandt auf den
    /// Erwartungswert der eigenen Größe.
    /// </summary>
    [Fact]
    public void Ein_gepflegtes_p_I_verschiebt_die_Szenariovorgaben()
    {
        WirtschaftlichkeitParameter satz = Satz();          // p_B = 1,5 %/a
        satz.PreissteigerungInvestition = 4.0;
        var cut = Aufbauen(satz);

        var zeile = cut.FindAll("table.epos-matrix tbody tr")[3];
        Assert.Equal("4,00", Zelle(zeile, 0).TextContent.Trim().Split(' ')[0]);
        Assert.Equal("3,00", Feld(zeile, 0).GetAttribute("value"));   // Best  = 4 - 1
        Assert.Equal("5,00", Feld(zeile, 1).GetAttribute("value"));   // Worst = 4 + 1
    }

    /// <summary>
    /// AUFTRAG #325 (Anwenderentscheid 17.09.2026): <b>Der Dialog führt die drei
    /// ausgezogenen Abschnitte nicht mehr.</b> „Bewertung nach DIN EN 17463" steht auf
    /// der Seite „Wirtschaftlichkeit", der KWK-Satz im Dialog
    /// „BHKW-Wirtschaftlichkeit"; der PV-Satz bleibt (Haltepunkt des Auftrags).
    ///
    /// <para><b>Was er nicht zeigt, schreibt er auch nicht:</b> Ein geladener Freitext
    /// geht wertgleich durch den Dialog hindurch — dieselbe Regel, mit der schon die
    /// ausgezogenen BHKW-Gruppen behandelt werden.</para>
    /// </summary>
    [Fact]
    public void Die_drei_ausgezogenen_Abschnitte_stehen_nicht_mehr_im_Dialog()
    {
        WirtschaftlichkeitParameter satz = Satz();
        satz.NichtMonetaer = "Versorgungssicherheit, Arbeitsschutz";
        satz.EinspeiseverguetungKWK = 0.09;
        var cut = Aufbauen(satz, speichern: () => true);

        // Kein Freitextfeld, keine Bewertungsgruppe.
        Assert.Empty(cut.FindAll("textarea"));
        Assert.DoesNotContain("Bewertung nach DIN EN 17463", cut.Markup);
        // Kein KWK-Satz - aber der PV-Satz steht da.
        Assert.DoesNotContain("KWK-Strom", cut.Markup);
        Assert.Contains("Einspeisevergütung PV", cut.Markup);

        cut.Find(".epos-knopf--primaer").Click();

        // Beides steht unverändert im Satz: Was der Dialog nicht zeigt, schreibt er nicht.
        Assert.Equal("Versorgungssicherheit, Arbeitsschutz", satz.NichtMonetaer);
        Assert.Equal(0.09, satz.EinspeiseverguetungKWK);
    }

    // =====================================================================
    // ETAPPE E9b — der ±-Knopf der Einspeisevergütung PV (E9b‑Q1, Lesart a)
    // =====================================================================

    /// <summary>
    /// Der ±-Knopf steht an der Einspeisevergütung PV und öffnet den
    /// <c>CaseEingabeDialog</c> als SZENARIOPAAR in einer Überlagerung: Titel und Kreuz
    /// trägt die Überlagerung („Ein Titel, eine Stelle"), das Blatt darin nennt den
    /// Erwartet-Wert und hat weder Nutzungsdauer noch Startjahr. OK legt das Paar auf
    /// beide Szenariosätze, und der Knopf trägt danach das Kennzeichen.
    /// </summary>
    [Fact]
    public void Der_Knopf_der_Einspeiseverguetung_pflegt_das_Paar_und_zeigt_das_Kennzeichen()
    {
        WirtschaftlichkeitParameter satz = Satz();               // v_pv = 0,0820 €/kWh
        WirtParameterErgebnis? ergebnis = null;
        var cut = Aufbauen(satz, geschlossen: e => ergebnis = e);

        IElement knopf = cut.Find("button.epos-szenarioknopf");
        Assert.Equal("± Einspeisevergütung PV", knopf.GetAttribute("aria-label"));
        Assert.Equal("Szenariowerte Best/Worst pflegen", knopf.GetAttribute("title"));
        Assert.Empty(cut.FindAll(".epos-szenarioknopf-kennzeichen"));
        Assert.False(cut.Instance.SzenarioOffen);

        knopf.Click();

        Assert.True(cut.Instance.SzenarioOffen);
        IElement ueberlagerung = cut.Find(".epos-ueberlagerung");
        Assert.Equal("Szenariowerte — Einspeisevergütung PV",
                     cut.Find(".epos-ueberlagerung-titel").TextContent.Trim());
        Assert.Empty(ueberlagerung.QuerySelectorAll(".epos-dialog-titel"));
        Assert.Contains(ueberlagerung.QuerySelectorAll(".epos-herleitung-text"),
                        e => e.TextContent == "Erwartet: 0,0820 €/kWh");
        // Keine Warnung: eine Vergütung von 0 wäre keine Datenlücke.
        Assert.Empty(ueberlagerung.QuerySelectorAll(".epos-warnbanner"));

        var felder = ueberlagerung.QuerySelectorAll("input[inputmode=decimal]");
        Assert.Equal(2, felder.Length);                          // nur Best und Worst
        Assert.Empty(ueberlagerung.QuerySelectorAll("input[inputmode=numeric]"));
        felder[0].Input("0,0900");
        cut.Find(".epos-ueberlagerung").QuerySelectorAll("input[inputmode=decimal]")[1].Input("0,0700");
        cut.Find(".epos-ueberlagerung .epos-knopf--primaer").Click();

        Assert.False(cut.Instance.SzenarioOffen);
        Assert.Null(ergebnis);                                   // der Dialog steht noch
        Assert.Equal(0.09, satz.SatzBest!.Einspeiseverguetung);
        Assert.Equal(0.07, satz.SatzWorst!.Einspeiseverguetung);
        Assert.True(cut.Instance.EinspeisungSzenarioGepflegt);

        knopf = cut.Find("button.epos-szenarioknopf");
        Assert.Contains("epos-szenarioknopf--gepflegt", knopf.ClassName);
        Assert.Single(cut.FindAll(".epos-szenarioknopf-kennzeichen"));
        Assert.Equal("± Einspeisevergütung PV (gepflegt)", knopf.GetAttribute("aria-label"));
        Assert.Equal("Szenariowerte gepflegt — Best 0,0900 €/kWh · Worst 0,0700 €/kWh",
                     knopf.GetAttribute("title"));
        // Die Herleitungszeile nennt die gepflegte Vergütung jetzt.
        Assert.Contains("Einspeisevergütung 0,090 €/kWh", cut.Instance.SzenarioZeileBest);
    }

    /// <summary>
    /// Ein leeres Feld heißt „wie Erwartet": OK mit geleerten Feldern schreibt
    /// <c>null</c>, nicht 0 — und der Knopf verliert sein Kennzeichen.
    /// </summary>
    [Fact]
    public void Geleerte_Felder_des_Paars_schreiben_wie_Erwartet()
    {
        WirtschaftlichkeitParameter satz = Satz();
        satz.SatzBest = SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.BEST);
        satz.SatzWorst = SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.WORST);
        satz.SatzBest.Einspeiseverguetung = 0.09;
        var cut = Aufbauen(satz);
        Assert.Single(cut.FindAll(".epos-szenarioknopf-kennzeichen"));

        cut.Find("button.epos-szenarioknopf").Click();
        var felder = cut.Find(".epos-ueberlagerung").QuerySelectorAll("input[inputmode=decimal]");
        Assert.Equal("0,0900", felder[0].GetAttribute("value"));
        Assert.Equal("", felder[1].GetAttribute("value") ?? "");  // nicht gepflegt = leer
        felder[0].Input("");
        cut.Find(".epos-ueberlagerung .epos-knopf--primaer").Click();

        Assert.Null(satz.SatzBest.Einspeiseverguetung);
        Assert.Null(satz.SatzWorst.Einspeiseverguetung);
        Assert.Empty(cut.FindAll(".epos-szenarioknopf-kennzeichen"));
    }

    /// <summary>
    /// E9a‑Q7: Die Kohärenzzeilen der Hülle (Tarif-Rollenmodell, PV-Vergütungsdialog)
    /// erscheinen erst, wenn ein Paar gepflegt ist — dann unter dem Knopf UND im Blatt
    /// der Überlagerung. Sie sperren nichts.
    /// </summary>
    [Fact]
    public void Die_Kohaerenzzeilen_erscheinen_erst_mit_einem_gepflegten_Paar()
    {
        const string ROLLEN = "Szenario-Einspeisevergütung ohne Wirkung: Rollenmodell.";
        WirtschaftlichkeitParameter satz = Satz();
        var cut = Aufbauen(satz, einspeisungHinweise: new[] { ROLLEN });

        Assert.DoesNotContain(cut.FindAll(".epos-kohaerenz-text"), e => e.TextContent == ROLLEN);

        cut.Find("button.epos-szenarioknopf").Click();
        // Im Blatt steht die Zeile schon beim Öffnen — dort wird gepflegt.
        Assert.Contains(cut.Find(".epos-ueberlagerung").QuerySelectorAll(".epos-kohaerenz-text"),
                        e => e.TextContent == ROLLEN);
        cut.Find(".epos-ueberlagerung").QuerySelectorAll("input[inputmode=decimal]")[1].Input("0,0500");
        cut.Find(".epos-ueberlagerung .epos-knopf--primaer").Click();

        Assert.Contains(cut.FindAll(".epos-kohaerenz-text"), e => e.TextContent == ROLLEN);
        Assert.Equal(0.05, satz.SatzWorst!.Einspeiseverguetung);
    }

    /// <summary>
    /// Esc gehört der OBERSTEN Ebene: Steht das Szenariopaar, schließt Esc nur die
    /// Überlagerung — der Parameterdialog bleibt stehen, und das Paar ist unverändert.
    /// Das ✕ der Überlagerung wirkt ebenso.
    /// </summary>
    [Fact]
    public void Esc_und_Kreuz_schliessen_nur_die_Ueberlagerung_des_Paars()
    {
        WirtschaftlichkeitParameter satz = Satz();
        WirtParameterErgebnis? ergebnis = null;
        var cut = Aufbauen(satz, geschlossen: e => ergebnis = e);

        cut.Find("button.epos-szenarioknopf").Click();
        cut.Find(".epos-ueberlagerung").QuerySelectorAll("input[inputmode=decimal]")[0].Input("0,0900");

        // Der Wirt selbst ignoriert Esc, solange die Überlagerung steht …
        cut.Find("div.epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.True(cut.Instance.SzenarioOffen);
        Assert.Null(ergebnis);

        // … und die Überlagerung schließt auf Esc, ohne zu schreiben.
        cut.Find(".epos-ueberlagerung").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(cut.Instance.SzenarioOffen);
        Assert.Null(ergebnis);
        Assert.Null(satz.SatzBest?.Einspeiseverguetung);

        cut.Find("button.epos-szenarioknopf").Click();
        cut.Find(".epos-ueberlagerung-zu").Click();
        Assert.False(cut.Instance.SzenarioOffen);
        Assert.Null(ergebnis);
    }

    /// <summary>
    /// Der Hilfeknopf des Blatts führt auf den Abschnitt „Weitere Werte je Szenario" der
    /// Wirtschaftlichkeitsseite — nicht auf die Kostenposition.
    /// </summary>
    [Fact]
    public void Das_Blatt_des_Paars_traegt_den_Hilfeschluessel_der_Szenariowerte()
    {
        var hilfe = new TestHilfe();
        Services.AddSingleton<IHilfeDienst>(hilfe);
        var cut = Aufbauen(Satz());

        cut.Find("button.epos-szenarioknopf").Click();
        cut.Find(".epos-ueberlagerung .epos-infoknopf").Click();

        Assert.Equal(new[] { "Form_WirtschaftlichkeitSzenariowerte.btn_Help" }, hilfe.Geoeffnet);
    }

    private static IElement Zelle(IElement zeile, int nummer) =>
        zeile.QuerySelectorAll(".epos-matrix-zelle")[nummer];

    private static IElement Feld(IElement zeile, int nummer) =>
        zeile.QuerySelectorAll("input")[nummer];

    // =====================================================================
    // CO₂-Zeile (K6)
    // =====================================================================

    [Fact]
    public void Die_CO2_Zeile_unterscheidet_Pfad_und_konstanten_Preis()
    {
        // 0 heisst seit K6 nicht mehr "aus", sondern "Pfad".
        var cut = Aufbauen(Satz(), brennstoff: true);
        Assert.Contains("2028", cut.Instance.Co2Zeile);

        cut.FindAll("input[inputmode=decimal]")[CO2].Input("95");
        Assert.Contains("95", cut.Instance.Co2Zeile);
    }

    // =====================================================================
    // Nullsemantik
    // =====================================================================

    /// <summary>
    /// AUFTRAG #325, HALTEPUNKT: <b>Der PV-Satz bleibt in DIESEM Dialog.</b> Er wird
    /// auch dort gebraucht, wo das Projekt gar keine PV-Anlage führt — der
    /// PV-Vergütungsdialog hängt aber an genau dieser Anlage. Zwei Rechenwege belegen
    /// es: <c>StromPreisCtrl.VerguetungenBauen</c> nimmt ihn als Rückfall für v_bhkw,
    /// und der Verkaufserlös der Arbitrage ist außerhalb des Spotmarkts eine Kopie
    /// von v_pv.
    /// </summary>
    [Fact]
    public void Der_PV_Einspeisesatz_bleibt_im_Parameterdialog()
    {
        WirtschaftlichkeitParameter satz = Satz();
        var cut = Aufbauen(satz);

        cut.FindAll("input[inputmode=decimal]")[EINSPEISUNG_PV].Input("0,1200");
        Assert.Equal(0.12, satz.Einspeiseverguetung);

        // Der Hinweis sagt, was der Satz sonst noch bewegt (SP-E-5).
        Assert.Contains(cut.FindAll(".epos-herleitung-text"),
                        e => e.TextContent.Contains("v_pv"));
    }

    [Fact]
    public void Ein_geleertes_Feld_behaelt_den_geladenen_Wert()
    {
        WirtschaftlichkeitParameter satz = Satz();
        var cut = Aufbauen(satz);

        cut.FindAll("input[inputmode=decimal]")[0].Input("");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(3.5, satz.Zinssatz);
    }

    // =====================================================================
    // Speichern und Schließen
    // =====================================================================

    [Fact]
    public void Speichern_uebernimmt_die_Eingaben_und_schliesst()
    {
        WirtschaftlichkeitParameter satz = Satz();
        WirtParameterErgebnis? ergebnis = null;
        int gerufen = 0;
        var cut = Aufbauen(satz, speichern: () => { gerufen++; return true; },
                           geschlossen: e => ergebnis = e);

        cut.FindAll("input[inputmode=decimal]")[0].Input("4,25");
        cut.Find("input[inputmode=numeric]").Input("25");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(1, gerufen);
        Assert.True(ergebnis!.Gespeichert);
        Assert.Equal(4.25, satz.Zinssatz);
        Assert.Equal(25, satz.Betrachtungszeitraum);
    }

    [Fact]
    public void Die_ausgezogenen_BHKW_Werte_bleiben_unberuehrt()
    {
        // Sie stehen nicht mehr im Dialog und gehen wertgleich in die Zeile. Seit den
        // Schemaschritten 90 und 91 führt der Parametersatz die KWKG-Rechengrößen gar
        // nicht mehr; geprüft wird deshalb an einer Angabe, die weiterhin projektweit
        // gilt.
        WirtschaftlichkeitParameter satz = Satz();
        satz.KwkgAbschlagNegativ = 25.0;
        var cut = Aufbauen(satz, bhkw: true);

        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(25.0, satz.KwkgAbschlagNegativ);
    }

    /// <summary>
    /// Die BHKW-Gruppe trägt keinen Weg aus dem Dialog heraus: Sie zeigt den
    /// Verweis, und der Dialog bleibt stehen. Der Einstieg in den Sammeldialog
    /// ist der eigene Knopf der Fußleiste der Wirtschaftlichkeitsseite.
    /// </summary>
    [Fact]
    public void Die_BHKW_Gruppe_traegt_keinen_Weg_aus_dem_Dialog()
    {
        WirtParameterErgebnis? ergebnis = null;
        var cut = Aufbauen(Satz(), bhkw: true, geschlossen: e => ergebnis = e);

        // Ohne Gesetzeskatalog ist kein einziger Sprungknopf gezeichnet - und
        // der BHKW-Block trägt überhaupt keinen Knopf mehr.
        Assert.Empty(cut.FindAll("button.epos-sprung"));
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Der_Katalogknopf_oeffnet_die_Ueberlagerung_und_haelt_den_Dialog_offen()
    {
        // iU9-W14c.3: Bis dahin fuehrte der Knopf ueber die Sprungbruecke
        // (Sprungziel.GesetzesparameterCo2) zu einem WinForms-Fenster ueber dem
        // Dialog. Der Katalog ist jetzt selbst Razor und steht als UEBERLAGERUNG im
        // selben Fenster (Risiko R2) - der Dialog bleibt stehen.
        int gerufen = 0;
        WirtParameterErgebnis? ergebnis = null;
        var cut = Aufbauen(Satz(), brennstoff: true, geschlossen: e => ergebnis = e,
                           gesetzeGaben: () => { gerufen++; return new Dictionary<string, object>(); });

        cut.Find("button.epos-sprung").Click();

        Assert.Equal(1, gerufen);
        Assert.True(cut.Instance.KatalogOffen);
        Assert.Null(ergebnis);
    }

    /// <summary>
    /// Esc schliesst die OBERSTE Ebene: steht der Katalog, bleibt der Dialog stehen.
    /// </summary>
    [Fact]
    public void Esc_schliesst_erst_den_Katalog_und_nicht_den_Dialog()
    {
        WirtParameterErgebnis? ergebnis = null;
        var cut = Aufbauen(Satz(), brennstoff: true, geschlossen: e => ergebnis = e,
                           gesetzeGaben: () => new Dictionary<string, object>());

        cut.Find("button.epos-sprung").Click();
        Assert.True(cut.Instance.KatalogOffen);

        cut.Find("div.epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Null(ergebnis);          // der Dialog steht noch
    }

    /// <summary>Das ✕ der Katalog-Ueberlagerung bricht NUR die Ebene ab.</summary>
    [Fact]
    public void Ueberlagerungskreuz_des_Katalogs_schliesst_nur_die_Ebene()
    {
        WirtParameterErgebnis? ergebnis = null;
        var cut = Aufbauen(Satz(), brennstoff: true, geschlossen: e => ergebnis = e,
                           gesetzeGaben: () => new Dictionary<string, object>());

        cut.Find("button.epos-sprung").Click();
        Assert.True(cut.Instance.KatalogOffen);

        cut.Find(".epos-ueberlagerung-zu").Click();

        Assert.False(cut.Instance.KatalogOffen);
        Assert.Null(ergebnis);          // der Dialog selbst steht noch
    }

    /// <summary>
    /// Ein Titel, eine Stelle (W11b-B-9): Die Ueberlagerung trägt Titel und Kreuz; der
    /// Gesetzeskatalog darin zeichnet keinen zweiten Kopf. Der Parametersatz der Hülle
    /// (<c>GesetzeskatalogHuelle.Gaben</c>, derselbe wie für das eigene Fenster) bleibt
    /// unverändert — <c>TitelText=""</c> an der Einbettungsstelle gilt auch dann, wenn
    /// der Satz einen Titel mitbringt (hier absichtlich einen gesetzt).
    /// </summary>
    [Fact]
    public void Der_Titel_des_Gesetzeskatalogs_erscheint_genau_einmal()
    {
        var cut = Aufbauen(Satz(), brennstoff: true,
                           gesetzeGaben: () => new Dictionary<string, object>
                           {
                               ["TitelText"] = "Gesetzliche Parameter"
                           });
        string titel = cut.Find("button.epos-sprung").TextContent.Trim();

        cut.Find("button.epos-sprung").Click();

        var ueberlagerung = cut.Find(".epos-ueberlagerung");
        Assert.Equal(titel, cut.Find(".epos-ueberlagerung-titel").TextContent.Trim());
        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(ueberlagerung.QuerySelectorAll(".epos-dialog-titel"));
        Assert.Empty(ueberlagerung.QuerySelectorAll(".epos-dialog-zu"));
        Assert.Single(ueberlagerung.QuerySelectorAll(".epos-dialog-kopf--ohnetitel"));

        // Der Kopf des Parameterdialogs selbst steht weiterhin genau einmal.
        Assert.Single(cut.FindAll(".epos-dialog-titel"));
        Assert.Single(cut.FindAll(".epos-dialog-zu"));
    }

    [Fact]
    public void Ohne_Gaben_fehlt_der_Katalogknopf()
    {
        var cut = Aufbauen(Satz(), brennstoff: true);

        Assert.Empty(cut.FindAll("button.epos-sprung"));
    }

    [Fact]
    public void Ein_Speicherfehler_meldet_sich_und_haelt_den_Dialog_offen()
    {
        WirtParameterErgebnis? ergebnis = null;
        var cut = Aufbauen(Satz(), speichern: () => false, geschlossen: e => ergebnis = e);

        cut.Find(".epos-knopf--primaer").Click();

        Assert.Null(ergebnis);
        Assert.Equal("Die Parameter konnten nicht gespeichert werden.",
                     cut.Find(".epos-warnbanner-text").TextContent);
    }

    [Fact]
    public void Abbrechen_und_Esc_melden_ohne_zu_speichern()
    {
        int gerufen = 0;
        WirtParameterErgebnis? ergebnis = null;
        var cut = Aufbauen(Satz(), speichern: () => { gerufen++; return true; },
                           geschlossen: e => ergebnis = e);

        cut.FindAll("button.epos-knopf").First(b => b.TextContent == "Abbrechen").Click();
        Assert.False(ergebnis!.Gespeichert);

        ergebnis = null;
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(ergebnis!.Gespeichert);
        Assert.Equal(0, gerufen);
    }

    /// <summary>Anwenderentscheid 15.09.2026: das Kreuz der Kopfzeile wirkt wie Esc.</summary>
    [Fact]
    public void Kreuz_meldet_ohne_zu_speichern()
    {
        int gerufen = 0;
        WirtParameterErgebnis? ergebnis = null;
        var cut = Aufbauen(Satz(), speichern: () => { gerufen++; return true; },
                           geschlossen: e => ergebnis = e);

        cut.Find(".epos-dialog-zu").Click();

        Assert.False(ergebnis!.Gespeichert);
        Assert.Equal(0, gerufen);
    }

    /// <summary>Titel-bedingter Kopf: ohne Titel zeigt der Kopf weder Titel noch Kreuz.</summary>
    [Fact]
    public void Ohne_Titel_zeigt_der_Kopf_weder_Titel_noch_Kreuz()
    {
        var cut = Render<WirtschaftlichkeitParameterDialog>(p => p
            .Add(x => x.Parameter, Satz())
            .Add(x => x.Kraftwerksparks, new[] { (0, "(keine Emissionsbilanz)"), (3, "Netzmix 2030") })
            .Add(x => x.ReferenzkesselZeile, "Referenzkessel (aus Projekt): Kessel A — η 92 %, Erdgas")
            .Add(x => x.Co2PrognoseAb, 2028)
            .Add(x => x.Speichern, () => true)
            .Add(x => x.Geschlossen, (WirtParameterErgebnis _) => { })
            .Add(x => x.TitelAnzeigen, false));

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
        // Der Hilfeknopf bleibt - er haengt nicht am Titel.
        Assert.NotEmpty(cut.FindAll(".epos-dialog-kopf"));
    }

    [Fact]
    public void Enter_bleibt_unbelegt_und_der_Infoknopf_traegt_den_Schluessel()
    {
        WirtParameterErgebnis? ergebnis = null;
        var cut = Aufbauen(Satz(), geschlossen: e => ergebnis = e);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Null(ergebnis);

        // ETAPPE E15: Der Kopf trägt genau einen Infoknopf; der zweite gehört der Gruppe
        // „Risiko" und zeigt auf ihren Abschnitt.
        Assert.Single(cut.FindAll(".epos-dialog-kopf .epos-infoknopf"));
        Assert.Equal(2, cut.FindAll(".epos-infoknopf").Count);
        Assert.Equal("Form_WirtschaftlichkeitParameter.btn_Help", cut.Instance.HilfeSchluessel);
    }

    [Fact]
    public void Der_Schlusshinweis_waechst_mit_der_Erzeugerlage()
    {
        string ohne = Aufbauen(Satz()).Instance.Hinweis;
        string mitBhkw = Aufbauen(Satz(), bhkw: true).Instance.Hinweis;
        string mitBeidem = Aufbauen(Satz(), bhkw: true, brennstoff: true).Instance.Hinweis;

        Assert.StartsWith("Die Parameter gelten für Stamm und alle Varianten", ohne);
        Assert.True(mitBhkw.Length > ohne.Length);
        Assert.True(mitBeidem.Length > mitBhkw.Length);
    }

    // =====================================================================
    //  Das Formularraster — Anwenderwunsch iU8-E-2 / W14a-E-7, Paket P2
    //  (Windows-Abnahme 05.09.2026)
    // =====================================================================


    /// <summary>
    /// <b>iU8-E-2 / W14a-E-7 (Paket P2):</b> Die Blöcke „Allgemein", „Strom" und
    /// „Brennstoff" stehen im <c>Formularraster</c>. Die Untergruppe des
    /// Vorläufers (<c>Form_WirtschaftlichkeitParameter.Gruppe</c> „Bilanz") ist
    /// jetzt eine <c>Formulargruppe</c> — leise Zwischenüberschrift, Felder
    /// bleiben direkte Rasterkinder.
    /// </summary>
    [Fact]
    public void Die_Bloecke_stehen_im_Formularraster()
    {
        var cut = Aufbauen(Satz(), brennstoff: true);

        Assert.True(cut.FindAll(".epos-formularraster").Count >= 3);
        Assert.True(cut.FindAll(".epos-formularraster .epos-feld--kurz").Count > 0);

        Assert.Single(cut.FindAll(".epos-formulargruppe-titel"));
        Assert.Empty(cut.FindAll("h3.epos-untergruppe"));

        // Zinssatz, Betrachtungszeitraum, CO2-Preis: Einheit in der Feldzeile.
        Assert.Contains(cut.FindAll(".epos-formularraster .epos-feld--kurz"),
                        f => f.QuerySelector(".epos-feld-zeile .epos-einheit") is not null);
    }
    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F4)
    // =====================================================================

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Sie bindet über die
    /// Sichtklasse <c>WirtschaftlichkeitParameterKiSicht</c> auf DREI Objekte: den
    /// Parametersatz und die zwei Szenariosätze. Ein Szenariofeld zeigt den
    /// WIRKSAMEN Wert und schreibt den gepflegten.
    /// </summary>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_setzt_Satz_und_Szenario()
    {
        var satz = Satz();
        var cut = Aufbauen(satz, brennstoff: true);

        Assert.True(KiMaskenbruecke.IstAngemeldet(
            KiMaskennamen.WIRTSCHAFTLICHKEIT_PARAMETER));

        KiFeldzugang zins = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.WIRTSCHAFTLICHKEIT_PARAMETER, "zinssatz");
        Assert.NotNull(zins);
        Assert.True(zins.Setzbar);

        KiFeldumsetzung neu = KiFeldwandler.Wandle(zins, "4,5");
        Assert.True(neu.Ok, neu.Grund);
        zins.Setzen(neu.Wert);
        cut.Render();
        Assert.Equal(4.5, satz.Zinssatz, 3);

        // Ein SZENARIOFELD: Es liegt an einem anderen Objekt und zeigt den
        // wirksamen Wert - die Vorgabe ist ein Prozentpunkt neben dem Erwartungswert.
        KiFeldzugang best = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.WIRTSCHAFTLICHKEIT_PARAMETER, "best_zinssatz");
        Assert.NotNull(best);
        Assert.True(best.Setzbar);

        KiFeldumsetzung neuBest = KiFeldwandler.Wandle(best, "3,0");
        Assert.True(neuBest.Ok, neuBest.Grund);
        best.Setzen(neuBest.Wert);
        cut.Render();
        Assert.Equal(3.0, Convert.ToDouble(best.Lesen(), CultureInfo.InvariantCulture), 3);
        Assert.Equal(3.0, satz.SatzBest!.Zinssatz!.Value, 3);
    }

    /// <summary>
    /// ETAPPE E9b: Die Zeilen 8 und 9 stehen auch im Katalog des Assistenten — vier
    /// Felder, die den GEPFLEGTEN Wert zeigen (leer = wie Erwartet) und ihn in genau
    /// dieses Szenario schreiben; ein geleertes Feld ist wieder „wie Erwartet".
    /// </summary>
    [Fact]
    public void Der_Assistent_setzt_Zeitraum_und_Menge_je_Szenario()
    {
        var satz = Satz();
        var cut = Aufbauen(satz);

        KiFeldzugang zeitraum = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.WIRTSCHAFTLICHKEIT_PARAMETER, "best_zeitraum");
        Assert.NotNull(zeitraum);
        Assert.True(zeitraum.Setzbar);
        Assert.Null(zeitraum.Lesen());                            // leer = wie Erwartet

        KiFeldumsetzung neu = KiFeldwandler.Wandle(zeitraum, "25");
        Assert.True(neu.Ok, neu.Grund);
        zeitraum.Setzen(neu.Wert);
        cut.Render();
        Assert.Equal(25, satz.SatzBest!.Zeitraum);
        Assert.Equal("25", Feld(cut.FindAll("table.epos-matrix tbody tr")[ZEILE_ZEITRAUM], 0)
                               .GetAttribute("value"));

        KiFeldzugang menge = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.WIRTSCHAFTLICHKEIT_PARAMETER, "worst_menge");
        Assert.NotNull(menge);
        KiFeldumsetzung neuMenge = KiFeldwandler.Wandle(menge, "-15");
        Assert.True(neuMenge.Ok, neuMenge.Grund);
        menge.Setzen(neuMenge.Wert);
        cut.Render();
        Assert.Equal(-15.0, satz.SatzWorst!.Menge);
        Assert.Equal(-15.0, Convert.ToDouble(menge.Lesen(), CultureInfo.InvariantCulture), 3);

        KiFeldumsetzung leer = KiFeldwandler.Wandle(menge, "");
        Assert.True(leer.Ok, leer.Grund);
        menge.Setzen(leer.Wert);
        Assert.Null(satz.SatzWorst.Menge);

        Assert.NotNull(KiMaskenbruecke.Feldzugang(KiMaskennamen.WIRTSCHAFTLICHKEIT_PARAMETER, "worst_zeitraum"));
        Assert.NotNull(KiMaskenbruecke.Feldzugang(KiMaskennamen.WIRTSCHAFTLICHKEIT_PARAMETER, "best_menge"));
    }

    // =====================================================================
    //  Risiko (ETAPPE E15, V-G7 — DIN EN 17463, 6.5 und Anhang F)
    // =====================================================================

    /// <summary>
    /// ETAPPE E15: Vorgabe AUS — die Klappliste steht auf „aus", alle drei Zahlenfelder
    /// sind gesperrt (nicht ausgeblendet), und die Herleitungszeile sagt, dass kein Risiko
    /// angesetzt ist.
    /// </summary>
    [Fact]
    public void Risiko_Vorgabe_aus_sperrt_die_drei_Felder()
    {
        var cut = Aufbauen(Satz());
        var zahlen = cut.FindAll("input[inputmode=decimal]");

        Assert.True(zahlen[RISIKO_ZUSCHLAG].HasAttribute("disabled"));
        Assert.True(zahlen[RISIKO_VERLUST].HasAttribute("disabled"));
        Assert.True(zahlen[RISIKO_P].HasAttribute("disabled"));
        Assert.Equal("0", cut.Find("select").GetAttribute("value"));
        Assert.Contains(cut.FindAll(".epos-herleitung-text"),
                        e => e.TextContent.StartsWith("Kein Risiko angesetzt"));
    }

    /// <summary>
    /// ETAPPE E15: Die Wahl „Zinszuschlag" gibt genau das Zuschlagsfeld frei; die
    /// Herleitungszeile nennt den Zins, mit dem gerechnet wird (i + Zuschlag).
    /// </summary>
    [Fact]
    public void Risiko_Zinszuschlag_gibt_sein_Feld_frei_und_nennt_den_Zins()
    {
        WirtschaftlichkeitParameter satz = Satz();
        var cut = Aufbauen(satz);

        cut.Find("select").Change("1");
        Assert.Equal(Risikoart.ZINS, satz.RisikoArt);

        var zahlen = cut.FindAll("input[inputmode=decimal]");
        Assert.False(zahlen[RISIKO_ZUSCHLAG].HasAttribute("disabled"));
        Assert.True(zahlen[RISIKO_VERLUST].HasAttribute("disabled"));
        Assert.True(zahlen[RISIKO_P].HasAttribute("disabled"));

        zahlen[RISIKO_ZUSCHLAG].Input("1");
        Assert.Equal(1.0, satz.RisikoZinszuschlag);
        Assert.Contains(cut.FindAll(".epos-herleitung-text"),
                        e => e.TextContent.Contains("3,50 % + 1,00 %-Punkte = 4,50 %"));
    }

    /// <summary>
    /// ETAPPE E15: Die Wahl „Zahlungsstromabzug" gibt R_loss und p_loss frei; die
    /// Herleitungszeile nennt den Abzug je Periode (10.000 € × 10 % = 1.000 €).
    /// Gespeichert wird mit „Speichern" des Dialogs; zurück auf „aus" wird die Art leer.
    /// </summary>
    [Fact]
    public void Risiko_Abzug_gibt_R_loss_und_p_loss_frei_und_speichert()
    {
        WirtschaftlichkeitParameter satz = Satz();
        WirtParameterErgebnis? ergebnis = null;
        var cut = Aufbauen(satz, geschlossen: e => ergebnis = e);

        cut.Find("select").Change("2");
        Assert.Equal(Risikoart.ABZUG, satz.RisikoArt);

        var zahlen = cut.FindAll("input[inputmode=decimal]");
        Assert.True(zahlen[RISIKO_ZUSCHLAG].HasAttribute("disabled"));
        Assert.False(zahlen[RISIKO_VERLUST].HasAttribute("disabled"));
        Assert.False(zahlen[RISIKO_P].HasAttribute("disabled"));

        zahlen[RISIKO_VERLUST].Input("10000");
        cut.FindAll("input[inputmode=decimal]")[RISIKO_P].Input("10");
        Assert.Equal(10000.0, satz.RisikoVerlust);
        Assert.Equal(10.0, satz.RisikoWahrscheinlichkeit);
        Assert.Contains(cut.FindAll(".epos-herleitung-text"),
                        e => e.TextContent.Contains("= 1.000,00 €"));

        cut.Find(".epos-knopf--primaer").Click();
        Assert.True(ergebnis!.Gespeichert);

        // Zurück auf „aus": leer heißt kein Risiko; die Zahlen bleiben erhalten.
        var neu = Aufbauen(satz);
        neu.Find("select").Change("0");
        Assert.Null(satz.RisikoArt);
        Assert.Equal(10000.0, satz.RisikoVerlust);
    }

    /// <summary>
    /// ETAPPE E15: Die Gruppe „Risiko" trägt ihren eigenen Infoknopf mit dem Schlüssel
    /// des help_mapping (Abschnitt „risiko" der Seite Wirtschaftlichkeit).
    /// </summary>
    [Fact]
    public void Risiko_Gruppe_traegt_ihren_Infoknopf()
    {
        var cut = Aufbauen(Satz());

        Assert.Contains(cut.FindAll(".epos-infoknopf"),
                        k => k.GetAttribute("title") != null);
        Assert.Contains("Form_WirtschaftlichkeitParameter.btn_Help_Risiko", cut.Markup);
    }

    /// <summary>
    /// ETAPPE E15: Die vier Felder der Gruppe stehen im Katalog des Assistenten — die Art
    /// als Wahl (Schlüssel ZINS/ABZUG, leer = aus), die drei Zahlen nullbar.
    /// </summary>
    [Fact]
    public void Der_Assistent_setzt_das_Risiko()
    {
        var satz = Satz();
        var cut = Aufbauen(satz);

        KiFeldzugang art = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.WIRTSCHAFTLICHKEIT_PARAMETER, "risiko_art");
        Assert.NotNull(art);
        Assert.True(art.Setzbar);
        KiFeldumsetzung abzug = KiFeldwandler.Wandle(art, Risikoart.ABZUG);
        Assert.True(abzug.Ok, abzug.Grund);
        art.Setzen(abzug.Wert);
        cut.Render();
        Assert.Equal(Risikoart.ABZUG, satz.RisikoArt);

        KiFeldzugang verlust = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.WIRTSCHAFTLICHKEIT_PARAMETER, "risiko_verlust");
        Assert.NotNull(verlust);
        Assert.Null(verlust.Lesen());
        KiFeldumsetzung v = KiFeldwandler.Wandle(verlust, "10000");
        Assert.True(v.Ok, v.Grund);
        verlust.Setzen(v.Wert);
        Assert.Equal(10000.0, satz.RisikoVerlust);

        Assert.NotNull(KiMaskenbruecke.Feldzugang(KiMaskennamen.WIRTSCHAFTLICHKEIT_PARAMETER, "risiko_zinszuschlag"));
        Assert.NotNull(KiMaskenbruecke.Feldzugang(KiMaskennamen.WIRTSCHAFTLICHKEIT_PARAMETER, "risiko_wahrscheinlichkeit"));
    }
}
