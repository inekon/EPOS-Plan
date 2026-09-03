using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Die Seite „Übersicht" (iU9-W5.5), Vorbild
/// <c>Views/BerichteKosten/UcBkUebersicht</c> (1 552 Z., K4).
///
/// <para>Soll: Stammprojekt-Auswahl und Filter, die Liste mit vier Spalten
/// und Zeilenmarkierung, Bezeichnerfeld und die drei Knöpfe, der
/// Komponentenbereich in beiden Ansichten (Gegenüberstellung ohne, Unterschiede
/// mit Aktionsspalte) und die Statuszeile.</para>
/// </summary>
public class UebersichtSeiteTests : BunitContext
{
    public UebersichtSeiteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // ---- Probendaten -----------------------------------------------------

    private static VarianteZeile[] Zeilen() => new[]
    {
        new VarianteZeile { IdProjekt = 1030, Art = "Stamm", Bezeichner = "(Stammprojekt)",
                            Projektname = "Musterhaus", SimStand = "02.09.2026 10:00",
                            IstStamm = true },
        new VarianteZeile { IdProjekt = 1031, Art = "Variante", Bezeichner = "WP klein",
                            Projektname = "Musterhaus", SimStand = "", Auffaellig = true }
    };

    /// <summary>Die Gegenüberstellung (Stammzeile markiert) — ohne Aktionsspalte.</summary>
    private static UebersichtStand Vergleichsansicht() => new UebersichtStand
    {
        Staemme = new[] { (1030, "Musterhaus"), (1040, "Bürohaus") },
        StammId = 1030,
        Zeilen = Zeilen(),
        MarkierteId = 1030,
        KomponentenTitel = "Komponenten der Gruppe im Vergleich",
        Spalten = new[] { "Gewerk", "Merkmal", "Stamm", "WP klein" },
        Vergleich = new[]
        {
            new VergleichZeile { Gewerk = "Wärmepumpe", Merkmal = "Anzahl Komponenten",
                                 Zellen = new[] { "1", "2" } },
            new VergleichZeile { Gewerk = "", Merkmal = "Komponente 1",
                                 Zellen = new[] { "WP 1", "WP klein 1" },
                                 Kurztexte = new[] { "Hersteller: A", "Hersteller: B" } }
        },
        AnlegenMoeglich = true,
        SimulierenMoeglich = true,
        Statuszeile = "2 Zeile(n) im Vergleich über 1 Variante(n)."
    };

    /// <summary>Die Unterschiede (Variantenzeile markiert) — mit Aktionsspalte.</summary>
    private static UebersichtStand Unterschiedsansicht() => new UebersichtStand
    {
        Staemme = new[] { (1030, "Musterhaus") },
        StammId = 1030,
        Zeilen = Zeilen(),
        MarkierteId = 1031,
        KomponentenTitel = "Unterschiede der Variante „WP klein“",
        Spalten = new[] { "Gewerk", "Merkmal", "Stamm", "Variante" },
        Vergleich = new[]
        {
            new VergleichZeile { Schluessel = 7, Gewerk = "Wärmepumpe", Merkmal = "Nennleistung",
                                 Zellen = new[] { "12,5", "9,0" }, MitAktion = true,
                                 AktionKurztext = "Dieses Feld aus einer anderen Version übernehmen" },
            new VergleichZeile { Schluessel = 8, Gewerk = "", Merkmal = "Bezeichner",
                                 Zellen = new[] { "WP 1", "WP 2" }, MitAktion = true,
                                 Sperrgrund = "Der Bezeichner ist die Schlüsselspalte." }
        },
        Loeschbar = true,
        AnlegenMoeglich = true,
        SimulierenMoeglich = true
    };

    private UebersichtStand _stand = Vergleichsansicht();
    private int _geladen;

    private IRenderedComponent<UebersichtSeite> Zeige(
        Action<Bunit.ComponentParameterCollectionBuilder<UebersichtSeite>>? mehr = null,
        UebersichtStand? stand = null)
    {
        _stand = stand ?? Vergleichsansicht();
        _geladen = 0;
        return Render<UebersichtSeite>(p =>
        {
            p.Add(x => x.Laden, () => { _geladen++; return _stand; });
            mehr?.Invoke(p);
        });
    }

    private static IReadOnlyDictionary<string, object> LeererSatz()
        => new Dictionary<string, object>();

    private static IReadOnlyList<IElement> Listenzeilen(IRenderedComponent<UebersichtSeite> cut)
        => cut.Find(".epos-variantentabelle tbody").QuerySelectorAll("tr");

    private static IReadOnlyList<IElement> Vergleichszeilen(IRenderedComponent<UebersichtSeite> cut)
        => cut.Find(".epos-vergleichstabelle tbody").QuerySelectorAll("tr");

    private static IReadOnlyList<IElement> Pflegeknoepfe(IRenderedComponent<UebersichtSeite> cut)
        => cut.Find(".epos-variantenpflege").QuerySelectorAll("button");

    // =====================================================================
    // Feldbestand
    // =====================================================================

