using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Energieträgerverwaltung (iU9-W4.4) — Vorbild
/// <c>Views/Kosten/Form_Energietraeger</c> mit der Trägerkarte
/// <c>ucFuelSettings</c>.
///
/// <para>Soll ist die Feldkarte: Kopf mit Kontextzeile, die Trägerliste unter
/// ihren Gruppen, die Knopfleiste je Kontext, die Trägerkarte mit ihren vier
/// Abschnitten und die Schlussleiste. Dazu die Unterdialoge, die seit dieser
/// Welle in einer Überlagerung stehen.</para>
/// </summary>
public class EnergietraegerDialogTests : EposBunitContext
{
    public EnergietraegerDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // ---- Probendaten -----------------------------------------------------

    private static readonly EnergietraegerDialog.EnergietraegerListe[] LISTE =
    {
        new(null, "Gas"),
        new(11, "Erdgas H"),
        new(12, "Flüssiggas"),
        new(null, "Strom"),
        new(21, "Elektrische Energie")
    };

    private static EnergietraegerStand Stand(bool strom = false)
    {
        var s = new EnergietraegerStand
        {
            TraegerZeile = "Erdgas H  (VDI 3805 3)",
            GruppeZeile = "Gruppe: Gas",
            Arbeitspreis = 0.65,
            Leistungspreis = 12,
            Grundpreis = 120,
            Heizwert = 10.1,
            Brennwert = 11.2,
            MitHeizwert = true,
            MitBrennwert = true,
            MitLeistungspreis = true,
            MitFormel = true,
            EinheitArbeitspreis = "€/Nm³",
            EinheitHeizwert = "kWh/Nm³",
            EinheitBrennwert = "kWh/Nm³",
            EinheitLeistungspreis = "€/(kW·a)",
            Basiseinheit = "Nm³",
            Preisbasen = new[] { (0, "Nm³"), (1, "kWh") },
            PreisbasisId = 0,
            PreisJeKwh = "0,0644 €",
            FormelText = "0,65 € ÷ 10,10 kWh = 0,0644 €/kWh",
            Regeln = new[]
            {
                new UmrechnungsregelZeile { Nummer = 0, Name = "Z-Faktor", Von = "Nm³",
                                            Nach = "kWh", Faktor = 10.1, Aktiv = true }
            },
            EffektivText = "effektiv: 1 Nm³ = 10,10 kWh (Hi)",
            EmissionenVerfuegbar = true,
            ModusOrt = "[globale Vorgabe]",
            Emissionszeilen = new[]
            {
                new EmissionsFeldZeile { Kuerzel = "CO2", Name = "Kohlendioxid",
                                         Einheit = "g/kWh", Wert = 201, Herkunft = "GEMIS 5.0" },
                new EmissionsFeldZeile { Kuerzel = "CH4", Name = "Methan", Einheit = "mg/kWh",
                                         Wert = 0.5, Herkunft = "Katalog", NurLesend = true }
            },
            EmissionsSumme = "CO₂-Äquivalent gesamt (ausgewählte Arten): 205,00 g/kWh",
            GueltigAb = new DateOnly(2026, 9, 3),
            Historie = new[]
            {
                new PreishistorieZeile("01.01.2026", "10,10", "Nm³", "0,62", "120,00", "12,00", 77)
            }
        };
        if (strom)
        {
            s.Aufschlaege = new StromAufschlaegeStand
            { Wahl = StromAufschlagWahl.Aufgeschluesselt };
            s.MitAufschlagSchalter = true;
            s.EffektivpreisText = "Bezugspreis inkl. Aufschläge: 21,50 ct/kWh";
        }
        return s;
    }

    private EnergietraegerAnsicht _ansicht = new();

    /// <summary>Der zuletzt geladene Träger (Prüfhilfe).</summary>
    private int _geladen;

    private IRenderedComponent<EnergietraegerDialog> Zeige(
        Action<Bunit.ComponentParameterCollectionBuilder<EnergietraegerDialog>>? mehr = null,
        EnergietraegerAnsicht? ansicht = null, bool katalog = true)
    {
        _ansicht = ansicht ?? new EnergietraegerAnsicht { Stand = Stand() };
        return Render<EnergietraegerDialog>(p =>
        {
            p.Add(x => x.Liste, LISTE);
            p.Add(x => x.Katalogkontext, katalog);
            p.Add(x => x.TraegerLaden, id => { _geladen = id; return _ansicht; });
            p.Add(x => x.Nachrechnen, () => _ansicht);
            mehr?.Invoke(p);
        });
    }

    // =====================================================================
    // Feldbestand
    // =====================================================================

    [Fact]
    public void Der_Dialog_zeigt_Kopf_Liste_Karte_und_Schlussleiste()
    {
        var cut = Zeige(p => p
            .Add(x => x.TitelText, "Energieträgerverwaltung")
            .Add(x => x.KontextText, "Kontext: Katalog (Stammdaten)"));

        Assert.Equal("Energieträgerverwaltung", cut.Find(".epos-dialog-titel").TextContent);
        Assert.StartsWith("Kontext: Katalog", cut.Find(".epos-kontextzeile").TextContent);
        Assert.Single(cut.FindAll(".epos-traeger-liste"));
        Assert.Single(cut.FindAll(".epos-traegerkarte"));
    }

    [Fact]
    public void Die_Liste_zeigt_Gruppenkoepfe_und_Traeger_getrennt()
    {
        var cut = Zeige();

        Assert.Equal(2, cut.FindAll(".epos-traeger-gruppenkopf").Count);
        Assert.Equal(3, cut.FindAll(".epos-traeger-eintrag").Count);
        Assert.Equal("▪ Gas", cut.FindAll(".epos-traeger-gruppenkopf")[0].TextContent.Trim());
    }

    [Fact]
    public void Der_erste_Traeger_ist_vorgewaehlt()
    {
        var cut = Zeige();

        Assert.Equal(11, cut.Instance.Traeger);
        Assert.Equal("true", cut.FindAll(".epos-traeger-eintrag")[0].GetAttribute("aria-pressed"));
    }

    [Fact]
    public void Eine_Vorwahl_trifft_den_genannten_Traeger()
    {
        var cut = Zeige(p => p.Add(x => x.TraegerVorwahl, 21));

        Assert.Equal(21, cut.Instance.Traeger);
    }

    // =====================================================================
    // Suche und Filter der Traegerliste (Anwenderwunsch 04.09.2026,
    // Windows-Abnahme; Vorbild KatalogImportDialog, W13)
    // =====================================================================

    /// <summary>Tippt in das Filterfeld über der Trägerliste.</summary>
    private static void Suchen(IRenderedComponent<EnergietraegerDialog> cut, string text)
        => cut.Find(".epos-traeger-liste input[type=text]").Input(text);

    /// <summary>
    /// Die Beschriftungen der sichtbaren Trägerzeilen — ohne das Wahlzeichen,
    /// das der Baustein <c>Zeilenwahl</c> voranstellt.
    /// </summary>
    private static string[] Eintraege(IRenderedComponent<EnergietraegerDialog> cut)
        => cut.FindAll(".epos-traeger-eintrag")
              .Select(e => e.TextContent.Trim().TrimStart('●', '○').Trim()).ToArray();

    /// <summary>Die Beschriftungen der sichtbaren Gruppenköpfe.</summary>
    private static string[] Gruppenkoepfe(IRenderedComponent<EnergietraegerDialog> cut)
        => cut.FindAll(".epos-traeger-gruppenkopf")
              .Select(e => e.TextContent.Trim().TrimStart('▪').Trim()).ToArray();

    [Fact]
    public void Ueber_der_Liste_steht_ein_Filterfeld()
    {
        var cut = Zeige(p => p.Add(x => x.SucheText, "Filter:"));

        Assert.Contains("Filter:", cut.Find(".epos-traeger-liste").TextContent);
        Assert.Single(cut.FindAll(".epos-traeger-liste input[type=text]"));
    }

    [Fact]
    public void Die_Suche_filtert_die_Traegerliste_und_blendet_leere_Gruppen_aus()
    {
        var cut = Zeige();

        Suchen(cut, "erd");

        // Nur „Erdgas H" trifft — die Gruppe „Strom" hat keinen Treffer und
        // faellt samt ihrem Kopf weg, „Gas" bleibt ueber ihrem einen Treffer.
        Assert.Equal(new[] { "Erdgas H" }, Eintraege(cut));
        Assert.Equal(new[] { "Gas" }, Gruppenkoepfe(cut));
    }

