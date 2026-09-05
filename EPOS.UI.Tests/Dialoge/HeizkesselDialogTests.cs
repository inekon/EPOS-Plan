using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Allgemein;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Verwaltung Heizkessel — der Projektdialog (iU9-W6.3). Soll ist die Feldkarte von
/// <c>Form_Heizkessel</c>: zwei Listen, die beiden Pfeile, zwei Filter, der
/// Detailblock mit Trägerwahl und die drei Katalogknöpfe.
/// </summary>
public class HeizkesselDialogTests : BunitContext
{
    private static readonly string[] Gruppen = { "Alle", "Gas", "Öl", "Holz" };
    private static readonly string[] Stufen = { "Alle", "bis 50 kW", ">50 bis 200 kW" };

    private static readonly KatalogZeile[] Katalog =
    {
        new(11, "Kessel A"), new(12, "Kessel B")
    };

    public HeizkesselDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;   // QuickGrid laedt ein JS-Modul
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static ErzeugerZeile Zeile(int schluessel, string name, int geraetId, int carrier = 5)
        => new() { Schluessel = schluessel, Bezeichner = name, GeraetId = geraetId,
                   CarrierId = carrier, Vorlauf = 70, Ruecklauf = 50 };

    private static ErzeugerDetail Detail(string name) => new(
        name, "Beschreibung",
        new[] { ("Brennstoff Typ:", "Erdgas E"), ("Leistung [kW]:", "120,00"),
                ("Investitionskosten [€]:", "12000,00") },
        ("Brennwertkessel", true));

