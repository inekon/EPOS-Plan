using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Waermepumpe;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <b>Die Parameterübersicht</b> — Anwenderwunsch <b>W14a‑E‑8</b> vom 06.09.2026:
/// „Für alle Menüs mit Anlagendaten: Erstelle einen Bearbeiten-Dialog zusätzlich im
/// Bearbeiten-Menü (optionale Anzeige), in dem 1. alle verfügbaren Parameter und
/// Eigenschaften angezeigt werden und 2. alle verwendeten Parameter gekennzeichnet
/// sind."
///
/// <para><b>Was geprüft wird.</b> Der Baustein selbst (zu/auf, Zeilenzahl,
/// Kennzeichnung als TEXT, „–" bei NULL, englische Kultur) und danach je
/// AUSPRÄGUNG mindestens ein Fall: die vier Katalogbrowser, die zwei Modulkataloge
/// und der Wärmepumpen-Stammdialog. Sieben Ausprägungen, drei Wirte — genau die
/// Menge, die der Anwenderwunsch nennt.</para>
///
/// <para><b>Ohne Delegat kein Aufklapper.</b> Der letzte Fall hält fest, dass ein
/// Dialog ohne <c>Uebersicht</c> unverändert zeichnet — sonst stünde bei jedem
/// Wirt, der ihn nicht füllt, ein leerer Knopf.</para>
///
/// <para>Die Sprache pinnt die Klasse selbst (Hausregel seit iU9‑W8).</para>
/// </summary>
public class ParameteruebersichtTests : BunitContext
{
    public ParameteruebersichtTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Kultur("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static void Kultur(string name)
    {
        var k = new CultureInfo(name);
        CultureInfo.DefaultThreadCurrentCulture = k;
        CultureInfo.DefaultThreadCurrentUICulture = k;
        Thread.CurrentThread.CurrentCulture = k;
        Thread.CurrentThread.CurrentUICulture = k;
        CultureInfo.CurrentCulture = k;
        CultureInfo.CurrentUICulture = k;
    }

    private static string T(string schluessel) => Resource.ResourceManager.GetString(schluessel) ?? schluessel;

    private static string Englisch(string schluessel) =>
        Resource.ResourceManager.GetString(schluessel, new CultureInfo("en-US")) ?? schluessel;

    // =====================================================================
    //  Prüfstände
    // =====================================================================

    /// <summary>
    /// Die Zeilen einer Anlagenart mit erfundenen Werten — der Baustein liest nichts,
    /// er bekommt die fertige Liste (wie am Gerät aus
    /// <c>ParameterUebersichtCtrl.Werte</c>).
    /// </summary>
    private static IReadOnlyList<Parameterwert> Zeilen(Anlagenart art, bool leer = false)
    {
        return ParameterVerwendung.Katalog(art, T)
            .Select(e => new Parameterwert(e, leer ? ParameterVerwendung.LEER : "42"))
            .ToList();
    }

    private IRenderedComponent<Parameteruebersicht> Uebersicht(Anlagenart art, bool leer = false) =>
        Render<Parameteruebersicht>(p => p.Add(x => x.Zeilen, Zeilen(art, leer)));

    // =====================================================================
    //  1 - Der Baustein
    // =====================================================================

    /// <summary>
    /// Vorgabe ZU: Der Knopf steht, die Tabelle nicht. Der Dialog sieht damit aus wie
    /// vor dem Anwenderwunsch.
    /// </summary>
    [Fact]
    public void Der_Aufklapper_ist_von_vorn_zu()
    {
        IRenderedComponent<Parameteruebersicht> k = Uebersicht(Anlagenart.Heizkessel);

        Assert.False(k.Instance.Offen);
        Assert.Equal("false", k.Find("button.epos-modulparameter-knopf").GetAttribute("aria-expanded"));
        Assert.Empty(k.FindAll("table.epos-parameteruebersicht-raster"));
        Assert.Contains(T("PARV_AUFKLAPPER"), k.Markup);
    }

    /// <summary>
    /// Aufgeklappt steht eine Zeile je Katalogeintrag — und der Kopf sagt an, dass
    /// der Block offen ist.
    /// </summary>
    [Fact]
    public void Aufgeklappt_steht_eine_Zeile_je_Katalogeintrag()
    {
        IRenderedComponent<Parameteruebersicht> k = Uebersicht(Anlagenart.Heizkessel);
        k.Find("button.epos-modulparameter-knopf").Click();

        Assert.True(k.Instance.Offen);
        Assert.Equal("true", k.Find("button.epos-modulparameter-knopf").GetAttribute("aria-expanded"));
        Assert.Equal(ParameterVerwendung.Katalog(Anlagenart.Heizkessel).Count,
                     k.FindAll("table.epos-parameteruebersicht-raster tbody tr").Count);
    }

    /// <summary>
    /// Ein zweiter Druck klappt wieder zu — der Zustand gehört dem Anwender.
    /// </summary>
    [Fact]
    public void Ein_zweiter_Druck_klappt_wieder_zu()
    {
        IRenderedComponent<Parameteruebersicht> k = Uebersicht(Anlagenart.Bhkw);
        k.Find("button.epos-modulparameter-knopf").Click();
        k.Find("button.epos-modulparameter-knopf").Click();

        Assert.False(k.Instance.Offen);
        Assert.Empty(k.FindAll("table.epos-parameteruebersicht-raster"));
    }

    /// <summary>
    /// <b>Die Kennzeichnung steht als TEXT da</b>, nicht nur als Farbe oder Zeichen —
    /// Hausregel für jeden Zustand, den ein Anwender unterscheiden muss.
    /// </summary>
    [Fact]
    public void Die_Verwendung_steht_als_Text_da()
    {
        IRenderedComponent<Parameteruebersicht> k = Uebersicht(Anlagenart.Heizkessel);
        k.Find("button.epos-modulparameter-knopf").Click();

        Assert.Contains(T("PARV_VERW_SIMULATION"), k.Markup);
        Assert.Contains(T("PARV_VERW_WIRTSCHAFT"), k.Markup);
        Assert.Contains(T("PARV_VERW_BERICHT"), k.Markup);
        Assert.Contains(T("PARV_VERW_DIALOG"), k.Markup);

        // Und jedes Kennzeichen trägt seinen Wortlaut in einem eigenen Element -
        // das Zeichen davor ist aria-hidden und nie die einzige Aussage.
        Assert.NotEmpty(k.FindAll("span.epos-verwendung span.epos-verwendung-text"));
        foreach (var zeichen in k.FindAll("span.epos-verwendung-zeichen"))
            Assert.Equal("true", zeichen.GetAttribute("aria-hidden"));
    }

    /// <summary>
    /// „nicht verwendet" erscheint dort, wo der Katalog es sagt — die fünf Maße der
    /// Wärmepumpe aus dem VDI-3805-Import.
    /// </summary>
    [Fact]
    public void Nicht_verwendet_wird_benannt()
    {
        IRenderedComponent<Parameteruebersicht> k = Uebersicht(Anlagenart.Waermepumpe);
        k.Find("button.epos-modulparameter-knopf").Click();

        Assert.Contains(T("PARV_VERW_KEINE"), k.Markup);
        Assert.Equal(5, k.FindAll("span.epos-verwendung--keine").Count);
    }

    /// <summary>
    /// NULL wird zum Halbgeviertstrich und nicht zur 0 — eine nicht gepflegte Größe
    /// ist etwas anderes als eine gemessene Null (Regel aus W6‑E‑1).
    /// </summary>
    [Fact]
    public void Ein_leerer_Wert_zeigt_den_Strich()
    {
        IRenderedComponent<Parameteruebersicht> k = Uebersicht(Anlagenart.Pufferspeicher, leer: true);
        k.Find("button.epos-modulparameter-knopf").Click();

        var werte = k.FindAll("td.epos-parameteruebersicht-wert");
        Assert.NotEmpty(werte);
        foreach (var td in werte)
            Assert.StartsWith(ParameterVerwendung.LEER, td.TextContent.Trim());
    }

    /// <summary>
    /// Die Einheit steht hinter dem Wert, nicht in einer eigenen Spalte — dieselbe
    /// Anordnung wie im Formularraster (iU8‑E‑2).
    /// </summary>
    [Fact]
    public void Die_Einheit_steht_hinter_dem_Wert()
    {
        IRenderedComponent<Parameteruebersicht> k = Uebersicht(Anlagenart.Heizkessel);
        k.Find("button.epos-modulparameter-knopf").Click();

        // Tab_Heizkessel_STAMM.Ptherm traegt „kW".
        Assert.Contains("42 kW", k.Markup);
    }

    /// <summary>
    /// Auf englischer Oberfläche stehen die englischen Texte — der Baustein holt sie
    /// selbst aus <c>MyResource</c> (Bauart <c>LizenzTexte</c>).
    /// </summary>
    [Fact]
    public void Auf_englischer_Oberflaeche_stehen_englische_Texte()
    {
        Kultur("en-US");
        try
        {
            IRenderedComponent<Parameteruebersicht> k = Uebersicht(Anlagenart.Stromspeicher);
            k.Find("button.epos-modulparameter-knopf").Click();

            Assert.Contains(Englisch("PARV_AUFKLAPPER"), k.Markup);
            Assert.Contains(Englisch("PARV_VERW_SIMULATION"), k.Markup);
            Assert.DoesNotContain("nicht verwendet", k.Markup);
        }
        finally
        {
            Kultur("de-DE");
        }
    }

    /// <summary>
    /// Ein leerer Satz Zeilen sperrt den Aufklapper nicht — er zeigt eine leere
    /// Tabelle. Der Knopf verschwindet nie, sonst wäre er ein wandernder Bedienpunkt.
    /// </summary>
    [Fact]
    public void Ohne_Zeilen_bleibt_der_Knopf_stehen()
    {
        IRenderedComponent<Parameteruebersicht> k =
            Render<Parameteruebersicht>(p => p.Add(x => x.Zeilen, Array.Empty<Parameterwert>()));

        k.Find("button.epos-modulparameter-knopf").Click();
        Assert.Single(k.FindAll("table.epos-parameteruebersicht-raster"));
        Assert.Empty(k.FindAll("table.epos-parameteruebersicht-raster tbody tr"));
    }

    // =====================================================================
    //  2 - Die sieben Auspraegungen in ihren drei Wirten
    // =====================================================================

    private IRenderedComponent<KatalogBrowserDialog> Browser(KatalogBrowserArt art, Anlagenart anlage)
    {
        KatalogBrowserProfil profil = KatalogBrowserProfil.Finde(art, T);

        var wege = new KatalogBrowserWege
        {
            Liste = (_, __) => new[] { new BrowserZeile(1, "Satz A") },
            Detail = name => profil.Detailfelder.Select(f => new BrowserFeldwert
            {
                Schluessel = f.Schluessel,
                Bezeichnung = f.Bezeichnung,
                Einheit = f.Einheit,
                Art = f.Art,
                Editierbar = f.Editierbar,
                Wert = f.Schluessel == KatalogBrowserProfil.FeldBezeichner ? name : "1"
            }).ToList()
        };

        return Render<KatalogBrowserDialog>(p => p
            .Add(x => x.Art, art)
            .Add(x => x.ProfilVorgabe, profil)
            .Add(x => x.Wege, wege)
            .Add(x => x.FilterEins, new[] { (0, "Alle") })
            .Add(x => x.FilterZwei, new[] { (0, "Alle") })
            .Add(x => x.Uebersicht, _ => Zeilen(anlage)));
    }

    /// <summary>
    /// Die vier Katalogbrowser tragen den Aufklapper, und aufgeklappt steht je
    /// Katalogeintrag eine Zeile.
    /// </summary>
    [Theory]
    [InlineData(KatalogBrowserArt.Heizkessel, Anlagenart.Heizkessel)]
    [InlineData(KatalogBrowserArt.Bhkw, Anlagenart.Bhkw)]
    [InlineData(KatalogBrowserArt.Solarkollektoren, Anlagenart.Solarkollektoren)]
    [InlineData(KatalogBrowserArt.Pufferspeicher, Anlagenart.Pufferspeicher)]
    public void Jeder_Katalogbrowser_traegt_die_Uebersicht(KatalogBrowserArt art, Anlagenart anlage)
    {
        IRenderedComponent<KatalogBrowserDialog> k = Browser(art, anlage);

        Assert.Single(k.FindAll("div.epos-parameteruebersicht"));
        k.Find("div.epos-parameteruebersicht button").Click();

        Assert.Equal(ParameterVerwendung.Katalog(anlage).Count,
                     k.FindAll("table.epos-parameteruebersicht-raster tbody tr").Count);
    }

    private IRenderedComponent<ModulKatalogDialog> Modulkatalog(ModulKatalogArt art, Anlagenart anlage)
    {
        ModulKatalogProfil profil = ModulKatalogProfil.Finde(art, T);

        var wege = new ModulKatalogWege
        {
            Liste = () => new[] { new ModulZeile(1, "Modul A") },
            Detail = name => profil.Felder.Select(f => new ModulFeldwert
            {
                Schluessel = f.Schluessel,
                Bezeichnung = f.Bezeichnung,
                Einheit = f.Einheit,
                Art = f.Art,
                Gruppe = f.Gruppe,
                Wert = f.Schluessel == ModulKatalogProfil.FeldBezeichner ? name : "1"
            }).ToList()
        };

        return Render<ModulKatalogDialog>(p => p
            .Add(x => x.Art, art)
            .Add(x => x.ProfilVorgabe, profil)
            .Add(x => x.Wege, wege)
            .Add(x => x.Uebersicht, _ => Zeilen(anlage)));
    }

    /// <summary>Die zwei Modulkataloge tragen den Aufklapper.</summary>
    [Theory]
    [InlineData(ModulKatalogArt.Photovoltaik, Anlagenart.Photovoltaik)]
    [InlineData(ModulKatalogArt.Stromspeicher, Anlagenart.Stromspeicher)]
    public void Jeder_Modulkatalog_traegt_die_Uebersicht(ModulKatalogArt art, Anlagenart anlage)
    {
        IRenderedComponent<ModulKatalogDialog> k = Modulkatalog(art, anlage);

        Assert.Single(k.FindAll("div.epos-parameteruebersicht"));
        k.Find("div.epos-parameteruebersicht button").Click();

        Assert.Equal(ParameterVerwendung.Katalog(anlage).Count,
                     k.FindAll("table.epos-parameteruebersicht-raster tbody tr").Count);
    }

    /// <summary>
    /// Die siebte Ausprägung: der Wärmepumpen-Stammdialog. Hier ist die Übersicht die
    /// Auskunft, die im Bestand fehlte — die Maske zeigt elf der achtzehn
    /// Fachspalten.
    /// </summary>
    [Fact]
    public void Der_Waermepumpen_Stammdialog_traegt_die_Uebersicht()
    {
        IRenderedComponent<WaermepumpeStammDialog> k = Render<WaermepumpeStammDialog>(p => p
            .Add(x => x.Liste, () => (IReadOnlyList<WaermepumpeStammZeile>)
                 new[] { new WaermepumpeStammZeile(1, "WP A", false) })
            .Add(x => x.Satz, _ => new WaermepumpeStammDaten { Id = 1, Name = "WP A" })
            .Add(x => x.Uebersicht, _ => Zeilen(Anlagenart.Waermepumpe)));

        Assert.Single(k.FindAll("div.epos-parameteruebersicht"));
        k.Find("div.epos-parameteruebersicht button").Click();

        Assert.Equal(ParameterVerwendung.Katalog(Anlagenart.Waermepumpe).Count,
                     k.FindAll("table.epos-parameteruebersicht-raster tbody tr").Count);
    }

    /// <summary>
    /// <b>Ohne Delegat kein Aufklapper.</b> Ein Wirt, der die Übersicht nicht füllt,
    /// zeichnet unverändert — kein leerer Knopf, keine leere Tabelle.
    /// </summary>
    [Fact]
    public void Ohne_Delegat_bleibt_der_Dialog_unveraendert()
    {
        KatalogBrowserProfil profil = KatalogBrowserProfil.Finde(KatalogBrowserArt.Heizkessel, T);

        IRenderedComponent<KatalogBrowserDialog> k = Render<KatalogBrowserDialog>(p => p
            .Add(x => x.Art, KatalogBrowserArt.Heizkessel)
            .Add(x => x.ProfilVorgabe, profil)
            .Add(x => x.Wege, new KatalogBrowserWege())
            .Add(x => x.FilterEins, new[] { (0, "Alle") })
            .Add(x => x.FilterZwei, new[] { (0, "Alle") }));

        Assert.Empty(k.FindAll("div.epos-parameteruebersicht"));
    }
}
