using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using KiKern;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Die Seite „Bericht" (iU9-W5.2), Vorbild <c>Views/Bericht/UcBericht</c>
/// (15 Kartenzeilen).
///
/// <para>Soll ist die Feldkarte: Variantenliste mit vier Spalten und
/// „Alle"/„Keine", Bausteinliste, der Hinweis „Jeder Bericht rechnet neu",
/// Ausgabeformat (drei Optionen), Zielordner mit „Durchsuchen…",
/// „Erstellen", die Fortschrittsanzeige
/// und der Abbrechen-Knopf während eines Laufs.</para>
/// </summary>
public class BerichtSeiteTests : BunitContext
{
    public BerichtSeiteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // ---- Probendaten -----------------------------------------------------

    private const string WIRTSCHAFT = "WIRTSCHAFT";

    private static BerichtStand Standard() => new BerichtStand
    {
        Varianten = new[]
        {
            new VarianteZeile { IdProjekt = 1030, Art = "Stamm", Bezeichner = "(Stammprojekt)",
                                Projektname = "Musterhaus", SimStand = "02.09.2026 10:00",
                                Speicher = "mit Speicherflotte: Lastspitzenkappung · 2 Einheiten",
                                IstStamm = true },
            new VarianteZeile { IdProjekt = 1031, Art = "Variante", Bezeichner = "Kessel groß",
                                Projektname = "Musterhaus", SimStand = "" ,
                                Speicher = "ohne Stromspeicher", Auffaellig = true },
            new VarianteZeile { IdProjekt = 1032, Art = "Variante", Bezeichner = "WP klein",
                                Projektname = "Musterhaus", SimStand = "01.09.2026 08:00",
                                Speicher = "Einzelspeicher: Dauernutzung" }
        },
        GewaehlteVarianten = new[] { 1030, 1031, 1032 },
        Bausteine = new[]
        {
            new BausteinZeile { Schluessel = "KOPF", Titel = "Projektkopf" },
            new BausteinZeile { Schluessel = "ERGEBNISSE", Titel = "Ergebnisse je Variante" },
            new BausteinZeile { Schluessel = WIRTSCHAFT, Titel = "Wirtschaftlichkeit" }
        },
        AktiveBausteine = new[] { "KOPF" },
        AusgabeId = 0,
        Zielordner = @"C:\Berichte"
    };

    private BerichtStand _stand = Standard();
    private int _geladen;

    private IRenderedComponent<BerichtSeite> Zeige(
        Action<Bunit.ComponentParameterCollectionBuilder<BerichtSeite>>? mehr = null,
        BerichtStand? stand = null)
    {
        _stand = stand ?? Standard();
        _geladen = 0;
        return Render<BerichtSeite>(p =>
        {
            p.Add(x => x.Laden, () => { _geladen++; return _stand; });
            p.Add(x => x.BausteinWirtschaft, WIRTSCHAFT);
            mehr?.Invoke(p);
        });
    }

    /// <summary>Die Haken der Variantenliste (erste Spalte des Rasters).</summary>
    private static IReadOnlyList<IElement> Haken(IRenderedComponent<BerichtSeite> cut)
        => cut.FindAll(".epos-raster tbody input[type=checkbox]");

    // =====================================================================
    // Feldbestand (Feldkarte)
    // =====================================================================

    [Fact]
    public void Die_Seite_zeigt_beide_Listen_Ausgabe_Zielordner_und_die_Knoepfe()
    {
        var cut = Zeige(p => p.Add(x => x.TitelText, "Bericht — Stamm: Musterhaus"));

        // Den Titel traegt seit dem 08.09.2026 der Rahmen BerichteKostenSeite; die Seite
        // selbst zeigt keinen zweiten Kopf mehr (doppelter Titel in der Windows-Abnahme).
        Assert.Equal("Bericht — Stamm: Musterhaus", cut.Instance.TitelText);
        Assert.Empty(cut.FindAll(".epos-dialog-titel"));

        // Variantenliste: fuenf Spalten plus die Wahlspalte (US-2: Stromspeicher).
        Assert.Equal(6, cut.FindAll(".epos-raster thead th").Count);
        Assert.Equal(3, Haken(cut).Count);

        // Bausteinliste (Mehrfachauswahl, ohne Sammelknoepfe).
        Assert.Single(cut.FindAll(".epos-mehrfachauswahl"));
        Assert.Equal(3, cut.FindAll(".epos-mehrfachauswahl-liste input[type=checkbox]").Count);
        Assert.Empty(cut.FindAll(".epos-mehrfachauswahl .epos-leiste"));

        // Ausgabeformat: drei Optionen.
        Assert.Equal(3, cut.FindAll(".epos-optionsgruppe input[type=radio]").Count);

        // Zielordner mit Waehler.
        Assert.Single(cut.FindAll(".epos-dateiwahl"));

        // Alle / Keine / Erstellen.
        Assert.Equal(3, cut.FindAll(".epos-leiste button").Count);
    }

