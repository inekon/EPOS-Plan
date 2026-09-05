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
/// Der PV-Vergütungsdialog (iU9-W2.4). Soll ist die Feldkarte von
/// <c>Form_PhotovoltaikVerguetung</c> (36 Zeilen, 7 Gruppen):
///
/// <list type="bullet">
/// <item>Kopf: Schalter „Vergütung anwenden"</item>
/// <item>Anlage: kWp-Override, Inbetriebnahme, Einspeiseart (2 Optionen)</item>
/// <item>Anzulegender Wert: AW-Override + drei Herleitungszeilen</item>
/// <item>Vermarktung: 4 Optionen, DV-Entgelt, PPA-Preis, PPA-Aufschlag</item>
/// <item>§ 51/§ 51a: Anwenden, iMSys-Jahr, Ausfallanteil, § 51a-Schalter</item>
/// <item>Bezugsbewertung: ein Schalter · Kappung: ein Auswahlfeld</item>
/// <item>Vorschau: zwei Zeilen · dazu die Knöpfe Marktwerte und Tarif</item>
/// </list>
///
/// Der Gesetzeskatalog kommt als Delegat herein — die Tests legen die Werte
/// selbst, ohne Datenbank.
/// </summary>
public class PhotovoltaikVerguetungDialogTests : BunitContext
{
    public PhotovoltaikVerguetungDialogTests()
    {
        Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Der Katalog mit den Grenzwerten, die der Dialog liest.</summary>
    private static double? Katalog(string schluessel, int jahr) => schluessel switch
    {
        DbWerte.GESETZ_EEG_EV_GRENZE_KW => 100,
        DbWerte.GESETZ_EEG_UNENTGELTLICH_GRENZE_KW => 200,
        DbWerte.GESETZ_EEG_51_GRENZE_KW => 100,
        DbWerte.GESETZ_EEG_EV_ABSCHLAG => 0.4,
        _ => null
    };

    private static ProjektPhotovoltaikModel Satz(double? kwpOverride = null) =>
        new ProjektPhotovoltaikModel
        {
            ID_Projekt = 1030,
            Aktiv = true,
            Einspeiseart = DbWerte.PV_EINSPEISEART_UEBERSCHUSS,
            Vermarktungsform = DbWerte.PV_VERMARKTUNG_EV,
            Inbetriebnahme = new DateTime(2026, 4, 1),
            KwpOverride = kwpOverride,
            DvEntgelt = 0.40,
            AusfallanteilProzent = 20,
            Par51a_Kompensieren = true
        };

    private IRenderedComponent<PhotovoltaikVerguetungDialog> Aufbauen(
        ProjektPhotovoltaikModel modell,
        double kwpRechnerisch = 30,
        double einspeisungMWh = 0,
        Func<bool>? speichern = null,
        Action<PvVerguetungErgebnis>? geschlossen = null,
        Func<MarktwertImport?>? import = null)
    {
        return Render<PhotovoltaikVerguetungDialog>(p => p
            .Add(x => x.Modell, modell)
            .Add(x => x.KwpRechnerisch, kwpRechnerisch)
            .Add(x => x.EinspeisungMWh, einspeisungMWh)
            .Add(x => x.Katalog, Katalog)
            .Add(x => x.Speichern, speichern ?? (() => true))
            .Add(x => x.MarktwerteImportieren, import)
            .Add(x => x.Geschlossen, geschlossen ?? (_ => { })));
    }

    // =====================================================================
    // Feldbestand
    // =====================================================================

    [Fact]
    public void Der_Feldbestand_der_Karte_steht_vollstaendig()
    {
        var cut = Aufbauen(Satz());

        // Zahlen: kWp-Override, AW-Override, DV, PPA-Preis, PPA-Aufschlag, Ausfall
        Assert.Equal(6, cut.FindAll("input[inputmode=decimal]").Count);
        Assert.Single(cut.FindAll("input[inputmode=numeric]"));   // iMSys-Jahr
        Assert.Single(cut.FindAll("input[type=date]"));           // Inbetriebnahme
        Assert.Equal(3, cut.FindAll("input[type=checkbox]").Count);   // aktiv, § 51a, Bezugsreihe
        Assert.Equal(6, cut.FindAll("input[type=radio]").Count);      // 2 Einspeiseart + 4 Vermarktung
        Assert.Equal(2, cut.FindAll("select").Count);                 // § 51, Kappung
    }

    [Fact]
    public void Die_sieben_Gruppen_stehen_in_der_Reihenfolge_der_Maske()
    {
        var cut = Aufbauen(Satz());
        var titel = cut.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent).ToList();

        Assert.Equal(new[]
        {
            "Anlage", "Anzulegender Wert", "Vermarktung",
            "Vergütungsausfall (§ 51 / § 51a)", "Strompreis / Bezugsbewertung",
            "60-%-Wirkleistungsbegrenzung (§ 9 Abs. 2 EEG)", "Vorschau"
        }, titel);
    }

