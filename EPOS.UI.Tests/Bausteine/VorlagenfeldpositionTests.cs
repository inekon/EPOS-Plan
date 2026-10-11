using System;
using System.Linq;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <b>Die Positionsform an der Marke</b> (Konzept Berichtsvorlagen 4.5, 9.4; Katalog v8): Eine Marke an einem
/// Standwert nennt neben dem Schlüssel die Positionsform <c>stand.&lt;n&gt;.&lt;rest&gt;</c> — bei einer Variante auch
/// <c>variante.&lt;n&gt;.&lt;rest&gt;</c> — und kopiert sie ohne Blockrahmen; die Position kaskadiert die Seite. Ein
/// Platzhalter ohne Word-Ausgabe trägt die Zeile „nur Excel“.
/// </summary>
[Collection("Vorlagenfeldhalter")]
public sealed class VorlagenfeldpositionTests : VorlagenfeldBunitContext
{
    private static readonly Vorlagenfeldanzeige StandJaz = Vorlagenfeldproben.StandWert with { Positionsrest = "kennzahl.eff.jaz" };
    private static readonly Vorlagenfeldanzeige StammJaz = Vorlagenfeldproben.Jaz with { Positionsrest = "kennzahl.eff.jaz" };
    private static readonly Vorlagenfeldanzeige StandBild = Vorlagenfeldproben.StandBild with { Positionsrest = "bild.deckung_waerme" };
    private static readonly Vorlagenfeldanzeige Monatswerte = new(
        "stand.tabelle.monatswerte", "Tabelle", "Tabelle", "je Stand", "Stand", "Nur Excel: die Monatswerte",
        Excel: "auf dem Musterblatt", Positionsrest: "tabelle.monatswerte", NurExcel: true);

    public VorlagenfeldpositionTests()
    {
        Vorlagenfeldhalter.Setzen(new[] { StandJaz, StammJaz, StandBild, Monatswerte, Vorlagenfeldproben.Kunde });
        Ansicht.Setzen(Vorlagenfeldstellung.Marken);
    }

    private IRenderedComponent<Vorlagenfeldknopf> Marke(string schluessel, Vorlagenfeldposition? position)
        => Render<Vorlagenfeldknopf>(p =>
        {
            p.Add(x => x.Vorlagenfeld, schluessel);
            if (position is not null) p.AddCascadingValue(position);
        });

    [Fact]
    public void An_einer_Variante_stehen_Stand_und_Variantenposition_und_kopieren_ohne_Blockrahmen()
    {
        var cut = Marke("stand.kennzahl.eff.jaz", new Vorlagenfeldposition(3, 2));

        Assert.Equal(new[] { "stand.3.kennzahl.eff.jaz", "variante.2.kennzahl.eff.jaz" }, cut.Instance.Positionsformen);
        var zeilen = cut.FindAll(".epos-vorlagenfeld-auf-position");
        Assert.Equal(2, zeilen.Count);
        Assert.Contains("{{stand.3.kennzahl.eff.jaz}}", zeilen[0].TextContent);
        Assert.Contains("{{variante.2.kennzahl.eff.jaz}}", zeilen[1].TextContent);
        Assert.Equal("Positionsform stand.3.kennzahl.eff.jaz kopieren",
                     zeilen[0].QuerySelector("button")!.GetAttribute("aria-label"));
        Assert.Contains("Stammprojekt ist Stand 1", cut.Markup);

        zeilen[1].QuerySelector("button")!.Click();
        Assert.Equal(new[] { "{{variante.2.kennzahl.eff.jaz}}" }, Ablage.Texte);
    }

    [Fact]
    public void Am_Stammprojekt_steht_nur_Stand_1()
    {
        var cut = Marke("stamm.kennzahl.eff.jaz", new Vorlagenfeldposition(1, 0));
        Assert.Equal(new[] { "stand.1.kennzahl.eff.jaz" }, cut.Instance.Positionsformen);
        cut.Find(".epos-vorlagenfeld-position-kopieren").Click();
        Assert.Equal(new[] { "{{stand.1.kennzahl.eff.jaz}}" }, Ablage.Texte);
    }

    [Fact]
    public void Ein_Bild_kopiert_die_Positionsform_als_nackten_Schluessel()
    {
        var cut = Marke("stand.bild.deckung_waerme", new Vorlagenfeldposition(2, 1));
        cut.Find(".epos-vorlagenfeld-position-kopieren").Click();
        Assert.Equal(new[] { "stand.2.bild.deckung_waerme" }, Ablage.Texte);
    }

