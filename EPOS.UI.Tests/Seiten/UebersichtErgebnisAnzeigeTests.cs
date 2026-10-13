using EPOS.UI.Seiten.Berichte;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Die reinen Funktionen der Anzeigeeinstellung „Ergebnisse — Energie“ (<see cref="UebersichtErgebnisAnzeige"/>):
/// Vorgabe ohne Benutzerzustand, Benutzerzustand vor Vorgabe, unbekannte Schlüssel, Kennzahl aus, „ohne Wert“ und die
/// Textform der Einstellungsschlüssel.
/// </summary>
public class UebersichtErgebnisAnzeigeTests
{
    private static ErgebnisVergleichZeile Zeile(string schluessel, bool ohneWert = false)
        => new() { Schluessel = schluessel, Bezeichnung = schluessel, OhneWert = ohneWert };

    private static ErgebnisVergleichGruppe Gruppe(string schluessel, ErgebnisGruppenZustand vorgabe, params ErgebnisVergleichZeile[] zeilen)
        => new() { Schluessel = schluessel, Titel = schluessel, Zustand = vorgabe, Zeilen = zeilen };

    private static IReadOnlyList<ErgebnisVergleichGruppe> Probe() => new[]
    {
        Gruppe("bedarf", ErgebnisGruppenZustand.Offen, Zeile("energie.waermebedarf"), Zeile("energie.waermerest", ohneWert: true)),
        Gruppe("endenergie", ErgebnisGruppenZustand.Offen, Zeile("endenergie:1"), Zeile("endenergie:7", ohneWert: true)),
        Gruppe("kaelte", ErgebnisGruppenZustand.Zugeklappt, Zeile("kaelte.strom"), Zeile("kaelte.leer", ohneWert: true))
    };

    private static readonly Dictionary<string, ErgebnisGruppenZustand> s_vorgaben = new()
    {
        ["bedarf"] = ErgebnisGruppenZustand.Offen,
        ["endenergie"] = ErgebnisGruppenZustand.Offen,
        ["kaelte"] = ErgebnisGruppenZustand.Zugeklappt
    };

    [Fact]
    public void Ohne_Benutzerzustand_gilt_die_Vorgabe_und_ohne_Wert_ist_aus()
    {
        ErgebnisAnzeigeAuswertung a = UebersichtErgebnisAnzeige.Anwenden(Probe(), null);

        Assert.Equal(new[] { "bedarf", "endenergie", "kaelte" }, a.Gruppen.Select(g => g.Schluessel));
        Assert.Equal(ErgebnisGruppenZustand.Zugeklappt, a.Gruppen[2].Zustand);
        Assert.Equal(ErgebnisGruppenZustand.Offen, a.Gruppen[0].Zustand);
        // Vorgabe: Zeilen ohne Wert ausgeblendet, je Gruppe eine.
        Assert.Equal(3, a.OhneWertAusgeblendet);
        Assert.DoesNotContain(a.Gruppen.SelectMany(g => g.Zeilen), z => z.OhneWert);
        Assert.True(ErgebnisAnzeigeEinstellung.Standard().IstStandard);
    }

    [Fact]
    public void Benutzerzustand_ueberschreibt_die_Vorgabe()
    {
        var e = new ErgebnisAnzeigeEinstellung { OhneWertAus = false };
        e.Gruppen["kaelte"] = ErgebnisGruppenZustand.Offen;
        e.Gruppen["bedarf"] = ErgebnisGruppenZustand.Ausgeblendet;

        ErgebnisAnzeigeAuswertung a = UebersichtErgebnisAnzeige.Anwenden(Probe(), e);

        Assert.Equal(ErgebnisGruppenZustand.Ausgeblendet, a.Gruppen.Single(g => g.Schluessel == "bedarf").Zustand);
        Assert.Equal(ErgebnisGruppenZustand.Offen, a.Gruppen.Single(g => g.Schluessel == "kaelte").Zustand);
        Assert.Equal(ErgebnisGruppenZustand.Offen, a.Gruppen.Single(g => g.Schluessel == "endenergie").Zustand);
        Assert.Equal(0, a.OhneWertAusgeblendet);
        Assert.Equal(2, a.Gruppen.Single(g => g.Schluessel == "kaelte").Zeilen.Count);
        // Die Eingabe bleibt unverändert.
        Assert.Equal(ErgebnisGruppenZustand.Offen, Probe()[0].Zustand);
        Assert.Equal(ErgebnisGruppenZustand.Zugeklappt,
                     UebersichtErgebnisAnzeige.Grundzustand("neu", ErgebnisGruppenZustand.Zugeklappt, e));
    }

