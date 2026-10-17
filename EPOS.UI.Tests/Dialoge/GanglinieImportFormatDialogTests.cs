using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Der Optionendialog „Format und Vorschau“ nach einer Formatbeschreibung</b> (IM-4): Ein
/// Sonderformat blendet Einheit, Raster und Konvention aus, zeigt Bild und Probezeile in seiner
/// Einheit, nennt keine Kennzahlen, wo es keine hat, und lehnt am OK BENANNT ab, was nicht zu
/// seiner festen Werteanzahl passt. Dazu das Jahresfeld (Formate mit Jahr) und der Baustein
/// <see cref="GanglinienFormatLauf"/>. Deutsche Texte: <see cref="EposBunitContext"/>.
/// </summary>
public class GanglinieImportFormatDialogTests : EposBunitContext
{
    private readonly List<string> _dateien = new();

    public GanglinieImportFormatDialogTests()
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

    /// <summary>„Nr;Wert“ mit Kopfzeile und <paramref name="anzahl"/> Werten (Wert = 10,5).</summary>
    private string Datei(int anzahl)
    {
        var zeilen = new List<string> { "Nr;Wert" };
        for (int i = 1; i <= anzahl; i++) zeilen.Add(i + ";10,5");
        string pfad = Path.Combine(Path.GetTempPath(), "epos-im4-" + Guid.NewGuid().ToString("N") + ".csv");
        File.WriteAllText(pfad, string.Join("\r\n", zeilen) + "\r\n");
        _dateien.Add(pfad);
        return pfad;
    }

    private static GanglinienImportFormat Quellprofil => GanglinienImportFormat.Quellprofil(DbWerte.WQ_PROFIL_BETRIEBSART_TAG);

    private IRenderedComponent<GanglinieImportOptionenDialog> Zeige(
        string pfad, GanglinienImportFormat? format, Action<GanglinienImportOptionen?>? geschlossen = null)
        => Render<GanglinieImportOptionenDialog>(p => p
            .Add(x => x.Pfad, pfad)
            .Add(x => x.Format, format)
            .Add(x => x.Erkennung, GanglinienDatei.Erkenne(pfad, format))
            .Add(x => x.Vorschau, (Func<string, GanglinienImportOptionen, Task<GanglinienVorschau?>>)(
                (d, o) => Task.FromResult<GanglinienVorschau?>(GanglinienDatei.Vorschau(d, o, format))))
            .Add(x => x.Geschlossen, (GanglinienImportOptionen? o) => geschlossen?.Invoke(o)));

