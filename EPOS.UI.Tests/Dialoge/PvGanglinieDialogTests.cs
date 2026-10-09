using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Seiten;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Dialog <b>„Photovoltaik Ganglinie"</b> (PVG, Schemaschritt 206): Feldbestand, Übernahme und Entfernen
/// an der geteilten Liste, OK/Abbrechen, die Katalogseite mit Kennzahlen, Vergleichen, Schloss, Löschen und
/// Import, der Katalogbetrieb über den Menüpunkt und die benannte Absage des Imports ohne Dateiwege.
/// </summary>
public class PvGanglinieDialogTests : EposBunitContext
{
    private static IReadOnlyList<Katalogfilterzeile> Katalog => new[]
    {
        Zeitreihenproben.Zeile(31, "PV Dach Ost", beschreibung: "Messung 2025", jahresarbeitMwh: 9.5, spitzeKw: 10.0),
        Zeitreihenproben.Zeile(32, "PV Dach West", beschreibung: "Messung 2025", jahresarbeitMwh: 8.8, spitzeKw: 9.0)
    };

    public PvGanglinieDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static ErzeugerZeile Zeile(int schluessel, string name, int id)
        => new() { Schluessel = schluessel, Bezeichner = name, GeraetId = id };

    private IRenderedComponent<PvGanglinieDialog> Aufbauen(
        List<ErzeugerZeile>? zeilen = null,
        Func<int, ErzeugerZeile?>? aufnehmen = null,
        Action<ErzeugerZeile>? entfernen = null,
        GanglinienKatalogwege? wege = null,
        Action<bool>? geschlossen = null,
        bool katalogbetrieb = false)
        => Render<PvGanglinieDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<ErzeugerZeile> { Zeile(1, "PV Dach Ost", 31) })
            .Add(x => x.Katalogbetrieb, katalogbetrieb)
            .Add(x => x.Katalogwege, wege ?? new GanglinienKatalogwege { Katalogzeilen = () => Task.FromResult(Katalog) })
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.PvGanglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Aufnehmen, aufnehmen ?? (id => Zeile(100000, "PV Dach West", id)))
            .Add(x => x.Entfernen, entfernen)
            .Add(x => x.HinweisText, "Hinweis zur Weiche")
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IElement Knopf(IRenderedComponent<PvGanglinieDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    [Fact]
    public void Der_Feldbestand_steht()
    {
        var cut = Aufbauen();

        Assert.Equal(2, cut.FindAll(".epos-raster").Count);
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-uebernahme button").Count);
        var ueberschriften = cut.FindAll(".epos-untergruppe").Select(e => e.TextContent).ToList();
        Assert.Contains("Ausgewählt im Projekt", ueberschriften);
        Assert.Contains("PV-Ganglinie aus DB", ueberschriften);
        Assert.Contains("Hinweis zur Weiche", cut.Markup);
        Assert.Equal("Photovoltaik Ganglinie", cut.Find(".epos-dialog-titel").TextContent);

        var knoepfe = cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Contains("OK", knoepfe);
        Assert.Contains("Abbrechen", knoepfe);
    }

    [Fact]
    public void Uebernehmen_und_Entfernen_arbeiten_an_der_geteilten_Liste()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "PV Dach Ost", 31) };
        int gerufen = 0;
        var entfernt = new List<ErzeugerZeile>();
        var cut = Aufbauen(zeilen, aufnehmen: id => { gerufen = id; return Zeile(100000, "PV Dach West", id); },
                           entfernen: z => entfernt.Add(z));

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr button")[1].Click();
        cut.FindAll(".epos-zweispalten-uebernahme button")[0].Click();
        Assert.Equal(32, gerufen);
        Assert.Equal(2, zeilen.Count);

        cut.FindAll(".epos-zweispalten-uebernahme button")[1].Click();
        Assert.Single(zeilen);
        Assert.Equal("PV Dach West", entfernt.Single().Bezeichner);
    }

    [Fact]
    public void OK_und_Abbrechen_melden_das_Ergebnis()
    {
        bool? ergebnis = null;
        Knopf(Aufbauen(geschlossen: b => ergebnis = b), "OK").Click();
        Assert.True(ergebnis);

        ergebnis = null;
        Knopf(Aufbauen(geschlossen: b => ergebnis = b), "Abbrechen").Click();
        Assert.False(ergebnis);
    }

    [Fact]
    public void Die_Katalogseite_traegt_Raster_Kennzahlen_Vergleichen_Loeschen_und_Import()
    {
        var cut = Aufbauen(wege: new GanglinienKatalogwege
        {
            Katalogzeilen = () => Task.FromResult(Katalog),
            Loeschen = _ => Task.FromResult(true),
            Einlesen = (_, _) => Task.FromResult(new GanglinienKatalogimport(false, false, "", "", ""))
        });

        var koepfe = cut.FindAll(".epos-raster")[1].QuerySelectorAll("thead th").Select(t => t.TextContent).ToList();
        Assert.Contains(koepfe, k => k.Contains("ZEITINTERVALL"));
        Assert.Contains(koepfe, k => k.Contains("BESCHREIBUNG"));
        Assert.Contains(koepfe, k => k.Contains("JAHRESARBEIT"));
        Assert.Contains(koepfe, k => k.Contains("SPITZE"));

        var knoepfe = cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Contains(knoepfe, k => k.StartsWith("Vergleichen"));
        Assert.Contains("Import…", knoepfe);
    }

    /// <summary>
    /// Der Detailblock zeigt zur markierten Ganglinie Raster, Jahresarbeit und die gepflegte Nennleistung; eine
    /// nicht gepflegte sagt, dass der Lauf die Spitze der Reihe nimmt.
    /// </summary>
    [Fact]
    public void Der_Detailblock_zeigt_Raster_Jahresarbeit_und_Nennleistung()
    {
        var katalog = new[]
        {
            Zeitreihenproben.Zeile(31, "PV Dach Ost", zeitintervall: 15, beschreibung: "Messung 2025",
                                   jahresarbeitMwh: 9.5, spitzeKw: 10.0)
                .MitZahl(Katalogfilterprofil.SpNennleistungKwp, 12.0, 2),
            Zeitreihenproben.Zeile(32, "PV Dach West", zeitintervall: 60, jahresarbeitMwh: 8.8, spitzeKw: 9.0)
        };
        var cut = Aufbauen(wege: new GanglinienKatalogwege
        {
            Katalogzeilen = () => Task.FromResult<IReadOnlyList<Katalogfilterzeile>>(katalog)
        });

        cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr")[0].QuerySelector("button")!.Click();
        var werte = cut.FindAll("input[readonly]").Select(e => e.GetAttribute("value")).ToList();
        Assert.Contains(Resource.PVG_RASTER_VIERTEL, werte);
        Assert.Contains("9,5 MWh", werte);
        Assert.Contains("12,00 kWp", werte);

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr")[1].QuerySelector("button")!.Click();
        werte = cut.FindAll("input[readonly]").Select(e => e.GetAttribute("value")).ToList();
        Assert.Contains(Resource.PVG_RASTER_STUNDE, werte);
        Assert.Contains(Resource.PVG_NENN_NICHT_GEPFLEGT, werte);
    }

    /// <summary>
    /// Der Import fragt die Nennleistung ab: vorbelegt aus dem Vorschlag der Hülle, geprüft über den Weg des Kerns
    /// (Hinweis über Nennleistung × 1,1), und das Einlesen bekommt den eingetragenen Wert.
    /// </summary>
    [Fact]
    public void Der_Import_fragt_die_Nennleistung_ab_prueft_sie_und_reicht_sie_weiter()
    {
        double? gelesen = -1;
        var cut = Aufbauen(wege: new GanglinienKatalogwege
        {
            Katalogzeilen = () => Task.FromResult(Katalog),
            DateiWaehlen = _ => Task.FromResult<string?>("D:/Daten/PV Sued.csv"),
            Einlesen = (_, _) => Task.FromResult(new GanglinienKatalogimport(false, true, "", "falscher Weg", "")),
            NennleistungVorschlagen = _ => Task.FromResult(new GanglinienNennleistungsvorschlag(8.0, false, 8.0)),
            NennleistungPruefen = PvGanglinieImportCtrl.Pruefhinweis,
            EinlesenMitNennleistung = (_, nenn, _) =>
            {
                gelesen = nenn;
                return Task.FromResult(new GanglinienKatalogimport(true, false, "PV Dach Ost", "", "Protokoll"));
            }
        });

        cut.Find("button.epos-importknopf").Click();
        Knopf(cut, "Datei Auswählen...").Click();
        Assert.Equal(8.0, cut.Instance.Katalogseite!.Nennleistung);
        Assert.Equal("", cut.Instance.Katalogseite.Nennleistungshinweis);
        Assert.Contains("Spitze der Reihe", cut.Markup);

        cut.Find(".epos-einlesen input[inputmode=decimal]").Input("5");
        Assert.NotEqual("", cut.Instance.Katalogseite.Nennleistungshinweis);
        Assert.Contains(cut.Instance.Katalogseite.Nennleistungshinweis, cut.Markup);

        Knopf(cut, "Datei Einlesen...").Click();
        Assert.Equal(5.0, gelesen);
        Assert.False(cut.Instance.Katalogseite.ImportOffen);
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog()
    {
        var cut = Render<PvGanglinieDialog>();
        Assert.NotNull(cut.Find(".epos-dialog"));
    }

    /// <summary>
    /// Der Menüpunkt „PV-Ganglinie" öffnet DENSELBEN Dialog ohne Projekt; der Parametersatz der Datenseite trifft
    /// nur Parameter des Dialogs, und ohne Dateiwege der Schale lehnt der Import benannt ab (iOS).
    /// </summary>
    [Fact]
    public void Der_Menuepunkt_oeffnet_den_Dialog_im_Katalogbetrieb()
    {
        Menuepunkt punkt = Menuetabelle.Alle.Single(x => x.Name == "MenuItem_PvGanglinie");
        Assert.Equal(Seitenschluessel.PvGanglinieAdmin, punkt.Ziel);

        var vorher = Katalogwege.PvGanglinienDatei;
        Katalogwege.PvGanglinienDatei = null;
        try
        {
            var parameter = typeof(PvGanglinieDialog).GetProperties()
                .Where(pi => pi.GetCustomAttributes(typeof(Microsoft.AspNetCore.Components.ParameterAttribute), true).Length > 0)
                .Select(pi => pi.Name).ToHashSet();

            IReadOnlyDictionary<string, object> gaben = PvGanglinieKatalogGaben.KatalogGaben();
            Assert.All(gaben.Keys, k => Assert.Contains(k, parameter));
            Assert.Equal(true, gaben["Katalogbetrieb"]);
            var wege = (GanglinienKatalogwege)gaben["Katalogwege"];
            Assert.Null(wege.Einlesen);
            Assert.Null(wege.NennleistungVorschlagen);
            Assert.Null(wege.EinlesenMitNennleistung);
            Assert.NotNull(wege.NennleistungPruefen);
            Assert.Equal(Resource.PVG_IMP_NICHT_VERFUEGBAR, wege.ImportAbgelehnt);

            IReadOnlyDictionary<string, object> projekt = PvGanglinieKatalogGaben.ProjektGaben(0, new List<PvGanglinieZuordnung>());
            Assert.All(projekt.Keys, k => Assert.Contains(k, parameter));
        }
        finally
        {
            Katalogwege.PvGanglinienDatei = vorher;
        }
    }
}
