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
/// (2 Optionen), Quelllisten, Vorschau, OK/Abbrechen.
///
/// <para><b>ANWENDERENTSCHEID 10.09.2026 (Ae25):</b> „OK = aus der Maske heraus und
/// uebernehmen, Abbrechen = aus der Maske raus, nicht speichern." Die Faelle der
/// Schlussleiste pruefen seither genau das — und die eine benannte Ausnahme, dass ein
/// FEHLSCHLAG die Maske offen haelt.</para>
///
/// <para><b>ANWENDERENTSCHEID 19.09.2026:</b> Unter „Aus Vorlage/Variante" steht der
/// KATALOGBLOCK der Administration — Komponente, Kategorie, Variante, Positionen der
/// Variante als Vorschau. Die schlichte Klappliste „Vorlage/Variante" ist fort.</para>
/// </summary>
public class VorlagenUebernahmeDialogTests : BunitContext
{
    private static readonly (int Id, string Text)[] Projekte =
    {
        (1030, "Musterprojekt  [1030]"),
        (1007, "Zweitprojekt  [1007]")
    };

    /// <summary>Die Varianten der Investitionsseite; Standard zuerst (wie der Katalog).</summary>
    private static readonly (int Id, string Text)[] VorlagenInvest =
    {
        (5, "Standard"),
        (9, "Variante Nord")
    };

    /// <summary>Die Varianten der Betriebsseite.</summary>
    private static readonly (int Id, string Text)[] VorlagenBetrieb =
    {
        (7, "Standard Betrieb")
    };

    private static readonly (int Id, string Text)[] Keine = Array.Empty<(int, string)>();

    private static IReadOnlyList<VorlagenPositionZeile> Positionen(VorlagenUebernahmeWahl wahl)
        => wahl.QuellVorlageId switch
        {
            5 => new[]
            {
                new VorlagenPositionZeile("Gerätekosten", "je kW Leistung", "850 €/kW",
                                          "20", "wird angelegt", false),
                new VorlagenPositionZeile("Montage", "fester Betrag", "1.200 €",
                                          "20", "vorhanden", true)
            },
            9 => new[]
            {
                new VorlagenPositionZeile("Gerätekosten Nord", "je kW Leistung", "910 €/kW",
                                          "20", "wird angelegt", false)
            },
            7 => new[]
            {
                new VorlagenPositionZeile("Wartung", "fester Jahresbetrag", "600 €/a",
                                          "", "wird angelegt", false)
            },
            _ => Array.Empty<VorlagenPositionZeile>()
        };

    public VorlagenUebernahmeDialogTests()
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<VorlagenUebernahmeDialog> Aufbauen(
        Action<VorlagenUebernahmeSchluss>? beimSchliessen = null,
        Func<bool, IReadOnlyList<(int Id, string Text)>>? vorlagenZu = null,
        Func<VorlagenUebernahmeWahl, IReadOnlyList<VorlagenPositionZeile>>? positionenZu = null,
        bool investVorwahl = true,
        bool zielWaehlbar = true,
        Func<int, IReadOnlyList<(int Id, string Text)>>? anlagenZu = null,
        Func<VorlagenUebernahmeWahl, VorlagenUebernahmeVorschau>? vorschau = null,
        Func<VorlagenUebernahmeWahl, VorlagenUebernahmeAntwort>? uebernehmen = null)
    {
        return Render<VorlagenUebernahmeDialog>(p => p
            .Add(x => x.KontextText, "BHKW · Investitionskosten")
            .Add(x => x.KomponenteText, "BHKW")
            .Add(x => x.Zielprojekte, Projekte)
            .Add(x => x.ZielWaehlbar, zielWaehlbar)
            .Add(x => x.InvestVorwahl, investVorwahl)
            .Add(x => x.VorlagenZu, vorlagenZu ?? (invest => invest ? VorlagenInvest : VorlagenBetrieb))
            .Add(x => x.PositionenZu, positionenZu ?? Positionen)
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

        // Ziel + Variante + Quellprojekt + Quellanlage.
        Assert.Equal(4, cut.FindAll("select").Count);
        // Zwei Quellen und zwei Kategorien.
        Assert.Equal(4, cut.FindAll("input[type=radio]").Count);
        Assert.Equal(2, cut.FindAll(".epos-leiste button.epos-knopf").Count);
        Assert.Equal("BHKW · Investitionskosten", cut.Find(".epos-kontextzeile").TextContent);
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
        Assert.Equal("Komponente:", texte[3].TextContent);
        Assert.Equal("Betriebskosten", texte[4].TextContent);
        Assert.Equal("Investitionskosten", texte[5].TextContent);
        Assert.Equal("Variante:", texte[6].TextContent);
        Assert.Equal("OK", cut.Find(".epos-knopf--primaer").TextContent);
        Assert.Equal("Abbrechen", cut.FindAll(".epos-leiste button.epos-knopf")[1].TextContent);
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
            .Add(x => x.VorlagenZu, invest => invest ? VorlagenInvest : VorlagenBetrieb)
            .Add(x => x.Quellprojekte, Projekte));

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Single(cut.FindAll(".epos-infoknopf"));
    }

