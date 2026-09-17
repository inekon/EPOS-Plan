using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Kosten;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die beiden Preisblöcke des Energieträgers: <c>StrompreisDetails</c>
/// („Strompreis Details", Anwenderentscheide SP-E-2/SP-E-3) und
/// <c>BrennstoffBestandteile</c> (Konzept BHKW § 4.1).
///
/// <para>Beide ZERLEGEN denselben Preis, statt ihn zu erhöhen. Soll beim Strom
/// ist der Anwenderwortlaut vom 16.09.2026: ein einklappbarer Bereich, drei
/// Gruppen, die Umlagen wahlweise als Summe oder einzeln, kein
/// Gesamtaufschlagsfeld, eine Kohärenzzeile gegen den Arbeitspreis und ein
/// Knopf „In Arbeitspreis übernehmen".</para>
/// </summary>
public class PreisbloeckeTests : EposBunitContext
{
    // Der Arbeitspreis erscheint als Zahl in der Anzeige; die CI-Läufer laufen
    // englisch. Dieselbe Klemmung wie in SpeichernLeisteTests.

    public PreisbloeckeTests()
    {
    }

    // =====================================================================
    // Strompreis Details
    // =====================================================================

    private static StrompreisDetailsStand StromStand() => new StrompreisDetailsStand
    {
        Beschaffung = 26.254, BeschaffungAktiv = true,
        Vertrieb = 2.0, VertriebAktiv = true,
        Netzentgelt = 7.5, NetzentgeltAktiv = true,
        Stromsteuer = 2.05, StromsteuerAktiv = true,
        Konzession = 1.32, KonzessionAktiv = true,
        Umlagen = 1.2, UmlagenAktiv = true
    };

    /// <summary>Klappt den Block auf — Vorgabe ist ZU.</summary>
    private static void Aufklappen(IRenderedComponent<StrompreisDetails> cut)
        => cut.Find(".epos-modulparameter-knopf").Click();

    [Fact]
    public void Der_Block_ist_zugeklappt_und_nennt_die_Summe_schon_im_Kopf()
    {
        var cut = Render<StrompreisDetails>(p => p
            .Add(x => x.Stand, StromStand())
            .Add(x => x.Anzeige, new PreisblockAnzeige("Summe der Anteile: 40,32 ct/kWh", "", false)));

        Assert.False(cut.Instance.Offen);
        Assert.Equal("false", cut.Find(".epos-modulparameter-knopf").GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll(".epos-preiszeile"));
        Assert.Contains("Summe der Anteile: 40,32 ct/kWh", cut.Markup);

        // SP-E-5 (a): Zugeklappt steht KEIN Eingabefeld mehr da - die beiden
        // Vergütungsfelder, die früher ausserhalb des Blocks standen, sind fort.
        Assert.Empty(cut.FindAll("input[type=text]"));
    }

    [Fact]
    public void Aufgeklappt_stehen_drei_Gruppen_und_sechs_Anteile()
    {
        var cut = Render<StrompreisDetails>(p => p.Add(x => x.Stand, StromStand()));
        Aufklappen(cut);

        Assert.True(cut.Instance.Offen);
        Assert.Equal("true", cut.Find(".epos-modulparameter-knopf").GetAttribute("aria-expanded"));
        Assert.Equal(3, cut.FindAll(".epos-untergruppe").Count);
        Assert.Equal(3, cut.FindAll(".epos-preisblock").Count);

        // Beschaffung, Vertrieb, Netz, Stromsteuer, Konzession, Umlagen.
        Assert.Equal(6, cut.FindAll(".epos-preiszeile").Count);
        // KEIN Gesamtaufschlagsfeld und KEINE Vergütung mehr: die 6 Anteile.
        Assert.Equal(6, cut.FindAll("input[type=text]").Count);
    }

