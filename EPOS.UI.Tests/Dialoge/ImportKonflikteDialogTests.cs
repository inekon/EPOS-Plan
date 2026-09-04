using AngleSharp.Dom;
using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Import;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der gemeinsame Konfliktdialog aller Importpfade (iU9-W12.3), Vorbild
/// <c>Views/Import/Form_ImportKonflikte</c>.
///
/// <para>Soll ist die Feldkarte der Code-Form: Kopfzeile, ein Raster mit den
/// drei Spalten Eintrag / Befund / Aktion, „Alle Konflikte auslassen",
/// „Übernehmen" und „Abbrechen". Geprüft werden die Regeln, die den Dialog
/// ausmachen: das Namensfeld nur beim Umbenennen, der Namensvorschlag, die
/// Rückgabe ALLER Zeilen und die OK-Prüfung.</para>
/// </summary>
public class ImportKonflikteDialogTests : BunitContext
{
    public ImportKonflikteDialogTests()
    {
        DeutscheOberflaeche();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>
    /// Die Sprache der Oberfläche wird auf de-DE gepinnt (Regel seit iU9-W8, Muster
    /// <c>DeutscheOberflaeche</c> aus <c>EPOS.Kern.Tests</c>) — Kultur UND Thread-Kultur,
    /// damit ein Lauf auf einem en-US-Läufer dieselben deutschen Texte sieht wie hier.
    /// Windows-Lauf 33839255709 fiel ohne das Pinnen mit "Cancel" statt "Abbrechen".
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

    private static ImportPruefung Pruefung(ImportBefund befund, string name,
                                           bool nameMehrfachInDb = false,
                                           bool nameDoppeltInAuswahl = false,
                                           KatalogSatz? vorhanden = null)
        => new()
        {
            Kandidat = new ImportKandidat { Name = name },
            Befund = befund,
            Vorhanden = vorhanden,
            NameMehrfachInDb = nameMehrfachInDb,
            NameDoppeltInAuswahl = nameDoppeltInAuswahl,
            AbweichendeSpalten = new List<string> { "Zeitinterval" }
        };

    private IRenderedComponent<ImportKonflikteDialog> Zeige(
        IReadOnlyList<ImportPruefung>? pruefungen = null,
        IReadOnlyCollection<string>? vergeben = null,
        Action<List<KonfliktEntscheidung>?>? geschlossen = null)
    {
        return Render<ImportKonflikteDialog>(p => p
            .Add(x => x.Pruefungen, pruefungen ?? new[]
            {
                Pruefung(ImportBefund.Neu, "Werk Nord"),
                Pruefung(ImportBefund.NameVorhanden, "Werk Süd")
            })
            .Add(x => x.VergebeneNamen, vergeben ?? new HashSet<string>(StringComparer.Ordinal))
            .Add(x => x.Geschlossen, (List<KonfliktEntscheidung>? l) => geschlossen?.Invoke(l)));
    }

    private static IElement Knopf(IRenderedComponent<ImportKonflikteDialog> cut, int i)
        => cut.FindAll(".epos-leiste button")[i];

    private static IReadOnlyList<IElement> Zeilen(IRenderedComponent<ImportKonflikteDialog> cut)
        => cut.FindAll(".epos-raster tbody tr");

    private static IElement Aktion(IRenderedComponent<ImportKonflikteDialog> cut, int zeile)
        => Zeilen(cut)[zeile].QuerySelectorAll("select")[0];

    // =====================================================================
    // Feldbestand
    // =====================================================================

    [Fact]
    public void Der_Dialog_zeigt_Kopf_Raster_und_drei_Knoepfe()
    {
        var cut = Zeige();

        Assert.Contains("Konflikte", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Single(cut.FindAll(".epos-konflikt-kopf"));
        Assert.Equal(3, cut.FindAll(".epos-raster thead th").Count);
        Assert.Equal(3, cut.FindAll(".epos-leiste button").Count);
        Assert.Equal(2, Zeilen(cut).Count);
    }

    [Fact]
    public void Der_Kopf_nennt_Gesamtzahl_und_Konflikte()
    {
        var cut = Zeige();

        // Zwei Eintraege, davon einer mit Konflikt (NameVorhanden).
        Assert.Contains("2", cut.Find(".epos-konflikt-kopf").TextContent);
        Assert.Contains("1", cut.Find(".epos-konflikt-kopf").TextContent);
    }

    /// <summary>Der Befundtext kann Zusatzzeilen tragen — sie stehen untereinander.</summary>
    [Fact]
    public void Der_Befund_bringt_seine_Zusatzzeilen_mit()
    {
        var cut = Zeige(new[]
        {
            Pruefung(ImportBefund.Identisch, "Auslieferung", nameMehrfachInDb: true,
                     vorhanden: new KatalogSatz { Name = "Auslieferung", ReadOnly = true })
        });

        Assert.Equal(3, Zeilen(cut)[0].QuerySelector(".epos-konflikt-befund")!
                                      .QuerySelectorAll("div").Length);
    }

    // =====================================================================
    // Die Aktion ist ein Wert (Befund W12-B19)
    // =====================================================================

    /// <summary>
    /// Die erlaubten Aktionen kommen aus dem Kern; ein Namenstreffer bietet drei
    /// an, ein sauberer Neuzugang zwei.
    /// </summary>
    [Fact]
    public void Jede_Zeile_bietet_genau_ihre_erlaubten_Aktionen()
    {
        var cut = Zeige();

        Assert.Equal(2, Aktion(cut, 0).QuerySelectorAll("option").Length);  // Neu
        Assert.Equal(3, Aktion(cut, 1).QuerySelectorAll("option").Length);  // NameVorhanden
    }

    [Fact]
    public void Ohne_Ueberschreibmoeglichkeit_bleiben_zwei_Aktionen()
    {
        var cut = Zeige(new[] { Pruefung(ImportBefund.NameVorhanden, "X", nameMehrfachInDb: true) });

        Assert.Equal(2, Aktion(cut, 0).QuerySelectorAll("option").Length);
    }

    /// <summary>
    /// Der Vorlaeufer las die Aktion aus dem ANZEIGETEXT zurueck. Hier steht sie als
    /// Wert in der Zeile — die Rueckgabe belegt das.
    /// </summary>
    [Fact]
    public void Die_Vorbelegung_folgt_dem_Befund()
    {
        var cut = Zeige();
        List<KonfliktEntscheidung> e = cut.Instance.Entscheidungen();

        Assert.Equal(KonfliktAktion.Importieren, e[0].Aktion);   // Neu
        Assert.Equal(KonfliktAktion.Auslassen, e[1].Aktion);     // NameVorhanden
    }

    // =====================================================================
    // Umbenennen
    // =====================================================================

    /// <summary>
    /// Die Namenszelle ist NUR beim Umbenennen ein Eingabefeld
    /// (<c>Grid_CellBeginEdit</c>).
    /// </summary>
    [Fact]
    public void Das_Namensfeld_erscheint_erst_beim_Umbenennen()
    {
        var cut = Zeige();
        Assert.Empty(cut.FindAll(".epos-raster tbody input[type=text]"));

        Aktion(cut, 1).Change("2");   // Auslassen, Ueberschreiben, Umbenennen -> Platz 2
        Assert.Single(cut.FindAll(".epos-raster tbody input[type=text]"));
    }

    /// <summary>
    /// Steht die Zeile auf „Umbenennen" und traegt noch den Originalnamen, bekommt
    /// sie einen Vorschlag; ein Wechsel zurueck stellt den Originalnamen wieder her.
    /// </summary>
    [Fact]
    public void Umbenennen_schlaegt_einen_freien_Namen_vor_und_nimmt_ihn_zurueck()
    {
        var cut = Zeige();

        Aktion(cut, 1).Change("2");
        Assert.Equal("Werk Süd (2)", cut.Instance.Entscheidungen()[1].NeuerName);

        Aktion(cut, 1).Change("0");   // wieder Auslassen
        Assert.Null(cut.Instance.Entscheidungen()[1].NeuerName);
    }

    [Fact]
    public void Der_Vorschlag_ueberspringt_vergebene_Namen()
    {
        var cut = Zeige(vergeben: new HashSet<string>(StringComparer.Ordinal)
        {
            DublettenPruefung.NormalisiereName("Werk Süd (2)")
        });

        Aktion(cut, 1).Change("2");
        Assert.Equal("Werk Süd (3)", cut.Instance.Entscheidungen()[1].NeuerName);
    }

    // =====================================================================
    // Alle Konflikte auslassen
    // =====================================================================

    /// <summary>Nur die KONFLIKTZEILEN werden gesetzt — die saubere bleibt stehen.</summary>
    [Fact]
    public void Alle_auslassen_trifft_nur_die_Konfliktzeilen()
    {
        var cut = Zeige();

        Aktion(cut, 1).Change("2");           // erst umbenennen
        Knopf(cut, 0).Click();                // dann "Alle Konflikte auslassen"

        List<KonfliktEntscheidung> e = cut.Instance.Entscheidungen();
        Assert.Equal(KonfliktAktion.Importieren, e[0].Aktion);   // unveraendert
        Assert.Equal(KonfliktAktion.Auslassen, e[1].Aktion);
        Assert.Null(e[1].NeuerName);
    }

    // =====================================================================
    // OK und Abbrechen
    // =====================================================================

    /// <summary>
    /// Die Rueckgabe enthaelt ALLE Zeilen, auch die konfliktfreien — die drei
    /// Sammelimporte zaehlen daraus „uebersprungen".
    /// </summary>
    [Fact]
    public void Uebernehmen_meldet_alle_Zeilen()
    {
        List<KonfliktEntscheidung>? ergebnis = null;
        var cut = Zeige(geschlossen: l => ergebnis = l);

        Knopf(cut, 2).Click();

        Assert.NotNull(ergebnis);
        Assert.Equal(2, ergebnis.Count);
        Assert.Equal("Werk Nord", ergebnis[0].Pruefung.Kandidat.Name);
    }

    /// <summary>
    /// Der Vorlaeufer zeigte eine MessageBox und liess den Dialog offen. Hier steht
    /// die Meldung als Warnbanner, die Zeile wird hervorgehoben — und geschlossen
    /// wird nicht.
    /// </summary>
    [Fact]
    public void Ein_leerer_Umbenennungsname_haelt_den_Dialog_offen()
    {
        List<KonfliktEntscheidung>? ergebnis = null;
        var cut = Zeige(geschlossen: l => ergebnis = l);

        Aktion(cut, 1).Change("2");
        cut.Find(".epos-raster tbody input[type=text]").Input("   ");
        Knopf(cut, 2).Click();

        Assert.Null(ergebnis);
        Assert.Single(cut.FindAll(".epos-warnbanner"));
    }

    [Fact]
    public void Zwei_Zeilen_mit_demselben_Zielnamen_werden_beanstandet()
    {
        List<KonfliktEntscheidung>? ergebnis = null;
        var cut = Zeige(new[]
        {
            Pruefung(ImportBefund.Neu, "Gleich"),
            Pruefung(ImportBefund.Neu, "Gleich")
        }, geschlossen: l => ergebnis = l);

        Knopf(cut, 2).Click();

        Assert.Null(ergebnis);
        Assert.Single(cut.FindAll(".epos-warnbanner"));
    }

    [Fact]
    public void Abbrechen_meldet_null()
    {
        List<KonfliktEntscheidung>? ergebnis = new();
        var cut = Zeige(geschlossen: l => ergebnis = l);

        Knopf(cut, 1).Click();
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Esc_meldet_ebenfalls_null()
    {
        List<KonfliktEntscheidung>? ergebnis = new();
        var cut = Zeige(geschlossen: l => ergebnis = l);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(ergebnis);
    }

    /// <summary>Befund W12-B20: Der Dialog hat einen Infoknopf mit der vorhandenen Zeile.</summary>
    [Fact]
    public void Der_Infoknopf_zeigt_auf_die_Zeile_der_Projektverwaltung()
    {
        TestHilfe hilfe = new(new HilfeEintrag("Projektverwaltung", "", ""));
        Services.AddSingleton<IHilfeDienst>(hilfe);

        var cut = Render<ImportKonflikteDialog>(p => p
            .Add(x => x.Pruefungen, new[] { Pruefung(ImportBefund.Neu, "A") })
            .Add(x => x.VergebeneNamen, new HashSet<string>(StringComparer.Ordinal)));

        Assert.Single(cut.FindAll(".epos-infoknopf"));
    }
}
