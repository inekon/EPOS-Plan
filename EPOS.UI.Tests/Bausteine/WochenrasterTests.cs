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

    // =================================================================================
    // Ohne Zusätze: das Markup des Bestands (Stufe KP2, Welle U0b)
    // =================================================================================

    /// <summary>
    /// Die Fälle des Bestands — so, wie das Sollwert-Zeitprogramm von AK1 das Raster zeichnet
    /// (<c>GebaeudeWaermeuebergabeFelder</c>: Wert, Vorgabe, Grenzen, Einheit, Kennung, Fehlermeldung),
    /// dazu gesperrt und ohne Gaben.
    /// </summary>
    private IRenderedComponent<Wochenraster> Bestandsfall(string fall)
    {
        double[] woche = Woche();
        woche[1 * 24 + 8] = 21.5;
        return fall switch
        {
            "ohne_gaben" => Render<Wochenraster>(),
            "vorgabe" => Render<Wochenraster>(p => p
                .Add(x => x.Vorgabe, Woche())
                .Add(x => x.Min, 5.0).Add(x => x.Max, 30.0).Add(x => x.Einheit, "°C")
                .Add(x => x.Kennung, "sollwertprofil")),
            "zeitprogramm" => Render<Wochenraster>(p => p
                .Add(x => x.Wert, woche)
                .Add(x => x.Vorgabe, Woche())
                .Add(x => x.Min, 5.0).Add(x => x.Max, 30.0).Add(x => x.Einheit, "°C")
                .Add(x => x.Kennung, "sollwertprofil")
                .Add(x => x.WertChanged, (double[]? _) => { })
                .Add(x => x.FehlerZustand, ((string Feld, bool Fehlerhaft) _) => { })),
            "gesperrt" => Render<Wochenraster>(p => p
                .Add(x => x.Wert, woche)
                .Add(x => x.Aktiv, false)),
            _ => throw new ArgumentOutOfRangeException(nameof(fall))
        };
    }

    private static string Abdruck(string markup)
        => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
               System.Text.Encoding.UTF8.GetBytes(markup.Replace("\r\n", "\n"))));

    /// <summary>
    /// <b>Ohne Zusätze bleibt das Markup Zeichen für Zeichen das des Bestands</b> — die Abdrücke
    /// sind am Stand vor der Welle U0b (<c>60b0b25f</c>) genommen. So bleibt der Weg des
    /// Sollwert-Zeitprogramms (AK1) unberührt, solange er <c>MitAus</c> und <c>Umbrechend</c> nicht setzt.
    /// </summary>
    // =================================================================================
    // Zusatz „aus" (MitAus): eine Zelle nimmt eine Zahl oder „aus" (P2)
    // =================================================================================

    [Fact]
    public void Mit_aus_meldet_eine_Zelle_NaN_und_zeigt_aus_als_eigenen_Zustand()
    {
        double[]? gemeldet = null;
        var cut = Render<Wochenraster>(p => p
            .Add(x => x.Wert, Woche())
            .Add(x => x.MitAus, true)
            .Add(x => x.WertChanged, (double[]? w) => gemeldet = w));

        Zelle(cut, "Di 08 Uhr").Input("aus");

        Assert.NotNull(gemeldet);
        double[] erwartet = Woche();
        erwartet[1 * 24 + 8] = double.NaN;
        Assert.Equal(erwartet, gemeldet);   // Equals: NaN gleich NaN, alle übrigen Zellen unverändert

        // Der Wirt übernimmt den Stand: Die Zelle zeigt „aus" als eigenen Zustand.
        cut.Render(p => p.Add(x => x.Wert, gemeldet));
        IElement zelle = Zelle(cut, "Di 08 Uhr");
        Assert.Equal("aus", zelle.GetAttribute("value"));
        Assert.Contains("epos-eingabe--aus", zelle.ClassName);
        Assert.DoesNotContain("epos-fehleingabe", zelle.ClassName);
        Assert.Single(cut.FindAll("input.epos-eingabe--aus"));
    }

    [Fact]
    public void Ein_neuer_Wert_hebt_aus_wieder_auf()
    {
        double[] woche = Woche();
        woche[6 * 24 + 3] = double.NaN;
        double[]? gemeldet = null;
        var cut = Render<Wochenraster>(p => p
            .Add(x => x.Wert, woche)
            .Add(x => x.MitAus, true)
            .Add(x => x.WertChanged, (double[]? w) => gemeldet = w));
        Assert.Equal("aus", Zelle(cut, "So 03 Uhr").GetAttribute("value"));
        Assert.Contains("epos-eingabe--aus", Zelle(cut, "So 03 Uhr").ClassName);

        Zelle(cut, "So 03 Uhr").Input("18");
        Assert.Equal(18.0, gemeldet![6 * 24 + 3]);

        cut.Render(p => p.Add(x => x.Wert, gemeldet));
        Assert.Equal("18", Zelle(cut, "So 03 Uhr").GetAttribute("value"));
        Assert.Empty(cut.FindAll("input.epos-eingabe--aus"));
    }

    [Fact]
    public void Ohne_MitAus_ist_aus_eine_Fehleingabe_wie_im_Bestand()
    {
        int meldungen = 0;
        var fehler = new List<(string Feld, bool Fehlerhaft)>();
        var cut = Render<Wochenraster>(p => p
            .Add(x => x.Wert, Woche())
            .Add(x => x.WertChanged, (double[]? _) => meldungen++)
            .Add(x => x.FehlerZustand, ((string Feld, bool Fehlerhaft) e) => fehler.Add(e)));

        Zelle(cut, "Mo 07 Uhr").Input("aus");

        Assert.Equal(0, meldungen);
        Assert.Contains(("Mo 07 Uhr", true), fehler);
        Assert.Empty(cut.FindAll(".epos-wochenraster-hinweis--aus"));
    }

    [Fact]
    public void Mit_aus_nimmt_auch_die_ganze_Zeile_aus_an_und_nennt_den_Zustand()
    {
        double[]? gemeldet = null;
        var cut = Render<Wochenraster>(p => p
            .Add(x => x.Wert, Woche())
            .Add(x => x.MitAus, true)
            .Add(x => x.WertChanged, (double[]? w) => gemeldet = w));

        Assert.Contains("abgeschaltet", cut.Find(".epos-wochenraster-hinweis--aus").TextContent);

        cut.Find(".epos-wochenraster-werkzeug select").Change("5");   // Sa
        cut.FindAll(".epos-wochenraster-werkzeug input").Single().Input("AUS");
        Knopf(cut, "Zeile setzen").Click();

        Assert.NotNull(gemeldet);
        for (int t = 0; t < 7; t++)
            for (int h = 0; h < 24; h++)
            {
                double w = gemeldet![t * 24 + h];
                if (t == 5) Assert.True(double.IsNaN(w), $"Sa {h}");
                else Assert.Equal(Woche()[t * 24 + h], w);
            }
    }

    [Fact]
    public void Der_Text_von_aus_kommt_aus_dem_Buendel()
    {
        double[]? gemeldet = null;
        var cut = Render<Wochenraster>(p => p
            .Add(x => x.Wert, Woche())
            .Add(x => x.MitAus, true)
            .Add(x => x.Texte, new WochenrasterTexte { Aus = "off", HinweisAus = "off: switched off" })
            .Add(x => x.WertChanged, (double[]? w) => gemeldet = w));

        Zelle(cut, "Mi 12 Uhr").Input("aus");
        Assert.Null(gemeldet);                       // der deutsche Text gilt hier nicht

        Zelle(cut, "Mi 12 Uhr").Input("off");
        Assert.True(double.IsNaN(gemeldet![2 * 24 + 12]));
        Assert.Equal("off: switched off", cut.Find(".epos-wochenraster-hinweis--aus").TextContent.Trim());
    }

    // =================================================================================
    // Zusatz „umbrechend": die Anordnung nach Behälterbreite
    // =================================================================================

    [Fact]
    public void Umbrechend_setzt_die_Anordnungsklasse_und_die_Stunde_je_Zelle()
    {
        var cut = Render<Wochenraster>(p => p
            .Add(x => x.Wert, Woche())
            .Add(x => x.Umbrechend, true));

        Assert.Equal("epos-wochenraster epos-wochenraster--umbrechend", cut.Find("div").ClassName);
        IReadOnlyList<IElement> zeilen = cut.FindAll("tbody tr");
        Assert.Equal(7, zeilen.Count);
        foreach (IElement zeile in zeilen)
        {
            string[] stunden = zeile.QuerySelectorAll("td").Select(td => td.GetAttribute("data-stunde") ?? "").ToArray();
            Assert.Equal(Enumerable.Range(0, 24).Select(h => h.ToString("00", CultureInfo.InvariantCulture)), stunden);
        }

        // Die Tabelle bleibt eine Tabelle: Kopf, Tagesnamen und 168 Zellen wie im Bestand.
        Assert.Equal(25, cut.FindAll("thead th").Count);
        Assert.Equal(168, cut.FindAll("table.epos-wochenraster-tabelle input").Count);
    }

    [Fact]
    public void Ohne_Umbrechend_traegt_keine_Zelle_eine_Stunde()
    {
        var cut = Render<Wochenraster>(p => p.Add(x => x.Wert, Woche()));

        Assert.Equal("epos-wochenraster", cut.Find("div").ClassName);
        Assert.Empty(cut.FindAll("td[data-stunde]"));
    }

    [Fact]
    public void Beide_Zusaetze_zusammen_bleiben_bedienbar()
    {
        double[]? gemeldet = null;
        var cut = Render<Wochenraster>(p => p
            .Add(x => x.Wert, Woche())
            .Add(x => x.MitAus, true)
            .Add(x => x.Umbrechend, true)
            .Add(x => x.WertChanged, (double[]? w) => gemeldet = w));

        Zelle(cut, "Fr 23 Uhr").Input("aus");

        Assert.True(double.IsNaN(gemeldet![4 * 24 + 23]));
        Assert.Contains("epos-wochenraster--umbrechend", cut.Find("div").ClassName);
    }

    [Theory]
    [InlineData("ohne_gaben", "572100E6CC1BFC8CE5FE909448D8244EDFDF7CEE8906967086F61FB0CC274D4F")]
    [InlineData("vorgabe", "919BC3D50D2FAC7CBD1BFFCE69AFA9E0972894DECE49919D6AB3809F83845E9C")]
    [InlineData("zeitprogramm", "6AF3ECA769FC1718EFAAF20684864346CEF56F334E92529C03769C3EF5B2CADF")]
    [InlineData("gesperrt", "EDADC0FB24F0D195AB0E06E7F764AEB9A4C9A44EAD7A686D6CC616A811D7ED72")]
    public void Ohne_Zusaetze_bleibt_das_Markup_das_des_Bestands(string fall, string abdruck)
    {
        string markup = Bestandsfall(fall).Markup;
        Assert.True(abdruck == Abdruck(markup), $"{fall}: {Abdruck(markup)} ({markup.Length} Zeichen)");
    }
}
