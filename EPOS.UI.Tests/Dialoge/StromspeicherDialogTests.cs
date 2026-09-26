using System.Globalization;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Allgemein;
using EPOS.UI.Dialoge.Erzeuger;
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
/// Verwaltung Stromspeicher (iU9-W6.6). Soll ist die Feldkarte von
/// <c>Form_Stromspeicher</c>: zwei Listen (die rechte mit zwei Spalten), die
/// beiden Pfeile und der reine Anzeigeblock mit sieben Feldern — davon zwei mit
/// Beschriftungen aus dem Ressourcenkatalog statt aus dem Designer.
/// </summary>
public class StromspeicherDialogTests : EposBunitContext
{

    /// <summary>
    /// Der Filterstand DIESES Prüfstands. Ohne ihn nähme der Dialog den aus dem
    /// <c>Katalogfilterregister</c> — der lebt prozessweit, und xunit fährt
    /// Testklassen nebeneinander. Dass das Register wirklich teilt, prüft
    /// <c>KatalogfilterstandTests</c>.
    /// </summary>
    private readonly Katalogfilterstand _filterstand = new();
    /// <summary>
    /// Das PROFIL des Projektdialogs (W14a-E-10 / S2.1): dieselben acht Spalten wie
    /// in der Verwaltung, dazu die neunte „im Projekt verwendet" (Q12).
    /// </summary>
    private static readonly Katalogfilterprofil Profil =
        Katalogfilterprofil.MitVerwendung(Anlagenart.Stromspeicher,
            s => Resource.ResourceManager.GetString(s) ?? s);

    private static IReadOnlyList<Katalogfilterzeile> Katalogzeilen() => new[]
    {
        new Katalogfilterzeile(41, "Speicher 10")
            .MitText(Katalogfilterprofil.SpBezeichner, "Speicher 10")
            .MitText(Katalogfilterprofil.SpHersteller, "BYD")
            .MitText(Katalogfilterprofil.SpChemie, "Lithium-Ionen")
            .MitZahl(Katalogfilterprofil.SpEnergie, 10.0)
            .MitZahl(Katalogfilterprofil.SpLeistung, 10.0)
            .MitZahl(Katalogfilterprofil.SpCrate, 1.0, 2)
            .MitZahl(Katalogfilterprofil.SpEtaRt, 0.9, 3)
            .MitZahl(Katalogfilterprofil.SpZyklen, 6000, 0),

        new Katalogfilterzeile(42, "Speicher 20")
            .MitText(Katalogfilterprofil.SpBezeichner, "Speicher 20")
            .MitText(Katalogfilterprofil.SpHersteller, "VARTA")
            .MitText(Katalogfilterprofil.SpChemie, "Lithium-Ionen")
            .MitZahl(Katalogfilterprofil.SpEnergie, 20.0)
            .MitZahl(Katalogfilterprofil.SpLeistung, 20.0)
            .MitZahl(Katalogfilterprofil.SpCrate, 1.0, 2)
            .MitZahl(Katalogfilterprofil.SpEtaRt, 0.92, 3)
            .MitZahl(Katalogfilterprofil.SpZyklen, 8000, 0),
    };

    public StromspeicherDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static ErzeugerZeile Zeile(int schluessel, string name, int geraetId)
        => new() { Schluessel = schluessel, Bezeichner = name, GeraetId = geraetId };

    private static ErzeugerDetail Detail(string name) => new(
        name, "",
        new[] { ("Typ:", "Lithium"), ("Leistung [kW]:", "10"),
                ("Energie (Kapazität) [kWh]:", "20"), ("Degradation [%/a]:", "0,1"),
                ("Ladezustand [%]:", "50"), ("Modulkosten [€/kWh]:", "400") });

    /// <summary>
    /// Das PROFIL des Modulkatalogs — aus ihm baut die Hülle über die
    /// <c>ModulFeldwertBruecke</c> die Felder des Aufklappers. Der Prüfstand nimmt
    /// DASSELBE Profil, damit die Feldarten hier nicht erfunden werden.
    /// </summary>
    private static readonly ModulKatalogProfil Modulprofil =
        ModulKatalogProfil.Finde(ModulKatalogArt.Stromspeicher,
            s => Resource.ResourceManager.GetString(s) ?? s);

    /// <summary>
    /// Die Felder des Aufklappers „Alle Daten anzeigen" (Anwenderentscheid 15.09.2026)
    /// — abgebildet wie in der Hülle: Der Bezeichner ist gesperrt, alles Übrige ist
    /// editierbar. Erst hier werden die sechs AP3-Gerätewerte im Projektdialog
    /// überhaupt sichtbar.
    /// </summary>
    private static List<BrowserFeldwert> Katalogfelder(string name)
    {
        var liste = new List<BrowserFeldwert>();
        foreach (ModulKatalogFeld f in Modulprofil.Felder)
        {
            string wert = f.Schluessel == ModulKatalogProfil.FeldBezeichner ? name
                        : f.Art == BrowserFeldArt.Zahl ? "12,5"
                        : f.Art == BrowserFeldArt.Ganzzahl ? "70"
                        : "Wert " + f.Schluessel;

            liste.Add(new BrowserFeldwert
            {
                Schluessel = f.Schluessel,
                Bezeichnung = f.Bezeichnung,
                Einheit = f.Einheit,
                Art = f.Art,
                Editierbar = !f.Gesperrt && f.Art != BrowserFeldArt.Auswahl,
                Wert = wert
            });
        }
        return liste;
    }

