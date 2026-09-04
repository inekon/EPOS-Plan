using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Gesetzeskatalog und sein Zeilendialog (iU9-W14c.1/W14c.2). Soll sind die
/// Feldkarten der gelöschten Masken <c>Form_Gesetzesparameter</c> (7 Zeilen:
/// Hinweis, Bereichsauswahl, sechsspaltige Liste, 4 Knöpfe) und
/// <c>Form_GesetzparameterZeile</c> (10 Zeilen: 4 Textfelder, 3 Auswahlfelder,
/// die graue Hinweiszeile, 2 Knöpfe).
///
/// <para><b>Diese Fälle ersetzen die drei Testdelegaten des Vorläufers.</b>
/// <c>FrageNeueZeile</c>, <c>FrageLoeschen</c> und <c>ZeileBearbeiten</c> waren
/// überschreibbare Eigenschaften auf einem <c>Control</c> — fünf WFO1000-Warnungen
/// „damit der Reflection-Harness beide Antworten prüfen kann". Einen solchen Test
/// gab es nie (Befund W14c-B14); hier klickt der Test unmittelbar auf
/// „Ja"/„Nein"/„Abbrechen".</para>
///
/// <para>Die Kultur ist auf de-DE gepinnt (Regel seit W8): Die Erwartungswerte sind
/// deutsche Beschriftungen, und der Windows-Läufer läuft mit englischer
/// Oberfläche.</para>
/// </summary>
public class GesetzeskatalogDialogTests : BunitContext
{
    private static readonly (string, string)[] KLASSEN =
    {
        ("KWKG", "KWK-Gesetz"),
        ("CO2_PREIS", "CO₂-Preis"),
        ("EEG", "EEG")
    };

    private static readonly (string, string)[] VORRAT =
    {
        ("KWKG", "KWK-Gesetz"),
        ("STROMSTEUER", "Stromsteuer"),
        ("CO2_PREIS", "CO₂-Preis")
    };

    private static readonly string[] EINHEITEN = { "EUR/MWh", "ct/kWh", "-" };
    private static readonly string[] STATUS = { "GESICHERT", "VORLAEUFIG", "PROGNOSE" };

    private static List<GesetzeskatalogDialog.Zeile> Zeilen() => new()
    {
        new(11, "KWKG_ZUSCHLAG_BIS50KW", "KWKG", 2023, "8", "ct/kWh", "GESICHERT", "KWKG 2023"),
        new(12, "KWKG_ZUSCHLAG_BIS50KW", "KWKG", 2026, "", "ct/kWh", "PROGNOSE", ""),
        new(13, "KWKG_VOLLBENUTZUNGSSTUNDEN", "KWKG", 2026, "3500", "h", "GESICHERT", "§ 8 KWKG")
    };

