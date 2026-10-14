using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Abschnitt „Übergabe" des Zonendialogs (E63, AK1z): sichtbar nur bei beheizter Zone, „wie
/// Gebäude" als Vorgabe der Art, Platzhalter mit dem wirksamen Wert (Gebäude, sonst Vorgabe der Art),
/// die Eingaben im Arbeitsstand, der Hinweis „ohne Wirkung", die benannte Sperre, die Bänder der
/// Prüfung und die Felder beim Assistenten.
///
/// <para>Die Kultur ist auf de-DE gepinnt (<see cref="EposBunitContext"/>): Die Erwartungswerte sind
/// deutsche Beschriftungen und Zahlen.</para>
/// </summary>
public class ZonenUebergabeDialogTests : EposBunitContext
{
    public ZonenUebergabeDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Das Gebäude: Heizkreis an, Radiator, Vorlauf 60 °C ausdrücklich, Soll am Tag 20 °C.</summary>
    private static GebaeudeArbeitsstand Gebaeude(bool heizkreis = true, string? art = DbWerte.UEBERGABE_RADIATOR)
    {
        var d = new GebaeudeKatalogDaten
        {
            Name = "Haus", WohnflaecheGesamt = 200, SollTag = 20, NachtAbsenkung = 16,
            HeizkreisAktiv = heizkreis, UebergabeArt = art, AuslegungVorlauf = 60
        };
        var arbeit = new GebaeudeArbeitsstand();
        arbeit.Laden(d, neu: false);
        return arbeit;
    }

    private IRenderedComponent<ZonenDialog> Aufbauen(ZoneDaten? zone = null, GebaeudeArbeitsstand? arbeit = null,
                                                      bool? koppelt = true, string? sperre = null,
                                                      Action<ZoneDaten?>? geschlossen = null)
    {
        arbeit ??= Gebaeude();
        return Render<ZonenDialog>(p => p
            .Add(x => x.Zone, zone ?? new ZoneDaten { Id = 7, Bezeichner = "Wohnen", Nutzflaeche = 150 })
            .Add(x => x.NutzflaecheGebaeude, 200.0)
            .Add(x => x.Gebaeude, arbeit.Vorgaben)
            .Add(x => x.Gebaeudestand, arbeit)
            .Add(x => x.ProjektKoppelt, koppelt)
            .Add(x => x.UebergabeSperre, sperre)
            .Add(x => x.Geschlossen, z => geschlossen?.Invoke(z)));
    }

    private static IElement? FeldOderNichts(IElement bereich, string beschriftung)
        => bereich.QuerySelectorAll("label.epos-feld")
                  .FirstOrDefault(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == beschriftung)
                  ?.QuerySelector("input, select");

    private static IElement Feld(IElement bereich, string beschriftung) => FeldOderNichts(bereich, beschriftung)!;

    private static IElement Abschnitt(IRenderedComponent<ZonenDialog> cut) => cut.Find(".epos-zonendialog");

    private static void Ok(IRenderedComponent<ZonenDialog> cut)
        => cut.Find(".epos-zonendialog > .epos-leiste button.epos-knopf--primaer").Click();

    [Fact]
    public void Der_Abschnitt_steht_nur_bei_beheizter_Zone()
    {
        var cut = Aufbauen();
        Assert.Contains("Übergabe", cut.Markup);
        Assert.NotNull(FeldOderNichts(Abschnitt(cut), "Übergabeart"));
        Assert.NotNull(FeldOderNichts(Abschnitt(cut), "Exponent"));

        cut.Find(".epos-zonendialog input.epos-schalter-kasten").Change(false);

        Assert.Null(FeldOderNichts(Abschnitt(cut), "Übergabeart"));
        Assert.Null(FeldOderNichts(Abschnitt(cut), "Exponent"));
        Assert.Contains("Eine unbeheizte Zone hat keine Wärmeübergabe", cut.Markup);
    }

    [Fact]
    public void Wie_Gebaeude_ist_die_Vorgabe_der_Art_und_nennt_die_Art_des_Gebaeudes()
    {
        var cut = Aufbauen();
        IElement art = Feld(Abschnitt(cut), "Übergabeart");

        Assert.Equal("0", art.GetAttribute("value"));
        Assert.Equal("wie Gebäude (Radiator)", art.QuerySelector("option[value=\"0\"]")!.TextContent.Trim());
        Assert.Null(cut.Instance.Arbeitsstand.UebergabeArt);
        Assert.Contains("Wirksame Übergabeart: Radiator (wie Gebäude)", cut.Markup);
    }

