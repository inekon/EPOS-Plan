using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Format und Vorschau des Lastgangimports (iU9-W12.2), Vorbild
/// <c>Views/Stromverbraucher/Form_GanglinieImportOptionen</c>.
///
/// <para>Soll ist die Feldkarte: acht Auswahllisten (die achte nur bei Excel),
/// der Kopfzeilenschalter, die Vorschautabelle und drei Fussknoepfe. Geprueft
/// werden ausserdem die beiden Regeln, die den Dialog ausmachen: Steuerwerte
/// sind PLAETZE, und die Vorschau ist NICHT reaktiv.</para>
/// </summary>
public class GanglinieImportOptionenDialogTests : BunitContext
{
    public GanglinieImportOptionenDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static GanglinienVorschau Erkennung(bool excel = false, int spalten = 2)
    {
        GanglinienVorschau v = new GanglinienVorschau
        {
            IstExcel = excel,
            Spaltenzahl = spalten,
            Lesbar = true
        };
        v.Vorschlag.Trennzeichen = ';';
        v.Vorschlag.Dezimaltrenner = ',';
        v.Vorschlag.Kopfzeile = true;
        v.Vorschlag.WertSpalte = 1;
        v.Vorschlag.ZeitSpalte = 0;
        v.Zeilen.Add(new[] { "Zeitstempel", "Leistung kW" });
        v.Zeilen.Add(new[] { "01.01.2023 00:00", "220,00" });
        v.Zeilen.Add(new[] { "01.01.2023 01:00", "232,23" });
        if (excel) { v.Blaetter.Add("Lastgang"); v.Blaetter.Add("Notizen"); }
        return v;
    }

    private IRenderedComponent<GanglinieImportOptionenDialog> Zeige(
        GanglinienVorschau? erkennung = null,
        Func<string, GanglinienImportOptionen, Task<GanglinienVorschau?>>? vorschau = null,
        Action<GanglinienImportOptionen?>? geschlossen = null)
    {
        return Render<GanglinieImportOptionenDialog>(p => p
            .Add(x => x.Pfad, @"C:\Daten\lastgang.csv")
            .Add(x => x.Erkennung, erkennung ?? Erkennung())
            .Add(x => x.Vorschau, vorschau)
            .Add(x => x.Geschlossen, (GanglinienImportOptionen? o) => geschlossen?.Invoke(o)));
    }

    private static IReadOnlyList<IElement> Listen(IRenderedComponent<GanglinieImportOptionenDialog> cut)
        => cut.FindAll(".epos-importoptionen-raster select");

    /// <summary>
    /// Ein Knopf der Fußleiste — sie läuft Aktualisieren(0) · Füller ·
    /// Abbrechen(1) · OK(2), die <c>SpeichernLeiste</c> des Hauses mit
    /// „Aktualisieren" im Aktionsschlitz.
    /// </summary>
    private static IElement Knopf(IRenderedComponent<GanglinieImportOptionenDialog> cut, int i)
        => cut.FindAll(".epos-leiste button")[i];

    // =====================================================================
    // Feldbestand
    // =====================================================================

    [Fact]
    public void Ohne_Excel_stehen_sieben_Auswahllisten()
    {
        var cut = Zeige();

        Assert.Equal(7, Listen(cut).Count);                        // ohne cbo_Blatt
        Assert.Single(cut.FindAll("input[type=checkbox]"));        // chk_Kopfzeile
        Assert.Equal(3, cut.FindAll(".epos-leiste button").Count); // Aktualisieren, Abbrechen, OK
    }

    /// <summary>
    /// Die Blattwahl haengt am Konstruktorparameter des Vorlaeufers und kann
    /// deshalb nicht im Designer stehen (<c>lbl_Blatt.Visible = istExcel</c>).
    /// </summary>
    [Fact]
    public void Bei_einer_Excelmappe_kommt_die_achte_Liste_dazu()
    {
        var cut = Zeige(Erkennung(excel: true));

        Assert.Equal(8, Listen(cut).Count);
        Assert.Equal(2, Listen(cut)[7].QuerySelectorAll("option").Length);   // zwei Blaetter
    }

