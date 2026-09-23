using System;
using System.Collections.Generic;
using System.Linq;
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
/// Stufe 5, V16; Bestand A9) — Katalogliste mit Profil statt eigener Tabelle und vier
/// Vorfiltern, Stammblatt mit Kenndaten, Hülle und „Alle Daten", Auswahlleiste mit
/// Vergleichen, Duplizieren… und Löschen (Verwendungssperre), Fußleiste Speichern ·
/// Verwerfen · Status · Neu… · Beenden.
///
/// <para>Die Kultur ist auf de-DE gepinnt — die Erwartungswerte sind deutsche
/// Beschriftungen.</para>
/// </summary>
public class GebaeudeAdminDialogTests : EposBunitContext
{
    private static readonly string[] KLASSEN =
    { "vor 1919", "1919 bis 1948", "1949 bis 1957", "1958 bis 1968", "1969 bis 1978" };

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
                .MitText(Katalogfilterprofil.SpBaujahr, KLASSEN[h.Klasse])
                .MitZahl(Katalogfilterprofil.SpFlaecheM2, h.Flaeche, 0))
            .ToList();

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
        }
    };

    public GebaeudeAdminDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Was die Wege des Dialogs gerufen haben — die Tests prüfen es.</summary>
    private sealed class Protokoll
    {
        internal readonly List<GebaeudeKenndaten> Gespeichert = new();
        internal readonly List<string> Geloescht = new();
        internal readonly List<(int Id, string Name)> Dupliziert = new();
        internal readonly List<string> Editor = new();
        internal int Gebaeudetypen;
        internal int Typlisten;
        internal bool? Geschlossen;
        internal List<Haus> Katalog = KATALOG.ToList();
    }

    private IRenderedComponent<GebaeudeAdminDialog> Aufbauen(
        Protokoll? p = null,
        Katalogfilterstand? filterstand = null,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? verwendung = null,
        bool mitEditor = true,
        bool mitTypen = true,
        EPOS.UI.Bausteine.Schlossweg? schloss = null)
    {
        Protokoll pr = p ?? new Protokoll();
        return Render<GebaeudeAdminDialog>(b => b
            .Add(x => x.Katalogzeilen, () => Zeilen(pr.Katalog))
            .Add(x => x.Schloss, schloss)
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerGebaeude(s => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Filterstandvorgabe, filterstand ?? new Katalogfilterstand())
            .Add(x => x.Satz, name =>
            {
                int i = pr.Katalog.FindIndex(h => h.Name == name);
                return i < 0 ? null : Satz(pr.Katalog[i], i + 1);
            })
            .Add(x => x.Gebaeudetypen, () =>
            {
                pr.Typlisten++;
                return new[] { "Wohnblock", "Schule", "Hotel" };
            })
            .Add(x => x.Gebaeudearten, () => new[] { "Einfamilienhaus", "Mehrfamilienhaus", "Hotel", "Schule" })
            .Add(x => x.Baualtersklassen, KLASSEN)
            .Add(x => x.Verwendungen, new[] { "Wohngebäude", "Gewerbe+Sonstige" })
            .Add(x => x.Verwendung, () => verwendung ?? new Dictionary<string, IReadOnlyList<string>>())
            .Add(x => x.Speichern, d =>
            {
                pr.Gespeichert.Add(d);
                return new KatalogSpeicherErgebnis(true, "", d.Name);
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
                ? n => { pr.Editor.Add(n); return new Dictionary<string, object>(); }
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

    // =================================================================================
    // Feldbestand und Gerüst
    // =================================================================================

    /// <summary>
    /// <b>Das Gerüst der Verwaltung</b>: Katalogliste mit fünf Spalten (Name, Gebäudeart,
    /// Verwendung, Baujahr, Fläche) statt der eigenen Tabelle, kein Projektteil, keine
    /// Vorfilter über der Liste; Stammblatt mit Kenndaten, Hülle und „Alle Daten";
    /// Fußleiste Speichern · Verwerfen · Neu… · Beenden.
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
        Assert.Equal(new[] { "Name", "Gebäudeart", "Verwendung", "Baujahr", "Fläche [m²]" }, koepfe);
        Assert.Equal(4, cut.FindAll(".epos-katalogliste tbody tr").Count);

        Assert.Equal(new[] { "Kenndaten", "Hülle", "Alle Daten" },
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
        Assert.Equal("Hotel · Gewerbe+Sonstige · 1969 bis 1978 · eigener Satz",
                     cut.Find(".epos-stammblatt-unter").TextContent);
        Assert.Equal(new[] { "Wohn-/Nutzfläche", "H_ges", "Rechenweg" },
                     cut.FindAll(".epos-stammblatt-kennzahl dt").Select(e => e.TextContent).ToArray());
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

        Assert.Equal(5, cut.FindAll(".epos-katalogliste thead .epos-trichter").Count);
        Assert.Equal(2, cut.FindAll(".epos-katalogliste tbody tr").Count);
    }

    // =================================================================================
    // Kenndaten: direkt bedienbar, Speichern und Verwerfen
    // =================================================================================

    /// <summary>
    /// <b>Speichern schreibt die fünf Kenndaten</b>: Gebäudetyp, Gebäudeart, Verwendung
    /// (Steuerwert), Baujahr (Index) und Beschreibung — danach meldet die Statuszeile.
    /// </summary>
    [Fact]
    public void Speichern_schreibt_die_Kenndaten()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);

        var klapplisten = cut.FindAll(".epos-stammblatt select");
        Assert.Equal(4, klapplisten.Count);                     // Typ, Art, Baujahr, Verwendung
        Assert.True(Knopf(cut, "Speichern").HasAttribute("disabled"));

        cut.FindAll(".epos-stammblatt select")[0].Change("1");      // Gebäudetyp Schule
        cut.FindAll(".epos-stammblatt select")[3].Change("1");      // Verwendung Gewerbe
        cut.Find(".epos-stammblatt textarea").Input("neu");

        Assert.True(cut.Instance.Geaendert);
        Assert.Equal("3 Felder geändert", cut.Find(".epos-stammblatt-hinweis").TextContent);

        Knopf(cut, "Speichern").Click();

        GebaeudeKenndaten d = Assert.Single(p.Gespeichert);
        Assert.Equal(new GebaeudeKenndaten("Haus A", "Schule", "Einfamilienhaus", "Nicht Wohngebaeude", 1, "neu"), d);
        Assert.False(cut.Instance.Geaendert);
        Assert.StartsWith("Gespeichert um", cut.Instance.Status);
    }

    /// <summary>Verwerfen nimmt den Arbeitsstand zurück; nichts wird geschrieben.</summary>
    [Fact]
    public void Verwerfen_nimmt_den_Arbeitsstand_zurueck()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);

        cut.FindAll(".epos-stammblatt select")[0].Change("2");
        Assert.Equal("Hotel", cut.Instance.ArbeitTyp);

        Knopf(cut, "Verwerfen").Click();

        Assert.Equal("Wohnblock", cut.Instance.ArbeitTyp);
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

        cut.Find(".epos-stammblatt textarea").Input("geändert");

        Zeilenklick.Zeile(cut, 1);
        Assert.Equal("Haus A", cut.Instance.Gewaehlt);
        Assert.Contains("ungespeicherte", cut.Instance.Meldung);

        Knopf(cut, "Neu…").Click();
        Assert.False(cut.Instance.KatalogeditorOffen);

        Knopf(cut, "Beenden").Click();
        Assert.Null(p.Geschlossen);
    }

    /// <summary>
    /// <b>Ein Auslieferungssatz</b> (V13): Schloss im Kopf, die Kenndaten als Text statt
    /// Klapplisten, „Speichern" und „Bearbeiten…" weich gesperrt, „Löschen" weich gesperrt.
    /// </summary>
    [Fact]
    public void Ein_Auslieferungssatz_ist_nur_lesbar()
    {
        var cut = Aufbauen();

        Zeilenklick.Zeile(cut, 3);                               // Schule D

        Assert.NotNull(cut.Find(".epos-stammblatt-kopf .epos-schloss"));
        Assert.Empty(cut.FindAll(".epos-stammblatt select"));
        Assert.Equal("true", Knopf(cut, "Speichern").GetAttribute("aria-disabled"));
        Assert.Equal("true", Knopf(cut, "Bearbeiten…").GetAttribute("aria-disabled"));
        Assert.Equal("true", Handlung(cut, "Löschen").GetAttribute("aria-disabled"));

        Knopf(cut, "Speichern").Click();
        Assert.Contains("Auslieferungssatz", cut.Instance.Meldung);
    }

    // =================================================================================
    // Auswahlleiste: Löschen, Duplizieren, Vergleichen
    // =================================================================================

    /// <summary>
    /// <b>Die Verwendungssperre</b>: Ein Gebäude, das ein Projekt führt, ist weich gegen
    /// Löschen gesperrt, und der Kurztext nennt das Projekt.
    /// </summary>
    [Fact]
    public void Loeschen_ist_gesperrt_wenn_ein_Projekt_das_Gebaeude_fuehrt()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p, verwendung: new Dictionary<string, IReadOnlyList<string>>
        {
            ["Haus A"] = new[] { "Projekt Nord" }
        });

        IElement loeschen = Handlung(cut, "Löschen");
        Assert.Equal("true", loeschen.GetAttribute("aria-disabled"));
        Assert.Contains("Projekt Nord", loeschen.GetAttribute("title"));

        loeschen.Click();
        Assert.Contains("Projekt Nord", cut.Instance.Meldung);
        Assert.Empty(p.Geloescht);
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
    // Überlagerungen: Katalogeditor, Neu, Gebäudetypen
    // =================================================================================

    /// <summary>
    /// <b>„Bearbeiten…"</b> im Kopf der Gruppe Hülle öffnet den Katalogeditor des gewählten
    /// Satzes als Überlagerung — mit Titel und genau einem Kreuz.
    /// </summary>
    [Fact]
    public void Bearbeiten_oeffnet_den_Katalogeditor_als_Ueberlagerung()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);

        Knopf(cut, "Bearbeiten…").Click();

        Assert.True(cut.Instance.KatalogeditorOffen);
        Assert.Equal(new[] { "Haus A" }, p.Editor);
        Assert.Single(cut.FindAll(".epos-ueberlagerung .epos-ueberlagerung-zu"));
        Assert.Contains("Gebäudedaten", cut.Find(".epos-ueberlagerung-titel").TextContent);

        cut.Find(".epos-ueberlagerung-zu").Click();
        Assert.False(cut.Instance.KatalogeditorOffen);
    }

    /// <summary>„Neu…" öffnet den Editor ohne Namen; ein neuer Satz ist danach gewählt.</summary>
    [Fact]
    public void Neu_oeffnet_den_Editor_und_waehlt_den_neuen_Satz()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);

        Knopf(cut, "Neu…").Click();
        Assert.Equal(new[] { "" }, p.Editor);

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

    /// <summary>Ohne Delegaten stehen weder „Bearbeiten…", „Neu…" noch „Gebäudetypen…" da.</summary>
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
    // Der Hilfe-Assistent: dieselbe Sicht wie der Projektdialog
    // =================================================================================

    /// <summary>
    /// Die Verwaltung meldet sich unter derselben Maske an wie der Projektdialog; das Feld
    /// „verwaltung" sagt <c>true</c>, und ein Filterfeld setzt den TRICHTER seiner Spalte.
    /// </summary>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_setzt_einen_Trichter()
    {
        var stand = new Katalogfilterstand();
        var cut = Aufbauen(filterstand: stand);

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.GEBAEUDE));
        Assert.Equal("Haus A", KiMaskenbruecke.Feldzugang(KiMaskennamen.GEBAEUDE, "name").Lesen());
        Assert.Contains(KiMaskenbruecke.Lesen(KiMaskennamen.GEBAEUDE),
                        f => f.Name == "verwaltung" && f.Text.Length > 0);

        WindowsFormsApplication1.KiFeldzugang zugang = KiMaskenbruecke.Feldzugang(KiMaskennamen.GEBAEUDE, "filter_baujahr");
        KiFeldumsetzung umsetzung = KiFeldwandler.Wandle(zugang, "1958 bis 1968");
        Assert.True(umsetzung.Ok, umsetzung.Grund);
        zugang.Setzen(umsetzung.Wert);
        cut.Render();

        Assert.Equal("1958 bis 1968", stand.Ausdruck(Katalogfilterprofil.SpBaujahr));
        Assert.Single(cut.FindAll(".epos-katalogliste tbody tr"));
    }
    // =================================================================================
    // „Schloss setzen…" / „Schloss aufheben…" (Entscheid AD-Q15)
    // =================================================================================

    /// <summary>
    /// <b>Das Schloss eines Gebäudes aufheben</b> (AD-Q15): Die Handlung steht zwischen
    /// „Duplizieren…" und „Löschen"; nach dem „Ja" ist „Schule D" ein eigener Satz, die
    /// Kenndaten sind bedienbar, „Speichern" ist frei und das Stammblatt trägt das Band.
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
    }
}
