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
/// Der WÄRMEBEDARF EINES GEBÄUDES (iU9-W9.8, Anwenderwunsch <b>W9‑E‑2</b> vom
/// 05.09.2026) — die Überlagerung hinter dem Knopf „Simulation…" des Gebäudedialogs.
///
/// <para><b>Was geprüft wird:</b> die drei Kennzahlen, die Einheitenwahl (W8‑O‑5), der
/// Schalter „sortiert" samt seiner Wirkung auf Bildauftrag und Achsenart — und
/// dass es weder einen Brauchwasser- noch einen Gesamt-Schalter gibt: der Anwender hat
/// beides ausdrücklich abbestellt.</para>
///
/// <para>Die Ganglinie steht seit der Etappe DG-E3, Gruppe (a), als
/// <c>DiagrammSvg</c> im Baum; der Zoom ist die viewBox der Zeichenfläche und
/// kostet keinen Rundlauf mehr in den Kern.</para>
///
/// <para>Die Kultur ist auf de-DE gepinnt — die Erwartungswerte sind deutsche
/// Zahlen und Beschriftungen.</para>
/// </summary>
public class GebaeudeBedarfDialogTests : EposBunitContext
{
    public GebaeudeBedarfDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Ein Bildauftrag, wie ihn die Komponente stellt.</summary>
    private sealed record Auftrag(bool Sortiert);

    /// <summary>
    /// Die zwei Zeichenmodelle — Ganglinie und Dauerlinie, je EINE Instanz. Der
    /// Baustein <c>DiagrammSvg</c> vergleicht die Referenz; ein je Zeichenlauf neu
    /// gebautes Modell verwürfe mit dem Baum auch Zoom und abgewählte Reihen.
    /// </summary>
    private static readonly Zeichenmodell GANG = Ganglinie(false);
    private static readonly Zeichenmodell DAUER = Ganglinie(true);

    /// <summary>
    /// Eine kurze, echte Ganglinie (eine Woche) — die Fälle prüfen die Bedienung,
    /// nicht die Rechenzeit eines Jahres.
    /// </summary>
    private static Zeichenmodell Ganglinie(bool sortiert)
    {
        var werte = new double[168];
        for (int i = 0; i < werte.Length; i++)
            werte[i] = 20.0 + 10.0 * Math.Sin(2 * Math.PI * i / 24.0);
        if (sortiert) Array.Sort(werte, (a, b) => b.CompareTo(a));

        return ChartRenderer.GanglinieNormiertModell(
            "Wärmelast Jahresganglinie",
            new[] { new ChartRenderer.Reihe("Wärmebedarf", werte, ChartRenderer.C_BEDARF) },
            "kW", ChartRenderer.Achse.Jahresstunden, sortiert);
    }

    private static GebaeudeBedarfDaten Daten(double mwh = 52.84, double kw = 31.5,
                                             double? vbh = 1677.0)
    {
        var monate = new double[12];
        for (int m = 0; m < 12; m++) monate[m] = mwh / 12.0;

        return new GebaeudeBedarfDaten
        {
            Name = "EFH-A-TS-212",
            HeizwaermeMwh = mwh,
            MaxLastKw = kw,
            VollbenutzungsstundenH = vbh,
            MonatswerteMwh = monate
        };
    }

