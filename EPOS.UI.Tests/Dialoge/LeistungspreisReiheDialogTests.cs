using Bunit;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Editor der saisonalen Leistungspreis-Sätze (iU9-W3.1), Vorbild
/// <c>Views/Kosten/Form_LeistungspreisReihe</c>.
///
/// <para>Der Feldbestand wird gegen die Feldkarte geprüft: ein Jahresfeld,
/// zwölf Monatsfelder, drei Knöpfe, Kontextzeile und Hinweiszeile.</para>
/// </summary>
public class LeistungspreisReiheDialogTests : BunitContext
{
    private static readonly string[] MONATE =
    {
        "Januar", "Februar", "März", "April", "Mai", "Juni",
        "Juli", "August", "September", "Oktober", "November", "Dezember"
    };

    public LeistungspreisReiheDialogTests()
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<LeistungspreisReiheDialog> Zeige(
        Action<Bunit.ComponentParameterCollectionBuilder<LeistungspreisReiheDialog>>? mehr = null)
    {
        return Render<LeistungspreisReiheDialog>(p =>
        {
            p.Add(x => x.Monatsnamen, MONATE);
            mehr?.Invoke(p);
        });
    }

    // =====================================================================
    // Feldbestand und Beschriftungen (Feldkarte)
    // =====================================================================

    [Fact]
    public void Der_Dialog_zeigt_dreizehn_Zahlenfelder_und_drei_Knoepfe()
    {
        var cut = Zeige();

        // 1 Jahr + 12 Monate
        Assert.Equal(13, cut.FindAll("input").Count);
        // Reihe löschen, Abbrechen, Übernehmen
        Assert.Equal(3, cut.FindAll(".epos-leiste button").Count);
    }

    [Fact]
    public void Die_zwoelf_Monatsnamen_stehen_an_ihren_Feldern()
    {
        var cut = Zeige();

        foreach (string monat in MONATE) Assert.Contains(monat + ":", cut.Markup);
    }

    [Fact]
    public void Kontextzeile_Einheit_und_Hinweis_stehen_im_Dialog()
    {
        var cut = Zeige(p => p
            .Add(x => x.KontextText, "Strom  —  Projektreihe")
            .Add(x => x.Einheit, "€/(kW·Monat)")
            .Add(x => x.HinweisText, "Eine gepflegte Reihe gilt vor dem konstanten Satz."));

        Assert.Equal("Strom  —  Projektreihe", cut.Find(".epos-kontextzeile").TextContent);
        Assert.Contains("€/(kW·Monat)", cut.Markup);
        Assert.Contains("gilt vor dem konstanten Satz", cut.Find(".epos-herleitung").TextContent);
    }

    // =====================================================================
    // Vorbelegung
    // =====================================================================

    [Fact]
    public void Die_geladenen_Werte_und_das_Jahr_stehen_in_den_Feldern()
    {
        var cut = Zeige(p => p
            .Add(x => x.Werte, new[] { 1.5, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0, 11.0, 12.0 })
            .Add(x => x.Jahr, 2031));

        Assert.Equal(2031, cut.Instance.JahrImFeld);
        Assert.Equal(1.5, cut.Instance.Felder[0]);
        Assert.Equal(12.0, cut.Instance.Felder[11]);
    }

    /// <summary>Weniger als zwölf gelieferte Werte füllen den Anfang, der Rest ist 0.</summary>
    [Fact]
    public void Eine_kurze_Werteliste_fuellt_nur_den_Anfang()
    {
        var cut = Zeige(p => p.Add(x => x.Werte, new[] { 4.0, 5.0 }));

        Assert.Equal(4.0, cut.Instance.Felder[0]);
        Assert.Equal(5.0, cut.Instance.Felder[1]);
        Assert.Equal(0.0, cut.Instance.Felder[2]);
        Assert.Equal(12, cut.Instance.Felder.Count);
    }

    [Fact]
    public void Ohne_eigene_Reihe_ist_Loeschen_gesperrt()
    {
        var cut = Zeige();

        Assert.True(cut.FindAll(".epos-leiste button")[0].HasAttribute("disabled"));
    }

    [Fact]
    public void Mit_eigener_Reihe_ist_Loeschen_frei()
    {
        var cut = Zeige(p => p.Add(x => x.LoeschenErlaubt, true));

        Assert.False(cut.FindAll(".epos-leiste button")[0].HasAttribute("disabled"));
    }

    // =====================================================================
    // Übernehmen
    // =====================================================================

