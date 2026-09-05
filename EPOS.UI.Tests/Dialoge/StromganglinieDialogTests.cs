using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Zuordnung von Stromganglinien zum Projekt (iU9-W12.5), Vorbild
/// <c>Views/Stromverbraucher/Form_Stromganglinie</c>.
///
/// <para>Soll ist die Feldkarte (7 Zeilen): zwei Listen, „◀"/„▶", „Bearbeiten…",
/// „OK", „Abbrechen". Geprüft werden dazu die A-Zeile (Entfernen über die ZEILE,
/// nicht über den Namen), der wörtlich übernommene Befund W12-B5 (keine
/// Dublettenprüfung) und die Verwaltung als Überlagerung.</para>
/// </summary>
public class StromganglinieDialogTests : BunitContext
{
    public StromganglinieDialogTests()
    {
        DeutscheOberflaeche();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>
    /// Die Sprache der Oberfläche wird auf de-DE gepinnt (Hausmuster seit iU9-W8) —
    /// Kultur UND Thread-Kultur, damit ein Lauf auf einem en-US-Läufer dieselben
    /// deutschen Texte sieht. Seit W12-E-1 prüft diese Klasse auch formatierte
    /// Meldungen; ohne das Pinnen hinge ihr Wortlaut an der Läuferkultur.
    /// </summary>
    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
    }

    private static List<GanglinienKatalogZeile> Katalog() => new()
    {
        new GanglinienKatalogZeile("Werk Nord", 4, false),
        new GanglinienKatalogZeile("Auslieferung", 1, true)
    };

    /// <summary>Ein Satz Gaben für die Verwaltung — mehr als „nicht null" braucht der Test nicht.</summary>
    private static IReadOnlyDictionary<string, object> VerwaltungsGaben() =>
        new Dictionary<string, object>
        {
            ["Katalog"] = new Func<Task<List<GanglinienKatalogZeile>>>(
                () => Task.FromResult(Katalog()))
        };

