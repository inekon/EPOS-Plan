using Bunit;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Feldkarten-Abgleich und Verhalten des Zeileneditors (iU9-W1.1).
/// Soll ist die Feldkarte von <c>Form_VorlagenPosition</c>: 5 Felder
/// (Bezeichnung, Kostenart, Erlös-Schalter, Empfehlung von, bis) plus Kopftitel
/// und OK/Abbrechen.
/// </summary>
public class VorlagenPositionDialogTests : BunitContext
{
    private static readonly (int Id, string Text)[] Kostenarten =
    {
        (0, "kapitalgebunden"),
        (1, "bedarfsgebunden"),
        (2, "betriebsgebunden"),
        (3, "sonstige"),
        (4, "Zuschuss")
    };

    public VorlagenPositionDialogTests()
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<VorlagenPositionDialog> Aufbauen(
        Action<VorlagenPositionErgebnis?> beimSchliessen,
        string bezeichnung = "Montage",
        int? kostenart = 1,
        bool istErloes = false,
        double? von = null,
        double? bis = null)
    {
        return Render<VorlagenPositionDialog>(p => p
            .Add(x => x.Kostenarten, Kostenarten)
            .Add(x => x.Bezeichnung, bezeichnung)
            .Add(x => x.KostenartId, kostenart)
            .Add(x => x.IstErloes, istErloes)
            .Add(x => x.EmpfehlungVon, von)
            .Add(x => x.EmpfehlungBis, bis)
            .Add(x => x.Geschlossen, beimSchliessen));
    }

    [Fact]
    public void Der_Feldbestand_der_Karte_steht_vollstaendig()
    {
        var cut = Aufbauen(_ => { });

        // 1 Textfeld + 2 Zahlenfelder = 3 <input type=text>, 1 Schalter, 1 Auswahl.
        Assert.Equal(3, cut.FindAll("input[type=text]").Count);
        Assert.Single(cut.FindAll("input[type=checkbox]"));
        Assert.Single(cut.FindAll("select"));
    }

    [Fact]
    public void Die_Maske_zeigt_die_heutigen_Beschriftungen()
    {
        var cut = Aufbauen(_ => { });

        Assert.Equal("Position bearbeiten", cut.Find(".epos-dialog-titel").TextContent);
        var texte = cut.FindAll(".epos-feld-text");
        Assert.Equal("Bezeichnung:", texte[0].TextContent);
        Assert.Equal("Kostenart:", texte[1].TextContent);
        Assert.Equal("Erlös/Zuschuss (negativer Ausweis)", texte[2].TextContent);
        Assert.Equal("Empfehlung von/bis:", texte[3].TextContent);
        Assert.Equal("bis", texte[4].TextContent);
    }

    [Fact]
    public void Die_Kostenartenliste_kommt_von_aussen()
    {
        var cut = Aufbauen(_ => { });

        var optionen = cut.FindAll("option");
        Assert.Equal(5, optionen.Count);
        Assert.Equal("kapitalgebunden", optionen[0].TextContent);
        Assert.Equal("Zuschuss", optionen[4].TextContent);
    }

    [Fact]
    public void Die_Vorbelegung_steht_in_den_Feldern()
    {
        var cut = Aufbauen(_ => { }, bezeichnung: "Montage", kostenart: 2,
                           istErloes: true, von: 1.5, bis: 3);

        var felder = cut.FindAll("input[type=text]");
        Assert.Equal("Montage", felder[0].GetAttribute("value"));
        Assert.Equal("1,5", felder[1].GetAttribute("value"));
        Assert.Equal("3", felder[2].GetAttribute("value"));
        Assert.Equal(2, cut.Instance.Kostenart);
        Assert.True(cut.Find("input[type=checkbox]").HasAttribute("checked"));
    }

    [Fact]
    public void Eine_unbekannte_Kostenart_faellt_auf_sonstige_zurueck()
    {
        // SetControls: index >= 0 ? index : 3.
        var cut = Aufbauen(_ => { }, kostenart: null);

        Assert.Equal(3, cut.Instance.Kostenart);
    }

