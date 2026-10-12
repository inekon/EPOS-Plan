using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Kältemaschine nach dem Schema der Projektdialoge</b> (AW3b, Entscheide KA-E-17 bis KA-E-22): die Zeile
/// der Projektliste ist die Wahl (Hervorhebung, ↑ ↓ Pos1 Ende, kein Wahlknopf), die Knopfleiste des Katalogs steht
/// am gewählten Satz, Schloss… und Löschen sind beim Projektsatz weich gesperrt, Bearbeiten… je Satzart, Neu… legt
/// einen Katalogsatz an. Gegen den Stub der Hülle aus <see cref="KaeltemaschineAnlageDialogTests"/>.
/// </summary>
public class KaeltemaschineAnlageKnopfleisteTests : EposBunitContext
{
    public KaeltemaschineAnlageKnopfleisteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static KaeltemaschineAnlageDialogTests.Projekt ZweiAnlagen()
    {
        var p = new KaeltemaschineAnlageDialogTests.Projekt();
        p.Gespeicherte.Add(new KaeltemaschineAnlageDaten
        {
            AnlagenId = 8, GeraetId = 80, Bezeichner = "KM Neubau", Anzahl = 1, KuehlCarrierId = 1,
            Geraet = new KaeltemaschineGeraetwerte { Bezeichner = "Kältemaschine 60 kW", Nennkaelteleistung = 60, NennEer = 3.6 }
        });
        return p;
    }

    private readonly Schlosspruefung _schloss = new();

    private IRenderedComponent<KaeltemaschineAnlageDialog> Aufbauen(KaeltemaschineAnlageDialogTests.Projekt p, bool mitEditor = false)
        => KaeltemaschineAnlageDialogTests.Aufbauen(this, p, b =>
        {
            b.Add(x => x.Vergleichsparameter, _ => Array.Empty<Parameterwert>())
             .Add(x => x.Schloss, _schloss.Weg());
            if (mitEditor)
                b.Add(x => x.KatalogGaben, () => new Dictionary<string, object>
                {
                    ["Katalogzeilen"] = new Func<IReadOnlyList<Katalogfilterzeile>>(p.Katalog)
                });
        });

    private static IElement AmSatz(IRenderedComponent<KaeltemaschineAnlageDialog> cut, string klasse)
        => cut.Find(".epos-zweispalten-satzknoepfe ." + klasse);

    private static IReadOnlyList<IElement> Projektzeilen(IRenderedComponent<KaeltemaschineAnlageDialog> cut)
        => cut.Find(".epos-zweispalten-bereich--projekt").QuerySelectorAll(".epos-projektzeile").ToList();

    [Fact]
    public void Die_Projektliste_traegt_keinen_Wahlknopf_die_Zeile_ist_die_Wahl()
    {
        var cut = Aufbauen(ZweiAnlagen());
        var liste = cut.Find(".epos-zweispalten-bereich--projekt");

        Assert.Empty(liste.QuerySelectorAll(".epos-zeilenwahl, .epos-anlagenwahl, input[type=radio]"));
        Assert.Equal(2, liste.QuerySelectorAll("td .epos-wahlkaestchen").Length);
        Assert.Equal(2, liste.QuerySelectorAll("td.epos-spalte-zeilenzelle > .epos-zeilenzelle").Length);
        Assert.NotNull(cut.Find(".epos-zweispalten-bereich--projekt .epos-zweispalten-inhalt--zeilenwahl"));

        Projektzeilen(cut)[1].Click();
        var zeilen = liste.QuerySelectorAll("tbody tr").ToList();
        zeilen = cut.Find(".epos-zweispalten-bereich--projekt").QuerySelectorAll("tbody tr").ToList();
        Assert.Single(zeilen, tr => tr.ClassList.Contains("epos-zeile--gewaehlt"));
        Assert.Contains("epos-zeile--gewaehlt", zeilen[1].ClassList);
        Assert.Equal("true", zeilen[1].QuerySelector(".epos-zeilenzelle")!.GetAttribute("aria-current"));
        Assert.Equal("KM Neubau", cut.Find(".epos-zweispalten-satzname").TextContent);
        Assert.Equal(0, cut.Instance.ProjektWahl.Anzahl);                                    // Kästchen unberührt
    }

