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
/// <item>Szenarien (immer, ETAPPE W5‑B‑9 vom 09.09.2026, seit W5‑B‑12 mit p_I):
///       sieben Größen × Best und Worst = 14 Zahlenfelder, dazu der Knopf
///       „Vorgaben“. Die Erwartet-Spalte ist Anzeige. Die Felder stehen im
///       Feldbestand ZWISCHEN Allgemein und Strom — daher die verschobenen Indizes
///       der Zahlenfelder unten.</item>
/// <item>Bewertung nach DIN EN 17463 (immer, ETAPPE W5‑B‑12): das mehrzeilige
///       Freitextfeld „Nicht monetäre Wirkungen“ (VALERI-Lücke G6)</item>
/// <item>Strom (immer): Einspeisung PV, Einspeisung KWK, Aufschläge-Anzeige (3)</item>
/// <item>BHKW (nur mit BHKW): Verweis + Sprungknopf</item>
/// <item>Brennstoff (nur mit Brennstoff-Erzeuger): CO₂ + Katalogknopf + Park +
///       Referenzkessel + Bilanzjahr, Methode, Biomasse, Nachweis (7)</item>
/// </list>
/// </summary>
public class WirtschaftlichkeitParameterDialogTests : BunitContext
{
    public WirtschaftlichkeitParameterDialogTests()
    {
        // Seit iU9-W14c.3 steht der Gesetzeskatalog als Ueberlagerung in diesem
        // Dialog, und der bringt ein Raster (QuickGrid) mit - das laedt sein
        // JS-Modul beim Zeichnen.
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>
    /// Die Sprache der Oberflaeche wird auf de-DE gepinnt (Muster
    /// <c>GebaeudeKatalogDialogTests</c>, Regel seit W8, verschaerft am 04.09.2026).
    /// Diese Klasse prueft deutsche Beschriftungen; sich darauf zu verlassen, dass
    /// eine andere Klasse den Prozessstandard gesetzt hat, war die Ursache der
    /// W12-Rotmeldung auf dem Windows-Laeufer.
    /// </summary>
    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
    }

