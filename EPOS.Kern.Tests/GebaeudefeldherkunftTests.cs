using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests;

/// <summary>
/// Die Auskunft „wirksame Herkunft je Gebäudefeld“ (Entwurf GD 2.1/4.1, Welle GD-1). Sie RUFT die Kaskade des Laufs
/// (<see cref="Zonenvorgaben"/>, <see cref="Gebaeudemodellvorgaben.WirksamerLuftwechsel(double?, double?, double?, out Luftwechselherkunft)"/>)
/// und rechnet nicht nach: Jede Zählung wird hier gegen den Ruf derselben Funktionen gehalten.
/// </summary>
public class GebaeudefeldherkunftTests
{
    private static Gebaeudevorgaben Gebaeude(double luftwechselrate = 0.5, double? infiltration = null, double? nutzer = null)
        => new(200, 2.5, 20, 16, 18, 15, 26, null, null, luftwechselrate, infiltration, nutzer, 1000, 5,
               false, null, null, null);

    private static Bauteilmerkmal Wand(bool aufbau) => new(DbWerte.BAUTEILART_AUSSENWAND, null, null, aufbau, false);
    private static Bauteilmerkmal Innenwand(bool aufbau) => new(DbWerte.BAUTEILART_INNENWAND, null, null, aufbau, false);
    private static Bauteilmerkmal Fenster(bool g) => new(DbWerte.BAUTEILART_FENSTER, null, null, false, g);

    private static Zonenmerkmale Zone(Zoneneingaben e, params Bauteilmerkmal[] b) => new(e, b);

    [Fact]
    public void Die_Einordnungsfelder_sind_in_jedem_Weg_wirkungslos()
    {
        foreach (bool vdi in new[] { true, false })
        {
            var a = Gebaeudefeldherkunft.Bilden(Gebaeude(), vdi, new List<Zonenmerkmale>(), 35);
            foreach (Gebaeudefeld f in Gebaeudefeldherkunft.Einordnung)
            {
                Assert.Equal(Feldherkunft.Wirkungslos, a[f].Herkunft);
                Assert.True(a[f].IstBeschreibend);
            }
            foreach (Gebaeudefeld f in Gebaeudefeldherkunft.Rechengroessen)
                Assert.False(a[f].IstBeschreibend);
        }
    }

    [Fact]
    public void Einzone_Klassenweg_alle_Rechengroessen_von_Hand()
    {
        var a = Gebaeudefeldherkunft.Bilden(Gebaeude(), true, null, 35);
        foreach (Gebaeudefeld f in Gebaeudefeldherkunft.Rechengroessen)
        {
            Assert.Equal(Feldherkunft.Hand, a[f].Herkunft);
            Assert.Equal(Feldanzeige.Feld, a[f].Anzeige);
        }
        Assert.Equal(0, a[Gebaeudefeld.Raumhoehe].Zonenzahl);
    }

    [Theory]
    [InlineData(0.5, null, null, Feldherkunft.Hand)]
    [InlineData(0.0, null, null, Feldherkunft.Vorgabe)]
    [InlineData(0.5, 0.2, null, Feldherkunft.Wirkungslos)]
    public void Einzone_Luftwechselrate_folgt_WirksamerLuftwechsel(double n, double? inf, double? nutz, Feldherkunft erwartet)
    {
        var a = Gebaeudefeldherkunft.Bilden(Gebaeude(n, inf, nutz), true, null, 35);
        Assert.Equal(erwartet, a[Gebaeudefeld.Luftwechselrate].Herkunft);
    }

    [Fact]
    public void Leere_Flaeche_je_Nutzer_ist_Vorgabe_und_Datei_ersetzt_Hand()
    {
        var a = Gebaeudefeldherkunft.Bilden(Gebaeude(), true, null, 0,
                                            new[] { Gebaeudefeld.Nutzflaeche, Gebaeudefeld.Gebaeudeart });
        Assert.Equal(Feldherkunft.Vorgabe, a[Gebaeudefeld.FlaecheJeNutzer].Herkunft);
        Assert.Equal(Feldherkunft.Datei, a[Gebaeudefeld.Nutzflaeche].Herkunft);
        Assert.Equal(Feldherkunft.Wirkungslos, a[Gebaeudefeld.Gebaeudeart].Herkunft);
    }

