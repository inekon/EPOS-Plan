using Bunit;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Uebernahme Stamm -> Projekt (iU9-W1.4). Soll ist die Feldkarte von
/// <c>Form_VorlagenUebernahme</c>: Kontextzeile, Zielprojekt, Quellgruppe
/// (2 Optionen), drei Quelllisten, Vorschau, Uebernehmen/Abbrechen.
/// </summary>
public class VorlagenUebernahmeDialogTests : BunitContext
{
    private static readonly (int Id, string Text)[] Projekte =
    {
        (1030, "Musterprojekt  [1030]"),
        (1007, "Zweitprojekt  [1007]")
    };

    private static readonly (int Id, string Text)[] Vorlagen =
    {
        (5, "Standard"),
        (9, "Variante Nord")
    };

    public VorlagenUebernahmeDialogTests()
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<VorlagenUebernahmeDialog> Aufbauen(
        Action<bool>? beimSchliessen = null,
        (int Id, string Text)[]? vorlagen = null,
        bool zielWaehlbar = true,
        Func<int, IReadOnlyList<(int Id, string Text)>>? anlagenZu = null,
        Func<VorlagenUebernahmeWahl, VorlagenUebernahmeVorschau>? vorschau = null,
        Func<VorlagenUebernahmeWahl, VorlagenUebernahmeAntwort>? uebernehmen = null)
    {
        return Render<VorlagenUebernahmeDialog>(p => p
            .Add(x => x.KontextText, "BHKW · Betriebskosten")
            .Add(x => x.Zielprojekte, Projekte)
            .Add(x => x.ZielWaehlbar, zielWaehlbar)
            .Add(x => x.Quellvorlagen, vorlagen ?? Vorlagen)
            .Add(x => x.Quellprojekte, Projekte)
            .Add(x => x.AnlagenZu, anlagenZu ?? (_ => new[] { (0, "(ohne Anlagenzuordnung)") }))
            .Add(x => x.Vorschau, vorschau ?? (_ => new VorlagenUebernahmeVorschau("Die Quelle enthält 7 Positionen.", true)))
            .Add(x => x.Uebernehmen, uebernehmen ?? (_ => new VorlagenUebernahmeAntwort(false, "7 Positionen angelegt.")))
            .Add(x => x.Geschlossen, beimSchliessen ?? (_ => { })));
    }

    [Fact]
    public void Der_Feldbestand_der_Karte_steht_vollstaendig()
    {
        var cut = Aufbauen();

        Assert.Equal(4, cut.FindAll("select").Count);          // Ziel + 3 Quelllisten
        Assert.Equal(2, cut.FindAll("input[type=radio]").Count);
        Assert.Equal(2, cut.FindAll("button.epos-knopf").Count);
        Assert.Equal("BHKW · Betriebskosten", cut.Find(".epos-kontextzeile").TextContent);
    }

    [Fact]
    public void Die_Maske_zeigt_die_heutigen_Beschriftungen()
    {
        var cut = Aufbauen();

        Assert.Equal("Übernahme ins Projekt", cut.Find(".epos-dialog-titel").TextContent);
        var texte = cut.FindAll(".epos-feld-text");
        Assert.Equal("Zielprojekt:", texte[0].TextContent);
        Assert.Equal("Aus Vorlage/Variante:", texte[1].TextContent);
        Assert.Equal("Aus Projekt/Anlage:", texte[2].TextContent);
        Assert.Equal("Übernehmen", cut.Find(".epos-knopf--primaer").TextContent);
    }

    [Fact]
    public void Die_Quelle_startet_bei_der_Vorlage_und_sperrt_die_Projektlisten()
    {
        // Auswahl_Geaendert: cmbQuellProjekt/.Anlage folgen rbQuelleProjekt,
        // cmbQuellVorlage folgt rbQuelleVorlage.
        var cut = Aufbauen();

        Assert.True(cut.Instance.AusVorlage);
        var listen = cut.FindAll("select");
        Assert.False(listen[1].HasAttribute("disabled"));   // Quellvorlage
        Assert.True(listen[2].HasAttribute("disabled"));    // Quellprojekt
        Assert.True(listen[3].HasAttribute("disabled"));    // Quellanlage
    }

    [Fact]
    public void Das_Umschalten_auf_Projekt_dreht_die_Sperren_um()
    {
        var cut = Aufbauen();

        cut.FindAll("input[type=radio]")[1].Change(true);

        Assert.False(cut.Instance.AusVorlage);
        var listen = cut.FindAll("select");
        Assert.True(listen[1].HasAttribute("disabled"));
        Assert.False(listen[2].HasAttribute("disabled"));
        Assert.False(listen[3].HasAttribute("disabled"));
    }

    [Fact]
    public void Ohne_Vorlage_ist_die_Quelle_Projekt_und_die_Vorlagenoption_gesperrt()
    {
        // rbQuelleVorlage.Enabled = _vorlagen.Count > 0;
        // if (_vorlagen.Count == 0) rbQuelleProjekt.Checked = true;
        var cut = Aufbauen(vorlagen: Array.Empty<(int, string)>());

        Assert.False(cut.Instance.AusVorlage);
        Assert.True(cut.FindAll("input[type=radio]")[0].HasAttribute("disabled"));
        // A-12: Anders als in WinForms sind die Projektlisten dabei bedienbar.
        Assert.False(cut.FindAll("select")[2].HasAttribute("disabled"));
    }