    [Fact]
    public void Die_Spaltenkoepfe_der_Variantenliste_stehen_wie_in_der_Karte()
    {
        var cut = Zeige();

        var koepfe = cut.FindAll(".epos-raster thead th");
        Assert.Equal("Art", koepfe[1].TextContent.Trim());
        Assert.Equal("Bezeichner", koepfe[2].TextContent.Trim());
        Assert.Equal("Projektname", koepfe[3].TextContent.Trim());
        Assert.Equal("Stromspeicher", koepfe[4].TextContent.Trim());
        Assert.Equal("Simulation", koepfe[5].TextContent.Trim());
    }

    /// <summary>
    /// AUFTRAG US-2: <b>Die Variantenliste sagt je Zeile, womit sie ihren Stromspeicher
    /// rechnet</b> — dieselbe Spalte wie auf der Wirtschaftlichkeit (#320). Auf dieser
    /// Seite entscheidet der Anwender, WELCHE Versionen in den Bericht kommen; ohne die
    /// Spalte sieht eine Version ohne Speicher aus wie eine mit.
    /// </summary>
    [Fact]
    public void Die_Variantenliste_nennt_je_Zeile_den_Speicherkontext()
    {
        var cut = Zeige();

        string tabelle = cut.Find(".epos-raster").TextContent;
        Assert.Contains("mit Speicherflotte: Lastspitzenkappung · 2 Einheiten", tabelle);
        Assert.Contains("ohne Stromspeicher", tabelle);
        Assert.Contains("Einzelspeicher: Dauernutzung", tabelle);
    }

    [Fact]
    public void Der_Hinweis_Jeder_Bericht_rechnet_neu_steht_als_Herleitung()
    {
        var cut = Zeige();

        Assert.Contains("rechnet neu", cut.Find(".epos-herleitung-text").TextContent);
    }

    [Fact]
    public void Ein_fehlender_Simulationsstand_wird_hervorgehoben()
    {
        var cut = Zeige();

        Assert.Single(cut.FindAll(".epos-veraltet"));
    }

    [Fact]
    public void Ohne_Ordnerwaehler_bleibt_der_Durchsuchen_Knopf_weg()
    {
        var cut = Zeige();

        Assert.Empty(cut.FindAll(".epos-dateiwahl button"));
    }

    [Fact]
    public void Mit_Ordnerwaehler_erscheint_der_Knopf_und_setzt_den_Pfad()
    {
        var cut = Zeige(p => p.Add(x => x.OrdnerWaehler,
            (string _) => Task.FromResult<string?>(@"D:\Neu")));

        cut.Find(".epos-dateiwahl button").Click();

        Assert.Equal(@"D:\Neu", cut.Find(".epos-dateiwahl input").GetAttribute("value"));
    }

    // =====================================================================
    // Vorbelegung und Auswahl
    // =====================================================================

    [Fact]
    public void Die_Seite_laedt_beim_Aufbau_genau_einmal()
    {
        var cut = Zeige();

        Assert.Equal(1, _geladen);
        Assert.Equal(3, cut.Instance.Gewaehlte.Count);
    }

    [Fact]
    public void Der_Haken_der_Stammzeile_ist_gesperrt()
    {
        var cut = Zeige();

        Assert.True(Haken(cut)[0].HasAttribute("disabled"));
        Assert.False(Haken(cut)[1].HasAttribute("disabled"));
    }

    [Fact]
    public void Ein_Abwaehlen_nimmt_die_Variante_aus_der_Gruppe()
    {
        var cut = Zeige();

        Haken(cut)[1].Change(false);

        Assert.DoesNotContain(1031, cut.Instance.Gewaehlte);
        Assert.Contains(1030, cut.Instance.Gewaehlte);
    }

    [Fact]
    public void Keine_laesst_den_Stamm_stehen()
    {
        var cut = Zeige();

        cut.FindAll(".epos-leiste button")[1].Click();   // „Keine"

        Assert.Single(cut.Instance.Gewaehlte);
        Assert.Contains(1030, cut.Instance.Gewaehlte);
    }

    [Fact]
    public void Alle_haakt_wieder_alles_an()
    {
        var cut = Zeige();

        cut.FindAll(".epos-leiste button")[1].Click();   // Keine
        cut.FindAll(".epos-leiste button")[0].Click();   // Alle

        Assert.Equal(3, cut.Instance.Gewaehlte.Count);
    }

    [Fact]
    public void Die_aktiven_Bausteine_stehen_angehakt()
    {
        var cut = Zeige();

        var haken = cut.FindAll(".epos-mehrfachauswahl-liste input[type=checkbox]");
        Assert.True(haken[0].HasAttribute("checked"));
        Assert.False(haken[2].HasAttribute("checked"));
    }