    [Fact]
    public void Die_Vorbelegung_kommt_aus_dem_geladenen_Satz()
    {
        var cut = Aufbauen(Satz(kwpOverride: 45));

        Assert.Equal("45,00", cut.FindAll("input[inputmode=decimal]")[0].GetAttribute("value"));
        Assert.Equal("2026-04-01", cut.Find("input[type=date]").GetAttribute("value"));
        Assert.True(cut.FindAll("input[type=checkbox]")[0].HasAttribute("checked"));
    }

    // =====================================================================
    // Live-Logik (Aktualisieren)
    // =====================================================================

    [Fact]
    public void Ueber_1_MW_warnt_der_Dialog_wegen_der_Ausschreibung()
    {
        var cut = Aufbauen(Satz(), kwpRechnerisch: 1500);

        Assert.Equal("über 1 MW: Ausschreibung — AW-Override nötig.", cut.Instance.Anlagenwarnung);
        Assert.Contains(cut.FindAll(".epos-warnbanner-text"),
                        e => e.TextContent.StartsWith("über 1 MW"));
    }

    [Fact]
    public void Ueber_100_kW_ist_die_feste_EV_gesperrt_und_die_Wahl_springt_auf_Marktpraemie()
    {
        // N3: Feste EV nur bis 100 kW. Der Vorlaeufer setzte in diesem Fall
        // rbMarktpraemie.Checked = true.
        ProjektPhotovoltaikModel m = Satz();
        var cut = Aufbauen(m, kwpRechnerisch: 250);

        Assert.Contains(0, cut.Instance.GesperrteFormen);
        Assert.Equal(DbWerte.PV_VERMARKTUNG_MARKTPRAEMIE, m.Vermarktungsform);
        Assert.True(cut.FindAll("input[type=radio]")[2].HasAttribute("disabled"));   // "Feste EV"
    }

    [Fact]
    public void Unter_100_kW_bleibt_die_feste_EV_waehlbar()
    {
        ProjektPhotovoltaikModel m = Satz();
        var cut = Aufbauen(m, kwpRechnerisch: 30);

        Assert.Empty(cut.Instance.GesperrteFormen);
        Assert.Equal(DbWerte.PV_VERMARKTUNG_EV, m.Vermarktungsform);
    }

    [Fact]
    public void Die_beiden_PPA_Felder_sind_nur_bei_PPA_bedienbar()
    {
        ProjektPhotovoltaikModel m = Satz();
        var cut = Aufbauen(m);
        var zahlen = cut.FindAll("input[inputmode=decimal]");

        Assert.True(zahlen[2].HasAttribute("disabled"));   // DV-Entgelt (nur Marktpraemie)
        Assert.True(zahlen[3].HasAttribute("disabled"));   // PPA-Festpreis
        Assert.True(zahlen[4].HasAttribute("disabled"));   // PPA-Aufschlag

        cut.FindAll("input[type=radio]")[4].Change(true);  // "Sonstige Direktvermarktung / PPA"

        zahlen = cut.FindAll("input[inputmode=decimal]");
        Assert.True(zahlen[2].HasAttribute("disabled"));
        Assert.False(zahlen[3].HasAttribute("disabled"));
        Assert.False(zahlen[4].HasAttribute("disabled"));
        Assert.Equal(DbWerte.PV_VERMARKTUNG_SONSTIGE_DV, m.Vermarktungsform);
    }

    [Fact]
    public void Der_Paragraf_51_Status_folgt_Stichtag_Leistung_und_iMSys()
    {
        ProjektPhotovoltaikModel alt = Satz();
        alt.Inbetriebnahme = new DateTime(2024, 6, 1);
        Assert.Equal("greift nicht: Inbetriebnahme vor dem 25.02.2025.",
                     Aufbauen(alt).Instance.Par51Status);

        Assert.Equal("greift ab der ersten negativen Viertelstunde.",
                     Aufbauen(Satz(), kwpRechnerisch: 250).Instance.Par51Status);

        ProjektPhotovoltaikModel mitMsys = Satz();
        mitMsys.IMSys_Einbaujahr = 2027;
        Assert.Equal("greift ab 2028 (Folgejahr des iMSys-Einbaus).",
                     Aufbauen(mitMsys).Instance.Par51Status);

        Assert.Equal("greift nicht: Anlage < 100 kW ohne iMSys.",
                     Aufbauen(Satz()).Instance.Par51Status);
    }

