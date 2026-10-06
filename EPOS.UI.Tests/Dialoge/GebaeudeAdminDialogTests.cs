using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Gebäudeverwaltung im Gerüst der Verwaltungen</b> (Konzept Administrationsdialoge,
/// Stufe 5, V16; Bestand A9; Welle #465) — Katalogliste mit Profil, Auswahlleiste mit
/// Vergleichen, Duplizieren…, Schloss und Löschen (Verwendungssperre), Fußleiste Speichern ·
/// Verwerfen · Status · Neu… · Beenden, und ein Stammblatt, das JEDES Feld des Katalogeditors
/// führt: Kenndaten, Konditionierung (fünf Zustandszeilen und das breite Blatt, Stufe KP2, Welle U4),
/// Hülle, Fenster, Kenngrößen und „Alle Daten" — auf demselben Arbeitsstand, mit derselben Prüfung und
/// demselben Schreibweg wie der Editor. Dazu die eigene Maske beim Hilfe-Assistenten
/// (<c>Form_Gebaeude_Admin</c>).
///
/// <para>Die Kultur ist auf de-DE gepinnt — die Erwartungswerte sind deutsche
/// Beschriftungen.</para>
/// </summary>
public class GebaeudeAdminDialogTests : EposBunitContext
{
    /// <summary>Die ersten fünf der 13 Baualtersklassen (E47).</summary>
    private static readonly string[] KLASSEN =
    { "bis 1859", "1860 bis 1918", "1919 bis 1948", "1949 bis 1957", "1958 bis 1968" };

    private sealed record Haus(string Name, string Art, string Verwendung, int Klasse, double Flaeche,
                               bool Geschuetzt = false);

    private static readonly Haus[] KATALOG =
    {
        new("Haus A", "Einfamilienhaus", "Wohngebaeude", 1, 140),
        new("Haus B", "Mehrfamilienhaus", "Wohngebaeude", 3, 900),
        new("Hotel C", "Hotel", "Nicht Wohngebaeude", 4, 2400),
        new("Schule D", "Schule", "Nicht Wohngebaeude", 2, 3100, Geschuetzt: true)
    };

    private static string Verwendungstext(string steuerwert)
        => steuerwert == "Wohngebaeude" ? "Wohngebäude" : "Gewerbe+Sonstige";

    private static IReadOnlyList<Katalogfilterzeile> Zeilen(IEnumerable<Haus> haeuser)
        => haeuser.Select((h, i) => new Katalogfilterzeile(i + 1, h.Name) { Geschuetzt = h.Geschuetzt }
                .MitText(Katalogfilterprofil.SpBezeichner, h.Name)
                .MitText(Katalogfilterprofil.SpGebaeudeart, h.Art)
                .MitText(Katalogfilterprofil.SpVerwendung, Verwendungstext(h.Verwendung))
                .MitText(Katalogfilterprofil.SpBaualtersklasse, KLASSEN[h.Klasse])
                .MitZahl(Katalogfilterprofil.SpFlaecheM2, h.Flaeche, 0))
            .ToList();

    /// <summary>
    /// Ein GÜLTIGER Feldsatz — er besteht die Prüfung des Katalogeditors: Pflichtzahlen gesetzt,
    /// U-Werte im Bereich, Bauweise 50 je m² (schwer), Ferien ohne Angabe.
    /// </summary>
    private static GebaeudeKatalogDaten Feldsatz(Haus h) => new()
    {
        Name = h.Name,
        Typ = "Wohnblock",
        Gebaeudeart = h.Art,
        Verwendung = h.Verwendung,
        Baualtersklasse = h.Klasse,
        Beschreibung = "Beschreibung " + h.Name,
        Bauart = 1,
        Bauweise = h.Flaeche * 50,
        WohnflaecheGesamt = h.Flaeche,
        FlaecheNutzer = 35,
        Waermegewinne = 5,
        Fensterdurchlassgrad = 0.6,
        Raumhoehe = 2.5,
        Luftwechselrate = 0.6,
        FensterflaecheNord = 10,
        FensterflaecheSued = 20,
        FensterflaecheOstWest = 10,
        FlaecheAussenwand = 120,
        Dachflaeche = 80,
        Grundflaeche = 80,
        SonstigeFlaechen = 0,
        UWertAussenwand = 0.35,
        UWertFenster = 1.1,
        UWertDachflaeche = 0.25,
        UWertGrundflaeche = 0.4,
        UWertSonstiges = 0,
        SollTag = 20,
        NachtAbsenkung = 16,
        MaxTemperatur = 26,
        WochenendAbsenkung = 0,
        SollFerien = 0,
        WbvkFensterWand = 0.05,
        WbvkAussenwandKeller = 0,
        WbvkWandDach = 0,
        AnschlussFensterWand = 40,
        AnschlussWandDach = 0,
        AnschlussAussenwandKeller = 0,
        WwBedarf = 700
    };

    private static GebaeudeStammblattDaten Satz(Haus h, int id) => new()
    {
        Id = id,
        Name = h.Name,
        Typ = "Wohnblock",
        Gebaeudeart = h.Art,
        Verwendung = h.Verwendung,
        Baualtersklasse = h.Klasse,
        Beschreibung = "Beschreibung " + h.Name,
        Wohnflaeche = h.Flaeche,
        HgesWK = 512.3,
        Rechenweg = "VDI 6007",
        Auslieferung = h.Geschuetzt,
        Huelle = new[]
        {
            new Stammblattwert("Außenwand", "120,0 m² · U 0,35 W/(m²K)"),
            new Stammblattwert("Fenster", "30,0 m² · U 1,10 W/(m²K)")
        },
        AlleDaten = new[]
        {
            Stammblattwert.Abschnitt("Kenngrößen"),
            new Stammblattwert("Raumhöhe", "2,50", "m")
        },
        Feldsatz = Feldsatz(h)
    };

    public GebaeudeAdminDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Was die Wege des Dialogs gerufen haben — die Tests prüfen es.</summary>
    private sealed class Protokoll
    {
        internal readonly List<(GebaeudeKatalogDaten Daten, bool Neu, string Name)> Gespeichert = new();
        internal readonly List<string> Geloescht = new();
        internal readonly List<(int Id, string Name)> Dupliziert = new();
        internal int Editor;
        internal int Gebaeudetypen;
        internal int Typlisten;
        internal bool? Geschlossen;
        internal List<Haus> Katalog = KATALOG.ToList();
        /// <summary>Passt den Feldsatz jedes gelesenen Satzes an (Stufe KP2, Welle U4: Nachtzeit, Konditionierung).</summary>
        internal Action<GebaeudeKatalogDaten>? Anpassen;
    }

    private IRenderedComponent<GebaeudeAdminDialog> Aufbauen(
        Protokoll? p = null,
        Katalogfilterstand? filterstand = null,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? kopien = null,
        bool mitEditor = true,
        bool mitTypen = true,
        EPOS.UI.Bausteine.Schlossweg? schloss = null,
        KonditionierungWeg? konditionierung = null)
    {
        Protokoll pr = p ?? new Protokoll();
        return Render<GebaeudeAdminDialog>(b => b
            .Add(x => x.Katalogzeilen, () => Zeilen(pr.Katalog))
            .Add(x => x.Schloss, schloss)
            .Add(x => x.Konditionierung, konditionierung)
            // Die Vorschau der Kalenderkarten sofort: Eine entprellte Vorschau rechnet nach 400 ms auf dem
            // Dispatcher - unter Last mitten im Fall, und ein Klick des Falls läuft dann erst hinterher.
            .Add(x => x.EntprellungMs, 0)
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerGebaeude(s => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Filterstandvorgabe, filterstand ?? new Katalogfilterstand())
            .Add(x => x.Satz, name =>
            {
                int i = pr.Katalog.FindIndex(h => h.Name == name);
                if (i < 0) return null;
                GebaeudeStammblattDaten s = Satz(pr.Katalog[i], i + 1);
                pr.Anpassen?.Invoke(s.Feldsatz!);
                return s;
            })
            .Add(x => x.Gebaeudetypen, () =>
            {
                pr.Typlisten++;
                return new[] { "Wohnblock", "Schule", "Hotel" };
            })
            .Add(x => x.Gebaeudearten, () => new[] { "Einfamilienhaus", "Mehrfamilienhaus", "Hotel", "Schule" })
            .Add(x => x.Baualtersklassen, KLASSEN)
            .Add(x => x.Verwendungen, new[] { "Wohngebäude", "Gewerbe+Sonstige" })
            // Die Loeschregel des Kerns, nachgebildet: gesperrt allein der Auslieferungssatz; der Hinweis
            // nennt die Projekte, deren Kopie bleibt (kopien: je Satz die Projekte).
            .Add(x => x.Loeschsperre, n => pr.Katalog.Find(h => h.Name == n)?.Geschuetzt == true ? "Auslieferungssatz" : "")
            .Add(x => x.Loeschhinweis, namen =>
            {
                var projekte = namen.SelectMany(n => kopien is not null && kopien.TryGetValue(n, out var k) ? k : Array.Empty<string>())
                                    .Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();
                return projekte.Count == 0 ? "" : "Die Projekte " + string.Join(", ", projekte) + " behalten ihre Kopie.";
            })
            .Add(x => x.Speichern, (d, neu, name) =>
            {
                pr.Gespeichert.Add((d.Kopie(), neu, name));
                return new GebaeudeKatalogErgebnis(true, "");
            })
            .Add(x => x.Loeschen, n =>
            {
                pr.Geloescht.Add(n);
                pr.Katalog.RemoveAll(h => h.Name == n);
                return true;
            })
            .Add(x => x.Duplizieren, (id, n) =>
            {
                pr.Dupliziert.Add((id, n));
                Haus vorlage = pr.Katalog[id - 1];
                pr.Katalog.Add(vorlage with { Name = n, Geschuetzt = false });
                return new KatalogSpeicherErgebnis(true, "", n);
            })
            .Add(x => x.Exists, n => pr.Katalog.Any(h => h.Name == n))
            .Add(x => x.KatalogGaben, mitEditor
                ? () => { pr.Editor++; return new Dictionary<string, object>(); }
                : null)
            .Add(x => x.GebaeudetypGaben, mitTypen
                ? () => { pr.Gebaeudetypen++; return new Dictionary<string, object>(); }
                : null)
            .Add(x => x.Geschlossen, e => pr.Geschlossen = e));
    }

    private static IElement Knopf(IRenderedComponent<GebaeudeAdminDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static IElement Handlung(IRenderedComponent<GebaeudeAdminDialog> cut, string text)
        => cut.FindAll(".epos-auswahlleiste button").First(b => b.TextContent.Trim() == text);

    private static IElement Fussleiste(IRenderedComponent<GebaeudeAdminDialog> cut)
        => cut.FindAll(".epos-katalog-dialog > .epos-leiste").Last();

    /// <summary>Das Eingabefeld „U bzw. ψ" einer Zeile des Hüll-Rasters.</summary>
    private static IElement Kennwertfeld(IRenderedComponent<GebaeudeAdminDialog> cut, string bauteil)
        => cut.FindAll($".epos-gebaeude-huellraster tr[data-bauteil={bauteil}] input")[0];

    /// <summary>Ein Eingabefeld des Blatts „Konditionierung" nach seiner Beschriftung („Heizen · Tag").</summary>
    private static IElement Blattfeld(IRenderedComponent<GebaeudeAdminDialog> cut, string beschriftung)
        => cut.FindAll("section.epos-blatt label.epos-feld")
              .First(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == beschriftung)
              .QuerySelector("input, select, textarea")!;

    /// <summary>Öffnet das Blatt „Konditionierung" über den Knopf im Kopf der Stammblattgruppe.</summary>
    private static void BlattOeffnen(IRenderedComponent<GebaeudeAdminDialog> cut)
        => cut.Find(".epos-stammblatt button.epos-gebaeude-kondknopf").Click();

    /// <summary>Der Rückknopf des Blatts („‹ Verwaltung Gebäude").</summary>
    private static void BlattZurueck(IRenderedComponent<GebaeudeAdminDialog> cut)
        => cut.Find("section.epos-blatt button.epos-blatt-zurueck").Click();

    /// <summary>Ein Zahlenfeld des Stammblatts nach seiner Beschriftung.</summary>
    private static IElement Feld(IRenderedComponent<GebaeudeAdminDialog> cut, string beschriftung)
        => cut.FindAll(".epos-stammblatt label.epos-feld")
              .First(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == beschriftung)
              .QuerySelector("input, select, textarea")!;

    // =================================================================================
    // Feldbestand und Gerüst
    // =================================================================================

    /// <summary>
    /// <b>Das Gerüst der Verwaltung</b>: Katalogliste mit fünf Spalten (Name, Gebäudeart,
    /// Verwendung, Baujahr, Fläche), kein Projektteil, keine Vorfilter über der Liste; im
    /// Stammblatt die Gruppen des Katalogeditors — Kenndaten, Konditionierung, Hülle, Fenster,
    /// Kenngrößen und „Alle Daten"; Fußleiste Speichern · Verwerfen · Neu… · Beenden.
    /// </summary>
    [Fact]
    public void Das_Geruest_steht_wie_bei_den_Verwaltungen()
    {
        var cut = Aufbauen();

        Assert.Equal("Verwaltung Gebäude", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("epos-katalog-dialog", cut.Find(".epos-dialog").ClassName);
        Assert.Single(cut.FindAll(".epos-katalograhmen"));
        Assert.Empty(cut.FindAll(".epos-zweispalten-uebernahme"));
        Assert.Empty(cut.FindAll("input[type=radio]"));
        Assert.DoesNotContain("ausgewählte Gebäude im Projekt:", cut.Markup);

        var koepfe = cut.FindAll(".epos-katalogliste thead .epos-spaltenkopf-text")
                        .Select(e => e.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "Name", "Gebäudeart", "Verwendung", "Baualtersklasse", "Fläche [m²]", "Kalender" }, koepfe);
        Assert.Equal(4, cut.FindAll(".epos-katalogliste tbody tr").Count);

        Assert.Equal(new[] { "Kenndaten", "Konditionierung", "Hülle", "Fenster nach Orientierung", "Kenngrößen", "Alle Daten" },
                     cut.FindAll(".epos-stammblattgruppe-titel").Select(e => e.TextContent).ToArray());
        Assert.Equal(new[] { "Speichern", "Verwerfen", "Neu…", "Beenden" },
                     Fussleiste(cut).QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray());
        Assert.Single(Fussleiste(cut).QuerySelectorAll("button.epos-knopf--primaer"));
    }

    /// <summary>Ohne Gaben zeichnet die Verwaltung — leere Liste, leeres Blatt, kein Knopf ohne Weg.</summary>
    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog()
    {
        var cut = Render<GebaeudeAdminDialog>();

        Assert.Equal("Verwaltung Gebäude", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal(new[] { "Beenden" },
                     Fussleiste(cut).QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray());
        Assert.Single(cut.FindAll(".epos-auswahlleiste-leise"));
    }

    /// <summary>
    /// <b>Die Zeile ist die Wahl</b> (V4): Beim Öffnen ist die erste Zeile gewählt, ein Klick
    /// auf die dritte zeigt ihr Stammblatt; der Kopf nennt Gebäudeart, Verwendung, Baujahr
    /// und die Herkunft, dazu drei Kennzahlen.
    /// </summary>
    [Fact]
    public void Die_Zeile_waehlt_und_das_Stammblatt_folgt()
    {
        var cut = Aufbauen();
        Assert.Equal("Haus A", cut.Instance.Gewaehlt);

        Zeilenklick.Zeile(cut, 2);

        Assert.Equal("Hotel C", cut.Instance.Gewaehlt);
        Assert.Equal("Hotel C", cut.Find(".epos-stammblatt-nametext").TextContent);
        Assert.Equal("Hotel · Gewerbe+Sonstige · 1958 bis 1968 · eigener Satz",
                     cut.Find(".epos-stammblatt-unter").TextContent);
        Assert.Equal(new[] { "Wohn-/Nutzfläche", "H_ges", "Rechenweg" },
                     cut.FindAll(".epos-stammblatt-kennzahl dt").Select(e => e.TextContent).ToArray());
        Assert.Equal(2400, cut.Instance.Arbeitsstand.WohnflaecheGesamt);
        Assert.Contains("Außenwand", cut.Find(".epos-stammblatt").TextContent);
    }

    /// <summary>
    /// <b>Die vier Vorfilter sind Trichter</b> (V16): Verwendung, Gebäudeart und Baujahr
    /// tragen einen Trichter, die Fläche als Zahl auch; ein gesetzter Trichter auf der
    /// Verwendung lässt nur die Gewerbegebäude stehen.
    /// </summary>
    [Fact]
    public void Die_Vorfilter_sind_Trichter_im_Spaltenkopf()
    {
        var stand = new Katalogfilterstand();
        stand.Setzen(Katalogfilterprofil.SpVerwendung, "Gewerbe");
        var cut = Aufbauen(filterstand: stand);

        Assert.Equal(6, cut.FindAll(".epos-katalogliste thead .epos-trichter").Count);
        Assert.Equal(2, cut.FindAll(".epos-katalogliste tbody tr").Count);
    }

    // =================================================================================
    // Das Stammblatt: jedes Feld des Katalogeditors (#465)
    // =================================================================================

    /// <summary>
    /// <b>Hülle und Wohnfläche sind bedienbar</b>: Das Hüll-Raster trägt acht Zeilen mit dem
    /// Kennwert als Eingabe (die Fensterfläche gerechnet), die Wohn-/Nutzfläche ist ein
    /// Zahlenfeld, die Bauart eine Klappliste — sieben Klapplisten stehen im Blatt (Typ, Art,
    /// Baualtersklasse, Verwendung, Energiestandard, Bauart, Randbedingung); „Alle Daten" ist zugeklappt.
    /// </summary>
    [Fact]
    public void Huelle_und_Wohnflaeche_sind_bedienbar()
    {
        var cut = Aufbauen();

        Assert.Equal(8, cut.FindAll(".epos-gebaeude-huellraster tbody tr").Count);
        Assert.Equal(15, cut.FindAll(".epos-gebaeude-huellraster tbody input").Count);
        Assert.Contains("40,00 m²", cut.Find(".epos-gebaeude-huellraster tr[data-bauteil=Fenster]").TextContent);
        Assert.Equal(7, cut.FindAll(".epos-stammblatt select").Count);
        Assert.Equal("140", Feld(cut, "Wohn-/Nutzfläche").GetAttribute("value"));
        Assert.Contains("H_ges", cut.Find(".epos-stammblatt").TextContent);
        Assert.False(cut.Instance.AlleDatenOffen);
        Assert.Empty(cut.FindAll(".epos-gebaeude-alledaten"));
    }

    /// <summary>
    /// <b>Speichern schreibt den ganzen Feldsatz über den Weg des Katalogeditors</b>: Kenndaten
    /// und ein Hüllwert geändert, der Fuß zählt drei Felder; geschrieben wird unter dem
    /// Bezeichner, als Überschreiben (nicht „neu"), mit den Ableitungen des Editors
    /// (Winterferienbeginn 0 → 366); der Warmwasserbedarf, den kein Feld zeigt, reist unverändert
    /// mit (Befund 25.09.2026: 700 → 0). Danach meldet die Statuszeile.
    /// </summary>
    [Fact]
    public void Speichern_schreibt_den_Feldsatz_ueber_den_Weg_des_Editors()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);
        Assert.True(Knopf(cut, "Speichern").HasAttribute("disabled"));

        cut.FindAll(".epos-stammblatt select")[0].Change("1");      // Gebäudetyp Schule
        cut.Find(".epos-stammblatt textarea").Input("neu");
        Kennwertfeld(cut, "Aussenwand").Input("0,3");

        Assert.True(cut.Instance.Geaendert);
        Assert.Equal("3 Felder geändert", cut.Find(".epos-stammblatt-hinweis").TextContent);

        Knopf(cut, "Speichern").Click();

        var (d, neu, name) = Assert.Single(p.Gespeichert);
        Assert.False(neu);
        Assert.Equal("Haus A", name);
        Assert.Equal("Schule", d.Typ);
        Assert.Equal("neu", d.Beschreibung);
        Assert.Equal(0.3, d.UWertAussenwand);
        Assert.Equal(120, d.FlaecheAussenwand);
        Assert.Equal(366, d.Ferienbeginn[0]);
        Assert.Equal(700, d.WwBedarf);
        Assert.False(cut.Instance.Geaendert);
        Assert.StartsWith("Gespeichert um", cut.Instance.Status);
    }

    /// <summary>
    /// <b>Das Baujahr steht in den Kenndaten neben der Baualtersklasse</b> (G4a): ein Zahlenfeld,
    /// das als geändertes Feld zählt und beim Speichern über den Weg des Editors geschrieben wird;
    /// die Klappliste daneben heißt „Baualtersklasse", nicht mehr „Baujahr".
    /// </summary>
    [Fact]
    public void Das_Baujahr_steht_neben_der_Baualtersklasse_und_wird_gespeichert()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);

        Assert.Equal("SELECT", Feld(cut, "Baualtersklasse").TagName.ToUpperInvariant());
        IElement jahr = Feld(cut, "Baujahr");
        Assert.Equal("INPUT", jahr.TagName.ToUpperInvariant());
        Assert.Equal("", jahr.GetAttribute("value") ?? "");
        List<string> beschriftungen = cut.FindAll(".epos-stammblatt label.epos-feld .epos-feld-text")
                                         .Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(beschriftungen.IndexOf("Baualtersklasse") + 1, beschriftungen.IndexOf("Baujahr"));

        Assert.Null(Feld(cut, "Baualtersklasse").GetAttribute("disabled"));
        jahr.Input("1965");
        Assert.True(cut.Instance.Geaendert);
        // E47 (F2): DAS BAUJAHR FÜHRT - die Klappliste zeigt die Klasse aus dem Jahr und ist gesperrt.
        Assert.NotNull(Feld(cut, "Baualtersklasse").GetAttribute("disabled"));
        Assert.Equal("1958 bis 1968", Feld(cut, "Baualtersklasse").QuerySelector("option[selected]")!.TextContent.Trim());
        Assert.Contains("Die Klasse folgt aus dem Baujahr 1965", cut.Markup);
        Knopf(cut, "Speichern").Click();

        var (d, neu, _) = Assert.Single(p.Gespeichert);
        Assert.False(neu);
        Assert.Equal(1965, d.Baujahr);
        Assert.Equal(4, d.Baualtersklasse);
    }

    /// <summary>
    /// <b>Der Energiestandard</b> (E47, F3): eine Klappliste in den Kenndaten, gefiltert nach der
    /// Verwendung, „keiner" als Platzhalter; gespeichert wird der Code über den Weg des Editors, und
    /// Lesemodus und Vergleich führen ihn als eigene Zeile.
    /// </summary>
    [Fact]
    public void Der_Energiestandard_steht_in_den_Kenndaten_und_wird_als_Code_gespeichert()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);

        IElement standard = Feld(cut, "Energiestandard");
        Assert.Equal("SELECT", standard.TagName.ToUpperInvariant());
        List<string> eintraege = standard.QuerySelectorAll("option").Select(o => o.TextContent.Trim()).ToList();
        Assert.Equal(12, eintraege.Count);                         // Wohngebäude: „keiner" und alle elf
        Assert.Equal("wie Baualtersklasse (unsaniert)", eintraege[0]);
        Assert.Contains("Effizienzhaus 85", eintraege);

        standard.Change("4");                                      // EH85
        Assert.True(cut.Instance.Geaendert);
        Knopf(cut, "Speichern").Click();
        var (d, _, _) = Assert.Single(p.Gespeichert);
        Assert.Equal("EH85", d.Energiestandard);

        // Ein Nichtwohngebäude (Hotel C) bekommt Effizienzhaus 115/100 und 85 nicht angeboten.
        Zeilenklick.Zeile(cut, 2);
        List<string> nichtwohnen = Feld(cut, "Energiestandard").QuerySelectorAll("option")
                                   .Select(o => o.TextContent.Trim()).ToList();
        Assert.Equal(10, nichtwohnen.Count);
        Assert.DoesNotContain("Effizienzhaus 85", nichtwohnen);
    }

    /// <summary>Der Vergleich führt den Energiestandard als eigene Zeile; ohne Standard steht „keiner".</summary>
    [Fact]
    public void Der_Vergleich_fuehrt_den_Energiestandard()
    {
        var cut = Aufbauen();
        cut.FindAll(".epos-katalogliste tbody td.epos-spalte-kaestchen input")[0].Change(true);
        cut.FindAll(".epos-katalogliste tbody td.epos-spalte-kaestchen input")[1].Change(true);
        Handlung(cut, "Vergleichen").Click();

        Assert.Contains(cut.Instance.Vergleichszeilen, z => z.Name == "Energiestandard" && !z.Abweichend);
    }

    /// <summary>Ein Baujahr außerhalb 1500 … 2100 färbt das Feld, und „Speichern" schreibt nichts.</summary>
    [Fact]
    public void Ein_ungueltiges_Baujahr_haelt_das_Speichern_an()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);

        Feld(cut, "Baujahr").Input("1499");
        Assert.Contains("epos-fehleingabe", Feld(cut, "Baujahr").ClassName);
        Knopf(cut, "Speichern").Click();

        Assert.Empty(p.Gespeichert);
        Assert.Contains("Baujahr", cut.Instance.Meldung);

        // Der Grund steht auch ROT in der Statuszeile neben dem Knopf.
        var status = cut.Find(".epos-leiste-fueller.epos-status");
        Assert.Equal(cut.Instance.Meldung, status.TextContent);
        Assert.Contains("epos-status--fehler", status.ClassName);
    }

    /// <summary>Der Vergleich führt Baualtersklasse und Baujahr als zwei Zeilen.</summary>
    [Fact]
    public void Der_Vergleich_fuehrt_Baualtersklasse_und_Baujahr_getrennt()
    {
        var cut = Aufbauen();
        cut.FindAll(".epos-katalogliste tbody td.epos-spalte-kaestchen input")[0].Change(true);
        cut.FindAll(".epos-katalogliste tbody td.epos-spalte-kaestchen input")[1].Change(true);
        Handlung(cut, "Vergleichen").Click();

        Assert.Contains(cut.Instance.Vergleichszeilen, z => z.Name == "Baualtersklasse" && z.Abweichend);
        Assert.Contains(cut.Instance.Vergleichszeilen, z => z.Name == "Baujahr" && !z.Abweichend);
    }

    /// <summary>
    /// <b>Die Nachtzeit steht im Blatt „Konditionierung"</b> (E43, E56 F3 (a); Stufe KP2, Welle U4): EIN
    /// Feld in der Nachtzelle der Heizspalte; es zählt als geändertes Feld, und „Speichern" der Verwaltung
    /// schreibt es über den Weg des Editors — das Blatt verwirft beim Rückweg nichts.
    /// </summary>
    [Fact]
    public void Die_Nachtzeit_steht_im_Blatt_und_wird_mit_Speichern_geschrieben()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);
        cut.Find(".epos-stammblatt .epos-modulparameter-knopf").Click();
        Assert.DoesNotContain(cut.FindAll(".epos-stammblatt label.epos-feld .epos-feld-text"),
                              f => f.TextContent.StartsWith("Nachtabsenkung", StringComparison.Ordinal));

        BlattOeffnen(cut);
        Blattfeld(cut, "Heizen · Nachtfenster").Input("23-5");
        Assert.True(cut.Instance.Geaendert);
        BlattZurueck(cut);
        Knopf(cut, "Speichern").Click();

        var (d, neu, _) = Assert.Single(p.Gespeichert);
        Assert.False(neu);
        Assert.Equal(23, d.NachtBeginn);
        Assert.Equal(5, d.NachtEnde);
    }

    /// <summary>
    /// Eine halbe Nachtzeit (aus einem geladenen Satz) hält „Speichern" an — mit der Regel des Kerns — und
    /// öffnet das Blatt „Konditionierung", in dem ihr Feld steht; das Warnband steht auch dort.
    /// </summary>
    [Fact]
    public void Eine_halbe_Nachtzeit_haelt_das_Speichern_an_und_oeffnet_das_Blatt()
    {
        var p = new Protokoll { Anpassen = d => { d.NachtBeginn = 22; d.NachtEnde = null; } };
        var cut = Aufbauen(p);
        Feld(cut, "Wohn-/Nutzfläche").Input("150");
        Assert.False(cut.Instance.KonditionierungOffen);

        Knopf(cut, "Speichern").Click();

        Assert.Empty(p.Gespeichert);
        Assert.Contains("beide eingeben oder beide leer lassen", cut.Instance.Meldung);
        Assert.True(cut.Instance.KonditionierungOffen);
        Assert.Contains("beide eingeben oder beide leer lassen", cut.Find("section.epos-blatt .epos-warnbanner").TextContent);
    }

    /// <summary>Der Vergleich führt Beginn und Ende der Nachtzeit; leer zeigt die Vorgabe.</summary>
    [Fact]
    public void Der_Vergleich_fuehrt_die_Nachtzeit()
    {
        var cut = Aufbauen();
        cut.FindAll(".epos-katalogliste tbody td.epos-spalte-kaestchen input")[0].Change(true);
        cut.FindAll(".epos-katalogliste tbody td.epos-spalte-kaestchen input")[1].Change(true);
        Handlung(cut, "Vergleichen").Click();

        Assert.Contains(cut.Instance.Vergleichszeilen, z => z.Name == "Nachtabsenkung von" && !z.Abweichend);
        Assert.Contains(cut.Instance.Vergleichszeilen, z => z.Name == "Nachtabsenkung bis" && !z.Abweichend);
    }

    /// <summary>
    /// <b>Die Bauart zieht die Bauweise nach</b> (W9‑O‑2) — derselbe Weg wie im Editor:
    /// „sehr schwer" bei 140 m² ergibt 14 000.
    /// </summary>
    [Fact]
    public void Die_Bauart_zieht_die_Bauweise_nach()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);

        Feld(cut, "Bauart").Change("2");
        Knopf(cut, "Speichern").Click();

        var (d, _, _) = Assert.Single(p.Gespeichert);
        Assert.Equal(2, d.Bauart);
        Assert.Equal(14000, d.Bauweise);
    }

    /// <summary>
    /// <b>Die Prüfung ist die des Katalogeditors</b>: eine Nutzfläche 0 hält den Speicherweg
    /// an, das Warnband nennt die Regel, geschrieben wird nichts.
    /// </summary>
    [Fact]
    public void Die_Pruefung_ist_die_des_Katalogeditors()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);

        Feld(cut, "Wohn-/Nutzfläche").Input("0");
        Knopf(cut, "Speichern").Click();

        Assert.Empty(p.Gespeichert);
        Assert.Equal("Die Nutzfläche muss größer als 0 sein.", cut.Instance.Meldung);
        Assert.True(cut.Instance.Geaendert);
    }

    /// <summary>
    /// <b>Eine verletzte Ferienregel öffnet das Blatt „Konditionierung"</b> — dort stehen die
    /// Ferienzeiträume (E56 F3 (a)); die Meldung ist die des Editors.
    /// </summary>
    [Fact]
    public void Eine_Ferienregel_oeffnet_das_Blatt()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);
        BlattOeffnen(cut);

        // Das erste Feld mit Ziffern ist das Nachtfenster der Heizspalte (E43, ein Feld „22–6"),
        // danach Tag und Monat der vier Ferienanfaenge und -enden.
        IReadOnlyList<IElement> ziffern = cut.FindAll("section.epos-blatt input[inputmode=numeric]");
        Assert.True(ziffern.Count >= 17, ziffern.Count.ToString(CultureInfo.InvariantCulture));
        cut.FindAll("section.epos-blatt input[inputmode=numeric]")[1].Input("1");    // Winter Beginn: 1.2.
        cut.FindAll("section.epos-blatt input[inputmode=numeric]")[2].Input("2");
        cut.FindAll("section.epos-blatt input[inputmode=numeric]")[9].Input("1");    // Winter Ende: 1.3.
        cut.FindAll("section.epos-blatt input[inputmode=numeric]")[10].Input("3");
        BlattZurueck(cut);
        Assert.False(cut.Instance.KonditionierungOffen);

        Knopf(cut, "Speichern").Click();

        Assert.Empty(p.Gespeichert);
        Assert.Equal("Die Ferien müssen über die Jahresgrenze gehen!", cut.Instance.Meldung);
        Assert.True(cut.Instance.KonditionierungOffen);
        Assert.False(cut.Instance.AlleDatenOffen);
    }

    /// <summary>Verwerfen nimmt den Arbeitsstand zurück — Kenndaten wie Hülle; nichts wird geschrieben.</summary>
    [Fact]
    public void Verwerfen_nimmt_den_Arbeitsstand_zurueck()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);

        cut.FindAll(".epos-stammblatt select")[0].Change("2");
        Kennwertfeld(cut, "Dach").Input("0,5");
        Assert.Equal("Hotel", cut.Instance.ArbeitTyp);
        Assert.Equal(0.5, cut.Instance.Arbeitsstand.UWertDachflaeche);

        Knopf(cut, "Verwerfen").Click();

        Assert.Equal("Wohnblock", cut.Instance.ArbeitTyp);
        Assert.Equal(0.25, cut.Instance.Arbeitsstand.UWertDachflaeche);
        Assert.False(cut.Instance.Geaendert);
        Assert.Empty(p.Gespeichert);
    }

    /// <summary>
    /// <b>Geänderte Felder halten an</b> (Konzept 3.3): ein Zeilenwechsel, „Neu…" und
    /// „Beenden" bleiben stehen, das Warnband sagt „Speichern oder Verwerfen".
    /// </summary>
    [Fact]
    public void Geaenderte_Felder_halten_Zeilenwechsel_Neu_und_Beenden_an()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);

        Kennwertfeld(cut, "Aussenwand").Input("0,3");

        Zeilenklick.Zeile(cut, 1);
        Assert.Equal("Haus A", cut.Instance.Gewaehlt);
        Assert.Contains("ungespeicherte", cut.Instance.Meldung);

        Knopf(cut, "Neu…").Click();
        Assert.False(cut.Instance.KatalogeditorOffen);

        Knopf(cut, "Beenden").Click();
        Assert.Null(p.Geschlossen);
    }

    /// <summary>
    /// <b>Ein Auslieferungssatz</b> (V13): Schloss im Kopf, Kenndaten und Hülle als Text statt
    /// Eingaben, „Speichern" und „Löschen" weich gesperrt; einen Knopf „Bearbeiten…" gibt es
    /// nicht mehr.
    /// </summary>
    [Fact]
    public void Ein_Auslieferungssatz_ist_nur_lesbar()
    {
        var cut = Aufbauen();

        Zeilenklick.Zeile(cut, 3);                               // Schule D

        Assert.NotNull(cut.Find(".epos-stammblatt-kopf .epos-schloss"));
        Assert.Empty(cut.FindAll(".epos-stammblatt select"));
        Assert.Empty(cut.FindAll(".epos-stammblatt input"));
        Assert.Contains("120,0 m² · U 0,35 W/(m²K)", cut.Find(".epos-stammblatt").TextContent);
        Assert.Equal("true", Knopf(cut, "Speichern").GetAttribute("aria-disabled"));
        Assert.Equal("true", Handlung(cut, "Löschen").GetAttribute("aria-disabled"));
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Bearbeiten…");

        Knopf(cut, "Speichern").Click();
        Assert.Contains("Auslieferungssatz", cut.Instance.Meldung);
    }

    // =================================================================================
    // Auswahlleiste: Löschen, Duplizieren, Vergleichen
    // =================================================================================

    /// <summary>
    /// <b>Dieselbe Löschregel wie im Gebäudedialog</b> (Anwenderentscheid 06.10.2026): Ein Gebäude, das
    /// ein Projekt führt, ist NICHT gesperrt. Die Rückfrage nennt die Projekte, die ihre Kopie behalten,
    /// und nach dem „Ja" wird gelöscht.
    /// </summary>
    [Fact]
    public void Loeschen_trotz_Projektkopie_die_Rueckfrage_nennt_die_Projekte()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p, kopien: new Dictionary<string, IReadOnlyList<string>>
        {
            ["Haus A"] = new[] { "Projekt Nord", "Projekt Süd" }
        });

        IElement loeschen = Handlung(cut, "Löschen");
        Assert.NotEqual("true", loeschen.GetAttribute("aria-disabled"));
        loeschen.Click();

        string frage = cut.Find(".epos-rueckfrage").TextContent;
        Assert.Contains("Haus A", frage);
        Assert.Contains("Die Projekte Projekt Nord, Projekt Süd behalten ihre Kopie.", frage);
        Knopf(cut, "Ja").Click();

        Assert.Equal(new[] { "Haus A" }, p.Geloescht);
    }

    /// <summary>
    /// <b>Der Auslieferungssatz bleibt gesperrt</b> — die Antwort des Kerns: allein gewählt ist der
    /// Knopf weich gesperrt; in einer Mehrfachwahl mit einem freien und einem benutzten Satz gehen
    /// beide, der Auslieferungssatz bleibt stehen, und die Rückfrage sagt es samt den Projekten.
    /// </summary>
    [Fact]
    public void Der_Auslieferungssatz_bleibt_gesperrt_und_die_Mehrfachwahl_loescht_den_Rest()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p, kopien: new Dictionary<string, IReadOnlyList<string>>
        {
            ["Haus A"] = new[] { "Projekt Nord" }
        });
        Zeilenklick.Zeile(cut, 3);                               // Schule D, Auslieferung
        IElement loeschen = Handlung(cut, "Löschen");
        Assert.Equal("true", loeschen.GetAttribute("aria-disabled"));
        Assert.Contains("Auslieferungssatz", loeschen.GetAttribute("title"));

        IReadOnlyList<IElement> kaestchen = cut.FindAll(".epos-katalogliste tbody td.epos-spalte-kaestchen input");
        kaestchen[0].Change(true);                               // Haus A
        cut.FindAll(".epos-katalogliste tbody td.epos-spalte-kaestchen input")[1].Change(true);   // Haus B
        cut.FindAll(".epos-katalogliste tbody td.epos-spalte-kaestchen input")[3].Change(true);   // Schule D
        Handlung(cut, "Löschen").Click();

        string frage = cut.Find(".epos-rueckfrage").TextContent;
        Assert.Contains("Haus A", frage);
        Assert.Contains("Haus B", frage);
        Assert.Contains("Schule D", frage);
        Assert.Contains("Projekt Nord", frage);
        Knopf(cut, "Ja").Click();

        Assert.Equal(new[] { "Haus A", "Haus B" }, p.Geloescht);
    }

    /// <summary>Löschen fragt zurück, schreibt nach dem „Ja" und nennt es in der Statuszeile.</summary>
    [Fact]
    public void Loeschen_fragt_zurueck_und_meldet_in_der_Statuszeile()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);
        Zeilenklick.Zeile(cut, 1);                               // Haus B

        Handlung(cut, "Löschen").Click();
        Assert.Contains("Haus B", cut.Find(".epos-rueckfrage").TextContent);

        Knopf(cut, "Ja").Click();

        Assert.Equal(new[] { "Haus B" }, p.Geloescht);
        Assert.Equal(3, cut.FindAll(".epos-katalogliste tbody tr").Count);
        Assert.Contains("Haus B", cut.Instance.Status);
    }

    /// <summary>
    /// <b>Duplizieren…</b> (AD-Q11): Namensabfrage mit „… (Kopie)", danach der eigene Satz
    /// gewählt und die Statuszeile nennt Original und Kopie.
    /// </summary>
    [Fact]
    public void Duplizieren_legt_den_eigenen_Satz_an_und_waehlt_ihn()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);
        Zeilenklick.Zeile(cut, 3);                               // Schule D, Auslieferung

        Handlung(cut, "Duplizieren...").Click();
        Assert.True(cut.Instance.Duplizierfrage);
        Assert.Contains("Schule D (Kopie)", cut.Find(".epos-ueberlagerung input").GetAttribute("value"));

        Knopf(cut, "OK").Click();

        Assert.Equal(new[] { (4, "Schule D (Kopie)") }, p.Dupliziert);
        Assert.Equal("Schule D (Kopie)", cut.Instance.Gewaehlt);
        Assert.Contains("Schule D (Kopie)", cut.Instance.Status);
    }

    /// <summary>Zwei Kästchen und „Vergleichen" zeigen den Vergleich im Stammblatt.</summary>
    [Fact]
    public void Vergleichen_zeigt_zwei_Saetze_im_Stammblatt()
    {
        var cut = Aufbauen();
        var kaestchen = cut.FindAll(".epos-katalogliste tbody td.epos-spalte-kaestchen input");
        kaestchen[0].Change(true);
        cut.FindAll(".epos-katalogliste tbody td.epos-spalte-kaestchen input")[2].Change(true);

        Handlung(cut, "Vergleichen").Click();

        Assert.True(cut.Instance.Vergleicht);
        Assert.Contains(cut.Instance.Vergleichszeilen, z => z.Name == "Gebäudeart" && z.Abweichend);
        Assert.NotNull(cut.Find(".epos-stammblatt .epos-vergleichstabelle"));
    }

    // =================================================================================
    // Überlagerungen: Katalogeditor (nur Neu…), Gebäudetypen
    // =================================================================================

    /// <summary>
    /// <b>„Neu…" ist der einzige Weg in den Katalogeditor</b> (AD-Q6) — bearbeitet wird im
    /// Stammblatt; ein neuer Satz ist danach gewählt.
    /// </summary>
    [Fact]
    public void Neu_oeffnet_den_Editor_und_waehlt_den_neuen_Satz()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Bearbeiten…");

        Knopf(cut, "Neu…").Click();
        Assert.Equal(1, p.Editor);
        Assert.True(cut.Instance.KatalogeditorOffen);
        Assert.Single(cut.FindAll(".epos-ueberlagerung .epos-ueberlagerung-zu"));
        Assert.Equal("Neu…", cut.Find(".epos-ueberlagerung-titel").TextContent);
        // Der Editor steht in der breiten Überlagerung wie im Gebäudedialog (Stufe KP2, Festlegung 7).
        Assert.Contains("epos-ueberlagerung--breit", cut.Find(".epos-ueberlagerung").ClassName);

        p.Katalog.Add(new Haus("Neubau E", "Einfamilienhaus", "Wohngebaeude", 0, 120));
        cut.Find(".epos-ueberlagerung-zu").Click();

        Assert.Equal("Neubau E", cut.Instance.Gewaehlt);
        Assert.Contains("Neubau E", cut.Instance.Status);
    }

    /// <summary>
    /// <b>„Gebäudetypen…"</b> im Kopf der Kenndaten öffnet die Verwaltung der Gebäudetypen
    /// als Überlagerung (ein Kreuz); danach liest die Klappliste „Gebäudetyp" neu.
    /// </summary>
    [Fact]
    public void Gebaeudetypen_oeffnet_die_Typenverwaltung_und_laedt_danach_neu()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);
        int vorher = p.Typlisten;

        Knopf(cut, "Gebäudetypen…").Click();

        Assert.True(cut.Instance.GebaeudetypOffen);
        Assert.Equal(1, p.Gebaeudetypen);
        Assert.Single(cut.FindAll(".epos-ueberlagerung .epos-ueberlagerung-zu"));

        cut.Find(".epos-ueberlagerung-zu").Click();

        Assert.False(cut.Instance.GebaeudetypOffen);
        Assert.Equal(vorher + 1, p.Typlisten);
    }

    /// <summary>Ohne Delegaten stehen weder „Neu…" noch „Gebäudetypen…" da.</summary>
    [Fact]
    public void Ohne_Delegat_kein_Knopf()
    {
        var cut = Aufbauen(mitEditor: false, mitTypen: false);

        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() is "Bearbeiten…" or "Neu…" or "Gebäudetypen…");
    }

    // =================================================================================
    // Schluss: Beenden, Esc, Kreuz
    // =================================================================================

    /// <summary>Beenden, Esc und das Kreuz melden <c>true</c> — jede Aktion hat schon geschrieben.</summary>
    [Fact]
    public void Beenden_Esc_und_Kreuz_schliessen_mit_true()
    {
        var p1 = new Protokoll();
        Knopf(Aufbauen(p1), "Beenden").Click();
        Assert.True(p1.Geschlossen);

        var p2 = new Protokoll();
        Aufbauen(p2).Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.True(p2.Geschlossen);

        var p3 = new Protokoll();
        Aufbauen(p3).Find(".epos-dialog-kopf .epos-dialog-zu").Click();
        Assert.True(p3.Geschlossen);
    }

    /// <summary>Esc schließt nicht, solange eine Überlagerung offen ist.</summary>
    [Fact]
    public void Esc_schliesst_nicht_bei_offener_Ueberlagerung()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);
        Knopf(cut, "Gebäudetypen…").Click();

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Null(p.Geschlossen);
    }

    // =================================================================================
    // Der Hilfe-Assistent: die eigene Maske Form_Gebaeude_Admin (#465)
    // =================================================================================

    /// <summary>
    /// <b>Die Verwaltung meldet ihre EIGENE Maske an</b> — unter ihrem Navigationsschlüssel,
    /// mit dem Satz und der Feldliste des Katalogeditors; die Projektdialog-Sicht
    /// <c>Form_Gebaeude</c> bleibt unangemeldet. Beim Schließen meldet sie ab.
    /// </summary>
    [Fact]
    public void Die_Verwaltung_meldet_ihre_eigene_Maske_an_und_ab()
    {
        var cut = Aufbauen();
        const string maske = KiMaskennamen.GEBAEUDE_ADMIN;

        Assert.Equal(WindowsFormsApplication1.Masken.GebaeudeAdmin, maske);
        Assert.True(KiMaskenbruecke.IstAngemeldet(maske));
        Assert.False(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.GEBAEUDE));
        Assert.Equal(maske, KiMaskenbruecke.AktiveMaske());
        IReadOnlyList<KiFeldwert> werte = KiMaskenbruecke.Lesen(maske);
        Assert.Contains(werte, w => w.Name == "satz" && w.IstWahl);
        Assert.Contains(werte, w => w.Name == "u_aussenwand");
        Assert.Contains(werte, w => w.Name == "kuehlung_aktiv");
        Assert.DoesNotContain(werte, w => w.Name == "betriebsart");
        Assert.Equal("Haus A", KiMaskenbruecke.Feldzugang(maske, "satz")!.Lesen());
        Assert.Equal("Haus A", KiMaskenbruecke.Feldzugang(maske, "name")!.Lesen());
        Assert.False(KiMaskenbruecke.Feldzugang(maske, "name")!.Setzbar);

        // Der Infoknopf gibt dem Assistenten den Namen der Verwaltung mit.
        Assert.Contains(cut.FindComponents<InfoKnopf>(), k => k.Instance.Dialogname == "Verwaltung Gebäude");

        cut.Instance.Dispose();
        Assert.False(KiMaskenbruecke.IstAngemeldet(maske));
    }

    /// <summary>
    /// <b>Der Assistent setzt einen Hüllwert</b> — er landet im Arbeitsstand des Stammblatts,
    /// „Speichern" wird frei und der Fuß zählt das Feld.
    /// </summary>
    [Fact]
    public void Der_Assistent_setzt_einen_Huellwert_und_Speichern_wird_frei()
    {
        var cut = Aufbauen();
        const string maske = KiMaskennamen.GEBAEUDE_ADMIN;

        WindowsFormsApplication1.KiFeldzugang zugang = KiMaskenbruecke.Feldzugang(maske, "u_aussenwand")!;
        Assert.True(zugang.Setzbar);
        Assert.Equal(0.35, zugang.Lesen());

        KiFeldumsetzung u = KiFeldwandler.Wandle(zugang, "0.3");
        Assert.True(u.Ok, u.Grund);
        zugang.Setzen(u.Wert);
        KiMaskenbruecke.Haken(maske).Auffrischung();
        cut.Render();

        Assert.Equal(0.3, cut.Instance.Arbeitsstand.UWertAussenwand);
        Assert.False(Knopf(cut, "Speichern").HasAttribute("disabled"));
        Assert.Equal("1 Feld geändert", cut.Find(".epos-stammblatt-hinweis").TextContent);
    }

    /// <summary>
    /// <b>Prüfung und Speicherweg des Assistenten sind die des Knopfes</b>: ohne Änderung
    /// benannt abgelehnt; eine Nutzfläche 0 nennt der Prüfhaken; mit gültigem Wert schreibt
    /// der Speicherhaken denselben Feldsatz und die Statuszeile meldet es. Die Bauart zieht
    /// auch hier die Bauweise nach.
    /// </summary>
    [Fact]
    public async Task Pruefung_und_Speicherweg_des_Assistenten_sind_die_des_Knopfes()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);
        const string maske = KiMaskennamen.GEBAEUDE_ADMIN;
        KiMaskenhaken haken = KiMaskenbruecke.Haken(maske);

        Assert.Equal(KiKern.KiStatus.Abgelehnt, (await haken.Speichern()).Status);

        KiMaskenbruecke.Feldzugang(maske, "wohnflaeche")!.Setzen(0.0);
        cut.Render();
        Assert.Equal("Die Nutzfläche muss größer als 0 sein.", haken.Befund());
        Assert.Equal(KiKern.KiStatus.Abgelehnt, (await haken.Speichern()).Status);
        Assert.Empty(p.Gespeichert);

        KiMaskenbruecke.Feldzugang(maske, "wohnflaeche")!.Setzen(200.0);
        KiMaskenbruecke.Feldzugang(maske, "bauart")!.Setzen(0);
        cut.Render();
        Assert.Equal("", haken.Befund());

        KiKern.KiErgebnis ok = await haken.Speichern();

        Assert.Equal(KiKern.KiStatus.Ausgefuehrt, ok.Status);
        var (d, neu, name) = Assert.Single(p.Gespeichert);
        Assert.False(neu);
        Assert.Equal("Haus A", name);
        Assert.Equal(200, d.WohnflaecheGesamt);
        Assert.Equal(4000, d.Bauweise);                  // leicht: 200 m² × 20
        Assert.StartsWith("Gespeichert um", cut.Instance.Status);
    }

    /// <summary>
    /// <b>Ein Auslieferungssatz ist für den Assistenten geschützt</b> (AD-Q11, AD-Q15): Schon
    /// <c>feld_setzen</c> lehnt ab und nennt „Duplizieren" und „Schloss", ebenso der
    /// Speicherweg. Die WAHL des Satzes bleibt frei.
    /// </summary>
    [Fact]
    public async Task Ein_Auslieferungssatz_lehnt_ab_und_nennt_den_Weg_die_Satzwahl_bleibt_frei()
    {
        Func<bool> schreibrechtVorher = Schreibnaht.Schreibrecht;
        Schreibnaht.Schreibrecht = Schreibnaht.ImmerErlaubt;
        try
        {
            var weg = new EPOS.UI.Bausteine.Schlossweg((ids, _) =>
                new EPOS.UI.Bausteine.SchlossErgebnis(true, "") { Geaendert = ids });
            var cut = Aufbauen(schloss: weg);
            const string maske = KiMaskennamen.GEBAEUDE_ADMIN;

            KiSatzSetzen(cut, maske, "Schule D");
            Assert.Equal("Schule D", cut.Instance.Gewaehlt);

            KiMaskenhaken haken = KiMaskenbruecke.Haken(maske);
            Assert.True(haken.IstSchreibgeschuetzt());
            Assert.Contains("duplizieren", haken.Schutzgrund());
            Assert.Contains("Schloss", haken.Schutzgrund());

            string? grund = Vorbedingung("feld_setzen", maske, ("feld", "u_aussenwand"), ("wert", "0.3"));
            Assert.NotNull(grund);
            Assert.Contains("Schloss", grund);
            Assert.Equal(KiKern.KiStatus.Abgelehnt, (await haken.Speichern()).Status);

            // Die Satzwahl geht durch, und der eigene Satz ist setzbar.
            Assert.Null(Vorbedingung("feld_setzen", maske, ("feld", "satz"), ("wert", "Haus A")));
            KiSatzSetzen(cut, maske, "Haus A");
            Assert.False(haken.IstSchreibgeschuetzt());
            Assert.Null(Vorbedingung("feld_setzen", maske, ("feld", "u_aussenwand"), ("wert", "0.3")));
        }
        finally
        {
            Schreibnaht.Schreibrecht = schreibrechtVorher;
        }
    }

    /// <summary>
    /// <b>„satz" wechselt die Zeile</b> wie ein Klick — und ungespeicherte Änderungen halten
    /// den Wechsel an: BENANNT, mit dem Text des Warnbands.
    /// </summary>
    [Fact]
    public void Der_Satz_wechselt_die_Zeile_und_Aenderungen_halten_den_Wechsel_an()
    {
        var cut = Aufbauen();
        const string maske = KiMaskennamen.GEBAEUDE_ADMIN;

        WindowsFormsApplication1.KiFeldzugang satz = KiMaskenbruecke.Feldzugang(maske, "satz")!;
        Assert.True(satz.IstWahl);
        Assert.Equal(4, satz.Wahleintraege().Count);

        KiSatzSetzen(cut, maske, "Haus B");
        Assert.Equal("Haus B", cut.Instance.Gewaehlt);
        Assert.Equal(900, cut.Instance.Arbeitsstand.WohnflaecheGesamt);

        KiMaskenbruecke.Feldzugang(maske, "u_dachflaeche")!.Setzen(0.5);
        cut.Render();

        var fehler = Assert.Throws<InvalidOperationException>(() => satz.Setzen("Haus A"));
        Assert.Contains("Speichern", fehler.Message);
        cut.Render();
        Assert.Equal("Haus B", cut.Instance.Gewaehlt);
        Assert.Contains("Speichern", cut.Instance.Meldung);
    }

    /// <summary>Setzt den Satz über die Brücke — der Weg des Assistenten.</summary>
    private static void KiSatzSetzen(IRenderedComponent<GebaeudeAdminDialog> cut, string maske, string name)
    {
        WindowsFormsApplication1.KiFeldzugang satz = KiMaskenbruecke.Feldzugang(maske, "satz")!;
        KiFeldumsetzung u = KiFeldwandler.Wandle(satz, name);
        Assert.True(u.Ok, u.Grund);
        satz.Setzen(u.Wert);
        cut.Render();
    }

    /// <summary>Die Vorbedingung einer Formularaktion — die Stelle, die ablehnt oder durchlässt.</summary>
    private static string? Vorbedingung(string aktion, string maske, params (string Name, string Wert)[] werte)
    {
        var parameter = new Dictionary<string, object?> { ["maske"] = maske };
        foreach (var (name, wert) in werte) parameter[name] = wert;

        var schicht = new KiAusfuehrung { Schreibrecht = () => true };
        KiKern.KiPruefErgebnis p = KiKern.KiPruefung.Pruefe(schicht.Register, aktion, parameter);
        Assert.True(p.Gueltig, p.FehlerText());

        return schicht.Register.Finde(aktion)!.Vorbedingung!(p.Aufruf!);
    }

    // =================================================================================
    // „Schloss setzen…" / „Schloss aufheben…" (Entscheid AD-Q15)
    // =================================================================================

    /// <summary>
    /// <b>Das Schloss eines Gebäudes aufheben</b> (AD-Q15): Die Handlung steht zwischen
    /// „Duplizieren…" und „Löschen"; nach dem „Ja" ist „Schule D" ein eigener Satz, die
    /// Felder sind bedienbar, „Speichern" ist frei und das Stammblatt trägt das Band.
    /// </summary>
    [Fact]
    public void Schloss_aufheben_nach_Rueckfrage_gibt_Speichern_frei()
    {
        var pr = new Protokoll();
        var aufrufe = new List<(IReadOnlyList<int> Ids, bool Gesperrt)>();
        var weg = new EPOS.UI.Bausteine.Schlossweg((ids, gesperrt) =>
        {
            aufrufe.Add((ids, gesperrt));
            foreach (int id in ids) pr.Katalog[id - 1] = pr.Katalog[id - 1] with { Geschuetzt = gesperrt };
            return new EPOS.UI.Bausteine.SchlossErgebnis(true, "") { Geaendert = ids };
        });
        var cut = Aufbauen(pr, schloss: weg);

        Zeilenklick.Zeile(cut, 3);       // "Schule D"
        Assert.Equal("Schule D", cut.Instance.Gewaehlt);
        Assert.Equal(new[] { "Vergleichen", "Duplizieren...", "Schloss aufheben...", "Löschen" },
                     Schlosspruefung.Handlungen(cut));

        Schlosspruefung.Knopf(cut).Click();
        Schlosspruefung.Ja(cut);

        Assert.Equal(new[] { 4 }, aufrufe.Single().Ids);
        Assert.True(Schlosspruefung.Band(cut));
        Assert.Equal("Schloss von „Schule D“ aufgehoben.", cut.Instance.Status);
        Assert.Empty(cut.FindAll(".epos-stammblatt-name .epos-schloss"));
        Assert.Equal(8, cut.FindAll(".epos-gebaeude-huellraster tbody tr").Count);
    }

    /// <summary>
    /// <b>E37: Die Kühlübergabe steht in „Alle Daten" unter der Kühlung</b> — derselbe Baustein
    /// wie im Katalogeditor, nur mit dem Haken „Gebäude wird gekühlt"; Schalter und Art gehen über
    /// den gemeinsamen Arbeitsstand in den Schreibweg, der Fuß zählt sie mit.
    /// </summary>
    [Fact]
    public void Die_Kuehluebergabe_steht_in_Alle_Daten_unter_der_Kuehlung()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);
        cut.Find(".epos-stammblatt .epos-modulparameter-knopf").Click();
        Assert.Empty(cut.FindAll(".epos-gebaeude-alledaten div.gebk-kuehluebergabe"));

        IElement Schalter(string text) => cut.FindAll(".epos-gebaeude-alledaten label.epos-schalter")
            .First(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == text).QuerySelector("input")!;

        Schalter("Gebäude wird gekühlt").Change(true);
        // Der Kühlsollwert steht in der Spalte „Kühlen" des Blatts „Konditionierung" (E56 F3 (a)).
        Assert.DoesNotContain(cut.FindAll(".epos-stammblatt label.epos-feld .epos-feld-text"),
                              f => f.TextContent.Trim() == "Kühlsollwert");
        BlattOeffnen(cut);
        Blattfeld(cut, "Kühlen · Tag").Input("26");
        BlattZurueck(cut);
        Assert.True(cut.Instance.AlleDatenOffen);
        Assert.Single(cut.FindAll(".epos-gebaeude-alledaten div.gebk-kuehluebergabe"));
        Schalter("Kühlübergabe rechnen (statt idealer Kühlung)").Change(true);
        Feld(cut, "Kühlübergabeart :").Change("2");
        Feld(cut, "Auslegung Kühlvorlauf :").Input("15");
        // Kühlung, Kühlsollwert, Schalter, Art, Auslegungsvorlauf.
        Assert.Equal("5 Felder geändert", cut.Find(".epos-stammblatt-hinweis").TextContent);

        Knopf(cut, "Speichern").Click();

        var (d, _, _) = Assert.Single(p.Gespeichert);
        Assert.True(d.KuehlungAktiv);
        Assert.True(d.KuehluebergabeAktiv);
        Assert.Equal(DbWerte.KUEHLUEBERGABE_FLAECHENKUEHLUNG, d.KuehlUebergabeArt);
        Assert.Equal(15.0, d.KuehlAuslegungVorlauf);
        Assert.Null(d.KuehlVorlaufgrenze);
    }

    // =================================================================================
    // Das Blatt „Konditionierung" (Stufe KP2, Welle U4; Entwurf KP2 Festlegung 6, E56 F3 (a))
    // =================================================================================

    /// <summary>Der reine Weg des Katalogbaus — derselbe Kern wie die Hülle, ohne Datenbank.</summary>
    private static KonditionierungWeg Katalogweg()
        => KonditionierungHuelle.ReinerWeg(Kalendereigentuemer.Katalogbau, projekt: false);

    /// <summary>Jeder gelesene Satz trägt eine (leere) Konditionierung — wie aus der Hülle mit Tabellen.</summary>
    private static Protokoll MitKonditionierung() => new() { Anpassen = d => d.Konditionierung = new KonditionierungDaten() };

    private static IElement Konditionierungsgruppe(IRenderedComponent<GebaeudeAdminDialog> cut)
        => cut.FindAll("section.epos-stammblattgruppe")
              .First(s => s.QuerySelector(".epos-stammblattgruppe-titel")?.TextContent == "Konditionierung");

    private static string[] Zustandswerte(IRenderedComponent<GebaeudeAdminDialog> cut)
        => Konditionierungsgruppe(cut).QuerySelectorAll(".epos-stammblattwert dd").Select(e => e.TextContent.Trim()).ToArray();

    /// <summary>
    /// <b>Die Gruppe „Konditionierung" trägt fünf Zustandszeilen als Text</b> und im Kopf
    /// „Konditionierung…"; der Knopf öffnet das BREITE Blatt mit derselben Komponente wie der Reiter
    /// des Editors — Liste, Stammblatt und Fußleiste stehen dann nicht da, der Rückknopf führt zurück.
    /// </summary>
    [Fact]
    public void Die_Gruppe_Konditionierung_zeigt_fuenf_Zustandszeilen_und_oeffnet_das_breite_Blatt()
    {
        var p = MitKonditionierung();
        var cut = Aufbauen(p, konditionierung: Katalogweg());

        IElement gruppe = Konditionierungsgruppe(cut);
        Assert.Contains("epos-stammblattgruppe--lesen", gruppe.ClassName);
        Assert.Empty(gruppe.QuerySelectorAll("input"));
        Assert.Equal(new[] { "Heizen", "Kühlen", "Lüftung", "Geräte", "Personen" },
                     gruppe.QuerySelectorAll(".epos-stammblattwert dt").Select(e => e.TextContent.Trim()).ToArray());
        Assert.All(Zustandswerte(cut), w => Assert.Equal("aus der Matrix", w));
        Assert.Equal("Konditionierung…", gruppe.QuerySelector("button.epos-gebaeude-kondknopf")!.TextContent.Trim());
        Assert.Null(gruppe.QuerySelector("p.epos-gebaeude-kondsperre"));

        BlattOeffnen(cut);

        Assert.True(cut.Instance.KonditionierungOffen);
        IElement blatt = cut.Find("section.epos-blatt");
        Assert.Contains("epos-blatt--breit", blatt.ClassName);
        Assert.Equal("Konditionierung", cut.Find(".epos-blatt-titel").TextContent.Trim());
        Assert.Contains("Verwaltung Gebäude", cut.Find(".epos-blatt-zurueck").TextContent);
        Assert.Single(cut.FindAll("section.epos-blatt table.epos-kond-matrix"));
        Assert.Empty(cut.FindAll(".epos-katalograhmen"));
        Assert.Empty(cut.FindAll(".epos-katalog-dialog > .epos-leiste"));
        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Contains("„Speichern“ der Verwaltung schreibt sie", cut.Find(".epos-gebaeude-kondblatt-hinweis").TextContent);

        BlattZurueck(cut);
        Assert.False(cut.Instance.KonditionierungOffen);
        Assert.Single(cut.FindAll(".epos-katalograhmen"));
        Assert.Empty(cut.FindAll("section.epos-blatt"));

        // Esc auf dem Blatt führt zurück, schließt die Verwaltung aber nicht.
        BlattOeffnen(cut);
        cut.Find("section.epos-blatt").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(cut.Instance.KonditionierungOffen);
        Assert.Null(p.Geschlossen);
    }

    /// <summary>
    /// <b>Das Blatt arbeitet am SELBEN Arbeitsstand wie das Stammblatt</b>: Eine Bestandszelle schreibt
    /// ihr Feld, eine Matrixzelle die Konditionierung (neue Fassung); der Fuß zählt beide, „Speichern" der
    /// Verwaltung schreibt beide über den Weg des Editors — ein zweites „Speichern" schreibt nichts.
    /// </summary>
    [Fact]
    public void Speichern_der_Verwaltung_schreibt_Bestandszelle_und_Matrix_einmal()
    {
        var p = MitKonditionierung();
        var cut = Aufbauen(p, konditionierung: Katalogweg());
        GebaeudeArbeitsstand arbeit = cut.Instance.Arbeit;
        BlattOeffnen(cut);
        Assert.Same(arbeit, cut.Instance.Konditionierungsbearbeitung.Arbeit);

        Blattfeld(cut, "Heizen · Tag").Input("21");
        Blattfeld(cut, "Personen · Nennwert").Input("1000");
        Assert.Equal(21.0, cut.Instance.Arbeitsstand.SollTag);
        Assert.True(cut.Instance.Arbeitsstand.Konditionierung!.Fassung > 0);
        BlattZurueck(cut);

        Assert.Equal("2 Felder geändert", cut.Find(".epos-stammblatt-hinweis").TextContent);

        Knopf(cut, "Speichern").Click();

        var (d, neu, name) = Assert.Single(p.Gespeichert);
        Assert.False(neu);
        Assert.Equal("Haus A", name);
        Assert.Equal(21.0, d.SollTag);
        Assert.NotNull(d.Konditionierung);
        Assert.True(d.Konditionierung!.Fassung > 0);
        Assert.Equal(1000.0, d.Konditionierung.Spalte(KonditionierungGroesse.Personen).Zelle(KonditionierungZeile.Nennwert).Wert);

        // Nach dem Schreiben liest die Verwaltung den Satz neu: nichts mehr geändert, kein zweites Schreiben.
        Assert.False(cut.Instance.Geaendert);
        Knopf(cut, "Speichern").Click();
        Assert.Single(p.Gespeichert);
    }

    /// <summary>„Verwerfen" nimmt auch die Konditionierung zurück; danach ist nichts mehr geändert.</summary>
    [Fact]
    public void Verwerfen_nimmt_die_Konditionierung_zurueck()
    {
        var p = MitKonditionierung();
        var cut = Aufbauen(p, konditionierung: Katalogweg());
        BlattOeffnen(cut);
        Blattfeld(cut, "Personen · Nennwert").Input("800");
        BlattZurueck(cut);
        Assert.True(cut.Instance.Geaendert);

        Knopf(cut, "Verwerfen").Click();

        Assert.False(cut.Instance.Geaendert);
        Assert.Equal(0, cut.Instance.Arbeitsstand.Konditionierung!.Fassung);
        Assert.Empty(p.Gespeichert);
    }

    /// <summary>
    /// <b>Ein ausgelieferter Satz steht im Blatt im Lesemodus</b>: Werte als Text, keine Eingabe, keine
    /// Handlung; die leise Zeile nennt den Grund statt des Schreibwegs. Der Knopf der Gruppe bleibt —
    /// lesen darf man.
    /// </summary>
    [Fact]
    public void Ein_Auslieferungssatz_steht_im_Blatt_als_Text()
    {
        var cut = Aufbauen(MitKonditionierung(), konditionierung: Katalogweg());
        Zeilenklick.Zeile(cut, 3);                               // Schule D
        Assert.Equal("Schule D", cut.Instance.Gewaehlt);

        BlattOeffnen(cut);

        Assert.Empty(cut.FindAll("section.epos-blatt table.epos-kond-matrix input"));
        Assert.NotEmpty(cut.FindAll("section.epos-blatt span.epos-kond-text"));
        Assert.Empty(cut.FindAll("section.epos-blatt button.epos-kond-zuruecknehmen"));
        Assert.DoesNotContain("schreibt sie", cut.Find(".epos-gebaeude-kondblatt-hinweis").TextContent);
    }

    /// <summary>
    /// <b>Ohne Weg der Konditionierung</b> (ohne Tabellen) steht das Blatt benannt gesperrt da — die
    /// Bestandszellen bleiben bedienbar, denn die Altfelder stehen nur noch hier (E56 F3 (a)); die Gruppe
    /// nennt den Grund unter den Zustandszeilen.
    /// </summary>
    [Fact]
    public void Ohne_Weg_nennt_die_Gruppe_den_Grund_und_die_Bestandszellen_bleiben()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);

        Assert.Contains("Die Konditionierung steht nicht zur Verfügung",
                        Konditionierungsgruppe(cut).QuerySelector("p.epos-gebaeude-kondsperre")!.TextContent);
        BlattOeffnen(cut);
        Assert.Empty(cut.FindAll("section.epos-blatt section.epos-kond-karte"));
        Blattfeld(cut, "Geräte · Nennwert").Input("7");
        Blattfeld(cut, "Lüftung · Infiltration").Input("0,2");
        BlattZurueck(cut);
        Knopf(cut, "Speichern").Click();

        var (d, _, _) = Assert.Single(p.Gespeichert);
        Assert.Equal(7.0, d.Waermegewinne);
        Assert.Equal(0.2, d.LuftwechselInfiltration);
    }

    /// <summary>
    /// <b>Die Altfelder stehen nicht mehr im Stammblatt</b> (E56 F3 (a)): Kenngrößen und „Alle Daten"
    /// tragen keine Sollwerte, keine Nachtzeit, keine inneren Wärmegewinne, keine Infiltration,
    /// Nutzerlüftung, Sommerlüftung, keinen Kühlsollwert und keine Maximalraumtemperatur — eine leise
    /// Zeile in „Alle Daten" sagt, wo sie stehen.
    /// </summary>
    [Fact]
    public void Alle_Daten_verliert_die_Altfelder_und_sagt_wo_sie_stehen()
    {
        var cut = Aufbauen();
        cut.Find(".epos-stammblatt .epos-modulparameter-knopf").Click();

        List<string> felder = cut.FindAll(".epos-stammblatt label .epos-feld-text")
                                 .Select(f => f.TextContent.Trim()).ToList();
        foreach (string altfeld in new[]
                 {
                     "Soll am Tag", "Nachtabsenkung", "Soll am Wochenende", "Soll in Ferien", "Maximalraumtemperatur",
                     "Interne Wärmegewinne", "Infiltration", "Nutzerlüftung", "Sommerlüftung", "Kühlsollwert"
                 })
            Assert.DoesNotContain(felder, f => f.StartsWith(altfeld, StringComparison.Ordinal));
        Assert.Contains(felder, f => f.StartsWith("Raumhöhe", StringComparison.Ordinal));
        Assert.Contains("stehen in der Gruppe „Konditionierung“", cut.Find(".epos-gebaeude-alledaten").TextContent);
    }

    /// <summary>
    /// <b>Die Verwaltung führt die Vorgabe-Matrix beim Assistenten</b> (Stufe KP2, Welle U4): dieselbe
    /// Feldtafel wie der Katalogeditor, über die Bearbeitung des Blatts am selben Arbeitsstand — ein
    /// gesetztes Feld zählt als Änderung und schreibt mit „Speichern"; ein Auslieferungssatz lehnt ab.
    /// </summary>
    [Fact]
    public void Der_Assistent_setzt_die_Matrix_der_Verwaltung()
    {
        var p = MitKonditionierung();
        var cut = Aufbauen(p, konditionierung: Katalogweg());

        KiFeldzugang personen = KiMaskenbruecke.Feldzugang(KiMaskennamen.GEBAEUDE_ADMIN, "kond_personen_nennwert");
        Assert.NotNull(personen);
        Assert.Null(personen.Lesen());
        personen.Setzen(900.0);
        cut.Render();

        Assert.Equal(900.0, personen.Lesen());
        Assert.True(cut.Instance.Geaendert);
        Knopf(cut, "Speichern").Click();
        var (d, _, _) = Assert.Single(p.Gespeichert);
        Assert.Equal(900.0, d.Konditionierung!.Spalte(KonditionierungGroesse.Personen).Zelle(KonditionierungZeile.Nennwert).Wert);
    }

    /// <summary>
    /// <b>Die Vorlagen im Blatt der Verwaltung beim Assistenten</b> (Welle U2 über U4): Der Weg des
    /// Katalogbaus reicht die Vorlagen; die Verwaltung meldet ihre Listen als Wahl an —
    /// <c>kond_heizen_vorlage</c> nennt die Einträge der Karte, Setzen übernimmt „Büro" wie der Knopf, das
    /// Feld liest danach die Herkunft, und „Speichern" schreibt die Konditionierung mit.
    /// </summary>
    [Fact]
    public void Der_Assistent_uebernimmt_eine_Vorlage_im_Blatt_der_Verwaltung()
    {
        var p = MitKonditionierung();
        KonditionierungWeg weg = KonditionierungHuelle.ReinerWeg(Kalendereigentuemer.Katalogbau, projekt: false,
                                                                vorlagen: Konditionierungsvorlagenablage.AusSaat());
        var cut = Aufbauen(p, konditionierung: weg);

        KiFeldzugang heizen = KiMaskenbruecke.Feldzugang(KiMaskennamen.GEBAEUDE_ADMIN, "kond_heizen_vorlage");
        Assert.NotNull(heizen);
        Assert.NotNull(heizen.Eintraege);
        Assert.Contains(heizen.Eintraege(), e => e.Text == "Büro");

        heizen.Setzen("Büro");
        cut.Render();

        Assert.Equal("Büro", heizen.Lesen());
        Assert.True(cut.Instance.Geaendert);
        Knopf(cut, "Speichern").Click();
        var (d, _, _) = Assert.Single(p.Gespeichert);
        Assert.True(d.Konditionierung!.Fassung > 0);
    }
}