    [Fact]
    public void Die_Beschriftungen_stehen_wie_im_Anwenderwortlaut()
    {
        var cut = Render<StrompreisDetails>(p => p.Add(x => x.Stand, StromStand()));
        Aufklappen(cut);

        foreach (string text in new[] { "Beschaffung und Vertrieb", "Netzentgelte",
                                        "Steuern, Abgaben und Umlagen",
                                        "Beschaffung", "Vertrieb", "Arbeitspreis Netz",
                                        "Stromsteuer", "Konzessionsabgabe", "Umlagen (Summe)",
                                        "Umlagen aufschlüsseln" })
            Assert.Contains(text, cut.Markup);
    }

    [Fact]
    public void Die_englischen_Beschriftungen_kommen_als_Gabe_herein()
    {
        var cut = Render<StrompreisDetails>(p => p
            .Add(x => x.Stand, StromStand())
            .Add(x => x.TitelDetails, "Electricity price details")
            .Add(x => x.GruppeBeschaffung, "Procurement and sales")
            .Add(x => x.GruppeNetz, "Grid charges")
            .Add(x => x.GruppeSteuern, "Taxes, duties and levies")
            .Add(x => x.LabelBeschaffung, "Procurement")
            .Add(x => x.LabelUmlagenEinzeln, "Itemise levies")
            .Add(x => x.InArbeitspreisText, "Apply to working price"));
        Aufklappen(cut);

        foreach (string text in new[] { "Electricity price details", "Procurement and sales",
                                        "Grid charges", "Taxes, duties and levies",
                                        "Procurement", "Itemise levies",
                                        "Apply to working price" })
            Assert.Contains(text, cut.Markup);
    }

    /// <summary>
    /// Die Umlagen stehen EINMAL: entweder das Summenfeld oder die drei
    /// Einzelposten. Der Schalter tauscht sie aus, statt sie nebeneinander zu
    /// stellen.
    /// </summary>
    [Fact]
    public void Der_Umlagenschalter_tauscht_Summenfeld_gegen_drei_Einzelposten()
    {
        var stand = StromStand();
        var cut = Render<StrompreisDetails>(p => p.Add(x => x.Stand, stand));
        Aufklappen(cut);

        Assert.Contains("Umlagen (Summe)", cut.Markup);
        Assert.DoesNotContain("KWKG-Umlage", cut.Markup);

        // Der letzte Schalter des Blocks ist „Umlagen aufschlüsseln".
        var schalter = cut.FindAll("input[type=checkbox]");
        schalter[schalter.Count - 1].Change(true);

        cut.WaitForAssertion(() => Assert.True(stand.UmlagenEinzeln));
        Assert.Contains("KWKG-Umlage", cut.Markup);
        Assert.Contains("Offshore-Netzumlage", cut.Markup);
        Assert.Contains("§ 19 StromNEV-Umlage", cut.Markup);
        Assert.DoesNotContain("Umlagen (Summe)", cut.Markup);
        Assert.Equal(8, cut.FindAll(".epos-preiszeile").Count);
    }

    [Fact]
    public void Ein_geaenderter_Wert_landet_im_Stand_und_wird_gemeldet()
    {
        var stand = StromStand();
        int gemeldet = 0;
        var cut = Render<StrompreisDetails>(p => p
            .Add(x => x.Stand, stand)
            .Add(x => x.Geaendert, () => gemeldet++));
        Aufklappen(cut);

        cut.FindAll(".epos-preiszeile input[type=text]")[0].Input("30,5");

        Assert.Equal(30.5, stand.Beschaffung);
        Assert.Equal(1, gemeldet);
    }

    [Fact]
    public void Die_Stromsteuer_Schnellwahl_traegt_ein_und_schaltet_aktiv()
    {
        var stand = StromStand();
        stand.Stromsteuer = 0;
        stand.StromsteuerAktiv = false;
        var cut = Render<StrompreisDetails>(p => p
            .Add(x => x.Stand, stand)
            .Add(x => x.SatzRegelfall, new Schnellwahlsatz("2,05", "Katalog: 20,5 €/MWh", 2.05))
            .Add(x => x.SatzReduziert, new Schnellwahlsatz("0,05", "Rückfallebene", 0.05, true)));
        Aufklappen(cut);

        cut.FindAll(".epos-schnellwahl-knopf")[0].Click();

        Assert.Equal(2.05, stand.Stromsteuer);
        Assert.True(stand.StromsteuerAktiv);
    }

