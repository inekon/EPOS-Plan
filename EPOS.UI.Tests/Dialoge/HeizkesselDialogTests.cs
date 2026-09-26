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
public class HeizkesselDialogTests : EposBunitContext
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
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static ErzeugerZeile Zeile(int schluessel, string name, int geraetId, int carrier = 5)
        => new() { Schluessel = schluessel, Bezeichner = name, GeraetId = geraetId,
                   CarrierId = carrier, Vorlauf = 70, Ruecklauf = 50 };

    /// <summary>
    /// Der Detailblock, wie ihn <c>HeizkesselHuelle.DetailZu</c> baut: Brennstoff Typ,
    /// Leistung und der Schalter „Brennwertkessel". Ein Feld „Investitionskosten" steht
    /// nicht darin — gepflegt wird der Preis im Aufklapper „Alle Daten anzeigen".
    /// </summary>
    private static ErzeugerDetail Detail(string name) => new(
        name, "Beschreibung",
        new[] { ("Brennstoff Typ:", "Erdgas E"), ("Leistung [kW]:", "120,00") },
        ("Brennwertkessel", true));

    private IRenderedComponent<HeizkesselDialog> Aufbauen(
        List<ErzeugerZeile>? zeilen = null,
        Func<int, TraegerVorbereitung>? vorbereiten = null,
        Func<int, EnergietraegerVarianteErgebnis, AufnahmeErgebnis>? aufnehmen = null,
        Action<ErzeugerZeile>? entfernen = null,
        Action<ErzeugerZeile, int>? traegerWechseln = null,
        Action<ErzeugerZeile>? uebernehmen = null,
        Func<int, bool>? katalogLoeschen = null,
        Func<string, IReadOnlyDictionary<string, object>>? editorGaben = null,
        bool wizard = false,
        Action<bool>? geschlossen = null,
        Func<ErzeugerZeile?, bool, Task>? kostenOeffnen = null,
        Func<ErzeugerZeile?, Task>? energiekosten = null,
        Func<string, IReadOnlyList<BrowserFeldwert>?>? katalogfelder = null,
        Func<string, IReadOnlyList<BrowserFeldwert>, KatalogSpeicherErgebnis>?
            katalogfelderSpeichern = null,
        Func<IReadOnlyList<Katalogfilterzeile>>? katalogzeilen = null)
    {
        return Render<HeizkesselDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<ErzeugerZeile> { Zeile(1, "Kessel A", 100) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, katalogzeilen ?? Katalogzeilen)
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
            .Add(x => x.EditorGaben, editorGaben)
            .Add(x => x.TraegerGaben, _ => new Dictionary<string, object>
            {
                ["Energietraeger"] = new[] { (3, "Erdgas E") }
            })
            .Add(x => x.Wizard, wizard)
            .Add(x => x.KostenOeffnen, kostenOeffnen)
            .Add(x => x.EnergiekostenOeffnen, energiekosten)
            .Add(x => x.Katalogfelder, katalogfelder)
            .Add(x => x.KatalogfelderSpeichern, katalogfelderSpeichern)
            .Add(x => x.Geschlossen, ok => geschlossen?.Invoke(ok)));
    }

    /// <summary>Der Auswahlpfad der Kostenknöpfe — drei Stück, in dieser Reihenfolge.</summary>
    private const string KOSTENKNOEPFE = ".epos-kostenleiste button.epos-knopf";

    /// <summary>
    /// Die Felder des Aufklappers — ein Ausschnitt des Heizkesselprofils: ein Text,
    /// eine Zahl und der Schalter „Brennwertkessel".
    /// </summary>
    private static List<BrowserFeldwert> Felder() => new()
    {
        new BrowserFeldwert
        {
            Schluessel = KatalogBrowserProfil.FeldFirma,
            Bezeichnung = "Hersteller:",
            Art = BrowserFeldArt.Text,
            Editierbar = true,
            Wert = "Musterwerk"
        },
        new BrowserFeldwert
        {
            Schluessel = KatalogBrowserProfil.FeldInvestitionskosten,
            Bezeichnung = "Investitionskosten:",
            Einheit = "€",
            Art = BrowserFeldArt.Zahl,
            Editierbar = true,
            Wert = "12000"
        },
        new BrowserFeldwert
        {
            Schluessel = KatalogBrowserProfil.FeldBrennwert,
            Bezeichnung = "Brennwertkessel:",
            Art = BrowserFeldArt.Schalter,
            Editierbar = true,
            Wert = "1"
        }
    };

    /// <summary>Wählt die erste Katalogzeile — erst dann gibt es einen Aufklapper.</summary>
    private static void KatalogsatzWaehlen(IRenderedComponent<HeizkesselDialog> cut)
        => cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();

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
    public void Der_Detailblock_zeigt_die_sechs_Felder_der_Gruppe_Modul()
    {
        var cut = Aufbauen();

        // Name, Brennstoff Typ, Leistung, Beschreibung (mehrzeilig), Brennwertkessel
        // (Schalter), Brennstoff Variante (Auswahl). Ein nur lesbares Feld
        // „Investitionskosten" steht nicht mehr darunter (Anwenderentscheid
        // 21.09.2026) — gepflegt wird der Preis im Aufklapper „Alle Daten anzeigen".
        var gruppe = cut.Find(".epos-gruppenkopf-koerper");
        Assert.Equal("Modul", cut.Find(".epos-gruppenkopf-titel").TextContent);
        Assert.Equal(3, gruppe.QuerySelectorAll("input[type=text][readonly]").Length);
        Assert.Single(gruppe.QuerySelectorAll("textarea"));
        Assert.Single(gruppe.QuerySelectorAll("input[type=checkbox]"));
    }

    /// <summary>
    /// <b>Es gibt keinen Administrationsknopf mehr</b> (Anwenderentscheid 15.09.2026:
    /// „nicht noetig, da schon unter Bearbeiten vorhanden"). Der Fall haelt die
    /// Abwesenheit fest - sonst kaeme der Knopf bei der naechsten Huellenpflege
    /// unbemerkt zurueck.
    /// </summary>
    [Fact]
    public void Es_gibt_keinen_Administrationsknopf_mehr()
    {
        var cut = Aufbauen();

        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent == "Administration...");
    }

    [Fact]
    public void Im_Assistenten_fehlen_OK_Abbrechen_und_die_Kostenleiste()
    {
        var cut = Aufbauen(wizard: true);

        Assert.Empty(cut.FindAll(".epos-status"));          // SpeichernLeiste
        Assert.Empty(cut.FindAll(".epos-kostenleiste"));
    }

    // =================================================================================
    // Die Kostenleiste
    // =================================================================================

    /// <summary>
    /// Der Leerlauf, den es zu beheben galt: Ohne Wege der Hülle steht KEIN Knopf —
    /// ein gezeichneter Knopf ohne Wirkung wäre eine Behauptung, die nicht stimmt.
    /// </summary>
    [Fact]
    public void Ohne_Wege_bleibt_die_Kostenleiste_leer()
    {
        var cut = Aufbauen();

        Assert.Empty(cut.FindAll(KOSTENKNOEPFE));
    }

    [Fact]
    public void Mit_den_Wegen_stehen_die_drei_Knoepfe()
    {
        var cut = Aufbauen(kostenOeffnen: (_, _) => Task.CompletedTask,
                           energiekosten: _ => Task.CompletedTask);

        var knoepfe = cut.FindAll(KOSTENKNOEPFE);
        Assert.Equal(3, knoepfe.Count);
        Assert.Equal("Investitionskosten…", knoepfe[0].TextContent);
        Assert.Equal("Betriebskosten…", knoepfe[1].TextContent);
        Assert.Equal("Energiekosten…", knoepfe[2].TextContent);
    }

    /// <summary>
    /// Beide Kostenknöpfe führen in dieselbe Maske und unterscheiden sich nur im
    /// Schalter <c>betrieb</c> — und beide nehmen die GEWÄHLTE Zeile mit.
    /// </summary>
    [Fact]
    public void Invest_und_Betrieb_nehmen_die_gewaehlte_Zeile_mit()
    {
        var gerufen = new List<(ErzeugerZeile? Zeile, bool Betrieb)>();
        var cut = Aufbauen(
            kostenOeffnen: (z, b) => { gerufen.Add((z, b)); return Task.CompletedTask; },
            energiekosten: _ => Task.CompletedTask);

        cut.FindAll(KOSTENKNOEPFE)[0].Click();
        cut.FindAll(KOSTENKNOEPFE)[1].Click();

        Assert.Equal(2, gerufen.Count);
        Assert.False(gerufen[0].Betrieb);
        Assert.True(gerufen[1].Betrieb);
        Assert.Equal(1, gerufen[0].Zeile!.Schluessel);
        Assert.Equal(100, gerufen[0].Zeile!.GeraetId);
    }

    [Fact]
    public void Energiekosten_nimmt_Traeger_und_Geraet_der_gewaehlten_Zeile_mit()
    {
        ErzeugerZeile? mitgegeben = null;
        int gerufen = 0;
        var cut = Aufbauen(
            kostenOeffnen: (_, _) => Task.CompletedTask,
            energiekosten: z => { gerufen++; mitgegeben = z; return Task.CompletedTask; });

        cut.FindAll(KOSTENKNOEPFE)[2].Click();

        Assert.Equal(1, gerufen);
        Assert.Equal(5, mitgegeben!.CarrierId);
        Assert.Equal(100, mitgegeben!.GeraetId);
    }

    /// <summary>
    /// Ohne gewählte Projektzeile geht KEINE Zeile mit — die Verwaltung zeigt dann
    /// die Komponente ohne Einengung (Muster der Kostenseite, Auftrag 268).
    /// </summary>
    [Fact]
    public void Ohne_Projektwahl_geht_keine_Zeile_mit()
    {
        ErzeugerZeile? mitgegeben = Zeile(99, "Platzhalter", 999);
        ErzeugerZeile? beiEnergie = Zeile(99, "Platzhalter", 999);
        var cut = Aufbauen(
            kostenOeffnen: (z, _) => { mitgegeben = z; return Task.CompletedTask; },
            energiekosten: z => { beiEnergie = z; return Task.CompletedTask; });

        // Eine Katalogzeile loescht die Projektwahl (listBox_Kessel_DB_SelectedIndexChanged).
        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        Assert.Null(cut.Instance.Projektzeile);

        cut.FindAll(KOSTENKNOEPFE)[0].Click();
        cut.FindAll(KOSTENKNOEPFE)[2].Click();

        Assert.Null(mitgegeben);
        Assert.Null(beiEnergie);
    }

    /// <summary>
    /// Nur einer der beiden Wege belegt: Dann steht auch nur sein Knopf
    /// (die Regel der <c>KostenKnoepfeLeiste</c>, hier über den Dialog geprüft).
    /// </summary>
    [Fact]
    public void Ein_fehlender_Weg_nimmt_seine_Knoepfe_mit()
    {
        var cut = Aufbauen(energiekosten: _ => Task.CompletedTask);

        var knoepfe = cut.FindAll(KOSTENKNOEPFE);
        Assert.Single(knoepfe);
        Assert.Equal("Energiekosten…", knoepfe[0].TextContent);
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
        Knopf(cut, "Löschen").Click();

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
        Knopf(cut, "Löschen").Click();
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
        Knopf(cut, "Bearbeiten...").Click();

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

    /// <summary>Das ✕ der Trägerwahl wirkt wie deren Esc: nichts wird aufgenommen.</summary>
    [Fact]
    public void Das_Kreuz_der_Traegerwahl_bricht_sie_ab()
    {
        bool aufgenommen = false;
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Kessel A", 100) };
        var cut = Aufbauen(zeilen, aufnehmen: (_, _) =>
        {
            aufgenommen = true;
            return new AufnahmeErgebnis(Zeile(9, "Kessel B", 200));
        });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-zweispalten-uebernahme button")[0].Click();
        Assert.True(cut.Instance.Traegerwahl);

        cut.Find(".epos-ueberlagerung-zu").Click();

        Assert.False(cut.Instance.Traegerwahl);
        Assert.False(aufgenommen);
        Assert.Single(zeilen);
    }

    /// <summary>Das ✕ über dem Katalogeditor schließt ihn — derselbe Weg wie Esc.</summary>
    [Fact]
    public void Das_Kreuz_des_Katalogeditors_bricht_ihn_ab()
    {
        var cut = Aufbauen(editorGaben: _ =>
            new Dictionary<string, object> { ["Daten"] = new HeizkesselKatalogDaten() });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        Knopf(cut, "Bearbeiten...").Click();
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));

        cut.Find(".epos-ueberlagerung-zu").Click();

        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
    }

    /// <summary>
    /// Die Trägerwahl steht in einer BETITELTEN Überlagerung — die trägt Titel und ✕,
    /// das eingebettete Blatt zeigt beides nicht mehr (<c>TitelText=""</c> hinter dem
    /// Parametersatz). Befund des Anwenders: „Doppeltes Kreuz dürfen nicht sein!"
    /// </summary>
    [Fact]
    public void Die_Ueberlagerung_Traegerwahl_zeigt_nur_ein_Kreuz()
    {
        var cut = Aufbauen();

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-zweispalten-uebernahme button")[0].Click();

        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
    }

    /// <summary>Dasselbe über dem Katalogeditor.</summary>
    [Fact]
    public void Die_Ueberlagerung_Katalogeditor_zeigt_nur_ein_Kreuz()
    {
        var cut = Aufbauen(editorGaben: _ =>
            new Dictionary<string, object> { ["Daten"] = new HeizkesselKatalogDaten() });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        Knopf(cut, "Bearbeiten...").Click();

        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
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

    // =================================================================================
    //  Der Aufklapper „Alle Daten anzeigen" — Anwenderentscheid 15.09.2026
    // =================================================================================
    //
    // „Es soll in diesem Bereich optional alle technischen Daten angezeigt und
    // bearbeitet werden koennen." Er ist der Ersatz fuer die Kosten- und
    // Emissionsgruppen, die aus dem Katalogeditor verschwunden sind, und fuer den
    // entfallenen Knopf "Administration..."; das Raster ist der Baustein
    // Katalogfelder, den auch der Katalogbrowser benutzt. Der Heizkesselkatalog
    // kennt KEINEN Schreibschutz - deshalb fehlen hier die zwei Schutzfragefaelle
    // des BHKW.

    /// <summary>Ohne den Weg zu den Feldern gibt es den Aufklapper gar nicht.</summary>
    [Fact]
    public void Ohne_Katalogfelder_gibt_es_keinen_Aufklapper()
    {
        var cut = Aufbauen();

        KatalogsatzWaehlen(cut);

        Assert.Empty(cut.FindAll(".epos-modulparameter"));
    }

    /// <summary>
    /// <b>Geholt wird mit der WAHL des Satzes</b> (Anwenderentscheid 16.09.2026: „Unter
    /// Bearbeiten sollen alle Parameter angezeigt werden und bearbeitbar sein"). Der
    /// Aufklapper steht offen, sobald ein Katalogsatz gewählt ist — genau EINE Abfrage
    /// je Satz, nicht eine je Klick.
    /// </summary>
    [Fact]
    public void Der_Aufklapper_holt_die_Felder_mit_der_Wahl_des_Satzes()
    {
        int rufe = 0;
        var cut = Aufbauen(katalogfelder: _ => { rufe++; return Felder(); });

        // Ohne gewaehlten Katalogsatz (die erste PROJEKTzeile steht vorgewaehlt) gibt es
        // den Block gar nicht - und keine Abfrage.
        Assert.Equal(0, rufe);
        Assert.Empty(cut.FindAll(".epos-modulparameter"));

        KatalogsatzWaehlen(cut);

        Assert.Equal(1, rufe);
        Assert.True(cut.Instance.ParameterOffen);
        Assert.Equal("true", cut.Find(".epos-modulparameter-knopf").GetAttribute("aria-expanded"));
        Assert.NotEmpty(cut.Find(".epos-modulparameter").QuerySelectorAll(".epos-feld"));
    }

    /// <summary>
    /// <b>Zuklappen geht weiterhin</b> — der Knopf bleibt, was er war, nur seine Vorgabe
    /// hat sich gedreht.
    /// </summary>
    [Fact]
    public void Der_Aufklapper_laesst_sich_weiterhin_zuklappen()
    {
        var cut = Aufbauen(katalogfelder: _ => Felder());

        KatalogsatzWaehlen(cut);

        cut.Find(".epos-modulparameter-knopf").Click();

        Assert.False(cut.Instance.ParameterOffen);
        Assert.Equal("false", cut.Find(".epos-modulparameter-knopf").GetAttribute("aria-expanded"));
        Assert.Empty(cut.Find(".epos-modulparameter").QuerySelectorAll(".epos-feld"));

        cut.Find(".epos-modulparameter-knopf").Click();

        Assert.True(cut.Instance.ParameterOffen);
        Assert.NotEmpty(cut.Find(".epos-modulparameter").QuerySelectorAll(".epos-feld"));
    }

    /// <summary>
    /// <b>Ein Satzwechsel bei offenem Aufklapper holt die Felder NEU.</b> Sonst stünde
    /// der Feldsatz des vorigen Kessels da, und „Speichern" schriebe ihn unter dem
    /// Namen des jetzt gewählten zurück.
    /// </summary>
    [Fact]
    public void Ein_Satzwechsel_zieht_die_Felder_des_neuen_Satzes_nach()
    {
        var gefragt = new List<string>();
        var cut = Aufbauen(katalogfelder: n => { gefragt.Add(n); return Felder(); });

        KatalogsatzWaehlen(cut);
        Assert.Equal(new[] { "Kessel A" }, gefragt);

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[1].Click();

        Assert.Equal(new[] { "Kessel A", "Kessel B" }, gefragt);
        Assert.True(cut.Instance.ParameterOffen);
    }

    /// <summary>
    /// <b>Ein Wechsel auf eine PROJEKTzeile räumt den Aufklapper leer.</b> Der
    /// Aufklapper gehört dem Katalogsatz; bliebe sein Feldsatz stehen, zeigte er Werte,
    /// zu denen es keine gewählte Katalogzeile mehr gibt.
    /// </summary>
    [Fact]
    public void Ein_Wechsel_auf_die_Projektzeile_raeumt_den_Aufklapper()
    {
        var cut = Aufbauen(katalogfelder: _ => Felder());

        KatalogsatzWaehlen(cut);
        Assert.NotEmpty(cut.Find(".epos-modulparameter").QuerySelectorAll(".epos-feld"));

        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[0].Click();

        // Ohne Katalogzeile zeichnet der Block gar nicht mehr - der Zustand "offen"
        // gehoert der Dialogsitzung und ueberlebt den Wechsel trotzdem.
        Assert.Empty(cut.FindAll(".epos-modulparameter"));
        Assert.True(cut.Instance.ParameterOffen);
    }

    /// <summary>
    /// <b>Ohne Speicherweg ist der Aufklapper reine Anzeige</b> — kein Knopf, jedes Feld
    /// nur lesbar. Das entscheidet der WIRT über den Delegaten, nicht der Dialog.
    /// </summary>
    [Fact]
    public void Ohne_Speicherweg_ist_der_Aufklapper_nur_Anzeige()
    {
        var cut = Aufbauen(katalogfelder: _ => Felder());

        KatalogsatzWaehlen(cut);

        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Speichern");
        Assert.All(cut.Find(".epos-modulparameter").QuerySelectorAll("input[type=text]"),
                   e => Assert.True(e.HasAttribute("readonly")));
    }

    /// <summary>
    /// <b>„Speichern" reicht die GEÄNDERTEN Felder durch</b> — und bleibt WEICH gesperrt
    /// (<c>aria-disabled</c>), solange nichts geändert wurde. Der Erfolg steht als
    /// „Gespeichert um …" AM Knopf, nicht als Band im Dialogkopf.
    /// </summary>
    [Fact]
    public void Speichern_im_Aufklapper_reicht_die_geaenderten_Felder_durch()
    {
        string? name = null;
        IReadOnlyList<BrowserFeldwert>? geschrieben = null;

        var cut = Aufbauen(
            katalogfelder: _ => Felder(),
            katalogfelderSpeichern: (n, f) =>
            {
                name = n; geschrieben = f;
                return new KatalogSpeicherErgebnis(true, "Datensatz gespeichert", n);
            });

        KatalogsatzWaehlen(cut);

        Assert.Equal("true", Knopf(cut, "Speichern").GetAttribute("aria-disabled"));
        Assert.False(Knopf(cut, "Speichern").HasAttribute("disabled"));

        cut.Find(".epos-modulparameter input[inputmode=decimal]").Input("15000");
        Assert.False(Knopf(cut, "Speichern").HasAttribute("aria-disabled"));

        Knopf(cut, "Speichern").Click();

        Assert.Equal("Kessel A", name);
        Assert.NotNull(geschrieben);
        Assert.Equal("15000",
            geschrieben!.First(f => f.Schluessel == KatalogBrowserProfil.FeldInvestitionskosten).Wert);
        Assert.Equal("", cut.Instance.Meldung);
        Assert.StartsWith("Gespeichert um ", Vermerk(cut).TextContent);
    }

    /// <summary>
    /// <b>Der Vermerk am Knopf</b> fällt mit der nächsten Eingabe weg; ohne Änderung ist
    /// Speichern weich gesperrt, und ein Klick nennt den Grund, statt zu schreiben.
    /// </summary>
    [Fact]
    public void Vermerk_am_Knopf_faellt_mit_der_Eingabe_und_ohne_Aenderung_nennt_der_Klick_den_Grund()
    {
        int schreibvorgaenge = 0;
        var cut = Aufbauen(
            katalogfelder: _ => Felder(),
            katalogfelderSpeichern: (n, _) => { schreibvorgaenge++; return new KatalogSpeicherErgebnis(true, "Datensatz gespeichert", n); });

        KatalogsatzWaehlen(cut);
        cut.Find(".epos-modulparameter input[inputmode=decimal]").Input("15000");
        Knopf(cut, "Speichern").Click();
        Assert.Equal(1, schreibvorgaenge);
        Assert.Equal("true", Knopf(cut, "Speichern").GetAttribute("aria-disabled"));

        // Ein zweiter Klick schreibt nicht, er nennt den Grund am Knopf.
        Knopf(cut, "Speichern").Click();
        Assert.Equal(1, schreibvorgaenge);
        Assert.Equal("Keine Änderung — es gibt nichts zu speichern.", Vermerk(cut).TextContent);

        // Die naechste Eingabe nimmt den Vermerk zurueck und hebt die Sperre auf.
        cut.Find(".epos-modulparameter input[inputmode=decimal]").Input("1");
        Assert.Empty(cut.FindAll(".epos-modulparameter .epos-speichervermerk [role=status]"));
        Assert.False(Knopf(cut, "Speichern").HasAttribute("aria-disabled"));
    }

    /// <summary>Die Rückmeldung neben dem Knopf „Speichern" des Aufklappers.</summary>
    private static AngleSharp.Dom.IElement Vermerk(Bunit.IRenderedComponent<HeizkesselDialog> cut)
        => cut.Find(".epos-modulparameter .epos-speichervermerk [role=status]");

    /// <summary>
    /// <b>Ein abgelehnter Schreibvorgang lässt den Stand stehen</b> und meldet den
    /// Grund des Katalogs — der Anwender soll ihn verbessern können.
    /// </summary>
    [Fact]
    public void Ein_abgelehntes_Speichern_meldet_den_Grund_und_haelt_die_Eingabe()
    {
        var cut = Aufbauen(
            katalogfelder: _ => Felder(),
            katalogfelderSpeichern: (_, _) =>
                new KatalogSpeicherErgebnis(false, "„Investitionskosten“ darf nicht negativ sein.", ""));

        KatalogsatzWaehlen(cut);
        cut.Find(".epos-modulparameter input[inputmode=decimal]").Input("15000");
        Knopf(cut, "Speichern").Click();

        Assert.Contains("darf nicht negativ sein", cut.Instance.Meldung);
        Assert.Equal("15000", cut.Find(".epos-modulparameter input[inputmode=decimal]")
                                 .GetAttribute("value"));

        // Der Grund steht auch rot AM Knopf - das Band oben bleibt fuer den Fehler.
        Assert.Contains("darf nicht negativ sein", Vermerk(cut).TextContent);
        Assert.Contains("epos-status--fehler", Vermerk(cut).ClassName);
    }

    /// <summary>
    /// <b>Der Aufklapper trägt den vollen Feldbestand des Profils</b>, Schalter
    /// eingeschlossen — er ist die einzige Pflegestelle der Spalten, die aus dem
    /// Katalogeditor gefallen sind.
    /// </summary>
    [Fact]
    public void Der_Aufklapper_zeichnet_jede_Feldart_des_Profils()
    {
        var cut = Aufbauen(katalogfelder: _ => Felder(),
                           katalogfelderSpeichern: (n, _) =>
                               new KatalogSpeicherErgebnis(true, "ok", n));

        KatalogsatzWaehlen(cut);

        var block = cut.Find(".epos-modulparameter");
        Assert.Equal(3, block.QuerySelectorAll(".epos-feld, .epos-schalter").Length);
        Assert.NotEmpty(block.QuerySelectorAll("input[type=text]"));
        Assert.NotEmpty(block.QuerySelectorAll("input[inputmode=decimal]"));
        Assert.NotEmpty(block.QuerySelectorAll("input[type=checkbox]"));
    }

    /// <summary>
    /// <b>Aufgeklappt ist die Vorgabe</b> (Anwenderentscheid 16.09.2026) — mit gewähltem
    /// Satz stehen die Felder da, ohne gewählten Satz gibt es den Block gar nicht.
    /// </summary>
    [Fact]
    public void Aufgeklappt_ist_die_Vorgabe_und_zeigt_die_Felder()
    {
        var cut = Aufbauen(katalogfelder: _ => Felder());

        Assert.Empty(cut.FindAll(".epos-modulparameter"));

        KatalogsatzWaehlen(cut);

        Assert.True(cut.Instance.ParameterOffen);
        Assert.Equal("true", cut.Find(".epos-modulparameter-knopf").GetAttribute("aria-expanded"));
        Assert.Equal("Alle Daten anzeigen",
                     cut.Find(".epos-modulparameter-knopf").QuerySelectorAll("span")[1].TextContent);
        Assert.Equal(3, cut.Find(".epos-modulparameter")
                           .QuerySelectorAll(".epos-feld, .epos-schalter").Length);
    }

    // =================================================================================
    //  Die Knopfzeile im Modulbereich — Anwenderentscheid 16.09.2026
    // =================================================================================
    //
    // „Der Bearbeiten-Button soll weiter oben (z. B. unter der blauen Zeile Modul)
    // stehen, so dass er besser sichtbar ist."

    /// <summary>
    /// <b>Die Knopfzeile steht als ERSTES unter dem Modulkopf</b>: links die
    /// Kostenknöpfe, rechts „Bearbeiten…", dazwischen der Füller. Danach erst die
    /// Felder, danach der Aufklapper.
    /// </summary>
    [Fact]
    public void Die_Knopfzeile_steht_unmittelbar_unter_dem_Modulkopf()
    {
        var cut = Aufbauen(kostenOeffnen: (_, _) => Task.CompletedTask,
                           energiekosten: _ => Task.CompletedTask,
                           katalogfelder: _ => Felder(),
                           editorGaben: _ => new Dictionary<string, object>());

        KatalogsatzWaehlen(cut);

        var kinder = cut.Find(".epos-gruppenkopf-koerper").Children.ToList();

        // Die Knopfzeile ist das erste Kind - vor Hilfeknopf, Feldern und Aufklapper.
        Assert.Contains("epos-leiste", kinder[0].ClassList);
        int raster = kinder.FindIndex(k => k.ClassList.Contains("epos-formularraster"));
        int parameter = kinder.FindIndex(k => k.ClassList.Contains("epos-modulparameter"));
        Assert.True(0 < raster && raster < parameter);

        // Links die Kostenknoepfe, dann der Fueller, rechts "Bearbeiten...".
        var teile = kinder[0].Children.ToList();
        Assert.Contains("epos-kostenleiste", teile[0].ClassList);
        Assert.Contains("epos-leiste-fueller", teile[1].ClassList);
        Assert.Equal("Bearbeiten...", teile[2].TextContent.Trim());
    }

    /// <summary>
    /// <b>„Bearbeiten…" bleibt gesperrt, solange kein Katalogsatz gewählt ist</b> — die
    /// Sperre ist mit dem Knopf nach oben gewandert, nicht verschwunden.
    /// </summary>
    [Fact]
    public void Bearbeiten_bleibt_in_der_Knopfzeile_ohne_Katalogsatz_gesperrt()
    {
        var cut = Aufbauen(editorGaben: _ => new Dictionary<string, object>());

        Assert.True(Knopf(cut, "Bearbeiten...").HasAttribute("disabled"));

        KatalogsatzWaehlen(cut);

        Assert.False(Knopf(cut, "Bearbeiten...").HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>„Brennstoff Variante" steht unmittelbar unter „Brennstoff Typ"</b>
    /// (Anwenderentscheid 16.09.2026) — beide in EINEM Block über die volle
    /// Rasterbreite, sonst stellte das zweispaltige Raster sie nebeneinander.
    /// </summary>
    [Fact]
    public void Brennstoff_Variante_steht_unmittelbar_unter_Brennstoff_Typ()
    {
        var cut = Aufbauen();

        var paar = cut.Find(".epos-formularraster > div.epos-feld--breit");
        var felder = paar.QuerySelectorAll(".epos-feld");

        Assert.Equal(2, felder.Length);
        Assert.Equal("Brennstoff Typ:", felder[0].QuerySelector(".epos-feld-text")!.TextContent);
        Assert.Equal("Brennstoff Variante:", felder[1].QuerySelector(".epos-feld-text")!.TextContent);
        Assert.NotNull(felder[1].QuerySelector("select"));

        // Und sie steht NICHT mehr ein zweites Mal weiter unten.
        Assert.Single(cut.FindAll(".epos-formularraster select"));
    }

    /// <summary>
    /// <b>Ohne Brennstoff-Typ-Feld bleibt die Trägerwahl stehen</b>, wo sie war — ein
    /// Wirt ohne Detailweg verliert sein Bedienelement nicht still.
    /// </summary>
    [Fact]
    public void Ohne_Brennstofftyp_im_Detail_bleibt_die_Traegerwahl_an_ihrer_Stelle()
    {
        var cut = Render<HeizkesselDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Kessel A", 100) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Varianten, _ => new[] { (5, "Erdgas E Variante") }));

        // Kein ProjektDetail: Der Detailblock ist leer, ein Paarblock entsteht nicht.
        Assert.Empty(cut.FindAll(".epos-formularraster > div.epos-feld--breit"));
        Assert.Single(cut.FindAll(".epos-formularraster select"));
        Assert.Contains("Brennstoff Variante:",
                        cut.FindAll(".epos-feld-text").Select(e => e.TextContent));
    }

    /// <summary>
    /// Der Knopf mit DIESER Beschriftung - gleich, in welcher Leiste er steht.
    /// </summary>
    /// <remarks>
    /// <b>Ueber den TEXT und nicht ueber den Index</b> (15.09.2026). Die Faelle
    /// griffen bis dahin mit <c>[0]</c> und <c>[1]</c> in die Knopfleiste der rechten
    /// Spalte. Als "Bearbeiten..." in den Modulbereich wanderte und
    /// "Administration..." entfiel, traf jeder dieser Indizes etwas anderes - oder
    /// nichts. Der Text sagt, was gemeint ist, und haelt den naechsten Umbau aus.
    /// </remarks>
    private static AngleSharp.Dom.IElement Knopf(
        Bunit.IRenderedComponent<HeizkesselDialog> cut, string beschriftung)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == beschriftung);

    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F1)
    // =====================================================================

    /// <summary>
    /// <b>Der Anlassfall der Welle:</b> „Setze die Vorlauftemperatur aller Heizkessel
    /// auf 55 °C". Nach dem Zeichnen steht die Maske an der <c>KiMaskenbruecke</c>,
    /// die Brücke liest den Vorlauf der gewählten Zeile und setzt ihn — und der Wert
    /// steht danach in der Zeile, die der Dialog führt.
    /// </summary>
    /// <remarks>
    /// <b>Monotone Aussage</b> (Muster <c>KiMaskenhakenTests</c>): Geprüft wird, was
    /// nach dem Zeichnen DA ist — nicht, dass nichts anderes da wäre. Die Brücke ist
    /// prozessweiter Zustand.
    /// </remarks>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_setzt_den_Vorlauf()
    {
        ErzeugerZeile zeile = Zeile(1, "Kessel A", 100);
        Aufbauen(zeilen: new List<ErzeugerZeile> { zeile });

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.HEIZKESSEL_PROJEKT));

        KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.HEIZKESSEL_PROJEKT, "vorlauf");
        Assert.NotNull(zugang);
        Assert.Equal(70, zugang.Lesen());

        Assert.True(zugang.Setzbar);
        zugang.Setzen(55);

        Assert.Equal(55, zeile.Vorlauf);
    }

    /// <summary>
    /// Der Name der Anlage ist ANZEIGE und kein Eingabefeld — er wird gelesen und
    /// erklärt, aber nie gesetzt (<c>nurLesen</c> im Katalog).
    /// </summary>
    [Fact]
    public void Der_Anlagenname_ist_fuer_den_Assistenten_nur_lesbar()
    {
        Aufbauen(zeilen: new List<ErzeugerZeile> { Zeile(1, "Kessel A", 100) });

        KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.HEIZKESSEL_PROJEKT, "anlage");

        Assert.Equal("Kessel A", zugang.Lesen());
        Assert.False(zugang.Setzbar);
    }

    // =================================================================================
    // „Alle Daten" für den Assistenten (Welle #458, Stufe 2)
    // =================================================================================

    /// <summary>
    /// <b>Der ZEUGE der Feldtafel „Alle Daten".</b> Der Assistent liest und setzt die
    /// Felder des gewählten KATALOGsatzes in der lebenden Liste des Aufklappers — der
    /// Aufklapper zeigt den neuen Wert —, und <c>dialog_speichern</c> geht den Weg des
    /// Knopfes „Speichern" im Aufklapper, mit genau diesen Feldern.
    /// </summary>
    [Fact]
    public async Task Der_Assistent_setzt_Alle_Daten_und_speichert_ueber_den_Aufklapper()
    {
        string? name = null;
        IReadOnlyList<BrowserFeldwert>? geschrieben = null;

        var cut = Aufbauen(
            katalogfelder: _ => Felder(),
            katalogfelderSpeichern: (n, f) =>
            {
                name = n; geschrieben = f;
                return new KatalogSpeicherErgebnis(true, "Datensatz gespeichert", n);
            });

        KatalogsatzWaehlen(cut);

        KiFeldzugang invest = KiMaskenbruecke.Feldzugang(KiMaskennamen.HEIZKESSEL_PROJEKT,
                                                         "katalog_investitionskosten");
        Assert.NotNull(invest);
        Assert.Equal(12000.0, invest.Lesen());
        Assert.True(invest.Setzbar);

        KiFeldumsetzung u = KiFeldwandler.Wandle(invest, "15000");
        Assert.True(u.Ok, u.Grund);
        invest.Setzen(u.Wert);
        cut.Render();

        Assert.Equal("15000", cut.Find(".epos-modulparameter input[inputmode=decimal]")
                                 .GetAttribute("value"));

        KiKern.KiErgebnis ergebnis =
            await KiMaskenbruecke.Haken(KiMaskennamen.HEIZKESSEL_PROJEKT).Speichern!();
        Assert.True(ergebnis.Erfolg, ergebnis.Text);
        Assert.Equal("Kessel A", name);
        Assert.Equal("15000",
            geschrieben!.First(f => f.Schluessel == KatalogBrowserProfil.FeldInvestitionskosten).Wert);
    }

    /// <summary>
    /// Ein AUSLIEFERUNGSSATZ im Aufklapper lehnt das Setzen benannt ab — mit dem Weg
    /// „Duplizieren…", demselben Grund wie in der Verwaltung.
    /// </summary>
    [Fact]
    public void Ein_Auslieferungssatz_lehnt_Alle_Daten_mit_dem_Weg_Duplizieren_ab()
    {
        var cut = Aufbauen(
            katalogfelder: _ => Felder(),
            katalogfelderSpeichern: (n, _) => new KatalogSpeicherErgebnis(true, "", n),
            katalogzeilen: () =>
            {
                IReadOnlyList<Katalogfilterzeile> zeilen = Katalogzeilen();
                foreach (Katalogfilterzeile z in zeilen) z.Geschuetzt = true;
                return zeilen;
            });

        KatalogsatzWaehlen(cut);

        KiFeldzugang firma = KiMaskenbruecke.Feldzugang(KiMaskennamen.HEIZKESSEL_PROJEKT, "katalog_firma");
        Assert.NotNull(firma);

        var fehler = Assert.Throws<InvalidOperationException>(() => firma.Setzen("Anderes Werk"));
        Assert.Equal(Resource.ADM_SPEICHERN_GESPERRT, fehler.Message);
    }

    /// <summary>
    /// Zugeklappt steht „Alle Daten" nicht vor dem Anwender — der Assistent liest dann
    /// nichts und lehnt das Setzen benannt ab, statt still zu schreiben.
    /// </summary>
    [Fact]
    public void Zugeklappt_liest_der_Assistent_nichts_und_setzt_nichts()
    {
        var cut = Aufbauen(katalogfelder: _ => Felder(),
                           katalogfelderSpeichern: (n, _) => new KatalogSpeicherErgebnis(true, "", n));

        KatalogsatzWaehlen(cut);
        cut.Find(".epos-modulparameter-knopf").Click();

        KiFeldzugang firma = KiMaskenbruecke.Feldzugang(KiMaskennamen.HEIZKESSEL_PROJEKT, "katalog_firma");
        Assert.Null(firma.Lesen());

        var fehler = Assert.Throws<InvalidOperationException>(() => firma.Setzen("Anderes Werk"));
        Assert.Equal(Resource.KI_DLG_ALLE_DATEN_ZU, fehler.Message);
    }

    // =================================================================================
    // Senken der Anlage (Anwenderentscheid 23.09.2026)
    // =================================================================================

    /// <summary>
    /// Die PROJEKTzeile zeigt ihre Senken — die Zeile „Senken: …" kommt fertig
    /// formuliert von der Hülle (Kern: <c>Senkenvorbelegung.Anzeigezeile</c>) und steht als
    /// Herleitungszeile im Detailraster.
    /// </summary>
    [Fact]
    public void Die_Projektzeile_zeigt_ihre_Senken()
    {
        ErzeugerZeile zeile = Zeile(1, "Kessel A", 100);
        zeile.Senken = "Senken: Heizkreis (Heizung + Warmwasser); Prozesswärme";
        var cut = Aufbauen(zeilen: new List<ErzeugerZeile> { zeile });

        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[0].Click();

        Assert.Contains(cut.FindAll(".epos-formularraster .epos-herleitung-text"),
                        e => e.TextContent == zeile.Senken);
    }

    /// <summary>Ohne Senkentext (kein Wärmeerzeuger, kein Projekt) steht keine Zeile.</summary>
    [Fact]
    public void Ohne_Senkentext_steht_keine_Senkenzeile()
    {
        var cut = Aufbauen();

        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[0].Click();

        Assert.DoesNotContain(cut.FindAll(".epos-herleitung-text"),
                              e => e.TextContent.StartsWith("Senken", StringComparison.Ordinal));
    }
}
