using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bunit;
using EPOS.UI.Bausteine;
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
        Func<IReadOnlyList<Katalogfilterzeile>>? katalogzeilen = null,
        Satzbearbeitungswege? projektsatzWege = null,
        Satzbearbeitungswege? katalogsatzWege = null,
        Func<IReadOnlyDictionary<string, object>>? neuGaben = null,
        Func<string>? summe = null,
        Rueckwegwege? rueckwegWege = null)
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
            .Add(x => x.ProjektsatzWege, projektsatzWege)
            .Add(x => x.KatalogsatzWege, katalogsatzWege)
            .Add(x => x.RueckwegWege, rueckwegWege)
            .Add(x => x.NeuGaben, neuGaben)
            .Add(x => x.SummePtherm, summe)
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
        => cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[0].Click();

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Die_beiden_Listen_die_Pfeile_und_die_Filter_stehen()
    {
        var cut = Aufbauen();

        Assert.Equal(2, cut.FindAll(".epos-raster").Count);
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-knopf--richtung").Count);
        // Entscheid #76, Anordnung seit W14a-E-10-Q2 untereinander: Jeder Knopf
        // traegt EIN Zeichen (▲ hinauf ins Projekt, ▼ hinunter in den Katalog)
        // und dazu seine Aufgabe im Klartext.
        Assert.Equal("▲", cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].QuerySelector(".epos-zweispalten-pfeil")!.TextContent);
        Assert.Equal("▼", cut.FindAll(".epos-zweispalten-knopf--entfernen")[0].QuerySelector(".epos-zweispalten-pfeil")!.TextContent);
        Assert.Equal("In das Projekt übernehmen",
                     cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].QuerySelector(".epos-zweispalten-knopftext")!.TextContent);
        Assert.Equal("Aus dem Projekt entfernen",
                     cut.FindAll(".epos-zweispalten-knopf--entfernen")[0].QuerySelector(".epos-zweispalten-knopftext")!.TextContent);

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
        // Katalogauswahl V1, Stufe 2: Die Felder stehen in der Satzflaeche der Detailzeile;
        // ihre Marke sagt „Projektsatz" (die erste Projektzeile ist beim Oeffnen gewaehlt).
        var gruppe = cut.Find(".epos-zweispalten-satz");
        Assert.Equal(Resource.AUSWAHL_MARKE_PROJEKTSATZ, cut.Find(".epos-zweispalten-satzzeile .epos-zweispalten-marke--satz").TextContent);
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
    /// <b>Die Kostenknöpfe gehören dem Projektsatz</b> (Katalogauswahl V1, 4.2): Mit einer
    /// Katalogzeile ist keine Projektzeile gewählt, und die Satzfläche trägt keine Kostenknöpfe.
    /// </summary>
    [Fact]
    public void Die_Kostenknoepfe_stehen_nur_beim_Projektsatz()
    {
        var cut = Aufbauen(
            kostenOeffnen: (_, _) => Task.CompletedTask,
            energiekosten: _ => Task.CompletedTask);
        Assert.Equal(3, cut.FindAll(KOSTENKNOEPFE).Count);

        // Eine Katalogzeile loescht die Projektwahl (listBox_Kessel_DB_SelectedIndexChanged).
        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[0].Click();
        Assert.Null(cut.Instance.Projektzeile);
        Assert.Empty(cut.FindAll(KOSTENKNOEPFE));
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

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[0].Click();

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

        Assert.True(cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].HasAttribute("disabled"));
    }

    [Fact]
    public void Der_Pfeil_oeffnet_zuerst_die_Traegerwahl()
    {
        var cut = Aufbauen();

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[0].Click();
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();

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

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[0].Click();
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();
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

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[0].Click();
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();

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

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[0].Click();
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();

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
        cut.FindAll(".epos-zweispalten-knopf--entfernen")[0].Click();

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

        cut.FindAll(".epos-zweispalten-knopf--entfernen")[0].Click();

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

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[0].Click();
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

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[0].Click();
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

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[0].Click();
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

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[0].Click();
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();
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

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[0].Click();
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

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[0].Click();
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();

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

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[0].Click();
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

        Katalogzeilen(cut)[0].QuerySelector(".epos-zeilenzelle--name")!.Click();
        Assert.False(cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].HasAttribute("disabled"));

        // Ein Filter, der GENAU diese Zeile ausblendet.
        _filterstand.Setzen(Katalogfilterprofil.SpBrennstoff, "Heizöl");
        cut.Render();

        Assert.Single(Katalogzeilen(cut));
        Assert.False(cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>S2.3 / Frage Q12 — „im Projekt verwendet".</b> EINMAL für die ganze Liste
    /// aus der Projektliste des Dialogs gestempelt, nicht je Zeile und nicht aus
    /// der Datenbank: Der Dialog schreibt erst beim OK zurück, eine Zählabfrage
    /// wäre nach der ersten Übernahme veraltet.
    /// </summary>
    [Fact]
    public void S2_3_Die_Verwendungsmarke_zaehlt_die_Projektliste()
    {
        var cut = Aufbauen(zeilen: new List<ErzeugerZeile> { Zeile(1, "Kessel A", 100) });

        var kopf = cut.FindAll(".epos-raster")[1]
                      .QuerySelectorAll("th").Select(e => e.TextContent.Trim()).ToList();
        // Konzept 4.10: die Verwendung steht als Marke am Bezeichner, die Spalte ist
        // nur noch waehlbar (standardmaessig aus).
        Assert.DoesNotContain(kopf, k => k.StartsWith("im Projekt verwendet"));

        var zeilen = Katalogzeilen(cut);
        Assert.Equal(2, zeilen.Count);
        Assert.Equal(1, zeilen.Count(z => z.QuerySelector(".epos-verwendet-marke") is not null));
        Assert.Equal(1, zeilen.Count(z => (z.ClassName ?? "").Contains("epos-zeile--verwendet")));

        var traegt = zeilen.First(z => z.QuerySelector(".epos-verwendet-marke") is not null);
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

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[1].Click();

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
    /// <b>KT-4: Wirft der Schreibweg</b>, nennt die Meldung den Grund, der geänderte Wert bleibt im
    /// Aufklapper stehen und der Dialog offen; das Ausnahmeprotokoll hat einen Vermerk.
    /// </summary>
    [Fact]
    public void Wirft_Speichern_im_Aufklapper_bleibt_die_Eingabe_und_die_Meldung_nennt_den_Grund()
    {
        bool? geschlossen = null;
        var cut = Aufbauen(
            katalogfelder: _ => Felder(),
            katalogfelderSpeichern: (_, _) => throw new InvalidOperationException("Datenbank gesperrt"),
            geschlossen: e => geschlossen = e);
        KatalogsatzWaehlen(cut);
        cut.Find(".epos-modulparameter input[inputmode=decimal]").Input("15000");

        var vermerke = new List<string>();
        void Mit(string z) { lock (vermerke) vermerke.Add(z); }
        Ausnahmeprotokoll.Vermerkt += Mit;
        try { Knopf(cut, "Speichern").Click(); }
        finally { Ausnahmeprotokoll.Vermerkt -= Mit; }

        Assert.Equal(string.Format(Resource.WURZEL_SCHREIBEN_FEHLER, "Kessel A", "Datenbank gesperrt"), cut.Instance.Meldung);
        Assert.Equal("15000", cut.Find(".epos-modulparameter input[inputmode=decimal]").GetAttribute("value"));
        Assert.Null(geschlossen);
        Assert.Contains(vermerke, v => v.Contains("nicht gespeichert"));
    }

    [Fact]
    public void Wirft_Loeschen_nennt_die_Meldung_den_Grund()
    {
        var cut = Aufbauen(katalogLoeschen: _ => throw new InvalidOperationException("Datenbank gesperrt"));

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[0].Click();
        Knopf(cut, "Löschen").Click();
        cut.FindAll(".epos-rueckfrage button")[0].Click();

        Assert.Equal(string.Format(Resource.WURZEL_LOESCHEN_FEHLER, "Kessel A", "Datenbank gesperrt"), cut.Instance.Meldung);
        Assert.NotEmpty(cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name"));
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
    /// <b>Die Knopfzeile steht als ERSTES in der Satzfläche</b>: beim Projektsatz links die
    /// Kostenknöpfe, dann der Füller, rechts die Hilfe; danach die Felder, danach „Alle Daten".
    /// „Bearbeiten…" steht seit Stufe 2 in den Leisten der Bereiche (Konzept 4.2).
    /// </summary>
    [Fact]
    public void Die_Knopfzeile_steht_als_erstes_in_der_Satzflaeche()
    {
        var cut = Aufbauen(kostenOeffnen: (_, _) => Task.CompletedTask,
                           energiekosten: _ => Task.CompletedTask,
                           projektsatzWege: Wege());

        var kinder = cut.Find(".epos-zweispalten-satz").Children.ToList();
        Assert.Contains("epos-leiste", kinder[0].ClassList);
        int raster = kinder.FindIndex(k => k.ClassList.Contains("epos-formularraster"));
        int parameter = kinder.FindIndex(k => k.ClassList.Contains("epos-modulparameter"));
        Assert.True(0 < raster && raster < parameter);

        var teile = kinder[0].Children.ToList();
        Assert.Contains("epos-kostenleiste", teile[0].ClassList);
        Assert.Contains("epos-leiste-fueller", teile[1].ClassList);
        Assert.Contains("epos-berechnungshilfe", teile[2].ClassList);
        Assert.Empty(cut.Find(".epos-zweispalten-satz").QuerySelectorAll("button")
                        .Where(k => k.TextContent.Trim() == "Bearbeiten..."));
    }

    // =================================================================================
    // Katalogauswahl V1, Stufe 2a: Knöpfe an ihrem Ort, Detailzeile, Bearbeiten je Bereich
    // =================================================================================

    /// <summary>Lese- und Speicherwege mit eigenem Feldsatz je Satz; <paramref name="gespeichert"/> fängt den Aufruf.</summary>
    private static Satzbearbeitungswege Wege(List<(int Id, IReadOnlyList<BrowserFeldwert> Felder)>? gespeichert = null,
                                             KatalogSpeicherErgebnis? ergebnis = null)
        => new()
        {
            Lesen = _ => Felder(),
            Speichern = l => { gespeichert?.AddRange(l); return ergebnis ?? new KatalogSpeicherErgebnis(true, "ok", ""); }
        };

    private static IReadOnlyList<Katalogfilterzeile> MitSchloss(params int[] gesperrt)
    {
        var zeilen = Katalogzeilen();
        foreach (var z in zeilen) z.Geschuetzt = gesperrt.Contains(z.Id);
        return zeilen;
    }

    /// <summary>Kreuzt die Katalogzeilen an (Kästchenspalte der Katalogliste).</summary>
    private static void KatalogAnkreuzen(IRenderedComponent<HeizkesselDialog> cut, params int[] zeilen)
    {
        foreach (int i in zeilen)
            cut.FindAll(".epos-raster")[1].QuerySelectorAll("td .epos-kaestchenzelle input")[i].Change(true);
    }

    [Fact]
    public void S2a_Die_Knoepfe_stehen_an_ihrem_Ort()
    {
        var cut = Aufbauen(projektsatzWege: Wege(), katalogsatzWege: Wege(),
                           neuGaben: () => new Dictionary<string, object>(), summe: () => "120");

        // D: Kontext im Dialogkopf, keine eigene Kontextzeile mehr.
        Assert.Equal("Geben Sie Daten des Spitzenlastkessels ein", cut.Find(".epos-dialog-kopf > .epos-dialog-kontext").TextContent);
        Assert.Empty(cut.FindAll(".epos-kontextzeile"));

        // P: Summe, Bearbeiten…, Entfernen - der Rueckweg „In die Datenbank uebernehmen…" nur mit seinen Wegen (S2b).
        var p = cut.Find(".epos-zweispalten-bereich--projekt > .epos-zweispalten-kopfleiste");
        Assert.Contains("120", p.QuerySelector(".epos-zweispalten-summe")!.TextContent);
        Assert.NotNull(p.QuerySelector(".epos-knopf--bearbeiten-projekt"));
        Assert.NotNull(p.QuerySelector(".epos-zweispalten-knopf--entfernen"));
        Assert.DoesNotContain(cut.FindAll("button"), k => k.TextContent.Contains("Datenbank übernehmen"));

        // K-Kopf: Suche und Trefferzahl links, Uebernehmen rechts.
        var k = cut.Find(".epos-zweispalten-bereich--katalog > .epos-zweispalten-kopfleiste");
        Assert.NotNull(k.QuerySelector(".epos-katalog-suchzeile input[type=search]"));
        Assert.NotNull(k.QuerySelector(".epos-katalog-treffer"));
        Assert.NotNull(k.QuerySelector(".epos-zweispalten-knopf--uebernehmen"));

        // K-Fuss: Vergleichen, Schloss, Loeschen, Bearbeiten nach dem Namen; Neu rechts.
        KatalogsatzWaehlen(cut);
        var fuss = cut.Find(".epos-zweispalten-fussleiste").Children.ToList();
        Assert.Equal("Kessel A:", fuss[0].TextContent);
        Assert.Contains(fuss, e => e.ClassList.Contains("epos-knopf--vergleichen"));
        Assert.Contains(fuss, e => e.ClassList.Contains("epos-knopf--loeschen"));
        Assert.Contains(fuss, e => e.ClassList.Contains("epos-knopf--bearbeiten-katalog"));
        int fueller = fuss.FindIndex(e => e.ClassList.Contains("epos-leiste-fueller"));
        Assert.True(fuss.FindIndex(e => e.ClassList.Contains("epos-knopf--neu")) > fueller);
        Assert.True(fuss.FindIndex(e => e.ClassList.Contains("epos-knopf--bearbeiten-katalog")) < fueller);
    }

    [Fact]
    public void S2a_Die_Detailzeile_nennt_Marke_Name_und_Kenndaten()
    {
        var cut = Aufbauen(katalogzeilen: () => MitSchloss(11));
        var zeile = cut.Find(".epos-zweispalten-satzzeile");
        Assert.Equal(Resource.AUSWAHL_MARKE_PROJEKTSATZ, zeile.QuerySelector(".epos-zweispalten-marke--satz")!.TextContent);
        Assert.Equal("Kessel A", zeile.QuerySelector(".epos-zweispalten-satzname")!.TextContent);
        Assert.Contains("Brennstoff Typ Erdgas E", zeile.QuerySelector(".epos-zweispalten-satzkenndaten")!.TextContent);

        KatalogsatzWaehlen(cut);
        zeile = cut.Find(".epos-zweispalten-satzzeile");
        Assert.Equal(Resource.AUSWAHL_MARKE_KATALOGSATZ, zeile.QuerySelector(".epos-zweispalten-marke--satz")!.TextContent);
        Assert.Contains(Resource.AUSWAHL_SATZ_NUR_LESEN, zeile.QuerySelector(".epos-zweispalten-satzkenndaten")!.TextContent);
    }

    [Fact]
    public void S2a_Bearbeiten_im_Projektbereich_oeffnet_die_Projektkopie_mit_Marke()
    {
        var cut = Aufbauen(projektsatzWege: Wege());

        cut.Find(".epos-knopf--bearbeiten-projekt").Click();

        Assert.Equal(Satzmarke.Projektsatz, cut.Instance.Bearbeitung!.Value.Art);
        Assert.Equal(100, Assert.Single(cut.Instance.Bearbeitung!.Value.Saetze).Id);
        var kopf = cut.Find(".epos-ueberlagerung-kopf");
        Assert.Equal(Resource.AUSWAHL_MARKE_PROJEKTSATZ, kopf.QuerySelector(".epos-zweispalten-marke--satz")!.TextContent);
        Assert.Equal(string.Format(Resource.Culture, Resource.SATZBEARB_TITEL_PROJEKT, "Kessel A"),
                     kopf.QuerySelector(".epos-ueberlagerung-titel")!.TextContent);
        Assert.Empty(cut.FindAll(".epos-satzbearbeitung-blaetter"));
    }

    [Fact]
    public void S2a_Mehrfach_Bearbeiten_blaettert_und_setzt_fuer_alle_in_einem_Speichern()
    {
        var gespeichert = new List<(int Id, IReadOnlyList<BrowserFeldwert> Felder)>();
        var cut = Aufbauen(katalogsatzWege: Wege(gespeichert), editorGaben: _ => new Dictionary<string, object>());

        KatalogAnkreuzen(cut, 0, 1);
        Assert.Equal(2, cut.Instance.KatalogWahl.Anzahl);
        cut.Find(".epos-knopf--bearbeiten-katalog").Click();

        Assert.False(cut.Instance.EditorOffen);           // mehrere Saetze: nicht der Einzeleditor
        var sb = cut.FindComponent<Satzbearbeitung>();
        Assert.Equal(2, sb.Instance.Aktiv.Count);
        Assert.Contains(string.Format(Resource.Culture, Resource.SATZBEARB_BLATT, 1, 2),
                        cut.Find(".epos-satzbearbeitung-blaetter").TextContent);
        Assert.Equal(string.Format(Resource.Culture, Resource.SATZBEARB_TITEL_KATALOG_N, 2),
                     cut.Find(".epos-ueberlagerung-titel").TextContent);

        // Hersteller im ersten Satz aendern und „fuer alle gewaehlten setzen".
        cut.Find(".epos-satzbearbeitung input[type=text]:not([readonly])").Input("Neuwerk");
        cut.FindAll(".epos-satzbearbeitung-fueralle input")[0].Change(true);

        // Blaettern zeigt den zweiten Satz mit dem uebertragenen Wert.
        cut.Find(".epos-satzbearbeitung-blaetter button:last-child").Click();
        Assert.Equal(1, sb.Instance.Index);
        Assert.Equal("Neuwerk", sb.Instance.Felder[1].First(f => f.Schluessel == KatalogBrowserProfil.FeldFirma).Wert);

        cut.Find(".epos-satzbearbeitung-speichern").Click();

        Assert.Equal(new[] { 11, 12 }, gespeichert.Select(g => g.Id).ToArray());
        Assert.All(gespeichert, g => Assert.Equal("Neuwerk", g.Felder.First(f => f.Schluessel == KatalogBrowserProfil.FeldFirma).Wert));
        Assert.Null(cut.Instance.Bearbeitung);
    }

    [Fact]
    public void S2a_Gesperrte_Katalogsaetze_werden_uebersprungen_und_genannt()
    {
        var cut = Aufbauen(katalogsatzWege: Wege(), katalogzeilen: () => MitSchloss(12));

        KatalogAnkreuzen(cut, 0, 1);
        cut.Find(".epos-knopf--bearbeiten-katalog").Click();

        var sb = cut.FindComponent<Satzbearbeitung>();
        Assert.Equal(11, Assert.Single(sb.Instance.Aktiv).Id);
        Assert.Contains("„Kessel B“", cut.Find(".epos-satzbearbeitung-hinweis--uebersprungen").TextContent);
        Assert.False(sb.Instance.NurLesend);
    }

    [Fact]
    public void S2a_Ein_gesperrter_Katalogsatz_allein_oeffnet_nur_lesend()
    {
        var cut = Aufbauen(katalogsatzWege: Wege(), katalogzeilen: () => MitSchloss(11),
                           editorGaben: _ => new Dictionary<string, object>());

        KatalogsatzWaehlen(cut);
        cut.Find(".epos-knopf--bearbeiten-katalog").Click();

        Assert.False(cut.Instance.EditorOffen);
        var sb = cut.FindComponent<Satzbearbeitung>();
        Assert.True(sb.Instance.NurLesend);
        Assert.Contains(Resource.ADM_SCHLOSS_ERST_AUFHEBEN, cut.Find(".epos-satzbearbeitung-hinweis--gesperrt").TextContent);
        Assert.Empty(cut.FindAll(".epos-satzbearbeitung-speichern"));
        Assert.Empty(cut.FindAll(".epos-satzbearbeitung input[type=text]:not([readonly])"));
    }

    [Fact]
    public void S2a_Ein_abgelehntes_Sammelspeichern_bleibt_offen_und_nennt_den_Grund()
    {
        var cut = Aufbauen(katalogsatzWege: Wege(ergebnis: new KatalogSpeicherErgebnis(false, "„Kessel B“ ist gesperrt.", "")));

        KatalogAnkreuzen(cut, 0, 1);
        cut.Find(".epos-knopf--bearbeiten-katalog").Click();
        cut.Find(".epos-satzbearbeitung input[type=text]:not([readonly])").Input("Neuwerk");
        cut.Find(".epos-satzbearbeitung-speichern").Click();

        Assert.NotNull(cut.Instance.Bearbeitung);
        Assert.Contains("gesperrt", cut.Find(".epos-satzbearbeitung-meldung").TextContent);
    }

    [Fact]
    public void S2a_Enter_im_Katalog_uebernimmt_und_die_Sammeluebernahme_fragt_je_Gruppe_einmal()
    {
        var aufgenommen = new List<int>();
        var cut = Aufbauen(aufnehmen: (id, _) =>
        {
            aufgenommen.Add(id);
            return new AufnahmeErgebnis(Zeile(20 + id, "Neu " + id, 300 + id));
        });

        // Enter (und Doppelklick) melden sich ueber das Skript beim Baustein.
        KatalogsatzWaehlen(cut);
        var baustein = cut.FindComponent<EPOS.UI.Bausteine.Zweispaltenauswahl>();
        cut.InvokeAsync(() => baustein.Instance.ListenTaste("Katalog", "Enter"));
        Assert.True(cut.Instance.Traegerwahl);
        cut.Find(".epos-ueberlagerung").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(aufgenommen);

        // Zwei angekreuzte Saetze derselben Traegergruppe: EINE Traegerwahl, zwei Aufnahmen.
        KatalogAnkreuzen(cut, 0, 1);
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();
        Assert.True(cut.Instance.Traegerwahl);
        var traeger = cut.FindComponent<EnergietraegerVarianteDialog>();
        cut.InvokeAsync(() => traeger.Instance.Geschlossen.InvokeAsync(new EnergietraegerVarianteErgebnis(3, "Erdgas E", "Var")));

        Assert.False(cut.Instance.Traegerwahl);
        Assert.Equal(new[] { 11, 12 }, aufgenommen.ToArray());
        Assert.Equal(0, cut.Instance.KatalogWahl.Anzahl);
    }

    [Fact]
    public void S2a_Entfernen_wirkt_auf_alle_gewaehlten_Projektzeilen()
    {
        var entfernt = new List<string>();
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Kessel A", 100), Zeile(2, "Kessel B", 101), Zeile(3, "Kessel C", 102) };
        var cut = Aufbauen(zeilen, entfernen: z => entfernt.Add(z.Bezeichner));

        var kaestchen = cut.FindAll(".epos-raster")[0].QuerySelectorAll("td .epos-wahlkaestchen");
        kaestchen[0].Click();
        cut.FindAll(".epos-raster")[0].QuerySelectorAll("td .epos-wahlkaestchen")[2].Click();
        Assert.Equal(2, cut.Instance.ProjektWahl.Anzahl);

        cut.Find(".epos-zweispalten-knopf--entfernen").Click();

        Assert.Equal(new[] { "Kessel A", "Kessel C" }, entfernt.ToArray());
        Assert.Equal("Kessel B", Assert.Single(zeilen).Bezeichner);
    }

    [Fact]
    public void S2a_Loeschen_mehrerer_nennt_Zahl_und_Namen_und_ueberspringt_gesperrte()
    {
        var geloescht = new List<int>();
        var cut = Aufbauen(katalogzeilen: () => MitSchloss(12), katalogLoeschen: id => { geloescht.Add(id); return true; });

        KatalogAnkreuzen(cut, 0, 1);
        cut.Find(".epos-knopf--loeschen").Click();
        var frage = cut.Find(".epos-rueckfrage").TextContent;
        Assert.Contains("\"Kessel A\"", frage);
        Assert.Contains(string.Format(Resource.Culture, Resource.SATZBEARB_UEBERSPRUNGEN, "„Kessel B“"), frage);
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

    // =================================================================================
    // Auslegung für Verteilung und Vorbelegung (Anwenderauftrag 30.09.2026)
    // =================================================================================

    /// <summary>Die Gruppe „Auslegung für Verteilung" im Detailraster.</summary>
    private static AngleSharp.Dom.IElement Auslegungsgruppe(IRenderedComponent<HeizkesselDialog> cut)
        => cut.FindAll(".epos-formularraster .epos-formulargruppe")
              .Single(g => g.QuerySelector(".epos-formulargruppe-titel")?.TextContent == "Auslegung für Verteilung");

    /// <summary>
    /// <b>Die Anordnung</b>: Vorlauf und Rücklauf stehen als eigene Gruppe unter einer
    /// Überschrift über alle Spalten — Überschrift, Vorlauf, Rücklauf unmittelbar
    /// hintereinander, keines der beiden Felder über die volle Breite. Damit beginnen sie
    /// eine neue Rasterzeile und stehen nebeneinander (zwei Feldpaare je Zeile). Darunter
    /// die Herkunft der Vorbelegung, dann die Senken; der Brennwert-Schalter bleibt
    /// außerhalb bei den Katalogdaten.
    /// </summary>
    [Fact]
    public void Vorlauf_und_Ruecklauf_stehen_als_Gruppe_nebeneinander_mit_Herleitung_und_Senken()
    {
        ErzeugerZeile zeile = Zeile(1, "Kessel A", 100);
        zeile.TemperaturHerleitung = "Vorgabe 70/50 °C — so rechnet die Simulation ohne Eintrag.";
        zeile.Senken = "Senken: Heizkreis (Heizung + Warmwasser)";
        var cut = Aufbauen(zeilen: new List<ErzeugerZeile> { zeile });

        var gruppe = Auslegungsgruppe(cut);
        var kinder = gruppe.Children.ToList();

        Assert.Equal(5, kinder.Count);
        Assert.Contains("epos-formulargruppe-titel", kinder[0].ClassList);
        Assert.Equal("Vorlauf:", kinder[1].QuerySelector(".epos-feld-text")!.TextContent);
        Assert.Equal("Rücklauf:", kinder[2].QuerySelector(".epos-feld-text")!.TextContent);
        Assert.DoesNotContain("epos-feld--breit", kinder[1].ClassList);
        Assert.DoesNotContain("epos-feld--breit", kinder[2].ClassList);
        Assert.Equal(zeile.TemperaturHerleitung, kinder[3].QuerySelector(".epos-herleitung-text")!.TextContent);
        Assert.Equal(zeile.Senken, kinder[4].QuerySelector(".epos-herleitung-text")!.TextContent);

        // Genau die zwei Temperaturfelder; der Brennwert-Schalter steht außerhalb der Gruppe.
        Assert.Equal(2, gruppe.QuerySelectorAll("input[inputmode=numeric]").Length);
        Assert.Empty(gruppe.QuerySelectorAll("input[type=checkbox]"));
        Assert.Single(cut.Find(".epos-zweispalten-satz").QuerySelectorAll("input[type=checkbox]"));
    }

    /// <summary>Ohne Vorbelegung (das Paar ist das der Anlage) steht nur die Senkenzeile.</summary>
    [Fact]
    public void Ohne_Vorbelegung_steht_keine_Herkunftszeile()
    {
        var cut = Aufbauen();

        var gruppe = Auslegungsgruppe(cut);

        Assert.Empty(gruppe.QuerySelectorAll(".epos-herleitung"));
        Assert.Equal(new[] { "Vorlauf:", "Rücklauf:" },
                     gruppe.QuerySelectorAll(".epos-feld-text").Select(e => e.TextContent));
    }

    /// <summary>
    /// <b>Die Vorbelegung</b>, wie die Hülle sie baut (Kern:
    /// <c>AnlagenTemperaturen.KesselPaar</c> und <c>Herleitung</c>): ohne Paar an Anlage und
    /// Kessel die Vorgabe 70/50 °C, mit Kesselpaar dessen Werte — jeweils in den Feldern und
    /// mit ihrer Herkunft darunter.
    /// </summary>
    [Theory]
    [InlineData(null, null, "70", "50", "Vorgabe 70/50 °C — so rechnet die Simulation ohne Eintrag.")]
    [InlineData(85, 65, "85", "65", "Aus dem Kesseldatensatz: 85/65 °C — so rechnet die Simulation ohne Eintrag.")]
    public void Ein_leeres_Paar_zeigt_die_Vorbelegung_des_Kerns(
        int? kesselVorlauf, int? kesselRuecklauf, string vorlauf, string ruecklauf, string herleitung)
    {
        AnlagenTemperaturen.PaarVorbelegung p = AnlagenTemperaturen.KesselPaar(0, 0, kesselVorlauf, kesselRuecklauf);
        ErzeugerZeile zeile = Zeile(1, "Kessel A", 100);
        zeile.Vorlauf = p.Vorlauf;
        zeile.Ruecklauf = p.Ruecklauf;
        zeile.TemperaturHerleitung = AnlagenTemperaturen.Herleitung(p);

        var cut = Aufbauen(zeilen: new List<ErzeugerZeile> { zeile });

        var gruppe = Auslegungsgruppe(cut);
        var felder = gruppe.QuerySelectorAll("input[inputmode=numeric]");
        Assert.Equal(vorlauf, felder[0].GetAttribute("value"));
        Assert.Equal(ruecklauf, felder[1].GetAttribute("value"));
        Assert.Equal(herleitung, gruppe.QuerySelector(".epos-herleitung-text")!.TextContent);
    }

    /// <summary>
    /// <b>Der Weg der Hülle</b> (<c>TemperaturVorbelegung.Kessel</c>, den
    /// <c>HeizkesselHuelle</c> beim Aufbau und beim Aufnehmen ruft): Ein Modell ohne Paar und
    /// ohne Kessel bekommt 70/50 °C INS MODELL — damit speichert OK es — und die Zeile dazu;
    /// ein gepflegtes Paar bleibt, wie es ist, ohne Zeile.
    /// </summary>
    [Fact]
    public void Die_Huelle_legt_die_Vorbelegung_ins_Modell()
    {
        var ohne = new WErzeugerModel { ID = 100001, ID_Type = WizardItemClass.KESSEL_TYP };
        Assert.Equal("Vorgabe 70/50 °C — so rechnet die Simulation ohne Eintrag.",
                     TemperaturVorbelegung.Kessel(ohne, wizard: false));
        Assert.Equal((70, 50), (ohne.Vorlauf, ohne.Ruecklauf));

        var gepflegt = new WErzeugerModel { ID = 7, ID_Type = WizardItemClass.KESSEL_TYP, Vorlauf = 80, Ruecklauf = 60 };
        Assert.Equal("", TemperaturVorbelegung.Kessel(gepflegt, wizard: true));
        Assert.Equal((80, 60), (gepflegt.Vorlauf, gepflegt.Ruecklauf));
    }

    /// <summary>
    /// Eine Eingabe macht das Paar zu dem des Anwenders: Die Herkunftszeile verschwindet,
    /// der Wert geht ins Modell, die Senkenzeile bleibt.
    /// </summary>
    [Fact]
    public void Eine_Eingabe_nimmt_die_Herkunftszeile_weg()
    {
        ErzeugerZeile zeile = Zeile(1, "Kessel A", 100);
        zeile.TemperaturHerleitung = "Vorgabe 70/50 °C — so rechnet die Simulation ohne Eintrag.";
        zeile.Senken = "Senken: Heizkreis (Heizung + Warmwasser)";
        var uebernommen = new List<ErzeugerZeile>();
        var cut = Aufbauen(zeilen: new List<ErzeugerZeile> { zeile }, uebernehmen: z => uebernommen.Add(z));

        Auslegungsgruppe(cut).QuerySelectorAll("input[inputmode=numeric]")[1].Input("45");

        var gruppe = Auslegungsgruppe(cut);
        Assert.Equal("", zeile.TemperaturHerleitung);
        Assert.Equal(45, zeile.Ruecklauf);
        Assert.Single(uebernommen);
        Assert.Equal(new[] { zeile.Senken },
                     gruppe.QuerySelectorAll(".epos-herleitung-text").Select(e => e.TextContent));
    }

    // =================================================================================
    // Katalogauswahl V1, Stufe 2b: Rückweg „In die Datenbank übernehmen…" (5.2, KA‑E‑9)
    // =================================================================================

    /// <summary>Rückwegwege mit festen Zeilen; <paramref name="geschrieben"/> fängt die Wahl.</summary>
    private static Rueckwegwege Rueckweg(IReadOnlyList<Rueckwegvorschlag> zeilen, List<Rueckwegwahl>? geschrieben = null,
                                         Func<string, bool>? belegt = null, KatalogSpeicherErgebnis? ergebnis = null,
                                         List<IReadOnlyList<int>>? gefragt = null)
        => new()
        {
            Vorschau = ids => { gefragt?.Add(ids); return zeilen; },
            NameBelegt = belegt ?? (_ => false),
            Uebernehmen = w => { geschrieben?.AddRange(w); return ergebnis ?? new KatalogSpeicherErgebnis(true, "2 Sätze übernommen", ""); },
        };

    [Fact]
    public void S2b_Der_Knopf_steht_nach_Bearbeiten_in_der_Projektleiste_und_wirkt_auf_die_Auswahl()
    {
        var gefragt = new List<IReadOnlyList<int>>();
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Kessel A", 100), Zeile(2, "Kessel B", 101), Zeile(3, "Kessel A", 100) };
        var cut = Aufbauen(zeilen, projektsatzWege: Wege(),
                           rueckwegWege: Rueckweg(new[] { new Rueckwegvorschlag(100, "Kessel A", "", Rueckwegsperre.UrsprungUnbekannt, "Kessel A") },
                                                  gefragt: gefragt));

        var leiste = cut.Find(".epos-zweispalten-bereich--projekt > .epos-zweispalten-kopfleiste");
        var knoepfe = leiste.QuerySelectorAll("button").ToList();
        int bearbeiten = knoepfe.FindIndex(k => k.ClassList.Contains("epos-knopf--bearbeiten-projekt"));
        int rueckweg = knoepfe.FindIndex(k => k.ClassList.Contains("epos-knopf--rueckweg"));
        Assert.True(bearbeiten >= 0 && rueckweg == bearbeiten + 1);
        Assert.Equal(Resource.KATRUECK_BTN, knoepfe[rueckweg].TextContent.Trim());

        // Drei Zeilen, zwei Geraete: gefragt wird je Geraet einmal.
        foreach (int i in new[] { 0, 1, 2 })
            cut.FindAll(".epos-raster")[0].QuerySelectorAll("td .epos-wahlkaestchen")[i].Click();
        cut.Find(".epos-knopf--rueckweg").Click();
        Assert.Equal(new[] { 100, 101 }, Assert.Single(gefragt).ToArray());
        Assert.NotNull(cut.Instance.Rueckweg);
    }

    [Fact]
    public void S2b_Die_Rueckfrage_steht_auf_neu_und_graut_ueberschreiben_mit_Grund_aus()
    {
        var zeilen = new[]
        {
            new Rueckwegvorschlag(100, "Kessel A", "Kessel A", Rueckwegsperre.Keine, "Kessel A (Projekt)"),
            new Rueckwegvorschlag(101, "Kessel B", "Kessel B", Rueckwegsperre.Gesperrt, "Kessel B (Projekt)"),
            new Rueckwegvorschlag(102, "Kessel C", "", Rueckwegsperre.UrsprungFehlt, "Kessel C"),
            new Rueckwegvorschlag(103, "Kessel D", "", Rueckwegsperre.UrsprungUnbekannt, "Kessel D"),
        };
        var cut = Aufbauen(rueckwegWege: Rueckweg(zeilen));
        cut.Find(".epos-knopf--rueckweg").Click();

        var kopf = cut.Find(".epos-ueberlagerung-kopf");
        Assert.Equal(string.Format(Resource.Culture, Resource.KATRUECK_TITEL_N, 4), kopf.QuerySelector(".epos-ueberlagerung-titel")!.TextContent);
        var reihen = cut.FindAll(".epos-rueckweg-zeile");
        Assert.Equal(4, reihen.Count);
        Assert.All(reihen, r => Assert.True(r.QuerySelector(".epos-rueckweg-neu")!.HasAttribute("checked")));
        Assert.False(reihen[0].QuerySelector(".epos-rueckweg-ueber")!.HasAttribute("disabled"));
        Assert.Contains(Resource.KATRUECK_GRUND_GESPERRT, reihen[1].TextContent);
        Assert.Contains(Resource.KATRUECK_GRUND_FEHLT, reihen[2].TextContent);
        Assert.Contains(Resource.KATRUECK_GRUND_UNBEKANNT, reihen[3].TextContent);
        foreach (int i in new[] { 1, 2, 3 }) Assert.True(reihen[i].QuerySelector(".epos-rueckweg-ueber")!.HasAttribute("disabled"));
        Assert.Equal("Kessel A (Projekt)", reihen[0].QuerySelector(".epos-rueckweg-namensfeld")!.GetAttribute("value"));
        Assert.False(cut.Find(".epos-rueckweg-uebernehmen").HasAttribute("disabled"));
        Assert.Contains(Resource.KATRUECK_HINWEIS_KOSTEN, cut.Find(".epos-rueckweg").TextContent);
    }

    [Fact]
    public void S2b_Ein_belegter_oder_doppelter_Name_sperrt_Uebernehmen_mit_Hinweis_am_Feld()
    {
        var zeilen = new[]
        {
            new Rueckwegvorschlag(100, "Kessel A", "", Rueckwegsperre.UrsprungUnbekannt, "Kessel A (Projekt)"),
            new Rueckwegvorschlag(101, "Kessel B", "", Rueckwegsperre.UrsprungUnbekannt, "Kessel B"),
        };
        var cut = Aufbauen(rueckwegWege: Rueckweg(zeilen, belegt: n => n == "Kessel A"));
        cut.Find(".epos-knopf--rueckweg").Click();

        cut.FindAll(".epos-rueckweg-namensfeld")[0].Input("Kessel A");
        Assert.Contains(Resource.KATRUECK_HINWEIS_NAME_BELEGT, cut.FindAll(".epos-rueckweg-zeile")[0].TextContent);
        Assert.True(cut.Find(".epos-rueckweg-uebernehmen").HasAttribute("disabled"));

        cut.FindAll(".epos-rueckweg-namensfeld")[0].Input("Kessel B");
        Assert.Contains(Resource.KATRUECK_HINWEIS_NAME_DOPPELT, cut.FindAll(".epos-rueckweg-zeile")[0].TextContent);
        Assert.True(cut.Find(".epos-rueckweg-uebernehmen").HasAttribute("disabled"));

        cut.FindAll(".epos-rueckweg-namensfeld")[0].Input("Kessel A neu");
        Assert.Empty(cut.FindAll(".epos-rueckweg-namenshinweis"));
        Assert.False(cut.Find(".epos-rueckweg-uebernehmen").HasAttribute("disabled"));
    }

    [Fact]
    public void S2b_Mehrfach_fuer_alle_ueberschreiben_faellt_je_Zeile_auf_neu_zurueck_und_nennt_sie()
    {
        var geschrieben = new List<Rueckwegwahl>();
        var zeilen = new[]
        {
            new Rueckwegvorschlag(100, "Kessel A", "Kessel A Katalog", Rueckwegsperre.Keine, "Kessel A (Projekt)"),
            new Rueckwegvorschlag(101, "Kessel B", "", Rueckwegsperre.UrsprungUnbekannt, "Kessel B"),
        };
        var cut = Aufbauen(rueckwegWege: Rueckweg(zeilen, geschrieben));
        cut.Find(".epos-knopf--rueckweg").Click();

        cut.Find(".epos-rueckweg-alle-ueber").Change(true);

        var reihen = cut.FindAll(".epos-rueckweg-zeile");
        Assert.True(reihen[0].QuerySelector(".epos-rueckweg-ueber")!.HasAttribute("checked"));
        Assert.True(reihen[1].QuerySelector(".epos-rueckweg-neu")!.HasAttribute("checked"));
        var feld = reihen[0].QuerySelector(".epos-rueckweg-namensfeld")!;
        Assert.True(feld.HasAttribute("disabled"));
        Assert.Equal("Kessel A Katalog", feld.GetAttribute("value"));
        Assert.Contains("„Kessel B“", cut.Find(".epos-rueckweg-rueckfall").TextContent);

        cut.Find(".epos-rueckweg-uebernehmen").Click();

        Assert.Equal(new[] { new Rueckwegwahl(100, true, ""), new Rueckwegwahl(101, false, "Kessel B") }, geschrieben.ToArray());
        Assert.Null(cut.Instance.Rueckweg);
        Assert.Equal("2 Sätze übernommen", cut.Instance.Meldung);
    }

    [Fact]
    public void S2b_Eine_Absage_des_Kerns_bleibt_in_der_Rueckfrage_stehen()
    {
        var zeilen = new[] { new Rueckwegvorschlag(100, "Kessel A", "", Rueckwegsperre.UrsprungUnbekannt, "Kessel A") };
        var cut = Aufbauen(rueckwegWege: Rueckweg(zeilen, ergebnis: new KatalogSpeicherErgebnis(false, "Name belegt. Es wurde nichts übernommen.", "")));
        cut.Find(".epos-knopf--rueckweg").Click();
        cut.Find(".epos-rueckweg-uebernehmen").Click();

        Assert.NotNull(cut.Instance.Rueckweg);
        Assert.Equal("Name belegt. Es wurde nichts übernommen.", cut.Find(".epos-rueckweg-meldung").TextContent);
        cut.Find(".epos-rueckweg-fuss .epos-knopf:not(.epos-knopf--primaer)").Click();
        Assert.Null(cut.Instance.Rueckweg);
    }

    [Fact]
    public void S2b_Ohne_Wege_kein_Knopf()
    {
        var cut = Aufbauen(projektsatzWege: Wege());
        Assert.Empty(cut.FindAll(".epos-knopf--rueckweg"));
    }
}
