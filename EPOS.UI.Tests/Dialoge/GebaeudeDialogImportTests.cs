using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Stufe G4, Welle 4 — der Gebäudeimport im Gebäudedialog (Architekturentscheid A17): EIN Knopf
/// „Importieren (gbXML, IFC)…" neben „Gebäude in DB neu…", der Zuordnungsdialog als Überlagerung
/// ohne eigenen Kopf, danach der Katalogeditor im Modus Neu, vorbelegt; erst nach dem Speichern im
/// Editor steht die neue Zeile samt Herkunftsschlüssel in der Projektliste.
///
/// <para>Die Kultur ist auf de-DE gepinnt — die Erwartungswerte sind deutsche Beschriftungen.</para>
/// </summary>
public class GebaeudeDialogImportTests : EposBunitContext
{
    private static readonly string[] KLASSEN = Enumerable.Range(0, 21).Select(i => "Klasse " + (char)('A' + i)).ToArray();

    public GebaeudeDialogImportTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Was die Delegaten des Importwegs gesehen haben.</summary>
    private sealed class Protokoll
    {
        public List<GebaeudeImportErgebnis> Uebernommen { get; } = new();
        public int EditorGaben { get; set; }
        public int Aufgenommen { get; set; }
        public List<(string Name, bool Neu)> Gespeichert { get; } = new();
        public int Geaendert { get; set; }
        public List<bool> Geschlossen { get; } = new();
    }

    /// <summary>Ein vollständig belegter Satz, der die Prüfung des Editors besteht.</summary>
    private static GebaeudeKatalogDaten Satz(string name) => new()
    {
        Name = name, Typ = "Einfamilienhaus", Beschreibung = "", Gebaeudeart = "Einfamilienhaus",
        Verwendung = "Wohngebaeude", Baualtersklasse = 4, Bauart = 1,
        WohnflaecheGesamt = 120, FlaecheNutzer = 35, Waermegewinne = 400, Fensterdurchlassgrad = 0.6, Raumhoehe = 2.5,
        FensterflaecheNord = 4, FensterflaecheSued = 14, FensterflaecheOstWest = 5,
        FlaecheAussenwand = 145, Dachflaeche = 60, Grundflaeche = 60, SonstigeFlaechen = 2,
        UWertAussenwand = 0.4, UWertFenster = 1.1, UWertDachflaeche = 0.25, UWertGrundflaeche = 0.4, UWertSonstiges = 1.8,
        WbvkFensterWand = 0.09, SollTag = 20, NachtAbsenkung = 17, MaxTemperatur = 24,
        WochenendAbsenkung = 0, SollFerien = 0, Luftwechselrate = 0.5
    };