    /// <summary>
    /// Eine Tafel über mehrere Stände (Sensitivität): Der Wirt reicht die Positionen als Liste; die Aufklappung nennt
    /// je Stand seinen Namen und seine Formen in der Folge der Liste, und die Liste geht der kaskadierten Position vor.
    /// </summary>
    [Fact]
    public void Mehrere_Staende_stehen_je_mit_Namen_und_gehen_der_kaskadierten_Position_vor()
    {
        var positionen = new[]
        {
            new Vorlagenfeldposition(2, 1).MitName("WP klein"),
            new Vorlagenfeldposition(3, 2, "BHKW"),
            new Vorlagenfeldposition(0, 0, "unbekannt"),
        };
        var cut = Render<Vorlagenfeldknopf>(p =>
        {
            p.Add(x => x.Vorlagenfeld, "stand.kennzahl.eff.jaz");
            p.Add(x => x.Positionen, positionen);
            p.AddCascadingValue(new Vorlagenfeldposition(1, 0));
        });

        Assert.Equal(new[]
        {
            "stand.2.kennzahl.eff.jaz", "variante.1.kennzahl.eff.jaz",
            "stand.3.kennzahl.eff.jaz", "variante.2.kennzahl.eff.jaz",
        }, cut.Instance.Positionsformen);
        Assert.Equal(new[] { "WP klein", "BHKW" },
                     cut.FindAll(".epos-vorlagenfeld-auf-positionsname").Select(e => e.TextContent.Trim()).ToArray());
        Assert.Equal(4, cut.FindAll(".epos-vorlagenfeld-auf-position").Count);

        cut.FindAll(".epos-vorlagenfeld-position-kopieren")[2].Click();
        Assert.Equal(new[] { "{{stand.3.kennzahl.eff.jaz}}" }, Ablage.Texte);

        // Eine leere Liste nennt keine Form — auch nicht die kaskadierte.
        var leer = Render<Vorlagenfeldknopf>(p =>
        {
            p.Add(x => x.Vorlagenfeld, "stand.kennzahl.eff.jaz");
            p.Add(x => x.Positionen, Array.Empty<Vorlagenfeldposition>());
            p.AddCascadingValue(new Vorlagenfeldposition(1, 0));
        });
        Assert.Empty(leer.Instance.Positionsformen);

        // Die einzelne kaskadierte Position trägt keinen Namen.
        Assert.Empty(Marke("stand.kennzahl.eff.jaz", new Vorlagenfeldposition(3, 2)).FindAll(".epos-vorlagenfeld-auf-positionsname"));
    }

    [Fact]
    public void Ohne_Position_oder_ohne_Standwert_steht_keine_Positionsform()
    {
        Assert.Empty(Marke("stand.kennzahl.eff.jaz", null).FindAll(".epos-vorlagenfeld-auf-position"));
        Assert.Empty(Marke("projekt.kunde", new Vorlagenfeldposition(2, 1)).FindAll(".epos-vorlagenfeld-auf-position"));
        Assert.Empty(Marke("stand.kennzahl.eff.jaz", new Vorlagenfeldposition(0, 0)).FindAll(".epos-vorlagenfeld-auf-position"));
    }

    [Fact]
    public void Ein_Platzhalter_ohne_Word_Ausgabe_traegt_die_Zeile_nur_Excel()
    {
        Assert.Equal("nur Excel – nicht im Word-Bericht",
                     Marke("stand.tabelle.monatswerte", null).Find(".epos-vorlagenfeld-auf-nurexcel").TextContent);
        Assert.Empty(Marke("stand.kennzahl.eff.jaz", null).FindAll(".epos-vorlagenfeld-auf-nurexcel"));
    }

    [Fact]
    public void Die_Kopie_einer_Positionsform_folgt_der_Art()
    {
        Assert.Equal("{{stand.2.kennzahl.eff.jaz}}", Vorlagenfeldkopie.FuerPosition(StandJaz, "stand.2.kennzahl.eff.jaz").Text);
        Assert.Equal("", Vorlagenfeldkopie.FuerPosition(StandJaz, "").Text);
        Assert.Equal("", Vorlagenfeldkopie.FuerPosition(null, "stand.2.x").Text);
        Assert.Equal(Kopierhinweis.EigenerAbsatz, Vorlagenfeldkopie.FuerPosition(Monatswerte, "stand.1.tabelle.monatswerte").Hinweis);
    }
}

