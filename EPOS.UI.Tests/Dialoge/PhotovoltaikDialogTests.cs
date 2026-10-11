using System.Globalization;
using System.Globalization;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Allgemein;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using EPOS.UI.Standards;
using KiKern;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Verwaltung Photovoltaik Module (iU9-W6.5). Soll ist die Feldkarte von
/// <c>Form_PV</c> — mit der Berichtigung aus R‑W6‑7: Die Karte ordnet die drei
/// Panel-Beschriftungen falsch zu; maßgeblich ist der Designer (Neigung [°],
/// Azimut [°], Anzahl Module).
/// </summary>
public class PhotovoltaikDialogTests : EposBunitContext
{

    /// <summary>
    /// Der Filterstand DIESES Prüfstands. Ohne ihn nähme der Dialog den aus dem
    /// <c>Katalogfilterregister</c> — der lebt prozessweit, und xunit fährt
    /// Testklassen nebeneinander. Dass das Register wirklich teilt, prüft
    /// <c>KatalogfilterstandTests</c>.
    /// </summary>
    private readonly Katalogfilterstand _filterstand = new();
    /// <summary>
    /// Das PROFIL des Projektdialogs (W14a-E-10 / S2.1): dieselben sieben Spalten wie
    /// in der Verwaltung, dazu die achte „im Projekt verwendet" (Q12).
    /// </summary>
    private static readonly Katalogfilterprofil Profil =
        Katalogfilterprofil.MitVerwendung(Anlagenart.Photovoltaik,
            s => Resource.ResourceManager.GetString(s) ?? s);

    private static IReadOnlyList<Katalogfilterzeile> Katalogzeilen() => new[]
    {
        new Katalogfilterzeile(31, "Modul 400")
            .MitText(Katalogfilterprofil.SpBezeichner, "Modul 400")
            .MitText(Katalogfilterprofil.SpHersteller, "Musterwerk")
            .MitZahl(Katalogfilterprofil.SpPstc, 400.0, 0)
            .MitZahl(Katalogfilterprofil.SpEta, 20.5, 1)
            .MitText(Katalogfilterprofil.SpTechnologie, "Mono-c-Si")
            .MitZahl(Katalogfilterprofil.SpModulflaeche, 1.95, 2)
            .MitZahl(Katalogfilterprofil.SpTnoct, 45.0, 0),

        new Katalogfilterzeile(32, "Modul 500")
            .MitText(Katalogfilterprofil.SpBezeichner, "Modul 500")
            .MitText(Katalogfilterprofil.SpHersteller, "Solar AG")
            .MitZahl(Katalogfilterprofil.SpPstc, 500.0, 0)
            .MitZahl(Katalogfilterprofil.SpEta, 21.8, 1)
            .MitText(Katalogfilterprofil.SpTechnologie, "Mono-c-Si")
            .MitZahl(Katalogfilterprofil.SpModulflaeche, 2.29, 2)
            .MitZahl(Katalogfilterprofil.SpTnoct, 44.0, 0),
    };

    public PhotovoltaikDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static ErzeugerZeile Zeile(int schluessel, string name, int geraetId)
        => new() { Schluessel = schluessel, Bezeichner = name, GeraetId = geraetId,
                   Neigung = 30, Azimut = 180, AnzahlModule = 20 };

    private static ErzeugerDetail Detail(string name) => new(
        name, "Beschreibung",
        new[] { ("Hersteller:", "Musterwerk"), ("Modul Leistung [W]:", "275,19") });

    /// <summary>
    /// Das PROFIL des Modulkatalogs — aus ihm baut die Hülle über die
    /// <c>ModulFeldwertBruecke</c> die Felder des Aufklappers. Der Prüfstand nimmt
    /// DASSELBE Profil, damit die Feldarten hier nicht erfunden werden.
    /// </summary>
    private static readonly ModulKatalogProfil Modulprofil =
        ModulKatalogProfil.Finde(ModulKatalogArt.Photovoltaik,
            s => Resource.ResourceManager.GetString(s) ?? s);

    /// <summary>
    /// Die Felder des Aufklappers „Alle Daten anzeigen" (Anwenderentscheid 15.09.2026)
    /// — abgebildet wie in der Hülle: Der Bezeichner ist gesperrt, ein Auswahlfeld
    /// bleibt Lesewert (es führt seine Optionen im Modulkatalog, nicht hier), alles
    /// Übrige ist editierbar.
    /// </summary>
    private static List<BrowserFeldwert> Katalogfelder(string name)
    {
        var liste = new List<BrowserFeldwert>();
        foreach (ModulKatalogFeld f in Modulprofil.Felder)
        {
            string wert = f.Schluessel == ModulKatalogProfil.FeldBezeichner ? name
                        : f.Art == BrowserFeldArt.Zahl ? "12,5"
                        : f.Art == BrowserFeldArt.Ganzzahl ? "70"
                        : "Wert " + f.Schluessel;

            liste.Add(new BrowserFeldwert
            {
                Schluessel = f.Schluessel,
                Bezeichnung = f.Bezeichnung,
                Einheit = f.Einheit,
                Art = f.Art,
                Editierbar = !f.Gesperrt && f.Art != BrowserFeldArt.Auswahl,
                Wert = wert
            });
        }
        return liste;
    }

    private IRenderedComponent<PhotovoltaikDialog> Aufbauen(
        List<ErzeugerZeile>? zeilen = null,
        Func<int, AufnahmeErgebnis>? aufnehmen = null,
        Action<ErzeugerZeile>? entfernen = null,
        Action<ErzeugerZeile>? uebernehmen = null,
        Func<string>? gesamt = null,
        Func<int, string>? katalogLoeschen = null,
        Func<string, IReadOnlyDictionary<string, object>>? editorGaben = null,
        bool wizard = false,
        Func<string, ErzeugerDetail>? detail = null,
        Func<string, IReadOnlyList<BrowserFeldwert>?>? katalogfelder = null,
        Func<string, IReadOnlyList<BrowserFeldwert>, KatalogSpeicherErgebnis>? felderSpeichern = null,
        Action<bool>? geschlossen = null,
        IReadOnlyList<(int Id, string Text)>? strangmodule = null,
        Satzbearbeitungswege? projektsatzWege = null,
        Satzbearbeitungswege? katalogsatzWege = null,
        Rueckwegwege? rueckwegWege = null,
        Func<IReadOnlyList<Katalogfilterzeile>>? katalogzeilen = null,
        Func<ErzeugerZeile?, bool, Task>? kostenOeffnen = null,
        Func<ErzeugerZeile?, Task>? energiekosten = null)
    {
        return Render<PhotovoltaikDialog>(p => p
            .Add(x => x.Strangmodule, strangmodule ?? Array.Empty<(int, string)>())
            .Add(x => x.Zeilen, zeilen ?? new List<ErzeugerZeile> { Zeile(1, "Modul 400", 31) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, katalogzeilen ?? Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Detail, detail ?? (n => Detail(n)))
            .Add(x => x.Aufnehmen, aufnehmen ?? (_ => new AufnahmeErgebnis(Zeile(9, "Modul 500", 32))))
            .Add(x => x.Entfernen, entfernen)
            .Add(x => x.Uebernehmen, uebernehmen)
            .Add(x => x.Gesamtleistung, gesamt ?? (() => "8"))
            .Add(x => x.KatalogLoeschen, katalogLoeschen)
            .Add(x => x.Katalogfelder, katalogfelder)
            .Add(x => x.KatalogfelderSpeichern, felderSpeichern)
            .Add(x => x.EditorGaben, editorGaben)
            .Add(x => x.ProjektsatzWege, projektsatzWege)
            .Add(x => x.KatalogsatzWege, katalogsatzWege)
            .Add(x => x.RueckwegWege, rueckwegWege)
            .Add(x => x.KostenOeffnen, kostenOeffnen)
            .Add(x => x.EnergiekostenOeffnen, energiekosten)
            .Add(x => x.Wizard, wizard)
            .Add(x => x.Geschlossen, ok => geschlossen?.Invoke(ok)));
    }

    /// <summary>Die zweite Liste ist die KATALOGliste; ihre erste Zeile ist „Modul 400".</summary>
    /// <summary>Wählt eine Katalogzeile — im Katalog ist die ZEILE die Wahl (Kästchenmodus).</summary>
    private static void KatalogzeileWaehlen(IRenderedComponent<PhotovoltaikDialog> cut, int nummer = 0)
        => cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[nummer].Click();

    /// <summary>Der Modulbereich — der Gruppenkopf „Modul Eigenschaften:".</summary>
    /// <summary>Wählt eine Projektzeile über ihre Wahlspalte.</summary>
    private static void ProjektzeileWaehlen(IRenderedComponent<PhotovoltaikDialog> cut, int nummer = 0)
        => cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[nummer].Click();

    private static void KatalogAnkreuzen(IRenderedComponent<PhotovoltaikDialog> cut, params int[] zeilen)
    {
        foreach (int i in zeilen)
            cut.FindAll(".epos-raster")[1].QuerySelectorAll("td .epos-kaestchenzelle input")[i].Change(true);
    }

    private static IReadOnlyList<Katalogfilterzeile> MitSchloss(params int[] gesperrt)
    {
        var zeilen = Katalogzeilen();
        foreach (var z in zeilen) z.Geschuetzt = gesperrt.Contains(z.Id);
        return zeilen;
    }

    private static Satzbearbeitungswege Wege(List<(int Id, IReadOnlyList<BrowserFeldwert> Felder)>? gespeichert = null,
                                             KatalogSpeicherErgebnis? ergebnis = null)
        => new()
        {
            Lesen = id => Katalogfelder("Satz " + id),
            Speichern = l => { gespeichert?.AddRange(l); return ergebnis ?? new KatalogSpeicherErgebnis(true, "ok", ""); }
        };

    /// <summary>
    /// UeS2: Das Fragment des Satzes steht nur in der Satz-Überlagerung — „Bearbeiten" der
    /// Detailzeile öffnet sie (falls zu); zurück kommt ihr Körper.
    /// </summary>
    private static AngleSharp.Dom.IElement Satz(IRenderedComponent<PhotovoltaikDialog> cut)
    {
        if (cut.FindAll(".epos-satzueberlagerung-koerper").Count == 0)
            cut.Find(".epos-zweispalten-satzkopf > .epos-zweispalten-bearbeiten").Click();
        return cut.Find(".epos-satzueberlagerung-koerper");
    }

    /// <summary>UeS2: Schließt eine offene Satz-Überlagerung wie Abbrechen bzw. Schließen.</summary>
    private static void SatzZu(IRenderedComponent<PhotovoltaikDialog> cut)
    {
        var knopf = cut.FindAll(".epos-satzueberlagerung-abbrechen, .epos-satzueberlagerung-schliessen");
        if (knopf.Count > 0) knopf[0].Click();
    }

    /// <summary>UeS2: Das OK der Satz-Überlagerung — es speichert, wie früher „Speichern" des Aufklappers.</summary>
    private static void Ok(IRenderedComponent<PhotovoltaikDialog> cut)
        => cut.Find(".epos-satzueberlagerung-ok").Click();

    /// <summary>Der Auswahlpfad der Kostenknöpfe — drei Stück, in dieser Reihenfolge.</summary>
    private const string KOSTENKNOEPFE = ".epos-kostenleiste button.epos-knopf";

    // =================================================================================
    // Katalogauswahl V1, Stufe 3: Knöpfe an ihrem Ort, Detailzeile, Bearbeiten je Bereich
    // =================================================================================

    [Fact]
    public void S3_Die_Knoepfe_stehen_an_ihrem_Ort_und_der_Katalogfuss_hat_kein_Neu()
    {
        var cut = Aufbauen(projektsatzWege: Wege(), katalogsatzWege: Wege(), gesamt: () => "8,000",
                           editorGaben: _ => new Dictionary<string, object>(), katalogLoeschen: _ => "");

        // D: Kontext im Dialogkopf, keine eigene Kontextzeile mehr.
        Assert.Equal("Eingabe der Photovoltaik Anlagendaten",
                     cut.Find(".epos-dialog-kopf > .epos-dialog-kontext").TextContent);
        Assert.Empty(cut.FindAll(".epos-kontextzeile"));

        // P: Summe kWp, Bearbeiten…, Entfernen — der Rückweg nur mit seinen Wegen.
        var p = cut.Find(".epos-zweispalten-bereich--projekt > .epos-zweispalten-kopfleiste");
        var summe = p.QuerySelector(".epos-zweispalten-summe")!;
        Assert.Contains("Summe aller ausgewählten Module [kWp]:", summe.TextContent);
        Assert.Contains("8,000", summe.TextContent);
        Assert.NotNull(p.QuerySelector(".epos-knopf--bearbeiten-projekt"));
        Assert.NotNull(p.QuerySelector(".epos-zweispalten-knopf--entfernen"));
        Assert.Empty(cut.FindAll(".epos-knopf--rueckweg"));

        // K-Kopf: Suche und Übernehmen.
        var k = cut.Find(".epos-zweispalten-bereich--katalog > .epos-zweispalten-kopfleiste");
        Assert.NotNull(k.QuerySelector(".epos-katalog-suchzeile input[type=search]"));
        Assert.NotNull(k.QuerySelector(".epos-zweispalten-knopf--uebernehmen"));

        // K-Fuß: Vergleichen, Schloss, Löschen, Bearbeiten nach dem Namen — kein Neu… (4.9).
        KatalogzeileWaehlen(cut);
        var fuss = cut.Find(".epos-zweispalten-fussleiste").Children.ToList();
        Assert.Equal("Modul 400:", fuss[0].TextContent);
        Assert.Contains(fuss, e => e.ClassList.Contains("epos-knopf--vergleichen"));
        Assert.Contains(fuss, e => e.ClassList.Contains("epos-knopf--loeschen"));
        Assert.Contains(fuss, e => e.ClassList.Contains("epos-knopf--bearbeiten-katalog"));
        Assert.Empty(cut.FindAll(".epos-knopf--neu"));
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() is "Neu..." or "Neu…");
    }