    public GesetzeskatalogDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;          // das Raster laedt sein JS-Modul
        DeutscheOberflaeche();
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Die Sprache der Oberfläche wird auf de-DE gepinnt (Regel seit W8).</summary>
    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
    }

    private IRenderedComponent<GesetzeskatalogDialog> Aufbauen(
        List<GesetzeskatalogDialog.Zeile>? zeilen = null,
        string vorwahl = "",
        Func<GesetzeskatalogZeileDialog.Zeilenwerte, Task<bool>>? anlegen = null,
        Func<GesetzeskatalogZeileDialog.Zeilenwerte, Task<bool>>? aendern = null,
        Func<int, Task<bool>>? loeschen = null,
        Func<GesetzeskatalogZeileDialog.Zeilenwerte, int, Task<string>>? pruefen = null,
        Action<bool>? geschlossen = null,
        int aktuellesJahr = 2026)
    {
        List<GesetzeskatalogDialog.Zeile> liste = zeilen ?? Zeilen();
        return Render<GesetzeskatalogDialog>(p => p
            .Add(x => x.Klassen, () => Task.FromResult(
                (IReadOnlyList<(string, string)>)KLASSEN.ToList()))
            .Add(x => x.Zeilen, k => Task.FromResult(
                (IReadOnlyList<GesetzeskatalogDialog.Zeile>)liste.Where(z => z.Klasse == k).ToList()))
            .Add(x => x.Klassenvorrat, VORRAT.ToList())
            .Add(x => x.Einheiten, EINHEITEN)
            .Add(x => x.Statuswerte, STATUS)
            .Add(x => x.Vorwahl, vorwahl)
            .Add(x => x.AktuellesJahr, aktuellesJahr)
            .Add(x => x.Anlegen, anlegen ?? (_ => Task.FromResult(true)))
            .Add(x => x.Aendern, aendern ?? (_ => Task.FromResult(true)))
            .Add(x => x.Loeschen, loeschen ?? (_ => Task.FromResult(true)))
            .Add(x => x.Pruefen, pruefen ?? ((_, _) => Task.FromResult("")))
            .Add(x => x.Geschlossen, geschlossen ?? (_ => { })));
    }

    // =====================================================================
    //  Feldbestand (Feldkarte Form_Gesetzesparameter, 7 Zeilen)
    // =====================================================================

    [Fact]
    public void Der_Katalog_zeigt_Hinweis_Bereichswahl_Liste_und_vier_Knoepfe()
    {
        var cut = Aufbauen();

        // lblHinweis: die Kernregel steht sichtbar auf der Maske.
        Assert.Contains("neue Jahreszeile", cut.Markup);

        // cbKlasse
        Assert.Single(cut.FindAll("select"));

        // Die SECHS Spalten der ListView plus die Wahlspalte.
        var spalten = cut.FindAll("th").Select(e => e.TextContent.Trim()).ToList();
        Assert.Contains("Schlüssel", spalten);
        Assert.Contains("Gültig ab", spalten);
        Assert.Contains("Wert", spalten);
        Assert.Contains("Einheit", spalten);
        Assert.Contains("Status", spalten);
        Assert.Contains("Quelle", spalten);

        // btnNeu, btnAendern, btnLoeschen, btnSchliessen - in dieser Reihenfolge.
        var knoepfe = cut.FindAll("div.epos-leiste button").Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(4, knoepfe.Count);
        Assert.StartsWith("Neu", knoepfe[0]);
        Assert.StartsWith("Ändern", knoepfe[1]);
        Assert.Equal("Löschen", knoepfe[2]);
        Assert.Equal("Schließen", knoepfe[3]);
    }

    /// <summary>Die Klassenliste kommt aus der DATENBANK — mit <c>EEG</c>.</summary>
    [Fact]
    public void Die_Bereichswahl_zeigt_die_Klassen_der_Datenbank()
    {
        var cut = Aufbauen();
        var eintraege = cut.FindAll("select option").Select(e => e.TextContent.Trim()).ToList();

        Assert.Equal(new[] { "KWK-Gesetz", "CO₂-Preis", "EEG" }, eintraege);
        Assert.Equal("KWKG", cut.Instance.GewaehlteKlasse);
    }

    /// <summary>
    /// Die Vorwahl — der Weg, den bis W14c <c>Sprungziel.GesetzesparameterCo2</c>
    /// ging (<c>f.GewaehlteKlasse = DbWerte.GESETZ_KLASSE_CO2_PREIS</c>).
    /// </summary>
    [Fact]
    public void Die_Vorwahl_stellt_den_Bereich_ein()
    {
        var cut = Aufbauen(vorwahl: "CO2_PREIS");

        Assert.Equal("CO2_PREIS", cut.Instance.GewaehlteKlasse);
    }

    [Fact]
    public void Eine_unbekannte_Vorwahl_faellt_auf_den_ersten_Bereich_zurueck()
    {
        var cut = Aufbauen(vorwahl: "GIBTESNICHT");

        Assert.Equal("KWKG", cut.Instance.GewaehlteKlasse);
    }

    [Fact]
    public void Die_Liste_zeigt_die_Zeilen_der_gewaehlten_Klasse()
    {
        var cut = Aufbauen();

        Assert.Equal(3, cut.Instance.ZeilenAnzahl);
        Assert.Contains("KWKG_VOLLBENUTZUNGSSTUNDEN", cut.Markup);
    }

    /// <summary>
    /// <b>Ein leeres Wertfeld bleibt leer</b> — es steht für „Satz entfallen" und ist
    /// etwas anderes als 0.
    /// </summary>
    [Fact]
    public void Eine_Zeile_ohne_Wert_zeigt_keine_Null()
    {
        var cut = Aufbauen();
        var zellen = cut.FindAll("td").Select(e => e.TextContent.Trim()).ToList();

        Assert.DoesNotContain("0", zellen.Where(z => z == "0"));
    }

    // =====================================================================
    //  A-13: Aendern und Loeschen haengen an der AUSWAHL (Befund W14c-B10)
    // =====================================================================

    [Fact]
    public void Ohne_Auswahl_sind_Aendern_und_Loeschen_gesperrt()
    {
        var cut = Aufbauen();
        var knoepfe = cut.FindAll("div.epos-leiste button");

        Assert.False(knoepfe[0].HasAttribute("disabled"));    // Neu geht immer
        Assert.True(knoepfe[1].HasAttribute("disabled"));     // Aendern
        Assert.True(knoepfe[2].HasAttribute("disabled"));     // Loeschen
        Assert.False(knoepfe[3].HasAttribute("disabled"));    // Schliessen
    }

    [Fact]
    public void Mit_Auswahl_sind_Aendern_und_Loeschen_frei()
    {
        var cut = Aufbauen();
        cut.FindAll("button.epos-anlagenwahl")[0].Click();

        var knoepfe = cut.FindAll("div.epos-leiste button");
        Assert.False(knoepfe[1].HasAttribute("disabled"));
        Assert.False(knoepfe[2].HasAttribute("disabled"));
    }

    // =====================================================================
    //  Der Zeilendialog als Ueberlagerung (Risiko R2)
    // =====================================================================

    [Fact]
    public void Neu_oeffnet_den_Zeilendialog_mit_dem_laufenden_Jahr()
    {
        var cut = Aufbauen(aktuellesJahr: 2027);
        cut.FindAll("div.epos-leiste button")[0].Click();       // Neu

        Assert.Single(cut.FindComponents<GesetzeskatalogZeileDialog>());
        GesetzeskatalogZeileDialog zeile = cut.FindComponent<GesetzeskatalogZeileDialog>().Instance;
        Assert.True(zeile.IstNeu);
        Assert.Equal(2027, zeile.JahrVon);
        Assert.Equal("KWKG", zeile.Klasse);           // die gewaehlte Klasse als Vorbelegung
        Assert.Equal(0, zeile.Id);
    }

    /// <summary>
    /// <b>Beim Ändern sind Schlüssel und Klasse gesperrt</b> — sie sind die Identität
    /// der Reihe und in der Datenbank eingefroren.
    /// </summary>
    [Fact]
    public void Aendern_oeffnet_den_Zeilendialog_gesperrt_auf_Schluessel_und_Klasse()
    {
        var cut = Aufbauen();
        cut.FindAll("button.epos-anlagenwahl")[0].Click();
        cut.FindAll("div.epos-leiste button")[1].Click();       // Aendern

        var zeile = cut.FindComponent<GesetzeskatalogZeileDialog>();
        Assert.False(zeile.Instance.IstNeu);
        Assert.Equal(11, zeile.Instance.Id);
        Assert.Equal("KWKG_ZUSCHLAG_BIS50KW", zeile.Instance.Schluessel);

        // Das Schluesselfeld ist nur lesend, die Klassenauswahl gesperrt.
        Assert.True(zeile.FindAll("input[readonly]").Count >= 1);
        Assert.True(zeile.FindAll("select[disabled]").Count >= 1);
    }

    // =====================================================================
    //  Die KERNREGEL: neue Jahreszeile? (drei Antworten)
    // =====================================================================

    /// <summary>
    /// Die Zeile gilt ab 2023, das laufende Jahr ist 2026 — die Rückfrage kommt, und
    /// „Ja" LEGT AN statt zu ändern.
    /// </summary>
    [Fact]
    public void Eine_Zeile_aus_der_Vergangenheit_fragt_und_Ja_legt_neu_an()
    {
        int angelegt = 0, geaendert = 0;
        var cut = Aufbauen(anlegen: _ => { angelegt++; return Task.FromResult(true); },
                           aendern: _ => { geaendert++; return Task.FromResult(true); });

        cut.FindAll("button.epos-anlagenwahl")[0].Click();       // die Zeile ab 2023
        cut.FindAll("div.epos-leiste button")[1].Click();       // Aendern
        cut.FindComponent<GesetzeskatalogZeileDialog>()
           .FindAll("button").First(b => b.TextContent.Trim() == "OK").Click();

        // Die Rueckfrage steht - mit DREI Knoepfen.
        var frage = cut.FindComponent<EPOS.UI.Bausteine.Rueckfrage>();
        Assert.True(frage.Instance.Offen);
        Assert.True(frage.Instance.MitAbbrechen);
        Assert.Contains("2023", frage.Instance.Frage);

        frage.FindAll("button").First(b => b.TextContent.Trim() == "Ja").Click();

        Assert.Equal(1, angelegt);
        Assert.Equal(0, geaendert);
    }

    [Fact]
    public void Nein_aendert_die_bestehende_Zeile()
    {
        int angelegt = 0, geaendert = 0;
        var cut = Aufbauen(anlegen: _ => { angelegt++; return Task.FromResult(true); },
                           aendern: _ => { geaendert++; return Task.FromResult(true); });

        cut.FindAll("button.epos-anlagenwahl")[0].Click();
        cut.FindAll("div.epos-leiste button")[1].Click();
        cut.FindComponent<GesetzeskatalogZeileDialog>()
           .FindAll("button").First(b => b.TextContent.Trim() == "OK").Click();
        cut.FindComponent<EPOS.UI.Bausteine.Rueckfrage>()
           .FindAll("button").First(b => b.TextContent.Trim() == "Nein").Click();

        Assert.Equal(0, angelegt);
        Assert.Equal(1, geaendert);
    }

    [Fact]
    public void Abbrechen_schreibt_gar_nichts()
    {
        int angelegt = 0, geaendert = 0;
        var cut = Aufbauen(anlegen: _ => { angelegt++; return Task.FromResult(true); },
                           aendern: _ => { geaendert++; return Task.FromResult(true); });

        cut.FindAll("button.epos-anlagenwahl")[0].Click();
        cut.FindAll("div.epos-leiste button")[1].Click();
        cut.FindComponent<GesetzeskatalogZeileDialog>()
           .FindAll("button").First(b => b.TextContent.Trim() == "OK").Click();
        cut.FindComponent<EPOS.UI.Bausteine.Rueckfrage>()
           .FindAll("button").First(b => b.TextContent.Trim() == "Abbrechen").Click();

        Assert.Equal(0, angelegt);
        Assert.Equal(0, geaendert);
    }

    /// <summary>
    /// Eine Zeile, die im laufenden Jahr beginnt, ist keine Gesetzesänderung — sie
    /// wird ohne Rückfrage geändert.
    /// </summary>
    [Fact]
    public void Eine_Zeile_aus_dem_laufenden_Jahr_fragt_nicht()
    {
        int geaendert = 0;
        var cut = Aufbauen(aendern: _ => { geaendert++; return Task.FromResult(true); });

        cut.FindAll("button.epos-anlagenwahl")[2].Click();       // die Zeile ab 2026
        cut.FindAll("div.epos-leiste button")[1].Click();
        cut.FindComponent<GesetzeskatalogZeileDialog>()
           .FindAll("button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.False(cut.FindComponent<EPOS.UI.Bausteine.Rueckfrage>().Instance.Offen);
        Assert.Equal(1, geaendert);
    }

    // =====================================================================
    //  Loeschen - mit Vorgabe "Nein" (A-1)
    // =====================================================================

    [Fact]
    public void Die_Loeschfrage_betont_Nein_und_nennt_Schluessel_und_Jahr()
    {
        var cut = Aufbauen();
        cut.FindAll("button.epos-anlagenwahl")[0].Click();
        cut.FindAll("div.epos-leiste button")[2].Click();       // Loeschen

        var fragen = cut.FindComponents<EPOS.UI.Bausteine.Rueckfrage>();
        var loeschfrage = fragen.First(f => f.Instance.VorgabeNein);
        Assert.True(loeschfrage.Instance.Offen);
        Assert.Contains("KWKG_ZUSCHLAG_BIS50KW", loeschfrage.Instance.Frage);
        Assert.Contains("2023", loeschfrage.Instance.Frage);

        // A-1: Der hervorgehobene Knopf ist "Nein", nicht "Ja".
        var knoepfe = loeschfrage.FindAll("button");
        Assert.DoesNotContain("epos-knopf--primaer",
            knoepfe.First(b => b.TextContent.Trim() == "Ja").ClassName);
        Assert.Contains("epos-knopf--primaer",
            knoepfe.First(b => b.TextContent.Trim() == "Nein").ClassName);
    }

    [Fact]
    public void Nein_loescht_nicht_und_Ja_loescht()
    {
        var geloescht = new List<int>();
        var cut = Aufbauen(loeschen: id => { geloescht.Add(id); return Task.FromResult(true); });

        cut.FindAll("button.epos-anlagenwahl")[0].Click();
        cut.FindAll("div.epos-leiste button")[2].Click();
        var loeschfrage = cut.FindComponents<EPOS.UI.Bausteine.Rueckfrage>().First(f => f.Instance.VorgabeNein);
        loeschfrage.FindAll("button").First(b => b.TextContent.Trim() == "Nein").Click();
        Assert.Empty(geloescht);

        cut.FindAll("div.epos-leiste button")[2].Click();
        loeschfrage = cut.FindComponents<EPOS.UI.Bausteine.Rueckfrage>().First(f => f.Instance.VorgabeNein);
        loeschfrage.FindAll("button").First(b => b.TextContent.Trim() == "Ja").Click();
        Assert.Equal(new[] { 11 }, geloescht);
    }

    /// <summary>Ein Fehlschlag beim Schreiben meldet sich im Banner statt zu schweigen.</summary>
    [Fact]
    public void Ein_Speicherfehler_meldet_sich()
    {
        var cut = Aufbauen(loeschen: _ => Task.FromResult(false));

        cut.FindAll("button.epos-anlagenwahl")[0].Click();
        cut.FindAll("div.epos-leiste button")[2].Click();
        cut.FindComponents<EPOS.UI.Bausteine.Rueckfrage>().First(f => f.Instance.VorgabeNein)
           .FindAll("button").First(b => b.TextContent.Trim() == "Ja").Click();

        Assert.NotEqual("", cut.Instance.Meldung);
    }

    // =====================================================================
    //  Schluss und Tastatur
    // =====================================================================

    /// <summary>„Schließen" liefert OK (Befund W14c-B11).</summary>
    [Fact]
    public void Schliessen_liefert_OK()
    {
        bool? antwort = null;
        var cut = Aufbauen(geschlossen: b => antwort = b);

        cut.FindAll("div.epos-leiste button")[3].Click();

        Assert.True(antwort);
    }

    [Fact]
    public void Esc_schliesst_den_Katalog_aber_nicht_ueber_eine_offene_Ebene()
    {
        bool? antwort = null;
        var cut = Aufbauen(geschlossen: b => antwort = b);

        // Mit offenem Zeilendialog bleibt der Katalog stehen.
        cut.FindAll("div.epos-leiste button")[0].Click();       // Neu
        cut.Find("div.epos-gesetzeskatalog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(antwort);

        // Ohne Ueberlagerung schliesst Esc - mit "abgebrochen".
        cut.FindComponent<GesetzeskatalogZeileDialog>()
           .FindAll("button").First(b => b.TextContent.Trim() == "Abbrechen").Click();
        cut.Find("div.epos-gesetzeskatalog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(antwort);
    }

    // =====================================================================
    //  Der Zeilendialog (Feldkarte Form_GesetzparameterZeile, 10 Zeilen)
    // =====================================================================

    [Fact]
    public void Der_Zeilendialog_zeigt_sieben_Felder_die_Hinweiszeile_und_zwei_Knoepfe()
    {
        var cut = Aufbauen();
        cut.FindAll("div.epos-leiste button")[0].Click();       // Neu
        var zeile = cut.FindComponent<GesetzeskatalogZeileDialog>();

        // 4 Textfelder (Schluessel, Jahr, Wert, Quelle) - Jahr und Wert sind
        // Ganzzahl- bzw. Zahlenfeld, also ebenfalls <input>.
        Assert.Equal(4, zeile.FindAll("input").Count);
        Assert.Equal(3, zeile.FindAll("select").Count);

        // lblWertLeer: die einzige Maske des Bestands, die diesen Unterschied
        // als eigene Beschriftung fuehrt.
        Assert.Contains("nicht 0", zeile.Markup);

        var knoepfe = zeile.FindAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Contains("OK", knoepfe);
        Assert.Contains("Abbrechen", knoepfe);

        // Befund W14c-B3, woertlich: KEIN Hilfeknopf in diesem Dialog.
        Assert.Empty(zeile.FindAll("button.epos-infoknopf"));
    }

    /// <summary>
    /// Der Zeilendialog bietet den KLASSENVORRAT an (acht Klassen), nicht die
    /// Klassen der Datenbank — Befund W14c-B5, wörtlich übernommen.
    /// </summary>
    [Fact]
    public void Der_Zeilendialog_bietet_den_Klassenvorrat_an()
    {
        var cut = Aufbauen();
        cut.FindAll("div.epos-leiste button")[0].Click();

        var zeile = cut.FindComponent<GesetzeskatalogZeileDialog>();
        var klassen = zeile.FindAll("select")[0].QuerySelectorAll("option")
                           .Select(o => o.TextContent.Trim()).ToList();

        Assert.Equal(new[] { "KWK-Gesetz", "Stromsteuer", "CO₂-Preis" }, klassen);
        Assert.DoesNotContain("EEG", klassen);
    }

    /// <summary>
    /// <b>Die Prüfung läuft im Zeilendialog und nur dort</b> (Befund W14c-B7): Ein
    /// Befund hält ihn offen und steht im Banner.
    /// </summary>
    [Fact]
    public void Ein_Pruefbefund_haelt_den_Zeilendialog_offen_und_meldet_sich()
    {
        int gerufen = 0, angelegt = 0;
        var cut = Aufbauen(
            pruefen: (_, _) => { gerufen++; return Task.FromResult("Bitte einen Schlüssel angeben."); },
            anlegen: _ => { angelegt++; return Task.FromResult(true); });

        cut.FindAll("div.epos-leiste button")[0].Click();
        var zeile = cut.FindComponent<GesetzeskatalogZeileDialog>();
        zeile.FindAll("button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.Equal(1, gerufen);
        Assert.Equal(0, angelegt);
        Assert.Single(cut.FindComponents<GesetzeskatalogZeileDialog>());       // steht noch
        Assert.Contains("Schlüssel", zeile.Instance.Meldung);
    }

    /// <summary>Beim Anlegen zählt keine eigene Id mit, beim Ändern die der Zeile.</summary>
    [Fact]
    public void Die_Pruefung_bekommt_beim_Aendern_die_eigene_Id()
    {
        var ids = new List<int>();
        var cut = Aufbauen(pruefen: (_, id) => { ids.Add(id); return Task.FromResult(""); });

        cut.FindAll("div.epos-leiste button")[0].Click();       // Neu
        cut.FindComponent<GesetzeskatalogZeileDialog>()
           .FindAll("button").First(b => b.TextContent.Trim() == "OK").Click();

        cut.FindAll("button.epos-anlagenwahl")[2].Click();       // Zeile ab 2026
        cut.FindAll("div.epos-leiste button")[1].Click();       // Aendern
        cut.FindComponent<GesetzeskatalogZeileDialog>()
           .FindAll("button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.Equal(new[] { 0, 13 }, ids);
    }

    /// <summary>
    /// Der Zeilendialog reicht beim ÄNDERN Schlüssel und Klasse unverändert durch —
    /// die Felder sind gesperrt, der Träger führt trotzdem den richtigen Wert.
    /// </summary>
    [Fact]
    public void Beim_Aendern_bleiben_Schluessel_und_Klasse_stehen()
    {
        GesetzeskatalogZeileDialog.Zeilenwerte? werte = null;
        var cut = Aufbauen(aendern: w => { werte = w; return Task.FromResult(true); });

        cut.FindAll("button.epos-anlagenwahl")[2].Click();       // Zeile ab 2026
        cut.FindAll("div.epos-leiste button")[1].Click();
        cut.FindComponent<GesetzeskatalogZeileDialog>()
           .FindAll("button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.NotNull(werte);
        Assert.Equal("KWKG_VOLLBENUTZUNGSSTUNDEN", werte!.Schluessel);
        Assert.Equal("KWKG", werte.Klasse);
        Assert.Equal(13, werte.Id);
        Assert.Equal(3500, werte.Wert);
    }

    /// <summary>„Abbrechen" im Zeilendialog schreibt nichts.</summary>
    [Fact]
    public void Abbrechen_im_Zeilendialog_schreibt_nichts()
    {
        int angelegt = 0;
        var cut = Aufbauen(anlegen: _ => { angelegt++; return Task.FromResult(true); });

        cut.FindAll("div.epos-leiste button")[0].Click();
        cut.FindComponent<GesetzeskatalogZeileDialog>()
           .FindAll("button").First(b => b.TextContent.Trim() == "Abbrechen").Click();

        Assert.Equal(0, angelegt);
        Assert.Empty(cut.FindComponents<GesetzeskatalogZeileDialog>());
    }
}
