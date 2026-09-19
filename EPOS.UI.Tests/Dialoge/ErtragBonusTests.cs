using Bunit;
using EPOS.UI.Dialoge.Allgemein;
using EPOS.UI.Dialoge.Kosten;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Abschnitt „Ertrag/Bonus" (iU9-W4.1), Vorbild
/// <c>Views/Kosten/ucErtragBonus</c>.
///
/// <para>Soll ist die Feldkarte: vier Gruppen für das BHKW (KWKG, Förderdauer,
/// Steuern, Pflegeorte), eine Gruppe für die Photovoltaik (Erklärung,
/// Stammprojekt, Knopf) und der Leersatz für alle übrigen Komponenten.</para>
/// </summary>
public class ErtragBonusTests : BunitContext
{
    private static readonly (int Id, string Text)[] PROJEKTE =
    {
        (7, "Musterprojekt"), (9, "Zweitprojekt")
    };

    // =====================================================================
    // Feldbestand
    // =====================================================================

    [Fact]
    public void Beim_BHKW_stehen_vier_Gruppen_in_der_Reihenfolge_der_Feldkarte()
    {
        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.IstBhkw, true)
            .Add(x => x.TitelKwkg, "KWKG-Zuschlag")
            .Add(x => x.TitelDauer, "Förderdauer und Jahresdeckel")
            .Add(x => x.TitelSteuern, "Steuervergünstigungen")
            .Add(x => x.TitelVerweise, "Pflegeorte"));

        var titel = cut.FindAll(".epos-gruppenkopf-titel");
        Assert.Equal(4, titel.Count);
        Assert.Equal("KWKG-Zuschlag", titel[0].TextContent);
        Assert.Equal("Förderdauer und Jahresdeckel", titel[1].TextContent);
        Assert.Equal("Steuervergünstigungen", titel[2].TextContent);
        Assert.Equal("Pflegeorte", titel[3].TextContent);
    }

    [Fact]
    public void Die_KWKG_Saetze_kommen_fertig_herein_und_stehen_in_fester_Breite()
    {
        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.IstBhkw, true)
            .Add(x => x.EinspeisungText, "bis 50 kW:      8,0 ct/kWh")
            .Add(x => x.SonderregelText, "Sonderregel neue Anlagen ≤ 50 kWel")
            .Add(x => x.EigenText, "Selbst genutzter KWK-Strom")
            .Add(x => x.DauerText, "Neue Anlagen: 30.000 Vollbenutzungsstunden")
            .Add(x => x.SteuernText, "Stromsteuer-Befreiung § 9 Abs. 1 Nr. 3")
            .Add(x => x.Fk7Text, "FK7: Der STROMPREIS-Teil"));

        Assert.Equal("bis 50 kW:      8,0 ct/kWh", cut.Find(".epos-ertrag-tabelle").TextContent);
        Assert.Contains("Sonderregel neue Anlagen", cut.Markup);
        Assert.Contains("Selbst genutzter KWK-Strom", cut.Markup);
        Assert.Contains("30.000 Vollbenutzungsstunden", cut.Markup);
        Assert.Contains("Stromsteuer-Befreiung", cut.Markup);
        Assert.Contains("FK7: Der STROMPREIS-Teil", cut.Markup);
    }

    [Fact]
    public void Bei_der_Photovoltaik_stehen_Erklaerung_Liste_und_Knopf()
    {
        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.IstPv, true)
            .Add(x => x.Projekte, PROJEKTE)
            .Add(x => x.PvErklaerungText, "Die PV-Vergütung wird STAMMPROJEKTBEZOGEN gepflegt")
            .Add(x => x.LabelPvProjekt, "Stammprojekt:")
            .Add(x => x.PvOeffnenText, "PV-Vergütungsdialog öffnen…"));

        Assert.Single(cut.FindAll(".epos-gruppenkopf"));
        Assert.Contains("STAMMPROJEKTBEZOGEN", cut.Markup);
        Assert.Equal(2, cut.Find("select").QuerySelectorAll("option").Length);
        Assert.Equal("PV-Vergütungsdialog öffnen…", cut.Find("button").TextContent);
    }

    [Fact]
    public void Ohne_BHKW_und_ohne_PV_steht_nur_der_Leersatz()
    {
        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.LeerText, "Diese Komponente führt keine laufenden Erträge"));

        Assert.Empty(cut.FindAll(".epos-gruppenkopf"));
        Assert.Contains("keine laufenden Erträge", cut.Markup);
    }

    // =====================================================================
    // Photovoltaik: Vorwahl und Sprung
    // =====================================================================

    [Fact]
    public void Ohne_Vorwahl_steht_das_erste_Projekt(){
        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.IstPv, true)
            .Add(x => x.Projekte, PROJEKTE));

        Assert.Equal(7, cut.Instance.GewaehltesProjekt);
    }

    [Fact]
    public void Eine_Vorwahl_wird_uebernommen()
    {
        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.IstPv, true)
            .Add(x => x.Projekte, PROJEKTE)
            .Add(x => x.ProjektVorwahl, 9));

        Assert.Equal(9, cut.Instance.GewaehltesProjekt);
    }

    [Fact]
    public void Der_Knopf_meldet_das_gewaehlte_Stammprojekt()
    {
        int gemeldet = 0;
        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.IstPv, true)
            .Add(x => x.Projekte, PROJEKTE)
            .Add(x => x.PvOeffnen, (int id) => gemeldet = id));

        cut.Find("select").Change("9");
        cut.Find("button").Click();

        Assert.Equal(9, gemeldet);
    }

    [Fact]
    public void Ohne_Projekte_ist_der_Knopf_gesperrt()
    {
        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.IstPv, true)
            .Add(x => x.Projekte, Array.Empty<(int, string)>()));

        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }

    // =====================================================================
    // Der Gesetzeskatalog - seit iU9-W14c.3 eine Ueberlagerung des WIRTS
    // =====================================================================

    [Fact]
    public void Ohne_Rueckruf_fehlt_der_Katalogknopf()
    {
        var cut = Render<ErtragBonus>(p => p.Add(x => x.IstBhkw, true));

        Assert.Empty(cut.FindAll("button"));
    }

    /// <summary>
    /// Bis W14c sprang der Knopf ueber die Sprungbruecke in ein WinForms-Fenster
    /// (<c>Sprungziel.Gesetzesparameter</c>) und las danach selbst neu. Der Katalog
    /// ist jetzt selbst Razor; diese Komponente ist ein REITERBLATT und kann keine
    /// Ueberlagerung oeffnen - sie meldet den Wunsch nach oben, und
    /// <c>KostenKomponenteDialog</c> zeigt den Katalog und laedt danach die Gaben
    /// dieses Blattes neu.
    /// </summary>
    [Fact]
    public void Der_Katalogknopf_meldet_den_Wunsch_nach_oben()
    {
        int gerufen = 0;
        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.IstBhkw, true)
            .Add(x => x.GesetzeGewuenscht, () => gerufen++));

        cut.Find("button").Click();

        Assert.Equal(1, gerufen);
    }

    [Fact]
    public void BHKW_und_PV_koennen_nicht_gleichzeitig_gelten_aber_beides_wird_gezeigt()
    {
        // Der Wirt entscheidet über HatInhalt; die Komponente zeigt, was sie
        // bekommt — dieselbe Arbeitsteilung wie im Vorläufer (Zeige).
        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.IstBhkw, true)
            .Add(x => x.IstPv, true)
            .Add(x => x.Projekte, PROJEKTE));

        Assert.Equal(5, cut.FindAll(".epos-gruppenkopf").Count);
    }

    // =====================================================================
    //  Das Formularraster — Anwenderwunsch iU8-E-2 / W14a-E-7, Paket P2
    //  (Windows-Abnahme 05.09.2026)
    // =====================================================================


    /// <summary>
    /// <b>iU8-E-2 / W14a-E-7 (Paket P2):</b> Die Projektwahl der PV-Ausprägung
    /// steht im <c>Formularraster</c> — Beschriftung neben dem Feld. Die
    /// Ertragstafel und die Herleitungszeilen bleiben Text; sie sind Ergebnis,
    /// kein Formularblock.
    /// </summary>
    [Fact]
    public void Die_Projektwahl_steht_im_Formularraster()
    {
        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.IstPv, true)
            .Add(x => x.TitelPv, "Photovoltaik")
            .Add(x => x.LabelPvProjekt, "PV-Projekt")
            .Add(x => x.Projekte, new[] { (1, "Variante A") }));

        Assert.Single(cut.FindAll(".epos-formularraster"));
        Assert.Single(cut.FindAll(".epos-formularraster .epos-feld"));
    }

    // =====================================================================
    //  KONZEPT § 2.16 — die Vergütungswahl je Variante (U38)
    // =====================================================================

    /// <summary>
    /// Eine VARIANTE bekommt die Optionsgruppe „vom Stammprojekt übernehmen |
    /// eigene Vergütung" mit der Vorgabe „übernehmen" und darunter die
    /// Erklärzeile, die die Herkunft nennt.
    /// </summary>
    [Fact]
    public void Bei_einer_Variante_steht_die_Wahl_mit_der_Erklaerzeile()
    {
        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.IstPv, true)
            .Add(x => x.IstVariante, true)
            .Add(x => x.Uebernommen, true)
            .Add(x => x.ProjektlisteZeigen, false)
            .Add(x => x.ProjektVorwahl, 9)
            .Add(x => x.LabelPvWahl, "Vergütung:")
            .Add(x => x.UebernehmenText, "vom Stammprojekt übernehmen")
            .Add(x => x.EigeneText, "eigene Vergütung")
            .Add(x => x.HerkunftText, "übernommen von Musterprojekt · anzulegender Wert 6,04 ct/kWh"));

        var optionen = cut.FindAll("input[type=radio]");
        Assert.Equal(2, optionen.Count);
        Assert.True(optionen[0].HasAttribute("checked"));
        Assert.False(optionen[1].HasAttribute("checked"));
        Assert.Contains("vom Stammprojekt übernehmen", cut.Markup);
        Assert.Contains("eigene Vergütung", cut.Markup);
        Assert.Contains("übernommen von Musterprojekt", cut.Markup);
    }

    /// <summary>
    /// Bei „eigene Vergütung" steht die zweite Option, und die Erklärzeile sagt es
    /// ebenso — beides aus denselben Gaben, keine Zweitwahrheit in der Komponente.
    /// </summary>
    [Fact]
    public void Eine_Variante_mit_eigenen_Werten_zeigt_die_zweite_Option()
    {
        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.IstPv, true)
            .Add(x => x.IstVariante, true)
            .Add(x => x.Uebernommen, false)
            .Add(x => x.ProjektlisteZeigen, false)
            .Add(x => x.ProjektVorwahl, 9)
            .Add(x => x.HerkunftText, "eigene Werte dieser Variante"));

        var optionen = cut.FindAll("input[type=radio]");
        Assert.False(optionen[0].HasAttribute("checked"));
        Assert.True(optionen[1].HasAttribute("checked"));
        Assert.Contains("eigene Werte dieser Variante", cut.Markup);
    }

    /// <summary>
    /// Der Wechsel meldet die neue Wahl an die Hülle (sie schreibt) und danach den
    /// Wunsch nach neuen Gaben an den Wirt. Eine Wahl, die schon steht, löst NICHTS
    /// aus — sonst öffnete jedes Neuzeichnen den Vergütungsdialog.
    /// </summary>
    [Fact]
    public void Der_Wechsel_meldet_die_Wahl_und_bittet_um_neue_Gaben()
    {
        bool? gewaehlt = null;
        int neuGeladen = 0;

        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.IstPv, true)
            .Add(x => x.IstVariante, true)
            .Add(x => x.Uebernommen, true)
            .Add(x => x.ProjektlisteZeigen, false)
            .Add(x => x.ProjektVorwahl, 9)
            .Add(x => x.WahlGeaendert, (bool u) => gewaehlt = u)
            .Add(x => x.NeuLaden, () => neuGeladen++));

        cut.FindAll("input[type=radio]")[1].Change("1");

        Assert.False(gewaehlt);
        Assert.Equal(1, neuGeladen);

        // Dieselbe Wahl noch einmal: nichts passiert.
        cut.FindAll("input[type=radio]")[1].Change("1");
        Assert.Equal(1, neuGeladen);
    }

    /// <summary>
    /// Ein STAMMPROJEKT führt immer eigene Werte — statt der Optionsgruppe steht
    /// die Zeile, wie viele Varianten seine Vergütung übernehmen (VV‑Q3).
    /// </summary>
    [Fact]
    public void Ein_Stammprojekt_bekommt_keine_Wahl_sondern_die_Zaehlzeile()
    {
        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.IstPv, true)
            .Add(x => x.IstVariante, false)
            .Add(x => x.ProjektlisteZeigen, false)
            .Add(x => x.ProjektVorwahl, 7)
            .Add(x => x.HerkunftText, "Stammprojekt — 2 Variante(n) übernehmen diese Vergütung"));

        Assert.Empty(cut.FindAll("input[type=radio]"));
        Assert.Contains("2 Variante(n) übernehmen", cut.Markup);
    }

    /// <summary>
    /// VV‑Q7: Im Projektmodus entfällt die Klappliste — der Stand steht fest, und
    /// der Knopf öffnet den Vergütungsdialog für ihn. Im Admin-Kontext bleibt sie.
    /// </summary>
    [Fact]
    public void Im_Projektmodus_entfaellt_die_Klappliste()
    {
        int gemeldet = 0;
        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.IstPv, true)
            .Add(x => x.ProjektlisteZeigen, false)
            .Add(x => x.ProjektVorwahl, 9)
            .Add(x => x.Projekte, PROJEKTE)
            .Add(x => x.PvOeffnen, (int id) => gemeldet = id));

        Assert.Empty(cut.FindAll("select"));
        cut.Find("button").Click();
        Assert.Equal(9, gemeldet);
    }
}
