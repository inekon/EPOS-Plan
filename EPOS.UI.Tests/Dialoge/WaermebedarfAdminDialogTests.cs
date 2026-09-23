using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Verwaltung der externen Wärmebedarfsganglinien (iU9-W13.2). Soll ist die
/// Feldkarte von <c>Form_AdminWaermeeinlesen</c> (11 Steuerelemente: 5 Knöpfe,
/// 3 Beschriftungen, 2 Textfelder, 1 Liste) — seit Stufe 4 der Neuordnung der
/// Administrationsdialoge im Gerüst der Gerätekataloge: Liste mit Kästchen,
/// Auswahlleiste, Stammblatt (Ganglinie, Herkunft), das Einlesen als Überlagerung
/// hinter „Import…".
///
/// <para>Die Kultur ist auf de-DE gepinnt: Die Erwartungswerte sind deutsche
/// Beschriftungen, und der Windows-Läufer läuft mit englischer Oberfläche.</para>
/// </summary>
public class WaermebedarfAdminDialogTests : EposBunitContext
{
    /// <summary>
    /// Der Katalog als <see cref="Katalogfilterzeile"/> — seit Stufe S3.2
    /// (W14a-E-10) traegt die Liste Jahresarbeit und Spitze und steht im Baustein
    /// <c>Katalogliste</c>.
    /// </summary>
    private static IReadOnlyList<Katalogfilterzeile> Katalog() => new[]
    {
        Zeitreihenproben.Zeile(1, "Buerohaus 2024", jahresarbeitMwh: 6137.6, spitzeKw: 2206.0),
        Zeitreihenproben.Zeile(2, "Auslieferung Standard", geschuetzt: true,
                               jahresarbeitMwh: 4724.7, spitzeKw: 1098.0),
        Zeitreihenproben.Zeile(3, "Werkhalle Nord", jahresarbeitMwh: 65.4, spitzeKw: 47.6)
    };

    /// <summary>Das Bild des Jahresverlaufs — dasselbe Modell, das die Hülle baut.</summary>
    private static readonly Zeichenmodell MODELL = ChartRenderer.JahresverlaufModell(
        "", Enumerable.Range(0, 8760).Select(i => 100.0 + 50.0 * Math.Cos(2 * Math.PI * i / 8760.0)).ToArray(),
        "Leistung [kW]", Farbrolle.HEIZWAERME);

