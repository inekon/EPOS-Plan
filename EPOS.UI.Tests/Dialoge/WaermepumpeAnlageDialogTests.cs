using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Waermepumpe;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Detailansicht einer Wärmepumpen-Anlage (iU9-W7.4). Soll ist die Feldkarte von
/// <c>Wizard_WPItem</c>: 47 Zeilen in drei Gruppen, zwei Reiterblätter mit den
/// Kennlinienbildern, die Kostenzeile — und OHNE die Pufferspeichergruppe (Ä19).
/// </summary>
public class WaermepumpeAnlageDialogTests : BunitContext
{
    private static readonly byte[] BildCop = { 1, 2, 3 };
    private static readonly byte[] BildLeistung = { 4, 5, 6 };

    private static readonly WaermepumpeStammZeile[] Stammliste =
    {
        new(1, "WP Alpha", false),
        new(2, "WP Beta", false)
    };

    public WaermepumpeAnlageDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Eine vollständig ausgefüllte Anlagenzeile — so kommt sie aus der Verwaltung.</summary>
    private static WaermepumpeAnlageDaten Voll() => new()
    {
        Bezeichner = "WP Alpha",
        IdWp = 77,                      // PROJEKT-Geräte-Id, nicht die Stamm-Id
        Vorlauf = 35,
        Ruecklauf = 28,
        Heizstab = true,
        HeizstabLeistung = 6,
        Sperrung = false,
        SperrzeitVon = 0,
        SperrzeitBis = 0,
        Nutzungszeit = 24,
        BivalenterBetrieb = false,
        Betriebsart = "",
        Abschaltpunkt = -5,
        Beschreibung = "Testgerät",
        Baujahr = 2023,
        Regelung = "stetig",
        Typ = "Luft-Wasser",
        Firma = "Alpha",
        Nennleistung = 12,
        Modulkosten = 4000,
        Volumen = 1.5,
        Solaranteil = 30,
        RendeMix = true
    };

    private static WaermepumpeStammDaten Stamm(int id) => new()
    {
        Id = id,
        Name = id == 1 ? "WP Alpha" : "WP Beta",
        Firma = id == 1 ? "Alpha" : "Beta",
        Beschreibung = id == 1 ? "Testgerät" : "Zweitgerät",
        Typ = id == 1 ? "Luft-Wasser" : "Sole-Wasser",
        Baujahr = id == 1 ? 2023 : 2019,
        Regelung = id == 1 ? "stetig" : "einstufig",
        Nennleistung = id == 1 ? 12 : 25,
        Heizstab = id == 1 ? 6 : 9,
        Modulkosten = id == 1 ? 4000 : 7000
    };

