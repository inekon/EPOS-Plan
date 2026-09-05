using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Photovoltaik;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// PV-Modulimport aus CEC und PVsyst-PAN (iU9-W13.3). Soll ist die Feldkarte von
/// <c>Form_CECImport</c> (Klasse <c>Main_PV_Test</c>, 75 Steuerelemente: zehn
/// Gitterspalten, sechs Knöpfe, zwei Klapplisten, vier Zahlenfelder, drei Reiter
/// mit 21 Textfeldern).
///
/// <para>Die Kultur ist auf de-DE gepinnt: Die Erwartungswerte sind deutsche
/// Beschriftungen, und der Windows-Läufer läuft mit englischer Oberfläche.</para>
/// </summary>
public class PvModulImportDialogTests : BunitContext
{
    public PvModulImportDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>
    /// Die Sprache der Oberfläche wird auf de-DE gepinnt (Muster
    /// <c>DeutscheOberflaeche</c> aus <c>EPOS.Kern.Tests</c>).
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

    // =====================================================================
    // Prüfstand
    // =====================================================================

    /// <summary>Drei CEC-Module mit steigender Leistung.</summary>
    private static List<UnifiedModule> DreiModule()
    {
        return new List<UnifiedModule>
        {
            Cec("Ablytek 6MN6A270", "Ablytek", "Mono-c-Si", 270.643, 242.1, 1.627, 8.81, 30.72),
            Cec("Ablytek 6MN6A400", "Ablytek", "Mono-c-Si", 400.0, 360.0, 2.0, 10.0, 40.0),
            Cec("Trina TSM-650", "Trina Solar", "Multi-c-Si", 650.0, 600.0, 3.1, 17.27, 37.7)
        };
    }

    private static UnifiedModule Cec(string name, string firma, string technologie,
                                     double stc, double ptc, double flaeche,
                                     double imp, double vmp)
    {
        var roh = new PVModule
        {
            Database = "CEC",
            Name = name,
            Manufacturer = firma,
            Technology = technologie,
            Bifacial = "0",
            STC = stc,
            PTC = ptc,
            A_c = flaeche,
            Length = 1.64,
            Width = 0.992,
            I_sc_ref = 9.34,
            V_oc_ref = 38.63,
            I_mp_ref = imp,
            V_mp_ref = vmp,
            alpha_sc = 0.00486614,
            beta_oc = -0.121182,
            gamma_pmp = -0.4509,
            T_NOCT = 47.4,
            Date = 2024
        };
        return UnifiedModule.FromPanCec(roh);
    }

    /// <summary>Ein PAN-Modul — es führt keine Temperaturkoeffizienten und kein NOCT.</summary>
    private static UnifiedModule Pan()
    {
        var pan = new PanModule
        {
            Manufacturer = "Trina Solar",
            Model = "TSM-650DEG21C.20",
            Technol = "mtSiMono",
            PNom = 650,
            Isc = 18.35,
            Voc = 45.5,
            Imp = 17.27,
            Vmp = 37.7,
            muPmpReq = -0.34,
            Width = 1.303,
            Height = 2.384,
            BifacialityFactor = 0.70,
            YearBegin = 2020
        };
        var svc = new PanDataService();
        svc.Aufnehmen(pan);
        return UnifiedModule.FromPanCec(svc.AllModules[0]);
    }