    [Fact]
    public void Das_Anhaken_der_Wirtschaftlichkeit_meldet_den_Hinweis()
    {
        var cut = Zeige(p => p.Add(x => x.MeldungWirtschaftHinweis,
                                   "Der Berichtslauf rechnet sie selbst mit."));

        cut.FindAll(".epos-mehrfachauswahl-liste input[type=checkbox]")[2].Change(true);

        Assert.Contains("rechnet sie selbst mit", cut.Instance.Status);
    }

    // =====================================================================
    // Erstellen
    // =====================================================================

    [Fact]
    public void Erstellen_fragt_erst_nach_und_nennt_die_Anzahl()
    {
        var cut = Zeige(p => p
            .Add(x => x.FrageStart, "{0} Version(en) neu rechnen?")
            .Add(x => x.Erstellen, (BerichtAuftrag a, Action<Laufschritt> m)
                => Task.FromResult(new LaufErgebnis { Erfolg = true })));

        cut.FindAll(".epos-leiste button")[2].Click();   // „Erstellen"

        Assert.Single(cut.FindAll(".epos-rueckfrage"));
        Assert.Contains("3 Version(en)", cut.Find(".epos-rueckfrage-text").TextContent);
    }

    [Fact]
    public void Nein_auf_die_Rueckfrage_startet_nichts()
    {
        int laeufe = 0;
        var cut = Zeige(p => p.Add(x => x.Erstellen, (BerichtAuftrag a, Action<Laufschritt> m) =>
        {
            laeufe++;
            return Task.FromResult(new LaufErgebnis { Erfolg = true });
        }));

        cut.FindAll(".epos-leiste button")[2].Click();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[1].Click();   // Nein

        Assert.Equal(0, laeufe);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
    }

    [Fact]
    public void Der_Auftrag_traegt_Varianten_ohne_Stamm_Bausteine_Format_und_Ordner()
    {
        BerichtAuftrag? auftrag = null;
        var cut = Zeige(p => p.Add(x => x.Erstellen, (BerichtAuftrag a, Action<Laufschritt> m) =>
        {
            auftrag = a;
            return Task.FromResult(new LaufErgebnis { Erfolg = true, Statuszeile = "fertig" });
        }));

        cut.FindAll(".epos-optionsgruppe input[type=radio]")[2].Change(true);   // „Beide"
        cut.FindAll(".epos-leiste button")[2].Click();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();          // Ja

        Assert.NotNull(auftrag);
        Assert.Equal(new[] { 1031, 1032 }, auftrag!.VariantenIds);
        Assert.Equal(new[] { "KOPF" }, auftrag.Bausteine);
        Assert.Equal(2, auftrag.AusgabeId);
        Assert.Equal(@"C:\Berichte", auftrag.Zielordner);
        Assert.Equal(3, auftrag.AnzahlMitStamm);
    }

    [Fact]
    public void Der_Fortschritt_erscheint_und_die_Statuszeile_zaehlt_mit()
    {
        var cut = Zeige(p => p.Add(x => x.Erstellen, (BerichtAuftrag a, Action<Laufschritt> m) =>
        {
            m(new Laufschritt(2, 4, "Variante 1"));
            return Task.FromResult(new LaufErgebnis { Erfolg = true, Statuszeile = "fertig" });
        }));

        cut.FindAll(".epos-leiste button")[2].Click();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();

        // Nach dem Lauf steht die Schlussmeldung; der Balken ist weg.
        Assert.Empty(cut.FindAll(".epos-fortschritt"));
        Assert.Equal("fertig", cut.Instance.Status);
    }

    [Fact]
    public void Nach_dem_Lauf_wird_die_Liste_neu_gelesen()
    {
        var cut = Zeige(p => p.Add(x => x.Erstellen, (BerichtAuftrag a, Action<Laufschritt> m)
            => Task.FromResult(new LaufErgebnis { Erfolg = true, Statuszeile = "fertig" })));

        cut.FindAll(".epos-leiste button")[2].Click();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();

        Assert.Equal(2, _geladen);   // Aufbau + nach dem Lauf
    }