    /// <summary>Der Weg eines Imports, wie ihn die Hülle baut — mit Probedaten.</summary>
    private static GebaeudeImportweg Weg(Protokoll p)
    {
        var gaben = new Dictionary<string, object>
        {
            ["Profil"] = new GebaeudeImportProfilDaten("Format Alpha, Format Beta", "(*.alpha;*.beta)|*.alpha;*.beta",
                                                        "Alpha 25 MB · Beta 50 MB", new[] { "Regel eins" }, "Form_Alpha.btn_Help"),
            ["Baualtersklassen"] = (IReadOnlyList<string>)KLASSEN,
            ["DateiWaehlen"] = new Func<string, Task<GebaeudeDateiwahl?>>(
                _ => Task.FromResult<GebaeudeDateiwahl?>(new GebaeudeDateiwahl("C:/ablage/haus.alpha", "haus.alpha", 20555))),
            ["Lesen"] = new Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>(
                (_, _, _) => Task.FromResult(new GebaeudeLesestand(true,
                    new GebaeudeImportKopf("haus.alpha", "Format Alpha", "Schema 1", "20,1 KB", "Regel eins"),
                    new[] { "Haus 1" }, Array.Empty<GebaeudeImportMeldung>(),
                    "Diese Datei ist im Projekt schon importiert: Gebäude „Altbau“, 24.09.2026 09:00."))),
            ["Zuordnen"] = new Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>(_ => new GebaeudeImportStand
            {
                Kopftext = "Kopf-Probe",
                Vorschlagsname = "Importhaus",
                ManuellHerkunftText = "Manuell",
                Zeilen = new[]
                {
                    new GebaeudeFeldzeileDaten
                    {
                        Zielfeld = "NUTZ", Gruppe = "Kenngrößen", Feld = "Nutzfläche", Wert = 120, Einheit = "m²",
                        HerkunftText = "Datei", HerkunftSchluessel = "DATEI", Haken = true, Eingebbar = true, HakenSetzbar = true,
                    },
                },
            }),
            ["Pruefen"] = new Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>(_ => Array.Empty<GebaeudeImportMeldung>()),
            ["Uebernehmen"] = new Func<GebaeudeImportErgebnis, Task<string?>>(e =>
            {
                p.Uebernommen.Add(e);
                return Task.FromResult<string?>(null);
            }),
        };

        return new GebaeudeImportweg(
            gaben,
            () =>
            {
                p.EditorGaben++;
                return new Dictionary<string, object>
                {
                    ["Daten"] = Satz(p.Uebernommen.Last().Gebaeudename),
                    ["Modus"] = GebaeudeKatalogModus.Neu,
                    ["Gebaeudetypen"] = new Func<IReadOnlyList<string>>(() => new[] { "Einfamilienhaus" }),
                    ["Gebaeudearten"] = new Func<IReadOnlyList<string>>(() => new[] { "Einfamilienhaus" }),
                    ["Baualtersklassen"] = (IReadOnlyList<string>)KLASSEN,
                    ["Speichern"] = new Func<GebaeudeKatalogDaten, bool, string, GebaeudeKatalogErgebnis>((d, neu, _) =>
                    {
                        p.Gespeichert.Add((d.Name, neu));
                        return new GebaeudeKatalogErgebnis(true, "");
                    }),
                    ["Vorbelegung"] = "Vorbelegt aus dem Import: Datei haus.alpha, Format Alpha.",
                    ["EntprellungMs"] = 0,
                };
            },
            () =>
            {
                p.Aufgenommen++;
                return new GebaeudeProjektZeile
                {
                    IdZ = 100001, IdGebaeude = 77, IdKatalog = 77, Name = p.Gespeichert.Last().Name,
                    Einheit = "Wohnfläche [m²]", Wohnflaeche = 120, Jahresnutzungsgrad = 1,
                    Herkunftsschluessel = "import-probe",
                };
            });
    }