    // ETAPPE W5-B-9 (09.09.2026): Der Szenarioblock steht ZWISCHEN Allgemein und
    // Strom und bringt seine Zahlenfelder mit. Die Indizes der uebrigen Felder
    // haben sich dadurch verschoben - sie stehen hier als Konstanten, damit der
    // naechste Umbau nur eine Zeile trifft statt sieben.
    // ETAPPE W5-B-12 (09.09.2026): p_I bringt ein viertes Dezimalfeld nach
    // "Allgemein" und eine siebte Zeile in die Szenariotabelle - genau diese zwei
    // Zahlen aendern sich hier, der Rest rechnet sich daraus.
    private const int ALLGEMEIN_FELDER = 4;
    private const int SZENARIO_FELDER = 14;
    private const int EINSPEISUNG_PV = ALLGEMEIN_FELDER + SZENARIO_FELDER;
    private const int EINSPEISUNG_KWK = EINSPEISUNG_PV + 1;
    private const int CO2 = EINSPEISUNG_KWK + 1;
    private const int FELDER_OHNE_ERZEUGER = ALLGEMEIN_FELDER + SZENARIO_FELDER + 2;

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
        NachhaltigkeitsnachweisBiomasse = true,
        // BHKW-Angaben: der Dialog zeigt sie nicht mehr (Auszug B5/BW9).
        KwkgBonus = 4.0,
        KwkgVbhKontingent = 30000
    };

    private IRenderedComponent<WirtschaftlichkeitParameterDialog> Aufbauen(
        WirtschaftlichkeitParameter satz,
        bool bhkw = false, bool brennstoff = false,
        Func<bool>? speichern = null,
        Action<WirtParameterErgebnis>? geschlossen = null,
        Func<IReadOnlyDictionary<string, object>>? gesetzeGaben = null)
    {
        return Render<WirtschaftlichkeitParameterDialog>(p => p
            .Add(x => x.Parameter, satz)
            .Add(x => x.HatBhkw, bhkw)
            .Add(x => x.HatBrennstoff, brennstoff)
            .Add(x => x.Kraftwerksparks, new[] { (0, "(keine Emissionsbilanz)"), (3, "Netzmix 2030") })
            .Add(x => x.ReferenzkesselZeile, "Referenzkessel (aus Projekt): Kessel A — η 92 %, Erdgas")
            .Add(x => x.Co2PrognoseAb, 2028)
            .Add(x => x.GesetzeGaben, gesetzeGaben)
            .Add(x => x.Speichern, speichern ?? (() => true))
            .Add(x => x.Geschlossen, geschlossen ?? (_ => { })));
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
                             "Bewertung nach DIN EN 17463",
                             "Strom — Einspeisung und Bezug" },
                     cut.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent).ToArray());

        // Zins, PreisE, PreisB, PreisI (4) + Szenarien 7×2 (14) + Einspeisung PV, KWK (2)
        Assert.Equal(FELDER_OHNE_ERZEUGER, cut.FindAll("input[inputmode=decimal]").Count);
        Assert.Single(cut.FindAll("input[inputmode=numeric]"));   // T
        Assert.Single(cut.FindAll("input[type=checkbox]"));       // Aufschlaege (Anzeige)
        Assert.Empty(cut.FindAll("select"));
    }

    [Fact]
    public void Der_Aufschlagshaken_zeigt_nur_an_und_ist_nicht_bedienbar()
    {
        // Ae16: Die Auswahl liegt im Energietraegerdialog.
        var cut = Aufbauen(Satz());

        Assert.True(cut.Find("input[type=checkbox]").HasAttribute("disabled"));
    }

    [Fact]
    public void Mit_BHKW_erscheint_der_Verweis_statt_der_beiden_alten_Gruppen()
    {
        // Etappe B5/BW9: KWKG- und Steuerangaben stehen im eigenen Dialog.
        var cut = Aufbauen(Satz(), bhkw: true);
        var titel = cut.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent).ToList();

        Assert.Contains("BHKW — KWKG, Energie- und Stromsteuer", titel);
        Assert.Single(cut.FindAll("button.epos-sprung"));
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
        Assert.Equal(2, cut.FindAll("input[inputmode=numeric]").Count);   // + Bilanzjahr
        Assert.Equal(3, cut.FindAll("select").Count);                     // Park, Methode, Biomasse
        Assert.Equal(2, cut.FindAll("input[type=checkbox]").Count);       // + Nachweis
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
    /// Die Tabelle trägt drei Wertspalten über sieben Zeilen (seit W5‑B‑12 mit p_I,
    /// unmittelbar hinter den beiden anderen Preissteigerungen). Die Erwartet-Spalte ist
    /// ANZEIGE — sie wiederholt die Projektparameter und trägt kein Eingabefeld
    /// („Kein Delegat ist kein Knopf“).
    /// </summary>
    [Fact]
    public void Die_Szenariotabelle_zeigt_drei_Spalten_und_sieben_Groessen()
    {
        var cut = Aufbauen(Satz());
        var tabelle = cut.FindAll("table.epos-matrix")[0];

        Assert.Equal(new[] { "Größe", "Erwartet", "Best", "Worst" },
                     tabelle.QuerySelectorAll("thead th").Select(e => e.TextContent).ToArray());

        var zeilen = tabelle.QuerySelectorAll("tbody tr");
        Assert.Equal(7, zeilen.Length);
        Assert.Equal(new[] { "Kalkulationszins", "Preissteigerung Energie",
                             "Preissteigerung Betrieb", "Preissteigerung Investition",
                             "Investition", "Erträge", "Nutzungsdauer" },
                     zeilen.Select(z => z.QuerySelector(".epos-matrix-titel")!.TextContent).ToArray());

        // Je Zeile genau ZWEI Eingabefelder - Best und Worst.
        foreach (var z in zeilen) Assert.Equal(2, z.QuerySelectorAll("input").Length);
        Assert.Equal(SZENARIO_FELDER, tabelle.QuerySelectorAll("input").Length);
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
    /// vierzehn Felder wieder auf <c>null</c> (seit W5‑B‑12 mit p_I); das ist NICHT
    /// dasselbe wie „auf die heutigen Vorgabezahlen setzen“, denn ein leeres Feld zieht
    /// bei einer geänderten Projektangabe mit.
    /// </summary>
    [Fact]
    public void Vorgaben_setzt_die_vierzehn_Felder_zurueck()
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

        cut.FindAll("button.epos-knopf").First(b => b.TextContent.Trim() == "Vorgaben").Click();

        Assert.Null(satz.SatzBest.Zinssatz);
        Assert.Null(satz.SatzWorst.PreissteigerungInvestition);
        Assert.True(satz.SatzBest.NurVorgaben);
        Assert.True(satz.SatzWorst.NurVorgaben);
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
    /// VALERI-Lücke G6: Das mehrzeilige Freitextfeld nimmt die nicht monetären Wirkungen
    /// auf und schreibt sie in den Parametersatz — von dort holen Bericht und Seite sie.
    /// </summary>
    [Fact]
    public void Der_Freitext_der_nicht_monetaeren_Wirkungen_wird_uebernommen()
    {
        WirtschaftlichkeitParameter satz = Satz();
        var cut = Aufbauen(satz);

        var feld = cut.Find("textarea");
        Assert.Equal("", feld.TextContent);

        feld.Input("Versorgungssicherheit, Arbeitsschutz");

        Assert.Equal("Versorgungssicherheit, Arbeitsschutz", satz.NichtMonetaer);
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

    [Fact]
    public void Ein_KWK_Preis_von_null_heisst_nicht_gepflegt()
    {
        WirtschaftlichkeitParameter satz = Satz();
        var cut = Aufbauen(satz);

        cut.FindAll("input[inputmode=decimal]")[EINSPEISUNG_KWK].Input("0,1200");
        Assert.Equal(0.12, satz.EinspeiseverguetungKWK);

        cut.FindAll("input[inputmode=decimal]")[EINSPEISUNG_KWK].Input("0");
        Assert.Null(satz.EinspeiseverguetungKWK);
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
    // Speichern und Sprünge
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
        Assert.Equal(WirtParameterSprung.Keiner, ergebnis.Sprung);
        Assert.Equal(4.25, satz.Zinssatz);
        Assert.Equal(25, satz.Betrachtungszeitraum);
    }

    [Fact]
    public void Die_ausgezogenen_BHKW_Werte_bleiben_unberuehrt()
    {
        // Sie stehen nicht mehr im Dialog und gehen wertgleich in die Zeile.
        WirtschaftlichkeitParameter satz = Satz();
        var cut = Aufbauen(satz, bhkw: true);

        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(4.0, satz.KwkgBonus);
        Assert.Equal(30000, satz.KwkgVbhKontingent);
    }

    [Fact]
    public void Der_BHKW_Knopf_meldet_den_nachgelagerten_Sprung()
    {
        WirtParameterErgebnis? ergebnis = null;
        var cut = Aufbauen(Satz(), bhkw: true, geschlossen: e => ergebnis = e);

        cut.Find("button.epos-sprung").Click();

        Assert.Equal(WirtParameterSprung.BhkwWirtschaftlichkeit, ergebnis!.Sprung);
        Assert.False(ergebnis.Gespeichert);
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

    [Fact]
    public void Enter_bleibt_unbelegt_und_der_Infoknopf_traegt_den_Schluessel()
    {
        WirtParameterErgebnis? ergebnis = null;
        var cut = Aufbauen(Satz(), geschlossen: e => ergebnis = e);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Null(ergebnis);

        Assert.Single(cut.FindAll(".epos-infoknopf"));
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
}