    private IRenderedComponent<PvModulImportDialog> Bauen(
        List<UnifiedModule>? module = null,
        string quelle = "CEC",
        Func<UnifiedModule, Task<PvVorpruefung>>? vorpruefen = null,
        Func<UnifiedModule, string, Task<bool>>? anlegen = null,
        Func<UnifiedModule, int, Task<bool>>? ueberschreiben = null,
        Func<Task<string?>>? panWaehlen = null,
        Func<string, Task<PvLeseErgebnis>>? panLaden = null,
        Action<bool>? geschlossen = null)
    {
        return Render<PvModulImportDialog>(p => p
            .Add(x => x.Quelle, quelle)
            .Add(x => x.CecLaden, (Func<IProgress<CecFortschritt>, CancellationToken, Task<PvLeseErgebnis>>)
                ((_, __) => Task.FromResult(new PvLeseErgebnis(
                    true, module ?? new List<UnifiedModule>(),
                    new CecFortschritt("CEC_MSG_GELADEN", "3")))))
            .Add(x => x.PanWaehlen, panWaehlen)
            .Add(x => x.PanLaden, panLaden)
            .Add(x => x.Vorpruefen, vorpruefen)
            .Add(x => x.Anlegen, anlegen)
            .Add(x => x.Ueberschreiben, ueberschreiben)
            .Add(x => x.Meldungstext, (Func<CecFortschritt, string>)Uebersetzen)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));
    }

    private static string Uebersetzen(CecFortschritt f)
    {
        string vorlage = WindowsFormsApplication1.MyResource.Resource
            .ResourceManager.GetString(f.Schluessel ?? "") ?? f.Schluessel ?? "";
        return f.Werte.Length == 0
            ? vorlage
            : string.Format(CultureInfo.CurrentCulture, vorlage, f.Werte);
    }

    private static IElement Knopf(IRenderedComponent<PvModulImportDialog> cut, string teil)
        => cut.FindAll("button").First(b => b.TextContent.Contains(teil));

    private static void CecLaden(IRenderedComponent<PvModulImportDialog> cut)
        => Knopf(cut, "CEC laden").Click();

    // =====================================================================
    // 1 — Feldbestand
    // =====================================================================

    /// <summary>
    /// Die zehn Gitterspalten, die drei Reiter, die beiden Quellenknöpfe und
    /// die Filterleiste — wörtlich die Feldkarte des Vorläufers.
    /// </summary>
    [Fact]
    public void Die_Maske_zeigt_ihre_Spalten_Reiter_und_Filter()
    {
        var cut = Bauen(DreiModule(), panWaehlen: () => Task.FromResult<string?>(""));
        CecLaden(cut);

        Assert.Equal("Photovoltaik Module Import", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Import - CEC und PAN Module", cut.Markup);

        string kopf = cut.Find("thead").TextContent;
        foreach (string spalte in new[]
                 { "Quelle", "Modulname", "Hersteller", "Technologie", "Pmp (W)",
                   "Effizienz (%)", "Isc [A]", "Bifazial", "Voc [V]", "Jahr" })
            Assert.Contains(spalte, kopf);

        Assert.Contains("📋 Übersicht", cut.Markup);
        Assert.Contains("⚡ Elektrisch", cut.Markup);
        Assert.Contains("🌡 Thermisch", cut.Markup);

        Assert.Contains("🌐 CEC laden", cut.Markup);
        Assert.Contains("🌿 PAN laden", cut.Markup);
        Assert.Contains("✖ Zurücksetzen", cut.Markup);
        Assert.Contains("✔ Auswahl übernehmen", cut.Markup);
    }

    /// <summary>
    /// Die Vorbelegung der vier Zahlenfelder ist wörtlich die des Vorläufers:
    /// <c>Nud(num_PMin, 0, 999, 0, 2)</c>, <c>Nud(num_PMax, 0, 999, 999, 2)</c>,
    /// <c>Nud(num_EffMin, 0, 100, 0, 2)</c>, <c>Nud(num_EffMax, 0, 100, 50, 2)</c>.
    /// </summary>
    [Fact]
    public void Die_Filtervorbelegung_ist_die_des_Vorlaeufers()
    {
        var cut = Bauen(DreiModule());

        var felder = cut.FindAll(".epos-pvimport-filter input[inputmode]");
        Assert.Equal("0,00", felder[0].GetAttribute("value"));
        Assert.Equal("999,00", felder[1].GetAttribute("value"));
        Assert.Equal("0,00", felder[2].GetAttribute("value"));
        Assert.Equal("50,00", felder[3].GetAttribute("value"));
    }

    /// <summary>Ohne Dateiwähler bleibt der PAN-Knopf weg — kein Delegat, kein Knopf.</summary>
    [Fact]
    public void Ohne_Dateiwaehler_gibt_es_den_PAN_Knopf_nicht()
    {
        Assert.DoesNotContain("PAN laden", Bauen(DreiModule()).Markup);
        Assert.Contains("PAN laden",
            Bauen(DreiModule(), panWaehlen: () => Task.FromResult<string?>("")).Markup);
    }

    /// <summary>Die Statuszeile des Vorläufers, wörtlich.</summary>
    [Fact]
    public void Die_Statuszeile_meldet_bereit_und_dann_die_Trefferzahl()
    {
        var cut = Bauen(DreiModule());

        Assert.Contains("Bereit. Bitte CEC Datenbank oder PAN Datei laden.", cut.Markup);

        CecLaden(cut);

        Assert.Contains("Filter Auswahl (3 Module gefunden)", cut.Markup);
    }

    // =====================================================================
    // 2 — Filter
    // =====================================================================

    [Fact]
    public void Nach_dem_Laden_stehen_die_Module_im_Gitter()
    {
        var cut = Bauen(DreiModule());

        CecLaden(cut);

        Assert.Equal(3, cut.Instance.SichtbareZeilen);
        Assert.Contains("Ablytek 6MN6A270", cut.Find("tbody").TextContent);
        Assert.Contains("Trina TSM-650", cut.Find("tbody").TextContent);
    }

    /// <summary>
    /// <b>„(alle)" ist ein STEUERWERT, kein Anzeigetext</b> (Befund W13-B39,
    /// Abweichung A-22): Der Vorläufer verglich gegen die Zeichenkette „(alle)";
    /// eine Übersetzung hätte den Filter still zerrissen. Hier ist es der
    /// Listenplatz 0.
    /// </summary>
    [Fact]
    public void Der_Herstellerfilter_laeuft_ueber_den_Listenplatz()
    {
        var cut = Bauen(DreiModule());
        CecLaden(cut);

        var liste = cut.FindAll(".epos-pvimport-filter select")[0];
        Assert.Equal("(alle)", liste.QuerySelectorAll("option")[0].TextContent);
        Assert.Equal(3, liste.QuerySelectorAll("option").Length);   // (alle) + zwei Hersteller

        liste.Change("2");                                          // "Trina Solar"
        Assert.Equal(1, cut.Instance.SichtbareZeilen);
        Assert.Contains("Trina TSM-650", cut.Find("tbody").TextContent);

        cut.FindAll(".epos-pvimport-filter select")[0].Change("0"); // wieder alle
        Assert.Equal(3, cut.Instance.SichtbareZeilen);
    }

    /// <summary>
    /// Die Suche ist eine PLATZHALTERSUCHE über <c>Suchmuster</c> — der Kern
    /// führt sie seit W9; der Vorläufer brachte eine dritte Fassung mit
    /// (Befund W13-B41, Abweichung A-23).
    /// </summary>
    [Fact]
    public void Die_Suche_kennt_Platzhalter()
    {
        var cut = Bauen(DreiModule());
        CecLaden(cut);

        var suche = cut.FindAll(".epos-pvimport-filter input[type='text']")[0];

        suche.Input("Ablytek*");
        Assert.Equal(2, cut.Instance.SichtbareZeilen);

        cut.FindAll(".epos-pvimport-filter input[type='text']")[0].Input("*650*");
        Assert.Equal(1, cut.Instance.SichtbareZeilen);

        // Ohne Platzhalter ist es eine Teilsuche.
        cut.FindAll(".epos-pvimport-filter input[type='text']")[0].Input("6MN");
        Assert.Equal(2, cut.Instance.SichtbareZeilen);
    }

    /// <summary>
    /// Der Leistungsfilter rechnet mit <c>I_mp · V_mp</c>, und eine Obergrenze
    /// von 0 zählt als „keine Obergrenze" — wörtlich <c>ApplyFilter</c> :228.
    /// </summary>
    [Fact]
    public void Der_Leistungsfilter_rechnet_mit_Imp_mal_Vmp()
    {
        var cut = Bauen(DreiModule());
        CecLaden(cut);

        // 8,81 * 30,72 = 270,6 | 10 * 40 = 400 | 17,27 * 37,7 = 651,1
        var felder = cut.FindAll(".epos-pvimport-filter input[inputmode]");
        felder[0].Input("300");
        Assert.Equal(2, cut.Instance.SichtbareZeilen);

        cut.FindAll(".epos-pvimport-filter input[inputmode]")[1].Input("500");
        Assert.Equal(1, cut.Instance.SichtbareZeilen);

        // Obergrenze 0 heisst "keine Obergrenze".
        cut.FindAll(".epos-pvimport-filter input[inputmode]")[1].Input("0");
        Assert.Equal(2, cut.Instance.SichtbareZeilen);
    }

    [Fact]
    public void Zuruecksetzen_stellt_die_Vorbelegung_wieder_her()
    {
        var cut = Bauen(DreiModule());
        CecLaden(cut);

        cut.FindAll(".epos-pvimport-filter input[type='text']")[0].Input("Trina*");
        Assert.Equal(1, cut.Instance.SichtbareZeilen);

        Knopf(cut, "Zurücksetzen").Click();

        Assert.Equal(3, cut.Instance.SichtbareZeilen);
        var felder = cut.FindAll(".epos-pvimport-filter input[inputmode]");
        Assert.Equal("999,00", felder[1].GetAttribute("value"));
    }

    // =====================================================================
    // 3 — Die 21 Detailfelder
    // =====================================================================

    /// <summary>
    /// Ein Klick füllt die drei Reiter. Die Quellenweiche liegt seit W13.0j am
    /// Modell und nicht mehr als dreizehn Ternäre im Anzeigecode (Befund
    /// W13-B43 und die 13 Ternäre aus <c>ShowDetail</c>).
    /// </summary>
    [Fact]
    public void Ein_Klick_fuellt_die_Detailfelder()
    {
        var cut = Bauen(DreiModule());
        CecLaden(cut);

        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();

        Assert.NotNull(cut.Instance.Gewaehlt);
        Assert.Equal("Ablytek 6MN6A270", cut.Instance.Gewaehlt!.Name);

        string uebersicht = cut.Find(".epos-formularraster").TextContent;
        Assert.Contains("Modulname:", uebersicht);
        Assert.Contains("Fläche [m²]:", uebersicht);

        var felder = cut.FindAll(".epos-formularraster input");
        Assert.Equal("Ablytek 6MN6A270", felder[0].GetAttribute("value"));
        Assert.Equal("Ablytek", felder[1].GetAttribute("value"));
        Assert.Equal("1,63", felder[6].GetAttribute("value"));    // Flaeche A_c
    }

    /// <summary>
    /// <b>Ein PAN-Modul zeigt „-" für die Werte, die es nicht führt</b> — wörtlich
    /// wie <c>ShowDetail</c> :425‑427 und :438. Die PTC-Leistung wird dagegen
    /// GESCHÄTZT (Befund W13-B43).
    /// </summary>
    [Fact]
    public void Ein_PAN_Modul_zeigt_Strich_wo_es_nichts_fuehrt()
    {
        var cut = Bauen(new List<UnifiedModule> { Pan() });
        CecLaden(cut);
        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();

        // Reiter "Elektrisch" oeffnen
        cut.FindAll("[role='tab']")[1].Click();

        var felder = cut.FindAll(".epos-formularraster input");
        Assert.Equal("-", felder[5].GetAttribute("value"));    // alpha_Isc
        Assert.Equal("-", felder[6].GetAttribute("value"));    // beta_Voc
        Assert.Equal("-0,3400", felder[7].GetAttribute("value"));  // gamma_pmp = muPmpReq
        Assert.Equal("650,00", felder[8].GetAttribute("value"));   // STC = PNom
        Assert.Equal("605,80", felder[9].GetAttribute("value"));   // PTC geschaetzt

        cut.FindAll("[role='tab']")[2].Click();
        Assert.Equal("-", cut.FindAll(".epos-formularraster input")[0].GetAttribute("value"));
    }

    /// <summary>
    /// Der Bifazialtext wird in der OBERFLÄCHE gebildet — im Kern stand er als
    /// „Ja (0,70)" bzw. „Nein" (Befund W13-B50, Abweichung A-18).
    /// </summary>
    [Fact]
    public void Der_Bifazialtext_entsteht_in_der_Oberflaeche()
    {
        var cut = Bauen(DreiModule());
        CecLaden(cut);
        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();

        Assert.Contains("Nein", cut.Find("tbody").TextContent);
        var felder = cut.FindAll(".epos-formularraster input");
        Assert.Equal("Nein", felder[5].GetAttribute("value"));
    }

    // =====================================================================
    // 4 — Übernehmen
    // =====================================================================

    [Fact]
    public void Ohne_Auswahl_meldet_die_Uebernahme_sich()
    {
        var cut = Bauen(DreiModule(), vorpruefen: _ => Task.FromResult(Vorpruefung(ImportBefund.Neu)));
        CecLaden(cut);

        Knopf(cut, "Auswahl übernehmen").Click();

        Assert.Equal("Bitte ein PV-Modul selektieren!", cut.Instance.Meldung);
    }

    /// <summary>
    /// Ein neuer Bezeichner geht ohne Konfliktdialog durch und meldet sich —
    /// der Dialog bleibt OFFEN, weil er ein Katalogfenster ist, aus dem der
    /// Anwender mehrere Module nacheinander übernimmt.
    /// </summary>
    [Fact]
    public void Ein_neues_Modul_wird_ohne_Rueckfrage_angelegt()
    {
        string? angelegt = null;
        var cut = Bauen(DreiModule(),
            vorpruefen: _ => Task.FromResult(Vorpruefung(ImportBefund.Neu)),
            anlegen: (_, name) => { angelegt = name; return Task.FromResult(true); });

        CecLaden(cut);
        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        Knopf(cut, "Auswahl übernehmen").Click();

        Assert.Equal("Ablytek 6MN6A270", angelegt);
        Assert.Equal("Datensatz erfolgreich gespeichert.", cut.Instance.Meldung);
        Assert.Empty(cut.FindAll("[role='dialog']"));
    }

    /// <summary>
    /// Ein Namenskonflikt öffnet den Konfliktdialog als ÜBERLAGERUNG — kein
    /// zweites Fenster (Risiko R2).
    /// </summary>
    [Fact]
    public void Ein_Namenskonflikt_oeffnet_die_Ueberlagerung()
    {
        var cut = Bauen(DreiModule(),
            vorpruefen: _ => Task.FromResult(Vorpruefung(ImportBefund.NameVorhanden)),
            ueberschreiben: (_, __) => Task.FromResult(true));

        CecLaden(cut);
        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        Knopf(cut, "Auswahl übernehmen").Click();

        Assert.Single(cut.FindAll("[role='dialog']"));
        Assert.Contains("Import: Konflikte prüfen", cut.Markup);
    }

    [Fact]
    public void Ein_Schreibfehler_wird_gemeldet()
    {
        var cut = Bauen(DreiModule(),
            vorpruefen: _ => Task.FromResult(Vorpruefung(ImportBefund.Neu)),
            anlegen: (_, __) => Task.FromResult(false));

        CecLaden(cut);
        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        Knopf(cut, "Auswahl übernehmen").Click();

        Assert.Equal("Fehler beim Speichern des Datensatzes!", cut.Instance.Meldung);
    }

    // =====================================================================
    // 5 — PAN und Tastatur
    // =====================================================================

    /// <summary>
    /// „PAN laden" wählt eine Datei und nimmt sie in die Sitzungsliste auf.
    /// Der Vorläufer verwarf das Ergebnis von <c>ParsePan</c> und verließ sich
    /// auf eine STATISCHE Nebenwirkung (Befunde W13-B45 und B46).
    /// </summary>
    [Fact]
    public void PAN_laden_nimmt_die_Datei_auf()
    {
        string? gelesen = null;
        var cut = Bauen(new List<UnifiedModule>(),
            panWaehlen: () => Task.FromResult<string?>(@"D:\module\trina.pan"),
            panLaden: pfad =>
            {
                gelesen = pfad;
                return Task.FromResult(new PvLeseErgebnis(
                    true, new List<UnifiedModule> { Pan() },
                    new CecFortschritt("PAN_MSG_GELESEN", "1")));
            });

        Knopf(cut, "PAN laden").Click();

        Assert.Equal(@"D:\module\trina.pan", gelesen);
        Assert.Equal(1, cut.Instance.SichtbareZeilen);
        Assert.Contains("PAN", cut.Find("tbody").TextContent);
    }

    [Fact]
    public void Ein_Lesefehler_erscheint_als_Warnbanner()
    {
        var cut = Bauen(new List<UnifiedModule>(),
            panWaehlen: () => Task.FromResult<string?>(@"D:\module\kaputt.pan"),
            panLaden: _ => Task.FromResult(new PvLeseErgebnis(
                false, null, new CecFortschritt("PAN_MSG_LESEFEHLER", "Zugriff verweigert"))));

        Knopf(cut, "PAN laden").Click();

        Assert.Contains("Zugriff verweigert", cut.Instance.Meldung);
        Assert.NotEmpty(cut.FindAll("[role='alert']"));
    }

    [Fact]
    public void Esc_schliesst_den_Dialog()
    {
        bool? ergebnis = null;
        var cut = Bauen(DreiModule(), geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.False(ergebnis);
    }

    /// <summary>
    /// Der Rückgabewert sagt, ob etwas geschrieben wurde. Der Vorläufer setzte
    /// nie ein <c>DialogResult</c>, und seine beiden Aufrufer werteten nichts aus.
    /// </summary>
    [Fact]
    public void Der_Rueckgabewert_sagt_ob_geschrieben_wurde()
    {
        bool? ergebnis = null;
        var cut = Bauen(DreiModule(),
            vorpruefen: _ => Task.FromResult(Vorpruefung(ImportBefund.Neu)),
            anlegen: (_, __) => Task.FromResult(true),
            geschlossen: b => ergebnis = b);

        CecLaden(cut);
        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        Knopf(cut, "Auswahl übernehmen").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.True(ergebnis);
    }

    // =====================================================================
    // Hilfen
    // =====================================================================

    private static PvVorpruefung Vorpruefung(ImportBefund befund)
    {
        var pruefungen = new List<ImportPruefung>
        {
            new() { Kandidat = new ImportKandidat { Name = "Ablytek 6MN6A270", Tag = null },
                    Befund = befund,
                    Vorhanden = befund == ImportBefund.Neu
                        ? null
                        : new KatalogSatz { Id = 5, Name = "Ablytek 6MN6A270" } }
        };
        return new PvVorpruefung(befund, pruefungen, new[] { "ablytek 6mn6a270" });
    }

    // =====================================================================
    //  Formularraster (Anwenderwunsch iU8-E-2, Paket P3, 05.09.2026)
    // =====================================================================

    /// <summary>
    /// Die drei Detailreiter stehen im Formularraster; der handgebaute Kasten <c>epos-pvimport-details</c> ist fort. Die zwei FILTERLEISTEN ueber dem Gitter bleiben Leisten - sie sind kein Formularblock.
    ///
    /// <para>Geprueft wird das MARKUP: Der Block traegt
    /// <c>epos-formularraster</c>, und darin stehen Felder. Was der Raster
    /// daraus MACHT (Beschriftungsspalte, kurzes Feld, zwei Spalten), steht
    /// als Stilblattprobe in <c>FormularrasterTests</c> - eine bunit-Probe
    /// rechnet kein CSS aus (Lehre W6-B-1).</para>
    /// </summary>
    [Fact]
    public void Die_Detailfelder_stehen_im_Formularraster()
    {
        var cut = Bauen(DreiModule());

        Assert.NotEmpty(cut.FindAll(".epos-formularraster .epos-feld"));

        // Die Filterleiste ist KEIN Raster geworden.
        Assert.NotEmpty(cut.FindAll(".epos-pvimport-filter"));
        Assert.Empty(cut.FindAll(".epos-pvimport-filter .epos-formularraster"));
    }
}