    [Fact]
    public void Der_empfohlene_Satz_steht_hervorgehoben_und_nennt_seine_Herkunft()
    {
        var cut = Render<StrompreisDetails>(p => p
            .Add(x => x.Stand, StromStand())
            .Add(x => x.SatzRegelfall, new Schnellwahlsatz("2,05", "Katalog: 20,5 €/MWh", 2.05))
            .Add(x => x.SatzReduziert, new Schnellwahlsatz("0,05", "Rückfallebene 0,05", 0.05, true)));
        Aufklappen(cut);

        var knoepfe = cut.FindAll(".epos-schnellwahl-knopf");
        Assert.DoesNotContain("--empfohlen", knoepfe[0].ClassName);
        Assert.Contains("--empfohlen", knoepfe[1].ClassName);
        Assert.Equal("Rückfallebene 0,05", knoepfe[1].GetAttribute("title"));
    }

    /// <summary>
    /// Der Rest-Vorschlag für die Beschaffung steht als KNOPF da und schreibt
    /// erst auf Klick — eine still eingetragene Zahl wäre eine Behauptung über
    /// den Beschaffungspreis, die niemand aufgestellt hat.
    /// </summary>
    [Fact]
    public void Der_Rest_Vorschlag_schreibt_die_Beschaffung_erst_auf_Klick()
    {
        var stand = StromStand();
        stand.Beschaffung = 0;
        stand.BeschaffungAktiv = false;

        var cut = Render<StrompreisDetails>(p => p
            .Add(x => x.Stand, stand)
            .Add(x => x.BeschaffungVorschlag, 26.254));
        Aufklappen(cut);

        Assert.Equal(0.0, stand.Beschaffung);
        var knopf = cut.FindAll(".epos-schnellwahl-knopf")[0];
        Assert.Contains("26,254", knopf.TextContent);

        knopf.Click();

        Assert.Equal(26.254, stand.Beschaffung);
        Assert.True(stand.BeschaffungAktiv);
    }

    [Fact]
    public void Ohne_Vorschlag_steht_kein_Restknopf()
    {
        var cut = Render<StrompreisDetails>(p => p.Add(x => x.Stand, StromStand()));
        Aufklappen(cut);

        Assert.Empty(cut.FindAll(".epos-schnellwahl-knopf"));
    }

    [Fact]
    public void Summe_Kohaerenzzeile_und_Rest_kommen_fertig_aus_der_Huelle()
    {
        var cut = Render<StrompreisDetails>(p => p
            .Add(x => x.Stand, StromStand())
            .Add(x => x.ArbeitspreisCtKwh, 38.0)
            .Add(x => x.Anzeige, new PreisblockAnzeige(
                "Summe der Anteile: 40,32 ct/kWh",
                "Nicht aufgeschlüsselter Rest: -2,32 ct/kWh", true)));
        Aufklappen(cut);

        Assert.Equal("Summe der Anteile: 40,32 ct/kWh",
                     cut.Find(".epos-preisblock-summe").TextContent);
        Assert.Contains("38", cut.Find(".epos-kohaerenz-text").TextContent);
        Assert.Equal("Nicht aufgeschlüsselter Rest: -2,32 ct/kWh",
                     cut.Find(".epos-preisblock-rest").TextContent);
        Assert.Contains("--negativ", cut.Find(".epos-preisblock-rest").ClassName);
    }

    /// <summary>
    /// Der Knopf MELDET nur — eingetragen wird der Wert vom Wirt.
    /// <b>Gegenprobe:</b> Ohne angehängten Rückruf meldet er nichts, und der
    /// Fall unten fiele durch.
    /// </summary>
    [Fact]
    public void Der_Uebernahmeknopf_meldet_und_schreibt_nichts_selbst()
    {
        var stand = StromStand();
        int gerufen = 0;
        var cut = Render<StrompreisDetails>(p => p
            .Add(x => x.Stand, stand)
            .Add(x => x.InArbeitspreis, () => gerufen++));
        Aufklappen(cut);

        cut.FindAll("button.epos-knopf")[^1].Click();

        Assert.Equal(1, gerufen);
        Assert.Equal(26.254, stand.Beschaffung);   // der Block schreibt keinen Preis
    }

