using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Gruppe „Wärmeübergabe" im Gebäude-Katalogeditor</b> (Konzept Anlagenkopplung 9.1, 9.2,
/// 9.6 Maske 2 und 3; Stufe AK1 Welle 3; E20, E24/E25, H1, H8, H10, H11, H12).
///
/// <para><b>Was geprüft wird — je Maske ein Fall nach 9.6:</b> die SICHTBARKEITSREGEL (der Haken
/// steht immer, die Übergabeart erst mit ihm, die übrigen Felder und das Zeitprogramm erst mit
/// einer rechnenden Art), das NULL-VERHALTEN (ein leeres Feld zeigt die Vorgabe als Zahl und
/// speichert NULL), die PRÜFREGELN (Auslegungspunkt, Bereiche, Zeitprogramm streng) und die
/// HERLEITUNGSZEILEN mit der Zahl des Kerns — dazu das Proportionalband (Schnellwahl oder frei),
/// die Entprellung der Herleitung und das Zeitprogramm als Wochenraster.</para>
///
/// <para>Die Kultur ist auf de-DE gepinnt: Die Erwartungswerte sind deutsche Beschriftungen und
/// Zahlen.</para>
/// </summary>
public class GebaeudeWaermeuebergabeTests : EposBunitContext
{
    private const string HAKEN = "Übergabe rechnen (statt idealer Regelung)";
    private const string ART = "Übergabeart :";

    public GebaeudeWaermeuebergabeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Ein vollständig belegter Satz, der jede übrige Prüfung besteht (Soll am Tag 20 °C).</summary>
    private static GebaeudeKatalogDaten Satz() => new()
    {
        Name = "Haus A",
        Typ = "Einfamilienhaus",
        Beschreibung = "Beschreibung",
        Gebaeudeart = "Hotel",
        Verwendung = "Wohngebaeude",
        Baualtersklasse = 4,
        Bauart = 1,
        WohnflaecheGesamt = 150,
        FlaecheNutzer = 35,
        Waermegewinne = 400,
        Fensterdurchlassgrad = 0.4,
        Raumhoehe = 2.5,
        FensterflaecheNord = 10,
        FensterflaecheSued = 20,
        FensterflaecheOstWest = 15,
        FlaecheAussenwand = 200,
        Dachflaeche = 120,
        Grundflaeche = 100,
        SonstigeFlaechen = 5,
        UWertAussenwand = 0.3,
        UWertFenster = 1.3,
        UWertDachflaeche = 0.2,
        UWertGrundflaeche = 0.35,
        UWertSonstiges = 0.5,
        WbvkFensterWand = 0.1,
        AnschlussFensterWand = 50,
        SollTag = 20,
        NachtAbsenkung = 17,
        MaxTemperatur = 24,
        WochenendAbsenkung = 0,
        SollFerien = 0,
        Luftwechselrate = 0.5,
        Modell = DbWerte.GEBAEUDE_MODELL_VDI6007
    };

    /// <summary>Derselbe Satz mit eingeschaltetem Heizkreis und Radiator, alle Übergabefelder leer.</summary>
    private static GebaeudeKatalogDaten Radiator()
    {
        GebaeudeKatalogDaten d = Satz();
        d.HeizkreisAktiv = true;
        d.UebergabeArt = DbWerte.UEBERGABE_RADIATOR;
        return d;
    }

    private IRenderedComponent<GebaeudeKatalogDialog> Aufbauen(
        GebaeudeKatalogDaten? daten = null,
        Func<GebaeudeKatalogDaten, bool, string, GebaeudeKatalogErgebnis>? speichern = null,
        Func<GebaeudeKatalogDaten, UebergabeHerleitungDaten?>? herleitung = null,
        int entprellungMs = 0)
        => Render<GebaeudeKatalogDialog>(p => p
            .Add(x => x.Daten, daten ?? Satz())
            .Add(x => x.Modus, GebaeudeKatalogModus.Bearbeiten)
            .Add(x => x.Gebaeudetypen, () => new[] { "Einfamilienhaus" })
            .Add(x => x.Gebaeudearten, () => new[] { "Hotel" })
            .Add(x => x.Baualtersklassen, new[] { "bis 1859", "1860 bis 1918", "1919 bis 1948", "1949 bis 1957", "1958 bis 1968" })
            .Add(x => x.Katalognamen, () => new[] { "Haus A" })
            .Add(x => x.Lies, _ => Satz())
            .Add(x => x.Speichern, speichern ?? ((_, _, _) => new GebaeudeKatalogErgebnis(true, "")))
            .Add(x => x.UebergabeHerleitung, herleitung)
            .Add(x => x.EntprellungMs, entprellungMs));

