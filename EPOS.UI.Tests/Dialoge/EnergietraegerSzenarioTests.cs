using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// ETAPPE E9b (Konzept § 2.11.5 „Pflege"; Entscheide E9b‑Q1 und E9b‑Q4, Lesart a) —
/// <b>die ±-Knöpfe an den Trägerpreisen</b>.
///
/// <para>Die Trägerkarte (<see cref="EnergietraegerEinstellungen"/>) trägt je Preis
/// (Arbeits-, Grund- und — wo der Träger ihn führt — Leistungspreis) einen ±-Knopf, aber
/// nur, wo die Hülle es freigibt (<c>MitSzenario</c>: im PROJEKT, bei einer Datenbank
/// mit den Spalten des Schemaschritts 117) und ein Wirt den Weg anbietet. Das
/// Kennzeichen sagt, ob ein Paar gepflegt ist; ein gepflegtes Paar ohne Erwartet-Preis
/// trägt das Warnzeichen und eine Kohärenzzeile, ebenso ein Szenario-Leistungspreis
/// neben Staffel oder Saisonreihe.</para>
///
/// <para>Der Wirt (<see cref="EnergietraegerDialog"/>) öffnet den <c>CaseEingabeDialog</c>
/// als Szenariopaar in seiner Überlagerung und legt das Ergebnis auf die Karte;
/// geschrieben wird mit der Karte (die Hülle prüft <c>EnergietraegerSzenarioHuelleTests</c>
/// gegen die Testdatenbank).</para>
/// </summary>
public class EnergietraegerSzenarioTests : EposBunitContext
{
    public EnergietraegerSzenarioTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static EnergietraegerStand Erdgas(bool mitSzenario = true, bool mitLeistungspreis = true) => new()
    {
        TraegerZeile = "Erdgas H  (VDI 3805 3)",
        GruppeZeile = "Gruppe: Gas",
        Arbeitspreis = 0.65,
        Leistungspreis = 12,
        Grundpreis = 120,
        Heizwert = 10.1,
        Brennwert = 11.2,
        MitHeizwert = true,
        MitBrennwert = true,
        MitLeistungspreis = mitLeistungspreis,
        EinheitArbeitspreis = "€/Nm³",
        EinheitLeistungspreis = "€/(kW·a)",
        Basiseinheit = "Nm³",
        MitSzenario = mitSzenario,
        TraegerName = "Erdgas H"
    };

    // =====================================================================
    //  Die Karte
    // =====================================================================

    private readonly List<string> _angefordert = new();

    private IRenderedComponent<EnergietraegerEinstellungen> Karte(EnergietraegerStand stand,
                                                                  bool mitWeg = true)
        => Render<EnergietraegerEinstellungen>(p =>
        {
            p.Add(x => x.Stand, stand);
            if (mitWeg) p.Add(x => x.SzenarioPflegen, (string preis) => _angefordert.Add(preis));
        });

    private static IReadOnlyList<IElement> Knoepfe(IRenderedComponent<EnergietraegerEinstellungen> cut)
        => cut.FindAll("button.epos-szenarioknopf");

    [Fact]
    public void Im_Projekt_traegt_die_Karte_je_Preis_einen_Knopf()
    {
        var cut = Karte(Erdgas());

        Assert.Equal(new[] { "Arbeitspreis", "Grundpreis", "Leistungspreis" },
                     cut.FindAll(".epos-szenarioknopf-text").Select(e => e.TextContent).ToArray());
        IElement gruppe = cut.Find(".epos-szenarioleiste");
        Assert.Equal("group", gruppe.GetAttribute("role"));
        Assert.Equal("Szenariopreise Best/Worst", gruppe.GetAttribute("aria-label"));
        Assert.Contains(cut.FindAll(".epos-herleitung-text"),
                        e => e.TextContent.StartsWith("± je Preis: Best (Günstig) und Worst (Ungünstig)"));
        Assert.Empty(cut.FindAll(".epos-szenarioknopf-kennzeichen"));
    }

    [Fact]
    public void Ohne_Leistungspreis_fehlt_sein_Knopf()
    {
        var cut = Karte(Erdgas(mitLeistungspreis: false));

        Assert.Equal(new[] { "Arbeitspreis", "Grundpreis" },
                     cut.FindAll(".epos-szenarioknopf-text").Select(e => e.TextContent).ToArray());
    }

    /// <summary>
    /// Nur im Projekt und nur mit Weg: Im Katalog (oder einer Datenbank ohne die Spalten)
    /// gibt die Hülle die Knöpfe nicht frei, und ohne Delegat gibt es keinen Knopf.
    /// </summary>
    [Fact]
    public void Ohne_Freigabe_oder_ohne_Weg_gibt_es_keine_Knoepfe()
    {
        Assert.Empty(Knoepfe(Karte(Erdgas(mitSzenario: false))));
        Assert.Empty(Karte(Erdgas(mitSzenario: false)).FindAll(".epos-szenarioleiste"));
        Assert.Empty(Knoepfe(Karte(Erdgas(), mitWeg: false)));
    }

