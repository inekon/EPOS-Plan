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
/// (2 Optionen), drei Quelllisten, Vorschau, OK/Abbrechen.
///
/// <para><b>ANWENDERENTSCHEID 10.09.2026 (Ae25):</b> „OK = aus der Maske heraus und
/// uebernehmen, Abbrechen = aus der Maske raus, nicht speichern." Die Faelle der
/// Schlussleiste pruefen seither genau das — und die eine benannte Ausnahme, dass ein
/// FEHLSCHLAG die Maske offen haelt.</para>
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
        Assert.Equal("OK", cut.Find(".epos-knopf--primaer").TextContent);
        Assert.Equal("Abbrechen", cut.FindAll("button.epos-knopf")[1].TextContent);
    }

    /// <summary>
    /// <b>Ae25 (10.09.2026):</b> Die Ueberschrift steht nur EINMAL. Als Bereich einer
    /// Ueberlagerung traegt deren Kopf den Titel; die Huelle setzt TitelText dann leer,
    /// und der h1 der Maske entfaellt — der Hilfeknopf bleibt.
    /// </summary>
    [Fact]
    public void Ohne_Titeltext_bleibt_die_Ueberschrift_der_Ueberlagerung_die_einzige()
    {
        var cut = Render<VorlagenUebernahmeDialog>(p => p
            .Add(x => x.TitelText, "")
            .Add(x => x.Zielprojekte, Projekte)
            .Add(x => x.Quellvorlagen, Vorlagen)
            .Add(x => x.Quellprojekte, Projekte));

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Single(cut.FindAll(".epos-infoknopf"));
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

    /// <summary><b>Ae25:</b> Ein gesperrtes OK schreibt auch dann nicht, wenn es
    /// betaetigt wird — die Vorschau hat nichts anzulegen gefunden.</summary>
    [Fact]
    public void Ohne_anlegbare_Positionen_schreibt_OK_nicht_und_schliesst_nicht()
    {
        int laeufe = 0;
        bool geschlossen = false;
        var cut = Aufbauen(beimSchliessen: _ => geschlossen = true,
                           vorschau: _ => new VorlagenUebernahmeVorschau("Die Quelle enthält 0 Positionen.", false),
                           uebernehmen: _ => { laeufe++; return new VorlagenUebernahmeAntwort(false, "x"); });

        Assert.False(cut.Instance.UebernahmeMoeglich);
        Assert.True(cut.Find(".epos-knopf--primaer").HasAttribute("disabled"));

        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(0, laeufe);
        Assert.False(geschlossen);
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
    /// <b>Ae25 (10.09.2026):</b> OK uebernimmt UND verlaesst die Maske; gemeldet wird
    /// <c>true</c>. Die Erfolgsmeldung des Controllers steht nicht mehr im Dialog —
    /// sie erschiene dort in dem Augenblick, in dem er verschwindet; bestaetigt wird in
    /// der Kostenverwaltung (<c>KostenKomponenteDialog.UebernahmeFertigMachen</c>).
    /// Bis zum 10.09.2026 blieb die Maske stehen (A-7 aus B5b).
    /// </summary>
    [Fact]
    public void OK_uebernimmt_und_schliesst_mit_true()
    {
        int laeufe = 0;
        bool? erfolg = null;
        var cut = Aufbauen(beimSchliessen: e => erfolg = e,
                           uebernehmen: _ =>
                           {
                               laeufe++;
                               return new VorlagenUebernahmeAntwort(false, "7 Positionen angelegt, 2 übersprungen.");
                           });

        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(1, laeufe);
        Assert.True(erfolg);
        Assert.Empty(cut.FindAll(".epos-warnbanner"));
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

    /// <summary><b>Ae25:</b> Abbrechen verlaesst die Maske ohne jeden Schreibweg und
    /// meldet <c>false</c> — auch das ist eine Antwort, keine Unterlassung.</summary>
    [Fact]
    public void Abbrechen_schliesst_mit_false_und_schreibt_nicht()
    {
        int laeufe = 0;
        bool? erfolg = null;
        var cut = Aufbauen(beimSchliessen: e => erfolg = e,
                           uebernehmen: _ => { laeufe++; return new VorlagenUebernahmeAntwort(false, "x"); });

        cut.FindAll("button.epos-knopf")[1].Click();

        Assert.Equal(0, laeufe);
        Assert.False(erfolg);
    }

    [Fact]
    public void Esc_schliesst_Enter_nicht()
    {
        // A-7 aus B5b: OK schreibt sofort, Enter bleibt unbelegt (Ae25 aendert daran
        // nichts - Esc ist Abbrechen und meldet deshalb false).
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
