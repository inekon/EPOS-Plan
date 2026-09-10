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
///
/// <para>
/// ANWENDERENTSCHEID 10.09.2026 (Ae25) — OK und Abbrechen: der Primaerknopf
/// heisst seither <c>OkText</c> ("OK"), schreibt UND schliesst die Maske mit
/// <c>true</c>; <c>AbbrechenText</c> schliesst ohne Wirkung mit <c>false</c>.
/// Bis dahin galt A-7 aus B5b — der Primaerknopf hiess "Uebernehmen", schrieb
/// sofort und liess die Maske STEHEN. Drei Faelle unten pruefen das neue
/// Verhalten (<see cref="Ok_uebernimmt_und_schliesst"/>,
/// <see cref="Abbrechen_schliesst_ohne_zu_schreiben"/>,
/// <see cref="Ein_Fehlschlag_haelt_die_Maske_offen_und_zeigt_den_Grund"/>) —
/// die eine Ausnahme bleibt ein Fehlschlag: Er haelt die Maske offen und zeigt
/// seinen Grund im Warnbanner.
/// </para>
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
        // Ae25: der Primaerknopf heisst "OK" (vorher "Uebernehmen", A-7 aus B5b).
        Assert.Equal("OK", cut.Find(".epos-knopf--primaer").TextContent);
        Assert.Equal("Abbrechen", cut.FindAll("button.epos-knopf")[1].TextContent);
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

    /// <summary>
    /// Ae25, die EINE Ausnahme: Ein Fehlschlag des Laufs haelt die Maske offen
    /// und zeigt seinen Grund im Warnbanner — Ersatz fuer den alten Fall
    /// "Die_Meldung_des_Laufs_erscheint_im_Dialog" (A-7 aus B5b), der annahm,
    /// JEDER Lauf (auch der erfolgreiche) bliebe im Dialog stehen und zeigte
    /// seine Meldung dort. Seit Ae25 schliesst ein erfolgreicher Lauf sofort;
    /// nur der Fehlschlag traegt noch eine Meldung im Dialog.
    /// </summary>
    [Fact]
    public void Ein_Fehlschlag_haelt_die_Maske_offen_und_zeigt_den_Grund()
    {
        bool? erfolg = null;
        var cut = Aufbauen(
            beimSchliessen: e => erfolg = e,
            uebernehmen: _ => new VorlagenUebernahmeAntwort(true, "Zielprojekt gesperrt."));

        cut.Find(".epos-knopf--primaer").Click();

        // W16b-O-2/W6-B-2-O-1: nach dem synchronen Click auf den gezeichneten
        // Zustand warten, nicht sofort pruefen.
        cut.WaitForAssertion(() =>
        {
            Assert.Equal("Zielprojekt gesperrt.", cut.Find(".epos-warnbanner-text").TextContent);
            Assert.Contains("fehler", cut.Find(".epos-warnbanner").ClassName);
        });
        Assert.Null(erfolg);   // die Maske ist nicht zu - Geschlossen wurde nicht gerufen.
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

    /// <summary>
    /// Ae25: OK uebernimmt die aktuelle Wahl UND verlaesst die Maske —
    /// Rueckgabe <c>true</c> heisst "der Lauf ist gelaufen und hat
    /// geschrieben". Vorher (A-7 aus B5b) schrieb "Uebernehmen" zwar sofort,
    /// liess die Maske aber offen stehen; erst ein zweiter Knopf schloss sie.
    /// </summary>
    [Fact]
    public void Ok_uebernimmt_und_schliesst()
    {
        bool geschrieben = false;
        bool? erfolg = null;
        var cut = Aufbauen(
            beimSchliessen: e => erfolg = e,
            uebernehmen: _ =>
            {
                geschrieben = true;
                return new VorlagenUebernahmeAntwort(false, "7 Positionen angelegt.");
            });

        cut.Find(".epos-knopf--primaer").Click();

        // W16b-O-2/W6-B-2-O-1: nach dem synchronen Click auf den gezeichneten
        // Zustand warten, nicht sofort pruefen.
        cut.WaitForAssertion(() => Assert.True(erfolg));
        Assert.True(geschrieben);
    }

    /// <summary>
    /// Ae25: Abbrechen verlaesst die Maske OHNE zu schreiben — Rueckgabe
    /// <c>false</c>, und der Uebernehmen-Delegat wird gar nicht erst gerufen.
    /// </summary>
    [Fact]
    public void Abbrechen_schliesst_ohne_zu_schreiben()
    {
        bool geschrieben = false;
        bool? erfolg = null;
        var cut = Aufbauen(
            beimSchliessen: e => erfolg = e,
            uebernehmen: _ =>
            {
                geschrieben = true;
                return new VorlagenUebernahmeAntwort(false, "7 Positionen angelegt.");
            });

        cut.FindAll("button.epos-knopf")[1].Click();

        cut.WaitForAssertion(() => Assert.False(erfolg));
        Assert.False(geschrieben);
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

    // =====================================================================
    //  Das Formularraster — Anwenderwunsch iU8-E-2 / W14a-E-7, Paket P2
    //  (Windows-Abnahme 05.09.2026)
    // =====================================================================


    /// <summary>
    /// <b>iU8-E-2 / W14a-E-7 (Paket P2):</b> Der Feldlauf steht im
    /// <c>Formularraster</c>, und zwar EINSPALTIG: Der Block trägt eine
    /// Reihenfolge — Ziel, dann Quelle, dann die Quelle im Einzelnen —, und
    /// nebeneinander gestellt liest sie sich nicht mehr als Weg.
    /// </summary>
    [Fact]
    public void Der_Feldlauf_steht_im_einspaltigen_Formularraster()
    {
        var cut = Aufbauen();

        Assert.Single(cut.FindAll(".epos-formularraster"));
        Assert.Single(cut.FindAll(".epos-formularraster.epos-formularraster--einspaltig"));
        Assert.True(cut.FindAll(".epos-formularraster .epos-feld").Count >= 4);
    }
}
