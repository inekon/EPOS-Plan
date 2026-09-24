using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
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
public class PhotovoltaikVerguetungDialogTests : EposBunitContext
{
    public PhotovoltaikVerguetungDialogTests()
    {
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
        Func<Task<MarktwertImport?>>? import = null,
        bool titelAnzeigen = true)
    {
        return Render<PhotovoltaikVerguetungDialog>(p => p
            .Add(x => x.Modell, modell)
            .Add(x => x.KwpRechnerisch, kwpRechnerisch)
            .Add(x => x.EinspeisungMWh, einspeisungMWh)
            .Add(x => x.Katalog, Katalog)
            .Add(x => x.Speichern, speichern ?? (() => true))
            .Add(x => x.MarktwerteImportieren, import)
            .Add(x => x.TitelAnzeigen, titelAnzeigen)
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
    //  KONZEPT § 2.16 — die Herkunft der Verguetung (U38)
    // =====================================================================

    /// <summary>
    /// Bei einer ÜBERNOMMENEN Vergütung nennt die Hinweiszeile die Herkunft, und der
    /// eine Weg heraus steht als Knopf „eigene Werte" da (VV‑Q3/VV‑Q6). Die Wahl
    /// selbst trifft der Reiter Ertrag/Bonus — dieser Dialog bietet sie nicht an.
    /// </summary>
    [Fact]
    public void Bei_Uebernahme_stehen_Hinweiszeile_und_Knopf_eigene_Werte()
    {
        int gerufen = 0;
        var cut = Render<PhotovoltaikVerguetungDialog>(p => p
            .Add(x => x.Modell, Satz())
            .Add(x => x.Katalog, Katalog)
            .Add(x => x.Speichern, () => true)
            .Add(x => x.Uebernommen, true)
            .Add(x => x.HerkunftText, "Vergütung dieser Variante: übernommen vom Stammprojekt")
            .Add(x => x.EigeneWerte, () => gerufen++)
            .Add(x => x.Geschlossen, _ => { }));

        Assert.Contains("übernommen vom Stammprojekt", cut.Markup);

        var knopf = cut.FindAll("button.epos-knopf")
                       .First(b => b.TextContent.Contains("eigene Werte"));
        knopf.Click();

        Assert.Equal(1, gerufen);
    }

    /// <summary>
    /// Bei EIGENEN Werten steht die Hinweiszeile ohne den Knopf — es gibt nichts zu
    /// lösen. Ohne Delegat bleibt er ebenfalls weg (ein Knopf, der nichts tut, ist
    /// eine Behauptung, die nicht stimmt).
    /// </summary>
    [Fact]
    public void Bei_eigenen_Werten_fehlt_der_Knopf_eigene_Werte()
    {
        var cut = Render<PhotovoltaikVerguetungDialog>(p => p
            .Add(x => x.Modell, Satz())
            .Add(x => x.Katalog, Katalog)
            .Add(x => x.Speichern, () => true)
            .Add(x => x.Uebernommen, false)
            .Add(x => x.HerkunftText, "Vergütung dieser Variante: eigene Werte")
            .Add(x => x.Geschlossen, _ => { }));

        Assert.Contains("eigene Werte", cut.Markup);
        Assert.DoesNotContain(cut.FindAll("button.epos-knopf"),
                              b => b.TextContent.Trim() == "eigene Werte");
    }

    // =====================================================================
    // Feldbestand
    // =====================================================================

    [Fact]
    public void Der_Feldbestand_der_Karte_steht_vollstaendig()
    {
        var cut = Aufbauen(Satz());

        // Zahlen: kWp-Override, Degradation (Paket B, Merge 5), AW-Override, DV, PPA-Preis,
        // PPA-Aufschlag, Ausfall
        Assert.Equal(7, cut.FindAll("input[inputmode=decimal]").Count);
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

        // U36: Die zweite Grenze ist noch nicht erreicht — eine Warnung, nicht zwei.
        Assert.Single(cut.Instance.Anlagenwarnungen);
        Assert.DoesNotContain(cut.FindAll(".epos-warnbanner-text"),
                              e => e.TextContent.StartsWith("über 2 MW"));
    }

    /// <summary>
    /// U36: Über 2 MW stehen BEIDE Warnungen. Sie hingen als <c>?:</c>-Kette
    /// aneinander — über 1 MW die Ausschreibung, SONST über 2 MW die Stromsteuer —,
    /// und der zweite Zweig konnte nie greifen: Jede Anlage über 2 MW ist auch über
    /// 1 MW. Es sind zwei unabhängige Schwellen aus zwei Gesetzen.
    /// </summary>
    [Fact]
    public void Ueber_2_MW_stehen_beide_Warnungen_nebeneinander()
    {
        var cut = Aufbauen(Satz(), kwpRechnerisch: 2500);

        Assert.Equal(2, cut.Instance.Anlagenwarnungen.Count);
        Assert.Contains(cut.FindAll(".epos-warnbanner-text"),
                        e => e.TextContent.StartsWith("über 1 MW"));
        Assert.Contains(cut.FindAll(".epos-warnbanner-text"),
                        e => e.TextContent.StartsWith("über 2 MW"));
    }

    /// <summary>Gegenprobe: Unter 1 MW warnt der Dialog gar nicht.</summary>
    [Fact]
    public void Unter_1_MW_steht_keine_Anlagenwarnung()
    {
        var cut = Aufbauen(Satz(), kwpRechnerisch: 900);

        Assert.Empty(cut.Instance.Anlagenwarnungen);
        Assert.Equal("", cut.Instance.Anlagenwarnung);
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

        // Seit Merge 5 steht die Degradation als zweites Zahlenfeld in der Anlage-Gruppe;
        // die drei Vermarktungsfelder ruecken um eins.
        Assert.True(zahlen[3].HasAttribute("disabled"));   // DV-Entgelt (nur Marktpraemie)
        Assert.True(zahlen[4].HasAttribute("disabled"));   // PPA-Festpreis
        Assert.True(zahlen[5].HasAttribute("disabled"));   // PPA-Aufschlag

        cut.FindAll("input[type=radio]")[4].Change(true);  // "Sonstige Direktvermarktung / PPA"

        zahlen = cut.FindAll("input[inputmode=decimal]");
        Assert.True(zahlen[3].HasAttribute("disabled"));
        Assert.False(zahlen[4].HasAttribute("disabled"));
        Assert.False(zahlen[5].HasAttribute("disabled"));
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
        // E3/2: Die Huelle waehlt ueber Dienste.Datei und antwortet deshalb
        // asynchron; der Pruefstand wartet auf den Zeichenlauf.
        var cut = Aufbauen(Satz(),
            import: () => Task.FromResult<MarktwertImport?>(new MarktwertImport(true, "18 Zeilen")));
        cut.FindAll("button.epos-sprung")[0].Click();
        cut.WaitForAssertion(() =>
            Assert.Contains(cut.FindAll(".epos-warnbanner-text"),
                            e => e.TextContent.Contains("18 Zeilen")));

        var cut2 = Aufbauen(Satz(),
            import: () => Task.FromResult<MarktwertImport?>(new MarktwertImport(false, "Spalte fehlt")));
        cut2.FindAll("button.epos-sprung")[0].Click();
        cut2.WaitForAssertion(() =>
            Assert.Contains(cut2.FindAll(".epos-warnbanner-text"),
                            e => e.TextContent.Contains("Spalte fehlt")));
    }

    [Fact]
    public void Ein_abgebrochener_Dateidialog_sagt_nichts()
    {
        var cut = Aufbauen(Satz(), import: () => Task.FromResult<MarktwertImport?>(null));

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

    /// <summary>Anwenderentscheid 15.09.2026: das Kreuz der Kopfzeile wirkt wie Esc.</summary>
    [Fact]
    public void Kreuz_meldet_ohne_zu_speichern()
    {
        PvVerguetungErgebnis? ergebnis = null;
        int gerufen = 0;
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
        var cut = Render<PhotovoltaikVerguetungDialog>(p => p
            .Add(x => x.Modell, Satz())
            .Add(x => x.KwpRechnerisch, 30)
            .Add(x => x.Katalog, Katalog)
            .Add(x => x.Speichern, () => true)
            .Add(x => x.Geschlossen, (PvVerguetungErgebnis _) => { })
            .Add(x => x.TitelAnzeigen, false));

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
        // Der Hilfeknopf bleibt - er haengt nicht am Titel.
        Assert.NotEmpty(cut.FindAll(".epos-dialog-kopf"));
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
    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F4)
    // =====================================================================

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Die Sichtklasse
    /// <c>PhotovoltaikVerguetungKiSicht</c> setzt über die WEGE der Maske: Eine 0
    /// heißt „nicht gepflegt", und die Zulässigkeitsprüfung läuft mit.
    /// </summary>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_setzt_ueber_ihre_Wege()
    {
        var modell = Satz();
        var cut = Aufbauen(modell);

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.PV_VERGUETUNG));

        KiFeldzugang leistung =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.PV_VERGUETUNG, "leistung");
        Assert.NotNull(leistung);
        Assert.True(leistung.Setzbar);

        KiFeldumsetzung neu = KiFeldwandler.Wandle(leistung, "42");
        Assert.True(neu.Ok, neu.Grund);
        leistung.Setzen(neu.Wert);
        cut.Render();
        Assert.Equal(42, modell.KwpOverride!.Value, 3);

        // Eine 0 heisst "nicht gepflegt" - der Weg der Maske fuehrt sie auf null.
        KiFeldumsetzung null_ = KiFeldwandler.Wandle(leistung, "0");
        Assert.True(null_.Ok, null_.Grund);
        leistung.Setzen(null_.Wert);
        cut.Render();
        Assert.Null(modell.KwpOverride);

        // Die VERMARKTUNGSFORM ist ein Wahlfeld ueber ihren Steuerwert.
        KiFeldzugang form =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.PV_VERGUETUNG, "vermarktungsform");
        Assert.Equal(4, form.Wahleintraege().Count);
    }

    // =====================================================================
    //  ETAPPE E9b — die ±-Knöpfe an DV-Entgelt und PPA-Preis (E9b‑Q1, E9a‑Q7)
    // =====================================================================

    private static AngleSharp.Dom.IElement Knopf(IRenderedComponent<PhotovoltaikVerguetungDialog> cut, string text)
        => cut.FindAll("button.epos-szenarioknopf")
              .First(b => b.QuerySelector(".epos-szenarioknopf-text")!.TextContent == text);

    /// <summary>
    /// Die zwei Knöpfe folgen der Sperre ihres Feldes: das DV-Entgelt nur bei der
    /// Marktprämie, der PPA-Preis nur bei der sonstigen Direktvermarktung.
    /// </summary>
    [Fact]
    public void Die_Knoepfe_an_DV_Entgelt_und_PPA_Preis_folgen_der_Vermarktungsform()
    {
        var ev = Aufbauen(Satz());                               // feste EV
        Assert.True(Knopf(ev, "DV-Entgelt").HasAttribute("disabled"));
        Assert.True(Knopf(ev, "PPA-Preis").HasAttribute("disabled"));

        ProjektPhotovoltaikModel mp = Satz();
        mp.Vermarktungsform = DbWerte.PV_VERMARKTUNG_MARKTPRAEMIE;
        var markt = Aufbauen(mp);
        Assert.False(Knopf(markt, "DV-Entgelt").HasAttribute("disabled"));
        Assert.True(Knopf(markt, "PPA-Preis").HasAttribute("disabled"));

        markt.FindAll("input[type=radio]")[4].Change(true);     // "Sonstige Direktvermarktung / PPA"
        Assert.True(Knopf(markt, "DV-Entgelt").HasAttribute("disabled"));
        Assert.False(Knopf(markt, "PPA-Preis").HasAttribute("disabled"));
    }

    /// <summary>
    /// Der Knopf öffnet das Szenariopaar in einer Überlagerung (ct/kWh, zwei Stellen, ohne
    /// Warnung); OK legt es auf das Modell, geschrieben wird mit „Übernehmen".
    /// </summary>
    [Fact]
    public void Der_DV_Knopf_pflegt_das_Paar_am_Modell_und_Uebernehmen_schreibt_es()
    {
        ProjektPhotovoltaikModel m = Satz();                     // DV-Entgelt 0,40 ct/kWh
        m.Vermarktungsform = DbWerte.PV_VERMARKTUNG_MARKTPRAEMIE;
        int gerufen = 0;
        var cut = Aufbauen(m, speichern: () => { gerufen++; return true; });

        Knopf(cut, "DV-Entgelt").Click();

        Assert.True(cut.Instance.SzenarioOffen);
        Assert.Equal("Szenariowerte — DV-Entgelt", cut.Find(".epos-ueberlagerung-titel").TextContent.Trim());
        Assert.Contains(cut.FindAll(".epos-ueberlagerung .epos-herleitung-text"),
                        e => e.TextContent == "Erwartet: 0,40 ct/kWh");
        Assert.Empty(cut.FindAll(".epos-ueberlagerung .epos-warnbanner"));

        cut.Find(".epos-ueberlagerung").QuerySelectorAll("input[inputmode=decimal]")[0].Input("0,30");
        cut.Find(".epos-ueberlagerung").QuerySelectorAll("input[inputmode=decimal]")[1].Input("0,60");
        cut.Find(".epos-ueberlagerung .epos-knopf--primaer").Click();

        Assert.False(cut.Instance.SzenarioOffen);
        Assert.Equal(0.30, m.DvEntgeltBest);
        Assert.Equal(0.60, m.DvEntgeltWorst);
        Assert.Null(m.PpaPreisBest);
        Assert.True(cut.Instance.DvSzenarioGepflegt);
        Assert.Contains("epos-szenarioknopf--gepflegt", Knopf(cut, "DV-Entgelt").ClassName);
        Assert.Equal("Szenariowerte gepflegt — Best 0,30 ct/kWh · Worst 0,60 ct/kWh",
                     Knopf(cut, "DV-Entgelt").GetAttribute("title"));
        Assert.Empty(cut.Instance.PvSzenarioZeilen);             // es wirkt: Marktprämie
        Assert.Equal(0, gerufen);

        cut.Find(".epos-knopf--primaer").Click();                // Übernehmen
        Assert.Equal(1, gerufen);
        Assert.Equal(0.30, m.DvEntgeltBest);
    }

    /// <summary>
    /// E9a‑Q7: Ein gepflegtes Paar, das der Lauf nicht liest, nennt seine Kohärenzzeile —
    /// die Vermarktungsform liest den Satz nicht, oder die Vergütung ist gar nicht
    /// angewendet. Sie sperren nichts.
    /// </summary>
    [Fact]
    public void Ein_gepflegtes_Paar_ohne_Wirkung_nennt_seine_Kohaerenzzeile()
    {
        ProjektPhotovoltaikModel m = Satz();                     // feste EV
        m.DvEntgeltBest = 0.30;
        var cut = Aufbauen(m);
        Assert.Equal(new[] { "DV-Entgelt je Szenario ohne Wirkung: Es gilt nur für die Direktvermarktung mit Marktprämie." },
                     cut.FindAll(".epos-kohaerenz-text").Select(e => e.TextContent)
                        .Where(t => t.Contains("je Szenario")).ToArray());

        ProjektPhotovoltaikModel ppa = Satz();
        ppa.Vermarktungsform = DbWerte.PV_VERMARKTUNG_MARKTPRAEMIE;
        ppa.PpaPreis = 8;
        ppa.PpaPreisWorst = 6;
        Assert.Equal(new[] { "PPA-Preis je Szenario ohne Wirkung: Er gilt nur für die sonstige Direktvermarktung (PPA)." },
                     Aufbauen(ppa).Instance.PvSzenarioZeilen);

        ProjektPhotovoltaikModel aus = Satz();
        aus.Aktiv = false;
        aus.DvEntgeltWorst = 0.5;
        Assert.Equal(new[] { "DV-Entgelt und PPA-Preis je Szenario ohne Wirkung: Die Vergütung wird nicht "
                             + "angewendet (Schalter „Vergütung anwenden“)." },
                     Aufbauen(aus).Instance.PvSzenarioZeilen);

        // Ohne gepflegtes Paar keine Zeile.
        Assert.Empty(Aufbauen(Satz()).Instance.PvSzenarioZeilen);
    }

    /// <summary>Esc gehört der obersten Ebene: erst das Paar, dann der Dialog.</summary>
    [Fact]
    public void Esc_schliesst_nur_die_Ueberlagerung_des_PV_Paars()
    {
        ProjektPhotovoltaikModel m = Satz();
        m.Vermarktungsform = DbWerte.PV_VERMARKTUNG_MARKTPRAEMIE;
        PvVerguetungErgebnis? ergebnis = null;
        var cut = Aufbauen(m, geschlossen: e => ergebnis = e);

        Knopf(cut, "DV-Entgelt").Click();
        cut.Find(".epos-ueberlagerung").QuerySelectorAll("input[inputmode=decimal]")[0].Input("0,30");

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.True(cut.Instance.SzenarioOffen);
        Assert.Null(ergebnis);

        cut.Find(".epos-ueberlagerung").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(cut.Instance.SzenarioOffen);
        Assert.Null(ergebnis);
        Assert.Null(m.DvEntgeltBest);
    }
}
