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

    // =====================================================================
    // Vorbelegung aus „Zum Bericht ›" der Wirtschaftlichkeitsseite
    // =====================================================================

    /// <summary>
    /// „Zum Bericht ›" reicht eine Vorbelegung mit: Die Seite hakt den Baustein
    /// Wirtschaftlichkeit an, übernimmt die Versionen der Vergleichsgruppe (nur solche der
    /// Gruppe, der Stamm bleibt) und nennt Zahl und Szenario in einer leisen Zeile. Die
    /// übrigen Häkchen des gespeicherten Standes bleiben.
    /// </summary>
    [Fact]
    public void Die_Vorbelegung_hakt_Wirtschaftlichkeit_an_und_uebernimmt_Versionen_und_Szenario()
    {
        var cut = Zeige(p => p.Add(x => x.Vorbelegung,
            new BerichtVorbelegung(true, new[] { 1032, 9999 }, 2, "Ungünstig")));

        Assert.Equal(new[] { 1030, 1032 }, cut.Instance.Gewaehlte.OrderBy(i => i).ToArray());
        var haken = Haken(cut);
        Assert.True(haken[0].HasAttribute("checked"));
        Assert.False(haken[1].HasAttribute("checked"));
        Assert.True(haken[2].HasAttribute("checked"));

        Assert.Contains(WIRTSCHAFT, cut.Instance.AktiveBausteine);
        Assert.Contains("KOPF", cut.Instance.AktiveBausteine);
        Assert.True(cut.FindAll(".epos-mehrfachauswahl-liste input[type=checkbox]")[2].HasAttribute("checked"));

        string zeile = cut.Find(".epos-bericht-vorbelegt").TextContent;
        Assert.Equal(cut.Instance.Vorbelegungszeile, zeile.Trim());
        Assert.Contains("2", zeile);
        Assert.Contains("Ungünstig", zeile);
    }

    /// <summary>
    /// Die Vorbelegung gilt EINMAL: Sie schreibt nichts (gemerkt wird erst mit „Erstellen"),
    /// und ein Auffrischen zeigt wieder den gespeicherten Stand ohne die leise Zeile. Ohne
    /// Vorbelegung steht die Zeile nicht da.
    /// </summary>
    [Fact]
    public async Task Die_Vorbelegung_gilt_einmal_und_ohne_sie_gilt_der_gespeicherte_Stand()
    {
        var ohne = Zeige();
        Assert.Empty(ohne.FindAll(".epos-bericht-vorbelegt"));
        Assert.DoesNotContain(WIRTSCHAFT, ohne.Instance.AktiveBausteine);

        var cut = Zeige(p => p.Add(x => x.Vorbelegung,
            new BerichtVorbelegung(true, new[] { 1030 }, 0, "Erwartet")));
        Assert.Equal(new[] { 1030 }, cut.Instance.Gewaehlte.ToArray());
        Assert.DoesNotContain(WIRTSCHAFT, _stand.AktiveBausteine);        // nichts geschrieben

        await cut.InvokeAsync(() => cut.Instance.Auffrischen());
        Assert.Equal(new[] { 1030, 1031, 1032 }, cut.Instance.Gewaehlte.OrderBy(i => i).ToArray());
        Assert.DoesNotContain(WIRTSCHAFT, cut.Instance.AktiveBausteine);
    }

    /// <summary>
    /// Der Auftrag von „Erstellen" nach einer Vorbelegung trägt Baustein und Versionen —
    /// der EINE Berichtsweg ist der dieser Seite.
    /// </summary>
    [Fact]
    public async Task Erstellen_nach_der_Vorbelegung_traegt_Baustein_und_Versionen()
    {
        BerichtAuftrag? auftrag = null;
        var cut = Zeige(p => p
            .Add(x => x.Vorbelegung, new BerichtVorbelegung(true, new[] { 1030, 1032 }, 0, "Erwartet"))
            .Add(x => x.Erstellen, (BerichtAuftrag a, Action<Laufschritt> m) =>
            {
                auftrag = a;
                return Task.FromResult(new LaufErgebnis { Erfolg = true });
            }));

        cut.FindAll(".epos-leiste button")[2].Click();                      // „Erstellen"
        await cut.InvokeAsync(() => cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click());   // Ja

        Assert.NotNull(auftrag);
        Assert.Equal(new[] { 1032 }, auftrag!.VariantenIds.ToArray());
        Assert.Contains(WIRTSCHAFT, auftrag.Bausteine);
    }

    // =====================================================================
    // Das Szenario des Wirtschaftlichkeitsberichts (Fachvorgabe E31, Nach #582)
    // =====================================================================

    private const string LABEL_SZENARIO = "Szenario der Wirtschaftlichkeit:";

    /// <summary>
    /// Der Standardstand mit den drei Szenarien der Hülle; gemerkt ist <paramref name="gemerkt"/>, mit
    /// <paramref name="wirtschaft"/> ist der Baustein Wirtschaftlichkeit angehakt.
    /// </summary>
    private static BerichtStand MitSzenarien(int gemerkt = 0, bool wirtschaft = false)
    {
        BerichtStand s = Standard();
        s.Szenarien = new[] { (0, "Erwartet"), (1, "Günstig"), (2, "Ungünstig") };
        s.SzenarioId = gemerkt;
        if (wirtschaft) s.AktiveBausteine = new[] { "KOPF", WIRTSCHAFT };
        return s;
    }

    /// <summary>Die Klappliste des Szenarios (das <c>select</c> des Auswahlfelds); <c>null</c> = keine.</summary>
    private static IElement? Szenarioliste(IRenderedComponent<BerichtSeite> cut)
        => cut.FindAll("label.epos-feld").FirstOrDefault(l => l.TextContent.Contains(LABEL_SZENARIO))
              ?.QuerySelector("select");

    /// <summary>
    /// Die Klappliste steht am Baustein Wirtschaftlichkeit — mit den drei Szenarien der Hülle, dem gemerkten gewählt,
    /// und nur, solange der Baustein angehakt ist. Ohne Szenarien der Hülle steht keine.
    /// </summary>
    [Fact]
    public void Die_Klappliste_steht_nur_am_angehakten_Baustein_Wirtschaftlichkeit()
    {
        Assert.Null(Szenarioliste(Zeige(stand: MitSzenarien(1))));             // Baustein nicht angehakt
        Assert.Null(Szenarioliste(Zeige(stand: Mit(Standard(), wirtschaft: true))));   // Hülle ohne Szenarien

        var cut = Zeige(stand: MitSzenarien(1, wirtschaft: true));
        IElement liste = Szenarioliste(cut)!;
        Assert.NotNull(liste);
        var optionen = liste.QuerySelectorAll("option");
        Assert.Equal(new[] { "Erwartet", "Günstig", "Ungünstig" }, optionen.Select(o => o.TextContent).ToArray());
        Assert.True(optionen[1].HasAttribute("selected"));
        Assert.Equal(1, cut.Instance.Szenariowahl);

        // Das Häkchen weg — die Klappliste weg; wieder angehakt, steht sie mit derselben Wahl.
        cut.FindAll(".epos-mehrfachauswahl-liste input[type=checkbox]")[2].Change(false);
        Assert.Null(Szenarioliste(cut));
        cut.FindAll(".epos-mehrfachauswahl-liste input[type=checkbox]")[2].Change(true);
        Assert.NotNull(Szenarioliste(cut));
        Assert.Equal(1, cut.Instance.Szenariowahl);
    }

    /// <summary>
    /// „Zum Bericht ›" reicht das Szenario der Einzelheiten mit: Es belegt die Klappliste vor (dieselben Nummern), die
    /// leise Zeile bleibt. Eine Nummer, die die Hülle nicht führt, lässt die gemerkte Wahl stehen.
    /// </summary>
    [Fact]
    public void Die_Vorbelegung_setzt_die_Klappliste()
    {
        var cut = Zeige(p => p.Add(x => x.Vorbelegung,
            new BerichtVorbelegung(true, new[] { 1030 }, 2, "Ungünstig")), MitSzenarien(0));

        Assert.Equal(2, cut.Instance.Szenariowahl);
        Assert.True(Szenarioliste(cut)!.QuerySelectorAll("option")[2].HasAttribute("selected"));
        Assert.Contains("Ungünstig", cut.Instance.Vorbelegungszeile);
        Assert.Single(cut.FindAll(".epos-bericht-vorbelegt"));

        var unbekannt = Zeige(p => p.Add(x => x.Vorbelegung,
            new BerichtVorbelegung(true, new[] { 1030 }, 7, "?")), MitSzenarien(1));
        Assert.Equal(1, unbekannt.Instance.Szenariowahl);
    }

    /// <summary>
    /// „Erstellen" reicht die Wahl der Klappliste im <see cref="BerichtAuftrag"/> an die Hülle; die Seite, neu aufgebaut
    /// aus dem gemerkten Stand, zeigt sie wieder. Der Assistent wählt über denselben Weg.
    /// </summary>
    [Fact]
    public async Task Erstellen_reicht_das_Szenario_und_der_Neuaufbau_zeigt_es()
    {
        BerichtAuftrag? auftrag = null;
        BerichtStand gemerkt = MitSzenarien(0, wirtschaft: true);
        var cut = Zeige(p => p.Add(x => x.Erstellen, (BerichtAuftrag a, Action<Laufschritt> m) =>
        {
            auftrag = a;
            gemerkt.SzenarioId = a.SzenarioId;            // so merkt es die Hülle (BerichtsKonfiguration.Szenario)
            return Task.FromResult(new LaufErgebnis { Erfolg = true });
        }), gemerkt);

        Szenarioliste(cut)!.Change("2");
        Assert.Equal(2, cut.Instance.Szenariowahl);

        cut.FindAll(".epos-leiste button")[2].Click();                      // „Erstellen"
        await cut.InvokeAsync(() => cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click());   // Ja
        Assert.NotNull(auftrag);
        Assert.Equal(2, auftrag!.SzenarioId);
        Assert.Contains(WIRTSCHAFT, auftrag.Bausteine);

        var neu = Zeige(stand: gemerkt);
        Assert.Equal(2, neu.Instance.Szenariowahl);
        Assert.True(Szenarioliste(neu)!.QuerySelectorAll("option")[2].HasAttribute("selected"));

        BerichtSeiteKiSicht sicht = neu.Instance.Assistentensicht;
        Assert.Equal(3, sicht.SzenarioWahl.Count);
        await neu.InvokeAsync(() => sicht.Szenario = 1);
        Assert.Equal(1, neu.Instance.Szenariowahl);
        Assert.Equal(1, sicht.Szenario);
    }

    // =====================================================================
    // VB‑E5: der vierte Eintrag „Alle drei Szenarien (VALERI)“ (VB‑Q1 a)
    // =====================================================================

    private const string VALERI = "Alle drei Szenarien (VALERI)";

    /// <summary>Der Stand mit den vier Einträgen der Hülle; gemerkt ist <paramref name="gemerkt"/>.</summary>
    private static BerichtStand MitVierEintraegen(int gemerkt = 0)
    {
        BerichtStand s = MitSzenarien(gemerkt, wirtschaft: true);
        s.Szenarien = new[] { (0, "Erwartet"), (1, "Günstig"), (2, "Ungünstig"), (BerichtStand.SZENARIO_VALERI, VALERI) };
        return s;
    }

    /// <summary>
    /// Die Klappliste zeigt die vier Einträge der Hülle; die Wahl des vierten geht als <see cref="BerichtStand.SZENARIO_VALERI"/>
    /// in den Auftrag, und der Neuaufbau aus dem gemerkten Stand (Rücklesen über die Darstellung) wählt ihn wieder. Der
    /// Assistent sieht vier Werte; die Vorbelegung mit dem vierten Eintrag wählt ihn.
    /// </summary>
    [Fact]
    public async Task Der_vierte_Eintrag_geht_in_den_Auftrag_und_wird_zurueckgelesen()
    {
        BerichtAuftrag? auftrag = null;
        BerichtStand gemerkt = MitVierEintraegen(1);
        var cut = Zeige(p => p.Add(x => x.Erstellen, (BerichtAuftrag a, Action<Laufschritt> m) =>
        {
            auftrag = a;
            gemerkt.SzenarioId = a.SzenarioId;            // so liest die Hülle die VALERI-Darstellung zurück
            return Task.FromResult(new LaufErgebnis { Erfolg = true });
        }), gemerkt);

        IElement liste = Szenarioliste(cut)!;
        Assert.Equal(new[] { "Erwartet", "Günstig", "Ungünstig", VALERI },
                     liste.QuerySelectorAll("option").Select(o => o.TextContent).ToArray());
        liste.Change(BerichtStand.SZENARIO_VALERI.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(BerichtStand.SZENARIO_VALERI, cut.Instance.Szenariowahl);

        cut.FindAll(".epos-leiste button")[2].Click();                      // „Erstellen"
        await cut.InvokeAsync(() => cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click());   // Ja
        Assert.Equal(BerichtStand.SZENARIO_VALERI, auftrag!.SzenarioId);

        var neu = Zeige(stand: gemerkt);
        Assert.Equal(BerichtStand.SZENARIO_VALERI, neu.Instance.Szenariowahl);
        Assert.True(Szenarioliste(neu)!.QuerySelectorAll("option")[3].HasAttribute("selected"));
        Assert.Equal(4, neu.Instance.Assistentensicht.SzenarioWahl.Count);

        var vorbelegt = Zeige(p => p.Add(x => x.Vorbelegung,
            new BerichtVorbelegung(true, new[] { 1030 }, BerichtStand.SZENARIO_VALERI, VALERI)), MitVierEintraegen(0));
        Assert.Equal(BerichtStand.SZENARIO_VALERI, vorbelegt.Instance.Szenariowahl);
        // Eigener Satz der VALERI-Darstellung (BK_BER_VORBELEGT_VALERI) statt des Satzes mit Szenarioname.
        string[] teile = WindowsFormsApplication1.MyResource.Resource.BK_BER_VORBELEGT_VALERI.Split("{0}");
        Assert.StartsWith(teile[0], vorbelegt.Instance.Vorbelegungszeile);
        Assert.EndsWith(teile[1], vorbelegt.Instance.Vorbelegungszeile);
        Assert.DoesNotContain(VALERI, vorbelegt.Instance.Vorbelegungszeile);

        // Gegenprobe: ein einzelnes Szenario behält den Satz mit dem Szenarionamen.
        var einzeln = Zeige(p => p.Add(x => x.Vorbelegung,
            new BerichtVorbelegung(true, new[] { 1030 }, 1, "Günstig")), MitVierEintraegen(0));
        Assert.Contains("Günstig", einzeln.Instance.Vorbelegungszeile);
        Assert.NotEqual(vorbelegt.Instance.Vorbelegungszeile, einzeln.Instance.Vorbelegungszeile);
    }

    /// <summary>Ein Stand mit angehaktem Baustein Wirtschaftlichkeit.</summary>
    private static BerichtStand Mit(BerichtStand s, bool wirtschaft)
    {
        if (wirtschaft) s.AktiveBausteine = new[] { "KOPF", WIRTSCHAFT };
        return s;
    }
}
