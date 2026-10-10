using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using EPOS.UI.Standards;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
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
/// und die Knopfgruppen des Grafikreiters.</item>
/// </list>
/// </summary>
public class BedarfErgebnisDialogTests : EposBunitContext
{
    public BedarfErgebnisDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>
    /// Die MONATSSÄULEN als Zeichenmodell (Etappe DG-E3, Gruppe (c)) — zwölf starre
    /// Fächer ohne Zeichenfläche: kein Zoom, dafür der Wert der Säule am Zeiger.
    /// </summary>
    private static readonly Zeichenmodell SAEULEN = Monatssaeulen("MWh");

    /// <summary>Dasselbe Bild mit kWh-Beschriftung — die zweite Fassung der Hülle.</summary>
    private static readonly Zeichenmodell SAEULEN_KWH = Monatssaeulen("kWh");

    private static Zeichenmodell Monatssaeulen(string einheit)
    {
        var werte = new double[12];
        for (int m = 0; m < 12; m++) werte[m] = 20.0 + m;

        return ChartRenderer.MonatsSaeulenModell("Prozesswärme", werte,
                                                 ChartRenderer.C_BEDARF, einheit);
    }

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
                                             Bedarfsgrafikquelle? grafik = null) => new()
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
        Sichten = new[] { new Monatssicht("Strombedarf", Reihe(10), SAEULEN) },
        Grafik = grafik
    };

    private static BedarfErgebnisDaten Waerme(bool mitBrauchwasser, int startReiter = 0,
                                              string titelZusatz = "")
    {
        var sichten = new List<Monatssicht>
        {
            new("Prozesse", Reihe(20), SAEULEN),
            new("Gebäude (incl. ext. Wärmebedarf)", Reihe(30), SAEULEN)
        };
        if (mitBrauchwasser) sichten.Add(new Monatssicht("Brauchwasser", Reihe(40), SAEULEN, true));

        return new BedarfErgebnisDaten
        {
            Sicht = ErgebnisSicht.Waerme,
            MitBrauchwasser = mitBrauchwasser,
            StartReiter = startReiter,
            TitelZusatz = titelZusatz,
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
        Action<Energieeinheit>? einheitGewaehlt = null,
        bool titelAnzeigen = true,
        Func<string, IReadOnlyList<ZeitreihenSpalte>, Task>? csv = null)
        => Render<BedarfErgebnisDialog>(p => p
            .Add(x => x.CsvSpeichern, csv)
            .Add(x => x.Daten, daten)
            .Add(x => x.TitelAnzeigen, titelAnzeigen)
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

        // ZWEI Bedarfsarten als Schaltknoepfe, kein Kaestchen.
        Reiterknopf(cut, "Grafik").Click();
        Assert.Equal(2, cut.FindAll(".epos-bedarfgrafik-reihen button").Count);
        Assert.DoesNotContain("Brauchwasser", cut.Find(".epos-bedarfgrafik-reihen").TextContent);
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

    /// <summary>
    /// Das Kreuz im Dialogkopf wirkt wie Esc/Enter: Es schließt mit <c>true</c>, denn
    /// dieser reine Anzeigedialog kennt kein Abbrechen — das Kreuz erfindet keine neue
    /// Semantik.
    /// </summary>
    [Fact]
    public void Kreuz_schliesst_wie_Esc()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(Waerme(false), geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog-zu").Click();

        Assert.True(ergebnis);
    }

    /// <summary>Titel-bedingter Kopf: ohne Titel zeigt der Kopf weder Titel noch Kreuz.</summary>
    [Fact]
    public void Ohne_Titel_zeigt_der_Kopf_weder_Titel_noch_Kreuz()
    {
        var cut = Aufbauen(Waerme(false), titelAnzeigen: false);

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
        // Der Hilfeknopf bleibt - er haengt nicht am Titel.
        Assert.NotEmpty(cut.FindAll(".epos-dialog-kopf"));
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

    /// <summary>
    /// Ein Wärmedatensatz, wie ihn die Hülle seit dem Entscheid baut: jede
    /// Energiekennzahl mit ihrer QUELLENEINHEIT.
    ///
    /// <para><b>Die dritte Zeile ist eine PROBE, kein Bestandsfall.</b> Seit W8‑O‑5b
    /// (07.09.2026) liefert die Hülle jede Energiekennzahl in MWh — die einzige
    /// kWh-Quelle war das Brauchwasser, und der Kern weist es jetzt selbst in MWh
    /// aus. Der Umrechner muss beide Richtungen können; genau dafür steht die Zeile
    /// hier.</para>
    /// </summary>
    private static BedarfErgebnisDaten WaermeMitEinheiten(bool mitBrauchwasser = true)
    {
        var zahlen = new double[12];
        for (int m = 0; m < 12; m++) zahlen[m] = 20 + m;

        var sicht = new Monatssicht("Prozesse", Reihe(20), SAEULEN)
        {
            Zahlen = zahlen,
            QuelleEinheit = Energieeinheit.MWh,
            ModellKWh = SAEULEN_KWH
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
                new ErgebnisKennzahl("Probe aus einer kWh-Quelle:", "97,00", "MWh")
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
        // 97 000 kWh sind 97,00 MWh - der frueher nur in EINER der beiden Ansichten
        // gezogene Teiler 1000 steht jetzt als Einheit am Wert.
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

        // Der Baustein bekommt die ZWEITE Fassung des Modells - dieselbe Zeichnung
        // mit kWh-Beschriftung. Geprueft wird die REFERENZ: An ihr entscheidet
        // DiagrammSvg, ob es seinen Knotenbaum neu baut.
        Assert.Same(SAEULEN, cut.FindComponent<DiagrammSvg>().Instance.Modell);

        Einheitenfeld(cut).Change("1");

        Assert.Same(SAEULEN_KWH, cut.FindComponent<DiagrammSvg>().Instance.Modell);
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
    // Der Grafikreiter: Bedarfsart und Zeitraster als Knopfgruppen (Anwenderwunsch WG)
    // =================================================================================

    /// <summary>Die Fächerzahl je Raster — 8 760 Stunden, 12 Monate, 52 Wochen, 365 Tage.</summary>
    private static int Zeilen(Bedarfsraster raster) => raster switch
    {
        Bedarfsraster.Monat => 12,
        Bedarfsraster.Woche => 52,
        Bedarfsraster.Tag => 365,
        _ => 8760
    };

    /// <summary>
    /// Eine Bildquelle wie die der Hülle, die ihre Aufrufe mitschreibt: das Jahr als Ganglinie mit
    /// Zeitachse, die Summen als Säulenstapel — echte Modelle des Kerns, je Aufruf ein neues.
    /// </summary>
    private static Bedarfsgrafikquelle Grafikquelle(
        List<(IReadOnlyList<int> Wahl, Bedarfsraster Raster, Energieeinheit Einheit)> ruf)
        => new()
        {
            MitReihe = new[] { 0, 1, 2 },
            Titel = (wahl, raster) => "Wärmebedarf " + raster,
            Modell = (wahl, raster, einheit) =>
            {
                ruf.Add((wahl, raster, einheit));
                int n = Zeilen(raster);
                if (raster == Bedarfsraster.Jahr)
                    return ChartRenderer.JahresverlaufModell("Jahr", Enumerable.Repeat(5.0, n).ToArray(),
                                                             "kW", ChartRenderer.C_BEDARF);
                string[] namen = Enumerable.Range(1, n).Select(i => "Fach " + i).ToArray();
                return ChartRenderer.SaeulenstapelModell("Summen", einheit.Text,
                    wahl.Select(i => new ChartRenderer.Reihe("Reihe " + i, Enumerable.Repeat(1.0, n).ToArray(),
                                                             Farbrolle.HEIZWAERME)).ToList(),
                    namen, namen);
            },
            Spalten = (wahl, raster, einheit) => wahl
                .Select(i => new ZeitreihenSpalte("Reihe " + i, einheit.Text, new double[Zeilen(raster)]))
                .ToList()
        };

    private static BedarfErgebnisDaten WaermeMitGrafik(
        List<(IReadOnlyList<int> Wahl, Bedarfsraster Raster, Energieeinheit Einheit)> ruf)
    {
        BedarfErgebnisDaten daten = Waerme(true, 2);
        daten.Grafik = Grafikquelle(ruf);
        return daten;
    }

    private static IElement Rasterknopf(IRenderedComponent<BedarfErgebnisDialog> cut, Bedarfsraster raster)
        => cut.Find($".epos-bedarfgrafik-raster button[data-raster={raster}]");

    private static IElement Reihenknopf(IRenderedComponent<BedarfErgebnisDialog> cut, int nr)
        => cut.Find($".epos-bedarfgrafik-reihen button[data-sicht=\"{nr}\"]");

    /// <summary>
    /// <b>Kompakt und waagerecht:</b> Beide Knopfgruppen stehen in EINER Leiste über dem Bild —
    /// Schaltknöpfe mit <c>aria-pressed</c>, keine Radioliste, kein Kästchen „Jahresverlauf“ —, und die
    /// Leiste ist im Stilblatt eine umbrechende Zeile (bunit misst kein Layout, geprüft wird die Regel).
    /// </summary>
    [Fact]
    public void Die_Knopfgruppen_stehen_waagerecht_in_einer_Leiste()
    {
        var cut = Aufbauen(WaermeMitGrafik(new()));

        IElement leiste = cut.Find(".epos-bedarfgrafik-leiste");
        Assert.Equal(new[] { "epos-bedarfgrafik-reihen", "epos-bedarfgrafik-raster" },
                     leiste.Children.Where(k => k.TagName == "DIV").Select(k => k.ClassList.Last()).ToArray());
        Assert.Equal(new[] { "Prozesse", "Gebäude (incl. ext. Wärmebedarf)", "Brauchwasser" },
                     leiste.QuerySelectorAll(".epos-bedarfgrafik-reihen button").Select(b => b.TextContent.Trim()));
        Assert.Equal(new[] { "Jahr", "Monat", "Woche", "Tag" },
                     leiste.QuerySelectorAll(".epos-bedarfgrafik-raster button").Select(b => b.TextContent.Trim()));
        Assert.Equal("true", Rasterknopf(cut, Bedarfsraster.Jahr).GetAttribute("aria-pressed"));
        Assert.Equal("true", Reihenknopf(cut, 2).GetAttribute("aria-pressed"));   // Startsicht Brauchwasser
        Assert.Equal("false", Reihenknopf(cut, 0).GetAttribute("aria-pressed"));

        IElement blatt = cut.Find(".epos-bedarfgrafik");
        Assert.Empty(blatt.QuerySelectorAll("input[type=radio]"));
        Assert.Empty(blatt.QuerySelectorAll("input[type=checkbox]"));

        string css = Stilblatt();
        string regel = css[css.IndexOf(".epos-bedarfgrafik-leiste {", StringComparison.Ordinal)..];
        regel = regel[..regel.IndexOf('}')];
        Assert.Contains("display: flex", regel);
        Assert.Contains("flex-wrap: wrap", regel);
        Assert.DoesNotContain("flex-direction", regel);
    }

    private static string Stilblatt()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;
        Assert.NotNull(d);
        return File.ReadAllText(Path.Combine(d!.FullName, "EPOS.UI", "wwwroot", "epos-ui.css"));
    }

    /// <summary>
    /// <b>Das Jahr zuerst:</b> die Ganglinie mit Zeitachse und Zoomleiste, Einheit kW am Zeiger; jedes
    /// Raster holt sein Bild bei der Hülle — die Summen in der gewählten Einheit — und trägt eine eigene
    /// Kennung.
    /// </summary>
    [Theory]
    [InlineData(Bedarfsraster.Monat, "bedarf-grafik-monat")]
    [InlineData(Bedarfsraster.Woche, "bedarf-grafik-woche")]
    [InlineData(Bedarfsraster.Tag, "bedarf-grafik-tag")]
    public void Der_Rasterwechsel_zeichnet_das_jeweilige_Bild(Bedarfsraster raster, string kennung)
    {
        var ruf = new List<(IReadOnlyList<int> Wahl, Bedarfsraster Raster, Energieeinheit Einheit)>();
        var cut = Aufbauen(WaermeMitGrafik(ruf), einheit: Energieeinheit.KWh);

        DiagrammSvg jahr = cut.FindComponent<DiagrammSvg>().Instance;
        Assert.Equal(Bedarfsraster.Jahr, cut.Instance.Raster);
        Assert.Equal("bedarf-grafik-jahr", jahr.Kennung);
        Assert.Equal("kW", jahr.Einheit);
        Assert.NotNull(jahr.Modell?.Flaeche);
        Assert.Single(cut.FindAll(".epos-diagramm-leiste"));
        Assert.Equal(new[] { 2 }, ruf[^1].Wahl);
        Assert.Equal(Bedarfsraster.Jahr, ruf[^1].Raster);

        Rasterknopf(cut, raster).Click();

        DiagrammSvg bild = cut.FindComponent<DiagrammSvg>().Instance;
        Assert.Equal(raster, cut.Instance.Raster);
        Assert.Equal(kennung, bild.Kennung);
        Assert.Equal("kWh", bild.Einheit);
        Assert.Null(bild.Modell?.Flaeche);                                   // Säulen: Fächer, kein Zoom
        Assert.Equal(raster, ruf[^1].Raster);
        Assert.Same(Energieeinheit.KWh, ruf[^1].Einheit);
        Assert.Equal("true", Rasterknopf(cut, raster).GetAttribute("aria-pressed"));
        Assert.Equal("false", Rasterknopf(cut, Bedarfsraster.Jahr).GetAttribute("aria-pressed"));
    }

    /// <summary>
    /// Die Bilder werden je Wahl, Raster und Einheit EINMAL geholt: Zurück auf ein schon gezeigtes Raster
    /// bekommt der Baustein dieselbe Modellreferenz — Zoom und Zeigerstelle bleiben.
    /// </summary>
    [Fact]
    public void Ein_schon_gezeigtes_Bild_wird_nicht_neu_geholt()
    {
        var ruf = new List<(IReadOnlyList<int> Wahl, Bedarfsraster Raster, Energieeinheit Einheit)>();
        var cut = Aufbauen(WaermeMitGrafik(ruf));
        Zeichenmodell? jahr = cut.FindComponent<DiagrammSvg>().Instance.Modell;

        Rasterknopf(cut, Bedarfsraster.Woche).Click();
        int nachWoche = ruf.Count;
        Rasterknopf(cut, Bedarfsraster.Jahr).Click();

        Assert.Same(jahr, cut.FindComponent<DiagrammSvg>().Instance.Modell);
        Assert.Equal(nachWoche, ruf.Count);
    }

    /// <summary>
    /// <b>Mehrfachwahl der Bedarfsarten:</b> Ein Klick nimmt eine Art hinzu oder heraus; die letzte
    /// gewählte bleibt stehen. Die Hülle bekommt die Wahl aufsteigend.
    /// </summary>
    [Fact]
    public void Die_Reihenwahl_wirkt_und_laesst_nie_eine_leere_Wahl()
    {
        var ruf = new List<(IReadOnlyList<int> Wahl, Bedarfsraster Raster, Energieeinheit Einheit)>();
        var cut = Aufbauen(WaermeMitGrafik(ruf));

        Reihenknopf(cut, 0).Click();
        Assert.Equal(new[] { 0, 2 }, cut.Instance.Grafikreihen);
        Assert.Equal(new[] { 0, 2 }, ruf[^1].Wahl);
        Assert.Equal("true", Reihenknopf(cut, 0).GetAttribute("aria-pressed"));

        Reihenknopf(cut, 2).Click();
        Assert.Equal(new[] { 0 }, cut.Instance.Grafikreihen);
        Assert.Equal(0, cut.Instance.Grafiksicht);

        Reihenknopf(cut, 0).Click();                                          // die letzte bleibt
        Assert.Equal(new[] { 0 }, cut.Instance.Grafikreihen);
        Assert.Equal("true", Reihenknopf(cut, 0).GetAttribute("aria-pressed"));
    }

    /// <summary>
    /// <b>„CSV…“ an jedem Bild</b> ruft die Naht der Plattform mit Bildtitel und Spalten: die Ganglinie
    /// über den Ganglinienexport der Kaskade (8 760 Zeilen), die Summen über den eigenen Export der
    /// Säulen (12, 52, 365 Zeilen) in der gewählten Einheit.
    /// </summary>
    [Theory]
    [InlineData(Bedarfsraster.Jahr, 8760)]
    [InlineData(Bedarfsraster.Monat, 12)]
    [InlineData(Bedarfsraster.Woche, 52)]
    [InlineData(Bedarfsraster.Tag, 365)]
    public void CSV_ruft_die_Naht_mit_der_Zeilenzahl_des_Rasters(Bedarfsraster raster, int zeilen)
    {
        var exporte = new List<(string Titel, IReadOnlyList<ZeitreihenSpalte> Spalten)>();
        var cut = Aufbauen(WaermeMitGrafik(new()),
                           csv: (t, s) => { exporte.Add((t, s)); return Task.CompletedTask; });

        Rasterknopf(cut, raster).Click();
        Assert.Single(cut.FindAll("button.epos-diagramm-csv"));
        cut.Find(raster == Bedarfsraster.Jahr ? "div.epos-diagramm-leiste button.epos-diagramm-csv"
                                              : ".epos-bedarfgrafik-leiste button.epos-diagramm-csv").Click();

        var (titel, spalten) = Assert.Single(exporte);
        Assert.Single(spalten);
        Assert.Equal(zeilen, spalten[0].Werte.Length);
        if (raster != Bedarfsraster.Jahr)
        {
            Assert.Equal("Wärmebedarf " + raster, titel);
            Assert.Equal("MWh", spalten[0].Einheit);
        }
    }

    [Fact]
    public void Ohne_Naht_gibt_es_keinen_CSV_Knopf()
    {
        var cut = Aufbauen(WaermeMitGrafik(new()));
        Assert.Empty(cut.FindAll("button.epos-diagramm-csv"));
        Rasterknopf(cut, Bedarfsraster.Woche).Click();
        Assert.Empty(cut.FindAll("button.epos-diagramm-csv"));
    }

    /// <summary>
    /// Die Stromkarte hat EINE Sicht: keine Gruppe der Bedarfsarten, wohl aber das Zeitraster.
    /// </summary>
    [Fact]
    public void Die_Stromkarte_zeigt_nur_das_Zeitraster()
    {
        var ruf = new List<(IReadOnlyList<int> Wahl, Bedarfsraster Raster, Energieeinheit Einheit)>();
        var cut = Aufbauen(Strom(2, Grafikquelle(ruf)), "Strombedarf Ergebnisse",
                           "Strombedarf monatlich", "Grafik Strombedarf");

        Assert.Empty(cut.FindAll(".epos-bedarfgrafik-reihen"));
        Assert.Equal(4, cut.FindAll(".epos-bedarfgrafik-raster button").Count);
        Assert.Equal(new[] { 0 }, ruf[^1].Wahl);
    }

    /// <summary>
    /// Ohne Bildquelle (keine Stundenreihen) bleibt das Zeitraster weg, und das Blatt zeigt die
    /// Monatssäulen der ersten gewählten Sicht als Pixelbild ohne Zoom.
    /// </summary>
    [Fact]
    public void Ohne_Bildquelle_stehen_die_Monatssaeulen_ohne_Raster()
    {
        var cut = Aufbauen(Waerme(true));
        Reiterknopf(cut, "Grafik").Click();

        Assert.Empty(cut.FindAll(".epos-bedarfgrafik-raster"));
        DiagrammSvg bild = cut.FindComponents<DiagrammSvg>().Single().Instance;
        Assert.Equal("bedarf-monate", bild.Kennung);
        Assert.True(bild.ZeigtWertAmElement);
        Assert.Empty(cut.FindAll(".epos-diagramm-leiste"));
        Assert.Empty(cut.FindAll("img"));
    }

    // =================================================================================
    // W8-O-5b: Vorschau und Lauf zeigen DIESELBE Brauchwassermenge (07.09.2026)
    // =================================================================================

    /// <summary>
    /// Die Brauchwasserkennzahl, wie die Hülle sie seit W8‑O‑5b baut — der Wert kommt
    /// aus dem KERN, nicht aus einer im Test gerechneten Zahl:
    /// <c>SimulationWaermebedarf.BrauchwassersummeUebernehmen</c> ist die eine Stelle,
    /// die aus der Stundenreihe [kWh] die ausgewiesene Menge [MWh] macht — auf dem
    /// Vorschauweg wie im Lauf.
    /// </summary>
    private static BedarfErgebnisDaten BrauchwasserAus(double[] stundenreihe)
    {
        var sim = new SimulationWaermebedarf();
        Array.Copy(stundenreihe, sim.brauchwasserwerte,
                   Math.Min(stundenreihe.Length, sim.brauchwasserwerte.Length));
        sim.BrauchwassersummeUebernehmen();

        return new BedarfErgebnisDaten
        {
            Sicht = ErgebnisSicht.Waerme,
            MitBrauchwasser = true,
            Kennzahlen = new[]
            {
                new ErgebnisKennzahl("Wärmebedarf Brauchwasser:",
                                     sim.Waermebedarf_Brauchwasser.ToString("F2",
                                         new CultureInfo("de-DE")), "MWh")
                {
                    Energie = sim.Waermebedarf_Brauchwasser,
                    QuelleEinheit = Energieeinheit.MWh
                }
            },
            Sichten = new[] { new Monatssicht("Brauchwasser", Reihe(40), SAEULEN, true) }
        };
    }

    /// <summary>Eine Stundenreihe mit einer bekannten Jahressumme in kWh.</summary>
    private static double[] Stundenreihe(double jahressummeKWh)
    {
        var reihe = new double[8760];
        for (int h = 0; h < reihe.Length; h++) reihe[h] = (double)(jahressummeKWh / 8760.0);
        return reihe;
    }

    /// <summary>
    /// <b>Der Fall, den der Anwender gesehen hat</b> (W8‑O‑5b): 4 380 kWh
    /// Brauchwasserbedarf standen in <c>Simulation → „Wärmebedarf-Details"</c> als
    /// „0,00 MWh" statt als „4,38 MWh" — die Hülle nahm den bereits geteilten Wert des
    /// Laufs ein zweites Mal als kWh entgegen. Jetzt trägt die Übergabe ihre Einheit,
    /// und der Dialog rechnet nur noch von MWh auf die gewählte Anzeigeeinheit um.
    /// </summary>
    [Fact]
    public void Die_Brauchwassermenge_wird_nicht_ein_zweites_Mal_geteilt()
    {
        var cut = Aufbauen(BrauchwasserAus(Stundenreihe(4380.0)));

        var zeile = cut.FindAll(".epos-raster tbody tr")[0];
        Assert.Contains("Wärmebedarf Brauchwasser", zeile.TextContent);
        Assert.Contains("4,38", zeile.TextContent);
        Assert.Contains("MWh", zeile.TextContent);
        Assert.DoesNotContain("0,00", zeile.TextContent);
    }

    /// <summary>
    /// <b>Vorschau und Lauf zeigen dieselbe Zahl.</b> Beide Wege füllen dieselbe
    /// Stundenreihe und gehen durch dieselbe Kernmethode; der Dialog zeigt deshalb
    /// zweimal denselben Text — in MWh wie in kWh.
    /// </summary>
    [Fact]
    public void Vorschau_und_Lauf_zeigen_dieselbe_Brauchwassermenge()
    {
        double[] reihe = Stundenreihe(4059.7);

        var vorschau = Aufbauen(BrauchwasserAus(reihe));
        var lauf = Aufbauen(BrauchwasserAus(reihe));

        string ausVorschau = vorschau.FindAll(".epos-raster tbody tr")[0].TextContent;
        string ausLauf = lauf.FindAll(".epos-raster tbody tr")[0].TextContent;
        Assert.Equal(ausVorschau, ausLauf);
        Assert.Contains("4,06", ausVorschau);

        Einheitenfeld(vorschau).Change("1");
        Einheitenfeld(lauf).Change("1");

        ausVorschau = vorschau.FindAll(".epos-raster tbody tr")[0].TextContent;
        ausLauf = lauf.FindAll(".epos-raster tbody tr")[0].TextContent;
        Assert.Equal(ausVorschau, ausLauf);
        Assert.Contains("4060", ausVorschau);
        Assert.Contains("kWh", ausVorschau);
    }

    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F3)
    // =====================================================================

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Sie bindet über die
    /// Sichtklasse <c>BedarfErgebnisKiSicht</c>: Die vier Schalter der Anzeige sind
    /// setzbar; „jahresverlauf“ ist das Zeitraster Jahr.
    /// </summary>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_wechselt_die_Sicht()
    {
        var cut = Aufbauen(Waerme(mitBrauchwasser: true));

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.BEDARF_ERGEBNIS));

        WindowsFormsApplication1.KiFeldzugang sicht =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.BEDARF_ERGEBNIS, "tabellensicht");
        Assert.NotNull(sicht);
        Assert.Equal(0, sicht.Lesen());

        KiFeldumsetzung umsetzung = KiFeldwandler.Wandle(sicht, "Prozesse");
        Assert.True(umsetzung.Ok, umsetzung.Grund);
        sicht.Setzen(umsetzung.Wert);
        cut.Render();

        Assert.Equal(0, cut.Instance.Tabellensicht);

        WindowsFormsApplication1.KiFeldzugang jahr =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.BEDARF_ERGEBNIS, "jahresverlauf");
        Assert.NotNull(jahr);
        Assert.True(jahr.Setzbar);
        Assert.Equal(false, jahr.Lesen());                 // ohne Bildquelle kein Jahr
    }

    /// <summary>
    /// Die Anzeigeeinheit ist ein WAHLFELD (KI-D-Q6) und wirkt auf alle drei
    /// Reiterblätter zugleich.
    /// </summary>
    [Fact]
    public void Der_Assistent_waehlt_die_Anzeigeeinheit_ueber_ihren_Text()
    {
        var cut = Aufbauen(WaermeMitEinheiten());

        WindowsFormsApplication1.KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.BEDARF_ERGEBNIS, "einheit");
        Assert.NotNull(zugang);

        KiFeldumsetzung umsetzung = KiFeldwandler.Wandle(zugang, Energieeinheit.KWh.Text);
        Assert.True(umsetzung.Ok, umsetzung.Grund);
        zugang.Setzen(umsetzung.Wert);
        cut.Render();

        Assert.Same(Energieeinheit.KWh, cut.Instance.Anzeigeeinheit);
    }
}