    private IRenderedComponent<GebaeudeBedarfDialog> Aufbauen(
        GebaeudeBedarfDaten? daten = null,
        List<Auftrag>? auftraege = null,
        Energieeinheit? einheit = null,
        Action<Energieeinheit>? einheitGewaehlt = null,
        Action<bool>? geschlossen = null,
        string titel = "Wärmebedarf Gebäude")
    {
        List<Auftrag> liste = auftraege ?? new List<Auftrag>();

        return Render<GebaeudeBedarfDialog>(p => p
            .Add(x => x.Daten, daten ?? Daten())
            .Add(x => x.TitelText, titel)
            .Add(x => x.Bildauftrag, sortiert =>
            {
                liste.Add(new Auftrag(sortiert));
                return sortiert ? DAUER : GANG;
            })
            .Add(x => x.Einheit, einheit ?? Energieeinheit.MWh)
            .Add(x => x.EinheitGewaehlt, einheitGewaehlt)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));
    }

    // =================================================================================
    // Feldbestand
    // =================================================================================

    /// <summary>
    /// Die drei Kennzahlen, der Gebäudename, das Bild und die Monatsübersicht — mehr
    /// nicht.
    /// </summary>
    [Fact]
    public void Der_Feldbestand_steht()
    {
        var cut = Aufbauen();

        Assert.Contains("Wärmebedarf Gebäude", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("EFH-A-TS-212", cut.Find(".epos-kontextzeile").TextContent);

        Assert.Contains("Wärmebedarf Heizung:", cut.Markup);
        Assert.Contains("max. Wärmelast", cut.Markup);
        Assert.Contains("Vollbenutzungsstunden:", cut.Markup);

        Assert.Contains("52,84", cut.Markup);      // MWh, deutsche Anzeige
        Assert.Contains("31,50", cut.Markup);      // kW
        Assert.Contains("1677,00", cut.Markup);    // h/a

        Assert.NotNull(cut.Find("svg.epos-flaeche"));
        Assert.Contains("Januar:", cut.Markup);
        Assert.Contains("Dezember:", cut.Markup);
    }

    /// <summary>
    /// <b>Die Ganglinie steht als SVG im Baum</b> (Etappe DG-E3, Gruppe (a)) — unter
    /// der Kennung <c>gebaeude-bedarf</c> und mit der Einheit <c>kW</c>.
    /// </summary>
    [Fact]
    public void Die_Ganglinie_steht_als_DiagrammSvg()
    {
        var cut = Aufbauen();

        Assert.Single(cut.FindComponents<DiagrammSvg>());
        Assert.Equal("gebaeude-bedarf", cut.FindComponent<DiagrammSvg>().Instance.Kennung);
        Assert.Equal("kW", cut.FindComponent<DiagrammSvg>().Instance.Einheit);
    }

    /// <summary>
    /// <b>Der Nachtrag des Anwenders</b> („ohne Brauchwasser und ohne gesamt"): Der
    /// Dialog zeigt GENAU EINE Reihe. Weder der Schalter „Gesamt" noch ein
    /// Bedarfsartschalter der Ergebnisseite steht hier.
    /// </summary>
    [Fact]
    public void Es_gibt_weder_Brauchwasser_noch_Gesamt()
    {
        var cut = Aufbauen();

        Assert.DoesNotContain("Brauchwasser", cut.Markup);
        Assert.DoesNotContain("Gesamt", cut.Markup);
        Assert.DoesNotContain("Prozess", cut.Markup);

        // Genau EIN Schalter: „sortiert".
        Assert.Single(cut.FindAll("input[type=checkbox]"));
        Assert.Contains("sortiert", cut.Markup);
    }

    /// <summary>Ohne Höchstlast gibt es keine Vollbenutzungsstunden, sondern „—".</summary>
    [Fact]
    public void Ohne_Hoechstlast_steht_ein_Strich()
    {
        var cut = Aufbauen(Daten(mwh: 0, kw: 0, vbh: null));

        Assert.Contains("—", cut.Markup);
    }

    /// <summary>Ohne Monatswerte bleibt die Monatstabelle weg.</summary>
    [Fact]
    public void Ohne_Monatswerte_gibt_es_keine_Monatstabelle()
    {
        var cut = Render<GebaeudeBedarfDialog>(p => p
            .Add(x => x.Daten, new GebaeudeBedarfDaten { Name = "X", HeizwaermeMwh = 1 }));

        Assert.DoesNotContain("Januar:", cut.Markup);
    }

    /// <summary>Ohne Delegat kein Bild — der Platzhalter steht.</summary>
    [Fact]
    public void Ohne_Bildauftrag_steht_der_Platzhalter()
    {
        var cut = Render<GebaeudeBedarfDialog>(p => p.Add(x => x.Daten, Daten()));

        Assert.Empty(cut.FindAll("svg.epos-flaeche"));
        Assert.Single(cut.FindAll(".epos-chartbild-platzhalter"));
        Assert.Contains("Kein Diagramm vorhanden", cut.Markup);
    }

    // =================================================================================
    // Der Schalter „sortiert"
    // =================================================================================

    /// <summary>
    /// Der Schalter lässt NEU ZEICHNEN: Der zweite Auftrag trägt <c>Sortiert = true</c>.
    /// Das ist die Dauerlinie — dieselbe Umschaltung wie im Bedarfsreiter der
    /// Ergebnisseite.
    /// </summary>
    [Fact]
    public void Der_Schalter_sortiert_laesst_neu_zeichnen()
    {
        var auftraege = new List<Auftrag>();
        var cut = Aufbauen(auftraege: auftraege);

        Assert.Contains(auftraege, a => !a.Sortiert);
        Assert.False(cut.Instance.Sortiert);

        cut.Find("input[type=checkbox]").Change(true);

        Assert.True(cut.Instance.Sortiert);
        Assert.Contains(auftraege, a => a.Sortiert);
    }

    // =================================================================================
    // Die Einheit (W8-O-5)
    // =================================================================================

    /// <summary>
    /// MWh ist die Vorgabe, kWh ist wählbar — und die Wahl geht an die Hülle zurück,
    /// damit sie gemerkt wird (dieselbe Ablage wie im Bedarfsergebnisdialog).
    /// </summary>
    [Fact]
    public void Die_Einheit_ist_waehlbar_und_wird_gemeldet()
    {
        Energieeinheit? gemeldet = null;
        var cut = Aufbauen(einheitGewaehlt: e => gemeldet = e);

        Assert.Same(Energieeinheit.MWh, cut.Instance.Anzeigeeinheit);
        Assert.Contains("52,84", cut.Markup);

        cut.Find("select").Change("1");   // kWh

        Assert.Same(Energieeinheit.KWh, cut.Instance.Anzeigeeinheit);
        Assert.Same(Energieeinheit.KWh, gemeldet);

        // 52,84 MWh sind 52 840 kWh, in kWh-Formatierung ohne Nachkommastellen.
        Assert.Contains("52840", cut.Markup.Replace(".", "").Replace(" ", ""));
    }

    /// <summary>
    /// Die LEISTUNG folgt der Einheitenwahl nicht — sie steht in kW, egal was gewählt
    /// ist (Hausregel: eine Energiemenge wird umgerechnet, eine Leistung nicht).
    /// </summary>
    [Fact]
    public void Die_Hoechstlast_bleibt_in_kW()
    {
        var cut = Aufbauen();

        cut.Find("select").Change("1");   // kWh

        Assert.Contains("31,50", cut.Markup);
        Assert.Contains("kW", cut.Markup);
    }

    // =================================================================================
    // Tastatur und Schlussleiste
    // =================================================================================

    [Fact]
    public void OK_schliesst()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        cut.FindAll("button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.True(ergebnis);
    }

    [Theory]
    [InlineData("Escape")]
    [InlineData("Enter")]
    public void Esc_und_Enter_schliessen(string taste)
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = taste });

        Assert.True(ergebnis);
    }

    /// <summary>Das Kreuz im Dialogkopf wirkt wie Esc/Enter: schließt mit <c>true</c>.</summary>
    [Fact]
    public void Kreuz_schliesst_wie_Esc()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog-zu").Click();

        Assert.True(ergebnis);
    }

    /// <summary>Titel-bedingter Kopf: ohne Titel zeigt der Kopf weder Titel noch Kreuz.</summary>
    [Fact]
    public void Ohne_Titel_zeigt_der_Kopf_weder_Titel_noch_Kreuz()
    {
        var cut = Aufbauen(titel: "");

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
        // Der Hilfeknopf bleibt - er haengt nicht am Titel.
        Assert.NotEmpty(cut.FindAll(".epos-dialog-kopf"));
    }

    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F3)
    // =====================================================================

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Sie bindet über die
    /// Sichtklasse <c>GebaeudeBedarfKiSicht</c>: Die Brücke liest die Jahressumme aus
    /// dem eingefrorenen Satz und legt den Schalter „sortiert" um.
    /// </summary>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_schaltet_die_Dauerlinie()
    {
        var cut = Aufbauen();

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.GEBAEUDE_BEDARF));

        WindowsFormsApplication1.KiFeldzugang summe =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.GEBAEUDE_BEDARF, "heizwaerme");
        Assert.NotNull(summe);
        Assert.False(summe.Setzbar);

        WindowsFormsApplication1.KiFeldzugang sortiert =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.GEBAEUDE_BEDARF, "sortiert");
        Assert.Equal(false, sortiert.Lesen());

        sortiert.Setzen(true);
        cut.Render();

        Assert.True(cut.Instance.Sortiert);
    }

    /// <summary>
    /// <b>Die Anzeigeeinheit ist ein WAHLFELD</b> (KI-D-Q6): Gesetzt wird über ihren
    /// Text; danach rechnet die Maske ihre Zahlen in dieser Einheit.
    /// </summary>
    [Fact]
    public void Der_Assistent_waehlt_die_Anzeigeeinheit_ueber_ihren_Text()
    {
        var cut = Aufbauen();

        WindowsFormsApplication1.KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.GEBAEUDE_BEDARF, "einheit");
        Assert.NotNull(zugang);

        KiFeldumsetzung umsetzung = KiFeldwandler.Wandle(zugang, Energieeinheit.KWh.Text);
        Assert.True(umsetzung.Ok, umsetzung.Grund);
        zugang.Setzen(umsetzung.Wert);
        cut.Render();

        Assert.Same(Energieeinheit.KWh, cut.Instance.Anzeigeeinheit);
    }

    // =================================================================================
    // Stufe G1 (ADR-006, Umsetzungskonzept 2.7): der Ausweis des Rechenwegs
    // =================================================================================

    [Fact]
    public void Der_Rechenweg_steht_bei_den_Kennzahlen()
    {
        GebaeudeBedarfDaten daten = new()
        {
            Name = "EFH", HeizwaermeMwh = 50, MaxLastKw = 30, VollbenutzungsstundenH = 1666,
            MonatswerteMwh = new double[12], Modelltext = "Tagesbilanz (Bestandsweg)"
        };
        var cut = Aufbauen(daten);

        Assert.Contains("Rechenweg:", cut.Markup);
        Assert.Contains("Tagesbilanz (Bestandsweg)", cut.Markup);
    }

    [Fact]
    public void Ohne_Rechenweg_steht_keine_Zeile()
    {
        var cut = Aufbauen();

        Assert.DoesNotContain("Rechenweg:", cut.Markup);
    }

    // =================================================================================
    // Stufe G2 (Konzept 8.2, Umsetzungskonzept 2.7): Kennzahlen, Vergleich, Raumtemperatur
    // =================================================================================

    /// <summary>Die Grenze der Kältezahl (K5) — der eine Text der Kälteseite.</summary>
    private static readonly string FEUCHTE = SimulationKaeltebedarf.GrenzeFeuchte;

    /// <summary>Zwölf Monatswerte der Kühlreihe [MWh] — Sommer mit Kälte.</summary>
    private static readonly double[] KUEHLMONATE =
        { 0, 0, 0, 0, 0.05, 0.2, 0.4, 0.4, 0.2, 0, 0, 0 };

    /// <summary>Ein Satz auf dem VDI-Weg mit dem Tagesbilanz-Weg als Vergleich.</summary>
    private static GebaeudeBedarfDaten VdiSatz(bool mitVergleich = true) => new()
    {
        Name = "EFH", HeizwaermeMwh = 60.0, MaxLastKw = 40.0, VollbenutzungsstundenH = 1500.0,
        MonatswerteMwh = new double[12], Modelltext = "VDI 6007", IstVdi6007 = true,
        SpitzeTagesmittelKw = 25.0, SpitzeQuantil95Kw = 20.0,
        KuehlenergieMwh = 1.25, KuehlstundenH = 312, MittlereRaumtemperaturC = 21.4,
        UeberhitzungsstundenH = 150, SommerlueftungsstundenH = 420,
        KaelteAbschnitt = true, KaeltelastMaxKw = 2.5, VollbenutzungsstundenKaelteH = 500.0,
        StundenHeizenUndKuehlenH = 7, KuehlMonatswerteMwh = KUEHLMONATE,
        KaelteHerleitung = new[] { "Gekühlt auf 26,0 °C, Kühlleistungsgrenze unbegrenzt.", FEUCHTE },
        Vergleich = mitVergleich
            ? new GebaeudeBedarfDaten
            {
                Name = "EFH", HeizwaermeMwh = 50.0, MaxLastKw = 32.0, VollbenutzungsstundenH = 1562.5,
                MonatswerteMwh = new double[12], Modelltext = "Tagesbilanz (Bestandsweg)",
                SpitzeTagesmittelKw = 20.0, SpitzeQuantil95Kw = 16.0
            }
            : null
    };

    [Fact]
    public void Auf_dem_VDI_Weg_stehen_die_neuen_Kennzahlen()
    {
        var cut = Aufbauen(VdiSatz());

        Assert.Contains("Kühlbedarf:", cut.Markup);
        Assert.DoesNotContain("(informativ)", cut.Markup);
        Assert.Contains("1,25", cut.Markup);                    // MWh
        Assert.Contains("Stunden mit Kühlbedarf:", cut.Markup);
        Assert.Contains("312", cut.Markup);
        Assert.Contains("mittlere Raumtemperatur (Nutzungszeit):", cut.Markup);
        Assert.Contains("21,40", cut.Markup);
        Assert.Contains("Überhitzungsstunden:", cut.Markup);
        Assert.Contains("Stunden mit Sommerlüftung:", cut.Markup);
        Assert.Equal(2, cut.FindAll("tr.gebb-vdi").Count);
        Assert.Equal(6, cut.FindAll("tr.gebb-kaelte").Count);
    }

    [Fact]
    public void Der_Kuehlbedarf_folgt_der_Einheitenwahl()
    {
        var cut = Aufbauen(VdiSatz(), einheit: Energieeinheit.KWh);

        Assert.Contains("1250", cut.Markup.Replace(".", ""));
    }

    [Fact]
    public void Auf_dem_Tagesbilanz_Weg_fehlen_die_VDI_Kennzahlen()
    {
        var cut = Aufbauen();

        Assert.Empty(cut.FindAll("tr.gebb-vdi"));
        Assert.DoesNotContain("Kühlbedarf", cut.Markup);
    }

    [Fact]
    public void Der_Vergleich_alt_neu_zeigt_vier_Spalten_und_fuenf_Zeilen()
    {
        var cut = Aufbauen(VdiSatz());

        var tabelle = cut.Find("table.gebb-vergleich");
        var koepfe = tabelle.QuerySelectorAll("th").Select(k => k.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "Kennzahl", "Tagesbilanz", "VDI 6007", "Abweichung" }, koepfe);
        Assert.Equal(5, tabelle.QuerySelectorAll("tbody tr").Length);

        // Heizwärme 50 → 60 MWh: +20,0 %; Spitze 32 → 40 kW: +25,0 %; Tagesmittel 20 → 25: +25,0 %.
        var erste = tabelle.QuerySelectorAll("tbody tr")[0].QuerySelectorAll("td").Select(z => z.TextContent.Trim()).ToArray();
        Assert.Equal("Wärmebedarf Heizung [MWh]", erste[0]);
        Assert.Equal("50,00", erste[1]);
        Assert.Equal("60,00", erste[2]);
        Assert.Equal("+20,0 %", erste[3]);
        Assert.Contains("+25,0 %", tabelle.QuerySelectorAll("tbody tr")[1].TextContent);
        Assert.Contains("Spitzenlast (Tagesmittel) [kW]", tabelle.QuerySelectorAll("tbody tr")[2].TextContent);
        Assert.Contains("-4,0 %", tabelle.QuerySelectorAll("tbody tr")[4].TextContent);   // 1562,5 → 1500 h/a
    }

    [Fact]
    public void Auf_dem_Tagesbilanz_Weg_steht_der_Vergleich_in_derselben_Spaltenordnung()
    {
        GebaeudeBedarfDaten vdi = VdiSatz(false);
        GebaeudeBedarfDaten alt = new()
        {
            Name = "EFH", HeizwaermeMwh = 50.0, MaxLastKw = 32.0, VollbenutzungsstundenH = 1562.5,
            MonatswerteMwh = new double[12], Modelltext = "Tagesbilanz (Bestandsweg)", Vergleich = vdi
        };
        var cut = Aufbauen(alt);

        var zellen = cut.Find("table.gebb-vergleich tbody tr").QuerySelectorAll("td").Select(z => z.TextContent.Trim()).ToArray();
        Assert.Equal("50,00", zellen[1]);                       // Tagesbilanz links, auch wenn sie der Hauptsatz ist
        Assert.Equal("60,00", zellen[2]);
    }

    [Fact]
    public void Ohne_Vergleich_steht_keine_Vergleichstabelle()
    {
        var cut = Aufbauen(VdiSatz(mitVergleich: false));

        Assert.Empty(cut.FindAll("table.gebb-vergleich"));
    }

    [Fact]
    public void Das_Bild_Raumtemperatur_steht_nur_mit_Delegat_und_wird_einmal_gerechnet()
    {
        int aufrufe = 0;
        var luft = new double[168];
        for (int i = 0; i < luft.Length; i++) luft[i] = 21.0 + Math.Sin(i / 12.0);
        Zeichenmodell raum = ChartRenderer.RaumtemperaturModell("Raumtemperatur", luft, luft, null, 26.0,
                                                                new ChartRenderer.Raumtemperaturnamen());
        var cut = Render<GebaeudeBedarfDialog>(p => p
            .Add(x => x.Daten, VdiSatz())
            .Add(x => x.Bildauftrag, s => s ? DAUER : GANG)
            .Add(x => x.BildauftragRaumtemperatur, () => { aufrufe++; return raum; }));

        Assert.Equal(2, cut.FindAll("svg.epos-flaeche").Count);
        cut.Render();
        Assert.Equal(1, aufrufe);

        var ohne = Aufbauen(VdiSatz());
        Assert.Single(ohne.FindAll("svg.epos-flaeche"));
    }

    // =================================================================================
    // Stufe KU1 — der Abschnitt „Kältebedarf" (Kühlkonzept 8.4, 8.6 Maske 3; E21, F-K18, K5)
    // =================================================================================

    /// <summary>Das Bild der Kältelast (eine Woche, Sommerform).</summary>
    private static Zeichenmodell Kaeltebild(bool sortiert)
    {
        var werte = new double[168];
        for (int i = 0; i < werte.Length; i++) werte[i] = Math.Max(0.0, 3.0 * Math.Sin(2 * Math.PI * i / 24.0));
        return ChartRenderer.GanglinieNormiertModell(
            "Kältelast Jahresganglinie",
            new[] { new ChartRenderer.Reihe("Kältelast", werte, ChartRenderer.C_BEDARF) },
            "kW", ChartRenderer.Achse.Jahresstunden, sortiert);
    }

    private static readonly Zeichenmodell KAELTE_GANG = Kaeltebild(false);
    private static readonly Zeichenmodell KAELTE_DAUER = Kaeltebild(true);

    private IRenderedComponent<GebaeudeBedarfDialog> MitKaeltebild(GebaeudeBedarfDaten daten, List<bool> auftraege)
        => Render<GebaeudeBedarfDialog>(p => p
            .Add(x => x.Daten, daten)
            .Add(x => x.Bildauftrag, s => s ? DAUER : GANG)
            .Add(x => x.BildauftragKaelte, s => { auftraege.Add(s); return s ? KAELTE_DAUER : KAELTE_GANG; }));

    /// <summary>Die Zeilentexte (erste Zelle) einer Kennzahltabelle.</summary>
    private static List<string> Zeilentexte(IRenderedComponent<GebaeudeBedarfDialog> cut, string seite)
        => cut.FindAll($"section[data-seite={seite}] table.gebb-kennzahlen tr")
              .Select(z => z.QuerySelector("td")!.TextContent.Trim()).ToList();

    /// <summary>
    /// <b>Symmetrie (E21; 8.6, dritter Zusatzfall)</b>: Der Abschnitt „Kältebedarf" führt
    /// dieselben Bausteine in derselben Folge wie die Wärmeseite — Kennzahltabelle mit Energie,
    /// Leistung und Vollbenutzungsstunden an denselben Plätzen, ein EIGENES Bild, die
    /// Monatswerte als zweite Spalte. Verglichen wird die BAUSTEINFOLGE, nicht die Zahlen; was
    /// eine Seite zusätzlich hat, steht hier benannt (sonst fällt der Fall).
    /// </summary>
    [Fact]
    public void Der_Abschnitt_Kaeltebedarf_fuehrt_dieselben_Bausteine_wie_die_Waermeseite()
    {
        var cut = MitKaeltebild(VdiSatz(), new List<bool>());

        List<string> waerme = Zeilentexte(cut, "waerme");
        List<string> kaelte = Zeilentexte(cut, "kaelte");

        // Energie, Leistung, Vollbenutzungsstunden - dieselben Plätze.
        Assert.Equal(new[] { "Wärmebedarf Heizung:", "max. Wärmelast", "Vollbenutzungsstunden:" }, waerme.Take(3));
        Assert.Equal(new[] { "Kühlbedarf:", "max. Kältelast", "Vollbenutzungsstunden:" }, kaelte.Take(3));

        // Die benannten Abweichungen: Rechenweg und Raumkennzahlen gehören dem Gebäude, die
        // drei Stundenzahlen der Kälteseite sind Zugewinne (Kühlkonzept 6.4).
        Assert.Equal(new[] { "Rechenweg:", "mittlere Raumtemperatur (Nutzungszeit):", "Stunden mit Sommerlüftung:" },
                     waerme.Skip(3));
        Assert.Equal(new[] { "Stunden mit Kühlbedarf:", "Überhitzungsstunden:", "Stunden mit Heizen und Kühlen:" },
                     kaelte.Skip(3));

        // Je Seite ein eigenes Bild - die Kälte nicht im Wärmebild.
        List<string> bilder = cut.FindComponents<DiagrammSvg>().Select(d => d.Instance.Kennung).ToList();
        Assert.Contains("gebaeude-bedarf", bilder);
        Assert.Contains("gebaeude-kaelte", bilder);
        Assert.NotNull(cut.Find("section[data-seite=kaelte] svg.epos-flaeche"));

        // Monatswerte: zwei Spalten, Heizung und Kühlung, nebeneinander (nicht verrechnet).
        Assert.Equal(new[] { "", "Heizung", "Kühlung", "" },
                     cut.FindAll("table.gebb-monate thead th").Select(th => th.TextContent.Trim()));
        Assert.Equal(12, cut.FindAll("td.gebb-kaelte-monat").Count);
        Assert.Equal("0,40", cut.FindAll("td.gebb-kaelte-monat")[6].TextContent.Trim());
    }

    /// <summary>
    /// K5: Die Grenze der Zahl steht als Satz NEBEN den Kältezahlen, im Abschnitt selbst — nicht
    /// als Fußnote; darüber die Zeile, wie der Kältebedarf entsteht.
    /// </summary>
    [Fact]
    public void Die_Feuchtegrenze_steht_im_Abschnitt_Kaeltebedarf()
    {
        var cut = Aufbauen(VdiSatz());

        string abschnitt = cut.Find("section[data-seite=kaelte]").TextContent;
        Assert.Contains(FEUCHTE, abschnitt);
        Assert.Contains("Gekühlt auf 26,0 °C", abschnitt);
        Assert.DoesNotContain(FEUCHTE, cut.Find("section[data-seite=waerme]").TextContent);
    }

    /// <summary>Der eine Schalter „sortiert" gilt auch für das Kältebild — es wird je Stellung einmal gerechnet.</summary>
    [Fact]
    public void Der_Schalter_sortiert_gilt_auch_fuer_das_Kaeltebild()
    {
        var auftraege = new List<bool>();
        var cut = MitKaeltebild(VdiSatz(), auftraege);
        Assert.Equal(new[] { false }, auftraege);

        cut.Find("input[type=checkbox]").Change(true);
        Assert.Equal(new[] { false, true }, auftraege);

        cut.Find("input[type=checkbox]").Change(false);
        Assert.Equal(new[] { false, true }, auftraege);   // zwischengespeichert, nicht neu gerechnet
    }

    /// <summary>
    /// <b>Bestandsweg-Gebäude (E20; 8.6, zweiter Zusatzfall)</b>: Der Abschnitt „Kältebedarf"
    /// zeigt 0 MIT Hinweis — nicht „—" und keine leere Gruppe. Hier ist die 0 eine Aussage, und
    /// der Hinweis sagt, wessen. Der Fall steht in der Löschliste der Stufe GA.
    /// </summary>
    [Fact]
    public void Ein_Bestandsweg_Gebaeude_zeigt_Kaeltebedarf_0_mit_Hinweis()
    {
        const string HINWEIS = "Gebäude „EFH“: Tagesbilanz (Bestandsweg) liefert keine Kühllast — Kältebedarf 0.";
        var daten = new GebaeudeBedarfDaten
        {
            Name = "EFH", HeizwaermeMwh = 50.0, MaxLastKw = 32.0, VollbenutzungsstundenH = 1562.5,
            MonatswerteMwh = new double[12], Modelltext = "Tagesbilanz (Bestandsweg)",
            KaelteAbschnitt = true, KuehlenergieMwh = 0.0, KaeltelastMaxKw = 0.0, KuehlstundenH = 0,
            KuehlMonatswerteMwh = new double[12],
            KaelteHerleitung = new[] { HINWEIS, FEUCHTE }
        };
        var cut = Aufbauen(daten);

        IElement abschnitt = cut.Find("section[data-seite=kaelte]");
        Assert.Contains(HINWEIS, abschnitt.TextContent);
        Assert.Equal(3, cut.FindAll("tr.gebb-kaelte").Count);
        foreach (IElement zelle in abschnitt.QuerySelectorAll("td.epos-zahl"))
            Assert.NotEqual("—", zelle.TextContent.Trim());
        Assert.Contains("0,00", abschnitt.TextContent);
        Assert.Empty(abschnitt.QuerySelectorAll("svg.epos-flaeche"));
    }

    /// <summary>Ohne Abschnitt (kein Satz, keine Gaben) steht weder die Gruppe noch die Kühlspalte da.</summary>
    [Fact]
    public void Ohne_Kaelteabschnitt_steht_keine_Kaeltegruppe_und_keine_Kuehlspalte()
    {
        var cut = Aufbauen();

        Assert.Empty(cut.FindAll("section[data-seite=kaelte]"));
        Assert.Empty(cut.FindAll("td.gebb-kaelte-monat"));
        Assert.Empty(cut.FindAll("table.gebb-monate thead"));
    }

    // =================================================================================
    // Anlagenkopplung AK1 Welle 3 (Konzept 9.4, 9.6 Maske 5) — Kacheln, Bild, Ausweis
    // =================================================================================

    /// <summary>Ein gekoppelt gerechneter Satz: Vorlauf 35,3 °C, Rücklauf 32,06 °C, 210,9 h begrenzt.</summary>
    private static GebaeudeBedarfDaten GekoppelterSatz(bool mitHeizstunden = true) => new()
    {
        Name = "Gebäude A", HeizwaermeMwh = 70.67, MaxLastKw = 34.5, VollbenutzungsstundenH = 2048.0,
        MonatswerteMwh = new double[12], Modelltext = "VDI 6007, gekoppelt (AK1)", IstVdi6007 = true,
        IstGekoppelt = true,
        VorlaufMittelC = mitHeizstunden ? 35.3 : null,
        RuecklaufMittelC = mitHeizstunden ? 32.06 : null,
        UebergabeBegrenztStundenH = mitHeizstunden ? 210.9 : 0.0,
        Heizkreiszeile = "Mittel der Stunden mit Heizbetrieb — Radiator, Auslegung 55/45 °C"
    };

    /// <summary>Das Bild „Vorlauf und Rücklauf" einer Woche — die zweite Wochenhälfte ohne Heizbetrieb.</summary>
    private static Zeichenmodell Vorlaufbild()
    {
        var v = new double[168];
        var r = new double[168];
        for (int i = 0; i < 168; i++)
        {
            bool heizt = i < 100;
            v[i] = heizt ? 40.0 + Math.Sin(i / 12.0) : double.NaN;
            r[i] = heizt ? v[i] - 7.0 : double.NaN;
        }
        return ChartRenderer.VorlaufRuecklaufModell("Vorlauf und Rücklauf", v, r, 55.0, 45.0,
                                                    new ChartRenderer.VorlaufRuecklaufnamen());
    }

    private static string Kachel(IElement kachel, string teil)
        => kachel.QuerySelector(".epos-kennzahlkachel-" + teil)?.TextContent.Trim() ?? "";

    /// <summary>
    /// Ein gekoppelt gerechnetes Gebäude zeigt unter den Kennzahlen ZWEI Kacheln — die Mittel von
    /// Vorlauf und Rücklauf mit Übergabeart und Auslegungspunkt, die Stunden mit begrenzter
    /// Übergabe — und die Rechenwegzeile trägt den Ausweis „gekoppelt (AK1)".
    /// </summary>
    [Fact]
    public void Ein_gekoppeltes_Gebaeude_zeigt_zwei_Kacheln_und_den_Ausweis()
    {
        var cut = Aufbauen(GekoppelterSatz());

        IReadOnlyList<IElement> kacheln = cut.FindAll("div.gebb-heizkreis .epos-kennzahlkachel");
        Assert.Equal(2, kacheln.Count);
        Assert.Equal("Vorlauf / Rücklauf", Kachel(kacheln[0], "titel"));
        Assert.Equal("35,3 / 32,1 °C", Kachel(kacheln[0], "wert"));
        Assert.Equal("Mittel der Stunden mit Heizbetrieb — Radiator, Auslegung 55/45 °C", Kachel(kacheln[0], "quelle"));
        Assert.Equal("Stunden mit begrenzter Übergabe", Kachel(kacheln[1], "titel"));
        Assert.Equal("211 h", Kachel(kacheln[1], "wert"));
        Assert.Contains("VDI 6007, gekoppelt (AK1)", cut.Find("table.gebb-kennzahlen").TextContent);

        // Die Kacheln stehen im Abschnitt „Wärmebedarf", nicht bei der Kälte.
        Assert.NotNull(cut.Find("section[data-seite=waerme]").QuerySelector("div.gebb-heizkreis"));
    }

    /// <summary>Ohne Heizstunde steht der Strich statt einer erfundenen Zahl.</summary>
    [Fact]
    public void Ohne_Heizstunde_zeigt_die_Vorlaufkachel_den_Strich()
    {
        var cut = Aufbauen(GekoppelterSatz(mitHeizstunden: false));

        IReadOnlyList<IElement> kacheln = cut.FindAll("div.gebb-heizkreis .epos-kennzahlkachel");
        Assert.Equal("—", Kachel(kacheln[0], "wert"));
        Assert.Equal("0 h", Kachel(kacheln[1], "wert"));
    }

    /// <summary>Ein ungekoppeltes Gebäude (jeder Bestandsfall) zeigt weder Kacheln noch Vorlaufbild.</summary>
    [Fact]
    public void Ohne_Kopplung_stehen_keine_Kacheln_und_kein_Vorlaufbild()
    {
        var cut = Aufbauen(VdiSatz());

        Assert.Empty(cut.FindAll("div.gebb-heizkreis"));
        Assert.Empty(cut.FindAll(".epos-kennzahlkachel"));
        Assert.DoesNotContain("gekoppelt", cut.Find("table.gebb-kennzahlen").TextContent);
        Assert.DoesNotContain("Stunden ohne Heizbetrieb", cut.Markup);
    }

    /// <summary>
    /// Das Bild „Vorlauf und Rücklauf" steht nur mit Delegat, wird einmal gerechnet (Zoom bleibt
    /// stehen), trägt die Zeile zu den Lücken — und kein Pfad schreibt ein „NaN".
    /// </summary>
    [Fact]
    public void Das_Vorlaufbild_steht_nur_mit_Delegat_und_wird_einmal_gerechnet()
    {
        int aufrufe = 0;
        Zeichenmodell bild = Vorlaufbild();
        var cut = Render<GebaeudeBedarfDialog>(p => p
            .Add(x => x.Daten, GekoppelterSatz())
            .Add(x => x.Bildauftrag, s => s ? DAUER : GANG)
            .Add(x => x.BildauftragVorlauf, () => { aufrufe++; return bild; }));

        Assert.Equal(2, cut.FindAll("svg.epos-flaeche").Count);
        Assert.Contains("Stunden ohne Heizbetrieb bleiben im Bild leer", cut.Markup);
        Assert.DoesNotContain("NaN", cut.Markup);
        cut.Render();
        Assert.Equal(1, aufrufe);

        var ohne = Aufbauen(GekoppelterSatz());
        Assert.Single(ohne.FindAll("svg.epos-flaeche"));
    }

    // =================================================================================
    // Stufe G6b: die Zonen eines Mehrzonengebaeudes
    // =================================================================================

    /// <summary>Ein Satz mit zwei Zonen — Wohnen beheizt, Keller unbeheizt (A2).</summary>
    private static GebaeudeBedarfDaten MitZonen() => new()
    {
        Name = "Haus mit Keller",
        HeizwaermeMwh = 12.5,
        MaxLastKw = 8.0,
        MonatswerteMwh = new double[12],
        IstVdi6007 = true,
        Zonen = new[]
        {
            new GebaeudeBedarfZoneDaten { Name = "Wohnen", IstBeheizt = true, HeizwaermeMwh = 12.5, MaxLastKw = 8.0,
                                          MittlereRaumtemperaturC = 20.5, UeberhitzungsstundenH = 12 },
            new GebaeudeBedarfZoneDaten { Name = "Keller", IstBeheizt = false,
                                          MittlereRaumtemperaturC = 11.25, UeberhitzungsstundenH = 0 },
        }
    };

    /// <summary>
    /// <b>Je Zone eine Zeile, auch unbeheizt</b> (Stufe G6b, A2): Die unbeheizte Zone trägt keine
    /// Heizwärme und keine Last („—"), aber Temperatur und Überhitzungsstunden. Die Diagrammwahl
    /// stellt das Gebäude und jede Zone zur Wahl; für eine Zone holt der Dialog ihre eigenen Bilder
    /// über die Delegaten der Zonen, bei der unbeheizten nennt das Wärmelastbild den Grund. Der
    /// Assistent liest und setzt die Wahl über das Katalogfeld „diagramm".
    /// </summary>
    [Fact]
    public void Ein_Gebaeude_mit_Zonen_zeigt_je_Zone_eine_Zeile_und_waehlt_die_Bilder_je_Zone()
    {
        var lastauftraege = new List<(int, bool)>();
        var raumauftraege = new List<int>();
        var cut = Render<GebaeudeBedarfDialog>(p => p
            .Add(x => x.Daten, MitZonen())
            .Add(x => x.Bildauftrag, sortiert => sortiert ? DAUER : GANG)
            .Add(x => x.BildauftragRaumtemperatur, () => GANG)
            .Add(x => x.BildauftragZone, (k, sortiert) => { lastauftraege.Add((k, sortiert)); return k == 0 ? GANG : null; })
            .Add(x => x.BildauftragRaumtemperaturZone, k => { raumauftraege.Add(k); return DAUER; })
            .Add(x => x.Einheit, Energieeinheit.MWh));

        IReadOnlyList<IElement> zeilen = cut.FindAll("table.gebb-zonen tbody tr");
        Assert.Equal(2, zeilen.Count);
        string[] wohnen = zeilen[0].QuerySelectorAll("td").Select(z => z.TextContent.Trim()).ToArray();
        string[] keller = zeilen[1].QuerySelectorAll("td").Select(z => z.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "Wohnen", "ja", "12,50", "8,00", "20,50", "12" }, wohnen);
        Assert.Equal(new[] { "Keller", "nein", "—", "—", "11,25", "0" }, keller);
        Assert.Contains("gebb-zone-unbeheizt", zeilen[1].ClassName);
        Assert.Contains(cut.FindAll(".epos-herleitung"), h => h.TextContent.Contains("Summe der Zonen"));

        // Die Diagrammwahl: Gebaeude, Wohnen, Keller - vorgewaehlt das ganze Gebaeude.
        IElement wahl = cut.FindAll("select").Single(s => s.QuerySelectorAll("option").Any(o => o.TextContent == "Keller"));
        Assert.Equal(new[] { "das ganze Gebäude", "Wohnen", "Keller" },
                     wahl.QuerySelectorAll("option").Select(o => o.TextContent.Trim()));
        Assert.Equal(0, cut.Instance.Bildzone);
        Assert.Empty(lastauftraege);

        wahl.Change("2");                                              // Keller
        Assert.Equal(2, cut.Instance.Bildzone);
        Assert.Equal(new[] { (1, false) }, lastauftraege);
        Assert.Equal(new[] { 1 }, raumauftraege);
        Assert.Contains(cut.FindAll(".epos-chartbild-platzhalter"),
                        x => x.TextContent.Contains("Die Zone „Keller“ ist unbeheizt und hat keine Wärmelast."));

        // Der Assistent liest und setzt die Wahl.
        WindowsFormsApplication1.KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.GEBAEUDE_BEDARF, "diagramm");
        Assert.NotNull(zugang);
        Assert.Equal(2, zugang.Lesen());
        zugang.Setzen(1);
        cut.Render();
        Assert.Equal(1, cut.Instance.Bildzone);
        Assert.Contains((0, false), lastauftraege);
    }

    /// <summary>Ein Gebäude ohne Zonen zeigt weder Zonentabelle noch Diagrammwahl.</summary>
    [Fact]
    public void Ohne_Zonen_steht_weder_Tabelle_noch_Diagrammwahl()
    {
        var cut = Aufbauen();
        Assert.Empty(cut.FindAll("table.gebb-zonen"));
        Assert.Single(cut.FindAll("select"));                           // nur die Einheit
        Assert.Null(KiMaskenbruecke.Feldzugang(KiMaskennamen.GEBAEUDE_BEDARF, "diagramm").Lesen());
    }
}