    [Fact]
    public void Die_Suche_trifft_auch_ueber_den_Gruppennamen()
    {
        var cut = Zeige();

        // „Gas" steht als Gruppe ueber beiden Traegern — wie im Importdialog
        // Bezeichner UND Firma geprueft werden.
        Suchen(cut, "Gas");

        Assert.Equal(new[] { "Erdgas H", "Flüssiggas" }, Eintraege(cut));
        Assert.Equal(new[] { "Gas" }, Gruppenkoepfe(cut));
    }

    [Fact]
    public void Eine_leere_Suche_zeigt_alle()
    {
        var cut = Zeige();

        Suchen(cut, "erd");
        Assert.Single(Eintraege(cut));

        Suchen(cut, "");
        Assert.Equal(3, Eintraege(cut).Length);
        Assert.Equal(2, Gruppenkoepfe(cut).Length);
    }

    [Fact]
    public void Gross_und_Kleinschreibung_ist_egal()
    {
        var cut = Zeige();

        Suchen(cut, "ERDGAS");
        Assert.Equal(new[] { "Erdgas H" }, Eintraege(cut));

        Suchen(cut, "erdgas");
        Assert.Equal(new[] { "Erdgas H" }, Eintraege(cut));
    }

    [Fact]
    public void Der_gewaehlte_Traeger_bleibt_ueber_einen_Filterwechsel_gewaehlt()
    {
        var cut = Zeige(p => p.Add(x => x.TraegerVorwahl, 21));
        Assert.Equal(21, cut.Instance.Traeger);

        // Der Filter blendet den Stromtraeger aus — gewaehlt bleibt er trotzdem,
        // die Karte rechts gehoert weiter ihm.
        Suchen(cut, "erd");
        Assert.Equal(21, cut.Instance.Traeger);
        Assert.DoesNotContain("Elektrische Energie", Eintraege(cut));

        // Und beim Leeren steht er wieder da — markiert.
        Suchen(cut, "");
        Assert.Equal(21, cut.Instance.Traeger);
        Assert.Equal("true", cut.FindAll(".epos-traeger-eintrag")[2].GetAttribute("aria-pressed"));
    }

    [Fact]
    public void Ohne_Treffer_sagt_die_Liste_es_statt_leer_zu_bleiben()
    {
        var cut = Zeige(p => p.Add(x => x.SucheLeerText, "Kein Treffer."));

        Suchen(cut, "Holzhackschnitzel");

        Assert.Empty(Eintraege(cut));
        Assert.Equal("Kein Treffer.", cut.Find(".epos-traeger-leer").TextContent.Trim());
    }

    [Fact]
    public void Die_Pfeiltasten_wandern_ueber_die_Treffer()
    {
        var cut = Zeige();
        Assert.Equal(11, cut.Instance.Traeger);

        // Ohne Filter laeuft ↓ ueber alle drei Traeger und ueberspringt die
        // Gruppenkoepfe — wie die ListBox des Vorlaeufers.
        cut.Find(".epos-traeger-eintraege").KeyDown(key: "ArrowDown");
        Assert.Equal(12, cut.Instance.Traeger);

        cut.Find(".epos-traeger-eintraege").KeyDown(key: "ArrowDown");
        Assert.Equal(21, cut.Instance.Traeger);
        Assert.Equal(21, _geladen);

        cut.Find(".epos-traeger-eintraege").KeyDown(key: "ArrowUp");
        Assert.Equal(12, cut.Instance.Traeger);

        cut.Find(".epos-traeger-eintraege").KeyDown(key: "End");
        Assert.Equal(21, cut.Instance.Traeger);

        cut.Find(".epos-traeger-eintraege").KeyDown(key: "Home");
        Assert.Equal(11, cut.Instance.Traeger);
    }

    [Fact]
    public void Unter_dem_Filter_wandert_die_Tastatur_nur_ueber_die_Treffer()
    {
        var cut = Zeige();

        Suchen(cut, "gas");     // Erdgas H, Flüssiggas — der Stromtraeger faellt weg
        cut.Find(".epos-traeger-eintraege").KeyDown(key: "End");

        Assert.Equal(12, cut.Instance.Traeger);
    }

    [Fact]
    public void Der_Katalogkontext_zeigt_drei_Knoepfe_der_Projektkontext_zwei()
    {
        var katalog = Zeige();
        var projekt = Zeige(katalog: false);

        Assert.Equal(3, katalog.Find(".epos-traeger-liste .epos-leiste")
                              .QuerySelectorAll("button").Length);
        Assert.Equal(2, projekt.Find(".epos-traeger-liste .epos-leiste")
                              .QuerySelectorAll("button").Length);
    }

    [Fact]
    public void Der_Stammkopf_steht_nur_im_Katalogkontext()
    {
        var katalog = Zeige();
        var projekt = Zeige(katalog: false);

        Assert.Contains("Bezeichnung:", katalog.Markup);
        Assert.DoesNotContain("Bezeichnung:", projekt.Markup);
    }

    [Fact]
    public void Die_beiden_Stromkarten_erscheinen_nur_beim_Stromtraeger()
    {
        var ohne = Zeige();
        var mit = Zeige(ansicht: new EnergietraegerAnsicht
        {
            Stand = Stand(strom: true), MitStromkarten = true, MitKostenprofil = true
        });

        Assert.Empty(ohne.FindAll(".epos-kachel"));
        Assert.Equal(2, mit.FindAll(".epos-kachel").Count);
    }

    [Fact]
    public void Ohne_Kostenprofil_bleibt_nur_die_Spotkarte()
    {
        var cut = Zeige(ansicht: new EnergietraegerAnsicht
        {
            Stand = Stand(strom: true), MitStromkarten = true, MitKostenprofil = false
        });

        Assert.Single(cut.FindAll(".epos-kachel"));
    }

    // =====================================================================
    // Trägerkarte
    // =====================================================================

    [Fact]
    public void Die_Traegerkarte_zeigt_ihre_zwei_Reiter_und_die_Historie()
    {
        var cut = Zeige();

        // iU9-W5.0 (Nachzug A-2): zwei Reiter wie im Vorlaeufer,
        // „Preise & Umrechnung" und „Emissionen"; die Historie samt
        // Speichern-Knopf steht UNTER der Leiste und gilt fuer beide.
        var reiter = cut.FindAll(".epos-traegerkarte .epos-reiter-knopf");
        Assert.Equal(2, reiter.Count);
        Assert.Equal("Preise & Umrechnung", reiter[0].TextContent.Trim());
        Assert.Equal("Emissionen", reiter[1].TextContent.Trim());

        // Der aktive Reiter zeigt Preise und Umrechnung, darunter die Historie.
        Assert.Equal(2, cut.FindAll(".epos-reiter-blatt > .epos-gruppenkopf").Count);
        Assert.Single(cut.FindAll(".epos-traegerkarte > .epos-gruppenkopf"));

        Assert.Equal("Erdgas H  (VDI 3805 3)", cut.Find(".epos-traeger-name").TextContent);
        Assert.Equal("Gruppe: Gas", cut.Find(".epos-traeger-gruppe").TextContent);
    }

    /// <summary>Stellt die Trägerkarte auf den Reiter „Emissionen".</summary>
    private static void ZeigeEmissionen(IRenderedComponent<EnergietraegerDialog> cut)
        => cut.FindAll(".epos-traegerkarte .epos-reiter-knopf")[1].Click();

    [Fact]
    public void Ohne_Heizwert_fehlen_Heizwertfeld_und_Formel()
    {
        EnergietraegerStand ohne = Stand();
        ohne.MitHeizwert = false;
        ohne.MitBrennwert = false;
        ohne.MitFormel = false;
        var cut = Zeige(ansicht: new EnergietraegerAnsicht { Stand = ohne });

        // Die Historie führt weiter eine Spalte „Heizwert"; das EINGABEfeld
        // dazu (Einheit kWh/Nm³) fehlt.
        Assert.DoesNotContain("kWh/Nm³", cut.Markup);
        Assert.DoesNotContain("Formel:", cut.Markup);
    }

