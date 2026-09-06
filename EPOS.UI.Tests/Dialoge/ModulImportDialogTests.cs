using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
/// Der EINE Geräteimport (Anwenderentscheid <b>W6‑O‑1</b> vom 06.09.2026) — beide
/// Ausprägungen in einer Prüfklasse, weil sie eine Komponente sind.
///
/// <para><b>Herkunft der Fälle.</b> Die Abschnitte 1 bis 5 sind die 17 bunit-Fälle des
/// abgelösten <c>PvModulImportDialog</c> (iU9‑W13.3, 594 Zeilen) — Soll ist weiterhin
/// die Feldkarte von <c>Form_CECImport</c> (Klasse <c>Main_PV_Test</c>, 75
/// Steuerelemente: zehn Gitterspalten, sechs Knöpfe, zwei Klapplisten, vier
/// Zahlenfelder, drei Reiter mit 21 Textfeldern). Abschnitt 6 sind die Fälle des
/// abgelösten <c>WechselrichterImportDialog</c> (W6‑E‑2/S1.5), Abschnitt 7 der neue
/// OND-Zweig und Abschnitt 8 der Dateiweg der Auslieferungsliste (W6‑O‑3).</para>
///
/// <para>Die Kultur ist auf de-DE gepinnt: Die Erwartungswerte sind deutsche
/// Beschriftungen, und der Windows-Läufer läuft mit englischer Oberfläche.</para>
/// </summary>
public class ModulImportDialogTests : BunitContext
{
    public ModulImportDialogTests()
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
    private static List<object> DreiModule()
    {
        return new List<object>
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

    /// <summary>Ein Gerät der CEC-Wechselrichterliste.</summary>
    private static CecWechselrichter Geraet(string name, double paco, double pdco = 0)
    {
        return new CecWechselrichter
        {
            Name = name,
            Paco = paco,
            Pdco = pdco > 0 ? pdco : paco * 1.05,
            Pso = paco * 0.006,
            Vdco = 340,
            C0 = -8.0e-06,
            Pnt = 0.1,
            Vdcmax = 600,
            Idcmax = 12,
            MpptLow = 100,
            MpptHigh = 480,
            CecDatum = "2024-01-01"
        };
    }

    private static List<object> DreiGeraete() => new List<object>
    {
        Geraet("Alpha AG: A-3000", 3000),
        Geraet("Alpha AG: A-5000", 5000),
        Geraet("Beta GmbH: B-10000", 10000)
    };

    /// <summary>Das Muster 2500TL aus Anhang A des Konzepts, als OND-Satz.</summary>
    private static OndWechselrichter Ond()
    {
        return OndWechselrichterDienst.Zerlege(
            File.ReadAllText(Probe("ond_muster_2500tl.ond"), AnsiEncoding.Get()),
            "ond_muster_2500tl.ond");
    }

    /// <summary>
    /// Die Importprobe unter <c>Referenzlaeufe/Importproben</c> — dasselbe
    /// Aufwärtssuchen wie in <c>KatalogImportTests</c>.
    /// </summary>
    private static string Probe(string name)
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
        {
            string kandidat = Path.Combine(d.FullName, "Referenzlaeufe", "Importproben", name);
            if (File.Exists(kandidat)) return kandidat;
        }

        Assert.Fail("Die Importprobe " + name + " wurde nicht gefunden.");
        return "";
    }