    [Fact]
    public void Ein_festes_Zielprojekt_ist_gesperrt()
    {
        // cmbZielProjekt.Enabled = zielProjektId <= 0 (Projektmodus).
        var cut = Aufbauen(zielWaehlbar: false);

        Assert.True(cut.FindAll("select")[0].HasAttribute("disabled"));
    }

    [Fact]
    public void Die_Vorschau_steht_von_Anfang_an()
    {
        var cut = Aufbauen();

        Assert.Equal("Die Quelle enthält 7 Positionen.", cut.Find(".epos-herleitung-text").TextContent);
    }

    [Fact]
    public void Jede_Aenderung_zieht_die_Vorschau_neu()
    {
        int laeufe = 0;
        var cut = Aufbauen(vorschau: _ =>
        {
            laeufe++;
            return new VorlagenUebernahmeVorschau("Lauf " + laeufe, true);
        });

        int nachAufbau = laeufe;
        cut.FindAll("select")[0].Change("1007");
        cut.FindAll("input[type=radio]")[1].Change(true);

        Assert.Equal(nachAufbau + 2, laeufe);
    }

    [Fact]
    public void Der_Projektwechsel_zieht_die_Anlagenliste_nach()
    {
        // QuellProjekt_Geaendert -> QuellAnlagenFuellen.
        var cut = Aufbauen(anlagenZu: projekt => projekt == 1007
            ? new[] { (77, "BHKW — Nord"), (0, "(ohne Anlagenzuordnung)") }
            : new[] { (0, "(ohne Anlagenzuordnung)") });

        cut.FindAll("input[type=radio]")[1].Change(true);
        cut.FindAll("select")[2].Change("1007");

        var anlagen = cut.FindAll("select")[3].QuerySelectorAll("option");
        Assert.Equal(2, anlagen.Length);
        Assert.Equal("BHKW — Nord", anlagen[0].TextContent);
        Assert.Equal(77, cut.Instance.QuellAnlage);   // der erste Eintrag ist gewaehlt
    }

    [Fact]
    public void Der_Uebernehmen_Knopf_folgt_der_Regel_der_Huelle()
    {
        var cut = Aufbauen(vorschau: wahl =>
            new VorlagenUebernahmeVorschau("", wahl.AusVorlage));

        Assert.True(cut.Instance.UebernahmeMoeglich);
        Assert.False(cut.Find(".epos-knopf--primaer").HasAttribute("disabled"));

        cut.FindAll("input[type=radio]")[1].Change(true);

        Assert.False(cut.Instance.UebernahmeMoeglich);
        Assert.True(cut.Find(".epos-knopf--primaer").HasAttribute("disabled"));
    }

    [Fact]
    public void Uebernehmen_gibt_der_Huelle_die_ganze_Wahl()
    {
        VorlagenUebernahmeWahl? erhalten = null;
        var cut = Aufbauen(uebernehmen: wahl =>
        {
            erhalten = wahl;
            return new VorlagenUebernahmeAntwort(false, "fertig");
        });

        cut.FindAll("select")[0].Change("1007");
        cut.FindAll("select")[1].Change("9");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.NotNull(erhalten);
        Assert.True(erhalten!.AusVorlage);
        Assert.Equal(1007, erhalten.ZielProjektId);
        Assert.Equal(9, erhalten.QuellVorlageId);
    }

    [Fact]
    public void Die_Meldung_des_Laufs_erscheint_im_Dialog()
    {
        var cut = Aufbauen(uebernehmen: _ =>
            new VorlagenUebernahmeAntwort(false, "7 Positionen angelegt, 2 übersprungen."));

        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal("7 Positionen angelegt, 2 übersprungen.",
                     cut.Find(".epos-warnbanner-text").TextContent);
        Assert.Contains("hinweis", cut.Find(".epos-warnbanner").ClassName);
    }

    [Fact]
    public void Ein_Fehler_erscheint_als_Fehlerbanner_und_der_Dialog_bleibt_offen()
    {
        bool geschlossen = false;
        var cut = Aufbauen(beimSchliessen: _ => geschlossen = true,
                           uebernehmen: _ => new VorlagenUebernahmeAntwort(true, "Zielprojekt gesperrt."));

        cut.Find(".epos-knopf--primaer").Click();

        Assert.False(geschlossen);
        Assert.Contains("fehler", cut.Find(".epos-warnbanner").ClassName);
    }

    [Fact]
    public void Schliessen_meldet_ob_uebernommen_wurde()
    {
        bool? erfolg = null;
        var cut = Aufbauen(beimSchliessen: e => erfolg = e);

        cut.FindAll("button.epos-knopf")[1].Click();
        Assert.False(erfolg);

        var zweiter = Aufbauen(beimSchliessen: e => erfolg = e);
        zweiter.Find(".epos-knopf--primaer").Click();
        zweiter.FindAll("button.epos-knopf")[1].Click();
        Assert.True(erfolg);
    }

    [Fact]
    public void Esc_schliesst_Enter_nicht()
    {
        // A-7 aus B5b: Uebernehmen schreibt sofort, Enter bleibt unbelegt.
        int gemeldet = 0;
        var cut = Aufbauen(beimSchliessen: _ => gemeldet++);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(0, gemeldet);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(1, gemeldet);
    }

    [Fact]
    public void Der_Hilfeknopf_traegt_den_Schluessel_der_Maske()
    {
        var hilfe = new TestHilfe();
        Services.AddSingleton<IHilfeDienst>(hilfe);

        var cut = Aufbauen();
        cut.Find(".epos-infoknopf").Click();

        Assert.Equal(new[] { "Form_VorlagenUebernahme.btn_Help" }, hilfe.Geoeffnet);
    }
}