    [Fact]
    public void Der_Kopf_nennt_den_Dateinamen()
    {
        var cut = Zeige();

        Assert.Contains("lastgang.csv", cut.Find(".epos-importoptionen-datei").TextContent);
    }

    [Fact]
    public void Die_Vorschau_zeigt_die_Zeilen_der_Erkennung()
    {
        var cut = Zeige();

        Assert.Equal(3, cut.FindAll(".epos-raster tbody tr").Count);
        // Zeile, Spalte 1, Spalte 2
        Assert.Equal(3, cut.FindAll(".epos-raster thead th").Count);
    }

    /// <summary>Ist die Kopfzeile gesetzt, steht die erste Zeile grau (<c>GrayText</c>).</summary>
    [Fact]
    public void Die_Kopfzeile_der_Vorschau_wird_gekennzeichnet()
    {
        var cut = Zeige();
        var zeilen = cut.FindAll(".epos-raster tbody tr");

        Assert.Contains("epos-vorschau-kopf", zeilen[0].InnerHtml);
        Assert.DoesNotContain("epos-vorschau-kopf", zeilen[1].InnerHtml);
    }

    // =====================================================================
    // Steuerwerte sind Plaetze
    // =====================================================================

    [Fact]
    public void Die_Erkennung_belegt_die_Listen_vor()
    {
        var cut = Zeige();
        var listen = Listen(cut);

        Assert.Equal("0", listen[0].GetAttribute("value"));   // Semikolon
        Assert.Equal("0", listen[1].GetAttribute("value"));   // Dezimalkomma
        Assert.Equal("1", listen[2].GetAttribute("value"));   // Wertspalte = Spalte 2
        Assert.Equal("1", listen[3].GetAttribute("value"));   // Zeitspalte 0 + 1 ("(keine)" vorweg)
        Assert.True(cut.Find("input[type=checkbox]").HasAttribute("checked"));
    }

    /// <summary>
    /// Die Zeitspaltenliste beginnt mit „(keine)"; Platz 0 heisst deshalb
    /// <c>ZeitSpalte = -1</c>.
    /// </summary>
    [Fact]
    public void Platz_null_der_Zeitspalte_heisst_keine_Zeitspalte()
    {
        var cut = Zeige();
        Listen(cut)[3].Change("0");

        Assert.Equal(-1, cut.Instance.Optionen().ZeitSpalte);
    }

    [Fact]
    public void Die_gewaehlten_Plaetze_werden_wieder_zu_Werten()
    {
        var cut = Zeige();
        var listen = Listen(cut);

        listen[0].Change("2");   // Tabulator
        listen[1].Change("1");   // Punkt
        listen[4].Change("1");   // kWh je Intervall
        listen[5].Change("2");   // Viertelstunde
        listen[6].Change("2");   // Intervallende

        GanglinienImportOptionen o = cut.Instance.Optionen();
        Assert.Equal('\t', o.Trennzeichen);
        Assert.Equal('.', o.Dezimaltrenner);
        Assert.Equal(GanglinienEinheit.KilowattstundeJeIntervall, o.Einheit);
        Assert.Equal(GanglinienRaster.Viertelstunde, o.Raster);
        Assert.Equal(IntervallKonvention.Ende, o.Konvention);
    }

    // =====================================================================
    // Vorschau aktualisieren
    // =====================================================================

    /// <summary>Ohne Rueckruf bleibt der Knopf gesperrt — dieselbe Regel wie bei der Dateiwahl.</summary>
    [Fact]
    public void Ohne_Vorschaurueckruf_ist_der_Aktualisierknopf_gesperrt()
    {
        Assert.True(Knopf(Zeige(), 0).HasAttribute("disabled"));
    }