    // =====================================================================
    // Brennstoff-Bestandteile
    // =====================================================================

    private static BrennstoffBestandteileStand BrennStand() => new BrennstoffBestandteileStand
    {
        Aufgeschluesselt = false,
        Energiesteuer = 0.55, EnergiesteuerAktiv = true,
        CO2 = 1.1, CO2Aktiv = true,
        Netzentgelt = 1.4, NetzentgeltAktiv = false,
        Vertrieb = null, VertriebAktiv = false
    };

    [Fact]
    public void Der_Brennstoff_Block_zeigt_vier_Komponenten_und_vier_Schnellwahlknoepfe()
    {
        var cut = Render<BrennstoffBestandteile>(p => p
            .Add(x => x.Stand, BrennStand())
            .Add(x => x.SatzRegel, new Schnellwahlsatz("§ 2: 0,55", "Katalog", 0.55))
            .Add(x => x.Satz53a, new Schnellwahlsatz("§ 53a: 0,45", "Katalog", 0.45))
            .Add(x => x.Satz54, new Schnellwahlsatz("§ 54: —", "kein Satz", null))
            .Add(x => x.SatzCo2, new Schnellwahlsatz("BEHG: 1,10", "55 €/t × 201 g/kWh", 1.1)));

        Assert.Equal(4, cut.FindAll(".epos-preiszeile").Count);
        Assert.Equal(4, cut.FindAll(".epos-schnellwahl-knopf").Count);
    }

    [Fact]
    public void Ein_nicht_belegbarer_Satz_sperrt_seinen_Knopf_und_nennt_den_Grund()
    {
        var cut = Render<BrennstoffBestandteile>(p => p
            .Add(x => x.Stand, BrennStand())
            .Add(x => x.Satz54, new Schnellwahlsatz("§ 54: —",
                "Diesem Energieträger ist im Katalog kein Energiesteuersatz zugeordnet.", null)));

        var knopf = cut.Find(".epos-schnellwahl-knopf");
        Assert.True(knopf.HasAttribute("disabled"));
        Assert.Contains("kein Energiesteuersatz zugeordnet", knopf.GetAttribute("title"));
    }

    [Fact]
    public void Ein_leeres_Feld_heisst_kein_Anteil_und_bleibt_leer()
    {
        var stand = BrennStand();
        var cut = Render<BrennstoffBestandteile>(p => p.Add(x => x.Stand, stand));

        Assert.Equal("", cut.FindAll(".epos-preiszeile input[type=text]")[3].GetAttribute("value"));

        cut.FindAll(".epos-preiszeile input[type=text]")[3].Input("");
        Assert.Null(stand.Vertrieb);
    }

    [Fact]
    public void Die_Schnellwahl_traegt_ein_schaltet_aktiv_und_nennt_die_Herkunft()
    {
        var stand = BrennStand();
        stand.Energiesteuer = null;
        stand.EnergiesteuerAktiv = false;
        var cut = Render<BrennstoffBestandteile>(p => p
            .Add(x => x.Stand, stand)
            .Add(x => x.SatzRegel, new Schnellwahlsatz("§ 2: 0,55",
                "5,5 €/MWh (ab 2024, EnergieStG)", 0.55)));

        cut.Find(".epos-schnellwahl-knopf").Click();

        Assert.Equal(0.55, stand.Energiesteuer);
        Assert.True(stand.EnergiesteuerAktiv);
        Assert.Equal("5,5 €/MWh (ab 2024, EnergieStG)", cut.Instance.Quelle);
    }

    [Fact]
    public void Der_CO2_Knopf_schaltet_die_CO2_Zeile_aktiv()
    {
        var stand = BrennStand();
        stand.CO2 = null;
        stand.CO2Aktiv = false;
        stand.EnergiesteuerAktiv = false;
        var cut = Render<BrennstoffBestandteile>(p => p
            .Add(x => x.Stand, stand)
            .Add(x => x.SatzCo2, new Schnellwahlsatz("BEHG: 1,10", "55 €/t × 201 g/kWh", 1.1)));

        cut.Find(".epos-schnellwahl-knopf").Click();

        Assert.Equal(1.1, stand.CO2);
        Assert.True(stand.CO2Aktiv);
        Assert.False(stand.EnergiesteuerAktiv);
    }