    [Fact]
    public void Die_Seite_zeigt_Auswahl_Filter_Liste_Pflege_und_Vergleich()
    {
        var cut = Zeige();

        Assert.Single(cut.FindAll("select"));                           // Stammprojekt
        Assert.Single(cut.FindAll(".epos-seite-zeile input[type=checkbox]"));  // nur Stämme
        Assert.Equal(5, cut.FindAll(".epos-variantentabelle thead th").Count);
        Assert.Equal(2, Listenzeilen(cut).Count);
        Assert.Equal(3, Pflegeknoepfe(cut).Count);                      // Anlegen, Löschen, Simulieren
        Assert.Contains("im Vergleich", cut.Find(".epos-untergruppe").TextContent);
        Assert.Contains("2 Zeile(n)", cut.Find(".epos-status").TextContent);
    }

    [Fact]
    public void Die_Gegenueberstellung_traegt_keine_Aktionsspalte()
    {
        var cut = Zeige();

        Assert.False(cut.Instance.MitAktionsspalte);
        Assert.Equal(4, cut.FindAll(".epos-vergleichstabelle thead th").Count);
        Assert.Empty(cut.FindAll(".epos-vergleichstabelle .epos-zellenaktionen"));
    }

    [Fact]
    public void Die_Unterschiedsansicht_traegt_die_Aktionsspalte()
    {
        var cut = Zeige(p => p.Add(x => x.UebernahmeGaben, (VergleichZeile _) => LeererSatz()),
                        stand: Unterschiedsansicht());

        Assert.True(cut.Instance.MitAktionsspalte);
        Assert.Equal(5, cut.FindAll(".epos-vergleichstabelle thead th").Count);
        Assert.Equal(2, cut.FindAll(".epos-vergleichstabelle .epos-zellenaktionen").Count);
    }

    /// <summary>
    /// „Ein Knopf, der beim Drücken nur erklärt, warum er nichts tut, wäre die
    /// schlechtere Auskunft" — der Vorläufer setzte einen grauen Strich.
    /// </summary>
    [Fact]
    public void Eine_gesperrte_Zeile_zeigt_den_Strich_mit_Begruendung()
    {
        var cut = Zeige(p => p.Add(x => x.UebernahmeGaben, (VergleichZeile _) => LeererSatz()),
                        stand: Unterschiedsansicht());

        var zeilen = Vergleichszeilen(cut);
        Assert.Single(zeilen[0].QuerySelectorAll(".epos-zellenaktionen button"));
        Assert.Empty(zeilen[1].QuerySelectorAll(".epos-zellenaktionen button"));

        var strich = zeilen[1].QuerySelector(".epos-gesperrt")!;
        Assert.Equal("—", strich.TextContent);
        Assert.Contains("Schlüsselspalte", strich.GetAttribute("title"));
    }

    [Fact]
    public void Das_Gewerk_steht_nur_in_der_ersten_Zeile_seines_Blocks()
    {
        var cut = Zeige();

        var zeilen = Vergleichszeilen(cut);
        Assert.Equal("Wärmepumpe", zeilen[0].QuerySelector(".epos-vergleich-gewerk")!.TextContent);
        Assert.Equal("", zeilen[1].QuerySelector(".epos-vergleich-gewerk")!.TextContent);
    }

    [Fact]
    public void Die_Merkmale_einer_Komponente_stehen_als_Kurztext_an_der_Zelle()
    {
        var cut = Zeige();

        var zellen = Vergleichszeilen(cut)[1].QuerySelectorAll("td");
        Assert.Equal("Hersteller: A", zellen[1].GetAttribute("title"));
        Assert.Equal("Hersteller: B", zellen[2].GetAttribute("title"));
    }

    [Fact]
    public void Ein_fehlender_Simulationsstand_wird_hervorgehoben()
    {
        var cut = Zeige();

        Assert.Single(cut.FindAll(".epos-veraltet"));
    }

    [Fact]
    public void Die_markierte_Zeile_ist_hervorgehoben()
    {
        var cut = Zeige();

        Assert.Contains("epos-zeile--markiert", Listenzeilen(cut)[0].ClassName);
        Assert.DoesNotContain("epos-zeile--markiert", Listenzeilen(cut)[1].ClassName);
    }

    // =====================================================================
    // Auswahl
    // =====================================================================

    [Fact]
    public void Ein_Stammwechsel_wird_gemeldet_und_laedt_neu()
    {
        int gemeldet = 0;
        var cut = Zeige(p => p.Add(x => x.StammGewechselt, (int id) => gemeldet = id));

        cut.Find("select").Change("1040");

        Assert.Equal(1040, gemeldet);
        Assert.Equal(2, _geladen);
    }

    [Fact]
    public void Der_Filter_wird_gemeldet_und_laedt_neu()
    {
        bool? gemeldet = null;
        var cut = Zeige(p => p.Add(x => x.FilterGewechselt, (bool an) => gemeldet = an));

        cut.Find(".epos-seite-zeile input[type=checkbox]").Change(true);

        Assert.True(gemeldet);
        Assert.Equal(2, _geladen);
    }

