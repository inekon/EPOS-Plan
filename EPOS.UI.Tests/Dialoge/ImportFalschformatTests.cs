using System.Text;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Solarthermie;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Kein Absturz bei falschem Dateiformat</b> (IM-1): Die CSV-Importe der Dialoge enden bei einer
/// falschen Datei in einer benannten Ablehnung im Warnbanner (Dateiname und Grund), der Dialog bleibt
/// gezeichnet und bedienbar, und eine Ausnahme aus der Kette fängt die gemeinsame Stelle der Hüllen
/// (<see cref="Importfang"/>) samt Vermerk im Ausnahmeprotokoll.
/// </summary>
public sealed class ImportFalschformatTests : EposBunitContext
{
    private readonly string _ordner =
        Path.Combine(Path.GetTempPath(), "epos-im1-ui-" + Guid.NewGuid().ToString("N").Substring(0, 8));

    private static IReadOnlyList<Katalogfilterzeile> Katalog => new[]
    {
        Zeitreihenproben.Zeile(31, "PV Dach Ost", beschreibung: "Messung 2025", jahresarbeitMwh: 9.5, spitzeKw: 10.0)
    };

    public ImportFalschformatTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        Directory.CreateDirectory(_ordner);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { try { Directory.Delete(_ordner, true); } catch { } }
        base.Dispose(disposing);
    }

    /// <summary>Eine CSV mit zehn Werten statt 8 760 — das gemeldete Muster „falsches Format“.</summary>
    private string ZehnZeilen()
    {
        string pfad = Path.Combine(_ordner, "falsch.csv");
        File.WriteAllText(pfad, string.Join("\n", Enumerable.Range(0, 10).Select(i => i + ".5")) + "\n",
                          new UTF8Encoding(false));
        return pfad;
    }

    private static IElement Knopf<T>(IRenderedComponent<T> cut, string text) where T : Microsoft.AspNetCore.Components.IComponent
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    [Fact]
    public void PvGanglinie_falsches_Format_meldet_Datei_und_Grund_im_Banner()
    {
        string pfad = ZehnZeilen();
        var wege = new GanglinienKatalogwege
        {
            Katalogzeilen = () => Task.FromResult(Katalog),
            DateiWaehlen = _ => Task.FromResult<string?>(pfad),
            Einlesen = PvGanglinieKatalogGaben.Einlesen,
            NennleistungVorschlagen = PvGanglinieKatalogGaben.Vorschlagen,
            NennleistungPruefen = PvGanglinieImportCtrl.Pruefhinweis,
            EinlesenMitNennleistung = PvGanglinieKatalogGaben.EinlesenMitNennleistung
        };
        var cut = Render<PvGanglinieDialog>(p => p
            .Add(x => x.Katalogbetrieb, true)
            .Add(x => x.Katalogwege, wege)
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.PvGanglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand()));

        cut.Find("button.epos-importknopf").Click();
        Knopf(cut, "Datei Auswählen...").Click();
        cut.WaitForAssertion(() => Assert.Contains("falsch.csv", cut.Markup));
        Assert.Null(cut.Instance.Katalogseite!.Nennleistung);   // keine Vorbelegung aus einer falschen Datei

        Knopf(cut, "Datei Einlesen...").Click();
        cut.WaitForAssertion(() => Assert.StartsWith("Datei \u201efalsch.csv\u201c: ", cut.Instance.Katalogseite!.Importmeldung));
        Assert.Contains("10 Werte", cut.Instance.Katalogseite!.Importmeldung);
        Assert.True(cut.Instance.Katalogseite.ImportOffen);          // die Überlagerung bleibt mit dem Grund offen
        Assert.Contains(cut.Instance.Katalogseite.Importmeldung, cut.Markup);
        Assert.NotEmpty(cut.FindAll(".epos-einlesen"));               // gezeichnet, keine Fehlansicht
    }

    [Fact]
    public void Solarganglinie_falsches_Format_meldet_Datei_und_Grund_im_Banner()
    {
        string pfad = ZehnZeilen();
        var wege = new GanglinienKatalogwege
        {
            Katalogzeilen = () => Task.FromResult(Katalog),
            DateiWaehlen = _ => Task.FromResult<string?>(pfad),
            Einlesen = SolarganglinieKatalogGaben.Einlesen
        };
        var cut = Render<SolarganglinieDialog>(p => p
            .Add(x => x.Katalogbetrieb, true)
            .Add(x => x.Katalogwege, wege)
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.Solarganglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand()));

        cut.Find("button.epos-importknopf").Click();
        Knopf(cut, "Datei Auswählen...").Click();
        Knopf(cut, "Datei Einlesen...").Click();
        cut.WaitForAssertion(() => Assert.Contains("Datei \u201efalsch.csv\u201c: ", cut.Markup));
        Assert.NotEmpty(cut.FindAll(".epos-einlesen"));
    }

    [Fact]
    public void Eine_Ausnahme_der_Kette_wird_benannt_abgelehnt_und_vermerkt()
    {
        var vermerke = new List<string>();
        Action<string> hoerer = z => { lock (vermerke) vermerke.Add(z); };
        Ausnahmeprotokoll.Vermerkt += hoerer;
        try
        {
            var wege = new GanglinienKatalogwege
            {
                Katalogzeilen = () => Task.FromResult(Katalog),
                DateiWaehlen = _ => Task.FromResult<string?>("D:/Daten/gesperrt.csv"),
                Einlesen = (p, _) => Importfang.Starten<GanglinienKatalogimport>(p,
                    () => throw new IOException("Die Datei wird von einem anderen Prozess verwendet."),
                    a => new GanglinienKatalogimport(false, true, "", a.Text, ""))
            };
            var cut = Render<SolarganglinieDialog>(p => p
                .Add(x => x.Katalogbetrieb, true)
                .Add(x => x.Katalogwege, wege)
                .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.Solarganglinie))
                .Add(x => x.Filterstandvorgabe, new Katalogfilterstand()));

            cut.Find("button.epos-importknopf").Click();
            Knopf(cut, "Datei Auswählen...").Click();
            Knopf(cut, "Datei Einlesen...").Click();

            cut.WaitForAssertion(() => Assert.Contains(
                "Die Datei \u201egesperrt.csv\u201c konnte nicht eingelesen werden: Die Datei wird von einem anderen Prozess verwendet.",
                cut.Markup));
            Assert.NotEmpty(cut.FindAll(".epos-einlesen"));
            lock (vermerke) Assert.Contains(vermerke, z => z.Contains("gesperrt.csv") && z.Contains("IOException"));
        }
        finally
        {
            Ausnahmeprotokoll.Vermerkt -= hoerer;
        }
    }

    [Fact]
    public async Task Ganglinienimport_der_Verwaltung_endet_bei_falscher_Datei_mit_Fehler_statt_Ausnahme()
    {
        string pfad = Path.Combine(_ordner, "binaer.csv");
        File.WriteAllBytes(pfad, new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00, 0x00, 0x01, 0x02 });
        IList<PruefMeldung>? vorgelegt = null;
        GanglinienImportErgebnis e = await StromganglinieAdminHuelle.Einlesen(pfad, GanglinienRaster.Unbekannt,
            new GanglinienImportRueckrufe
            {
                Optionen = (_, v) => Task.FromResult(v.Vorschlag),
                Protokoll = (m, _, _) => { vorgelegt = m; return Task.FromResult(false); }
            });
        Assert.Equal(ImportAusgang.Fehler, e.Ausgang);
        Assert.NotNull(vorgelegt);
        Assert.Contains(vorgelegt!, m => m.Schluessel == GanglinienDatei.SchluesselKeinText);
    }

    [Fact]
    public void Optionendialog_nennt_den_Grund_wenn_die_Vorschau_nicht_lesbar_ist()
    {
        GanglinienVorschau erkennung = new GanglinienVorschau { Spaltenzahl = 2, Lesbar = true };
        erkennung.Vorschlag.Trennzeichen = ';';
        erkennung.Vorschlag.WertSpalte = 1;
        erkennung.Zeilen.Add(new[] { "01.01.2023 00:00", "220,00" });
        var cut = Render<GanglinieImportOptionenDialog>(p => p
            .Add(x => x.Pfad, Path.Combine(_ordner, "lastgang.csv"))
            .Add(x => x.Erkennung, erkennung)
            .Add(x => x.Vorschau, (string pf, GanglinienImportOptionen o) =>
                Importfang.Starten<GanglinienVorschau?>(pf, () => throw new InvalidDataException("Feld zu lang"),
                                                        a => Importfang.AlsVorschau(a))));

        Assert.Empty(cut.FindAll(".epos-warnbanner, [role=alert]").Where(e => e.TextContent.Contains("nicht lesbar")));
        cut.FindAll(".epos-leiste button")[0].Click();   // „Vorschau aktualisieren"

        cut.WaitForAssertion(() => Assert.StartsWith("Mit diesen Einstellungen ist die Datei nicht lesbar: ",
                                                     cut.Instance.Meldung));
        Assert.Contains("Feld zu lang", cut.Instance.Meldung);
        Assert.Contains(cut.Instance.Meldung, cut.Markup);
        Assert.Equal(3, cut.FindAll(".epos-leiste button").Count);   // der Dialog bleibt bedienbar
    }
}
