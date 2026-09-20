using System.Globalization;
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
}
