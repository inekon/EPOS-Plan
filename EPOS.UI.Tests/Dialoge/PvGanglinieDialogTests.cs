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
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-knopf--richtung").Count);
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
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();
        Assert.Equal(32, gerufen);
        Assert.Equal(2, zeilen.Count);

        cut.FindAll(".epos-zweispalten-knopf--entfernen")[0].Click();
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

    /// <summary>
    /// <b>Die Nennleistung eines Katalogsatzes nachträglich bearbeiten</b>: „Nennleistung bearbeiten…" öffnet eine
    /// Überlagerung mit dem gepflegten Wert, der Prüfhinweis des Imports (Spitze über Nennleistung × 1,1) steht als
    /// Banner, OK schreibt über den Weg der Hülle, die Seite liest neu, der Detailblock zeigt den neuen Wert und der
    /// Hinweis bleibt als Banner stehen; ein Fehlschlag hält die Überlagerung mit dem Grund offen.
    /// </summary>
    [Fact]
    public void Die_Nennleistung_eines_Katalogsatzes_wird_bearbeitet_und_geprueft()
    {
        double? gepflegt = 12.0;
        string? geschriebenFuer = null;
        bool scheitern = true;
        IReadOnlyList<Katalogfilterzeile> Katalogstand() => new[]
        {
            gepflegt.HasValue
                ? Zeitreihenproben.Zeile(31, "PV Dach Ost", zeitintervall: 60, jahresarbeitMwh: 9.5, spitzeKw: 10.0)
                    .MitZahl(Katalogfilterprofil.SpNennleistungKwp, gepflegt.Value, 2)
                : Zeitreihenproben.Zeile(31, "PV Dach Ost", zeitintervall: 60, jahresarbeitMwh: 9.5, spitzeKw: 10.0),
            Zeitreihenproben.Zeile(32, "PV Dach West", geschuetzt: true, zeitintervall: 60, jahresarbeitMwh: 8.8, spitzeKw: 9.0)
        };
        var cut = Aufbauen(wege: new GanglinienKatalogwege
        {
            Katalogzeilen = () => Task.FromResult(Katalogstand()),
            NennleistungPruefenFuer = (_, kwp) => PvGanglinieImportCtrl.Pruefhinweis(10.0, kwp),
            NennleistungSchreiben = (name, kwp) =>
            {
                if (scheitern) return Task.FromResult(new GanglinienNennleistungsschrieb(false, "Schreibfehler", ""));
                geschriebenFuer = name;
                gepflegt = kwp;
                return Task.FromResult(new GanglinienNennleistungsschrieb(true, "gespeichert",
                    PvGanglinieImportCtrl.Pruefhinweis(10.0, kwp)));
            }
        });

        // Ohne Wahl ist der Knopf gesperrt.
        Assert.True(cut.Find("button.epos-nennleistungknopf").HasAttribute("disabled"));

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr")[0].QuerySelector("button")!.Click();
        cut.Find("button.epos-nennleistungknopf").Click();
        Assert.True(cut.Instance.Katalogseite!.NennleistungOffen);
        Assert.True(cut.Instance.Katalogseite.HaeltEsc);
        Assert.Equal("", cut.Instance.Katalogseite.NennleistungBearbeitenHinweis);
        Assert.Contains(Resource.PVG_NENN_PROJEKTKOPIEN, cut.Markup);

        cut.Find(".epos-nennleistung-bearbeiten input[inputmode=decimal]").Input("5");
        string hinweis = cut.Instance.Katalogseite.NennleistungBearbeitenHinweis;
        Assert.NotEqual("", hinweis);
        Assert.Contains(hinweis, cut.Find(".epos-nennleistung-bearbeiten").TextContent);

        // Ein Fehlschlag hält die Überlagerung mit dem Grund offen.
        cut.Find(".epos-nennleistung-bearbeiten").QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == "OK").Click();
        Assert.True(cut.Instance.Katalogseite.NennleistungOffen);
        Assert.Contains("Schreibfehler", cut.Markup);

        scheitern = false;
        cut.Find(".epos-nennleistung-bearbeiten").QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == "OK").Click();
        Assert.Equal("PV Dach Ost", geschriebenFuer);
        Assert.Equal(5.0, gepflegt);
        Assert.False(cut.Instance.Katalogseite.NennleistungOffen);
        Assert.Equal(hinweis, cut.Instance.Katalogseite.Meldung);
        Assert.Equal("gespeichert", cut.Instance.Katalogseite.Status);
        var werte = cut.FindAll("input[readonly]").Select(e => e.GetAttribute("value")).ToList();
        Assert.Contains("5,00 kWp", werte);

        // Abbrechen schreibt nicht.
        cut.Find("button.epos-nennleistungknopf").Click();
        cut.Find(".epos-nennleistung-bearbeiten input[inputmode=decimal]").Input("");
        cut.Find(".epos-nennleistung-bearbeiten").QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == "Abbrechen").Click();
        Assert.False(cut.Instance.Katalogseite.NennleistungOffen);
        Assert.Equal(5.0, gepflegt);
    }

    /// <summary>Ein Auslieferungssatz: der Knopf ist weich gesperrt und nennt den Grund, statt zu öffnen.</summary>
    [Fact]
    public void Am_Auslieferungssatz_ist_die_Nennleistung_weich_gesperrt()
    {
        var cut = Aufbauen(wege: new GanglinienKatalogwege
        {
            Katalogzeilen = () => Task.FromResult<IReadOnlyList<Katalogfilterzeile>>(new[]
            {
                Zeitreihenproben.Zeile(32, "PV Dach West", geschuetzt: true, zeitintervall: 60, jahresarbeitMwh: 8.8, spitzeKw: 9.0)
            }),
            NennleistungSchreiben = (_, _) => throw new InvalidOperationException("darf nicht schreiben")
        });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr")[0].QuerySelector("button")!.Click();
        var knopf = cut.Find("button.epos-nennleistungknopf");
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Equal(Resource.PVG_MSG_NENN_SCHREIBGESCHUETZT, knopf.GetAttribute("title"));
        knopf.Click();
        Assert.False(cut.Instance.Katalogseite!.NennleistungOffen);
        Assert.Equal(Resource.PVG_MSG_NENN_SCHREIBGESCHUETZT, cut.Instance.Katalogseite.Meldung);
    }

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Sie bindet über die Sichtklasse
    /// <c>PvGanglinieKiSicht</c>: Die Katalogwahl ist ein WAHLFELD, ein Setzen zieht den Detailblock nach
    /// (Quelle, Raster, Jahresarbeit, Nennleistung); Zuordnung und Importstand liest der Assistent nur.
    /// </summary>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_liest_die_Ganglinie()
    {
        var katalog = new[]
        {
            Zeitreihenproben.Zeile(31, "PV Dach Ost", zeitintervall: 15, beschreibung: "Messung 2025",
                                   jahresarbeitMwh: 9.5, spitzeKw: 10.0)
                .MitZahl(Katalogfilterprofil.SpNennleistungKwp, 12.0, 2),
            Zeitreihenproben.Zeile(32, "PV Dach West", zeitintervall: 60, beschreibung: "Simulation",
                                   jahresarbeitMwh: 8.8, spitzeKw: 9.0)
        };
        var cut = Aufbauen(wege: new GanglinienKatalogwege
        {
            Katalogzeilen = () => Task.FromResult<IReadOnlyList<Katalogfilterzeile>>(katalog),
            Einlesen = (_, _) => Task.FromResult(new GanglinienKatalogimport(false, false, "", "", ""))
        });

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.PV_GANGLINIE));
        WindowsFormsApplication1.KiFeldzugang Feld(string id)
            => KiMaskenbruecke.Feldzugang(KiMaskennamen.PV_GANGLINIE, id);

        Assert.False(Feld("projektganglinie").Setzbar);
        Assert.False(Feld("nennleistung").Setzbar);
        WindowsFormsApplication1.KiFeldzugang wahl = Feld("katalogganglinie");
        Assert.True(wahl.Setzbar);
        Assert.Equal(Resource.KI_DLG_PVG_IMPORT_KEIN, Feld("importzustand").Lesen());

        KiFeldumsetzung umsetzung = KiFeldwandler.Wandle(wahl, "PV Dach Ost");
        Assert.True(umsetzung.Ok, umsetzung.Grund);
        wahl.Setzen(umsetzung.Wert);
        cut.Render();

        Assert.Equal("PV Dach Ost", cut.Instance.Katalogzeile?.Bezeichner);
        Assert.Equal("Messung 2025", Feld("quelle").Lesen());
        Assert.Equal(Resource.PVG_RASTER_VIERTEL, Feld("aufloesung").Lesen());
        Assert.Equal("9,5 MWh", Feld("jahressumme").Lesen());
        Assert.Equal("12,00 kWp", Feld("nennleistung").Lesen());
        Assert.Equal(true, Feld("im_projekt").Lesen());
        Assert.Equal(false, Feld("katalogbetrieb").Lesen());

        wahl.Setzen(KiFeldwandler.Wandle(wahl, "PV Dach West").Wert);
        cut.Render();
        Assert.Equal(false, Feld("im_projekt").Lesen());
        Assert.Equal(Resource.PVG_NENN_NICHT_GEPFLEGT, Feld("nennleistung").Lesen());

        cut.Find("button.epos-importknopf").Click();
        Assert.Equal(Resource.KI_DLG_PVG_IMPORT_OFFEN, Feld("importzustand").Lesen());
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

    // =================================================================================
    // Projektkopie und Katalog (E113)
    // =================================================================================

    /// <summary>Der Dialog mit den Wegen „Abweichung" und „Erneuern"; zwei Projektzeilen, nur die erste weicht ab.</summary>
    private IRenderedComponent<PvGanglinieDialog> MitErneuerung(Dictionary<string, string> abweichung,
                                                                 Func<string, Task<PvGanglinieErneuerung>> erneuern)
        => Render<PvGanglinieDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "PV Dach Ost", 31), Zeile(2, "PV Dach West", 32) })
            .Add(x => x.Katalogwege, new GanglinienKatalogwege { Katalogzeilen = () => Task.FromResult(Katalog) })
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.PvGanglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.KatalogAbweichung, n => abweichung.TryGetValue(n, out string? t) ? t : "")
            .Add(x => x.AusKatalogErneuern, erneuern));

    private static void ProjektzeileWaehlen(IRenderedComponent<PvGanglinieDialog> cut, int index)
        => cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr button")[index].Click();

    /// <summary>
    /// Die abweichende Projektzeile trägt das Zeichen samt Satz; markiert, zeigt der Detailblock den Satz und den Knopf
    /// „Aus dem Katalog erneuern…" — eine gleiche Zeile trägt beides nicht. Der Assistent liest die Abweichung mit.
    /// </summary>
    [Fact]
    public void Die_Abweichung_steht_an_der_Projektzeile_und_im_Detailblock()
    {
        var abw = new Dictionary<string, string> { ["PV Dach Ost"] = "weicht vom Katalog ab: Nennleistung, Reihe" };
        var cut = MitErneuerung(abw, _ => throw new InvalidOperationException("darf nicht schreiben"));

        IReadOnlyList<IElement> zeichen = cut.FindAll(".epos-pvg-abweichung");
        Assert.Single(zeichen);
        Assert.Equal("weicht vom Katalog ab: Nennleistung, Reihe", zeichen[0].GetAttribute("title"));
        Assert.Empty(cut.FindAll(".epos-pvg-erneuernknopf"));

        ProjektzeileWaehlen(cut, 1);
        Assert.Empty(cut.FindAll(".epos-pvg-abweichungszeile"));
        Assert.Empty(cut.FindAll(".epos-pvg-erneuernknopf"));
        Assert.Equal("", KiMaskenbruecke.Feldzugang(KiMaskennamen.PV_GANGLINIE, "katalogabweichung").Lesen());

        ProjektzeileWaehlen(cut, 0);
        Assert.Contains("weicht vom Katalog ab: Nennleistung, Reihe", cut.Find(".epos-pvg-abweichungszeile").TextContent);
        Assert.Equal(Resource.PVG_BTN_ERNEUERN, cut.Find(".epos-pvg-erneuernknopf").TextContent.Trim());
        Assert.Equal("weicht vom Katalog ab: Nennleistung, Reihe",
                     KiMaskenbruecke.Feldzugang(KiMaskennamen.PV_GANGLINIE, "katalogabweichung").Lesen());
        Assert.False(KiMaskenbruecke.Feldzugang(KiMaskennamen.PV_GANGLINIE, "katalogabweichung").Setzbar);
    }

    /// <summary>
    /// „Aus dem Katalog erneuern…" fragt zurück (Name, Abweichung, Hinweis auf die nächste Simulation); Abbrechen
    /// schreibt nichts, ein Fehlschlag hält die Rückfrage mit dem Grund offen, OK erneuert, das Zeichen fällt und die
    /// Bestätigung steht im Dialog.
    /// </summary>
    [Fact]
    public void Erneuern_fragt_zurueck_bricht_ab_meldet_den_Grund_und_erneuert()
    {
        var abw = new Dictionary<string, string> { ["PV Dach Ost"] = "weicht vom Katalog ab: Reihe" };
        var gerufen = new List<string>();
        bool scheitern = true;
        var cut = MitErneuerung(abw, name =>
        {
            gerufen.Add(name);
            if (scheitern) return Task.FromResult(new PvGanglinieErneuerung(false, "Schreibfehler"));
            abw.Remove(name);
            return Task.FromResult(new PvGanglinieErneuerung(true, "erneuert: " + name));
        });

        ProjektzeileWaehlen(cut, 0);
        cut.Find(".epos-pvg-erneuernknopf").Click();
        Assert.True(cut.Instance.ErneuernOffen);
        string frage = cut.Find(".epos-pvg-erneuern").TextContent;
        Assert.Contains("PV Dach Ost", frage);
        Assert.Contains("weicht vom Katalog ab: Reihe", frage);
        Assert.Contains(Resource.PVG_ERNEUERN_SIMULATION, frage);

        // Abbrechen: nichts geschrieben, die Rückfrage ist zu, die Abweichung bleibt.
        cut.Find(".epos-pvg-erneuern").QuerySelectorAll("button").First(b => b.TextContent.Trim() == "Abbrechen").Click();
        Assert.False(cut.Instance.ErneuernOffen);
        Assert.Empty(gerufen);
        Assert.Single(cut.FindAll(".epos-pvg-abweichung"));

        // Fehlschlag: die Rückfrage bleibt mit dem Grund offen.
        cut.Find(".epos-pvg-erneuernknopf").Click();
        cut.Find(".epos-pvg-erneuern").QuerySelectorAll("button").First(b => b.TextContent.Trim() == "OK").Click();
        Assert.True(cut.Instance.ErneuernOffen);
        Assert.Contains("Schreibfehler", cut.Find(".epos-pvg-erneuern").TextContent);

        // OK: erneuert, das Zeichen fällt, die Bestätigung steht.
        scheitern = false;
        cut.Find(".epos-pvg-erneuern").QuerySelectorAll("button").First(b => b.TextContent.Trim() == "OK").Click();
        Assert.False(cut.Instance.ErneuernOffen);
        Assert.Equal(new[] { "PV Dach Ost", "PV Dach Ost" }, gerufen);
        Assert.Empty(cut.FindAll(".epos-pvg-abweichung"));
        Assert.Empty(cut.FindAll(".epos-pvg-erneuernknopf"));
        Assert.Equal("erneuert: PV Dach Ost", cut.Find(".epos-pvg-erneuernstatus").TextContent);
    }

    /// <summary>Esc schließt bei offener Rückfrage nicht den Dialog (Blätter zuerst).</summary>
    [Fact]
    public void Esc_schliesst_bei_offener_Rueckfrage_nicht_den_Dialog()
    {
        var abw = new Dictionary<string, string> { ["PV Dach Ost"] = "weicht vom Katalog ab: Raster" };
        bool? geschlossen = null;
        var cut = Render<PvGanglinieDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "PV Dach Ost", 31) })
            .Add(x => x.Katalogwege, new GanglinienKatalogwege { Katalogzeilen = () => Task.FromResult(Katalog) })
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.PvGanglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.KatalogAbweichung, n => abw.TryGetValue(n, out string? t) ? t : "")
            .Add(x => x.AusKatalogErneuern, _ => Task.FromResult(new PvGanglinieErneuerung(true, "")))
            .Add(x => x.Geschlossen, b => geschlossen = b));

        ProjektzeileWaehlen(cut, 0);
        cut.Find(".epos-pvg-erneuernknopf").Click();
        cut.Find(".epos-dialog").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });
        Assert.Null(geschlossen);
    }
}