    /// <summary>Die Sperre aus <c>btnUebernehmen_Click</c>: Summe 0 schreibt nicht.</summary>
    [Fact]
    public void Zwoelf_Nullen_melden_sich_und_schreiben_nicht()
    {
        bool geschrieben = false;
        var cut = Zeige(p => p
            .Add(x => x.Uebernehmen, (j, w) => { geschrieben = true; return true; })
            .Add(x => x.MeldungAllesNull, "Alle zwölf Sätze sind 0"));

        cut.FindAll(".epos-leiste button")[2].Click();

        Assert.False(geschrieben);
        Assert.Equal("Alle zwölf Sätze sind 0", cut.Instance.Meldung);
        Assert.Contains("Alle zwölf Sätze sind 0", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Uebernehmen_reicht_Jahr_und_zwoelf_Werte_weiter_und_schliesst()
    {
        int? jahr = null;
        double[]? werte = null;
        bool? ergebnis = null;

        var cut = Zeige(p => p
            .Add(x => x.Jahr, 2029)
            .Add(x => x.Werte, new[] { 2.0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3.0 })
            .Add(x => x.Uebernehmen, (j, w) =>
            {
                jahr = j;
                werte = System.Linq.Enumerable.ToArray(w);
                return true;
            })
            .Add(x => x.Geschlossen, (bool ok) => ergebnis = ok));

        cut.FindAll(".epos-leiste button")[2].Click();

        Assert.Equal(2029, jahr);
        Assert.NotNull(werte);
        Assert.Equal(12, werte!.Length);
        Assert.Equal(2.0, werte[0]);
        Assert.Equal(3.0, werte[11]);
        Assert.True(ergebnis);
    }

    [Fact]
    public void Ein_geaenderter_Monatswert_geht_mit()
    {
        double[]? werte = null;
        var cut = Zeige(p => p
            .Add(x => x.Werte, new[] { 1.0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.0 })
            .Add(x => x.Uebernehmen, (j, w) =>
            {
                werte = System.Linq.Enumerable.ToArray(w);
                return true;
            }));

        // Feld 0 ist das Jahr, Feld 1 der Januar, Feld 6 der Juni.
        cut.FindAll("input")[6].Input("7,25");
        cut.FindAll(".epos-leiste button")[2].Click();

        Assert.NotNull(werte);
        Assert.Equal(7.25, werte![5]);
    }

    /// <summary>A-7 aus Welle 2: Ein geleertes Feld behält seinen geladenen Wert.</summary>
    [Fact]
    public void Ein_geleertes_Jahresfeld_behaelt_das_geladene_Jahr()
    {
        int? jahr = null;
        var cut = Zeige(p => p
            .Add(x => x.Jahr, 2028)
            .Add(x => x.Werte, new[] { 5.0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.0 })
            .Add(x => x.Uebernehmen, (j, w) => { jahr = j; return true; }));

        cut.FindAll("input")[0].Input("");
        cut.FindAll(".epos-leiste button")[2].Click();

        Assert.Equal(2028, jahr);
    }

    [Fact]
    public void Ein_gescheitertes_Speichern_haelt_den_Dialog_offen_und_meldet()
    {
        bool geschlossen = false;
        var cut = Zeige(p => p
            .Add(x => x.Werte, new[] { 1.0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.0 })
            .Add(x => x.Uebernehmen, (j, w) => false)
            .Add(x => x.MeldungSpeicherfehler, "nicht gespeichert")
            .Add(x => x.Geschlossen, (bool ok) => geschlossen = true));

        cut.FindAll(".epos-leiste button")[2].Click();

        Assert.False(geschlossen);
        Assert.Contains("nicht gespeichert", cut.Find(".epos-warnbanner").TextContent);
    }

    // =====================================================================
    // Löschen und Abbrechen
    // =====================================================================

    [Fact]
    public void Loeschen_ruft_den_Delegaten_und_schliesst()
    {
        bool geloescht = false;
        bool? ergebnis = null;

        var cut = Zeige(p => p
            .Add(x => x.LoeschenErlaubt, true)
            .Add(x => x.Loeschen, () => { geloescht = true; return true; })
            .Add(x => x.Geschlossen, (bool ok) => ergebnis = ok));

        cut.FindAll(".epos-leiste button")[0].Click();

        Assert.True(geloescht);
        Assert.True(ergebnis);
    }

    [Fact]
    public void Ein_gescheitertes_Loeschen_meldet_und_haelt_offen()
    {
        bool geschlossen = false;
        var cut = Zeige(p => p
            .Add(x => x.LoeschenErlaubt, true)
            .Add(x => x.Loeschen, () => false)
            .Add(x => x.MeldungLoeschfehler, "nicht gelöscht")
            .Add(x => x.Geschlossen, (bool ok) => geschlossen = true));

        cut.FindAll(".epos-leiste button")[0].Click();

        Assert.False(geschlossen);
        Assert.Contains("nicht gelöscht", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Abbrechen_und_Esc_melden_false()
    {
        bool? ergebnis = null;
        var cut = Zeige(p => p.Add(x => x.Geschlossen, (bool ok) => ergebnis = ok));

        cut.FindAll(".epos-leiste button")[1].Click();
        Assert.False(ergebnis);

        ergebnis = null;
        cut.Find(".epos-dialog").KeyDown("Escape");
        Assert.False(ergebnis);
    }

    /// <summary>Enter ist unbelegt — „Übernehmen" schreibt sofort (A-7 aus B5b).</summary>
    [Fact]
    public void Enter_ist_nicht_belegt()
    {
        bool geschrieben = false;
        bool geschlossen = false;
        var cut = Zeige(p => p
            .Add(x => x.Werte, new[] { 1.0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.0 })
            .Add(x => x.Uebernehmen, (j, w) => { geschrieben = true; return true; })
            .Add(x => x.Geschlossen, (bool ok) => geschlossen = true));

        cut.Find(".epos-dialog").KeyDown("Enter");

        Assert.False(geschrieben);
        Assert.False(geschlossen);
    }

    [Fact]
    public void Der_Infoknopf_traegt_den_Schluessel_der_alten_Maske()
    {
        var hilfe = new TestHilfe();
        Services.AddSingleton<IHilfeDienst>(hilfe);   // gewinnt gegen KeineHilfe

        var cut = Zeige();
        cut.Find(".epos-infoknopf").Click();

        Assert.Equal(new[] { "Form_LeistungspreisReihe.btn_Help" }, hilfe.Geoeffnet);
    }
}
