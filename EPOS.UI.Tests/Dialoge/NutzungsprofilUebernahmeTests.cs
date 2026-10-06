using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>„Nutzungsprofil übernehmen…"</b> (Stufe NP3b; Konzept Nutzungsprofile 6.2, NP-F13, NP-F17, NP-F18) — der Baustein
/// im Reiter „Konditionierung" und in der Zonenmatrix über dem reinen Weg der Hülle (ohne Datenbank, mit Profilen des
/// Prüfstands): Liste gruppiert nach Kategorie mit Kurzform, leeres Profil benannt ohne Vorschau, Nennwertzeile mit
/// Herleitung, Rückfrage nach P12 vor dem Eintragen (Vorgabe „Nein", wo ersetzt wird), Ergebnis im Arbeitsstand samt
/// Profilname der Zone — geschrieben wird erst mit OK. Ohne Delegaten kein Knopf.
/// </summary>
public class NutzungsprofilUebernahmeTests : EposBunitContext
{
    public NutzungsprofilUebernahmeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private readonly List<string> _meldungen = new();

    /// <summary>Ein Büro mit Kennwerten: Mo–Fr 7–18 Uhr, Heizen 21/17 °C, Geräte 8 W/m² (Q39).</summary>
    internal static Raumnutzungsprofil Buero() => new()
    {
        Id = 11,
        Bezeichner = "Büro Probe",
        Nummer = "1",
        Nutzung_Von = 7,
        Nutzung_Bis = 18,
        Nutzungstage_Woche = "1111100",
        Heiz_Soll = 21,
        Heiz_Soll_Ausserhalb = 17,
        Geraete_Leistung = 8,
        Geraete_Anteil = 1,
        Geraete_Anteil_Ausserhalb = 0.1,
    };

    /// <summary>Ein Normprofil ohne Werte (NP-F13).</summary>
    internal static Raumnutzungsprofil Leer() => new() { Id = 12, Bezeichner = "Einzelbüro", Nummer = "1" };

    /// <summary>Außenluft flächenbezogen, rund um die Uhr (NP-F10).</summary>
    internal static Raumnutzungsprofil Flaechenluft() => new()
    {
        Id = 13,
        Bezeichner = "Halle Probe",
        Nutzung_Von = 0,
        Nutzung_Bis = 24,
        Nutzungstage_Woche = "1111111",
        Aussenluft = 3,
        Aussenluft_Einheit = RaumnutzungSchema.EINHEIT_JE_FLAECHE,
    };

    internal static IReadOnlyList<(string, Raumnutzungsprofil)> Profile() => new[]
    {
        ("Eigene", Buero()), ("Eigene", Flaechenluft()), ("DIN V 18599-10", Leer()),
    };

    internal static KonditionierungWeg Weg(bool mitProfilen = true)
        => KonditionierungHuelle.ReinerWeg(Kalendereigentuemer.Katalogbau, projekt: false,
                                           profile: mitProfilen ? Profile() : null);

    private (IRenderedComponent<NutzungsprofilUebernahme> Cut, KonditionierungBearbeitung Bearbeitung, GebaeudeArbeitsstand Arbeit)
        Aufbauen(KonditionierungWeg weg, GebaeudeKatalogDaten? satz = null, ZoneDaten? zone = null, bool lesemodus = false)
    {
        var arbeit = new GebaeudeArbeitsstand();
        arbeit.Laden(satz ?? KalenderkarteTests.Satz(), neu: false);
        if (zone is not null) arbeit.Zonen.Add(zone);
        KonditionierungBearbeitung b = zone is null
            ? new KonditionierungBearbeitung(arbeit, () => weg)
            : new KonditionierungBearbeitung(arbeit, zone, () => weg);
        b.Melden = (m, _) => _meldungen.Add(m);
        var cut = Render<NutzungsprofilUebernahme>(p => p
            .Add(x => x.Bearbeitung, b)
            .Add(x => x.Texte, new RaumnutzungTexte())
            .Add(x => x.Lesemodus, lesemodus));
        return (cut, b, arbeit);
    }

