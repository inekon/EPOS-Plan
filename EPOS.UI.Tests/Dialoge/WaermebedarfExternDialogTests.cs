using System.Globalization;
using System.Threading;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Allgemein;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Wärmebedarf extern (iU9-W9.4). Soll ist die Feldkarte von
/// <c>Form_Waermebedarf</c>: 11 Zeilen — zwei Listen, zwei Pfeile, drei Knöpfe — plus das
/// KANALFELD, das der Vorläufer zur Laufzeit anlegte
/// (<c>KanalControlsAufbauen</c>:72-116).
///
/// <para>Seit dem Anwenderwunsch <b>W9‑E‑3</b> der Windows-Abnahme vom 05.09.2026
/// kommen die vier Knöpfe der Katalogseite, der Formathinweis und die Grafik dazu —
/// wörtlich dieselben Bausteine wie im Stromganglinien-Dialog (W12‑E‑1/W12‑E‑2).</para>
///
/// <para>Die Kultur ist auf de-DE gepinnt — die Erwartungswerte sind deutsche
/// Beschriftungen.</para>
/// </summary>
public class WaermebedarfExternDialogTests : BunitContext
{
    private static readonly List<WaermebedarfAdminDialog.Katalogzeile> KATALOG = new()
    {
        new WaermebedarfAdminDialog.Katalogzeile(1, "Ganglinie A", false),
        new WaermebedarfAdminDialog.Katalogzeile(2, "Ganglinie B", true),
        new WaermebedarfAdminDialog.Katalogzeile(3, "Ganglinie C", false)
    };

    private static readonly (string Wert, string Text)[] KANAELE =
    {
        ("HEIZUNG", "Heizung"),
        ("BRAUCHWASSER", "Brauchwasser"),
        ("PROZESS", "Prozesswärme")
    };

    public WaermebedarfExternDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>
    /// Die Sprache der Oberfläche wird auf de-DE gepinnt (Hausmuster seit iU9-W8) —
    /// Kultur UND Thread-Kultur, damit ein Lauf auf einem en-US-Läufer dieselben
    /// deutschen Texte sieht. Seit W9‑E‑3 prüft diese Klasse auch formatierte
    /// Meldungen; ohne das Pinnen hinge ihr Wortlaut an der Läuferkultur.
    /// </summary>
    private static void DeutscheOberflaeche() => Kultur("de-DE");

    private static void Kultur(string name)
    {
        var k = new CultureInfo(name);
        CultureInfo.DefaultThreadCurrentCulture = k;
        CultureInfo.DefaultThreadCurrentUICulture = k;
        Thread.CurrentThread.CurrentCulture = k;
        Thread.CurrentThread.CurrentUICulture = k;
        CultureInfo.CurrentCulture = k;
        CultureInfo.CurrentUICulture = k;
    }

    private static WaermebedarfExternZeile Zeile(int idZ, string name = "Ganglinie A",
                                                 string kanal = "HEIZUNG") => new()
    {
        IdZ = idZ,
        IdGanglinie = 5,
        Bezeichner = name,
        Kanal = kanal
    };

