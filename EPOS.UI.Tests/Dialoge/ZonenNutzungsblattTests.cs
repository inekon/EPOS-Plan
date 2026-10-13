using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Ein Probeweg des Blatts „Nutzung und Kalender“ (MA-2) ohne Datenbank: drei Zonen (eine unbeheizt), zwei Katalogprofile;
/// jede Handlung merkt ihre Zonen und macht den Stand zurücknehmbar.
/// </summary>
internal sealed class ZonenNutzungProbeweg
{
    public readonly List<(string Weg, int[] Zonen, long? Profil)> Aufrufe = new();
    public bool Geaendert;
    /// <summary>Der Text der Ferien-Rückfrage (E139); <c>null</c> = keine Zone mit eigenen Ferien.</summary>
    public string? Rueckfrage;
    /// <summary>Die Antwort des Dialogdiensts auf die Rückfrage.</summary>
    public bool Zustimmung;
    public readonly List<string> Gefragt = new();

    private static IReadOnlyDictionary<string, Kalenderkennung> Kennungen(Kalenderkennung k, bool beheizt)
        => ZonenNutzungCtrl.GROESSEN.ToDictionary(g => g, g => !beheizt && (g == DbWerte.KOND_GROESSE_HEIZSOLL || g == DbWerte.KOND_GROESSE_KUEHLSOLL)
                                                                   ? Kalenderkennung.Entfaellt : k);

    public ZonenNutzungsstand Stand() => new(7, new[]
    {
        new ZonenNutzungszeile(1, "Gästezimmer", 300, true, "Hotelzimmer", 11, "DIN V 18599-10", Profilherkunft.Profil, "",
                               Kennungen(Kalenderkennung.Profil, true), 21, 18, Ferienkennung.Projekt, Ferienkennung.Abweichung,
                               Pruefampel.Gruen, Array.Empty<string>()),
        new ZonenNutzungszeile(2, "Gastronomie", 200, true, "", null, "", Profilherkunft.Vorgabe, "Restaurant",
                               Kennungen(Kalenderkennung.Geerbt, true), null, null, Ferienkennung.Fehlt, Ferienkennung.Projekt,
                               Pruefampel.Gelb, new[] { "Zone ohne Profil" }),
        new ZonenNutzungszeile(3, "Keller", 500, false, "", null, "", Profilherkunft.Datei, "",
                               Kennungen(Kalenderkennung.Eigen, false), null, null, Ferienkennung.Projekt, Ferienkennung.Projekt,
                               Pruefampel.Gruen, Array.Empty<string>()),
    }, true, true, Array.Empty<string>(), Geaendert);

    private ZonenNutzungsergebnis Merken(string weg, IReadOnlyCollection<int> zonen, long? profil = null)
    {
        Aufrufe.Add((weg, zonen.OrderBy(z => z).ToArray(), profil));
        Geaendert = true;
        return new ZonenNutzungsergebnis(true, zonen.Count + " Zonen geändert.", zonen.Count, Stand());
    }

    public ZonenNutzungWeg Weg() => new()
    {
        Gebaeudename = "Hotel",
        Lesen = Stand,
        Profile = () => new[] { new ZonenNutzungsprofil(11, "Hotelzimmer", "DIN V 18599-10"), new ZonenNutzungsprofil(12, "Restaurant", "DIN V 18599-10") },
        ProfilZuweisen = (z, p) => Merken("Profil", z, p),
        ProjektvorgabeUebernehmen = (z, ersetzen) => Merken(ersetzen ? "Projekt+Ferien" : "Projekt", z),
        FerienRueckfrage = _ => Rueckfrage,
        Fragen = text => { Gefragt.Add(text); return Task.FromResult(Zustimmung); },
        KalenderKopieren = (q, z) => Merken("Kopieren" + q, z),
        SollwerteSetzen = (z, t, n) => Merken("Soll" + t + "/" + n, z),
        Zuruecknehmen = () => { Aufrufe.Add(("Zurueck", Array.Empty<int>(), null)); Geaendert = false; return new ZonenNutzungsergebnis(true, "zurück", 2, Stand()); },
    };
}

/// <summary>
/// MA-2: das Blatt „Nutzung und Kalender“ — Tabelle aller Zonen mit Kästchen und Kopfkästchen, Kalendermarken und Ferienmarken,
/// Werkzeugleiste nur mit Wahl, „Profil zuweisen…“, „Vorschlag übernehmen“, Projektvorgabe, Kalender kopieren, Sollwerte und
/// Zurücknehmen über den Weg der Hülle, Sprung in den Zonendialog.
/// </summary>
public sealed class ZonenNutzungsblattTests : EposBunitContext
{
    public ZonenNutzungsblattTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<ZonenNutzungsblatt> Blatt(ZonenNutzungProbeweg p, List<int>? geoeffnet = null)
        => Render<ZonenNutzungsblatt>(c =>
        {
            c.Add(x => x.Weg, p.Weg());
            if (geoeffnet is not null) c.Add(x => x.ZoneOeffnen, id => geoeffnet.Add(id));
        });

