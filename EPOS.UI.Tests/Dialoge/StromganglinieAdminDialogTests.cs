using AngleSharp.Dom;
using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Dienste;
using KiKern;
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
public class StromganglinieAdminDialogTests : EposBunitContext
{
    public StromganglinieAdminDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static IReadOnlyList<Katalogfilterzeile> Katalog() => Zeitreihenproben.Stromganglinien();

    private IRenderedComponent<StromganglinieAdminDialog> Zeige(
        Func<Task<IReadOnlyList<Katalogfilterzeile>>>? katalog = null,
        Func<string, Task<bool>>? loeschen = null,
        Func<string, Task<string?>>? waehlen = null,
        Func<string, GanglinienRaster, GanglinienImportRueckrufe,
             Task<GanglinienImportErgebnis>>? einlesen = null,
        Action<bool>? geschlossen = null)
    {
        return Render<StromganglinieAdminDialog>(p => p
            .Add(x => x.Katalogzeilen, katalog ?? (() => Task.FromResult(Katalog())))
            .Add(x => x.Katalogprofil, Zeitreihenproben.Profil(Zeitreihenart.Stromganglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Loeschen, loeschen ?? (n => Task.FromResult(true)))
            .Add(x => x.DateiWaehlen, waehlen)
            .Add(x => x.Einlesen, einlesen)
            .Add(x => x.Geschlossen, (bool ok) => geschlossen?.Invoke(ok)));
    }

    private static IElement LoeschKnopf(IRenderedComponent<StromganglinieAdminDialog> cut)
        => cut.FindAll(".epos-dialog > .epos-leiste button")[0];

    private static IElement OkKnopf(IRenderedComponent<StromganglinieAdminDialog> cut)
        => cut.FindAll(".epos-dialog > .epos-leiste button")[1];

    /// <summary>Die Zeile ist die Wahl (Konzept Administrationsdialoge, V4).</summary>
    private static void Waehle(IRenderedComponent<StromganglinieAdminDialog> cut, int zeile)
        => Zeilenklick.Zeile(cut, zeile);

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

        // Die Fussleiste laeuft Vorschau aktualisieren . Fueller . Abbrechen . OK;
        // "Abbrechen" steht unmittelbar VOR dem primaeren Knopf (DL-2f).
        cut.FindAll(".epos-importoptionen .epos-leiste button")[^2].Click();   // "Abbrechen"
        await fertig.Task.MitZeitgrenze("Optionen");

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
        await fertig.Task.MitZeitgrenze("Protokoll");

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
        await fertig.Task.MitZeitgrenze("Konflikte");

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

    /// <summary>Esc wirkt wie „Beenden" — EIN Schlussweg (Konzept Administrationsdialoge, V15).</summary>
    [Fact]
    public void Esc_meldet_wie_Beenden()
    {
        bool? ergebnis = null;
        var cut = Zeige(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.True(ergebnis);
    }

    /// <summary>Das Schliesskreuz im Kopf wirkt wie Esc und wie „Beenden" (V15).</summary>
    [Fact]
    public void Kreuz_meldet_wie_Beenden()
    {
        bool? ergebnis = null;
        var cut = Zeige(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog-zu").Click();
        Assert.True(ergebnis);
    }

    /// <summary>„OK" heißt in der Verwaltung „Beenden" (V15) — primär und zuletzt.</summary>
    [Fact]
    public void Der_Schlussknopf_heisst_Beenden()
    {
        var cut = Zeige();
        var letzter = cut.FindAll(".epos-dialog > .epos-leiste button").Last();

        Assert.Equal("Beenden", letzter.TextContent.Trim());
        Assert.Contains("epos-knopf--primaer", letzter.ClassName);
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

    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F6)
    // =====================================================================

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Das Zeitintervall ist ein
    /// WAHLFELD über seinen angezeigten Text; die Markierung steht als Anzeige
    /// daneben und lässt sich nicht setzen.
    /// </summary>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_setzt_das_Zeitraster()
    {
        var cut = Zeige();

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.STROMGANGLINIE_ADMIN));

        KiFeldzugang raster = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.STROMGANGLINIE_ADMIN, "zeitintervall");
        Assert.NotNull(raster);
        Assert.True(raster.Setzbar);

        // Die Klappliste führt genau zwei Einträge (Befund W12-B15).
        IReadOnlyList<KiWahleintrag> eintraege = raster.Wahleintraege();
        Assert.Equal(2, eintraege.Count);

        KiFeldumsetzung wahl = KiFeldwandler.Wandle(raster, eintraege[1].Text);
        Assert.True(wahl.Ok, wahl.Grund);
        raster.Setzen(wahl.Wert);
        cut.Render();

        Assert.Equal(1, Convert.ToInt32(raster.Lesen(), CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// <b>Die Markierung ist eine ANZEIGE.</b> Sie zu setzen hieße, in einer Liste zu
    /// blättern, die nach einem Import eine andere ist — der Assistent nennt sie und
    /// lehnt das Setzen benannt ab.
    /// </summary>
    [Fact]
    public void Die_markierte_Ganglinie_bleibt_lesbar()
    {
        var cut = Zeige();
        Waehle(cut, 0);

        KiFeldzugang markiert = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.STROMGANGLINIE_ADMIN, "markierte_ganglinie");
        Assert.NotNull(markiert);
        Assert.False(markiert.Setzbar);
        Assert.False(string.IsNullOrEmpty(Convert.ToString(markiert.Lesen(),
                                                           CultureInfo.InvariantCulture)));
    }
}
