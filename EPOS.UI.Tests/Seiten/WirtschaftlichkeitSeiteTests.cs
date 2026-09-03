using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Die Seite „Wirtschaftlichkeit" (iU9-W5.3), Vorbild
/// <c>Views/Wirtschaftlichkeit/UcWirtschaftlichkeit</c> (11 Kartenzeilen und
/// drei programmatisch gebaute Knöpfe).
///
/// <para>Soll ist die Feldkarte: vier Kennzahl-Karten, die Vergleichsgruppe
/// mit Haken, die Szenariowahl, der Parameternachweis, die Vergleichstabelle,
/// die Sicht-Knöpfe (Photovoltaik, BHKW, Strombezug — je nach Ausstattung),
/// „Parameter…", „Verlauf…", „Berechnen" und der Abbrechen-Knopf während
/// eines Laufs.</para>
/// </summary>
public class WirtschaftlichkeitSeiteTests : BunitContext
{
    public WirtschaftlichkeitSeiteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // ---- Probendaten -----------------------------------------------------

    private static ErgebnisAnsicht Ansicht(string erste = "12.500 €") => new ErgebnisAnsicht
    {
        Kacheln = new[]
        {
            new KachelZeile { Titel = "Kapitalwert ggü. Stamm", Wert = erste,
                              Quelle = "beste Variante: WP klein" },
            new KachelZeile { Titel = "Annuität", Wert = "1.200 €/a" },
            new KachelZeile { Titel = "Amortisation", Wert = "8,4 a" },
            new KachelZeile { Titel = "Interner Zinsfuß", Wert = "6,1 %" }
        },
        Matrix = new ErgebnisMatrix
        {
            Spalten = new[] { "Kennzahl", "Stamm", "WP klein" },
            Zeilen = new[]
            {
                new MatrixZeile { Titel = "Kapitalwert", Zellen = new[] { "-90.000", "-77.500" } },
                new MatrixZeile { Titel = "Annuität", Zellen = new[] { "—", "1.200" } }
            }
        }
    };

    private static WirtschaftlichkeitStand Standard(bool pv = true, bool bhkw = true,
                                                    bool strom = true) => new WirtschaftlichkeitStand
    {
        Varianten = new[]
        {
            new VarianteZeile { IdProjekt = 1030, Art = "Stamm", Bezeichner = "(Stammprojekt)",
                                Projektname = "Musterhaus", SimStand = "02.09.2026 10:00",
                                IstStamm = true },
            new VarianteZeile { IdProjekt = 1031, Art = "Variante", Bezeichner = "WP klein",
                                Projektname = "Musterhaus", SimStand = "", Auffaellig = true }
        },
        GewaehlteVarianten = new[] { 1030, 1031 },
        Szenarien = new[] { (0, "Erwartet"), (1, "Best Case"), (2, "Worst Case") },
        SzenarioId = 0,
        Parameterzeile = "Parameter: 20 a, 3,0 % · Referenz: Stammprojekt",
        Ansicht = Ansicht(),
        MitPhotovoltaik = pv,
        MitBhkw = bhkw,
        MitStrombezug = strom,
        Statuszeile = "Gespeicherte Ergebnisse vom 02.09.2026 11:00."
    };

    private WirtschaftlichkeitStand _stand = Standard();
    private int _geladen;

    private IRenderedComponent<WirtschaftlichkeitSeite> Zeige(
        Action<Bunit.ComponentParameterCollectionBuilder<WirtschaftlichkeitSeite>>? mehr = null,
        WirtschaftlichkeitStand? stand = null)
    {
        _stand = stand ?? Standard();
        _geladen = 0;
        return Render<WirtschaftlichkeitSeite>(p =>
        {
            p.Add(x => x.Laden, () => { _geladen++; return _stand; });
            mehr?.Invoke(p);
        });
    }

    /// <summary>Ein Parametersatz, der jeden Unterdialog aufbauen lässt.</summary>
    private static IReadOnlyDictionary<string, object> LeererSatz()
        => new Dictionary<string, object>();

    private static IReadOnlyList<IElement> Fussknoepfe(IRenderedComponent<WirtschaftlichkeitSeite> cut)
        => cut.FindAll(".epos-seite > .epos-leiste button");

    // =====================================================================
    // Feldbestand (Feldkarte)
    // =====================================================================

    [Fact]
    public void Die_Seite_zeigt_Karten_Liste_Szenario_Parameterzeile_und_Tabelle()
    {
        var cut = Zeige();

        Assert.Equal(4, cut.FindAll(".epos-kennzahlkachel").Count);
        Assert.Equal(5, cut.FindAll(".epos-raster:not(.epos-matrix) thead th").Count);   // Wahlspalte + 4
        Assert.Equal(2, cut.FindAll(".epos-raster tbody input[type=checkbox]").Count);
        Assert.Single(cut.FindAll("select"));                          // Szenario
        Assert.Contains("Referenz: Stammprojekt", cut.Find(".epos-herleitung-text").TextContent);
        Assert.Single(cut.FindAll(".epos-matrix"));
    }