    [Fact]
    public void Ohne_Delegaten_und_im_Lesemodus_steht_kein_Knopf()
    {
        Assert.Empty(Aufbauen(Weg(mitProfilen: false)).Cut.FindAll(".epos-nutzungsuebernahme-oeffnen"));
        Assert.Empty(Aufbauen(Weg(), lesemodus: true).Cut.FindAll(".epos-nutzungsuebernahme-oeffnen"));
        Assert.False(KonditionierungWeg.Keiner.Bietet(KonditionierungHandlung.ProfilUebernehmen));
    }

    [Fact]
    public void Die_Liste_gruppiert_nach_Kategorie_mit_Kurzform_und_benennt_das_leere_Profil()
    {
        var (cut, _, _) = Aufbauen(Weg());
        cut.Find(".epos-nutzungsuebernahme-oeffnen").Click();

        var gruppen = cut.FindAll(".epos-nutzungsuebernahme-gruppe");
        Assert.Equal(new[] { "Eigene", "DIN V 18599-10" }, gruppen.Select(g => g.GetAttribute("aria-label")).ToArray());
        var buero = cut.Find("[data-profil='11'] .epos-nutzungsuebernahme-kurz").TextContent;
        Assert.Contains("Mo–Fr", buero);
        Assert.Contains("7–18 h", buero);
        Assert.Contains("W/m²", buero);
        Assert.Equal("ohne Werte", cut.Find("[data-profil='12'] .epos-nutzungsuebernahme-kurz").TextContent);

        // Ohne Wahl ist „Übernehmen" weich gesperrt und meldet den Versuch.
        var knopf = cut.Find(".epos-nutzungsuebernahme-uebernehmen");
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        knopf.Click();
        Assert.Contains(new RaumnutzungTexte().GrundOhneWahl, _meldungen);
    }

    [Fact]
    public void Die_Wahl_zeigt_Vorschau_und_Nennwertzeile_mit_Herleitung_ohne_den_Stand_zu_aendern()
    {
        var (cut, b, arbeit) = Aufbauen(Weg());
        int fassung = arbeit.Stand.Konditionierung!.Fassung;
        cut.Find(".epos-nutzungsuebernahme-oeffnen").Click();
        cut.Find("[data-profil='11']").Click();

        Assert.Equal("true", cut.Find("[data-profil='11']").GetAttribute("aria-selected"));
        Assert.NotEmpty(cut.FindAll(".epos-nutzungsuebernahme-vorschau svg"));
        // 8 W/m² × 150 m² (Nutzfläche des Satzes) = 1200 W (NP-F18, Q39).
        string zeile = Assert.Single(b.Profilnennwerte(new RaumnutzungTexte()));
        Assert.Contains("8 W/m² × 150 m² = 1200 W", zeile);
        Assert.Contains("8 W/m² × 150 m² = 1200 W", cut.Markup);
        Assert.Equal(fassung, arbeit.Stand.Konditionierung!.Fassung);
        Assert.Null(b.OffeneFrage);
    }