    private IRenderedComponent<HeizkesselDialog> Aufbauen(
        List<ErzeugerZeile>? zeilen = null,
        Func<int, TraegerVorbereitung>? vorbereiten = null,
        Func<int, EnergietraegerVarianteErgebnis, AufnahmeErgebnis>? aufnehmen = null,
        Action<ErzeugerZeile>? entfernen = null,
        Action<ErzeugerZeile, int>? traegerWechseln = null,
        Action<ErzeugerZeile>? uebernehmen = null,
        Func<int, bool>? katalogLoeschen = null,
        Func<IReadOnlyDictionary<string, object>>? verwaltung = null,
        Func<string, IReadOnlyDictionary<string, object>>? editorGaben = null,
        bool wizard = false,
        Action<bool>? geschlossen = null)
    {
        return Render<HeizkesselDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<ErzeugerZeile> { Zeile(1, "Kessel A", 100) })
            .Add(x => x.Gruppen, Gruppen)
            .Add(x => x.Leistungsstufen, Stufen)
            .Add(x => x.Filtern, (_, _) => Katalog)
            .Add(x => x.KatalogDetail, n => Detail(n))
            .Add(x => x.ProjektDetail, _ => Detail("Kessel A"))
            .Add(x => x.Varianten, _ => new[] { (5, "Erdgas E Variante"), (6, "Erdgas LL Variante") })
            .Add(x => x.Vorbereiten, vorbereiten ??
                 (_ => new TraegerVorbereitung(new[] { (3, "Erdgas E") }, 3)))
            .Add(x => x.Aufnehmen, aufnehmen ??
                 ((_, _) => new AufnahmeErgebnis(Zeile(9, "Kessel B", 200), "angelegt")))
            .Add(x => x.Entfernen, entfernen)
            .Add(x => x.TraegerWechseln, traegerWechseln)
            .Add(x => x.Uebernehmen, uebernehmen)
            .Add(x => x.KatalogLoeschen, katalogLoeschen ?? (_ => true))
            .Add(x => x.VerwaltungGaben, verwaltung)
            .Add(x => x.EditorGaben, editorGaben)
            .Add(x => x.TraegerGaben, _ => new Dictionary<string, object>
            {
                ["Energietraeger"] = new[] { (3, "Erdgas E") }
            })
            .Add(x => x.Wizard, wizard)
            .Add(x => x.Geschlossen, ok => geschlossen?.Invoke(ok)));
    }

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Die_beiden_Listen_die_Pfeile_und_die_Filter_stehen()
    {
        var cut = Aufbauen();

        Assert.Equal(2, cut.FindAll(".epos-raster").Count);
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-mitte button").Count);
        // Entscheid #76: Jeder Knopf traegt BEIDE Zeichen im Markup - das Stilblatt
        // zeigt je nach Anordnung eines davon - und dazu seine Aufgabe im Klartext.
        Assert.Equal("◀", cut.FindAll(".epos-zweispalten-mitte button")[0].QuerySelector(".epos-zweispalten-pfeil--breit")!.TextContent);
        Assert.Equal("▲", cut.FindAll(".epos-zweispalten-mitte button")[0].QuerySelector(".epos-zweispalten-pfeil--schmal")!.TextContent);
        Assert.Equal("▶", cut.FindAll(".epos-zweispalten-mitte button")[1].QuerySelector(".epos-zweispalten-pfeil--breit")!.TextContent);
        Assert.Equal("▼", cut.FindAll(".epos-zweispalten-mitte button")[1].QuerySelector(".epos-zweispalten-pfeil--schmal")!.TextContent);
        Assert.Equal("In das Projekt übernehmen",
                     cut.FindAll(".epos-zweispalten-mitte button")[0].QuerySelector(".epos-zweispalten-knopftext")!.TextContent);
        Assert.Equal("Aus dem Projekt entfernen",
                     cut.FindAll(".epos-zweispalten-mitte button")[1].QuerySelector(".epos-zweispalten-knopftext")!.TextContent);

        var ueberschriften = cut.FindAll(".epos-untergruppe").Select(e => e.TextContent).ToList();
        Assert.Contains("ausgewählt im Projekt", ueberschriften);
        Assert.Contains("Kessel aus Datenbank", ueberschriften);

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.Contains("Filtern nach Brennstoffart:", texte);
        Assert.Contains("Filtern nach Leistung:", texte);
        Assert.Contains("Brennstoff Variante:", texte);
        Assert.Contains("Vorlauf:", texte);
        Assert.Contains("Rücklauf:", texte);
    }

    [Fact]
    public void Der_Detailblock_zeigt_die_sieben_Felder_der_Gruppe_Modul()
    {
        var cut = Aufbauen();

        // Name, Brennstoff Typ, Leistung, Investition, Beschreibung (mehrzeilig),
        // Brennwertkessel (Schalter), Brennstoff Variante (Auswahl).
        var gruppe = cut.Find(".epos-gruppenkopf-koerper");
        Assert.Equal("Modul", cut.Find(".epos-gruppenkopf-titel").TextContent);
        Assert.Equal(4, gruppe.QuerySelectorAll("input[type=text][readonly]").Length);
        Assert.Single(gruppe.QuerySelectorAll("textarea"));
        Assert.Single(gruppe.QuerySelectorAll("input[type=checkbox]"));
    }

    /// <summary>
    /// Ohne Parametersatz der Katalogverwaltung kein Knopf — Hausregel. Seit
    /// iU9-W14a.4 ist die Verwaltung eine ÜBERLAGERUNG im selben Fenster statt eines
    /// zweiten Fensters über die Sprungbrücke (Risiko R2).
    /// </summary>
    [Fact]
    public void Der_Admin_Knopf_erscheint_nur_mit_Verwaltungsgaben()
    {
        var ohne = Aufbauen();
        Assert.DoesNotContain(ohne.FindAll("button").Select(b => b.TextContent), t => t == "Administration...");

        var mit = Aufbauen(verwaltung: () => Verwaltungsgaben());
        Assert.Contains(mit.FindAll("button").Select(b => b.TextContent), t => t == "Administration...");
    }

    /// <summary>Der Knopf öffnet die Verwaltung als Überlagerung, nicht als Fenster.</summary>
    [Fact]
    public void Der_Admin_Knopf_oeffnet_die_Verwaltung_als_Ueberlagerung()
    {
        var cut = Aufbauen(verwaltung: () => Verwaltungsgaben());

        Assert.False(cut.Instance.VerwaltungOffen);
        cut.FindAll("button").First(b => b.TextContent == "Administration...").Click();

        Assert.True(cut.Instance.VerwaltungOffen);
        Assert.NotEmpty(cut.FindAll(".epos-ueberlagerung"));
    }

    /// <summary>Ein Mindestsatz für die Überlagerung — der Browser braucht sein Profil.</summary>
    private static IReadOnlyDictionary<string, object> Verwaltungsgaben()
        => new Dictionary<string, object>
        {
            ["Art"] = WindowsFormsApplication1.KatalogBrowserArt.Heizkessel,
            ["Wege"] = new EPOS.UI.Dialoge.Erzeuger.KatalogBrowserWege()
        };

    [Fact]
    public void Im_Assistenten_fehlen_OK_Abbrechen_und_die_Kostenleiste()
    {
        var cut = Aufbauen(wizard: true);

        Assert.Empty(cut.FindAll(".epos-status"));          // SpeichernLeiste
        Assert.Empty(cut.FindAll(".epos-kostenleiste"));
    }

    // =================================================================================
    // Auswahl und Detail
    // =================================================================================

    [Fact]
    public void Beim_Oeffnen_ist_die_erste_Projektzeile_gewaehlt()
    {
        // SetControls: listBox_Kessel.Items[0].Selected = true.
        var cut = Aufbauen();

        Assert.NotNull(cut.Instance.Projektzeile);
        Assert.Equal("Kessel A", cut.Instance.Projektzeile!.Bezeichner);
    }

    [Fact]
    public void Eine_Katalogzeile_verdeckt_die_Traegerwahl()
    {
        // listBox_Kessel_DB_SelectedIndexChanged: cmbBrennstoffArt.Visible = false.
        var cut = Aufbauen();
        Assert.Contains("Brennstoff Variante:",
                        cut.FindAll(".epos-feld-text").Select(e => e.TextContent));

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();

        Assert.Null(cut.Instance.Projektzeile);
        Assert.NotNull(cut.Instance.Katalogzeile);
        Assert.DoesNotContain("Brennstoff Variante:",
                              cut.FindAll(".epos-feld-text").Select(e => e.TextContent));
    }

    // =================================================================================
    // Hinzufuegen
    // =================================================================================

    [Fact]
    public void Ohne_Katalogwahl_tut_der_Pfeil_nichts()
    {
        // btn_Kessel_Hinzu_Click: listBox_Kessel_DB.Text == "" -> return.
        var cut = Aufbauen();

        Assert.True(cut.FindAll(".epos-zweispalten-mitte button")[0].HasAttribute("disabled"));
    }

    [Fact]
    public void Der_Pfeil_oeffnet_zuerst_die_Traegerwahl()
    {
        var cut = Aufbauen();

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-zweispalten-mitte button")[0].Click();

        Assert.True(cut.Instance.Traegerwahl);
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
    }

    [Fact]
    public void Ein_abgebrochener_Traegerdialog_fuegt_nichts_hinzu()
    {
        // Punkt 2 des Bestands: kein verwaister Eintrag mit ID_Carrier = 0.
        bool aufgenommen = false;
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Kessel A", 100) };
        var cut = Aufbauen(zeilen, aufnehmen: (_, _) =>
        {
            aufgenommen = true;
            return new AufnahmeErgebnis(Zeile(9, "Kessel B", 200));
        });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-zweispalten-mitte button")[0].Click();
        cut.Find(".epos-ueberlagerung").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.False(cut.Instance.Traegerwahl);
        Assert.False(aufgenommen);
        Assert.Single(zeilen);
    }

    [Fact]
    public void Ein_Fehler_beim_Anlegen_nimmt_nichts_auf_und_meldet()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Kessel A", 100) };
        var cut = Aufbauen(zeilen,
            aufnehmen: (_, _) => new AufnahmeErgebnis(null, "Der Energieträger konnte nicht angelegt werden.", true));

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-zweispalten-mitte button")[0].Click();

        // Der Traegerdialog laesst OK erst zu, wenn ein Variantenname dasteht.
        cut.Find(".epos-ueberlagerung input[type=text]").Input("Erdgas E Variante");
        cut.Find(".epos-ueberlagerung .epos-knopf--primaer").Click();

        Assert.Single(zeilen);
        Assert.Contains("nicht angelegt", cut.Instance.Meldung);
    }

    [Fact]
    public void Eine_nicht_leere_Vorbereitungsmeldung_bricht_ab()
    {
        var cut = Aufbauen(vorbereiten: _ => new TraegerVorbereitung(
            Array.Empty<(int, string)>(), null,
            "Der ausgewählte Heizkessel wurde in den Stammdaten nicht gefunden."));

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-zweispalten-mitte button")[0].Click();

        Assert.False(cut.Instance.Traegerwahl);
        Assert.Contains("nicht gefunden", cut.Instance.Meldung);
    }

    // =================================================================================
    // Entfernen - die Regel "eine Kopie, mehrere Zeilen"
    // =================================================================================

    [Fact]
    public void Der_Pfeil_zurueck_entfernt_genau_die_gewaehlte_Zeile()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Kessel A", 100), Zeile(2, "Kessel A", 100) };
        var entfernt = new List<ErzeugerZeile>();
        var cut = Aufbauen(zeilen, entfernen: z => entfernt.Add(z));

        // Die ZWEITE Zeile waehlen und entfernen - bei Namensgleichheit muss genau sie
        // gehen (der Bestand traf ueber ListViewItem.Tag, nicht ueber den Namen).
        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[1].Click();
        cut.FindAll(".epos-zweispalten-mitte button")[1].Click();

        Assert.Single(zeilen);
        Assert.Equal(1, zeilen[0].Schluessel);
        Assert.Single(entfernt);
        Assert.Equal(2, entfernt[0].Schluessel);
    }

    [Fact]
    public void Nach_dem_Entfernen_ist_wieder_die_erste_Zeile_gewaehlt()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Kessel A", 100), Zeile(2, "Kessel B", 200) };
        var cut = Aufbauen(zeilen);

        cut.FindAll(".epos-zweispalten-mitte button")[1].Click();

        Assert.Single(zeilen);
        Assert.Equal(2, cut.Instance.Projektzeile!.Schluessel);
    }

    // =================================================================================
    // Zeilenwerte
    // =================================================================================

    [Fact]
    public void Ein_Traegerwechsel_schreibt_sofort()
    {
        // cmbBrennstoffArt_SelectedIndexChanged: UPDATE energy_Project_settings.
        (ErzeugerZeile Zeile, int Neu)? gemeldet = null;
        var cut = Aufbauen(traegerWechseln: (z, n) => gemeldet = (z, n));

        var listen = cut.FindAll("select");
        // Filter Brennstoff, Filter Leistung, Trägerwahl.
        listen[2].Change("6");

        Assert.NotNull(gemeldet);
        Assert.Equal(6, gemeldet!.Value.Neu);
        Assert.Equal(6, cut.Instance.Projektzeile!.CarrierId);
    }

    [Fact]
    public void Vorlauf_und_Ruecklauf_wandern_ins_Modell()
    {
        var uebernommen = new List<ErzeugerZeile>();
        var cut = Aufbauen(uebernehmen: z => uebernommen.Add(z));

        var felder = cut.FindAll("input[inputmode=numeric]");
        felder[0].Input("75");
        felder[1].Input("55");

        Assert.Equal(75, cut.Instance.Projektzeile!.Vorlauf);
        Assert.Equal(55, cut.Instance.Projektzeile!.Ruecklauf);
        Assert.Equal(2, uebernommen.Count);
    }

    // =================================================================================
    // Katalogpflege
    // =================================================================================

    [Fact]
    public void Loeschen_fragt_zuerst_nach()
    {
        // A-4: Der Vorlaeufer loeschte OHNE Rueckfrage, obwohl der Satz global gilt.
        var geloescht = new List<int>();
        var cut = Aufbauen(katalogLoeschen: id => { geloescht.Add(id); return true; });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-zweispalten-spalte")[1].QuerySelectorAll(".epos-leiste button")[1].Click();

        Assert.Single(cut.FindAll(".epos-rueckfrage"));
        Assert.Empty(geloescht);

        cut.FindAll(".epos-rueckfrage button")[0].Click();
        Assert.Single(geloescht);
        Assert.Equal(11, geloescht[0]);
    }

    [Fact]
    public void Nein_auf_die_Loeschfrage_loescht_nichts()
    {
        var geloescht = new List<int>();
        var cut = Aufbauen(katalogLoeschen: id => { geloescht.Add(id); return true; });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-zweispalten-spalte")[1].QuerySelectorAll(".epos-leiste button")[1].Click();
        cut.FindAll(".epos-rueckfrage button")[1].Click();

        Assert.Empty(geloescht);
    }

    [Fact]
    public void Bearbeiten_oeffnet_den_Katalogeditor_in_einer_Ueberlagerung()
    {
        string? gefragt = null;
        var cut = Aufbauen(editorGaben: name =>
        {
            gefragt = name;
            return new Dictionary<string, object> { ["Daten"] = new HeizkesselKatalogDaten() };
        });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-zweispalten-spalte")[1].QuerySelectorAll(".epos-leiste button")[0].Click();

        Assert.Equal("Kessel A", gefragt);
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
    }

    // =================================================================================
    // Abschluss und Tastatur
    // =================================================================================

    [Fact]
    public void OK_und_Abbrechen_melden_ihr_Ergebnis()
    {
        bool? gemeldet = null;
        var cut = Aufbauen(geschlossen: ok => gemeldet = ok);

        cut.Find(".epos-leiste .epos-knopf--primaer").Click();
        Assert.True(gemeldet);
    }

    [Fact]
    public void Esc_bricht_ab_und_Enter_ist_nicht_belegt()
    {
        int rufe = 0;
        bool? gemeldet = null;
        var cut = Aufbauen(geschlossen: ok => { gemeldet = ok; rufe++; });

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(0, rufe);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(1, rufe);
        Assert.False(gemeldet);
    }
}
