using AngleSharp.Dom;
using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Stammdatenverwaltung der Stromganglinien (iU9-W12.4), Vorbild
/// <c>Views/Stromverbraucher/Form_Stromganglinie_Admin</c>.
///
/// <para>Soll ist die Feldkarte: Katalogliste mit Zeilenwahl, Rasterliste mit
/// ZWEI Einträgen, Dateiwahl mit „Datei Einlesen...", „Ganglinie Löschen" und
/// „OK". Geprüft werden die ReadOnly-Sperre, die neue Rückfrage vor dem Löschen
/// (A-Zeile zu W12-B12) und die drei Überlagerungen der Importkette.</para>
/// </summary>
public class StromganglinieAdminDialogTests : BunitContext
{
    public StromganglinieAdminDialogTests()
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

    private static List<GanglinienKatalogZeile> Katalog() => new()
    {
        new GanglinienKatalogZeile("Werk Nord", 4, false),
        new GanglinienKatalogZeile("Auslieferung", 1, true)
    };

    private IRenderedComponent<StromganglinieAdminDialog> Zeige(
        Func<Task<List<GanglinienKatalogZeile>>>? katalog = null,
        Func<string, Task<bool>>? loeschen = null,
        Func<string, Task<string?>>? waehlen = null,
        Func<string, GanglinienRaster, GanglinienImportRueckrufe,
             Task<GanglinienImportErgebnis>>? einlesen = null,
        Action<bool>? geschlossen = null)
    {
        return Render<StromganglinieAdminDialog>(p => p
            .Add(x => x.Katalog, katalog ?? (() => Task.FromResult(Katalog())))
            .Add(x => x.Loeschen, loeschen ?? (n => Task.FromResult(true)))
            .Add(x => x.DateiWaehlen, waehlen)
            .Add(x => x.Einlesen, einlesen)
            .Add(x => x.Geschlossen, (bool ok) => geschlossen?.Invoke(ok)));
    }

    private static IElement LoeschKnopf(IRenderedComponent<StromganglinieAdminDialog> cut)
        => cut.FindAll(".epos-dialog > .epos-leiste button")[0];

    private static IElement OkKnopf(IRenderedComponent<StromganglinieAdminDialog> cut)
        => cut.FindAll(".epos-dialog > .epos-leiste button")[1];

    private static void Waehle(IRenderedComponent<StromganglinieAdminDialog> cut, int zeile)
        => cut.FindAll(".epos-raster tbody tr")[zeile].QuerySelector("button")!.Click();

    // =====================================================================
    // Feldbestand
    // =====================================================================