    /// <summary>
    /// E93: Die Vorschau nennt die Nutzungstage am Ziel — abgeleitet aus Wochenmuster und Feiertagen (Mo–Fr ohne
    /// Feiertagsvorgabe im Bezugsjahr 2025: 261), abzüglich der Ferientage des Gebäudes (1. bis 30. Juli: 22 Werktage).
    /// </summary>
    [Fact]
    public void Die_Vorschau_nennt_die_Nutzungstage_am_Ziel_abzueglich_seiner_Ferien()
    {
        GebaeudeKatalogDaten satz = KalenderkarteTests.Satz();
        satz.Ferienbeginn = new[] { 182, 0, 0, 0 };
        satz.Ferienende = new[] { 211, 0, 0, 0 };
        var (cut, b, _) = Aufbauen(Weg(), satz);
        cut.Find(".epos-nutzungsuebernahme-oeffnen").Click();
        cut.Find("[data-profil='11']").Click();

        const string erwartet = "Nutzungstage im Jahr: 261 (aus Wochenmuster und Feiertagen), abzüglich 22 Ferientage = 239";
        Assert.Equal(erwartet, b.Profilnutzungstage());
        Assert.Equal(erwartet, cut.Find(".epos-nutzungsuebernahme-tage .epos-herleitung-text").TextContent);

        // Ein Profil ohne Werte nennt keine Nutzungstage.
        cut.Find("[data-profil='12']").Click();
        Assert.Empty(cut.FindAll(".epos-nutzungsuebernahme-tage"));
    }

    [Fact]
    public void Uebernehmen_fragt_vorher_und_traegt_mit_Ja_in_den_Arbeitsstand()
    {
        var (cut, b, arbeit) = Aufbauen(Weg());
        cut.Find(".epos-nutzungsuebernahme-oeffnen").Click();
        cut.Find("[data-profil='11']").Click();
        cut.Find(".epos-nutzungsuebernahme-uebernehmen").Click();

        // Ein leeres Ziel: nichts wird ersetzt - die Vorgabe ist nicht „Nein".
        KonditionierungBearbeitung.Rueckfrage frage = b.OffeneFrage!;
        Assert.False(frage.VorgabeNein);
        Assert.Contains("„Büro Probe“", frage.Text);
        Assert.Contains("8 W/m² × 150 m² = 1200 W", frage.Text);
        Assert.False(b.Angelegt(KonditionierungGroesse.Heizen));

        b.Beantworten(true);
        Assert.True(b.Angelegt(KonditionierungGroesse.Heizen));
        Assert.True(b.Angelegt(KonditionierungGroesse.Geraete));
        // NP2b-5c: Die Herkunft ist ein Nutzungsprofil, keine Vorlage — unterschieden über die Herkunftsart.
        Assert.Null(b.Herkunft(KonditionierungGroesse.Heizen));
        Assert.Equal("Nutzungsprofil Büro Probe", b.HerkunftText(KonditionierungGroesse.Heizen));
        Assert.Equal(1200, arbeit.Stand.Konditionierung!.Spalte(KonditionierungGroesse.Geraete).Kalender!.Nennwert);

        // Ein zweites Mal ersetzt es den Matrixbereich (P12): die Rückfrage steht mit Vorgabe „Nein".
        b.ProfilWaehlen(11);
        Assert.True(b.ProfilUebernehmenFragen(new RaumnutzungTexte()));
        Assert.True(b.OffeneFrage!.VorgabeNein);
        Assert.Contains(new RaumnutzungTexte().FrageErsetzt, b.OffeneFrage.Text);
        b.Beantworten(false);

        // „Zurücknehmen" nimmt die Übernahme als EINEN Schritt zurück.
        Assert.True(b.Zuruecknehmen());
        Assert.False(b.Angelegt(KonditionierungGroesse.Heizen));
    }