    [Fact]
    public void Die_Matrix_traegt_je_Version_eine_Spalte()
    {
        var cut = Zeige();

        var koepfe = cut.Find(".epos-matrix thead tr").QuerySelectorAll("th");
        Assert.Equal(3, koepfe.Length);
        Assert.Equal("Kennzahl", koepfe[0].TextContent);
        Assert.Equal("WP klein", koepfe[2].TextContent);

        var zeilen = cut.Find(".epos-matrix tbody").QuerySelectorAll("tr");
        Assert.Equal(2, zeilen.Length);
        Assert.Equal("Kapitalwert", zeilen[0].QuerySelector(".epos-matrix-titel")!.TextContent);
        Assert.Equal("-77.500", zeilen[0].QuerySelectorAll(".epos-matrix-zelle")[1].TextContent);
    }

    [Fact]
    public void Ohne_Ergebnisse_bleibt_die_Tabelle_weg()
    {
        WirtschaftlichkeitStand ohne = Standard();
        ohne.Ansicht = new ErgebnisAnsicht { Kacheln = ohne.Ansicht.Kacheln };
        var cut = Zeige(stand: ohne);

        Assert.Empty(cut.FindAll(".epos-matrix"));
        Assert.Equal(4, cut.FindAll(".epos-kennzahlkachel").Count);
    }

