using Bunit;
using EPOS.UI.Dialoge.Berichte;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Übernahmedialog der Seite „Übersicht" (iU9-W5.1), Vorbild
/// <c>Views/BerichteKosten/Form_BkUebernahme</c> (13 Kartenzeilen).
///
/// <para>Soll ist die Feldkarte: Kopf mit Gegenstand, die Quellenauswahl, je
/// nach Fall die Wertgegenüberstellung (Quelle/Ziel) oder die
/// Klartext-Zusammenfassung, die Komponentenzeile, die Begründung und die
/// Schlussleiste aus Abbrechen und OK.</para>
/// </summary>
public class BkUebernahmeDialogTests : BunitContext
{
    public BkUebernahmeDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // ---- Probendaten -----------------------------------------------------

    private static readonly UebernahmeQuelle[] QUELLEN =
    {
        new UebernahmeQuelle { Id = 1030, Anzeige = "Stammprojekt „Musterhaus\"" },
        new UebernahmeQuelle { Id = 1031, Anzeige = "Variante „Kessel groß\"" }
    };

    private int _gefragt;

    private static UebernahmeVorschau Moeglich(int id) => new UebernahmeVorschau
    {
        Moeglich = true,
        WertQuelle = "12,5 kW",
        WertZiel = "9,0 kW",
        Komponenten = "Quelle: WP 1  →  Ziel: WP 1"
    };

    private IRenderedComponent<BkUebernahmeDialog> Zeige(
        Action<Bunit.ComponentParameterCollectionBuilder<BkUebernahmeDialog>>? mehr = null,
        Func<int, UebernahmeVorschau>? lader = null,
        bool mitKlartext = false)
    {
        _gefragt = 0;
        return Render<BkUebernahmeDialog>(p =>
        {
            p.Add(x => x.TitelText, "Merkmal übernehmen");
            p.Add(x => x.Gegenstand, "Wärmepumpe · Nennleistung");
            p.Add(x => x.ZielName, "Kessel groß");
            p.Add(x => x.Quellen, QUELLEN);
            p.Add(x => x.MitKlartext, mitKlartext);
            p.Add(x => x.Lader, lader ?? (id => { _gefragt = id; return Moeglich(id); }));
            mehr?.Invoke(p);
        });
    }

    // =====================================================================
    // Feldbestand (Feldkarte)
    // =====================================================================