    private static string Gruppe(IRenderedComponent<GebaeudeKatalogDialog> cut)
        => cut.Find("div.gebk-waermeuebergabe").TextContent;

    private static IElement? FeldOderNull(IRenderedComponent<GebaeudeKatalogDialog> cut, string beschriftung)
        => cut.FindAll("div.gebk-waermeuebergabe label.epos-feld")
              .FirstOrDefault(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == beschriftung);

    private static IElement Eingabe(IRenderedComponent<GebaeudeKatalogDialog> cut, string beschriftung)
        => FeldOderNull(cut, beschriftung)!.QuerySelector("input")!;

    private static IElement Haken(IRenderedComponent<GebaeudeKatalogDialog> cut, string beschriftung)
        => cut.FindAll("div.gebk-waermeuebergabe label.epos-schalter")
              .First(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == beschriftung)
              .QuerySelector("input")!;

    private static IElement Band(IRenderedComponent<GebaeudeKatalogDialog> cut, string text)
        => cut.FindAll("div.gebk-waermeuebergabe label")
              .First(l => l.QuerySelector("input[type=radio]") is not null
                          && l.TextContent.Trim() == text)
              .QuerySelector("input[type=radio]")!;

    private static void Ok(IRenderedComponent<GebaeudeKatalogDialog> cut)
        => cut.Find(".epos-leiste button.epos-knopf--primaer").Click();

    // =================================================================================
    // Sichtbarkeitsregel (9.1)
    // =================================================================================

    [Fact]
    public void Die_Gruppe_steht_immer_da_und_ohne_Haken_nur_mit_dem_Haken()
    {
        var cut = Aufbauen();

        Assert.Contains(cut.FindAll(".epos-gruppenkopf-titel, .epos-gruppenkopf h2, .epos-gruppenkopf h3"),
                        e => e.TextContent.Contains("Wärmeübergabe"));
        Assert.False(Haken(cut, HAKEN).HasAttribute("checked"));
        Assert.Null(FeldOderNull(cut, ART));
        Assert.Null(FeldOderNull(cut, "Exponent :"));
        Assert.Empty(cut.FindAll("div.gebk-waermeuebergabe table.epos-wochenraster-tabelle"));
        Assert.Contains("Ohne Haken rechnet das Gebäude wie bisher mit idealer Regelung.", Gruppe(cut));
        Assert.Contains("Projekteinstellung Anlagenkopplung", Gruppe(cut));
    }

    [Fact]
    public void Der_Haken_zeigt_die_Uebergabeart_und_erst_eine_rechnende_Art_die_Felder()
    {
        var cut = Aufbauen();

        Haken(cut, HAKEN).Change(true);
        Assert.NotNull(FeldOderNull(cut, ART));
        Assert.Null(FeldOderNull(cut, "Exponent :"));
        Assert.Contains("Übergabeart „ideal“", Gruppe(cut));

        // Einträge: ideal, Radiator, Flächenheizung, Konvektor - gewählt wird die Id.
        IElement liste = FeldOderNull(cut, ART)!.QuerySelector("select")!;
        Assert.Equal(new[] { "ideal (keine Übergabe)", "Radiator", "Flächenheizung", "Konvektor" },
                     liste.QuerySelectorAll("option").Select(o => o.TextContent.Trim()));
        liste.Change("1");

        foreach (string feld in new[] { "Exponent :", "Auslegung Vorlauf :", "Auslegung Rücklauf :", "Auslegung Raum :",
                                        "Auslegungs-Außentemperatur :", "Nennleistung der Übergabe :" })
            Assert.NotNull(FeldOderNull(cut, feld));
        Assert.NotEmpty(cut.FindAll("div.gebk-waermeuebergabe table.epos-wochenraster-tabelle"));

        // Beim ersten Einschalten schlägt der Dialog die Heizkurve vor (8.1); Niveau und Steilheit
        // stehen mit ihrem Haken und gehen mit ihm.
        Assert.True(Haken(cut, "Heizkurve fahren").HasAttribute("checked"));
        Assert.NotNull(FeldOderNull(cut, "Niveau der Heizkurve :"));
        Assert.NotNull(FeldOderNull(cut, "Steilheit der Heizkurve :"));
        Haken(cut, "Heizkurve fahren").Change(false);
        Assert.Null(FeldOderNull(cut, "Niveau der Heizkurve :"));
        Assert.Null(FeldOderNull(cut, "Steilheit der Heizkurve :"));
    }

