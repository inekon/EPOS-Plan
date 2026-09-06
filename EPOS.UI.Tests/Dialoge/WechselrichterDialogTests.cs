using System.Globalization;
using System.Threading;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Photovoltaik;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die zwei Wechselrichtermasken der Stufe S1 (Anwenderentscheid <b>W6‑E‑2</b> vom
/// 06.09.2026, <c>Konzept_Wechselrichter_EPOS-Plan.md</c>):
/// die VERWALTUNG als dritte Ausprägung von <see cref="ModulKatalogDialog"/> und der
/// CEC-IMPORT.
///
/// <para><b>Ohne Vorläufer, also ohne Feldkarte.</b> Die zehn anderen
/// Katalogverwaltungen des Hauses gleichen ihre Felder gegen die vermessene
/// WinForms-Maske ab; hier gibt es keine — der Wechselrichter war die einzige
/// Gerätefamilie ohne Katalog. Soll ist deshalb das PROFIL (Konzept 6): 25 Felder in
/// drei Gruppen, ein Pflichtfeld, ein Herstellerfilter.</para>
///
/// <para>Die Sprache pinnt die Klasse selbst (Regel seit iU9‑W8).</para>
/// </summary>
public class WechselrichterDialogTests : BunitContext
{
    public WechselrichterDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

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

    /// <summary>Das Profil in DEUTSCH — so, wie die Hülle es liefert.</summary>
    private static ModulKatalogProfil Profil() =>
        ModulKatalogProfil.Finde(ModulKatalogArt.Wechselrichter,
            s => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(s) ?? s);

    // =================================================================================
    //  Die VERWALTUNG — dritte Ausprägung des Modulkatalogs
    // =================================================================================

    private static IReadOnlyList<ModulFeldwert> Felder(string name)
    {
        var liste = new List<ModulFeldwert>();
        foreach (ModulKatalogFeld feld in Profil().Felder)
        {
            string wert = feld.Schluessel == ModulKatalogProfil.FeldBezeichner ? name
                        : feld.Schluessel == ModulKatalogProfil.FeldHerkunft ? DbWerte.WR_HERKUNFT_CEC
                        : feld.Art == BrowserFeldArt.Zahl ? "0,97"
                        : feld.Art == BrowserFeldArt.Ganzzahl ? "2"
                        : "Wert";

            liste.Add(new ModulFeldwert
            {
                Schluessel = feld.Schluessel,
                Bezeichnung = feld.Bezeichnung,
                Einheit = feld.Einheit,
                Art = feld.Art,
                LeerErlaubt = feld.LeerErlaubt,
                Gesperrt = feld.Gesperrt,
                Gruppe = feld.Gruppe,
                Wert = wert
            });
        }
        return liste;
    }

    private static IReadOnlyList<ModulZeile> Alle() => new[]
    {
        new ModulZeile(1, "Alpha 2500TL"),
        new ModulZeile(2, "Alpha 5000TL"),
        new ModulZeile(3, "Beta 10K")
    };

    private static IReadOnlyList<ModulZeile> Gefiltert(string hersteller)
    {
        if (string.IsNullOrEmpty(hersteller)) return Alle();
        return hersteller == "Alpha AG"
            ? new[] { new ModulZeile(1, "Alpha 2500TL"), new ModulZeile(2, "Alpha 5000TL") }
            : new[] { new ModulZeile(3, "Beta 10K") };
    }

    private IRenderedComponent<ModulKatalogDialog> Verwaltung(
        ModulKatalogWege? wege = null, Action<ModulErgebnis>? geschlossen = null)
    {
        ModulKatalogWege standard = new()
        {
            Liste = Alle,
            Hersteller = () => new[] { "Alpha AG", "Beta GmbH" },
            ListeGefiltert = Gefiltert,
            Detail = Felder,
            Speichern = (f, _, __) => new KatalogSpeicherErgebnis(
                true, "Datensatz gespeichert",
                f.First(x => x.Schluessel == ModulKatalogProfil.FeldBezeichner).Wert),
            Loeschen = n => new KatalogSpeicherErgebnis(true, "", n)
        };

        return Render<ModulKatalogDialog>(p => p
            .Add(x => x.Art, ModulKatalogArt.Wechselrichter)
            .Add(x => x.ProfilVorgabe, Profil())
            .Add(x => x.Wege, wege ?? standard)
            .Add(x => x.Geschlossen, e => geschlossen?.Invoke(e)));
    }

    /// <summary>
    /// Titel, Liste und die 25 Felder des Profils — jedes mit seiner Beschriftung.
    /// </summary>
    [Fact]
    public void Die_Verwaltung_zeigt_ihren_Titel_und_alle_Felder()
    {
        IRenderedComponent<ModulKatalogDialog> cut = Verwaltung();

        Assert.Equal("Verwaltung Wechselrichter", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal(3, cut.Instance.Zeilen.Count);
        Assert.Equal("Alpha 2500TL", cut.Instance.Gewaehlt);

        List<string> texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        foreach (ModulKatalogFeld feld in Profil().Felder)
            Assert.Contains(feld.Bezeichnung, texte);
    }

    /// <summary>
    /// <b>DREI Feldgruppen</b> (Konzept 6) — Gerät, Eingang, Wirkungsgrad. Der
    /// Wechselrichter ist die erste Ausprägung mit einer dritten; ein Block mit
    /// zwanzig Feldern wäre nicht lesbar.
    /// </summary>
    [Fact]
    public void Die_Verwaltung_gliedert_ihre_Felder_in_drei_Gruppen()
    {
        IRenderedComponent<ModulKatalogDialog> cut = Verwaltung();
        List<string> titel = cut.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent).ToList();

        // Liste + Geraet + Eingang + Wirkungsgrad
        Assert.Equal(4, titel.Count);
        Assert.Contains(Profil().GruppeBestand, titel);
        Assert.Contains(Profil().GruppeZwei, titel);
        Assert.Contains(Profil().GruppeDrei, titel);
    }

    /// <summary>
    /// <b>Der Herstellerfilter</b> (Konzept 6): Die dritte Ausprägung ist die einzige
    /// mit einem — die Wahl engt die Liste ein, „alle" hebt sie wieder auf.
    /// </summary>
    [Fact]
    public void Der_Herstellerfilter_engt_die_Liste_ein_und_gibt_sie_wieder_frei()
    {
        IRenderedComponent<ModulKatalogDialog> cut = Verwaltung();

        // Die Filterzeile steht ueber der Liste und traegt "alle" plus zwei Firmen.
        var wahl = cut.FindAll(".epos-katalog-liste select")[0];
        Assert.Equal(3, wahl.QuerySelectorAll("option").Count);
        Assert.Equal(3, cut.Instance.Zeilen.Count);

        wahl.Change("1");                       // Alpha AG
        Assert.Equal("Alpha AG", cut.Instance.GewaehlterHersteller);
        Assert.Equal(2, cut.Instance.Zeilen.Count);

        wahl.Change("2");                       // Beta GmbH
        Assert.Equal(1, cut.Instance.Zeilen.Count);

        wahl.Change("0");                       // alle
        Assert.Equal("", cut.Instance.GewaehlterHersteller);
        Assert.Equal(3, cut.Instance.Zeilen.Count);
    }

    /// <summary>
    /// <b>Ohne Delegaten kein Bedienelement</b> — die Hausregel des Dateiwählers gilt
    /// auch hier: Ein Filter ohne Datenweg wäre eine Liste, die nichts bewirkt.
    /// </summary>
    [Fact]
    public void Ohne_Herstellerweg_gibt_es_keine_Filterzeile()
    {
        IRenderedComponent<ModulKatalogDialog> cut = Verwaltung(new ModulKatalogWege
        {
            Liste = Alle,
            Detail = Felder
        });

        Assert.Empty(cut.FindAll(".epos-katalog-liste select"));
        Assert.Equal(3, cut.Instance.Zeilen.Count);
    }

    /// <summary>
    /// Bezeichner und Herkunft sind gesperrt: der eine ist der WHERE-Schlüssel, die
    /// andere die Auskunft des Imports.
    /// </summary>
    [Fact]
    public void Bezeichner_und_Herkunft_sind_gesperrt()
    {
        IRenderedComponent<ModulKatalogDialog> cut = Verwaltung();
        var gesperrt = cut.FindAll("input[readonly]");

        Assert.Equal(2, gesperrt.Count);
        Assert.Equal("Alpha 2500TL", gesperrt[0].GetAttribute("value"));
        Assert.Equal(DbWerte.WR_HERKUNFT_CEC, gesperrt[1].GetAttribute("value"));
    }

    /// <summary>
    /// Eine andere Zeile zieht ihren Feldsatz nach — der Weg, den jede
    /// Katalogverwaltung des Hauses geht.
    /// </summary>
    [Fact]
    public void Eine_andere_Zeile_zieht_ihren_Feldsatz_nach()
    {
        IRenderedComponent<ModulKatalogDialog> cut = Verwaltung();

        cut.FindAll(".epos-anlagenwahl")[2].Click();

        Assert.Equal("Beta 10K", cut.Instance.Gewaehlt);
        Assert.Equal("Beta 10K", cut.FindAll("input[readonly]")[0].GetAttribute("value"));
    }

    /// <summary>
    /// „Neu…" fragt erst den Namen und belegt danach die Herkunft mit
    /// <see cref="DbWerte.WR_HERKUNFT_HAND"/> — ein handgepflegter Satz sagt das von
    /// sich.
    /// </summary>
    [Fact]
    public void Neu_belegt_die_Herkunft_mit_HAND()
    {
        IRenderedComponent<ModulKatalogDialog> cut = Verwaltung();

        cut.FindAll(".epos-leiste .epos-knopf")[1].Click();
        Assert.True(cut.Instance.Namensfrage);

        cut.FindAll(".epos-ueberlagerung input[type=text]")[0].Input("Neues Geraet");
        cut.FindAll(".epos-ueberlagerung .epos-knopf--primaer")[0].Click();

        Assert.True(cut.Instance.IstNeu);

        List<string?> gesperrt = cut.FindAll("input[readonly]")
                                    .Select(e => e.GetAttribute("value")).ToList();
        Assert.Contains("Neues Geraet", gesperrt);
        Assert.Contains(DbWerte.WR_HERKUNFT_HAND, gesperrt);
    }

    /// <summary>
    /// <b>Das EINE Pflichtfeld</b> ist die AC-Nennleistung (Konzept 6): Ein leeres
    /// Feld meldet sich, alles andere darf leer bleiben und schaltet dann seine
    /// Prüfung ab.
    /// </summary>
    [Fact]
    public void Nur_die_AC_Nennleistung_ist_Pflicht()
    {
        var leer = new List<ModulFeldwert>(Felder("Alpha 2500TL"));
        foreach (ModulFeldwert f in leer)
            if (!f.Gesperrt) f.Wert = "";

        IRenderedComponent<ModulKatalogDialog> cut = Verwaltung(new ModulKatalogWege
        {
            Liste = Alle,
            Hersteller = () => new[] { "Alpha AG" },
            ListeGefiltert = Gefiltert,
            Detail = _ => leer,
            Speichern = (_, __, ___) => new KatalogSpeicherErgebnis(true, "gespeichert", "Alpha 2500TL")
        });

        cut.FindAll(".epos-leiste .epos-knopf")[0].Click();

        Assert.Contains(Profil().Felder
                            .Single(f => f.Schluessel == ModulKatalogProfil.FeldPAcNenn).Feldname,
                        cut.Instance.Meldung);
    }

    /// <summary>
    /// <b>Gelöscht wird mit Rückfrage</b> — die Angleichung E-3 der Welle 14 gilt für
    /// jede Ausprägung dieser Komponente.
    /// </summary>
    [Fact]
    public void Loeschen_fragt_zurueck()
    {
        string? geloescht = null;
        IRenderedComponent<ModulKatalogDialog> cut = Verwaltung(new ModulKatalogWege
        {
            Liste = Alle,
            Hersteller = () => new[] { "Alpha AG" },
            ListeGefiltert = Gefiltert,
            Detail = Felder,
            Loeschen = n => { geloescht = n; return new KatalogSpeicherErgebnis(true, "", n); }
        });

        cut.FindAll(".epos-leiste .epos-knopf")[2].Click();
        Assert.True(cut.Instance.Loeschfrage);
        Assert.Null(geloescht);

        cut.Find(".epos-rueckfrage .epos-knopf--primaer").Click();
        Assert.Equal("Alpha 2500TL", geloescht);
    }

    // =================================================================================
    //  Der IMPORT
    // =================================================================================

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

    private static IReadOnlyList<CecWechselrichter> Liste() => new[]
    {
        Geraet("Alpha AG: A-3000", 3000),
        Geraet("Alpha AG: A-5000", 5000),
        Geraet("Beta GmbH: B-10000", 10000)
    };

    private IRenderedComponent<WechselrichterImportDialog> Import(
        Func<CecWechselrichter, Task<PvVorpruefung>>? vorpruefen = null,
        Func<CecWechselrichter, string, Task<bool>>? anlegen = null,
        Action<bool>? geschlossen = null)
    {
        return Render<WechselrichterImportDialog>(p => p
            .Add(x => x.Laden, (_, __) => Task.FromResult(
                new WrLeseErgebnis(true, Liste(), new CecFortschritt("CEC_MSG_GELADEN", "3"))))
            .Add(x => x.Vorpruefen, vorpruefen ?? (_ => Task.FromResult(
                new PvVorpruefung(ImportBefund.Neu, null, null))))
            .Add(x => x.Anlegen, anlegen ?? ((_, __) => Task.FromResult(true)))
            .Add(x => x.Ueberschreiben, (_, __) => Task.FromResult(true))
            .Add(x => x.Meldungstext, f => f.Schluessel)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));
    }

    /// <summary>
    /// Vor dem Laden ist das Raster leer; „CEC-Liste laden" füllt es und meldet die
    /// Zahl der Treffer.
    /// </summary>
    [Fact]
    public void Der_Import_laedt_die_Liste_in_das_Raster()
    {
        IRenderedComponent<WechselrichterImportDialog> cut = Import();

        Assert.Equal("Wechselrichter einlesen (CEC)", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal(0, cut.Instance.SichtbareZeilen);

        cut.Find(".epos-leiste .epos-knopf--primaer").Click();

        Assert.Equal(3, cut.Instance.SichtbareZeilen);
    }

    /// <summary>
    /// Die Filterleiste: Herstellerwahl und Suchmuster engen ein, „Zurücksetzen" gibt
    /// die Liste wieder frei. Der erste Eintrag der Herstellerliste ist ein
    /// STEUERWERT und kein Anzeigetext, gegen den verglichen wird.
    /// </summary>
    [Fact]
    public void Der_Import_filtert_nach_Hersteller_und_Suchmuster()
    {
        IRenderedComponent<WechselrichterImportDialog> cut = Import();
        cut.Find(".epos-leiste .epos-knopf--primaer").Click();

        var hersteller = cut.FindAll(".epos-pvimport-filter select")[0];
        Assert.Equal(3, hersteller.QuerySelectorAll("option").Count);   // alle + zwei Firmen

        hersteller.Change("1");
        Assert.Equal(2, cut.Instance.SichtbareZeilen);

        cut.FindAll(".epos-pvimport-filter input[type=text]")[0].Input("*10000*");
        Assert.Equal(0, cut.Instance.SichtbareZeilen);

        cut.FindAll(".epos-pvimport-filter .epos-knopf").Last().Click();
        Assert.Equal(3, cut.Instance.SichtbareZeilen);
    }

    /// <summary>
    /// Die Wahl einer Zeile füllt die Detailfelder — und die Kennlinie steht als
    /// gerechnete Stützstelle da, nicht als Rohwert der Datei.
    /// </summary>
    [Fact]
    public void Eine_gewaehlte_Zeile_zeigt_ihre_gerechnete_Kennlinie()
    {
        IRenderedComponent<WechselrichterImportDialog> cut = Import();
        cut.Find(".epos-leiste .epos-knopf--primaer").Click();

        cut.FindAll(".epos-anlagenwahl")[0].Click();
        Assert.NotNull(cut.Instance.Gewaehlt);
        Assert.Equal("Alpha AG: A-3000", cut.Instance.Gewaehlt!.Name);

        // Der Pruefwert aus Konzept 3.3.3: eta bei Nennlast ist Paco/Pdco.
        double?[] etas = cut.Instance.Gewaehlt.Stuetzstellen();
        Assert.Equal(3000.0 / (3000.0 * 1.05), etas[5]!.Value, 12);
    }

    /// <summary>
    /// „Übernehmen" ohne Auswahl meldet sich, statt stillzuschweigen; mit Auswahl
    /// geht der Satz an den Schreibweg.
    /// </summary>
    [Fact]
    public void Uebernehmen_ohne_Auswahl_meldet_sich()
    {
        string? angelegt = null;
        IRenderedComponent<WechselrichterImportDialog> cut =
            Import(anlegen: (g, name) => { angelegt = name; return Task.FromResult(true); });

        cut.Find(".epos-leiste .epos-knopf--primaer").Click();

        // Ohne Auswahl ist der Knopf gesperrt - der Weg ueber die Tastatur meldet.
        Assert.True(cut.FindAll(".epos-leiste")[1].QuerySelectorAll("button")[0].HasAttribute("disabled"));

        cut.FindAll(".epos-anlagenwahl")[1].Click();
        cut.FindAll(".epos-leiste")[1].QuerySelectorAll("button")[0].Click();

        Assert.Equal("Alpha AG: A-5000", angelegt);
    }

    /// <summary>
    /// <b>Ein Plausibilitätsfehler SPERRT die Übernahme</b>, eine Warnung fragt
    /// zurück — dieselbe Zweiteilung wie beim Modulimport.
    /// </summary>
    [Fact]
    public void Ein_Plausibilitaetsfehler_sperrt_die_Uebernahme()
    {
        string? angelegt = null;
        IRenderedComponent<WechselrichterImportDialog> cut = Import(
            vorpruefen: _ => Task.FromResult(
                new PvVorpruefung(ImportBefund.Neu, null, null, "Die Kennlinie taugt nicht.", true)),
            anlegen: (g, name) => { angelegt = name; return Task.FromResult(true); });

        cut.Find(".epos-leiste .epos-knopf--primaer").Click();
        cut.FindAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-leiste")[1].QuerySelectorAll("button")[0].Click();

        Assert.Null(angelegt);
        Assert.Contains("Die Kennlinie taugt nicht.", cut.Instance.Meldung);
        Assert.False(cut.Instance.PlausiOffen);
    }

    /// <summary>Eine Warnung fragt zurück; „Nein" schreibt nichts.</summary>
    [Fact]
    public void Eine_Plausibilitaetswarnung_fragt_zurueck()
    {
        string? angelegt = null;
        IRenderedComponent<WechselrichterImportDialog> cut = Import(
            vorpruefen: _ => Task.FromResult(
                new PvVorpruefung(ImportBefund.Neu, null, null, "Die MPPT-Zahl fehlt.", false)),
            anlegen: (g, name) => { angelegt = name; return Task.FromResult(true); });

        cut.Find(".epos-leiste .epos-knopf--primaer").Click();
        cut.FindAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-leiste")[1].QuerySelectorAll("button")[0].Click();

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

        IRenderedComponent<WechselrichterImportDialog> cut = Import(
            vorpruefen: _ => Task.FromResult(new PvVorpruefung(
                ImportBefund.NameVorhanden, new[] { pruefung }, new[] { "alpha ag: a-3000" })));

        cut.Find(".epos-leiste .epos-knopf--primaer").Click();
        cut.FindAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-leiste")[1].QuerySelectorAll("button")[0].Click();

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
        IRenderedComponent<WechselrichterImportDialog> cut = Import(geschlossen: b => ergebnis = b);

        cut.FindAll(".epos-leiste")[1].QuerySelectorAll("button")[1].Click();

        Assert.False(ergebnis);
    }
}
