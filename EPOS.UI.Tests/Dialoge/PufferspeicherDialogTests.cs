using System.Globalization;
using Bunit;
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
/// Verwaltung Pufferspeicher (iU9-W6.7). Soll ist die Feldkarte von
/// <c>Form_PufferSp</c>: zwei Listen, zwei Filter (Hersteller und Volumen), der
/// Detailblock und die Eindeutigkeitsrückfrage vor dem Aufnehmen.
/// </summary>
public class PufferspeicherDialogTests : EposBunitContext
{

    /// <summary>
    /// Der Filterstand DIESES Prüfstands. Ohne ihn nähme der Dialog den aus dem
    /// <c>Katalogfilterregister</c> — der lebt prozessweit, und xunit fährt
    /// Testklassen nebeneinander. Dass das Register wirklich teilt, prüft
    /// <c>KatalogfilterstandTests</c>.
    /// </summary>
    private readonly Katalogfilterstand _filterstand = new();
    /// <summary>
    /// Das PROFIL des Projektdialogs (W14a-E-10 / S2.1): dieselben fünf Spalten wie
    /// in der Verwaltung, dazu die sechste „im Projekt verwendet" (Q12).
    /// </summary>
    private static readonly Katalogfilterprofil Profil =
        Katalogfilterprofil.MitVerwendung(Anlagenart.Pufferspeicher,
            s => Resource.ResourceManager.GetString(s) ?? s);

    private static IReadOnlyList<Katalogfilterzeile> Katalogzeilen() => new[]
    {
        new Katalogfilterzeile(51, "Speicher 600 Ltr")
            .MitText(Katalogfilterprofil.SpBezeichner, "Speicher 600 Ltr")
            .MitText(Katalogfilterprofil.SpHersteller, "Musterwerk")
            .MitText(Katalogfilterprofil.SpSpeichertyp, "stehend")
            .MitZahl(Katalogfilterprofil.SpVolumen, 600.0, 0)
            .MitZahl(Katalogfilterprofil.SpVerluste, 1.8, 2),

        new Katalogfilterzeile(52, "Speicher 800 Ltr")
            .MitText(Katalogfilterprofil.SpBezeichner, "Speicher 800 Ltr")
            .MitText(Katalogfilterprofil.SpHersteller, "Musterwerk")
            .MitText(Katalogfilterprofil.SpSpeichertyp, "stehend")
            .MitZahl(Katalogfilterprofil.SpVolumen, 800.0, 0)
            .MitZahl(Katalogfilterprofil.SpVerluste, 2.1, 2),
    };

    public PufferspeicherDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static ErzeugerZeile Zeile(int schluessel, string name, int geraetId)
        => new() { Schluessel = schluessel, Bezeichner = name, GeraetId = geraetId };

    private static ErzeugerDetail Detail(string name) => new(
        name, "",
        new[] { ("Hersteller:", "Musterwerk"), ("Speichertyp:", "stehend"),
                ("Bereitschaftsverluste:", "1,5"), ("Gesamtvolumen [l]:", "600,0"),
                ("Investitionskosten [€]:", "2500,0") });