    [Fact]
    public void Auf_dem_Tagesbilanz_Weg_sagt_eine_Zeile_dass_die_Eingaben_erst_mit_VDI_6007_gelten()
    {
        GebaeudeKatalogDaten d = Radiator();
        d.Modell = DbWerte.GEBAEUDE_MODELL_TAGESBILANZ;
        var cut = Aufbauen(d);

        Assert.Contains("Tagesbilanz (Bestandsweg) rechnet keine Anlagenkopplung", Gruppe(cut));

        var vdi = Aufbauen(Radiator());
        Assert.DoesNotContain("Tagesbilanz (Bestandsweg) rechnet keine Anlagenkopplung", Gruppe(vdi));
    }

    // =================================================================================
    // NULL-Verhalten (9.1, 8.4): leer = Vorgabe als Zahl, gespeichert wird NULL
    // =================================================================================

    [Fact]
    public void Ein_leeres_Feld_zeigt_die_Vorgabe_der_Art_als_Zahl()
    {
        var cut = Aufbauen(Radiator());

        Assert.Equal("Vorgabe 1,3", Eingabe(cut, "Exponent :").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe 55", Eingabe(cut, "Auslegung Vorlauf :").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe 45", Eingabe(cut, "Auslegung Rücklauf :").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe 20", Eingabe(cut, "Auslegung Raum :").GetAttribute("placeholder"));
        // Ohne Herleitungsweg (kein Projekt) sagt der Platzhalter, dass die Zahl hergeleitet wird.
        Assert.Equal("Vorgabe: hergeleitet", Eingabe(cut, "Auslegungs-Außentemperatur :").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe: hergeleitet", Eingabe(cut, "Nennleistung der Übergabe :").GetAttribute("placeholder"));
        Assert.Equal("", Eingabe(cut, "Exponent :").GetAttribute("value") ?? "");
    }

    [Fact]
    public void Leere_Felder_speichern_NULL_und_gesetzte_ihren_Wert()
    {
        GebaeudeKatalogDaten geschrieben = null!;
        var cut = Aufbauen(Radiator(), speichern: (d, _, _) => { geschrieben = d; return new(true, ""); });

        Eingabe(cut, "Exponent :").Input("1,25");
        Ok(cut);

        Assert.True(geschrieben.HeizkreisAktiv);
        Assert.Equal(DbWerte.UEBERGABE_RADIATOR, geschrieben.UebergabeArt);
        Assert.Equal(1.25, geschrieben.UebergabeExponent);
        Assert.Null(geschrieben.AuslegungVorlauf);
        Assert.Null(geschrieben.AuslegungRuecklauf);
        Assert.Null(geschrieben.AuslegungRaumtemperatur);
        Assert.Null(geschrieben.AuslegungAussentemperatur);
        Assert.Null(geschrieben.UebergabeLeistungNennKw);
        Assert.False(geschrieben.HeizkurveAktiv);
        Assert.Null(geschrieben.HeizkurveNiveau);
        Assert.Null(geschrieben.HeizkurveSteilheit);
        Assert.Null(geschrieben.ReglerProportionalband);
        Assert.Null(geschrieben.Sollwertprofil);
    }

    [Fact]
    public void Ohne_Haken_bleiben_die_Eingaben_stehen_und_werden_mitgespeichert()
    {
        GebaeudeKatalogDaten d = Radiator();
        d.AuslegungVorlauf = 50;
        d.UebergabeExponent = 1.2;
        GebaeudeKatalogDaten geschrieben = null!;
        var cut = Aufbauen(d, speichern: (x, _, _) => { geschrieben = x; return new(true, ""); });

        Haken(cut, HAKEN).Change(false);
        Ok(cut);

        Assert.False(geschrieben.HeizkreisAktiv);
        Assert.Equal(DbWerte.UEBERGABE_RADIATOR, geschrieben.UebergabeArt);
        Assert.Equal(50.0, geschrieben.AuslegungVorlauf);
        Assert.Equal(1.2, geschrieben.UebergabeExponent);
    }

    /// <summary>
    /// <b>Ein- und wieder Ausschalten ist keine Änderung</b> (Befund 25.09.2026): Der Vorschlag der
    /// Heizkurve beim Einschalten fällt mit dem Haken — OK schreibt keinen Satz ohne Heizkreis mit
    /// <c>HeizkurveAktiv = true</c>, den der Anwender nie gesehen hat.
    /// </summary>
    [Fact]
    public void Ein_und_Ausschalten_nimmt_den_Vorschlag_der_Heizkurve_zurueck()
    {
        GebaeudeKatalogDaten geschrieben = null!;
        var cut = Aufbauen(speichern: (x, _, _) => { geschrieben = x; return new(true, ""); });

        Haken(cut, HAKEN).Change(true);
        Haken(cut, HAKEN).Change(false);
        Ok(cut);

        Assert.False(geschrieben.HeizkreisAktiv);
        Assert.False(geschrieben.HeizkurveAktiv);
    }

    /// <summary>
    /// Hat der Anwender den Haken „Heizkurve fahren" selbst gesetzt, ist es seine Wahl: Sie bleibt
    /// beim Ausschalten des Heizkreises stehen wie jede andere Eingabe der Gruppe.
    /// </summary>
    [Fact]
    public void Eine_selbst_gewaehlte_Heizkurve_bleibt_beim_Ausschalten_stehen()
    {
        GebaeudeKatalogDaten geschrieben = null!;
        var cut = Aufbauen(speichern: (x, _, _) => { geschrieben = x; return new(true, ""); });

        Haken(cut, HAKEN).Change(true);
        FeldOderNull(cut, ART)!.QuerySelector("select")!.Change("1");
        Haken(cut, "Heizkurve fahren").Change(false);
        Haken(cut, "Heizkurve fahren").Change(true);
        Haken(cut, HAKEN).Change(false);
        Ok(cut);

        Assert.False(geschrieben.HeizkreisAktiv);
        Assert.True(geschrieben.HeizkurveAktiv);
    }

    // =================================================================================
    // Prüfregeln (9.1, 9.5)
    // =================================================================================

    [Fact]
    public void Ein_Auslegungsvorlauf_unter_der_Raumtemperatur_meldet_beim_OK()
    {
        bool geschrieben = false;
        var cut = Aufbauen(Radiator(), speichern: (_, _, _) => { geschrieben = true; return new(true, ""); });

        // 25 °C ist der kleinste zulässige Vorlauf, 26 °C die größte Raumtemperatur - beide Felder
        // gültig, der Zusammenhang nicht.
        Eingabe(cut, "Auslegung Raum :").Input("26");
        Eingabe(cut, "Auslegung Vorlauf :").Input("25");
        Ok(cut);

        Assert.False(geschrieben);
        Assert.Contains("Der Auslegungsvorlauf 25,0 °C liegt nicht über der Auslegungs-Raumtemperatur 26,0 °C",
                        cut.Instance.Meldung);
    }

    [Fact]
    public void Ein_Ruecklauf_ueber_dem_Vorlauf_meldet_beim_OK()
    {
        bool geschrieben = false;
        var cut = Aufbauen(Radiator(), speichern: (_, _, _) => { geschrieben = true; return new(true, ""); });

        Eingabe(cut, "Auslegung Rücklauf :").Input("60");
        Ok(cut);

        Assert.False(geschrieben);
        Assert.Contains("Der Auslegungsrücklauf 60,0 °C muss zwischen", cut.Instance.Meldung);
    }

    [Fact]
    public void Ein_Exponent_ausserhalb_des_Bereichs_faerbt_und_wird_nicht_uebernommen()
    {
        GebaeudeKatalogDaten geschrieben = null!;
        var cut = Aufbauen(Radiator(), speichern: (d, _, _) => { geschrieben = d; return new(true, ""); });

        Eingabe(cut, "Exponent :").Input("9");

        Assert.Contains("epos-fehleingabe", Eingabe(cut, "Exponent :").ClassName);
    }

    [Fact]
    public void Ein_Zeitprogramm_mit_falscher_Wertzahl_meldet_beim_OK()
    {
        GebaeudeKatalogDaten d = Radiator();
        d.Sollwertprofil = "20;20";
        bool geschrieben = false;
        var cut = Aufbauen(d, speichern: (_, _, _) => { geschrieben = true; return new(true, ""); });

        Ok(cut);

        Assert.False(geschrieben);
        Assert.Contains("Das Sollwert-Zeitprogramm hat 2 statt 168 Werte.", cut.Instance.Meldung);
    }

    [Fact]
    public void Ohne_Haken_gilt_keine_Regel_der_Gruppe()
    {
        GebaeudeKatalogDaten d = Satz();
        d.UebergabeArt = DbWerte.UEBERGABE_RADIATOR;
        d.AuslegungVorlauf = 15;          // widerspräche dem Raum - aber ohne Haken ruht die Gruppe
        d.Sollwertprofil = "20";
        bool geschrieben = false;
        var cut = Aufbauen(d, speichern: (_, _, _) => { geschrieben = true; return new(true, ""); });

        Ok(cut);

        Assert.True(geschrieben);
    }

    // =================================================================================
    // Herleitungszeilen mit der Zahl des Kerns (8.4, H10) - entprellt, sofort beim Öffnen
    // =================================================================================

    [Fact]
    public void Die_Herleitungszeilen_nennen_die_hergeleiteten_Zahlen()
    {
        var cut = Aufbauen(Radiator(), herleitung: _ => new UebergabeHerleitungDaten(-12, 8.4, ""));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("das kälteste Tagesmittel der Klimareihe des Projekts, abgerundet — -12 °C", Gruppe(cut));
            Assert.Contains("— 8,4 kW", Gruppe(cut));
            Assert.Equal("Vorgabe -12", Eingabe(cut, "Auslegungs-Außentemperatur :").GetAttribute("placeholder"));
            Assert.Equal("Vorgabe 8,4", Eingabe(cut, "Nennleistung der Übergabe :").GetAttribute("placeholder"));
        });
    }

