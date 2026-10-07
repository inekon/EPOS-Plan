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
/// <b>Die Quellenwahl und der Weg „nur Projektdatei“</b> (Datenaustauschkonzept 16.1) im Gebäudeimportdialog, über die echte
/// Hülle: die Wahl mit vier Einträgen (IFC, gbXML, IFC + Projektdatei, nur Projektdatei) setzt den Filter des Dateiwählers;
/// eine gelesene <c>.sqproj</c> — auch über „alle Formate“ — blendet „Projektdatei dazuladen“ aus, zeigt „Quelle:
/// Projektdatei“, die Nordrichtungsabfrage ohne Nordwinkel, die Herkunft „Projektdatei“ und den leisen Hinweis auf offene
/// unbeheizte Räume; „IFC + Projektdatei“ öffnet nach dem Lesen die Wahl der Projektdatei.
/// </summary>
public class GebaeudeImportNurProjektdateiDialogTests : EposBunitContext
{
    private readonly List<string> _dateien = new();
    private readonly List<string> _filter = new();

    public GebaeudeImportNurProjektdateiDialogTests()
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

    private IRenderedComponent<GebaeudeImportDialog> Bauen(string datei, string? projektdatei = null, GebaeudeImportTexte? texte = null)
    {
        var huelle = new GebaeudeImportHuelle(ios: false);
        IReadOnlyDictionary<string, object> g = huelle.Gaben();
        return Render<GebaeudeImportDialog>(c =>
        {
            c.Add(x => x.Profil, g["Profil"] as GebaeudeImportProfilDaten);
            c.Add(x => x.Baualtersklassen, (IReadOnlyList<string>)g["Baualtersklassen"]);
            c.Add(x => x.DateiWaehlen, (Func<string, Task<GebaeudeDateiwahl?>>)(filter =>
            {
                _filter.Add(filter);
                return Task.FromResult<GebaeudeDateiwahl?>(new GebaeudeDateiwahl(datei, Path.GetFileName(datei), new FileInfo(datei).Length));
            }));
            c.Add(x => x.Lesen, (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)g["Lesen"]);
            c.Add(x => x.Zuordnen, (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)g["Zuordnen"]);
            c.Add(x => x.Pruefen, (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)g["Pruefen"]);
            c.Add(x => x.NordrichtungDaten, (Func<GebaeudeNordrichtungDaten?>)g["NordrichtungDaten"]);
            c.Add(x => x.NordrichtungSetzen, (Func<double?, CancellationToken, Task<GebaeudeLesestand>>)g["NordrichtungSetzen"]);
            c.Add(x => x.ProjektdateiWaehlen, (Func<Task<string?>>)(() => Task.FromResult(projektdatei)));
            c.Add(x => x.ProjektdateiLesen, (Func<string, int, CancellationToken, Task<GebaeudeProjektdateiDaten>>)g["ProjektdateiLesen"]);
            c.Add(x => x.ProjektdateiEntfernen, (Action)g["ProjektdateiEntfernen"]);
            if (texte is not null) c.Add(x => x.Texte, texte);
        });
    }

