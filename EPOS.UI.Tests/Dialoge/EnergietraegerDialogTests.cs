using System.Globalization;
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
public class EnergietraegerDialogTests : BunitContext
{
    private readonly CultureInfo _kulturVorher = CultureInfo.CurrentCulture;

    public EnergietraegerDialogTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentCulture = _kulturVorher;
        base.Dispose(disposing);
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
                new PreishistorieZeile("01.01.2026", "10,10", "Nm³", "0,62", "120,00", "12,00")
            }
        };
        if (strom)
        {
            s.Aufschlaege = new StromAufschlaegeStand { Aufgeschluesselt = true };
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
    public void Die_Traegerkarte_zeigt_ihre_vier_Abschnitte()
    {
        var cut = Zeige();

        // Preise, Umrechnung, Emissionen, Historie
        Assert.Equal(4, cut.FindAll(".epos-traegerkarte > .epos-gruppenkopf").Count);
        Assert.Equal("Erdgas H  (VDI 3805 3)", cut.Find(".epos-traeger-name").TextContent);
        Assert.Equal("Gruppe: Gas", cut.Find(".epos-traeger-gruppe").TextContent);
    }

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

        Assert.Contains("CO2  [g/kWh]", cut.Markup);
        Assert.Contains("SO2  [g/kWh]", cut.Markup);
        Assert.Contains("NOx  [g/kWh]", cut.Markup);
    }

    [Fact]
    public void Eine_nur_lesende_Emissionszeile_sperrt_Feld_und_Katalogknopf()
    {
        var cut = Zeige();

        // Raster 0 = Umrechnungsregeln, 1 = Emissionen, 2 = Preishistorie.
        var zeilen = cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr");
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

        cut.Find(".epos-traeger-inhalt .epos-feldpaar button").Click();

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

        var zeilen = cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr");
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

    [Fact]
    public void Der_Hilfeschluessel_bleibt_der_der_Maske()
    {
        var hilfe = new TestHilfe();
        Services.AddSingleton<IHilfeDienst>(hilfe);

        var cut = Zeige();
        cut.Find(".epos-infoknopf").Click();

        Assert.Equal(new[] { "Form_Energietraeger.btn_Help" }, hilfe.Geoeffnet);
    }
}
