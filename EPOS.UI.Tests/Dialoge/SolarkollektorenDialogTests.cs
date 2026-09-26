using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Solarthermie;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Eingabe der Solarkollektoren (iU9-W7.7). Soll ist die Feldkarte von
/// <c>Form_SolarKollektoren</c>: zwei Listen mit den beiden Pfeilen, der Modulblock
/// mit sechs Anzeigefeldern und die Gruppe „Kollektor" mit sechs Bedienelementen.
/// </summary>
public class SolarkollektorenDialogTests : EposBunitContext
{

    /// <summary>
    /// Der Filterstand DIESES Prüfstands. Ohne ihn nähme der Dialog den aus dem
    /// <c>Katalogfilterregister</c> — der lebt prozessweit, und xunit fährt
    /// Testklassen nebeneinander. Dass das Register wirklich teilt, prüft
    /// <c>KatalogfilterstandTests</c>.
    /// </summary>
    private readonly Katalogfilterstand _filterstand = new();
    /// <summary>
    /// Das PROFIL des Projektdialogs (W14a-E-10 / S2.1): dieselben sechs Spalten wie
    /// in der Verwaltung, dazu die siebte „im Projekt verwendet" (Q12).
    /// </summary>
    private static readonly Katalogfilterprofil Profil =
        Katalogfilterprofil.MitVerwendung(Anlagenart.Solarkollektoren,
            s => Resource.ResourceManager.GetString(s) ?? s);

    private static IReadOnlyList<Katalogfilterzeile> Katalogzeilen() => new[]
    {
        new Katalogfilterzeile(11, "Vitosol 200")
            .MitText(Katalogfilterprofil.SpBezeichner, "Vitosol 200")
            .MitText(Katalogfilterprofil.SpHersteller, "Viessmann")
            .MitText(Katalogfilterprofil.SpKollektortyp, "Flach")
            .MitZahl(Katalogfilterprofil.SpApertur, 2.31, 2)
            .MitZahl(Katalogfilterprofil.SpEtaNull, 0.8, 3)
            .MitZahl(Katalogfilterprofil.SpK1, 3.5, 2),

        new Katalogfilterzeile(12, "Vitosol 300")
            .MitText(Katalogfilterprofil.SpBezeichner, "Vitosol 300")
            .MitText(Katalogfilterprofil.SpHersteller, "Viessmann")
            .MitText(Katalogfilterprofil.SpKollektortyp, "Röhre")
            .MitZahl(Katalogfilterprofil.SpApertur, 3.0, 2)
            .MitZahl(Katalogfilterprofil.SpEtaNull, 0.64, 3)
            .MitZahl(Katalogfilterprofil.SpK1, 1.0, 2),
    };

    public SolarkollektorenDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static ErzeugerZeile Zeile(int schluessel, string name, int geraetId = 11) => new()
    {
        Schluessel = schluessel,
        Bezeichner = name,
        GeraetId = geraetId,
        AnzahlModule = 4,
        Neigung = 30,
        Azimut = 0,
        Vorlauf = 60,
        Ruecklauf = 40
    };

    private static ErzeugerDetail Detail(string name) => new(
        name, "",
        new[] { ("Kollektor:", "Flach"), ("Hersteller :", "Viessmann"),
                ("Beschreibung :", "Flachkollektor"), ("Aperturfläche:", "2,31") });

    /// <summary>
    /// Die Felder des Aufklappers „Alle Daten anzeigen" — der Feldsatz des
    /// Kollektorprofils in Kurzform: der Bezeichner nur lesbar, die übrigen editierbar.
    /// </summary>
    private static List<BrowserFeldwert> Katalogfelder(string name) => new()
    {
        new BrowserFeldwert
        {
            Schluessel = KatalogBrowserProfil.FeldBezeichner, Bezeichnung = "Name:",
            Art = BrowserFeldArt.Text, Editierbar = false, Wert = name
        },
        new BrowserFeldwert
        {
            Schluessel = KatalogBrowserProfil.FeldKollektortyp, Bezeichnung = "Kollektor:",
            Art = BrowserFeldArt.Text, Editierbar = true, Wert = "Flach"
        },
        new BrowserFeldwert
        {
            Schluessel = KatalogBrowserProfil.FeldInvestitionskosten,
            Bezeichnung = "Investitionskosten:", Einheit = "€",
            Art = BrowserFeldArt.Zahl, Editierbar = true, Wert = "850"
        }
    };

