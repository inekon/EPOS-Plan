using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bunit;
using EPOS.UI.Dialoge.Allgemein;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
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
        Katalogfilterprofil.MitVerwendung(Anlagenart.Heizkessel,
            s => Resource.ResourceManager.GetString(s) ?? s);

    /// <summary>
    /// Die Katalogzeilen. Sie tragen echte Werte, damit sich Filter und Sortierung
    /// pruefen lassen — der Filter arbeitet auf dem ANGEZEIGTEN Wert (5.6.3).
    /// </summary>
    private static IReadOnlyList<Katalogfilterzeile> Katalogzeilen() => new[]
    {
        new Katalogfilterzeile(11, "Kessel A")
            .MitText(Katalogfilterprofil.SpBezeichner, "Kessel A")
            .MitText(Katalogfilterprofil.SpHersteller, "Vaillant")
            .MitText(Katalogfilterprofil.SpBrennstoff, "Erdgas E")
            .MitZahl(Katalogfilterprofil.SpPtherm, 15.0)
            .MitZahl(Katalogfilterprofil.SpEta, 0.97, 3)
            .MitKennzeichen(Katalogfilterprofil.SpBrennwert, false),

        new Katalogfilterzeile(12, "Kessel B")
            .MitText(Katalogfilterprofil.SpBezeichner, "Kessel B")
            .MitText(Katalogfilterprofil.SpHersteller, "Buderus")
            .MitText(Katalogfilterprofil.SpBrennstoff, "Heizöl EL")
            .MitZahl(Katalogfilterprofil.SpPtherm, 80.0)
            .MitZahl(Katalogfilterprofil.SpEta, 0.92, 3)
            .MitKennzeichen(Katalogfilterprofil.SpBrennwert, true),
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
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
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
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-uebernahme button").Count);
        // Entscheid #76, Anordnung seit W14a-E-10-Q2 untereinander: Jeder Knopf
        // traegt EIN Zeichen (▲ hinauf ins Projekt, ▼ hinunter in den Katalog)
        // und dazu seine Aufgabe im Klartext.
        Assert.Equal("▲", cut.FindAll(".epos-zweispalten-uebernahme button")[0].QuerySelector(".epos-zweispalten-pfeil")!.TextContent);
        Assert.Equal("▼", cut.FindAll(".epos-zweispalten-uebernahme button")[1].QuerySelector(".epos-zweispalten-pfeil")!.TextContent);
        Assert.Equal("In das Projekt übernehmen",
                     cut.FindAll(".epos-zweispalten-uebernahme button")[0].QuerySelector(".epos-zweispalten-knopftext")!.TextContent);
        Assert.Equal("Aus dem Projekt entfernen",
                     cut.FindAll(".epos-zweispalten-uebernahme button")[1].QuerySelector(".epos-zweispalten-knopftext")!.TextContent);

        var ueberschriften = cut.FindAll(".epos-untergruppe").Select(e => e.TextContent).ToList();
        Assert.Contains("ausgewählt im Projekt", ueberschriften);
        Assert.Contains("Kessel aus Datenbank", ueberschriften);

        // W14a-E-10 / S2.1: Die zwei Klapplisten sind weg; an ihrer Stelle steht
        // die EINE Suchzeile mit der Trefferzahl, und der Filter sitzt im
        // Spaltenkopf.
        Assert.Single(cut.FindAll(".epos-katalog-suchzeile"));
        Assert.Equal("2 von 2 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.DoesNotContain("Filtern nach Brennstoffart:", texte);
        Assert.DoesNotContain("Filtern nach Leistung:", texte);
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

        Assert.True(cut.FindAll(".epos-zweispalten-uebernahme button")[0].HasAttribute("disabled"));
    }

    [Fact]
    public void Der_Pfeil_oeffnet_zuerst_die_Traegerwahl()
    {
        var cut = Aufbauen();

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-zweispalten-uebernahme button")[0].Click();

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
        cut.FindAll(".epos-zweispalten-uebernahme button")[0].Click();
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
        cut.FindAll(".epos-zweispalten-uebernahme button")[0].Click();

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
        cut.FindAll(".epos-zweispalten-uebernahme button")[0].Click();

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
        cut.FindAll(".epos-zweispalten-uebernahme button")[1].Click();

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

        cut.FindAll(".epos-zweispalten-uebernahme button")[1].Click();

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

        // Seit S2.1 gibt es nur noch EINE Klappliste: die Trägerwahl. Die zwei
        // Filterlisten sind Spaltenköpfe geworden.
        var listen = cut.FindAll("select");
        Assert.Single(listen);
        listen[0].Change("6");

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

    // =================================================================================
    //  Stufe S2.1 / S2.3 / S2.5 - Anwenderentscheid W14a-E-10 vom 07.09.2026
    // =================================================================================

    /// <summary>Die Katalogliste - das UNTERE der beiden Raster.</summary>
    private static IReadOnlyList<AngleSharp.Dom.IElement> Katalogzeilen(
        IRenderedComponent<HeizkesselDialog> cut)
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

        _filterstand.Setzen(Katalogfilterprofil.SpBrennstoff, "Heizöl");
        cut.Render();

        Assert.Single(Katalogzeilen(cut));
        Assert.Equal("1 von 2 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);
        Assert.Contains("Kessel B", Katalogzeilen(cut)[0].TextContent);

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

        _filterstand.Sortieren(Katalogfilterprofil.SpPtherm);
        cut.Render();
        Assert.Contains("Kessel A", Katalogzeilen(cut)[0].TextContent);

        _filterstand.Sortieren(Katalogfilterprofil.SpPtherm);
        cut.Render();
        Assert.False(_filterstand.Aufsteigend);
        Assert.Contains("Kessel B", Katalogzeilen(cut)[0].TextContent);

        _filterstand.Sortieren(Katalogfilterprofil.SpPtherm);
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
        _filterstand.Setzen(Katalogfilterprofil.SpBrennstoff, "Heizöl");
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
        var cut = Aufbauen(zeilen: new List<ErzeugerZeile> { Zeile(1, "Kessel A", 100) });

        var kopf = cut.FindAll(".epos-raster")[1]
                      .QuerySelectorAll("th").Select(e => e.TextContent.Trim()).ToList();
        Assert.Contains(kopf, k => k.StartsWith("im Projekt verwendet"));

        var zeilen = Katalogzeilen(cut);
        Assert.Equal(2, zeilen.Count);
        Assert.Equal(1, zeilen.Count(z => z.QuerySelectorAll("td").Last().TextContent.Trim() == "Ja"));

        var traegt = zeilen.First(z => z.QuerySelectorAll("td").Last().TextContent.Trim() == "Ja");
        Assert.Contains("Kessel A", traegt.TextContent);
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
        _filterstand.Setzen(Katalogfilterprofil.SpBrennstoff, "Heizöl");
        ersterAufbau.Render();
        Assert.Single(Katalogzeilen(ersterAufbau));

        // Der Dialog geht zu und wieder auf - derselbe Stand, dieselbe Sicht.
        var zweiterAufbau = Aufbauen();

        Assert.Single(Katalogzeilen(zweiterAufbau));
        Assert.Equal("1 von 2 Sätzen", zweiterAufbau.Find(".epos-katalog-treffer").TextContent);
        Assert.Single(zweiterAufbau.FindAll(".epos-katalog-ruecksetzer"));
    }
}
