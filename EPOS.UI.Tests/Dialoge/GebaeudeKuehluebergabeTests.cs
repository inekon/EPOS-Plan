using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Der Unterabschnitt „Kühlübergabe" der Gruppe „Kühlung"</b> im Gebäude-Katalogeditor (E37;
/// Anlagenkopplung 7.2, 8.1, 9.1; A1, A2, A4) — gespiegelt zu <see cref="GebaeudeWaermeuebergabeTests"/>.
///
/// <para><b>Was geprüft wird:</b> die SICHTBARKEITSREGEL (nur mit Kühlung; der Schalter, mit ihm die
/// Art, mit einer rechnenden Art die Felder), das NULL-VERHALTEN (leer zeigt die Vorgabe der Art als
/// Zahl und speichert NULL; die Art bleibt beim Abschalten), die PRÜFREGELN (Reihenfolge des
/// Auslegungspunkts, nur mit Haken und Kühlung), die HERLEITUNGSZEILEN mit der Zahl des Kerns
/// (Auslegungstag, Kaltwasser-Vorlauf, Wirksamkeit samt Grund) und die Grenze der Zahl (K5) — dazu
/// der Baustein ohne Gaben.</para>
/// </summary>
public class GebaeudeKuehluebergabeTests : EposBunitContext
{
    private const string KUEHLUNG = "Gebäude wird gekühlt";
    private const string HAKEN = "Kühlübergabe rechnen (statt idealer Kühlung)";
    private const string ART = "Kühlübergabeart :";

    public GebaeudeKuehluebergabeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Ein vollständig belegter Satz, der jede übrige Prüfung besteht (Soll am Tag 20 °C, gekühlt auf 26 °C).</summary>
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
        MaxTemperatur = 28,
        WochenendAbsenkung = 0,
        SollFerien = 0,
        Luftwechselrate = 0.5,
        Modell = DbWerte.GEBAEUDE_MODELL_VDI6007,
        KuehlungAktiv = true,
        KuehlSollwert = 26
    };

    /// <summary>Derselbe Satz mit eingeschalteter Kühlübergabe und Kühldecke, alle Felder leer.</summary>
    private static GebaeudeKatalogDaten Kuehldecke()
    {
        GebaeudeKatalogDaten d = Satz();
        d.KuehluebergabeAktiv = true;
        d.KuehlUebergabeArt = DbWerte.KUEHLUEBERGABE_KUEHLDECKE;
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
            .Add(x => x.Baualtersklassen, new[] { "vor 1919", "1919 bis 1948", "1949 bis 1957", "1958 bis 1968", "1969 bis 1978" })
            .Add(x => x.Katalognamen, () => new[] { "Haus A" })
            .Add(x => x.Lies, _ => Satz())
            .Add(x => x.Speichern, speichern ?? ((_, _, _) => new GebaeudeKatalogErgebnis(true, "")))
            .Add(x => x.UebergabeHerleitung, herleitung)
            .Add(x => x.EntprellungMs, entprellungMs));

    private static string Abschnitt(IRenderedComponent<GebaeudeKatalogDialog> cut)
        => cut.Find("div.gebk-kuehluebergabe").TextContent;

    private static IElement? FeldOderNull(IRenderedComponent<GebaeudeKatalogDialog> cut, string beschriftung)
        => cut.FindAll("div.gebk-kuehluebergabe label.epos-feld")
              .FirstOrDefault(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == beschriftung);

    private static IElement Eingabe(IRenderedComponent<GebaeudeKatalogDialog> cut, string beschriftung)
        => FeldOderNull(cut, beschriftung)!.QuerySelector("input")!;

    private static IElement Haken(IRenderedComponent<GebaeudeKatalogDialog> cut, string beschriftung)
        => cut.FindAll("label.epos-schalter")
              .First(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == beschriftung)
              .QuerySelector("input")!;

    private static void Ok(IRenderedComponent<GebaeudeKatalogDialog> cut)
        => cut.Find(".epos-leiste button.epos-knopf--primaer").Click();

    private static KuehluebergabeHerleitungDaten Kaelte(bool koppelt = true, bool kuehlt = true, string befund = "")
        => new("17. Juli", 22.04, 2.997, VorlaufAusAnlage: true, VorlaufQuelleC: 7.0, VorlaufC: 16.0, Gekappt: true,
               VorlaufgrenzeC: 16.0, ProjektKoppelt: koppelt, ProjektKuehlt: kuehlt, Befund: befund);

    // =================================================================================
    // Sichtbarkeitsregel (A1): nur mit Kühlung, Schalter, Art, Felder
    // =================================================================================

    [Fact]
    public void Ohne_Kuehlung_steht_kein_Unterabschnitt_da()
    {
        GebaeudeKatalogDaten d = Kuehldecke();
        d.KuehlungAktiv = false;
        var cut = Aufbauen(d);

        Assert.Empty(cut.FindAll("div.gebk-kuehluebergabe"));

        // Der Haken der Kühlung holt ihn - mit den Werten, die unsichtbar mitreisten.
        Haken(cut, KUEHLUNG).Change(true);
        Assert.Contains("Kühlübergabe", Abschnitt(cut));
        Assert.True(Haken(cut, HAKEN).HasAttribute("checked"));
        Assert.NotNull(FeldOderNull(cut, "Exponent der Kühlübergabe :"));
    }

    [Fact]
    public void Mit_Kuehlung_steht_der_Schalter_und_erst_mit_ihm_die_Art()
    {
        var cut = Aufbauen();

        Assert.False(Haken(cut, HAKEN).HasAttribute("checked"));
        Assert.Null(FeldOderNull(cut, ART));
        Assert.Contains("Ohne Haken rechnet die Kühlung ideal", Abschnitt(cut));
        Assert.Contains("Heizkreis (AK1)“ und der Projekteinstellung „Kühlung rechnen“", Abschnitt(cut));

        Haken(cut, HAKEN).Change(true);
        IElement liste = FeldOderNull(cut, ART)!.QuerySelector("select")!;
        Assert.Equal(new[] { "ideal (keine Kühlübergabe)", "Kühldecke", "Flächenkühlung", "Gebläsekonvektor" },
                     liste.QuerySelectorAll("option").Select(o => o.TextContent.Trim()));
        Assert.Null(FeldOderNull(cut, "Exponent der Kühlübergabe :"));
        Assert.Contains("Kühlübergabeart „ideal“", Abschnitt(cut));

        liste.Change("1");
        foreach (string feld in new[] { "Exponent der Kühlübergabe :", "Nennleistung der Kühlübergabe :",
                                        "Auslegung Kühlvorlauf :", "Auslegung Kühlrücklauf :",
                                        "Auslegung Raum (Kühlung) :", "Untere Vorlaufgrenze :" })
            Assert.NotNull(FeldOderNull(cut, feld));
        Assert.Contains("Vorgaben der Kühlübergabeart Kühldecke: Exponent 1,10, Auslegung 16/19 °C", Abschnitt(cut));
        Assert.Contains("Vorlaufgrenze 16 °C", Abschnitt(cut));
    }

    [Fact]
    public void Die_Grenze_der_Zahl_steht_an_jedem_gerechneten_Satz()
    {
        var cut = Aufbauen(Kuehldecke());

        Assert.Contains("Die Vorlaufgrenze ist eine Vorgabe, keine gerechnete Taupunktgrenze", Abschnitt(cut));
        Assert.Contains("Die Kühlübergabe rechnet sensibel, ohne Entfeuchtung", Abschnitt(cut));
    }

    // =================================================================================
    // NULL-Verhalten (A4): leer = Vorgabe als Zahl, gespeichert wird NULL
    // =================================================================================

    [Fact]
    public void Ein_leeres_Feld_zeigt_die_Vorgabe_der_Art_als_Zahl()
    {
        var cut = Aufbauen(Kuehldecke());

        Assert.Equal("Vorgabe 1,1", Eingabe(cut, "Exponent der Kühlübergabe :").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe 16", Eingabe(cut, "Auslegung Kühlvorlauf :").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe 19", Eingabe(cut, "Auslegung Kühlrücklauf :").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe 26", Eingabe(cut, "Auslegung Raum (Kühlung) :").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe 16", Eingabe(cut, "Untere Vorlaufgrenze :").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe: hergeleitet", Eingabe(cut, "Nennleistung der Kühlübergabe :").GetAttribute("placeholder"));
    }

    [Fact]
    public void Der_Geblaesekonvektor_hat_keine_Vorlaufgrenze()
    {
        GebaeudeKatalogDaten d = Kuehldecke();
        d.KuehlUebergabeArt = DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR;
        var cut = Aufbauen(d);

        Assert.Equal("Vorgabe 7", Eingabe(cut, "Auslegung Kühlvorlauf :").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe 12", Eingabe(cut, "Auslegung Kühlrücklauf :").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe: keine Grenze", Eingabe(cut, "Untere Vorlaufgrenze :").GetAttribute("placeholder"));
        Assert.Contains("Vorlaufgrenze keine", Abschnitt(cut));
    }

    [Fact]
    public void Leere_Felder_speichern_NULL_und_gesetzte_ihren_Wert()
    {
        GebaeudeKatalogDaten geschrieben = null!;
        var cut = Aufbauen(Kuehldecke(), speichern: (d, _, _) => { geschrieben = d; return new(true, ""); });

        Eingabe(cut, "Exponent der Kühlübergabe :").Input("1,05");
        Eingabe(cut, "Untere Vorlaufgrenze :").Input("17");
        Ok(cut);

        Assert.True(geschrieben.KuehluebergabeAktiv);
        Assert.Equal(DbWerte.KUEHLUEBERGABE_KUEHLDECKE, geschrieben.KuehlUebergabeArt);
        Assert.Equal(1.05, geschrieben.KuehlUebergabeExponent);
        Assert.Equal(17.0, geschrieben.KuehlVorlaufgrenze);
        Assert.Null(geschrieben.KuehlUebergabeLeistungNennKw);
        Assert.Null(geschrieben.KuehlAuslegungVorlauf);
        Assert.Null(geschrieben.KuehlAuslegungRuecklauf);
        Assert.Null(geschrieben.KuehlAuslegungRaumtemperatur);
    }

    [Fact]
    public void Ohne_Haken_bleiben_Art_und_Eingaben_stehen()
    {
        GebaeudeKatalogDaten d = Kuehldecke();
        d.KuehlAuslegungVorlauf = 17;
        GebaeudeKatalogDaten geschrieben = null!;
        var cut = Aufbauen(d, speichern: (x, _, _) => { geschrieben = x; return new(true, ""); });

        Haken(cut, HAKEN).Change(false);
        Assert.Null(FeldOderNull(cut, ART));
        Ok(cut);

        Assert.False(geschrieben.KuehluebergabeAktiv);
        Assert.Equal(DbWerte.KUEHLUEBERGABE_KUEHLDECKE, geschrieben.KuehlUebergabeArt);
        Assert.Equal(17.0, geschrieben.KuehlAuslegungVorlauf);
    }

    [Fact]
    public void Ideal_haelt_NULL()
    {
        GebaeudeKatalogDaten d = Satz();
        d.KuehluebergabeAktiv = true;
        GebaeudeKatalogDaten geschrieben = null!;
        var cut = Aufbauen(d, speichern: (x, _, _) => { geschrieben = x; return new(true, ""); });

        IElement liste = FeldOderNull(cut, ART)!.QuerySelector("select")!;
        liste.Change("2");
        FeldOderNull(cut, ART)!.QuerySelector("select")!.Change("0");
        Ok(cut);

        Assert.Null(geschrieben.KuehlUebergabeArt);
    }

    // =================================================================================
    // Prüfregeln (7.2, 9.1) — nur mit Kühlung und Haken
    // =================================================================================

    [Fact]
    public void Ein_Ruecklauf_ueber_der_Raumtemperatur_meldet_beim_OK()
    {
        bool geschrieben = false;
        var cut = Aufbauen(Kuehldecke(), speichern: (_, _, _) => { geschrieben = true; return new(true, ""); });

        Eingabe(cut, "Auslegung Kühlrücklauf :").Input("27");
        Ok(cut);

        Assert.False(geschrieben);
        Assert.Contains("Der Auslegungspunkt der Kühlübergabe muss aufsteigen: Vorlauf 16,0 °C unter Rücklauf 27,0 °C unter Raumtemperatur 26,0 °C.",
                        cut.Instance.Meldung);
    }

    [Fact]
    public void Ohne_Kuehlung_oder_ohne_Haken_gilt_keine_Regel()
    {
        foreach (bool kuehlung in new[] { false, true })
        {
            GebaeudeKatalogDaten d = Kuehldecke();
            d.KuehlungAktiv = kuehlung;
            d.KuehluebergabeAktiv = !kuehlung;          // mit Kühlung ohne Haken, ohne Kühlung mit Haken
            d.KuehlAuslegungRuecklauf = 30;             // widerspräche dem Raum
            bool geschrieben = false;
            var cut = Aufbauen(d, speichern: (_, _, _) => { geschrieben = true; return new(true, ""); });

            Ok(cut);

            Assert.True(geschrieben, "Kühlung " + kuehlung);
        }
    }

    [Fact]
    public void Eine_Vorlaufgrenze_ueber_dem_Auslegungsvorlauf_ist_ein_Hinweis_keine_Regel()
    {
        GebaeudeKatalogDaten d = Kuehldecke();
        d.KuehlVorlaufgrenze = 18;
        bool geschrieben = false;
        var cut = Aufbauen(d, speichern: (_, _, _) => { geschrieben = true; return new(true, ""); });

        Assert.Contains("Die Vorlaufgrenze 18,0 °C liegt über dem Auslegungsvorlauf 16,0 °C", Abschnitt(cut));
        Ok(cut);
        Assert.True(geschrieben);
    }

    // =================================================================================
    // Herleitungszeilen mit der Zahl des Kerns (A2, 7.2) - entprellt, sofort beim Öffnen
    // =================================================================================

    [Fact]
    public void Die_Herleitungszeilen_nennen_Auslegungstag_Vorlauf_und_Wirksamkeit()
    {
        var cut = Aufbauen(Kuehldecke(), herleitung: _ => new UebergabeHerleitungDaten(null, null, "", Kaelte()));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("die Kühllast des Auslegungstags 17. Juli (höchstes Tagesmittel der Außenluft, 22,0 °C) — 3,0 kW, sensibel", Abschnitt(cut));
            Assert.Contains("Kaltwasser-Vorlauf fest 16,0 °C: Die Wärmepumpe im Kühlbetrieb liefert 7,0 °C", Abschnitt(cut));
            Assert.Contains("In diesem Projekt wirksam", Abschnitt(cut));
            Assert.Equal("Vorgabe 2,997", Eingabe(cut, "Nennleistung der Kühlübergabe :").GetAttribute("placeholder"));
        });
    }

    [Fact]
    public void Die_Wirksamkeit_nennt_ihren_Grund()
    {
        var ohneStufe = Aufbauen(Kuehldecke(), herleitung: _ => new UebergabeHerleitungDaten(null, null, "", Kaelte(koppelt: false)));
        ohneStufe.WaitForAssertion(() => Assert.Contains("Es rechnet ohne Anlagenkopplung", Abschnitt(ohneStufe)));

        var ohneKaelte = Aufbauen(Kuehldecke(), herleitung: _ => new UebergabeHerleitungDaten(null, null, "", Kaelte(kuehlt: false)));
        ohneKaelte.WaitForAssertion(() => Assert.Contains("Es rechnet keine Kälte", Abschnitt(ohneKaelte)));

        GebaeudeKatalogDaten d = Kuehldecke();
        d.KuehlSollwert = null;
        var ohneSollwert = Aufbauen(d);
        Assert.Contains("Ohne Kühlsollwert wird das Gebäude nicht gekühlt", Abschnitt(ohneSollwert));
    }

    [Fact]
    public void Ein_Befund_des_Kerns_steht_statt_der_Zahl()
    {
        var cut = Aufbauen(Kuehldecke(),
                           herleitung: _ => new UebergabeHerleitungDaten(null, null, "", Kaelte(befund: "Die Kühllast ist null.")));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Keine hergeleitete Zahl: Die Kühllast ist null.", Abschnitt(cut));
            Assert.Equal("Vorgabe: hergeleitet", Eingabe(cut, "Nennleistung der Kühlübergabe :").GetAttribute("placeholder"));
        });
    }

    [Fact]
    public void Die_Herleitung_laeuft_beim_Oeffnen_sofort_und_danach_entprellt()
    {
        int aufrufe = 0;
        var cut = Aufbauen(Kuehldecke(), herleitung: _ => { aufrufe++; return new UebergabeHerleitungDaten(null, null, "", Kaelte()); },
                           entprellungMs: 60_000);

        cut.WaitForAssertion(() => Assert.Equal(1, aufrufe));
        Eingabe(cut, "Auslegung Kühlvorlauf :").Input("1");
        Eingabe(cut, "Auslegung Kühlvorlauf :").Input("15");
        Assert.Equal(1, aufrufe);
        Assert.Equal(1, cut.FindComponent<GebaeudeKuehluebergabeFelder>().Instance.Herleitungen);
    }

    // =================================================================================
    // Der Baustein ohne Gaben und der Arbeitsstand
    // =================================================================================

    [Fact]
    public void Der_Baustein_zeichnet_ohne_Gaben()
    {
        var cut = Render<GebaeudeKuehluebergabeFelder>();

        Assert.Empty(cut.FindAll("div.gebk-kuehluebergabe"));
    }

    [Fact]
    public void Der_Arbeitsstand_zaehlt_die_acht_Felder_als_Abweichung()
    {
        var a = new GebaeudeArbeitsstand();
        GebaeudeKatalogDaten geladen = Kuehldecke();
        a.Laden(geladen, neu: false);
        Assert.Equal(0, a.Abweichungen(geladen));

        a.KuehluebergabeSetzen(false);
        a.KuehlArtWaehlen(3);
        a.Stand.KuehlUebergabeExponent = 1.0;
        a.Stand.KuehlUebergabeLeistungNennKw = 5;
        a.Stand.KuehlAuslegungVorlauf = 8;
        a.Stand.KuehlAuslegungRuecklauf = 13;
        a.Stand.KuehlAuslegungRaumtemperatur = 25;
        a.Stand.KuehlVorlaufgrenze = 10;

        Assert.Equal(8, a.Abweichungen(geladen));
        Assert.Equal(DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR, a.Stand.KuehlUebergabeArt);
    }
}