    [Fact]
    public void Der_Kappungsstatus_folgt_Wahl_und_Vermarktungsform()
    {
        // AUTO + feste EV + kein iMSys = aktiv.
        Assert.Equal("aktiv: Einspeisung auf 60 % der kWp begrenzt (ohne iMSys).",
                     Aufbauen(Satz()).Instance.KappungStatus);

        ProjektPhotovoltaikModel aus = Satz();
        aus.Kappung60_Anwenden = DbWerte.PV_SCHALTER_NEIN;
        Assert.Equal("abgeschaltet.", Aufbauen(aus).Instance.KappungStatus);

        ProjektPhotovoltaikModel dv = Satz();
        dv.Vermarktungsform = DbWerte.PV_VERMARKTUNG_MARKTPRAEMIE;
        Assert.Equal("greift nicht (Direktvermarktung oder iMSys vorhanden).",
                     Aufbauen(dv).Instance.KappungStatus);
    }

    [Fact]
    public void Ohne_Simulationsergebnis_sagt_die_Vorschau_warum_sie_leer_ist()
    {
        var cut = Aufbauen(Satz());

        Assert.StartsWith("Noch kein Simulationsergebnis", cut.Instance.VorschauText);
    }

    [Fact]
    public void Mit_Einspeisemenge_nennt_die_Vorschau_Menge_Satz_und_Erloes()
    {
        var cut = Aufbauen(Satz(), einspeisungMWh: 12.5);

        Assert.StartsWith("Einspeisung 12,5 MWh/a", cut.Instance.VorschauText);
    }

    // =====================================================================
    // Nullsemantik "0 = keiner"
    // =====================================================================

    [Fact]
    public void Null_in_den_Override_Feldern_heisst_kein_Override()
    {
        ProjektPhotovoltaikModel m = Satz(kwpOverride: 45);
        var cut = Aufbauen(m);

        cut.FindAll("input[inputmode=decimal]")[0].Input("0");
        Assert.Null(m.KwpOverride);

        cut.FindAll("input[inputmode=decimal]")[0].Input("60");
        Assert.Equal(60, m.KwpOverride);
    }

    [Fact]
    public void Ein_iMSys_Jahr_unter_2000_gilt_als_keins()
    {
        ProjektPhotovoltaikModel m = Satz();
        var cut = Aufbauen(m);

        cut.Find("input[inputmode=numeric]").Input("0");
        Assert.Null(m.IMSys_Einbaujahr);

        cut.Find("input[inputmode=numeric]").Input("2027");
        Assert.Equal(2027, m.IMSys_Einbaujahr);
    }

    // =====================================================================
    // Speichern und Sprung
    // =====================================================================

    [Fact]
    public void Uebernehmen_speichert_und_meldet_das_Ergebnis()
    {
        PvVerguetungErgebnis? ergebnis = null;
        int gerufen = 0;
        var cut = Aufbauen(Satz(), speichern: () => { gerufen++; return true; },
                           geschlossen: e => ergebnis = e);

        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(1, gerufen);
        Assert.NotNull(ergebnis);
        Assert.True(ergebnis!.Gespeichert);
        Assert.Equal(PvSprung.Keiner, ergebnis.Sprung);
    }

    [Fact]
    public void Ohne_Inbetriebnahme_haelt_der_aktive_Dialog_an()
    {
        // In WinForms war das eine tote Pruefung - ein DateTimePicker kann nicht
        // leer sein; ein Datumsfeld schon.
        ProjektPhotovoltaikModel m = Satz();
        int gerufen = 0;
        var cut = Aufbauen(m, speichern: () => { gerufen++; return true; });

        cut.Find("input[type=date]").Change("");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(0, gerufen);
        Assert.Equal("Bitte das Inbetriebnahmedatum angeben.",
                     cut.FindAll(".epos-warnbanner-text").Last().TextContent);
    }

    [Fact]
    public void Ein_Speicherfehler_meldet_sich_und_haelt_den_Dialog_offen()
    {
        PvVerguetungErgebnis? ergebnis = null;
        var cut = Aufbauen(Satz(), speichern: () => false, geschlossen: e => ergebnis = e);

        cut.Find(".epos-knopf--primaer").Click();

        Assert.Null(ergebnis);
        Assert.Contains(cut.FindAll(".epos-warnbanner-text"),
                        e => e.TextContent.StartsWith("Die PV-Vergütung konnte nicht"));
    }