    [Fact]
    public void Ohne_Leistungspreis_fehlt_die_ganze_Zeile_samt_Saisonknopf()
    {
        EnergietraegerStand ohne = Stand();
        ohne.MitLeistungspreis = false;
        var cut = Zeige(ansicht: new EnergietraegerAnsicht { Stand = ohne });

        Assert.DoesNotContain("Saisonale Sätze", cut.Markup);
    }

    [Fact]
    public void Der_Verstosshinweis_erscheint_als_Warnbanner()
    {
        EnergietraegerStand stand = Stand();
        stand.VerstossText = "Der Träger erreicht kWh nicht.";
        var cut = Zeige(ansicht: new EnergietraegerAnsicht { Stand = stand });

        Assert.Contains("Der Träger erreicht kWh nicht.", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Ohne_Artenkatalog_bleiben_die_drei_Bestandsfelder()
    {
        EnergietraegerStand stand = Stand();
        stand.EmissionenVerfuegbar = false;
        stand.AltCO2 = 201;
        var cut = Zeige(ansicht: new EnergietraegerAnsicht { Stand = stand });
        ZeigeEmissionen(cut);

        Assert.Contains("CO2  [g/kWh]", cut.Markup);
        Assert.Contains("SO2  [g/kWh]", cut.Markup);
        Assert.Contains("NOx  [g/kWh]", cut.Markup);
    }

    [Fact]
    public void Eine_nur_lesende_Emissionszeile_sperrt_Feld_und_Katalogknopf()
    {
        var cut = Zeige();
        ZeigeEmissionen(cut);

        // Im Reiter „Emissionen": Raster 0 = Emissionen, 1 = Preishistorie
        // (die Umrechnungsregeln stehen im anderen Reiter).
        var zeilen = cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr");
        Assert.Equal(2, zeilen.Length);
        Assert.False(zeilen[0].QuerySelector("input[type=text]")!.HasAttribute("disabled"));
        Assert.True(zeilen[1].QuerySelector("input[type=text]")!.HasAttribute("disabled"));
        Assert.True(zeilen[1].QuerySelector("button")!.HasAttribute("disabled"));
    }

    // =====================================================================
    // Ablauf
    // =====================================================================

    [Fact]
    public void Ein_Traegerwechsel_laedt_die_Karte_neu()
    {
        var cut = Zeige();

        cut.FindAll(".epos-traeger-eintrag")[2].Click();

        Assert.Equal(21, _geladen);
        Assert.Equal(21, cut.Instance.Traeger);
    }

    [Fact]
    public void Der_Stammkopf_meldet_eine_leere_Bezeichnung()
    {
        var cut = Zeige(p => p
            .Add(x => x.StammSchreiben, (string n, int? g) => false)
            .Add(x => x.MeldungStammLeer, "Bezeichnung darf nicht leer sein."));

        // Der Uebernahmeknopf steht seit iU8-E-2 / W14a-E-7 (Paket P2) in einer
        // eigenen Leiste unter dem Formularraster; das handgebaute Feldpaar
        // des Stammkopfs ist damit entfallen.
        cut.Find(".epos-traeger-inhalt .epos-leiste button").Click();

        Assert.Contains("nicht leer", cut.Instance.Meldung);
    }

    [Fact]
    public void Loeschen_fragt_erst_nach_und_nennt_einen_Grund()
    {
        int geloescht = 0;
        var cut = Zeige(p => p
            .Add(x => x.TraegerLoeschen, () => { geloescht++; return (false, "wird verwendet"); })
            .Add(x => x.VorlageLoeschen, "Energieträger „{0}\" löschen?")
            .Add(x => x.VorlageGesperrt, "Der Träger wird verwendet und bleibt erhalten: {0}"));

        cut.Find(".epos-traeger-liste .epos-leiste").QuerySelectorAll("button")[2].Click();
        Assert.Contains("Energieträger „Erdgas H\" löschen?", cut.Markup);
        Assert.Equal(0, geloescht);

        cut.FindAll(".epos-rueckfrage .epos-knopf")[0].Click();
        Assert.Equal(1, geloescht);
        Assert.Contains("wird verwendet", cut.Instance.Meldung);
    }

    [Fact]
    public void Die_Kataloguebernahme_zeigt_die_freien_Traeger_zur_Mehrfachwahl()
    {
        var cut = Zeige(katalog: false, mehr: p => p
            .Add(x => x.Freie, new[] { (31, "Fernwärme"), (32, "Pellets") }));

        cut.Find(".epos-traeger-liste .epos-leiste").QuerySelectorAll("button")[0].Click();

        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
        Assert.Equal(2, cut.FindAll(".epos-mehrfachauswahl-liste input[type=checkbox]").Count);
    }

    [Fact]
    public void Ohne_freie_Traeger_meldet_die_Uebernahme_statt_zu_oeffnen()
    {
        var cut = Zeige(katalog: false, mehr: p => p
            .Add(x => x.UebernahmeLeer, "Alle Katalogträger sind dem Projekt bereits zugeordnet."));

        cut.Find(".epos-traeger-liste .epos-leiste").QuerySelectorAll("button")[0].Click();

        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
        Assert.Contains("bereits zugeordnet", cut.Instance.Meldung);
    }

    [Fact]
    public void Die_Uebernahme_reicht_die_Wahl_weiter()
    {
        IReadOnlyList<int>? gewaehlt = null;
        var cut = Zeige(katalog: false, mehr: p => p
            .Add(x => x.Freie, new[] { (31, "Fernwärme"), (32, "Pellets") })
            .Add(x => x.InsProjekt, (IReadOnlyList<int> ids) => { gewaehlt = ids; return 32; }));

        cut.Find(".epos-traeger-liste .epos-leiste").QuerySelectorAll("button")[0].Click();
        cut.FindAll(".epos-mehrfachauswahl-liste input[type=checkbox]")[1].Change(true);
        // Die erste Leiste der Überlagerung ist „Alle"/„Keine" der
        // Mehrfachauswahl; die zweite trägt Abbrechen und Übernehmen.
        var leisten = cut.FindAll(".epos-ueberlagerung .epos-leiste");
        leisten[^1].QuerySelectorAll("button")[1].Click();

        Assert.Equal(new[] { 32 }, gewaehlt);
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
    }

    [Fact]
    public void Die_Karte_Kostenprofil_oeffnet_den_Dialog_in_der_Ueberlagerung()
    {
        var cut = Zeige(katalog: false,
            ansicht: new EnergietraegerAnsicht
            {
                Stand = Stand(strom: true), MitStromkarten = true, MitKostenprofil = true
            },
            mehr: p => p.Add(x => x.KostenprofilGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Bezeichner"] = "Standard",
                    ["Monatswerte"] = (IReadOnlyList<double>)new double[12],
                    ["Wochenwerte"] = (IReadOnlyList<double>)new double[168]
                }));

        cut.FindAll(".epos-kachel")[0].Click();

        Assert.Equal(EnergietraegerDialog.Unterdialog.Kostenprofil, cut.Instance.OffenerUnterdialog);
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
    }

    [Fact]
    public void Der_Emissionskatalog_einer_Zeile_oeffnet_mit_ihrem_Kuerzel()
    {
        string? kuerzel = null;
        var cut = Zeige(p => p.Add(x => x.EmissionskatalogGaben, (string k) =>
        {
            kuerzel = k;
            return (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
            {
                ["Arten"] = (IReadOnlyList<EmissionsartZeile>)Array.Empty<EmissionsartZeile>()
            };
        }));

        ZeigeEmissionen(cut);
        var zeilen = cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr");
        zeilen[0].QuerySelector("button")!.Click();

        Assert.Equal("CO2", kuerzel);
        Assert.Equal(EnergietraegerDialog.Unterdialog.Emissionskatalog,
                     cut.Instance.OffenerUnterdialog);
    }

    // =====================================================================
    // Schlussleiste (Ä14)
    // =====================================================================

    [Fact]
    public void Speichern_bestaetigt_in_der_Kontextzeile()
    {
        int gespeichert = 0;
        var cut = Zeige(p => p
            .Add(x => x.KontextText, "Kontext: Katalog (Stammdaten)")
            .Add(x => x.Speichern, () => { gespeichert++; return true; })
            .Add(x => x.VorlageGespeichert, " — gespeichert {0} Uhr"));

        cut.FindAll(".epos-dialog > .epos-leiste")[^1].QuerySelectorAll("button")[1].Click();

        Assert.Equal(1, gespeichert);
        Assert.Contains("gespeichert", cut.Find(".epos-kontextzeile").TextContent);
    }

    [Fact]
    public void Ein_abgelehntes_Speichern_nennt_den_Grund_und_schliesst_nicht()
    {
        bool? ergebnis = null;
        var cut = Zeige(p => p
            .Add(x => x.Speichern, () => false)
            .Add(x => x.SpeichernGrund, () => "Der Träger erreicht kWh nicht.")
            .Add(x => x.Geschlossen, (bool ok) => ergebnis = ok));

        cut.FindAll(".epos-dialog > .epos-leiste")[^1].QuerySelectorAll("button")[2].Click();

        Assert.Null(ergebnis);
        Assert.Contains("erreicht kWh nicht", cut.Instance.Meldung);
    }

    [Fact]
    public void Abbrechen_schliesst_ohne_zu_speichern()
    {
        bool? ergebnis = null;
        int gespeichert = 0;
        var cut = Zeige(p => p
            .Add(x => x.Speichern, () => { gespeichert++; return true; })
            .Add(x => x.Geschlossen, (bool ok) => ergebnis = ok));

        cut.FindAll(".epos-dialog > .epos-leiste")[^1].QuerySelectorAll("button")[0].Click();

        Assert.Equal(0, gespeichert);
        Assert.False(ergebnis);
    }

    [Fact]
    public void Esc_schliesst_den_Dialog_Enter_nicht()
    {
        bool? ergebnis = null;
        var cut = Zeige(p => p.Add(x => x.Geschlossen, (bool ok) => ergebnis = ok));

        cut.Find(".epos-dialog").KeyDown(key: "Enter");
        Assert.Null(ergebnis);

        cut.Find(".epos-dialog").KeyDown(key: "Escape");
        Assert.False(ergebnis);
    }

    /// <summary>Anwenderentscheid 15.09.2026: Das Kreuz im Kopf wirkt wie Esc/Abbrechen.</summary>
    [Fact]
    public void Das_Kreuz_im_Kopf_schliesst_wie_Abbrechen()
    {
        bool? ergebnis = null;
        var cut = Zeige(p => p.Add(x => x.Geschlossen, (bool ok) => ergebnis = ok));

        cut.Find(".epos-dialog-zu").Click();

        Assert.False(ergebnis);
    }

    /// <summary>
    /// Die Neu-Überlagerung war <c>Schliessbar="false"</c> — seit dem
    /// Anwenderentscheid 15.09.2026 trägt sie ihr eigenes Kreuz, das denselben
    /// Weg wie Abbrechen der Namensabfrage geht.
    /// </summary>
    [Fact]
    public void Das_Kreuz_der_Neu_Ueberlagerung_schliesst_sie_wieder()
    {
        var cut = Zeige(p => p.Add(x => x.NamensGaben, () => new Dictionary<string, object>()));

        cut.Find(".epos-traeger-liste .epos-leiste").QuerySelectorAll("button")[0].Click();
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));

        cut.Find(".epos-ueberlagerung-zu").Click();

        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
    }

