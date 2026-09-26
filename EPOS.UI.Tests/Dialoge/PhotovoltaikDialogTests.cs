using System.Globalization;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Allgemein;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using KiKern;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Verwaltung Photovoltaik Module (iU9-W6.5). Soll ist die Feldkarte von
/// <c>Form_PV</c> — mit der Berichtigung aus R‑W6‑7: Die Karte ordnet die drei
/// Panel-Beschriftungen falsch zu; maßgeblich ist der Designer (Neigung [°],
/// Azimut [°], Anzahl Module).
/// </summary>
public class PhotovoltaikDialogTests : EposBunitContext
{

    /// <summary>
    /// Der Filterstand DIESES Prüfstands. Ohne ihn nähme der Dialog den aus dem
    /// <c>Katalogfilterregister</c> — der lebt prozessweit, und xunit fährt
    /// Testklassen nebeneinander. Dass das Register wirklich teilt, prüft
    /// <c>KatalogfilterstandTests</c>.
    /// </summary>
    private readonly Katalogfilterstand _filterstand = new();
    /// <summary>
    /// Das PROFIL des Projektdialogs (W14a-E-10 / S2.1): dieselben sieben Spalten wie
    /// in der Verwaltung, dazu die achte „im Projekt verwendet" (Q12).
    /// </summary>
    private static readonly Katalogfilterprofil Profil =
        Katalogfilterprofil.MitVerwendung(Anlagenart.Photovoltaik,
            s => Resource.ResourceManager.GetString(s) ?? s);

    private static IReadOnlyList<Katalogfilterzeile> Katalogzeilen() => new[]
    {
        new Katalogfilterzeile(31, "Modul 400")
            .MitText(Katalogfilterprofil.SpBezeichner, "Modul 400")
            .MitText(Katalogfilterprofil.SpHersteller, "Musterwerk")
            .MitZahl(Katalogfilterprofil.SpPstc, 400.0, 0)
            .MitZahl(Katalogfilterprofil.SpEta, 20.5, 1)
            .MitText(Katalogfilterprofil.SpTechnologie, "Mono-c-Si")
            .MitZahl(Katalogfilterprofil.SpModulflaeche, 1.95, 2)
            .MitZahl(Katalogfilterprofil.SpTnoct, 45.0, 0),

        new Katalogfilterzeile(32, "Modul 500")
            .MitText(Katalogfilterprofil.SpBezeichner, "Modul 500")
            .MitText(Katalogfilterprofil.SpHersteller, "Solar AG")
            .MitZahl(Katalogfilterprofil.SpPstc, 500.0, 0)
            .MitZahl(Katalogfilterprofil.SpEta, 21.8, 1)
            .MitText(Katalogfilterprofil.SpTechnologie, "Mono-c-Si")
            .MitZahl(Katalogfilterprofil.SpModulflaeche, 2.29, 2)
            .MitZahl(Katalogfilterprofil.SpTnoct, 44.0, 0),
    };

    public PhotovoltaikDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static ErzeugerZeile Zeile(int schluessel, string name, int geraetId)
        => new() { Schluessel = schluessel, Bezeichner = name, GeraetId = geraetId,
                   Neigung = 30, Azimut = 180, AnzahlModule = 20 };

    private static ErzeugerDetail Detail(string name) => new(
        name, "Beschreibung",
        new[] { ("Hersteller:", "Musterwerk"), ("Modul Leistung [W]:", "275,19") });

    /// <summary>
    /// Das PROFIL des Modulkatalogs — aus ihm baut die Hülle über die
    /// <c>ModulFeldwertBruecke</c> die Felder des Aufklappers. Der Prüfstand nimmt
    /// DASSELBE Profil, damit die Feldarten hier nicht erfunden werden.
    /// </summary>
    private static readonly ModulKatalogProfil Modulprofil =
        ModulKatalogProfil.Finde(ModulKatalogArt.Photovoltaik,
            s => Resource.ResourceManager.GetString(s) ?? s);

    /// <summary>
    /// Die Felder des Aufklappers „Alle Daten anzeigen" (Anwenderentscheid 15.09.2026)
    /// — abgebildet wie in der Hülle: Der Bezeichner ist gesperrt, ein Auswahlfeld
    /// bleibt Lesewert (es führt seine Optionen im Modulkatalog, nicht hier), alles
    /// Übrige ist editierbar.
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