    [Fact]
    public void Ein_Klick_meldet_die_Preisart()
    {
        var cut = Karte(Erdgas());

        Knoepfe(cut)[0].Click();
        Knoepfe(cut)[1].Click();
        Knoepfe(cut)[2].Click();

        Assert.Equal(new[] { EnergietraegerEinstellungen.PREIS_ARBEIT, EnergietraegerEinstellungen.PREIS_GRUND,
                             EnergietraegerEinstellungen.PREIS_LEISTUNG }, _angefordert);
    }

    /// <summary>
    /// Das Kennzeichen folgt der 1e−9-Regel des Kerns: Ein Szenariopreis gleich dem
    /// Erwartet-Preis ist keine Pflege. Der Kurztext nennt die gepflegten Werte in der
    /// Einheit des Feldes daneben — der Arbeitspreis mit vier Stellen.
    /// </summary>
    [Fact]
    public void Das_Kennzeichen_steht_nur_an_einem_abweichenden_Paar()
    {
        EnergietraegerStand s = Erdgas();
        s.SzenarioArbeitBest = 0.70;
        s.SzenarioGrundBest = 120;                  // = Erwartet: keine Pflege
        var cut = Karte(s);

        IElement arbeit = Knoepfe(cut)[0];
        Assert.Contains("epos-szenarioknopf--gepflegt", arbeit.ClassName);
        Assert.Equal("Szenariowerte gepflegt — Best 0,7000 €/Nm³ · Worst wie Erwartet",
                     arbeit.GetAttribute("title"));
        Assert.Equal("± Arbeitspreis (gepflegt)", arbeit.GetAttribute("aria-label"));

        Assert.DoesNotContain("epos-szenarioknopf--gepflegt", Knoepfe(cut)[1].ClassName);
        Assert.Equal("Szenariowerte Best/Worst pflegen", Knoepfe(cut)[1].GetAttribute("title"));
        Assert.Empty(cut.FindAll(".epos-kohaerenz-text"));
    }

    /// <summary>
    /// E9b‑Q4 (warnen, nicht verweigern): Ein gepflegter Arbeitspreis ohne Erwartet-Preis
    /// trägt das Warnzeichen und eine Kohärenzzeile. Ohne Pflege gibt es nichts zu warnen.
    /// </summary>
    [Fact]
    public void Ein_gepflegter_Preis_ohne_Erwartet_Preis_warnt()
    {
        EnergietraegerStand ohne = Erdgas();
        ohne.Arbeitspreis = 0;
        ohne.SzenarioArbeitOhneErwartet = true;
        Assert.Empty(Karte(ohne).FindAll(".epos-szenarioknopf-warnung"));

        ohne.SzenarioArbeitWorst = 0.80;
        var cut = Karte(ohne);

        Assert.Single(cut.FindAll(".epos-szenarioknopf-warnung"));
        Assert.Equal("± Arbeitspreis (gepflegt) (ohne Erwartet-Wert)", Knoepfe(cut)[0].GetAttribute("aria-label"));
        Assert.Equal(new[] { "Arbeitspreis: Szenariopreis ohne Erwartet-Preis — Günstig und Ungünstig "
                             + "rechnen damit, Erwartet zeigt die Datenlücke." },
                     cut.FindAll(".epos-kohaerenz-text").Select(e => e.TextContent).ToArray());
    }

    /// <summary>
    /// E9a‑Q3 (Lesart a): Neben einer gepflegten Staffel oder Saisonreihe bleibt ein
    /// Szenario-Leistungspreis ohne Wirkung — die Zeile der Hülle steht da, sobald er
    /// gepflegt ist.
    /// </summary>
    [Fact]
    public void Neben_Staffel_oder_Saisonreihe_sagt_die_Karte_dass_der_Leistungspreis_nicht_wirkt()
    {
        const string ZEILE = "Szenario-Leistungspreis des Energieträgers „Erdgas H“ ohne Wirkung: …";
        EnergietraegerStand s = Erdgas();
        s.SzenarioLeistungOhneWirkung = ZEILE;
        Assert.Empty(Karte(s).FindAll(".epos-kohaerenz-text"));

        s.SzenarioLeistungBest = 15;
        var cut = Karte(s);

        Assert.Equal(new[] { ZEILE }, cut.FindAll(".epos-kohaerenz-text").Select(e => e.TextContent).ToArray());
        Assert.Contains("epos-szenarioknopf--gepflegt", Knoepfe(cut)[2].ClassName);
        Assert.Equal(new[] { ZEILE }, cut.Instance.SzenarioZeilen);
    }

    // =====================================================================
    //  Der Wirt: die Überlagerung des Szenariopaars
    // =====================================================================

    private static readonly EnergietraegerDialog.EnergietraegerListe[] LISTE =
    {
        new(null, "Gas"),
        new(11, "Erdgas H")
    };

    private EnergietraegerAnsicht _ansicht = new();