    private IRenderedComponent<StromganglinieDialog> Zeige(
        List<GanglinienProjektZeile>? zeilen = null,
        Func<Task<List<GanglinienKatalogZeile>>>? katalog = null,
        IReadOnlyDictionary<string, object>? verwaltung = null,
        bool wizard = false,
        Action<bool>? geschlossen = null,
        Action? geaendert = null)
    {
        return Render<StromganglinieDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<GanglinienProjektZeile>())
            .Add(x => x.Katalog, katalog ?? (() => Task.FromResult(Katalog())))
            .Add(x => x.Verwaltung, verwaltung ?? VerwaltungsGaben())
            .Add(x => x.Wizard, wizard)
            .Add(x => x.Geaendert, geaendert)
            .Add(x => x.Geschlossen, (bool ok) => geschlossen?.Invoke(ok)));
    }

    /// <summary>
    /// Der Dialog MIT der Datenbankseite (W12-E-1): Importieren, Speichern unter,
    /// Löschen. Ein nicht gesetzter Delegat heißt „diesen Knopf gibt es nicht" —
    /// deshalb sind die Vorgaben hier gesetzt und nicht null.
    /// </summary>
    private IRenderedComponent<StromganglinieDialog> ZeigeMitKatalogpflege(
        List<GanglinienKatalogZeile>? katalog = null,
        Func<string, Task<bool>>? loeschen = null,
        Func<string, Task<bool>>? zuordnung = null,
        Func<string, string, Task<bool>>? kopieren = null,
        Func<string, Task<string?>>? waehlen = null,
        Func<string, GanglinienRaster, GanglinienImportRueckrufe,
             Task<GanglinienImportErgebnis>>? einlesen = null)
    {
        List<GanglinienKatalogZeile> liste = katalog ?? Katalog();

        return Render<StromganglinieDialog>(p => p
            .Add(x => x.Zeilen, new List<GanglinienProjektZeile>())
            .Add(x => x.Katalog, () => Task.FromResult(liste))
            .Add(x => x.Verwaltung, VerwaltungsGaben())
            .Add(x => x.Loeschen, loeschen ?? (n => Task.FromResult(true)))
            .Add(x => x.HatProjektzuordnung, zuordnung ?? (n => Task.FromResult(false)))
            .Add(x => x.Kopieren, kopieren ?? ((q, z) => Task.FromResult(true)))
            .Add(x => x.DateiWaehlen, waehlen ?? (f => Task.FromResult<string?>(null)))
            .Add(x => x.Einlesen, einlesen ?? ((pf, r, rr) =>
                Task.FromResult(new GanglinienImportErgebnis { Ausgang = ImportAusgang.Abgebrochen }))));
    }

    /// <summary>Die Knopfleiste unter der Katalogliste.</summary>
    private static IHtmlCollection<IElement> Katalogknoepfe(
        IRenderedComponent<StromganglinieDialog> cut)
        => Spalte(cut, 1).QuerySelectorAll(".epos-leiste button");

    private static IElement Katalogknopf(IRenderedComponent<StromganglinieDialog> cut, string text)
        => Katalogknoepfe(cut).First(b => b.TextContent.Contains(text, StringComparison.Ordinal));

    private static IElement Spalte(IRenderedComponent<StromganglinieDialog> cut, int i)
        => cut.FindAll(".epos-zweispalten-spalte")[i];

    private static IHtmlCollection<IElement> Zeilen(IRenderedComponent<StromganglinieDialog> cut, int spalte)
        => Spalte(cut, spalte).QuerySelectorAll("tbody tr");

    private static void Waehle(IRenderedComponent<StromganglinieDialog> cut, int spalte, int zeile)
        => Zeilen(cut, spalte)[zeile].QuerySelector("button")!.Click();

    private static IElement Hinzu(IRenderedComponent<StromganglinieDialog> cut)
        => cut.FindAll(".epos-zweispalten-mitte button")[0];

    private static IElement Entfernen(IRenderedComponent<StromganglinieDialog> cut)
        => cut.FindAll(".epos-zweispalten-mitte button")[1];

    private static IElement Fussknopf(IRenderedComponent<StromganglinieDialog> cut, int i)
        => cut.FindAll(".epos-dialog > .epos-leiste button")[i];

    // =====================================================================
    // Feldbestand (Feldkarte Form_Stromganglinie, 7 Zeilen)
    // =====================================================================

    [Fact]
    public void Der_Dialog_zeigt_zwei_Listen_zwei_Pfeile_und_drei_Knoepfe()
    {
        var cut = Zeige();

        Assert.Equal("Stromganglinien", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-spalte").Count);
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-mitte button").Count);
        // Entscheid #76: Jeder Knopf traegt BEIDE Zeichen im Markup - das Stilblatt
        // zeigt je nach Anordnung eines davon - und dazu seine Aufgabe im Klartext.
        Assert.Equal("◀", cut.FindAll(".epos-zweispalten-mitte button")[0].QuerySelector(".epos-zweispalten-pfeil--breit")!.TextContent);
        Assert.Equal("▲", cut.FindAll(".epos-zweispalten-mitte button")[0].QuerySelector(".epos-zweispalten-pfeil--schmal")!.TextContent);
        Assert.Equal("▶", cut.FindAll(".epos-zweispalten-mitte button")[1].QuerySelector(".epos-zweispalten-pfeil--breit")!.TextContent);
        Assert.Equal("▼", cut.FindAll(".epos-zweispalten-mitte button")[1].QuerySelector(".epos-zweispalten-pfeil--schmal")!.TextContent);
        Assert.Equal("In das Projekt übernehmen",
                     cut.FindAll(".epos-zweispalten-mitte button")[0].QuerySelector(".epos-zweispalten-knopftext")!.TextContent);
        Assert.Equal("Aus dem Projekt entfernen",
                     cut.FindAll(".epos-zweispalten-mitte button")[1].QuerySelector(".epos-zweispalten-knopftext")!.TextContent);

        // "Bearbeiten..." steht unter der Katalogliste, OK und Abbrechen im Fuss.
        // OHNE die Gaben der Katalogpflege (W12-E-1) ist es der EINZIGE Knopf dort —
        // kein Delegat, kein Knopf.
        Assert.Single(Katalogknoepfe(cut));
        Assert.Contains("Bearbeiten", Katalogknoepfe(cut)[0].TextContent);
        Assert.Equal(2, cut.FindAll(".epos-dialog > .epos-leiste button").Count);

        Assert.Contains("Ausgewählt im Projekt", Spalte(cut, 0).QuerySelector("h2")!.TextContent);
        Assert.Contains("Stromganglinie aus DB", Spalte(cut, 1).QuerySelector("h2")!.TextContent);
    }

    /// <summary>
    /// Der Katalog kommt über den Delegaten und steht nach dem ersten Zeichnen —
    /// der Vorläufer las ihn im Konstruktor (<c>StromganglinieStammCtrl.ReadAll</c>).
    /// </summary>
    [Fact]
    public void Der_Katalog_wird_beim_Aufbau_geladen()
    {
        var cut = Zeige();

        Assert.Equal(2, Zeilen(cut, 1).Length);
        Assert.Contains("Werk Nord", Zeilen(cut, 1)[0].TextContent);
        Assert.Empty(Zeilen(cut, 0));
    }

    /// <summary>Ohne Gaben bleibt „Bearbeiten…" weg — dieselbe Regel wie bei Dateiwahl und Sprung.</summary>
    [Fact]
    public void Ohne_Verwaltung_bleibt_der_Bearbeiten_Knopf_weg()
    {
        var cut = Render<StromganglinieDialog>(p => p
            .Add(x => x.Zeilen, new List<GanglinienProjektZeile>())
            .Add(x => x.Katalog, () => Task.FromResult(Katalog())));

        Assert.Null(Spalte(cut, 1).QuerySelector(".epos-leiste button"));
    }

    // =====================================================================
    // Hinzufuegen und Entfernen
    // =====================================================================

    /// <summary>
    /// Beide Pfeile sind gesperrt, solange in ihrer Quellspalte nichts markiert ist.
    /// </summary>
    [Fact]
    public void Die_Pfeile_bleiben_ohne_Markierung_gesperrt()
    {
        var cut = Zeige();

        Assert.True(Hinzu(cut).HasAttribute("disabled"));
        Assert.True(Entfernen(cut).HasAttribute("disabled"));

        Waehle(cut, 1, 0);
        Assert.False(Hinzu(cut).HasAttribute("disabled"));
        Assert.True(Entfernen(cut).HasAttribute("disabled"));
    }

    /// <summary>
    /// „◀" legt die Zuordnung an — mit dem Zähler ab 100000 des Vorläufers („noch
    /// nicht gespeichert, also noch unbekannt") und ohne Ganglinien-Id; die holt die
    /// Hülle beim Zurückschreiben über den Bezeichner.
    /// </summary>
    [Fact]
    public void Hinzufuegen_legt_die_Zuordnung_mit_dem_Schluessel_ab_100000_an()
    {
        List<GanglinienProjektZeile> liste = new();
        var cut = Zeige(liste);

        Waehle(cut, 1, 0);
        Hinzu(cut).Click();

        Assert.Single(liste);
        Assert.Equal(StromganglinieDialog.StartIndex, liste[0].Schluessel);
        Assert.Equal(0, liste[0].GanglinieId);
        Assert.Equal("Werk Nord", liste[0].Bezeichner);
        Assert.Single(Zeilen(cut, 0));
    }

    /// <summary>
    /// <b>Befund W12-B5, wörtlich übernommen.</b> Der Vorläufer prüfte beim
    /// Hinzufügen nicht auf Dubletten; derselbe Katalogeintrag geht beliebig oft.
    /// Ob das so bleiben soll, ist eine Anwenderfrage.
    /// </summary>
    [Fact]
    public void Derselbe_Katalogeintrag_laesst_sich_mehrfach_zuordnen()
    {
        List<GanglinienProjektZeile> liste = new();
        var cut = Zeige(liste);

        Waehle(cut, 1, 0);
        Hinzu(cut).Click();
        Waehle(cut, 1, 0);
        Hinzu(cut).Click();

        Assert.Equal(2, liste.Count);
        Assert.Equal(StromganglinieDialog.StartIndex, liste[0].Schluessel);
        Assert.Equal(StromganglinieDialog.StartIndex + 1, liste[1].Schluessel);
    }

    /// <summary>
    /// <b>A-Zeile.</b> „▶" entfernt die GEWÄHLTE Zeile. Der Vorläufer suchte die
    /// erste Zeile mit demselben Namen (<c>btn_Entfernen_Click</c> :89) — bei zwei
    /// gleich benannten Zuordnungen also nicht die markierte.
    /// </summary>
    [Fact]
    public void Entfernen_trifft_die_gewaehlte_Zeile_und_nicht_den_ersten_Namensvetter()
    {
        List<GanglinienProjektZeile> liste = new()
        {
            new GanglinienProjektZeile(7, 3, "Werk Nord"),
            new GanglinienProjektZeile(8, 3, "Werk Nord")
        };
        var cut = Zeige(liste);

        Waehle(cut, 0, 1);
        Entfernen(cut).Click();

        Assert.Single(liste);
        Assert.Equal(7, liste[0].Schluessel);
    }

    /// <summary>Eine Wahl in der einen Spalte hebt die Wahl in der anderen auf.</summary>
    [Fact]
    public void Die_beiden_Listen_teilen_sich_eine_Markierung_nicht()
    {
        var cut = Zeige(new List<GanglinienProjektZeile>
        {
            new GanglinienProjektZeile(7, 3, "Werk Nord")
        });

        Waehle(cut, 0, 0);
        Assert.NotNull(cut.Instance.Projektzeile);
        Assert.Null(cut.Instance.Katalogzeile);

        Waehle(cut, 1, 0);
        Assert.Null(cut.Instance.Projektzeile);
        Assert.NotNull(cut.Instance.Katalogzeile);
    }

    // =====================================================================
    // Verwaltung als Ueberlagerung
    // =====================================================================

    /// <summary>
    /// „Bearbeiten…" zeigt die Verwaltung als Überlagerung — kein zweites Fenster,
    /// keine zweite WebView (Risiko R2).
    /// </summary>
    [Fact]
    public void Bearbeiten_zeigt_die_Verwaltung_als_Ueberlagerung()
    {
        var cut = Zeige();

        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
        Spalte(cut, 1).QuerySelector(".epos-leiste button")!.Click();
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
    }

    /// <summary>
    /// Esc schließt die OBERSTE Ebene: Steht die Verwaltung offen, meldet der Wirt
    /// nichts.
    /// </summary>
    [Fact]
    public void Esc_meldet_nichts_solange_die_Verwaltung_offen_ist()
    {
        bool? ergebnis = null;
        var cut = Zeige(geschlossen: b => ergebnis = b);

        Spalte(cut, 1).QuerySelector(".epos-leiste button")!.Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Null(ergebnis);
    }

    // =====================================================================
    // Schluss
    // =====================================================================

    [Fact]
    public void OK_meldet_true_Abbrechen_und_Esc_melden_false()
    {
        bool? ergebnis = null;
        var cut = Zeige(geschlossen: b => ergebnis = b);

        Fussknopf(cut, 1).Click();
        Assert.True(ergebnis);

        Fussknopf(cut, 0).Click();
        Assert.False(ergebnis);

        ergebnis = null;
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(ergebnis);
    }

    /// <summary>
    /// Assistentenbetrieb (seit iU9-W16a.1 die Assistentenseite 6, vorher der
    /// Zwilling <c>Wizard_Stromlastgang</c>): keine Schlussleiste, und Esc meldet
    /// nichts — die Knöpfe stellt der Assistent.
    /// </summary>
    [Fact]
    public void Im_Assistentenbetrieb_faellt_die_Schlussleiste_weg()
    {
        bool? ergebnis = null;
        var cut = Zeige(wizard: true, geschlossen: b => ergebnis = b);

        Assert.Empty(cut.FindAll(".epos-dialog > .epos-leiste"));
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-mitte button").Count);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(ergebnis);
    }

    /// <summary>
    /// iU9-W16a.1 — der RÜCKWEG der Assistentenseite. Im Dialogbetrieb schreibt die
    /// Hülle die Liste nach dem Schließen zurück; als Assistentenseite gibt es kein
    /// Schließen, deshalb meldet die Komponente JEDE Änderung an
    /// <c>Zeilen</c> über <c>Geaendert</c>.
    /// </summary>
    [Fact]
    public void Jede_Aenderung_der_Zuordnung_wird_gemeldet()
    {
        int meldungen = 0;
        var zeilen = new List<GanglinienProjektZeile>();
        var cut = Zeige(zeilen, wizard: true, geaendert: () => meldungen++);

        cut.WaitForAssertion(() => Assert.Equal(2, Zeilen(cut, 1).Length));

        // Hinzufuegen meldet.
        Waehle(cut, 1, 0);
        Hinzu(cut).Click();
        Assert.Single(zeilen);
        Assert.Equal(1, meldungen);

        // Entfernen meldet.
        Waehle(cut, 0, 0);
        Entfernen(cut).Click();
        Assert.Empty(zeilen);
        Assert.Equal(2, meldungen);
    }

    /// <summary>
    /// Ohne den Rückruf (Dialogbetrieb) bleibt alles beim Alten — die Liste wird
    /// trotzdem an Ort und Stelle bearbeitet.
    /// </summary>
    [Fact]
    public void Ohne_Rueckruf_wird_die_Liste_trotzdem_gepflegt()
    {
        var zeilen = new List<GanglinienProjektZeile>();
        var cut = Zeige(zeilen);

        cut.WaitForAssertion(() => Assert.Equal(2, Zeilen(cut, 1).Length));

        Waehle(cut, 1, 0);
        Hinzu(cut).Click();

        Assert.Single(zeilen);
    }
    // =====================================================================
    // W12-E-1: Importieren, Speichern unter, Loeschen (Windows-Abnahme 05.09.2026)
    // =====================================================================

    /// <summary>
    /// Der Anwenderwunsch im Bild: unter der Katalogliste stehen VIER Knöpfe, in
    /// dieser Reihenfolge. Vorher war „Bearbeiten…" der einzige.
    /// </summary>
    [Fact]
    public void Unter_der_Katalogliste_stehen_vier_Knoepfe()
    {
        var cut = ZeigeMitKatalogpflege();

        string[] texte = Katalogknoepfe(cut).Select(b => b.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "CSV-Datei importieren...", "Speichern unter...", "Löschen", "Bearbeiten..." },
                     texte);
    }

    /// <summary>
    /// „Kein Delegat, kein Knopf": Fehlt der jeweilige Rückruf, ist der Knopf gar
    /// nicht da — und ohne jede Gabe zeichnet der Dialog nur die zwei Listen
    /// (Regel W16b-B1).
    /// </summary>
    [Fact]
    public void Ohne_Delegaten_bleibt_der_jeweilige_Knopf_weg()
    {
        var ohneAlles = Render<StromganglinieDialog>(p => p
            .Add(x => x.Zeilen, new List<GanglinienProjektZeile>())
            .Add(x => x.Katalog, () => Task.FromResult(Katalog())));
        Assert.Empty(Spalte(ohneAlles, 1).QuerySelectorAll(".epos-leiste button"));
        Assert.Empty(ohneAlles.FindAll(".epos-formathinweis"));

        // Ein Dateiwähler OHNE Einlesekette ist kein Importweg.
        var halb = Render<StromganglinieDialog>(p => p
            .Add(x => x.Zeilen, new List<GanglinienProjektZeile>())
            .Add(x => x.Katalog, () => Task.FromResult(Katalog()))
            .Add(x => x.DateiWaehlen, (Func<string, Task<string?>>)(f => Task.FromResult<string?>(null))));
        Assert.Empty(halb.FindAll(".epos-formathinweis"));
    }

    /// <summary>
    /// <b>Der Formathinweis</b> — der Anwender soll nicht erst am Prüfprotokoll
    /// erfahren, welche Datei gemeint war.
    ///
    /// <para><b>Seit W12‑E‑2 ist die sichtbare Zeile EINZEILIG</b>: Dateiart,
    /// Wertzahl, ein Wert je Zeile, Verweis auf den Infoknopf. Die neun Zeilen
    /// standen genau dort, wo jetzt die Grafik steht. Die sechs Angaben, die die
    /// Kette wirklich auswertet, hängen unverändert AM INFOKNOPF — geprüft wird
    /// deshalb beides: die kurze Zeile und der vollständige Kurztext.</para>
    /// </summary>
    [Fact]
    public void Der_Formathinweis_ist_einzeilig_und_der_Infoknopf_traegt_den_vollen_Wortlaut()
    {
        var cut = ZeigeMitKatalogpflege();

        IElement hinweis = Spalte(cut, 1).QuerySelector(".epos-formathinweis")!;
        string kurz = hinweis.QuerySelector(".epos-herleitung-text")!.TextContent;

        // Die sichtbare Zeile: kurz, aber vollständig genug, um zu wissen, was
        // gemeint ist. 160 Zeichen sind rund eine Zeile in Dialogbreite.
        Assert.True(kurz.Length <= 160, "Der Formathinweis ist nicht mehr einzeilig: " + kurz);
        Assert.Contains("CSV", kurz, StringComparison.Ordinal);
        Assert.Contains("8.760", kurz, StringComparison.Ordinal);
        Assert.Contains("35.040", kurz, StringComparison.Ordinal);
        Assert.Contains("Infoknopf", kurz, StringComparison.Ordinal);

        // Und ausgerechnet die Angaben, die nicht mehr dastehen, sind auch nicht
        // verschwunden - sie hängen am Infoknopf.
        Assert.DoesNotContain("Semikolon", kurz, StringComparison.Ordinal);

        IElement info = hinweis.QuerySelector(".epos-infoknopf")!;
        string lang = info.GetAttribute("title")!;

        Assert.Contains("Semikolon", lang, StringComparison.Ordinal);
        Assert.Contains("Kopfzeile", lang, StringComparison.Ordinal);
        Assert.Contains("Dezimaltrennzeichen", lang, StringComparison.Ordinal);
        Assert.Contains("kW", lang, StringComparison.Ordinal);
        Assert.Contains("Dateiname ohne Erweiterung", lang, StringComparison.Ordinal);
    }

    /// <summary>
    /// Der Dateiwähler DARF warten (W13-B-1) und wird await-et; danach läuft
    /// DIESELBE Kette wie in der Verwaltung, und der Katalog wird neu gezogen.
    /// </summary>
    [Fact]
    public async Task Importieren_waehlt_die_Datei_und_faehrt_die_Kette()
    {
        TaskCompletionSource<string?> waehler = new();
        string? gesehenPfad = null;
        GanglinienRaster gesehenRaster = GanglinienRaster.Minute;
        int katalogLaeufe = 0;

        var liste = Katalog();
        var cut = Render<StromganglinieDialog>(p => p
            .Add(x => x.Zeilen, new List<GanglinienProjektZeile>())
            .Add(x => x.Katalog, () => { katalogLaeufe++; return Task.FromResult(liste); })
            .Add(x => x.DateiWaehlen, (Func<string, Task<string?>>)(f => waehler.Task))
            .Add(x => x.Einlesen, (Func<string, GanglinienRaster, GanglinienImportRueckrufe,
                                        Task<GanglinienImportErgebnis>>)((pfad, raster, r) =>
            {
                gesehenPfad = pfad;
                gesehenRaster = raster;
                return Task.FromResult(new GanglinienImportErgebnis
                {
                    Ausgang = ImportAusgang.Erfolg,
                    Meldung = "Ganglinie eingelesen",
                    MeldungStufe = PruefStufe.Info
                });
            })));

        cut.WaitForAssertion(() => Assert.Equal(1, katalogLaeufe));

        Katalogknopf(cut, "importieren").Click();
        Assert.Null(gesehenPfad);                       // der Wähler steht noch offen

        waehler.SetResult(@"C:\Daten\lastgang.csv");
        await cut.InvokeAsync(() => Task.CompletedTask);

        cut.WaitForAssertion(() => Assert.Equal(@"C:\Daten\lastgang.csv", gesehenPfad));

        // Die Maske gibt KEINE Rastervorgabe - die Kette erkennt es selbst, und der
        // Optionendialog laesst es uebersteuern.
        Assert.Equal(GanglinienRaster.Unbekannt, gesehenRaster);
        cut.WaitForAssertion(() => Assert.Equal("Ganglinie eingelesen", cut.Instance.Meldung));
        cut.WaitForAssertion(() => Assert.Equal(2, katalogLaeufe));
    }

    /// <summary>Ein abgebrochener Wähler liest nichts und meldet nichts.</summary>
    [Fact]
    public void Ein_abgebrochener_Dateiwaehler_liest_nichts()
    {
        int gerufen = 0;
        var cut = ZeigeMitKatalogpflege(
            waehlen: f => Task.FromResult<string?>(null),
            einlesen: (pfad, raster, r) =>
            {
                gerufen++;
                return Task.FromResult(new GanglinienImportErgebnis());
            });

        Katalogknopf(cut, "importieren").Click();

        Assert.Equal(0, gerufen);
        Assert.Equal("", cut.Instance.Meldung);
    }

    // --- Loeschen --------------------------------------------------------

    /// <summary>Ohne gewählten Katalogeintrag sind „Löschen" und „Speichern unter" gesperrt.</summary>
    [Fact]
    public void Loeschen_und_Speichern_unter_sind_ohne_Auswahl_gesperrt()
    {
        var cut = ZeigeMitKatalogpflege();

        Assert.True(Katalogknopf(cut, "Löschen").HasAttribute("disabled"));
        Assert.True(Katalogknopf(cut, "Speichern unter").HasAttribute("disabled"));

        Waehle(cut, 1, 0);
        Assert.False(Katalogknopf(cut, "Löschen").HasAttribute("disabled"));
        Assert.False(Katalogknopf(cut, "Speichern unter").HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>Sperre 1</b> (Muster der Solarganglinie, W14b): Eine einem Projekt
    /// zugeordnete Ganglinie bleibt stehen — mit Grund, und ohne dass die Rückfrage
    /// überhaupt kommt.
    /// </summary>
    [Fact]
    public void Eine_zugeordnete_Ganglinie_wird_nicht_geloescht()
    {
        int geloescht = 0;
        var cut = ZeigeMitKatalogpflege(
            zuordnung: n => Task.FromResult(n == "Werk Nord"),
            loeschen: n => { geloescht++; return Task.FromResult(true); });

        Waehle(cut, 1, 0);                              // "Werk Nord"
        Katalogknopf(cut, "Löschen").Click();

        Assert.Equal(0, geloescht);
        Assert.Contains("Projektzuordnung", cut.Instance.Meldung);
        Assert.Single(cut.FindAll(".epos-warnbanner"));
        Assert.All(cut.FindComponents<Rueckfrage>(), f => Assert.False(f.Instance.Offen));
    }

    /// <summary>
    /// <b>Sperre 2</b>: Ein Auslieferungssatz bleibt stehen. Der Grund hängt
    /// zusätzlich als <c>title</c> am Knopf — er ist synchron bekannt (Staffelung
    /// W16b-E-6: der Grund am Bedienelement, das Banner erst nach dem Versuch).
    /// </summary>
    [Fact]
    public void Ein_Auslieferungssatz_wird_nicht_geloescht()
    {
        int geloescht = 0;
        var cut = ZeigeMitKatalogpflege(loeschen: n => { geloescht++; return Task.FromResult(true); });

        Waehle(cut, 1, 1);                              // "Auslieferung", NurLesen
        Assert.Contains("schreibgeschützt", Katalogknopf(cut, "Löschen").GetAttribute("title")!);

        Katalogknopf(cut, "Löschen").Click();

        Assert.Equal(0, geloescht);
        Assert.Contains("schreibgeschützt", cut.Instance.Meldung);
        Assert.All(cut.FindComponents<Rueckfrage>(), f => Assert.False(f.Instance.Offen));
    }

    /// <summary>Vor dem Löschen kommt die Rückfrage; „Ja" löscht und meldet.</summary>
    [Fact]
    public void Vor_dem_Loeschen_kommt_eine_Rueckfrage()
    {
        List<string> geloescht = new();
        var liste = Katalog();
        var cut = ZeigeMitKatalogpflege(
            katalog: liste,
            loeschen: n => { geloescht.Add(n); liste.RemoveAll(z => z.Bezeichner == n); return Task.FromResult(true); });

        Waehle(cut, 1, 0);
        Katalogknopf(cut, "Löschen").Click();

        Rueckfrage frage = cut.FindComponents<Rueckfrage>().First(f => f.Instance.Offen).Instance;
        Assert.Contains("Werk Nord", frage.Frage);
        Assert.Empty(geloescht);

        cut.FindComponents<Rueckfrage>().First(f => f.Instance.Offen)
           .FindAll("button").First(b => b.TextContent.Trim() == "Ja").Click();

        Assert.Equal(new[] { "Werk Nord" }, geloescht);
        cut.WaitForAssertion(() => Assert.Contains("gelöscht", cut.Instance.Meldung));
        cut.WaitForAssertion(() => Assert.Single(Zeilen(cut, 1)));
    }

    /// <summary>„Nein" lässt die Ganglinie stehen.</summary>
    [Fact]
    public void Nein_laesst_die_Ganglinie_stehen()
    {
        int geloescht = 0;
        var cut = ZeigeMitKatalogpflege(loeschen: n => { geloescht++; return Task.FromResult(true); });

        Waehle(cut, 1, 0);
        Katalogknopf(cut, "Löschen").Click();
        cut.FindComponents<Rueckfrage>().First(f => f.Instance.Offen)
           .FindAll("button").First(b => b.TextContent.Trim() == "Nein").Click();

        Assert.Equal(0, geloescht);
        Assert.All(cut.FindComponents<Rueckfrage>(), f => Assert.False(f.Instance.Offen));
        Assert.Equal(2, Zeilen(cut, 1).Length);
    }

    // --- Speichern unter -------------------------------------------------

    /// <summary>
    /// „Speichern unter…" fragt den Namen und schlägt „&lt;Name&gt; - Kopie" vor —
    /// dieselbe Schreibweise, die der Bestand schon führt.
    /// </summary>
    [Fact]
    public void Speichern_unter_schlaegt_den_Namen_mit_Kopie_vor()
    {
        var cut = ZeigeMitKatalogpflege();

        Waehle(cut, 1, 0);
        Katalogknopf(cut, "Speichern unter").Click();

        var namensdialog = cut.FindComponent<EPOS.UI.Dialoge.Allgemein.NamensDialog>();
        Assert.Equal("Werk Nord - Kopie", namensdialog.Instance.Name);
    }

    /// <summary>
    /// <b>Die Dublettenprüfung läuft VOR dem Einfügen</b>: Ein vergebener Name hält
    /// den Namensdialog offen und sagt, warum — kein UNIQUE-Fehler aus SQLite.
    /// </summary>
    [Fact]
    public void Ein_vergebener_Name_wird_abgewiesen_bevor_kopiert_wird()
    {
        int kopiert = 0;
        var cut = ZeigeMitKatalogpflege(kopieren: (q, z) => { kopiert++; return Task.FromResult(true); });

        Waehle(cut, 1, 0);
        Katalogknopf(cut, "Speichern unter").Click();

        var namensdialog = cut.FindComponent<EPOS.UI.Dialoge.Allgemein.NamensDialog>();
        namensdialog.Find("input").Input("Auslieferung");        // gibt es schon
        namensdialog.FindAll("button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.Equal(0, kopiert);
        Assert.Single(cut.FindComponents<EPOS.UI.Dialoge.Allgemein.NamensDialog>());
        Assert.Contains("bereits in der Datenbank", namensdialog.Markup);
    }

    /// <summary>Ein freier Name legt die Kopie an; die Meldung nennt beide Namen.</summary>
    [Fact]
    public void Ein_freier_Name_legt_die_Kopie_an()
    {
        List<(string Quelle, string Ziel)> kopien = new();
        var liste = Katalog();
        var cut = ZeigeMitKatalogpflege(
            katalog: liste,
            kopieren: (q, z) =>
            {
                kopien.Add((q, z));
                liste.Add(new GanglinienKatalogZeile(z, 4, false));
                return Task.FromResult(true);
            });

        Waehle(cut, 1, 0);
        Katalogknopf(cut, "Speichern unter").Click();

        var namensdialog = cut.FindComponent<EPOS.UI.Dialoge.Allgemein.NamensDialog>();
        namensdialog.FindAll("button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.Equal(new[] { ("Werk Nord", "Werk Nord - Kopie") }, kopien);
        cut.WaitForAssertion(() => Assert.Contains("Werk Nord - Kopie", cut.Instance.Meldung));
        cut.WaitForAssertion(() => Assert.Equal(3, Zeilen(cut, 1).Length));
    }

    /// <summary>
    /// Scheitert die Kopie trotzdem (der Kern prüft dieselbe Dublette ein zweites
    /// Mal), steht ein Fehlerbanner — keine Ausnahme.
    /// </summary>
    [Fact]
    public void Eine_gescheiterte_Kopie_meldet_sich_als_Banner()
    {
        var cut = ZeigeMitKatalogpflege(kopieren: (q, z) => Task.FromResult(false));

        Waehle(cut, 1, 0);
        Katalogknopf(cut, "Speichern unter").Click();
        cut.FindComponent<EPOS.UI.Dialoge.Allgemein.NamensDialog>()
           .FindAll("button").First(b => b.TextContent.Trim() == "OK").Click();

        cut.WaitForAssertion(() => Assert.Contains("konnte nicht angelegt werden", cut.Instance.Meldung));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-warnbanner--fehler")));
    }

    /// <summary>Abbrechen im Namensdialog kopiert nichts.</summary>
    [Fact]
    public void Abbrechen_im_Namensdialog_kopiert_nichts()
    {
        int kopiert = 0;
        var cut = ZeigeMitKatalogpflege(kopieren: (q, z) => { kopiert++; return Task.FromResult(true); });

        Waehle(cut, 1, 0);
        Katalogknopf(cut, "Speichern unter").Click();
        cut.FindComponent<EPOS.UI.Dialoge.Allgemein.NamensDialog>()
           .FindAll("button").First(b => b.TextContent.Trim() == "Abbrechen").Click();

        Assert.Equal(0, kopiert);
        Assert.Empty(cut.FindComponents<EPOS.UI.Dialoge.Allgemein.NamensDialog>());
        Assert.Equal("", cut.Instance.Meldung);
    }

    // =====================================================================
    // Die Grafik der gewaehlten Ganglinie (W12-E-2)
    // =====================================================================

    /// <summary>Ein Bildauftrag, wie ihn die Grafik stellt.</summary>
    private sealed record Auftrag(GanglinienWahl Wahl, bool Sortiert, Diagrammbereich? Bereich);

    /// <summary>
    /// Der Dialog MIT der Grafikseite (W12‑E‑2). Die Kennzahlen kommen aus einem
    /// Wörterbuch je Bezeichner — so lässt sich prüfen, dass wirklich die MARKIERTE
    /// Ganglinie gezeigt wird und nicht irgendeine.
    /// </summary>
    private IRenderedComponent<StromganglinieDialog> ZeigeMitGrafik(
        List<GanglinienProjektZeile>? zeilen = null,
        List<Auftrag>? auftraege = null,
        Dictionary<string, GanglinienKennzahlen?>? kennzahlen = null,
        Energieeinheit? einheit = null,
        Action<Energieeinheit>? einheitGewaehlt = null)
    {
        List<Auftrag> liste = auftraege ?? new List<Auftrag>();
        Dictionary<string, GanglinienKennzahlen?> zahlen = kennzahlen ?? new()
        {
            ["Werk Nord"] = new GanglinienKennzahlen(4790.086, 2070.0, 2314.0512077294684),
            ["Auslieferung"] = new GanglinienKennzahlen(1000.0, 500.0, 2000.0)
        };

        return Render<StromganglinieDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<GanglinienProjektZeile>())
            .Add(x => x.Katalog, () => Task.FromResult(Katalog()))
            .Add(x => x.Kennzahlen, (GanglinienWahl w) =>
                Task.FromResult(zahlen.TryGetValue(w.Bezeichner, out GanglinienKennzahlen? k)
                                ? k : null))
            .Add(x => x.Bildauftrag, (GanglinienWahl w, bool sortiert, Diagrammbereich? bereich) =>
            {
                liste.Add(new Auftrag(w, sortiert, bereich));
                return new byte[] { 1, 2, 3 };
            })
            .Add(x => x.Einheit, einheit ?? Energieeinheit.MWh)
            .Add(x => x.EinheitGewaehlt, einheitGewaehlt));
    }

    /// <summary>
    /// <b>Ohne Markierung keine Grafik.</b> Beim Aufbau ist nichts gewählt — dann
    /// stünde eine Kennzahlenzeile ohne Bezug da.
    /// </summary>
    [Fact]
    public void Ohne_Wahl_steht_keine_Grafik()
    {
        var cut = ZeigeMitGrafik();

        Assert.Empty(cut.FindAll(".epos-ganglinie-grafik"));
        Assert.Empty(cut.FindAll("img.epos-chartbild"));
        Assert.Null(cut.Instance.Grafikkennzahlen);
    }

    /// <summary>
    /// <b>Ohne Delegaten keine Grafik</b> — dieselbe Regel wie bei den vier Knöpfen.
    /// Eine markierte Zeile allein reicht nicht.
    /// </summary>
    [Fact]
    public void Ohne_Kennzahlen_Delegat_bleibt_die_Grafik_weg()
    {
        var cut = Zeige();
        Waehle(cut, 1, 0);

        Assert.Empty(cut.FindAll(".epos-ganglinie-grafik"));
    }

    /// <summary>
    /// <b>Eine markierte KATALOGzeile bringt Bild und Kennzahlen.</b> Geprüft werden
    /// alle drei Zahlen in deutscher Anzeige (MWh, kW, h/a) und der Bildauftrag, der
    /// die gewählte Ganglinie nennt.
    /// </summary>
    [Fact]
    public void Mit_gewaehlter_Katalogzeile_stehen_Bild_und_Kennzahlen()
    {
        var auftraege = new List<Auftrag>();
        var cut = ZeigeMitGrafik(auftraege: auftraege);

        Waehle(cut, 1, 0);      // "Werk Nord"

        Assert.Single(cut.FindAll(".epos-ganglinie-grafik"));
        Assert.NotNull(cut.Find("img.epos-chartbild"));
        Assert.Contains("Werk Nord", cut.Find(".epos-ganglinie-grafik .epos-kontextzeile").TextContent);

        Assert.Equal(3, cut.FindAll(".epos-ganglinie-kennzahl").Count);
        Assert.Contains("Jahresarbeit:", cut.Markup);
        Assert.Contains("Spitzenlast:", cut.Markup);
        Assert.Contains("Vollbenutzungsstunden:", cut.Markup);

        Assert.Contains("4790,09", cut.Markup);     // MWh, deutsche Anzeige
        Assert.Contains("2070,00", cut.Markup);     // kW
        Assert.Contains("2314,05", cut.Markup);     // h/a

        // Der Bildauftrag kennt die Wahl: rechte Spalte, also der Katalog.
        Assert.Contains(auftraege, a => a.Wahl.AusKatalog && a.Wahl.Bezeichner == "Werk Nord"
                                        && !a.Sortiert && a.Bereich is null);
    }

    /// <summary>
    /// <b>Auch die linke Spalte zeigt ihre Ganglinie.</b> Eine Projektzeile trägt die
    /// Id der PROJEKTKOPIE mit; eine eben erst zugeordnete Zeile hat noch keine
    /// (<c>GanglinieId</c> = 0) und wird über ihren Bezeichner gefunden.
    /// </summary>
    [Fact]
    public void Mit_gewaehlter_Projektzeile_steht_die_Grafik_dieser_Zeile()
    {
        var auftraege = new List<Auftrag>();
        var zeilen = new List<GanglinienProjektZeile>
        {
            new GanglinienProjektZeile(7, 4711, "Werk Nord")
        };

        var cut = ZeigeMitGrafik(zeilen: zeilen, auftraege: auftraege);
        Waehle(cut, 0, 0);

        Assert.Single(cut.FindAll(".epos-ganglinie-grafik"));
        Assert.Contains(auftraege, a => !a.Wahl.AusKatalog && a.Wahl.GanglinieId == 4711);
    }

    /// <summary>
    /// Zu einer Ganglinie OHNE brauchbare Reihe (der Kern liefert <c>null</c>) bleibt
    /// die Grafik weg — statt einen leeren Rahmen mit Nullen zu zeigen.
    /// </summary>
    [Fact]
    public void Ohne_Kennzahlen_zu_dieser_Ganglinie_bleibt_die_Grafik_weg()
    {
        var cut = ZeigeMitGrafik(kennzahlen: new Dictionary<string, GanglinienKennzahlen?>
        {
            ["Werk Nord"] = null
        });

        Waehle(cut, 1, 0);

        Assert.Empty(cut.FindAll(".epos-ganglinie-grafik"));
        Assert.Null(cut.Instance.Grafikkennzahlen);
    }

    /// <summary>
    /// <b>Der Schalter „sortiert" wechselt die Reihe</b> — aus der Jahresganglinie
    /// wird die Dauerlinie; der zweite Bildauftrag trägt <c>Sortiert = true</c>.
    /// Dieselbe Umschaltung wie im Bedarfsreiter der Ergebnisseite.
    /// </summary>
    [Fact]
    public void Der_Schalter_sortiert_laesst_neu_zeichnen()
    {
        var auftraege = new List<Auftrag>();
        var cut = ZeigeMitGrafik(auftraege: auftraege);

        Waehle(cut, 1, 0);
        Assert.Contains(auftraege, a => !a.Sortiert);

        GanglinienGrafik grafik = cut.FindComponent<GanglinienGrafik>().Instance;
        Assert.False(grafik.Sortiert);

        cut.Find(".epos-ganglinie-leiste input[type=checkbox]").Change(true);

        Assert.True(grafik.Sortiert);
        Assert.Contains(auftraege, a => a.Sortiert);
    }

    /// <summary>
    /// Der Datenzoom (Befund A‑1): Ein aufgezogenes Rechteck geht UNVERÄNDERT in den
    /// Bildauftrag, und ein Schalterwechsel verwirft ihn wieder.
    /// </summary>
    [Fact]
    public async Task Ein_aufgezogener_Bereich_geht_in_den_Bildauftrag()
    {
        var auftraege = new List<Auftrag>();
        var cut = ZeigeMitGrafik(auftraege: auftraege);
        Waehle(cut, 1, 0);

        Diagramm diagramm = cut.FindComponent<Diagramm>().Instance;
        await cut.InvokeAsync(() => diagramm.BereichGemeldet(0.25, 0.5, 0.1, 0.9));

        GanglinienGrafik grafik = cut.FindComponent<GanglinienGrafik>().Instance;
        Assert.NotNull(grafik.Bereich);
        Assert.Contains(auftraege, a => a.Bereich is not null && a.Bereich.XBis == 0.5);

        cut.Find(".epos-ganglinie-leiste input[type=checkbox]").Change(true);
        Assert.Null(grafik.Bereich);
    }

    /// <summary>
    /// <b>Die Einheit (Anwenderentscheid W8‑O‑5):</b> MWh ist die Vorgabe, kWh ist
    /// wählbar, und die Wahl geht an die Hülle zurück, damit sie gemerkt wird. Nur
    /// die Jahresarbeit folgt ihr — die Spitze bleibt kW.
    /// </summary>
    [Fact]
    public void Die_Einheit_ist_waehlbar_und_gilt_nur_fuer_die_Jahresarbeit()
    {
        Energieeinheit? gemeldet = null;
        var cut = ZeigeMitGrafik(einheitGewaehlt: e => gemeldet = e);

        Waehle(cut, 1, 0);
        Assert.Contains("MWh", cut.Find(".epos-ganglinie-kennzahlen").TextContent);
        Assert.Contains("4790,09", cut.Markup);

        IElement wahl = cut.Find(".epos-ganglinie-leiste select");
        wahl.Change("1");                       // kWh

        Assert.Equal(Energieeinheit.KWh, gemeldet);
        Assert.Contains("kWh", cut.Find(".epos-ganglinie-kennzahlen").TextContent);

        // 4 790,086 MWh = 4 790 086 kWh, ohne Nachkommastellen (F0 der Einheit -
        // und F0 setzt keine Tausendertrennzeichen).
        Assert.Contains("4790086", cut.Markup);

        // Die Leistung bleibt kW und behaelt ihre zwei Stellen.
        Assert.Contains("2070,00", cut.Markup);
    }

    /// <summary>
    /// Ohne Höchstlast gibt es keine Vollbenutzungsstunden, sondern „—" — dieselbe
    /// Regel wie im Gebäudebedarfsdialog.
    /// </summary>
    [Fact]
    public void Ohne_Spitze_steht_ein_Strich_statt_der_Vollbenutzungsstunden()
    {
        var cut = ZeigeMitGrafik(kennzahlen: new Dictionary<string, GanglinienKennzahlen?>
        {
            ["Werk Nord"] = new GanglinienKennzahlen(0, 0, null)
        });

        Waehle(cut, 1, 0);

        Assert.Contains("—", cut.Find(".epos-ganglinie-kennzahlen").TextContent);
    }

    /// <summary>
    /// <b>Die Grafik gehört zur MARKIERTEN Zeile.</b> Wer die Markierung wechselt,
    /// sieht die andere Ganglinie — und nicht das zwischengespeicherte Bild der
    /// vorigen.
    /// </summary>
    [Fact]
    public void Ein_Wechsel_der_Markierung_wechselt_die_Grafik()
    {
        var auftraege = new List<Auftrag>();
        var cut = ZeigeMitGrafik(auftraege: auftraege);

        Waehle(cut, 1, 0);
        Assert.Contains("Werk Nord", cut.Find(".epos-ganglinie-grafik .epos-kontextzeile").TextContent);

        Waehle(cut, 1, 1);
        Assert.Contains("Auslieferung", cut.Find(".epos-ganglinie-grafik .epos-kontextzeile").TextContent);
        Assert.Contains("1000,00", cut.Markup);
        Assert.Contains(auftraege, a => a.Wahl.Bezeichner == "Auslieferung");
    }

    /// <summary>
    /// Wird die markierte Zuordnung entfernt, geht ihre Grafik mit: Zahlen zu einer
    /// Zeile, die nicht mehr dasteht, sind schlimmer als keine Zahlen.
    /// </summary>
    [Fact]
    public void Das_Entfernen_der_Zeile_nimmt_ihre_Grafik_mit()
    {
        var zeilen = new List<GanglinienProjektZeile>
        {
            new GanglinienProjektZeile(7, 4711, "Werk Nord")
        };
        var cut = ZeigeMitGrafik(zeilen: zeilen);

        Waehle(cut, 0, 0);
        Assert.Single(cut.FindAll(".epos-ganglinie-grafik"));

        Entfernen(cut).Click();

        Assert.Empty(cut.FindAll(".epos-ganglinie-grafik"));
    }

    // --- Esc schliesst immer nur die oberste Ebene -----------------------

    [Fact]
    public void Esc_meldet_nichts_solange_die_Rueckfrage_oder_der_Namensdialog_steht()
    {
        bool? ergebnis = null;
        var cut = Render<StromganglinieDialog>(p => p
            .Add(x => x.Zeilen, new List<GanglinienProjektZeile>())
            .Add(x => x.Katalog, () => Task.FromResult(Katalog()))
            .Add(x => x.Loeschen, (Func<string, Task<bool>>)(n => Task.FromResult(true)))
            .Add(x => x.Kopieren, (Func<string, string, Task<bool>>)((q, z) => Task.FromResult(true)))
            .Add(x => x.Geschlossen, (bool ok) => ergebnis = ok));

        Waehle(cut, 1, 0);

        Katalogknopf(cut, "Löschen").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(ergebnis);

        cut.FindComponents<Rueckfrage>().First(f => f.Instance.Offen)
           .FindAll("button").First(b => b.TextContent.Trim() == "Nein").Click();

        Katalogknopf(cut, "Speichern unter").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(ergebnis);
    }
}