    private static IReadOnlyList<IElement> Zeilen(IRenderedComponent<ZonenNutzungsblatt> cut)
        => cut.FindAll("table.epos-zonennutzung-tabelle tbody tr");

    private static IElement Knopf(IRenderedComponent<ZonenNutzungsblatt> cut, string klasse)
        => cut.Find(".epos-zonennutzung-werkzeuge button." + klasse);

    [Fact]
    public void Tabelle_zeigt_alle_Zonen_mit_Marken_und_Ampel()
    {
        var cut = Blatt(new ZonenNutzungProbeweg());
        Assert.Equal("Hotel", cut.Find(".epos-zonennutzung-gebaeude").TextContent);
        IReadOnlyList<IElement> zeilen = Zeilen(cut);
        Assert.Equal(3, zeilen.Count);
        Assert.Equal(5, zeilen[0].QuerySelectorAll(".epos-zonennutzung-kalender .epos-zonennutzung-marke").Length);
        Assert.Equal(5, zeilen[0].QuerySelectorAll(".epos-zonennutzung-marke--profil").Length);
        Assert.Equal(5, zeilen[1].QuerySelectorAll(".epos-zonennutzung-marke--geerbt").Length);
        Assert.Equal(2, zeilen[2].QuerySelectorAll(".epos-zonennutzung-marke--entfaellt").Length);
        Assert.NotNull(zeilen[0].QuerySelector("[data-zeile=SAISON].epos-zonennutzung-marke--abweichung"));
        Assert.NotNull(zeilen[1].QuerySelector("[data-zeile=FERIEN].epos-zonennutzung-marke--fehlt"));
        Assert.Contains("epos-ampel-gelb", zeilen[1].QuerySelector(".epos-ampel")!.ClassName);
        Assert.Contains("Restaurant", zeilen[1].QuerySelector(".epos-zonennutzung-vorschlag")!.TextContent);
        Assert.Equal(R.ZNB_HERKUNFT_DATEI, zeilen[2].QuerySelector(".epos-zonennutzung-herkunft")!.TextContent);
        // Ohne Delegat keine Spalte „Einzelheiten“.
        Assert.Empty(cut.FindAll("button.epos-zonennutzung-zone"));
    }

    [Fact]
    public void Werkzeugleiste_nur_mit_Wahl_und_Kopfkaestchen_waehlt_alle()
    {
        var cut = Blatt(new ZonenNutzungProbeweg());
        Assert.True(Knopf(cut, "epos-zonennutzung-profil").HasAttribute("disabled"));
        Assert.Equal(R.ZNB_SPERRE_WAHL, Knopf(cut, "epos-zonennutzung-projekt").GetAttribute("title"));
        cut.Find("input.epos-zonennutzung-alle").Change(true);
        Assert.Equal(3, cut.Instance.Wahl.Count);
        Assert.False(Knopf(cut, "epos-zonennutzung-profil").HasAttribute("disabled"));
        cut.Find("input.epos-zonennutzung-alle").Change(false);
        Assert.Empty(cut.Instance.Wahl);
        Zeilen(cut)[1].QuerySelector("input.epos-zonennutzung-zeilenwahl")!.Change(true);
        Assert.Equal(new[] { 2 }, cut.Instance.Wahl);
        Assert.False(Knopf(cut, "epos-zonennutzung-soll").HasAttribute("disabled"));
    }

    [Fact]
    public void Profil_zuweisen_ruft_die_Huelle_fuer_alle_gewaehlten_Zonen()
    {
        var p = new ZonenNutzungProbeweg();
        var geaendert = 0;
        var cut = Render<ZonenNutzungsblatt>(c => c.Add(x => x.Weg, p.Weg()).Add(x => x.Geaendert, () => geaendert++));
        Zeilen(cut)[0].QuerySelector("input.epos-zonennutzung-zeilenwahl")!.Change(true);
        Zeilen(cut)[1].QuerySelector("input.epos-zonennutzung-zeilenwahl")!.Change(true);
        Knopf(cut, "epos-zonennutzung-profil").Click();
        Assert.Equal(ZonenNutzungsblatt.Werkzeuge.Profil, cut.Instance.OffenesWerkzeug);
        cut.Find("select.epos-zonennutzung-profilwahl").Change("12");
        cut.Find("button.epos-zonennutzung-anwenden").Click();
        Assert.Single(p.Aufrufe);
        Assert.Equal("Profil", p.Aufrufe[0].Weg);
        Assert.Equal(new[] { 1, 2 }, p.Aufrufe[0].Zonen);
        Assert.Equal(12L, p.Aufrufe[0].Profil);
        Assert.Equal(1, geaendert);
        Assert.Equal("2 Zonen geändert.", cut.Find(".epos-zonennutzung-meldung").TextContent);
        // Nach der Änderung steht „Zurücknehmen“ und ruft den Weg.
        Knopf(cut, "epos-zonennutzung-zurueck").Click();
        Assert.Equal("Zurueck", p.Aufrufe[^1].Weg);
        Assert.Empty(cut.FindAll(".epos-zonennutzung-werkzeuge button.epos-zonennutzung-zurueck"));
    }

