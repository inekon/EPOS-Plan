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
public class SolarkollektorenDialogTests : BunitContext
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
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
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

    private IRenderedComponent<SolarkollektorenDialog> Aufbauen(
        List<ErzeugerZeile>? zeilen = null,
        Func<int, AufnahmeErgebnis>? aufnehmen = null,
        Action<ErzeugerZeile>? entfernen = null,
        Action<ErzeugerZeile>? uebernehmen = null,
        Func<string, bool, IReadOnlyDictionary<string, object>>? editorGaben = null,
        Func<string, bool>? katalogLoeschen = null,
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
        Assert.Contains("Kollektor in DB ändern...", knoepfe);
        Assert.Contains("Kollektor in DB neu...", knoepfe);
        Assert.Contains("Kollektor in DB löschen", knoepfe);
        Assert.Contains("Übernehmen", knoepfe);
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
    public void Die_Kollektorgruppe_traegt_sechs_Bedienelemente()
    {
        var cut = Aufbauen();
        var kollektor = cut.FindAll(".epos-gruppenkopf-koerper")[1];

        // Anzahl, Neigung, Azimut, Vorlauf, Ruecklauf plus die gerechnete Flaeche.
        Assert.Equal(5, kollektor.QuerySelectorAll("input:not([readonly])").Length);
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

        // Reihenfolge: Anzahl, Aperturflaeche (gerechnet), Neigung, Azimut, Vorlauf, Ruecklauf.
        Assert.Equal("4", werte[0]);
        Assert.Equal("10", werte[1]);            // 2,5 m² x 4
        Assert.Equal("30", werte[2]);
        Assert.Equal("0", werte[3]);
        Assert.Equal("60", werte[4]);
        Assert.Equal("40", werte[5]);
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
    public void Uebernehmen_schreibt_die_fuenf_Ganzzahlen_und_meldet()
    {
        var zeile = Zeile(1, "Vitosol 200");
        var uebernommen = new List<ErzeugerZeile>();
        var cut = Aufbauen(new List<ErzeugerZeile> { zeile }, uebernehmen: z => uebernommen.Add(z));

        var kollektor = cut.FindAll(".epos-gruppenkopf-koerper")[1];
        kollektor.QuerySelectorAll("input")[0].Input("6");     // Anzahl
        kollektor.QuerySelectorAll("input")[2].Input("35");    // Neigung
        kollektor.QuerySelectorAll("input")[3].Input("15");    // Azimut
        kollektor.QuerySelectorAll("input")[4].Input("55");    // Vorlauf
        kollektor.QuerySelectorAll("input")[5].Input("35");    // Ruecklauf
        Knopf(cut, "Übernehmen").Click();

        Assert.Equal(6, zeile.AnzahlModule);
        Assert.Equal(35, zeile.Neigung);
        Assert.Equal(15, zeile.Azimut);
        Assert.Equal(55, zeile.Vorlauf);
        Assert.Equal(35, zeile.Ruecklauf);
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

        Assert.True(Knopf(cut, "Kollektor in DB ändern...").HasAttribute("disabled"));
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
        Knopf(cut, "Kollektor in DB ändern...").Click();

        Assert.True(cut.Instance.EditorOffen);
        Assert.Equal("Vitosol 200", name);
        Assert.False(neu);
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
}
