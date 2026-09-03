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

    // ------------------------------------------------------------ iU9-W2.1

    [Fact]
    public void Ohne_Hinweistext_steht_keine_Herleitungszeile()
    {
        var cut = Aufbauen(_ => { });

        Assert.Empty(cut.FindAll(".epos-herleitung"));
    }

    [Fact]
    public void Der_Hinweistext_steht_ueber_dem_Feld()
    {
        // Form_AlsVariante.lblHinweis: "Der aktuelle Stand wird als eigenstaendige
        // Variante des Stammprojekts ... gesichert."
        var cut = Render<NamensDialog>(p => p
            .Add(x => x.TitelText, "Als Variante speichern")
            .Add(x => x.HinweisText, "Der aktuelle Stand wird gesichert.")
            .Add(x => x.Geschlossen, (Action<string?>)(_ => { })));

        Assert.Equal("Der aktuelle Stand wird gesichert.",
                     cut.Find(".epos-herleitung-text").TextContent);
    }

    [Fact]
    public void Ohne_Zusatzfrage_gibt_es_nur_ein_Textfeld()
    {
        var cut = Aufbauen(_ => { });

        Assert.Single(cut.FindAll("input[type=text]"));
    }

    [Fact]
    public void Die_Beschreibung_ist_ein_zweites_Feld_mit_eigener_Vorbelegung()
    {
        // Form_GebaeudetypNeu: Bezeichner UND Beschreibung.
        var cut = Render<NamensDialog>(p => p
            .Add(x => x.FrageText, "Bezeichner")
            .Add(x => x.ZusatzFrageText, "Beschreibung:")
            .Add(x => x.ZusatzVorbelegung, "Bürogebäude")
            .Add(x => x.Geschlossen, (Action<string?>)(_ => { })));

        var felder = cut.FindAll("input[type=text]");
        Assert.Equal(2, felder.Count);
        Assert.Equal("Bürogebäude", felder[1].GetAttribute("value"));
        Assert.Equal("Beschreibung:", cut.FindAll(".epos-feld-text")[1].TextContent);
    }

    [Fact]
    public void OK_meldet_die_Beschreibung_vor_dem_Namen()
    {
        // Reihenfolge ist Pflicht: Geschlossen nimmt der Huelle das Fenster weg.
        var reihenfolge = new List<string>();
        string? name = null;
        string? beschreibung = null;

        var cut = Render<NamensDialog>(p => p
            .Add(x => x.ZusatzFrageText, "Beschreibung:")
            .Add(x => x.ZusatzGeschlossen,
                 (Action<string>)(t => { beschreibung = t; reihenfolge.Add("zusatz"); }))
            .Add(x => x.Geschlossen,
                 (Action<string?>)(t => { name = t; reihenfolge.Add("name"); })));

        cut.FindAll("input[type=text]")[0].Input("Werkhalle");
        cut.FindAll("input[type=text]")[1].Input("Halle 3, unbeheizt");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal("Werkhalle", name);
        Assert.Equal("Halle 3, unbeheizt", beschreibung);
        Assert.Equal(new[] { "zusatz", "name" }, reihenfolge);
    }

    [Fact]
    public void OkNurMitText_sperrt_den_Knopf_solange_das_Feld_leer_ist()
    {
        // Form_AlsVariante: btnAnlegen.Enabled = Bezeichner.Length > 0.
        var cut = Render<NamensDialog>(p => p
            .Add(x => x.OkNurMitText, true)
            .Add(x => x.Geschlossen, (Action<string?>)(_ => { })));

        Assert.False(cut.Instance.OkErlaubt);
        Assert.True(cut.Find(".epos-knopf--primaer").HasAttribute("disabled"));

        cut.Find("input[type=text]").Input("Sommerbetrieb");

        Assert.True(cut.Instance.OkErlaubt);
        Assert.False(cut.Find(".epos-knopf--primaer").HasAttribute("disabled"));
    }

    [Fact]
    public void Enter_bestaetigt_nicht_solange_OK_gesperrt_ist()
    {
        bool gemeldet = false;
        var cut = Render<NamensDialog>(p => p
            .Add(x => x.OkNurMitText, true)
            .Add(x => x.Geschlossen, (Action<string?>)(_ => gemeldet = true)));

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.False(gemeldet);

        // Esc bleibt frei - Abbrechen darf nie gesperrt sein.
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.True(gemeldet);
    }
}
