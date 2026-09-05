using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Tarifdialog (iU9-W2.3). Soll ist die Handkarte der gelöschten Maske
/// <c>Views/Wirtschaftlichkeit/Form_Tarifstruktur.cs</c> (K4, ohne Designer —
/// die Felder wurden aus <c>InitializeComponent</c> von Hand aufgenommen):
///
/// <list type="bullet">
/// <item>Kopf: Schalter „aktiv", Auswahl „Tarifmodell", Datum „Preisstand" (3)</item>
/// <item>Zeitzonen: Winter von/bis Monat, HT von/bis Stunde (4)</item>
/// <item>Zonenmodell: 4 Bezugs- + 4 Einspeisepreise + 3 Staffelfelder (11)</item>
/// <item>Rollenmodell: je Rolle Arbeit, Grund, Modell, Monat und 4×3 Staffel
///       (16), zweimal — dazu Einspeisung Arbeit + Grund (2)</item>
/// </list>
///
/// Zahlen in der Anzeige: <c>de-DE</c> wie in <c>SpeichernLeisteTests</c> —
/// die CI-Läufer laufen englisch.
/// </summary>
public class TarifstrukturDialogTests : BunitContext
{
    public TarifstrukturDialogTests()
    {
        Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static TarifParameter Satz()
    {
        var p = new TarifParameter
        {
            IdStamm = 1030,
            Aktiv = true,
            Modus = DbWerte.TARIF_MODUS_ZONEN,
            WinterVonMonat = 10,
            WinterBisMonat = 3,
            HtVonStunde = 6,
            HtBisStunde = 22,
            PreisBezugWinterHT = 0.3210,
            PreisBezugWinterNT = 0.2100,
            PreisBezugSommerHT = 0.3000,
            PreisBezugSommerNT = 0.1900,
            PreisEinspWinterHT = 0.0800,
            PreisEinspWinterNT = 0.0700,
            PreisEinspSommerHT = 0.0600,
            PreisEinspSommerNT = 0.0500,
            StaffelGrenzeKW = 500,
            StaffelPreis1EurKW = 120,
            StaffelPreis2EurKW = 95
        };
        p.Bezug.ArbeitspreisEurKWh = 0.2500;
        p.Bezug.GrundpreisEurJahr = 1200;
        p.Bezug.MonatspreisEurKWMonat = 8.5;
        p.Bezug.Leistungsmodell = DbWerte.LEISTUNGSMODELL_STAFFEL;
        p.Bezug.Stufen[0].ObergrenzeKW = 500;
        p.Bezug.Stufen[0].PreisSommer = 30;
        p.Bezug.Stufen[0].PreisWinter = 90;
        p.Reststrom.ArbeitspreisEurKWh = 0.2800;
        p.Einspeisung.ArbeitspreisEurKWh = 0.0650;
        p.Einspeisung.GrundpreisEurJahr = 40;
        return p;
    }

    private IRenderedComponent<TarifstrukturDialog> Aufbauen(
        TarifParameter satz,
        TarifSicht sicht = TarifSicht.Komplett,
        Func<bool>? speichern = null,
        Action<bool>? geschlossen = null)
    {
        return Render<TarifstrukturDialog>(p => p
            .Add(x => x.Tarif, satz)
            .Add(x => x.Sicht, sicht)
            .Add(x => x.Speichern, speichern ?? (() => true))
            .Add(x => x.Geschlossen, geschlossen ?? (_ => { })));
    }

    // =====================================================================
    // Feldbestand
    // =====================================================================

    [Fact]
    public void Die_volle_Sicht_zeigt_den_Feldbestand_der_Karte()
    {
        var cut = Aufbauen(Satz());

        // 4 Zeitzonen (Ganzzahl) + 11 Zonen + 2×16 Rollen + 2 Einspeisung
        Assert.Equal(4, cut.FindAll("input[inputmode=numeric]").Count);
        Assert.Equal(11 + 2 * 15 + 2, cut.FindAll("input[inputmode=decimal]").Count);

        Assert.Single(cut.FindAll("input[type=checkbox]"));      // aktiv
        Assert.Single(cut.FindAll("input[type=date]"));          // Preisstand
        Assert.Equal(3, cut.FindAll("select").Count);            // Modus + 2 Leistungsmodelle
        Assert.Equal(2, cut.FindAll("button.epos-knopf").Count); // Speichern, Abbrechen
    }

    [Fact]
    public void Die_Gruppentitel_stehen_wie_in_der_Maske()
    {
        var cut = Aufbauen(Satz());
        var titel = cut.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent).ToList();

        Assert.Contains("Zeitzonen (HT gilt Mo–Fr; Referenzjahr 2026)", titel);
        Assert.Contains("Zonenmodell (Stufe W3) — vier Zonenpreise, zweistufige Staffel", titel);
        Assert.Contains("Rollenmodell (Etappe E5) — Differenzmethode „vermiedene Kosten“", titel);
    }

