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
        Action<bool>? geschlossen = null,
        int? projektvorgabe = null)
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
        Assert.Contains("Brennstoff:", texte);
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
}