    /// <summary>
    /// Der Knopf ruft die Neuzerlegung MIT den gewaehlten Optionen — und rettet sie
    /// danach, obwohl die Listen neu gefuellt werden (:300-302).
    /// </summary>
    [Fact]
    public void Aktualisieren_zerlegt_mit_den_gewaehlten_Optionen_und_behaelt_sie()
    {
        char gesehen = '?';
        var cut = Zeige(vorschau: (pfad, o) =>
        {
            gesehen = o.Trennzeichen;
            GanglinienVorschau neu = Erkennung(spalten: 3);
            neu.Vorschlag.Trennzeichen = ';';       // die Erkennung raet wieder Semikolon
            return Task.FromResult<GanglinienVorschau?>(neu);
        });

        Listen(cut)[0].Change("2");                 // Tabulator waehlen
        Knopf(cut, 0).Click();

        Assert.Equal('\t', gesehen);
        Assert.Equal('\t', cut.Instance.Optionen().Trennzeichen);   // gerettet
        Assert.Equal(4, cut.FindAll(".epos-raster thead th").Count); // drei Spalten + Zeilennummer
    }

    [Fact]
    public void Eine_unlesbare_Neuzerlegung_laesst_die_Vorschau_stehen()
    {
        var cut = Zeige(vorschau: (pfad, o) =>
            Task.FromResult<GanglinienVorschau?>(new GanglinienVorschau { Lesbar = false }));

        Knopf(cut, 0).Click();

        Assert.Equal(3, cut.FindAll(".epos-raster tbody tr").Count);
    }

    // =====================================================================
    // Schluss
    // =====================================================================

    /// <summary>
    /// <b>Befund W12-B16, woertlich behalten.</b> OK prueft nichts — auch nicht,
    /// ob Wert- und Zeitspalte auf demselben Platz stehen.
    /// </summary>
    [Fact]
    public void OK_meldet_die_Optionen_ohne_jede_Pruefung()
    {
        GanglinienImportOptionen? ergebnis = null;
        var cut = Zeige(geschlossen: o => ergebnis = o);

        Listen(cut)[2].Change("0");   // Wertspalte 1
        Listen(cut)[3].Change("1");   // Zeitspalte ebenfalls 1
        Knopf(cut, 2).Click();

        Assert.NotNull(ergebnis);
        Assert.Equal(0, ergebnis!.WertSpalte);
        Assert.Equal(0, ergebnis.ZeitSpalte);
    }

    [Fact]
    public void Abbrechen_meldet_null()
    {
        GanglinienImportOptionen? ergebnis = new();
        var cut = Zeige(geschlossen: o => ergebnis = o);

        Knopf(cut, 1).Click();
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Esc_meldet_ebenfalls_null()
    {
        GanglinienImportOptionen? ergebnis = new();
        var cut = Zeige(geschlossen: o => ergebnis = o);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(ergebnis);
    }

    /// <summary>Das Schliesskreuz im Kopf wirkt wie Esc/Abbrechen.</summary>
    [Fact]
    public void Kreuz_meldet_ebenfalls_null()
    {
        GanglinienImportOptionen? ergebnis = new();
        var cut = Zeige(geschlossen: o => ergebnis = o);

        cut.Find(".epos-dialog-zu").Click();
        Assert.Null(ergebnis);
    }

    // =====================================================================
    //  Formularraster (Anwenderwunsch iU8-E-2, Paket P3, 05.09.2026)
    // =====================================================================

    /// <summary>
    /// Die acht Formatlisten stehen im Formularraster; der Kasten <c>epos-importoptionen-raster</c> bleibt als Anker der Proben, das Ordnen macht der Raster.
    ///
    /// <para>Geprueft wird das MARKUP: Der Block traegt
    /// <c>epos-formularraster</c>, und darin stehen Felder. Was der Raster
    /// daraus MACHT (Beschriftungsspalte, kurzes Feld, zwei Spalten), steht
    /// als Stilblattprobe in <c>FormularrasterTests</c> - eine bunit-Probe
    /// rechnet kein CSS aus (Lehre W6-B-1).</para>
    /// </summary>
    [Fact]
    public void Die_Formatlisten_stehen_im_Formularraster()
    {
        var cut = Zeige();

        Assert.Single(cut.FindAll(".epos-importoptionen-raster .epos-formularraster"));
        Assert.NotEmpty(cut.FindAll(".epos-formularraster .epos-feld"));
    }

    // =====================================================================
    // Die Fussleiste nach der Hausregel (Konzept Knopfleisten, Abschnitt 5)
    // =====================================================================

    /// <summary>
    /// <b>Aktualisieren · Füller · Abbrechen · OK (primär, zuletzt)</b> — die eine
    /// <c>SpeichernLeiste</c> des Hauses. „Aktualisieren" wirkt auf den
    /// Eingabeblock und steht deshalb im Aktionsschlitz links vom Füller, nicht
    /// mehr zwischen den beiden Schlussknöpfen.
    /// </summary>
    [Fact]
    public void Die_Fussleiste_ist_die_SpeichernLeiste_mit_Aktionsschlitz()
    {
        var cut = Zeige();
        var leisten = cut.FindAll(".epos-leiste");
        IElement fuss = leisten[leisten.Count - 1];

        var knoepfe = fuss.QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToList();
        // Die Texte kommen aus den Ressourcen - diese Klasse pinnt keine Kultur.
        Assert.Equal(
            new[]
            {
                WindowsFormsApplication1.MyResource.Resource.IMPORT_BTN_AKTUALISIEREN,
                WindowsFormsApplication1.MyResource.Resource.IMPORT_BTN_ABBRECHEN,
                WindowsFormsApplication1.MyResource.Resource.SIM_BTN_OK
            },
            knoepfe);

        // Der Fueller ist die Statusspanne, und sie steht ZWISCHEN Aktualisieren
        // und Abbrechen.
        Assert.Single(fuss.QuerySelectorAll(".epos-status"));
        var kinder = fuss.Children.Select(e => e.ClassName ?? "").ToList();
        Assert.Equal(1, kinder.FindIndex(k => k.Contains("epos-status")));

        // Genau ein primaerer Knopf, und er steht zuletzt.
        var primaer = fuss.QuerySelectorAll("button.epos-knopf--primaer");
        Assert.Single(primaer);
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SIM_BTN_OK,
                     primaer[0].TextContent.Trim());
    }