    [Fact]
    public void An_der_Zone_traegt_der_Arbeitsstand_den_Profilnamen_auch_bei_einem_leeren_Profil()
    {
        var zone = new ZoneDaten { Id = 7, Bezeichner = "Büro", Nutzflaeche = 40, Konditionierung = new KonditionierungDaten() };
        var (cut, b, _) = Aufbauen(Weg(), zone: zone);
        cut.Find(".epos-nutzungsuebernahme-oeffnen").Click();

        // Leeres Profil (NP-F13): keine Vorschau, an der Zone trotzdem übernehmbar - nur der Name.
        cut.Find("[data-profil='12']").Click();
        Assert.NotEmpty(cut.FindAll(".epos-nutzungsuebernahme-ohnewerte"));
        Assert.Empty(cut.FindAll(".epos-nutzungsuebernahme-vorschau svg"));
        Assert.Null(b.Profilsperre(new RaumnutzungTexte()));
        cut.Find(".epos-nutzungsuebernahme-uebernehmen").Click();
        Assert.Contains(new RaumnutzungTexte().FrageOhneWerte, b.OffeneFrage!.Text);
        b.Beantworten(true);
        Assert.Equal("Einzelbüro", zone.Nutzungsprofil);
        Assert.False(b.Angelegt(KonditionierungGroesse.Heizen));

        // Ein Profil mit Werten: Kalender an der Zone, Nennwert aus ihrer Fläche (8 × 40 = 320 W).
        b.ProfilWaehlen(11);
        Assert.Contains("8 W/m² × 40 m² = 320 W", b.Profilnennwerte(new RaumnutzungTexte()).Single());
        Assert.True(b.ProfilUebernehmenFragen(new RaumnutzungTexte()));
        b.Beantworten(true);
        Assert.Equal("Büro Probe", zone.Nutzungsprofil);
        Assert.True(b.Angelegt(KonditionierungGroesse.Heizen));
    }

    /// <summary>
    /// Rote Probe NP-F10: Außenluft in m³/(h·m²) ohne lichte Höhe des Ziels setzt die Lüftung benannt nicht; mit der
    /// Raumhöhe des Gebäudes bzw. der Zone wird sie umgerechnet und übernommen.
    /// </summary>
    [Fact]
    public void Aussenluft_je_Flaeche_braucht_die_lichte_Hoehe_des_Ziels()
    {
        var t = new RaumnutzungTexte();
        // Ohne Höhe: benannt, nicht gesetzt.
        var (_, ohne, _) = Aufbauen(Weg());
        Assert.Null(ohne.Zielhoehe);
        ohne.ProfilWaehlen(13);
        Assert.Contains(ohne.Profilhinweise(), h => h.Contains("m³/(h·m²)"));
        // Das Profil trägt nur die Lüftung: am Gebäude ist nichts zu übernehmen, der Grund ist der Hinweis.
        Assert.Contains("m³/(h·m²)", ohne.Profilsperre(t));
        Assert.False(ohne.Profilwoche(KonditionierungGroesse.Lueftung) is not null);

        // Mit der Raumhöhe des Gebäudes: 3 m³/(h·m²) ÷ 3 m = 1 1/h, rund um die Uhr.
        GebaeudeKatalogDaten satz = KalenderkarteTests.Satz();
        satz.Raumhoehe = 3;
        var (_, mit, _) = Aufbauen(Weg(), satz);
        Assert.Equal(3, mit.Zielhoehe);
        mit.ProfilWaehlen(13);
        Assert.Empty(mit.Profilhinweise());
        Assert.True(mit.ProfilUebernehmenFragen(t));
        mit.Beantworten(true);
        Assert.True(mit.Angelegt(KonditionierungGroesse.Lueftung));

        // An der Zone gilt ihre eigene Höhe vor der des Gebäudes.
        var zone = new ZoneDaten { Id = 7, Bezeichner = "Halle", Nutzflaeche = 40, Raumhoehe = 6, Konditionierung = new KonditionierungDaten() };
        var (_, anZone, _) = Aufbauen(Weg(), satz, zone);
        Assert.Equal(6, anZone.Zielhoehe);
        Assert.Equal(40, anZone.Zielflaeche);
    }

    [Fact]
    public void Am_Gebaeude_ist_ein_leeres_Profil_weich_gesperrt()
    {
        var (_, b, _) = Aufbauen(Weg());
        b.ProfilWaehlen(12);
        Assert.Equal(new RaumnutzungTexte().GrundOhneWerte, b.Profilsperre(new RaumnutzungTexte()));
        Assert.False(b.ProfilUebernehmenFragen(new RaumnutzungTexte()));
        Assert.Null(b.OffeneFrage);
    }
}