    [Fact]
    public void Im_Altweg_wirken_alle_Rechengroessen_auch_mit_Zonen_und_Lueftungswerten()
    {
        var zonen = new[] { Zone(new Zoneneingaben(Raumhoehe: 3, Volumen: 300, LuftwechselInfiltration: 0.2), Wand(true)) };
        var a = Gebaeudefeldherkunft.Bilden(Gebaeude(0.5, 0.2, 0.3), false, zonen, 35);
        foreach (Gebaeudefeld f in Gebaeudefeldherkunft.Rechengroessen)
            Assert.Equal(Feldherkunft.Hand, a[f].Herkunft);
    }

    /// <summary>Jede Zone mit eigener Höhe bzw. eigenem Volumen: Die Raumhöhe des Gebäudes ist ausgeblendet.</summary>
    [Fact]
    public void Zonen_mit_eigener_Hoehe_blenden_die_Raumhoehe_aus()
    {
        var zonen = new[]
        {
            Zone(new Zoneneingaben(Nutzflaeche: 100, Raumhoehe: 3.0), Wand(true), Innenwand(true)),
            Zone(new Zoneneingaben(Nutzflaeche: 100, Volumen: 280), Wand(true), Innenwand(true)),
        };
        var a = Gebaeudefeldherkunft.Bilden(Gebaeude(), true, zonen, 35);
        Assert.Equal(Feldherkunft.Wirkungslos, a[Gebaeudefeld.Raumhoehe].Herkunft);
        Assert.Equal(Feldanzeige.JeZone, a[Gebaeudefeld.Raumhoehe].Anzeige);
        Assert.Equal(2, a[Gebaeudefeld.Raumhoehe].Zonenzahl);
    }

    /// <summary>
    /// Die Zählung „Vorgabe für n Zonen“ ist der Ruf von <see cref="Zonenvorgaben.Bilden(Zoneneingaben, Gebaeudevorgaben, int)"/>:
    /// eine Zone erbt die Höhe genau dann, wenn ihr Volumen abgeleitet und ihre Höhe die des Gebäudes ist.
    /// </summary>
    [Fact]
    public void Erbende_Zonen_zaehlen_wie_die_Kaskade_des_Laufs()
    {
        Gebaeudevorgaben g = Gebaeude();
        var eingaben = new[]
        {
            new Zoneneingaben(Nutzflaeche: 50),
            new Zoneneingaben(Nutzflaeche: 50, Raumhoehe: 3),
            new Zoneneingaben(Nutzflaeche: 50, Volumen: 120),
            new Zoneneingaben(Nutzflaeche: 50, LuftwechselNutzer: 0.4),
        };
        var zonen = eingaben.Select(e => Zone(e, Wand(true), Innenwand(true))).ToArray();
        var a = Gebaeudefeldherkunft.Bilden(g, true, zonen, 35);

        int hoehe = eingaben.Count(e =>
        {
            Zonenvorgaben v = Zonenvorgaben.Bilden(e, g, eingaben.Length);
            return v.Volumen.Herkunft == Vorgabeherkunft.Abgeleitet && v.Raumhoehe.Herkunft == Vorgabeherkunft.Gebaeude;
        });
        int luft = eingaben.Count(e =>
        {
            Zonenvorgaben v = Zonenvorgaben.Bilden(e, g, eingaben.Length);
            Gebaeudemodellvorgaben.WirksamerLuftwechsel(g.Luftwechselrate, v.LuftwechselInfiltration.Wert, v.LuftwechselNutzer.Wert,
                                                        out Luftwechselherkunft h);
            return h == Luftwechselherkunft.Luftwechselrate;
        });

        Assert.Equal(2, hoehe);
        Assert.Equal(3, luft);
        Assert.Equal(Feldherkunft.Geerbt, a[Gebaeudefeld.Raumhoehe].Herkunft);
        Assert.Equal(Feldanzeige.Feld, a[Gebaeudefeld.Raumhoehe].Anzeige);
        Assert.Equal(hoehe, a[Gebaeudefeld.Raumhoehe].Anzahl);
        Assert.Equal(Feldherkunft.Geerbt, a[Gebaeudefeld.Luftwechselrate].Herkunft);
        Assert.Equal(luft, a[Gebaeudefeld.Luftwechselrate].Anzahl);
    }

