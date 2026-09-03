using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Kosten;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die beiden Preisblöcke des Energieträgers (iU9-W4.3):
/// <c>StromAufschlaege</c> (Vorbild <c>ucStromAufschlaege</c>, 705 Z.) und
/// <c>BrennstoffBestandteile</c> (Vorbild <c>ucBrennstoffBestandteile</c>,
/// 863 Z.).
///
/// <para>Soll ist die Feldkarte: fünf bzw. vier Komponentenzeilen aus Schalter
/// und Wertfeld, der Modusumschalter, die Schnellwahlknöpfe, Summen- und
/// Restzeile — beim Strom zusätzlich Override und die zwei
/// Vergütungsfelder.</para>
/// </summary>
public class PreisbloeckeTests : BunitContext
{
    // Der Arbeitspreis erscheint als Zahl in der Anzeige; die CI-Läufer laufen
    // englisch. Dieselbe Klemmung wie in SpeichernLeisteTests.
    private readonly CultureInfo _kulturVorher = CultureInfo.CurrentCulture;

    public PreisbloeckeTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
    }

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentCulture = _kulturVorher;
        base.Dispose(disposing);
    }

    // =====================================================================
    // Strom-Aufschläge
    // =====================================================================

    private static StromAufschlaegeStand StromStand() => new StromAufschlaegeStand
    {
        Aufgeschluesselt = true,
        Netzentgelt = 7.5, NetzentgeltAktiv = true,
        Umlagen = 1.2, UmlagenAktiv = true,
        Stromsteuer = 2.05, StromsteuerAktiv = true,
        Konzession = 1.32, KonzessionAktiv = true,
        Vertrieb = 2.0, VertriebAktiv = true,
        Override = 15.0,
        VerguetungPv = 8.2,
        VerguetungBhkw = 6.4
    };

    [Fact]
    public void Der_Strom_Block_zeigt_fuenf_Komponenten_Override_und_zwei_Verguetungen()
    {
        var cut = Render<StromAufschlaege>(p => p.Add(x => x.Stand, StromStand()));

        Assert.Equal(5, cut.FindAll(".epos-preiszeile").Count);
        Assert.Equal(5, cut.FindAll(".epos-preiszeile input[type=checkbox]").Count);
        // 5 Komponenten + Override + zwei Vergütungen
        Assert.Equal(8, cut.FindAll("input[type=text]").Count);
        Assert.Equal(2, cut.FindAll(".epos-gruppenkopf").Count);
    }

    [Fact]
    public void Die_Beschriftungen_stehen_wie_in_der_Feldkarte()
    {
        var cut = Render<StromAufschlaege>(p => p
            .Add(x => x.Stand, StromStand())
            .Add(x => x.LabelNetzentgelt, "Netzentgelt Arbeit")
            .Add(x => x.LabelUmlagen, "Umlagen (Summe)")
            .Add(x => x.LabelStromsteuer, "Stromsteuer")
            .Add(x => x.LabelKonzession, "Konzessionsabgabe")
            .Add(x => x.LabelVertrieb, "Vertrieb"));

        foreach (string text in new[] { "Netzentgelt Arbeit", "Umlagen (Summe)",
                                        "Stromsteuer", "Konzessionsabgabe", "Vertrieb" })
            Assert.Contains(text, cut.Markup);
    }

    [Fact]
    public void Im_Override_Modus_bleiben_die_Komponenten_lesbar_aber_gesperrt()
    {
        var stand = StromStand();
        stand.Aufgeschluesselt = false;
        var cut = Render<StromAufschlaege>(p => p.Add(x => x.Stand, stand));

        // Sichtbar wie bisher (Fachkonzept 4.2), aber ohne Wirkung.
        Assert.Equal(5, cut.FindAll(".epos-preiszeile").Count);
        Assert.All(cut.FindAll(".epos-preiszeile input"), e => Assert.True(e.HasAttribute("disabled")));
        // Der Override ist genau jetzt bedienbar.
        Assert.False(cut.FindAll("input[type=text]")[5].HasAttribute("disabled"));
    }

    [Fact]
    public void Der_Modusumschalter_meldet_die_Aenderung()
    {
        var stand = StromStand();
        int gemeldet = 0;
        var cut = Render<StromAufschlaege>(p => p
            .Add(x => x.Stand, stand)
            .Add(x => x.Geaendert, () => gemeldet++));

        cut.FindAll(".epos-optionsgruppe input[type=radio]")[0].Change(true);

        Assert.False(stand.Aufgeschluesselt);
        Assert.Equal(1, gemeldet);
    }

    [Fact]
    public void Ein_geaenderter_Wert_landet_im_Stand_und_wird_gemeldet()
    {
        var stand = StromStand();
        int gemeldet = 0;
        var cut = Render<StromAufschlaege>(p => p
            .Add(x => x.Stand, stand)
            .Add(x => x.Geaendert, () => gemeldet++));

        cut.FindAll(".epos-preiszeile input[type=text]")[0].Input("8,25");

        Assert.Equal(8.25, stand.Netzentgelt);
        Assert.Equal(1, gemeldet);
    }

    [Fact]
    public void Die_Stromsteuer_Schnellwahl_traegt_ein_und_schaltet_aktiv()
    {
        var stand = StromStand();
        stand.Stromsteuer = 0;
        stand.StromsteuerAktiv = false;
        var cut = Render<StromAufschlaege>(p => p
            .Add(x => x.Stand, stand)
            .Add(x => x.SatzRegelfall, new Schnellwahlsatz("2,05", "Katalog: 20,5 €/MWh", 2.05))
            .Add(x => x.SatzReduziert, new Schnellwahlsatz("0,05", "Rückfallebene", 0.05, true)));

        cut.FindAll(".epos-schnellwahl-knopf")[0].Click();

        Assert.Equal(2.05, stand.Stromsteuer);
        Assert.True(stand.StromsteuerAktiv);
    }

    [Fact]
    public void Der_empfohlene_Satz_steht_hervorgehoben_und_nennt_seine_Herkunft()
    {
        var cut = Render<StromAufschlaege>(p => p
            .Add(x => x.Stand, StromStand())
            .Add(x => x.SatzRegelfall, new Schnellwahlsatz("2,05", "Katalog: 20,5 €/MWh", 2.05))
            .Add(x => x.SatzReduziert, new Schnellwahlsatz("0,05", "Rückfallebene 0,05", 0.05, true)));

        var knoepfe = cut.FindAll(".epos-schnellwahl-knopf");
        Assert.DoesNotContain("--empfohlen", knoepfe[0].ClassName);
        Assert.Contains("--empfohlen", knoepfe[1].ClassName);
        Assert.Equal("Rückfallebene 0,05", knoepfe[1].GetAttribute("title"));
    }

    [Fact]
    public void Summe_und_Rest_kommen_fertig_aus_der_Huelle()
    {
        var cut = Render<StromAufschlaege>(p => p
            .Add(x => x.Stand, StromStand())
            .Add(x => x.Anzeige, new PreisblockAnzeige(
                "Summe aktiv: 14,07 ct/kWh (wirksam 14,07)",
                "Nicht aufgeschlüsselter Rest: −0,93 ct/kWh", true)));

        Assert.Equal("Summe aktiv: 14,07 ct/kWh (wirksam 14,07)",
                     cut.Find(".epos-preisblock-summe").TextContent);
        Assert.Contains("--negativ", cut.Find(".epos-preisblock-rest").ClassName);
    }

    [Fact]
    public void Ein_positiver_Rest_bleibt_leise()
    {
        var cut = Render<StromAufschlaege>(p => p
            .Add(x => x.Stand, StromStand())
            .Add(x => x.Anzeige, new PreisblockAnzeige("Summe", "Rest: 0,93 ct/kWh", false)));

        Assert.DoesNotContain("--negativ", cut.Find(".epos-preisblock-rest").ClassName);
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
}