    /// <summary>
    /// Befund W4‑B‑1 (Windows-Abnahme 04.09.2026): „Die Preisbasis wird
    /// teilweise nicht angezeigt oder doppelt."
    ///
    /// <para>Die Ursache lag in der Hülle, die die Liste aus der
    /// <c>to_unit</c> JEDER Umrechnungsregel baute — geprüft wird das im Kern
    /// (<c>EPOS.Kern.Tests/PreisbasenTests</c>). Der Dialog hat seinen eigenen
    /// Anteil daran: Er muss die Einträge <b>eins zu eins</b> zeigen und die
    /// <b>Id</b> zurückmelden, nicht die Position — sonst verschöbe schon eine
    /// bereinigte Liste die gewählte Zeile.</para>
    /// </summary>
    [Fact]
    public void Die_Preisbasis_zeigt_jeden_Eintrag_einmal_und_meldet_seine_Id()
    {
        int? gemeldet = null;
        var cut = Zeige(p => p.Add(x => x.PreisbasisGewechselt, (int id) => gemeldet = id));

        var feld = cut.FindAll("select")
                      .First(s => s.QuerySelectorAll("option")
                                   .Any(o => o.TextContent.Trim() == "Nm³"));

        var texte = feld.QuerySelectorAll("option").Select(o => o.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "Nm³", "kWh" }, texte);
        Assert.Equal(texte.Length, texte.Distinct().Count());

        // Der Wert der Option ist die Id des Standes, nicht ihr Listenplatz.
        Assert.Equal(new[] { "0", "1" },
                     feld.QuerySelectorAll("option").Select(o => o.GetAttribute("value")).ToArray());

