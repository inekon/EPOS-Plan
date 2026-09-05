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

    /// <summary>
    /// Die Stromkarte, wie die Hülle sie seit dem Anwenderwunsch <b>W8‑E‑2</b>
    /// (Windows-Abnahme 05.09.2026) baut: die LEISTUNG für sich, darunter die zwei
    /// Posten, darunter abgesetzt die Summe. „max. Strombedarf" heißt jetzt „max.
    /// Leistung" und „Strombedarf Gebäude" heißt „Strombedarf aus Profil".
    /// </summary>
    private static BedarfErgebnisDaten Strom(int startReiter = 0,
                                             Ganglinienquelle? ganglinie = null) => new()
    {
        Sicht = ErgebnisSicht.Strom,
        StartReiter = startReiter,
        Kennzahlen = new[]
        {
            new ErgebnisKennzahl("max. Leistung:", "12,00", "kW") { Art = Kennzahlart.Leistung },
            new ErgebnisKennzahl("Stromganglinie:", "5,00", "MWh"),
            new ErgebnisKennzahl("Strombedarf aus Profil:", "335,00", "MWh"),
            new ErgebnisKennzahl("Gesamter Strombedarf:", "340,00", "MWh")
                { Art = Kennzahlart.Summe }
        },
        Sichten = new[] { new Monatssicht("Strombedarf", Reihe(10), BILD) },
        Ganglinie = ganglinie
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
            // Dieselbe Gliederung wie beim Strom (W8-E-2): Leistung, Posten, Summe.
            Kennzahlen = new[]
            {
                new ErgebnisKennzahl("max. Wärmelast:", "180,00", "kW")
                    { Art = Kennzahlart.Leistung },
                new ErgebnisKennzahl("Netzverluste:", "3,00", "MWh"),
                new ErgebnisKennzahl("Externer Wärmebedarf:", "0,00", "MWh"),
                new ErgebnisKennzahl("Wärmebedarf Prozess:", "200,00", "MWh"),
                new ErgebnisKennzahl("Wärmebedarf Gebäude:", "600,00", "MWh"),
                new ErgebnisKennzahl(mitBrauchwasser ? "Wärmebedarf Brauchwasser:" : "davon Brauchwasser:",
                             "97,00", "MWh"),
                new ErgebnisKennzahl("Gesamter Wärmebedarf:", "900,00", "MWh")
                    { Art = Kennzahlart.Summe }
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

        // Vier Kennzahlen in DREI Kategorien (W8-E-2): eine Leistung, zwei Posten,
        // eine Summe im Fuss.
        var zeilen = cut.FindAll(".epos-kennzahlen tbody tr:not(.epos-kennzahlen-kopf)");
        Assert.Equal(3, zeilen.Count);
        Assert.Contains("max. Leistung:", zeilen[0].TextContent);
        Assert.Contains("kW", zeilen[0].TextContent);
        Assert.Contains("Stromganglinie:", zeilen[1].TextContent);
        Assert.Contains("Strombedarf aus Profil:", zeilen[2].TextContent);

        var summe = cut.FindAll(".epos-kennzahlen tfoot tr");
        Assert.Single(summe);
        Assert.Contains("Gesamter Strombedarf:", summe[0].TextContent);
        Assert.Contains("340,00", summe[0].TextContent);

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

        // Sieben Kennzahlen: eine Leistung, fuenf Posten, eine Summe im Fuss (W8-E-2).
        Assert.Equal(6, cut.FindAll(".epos-kennzahlen tbody tr:not(.epos-kennzahlen-kopf)").Count);
        Assert.Single(cut.FindAll(".epos-kennzahlen tfoot tr"));
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

        Assert.Equal(6, cut.FindAll(".epos-kennzahlen tbody tr:not(.epos-kennzahlen-kopf)").Count);
        Assert.Single(cut.FindAll(".epos-kennzahlen tfoot tr"));
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

    // =================================================================================
    // Die drei Kategorien (Anwenderwunsch W8-E-2 vom 05.09.2026)
    // =================================================================================

    /// <summary>
    /// <b>Die LEISTUNG steht für sich und NICHT in der Summe.</b> Das war die
    /// Beanstandung: „max. Strombedarf ist falsch — das ist die max. Leistung, eine
    /// eigene Kategorie in kW, kein Strombedarf." Sie steht deshalb in ihrem eigenen
    /// Block mit eigener Überschrift, und der Summenfuß enthält sie nicht.
    /// </summary>
    [Fact]
    public void Die_Leistung_steht_in_einem_eigenen_Block_und_nicht_in_der_Summe()
    {
        var cut = Aufbauen(Strom(), "Strombedarf Ergebnisse", "Strombedarf monatlich",
                           "Grafik Strombedarf");

        var koepfe = cut.FindAll(".epos-kennzahlen-kopf").Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Leistung", "Energie" }, koepfe);

        // Der Leistungsblock trägt GENAU die Leistungszeile.
        var block = cut.FindAll(".epos-kennzahlen tbody")[0];
        Assert.Contains("max. Leistung:", block.TextContent);
        Assert.Contains("kW", block.TextContent);
        Assert.DoesNotContain("Stromganglinie", block.TextContent);

        // Und der Summenfuß kennt sie nicht.
        string fuss = cut.Find(".epos-kennzahlen tfoot").TextContent;
        Assert.DoesNotContain("max. Leistung", fuss);
        Assert.DoesNotContain("kW", fuss);

        Assert.Single(cut.Instance.Leistungen);
        Assert.Equal(2, cut.Instance.Posten.Count);
        Assert.Single(cut.Instance.Summen);
    }

    /// <summary>
    /// <b>Die Summe steht UNTEN</b> — im <c>tfoot</c> und damit hinter allen Posten,
    /// nicht als zweite Zeile mittendrin wie im Bestand.
    /// </summary>
    [Fact]
    public void Die_Summe_ist_der_Fuss_der_Tabelle()
    {
        var cut = Aufbauen(Waerme(false));

        var tabelle = cut.Find(".epos-kennzahlen");
        int posten = tabelle.QuerySelectorAll("tbody tr:not(.epos-kennzahlen-kopf)").Length;
        Assert.Equal(6, posten);

        var fuss = tabelle.QuerySelector("tfoot tr");
        Assert.NotNull(fuss);
        Assert.Contains("Gesamter Wärmebedarf:", fuss!.TextContent);
        Assert.Contains("900,00", fuss.TextContent);

        // Der Fuss steht im Markup HINTER dem letzten Posten.
        Assert.True(tabelle.InnerHtml.IndexOf("Gesamter Wärmebedarf", StringComparison.Ordinal)
                    > tabelle.InnerHtml.IndexOf("davon Brauchwasser", StringComparison.Ordinal));
    }

    /// <summary>
    /// <b>Ein Datensatz OHNE Kategorien sieht aus wie vorher</b> — alles in einem Block,
    /// keine Zwischenüberschrift, kein Fuß. Das ist die Rückfallsicherung für jeden
    /// Aufrufer, der die Gliederung nicht mitgibt.
    /// </summary>
    [Fact]
    public void Ohne_Kategorien_bleibt_das_Blatt_eine_schlichte_Liste()
    {
        var cut = Aufbauen(WaermeMitEinheiten());

        Assert.Empty(cut.FindAll(".epos-kennzahlen-kopf"));
        Assert.Empty(cut.FindAll(".epos-kennzahlen tfoot tr"));
        Assert.Equal(3, cut.FindAll(".epos-kennzahlen tbody tr").Count);
    }

    // =================================================================================
    // Der Zeitumschalter Jahr | Woche | Tag (W8-E-2)
    // =================================================================================

    /// <summary>Eine Quelle, die ihre Aufrufe mitschreibt.</summary>
    private static Ganglinienquelle Gangquelle(List<(Gangstufe Stufe, int Nummer)> ruf)
        => new()
        {
            Wochen = 52,
            Tage = 365,
            Bild = (stufe, nummer) => { ruf.Add((stufe, nummer)); return BILD_KWH; }
        };

    [Fact]
    public void Der_Grafikreiter_zeigt_Jahr_Woche_und_Tag()
    {
        var ruf = new List<(Gangstufe, int)>();
        var cut = Aufbauen(Strom(2, Gangquelle(ruf)), "Strombedarf Ergebnisse",
                           "Strombedarf monatlich", "Grafik Strombedarf");

        var stufen = cut.FindAll(".epos-gang-stufen .epos-option")
                        .Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Jahr", "Woche", "Tag" }, stufen);

        // JAHR ist die Vorgabe und zeigt UNVERAENDERT das Saeulenbild des Bestands -
        // kein Navigator, kein Aufruf der Ganglinienquelle.
        Assert.Equal(Gangstufe.Jahr, cut.FindComponent<BedarfGangGrafik>().Instance.Stufe);
        Assert.Empty(cut.FindAll(".epos-gang-navigator"));
        Assert.Empty(ruf);
        Assert.Contains(Convert.ToBase64String(BILD),
                        cut.Find("img.epos-chartbild").GetAttribute("src") ?? "");
    }

    [Fact]
    public void Woche_und_Tag_holen_ihr_Bild_beim_Kern()
    {
        var ruf = new List<(Gangstufe Stufe, int Nummer)>();
        var cut = Aufbauen(Strom(2, Gangquelle(ruf)), "Strombedarf Ergebnisse",
                           "Strombedarf monatlich", "Grafik Strombedarf");

        cut.FindAll(".epos-gang-stufen input[type=radio]")[1].Change(true);

        var gang = cut.FindComponent<BedarfGangGrafik>().Instance;
        Assert.Equal(Gangstufe.Woche, gang.Stufe);
        Assert.Equal(1, gang.Nummer);
        Assert.Contains("Woche 1 von 52", cut.Find(".epos-gang-marke").TextContent);
        Assert.Contains((Gangstufe.Woche, 0), ruf);

        cut.FindAll(".epos-gang-stufen input[type=radio]")[2].Change(true);
        Assert.Equal(Gangstufe.Tag, cut.FindComponent<BedarfGangGrafik>().Instance.Stufe);
        Assert.Contains("Tag 1 von 365", cut.Find(".epos-gang-marke").TextContent);
        Assert.Contains((Gangstufe.Tag, 0), ruf);
    }

    /// <summary>
    /// <b>Der Navigator ist eine Schleife</b> — hinter Woche 52 kommt Woche 1, und vor
    /// Woche 1 steht Woche 52. Wer den Jahreswechsel ansehen will, soll nicht durch
    /// 51 Wochen zurückgehen müssen.
    /// </summary>
    [Fact]
    public void Der_Navigator_schaltet_vor_zurueck_und_im_Ring()
    {
        var ruf = new List<(Gangstufe Stufe, int Nummer)>();
        var cut = Aufbauen(Strom(2, Gangquelle(ruf)), "Strombedarf Ergebnisse",
                           "Strombedarf monatlich", "Grafik Strombedarf");
        cut.FindAll(".epos-gang-stufen input[type=radio]")[1].Change(true);

        // Nach jedem Klick neu suchen: Der Zeichenlauf tauscht die Knoepfe aus.
        IElement Knopf(int i) => cut.FindAll(".epos-gang-knopf")[i];

        Knopf(1).Click();                                        // vor
        Assert.Equal(2, cut.FindComponent<BedarfGangGrafik>().Instance.Nummer);
        Assert.Contains((Gangstufe.Woche, 1), ruf);

        Knopf(0).Click();                                        // zurueck
        Assert.Equal(1, cut.FindComponent<BedarfGangGrafik>().Instance.Nummer);

        Knopf(0).Click();                                        // ueber den Anfang hinaus
        Assert.Equal(52, cut.FindComponent<BedarfGangGrafik>().Instance.Nummer);
        Assert.Contains((Gangstufe.Woche, 51), ruf);

        Knopf(1).Click();                                        // und wieder herum
        Assert.Equal(1, cut.FindComponent<BedarfGangGrafik>().Instance.Nummer);
    }

    /// <summary>
    /// <b>Die Wärmeausprägungen dürfen sich nicht verschlechtern</b> (W9‑B‑4/B‑5): Ohne
    /// Ganglinienquelle gibt es weder Umschalter noch Navigator, und der Grafikreiter
    /// sieht aus wie zuvor — samt Sichtwahl und Jahresverlauf-Schalter.
    /// </summary>
    [Fact]
    public void Ohne_Ganglinienquelle_bleibt_der_Grafikreiter_unveraendert()
    {
        var cut = Aufbauen(Waerme(true, 2));

        Assert.Empty(cut.FindAll(".epos-gang-stufen"));
        Assert.Empty(cut.FindAll(".epos-gang-navigator"));

        // Die drei Sichten und der Schalter „Jahresverlauf" stehen unveraendert da.
        Assert.Equal(3, cut.FindAll(".epos-option").Count);
        Assert.Single(cut.FindAll("input[type=checkbox]"));
    }
}
