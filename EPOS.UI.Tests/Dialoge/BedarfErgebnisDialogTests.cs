using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Simulation Ergebnisse (iU9-W8.2). Soll sind die Feldkarten der DREI abgelösten
/// Masken — geprüft wird deshalb je AUSPRÄGUNG, nicht je Komponente (Risiko R-W8-1):
///
/// <list type="bullet">
/// <item><c>Form_ErgStromverbraucher</c>: vier Kennzahlen, eine Monatsreihe, keine
/// Optionsgruppe, Reiter „Strombedarf Ergebnisse / Strombedarf monatlich / Grafik
/// Strombedarf".</item>
/// <item><c>Form_ErgProzesswaerme</c>: sieben Kennzahlen, ZWEI Sichten, Reiter
/// „Wärmebedarf Ergebnisse / Übersicht monatlich / Grafik".</item>
/// <item><c>Form_ErgBrauchwasserwaerme</c>: dieselben sieben Kennzahlen, DREI Sichten
/// und der Schalter „Jahresverlauf".</item>
/// </list>
/// </summary>
public class BedarfErgebnisDialogTests : BunitContext
{
    public BedarfErgebnisDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>
    /// Die Sprache der Oberfläche wird auf de-DE gepinnt (Regel seit iU9‑W8, Muster
    /// <c>DeutscheOberflaeche</c> aus <c>GebaeudeKatalogDialogTests</c>) — Kultur UND
    /// Thread-Kultur, damit ein Lauf unter <c>LANG=en_US.UTF-8</c> dieselbe
    /// Zahlenschreibweise sieht.
    /// </summary>
    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
    }

    private static readonly byte[] BILD = { 1, 2, 3, 4 };

    private static string[] Reihe(double start)
    {
        var w = new string[12];
        for (int m = 0; m < 12; m++) w[m] = (start + m).ToString("F2", new CultureInfo("de-DE"));
        return w;
    }

    // --- die drei Ausprägungen als Datensatz -----------------------------------

    private static BedarfErgebnisDaten Strom(int startReiter = 0) => new()
    {
        Sicht = ErgebnisSicht.Strom,
        StartReiter = startReiter,
        Kennzahlen = new[]
        {
            new ErgebnisKennzahl("max. Strombedarf:", "12,00", "kW"),
            new ErgebnisKennzahl("Gesamter Strombedarf:", "340,00", "MWh"),
            new ErgebnisKennzahl("Stromganglinie:", "5,00", "MWh"),
            new ErgebnisKennzahl("Strombedarf Gebäude:", "335,00", "MWh")
        },
        Sichten = new[] { new Monatssicht("Strombedarf", Reihe(10), BILD) }
    };

    private static BedarfErgebnisDaten Waerme(bool mitBrauchwasser, int startReiter = 0,
                                              string titelZusatz = "")
    {
        var sichten = new List<Monatssicht>
        {
            new("Prozesse", Reihe(20), BILD),
            new("Gebäude (incl. ext. Wärmebedarf)", Reihe(30), BILD)
        };
        if (mitBrauchwasser) sichten.Add(new Monatssicht("Brauchwasser", Reihe(40), BILD, true));

        return new BedarfErgebnisDaten
        {
            Sicht = ErgebnisSicht.Waerme,
            MitBrauchwasser = mitBrauchwasser,
            StartReiter = startReiter,
            TitelZusatz = titelZusatz,
            JahresverlaufBild = mitBrauchwasser ? BILD : null,
            Kennzahlen = new[]
            {
                new ErgebnisKennzahl("Netzverluste:", "3,00", "MWh"),
                new ErgebnisKennzahl("Gesamter Wärmebedarf:", "900,00", "MWh"),
                new ErgebnisKennzahl("Externer Wärmebedarf:", "0,00", "MWh"),
                new ErgebnisKennzahl("Wärmebedarf Prozess:", "200,00", "MWh"),
                new ErgebnisKennzahl("Wärmebedarf Gebäude:", "600,00", "MWh"),
                new ErgebnisKennzahl("max. Wärmelast:", "180,00", "kW"),
                new ErgebnisKennzahl(mitBrauchwasser ? "Wärmebedarf Brauchwasser:" : "davon Brauchwasser:",
                             "97,00", "MWh")
            },
            Sichten = sichten
        };
    }

    private IRenderedComponent<BedarfErgebnisDialog> Aufbauen(
        BedarfErgebnisDaten daten,
        string reiterKennzahlen = "Wärmebedarf Ergebnisse",
        string reiterMonate = "Übersicht monatlich",
        string reiterGrafik = "Grafik",
        Action<bool>? geschlossen = null,
        Energieeinheit? einheit = null,
        Action<Energieeinheit>? einheitGewaehlt = null)
        => Render<BedarfErgebnisDialog>(p => p
            .Add(x => x.Daten, daten)
            .Add(x => x.ReiterKennzahlen, reiterKennzahlen)
            .Add(x => x.ReiterMonate, reiterMonate)
            .Add(x => x.ReiterGrafik, reiterGrafik)
            .Add(x => x.Einheit, einheit ?? Energieeinheit.MWh)
            .Add(x => x.EinheitGewaehlt, einheitGewaehlt)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IElement Reiterknopf(IRenderedComponent<BedarfErgebnisDialog> cut, string text)
        => cut.FindAll("[role=tab]").First(b => b.TextContent.Trim() == text);

    // =================================================================================
    // Feldbestand je Ausprägung
    // =================================================================================

    [Fact]
    public void Der_Feldbestand_der_Stromkarte_steht()
    {
        var cut = Aufbauen(Strom(), "Strombedarf Ergebnisse", "Strombedarf monatlich",
                           "Grafik Strombedarf");

        var reiter = cut.FindAll("[role=tab]").Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Strombedarf Ergebnisse", "Strombedarf monatlich", "Grafik Strombedarf" },
                     reiter);

        // Vier Kennzahlen mit den Beschriftungen des Designers.
        var zeilen = cut.FindAll(".epos-raster tbody tr");
        Assert.Equal(4, zeilen.Count);
        Assert.Contains("max. Strombedarf:", zeilen[0].TextContent);
        Assert.Contains("kW", zeilen[0].TextContent);
        Assert.Contains("Strombedarf Gebäude:", zeilen[3].TextContent);

        // EINE Sicht heisst: keine Optionsgruppe (der Vorlaeufer hatte dort keine).
        cut.Find("[role=tab]:nth-child(2)").Click();
        Assert.Empty(cut.FindAll(".epos-optionsgruppe"));
    }

    [Fact]
    public void Der_Feldbestand_der_Prozesskarte_steht()
    {
        var cut = Aufbauen(Waerme(false));

        var reiter = cut.FindAll("[role=tab]").Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Wärmebedarf Ergebnisse", "Übersicht monatlich", "Grafik" }, reiter);

        Assert.Equal(7, cut.FindAll(".epos-raster tbody tr").Count);
        Assert.Contains("davon Brauchwasser:", cut.Markup);

        // ZWEI Optionen, kein Jahresverlauf-Schalter.
        Reiterknopf(cut, "Grafik").Click();
        Assert.Equal(2, cut.FindAll(".epos-option").Count);
        Assert.DoesNotContain("Brauchwasser", cut.Find(".epos-optionsgruppe").TextContent);
        Assert.Empty(cut.FindAll("input[type=checkbox]"));
    }

    [Fact]
    public void Der_Feldbestand_der_Brauchwasserkarte_steht()
    {
        var cut = Aufbauen(Waerme(true));

        Assert.Equal(7, cut.FindAll(".epos-raster tbody tr").Count);
        Assert.Contains("Wärmebedarf Brauchwasser:", cut.Markup);

        Reiterknopf(cut, "Übersicht monatlich").Click();
        Assert.Equal(3, cut.FindAll(".epos-option").Count);
    }

    // =================================================================================
    // Startreiter und Startsicht
    // =================================================================================

    [Fact]
    public void Der_Startreiter_wirkt()
    {
        Assert.Equal("KENNZAHLEN", Aufbauen(Waerme(false)).Instance.Reiterblattschluessel);
        Assert.Equal("MONATE", Aufbauen(Waerme(false, 1)).Instance.Reiterblattschluessel);
        Assert.Equal("GRAFIK", Aufbauen(Waerme(false, 2)).Instance.Reiterblattschluessel);
    }

    /// <summary>
    /// <c>Form_ErgBrauchwasserwaerme.SetPage</c>:421 setzt mit dem Reiter zugleich beide
    /// Optionsgruppen — genau deshalb landet „Berechnen" dort auf der Brauchwassersicht.
    /// Die beiden anderen Masken kennen das nicht.
    /// </summary>
    [Fact]
    public void Der_Startreiter_waehlt_NUR_beim_Brauchwasser_zugleich_die_Sicht()
    {
        var bw = Aufbauen(Waerme(true, 2));
        Assert.Equal(2, bw.Instance.Tabellensicht);
        Assert.Equal(2, bw.Instance.Grafiksicht);

        var prozess = Aufbauen(Waerme(false, 1));
        Assert.Equal(0, prozess.Instance.Tabellensicht);
        Assert.Equal(0, prozess.Instance.Grafiksicht);
    }

    [Fact]
    public void Der_Titelzusatz_haengt_hinter_dem_Titel()
    {
        var cut = Aufbauen(Waerme(true, 2, "Wohnhaus West"));
        Assert.Equal("Simulation Ergebnisse - Wohnhaus West", cut.Instance.Titel);
        Assert.Contains("Simulation Ergebnisse - Wohnhaus West", cut.Find(".epos-dialog-titel").TextContent);
    }

    // =================================================================================
    // Sichtwechsel
    // =================================================================================

    [Fact]
    public void Der_Sichtwechsel_tauscht_die_Monatstabelle()
    {
        var cut = Aufbauen(Waerme(true));
        Reiterknopf(cut, "Übersicht monatlich").Click();

        // Sicht 0 = Prozesse, Reihe beginnt bei 20.
        Assert.Contains("20,00", cut.Find(".epos-raster tbody tr").TextContent);

        cut.FindAll(".epos-option input")[2].Change(true);
        Assert.Equal(2, cut.Instance.Tabellensicht);
        Assert.Contains("40,00", cut.Find(".epos-raster tbody tr").TextContent);
    }

    /// <summary>
    /// Die beiden Optionsgruppen sind bewusst NICHT gekoppelt — der Vorläufer führte je
    /// eine für Tabelle und Bild.
    /// </summary>
    [Fact]
    public void Tabelle_und_Grafik_haben_getrennte_Sichten()
    {
        var cut = Aufbauen(Waerme(true));

        Reiterknopf(cut, "Übersicht monatlich").Click();
        cut.FindAll(".epos-option input")[1].Change(true);
        Assert.Equal(1, cut.Instance.Tabellensicht);

        Reiterknopf(cut, "Grafik").Click();
        Assert.Equal(0, cut.Instance.Grafiksicht);
    }

    // =================================================================================
    // Jahresverlauf
    // =================================================================================

    [Fact]
    public void Der_Jahresverlauf_Schalter_steht_nur_bei_der_Brauchwassersicht()
    {
        var cut = Aufbauen(Waerme(true));
        Reiterknopf(cut, "Grafik").Click();

        Assert.Empty(cut.FindAll("input[type=checkbox]"));   // Sicht 0 = Prozesse

        cut.FindAll(".epos-option input")[2].Change(true);
        Assert.Single(cut.FindAll("input[type=checkbox]"));

        // Zurueck auf Gebaeude: der Schalter verschwindet UND faellt zurueck.
        cut.Find("input[type=checkbox]").Change(true);
        Assert.True(cut.Instance.JahresverlaufGewaehlt);

        cut.FindAll(".epos-option input")[1].Change(true);
        Assert.Empty(cut.FindAll("input[type=checkbox]"));
        Assert.False(cut.Instance.JahresverlaufGewaehlt);
    }

    // =================================================================================
    // Leere Reihen und Tastatur
    // =================================================================================

    [Fact]
    public void Eine_fehlende_Monatsreihe_zeigt_einen_Gedankenstrich()
    {
        var daten = Waerme(false);
        daten.Sichten = new[] { new Monatssicht("Prozesse", null, null) };

        var cut = Aufbauen(daten);
        Reiterknopf(cut, "Übersicht monatlich").Click();

        Assert.Contains("—", cut.Find(".epos-raster tbody tr").TextContent);
    }

    /// <summary>
    /// Esc UND Enter schließen: Der Dialog zeigt nur an und trägt genau einen Knopf —
    /// hier kann Enter nichts versehentlich schreiben.
    /// </summary>
    [Fact]
    public void Esc_und_Enter_schliessen_den_Anzeigedialog()
    {
        int esc = 0;
        Aufbauen(Waerme(false), geschlossen: _ => esc++)
            .Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(1, esc);

        int enter = 0;
        Aufbauen(Waerme(false), geschlossen: _ => enter++)
            .Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(1, enter);
    }

    [Fact]
    public void Der_OK_Knopf_meldet_und_es_gibt_keinen_Abbrechen_Knopf()
    {
        bool gemeldet = false;
        var cut = Aufbauen(Waerme(false), geschlossen: _ => gemeldet = true);

        var knoepfe = cut.FindAll(".epos-schlussleiste button").Select(b => b.TextContent.Trim()).ToList();
        Assert.DoesNotContain("Abbrechen", knoepfe);

        cut.FindAll("button").First(b => b.TextContent.Trim() == "OK").Click();
        Assert.True(gemeldet);
    }

    // =================================================================================
    // Die Einheitenwahl (Anwenderentscheid W8-O-5 vom 04.09.2026)
    // =================================================================================

    private static readonly byte[] BILD_KWH = { 9, 9, 9, 9 };

    /// <summary>
    /// Ein Wärmedatensatz, wie ihn die Hülle seit dem Entscheid baut: jede
    /// Energiekennzahl mit ihrer QUELLENEINHEIT. Der Brauchwasserwert kommt aus
    /// <c>brauchwasserwerte.Sum()</c> und liegt deshalb in kWh, alle übrigen in MWh.
    /// </summary>
    private static BedarfErgebnisDaten WaermeMitEinheiten(bool mitBrauchwasser = true)
    {
        var zahlen = new double[12];
        for (int m = 0; m < 12; m++) zahlen[m] = 20 + m;

        var sicht = new Monatssicht("Prozesse", Reihe(20), BILD)
        {
            Zahlen = zahlen,
            QuelleEinheit = Energieeinheit.MWh,
            BildKWh = BILD_KWH
        };

        return new BedarfErgebnisDaten
        {
            Sicht = ErgebnisSicht.Waerme,
            MitBrauchwasser = mitBrauchwasser,
            Kennzahlen = new[]
            {
                new ErgebnisKennzahl("max. Wärmelast:", "180,00", "kW"),
                new ErgebnisKennzahl("Gesamter Wärmebedarf:", "900,00", "MWh")
                {
                    Energie = 900, QuelleEinheit = Energieeinheit.MWh
                },
                new ErgebnisKennzahl("Wärmebedarf Brauchwasser:", "97,00", "MWh")
                {
                    Energie = 97000, QuelleEinheit = Energieeinheit.KWh
                }
            },
            Sichten = new[] { sicht }
        };
    }

    private static IElement Einheitenfeld(IRenderedComponent<BedarfErgebnisDialog> cut)
        => cut.Find("select");

    [Fact]
    public void Die_Vorgabe_ist_MWh_und_zeigt_die_Zahlen_des_Bestands()
    {
        var cut = Aufbauen(WaermeMitEinheiten());

        Assert.Same(Energieeinheit.MWh, cut.Instance.Anzeigeeinheit);

        var zeilen = cut.FindAll(".epos-raster tbody tr");
        Assert.Contains("180,00", zeilen[0].TextContent);   // Leistung, unveraendert
        Assert.Contains("kW", zeilen[0].TextContent);
        Assert.Contains("900,00", zeilen[1].TextContent);
        Assert.Contains("MWh", zeilen[1].TextContent);
        // 97 000 kWh sind die 97,00 MWh des Bestands - der frueher nur in EINER der
        // beiden Ansichten gezogene Teiler 1000 steht jetzt als Einheit am Wert.
        Assert.Contains("97,00", zeilen[2].TextContent);
        Assert.Contains("MWh", zeilen[2].TextContent);
    }

    [Fact]
    public void Umschalten_auf_kWh_aendert_Zahl_und_Einheitentext()
    {
        var cut = Aufbauen(WaermeMitEinheiten());

        Einheitenfeld(cut).Change("1");

        Assert.Same(Energieeinheit.KWh, cut.Instance.Anzeigeeinheit);
        var zeilen = cut.FindAll(".epos-raster tbody tr");
        Assert.Contains("180,00", zeilen[0].TextContent);   // kW bleibt kW
        Assert.Contains("kW", zeilen[0].TextContent);
        Assert.Contains("900000", zeilen[1].TextContent);
        Assert.Contains("kWh", zeilen[1].TextContent);
        Assert.Contains("97000", zeilen[2].TextContent);
    }

    [Fact]
    public void Die_Monatstabelle_folgt_der_Wahl()
    {
        var cut = Aufbauen(WaermeMitEinheiten());
        Reiterknopf(cut, "Übersicht monatlich").Click();

        var januar = cut.FindAll(".epos-raster tbody tr")[0];
        Assert.Contains("20,00", januar.TextContent);
        Assert.Contains("MWh", januar.TextContent);

        Einheitenfeld(cut).Change("1");

        januar = cut.FindAll(".epos-raster tbody tr")[0];
        Assert.Contains("20000", januar.TextContent);
        Assert.Contains("kWh", januar.TextContent);
    }

    [Fact]
    public void Das_Saeulenbild_wechselt_mit_der_Einheit()
    {
        var cut = Aufbauen(WaermeMitEinheiten());
        Reiterknopf(cut, "Grafik").Click();

        string mwh = cut.Find("img.epos-chartbild").GetAttribute("src") ?? "";
        Einheitenfeld(cut).Change("1");
        string kwh = cut.Find("img.epos-chartbild").GetAttribute("src") ?? "";

        Assert.NotEqual(mwh, kwh);
        Assert.Contains(Convert.ToBase64String(BILD), mwh);
        Assert.Contains(Convert.ToBase64String(BILD_KWH), kwh);
    }

    [Fact]
    public void Die_Wahl_wird_beim_Aendern_gemeldet()
    {
        Energieeinheit? gemerkt = null;
        var cut = Aufbauen(WaermeMitEinheiten(), einheitGewaehlt: e => gemerkt = e);

        Einheitenfeld(cut).Change("1");
        Assert.Same(Energieeinheit.KWh, gemerkt);
    }

    [Fact]
    public void Der_Dialog_oeffnet_mit_der_gemerkten_Einheit()
    {
        var cut = Aufbauen(WaermeMitEinheiten(), einheit: Energieeinheit.KWh);

        Assert.Same(Energieeinheit.KWh, cut.Instance.Anzeigeeinheit);
        Assert.Contains("900000", cut.FindAll(".epos-raster tbody tr")[1].TextContent);
    }

    /// <summary>
    /// Ein Datensatz aus fertigen Texten — ohne Zahl und Quelleneinheit — bleibt, wie er
    /// ist: kein Wahlfeld, keine Umrechnung.
    /// </summary>
    [Fact]
    public void Ohne_Zahlen_erscheint_kein_Wahlfeld()
    {
        var cut = Aufbauen(Waerme(false));
        Assert.Empty(cut.FindAll("select"));
    }
}
