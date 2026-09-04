using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Verwaltung der externen Wärmebedarfsganglinien (iU9-W13.2). Soll ist die
/// Feldkarte von <c>Form_AdminWaermeeinlesen</c> (11 Steuerelemente: 5 Knöpfe,
/// 3 Beschriftungen, 2 Textfelder, 1 Liste).
///
/// <para>Die Kultur ist auf de-DE gepinnt: Die Erwartungswerte sind deutsche
/// Beschriftungen, und der Windows-Läufer läuft mit englischer Oberfläche.</para>
/// </summary>
public class WaermebedarfAdminDialogTests : BunitContext
{
    private static readonly List<WaermebedarfAdminDialog.Katalogzeile> KATALOG = new()
    {
        new(1, "Buerohaus 2024", false),
        new(2, "Auslieferung Standard", true),
        new(3, "Werkhalle Nord", false)
    };

    public WaermebedarfAdminDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>
    /// Die Sprache der Oberfläche wird auf de-DE gepinnt (Muster
    /// <c>DeutscheOberflaeche</c> aus <c>EPOS.Kern.Tests</c>).
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

    private IRenderedComponent<WaermebedarfAdminDialog> Aufbauen(
        Func<string, Task<bool>>? hatZuordnung = null,
        Func<string, Task<bool>>? loeschen = null,
        Func<string, Task<string?>>? dateiWaehlen = null,
        Func<string, Task<AblageErgebnis>>? ablegen = null,
        Func<string, Task<bool>>? mitSystem = null,
        Func<string, WaermebedarfImportRueckrufe, IProgress<ImportFortschritt>,
             Task<WaermebedarfImportErgebnis>>? einlesen = null,
        Action<bool>? geschlossen = null)
        => Render<WaermebedarfAdminDialog>(p => p
            .Add(x => x.Katalog, () => Task.FromResult(new List<WaermebedarfAdminDialog.Katalogzeile>(KATALOG)))
            .Add(x => x.HatProjektzuordnung, hatZuordnung ?? (_ => Task.FromResult(false)))
            .Add(x => x.Loeschen, loeschen ?? (_ => Task.FromResult(true)))
            .Add(x => x.DateiWaehlen, dateiWaehlen)
            .Add(x => x.Ablegen, ablegen)
            .Add(x => x.MitSystemOeffnen, mitSystem)
            .Add(x => x.Einlesen, einlesen)
            .Add(x => x.Ordner, @"C:\Users\probe\AppData\Local\WP-Plan\Waermebedarf")
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IElement Knopf(IRenderedComponent<WaermebedarfAdminDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // =====================================================================
    // 1 — Feldbestand
    // =====================================================================

    /// <summary>
    /// Die fünf Knöpfe der Feldkarte, wörtlich: „Datei Auswählen…",
    /// „Inhalt anzeigen…", „Datei in DB Einlesen…", „DB Ganglinie Löschen",
    /// „Beenden". Dazu der Ordnerpfad und die Katalogliste.
    /// </summary>
    [Fact]
    public void Die_Maske_zeigt_ihre_fuenf_Knoepfe_und_den_Ordner()
    {
        var cut = Aufbauen(dateiWaehlen: _ => Task.FromResult<string?>(""));

        Assert.Equal("Wärmebedarf Ganglinie", cut.Find(".epos-dialog-titel").TextContent);

        string knoepfe = string.Join("|", cut.FindAll("button").Select(b => b.TextContent.Trim()));
        Assert.Contains("Datei Auswählen...", knoepfe);
        Assert.Contains("Inhalt anzeigen...", knoepfe);
        Assert.Contains("Datei in DB Einlesen...", knoepfe);
        Assert.Contains("DB Ganglinie Löschen", knoepfe);
        Assert.Contains("Beenden", knoepfe);

        Assert.Contains("Ganglinien aus DB", cut.Markup);
        Assert.Contains("Datei Basis Ordner:", cut.Markup);
        Assert.Contains(@"C:\Users\probe\AppData\Local\WP-Plan\Waermebedarf", cut.Markup);
    }

    /// <summary>
    /// Der Hinweis nennt den Punkt als Dezimaltrennzeichen. Der Vorläufer schrieb
    /// „Dezimaltrennzeichen ','" — <c>WaermebedarfStammCtrl.ImportGanglinie</c>
    /// parst aber mit <c>InvariantCulture</c>, ein Komma hätte die Datei abgelehnt
    /// (Befund W13-B56, Abweichung A-10).
    /// </summary>
    [Fact]
    public void Der_Hinweis_nennt_den_Punkt_als_Dezimaltrennzeichen()
    {
        var cut = Aufbauen();

        Assert.Contains("Stundenwerte über 1 Jahr als Textdatei (Dezimaltrennzeichen '.')", cut.Markup);
    }

    [Fact]
    public void Der_Katalog_steht_in_der_Liste()
    {
        var cut = Aufbauen();

        Assert.Equal(3, cut.FindAll("tbody tr").Count);
        Assert.Contains("Buerohaus 2024", cut.Find("tbody").TextContent);
        Assert.Contains("Werkhalle Nord", cut.Find("tbody").TextContent);
    }

    // =====================================================================
    // 2 — Löschen
    // =====================================================================

    /// <summary>Ohne Auswahl bleibt der Löschknopf gesperrt.</summary>
    [Fact]
    public void Ohne_Auswahl_ist_der_Loeschknopf_gesperrt()
    {
        var cut = Aufbauen();

        Assert.True(Knopf(cut, "DB Ganglinie Löschen").HasAttribute("disabled"));

        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        Assert.False(Knopf(cut, "DB Ganglinie Löschen").HasAttribute("disabled"));
        Assert.Equal("Buerohaus 2024", cut.Instance.Gewaehlt);
    }

    /// <summary>
    /// <b>Prüfregel 1, wörtlich:</b> Eine zugeordnete Ganglinie bleibt stehen —
    /// und die Rückfrage kommt gar nicht erst. Die Sperre steht seit W9.0d als
    /// <c>HatProjektzuordnung</c> im Kern; die Maske rief sie nie und baute
    /// stattdessen inline-SQL aus dem Anwendertext (Befund W13-B8).
    /// </summary>
    [Fact]
    public void Eine_zugeordnete_Ganglinie_bleibt_stehen()
    {
        bool geloescht = false;
        var cut = Aufbauen(hatZuordnung: _ => Task.FromResult(true),
                           loeschen: _ => { geloescht = true; return Task.FromResult(true); });

        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        Knopf(cut, "DB Ganglinie Löschen").Click();

        Assert.Equal("Es existiert eine Projektzuordnung, Löschen nicht möglich!", cut.Instance.Meldung);
        Assert.False(geloescht);
        Assert.Empty(cut.FindAll("[role='dialog']"));
    }

    /// <summary>
    /// <b>Prüfregel 2:</b> Ein Auslieferungssatz bleibt stehen. Die Rückfrage
    /// kommt auch hier nicht.
    /// </summary>
    [Fact]
    public void Ein_Auslieferungssatz_bleibt_stehen()
    {
        bool geloescht = false;
        var cut = Aufbauen(loeschen: _ => { geloescht = true; return Task.FromResult(true); });

        cut.FindAll("tbody .epos-anlagenwahl")[1].Click();   // "Auslieferung Standard"
        Knopf(cut, "DB Ganglinie Löschen").Click();

        Assert.Contains("schreibgeschützt", cut.Instance.Meldung);
        Assert.False(geloescht);
    }

    /// <summary>
    /// <b>A-Zeile:</b> Vor dem Löschen wird gefragt — der Vorläufer löschte ohne
    /// jede Sicherheitsabfrage.
    /// </summary>
    [Fact]
    public void Vor_dem_Loeschen_wird_gefragt()
    {
        string? geloescht = null;
        var cut = Aufbauen(loeschen: n => { geloescht = n; return Task.FromResult(true); });

        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        Knopf(cut, "DB Ganglinie Löschen").Click();

        Assert.Single(cut.FindAll("[role='dialog']"));
        Assert.Contains("Die Ganglinie \"Buerohaus 2024\" aus dem Katalog löschen?", cut.Markup);
        Assert.Null(geloescht);

        Knopf(cut, "Nein").Click();
        Assert.Null(geloescht);

        Knopf(cut, "DB Ganglinie Löschen").Click();
        Knopf(cut, "Ja").Click();
        Assert.Equal("Buerohaus 2024", geloescht);
        Assert.Contains("wurde gelöscht", cut.Instance.Meldung);
    }

    // =====================================================================
    // 3 — Datei wählen, anzeigen, einlesen
    // =====================================================================

    /// <summary>
    /// Ohne gewählte Datei sind „Inhalt anzeigen…" und „Datei in DB Einlesen…"
    /// gesperrt. Der Vorläufer ließ beide immer bedienbar: <c>filebasename</c>
    /// war ein FELD, und ein Abbruch der Dateiwahl ließ die Kette mit der Datei
    /// des vorigen Laufs weiterlaufen (Befund W13-B10).
    /// </summary>
    [Fact]
    public void Ohne_Datei_sind_Anzeigen_und_Einlesen_gesperrt()
    {
        var cut = Aufbauen(dateiWaehlen: _ => Task.FromResult<string?>(""),
                           mitSystem: _ => Task.FromResult(true),
                           einlesen: (_, __, ___) => Task.FromResult(new WaermebedarfImportErgebnis()));

        Assert.True(Knopf(cut, "Inhalt anzeigen...").HasAttribute("disabled"));
        Assert.True(Knopf(cut, "Datei in DB Einlesen...").HasAttribute("disabled"));
    }

    /// <summary>
    /// Die gewählte Datei wird verlustfrei abgelegt; danach steht der Pfad der
    /// ABLAGE im Feld, nicht der der Quelle.
    /// </summary>
    [Fact]
    public void Die_gewaehlte_Datei_wird_abgelegt()
    {
        var cut = Aufbauen(
            dateiWaehlen: _ => Task.FromResult<string?>(@"D:\quelle\jahr.txt"),
            ablegen: _ => Task.FromResult(new AblageErgebnis(@"C:\ablage\jahr.txt")),
            mitSystem: _ => Task.FromResult(true));

        Knopf(cut, "Datei Auswählen...").Click();

        Assert.Equal(@"C:\ablage\jahr.txt", cut.Instance.Pfad);
        Assert.False(Knopf(cut, "Inhalt anzeigen...").HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>Befund W13-B9, behoben:</b> Ein Fehlschlag der Ablage kommt als Warnung
    /// — der Vorläufer verschluckte ihn mit <c>catch { }</c>. Der Import läuft
    /// dann mit der Originaldatei weiter.
    /// </summary>
    [Fact]
    public void Ein_Fehlschlag_der_Ablage_wird_gemeldet()
    {
        var cut = Aufbauen(
            dateiWaehlen: _ => Task.FromResult<string?>(@"D:\quelle\jahr.txt"),
            ablegen: _ => Task.FromResult(new AblageErgebnis("", "Zugriff verweigert")));

        Knopf(cut, "Datei Auswählen...").Click();

        Assert.Equal("Zugriff verweigert", cut.Instance.Meldung);
        Assert.Equal(@"D:\quelle\jahr.txt", cut.Instance.Pfad);
    }

    /// <summary>„Inhalt anzeigen…" reicht den Pfad an die Systemanwendung weiter.</summary>
    [Fact]
    public void Inhalt_anzeigen_reicht_den_Pfad_weiter()
    {
        string? gesehen = null;
        var cut = Aufbauen(
            dateiWaehlen: _ => Task.FromResult<string?>(@"D:\quelle\jahr.txt"),
            ablegen: p => Task.FromResult(new AblageErgebnis(p)),
            mitSystem: p => { gesehen = p; return Task.FromResult(true); });

        Knopf(cut, "Datei Auswählen...").Click();
        Knopf(cut, "Inhalt anzeigen...").Click();

        Assert.Equal(@"D:\quelle\jahr.txt", gesehen);
    }

    /// <summary>
    /// Der erfolgreiche Import meldet sich, lädt den Katalog neu und gibt die
    /// Datei frei. Der Vorläufer meldete GAR NICHTS — der Anwender sah nur die
    /// neue Zeile in der Liste.
    /// </summary>
    [Fact]
    public void Ein_erfolgreicher_Import_meldet_sich()
    {
        string? gelesen = null;
        var cut = Aufbauen(
            dateiWaehlen: _ => Task.FromResult<string?>(@"D:\quelle\jahr.txt"),
            ablegen: p => Task.FromResult(new AblageErgebnis(p)),
            einlesen: (p, _, __) =>
            {
                gelesen = p;
                return Task.FromResult(new WaermebedarfImportErgebnis
                {
                    Erfolgreich = true,
                    Bezeichner = "jahr",
                    Meldung = "Die Ganglinie \"jahr\" wurde mit 8760 Werten eingelesen."
                });
            });

        Knopf(cut, "Datei Auswählen...").Click();
        Knopf(cut, "Datei in DB Einlesen...").Click();

        Assert.Equal(@"D:\quelle\jahr.txt", gelesen);
        Assert.Contains("8760 Werten", cut.Instance.Meldung);
        Assert.Equal("", cut.Instance.Pfad);
    }

    /// <summary>Ein Fehler beim Import lässt die Datei stehen und meldet ihn.</summary>
    [Fact]
    public void Ein_Fehler_beim_Import_laesst_die_Datei_stehen()
    {
        var cut = Aufbauen(
            dateiWaehlen: _ => Task.FromResult<string?>(@"D:\quelle\jahr.txt"),
            ablegen: p => Task.FromResult(new AblageErgebnis(p)),
            einlesen: (_, __, ___) => Task.FromResult(new WaermebedarfImportErgebnis
            {
                Erfolgreich = false,
                Meldung = "Zeile 7 ist leer."
            }));

        Knopf(cut, "Datei Auswählen...").Click();
        Knopf(cut, "Datei in DB Einlesen...").Click();

        Assert.Equal("Zeile 7 ist leer.", cut.Instance.Meldung);
        Assert.Equal(@"D:\quelle\jahr.txt", cut.Instance.Pfad);
    }

    /// <summary>
    /// <b>Befund W13-B2, behoben:</b> Der Konfliktdialog erscheint als
    /// ÜBERLAGERUNG. Der Vorläufer prüfte mit <c>listBox.FindString</c> in der
    /// Anzeige und stieg bei einem Treffer STILL aus.
    /// </summary>
    [Fact]
    public void Der_Konfliktdialog_erscheint_als_Ueberlagerung()
    {
        var wartet = new TaskCompletionSource<bool>();

        var cut = Aufbauen(
            dateiWaehlen: _ => Task.FromResult<string?>(@"D:\quelle\jahr.txt"),
            ablegen: p => Task.FromResult(new AblageErgebnis(p)),
            einlesen: async (_, rueckrufe, __) =>
            {
                var pruefungen = new List<ImportPruefung>
                {
                    new() { Kandidat = new ImportKandidat { Name = "jahr", Tag = 0 },
                            Befund = ImportBefund.NameVorhanden,
                            Vorhanden = new KatalogSatz { Id = 1, Name = "jahr" } }
                };
                await rueckrufe.Konflikte(pruefungen, new HashSet<string> { "jahr" });
                wartet.TrySetResult(true);
                return new WaermebedarfImportErgebnis();
            });

        Knopf(cut, "Datei Auswählen...").Click();
        Knopf(cut, "Datei in DB Einlesen...").Click();

        Assert.Single(cut.FindAll("[role='dialog']"));
        Assert.Contains("Import: Konflikte prüfen", cut.Markup);
    }

    // =====================================================================
    // 4 — Tastatur und Schluss
    // =====================================================================

    [Fact]
    public void Esc_schliesst_den_Dialog()
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

        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        Knopf(cut, "DB Ganglinie Löschen").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Null(ergebnis);
    }

    [Fact]
    public void Beenden_schliesst_mit_OK()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        Knopf(cut, "Beenden").Click();

        Assert.True(ergebnis);
    }
}