    /// <summary>
    /// Das Sporthaus ohne Nordwinkel: zwei Simulationszonen, die zweite mit dem unbeheizten Lager (wie in
    /// <c>SqprojImportTests.Sporthaus</c>, ohne Aufbauten).
    /// </summary>
    private string Sporthaus() => Merken(new SqprojProbenErzeuger { Baujahr = "1970-05-01 00:00:00" }
        .Geschoss("F1", "EG", 0.0, 3.0).Geschoss("F2", "OG", 3.0, 3.0)
        .Raum("R1", "Halle", "F1", "{31111111-1111-1111-1111-111111111111}", 50.0, null, 1, 3.0)
        .Raum("R2", "Umkleide", "F1", "{32222222-2222-2222-2222-222222222222}", 20.0, null, 1, 3.0)
        .Raum("R3", "Buero", "F2", "{33333333-3333-3333-3333-333333333333}", 30.0, null, 1, 3.0)
        .Raum("R4", "Lager", "F2", null, 10.0, null, 2, 3.0)
        .Zone("Z1", "Sport", 6, null, "R1", "R2")
        .Zone("Z2", "Verwaltung", 6, null, "R3", "R4")
        .Zone("Z3", "Nutzung Halle", 5, null, "R1")
        .Zone("Z4", "Nutzung Rest", 5, null, "R2", "R3", "R4")
        .Flaeche("W1", 1, 3, 30.0, 34.0, 180.0, 90.0, cad: "C1", u: 0.5).Bezug("E1", "R1", "W1", 1)
        .Flaeche("W2", 1, 3, 25.0, 25.0, 0.0, 90.0, cad: "C2", u: 0.5).Bezug("E3", "R3", "W2", 1)
        .Flaeche("BP1", 11, 5, 50.0, 50.0, null, 0.0, cad: "C3", u: 0.8).Bezug("E4", "R1", "BP1", 8)
        .Flaeche("BP2", 11, 5, 20.0, 20.0, null, 0.0, cad: "C4", u: 0.8).Bezug("E5", "R2", "BP2", 8)
        .Flaeche("DA1", 5, 3, 30.0, 30.0, null, 0.0, cad: "C5", u: 0.4).Bezug("E6", "R3", "DA1", 9)
        .Flaeche("DA2", 5, 3, 10.0, 10.0, null, 0.0, cad: "C6", u: 0.4).Bezug("E7", "R4", "DA2", 9)
        .Schreiben(SqprojProbenErzeuger.TempPfad("dialog_nur")));

    private static IElement Quellwahl(IRenderedComponent<GebaeudeImportDialog> cut)
        => cut.Find(".epos-gebimport-quellwahl select");

    private static void Einlesen(IRenderedComponent<GebaeudeImportDialog> cut)
    {
        IElement Dateiknopf() => cut.FindAll("button").First(k => k.TextContent.Contains(cut.Instance.Texte.DateiKnopf.TrimEnd('…', '.')));
        Dateiknopf().Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-gebimport-zeilen tbody tr")));
        cut.WaitForAssertion(() => Assert.False(Dateiknopf().HasAttribute("disabled")));
    }

    /// <summary>Wählt einen Eintrag der Quellenwahl nach seiner Stelle (1 = IFC … 4 = nur Projektdatei).</summary>
    private static void Waehlen(IRenderedComponent<GebaeudeImportDialog> cut, int id)
        => Quellwahl(cut).Change(id.ToString(System.Globalization.CultureInfo.InvariantCulture));

    [Fact]
    public void Die_Quellenwahl_hat_vier_Eintraege_und_setzt_den_Filter()
    {
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(Sporthaus());
        IReadOnlyList<string> eintraege = Quellwahl(cut).QuerySelectorAll("option").Where(o => !string.IsNullOrEmpty(o.GetAttribute("value")))
                                                        .Select(o => o.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "IFC", "gbXML", "IFC + Projektdatei", "Nur Projektdatei (.sqproj)" }, eintraege);
        Assert.Null(cut.Instance.Quelle);

        Waehlen(cut, 4);
        Assert.Equal("SQPROJ", cut.Instance.Quelle);
        Einlesen(cut);
        Assert.EndsWith("|*.sqproj", _filter.Last());