    [Fact]
    public void Die_Vorbelegung_kommt_aus_dem_geladenen_Tarifsatz()
    {
        var cut = Aufbauen(Satz());
        var zahlen = cut.FindAll("input[inputmode=decimal]");

        Assert.Equal("0,3210", zahlen[0].GetAttribute("value"));   // Bezug Winter HT
        Assert.Equal("0,0800", zahlen[4].GetAttribute("value"));   // Einspeisung Winter HT
        Assert.Equal("500", zahlen[8].GetAttribute("value"));      // Staffelgrenze
        Assert.True(cut.FindAll("input[type=checkbox]")[0].HasAttribute("checked"));
    }

    // =====================================================================
    // Sichten (Ae18)
    // =====================================================================

    [Fact]
    public void Die_Strombezugssicht_laesst_die_Einspeisung_weg()
    {
        var cut = Aufbauen(Satz(), TarifSicht.Strombezug);

        // Zonen: nur Bezug (4) + Staffel (3); Rollen: nur Bezug (15)
        Assert.Equal(4 + 3 + 15, cut.FindAll("input[inputmode=decimal]").Count);
        Assert.Equal(2, cut.FindAll("select").Count);   // Modus + 1 Leistungsmodell
    }

    [Fact]
    public void Die_PV_Sicht_zeigt_nur_die_Einspeisepreise_beider_Modelle()
    {
        var cut = Aufbauen(Satz(), TarifSicht.Photovoltaik);

        // Zonen: nur Einspeisung (4); Rollen: nur Einspeisung (2)
        Assert.Equal(4 + 2, cut.FindAll("input[inputmode=decimal]").Count);
        Assert.Single(cut.FindAll("select"));           // nur der Modus
    }

    [Fact]
    public void Eine_Komponentensicht_sagt_dass_der_Tarifsatz_geteilt_ist()
    {
        Assert.DoesNotContain(Aufbauen(Satz()).FindAll(".epos-herleitung"),
                              e => e.TextContent.StartsWith("Komponentensicht"));

        var cut = Aufbauen(Satz(), TarifSicht.Bhkw);
        Assert.Contains(cut.FindAll(".epos-herleitung-text"),
                        e => e.TextContent.StartsWith("Komponentensicht"));
    }

    [Fact]
    public void Eine_Sicht_ueberschreibt_nur_ihre_eigenen_Felder()
    {
        // Ae18: In der PV-Sicht sind Bezugspreise und Staffel gar nicht gebaut -
        // sie muessen den geladenen Wert behalten.
        TarifParameter satz = Satz();
        var cut = Aufbauen(satz, TarifSicht.Photovoltaik);

        cut.FindAll("input[inputmode=decimal]")[0].Input("0,0900");   // Einspeisung Winter HT
        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(0.0900, satz.PreisEinspWinterHT, 4);
        Assert.Equal(0.3210, satz.PreisBezugWinterHT, 4);   // unberuehrt
        Assert.Equal(500, satz.StaffelGrenzeKW, 3);         // unberuehrt
    }

    // =====================================================================
    // Modellumschaltung
    // =====================================================================

    [Fact]
    public void Im_Zonenmodell_ist_der_Rollenblock_gesperrt_bleibt_aber_lesbar()
    {
        var cut = Aufbauen(Satz());
        var zahlen = cut.FindAll("input[inputmode=decimal]");

        Assert.False(zahlen[0].HasAttribute("disabled"));    // Zonen: bedienbar
        Assert.True(zahlen[11].HasAttribute("disabled"));    // Rollen: gesperrt
        Assert.Equal("0,2500", zahlen[11].GetAttribute("value"));
    }

    [Fact]
    public void Das_Rollenmodell_dreht_die_Sperren_um_und_nimmt_HT_NT_heraus()
    {
        var cut = Aufbauen(Satz());

        cut.Find("select").Change("1");   // Rollenmodell

        Assert.True(cut.Instance.Rollen);
        var zahlen = cut.FindAll("input[inputmode=decimal]");
        Assert.True(zahlen[0].HasAttribute("disabled"));     // Zonenpreise gesperrt
        Assert.False(zahlen[11].HasAttribute("disabled"));   // Rollenpreise frei

        // HT/NT entfaellt im Rollenmodell (L10), Winterspanne bleibt.
        var ganz = cut.FindAll("input[inputmode=numeric]");
        Assert.False(ganz[0].HasAttribute("disabled"));      // Winter von
        Assert.True(ganz[2].HasAttribute("disabled"));       // HT von
        Assert.True(ganz[3].HasAttribute("disabled"));       // HT bis
    }

    // =====================================================================
    // Speichern
    // =====================================================================

    [Fact]
    public void Speichern_uebernimmt_die_Eingaben_und_schliesst()
    {
        TarifParameter satz = Satz();
        bool? ergebnis = null;
        int gerufen = 0;
        var cut = Aufbauen(satz, speichern: () => { gerufen++; return true; },
                           geschlossen: e => ergebnis = e);

        cut.FindAll("input[inputmode=decimal]")[0].Input("0,4000");
        cut.FindAll("input[inputmode=numeric]")[0].Input("11");
        cut.Find("input[type=date]").Change("2026-01-01");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(1, gerufen);
        Assert.True(ergebnis);
        Assert.Equal(0.4000, satz.PreisBezugWinterHT, 4);
        Assert.Equal(11, satz.WinterVonMonat);
        Assert.Equal(new DateTime(2026, 1, 1), satz.GueltigAb);
    }