    [Fact]
    public void In_Arbeitspreis_uebernehmen_geht_nur_im_aufgeschluesselten_Modus()
    {
        var stand = BrennStand();
        var cut = Render<BrennstoffBestandteile>(p => p.Add(x => x.Stand, stand));

        var knopf = cut.FindAll("button")[^1];
        Assert.True(knopf.HasAttribute("disabled"));

        cut.FindAll(".epos-optionsgruppe input[type=radio]")[1].Change(true);
        Assert.False(cut.FindAll("button")[^1].HasAttribute("disabled"));
    }

    [Fact]
    public void Der_Knopf_meldet_nur_und_schreibt_nichts()
    {
        int gemeldet = 0;
        var stand = BrennStand();
        stand.Aufgeschluesselt = true;
        var cut = Render<BrennstoffBestandteile>(p => p
            .Add(x => x.Stand, stand)
            .Add(x => x.InArbeitspreis, () => gemeldet++));

        cut.FindAll("button")[^1].Click();

        Assert.Equal(1, gemeldet);
    }

    [Fact]
    public void Der_Arbeitspreis_steht_als_Bezugsgroesse_darunter()
    {
        var cut = Render<BrennstoffBestandteile>(p => p
            .Add(x => x.Stand, BrennStand())
            .Add(x => x.ArbeitspreisCtKwh, 6.44)
            .Add(x => x.LabelArbeitspreis, "Arbeitspreis (Trägerdialog)"));

        Assert.Contains("Arbeitspreis (Trägerdialog): 6,44 ct/kWh", cut.Markup);
    }

    [Fact]
    public void In_beiden_Modi_bleiben_die_Komponentenfelder_schreibbar()
    {
        var stand = BrennStand();
        var cut = Render<BrennstoffBestandteile>(p => p.Add(x => x.Stand, stand));

        Assert.All(cut.FindAll(".epos-preiszeile input"),
                   e => Assert.False(e.HasAttribute("disabled")));
    }

    // =====================================================================
    //  Das Formularraster — Anwenderwunsch iU8-E-2 / W14a-E-7, Paket P2
    //  (Windows-Abnahme 05.09.2026)
    // =====================================================================


    /// <summary>
    /// <b>SP-E-5 (a), Anwenderbefund 17.09.2026:</b> Die Gruppe „Vergütung für
    /// eingespeisten Strom" ist von der Trägerkarte VERSCHWUNDEN — weder der
    /// Gruppentitel noch die beiden Feldbeschriftungen stehen noch im Markup,
    /// zugeklappt wie aufgeklappt.
    ///
    /// <para>Die drei PREISBLÖCKE bleiben, wie sie sind: Ihre Zeilen tragen
    /// Schalter, Wert und Schnellwahlknopf NEBENEINANDER; das ist eine
    /// Bearbeitungszeile, keine Formularzeile.</para>
    /// </summary>
    [Fact]
    public void Die_Verguetungsgruppe_steht_nicht_mehr_auf_der_Traegerkarte()
    {
        var cut = Render<StrompreisDetails>(p => p.Add(x => x.Stand, StromStand()));

        Assert.DoesNotContain("Vergütung für eingespeisten Strom", cut.Markup);
        Assert.DoesNotContain("v_pv", cut.Markup);
        Assert.DoesNotContain("v_bhkw", cut.Markup);

        Aufklappen(cut);

        Assert.DoesNotContain("Vergütung für eingespeisten Strom", cut.Markup);
        Assert.DoesNotContain("v_pv", cut.Markup);
        Assert.DoesNotContain("v_bhkw", cut.Markup);

        // Die drei Preisblöcke stehen unverändert da.
        Assert.Equal(3, cut.FindAll(".epos-preisblock").Count);
        Assert.Empty(cut.FindAll(".epos-formularraster .epos-preisblock"));
    }
}