    /// <summary>E139: Tragen gewählte Zonen eigene Ferien, fragt das Blatt über den Dialogdienst; die Antwort geht an den Weg.</summary>
    [Theory]
    [InlineData(false, "Projekt")]
    [InlineData(true, "Projekt+Ferien")]
    public void Projektvorgabe_fragt_bei_eigenen_Ferien_vorher(bool zustimmung, string erwartet)
    {
        var p = new ZonenNutzungProbeweg { Rueckfrage = "Gästezimmer trägt eigene Ferien.", Zustimmung = zustimmung };
        var cut = Blatt(p);
        cut.Find("input.epos-zonennutzung-alle").Change(true);
        Knopf(cut, "epos-zonennutzung-projekt").Click();
        Assert.Equal(new[] { "Gästezimmer trägt eigene Ferien." }, p.Gefragt);
        Assert.Equal(new[] { erwartet }, p.Aufrufe.Select(a => a.Weg));
    }

    /// <summary>Ohne Zone mit eigenen Ferien fragt das Blatt nicht.</summary>
    [Fact]
    public void Projektvorgabe_ohne_eigene_Ferien_fragt_nicht()
    {
        var p = new ZonenNutzungProbeweg { Zustimmung = true };
        var cut = Blatt(p);
        cut.Find("input.epos-zonennutzung-alle").Change(true);
        Knopf(cut, "epos-zonennutzung-projekt").Click();
        Assert.Empty(p.Gefragt);
        Assert.Equal(new[] { "Projekt" }, p.Aufrufe.Select(a => a.Weg));
    }

    [Fact]
    public void Vorschlag_Projektvorgabe_Kopieren_und_Sollwerte_nehmen_die_Wahl()
    {
        var p = new ZonenNutzungProbeweg();
        var cut = Blatt(p);
        cut.Find("input.epos-zonennutzung-alle").Change(true);
        Knopf(cut, "epos-zonennutzung-vorschlag").Click();
        Knopf(cut, "epos-zonennutzung-projekt").Click();
        Knopf(cut, "epos-zonennutzung-kopieren").Click();
        cut.Find("select.epos-zonennutzung-quellwahl").Change("1");
        cut.Find("button.epos-zonennutzung-anwenden").Click();
        Knopf(cut, "epos-zonennutzung-soll").Click();
        cut.Find("input.epos-zonennutzung-tag").Change("21");
        cut.Find("input.epos-zonennutzung-nacht").Change("x");
        cut.Find("button.epos-zonennutzung-anwenden").Click();
        Assert.Equal(R.ZNB_MSG_ZAHL, cut.Find(".epos-zonennutzung-meldung").TextContent);
        cut.Find("input.epos-zonennutzung-nacht").Change("");
        cut.Find("button.epos-zonennutzung-anwenden").Click();
        Assert.Equal(new[] { "Profil", "Projekt", "Kopieren1", "Soll21/" }, p.Aufrufe.Select(a => a.Weg));
        Assert.All(p.Aufrufe, a => Assert.Equal(new[] { 1, 2, 3 }, a.Zonen));
        Assert.Null(p.Aufrufe[0].Profil);
    }

    [Fact]
    public void Profil_je_Zeile_und_Sprung_in_den_Zonendialog()
    {
        var p = new ZonenNutzungProbeweg();
        var geoeffnet = new List<int>();
        var cut = Blatt(p, geoeffnet);
        Zeilen(cut)[1].QuerySelector("select.epos-zonennutzung-zeilenprofil")!.Change("12");
        Assert.Equal("Profil", p.Aufrufe[0].Weg);
        Assert.Equal(new[] { 2 }, p.Aufrufe[0].Zonen);
        Assert.Equal(12L, p.Aufrufe[0].Profil);
        Zeilen(cut)[2].QuerySelector("button.epos-zonennutzung-zone")!.Click();
        Assert.Equal(new[] { 3 }, geoeffnet);
    }

    [Fact]
    public void Ohne_Stand_eine_Meldung()
    {
        var cut = Render<ZonenNutzungsblatt>(c => c.Add(x => x.Weg, new ZonenNutzungWeg()));
        Assert.Equal(R.ZNB_MSG_NICHT_GELESEN, cut.Find(".epos-zonennutzung-ohne").TextContent);
    }
}