    [Fact]
    public void Nach_dem_Lauf_kommt_keine_zweite_Rueckfrage_zum_Oeffnen()
    {
        // Geöffnet wird über „Öffnen" an der Erfolgszeile (BerichtSeiteErgebnisTests) —
        // eine Frage der Hülle stellt die Seite nicht.
        string? geoeffnet = null;
        var cut = Zeige(p => p
            .Add(x => x.DateiOeffnen, (string d) => { geoeffnet = d; return Task.CompletedTask; })
            .Add(x => x.Erstellen, (BerichtAuftrag a, Action<Laufschritt> m)
                => Task.FromResult(new LaufErgebnis
                {
                    Erfolg = true,
                    Statuszeile = "erstellt",
                    Meldung = @"C:\Berichte\Bericht.docx",
                    Frage = "Bericht jetzt öffnen?",
                    Datei = @"C:\Berichte\Bericht.docx"
                })));

        cut.FindAll(".epos-leiste button")[2].Click();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();   // Ja zum Start

        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
        Assert.Null(geoeffnet);
    }

    [Fact]
    public void Ein_Fehler_erscheint_als_Warnbanner()
    {
        var cut = Zeige(p => p.Add(x => x.Erstellen, (BerichtAuftrag a, Action<Laufschritt> m)
            => Task.FromResult(new LaufErgebnis { Fehler = "Word war nicht erreichbar." })));

        cut.FindAll(".epos-leiste button")[2].Click();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();

        Assert.Contains("Word war nicht erreichbar", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Ein_Abbruch_meldet_sich_in_der_Statuszeile()
    {
        var cut = Zeige(p => p
            .Add(x => x.StatusAbgebrochen, "Vorgang abgebrochen.")
            .Add(x => x.Erstellen, (BerichtAuftrag a, Action<Laufschritt> m)
                => Task.FromResult(new LaufErgebnis { Abgebrochen = true })));

        cut.FindAll(".epos-leiste button")[2].Click();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();

        Assert.Equal("Vorgang abgebrochen.", cut.Instance.Status);
    }

    [Fact]
    public void Der_Hilfeknopf_traegt_den_Schluessel_der_alten_Maske()
    {
        var cut = Zeige();

        Assert.Equal("UcBericht.btn_Help", cut.Instance.HilfeSchluessel);
    }

    // =====================================================================
    //  Das Formularraster — Anwenderwunsch iU8-E-2 / W14a-E-7, Paket P2
    //  (Windows-Abnahme 05.09.2026)
    // =====================================================================


    /// <summary>
    /// <b>iU8-E-2 / W14a-E-7 (Paket P2):</b> Das Pfadfeld des Zielordners steht im
    /// <c>Formularraster</c>, EINSPALTIG — ein Pfad braucht die ganze Breite. Die
    /// Kopfzeilen der Seite (<c>epos-seite-zeile</c>) bleiben Zeilen: Sie tragen
    /// die Werkzeuge der Seite, keinen Formularblock.
    /// </summary>
    [Fact]
    public void Der_Zielordner_steht_im_einspaltigen_Formularraster()
    {
        var cut = Zeige();

        Assert.Single(cut.FindAll(".epos-formularraster.epos-formularraster--einspaltig"));
        Assert.Single(cut.FindAll(".epos-formularraster .epos-feld"));

        // Die Seitenzeilen bleiben ausserhalb.
        Assert.Empty(cut.FindAll(".epos-formularraster .epos-seite-zeile"));
    }

    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F6)
    // =====================================================================

    /// <summary>
    /// <b>Der ZEUGE dieser Seite an der Maskenbrücke.</b> Ausgabeform und Zielordner
    /// sind Einstellwerte; die zwei Mengen stehen als Aufstellung und lassen sich
    /// nicht setzen.
    /// </summary>
    [Fact]
    public void Die_Seite_meldet_sich_beim_Assistenten_an_und_setzt_die_Ausgabeform()
    {
        var cut = Zeige();

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.BERICHTSEITE));

        KiFeldzugang ausgabe = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.BERICHTSEITE, "ausgabe");
        Assert.NotNull(ausgabe);
        Assert.True(ausgabe.Setzbar);
        Assert.Equal(3, ausgabe.Wahleintraege().Count);

        KiFeldumsetzung wahl = KiFeldwandler.Wandle(ausgabe, "1");
        Assert.True(wahl.Ok, wahl.Grund);
        ausgabe.Setzen(wahl.Wert);
        cut.Render();
        Assert.Equal(1, _stand.AusgabeId);

        KiFeldzugang ziel = KiMaskenbruecke.Feldzugang(KiMaskennamen.BERICHTSEITE, "zielordner");
        ziel.Setzen(@"D:\Ausgabe");
        cut.Render();
        Assert.Equal(@"D:\Ausgabe", _stand.Zielordner);

        // Die zwei MENGEN sind Anzeigen: Sie nennen, was angehakt ist.
        KiFeldzugang varianten = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.BERICHTSEITE, "varianten");
        Assert.False(varianten.Setzbar);
        Assert.Contains("Musterhaus", Convert.ToString(varianten.Lesen()) ?? "");

        KiFeldzugang bausteine = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.BERICHTSEITE, "bausteine");
        Assert.False(bausteine.Setzbar);
    }
}
