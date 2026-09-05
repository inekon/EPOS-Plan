using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Worst/Best Case einer Kostenposition (iU9-W1.3). Soll ist die Feldkarte von
/// <c>Form_CaseEingabe</c> — vier Drehfelder aus dem Designer — PLUS die vier
/// zur Laufzeit angelegten Felder (Optionsgruppe absolut/%, Umrechnungszeile,
/// Startjahr, Zuschuss-Schalter).
///
/// Die Umrechnungszeile traegt Zahlen; die UI-Kultur wird deshalb wie in
/// <c>SpeichernLeisteTests</c> auf de-DE festgehalten.
/// </summary>
public class CaseEingabeDialogTests : BunitContext
{
    private readonly CultureInfo _kulturVorher = CultureInfo.CurrentCulture;
    private readonly CultureInfo _uiKulturVorher = CultureInfo.CurrentUICulture;

    public CaseEingabeDialogTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentCulture = _kulturVorher;
        CultureInfo.CurrentUICulture = _uiKulturVorher;
        base.Dispose(disposing);
    }

    private IRenderedComponent<CaseEingabeDialog> Aufbauen(
        Action<CaseEingabeErgebnis?> beimSchliessen,
        double betrag = 10000,
        double best = 8000,
        double worst = 12000,
        double bestDauer = 20,
        double worstDauer = 15,
        int startJahr = 0,
        bool zuschussMoeglich = false,
        bool istZuschuss = false,
        bool istErloes = false)
    {
        return Render<CaseEingabeDialog>(p => p
            .Add(x => x.Betrag, betrag)
            .Add(x => x.BestCase, best)
            .Add(x => x.WorstCase, worst)
            .Add(x => x.BestNutzungsdauer, bestDauer)
            .Add(x => x.WorstNutzungsdauer, worstDauer)
            .Add(x => x.StartJahr, startJahr)
            .Add(x => x.ZuschussMoeglich, zuschussMoeglich)
            .Add(x => x.IstZuschuss, istZuschuss)
            .Add(x => x.IstErloes, istErloes)
            .Add(x => x.Geschlossen, beimSchliessen));
    }

    [Fact]
    public void Der_Feldbestand_der_Karte_steht_vollstaendig()
    {
        var cut = Aufbauen(_ => { });

        // 4 Zahlenfelder (Karte) + 1 Ganzzahlfeld Startjahr (Laufzeit).
        Assert.Equal(5, cut.FindAll("input[type=text]").Count);
        // Die Optionsgruppe absolut/% (Laufzeit, KD6 § 11).
        Assert.Equal(2, cut.FindAll("input[type=radio]").Count);
    }

    [Fact]
    public void Die_Maske_zeigt_die_heutigen_Beschriftungen()
    {
        var cut = Aufbauen(_ => { });

        Assert.Equal("Eingabe Worst/Best Case", cut.Find(".epos-dialog-titel").TextContent);

        var gruppen = cut.FindAll(".epos-gruppenkopf-titel");
        Assert.Equal("Kosten:", gruppen[0].TextContent);
        Assert.Equal("Nutzungsdauer:", gruppen[1].TextContent);

        var texte = cut.FindAll(".epos-feld-text");
        Assert.Equal("Eingabe absolut [€]", texte[0].TextContent);
        Assert.Equal("Eingabe in % vom Erwartungswert", texte[1].TextContent);
        Assert.Equal("Best Case [€]:", texte[2].TextContent);
        Assert.Equal("Worst Case [€]:", texte[3].TextContent);
        Assert.Equal("Best Case [a]:", texte[4].TextContent);
        Assert.Equal("Worst Case [a]:", texte[5].TextContent);
        Assert.Equal("Startjahr (0 = sofort; Jahr X: Zahlung/Betrieb ab X):", texte[6].TextContent);
    }

    [Fact]
    public void Die_Vorbelegung_steht_in_den_Feldern()
    {
        var cut = Aufbauen(_ => { }, best: 8000, worst: 12000, bestDauer: 20,
                           worstDauer: 15, startJahr: 4);

        var felder = cut.FindAll("input[type=text]");
        Assert.Equal("8000", felder[0].GetAttribute("value"));
        Assert.Equal("12000", felder[1].GetAttribute("value"));
        Assert.Equal("20", felder[2].GetAttribute("value"));
        Assert.Equal("15", felder[3].GetAttribute("value"));
        Assert.Equal("4", felder[4].GetAttribute("value"));
    }

    [Fact]
    public void Der_Dialog_startet_absolut()
    {
        // rbCaseAbsolut.Checked = true.
        var cut = Aufbauen(_ => { });

        Assert.False(cut.Instance.ProzentModus);
        Assert.True(cut.FindAll("input[type=radio]")[0].HasAttribute("checked"));
        Assert.Empty(cut.FindAll(".epos-herleitung"));
    }

    [Fact]
    public void Ohne_Erwartungswert_bleibt_der_Prozentmodus_gesperrt()
    {
        // _rbProzent.Enabled = _daten.Betrag != 0.
        var cut = Aufbauen(_ => { }, betrag: 0);

        Assert.True(cut.FindAll("input[type=radio]")[1].HasAttribute("disabled"));

        cut.FindAll("input[type=radio]")[1].Change(true);
        Assert.False(cut.Instance.ProzentModus);
    }

    [Fact]
    public void Das_Umschalten_auf_Prozent_rechnet_die_Betraege_in_Abweichungen()
    {
        // ProzentModusGewechselt: (BestCase - Betrag) / |Betrag| * 100, auf 1 gerundet.
        var cut = Aufbauen(_ => { }, betrag: 10000, best: 8000, worst: 12000);

        cut.FindAll("input[type=radio]")[1].Change(true);

        Assert.True(cut.Instance.ProzentModus);
        var felder = cut.FindAll("input[type=text]");
        Assert.Equal("-20", felder[0].GetAttribute("value"));
        Assert.Equal("20", felder[1].GetAttribute("value"));
    }

    [Fact]
    public void Eine_Null_bleibt_im_Prozentmodus_eine_Null()
    {
        // 0 heisst "nicht gepflegt" und darf nicht zu -100 % werden.
        var cut = Aufbauen(_ => { }, betrag: 10000, best: 0, worst: 0);

        cut.FindAll("input[type=radio]")[1].Change(true);

        var felder = cut.FindAll("input[type=text]");
        Assert.Equal("0", felder[0].GetAttribute("value"));
        Assert.Equal("0", felder[1].GetAttribute("value"));
    }

    [Fact]
    public void Der_Prozentmodus_zeigt_die_Umrechnungszeile()
    {
        var cut = Aufbauen(_ => { }, betrag: 10000, best: 8000, worst: 12000);

        cut.FindAll("input[type=radio]")[1].Change(true);

        Assert.Contains("ergibt:", cut.Find(".epos-herleitung-text").TextContent);
        Assert.Contains("8.000,00", cut.Find(".epos-herleitung-text").TextContent);
    }

    [Fact]
    public void Das_Zurueckschalten_liefert_die_Betraege_wieder()
    {
        var cut = Aufbauen(_ => { }, betrag: 10000, best: 8000, worst: 12000);

        cut.FindAll("input[type=radio]")[1].Change(true);
        cut.FindAll("input[type=radio]")[0].Change(true);

        Assert.False(cut.Instance.ProzentModus);
        var felder = cut.FindAll("input[type=text]");
        Assert.Equal("8000", felder[0].GetAttribute("value"));
        Assert.Equal("12000", felder[1].GetAttribute("value"));
    }

    [Fact]
    public void OK_liefert_im_Prozentmodus_Betraege()
    {
        // btn_OK_Click (KL9): persistiert wird IMMER der Betrag.
        CaseEingabeErgebnis? ergebnis = null;
        var cut = Aufbauen(e => ergebnis = e, betrag: 10000, best: 8000, worst: 12000);

        cut.FindAll("input[type=radio]")[1].Change(true);
        cut.FindAll("input[type=text]")[0].Input("-10");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.NotNull(ergebnis);
        Assert.Equal(9000, ergebnis!.BestCase);
        Assert.Equal(12000, ergebnis.WorstCase);
    }

    [Fact]
    public void OK_liefert_im_Absolutmodus_die_Eingaben()
    {
        CaseEingabeErgebnis? ergebnis = null;
        var cut = Aufbauen(e => ergebnis = e);

        cut.FindAll("input[type=text]")[0].Input("7500");
        cut.FindAll("input[type=text]")[2].Input("18");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(7500, ergebnis!.BestCase);
        Assert.Equal(12000, ergebnis.WorstCase);
        Assert.Equal(18, ergebnis.BestNutzungsdauer);
        Assert.Equal(15, ergebnis.WorstNutzungsdauer);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]     // btn_OK_Click: Value > 1 ? Value : 0
    [InlineData(2, 2)]
    [InlineData(7, 7)]
    public void Das_Startjahr_folgt_der_Regel_des_Vorlaeufers(int eingabe, int erwartet)
    {
        CaseEingabeErgebnis? ergebnis = null;
        var cut = Aufbauen(e => ergebnis = e, startJahr: eingabe);

        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(erwartet, ergebnis!.StartJahr);
    }

    [Fact]
    public void Ohne_angebotenen_Schalter_bleibt_das_Zuschusskennzeichen_unveraendert()
    {
        CaseEingabeErgebnis? ergebnis = null;
        var cut = Aufbauen(e => ergebnis = e, zuschussMoeglich: false, istZuschuss: true);

        Assert.Empty(cut.FindAll("input[type=checkbox]"));
        cut.Find(".epos-knopf--primaer").Click();

        Assert.True(ergebnis!.IstZuschuss);
    }

    [Fact]
    public void Der_Zuschuss_Schalter_erscheint_nur_wenn_der_Aufrufer_ihn_anbietet()
    {
        CaseEingabeErgebnis? ergebnis = null;
        var cut = Aufbauen(e => ergebnis = e, zuschussMoeglich: true, istZuschuss: false);

        Assert.Single(cut.FindAll("input[type=checkbox]"));
        cut.Find("input[type=checkbox]").Change(true);
        cut.Find(".epos-knopf--primaer").Click();

        Assert.True(ergebnis!.IstZuschuss);
    }

    [Fact]
    public void Eine_Erlösposition_bekommt_eine_erklaerende_Zeile()
    {
        var cut = Aufbauen(_ => { }, istErloes: true);

        Assert.Contains("Erlösposition", cut.Find(".epos-herleitung-text").TextContent);
    }

    [Fact]
    public void Abbrechen_liefert_null_und_Esc_ebenso()
    {
        CaseEingabeErgebnis? ergebnis = new(1, 2, 3, 4, 5, true);
        int gemeldet = 0;
        var cut = Aufbauen(e => { ergebnis = e; gemeldet++; });

        cut.FindAll("button.epos-knopf")[0].Click();
        Assert.Equal(1, gemeldet);
        Assert.Null(ergebnis);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(2, gemeldet);
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Der_Hilfeknopf_traegt_den_Schluessel_der_Maske()
    {
        var hilfe = new TestHilfe();
        Services.AddSingleton<IHilfeDienst>(hilfe);

        var cut = Aufbauen(_ => { });
        cut.Find(".epos-infoknopf").Click();

        Assert.Equal(new[] { "Form_CaseEingabe.btn_Help" }, hilfe.Geoeffnet);
    }

    // =====================================================================
    //  Das Formularraster — Anwenderwunsch iU8-E-2 / W14a-E-7, Paket P2
    //  (Windows-Abnahme 05.09.2026)
    // =====================================================================


    /// <summary>
    /// <b>iU8-E-2 / W14a-E-7 (Paket P2):</b> Kosten, Nutzungsdauer und der
    /// Startjahr-Block stehen im <c>Formularraster</c> — Beschriftung neben dem
    /// Feld, Zahlenfelder kurz mit der Einheit dahinter.
    /// </summary>
    [Fact]
    public void Die_Bloecke_stehen_im_Formularraster()
    {
        var cut = Aufbauen(_ => { });

        Assert.True(cut.FindAll(".epos-formularraster").Count >= 3);
        Assert.True(cut.FindAll(".epos-formularraster .epos-feld--kurz").Count > 0);

        Assert.Contains(cut.FindAll(".epos-formularraster .epos-feld--kurz"),
                        f => f.QuerySelector(".epos-feld-zeile .epos-einheit") is not null);
    }
}