    // =====================================================================
    // Erste Datenzeile und Zeilenzahl (IM-3)
    // =====================================================================

    /// <summary>Eine Stundenreihe „Nr;Wert“ mit <paramref name="kopf"/> Kopfzeilen und <paramref name="daten"/> Werten (Wert = Nummer + 0,5).</summary>
    private static string Probedatei(int kopf, int daten)
    {
        var zeilen = new List<string>();
        if (kopf >= 1) zeilen.Add("Zeitpunkt;Leistung");
        for (int k = 2; k <= kopf; k++) zeilen.Add("Einheit;kW");
        for (int i = 1; i <= daten; i++) zeilen.Add(i + ";" + i + ",5");
        string pfad = Path.Combine(Path.GetTempPath(), "epos-im3-" + Guid.NewGuid().ToString("N") + ".csv");
        File.WriteAllText(pfad, string.Join("\r\n", zeilen) + "\r\n");
        return pfad;
    }

    /// <summary>Der Dialog über einer echten Datei — Erkennung und Neuzerlegung über den Kern.</summary>
    private IRenderedComponent<GanglinieImportOptionenDialog> ZeigeDatei(
        string pfad, Action<GanglinienImportOptionen?>? geschlossen = null)
        => Render<GanglinieImportOptionenDialog>(p => p
            .Add(x => x.Pfad, pfad)
            .Add(x => x.Erkennung, GanglinienDatei.Erkenne(pfad))
            .Add(x => x.Vorschau, (Func<string, GanglinienImportOptionen, Task<GanglinienVorschau?>>)(
                (d, o) => Task.FromResult<GanglinienVorschau?>(GanglinienDatei.Vorschau(d, o))))
            .Add(x => x.Geschlossen, (GanglinienImportOptionen? o) => geschlossen?.Invoke(o)));

    /// <summary>Die zwei Zahlenfelder des Lesefensters: [0] erste Datenzeile, [1] Zeilenzahl.</summary>
    private static IReadOnlyList<IElement> Fensterfelder(IRenderedComponent<GanglinieImportOptionenDialog> cut)
        => cut.FindAll(".epos-importoptionen-raster input");