    private IRenderedComponent<GebaeudeDialog> Aufbauen(Protokoll p, List<GebaeudeProjektZeile> zeilen, bool mitImport = true)
        => Render<GebaeudeDialog>(c => c
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.Katalogzeilen, () => new[]
            {
                new WindowsFormsApplication1.Katalogfilterzeile(1, "Haus 1990")
                    .MitText(WindowsFormsApplication1.Katalogfilterprofil.SpBezeichner, "Haus 1990")
                    .MitText(WindowsFormsApplication1.Katalogfilterprofil.SpGebaeudeart, "Einfamilienhaus")
                    .MitZahl(WindowsFormsApplication1.Katalogfilterprofil.SpFlaecheM2, 150, 0)
            })
            .Add(x => x.Filterstandvorgabe, new WindowsFormsApplication1.Katalogfilterstand())
            .Add(x => x.StammDetail, n => new GebaeudeStammDetail(n, "Einfamilienhaus", "", "150,00"))
            .Add(x => x.KatalogGaben, _ => new Dictionary<string, object>())
            .Add(x => x.ImportGaben, mitImport ? () => Weg(p) : null)
            .Add(x => x.Geaendert, () => p.Geaendert++)
            .Add(x => x.Geschlossen, b => p.Geschlossen.Add(b)));

    private static List<GebaeudeProjektZeile> EineZeile() => new()
    {
        new GebaeudeProjektZeile { IdZ = 1, IdGebaeude = 7, IdKatalog = 42, Name = "Haus 1990", Einheit = "Wohnfläche [m²]", Wohnflaeche = 150 }
    };

    private static IElement Importknopf(IRenderedComponent<GebaeudeDialog> cut) => cut.Find("button.epos-importknopf");

    /// <summary>Knopf → Datei wählen → gelesen und zugeordnet (der Zuordnungsdialog steht).</summary>
    private static IRenderedComponent<GebaeudeImportDialog> ImportEinlesen(IRenderedComponent<GebaeudeDialog> cut)
    {
        Importknopf(cut).Click();
        IRenderedComponent<GebaeudeImportDialog> imp = cut.FindComponent<GebaeudeImportDialog>();
        imp.FindAll("button").First(k => k.TextContent.Contains("Datei wählen")).Click();
        imp.WaitForAssertion(() => Assert.NotEmpty(imp.FindAll(".epos-gebimport-zeilen tbody tr")));
        return imp;
    }

    // =================================================================================
    // Knopf und Überlagerung
    // =================================================================================

    [Fact]
    public void Der_Knopf_steht_nur_mit_Delegat_neben_Gebaeude_in_DB_neu()
    {
        var p = new Protokoll();
        Assert.Empty(Aufbauen(p, EineZeile(), mitImport: false).FindAll("button.epos-importknopf"));

        var cut = Aufbauen(p, EineZeile());
        IElement knopf = Importknopf(cut);
        Assert.Equal("Importieren (gbXML, IFC)…", knopf.TextContent.Trim());
        Assert.Contains("gbXML- oder IFC-Datei", knopf.GetAttribute("title"));

        // In der Leiste unter dem Katalog, direkt nach „Gebäude in DB neu...".
        var leiste = knopf.ParentElement!.QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(leiste.IndexOf("Gebäude in DB neu...") + 1, leiste.IndexOf("Importieren (gbXML, IFC)…"));
        Assert.False(cut.Instance.ImportOffen);
    }

    [Fact]
    public void Die_Ueberlagerung_oeffnet_ohne_eigenen_Kopf_und_Abbrechen_schreibt_nichts()
    {
        var p = new Protokoll();
        var zeilen = EineZeile();
        var cut = Aufbauen(p, zeilen);

        Importknopf(cut).Click();
        Assert.True(cut.Instance.ImportOffen);
        Assert.True(cut.Instance.ImportLaeuft);
        // Titel und Kreuz führt der Importdialog selbst, die Überlagerung trägt keins von beidem.
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-titel"));
        Assert.Equal("Gebäude importieren", cut.Find(".epos-ueberlagerung-inhalt h1.epos-dialog-titel").TextContent);

        // Esc im Gebäudedialog schließt ihn nicht, solange der Import steht.
        cut.Find("div.epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(p.Geschlossen);

        IRenderedComponent<GebaeudeImportDialog> imp = cut.FindComponent<GebaeudeImportDialog>();
        imp.FindAll(".epos-leiste button").First(k => k.TextContent == "Abbrechen").Click();

        Assert.False(cut.Instance.ImportOffen);
        Assert.False(cut.Instance.ImportLaeuft);
        Assert.False(cut.Instance.KatalogeditorOffen);
        Assert.Empty(p.Uebernommen);
        Assert.Equal(0, p.EditorGaben);
        Assert.Equal(0, p.Aufgenommen);
        Assert.Single(zeilen);
        Assert.Equal(0, p.Geaendert);
    }

    [Fact]
    public void Nach_der_Uebernahme_oeffnet_der_Editor_im_Modus_Neu_vorbelegt()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p, EineZeile());
        IRenderedComponent<GebaeudeImportDialog> imp = ImportEinlesen(cut);
        Assert.Contains("schon importiert: Gebäude „Altbau“", imp.Find(".epos-gebimport-schonimportiert").TextContent);

        imp.Find(".epos-leiste button.epos-knopf--primaer").Click();

        Assert.Equal("Importhaus", Assert.Single(p.Uebernommen).Gebaeudename);
        Assert.False(cut.Instance.ImportOffen);
        Assert.True(cut.Instance.KatalogeditorOffen);
        Assert.True(cut.Instance.ImportLaeuft);
        Assert.Equal(1, p.EditorGaben);

        IRenderedComponent<GebaeudeKatalogDialog> editor = cut.FindComponent<GebaeudeKatalogDialog>();
        Assert.Equal(GebaeudeKatalogModus.Neu, editor.Instance.Modus);
        Assert.Equal("Importhaus", editor.Instance.Arbeitsstand.Name);
        Assert.Equal(120.0, editor.Instance.Arbeitsstand.WohnflaecheGesamt);
        Assert.Contains("Vorbelegt aus dem Import: Datei haus.alpha, Format Alpha.",
                        editor.FindAll(".epos-herleitung-text").Select(e => e.TextContent));
        Assert.Equal("Gebäude in DB neu...", cut.Find(".epos-ueberlagerung-titel").TextContent);
        Assert.Equal(0, p.Aufgenommen);                            // noch keine Zeile
    }

    [Fact]
    public void Nach_dem_Speichern_im_Editor_steht_die_neue_Zeile_mit_Herkunftsschluessel()
    {
        var p = new Protokoll();
        var zeilen = EineZeile();
        var cut = Aufbauen(p, zeilen);
        ImportEinlesen(cut).Find(".epos-leiste button.epos-knopf--primaer").Click();

        IRenderedComponent<GebaeudeKatalogDialog> editor = cut.FindComponent<GebaeudeKatalogDialog>();
        editor.Find(".epos-leiste button.epos-knopf--primaer").Click();

        Assert.Equal(("Importhaus", true), Assert.Single(p.Gespeichert));   // angelegt, nicht überschrieben
        Assert.False(cut.Instance.KatalogeditorOffen);
        Assert.False(cut.Instance.ImportLaeuft);
        Assert.Equal(1, p.Aufgenommen);
        Assert.Equal(2, zeilen.Count);
        GebaeudeProjektZeile neu = zeilen[1];
        Assert.Equal("Importhaus", neu.Name);
        Assert.Equal("import-probe", neu.Herkunftsschluessel);
        Assert.Same(neu, cut.Instance.Gewaehlt);                    // die neue Zeile ist markiert
        Assert.Equal(1, p.Geaendert);
        Assert.Contains("„Importhaus“ steht jetzt im Katalog und in der Projektliste", cut.Instance.Meldung);
        Assert.Empty(p.Geschlossen);                                 // geschrieben wird mit dem OK des Gebäudedialogs
    }

    [Fact]
    public void Ohne_Speichern_im_Editor_entsteht_keine_Zeile()
    {
        var p = new Protokoll();
        var zeilen = EineZeile();
        var cut = Aufbauen(p, zeilen);
        ImportEinlesen(cut).Find(".epos-leiste button.epos-knopf--primaer").Click();

        IRenderedComponent<GebaeudeKatalogDialog> editor = cut.FindComponent<GebaeudeKatalogDialog>();
        editor.FindAll(".epos-leiste button").First(k => k.TextContent.Trim() == "Abbrechen").Click();

        Assert.False(cut.Instance.KatalogeditorOffen);
        Assert.False(cut.Instance.ImportLaeuft);
        Assert.Empty(p.Gespeichert);
        Assert.Equal(0, p.Aufgenommen);
        Assert.Single(zeilen);
        Assert.Equal(0, p.Geaendert);

        // Ein gewöhnlicher Editor („Gebäude in DB neu…") nimmt auch nach dem Speichern keine Zeile auf.
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Gebäude in DB neu...").Click();
        Assert.True(cut.Instance.KatalogeditorOffen);
        Assert.False(cut.Instance.ImportLaeuft);
    }
}