    [Fact]
    public void Die_Quelle_startet_bei_der_Vorlage_und_sperrt_die_Projektlisten()
    {
        // Auswahl_Geaendert: cmbQuellProjekt/.Anlage folgen rbQuelleProjekt.
        var cut = Aufbauen();

        Assert.True(cut.Instance.AusVorlage);
        var listen = cut.FindAll("select");
        Assert.False(listen[1].HasAttribute("disabled"));   // Variante im Katalogblock
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
        Assert.Equal(3, listen.Count);                      // der Katalogblock ist fort
        Assert.False(listen[1].HasAttribute("disabled"));   // Quellprojekt
        Assert.False(listen[2].HasAttribute("disabled"));   // Quellanlage
    }

    /// <summary>
    /// <b>ANWENDERBEFUND 19.09.2026:</b> Der Anwender fand die Option „Aus
    /// Vorlage/Variante" ABGEWAEHLT vor und die Variantenliste grau. Dieser Fall haelt
    /// den Klickweg fest: Fuehrt der Katalog eine Variante, ist die Option vorbelegt,
    /// NICHT gesperrt, und ein Wechsel hin und zurueck bringt den Katalogblock wieder.
    /// Ein Fehler im Klickweg der <c>Optionsgruppe</c> liesse sich hier sehen.
    /// </summary>
    [Fact]
    public void Die_Vorlagenoption_ist_vorbelegt_und_laesst_sich_zurueckwaehlen()
    {
        var cut = Aufbauen();

        Assert.True(cut.Instance.AusVorlage);
        Assert.False(cut.FindAll("input[type=radio]")[0].HasAttribute("disabled"));
        Assert.Empty(cut.FindAll(".epos-option-beschreibung"));

        cut.FindAll("input[type=radio]")[1].Change(true);
        Assert.False(cut.Instance.AusVorlage);
        Assert.Empty(cut.FindAll(".epos-zeilenraster"));

        cut.FindAll("input[type=radio]")[0].Change(true);
        Assert.True(cut.Instance.AusVorlage);
        Assert.Single(cut.FindAll(".epos-zeilenraster"));
        Assert.Equal(5, cut.Instance.QuellVorlage);
    }

