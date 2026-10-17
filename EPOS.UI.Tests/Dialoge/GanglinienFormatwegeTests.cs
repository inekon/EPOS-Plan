using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Dienste;
using EPOS.UI.Standards;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Wege Spotpreis und TWW-Messreihen über „Format und Vorschau“</b> (IM-4, Teil 2): Die
/// Dialoge rufen nach der Dateiwahl den Optionendialog mit ihrem Format — der Spotpreis mit
/// Preisachse und Jahresfeld (das Jahr geht an die Prüfung der Hülle), die Messreihe mit der
/// Tagesansicht statt des Jahresbilds. Abbruch lässt alles, wie es war; eine unlesbare Datei steht
/// im Banner, und keine Prüfung läuft. Echte Dateien, die Wege des Kerns
/// (<see cref="GanglinienFormatwege.Kern"/>), das Format der Messreihe aus der Hülle. Kultur
/// de-DE (<see cref="EposBunitContext"/>).
/// </summary>
public class GanglinienFormatwegeTests : EposBunitContext
{
    private readonly List<string> _dateien = new();

    public GanglinienFormatwegeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    protected override void Dispose(bool disposing)
    {
        foreach (string d in _dateien)
            try { File.Delete(d); } catch { }
        base.Dispose(disposing);
    }

    private string Datei(IEnumerable<string> zeilen)
    {
        string pfad = Path.Combine(Path.GetTempPath(), "epos-im4b-" + Guid.NewGuid().ToString("N") + ".csv");
        File.WriteAllLines(pfad, zeilen);
        _dateien.Add(pfad);
        return pfad;
    }

    /// <summary>Spotpreise eines Jahres im Format der Bundesnetzagentur/SMARD.</summary>
    private string Spotdatei(int jahr)
    {
        var zeilen = new List<string> { "Datum;von;Zeitzone von;bis;Zeitzone bis;Spotmarktpreis in ct/kWh" };
        for (var t = new DateTime(jahr, 1, 1); t.Year == jahr; t = t.AddHours(1))
            zeilen.Add(t.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";" + t.ToString("HH:mm", CultureInfo.InvariantCulture)
                       + ";CET;" + t.AddHours(1).ToString("HH:mm", CultureInfo.InvariantCulture) + ";CET;"
                       + (4 + t.Hour * 0.5).ToString("0.000", CultureInfo.InvariantCulture).Replace('.', ','));
        return Datei(zeilen);
    }

    /// <summary>Eine 15-Minuten-Messreihe ab 06:00 über drei Tage.</summary>
    private string Messdatei()
    {
        var zeilen = new List<string> { "Zeitstempel;Wert [kWh]" };
        var beginn = new DateTime(2025, 1, 1, 6, 0, 0);
        for (int i = 0; i < 3 * 96; i++)
            zeilen.Add(beginn.AddMinutes(15.0 * i).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + ";0.25");
        return Datei(zeilen);
    }

    private string Fehlt() => Path.Combine(Path.GetTempPath(), "epos-im4b-fehlt-" + Guid.NewGuid().ToString("N") + ".csv");

    private static IElement Knopf<T>(IRenderedComponent<T> cut, string text) where T : Microsoft.AspNetCore.Components.IComponent
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // ================================================================== Spotpreis

    private sealed class Spotstand
    {
        public readonly List<(string Pfad, int Jahr)> Geprueft = new();
        public bool Geschlossen;
    }

    private IRenderedComponent<SpotpreisImportDialog> Spot(string pfad, Spotstand s)
        => Render<SpotpreisImportDialog>(p => p
            .Add(x => x.Waehlen, f => Task.FromResult<string?>(pfad))
            .Add(x => x.Formatwege, GanglinienFormatwege.Kern())
            .Add(x => x.Pruefen, (d, jahr) =>
            {
                s.Geprueft.Add((d, jahr));
                return Task.FromResult(new SpotpreisPruefung(true, "Protokoll", jahr > 0 ? jahr : 1999));
            })
            .Add(x => x.Geschlossen, (bool ok) => s.Geschlossen = true));