    [Fact]
    public void Erste_Datenzeile_und_Zeilenzahl_kommen_aus_der_Erkennung()
    {
        string pfad = Probedatei(2, 8760);
        try
        {
            var cut = ZeigeDatei(pfad);
            IReadOnlyList<IElement> felder = Fensterfelder(cut);
            Assert.Equal(2, felder.Count);
            Assert.Equal("3", felder[0].GetAttribute("value"));
            Assert.Equal("8760", felder[1].GetAttribute("value"));
            Assert.Equal(3, cut.Instance.ErsteDatenzeile);
            Assert.Equal(8760, cut.Instance.Zeilenzahl);

            // Die zwei Kopfzeilen stehen grau, die erste Datenzeile nicht.
            var zeilen = cut.FindAll(".epos-raster tbody tr");
            Assert.NotEmpty(zeilen[1].QuerySelectorAll(".epos-vorschau-kopf"));
            Assert.Empty(zeilen[2].QuerySelectorAll(".epos-vorschau-kopf"));

            // Unverändert gelesen wird bis zum Dateiende.
            GanglinienImportOptionen o = cut.Instance.Optionen();
            Assert.Equal(3, o.ErsteDatenzeile);
            Assert.Equal(0, o.Zeilenzahl);
        }
        finally { File.Delete(pfad); }
    }

    [Fact]
    public void Ein_geaenderter_Startwert_liest_ab_dieser_Zeile()
    {
        string pfad = Probedatei(1, 8770);
        GanglinienImportOptionen? ergebnis = null;
        try
        {
            var cut = ZeigeDatei(pfad, o => ergebnis = o);
            Assert.Equal(2, cut.Instance.ErsteDatenzeile);
            Assert.Equal(8770, cut.Instance.Zeilenzahl);

            Fensterfelder(cut)[0].Input("7");
            Fensterfelder(cut)[1].Input("8760");
            Knopf(cut, 0).Click();

            GanglinienProbe? probe = cut.Instance.Probe;
            Assert.NotNull(probe);
            Assert.True(probe!.Erfolgreich, probe.Grund);
            Assert.Equal(8760, probe.Datenzeilen);
            Assert.Equal(6.5, probe.WerteKw[0], 9);
            Assert.Equal(7, cut.Instance.ErsteDatenzeile);
            Assert.Equal(8760, cut.Instance.Zeilenzahl);

            Knopf(cut, 2).Click();
            Assert.NotNull(ergebnis);
            Assert.Equal(7, ergebnis!.ErsteDatenzeile);
            Assert.Equal(8760, ergebnis.Zeilenzahl);
            Assert.True(ergebnis.Kopfzeile);
        }
        finally { File.Delete(pfad); }
    }

    [Fact]
    public void Zu_wenig_Zeilen_ab_der_ersten_Datenzeile_werden_benannt()
    {
        string pfad = Probedatei(1, 8770);
        try
        {
            var cut = ZeigeDatei(pfad);
            Fensterfelder(cut)[0].Input("20");
            Fensterfelder(cut)[1].Input("8760");
            Knopf(cut, 0).Click();

            GanglinienProbe? probe = cut.Instance.Probe;
            Assert.NotNull(probe);
            Assert.False(probe!.Erfolgreich);
            string zeile = cut.Find(".epos-importoptionen-probe").TextContent;
            Assert.Contains("8752", zeile);
            Assert.Contains("8760", zeile);
            Assert.Empty(cut.FindAll(".epos-importoptionen-bild"));
        }
        finally { File.Delete(pfad); }
    }

    [Fact]
    public void Der_Kopfzeilenschalter_und_die_erste_Datenzeile_gehen_zusammen()
    {
        var cut = Zeige();
        Assert.Equal(2, cut.Instance.ErsteDatenzeile);

        cut.Find("input[type=checkbox]").Change(false);
        Assert.Equal(1, cut.Instance.ErsteDatenzeile);
        Assert.False(cut.Instance.Optionen().Kopfzeile);

        Fensterfelder(cut)[0].Input("4");
        Assert.True(cut.Instance.Optionen().Kopfzeile);
        Assert.Equal(4, cut.Instance.Optionen().ErsteDatenzeile);
    }
}