    [Fact]
    public void Ein_Befund_des_Kerns_steht_statt_der_Zahl()
    {
        var cut = Aufbauen(Radiator(), herleitung: _ => new UebergabeHerleitungDaten(null, null, "Die Klimareihe fehlt."));

        cut.WaitForAssertion(() =>
            Assert.Contains("Keine hergeleitete Zahl: Die Klimareihe fehlt.", Gruppe(cut)));
    }

    [Fact]
    public void Die_Herleitung_laeuft_beim_Oeffnen_sofort_und_danach_entprellt()
    {
        int aufrufe = 0;
        var cut = Aufbauen(Radiator(), herleitung: _ => { aufrufe++; return new UebergabeHerleitungDaten(-10, 5.0, ""); },
                           entprellungMs: 60_000);

        cut.WaitForAssertion(() => Assert.Equal(1, aufrufe));

        // Eingaben innerhalb der Entprellung fragen den Kern nicht je Zeichen.
        Eingabe(cut, "Auslegung Vorlauf :").Input("5");
        Eingabe(cut, "Auslegung Vorlauf :").Input("50");
        Assert.Equal(1, aufrufe);
        Assert.Equal(1, cut.FindComponent<GebaeudeWaermeuebergabeFelder>().Instance.Herleitungen);
    }