    [Fact]
    public void Unbekannte_Schluessel_werden_still_uebergangen()
    {
        ErgebnisAnzeigeEinstellung e = UebersichtErgebnisAnzeige.AusText(
            "veraltet=ausgeblendet;kaelte=quer;bedarf=AUSGEBLENDET;endenergie=offen;kaputt;=offen",
            "gibt.es.nicht;energie.waermebedarf", 1, s_vorgaben);

        // Nur bedarf bleibt: veraltet ist unbekannt, „quer“ kein Zustand, endenergie gleich der Vorgabe.
        Assert.Equal(new[] { "bedarf" }, e.Gruppen.Keys);
        Assert.Equal(ErgebnisGruppenZustand.Ausgeblendet, e.Gruppen["bedarf"]);

        ErgebnisAnzeigeAuswertung a = UebersichtErgebnisAnzeige.Anwenden(Probe(), e);
        // Die unbekannte Kennzahl wirkt nicht; die bekannte ist ab.
        Assert.Equal(new[] { "endenergie", "kaelte" }, a.Gruppen.Where(g => g.Zustand != ErgebnisGruppenZustand.Ausgeblendet)
                                                                 .Select(g => g.Schluessel));
        Assert.Equal(2, a.OhneWertAusgeblendet);   // die ausgeblendete Gruppe zählt nicht mit
    }

    [Fact]
    public void Kennzahl_aus_entfaellt_und_leere_Gruppe_mit()
    {
        var e = new ErgebnisAnzeigeEinstellung { OhneWertAus = true };
        e.KennzahlenAus.Add("endenergie:1");
        e.KennzahlenAus.Add("kaelte.strom");
        e.KennzahlenAus.Add("kaelte.leer");

        ErgebnisAnzeigeAuswertung a = UebersichtErgebnisAnzeige.Anwenden(Probe(), e);

        Assert.Equal(new[] { "bedarf" }, a.Gruppen.Select(g => g.Schluessel));
        // Abgewählt zählt nicht als „ohne Wert“: nur waermerest und endenergie:7.
        Assert.Equal(2, a.OhneWertAusgeblendet);
    }

    [Fact]
    public void Ohne_Wert_heisst_null_oder_leer_in_allen_Versionen()
    {
        Assert.True(UebersichtErgebnisAnzeige.OhneWert(new double?[] { null, 0.0, null }));
        Assert.True(UebersichtErgebnisAnzeige.OhneWert(Array.Empty<double?>()));
        Assert.False(UebersichtErgebnisAnzeige.OhneWert(new double?[] { 0.0, 0.001 }));
        Assert.False(UebersichtErgebnisAnzeige.OhneWert(new double?[] { null, -4.0 }));

        var e = new ErgebnisAnzeigeEinstellung { OhneWertAus = false };
        Assert.Equal(6, UebersichtErgebnisAnzeige.Anwenden(Probe(), e).Gruppen.Sum(g => g.Zeilen.Count));
    }

    [Fact]
    public void Textform_ist_geordnet_und_kehrt_zurueck()
    {
        var e = new ErgebnisAnzeigeEinstellung { OhneWertAus = false };
        e.Gruppen["kaelte"] = ErgebnisGruppenZustand.Ausgeblendet;
        e.Gruppen["bedarf"] = ErgebnisGruppenZustand.Zugeklappt;
        e.KennzahlenAus.Add("endenergie:7");
        e.KennzahlenAus.Add("energie.strommax");

        Assert.Equal("bedarf=zugeklappt;kaelte=ausgeblendet", UebersichtErgebnisAnzeige.GruppenText(e));
        Assert.Equal("endenergie:7;energie.strommax", UebersichtErgebnisAnzeige.KennzahlenText(e));

        ErgebnisAnzeigeEinstellung zurueck = UebersichtErgebnisAnzeige.AusText(
            UebersichtErgebnisAnzeige.GruppenText(e), UebersichtErgebnisAnzeige.KennzahlenText(e), 0, s_vorgaben);
        Assert.Equal(e.Gruppen, zurueck.Gruppen);
        Assert.Equal(e.KennzahlenAus, zurueck.KennzahlenAus);
        Assert.False(zurueck.OhneWertAus);
        Assert.False(zurueck.IstStandard);
    }

    [Fact]
    public void Ergebnis_des_Dialogs_haelt_nur_Abweichungen_von_der_Vorgabe()
    {
        var gruppen = new[]
        {
            new ErgebnisAnpassGruppe { Schluessel = "bedarf", Vorgabe = ErgebnisGruppenZustand.Offen },
            new ErgebnisAnpassGruppe { Schluessel = "kaelte", Vorgabe = ErgebnisGruppenZustand.Zugeklappt }
        };
        var zustaende = new Dictionary<string, ErgebnisGruppenZustand>
        {
            ["bedarf"] = ErgebnisGruppenZustand.Offen,
            ["kaelte"] = ErgebnisGruppenZustand.Ausgeblendet
        };

        ErgebnisAnzeigeEinstellung e = UebersichtErgebnisAnzeige.Ergebnis(gruppen, zustaende, new[] { "endenergie:9" }, true);

        Assert.Equal(new[] { "kaelte" }, e.Gruppen.Keys);
        Assert.Contains("endenergie:9", e.KennzahlenAus);   // auch eine heute nicht gezeigte Trägerzeile bleibt ab
        Assert.True(e.OhneWertAus);
    }
}
