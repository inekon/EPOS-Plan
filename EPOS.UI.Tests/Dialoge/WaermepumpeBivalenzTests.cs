using Bunit;
using EPOS.UI.Dialoge.Waermepumpe;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Übergabegrenze UB‑E1 im Erzeugerdialog</b> (Fachkonzept 6.2, Umsetzungskonzept 5.1, 6.3; Mockup
/// <c>Waermepumpe_Bivalenz_Uebergabe.html</c>): die Herleitungszeile unter der Gruppe „Betrieb" — mit Kopplung im
/// Wortlaut des Zahlenbeispiels, ohne Kopplung der Hinweistext, ohne Einbindung „ruht" samt Werten — und die
/// Schnellwahl des Höchstvorlaufs in „Betriebszeiten", die nur ein leeres Feld füllt. Kultur de-DE über die
/// <see cref="Kulturvorrichtung"/> des <see cref="EposBunitContext"/>.
/// </summary>
public class WaermepumpeBivalenzTests : EposBunitContext
{
    /// <summary>Der Wortlaut des Mockups (Fachkonzept 6.2).</summary>
    private const string ZAHLENBEISPIEL =
        "Übergabe bei Höchstvorlauf 55 °C: 5,7 kW von 10,0 kW Heizlast (57 %), Rücklauf 46,5 °C, Spreizung 8,5 K · "
        + "erster Bivalenzpunkt +1,8 °C (nach Kennfeld allein −3,8 °C) · zweiter Bivalenzpunkt −3,6 °C (Vorwärmbetrieb) · "
        + "Wärmepumpe bei −7 °C 70 % der Kesselleistung (§ 43 GModG: mindestens 30 %).";

    public WaermepumpeBivalenzTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static WaermepumpeBivalenzWerte Beispiel(BivalenzKennzeichen k) => new()
    {
        Kennzeichen = k,
        HoechstvorlaufC = 55.0,
        UebergabeKw = 5.68,
        HeizlastKw = 10.0,
        Anteil = 0.568,
        RuecklaufC = 46.48,
        SpreizungK = 8.52,
        ErsterC = 1.83,
        KennfeldAlleinC = -3.84,
        ZweiterC = -3.56,
        Vorwaermbetrieb = true,
        KesselAnteil = 0.70,
        KesselMindestanteil = 0.30,
    };

    private static WaermepumpeAnlageDaten Daten(WaermepumpeBivalenzWerte? werte = null, double? vorlaufMax = null,
                                                bool mitListe = true) => new()
    {
        Bezeichner = "WP Alpha",
        Vorlauf = 50,
        Ruecklauf = 40,
        CarrierId = 60,
        VorlaufMax = vorlaufMax,
        Bivalenz = werte,
        Kaeltemittelliste = mitListe ? BivalenzAbbildung.Kaeltemittelliste() : Array.Empty<KaeltemittelEintrag>(),
    };

    private IRenderedComponent<WaermepumpeKonfiguration> Aufbauen(WaermepumpeAnlageDaten d)
        => Render<WaermepumpeKonfiguration>(p => p
            .Add(x => x.Daten, d)
            .Add(x => x.Traegerkatalog, Array.Empty<EPOS.UI.Bausteine.EnergietraegerWahl.Eintrag>())
            .Add(x => x.Aktiv, true));

    private static string Herleitung(IRenderedComponent<WaermepumpeKonfiguration> c)
        => c.Find("[data-gruppe=bivalenz-herleitung]").TextContent.Trim();

    [Fact]
    public void Mit_Kopplung_steht_das_Zahlenbeispiel_im_Wortlaut_des_Mockups()
    {
        var c = Aufbauen(Daten(Beispiel(BivalenzKennzeichen.Wirksam)));
        string zeile = Herleitung(c);
        Assert.Equal(ZAHLENBEISPIEL, zeile);
        Assert.Contains("5,7 kW von 10,0 kW", zeile);
        Assert.Contains("+1,8 °C", zeile);
        Assert.Contains("−3,6 °C", zeile);
    }

    [Fact]
    public void Ohne_Kopplung_steht_der_Hinweistext()
    {
        var t = new WaermepumpeKonfigurationTexte();
        var c = Aufbauen(Daten(new WaermepumpeBivalenzWerte { Kennzeichen = BivalenzKennzeichen.OhneKopplung }));
        Assert.Equal(t.HerleitungOhneKopplung, Herleitung(c));
        Assert.Contains("Kopplung aus", Herleitung(c));
        Assert.DoesNotContain("Bivalenzpunkt", Herleitung(c));
    }

    [Fact]
    public void Ohne_Einbindung_ruht_die_Grenze_und_die_Werte_stehen_trotzdem_da()
    {
        var c = Aufbauen(Daten(Beispiel(BivalenzKennzeichen.NichtWirksam)));
        string zeile = Herleitung(c);
        Assert.StartsWith("Einbindung nicht gesetzt — Übergabegrenze ruht. ", zeile);
        Assert.EndsWith(ZAHLENBEISPIEL, zeile);
    }

    [Fact]
    public void Ohne_Herleitung_zeichnet_die_Gruppe_keine_Zeile()
    {
        var c = Aufbauen(Daten(mitListe: false));
        Assert.Empty(c.FindAll("[data-gruppe=bivalenz-herleitung]"));
        Assert.Empty(c.FindAll("[data-gruppe=schnellwahl]"));
    }

    [Fact]
    public void Der_Rechenweg_der_Huelle_folgt_dem_Arbeitsstand()
    {
        var d = Daten();
        d.BivalenzRechnen = x => Beispiel(BivalenzKennzeichen.Wirksam) with { HoechstvorlaufC = x.VorlaufMax ?? x.Vorlauf ?? 0 };
        var c = Aufbauen(d);
        Assert.StartsWith("Übergabe bei Höchstvorlauf 50 °C", Herleitung(c));
        c.Find("[data-kaeltemittel='R290']").Click();
        Assert.StartsWith("Übergabe bei Höchstvorlauf 70 °C", Herleitung(c));
    }

    [Fact]
    public void Die_Schnellwahl_zeigt_vier_Knoepfe_und_die_Regel()
    {
        var t = new WaermepumpeKonfigurationTexte();
        var c = Aufbauen(Daten());
        var knoepfe = c.FindAll("[data-gruppe=schnellwahl] button");
        Assert.Equal(new[] { "R410A / R32 · 55 °C", "R290 · 70 °C", "R744 · 80 °C, Rücklauf ≤ 40 °C (Abwertung ab 30 °C)", "R1234ze(E) · 80 °C" },
                     knoepfe.Select(k => k.TextContent.Trim()).ToArray());
        Assert.Contains(t.ZeileSchnellwahl, c.Find("[data-gruppe=betriebszeiten]").TextContent);
        Assert.Contains(t.LabelKaeltemittel, c.Find("[data-gruppe=betriebszeiten]").TextContent);
    }

    [Fact]
    public void Die_Schnellwahl_fuellt_ein_leeres_Feld()
    {
        var d = Daten();
        var c = Aufbauen(d);
        c.Find("[data-kaeltemittel='R290']").Click();
        Assert.Equal(70.0, d.VorlaufMax);
        Assert.Equal("R290", d.Kaeltemittel);
        Assert.Equal("true", c.Find("[data-kaeltemittel='R290']").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void Die_Schnellwahl_laesst_ein_gefuelltes_Feld_stehen()
    {
        var d = Daten(vorlaufMax: 60.0);
        var c = Aufbauen(d);
        c.Find("[data-kaeltemittel='R744']").Click();
        Assert.Equal(60.0, d.VorlaufMax);
        Assert.Equal("R744", d.Kaeltemittel);
        Assert.Contains("Der höchste Vorlauf ist gepflegt (60 °C) — die Schnellwahl lässt ihn stehen.",
                        c.Find("[data-gruppe=betriebszeiten]").TextContent);
    }

    [Fact]
    public void Die_Klappliste_waehlt_wie_der_Knopf_und_kennt_elf_Codes()
    {
        var d = Daten();
        var c = Aufbauen(d);
        var wahl = c.Find("[data-gruppe=betriebszeiten] select");
        Assert.Equal(12, wahl.QuerySelectorAll("option").Length);   // elf Codes und „nicht gewählt"
        int r744 = d.Kaeltemittelliste.ToList().FindIndex(e => e.Code == "R744");
        wahl.Change(r744.ToString());
        Assert.Equal(80.0, d.VorlaufMax);
        // Eine Klasse ohne eigenen Höchstvorlauf (allgemeine Vorgabe) füllt nichts.
        var d2 = Daten();
        WaermepumpeKonfiguration.SchnellwahlAnwenden(d2, "R134a");
        Assert.Null(d2.VorlaufMax);
        Assert.Equal("R134a", d2.Kaeltemittel);
    }

    [Fact]
    public void Englisch_mit_Punkt_und_Vorzeichen()
    {
        using var _ = new Kulturvorrichtung("en-US");
        string zeile = WaermepumpeBivalenzText.Zeile(Beispiel(BivalenzKennzeichen.Wirksam), new WaermepumpeKonfigurationTexte());
        Assert.Contains("5.7 kW of 10.0 kW", zeile);
        Assert.Contains("+1.8 °C", zeile);
        Assert.Contains("−3.6 °C", zeile);
    }

    // =================================================================================================
    // UB‑E2: Gruppe „Bivalenz und Übergabe"
    // =================================================================================================

    private static WaermepumpeGrenzwerte R410a() => new(5.0, 10.0, 3.0, GrenzwertHerkunft.VorgabeKaeltemittel,
        0.60, GrenzwertHerkunft.VorgabeKaeltemittel, 52.0, GrenzwertHerkunft.Abgeleitet, 55.0);

    private static WaermepumpeGrenzwerte R744() => new(5.0, 30.0, 3.0, GrenzwertHerkunft.VorgabeKaeltemittel,
        0.60, GrenzwertHerkunft.VorgabeKaeltemittel, 40.0, GrenzwertHerkunft.VorgabeKaeltemittel, 80.0, 30.0, 2.5, 40.0);

    private static WaermepumpeAnlageDaten Bivalent(string betriebsart, WaermepumpeBivalenzWerte? werte = null)
    {
        WaermepumpeAnlageDaten d = Daten(werte);
        d.BivalenterBetrieb = true;
        d.Betriebsart = betriebsart;
        return d;
    }

    private static string Lesewerte(IRenderedComponent<WaermepumpeKonfiguration> c)
        => c.Find("[data-gruppe=bivalenz-lesewerte]").TextContent;

    [Fact]
    public void Die_Gruppe_traegt_den_neuen_Titel_und_zieht_die_Felder_um()
    {
        var t = new WaermepumpeKonfigurationTexte();
        var c = Aufbauen(Bivalent(DbWerte.WP_BETRIEBSART_TEILPARALLEL));
        var gruppen = c.FindAll(".epos-formulargruppe-titel").Select(e => e.TextContent.Trim()).ToList();
        Assert.Contains("Bivalenz und Übergabe", gruppen);
        Assert.DoesNotContain(t.GruppeBetrieb, gruppen);
        Assert.Single(c.FindAll("[data-gruppe=bivalenz-einbindung]"));
        Assert.Single(c.FindAll("[data-gruppe=bivalenz-vorwaermbetrieb]"));
    }

    [Theory]
    [InlineData(DbWerte.WP_BETRIEBSART_PARALLEL, true)]
    [InlineData(DbWerte.WP_BETRIEBSART_TEILPARALLEL, true)]
    [InlineData(DbWerte.WP_BETRIEBSART_ALTERNATIV, false)]
    public void Vorwaermbetrieb_je_Betriebsart(string art, bool waehlbar)
    {
        var t = new WaermepumpeKonfigurationTexte();
        var d = Bivalent(art);
        d.Vorwaermbetrieb = true;
        var c = Aufbauen(d);
        var gruppe = c.Find("[data-gruppe=bivalenz-vorwaermbetrieb]");
        var kaestchen = gruppe.QuerySelector("input")!;
        bool gesperrt = kaestchen.HasAttribute("disabled") || kaestchen.GetAttribute("aria-disabled") == "true";
        Assert.Equal(!waehlbar, gesperrt);
        Assert.Contains(waehlbar ? t.HinweisVorwaermbetrieb : t.HinweisVorwaermbetriebAlternativ, gruppe.TextContent);
        Assert.Equal(waehlbar, kaestchen.HasAttribute("checked"));
    }

    [Fact]
    public void Ohne_bivalenten_Betrieb_kein_Vorwaermbetrieb()
    {
        var c = Aufbauen(Daten());
        Assert.Empty(c.FindAll("[data-gruppe=bivalenz-vorwaermbetrieb]"));
        Assert.Single(c.FindAll("[data-gruppe=bivalenz-einbindung]"));
    }

    [Fact]
    public void Einbindung_leer_zeigt_nicht_gewaehlt_und_die_Wahl_schreibt_den_Steuerwert()
    {
        var t = new WaermepumpeKonfigurationTexte();
        var d = Daten(Beispiel(BivalenzKennzeichen.NichtWirksam));
        var c = Aufbauen(d);
        var wahl = c.Find("[data-gruppe=bivalenz-einbindung] select");
        Assert.Equal(4, wahl.QuerySelectorAll("option").Length);   // drei Werte und „nicht gewählt"
        Assert.Contains(t.EinbindungLeer, wahl.TextContent);
        Assert.StartsWith(t.HerleitungNichtWirksam, Herleitung(c));
        wahl.Change("1");
        Assert.Equal("PUFFER", d.Einbindung);
        Assert.Contains(t.HinweisEinbindungPuffer, c.Find("[data-gruppe=bivalenz-einbindung]").TextContent);
        c.Find("[data-gruppe=bivalenz-einbindung] select").Change("0");
        Assert.Equal("DIREKT", d.Einbindung);
        // Die Kopie trägt Einbindung und Vorwärmbetrieb mit.
        d.Vorwaermbetrieb = true;
        var k = d.Kopie();
        Assert.Equal("DIREKT", k.Einbindung);
        Assert.True(k.Vorwaermbetrieb);
    }

    [Fact]
    public void Herleitung_des_Abschaltpunkts_eingegeben_berechnet_massgebend()
    {
        var w = Beispiel(BivalenzKennzeichen.Wirksam) with { AbschaltpunktC = -10.0, ZweiterC = -3.5, MassgebendC = -3.5 };
        var c = Aufbauen(Bivalent(DbWerte.WP_BETRIEBSART_TEILPARALLEL, w));
        Assert.Equal("eingegeben −10 °C · aus der Übergabe berechnet −3,5 °C · maßgebend −3,5 °C",
                     c.Find("[data-gruppe=bivalenz-abschaltpunkt]").TextContent.Trim());

        var waermer = w with { AbschaltpunktC = 3.0, MassgebendC = 3.0 };
        Assert.Equal("eingegeben +3 °C · aus der Übergabe berechnet −3,5 °C · maßgebend +3 °C",
                     WaermepumpeBivalenzText.AbschaltpunktZeile(waermer, new WaermepumpeKonfigurationTexte()));
        // Im Parallelbetrieb gilt kein Abschaltpunkt — keine Zeile.
        var p = Aufbauen(Bivalent(DbWerte.WP_BETRIEBSART_PARALLEL, w));
        Assert.Empty(p.FindAll("[data-gruppe=bivalenz-abschaltpunkt]"));
    }

    [Fact]
    public void Lesewerte_R410A_mit_Herkunft_und_abgeleitetem_Ruecklauf()
    {
        var c = Aufbauen(Daten(Beispiel(BivalenzKennzeichen.Wirksam) with { Grenzen = R410a() }, vorlaufMax: 55.0));
        string l = Lesewerte(c);
        Assert.Contains("Höchster Vorlauf: 55 °C · Betriebszeiten", l);
        Assert.Contains("Spreizung Auslegung / max. / min.: 5 / 10 / 3 K · Vorgabe nach Kältemittel", l);
        Assert.Contains("Mindestvolumenstrom: 60 % · Vorgabe nach Kältemittel", l);
        Assert.Contains("Höchster Rücklauf: 52 °C · abgeleitet", l);
        Assert.Contains("abgeleitet: 52 °C = 55 − 3 (Höchstvorlauf − Mindestspreizung)", l);
        Assert.Contains("Rücklauf R744: Bezug / Abwertung / Grenze: — / — / — °C · %/K · °C · nur R744", l);
        Assert.Contains("Gerätegrenzen werden im Katalog gepflegt", l);
    }

    [Fact]
    public void Lesewerte_R744_mit_Bezug_Abwertung_und_Grenze()
    {
        var c = Aufbauen(Daten(Beispiel(BivalenzKennzeichen.Wirksam) with { Grenzen = R744() }, vorlaufMax: 80.0));
        string l = Lesewerte(c);
        Assert.Contains("Spreizung Auslegung / max. / min.: 5 / 30 / 3 K", l);
        Assert.Contains("Höchster Rücklauf: 40 °C · Vorgabe nach Kältemittel", l);
        Assert.Contains("Rücklauf R744: Bezug / Abwertung / Grenze: 30 / 2,5 / 40 °C · %/K · °C", l);
        Assert.Contains("bei R744: Bezugsrücklauf 30 °C · Abwertung 2,5 %/K · Grenze 40 °C", l);
        Assert.DoesNotContain("abgeleitet:", l);
    }

    [Fact]
    public void Jede_weiche_Sperre_steht_als_Banner_und_Speichern_bleibt_moeglich()
    {
        var befunde = new[]
        {
            new WaermepumpeBivalenzBefund(BivalenzBefundArt.Spreizung, false, 10, 8),
            new WaermepumpeBivalenzBefund(BivalenzBefundArt.Hoechstvorlauf, false, 30, 35),
            new WaermepumpeBivalenzBefund(BivalenzBefundArt.RuecklaufNie, false, 40, 45),
            new WaermepumpeBivalenzBefund(BivalenzBefundArt.VorwaermOhneKessel, false),
            new WaermepumpeBivalenzBefund(BivalenzBefundArt.Kaskade, false, 2, 1),
            new WaermepumpeBivalenzBefund(BivalenzBefundArt.Gmodg, true, 0.25, 0.30),
            new WaermepumpeBivalenzBefund(BivalenzBefundArt.UebergabeBegrenzt, true, 1.83),
        };
        var c = Aufbauen(Daten(Beispiel(BivalenzKennzeichen.Wirksam) with { Befunde = befunde }));
        var banner = c.FindAll("[data-gruppe=bivalenz-befunde] .epos-warnbanner");
        Assert.Equal(7, banner.Count);
        Assert.Equal(5, banner.Count(b => b.ClassList.Contains("epos-warnbanner--warnung")));
        string text = c.Find("[data-gruppe=bivalenz-befunde]").TextContent;
        Assert.Contains("Mindestspreizung (10 K) ist nicht kleiner als die Höchstspreizung (8 K)", text);
        Assert.Contains("höchste Vorlauf (30 °C) liegt unter dem Auslegungsvorlauf der Flächenheizung (35 °C)", text);
        Assert.Contains("Die Wärmepumpe liefert bei diesem Rücklauf nie.", text);
        Assert.Contains("ohne Kessel oder Heizstab in der Kaskade", text);
        Assert.Contains("hinter dem Kessel (Platz 2 nach Platz 1)", text);
        Assert.Contains("25 % der Kesselleistung; § 43 GModG verlangt mindestens 30 %", text);
        Assert.Contains("Unter +1,8 °C reicht der Höchstvorlauf nicht mehr", text);
    }

    [Fact]
    public void Die_Schnellwahl_nennt_das_Speichern_des_Kaeltemittels()
    {
        var t = new WaermepumpeKonfigurationTexte();
        Assert.Equal("Die Schnellwahl speichert das Kältemittel und füllt nur leere Felder; Herstellerangaben zur "
                     + "Einsatzgrenze haben Vorrang.", t.ZeileSchnellwahl);
        // Die Klappliste zeigt das gespeicherte Kältemittel des Geräts.
        var d = Daten(vorlaufMax: 60.0);
        d.Kaeltemittel = "R290";
        var c = Aufbauen(d);
        int r290 = d.Kaeltemittelliste.ToList().FindIndex(e => e.Code == "R290");
        Assert.Equal(r290.ToString(), c.Find("[data-gruppe=betriebszeiten] select").GetAttribute("value")
                                      ?? c.Find("[data-gruppe=betriebszeiten] select option[selected]").GetAttribute("value"));
    }
}