    [Fact]
    public void Eine_Zeilenmarkierung_wird_gemeldet_und_laedt_neu()
    {
        int gemeldet = 0;
        var cut = Zeige(p => p.Add(x => x.ZeileMarkiert, (int id) => gemeldet = id));

        Listenzeilen(cut)[1].QuerySelector(".epos-anlagenwahl")!.Click();

        Assert.Equal(1031, gemeldet);
        Assert.Equal(2, _geladen);
    }

    // =====================================================================
    // Variante anlegen und löschen
    // =====================================================================

    [Fact]
    public void Anlegen_gibt_den_Bezeichner_weiter_und_meldet()
    {
        string? bezeichner = null;
        var cut = Zeige(p => p.Add(x => x.VarianteAnlegen, (string b) =>
        {
            bezeichner = b;
            return "Variante „Kessel groß“ wurde angelegt.";
        }));

        cut.Find(".epos-variantenpflege input[type=text]").Input("Kessel groß");
        Pflegeknoepfe(cut)[0].Click();

        Assert.Equal("Kessel groß", bezeichner);
        Assert.Contains("wurde angelegt", cut.Instance.Status);
        Assert.Equal(2, _geladen);
    }

    [Fact]
    public void Loeschen_ist_ohne_markierte_Variante_gesperrt()
    {
        var cut = Zeige();   // Stammzeile markiert

        Assert.True(Pflegeknoepfe(cut)[1].HasAttribute("disabled"));
    }

    [Fact]
    public void Loeschen_fragt_nach_und_loescht_erst_bei_Ja()
    {
        int geloescht = 0;
        var cut = Zeige(p => p
            .Add(x => x.LoeschFrage, () => "Variante „WP klein“ wirklich löschen?")
            .Add(x => x.VarianteLoeschen, () => { geloescht++; return "gelöscht."; }),
            stand: Unterschiedsansicht());

        Pflegeknoepfe(cut)[1].Click();
        Assert.Contains("wirklich löschen", cut.Find(".epos-rueckfrage-text").TextContent);

        cut.FindAll(".epos-rueckfrage .epos-leiste button")[1].Click();   // Nein
        Assert.Equal(0, geloescht);

        Pflegeknoepfe(cut)[1].Click();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();   // Ja

        Assert.Equal(1, geloescht);
        Assert.Equal("gelöscht.", cut.Instance.Status);
    }

    // =====================================================================
    // Simulation
    // =====================================================================

    [Fact]
    public void Die_Simulation_meldet_ihr_Protokoll_im_Fenster()
    {
        var cut = Zeige(p => p.Add(x => x.Simulation, (Action<Laufschritt> m) =>
        {
            m(new Laufschritt(1, 2, "Stammprojekt"));
            return Task.FromResult(new LaufErgebnis
            {
                Erfolg = true,
                Statuszeile = "2 Lauf/Läufe beendet.",
                Meldung = "Stamm „Musterhaus“: 8760 Stunden"
            });
        }));

        Pflegeknoepfe(cut)[2].Click();

        Assert.Equal("2 Lauf/Läufe beendet.", cut.Instance.Status);
        Assert.Contains("8760 Stunden", cut.Find(".epos-warnbanner").TextContent);
        Assert.Equal(2, _geladen);
    }

    // =====================================================================
    // Übernahme
    // =====================================================================

    [Fact]
    public void Der_Uebernahmeknopf_oeffnet_den_Dialog_in_der_Ueberlagerung()
    {
        VergleichZeile? gefragt = null;
        var cut = Zeige(p => p.Add(x => x.UebernahmeGaben, (VergleichZeile z) =>
        {
            gefragt = z;
            return LeererSatz();
        }), stand: Unterschiedsansicht());

        Vergleichszeilen(cut)[0].QuerySelector(".epos-zellenaktionen button")!.Click();

        Assert.NotNull(gefragt);
        Assert.Equal(7, gefragt!.Schluessel);
        Assert.True(cut.Instance.UebernahmeOffen);
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
    }

    [Fact]
    public void Ein_Abbruch_im_Uebernahmedialog_schreibt_nichts()
    {
        int uebernommen = 0;
        var cut = Zeige(p => p
            .Add(x => x.UebernahmeGaben, (VergleichZeile _) => LeererSatz())
            .Add(x => x.Uebernehmen, (VergleichZeile z, int q) => { uebernommen++; return "ok"; }),
            stand: Unterschiedsansicht());

        Vergleichszeilen(cut)[0].QuerySelector(".epos-zellenaktionen button")!.Click();
        cut.Find(".epos-ueberlagerung").KeyDown("Escape");

        Assert.False(cut.Instance.UebernahmeOffen);
        Assert.Equal(0, uebernommen);
    }

    [Fact]
    public void Ohne_Gaben_bleibt_der_Uebernahmeknopf_weg()
    {
        var cut = Zeige(stand: Unterschiedsansicht());

        Assert.Empty(cut.FindAll(".epos-zellenaktionen button"));
    }

    [Fact]
    public void Der_Hilfeknopf_traegt_den_Schluessel_der_alten_Maske()
    {
        var cut = Zeige();

        Assert.Equal("UcBkUebersicht.btn_Help", cut.Instance.HilfeSchluessel);
    }
}