/// <summary>
/// <b>Der Positionsrest der Hülle</b> (<c>VorlagenfeldanzeigeHuelle.Positionsrest</c>): Standwerte und Stammwerte mit
/// Zwilling im Kontext Stand tragen ihn, und der Katalog löst die Positionsform daraus wirklich auf; alles Übrige nicht.
/// </summary>
[Collection("Vorlagenfeldhalter")]
public sealed class VorlagenfeldPositionsrestTests
{
    [Theory]
    [InlineData("stand.kennzahl.eff.jaz", "kennzahl.eff.jaz")]
    [InlineData("stamm.kennzahl.eff.jaz", "kennzahl.eff.jaz")]
    [InlineData("stand.bild.heizkessel", "bild.heizkessel")]
    [InlineData("stand.speicher.autarkie", "speicher.autarkie")]
    [InlineData("stand.tabelle.monatswerte", "tabelle.monatswerte")]
    [InlineData("stamm.wirtschaft.investition", "wirtschaft.investition")]
    [InlineData("stand.tabelle.sensitivitaet", "tabelle.sensitivitaet")]
    [InlineData("stand.tabelle.mehrjahres", "tabelle.mehrjahres")]
    [InlineData("stand.bild.zahlungsstrom", "bild.zahlungsstrom")]
    [InlineData("projekt.kunde", "")]
    [InlineData("stamm.bild.speichertemperaturen", "")]
    [InlineData("tabelle.vergleich.liste", "")]
    [InlineData("wirtschaft.beste.kapitalwert", "")]
    public void Der_Positionsrest_nennt_was_der_Katalog_nach_Position_aufloest(string schluessel, string rest)
    {
        Vorlagenfeld feld = Vorlagenfeldkatalog.Finde(schluessel);
        Assert.NotNull(feld);
        Assert.Equal(rest, VorlagenfeldanzeigeHuelle.Positionsrest(feld));
        if (rest.Length > 0)
            Assert.NotNull(Vorlagenfeldkatalog.Finde("stand.2." + rest));
    }

    [Fact]
    public void Nur_Excel_steht_genau_an_den_Eintraegen_ohne_Word_Ausgabe()
    {
        foreach (Vorlagenfeldanzeige a in VorlagenfeldanzeigeHuelle.Bilden())
        {
            Vorlagenfeld f = Vorlagenfeldkatalog.Finde(a.Schluessel);
            Assert.Equal((f.Ausgaben & Vorlagenausgabe.Word) == 0, a.NurExcel);
        }
        Assert.True(VorlagenfeldanzeigeHuelle.Bilden().Single(a => a.Schluessel == "tabelle.wirtschaft.parameter").NurExcel);
    }

    [Fact]
    public void Die_Position_eines_Projekts_zaehlt_Stamm_und_Varianten_in_der_Gruppe()
    {
        var gruppe = new[]
        {
            new VariantenCtrl.VarianteInfo { IdProjekt = 10, IstStamm = true },
            new VariantenCtrl.VarianteInfo { IdProjekt = 11 },
            new VariantenCtrl.VarianteInfo { IdProjekt = 12 },
        };
        Assert.Equal(new Vorlagenfeldposition(1, 0), VorlagenfeldpositionHuelle.Aus(gruppe, 10));
        Assert.Equal(new Vorlagenfeldposition(3, 2), VorlagenfeldpositionHuelle.Aus(gruppe, 12));
        Assert.Null(VorlagenfeldpositionHuelle.Aus(gruppe, 99));
        Assert.Null(VorlagenfeldpositionHuelle.Aus(null!, 10));
    }

    /// <summary>
    /// Die Positionen einer ganzen Gruppe in einem Zug (Wirtschaftlichkeitsseite): dieselbe Zählung wie
    /// <c>Aus</c>, je Projekt einmal — ein doppelter Eintrag behält seine erste Stelle.
    /// </summary>
    [Fact]
    public void Die_Positionen_einer_Gruppe_zaehlen_wie_die_einzelne()
    {
        var je = VorlagenfeldpositionHuelle.Je(new[] { (10, true), (11, false), (12, false), (11, false) });

        Assert.Equal(3, je.Count);
        Assert.Equal(new Vorlagenfeldposition(1, 0), je[10]);
        Assert.Equal(new Vorlagenfeldposition(2, 1), je[11]);
        Assert.Equal(new Vorlagenfeldposition(3, 2), je[12]);
        Assert.Empty(VorlagenfeldpositionHuelle.Je(null!));
        Assert.Equal(new Vorlagenfeldposition(2, 1, "WP"), je[11].MitName(" WP "));
    }
}