    private IRenderedComponent<ModulImportDialog> Bauen(
        ModulImportArt art = ModulImportArt.Photovoltaik,
        List<object>? saetze = null,
        string quelle = "CEC",
        Func<object, Task<ImportVorpruefung>>? vorpruefen = null,
        Func<object, string, Task<bool>>? anlegen = null,
        Func<object, int, Task<bool>>? ueberschreiben = null,
        Func<ImportQuelle, Task<string?>>? dateiWaehlen = null,
        Func<ImportQuelle, string, Task<ImportLeseErgebnis>>? dateiLaden = null,
        Action<bool>? geschlossen = null)
    {
        var wege = new ModulImportWege
        {
            Netz = (_, __, ___) => Task.FromResult(new ImportLeseErgebnis(
                true, saetze ?? new List<object>(),
                new CecFortschritt("CEC_MSG_GELADEN", "3"))),
            DateiWaehlen = dateiWaehlen,
            DateiLaden = dateiLaden,
            Vorpruefen = vorpruefen,
            Anlegen = anlegen,
            Ueberschreiben = ueberschreiben,
            Meldungstext = Uebersetzen
        };

        return Render<ModulImportDialog>(p => p
            .Add(x => x.Art, art)
            .Add(x => x.Quelle, quelle)
            .Add(x => x.Wege, wege)
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

    private static IElement Knopf(IRenderedComponent<ModulImportDialog> cut, string teil)
        => cut.FindAll("button").First(b => b.TextContent.Contains(teil));

    private static void CecLaden(IRenderedComponent<ModulImportDialog> cut)
        => Knopf(cut, "CEC laden").Click();

    /// <summary>Der erste Quellenknopf der Leiste — der Netzabruf.</summary>
    private static void Laden(IRenderedComponent<ModulImportDialog> cut)
        => cut.Find(".epos-leiste .epos-knopf--primaer").Click();

    /// <summary>Der Knopf „Übernehmen" der Fußleiste.</summary>
    private static IElement Uebernehmen(IRenderedComponent<ModulImportDialog> cut)
        => cut.FindAll(".epos-leiste")[1].QuerySelectorAll("button")[0];

    // =====================================================================
    // 1 — Feldbestand
    // =====================================================================

    /// <summary>
    /// Die zehn Gitterspalten, die drei Reiter, die Quellenknöpfe und die Filterleiste —
    /// wörtlich die Feldkarte des Vorläufers, ergänzt um die dritte Quelle „CEC-Datei"
    /// (W6‑O‑3).
    /// </summary>
    [Fact]
    public void Die_Maske_zeigt_ihre_Spalten_Reiter_und_Filter()
    {
        var cut = Bauen(saetze: DreiModule(),
                        dateiWaehlen: _ => Task.FromResult<string?>(""),
                        dateiLaden: (_, __) => Task.FromResult(
                            new ImportLeseErgebnis(true, new List<object>(),
                                                   new CecFortschritt("PAN_MSG_GELESEN", "0"))));
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
        Assert.Contains("📄 CEC-Datei laden", cut.Markup);
        Assert.Contains("🌿 PAN laden", cut.Markup);
        Assert.Contains("✖ Zurücksetzen", cut.Markup);
        Assert.Contains("✔ Auswahl übernehmen", cut.Markup);
    }

    /// <summary>
    /// Die Vorbelegung der vier Zahlenfelder ist wörtlich die des Vorläufers:
    /// <c>Nud(num_PMin, 0, 999, 0, 2)</c>, <c>Nud(num_PMax, 0, 999, 999, 2)</c>,
    /// <c>Nud(num_EffMin, 0, 100, 0, 2)</c>, <c>Nud(num_EffMax, 0, 100, 50, 2)</c> —
    /// jetzt als DATEN im Profil (<c>ModulImportProfil.Zahlenfilter</c>).
    /// </summary>
    [Fact]
    public void Die_Filtervorbelegung_ist_die_des_Vorlaeufers()
    {
        var cut = Bauen(saetze: DreiModule());

        var felder = cut.FindAll(".epos-pvimport-filter input[inputmode]");
        Assert.Equal("0,00", felder[0].GetAttribute("value"));
        Assert.Equal("999,00", felder[1].GetAttribute("value"));
        Assert.Equal("0,00", felder[2].GetAttribute("value"));
        Assert.Equal("50,00", felder[3].GetAttribute("value"));
    }

    /// <summary>
    /// Ohne Dateiwähler bleiben die zwei DATEIQUELLEN weg — kein Delegat, kein Knopf.
    /// Der Netzabruf bleibt, er braucht keinen.
    /// </summary>
    [Fact]
    public void Ohne_Dateiwaehler_gibt_es_die_Dateiknoepfe_nicht()
    {
        string ohne = Bauen(saetze: DreiModule()).Markup;
        Assert.DoesNotContain("PAN laden", ohne);
        Assert.DoesNotContain("CEC-Datei laden", ohne);
        Assert.Contains("CEC laden", ohne);

        string mit = Bauen(saetze: DreiModule(),
                           dateiWaehlen: _ => Task.FromResult<string?>(""),
                           dateiLaden: (_, __) => Task.FromResult(
                               new ImportLeseErgebnis(true, new List<object>(),
                                                      new CecFortschritt("PAN_MSG_GELESEN", "0")))).Markup;
        Assert.Contains("PAN laden", mit);
        Assert.Contains("CEC-Datei laden", mit);
    }

    /// <summary>Die Statuszeile des Vorläufers, wörtlich.</summary>
    [Fact]
    public void Die_Statuszeile_meldet_bereit_und_dann_die_Trefferzahl()
    {
        var cut = Bauen(saetze: DreiModule());

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
        var cut = Bauen(saetze: DreiModule());

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
        var cut = Bauen(saetze: DreiModule());
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
        var cut = Bauen(saetze: DreiModule());
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
        var cut = Bauen(saetze: DreiModule());
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
        var cut = Bauen(saetze: DreiModule());
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
    /// W13-B43); seit W6‑O‑1 baut das PROFIL die Werte, und die Maske zeigt nur
    /// noch, was in der Zeile steht.
    /// </summary>
    [Fact]
    public void Ein_Klick_fuellt_die_Detailfelder()
    {
        var cut = Bauen(saetze: DreiModule());
        CecLaden(cut);

        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();

        Assert.NotNull(cut.Instance.Gewaehlt);
        Assert.Equal("Ablytek 6MN6A270", ((UnifiedModule)cut.Instance.Gewaehlt!).Name);

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
        var cut = Bauen(saetze: new List<object> { Pan() });
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
    /// Der Bifazialtext ist ein ÜBERSETZTER Text und kein deutsches Literal des
    /// Kerns (Befund W13-B50, Abweichung A-18). Er entsteht seit W6‑O‑1 im Profil —
    /// das die Hülle bzw. die Komponente mit dem Übersetzer baut, so dass er
    /// weiterhin in der Sprache der Oberfläche steht.
    /// </summary>
    [Fact]
    public void Der_Bifazialtext_steht_in_der_Sprache_der_Oberflaeche()
    {
        var cut = Bauen(saetze: DreiModule());
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
        var cut = Bauen(saetze: DreiModule(),
                        vorpruefen: _ => Task.FromResult(Vorpruefung(ImportBefund.Neu)));
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
        var cut = Bauen(saetze: DreiModule(),
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
        var cut = Bauen(saetze: DreiModule(),
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
        var cut = Bauen(saetze: DreiModule(),
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
        ImportQuelle? benutzt = null;

        var cut = Bauen(saetze: new List<object>(),
            dateiWaehlen: q => { benutzt = q; return Task.FromResult<string?>(@"D:\module\trina.pan"); },
            dateiLaden: (q, pfad) =>
            {
                gelesen = pfad;
                return Task.FromResult(new ImportLeseErgebnis(
                    true, new List<object> { Pan() },
                    new CecFortschritt("PAN_MSG_GELESEN", "1")));
            });

        Knopf(cut, "PAN laden").Click();

        Assert.Equal(@"D:\module\trina.pan", gelesen);
        Assert.Equal(ModulImportProfil.QuellePan, benutzt!.Schluessel);
        Assert.Equal("(*.pan)|*.pan", benutzt.Dateifilter);
        Assert.Equal("PAN", benutzt.Unterordner);
        Assert.Equal(1, cut.Instance.SichtbareZeilen);
        Assert.Contains("PAN", cut.Find("tbody").TextContent);
    }

    [Fact]
    public void Ein_Lesefehler_erscheint_als_Warnbanner()
    {
        var cut = Bauen(saetze: new List<object>(),
            dateiWaehlen: _ => Task.FromResult<string?>(@"D:\module\kaputt.pan"),
            dateiLaden: (_, __) => Task.FromResult(new ImportLeseErgebnis(
                false, null, new CecFortschritt("PAN_MSG_LESEFEHLER", "Zugriff verweigert"))));

        Knopf(cut, "PAN laden").Click();

        Assert.Contains("Zugriff verweigert", cut.Instance.Meldung);
        Assert.NotEmpty(cut.FindAll("[role='alert']"));
    }

    [Fact]
    public void Esc_schliesst_den_Dialog()
    {
        bool? ergebnis = null;
        var cut = Bauen(saetze: DreiModule(), geschlossen: b => ergebnis = b);

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
        var cut = Bauen(saetze: DreiModule(),
            vorpruefen: _ => Task.FromResult(Vorpruefung(ImportBefund.Neu)),
            anlegen: (_, __) => Task.FromResult(true),
            geschlossen: b => ergebnis = b);

        CecLaden(cut);
        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        Knopf(cut, "Auswahl übernehmen").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.True(ergebnis);
    }

    /// <summary>
    /// Die drei Detailreiter stehen im Formularraster; der handgebaute Kasten
    /// <c>epos-pvimport-details</c> ist fort. Die zwei FILTERLEISTEN über dem Gitter
    /// bleiben Leisten — sie sind kein Formularblock.
    ///
    /// <para>Geprüft wird das MARKUP: Der Block trägt <c>epos-formularraster</c>, und
    /// darin stehen Felder. Was der Raster daraus MACHT, steht als Stilblattprobe in
    /// <c>FormularrasterTests</c> — eine bunit-Probe rechnet kein CSS aus
    /// (Lehre W6‑B‑1).</para>
    /// </summary>
    [Fact]
    public void Die_Detailfelder_stehen_im_Formularraster()
    {
        var cut = Bauen(saetze: DreiModule());

        Assert.NotEmpty(cut.FindAll(".epos-formularraster .epos-feld"));

        // Die Filterleiste ist KEIN Raster geworden.
        Assert.NotEmpty(cut.FindAll(".epos-pvimport-filter"));
        Assert.Empty(cut.FindAll(".epos-pvimport-filter .epos-formularraster"));
    }

    // =====================================================================
    // 6 — Die zweite Ausprägung: Wechselrichter aus der CEC-Liste
    // =====================================================================

    /// <summary>
    /// Vor dem Laden ist das Raster leer; „CEC-Liste laden" füllt es und meldet die
    /// Zahl der Treffer. Der Titel nennt seit dem OND-Zweig beide Quellen.
    /// </summary>
    [Fact]
    public void Der_Import_laedt_die_Liste_in_das_Raster()
    {
        var cut = Bauen(ModulImportArt.Wechselrichter, DreiGeraete());

        Assert.Equal("Wechselrichter einlesen (CEC und OND)",
                     cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal(0, cut.Instance.SichtbareZeilen);

        Laden(cut);

        Assert.Equal(3, cut.Instance.SichtbareZeilen);
        Assert.Contains("Filter Auswahl (3 Geräte gefunden)", cut.Markup);
    }

    /// <summary>
    /// <b>Die Ausprägung entscheidet den Spaltensatz</b> (W6‑O‑1): sieben Spalten
    /// statt zehn, mit AC-Nennleistung, Euro-Wirkungsgrad und MPP-Fenster — und ohne
    /// Technologieklappliste, denn ein Wechselrichter hat keine.
    /// </summary>
    [Fact]
    public void Die_Wechselrichter_Auspraegung_traegt_ihren_eigenen_Spaltensatz()
    {
        var cut = Bauen(ModulImportArt.Wechselrichter, DreiGeraete());
        Laden(cut);

        string kopf = cut.Find("thead").TextContent;
        foreach (string spalte in new[]
                 { "Quelle", "Gerät", "Hersteller", "P_AC [kW]", "η euro",
                   "MPP-Fenster [V]", "U_dc max [V]" })
            Assert.Contains(spalte, kopf);

        Assert.DoesNotContain("Bifazial", kopf);

        // EINE Klappliste (Hersteller) und EIN Zahlenbereich (AC-Nennleistung).
        Assert.Single(cut.FindAll(".epos-pvimport-filter select"));
        Assert.Equal(2, cut.FindAll(".epos-pvimport-filter input[inputmode]").Count);
    }

    /// <summary>
    /// Die Filterleiste: Herstellerwahl und Suchmuster engen ein, „Zurücksetzen" gibt
    /// die Liste wieder frei. Der erste Eintrag der Herstellerliste ist ein
    /// STEUERWERT und kein Anzeigetext, gegen den verglichen wird.
    /// </summary>
    [Fact]
    public void Der_Import_filtert_nach_Hersteller_und_Suchmuster()
    {
        var cut = Bauen(ModulImportArt.Wechselrichter, DreiGeraete());
        Laden(cut);

        var hersteller = cut.FindAll(".epos-pvimport-filter select")[0];
        Assert.Equal(3, hersteller.QuerySelectorAll("option").Length);   // alle + zwei Firmen

        hersteller.Change("1");
        Assert.Equal(2, cut.Instance.SichtbareZeilen);

        cut.FindAll(".epos-pvimport-filter input[type=text]")[0].Input("*10000*");
        Assert.Equal(0, cut.Instance.SichtbareZeilen);

        Knopf(cut, "Zurücksetzen").Click();
        Assert.Equal(3, cut.Instance.SichtbareZeilen);
    }

    /// <summary>
    /// Die Wahl einer Zeile füllt die Detailfelder — und die Kennlinie steht als
    /// gerechnete Stützstelle da, nicht als Rohwert der Datei.
    /// </summary>
    [Fact]
    public void Eine_gewaehlte_Zeile_zeigt_ihre_gerechnete_Kennlinie()
    {
        var cut = Bauen(ModulImportArt.Wechselrichter, DreiGeraete());
        Laden(cut);

        cut.FindAll(".epos-anlagenwahl")[0].Click();
        Assert.NotNull(cut.Instance.Gewaehlt);

        var g = (CecWechselrichter)cut.Instance.Gewaehlt!;
        Assert.Equal("Alpha AG: A-3000", g.Name);

        // Der Pruefwert aus Konzept 3.3.3: eta bei Nennlast ist Paco/Pdco.
        double?[] etas = g.Stuetzstellen();
        Assert.Equal(3000.0 / (3000.0 * 1.05), etas[5]!.Value, 12);

        // ... und genau das steht im Reiter "Wirkungsgrad" (F4).
        cut.FindAll("[role='tab']")[1].Click();
        Assert.Equal(etas[5]!.Value.ToString("F4", CultureInfo.CurrentCulture),
                     cut.FindAll(".epos-formularraster input")[5].GetAttribute("value"));
    }

    /// <summary>
    /// „Übernehmen" ohne Auswahl ist gesperrt; mit Auswahl geht der Satz an den
    /// Schreibweg — und zwar unter seinem Bezeichner.
    /// </summary>
    [Fact]
    public void Uebernehmen_ohne_Auswahl_meldet_sich()
    {
        string? angelegt = null;
        var cut = Bauen(ModulImportArt.Wechselrichter, DreiGeraete(),
            vorpruefen: _ => Task.FromResult(new ImportVorpruefung(ImportBefund.Neu, null, null)),
            anlegen: (_, name) => { angelegt = name; return Task.FromResult(true); });

        Laden(cut);

        Assert.True(Uebernehmen(cut).HasAttribute("disabled"));

        cut.FindAll(".epos-anlagenwahl")[1].Click();
        Uebernehmen(cut).Click();

        Assert.Equal("Alpha AG: A-5000", angelegt);
    }

    /// <summary>
    /// <b>Ein Plausibilitätsfehler SPERRT die Übernahme</b>, eine Warnung fragt
    /// zurück — dieselbe Zweiteilung wie beim Modulimport, und seit W6‑O‑1
    /// derselbe Programmcode.
    /// </summary>
    [Fact]
    public void Ein_Plausibilitaetsfehler_sperrt_die_Uebernahme()
    {
        string? angelegt = null;
        var cut = Bauen(ModulImportArt.Wechselrichter, DreiGeraete(),
            vorpruefen: _ => Task.FromResult(
                new ImportVorpruefung(ImportBefund.Neu, null, null, "Die Kennlinie taugt nicht.", true)),
            anlegen: (_, name) => { angelegt = name; return Task.FromResult(true); });

        Laden(cut);
        cut.FindAll(".epos-anlagenwahl")[0].Click();
        Uebernehmen(cut).Click();

        Assert.Null(angelegt);
        Assert.Contains("Die Kennlinie taugt nicht.", cut.Instance.Meldung);
        Assert.False(cut.Instance.PlausiOffen);
    }

    /// <summary>Eine Warnung fragt zurück; „Nein" schreibt nichts.</summary>
    [Fact]
    public void Eine_Plausibilitaetswarnung_fragt_zurueck()
    {
        string? angelegt = null;
        var cut = Bauen(ModulImportArt.Wechselrichter, DreiGeraete(),
            vorpruefen: _ => Task.FromResult(
                new ImportVorpruefung(ImportBefund.Neu, null, null, "Die MPPT-Zahl fehlt.", false)),
            anlegen: (_, name) => { angelegt = name; return Task.FromResult(true); });

        Laden(cut);
        cut.FindAll(".epos-anlagenwahl")[0].Click();
        Uebernehmen(cut).Click();

        Assert.True(cut.Instance.PlausiOffen);
        Assert.Null(angelegt);

        cut.FindAll(".epos-rueckfrage .epos-knopf").Last().Click();   // Nein
        Assert.False(cut.Instance.PlausiOffen);
        Assert.Null(angelegt);
    }

    /// <summary>
    /// <b>Der Dublettenweg</b>: Ein bereits vorhandenes Gerät führt in den
    /// Konfliktdialog — dieselbe Überlagerung wie beim Modulimport und bei den vier
    /// VDI-Importen.
    /// </summary>
    [Fact]
    public void Eine_Dublette_fuehrt_in_den_Konfliktdialog()
    {
        var pruefung = new ImportPruefung
        {
            Kandidat = new ImportKandidat { Name = "Alpha AG: A-3000" },
            Befund = ImportBefund.NameVorhanden,
            Vorhanden = new KatalogSatz { Id = 7, Name = "Alpha AG: A-3000" }
        };

        var cut = Bauen(ModulImportArt.Wechselrichter, DreiGeraete(),
            vorpruefen: _ => Task.FromResult(new ImportVorpruefung(
                ImportBefund.NameVorhanden, new[] { pruefung }, new[] { "alpha ag: a-3000" })));

        Laden(cut);
        cut.FindAll(".epos-anlagenwahl")[0].Click();
        Uebernehmen(cut).Click();

        Assert.NotEmpty(cut.FindAll(".epos-ueberlagerung"));
    }

    /// <summary>
    /// „Abbrechen" meldet, dass nichts geschrieben wurde — der Rückgabewert, an dem
    /// die Hülle ihr Fenster schließt.
    /// </summary>
    [Fact]
    public void Schliessen_meldet_ob_geschrieben_wurde()
    {
        bool? ergebnis = null;
        var cut = Bauen(ModulImportArt.Wechselrichter, DreiGeraete(),
                        geschlossen: b => ergebnis = b);

        cut.FindAll(".epos-leiste")[1].QuerySelectorAll("button")[1].Click();

        Assert.False(ergebnis);
    }

    // =====================================================================
    // 7 — Der OND-Zweig (W6‑O‑1, Konzept 5.2)
    // =====================================================================

    /// <summary>
    /// <b>Die dritte Quelle der zweiten Ausprägung.</b> Der Dateiwähler bietet
    /// <c>.ond</c> an und macht im Herstellerdatenordner auf; die gelesene Datei steht
    /// mit Herkunft OND im Raster, neben CEC-Geräten in derselben Liste.
    /// </summary>
    [Fact]
    public void OND_laden_stellt_das_Geraet_mit_seiner_Herkunft_ins_Raster()
    {
        ImportQuelle? benutzt = null;

        var cut = Bauen(ModulImportArt.Wechselrichter, DreiGeraete(),
            dateiWaehlen: q => { benutzt = q; return Task.FromResult<string?>(@"D:\wr\muster.ond"); },
            dateiLaden: (q, pfad) => Task.FromResult(new ImportLeseErgebnis(
                true, new List<object> { Ond() }, new CecFortschritt("OND_MSG_GELESEN", "1"))));

        Knopf(cut, "OND laden").Click();

        Assert.Equal(ModulImportProfil.QuelleOnd, benutzt!.Schluessel);
        Assert.Equal("(*.ond)|*.ond", benutzt.Dateifilter);
        Assert.Equal("PV", benutzt.Unterordner);

        Assert.Equal(1, cut.Instance.SichtbareZeilen);
        string zeile = cut.Find("tbody").TextContent;
        Assert.Contains("Musterwerk Muster 2500TL", zeile);
        Assert.Contains("OND", zeile);
    }

    /// <summary>
    /// <b>Die Kennlinie einer OND-Datei kommt aus der Datei</b> und nicht aus dem
    /// Sandia-Modell (Konzept 5.2): die sechs Stützstellen des Anhangs A und der
    /// Euro-Wirkungsgrad, den das Datenblatt selbst nennt (<c>EfficEuro</c>).
    /// </summary>
    [Fact]
    public void Ein_OND_Geraet_zeigt_die_Stuetzstellen_der_Datei()
    {
        var cut = Bauen(ModulImportArt.Wechselrichter, new List<object>(),
            dateiWaehlen: _ => Task.FromResult<string?>(@"D:\wr\muster.ond"),
            dateiLaden: (_, __) => Task.FromResult(new ImportLeseErgebnis(
                true, new List<object> { Ond() }, new CecFortschritt("OND_MSG_GELESEN", "1"))));

        Knopf(cut, "OND laden").Click();
        cut.FindAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll("[role='tab']")[1].Click();

        var felder = cut.FindAll(".epos-formularraster input");
        Assert.Equal("0,9000", felder[0].GetAttribute("value"));   // 5 %
        Assert.Equal("0,9400", felder[1].GetAttribute("value"));   // 10 %
        Assert.Equal("0,9620", felder[2].GetAttribute("value"));   // 20 %
        Assert.Equal("0,9700", felder[3].GetAttribute("value"));   // 30 %
        Assert.Equal("0,9750", felder[4].GetAttribute("value"));   // 50 %
        Assert.Equal("0,9700", felder[5].GetAttribute("value"));   // 100 %
        Assert.Equal("0,9680", felder[6].GetAttribute("value"));   // EfficEuro der Datei
        Assert.Equal("0,9750", felder[7].GetAttribute("value"));   // EfficMax der Datei

        // Eine OND-Datei fuehrt kein Sandia-Modell.
        Assert.Equal("–", felder[8].GetAttribute("value"));        // Sandia Pdco
        Assert.Equal("350,0", felder[9].GetAttribute("value"));    // VMppNom als Bezugsspannung
        Assert.Equal("–", felder[10].GetAttribute("value"));       // Sandia C0
    }

    /// <summary>
    /// <b>Was der OND-Import kann und der CEC-Import nicht</b> (offener Punkt W6‑O‑2):
    /// MPPT-Zahl, Scheinleistung, DC-Leistung und Einschaltspannung stehen in der
    /// Datei; die CEC-Liste führt sie nicht und zeigt dort einen Strich.
    /// </summary>
    [Fact]
    public void Der_OND_Satz_fuellt_was_die_CEC_Liste_offen_laesst()
    {
        var cut = Bauen(ModulImportArt.Wechselrichter, new List<object>(),
            dateiWaehlen: _ => Task.FromResult<string?>(@"D:\wr\muster.ond"),
            dateiLaden: (_, __) => Task.FromResult(new ImportLeseErgebnis(
                true, new List<object> { Ond() }, new CecFortschritt("OND_MSG_GELESEN", "1"))));

        Knopf(cut, "OND laden").Click();
        cut.FindAll(".epos-anlagenwahl")[0].Click();

        ImportZeile zeile = cut.Instance.GewaehlteZeile!;
        Assert.Equal("2,500", zeile.Feld(ModulImportProfil.FeldPAcNenn));
        Assert.Equal("2,500", zeile.Feld(ModulImportProfil.FeldSAcMax));
        Assert.Equal("2,750", zeile.Feld(ModulImportProfil.FeldPDcMax));
        Assert.Equal("100,0", zeile.Feld(ModulImportProfil.FeldUStart));
        Assert.Equal("1", zeile.Feld(ModulImportProfil.FeldAnzahlMppt));
        Assert.Equal("OND", zeile.Feld(ModulImportProfil.FeldHerkunft));

        // Zum Vergleich: derselbe Feldsatz aus der CEC-Zeile.
        ImportZeile cec = ModulImportProfil.Finde(ModulImportArt.Wechselrichter, ImportTexte.Zu)
                                           .Zeile(0, Geraet("Alpha AG: A-3000", 3000));
        Assert.Equal("–", cec.Feld(ModulImportProfil.FeldSAcMax));
        Assert.Equal("–", cec.Feld(ModulImportProfil.FeldPDcMax));
        Assert.Equal("–", cec.Feld(ModulImportProfil.FeldUStart));
        Assert.Equal("–", cec.Feld(ModulImportProfil.FeldAnzahlMppt));
        Assert.Equal("CEC", cec.Feld(ModulImportProfil.FeldHerkunft));
    }

    // =====================================================================
    // 8 — Die Auslieferungsliste als DATEI (W6‑O‑3)
    // =====================================================================

    /// <summary>
    /// <b>Der Weg des Anwenderentscheids W6‑O‑3</b> („Liste als Datei und dann über
    /// Import"): Administration → Import öffnen → „CEC-Datei laden" → Datei wählen →
    /// Zeile wählen → Übernehmen. Der Dateiwähler fragt mit dem CSV-Filter und macht im
    /// Unterordner <c>PV</c> auf — dort liegen <c>CEC Modules.csv</c> und
    /// <c>CEC Inverters.csv</c>.
    /// </summary>
    [Theory]
    [InlineData(ModulImportArt.Photovoltaik)]
    [InlineData(ModulImportArt.Wechselrichter)]
    public void Die_Auslieferungsliste_wird_ueber_den_Dateiweg_eingelesen(ModulImportArt art)
    {
        ImportQuelle? benutzt = null;
        string? gelesen = null;
        List<object> saetze = art == ModulImportArt.Photovoltaik ? DreiModule() : DreiGeraete();

        var cut = Bauen(art, new List<object>(),
            dateiWaehlen: q => { benutzt = q; return Task.FromResult<string?>(@"C:\daten\PV\CEC.csv"); },
            dateiLaden: (q, pfad) =>
            {
                gelesen = pfad;
                return Task.FromResult(new ImportLeseErgebnis(
                    true, saetze, new CecFortschritt("CEC_MSG_GELADEN", "3")));
            });

        Knopf(cut, "CEC-Datei laden").Click();

        Assert.Equal(ModulImportProfil.QuelleCecDatei, benutzt!.Schluessel);
        Assert.Equal("(*.csv)|*.csv", benutzt.Dateifilter);
        Assert.Equal("PV", benutzt.Unterordner);
        Assert.Equal(@"C:\daten\PV\CEC.csv", gelesen);
        Assert.Equal(3, cut.Instance.SichtbareZeilen);
    }

    // =====================================================================
    // Hilfen
    // =====================================================================

    private static ImportVorpruefung Vorpruefung(ImportBefund befund)
    {
        var pruefungen = new List<ImportPruefung>
        {
            new() { Kandidat = new ImportKandidat { Name = "Ablytek 6MN6A270", Tag = null },
                    Befund = befund,
                    Vorhanden = befund == ImportBefund.Neu
                        ? null
                        : new KatalogSatz { Id = 5, Name = "Ablytek 6MN6A270" } }
        };
        return new ImportVorpruefung(befund, pruefungen, new[] { "ablytek 6mn6a270" });
    }
}