    public WaermebedarfAdminDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<WaermebedarfAdminDialog> Aufbauen(
        Func<string, Task<bool>>? hatZuordnung = null,
        Func<string, Task<bool>>? loeschen = null,
        Func<string, Task<string?>>? dateiWaehlen = null,
        Func<string, Task<AblageErgebnis>>? ablegen = null,
        Func<string, Task<bool>>? mitSystem = null,
        Func<string, GanglinienRaster, GanglinienImportRueckrufe,
             Task<GanglinienImportErgebnis>>? einlesen = null,
        Action<bool>? geschlossen = null,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? verwendung = null,
        Func<string, Task<Ganglinienansicht>>? ansicht = null,
        IReadOnlyList<Katalogfilterzeile>? katalog = null)
    {
        IReadOnlyList<Katalogfilterzeile> zeilen = katalog ?? Katalog();
        return Render<WaermebedarfAdminDialog>(p => p
            .Add(x => x.Katalogzeilen, () => Task.FromResult(zeilen))
            .Add(x => x.Katalogprofil, Zeitreihenproben.Profil(Zeitreihenart.Waermebedarf))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.HatProjektzuordnung, hatZuordnung ?? (_ => Task.FromResult(false)))
            .Add(x => x.Loeschen, loeschen ?? (_ => Task.FromResult(true)))
            .Add(x => x.DateiWaehlen, dateiWaehlen)
            .Add(x => x.Ablegen, ablegen)
            .Add(x => x.MitSystemOeffnen, mitSystem)
            .Add(x => x.Einlesen, einlesen ?? ((_, __, ___) => Task.FromResult(new GanglinienImportErgebnis())))
            .Add(x => x.Verwendung, verwendung is null
                ? null
                : () => Task.FromResult(verwendung))
            .Add(x => x.Ansicht, ansicht ?? (_ => Task.FromResult(new Ganglinienansicht(
                MODELL, new GanglinienKennzahlen(6137.6, 2206.0, 2782.2)))))
            .Add(x => x.Ordner, @"C:\Users\probe\AppData\Local\WP-Plan\Waermebedarf")
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));
    }

    private static IElement Knopf(IRenderedComponent<WaermebedarfAdminDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    /// <summary>„Import…" in der Fußleiste öffnet die Überlagerung „Datei einlesen" (V14).</summary>
    private static void ImportOeffnen(IRenderedComponent<WaermebedarfAdminDialog> cut)
    {
        cut.Find("button.epos-importknopf").Click();
        Assert.Single(cut.FindAll(".epos-einlesen"));
    }

    // =====================================================================
    // 1 — Feldbestand
    // =====================================================================

    /// <summary>
    /// Die fünf Knöpfe der Feldkarte, wörtlich: „Datei Auswählen…",
    /// „Inhalt anzeigen…", „Datei in DB Einlesen…", „DB Ganglinie Löschen",
    /// „Beenden". Seit Stufe 4 stehen sie an ihren drei Orten: „Löschen" in der
    /// Auswahlleiste, „Beenden" und „Import…" in der Fußleiste, die drei des Einlesens
    /// in der Überlagerung samt Ordnerpfad.
    /// </summary>
    [Fact]
    public void Die_Maske_zeigt_ihre_fuenf_Knoepfe_und_den_Ordner()
    {
        var cut = Aufbauen(dateiWaehlen: _ => Task.FromResult<string?>(""));

        Assert.Equal("Wärmebedarf Ganglinie", cut.Find(".epos-dialog-titel").TextContent);

        Assert.Contains("DB Ganglinie Löschen", cut.Find(".epos-auswahlleiste").TextContent);
        var fuss = cut.FindAll(".epos-katalog-dialog > .epos-leiste").Last();
        Assert.Equal(new[] { "Import…", "Beenden" },
                     fuss.QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray());
        Assert.Empty(cut.FindAll(".epos-einlesen"));

        ImportOeffnen(cut);
        string knoepfe = string.Join("|", cut.Find(".epos-einlesen").QuerySelectorAll("button")
                                             .Select(b => b.TextContent.Trim()));
        Assert.Contains("Datei Auswählen...", knoepfe);
        Assert.Contains("Inhalt anzeigen...", knoepfe);
        Assert.Contains("Datei in DB Einlesen...", knoepfe);
        Assert.Contains("Abbrechen", knoepfe);

        Assert.Contains("Datei Basis Ordner:", cut.Find(".epos-einlesen").TextContent);
        Assert.Contains(@"C:\Users\probe\AppData\Local\WP-Plan\Waermebedarf", cut.Markup);
    }

    /// <summary>
    /// <b>W9‑E‑3:</b> Der Hinweis nennt, was die gemeinsame Kette wirklich annimmt —
    /// CSV/Text, beide Raster, ein Wert je Zeile —, und der volle Wortlaut hängt am
    /// Infoknopf. Seit Stufe 4 steht er in der Überlagerung des Einlesens.
    /// </summary>
    [Fact]
    public void Der_Hinweis_nennt_beide_Raster_und_haengt_am_Infoknopf()
    {
        var cut = Aufbauen();
        ImportOeffnen(cut);

        Assert.Contains("8.760 Stunden- oder 35.040 Viertelstundenwerte", cut.Markup);
        Assert.Contains("ein Wert je Zeile", cut.Markup);

        IElement hinweis = cut.Find(".epos-formathinweis");
        Assert.NotNull(hinweis.QuerySelector("button"));
    }

    [Fact]
    public void Der_Katalog_steht_in_der_Liste()
    {
        var cut = Aufbauen();

        Assert.Equal(3, cut.FindAll(".epos-katalogliste tbody tr").Count);
        Assert.Contains("Buerohaus 2024", cut.Find(".epos-katalogliste tbody").TextContent);
        Assert.Contains("Werkhalle Nord", cut.Find(".epos-katalogliste tbody").TextContent);
    }

    // =====================================================================
    // 2 — Löschen
    // =====================================================================

    /// <summary>
    /// Beim Öffnen steht die erste Zeile im Stammblatt (Konzept 3.3) — Löschen ist
    /// frei; eine Auslieferungszeile sperrt es WEICH mit dem Grund im Kurztext.
    /// </summary>
    [Fact]
    public void Ohne_Auswahl_ist_der_Loeschknopf_gesperrt()
    {
        var cut = Aufbauen();

        Assert.Equal("Buerohaus 2024", cut.Instance.Gewaehlt);
        Assert.False(Knopf(cut, "DB Ganglinie Löschen").HasAttribute("aria-disabled"));

        Zeilenklick.Zeile(cut, 1);   // "Auslieferung Standard"
        IElement knopf = Knopf(cut, "DB Ganglinie Löschen");
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Equal("Auslieferungssatz – Löschen gesperrt.", knopf.GetAttribute("title"));

        // Ohne Katalog gibt es keine Fokuszeile und keine Loeschhandlung.
        var leer = Aufbauen(katalog: Array.Empty<Katalogfilterzeile>());
        Assert.DoesNotContain(leer.FindAll("button"), b => b.TextContent.Trim() == "DB Ganglinie Löschen");
        Assert.Equal("", leer.Instance.Gewaehlt);
    }

    /// <summary>
    /// <b>Prüfregel 1, wörtlich:</b> Eine zugeordnete Ganglinie bleibt stehen —
    /// und die Rückfrage kommt gar nicht erst. Kennt die Verwendungskarte die
    /// Zuordnung nicht, fragt der Dialog vor der Rückfrage die Datenbank
    /// (<c>HatProjektzuordnung</c>, Befund W13-B8).
    /// </summary>
    [Fact]
    public void Eine_zugeordnete_Ganglinie_bleibt_stehen()
    {
        bool geloescht = false;
        var cut = Aufbauen(hatZuordnung: _ => Task.FromResult(true),
                           loeschen: _ => { geloescht = true; return Task.FromResult(true); });

        Zeilenklick.Zeile(cut, 0);
        Knopf(cut, "DB Ganglinie Löschen").Click();

        Assert.Equal("Es existiert eine Projektzuordnung, Löschen nicht möglich!", cut.Instance.Meldung);
        Assert.False(geloescht);
        Assert.Empty(cut.FindAll("[role='dialog']"));
    }

    /// <summary>
    /// <b>Stufe 4 (Konzept 3.4):</b> Führt ein Projekt den Lastgang, ist „Löschen" WEICH
    /// gesperrt — und der Kurztext NENNT das Projekt; die Herkunft sagt es auch.
    /// </summary>
    [Fact]
    public void Eine_verwendete_Ganglinie_sperrt_Loeschen_mit_dem_Projektnamen()
    {
        bool geloescht = false;
        var cut = Aufbauen(loeschen: _ => { geloescht = true; return Task.FromResult(true); },
                           verwendung: new Dictionary<string, IReadOnlyList<string>>
                           {
                               ["Buerohaus 2024"] = new[] { "Projekt 1", "Heinestr 15" }
                           });

        IElement knopf = Knopf(cut, "DB Ganglinie Löschen");
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.False(knopf.HasAttribute("disabled"));
        Assert.Equal("In Projekten verwendet („Projekt 1“, „Heinestr 15“) – Löschen gesperrt; dort zuerst entfernen.",
                     knopf.GetAttribute("title"));

        knopf.Click();
        Assert.Contains("„Projekt 1“", cut.Instance.Meldung);
        Assert.False(geloescht);

        var herkunft = cut.FindAll(".epos-stammblattgruppe").Single(g => g.GetAttribute("aria-label") == "Herkunft");
        Assert.Contains("Projekt 1, Heinestr 15", herkunft.TextContent);
    }

    /// <summary>
    /// <b>Prüfregel 2:</b> Ein Auslieferungssatz bleibt stehen; seit Stufe 4 ist
    /// „Löschen" weich gesperrt, und der Klick nennt den Grund.
    /// </summary>
    [Fact]
    public void Ein_Auslieferungssatz_bleibt_stehen()
    {
        bool geloescht = false;
        var cut = Aufbauen(loeschen: _ => { geloescht = true; return Task.FromResult(true); });

        Zeilenklick.Zeile(cut, 1);   // "Auslieferung Standard"
        Knopf(cut, "DB Ganglinie Löschen").Click();

        Assert.Contains("Löschen gesperrt", cut.Instance.Meldung);
        Assert.False(geloescht);
    }

    /// <summary>
    /// <b>A-Zeile:</b> Vor dem Löschen wird gefragt — der Vorläufer löschte ohne
    /// jede Sicherheitsabfrage. Gelungenes nennt die Statuszeile.
    /// </summary>
    [Fact]
    public void Vor_dem_Loeschen_wird_gefragt()
    {
        string? geloescht = null;
        var cut = Aufbauen(loeschen: n => { geloescht = n; return Task.FromResult(true); });

        Zeilenklick.Zeile(cut, 0);
        Knopf(cut, "DB Ganglinie Löschen").Click();

        Assert.Single(cut.FindAll("[role='dialog']"));
        Assert.Contains("Die Ganglinie \"Buerohaus 2024\" aus dem Katalog löschen?", cut.Markup);
        Assert.Null(geloescht);

        Knopf(cut, "Nein").Click();
        Assert.Null(geloescht);

        Knopf(cut, "DB Ganglinie Löschen").Click();
        Knopf(cut, "Ja").Click();
        Assert.Equal("Buerohaus 2024", geloescht);
        Assert.Contains("wurde gelöscht", cut.Instance.Status);
    }

    /// <summary>
    /// <b>Mehrere Zeilen</b> (AD-Q9): Die Rückfrage nennt die gelöschten und die, die
    /// stehen bleiben — Auslieferung und Verwendung getrennt.
    /// </summary>
    [Fact]
    public void Loeschen_mehrerer_Zeilen_laesst_Auslieferung_und_Verwendung_stehen()
    {
        var geloescht = new List<string>();
        var cut = Aufbauen(loeschen: n => { geloescht.Add(n); return Task.FromResult(true); },
                           verwendung: new Dictionary<string, IReadOnlyList<string>>
                           {
                               ["Werkhalle Nord"] = new[] { "Projekt 1" }
                           });

        foreach (int i in new[] { 0, 1, 2 })
            cut.FindAll(".epos-katalogliste tbody td.epos-spalte-kaestchen input")[i].Change(true);
        Assert.StartsWith("3 gewählt", cut.Find(".epos-auswahlleiste-was").TextContent);

        Knopf(cut, "DB Ganglinie Löschen").Click();
        string frage = cut.FindComponent<Rueckfrage>().Instance.Frage;
        Assert.Contains("Buerohaus 2024", frage);
        Assert.Contains("Stehen bleiben (Auslieferungssatz): Auslieferung Standard", frage);
        Assert.Contains("Stehen bleiben (in Projekten verwendet): Werkhalle Nord", frage);

        Knopf(cut, "Ja").Click();
        Assert.Equal(new[] { "Buerohaus 2024" }, geloescht);
        Assert.Equal("1 gelöscht, 2 stehen geblieben", cut.Instance.Status);
    }

    // =====================================================================
    // 3 — Stammblatt (Stufe 4, V9)
    // =====================================================================

    /// <summary>
    /// <b>Das Stammblatt</b>: Kopf mit Jahresarbeit, Spitze und Volllaststunden; die
    /// Gruppen Ganglinie (das Bild des Kerns) und Herkunft (als Text).
    /// </summary>
    [Fact]
    public void Das_Stammblatt_traegt_Ganglinie_Herkunft_und_drei_Kennzahlen()
    {
        string? gefragt = null;
        var cut = Aufbauen(ansicht: n =>
        {
            gefragt = n;
            return Task.FromResult(new Ganglinienansicht(MODELL, new GanglinienKennzahlen(6137.6, 2206.0, 2782.2)));
        });

        Assert.Equal("Buerohaus 2024", gefragt);
        Assert.Equal("Buerohaus 2024", cut.Find(".epos-stammblatt-nametext").TextContent);
        Assert.Equal("eigener Satz", cut.Find(".epos-stammblatt-unter").TextContent);

        var kz = cut.FindAll(".epos-stammblatt-kennzahl");
        Assert.Equal(3, kz.Count);
        Assert.Equal("6.137,6 MWh", kz[0].QuerySelector("dd")!.TextContent);
        Assert.Equal("Jahresarbeit", kz[0].QuerySelector("dt")!.TextContent);
        Assert.Equal("2.782 h", kz[2].QuerySelector("dd")!.TextContent);

        var titel = cut.FindAll(".epos-stammblattgruppe-titel").Select(e => e.TextContent).ToList();
        Assert.Equal(new[] { "Ganglinie", "Herkunft" }, titel);
        Assert.Single(cut.FindComponents<DiagrammSvg>());
        Assert.Equal("wbad-ganglinie-1", cut.FindComponent<DiagrammSvg>().Instance.Kennung);

        var herkunft = cut.FindAll(".epos-stammblattgruppe").Single(g => g.GetAttribute("aria-label") == "Herkunft");
        Assert.Empty(herkunft.QuerySelectorAll("input, textarea, select"));
        Assert.Equal(new[] { "Satz", "Verwendet in", "Ablageordner" },
                     herkunft.QuerySelectorAll("dt").Select(e => e.TextContent).ToArray());
    }

    /// <summary>
    /// <b>„groß…"</b> öffnet dasselbe Modell breit, mit eigener Kennung; ohne Bild gibt es
    /// kein „groß…", sondern den Grund als Platzhalter.
    /// </summary>
    [Fact]
    public void Gross_zeigt_die_Ganglinie_breit_und_ohne_Bild_steht_der_Grund()
    {
        var cut = Aufbauen();
        Knopf(cut, "groß…").Click();

        var kennungen = cut.FindComponents<DiagrammSvg>().Select(d => d.Instance.Kennung).ToList();
        Assert.Equal(new[] { "wbad-ganglinie-1", "wbad-ganglinie-gross-1" }, kennungen);
        Assert.Equal("Ganglinie – Buerohaus 2024", cut.FindAll(".epos-ueberlagerung-titel").Last().TextContent);

        var ohne = Aufbauen(ansicht: _ => Task.FromResult(Ganglinienansicht.Ohne("Keine brauchbare Reihe.")));
        Assert.DoesNotContain(ohne.FindAll("button"), b => b.TextContent.Trim() == "groß…");
        Assert.Contains("Keine brauchbare Reihe.", ohne.Markup);
    }

    // =====================================================================
    // 4 — Datei wählen, anzeigen, einlesen (in der Überlagerung, V14)
    // =====================================================================

    /// <summary>
    /// <b>„Import…" öffnet die Überlagerung „Datei einlesen"</b> mit Titel und EINEM
    /// Kreuz; Kreuz, Esc und „Abbrechen" schließen sie, ohne den Dialog zu schließen.
    /// </summary>
    [Fact]
    public void Import_oeffnet_die_Ueberlagerung_und_Kreuz_Esc_Abbrechen_schliessen_sie()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        ImportOeffnen(cut);
        IElement ueberlagerung = cut.Find(".epos-ueberlagerung");
        Assert.Equal("Datei einlesen", ueberlagerung.QuerySelector(".epos-ueberlagerung-titel")!.TextContent);
        Assert.Single(ueberlagerung.QuerySelectorAll(".epos-ueberlagerung-zu"));

        cut.Find(".epos-ueberlagerung-zu").Click();
        Assert.False(cut.Instance.ImportOffen);

        ImportOeffnen(cut);
        cut.Find(".epos-ueberlagerung").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(cut.Instance.ImportOffen);

        ImportOeffnen(cut);
        cut.Find(".epos-einlesen").QuerySelectorAll("button").First(b => b.TextContent.Trim() == "Abbrechen").Click();
        Assert.False(cut.Instance.ImportOffen);
        Assert.Null(ergebnis);
    }

    /// <summary>Ohne Einleseweg kein „Import…" (Hausregel „Kein Delegat, kein Knopf").</summary>
    [Fact]
    public void Ohne_Einleseweg_steht_kein_Importknopf()
    {
        var cut = Render<WaermebedarfAdminDialog>(p => p
            .Add(x => x.Katalogzeilen, () => Task.FromResult(Katalog()))
            .Add(x => x.Katalogprofil, Zeitreihenproben.Profil(Zeitreihenart.Waermebedarf))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand()));

        Assert.Empty(cut.FindAll("button.epos-importknopf"));
    }

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
                           einlesen: (_, __, ___) => Task.FromResult(new GanglinienImportErgebnis()));
        ImportOeffnen(cut);

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
        ImportOeffnen(cut);

        Knopf(cut, "Datei Auswählen...").Click();

        Assert.Equal(@"C:\ablage\jahr.txt", cut.Instance.Pfad);
        Assert.False(Knopf(cut, "Inhalt anzeigen...").HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>Befund W13-B9, behoben:</b> Ein Fehlschlag der Ablage kommt als Warnung —
    /// seit Stufe 4 in der Überlagerung. Der Import läuft dann mit der Originaldatei
    /// weiter.
    /// </summary>
    [Fact]
    public void Ein_Fehlschlag_der_Ablage_wird_gemeldet()
    {
        var cut = Aufbauen(
            dateiWaehlen: _ => Task.FromResult<string?>(@"D:\quelle\jahr.txt"),
            ablegen: _ => Task.FromResult(new AblageErgebnis("", "Zugriff verweigert")));
        ImportOeffnen(cut);

        Knopf(cut, "Datei Auswählen...").Click();

        Assert.Equal("Zugriff verweigert", cut.Instance.Importmeldung);
        Assert.Contains("Zugriff verweigert", cut.Find(".epos-einlesen").TextContent);
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
        ImportOeffnen(cut);

        Knopf(cut, "Datei Auswählen...").Click();
        Knopf(cut, "Inhalt anzeigen...").Click();

        Assert.Equal(@"D:\quelle\jahr.txt", gesehen);
    }

    /// <summary>
    /// Der erfolgreiche Import schließt die Überlagerung, lädt den Katalog neu, wählt
    /// den neuen Lastgang und meldet sich in der Statuszeile (V14). Der Vorläufer
    /// meldete GAR NICHTS.
    /// </summary>
    [Fact]
    public void Ein_erfolgreicher_Import_meldet_sich()
    {
        string? gelesen = null;
        var liste = new List<Katalogfilterzeile>(Katalog());
        var cut = Aufbauen(
            katalog: liste,
            dateiWaehlen: _ => Task.FromResult<string?>(@"D:\quelle\jahr.txt"),
            ablegen: p => Task.FromResult(new AblageErgebnis(p)),
            einlesen: (p, _, __) =>
            {
                gelesen = p;
                liste.Add(Zeitreihenproben.Zeile(9, "jahr", jahresarbeitMwh: 10.0, spitzeKw: 5.0));
                return Task.FromResult(new GanglinienImportErgebnis
                {
                    Ausgang = ImportAusgang.Erfolg,
                    Bezeichner = "jahr",
                    Meldung = "Die Ganglinie \"jahr\" wurde mit 8760 Werten eingelesen."
                });
            });
        ImportOeffnen(cut);

        Knopf(cut, "Datei Auswählen...").Click();
        Knopf(cut, "Datei in DB Einlesen...").Click();

        Assert.Equal(@"D:\quelle\jahr.txt", gelesen);
        Assert.Contains("8760 Werten", cut.Instance.Status);
        Assert.Equal("", cut.Instance.Pfad);
        Assert.False(cut.Instance.ImportOffen);
        Assert.Equal("jahr", cut.Instance.Gewaehlt);
    }

    /// <summary>Ein Fehler beim Import lässt Datei und Überlagerung stehen und meldet ihn dort.</summary>
    [Fact]
    public void Ein_Fehler_beim_Import_laesst_die_Datei_stehen()
    {
        var cut = Aufbauen(
            dateiWaehlen: _ => Task.FromResult<string?>(@"D:\quelle\jahr.txt"),
            ablegen: p => Task.FromResult(new AblageErgebnis(p)),
            einlesen: (_, __, ___) => Task.FromResult(new GanglinienImportErgebnis
            {
                Ausgang = ImportAusgang.Fehler,
                MeldungStufe = PruefStufe.Fehler,
                Meldung = "Zeile 7 ist leer."
            }));
        ImportOeffnen(cut);

        Knopf(cut, "Datei Auswählen...").Click();
        Knopf(cut, "Datei in DB Einlesen...").Click();

        Assert.Equal("Zeile 7 ist leer.", cut.Instance.Importmeldung);
        Assert.Equal(@"D:\quelle\jahr.txt", cut.Instance.Pfad);
        Assert.True(cut.Instance.ImportOffen);
    }

    /// <summary>
    /// <b>Befund W13-B2, behoben:</b> Der Konfliktdialog erscheint als
    /// ÜBERLAGERUNG — seit Stufe 4 über der Überlagerung des Einlesens.
    /// </summary>
    [Fact]
    public void Der_Konfliktdialog_erscheint_als_Ueberlagerung()
    {
        var wartet = new TaskCompletionSource<bool>();

        var cut = Aufbauen(
            dateiWaehlen: _ => Task.FromResult<string?>(@"D:\quelle\jahr.txt"),
            ablegen: p => Task.FromResult(new AblageErgebnis(p)),
            einlesen: async (_, __, rueckrufe) =>
            {
                var pruefungen = new List<ImportPruefung>
                {
                    new() { Kandidat = new ImportKandidat { Name = "jahr", Tag = 0 },
                            Befund = ImportBefund.NameVorhanden,
                            Vorhanden = new KatalogSatz { Id = 1, Name = "jahr" } }
                };
                await rueckrufe.Konflikte!(pruefungen, new HashSet<string> { "jahr" });
                wartet.TrySetResult(true);
                return new GanglinienImportErgebnis();
            });
        ImportOeffnen(cut);

        Knopf(cut, "Datei Auswählen...").Click();
        Knopf(cut, "Datei in DB Einlesen...").Click();

        Assert.Equal(2, cut.FindAll("[role='dialog']").Count);
        Assert.Contains("Import: Konflikte prüfen", cut.Markup);
    }

    // =====================================================================
    // 5 — Tastatur und Schluss
    // =====================================================================

    /// <summary>Esc wirkt wie „Beenden" — EIN Schlussweg (Konzept Administrationsdialoge, V15).</summary>
    [Fact]
    public void Esc_schliesst_den_Dialog()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.True(ergebnis);
    }

    /// <summary>Das Kreuz im Dialogkopf wirkt wie Esc und wie „Beenden" (V15).</summary>
    [Fact]
    public void Kreuz_schliesst_wie_Esc()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog-zu").Click();

        Assert.True(ergebnis);
    }

    /// <summary>Esc schließt zuerst die Rückfrage, nicht den Dialog.</summary>
    [Fact]
    public void Esc_laesst_die_untere_Ebene_stehen()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        Zeilenklick.Zeile(cut, 0);
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
