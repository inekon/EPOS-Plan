using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Die Seite „Kosten" (iU9-W5.4), Vorbild
/// <c>Views/BerichteKosten/UcBkKosten</c> (1 311 Z., K4 — die Oberfläche
/// stand im Code).
///
/// <para>Soll: Projektzeile, die zwei Einstiege, drei Kategorie-Karten, die
/// Anlagentabelle mit drei Spalten und Summenzeile, die Energieträgertabelle
/// mit zehn Spalten, die Zeilenfarben (rosa/gelb), die Kurztexte und die
/// Fußzeile.</para>
/// </summary>
public class KostenSeiteTests : BunitContext
{
    public KostenSeiteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        DeutscheOberflaeche();
    }

    /// <summary>
    /// Die Sprache der Oberfläche wird auf de-DE gepinnt (Muster
    /// <c>UebersichtSeiteTests</c>) — Kultur UND Thread-Kultur, damit ein Lauf
    /// unter <c>LANG=en_US.UTF-8</c> dieselben Beschriftungen und dieselbe
    /// Zahlschreibweise sieht.
    /// </summary>
    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
    }

    // ---- Probendaten -----------------------------------------------------

    private static readonly string[] SPALTEN =
    {
        "Energieträger", "Abrechnungseinheit", "Heizwert", "Arbeitspreis [€/Einheit]",
        "Arbeitspreis [€/kWh]", "Grundpreis", "Leistungspreis [€/(kW·a)]",
        "CO₂ [g/kWh]", "SO₂ [mg/kWh]", "NOx [mg/kWh]"
    };

    private static TraegerZeile Traeger(int id, string name, ZeilenArt art = ZeilenArt.Normal)
        => new TraegerZeile
        {
            TraegerId = id,
            Art = art,
            Kurztext = "verwendet von: BHKW 1",
            EmissionKurztext = "CO₂ aus Ebene „Projekt“, Modus CO2",
            Zellen = new[] { name, "kWh", "1,00", "0,3500", "0,3500", "120,00",
                             "—", "240,00", "—", "—" }
        };

    private static KostenStand Standard(bool bedienbar = true) => new KostenStand
    {
        Projektzeile = "Projekt: Musterhaus",
        Bedienbar = bedienbar,
        Kacheln = new[]
        {
            new KachelZeile { Titel = "Investition", Wert = "12.001,00 €",
                              Quelle = "abzüglich Zuschuss 1.000,00 €" },
            new KachelZeile { Titel = "Betrieb", Wert = "—" },
            new KachelZeile { Titel = "Energie", Wert = "3.400,00 €/a" }
        },
        Komponenten = new[]
        {
            new KostenZeile { Schluessel = 11, Anzeige = "Wärmepumpe — WP 1",
                              Summe = "8.000,00", Betrieb = "99,00", TraegerId = 21 },
            new KostenZeile { Schluessel = 12, Anzeige = "Heizkessel — Kessel",
                              Summe = "—", Betrieb = "—", Art = ZeilenArt.OhnePosition,
                              Kurztext = "Das Gewerk „Heizkessel“ führt keine Position." },
            new KostenZeile { Schluessel = 13, Anzeige = "Solarthermie — ohne Anlagenzuordnung",
                              Summe = "500,00", Betrieb = "—", Art = ZeilenArt.OhneZuordnung,
                              Loeschbar = true, Kurztext = "Positionen ohne Zuordnung" },
            new KostenZeile { Schluessel = 0, Anzeige = "Gesamt", Summe = "8.500,00",
                              Betrieb = "99,00", Art = ZeilenArt.Summe }
        },
        TraegerSpalten = SPALTEN,
        Traeger = new[]
        {
            Traeger(21, "Elektrische Energie"),
            Traeger(22, "Erdgas H — nicht zugeordnet", ZeilenArt.OhnePosition)
        },
        Statuszeile = "3 Investitionsposition(en)  ·  Gewerke ohne Kostenposition: Heizkessel"
    };

    private KostenStand _stand = Standard();
    private int _geladen;

    private IRenderedComponent<KostenSeite> Zeige(
        Action<Bunit.ComponentParameterCollectionBuilder<KostenSeite>>? mehr = null,
        KostenStand? stand = null)
    {
        _stand = stand ?? Standard();
        _geladen = 0;
        return Render<KostenSeite>(p =>
        {
            p.Add(x => x.Laden, () => { _geladen++; return _stand; });
            mehr?.Invoke(p);
        });
    }

    private static IReadOnlyDictionary<string, object> LeererSatz()
        => new Dictionary<string, object>();

    private static IReadOnlyList<IElement> Anlagenzeilen(IRenderedComponent<KostenSeite> cut)
        => cut.Find(".epos-kostentabelle tbody").QuerySelectorAll("tr");

    private static IReadOnlyList<IElement> Traegerzeilen(IRenderedComponent<KostenSeite> cut)
        => cut.Find(".epos-traegertabelle tbody").QuerySelectorAll("tr");

    // =====================================================================
    // Feldbestand
    // =====================================================================

    [Fact]
    public void Die_Seite_zeigt_Projektzeile_Karten_beide_Tabellen_und_die_Fusszeile()
    {
        var cut = Zeige();

        Assert.Equal("Projekt: Musterhaus", cut.Find(".epos-seite-titel").TextContent);
        Assert.Equal(3, cut.FindAll(".epos-kennzahlkachel").Count);
        Assert.Equal(4, cut.FindAll(".epos-kostentabelle thead th").Count);   // Aktionen + 3
        Assert.Equal(10, cut.FindAll(".epos-traegertabelle thead th").Count);
        Assert.Contains("Gewerke ohne Kostenposition", cut.Find(".epos-status").TextContent);
    }

    [Fact]
    public void Die_zehn_Spaltenkoepfe_der_Traegertabelle_stehen_wie_in_der_Karte()
    {
        var cut = Zeige();

        var koepfe = cut.FindAll(".epos-traegertabelle thead th");
        Assert.Equal("Energieträger", koepfe[0].TextContent);
        Assert.Equal("Arbeitspreis [€/kWh]", koepfe[4].TextContent);
        Assert.Equal("NOx [mg/kWh]", koepfe[9].TextContent);
    }

    [Fact]
    public void Die_Zeilenfarben_des_Vorlaeufers_bleiben()
    {
        var cut = Zeige();

        var zeilen = Anlagenzeilen(cut);
        Assert.Equal(4, zeilen.Count);
        Assert.Equal("", zeilen[0].ClassName);
        Assert.Contains("epos-zeile--fehlt", zeilen[1].ClassName);
        Assert.Contains("epos-zeile--lose", zeilen[2].ClassName);
        Assert.Contains("epos-zeile--summe", zeilen[3].ClassName);

        Assert.Contains("epos-zeile--fehlt", Traegerzeilen(cut)[1].ClassName);
    }

    [Fact]
    public void Die_Kurztexte_stehen_an_den_Zeilen()
    {
        var cut = Zeige();

        Assert.Contains("führt keine Position", Anlagenzeilen(cut)[1].GetAttribute("title"));
        Assert.Contains("verwendet von: BHKW 1", Traegerzeilen(cut)[0].GetAttribute("title"));
    }

    [Fact]
    public void Die_drei_Emissionsspalten_tragen_ihre_Herkunft_als_Kurztext()
    {
        var cut = Zeige();

        var zellen = Traegerzeilen(cut)[0].QuerySelectorAll("td");
        Assert.Contains("Ebene", zellen[7].GetAttribute("title"));
        Assert.Contains("Ebene", zellen[9].GetAttribute("title"));
        Assert.Contains("verwendet von", zellen[0].GetAttribute("title"));
    }

    [Fact]
    public void Summenzeile_und_Hinweiszeile_haben_keinen_Wahlknopf()
    {
        var cut = Zeige();

        Assert.Empty(Anlagenzeilen(cut)[3].QuerySelectorAll(".epos-anlagenwahl"));
        Assert.Single(Anlagenzeilen(cut)[0].QuerySelectorAll(".epos-anlagenwahl"));
    }

    [Fact]
    public void Ohne_Projekt_sind_beide_Einstiege_gesperrt()
    {
        var cut = Zeige(p => p
            .Add(x => x.VerwaltungGaben, (KostenZeile? _) => LeererSatz())
            .Add(x => x.TraegerGaben, () => LeererSatz()),
            stand: Standard(bedienbar: false));

        foreach (IElement k in cut.FindAll(".epos-kostenkopf button"))
            Assert.True(k.HasAttribute("disabled"));
    }

    /// <summary>Ohne Delegat kein Knopf (A-18 aus Welle 2).</summary>
    [Fact]
    public void Ohne_Gaben_bleiben_die_Einstiege_weg()
    {
        var cut = Zeige();

        Assert.Empty(cut.FindAll(".epos-kostenkopf button"));
    }

    // =====================================================================
    // Ä19: Anlage wählen kennzeichnet den Träger
    // =====================================================================

    [Fact]
    public void Die_Wahl_einer_Anlage_kennzeichnet_ihren_Energietraeger()
    {
        var cut = Zeige();

        Anlagenzeilen(cut)[0].QuerySelector(".epos-anlagenwahl")!.Click();

        Assert.Equal(11, cut.Instance.GewaehlteAnlage);
        Assert.Contains("epos-zeile--markiert", Traegerzeilen(cut)[0].ClassName);
        Assert.DoesNotContain("epos-zeile--markiert", Traegerzeilen(cut)[1].ClassName);
    }

    [Fact]
    public void Eine_Anlage_ohne_Traeger_kennzeichnet_nichts()
    {
        var cut = Zeige();

        Anlagenzeilen(cut)[1].QuerySelector(".epos-anlagenwahl")!.Click();

        Assert.Equal(12, cut.Instance.GewaehlteAnlage);
        Assert.Empty(cut.FindAll(".epos-zeile--markiert"));
    }

    // =====================================================================
    // Die zwei Einstiege
    // =====================================================================

    [Fact]
    public void Die_Kostenverwaltung_bekommt_die_gewaehlte_Anlage_mit()
    {
        KostenZeile? mitgegeben = null;
        bool gefragt = false;
        var cut = Zeige(p => p.Add(x => x.VerwaltungGaben, (KostenZeile? z) =>
        {
            mitgegeben = z; gefragt = true;
            return LeererSatz();
        }));

        Anlagenzeilen(cut)[0].QuerySelector(".epos-anlagenwahl")!.Click();
        cut.Find(".epos-kostenkopf button").Click();

        Assert.True(gefragt);
        Assert.NotNull(mitgegeben);
        Assert.Equal(11, mitgegeben!.Schluessel);
        Assert.Equal(KostenSeite.Unterdialog.Verwaltung, cut.Instance.OffenerUnterdialog);
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
    }

    [Fact]
    public void Die_Energietraegerverwaltung_oeffnet_ihren_Bereich()
    {
        var cut = Zeige(p => p.Add(x => x.TraegerGaben, () => LeererSatz()));

        cut.Find(".epos-kostenkopf button").Click();

        Assert.Equal(KostenSeite.Unterdialog.Traeger, cut.Instance.OffenerUnterdialog);
    }

    [Fact]
    public void Nach_dem_Schliessen_frischt_die_Seite_auf()
    {
        var cut = Zeige(p => p.Add(x => x.TraegerGaben, () => LeererSatz()));

        cut.Find(".epos-kostenkopf button").Click();
        cut.Find(".epos-ueberlagerung").KeyDown("Escape");

        Assert.Equal(KostenSeite.Unterdialog.Keins, cut.Instance.OffenerUnterdialog);
        Assert.Equal(2, _geladen);
    }

    [Fact]
    public void Ohne_Parametersatz_bleibt_der_Bereich_zu()
    {
        var cut = Zeige(p => p.Add(x => x.TraegerGaben,
            () => (IReadOnlyDictionary<string, object>?)null));

        cut.Find(".epos-kostenkopf button").Click();

        Assert.Equal(KostenSeite.Unterdialog.Keins, cut.Instance.OffenerUnterdialog);
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
    }

    // =====================================================================
    // Lose Positionen löschen (A-3: Knopf statt Doppelklick)
    // =====================================================================

    [Fact]
    public void Nur_die_gelbe_Zeile_traegt_den_Loeschknopf()
    {
        var cut = Zeige(p => p.Add(x => x.LoeschFrage, (KostenZeile z) => "wirklich löschen?"));

        Assert.Empty(Anlagenzeilen(cut)[0].QuerySelectorAll(".epos-zr-knopf"));
        Assert.Single(Anlagenzeilen(cut)[2].QuerySelectorAll(".epos-zr-knopf"));
    }

    [Fact]
    public void Der_Loeschknopf_fragt_nach_und_loescht_erst_bei_Ja()
    {
        KostenZeile? geloescht = null;
        var cut = Zeige(p => p
            .Add(x => x.LoeschFrage, (KostenZeile z) => "Alle Positionen ohne Zuordnung löschen?")
            .Add(x => x.Loeschen, (KostenZeile z) => { geloescht = z; return "2 Position(en) gelöscht."; }));

        Anlagenzeilen(cut)[2].QuerySelector(".epos-zr-knopf")!.Click();
        Assert.Contains("ohne Zuordnung löschen", cut.Find(".epos-rueckfrage-text").TextContent);

        cut.FindAll(".epos-rueckfrage .epos-leiste button")[1].Click();   // Nein
        Assert.Null(geloescht);

        Anlagenzeilen(cut)[2].QuerySelector(".epos-zr-knopf")!.Click();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();   // Ja

        Assert.NotNull(geloescht);
        Assert.Equal(13, geloescht!.Schluessel);
        Assert.Equal("2 Position(en) gelöscht.", cut.Instance.Status);
    }

    [Fact]
    public void Eine_leere_Frage_haelt_die_Rueckfrage_zu()
    {
        var cut = Zeige(p => p.Add(x => x.LoeschFrage, (KostenZeile z) => ""));

        Anlagenzeilen(cut)[2].QuerySelector(".epos-zr-knopf")!.Click();

        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
    }

    [Fact]
    public void Der_Hilfeknopf_traegt_den_Schluessel_der_alten_Maske()
    {
        var cut = Zeige();

        Assert.Equal("UcBkKosten.btn_Help", cut.Instance.HilfeSchluessel);
    }

    // =====================================================================
    // W5-O3 (Anwenderentscheid 04.09.2026): der Doppelklick als ZWEITER Weg
    // =====================================================================

    [Fact]
    public void Der_Doppelklick_auf_die_lose_Zeile_stellt_dieselbe_Rueckfrage()
    {
        var cut = Zeige(p => p
            .Add(x => x.LoeschFrage, (KostenZeile z) => "Alle Positionen ohne Zuordnung löschen?"));

        Anlagenzeilen(cut)[2].DoubleClick();

        Assert.Contains("ohne Zuordnung löschen", cut.Find(".epos-rueckfrage-text").TextContent);
    }

    [Fact]
    public void Der_Doppelklick_loescht_bei_Ja_und_nicht_bei_Nein()
    {
        KostenZeile? geloescht = null;
        var cut = Zeige(p => p
            .Add(x => x.LoeschFrage, (KostenZeile z) => "Alle Positionen ohne Zuordnung löschen?")
            .Add(x => x.Loeschen, (KostenZeile z) => { geloescht = z; return "2 Position(en) gelöscht."; }));

        Anlagenzeilen(cut)[2].DoubleClick();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[1].Click();   // Nein
        Assert.Null(geloescht);

        Anlagenzeilen(cut)[2].DoubleClick();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();   // Ja

        Assert.NotNull(geloescht);
        Assert.Equal(13, geloescht!.Schluessel);
        Assert.Equal("2 Position(en) gelöscht.", cut.Instance.Status);
    }

    [Fact]
    public void Der_Doppelklick_auf_eine_gebundene_Zeile_tut_nichts()
    {
        KostenZeile? geloescht = null;
        var cut = Zeige(p => p
            .Add(x => x.LoeschFrage, (KostenZeile z) => "wirklich löschen?")
            .Add(x => x.Loeschen, (KostenZeile z) => { geloescht = z; return "gelöscht"; }));

        Anlagenzeilen(cut)[0].DoubleClick();   // gewöhnliche Zeile
        Anlagenzeilen(cut)[1].DoubleClick();   // rosa: Gewerk ohne Position
        Anlagenzeilen(cut)[3].DoubleClick();   // Summenzeile

        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
        Assert.Null(geloescht);
    }

    // =====================================================================
    // Befund W5-B-1: die Aktionsspalte war auf dem Bildschirm nicht zu sehen
    // =====================================================================

    [Fact]
    public void Der_Papierkorb_der_losen_Zeile_ist_gerendert_und_traegt_seinen_Kurztext()
    {
        var cut = Zeige(p => p.Add(x => x.LoeschFrage, (KostenZeile z) => "wirklich löschen?"));

        IElement knopf = Anlagenzeilen(cut)[2].QuerySelector(".epos-zellenaktionen .epos-zr-knopf")!;

        Assert.Equal("Positionen ohne Anlagenzuordnung löschen", knopf.GetAttribute("title"));
        Assert.Contains("🗑", knopf.TextContent);
    }

    [Fact]
    public void Die_Aktionsspalte_ist_eine_echte_Tabellenzelle_mit_eigenem_Flexkasten()
    {
        var cut = Zeige(p => p.Add(x => x.LoeschFrage, (KostenZeile z) => "wirklich löschen?"));

        // Der Spaltenkopf ist BESCHRIFTET — eine leere Spalte hat nichts,
        // woraus sie ihre Breite nehmen könnte (Befund W5-B-1).
        IElement kopf = cut.FindAll(".epos-kostentabelle thead th")[0];
        Assert.Equal("Aktionen", kopf.TextContent);
        Assert.Contains("epos-zellenaktionen", kopf.ClassName);

        // Die Zelle ist ein <td> und trägt den Flexkasten IN sich, nicht auf sich.
        IElement zelle = Anlagenzeilen(cut)[2].QuerySelector("td.epos-zellenaktionen")!;
        IElement inhalt = zelle.QuerySelector(".epos-zellenaktionen-inhalt")!;

        Assert.Equal(2, inhalt.QuerySelectorAll("button").Length);   // Wahl + Papierkorb
        Assert.Single(inhalt.QuerySelectorAll(".epos-anlagenwahl"));
        Assert.Single(inhalt.QuerySelectorAll(".epos-zr-knopf"));
    }

    /// <summary>
    /// Die Regressionswache zu W5-B-1. Der Fehler stand NUR im Stilblatt —
    /// keine bunit-Probe hätte ihn je gesehen, denn das Markup war richtig.
    /// Deshalb liest dieser Test das Stilblatt selbst: <c>display:flex</c> auf
    /// einem <c>&lt;td&gt;</c> nimmt der Zelle ihre Rolle als Tabellenzelle
    /// (CSS 2.1, 17.2.1), und ein Aktionsknopf darf nie erst bei
    /// <c>:hover</c> erscheinen — Berührung kennt kein Hover (iL4).
    /// </summary>
    [Fact]
    public void Die_Aktionszelle_traegt_im_Stilblatt_kein_display_flex()
    {
        string block = Stilblock(".epos-zellenaktionen {");

        Assert.DoesNotContain("display", block);
        Assert.DoesNotContain("hover", block);
        Assert.Contains("width", block);

        // Der Flexkasten steht in der EIGENEN Klasse.
        Assert.Contains("display: flex", Stilblock(".epos-zellenaktionen-inhalt {"));
    }

    /// <summary>Liest den Rumpf einer Regel aus <c>EPOS.UI/wwwroot/epos-ui.css</c>.</summary>
    private static string Stilblock(string selektor)
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);   // das Stilblatt muss im Baum stehen
        string css = File.ReadAllText(Path.Combine(d!.FullName, "EPOS.UI", "wwwroot", "epos-ui.css"));

        int a = css.IndexOf(selektor, StringComparison.Ordinal);
        Assert.True(a >= 0, $"Regel {selektor} steht nicht im Stilblatt");
        int e = css.IndexOf('}', a);
        Assert.True(e > a);
        return css.Substring(a + selektor.Length, e - a - selektor.Length);
    }
}