    private static IElement Knopf(IRenderedComponent<GanglinieImportOptionenDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // ===================================================================== Feldbestand

    [Fact]
    public void Das_Quellprofil_blendet_Einheit_Raster_und_Konvention_aus()
    {
        var ganglinie = Zeige(Datei(8760), null);
        var quell = Zeige(Datei(365), Quellprofil);

        Assert.Equal(7, ganglinie.FindAll("select").Count);
        Assert.Equal(4, quell.FindAll("select").Count);   // Trennzeichen, Dezimal, Wert- und Zeitspalte
        Assert.DoesNotContain(Resource.IMPORT_LBL_EINHEIT, quell.Markup);
        Assert.DoesNotContain(Resource.IMPORT_LBL_RASTER, quell.Markup);
        Assert.DoesNotContain(Resource.IMPORT_LBL_KONVENTION, quell.Markup);
        Assert.Contains(Resource.IMPORT_LBL_EINHEIT, ganglinie.Markup);
        Assert.Equal(Resource.IMPORT_TITEL_QUELLPROFIL, quell.Find(".epos-dialog-titel").TextContent);
        Assert.Equal(Resource.IMPORT_TITEL_OPTIONEN, ganglinie.Find(".epos-dialog-titel").TextContent);
        Assert.Empty(quell.FindAll("input[name=Jahr], [data-feld=Jahr]"));
    }

    [Fact]
    public void Das_Bild_des_Quellprofils_steht_in_Grad_Celsius_ohne_Kennzahlen()
    {
        var cut = Zeige(Datei(365), Quellprofil);

        Assert.True(cut.Instance.Probe!.Erfolgreich);
        Assert.Equal("365 Datenzeilen gelesen → 365 Werte in °C", cut.Find(".epos-importoptionen-probe").TextContent);
        Assert.Empty(cut.FindAll(".epos-importoptionen-kennzahlen"));
        var bild = cut.FindComponent<DiagrammSvg>();
        Assert.Equal("°C", bild.Instance.Einheit);
        Assert.Contains(Resource.CHART_ACHSE_QUELLTEMPERATUR, cut.Markup);
    }

    [Fact]
    public void Die_Ganglinie_nennt_Jahressumme_und_Hoechstleistung_und_zeichnet_in_kW()
    {
        var cut = Zeige(Datei(8760), null);

        Assert.Equal("Jahressumme 91.980 kWh · Höchstleistung 10,5 kW",
                     cut.Find(".epos-importoptionen-kennzahlen").TextContent);
        Assert.Equal("kW", cut.FindComponent<DiagrammSvg>().Instance.Einheit);
    }

    // ===================================================================== feste Werteanzahl

    [Fact]
    public void Eine_falsche_Werteanzahl_lehnt_OK_benannt_ab_und_der_Dialog_bleibt_offen()
    {
        bool geschlossen = false;
        var cut = Zeige(Datei(300), Quellprofil, _ => geschlossen = true);

        Assert.False(cut.Instance.Probe!.Erfolgreich);
        Assert.Contains("abgelehnt", cut.Find(".epos-importoptionen-probe").TextContent);

        Knopf(cut, Resource.SIM_BTN_OK).Click();

        Assert.False(geschlossen);
        Assert.Equal("Die Datei passt nicht zum Format und wird nicht übernommen: "
                     + "Die Datei liefert 300 Werte, erwartet sind genau 365.", cut.Instance.Meldung);
        Assert.Contains("nicht übernommen", cut.Find(".epos-warnbanner-text").TextContent);
    }

    [Fact]
    public void Ein_passendes_Fenster_ueber_die_Zeilenzahl_besteht_das_OK()
    {
        GanglinienImportOptionen? ergebnis = null;
        var cut = Zeige(Datei(400), Quellprofil, o => ergebnis = o);
        Assert.False(cut.Instance.Probe!.Erfolgreich);

        cut.FindAll(".epos-importoptionen-raster input")[1].Input("365");
        Knopf(cut, Resource.SIM_BTN_OK).Click();

        Assert.NotNull(ergebnis);
        Assert.Equal(365, ergebnis!.Zeilenzahl);
        Assert.Equal("", cut.Instance.Meldung);
    }

    [Fact]
    public void Der_Lastgang_bestaetigt_wie_bisher_ohne_Pruefschritt()
    {
        GanglinienImportOptionen? ergebnis = null;
        var cut = Zeige(Datei(300), null, o => ergebnis = o);
        Knopf(cut, Resource.SIM_BTN_OK).Click();
        Assert.NotNull(ergebnis);
    }

    // ===================================================================== Jahresfeld

    [Fact]
    public void Ein_Format_mit_Jahr_zeigt_das_Jahresfeld_und_gibt_das_Jahr_zurueck()
    {
        var mitJahr = new GanglinienImportFormat
        {
            Kennung = "PRUEFJAHR", Einheit = "€/MWh", Werteanzahl = 24, Jahresfeld = true
        };
        GanglinienImportOptionen? ergebnis = null;
        var cut = Zeige(Datei(24), mitJahr, o => ergebnis = o);

        Assert.Contains(Resource.IMPORT_LBL_JAHR, cut.Markup);
        var felder = cut.FindAll(".epos-importoptionen-raster input");
        Assert.Equal(3, felder.Count);
        felder[2].Input("2025");
        Assert.Equal(2025, cut.Instance.Jahr);

        Knopf(cut, Resource.SIM_BTN_OK).Click();
        Assert.Equal(2025, ergebnis!.Jahr);
        Assert.Equal("€/MWh", cut.FindComponent<DiagrammSvg>().Instance.Einheit);
    }

    // ===================================================================== Baustein

    [Fact]
    public async Task Der_Baustein_liefert_Werte_Optionen_oder_null_bei_Abbruch()
    {
        string pfad = Datei(365);
        var lauf = Render<GanglinienFormatLauf>(p => p.Add(x => x.Wege, GanglinienFormatwege.Kern()));
        Assert.True(lauf.Instance.Bereit);

        Task<GanglinienFormatErgebnis?> laufend = lauf.Instance.Lesen(pfad, Quellprofil);
        lauf.WaitForState(() => lauf.FindComponents<GanglinieImportOptionenDialog>().Count == 1);
        Assert.True(lauf.Instance.EtwasOffen);
        lauf.FindAll("button").First(b => b.TextContent.Trim() == Resource.SIM_BTN_OK).Click();

        GanglinienFormatErgebnis? erg = await laufend;
        Assert.NotNull(erg);
        Assert.True(erg!.Erfolgreich, erg.Grund);
        Assert.Equal(365, erg.Werte.Length);
        Assert.Equal(10.5, erg.Werte[200], 9);
        Assert.NotNull(erg.Optionen);
        Assert.False(lauf.Instance.EtwasOffen);

        laufend = lauf.Instance.Lesen(pfad, Quellprofil);
        lauf.WaitForState(() => lauf.FindComponents<GanglinieImportOptionenDialog>().Count == 1);
        lauf.FindAll("button").First(b => b.TextContent.Trim() == Resource.IMPORT_BTN_ABBRECHEN).Click();
        Assert.Null(await laufend);
    }

    [Fact]
    public async Task Eine_unlesbare_Datei_kommt_mit_Grund_und_ohne_Dialog_zurueck()
    {
        var lauf = Render<GanglinienFormatLauf>(p => p.Add(x => x.Wege, GanglinienFormatwege.Kern()));
        string fehlt = Path.Combine(Path.GetTempPath(), "epos-im4-fehlt-" + Guid.NewGuid().ToString("N") + ".csv");

        GanglinienFormatErgebnis? erg = await lauf.Instance.Lesen(fehlt, Quellprofil);

        Assert.NotNull(erg);
        Assert.False(erg!.Erfolgreich);
        Assert.NotEqual("", erg.Grund);
        Assert.Empty(erg.Werte);
        Assert.Empty(lauf.FindComponents<GanglinieImportOptionenDialog>());
    }

    [Fact]
    public async Task Ohne_Wege_tut_der_Baustein_nichts()
    {
        var lauf = Render<GanglinienFormatLauf>();
        Assert.False(lauf.Instance.Bereit);
        Assert.Null(await lauf.Instance.Lesen(Datei(365), Quellprofil));
        Assert.Equal("", lauf.Markup.Trim());
    }
}