    private IRenderedComponent<WaermepumpeAnlageDialog> Aufbauen(
        WaermepumpeAnlageDaten? daten = null,
        Func<int?, int?, string?>? temperaturen = null,
        Func<bool>? kostenBereit = null,
        Func<(double, double)>? kostensumme = null,
        Func<Task>? kostenOeffnen = null,
        Func<IReadOnlyDictionary<string, object>>? stammGaben = null,
        Action<bool>? geschlossen = null)
        => Render<WaermepumpeAnlageDialog>(p => p
            .Add(x => x.Daten, daten ?? Voll())
            .Add(x => x.Stammliste, () => Stammliste)
            .Add(x => x.Vorlaeufe, _ => new[] { 35, 45, 55 })
            .Add(x => x.Bilder, _ => new KennlinienBilder(BildCop, BildLeistung))
            .Add(x => x.Stammdaten, Stamm)
            .Add(x => x.TemperaturenPruefen, temperaturen ?? ((v, r) =>
                (v is null || r is null || v <= r) ? "Die Vorlauftemperatur muss über der Rücklauftemperatur liegen." : null))
            .Add(x => x.KostenBereit, kostenBereit ?? (() => true))
            .Add(x => x.Kostensumme, kostensumme ?? (() => (12000d, 340d)))
            .Add(x => x.KostenOeffnen, kostenOeffnen)
            .Add(x => x.Katalog, () => new[]
            {
                new WaermepumpenKatalogZeile("Beta", "WP Beta", "Split", "Außen",
                                             60, 35, 25, 9, "Sole-Wasser", "einstufig", "Heizen")
            })
            .Add(x => x.StammGaben, stammGaben)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IElement Knopf(IRenderedComponent<WaermepumpeAnlageDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Die_drei_Gruppen_und_die_zwei_Reiter_stehen()
    {
        var cut = Aufbauen();

        var gruppen = cut.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Wärmepumpen Kenndaten", "Auslegung für Verteilung",
                             "Spitzenlast und Betrieb" }, gruppen);

        var reiter = cut.FindAll(".epos-reiter-knopf").Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "COP", "Leistung" }, reiter);
    }

    [Fact]
    public void Die_Pufferspeichergruppe_wird_gar_nicht_gezeichnet()
    {
        // Ä19: Volumen, Kapazitaet, Anteil Solaranlage und rende MIX laufen im
        // Datensatz mit, gepflegt wird der Puffer in der Simulation-Konfiguration.
        var cut = Aufbauen();
        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();

        Assert.DoesNotContain("Volumen", texte);
        Assert.DoesNotContain("Kapazität", texte);
        Assert.DoesNotContain("Anteil Speicher für Solaranlage", texte);
        Assert.DoesNotContain(cut.FindAll(".epos-schalter").Select(e => e.TextContent),
                              t => t.Contains("rende MIX"));
    }

    [Fact]
    public void Die_Stammfelder_sind_nur_lesbar()
    {
        var cut = Aufbauen();
        var gruppe = cut.FindAll(".epos-gruppenkopf-koerper")[0];

        // Beschreibung, Hersteller, Typ, Regelung, Baujahr, Nennleistung.
        Assert.Equal(6, gruppe.QuerySelectorAll("input[readonly]").Length);
        // Der Heizstab ist das EINZIGE bearbeitbare Feld der Gruppe.
        Assert.Single(gruppe.QuerySelectorAll("input:not([readonly])"));
    }

    [Fact]
    public void Die_Beschriftungen_stehen_wie_im_Designer()
    {
        var cut = Aufbauen();
        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        var schalter = cut.FindAll(".epos-schalter").Select(e => e.TextContent.Trim()).ToList();

        foreach (string soll in new[]
                 {
                     "Bezeichnung", "Hersteller", "Wärmepumpentyp", "Leistungsstufen",
                     "Baujahr", "Nennleistung", "Heizstab", "Vorlauf", "Rücklauf",
                     "Sperrzeit von", "Sperrzeit bis", "Nutzungsdauer"
                 })
            Assert.Contains(soll, texte);

        Assert.Contains("Wärmeerzeuger Spitzenlast:", schalter);
        Assert.Contains("Wärmepumpenleistung / maximale Betriebszeit:", schalter);
        Assert.Contains("Bivalenter Betrieb", schalter);
    }

    /// <summary>Die Maske ist lokalisiert (39 englische Texte, W7.9).</summary>
    [Fact]
    public void Die_englischen_Texte_lassen_sich_setzen()
    {
        var cut = Render<WaermepumpeAnlageDialog>(p => p
            .Add(x => x.Daten, Voll())
            .Add(x => x.Stammliste, () => Stammliste)
            .Add(x => x.TitelText, "Detail view")
            .Add(x => x.LabelBivalent, "Bivalent operation")
            .Add(x => x.LabelNutzungszeit, "Duration of use"));

        Assert.Equal("Detail view", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Duration of use", cut.FindAll(".epos-feld-text").Select(e => e.TextContent));
        Assert.Contains(cut.FindAll(".epos-schalter").Select(e => e.TextContent.Trim()),
                        t => t == "Bivalent operation");
    }

    // =================================================================================
    // Die stille Vorwahl (Ä21) und die Nutzerwahl (Ä23)
    // =================================================================================

    [Fact]
    public void Der_Aufbau_laesst_die_Projekt_Geraete_Id_stehen()
    {
        // Ä21: Der Vorlaeufer musste sich dafuer mit m_bStilleFuellung gegen seinen
        // eigenen Auswahl-Handler wehren. Hier gibt es das Problem nicht.
        var daten = Voll();
        Aufbauen(daten);
        Assert.Equal(77, daten.IdWp);
    }

    [Fact]
    public void Eine_Nutzerwahl_wechselt_die_Id_UND_die_Stammfelder()
    {
        // Ä23: Sonst truege das Listenobjekt nach einem Wechsel weiter die
        // Nennleistung der vorherigen Wahl, und die Verwaltungsliste zeigte 0 kW.
        var daten = Voll();
        var cut = Aufbauen(daten);

        cut.FindAll(".epos-raster tbody tr button")[1].Click();   // WP Beta

        Assert.Equal(2, daten.IdWp);
        Assert.Equal("WP Beta", daten.Bezeichner);
        Assert.Equal(25, daten.Nennleistung);
        Assert.Equal("einstufig", daten.Regelung);
        Assert.Equal(7000, daten.Modulkosten);
    }

    [Fact]
    public void Die_Vorlaufliste_kommt_aus_den_Kennlinien()
    {
        var cut = Aufbauen();
        var stufen = cut.FindAll(".epos-gruppenkopf-koerper")[1]
                        .QuerySelectorAll("select option").Select(o => o.TextContent).ToList();
        Assert.Equal(new[] { "35", "45", "55" }, stufen);
    }

    [Fact]
    public void Ein_Vorlauf_ausserhalb_der_Kennlinien_bleibt_stehen()
    {
        // A-16: Der Vorlaeufer hatte eine frei beschreibbare ComboBox.
        var daten = Voll();
        daten.Vorlauf = 60;
        var cut = Aufbauen(daten);

        var stufen = cut.FindAll(".epos-gruppenkopf-koerper")[1]
                        .QuerySelectorAll("select option").Select(o => o.TextContent).ToList();
        Assert.Equal(new[] { "60", "35", "45", "55" }, stufen);
    }

    [Fact]
    public void Der_Ruecklauf_bleibt_frei_eingebbar_und_nennt_die_Vorschlaege()
    {
        // A-18: RUECKLAUF_VORSCHLAEGE ist ausdruecklich eine Vorschlagsliste ohne
        // Grenzwirkung - fuer 35/28 gab es frueher gar keinen Eintrag.
        var daten = Voll();
        var cut = Aufbauen(daten);

        var auslegung = cut.FindAll(".epos-gruppenkopf-koerper")[1];
        auslegung.QuerySelectorAll("input")[0].Input("26");
        Assert.Equal(26, daten.Ruecklauf);

        Assert.Contains("20, 22, 25, 28, 30, 32, 35, 40, 45",
                        cut.FindAll(".epos-herleitung").Select(e => e.TextContent).First(t => t.Contains("20, 22")));
    }

    // =================================================================================
    // Sichtbarkeitsregeln
    // =================================================================================

    [Fact]
    public void Die_Betriebsart_erscheint_erst_mit_bivalentem_Betrieb()
    {
        var cut = Aufbauen();
        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.DoesNotContain("Betriebsart", texte);

        cut.FindAll(".epos-schalter input[type=checkbox]")[2].Change(true);   // Bivalenter Betrieb

        Assert.Contains("Betriebsart", cut.FindAll(".epos-feld-text").Select(e => e.TextContent));
    }

    [Fact]
    public void Die_Bivalenztemperatur_erscheint_nur_wo_sie_rechenwirksam_ist()
    {
        var daten = Voll();
        daten.BivalenterBetrieb = true;
        var cut = Aufbauen(daten);

        // Ohne Betriebsart: kein Feld.
        Assert.DoesNotContain("Bivalenztemperatur",
                              cut.FindAll(".epos-feld-text").Select(e => e.TextContent));

        IElement Betriebsart() => cut.FindAll(".epos-gruppenkopf-koerper")[2].QuerySelectorAll("select")[0];

        // Parallelbetrieb wertet den Abschaltpunkt NICHT aus - kein Feld.
        Betriebsart().Change("1");
        Assert.DoesNotContain("Bivalenztemperatur",
                              cut.FindAll(".epos-feld-text").Select(e => e.TextContent));

        // Alternativ- und Teilparallelbetrieb werten ihn aus.
        Betriebsart().Change("0");
        Assert.Contains("Bivalenztemperatur", cut.FindAll(".epos-feld-text").Select(e => e.TextContent));

        Betriebsart().Change("2");
        Assert.Contains("Bivalenztemperatur", cut.FindAll(".epos-feld-text").Select(e => e.TextContent));
    }

    [Fact]
    public void Die_Betriebsarten_sind_die_Steuerwerte_aus_DbWerte()
    {
        var daten = Voll();
        daten.BivalenterBetrieb = true;
        var cut = Aufbauen(daten);

        var werte = cut.FindAll(".epos-gruppenkopf-koerper")[2]
                       .QuerySelectorAll("select option").Select(o => o.TextContent).ToList();

        // Der leere erste Eintrag ist der Platzhalter - er entspricht der leeren
        // ComboBox des Vorlaeufers, bei der btn_Beenden "Bitte Betriebsart
        // auswaehlen!" meldete.
        Assert.Equal(new[] { "", DbWerte.WP_BETRIEBSART_ALTERNATIV,
                             DbWerte.WP_BETRIEBSART_PARALLEL,
                             DbWerte.WP_BETRIEBSART_TEILPARALLEL }, werte);
    }

    // =================================================================================
    // Kostenzeile
    // =================================================================================

    [Fact]
    public void Ohne_Delegat_gibt_es_keinen_Kostenknopf()
    {
        var cut = Aufbauen();
        Assert.Empty(cut.FindAll(".epos-kostenleiste button"));
    }

    [Fact]
    public void Mit_Anlagenzeile_zeigt_die_Kostenzeile_die_Summen()
    {
        var cut = Aufbauen(kostenOeffnen: () => Task.CompletedTask);

        Assert.False(cut.Find(".epos-kostenleiste button").HasAttribute("disabled"));
        Assert.Equal("Invest 12.000 € · Betrieb 340 €/a",
                     cut.Find(".epos-kostenleiste-hinweis").TextContent.Trim());
    }

    [Fact]
    public void Ohne_Anlagenzeile_ist_der_Knopf_gesperrt_und_der_Grund_lesbar()
    {
        // Ä22: Kosten haengen an der Anlagenzeile; bei einer noch nicht gespeicherten
        // Neuanlage gibt es sie nicht. Der Tooltip des Vorlaeufers steht hier
        // ZUSAETZLICH als Herleitungszeile - ein Tooltip ist auf einem
        // Beruehrungsgeraet nicht erreichbar.
        var cut = Aufbauen(kostenBereit: () => false, kostenOeffnen: () => Task.CompletedTask);

        Assert.True(cut.Find(".epos-kostenleiste button").HasAttribute("disabled"));
        Assert.Equal("Invest — · Betrieb —", cut.Find(".epos-kostenleiste-hinweis").TextContent.Trim());
        Assert.Contains(cut.FindAll(".epos-herleitung").Select(e => e.TextContent),
                        t => t.Contains("zuerst mit OK anlegen"));
    }

    // =================================================================================
    // Pruefungen beim OK
    // =================================================================================

    [Fact]
    public void OK_ohne_Betriebsart_bei_Bivalenz_meldet()
    {
        bool? ergebnis = null;
        var daten = Voll();
        daten.BivalenterBetrieb = true;
        var cut = Aufbauen(daten, geschlossen: b => ergebnis = b);

        Knopf(cut, "OK").Click();

        Assert.Null(ergebnis);
        Assert.Contains("Bitte Betriebsart auswählen!", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void OK_ohne_Waermepumpe_meldet()
    {
        var daten = Voll();
        daten.Bezeichner = "";
        var cut = Aufbauen(daten);

        Knopf(cut, "OK").Click();
        Assert.Contains("Bitte Wärmepumpe auswählen!", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Vorlauf_kleiner_gleich_Ruecklauf_meldet_aus_der_Kernpruefung()
    {
        var daten = Voll();
        daten.Vorlauf = 35;
        daten.Ruecklauf = 35;
        var cut = Aufbauen(daten);

        Knopf(cut, "OK").Click();
        Assert.Contains("über der Rücklauftemperatur", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Jede_der_vier_Pflicht_Ganzzahlen_wird_beim_Namen_genannt()
    {
        (string Feld, Action<WaermepumpeAnlageDaten> Leeren)[] faelle =
        {
            ("Sperrzeit von",     d => d.SperrzeitVon = null),
            ("Sperrzeit bis",     d => d.SperrzeitBis = null),
            ("Nutzungsdauer",     d => d.Nutzungszeit = null),
            ("Leistung Heizstab", d => d.HeizstabLeistung = null)
        };

        foreach (var fall in faelle)
        {
            var daten = Voll();
            fall.Leeren(daten);

            bool? ergebnis = null;
            var cut = Aufbauen(daten, geschlossen: b => ergebnis = b);
            Knopf(cut, "OK").Click();

            Assert.Null(ergebnis);
            Assert.Contains(fall.Feld, cut.Find(".epos-warnbanner").TextContent);
        }
    }

    [Fact]
    public void Die_Bivalenztemperatur_darf_leer_bleiben()
    {
        bool? ergebnis = null;
        var daten = Voll();
        daten.Abschaltpunkt = null;
        var cut = Aufbauen(daten, geschlossen: b => ergebnis = b);

        Knopf(cut, "OK").Click();
        Assert.True(ergebnis);
    }

    [Fact]
    public void OK_meldet_true_wenn_alles_steht()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        Knopf(cut, "OK").Click();
        Assert.True(ergebnis);
    }

    // =================================================================================
    // Ueberlagerungen und Tastatur
    // =================================================================================

    [Fact]
    public void Der_Modulkatalog_setzt_die_Wahl()
    {
        var daten = Voll();
        var cut = Aufbauen(daten);

        Knopf(cut, "📋  Modul-Katalog...").Click();
        Assert.True(cut.Instance.KatalogOffen);

        var ueberlagerung = cut.Find(".epos-ueberlagerung");
        ueberlagerung.QuerySelector(".epos-raster tbody tr button")!.Click();
        ueberlagerung.QuerySelectorAll("button")
                     .First(b => b.TextContent.Trim() == "✔ Auswahl übernehmen").Click();

        Assert.Equal("WP Beta", daten.Bezeichner);
        Assert.Equal(2, daten.IdWp);
        Assert.False(cut.Instance.KatalogOffen);
    }

    [Fact]
    public void Ohne_StammGaben_bleibt_der_Parameterknopf_wirkungslos()
    {
        var cut = Aufbauen();
        Knopf(cut, "Parameter Bearbeiten...").Click();
        Assert.False(cut.Instance.StammdialogOffen);
    }

    [Fact]
    public void Abbrechen_und_Esc_melden_false()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);
        Knopf(cut, "Abbrechen").Click();
        Assert.False(ergebnis);

        ergebnis = null;
        var cut2 = Aufbauen(geschlossen: b => ergebnis = b);
        cut2.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(ergebnis);
    }

    [Fact]
    public void Der_Dialog_schreibt_in_den_uebergebenen_Datensatz()
    {
        // Er ist die KOPIE der Huelle: Erst der OK-Zweig der Huelle uebertraegt sie in
        // das Listenobjekt, ein Abbruch verwirft sie. Hier steht fest, dass die
        // Eingabe im uebergebenen Satz ankommt.
        var daten = Voll();
        var cut = Aufbauen(daten);

        var spitzenlast = cut.FindAll(".epos-gruppenkopf-koerper")[2];
        spitzenlast.QuerySelectorAll("input[type=text]")[0].Input("3");

        Assert.Equal(3, daten.SperrzeitVon);
    }

    [Fact]
    public void Esc_bei_offener_Ueberlagerung_schliesst_den_Dialog_nicht()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        Knopf(cut, "📋  Modul-Katalog...").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Null(ergebnis);
    }
}