        Waehlen(cut, 1);
        Einlesen(cut);
        Assert.Contains("*.ifc", _filter.Last());
        Assert.DoesNotContain("sqproj", _filter.Last());
    }

    [Fact]
    public void Nur_Projektdatei_blendet_das_Dazuladen_aus_und_nennt_die_Quelle()
    {
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(Sporthaus());
        Einlesen(cut);   // über „alle Formate“: die Quelle folgt der Endung

        Assert.True(cut.Instance.NurProjektdatei);
        Assert.Equal("SQPROJ", cut.Instance.Quelle);
        Assert.Empty(cut.FindAll("button[data-aktion='projektdatei']"));
        Assert.Empty(cut.FindAll("button[data-aktion='projektdatei-entfernen']"));
        IElement zeile = cut.Find(".epos-gebimport-quellzeile");
        Assert.Equal(nameof(GebaeudeImportWeg.NurProjektdatei), zeile.GetAttribute("data-quelle"));
        Assert.Contains("Quelle: Projektdatei", zeile.TextContent);

        // Die Nordrichtungsabfrage: die Datei nennt keinen Nordwinkel — angenommen, im Warnstil.
        Assert.Equal("ANNAHME", cut.Find(".epos-gebimport-nordrichtung").GetAttribute("data-herkunft"));

        // Die Herkunftsspalte der Feldzeilen.
        Assert.Contains(cut.FindAll(".epos-gebimport-zeilen tbody tr td.epos-gebimport-herkunft"), td => td.TextContent.Trim() == "Projektdatei");
    }

    [Fact]
    public void Nur_Projektdatei_bietet_die_Zonierung_und_meldet_offene_unbeheizte_Raeume_leise()
    {
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(Sporthaus());
        Einlesen(cut);
        Assert.True(cut.Instance.NurProjektdatei);
        // Beide Zonierungen der Datei: der Umschalter DIN/Simulation bleibt, die Vorgabe ist Simulation.
        IElement umschalter = cut.Find("select.epos-gebimport-sq-zonierung");
        Assert.Equal(GebaeudeZonierungSchluessel.SIMULATION, umschalter.GetAttribute("data-zonierung"));
        // Das unbeheizte Lager in der Zone „Verwaltung“: offen, bis die Beheizung entschieden ist — leise, kein Banner.
        Assert.True(cut.Instance.NichtZugeordnet > 0);
        Assert.Contains("unbeheizte Räume", cut.Find(".epos-gebimport-squnbeheizt").TextContent);
    }

    [Fact]
    public void Ifc_plus_Projektdatei_oeffnet_nach_dem_Lesen_die_Projektdatei()
    {
        string ifc = Merken(SqprojProbenErzeuger.HottcadZonenhaus(Wurzel()));
        string sq = Merken(SqprojProbenErzeuger.Zonenhaus().Schreiben(SqprojProbenErzeuger.TempPfad("dialog_dazu")));
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(ifc, sq);
        Waehlen(cut, 3);
        Einlesen(cut);
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-gebimport-sqbilanz")));
        Assert.False(cut.Instance.NurProjektdatei);
        Assert.NotEmpty(cut.FindAll("button[data-aktion='projektdatei']"));
        IElement zeile = cut.Find(".epos-gebimport-quellzeile");
        Assert.Equal(nameof(GebaeudeImportWeg.MitProjektdatei), zeile.GetAttribute("data-quelle"));
        Assert.Contains("Quelle: IFC + Projektdatei", zeile.TextContent);
    }

    [Fact]
    public void Die_Texte_stehen_deutsch_und_englisch()
    {
        var de = new GebaeudeImportTexte();
        Assert.Equal("Quelle", de.Quellwahl);
        Assert.Equal("Quelle: {0}", de.Quellzeile);
        using (new Kulturvorrichtung("en-US"))
        {
            var en = new GebaeudeImportTexte();
            Assert.Equal("Source", en.Quellwahl);
            Assert.Equal("{0} + project file", en.QuelleMitProjektdatei);
            Assert.StartsWith("A zone of the project file", en.SqNurUnbeheizt);
        }
    }

    private static string Wurzel()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d != null && !File.Exists(Path.Combine(d.FullName, "EPOS.Kern", "EPOS.Kern.csproj")))
            d = d.Parent;
        Assert.True(d != null, "Die Repowurzel ist vom Ausgabeordner aus nicht zu finden.");
        return d!.FullName;
    }
}