    [Fact]
    public void Der_Dialog_zeigt_Katalog_Rasterliste_Dateiwahl_und_zwei_Knoepfe()
    {
        var cut = Zeige();

        Assert.Contains("Stromganglinien", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal(2, cut.FindAll(".epos-raster tbody tr").Count);
        Assert.Single(cut.FindAll("select"));
        Assert.Single(cut.FindAll(".epos-dateiwahl"));
        Assert.Equal(2, cut.FindAll(".epos-dialog > .epos-leiste button").Count);
    }

    /// <summary>
    /// <b>Befund W12-B15.</b> Die Auswahlliste hat ZWEI Einträge — die Abbildung im
    /// Kern kennt drei, der dritte ist unerreichbar.
    /// </summary>
    [Fact]
    public void Die_Rasterliste_hat_genau_zwei_Eintraege()
    {
        var cut = Zeige();

        Assert.Equal(2, cut.Find("select").QuerySelectorAll("option").Length);
    }

    /// <summary>Ohne Wähler erscheint kein Knopf — dieselbe Regel wie überall.</summary>
    [Fact]
    public void Ohne_Dateiwaehler_bleibt_der_Einleseknopf_weg()
    {
        Assert.Empty(Zeige().FindAll(".epos-dateiwahl button"));
        Assert.Single(Zeige(waehlen: f => Task.FromResult<string?>(null)).FindAll(".epos-dateiwahl button"));
    }

    [Fact]
    public void Loeschen_ist_ohne_Auswahl_gesperrt()
    {
        var cut = Zeige();
        Assert.True(LoeschKnopf(cut).HasAttribute("disabled"));

        Waehle(cut, 0);
        Assert.False(LoeschKnopf(cut).HasAttribute("disabled"));
        Assert.Equal("Werk Nord", cut.Instance.Gewaehlt);
    }

    // =====================================================================
    // Loeschen
    // =====================================================================

    /// <summary>
    /// Prüfregel 1, wörtlich: Ein Auslieferungssatz bleibt stehen — und die Meldung
    /// steht jetzt im Katalog statt hartkodiert im Quelltext (Befund W12-B12).
    /// </summary>
    [Fact]
    public void Ein_Auslieferungssatz_wird_nicht_geloescht()
    {
        bool gerufen = false;
        var cut = Zeige(loeschen: n => { gerufen = true; return Task.FromResult(true); });

        Waehle(cut, 1);                       // "Auslieferung", NurLesen
        LoeschKnopf(cut).Click();

        Assert.False(gerufen);
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));    // keine Rueckfrage
        Assert.Contains("schreibgeschützt", cut.Instance.Meldung);
    }

    /// <summary>
    /// <b>A-Zeile zu Befund W12-B12.</b> Der Vorläufer löschte OHNE Rückfrage; jetzt
    /// steht eine davor.
    /// </summary>
    [Fact]
    public void Vor_dem_Loeschen_kommt_eine_Rueckfrage()
    {
        string? geloescht = null;
        var cut = Zeige(loeschen: n => { geloescht = n; return Task.FromResult(true); });

        Waehle(cut, 0);
        LoeschKnopf(cut).Click();

        Assert.Single(cut.FindAll(".epos-rueckfrage"));
        Assert.Null(geloescht);

        cut.Find(".epos-rueckfrage .epos-leiste button").Click();   // "Ja"
        Assert.Equal("Werk Nord", geloescht);
    }

    [Fact]
    public void Nein_laesst_die_Ganglinie_stehen()
    {
        string? geloescht = null;
        var cut = Zeige(loeschen: n => { geloescht = n; return Task.FromResult(true); });

        Waehle(cut, 0);
        LoeschKnopf(cut).Click();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[1].Click();   // "Nein"

        Assert.Null(geloescht);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
    }

    // =====================================================================
    // Die Importkette und ihre drei Ueberlagerungen
    // =====================================================================

    /// <summary>
    /// Die Kette bekommt den gewählten Pfad UND das Raster aus der Auswahlliste —
    /// sie übersteuert die Erkennung (Vorläufer :149).
    /// </summary>
    [Fact]
    public void Die_Dateiwahl_startet_die_Kette_mit_dem_gewaehlten_Raster()
    {
        string? gesehenPfad = null;
        GanglinienRaster gesehenRaster = GanglinienRaster.Minute;

        var cut = Zeige(
            waehlen: f => Task.FromResult<string?>(@"C:\Daten\lastgang.csv"),
            einlesen: (pfad, raster, r) =>
            {
                gesehenPfad = pfad;
                gesehenRaster = raster;
                return Task.FromResult(new GanglinienImportErgebnis
                {
                    Ausgang = ImportAusgang.Erfolg,
                    Meldung = "fertig",
                    MeldungStufe = PruefStufe.Info
                });
            });

        cut.Find("select").Change("1");                   // Viertelstundenwerte
        cut.Find(".epos-dateiwahl button").Click();

        Assert.Equal(@"C:\Daten\lastgang.csv", gesehenPfad);
        Assert.Equal(GanglinienRaster.Viertelstunde, gesehenRaster);
        Assert.Equal("fertig", cut.Instance.Meldung);
    }

    /// <summary>
    /// Der Rückruf „Optionen" öffnet die Überlagerung; ihr „Abbrechen" löst die
    /// wartende Kette wieder auf.
    /// </summary>
    [Fact]
    public async Task Der_Optionenrueckruf_erscheint_als_Ueberlagerung()
    {
        GanglinienImportOptionen? gemeldet = new();
        TaskCompletionSource fertig = new();

        var cut = Zeige(
            waehlen: f => Task.FromResult<string?>(@"C:\Daten\lastgang.csv"),
            einlesen: async (pfad, raster, r) =>
            {
                gemeldet = await r.Optionen!(pfad, new GanglinienVorschau { Lesbar = true, Spaltenzahl = 2 });
                fertig.SetResult();
                return new GanglinienImportErgebnis { Ausgang = ImportAusgang.Abgebrochen };
            });

        cut.Find(".epos-dateiwahl button").Click();

        // Die Ueberlagerung steht - mit dem Optionendialog darin.
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-importoptionen")));

        cut.Find(".epos-importoptionen .epos-leiste button").Click();   // "Abbrechen"
        await fertig.Task;

        Assert.Null(gemeldet);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".epos-importoptionen")));
    }

    /// <summary>Dasselbe für das Protokoll: „OK" gibt <c>true</c> zurück.</summary>
    [Fact]
    public async Task Der_Protokollrueckruf_erscheint_als_Ueberlagerung()
    {
        bool weiter = false;
        TaskCompletionSource fertig = new();

        var cut = Zeige(
            waehlen: f => Task.FromResult<string?>(@"C:\Daten\lastgang.csv"),
            einlesen: async (pfad, raster, r) =>
            {
                weiter = await r.Protokoll!(new List<PruefMeldung>
                {
                    new(PruefStufe.Warnung, "IMPORT_PROT_SCHALTJAHR", "8784", "8760", "24")
                }, true, true);
                fertig.SetResult();
                return new GanglinienImportErgebnis { Ausgang = ImportAusgang.Abgebrochen };
            });

        cut.Find(".epos-dateiwahl button").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-ganglinie-protokoll")));

        cut.FindAll(".epos-ganglinie-protokoll .epos-leiste button")[1].Click();   // "OK"
        await fertig.Task;

        Assert.True(weiter);
    }

    /// <summary>Und für die Konflikte: „Übernehmen" liefert die Entscheidungen.</summary>
    [Fact]
    public async Task Der_Konfliktrueckruf_erscheint_als_Ueberlagerung()
    {
        List<KonfliktEntscheidung>? entscheidungen = null;
        TaskCompletionSource fertig = new();

        var cut = Zeige(
            waehlen: f => Task.FromResult<string?>(@"C:\Daten\lastgang.csv"),
            einlesen: async (pfad, raster, r) =>
            {
                entscheidungen = await r.Konflikte!(new List<ImportPruefung>
                {
                    new()
                    {
                        Kandidat = new ImportKandidat { Name = "lastgang" },
                        Befund = ImportBefund.NameVorhanden,
                        AbweichendeSpalten = new List<string> { "Zeitinterval" }
                    }
                }, new HashSet<string>(StringComparer.Ordinal));
                fertig.SetResult();
                return new GanglinienImportErgebnis { Ausgang = ImportAusgang.Abgebrochen };
            });

        cut.Find(".epos-dateiwahl button").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-importkonflikte")));

        cut.FindAll(".epos-importkonflikte .epos-leiste button")[2].Click();   // "Uebernehmen"
        await fertig.Task;

        Assert.NotNull(entscheidungen);
        Assert.Single(entscheidungen!);
        Assert.Equal(KonfliktAktion.Auslassen, entscheidungen![0].Aktion);
    }

    // =====================================================================
    // Schluss
    // =====================================================================

    [Fact]
    public void OK_meldet_true()
    {
        bool? ergebnis = null;
        var cut = Zeige(geschlossen: b => ergebnis = b);

        OkKnopf(cut).Click();
        Assert.True(ergebnis);
    }

    [Fact]
    public void Esc_meldet_false()
    {
        bool? ergebnis = null;
        var cut = Zeige(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(ergebnis);
    }

    /// <summary>
    /// Steht eine Überlagerung, schließt Esc NUR sie — der Wirt wertet die Taste
    /// erst danach für sich aus (Muster W7.5).
    /// </summary>
    [Fact]
    public void Esc_schliesst_bei_offener_Rueckfrage_nicht_den_ganzen_Dialog()
    {
        bool? ergebnis = null;
        var cut = Zeige(geschlossen: b => ergebnis = b);

        Waehle(cut, 0);
        LoeschKnopf(cut).Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Null(ergebnis);
    }
}
