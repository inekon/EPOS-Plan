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
/// Verwaltung Photovoltaik Module (iU9-W6.5). Soll ist die Feldkarte von
/// <c>Form_PV</c> — mit der Berichtigung aus R‑W6‑7: Die Karte ordnet die drei
/// Panel-Beschriftungen falsch zu; maßgeblich ist der Designer (Neigung [°],
/// Azimut [°], Anzahl Module).
/// </summary>
public class PhotovoltaikDialogTests : BunitContext
{
    private static readonly string[] Hersteller = { "Alle", "Musterwerk", "Solar AG" };

    private static readonly KatalogZeile[] Katalog =
    {
        new(31, "Modul 400"), new(32, "Modul 500")
    };

    public PhotovoltaikDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static ErzeugerZeile Zeile(int schluessel, string name, int geraetId)
        => new() { Schluessel = schluessel, Bezeichner = name, GeraetId = geraetId,
                   Neigung = 30, Azimut = 180, AnzahlModule = 20 };

    private static ErzeugerDetail Detail(string name) => new(
        name, "Beschreibung",
        new[] { ("Hersteller:", "Musterwerk"), ("Modul Leistung [KW]:", "0,40") });

    private IRenderedComponent<PhotovoltaikDialog> Aufbauen(
        List<ErzeugerZeile>? zeilen = null,
        Func<int, AufnahmeErgebnis>? aufnehmen = null,
        Action<ErzeugerZeile>? entfernen = null,
        Action<ErzeugerZeile>? uebernehmen = null,
        Func<string>? gesamt = null,
        Func<int, bool>? katalogLoeschen = null,
        Func<IReadOnlyDictionary<string, object>>? verwaltung = null,
        bool wizard = false,
        Action<bool>? geschlossen = null)
    {
        return Render<PhotovoltaikDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<ErzeugerZeile> { Zeile(1, "Modul 400", 31) })
            .Add(x => x.Hersteller, Hersteller)
            .Add(x => x.Filtern, _ => Katalog)
            .Add(x => x.Detail, n => Detail(n))
            .Add(x => x.Aufnehmen, aufnehmen ?? (_ => new AufnahmeErgebnis(Zeile(9, "Modul 500", 32))))
            .Add(x => x.Entfernen, entfernen)
            .Add(x => x.Uebernehmen, uebernehmen)
            .Add(x => x.Gesamtleistung, gesamt ?? (() => "8"))
            .Add(x => x.KatalogLoeschen, katalogLoeschen ?? (_ => true))
            .Add(x => x.VerwaltungGaben, verwaltung)
            .Add(x => x.Wizard, wizard)
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
        cut.FindAll(".epos-auswahlpfeile button")[0].Click();

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
        cut.FindAll(".epos-auswahlpfeile button")[1].Click();

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

    // =================================================================================
    // Katalogpflege und Tastatur
    // =================================================================================

    [Fact]
    public void Loeschen_fragt_zuerst_nach()
    {
        var geloescht = new List<int>();
        var cut = Aufbauen(katalogLoeschen: id => { geloescht.Add(id); return true; });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-auswahlspalte")[1].QuerySelectorAll(".epos-leiste button")[0].Click();

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
        cut.FindAll(".epos-auswahlspalte")[1].QuerySelectorAll(".epos-leiste button")[0].Click();

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
}