    private IRenderedComponent<WaermebedarfExternDialog> Aufbauen(
        List<WaermebedarfExternZeile>? zeilen = null,
        bool wizard = false,
        Func<string, bool>? hatZuordnung = null,
        Func<string, bool>? katalogLoeschen = null,
        IReadOnlyDictionary<string, object>? verwaltung = null,
        Func<Task<List<WaermebedarfAdminDialog.Katalogzeile>>>? katalog = null,
        Func<string, Task<string?>>? dateiWaehlen = null,
        Func<string, GanglinienRaster, GanglinienImportRueckrufe,
             Task<GanglinienImportErgebnis>>? einlesen = null,
        Func<string, string, Task<bool>>? kopieren = null,
        Action? geaendert = null,
        Action<bool>? geschlossen = null)
        => Render<WaermebedarfExternDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<WaermebedarfExternZeile> { Zeile(1) })
            .Add(x => x.Wizard, wizard)
            .Add(x => x.Katalog, katalog ?? (() => Task.FromResult(
                new List<WaermebedarfAdminDialog.Katalogzeile>(KATALOG))))
            .Add(x => x.Aufnehmen, n => new WaermebedarfExternZeile
            {
                IdZ = 0, IdGanglinie = 9, Bezeichner = n, Kanal = "HEIZUNG"
            })
            .Add(x => x.HatProjektzuordnung, hatZuordnung ?? (_ => false))
            .Add(x => x.KatalogLoeschen, katalogLoeschen ?? (_ => true))
            .Add(x => x.VerwaltungGaben, verwaltung)
            .Add(x => x.DateiWaehlen, dateiWaehlen)
            .Add(x => x.Einlesen, einlesen)
            .Add(x => x.Kopieren, kopieren)
            .Add(x => x.Kanaele, KANAELE)
            .Add(x => x.Geaendert, geaendert)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    /// <summary>Derselbe Aufbau, aber MIT der Grafik (Kennzahlen und Bildauftrag).</summary>
    private IRenderedComponent<WaermebedarfExternDialog> ZeigeMitGrafik(
        Dictionary<string, GanglinienKennzahlen?>? kennzahlen = null,
        List<Auftrag>? auftraege = null,
        List<WaermebedarfExternZeile>? zeilen = null)
    {
        Dictionary<string, GanglinienKennzahlen?> zahlen = kennzahlen ?? new()
        {
            ["Ganglinie A"] = new GanglinienKennzahlen(1234.5, 500.0, 2469.0),
            ["Ganglinie B"] = new GanglinienKennzahlen(1000.0, 250.0, 4000.0)
        };

        return Render<WaermebedarfExternDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<WaermebedarfExternZeile> { Zeile(1) })
            .Add(x => x.Katalog, () => Task.FromResult(
                new List<WaermebedarfAdminDialog.Katalogzeile>(KATALOG)))
            .Add(x => x.Aufnehmen, n => new WaermebedarfExternZeile
            {
                IdZ = 0, IdGanglinie = 9, Bezeichner = n, Kanal = "HEIZUNG"
            })
            .Add(x => x.Kanaele, KANAELE)
            .Add(x => x.Kennzahlen, (GanglinienWahl w) =>
                Task.FromResult(zahlen.TryGetValue(w.Bezeichner, out GanglinienKennzahlen? k)
                                ? k : null))
            .Add(x => x.Bildauftrag, (GanglinienWahl w, bool sortiert, Diagrammbereich? bereich) =>
            {
                auftraege?.Add(new Auftrag(w, sortiert, bereich));
                return new byte[] { 1, 2, 3 };
            }));
    }

    private sealed record Auftrag(GanglinienWahl Wahl, bool Sortiert, Diagrammbereich? Bereich);

    private static IElement Knopf(IRenderedComponent<WaermebedarfExternDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    /// <summary>
    /// Die zwei Richtungsknöpfe stehen seit dem Anwenderentscheid #76 in der
    /// Mittelspalte des Bausteins <c>Zweispaltenauswahl</c>; ihr Zeichen ist ein
    /// eigenes Element neben dem Text, sie sind also nicht mehr über ihren
    /// Text zu finden.
    /// </summary>
    private static IElement Uebernehmen(IRenderedComponent<WaermebedarfExternDialog> cut)
        => cut.FindAll(".epos-zweispalten-mitte button")[0];

    private static IElement Entfernen(IRenderedComponent<WaermebedarfExternDialog> cut)
        => cut.FindAll(".epos-zweispalten-mitte button")[1];

    /// <summary>
    /// „OK" IM Namensdialog. Der Dialog fuehrt zwei Knoepfe dieses Namens - den der
    /// Schlussleiste und den der Ueberlagerung; ueber den Text allein traefe man
    /// immer den ersten.
    /// </summary>
    private static void OkImNamensdialog(IRenderedComponent<WaermebedarfExternDialog> cut)
        => cut.FindComponent<NamensDialog>()
              .FindAll("button").First(b => b.TextContent.Trim() == "OK").Click();

    /// <summary>Der Wahlknopf der n-ten KATALOGzeile (die Projektliste steht davor).</summary>
    private static IElement KatalogWahl(IRenderedComponent<WaermebedarfExternDialog> cut,
                                        int projektzeilen, int index)
        => cut.FindAll("button.epos-anlagenwahl")[projektzeilen + index];

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Der_Feldbestand_der_Karte_steht()
    {
        var cut = Aufbauen();

        Assert.Contains("Wärmebedarf Extern", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Wärmebedarfsdaten (Ganglinien)", cut.Markup);
        Assert.Contains("Ausgewählt im Projekt", cut.Markup);
        Assert.Contains("Wärmebedarf aus DB", cut.Markup);

        // Das Kanalfeld ist die einzige Klappliste.
        Assert.Single(cut.FindAll("select"));
        Assert.Contains("Kanal", cut.Markup);

        foreach (string t in new[] { "Löschen", "OK", "Abbrechen" })
            Assert.NotNull(Knopf(cut, t));

        // Die zwei Pfeile: Klartext statt blossem Zeichen (Entscheid #76).
        Assert.Contains("In das Projekt übernehmen", Uebernehmen(cut).TextContent);
        Assert.Contains("Aus dem Projekt entfernen", Entfernen(cut).TextContent);
    }

    [Fact]
    public void Die_Kanalliste_fuehrt_die_drei_Kanaele()
    {
        var cut = Aufbauen();

        IElement kanal = cut.Find("select");
        Assert.Equal(3, kanal.QuerySelectorAll("option").Length);
        Assert.Contains("Heizung", cut.Markup);
        Assert.Contains("Brauchwasser", cut.Markup);
        Assert.Contains("Prozesswärme", cut.Markup);
    }

    /// <summary>
    /// <b>W9‑E‑3, Punkt 4:</b> Der Kanal ist ein Parameterfeld und steht deshalb im
    /// Baustein <c>Formularraster</c> (Hausregel iU8‑E‑2) — Beschriftung NEBEN dem
    /// Feld. Er bleibt in der LINKEN Spalte: Er gehört zur Zuordnung, nicht zum
    /// Katalog (Regel des Bausteins <c>Zweispaltenauswahl</c>).
    /// </summary>
    [Fact]
    public void Das_Kanalfeld_steht_im_Formularraster_der_linken_Spalte()
    {
        var cut = Aufbauen();

        IElement raster = cut.Find(".epos-zweispalten-spalte--links .epos-formularraster");
        Assert.NotNull(raster.QuerySelector("select"));
    }

    [Fact]
    public void Im_Assistenten_gibt_es_keine_Schlussleiste()
    {
        var cut = Aufbauen(wizard: true);

        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "OK");
    }

    /// <summary>Ohne Parametersatz der Verwaltung kein „Bearbeiten…"-Knopf.</summary>
    [Fact]
    public void Ohne_Verwaltung_gibt_es_keinen_Bearbeitenknopf()
    {
        Assert.DoesNotContain("Bearbeiten...", Aufbauen().Markup);
        Assert.Contains("Bearbeiten...",
                        Aufbauen(verwaltung: new Dictionary<string, object>()).Markup);
    }

    // =================================================================================
    // W9-E-3: Die Knopfleiste unter der Katalogliste
    // =================================================================================

    /// <summary>
    /// Die vier Knöpfe in ihrer Reihenfolge — dieselbe wie im Stromdialog:
    /// Importieren, Speichern unter, Löschen, Bearbeiten.
    /// </summary>
    [Fact]
    public void Die_Katalogleiste_traegt_vier_Knoepfe_in_dieser_Reihenfolge()
    {
        var cut = Aufbauen(
            verwaltung: new Dictionary<string, object>(),
            dateiWaehlen: _ => Task.FromResult<string?>(""),
            einlesen: (_, __, ___) => Task.FromResult(new GanglinienImportErgebnis()),
            kopieren: (_, __) => Task.FromResult(true));

        var texte = cut.FindAll(".epos-zweispalten-spalte--rechts .epos-leiste button")
                       .Select(b => b.TextContent.Trim()).ToList();

        Assert.Equal(new[]
        {
            "CSV-Datei importieren...", "Speichern unter...",
            "Löschen", "Bearbeiten..."
        }, texte);
    }

    /// <summary>
    /// <b>Kein Delegat, kein Knopf.</b> „CSV-Datei importieren…" braucht BEIDES —
    /// den Dateiwähler und die Kette; „Speichern unter…" den Kopierweg.
    /// </summary>
    [Fact]
    public void Ohne_Delegaten_bleibt_der_jeweilige_Knopf_weg()
    {
        Assert.DoesNotContain("CSV-Datei importieren...", Aufbauen().Markup);
        Assert.DoesNotContain("Speichern unter...", Aufbauen().Markup);

        // Halbfall: Waehler ohne Kette - immer noch kein Importknopf.
        Assert.DoesNotContain("CSV-Datei importieren...",
            Aufbauen(dateiWaehlen: _ => Task.FromResult<string?>("")).Markup);

        Assert.Contains("Speichern unter...",
            Aufbauen(kopieren: (_, __) => Task.FromResult(true)).Markup);
    }

    /// <summary>
    /// Der Formathinweis steht einzeilig unter der Leiste, der volle Wortlaut hängt
    /// am Infoknopf daneben. Er nennt genau das, was die Kette wirklich auswertet.
    /// </summary>
    [Fact]
    public void Der_Formathinweis_nennt_die_Angaben_und_haengt_am_Infoknopf()
    {
        var cut = Aufbauen(
            dateiWaehlen: _ => Task.FromResult<string?>(""),
            einlesen: (_, __, ___) => Task.FromResult(new GanglinienImportErgebnis()));

        IElement hinweis = cut.Find(".epos-formathinweis");
        Assert.Contains("8.760 Stunden- oder 35.040 Viertelstundenwerte", hinweis.TextContent);
        Assert.Contains("ein Wert je Zeile", hinweis.TextContent);

        IElement knopf = hinweis.QuerySelector("button")!;
        string kurztext = knopf.GetAttribute("title") ?? "";
        Assert.Contains("Semikolon, Komma, Tabulator", kurztext);
        Assert.Contains("Leistung in kW oder als Arbeit in kWh", kurztext);
        Assert.Contains("Kopfzeile", kurztext);
        Assert.Contains("Dezimaltrennzeichen Komma oder Punkt", kurztext);
    }

    /// <summary>
    /// <b>W13‑B‑1:</b> Der Dateiwähler DARF warten; die Kette läuft erst danach —
    /// und ohne Rastervorgabe, damit die Erkennung entscheidet.
    /// </summary>
    [Fact]
    public void Der_Dateiwaehler_darf_warten_und_die_Kette_laeuft_danach()
    {
        var wartet = new TaskCompletionSource<string?>();
        string? gelesen = null;
        GanglinienRaster raster = GanglinienRaster.Stunde;

        var cut = Aufbauen(
            dateiWaehlen: _ => wartet.Task,
            einlesen: (p, r, __) =>
            {
                gelesen = p;
                raster = r;
                return Task.FromResult(new GanglinienImportErgebnis());
            });

        Knopf(cut, "CSV-Datei importieren...").Click();
        Assert.Null(gelesen);

        wartet.SetResult(@"D:\quelle\jahr.csv");
        cut.WaitForAssertion(() => Assert.Equal(@"D:\quelle\jahr.csv", gelesen));
        Assert.Equal(GanglinienRaster.Unbekannt, raster);
    }

    /// <summary>Ein abgebrochener Wähler liest nichts.</summary>
    [Fact]
    public void Ein_abgebrochener_Waehler_liest_nichts()
    {
        bool gerufen = false;
        var cut = Aufbauen(
            dateiWaehlen: _ => Task.FromResult<string?>(""),
            einlesen: (_, __, ___) =>
            {
                gerufen = true;
                return Task.FromResult(new GanglinienImportErgebnis());
            });

        Knopf(cut, "CSV-Datei importieren...").Click();

        Assert.False(gerufen);
    }

    // =================================================================================
    // Kanal
    // =================================================================================

    [Fact]
    public void Ohne_markierte_Zeile_ist_die_Kanalliste_gesperrt()
    {
        var cut = Aufbauen(zeilen: new List<WaermebedarfExternZeile>());

        Assert.True(cut.Find("select").HasAttribute("disabled"));
    }

    [Fact]
    public void Die_Kanalwahl_wirkt_auf_die_MARKIERTE_Zeile()
    {
        var zeilen = new List<WaermebedarfExternZeile> { Zeile(1), Zeile(2, "Ganglinie B") };
        var cut = Aufbauen(zeilen: zeilen);

        cut.FindAll("button.epos-anlagenwahl")[1].Click();   // zweite Projektzeile
        cut.Find("select").Change("1");                      // Brauchwasser

        Assert.Equal("HEIZUNG", zeilen[0].Kanal);
        Assert.Equal("BRAUCHWASSER", zeilen[1].Kanal);
    }

    [Fact]
    public void Ein_unbekannter_Kanal_faellt_auf_Heizung_zurueck()
    {
        var cut = Aufbauen(zeilen: new List<WaermebedarfExternZeile> { Zeile(1, kanal: "XYZ") });

        Assert.Equal("0", cut.Find("select").GetAttribute("value"));
    }

    // =================================================================================
    // Uebernehmen und Entfernen
    // =================================================================================

    [Fact]
    public void Der_Pfeil_nach_links_legt_eine_neue_Zeile_auf_Heizung_an()
    {
        bool gemeldet = false;
        var zeilen = new List<WaermebedarfExternZeile>();
        var cut = Aufbauen(zeilen: zeilen, geaendert: () => gemeldet = true);

        cut.FindAll("button.epos-anlagenwahl").First().Click();   // erste Katalogzeile
        Uebernehmen(cut).Click();

        Assert.Single(zeilen);
        Assert.Equal("HEIZUNG", zeilen[0].Kanal);
        Assert.Equal("Ganglinie A", zeilen[0].Bezeichner);
        Assert.True(gemeldet);
        Assert.Same(zeilen[0], cut.Instance.Gewaehlt);
    }

    /// <summary>
    /// „▶" trifft die MARKIERTE Zeile. Der Vorläufer nahm die erste Zeile gleichen
    /// Namens — bei zwei Zuordnungen derselben Ganglinie die falsche (A-9).
    /// </summary>
    [Fact]
    public void Der_Pfeil_nach_rechts_trifft_die_markierte_Zeile()
    {
        var zeilen = new List<WaermebedarfExternZeile>
        {
            Zeile(1, "Ganglinie A", "HEIZUNG"),
            Zeile(2, "Ganglinie A", "PROZESS")
        };
        var cut = Aufbauen(zeilen: zeilen);

        cut.FindAll("button.epos-anlagenwahl")[1].Click();
        Entfernen(cut).Click();

        Assert.Single(zeilen);
        Assert.Equal(1, zeilen[0].IdZ);
        Assert.Equal("HEIZUNG", zeilen[0].Kanal);
    }

    // =================================================================================
    // Katalog loeschen
    // =================================================================================

    [Fact]
    public void Loeschen_mit_Projektzuordnung_meldet_den_Grund()
    {
        bool geloescht = false;
        var cut = Aufbauen(hatZuordnung: _ => true,
                           katalogLoeschen: _ => { geloescht = true; return true; });

        KatalogWahl(cut, 1, 0).Click();
        Knopf(cut, "Löschen").Click();

        Assert.False(geloescht);
        Assert.Contains("Projektzuordnung", cut.Instance.Meldung);
        Assert.DoesNotContain("wirklich gelöscht", cut.Markup);
    }

    /// <summary>
    /// <b>W9‑E‑3:</b> Ein Auslieferungssatz bleibt stehen und nennt seinen Grund
    /// schon am Knopf — bis dahin kannte dieser Dialog das Kennzeichen gar nicht,
    /// und der Controller zeigte eine <c>MessageBox</c>.
    /// </summary>
    [Fact]
    public void Ein_Auslieferungssatz_bleibt_stehen_und_nennt_den_Grund()
    {
        bool geloescht = false;
        var cut = Aufbauen(katalogLoeschen: _ => { geloescht = true; return true; });

        KatalogWahl(cut, 1, 1).Click();     // "Ganglinie B" traegt ReadOnly
        IElement knopf = Knopf(cut, "Löschen");
        Assert.Contains("schreibgeschützt", knopf.GetAttribute("title") ?? "");

        knopf.Click();

        Assert.False(geloescht);
        Assert.Contains("schreibgeschützt", cut.Instance.Meldung);
        Assert.DoesNotContain("wirklich gelöscht", cut.Markup);
    }

    /// <summary>Der Vorläufer löschte auf einen Klick; jetzt wird gefragt (A-8).</summary>
    [Fact]
    public void Loeschen_ohne_Projektzuordnung_fragt_nach()
    {
        string geloescht = "";
        var cut = Aufbauen(katalogLoeschen: n => { geloescht = n; return true; });

        KatalogWahl(cut, 1, 0).Click();
        Knopf(cut, "Löschen").Click();

        Assert.Contains("wirklich gelöscht", cut.Markup);
        Knopf(cut, "Ja").Click();

        Assert.Equal("Ganglinie A", geloescht);
        Assert.Contains("gelöscht", cut.Instance.Meldung);
    }

    [Fact]
    public void Loeschen_mit_Nein_laesst_alles_stehen()
    {
        bool gerufen = false;
        var cut = Aufbauen(katalogLoeschen: _ => { gerufen = true; return true; });

        KatalogWahl(cut, 1, 0).Click();
        Knopf(cut, "Löschen").Click();
        Knopf(cut, "Nein").Click();

        Assert.False(gerufen);
    }

    // =================================================================================
    // W9-E-3: Speichern unter
    // =================================================================================

    [Fact]
    public void Speichern_unter_ist_ohne_Auswahl_gesperrt()
    {
        var cut = Aufbauen(kopieren: (_, __) => Task.FromResult(true));

        Assert.True(Knopf(cut, "Speichern unter...").HasAttribute("disabled"));
    }

    [Fact]
    public void Speichern_unter_schlaegt_den_Namen_mit_Kopie_vor()
    {
        var cut = Aufbauen(kopieren: (_, __) => Task.FromResult(true));

        KatalogWahl(cut, 1, 0).Click();
        Knopf(cut, "Speichern unter...").Click();

        Assert.Contains("Wärmebedarfsganglinie speichern unter", cut.Markup);
        Assert.Equal("Ganglinie A - Kopie",
                     cut.FindComponent<NamensDialog>().Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Ein_vergebener_Name_haelt_den_Namensdialog_offen()
    {
        bool gerufen = false;
        var cut = Aufbauen(kopieren: (_, __) => { gerufen = true; return Task.FromResult(true); });

        KatalogWahl(cut, 1, 0).Click();
        Knopf(cut, "Speichern unter...").Click();

        var namensdialog = cut.FindComponent<NamensDialog>();
        namensdialog.Find("input").Input("Ganglinie C");        // gibt es schon
        namensdialog.FindAll("button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.False(gerufen);
        Assert.Single(cut.FindComponents<NamensDialog>());
        Assert.Contains("bereits in der Datenbank", namensdialog.Markup);
    }

    [Fact]
    public void Ein_freier_Name_legt_die_Kopie_an()
    {
        string quelle = "", ziel = "";
        var cut = Aufbauen(kopieren: (q, z) => { quelle = q; ziel = z; return Task.FromResult(true); });

        KatalogWahl(cut, 1, 0).Click();
        Knopf(cut, "Speichern unter...").Click();
        OkImNamensdialog(cut);

        Assert.Equal("Ganglinie A", quelle);
        Assert.Equal("Ganglinie A - Kopie", ziel);
        Assert.Contains("wurde als", cut.Instance.Meldung);
    }

    [Fact]
    public void Eine_gescheiterte_Kopie_meldet_sich()
    {
        var cut = Aufbauen(kopieren: (_, __) => Task.FromResult(false));

        KatalogWahl(cut, 1, 0).Click();
        Knopf(cut, "Speichern unter...").Click();
        OkImNamensdialog(cut);

        Assert.Contains("konnte nicht angelegt werden", cut.Instance.Meldung);
    }

    // =================================================================================
    // W9-E-3: Die Grafik
    // =================================================================================

    /// <summary>Ohne Markierung gibt es keine Grafik — auch nicht mit Delegaten.</summary>
    [Fact]
    public void Ohne_Markierung_steht_keine_Grafik()
    {
        var cut = ZeigeMitGrafik(zeilen: new List<WaermebedarfExternZeile>());

        Assert.Null(cut.Instance.Grafikkennzahlen);
        Assert.Empty(cut.FindAll(".epos-ganglinie-grafik"));
    }

    /// <summary>
    /// Eine markierte KATALOGzeile bringt die Grafik — mit den Kennzahlen und dem
    /// Bild aus dem Kern.
    /// </summary>
    [Fact]
    public void Eine_markierte_Katalogzeile_bringt_die_Grafik()
    {
        var auftraege = new List<Auftrag>();
        var cut = ZeigeMitGrafik(auftraege: auftraege);

        KatalogWahl(cut, 1, 1).Click();      // "Ganglinie B"

        Assert.NotNull(cut.Instance.Grafikkennzahlen);
        Assert.True(cut.Instance.Grafikwahl!.AusKatalog);
        Assert.Equal("Ganglinie B", cut.Instance.Grafikwahl.Bezeichner);
        Assert.Single(cut.FindAll(".epos-ganglinie-grafik"));
        Assert.Contains("Wärmelast Jahresganglinie", cut.Markup);

        Assert.NotEmpty(auftraege);
        Assert.False(auftraege[0].Sortiert);
        Assert.Null(auftraege[0].Bereich);
    }

    /// <summary>
    /// <b>Die Id gilt nur für eine GESPEICHERTE Zuordnung.</b> Eine eben erst
    /// aufgenommene Zeile trägt die STAMM-Id, nicht die der Projektkopie — sie darf
    /// deshalb nicht als Kopie-Id gelesen werden, sonst zeigte die Grafik eine
    /// FREMDE Ganglinie.
    /// </summary>
    [Fact]
    public void Eine_neu_aufgenommene_Zeile_wird_ueber_den_Katalog_gelesen()
    {
        var zeilen = new List<WaermebedarfExternZeile>();
        var cut = ZeigeMitGrafik(zeilen: zeilen);

        cut.FindAll("button.epos-anlagenwahl").First().Click();   // erste Katalogzeile
        Uebernehmen(cut).Click();

        Assert.NotNull(cut.Instance.Grafikwahl);
        Assert.False(cut.Instance.Grafikwahl!.AusKatalog);
        Assert.Equal(0, cut.Instance.Grafikwahl.GanglinieId);      // = Rueckfall ueber den Namen
        Assert.Equal("Ganglinie A", cut.Instance.Grafikwahl.Bezeichner);
    }

    /// <summary>
    /// Eine gespeicherte Projektzeile wird über ihre KOPIE-Id gelesen — genau
    /// dieselbe Reihe, die der Lauf liest.
    /// </summary>
    [Fact]
    public void Eine_gespeicherte_Projektzeile_wird_ueber_die_Kopie_gelesen()
    {
        var cut = ZeigeMitGrafik();

        cut.FindAll("button.epos-anlagenwahl").First().Click();   // die Projektzeile

        Assert.NotNull(cut.Instance.Grafikwahl);
        Assert.False(cut.Instance.Grafikwahl!.AusKatalog);
        Assert.Equal(5, cut.Instance.Grafikwahl.GanglinieId);
    }

    /// <summary>Der Schalter „sortiert" zeichnet neu — als Dauerlinie.</summary>
    [Fact]
    public void Der_Schalter_sortiert_zeichnet_neu()
    {
        var auftraege = new List<Auftrag>();
        var cut = ZeigeMitGrafik(auftraege: auftraege);

        KatalogWahl(cut, 1, 0).Click();
        auftraege.Clear();

        cut.Find(".epos-ganglinie-leiste input[type='checkbox']").Change(true);

        Assert.Contains(auftraege, a => a.Sortiert);
    }

    /// <summary>Ohne brauchbare Reihe bleibt die Grafik weg statt leer zu stehen.</summary>
    [Fact]
    public void Ohne_brauchbare_Reihe_bleibt_die_Grafik_weg()
    {
        var cut = ZeigeMitGrafik(kennzahlen: new Dictionary<string, GanglinienKennzahlen?>());

        KatalogWahl(cut, 1, 0).Click();

        Assert.Null(cut.Instance.Grafikkennzahlen);
        Assert.Empty(cut.FindAll(".epos-ganglinie-grafik"));
    }

    /// <summary>
    /// iU9-W13.2: „Bearbeiten…" zeigt die Ganglinienverwaltung als
    /// ÜBERLAGERUNG im selben Fenster. Bis Welle 13 war es ein Sprung über die
    /// <c>Sprungbruecke</c> in ein WinForms-Fenster; ist das Ziel selbst Blazor,
    /// wären zwei WebViews übereinander Risiko R2.
    /// </summary>
    [Fact]
    public void Bearbeiten_zeigt_die_Ganglinienverwaltung_als_Ueberlagerung()
    {
        var cut = Aufbauen(verwaltung: new Dictionary<string, object>());

        Assert.Empty(cut.FindAll("[role='dialog']"));

        Knopf(cut, "Bearbeiten...").Click();

        Assert.Single(cut.FindAll("[role='dialog']"));
        Assert.Contains("Wärmebedarf Ganglinie", cut.Markup);
    }

    // =================================================================================
    // Tastatur und Schlussleiste
    // =================================================================================

    [Fact]
    public void Esc_schliesst_mit_Abbruch()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.False(ergebnis);
    }

    /// <summary>Esc schließt zuerst die Rückfrage, nicht den Dialog.</summary>
    [Fact]
    public void Esc_laesst_die_untere_Ebene_stehen()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        KatalogWahl(cut, 1, 0).Click();
        Knopf(cut, "Löschen").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Null(ergebnis);
    }

    [Fact]
    public void OK_meldet_true()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        Knopf(cut, "OK").Click();

        Assert.True(ergebnis);
    }

    // =================================================================================
    // Die Anordnung - Anwenderentscheid #76 vom 05.09.2026
    // =================================================================================

    /// <summary>
    /// Der Anwender hat nach der Windows-Abnahme entschieden, dass auch dieser Dialog
    /// dem BHKW-PLAN-Schema folgt: Projektliste LINKS, Katalog RECHTS, die zwei
    /// Pfeilknöpfe in einer schmalen Mittelspalte dazwischen. Bis dahin standen die
    /// beiden Listen untereinander und die Knöpfe unter der Projektliste.
    /// </summary>
    [Fact]
    public void Projektliste_links_Katalog_rechts_Pfeile_dazwischen()
    {
        var cut = Aufbauen();

        var bereiche = cut.FindAll(".epos-zweispalten > div")
                          .Select(e => e.ClassName ?? "").ToList();

        Assert.Equal(3, bereiche.Count);
        Assert.Contains("epos-zweispalten-spalte--links", bereiche[0]);
        Assert.Contains("epos-zweispalten-mitte", bereiche[1]);
        Assert.Contains("epos-zweispalten-spalte--rechts", bereiche[2]);

        // Beide Listen stehen weiterhin in ihrem Rahmen (Befund W9-B-2).
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-spalte .epos-raster-huelle").Count);
    }

    // =================================================================================
    // Die zweite Sprache
    // =================================================================================

    /// <summary>
    /// Auf einem englischen Läufer stehen die englischen Texte — kein Schlüssel ist
    /// beim Einhängen der neuen Knöpfe deutsch hängengeblieben. Die Kultur wird
    /// danach wieder auf de-DE gestellt, damit die übrigen Fälle der Klasse ihre
    /// Erwartung behalten.
    /// </summary>
    [Fact]
    public void In_englischer_Oberflaeche_stehen_die_englischen_Texte()
    {
        try
        {
            Kultur("en-US");

            var cut = Aufbauen(
                dateiWaehlen: _ => Task.FromResult<string?>(""),
                einlesen: (_, __, ___) => Task.FromResult(new GanglinienImportErgebnis()),
                kopieren: (_, __) => Task.FromResult(true));

            Assert.Contains("Import CSV file...", cut.Markup);
            Assert.Contains("Save as...", cut.Markup);
            Assert.Contains("8,760 hourly or 35,040 quarter-hourly values", cut.Markup);
        }
        finally
        {
            DeutscheOberflaeche();
        }
    }

    // =================================================================================
    // Die Knopftexte - Anwenderentscheid W9-O-9 vom 06.09.2026
    // =================================================================================

    /// <summary>
    /// <b>W9‑O‑9 (06.09.2026), wörtlich:</b> „Knopftexte im Dialog ‚Wärmebedarf
    /// Extern' wortgleich zum Stromdialog". „Einlesen/Bearbeiten.." heißt seither
    /// „Bearbeiten…", „DB Ganglinie löschen" heißt „Löschen" — und zwar nicht als
    /// abgeschriebene Wörter, sondern über DENSELBEN Schlüssel
    /// (<c>STROMGL_BTN_BEARBEITEN</c>/<c>STROMGL_BTN_LOESCHEN</c>), damit die zwei
    /// Dialoge beim nächsten Textwechsel nicht wieder auseinanderlaufen. Der Zeuge
    /// steht in BEIDEN Sprachen; die Kultur wird danach zurückgestellt.
    /// </summary>
    [Theory]
    [InlineData("de-DE", "Bearbeiten...", "Löschen")]
    [InlineData("en-US", "Edit...", "Delete")]
    public void Bearbeiten_und_Loeschen_tragen_die_Stromtexte(
        string kultur, string bearbeiten, string loeschen)
    {
        try
        {
            Kultur(kultur);

            var cut = Aufbauen(verwaltung: new Dictionary<string, object>());

            // Der Wortlaut ...
            Assert.NotNull(Knopf(cut, bearbeiten));
            Assert.NotNull(Knopf(cut, loeschen));

            // ... und die Quelle: es ist der Schluessel des Stromdialogs.
            Assert.Equal(Resource.STROMGL_BTN_BEARBEITEN, bearbeiten);
            Assert.Equal(Resource.STROMGL_BTN_LOESCHEN, loeschen);

            // Die zwei alten Waermetexte stehen nirgends mehr.
            Assert.DoesNotContain("Einlesen/Bearbeiten..", cut.Markup);
            Assert.DoesNotContain("DB Ganglinie löschen", cut.Markup);
            Assert.DoesNotContain("Reading/Editing..", cut.Markup);
            Assert.DoesNotContain("Delete DB curve", cut.Markup);
        }
        finally
        {
            DeutscheOberflaeche();
        }
    }

    /// <summary>
    /// <b>W9‑O‑9:</b> Die Rückfrage vor dem Löschen trägt den DIALOGTITEL wie beim
    /// Strom (<c>StromganglinieDialog</c>:249). Bis dahin stand dort der Knopftext;
    /// mit dem kurzen „Löschen" wäre die Überschrift nichtssagend geworden.
    /// </summary>
    [Fact]
    public void Die_Loeschrueckfrage_traegt_den_Dialogtitel()
    {
        var cut = Aufbauen(katalogLoeschen: _ => true);

        KatalogWahl(cut, 1, 0).Click();
        Knopf(cut, "Löschen").Click();

        Assert.Equal("Wärmebedarf Extern",
                     cut.Find("[role='dialog'] .epos-ueberlagerung-titel").TextContent.Trim());
    }
}
