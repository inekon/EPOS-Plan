using Bunit;
using EPOS.UI.Dialoge.Allgemein;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die eine Namensabfrage fuer fuenf zeichengleiche Masken (iU9-W1.2).
/// Soll sind die Feldkarten von <c>Form_VariantenName</c> (Textfeld, OK,
/// Abbrechen, Kopftitel) und <c>Form_KostenItemNeu</c> (dieselben Felder).
/// </summary>
public class NamensDialogTests : BunitContext
{
    public NamensDialogTests()
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<NamensDialog> Aufbauen(
        Action<string?> beimSchliessen,
        string titel = "Neue Variante",
        string frage = "Name der neuen Variante:",
        string vorbelegung = "",
        string meldungLeer = "")
    {
        return Render<NamensDialog>(p => p
            .Add(x => x.TitelText, titel)
            .Add(x => x.FrageText, frage)
            .Add(x => x.Vorbelegung, vorbelegung)
            .Add(x => x.MeldungLeer, meldungLeer)
            .Add(x => x.Geschlossen, beimSchliessen));
    }

    [Fact]
    public void Der_Feldbestand_der_Karte_steht_vollstaendig()
    {
        var cut = Aufbauen(_ => { });

        Assert.Single(cut.FindAll("input[type=text]"));
        Assert.Equal(2, cut.FindAll("button.epos-knopf").Count);   // Abbrechen und OK
    }

    [Fact]
    public void Titel_und_Frage_kommen_vom_Aufrufer()
    {
        var cut = Aufbauen(_ => { }, titel: "Neuer Energieträger",
                           frage: "Bezeichnung des neuen Trägers:");

        Assert.Equal("Neuer Energieträger", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal("Bezeichnung des neuen Trägers:", cut.Find(".epos-feld-text").TextContent);
    }

    [Fact]
    public void Die_Vorbelegung_steht_sichtbar_im_Feld()
    {
        // FK9: Der Aufrufer schlaegt "<Komponente> - Variante n" vor.
        var cut = Aufbauen(_ => { }, vorbelegung: "BHKW — Variante 3");

        Assert.Equal("BHKW — Variante 3", cut.Find("input[type=text]").GetAttribute("value"));
        Assert.Equal("BHKW — Variante 3", cut.Instance.Name);
    }

    [Fact]
    public void OK_liefert_den_getrimmten_Namen()
    {
        string? ergebnis = null;
        var cut = Aufbauen(e => ergebnis = e);

        cut.Find("input[type=text]").Input("  Sommerbetrieb  ");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal("Sommerbetrieb", ergebnis);
    }

    [Fact]
    public void Ein_leerer_Name_haelt_den_Dialog_offen()
    {
        // btnOk_Click: name.Length == 0 -> return, Dialog bleibt offen.
        bool gemeldet = false;
        var cut = Aufbauen(_ => gemeldet = true, vorbelegung: "Vorschlag");

        cut.Find("input[type=text]").Input("   ");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.False(gemeldet);
    }

    [Fact]
    public void Der_leere_Name_meldet_sich_wenn_der_Aufrufer_einen_Text_mitgibt()
    {
        // Form_KostenItemNeu zeigte hier eine MessageBox ("Bezeichnung eingeben!").
        var cut = Aufbauen(_ => { }, meldungLeer: "Bitte einen Namen eingeben.");

        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal("Bitte einen Namen eingeben.", cut.Find(".epos-warnbanner-text").TextContent);
    }

    [Fact]
    public void Ohne_Meldungstext_bleibt_der_Dialog_stumm()
    {
        var cut = Aufbauen(_ => { });

        cut.Find(".epos-knopf--primaer").Click();

        Assert.Empty(cut.FindAll(".epos-warnbanner"));
    }

    [Fact]
    public void Eine_Eingabe_loescht_die_Meldung()
    {
        var cut = Aufbauen(_ => { }, meldungLeer: "Bitte einen Namen eingeben.");

        cut.Find(".epos-knopf--primaer").Click();
        Assert.Single(cut.FindAll(".epos-warnbanner"));

        cut.Find("input[type=text]").Input("Kesseltausch");
        Assert.Empty(cut.FindAll(".epos-warnbanner"));
    }

    [Fact]
    public void Abbrechen_liefert_null()
    {
        string? ergebnis = "noch nicht gesetzt";
        bool gemeldet = false;
        var cut = Aufbauen(e => { ergebnis = e; gemeldet = true; }, vorbelegung: "Vorschlag");

        cut.FindAll("button.epos-knopf")[0].Click();

        Assert.True(gemeldet);
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Enter_bestaetigt_und_Esc_bricht_ab()
    {
        string? ergebnis = null;
        int gemeldet = 0;
        var cut = Aufbauen(e => { ergebnis = e; gemeldet++; }, vorbelegung: "Vorschlag");

        cut.Find("input[type=text]").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(1, gemeldet);
        Assert.Equal("Vorschlag", ergebnis);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(2, gemeldet);
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Ohne_Hilfeschluessel_gibt_es_keinen_Infoknopf()
    {
        var cut = Aufbauen(_ => { });

        Assert.Empty(cut.FindAll(".epos-infoknopf"));
    }
}