    [Fact]
    public void S3_Ohne_Loeschweg_kein_Loeschknopf_und_ohne_Summe_keine_Summe()
    {
        var cut = Render<PhotovoltaikDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Modul 400", 31) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand));
        KatalogzeileWaehlen(cut);
        Assert.Empty(cut.FindAll(".epos-knopf--loeschen"));
        Assert.Empty(cut.FindAll(".epos-zweispalten-summe"));
    }

    [Fact]
    public void S3_Die_Detailzeile_nennt_Marke_Name_und_Kenndaten()
    {
        var cut = Aufbauen(katalogzeilen: () => MitSchloss(31));
        var zeile = cut.Find(".epos-zweispalten-satzzeile");
        Assert.Equal(Resource.AUSWAHL_MARKE_PROJEKTSATZ, zeile.QuerySelector(".epos-zweispalten-marke--satz")!.TextContent);
        Assert.Equal("Modul 400", zeile.QuerySelector(".epos-zweispalten-satzname")!.TextContent);
        Assert.Contains("Hersteller Musterwerk", zeile.QuerySelector(".epos-zweispalten-satzkenndaten")!.TextContent);

        KatalogzeileWaehlen(cut);
        zeile = cut.Find(".epos-zweispalten-satzzeile");
        Assert.Equal(Resource.AUSWAHL_MARKE_KATALOGSATZ, zeile.QuerySelector(".epos-zweispalten-marke--satz")!.TextContent);
        Assert.Contains(Resource.AUSWAHL_SATZ_NUR_LESEN, zeile.QuerySelector(".epos-zweispalten-satzkenndaten")!.TextContent);
    }