    [Fact]
    public void Der_Spotpreis_laeuft_ueber_den_Optionendialog_mit_Preisachse_und_Jahresfeld()
    {
        var s = new Spotstand();
        var cut = Spot(Spotdatei(2025), s);
        cut.Find(".epos-dateiwahl button").Click();

        cut.WaitForState(() => cut.FindComponents<GanglinieImportOptionenDialog>().Count == 1);
        var optionen = cut.FindComponent<GanglinieImportOptionenDialog>();
        Assert.Equal(GanglinienImportFormat.KENNUNG_SPOTPREIS, optionen.Instance.Format!.Kennung);
        Assert.Equal(Resource.IMPORT_TITEL_SPOTPREIS, optionen.Find(".epos-dialog-titel").TextContent);
        Assert.Equal(2025, optionen.Instance.Jahr);
        Assert.Single(optionen.FindComponents<Ganzzahlfeld>().Where(f => f.Instance.Feldname == "Jahr"));
        Assert.True(optionen.Instance.Probe!.Erfolgreich, optionen.Instance.Probe.Grund);
        Assert.Contains("8.760 Werte in ct/kWh", optionen.Instance.Probe.Zeile);
        Assert.StartsWith("Jahr der Datei 2025", optionen.Find(".epos-importoptionen-kennzahlen").TextContent);
        Assert.Contains(Resource.IMPORT_ACHSE_SPOTPREIS, optionen.Markup);
        Assert.Empty(s.Geprueft);                                   // erst nach OK

        Knopf(optionen, "OK").Click();

        cut.WaitForAssertion(() => Assert.Single(s.Geprueft));
        Assert.Equal(2025, s.Geprueft[0].Jahr);
        Assert.Equal(2025, cut.Instance.Jahr);
        Assert.Equal("Protokoll", cut.Instance.Protokoll);
        Assert.False(cut.Instance.StatusIstFehler);
        Assert.Empty(cut.FindComponents<GanglinieImportOptionenDialog>());
    }

    [Fact]
    public void Ein_geaendertes_Jahr_geht_an_die_Pruefung_der_Huelle()
    {
        var s = new Spotstand();
        var cut = Spot(Spotdatei(2025), s);
        cut.Find(".epos-dateiwahl button").Click();
        cut.WaitForState(() => cut.FindComponents<GanglinieImportOptionenDialog>().Count == 1);
        var optionen = cut.FindComponent<GanglinieImportOptionenDialog>();

        optionen.FindComponents<Ganzzahlfeld>().First(f => f.Instance.Feldname == "Jahr").Find("input").Input("2030");
        Knopf(optionen, "OK").Click();

        cut.WaitForAssertion(() => Assert.Single(s.Geprueft));
        Assert.Equal(2030, s.Geprueft[0].Jahr);
        Assert.Equal(2030, cut.Instance.Jahr);
    }