    [Fact]
    public void Ein_geleertes_Feld_behaelt_den_geladenen_Wert()
    {
        // Eine NumericUpDown konnte nicht leer sein; ein Eingabefeld schon.
        TarifParameter satz = Satz();
        var cut = Aufbauen(satz);

        cut.FindAll("input[inputmode=decimal]")[0].Input("");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(0.3210, satz.PreisBezugWinterHT, 4);
    }

    [Fact]
    public void Ein_leeres_HT_Fenster_haelt_den_Dialog_an()
    {
        TarifParameter satz = Satz();
        int gerufen = 0;
        var cut = Aufbauen(satz, speichern: () => { gerufen++; return true; });

        cut.FindAll("input[inputmode=numeric]")[2].Input("22");   // HT von = HT bis
        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(0, gerufen);
        Assert.Equal("Das HT-Fenster ist leer (von ≥ bis).",
                     cut.Find(".epos-warnbanner-text").TextContent);
    }

    [Fact]
    public void Aktiv_ohne_Bezugspreis_warnt_einmal_und_speichert_dann()
    {
        // Der Vorlaeufer zeigte hier eine MessageBox und speicherte danach.
        TarifParameter satz = Satz();
        int gerufen = 0;
        var cut = Aufbauen(satz, speichern: () => { gerufen++; return true; });

        var zahlen = cut.FindAll("input[inputmode=decimal]");
        for (int i = 0; i < 4; i++) zahlen[i].Input("0");

        cut.Find(".epos-knopf--primaer").Click();
        Assert.Equal(0, gerufen);
        Assert.Contains("kein Bezugspreis gepflegt", cut.Find(".epos-warnbanner-text").TextContent);

        cut.Find(".epos-knopf--primaer").Click();
        Assert.Equal(1, gerufen);
    }

    [Fact]
    public void Ein_Speicherfehler_meldet_sich_und_haelt_den_Dialog_offen()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(Satz(), speichern: () => false, geschlossen: e => ergebnis = e);

        cut.Find(".epos-knopf--primaer").Click();

        Assert.Null(ergebnis);
        Assert.Equal("Die Tarifstruktur konnte nicht gespeichert werden.",
                     cut.Find(".epos-warnbanner-text").TextContent);
    }

    [Fact]
    public void Abbrechen_und_Esc_melden_false_und_schreiben_nicht()
    {
        TarifParameter satz = Satz();
        int gerufen = 0;
        bool? ergebnis = null;
        var cut = Aufbauen(satz, speichern: () => { gerufen++; return true; },
                           geschlossen: e => ergebnis = e);

        cut.FindAll("button.epos-knopf")[0].Click();
        Assert.False(ergebnis);
        Assert.Equal(0, gerufen);

        ergebnis = null;
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(ergebnis);
        Assert.Equal(0, gerufen);
    }

    [Fact]
    public void Enter_bleibt_unbelegt()
    {
        // A-7 aus B5b: In einer Maske mit fuenfzig Zahlenfeldern waere ein
        // versehentliches Enter kein Bestaetigen, sondern ein Zufall.
        bool gemeldet = false;
        var cut = Aufbauen(Satz(), geschlossen: _ => gemeldet = true);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.False(gemeldet);
    }

    [Fact]
    public void Der_Infoknopf_traegt_den_Schluessel_der_Maske()
    {
        var cut = Aufbauen(Satz());

        Assert.Single(cut.FindAll(".epos-infoknopf"));
        Assert.Equal("Form_Tarifstruktur.btn_Help", cut.Instance.HilfeSchluessel);
    }

    [Fact]
    public void Das_Leistungsmodell_geht_als_Steuerwert_zurueck()
    {
        TarifParameter satz = Satz();
        var cut = Aufbauen(satz);

        // select[0] = Modus, select[1] = Leistungsmodell der Bezugsrolle
        cut.FindAll("select")[1].Change("2");   // Jahreshoechstlast
        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(DbWerte.LEISTUNGSMODELL_JAHRESHOECHSTLAST, satz.Bezug.Leistungsmodell);
    }

    [Fact]
    public void Die_Staffelfelder_tragen_Stufe_und_Spalte_in_der_Beschriftung()
    {
        // Abweichung zur Maske: dort drei Spaltenkoepfe ueber einem Raster,
        // hier je Feld eine eigene Beschriftung (Touch und Sprachausgabe).
        var cut = Aufbauen(Satz());
        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();

        Assert.Contains("Stufe 1 — Obergrenze [kW]", texte);
        Assert.Contains("Stufe 2 — Sommer [€/kW·a]", texte);
        Assert.Contains("Stufe 4 (Rest) — Winter [€/kW·a]", texte);
    }
}