    /// <summary>Lüftungswerte am Gebäude erben alle Zonen — dann wirkt die Luftwechselrate nirgends und steht nicht da.</summary>
    [Fact]
    public void Belueftete_Zonen_blenden_die_Luftwechselrate_aus()
    {
        var zonen = new[] { Zone(new Zoneneingaben(Nutzflaeche: 100)), Zone(new Zoneneingaben(Nutzflaeche: 100)) };
        var a = Gebaeudefeldherkunft.Bilden(Gebaeude(0.5, 0.15, null), true, zonen, 35);
        Assert.Equal(Feldherkunft.Wirkungslos, a[Gebaeudefeld.Luftwechselrate].Herkunft);
        Assert.Equal(Feldanzeige.JeZone, a[Gebaeudefeld.Luftwechselrate].Anzeige);

        var b = Gebaeudefeldherkunft.Bilden(Gebaeude(0.0), true, zonen, 35);
        Assert.Equal(Feldherkunft.Vorgabe, b[Gebaeudefeld.Luftwechselrate].Herkunft);
        Assert.Equal(2, b[Gebaeudefeld.Luftwechselrate].Anzahl);
    }

    [Fact]
    public void Fensterdurchlassgrad_und_Bauweise_stehen_als_Rueckfall()
    {
        var mitRueckfall = new[]
        {
            Zone(new Zoneneingaben(Nutzflaeche: 100), Wand(false), Fenster(false), Fenster(true)),
            Zone(new Zoneneingaben(Nutzflaeche: 100), Wand(true), Innenwand(true), Fenster(false)),
        };
        var a = Gebaeudefeldherkunft.Bilden(Gebaeude(), true, mitRueckfall, 35);
        Assert.Equal(Feldanzeige.Rueckfall, a[Gebaeudefeld.Fensterdurchlassgrad].Anzeige);
        Assert.Equal(Feldherkunft.Geerbt, a[Gebaeudefeld.Fensterdurchlassgrad].Herkunft);
        Assert.Equal(2, a[Gebaeudefeld.Fensterdurchlassgrad].Anzahl);
        // Zone 1: Außengruppe ohne Aufbau und Innengruppe leer; Zone 2: beide mit Aufbau.
        Assert.Equal(Feldherkunft.Geerbt, a[Gebaeudefeld.Bauweise].Herkunft);
        Assert.Equal(2, a[Gebaeudefeld.Bauweise].Anzahl);

        var ohne = new[] { Zone(new Zoneneingaben(Nutzflaeche: 200), Wand(true), Innenwand(true), Fenster(true)) };
        var b = Gebaeudefeldherkunft.Bilden(Gebaeude(), true, ohne, 35);
        Assert.Equal(Feldherkunft.Wirkungslos, b[Gebaeudefeld.Fensterdurchlassgrad].Herkunft);
        Assert.Equal(Feldanzeige.Rueckfall, b[Gebaeudefeld.Fensterdurchlassgrad].Anzeige);
        Assert.Equal(Feldherkunft.Wirkungslos, b[Gebaeudefeld.Bauweise].Herkunft);
        Assert.Equal(0, b[Gebaeudefeld.Bauweise].Anzahl);
    }

    [Fact]
    public void Eine_Trennflaeche_zaehlt_innen_ausser_sie_ist_ausdruecklich_aussen()
    {
        var trenn = new Bauteilmerkmal(DbWerte.BAUTEILART_AUSSENWAND, DbWerte.RANDBEDINGUNG_ZONE, DbWerte.TRENNFLAECHE_IW, true, false);
        var zonen = new[] { Zone(new Zoneneingaben(Nutzflaeche: 200), Wand(true), trenn) };
        Assert.Equal(0, Gebaeudefeldherkunft.Bilden(Gebaeude(), true, zonen, 35)[Gebaeudefeld.Bauweise].Anzahl);

        var aussen = trenn with { TrennflaecheZuordnung = DbWerte.TRENNFLAECHE_AW };
        zonen = new[] { Zone(new Zoneneingaben(Nutzflaeche: 200), Wand(true), aussen) };
        Assert.Equal(1, Gebaeudefeldherkunft.Bilden(Gebaeude(), true, zonen, 35)[Gebaeudefeld.Bauweise].Anzahl);
    }
}