    /// <summary>
    /// <b>19.09.2026:</b> Ohne Variante in BEIDEN Kategorien bleibt die Option
    /// gesperrt — aber sie erklaert sich jetzt, statt still zu sein. Ein Anwender darf
    /// eine gesperrte Option nicht fuer eine bloss nicht gewaehlte halten.
    /// </summary>
    [Fact]
    public void Ohne_Vorlage_ist_die_Quelle_Projekt_und_die_Vorlagenoption_erklaert_gesperrt()
    {
        // rbQuelleVorlage.Enabled = _vorlagen.Count > 0;
        // if (_vorlagen.Count == 0) rbQuelleProjekt.Checked = true;
        var cut = Aufbauen(vorlagenZu: _ => Keine);

        Assert.False(cut.Instance.AusVorlage);
        Assert.True(cut.FindAll("input[type=radio]")[0].HasAttribute("disabled"));
        Assert.Contains("Administration", cut.Find(".epos-option-beschreibung").TextContent);
        // A-12: Anders als in WinForms sind die Projektlisten dabei bedienbar.
        Assert.False(cut.FindAll("select")[1].HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>19.09.2026:</b> Fuehrt der Katalog nur in der ANDEREN Kategorie eine
    /// Variante, ist die Option waehlbar und die Kategorie-Vorwahl springt dorthin.
    /// </summary>
    [Fact]
    public void Fuehrt_nur_die_andere_Kategorie_eine_Variante_springt_die_Vorwahl()
    {
        var cut = Aufbauen(vorlagenZu: invest => invest ? Keine : VorlagenBetrieb);

        Assert.True(cut.Instance.AusVorlage);
        Assert.False(cut.Instance.Invest);
        Assert.Equal(7, cut.Instance.QuellVorlage);
        Assert.Empty(cut.FindAll(".epos-option-beschreibung"));
        // Die Betriebsseite kennt keine Nutzungsdauer — die Spalte fehlt.
        Assert.Equal(4, cut.FindAll(".epos-zr-kopfzelle").Count);
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

        Assert.Equal("Die Quelle enthält 7 Positionen.",
                     cut.FindAll(".epos-herleitung-text")[^1].TextContent);
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
        cut.FindAll("select")[1].Change("1007");

        var anlagen = cut.FindAll("select")[2].QuerySelectorAll("option");
        Assert.Equal(2, anlagen.Length);
        Assert.Equal("BHKW — Nord", anlagen[0].TextContent);
        Assert.Equal(77, cut.Instance.QuellAnlage);   // der erste Eintrag ist gewaehlt
    }

    // =====================================================================
    //  Der Katalogblock — Anwenderentscheid 19.09.2026
    // =====================================================================

    /// <summary>Der Block steht NUR unter der gewaehlten Option.</summary>
    [Fact]
    public void Der_Katalogblock_steht_nur_unter_der_gewaehlten_Option()
    {
        var cut = Aufbauen();

        Assert.Single(cut.FindAll(".epos-zeilenraster"));
        Assert.Equal("BHKW", cut.Find(".epos-lesewert").TextContent);

        cut.FindAll("input[type=radio]")[1].Change(true);

        Assert.Empty(cut.FindAll(".epos-zeilenraster"));
        Assert.Empty(cut.FindAll(".epos-lesewert"));
        Assert.Equal(2, cut.FindAll("input[type=radio]").Count);
    }

    /// <summary>
    /// Ein Kategoriewechsel laedt die Variantenliste dieser Kategorie und setzt die
    /// Vorwahl auf die erste Zeile — das ist die Standardvariante.
    /// </summary>
    [Fact]
    public void Der_Kategoriewechsel_laedt_die_Varianten_und_waehlt_die_erste()
    {
        var cut = Aufbauen();
        cut.FindAll("select")[1].Change("9");
        Assert.Equal(9, cut.Instance.QuellVorlage);

        // Die Kategorien stehen als drittes und viertes Optionsfeld: Betrieb, Invest.
        cut.FindAll("input[type=radio]")[2].Change(true);

        Assert.False(cut.Instance.Invest);
        Assert.Equal(7, cut.Instance.QuellVorlage);
        var varianten = cut.FindAll("select")[1].QuerySelectorAll("option");
        Assert.Single(varianten);
        Assert.Equal("Standard Betrieb", varianten[0].TextContent);
    }

    [Fact]
    public void Die_Vorschau_zeigt_die_Positionen_der_gewaehlten_Variante()
    {
        var cut = Aufbauen();

        Assert.Equal(2, cut.FindAll(".epos-zr-zeile").Count);
        var zellen = cut.FindAll(".epos-zr-zeile")[0].QuerySelectorAll(".epos-zr-zelle");
        Assert.Equal(5, zellen.Length);
        Assert.Equal("Gerätekosten", zellen[0].TextContent);
        Assert.Equal("je kW Leistung", zellen[1].TextContent);
        Assert.Equal("850 €/kW", zellen[2].TextContent);
        Assert.Equal("20", zellen[3].TextContent);

        cut.FindAll("select")[1].Change("9");

        Assert.Single(cut.FindAll(".epos-zr-zeile"));
        Assert.Equal("Gerätekosten Nord",
                     cut.Find(".epos-zr-zeile .epos-zr-zelle").TextContent);
    }

    /// <summary>
    /// <b>ANWENDERBEFUND 19.09.2026:</b> „Die Quelle enthaelt 7 Positionen, das Ziel
    /// fuehrt bereits 7" sagt nicht, WELCHE fehlen. Die Zeile sagt es.
    /// </summary>
    [Fact]
    public void Die_Vorschauzeilen_kennzeichnen_vorhandene_und_neue_Positionen()
    {
        var cut = Aufbauen();

        var zeilen = cut.FindAll(".epos-zr-zeile");
        Assert.Equal("wird angelegt",
                     zeilen[0].QuerySelectorAll(".epos-zr-zelle")[4].TextContent);
        Assert.Equal("vorhanden",
                     zeilen[1].QuerySelectorAll(".epos-zr-zelle")[4].TextContent);
    }

    [Fact]
    public void Eine_leere_Vorlage_zeigt_eine_Erklaerzeile_statt_leerer_Tabelle()
    {
        var cut = Aufbauen(positionenZu: _ => Array.Empty<VorlagenPositionZeile>());

        Assert.Empty(cut.FindAll(".epos-zeilenraster"));
        Assert.Equal("Diese Vorlage führt keine Positionen.",
                     cut.FindAll(".epos-herleitung-text")[0].TextContent);
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
        Assert.True(erhalten.Invest);
    }

    /// <summary>
    /// <b>19.09.2026:</b> OK reicht Vorlage UND Kategorie durch — nach einem
    /// Kategoriewechsel also die Variante der anderen Seite und deren Kategorie.
    /// Der Schluss meldet dieselbe Kategorie, damit der Wirt auf sie umschaltet.
    /// </summary>
    [Fact]
    public void OK_reicht_Vorlage_und_Kategorie_durch()
    {
        VorlagenUebernahmeWahl? erhalten = null;
        VorlagenUebernahmeSchluss? schluss = null;
        var cut = Aufbauen(beimSchliessen: s => schluss = s,
                           uebernehmen: wahl =>
                           {
                               erhalten = wahl;
                               return new VorlagenUebernahmeAntwort(false, "fertig");
                           });

        cut.FindAll("input[type=radio]")[2].Change(true);   // Betriebskosten
        cut.Find(".epos-knopf--primaer").Click();

        Assert.NotNull(erhalten);
        Assert.Equal(7, erhalten!.QuellVorlageId);
        Assert.False(erhalten.Invest);
        Assert.NotNull(schluss);
        Assert.True(schluss!.Geschrieben);
        Assert.False(schluss.Invest);
    }

    /// <summary>
    /// <b>Ae25 (10.09.2026):</b> OK uebernimmt UND verlaesst die Maske; gemeldet wird
    /// <c>Geschrieben</c>. Die Erfolgsmeldung des Controllers steht nicht mehr im
    /// Dialog — sie erschiene dort in dem Augenblick, in dem er verschwindet;
    /// bestaetigt wird in der Kostenverwaltung
    /// (<c>KostenKomponenteDialog.UebernahmeFertigMachen</c>).
    /// </summary>
    [Fact]
    public void OK_uebernimmt_und_schliesst_mit_true()
    {
        int laeufe = 0;
        VorlagenUebernahmeSchluss? schluss = null;
        var cut = Aufbauen(beimSchliessen: e => schluss = e,
                           uebernehmen: _ =>
                           {
                               laeufe++;
                               return new VorlagenUebernahmeAntwort(false, "7 Positionen angelegt, 2 übersprungen.");
                           });

        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(1, laeufe);
        Assert.True(schluss?.Geschrieben);
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
        VorlagenUebernahmeSchluss? schluss = null;
        var cut = Aufbauen(beimSchliessen: e => schluss = e,
                           uebernehmen: _ => { laeufe++; return new VorlagenUebernahmeAntwort(false, "x"); });

        cut.FindAll(".epos-leiste button.epos-knopf")[1].Click();

        Assert.Equal(0, laeufe);
        Assert.False(schluss?.Geschrieben);
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

    /// <summary>Anwenderentscheid 15.09.2026: Das Kreuz im Kopf wirkt wie Esc/Abbrechen.</summary>
    [Fact]
    public void Das_Kreuz_schliesst_wie_Esc()
    {
        int gemeldet = 0;
        var cut = Aufbauen(beimSchliessen: _ => gemeldet++);

        cut.Find(".epos-dialog-zu").Click();

        Assert.Equal(1, gemeldet);
    }

    [Fact]
    public void Ohne_Titeltext_zeigt_der_Kopf_kein_Kreuz()
    {
        var cut = Render<VorlagenUebernahmeDialog>(p => p
            .Add(x => x.TitelText, "")
            .Add(x => x.Zielprojekte, Projekte)
            .Add(x => x.VorlagenZu, invest => invest ? VorlagenInvest : VorlagenBetrieb)
            .Add(x => x.Quellprojekte, Projekte));

        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
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
