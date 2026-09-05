using System.Globalization;
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
/// <item>Allgemein (immer): Zins, T, Preissteigerung Energie und Betrieb (4)</item>
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

        Assert.Equal(new[] { "Allgemein", "Strom — Einspeisung und Bezug" },
                     cut.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent).ToArray());

        // Zins, PreisE, PreisB, Einspeisung PV, Einspeisung KWK
        Assert.Equal(5, cut.FindAll("input[inputmode=decimal]").Count);
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
        Assert.Equal(5, cut.FindAll("input[inputmode=decimal]").Count);
    }

    [Fact]
    public void Mit_Brennstoff_erscheint_die_Emissionsgruppe_vollstaendig()
    {
        var cut = Aufbauen(Satz(), brennstoff: true,
                           gesetzeGaben: () => new Dictionary<string, object>());
        var titel = cut.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent).ToList();

        Assert.Contains("Brennstoff — BEHG und Emissionsbilanz (BHKW/Kessel)", titel);
        Assert.Equal(6, cut.FindAll("input[inputmode=decimal]").Count);   // + CO2
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
        Assert.Equal("0,0820", zahlen[3].GetAttribute("value"));
        Assert.Equal("20", cut.Find("input[inputmode=numeric]").GetAttribute("value"));
    }

    // =====================================================================
    // CO₂-Zeile (K6)
    // =====================================================================

    [Fact]
    public void Die_CO2_Zeile_unterscheidet_Pfad_und_konstanten_Preis()
    {
        // 0 heisst seit K6 nicht mehr "aus", sondern "Pfad".
        var cut = Aufbauen(Satz(), brennstoff: true);
        Assert.Contains("2028", cut.Instance.Co2Zeile);

        cut.FindAll("input[inputmode=decimal]")[5].Input("95");
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

        cut.FindAll("input[inputmode=decimal]")[4].Input("0,1200");
        Assert.Equal(0.12, satz.EinspeiseverguetungKWK);

        cut.FindAll("input[inputmode=decimal]")[4].Input("0");
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
