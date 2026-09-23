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
/// die Felder wurden aus <c>InitializeComponent</c> von Hand aufgenommen), ohne
/// das Zonenmodell:
///
/// <list type="bullet">
/// <item>Kopf: Schalter „aktiv", Datum „Preisstand" (2)</item>
/// <item>Winterspanne: Winter von/bis Monat (2)</item>
/// <item>Rollenmodell: je Rolle Arbeit, Grund, Modell, Monat und 4×3 Staffel
///       (16), zweimal — dazu Einspeisung Arbeit + Grund (2)</item>
/// </list>
///
/// <para><b>Q11 (E7b, Anwender 22.09.2026: „kein HT/NT").</b> Das Zonenmodell —
/// Modellwahl, HT-Fenster, vier Bezugs- und vier Einspeise-Zonenpreise, die
/// zweistufige Leistungspreis-Staffel — und die Sichten „Strombezug" und
/// „Komplett" sind entfallen. Die Staffel pflegt der Stromträger in der
/// Kostenverwaltung (<see cref="EnergietraegerStaffelTests"/>).</para>
///
/// Zahlen in der Anzeige: <c>de-DE</c> wie in <c>SpeichernLeisteTests</c> —
/// die CI-Läufer laufen englisch.
/// </summary>
public class TarifstrukturDialogTests : EposBunitContext
{
    public TarifstrukturDialogTests()
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // Die Lage der Zahlenfelder in der BHKW-Sicht: je Rolle Arbeit, Grund, Monat
    // und 4×3 Stufenfelder (15), die Einspeisung dahinter.
    private const int BEZUG_ARBEIT = 0;
    private const int BEZUG_STUFE1_GRENZE = 3;
    private const int REST_ARBEIT = 15;
    private const int EINSP_ARBEIT = 30;

