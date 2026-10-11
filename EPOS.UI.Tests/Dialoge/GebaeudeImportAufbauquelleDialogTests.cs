using AngleSharp.Dom;
using Bunit;
using EPOS.Kern.Tests;
using EPOS.UI.Dialoge.Import;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Wahl der Aufbauquelle</b> (Anwenderentscheid vom 08.10.2026), gefahren über die echte Hülle: Beim Weg „IFC + Projektdatei“
/// schlägt die Standprüfung beim Standzonenhaus an — die Rückfrage steht mit Anteil, Anzeichen und Tabelle, ohne Vorgabe; OK ist
/// gesperrt mit dem Grund der Prüfung; „Projektdatei“ zeigt deren U in der Zuordnung, „IFC“ den Stand vor dem Dazuladen; „Abbrechen
/// und neu exportieren“ schließt nach dem Hinweis ohne Übernahme; ein stimmiges Paar und der Weg „nur Projektdatei“ fragen nicht;
/// die Texte deutsch und englisch.
/// </summary>
public class GebaeudeImportAufbauquelleDialogTests : EposBunitContext
{
    private readonly List<string> _dateien = new();
    private GebaeudeImportStand? _letzterStand;
    private int _geschlossen;
    private GebaeudeImportErgebnis? _ergebnis;

    public GebaeudeImportAufbauquelleDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            foreach (string d in _dateien)
                try { File.Delete(d); } catch (IOException) { }
        base.Dispose(disposing);
    }

    private string Merken(string pfad)
    {
        _dateien.Add(pfad);
        return pfad;
    }

    /// <summary>
    /// Der Dialog mit den Gaben der echten Hülle; <paramref name="datei"/> = die Gebäudedatei (ohne: die HottCAD-Fassung der
    /// IFC-Probe), <paramref name="probe"/> = die dazuzuladende Projektdatei. Der letzte Stand der Zuordnung wird mitgeschrieben.
    /// </summary>
    private IRenderedComponent<GebaeudeImportDialog> Bauen(SqprojProbenErzeuger? probe, string? datei = null)
    {
        string gebaeude = datei ?? Merken(SqprojProbenErzeuger.HottcadZonenhaus(Wurzel()));
        string? sq = probe is null ? null : Merken(probe.Schreiben(SqprojProbenErzeuger.TempPfad("aufbauquelle")));
        var huelle = new GebaeudeImportHuelle(ios: false);
        IReadOnlyDictionary<string, object> g = huelle.Gaben();
        var zuordnen = (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)g["Zuordnen"];
        return Render<GebaeudeImportDialog>(c =>
        {
            c.Add(x => x.Profil, g["Profil"] as GebaeudeImportProfilDaten);
            c.Add(x => x.Baualtersklassen, (IReadOnlyList<string>)g["Baualtersklassen"]);
            c.Add(x => x.DateiWaehlen, (Func<string, Task<GebaeudeDateiwahl?>>)(_ =>
                Task.FromResult<GebaeudeDateiwahl?>(new GebaeudeDateiwahl(gebaeude, Path.GetFileName(gebaeude), new FileInfo(gebaeude).Length))));
            c.Add(x => x.Lesen, (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)g["Lesen"]);
            c.Add(x => x.Zuordnen, (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)(a => _letzterStand = zuordnen(a)));
            c.Add(x => x.Pruefen, (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)g["Pruefen"]);
            c.Add(x => x.Uebernehmen, (Func<GebaeudeImportErgebnis, Task<string?>>)(_ => Task.FromResult<string?>(null)));
            c.Add(x => x.Geschlossen, (GebaeudeImportErgebnis? e) => { _geschlossen++; _ergebnis = e; });
            c.Add(x => x.ProjektdateiWaehlen, (Func<Task<string?>>)(() => Task.FromResult(sq)));
            c.Add(x => x.ProjektdateiLesen, (Func<string, int, CancellationToken, Task<GebaeudeProjektdateiDaten>>)g["ProjektdateiLesen"]);
            c.Add(x => x.ProjektdateiEntfernen, (Action)g["ProjektdateiEntfernen"]);
            c.Add(x => x.AufbauquelleWaehlen, (Func<string, GebaeudeProjektdateiDaten?>)g["AufbauquelleWaehlen"]);
        });
    }

    private static void Einlesen(IRenderedComponent<GebaeudeImportDialog> cut)
    {
        IElement Dateiknopf() => cut.FindAll("button").First(k => k.TextContent.Contains(cut.Instance.Texte.DateiKnopf.TrimEnd('…', '.')));
        Dateiknopf().Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-gebimport-zeilen tbody tr")));
        cut.WaitForAssertion(() => Assert.False(Dateiknopf().HasAttribute("disabled")));
    }

    private static IElement Aktion(IRenderedComponent<GebaeudeImportDialog> cut, string aktion)
        => cut.Find("button[data-aktion='" + aktion + "']");

    private static void Laden(IRenderedComponent<GebaeudeImportDialog> cut)
    {
        Aktion(cut, "projektdatei").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-gebimport-sqbilanz, .epos-gebimport-sqbanner")));
        cut.WaitForAssertion(() => Assert.False(Aktion(cut, "projektdatei").HasAttribute("disabled")));
    }

    private static IElement OkKnopf(IRenderedComponent<GebaeudeImportDialog> cut)
        => cut.FindAll("button.epos-knopf--primaer").Single(k => k.TextContent == cut.Instance.Texte.Ok);

    /// <summary>Das U der Fassade Süd in der Bauteilliste des letzten Stands.</summary>
    private string UFassade()
        => _letzterStand!.Bauteile!.Zeilen.First(z => z.Bezeichner.StartsWith("Fassade S", StringComparison.Ordinal)).UWert;

    [Fact]
    public void Die_Rueckfrage_steht_mit_Anteil_Anzeichen_und_Tabelle_ohne_Vorgabe()
    {
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(SqprojProbenErzeuger.StandZonenhaus());
        Einlesen(cut);
        Assert.Empty(cut.FindAll(".epos-gebimport-aufbauquelle"));
        Laden(cut);

        Assert.True(cut.Instance.AufbauquelleFrageOffen);
        IElement abschnitt = cut.Find(".epos-gebimport-aufbauquelle");
        Assert.Equal("offen", abschnitt.GetAttribute("data-wahl"));
        GebaeudeStandpruefungDaten p = _letzterStand!.Projektdatei!.Standpruefung!;
        Assert.True(p.Angeschlagen);
        string anteil = Math.Round(p.AnteilProzent, 1).ToString(System.Globalization.CultureInfo.CurrentCulture);
        Assert.Contains(anteil + " % der Hüllfläche", cut.Find(".epos-gebimport-aqsatz").TextContent);

        // Die Anzeichen als Liste (das Baujahr: 1950 in der Projektdatei, das der IFC nicht).
        IReadOnlyList<IElement> anzeichen = cut.FindAll(".epos-gebimport-aqanzeichen li");
        Assert.NotEmpty(anzeichen);
        Assert.Equal(p.Anzeichen.Select(a => a.Schluessel), anzeichen.Select(li => li.GetAttribute("data-anzeichen")));

        // Die Tabelle je Bauteilart mit beiden Median-U; die Wand der Projektdatei mit dem U des Standhauses.
        IReadOnlyList<IElement> arten = cut.FindAll(".epos-gebimport-aqarten tbody tr");
        Assert.Equal(p.JeArt.Count, arten.Count);
        Assert.Contains(arten, tr => tr.QuerySelector("td[data-wert='projektdatei']")!.TextContent == "1,40");
        Assert.Equal(4, cut.FindAll(".epos-gebimport-aqarten thead th").Count);

        // Die Beispiele aufklappbar, höchstens fünf.
        IElement beispiele = cut.Find("details.epos-gebimport-aqbeispiele");
        Assert.False(beispiele.HasAttribute("open"));
        Assert.InRange(beispiele.QuerySelectorAll("tbody tr").Length, 1, 5);

        // Drei Wege, keiner vorgewählt, kein Hauptknopf im Abschnitt; das Banner derselben Warnung entfällt.
        IReadOnlyList<IElement> wege = cut.FindAll(".epos-gebimport-aqwege button");
        Assert.Equal(new[] { "aufbauquelle-projektdatei", "aufbauquelle-datei", "aufbauquelle-neu-exportieren" },
                     wege.Select(k => k.GetAttribute("data-aktion")));
        Assert.Equal(new[] { "Aufbauten der Projektdatei verwenden", "Aufbauten der IFC verwenden", "Abbrechen und neu exportieren" },
                     wege.Select(k => k.TextContent));
        Assert.All(wege, k => Assert.DoesNotContain("epos-knopf--primaer", k.ClassName));
        Assert.All(wege, k => Assert.False(k.HasAttribute("disabled")));
        Assert.Empty(abschnitt.QuerySelectorAll("input[type=radio]"));
        Assert.DoesNotContain(cut.FindAll(".epos-gebimport-sqbanner"), b => b.TextContent.Contains(p.Meldung, StringComparison.Ordinal));
    }

    [Fact]
    public void OK_ist_gesperrt_mit_dem_Grund_der_Pruefung_solange_die_Wahl_offen_ist()
    {
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(SqprojProbenErzeuger.StandZonenhaus());
        Einlesen(cut);
        Laden(cut);

        IElement ok = OkKnopf(cut);
        Assert.Equal("true", ok.GetAttribute("aria-disabled"));
        string grund = ok.GetAttribute("title")!;
        Assert.StartsWith("IFC und Projektdatei stammen aus verschiedenen Projektständen", grund, StringComparison.Ordinal);

        // Der Versuch meldet denselben Grund aus der Prüfung der Hülle; übernommen wird nichts.
        ok.Click();
        cut.WaitForAssertion(() => Assert.Contains(cut.FindAll(".epos-warnbanner"), b => b.TextContent.Contains(grund, StringComparison.Ordinal)));
        Assert.Equal(0, _geschlossen);

        Aktion(cut, "aufbauquelle-datei").Click();
        Assert.False(cut.Instance.AufbauquelleFrageOffen);
        Assert.NotEqual(grund, OkKnopf(cut).GetAttribute("title"));
    }

    [Fact]
    public void Projektdatei_zeigt_deren_U_und_IFC_den_Stand_vor_dem_Dazuladen()
    {
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(SqprojProbenErzeuger.StandZonenhaus());
        Einlesen(cut);
        string heute = UFassade();
        Laden(cut);

        Aktion(cut, "aufbauquelle-projektdatei").Click();
        Assert.Equal(GebaeudeAufbauquelleSchluessel.PROJEKTDATEI, _letzterStand!.Projektdatei!.Aufbauquelle);
        Assert.Equal("projektdatei", cut.Find(".epos-gebimport-aufbauquelle").GetAttribute("data-wahl"));
        Assert.Equal("Aufbauten: Projektdatei", cut.Find(".epos-gebimport-aqquelle").TextContent);
        Assert.Empty(cut.FindAll(".epos-gebimport-aqwege"));
        Assert.StartsWith("1,4", UFassade(), StringComparison.Ordinal);
        Assert.NotEqual(heute, UFassade());

        // Änderbar, solange nicht übernommen ist: der Knopf der anderen Quelle.
        IElement wechsel = Aktion(cut, "aufbauquelle-datei");
        Assert.Equal("Aufbauten der IFC verwenden", wechsel.TextContent);
        wechsel.Click();
        Assert.Equal(GebaeudeAufbauquelleSchluessel.DATEI, _letzterStand!.Projektdatei!.Aufbauquelle);
        Assert.Equal("Aufbauten: IFC", cut.Find(".epos-gebimport-aqquelle").TextContent);
        Assert.Equal(heute, UFassade());
        Assert.Equal("Aufbauten der Projektdatei verwenden", Aktion(cut, "aufbauquelle-projektdatei").TextContent);
    }

    [Fact]
    public void Abbrechen_und_neu_exportieren_schliesst_nach_dem_Hinweis_ohne_Uebernahme()
    {
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(SqprojProbenErzeuger.StandZonenhaus());
        Einlesen(cut);
        Laden(cut);

        Aktion(cut, "aufbauquelle-neu-exportieren").Click();
        Assert.True(cut.Instance.NeuExportFrageOffen);
        Assert.Contains("aus demselben Projektstand neu", cut.Find(".epos-rueckfrage-text").TextContent);

        // „Zurück“ lässt den Dialog offen und die Wahl offen.
        cut.FindAll(".epos-rueckfrage button").Single(k => k.TextContent == "Zurück").Click();
        Assert.False(cut.Instance.NeuExportFrageOffen);
        Assert.Equal(0, _geschlossen);
        Assert.True(cut.Instance.AufbauquelleFrageOffen);

        Aktion(cut, "aufbauquelle-neu-exportieren").Click();
        cut.FindAll(".epos-rueckfrage button").Single(k => k.TextContent == "Import beenden").Click();
        cut.WaitForAssertion(() => Assert.Equal(1, _geschlossen));
        Assert.Null(_ergebnis);
    }

    [Fact]
    public void Ein_stimmiges_Paar_zeigt_keine_Rueckfrage()
    {
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(SqprojProbenErzeuger.Zonenhaus());
        Einlesen(cut);
        Laden(cut);

        Assert.False(cut.Instance.AufbauquelleFrageOffen);
        Assert.Empty(cut.FindAll(".epos-gebimport-aufbauquelle"));
        Assert.Null(OkKnopf(cut).GetAttribute("title"));
    }

    [Fact]
    public void Der_Weg_nur_Projektdatei_zeigt_keine_Rueckfrage()
    {
        string sq = Merken(SqprojProbenErzeuger.StandZonenhaus().Schreiben(SqprojProbenErzeuger.TempPfad("aufbauquelle_nur")));
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(null, sq);
        Einlesen(cut);

        Assert.True(cut.Instance.NurProjektdatei);
        Assert.False(cut.Instance.AufbauquelleFrageOffen);
        Assert.Empty(cut.FindAll(".epos-gebimport-aufbauquelle"));
    }

    [Fact]
    public void Die_Texte_stehen_deutsch_und_englisch()
    {
        var deutsch = new GebaeudeImportTexte();
        Assert.Equal("Welche Aufbauten gelten?", deutsch.AqTitel);
        Assert.Equal("Aufbauten der Projektdatei verwenden", deutsch.AqProjektdatei);
        Assert.Equal("Aufbauten der IFC verwenden", deutsch.AqDatei);
        Assert.Equal("Abbrechen und neu exportieren", deutsch.AqNeuExport);
        Assert.Equal("Aufbauten: Projektdatei", deutsch.AqGewaehltProjektdatei);

        GebaeudeImportTexte englisch;
        using (new Kulturvorrichtung("en-US")) englisch = new GebaeudeImportTexte();
        Assert.Equal("Which constructions apply?", englisch.AqTitel);
        Assert.Equal("Use constructions of the project file", englisch.AqProjektdatei);
        Assert.Equal("Use constructions of the IFC", englisch.AqDatei);
        Assert.Equal("Cancel and export again", englisch.AqNeuExport);
        Assert.Equal("Constructions: project file", englisch.AqGewaehltProjektdatei);
        Assert.Equal("Constructions: IFC", englisch.AqGewaehltDatei);
        Assert.StartsWith("The constructions of the IFC and the project file do not match", englisch.AqSatz, StringComparison.Ordinal);
        Assert.StartsWith("IFC and project file come from different project states", englisch.AqSperre, StringComparison.Ordinal);
        Assert.Equal("End import", englisch.AqNeuExportJa);
        Assert.Equal("Back", englisch.AqNeuExportNein);
    }
    private static string Wurzel()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;
        Assert.NotNull(d);
        return d!.FullName;
    }
}