    [Fact]
    public void Die_Platzhalter_zeigen_Gebaeudewert_bzw_Vorgabe_der_wirksamen_Art()
    {
        var cut = Aufbauen();
        IElement w = Abschnitt(cut);

        Assert.Equal("Vorgabe: 1,3", Feld(w, "Exponent").GetAttribute("placeholder"));
        Assert.Equal("wie Gebäude: 60", Feld(w, "Auslegung Vorlauf").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe: 45", Feld(w, "Auslegung Rücklauf").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe: 20", Feld(w, "Auslegung Raum").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe: 1", Feld(w, "Proportionalband des Raumreglers").GetAttribute("placeholder"));
        Assert.Equal("Anteil des Gebäudes nach Nutzfläche", Feld(w, "Nennleistung der Übergabe").GetAttribute("placeholder"));

        // Eine eigene Art der Zone: die Vorgaben der Flächenheizung, der Vorlauf des Gebäudes bleibt.
        Feld(w, "Übergabeart").Change("3");
        w = Abschnitt(cut);
        Assert.Equal(DbWerte.UEBERGABE_FLAECHE, cut.Instance.Arbeitsstand.UebergabeArt);
        Assert.Equal("Vorgabe: 1,1", Feld(w, "Exponent").GetAttribute("placeholder"));
        Assert.Equal("wie Gebäude: 60", Feld(w, "Auslegung Vorlauf").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe: 28", Feld(w, "Auslegung Rücklauf").GetAttribute("placeholder"));
        Assert.Contains("Wirksame Übergabeart: Flächenheizung (an der Zone gewählt)", cut.Markup);
    }

    [Fact]
    public void Eine_ideale_Art_blendet_die_Zahlenfelder_aus()
    {
        var cut = Aufbauen(arbeit: Gebaeude(art: null));

        Assert.NotNull(FeldOderNichts(Abschnitt(cut), "Übergabeart"));
        Assert.Null(FeldOderNichts(Abschnitt(cut), "Exponent"));
        Assert.Contains("Wirksame Übergabeart: ideal (wie Gebäude)", cut.Markup);

        Feld(Abschnitt(cut), "Übergabeart").Change("2");
        Assert.NotNull(FeldOderNichts(Abschnitt(cut), "Exponent"));
    }

    [Fact]
    public void Die_Eingaben_landen_im_Arbeitsstand_und_OK_gibt_sie_zurueck()
    {
        ZoneDaten? zurueck = null;
        var cut = Aufbauen(geschlossen: z => zurueck = z);
        IElement w = Abschnitt(cut);

        Feld(w, "Übergabeart").Change("4");
        w = Abschnitt(cut);
        Feld(w, "Exponent").Input("1,35");
        Feld(w, "Nennleistung der Übergabe").Input("4,5");
        Feld(w, "Auslegung Vorlauf").Input("50");
        Feld(w, "Auslegung Rücklauf").Input("40");
        Feld(w, "Auslegung Raum").Input("21");
        Feld(w, "Proportionalband des Raumreglers").Input("2");
        Ok(cut);

        Assert.NotNull(zurueck);
        Assert.Equal(DbWerte.UEBERGABE_KONVEKTOR, zurueck!.UebergabeArt);
        Assert.Equal(1.35, zurueck.UebergabeExponent);
        Assert.Equal(4.5, zurueck.UebergabeLeistungNennKw);
        Assert.Equal(50.0, zurueck.AuslegungVorlauf);
        Assert.Equal(40.0, zurueck.AuslegungRuecklauf);
        Assert.Equal(21.0, zurueck.AuslegungRaumtemperatur);
        Assert.Equal(2.0, zurueck.ReglerProportionalband);
        Assert.Equal(zurueck.UebergabeArt, zurueck.Eingaben().UebergabeArt);
        Assert.Equal(50.0, zurueck.Eingaben().AuslegungVorlaufC);
    }

    [Fact]
    public void Wie_Gebaeude_waehlen_setzt_die_Art_auf_null()
    {
        ZoneDaten? zurueck = null;
        var cut = Aufbauen(zone: new ZoneDaten { Id = 7, Bezeichner = "Wohnen", UebergabeArt = DbWerte.UEBERGABE_FLAECHE },
                           geschlossen: z => zurueck = z);
        Assert.Equal("3", Feld(Abschnitt(cut), "Übergabeart").GetAttribute("value"));

        Feld(Abschnitt(cut), "Übergabeart").Change("0");
        Ok(cut);

        Assert.Null(zurueck!.UebergabeArt);
    }

    [Fact]
    public void Bei_Heizkreis_aus_nennt_der_Abschnitt_ohne_Wirkung_und_bleibt_bearbeitbar()
    {
        var cut = Aufbauen(arbeit: Gebaeude(heizkreis: false));

        Assert.Contains("Ohne Wirkung, solange der Heizkreis des Gebäudes aus ist", cut.Markup);
        Feld(Abschnitt(cut), "Exponent").Input("1,2");
        Assert.Equal(1.2, cut.Instance.Arbeitsstand.UebergabeExponent);
    }

    [Fact]
    public void Ohne_Kopplungsstufe_im_Projekt_nennt_der_Abschnitt_ohne_Wirkung()
    {
        var cut = Aufbauen(koppelt: false);
        Assert.Contains("Ohne Wirkung, solange das Projekt keine Anlagenkopplung (AK1) rechnet", cut.Markup);

        var mit = Aufbauen(koppelt: true);
        Assert.DoesNotContain("Ohne Wirkung", mit.Markup);
    }

    [Fact]
    public void Ohne_Schemaschritt_steht_allein_die_benannte_Sperre()
    {
        var cut = Aufbauen(sperre: "Die Datenbank kennt die Übergabe je Zone nicht.");

        Assert.Contains("Die Datenbank kennt die Übergabe je Zone nicht.", cut.Markup);
        Assert.Null(FeldOderNichts(Abschnitt(cut), "Übergabeart"));
    }

    /// <summary>
    /// Ein Wert außerhalb des Bandes, der schon in der Zone steht (etwa aus der Datenbank), hält das OK mit
    /// der Meldung des Kerns an — dieselben Bänder wie <c>GebaeudeZonenCtrl.UebergabePruefen</c>.
    /// </summary>
    [Theory]
    [InlineData(95.0, null, null, "Zone „Wohnen“: Der Auslegungsvorlauf 95 °C liegt außerhalb von 25 bis 90 °C.")]
    [InlineData(null, 30.0, null, "Zone „Wohnen“: Die Auslegungsraumtemperatur 30 °C liegt außerhalb von 15 bis 26 °C.")]
    [InlineData(null, null, 6.0, "Zone „Wohnen“: Das Proportionalband 6 K liegt außerhalb von 0 bis 5 K.")]
    public void Werte_ausserhalb_der_Baender_halten_das_OK_an(double? vorlauf, double? raum, double? band, string meldung)
    {
        bool geschlossen = false;
        var zone = new ZoneDaten
        {
            Id = 7, Bezeichner = "Wohnen", AuslegungVorlauf = vorlauf, AuslegungRaumtemperatur = raum,
            ReglerProportionalband = band
        };
        var cut = Aufbauen(zone: zone, geschlossen: _ => geschlossen = true);

        Ok(cut);

        Assert.False(geschlossen);
        Assert.Equal(meldung, cut.Instance.Meldung);
    }

    [Fact]
    public void Eine_Eingabe_ausserhalb_des_Bandes_ist_ein_Fehlerfeld()
    {
        bool geschlossen = false;
        var cut = Aufbauen(geschlossen: _ => geschlossen = true);

        Feld(Abschnitt(cut), "Auslegung Vorlauf").Input("95");
        Ok(cut);

        Assert.False(geschlossen);
        Assert.Contains("Auslegung Vorlauf", cut.Instance.Meldung);
        Assert.Null(cut.Instance.Arbeitsstand.AuslegungVorlauf);
    }

    [Fact]
    public void Ein_Ruecklauf_ueber_dem_Vorlauf_wird_benannt_abgelehnt()
    {
        bool geschlossen = false;
        var cut = Aufbauen(geschlossen: _ => geschlossen = true);
        IElement w = Abschnitt(cut);

        Feld(w, "Auslegung Vorlauf").Input("50");
        Feld(w, "Auslegung Rücklauf").Input("55");
        Ok(cut);

        Assert.False(geschlossen);
        Assert.Equal("Zone „Wohnen“: Der Auslegungsrücklauf 55 °C muss unter dem Auslegungsvorlauf 50 °C liegen.",
                     cut.Instance.Meldung);
    }

    [Fact]
    public void Die_ausgeblendeten_Fehlerfelder_halten_das_OK_nicht_an()
    {
        bool geschlossen = false;
        var cut = Aufbauen(geschlossen: _ => geschlossen = true);
        Feld(Abschnitt(cut), "Exponent").Input("9");
        Feld(Abschnitt(cut), "Übergabeart").Change("1");   // ideal: die Zahlenfelder fallen

        Ok(cut);

        Assert.True(geschlossen, cut.Instance.Meldung);
    }

    [Fact]
    public void Der_Assistent_liest_und_setzt_die_Uebergabe_der_Zone()
    {
        var cut = Aufbauen();

        KiFeldzugang art = KiMaskenbruecke.Feldzugang(KiMaskennamen.ZONE, "uebergabe_art");
        Assert.NotNull(art);
        Assert.Equal("", art.Lesen());
        art.Setzen("konvektor");
        KiFeldzugang vorlauf = KiMaskenbruecke.Feldzugang(KiMaskennamen.ZONE, "auslegung_vorlauf");
        Assert.NotNull(vorlauf);
        vorlauf.Setzen(48.0);
        cut.Render();
        Assert.Equal(DbWerte.UEBERGABE_KONVEKTOR, cut.Instance.Arbeitsstand.UebergabeArt);
        Assert.Equal(48.0, cut.Instance.Arbeitsstand.AuslegungVorlauf);

        KiFeldzugang wirksam = KiMaskenbruecke.Feldzugang(KiMaskennamen.ZONE, "uebergabe_wirksam");
        Assert.NotNull(wirksam);
        Assert.False(wirksam.Setzbar);
        string text = (string)wirksam.Lesen()!;
        Assert.Contains("Konvektor (an der Zone gewählt)", text);
        Assert.Contains("Auslegung Vorlauf: 48 (an der Zone gewählt)", text);
        Assert.Contains("Exponent: Vorgabe: 1,4", text);

        Assert.ThrowsAny<Exception>(() => art.Setzen("FUSSBODEN"));
        Assert.Equal(DbWerte.UEBERGABE_KONVEKTOR, cut.Instance.Arbeitsstand.UebergabeArt);
    }
}