    [Fact]
    public void Abbrechen_im_Optionendialog_laesst_den_Spotpreisdialog_wie_er_war_und_Esc_schliesst_ihn_nicht()
    {
        var s = new Spotstand();
        var cut = Spot(Spotdatei(2025), s);
        cut.Find(".epos-dateiwahl button").Click();
        cut.WaitForState(() => cut.FindComponents<GanglinieImportOptionenDialog>().Count == 1);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(s.Geschlossen);                               // der Optionendialog steht

        Knopf(cut.FindComponent<GanglinieImportOptionenDialog>(), "Abbrechen").Click();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindComponents<GanglinieImportOptionenDialog>()));
        Assert.Empty(s.Geprueft);
        Assert.Equal("", cut.Instance.Protokoll);
        Assert.Equal("", cut.Instance.Status);
        Assert.Equal(0, cut.Instance.Jahr);
    }

    [Fact]
    public void Eine_unlesbare_Spotpreisdatei_steht_im_Banner_ohne_Pruefung()
    {
        var s = new Spotstand();
        var cut = Spot(Fehlt(), s);
        cut.Find(".epos-dateiwahl button").Click();

        cut.WaitForAssertion(() => Assert.True(cut.Instance.StatusIstFehler));
        Assert.StartsWith("Die Datei passt nicht zum Format", cut.Instance.Status);
        Assert.Single(cut.FindAll(".epos-warnbanner-text"));
        Assert.Empty(cut.FindComponents<GanglinieImportOptionenDialog>());
        Assert.Empty(s.Geprueft);
    }

    [Fact]
    public void Ohne_Formatwege_prueft_der_Spotpreisdialog_wie_bisher_im_Jahr_der_Datei()
    {
        var geprueft = new List<int>();
        var cut = Render<SpotpreisImportDialog>(p => p
            .Add(x => x.Waehlen, f => Task.FromResult<string?>("C:/spot.csv"))
            .Add(x => x.Pruefen, (d, jahr) => { geprueft.Add(jahr); return Task.FromResult(new SpotpreisPruefung(true, "", 2024)); }));
        cut.Find(".epos-dateiwahl button").Click();

        cut.WaitForAssertion(() => Assert.Equal(new[] { 0 }, geprueft));
        Assert.Equal(2024, cut.Instance.Jahr);
    }

    // ================================================================== TWW-Messreihen

    private IRenderedComponent<TwwMessreihenDialog> Tww(string pfad, List<string> geprueft)
        => Render<TwwMessreihenDialog>(p => p
            .Add(x => x.DateiWaehlen, f => Task.FromResult(pfad))
            .Add(x => x.Formatwege, GanglinienFormatwege.Kern())
            .Add(x => x.Messformat, e =>
            {
                e.LueckenschwelleAnteil ??= 0.05;                 // ohne Datenbank: die Vorgabe des Lesers
                return ZapfprofilHuelle.Messformat(e);
            })
            .Add(x => x.Pruefen, (d, e, abbruch) =>
            {
                geprueft.Add(d);
                return Task.FromResult(new TwwMessreihenpruefungDaten { Bezeichnung = "messung", Zusammenfassung = "gut" });
            }));

    [Fact]
    public void Die_Messreihe_zeigt_im_Optionendialog_die_Tagesansicht_ohne_Jahresfeld()
    {
        var geprueft = new List<string>();
        string pfad = Messdatei();
        var cut = Tww(pfad, geprueft);
        Knopf(cut, "Datei wählen…").Click();

        cut.WaitForState(() => cut.FindComponents<GanglinieImportOptionenDialog>().Count == 1);
        var optionen = cut.FindComponent<GanglinieImportOptionenDialog>();
        Assert.Equal(GanglinienBildart.Tag, optionen.Instance.Format!.Bildart);
        Assert.Equal(Resource.IMPORT_TITEL_TWW_MESSREIHE, optionen.Find(".epos-dialog-titel").TextContent);
        Assert.Empty(optionen.FindComponents<Ganzzahlfeld>().Where(f => f.Instance.Feldname == "Jahr"));
        Assert.True(optionen.Instance.Probe!.Erfolgreich, optionen.Instance.Probe.Grund);
        Assert.Equal("3,0 Tage gelesen · gezeigt: 02.01.2025",
                     optionen.Find(".epos-importoptionen-kennzahlen").TextContent);
        Assert.Contains(Resource.IMPORT_ACHSE_TAGESSTUNDE, optionen.Markup);
        Assert.DoesNotContain(Resource.CHART_ACHSE_JAHRESSTUNDEN, optionen.Markup);
        Assert.Empty(geprueft);

        Knopf(optionen, "OK").Click();

        cut.WaitForAssertion(() => Assert.Equal(new[] { pfad }, geprueft));
        Assert.Equal("3,0 Tage gelesen · gezeigt: 02.01.2025", cut.Instance.Vorschau);
        Assert.Empty(cut.FindComponents<GanglinieImportOptionenDialog>());
    }

    [Fact]
    public void Eine_unlesbare_Messdatei_steht_im_Banner_und_wird_nicht_geprueft()
    {
        var geprueft = new List<string>();
        var cut = Tww(Fehlt(), geprueft);
        Knopf(cut, "Datei wählen…").Click();

        cut.WaitForAssertion(() => Assert.StartsWith("Die Datei passt nicht zum Format", cut.Instance.Meldung));
        Assert.Empty(cut.FindComponents<GanglinieImportOptionenDialog>());
        Assert.Empty(geprueft);
        Assert.Equal("", cut.Instance.Vorschau);
    }

    [Fact]
    public void Abbrechen_im_Optionendialog_laesst_die_Messdaten_wie_sie_waren()
    {
        var geprueft = new List<string>();
        bool geschlossen = false;
        var cut = Render<TwwMessreihenDialog>(p => p
            .Add(x => x.DateiWaehlen, f => Task.FromResult(Messdatei()))
            .Add(x => x.Formatwege, GanglinienFormatwege.Kern())
            .Add(x => x.Messformat, e => { e.LueckenschwelleAnteil ??= 0.05; return ZapfprofilHuelle.Messformat(e); })
            .Add(x => x.Pruefen, (d, e, abbruch) => { geprueft.Add(d); return Task.FromResult(new TwwMessreihenpruefungDaten()); })
            .Add(x => x.Geschlossen, g => geschlossen = true));
        Knopf(cut, "Datei wählen…").Click();
        cut.WaitForState(() => cut.FindComponents<GanglinieImportOptionenDialog>().Count == 1);

        cut.Find(".epos-tww-messreihen").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(geschlossen);

        Knopf(cut.FindComponent<GanglinieImportOptionenDialog>(), "Abbrechen").Click();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindComponents<GanglinieImportOptionenDialog>()));
        Assert.Empty(geprueft);
        Assert.Equal("", cut.Instance.Meldung);
    }

    [Fact]
    public void Die_Huelle_der_Messreihen_reicht_Formatwege_und_Messformat()
    {
        IReadOnlyDictionary<string, object> gaben = ZapfprofilHuelle.MessreihenGaben(0);
        Assert.IsType<GanglinienFormatwege>(gaben["Formatwege"]);
        var format = Assert.IsType<Func<TwwMessreiheneingabeDaten, GanglinienImportFormat>>(gaben["Messformat"]);
        Assert.Equal(GanglinienImportFormat.KENNUNG_TWW_MESSREIHE,
                     format(new TwwMessreiheneingabeDaten { LueckenschwelleAnteil = 0.05 }).Kennung);
    }
}
