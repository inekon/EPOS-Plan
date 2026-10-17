using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using KiKern;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Hilfe;

/// <summary>
/// <b>Der Assistent wählt Modul und Wechselrichter eines Strangs</b> (Anwenderentscheid
/// 16.10.2026) — über den ÜBERNAHMEWEG des Strangbausteins, nie über einen direkten
/// Setzer der Id: dieselbe Klappliste, dieselbe Projektkopie, dieselbe Bewertung wie bei
/// der Wahl durch den Anwender. Eine ungeeignete Wahl lehnt er benannt ab.
/// </summary>
public class PvStrangKiWahlTests : EposBunitContext
{
    public PvStrangKiWahlTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private const string GRUND_STROM = "Strom eines Strangs 13,7 A über der Grenze je MPPT 12,0 A";
    private const string GRUND_ROT = "Strang „Dach Süd“: Leerlaufspannung über der Grenze";

    private static readonly (int Id, string Text)[] KATALOG = { (7, "Muster 2500TL"), (8, "Muster 5000TL-2M") };
    private static readonly (int Id, string Text)[] MODULE = { (3, "Modul 300"), (4, "Modul 400") };

    /// <summary>Die Bewertung der Hülle: Gerät 7 geeignet, Gerät 8 ungeeignet wegen Strom.</summary>
    private static IReadOnlyList<WechselrichterVorschlagZeile> Bewertung(ErzeugerZeile z, string hersteller) => new[]
    {
        new WechselrichterVorschlagZeile(7, "Muster 2500TL", "Muster", Wechselrichtereignung.Geeignet,
                                         "geeignet", "", "1,06", 1, "1 × (1 × 10)", "", ""),
        new WechselrichterVorschlagZeile(8, "Muster 5000TL-2M", "Muster", Wechselrichtereignung.Ungeeignet,
                                         "ungeeignet", GRUND_STROM, "", 0, "", "", "")
    };

    /// <summary>Die Ampel: Modul 300 (Projektkopie 2003) macht den Strang rot.</summary>
    private static StrangBefund Ampel(ErzeugerZeile z, IReadOnlyList<StrangZeile> straenge)
    {
        var zeilen = new List<Ampelzeile>();
        foreach (StrangZeile s in straenge)
            zeilen.Add(s.ModulId == 2003 ? new Ampelzeile(Ampelfarbe.Rot, GRUND_ROT)
                                         : new Ampelzeile(Ampelfarbe.Gruen, "Strang „" + s.Bezeichner + "“: in Ordnung"));
        return new StrangBefund(zeilen, Array.Empty<Ampelzeile>(), 10);
    }

    private (ErzeugerZeile Zeile, KiMaskenanmeldung Anmeldung) Aufbauen(bool mitBewertung = true)
    {
        var zeile = new ErzeugerZeile
        {
            Schluessel = 1, Bezeichner = "Anlage", Neigung = 30, Azimut = 0,
            AnzahlModule = 10, MitWechselrichter = true
        };
        zeile.Straenge.Add(new StrangZeile { Rang = 1, Bezeichner = "Dach Süd", ModuleReihe = 10 });

        var cut = Render<PvStraengeFelder>(p => p
            .Add(x => x.Zeile, zeile)
            .Add(x => x.NeigungAnlage, zeile.Neigung)
            .Add(x => x.AzimutAnlage, zeile.Azimut)
            .Add(x => x.KwpAnlage, 2.752)
            .Add(x => x.Geraete, KATALOG)
            .Add(x => x.GeraetUebernehmen, id => new GeraetWahl(1000 + id, Name(KATALOG, id)))
            .Add(x => x.Module, MODULE)
            .Add(x => x.ModulUebernehmen, id => new GeraetWahl(2000 + id, Name(MODULE, id)))
            .Add(x => x.Pruefen, Ampel)
            .Add(x => x.WechselrichterVorschlagen, mitBewertung ? Bewertung : null));

        PvStraengeFelder baustein = cut.Instance;
        var sicht = new PhotovoltaikKiSicht { Zeilenquelle = () => zeile, Strangwahl = () => baustein };
        return (zeile, KiMaskenanmeldung.Fuer(KiMaskennamen.PHOTOVOLTAIK, () => sicht, new KiMaskenhaken()));
    }

    private static string Name((int Id, string Text)[] liste, int id)
    {
        foreach (var e in liste) if (e.Id == id) return e.Text;
        return "";
    }