        feld.Change("1");
        Assert.Equal(1, gemeldet);
    }

    [Fact]
    public void Der_Hilfeschluessel_bleibt_der_der_Maske()
    {
        var hilfe = new TestHilfe();
        Services.AddSingleton<IHilfeDienst>(hilfe);

        var cut = Zeige();
        cut.Find(".epos-infoknopf").Click();

        Assert.Equal(new[] { "Form_Energietraeger.btn_Help" }, hilfe.Geoeffnet);
    }

    // =====================================================================
    //  Das Formularraster — Anwenderwunsch iU8-E-2 / W14a-E-7, Paket P2
    //  (Windows-Abnahme 05.09.2026)
    // =====================================================================


    /// <summary>
    /// <b>iU8-E-2 / W14a-E-7 (Paket P2):</b> Der Stammkopf im Katalogkontext und
    /// der Preisblock von <c>EnergietraegerEinstellungen</c> stehen im
    /// <c>Formularraster</c> — die handgebauten <c>epos-feldpaar</c> entfallen,
    /// der Raster stellt zwei Feldpaare je Zeile, sobald die Spalte breit genug
    /// ist. Das Suchfeld über der Liste bleibt, wo es ist: Es gehört zur Liste,
    /// nicht zu einem Formularblock.
    /// </summary>
    [Fact]
    public void Stammkopf_und_Preisblock_stehen_im_Formularraster()
    {
        var cut = Zeige();

        Assert.True(cut.FindAll(".epos-formularraster").Count >= 2);
        Assert.True(cut.FindAll(".epos-formularraster .epos-feld").Count >= 4);
        Assert.True(cut.FindAll(".epos-formularraster .epos-feld--kurz").Count > 0);

        // Das Suchfeld der Liste steht NICHT im Raster.
        Assert.Empty(cut.FindAll(".epos-traeger-liste .epos-formularraster"));
    }

    // =====================================================================
    // Energietraegerverwaltung im Projektkontext (Anwenderbefund 08.09.2026, ET-1/ET-3)
    // =====================================================================

    /// <summary>ET‑1: Die freien Träger kommen frisch über <c>FreieLaden</c>, auch wenn <c>Freie</c> leer ist.</summary>
    [Fact]
    public void Die_Kataloguebernahme_holt_die_freien_Traeger_frisch()
    {
        int gefragt = 0;
        var cut = Zeige(katalog: false, mehr: p => p
            .Add(x => x.FreieLaden, () => { gefragt++; return new[] { (31, "Fernwärme › Fernwärme"), (32, "Holz › Pellets") }; }));

        cut.Find(".epos-traeger-liste .epos-leiste").QuerySelectorAll("button")[0].Click();

        Assert.Equal(1, gefragt);
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
        Assert.Equal(2, cut.FindAll(".epos-mehrfachauswahl-liste input[type=checkbox]").Count);
        Assert.Contains("Fernwärme › Fernwärme", cut.Markup);
    }

    /// <summary>ET‑3: Ein verwendeter, nicht zugeordneter Träger steht markiert in der Liste, der Kurztext nennt die Verwender.</summary>
    [Fact]
    public void Ein_verwendeter_nicht_zugeordneter_Traeger_steht_markiert_in_der_Liste()
    {
        var liste = new EnergietraegerDialog.EnergietraegerListe[]
        {
            new(null, "Gas"),
            new(11, "Erdgas E", "verwendet von: Heizkessel „Vitocrossal“"),
            new(null, "Strom"),
            new(60, "Elektrische Energie", "verwendet von: Wärmepumpe „CS6800iAW“, Photovoltaik „Jinkosolar“ — nicht zugeordnet", false)
        };
        _ansicht = new EnergietraegerAnsicht { Stand = Stand() };
        var cut = Render<EnergietraegerDialog>(p =>
        {
            p.Add(x => x.Liste, liste);
            p.Add(x => x.Katalogkontext, false);
            p.Add(x => x.NichtZugeordnetText, "nicht zugeordnet");
            p.Add(x => x.TraegerLaden, id => { _geladen = id; return _ansicht; });
            p.Add(x => x.Nachrechnen, () => _ansicht);
        });

        var eintraege = cut.FindAll(".epos-traeger-eintrag");
        Assert.Equal(2, eintraege.Count);
        Assert.DoesNotContain("nicht zugeordnet", eintraege[0].TextContent);
        Assert.Equal("verwendet von: Heizkessel „Vitocrossal“", eintraege[0].GetAttribute("title"));
        Assert.Contains("Elektrische Energie ⚠ nicht zugeordnet", eintraege[1].TextContent);
        Assert.Contains("epos-traeger-eintrag--offen", eintraege[1].ClassName);
        Assert.Contains("Wärmepumpe", eintraege[1].GetAttribute("title"));

        // Auch der unzugeordnete Traeger laesst sich waehlen - Speichern ordnet ihn zu.
        eintraege[1].Click();
        Assert.Equal(60, _geladen);
    }

    // =====================================================================
    // Preishistorie und Katalogübernahme (Anwenderbefund 14.09.2026, B2/B3)
    // =====================================================================

    /// <summary>Der Knopf der Gruppe „Preise" — er steht nur im Projektkontext.</summary>
    private static IElement? Uebernahmeknopf(IRenderedComponent<EnergietraegerDialog> cut)
    {
        foreach (IElement k in cut.FindAll(".epos-traegerkarte button"))
        {
            if (k.TextContent.Trim() == "Katalogwerte übernehmen") return k;
        }
        return null;
    }

    /// <summary>
    /// B3: „Eine Übernahme der Stammdaten-Kosten (aus der Administration) soll
    /// möglich sein." Der Knopf steht in der Preisgruppe — aber nur, wo es einen
    /// Katalog GEGENÜBER gibt, also im Projektkontext.
    /// </summary>
    [Fact]
    public void Der_Uebernahmeknopf_steht_nur_im_Projektkontext()
    {
        EnergietraegerStand mit = Stand();
        mit.MitKatalogUebernahme = true;
        Assert.NotNull(Uebernahmeknopf(Zeige(katalog: false,
            ansicht: new EnergietraegerAnsicht { Stand = mit })));

        EnergietraegerStand ohne = Stand();
        ohne.MitKatalogUebernahme = false;
        Assert.Null(Uebernahmeknopf(Zeige(ansicht: new EnergietraegerAnsicht { Stand = ohne })));
    }

    /// <summary>Der Klick ruft den Rückruf der Hülle — geschrieben wird erst mit „Speichern".</summary>
    [Fact]
    public void Ein_Klick_auf_Katalogwerte_uebernehmen_ruft_den_Rueckruf()
    {
        int gerufen = 0;
        EnergietraegerStand stand = Stand();
        stand.MitKatalogUebernahme = true;

        var cut = Zeige(katalog: false, ansicht: new EnergietraegerAnsicht { Stand = stand },
                        mehr: p => p.Add(x => x.KatalogUebernehmen, () => gerufen++));

        Uebernahmeknopf(cut)!.Click();

        Assert.Equal(1, gerufen);
    }

    /// <summary>Die Hinweiszeile sagt, dass die Werte noch nicht geschrieben sind.</summary>
    [Fact]
    public void Nach_der_Uebernahme_steht_die_Hinweiszeile_da()
    {
        EnergietraegerStand stand = Stand();
        stand.MitKatalogUebernahme = true;
        stand.UebernahmeHinweis = "Katalogwerte übernommen — noch nicht gespeichert.";

        var cut = Zeige(katalog: false, ansicht: new EnergietraegerAnsicht { Stand = stand });

        Assert.Contains("noch nicht gespeichert", cut.Markup);
    }

    /// <summary>
    /// B2: Der Spaltenkopf der Historie nennt die Einheit des Heizwerts —
    /// kWh/&lt;Abrechnungseinheit&gt;, nicht „kWh/kWh".
    /// </summary>
    [Fact]
    public void Der_Historien_Spaltenkopf_nennt_die_Einheit()
    {
        var cut = Zeige();

        Assert.Contains("Heizwert [kWh/Nm³]", cut.Markup);
    }

    /// <summary>Ohne Heizwert bleibt es beim nackten Wort — der Träger führt keinen.</summary>
    [Fact]
    public void Ohne_Heizwert_bleibt_der_Spaltenkopf_ohne_Einheit()
    {
        EnergietraegerStand ohne = Stand();
        ohne.MitHeizwert = false;
        ohne.MitBrennwert = false;
        ohne.MitFormel = false;

        var cut = Zeige(ansicht: new EnergietraegerAnsicht { Stand = ohne });

        Assert.DoesNotContain("kWh/Nm³", cut.Markup);
        Assert.Contains("Heizwert", cut.Markup);
    }

    /// <summary>
    /// Im Katalogkontext sagt die Karte, warum dort keine Historienzeile entsteht
    /// — benannt abgelehnt statt still übergangen.
    /// </summary>
    [Fact]
    public void Der_Katalogkontext_nennt_den_Grund_unter_der_Tabelle()
    {
        EnergietraegerStand stand = Stand();
        stand.HistorieHinweis = "Die Preishistorie wird je Projekt geführt.";

        var cut = Zeige(ansicht: new EnergietraegerAnsicht { Stand = stand });

        Assert.Contains("je Projekt geführt", cut.Markup);
    }

    // =====================================================================
    // Preisstände löschen (Anwenderwunsch 14.09.2026)
    // =====================================================================

    /// <summary>Die Löschknöpfe der Historientabelle — einer je Zeile.</summary>
    private static IReadOnlyList<IElement> Loeschknoepfe(
        IRenderedComponent<EnergietraegerDialog> cut)
        => cut.FindAll(".epos-traegerkarte > .epos-gruppenkopf button[title]");

    /// <summary>Zwei Stände — der zweite trägt einen anderen Schlüssel.</summary>
    private static EnergietraegerStand ZweiStaende()
    {
        EnergietraegerStand stand = Stand();
        stand.Historie = new[]
        {
            new PreishistorieZeile("14.09.2026", "10,50", "Nm³", "0,9100", "0,00", "0,00", 12),
            new PreishistorieZeile("01.01.2026", "10,10", "Nm³", "0,6200", "120,00", "12,00", 7)
        };
        return stand;
    }

    [Fact]
    public void Die_Historientabelle_hat_eine_Aktionsspalte_mit_Loeschknopf()
    {
        var cut = Zeige(p => p.Add(x => x.HistorieLoeschen, _ => true),
                        ansicht: new EnergietraegerAnsicht { Stand = ZweiStaende() });

        // Die Spalte trägt einen Kopf mit Beschriftung (Hausregel), der Knopf
        // steht je Zeile und ist immer sichtbar.
        var koepfe = cut.FindAll(".epos-traegerkarte > .epos-gruppenkopf th");
        Assert.Equal("Löschen", koepfe[^1].TextContent.Trim());
        Assert.Equal(2, Loeschknoepfe(cut).Count);
        Assert.Equal("Preisstand löschen", Loeschknoepfe(cut)[0].GetAttribute("title"));
    }

    /// <summary>Ohne Rückruf bleibt der Knopf gesperrt — kein Delegat, kein Knopf.</summary>
    [Fact]
    public void Ohne_Rueckruf_ist_der_Loeschknopf_gesperrt()
    {
        var cut = Zeige(ansicht: new EnergietraegerAnsicht { Stand = ZweiStaende() });

        Assert.All(Loeschknoepfe(cut), k => Assert.True(k.HasAttribute("disabled")));
    }

    [Fact]
    public void Der_Loeschknopf_fragt_erst_nach()
    {
        int geloescht = 0;
        var cut = Zeige(p => p.Add(x => x.HistorieLoeschen, _ => { geloescht++; return true; }),
                        ansicht: new EnergietraegerAnsicht { Stand = ZweiStaende() });

        Loeschknoepfe(cut)[0].Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Preisstand vom 14.09.2026 löschen?", cut.Markup));
        Assert.Equal(0, geloescht);
    }

    [Fact]
    public void Ja_loescht_die_gewaehlte_Zeile()
    {
        PreishistorieZeile? gemeldet = null;
        var cut = Zeige(p => p.Add(x => x.HistorieLoeschen, z => { gemeldet = z; return true; }),
                        ansicht: new EnergietraegerAnsicht { Stand = ZweiStaende() });

        // Die ZWEITE Zeile — gemeldet werden muss ihr Schlüssel, nicht der der ersten.
        Loeschknoepfe(cut)[1].Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-rueckfrage")));

        cut.FindAll(".epos-rueckfrage .epos-knopf")[0].Click();

        cut.WaitForAssertion(() => Assert.NotNull(gemeldet));
        Assert.Equal(7, gemeldet!.Id);
        Assert.Equal("01.01.2026", gemeldet.GueltigAb);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".epos-rueckfrage")));
    }

    [Fact]
    public void Nein_laesst_den_Preisstand_stehen()
    {
        int geloescht = 0;
        var cut = Zeige(p => p.Add(x => x.HistorieLoeschen, _ => { geloescht++; return true; }),
                        ansicht: new EnergietraegerAnsicht { Stand = ZweiStaende() });

        Loeschknoepfe(cut)[0].Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-rueckfrage")));

        cut.FindAll(".epos-rueckfrage .epos-knopf")[1].Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".epos-rueckfrage")));
        Assert.Equal(0, geloescht);
    }

    /// <summary>Ein abgelehntes Löschen nennt den Grund — wie beim Speichern.</summary>
    [Fact]
    public void Ein_abgelehntes_Loeschen_nennt_den_Grund()
    {
        var cut = Zeige(p => p
            .Add(x => x.HistorieLoeschen, _ => false)
            .Add(x => x.HistorieLoeschenGrund, () => "Dieser Preisstand ist nicht mehr vorhanden."),
            ansicht: new EnergietraegerAnsicht { Stand = ZweiStaende() });

        Loeschknoepfe(cut)[0].Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-rueckfrage")));

        cut.FindAll(".epos-rueckfrage .epos-knopf")[0].Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("nicht mehr vorhanden", cut.Instance.Meldung));
    }

    // =====================================================================
    // Komponentenkontext (Anwenderwunsch 14.09.2026, Auftrag 268)
    // =====================================================================

    /// <summary>
    /// Die Kopfzeile nennt, wofür die Liste eingeengt ist — die Hülle baut den Text,
    /// der Dialog zeigt ihn.
    /// </summary>
    [Fact]
    public void Die_Kopfzeile_nennt_die_Komponente_und_ihre_Gruppe()
    {
        var cut = Zeige(katalog: false, mehr: p => p
            .Add(x => x.KontextText, "Kontext: Projekt 1027 — für Wärmepumpe: nur Gruppe Strom"));

        Assert.Contains("für Wärmepumpe: nur Gruppe Strom",
                        cut.Find(".epos-kontextzeile").TextContent);
    }

    /// <summary>
    /// ET‑E‑3 (Anwenderentscheid 17.09.2026): Ohne gewählte Anlagenzeile engt das
    /// PROJEKT die Katalogübernahme ein — die Kopfzeile sagt worauf, in beiden Sprachen
    /// aus derselben Ressourcenzeile.
    /// </summary>
    [Theory]
    [InlineData("de-DE", "Übernahme eingeengt auf die Anlagen des Projekts")]
    [InlineData("en-US", "Catalogue transfer limited to the project's systems")]
    public void Die_Kopfzeile_nennt_die_Einengung_auf_die_Anlagen_des_Projekts(
        string kultur, string erwartet)
    {
        using var _ = new Kulturvorrichtung(kultur);

        string text = "Kontext: Projekt 1030 — " + string.Format(
            CultureInfo.CurrentCulture,
            WindowsFormsApplication1.MyResource.Resource.KDLG_ET_KONTEXT_PROJEKTANLAGEN,
            "Gas, Wasserstoff");

        var cut = Zeige(katalog: false, mehr: p => p.Add(x => x.KontextText, text));

        string kopfzeile = cut.Find(".epos-kontextzeile").TextContent;
        Assert.Contains(erwartet, kopfzeile);
        Assert.Contains("Gas, Wasserstoff", kopfzeile);
    }

    /// <summary>
    /// Mit Komponentenkontext kommt die Liste bereits eingeengt herein — der Dialog
    /// zeigt, was er bekommt, und die leere Gruppe fällt samt Kopf weg.
    /// </summary>
    [Fact]
    public void Mit_Komponentenkontext_stehen_nur_die_zulaessigen_Traeger_in_der_Liste()
    {
        var liste = new EnergietraegerDialog.EnergietraegerListe[]
        {
            new(null, "Strom"),
            new(60, "Elektrische Energie")
        };
        _ansicht = new EnergietraegerAnsicht { Stand = Stand() };
        var cut = Render<EnergietraegerDialog>(p =>
        {
            p.Add(x => x.Liste, liste);
            p.Add(x => x.Katalogkontext, false);
            p.Add(x => x.TraegerLaden, id => { _geladen = id; return _ansicht; });
            p.Add(x => x.Nachrechnen, () => _ansicht);
        });

        Assert.Equal(new[] { "Elektrische Energie" }, Eintraege(cut));
        Assert.Equal(new[] { "Strom" }, Gruppenkoepfe(cut));
    }

    /// <summary>
    /// Ein zugeordneter Träger, der nicht zur Komponente passt, VERSCHWINDET NICHT — er
    /// steht markiert da, samt Hinweis. Wegfiltern hieße, eine falsche Zuordnung zu
    /// verstecken.
    /// </summary>
    [Fact]
    public void Ein_unzulaessiger_zugeordneter_Traeger_steht_markiert_in_der_Liste()
    {
        var liste = new EnergietraegerDialog.EnergietraegerListe[]
        {
            new(null, "Gas"),
            new(63, "Erdgas E", "", true, false),
            new(null, "Strom"),
            new(60, "Elektrische Energie")
        };
        _ansicht = new EnergietraegerAnsicht { Stand = Stand() };
        var cut = Render<EnergietraegerDialog>(p =>
        {
            p.Add(x => x.Liste, liste);
            p.Add(x => x.Katalogkontext, false);
            p.Add(x => x.PasstNichtText, "passt nicht zur Komponente");
            p.Add(x => x.TraegerLaden, id => { _geladen = id; return _ansicht; });
            p.Add(x => x.Nachrechnen, () => _ansicht);
        });

        var eintraege = cut.FindAll(".epos-traeger-eintrag");
        Assert.Equal(2, eintraege.Count);
        Assert.Contains("Erdgas E ⚠ passt nicht zur Komponente", eintraege[0].TextContent);
        Assert.Contains("epos-traeger-eintrag--offen", eintraege[0].ClassName);
        Assert.DoesNotContain("passt nicht", eintraege[1].TextContent);

        // Wählbar bleibt er - sonst käme man an seine Preise nicht mehr heran.
        eintraege[0].Click();
        Assert.Equal(63, _geladen);
    }

    /// <summary>
    /// Beide Markierungen zugleich: nicht zugeordnet UND nicht passend.
    /// </summary>
    [Fact]
    public void Nicht_zugeordnet_und_nicht_passend_stehen_nebeneinander()
    {
        var liste = new EnergietraegerDialog.EnergietraegerListe[]
        {
            new(null, "Gas"),
            new(63, "Erdgas E", "verwendet von: Heizkessel „Vitocrossal“", false, false)
        };
        _ansicht = new EnergietraegerAnsicht { Stand = Stand() };
        var cut = Render<EnergietraegerDialog>(p =>
        {
            p.Add(x => x.Liste, liste);
            p.Add(x => x.Katalogkontext, false);
            p.Add(x => x.NichtZugeordnetText, "nicht zugeordnet");
            p.Add(x => x.PasstNichtText, "passt nicht zur Komponente");
            p.Add(x => x.TraegerLaden, id => { _geladen = id; return _ansicht; });
            p.Add(x => x.Nachrechnen, () => _ansicht);
        });

        string text = cut.FindAll(".epos-traeger-eintrag")[0].TextContent;
        Assert.Contains("nicht zugeordnet", text);
        Assert.Contains("passt nicht zur Komponente", text);
    }

    /// <summary>
    /// „Aus Katalog übernehmen…" bietet nur, was die Hülle hereingibt — mit
    /// Komponentenkontext also nur die zulässigen Katalogträger.
    /// </summary>
    [Fact]
    public void Die_Uebernahme_bietet_nur_die_hereingegebenen_Traeger()
    {
        var cut = Zeige(katalog: false, mehr: p => p
            .Add(x => x.FreieLaden, () => new[] { (58, "Strom › Elektrische Energie 2") }));

        cut.Find(".epos-traeger-liste .epos-leiste").QuerySelectorAll("button")[0].Click();

        Assert.Single(cut.FindAll(".epos-mehrfachauswahl-liste input[type=checkbox]"));
        Assert.Contains("Strom › Elektrische Energie 2", cut.Markup);
        Assert.DoesNotContain("Erdgas", cut.Find(".epos-mehrfachauswahl-liste").TextContent);
    }

    // =====================================================================
    //  „Das Kreuz steht beim Titel" (Anwenderentscheid 15.09.2026)
    // =====================================================================

    /// <summary>Titel-bedingter Kopf: ohne Titel zeigt der Kopf weder Titel noch Kreuz.</summary>
    [Fact]
    public void Ohne_Titel_zeigt_der_Kopf_weder_Titel_noch_Kreuz()
    {
        var cut = Zeige(p => p.Add(x => x.TitelAnzeigen, false));

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
        // Der Hilfeknopf bleibt - er haengt nicht am Titel.
        Assert.NotEmpty(cut.FindAll(".epos-dialog-kopf"));
    }

    /// <summary>Die Prüfung je Überlagerung: genau EIN ✕, kein zweiter Titel darunter.</summary>
    private static void NurEinKreuz(IRenderedComponent<EnergietraegerDialog> cut)
    {
        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
    }

    [Fact]
    public void Die_Ueberlagerung_Neuer_Traeger_zeigt_nur_ein_Kreuz()
    {
        var cut = Zeige(p => p.Add(x => x.NamensGaben, () => new Dictionary<string, object>()));

        cut.Find(".epos-traeger-liste .epos-leiste").QuerySelectorAll("button")[0].Click();

        NurEinKreuz(cut);
    }

    [Fact]
    public void Die_Ueberlagerung_Kostenprofil_zeigt_nur_ein_Kreuz()
    {
        var cut = Zeige(katalog: false,
            ansicht: new EnergietraegerAnsicht
            {
                Stand = Stand(strom: true), MitStromkarten = true, MitKostenprofil = true
            },
            mehr: p => p.Add(x => x.KostenprofilGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Bezeichner"] = "Standard",
                    ["Monatswerte"] = (IReadOnlyList<double>)new double[12],
                    ["Wochenwerte"] = (IReadOnlyList<double>)new double[168]
                }));

        cut.FindAll(".epos-kachel")[0].Click();

        NurEinKreuz(cut);
    }

    [Fact]
    public void Die_Ueberlagerung_Spotpreis_zeigt_nur_ein_Kreuz()
    {
        var cut = Zeige(katalog: false,
            ansicht: new EnergietraegerAnsicht
            {
                Stand = Stand(strom: true), MitStromkarten = true, MitKostenprofil = true
            },
            mehr: p => p.Add(x => x.SpotpreisGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>()));

        cut.FindAll(".epos-kachel")[1].Click();

        NurEinKreuz(cut);
    }

    [Fact]
    public void Die_Ueberlagerung_Saisonreihe_zeigt_nur_ein_Kreuz()
    {
        var cut = Zeige(p => p.Add(x => x.SaisonGaben, () =>
            (IReadOnlyDictionary<string, object>)new Dictionary<string, object>()));

        cut.FindAll("button").Single(x => x.TextContent.Contains("Saisonale Sätze")).Click();

        NurEinKreuz(cut);
    }

    [Fact]
    public void Die_Ueberlagerung_Emissionskatalog_zeigt_nur_ein_Kreuz()
    {
        var cut = Zeige(p => p.Add(x => x.EmissionskatalogGaben, (string k) =>
            (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
            {
                ["Arten"] = (IReadOnlyList<EmissionsartZeile>)Array.Empty<EmissionsartZeile>()
            }));

        ZeigeEmissionen(cut);
        cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr")[0]
           .QuerySelector("button")!.Click();

        NurEinKreuz(cut);
    }

    // =====================================================================
    // ET-5 / ET-6 (Anwenderbefund 16.09.2026): Die Liste folgt jedem
    // Schreibweg — und der stille Wiederzuordner wird benannt
    // =====================================================================

    /// <summary>Der veränderliche Listenstand, den <c>ListeNeuLaden</c> zurückgibt.</summary>
    private List<EnergietraegerDialog.EnergietraegerListe> _stand = new();

    /// <summary>
    /// Wie <see cref="Zeige"/>, nur mit angeschlossenem Nachlade-Weg: Die Hülle würde
    /// hier ihre Trägerliste frisch aus der Datenbank bauen; der Prüfstand nimmt eine
    /// Liste, die der Schreibweg des Falls verändert.
    /// </summary>
    private IRenderedComponent<EnergietraegerDialog> ZeigeMitNachladen(
        Action<Bunit.ComponentParameterCollectionBuilder<EnergietraegerDialog>> mehr,
        bool katalog = true)
    {
        _stand = new List<EnergietraegerDialog.EnergietraegerListe>(LISTE);
        return Zeige(p =>
        {
            p.Add(x => x.ListeNeuLaden,
                  () => (IReadOnlyList<EnergietraegerDialog.EnergietraegerListe>)_stand);
            mehr(p);
        }, katalog: katalog);
    }

    /// <summary>Das Markup der Trägerliste — ohne die Karte rechts.</summary>
    private static string Listenmarkup(IRenderedComponent<EnergietraegerDialog> cut)
        => cut.Find(".epos-traeger-eintraege").OuterHtml;

    [Fact]
    public void Nach_dem_Entfernen_verschwindet_der_Traeger_aus_der_Liste()
    {
        var cut = ZeigeMitNachladen(p => p
            .Add(x => x.AusProjekt, () =>
            {
                _stand.RemoveAll(e => e.Traeger == 11);
                return (true, "");
            })
            .Add(x => x.VorlageEntfernen, "Träger „{0}\" aus dem Projekt entfernen?"),
            katalog: false);

        Assert.Contains("Erdgas H", Listenmarkup(cut));
        Assert.Equal(11, cut.Instance.Traeger);

        // „Entfernen" ist der zweite Knopf der Listenleiste im Projektkontext.
        cut.Find(".epos-traeger-liste .epos-leiste").QuerySelectorAll("button")[1].Click();
        cut.FindAll(".epos-rueckfrage .epos-knopf")[0].Click();

        Assert.DoesNotContain("Erdgas H", Listenmarkup(cut));
        Assert.DoesNotContain(cut.Instance.Angezeigt, e => e.Traeger == 11);
        Assert.Null(cut.Instance.Traeger);
    }

    [Fact]
    public void Nach_dem_Loeschen_verschwindet_der_Traeger_aus_der_Liste()
    {
        var cut = ZeigeMitNachladen(p => p
            .Add(x => x.TraegerLoeschen, () =>
            {
                _stand.RemoveAll(e => e.Traeger == 11);
                return (true, "");
            })
            .Add(x => x.VorlageLoeschen, "Energieträger „{0}\" löschen?"));

        cut.Find(".epos-traeger-liste .epos-leiste").QuerySelectorAll("button")[2].Click();
        cut.FindAll(".epos-rueckfrage .epos-knopf")[0].Click();

        Assert.DoesNotContain("Erdgas H", Listenmarkup(cut));
        Assert.Null(cut.Instance.Traeger);
    }

    [Fact]
    public void Nach_Neu_steht_der_neue_Traeger_in_der_Liste_und_ist_markiert()
    {
        var cut = ZeigeMitNachladen(p => p
            .Add(x => x.NamensGaben, () => (IReadOnlyDictionary<string, object>)
                new Dictionary<string, object>())
            .Add(x => x.TraegerNeu, (string name) =>
            {
                _stand.Add(new EnergietraegerDialog.EnergietraegerListe(41, name));
                return 41;
            }));

        cut.Find(".epos-traeger-liste .epos-leiste").QuerySelectorAll("button")[0].Click();
        cut.Find(".epos-ueberlagerung .epos-eingabe").Input("Klärgas");
        cut.Find(".epos-ueberlagerung .epos-knopf--primaer").Click();

        Assert.Contains("Klärgas", Listenmarkup(cut));
        Assert.Equal(41, cut.Instance.Traeger);
        Assert.Equal(41, _geladen);
    }

    [Fact]
    public void Nach_Variante_steht_die_Variante_in_der_Liste_und_ist_markiert()
    {
        var cut = ZeigeMitNachladen(p => p
            .Add(x => x.TraegerVariante, () =>
            {
                _stand.Add(new EnergietraegerDialog.EnergietraegerListe(42, "Erdgas H Variante"));
                return 42;
            }));

        cut.Find(".epos-traeger-liste .epos-leiste").QuerySelectorAll("button")[1].Click();

        Assert.Contains("Erdgas H Variante", Listenmarkup(cut));
        Assert.Equal(42, cut.Instance.Traeger);
    }

    /// <summary>
    /// Der Weg des Anwenderbefunds: Bis ET-5 setzte die Übernahme <c>_traegerId</c> auf
    /// einen Träger, den die Liste GAR NICHT führte — Liste ohne Markierung, Karte mit
    /// neuem Träger.
    /// </summary>
    [Fact]
    public void Nach_der_Kataloguebernahme_steht_der_Traeger_in_der_Liste_und_ist_markiert()
    {
        var cut = ZeigeMitNachladen(p => p
            .Add(x => x.Freie, new[] { (31, "Fernwärme"), (32, "Pellets") })
            .Add(x => x.InsProjekt, (IReadOnlyList<int> ids) =>
            {
                _stand.Add(new EnergietraegerDialog.EnergietraegerListe(32, "Pellets"));
                return 32;
            }),
            katalog: false);

        cut.Find(".epos-traeger-liste .epos-leiste").QuerySelectorAll("button")[0].Click();
        cut.FindAll(".epos-mehrfachauswahl-liste input[type=checkbox]")[1].Change(true);
        var leisten = cut.FindAll(".epos-ueberlagerung .epos-leiste");
        leisten[^1].QuerySelectorAll("button")[1].Click();

        Assert.Contains("Pellets", Listenmarkup(cut));
        Assert.Equal(32, cut.Instance.Traeger);
    }

    [Fact]
    public void Nach_dem_Stamm_Speichern_traegt_die_Liste_den_neuen_Namen()
    {
        var cut = ZeigeMitNachladen(p => p
            .Add(x => x.StammSchreiben, (string n, int? g) =>
            {
                int i = _stand.FindIndex(e => e.Traeger == 11);
                _stand[i] = _stand[i] with { Text = "Erdgas H neu" };
                return true;
            }));

        cut.Find(".epos-traeger-inhalt .epos-leiste button").Click();

        Assert.Contains("Erdgas H neu", Listenmarkup(cut));
        // Die Markierung bleibt beim Träger — umbenannt ist nicht entfernt.
        Assert.Equal(11, cut.Instance.Traeger);
    }

    /// <summary>
    /// ET-3: Ein verwendeter, noch nicht zugeordneter Träger steht markiert in der
    /// Liste; das Speichern ordnet ihn zu — die Marke muss danach weg sein.
    /// </summary>
    [Fact]
    public void Nach_dem_Speichern_folgt_die_Liste_der_Zuordnung()
    {
        // Beim Öffnen trägt der Eintrag die Marke „nicht zugeordnet" ...
        var mitMarke = new List<EnergietraegerDialog.EnergietraegerListe>
        {
            new(null, "Gas"),
            new(11, "Erdgas H", "verwendet von: Heizkessel", false),
            new(12, "Flüssiggas"),
            new(null, "Strom"),
            new(21, "Elektrische Energie")
        };
        // ... nach dem Speichern nicht mehr, denn es hat ihn zugeordnet.
        _stand = new List<EnergietraegerDialog.EnergietraegerListe>(LISTE);

        _ansicht = new EnergietraegerAnsicht { Stand = Stand() };
        var cut = Render<EnergietraegerDialog>(p => p
            .Add(x => x.Liste, mitMarke)
            .Add(x => x.Katalogkontext, false)
            .Add(x => x.TraegerLaden, id => { _geladen = id; return _ansicht; })
            .Add(x => x.Nachrechnen, () => _ansicht)
            .Add(x => x.ListeNeuLaden,
                 () => (IReadOnlyList<EnergietraegerDialog.EnergietraegerListe>)_stand)
            .Add(x => x.Speichern, () => true));

        Assert.Contains("epos-traeger-eintrag--offen", Listenmarkup(cut));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Speichern").Click();

        Assert.DoesNotContain("epos-traeger-eintrag--offen", Listenmarkup(cut));
        Assert.Equal(11, cut.Instance.Traeger);
    }

    /// <summary>
    /// ET-6: Nach dem Entfernen steht ein Hinweis, WENN das Projekt seinen Stromträger
    /// im selben Atemzug wieder bekommen hat. Sonst sagt der Dialog nichts.
    /// </summary>
    [Fact]
    public void Das_Entfernen_nennt_den_wieder_zugeordneten_Stromtraeger()
    {
        var cut = ZeigeMitNachladen(p => p
            .Add(x => x.AusProjekt, () =>
            {
                _stand.RemoveAll(e => e.Traeger == 11);
                return (true, "");
            })
            .Add(x => x.StromZugeordnet, () => "Elektrische Energie")
            .Add(x => x.VorlageStromZugeordnet,
                 "Das Projekt führt elektrische Anlagen; der Stromträger „{0}\" wurde zugeordnet.")
            .Add(x => x.VorlageEntfernen, "Träger „{0}\" entfernen?"),
            katalog: false);

        cut.Find(".epos-traeger-liste .epos-leiste").QuerySelectorAll("button")[1].Click();
        cut.FindAll(".epos-rueckfrage .epos-knopf")[0].Click();

        Assert.Contains("elektrische Anlagen", cut.Instance.Meldung);
        Assert.Contains("Elektrische Energie", cut.Instance.Meldung);
    }

    [Fact]
    public void Der_Hinweis_zum_Stromtraeger_erscheint_auch_auf_Englisch()
    {
        var cut = ZeigeMitNachladen(p => p
            .Add(x => x.AusProjekt, () =>
            {
                _stand.RemoveAll(e => e.Traeger == 11);
                return (true, "");
            })
            .Add(x => x.StromZugeordnet, () => "Electrical energy")
            .Add(x => x.VorlageStromZugeordnet,
                 "The project has electrical equipment; the electricity carrier „{0}\" has been assigned.")
            .Add(x => x.VorlageEntfernen, "Remove carrier „{0}\"?"),
            katalog: false);

        cut.Find(".epos-traeger-liste .epos-leiste").QuerySelectorAll("button")[1].Click();
        cut.FindAll(".epos-rueckfrage .epos-knopf")[0].Click();

        Assert.Contains("electrical equipment", cut.Instance.Meldung);
        Assert.Contains("Electrical energy", cut.Instance.Meldung);
    }

    [Fact]
    public void Ohne_Wiederzuordnung_bleibt_der_Hinweis_aus()
    {
        var cut = ZeigeMitNachladen(p => p
            .Add(x => x.AusProjekt, () =>
            {
                _stand.RemoveAll(e => e.Traeger == 11);
                return (true, "");
            })
            .Add(x => x.StromZugeordnet, () => "")
            .Add(x => x.VorlageEntfernen, "Träger „{0}\" entfernen?"),
            katalog: false);

        cut.Find(".epos-traeger-liste .epos-leiste").QuerySelectorAll("button")[1].Click();
        cut.FindAll(".epos-rueckfrage .epos-knopf")[0].Click();

        Assert.Equal("", cut.Instance.Meldung);
    }

    /// <summary>
    /// Ohne Nachlade-Weg ändert sich nichts: Ein Prüfstand ohne Hülle zeichnet weiter
    /// die Liste, die er bekommen hat — der Delegat ist eine Zugabe, keine Pflicht.
    /// </summary>
    [Fact]
    public void Ohne_Nachlade_Weg_bleibt_die_Liste_stehen()
    {
        var cut = Zeige(p => p
            .Add(x => x.TraegerLoeschen, () => (true, ""))
            .Add(x => x.VorlageLoeschen, "Energieträger „{0}\" löschen?"));

        cut.Find(".epos-traeger-liste .epos-leiste").QuerySelectorAll("button")[2].Click();
        cut.FindAll(".epos-rueckfrage .epos-knopf")[0].Click();

        Assert.Contains("Erdgas H", Listenmarkup(cut));
        Assert.Null(cut.Instance.Traeger);
    }
}
