using System.Globalization;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Der VARIANTENVERGLEICH (iU9-W11b.12), Vorbild
/// <c>Form_SpeicherVariantenVergleich</c> (855 Z.).
///
/// <para>Soll: zwoelf Spalten, die beste Zeile hinterlegt, die aktive fett, die
/// nicht rechenbare in Firebrick mit dem Grund als Mouseover, die Rueckfrage vor
/// dem Umstellen, der stille Ruecksprung auf einer bereits aktiven Zeile, die
/// Meldung ohne Auswahl, das Protokoll und der ECHTE Fortschritt.</para>
/// </summary>
public class SpeicherVariantenVergleichTests : BunitContext
{
    private readonly CultureInfo _kulturVorher = CultureInfo.CurrentUICulture;

    public SpeicherVariantenVergleichTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
    }

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentUICulture = _kulturVorher;
        base.Dispose(disposing);
    }

    private static VergleichDaten Daten(bool keineAktive = false) => new VergleichDaten
    {
        Zeilen = new[]
        {
            new Vergleichszeile
            {
                IdEnergieanlage = 11, Bezeichnung = "Speicher A", Aktiv = !keineAktive,
                Gerechnet = true, Betriebsart = "Grünstrom", Berechnungsart = "Dauernutzung",
                Kapazitaet = "10,0", Leistung = "11,0", Investition = "8.000",
                Ertrag = "1.200", DeltaJ = "310", Amortisation = "12,5",
                Kapitalwert = "2.400", Vollzyklen = "180,0"
            },
            new Vergleichszeile
            {
                IdEnergieanlage = 12, Bezeichnung = "Speicher B", Gerechnet = true,
                Betriebsart = "Graustrom", Berechnungsart = "Preissteuerung / Arbitrage",
                Kapazitaet = "20,0", Leistung = "15,0", Investition = "14.000",
                Ertrag = "2.100", DeltaJ = "520", Amortisation = "9,8",
                Kapitalwert = "5.100", Vollzyklen = "240,0"
            },
            new Vergleichszeile
            {
                IdEnergieanlage = 13, Bezeichnung = "Speicher C", Gerechnet = false,
                Hinweis = "Für diese Anlage ist kein Speicher hinterlegt."
            }
        },
        BesteZeile = 1,
        Status = "3 Varianten gerechnet (412 ms). Beste nach ΔJ: Speicher B.",
        HinweisKeineAktive = keineAktive,
        Protokoll = "Speicher C: Für diese Anlage ist kein Speicher hinterlegt."
    };

    private IRenderedComponent<SpeicherVariantenVergleich> Zeichnen(
        VergleichDaten daten, bool laeuft = false,
        Action<int>? aktiv = null, Action? csv = null, Action? zu = null)
        => Render<SpeicherVariantenVergleich>(p =>
        {
            p.Add(x => x.Daten, daten);
            p.Add(x => x.Laeuft, laeuft);
            p.Add(x => x.Anteil, 0.42);
            p.Add(x => x.FortschrittText, "Variante 3 von 7");
            if (aktiv is not null) p.Add(x => x.AktivSetzen, EventCallback.Factory.Create<int>(this, aktiv));
            if (csv is not null) p.Add(x => x.Csv, EventCallback.Factory.Create(this, csv));
            if (zu is not null) p.Add(x => x.Geschlossen, EventCallback.Factory.Create(this, zu));
        });

    // =====================================================================

    [Fact]
    public void Die_Tabelle_hat_zwoelf_Spalten()
    {
        var seite = Zeichnen(Daten());
        Assert.Equal(12, seite.FindAll("table.epos-simerg-vergleich thead th").Count);
    }

    /// <summary>Beste gruen, aktive fett, Fehlerzeile in Firebrick — drei Aussagen.</summary>
    [Fact]
    public void Beste_aktive_und_Fehlerzeile_tragen_ihre_Klasse()
    {
        var seite = Zeichnen(Daten());
        var zeilen = seite.FindAll("table.epos-simerg-vergleich tbody tr");

        Assert.Contains("epos-simerg-aktiv", zeilen[0].ClassName);
        Assert.Contains("epos-simerg-beste", zeilen[1].ClassName);
        Assert.Contains("epos-simerg-fehler", zeilen[2].ClassName);
    }

    /// <summary>
    /// Die Fehlerzeile: leere Zahlenspalten und der Grund an der Stelle der
    /// Vergleichsgroesse, dazu der Hinweis als Mouseover (woertlich :438-446).
    /// </summary>
    [Fact]
    public void Die_Fehlerzeile_nennt_ihren_Grund()
    {
        var seite = Zeichnen(Daten());
        var zeile = seite.FindAll("table.epos-simerg-vergleich tbody tr")[2];

        Assert.Contains("nicht rechenbar", zeile.TextContent);
        Assert.Contains("kein Speicher hinterlegt", zeile.GetAttribute("title") ?? "");
    }

    /// <summary>Ohne Markierung meldet der Knopf, statt etwas zu tun (:598-602).</summary>
    [Fact]
    public void Ohne_Markierung_meldet_der_Aktivknopf()
    {
        int gesetzt = -1;
        var seite = Zeichnen(Daten(), aktiv: id => gesetzt = id);

        seite.FindAll("div.epos-simerg-fuss button")[0].Click();

        Assert.Equal(-1, gesetzt);
        Assert.Single(seite.FindAll("[role='alert']"));
    }

    /// <summary>Vor dem Umstellen steht die Rueckfrage — sie nennt die Variante.</summary>
    [Fact]
    public void Vor_dem_Umstellen_steht_die_Rueckfrage()
    {
        int gesetzt = -1;
        var seite = Zeichnen(Daten(), aktiv: id => gesetzt = id);

        seite.FindAll("table.epos-simerg-vergleich tbody tr")[1].DoubleClick();
        Assert.Contains("Speicher B", seite.Markup);
        Assert.Equal(-1, gesetzt);

        seite.Find("[role='dialog'] button").Click();
        Assert.Equal(12, gesetzt);
    }

    /// <summary>Eine bereits aktive Zeile: stiller Ruecksprung (woertlich :604).</summary>
    [Fact]
    public void Eine_bereits_aktive_Zeile_fragt_nicht()
    {
        int gesetzt = -1;
        var seite = Zeichnen(Daten(), aktiv: id => gesetzt = id);

        seite.FindAll("table.epos-simerg-vergleich tbody tr")[0].DoubleClick();

        Assert.Empty(seite.FindAll("[role='dialog']"));
        Assert.Equal(-1, gesetzt);
    }

    /// <summary>
    /// Die Warnung gilt genau dann, wenn es Varianten gibt, aber keine aktiv ist
    /// — dann rechnet der Gesamtlauf noch die Aggregation.
    /// </summary>
    [Fact]
    public void Ohne_aktive_Variante_steht_der_Langtext()
    {
        Assert.Empty(Zeichnen(Daten()).FindAll("[role='alert']"));
        Assert.Single(Zeichnen(Daten(keineAktive: true)).FindAll("[role='alert']"));
    }

    /// <summary>
    /// Waehrend der Rechnung steht NUR der Fortschritt da — und er ist echt
    /// (die Schleife kennt ihre Laenge).
    /// </summary>
    [Fact]
    public void Waehrend_der_Rechnung_steht_nur_der_Fortschritt()
    {
        var seite = Zeichnen(Daten(), laeuft: true);

        Assert.Empty(seite.FindAll("table.epos-simerg-vergleich"));
        Assert.Contains("Variante 3 von 7", seite.Markup);
    }

    [Fact]
    public void Das_Protokoll_steht_als_mehrzeiliges_Feld()
    {
        var seite = Zeichnen(Daten());
        Assert.Single(seite.FindAll("textarea"));
    }

    [Fact]
    public void Die_Fusszeile_meldet_ihre_Klicks()
    {
        int csv = 0, zu = 0;
        var seite = Zeichnen(Daten(), csv: () => csv++, zu: () => zu++);

        var knoepfe = seite.FindAll("div.epos-simerg-fuss button");
        Assert.Equal(3, knoepfe.Count);
        knoepfe[1].Click();
        knoepfe[2].Click();

        Assert.Equal(1, csv);
        Assert.Equal(1, zu);
    }
}