    /// <summary>
    /// Die Projektzeile liest ihr Detail über die ZEILE (Geräte-ID der Projektkopie), der Katalog über den Namen —
    /// der Name der Anlage darf vom Modul abweichen.
    /// </summary>
    [Fact]
    public void S3_Projektzeile_und_Katalogsatz_holen_ihr_Detail_je_aus_ihrer_Quelle()
    {
        var projekt = new List<int>();
        var katalog = new List<string>();
        var cut = Render<PhotovoltaikDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "PV Ost/West", 731) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.ProjektDetail, z => { projekt.Add(z.GeraetId); return Detail("Modul 400"); })
            .Add(x => x.Detail, n => { katalog.Add(n); return Detail(n); }));

        KatalogzeileWaehlen(cut, 1);

        Assert.Equal(new[] { 731 }, projekt);
        Assert.Equal(new[] { "Modul 500" }, katalog);
    }

    [Fact]
    public void S3_Alle_Daten_des_Projektsatzes_speichern_ueber_den_Kernweg()
    {
        var gespeichert = new List<(int Id, IReadOnlyList<BrowserFeldwert> Felder)>();
        var cut = Aufbauen(projektsatzWege: Wege(gespeichert));

        Assert.True(cut.Instance.ParameterOffen);
        Satz(cut);   // UeS2: „Alle Daten" stehen in der Satz-Ueberlagerung, ihr OK speichert
        var felder = cut.FindComponent<Katalogfelder>();
        Assert.Contains(felder.Instance.Felder, f => f.Schluessel == ModulKatalogProfil.FeldModulkosten && f.Editierbar);

        cut.FindAll(".epos-modulparameter input[inputmode=decimal]")[0].Input("42");
        Ok(cut);

        Assert.Equal(31, Assert.Single(gespeichert).Id);
    }

    [Fact]
    public void S3_Bearbeiten_im_Projektbereich_oeffnet_die_Projektkopie_je_Modul_einmal()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Modul 400", 31), Zeile(2, "Modul 400 Ost", 31),
                                               Zeile(3, "Modul 500", 32) };
        var cut = Aufbauen(zeilen, projektsatzWege: Wege());

        foreach (int i in new[] { 0, 1, 2 })
            cut.FindAll(".epos-raster")[0].QuerySelectorAll("td .epos-wahlkaestchen")[i].Click();
        cut.Find(".epos-knopf--bearbeiten-projekt").Click();

        Assert.Equal(Satzmarke.Projektsatz, cut.Instance.Bearbeitung!.Value.Art);
        Assert.Equal(new[] { 31, 32 }, cut.Instance.Bearbeitung!.Value.Saetze.Select(s => s.Id).ToArray());
        var kopf = cut.Find(".epos-ueberlagerung-kopf");
        Assert.Equal(Resource.AUSWAHL_MARKE_PROJEKTSATZ, kopf.QuerySelector(".epos-zweispalten-marke--satz")!.TextContent);
    }

    [Fact]
    public void S3_Ein_einzelner_ungesperrter_Katalogsatz_oeffnet_den_Katalogeditor_mit_Vorwahl()
    {
        string? vorgewaehlt = null;
        var cut = Aufbauen(katalogsatzWege: Wege(),
                           editorGaben: n => { vorgewaehlt = n; return new Dictionary<string, object>
                           {
                               ["Art"] = ModulKatalogArt.Photovoltaik,
                               ["Wege"] = new ModulKatalogWege(),
                               ["Vorwahl"] = n
                           }; });
        KatalogzeileWaehlen(cut, 1);
        cut.Find(".epos-knopf--bearbeiten-katalog").Click();

        Assert.True(cut.Instance.EditorOffen);
        Assert.Null(cut.Instance.Bearbeitung);
        Assert.Equal("Modul 500", vorgewaehlt);
        Assert.NotNull(cut.FindComponent<ModulKatalogDialog>());
    }

    [Fact]
    public void S3_Mehrfach_Bearbeiten_blaettert_und_setzt_fuer_alle_in_einem_Speichern()
    {
        var gespeichert = new List<(int Id, IReadOnlyList<BrowserFeldwert> Felder)>();
        var cut = Aufbauen(katalogsatzWege: Wege(gespeichert), editorGaben: _ => new Dictionary<string, object>());

        KatalogAnkreuzen(cut, 0, 1);
        Assert.Equal(2, cut.Instance.KatalogWahl.Anzahl);
        cut.Find(".epos-knopf--bearbeiten-katalog").Click();

        Assert.False(cut.Instance.EditorOffen);
        var sb = cut.FindComponent<Satzbearbeitung>();
        Assert.Equal(2, sb.Instance.Aktiv.Count);
        Assert.Equal(string.Format(Resource.Culture, Resource.SATZBEARB_TITEL_KATALOG_N, 2),
                     cut.Find(".epos-ueberlagerung-titel").TextContent);

        cut.Find(".epos-satzbearbeitung input[type=text]:not([readonly])").Input("Neuwerk");
        cut.FindAll(".epos-satzbearbeitung-fueralle input")[0].Change(true);
        cut.Find(".epos-satzbearbeitung-speichern").Click();

        Assert.Equal(new[] { 31, 32 }, gespeichert.Select(g => g.Id).ToArray());
        Assert.All(gespeichert, g => Assert.Equal("Neuwerk",
            g.Felder.First(f => f.Schluessel == ModulKatalogProfil.FeldFirma).Wert));
        Assert.Null(cut.Instance.Bearbeitung);
    }

    [Fact]
    public void S3_Gesperrte_Katalogsaetze_werden_uebersprungen_und_genannt()
    {
        var cut = Aufbauen(katalogsatzWege: Wege(), katalogzeilen: () => MitSchloss(32));

        KatalogAnkreuzen(cut, 0, 1);
        cut.Find(".epos-knopf--bearbeiten-katalog").Click();

        var sb = cut.FindComponent<Satzbearbeitung>();
        Assert.Equal(31, Assert.Single(sb.Instance.Aktiv).Id);
        Assert.Contains("„Modul 500“", cut.Find(".epos-satzbearbeitung-hinweis--uebersprungen").TextContent);
    }

    [Fact]
    public void S3_Ein_gesperrter_Katalogsatz_allein_oeffnet_nur_lesend()
    {
        var cut = Aufbauen(katalogsatzWege: Wege(), katalogzeilen: () => MitSchloss(31),
                           editorGaben: _ => new Dictionary<string, object>());

        KatalogzeileWaehlen(cut);
        cut.Find(".epos-knopf--bearbeiten-katalog").Click();

        Assert.False(cut.Instance.EditorOffen);
        Assert.True(cut.FindComponent<Satzbearbeitung>().Instance.NurLesend);
        Assert.Empty(cut.FindAll(".epos-satzbearbeitung-speichern"));
    }

    [Fact]
    public void S3_Ein_abgelehntes_Sammelspeichern_bleibt_offen_und_nennt_den_Grund()
    {
        var cut = Aufbauen(katalogsatzWege: Wege(ergebnis: new KatalogSpeicherErgebnis(false, "„Modul 500“ ist gesperrt.", "")));

        KatalogAnkreuzen(cut, 0, 1);
        cut.Find(".epos-knopf--bearbeiten-katalog").Click();
        cut.Find(".epos-satzbearbeitung input[type=text]:not([readonly])").Input("Neuwerk");
        cut.Find(".epos-satzbearbeitung-speichern").Click();

        Assert.NotNull(cut.Instance.Bearbeitung);
        Assert.Contains("gesperrt", cut.Find(".epos-satzbearbeitung-meldung").TextContent);
    }

    [Fact]
    public void S3_Die_Sammeluebernahme_legt_je_angekreuztem_Satz_eine_Zeile_an()
    {
        var aufgenommen = new List<int>();
        var zeilen = new List<ErzeugerZeile>();
        int summenRufe = 0;
        var cut = Aufbauen(zeilen, aufnehmen: id =>
        {
            aufgenommen.Add(id);
            return new AufnahmeErgebnis(Zeile(20 + id, "Neu " + id, 100 + id));
        }, gesamt: () => { summenRufe++; return "0"; });

        KatalogAnkreuzen(cut, 0, 1);
        int vorher = summenRufe;
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();

        Assert.Equal(new[] { 31, 32 }, aufgenommen.ToArray());
        Assert.Equal(2, zeilen.Count);
        Assert.Equal(0, cut.Instance.KatalogWahl.Anzahl);
        Assert.True(summenRufe > vorher);
    }

    [Fact]
    public void S3_Entfernen_wirkt_auf_alle_gewaehlten_Projektzeilen()
    {
        var entfernt = new List<string>();
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "A", 131), Zeile(2, "B", 132), Zeile(3, "C", 133) };
        var cut = Aufbauen(zeilen, entfernen: z => entfernt.Add(z.Bezeichner));

        cut.FindAll(".epos-raster")[0].QuerySelectorAll("td .epos-wahlkaestchen")[0].Click();
        cut.FindAll(".epos-raster")[0].QuerySelectorAll("td .epos-wahlkaestchen")[2].Click();
        cut.Find(".epos-zweispalten-knopf--entfernen").Click();

        Assert.Equal(new[] { "A", "C" }, entfernt.ToArray());
        Assert.Equal("B", Assert.Single(zeilen).Bezeichner);
    }

    [Fact]
    public void S3_Loeschen_fragt_nach_und_nennt_den_Namen()
    {
        var geloescht = new List<int>();
        var cut = Aufbauen(katalogLoeschen: id => { geloescht.Add(id); return ""; });
        KatalogzeileWaehlen(cut, 1);
        cut.Find(".epos-knopf--loeschen").Click();

        Assert.Contains("Modul 500", cut.Find(".epos-rueckfrage").TextContent);
        Assert.Empty(geloescht);
        cut.FindAll(".epos-rueckfrage button").First(b => b.TextContent.Trim() == "Ja").Click();
        Assert.Equal(new[] { 32 }, geloescht.ToArray());
        Assert.Null(cut.Instance.Katalogzeile);
    }

    [Fact]
    public void S3_Loeschen_mehrerer_ueberspringt_gesperrte_und_meldet_einen_Grund()
    {
        var geloescht = new List<int>();
        var cut = Aufbauen(katalogzeilen: () => MitSchloss(32),
                           katalogLoeschen: id => { geloescht.Add(id); return id == 31 ? "verwendet" : ""; });

        KatalogAnkreuzen(cut, 0, 1);
        cut.Find(".epos-knopf--loeschen").Click();
        Assert.Contains(string.Format(Resource.Culture, Resource.SATZBEARB_UEBERSPRUNGEN, "„Modul 500“"),
                        cut.Find(".epos-rueckfrage").TextContent);
        cut.FindAll(".epos-rueckfrage button").First(b => b.TextContent.Trim() == "Ja").Click();

        Assert.Equal(new[] { 31 }, geloescht.ToArray());
        Assert.Equal("verwendet", cut.Instance.Meldung);
    }

    [Fact]
    public void S3_Die_Knopfzeile_steht_als_erstes_in_der_Satzflaeche()
    {
        var cut = Aufbauen(kostenOeffnen: (_, _) => Task.CompletedTask,
                           energiekosten: _ => Task.CompletedTask,
                           projektsatzWege: Wege());

        Assert.Empty(cut.FindAll(".epos-zweispalten-satz .epos-leiste"));   // UeS2: zu steht nur die Zusammenfassung
        var kinder = Satz(cut).Children.ToList();
        Assert.Contains("epos-leiste", kinder[0].ClassList);
        int raster = kinder.FindIndex(k => k.ClassList.Contains("epos-formularraster"));
        int anlage = kinder.FindIndex(k => k.ClassList.Contains("epos-anlagenblock"));
        int parameter = kinder.FindIndex(k => k.ClassList.Contains("epos-modulparameter"));
        Assert.True(0 < raster && raster < anlage && anlage < parameter);

        var teile = kinder[0].Children.ToList();
        Assert.Contains("epos-kostenleiste", teile[0].ClassList);
        Assert.Contains("epos-leiste-fueller", teile[1].ClassList);
        Assert.Contains("epos-berechnungshilfe", teile[2].ClassList);
    }

    [Fact]
    public void S3_Ohne_Wahl_steht_der_Leerhinweis()
    {
        var cut = Aufbauen(zeilen: new List<ErzeugerZeile>(), katalogzeilen: () => Array.Empty<Katalogfilterzeile>());
        Assert.Equal(Resource.AUSWAHL_SATZ_LEER, cut.Find(".epos-zweispalten-satzleer").TextContent);
    }

    [Fact]
    public void S3_Die_Kostenknoepfe_stehen_nur_beim_Projektsatz_und_fehlen_im_Assistenten()
    {
        var kosten = new List<bool>();
        int energie = 0;
        ErzeugerZeile? gesehen = null;
        var cut = Aufbauen(
            kostenOeffnen: (zeile, betrieb) => { gesehen = zeile; kosten.Add(betrieb); return Task.CompletedTask; },
            energiekosten: _ => { energie++; return Task.CompletedTask; });

        var ohne = Aufbauen();
        Satz(ohne);   // UeS2: die Kostenknoepfe stehen in der Satz-Ueberlagerung
        Assert.Empty(ohne.FindAll(KOSTENKNOEPFE));
        Assert.Empty(cut.FindAll(KOSTENKNOEPFE));   // zu steht nur die Zusammenfassung
        Satz(cut);
        var knoepfe = cut.FindAll(KOSTENKNOEPFE);
        Assert.Equal(3, knoepfe.Count);
        knoepfe[0].Click();
        cut.FindAll(KOSTENKNOEPFE)[1].Click();
        cut.FindAll(KOSTENKNOEPFE)[2].Click();
        Assert.Equal(new[] { false, true }, kosten);
        Assert.Equal(1, energie);
        Assert.Equal(31, gesehen!.GeraetId);

        SatzZu(cut);
        KatalogzeileWaehlen(cut);
        Assert.Null(cut.Instance.Projektzeile);
        Satz(cut);
        Assert.Empty(cut.FindAll(KOSTENKNOEPFE));

        var wizard = Aufbauen(wizard: true, kostenOeffnen: (_, _) => Task.CompletedTask);
        Satz(wizard);
        Assert.Empty(wizard.FindAll(".epos-kostenleiste"));
    }

    // =================================================================================
    // Stufe 3: der Projektsatz und die Überlagerung „Stränge und Wechselrichter…" (KA-E-11)
    // =================================================================================

    [Fact]
    public void S3_Der_Projektsatz_traegt_Stueckzahl_Neigung_Azimut_und_das_Ertragsmodell()
    {
        var cut = Aufbauen();
        Assert.Empty(cut.FindAll(".epos-anlagenblock"));   // UeS2: zu steht nur die Zusammenfassung

        Assert.Single(Satz(cut).QuerySelectorAll(".epos-anlagenblock"));
        Assert.Single(cut.FindComponents<PvModellFelder>());
        // Die Strangtabelle steht NICHT in der Satzfläche - sie ist die Überlagerung.
        Assert.Empty(cut.FindComponents<PvStraengeFelder>());
        Assert.Single(cut.FindAll(".epos-knopf--straenge"));

        SatzZu(cut);
        KatalogzeileWaehlen(cut);
        Satz(cut);
        Assert.Empty(cut.FindAll(".epos-anlagenblock"));
        Assert.Empty(cut.FindComponents<PvModellFelder>());
        Assert.Empty(cut.FindAll(".epos-knopf--straenge"));
    }

    [Fact]
    public void S3_Straenge_und_Wechselrichter_oeffnet_die_Ueberlagerung_mit_der_Strangtabelle()
    {
        bool? gemeldet = null;
        var cut = Aufbauen(geschlossen: ok => gemeldet = ok);
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung

        cut.Find(".epos-knopf--straenge").Click();

        Assert.True(cut.Instance.StraengeOffen);
        Assert.Single(cut.FindComponents<PvStraengeFelder>());
        // UeS2: ueber der Satz-Ueberlagerung - ihr Titel steht als zweiter.
        Assert.Equal("Stränge und Wechselrichter – Modul 400", cut.FindAll(".epos-ueberlagerung-titel").Last().TextContent);
        Assert.Equal(Resource.AUSWAHL_MARKE_PROJEKTSATZ,
                     cut.Find(".epos-ueberlagerung-kopf .epos-zweispalten-marke--satz").TextContent);

        // Esc schliesst bei offener Überlagerung den Dialog nicht.
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(gemeldet);

        cut.FindAll(".epos-ueberlagerung .epos-leiste button").Last(b => b.TextContent.Trim() == "OK").Click();
        Assert.False(cut.Instance.StraengeOffen);
        Assert.Empty(cut.FindComponents<PvStraengeFelder>());
        Assert.Null(gemeldet);
    }

    [Fact]
    public void S3_Mit_Modulname_fuehrt_die_Strangmodulliste_die_Katalog_Id_des_Moduls()
    {
        // Die Zeile traegt die Projektkopie (Id 731) und einen eigenen Anlagennamen; das Band zum Katalog ist der
        // Name des Moduls.
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "PV Ost/West", 731) };
        var cut = Render<PhotovoltaikDialog>(p => p
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Strangmodule, new[] { (31, "Modul 400"), (32, "Modul 500") })
            .Add(x => x.Modulname, z => z.GeraetId == 731 ? "Modul 500" : ""));

        Assert.Equal(new[] { (32, "Modul 500") }, cut.Instance.ProjektmoduleFuerStraenge().ToArray());
    }

    [Fact]
    public void S3_Die_Vorgabe_der_Summe_sagt_kWp()
    {
        Assert.Equal("Summe aller ausgewählten Module [kWp]:", new PhotovoltaikDialog().LabelSumme);
        Assert.Equal("Stränge und Wechselrichter…", new PhotovoltaikDialog().BtnStraengeText);
    }


    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Der_Feldbestand_der_Karte_steht()
    {
        var cut = Aufbauen();

        Assert.Equal(2, cut.FindAll(".epos-raster").Count);
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-knopf--richtung").Count);

        var ueberschriften = cut.FindAll(".epos-untergruppe").Select(e => e.TextContent).ToList();
        Assert.Contains("ausgewählte Module", ueberschriften);
        Assert.Contains("Module aus Datenbank", ueberschriften);

        // Katalogauswahl V1: keine Gruppenköpfe mehr - die Detailzeile „gewählter Satz" trägt Modul und Anlage.
        Assert.Empty(cut.FindAll(".epos-gruppenkopf-titel"));
        Assert.Empty(cut.FindAll(".epos-anlagenblock"));   // UeS2: zu steht nur die Zusammenfassung
        Assert.Single(Satz(cut).QuerySelectorAll(".epos-anlagenblock"));
    }

    [Fact]
    public void Die_drei_Anlagenfelder_tragen_die_Beschriftungen_des_Designers()
    {
        // R-W6-7: Die Feldkarte ordnet "Azimut [°]" dem Feld textBox_AnlagenLeistung
        // und "10" dem Feld textBox_Azimut zu. Der Designer sagt es anders, und er
        // hat recht: label3 "Neigung [°]:" liegt ueber textBox_Neigung, label6
        // "Azimut [°]:" ueber textBox_Azimut, label7 "Anzahl Module:" ueber
        // textBox_AnlagenLeistung.
        var cut = Aufbauen();
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung

        var block = cut.Find(".epos-anlagenblock");
        var texte = block.QuerySelectorAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.Equal(new[] { "Neigung [°]:", "Azimut [°]:", "Anzahl Module:" }, texte);

        // Neigung und Azimut sind ganzzahlig, die Anzahl Module ist ein double
        // (WErzeugerModel.PV_Leistung) - der Feldname taeuscht, der Inhalt ist eine
        // Stueckzahl.
        Assert.Equal(2, block.QuerySelectorAll("input[inputmode=numeric]").Length);
        Assert.Single(block.QuerySelectorAll("input[inputmode=decimal]"));
    }

    [Fact]
    public void Der_Anlagenblock_erscheint_nur_bei_gewaehlter_Projektzeile()
    {
        // panel1.Visible - der Vorlaeufer blendete ihn beim Katalogsatz aus.
        var cut = Aufbauen();
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung
        Assert.Single(cut.FindAll(".epos-anlagenblock"));
        SatzZu(cut);

        KatalogzeileWaehlen(cut, 0);
        Satz(cut);

        Assert.Empty(cut.FindAll(".epos-anlagenblock"));
    }

    /// <summary>Ein Mindestsatz für die Überlagerung — der Katalog braucht sein Profil.</summary>
    [Fact]
    public void Im_Assistenten_fehlt_die_OK_Leiste()
    {
        var cut = Aufbauen(wizard: true);
        Assert.Empty(cut.FindAll(".epos-status"));
    }

    // =================================================================================
    // Aufnehmen und Entfernen
    // =================================================================================

    [Fact]
    public void Der_Pfeil_nimmt_ohne_Traegerdialog_auf()
    {
        // Anders als Heizkessel und BHKW: keine Traegervariante, keine Projektkopie.
        int? gefragt = null;
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Modul 400", 31) };
        var cut = Aufbauen(zeilen, aufnehmen: id =>
        {
            gefragt = id;
            return new AufnahmeErgebnis(Zeile(9, "Modul 500", 32));
        });

        KatalogzeileWaehlen(cut, 1);
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();

        Assert.Equal(32, gefragt);
        Assert.Equal(2, zeilen.Count);
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
    }

    [Fact]
    public void Der_Pfeil_zurueck_entfernt_die_ZEILE_nicht_ihren_Index()
    {
        // A-5: btn_Entfernen_Click nahm RemoveAt(SelectedIndex) auf eine Liste, die im
        // Assistenten ALLE Erzeugertypen fuehrt - der Index passte dort nicht.
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Modul 400", 31), Zeile(2, "Modul 400", 31) };
        var entfernt = new List<ErzeugerZeile>();
        var cut = Aufbauen(zeilen, entfernen: z => entfernt.Add(z));

        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[1].Click();
        cut.FindAll(".epos-zweispalten-knopf--entfernen")[0].Click();

        Assert.Single(zeilen);
        Assert.Equal(1, zeilen[0].Schluessel);
        Assert.Equal(2, entfernt[0].Schluessel);
    }

    // =================================================================================
    // Anlagenwerte und Gesamtleistung
    // =================================================================================

    [Fact]
    public void Die_drei_Anlagenwerte_wandern_ins_Modell()
    {
        var uebernommen = new List<ErzeugerZeile>();
        var cut = Aufbauen(uebernehmen: z => uebernommen.Add(z));

        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung
        var block = cut.Find(".epos-anlagenblock");
        block.QuerySelectorAll("input[inputmode=numeric]")[0].Input("35");
        block.QuerySelectorAll("input[inputmode=numeric]")[1].Input("200");
        block.QuerySelectorAll("input[inputmode=decimal]")[0].Input("25");

        Assert.Equal(35, cut.Instance.Projektzeile!.Neigung);
        Assert.Equal(200, cut.Instance.Projektzeile!.Azimut);
        Assert.Equal(25, cut.Instance.Projektzeile!.AnzahlModule);
        Assert.Equal(3, uebernommen.Count);
    }

    [Fact]
    public void Eine_neue_Modulzahl_zieht_die_Gesamtleistung_nach()
    {
        int rufe = 0;
        var cut = Aufbauen(gesamt: () => (++rufe).ToString());

        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung
        int vorher = rufe;
        cut.Find(".epos-anlagenblock input[inputmode=decimal]").Input("25");

        Assert.True(rufe > vorher, "Die Gesamtleistung wurde nicht neu erfragt.");
    }

    [Fact]
    public void Die_Gesamtleistung_kommt_fertig_von_aussen()
    {
        var cut = Aufbauen(gesamt: () => "12,50");
        Assert.Equal("12,50", cut.Instance.Gesamt);
    }

    // =====================================================================
    //  Anwenderentscheid W6‑O‑5 (05.09.2026) — „Gesamtleistung in kW"
    // =====================================================================

    // =================================================================================
    // Katalogpflege und Tastatur
    // =================================================================================

    [Fact]
    public void Esc_bricht_ab_und_Enter_ist_nicht_belegt()
    {
        int rufe = 0;
        bool? gemeldet = null;
        var cut = Aufbauen(geschlossen: ok => { gemeldet = ok; rufe++; });

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(0, rufe);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(1, rufe);
        Assert.False(gemeldet);
    }

    /// <summary>
    /// <b>„Das Kreuz steht beim Titel"</b> (Anwenderentscheid 15.09.2026): Das ✕ der
    /// Kopfzeile wirkt genau wie Esc — es schließt ohne zu speichern und meldet
    /// <c>false</c>.
    /// </summary>
    [Fact]
    public void Das_Kreuz_im_Kopf_bricht_ab_wie_Esc()
    {
        int rufe = 0;
        bool? gemeldet = null;
        var cut = Aufbauen(geschlossen: ok => { gemeldet = ok; rufe++; });

        cut.Find(".epos-dialog-zu").Click();

        Assert.Equal(1, rufe);
        Assert.False(gemeldet);
    }

    // =====================================================================
    //  Formularraster — Anwenderwunsch iU8‑E‑2, Paket P1 (05.09.2026)
    // =====================================================================

    /// <summary>
    /// <b>iU8‑E‑2, Paket P1:</b> „Darstellung der Dialoge kompakter und
    /// übersichtlicher — Parameterblöcke rechts."
    ///
    /// <para>Der Detailblock des Projektdialogs steht seither im <c>Formularraster</c>: Die Beschriftung
    /// fällt NEBEN das Feld, die Felder ordnen sich in eine oder zwei Spalten,
    /// und ein Zahlenfeld ist kurz mit der Einheit unmittelbar dahinter. Zuvor
    /// nahm jedes Feld die volle Breite und die Beschriftung stand darüber.</para>
    ///
    /// <para>Die Regeln dahinter hält <c>Bausteine/FormularrasterTests</c>;
    /// hier steht nur, dass der Block ihn TRÄGT.</para>
    /// </summary>
    [Fact]
    public void Der_Detailblock_steht_im_Formularraster()
    {
        var cut = Aufbauen();
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung

        var raster = cut.FindAll(".epos-formularraster");
        Assert.NotEmpty(raster);
        Assert.Contains(raster, r => r.QuerySelectorAll(".epos-feld").Length > 0);
    }

    // =====================================================================
    //  Der Aufklapper „Alle Daten anzeigen" — Anwenderentscheid 15.09.2026
    // =====================================================================
    //  Er löst den Parameterblock aus W6‑E‑1 (05.09.2026) ab: Damals dreizehn
    //  NUR LESBARE Zeilen aus einer eigenen Liste, jetzt ALLE Felder des
    //  Katalogsatzes in der Bauart aller sechs Erzeuger — derselbe Baustein
    //  Katalogfelder, gespeist aus derselben Feldliste wie der Modulkatalog,
    //  und bearbeitbar, wo der Katalog einen Speicherweg hat.

    /// <summary>
    /// <b>Der Aufklapper gehört zum KATALOGSATZ.</b> Beim Öffnen ist die Projektzeile
    /// markiert — dann gibt es ihn nicht; er erscheint mit der Wahl in der
    /// Katalogliste.
    /// </summary>
    [Fact]
    public void Ohne_gewaehlten_Katalogsatz_gibt_es_keinen_Aufklapper()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder);
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung

        Assert.Empty(cut.FindAll(".epos-modulparameter-knopf"));

        SatzZu(cut);
        KatalogzeileWaehlen(cut);
        Satz(cut);
        Assert.Single(cut.FindAll(".epos-modulparameter-knopf"));
    }

    /// <summary>
    /// Ohne Weg zu den Feldern kein Aufklapper — Hausregel „kein Delegat, kein
    /// Knopf". Ein leerer Aufklapper wäre ein Versprechen ohne Inhalt.
    /// </summary>
    [Fact]
    public void Ohne_Katalogfelder_gibt_es_keinen_Aufklapper()
    {
        var cut = Aufbauen();
        KatalogzeileWaehlen(cut);

        Assert.Empty(cut.FindAll(".epos-modulparameter-knopf"));
    }

    /// <summary>
    /// AUFGEKLAPPT IST DIE VORGABE (Anwenderentscheid 16.09.2026: „Unter Bearbeiten
    /// sollen alle Parameter angezeigt werden und bearbeitbar sein"), und der Weg zu
    /// den Feldern wird mit der WAHL gegangen — einmal je Satz, nicht je Klick. Ohne
    /// gewählten Satz gibt es den Block nicht, und dann wird auch nichts abgefragt.
    /// </summary>
    [Fact]
    public void Der_Parameterblock_ist_aufgeklappt_die_Vorgabe()
    {
        int rufe = 0;
        var cut = Aufbauen(katalogfelder: n => { rufe++; return Katalogfelder(n); });

        Assert.Equal(0, rufe);
        Assert.Empty(cut.FindAll(".epos-modulparameter"));

        KatalogzeileWaehlen(cut);
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung

        var knopf = cut.Find(".epos-modulparameter-knopf");
        Assert.Equal("true", knopf.GetAttribute("aria-expanded"));
        Assert.Contains("Alle Daten anzeigen", knopf.TextContent);

        Assert.True(cut.Instance.ParameterOffen);
        Assert.Equal(1, rufe);

        var block = cut.Find(".epos-modulparameter");
        Assert.Equal(Modulprofil.Felder.Count, block.QuerySelectorAll(".epos-feld").Length);
    }

    /// <summary>
    /// Der Knopf klappt zu und wieder auf — der Zustand gehört dem Dialog, nicht
    /// dem Browser.
    /// </summary>
    [Fact]
    public void Der_Knopf_klappt_zu_und_wieder_auf()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder);
        KatalogzeileWaehlen(cut);
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung

        cut.Find(".epos-modulparameter-knopf").Click();
        Assert.False(cut.Instance.ParameterOffen);
        Assert.Equal("false", cut.Find(".epos-modulparameter-knopf").GetAttribute("aria-expanded"));
        Assert.Empty(cut.Find(".epos-modulparameter").QuerySelectorAll(".epos-feld"));

        cut.Find(".epos-modulparameter-knopf").Click();
        Assert.True(cut.Instance.ParameterOffen);
        Assert.NotEmpty(cut.Find(".epos-modulparameter").QuerySelectorAll(".epos-feld"));
    }

    /// <summary>
    /// <b>Der Block gehört zum GEWÄHLTEN Modul.</b> Wer in der Katalogliste ein anderes
    /// Modul wählt, sieht dessen Werte — und der Aufklappzustand bleibt stehen, sonst
    /// müsste man ihn beim Vergleichen zweier Module jedes Mal neu aufziehen.
    /// </summary>
    [Fact]
    public void Ein_Modulwechsel_zieht_den_Block_nach()
    {
        int rufe = 0;
        var cut = Aufbauen(katalogfelder: n => { rufe++; return Katalogfelder(n); });
        KatalogzeileWaehlen(cut);
        Satz(cut);   // UeS2: „Alle Daten" stehen in der Satz-Ueberlagerung

        Assert.Equal(1, rufe);
        Assert.Equal("Modul 400", cut.Find(".epos-modulparameter")
                                     .QuerySelectorAll("input")[0].GetAttribute("value"));

        // Zweite Zeile der KATALOGliste: "Modul 500".
        SatzZu(cut);   // Abbrechen liest den Satz neu (verwirft)
        int vorher = rufe;
        KatalogzeileWaehlen(cut, 1);
        Satz(cut);

        Assert.True(cut.Instance.ParameterOffen);
        Assert.Equal(vorher + 1, rufe);   // der Wechsel liest genau einmal
        Assert.Equal("Modul 500", cut.Find(".epos-modulparameter")
                                     .QuerySelectorAll("input")[0].GetAttribute("value"));
    }

    /// <summary>
    /// <b>Zwei Felder bleiben Lesewerte:</b> der Bezeichner — er ist der Schlüssel des
    /// <c>UPDATE</c> — und die Zelltechnologie, ein Auswahlfeld, dessen Optionen der
    /// Aufklapper nicht führt. Die Zahl der gesperrten Felder liest der Prüfstand aus
    /// DEMSELBEN Profil, aus dem die Hülle sie ableitet.
    /// </summary>
    [Fact]
    public void Der_Bezeichner_und_das_Auswahlfeld_bleiben_Lesewerte()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder,
                           felderSpeichern: (n, _) => new KatalogSpeicherErgebnis(true, "", n));
        KatalogzeileWaehlen(cut);
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung

        int erwartet = Modulprofil.Felder
            .Count(f => f.Gesperrt || f.Art == BrowserFeldArt.Auswahl);

        Assert.True(erwartet > 0, "Das Profil führt weder ein gesperrtes noch ein Auswahlfeld.");
        Assert.Equal(erwartet,
            cut.Find(".epos-modulparameter").QuerySelectorAll("input[readonly]").Length);
    }

    /// <summary>
    /// <b>Ohne Speicherweg ist der Aufklapper reine Anzeige</b> — kein Knopf, jedes
    /// Feld nur lesbar. Das ist die Lage eines Katalogs ohne Schreibweg, keine
    /// Entscheidung dieses Dialogs.
    /// </summary>
    [Fact]
    public void Ohne_Speicherweg_ist_der_Aufklapper_nur_Anzeige()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder);
        KatalogzeileWaehlen(cut);
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung

        Assert.Empty(cut.FindAll(".epos-modulparameter .epos-leiste"));

        var eingaben = cut.FindAll(".epos-modulparameter input");
        Assert.NotEmpty(eingaben);
        Assert.All(eingaben, e => Assert.True(e.HasAttribute("readonly")));
    }

    /// <summary>
    /// <b>Speichern reicht die GEÄNDERTEN Felder hinaus</b> — und ist ohne Änderung
    /// gesperrt. Was daraus wird, entscheidet die Hülle; die Komponente kennt weder
    /// Tabelle noch Spalte.
    /// </summary>
    [Fact]
    public void Speichern_reicht_die_geaenderten_Felder_hinaus()
    {
        string? name = null;
        IReadOnlyList<BrowserFeldwert>? gesehen = null;
        var cut = Aufbauen(
            katalogfelder: Katalogfelder,
            felderSpeichern: (n, f) =>
            {
                name = n;
                gesehen = f;
                return new KatalogSpeicherErgebnis(true, "", n);
            });

        KatalogzeileWaehlen(cut);
        Satz(cut);   // UeS2: „Alle Daten" stehen in der Satz-Ueberlagerung, ihr OK speichert

        Assert.Empty(cut.FindAll(".epos-modulparameter .epos-leiste .epos-knopf"));

        cut.FindAll(".epos-modulparameter input[inputmode=decimal]")[0].Input("42");

        Assert.False(cut.Find(".epos-satzueberlagerung-ok").HasAttribute("disabled"));
        Ok(cut);

        Assert.Equal("Modul 400", name);
        Assert.NotNull(gesehen);
        Assert.Contains(gesehen!, f => f.Wert == "42");
    }

    /// <summary>
    /// <b>Der Vermerk am Knopf:</b> „Gespeichert um …" steht neben „Speichern", nicht als
    /// Band; ohne Änderung ist der Knopf weich gesperrt, ein Klick nennt den Grund, und
    /// die nächste Eingabe nimmt den Vermerk zurück.
    /// </summary>
    [Fact]
    public void UeS2_OK_schreibt_nur_eine_Aenderung_und_ein_zweites_Oeffnen_schreibt_nicht_erneut()
    {
        int schreibvorgaenge = 0;
        var cut = Aufbauen(katalogfelder: Katalogfelder,
                           felderSpeichern: (n, _) =>
                           {
                               schreibvorgaenge++;
                               return new KatalogSpeicherErgebnis(true, "Datensatz gespeichert", n);
                           });
        KatalogzeileWaehlen(cut);
        Satz(cut);   // UeS2: „Alle Daten" stehen in der Satz-Ueberlagerung, ihr OK speichert

        cut.FindAll(".epos-modulparameter input[inputmode=decimal]")[0].Input("42");
        Ok(cut);

        Assert.Equal(1, schreibvorgaenge);
        Assert.Equal("", cut.Instance.Meldung);
        Assert.False(cut.Instance.SatzUeberlagerungOffen);

        // Wieder geoeffnet und ohne Aenderung bestaetigt: kein zweiter Schreibvorgang.
        Satz(cut);
        Ok(cut);
        Assert.Equal(1, schreibvorgaenge);

        // Die naechste Eingabe schreibt wieder.
        Satz(cut);
        cut.FindAll(".epos-modulparameter input[inputmode=decimal]")[0].Input("43");
        Ok(cut);
        Assert.Equal(2, schreibvorgaenge);
    }

    /// <summary>
    /// <b>Eine ungültige Zahl sperrt das Speichern.</b> Das Zahlenfeld färbt sich und
    /// meldet seinen Zustand nach oben — dieselbe Naht, die der Katalogbrowser schon
    /// hatte.
    /// </summary>
    [Fact]
    public void Ein_Fehlerzustand_sperrt_das_Speichern()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder,
                           felderSpeichern: (n, _) => new KatalogSpeicherErgebnis(true, "", n));
        KatalogzeileWaehlen(cut);
        Satz(cut);   // UeS2: „Alle Daten" stehen in der Satz-Ueberlagerung, ihr OK speichert

        cut.FindAll(".epos-modulparameter input[inputmode=decimal]")[0].Input("42");
        Assert.False(cut.Find(".epos-satzueberlagerung-ok").HasAttribute("disabled"));

        cut.FindAll(".epos-modulparameter input[inputmode=decimal]")[0].Input("keine Zahl");
        Assert.True(cut.Find(".epos-satzueberlagerung-ok").HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>Der Befund des Katalogs zählt.</b> Lehnt der Speicherweg ab, meldet der
    /// Dialog — und der geänderte Stand bleibt stehen, damit der Anwender ihn
    /// verbessern kann, statt ihn zu verlieren.
    /// </summary>
    [Fact]
    public void Eine_abgelehnte_Speicherung_meldet_und_haelt_den_Stand()
    {
        var cut = Aufbauen(
            katalogfelder: Katalogfelder,
            felderSpeichern: (n, _) => new KatalogSpeicherErgebnis(
                false, "Der Datensatz ist schreibgeschützt.", n));

        KatalogzeileWaehlen(cut);
        Satz(cut);   // UeS2: „Alle Daten" stehen in der Satz-Ueberlagerung, ihr OK speichert
        cut.FindAll(".epos-modulparameter input[inputmode=decimal]")[0].Input("42");
        Ok(cut);

        Assert.Equal("Der Datensatz ist schreibgeschützt.", cut.Instance.Meldung);
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Equal("Der Datensatz ist schreibgeschützt.", cut.Find(".epos-satzueberlagerung-fuss [role=alert]").TextContent.Trim());

        // Das OK bleibt frei: Die Aenderung steht noch im Aufklapper.
        Assert.Equal("42", cut.FindAll(".epos-modulparameter input[inputmode=decimal]")[0].GetAttribute("value"));
        Assert.False(cut.Find(".epos-satzueberlagerung-ok").HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>Die Komponente formatiert und übersetzt nichts.</b> Unter <c>en-US</c>
    /// stehen genau die Texte da, die die Hülle hereingibt — samt der Zahlen, die
    /// dort schon in der Kultur des Anwenders formatiert wurden.
    /// </summary>
    [Fact]
    public void Auf_englisch_zeigt_die_Komponente_was_die_Huelle_hereingibt()
    {
        using var _ = new Kulturvorrichtung("en-US");

        var cut = Render<PhotovoltaikDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Modul 400", 31) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Detail, n => new ErzeugerDetail(n, "", Array.Empty<(string, string)>()))
            .Add(x => x.Katalogfelder, (Func<string, IReadOnlyList<BrowserFeldwert>?>)(_ =>
                new List<BrowserFeldwert>
                {
                    new() { Schluessel = "ETA", Bezeichnung = "Efficiency:", Einheit = "%",
                            Art = BrowserFeldArt.Zahl, Editierbar = false, Wert = "16.91" },
                    new() { Schluessel = "TECH", Bezeichnung = "Cell technology:",
                            Art = BrowserFeldArt.Text, Editierbar = false, Wert = "–" }
                }))
            .Add(x => x.LabelAlleParameter, "Show all data")
            .Add(x => x.Gesamtleistung, () => "8"));

        KatalogzeileWaehlen(cut);
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung

        // Aufgeklappt ist die Vorgabe (Anwenderentscheid 16.09.2026) - der Knopf
        // traegt seine englische Beschriftung, geklickt wird nicht: Ein Klick
        // klappte den Block ZU.
        var knopf = cut.Find(".epos-modulparameter-knopf");
        Assert.Contains("Show all data", knopf.TextContent);
        Assert.Equal("true", knopf.GetAttribute("aria-expanded"));

        var block = cut.Find(".epos-modulparameter");
        Assert.Equal(new[] { "Efficiency:", "Cell technology:" },
                     block.QuerySelectorAll(".epos-feld-text").Select(e => e.TextContent).ToArray());
        Assert.Equal(new[] { "16.91", "–" },
                     block.QuerySelectorAll("input").Select(e => e.GetAttribute("value")).ToArray());
    }

    // =================================================================================
    //  Stufe S2.1 / S2.3 / S2.5 - Anwenderentscheid W14a-E-10 vom 07.09.2026
    // =================================================================================

    /// <summary>Die Katalogliste - das UNTERE der beiden Raster.</summary>
    private static IReadOnlyList<AngleSharp.Dom.IElement> Katalogzeilen(
        IRenderedComponent<PhotovoltaikDialog> cut)
        => cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr").ToList();

    /// <summary>
    /// <b>S2.1 — der Filter sitzt im SPALTENKOPF.</b> Der Ausdruck steht im
    /// <c>Katalogfilterstand</c> des Wirtes, <c>Katalogfilter.Anwenden</c>
    /// schränkt die Menge im Kern ein, und das Raster bekommt die BEREITS
    /// eingeschränkte Liste (5.6.6 — gefiltert wird vor dem Raster, nie im Raster).
    /// </summary>
    [Fact]
    public void S2_1_Der_Spaltenfilter_schraenkt_die_Katalogliste_ein()
    {
        var cut = Aufbauen();
        Assert.Equal(2, Katalogzeilen(cut).Count);
        Assert.Equal("2 von 2 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);

        _filterstand.Setzen(Katalogfilterprofil.SpHersteller, "Solar");
        cut.Render();

        Assert.Single(Katalogzeilen(cut));
        Assert.Equal("1 von 2 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);
        Assert.Contains("Modul 500", Katalogzeilen(cut)[0].TextContent);

        // Der Trichter dieser Spalte ist jetzt GEFUELLT - im Markup, nicht nur
        // in der Farbe (Auflage des Anwenders zu Rev. 3).
        Assert.Contains(cut.FindAll(".epos-trichter-bild path"),
                        e => e.GetAttribute("fill") == "currentColor");

        // Und der Ruecksetzer der Suchzeile holt alles zurueck.
        cut.Find(".epos-katalog-ruecksetzer").Click();
        Assert.Equal(2, Katalogzeilen(cut).Count);
    }

    /// <summary>
    /// <b>S2.1 — jede Parameterspalte sortiert</b>, im Zyklus auf → ab → aus
    /// (höchstens eine Spalte zugleich, 5.6.2).
    /// </summary>
    [Fact]
    public void S2_1_Die_Katalogliste_laesst_sich_ueber_den_Spaltenkopf_sortieren()
    {
        var cut = Aufbauen();

        _filterstand.Sortieren(Katalogfilterprofil.SpPstc);
        cut.Render();
        Assert.Contains("Modul 400", Katalogzeilen(cut)[0].TextContent);

        _filterstand.Sortieren(Katalogfilterprofil.SpPstc);
        cut.Render();
        Assert.False(_filterstand.Aufsteigend);
        Assert.Contains("Modul 500", Katalogzeilen(cut)[0].TextContent);

        _filterstand.Sortieren(Katalogfilterprofil.SpPstc);
        Assert.Equal("", _filterstand.Sortierspalte);
    }

    /// <summary>
    /// <b>S2.1 — die Markierung hängt am BEZEICHNER.</b> Sie bleibt stehen, auch
    /// wenn ein Filter die Zeile ausblendet; der Übernahmeknopf bleibt frei und
    /// nimmt DENSELBEN Satz auf (Hausregel aus dem <c>EnergietraegerDialog</c>, W4).
    /// </summary>
    [Fact]
    public void S2_1_Die_Markierung_ueberlebt_einen_Filterwechsel()
    {
        var cut = Aufbauen();

        Katalogzeilen(cut)[0].QuerySelector(".epos-zeilenzelle--name")!.Click();
        Assert.False(cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].HasAttribute("disabled"));

        // Ein Filter, der GENAU diese Zeile ausblendet.
        _filterstand.Setzen(Katalogfilterprofil.SpHersteller, "Solar");
        cut.Render();

        Assert.Single(Katalogzeilen(cut));
        Assert.False(cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>S2.3 / Frage Q12 — „im Projekt verwendet".</b> EINMAL für die ganze Liste
    /// aus der Projektliste des Dialogs gestempelt, nicht je Zeile und nicht aus
    /// der Datenbank: Der Dialog schreibt erst beim OK zurück, eine Zählabfrage
    /// wäre nach der ersten Übernahme veraltet.
    /// </summary>
    [Fact]
    public void S2_3_Die_Verwendungsmarke_zaehlt_die_Projektliste()
    {
        var cut = Aufbauen(zeilen: new List<ErzeugerZeile> { Zeile(1, "Modul 400", 31) });

        var kopf = cut.FindAll(".epos-raster")[1]
                      .QuerySelectorAll("th").Select(e => e.TextContent.Trim()).ToList();
        // Konzept 4.10: die Verwendung steht als Marke am Bezeichner, die Spalte ist
        // nur noch waehlbar (standardmaessig aus).
        Assert.DoesNotContain(kopf, k => k.StartsWith("im Projekt verwendet"));

        var zeilen = Katalogzeilen(cut);
        Assert.Equal(2, zeilen.Count);
        Assert.Equal(1, zeilen.Count(z => z.QuerySelector(".epos-verwendet-marke") is not null));
        Assert.Equal(1, zeilen.Count(z => (z.ClassName ?? "").Contains("epos-zeile--verwendet")));

        var traegt = zeilen.First(z => z.QuerySelector(".epos-verwendet-marke") is not null);
        Assert.Contains("Modul 400", traegt.TextContent);
    }

    /// <summary>
    /// <b>S2.5 / Frage Q2 — der Filterstand überlebt Schließen und Öffnen.</b>
    /// Hier steht dafür ein EIGENER Stand statt des <c>Katalogfilterregister</c>
    /// (xunit fährt Testklassen nebeneinander); dass das Register ihn wirklich
    /// zwischen Verwaltung und Projektdialog teilt, prüft
    /// <c>EPOS.Kern.Tests/KatalogfilterstandTests</c>.
    /// </summary>
    [Fact]
    public void S2_5_Der_Filterstand_ueberlebt_einen_zweiten_Aufbau()
    {
        var ersterAufbau = Aufbauen();
        _filterstand.Setzen(Katalogfilterprofil.SpHersteller, "Solar");
        ersterAufbau.Render();
        Assert.Single(Katalogzeilen(ersterAufbau));

        // Der Dialog geht zu und wieder auf - derselbe Stand, dieselbe Sicht.
        var zweiterAufbau = Aufbauen();

        Assert.Single(Katalogzeilen(zweiterAufbau));
        Assert.Equal("1 von 2 Sätzen", zweiterAufbau.Find(".epos-katalog-treffer").TextContent);
        Assert.Single(zweiterAufbau.FindAll(".epos-katalog-ruecksetzer"));
    }

    // =================================================================================
    // ET-5 (Anwenderentscheid 08.09.2026): der Energietraeger der Anlage, Gruppe > Art
    // =================================================================================

    private static IReadOnlyList<EnergietraegerWahl.Eintrag> Traegerkatalog() => new[]
    {
        new EnergietraegerWahl.Eintrag(11, "Gas", "Erdgas E"),
        new EnergietraegerWahl.Eintrag(60, "Strom", "Elektrische Energie"),
        new EnergietraegerWahl.Eintrag(58, "Strom", "Elektrische Energie 2")
    };

    [Fact]
    public void Die_markierte_Anlage_zeigt_ihren_Energietraeger_und_meldet_den_Wechsel()
    {
        (ErzeugerZeile Zeile, int Neu)? gemeldet = null;
        ErzeugerZeile zeile = Zeile(1, "Modul 400", 31);
        zeile.CarrierId = 60;
        var cut = Render<PhotovoltaikDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { zeile })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Detail, n => Detail(n))
            .Add(x => x.Gesamtleistung, () => "8")
            .Add(x => x.KatalogLoeschen, _ => "")
            .Add(x => x.Traegerkatalog, Traegerkatalog())
            .Add(x => x.TraegerWechseln, (ErzeugerZeile z, int neu) => gemeldet = (z, neu)));

        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung

        var selects = cut.Find(".epos-traegerwahl").QuerySelectorAll("select");
        Assert.Equal(2, selects.Length);
        Assert.Contains("Strom", selects[0].InnerHtml);
        Assert.Contains("Elektrische Energie 2", selects[1].InnerHtml);
        Assert.DoesNotContain("Erdgas E", selects[1].InnerHtml);

        selects[1].Change("58");

        Assert.Equal(58, gemeldet?.Neu);
        Assert.Same(zeile, gemeldet?.Zeile);
        Assert.Equal(58, zeile.CarrierId);
    }

    [Fact]
    public void Ohne_Traegerkatalog_steht_keine_Traegerwahl()
    {
        var cut = Aufbauen();
        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        Assert.Empty(cut.FindAll(".epos-traegerwahl"));
    }

    // =====================================================================
    //  Der Hilfe-Assistent: die Felder der Bausteine (Welle KI-F1)
    // =====================================================================

    /// <summary>
    /// Die Maske gibt seit dieser Welle auch die MODELLFELDER heraus — sie stehen im
    /// Baustein <c>PvModellFelder</c> und binden an dieselbe Projektzeile.
    /// </summary>
    /// <remarks>
    /// <b>Monotone Aussage</b> (Muster <c>KiMaskenhakenTests</c>): Geprüft wird, was
    /// nach dem Zeichnen DA ist. Die Brücke ist prozessweiter Zustand.
    /// </remarks>
    [Fact]
    public void Der_Assistent_setzt_die_Systemverluste_der_gewaehlten_Zeile()
    {
        ErzeugerZeile zeile = Zeile(1, "Anlage A", 100);
        zeile.Systemverluste = 3.0;
        Aufbauen(zeilen: new List<ErzeugerZeile> { zeile });

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.PHOTOVOLTAIK));

        KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.PHOTOVOLTAIK, "systemverluste");
        Assert.NotNull(zugang);
        Assert.Equal(3.0, zugang.Lesen());

        zugang.Setzen(5.5);
        Assert.Equal(5.5, zeile.Systemverluste);
    }

    /// <summary>
    /// Die STRÄNGE sind eine Liste: Je vorhandener Strangzeile wird aus der
    /// Spaltendeklaration ein gewöhnliches Feld, und sein Klartextname trägt den
    /// Bezeichner des Strangs.
    /// </summary>
    [Fact]
    public void Der_Assistent_liest_und_setzt_die_Straenge_je_Zeile()
    {
        ErzeugerZeile zeile = Zeile(1, "Anlage A", 100);
        zeile.MitWechselrichter = true;
        zeile.Straenge.Add(new StrangZeile { Rang = 1, Bezeichner = "Dach Süd", ModuleReihe = 12 });
        zeile.Straenge.Add(new StrangZeile { Rang = 2, Bezeichner = "Dach West", ModuleReihe = 8 });

        Aufbauen(zeilen: new List<ErzeugerZeile> { zeile });

        var reihen = KiMaskenbruecke.Lesen(KiMaskennamen.PHOTOVOLTAIK)
                                    .Where(w => w.Name.StartsWith("strang_module_reihe"))
                                    .ToList();

        Assert.Equal(2, reihen.Count);
        Assert.Contains(reihen, w => w.Anzeigename.Contains("Dach Süd"));
        Assert.Contains(reihen, w => w.Anzeigename.Contains("Dach West"));

        // Der Schluessel der ZWEITEN Zeile - er trägt ihre Nummer, der Anzeigename
        // ihren Bezeichner.
        string schluessel = reihen.First(w => w.Anzeigename.Contains("Dach West")).Name;

        KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.PHOTOVOLTAIK, schluessel);
        Assert.NotNull(zugang);
        Assert.Equal(8, zugang.Lesen());

        zugang.Setzen(16);
        Assert.Equal(16, zeile.Straenge[1].ModuleReihe);
    }

    /// <summary>
    /// <b>Der ZEUGE der Sichtklasse <c>PhotovoltaikKiSicht</c></b> (Welle KI‑F7,
    /// Anwenderentscheid 21.09.2026): Die Maske gibt seit dieser Welle auch die zwei
    /// AUSLEGUNGSTEMPERATUREN heraus — sie stehen im Strangabschnitt, gehören aber dem
    /// PROJEKT und hängen deshalb nicht an der Anlagenzeile.
    /// </summary>
    /// <remarks>
    /// <b>Gesetzt wird über den Weg der Maske.</b> Nach der Setzung steht die Zahl im
    /// lebenden Feld des Strangbausteins UND in den Projekteinstellungen — das zweite
    /// misst der Rückruf <c>AuslegungstemperaturenSetzen</c>. Genau dafür gibt es die
    /// Sichtklasse: An <c>ErzeugerZeile</c> gäbe es diese Größe gar nicht.
    /// </remarks>
    [Fact]
    public void Der_Assistent_setzt_die_Auslegungstemperatur_des_Projekts()
    {
        double? kalt = null, heiss = null;
        ErzeugerZeile zeile = Zeile(1, "Anlage A", 100);

        IRenderedComponent<PhotovoltaikDialog> cut = Render<PhotovoltaikDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { zeile })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Detail, n => Detail(n))
            .Add(x => x.Gesamtleistung, () => "8")
            .Add(x => x.AuslegungKalt, -12.0)
            .Add(x => x.AuslegungHeiss, 65.0)
            .Add(x => x.AuslegungstemperaturenSetzen,
                 (double? k, double? h) => { kalt = k; heiss = h; }));

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.PHOTOVOLTAIK));

        KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.PHOTOVOLTAIK, "auslegung_kalt");
        Assert.NotNull(zugang);
        Assert.Equal(-12.0, zugang.Lesen());
        Assert.True(zugang.Setzbar);

        // Der Assistent ruft aus seinem EIGENEN Faden; der Weg der Maske geht über den
        // Blazor-Verteiler und ist damit nicht sofort fertig (Hausregel EPOS.UI).
        zugang.Setzen(-15.5);

        cut.WaitForAssertion(() => Assert.Equal(-15.5, kalt));
        Assert.Equal(65.0, heiss);
        Assert.Equal(-15.5, KiMaskenbruecke.Feldzugang(KiMaskennamen.PHOTOVOLTAIK,
                                                       "auslegung_kalt").Lesen());

        // Die Felder der ZEILE gehen weiterhin an die Zeile — die Sicht reicht sie
        // unverändert durch.
        KiMaskenbruecke.Feldzugang(KiMaskennamen.PHOTOVOLTAIK, "neigung").Setzen(35);
        Assert.Equal(35, zeile.Neigung);
    }

    /// <summary>
    /// <b>Das RECHENMODELL ist ein Wahlfeld mit den Einträgen der Maske</b> (KI‑D‑Q6,
    /// KI‑D‑Q7): „Einfach" und „Erweitert" stehen im Auswahlfeld des Bausteins, und
    /// über genau diese Texte trifft der Assistent den Wahrheitswert der Anlage.
    /// </summary>
    [Fact]
    public void Der_Assistent_waehlt_das_Rechenmodell_ueber_seinen_Text()
    {
        ErzeugerZeile zeile = Zeile(1, "Anlage A", 100);
        Aufbauen(zeilen: new List<ErzeugerZeile> { zeile });

        KiFeldzugang modell =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.PHOTOVOLTAIK, "modell_erweitert");
        Assert.NotNull(modell);
        Assert.True(modell.IstWahl);

        IReadOnlyList<KiWahleintrag> eintraege = modell.Wahleintraege();
        Assert.Equal(2, eintraege.Count);

        KiFeldumsetzung wahl = KiFeldwandler.Wandle(modell, eintraege[1].Text);
        Assert.True(wahl.Ok, wahl.Grund);
        modell.Setzen(wahl.Wert);

        Assert.True(zeile.ModellErweitert);
    }

    // =================================================================================
    // Die Modulspalte der Strangtabelle: nur die PROJEKTMODULE (26.09.2026)
    // =================================================================================

    /// <summary>Der Modulkatalog, den die Hülle als <c>Strangmodule</c> hereinreicht.</summary>
    private static readonly (int Id, string Text)[] MODULKATALOG =
    {
        (31, "Modul 400"),
        (32, "Modul 500"),
        (33, "Modul 600"),
        (34, "Modul 700")
    };

    private static ErzeugerZeile Strangzeile(int schluessel, string name, int geraetId,
                                             params StrangZeile[] straenge)
    {
        ErzeugerZeile z = Zeile(schluessel, name, geraetId);
        z.MitWechselrichter = true;
        z.Straenge.AddRange(straenge.Length > 0
            ? straenge
            : new[] { new StrangZeile { Rang = 1, ModuleReihe = 10 } });
        return z;
    }

    /// <summary>Die Modulklappliste der Strangtabelle — sie steht in der Überlagerung „Stränge und Wechselrichter…".</summary>
    private static IReadOnlyList<string> Modulklappliste(IRenderedComponent<PhotovoltaikDialog> cut)
    {
        if (!cut.Instance.StraengeOffen)
        {
            Satz(cut);   // UeS2: „Stränge und Wechselrichter…" steht in der Satz-Ueberlagerung
            cut.Find(".epos-knopf--straenge").Click();
        }
        return cut.FindComponents<Auswahlfeld>()
                  .First(f => f.Instance.Kurzname == "Modul")
                  .Instance.Eintraege.Select(e => e.Text).ToList();
    }

    /// <summary>
    /// <b>Die Klappliste je Strang bietet nur die dem Projekt zugeordneten Module</b>
    /// (Anwenderwunsch 26.09.2026) — „(Modul der Anlage)" voran, dann die Module der
    /// Projektliste in deren Reihenfolge, jedes einmal; der übrige Katalog fehlt.
    /// </summary>
    [Fact]
    public void Die_Strangmodulliste_zeigt_nur_die_Projektmodule()
    {
        var zeilen = new List<ErzeugerZeile>
        {
            Strangzeile(1, "Modul 600", 33),
            Strangzeile(2, "Modul 400", 31),
            Strangzeile(3, "Modul 600", 33)
        };
        var cut = Aufbauen(zeilen, strangmodule: MODULKATALOG);
        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[0].Click();

        Assert.Equal(new[] { "(Modul der Anlage)", "Modul 600", "Modul 400" },
                     Modulklappliste(cut));
    }

    /// <summary>
    /// <b>Ein gespeichertes Strangmodul, das nicht mehr im Projekt ist, bleibt als
    /// Eintrag stehen</b> — gekennzeichnet und gewählt, damit nichts still verloren geht.
    /// </summary>
    [Fact]
    public void Ein_Strangmodul_ausserhalb_des_Projekts_bleibt_als_Eintrag()
    {
        var zeilen = new List<ErzeugerZeile>
        {
            Strangzeile(1, "Modul 400", 31, new StrangZeile
            {
                Rang = 1, ModuleReihe = 10, ModulId = 5150, ModulName = "Modul 700"
            })
        };
        var cut = Aufbauen(zeilen, strangmodule: MODULKATALOG);
        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[0].Click();

        Assert.Equal(new[] { "(Modul der Anlage)", "Modul 400", "Modul 700 (nicht mehr im Projekt)" },
                     Modulklappliste(cut));
        var wahl = cut.FindComponents<Auswahlfeld>().First(f => f.Instance.Kurzname == "Modul");
        Assert.Equal(-1, wahl.Instance.Auswahl);
        Assert.Equal(5150, zeilen[0].Straenge[0].ModulId);
    }

    /// <summary>
    /// <b>Die Liste folgt der Zuordnung im selben Dialog:</b> Ein neu übernommenes Modul
    /// erscheint sofort in der Strangliste, ein entferntes verschwindet.
    /// </summary>
    [Fact]
    public void Die_Strangmodulliste_folgt_Zuordnen_und_Entfernen()
    {
        var zeilen = new List<ErzeugerZeile> { Strangzeile(1, "Modul 400", 31) };
        var cut = Aufbauen(zeilen,
                           aufnehmen: id => new AufnahmeErgebnis(Strangzeile(9, "Modul 500", 32)),
                           strangmodule: MODULKATALOG);
        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        Assert.Equal(new[] { "(Modul der Anlage)", "Modul 400" }, Modulklappliste(cut));

        // Zuordnen: Katalogzeile "Modul 500" wählen und übernehmen.
        KatalogzeileWaehlen(cut, 1);
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();
        Assert.Equal(new[] { "(Modul der Anlage)", "Modul 400", "Modul 500" }, Modulklappliste(cut));

        // Entfernen: die neue Zeile wird gewählt und geht wieder hinaus (die Mehrfachwahl der Projektliste
        // trägt sonst noch die zuerst gewählte Zeile, Katalogauswahl V1).
        ProjektzeileWaehlen(cut, 1);
        cut.FindAll(".epos-zweispalten-knopf--entfernen")[0].Click();
        Assert.Equal(new[] { "(Modul der Anlage)", "Modul 400" }, Modulklappliste(cut));
    }

    /// <summary>
    /// <b>Ein Satz, den der Katalog der Hülle (noch) nicht führt</b> — etwa gerade neu
    /// angelegt —, steht mit dem Bezeichner der Projektzeile in der Liste.
    /// </summary>
    [Fact]
    public void Ein_Projektmodul_ohne_Katalogeintrag_steht_mit_seinem_Bezeichner()
    {
        var zeilen = new List<ErzeugerZeile> { Strangzeile(1, "Modul Neu", 99) };
        var cut = Aufbauen(zeilen, strangmodule: MODULKATALOG);
        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[0].Click();

        Assert.Equal(new[] { "(Modul der Anlage)", "Modul Neu" }, Modulklappliste(cut));
    }

    // =================================================================================
    // Katalogauswahl V1, Stufe 3: Rückweg „In die Datenbank übernehmen…" (5.2, KA‑E‑9)
    // =================================================================================

    private static Rueckwegwege Rueckweg(IReadOnlyList<Rueckwegvorschlag> zeilen, List<Rueckwegwahl>? geschrieben = null,
                                         Func<string, bool>? belegt = null, KatalogSpeicherErgebnis? ergebnis = null,
                                         List<IReadOnlyList<int>>? gefragt = null)
        => new()
        {
            Vorschau = ids => { gefragt?.Add(ids); return zeilen; },
            NameBelegt = belegt ?? (_ => false),
            Uebernehmen = w => { geschrieben?.AddRange(w); return ergebnis ?? new KatalogSpeicherErgebnis(true, "2 Sätze übernommen", ""); },
        };

    [Fact]
    public void S3b_Der_Knopf_steht_nach_Bearbeiten_und_fragt_je_Projektkopie_einmal()
    {
        var gefragt = new List<IReadOnlyList<int>>();
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Modul 400", 31), Zeile(2, "Modul 500", 32), Zeile(3, "Modul 400 Ost", 31) };
        var cut = Aufbauen(zeilen, projektsatzWege: Wege(),
                           rueckwegWege: Rueckweg(new[] { new Rueckwegvorschlag(31, "Modul 400", "", Rueckwegsperre.UrsprungUnbekannt, "Modul 400") },
                                                  gefragt: gefragt));

        var knoepfe = cut.Find(".epos-zweispalten-bereich--projekt > .epos-zweispalten-kopfleiste").QuerySelectorAll("button").ToList();
        int bearbeiten = knoepfe.FindIndex(k => k.ClassList.Contains("epos-knopf--bearbeiten-projekt"));
        int rueckweg = knoepfe.FindIndex(k => k.ClassList.Contains("epos-knopf--rueckweg"));
        Assert.True(bearbeiten >= 0 && rueckweg == bearbeiten + 1);

        foreach (int i in new[] { 0, 1, 2 })
            cut.FindAll(".epos-raster")[0].QuerySelectorAll("td .epos-wahlkaestchen")[i].Click();
        cut.Find(".epos-knopf--rueckweg").Click();
        Assert.Equal(new[] { 31, 32 }, Assert.Single(gefragt).ToArray());
        Assert.NotNull(cut.Instance.Rueckweg);
    }

    [Fact]
    public void S3b_Uebernehmen_meldet_und_liest_den_Katalog_neu()
    {
        int gelesen = 0;
        var geschrieben = new List<Rueckwegwahl>();
        var zeilen = new[] { new Rueckwegvorschlag(31, "Modul 400", "", Rueckwegsperre.UrsprungUnbekannt, "Modul 400 (Projekt)") };
        var cut = Aufbauen(rueckwegWege: Rueckweg(zeilen, geschrieben),
                           katalogzeilen: () => { gelesen++; return Katalogzeilen(); });
        int vorher = gelesen;
        cut.Find(".epos-knopf--rueckweg").Click();
        cut.Find(".epos-rueckweg-uebernehmen").Click();

        Assert.Equal(new[] { new Rueckwegwahl(31, false, "Modul 400 (Projekt)") }, geschrieben.ToArray());
        Assert.Null(cut.Instance.Rueckweg);
        Assert.Equal("2 Sätze übernommen", cut.Instance.Meldung);
        Assert.True(gelesen > vorher);
    }

    [Fact]
    public void S3b_Der_Hinweis_nennt_was_im_Projekt_bleibt()
    {
        var zeilen = new[] { new Rueckwegvorschlag(31, "Modul 400", "", Rueckwegsperre.UrsprungUnbekannt, "Modul 400") };
        var cut = Render<PhotovoltaikDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Modul 400", 31) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.RueckwegWege, Rueckweg(zeilen))
            .Add(x => x.RueckwegBleibtText, "Im Projekt bleiben: Stränge und Wechselrichterzuordnung."));
        cut.Find(".epos-knopf--rueckweg").Click();
        Assert.Contains("Im Projekt bleiben: Stränge und Wechselrichterzuordnung.", cut.Find(".epos-rueckweg").TextContent);
    }

    [Fact]
    public void S3b_Ohne_Wege_kein_Knopf()
    {
        var cut = Aufbauen(projektsatzWege: Wege());
        Assert.Empty(cut.FindAll(".epos-knopf--rueckweg"));
    }

    // =================================================================================
    // UeS1: Satz-Ueberlagerung in voller Hoehe
    // =================================================================================

    private const string FeldImKoerper = ".epos-satzueberlagerung-koerper .epos-modulparameter input[type=text]:not([readonly])";

    /// <summary>UeS1: „Bearbeiten…" EINER Projektkopie (auch zweier Zeilen desselben Moduls) öffnet die Satz-Überlagerung.</summary>
    [Fact]
    public void UeS1_Bearbeiten_einer_Projektkopie_oeffnet_die_Satzueberlagerung()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Modul 400", 31), Zeile(2, "Modul 400 Ost", 31) };
        var cut = Aufbauen(zeilen, projektsatzWege: Wege());

        foreach (int i in new[] { 0, 1 })
            cut.FindAll(".epos-raster")[0].QuerySelectorAll("td .epos-wahlkaestchen")[i].Click();
        cut.Find(".epos-knopf--bearbeiten-projekt").Click();

        Assert.Null(cut.Instance.Bearbeitung);
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Equal(Resource.AUSWAHL_MARKE_PROJEKTSATZ,
                     cut.Find(".epos-ueberlagerung--satz .epos-ueberlagerung-kopf .epos-zweispalten-marke--satz").TextContent);
        Assert.Single(cut.FindAll(".epos-satzueberlagerung-koerper .epos-modulparameter"));
        Assert.Single(cut.FindAll(".epos-satzueberlagerung-koerper .epos-anlagenblock"));
        // „Stränge und Wechselrichter…" bleibt; der Speichern-Knopf der Satzflaeche steht nicht.
        Assert.Single(cut.FindAll(".epos-satzueberlagerung-koerper .epos-knopf--straenge"));
        Assert.Empty(cut.FindAll(".epos-satzueberlagerung-koerper .epos-speichervermerk"));
    }

    /// <summary>UeS1: OK schreibt die Projektkopie über den Speicherweg der Satzfläche und schließt.</summary>
    [Fact]
    public void UeS1_OK_der_Satzueberlagerung_schreibt_die_Projektkopie()
    {
        var gespeichert = new List<(int Id, IReadOnlyList<BrowserFeldwert> Felder)>();
        var cut = Aufbauen(projektsatzWege: Wege(gespeichert));

        cut.Find(".epos-knopf--bearbeiten-projekt").Click();
        cut.Find(FeldImKoerper).Input("Neuwerk");
        cut.Find(".epos-satzueberlagerung-ok").Click();

        var satz = Assert.Single(gespeichert);
        Assert.Equal(31, satz.Id);
        Assert.Contains(satz.Felder, f => f.Wert == "Neuwerk");
        Assert.False(cut.Instance.SatzUeberlagerungOffen);
        Assert.Empty(cut.FindAll(".epos-modulparameter"));   // UeS2: zu steht nur die Zusammenfassung
    }

    /// <summary>UeS1: Wirft der Speicherweg (KT‑4-Fangweg), bleibt die Überlagerung mit dem Grund offen.</summary>
    [Fact]
    public void UeS1_Ein_werfender_Speicherweg_haelt_die_Satzueberlagerung_offen()
    {
        var wege = new Satzbearbeitungswege
        {
            Lesen = id => Katalogfelder("Satz " + id),
            Speichern = _ => throw new InvalidOperationException("Datenbank gesperrt")
        };
        var cut = Aufbauen(projektsatzWege: wege);

        cut.Find(".epos-knopf--bearbeiten-projekt").Click();
        cut.Find(FeldImKoerper).Input("Neuwerk");
        cut.Find(".epos-satzueberlagerung-ok").Click();

        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.False(string.IsNullOrWhiteSpace(cut.Find(".epos-satzueberlagerung-fuss [role=alert]").TextContent));
        Assert.Equal("Neuwerk", cut.Find(FeldImKoerper).GetAttribute("value"));
    }

    /// <summary>UeS1: Abbrechen verwirft die Feldänderung und stellt Neigung, Azimut und Albedo wie beim Öffnen her.</summary>
    [Fact]
    public void UeS1_Abbrechen_verwirft_Feld_und_stellt_die_Anlagenfelder_her()
    {
        var gespeichert = new List<(int Id, IReadOnlyList<BrowserFeldwert> Felder)>();
        var uebernommen = new List<ErzeugerZeile>();
        var zeile = Zeile(1, "Modul 400", 31);
        zeile.Albedo = 0.3;
        var cut = Aufbauen(new List<ErzeugerZeile> { zeile }, projektsatzWege: Wege(gespeichert), uebernehmen: uebernommen.Add);

        cut.Find(".epos-knopf--bearbeiten-projekt").Click();
        cut.Find(FeldImKoerper).Input("Neuwerk");
        var block = cut.Find(".epos-satzueberlagerung-koerper .epos-anlagenblock");
        block.QuerySelectorAll("input")[0].Input("45");
        zeile.Albedo = 0.6;                                  // wie aus PvModellFelder
        Assert.Equal(45, zeile.Neigung);

        cut.Find(".epos-satzueberlagerung-abbrechen").Click();

        Assert.Empty(gespeichert);
        Assert.False(cut.Instance.SatzUeberlagerungOffen);
        Assert.Equal(30, zeile.Neigung);
        Assert.Equal(0.3, zeile.Albedo);
        Assert.Same(zeile, uebernommen[^1]);
        // UeS2: wieder geoeffnet steht der verworfene Stand nicht mehr da.
        Assert.Equal("30", Satz(cut).QuerySelector(".epos-anlagenblock input")!.GetAttribute("value"));
        Assert.DoesNotContain(cut.FindAll(".epos-satzueberlagerung-koerper .epos-modulparameter input"), e => e.GetAttribute("value") == "Neuwerk");
    }

    /// <summary>UeS1: Ein gesperrter Katalogsatz allein öffnet die Satz-Überlagerung nur lesend.</summary>
    [Fact]
    public void UeS1_Ein_gesperrter_Katalogsatz_oeffnet_die_Ueberlagerung_nur_lesend()
    {
        var cut = Aufbauen(katalogsatzWege: Wege(), katalogzeilen: () => MitSchloss(31),
                           katalogfelder: Katalogfelder, felderSpeichern: (_, _) => new KatalogSpeicherErgebnis(true, "ok", ""),
                           editorGaben: _ => new Dictionary<string, object>());

        KatalogzeileWaehlen(cut);
        cut.Find(".epos-knopf--bearbeiten-katalog").Click();

        Assert.False(cut.Instance.EditorOffen);
        Assert.Null(cut.Instance.Bearbeitung);
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Contains(Resource.ADM_SCHLOSS_ERST_AUFHEBEN, cut.Find(".epos-satzueberlagerung-hinweis").TextContent);
        Assert.Empty(cut.FindAll(".epos-satzueberlagerung-ok"));
        Assert.Empty(cut.FindAll(".epos-satzueberlagerung-koerper .epos-modulparameter input[type=text]:not([readonly])"));
        cut.Find(".epos-satzueberlagerung-schliessen").Click();
        Assert.False(cut.Instance.SatzUeberlagerungOffen);
    }

    // =================================================================================
    // UeS2: Zusammenfassung der Detailzeile und Stift je Zeile (Anwenderentscheid 10.10.2026)
    // =================================================================================

    private static Dictionary<string, string> Angaben(IRenderedComponent<PhotovoltaikDialog> cut)
        => cut.FindAll(".epos-satzzusammenfassung-angabe")
              .ToDictionary(a => a.QuerySelector("dt")!.TextContent, a => a.QuerySelector("dd")!.TextContent);

    [Fact]
    public void UeS2_Die_Zusammenfassung_des_Projektsatzes_nennt_die_Anlagendaten()
    {
        var zeile = Zeile(1, "Modul 400", 31);
        zeile.Albedo = 0.3;
        zeile.ModellErweitert = true;
        var cut = Render<PhotovoltaikDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { zeile })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Detail, n => new ErzeugerDetail(n, "", new[] { ("Leistung [W]:", "999") },
                                                        Kennwerte: new ErzeugerKennwerte { ModulleistungW = 400 }))
            .Add(x => x.Gesamtleistung, () => "8")
            .Add(x => x.KostenOeffnen, (_, _) => Task.CompletedTask)
            .Add(x => x.Kostensumme, _ => (9000.0, 150.0)));

        cut.Find(".epos-zweispalten-satzzeile").Click();

        var k = CultureInfo.CurrentCulture;
        var angaben = Angaben(cut);
        Assert.Equal("20", angaben[Resource.AUSWAHL_ZF_ANZAHL]);
        Assert.Equal(Satzangaben.Zahl(8, "kWp"), angaben[Resource.AUSWAHL_ZF_LEISTUNG_GESAMT]);   // 400 W x 20
        Assert.Equal("30° / 180°", angaben[Resource.AUSWAHL_ZF_AUSRICHTUNG]);
        Assert.Equal(0.3.ToString("0.##", k), angaben[Resource.AUSWAHL_ZF_ALBEDO]);
        Assert.Equal(new PvModellTexte().ModellErweitert, angaben[Resource.AUSWAHL_ZF_ERTRAGSMODELL]);
        Assert.Equal(string.Format(k, Resource.AUSWAHL_ZF_EURO, 9000.0), angaben[Resource.AUSWAHL_ZF_INVEST]);
        Assert.Equal(string.Format(k, Resource.AUSWAHL_ZF_EURO_JAHR, 150.0), angaben[Resource.AUSWAHL_ZF_BETRIEB]);
        // Kostenknoepfe, Anlagenblock und „Stränge und Wechselrichter…" stehen in der Ueberlagerung.
        Assert.Empty(cut.FindAll(KOSTENKNOEPFE));
        Assert.Empty(cut.FindAll(".epos-anlagenblock"));
        Assert.Empty(cut.FindAll(".epos-knopf--straenge"));
        var koerper = Satz(cut);
        Assert.NotEmpty(koerper.QuerySelectorAll(KOSTENKNOEPFE));
        Assert.Single(koerper.QuerySelectorAll(".epos-knopf--straenge"));
    }

    [Fact]
    public void UeS2_Die_Anlagenleistung_der_Huelle_geht_vor_und_leer_heisst_Albedo_0_2()
    {
        var cut = Render<PhotovoltaikDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Modul 400", 31) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Detail, n => Detail(n))
            .Add(x => x.AnlagenKwp, _ => 5.5));

        cut.Find(".epos-zweispalten-satzzeile").Click();

        var k = CultureInfo.CurrentCulture;
        var angaben = Angaben(cut);
        Assert.Equal(Satzangaben.Zahl(5.5, "kWp"), angaben[Resource.AUSWAHL_ZF_LEISTUNG_GESAMT]);
        Assert.Equal(0.2.ToString("0.##", k), angaben[Resource.AUSWAHL_ZF_ALBEDO]);
        Assert.Equal(new PvModellTexte().ModellEinfach, angaben[Resource.AUSWAHL_ZF_ERTRAGSMODELL]);
        Assert.False(angaben.ContainsKey(Resource.AUSWAHL_ZF_INVEST));
    }

    [Fact]
    public void UeS2_Die_Zusammenfassung_des_Katalogsatzes_nennt_Hersteller_Leistung_Wirkungsgrad_und_Flaeche()
    {
        var cut = Aufbauen();
        KatalogzeileWaehlen(cut, 1);
        cut.Find(".epos-zweispalten-satzzeile").Click();

        var texte = cut.FindAll(".epos-satzzusammenfassung-angabe dd").Select(e => e.TextContent).ToList();
        Assert.Equal(4, texte.Count);
        Assert.Equal("Solar AG", texte[0]);
        Assert.Equal("500 W", texte[1]);
        Assert.EndsWith(" %", texte[2]);
        Assert.EndsWith(" m²", texte[3]);
        Assert.DoesNotContain("Mono-c-Si", texte);   // nur die vier Angaben des Auftrags
    }

    [Fact]
    public void UeS2_Der_Stift_der_Projektzeile_oeffnet_die_Projektkopie_in_der_Ueberlagerung()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Modul 400", 31), Zeile(2, "Modul 500", 32) };
        var cut = Aufbauen(zeilen, projektsatzWege: Wege());

        var stifte = cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-zeilenstift");
        Assert.Equal(2, stifte.Length);
        stifte[1].Click();

        Assert.Equal(32, cut.Instance.Projektzeile!.GeraetId);
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Equal(Resource.AUSWAHL_MARKE_PROJEKTSATZ,
                     cut.Find(".epos-ueberlagerung--satz .epos-ueberlagerung-kopf .epos-zweispalten-marke--satz").TextContent);
        Assert.NotEmpty(cut.FindAll(FeldImKoerper));
        Assert.Single(cut.FindAll(".epos-satzueberlagerung-ok"));
    }

    [Fact]
    public void UeS2_Ohne_Weg_der_Projektkopie_kein_Stift_und_das_OK_behaelt_die_Anlagenfelder()
    {
        var cut = Aufbauen(uebernehmen: _ => { });
        Assert.Empty(cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-zeilenstift"));
        Assert.Equal(2, cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenstift").Length);

        // Der Assistent hat keinen Weg der Projektkopie - die Ueberlagerung ist trotzdem nicht nur lesend.
        Satz(cut).QuerySelector(".epos-anlagenblock")!.QuerySelectorAll("input[inputmode=numeric]")[0].Input("40");
        Ok(cut);
        Assert.Equal(40, cut.Instance.Projektzeile!.Neigung);
    }

    [Fact]
    public void UeS2_Der_Stift_einer_gesperrten_Katalogzeile_oeffnet_nur_lesend()
    {
        var cut = Aufbauen(katalogzeilen: () => MitSchloss(31), katalogfelder: Katalogfelder,
                           felderSpeichern: (_, _) => new KatalogSpeicherErgebnis(true, "ok", ""),
                           editorGaben: _ => new Dictionary<string, object>());

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenstift")[0].Click();

        Assert.Equal("Modul 400", cut.Instance.Katalogzeile!.Bezeichner);
        Assert.False(cut.Instance.EditorOffen);
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Contains(Resource.ADM_SCHLOSS_ERST_AUFHEBEN, cut.Find(".epos-satzueberlagerung-hinweis").TextContent);
        Assert.Empty(cut.FindAll(".epos-satzueberlagerung-ok"));
    }

    [Fact]
    public void UeS2_Der_Stift_einer_ungesperrten_Katalogzeile_oeffnet_den_Katalogeditor()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder, editorGaben: _ => new Dictionary<string, object>());

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenstift")[1].Click();

        Assert.Equal("Modul 500", cut.Instance.Katalogzeile!.Bezeichner);
        Assert.True(cut.Instance.EditorOffen);
        Assert.False(cut.Instance.SatzUeberlagerungOffen);
    }

    /// <summary>UeS2b: „Grundlagen" und „Berechnung" im Kopf der Detailzeile, offen in der Überlagerung — je Ansicht einmal.</summary>
    [Fact]
    public void UeS2b_Die_Infoknoepfe_stehen_im_Kopf_der_Detailzeile_und_nie_doppelt()
    {
        var cut = Aufbauen(projektsatzWege: Wege());
        string[] schluessel = { "Form_PV.Grundlagen", "Form_PV.Berechnung" };

        foreach (string s in schluessel)
            Assert.Single(cut.FindComponents<InfoKnopf>(), k => k.Instance.Schluessel == s);
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-satzkopf .epos-zweispalten-satzkopfknoepfe .epos-hilfepille").Count);

        Satz(cut);
        foreach (string s in schluessel)
            Assert.Single(cut.FindComponents<InfoKnopf>(), k => k.Instance.Schluessel == s);
        Assert.Empty(cut.FindAll(".epos-zweispalten-satzkopfknoepfe"));
    }
}
