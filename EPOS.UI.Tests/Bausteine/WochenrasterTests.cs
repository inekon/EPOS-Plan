using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <b>Das Wochenraster</b> (Konzept Anlagenkopplung 9.2; Stufe AK1 Welle 3) — ein Zeitprogramm
/// von 168 Wochenstunden als Raster 7 × 24.
///
/// <para><b>Was geprüft wird:</b> ohne Zeitprogramm die VORGABE gesperrt samt Knopf, der daraus
/// ein Zeitprogramm macht; mit Zeitprogramm meldet jede Zelle das GANZE Raster; eine geleerte
/// oder ungültige Zelle ist ein Fehlerfeld und ändert nichts (streng, H-F10 — kein Auffüllen);
/// Zeile setzen und Kopieren auf Werktage, Wochenende und alle Tage; Verwerfen meldet
/// <c>null</c>; das Vorschaubild bekommt die angezeigte Woche und wird nur bei geändertem
/// Wert neu gebaut; der Fall ohne Gaben.</para>
///
/// <para>Die Kultur ist auf de-DE gepinnt: Die Zellen zeigen deutsche Zahlen.</para>
/// </summary>
public class WochenrasterTests : EposBunitContext
{
    private const int WERTE = 168;

    public WochenrasterTests()
    {
        // DiagrammSvg (Vorschaubild) ruft sein Modul; im Prüfstand ohne Antwort.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>Eine Bestandswoche: werktags 06–22 Uhr 20 °C, sonst 17 °C; am Wochenende 18 °C.</summary>
    private static double[] Woche()
    {
        var w = new double[WERTE];
        for (int t = 0; t < 7; t++)
            for (int h = 0; h < 24; h++)
                w[t * 24 + h] = t >= 5 ? 18.0 : (h >= 6 && h < 22 ? 20.0 : 17.0);
        return w;
    }

    private static IElement Zelle(IRenderedComponent<Wochenraster> cut, string name)
        => cut.Find($"label.epos-feld[title='{name}'] input");

    private static IElement Knopf(IRenderedComponent<Wochenraster> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // =================================================================================
    // Ohne Zeitprogramm: die Vorgabe
    // =================================================================================

    [Fact]
    public void Ohne_Zeitprogramm_zeigt_das_Raster_die_Vorgabe_gesperrt()
    {
        var cut = Render<Wochenraster>(p => p.Add(x => x.Vorgabe, Woche()));

        Assert.Contains("Noch kein Zeitprogramm", cut.Find(".epos-wochenraster-hinweis").TextContent);
        IReadOnlyList<IElement> zellen = cut.FindAll("table.epos-wochenraster-tabelle input");
        Assert.Equal(WERTE, zellen.Count);
        Assert.All(zellen, z => Assert.True(z.HasAttribute("disabled")));
        Assert.Equal("20", Zelle(cut, "Mo 07 Uhr").GetAttribute("value"));
        Assert.Equal("17", Zelle(cut, "Mo 23 Uhr").GetAttribute("value"));
        Assert.Equal("18", Zelle(cut, "So 12 Uhr").GetAttribute("value"));

        // Ohne Zeitprogramm gibt es kein Werkzeug, nur den Knopf, der eins anlegt.
        Assert.Empty(cut.FindAll(".epos-wochenraster-werkzeug"));
        Assert.False(Knopf(cut, "Als Zeitprogramm bearbeiten").HasAttribute("disabled"));
    }

    [Fact]
    public void Als_Zeitprogramm_bearbeiten_meldet_die_Vorgabe_als_168_Werte()
    {
        double[]? gemeldet = null;
        var cut = Render<Wochenraster>(p => p
            .Add(x => x.Vorgabe, Woche())
            .Add(x => x.WertChanged, (double[]? w) => gemeldet = w));

        Knopf(cut, "Als Zeitprogramm bearbeiten").Click();

        Assert.NotNull(gemeldet);
        Assert.Equal(Woche(), gemeldet);
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_es_leer_und_sperrt_das_Anlegen()
    {
        var cut = Render<Wochenraster>();

        Assert.Equal(WERTE, cut.FindAll("table.epos-wochenraster-tabelle input").Count);
        Assert.All(cut.FindAll("table.epos-wochenraster-tabelle input"),
                   z => Assert.True(string.IsNullOrEmpty(z.GetAttribute("value"))));
        Assert.True(Knopf(cut, "Als Zeitprogramm bearbeiten").HasAttribute("disabled"));
        Assert.Empty(cut.FindAll("svg"));
    }

    [Fact]
    public void Die_Zeilen_heissen_nach_den_Wochentagen_und_die_Spalten_nach_der_Stunde()
    {
        var cut = Render<Wochenraster>(p => p.Add(x => x.Vorgabe, Woche()));

        Assert.Equal(new[] { "Mo", "Di", "Mi", "Do", "Fr", "Sa", "So" },
                     cut.FindAll("tbody th[scope=row]").Select(t => t.TextContent.Trim()));
        IReadOnlyList<IElement> kopf = cut.FindAll("thead th");
        Assert.Equal(25, kopf.Count);
        Assert.Equal("00", kopf[1].TextContent.Trim());
        Assert.Equal("23", kopf[24].TextContent.Trim());
    }

    // =================================================================================
    // Mit Zeitprogramm: jede Eingabe meldet das ganze Raster
    // =================================================================================

    [Fact]
    public void Eine_Zelle_meldet_das_ganze_Raster_mit_dem_neuen_Wert()
    {
        double[]? gemeldet = null;
        var cut = Render<Wochenraster>(p => p
            .Add(x => x.Wert, Woche())
            .Add(x => x.WertChanged, (double[]? w) => gemeldet = w));

        Zelle(cut, "Di 08 Uhr").Input("21,5");

        Assert.NotNull(gemeldet);
        double[] erwartet = Woche();
        erwartet[1 * 24 + 8] = 21.5;
        Assert.Equal(erwartet, gemeldet);
    }

    [Fact]
    public void Eine_geleerte_Zelle_ist_ein_Fehlerfeld_und_aendert_nichts()
    {
        int meldungen = 0;
        var fehler = new List<(string Feld, bool Fehlerhaft)>();
        var cut = Render<Wochenraster>(p => p
            .Add(x => x.Wert, Woche())
            .Add(x => x.WertChanged, (double[]? _) => meldungen++)
            .Add(x => x.FehlerZustand, ((string Feld, bool Fehlerhaft) e) => fehler.Add(e)));

        Zelle(cut, "Mi 10 Uhr").Input("");

        Assert.Equal(0, meldungen);
        Assert.Contains(("Mi 10 Uhr", true), fehler);
    }

    [Fact]
    public void Ein_Wert_ausserhalb_der_Grenzen_faerbt_und_wird_nicht_gemeldet()
    {
        int meldungen = 0;
        var fehler = new List<(string Feld, bool Fehlerhaft)>();
        var cut = Render<Wochenraster>(p => p
            .Add(x => x.Wert, Woche())
            .Add(x => x.Min, 5.0)
            .Add(x => x.Max, 30.0)
            .Add(x => x.WertChanged, (double[]? _) => meldungen++)
            .Add(x => x.FehlerZustand, ((string Feld, bool Fehlerhaft) e) => fehler.Add(e)));

        Zelle(cut, "Fr 18 Uhr").Input("45");

        Assert.Equal(0, meldungen);
        Assert.Contains(("Fr 18 Uhr", true), fehler);
        Assert.Contains("epos-fehleingabe", Zelle(cut, "Fr 18 Uhr").ClassName);
    }

    [Fact]
    public void Zeile_setzen_belegt_die_gewaehlte_Zeile_mit_einem_Wert()
    {
        double[]? gemeldet = null;
        var cut = Render<Wochenraster>(p => p
            .Add(x => x.Wert, Woche())
            .Add(x => x.WertChanged, (double[]? w) => gemeldet = w));

        cut.Find(".epos-wochenraster-werkzeug select").Change("2");   // Mi
        cut.FindAll(".epos-wochenraster-werkzeug input").Single().Input("19");
        Knopf(cut, "Zeile setzen").Click();

        Assert.NotNull(gemeldet);
        for (int t = 0; t < 7; t++)
            for (int h = 0; h < 24; h++)
                Assert.Equal(t == 2 ? 19.0 : Woche()[t * 24 + h], gemeldet![t * 24 + h]);
    }

    [Fact]
    public void Ohne_Zeilenwert_ist_Zeile_setzen_gesperrt()
    {
        var cut = Render<Wochenraster>(p => p.Add(x => x.Wert, Woche()));

        Assert.True(Knopf(cut, "Zeile setzen").HasAttribute("disabled"));
    }

    [Theory]
    [InlineData("auf Mo–Fr kopieren", new[] { 0, 1, 2, 3, 4 })]
    [InlineData("auf Sa–So kopieren", new[] { 5, 6 })]
    [InlineData("auf alle Tage kopieren", new[] { 0, 1, 2, 3, 4, 5, 6 })]
    public void Kopieren_traegt_die_gewaehlte_Zeile_auf_die_Zieltage(string knopf, int[] ziele)
    {
        // Quelle ist der Sonntag mit einem eigenen Verlauf: 0 Uhr 10 °C, jede Stunde 0,5 K mehr.
        double[] woche = Woche();
        for (int h = 0; h < 24; h++) woche[6 * 24 + h] = 10.0 + 0.5 * h;

        double[]? gemeldet = null;
        var cut = Render<Wochenraster>(p => p
            .Add(x => x.Wert, woche)
            .Add(x => x.WertChanged, (double[]? w) => gemeldet = w));

        cut.Find(".epos-wochenraster-werkzeug select").Change("6");   // So
        Knopf(cut, knopf).Click();

        Assert.NotNull(gemeldet);
        for (int t = 0; t < 7; t++)
            for (int h = 0; h < 24; h++)
            {
                double erwartet = ziele.Contains(t) || t == 6 ? 10.0 + 0.5 * h : woche[t * 24 + h];
                Assert.Equal(erwartet, gemeldet![t * 24 + h]);
            }
    }

    [Fact]
    public void Verwerfen_meldet_null()
    {
        bool gemeldet = false;
        double[]? wert = Woche();
        var cut = Render<Wochenraster>(p => p
            .Add(x => x.Wert, Woche())
            .Add(x => x.WertChanged, (double[]? w) => { gemeldet = true; wert = w; }));

        Knopf(cut, "Zeitprogramm verwerfen").Click();

        Assert.True(gemeldet);
        Assert.Null(wert);
    }

    [Fact]
    public void Gesperrt_zeigt_es_die_Werte_ohne_Werkzeug_und_Knoepfe()
    {
        var cut = Render<Wochenraster>(p => p
            .Add(x => x.Wert, Woche())
            .Add(x => x.Aktiv, false));

        Assert.Empty(cut.FindAll(".epos-wochenraster-werkzeug"));
        Assert.Empty(cut.FindAll(".epos-wochenraster-knoepfe"));
        Assert.All(cut.FindAll("table.epos-wochenraster-tabelle input"), z => Assert.True(z.HasAttribute("disabled")));
    }

    // =================================================================================
    // Das Vorschaubild
    // =================================================================================

    [Fact]
    public void Die_Vorschau_bekommt_die_angezeigte_Woche_und_baut_nur_bei_neuem_Wert_neu()
    {
        var aufrufe = new List<double[]>();
        Zeichenmodell Bild(double[] w)
        {
            aufrufe.Add(w);
            return new Zeichenmodell(200, 100, Farbton.Aus(Farbrolle.HINTERGRUND));
        }

        var cut = Render<Wochenraster>(p => p
            .Add(x => x.Vorgabe, Woche())
            .Add(x => x.Vorschau, (Func<double[], Zeichenmodell?>)Bild));

        Assert.Single(aufrufe);
        Assert.Equal(Woche(), aufrufe[0]);

        // Ein Zeichenlauf mit demselben Wert baut das Modell nicht neu (Hausregel: das Modell
        // wird zwischengespeichert, sonst verwürfe DiagrammSvg Zoom und Zeigerstelle).
        cut.Render(p => p.Add(x => x.Vorgabe, Woche()));
        Assert.Single(aufrufe);

        double[] neu = Woche();
        neu[0] = 16.0;
        cut.Render(p => p.Add(x => x.Wert, neu));
        Assert.Equal(2, aufrufe.Count);
        Assert.Equal(16.0, aufrufe[1][0]);
    }

    [Fact]
    public void Der_Zellname_nennt_Tag_und_Stunde()
    {
        var cut = Render<Wochenraster>(p => p.Add(x => x.Vorgabe, Woche()));

        Assert.Equal("Mo 00 Uhr", cut.Instance.Zellname(0, 0));
        Assert.Equal("So 23 Uhr", cut.Instance.Zellname(6, 23));
        Assert.Equal(CultureInfo.GetCultureInfo("de-DE").Name, CultureInfo.CurrentCulture.Name);
    }
}
