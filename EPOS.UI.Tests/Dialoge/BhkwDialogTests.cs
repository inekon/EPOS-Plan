using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Verwaltung BHKW — der Projektdialog (iU9-W6.4). Soll ist die Feldkarte von
/// <c>Form_BHKWEing</c>: zwei Listen (die rechte mit zwei Spalten), die beiden
/// Pfeile, zwei Filter, die Leistungssumme, der Detailblock mit Trägerwahl und
/// Grenzleistung sowie die drei Katalogknöpfe.
/// </summary>
public class BhkwDialogTests : BunitContext
{
    private static readonly string[] Gruppen = { "Alle", "Gas", "Öl" };
    private static readonly string[] Stufen = { "Alle", "kleiner 20 kW", "20 bis 40 kW" };

    private static readonly KatalogZeile[] Katalog =
    {
        new(21, "Modul A", "Musterwerk\nBrennstoff: Erdgas E\nPtherm: 80 kW\nPel: 40 kW"),
        new(22, "Modul B", "Musterwerk\nBrennstoff: Erdgas LL\nPtherm: 120 kW\nPel: 60 kW")
    };

    public BhkwDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static ErzeugerZeile Zeile(int schluessel, string name, int geraetId, int carrier = 5)
        => new() { Schluessel = schluessel, Bezeichner = name, GeraetId = geraetId,
                   CarrierId = carrier, Grenzleistung = 50, Vorlauf = 80, Ruecklauf = 60 };

    private static ErzeugerDetail Detail(string name) => new(
        name, "Beschreibung",
        new[] { ("Hersteller:", "Musterwerk"),
                ("thermische Leistung [kWth]:", "80"),
                ("elektrische Leistung [kWel]:", "40") });

    private IRenderedComponent<BhkwDialog> Aufbauen(
        List<ErzeugerZeile>? zeilen = null,
        Func<int, TraegerVorbereitung>? vorbereiten = null,
        Func<int, EnergietraegerVarianteErgebnis, AufnahmeErgebnis>? aufnehmen = null,
        Action<ErzeugerZeile>? entfernen = null,
        Action<ErzeugerZeile, int>? traegerWechseln = null,
        Action<ErzeugerZeile>? uebernehmen = null,
        Func<int, string>? katalogLoeschen = null,
        Func<string>? summe = null,
        Func<string, IReadOnlyDictionary<string, object>>? editorGaben = null,
        Func<string, IReadOnlyDictionary<string, object>>? editorGabenNeu = null,
        bool wizard = false,
        Action<bool>? geschlossen = null)
    {
        return Render<BhkwDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<ErzeugerZeile> { Zeile(1, "Modul A", 100) })
            .Add(x => x.Gruppen, Gruppen)
            .Add(x => x.Leistungsstufen, Stufen)
            .Add(x => x.Filtern, (_, _) => Katalog)
            .Add(x => x.KatalogDetail, n => Detail(n))
            .Add(x => x.ProjektDetail, n => Detail(n))
            .Add(x => x.Varianten, _ => new[] { (5, "Erdgas E Variante"), (6, "Erdgas LL Variante") })
            .Add(x => x.Vorbereiten, vorbereiten ??
                 (_ => new TraegerVorbereitung(new[] { (3, "Erdgas E") }, 3)))
            .Add(x => x.Aufnehmen, aufnehmen ??
                 ((_, _) => new AufnahmeErgebnis(Zeile(9, "Modul B", 200), "angelegt")))
            .Add(x => x.Entfernen, entfernen)
            .Add(x => x.TraegerWechseln, traegerWechseln)
            .Add(x => x.Uebernehmen, uebernehmen)
            .Add(x => x.KatalogLoeschen, katalogLoeschen ?? (_ => ""))
            .Add(x => x.SummePtherm, summe ?? (() => "80"))
            .Add(x => x.EditorGaben, editorGaben)
            .Add(x => x.EditorGabenNeu, editorGabenNeu)
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