    private static KiFeldzugang Zugang(string spalte)
    {
        string name = KiMaskenbruecke.Lesen(KiMaskennamen.PHOTOVOLTAIK)
                                     .Select(w => w.Name)
                                     .First(n => n.StartsWith(spalte + "_", StringComparison.Ordinal));
        return KiMaskenbruecke.Feldzugang(KiMaskennamen.PHOTOVOLTAIK, name)!;
    }

    private static void Setze(KiFeldzugang zugang, string wert)
    {
        KiFeldumsetzung neu = KiFeldwandler.Wandle(zugang, wert);
        Assert.True(neu.Ok, neu.Grund);
        zugang.Setzen(neu.Wert);
    }

    [Fact]
    public void Ein_geeigneter_Wechselrichter_wird_ueber_den_Uebernahmeweg_gewaehlt()
    {
        var (zeile, anmeldung) = Aufbauen();
        using (anmeldung)
        {
            KiFeldzugang zugang = Zugang("strang_wechselrichter");
            Assert.True(zugang.IstWahl);
            Assert.Contains(zugang.Wahleintraege(), e => e.Text == "Muster 2500TL");

            Setze(zugang, "Muster 2500TL");

            // Die PROJEKTKOPIE des Übernahmewegs, nicht die Katalog-Id.
            Assert.Equal(1007, zeile.Straenge[0].WechselrichterId);
            Assert.Equal("Muster 2500TL", zeile.Straenge[0].WechselrichterName);
            Assert.Equal(7, zugang.Lesen());
        }
    }

    [Fact]
    public void Ein_ungeeigneter_Wechselrichter_wird_benannt_abgelehnt()
    {
        var (zeile, anmeldung) = Aufbauen();
        using (anmeldung)
        {
            KiFeldzugang zugang = Zugang("strang_wechselrichter");

            var fehler = Assert.Throws<InvalidOperationException>(() => Setze(zugang, "Muster 5000TL-2M"));

            Assert.Equal(string.Format(CultureInfo.CurrentCulture, Resource.KI_PVS_WR_UNGEEIGNET,
                                       "Muster 5000TL-2M", GRUND_STROM), fehler.Message);
            Assert.Equal(0, zeile.Straenge[0].WechselrichterId);
        }
    }

    [Fact]
    public void Ohne_Bewertung_waehlt_der_Assistent_kein_Geraet()
    {
        var (zeile, anmeldung) = Aufbauen(mitBewertung: false);
        using (anmeldung)
        {
            var fehler = Assert.Throws<InvalidOperationException>(
                () => Setze(Zugang("strang_wechselrichter"), "Muster 2500TL"));

            Assert.Equal(Resource.KI_PVS_WR_OHNE_BEWERTUNG, fehler.Message);
            Assert.Equal(0, zeile.Straenge[0].WechselrichterId);
        }
    }

    [Fact]
    public void Ein_vertraegliches_Modul_wird_gewaehlt_ein_unvertraegliches_abgelehnt()
    {
        var (zeile, anmeldung) = Aufbauen();
        using (anmeldung)
        {
            KiFeldzugang zugang = Zugang("strang_modul");

            var fehler = Assert.Throws<InvalidOperationException>(() => Setze(zugang, "Modul 300"));
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, Resource.KI_PVS_MODUL_UNVERTRAEGLICH,
                                       "Modul 300", GRUND_ROT), fehler.Message);
            Assert.Equal(0, zeile.Straenge[0].ModulId);

            Setze(zugang, "Modul 400");
            Assert.Equal(2004, zeile.Straenge[0].ModulId);
            Assert.Equal("Modul 400", zeile.Straenge[0].ModulName);
            Assert.Equal(4, zugang.Lesen());
        }
    }

    [Fact]
    public void Ohne_Strangabschnitt_lehnt_die_Wahl_benannt_ab()
    {
        var zeile = new ErzeugerZeile { Schluessel = 1, Bezeichner = "Anlage", AnzahlModule = 10 };
        zeile.Straenge.Add(new StrangZeile { Rang = 1, Bezeichner = "Dach Süd" });
        var sicht = new PhotovoltaikKiSicht { Zeilenquelle = () => zeile };
        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.PHOTOVOLTAIK, () => sicht, new KiMaskenhaken());

        var fehler = Assert.Throws<InvalidOperationException>(
            () => Zugang("strang_wechselrichter").Setzen(7));

        Assert.Equal(Resource.KI_PVS_STRANG_OHNE_ABSCHNITT, fehler.Message);
    }
}
