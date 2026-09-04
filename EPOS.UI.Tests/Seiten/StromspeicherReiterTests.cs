using System.Globalization;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Der STROMSPEICHER-Reiter (iU9-W11b.9), Vorbild <c>tabPage_Stromspeicher</c> —
/// die Seite mit NULL Designer-Kindern, die im Vorlaeufer vollstaendig
/// programmatisch entstand.
///
/// <para>Soll: Kopfzeile mit bzw. ohne Lauf, zwoelf Kacheln, das SoC-Bild, die
/// 39 Kennzahlzeilen in drei Gruppen mit ihrer Warnstufe, die Vergleichsspalte
/// nur mit Vergleichslauf, die Ampel, die Warnzeile „ohne Erzeugung" und der
/// Vergleichsknopf erst ab zwei Varianten.</para>
/// </summary>
public class StromspeicherReiterTests : BunitContext
{
    private readonly CultureInfo _kulturVorher = CultureInfo.CurrentUICulture;
    private readonly CultureInfo _zahlenVorher = CultureInfo.CurrentCulture;
    private readonly List<Bildauftrag> _auftraege = new();

    public StromspeicherReiterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
    }

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentUICulture = _kulturVorher;
        CultureInfo.CurrentCulture = _zahlenVorher;
        base.Dispose(disposing);
    }

    private static SpeicherKennzahlenBlock.Zeile Z(string gruppe, string name, string wert,
                                                   string vergleich = "",
                                                   KennzahlStufe stufe = KennzahlStufe.Unbestimmt)
        => new SpeicherKennzahlenBlock.Zeile(gruppe, name, wert, vergleich, "kWh/a", stufe);

    private static SpeicherErgebnisDaten Daten(bool lauf = true, bool vergleich = false,
                                               bool mehrere = true, string hinweis = "")
        => new SpeicherErgebnisDaten
        {
            LaufVorhanden = lauf,
            Kopf = lauf ? "Speicher 1 · Grünstrom · Dauernutzung" : "Noch keine Speicherrechnung",
            Kacheln = lauf
                ? new (string, string)[]
                {
                    ("Kapazität", "10,0"), ("Leistung", "11,0"), ("SoC [%]", "10 … 90"),
                    ("SoC [kWh]", "1,0 … 9,0"), ("Betriebsart", "Grünstrom"),
                    ("Berechnungsart", "Dauernutzung"), ("Ertrag", "312,50"),
                    ("Überschuss", "40,00"), ("Amortisation", "12,5"),
                    ("Vollzyklen", "180,0"), ("Eigenverbrauch", "62,5"), ("Autarkie", "38,1")
                }
                : Array.Empty<(string, string)>(),
            Kennzahlen = lauf
                ? new[]
                {
                    Z(SpeicherKennzahlenBlock.GRUPPE_ENERGIE, "Last", "12.000", vergleich ? "11.500" : ""),
                    Z(SpeicherKennzahlenBlock.GRUPPE_SPEICHER, "Vollzyklen", "180,0",
                      vergleich ? "175,0" : "", KennzahlStufe.Knapp),
                    Z(SpeicherKennzahlenBlock.GRUPPE_WIRTSCHAFT, "Ertrag", "312,50",
                      vergleich ? "300,00" : "", KennzahlStufe.Ok)
                }
                : Array.Empty<SpeicherKennzahlenBlock.Zeile>(),
            MitVergleich = vergleich,
            Ampel = lauf ? "180 von 250 Zyklen" : "",
            AmpelWarnung = false,
            Erzeugungshinweis = hinweis,
            MehrereVarianten = mehrere
        };

    private IRenderedComponent<StromspeicherReiter> Zeichnen(SpeicherErgebnisDaten daten,
                                                             Action? csv = null,
                                                             Action? vergleich = null)
        => Render<StromspeicherReiter>(p =>
        {
            p.Add(x => x.Daten, daten);
            p.Add(x => x.Bild, a => { _auftraege.Add(a); return new byte[] { 1 }; });
            if (csv is not null) p.Add(x => x.Csv, EventCallback.Factory.Create(this, csv));
            if (vergleich is not null) p.Add(x => x.Vergleich, EventCallback.Factory.Create(this, vergleich));
        });

    // =====================================================================

    [Fact]
    public void Die_Kopfzeile_nennt_Variante_Betriebsart_und_Berechnungsart()
    {
        var seite = Zeichnen(Daten());
        Assert.Contains("Grünstrom", seite.Find("p.epos-simerg-status").TextContent);
    }

    /// <summary>
    /// Ohne Speicherlauf steht nur die Warnzeile da - kein Bild, keine Kacheln,
    /// keine Kennzahlen (<c>SpeicherErgebnisAnzeigen</c> :7196-7211).
    /// </summary>
    [Fact]
    public void Ohne_Lauf_bleibt_nur_die_Warnzeile()
    {
        var seite = Zeichnen(Daten(lauf: false));

        Assert.Contains("epos-simerg-warn", seite.Find("p.epos-simerg-status").ClassName);
        Assert.Empty(seite.FindAll("img"));
        Assert.Empty(seite.FindAll("table"));
        Assert.Empty(seite.FindAll("button"));
    }

    [Fact]
    public void Zwoelf_Kacheln_stehen_im_Kachelraster()
    {
        var seite = Zeichnen(Daten());
        Assert.Equal(12, seite.FindAll(".epos-kennzahlkachel").Count);
    }

    [Fact]
    public void Das_SoC_Bild_wird_angefordert()
    {
        Zeichnen(Daten());
        Assert.Contains(_auftraege, a => a.Bild == Bilder.SpeicherSoc);
    }

    /// <summary>Drei Gruppen mit je einer Ueberschriftszeile.</summary>
    [Fact]
    public void Die_Kennzahlen_stehen_in_drei_Gruppen()
    {
        var seite = Zeichnen(Daten());

        Assert.Equal(3, seite.FindAll("table.epos-simerg-kennzahlen tbody").Count);
        Assert.Contains("Energie", seite.Markup);
        Assert.Contains("Wirtschaft", seite.Markup);
    }

    /// <summary>
    /// Die Warnstufe faerbt die Zeile - dieselben vier Werte, die
    /// <c>SpWarnfarbe</c> als <c>Color.FromArgb</c> setzte.
    /// </summary>
    [Fact]
    public void Die_Warnstufe_faerbt_die_Zeile()
    {
        var seite = Zeichnen(Daten());

        Assert.Single(seite.FindAll("tr.epos-stufe-knapp"));
        Assert.Single(seite.FindAll("tr.epos-stufe-ok"));
        Assert.Single(seite.FindAll("tr.epos-stufe-unbestimmt"));
    }

    /// <summary>Ohne Vergleichslauf gibt es die Vergleichsspalte gar nicht.</summary>
    [Fact]
    public void Die_Vergleichsspalte_haengt_am_Vergleichslauf()
    {
        Assert.Equal(3, Zeichnen(Daten()).FindAll("table.epos-simerg-kennzahlen thead th").Count);
        Assert.Equal(4, Zeichnen(Daten(vergleich: true))
                        .FindAll("table.epos-simerg-kennzahlen thead th").Count);
    }

    /// <summary>Vergleichen laesst sich erst ab zwei Varianten (Fachkonzept 7.3).</summary>
    [Fact]
    public void Der_Vergleichsknopf_erscheint_erst_ab_zwei_Varianten()
    {
        var eine = Zeichnen(Daten(mehrere: false), csv: () => { }, vergleich: () => { });
        Assert.Single(eine.FindAll("button"));

        var zwei = Zeichnen(Daten(), csv: () => { }, vergleich: () => { });
        Assert.Equal(2, zwei.FindAll("button").Count);
    }

    /// <summary>Die Warnzeile eines Laufs ohne jede Erzeugung (Abnahmebefund 2).</summary>
    [Fact]
    public void Ein_Lauf_ohne_Erzeugung_bekommt_seine_Warnzeile()
    {
        Assert.Empty(Zeichnen(Daten()).FindAll("[role='alert']"));
        Assert.Single(Zeichnen(Daten(hinweis: "Der Lauf führte keine Erzeugung."))
                      .FindAll("[role='alert']"));
    }

    [Fact]
    public void Die_beiden_Knoepfe_melden_ihren_Klick()
    {
        int csv = 0, vgl = 0;
        var seite = Zeichnen(Daten(), () => csv++, () => vgl++);

        var knoepfe = seite.FindAll("button");
        knoepfe[0].Click();
        knoepfe[1].Click();

        Assert.Equal(1, csv);
        Assert.Equal(1, vgl);
    }
}