    [Fact]
    public void Der_Dialog_zeigt_Kopf_Quellenwahl_Werte_und_Schlussleiste()
    {
        var cut = Zeige();

        Assert.Equal("Merkmal übernehmen", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal("Wärmepumpe · Nennleistung", cut.Find(".epos-kontextzeile").TextContent);
        Assert.Single(cut.FindAll("select"));                      // Quelle
        Assert.Equal(3, cut.FindAll(".epos-kohaerenz").Count);     // Quellwert, Ziel, Zielwert
        Assert.Equal(2, cut.FindAll(".epos-leiste button").Count); // Abbrechen, OK
    }

    [Fact]
    public void Die_Quellen_stehen_in_der_Reihenfolge_der_Huelle()
    {
        var cut = Zeige();

        var eintraege = cut.Find("select").QuerySelectorAll("option");
        Assert.Equal(2, eintraege.Length);
        Assert.Equal("Stammprojekt „Musterhaus\"", eintraege[0].TextContent);
        Assert.Equal("Variante „Kessel groß\"", eintraege[1].TextContent);
    }

    /// <summary>Der Vorläufer setzte <c>cbQuelle.SelectedIndex = 0</c>.</summary>
    [Fact]
    public void Die_erste_Quelle_ist_die_Vorgabe_und_wird_sofort_geladen()
    {
        var cut = Zeige();

        Assert.Equal(1030, cut.Instance.GewaehlteQuelleId);
        Assert.Equal(1030, _gefragt);
    }

    [Fact]
    public void Ein_Quellenwechsel_laedt_die_Vorschau_neu()
    {
        var cut = Zeige();

        cut.Find("select").Change("1031");

        Assert.Equal(1031, cut.Instance.GewaehlteQuelleId);
        Assert.Equal(1031, _gefragt);
    }

    [Fact]
    public void Die_Werte_von_Quelle_und_Ziel_stehen_gegenueber()
    {
        var cut = Zeige();

        var zeilen = cut.FindAll(".epos-kohaerenz");
        Assert.Contains("12,5 kW", zeilen[0].TextContent);
        Assert.Contains("Kessel groß", zeilen[1].TextContent);
        Assert.Contains("9,0 kW", zeilen[2].TextContent);
    }

    [Fact]
    public void Ein_leerer_Wert_erscheint_als_Gedankenstrich()
    {
        var cut = Zeige(lader: id => new UebernahmeVorschau { Moeglich = true });

        Assert.Contains("—", cut.FindAll(".epos-kohaerenz")[0].TextContent);
    }

    [Fact]
    public void Die_betroffenen_Komponenten_stehen_als_Herleitung()
    {
        var cut = Zeige();

        Assert.Equal("Quelle: WP 1  →  Ziel: WP 1",
                     cut.Find(".epos-herleitung-text").TextContent);
    }

    [Fact]
    public void Ohne_Komponentenzeile_bleibt_die_Herleitung_weg()
    {
        var cut = Zeige(lader: id => new UebernahmeVorschau { Moeglich = true });

        Assert.Empty(cut.FindAll(".epos-herleitung"));
    }

    // =====================================================================
    // Die zweite Füllung: Klartext (Komponenten-Übernahme)
    // =====================================================================

    [Fact]
    public void Im_Klartextmodus_steht_die_Zusammenfassung_statt_der_zwei_Werte()
    {
        var cut = Zeige(mitKlartext: true, lader: id => new UebernahmeVorschau
        {
            Moeglich = true,
            Klartext = "anlegen: 2\nersetzen: 1\nentfernen: 0"
        });

        Assert.Empty(cut.FindAll(".epos-kohaerenz"));
        var felder = cut.FindAll("textarea");
        Assert.Single(felder);
        Assert.Contains("anlegen: 2", felder[0].TextContent);
    }

    [Fact]
    public void Im_Klartextmodus_ist_das_Zielfeld_nur_lesend()
    {
        var cut = Zeige(mitKlartext: true, lader: id => new UebernahmeVorschau { Moeglich = true });

        var ziel = cut.Find("input[type=text]");
        Assert.Equal("Kessel groß", ziel.GetAttribute("value"));
        Assert.True(ziel.HasAttribute("readonly"));
    }

    // =====================================================================
    // Sperre und Begründung
    // =====================================================================

    [Fact]
    public void Eine_unmoegliche_Uebernahme_sperrt_OK_und_nennt_den_Grund()
    {
        var cut = Zeige(lader: id => new UebernahmeVorschau
        {
            Moeglich = false,
            Grund = "Quelle und Ziel stimmen bereits überein."
        });

        Assert.True(cut.FindAll(".epos-leiste button")[1].HasAttribute("disabled"));
        Assert.Contains("stimmen bereits überein", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Ohne_Lader_bleibt_der_Dialog_gesperrt_und_meldet_es()
    {
        var cut = Render<BkUebernahmeDialog>(p => p
            .Add(x => x.Quellen, QUELLEN)
            .Add(x => x.MeldungKeineQuelle, "keine Quelle"));

        Assert.True(cut.FindAll(".epos-leiste button")[1].HasAttribute("disabled"));
        Assert.Contains("keine Quelle", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Ohne_Quelle_bleibt_der_Dialog_gesperrt()
    {
        var cut = Render<BkUebernahmeDialog>(p => p
            .Add(x => x.Quellen, Array.Empty<UebernahmeQuelle>())
            .Add(x => x.Lader, id => Moeglich(id)));

        Assert.Equal(-1, cut.Instance.GewaehlteQuelleId);
        Assert.True(cut.FindAll(".epos-leiste button")[1].HasAttribute("disabled"));
    }

    // =====================================================================
    // Abschluss
    // =====================================================================

    [Fact]
    public void OK_meldet_die_gewaehlte_Quelle()
    {
        BkUebernahmeErgebnis? ergebnis = null;
        var cut = Zeige(p => p.Add(x => x.Geschlossen, (BkUebernahmeErgebnis? e) => ergebnis = e));

        cut.Find("select").Change("1031");
        cut.FindAll(".epos-leiste button")[1].Click();

        Assert.NotNull(ergebnis);
        Assert.Equal(1031, ergebnis!.QuelleId);
    }

    [Fact]
    public void Abbrechen_und_Esc_melden_null()
    {
        BkUebernahmeErgebnis? ergebnis = new BkUebernahmeErgebnis(1);
        bool gemeldet = false;
        var cut = Zeige(p => p.Add(x => x.Geschlossen, (BkUebernahmeErgebnis? e) =>
        {
            ergebnis = e; gemeldet = true;
        }));

        cut.FindAll(".epos-leiste button")[0].Click();
        Assert.True(gemeldet);
        Assert.Null(ergebnis);

        gemeldet = false;
        cut.Find(".epos-dialog").KeyDown("Escape");
        Assert.True(gemeldet);
        Assert.Null(ergebnis);
    }

    /// <summary>A-7 aus B5b: „OK" schreibt sofort, Enter bleibt unbelegt.</summary>
    [Fact]
    public void Enter_ist_nicht_belegt()
    {
        bool gemeldet = false;
        var cut = Zeige(p => p.Add(x => x.Geschlossen, (BkUebernahmeErgebnis? _) => gemeldet = true));

        cut.Find(".epos-dialog").KeyDown("Enter");

        Assert.False(gemeldet);
    }

    [Fact]
    public void Der_Hilfeknopf_traegt_den_Schluessel_der_alten_Maske()
    {
        var cut = Zeige();

        Assert.Equal("Form_BkUebernahme.btn_Help", cut.Instance.HilfeSchluessel);
    }
}