    [Fact]
    public void Die_Pfeiltasten_bewegen_die_Wahl_der_Projektliste()
    {
        var cut = Aufbauen(ZweiAnlagen());
        string huelle = ".epos-zweispalten-bereich--projekt .epos-raster-huelle";
        Assert.Equal("0", cut.Find(huelle).GetAttribute("tabindex"));

        cut.Find(huelle).KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal("KM Neubau", cut.Find(".epos-zweispalten-satzname").TextContent);
        cut.Find(huelle).KeyDown(new KeyboardEventArgs { Key = "Home" });
        Assert.Equal("KM Bestand", cut.Find(".epos-zweispalten-satzname").TextContent);
        cut.Find(huelle).KeyDown(new KeyboardEventArgs { Key = "End" });
        Assert.Equal("KM Neubau", cut.Find(".epos-zweispalten-satzname").TextContent);
    }

    [Fact]
    public void Die_Knopfleiste_steht_am_Satz_in_fester_Folge_ohne_Stift_und_Fussleiste()
    {
        var cut = Aufbauen(ZweiAnlagen(), mitEditor: true);

        Assert.Empty(cut.FindAll(".epos-zweispalten-fussleiste"));
        Assert.Empty(cut.FindAll(".epos-zweispalten-satzkopf > .epos-zweispalten-bearbeiten"));
        Assert.Null(cut.Find(".epos-zweispalten-bereich--projekt > .epos-zweispalten-kopfleiste").QuerySelector(".epos-knopf--bearbeiten-projekt"));
        string[] folge = { "epos-knopf--vergleichen", "epos-katalogschloss", "epos-knopf--loeschen",
                           "epos-knopf--bearbeiten-projekt", "epos-knopf--neu", "epos-kaeltemaschine-typkennfelder",
                           "epos-kaeltemaschine-verwaltung" };
        var namen = cut.Find(".epos-zweispalten-satzkopf > .epos-zweispalten-satzknoepfe").Children
            .Select(e => folge.FirstOrDefault(f => e.ClassList.Contains(f))).OfType<string>().ToList();
        Assert.Equal(folge.Where(namen.Contains), namen);
        foreach (string f in new[] { "epos-knopf--vergleichen", "epos-katalogschloss", "epos-knopf--loeschen", "epos-knopf--bearbeiten-projekt", "epos-knopf--neu" })
            Assert.Contains(f, namen);
    }

    [Fact]
    public void Beim_Projektsatz_sind_Schloss_und_Loeschen_weich_gesperrt()
    {
        var p = ZweiAnlagen();
        var cut = Aufbauen(p);
        Assert.Equal(R.AUSWAHL_MARKE_PROJEKTSATZ, cut.Find(".epos-zweispalten-satzzeile .epos-zweispalten-marke--satz").TextContent);

        foreach (string klasse in new[] { "epos-katalogschloss", "epos-knopf--loeschen" })
        {
            var knopf = AmSatz(cut, klasse);
            Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
            Assert.False(knopf.HasAttribute("disabled"));
            Assert.Equal(R.AUSWAHL_SPERRE_KATALOGSATZ, knopf.GetAttribute("title"));
        }
        AmSatz(cut, "epos-knopf--loeschen").Click();
        Assert.Contains(R.AUSWAHL_SPERRE_KATALOGSATZ, cut.Find(".epos-warnbanner-text").TextContent);
        Assert.Empty(cut.FindAll(".epos-rueckfrage--offen"));
        Assert.False(cut.Instance.Loeschfrage);
        AmSatz(cut, "epos-katalogschloss").Click();
        Assert.Empty(_schloss.Aufrufe);
        Assert.Empty(p.KatalogGeloescht);

        Assert.Empty(cut.FindAll(".epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-katalog"));
        AmSatz(cut, "epos-knopf--bearbeiten-projekt").Click();
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
    }

    [Fact]
    public void Beim_Katalogsatz_wirken_Loeschen_und_Bearbeiten_auf_den_Katalog()
    {
        var p = new KaeltemaschineAnlageDialogTests.Projekt { Gesperrt = true };
        var cut = Aufbauen(p, mitEditor: true);
        Zeilenklick.Zeile(cut, 1);                                                          // freier Katalogsatz

        foreach (string klasse in new[] { "epos-katalogschloss", "epos-knopf--loeschen" })
            Assert.False(AmSatz(cut, klasse).HasAttribute("aria-disabled"));
        Assert.Empty(cut.FindAll(".epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-projekt"));
        AmSatz(cut, "epos-knopf--loeschen").Click();
        Assert.True(cut.Instance.Loeschfrage);
        cut.Find(".epos-rueckfrage").QuerySelectorAll(".epos-knopf").First(b => b.TextContent.Trim() == R.ALLG_BTN_NEIN).Click();

        AmSatz(cut, "epos-knopf--bearbeiten-katalog").Click();                             // frei: Katalogeditor
        Assert.True(cut.Instance.EditorOffen);
        Assert.Equal(12, cut.Instance.EditorVorwahl);
    }