    private IRenderedComponent<EnergietraegerDialog> Dialog(EnergietraegerStand stand,
                                                            Action<bool>? geschlossen = null)
    {
        _ansicht = new EnergietraegerAnsicht { Stand = stand, StammName = "Erdgas H" };
        return Render<EnergietraegerDialog>(p =>
        {
            p.Add(x => x.Liste, LISTE);
            p.Add(x => x.Katalogkontext, false);
            p.Add(x => x.TraegerLaden, _ => _ansicht);
            p.Add(x => x.Nachrechnen, () => _ansicht);
            if (geschlossen is not null) p.Add(x => x.Geschlossen, geschlossen);
        });
    }

    [Fact]
    public void Der_Knopf_oeffnet_das_Paar_in_der_Ueberlagerung_und_OK_legt_es_auf_die_Karte()
    {
        EnergietraegerStand s = Erdgas();
        var cut = Dialog(s);

        cut.FindAll("button.epos-szenarioknopf")[0].Click();

        Assert.True(cut.Instance.SzenarioOffen);
        IElement ueberlagerung = cut.Find(".epos-ueberlagerung");
        Assert.Equal("Szenariowerte — Arbeitspreis Erdgas H",
                     cut.Find(".epos-ueberlagerung-titel").TextContent.Trim());
        Assert.Empty(ueberlagerung.QuerySelectorAll(".epos-dialog-titel"));    // ein Titel, eine Stelle
        Assert.Contains(ueberlagerung.QuerySelectorAll(".epos-herleitung-text"),
                        e => e.TextContent == "Erwartet: 0,6500 €/Nm³");
        Assert.Empty(ueberlagerung.QuerySelectorAll(".epos-warnbanner"));

        ueberlagerung.QuerySelectorAll("input[inputmode=decimal]")[0].Input("0,7000");
        cut.Find(".epos-ueberlagerung").QuerySelectorAll("input[inputmode=decimal]")[1].Input("0,5500");
        cut.Find(".epos-ueberlagerung .epos-knopf--primaer").Click();

        Assert.False(cut.Instance.SzenarioOffen);
        Assert.Equal(0.70, s.SzenarioArbeitBest);
        Assert.Equal(0.55, s.SzenarioArbeitWorst);
        Assert.Null(s.SzenarioGrundBest);                                      // nur dieses Paar
        Assert.Contains("epos-szenarioknopf--gepflegt", cut.FindAll("button.epos-szenarioknopf")[0].ClassName);
    }

    /// <summary>
    /// Der Grundpreis warnt nicht (0 €/a ist ein gültiger Vertragswert), der
    /// Leistungspreis nimmt die Kohärenzzeile der Hülle mit ins Blatt.
    /// </summary>
    [Fact]
    public void Grund_und_Leistungspreis_oeffnen_mit_ihren_eigenen_Regeln()
    {
        const string ZEILE = "Szenario-Leistungspreis ohne Wirkung (Staffel).";
        EnergietraegerStand s = Erdgas();
        s.Grundpreis = 0;
        s.SzenarioLeistungOhneWirkung = ZEILE;
        var cut = Dialog(s);

        cut.FindAll("button.epos-szenarioknopf")[1].Click();
        Assert.Equal("Szenariowerte — Grundpreis Erdgas H", cut.Find(".epos-ueberlagerung-titel").TextContent.Trim());
        Assert.Contains(cut.FindAll(".epos-ueberlagerung .epos-herleitung-text"),
                        e => e.TextContent == "Erwartet: kein Wert gepflegt");
        Assert.Empty(cut.FindAll(".epos-ueberlagerung .epos-warnbanner"));
        cut.Find(".epos-ueberlagerung-zu").Click();
        Assert.False(cut.Instance.SzenarioOffen);

        cut.FindAll("button.epos-szenarioknopf")[2].Click();
        Assert.Contains(cut.FindAll(".epos-ueberlagerung .epos-kohaerenz-text"), e => e.TextContent == ZEILE);
        cut.Find(".epos-ueberlagerung").QuerySelectorAll("input[inputmode=decimal]")[1].Input("20");
        cut.Find(".epos-ueberlagerung .epos-knopf--primaer").Click();

        Assert.Equal(20.0, s.SzenarioLeistungWorst);
        Assert.Null(s.SzenarioLeistungBest);
    }

    /// <summary>
    /// Esc gehört der obersten Ebene: Der Wirt schließt nicht, solange das Paar steht, und
    /// Esc in der Überlagerung schließt nur sie — ohne zu schreiben.
    /// </summary>
    [Fact]
    public void Esc_schliesst_nur_die_Ueberlagerung_des_Paars()
    {
        bool? ergebnis = null;
        EnergietraegerStand s = Erdgas();
        var cut = Dialog(s, ok => ergebnis = ok);

        cut.FindAll("button.epos-szenarioknopf")[0].Click();
        cut.Find(".epos-ueberlagerung").QuerySelectorAll("input[inputmode=decimal]")[0].Input("0,7000");

        cut.Find(".epos-dialog").KeyDown(key: "Escape");
        Assert.True(cut.Instance.SzenarioOffen);
        Assert.Null(ergebnis);

        cut.Find(".epos-ueberlagerung").KeyDown(key: "Escape");
        Assert.False(cut.Instance.SzenarioOffen);
        Assert.Null(ergebnis);
        Assert.Null(s.SzenarioArbeitBest);
    }
}