    private static TarifParameter Satz()
    {
        var p = new TarifParameter
        {
            IdStamm = 1030,
            Aktiv = true,
            Modus = DbWerte.TARIF_MODUS_ROLLEN,
            WinterVonMonat = 10,
            WinterBisMonat = 3
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
        TarifSicht sicht = TarifSicht.Bhkw,
        Func<bool>? speichern = null,
        Action<bool>? geschlossen = null,
        bool titelAnzeigen = true)
    {
        return Render<TarifstrukturDialog>(p => p
            .Add(x => x.Tarif, satz)
            .Add(x => x.Sicht, sicht)
            .Add(x => x.Speichern, speichern ?? (() => true))
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
        Assert.Equal("Tarifstruktur BHKW (Strom)", cut.Find("h1.epos-dialog-titel").TextContent);
    }

    // =====================================================================
    // Feldbestand
    // =====================================================================

    [Fact]
    public void Die_Bhkw_Sicht_zeigt_den_Feldbestand_des_Rollenmodells()
    {
        var cut = Aufbauen(Satz());

        // 2 Winterspanne (Ganzzahl); 2×15 Rollen + 2 Einspeisung
        Assert.Equal(2, cut.FindAll("input[inputmode=numeric]").Count);
        Assert.Equal(2 * 15 + 2, cut.FindAll("input[inputmode=decimal]").Count);

        Assert.Single(cut.FindAll("input[type=checkbox]"));      // aktiv
        Assert.Single(cut.FindAll("input[type=date]"));          // Preisstand
        Assert.Equal(2, cut.FindAll("select").Count);            // 2 Leistungsmodelle
        Assert.Equal(2, cut.FindAll("button.epos-knopf:not(.epos-dialog-zu)").Count); // Speichern, Abbrechen
    }

    [Fact]
    public void Die_Gruppentitel_stehen_wie_in_der_Maske()
    {
        var cut = Aufbauen(Satz());
        var titel = cut.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent).ToList();

        Assert.Contains("Winterspanne (Sommer- und Wintermaximum des Leistungspreismodells „Staffel“)", titel);
        Assert.Contains("Rollenmodell (Etappe E5) — Differenzmethode „vermiedene Kosten“", titel);
        Assert.Equal(2, titel.Count);
    }

    /// <summary>
    /// Q11 (E7b): Den Zeitzonentarif gibt es nicht mehr — keine Modellwahl, kein
    /// HT-Fenster, keine Zonenpreise, keine zweistufige Staffel im Dialog.
    /// </summary>
    [Fact]
    public void Das_Zonenmodell_steht_nicht_mehr_im_Dialog()
    {
        string markup = Aufbauen(Satz()).Markup;

        Assert.DoesNotContain("Zonenmodell", markup);
        Assert.DoesNotContain("Tarifmodell", markup);
        Assert.DoesNotContain("HT von Stunde", markup);
        Assert.DoesNotContain("Winter HT", markup);
        Assert.DoesNotContain("Sommer NT", markup);

        // Die zweistufige Staffel des Zonenmodells — nicht zu verwechseln mit den
        // vier Leistungsstufen des Rollenmodells, deren Hinweis „Staffelgrenzen" nennt.
        Assert.DoesNotContain("Staffelgrenze [kW]", markup);
        Assert.DoesNotContain("Preis bis Grenze", markup);
        Assert.DoesNotContain("Leistungspreis-Staffel", markup);
    }

    [Fact]
    public void Die_Vorbelegung_kommt_aus_dem_geladenen_Tarifsatz()
    {
        var cut = Aufbauen(Satz());
        var zahlen = cut.FindAll("input[inputmode=decimal]");

        Assert.Equal("0,2500", zahlen[BEZUG_ARBEIT].GetAttribute("value"));
        Assert.Equal("500", zahlen[BEZUG_STUFE1_GRENZE].GetAttribute("value"));
        Assert.Equal("0,2800", zahlen[REST_ARBEIT].GetAttribute("value"));
        Assert.Equal("0,0650", zahlen[EINSP_ARBEIT].GetAttribute("value"));
        Assert.Equal("10", cut.FindAll("input[inputmode=numeric]")[0].GetAttribute("value"));
        Assert.True(cut.FindAll("input[type=checkbox]")[0].HasAttribute("checked"));
    }

    // =====================================================================
    // Sichten (Ae18)
    // =====================================================================

    [Fact]
    public void Die_PV_Sicht_zeigt_nur_die_Einspeisung()
    {
        var cut = Aufbauen(Satz(), TarifSicht.Photovoltaik);

        Assert.Equal(2, cut.FindAll("input[inputmode=decimal]").Count);   // Einspeisung
        Assert.Empty(cut.FindAll("input[inputmode=numeric]"));            // keine Winterspanne
        Assert.Empty(cut.FindAll("select"));                              // keine Bezugsrolle
        Assert.Equal("Tarifstruktur PV-Einspeisung", cut.Find("h1.epos-dialog-titel").TextContent);
    }

    [Fact]
    public void Jede_Sicht_sagt_dass_der_Tarifsatz_geteilt_ist()
    {
        foreach (TarifSicht sicht in Enum.GetValues<TarifSicht>())
        {
            var cut = Aufbauen(Satz(), sicht);
            Assert.Contains(cut.FindAll(".epos-herleitung-text"),
                            e => e.TextContent.StartsWith("Komponentensicht"));
        }
    }

    /// <summary>Q11 (E7b): Es bleiben zwei Sichten — BHKW und Photovoltaik.</summary>
    [Fact]
    public void Es_gibt_nur_die_Sichten_Bhkw_und_Photovoltaik()
    {
        Assert.Equal(new[] { "Bhkw", "Photovoltaik" }, Enum.GetNames<TarifSicht>());
    }

    [Fact]
    public void Eine_Sicht_ueberschreibt_nur_ihre_eigenen_Felder()
    {
        // Ae18: In der PV-Sicht sind Bezugsrollen und Winterspanne gar nicht
        // gebaut - sie muessen den geladenen Wert behalten.
        TarifParameter satz = Satz();
        var cut = Aufbauen(satz, TarifSicht.Photovoltaik);

        cut.FindAll("input[inputmode=decimal]")[0].Input("0,0900");   // Einspeisepreis
        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(0.0900, satz.Einspeisung.ArbeitspreisEurKWh, 4);
        Assert.Equal(0.2500, satz.Bezug.ArbeitspreisEurKWh, 4);    // unberuehrt
        Assert.Equal(0.2800, satz.Reststrom.ArbeitspreisEurKWh, 4); // unberuehrt
        Assert.Equal(10, satz.WinterVonMonat);                      // unberuehrt
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

        cut.FindAll("input[inputmode=decimal]")[BEZUG_ARBEIT].Input("0,4000");
        cut.FindAll("input[inputmode=numeric]")[0].Input("11");
        cut.Find("input[type=date]").Change("2026-01-01");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(1, gerufen);
        Assert.True(ergebnis);
        Assert.Equal(0.4000, satz.Bezug.ArbeitspreisEurKWh, 4);
        Assert.Equal(11, satz.WinterVonMonat);
        Assert.Equal(new DateTime(2026, 1, 1), satz.GueltigAb);
        Assert.Equal(DbWerte.TARIF_MODUS_ROLLEN, satz.Modus);
    }

    /// <summary>
    /// Q11 (E7b): Ein Satz, der noch auf dem Zonenmodell steht (eine Datenbank vor
    /// Schemaschritt 103), wird mit dem ersten Speichern ein Rollentarif — mit den
    /// Rollenpreisen, die der Dialog zeigt. Einen anderen Modus rechnet der Kern
    /// nicht mehr.
    /// </summary>
    [Fact]
    public void Ein_Satz_im_Zonenmodell_wird_beim_Speichern_ein_Rollentarif()
    {
        TarifParameter satz = Satz();
        satz.Modus = DbWerte.TARIF_MODUS_ZONEN;
        Assert.False(satz.Wirksam);

        var cut = Aufbauen(satz);
        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(DbWerte.TARIF_MODUS_ROLLEN, satz.Modus);
        Assert.True(satz.Wirksam);
        Assert.Equal(0.2500, satz.Bezug.ArbeitspreisEurKWh, 4);
    }

    [Fact]
    public void Ein_geleertes_Feld_behaelt_den_geladenen_Wert()
    {
        // Eine NumericUpDown konnte nicht leer sein; ein Eingabefeld schon.
        TarifParameter satz = Satz();
        var cut = Aufbauen(satz);

        cut.FindAll("input[inputmode=decimal]")[BEZUG_ARBEIT].Input("");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(0.2500, satz.Bezug.ArbeitspreisEurKWh, 4);
    }

    [Fact]
    public void Aktiv_ohne_Arbeitspreis_warnt_einmal_und_speichert_dann()
    {
        // Der Vorlaeufer zeigte hier eine MessageBox und speicherte danach.
        TarifParameter satz = Satz();
        int gerufen = 0;
        var cut = Aufbauen(satz, speichern: () => { gerufen++; return true; });

        cut.FindAll("input[inputmode=decimal]")[BEZUG_ARBEIT].Input("0");
        cut.FindAll("input[inputmode=decimal]")[REST_ARBEIT].Input("0");

        cut.Find(".epos-knopf--primaer").Click();
        Assert.Equal(0, gerufen);
        Assert.Contains("weder für den Bezug noch für den Reststrom",
                        cut.Find(".epos-warnbanner-text").TextContent);

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

    /// <summary>Anwenderentscheid 15.09.2026: das Kreuz der Kopfzeile wirkt wie Esc.</summary>
    [Fact]
    public void Kreuz_meldet_false_und_schreibt_nicht()
    {
        TarifParameter satz = Satz();
        int gerufen = 0;
        bool? ergebnis = null;
        var cut = Aufbauen(satz, speichern: () => { gerufen++; return true; },
                           geschlossen: e => ergebnis = e);

        cut.Find(".epos-dialog-zu").Click();

        Assert.False(ergebnis);
        Assert.Equal(0, gerufen);
    }

    /// <summary>Titel-bedingter Kopf: ohne Titel zeigt der Kopf weder Titel noch Kreuz.</summary>
    [Fact]
    public void Ohne_Titel_zeigt_der_Kopf_weder_Titel_noch_Kreuz()
    {
        var cut = Render<TarifstrukturDialog>(p => p
            .Add(x => x.Tarif, Satz())
            .Add(x => x.Sicht, TarifSicht.Bhkw)
            .Add(x => x.Speichern, () => true)
            .Add(x => x.Geschlossen, (bool _) => { })
            .Add(x => x.TitelAnzeigen, false));

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
        // Der Hilfeknopf bleibt - er haengt nicht am Titel.
        Assert.NotEmpty(cut.FindAll(".epos-dialog-kopf"));
    }

    [Fact]
    public void Enter_bleibt_unbelegt()
    {
        // A-7 aus B5b: In einer Maske mit vierzig Zahlenfeldern waere ein
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

        // select[0] = Leistungsmodell der Bezugsrolle (eine Modellwahl gibt es nicht mehr)
        cut.FindAll("select")[0].Change("2");   // Jahreshoechstlast
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

    // =====================================================================
    //  Das Formularraster — Anwenderwunsch iU8-E-2 / W14a-E-7, Paket P2
    //  (Windows-Abnahme 05.09.2026)
    // =====================================================================


    /// <summary>
    /// <b>iU8-E-2 / W14a-E-7 (Paket P2):</b> Die Parameterblöcke stehen im
    /// <c>Formularraster</c> — Beschriftung NEBEN dem Feld, Zahlenfelder kurz mit
    /// der Einheit dahinter. Zuvor nahm jedes Feld die volle Breite und die
    /// Beschriftung stand darüber.
    ///
    /// <para>Die programmatisch gebauten Untergruppen des Vorläufers
    /// (<c>Form_Tarifstruktur.Gruppe</c>) sind jetzt <c>Formulargruppe</c>n: leise
    /// Zwischenüberschriften, deren Felder DIREKTE Rasterkinder bleiben. Der alte
    /// <c>h3.epos-untergruppe</c> steht deshalb nicht mehr im Dialog.</para>
    /// </summary>
    [Fact]
    public void Die_Bloecke_stehen_im_Formularraster()
    {
        var cut = Aufbauen(Satz());

        // Kopf, Winterspanne, Rollenmodell.
        Assert.True(cut.FindAll(".epos-formularraster").Count >= 3);
        Assert.True(cut.FindAll(".epos-formularraster .epos-feld").Count > 0);

        // Die Untergruppen sind Formulargruppen geworden.
        Assert.True(cut.FindAll(".epos-formulargruppe-titel").Count > 0);
        Assert.Empty(cut.FindAll("h3.epos-untergruppe"));
    }

    /// <summary>
    /// Die Preisfelder melden sich als KURZE Felder — daran hängt im Raster die
    /// kurze Breite (Vorbild: 62 px) statt der vollen Feldspalte.
    /// </summary>
    [Fact]
    public void Die_Zahlenfelder_sind_kurze_Felder()
    {
        var cut = Aufbauen(Satz());

        Assert.True(cut.FindAll(".epos-formularraster .epos-feld--kurz").Count > 0);
    }
    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F4)
    // =====================================================================

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Sie bindet über die
    /// Sichtklasse <c>TarifstrukturKiSicht</c> auf die lebenden Eingabefelder; der
    /// <c>TarifParameter</c> des Kerns bleibt bis zum OK unangetastet.
    /// </summary>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_setzt_einen_Rollenpreis()
    {
        TarifParameter satz = Satz();
        var cut = Aufbauen(satz);

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.TARIFSTRUKTUR));

        KiFeldzugang preis = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.TARIFSTRUKTUR, "bezug_arbeitspreis");
        Assert.NotNull(preis);
        Assert.True(preis.Setzbar);