    private IRenderedComponent<PhotovoltaikDialog> Aufbauen(
        List<ErzeugerZeile>? zeilen = null,
        Func<int, AufnahmeErgebnis>? aufnehmen = null,
        Action<ErzeugerZeile>? entfernen = null,
        Action<ErzeugerZeile>? uebernehmen = null,
        Func<string>? gesamt = null,
        Func<int, bool>? katalogLoeschen = null,
        Func<IReadOnlyDictionary<string, object>>? verwaltung = null,
        bool wizard = false,
        Func<string, ErzeugerDetail>? detail = null,
        Func<string, IReadOnlyList<BrowserFeldwert>?>? katalogfelder = null,
        Func<string, IReadOnlyList<BrowserFeldwert>, KatalogSpeicherErgebnis>? felderSpeichern = null,
        Action<bool>? geschlossen = null)
    {
        return Render<PhotovoltaikDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<ErzeugerZeile> { Zeile(1, "Modul 400", 31) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Detail, detail ?? (n => Detail(n)))
            .Add(x => x.Aufnehmen, aufnehmen ?? (_ => new AufnahmeErgebnis(Zeile(9, "Modul 500", 32))))
            .Add(x => x.Entfernen, entfernen)
            .Add(x => x.Uebernehmen, uebernehmen)
            .Add(x => x.Gesamtleistung, gesamt ?? (() => "8"))
            .Add(x => x.KatalogLoeschen, katalogLoeschen ?? (_ => true))
            .Add(x => x.Katalogfelder, katalogfelder)
            .Add(x => x.KatalogfelderSpeichern, felderSpeichern)
            .Add(x => x.VerwaltungGaben, verwaltung)
            .Add(x => x.Wizard, wizard)
            .Add(x => x.Geschlossen, ok => geschlossen?.Invoke(ok)));
    }

    /// <summary>Die zweite Liste ist die KATALOGliste; ihre erste Zeile ist „Modul 400".</summary>
    private static void KatalogzeileWaehlen(IRenderedComponent<PhotovoltaikDialog> cut, int nummer = 0)
        => cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[nummer].Click();

    /// <summary>Der Modulbereich — der Gruppenkopf „Modul Eigenschaften:".</summary>
    private static AngleSharp.Dom.IElement Modulbereich(IRenderedComponent<PhotovoltaikDialog> cut)
        => cut.FindAll(".epos-gruppenkopf")
              .First(e => e.TextContent.Contains("Modul Eigenschaften:"));

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
        Assert.Contains("ausgewählte Module", ueberschriften);
        Assert.Contains("Module aus Datenbank", ueberschriften);

        var gruppen = cut.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent).ToList();
        Assert.Contains("PV Anlage Eigenschaften:", gruppen);
        Assert.Contains("Modul Eigenschaften:", gruppen);
    }

    [Fact]
    public void Die_drei_Anlagenfelder_tragen_die_Beschriftungen_des_Designers()
    {
        // R-W6-7: Die Feldkarte ordnet "Azimut [°]" dem Feld textBox_AnlagenLeistung
        // und "10" dem Feld textBox_Azimut zu. Der Designer sagt es anders, und er
        // hat recht: label3 "Neigung [°]:" liegt ueber textBox_Neigung, label6
        // "Azimut [°]:" ueber textBox_Azimut, label7 "Anzahl Module:" ueber
        // textBox_AnlagenLeistung.
        var cut = Aufbauen();

        var block = cut.Find(".epos-anlagenblock");
        var texte = block.QuerySelectorAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.Equal(new[] { "Neigung [°]:", "Azimut [°]:", "Anzahl Module:" }, texte);

        // Neigung und Azimut sind ganzzahlig, die Anzahl Module ist ein double
        // (WErzeugerModel.PV_Leistung) - der Feldname taeuscht, der Inhalt ist eine
        // Stueckzahl.
        Assert.Equal(2, block.QuerySelectorAll("input[inputmode=numeric]").Length);
        Assert.Single(block.QuerySelectorAll("input[inputmode=decimal]"));
    }

    [Fact]
    public void Der_Anlagenblock_erscheint_nur_bei_gewaehlter_Projektzeile()
    {
        // panel1.Visible - der Vorlaeufer blendete ihn beim Katalogsatz aus.
        var cut = Aufbauen();
        Assert.Single(cut.FindAll(".epos-anlagenblock"));

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();

        Assert.Empty(cut.FindAll(".epos-anlagenblock"));
    }

    /// <summary>
    /// Ohne Parametersatz der Modulverwaltung kein Knopf — Hausregel. Seit
    /// iU9-W14a.3 ist die Verwaltung eine ÜBERLAGERUNG im selben Fenster.
    /// </summary>
    [Fact]
    public void Der_Bearbeiten_Knopf_erscheint_nur_mit_Verwaltungsgaben()
    {
        var ohne = Aufbauen();
        Assert.DoesNotContain(ohne.FindAll("button").Select(b => b.TextContent),
                              t => t == "Modul Bearbeiten...");

        var mit = Aufbauen(verwaltung: () => Verwaltungsgaben());
        Assert.Contains(mit.FindAll("button").Select(b => b.TextContent),
                        t => t == "Modul Bearbeiten...");
    }

    /// <summary>Ein Mindestsatz für die Überlagerung — der Katalog braucht sein Profil.</summary>
    private static IReadOnlyDictionary<string, object> Verwaltungsgaben()
        => new Dictionary<string, object>
        {
            ["Art"] = WindowsFormsApplication1.ModulKatalogArt.Photovoltaik,
            ["Wege"] = new EPOS.UI.Dialoge.Erzeuger.ModulKatalogWege()
        };

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
    public void Der_Pfeil_nimmt_ohne_Traegerdialog_auf()
    {
        // Anders als Heizkessel und BHKW: keine Traegervariante, keine Projektkopie.
        int? gefragt = null;
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Modul 400", 31) };
        var cut = Aufbauen(zeilen, aufnehmen: id =>
        {
            gefragt = id;
            return new AufnahmeErgebnis(Zeile(9, "Modul 500", 32));
        });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[1].Click();
        cut.FindAll(".epos-zweispalten-uebernahme button")[0].Click();

        Assert.Equal(32, gefragt);
        Assert.Equal(2, zeilen.Count);
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
    }

    [Fact]
    public void Der_Pfeil_zurueck_entfernt_die_ZEILE_nicht_ihren_Index()
    {
        // A-5: btn_Entfernen_Click nahm RemoveAt(SelectedIndex) auf eine Liste, die im
        // Assistenten ALLE Erzeugertypen fuehrt - der Index passte dort nicht.
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Modul 400", 31), Zeile(2, "Modul 400", 31) };
        var entfernt = new List<ErzeugerZeile>();
        var cut = Aufbauen(zeilen, entfernen: z => entfernt.Add(z));

        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[1].Click();
        cut.FindAll(".epos-zweispalten-uebernahme button")[1].Click();

        Assert.Single(zeilen);
        Assert.Equal(1, zeilen[0].Schluessel);
        Assert.Equal(2, entfernt[0].Schluessel);
    }

    // =================================================================================
    // Anlagenwerte und Gesamtleistung
    // =================================================================================

    [Fact]
    public void Die_drei_Anlagenwerte_wandern_ins_Modell()
    {
        var uebernommen = new List<ErzeugerZeile>();
        var cut = Aufbauen(uebernehmen: z => uebernommen.Add(z));

        var block = cut.Find(".epos-anlagenblock");
        block.QuerySelectorAll("input[inputmode=numeric]")[0].Input("35");
        block.QuerySelectorAll("input[inputmode=numeric]")[1].Input("200");
        block.QuerySelectorAll("input[inputmode=decimal]")[0].Input("25");

        Assert.Equal(35, cut.Instance.Projektzeile!.Neigung);
        Assert.Equal(200, cut.Instance.Projektzeile!.Azimut);
        Assert.Equal(25, cut.Instance.Projektzeile!.AnzahlModule);
        Assert.Equal(3, uebernommen.Count);
    }

    [Fact]
    public void Eine_neue_Modulzahl_zieht_die_Gesamtleistung_nach()
    {
        int rufe = 0;
        var cut = Aufbauen(gesamt: () => (++rufe).ToString());

        int vorher = rufe;
        cut.Find(".epos-anlagenblock input[inputmode=decimal]").Input("25");

        Assert.True(rufe > vorher, "Die Gesamtleistung wurde nicht neu erfragt.");
    }

    [Fact]
    public void Die_Gesamtleistung_kommt_fertig_von_aussen()
    {
        var cut = Aufbauen(gesamt: () => "12,50");
        Assert.Equal("12,50", cut.Instance.Gesamt);
    }

    // =====================================================================
    //  Anwenderentscheid W6‑O‑5 (05.09.2026) — „Gesamtleistung in kW"
    // =====================================================================

    /// <summary>
    /// <b>W6‑O‑5:</b> Der Block „Modul Eigenschaften" zeigte zwei Leistungen mit
    /// falscher Einheit — die Katalogspalte <c>Leistung</c> führt WATT je Modul, und
    /// über beiden stand „[KW]". Seither heißt das Modulfeld „[W]" und die
    /// Gesamtleistung „[kW]".
    ///
    /// <para>Zehn Module à 275,19 W sind 2,752 kW. Gerechnet und formatiert wird das
    /// im Kern (<c>PhotovoltaikCtrl.GesamtleistungText</c>, Nachweis
    /// <c>EPOS.Kern.Tests/PvModulparameterTests</c>); hier steht, dass die Komponente
    /// die fertige Zahl unter der richtigen Beschriftung zeigt. Die Einheit steht in
    /// der BESCHRIFTUNG und nicht im Wert — so wie bei jedem anderen Feld des Blocks.</para>
    /// </summary>
    [Fact]
    public void Zehn_Module_stehen_als_zwei_Komma_sieben_fuenf_zwei_kW_da()
    {
        // Zehn Module in der Projektzeile - die Zahl selbst rechnet die Huelle.
        var zeilen = new List<ErzeugerZeile>
        {
            new() { Schluessel = 1, Bezeichner = "Modul 400", GeraetId = 31,
                    Neigung = 30, Azimut = 180, AnzahlModule = 10 }
        };
        var cut = Aufbauen(zeilen, gesamt: () => "2,752");

        var block = cut.FindAll(".epos-gruppenkopf")
                       .First(e => e.TextContent.Contains("Modul Eigenschaften:"));

        var texte = block.QuerySelectorAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.Contains("Modul Leistung [W]:", texte);
        Assert.Contains("Gesamtleistung [kW]:", texte);

        // Und das Feld dahinter zeigt genau die Zahl, die hereinkam.
        Assert.Equal("2,752", cut.Instance.Gesamt);
        Assert.Contains("2,752",
            block.QuerySelectorAll("input").Select(e => e.GetAttribute("value")));
    }

    /// <summary>
    /// Die englische Beschriftung heißt „Total power [kW]" — sie sagte die Einheit
    /// schon vor W6‑O‑5 richtig; geändert hat sich dort nur „Module power [W]".
    /// Die Komponente übersetzt nichts, sie zeigt, was die Hülle hereingibt.
    /// </summary>
    [Fact]
    public void Auf_englisch_heissen_die_zwei_Felder_W_und_kW()
    {
        using var _ = new Kulturvorrichtung("en-US");

        var cut = Render<PhotovoltaikDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Modul 400", 31) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Detail, n => new ErzeugerDetail(
                n, "", new[] { ("Module power [W]:", "275.19") }))
            .Add(x => x.LabelGesamtleistung, "Total power [kW]:")
            .Add(x => x.Gesamtleistung, () => "2.752"));

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.Contains("Module power [W]:", texte);
        Assert.Contains("Total power [kW]:", texte);
        Assert.Equal("2.752", cut.Instance.Gesamt);
    }

    /// <summary>
    /// Die VORGABE der Komponente trägt die neue Einheit — eine Hülle, die den
    /// Ressourcenschlüssel nicht setzt, zeigt nicht wieder „[KW]".
    /// </summary>
    [Fact]
    public void Die_Vorgabe_der_Beschriftung_sagt_kW()
    {
        Assert.Equal("Gesamtleistung [kW]:", new PhotovoltaikDialog().LabelGesamtleistung);
    }

    // =================================================================================
    // Katalogpflege und Tastatur
    // =================================================================================

    [Fact]
    public void Loeschen_fragt_zuerst_nach()
    {
        var geloescht = new List<int>();
        var cut = Aufbauen(katalogLoeschen: id => { geloescht.Add(id); return true; });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-zweispalten-spalte")[1].QuerySelectorAll(".epos-leiste button")[0].Click();

        Assert.Single(cut.FindAll(".epos-rueckfrage"));
        Assert.Empty(geloescht);

        cut.FindAll(".epos-rueckfrage button")[0].Click();
        Assert.Equal(new[] { 31 }, geloescht);
    }

    /// <summary>
    /// „Modul Bearbeiten…" öffnet den Modulkatalog als ÜBERLAGERUNG im selben Fenster —
    /// bis iU9-W14a war es ein Sprung in ein zweites Fenster
    /// (<c>Sprungziel.PvAdmin</c>).
    /// </summary>
    [Fact]
    public void Bearbeiten_oeffnet_die_Modulverwaltung_als_Ueberlagerung()
    {
        var cut = Aufbauen(verwaltung: () => Verwaltungsgaben());

        Assert.False(cut.Instance.VerwaltungOffen);

        // Der Knopf steht seit dem 15.09.2026 im MODULBEREICH, nicht mehr unter der
        // Katalogliste.
        Modulbereich(cut).QuerySelectorAll(".epos-leiste button")
                         .First(b => b.TextContent == "Modul Bearbeiten...").Click();

        Assert.True(cut.Instance.VerwaltungOffen);
        Assert.NotEmpty(cut.FindAll(".epos-ueberlagerung"));
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

    // =====================================================================
    //  Der Aufklapper „Alle Daten anzeigen" — Anwenderentscheid 15.09.2026
    // =====================================================================
    //  Er löst den Parameterblock aus W6‑E‑1 (05.09.2026) ab: Damals dreizehn
    //  NUR LESBARE Zeilen aus einer eigenen Liste, jetzt ALLE Felder des
    //  Katalogsatzes in der Bauart aller sechs Erzeuger — derselbe Baustein
    //  Katalogfelder, gespeist aus derselben Feldliste wie der Modulkatalog,
    //  und bearbeitbar, wo der Katalog einen Speicherweg hat.

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
    /// Ohne Weg zu den Feldern kein Aufklapper — Hausregel „kein Delegat, kein
    /// Knopf". Ein leerer Aufklapper wäre ein Versprechen ohne Inhalt.
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
    /// Der Knopf klappt zu und wieder auf — der Zustand gehört dem Dialog, nicht
    /// dem Browser.
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
    /// <b>Der Block gehört zum GEWÄHLTEN Modul.</b> Wer in der Katalogliste ein anderes
    /// Modul wählt, sieht dessen Werte — und der Aufklappzustand bleibt stehen, sonst
    /// müsste man ihn beim Vergleichen zweier Module jedes Mal neu aufziehen.
    /// </summary>
    [Fact]
    public void Ein_Modulwechsel_zieht_den_Block_nach()
    {
        int rufe = 0;
        var cut = Aufbauen(katalogfelder: n => { rufe++; return Katalogfelder(n); });
        KatalogzeileWaehlen(cut);

        Assert.Equal(1, rufe);
        Assert.Equal("Modul 400", cut.Find(".epos-modulparameter")
                                     .QuerySelectorAll("input")[0].GetAttribute("value"));

        // Zweite Zeile der KATALOGliste: "Modul 500".
        KatalogzeileWaehlen(cut, 1);

        Assert.True(cut.Instance.ParameterOffen);
        Assert.Equal(2, rufe);
        Assert.Equal("Modul 500", cut.Find(".epos-modulparameter")
                                     .QuerySelectorAll("input")[0].GetAttribute("value"));
    }

    /// <summary>
    /// <b>Zwei Felder bleiben Lesewerte:</b> der Bezeichner — er ist der Schlüssel des
    /// <c>UPDATE</c> — und die Zelltechnologie, ein Auswahlfeld, dessen Optionen der
    /// Aufklapper nicht führt. Die Zahl der gesperrten Felder liest der Prüfstand aus
    /// DEMSELBEN Profil, aus dem die Hülle sie ableitet.
    /// </summary>
    [Fact]
    public void Der_Bezeichner_und_das_Auswahlfeld_bleiben_Lesewerte()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder,
                           felderSpeichern: (n, _) => new KatalogSpeicherErgebnis(true, "", n));
        KatalogzeileWaehlen(cut);

        int erwartet = Modulprofil.Felder
            .Count(f => f.Gesperrt || f.Art == BrowserFeldArt.Auswahl);

        Assert.True(erwartet > 0, "Das Profil führt weder ein gesperrtes noch ein Auswahlfeld.");
        Assert.Equal(erwartet,
            cut.Find(".epos-modulparameter").QuerySelectorAll("input[readonly]").Length);
    }

    /// <summary>
    /// <b>Ohne Speicherweg ist der Aufklapper reine Anzeige</b> — kein Knopf, jedes
    /// Feld nur lesbar. Das ist die Lage eines Katalogs ohne Schreibweg, keine
    /// Entscheidung dieses Dialogs.
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

        Assert.Equal("Modul 400", name);
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
    /// meldet seinen Zustand nach oben — dieselbe Naht, die der Katalogbrowser schon
    /// hatte.
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

        // Der Knopf bleibt frei: Die Aenderung steht noch im Aufklapper.
        Assert.False(cut.Find(".epos-modulparameter .epos-leiste .epos-knopf")
                        .HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>Die Knöpfe zum gewählten Satz stehen im MODULBEREICH</b> (Anwenderentscheid
    /// 15.09.2026): „Modul Bearbeiten…" ist dorthin gewandert, wo der Satz steht;
    /// „Modul Löschen" bleibt bei der LISTE, denn es wirkt auf die Listenzeile.
    /// </summary>
    [Fact]
    public void Bearbeiten_steht_im_Modulbereich_Loeschen_bei_der_Liste()
    {
        var cut = Aufbauen(verwaltung: () => Verwaltungsgaben());
        KatalogzeileWaehlen(cut);

        var unterDerListe = cut.FindAll(".epos-zweispalten-spalte")[1]
                               .QuerySelectorAll(".epos-leiste button")
                               .Select(b => b.TextContent).ToList();
        Assert.Equal(new[] { "Modul Löschen" }, unterDerListe);

        var imModulbereich = Modulbereich(cut)
                             .QuerySelectorAll(".epos-leiste button")
                             .Select(b => b.TextContent).ToList();
        Assert.Contains("Modul Bearbeiten...", imModulbereich);
    }

    /// <summary>
    /// <b>Die Knopfzeile steht als ERSTES unter dem Modulkopf</b> (Anwenderentscheid
    /// 16.09.2026: „Der Bearbeiten-Button soll weiter oben … stehen, so dass er besser
    /// sichtbar ist"): links die Kostenknöpfe, rechts „Modul Bearbeiten…", dazwischen
    /// der Füller. Danach erst die Felder, danach der Aufklapper.
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
        Assert.Equal("Modul Bearbeiten...", teile[2].TextContent.Trim());
    }

    /// <summary>
    /// <b>Die Kostenleiste steht LINKS in der Knopfzeile</b> und im Assistenten gar
    /// nicht — dieselbe Anordnung wie beim Heizkessel. Ohne Delegat zeichnet die
    /// Leiste keinen Knopf (ihre eigene Regel).
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

        var cut = Render<PhotovoltaikDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Modul 400", 31) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Detail, n => Detail(n))
            .Add(x => x.Gesamtleistung, () => "8")
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

    /// <summary>
    /// <b>Die Komponente formatiert und übersetzt nichts.</b> Unter <c>en-US</c>
    /// stehen genau die Texte da, die die Hülle hereingibt — samt der Zahlen, die
    /// dort schon in der Kultur des Anwenders formatiert wurden.
    /// </summary>
    [Fact]
    public void Auf_englisch_zeigt_die_Komponente_was_die_Huelle_hereingibt()
    {
        using var _ = new Kulturvorrichtung("en-US");

        var cut = Render<PhotovoltaikDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Modul 400", 31) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Detail, n => new ErzeugerDetail(n, "", Array.Empty<(string, string)>()))
            .Add(x => x.Katalogfelder, (Func<string, IReadOnlyList<BrowserFeldwert>?>)(_ =>
                new List<BrowserFeldwert>
                {
                    new() { Schluessel = "ETA", Bezeichnung = "Efficiency:", Einheit = "%",
                            Art = BrowserFeldArt.Zahl, Editierbar = false, Wert = "16.91" },
                    new() { Schluessel = "TECH", Bezeichnung = "Cell technology:",
                            Art = BrowserFeldArt.Text, Editierbar = false, Wert = "–" }
                }))
            .Add(x => x.LabelAlleParameter, "Show all data")
            .Add(x => x.Gesamtleistung, () => "8"));

        KatalogzeileWaehlen(cut);

        // Aufgeklappt ist die Vorgabe (Anwenderentscheid 16.09.2026) - der Knopf
        // traegt seine englische Beschriftung, geklickt wird nicht: Ein Klick
        // klappte den Block ZU.
        var knopf = cut.Find(".epos-modulparameter-knopf");
        Assert.Contains("Show all data", knopf.TextContent);
        Assert.Equal("true", knopf.GetAttribute("aria-expanded"));

        var block = cut.Find(".epos-modulparameter");
        Assert.Equal(new[] { "Efficiency:", "Cell technology:" },
                     block.QuerySelectorAll(".epos-feld-text").Select(e => e.TextContent).ToArray());
        Assert.Equal(new[] { "16.91", "–" },
                     block.QuerySelectorAll("input").Select(e => e.GetAttribute("value")).ToArray());
    }

    // =================================================================================
    //  Stufe S2.1 / S2.3 / S2.5 - Anwenderentscheid W14a-E-10 vom 07.09.2026
    // =================================================================================

    /// <summary>Die Katalogliste - das UNTERE der beiden Raster.</summary>
    private static IReadOnlyList<AngleSharp.Dom.IElement> Katalogzeilen(
        IRenderedComponent<PhotovoltaikDialog> cut)
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

        _filterstand.Setzen(Katalogfilterprofil.SpHersteller, "Solar");
        cut.Render();

        Assert.Single(Katalogzeilen(cut));
        Assert.Equal("1 von 2 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);
        Assert.Contains("Modul 500", Katalogzeilen(cut)[0].TextContent);

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

        _filterstand.Sortieren(Katalogfilterprofil.SpPstc);
        cut.Render();
        Assert.Contains("Modul 400", Katalogzeilen(cut)[0].TextContent);

        _filterstand.Sortieren(Katalogfilterprofil.SpPstc);
        cut.Render();
        Assert.False(_filterstand.Aufsteigend);
        Assert.Contains("Modul 500", Katalogzeilen(cut)[0].TextContent);

        _filterstand.Sortieren(Katalogfilterprofil.SpPstc);
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
        _filterstand.Setzen(Katalogfilterprofil.SpHersteller, "Solar");
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
        var cut = Aufbauen(zeilen: new List<ErzeugerZeile> { Zeile(1, "Modul 400", 31) });

        var kopf = cut.FindAll(".epos-raster")[1]
                      .QuerySelectorAll("th").Select(e => e.TextContent.Trim()).ToList();
        Assert.Contains(kopf, k => k.StartsWith("im Projekt verwendet"));

        var zeilen = Katalogzeilen(cut);
        Assert.Equal(2, zeilen.Count);
        Assert.Equal(1, zeilen.Count(z => z.QuerySelectorAll("td").Last().TextContent.Trim() == "Ja"));

        var traegt = zeilen.First(z => z.QuerySelectorAll("td").Last().TextContent.Trim() == "Ja");
        Assert.Contains("Modul 400", traegt.TextContent);
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
        _filterstand.Setzen(Katalogfilterprofil.SpHersteller, "Solar");
        ersterAufbau.Render();
        Assert.Single(Katalogzeilen(ersterAufbau));

        // Der Dialog geht zu und wieder auf - derselbe Stand, dieselbe Sicht.
        var zweiterAufbau = Aufbauen();

        Assert.Single(Katalogzeilen(zweiterAufbau));
        Assert.Equal("1 von 2 Sätzen", zweiterAufbau.Find(".epos-katalog-treffer").TextContent);
        Assert.Single(zweiterAufbau.FindAll(".epos-katalog-ruecksetzer"));
    }

    // =================================================================================
    // ET-5 (Anwenderentscheid 08.09.2026): der Energietraeger der Anlage, Gruppe > Art
    // =================================================================================

    private static IReadOnlyList<EnergietraegerWahl.Eintrag> Traegerkatalog() => new[]
    {
        new EnergietraegerWahl.Eintrag(11, "Gas", "Erdgas E"),
        new EnergietraegerWahl.Eintrag(60, "Strom", "Elektrische Energie"),
        new EnergietraegerWahl.Eintrag(58, "Strom", "Elektrische Energie 2")
    };

    [Fact]
    public void Die_markierte_Anlage_zeigt_ihren_Energietraeger_und_meldet_den_Wechsel()
    {
        (ErzeugerZeile Zeile, int Neu)? gemeldet = null;
        ErzeugerZeile zeile = Zeile(1, "Modul 400", 31);
        zeile.CarrierId = 60;
        var cut = Render<PhotovoltaikDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { zeile })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Detail, n => Detail(n))
            .Add(x => x.Gesamtleistung, () => "8")
            .Add(x => x.KatalogLoeschen, _ => true)
            .Add(x => x.Traegerkatalog, Traegerkatalog())
            .Add(x => x.TraegerWechseln, (ErzeugerZeile z, int neu) => gemeldet = (z, neu)));

        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[0].Click();

        var selects = cut.Find(".epos-traegerwahl").QuerySelectorAll("select");
        Assert.Equal(2, selects.Length);
        Assert.Contains("Strom", selects[0].InnerHtml);
        Assert.Contains("Elektrische Energie 2", selects[1].InnerHtml);
        Assert.DoesNotContain("Erdgas E", selects[1].InnerHtml);

        selects[1].Change("58");

        Assert.Equal(58, gemeldet?.Neu);
        Assert.Same(zeile, gemeldet?.Zeile);
        Assert.Equal(58, zeile.CarrierId);
    }

    [Fact]
    public void Ohne_Traegerkatalog_steht_keine_Traegerwahl()
    {
        var cut = Aufbauen();
        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        Assert.Empty(cut.FindAll(".epos-traegerwahl"));
    }

    // =====================================================================
    //  Der Hilfe-Assistent: die Felder der Bausteine (Welle KI-F1)
    // =====================================================================

    /// <summary>
    /// Die Maske gibt seit dieser Welle auch die MODELLFELDER heraus — sie stehen im
    /// Baustein <c>PvModellFelder</c> und binden an dieselbe Projektzeile.
    /// </summary>
    /// <remarks>
    /// <b>Monotone Aussage</b> (Muster <c>KiMaskenhakenTests</c>): Geprüft wird, was
    /// nach dem Zeichnen DA ist. Die Brücke ist prozessweiter Zustand.
    /// </remarks>
    [Fact]
    public void Der_Assistent_setzt_die_Systemverluste_der_gewaehlten_Zeile()
    {
        ErzeugerZeile zeile = Zeile(1, "Anlage A", 100);
        zeile.Systemverluste = 3.0;
        Aufbauen(zeilen: new List<ErzeugerZeile> { zeile });

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.PHOTOVOLTAIK));

        KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.PHOTOVOLTAIK, "systemverluste");
        Assert.NotNull(zugang);
        Assert.Equal(3.0, zugang.Lesen());

        zugang.Setzen(5.5);
        Assert.Equal(5.5, zeile.Systemverluste);
    }

    /// <summary>
    /// Die STRÄNGE sind eine Liste: Je vorhandener Strangzeile wird aus der
    /// Spaltendeklaration ein gewöhnliches Feld, und sein Klartextname trägt den
    /// Bezeichner des Strangs.
    /// </summary>
    [Fact]
    public void Der_Assistent_liest_und_setzt_die_Straenge_je_Zeile()
    {
        ErzeugerZeile zeile = Zeile(1, "Anlage A", 100);
        zeile.MitWechselrichter = true;
        zeile.Straenge.Add(new StrangZeile { Rang = 1, Bezeichner = "Dach Süd", ModuleReihe = 12 });
        zeile.Straenge.Add(new StrangZeile { Rang = 2, Bezeichner = "Dach West", ModuleReihe = 8 });

        Aufbauen(zeilen: new List<ErzeugerZeile> { zeile });

        var reihen = KiMaskenbruecke.Lesen(KiMaskennamen.PHOTOVOLTAIK)
                                    .Where(w => w.Name.StartsWith("strang_module_reihe"))
                                    .ToList();

        Assert.Equal(2, reihen.Count);
        Assert.Contains(reihen, w => w.Anzeigename.Contains("Dach Süd"));
        Assert.Contains(reihen, w => w.Anzeigename.Contains("Dach West"));

        // Der Schluessel der ZWEITEN Zeile - er trägt ihre Nummer, der Anzeigename
        // ihren Bezeichner.
        string schluessel = reihen.First(w => w.Anzeigename.Contains("Dach West")).Name;

        KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.PHOTOVOLTAIK, schluessel);
        Assert.NotNull(zugang);
        Assert.Equal(8, zugang.Lesen());

        zugang.Setzen(16);
        Assert.Equal(16, zeile.Straenge[1].ModuleReihe);
    }

    /// <summary>
    /// <b>Der ZEUGE der Sichtklasse <c>PhotovoltaikKiSicht</c></b> (Welle KI‑F7,
    /// Anwenderentscheid 21.09.2026): Die Maske gibt seit dieser Welle auch die zwei
    /// AUSLEGUNGSTEMPERATUREN heraus — sie stehen im Strangabschnitt, gehören aber dem
    /// PROJEKT und hängen deshalb nicht an der Anlagenzeile.
    /// </summary>
    /// <remarks>
    /// <b>Gesetzt wird über den Weg der Maske.</b> Nach der Setzung steht die Zahl im
    /// lebenden Feld des Strangbausteins UND in den Projekteinstellungen — das zweite
    /// misst der Rückruf <c>AuslegungstemperaturenSetzen</c>. Genau dafür gibt es die
    /// Sichtklasse: An <c>ErzeugerZeile</c> gäbe es diese Größe gar nicht.
    /// </remarks>
    [Fact]
    public void Der_Assistent_setzt_die_Auslegungstemperatur_des_Projekts()
    {
        double? kalt = null, heiss = null;
        ErzeugerZeile zeile = Zeile(1, "Anlage A", 100);

        IRenderedComponent<PhotovoltaikDialog> cut = Render<PhotovoltaikDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { zeile })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Detail, n => Detail(n))
            .Add(x => x.Gesamtleistung, () => "8")
            .Add(x => x.AuslegungKalt, -12.0)
            .Add(x => x.AuslegungHeiss, 65.0)
            .Add(x => x.AuslegungstemperaturenSetzen,
                 (double? k, double? h) => { kalt = k; heiss = h; }));

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.PHOTOVOLTAIK));

        KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.PHOTOVOLTAIK, "auslegung_kalt");
        Assert.NotNull(zugang);
        Assert.Equal(-12.0, zugang.Lesen());
        Assert.True(zugang.Setzbar);

        // Der Assistent ruft aus seinem EIGENEN Faden; der Weg der Maske geht über den
        // Blazor-Verteiler und ist damit nicht sofort fertig (Hausregel EPOS.UI).
        zugang.Setzen(-15.5);

        cut.WaitForAssertion(() => Assert.Equal(-15.5, kalt));
        Assert.Equal(65.0, heiss);
        Assert.Equal(-15.5, KiMaskenbruecke.Feldzugang(KiMaskennamen.PHOTOVOLTAIK,
                                                       "auslegung_kalt").Lesen());

        // Die Felder der ZEILE gehen weiterhin an die Zeile — die Sicht reicht sie
        // unverändert durch.
        KiMaskenbruecke.Feldzugang(KiMaskennamen.PHOTOVOLTAIK, "neigung").Setzen(35);
        Assert.Equal(35, zeile.Neigung);
    }

    /// <summary>
    /// <b>Das RECHENMODELL ist ein Wahlfeld mit den Einträgen der Maske</b> (KI‑D‑Q6,
    /// KI‑D‑Q7): „Einfach" und „Erweitert" stehen im Auswahlfeld des Bausteins, und
    /// über genau diese Texte trifft der Assistent den Wahrheitswert der Anlage.
    /// </summary>
    [Fact]
    public void Der_Assistent_waehlt_das_Rechenmodell_ueber_seinen_Text()
    {
        ErzeugerZeile zeile = Zeile(1, "Anlage A", 100);
        Aufbauen(zeilen: new List<ErzeugerZeile> { zeile });

        KiFeldzugang modell =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.PHOTOVOLTAIK, "modell_erweitert");
        Assert.NotNull(modell);
        Assert.True(modell.IstWahl);

        IReadOnlyList<KiWahleintrag> eintraege = modell.Wahleintraege();
        Assert.Equal(2, eintraege.Count);

        KiFeldumsetzung wahl = KiFeldwandler.Wandle(modell, eintraege[1].Text);
        Assert.True(wahl.Ok, wahl.Grund);
        modell.Setzen(wahl.Wert);

        Assert.True(zeile.ModellErweitert);
    }
}
