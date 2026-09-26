using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Kosten;
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
/// Verwaltung BHKW — der Projektdialog (iU9-W6.4). Soll ist die Feldkarte von
/// <c>Form_BHKWEing</c>: zwei Listen (die rechte mit zwei Spalten), die beiden
/// Pfeile, zwei Filter, die Leistungssumme, der Detailblock mit Trägerwahl und
/// Grenzleistung sowie die drei Katalogknöpfe.
/// </summary>
public class BhkwDialogTests : EposBunitContext
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
        Katalogfilterprofil.MitVerwendung(Anlagenart.Bhkw,
            s => Resource.ResourceManager.GetString(s) ?? s);

    /// <summary>
    /// Die Katalogzeilen. Was der Vorlaeufer als Mehrzeiler in EINE Zelle schrieb
    /// (Firma, Brennstoff, Ptherm, Pel), steht jetzt als eigene, sortierbare Spalte.
    /// </summary>
    private static IReadOnlyList<Katalogfilterzeile> Katalogzeilen() => new[]
    {
        new Katalogfilterzeile(21, "Modul A")
            .MitText(Katalogfilterprofil.SpBezeichner, "Modul A")
            .MitText(Katalogfilterprofil.SpHersteller, "Musterwerk")
            .MitText(Katalogfilterprofil.SpBrennstoff, "Erdgas E")
            .MitZahl(Katalogfilterprofil.SpPel, 40.0)
            .MitZahl(Katalogfilterprofil.SpPtherm, 80.0)
            .MitZahl(Katalogfilterprofil.SpSigma, 0.5, 2)
            .MitZahl(Katalogfilterprofil.SpEta, 0.9, 3)
            .MitText(Katalogfilterprofil.SpMotortyp, "Gas-Otto-Motor"),

        new Katalogfilterzeile(22, "Modul B")
            .MitText(Katalogfilterprofil.SpBezeichner, "Modul B")
            .MitText(Katalogfilterprofil.SpHersteller, "Musterwerk")
            .MitText(Katalogfilterprofil.SpBrennstoff, "Erdgas LL")
            .MitZahl(Katalogfilterprofil.SpPel, 60.0)
            .MitZahl(Katalogfilterprofil.SpPtherm, 120.0)
            .MitZahl(Katalogfilterprofil.SpSigma, 0.5, 2)
            .MitZahl(Katalogfilterprofil.SpEta, 0.9, 3)
            .MitText(Katalogfilterprofil.SpMotortyp, "Gas-Otto-Motor"),
    };

    public BhkwDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static ErzeugerZeile Zeile(int schluessel, string name, int geraetId, int carrier = 5)
        => new() { Schluessel = schluessel, Bezeichner = name, GeraetId = geraetId,
                   CarrierId = carrier, Grenzleistung = 50, Vorlauf = 80, Ruecklauf = 60 };

    /// <summary>
    /// Der Detailblock, wie ihn die Hülle liefert. <b>„Brennstoff Typ:" steht seit dem
    /// 16.09.2026 vorn</b> (Anwenderentscheid): Der Dialog findet das Feld an dieser
    /// Beschriftung wieder und stellt die Variante unmittelbar darunter.
    /// </summary>
    private static ErzeugerDetail Detail(string name) => new(
        name, "Beschreibung",
        new[] { ("Brennstoff Typ:", "Erdgas LL"),
                ("Hersteller:", "Musterwerk"),
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
        Action<bool>? geschlossen = null,
        int? projektvorgabe = null,
        Func<ErzeugerZeile?, bool, Task>? kostenOeffnen = null,
        Func<ErzeugerZeile?, Task>? energiekosten = null,
        Func<string, IReadOnlyList<BrowserFeldwert>?>? katalogfelder = null,
        Func<string, IReadOnlyList<BrowserFeldwert>, bool, KatalogSpeicherErgebnis>? katalogfelderSpeichern = null,
        Func<string, bool>? katalogfelderGeschuetzt = null)
    {
        return Render<BhkwDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<ErzeugerZeile> { Zeile(1, "Modul A", 100) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
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
            .Add(x => x.Projektvorgabe, projektvorgabe)
            .Add(x => x.KostenOeffnen, kostenOeffnen)
            .Add(x => x.EnergiekostenOeffnen, energiekosten)
            .Add(x => x.Katalogfelder, katalogfelder)
            .Add(x => x.KatalogfelderSpeichern, katalogfelderSpeichern)
            .Add(x => x.KatalogfelderGeschuetzt, katalogfelderGeschuetzt)
            .Add(x => x.Geschlossen, ok => geschlossen?.Invoke(ok)));
    }

    /// <summary>
    /// Die Felder des Aufklappers — ein Ausschnitt des BHKW-Profils: ein Text, eine
    /// Zahl und die ABGELEITETE Investition je kWel, die niemand von Hand setzt.
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
            Schluessel = KatalogBrowserProfil.FeldKostenModul,
            Bezeichnung = "Modul:",
            Einheit = "€",
            Art = BrowserFeldArt.Zahl,
            Editierbar = true,
            Wert = "40000"
        },
        new BrowserFeldwert
        {
            Schluessel = KatalogBrowserProfil.FeldInvestitionJeKwel,
            Bezeichnung = "Investition je kW elektrisch:",
            Einheit = "€ / kWel",
            Art = BrowserFeldArt.Zahl,
            Editierbar = false,
            Wert = "1250"
        }
    };

    /// <summary>Wählt die erste Katalogzeile — erst dann gibt es einen Aufklapper.</summary>
    private static void KatalogsatzWaehlen(IRenderedComponent<BhkwDialog> cut)
        => cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();

    /// <summary>Der Auswahlpfad der Kostenknöpfe — drei Stück, in dieser Reihenfolge.</summary>
    private const string KOSTENKNOEPFE = ".epos-kostenleiste button.epos-knopf";

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Die_beiden_Listen_die_Pfeile_und_die_Filter_stehen()
    {
        var cut = Aufbauen();

        Assert.Equal(2, cut.FindAll(".epos-raster").Count);
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-uebernahme button").Count);

        var ueberschriften = cut.FindAll(".epos-untergruppe").Select(e => e.TextContent).ToList();
        Assert.Contains("Ausgewählte Module:", ueberschriften);
        Assert.Contains("Module in Datenbank:", ueberschriften);

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.Contains("Summe aller ausgewählten Module [kWth]:", texte);

        // W14a-E-10 / S2.1: Die zwei Klapplisten sind weg; an ihrer Stelle steht
        // die EINE Suchzeile, und der Filter sitzt im Spaltenkopf.
        Assert.DoesNotContain("Filtern nach Brennstoffart", texte);
        Assert.DoesNotContain("Filtern nach Leistung", texte);
        Assert.Single(cut.FindAll(".epos-katalog-suchzeile"));
        // Seit dem 16.09.2026 ein PAAR: der Typ aus dem Detailblock, darunter die
        // Variante - und die heisst nicht mehr „Brennstoff:".
        Assert.Contains("Brennstoff Typ:", texte);
        Assert.Contains("Brennstoff Variante:", texte);
        Assert.DoesNotContain("Brennstoff:", texte);
        Assert.Contains("Untere Grenzleistung des ausgewählten Moduls:", texte);
        Assert.Contains("Vorlauf", texte);
        Assert.Contains("Rücklauf", texte);
    }

    /// <summary>
    /// <b>Aus der Sammelspalte werden SPALTEN</b> (W14a-E-10 / S2.1). Der Vorläufer
    /// trug im DataGridView „Name" und einen Mehrzeiler „Eigenschaften" (Firma,
    /// Brennstoff, Ptherm, Pel) — vier Werte in EINER Zelle, nach keinem davon
    /// sortierbar. Jetzt stehen sie einzeln, jede mit Sortierpfeil.
    /// </summary>
    [Fact]
    public void Die_Katalogliste_zeigt_die_Spalten_des_Profils()
    {
        var cut = Aufbauen();

        var kopf = cut.FindAll(".epos-raster")[1].QuerySelectorAll("th")
                      .Select(e => e.TextContent.Trim()).ToList();

        // Wahl + acht Parameterspalten + „im Projekt verwendet" (Q12).
        Assert.Equal(Profil.Spalten.Count + 1, kopf.Count);
        Assert.Contains(kopf, k => k.StartsWith("Hersteller"));
        Assert.Contains(kopf, k => k.StartsWith("Brennstoff"));
        Assert.Contains(kopf, k => k.Contains("P_el"));
        Assert.Contains(kopf, k => k.Contains("Motortyp"));

        // Der Mehrzeiler ist ersatzlos gefallen.
        Assert.Empty(cut.FindAll(".epos-mehrzeilig"));
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
        // Der TYP gehoert zum Modul und bleibt stehen; die VARIANTE ist eine
        // Projekteigenschaft und verschwindet mit der Projektzeile.
        Assert.Contains("Brennstoff Typ:", texte);
        Assert.DoesNotContain("Brennstoff Variante:", texte);
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
        cut.FindAll(".epos-zweispalten-uebernahme button")[0].Click();

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
        cut.FindAll(".epos-zweispalten-uebernahme button")[0].Click();
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

        cut.FindAll(".epos-zweispalten-uebernahme button")[1].Click();

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
        cut.FindAll(".epos-zweispalten-uebernahme button")[1].Click();

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

    // =================================================================================
    // W6-E-7: die Herleitung unter der Grenzleistung (07.09.2026)
    // =================================================================================

    /// <summary>
    /// Steht im Modulfeld eine <b>0</b>, greift die PROJEKTVORGABE durch — und der
    /// Dialog sagt, welche. Vor W6‑E‑7 fing darunter ein stiller Fallback auf 30 % ab;
    /// ohne ihn muss der Anwender sehen, womit gerechnet wird.
    /// </summary>
    [Fact]
    public void Bei_Modulwert_0_nennt_die_Herleitung_die_Projektvorgabe()
    {
        var zeile = Zeile(1, "Modul A", 100);
        zeile.Grenzleistung = 0;

        var cut = Aufbauen(zeilen: new List<ErzeugerZeile> { zeile }, projektvorgabe: 30);

        Assert.Equal("0 = Projektvorgabe (30 %)",
                     cut.Find("p.epos-herleitung").TextContent.Trim());
    }

    /// <summary>
    /// Ist auch die Projektvorgabe 0, sagt die Zeile ausdrücklich, dass es dann KEINE
    /// Untergrenze gibt — das ist der Fall, den W6‑E‑7 überhaupt erst möglich macht.
    /// </summary>
    [Fact]
    public void Ist_auch_die_Projektvorgabe_0_nennt_die_Herleitung_die_fehlende_Untergrenze()
    {
        var zeile = Zeile(1, "Modul A", 100);
        zeile.Grenzleistung = 0;

        var cut = Aufbauen(zeilen: new List<ErzeugerZeile> { zeile }, projektvorgabe: 0);

        Assert.Contains("keine Untergrenze",
                        cut.Find("p.epos-herleitung").TextContent);
    }

    /// <summary>
    /// Führt das Modul einen EIGENEN Wert, überstimmt er die Vorgabe — dann steht die
    /// Zahl im Feld, und die Zeile schweigt.
    /// </summary>
    [Fact]
    public void Mit_eigenem_Modulwert_schweigt_die_Herleitung()
    {
        // Zeile(...) legt Grenzleistung = 50 an.
        var cut = Aufbauen(projektvorgabe: 30);

        Assert.Empty(cut.FindAll("p.epos-herleitung"));
    }

    /// <summary>
    /// Ohne Projekt (Assistentenbetrieb) kennt die Hülle die Vorgabe nicht und gibt
    /// <c>null</c> — dann bleibt die Zeile weg, statt eine Zahl zu behaupten.
    /// </summary>
    [Fact]
    public void Ohne_bekannte_Projektvorgabe_bleibt_die_Herleitung_weg()
    {
        var zeile = Zeile(1, "Modul A", 100);
        zeile.Grenzleistung = 0;

        var cut = Aufbauen(zeilen: new List<ErzeugerZeile> { zeile }, projektvorgabe: null);

        Assert.Empty(cut.FindAll("p.epos-herleitung"));
    }

    [Fact]
    public void Ein_Traegerwechsel_schreibt_sofort()
    {
        int? gemeldet = null;
        var cut = Aufbauen(traegerWechseln: (_, n) => gemeldet = n);

        // Filter Brennstoff, Filter Leistung, Trägerwahl.
        // Seit S2.1 gibt es nur noch EINE Klappliste: die Trägerwahl.
        cut.FindAll("select")[0].Change("6");

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
        Knopf(cut, "Löschen").Click();

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
        Knopf(cut, "Löschen").Click();
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

        Knopf(cut, "Neu..").Click();
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
        Knopf(cut, "Bearbeiten...").Click();

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
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Modul A", 100) };
        var cut = Aufbauen(zeilen, aufnehmen: (_, _) =>
        {
            aufgenommen = true;
            return new AufnahmeErgebnis(Zeile(9, "Modul B", 200), "angelegt");
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
            new Dictionary<string, object> { ["Daten"] = new BhkwKatalogDaten() });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        Knopf(cut, "Bearbeiten...").Click();
        Assert.True(cut.Instance.Katalogeditor);

        cut.Find(".epos-ueberlagerung-zu").Click();

        Assert.False(cut.Instance.Katalogeditor);
    }

    /// <summary>
    /// Das ✕ über der Namensfrage von „Neu…" bricht sie ab: Es entsteht kein Name,
    /// und der Editor geht nicht auf.
    /// </summary>
    [Fact]
    public void Das_Kreuz_der_Namensfrage_oeffnet_keinen_Editor()
    {
        bool gefragt = false;
        var cut = Aufbauen(editorGabenNeu: _ =>
        {
            gefragt = true;
            return new Dictionary<string, object> { ["Daten"] = new BhkwKatalogDaten() };
        });

        Knopf(cut, "Neu..").Click();
        cut.Find(".epos-ueberlagerung-zu").Click();

        Assert.False(gefragt);
        Assert.False(cut.Instance.Katalogeditor);
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
            new Dictionary<string, object> { ["Daten"] = new BhkwKatalogDaten() });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        Knopf(cut, "Bearbeiten...").Click();

        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
    }

    // =================================================================================
    //  Der Aufklapper „Alle Daten anzeigen" — Anwenderentscheid 15.09.2026
    // =================================================================================
    //
    // „Es soll in diesem Bereich optional alle technischen Daten angezeigt und
    // bearbeitet werden koennen." Er ist der Ersatz fuer die Kosten-, BEHG- und
    // Emissionsgruppen, die aus dem Katalogeditor verschwunden sind; das Raster ist
    // der Baustein Katalogfelder, den auch der Katalogbrowser benutzt.

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
    /// der Feldsatz des vorigen Moduls da, und „Speichern" schriebe ihn unter dem Namen
    /// des jetzt gewählten zurück.
    /// </summary>
    [Fact]
    public void Ein_Satzwechsel_zieht_die_Felder_des_neuen_Satzes_nach()
    {
        var gefragt = new List<string>();
        var cut = Aufbauen(katalogfelder: n => { gefragt.Add(n); return Felder(); });

        KatalogsatzWaehlen(cut);
        Assert.Equal(new[] { "Modul A" }, gefragt);

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[1].Click();

        Assert.Equal(new[] { "Modul A", "Modul B" }, gefragt);
        Assert.True(cut.Instance.ParameterOffen);
    }

    /// <summary>
    /// <b>Ohne Speicherweg ist der Aufklapper reine Anzeige</b> — kein Knopf, jedes Feld
    /// nur lesbar. Das ist die Lage der meisten Kataloge, nicht eine Entscheidung dieses
    /// Dialogs.
    /// </summary>
    [Fact]
    public void Ohne_Speicherweg_ist_der_Aufklapper_nur_Anzeige()
    {
        var cut = Aufbauen(katalogfelder: _ => Felder());

        KatalogsatzWaehlen(cut);

        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Speichern");
        Assert.All(cut.Find(".epos-modulparameter").QuerySelectorAll("input"),
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
        bool? schutz = null;

        var cut = Aufbauen(
            katalogfelder: _ => Felder(),
            katalogfelderSpeichern: (n, f, s) =>
            {
                name = n; geschrieben = f; schutz = s;
                return new KatalogSpeicherErgebnis(true, "Datensatz gespeichert", n);
            });

        KatalogsatzWaehlen(cut);

        Assert.Equal("true", Knopf(cut, "Speichern").GetAttribute("aria-disabled"));
        Assert.False(Knopf(cut, "Speichern").HasAttribute("disabled"));

        cut.Find(".epos-modulparameter input[inputmode=decimal]").Input("60000");
        Assert.False(Knopf(cut, "Speichern").HasAttribute("aria-disabled"));

        Knopf(cut, "Speichern").Click();

        Assert.Equal("Modul A", name);
        Assert.NotNull(geschrieben);
        Assert.Equal("60000",
            geschrieben!.First(f => f.Schluessel == KatalogBrowserProfil.FeldKostenModul).Wert);

        // Ohne Schreibschutzweg wird ohne Rueckfrage und ohne Uebergehen geschrieben.
        Assert.False(schutz);
        Assert.False(cut.Instance.Schutzfrage);
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
            katalogfelderSpeichern: (n, _, _) => { schreibvorgaenge++; return new KatalogSpeicherErgebnis(true, "Datensatz gespeichert", n); });

        KatalogsatzWaehlen(cut);
        cut.Find(".epos-modulparameter input[inputmode=decimal]").Input("60000");
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
    private static AngleSharp.Dom.IElement Vermerk(Bunit.IRenderedComponent<BhkwDialog> cut)
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
            katalogfelderSpeichern: (_, _, _) =>
                new KatalogSpeicherErgebnis(false, "„Modul“ darf nicht negativ sein.", ""));

        KatalogsatzWaehlen(cut);
        cut.Find(".epos-modulparameter input[inputmode=decimal]").Input("60000");
        Knopf(cut, "Speichern").Click();

        Assert.Contains("darf nicht negativ sein", cut.Instance.Meldung);
        Assert.Equal("60000", cut.Find(".epos-modulparameter input[inputmode=decimal]")
                                 .GetAttribute("value"));

        // Der Grund steht auch rot AM Knopf - das Band oben bleibt fuer den Fehler.
        Assert.Contains("darf nicht negativ sein", Vermerk(cut).TextContent);
        Assert.Contains("epos-status--fehler", Vermerk(cut).ClassName);
    }

    /// <summary>
    /// <b>Das BHKW ist die einzige Familie mit Schreibschutz:</b> Ein Auslieferungssatz
    /// wird vor dem Überschreiben erfragt, und erst „Ja" hebt den Schutz für GENAU
    /// diesen Vorgang auf.
    /// </summary>
    [Fact]
    public void Ein_geschuetzter_Satz_fragt_nach_und_Ja_hebt_den_Schutz_auf()
    {
        bool? schutz = null;
        var cut = Aufbauen(
            katalogfelder: _ => Felder(),
            katalogfelderSpeichern: (n, _, s) =>
            {
                schutz = s;
                return new KatalogSpeicherErgebnis(true, "Datensatz gespeichert", n);
            },
            katalogfelderGeschuetzt: _ => true);

        KatalogsatzWaehlen(cut);
        cut.Find(".epos-modulparameter input[inputmode=decimal]").Input("60000");
        Knopf(cut, "Speichern").Click();

        // Erst die Rueckfrage - geschrieben ist noch nichts.
        Assert.True(cut.Instance.Schutzfrage);
        Assert.Null(schutz);
        Assert.Contains("Modul A", cut.Find(".epos-rueckfrage-text").TextContent);

        cut.FindAll(".epos-rueckfrage button")[0].Click();

        Assert.False(cut.Instance.Schutzfrage);
        Assert.True(schutz);
    }

    /// <summary>„Nein" schreibt nichts.</summary>
    [Fact]
    public void Nein_auf_die_Schreibschutzfrage_schreibt_nichts()
    {
        bool geschrieben = false;
        var cut = Aufbauen(
            katalogfelder: _ => Felder(),
            katalogfelderSpeichern: (n, _, _) =>
            {
                geschrieben = true;
                return new KatalogSpeicherErgebnis(true, "Datensatz gespeichert", n);
            },
            katalogfelderGeschuetzt: _ => true);

        KatalogsatzWaehlen(cut);
        cut.Find(".epos-modulparameter input[inputmode=decimal]").Input("60000");
        Knopf(cut, "Speichern").Click();
        cut.FindAll(".epos-rueckfrage button")[1].Click();

        Assert.False(cut.Instance.Schutzfrage);
        Assert.False(geschrieben);
    }

    /// <summary>
    /// <b>Ein eigener Satz wird ohne Rückfrage geschrieben</b> — der Schreibschutzweg
    /// meldet <c>false</c>, und der Dialog fragt dann nicht.
    /// </summary>
    [Fact]
    public void Ein_eigener_Satz_wird_ohne_Rueckfrage_geschrieben()
    {
        bool? schutz = null;
        var cut = Aufbauen(
            katalogfelder: _ => Felder(),
            katalogfelderSpeichern: (n, _, s) =>
            {
                schutz = s;
                return new KatalogSpeicherErgebnis(true, "Datensatz gespeichert", n);
            },
            katalogfelderGeschuetzt: _ => false);

        KatalogsatzWaehlen(cut);
        cut.Find(".epos-modulparameter input[inputmode=decimal]").Input("60000");
        Knopf(cut, "Speichern").Click();

        Assert.False(cut.Instance.Schutzfrage);
        Assert.False(schutz);
    }

    // =====================================================================
    //  Die Knopfzeile im Modulbereich — Anwenderentscheid 16.09.2026
    // =====================================================================
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
                           katalogfelder: _ => Felder());

        KatalogsatzWaehlen(cut);

        var kinder = cut.Find(".epos-gruppenkopf-koerper").Children.ToList();

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
    /// <b>„Bearbeiten…" bleibt gesperrt, solange kein Katalogsatz gewählt ist</b> — die
    /// Sperre ist mit dem Knopf nach oben gewandert, nicht verschwunden.
    /// </summary>
    [Fact]
    public void Bearbeiten_bleibt_in_der_Knopfzeile_ohne_Katalogsatz_gesperrt()
    {
        var cut = Aufbauen();

        Assert.True(Knopf(cut, "Bearbeiten...").HasAttribute("disabled"));

        KatalogsatzWaehlen(cut);

        Assert.False(Knopf(cut, "Bearbeiten...").HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>„Brennstoff Variante" steht unmittelbar unter „Brennstoff Typ"</b>
    /// (Anwenderentscheid 16.09.2026) — beide in EINEM Block über die volle
    /// Rasterbreite, sonst stellte das zweispaltige Raster sie nebeneinander. Den TYP
    /// liefert die Hülle als erstes Feld des Detailblocks; das BHKW hatte ihn dort bis
    /// dahin gar nicht.
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
        var cut = Render<BhkwDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Modul A", 100) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.SummePtherm, () => "80")
            .Add(x => x.Varianten, _ => new[] { (5, "Erdgas E Variante") }));

        // Kein ProjektDetail: Der Detailblock ist leer, ein Paarblock entsteht nicht.
        Assert.Empty(cut.FindAll(".epos-formularraster > div.epos-feld--breit"));
        Assert.Single(cut.FindAll(".epos-formularraster select"));
        Assert.Contains("Brennstoff Variante:",
                        cut.FindAll(".epos-feld-text").Select(e => e.TextContent));
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

    // =================================================================================
    //  Stufe S2.1 / S2.3 / S2.5 - Anwenderentscheid W14a-E-10 vom 07.09.2026
    // =================================================================================

    /// <summary>Die Katalogliste - das UNTERE der beiden Raster.</summary>
    private static IReadOnlyList<AngleSharp.Dom.IElement> Katalogzeilen(
        IRenderedComponent<BhkwDialog> cut)
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

        _filterstand.Setzen(Katalogfilterprofil.SpBrennstoff, "Erdgas LL");
        cut.Render();

        Assert.Single(Katalogzeilen(cut));
        Assert.Equal("1 von 2 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);
        Assert.Contains("Modul B", Katalogzeilen(cut)[0].TextContent);

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

        _filterstand.Sortieren(Katalogfilterprofil.SpPel);
        cut.Render();
        Assert.Contains("Modul A", Katalogzeilen(cut)[0].TextContent);

        _filterstand.Sortieren(Katalogfilterprofil.SpPel);
        cut.Render();
        Assert.False(_filterstand.Aufsteigend);
        Assert.Contains("Modul B", Katalogzeilen(cut)[0].TextContent);

        _filterstand.Sortieren(Katalogfilterprofil.SpPel);
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
        _filterstand.Setzen(Katalogfilterprofil.SpBrennstoff, "Erdgas LL");
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
        var cut = Aufbauen(zeilen: new List<ErzeugerZeile> { Zeile(1, "Modul A", 100) });

        var kopf = cut.FindAll(".epos-raster")[1]
                      .QuerySelectorAll("th").Select(e => e.TextContent.Trim()).ToList();
        Assert.Contains(kopf, k => k.StartsWith("im Projekt verwendet"));

        var zeilen = Katalogzeilen(cut);
        Assert.Equal(2, zeilen.Count);
        Assert.Equal(1, zeilen.Count(z => z.QuerySelectorAll("td").Last().TextContent.Trim() == "Ja"));

        var traegt = zeilen.First(z => z.QuerySelectorAll("td").Last().TextContent.Trim() == "Ja");
        Assert.Contains("Modul A", traegt.TextContent);
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
        _filterstand.Setzen(Katalogfilterprofil.SpBrennstoff, "Erdgas LL");
        ersterAufbau.Render();
        Assert.Single(Katalogzeilen(ersterAufbau));

        // Der Dialog geht zu und wieder auf - derselbe Stand, dieselbe Sicht.
        var zweiterAufbau = Aufbauen();

        Assert.Single(Katalogzeilen(zweiterAufbau));
        Assert.Equal("1 von 2 Sätzen", zweiterAufbau.Find(".epos-katalog-treffer").TextContent);
        Assert.Single(zweiterAufbau.FindAll(".epos-katalog-ruecksetzer"));
    }

    /// <summary>Der Knopf mit DIESER Beschriftung - gleich, in welcher Leiste er steht.</summary>
    /// <remarks>
    /// Über den TEXT und nicht über den Index (15.09.2026): "Bearbeiten..." ist in den
    /// Modulbereich gewandert, die Indizes der Listenleiste haben sich dadurch
    /// verschoben. Der Text sagt, was gemeint ist.
    /// </remarks>
    private static AngleSharp.Dom.IElement Knopf(
        Bunit.IRenderedComponent<BhkwDialog> cut, string beschriftung)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == beschriftung);

    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F1)
    // =====================================================================

    /// <summary>
    /// Nach dem Zeichnen steht die Maske an der <c>KiMaskenbruecke</c>; die Brücke
    /// liest die untere Grenzleistung der gewählten Zeile und setzt sie — und der Wert
    /// steht danach in der Zeile, die der Dialog führt.
    /// </summary>
    /// <remarks>
    /// <b>Monotone Aussage</b> (Muster <c>KiMaskenhakenTests</c>): Geprüft wird, was
    /// nach dem Zeichnen DA ist. Die Brücke ist prozessweiter Zustand.
    /// </remarks>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_setzt_die_Grenzleistung()
    {
        ErzeugerZeile zeile = Zeile(1, "Modul A", 100);
        Aufbauen(zeilen: new List<ErzeugerZeile> { zeile });

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.BHKW_PROJEKT));

        KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.BHKW_PROJEKT, "grenzleistung");
        Assert.NotNull(zugang);
        Assert.Equal(50.0, zugang.Lesen());

        Assert.True(zugang.Setzbar);
        zugang.Setzen(35.0);

        Assert.Equal(35.0, zeile.Grenzleistung);
    }

    /// <summary>
    /// Der Modulname ist ANZEIGE und kein Eingabefeld — er wird gelesen und erklärt,
    /// aber nie gesetzt (<c>nurLesen</c> im Katalog).
    /// </summary>
    [Fact]
    public void Der_Modulname_ist_fuer_den_Assistenten_nur_lesbar()
    {
        Aufbauen(zeilen: new List<ErzeugerZeile> { Zeile(1, "Modul A", 100) });

        KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.BHKW_PROJEKT, "anlage");

        Assert.Equal("Modul A", zugang.Lesen());
        Assert.False(zugang.Setzbar);
    }

    // =================================================================================
    // Senken der Anlage (Anwenderentscheid 23.09.2026)
    // =================================================================================

    /// <summary>Die PROJEKTzeile zeigt ihre Senken — wie im HeizkesselDialog.</summary>
    [Fact]
    public void Die_Projektzeile_zeigt_ihre_Senken()
    {
        ErzeugerZeile zeile = Zeile(1, "Modul A", 100);
        zeile.Senken = "Senken: Heizkreis (Heizung + Warmwasser); Prozesswärme";
        var cut = Aufbauen(zeilen: new List<ErzeugerZeile> { zeile });

        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[0].Click();

        Assert.Contains(cut.FindAll(".epos-formularraster .epos-herleitung-text"),
                        e => e.TextContent == zeile.Senken);
    }

    /// <summary>Ohne Senkentext steht keine Zeile.</summary>
    [Fact]
    public void Ohne_Senkentext_steht_keine_Senkenzeile()
    {
        var cut = Aufbauen();

        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[0].Click();

        Assert.DoesNotContain(cut.FindAll(".epos-herleitung-text"),
                              e => e.TextContent.StartsWith("Senken", StringComparison.Ordinal));
    }
}