    [Fact]
    public void OK_liefert_die_eingegebenen_Werte()
    {
        VorlagenPositionErgebnis? ergebnis = null;
        var cut = Aufbauen(e => ergebnis = e, kostenart: 0);

        cut.FindAll("input[type=text]")[0].Input("  Wartung  ");
        cut.Find("select").Change("4");
        cut.Find("input[type=checkbox]").Change(true);
        cut.FindAll("input[type=text]")[1].Input("2,5");
        cut.FindAll("input[type=text]")[2].Input("7.5");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.NotNull(ergebnis);
        Assert.Equal("Wartung", ergebnis!.Bezeichnung);
        Assert.Equal(4, ergebnis.KostenartId);
        Assert.True(ergebnis.IstErloes);
        Assert.Equal(2.5, ergebnis.EmpfehlungVon);
        Assert.Equal(7.5, ergebnis.EmpfehlungBis);
    }

    [Fact]
    public void Eine_leere_Empfehlung_bleibt_leer()
    {
        // Program.ZahlPruefen(..., leerErlaubt: true) - NULL heisst "nicht gepflegt".
        VorlagenPositionErgebnis? ergebnis = null;
        var cut = Aufbauen(e => ergebnis = e, von: 4, bis: 9);

        cut.FindAll("input[type=text]")[1].Input("");
        cut.FindAll("input[type=text]")[2].Input("");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.NotNull(ergebnis);
        Assert.Null(ergebnis!.EmpfehlungVon);
        Assert.Null(ergebnis.EmpfehlungBis);
    }

    [Fact]
    public void Eine_ungueltige_Zahl_faerbt_das_Feld_und_meldet_sie_nicht()
    {
        // A-8 aus B5b: Hausregel statt MessageBox von Program.ZahlPruefen.
        VorlagenPositionErgebnis? ergebnis = null;
        var cut = Aufbauen(e => ergebnis = e, von: 4);

        cut.FindAll("input[type=text]")[1].Input("vier");

        Assert.Contains("epos-fehleingabe", cut.FindAll("input[type=text]")[1].ClassName);

        cut.Find(".epos-knopf--primaer").Click();
        Assert.Equal(4, ergebnis!.EmpfehlungVon);
    }

    [Fact]
    public void OK_ohne_Bezeichnung_meldet_und_haelt_den_Dialog_offen()
    {
        bool geschlossen = false;
        var cut = Aufbauen(_ => geschlossen = true);

        cut.FindAll("input[type=text]")[0].Input("   ");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.False(geschlossen);
        Assert.Equal("Bitte eine Bezeichnung eingeben.", cut.Find(".epos-warnbanner-text").TextContent);
    }

    [Fact]
    public void Abbrechen_liefert_null()
    {
        VorlagenPositionErgebnis? ergebnis = new("x", 0, false, null, null);
        bool gemeldet = false;
        var cut = Aufbauen(e => { ergebnis = e; gemeldet = true; });

        cut.FindAll("button.epos-knopf")[0].Click();

        Assert.True(gemeldet);
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Enter_bestaetigt_und_Esc_bricht_ab()
    {
        VorlagenPositionErgebnis? ergebnis = null;
        int gemeldet = 0;
        var cut = Aufbauen(e => { ergebnis = e; gemeldet++; });

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(1, gemeldet);
        Assert.NotNull(ergebnis);

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

        Assert.Equal(new[] { "Form_VorlagenPosition.btn_Help" }, hilfe.Geoeffnet);
    }

    // =====================================================================
    //  Das Formularraster — Anwenderwunsch iU8-E-2 / W14a-E-7, Paket P2
    //  (Windows-Abnahme 05.09.2026)
    // =====================================================================


    /// <summary>
    /// <b>iU8-E-2 / W14a-E-7 (Paket P2):</b> Der Feldlauf steht im
    /// <c>Formularraster</c> — Beschriftung neben dem Feld, die beiden
    /// Empfehlungsfelder kurz.
    /// </summary>
    [Fact]
    public void Der_Feldlauf_steht_im_Formularraster()
    {
        var cut = Aufbauen(_ => { });

        Assert.Single(cut.FindAll(".epos-formularraster"));
        Assert.True(cut.FindAll(".epos-formularraster .epos-feld").Count >= 4);
        Assert.True(cut.FindAll(".epos-formularraster .epos-feld--kurz").Count >= 2);
    }
}