    [Fact]
    public void Die_Herleitung_bekommt_den_Stand_des_Dialogs()
    {
        GebaeudeKatalogDaten? gesehen = null;
        var cut = Aufbauen(Radiator(), herleitung: d => { gesehen = d; return null; });

        Eingabe(cut, "Auslegung Raum :").Input("21");

        cut.WaitForAssertion(() => Assert.Equal(21.0, gesehen?.AuslegungRaumtemperatur));
        Assert.Equal(DbWerte.UEBERGABE_RADIATOR, gesehen!.UebergabeArt);
    }

    // =================================================================================
    // Proportionalband (H1, E25): Schnellwahl 0,5 / 1 / 2 K oder frei
    // =================================================================================

    [Fact]
    public void Das_Band_waehlt_schnell_und_die_Vorgabe_bleibt_NULL()
    {
        GebaeudeKatalogDaten geschrieben = null!;
        var cut = Aufbauen(Radiator(), speichern: (d, _, _) => { geschrieben = d; return new(true, ""); });

        Assert.True(Band(cut, "1,0 K").HasAttribute("checked"));
        Band(cut, "2,0 K").Change(true);
        Ok(cut);
        Assert.Equal(2.0, geschrieben.ReglerProportionalband);

        var zweiter = Aufbauen(Radiator(), speichern: (d, _, _) => { geschrieben = d; return new(true, ""); });
        Band(zweiter, "0,5 K").Change(true);
        Band(zweiter, "1,0 K").Change(true);
        Ok(zweiter);
        Assert.Null(geschrieben.ReglerProportionalband);
    }