    private IRenderedComponent<StromspeicherDialog> Aufbauen(
        List<ErzeugerZeile>? zeilen = null,
        Func<int, AufnahmeErgebnis>? aufnehmen = null,
        Action<ErzeugerZeile>? entfernen = null,
        Func<IReadOnlyDictionary<string, object>>? verwaltung = null,
        bool wizard = false,
        Func<string, IReadOnlyList<BrowserFeldwert>?>? katalogfelder = null,
        Func<string, IReadOnlyList<BrowserFeldwert>, KatalogSpeicherErgebnis>? felderSpeichern = null,
        Action<bool>? geschlossen = null)
    {
        return Render<StromspeicherDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<ErzeugerZeile> { Zeile(1, "Speicher 10", 41) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Detail, n => Detail(n))
            .Add(x => x.Aufnehmen, aufnehmen ?? (_ => new AufnahmeErgebnis(Zeile(9, "Speicher 20", 42))))
            .Add(x => x.Entfernen, entfernen)
            .Add(x => x.Katalogfelder, katalogfelder)
            .Add(x => x.KatalogfelderSpeichern, felderSpeichern)
            .Add(x => x.VerwaltungGaben, verwaltung)
            .Add(x => x.Wizard, wizard)
            .Add(x => x.Geschlossen, ok => geschlossen?.Invoke(ok)));
    }

    /// <summary>Die zweite Liste ist die KATALOGliste; ihre erste Zeile ist „Speicher 10".</summary>
    private static void KatalogzeileWaehlen(IRenderedComponent<StromspeicherDialog> cut, int nummer = 0)
        => cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[nummer].Click();

    /// <summary>Der Modulbereich — der einzige Gruppenkopf dieses Dialogs.</summary>
    private static AngleSharp.Dom.IElement Modulbereich(IRenderedComponent<StromspeicherDialog> cut)
        => cut.Find(".epos-gruppenkopf");

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
        Assert.Contains("ausgewählte Stromspeicher:", ueberschriften);
        Assert.Contains("Stromspeicher aus Datenbank:", ueberschriften);

        // Sieben NUR LESBARE Anzeigefelder: Name, Typ, Leistung, Energie, Degradation,
        // Ladezustand, Modulkosten.
        Assert.Equal(7, cut.FindAll(".epos-gruppenkopf-koerper input[readonly]").Count);
    }

    [Fact]
    public void Die_zwei_berichtigten_Beschriftungen_stehen_da()
    {
        // EinheitenBeschriftungKorrigieren: Der Designer trug "Energie [kW]" und
        // "Modulkosten" - beides fachlich falsch (Abnahmebefund 1).
        var cut = Aufbauen();

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.Contains("Energie (Kapazität) [kWh]:", texte);
        Assert.Contains("Modulkosten [€/kWh]:", texte);
        Assert.DoesNotContain("Energie [kW]:", texte);
    }

    /// <summary>
    /// <b>Aus der Sammelspalte werden SPALTEN</b> (W14a-E-10 / S2.1). Der Vorläufer
    /// trug „Name" und einen Mehrzeiler „Eigenschaften"; jetzt steht jeder Wert in
    /// seiner eigenen, sortierbaren Spalte — und der Speicherkatalog hat damit
    /// überhaupt zum ersten Mal einen Filter.
    /// </summary>
    [Fact]
    public void Die_Katalogliste_zeigt_die_Spalten_des_Profils()
    {
        var cut = Aufbauen();

        var kopf = cut.FindAll(".epos-raster")[1].QuerySelectorAll("th")
                      .Select(e => e.TextContent.Trim()).ToList();

        Assert.Equal(Profil.Spalten.Count + 1, kopf.Count);
        Assert.Contains(kopf, k => k.StartsWith("Hersteller"));
        Assert.Contains(kopf, k => k.Contains("kWh"));
        Assert.Empty(cut.FindAll(".epos-mehrzeilig"));

        Assert.Single(cut.FindAll(".epos-katalog-suchzeile"));
    }

    /// <summary>
    /// Ohne Parametersatz der Speicherverwaltung kein Knopf — Hausregel. Seit
    /// iU9-W14a.3 ist die Verwaltung eine ÜBERLAGERUNG im selben Fenster.
    /// </summary>
    [Fact]
    public void Der_Bearbeiten_Knopf_erscheint_nur_mit_Verwaltungsgaben()
    {
        var ohne = Aufbauen();
        Assert.DoesNotContain(ohne.FindAll("button").Select(b => b.TextContent), t => t == "Bearbeiten...");

        var mit = Aufbauen(verwaltung: () => Verwaltungsgaben());
        Assert.Contains(mit.FindAll("button").Select(b => b.TextContent), t => t == "Bearbeiten...");
    }

    [Fact]
    public void Im_Assistenten_fehlt_die_OK_Leiste()
    {
        var cut = Aufbauen(wizard: true);
        Assert.Empty(cut.FindAll(".epos-status"));
    }

    // =================================================================================
    // Aufnehmen und Entfernen
    // =================================================================================

    [Fact]
    public void Je_Klick_entsteht_eine_eigene_Zeile()
    {
        // AP2b: Bis dahin landete immer dasselbe Feldobjekt in der Liste; zweimal
        // derselbe Speicher sind zwei Zeilen.
        var zeilen = new List<ErzeugerZeile>();
        int naechster = 10;
        var cut = Aufbauen(zeilen, aufnehmen: id =>
            new AufnahmeErgebnis(Zeile(naechster++, "Speicher 10", id)));

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-zweispalten-uebernahme button")[0].Click();
        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-zweispalten-uebernahme button")[0].Click();

        Assert.Equal(2, zeilen.Count);
        Assert.NotEqual(zeilen[0].Schluessel, zeilen[1].Schluessel);
    }

    [Fact]
    public void Der_Pfeil_zurueck_trifft_genau_die_gewaehlte_Zeile()
    {
        // A-17: Der Vorlaeufer nahm die ERSTE Zeile gleichen Namens.
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Speicher 10", 41), Zeile(2, "Speicher 10", 41) };
        var entfernt = new List<ErzeugerZeile>();
        var cut = Aufbauen(zeilen, entfernen: z => entfernt.Add(z));

        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[1].Click();
        cut.FindAll(".epos-zweispalten-uebernahme button")[1].Click();

        Assert.Single(zeilen);
        Assert.Equal(1, zeilen[0].Schluessel);
        Assert.Equal(2, entfernt[0].Schluessel);
    }

    [Fact]
    public void Nach_der_letzten_Zeile_wandert_die_Auswahl_in_den_Katalog()
    {
        // btn_Entfernen_Click baute dafuer einen Mausklick auf die erste Rasterzeile nach.
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Speicher 10", 41) };
        var cut = Aufbauen(zeilen);

        cut.FindAll(".epos-zweispalten-uebernahme button")[1].Click();

        Assert.Empty(zeilen);
        Assert.Null(cut.Instance.Projektzeile);
        Assert.Equal(41, cut.Instance.Katalogzeile!.Id);
    }

    // =================================================================================
    // Detail und Tastatur
    // =================================================================================

    [Fact]
    public void Beide_Listen_holen_ihr_Detail_aus_demselben_Katalogsatz()
    {
        // listBox_SP_SelectedIndexChanged und dataGridView1_Click sind zeichengleich -
        // es gibt keine Projektkopie, die abweichen koennte.
        var gefragt = new List<string>();
        var cut = Render<StromspeicherDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Speicher 10", 41) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Detail, n => { gefragt.Add(n); return Detail(n); }));

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[1].Click();

        Assert.Equal(new[] { "Speicher 10", "Speicher 20" }, gefragt);
    }

    /// <summary>
    /// „Bearbeiten" öffnet den Modulkatalog als ÜBERLAGERUNG im selben Fenster — bis
    /// iU9-W14a war es ein Sprung in ein zweites Fenster
    /// (<c>Sprungziel.StromspeicherAdmin</c>).
    /// </summary>
    [Fact]
    public void Bearbeiten_oeffnet_die_Speicherverwaltung_als_Ueberlagerung()
    {
        var cut = Aufbauen(verwaltung: () => Verwaltungsgaben());

        Assert.False(cut.Instance.VerwaltungOffen);

        // Der Knopf steht seit dem 15.09.2026 im MODULBEREICH, nicht mehr unter der
        // Katalogliste.
        Modulbereich(cut).QuerySelectorAll(".epos-leiste button")
                         .First(b => b.TextContent == "Bearbeiten...").Click();

        Assert.True(cut.Instance.VerwaltungOffen);
        Assert.NotEmpty(cut.FindAll(".epos-ueberlagerung"));
    }

    /// <summary>
    /// <b>Die Überlagerung steht am DIALOGENDE, nicht in der Kopfzeile.</b> Bis zum
    /// 15.09.2026 stand ihr Markup innerhalb von <c>.epos-dialog-kopf</c> — ein
    /// Ausrutscher, den nur die Kaskade verdeckte. Der Kopf trägt Titel, Hilfeknopf
    /// und ✕, sonst nichts.
    /// </summary>
    [Fact]
    public void Die_Ueberlagerung_haengt_nicht_in_der_Kopfzeile()
    {
        var cut = Aufbauen(verwaltung: () => Verwaltungsgaben());

        Assert.Empty(cut.Find(".epos-dialog-kopf").QuerySelectorAll(".epos-ueberlagerung"));

        Modulbereich(cut).QuerySelectorAll(".epos-leiste button")
                         .First(b => b.TextContent == "Bearbeiten...").Click();

        Assert.NotEmpty(cut.FindAll(".epos-ueberlagerung"));
        Assert.Empty(cut.Find(".epos-dialog-kopf").QuerySelectorAll(".epos-ueberlagerung"));
    }

    /// <summary>
    /// <b>Die Knöpfe zum gewählten Satz stehen im MODULBEREICH</b> (Anwenderentscheid
    /// 15.09.2026): „Bearbeiten…" ist dorthin gewandert, wo der Satz steht. Unter der
    /// Katalogliste steht kein Knopf mehr — einen Löschknopf hat dieser Dialog nie
    /// gehabt.
    /// </summary>
    [Fact]
    public void Bearbeiten_steht_im_Modulbereich_und_nicht_mehr_bei_der_Liste()
    {
        var cut = Aufbauen(verwaltung: () => Verwaltungsgaben());
        KatalogzeileWaehlen(cut);

        Assert.Empty(cut.FindAll(".epos-zweispalten-spalte")[1]
                        .QuerySelectorAll(".epos-leiste button"));

        Assert.Contains(Modulbereich(cut).QuerySelectorAll(".epos-leiste button")
                                         .Select(b => b.TextContent),
                        t => t == "Bearbeiten...");
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
        var cut = Aufbauen(verwaltung: () => Verwaltungsgaben(),
                           katalogfelder: Katalogfelder);
        KatalogzeileWaehlen(cut);

        var kinder = Modulbereich(cut).QuerySelector(".epos-gruppenkopf-koerper")!
                                      .Children.ToList();

        Assert.Contains("epos-leiste", kinder[0].ClassList);
        int raster = kinder.FindIndex(k => k.ClassList.Contains("epos-formularraster"));
        int parameter = kinder.FindIndex(k => k.ClassList.Contains("epos-modulparameter"));
        Assert.True(0 < raster && raster < parameter);

        var teile = kinder[0].Children.ToList();
        Assert.Contains("epos-kostenleiste", teile[0].ClassList);
        Assert.Contains("epos-leiste-fueller", teile[1].ClassList);
        Assert.Equal("Bearbeiten...", teile[2].TextContent.Trim());
    }

    /// <summary>Ein Mindestsatz für die Überlagerung — der Katalog braucht sein Profil.</summary>
    private static IReadOnlyDictionary<string, object> Verwaltungsgaben()
        => new Dictionary<string, object>
        {
            ["Art"] = WindowsFormsApplication1.ModulKatalogArt.Stromspeicher,
            ["Wege"] = new EPOS.UI.Dialoge.Erzeuger.ModulKatalogWege()
        };

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

    /// <summary>
    /// <b>„Das Kreuz steht beim Titel"</b> (Anwenderentscheid 15.09.2026): Das ✕ der
    /// Kopfzeile wirkt genau wie Esc — es schließt ohne zu speichern und meldet
    /// <c>false</c>.
    /// </summary>
    [Fact]
    public void Das_Kreuz_im_Kopf_bricht_ab_wie_Esc()
    {
        int rufe = 0;
        bool? gemeldet = null;
        var cut = Aufbauen(geschlossen: ok => { gemeldet = ok; rufe++; });

        cut.Find(".epos-dialog-zu").Click();

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

    // =================================================================================
    //  Stufe S2.1 / S2.3 / S2.5 - Anwenderentscheid W14a-E-10 vom 07.09.2026
    // =================================================================================

    /// <summary>Die Katalogliste - das UNTERE der beiden Raster.</summary>
    private static IReadOnlyList<AngleSharp.Dom.IElement> Katalogzeilen(
        IRenderedComponent<StromspeicherDialog> cut)
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

        _filterstand.Setzen(Katalogfilterprofil.SpHersteller, "VARTA");
        cut.Render();

        Assert.Single(Katalogzeilen(cut));
        Assert.Equal("1 von 2 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);
        Assert.Contains("Speicher 20", Katalogzeilen(cut)[0].TextContent);

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

        _filterstand.Sortieren(Katalogfilterprofil.SpEnergie);
        cut.Render();
        Assert.Contains("Speicher 10", Katalogzeilen(cut)[0].TextContent);

        _filterstand.Sortieren(Katalogfilterprofil.SpEnergie);
        cut.Render();
        Assert.False(_filterstand.Aufsteigend);
        Assert.Contains("Speicher 20", Katalogzeilen(cut)[0].TextContent);

        _filterstand.Sortieren(Katalogfilterprofil.SpEnergie);
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
        _filterstand.Setzen(Katalogfilterprofil.SpHersteller, "VARTA");
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
        var cut = Aufbauen(zeilen: new List<ErzeugerZeile> { Zeile(1, "Speicher 10", 41) });

        var kopf = cut.FindAll(".epos-raster")[1]
                      .QuerySelectorAll("th").Select(e => e.TextContent.Trim()).ToList();
        Assert.Contains(kopf, k => k.StartsWith("im Projekt verwendet"));

        var zeilen = Katalogzeilen(cut);
        Assert.Equal(2, zeilen.Count);
        Assert.Equal(1, zeilen.Count(z => z.QuerySelectorAll("td").Last().TextContent.Trim() == "Ja"));

        var traegt = zeilen.First(z => z.QuerySelectorAll("td").Last().TextContent.Trim() == "Ja");
        Assert.Contains("Speicher 10", traegt.TextContent);
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
        _filterstand.Setzen(Katalogfilterprofil.SpHersteller, "VARTA");
        ersterAufbau.Render();
        Assert.Single(Katalogzeilen(ersterAufbau));

        // Der Dialog geht zu und wieder auf - derselbe Stand, dieselbe Sicht.
        var zweiterAufbau = Aufbauen();

        Assert.Single(Katalogzeilen(zweiterAufbau));
        Assert.Equal("1 von 2 Sätzen", zweiterAufbau.Find(".epos-katalog-treffer").TextContent);
        Assert.Single(zweiterAufbau.FindAll(".epos-katalog-ruecksetzer"));
    }

    // =================================================================================
    // ET-5 (Anwenderentscheid 08.09.2026): der Energietraeger des Speichers, Gruppe > Art
    // =================================================================================

    [Fact]
    public void Die_markierte_Anlage_zeigt_ihren_Energietraeger_und_meldet_den_Wechsel()
    {
        (ErzeugerZeile Zeile, int Neu)? gemeldet = null;
        ErzeugerZeile zeile = Zeile(1, "Speicher 10", 41);
        zeile.CarrierId = 60;
        var cut = Render<StromspeicherDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { zeile })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Detail, n => Detail(n))
            .Add(x => x.Traegerkatalog, new[]
            {
                new EnergietraegerWahl.Eintrag(11, "Gas", "Erdgas E"),
                new EnergietraegerWahl.Eintrag(60, "Strom", "Elektrische Energie"),
                new EnergietraegerWahl.Eintrag(58, "Strom", "Elektrische Energie 2")
            })
            .Add(x => x.TraegerWechseln, (ErzeugerZeile z, int neu) => gemeldet = (z, neu)));

        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[0].Click();

        var selects = cut.Find(".epos-traegerwahl").QuerySelectorAll("select");
        Assert.Equal(2, selects.Length);
        Assert.Contains("Elektrische Energie 2", selects[1].InnerHtml);

        selects[1].Change("58");

        Assert.Equal(58, gemeldet?.Neu);
        Assert.Equal(58, zeile.CarrierId);
    }

    // =====================================================================
    //  Der Aufklapper „Alle Daten anzeigen" — Anwenderentscheid 15.09.2026
    // =====================================================================
    //  Der Detailblock darüber zeigt sechs der vierzehn Katalogspalten — die
    //  sechs des Vorläufers. Die übrigen acht, darunter die AP3-Gerätewerte,
    //  waren aus diesem Dialog heraus gar nicht zu sehen; jetzt stehen sie im
    //  Aufklapper, in der Bauart aller sechs Erzeuger und bearbeitbar.

    /// <summary>
    /// <b>Der Aufklapper gehört zum KATALOGSATZ.</b> Beim Öffnen ist die Projektzeile
    /// markiert — dann gibt es ihn nicht; er erscheint mit der Wahl in der
    /// Katalogliste.
    /// </summary>
    [Fact]
    public void Ohne_gewaehlten_Katalogsatz_gibt_es_keinen_Aufklapper()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder);

        Assert.Empty(cut.FindAll(".epos-modulparameter-knopf"));

        KatalogzeileWaehlen(cut);
        Assert.Single(cut.FindAll(".epos-modulparameter-knopf"));
    }

    /// <summary>
    /// Ohne Weg zu den Feldern kein Aufklapper — Hausregel „kein Delegat, kein Knopf".
    /// </summary>
    [Fact]
    public void Ohne_Katalogfelder_gibt_es_keinen_Aufklapper()
    {
        var cut = Aufbauen();
        KatalogzeileWaehlen(cut);

        Assert.Empty(cut.FindAll(".epos-modulparameter-knopf"));
    }

    /// <summary>
    /// AUFGEKLAPPT IST DIE VORGABE (Anwenderentscheid 16.09.2026: „Unter Bearbeiten
    /// sollen alle Parameter angezeigt werden und bearbeitbar sein"), und der Weg zu
    /// den Feldern wird mit der WAHL gegangen — einmal je Satz, nicht je Klick. Ohne
    /// gewählten Satz gibt es den Block nicht, und dann wird auch nichts abgefragt.
    /// </summary>
    [Fact]
    public void Der_Parameterblock_ist_aufgeklappt_die_Vorgabe()
    {
        int rufe = 0;
        var cut = Aufbauen(katalogfelder: n => { rufe++; return Katalogfelder(n); });

        Assert.Equal(0, rufe);
        Assert.Empty(cut.FindAll(".epos-modulparameter"));

        KatalogzeileWaehlen(cut);

        var knopf = cut.Find(".epos-modulparameter-knopf");
        Assert.Equal("true", knopf.GetAttribute("aria-expanded"));
        Assert.Contains("Alle Daten anzeigen", knopf.TextContent);

        Assert.True(cut.Instance.ParameterOffen);
        Assert.Equal(1, rufe);

        var block = cut.Find(".epos-modulparameter");
        Assert.Equal(Modulprofil.Felder.Count, block.QuerySelectorAll(".epos-feld").Length);
    }

    /// <summary>
    /// Der Knopf klappt zu und wieder auf — der Zustand gehört dem Dialog.
    /// </summary>
    [Fact]
    public void Der_Knopf_klappt_zu_und_wieder_auf()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder);
        KatalogzeileWaehlen(cut);

        cut.Find(".epos-modulparameter-knopf").Click();
        Assert.False(cut.Instance.ParameterOffen);
        Assert.Equal("false", cut.Find(".epos-modulparameter-knopf").GetAttribute("aria-expanded"));
        Assert.Empty(cut.Find(".epos-modulparameter").QuerySelectorAll(".epos-feld"));

        cut.Find(".epos-modulparameter-knopf").Click();
        Assert.True(cut.Instance.ParameterOffen);
        Assert.NotEmpty(cut.Find(".epos-modulparameter").QuerySelectorAll(".epos-feld"));
    }

    /// <summary>
    /// <b>Der Block gehört zum GEWÄHLTEN Satz.</b> Wer in der Katalogliste einen
    /// anderen Speicher wählt, sieht dessen Werte — und der Aufklappzustand bleibt
    /// stehen, sonst müsste man ihn beim Vergleichen zweier Speicher jedes Mal neu
    /// aufziehen.
    /// </summary>
    [Fact]
    public void Ein_Satzwechsel_zieht_den_Block_nach()
    {
        int rufe = 0;
        var cut = Aufbauen(katalogfelder: n => { rufe++; return Katalogfelder(n); });
        KatalogzeileWaehlen(cut);

        Assert.Equal(1, rufe);
        Assert.Equal("Speicher 10", cut.Find(".epos-modulparameter")
                                       .QuerySelectorAll("input")[0].GetAttribute("value"));

        // Zweite Zeile der KATALOGliste: "Speicher 20".
        KatalogzeileWaehlen(cut, 1);

        Assert.True(cut.Instance.ParameterOffen);
        Assert.Equal(2, rufe);
        Assert.Equal("Speicher 20", cut.Find(".epos-modulparameter")
                                       .QuerySelectorAll("input")[0].GetAttribute("value"));
    }

    /// <summary>
    /// <b>Der Bezeichner bleibt Lesewert</b> — er ist der Schlüssel des <c>UPDATE</c>
    /// und wird im Aufklapper nicht umbenannt. Die Zahl der gesperrten Felder liest
    /// der Prüfstand aus DEMSELBEN Profil, aus dem die Hülle sie ableitet.
    /// </summary>
    [Fact]
    public void Der_Bezeichner_bleibt_Lesewert()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder,
                           felderSpeichern: (n, _) => new KatalogSpeicherErgebnis(true, "", n));
        KatalogzeileWaehlen(cut);

        int erwartet = Modulprofil.Felder
            .Count(f => f.Gesperrt || f.Art == BrowserFeldArt.Auswahl);

        Assert.True(erwartet > 0, "Das Profil führt kein gesperrtes Feld.");
        Assert.Equal(erwartet,
            cut.Find(".epos-modulparameter").QuerySelectorAll("input[readonly]").Length);
    }

    /// <summary>
    /// <b>Ohne Speicherweg ist der Aufklapper reine Anzeige</b> — kein Knopf, jedes
    /// Feld nur lesbar.
    /// </summary>
    [Fact]
    public void Ohne_Speicherweg_ist_der_Aufklapper_nur_Anzeige()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder);
        KatalogzeileWaehlen(cut);

        Assert.Empty(cut.FindAll(".epos-modulparameter .epos-leiste"));

        var eingaben = cut.FindAll(".epos-modulparameter input");
        Assert.NotEmpty(eingaben);
        Assert.All(eingaben, e => Assert.True(e.HasAttribute("readonly")));
    }

    /// <summary>
    /// <b>Speichern reicht die GEÄNDERTEN Felder hinaus</b> — und ist ohne Änderung
    /// gesperrt. Was daraus wird, entscheidet die Hülle; die Komponente kennt weder
    /// Tabelle noch Spalte.
    /// </summary>
    [Fact]
    public void Speichern_reicht_die_geaenderten_Felder_hinaus()
    {
        string? name = null;
        IReadOnlyList<BrowserFeldwert>? gesehen = null;
        var cut = Aufbauen(
            katalogfelder: Katalogfelder,
            felderSpeichern: (n, f) =>
            {
                name = n;
                gesehen = f;
                return new KatalogSpeicherErgebnis(true, "", n);
            });

        KatalogzeileWaehlen(cut);

        Assert.Equal("true", cut.Find(".epos-modulparameter .epos-leiste .epos-knopf")
                                .GetAttribute("aria-disabled"));

        cut.FindAll(".epos-modulparameter input[inputmode=decimal]")[0].Input("42");

        var speichern = cut.Find(".epos-modulparameter .epos-leiste .epos-knopf");
        Assert.False(speichern.HasAttribute("disabled"));
        speichern.Click();

        Assert.Equal("Speicher 10", name);
        Assert.NotNull(gesehen);
        Assert.Contains(gesehen!, f => f.Wert == "42");
    }

    /// <summary>
    /// <b>Der Vermerk am Knopf:</b> „Gespeichert um …" steht neben „Speichern", nicht als
    /// Band; ohne Änderung ist der Knopf weich gesperrt, ein Klick nennt den Grund, und
    /// die nächste Eingabe nimmt den Vermerk zurück.
    /// </summary>
    [Fact]
    public void Speichern_meldet_am_Knopf_und_die_Eingabe_nimmt_den_Vermerk_zurueck()
    {
        int schreibvorgaenge = 0;
        var cut = Aufbauen(katalogfelder: Katalogfelder,
                           felderSpeichern: (n, _) =>
                           {
                               schreibvorgaenge++;
                               return new KatalogSpeicherErgebnis(true, "Datensatz gespeichert", n);
                           });
        KatalogzeileWaehlen(cut);

        cut.FindAll(".epos-modulparameter input[inputmode=decimal]")[0].Input("42");
        cut.Find(".epos-modulparameter .epos-speichervermerk button").Click();

        Assert.Equal(1, schreibvorgaenge);
        Assert.Equal("", cut.Instance.Meldung);
        Assert.StartsWith("Gespeichert um ", cut.Find(".epos-modulparameter .epos-speichervermerk [role=status]").TextContent);
        Assert.Equal("true", cut.Find(".epos-modulparameter .epos-speichervermerk button")
                                .GetAttribute("aria-disabled"));

        // Ohne Aenderung schreibt ein Klick nicht, er nennt den Grund am Knopf.
        cut.Find(".epos-modulparameter .epos-speichervermerk button").Click();
        Assert.Equal(1, schreibvorgaenge);
        Assert.Equal("Keine Änderung — es gibt nichts zu speichern.",
                     cut.Find(".epos-modulparameter .epos-speichervermerk [role=status]").TextContent);

        cut.FindAll(".epos-modulparameter input[inputmode=decimal]")[0].Input("43");
        Assert.Empty(cut.FindAll(".epos-modulparameter .epos-speichervermerk [role=status]"));
        Assert.False(cut.Find(".epos-modulparameter .epos-speichervermerk button")
                        .HasAttribute("aria-disabled"));
    }

    /// <summary>
    /// <b>Eine ungültige Zahl sperrt das Speichern.</b> Das Zahlenfeld färbt sich und
    /// meldet seinen Zustand nach oben.
    /// </summary>
    [Fact]
    public void Ein_Fehlerzustand_sperrt_das_Speichern()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder,
                           felderSpeichern: (n, _) => new KatalogSpeicherErgebnis(true, "", n));
        KatalogzeileWaehlen(cut);

        cut.FindAll(".epos-modulparameter input[inputmode=decimal]")[0].Input("42");
        Assert.False(cut.Find(".epos-modulparameter .epos-leiste .epos-knopf")
                        .HasAttribute("disabled"));

        cut.FindAll(".epos-modulparameter input[inputmode=decimal]")[0].Input("keine Zahl");
        Assert.True(cut.Find(".epos-modulparameter .epos-leiste .epos-knopf")
                       .HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>Der Befund des Katalogs zählt.</b> Lehnt der Speicherweg ab, meldet der
    /// Dialog — und der geänderte Stand bleibt stehen, damit der Anwender ihn
    /// verbessern kann, statt ihn zu verlieren.
    /// </summary>
    [Fact]
    public void Eine_abgelehnte_Speicherung_meldet_und_haelt_den_Stand()
    {
        var cut = Aufbauen(
            katalogfelder: Katalogfelder,
            felderSpeichern: (n, _) => new KatalogSpeicherErgebnis(
                false, "Der Datensatz ist schreibgeschützt.", n));

        KatalogzeileWaehlen(cut);
        cut.FindAll(".epos-modulparameter input[inputmode=decimal]")[0].Input("42");
        cut.Find(".epos-modulparameter .epos-leiste .epos-knopf").Click();

        Assert.Equal("Der Datensatz ist schreibgeschützt.", cut.Instance.Meldung);
        Assert.Equal("Der Datensatz ist schreibgeschützt.", cut.Find(".epos-modulparameter .epos-speichervermerk [role=status]").TextContent);
        Assert.Contains("epos-status--fehler", cut.Find(".epos-modulparameter .epos-speichervermerk [role=status]").ClassName);
        Assert.Single(cut.FindAll(".epos-warnbanner"));

        // Der Knopf bleibt frei: Die Aenderung steht noch im Aufklapper.
        Assert.False(cut.Find(".epos-modulparameter .epos-leiste .epos-knopf")
                        .HasAttribute("disabled"));
    }

    // =====================================================================
    //  Die Kostenleiste im Modulbereich — Anwenderentscheid 15.09.2026
    // =====================================================================

    /// <summary>
    /// <b>Die Kostenleiste steht ÜBER „Bearbeiten…"</b> und im Assistenten gar nicht —
    /// dieselbe Anordnung wie beim Heizkessel. Ohne Delegat zeichnet die Leiste keinen
    /// Knopf (ihre eigene Regel).
    /// </summary>
    [Fact]
    public void Die_Kostenleiste_steht_im_Modulbereich_und_fehlt_im_Assistenten()
    {
        var cut = Aufbauen();
        Assert.Single(Modulbereich(cut).QuerySelectorAll(".epos-kostenleiste"));
        Assert.Empty(cut.FindAll(".epos-kostenleiste button"));

        var wizard = Aufbauen(wizard: true);
        Assert.Empty(wizard.FindAll(".epos-kostenleiste"));
    }

    /// <summary>
    /// Mit Delegaten stehen die drei Knöpfe da und reichen die GEWÄHLTE Zeile durch —
    /// beim Katalogsatz ist das <c>null</c>, dann zeigt die Verwaltung die Komponente
    /// ohne Einengung auf eine Anlage.
    /// </summary>
    [Fact]
    public void Die_Kostenknoepfe_reichen_die_gewaehlte_Zeile_durch()
    {
        var kosten = new List<bool>();
        int energie = 0;
        ErzeugerZeile? gesehen = null;

        var cut = Render<StromspeicherDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Speicher 10", 41) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Detail, n => Detail(n))
            .Add(x => x.KostenOeffnen, (Func<ErzeugerZeile?, bool, Task>)((zeile, betrieb) =>
            {
                gesehen = zeile;
                kosten.Add(betrieb);
                return Task.CompletedTask;
            }))
            .Add(x => x.EnergiekostenOeffnen, (Func<ErzeugerZeile?, Task>)(_ =>
            {
                energie++;
                return Task.CompletedTask;
            })));

        var knoepfe = cut.FindAll(".epos-kostenleiste button");
        Assert.Equal(3, knoepfe.Count);

        knoepfe[0].Click();
        Assert.Equal(new[] { false }, kosten);
        Assert.NotNull(gesehen);                       // beim Oeffnen ist die Projektzeile gewaehlt

        cut.FindAll(".epos-kostenleiste button")[1].Click();
        Assert.Equal(new[] { false, true }, kosten);

        cut.FindAll(".epos-kostenleiste button")[2].Click();
        Assert.Equal(1, energie);
    }

    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F1)
    // =====================================================================

    /// <summary>
    /// Nach dem Zeichnen steht die Maske an der <c>KiMaskenbruecke</c>, und die Brücke
    /// liest den Namen des gewählten Speichers. SETZEN geht nicht: Das eine Feld der
    /// Maske ist eine Anzeige (<c>nurLesen</c>) — die Betriebsführung der Speicher
    /// steht in der Ansicht „Stromspeicher-Auslegung".
    /// </summary>
    /// <remarks>
    /// <b>Monotone Aussage</b> (Muster <c>KiMaskenhakenTests</c>): Geprüft wird, was
    /// nach dem Zeichnen DA ist. Die Brücke ist prozessweiter Zustand.
    /// </remarks>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_gibt_den_Namen_heraus()
    {
        Aufbauen(zeilen: new List<ErzeugerZeile> { Zeile(1, "Speicher 10", 41) });

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.STROMSPEICHER_PROJEKT));

        KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.STROMSPEICHER_PROJEKT, "anlage");
        Assert.NotNull(zugang);
        Assert.Equal("Speicher 10", zugang.Lesen());
        Assert.False(zugang.Setzbar);
    }

    /// <summary>
    /// <b>Speichern ist der Knopf des Aufklappers „Alle Daten"</b> (Welle #458,
    /// Stufe 2) — trägt er keine Änderung, lehnt <c>dialog_speichern</c> benannt ab:
    /// Die Liste gibt die Maske erst beim OK an die Hülle.
    /// </summary>
    [Fact]
    public async Task Ohne_Aenderung_im_Aufklapper_lehnt_Speichern_benannt_ab()
    {
        Aufbauen();

        KiMaskenhaken haken = KiMaskenbruecke.Haken(KiMaskennamen.STROMSPEICHER_PROJEKT);

        Assert.NotNull(haken.Auffrischen);
        Assert.NotNull(haken.Schreibgeschuetzt);
        Assert.NotNull(haken.Speichern);

        KiKern.KiErgebnis ergebnis = await haken.Speichern!();
        Assert.False(ergebnis.Erfolg);
        Assert.Contains("Alle Daten", ergebnis.Text, StringComparison.Ordinal);
    }
}