        var ueberschriften = cut.FindAll(".epos-untergruppe").Select(e => e.TextContent).ToList();
        Assert.Contains("Ausgewählte Module:", ueberschriften);
        Assert.Contains("Module in Datenbank:", ueberschriften);

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.Contains("Summe aller ausgewählten Module [kWth]:", texte);
        Assert.Contains("Filtern nach Brennstoffart", texte);
        Assert.Contains("Filtern nach Leistung", texte);
        Assert.Contains("Brennstoff:", texte);
        Assert.Contains("Untere Grenzleistung des ausgewählten Moduls:", texte);
        Assert.Contains("Vorlauf", texte);
        Assert.Contains("Rücklauf", texte);
    }

    [Fact]
    public void Die_Katalogliste_hat_die_zweite_Spalte_Eigenschaften()
    {
        // Der Vorlaeufer trug im DataGridView "Name" und einen Mehrzeiler
        // "Eigenschaften" (Firma, Brennstoff, Ptherm, Pel).
        var cut = Aufbauen();

        var kopf = cut.FindAll(".epos-raster")[1].QuerySelectorAll("th")
                      .Select(e => e.TextContent.Trim()).ToList();
        Assert.Contains("Name", kopf);
        Assert.Contains("Eigenschaften", kopf);

        var zellen = cut.FindAll(".epos-mehrzeilig");
        Assert.Equal(2, zellen.Count);
        Assert.Contains("Ptherm: 80 kW", zellen[0].TextContent);
    }

    [Fact]
    public void Die_Leistungssumme_kommt_fertig_von_aussen()
    {
        var cut = Aufbauen(summe: () => "200");

        Assert.Equal("200", cut.Instance.Summe);
    }

    [Fact]
    public void Im_Assistenten_fehlen_OK_Abbrechen_und_die_Kostenleiste()
    {
        var cut = Aufbauen(wizard: true);

        Assert.Empty(cut.FindAll(".epos-status"));
        Assert.Empty(cut.FindAll(".epos-kostenleiste"));
    }

    // =================================================================================
    // Auswahl
    // =================================================================================

    [Fact]
    public void Beim_Oeffnen_ist_die_erste_Projektzeile_gewaehlt()
    {
        var cut = Aufbauen();

        Assert.NotNull(cut.Instance.Projektzeile);
        Assert.Equal("Modul A", cut.Instance.Projektzeile!.Bezeichner);
    }

    [Fact]
    public void Eine_Katalogzeile_verdeckt_Traegerwahl_und_Grenzleistung()
    {
        var cut = Aufbauen();
        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.DoesNotContain("Brennstoff:", texte);
        Assert.DoesNotContain("Untere Grenzleistung des ausgewählten Moduls:", texte);
    }

    // =================================================================================
    // Hinzufuegen und Entfernen
    // =================================================================================

    [Fact]
    public void Der_Pfeil_oeffnet_zuerst_die_Traegerwahl()
    {
        var cut = Aufbauen();

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-zweispalten-mitte button")[0].Click();

        Assert.True(cut.Instance.Traegerwahl);
    }

    [Fact]
    public void Nach_dem_Hinzufuegen_wird_die_Summe_neu_erfragt()
    {
        int rufe = 0;
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Modul A", 100) };
        var cut = Aufbauen(zeilen, summe: () => (++rufe * 100).ToString());

        int vorher = rufe;
        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-zweispalten-mitte button")[0].Click();
        cut.Find(".epos-ueberlagerung input[type=text]").Input("Erdgas E Variante");
        cut.Find(".epos-ueberlagerung .epos-knopf--primaer").Click();

        Assert.Equal(2, zeilen.Count);
        Assert.True(rufe > vorher, "Die Summe wurde nach dem Hinzufuegen nicht neu erfragt.");
    }

    [Fact]
    public void Nach_dem_Entfernen_der_letzten_Zeile_wird_die_erste_Katalogzeile_gewaehlt()
    {
        // btn_BHKW_Löschen_Click: Bleibt nichts uebrig, wandert die Auswahl in die
        // rechte Liste (Z. 749-758).
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Modul A", 100) };
        var cut = Aufbauen(zeilen);

        cut.FindAll(".epos-zweispalten-mitte button")[1].Click();

        Assert.Empty(zeilen);
        Assert.Null(cut.Instance.Projektzeile);
        Assert.NotNull(cut.Instance.Katalogzeile);
        Assert.Equal(21, cut.Instance.Katalogzeile!.Id);
    }

    [Fact]
    public void Der_Pfeil_zurueck_entfernt_genau_die_gewaehlte_Zeile()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Modul A", 100), Zeile(2, "Modul A", 100) };
        var entfernt = new List<ErzeugerZeile>();
        var cut = Aufbauen(zeilen, entfernen: z => entfernt.Add(z));

        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[1].Click();
        cut.FindAll(".epos-zweispalten-mitte button")[1].Click();

        Assert.Single(zeilen);
        Assert.Equal(1, zeilen[0].Schluessel);
        Assert.Equal(2, entfernt[0].Schluessel);
    }

    // =================================================================================
    // Zeilenwerte
    // =================================================================================

    [Fact]
    public void Die_Grenzleistung_wandert_ins_Modell()
    {
        var uebernommen = new List<ErzeugerZeile>();
        var cut = Aufbauen(uebernehmen: z => uebernommen.Add(z));

        cut.Find("input[inputmode=decimal]").Input("42");

        Assert.Equal(42, cut.Instance.Projektzeile!.Grenzleistung);
        Assert.Single(uebernommen);
    }

    [Fact]
    public void Ein_Traegerwechsel_schreibt_sofort()
    {
        int? gemeldet = null;
        var cut = Aufbauen(traegerWechseln: (_, n) => gemeldet = n);

        // Filter Brennstoff, Filter Leistung, Trägerwahl.
        cut.FindAll("select")[2].Change("6");

        Assert.Equal(6, gemeldet);
        Assert.Equal(6, cut.Instance.Projektzeile!.CarrierId);
    }

    // =================================================================================
    // Katalogpflege
    // =================================================================================

    [Fact]
    public void Loeschen_fragt_zuerst_nach()
    {
        var geloescht = new List<int>();
        var cut = Aufbauen(katalogLoeschen: id => { geloescht.Add(id); return ""; });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-zweispalten-spalte")[1].QuerySelectorAll(".epos-leiste button")[2].Click();

        Assert.Single(cut.FindAll(".epos-rueckfrage"));
        Assert.Empty(geloescht);

        cut.FindAll(".epos-rueckfrage button")[0].Click();
        Assert.Single(geloescht);
        Assert.Equal(21, geloescht[0]);
    }

    [Fact]
    public void Ein_schreibgeschuetzter_Satz_wird_mit_Grund_abgelehnt()
    {
        var cut = Aufbauen(katalogLoeschen: _ =>
            "Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.");

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-zweispalten-spalte")[1].QuerySelectorAll(".epos-leiste button")[2].Click();
        cut.FindAll(".epos-rueckfrage button")[0].Click();

        Assert.Contains("schreibgeschützt", cut.Instance.Meldung);
    }

    [Fact]
    public void Neu_fragt_erst_den_Namen_und_oeffnet_dann_den_Editor()
    {
        string? gefragt = null;
        var cut = Aufbauen(editorGabenNeu: name =>
        {
            gefragt = name;
            return new Dictionary<string, object> { ["Daten"] = new BhkwKatalogDaten() };
        });

        cut.FindAll(".epos-zweispalten-spalte")[1].QuerySelectorAll(".epos-leiste button")[1].Click();
        cut.Find(".epos-ueberlagerung input[type=text]").Input("Neues Modul");
        cut.Find(".epos-ueberlagerung .epos-knopf--primaer").Click();

        Assert.Equal("Neues Modul", gefragt);
        Assert.True(cut.Instance.Katalogeditor);
    }

    [Fact]
    public void Bearbeiten_oeffnet_den_Editor_mit_dem_gewaehlten_Namen()
    {
        string? gefragt = null;
        var cut = Aufbauen(editorGaben: name =>
        {
            gefragt = name;
            return new Dictionary<string, object> { ["Daten"] = new BhkwKatalogDaten() };
        });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-zweispalten-spalte")[1].QuerySelectorAll(".epos-leiste button")[0].Click();

        Assert.Equal("Modul A", gefragt);
        Assert.True(cut.Instance.Katalogeditor);
    }

    // =================================================================================
    // Abschluss und Tastatur
    // =================================================================================

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
    // =====================================================================
    //  Formularraster — Anwenderwunsch iU8‑E‑2, Paket P1 (05.09.2026)
    // =====================================================================

    /// <summary>
    /// <b>iU8‑E‑2, Paket P1:</b> „Darstellung der Dialoge kompakter und
    /// übersichtlicher — Parameterblöcke rechts."
    ///
    /// <para>Der Detailblock des Projektdialogs steht seither im <c>Formularraster</c>: Die Beschriftung
    /// fällt NEBEN das Feld, die Felder ordnen sich in eine oder zwei Spalten,
    /// und ein Zahlenfeld ist kurz mit der Einheit unmittelbar dahinter. Zuvor
    /// nahm jedes Feld die volle Breite und die Beschriftung stand darüber.</para>
    ///
    /// <para>Die Regeln dahinter hält <c>Bausteine/FormularrasterTests</c>;
    /// hier steht nur, dass der Block ihn TRÄGT.</para>
    /// </summary>
    [Fact]
    public void Der_Detailblock_steht_im_Formularraster()
    {
        var cut = Aufbauen();

        var raster = cut.FindAll(".epos-formularraster");
        Assert.NotEmpty(raster);
        Assert.Contains(raster, r => r.QuerySelectorAll(".epos-feld").Length > 0);
    }

    /// <summary>
    /// <b>Anwenderfoto „Verwaltung BHKW" (05.09.2026):</b> „Stelle diesen
    /// Dialog kompakter dar, insbesondere Daten zum BHKW-Modul unten."
    ///
    /// <para>So ist der Block seither aufgeteilt: Modulname und Hersteller
    /// nehmen die Feldspalte, die beiden LEISTUNGEN sind kurze Felder und
    /// stehen damit zu zweit in einer Zeile, die Beschreibung spannt über
    /// beide Spalten, und Träger, Grenzleistung, Vor- und Rücklauf folgen als
    /// Auswahlfeld und drei kurze Felder. Geprüft wird die Selbstmeldung, denn
    /// die Breite selbst steht im Stilblatt (Lehre W6‑B‑1).</para>
    /// </summary>
    [Fact]
    public void Die_Leistungen_des_Moduls_sind_kurze_Felder_die_Beschreibung_ist_breit()
    {
        var cut = Aufbauen();

        var block = cut.FindAll(".epos-formularraster")[^1];

        // Die zwei Leistungen sind Zahlen und melden sich als kurz; Hersteller
        // und Modulname sind Text und bleiben in der Feldspalte.
        var kurz = block.QuerySelectorAll(".epos-feld--kurz");
        var kurzeTexte = kurz.Select(f => f.QuerySelector(".epos-feld-text")?.TextContent).ToList();

        Assert.Contains("thermische Leistung [kWth]:", kurzeTexte);
        Assert.Contains("elektrische Leistung [kWel]:", kurzeTexte);
        Assert.DoesNotContain("Hersteller:", kurzeTexte);

        // Die Beschreibung ist mehrzeilig und meldet sich als LANG.
        Assert.NotEmpty(block.QuerySelectorAll(".epos-feld--breit textarea"));
    }
}
