using System.Globalization;
using System.Text;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Der Import einer PV-Ganglinie über den Optionendialog „Format und Vorschau“</b>: Nach der Dateiwahl
/// steht der Optionendialog (Vorbelegung aus der Formaterkennung des Katalogimports), OK liest die Datei
/// über die Importkette ohne Ablage und belegt die Nennleistung aus der gelesenen Reihe vor, „Einlesen“
/// schreibt die gelesene Reihe samt Nennleistung; Abbrechen im Optionendialog importiert nichts.
/// Dazu das Bild der gelesenen Reihe im Optionendialog (Zeilenzahl, Raster, neu mit „Vorschau
/// aktualisieren“, kein Bild bei unlesbarer Datei) und die Grafik der Satzansicht des PV-Dialogs.
/// Gelesen wird eine echte Datei mit dem Kern — ohne Datenbank.
/// </summary>
public class PvGanglinieImportOptionenTests : EposBunitContext
{
    private readonly string _ordner = Path.Combine(Path.GetTempPath(), "epos-pvg-optionen-" + Guid.NewGuid().ToString("N"));
    private readonly string _datei;

    public PvGanglinieImportOptionenTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        Directory.CreateDirectory(_ordner);
        _datei = Path.Combine(_ordner, "PV Sued.csv");
        var sb = new StringBuilder("Stunde;PV Sued 9,8 kWp\r\n");
        for (int h = 0; h < 8760; h++)
        {
            int t = h % 24;
            double w = t >= 8 && t <= 16 ? 4.0 + (t % 3) * 0.25 : 0.0;
            sb.Append((h + 1).ToString(CultureInfo.InvariantCulture)).Append(';')
              .Append(w.ToString("0.0##", CultureInfo.InvariantCulture)).Append("\r\n");
        }
        File.WriteAllText(_datei, sb.ToString());
    }

    /// <summary>Räumt die Probedatei weg.</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try { Directory.Delete(_ordner, true); } catch (IOException) { }
        }
        base.Dispose(disposing);
    }

    private sealed class Protokoll
    {
        public GanglinienImportErgebnis? Geschrieben;
        public double? Nennleistung;
        public int Schreibzahl;
    }

    private GanglinienKatalogwege Wege(Protokoll p) => new()
    {
        Katalogzeilen = () => Task.FromResult<IReadOnlyList<Katalogfilterzeile>>(Array.Empty<Katalogfilterzeile>()),
        DateiWaehlen = _ => Task.FromResult<string?>(_datei),
        Einlesen = (_, _) => Task.FromResult(new GanglinienKatalogimport(false, true, "", "falscher Weg", "")),
        NennleistungVorschlagen = _ => Task.FromResult(new GanglinienNennleistungsvorschlag(1.0, false, 1.0)),
        NennleistungPruefen = PvGanglinieImportCtrl.Pruefhinweis,
        Lesen = (pfad, r) => GanglinienImportAblauf.OhneAblage(pfad, r, StundenganglinieDatei.Erkenne),
        Vorschau = (pfad, o) => Task.FromResult<GanglinienVorschau?>(GanglinienDatei.Vorschau(pfad, o)),
        NennleistungAusLesung = (pfad, g) =>
        {
            PvGanglinieVorschlag v = PvGanglinieImportCtrl.Vorschlagen(StundenganglinieDatei.AusImport(pfad, g));
            return Task.FromResult(new GanglinienNennleistungsvorschlag(v.VorschlagKwp, v.AusDateikopf, v.SpitzeKw));
        },
        EinlesenGelesen = (_, g, nenn, _) =>
        {
            p.Geschrieben = g;
            p.Nennleistung = nenn;
            p.Schreibzahl++;
            return Task.FromResult(new GanglinienKatalogimport(true, false, "PV Sued", "", "Protokoll"));
        }
    };

    private IRenderedComponent<PvGanglinieDialog> Aufbauen(Protokoll p)
        => Render<PvGanglinieDialog>(x => x
            .Add(d => d.Katalogbetrieb, true)
            .Add(d => d.Katalogwege, Wege(p))
            .Add(d => d.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.PvGanglinie))
            .Add(d => d.Filterstandvorgabe, new Katalogfilterstand()));

    private static IElement Knopf(IRenderedComponent<PvGanglinieDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static IElement OptionenKnopf(IRenderedComponent<PvGanglinieDialog> cut, string text)
        => cut.FindAll(".epos-importoptionen button").First(b => b.TextContent.Trim() == text);

    [Fact]
    public void Die_Dateiwahl_oeffnet_den_Optionendialog_OK_liest_und_fragt_die_Nennleistung()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);

        cut.Find("button.epos-importknopf").Click();
        Knopf(cut, "Datei Auswählen...").Click();

        cut.WaitForAssertion(() => Assert.True(cut.Instance.Katalogseite!.OptionenOffen));
        Assert.NotEmpty(cut.FindAll(".epos-importoptionen"));
        Assert.Null(cut.Instance.Katalogseite!.Gelesen);

        OptionenKnopf(cut, Resource.SIM_BTN_OK).Click();

        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Katalogseite!.Gelesen));
        GanglinienImportErgebnis g = cut.Instance.Katalogseite!.Gelesen!;
        Assert.Equal(8760, g.Werte.Length);
        Assert.Equal('.', g.Optionen.Dezimaltrenner);
        Assert.Equal(1, g.Optionen.WertSpalte);                    // die laufende Nummer ist übersprungen
        Assert.Equal(9.8, cut.Instance.Katalogseite.Nennleistung); // aus dem Dateikopf
        Assert.Equal(0, p.Schreibzahl);

        Knopf(cut, "Datei Einlesen...").Click();
        cut.WaitForAssertion(() => Assert.Equal(1, p.Schreibzahl));
        Assert.Equal(9.8, p.Nennleistung);
        Assert.Same(g, p.Geschrieben);
        Assert.False(cut.Instance.Katalogseite.ImportOffen);
    }

    [Fact]
    public void Abbrechen_im_Optionendialog_importiert_nichts()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);

        cut.Find("button.epos-importknopf").Click();
        Knopf(cut, "Datei Auswählen...").Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.Katalogseite!.OptionenOffen));

        OptionenKnopf(cut, Resource.IMPORT_BTN_ABBRECHEN).Click();

        cut.WaitForAssertion(() => Assert.False(cut.Instance.Katalogseite!.ImportOffen));
        Assert.False(cut.Instance.Katalogseite!.OptionenOffen);
        Assert.Null(cut.Instance.Katalogseite.Gelesen);
        Assert.Equal(0, p.Schreibzahl);
        Assert.Empty(cut.FindAll(".epos-importoptionen"));
    }

    private IRenderedComponent<GanglinieImportOptionenDialog> Optionendialog(
        Func<string, GanglinienImportOptionen, Task<GanglinienVorschau?>> vorschau)
        => Render<GanglinieImportOptionenDialog>(x => x
            .Add(d => d.Pfad, _datei)
            .Add(d => d.Erkennung, StundenganglinieDatei.Erkenne(_datei))
            .Add(d => d.Vorschau, vorschau));

    [Fact]
    public void Der_Optionendialog_zeigt_Zeilenzahl_Raster_und_das_Bild_der_gelesenen_Reihe()
    {
        var cut = Optionendialog((pfad, o) => Task.FromResult<GanglinienVorschau?>(GanglinienDatei.Vorschau(pfad, o)));

        GanglinienProbe erste = cut.Instance.Probe!;
        Assert.True(erste.Erfolgreich);
        Assert.Equal(8760, erste.Datenzeilen);
        Assert.Contains("8.760 Datenzeilen gelesen", cut.Find(".epos-importoptionen-probe").TextContent);
        Assert.Contains(Resource.IMPORT_PROBE_STUNDE, cut.Find(".epos-importoptionen-probe").TextContent);
        Assert.NotEmpty(cut.FindAll(".epos-importoptionen-bild svg"));

        cut.FindAll("button").First(b => b.TextContent.Trim() == Resource.IMPORT_BTN_AKTUALISIEREN).Click();
        cut.WaitForAssertion(() => Assert.NotSame(erste, cut.Instance.Probe));
        Assert.True(cut.Instance.Probe!.Erfolgreich);
        Assert.NotEmpty(cut.FindAll(".epos-importoptionen-bild svg"));
    }

    [Fact]
    public void Eine_unlesbare_Datei_zeigt_kein_Bild_nur_den_Grund()
    {
        var cut = Optionendialog((pfad, o) =>
            Task.FromResult<GanglinienVorschau?>(GanglinienDatei.Vorschau(Path.Combine(_ordner, "fehlt.csv"), o)));
        Assert.NotEmpty(cut.FindAll(".epos-importoptionen-bild svg"));

        cut.FindAll("button").First(b => b.TextContent.Trim() == Resource.IMPORT_BTN_AKTUALISIEREN).Click();

        cut.WaitForAssertion(() => Assert.Null(cut.Instance.Probe));
        Assert.Empty(cut.FindAll(".epos-importoptionen-bild svg"));
        Assert.Empty(cut.FindAll(".epos-importoptionen-probe"));
        Assert.NotEqual("", cut.Instance.Meldung);
    }

    [Fact]
    public void Eine_Reihe_ohne_Raster_nennt_Zeilenzahl_und_Grund_ohne_Bild()
    {
        string kurz = Path.Combine(_ordner, "kurz.csv");
        File.WriteAllText(kurz, "1;2.5\r\n2;3.5\r\n3;4.5\r\n");
        var cut = Render<GanglinieImportOptionenDialog>(x => x
            .Add(d => d.Pfad, kurz)
            .Add(d => d.Erkennung, GanglinienDatei.Erkenne(kurz)));

        Assert.False(cut.Instance.Probe!.Erfolgreich);
        Assert.Contains("3 Datenzeilen gelesen", cut.Find(".epos-importoptionen-probe").TextContent);
        Assert.Empty(cut.FindAll(".epos-importoptionen-bild svg"));
    }

    [Fact]
    public void Die_Satzansicht_zeigt_die_Ganglinie_als_Jahresbild()
    {
        double[] werte = Enumerable.Range(0, 8760).Select(h => (double)(h % 24)).ToArray();
        var wege = new GanglinienKatalogwege
        {
            Katalogzeilen = () => Task.FromResult<IReadOnlyList<Katalogfilterzeile>>(new[]
            {
                Zeitreihenproben.Zeile(31, "PV Dach Ost", zeitintervall: 60, jahresarbeitMwh: 9.5, spitzeKw: 10.0)
            }),
            Kennzahlen = _ => Task.FromResult<GanglinienKennzahlen?>(new GanglinienKennzahlen(9.5, 10.0, 950)),
            Bild = (_, _) => ChartRenderer.JahresverlaufModell("", werte, "kW",
                                                               WindowsFormsApplication1.Zeichnung.Farbrolle.STROM_PV),
            BildTitel = "PV"
        };
        var cut = Render<PvGanglinieDialog>(x => x
            .Add(d => d.Katalogbetrieb, true)
            .Add(d => d.Katalogwege, wege)
            .Add(d => d.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.PvGanglinie))
            .Add(d => d.Filterstandvorgabe, new Katalogfilterstand())
            .Add(d => d.CsvSpeichern, (_, _, _) => Task.CompletedTask));
        Assert.Null(cut.Instance.Grafikkennzahlen);

        cut.Find(".epos-raster tbody tr button").Click();

        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Grafikkennzahlen));
        Assert.NotEmpty(cut.FindAll("svg"));
        Assert.Contains("CSV", cut.Markup);
    }
}