    [Fact]
    public void Frei_zeigt_das_Feld_und_speichert_den_Wert()
    {
        GebaeudeKatalogDaten geschrieben = null!;
        var cut = Aufbauen(Radiator(), speichern: (d, _, _) => { geschrieben = d; return new(true, ""); });

        Assert.Null(FeldOderNull(cut, "Proportionalband (frei) :"));
        Band(cut, "frei").Change(true);
        Eingabe(cut, "Proportionalband (frei) :").Input("3,5");
        Ok(cut);

        Assert.Equal(3.5, geschrieben.ReglerProportionalband);
    }

    [Fact]
    public void Ein_gespeicherter_Wert_ohne_Schnellwahl_zeigt_frei()
    {
        GebaeudeKatalogDaten d = Radiator();
        d.ReglerProportionalband = 1.5;
        var cut = Aufbauen(d);

        Assert.True(Band(cut, "frei").HasAttribute("checked"));
        Assert.Equal("1,5", Eingabe(cut, "Proportionalband (frei) :").GetAttribute("value"));
    }

    // =================================================================================
    // Das Sollwert-Zeitprogramm (9.2): ein Wochenraster, gespeichert als Text
    // =================================================================================

    [Fact]
    public void Das_Zeitprogramm_entsteht_aus_der_Bestandswoche_und_reist_als_Text()
    {
        GebaeudeKatalogDaten geschrieben = null!;
        var cut = Aufbauen(Radiator(), speichern: (d, _, _) => { geschrieben = d; return new(true, ""); });

        Assert.Contains("Ohne Zeitprogramm gelten die Sollwerte des Gebäudes", Gruppe(cut));
        cut.FindAll("div.gebk-waermeuebergabe button").First(b => b.TextContent.Trim() == "Als Zeitprogramm bearbeiten").Click();
        Assert.Contains("Das Zeitprogramm gilt je Wochenstunde", Gruppe(cut));
        Ok(cut);

        Assert.NotNull(geschrieben.Sollwertprofil);
        AnlagenkopplungSchema.Wochenprofil p = AnlagenkopplungSchema.WochenprofilLesen(geschrieben.Sollwertprofil);
        Assert.Equal(AnlagenkopplungSchema.WochenprofilBefund.Gelesen, p.Befund);
        Assert.Equal(168, p.Werte.Length);
        // Montag 07 Uhr: Tag 20 °C; Montag 23 Uhr: Nacht 17 °C (die Bestandswoche des Satzes).
        Assert.Equal(20.0, p.Werte[7]);
        Assert.Equal(17.0, p.Werte[23]);
    }

    [Fact]
    public void Ein_gespeichertes_Zeitprogramm_steht_im_Raster_und_Verwerfen_schreibt_NULL()
    {
        GebaeudeKatalogDaten d = Radiator();
        d.Sollwertprofil = AnlagenkopplungSchema.WochenprofilSchreiben(Enumerable.Repeat(19.5, 168).ToArray());
        GebaeudeKatalogDaten geschrieben = null!;
        var cut = Aufbauen(d, speichern: (x, _, _) => { geschrieben = x; return new(true, ""); });

        Assert.Equal("19,5", cut.Find("div.gebk-waermeuebergabe label.epos-feld[title='Mo 00 Uhr'] input").GetAttribute("value"));
        cut.FindAll("div.gebk-waermeuebergabe button").First(b => b.TextContent.Trim() == "Zeitprogramm verwerfen").Click();
        Ok(cut);

        Assert.Null(geschrieben.Sollwertprofil);
        Assert.Equal(CultureInfo.GetCultureInfo("de-DE").Name, CultureInfo.CurrentCulture.Name);
    }
}
