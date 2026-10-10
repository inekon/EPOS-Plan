using System.Globalization;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Allgemein;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Verwaltung Stromspeicher (iU9-W6.6) als Wirt der Katalogauswahl V1, Stufe 3 (Konzept Projektdialoge mit
/// Katalogauswahl 4.2, 4.6, 4.9): Summe kWh in der Projekt-Kopfleiste, Bearbeiten… und „In die Datenbank
/// übernehmen…" im Projekt, Vergleichen, Schloss, Löschen, Bearbeiten (ohne Neu) im Katalogfuß, Kosten und
/// Energieträger nur beim Projektsatz, „Alle Daten" auch für die Projektkopie.
/// </summary>
public class StromspeicherDialogTests : EposBunitContext
{
    /// <summary>Der Filterstand DIESES Prüfstands — das Register lebt prozessweit.</summary>
    private readonly Katalogfilterstand _filterstand = new();

    /// <summary>Das PROFIL des Projektdialogs: die acht Spalten der Verwaltung und „im Projekt verwendet" (Q12).</summary>
    private static readonly Katalogfilterprofil Profil =
        Katalogfilterprofil.MitVerwendung(Anlagenart.Stromspeicher,
            s => Resource.ResourceManager.GetString(s) ?? s);

    private static IReadOnlyList<Katalogfilterzeile> Katalogzeilen() => new[]
    {
        new Katalogfilterzeile(41, "Speicher 10")
            .MitText(Katalogfilterprofil.SpBezeichner, "Speicher 10")
            .MitText(Katalogfilterprofil.SpHersteller, "Werk A")
            .MitText(Katalogfilterprofil.SpChemie, "Lithium-Ionen")
            .MitZahl(Katalogfilterprofil.SpEnergie, 10.0)
            .MitZahl(Katalogfilterprofil.SpLeistung, 10.0)
            .MitZahl(Katalogfilterprofil.SpCrate, 1.0, 2)
            .MitZahl(Katalogfilterprofil.SpEtaRt, 0.9, 3)
            .MitZahl(Katalogfilterprofil.SpZyklen, 6000, 0),

        new Katalogfilterzeile(42, "Speicher 20")
            .MitText(Katalogfilterprofil.SpBezeichner, "Speicher 20")
            .MitText(Katalogfilterprofil.SpHersteller, "Werk B")
            .MitText(Katalogfilterprofil.SpChemie, "Lithium-Ionen")
            .MitZahl(Katalogfilterprofil.SpEnergie, 20.0)
            .MitZahl(Katalogfilterprofil.SpLeistung, 20.0)
            .MitZahl(Katalogfilterprofil.SpCrate, 1.0, 2)
            .MitZahl(Katalogfilterprofil.SpEtaRt, 0.92, 3)
            .MitZahl(Katalogfilterprofil.SpZyklen, 8000, 0),
    };

    public StromspeicherDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static ErzeugerZeile Zeile(int schluessel, string name, int geraetId)
        => new() { Schluessel = schluessel, Bezeichner = name, GeraetId = geraetId };

    private static ErzeugerDetail Detail(string name) => new(
        name, "",
        new[] { ("Typ:", "Lithium"), ("Leistung [kW]:", "10"),
                ("Energie (Kapazität) [kWh]:", "20"), ("Degradation [%/a]:", "0,1"),
                ("Ladezustand [%]:", "50"), ("Modulkosten [€/kWh]:", "400") });

    /// <summary>Das PROFIL des Modulkatalogs — daraus baut die Hülle die Felder des Aufklappers.</summary>
    private static readonly ModulKatalogProfil Modulprofil =
        ModulKatalogProfil.Finde(ModulKatalogArt.Stromspeicher,
            s => Resource.ResourceManager.GetString(s) ?? s);

    /// <summary>Die Felder des Aufklappers „Alle Daten anzeigen" — abgebildet wie in der Hülle.</summary>
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

    private IRenderedComponent<StromspeicherDialog> Aufbauen(
        List<ErzeugerZeile>? zeilen = null,
        Func<int, AufnahmeErgebnis>? aufnehmen = null,
        Action<ErzeugerZeile>? entfernen = null,
        bool wizard = false,
        Func<string, IReadOnlyList<BrowserFeldwert>?>? katalogfelder = null,
        Func<string, IReadOnlyList<BrowserFeldwert>, KatalogSpeicherErgebnis>? felderSpeichern = null,
        Action<bool>? geschlossen = null,
        Func<string>? summe = null,
        Func<string, IReadOnlyDictionary<string, object>>? editorGaben = null,
        Func<int, string>? katalogLoeschen = null,
        Satzbearbeitungswege? projektsatzWege = null,
        Satzbearbeitungswege? katalogsatzWege = null,
        Rueckwegwege? rueckwegWege = null,
        Func<IReadOnlyList<Katalogfilterzeile>>? katalogzeilen = null,
        Func<ErzeugerZeile?, bool, Task>? kostenOeffnen = null,
        Func<ErzeugerZeile?, Task>? energiekosten = null,
        Func<ErzeugerZeile, ErzeugerDetail?>? projektDetail = null)
    {
        return Render<StromspeicherDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<ErzeugerZeile> { Zeile(1, "Speicher 10", 141) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, katalogzeilen ?? Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.KatalogDetail, n => Detail(n))
            .Add(x => x.ProjektDetail, projektDetail ?? (z => Detail(z.Bezeichner)))
            .Add(x => x.Aufnehmen, aufnehmen ?? (_ => new AufnahmeErgebnis(Zeile(9, "Speicher 20", 142))))
            .Add(x => x.Entfernen, entfernen)
            .Add(x => x.Katalogfelder, katalogfelder)
            .Add(x => x.KatalogfelderSpeichern, felderSpeichern)
            .Add(x => x.SummeKapazitaet, summe)
            .Add(x => x.EditorGaben, editorGaben)
            .Add(x => x.KatalogLoeschen, katalogLoeschen)
            .Add(x => x.ProjektsatzWege, projektsatzWege)
            .Add(x => x.KatalogsatzWege, katalogsatzWege)
            .Add(x => x.RueckwegWege, rueckwegWege)
            .Add(x => x.KostenOeffnen, kostenOeffnen)
            .Add(x => x.EnergiekostenOeffnen, energiekosten)
            .Add(x => x.Wizard, wizard)
            .Add(x => x.Geschlossen, ok => geschlossen?.Invoke(ok)));
    }

    /// <summary>Wählt eine Katalogzeile — im Katalog ist die ZEILE die Wahl (Kästchenmodus).</summary>
    private static void KatalogzeileWaehlen(IRenderedComponent<StromspeicherDialog> cut, int nummer = 0)
        => cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[nummer].Click();

    /// <summary>Wählt eine Projektzeile über ihre Wahlspalte.</summary>
    private static void ProjektzeileWaehlen(IRenderedComponent<StromspeicherDialog> cut, int nummer = 0)
        => cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[nummer].Click();

    private static void KatalogAnkreuzen(IRenderedComponent<StromspeicherDialog> cut, params int[] zeilen)
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

    /// <summary>Der Auswahlpfad der Kostenknöpfe — drei Stück, in dieser Reihenfolge.</summary>
    private const string KOSTENKNOEPFE = ".epos-kostenleiste button.epos-knopf";

    // =================================================================================
    // Katalogauswahl V1, Stufe 3: Knöpfe an ihrem Ort, Detailzeile, Bearbeiten je Bereich
    // =================================================================================

    [Fact]
    public void S3_Die_Knoepfe_stehen_an_ihrem_Ort_und_der_Katalogfuss_hat_kein_Neu()
    {
        var cut = Aufbauen(projektsatzWege: Wege(), katalogsatzWege: Wege(), summe: () => "30",
                           editorGaben: _ => new Dictionary<string, object>(), katalogLoeschen: _ => "");

        // D: Kontext im Dialogkopf, keine eigene Kontextzeile mehr.
        Assert.Equal("Geben Sie Daten der Stromspeicher ein",
                     cut.Find(".epos-dialog-kopf > .epos-dialog-kontext").TextContent);
        Assert.Empty(cut.FindAll(".epos-kontextzeile"));

        // P: Summe kWh, Bearbeiten…, Entfernen — der Rückweg nur mit seinen Wegen.
        var p = cut.Find(".epos-zweispalten-bereich--projekt > .epos-zweispalten-kopfleiste");
        var summe = p.QuerySelector(".epos-zweispalten-summe")!;
        Assert.Contains("Summe aller ausgewählten Speicher [kWh]:", summe.TextContent);
        Assert.Contains("30", summe.TextContent);
        Assert.NotNull(p.QuerySelector(".epos-knopf--bearbeiten-projekt"));
        Assert.NotNull(p.QuerySelector(".epos-zweispalten-knopf--entfernen"));
        Assert.Empty(cut.FindAll(".epos-knopf--rueckweg"));

        // K-Kopf: Suche und Trefferzahl, Übernehmen.
        var k = cut.Find(".epos-zweispalten-bereich--katalog > .epos-zweispalten-kopfleiste");
        Assert.NotNull(k.QuerySelector(".epos-katalog-suchzeile input[type=search]"));
        Assert.NotNull(k.QuerySelector(".epos-zweispalten-knopf--uebernehmen"));

        // K-Fuß: Vergleichen, Löschen, Bearbeiten nach dem Namen — kein Neu… (4.9).
        KatalogzeileWaehlen(cut);
        var fuss = cut.Find(".epos-zweispalten-fussleiste").Children.ToList();
        Assert.Equal("Speicher 10:", fuss[0].TextContent);
        Assert.Contains(fuss, e => e.ClassList.Contains("epos-knopf--vergleichen"));
        Assert.Contains(fuss, e => e.ClassList.Contains("epos-knopf--loeschen"));
        Assert.Contains(fuss, e => e.ClassList.Contains("epos-knopf--bearbeiten-katalog"));
        Assert.Empty(cut.FindAll(".epos-knopf--neu"));
    }

    [Fact]
    public void S3_Ohne_Loeschweg_kein_Loeschknopf_und_ohne_Summe_keine_Summe()
    {
        var cut = Aufbauen();
        KatalogzeileWaehlen(cut);
        Assert.Empty(cut.FindAll(".epos-knopf--loeschen"));
        Assert.Empty(cut.FindAll(".epos-zweispalten-summe"));
    }

    [Fact]
    public void S3_Die_Detailzeile_nennt_Marke_Name_und_Kenndaten()
    {
        var cut = Aufbauen(katalogzeilen: () => MitSchloss(41));
        var zeile = cut.Find(".epos-zweispalten-satzzeile");
        Assert.Equal(Resource.AUSWAHL_MARKE_PROJEKTSATZ, zeile.QuerySelector(".epos-zweispalten-marke--satz")!.TextContent);
        Assert.Equal("Speicher 10", zeile.QuerySelector(".epos-zweispalten-satzname")!.TextContent);
        Assert.Contains("Typ Lithium", zeile.QuerySelector(".epos-zweispalten-satzkenndaten")!.TextContent);

        KatalogzeileWaehlen(cut);
        zeile = cut.Find(".epos-zweispalten-satzzeile");
        Assert.Equal(Resource.AUSWAHL_MARKE_KATALOGSATZ, zeile.QuerySelector(".epos-zweispalten-marke--satz")!.TextContent);
        Assert.Contains(Resource.AUSWAHL_SATZ_NUR_LESEN, zeile.QuerySelector(".epos-zweispalten-satzkenndaten")!.TextContent);
    }

    /// <summary>
    /// Die Projektzeile liest ihr Detail über die ZEILE (Geräte-ID der Projektkopie), der Katalog über den Namen —
    /// Varianten desselben Speichers tragen eigene Namen.
    /// </summary>
    [Fact]
    public void S3_Projektzeile_und_Katalogsatz_holen_ihr_Detail_je_aus_ihrer_Quelle()
    {
        var projekt = new List<int>();
        var katalog = new List<string>();
        var cut = Render<StromspeicherDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Speicher 10 (2)", 141) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.ProjektDetail, z => { projekt.Add(z.GeraetId); return Detail("Speicher 10"); })
            .Add(x => x.KatalogDetail, n => { katalog.Add(n); return Detail(n); }));

        KatalogzeileWaehlen(cut, 1);

        Assert.Equal(new[] { 141 }, projekt);
        Assert.Equal(new[] { "Speicher 20" }, katalog);
    }

    [Fact]
    public void S3_Alle_Daten_des_Projektsatzes_speichern_ueber_den_Kernweg()
    {
        var gespeichert = new List<(int Id, IReadOnlyList<BrowserFeldwert> Felder)>();
        var cut = Aufbauen(projektsatzWege: Wege(gespeichert));

        Assert.True(cut.Instance.ParameterOffen);
        var felder = cut.FindComponent<Katalogfelder>();
        Assert.Contains(felder.Instance.Felder, f => f.Schluessel == ModulKatalogProfil.FeldModulkosten && f.Editierbar);
        Assert.Contains(felder.Instance.Felder, f => f.Schluessel == ModulKatalogProfil.FeldInvestitionFix && f.Editierbar);

        cut.FindAll(".epos-modulparameter input[inputmode=decimal]")[0].Input("42");
        cut.Find(".epos-modulparameter .epos-speichervermerk button").Click();

        Assert.Equal(141, Assert.Single(gespeichert).Id);
    }

    [Fact]
    public void S3_Bearbeiten_im_Projektbereich_oeffnet_die_Projektkopie_je_Geraet_einmal()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Speicher 10", 141), Zeile(2, "Speicher 10 (2)", 141) };
        var cut = Aufbauen(zeilen, projektsatzWege: Wege());

        foreach (int i in new[] { 0, 1 })
            cut.FindAll(".epos-raster")[0].QuerySelectorAll("td .epos-wahlkaestchen")[i].Click();
        cut.Find(".epos-knopf--bearbeiten-projekt").Click();

        Assert.Equal(Satzmarke.Projektsatz, cut.Instance.Bearbeitung!.Value.Art);
        Assert.Equal(141, Assert.Single(cut.Instance.Bearbeitung!.Value.Saetze).Id);
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
                               ["Art"] = ModulKatalogArt.Stromspeicher,
                               ["Wege"] = new ModulKatalogWege(),
                               ["Vorwahl"] = n
                           }; });
        KatalogzeileWaehlen(cut, 1);
        cut.Find(".epos-knopf--bearbeiten-katalog").Click();

        Assert.True(cut.Instance.EditorOffen);
        Assert.Null(cut.Instance.Bearbeitung);
        Assert.Equal("Speicher 20", vorgewaehlt);
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

        Assert.Equal(new[] { 41, 42 }, gespeichert.Select(g => g.Id).ToArray());
        Assert.All(gespeichert, g => Assert.Equal("Neuwerk",
            g.Felder.First(f => f.Schluessel == ModulKatalogProfil.FeldFirma).Wert));
        Assert.Null(cut.Instance.Bearbeitung);
    }

    [Fact]
    public void S3_Gesperrte_Katalogsaetze_werden_uebersprungen_und_genannt()
    {
        var cut = Aufbauen(katalogsatzWege: Wege(), katalogzeilen: () => MitSchloss(42));

        KatalogAnkreuzen(cut, 0, 1);
        cut.Find(".epos-knopf--bearbeiten-katalog").Click();

        var sb = cut.FindComponent<Satzbearbeitung>();
        Assert.Equal(41, Assert.Single(sb.Instance.Aktiv).Id);
        Assert.Contains("„Speicher 20“", cut.Find(".epos-satzbearbeitung-hinweis--uebersprungen").TextContent);
    }

    [Fact]
    public void S3_Ein_gesperrter_Katalogsatz_allein_oeffnet_nur_lesend()
    {
        var cut = Aufbauen(katalogsatzWege: Wege(), katalogzeilen: () => MitSchloss(41),
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
        var cut = Aufbauen(katalogsatzWege: Wege(ergebnis: new KatalogSpeicherErgebnis(false, "„Speicher 20“ ist gesperrt.", "")));

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
        }, summe: () => { summenRufe++; return "0"; });

        KatalogAnkreuzen(cut, 0, 1);
        int vorher = summenRufe;
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();

        Assert.Equal(new[] { 41, 42 }, aufgenommen.ToArray());
        Assert.Equal(2, zeilen.Count);
        Assert.Equal(0, cut.Instance.KatalogWahl.Anzahl);
        Assert.True(summenRufe > vorher);
    }

    [Fact]
    public void S3_Entfernen_wirkt_auf_alle_gewaehlten_Projektzeilen()
    {
        var entfernt = new List<string>();
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "A", 141), Zeile(2, "B", 142), Zeile(3, "C", 143) };
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

        Assert.Contains("„Speicher 20“".Trim('„', '“'), cut.Find(".epos-rueckfrage").TextContent);
        Assert.Empty(geloescht);
        cut.FindAll(".epos-rueckfrage button").First(b => b.TextContent.Trim() == "Ja").Click();
        Assert.Equal(new[] { 42 }, geloescht.ToArray());
        Assert.Null(cut.Instance.Katalogzeile);
    }

    [Fact]
    public void S3_Loeschen_mehrerer_ueberspringt_gesperrte_und_meldet_einen_Grund()
    {
        var geloescht = new List<int>();
        var cut = Aufbauen(katalogzeilen: () => MitSchloss(42),
                           katalogLoeschen: id => { geloescht.Add(id); return id == 41 ? "verwendet" : ""; });

        KatalogAnkreuzen(cut, 0, 1);
        cut.Find(".epos-knopf--loeschen").Click();
        Assert.Contains(string.Format(Resource.Culture, Resource.SATZBEARB_UEBERSPRUNGEN, "„Speicher 20“"),
                        cut.Find(".epos-rueckfrage").TextContent);
        cut.FindAll(".epos-rueckfrage button").First(b => b.TextContent.Trim() == "Ja").Click();

        Assert.Equal(new[] { 41 }, geloescht.ToArray());
        Assert.Equal("verwendet", cut.Instance.Meldung);
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

        Assert.Contains("ausgewählte Stromspeicher:", cut.Markup);
        Assert.Contains("Stromspeicher aus Datenbank:", cut.Markup);

        // Sieben NUR LESBARE Anzeigefelder: Name, Typ, Leistung, Energie, Degradation, Ladezustand, Modulkosten.
        Assert.Equal(7, cut.FindAll(".epos-formularraster input[readonly]").Count);
    }

    [Fact]
    public void Die_zwei_berichtigten_Beschriftungen_stehen_da()
    {
        var cut = Aufbauen();

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.Contains("Energie (Kapazität) [kWh]:", texte);
        Assert.Contains("Modulkosten [€/kWh]:", texte);
        Assert.DoesNotContain("Energie [kW]:", texte);
    }

    [Fact]
    public void Die_Katalogliste_zeigt_die_Spalten_des_Profils()
    {
        var cut = Aufbauen();

        var kopf = cut.FindAll(".epos-raster")[1].QuerySelectorAll("th")
                      .Select(e => e.TextContent.Trim()).ToList();

        Assert.Equal(Profil.Spalten.Count(s => !s.StandardAus) + 1, kopf.Count);   // Kästchen + Spalten (4.10)
        Assert.Contains(kopf, k => k.StartsWith("Hersteller"));
        Assert.Contains(kopf, k => k.Contains("kWh"));
        Assert.Empty(cut.FindAll(".epos-mehrzeilig"));
        Assert.Single(cut.FindAll(".epos-katalog-suchzeile"));
    }

    [Fact]
    public void Im_Assistenten_fehlen_OK_Abbrechen_und_die_Kostenleiste()
    {
        var cut = Aufbauen(wizard: true, kostenOeffnen: (_, _) => Task.CompletedTask);
        Assert.Empty(cut.FindAll(".epos-status"));
        Assert.Empty(cut.FindAll(".epos-kostenleiste"));
    }

    // =================================================================================
    // Aufnehmen und Entfernen
    // =================================================================================

    [Fact]
    public void Je_Klick_entsteht_eine_eigene_Zeile()
    {
        // AP2b: zweimal derselbe Speicher sind zwei Zeilen.
        var zeilen = new List<ErzeugerZeile>();
        int naechster = 10;
        var cut = Aufbauen(zeilen, aufnehmen: id =>
            new AufnahmeErgebnis(Zeile(naechster++, "Speicher 10", 100 + id)));

        KatalogzeileWaehlen(cut);
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();
        KatalogzeileWaehlen(cut);
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();

        Assert.Equal(2, zeilen.Count);
        Assert.NotEqual(zeilen[0].Schluessel, zeilen[1].Schluessel);
    }

    [Fact]
    public void Eine_Absage_der_Aufnahme_meldet_den_Grund()
    {
        var zeilen = new List<ErzeugerZeile>();
        var cut = Aufbauen(zeilen, aufnehmen: _ => new AufnahmeErgebnis(null, "nicht gefunden", true));
        KatalogzeileWaehlen(cut);
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();
        Assert.Empty(zeilen);
        Assert.Equal("nicht gefunden", cut.Instance.Meldung);
    }

    [Fact]
    public void Der_Pfeil_zurueck_trifft_genau_die_gewaehlte_Zeile()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Speicher 10", 141), Zeile(2, "Speicher 10", 141) };
        var entfernt = new List<ErzeugerZeile>();
        var cut = Aufbauen(zeilen, entfernen: z => entfernt.Add(z));

        ProjektzeileWaehlen(cut, 1);
        cut.FindAll(".epos-zweispalten-knopf--entfernen")[0].Click();

        Assert.Single(zeilen);
        Assert.Equal(1, zeilen[0].Schluessel);
        Assert.Equal(2, entfernt[0].Schluessel);
    }

    [Fact]
    public void Nach_der_letzten_Zeile_wandert_die_Auswahl_in_den_Katalog()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Speicher 10", 141) };
        var cut = Aufbauen(zeilen);

        cut.FindAll(".epos-zweispalten-knopf--entfernen")[0].Click();

        Assert.Empty(zeilen);
        Assert.Null(cut.Instance.Projektzeile);
        Assert.Equal(41, cut.Instance.Katalogzeile!.Id);
    }

    // =================================================================================
    // Tastatur, Kreuz, Satzfläche
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

    [Fact]
    public void Esc_schliesst_bei_offener_Satzbearbeitung_den_Dialog_nicht()
    {
        int rufe = 0;
        var cut = Aufbauen(projektsatzWege: Wege(), geschlossen: _ => rufe++);
        cut.Find(".epos-knopf--bearbeiten-projekt").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(0, rufe);
    }

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

    [Fact]
    public void Die_Knopfzeile_steht_als_erstes_in_der_Satzflaeche()
    {
        var cut = Aufbauen(kostenOeffnen: (_, _) => Task.CompletedTask,
                           energiekosten: _ => Task.CompletedTask,
                           projektsatzWege: Wege());

        var kinder = cut.Find(".epos-zweispalten-satz").Children.ToList();
        Assert.Contains("epos-leiste", kinder[0].ClassList);
        int raster = kinder.FindIndex(k => k.ClassList.Contains("epos-formularraster"));
        int parameter = kinder.FindIndex(k => k.ClassList.Contains("epos-modulparameter"));
        Assert.True(0 < raster && raster < parameter);

        var teile = kinder[0].Children.ToList();
        Assert.Contains("epos-kostenleiste", teile[0].ClassList);
        Assert.Contains("epos-leiste-fueller", teile[1].ClassList);
        Assert.Contains("epos-berechnungshilfe", teile[2].ClassList);
        Assert.Empty(cut.Find(".epos-zweispalten-satz").QuerySelectorAll("button")
                        .Where(k => k.TextContent.Trim() == "Bearbeiten..."));
    }

    [Fact]
    public void Ohne_Wahl_steht_der_Leerhinweis()
    {
        var cut = Aufbauen(zeilen: new List<ErzeugerZeile>(), katalogzeilen: () => Array.Empty<Katalogfilterzeile>());
        Assert.Equal(Resource.AUSWAHL_SATZ_LEER, cut.Find(".epos-zweispalten-satzleer").TextContent);
    }

    // =================================================================================
    //  Stufe S2.1 / S2.3 / S2.5 - Anwenderentscheid W14a-E-10 vom 07.09.2026
    // =================================================================================

    /// <summary>Die Katalogliste - das UNTERE der beiden Raster.</summary>
    private static IReadOnlyList<AngleSharp.Dom.IElement> Katalogzeilen(
        IRenderedComponent<StromspeicherDialog> cut)
        => cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr").ToList();

    [Fact]
    public void S2_1_Der_Spaltenfilter_schraenkt_die_Katalogliste_ein()
    {
        var cut = Aufbauen();
        Assert.Equal(2, Katalogzeilen(cut).Count);
        Assert.Equal("2 von 2 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);

        _filterstand.Setzen(Katalogfilterprofil.SpHersteller, "Werk B");
        cut.Render();

        Assert.Single(Katalogzeilen(cut));
        Assert.Equal("1 von 2 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);
        Assert.Contains("Speicher 20", Katalogzeilen(cut)[0].TextContent);

        cut.Find(".epos-katalog-ruecksetzer").Click();
        Assert.Equal(2, Katalogzeilen(cut).Count);
    }

    [Fact]
    public void S2_1_Die_Katalogliste_laesst_sich_ueber_den_Spaltenkopf_sortieren()
    {
        var cut = Aufbauen();

        _filterstand.Sortieren(Katalogfilterprofil.SpEnergie);
        cut.Render();
        Assert.Contains("Speicher 10", Katalogzeilen(cut)[0].TextContent);

        _filterstand.Sortieren(Katalogfilterprofil.SpEnergie);
        cut.Render();
        Assert.False(_filterstand.Aufsteigend);
        Assert.Contains("Speicher 20", Katalogzeilen(cut)[0].TextContent);
    }

    [Fact]
    public void S2_1_Die_Markierung_ueberlebt_einen_Filterwechsel()
    {
        var cut = Aufbauen();

        KatalogzeileWaehlen(cut);
        Assert.False(cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].HasAttribute("disabled"));

        _filterstand.Setzen(Katalogfilterprofil.SpHersteller, "Werk B");
        cut.Render();

        Assert.Single(Katalogzeilen(cut));
        Assert.False(cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].HasAttribute("disabled"));
    }

    [Fact]
    public void S2_3_Die_Verwendungsmarke_zaehlt_die_Projektliste()
    {
        var cut = Aufbauen(zeilen: new List<ErzeugerZeile> { Zeile(1, "Speicher 10", 141) });

        var zeilen = Katalogzeilen(cut);
        Assert.Equal(2, zeilen.Count);
        Assert.Equal(1, zeilen.Count(z => z.QuerySelector(".epos-verwendet-marke") is not null));
        Assert.Contains("Speicher 10", zeilen.First(z => z.QuerySelector(".epos-verwendet-marke") is not null).TextContent);
    }

    [Fact]
    public void S2_5_Der_Filterstand_ueberlebt_einen_zweiten_Aufbau()
    {
        var ersterAufbau = Aufbauen();
        _filterstand.Setzen(Katalogfilterprofil.SpHersteller, "Werk B");
        ersterAufbau.Render();
        Assert.Single(Katalogzeilen(ersterAufbau));

        var zweiterAufbau = Aufbauen();

        Assert.Single(Katalogzeilen(zweiterAufbau));
        Assert.Single(zweiterAufbau.FindAll(".epos-katalog-ruecksetzer"));
    }

    // =================================================================================
    // ET-5: der Energietraeger des Speichers, Gruppe > Art — nur beim Projektsatz
    // =================================================================================

    [Fact]
    public void Die_markierte_Anlage_zeigt_ihren_Energietraeger_und_meldet_den_Wechsel()
    {
        (ErzeugerZeile Zeile, int Neu)? gemeldet = null;
        ErzeugerZeile zeile = Zeile(1, "Speicher 10", 141);
        zeile.CarrierId = 60;
        var cut = Render<StromspeicherDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { zeile })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.KatalogDetail, n => Detail(n))
            .Add(x => x.ProjektDetail, z => Detail(z.Bezeichner))
            .Add(x => x.Traegerkatalog, new[]
            {
                new EnergietraegerWahl.Eintrag(11, "Gas", "Erdgas E"),
                new EnergietraegerWahl.Eintrag(60, "Strom", "Elektrische Energie"),
                new EnergietraegerWahl.Eintrag(58, "Strom", "Elektrische Energie 2")
            })
            .Add(x => x.TraegerWechseln, (ErzeugerZeile z, int neu) => gemeldet = (z, neu)));

        var selects = cut.Find(".epos-traegerwahl").QuerySelectorAll("select");
        Assert.Equal(2, selects.Length);
        selects[1].Change("58");

        Assert.Equal(58, gemeldet?.Neu);
        Assert.Equal(58, zeile.CarrierId);

        // Beim Katalogsatz gibt es nichts zu wählen.
        KatalogzeileWaehlen(cut);
        Assert.Empty(cut.FindAll(".epos-traegerwahl"));
    }

    // =====================================================================
    //  Der Aufklapper „Alle Daten anzeigen" — beim Katalogsatz
    // =====================================================================

    [Fact]
    public void Ohne_Projektsatzwege_hat_der_Projektsatz_keinen_Aufklapper()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder);

        Assert.Empty(cut.FindAll(".epos-modulparameter-knopf"));

        KatalogzeileWaehlen(cut);
        Assert.Single(cut.FindAll(".epos-modulparameter-knopf"));
    }

    [Fact]
    public void Ohne_Katalogfelder_gibt_es_keinen_Aufklapper()
    {
        var cut = Aufbauen();
        KatalogzeileWaehlen(cut);

        Assert.Empty(cut.FindAll(".epos-modulparameter-knopf"));
    }

    [Fact]
    public void Der_Parameterblock_ist_aufgeklappt_die_Vorgabe()
    {
        int rufe = 0;
        var cut = Aufbauen(katalogfelder: n => { rufe++; return Katalogfelder(n); });

        Assert.Equal(0, rufe);
        KatalogzeileWaehlen(cut);

        var knopf = cut.Find(".epos-modulparameter-knopf");
        Assert.Equal("true", knopf.GetAttribute("aria-expanded"));
        Assert.Contains("Alle Daten anzeigen", knopf.TextContent);
        Assert.Equal(1, rufe);
        Assert.Equal(Modulprofil.Felder.Count, cut.Find(".epos-modulparameter").QuerySelectorAll(".epos-feld").Length);
    }

    [Fact]
    public void Der_Knopf_klappt_zu_und_wieder_auf()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder);
        KatalogzeileWaehlen(cut);

        cut.Find(".epos-modulparameter-knopf").Click();
        Assert.False(cut.Instance.ParameterOffen);
        Assert.Empty(cut.Find(".epos-modulparameter").QuerySelectorAll(".epos-feld"));

        cut.Find(".epos-modulparameter-knopf").Click();
        Assert.True(cut.Instance.ParameterOffen);
        Assert.NotEmpty(cut.Find(".epos-modulparameter").QuerySelectorAll(".epos-feld"));
    }

    [Fact]
    public void Ein_Satzwechsel_zieht_den_Block_nach()
    {
        int rufe = 0;
        var cut = Aufbauen(katalogfelder: n => { rufe++; return Katalogfelder(n); });
        KatalogzeileWaehlen(cut);
        Assert.Equal("Speicher 10", cut.Find(".epos-modulparameter").QuerySelectorAll("input")[0].GetAttribute("value"));

        KatalogzeileWaehlen(cut, 1);

        Assert.Equal(2, rufe);
        Assert.Equal("Speicher 20", cut.Find(".epos-modulparameter").QuerySelectorAll("input")[0].GetAttribute("value"));
    }

    [Fact]
    public void Der_Bezeichner_bleibt_Lesewert()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder,
                           felderSpeichern: (n, _) => new KatalogSpeicherErgebnis(true, "", n));
        KatalogzeileWaehlen(cut);

        int erwartet = Modulprofil.Felder.Count(f => f.Gesperrt || f.Art == BrowserFeldArt.Auswahl);
        Assert.True(erwartet > 0, "Das Profil führt kein gesperrtes Feld.");
        Assert.Equal(erwartet, cut.Find(".epos-modulparameter").QuerySelectorAll("input[readonly]").Length);
    }

    [Fact]
    public void Ohne_Speicherweg_ist_der_Aufklapper_nur_Anzeige()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder);
        KatalogzeileWaehlen(cut);

        Assert.Empty(cut.FindAll(".epos-modulparameter .epos-leiste"));
        Assert.All(cut.FindAll(".epos-modulparameter input"), e => Assert.True(e.HasAttribute("readonly")));
    }

    [Fact]
    public void Ein_gesperrter_Katalogsatz_zeigt_Alle_Daten_nur_lesend_mit_Hinweis()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder, katalogzeilen: () => MitSchloss(41),
                           felderSpeichern: (n, _) => new KatalogSpeicherErgebnis(true, "", n));
        KatalogzeileWaehlen(cut);

        Assert.Contains(Resource.ADM_SCHLOSS_ERST_AUFHEBEN, cut.Find(".epos-modulparameter .epos-satzbearbeitung-hinweis").TextContent);
        Assert.Empty(cut.FindAll(".epos-modulparameter .epos-leiste"));
    }

    [Fact]
    public void Speichern_reicht_die_geaenderten_Felder_hinaus()
    {
        string? name = null;
        IReadOnlyList<BrowserFeldwert>? gesehen = null;
        var cut = Aufbauen(
            katalogfelder: Katalogfelder,
            felderSpeichern: (n, f) => { name = n; gesehen = f; return new KatalogSpeicherErgebnis(true, "", n); });

        KatalogzeileWaehlen(cut);
        cut.FindAll(".epos-modulparameter input[inputmode=decimal]")[0].Input("42");
        cut.Find(".epos-modulparameter .epos-leiste .epos-knopf").Click();

        Assert.Equal("Speicher 10", name);
        Assert.Contains(gesehen!, f => f.Wert == "42");
    }

    [Fact]
    public void Speichern_meldet_am_Knopf_und_die_Eingabe_nimmt_den_Vermerk_zurueck()
    {
        int schreibvorgaenge = 0;
        var cut = Aufbauen(katalogfelder: Katalogfelder,
                           felderSpeichern: (n, _) => { schreibvorgaenge++; return new KatalogSpeicherErgebnis(true, "Datensatz gespeichert", n); });
        KatalogzeileWaehlen(cut);

        cut.FindAll(".epos-modulparameter input[inputmode=decimal]")[0].Input("42");
        cut.Find(".epos-modulparameter .epos-speichervermerk button").Click();

        Assert.Equal(1, schreibvorgaenge);
        Assert.Equal("", cut.Instance.Meldung);
        Assert.StartsWith("Gespeichert um ", cut.Find(".epos-modulparameter .epos-speichervermerk [role=status]").TextContent);

        cut.FindAll(".epos-modulparameter input[inputmode=decimal]")[0].Input("43");
        Assert.Empty(cut.FindAll(".epos-modulparameter .epos-speichervermerk [role=status]"));
    }

    [Fact]
    public void Ein_Fehlerzustand_sperrt_das_Speichern()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder,
                           felderSpeichern: (n, _) => new KatalogSpeicherErgebnis(true, "", n));
        KatalogzeileWaehlen(cut);

        cut.FindAll(".epos-modulparameter input[inputmode=decimal]")[0].Input("keine Zahl");
        Assert.True(cut.Find(".epos-modulparameter .epos-leiste .epos-knopf").HasAttribute("disabled"));
    }

    [Fact]
    public void Eine_abgelehnte_Speicherung_meldet_und_haelt_den_Stand()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder,
                           felderSpeichern: (n, _) => new KatalogSpeicherErgebnis(false, "Der Datensatz ist schreibgeschützt.", n));

        KatalogzeileWaehlen(cut);
        cut.FindAll(".epos-modulparameter input[inputmode=decimal]")[0].Input("42");
        cut.Find(".epos-modulparameter .epos-leiste .epos-knopf").Click();

        Assert.Equal("Der Datensatz ist schreibgeschützt.", cut.Instance.Meldung);
        Assert.Single(cut.FindAll(".epos-warnbanner"));
        Assert.False(cut.Find(".epos-modulparameter .epos-leiste .epos-knopf").HasAttribute("disabled"));
    }

    // =====================================================================
    //  Die Kostenleiste — nur beim Projektsatz (KA-E-12)
    // =====================================================================

    [Fact]
    public void Ohne_Wege_bleibt_die_Kostenleiste_leer()
    {
        var cut = Aufbauen();
        Assert.Empty(cut.FindAll(KOSTENKNOEPFE));
    }

    [Fact]
    public void Die_Kostenknoepfe_stehen_nur_beim_Projektsatz_und_reichen_die_Zeile_durch()
    {
        var kosten = new List<bool>();
        int energie = 0;
        ErzeugerZeile? gesehen = null;
        var cut = Aufbauen(
            kostenOeffnen: (zeile, betrieb) => { gesehen = zeile; kosten.Add(betrieb); return Task.CompletedTask; },
            energiekosten: _ => { energie++; return Task.CompletedTask; });

        var knoepfe = cut.FindAll(KOSTENKNOEPFE);
        Assert.Equal(3, knoepfe.Count);
        knoepfe[0].Click();
        cut.FindAll(KOSTENKNOEPFE)[1].Click();
        cut.FindAll(KOSTENKNOEPFE)[2].Click();
        Assert.Equal(new[] { false, true }, kosten);
        Assert.Equal(1, energie);
        Assert.Equal(141, gesehen!.GeraetId);

        KatalogzeileWaehlen(cut);
        Assert.Null(cut.Instance.Projektzeile);
        Assert.Empty(cut.FindAll(KOSTENKNOEPFE));
    }

    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F1)
    // =====================================================================

    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_gibt_den_Namen_heraus()
    {
        Aufbauen(zeilen: new List<ErzeugerZeile> { Zeile(1, "Speicher 10", 141) });

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.STROMSPEICHER_PROJEKT));

        KiFeldzugang zugang = KiMaskenbruecke.Feldzugang(KiMaskennamen.STROMSPEICHER_PROJEKT, "anlage");
        Assert.NotNull(zugang);
        Assert.Equal("Speicher 10", zugang.Lesen());
        Assert.False(zugang.Setzbar);
    }

    [Fact]
    public async Task Ohne_Aenderung_im_Aufklapper_lehnt_Speichern_benannt_ab()
    {
        Aufbauen();

        KiMaskenhaken haken = KiMaskenbruecke.Haken(KiMaskennamen.STROMSPEICHER_PROJEKT);
        Assert.NotNull(haken.Speichern);

        KiKern.KiErgebnis ergebnis = await haken.Speichern!();
        Assert.False(ergebnis.Erfolg);
        Assert.Contains("Alle Daten", ergebnis.Text, StringComparison.Ordinal);
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
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Speicher 10", 141), Zeile(2, "Speicher 20", 142), Zeile(3, "Speicher 10 (2)", 141) };
        var cut = Aufbauen(zeilen, projektsatzWege: Wege(),
                           rueckwegWege: Rueckweg(new[] { new Rueckwegvorschlag(141, "Speicher 10", "", Rueckwegsperre.UrsprungUnbekannt, "Speicher 10") },
                                                  gefragt: gefragt));

        var knoepfe = cut.Find(".epos-zweispalten-bereich--projekt > .epos-zweispalten-kopfleiste").QuerySelectorAll("button").ToList();
        int bearbeiten = knoepfe.FindIndex(k => k.ClassList.Contains("epos-knopf--bearbeiten-projekt"));
        int rueckweg = knoepfe.FindIndex(k => k.ClassList.Contains("epos-knopf--rueckweg"));
        Assert.True(bearbeiten >= 0 && rueckweg == bearbeiten + 1);

        foreach (int i in new[] { 0, 1, 2 })
            cut.FindAll(".epos-raster")[0].QuerySelectorAll("td .epos-wahlkaestchen")[i].Click();
        cut.Find(".epos-knopf--rueckweg").Click();
        Assert.Equal(new[] { 141, 142 }, Assert.Single(gefragt).ToArray());
        Assert.NotNull(cut.Instance.Rueckweg);
    }

    [Fact]
    public void S3b_Uebernehmen_meldet_und_liest_den_Katalog_neu()
    {
        int gelesen = 0;
        var geschrieben = new List<Rueckwegwahl>();
        var zeilen = new[] { new Rueckwegvorschlag(141, "Speicher 10", "", Rueckwegsperre.UrsprungUnbekannt, "Speicher 10 (Projekt)") };
        var cut = Aufbauen(rueckwegWege: Rueckweg(zeilen, geschrieben),
                           katalogzeilen: () => { gelesen++; return Katalogzeilen(); });
        int vorher = gelesen;
        cut.Find(".epos-knopf--rueckweg").Click();
        cut.Find(".epos-rueckweg-uebernehmen").Click();

        Assert.Equal(new[] { new Rueckwegwahl(141, false, "Speicher 10 (Projekt)") }, geschrieben.ToArray());
        Assert.Null(cut.Instance.Rueckweg);
        Assert.Equal("2 Sätze übernommen", cut.Instance.Meldung);
        Assert.True(gelesen > vorher);
    }

    [Fact]
    public void S3b_Der_Hinweis_nennt_was_im_Projekt_bleibt()
    {
        var zeilen = new[] { new Rueckwegvorschlag(141, "Speicher 10", "", Rueckwegsperre.UrsprungUnbekannt, "Speicher 10") };
        var cut = Render<StromspeicherDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Speicher 10", 141) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.RueckwegWege, Rueckweg(zeilen))
            .Add(x => x.RueckwegBleibtText, "Im Projekt bleiben: Betriebsführung der Anlage."));
        cut.Find(".epos-knopf--rueckweg").Click();
        Assert.Contains("Im Projekt bleiben: Betriebsführung der Anlage.", cut.Find(".epos-rueckweg").TextContent);
    }

    [Fact]
    public void S3b_Ohne_Wege_kein_Knopf()
    {
        var cut = Aufbauen(projektsatzWege: Wege());
        Assert.Empty(cut.FindAll(".epos-knopf--rueckweg"));
    }
}