    private IRenderedComponent<PufferspeicherDialog> Aufbauen(
        List<ErzeugerZeile>? zeilen = null,
        Func<int, string>? dublettenfrage = null,
        Func<int, bool, AufnahmeErgebnis>? aufnehmen = null,
        Action<ErzeugerZeile>? entfernen = null,
        Func<int, ErzeugerDetail?>? projektDetail = null,
        Func<int, bool>? katalogLoeschen = null,
        Func<IReadOnlyDictionary<string, object>>? verwaltung = null,
        Action<bool>? geschlossen = null)
    {
        return Render<PufferspeicherDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<ErzeugerZeile> { Zeile(1, "Speicher 600 Liter", 51) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.KatalogDetail, id => Detail("Speicher " + id))
            .Add(x => x.ProjektDetail, projektDetail ?? (id => Detail("Projektkopie " + id)))
            .Add(x => x.Dublettenfrage, dublettenfrage ?? (_ => ""))
            .Add(x => x.Aufnehmen, aufnehmen ??
                 ((id, _) => new AufnahmeErgebnis(Zeile(9, "Speicher 800 Ltr", id))))
            .Add(x => x.Entfernen, entfernen)
            .Add(x => x.KatalogLoeschen, katalogLoeschen ?? (_ => true))
            .Add(x => x.VerwaltungGaben, verwaltung)
            .Add(x => x.Geschlossen, ok => geschlossen?.Invoke(ok)));
    }

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Der_Feldbestand_der_Karte_steht()
    {
        var cut = Aufbauen();

        Assert.Equal(2, cut.FindAll(".epos-raster").Count);
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-uebernahme button").Count);

        // W14a-E-10 / S2.1: Die zwei Klapplisten sind weg - Hersteller und
        // Speichertyp sind Spalten mit Trichter, und "200..500" im Volumenfeld
        // leistet genauer, was die sechs festen Stufen ungefaehr taten.
        Assert.Empty(cut.FindAll("select"));

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.DoesNotContain("Filtern nach Hersteller:", texte);
        Assert.DoesNotContain("Filtern nach Volumen:", texte);
        Assert.Single(cut.FindAll(".epos-katalog-suchzeile"));

        // Sechs NUR LESBARE Anzeigefelder: Name, Hersteller, Typ, Verluste, Volumen,
        // Investitionskosten.
        Assert.Equal(6, cut.FindAll(".epos-gruppenkopf-koerper input[readonly]").Count);
    }

    /// <summary>
    /// <b>Die sechs festen Volumenstufen sind gefallen</b> (Frage Q5, Stufe S2.1):
    /// „Sortieren nach Volumen und der Ausdruck <c>200..500</c> leisten dasselbe
    /// genauer" — und lösen den Einwand, dass eine Stufe „bis 500 l" für einen
    /// 480‑l‑Speicher jede Zeile darunter mitbringt. An ihre Stelle tritt die
    /// Zahlenspalte mit ihrem Trichter.
    /// </summary>
    [Fact]
    public void Die_Volumenspalte_traegt_einen_Trichter_statt_sechs_Stufen()
    {
        var cut = Aufbauen();

        var kopf = cut.FindAll(".epos-raster")[1].QuerySelectorAll("th")
                      .Select(e => e.TextContent.Trim()).ToList();

        Assert.Contains(kopf, k => k.Contains("[l]"));     // die Volumenspalte, Kopf „V [l]"
        Assert.Equal(Profil.Spalten.Count + 1, kopf.Count);

        // Fuenf Trichter: alles ausser der Wahlspalte und dem Kennzeichen Q12.
        Assert.Equal(Profil.Spalten.Count(x => x.Filterbar), cut.FindAll(".epos-trichter").Count);
    }

    /// <summary>
    /// Ohne Parametersatz der Speicherverwaltung kein Knopf — Hausregel. Seit
    /// iU9-W14a.4 ist die Verwaltung eine ÜBERLAGERUNG im selben Fenster.
    /// </summary>
    [Fact]
    public void Der_Bearbeiten_Knopf_erscheint_nur_mit_Verwaltungsgaben()
    {
        var ohne = Aufbauen();
        Assert.DoesNotContain(ohne.FindAll("button").Select(b => b.TextContent), t => t == "Bearbeiten...");

        var mit = Aufbauen(verwaltung: () => Verwaltungsgaben());
        Assert.Contains(mit.FindAll("button").Select(b => b.TextContent), t => t == "Bearbeiten...");
    }

    /// <summary>Ein Mindestsatz für die Überlagerung — der Browser braucht sein Profil.</summary>
    private static IReadOnlyDictionary<string, object> Verwaltungsgaben()
        => new Dictionary<string, object>
        {
            ["Art"] = WindowsFormsApplication1.KatalogBrowserArt.Pufferspeicher,
            ["Wege"] = new EPOS.UI.Dialoge.Erzeuger.KatalogBrowserWege()
        };

    // =================================================================================
    // Detailquellen
    // =================================================================================

    [Fact]
    public void Eine_Projektzeile_zeigt_ihre_Kopie_ein_Katalogsatz_den_Stamm()
    {
        // Befund 4: Die Projektkopie kann anders heissen als die Vorlage.
        var cut = Aufbauen();

        Assert.Equal("Projektkopie 51",
                     cut.FindAll(".epos-gruppenkopf-koerper input[readonly]")[0].GetAttribute("value"));

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();

        Assert.Equal("Speicher 51",
                     cut.FindAll(".epos-gruppenkopf-koerper input[readonly]")[0].GetAttribute("value"));
    }

    // =================================================================================
    // Die Eindeutigkeitsrueckfrage
    // =================================================================================

    [Fact]
    public void Ein_neues_Geraet_wird_ohne_Rueckfrage_aufgenommen()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Speicher 600 Liter", 51) };
        var cut = Aufbauen(zeilen);

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[1].Click();
        cut.FindAll(".epos-zweispalten-uebernahme button")[0].Click();

        Assert.False(cut.Instance.Dublettenwarnung);
        Assert.Equal(2, zeilen.Count);
    }

    [Fact]
    public void Ein_zweites_gleiches_Geraet_loest_die_Rueckfrage_aus()
    {
        bool aufgenommen = false;
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Speicher 600 Ltr", 51) };
        var cut = Aufbauen(zeilen,
            dublettenfrage: _ => "Speicher 600 Ltr steht bereits in der Liste. Trotzdem aufnehmen?",
            aufnehmen: (id, _) => { aufgenommen = true; return new AufnahmeErgebnis(Zeile(9, "x", id)); });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-zweispalten-uebernahme button")[0].Click();

        Assert.True(cut.Instance.Dublettenwarnung);
        Assert.False(aufgenommen);
        Assert.Contains("steht bereits", cut.Find(".epos-rueckfrage-text").TextContent);
    }

    [Fact]
    public void Nein_auf_die_Rueckfrage_fuegt_nichts_hinzu()
    {
        bool aufgenommen = false;
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Speicher 600 Ltr", 51) };
        var cut = Aufbauen(zeilen, dublettenfrage: _ => "steht bereits",
            aufnehmen: (id, _) => { aufgenommen = true; return new AufnahmeErgebnis(Zeile(9, "x", id)); });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-zweispalten-uebernahme button")[0].Click();
        cut.FindAll(".epos-rueckfrage button")[1].Click();

        Assert.False(aufgenommen);
        Assert.Single(zeilen);
    }

    [Fact]
    public void Ja_auf_die_Rueckfrage_erzwingt_die_Geraetekopie()
    {
        bool? erzwungen = null;
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Speicher 600 Ltr", 51) };
        var cut = Aufbauen(zeilen, dublettenfrage: _ => "steht bereits",
            aufnehmen: (id, e) => { erzwungen = e; return new AufnahmeErgebnis(Zeile(9, "x", id)); });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-zweispalten-uebernahme button")[0].Click();
        cut.FindAll(".epos-rueckfrage button")[0].Click();

        Assert.True(erzwungen);
        Assert.Equal(2, zeilen.Count);
    }

    // =================================================================================
    // Entfernen und Katalogpflege
    // =================================================================================

    [Fact]
    public void Der_Pfeil_zurueck_trifft_genau_die_gewaehlte_Zeile()
    {
        // Befund 4: Der Vorlaeufer brauchte dafuer eine Parallelliste - Items.Remove(Text)
        // traf bei gleichnamigen Eintraegen immer den ersten.
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Speicher 600 Ltr", 51),
                                               Zeile(2, "Speicher 600 Ltr", 51) };
        var entfernt = new List<ErzeugerZeile>();
        var cut = Aufbauen(zeilen, entfernen: z => entfernt.Add(z));

        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[1].Click();
        cut.FindAll(".epos-zweispalten-uebernahme button")[1].Click();

        Assert.Single(zeilen);
        Assert.Equal(1, zeilen[0].Schluessel);
        Assert.Equal(2, entfernt[0].Schluessel);
    }

    [Fact]
    public void Loeschen_ohne_Katalogwahl_sagt_es()
    {
        // PSP_MELDUNG_MODUL_WAEHLEN - der Vorlaeufer meldete das ebenfalls.
        var cut = Aufbauen();

        cut.FindAll(".epos-zweispalten-spalte")[1].QuerySelectorAll(".epos-leiste button")[0].Click();

        Assert.Contains("Modul", cut.Instance.Meldung);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
    }

    [Fact]
    public void Loeschen_geht_ueber_die_Katalog_Id()
    {
        // V0-9: Der fruehere Weg ueber den Bezeichner traf bei gleichnamigen
        // Katalogeintraegen alle Namensvettern auf einmal.
        var geloescht = new List<int>();
        var cut = Aufbauen(katalogLoeschen: id => { geloescht.Add(id); return true; });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[1].Click();
        cut.FindAll(".epos-zweispalten-spalte")[1].QuerySelectorAll(".epos-leiste button")[0].Click();
        cut.FindAll(".epos-rueckfrage button")[0].Click();

        Assert.Equal(new[] { 52 }, geloescht);
    }

    /// <summary>
    /// „Bearbeiten…" öffnet die Speicherverwaltung als ÜBERLAGERUNG im selben
    /// Fenster — bis iU9-W14a war es ein Sprung in ein zweites Fenster
    /// (<c>Sprungziel.PufferSpAdmin</c>).
    /// </summary>
    [Fact]
    public void Bearbeiten_oeffnet_die_Speicherverwaltung_als_Ueberlagerung()
    {
        var cut = Aufbauen(verwaltung: () => Verwaltungsgaben());

        Assert.False(cut.Instance.VerwaltungOffen);
        cut.FindAll(".epos-zweispalten-spalte")[1].QuerySelectorAll(".epos-leiste button")[0].Click();

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
        IRenderedComponent<PufferspeicherDialog> cut)
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

        _filterstand.Setzen(Katalogfilterprofil.SpVolumen, "700..900");
        cut.Render();

        Assert.Single(Katalogzeilen(cut));
        Assert.Equal("1 von 2 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);
        Assert.Contains("Speicher 800 Ltr", Katalogzeilen(cut)[0].TextContent);

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

        _filterstand.Sortieren(Katalogfilterprofil.SpVolumen);
        cut.Render();
        Assert.Contains("Speicher 600 Ltr", Katalogzeilen(cut)[0].TextContent);

        _filterstand.Sortieren(Katalogfilterprofil.SpVolumen);
        cut.Render();
        Assert.False(_filterstand.Aufsteigend);
        Assert.Contains("Speicher 800 Ltr", Katalogzeilen(cut)[0].TextContent);

        _filterstand.Sortieren(Katalogfilterprofil.SpVolumen);
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
        _filterstand.Setzen(Katalogfilterprofil.SpVolumen, "700..900");
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
        var cut = Aufbauen(zeilen: new List<ErzeugerZeile> { Zeile(1, "Speicher 600 Ltr", 51) });

        var kopf = cut.FindAll(".epos-raster")[1]
                      .QuerySelectorAll("th").Select(e => e.TextContent.Trim()).ToList();
        Assert.Contains(kopf, k => k.StartsWith("im Projekt verwendet"));

        var zeilen = Katalogzeilen(cut);
        Assert.Equal(2, zeilen.Count);
        Assert.Equal(1, zeilen.Count(z => z.QuerySelectorAll("td").Last().TextContent.Trim() == "Ja"));

        var traegt = zeilen.First(z => z.QuerySelectorAll("td").Last().TextContent.Trim() == "Ja");
        Assert.Contains("Speicher 600 Ltr", traegt.TextContent);
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
        _filterstand.Setzen(Katalogfilterprofil.SpVolumen, "700..900");
        ersterAufbau.Render();
        Assert.Single(Katalogzeilen(ersterAufbau));

        // Der Dialog geht zu und wieder auf - derselbe Stand, dieselbe Sicht.
        var zweiterAufbau = Aufbauen();

        Assert.Single(Katalogzeilen(zweiterAufbau));
        Assert.Equal("1 von 2 Sätzen", zweiterAufbau.Find(".epos-katalog-treffer").TextContent);
        Assert.Single(zweiterAufbau.FindAll(".epos-katalog-ruecksetzer"));
    }
}