    [Fact]
    public void Der_Tarifknopf_meldet_den_Sprung_und_schliesst()
    {
        PvVerguetungErgebnis? ergebnis = null;
        var cut = Aufbauen(Satz(), geschlossen: e => ergebnis = e);

        cut.FindAll("button.epos-sprung").Last().Click();

        Assert.NotNull(ergebnis);
        Assert.Equal(PvSprung.Tarif, ergebnis!.Sprung);
        Assert.False(ergebnis.Gespeichert);
    }

    [Fact]
    public void Ohne_Importdelegat_fehlt_der_Marktwertknopf()
    {
        var cut = Aufbauen(Satz());

        Assert.Single(cut.FindAll("button.epos-sprung"));   // nur der Tarifknopf
    }

    [Fact]
    public void Der_Marktwertimport_meldet_Erfolg_und_Fehler_als_Banner()
    {
        var cut = Aufbauen(Satz(), import: () => new MarktwertImport(true, "18 Zeilen"));
        cut.FindAll("button.epos-sprung")[0].Click();
        Assert.Contains(cut.FindAll(".epos-warnbanner-text"),
                        e => e.TextContent.Contains("18 Zeilen"));

        var cut2 = Aufbauen(Satz(), import: () => new MarktwertImport(false, "Spalte fehlt"));
        cut2.FindAll("button.epos-sprung")[0].Click();
        Assert.Contains(cut2.FindAll(".epos-warnbanner-text"),
                        e => e.TextContent.Contains("Spalte fehlt"));
    }

    [Fact]
    public void Ein_abgebrochener_Dateidialog_sagt_nichts()
    {
        var cut = Aufbauen(Satz(), import: () => null);

        cut.FindAll("button.epos-sprung")[0].Click();

        Assert.Empty(cut.FindAll(".epos-warnbanner"));
    }

    [Fact]
    public void Abbrechen_und_Esc_melden_ohne_zu_speichern()
    {
        PvVerguetungErgebnis? ergebnis = null;
        int gerufen = 0;
        var cut = Aufbauen(Satz(), speichern: () => { gerufen++; return true; },
                           geschlossen: e => ergebnis = e);

        cut.FindAll("button.epos-knopf").First(b => b.TextContent == "Abbrechen").Click();
        Assert.False(ergebnis!.Gespeichert);
        Assert.Equal(0, gerufen);

        ergebnis = null;
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(ergebnis!.Gespeichert);
        Assert.Equal(0, gerufen);
    }

    [Fact]
    public void Enter_bleibt_unbelegt_und_der_Infoknopf_traegt_den_Schluessel()
    {
        PvVerguetungErgebnis? ergebnis = null;
        var cut = Aufbauen(Satz(), geschlossen: e => ergebnis = e);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Null(ergebnis);

        Assert.Single(cut.FindAll(".epos-infoknopf"));
        Assert.Equal("Form_PhotovoltaikVerguetung.btn_Help", cut.Instance.HilfeSchluessel);
    }

    // =====================================================================
    //  Das Formularraster — Anwenderwunsch iU8-E-2 / W14a-E-7, Paket P2
    //  (Windows-Abnahme 05.09.2026)
    // =====================================================================


    /// <summary>
    /// <b>iU8-E-2 / W14a-E-7 (Paket P2):</b> Die fünf Parameterblöcke stehen im
    /// <c>Formularraster</c> — Beschriftung neben dem Feld, Zahlenfelder kurz mit
    /// der Einheit unmittelbar dahinter. Die Herleitungs- und Warnzeilen bleiben
    /// ausserhalb: Sie sind Sätze, keine Felder.
    /// </summary>
    [Fact]
    public void Die_Bloecke_stehen_im_Formularraster()
    {
        var cut = Aufbauen(Satz());

        Assert.True(cut.FindAll(".epos-formularraster").Count >= 4);
        Assert.True(cut.FindAll(".epos-formularraster .epos-feld--kurz").Count > 0);

        // Die Einheit steht IN der Feldzeile des kurzen Feldes (Ausfallanteil "%").
        Assert.Contains(cut.FindAll(".epos-formularraster .epos-feld--kurz"),
                        f => f.QuerySelector(".epos-feld-zeile .epos-einheit") is not null);
    }
}
