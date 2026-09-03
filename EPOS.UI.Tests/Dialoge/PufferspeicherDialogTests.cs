using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Allgemein;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Verwaltung Pufferspeicher (iU9-W6.7). Soll ist die Feldkarte von
/// <c>Form_PufferSp</c>: zwei Listen, zwei Filter (Hersteller und Volumen), der
/// Detailblock und die Eindeutigkeitsrückfrage vor dem Aufnehmen.
/// </summary>
public class PufferspeicherDialogTests : BunitContext
{
    private static readonly string[] Hersteller = { "Alle", "Musterwerk" };
    private static readonly string[] Volumen = { "Alle", "bis 100 l", "100 bis 200 l" };

    private static readonly KatalogZeile[] Katalog =
    {
        new(51, "Speicher 600 Ltr"), new(52, "Speicher 800 Ltr")
    };

    public PufferspeicherDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
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
        Func<string, Task<bool>>? sprung = null,
        Action<bool>? geschlossen = null)
    {
        return Render<PufferspeicherDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<ErzeugerZeile> { Zeile(1, "Speicher 600 Liter", 51) })
            .Add(x => x.Hersteller, Hersteller)
            .Add(x => x.Volumenstufen, Volumen)
            .Add(x => x.Filtern, (_, _) => Katalog)
            .Add(x => x.KatalogDetail, id => Detail("Speicher " + id))
            .Add(x => x.ProjektDetail, projektDetail ?? (id => Detail("Projektkopie " + id)))
            .Add(x => x.Dublettenfrage, dublettenfrage ?? (_ => ""))
            .Add(x => x.Aufnehmen, aufnehmen ??
                 ((id, _) => new AufnahmeErgebnis(Zeile(9, "Speicher 800 Ltr", id))))
            .Add(x => x.Entfernen, entfernen)
            .Add(x => x.KatalogLoeschen, katalogLoeschen ?? (_ => true))
            .Add(x => x.Sprung, sprung)
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
        Assert.Equal(2, cut.FindAll(".epos-auswahlpfeile button").Count);
        Assert.Equal(2, cut.FindAll("select").Count);        // Hersteller, Volumen

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.Contains("Filtern nach Hersteller:", texte);
        Assert.Contains("Filtern nach Volumen:", texte);

        // Sechs NUR LESBARE Anzeigefelder: Name, Hersteller, Typ, Verluste, Volumen,
        // Investitionskosten.
        Assert.Equal(6, cut.FindAll(".epos-gruppenkopf-koerper input[readonly]").Count);
    }

    [Fact]
    public void Die_sechs_Volumenstufen_stehen_als_Auswahl()
    {
        var cut = Aufbauen();

        var eintraege = cut.FindAll("select")[1].QuerySelectorAll("option")
                           .Select(e => e.TextContent).ToList();
        Assert.Equal(Volumen, eintraege);
    }

    [Fact]
    public void Der_Bearbeiten_Knopf_erscheint_nur_mit_Sprung()
    {
        var ohne = Aufbauen();
        Assert.DoesNotContain(ohne.FindAll("button").Select(b => b.TextContent), t => t == "Bearbeiten...");

        var mit = Aufbauen(sprung: _ => Task.FromResult(true));
        Assert.Contains(mit.FindAll("button").Select(b => b.TextContent), t => t == "Bearbeiten...");
    }

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
        cut.FindAll(".epos-auswahlpfeile button")[0].Click();

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
        cut.FindAll(".epos-auswahlpfeile button")[0].Click();

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
        cut.FindAll(".epos-auswahlpfeile button")[0].Click();
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
        cut.FindAll(".epos-auswahlpfeile button")[0].Click();
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
        cut.FindAll(".epos-auswahlpfeile button")[1].Click();

        Assert.Single(zeilen);
        Assert.Equal(1, zeilen[0].Schluessel);
        Assert.Equal(2, entfernt[0].Schluessel);
    }

    [Fact]
    public void Loeschen_ohne_Katalogwahl_sagt_es()
    {
        // PSP_MELDUNG_MODUL_WAEHLEN - der Vorlaeufer meldete das ebenfalls.
        var cut = Aufbauen();

        cut.FindAll(".epos-auswahlspalte")[1].QuerySelectorAll(".epos-leiste button")[0].Click();

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
        cut.FindAll(".epos-auswahlspalte")[1].QuerySelectorAll(".epos-leiste button")[0].Click();
        cut.FindAll(".epos-rueckfrage button")[0].Click();

        Assert.Equal(new[] { 52 }, geloescht);
    }

    [Fact]
    public void Bearbeiten_springt_in_die_Speicherverwaltung()
    {
        string? ziel = null;
        var cut = Aufbauen(sprung: s => { ziel = s; return Task.FromResult(true); });

        cut.FindAll(".epos-auswahlspalte")[1].QuerySelectorAll(".epos-leiste button")[0].Click();

        Assert.Equal(Sprungziel.PufferSpAdmin, ziel);
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
}