    private IRenderedComponent<SolarkollektorenDialog> Aufbauen(
        List<ErzeugerZeile>? zeilen = null,
        Func<int, AufnahmeErgebnis>? aufnehmen = null,
        Action<ErzeugerZeile>? entfernen = null,
        Action<ErzeugerZeile>? uebernehmen = null,
        Func<string, bool, IReadOnlyDictionary<string, object>>? editorGaben = null,
        Func<string, bool>? katalogLoeschen = null,
        Func<string, IReadOnlyList<BrowserFeldwert>?>? katalogfelder = null,
        Func<string, IReadOnlyList<BrowserFeldwert>, KatalogSpeicherErgebnis>? felderSpeichern = null,
        Func<ErzeugerZeile?, bool, Task>? kostenOeffnen = null,
        bool wizard = false,
        Action<bool>? geschlossen = null)
        => Render<SolarkollektorenDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<ErzeugerZeile> { Zeile(1, "Vitosol 200") })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Detail, Detail)
            .Add(x => x.Modulflaeche, _ => 2.5)
            .Add(x => x.Aufnehmen, aufnehmen ?? (_ => new AufnahmeErgebnis(Zeile(9, "Vitosol 300", 12))))
            .Add(x => x.Entfernen, entfernen)
            .Add(x => x.Uebernehmen, uebernehmen)
            .Add(x => x.EditorGaben, editorGaben)
            .Add(x => x.KatalogLoeschen, katalogLoeschen ?? (_ => true))
            .Add(x => x.Katalogfelder, katalogfelder)
            .Add(x => x.KatalogfelderSpeichern, felderSpeichern)
            .Add(x => x.KostenOeffnen, kostenOeffnen)
            .Add(x => x.Wizard, wizard)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IElement Knopf(IRenderedComponent<SolarkollektorenDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Der_Feldbestand_der_Karte_steht()
    {
        var cut = Aufbauen();

        Assert.Equal(2, cut.FindAll(".epos-raster").Count);
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-uebernahme button").Count);

        var ueberschriften = cut.FindAll(".epos-untergruppe").Select(e => e.TextContent).ToList();
        Assert.Contains("Auswahl in Projekt:", ueberschriften);
        Assert.Contains("Auswahl in DB:", ueberschriften);

        var gruppen = cut.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Modul", "Kollektor" }, gruppen);

        var knoepfe = cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList();

        // „Kollektor in DB ändern…" heisst seit dem 15.09.2026 „Bearbeiten…" und steht
        // im Modulbereich; „neu…" und „löschen" bleiben bei der Liste.
        Assert.Contains("Bearbeiten...", knoepfe);
        Assert.DoesNotContain("Kollektor in DB ändern...", knoepfe);
        Assert.Contains("Kollektor in DB neu...", knoepfe);
        Assert.Contains("Kollektor in DB löschen", knoepfe);
        Assert.Contains("Übernehmen", knoepfe);
    }

    /// <summary>
    /// <b>„Bearbeiten…" steht im MODULBEREICH, „neu…" und „löschen" bei der Liste</b>
    /// (Anwenderentscheid 15.09.2026, „alle sechs Erzeuger im gleichen Schema"): Der
    /// eine Knopf wirkt auf den gewählten SATZ und gehört deshalb dorthin, wo dieser
    /// Satz steht; die beiden anderen wirken auf die LISTE.
    /// </summary>
    [Fact]
    public void Bearbeiten_steht_im_Modulbereich_und_Neu_und_Loeschen_bei_der_Liste()
    {
        var cut = Aufbauen();

        var listenknoepfe = cut.FindAll(".epos-zweispalten-spalte")[1]
                               .QuerySelectorAll(".epos-leiste button")
                               .Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Kollektor in DB neu...", "Kollektor in DB löschen" }, listenknoepfe);

        var modulknoepfe = cut.FindAll(".epos-gruppenkopf-koerper")[0]
                              .QuerySelectorAll(".epos-leiste button")
                              .Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Bearbeiten..." }, modulknoepfe);
    }

    [Fact]
    public void Der_Modulblock_ist_reine_Anzeige()
    {
        var cut = Aufbauen();
        var modul = cut.FindAll(".epos-gruppenkopf-koerper")[0];

        // Name plus die vier Detailfelder.
        Assert.Equal(5, modul.QuerySelectorAll("input[readonly]").Length);
        Assert.Empty(modul.QuerySelectorAll("input:not([readonly])"));
    }

    [Fact]
    public void Die_Kollektorgruppe_traegt_vier_Bedienelemente()
    {
        var cut = Aufbauen();
        var kollektor = cut.FindAll(".epos-gruppenkopf-koerper")[1];

        // Anzahl, Neigung, Azimut plus die gerechnete Flaeche - Vor- und Ruecklauf fuehrt
        // die Gruppe nicht, sie haetten beim Kollektor keinen Rechenweg.
        Assert.Equal(3, kollektor.QuerySelectorAll("input:not([readonly])").Length);
        Assert.DoesNotContain("Vorlauf", kollektor.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Rücklauf", kollektor.TextContent, StringComparison.Ordinal);
        Assert.Single(kollektor.QuerySelectorAll("input[readonly]"));
        Assert.Contains("Übernehmen", kollektor.QuerySelectorAll("button").Select(b => b.TextContent.Trim()));
    }

    /// <summary>Die Maske ist lokalisiert (22 englische Texte, W7.9).</summary>
    [Fact]
    public void Die_englischen_Texte_lassen_sich_setzen()
    {
        var cut = Render<SolarkollektorenDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Vitosol 200") })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Detail, Detail)
            .Add(x => x.TitelText, "Entering the solar panels")
            .Add(x => x.LabelAnzahl, "Modules:")
            .Add(x => x.BtnUebernehmenText, "Take over"));

        Assert.Equal("Entering the solar panels", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Modules:", cut.FindAll(".epos-feld-text").Select(e => e.TextContent));
        Assert.Contains("Take over", cut.FindAll("button").Select(b => b.TextContent.Trim()));
    }

    [Fact]
    public void Im_Assistenten_fehlt_die_OK_Leiste()
    {
        var cut = Aufbauen(wizard: true);
        Assert.Empty(cut.FindAll(".epos-status"));
    }

    // =================================================================================
    // Die Kollektorgruppe erscheint nur bei einer Projektzeile
    // =================================================================================

    [Fact]
    public void Eine_Katalogzeile_zeigt_das_Detail_OHNE_Kollektorgruppe()
    {
        // dataGridView1_Click:289 blendete groupBox_Kollektor aus.
        var cut = Aufbauen();
        Assert.Equal(2, cut.FindAll(".epos-gruppenkopf-titel").Count);

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr button")[0].Click();

        var gruppen = cut.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Modul" }, gruppen);
    }

    [Fact]
    public void Eine_Projektzeile_fuellt_die_Kollektorgruppe()
    {
        var cut = Aufbauen();
        var werte = cut.FindAll(".epos-gruppenkopf-koerper")[1]
                       .QuerySelectorAll("input").Select(e => e.GetAttribute("value")).ToList();

        // Reihenfolge: Anzahl, Aperturflaeche (gerechnet), Neigung, Azimut.
        Assert.Equal(4, werte.Count);
        Assert.Equal("4", werte[0]);
        Assert.Equal("10", werte[1]);            // 2,5 m² x 4
        Assert.Equal("30", werte[2]);
        Assert.Equal("0", werte[3]);
    }

    /// <summary>
    /// <b>Die Aperturfläche steht gerundet da</b> — höchstens zwei Nachkommastellen, in der
    /// Kultur des Anwenders. 2,51 m² × 3 ist als Gleitkommazahl 7,529999999999999; ohne
    /// Rundung stand genau das im Feld.
    /// </summary>
    [Theory]
    [InlineData("de-DE", "7,53")]
    [InlineData("en-US", "7.53")]
    public void Die_Aperturflaeche_steht_gerundet_in_der_Kultur_des_Anwenders(string kultur, string erwartet)
    {
        using var _ = new Kulturvorrichtung(kultur);
        var zeile = Zeile(1, "Vitosol 200");
        zeile.AnzahlModule = 3;

        var cut = Render<SolarkollektorenDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { zeile })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Detail, Detail)
            .Add(x => x.Modulflaeche, _ => 2.51));

        Assert.Equal(erwartet, cut.FindAll(".epos-gruppenkopf-koerper")[1]
                                  .QuerySelectorAll("input")[1].GetAttribute("value"));
    }

    [Fact]
    public void Die_Aperturflaeche_folgt_der_Modulanzahl_live()
    {
        // textBox_Anzahl_TextChanged:367 rechnete bei jedem Tastendruck nach.
        var cut = Aufbauen();
        var kollektor = cut.FindAll(".epos-gruppenkopf-koerper")[1];

        kollektor.QuerySelectorAll("input")[0].Input("6");

        Assert.Equal("15", cut.FindAll(".epos-gruppenkopf-koerper")[1]
                              .QuerySelectorAll("input")[1].GetAttribute("value"));
    }

    // =================================================================================
    // Aufnehmen, Entfernen, Uebernehmen
    // =================================================================================

    [Fact]
    public void Der_linke_Pfeil_ist_ohne_Katalogwahl_gesperrt()
    {
        var cut = Aufbauen();
        var pfeile = cut.FindAll(".epos-zweispalten-uebernahme button");

        Assert.True(pfeile[0].HasAttribute("disabled"));    // ◀ ohne Katalogwahl
        Assert.False(pfeile[1].HasAttribute("disabled"));   // ▶ mit Projektzeile
    }

    [Fact]
    public void Der_linke_Pfeil_legt_eine_Zeile_an_und_waehlt_sie()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Vitosol 200") };
        int gerufen = 0;
        var cut = Aufbauen(zeilen, aufnehmen: id =>
        {
            gerufen = id;
            return new AufnahmeErgebnis(Zeile(9, "Vitosol 300", 12));
        });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr button")[1].Click();  // Katalogzeile 2
        cut.FindAll(".epos-zweispalten-uebernahme button")[0].Click();

        Assert.Equal(12, gerufen);
        Assert.Equal(2, zeilen.Count);
        Assert.Equal("Vitosol 300", cut.Instance.Projektzeile!.Bezeichner);
    }

    [Fact]
    public void Eine_Ablehnung_beim_Aufnehmen_meldet_und_legt_nichts_an()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Vitosol 200") };
        var cut = Aufbauen(zeilen, aufnehmen: _ =>
            new AufnahmeErgebnis(null, "Der Datensatz konnte nicht in das Projekt übernommen werden.", true));

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr button")[0].Click();
        cut.FindAll(".epos-zweispalten-uebernahme button")[0].Click();

        Assert.Single(zeilen);
        Assert.Contains("nicht in das Projekt", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Der_rechte_Pfeil_entfernt_genau_die_gewaehlte_Zeile()
    {
        var a = Zeile(1, "Vitosol 200");
        var b = Zeile(2, "Vitosol 300", 12);
        var zeilen = new List<ErzeugerZeile> { a, b };

        var entfernt = new List<ErzeugerZeile>();
        var cut = Aufbauen(zeilen, entfernen: z => entfernt.Add(z));

        cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr")[1]
           .QuerySelector("button")!.Click();
        cut.FindAll(".epos-zweispalten-uebernahme button")[1].Click();

        Assert.Single(zeilen);
        Assert.Same(a, zeilen[0]);
        Assert.Same(b, entfernt.Single());
    }

    [Fact]
    public void Uebernehmen_schreibt_die_drei_Ganzzahlen_und_meldet()
    {
        var zeile = Zeile(1, "Vitosol 200");
        var uebernommen = new List<ErzeugerZeile>();
        var cut = Aufbauen(new List<ErzeugerZeile> { zeile }, uebernehmen: z => uebernommen.Add(z));

        var kollektor = cut.FindAll(".epos-gruppenkopf-koerper")[1];
        kollektor.QuerySelectorAll("input")[0].Input("6");     // Anzahl
        kollektor.QuerySelectorAll("input")[2].Input("35");    // Neigung
        kollektor.QuerySelectorAll("input")[3].Input("15");    // Azimut
        Knopf(cut, "Übernehmen").Click();

        Assert.Equal(6, zeile.AnzahlModule);
        Assert.Equal(35, zeile.Neigung);
        Assert.Equal(15, zeile.Azimut);
        // Vor- und Ruecklauf der Zeile fasst der Dialog nicht an.
        Assert.Equal(60, zeile.Vorlauf);
        Assert.Equal(40, zeile.Ruecklauf);
        Assert.Same(zeile, uebernommen.Single());

        // A-24: Der 500-ms-Bildblitz mit Thread.Sleep wird ein Hinweis.
        Assert.Contains("übernommen", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Ein_leeres_Ganzzahlfeld_gilt_als_Null()
    {
        // Program.GanzzahlPruefen(..., leerErlaubt: true) im Vorlaeufer.
        var zeile = Zeile(1, "Vitosol 200");
        var cut = Aufbauen(new List<ErzeugerZeile> { zeile });

        cut.FindAll(".epos-gruppenkopf-koerper")[1].QuerySelectorAll("input")[3].Input("");
        Knopf(cut, "Übernehmen").Click();

        Assert.Equal(0, zeile.Azimut);
    }

    // =================================================================================
    // Katalogpflege
    // =================================================================================

    [Fact]
    public void Katalog_aendern_und_loeschen_sind_ohne_Katalogwahl_gesperrt()
    {
        var cut = Aufbauen();

        Assert.True(Knopf(cut, "Bearbeiten...").HasAttribute("disabled"));
        Assert.True(Knopf(cut, "Kollektor in DB löschen").HasAttribute("disabled"));
        Assert.False(Knopf(cut, "Kollektor in DB neu...").HasAttribute("disabled"));
    }

    [Fact]
    public void Kollektor_aendern_zeigt_den_Editor_in_der_Ueberlagerung()
    {
        string? name = null;
        bool? neu = null;
        var cut = Aufbauen(editorGaben: (n, b) =>
        {
            name = n; neu = b;
            return new Dictionary<string, object> { ["Daten"] = new SolarkollektorKatalogDaten { Name = n } };
        });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr button")[0].Click();
        Knopf(cut, "Bearbeiten...").Click();

        Assert.True(cut.Instance.EditorOffen);
        Assert.Equal("Vitosol 200", name);
        Assert.False(neu);
    }

    /// <summary>
    /// Die Katalogeditor-Ueberlagerung ist seit dem Anwenderentscheid 15.09.2026
    /// ("das Kreuz steht beim Titel") nicht mehr Schliessbar="false": Ihr eigenes
    /// Kreuz verwirft den Editor - derselbe Weg wie ihr <c>Geschlossen</c>
    /// (<c>_editorGaben = null</c>).
    /// </summary>
    [Fact]
    public void Ueberlagerungskreuz_verwirft_den_Katalogeditor()
    {
        var cut = Aufbauen(editorGaben: (n, b) =>
            new Dictionary<string, object> { ["Daten"] = new SolarkollektorKatalogDaten { Name = n } });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr button")[0].Click();
        Knopf(cut, "Bearbeiten...").Click();
        Assert.True(cut.Instance.EditorOffen);

        cut.Find(".epos-ueberlagerung-zu").Click();
        Assert.False(cut.Instance.EditorOffen);
    }

    /// <summary>
    /// Ein Titel, eine Stelle (Befund „Doppeltes Kreuz dürfen nicht sein!"): Die
    /// Ueberlagerung trägt Titel UND Kreuz, der eingebettete Katalogeditor keins von
    /// beiden — <c>TitelText=""</c> steht RECHTS vom Parametersatz der Hülle und gilt
    /// deshalb auch dann, wenn dieser einen Titel mitbrächte.
    /// </summary>
    [Fact]
    public void Die_Ueberlagerung_Katalogeditor_zeigt_nur_ein_Kreuz()
    {
        var cut = Aufbauen(editorGaben: (n, b) =>
            new Dictionary<string, object> { ["Daten"] = new SolarkollektorKatalogDaten { Name = n } });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr button")[0].Click();
        Knopf(cut, "Bearbeiten...").Click();

        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
    }

    [Fact]
    public void Kollektor_neu_fragt_erst_den_Namen()
    {
        string? name = null;
        bool? neu = null;
        var cut = Aufbauen(editorGaben: (n, b) =>
        {
            name = n; neu = b;
            return new Dictionary<string, object> { ["Daten"] = new SolarkollektorKatalogDaten { Name = n } };
        });

        Knopf(cut, "Kollektor in DB neu...").Click();
        Assert.False(cut.Instance.EditorOffen);

        cut.Find(".epos-ueberlagerung input").Input("Neuer Kollektor");
        cut.Find(".epos-ueberlagerung").QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == "OK").Click();

        Assert.True(cut.Instance.EditorOffen);
        Assert.Equal("Neuer Kollektor", name);
        Assert.True(neu);
    }

    [Fact]
    public void Kollektor_loeschen_fragt_nach()
    {
        var geloescht = new List<string>();
        var cut = Aufbauen(katalogLoeschen: n => { geloescht.Add(n); return true; });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr button")[0].Click();
        Knopf(cut, "Kollektor in DB löschen").Click();

        Assert.Single(cut.FindAll(".epos-rueckfrage"));
        Assert.Empty(geloescht);

        cut.Find(".epos-rueckfrage").QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == "Ja").Click();

        Assert.Equal("Vitosol 200", geloescht.Single());
    }

    // =================================================================================
    // Abschluss und Tastatur
    // =================================================================================

    [Fact]
    public void OK_und_Abbrechen_melden_das_Ergebnis()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);
        Knopf(cut, "OK").Click();
        Assert.True(ergebnis);

        ergebnis = null;
        var cut2 = Aufbauen(geschlossen: b => ergebnis = b);
        Knopf(cut2, "Abbrechen").Click();
        Assert.False(ergebnis);
    }

    [Fact]
    public void Esc_schliesst_nur_ohne_offene_Rueckfrage()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr button")[0].Click();
        Knopf(cut, "Kollektor in DB löschen").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(ergebnis);
    }

    /// <summary>Das Schliesskreuz im Kopf wirkt wie Abbrechen/Esc: schliesst mit false.</summary>
    [Fact]
    public void Kreuz_schliesst_mit_false()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog-zu").Click();
        Assert.False(ergebnis);
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

    // =================================================================================
    //  Stufe S2.1 / S2.3 / S2.5 - Anwenderentscheid W14a-E-10 vom 07.09.2026
    // =================================================================================

    /// <summary>Die Katalogliste - das UNTERE der beiden Raster.</summary>
    private static IReadOnlyList<AngleSharp.Dom.IElement> Katalogzeilen(
        IRenderedComponent<SolarkollektorenDialog> cut)
        => cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr").ToList();

    /// <summary>
    /// <b>S2.1 — der Filter sitzt im SPALTENKOPF.</b> Der Ausdruck steht im
    /// <c>Katalogfilterstand</c> des Wirtes, <c>Katalogfilter.Anwenden</c>
    /// schränkt die Menge im Kern ein, und das Raster bekommt die BEREITS
    /// eingeschränkte Liste (5.6.6 — gefiltert wird vor dem Raster, nie im Raster).
    /// </summary>
    [Fact]
    public void S2_1_Der_Spaltenfilter_schraenkt_die_Katalogliste_ein()
    {
        var cut = Aufbauen();
        Assert.Equal(2, Katalogzeilen(cut).Count);
        Assert.Equal("2 von 2 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);

        _filterstand.Setzen(Katalogfilterprofil.SpKollektortyp, "Röhre");
        cut.Render();

        Assert.Single(Katalogzeilen(cut));
        Assert.Equal("1 von 2 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);
        Assert.Contains("Vitosol 300", Katalogzeilen(cut)[0].TextContent);

        // Der Trichter dieser Spalte ist jetzt GEFUELLT - im Markup, nicht nur
        // in der Farbe (Auflage des Anwenders zu Rev. 3).
        Assert.Contains(cut.FindAll(".epos-trichter-bild path"),
                        e => e.GetAttribute("fill") == "currentColor");

        // Und der Ruecksetzer der Suchzeile holt alles zurueck.
        cut.Find(".epos-katalog-ruecksetzer").Click();
        Assert.Equal(2, Katalogzeilen(cut).Count);
    }

    /// <summary>
    /// <b>S2.1 — jede Parameterspalte sortiert</b>, im Zyklus auf → ab → aus
    /// (höchstens eine Spalte zugleich, 5.6.2).
    /// </summary>
    [Fact]
    public void S2_1_Die_Katalogliste_laesst_sich_ueber_den_Spaltenkopf_sortieren()
    {
        var cut = Aufbauen();

        _filterstand.Sortieren(Katalogfilterprofil.SpApertur);
        cut.Render();
        Assert.Contains("Vitosol 200", Katalogzeilen(cut)[0].TextContent);

        _filterstand.Sortieren(Katalogfilterprofil.SpApertur);
        cut.Render();
        Assert.False(_filterstand.Aufsteigend);
        Assert.Contains("Vitosol 300", Katalogzeilen(cut)[0].TextContent);

        _filterstand.Sortieren(Katalogfilterprofil.SpApertur);
        Assert.Equal("", _filterstand.Sortierspalte);
    }

    /// <summary>
    /// <b>S2.1 — die Markierung hängt am BEZEICHNER.</b> Sie bleibt stehen, auch
    /// wenn ein Filter die Zeile ausblendet; der Übernahmeknopf bleibt frei und
    /// nimmt DENSELBEN Satz auf (Hausregel aus dem <c>EnergietraegerDialog</c>, W4).
    /// </summary>
    [Fact]
    public void S2_1_Die_Markierung_ueberlebt_einen_Filterwechsel()
    {
        var cut = Aufbauen();

        Katalogzeilen(cut)[0].QuerySelector(".epos-anlagenwahl")!.Click();
        Assert.False(cut.FindAll(".epos-zweispalten-uebernahme button")[0].HasAttribute("disabled"));

        // Ein Filter, der GENAU diese Zeile ausblendet.
        _filterstand.Setzen(Katalogfilterprofil.SpKollektortyp, "Röhre");
        cut.Render();

        Assert.Single(Katalogzeilen(cut));
        Assert.False(cut.FindAll(".epos-zweispalten-uebernahme button")[0].HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>S2.3 / Frage Q12 — „im Projekt verwendet".</b> EINMAL für die ganze Liste
    /// aus der Projektliste des Dialogs gestempelt, nicht je Zeile und nicht aus
    /// der Datenbank: Der Dialog schreibt erst beim OK zurück, eine Zählabfrage
    /// wäre nach der ersten Übernahme veraltet.
    /// </summary>
    [Fact]
    public void S2_3_Die_Spalte_im_Projekt_verwendet_zaehlt_die_Projektliste()
    {
        var cut = Aufbauen(zeilen: new List<ErzeugerZeile> { Zeile(1, "Vitosol 200") });

        var kopf = cut.FindAll(".epos-raster")[1]
                      .QuerySelectorAll("th").Select(e => e.TextContent.Trim()).ToList();
        Assert.Contains(kopf, k => k.StartsWith("im Projekt verwendet"));

        var zeilen = Katalogzeilen(cut);
        Assert.Equal(2, zeilen.Count);
        Assert.Equal(1, zeilen.Count(z => z.QuerySelectorAll("td").Last().TextContent.Trim() == "Ja"));

        var traegt = zeilen.First(z => z.QuerySelectorAll("td").Last().TextContent.Trim() == "Ja");
        Assert.Contains("Vitosol 200", traegt.TextContent);
    }

    /// <summary>
    /// <b>S2.5 / Frage Q2 — der Filterstand überlebt Schließen und Öffnen.</b>
    /// Hier steht dafür ein EIGENER Stand statt des <c>Katalogfilterregister</c>
    /// (xunit fährt Testklassen nebeneinander); dass das Register ihn wirklich
    /// zwischen Verwaltung und Projektdialog teilt, prüft
    /// <c>EPOS.Kern.Tests/KatalogfilterstandTests</c>.
    /// </summary>
    [Fact]
    public void S2_5_Der_Filterstand_ueberlebt_einen_zweiten_Aufbau()
    {
        var ersterAufbau = Aufbauen();
        _filterstand.Setzen(Katalogfilterprofil.SpKollektortyp, "Röhre");
        ersterAufbau.Render();
        Assert.Single(Katalogzeilen(ersterAufbau));

        // Der Dialog geht zu und wieder auf - derselbe Stand, dieselbe Sicht.
        var zweiterAufbau = Aufbauen();

        Assert.Single(Katalogzeilen(zweiterAufbau));
        Assert.Equal("1 von 2 Sätzen", zweiterAufbau.Find(".epos-katalog-treffer").TextContent);
        Assert.Single(zweiterAufbau.FindAll(".epos-katalog-ruecksetzer"));
    }

    // =================================================================================
    // Der Aufklapper „Alle Daten anzeigen" (Anwenderentscheid 15.09.2026)
    // =================================================================================

    /// <summary>
    /// Ohne Weg zu den Katalogfeldern kein Aufklapper — Hausregel „kein Delegat, kein
    /// Knopf". Und er gehört dem KATALOGsatz: Steht eine Projektzeile, ist er weg.
    /// </summary>
    [Fact]
    public void Der_Aufklapper_steht_nur_mit_Weg_und_nur_am_Katalogsatz()
    {
        var ohne = Aufbauen();
        KatalogZeileWaehlen(ohne, 0);
        Assert.Empty(ohne.FindAll(".epos-modulparameter-knopf"));

        var mit = Aufbauen(katalogfelder: Katalogfelder);
        Assert.Empty(mit.FindAll(".epos-modulparameter-knopf"));   // erste Projektzeile

        KatalogZeileWaehlen(mit, 0);
        Assert.Single(mit.FindAll(".epos-modulparameter-knopf"));
    }

    /// <summary>
    /// <b>Geholt wird mit der WAHL des Satzes</b> (Anwenderentscheid 16.09.2026: „Unter
    /// Bearbeiten sollen alle Parameter angezeigt werden und bearbeitbar sein"). Der
    /// Aufklapper steht offen, sobald ein Katalogsatz gewählt ist — genau EINE Abfrage
    /// je Satz; ohne gewählten Satz gibt es den Block nicht.
    /// </summary>
    [Fact]
    public void Die_Felder_kommen_mit_der_Wahl_des_Satzes()
    {
        int rufe = 0;
        var cut = Aufbauen(katalogfelder: n => { rufe++; return Katalogfelder(n); });

        Assert.Equal(0, rufe);
        Assert.Empty(cut.FindAll(".epos-modulparameter"));

        KatalogZeileWaehlen(cut, 0);

        Assert.Equal(1, rufe);
        Assert.True(cut.Instance.ParameterOffen);
        Assert.Equal("true", cut.Find(".epos-modulparameter-knopf").GetAttribute("aria-expanded"));
        Assert.Equal(3, cut.Find(".epos-modulparameter").QuerySelectorAll(".epos-feld").Length);
    }

    /// <summary>
    /// <b>Zuklappen geht weiterhin</b> — der Knopf bleibt, was er war, nur seine Vorgabe
    /// hat sich gedreht.
    /// </summary>
    [Fact]
    public void Der_Aufklapper_laesst_sich_weiterhin_zuklappen()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder);

        KatalogZeileWaehlen(cut, 0);

        cut.Find(".epos-modulparameter-knopf").Click();

        Assert.False(cut.Instance.ParameterOffen);
        Assert.Equal("false", cut.Find(".epos-modulparameter-knopf").GetAttribute("aria-expanded"));
        Assert.Empty(cut.Find(".epos-modulparameter").QuerySelectorAll(".epos-feld"));

        cut.Find(".epos-modulparameter-knopf").Click();

        Assert.True(cut.Instance.ParameterOffen);
        Assert.NotEmpty(cut.Find(".epos-modulparameter").QuerySelectorAll(".epos-feld"));
    }

    /// <summary>
    /// <b>Die Knopfzeile steht als ERSTES unter dem Modulkopf</b> (Anwenderentscheid
    /// 16.09.2026: „Der Bearbeiten-Button soll weiter oben … stehen, so dass er besser
    /// sichtbar ist"): links die Kostenknöpfe, rechts „Bearbeiten…", dazwischen der
    /// Füller. Danach erst die Felder, danach der Aufklapper.
    /// </summary>
    [Fact]
    public void Die_Knopfzeile_steht_unmittelbar_unter_dem_Modulkopf()
    {
        var cut = Aufbauen(kostenOeffnen: (_, _) => Task.CompletedTask,
                           katalogfelder: Katalogfelder);
        KatalogZeileWaehlen(cut, 0);

        var kinder = cut.FindAll(".epos-gruppenkopf-koerper")[0].Children.ToList();

        Assert.Contains("epos-leiste", kinder[0].ClassList);
        int raster = kinder.FindIndex(k => k.ClassList.Contains("epos-formularraster"));
        int parameter = kinder.FindIndex(k => k.ClassList.Contains("epos-modulparameter"));
        Assert.True(0 < raster && raster < parameter);

        var teile = kinder[0].Children.ToList();
        Assert.Contains("epos-kostenleiste", teile[0].ClassList);
        Assert.Contains("epos-leiste-fueller", teile[1].ClassList);
        Assert.Equal("Bearbeiten...", teile[2].TextContent.Trim());
    }

    /// <summary>
    /// „Speichern" reicht die GEÄNDERTEN Felder an den Schreibweg — und ist vorher
    /// gesperrt: Ohne Änderung gibt es nichts zu schreiben.
    /// </summary>
    [Fact]
    public void Speichern_reicht_die_geaenderten_Felder_an_den_Schreibweg()
    {
        IReadOnlyList<BrowserFeldwert>? gesehen = null;
        string? name = null;
        var cut = Aufbauen(katalogfelder: Katalogfelder,
                           felderSpeichern: (n, f) =>
                           {
                               name = n; gesehen = f;
                               return new KatalogSpeicherErgebnis(true, "Datensatz gespeichert", n);
                           });

        KatalogZeileWaehlen(cut, 0);

        var speichern = Knopf(cut, "Speichern");
        Assert.True(speichern.HasAttribute("disabled"));

        cut.Find(".epos-modulparameter").QuerySelectorAll("input[inputmode=decimal]")[0].Input("900");
        speichern = Knopf(cut, "Speichern");
        Assert.False(speichern.HasAttribute("disabled"));
        speichern.Click();

        Assert.Equal("Vitosol 200", name);
        Assert.NotNull(gesehen);
        Assert.Equal("900",
            gesehen!.First(f => f.Schluessel == KatalogBrowserProfil.FeldInvestitionskosten).Wert);
        Assert.Contains("gespeichert", cut.Find(".epos-warnbanner").TextContent);
    }

    /// <summary>
    /// Ohne Schreibweg ist der Aufklapper reine ANZEIGE: kein Speichern-Knopf, kein
    /// beschreibbares Feld. Das ist die Lage des Katalogs, keine Entscheidung des
    /// Dialogs — er liest sie am Delegaten ab.
    /// </summary>
    [Fact]
    public void Ohne_Schreibweg_zeigt_der_Aufklapper_nur_an()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder);

        KatalogZeileWaehlen(cut, 0);

        var block = cut.Find(".epos-modulparameter");
        Assert.DoesNotContain(block.QuerySelectorAll("button").Select(b => b.TextContent.Trim()),
                              t => t == "Speichern");
        Assert.Empty(block.QuerySelectorAll("input:not([readonly])"));
    }

    /// <summary>
    /// Eine Fehleingabe in einem Zahlenfeld sperrt „Speichern" — der Dialog schriebe
    /// sonst den Stand VOR der angefangenen Zahl zurück.
    /// </summary>
    [Fact]
    public void Eine_Fehleingabe_sperrt_den_Speichern_Knopf()
    {
        bool geschrieben = false;
        var cut = Aufbauen(katalogfelder: Katalogfelder,
                           felderSpeichern: (n, _) =>
                           {
                               geschrieben = true;
                               return new KatalogSpeicherErgebnis(true, "ok", n);
                           });

        KatalogZeileWaehlen(cut, 0);

        // Erst eine gueltige Aenderung - sie gibt den Knopf frei.
        cut.Find(".epos-modulparameter").QuerySelectorAll("input[inputmode=decimal]")[0].Input("900");
        Assert.False(Knopf(cut, "Speichern").HasAttribute("disabled"));

        // Dann die Fehleingabe: der Knopf geht wieder zu.
        cut.Find(".epos-modulparameter").QuerySelectorAll("input[inputmode=decimal]")[0].Input("neunhundert");

        Assert.True(Knopf(cut, "Speichern").HasAttribute("disabled"));
        Assert.False(geschrieben);
    }

    /// <summary>
    /// Ein abgelehnter Schreibweg (Schreibschutz der Auslieferung) meldet den Grund und
    /// lässt den geänderten Stand stehen — der Anwender soll ihn verbessern können.
    /// </summary>
    [Fact]
    public void Eine_Ablehnung_meldet_den_Grund()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder,
                           felderSpeichern: (_, __) =>
                               new KatalogSpeicherErgebnis(false, "Schreibgeschützt.", ""));

        KatalogZeileWaehlen(cut, 0);
        cut.Find(".epos-modulparameter").QuerySelectorAll("input[inputmode=decimal]")[0].Input("900");
        Knopf(cut, "Speichern").Click();

        Assert.Contains("Schreibgeschützt.", cut.Find(".epos-warnbanner").TextContent);
        Assert.True(cut.Instance.ParameterOffen);
    }

    // =================================================================================
    // Die Kostenknöpfe im Modulbereich
    // =================================================================================

    /// <summary>
    /// <b>Zwei Knöpfe, kein dritter</b> (Anwenderentscheid 15.09.2026): Investitions-
    /// und Betriebskosten führen in DIESELBE Maske und unterscheiden sich nur im
    /// Schalter; „Energiekosten…" gibt es nicht — die Sonne ist kein gekaufter Träger.
    /// Ohne Delegat fehlt die Leiste ganz, im Assistenten ebenso.
    /// </summary>
    [Fact]
    public void Die_Kostenleiste_steht_nur_mit_Weg_und_traegt_zwei_Knoepfe()
    {
        Assert.Empty(Aufbauen().FindAll(".epos-kostenleiste button"));

        var gerufen = new List<bool>();
        Func<ErzeugerZeile?, bool, Task> weg = (_, betrieb) =>
        {
            gerufen.Add(betrieb);
            return Task.CompletedTask;
        };

        Assert.Empty(Aufbauen(kostenOeffnen: weg, wizard: true).FindAll(".epos-kostenleiste button"));

        var cut = Aufbauen(kostenOeffnen: weg);
        Assert.Equal(2, cut.FindAll(".epos-kostenleiste button").Count);

        cut.FindAll(".epos-kostenleiste button")[0].Click();
        cut.FindAll(".epos-kostenleiste button")[1].Click();
        Assert.Equal(new[] { false, true }, gerufen);
    }

    /// <summary>Die Kostenleiste bekommt die GEWÄHLTE Projektzeile mit.</summary>
    [Fact]
    public void Die_Kostenleiste_reicht_die_gewaehlte_Projektzeile_durch()
    {
        ErzeugerZeile? gesehen = null;
        var cut = Aufbauen(kostenOeffnen: (zeile, _) =>
        {
            gesehen = zeile;
            return Task.CompletedTask;
        });

        cut.FindAll(".epos-kostenleiste button")[0].Click();

        Assert.NotNull(gesehen);
        Assert.Equal(1, gesehen!.Schluessel);
    }

    /// <summary>Wählt die Katalogzeile mit dieser Nummer in der rechten Liste.</summary>
    private static void KatalogZeileWaehlen(IRenderedComponent<SolarkollektorenDialog> cut, int nr)
        => cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr button")[nr].Click();

    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F1)
    // =====================================================================

    /// <summary>
    /// Nach dem Zeichnen steht die Maske an der <c>KiMaskenbruecke</c>; die Brücke
    /// liest die Neigung aus dem ARBEITSSTAND der Kollektorgruppe und setzt sie — und
    /// der neue Wert steht danach im Eingabefeld, nicht erst in der Zeile.
    /// </summary>
    /// <remarks>
    /// <b>Der Arbeitsstand ist das Daten-Objekt, und das ist der Punkt.</b> Die Zeile
    /// bekommt die fünf Zahlen erst mit „Übernehmen"; eine Setzung in die Zeile bliebe
    /// auf der Maske unsichtbar und würde vom nächsten „Übernehmen" überschrieben.
    /// </remarks>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_setzt_die_Neigung()
    {
        ErzeugerZeile zeile = Zeile(1, "Vitosol 200");
        var cut = Aufbauen(zeilen: new List<ErzeugerZeile> { zeile });

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.SOLARKOLLEKTOREN_PROJEKT));

        KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.SOLARKOLLEKTOREN_PROJEKT, "neigung");
        Assert.NotNull(zugang);
        Assert.Equal(30, zugang.Lesen());

        Assert.True(zugang.Setzbar);
        zugang.Setzen(35);

        // Der Arbeitsstand trägt den neuen Wert - die Zeile noch nicht.
        Assert.Equal(35, zugang.Lesen());
        Assert.Equal(30, zeile.Neigung);

        // „Übernehmen" trägt ihn hinüber - derselbe Weg, den der Speicherhaken geht.
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Übernehmen").Click();
        Assert.Equal(35, zeile.Neigung);
    }

    // =================================================================================
    // Senken der Anlage (Anwenderentscheid 23.09.2026)
    // =================================================================================

    /// <summary>
    /// Die PROJEKTzeile zeigt ihre Senken in der Gruppe „Kollektor" — bei der
    /// Solarthermie entscheidet die Senke, ob das Kollektorfeld überhaupt einen Abnehmer
    /// findet.
    /// </summary>
    [Fact]
    public void Die_Projektzeile_zeigt_ihre_Senken()
    {
        ErzeugerZeile zeile = Zeile(1, "Vitosol 200");
        zeile.Senken = "Senken: Heizkreis (Heizung + Warmwasser); Prozesswärme";
        var cut = Aufbauen(new List<ErzeugerZeile> { zeile });

        cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr")[0]
           .QuerySelector("button")!.Click();

        Assert.Contains(cut.FindAll(".epos-formularraster .epos-herleitung-text"),
                        e => e.TextContent == zeile.Senken);
    }

    /// <summary>Ohne Senkentext steht keine Zeile.</summary>
    [Fact]
    public void Ohne_Senkentext_steht_keine_Senkenzeile()
    {
        var cut = Aufbauen();

        cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr")[0]
           .QuerySelector("button")!.Click();

        Assert.DoesNotContain(cut.FindAll(".epos-herleitung-text"),
                              e => e.TextContent.StartsWith("Senken", StringComparison.Ordinal));
    }
}