    [Fact]
    public void Der_gesperrte_Katalogsatz_oeffnet_ueber_Bearbeiten_nur_lesend()
    {
        var p = new KaeltemaschineAnlageDialogTests.Projekt { Gesperrt = true };
        var cut = Aufbauen(p, mitEditor: true);
        Zeilenklick.Zeile(cut, 0);                                                          // gesperrter Katalogsatz

        AmSatz(cut, "epos-knopf--bearbeiten-katalog").Click();
        Assert.False(cut.Instance.EditorOffen);
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Contains(R.ADM_SCHLOSS_ERST_AUFHEBEN, cut.Find(".epos-satzueberlagerung-koerper").TextContent);
    }

    [Fact]
    public void Der_Stift_je_Zeile_ist_Bearbeiten_fuer_diese_Zeile()
    {
        var cut = Aufbauen(ZweiAnlagen());

        cut.Find(".epos-zweispalten-bereich--projekt").QuerySelectorAll(".epos-zeilenstift")[1].Click();

        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Equal("KM Neubau", cut.Find(".epos-zweispalten-satzname").TextContent);
        Assert.Contains("epos-zeile--gewaehlt",
                        cut.Find(".epos-zweispalten-bereich--projekt").QuerySelectorAll("tbody tr")[1].ClassList);
    }

    [Fact]
    public void Vergleichen_beim_Projektsatz_vergleicht_die_angekreuzten_Anlagen()
    {
        var cut = Aufbauen(ZweiAnlagen());
        Assert.True((AmSatz(cut, "epos-knopf--vergleichen").GetAttribute("aria-disabled") == "true"));

        foreach (int i in new[] { 0, 1 })
            cut.Find(".epos-zweispalten-bereich--projekt").QuerySelectorAll("td .epos-wahlkaestchen")[i].Click();
        Assert.False((AmSatz(cut, "epos-knopf--vergleichen").GetAttribute("aria-disabled") == "true"));
        AmSatz(cut, "epos-knopf--vergleichen").Click();

        string P = WindowsFormsApplication1.MyResource.Resource.AUSWAHL_VERGLEICH_HERKUNFT_PROJEKT + " ";
        var koepfe = cut.FindAll(".epos-vergleich thead th").Select(th => th.TextContent).ToList();
        Assert.Equal(new[] { P + "KM Bestand", P + "KM Neubau" }, koepfe.Skip(1).ToArray());
        Assert.NotEmpty(cut.FindAll(".epos-vergleich tbody tr"));
    }

    [Fact]
    public void Neu_legt_einen_Katalogsatz_an_der_Editor_geht_im_Modus_Neu_auf()
    {
        var cut = Aufbauen(ZweiAnlagen(), mitEditor: true);

        AmSatz(cut, "epos-knopf--neu").Click();

        Assert.True(cut.Instance.EditorOffen);
        Assert.Equal(0, cut.Instance.EditorVorwahl);
        Assert.True(cut.FindComponent<KaeltemaschineKatalogDialog>().Instance.NeuBeimStart);
    }

    [Fact]
    public void Ohne_Katalogeditor_gibt_es_kein_Neu()
    {
        var cut = Aufbauen(ZweiAnlagen());
        Assert.Empty(cut.FindAll(".epos-zweispalten-satzknoepfe .epos-knopf--neu"));
    }

    [Fact]
    public void Die_Schlussleiste_ist_OK_und_Abbrechen_ohne_Symbol()
    {
        var cut = Aufbauen(ZweiAnlagen());
        var knoepfe = cut.FindAll(".epos-kaeltemaschine-anlage > .epos-leiste button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(new[] { R.ALLG_BTN_ABBRECHEN, R.ALLG_BTN_OK }, knoepfe.TakeLast(2).ToArray());
        Assert.DoesNotContain(knoepfe, k => k.Contains('❌') || k.Contains('✔'));
    }

    /// <summary>KA-E-24: Vergleichen… stellt eine Projektzeile (Werte des Projektgeräts über seine ID) und einen Katalogsatz nebeneinander.</summary>
    [Fact]
    public void KA_E_24_Vergleichen_stellt_Projektzeile_und_Katalogsatz_nebeneinander()
    {
        var cut = Aufbauen(ZweiAnlagen());
        var ids = new List<int>();
        cut.Render(p => p
            .Add(x => x.Vergleichsparameter, n => VergleichWirtHilfe.Werte("K:" + n))
            .Add(x => x.VergleichsparameterProjekt, id => { ids.Add(id); return VergleichWirtHilfe.Werte("P:" + id); }));
        VergleichWirtHilfe.ProjektzeileGegenKatalogsatz(cut, ids, "KM Bestand");
    }
}