    [Fact]
    public void Die_drei_Sichtknoepfe_folgen_der_Ausstattung()
    {
        var alle = Zeige(p => p.Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog _) => LeererSatz()));
        Assert.Equal(6, Fussknoepfe(alle).Count);   // PV, BHKW, Strom, Parameter, Verlauf, Berechnen

        var ohne = Zeige(p => p.Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog _) => LeererSatz()),
                         stand: Standard(pv: false, bhkw: false, strom: false));
        Assert.Equal(3, Fussknoepfe(ohne).Count);   // Parameter, Verlauf, Berechnen
    }

    /// <summary>Ohne Delegat kein Knopf (A-18 aus Welle 2).</summary>
    [Fact]
    public void Ohne_Gaben_bleibt_nur_Berechnen()
    {
        var cut = Zeige();

        Assert.Single(Fussknoepfe(cut));
        Assert.Equal("Berechnen", Fussknoepfe(cut)[0].TextContent.Trim());
    }

    /// <summary>Ä16: Der Sammel-Einstieg „Tarifstruktur…" gibt es nicht mehr.</summary>
    [Fact]
    public void Der_Sammel_Einstieg_Tarifstruktur_fehlt()
    {
        var cut = Zeige(p => p.Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog _) => LeererSatz()));

        Assert.DoesNotContain("Tarifstruktur", cut.Markup);
    }

    // =====================================================================
    // Vorbelegung und Auswahl
    // =====================================================================

    [Fact]
    public void Die_Seite_laedt_beim_Aufbau_und_zeigt_die_Statuszeile()
    {
        var cut = Zeige();

        Assert.Equal(1, _geladen);
        Assert.Contains("Gespeicherte Ergebnisse vom", cut.Instance.Status);
    }

    [Fact]
    public void Der_Haken_der_Stammzeile_ist_gesperrt()
    {
        var cut = Zeige();

        var haken = cut.FindAll(".epos-raster tbody input[type=checkbox]");
        Assert.True(haken[0].HasAttribute("disabled"));
        Assert.False(haken[1].HasAttribute("disabled"));
    }

    [Fact]
    public void Ein_Szenariowechsel_zeigt_neu_ohne_zu_rechnen()
    {
        int gefragt = -1;
        var cut = Zeige(p => p.Add(x => x.Anzeigen, (int id) =>
        {
            gefragt = id;
            return Ansicht("9.000 €");
        }));

        cut.Find("select").Change("2");

        Assert.Equal(2, gefragt);
        Assert.Equal("9.000 €", cut.FindAll(".epos-kennzahlkachel-wert")[0].TextContent);
        Assert.Equal(1, _geladen);   // NICHT neu geladen
    }

    // =====================================================================
    // Unterdialoge in der Überlagerung
    // =====================================================================

    [Theory]
    [InlineData(0, WirtschaftlichkeitSeite.Unterdialog.Photovoltaik)]
    [InlineData(1, WirtschaftlichkeitSeite.Unterdialog.Bhkw)]
    [InlineData(2, WirtschaftlichkeitSeite.Unterdialog.Strombezug)]
    [InlineData(3, WirtschaftlichkeitSeite.Unterdialog.Parameter)]
    [InlineData(4, WirtschaftlichkeitSeite.Unterdialog.Verlauf)]
    public void Jeder_Sichtknopf_oeffnet_seinen_Bereich(
        int knopf, WirtschaftlichkeitSeite.Unterdialog erwartet)
    {
        WirtschaftlichkeitSeite.Unterdialog gefragt = WirtschaftlichkeitSeite.Unterdialog.Keins;
        var cut = Zeige(p => p.Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog a) =>
        {
            gefragt = a;
            return LeererSatz();
        }));

        Fussknoepfe(cut)[knopf].Click();

        Assert.Equal(erwartet, gefragt);
        Assert.Equal(erwartet, cut.Instance.OffenerUnterdialog);
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
    }

    [Fact]
    public void Ohne_Parametersatz_bleibt_der_Bereich_zu_und_die_Huelle_meldet()
    {
        var cut = Zeige(p => p
            .Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog _)
                => (IReadOnlyDictionary<string, object>?)null)
            .Add(x => x.Nachlauf, (WirtschaftlichkeitSeite.Unterdialog _, bool _)
                => "Zu diesem Projekt gibt es keine BHKW-Vergleichsgruppe."));

        Fussknoepfe(cut)[1].Click();   // BHKW

        Assert.Equal(WirtschaftlichkeitSeite.Unterdialog.Keins, cut.Instance.OffenerUnterdialog);
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
        Assert.Contains("keine BHKW-Vergleichsgruppe", cut.Instance.Status);
    }

    [Fact]
    public void Nach_dem_Schliessen_frischt_die_Seite_auf_und_meldet()
    {
        var cut = Zeige(p => p
            .Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog _) => LeererSatz())
            .Add(x => x.Nachlauf, (WirtschaftlichkeitSeite.Unterdialog a, bool ok)
                => ok ? "Parameter gespeichert — bitte neu berechnen." : ""));

        Fussknoepfe(cut)[3].Click();                          // Parameter
        // Esc auf der Ueberlagerung schliesst ohne zu speichern.
        cut.Find(".epos-ueberlagerung").KeyDown("Escape");

        Assert.Equal(WirtschaftlichkeitSeite.Unterdialog.Keins, cut.Instance.OffenerUnterdialog);
        Assert.Equal(2, _geladen);                            // Aufbau + Nachlauf
    }

    // =====================================================================
    // Berechnen
    // =====================================================================

    [Fact]
    public void Berechnen_uebergibt_die_Varianten_ohne_Stamm()
    {
        IReadOnlyList<int>? ids = null;
        var cut = Zeige(p => p.Add(x => x.Berechnen,
            (IReadOnlyList<int> v, Action<Laufschritt> m) =>
            {
                ids = v;
                return Task.FromResult(new LaufErgebnis { Erfolg = true, Statuszeile = "Berechnet." });
            }));

        Fussknoepfe(cut)[0].Click();   // „Berechnen" (ohne Gaben der einzige Knopf)

        Assert.NotNull(ids);
        Assert.Equal(new[] { 1031 }, ids!);
        Assert.Equal("Berechnet.", cut.Instance.Status);
        Assert.Equal(2, _geladen);
    }

    [Fact]
    public void Der_Fortschritt_zaehlt_und_verschwindet_danach()
    {
        var cut = Zeige(p => p.Add(x => x.Berechnen,
            (IReadOnlyList<int> v, Action<Laufschritt> m) =>
            {
                m(new Laufschritt(1, 2, "Stammprojekt"));
                return Task.FromResult(new LaufErgebnis { Erfolg = true, Statuszeile = "Berechnet." });
            }));

        Fussknoepfe(cut)[0].Click();

        Assert.Empty(cut.FindAll(".epos-fortschritt"));
        Assert.False(cut.Instance.Beschaeftigt);
    }

    [Fact]
    public void Ein_Abbruch_meldet_sich_und_laedt_nicht_neu()
    {
        var cut = Zeige(p => p
            .Add(x => x.StatusAbgebrochen, "Vorgang abgebrochen.")
            .Add(x => x.Berechnen, (IReadOnlyList<int> v, Action<Laufschritt> m)
                => Task.FromResult(new LaufErgebnis { Abgebrochen = true })));

        Fussknoepfe(cut)[0].Click();

        Assert.Equal("Vorgang abgebrochen.", cut.Instance.Status);
        Assert.Equal(1, _geladen);
    }

    [Fact]
    public void Ein_Rechenfehler_erscheint_als_Warnbanner()
    {
        var cut = Zeige(p => p.Add(x => x.Berechnen, (IReadOnlyList<int> v, Action<Laufschritt> m)
            => Task.FromResult(new LaufErgebnis { Fehler = "Simulation gescheitert." })));

        Fussknoepfe(cut)[0].Click();

        Assert.Contains("Simulation gescheitert", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Der_Hilfeknopf_traegt_den_Schluessel_der_alten_Maske()
    {
        var cut = Zeige();

        Assert.Equal("UcWirtschaftlichkeit.btn_Help", cut.Instance.HilfeSchluessel);
    }
}
