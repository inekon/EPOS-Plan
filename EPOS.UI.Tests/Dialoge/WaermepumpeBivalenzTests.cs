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
}
