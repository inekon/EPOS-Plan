using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Waermepumpe;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Gruppe „Gerätegrenzen" des Wärmepumpen-Stammblatts</b> (UB‑E3‑b, Umsetzungskonzept Übergabegrenze 6.3/6.4):
/// Klappliste „Kältemittel" und sieben Zahlenfelder, Schnellwahl füllt nur leere Felder, ein Auslieferungssatz ist
/// gesperrt bzw. zeigt Lesewerte, ein Wert außerhalb des Schemabereichs löst einen Hinweis aus. Kultur de-DE
/// (<see cref="EposBunitContext"/> mit der <c>Kulturvorrichtung</c>).
/// </summary>
public class WaermepumpeStammGrenzenTests : EposBunitContext
{
    /// <summary>Die Klappliste wie aus <c>BivalenzAbbildung.Kaeltemittelliste</c>: R290 (Klasse), R744 (Klasse), SONSTIGES.</summary>
    private static readonly KaeltemittelEintrag[] Liste =
    {
        new("R290", 70.0, Klasse: true, SpreizungAuslegungK: 5.0, SpreizungMaxK: 10.0, SpreizungMinK: 3.0,
            MindestvolumenstromProzent: 50.0),
        new("R744", 90.0, 40.0, 30.0, true, 5.0, 40.0, 3.0, 50.0, 2.5),
        new("SONSTIGES", null),
    };

    private static WaermepumpeStammDaten Satz() => new()
    {
        Id = 1, Name = "WP Alpha", Nennleistung = 12, Kaeltemittelliste = Liste,
        SpreizungMaxK = 12.0,                        // gepflegt - bleibt bei der Schnellwahl
    };

    private IRenderedComponent<WaermepumpeGeraetegrenzenFelder> Aufbauen(WaermepumpeStammDaten d, bool aktiv = true)
        => Render<WaermepumpeGeraetegrenzenFelder>(p => p
            .Add(x => x.Daten, d)
            .Add(x => x.Aktiv, aktiv));

    [Fact]
    public void Kaeltemittel_und_sieben_Zahlenfelder_stehen_da()
    {
        var t = new WaermepumpeStammFelderTexte();
        var cut = Aufbauen(Satz());

        Assert.Single(cut.FindAll("select"));
        Assert.Equal(7, cut.FindAll("input").Count);
        foreach (string label in new[] { t.LabelKaeltemittel, t.LabelSpreizungAuslegung, t.LabelSpreizungMax, t.LabelSpreizungMin,
                                         t.LabelMindestvolumenstrom, t.LabelRuecklaufMax, t.LabelRuecklaufBezug,
                                         t.LabelRuecklaufAbwertung })
            Assert.Contains(label, cut.Markup);
        Assert.Contains("%/K", cut.Markup);
        // Ohne R744 der Hinweis zu Bezugsrücklauf und Abwertung.
        Assert.Contains(t.HinweisNurR744, cut.Markup);
        // Die Optionen: „nicht gewählt" und die drei Codes.
        Assert.Equal(4, cut.FindAll("option").Count);
    }

    [Fact]
    public void Schnellwahl_fuellt_nur_leere_Felder()
    {
        WaermepumpeStammDaten d = Satz();
        var cut = Aufbauen(d);

        cut.Find("select").Change("0");             // R290

        Assert.Equal("R290", d.Kaeltemittel);
        Assert.Equal(5.0, d.SpreizungAuslegungK);
        Assert.Equal(12.0, d.SpreizungMaxK);        // gepflegt - unverändert
        Assert.Equal(3.0, d.SpreizungMinK);
        Assert.Equal(50.0, d.MindestvolumenstromProzent);
        Assert.Null(d.RuecklaufMaxC);               // R290 hat keine Rücklaufgrenze der Klasse
        Assert.Null(d.RuecklaufBezugC);
        Assert.Null(d.RuecklaufAbwertungProzentJeK);
        Assert.Equal(3, cut.Instance.ZuletztGefuellt);
        Assert.Contains(string.Format(CultureInfo.CurrentCulture, new WaermepumpeStammFelderTexte().HinweisSchnellwahlGefuellt, 3),
                        cut.Markup);

        // R744 danach: füllt nur, was noch leer ist - Grenze, Bezug, Abwertung.
        cut.Find("select").Change("1");
        Assert.Equal(40.0, d.RuecklaufMaxC);
        Assert.Equal(30.0, d.RuecklaufBezugC);
        Assert.Equal(2.5, d.RuecklaufAbwertungProzentJeK);
        Assert.Equal(5.0, d.SpreizungAuslegungK);
        Assert.DoesNotContain(new WaermepumpeStammFelderTexte().HinweisNurR744, cut.Markup);

        // Ohne eigene Klasse füllt die Wahl nichts.
        WaermepumpeStammDaten s = Satz();
        Assert.Equal(0, WaermepumpeGeraetegrenzenFelder.Schnellwahl(s, Liste[2]));
        Assert.Null(s.SpreizungMinK);
    }

    [Fact]
    public void Auslieferungssatz_ist_gesperrt_und_zeigt_Lesewerte()
    {
        WaermepumpeStammDaten d = Satz();
        d.NurLesen = true;
        d.Kaeltemittel = "R744";
        d.RuecklaufMaxC = 45.0;
        var cut = Aufbauen(d, aktiv: false);

        Assert.True(cut.Find("select").HasAttribute("disabled"));
        Assert.All(cut.FindAll("input"), i => Assert.True(i.HasAttribute("disabled")));

        var t = new WaermepumpeStammFelderTexte();
        var werte = WaermepumpeGeraetegrenzenFelder.Grenzwerte(d, t);
        Assert.Equal(8, werte.Count);
        Assert.Equal(t.KaeltemittelText("R744"), werte[0].Wert);
        Assert.Equal("45", werte[5].Wert);
        Assert.Equal("12", werte[2].Wert);
        Assert.Equal("", werte[1].Wert);            // leer bleibt leer
    }

    [Fact]
    public void Wert_ausserhalb_des_Bereichs_gibt_einen_Hinweis()
    {
        WaermepumpeStammDaten d = Satz();
        d.RuecklaufMaxC = 75.0;                     // CHECK 20 ... 70
        var t = new WaermepumpeStammFelderTexte();
        var cut = Aufbauen(d);

        string hinweis = string.Format(CultureInfo.CurrentCulture, t.HinweisBereich, t.LabelRuecklaufMax, "20", "70");
        Assert.Contains(hinweis, cut.Markup);
        Assert.Single(WaermepumpeGeraetegrenzenFelder.Bereichshinweise(d, t));

        d.RuecklaufMaxC = 55.0;
        Assert.Empty(WaermepumpeGeraetegrenzenFelder.Bereichshinweise(d, t));
    }
}