        KiFeldumsetzung neu = KiFeldwandler.Wandle(preis, "0,2850");
        Assert.True(neu.Ok, neu.Grund);
        preis.Setzen(neu.Wert);
        cut.Render();
        Assert.Equal(0.285, Convert.ToDouble(preis.Lesen(), CultureInfo.InvariantCulture), 4);
        Assert.Equal(0.2500, satz.Bezug.ArbeitspreisEurKWh, 4);   // bis zum OK unangetastet

        // Das LEISTUNGSMODELL ist ein Wahlfeld ueber seinen Steuerwert.
        KiFeldzugang modell = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.TARIFSTRUKTUR, "bezug_leistungsmodell");
        Assert.Equal(3, modell.Wahleintraege().Count);

        KiFeldumsetzung jahr = KiFeldwandler.Wandle(modell, modell.Wahleintraege()[2].Text);
        Assert.True(jahr.Ok, jahr.Grund);
        modell.Setzen(jahr.Wert);
        cut.Render();
        Assert.Equal(DbWerte.LEISTUNGSMODELL_JAHRESHOECHSTLAST,
                     Convert.ToString(modell.Lesen(), CultureInfo.InvariantCulture));

        // Q11 (E7b): Modellwahl, HT-Fenster, Zonenpreise und Staffel gibt es nicht mehr.
        var katalog = KiDialoge.Katalog.Finde(KiMaskennamen.TARIFSTRUKTUR)!;
        foreach (string entfallen in new[]
                 { "modell", "hochtarif_von", "hochtarif_bis", "bezug_winter_hoch",
                   "einspeisung_sommer_nieder", "staffel_grenze", "staffel_preis_oben" })
            Assert.Null(katalog.FindeFeld(entfallen));

        // Die vier Leistungsstufen je Rolle sind NICHT deklariert - eine Wertetafel.
        Assert.Null(katalog.FindeFeld("bezug_stufe_1"));
    }
}
