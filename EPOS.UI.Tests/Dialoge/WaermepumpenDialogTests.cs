using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Waermepumpe;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Wärmepumpen Verwaltung als Wirt der Katalogauswahl V1, Stufe 3 (Konzept Projektdialoge mit Katalogauswahl 4.2,
/// 4.6, 4.9): oben die Wärmepumpen des Projekts, darunter der Katalog, unten die Detailzeile mit Kenndaten und Kennlinie;
/// „Anlage…" öffnet die Anlagenseite als Überlagerung.
/// </summary>
public class WaermepumpenDialogTests : EposBunitContext
{
    public WaermepumpenDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static WaermepumpeAnlageDaten Zeile(string name, int nennleistung = 12) => new()
    {
        Bezeichner = name,
        Firma = "Bosch",
        IdWp = 1,
        Vorlauf = 35,
        Ruecklauf = 28,
        Nennleistung = nennleistung,
        Betriebsart = DbWerte.WP_BETRIEBSART_PARALLEL,
        SperrzeitVon = 0,
        SperrzeitBis = 0,
        HeizstabLeistung = 6
    };

    private static readonly Katalogfilterzeile[] Katalog =
    {
        new Katalogfilterzeile(1, "WP Neu")
            .MitText(Katalogfilterprofil.SpHersteller, "Alpha")
            .MitText(Katalogfilterprofil.SpBezeichner, "WP Neu")
            .MitText(Katalogfilterprofil.SpQuelle, "Sole-Wasser")
            .MitZahl(Katalogfilterprofil.SpNennleistung, 20, 1)
            .MitZahl(Katalogfilterprofil.SpVlMin, 35, 0)
            .MitZahl(Katalogfilterprofil.SpVlMax, 60, 0)
            .MitZahl(Katalogfilterprofil.SpZuheizung, 9, 1)
            .MitZahl(Katalogfilterprofil.SpKuehlleistung, 0.0, 1)
            .MitZahl(Katalogfilterprofil.SpCop, 4.1, 2)
    };

    private static readonly Katalogfilterprofil Katalogprofil =
        Katalogfilterprofil.MitVerwendung(Anlagenart.Waermepumpe);

    /// <summary>Der Parametersatz der eingebetteten Detailansicht; <paramref name="mangel"/> = Temperaturprüfung schlägt an.</summary>
    private static IReadOnlyDictionary<string, object> AnlageGaben(WaermepumpeAnlageDaten daten, string? mangel = null)
        => new Dictionary<string, object>
        {
            ["Daten"] = daten,
            ["Stammliste"] = new Func<IReadOnlyList<WaermepumpeStammZeile>>(
                () => new[] { new WaermepumpeStammZeile(1, daten.Bezeichner, false),
                              new WaermepumpeStammZeile(2, "WP Neu", false) }),
            ["TemperaturenPruefen"] = new Func<int?, int?, string?>((_, _) => mangel)
        };

    private IRenderedComponent<WaermepumpenDialog> Aufbauen(
        List<WaermepumpeAnlageDaten>? zeilen = null,
        Func<string, WaermepumpeAnlageDaten?>? anlegen = null,
        Action<WaermepumpeAnlageDaten>? uebernehmen = null,
        Action<WaermepumpeAnlageDaten>? entfernen = null,
        bool wizard = false,
        Action<bool>? geschlossen = null,
        string? mangel = null,
        Func<WaermepumpeAnlageDaten, int, bool>? umstellen = null,
        IReadOnlyList<Katalogfilterzeile>? katalog = null,
        Satzbearbeitungswege? projektsatzWege = null,
        Satzbearbeitungswege? katalogsatzWege = null,
        Func<string, IReadOnlyDictionary<string, object>>? editorGaben = null,
        Func<int, string>? katalogLoeschen = null,
        Rueckwegwege? rueckweg = null,
        Func<WaermepumpeAnlageDaten, bool, Task>? kosten = null,
        Func<WaermepumpeAnlageDaten, IReadOnlyDictionary<string, object>>? anlageGaben = null)
        => Render<WaermepumpenDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") })
            .Add(x => x.Katalog, () => katalog ?? Katalog)
            .Add(x => x.Katalogprofil, Katalogprofil)
            .Add(x => x.AnlageGaben, anlageGaben ?? (d => AnlageGaben(d, mangel)))
            .Add(x => x.Anlegen, anlegen ?? (n => Zeile(n, 20)))
            .Add(x => x.Uebernehmen, uebernehmen)
            .Add(x => x.Entfernen, entfernen)
            .Add(x => x.Umstellen, umstellen ?? ((d, id) => { d.Bezeichner = "WP Neu"; d.IdWp = id; return true; }))
            .Add(x => x.ProjektSatz, d => Stamm(d.Bezeichner, d.Firma, d.Nennleistung))
            .Add(x => x.KatalogSatz, id => Stamm("WP Neu", "Alpha", 20))
            .Add(x => x.ProjektBilder, _ => KennlinienBilder.Leer)
            .Add(x => x.KatalogBilder, _ => KennlinienBilder.Leer)
            .Add(x => x.ProjektsatzWege, projektsatzWege)
            .Add(x => x.KatalogsatzWege, katalogsatzWege)
            .Add(x => x.EditorGaben, editorGaben)
            .Add(x => x.KatalogLoeschen, katalogLoeschen)
            .Add(x => x.RueckwegWege, rueckweg)
            .Add(x => x.KostenOeffnen, kosten)
            .Add(x => x.Wizard, wizard)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static WaermepumpeStammDaten Stamm(string name, string firma, int nennleistung) => new()
    {
        Id = 1, Name = name, Firma = firma, Nennleistung = nennleistung, Heizstab = 6, Modulkosten = 9000
    };

    private static Satzbearbeitungswege Wege(List<(int Id, IReadOnlyList<BrowserFeldwert> Felder)>? gespeichert = null)
        => new()
        {
            Lesen = id => new List<BrowserFeldwert>
            {
                new() { Schluessel = "Firma", Bezeichnung = "Hersteller:", Art = BrowserFeldArt.Text, Editierbar = true, Wert = "Alpha" },
                new() { Schluessel = "Modulkosten", Bezeichnung = "Modulkosten:", Art = BrowserFeldArt.Zahl, Editierbar = true, Wert = "100" },
            },
            Speichern = l => { gespeichert?.AddRange(l); return new KatalogSpeicherErgebnis(true, "gespeichert", ""); }
        };

    private static IReadOnlyList<Katalogfilterzeile> ZweiSaetze(params int[] gesperrt)
    {
        var a = new Katalogfilterzeile(1, "WP Neu").MitText(Katalogfilterprofil.SpBezeichner, "WP Neu");
        var b = new Katalogfilterzeile(2, "WP Zwei").MitText(Katalogfilterprofil.SpBezeichner, "WP Zwei");
        a.Geschuetzt = gesperrt.Contains(1);
        b.Geschuetzt = gesperrt.Contains(2);
        return new[] { a, b };
    }

    /// <summary>Wählt eine Katalogzeile — im Katalog ist die ZEILE die Wahl (Kästchenmodus).</summary>
    private static void KatalogzeileWaehlen(IRenderedComponent<WaermepumpenDialog> cut, int nummer = 0)
        => cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[nummer].Click();

    private static void KatalogAnkreuzen(IRenderedComponent<WaermepumpenDialog> cut, params int[] zeilen)
    {
        foreach (int i in zeilen)
            cut.FindAll(".epos-raster")[1].QuerySelectorAll("td .epos-kaestchenzelle input")[i].Change(true);
    }

    private static void ProjektAnkreuzen(IRenderedComponent<WaermepumpenDialog> cut, params int[] zeilen)
    {
        foreach (int i in zeilen)
            cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody td.epos-spalte-kaestchen .epos-wahlkaestchen")[i].Click();
    }

    private static IElement Knopf(IRenderedComponent<WaermepumpenDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    /// <summary>Die Zeilenwahl der PROJEKTliste (das erste Raster).</summary>
    private static IElement Projektwahl(IRenderedComponent<WaermepumpenDialog> cut, int index)
        => cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[index];

    private static IElement Pfeil(IRenderedComponent<WaermepumpenDialog> cut, int index)
        => cut.Find(index == 0 ? ".epos-zweispalten-knopf--uebernehmen" : ".epos-zweispalten-knopf--entfernen");

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Die_Spalten_der_Projektliste_stehen()
    {
        var cut = Aufbauen();
        var kopf = cut.FindAll(".epos-raster")[0].QuerySelectorAll("th:not(.epos-spalte-kaestchen)")
                      .Select(e => e.TextContent.Trim()).ToList();

        Assert.Equal(new[] { "Wahl", "Hersteller", "Typ", "Leistung [kW]", "Vorlauf [°C]",
                             "Rücklauf [°C]", "Betriebsart" }, kopf);
        // KA-E-5: die Kästchenspalte der Mehrfachwahl steht davor.
        Assert.Single(cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody td.epos-spalte-kaestchen .epos-wahlkaestchen"));
    }

    [Fact]
    public void Zwei_Listen_zwei_Pfeile_und_die_OK_Leiste_stehen_keine_Ueberlagerung_offen()
    {
        var cut = Aufbauen();

        Assert.Equal(2, cut.FindAll(".epos-raster").Count);
        var pfeile = cut.FindAll(".epos-zweispalten-knopf--richtung").Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(2, pfeile.Count);
        Assert.Contains(pfeile, t => t.Contains("In das Projekt übernehmen"));
        Assert.Contains(pfeile, t => t.Contains("Aus dem Projekt entfernen"));

        var knoepfe = cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Contains("OK", knoepfe);
        Assert.Contains("❌Abbrechen", knoepfe);
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
        // Die Anlagenseite steht nicht mehr eingebettet - sie ist die Überlagerung „Anlage…".
        Assert.Empty(cut.FindAll(".epos-wp-anlage"));
        Assert.False(cut.Instance.DetailOffen);
    }

    [Fact]
    public void Die_Zeile_zeigt_die_Werte_der_Anlage()
    {
        var cut = Aufbauen();
        var zellen = cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody td")
                        .Select(e => e.TextContent.Trim()).ToList();

        Assert.Contains("Bosch", zellen);
        Assert.Contains("WP Alpha", zellen);
        Assert.Contains("12", zellen);
        Assert.Contains("35", zellen);
        Assert.Contains("28", zellen);
        Assert.Contains(DbWerte.WP_BETRIEBSART_PARALLEL, zellen);
    }

    [Fact]
    public void Die_englischen_Texte_lassen_sich_setzen_der_Kontext_steht_im_Dialogkopf()
    {
        var cut = Render<WaermepumpenDialog>(p => p
            .Add(x => x.Zeilen, new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") })
            .Add(x => x.TitelText, "Heat pump management")
            .Add(x => x.KopfbandText, "Enter the heat pump data")
            .Add(x => x.LabelHinzu, "Add to project"));

        Assert.Equal("Heat pump management", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal("Enter the heat pump data", cut.Find(".epos-dialog-kopf .epos-dialog-kontext").TextContent.Trim());
        Assert.Empty(cut.FindAll(".epos-kontextzeile"));
        Assert.Contains(cut.FindAll("button").Select(b => b.TextContent.Trim()), t => t.Contains("Add to project"));
    }

    [Fact]
    public void Im_Assistenten_fehlen_OK_Leiste_Kostenknoepfe_und_Bearbeiten()
    {
        var cut = Aufbauen(wizard: true, kosten: (_, _) => Task.CompletedTask);
        var knoepfe = cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.DoesNotContain("OK", knoepfe);
        Assert.Empty(cut.FindAll(".epos-kostenleiste"));
        Assert.Empty(cut.FindAll(".epos-knopf--bearbeiten-projekt"));
        Assert.Single(cut.FindAll(".epos-knopf--anlage"));       // die Anlage geht auch im Assistenten auf
    }

    // =================================================================================
    // Knöpfe an ihrem Ort (4.2, 4.9)
    // =================================================================================

    [Fact]
    public void S3_Die_Knoepfe_stehen_an_ihrem_Ort_Katalogfuss_ohne_Vergleichen_und_Neu()
    {
        var cut = Aufbauen(projektsatzWege: Wege(), katalogsatzWege: Wege(), katalogLoeschen: _ => "",
                           rueckweg: Rueckweg(Array.Empty<Rueckwegvorschlag>()));

        var projekt = cut.Find(".epos-zweispalten-bereich--projekt .epos-zweispalten-kopfleiste").TextContent;
        Assert.Contains("Markierte auf diese Wärmepumpe umstellen", projekt);
        Assert.Contains(Resource.AUSWAHL_BTN_BEARBEITEN, projekt);
        Assert.Contains(Resource.KATRUECK_BTN, projekt);

        var fuss = cut.Find(".epos-zweispalten-fussleiste").TextContent;
        Assert.Contains("Löschen", fuss);
        Assert.Contains("Bearbeiten...", fuss);
        Assert.DoesNotContain(Resource.AUSWAHL_BTN_VERGLEICHEN, fuss);
        Assert.Empty(cut.FindAll(".epos-knopf--vergleichen"));
        Assert.Empty(cut.FindAll(".epos-knopf--neu"));
    }

    [Fact]
    public void Die_Detailzeile_zeigt_beim_Projektsatz_die_Kenndaten_der_Kopie_und_die_Marke()
    {
        var cut = Aufbauen();

        Assert.Equal(Satzmarke.Projektsatz, cut.FindComponent<Zweispaltenauswahl>().Instance.SatzArt);
        var werte = cut.Find(".epos-wp-satz").TextContent;
        Assert.Contains("Bosch", werte);
        Assert.Contains("WP Alpha", werte);
    }

    [Fact]
    public void Die_Detailzeile_zeigt_beim_Katalogsatz_dessen_Kenndaten_ohne_Kosten_und_Anlage()
    {
        var cut = Aufbauen(kosten: (_, _) => Task.CompletedTask);
        Assert.Single(cut.FindAll(".epos-kostenleiste"));

        KatalogzeileWaehlen(cut);

        Assert.Null(cut.Instance.Gewaehlt);
        Assert.Equal(Satzmarke.Katalogsatz, cut.FindComponent<Zweispaltenauswahl>().Instance.SatzArt);
        Assert.Contains("Alpha", cut.Find(".epos-wp-satz").TextContent);
        Assert.Empty(cut.FindAll(".epos-kostenleiste"));          // KA-E-12: Kosten nur beim Projektsatz
        Assert.Empty(cut.FindAll(".epos-knopf--anlage"));
    }

    [Fact]
    public void Die_Kostenknoepfe_wirken_auf_die_gewaehlte_Projektzeile()
    {
        var zeilen = new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") };
        var gerufen = new List<(WaermepumpeAnlageDaten, bool)>();
        var cut = Aufbauen(zeilen, kosten: (d, b) => { gerufen.Add((d, b)); return Task.CompletedTask; });

        cut.FindAll(".epos-kostenleiste button.epos-knopf")[0].Click();

        var (zeile, betrieb) = Assert.Single(gerufen);
        Assert.Same(zeilen[0], zeile);
        Assert.False(betrieb);
    }

    [Fact]
    public void Ohne_Zeile_steht_der_Leersatz()
    {
        var cut = Aufbauen(new List<WaermepumpeAnlageDaten>());

        Assert.False(cut.Instance.DetailOffen);
        Assert.Contains("Links eine Wärmepumpe markieren", cut.Markup);
        Assert.True(Pfeil(cut, 1).HasAttribute("disabled"));
    }

    // =================================================================================
    // „Anlage…" - die Anlagenseite als Überlagerung (KA-E-11)
    // =================================================================================

    [Fact]
    public void Anlage_oeffnet_die_Anlagenseite_als_Ueberlagerung_mit_eigenem_Titel()
    {
        var cut = Aufbauen();

        Knopf(cut, "Anlage…").Click();

        Assert.True(cut.Instance.DetailOffen);
        Assert.Single(cut.FindAll(".epos-wp-anlage-ueberlagerung .epos-wp-anlage"));
        Assert.Contains("WP Alpha", cut.Find(".epos-wp-anlage-ueberlagerung .epos-dialog-titel").TextContent);
        Assert.Contains("Konfiguration…", cut.FindAll(".epos-wp-anlage-ueberlagerung button").Select(b => b.TextContent.Trim()));
    }

    /// <summary>
    /// Eine Handlung, ein Ort: Die Kostenknöpfe des Projektsatzes stehen in der
    /// Detailzeile; die Überlagerung „Anlage…“ zeigt sie nicht noch einmal — auch dann
    /// nicht, wenn die Hülle ihr die Kostenwege mitgibt.
    /// </summary>
    [Fact]
    public void Anlage_Ueberlagerung_zeigt_keine_Kostenknoepfe_die_Detailzeile_traegt_sie()
    {
        var cut = Aufbauen(
            kosten: (_, _) => Task.CompletedTask,
            anlageGaben: d => new Dictionary<string, object>(AnlageGaben(d))
            {
                ["KostenBereit"] = new Func<bool>(() => true),
                ["KostenOeffnen"] = new Func<bool, Task>(_ => Task.CompletedTask)
            });
        Assert.NotEmpty(cut.FindAll(".epos-kostenleiste button.epos-knopf"));

        Knopf(cut, "Anlage…").Click();

        Assert.True(cut.Instance.DetailOffen);
        Assert.Empty(cut.FindAll(".epos-wp-anlage-ueberlagerung .epos-kostenleiste button.epos-knopf"));
        Assert.NotEmpty(cut.FindAll(".epos-kostenleiste button.epos-knopf")
            .Where(k => k.Closest(".epos-wp-anlage-ueberlagerung") is null));
    }

    [Fact]
    public void Anlage_OK_uebernimmt_die_Zeile_ins_Modell()
    {
        var zeilen = new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") };
        var uebernommen = new List<WaermepumpeAnlageDaten>();
        var cut = Aufbauen(zeilen, uebernehmen: d => uebernommen.Add(d));

        Knopf(cut, "Anlage…").Click();
        cut.FindAll(".epos-wp-anlage-ueberlagerung button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.False(cut.Instance.DetailOffen);
        Assert.Same(zeilen[0], Assert.Single(uebernommen));
    }

    [Fact]
    public void Anlage_Abbrechen_setzt_die_Zeile_auf_den_Stand_beim_Oeffnen_zurueck()
    {
        var zeilen = new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") };
        var uebernommen = new List<WaermepumpeAnlageDaten>();
        var cut = Aufbauen(zeilen, uebernehmen: d => uebernommen.Add(d));

        Knopf(cut, "Anlage…").Click();
        zeilen[0].Vorlauf = 55;
        zeilen[0].HeizstabLeistung = 9;
        cut.FindAll(".epos-wp-anlage-ueberlagerung button").First(b => b.TextContent.Trim() == "Abbrechen").Click();

        Assert.False(cut.Instance.DetailOffen);
        Assert.Equal(35, zeilen[0].Vorlauf);
        Assert.Equal(6, zeilen[0].HeizstabLeistung);
        Assert.Empty(uebernommen);
    }

    [Fact]
    public void Anlage_OK_mit_Mangel_schliesst_die_Ueberlagerung_nicht()
    {
        var uebernommen = new List<WaermepumpeAnlageDaten>();
        var cut = Aufbauen(uebernehmen: d => uebernommen.Add(d), mangel: "Temperaturen prüfen.");

        Knopf(cut, "Anlage…").Click();
        cut.FindAll(".epos-wp-anlage-ueberlagerung button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.True(cut.Instance.DetailOffen);
        Assert.Empty(uebernommen);
    }

    // =================================================================================
    // Übernehmen, Entfernen, Umstellen
    // =================================================================================

    [Fact]
    public void Uebernehmen_haengt_die_Zeile_an_waehlt_sie_und_oeffnet_ihre_Anlage()
    {
        var zeilen = new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") };
        var uebernommen = new List<WaermepumpeAnlageDaten>();
        var cut = Aufbauen(zeilen, uebernehmen: d => uebernommen.Add(d));

        Assert.True(Pfeil(cut, 0).HasAttribute("disabled"));
        KatalogzeileWaehlen(cut);
        Pfeil(cut, 0).Click();

        Assert.Equal(2, zeilen.Count);
        Assert.Equal("WP Neu", zeilen[1].Bezeichner);
        Assert.Same(zeilen[1], cut.Instance.Gewaehlt);
        Assert.Same(zeilen[1], Assert.Single(uebernommen));
        Assert.True(cut.Instance.DetailOffen);                     // eine neue Zeile braucht ihre Anlagendaten
    }

    [Fact]
    public void Sammeluebernahme_legt_je_angekreuztem_Satz_eine_Zeile_an_ohne_Anlage_zu_oeffnen()
    {
        var zeilen = new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") };
        var cut = Aufbauen(zeilen, katalog: ZweiSaetze());

        KatalogAnkreuzen(cut, 0, 1);
        Pfeil(cut, 0).Click();

        Assert.Equal(new[] { "WP Alpha", "WP Neu", "WP Zwei" }, zeilen.Select(z => z.Bezeichner));
        Assert.False(cut.Instance.DetailOffen);
        Assert.Equal(0, cut.Instance.KatalogWahl.Anzahl);
    }

    [Fact]
    public void OK_haelt_bei_einer_neuen_ungeprueften_Zeile_an_und_oeffnet_ihre_Anlage()
    {
        var zeilen = new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") };
        bool? ergebnis = null;
        var cut = Aufbauen(zeilen, katalog: ZweiSaetze(), geschlossen: b => ergebnis = b);
        KatalogAnkreuzen(cut, 0, 1);
        Pfeil(cut, 0).Click();

        cut.FindAll(".epos-dialog-fuss button, .epos-leiste button").Last(b => b.TextContent.Trim() == "OK").Click();

        Assert.Null(ergebnis);
        Assert.True(cut.Instance.DetailOffen);
        Assert.Equal("WP Neu", cut.Instance.Gewaehlt!.Bezeichner);
        Assert.Contains("WP Neu", cut.Instance.Meldung);
    }

    [Fact]
    public void OK_meldet_true_wenn_alle_Zeilen_geprueft_sind()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        Knopf(cut, "OK").Click();

        Assert.True(ergebnis);
    }

    [Fact]
    public void Entfernen_trifft_die_ZEILE_und_nicht_den_Index()
    {
        var a = Zeile("WP Alpha");
        var b = Zeile("WP Beta");
        var zeilen = new List<WaermepumpeAnlageDaten> { a, b };
        var entfernt = new List<WaermepumpeAnlageDaten>();
        var cut = Aufbauen(zeilen, entfernen: d => entfernt.Add(d));

        Projektwahl(cut, 1).Click();
        Pfeil(cut, 1).Click();

        Assert.Same(a, Assert.Single(zeilen));
        Assert.Same(b, Assert.Single(entfernt));
        Assert.Same(a, cut.Instance.Gewaehlt);                     // die Wahl wandert auf die erste Zeile
    }

    [Fact]
    public void Entfernen_wirkt_auf_alle_angekreuzten_Zeilen()
    {
        var zeilen = new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha"), Zeile("WP Beta"), Zeile("WP Gamma") };
        var cut = Aufbauen(zeilen);

        ProjektAnkreuzen(cut, 0, 2);
        Pfeil(cut, 1).Click();

        Assert.Equal("WP Beta", Assert.Single(zeilen).Bezeichner);
    }

    [Fact]
    public void Umstellen_braucht_genau_eine_Zeile_und_genau_einen_Satz_und_legt_keine_Zeile_an()
    {
        var zeilen = new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") };
        var gerufen = new List<(WaermepumpeAnlageDaten, int)>();
        var cut = Aufbauen(zeilen, katalog: ZweiSaetze(),
                           umstellen: (d, id) => { gerufen.Add((d, id)); d.Bezeichner = "WP Zwei"; return true; });

        Assert.True(Knopf(cut, "Markierte auf diese Wärmepumpe umstellen").HasAttribute("disabled"));
        KatalogAnkreuzen(cut, 0, 1);
        Assert.True(Knopf(cut, "Markierte auf diese Wärmepumpe umstellen").HasAttribute("disabled"));

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("td .epos-kaestchenzelle input")[0].Change(false);
        var umstellen = Knopf(cut, "Markierte auf diese Wärmepumpe umstellen");
        Assert.False(umstellen.HasAttribute("disabled"));
        umstellen.Click();

        var (zeile, id) = Assert.Single(gerufen);
        Assert.Same(zeilen[0], zeile);
        Assert.Equal(2, id);
        Assert.Single(zeilen);
        Assert.Same(zeilen[0], cut.Instance.Gewaehlt);
        Assert.Contains("WP Zwei", cut.FindAll(".epos-raster")[0].TextContent);
    }

    // =================================================================================
    // Bearbeiten je Bereich (4.6, KA-E-8, KA-E-13)
    // =================================================================================

    [Fact]
    public void Ein_ungesperrter_Katalogsatz_oeffnet_den_vollen_Katalogeditor_mit_Vorwahl()
    {
        string? vorwahl = null;
        var cut = Aufbauen(katalogsatzWege: Wege(),
                           editorGaben: n => { vorwahl = n; return new Dictionary<string, object>(); });

        KatalogzeileWaehlen(cut);
        Knopf(cut, "Bearbeiten...").Click();

        Assert.True(cut.Instance.EditorOffen);
        Assert.Equal("WP Neu", vorwahl);
        Assert.Null(cut.Instance.Bearbeitung);
    }

    [Fact]
    public void Mehrere_Katalogsaetze_oeffnen_die_Satzbearbeitung_gesperrte_uebersprungen()
    {
        var cut = Aufbauen(katalog: ZweiSaetze(2), katalogsatzWege: Wege(),
                           editorGaben: _ => new Dictionary<string, object>());

        KatalogAnkreuzen(cut, 0, 1);
        Knopf(cut, "Bearbeiten...").Click();

        var b = cut.Instance.Bearbeitung!.Value;
        Assert.Equal(Satzmarke.Katalogsatz, b.Art);
        Assert.Equal(2, b.Saetze.Count);
        Assert.Contains("„WP Zwei“", cut.Find(".epos-satzbearbeitung-hinweis--uebersprungen").TextContent);
        Assert.False(cut.Instance.EditorOffen);
    }

    [Fact]
    public void Ein_gesperrter_Katalogsatz_oeffnet_nur_lesend()
    {
        var cut = Aufbauen(katalog: ZweiSaetze(1), katalogsatzWege: Wege(),
                           editorGaben: _ => new Dictionary<string, object>());

        KatalogzeileWaehlen(cut);
        Knopf(cut, "Bearbeiten...").Click();

        Assert.True(cut.FindComponent<Satzbearbeitung>().Instance.NurLesend);
    }

    [Fact]
    public void Projekt_Bearbeiten_nimmt_die_Projektkopien_je_Geraet_einmal_und_speichert_alle()
    {
        var a = Zeile("WP Alpha");
        var b = Zeile("WP Alpha");                                 // dieselbe Kopie (IdWp 1)
        var gespeichert = new List<(int Id, IReadOnlyList<BrowserFeldwert> Felder)>();
        var cut = Aufbauen(new List<WaermepumpeAnlageDaten> { a, b }, projektsatzWege: Wege(gespeichert));

        ProjektAnkreuzen(cut, 0, 1);
        cut.Find(".epos-knopf--bearbeiten-projekt").Click();

        var satz = Assert.Single(cut.Instance.Bearbeitung!.Value.Saetze);
        Assert.Equal(1, satz.Id);
        cut.Find(".epos-satzbearbeitung input[type=text]:not([readonly])").Input("Neuwerk");
        cut.Find(".epos-satzbearbeitung-speichern").Click();
        Assert.Equal(1, Assert.Single(gespeichert).Id);
        Assert.Null(cut.Instance.Bearbeitung);
    }

    // =================================================================================
    // Rückweg und Löschen (KA-E-9, KA-E-16)
    // =================================================================================

    private static Rueckwegwege Rueckweg(IReadOnlyList<Rueckwegvorschlag> zeilen, List<IReadOnlyList<int>>? gefragt = null)
        => new()
        {
            Vorschau = ids => { gefragt?.Add(ids); return zeilen; },
            NameBelegt = _ => false,
            Uebernehmen = _ => new KatalogSpeicherErgebnis(true, "„WP Alpha“ übernommen", ""),
        };

    [Fact]
    public void Der_Rueckweg_fragt_zu_den_Projektkopien_der_Auswahl()
    {
        var gefragt = new List<IReadOnlyList<int>>();
        var vorschlag = new Rueckwegvorschlag(1, "WP Alpha", "WP Alpha", Rueckwegsperre.Keine, "WP Alpha (Projekt)");
        var cut = Aufbauen(rueckweg: Rueckweg(new[] { vorschlag }, gefragt));

        cut.Find(".epos-knopf--rueckweg").Click();

        Assert.Equal(new[] { 1 }, Assert.Single(gefragt));
        Assert.NotNull(cut.Instance.Rueckweg);
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
    }

    [Fact]
    public void Loeschen_fragt_und_loescht_den_Katalogsatz_nach_Id()
    {
        var geloescht = new List<int>();
        var cut = Aufbauen(katalog: ZweiSaetze(), katalogLoeschen: id => { geloescht.Add(id); return ""; });

        KatalogzeileWaehlen(cut, 1);
        cut.Find(".epos-knopf--loeschen").Click();
        Assert.Empty(geloescht);
        Knopf(cut, "Ja").Click();

        Assert.Equal(2, Assert.Single(geloescht));
    }

    // =================================================================================
    // Abschluss
    // =================================================================================

    [Fact]
    public void Abbrechen_und_Esc_melden_false()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);
        Knopf(cut, "❌Abbrechen").Click();
        Assert.False(ergebnis);

        bool? zweites = null;
        var cut2 = Aufbauen(geschlossen: b => zweites = b);
        cut2.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(zweites);
    }

    [Fact]
    public void Esc_bei_offener_Anlage_schliesst_den_Dialog_nicht()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);
        Knopf(cut, "Anlage…").Click();

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Null(ergebnis);
    }

    [Fact]
    public void Kreuz_meldet_false()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog-kopf .epos-dialog-zu").Click();

        Assert.False(ergebnis);
    }

    [Fact]
    public void Ohne_Titel_zeigt_der_Kopf_weder_Titel_noch_Kreuz()
    {
        var cut = Render<WaermepumpenDialog>(p => p
            .Add(x => x.Zeilen, new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") })
            .Add(x => x.Katalog, () => Katalog)
            .Add(x => x.Katalogprofil, Katalogprofil)
            .Add(x => x.TitelText, ""));

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
        Assert.NotEmpty(cut.FindAll(".epos-dialog-kopf"));
    }

    [Fact]
    public void Der_Erstfokus_liegt_auf_der_aeusseren_Wurzel()
    {
        var cut = Aufbauen();

        var wurzel = cut.Find(".epos-dialog").GetAttribute("blazor:elementReference");
        var fokus = Assert.Single(JSInterop.Invocations,
            a => a.Identifier == "Blazor._internal.domWrapper.focus");
        Assert.Equal(wurzel, ((Microsoft.AspNetCore.Components.ElementReference)fokus.Arguments[0]!).Id);
        Assert.Equal(true, fokus.Arguments[1]);
    }
}
