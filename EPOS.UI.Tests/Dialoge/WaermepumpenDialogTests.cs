using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Waermepumpe;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Wärmepumpen Verwaltung als Wirt der Katalogauswahl V1, Stufe 3 (Konzept Projektdialoge mit Katalogauswahl 4.2,
/// 4.6, 4.9): oben die Wärmepumpen des Projekts, darunter der Katalog, unten die Detailzeile mit Kenndaten und Kennlinie;
/// „Anlage…" öffnet die Anlagenseite als Überlagerung.
/// </summary>
public class WaermepumpenDialogTests : EposBunitContext
{
    public WaermepumpenDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static WaermepumpeAnlageDaten Zeile(string name, int nennleistung = 12) => new()
    {
        Bezeichner = name,
        Firma = "Bosch",
        IdWp = 1,
        Vorlauf = 35,
        Ruecklauf = 28,
        Nennleistung = nennleistung,
        Betriebsart = DbWerte.WP_BETRIEBSART_PARALLEL,
        SperrzeitVon = 0,
        SperrzeitBis = 0,
        HeizstabLeistung = 6
    };

    private static readonly Katalogfilterzeile[] Katalog =
    {
        new Katalogfilterzeile(1, "WP Neu")
            .MitText(Katalogfilterprofil.SpHersteller, "Alpha")
            .MitText(Katalogfilterprofil.SpBezeichner, "WP Neu")
            .MitText(Katalogfilterprofil.SpQuelle, "Sole-Wasser")
            .MitZahl(Katalogfilterprofil.SpNennleistung, 20, 1)
            .MitZahl(Katalogfilterprofil.SpVlMin, 35, 0)
            .MitZahl(Katalogfilterprofil.SpVlMax, 60, 0)
            .MitZahl(Katalogfilterprofil.SpZuheizung, 9, 1)
            .MitZahl(Katalogfilterprofil.SpKuehlleistung, 0.0, 1)
            .MitZahl(Katalogfilterprofil.SpCop, 4.1, 2)
    };

    private static readonly Katalogfilterprofil Katalogprofil =
        Katalogfilterprofil.MitVerwendung(Anlagenart.Waermepumpe);

    /// <summary>Der Parametersatz der eingebetteten Detailansicht; <paramref name="mangel"/> = Temperaturprüfung schlägt an.</summary>
    private static IReadOnlyDictionary<string, object> AnlageGaben(WaermepumpeAnlageDaten daten, string? mangel = null)
        => new Dictionary<string, object>
        {
            ["Daten"] = daten,
            ["TemperaturenPruefen"] = new Func<int?, int?, string?>((_, _) => mangel),
            ["Kennlinien"] = new Func<int, IReadOnlyList<KennlinienZeile>>(_ => new List<KennlinienZeile>())
        };

    private IRenderedComponent<WaermepumpenDialog> Aufbauen(
        List<WaermepumpeAnlageDaten>? zeilen = null,
        Func<string, WaermepumpeAnlageDaten?>? anlegen = null,
        Action<WaermepumpeAnlageDaten>? uebernehmen = null,
        Action<WaermepumpeAnlageDaten>? entfernen = null,
        bool wizard = false,
        Action<bool>? geschlossen = null,
        string? mangel = null,
        Func<WaermepumpeAnlageDaten, int, bool>? umstellen = null,
        IReadOnlyList<Katalogfilterzeile>? katalog = null,
        Func<WaermepumpeStammDaten, bool, KatalogSpeicherErgebnis>? katalogSpeichern = null,
        Func<int, IReadOnlyList<KennlinienZeile>>? katalogKennlinien = null,
        Func<int, IReadOnlyList<KennlinienZeile>, bool>? katalogKennlinienAbgleichen = null,
        Func<IReadOnlyDictionary<string, object>>? neuGaben = null,
        Func<int, string>? katalogLoeschen = null,
        Rueckwegwege? rueckweg = null,
        Func<WaermepumpeAnlageDaten, bool, Task>? kosten = null,
        Func<WaermepumpeAnlageDaten, IReadOnlyDictionary<string, object>>? anlageGaben = null,
        Func<WaermepumpeAnlageDaten, (double Invest, double Betrieb)>? kostensumme = null,
        Schlossweg? schloss = null)
        => Render<WaermepumpenDialog>(p => p
            .Add(x => x.Schloss, schloss)
            .Add(x => x.Zeilen, zeilen ?? new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") })
            .Add(x => x.Katalog, () => katalog ?? Katalog)
            .Add(x => x.Katalogprofil, Katalogprofil)
            .Add(x => x.AnlageGaben, anlageGaben ?? (d => AnlageGaben(d, mangel)))
            .Add(x => x.Anlegen, anlegen ?? (n => Zeile(n, 20)))
            .Add(x => x.Uebernehmen, uebernehmen)
            .Add(x => x.Entfernen, entfernen)
            .Add(x => x.Umstellen, umstellen ?? ((d, id) => { d.Bezeichner = "WP Neu"; d.IdWp = id; return true; }))
            .Add(x => x.ProjektSatz, d => Stamm(d.Bezeichner, d.Firma, d.Nennleistung))
            .Add(x => x.KatalogSatz, id => Stamm("WP Neu", "Alpha", 20))
            .Add(x => x.ProjektBilder, _ => KennlinienBilder.Leer)
            .Add(x => x.KatalogBilder, _ => KennlinienBilder.Leer)
            .Add(x => x.NeuGaben, neuGaben)
            .Add(x => x.KatalogSpeichern, katalogSpeichern)
            .Add(x => x.KatalogKennlinien, katalogKennlinien)
            .Add(x => x.KatalogKennlinienAbgleichen, katalogKennlinienAbgleichen)
            .Add(x => x.KatalogLoeschen, katalogLoeschen)
            .Add(x => x.RueckwegWege, rueckweg)
            .Add(x => x.KostenOeffnen, kosten)
            .Add(x => x.Kostensumme, kostensumme)
            .Add(x => x.Wizard, wizard)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static WaermepumpeStammDaten Stamm(string name, string firma, int nennleistung) => new()
    {
        Id = 1, Name = name, Firma = firma, Nennleistung = nennleistung, Heizstab = 6, Modulkosten = 9000
    };

    private static Satzbearbeitungswege Wege(List<(int Id, IReadOnlyList<BrowserFeldwert> Felder)>? gespeichert = null)
        => new()
        {
            Lesen = id => new List<BrowserFeldwert>
            {
                new() { Schluessel = "Firma", Bezeichnung = "Hersteller:", Art = BrowserFeldArt.Text, Editierbar = true, Wert = "Alpha" },
                new() { Schluessel = "Modulkosten", Bezeichnung = "Modulkosten:", Art = BrowserFeldArt.Zahl, Editierbar = true, Wert = "100" },
            },
            Speichern = l => { gespeichert?.AddRange(l); return new KatalogSpeicherErgebnis(true, "gespeichert", ""); }
        };

    private static IReadOnlyList<Katalogfilterzeile> ZweiSaetze(params int[] gesperrt)
    {
        var a = new Katalogfilterzeile(1, "WP Neu").MitText(Katalogfilterprofil.SpBezeichner, "WP Neu");
        var b = new Katalogfilterzeile(2, "WP Zwei").MitText(Katalogfilterprofil.SpBezeichner, "WP Zwei");
        a.Geschuetzt = gesperrt.Contains(1);
        b.Geschuetzt = gesperrt.Contains(2);
        return new[] { a, b };
    }

    /// <summary>Wählt eine Katalogzeile — im Katalog ist die ZEILE die Wahl (Kästchenmodus).</summary>
    private static void KatalogzeileWaehlen(IRenderedComponent<WaermepumpenDialog> cut, int nummer = 0)
        => cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[nummer].Click();

    private static void KatalogAnkreuzen(IRenderedComponent<WaermepumpenDialog> cut, params int[] zeilen)
    {
        foreach (int i in zeilen)
            cut.FindAll(".epos-raster")[1].QuerySelectorAll("td .epos-kaestchenzelle input")[i].Change(true);
    }

    private static void ProjektAnkreuzen(IRenderedComponent<WaermepumpenDialog> cut, params int[] zeilen)
    {
        foreach (int i in zeilen)
            cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody td.epos-spalte-kaestchen .epos-wahlkaestchen")[i].Click();
    }

    /// <summary>
    /// UeS2: Das Fragment des Satzes steht nur in der Satz-Überlagerung — „Bearbeiten…" der
    /// Knopfleiste am Satz (AW1) öffnet sie (falls zu); zurück kommt ihr Körper.
    /// </summary>
    private static AngleSharp.Dom.IElement Satz(IRenderedComponent<WaermepumpenDialog> cut)
    {
        if (cut.FindAll(".epos-satzueberlagerung-koerper").Count == 0)
            cut.Find(BEARBEITEN_AM_SATZ).Click();
        return cut.Find(".epos-satzueberlagerung-koerper");
    }

    /// <summary>„Bearbeiten…" der Knopfleiste am gewählten Satz (AW1), je nach Satzart.</summary>
    private const string BEARBEITEN_AM_SATZ =
        ".epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-projekt, .epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-katalog";

    /// <summary>UeS2: Schließt eine offene Satz-Überlagerung wie Abbrechen bzw. Schließen.</summary>
    private static void SatzZu(IRenderedComponent<WaermepumpenDialog> cut)
    {
        var knopf = cut.FindAll(".epos-satzueberlagerung-abbrechen, .epos-satzueberlagerung-schliessen");
        if (knopf.Count > 0) knopf[0].Click();
    }

    /// <summary>UeS2: Das OK der Satz-Überlagerung — es speichert, wie früher „Speichern" des Aufklappers.</summary>
    private static void Ok(IRenderedComponent<WaermepumpenDialog> cut)
        => cut.Find(".epos-satzueberlagerung-ok").Click();

    private static IElement Knopf(IRenderedComponent<WaermepumpenDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    /// <summary>Die Wahl einer Zeile der PROJEKTliste (das erste Raster): ihre erste Zelle (AW1: die Zeile ist die Wahl).</summary>
    private static IElement Projektwahl(IRenderedComponent<WaermepumpenDialog> cut, int index)
        => cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr")[index].QuerySelector(".epos-projektzeile")!;

    private static IElement Pfeil(IRenderedComponent<WaermepumpenDialog> cut, int index)
        => cut.Find(index == 0 ? ".epos-zweispalten-knopf--uebernehmen" : ".epos-zweispalten-knopf--entfernen");

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Die_Spalten_der_Projektliste_stehen()
    {
        var cut = Aufbauen();
        var kopf = cut.FindAll(".epos-raster")[0].QuerySelectorAll("th:not(.epos-spalte-kaestchen)")
                      .Select(e => e.TextContent.Trim()).ToList();

        // AW1: keine Spalte „Wahl" mehr - die Zeile ist die Wahl.
        Assert.Equal(new[] { "Hersteller", "Typ", "Leistung [kW]", "Vorlauf [°C]",
                             "Rücklauf [°C]", "Betriebsart", "Bearbeiten" }, kopf);
        // KA-E-5: die Kästchenspalte der Mehrfachwahl steht davor.
        Assert.Single(cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody td.epos-spalte-kaestchen .epos-wahlkaestchen"));
    }

    [Fact]
    public void Zwei_Listen_zwei_Pfeile_und_die_OK_Leiste_stehen_keine_Ueberlagerung_offen()
    {
        var cut = Aufbauen();

        Assert.Equal(2, cut.FindAll(".epos-raster").Count);
        var pfeile = cut.FindAll(".epos-zweispalten-knopf--richtung").Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(2, pfeile.Count);
        Assert.Contains(pfeile, t => t.Contains("In das Projekt übernehmen"));
        Assert.Contains(pfeile, t => t.Contains("Aus dem Projekt entfernen"));

        var knoepfe = cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Contains("OK", knoepfe);
        Assert.Contains(Resource.ALLG_BTN_ABBRECHEN, knoepfe);   // AW2: die Schlussleiste der anderen Projektdialoge
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
        // AW2: Die Anlagenfelder stehen nur in der Satz-Ueberlagerung („Bearbeiten…").
        Assert.Empty(cut.FindAll(".epos-wp-anlage"));
        Assert.False(cut.Instance.DetailOffen);
    }

    [Fact]
    public void Die_Zeile_zeigt_die_Werte_der_Anlage()
    {
        var cut = Aufbauen();
        var zellen = cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody td")
                        .Select(e => e.TextContent.Trim()).ToList();

        Assert.Contains("Bosch", zellen);
        Assert.Contains("WP Alpha", zellen);
        Assert.Contains("12", zellen);
        Assert.Contains("35", zellen);
        Assert.Contains("28", zellen);
        Assert.Contains(DbWerte.WP_BETRIEBSART_PARALLEL, zellen);
    }

    [Fact]
    public void Die_englischen_Texte_lassen_sich_setzen_der_Kontext_steht_im_Dialogkopf()
    {
        var cut = Render<WaermepumpenDialog>(p => p
            .Add(x => x.Zeilen, new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") })
            .Add(x => x.TitelText, "Heat pump management")
            .Add(x => x.KopfbandText, "Enter the heat pump data")
            .Add(x => x.LabelHinzu, "Add to project"));

        Assert.Equal("Heat pump management", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal("Enter the heat pump data", cut.Find(".epos-dialog-kopf .epos-dialog-kontext").TextContent.Trim());
        Assert.Empty(cut.FindAll(".epos-kontextzeile"));
        Assert.Contains(cut.FindAll("button").Select(b => b.TextContent.Trim()), t => t.Contains("Add to project"));
    }

    [Fact]
    public void Im_Assistenten_fehlen_OK_Leiste_Kostenknoepfe_und_Bearbeiten()
    {
        var cut = Aufbauen(wizard: true, kosten: (_, _) => Task.CompletedTask);
        var knoepfe = cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.DoesNotContain("OK", knoepfe);
        Assert.Empty(cut.FindAll(".epos-kostenleiste"));
        // AW1: kein „Bearbeiten…" in der Projektleiste; am Satz zeigt es ohne Weg der Projektkopie nur die Ueberlagerung.
        Assert.Empty(cut.FindAll(".epos-zweispalten-bereich--projekt .epos-zweispalten-kopfleiste .epos-knopf--bearbeiten-projekt"));
        Assert.Empty(cut.FindAll(".epos-knopf--anlage"));        // AW2: kein „Anlage…" mehr
        Assert.Single(Satz(cut).QuerySelectorAll(".epos-wp-anlagefelder"));   // die Anlage geht auch im Assistenten auf
        Assert.Empty(cut.FindAll(".epos-wp-satz-kostenzeile .epos-kostenleiste"));
    }

    // =================================================================================
    // Knöpfe an ihrem Ort (4.2, 4.9)
    // =================================================================================

    [Fact]
    public void S3_Die_Knoepfe_stehen_an_ihrem_Ort_mit_Vergleichen_und_Neu()
    {
        var cut = Aufbauen(katalogLoeschen: _ => "", neuGaben: () => new Dictionary<string, object>(),
                           rueckweg: Rueckweg(Array.Empty<Rueckwegvorschlag>()));

        var projekt = cut.Find(".epos-zweispalten-bereich--projekt .epos-zweispalten-kopfleiste").TextContent;
        Assert.Contains("Markierte auf diese Wärmepumpe umstellen", projekt);
        Assert.DoesNotContain(Resource.AUSWAHL_BTN_BEARBEITEN, projekt);   // AW1: Bearbeiten nur am Satz
        Assert.Contains(Resource.KATRUECK_BTN, projekt);

        // AW1: keine Fussleiste - die Knoepfe stehen am gewaehlten Satz.
        Assert.Empty(cut.FindAll(".epos-zweispalten-fussleiste"));
        var fuss = cut.Find(".epos-zweispalten-satzkopf > .epos-zweispalten-satzknoepfe").TextContent;
        Assert.Contains("Löschen", fuss);
        Assert.Contains("Bearbeiten...", fuss);
        Assert.Contains(Resource.AUSWAHL_BTN_VERGLEICHEN, fuss);   // AW2: wie in den anderen Projektdialogen
        Assert.Single(cut.FindAll(".epos-zweispalten-satzknoepfe .epos-knopf--vergleichen"));
        Assert.Single(cut.FindAll(".epos-zweispalten-satzknoepfe .epos-knopf--neu"));
    }

    [Fact]
    public void Die_Detailzeile_zeigt_beim_Projektsatz_die_Kenndaten_der_Kopie_und_die_Marke()
    {
        var cut = Aufbauen();

        Assert.Equal(Satzmarke.Projektsatz, cut.FindComponent<Zweispaltenauswahl>().Instance.SatzArt);
        Assert.Empty(cut.FindAll(".epos-wp-anlagefelder"));   // UeS2: zu steht nur die Zusammenfassung
        var werte = Satz(cut).QuerySelectorAll(".epos-wp-abschnitt--kenndaten input").Select(i => i.GetAttribute("value")).ToList();
        Assert.Contains("Bosch", werte);
        Assert.Contains("WP Alpha", werte);
    }

    [Fact]
    public void Die_Detailzeile_zeigt_beim_Katalogsatz_dessen_Kenndaten_ohne_Kosten_und_Anlage()
    {
        var cut = Aufbauen(kosten: (_, _) => Task.CompletedTask);
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung
        Assert.Single(cut.FindAll(".epos-wp-satz-kostenzeile .epos-kostenleiste"));
        SatzZu(cut);

        KatalogzeileWaehlen(cut);
        Satz(cut);

        Assert.Null(cut.Instance.Gewaehlt);
        Assert.Equal(Satzmarke.Katalogsatz, cut.FindComponent<Zweispaltenauswahl>().Instance.SatzArt);
        Assert.Equal("Alpha", cut.Instance.SatzBearbeitung!.Firma);
        Assert.Single(cut.FindAll(".epos-wp-anlagefelder--katalog"));
        Assert.Empty(cut.FindAll(".epos-wp-satz-kostenzeile .epos-kostenleiste"));   // KA-E-12: Kosten nur beim Projektsatz
        Assert.Empty(cut.FindAll(".epos-knopf--anlage"));
    }

    [Fact]
    public void Die_Kostenknoepfe_wirken_auf_die_gewaehlte_Projektzeile()
    {
        var zeilen = new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") };
        var gerufen = new List<(WaermepumpeAnlageDaten, bool)>();
        var cut = Aufbauen(zeilen, kosten: (d, b) => { gerufen.Add((d, b)); return Task.CompletedTask; });

        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung
        cut.FindAll(".epos-kostenleiste button.epos-knopf")[0].Click();

        var (zeile, betrieb) = Assert.Single(gerufen);
        Assert.Same(zeilen[0], zeile);
        Assert.False(betrieb);
    }

    [Fact]
    public void Ohne_Zeile_steht_der_Leersatz()
    {
        var cut = Aufbauen(new List<WaermepumpeAnlageDaten>());

        Assert.False(cut.Instance.DetailOffen);
        // UeS2: ohne Satz nennt die Detailzeile den Leersatz, „Bearbeiten" ist gesperrt.
        Assert.Equal(Resource.AUSWAHL_SATZ_LEER, cut.Find(".epos-zweispalten-satzname").TextContent);
        Assert.Empty(cut.FindAll(".epos-zweispalten-bearbeiten"));   // AW1: kein Stift in der Detailzeile
        Assert.All(cut.FindAll(BEARBEITEN_AM_SATZ), k => Assert.True(k.HasAttribute("disabled")));
        Assert.True(Pfeil(cut, 1).HasAttribute("disabled"));
    }

    // =================================================================================
    // „Anlage…" - die Anlagenseite als Überlagerung (KA-E-11)
    // =================================================================================

    [Fact]
    public void Uebernehmen_haengt_die_Zeile_an_waehlt_sie_und_oeffnet_ihre_Anlage()
    {
        var zeilen = new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") };
        var uebernommen = new List<WaermepumpeAnlageDaten>();
        var cut = Aufbauen(zeilen, uebernehmen: d => uebernommen.Add(d));

        Assert.True(Pfeil(cut, 0).HasAttribute("disabled"));
        KatalogzeileWaehlen(cut);
        Pfeil(cut, 0).Click();

        Assert.Equal(2, zeilen.Count);
        Assert.Equal("WP Neu", zeilen[1].Bezeichner);
        Assert.Same(zeilen[1], cut.Instance.Gewaehlt);
        Assert.Same(zeilen[1], Assert.Single(uebernommen));
        Assert.True(cut.Instance.DetailOffen);                     // eine neue Zeile braucht ihre Anlagendaten
    }

    [Fact]
    public void Sammeluebernahme_legt_je_angekreuztem_Satz_eine_Zeile_an_ohne_Anlage_zu_oeffnen()
    {
        var zeilen = new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") };
        var cut = Aufbauen(zeilen, katalog: ZweiSaetze());

        KatalogAnkreuzen(cut, 0, 1);
        Pfeil(cut, 0).Click();

        Assert.Equal(new[] { "WP Alpha", "WP Neu", "WP Zwei" }, zeilen.Select(z => z.Bezeichner));
        Assert.False(cut.Instance.DetailOffen);
        Assert.Equal(0, cut.Instance.KatalogWahl.Anzahl);
    }

    [Fact]
    public void OK_haelt_bei_einer_neuen_ungeprueften_Zeile_an_und_oeffnet_ihre_Anlage()
    {
        var zeilen = new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") };
        bool? ergebnis = null;
        var cut = Aufbauen(zeilen, katalog: ZweiSaetze(), geschlossen: b => ergebnis = b);
        KatalogAnkreuzen(cut, 0, 1);
        Pfeil(cut, 0).Click();

        cut.FindAll(".epos-dialog-fuss button, .epos-leiste button").Last(b => b.TextContent.Trim() == "OK").Click();

        Assert.Null(ergebnis);
        Assert.True(cut.Instance.DetailOffen);
        Assert.Equal("WP Neu", cut.Instance.Gewaehlt!.Bezeichner);
        Assert.Contains("WP Neu", cut.Instance.Meldung);
    }

    [Fact]
    public void OK_meldet_true_wenn_alle_Zeilen_geprueft_sind()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        Knopf(cut, "OK").Click();

        Assert.True(ergebnis);
    }

    [Fact]
    public void Entfernen_trifft_die_ZEILE_und_nicht_den_Index()
    {
        var a = Zeile("WP Alpha");
        var b = Zeile("WP Beta");
        var zeilen = new List<WaermepumpeAnlageDaten> { a, b };
        var entfernt = new List<WaermepumpeAnlageDaten>();
        var cut = Aufbauen(zeilen, entfernen: d => entfernt.Add(d));

        Projektwahl(cut, 1).Click();
        Pfeil(cut, 1).Click();

        Assert.Same(a, Assert.Single(zeilen));
        Assert.Same(b, Assert.Single(entfernt));
        Assert.Same(a, cut.Instance.Gewaehlt);                     // die Wahl wandert auf die erste Zeile
    }

    [Fact]
    public void Entfernen_wirkt_auf_alle_angekreuzten_Zeilen()
    {
        var zeilen = new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha"), Zeile("WP Beta"), Zeile("WP Gamma") };
        var cut = Aufbauen(zeilen);

        ProjektAnkreuzen(cut, 0, 2);
        Pfeil(cut, 1).Click();

        Assert.Equal("WP Beta", Assert.Single(zeilen).Bezeichner);
    }

    [Fact]
    public void Umstellen_braucht_genau_eine_Zeile_und_genau_einen_Satz_und_legt_keine_Zeile_an()
    {
        var zeilen = new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") };
        var gerufen = new List<(WaermepumpeAnlageDaten, int)>();
        var cut = Aufbauen(zeilen, katalog: ZweiSaetze(),
                           umstellen: (d, id) => { gerufen.Add((d, id)); d.Bezeichner = "WP Zwei"; return true; });

        Assert.True(Knopf(cut, "Markierte auf diese Wärmepumpe umstellen").HasAttribute("disabled"));
        KatalogAnkreuzen(cut, 0, 1);
        Assert.True(Knopf(cut, "Markierte auf diese Wärmepumpe umstellen").HasAttribute("disabled"));

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("td .epos-kaestchenzelle input")[0].Change(false);
        var umstellen = Knopf(cut, "Markierte auf diese Wärmepumpe umstellen");
        Assert.False(umstellen.HasAttribute("disabled"));
        umstellen.Click();

        var (zeile, id) = Assert.Single(gerufen);
        Assert.Same(zeilen[0], zeile);
        Assert.Equal(2, id);
        Assert.Single(zeilen);
        Assert.Same(zeilen[0], cut.Instance.Gewaehlt);
        Assert.Contains("WP Zwei", cut.FindAll(".epos-raster")[0].TextContent);
    }

    // =================================================================================
    // Bearbeiten je Bereich (4.6, KA-E-8, KA-E-13)
    // =================================================================================

    /// <summary>
    /// Die Detailzeile nennt die GEWÄHLTE Projektzeile — Marke, Name und Kenndaten —, nie „Kein Satz gewählt";
    /// die Satzfläche zeigt die Kenndaten des Projektsatzes, keine Anlagenbausteine.
    /// </summary>
    [Fact]
    public void UeS1_Detailzeile_und_Satzflaeche_zeigen_die_gewaehlte_Projektzeile()
    {
        var a = Zeile("WP Alpha");
        var b = Zeile("WP Beta", 20);
        b.IdWp = 2;
        var cut = Aufbauen(new List<WaermepumpeAnlageDaten> { a, b });

        Projektwahl(cut, 1).Click();
        var zeile = cut.Find(".epos-zweispalten-satzzeile");
        Assert.Equal(Resource.AUSWAHL_MARKE_PROJEKTSATZ, zeile.QuerySelector(".epos-zweispalten-marke")!.TextContent);
        Assert.Equal("WP Beta", zeile.QuerySelector(".epos-zweispalten-satzname")!.TextContent);
        Assert.Contains("Bosch", zeile.QuerySelector(".epos-zweispalten-satzkenndaten")!.TextContent);
        Assert.DoesNotContain(Resource.AUSWAHL_SATZ_LEER, zeile.TextContent);

        // Ein Projektsatz ohne Bezeichner heisst nach seinem Typ - die Zeile ist gewaehlt.
        b.Bezeichner = "";
        b.Typ = "Typ B";
        Projektwahl(cut, 0).Click();
        Projektwahl(cut, 1).Click();
        Assert.Equal("Typ B", cut.Find(".epos-zweispalten-satzname").TextContent);

        var flaeche = Satz(cut);   // AW2: die Satz-Ueberlagerung traegt die Anlagenfelder der Zeile
        Assert.Single(flaeche.QuerySelectorAll(".epos-wp-anlagefelder"));
        Assert.Contains("Auslegung für Verteilung", flaeche.TextContent);
        Assert.Same(b, cut.Instance.Gewaehlt);
    }

    [Fact]
    public void Der_Rueckweg_fragt_zu_den_Projektkopien_der_Auswahl()
    {
        var gefragt = new List<IReadOnlyList<int>>();
        var vorschlag = new Rueckwegvorschlag(1, "WP Alpha", "WP Alpha", Rueckwegsperre.Keine, "WP Alpha (Projekt)");
        var cut = Aufbauen(rueckweg: Rueckweg(new[] { vorschlag }, gefragt));

        cut.Find(".epos-knopf--rueckweg").Click();

        Assert.Equal(new[] { 1 }, Assert.Single(gefragt));
        Assert.NotNull(cut.Instance.Rueckweg);
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
    }

    [Fact]
    public void Loeschen_fragt_und_loescht_den_Katalogsatz_nach_Id()
    {
        var geloescht = new List<int>();
        var cut = Aufbauen(katalog: ZweiSaetze(), katalogLoeschen: id => { geloescht.Add(id); return ""; });

        KatalogzeileWaehlen(cut, 1);
        cut.Find(".epos-knopf--loeschen").Click();
        Assert.Empty(geloescht);
        Knopf(cut, "Ja").Click();

        Assert.Equal(2, Assert.Single(geloescht));
    }

    // =================================================================================
    // Abschluss
    // =================================================================================

    [Fact]
    public void Abbrechen_und_Esc_melden_false()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);
        Knopf(cut, Resource.ALLG_BTN_ABBRECHEN).Click();
        Assert.False(ergebnis);

        bool? zweites = null;
        var cut2 = Aufbauen(geschlossen: b => zweites = b);
        cut2.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(zweites);
    }

    [Fact]
    public void Kreuz_meldet_false()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog-kopf .epos-dialog-zu").Click();

        Assert.False(ergebnis);
    }

    [Fact]
    public void Ohne_Titel_zeigt_der_Kopf_weder_Titel_noch_Kreuz()
    {
        var cut = Render<WaermepumpenDialog>(p => p
            .Add(x => x.Zeilen, new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") })
            .Add(x => x.Katalog, () => Katalog)
            .Add(x => x.Katalogprofil, Katalogprofil)
            .Add(x => x.TitelText, ""));

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
        Assert.NotEmpty(cut.FindAll(".epos-dialog-kopf"));
    }

    [Fact]
    public void Der_Erstfokus_liegt_auf_der_aeusseren_Wurzel()
    {
        var cut = Aufbauen();

        var wurzel = cut.Find(".epos-dialog").GetAttribute("blazor:elementReference");
        var fokus = Assert.Single(JSInterop.Invocations,
            a => a.Identifier == "Blazor._internal.domWrapper.focus");
        Assert.Equal(wurzel, ((Microsoft.AspNetCore.Components.ElementReference)fokus.Arguments[0]!).Id);
        Assert.Equal(true, fokus.Arguments[1]);
    }

    // =================================================================================
    // UeS2: Zusammenfassung der Detailzeile und Stift je Zeile (Anwenderentscheid 10.10.2026)
    // =================================================================================

    private static Dictionary<string, string> Angaben(IRenderedComponent<WaermepumpenDialog> cut)
        => cut.FindAll(".epos-satzzusammenfassung-angabe")
              .ToDictionary(a => a.QuerySelector("dt")!.TextContent, a => a.QuerySelector("dd")!.TextContent);

    [Fact]
    public void UeS2_Die_Zusammenfassung_des_Projektsatzes_nennt_die_Anlagendaten_und_ist_nur_Anzeige()
    {
        var zeile = Zeile("WP Alpha");
        zeile.Typ = "Sole-Wasser";
        zeile.Kuehlbetrieb = true;
        var cut = Aufbauen(new List<WaermepumpeAnlageDaten> { zeile }, kosten: (_, _) => Task.CompletedTask,
                           kostensumme: _ => (15000.0, 250.0));

        cut.Find(".epos-zweispalten-satzzeile").Click();

        var k = System.Globalization.CultureInfo.CurrentCulture;
        var angaben = Angaben(cut);
        Assert.Equal("12 kW", angaben[Resource.AUSWAHL_ZF_LEISTUNG]);
        Assert.Equal("Sole-Wasser", angaben[Resource.AUSWAHL_ZF_QUELLE]);
        Assert.Equal("35/28 °C", angaben[Resource.AUSWAHL_ZF_VORLAUF_RUECKLAUF]);
        Assert.Equal(DbWerte.WP_BETRIEBSART_PARALLEL, angaben[Resource.AUSWAHL_ZF_BETRIEBSART]);
        Assert.Equal(Resource.ALLG_BTN_JA, angaben[Resource.AUSWAHL_ZF_KUEHLBETRIEB]);
        Assert.Equal(string.Format(k, Resource.AUSWAHL_ZF_EURO, 15000.0), angaben[Resource.AUSWAHL_ZF_INVEST]);
        Assert.Equal(string.Format(k, Resource.AUSWAHL_ZF_EURO_JAHR, 250.0), angaben[Resource.AUSWAHL_ZF_BETRIEB]);
        // Nur Anzeige: Kenndaten, Kennlinien und Kostenknoepfe stehen in der Ueberlagerung.
        Assert.Empty(cut.FindAll(".epos-wp-satz"));
        Assert.Empty(cut.FindAll(".epos-kostenleiste"));
        Assert.Empty(cut.FindAll(".epos-satzzusammenfassung input, .epos-satzzusammenfassung button"));
    }

    [Fact]
    public void UeS2_Ohne_Kostenweg_keine_Kosten_und_Kuehlbetrieb_Nein()
    {
        var cut = Aufbauen();
        cut.Find(".epos-zweispalten-satzzeile").Click();

        var angaben = Angaben(cut);
        Assert.Equal(Resource.ALLG_BTN_NEIN, angaben[Resource.AUSWAHL_ZF_KUEHLBETRIEB]);
        Assert.False(angaben.ContainsKey(Resource.AUSWAHL_ZF_INVEST));
        Assert.False(angaben.ContainsKey(Resource.AUSWAHL_ZF_BETRIEB));
    }

    [Fact]
    public void UeS2_Die_Zusammenfassung_des_Katalogsatzes_nennt_Hersteller_Quelle_Leistung_und_VL_max()
    {
        var cut = Aufbauen();
        KatalogzeileWaehlen(cut);
        cut.Find(".epos-zweispalten-satzzeile").Click();

        var texte = cut.FindAll(".epos-satzzusammenfassung-angabe dd").Select(e => e.TextContent).ToList();
        Assert.Equal(4, texte.Count);
        Assert.Equal("Alpha", texte[0]);
        Assert.Equal("Sole-Wasser", texte[1]);
        Assert.EndsWith(" kW", texte[2]);
        Assert.StartsWith("60", texte[3]);
        Assert.EndsWith(" °C", texte[3]);
    }

    /// <summary>AW1: kein Wahlknopf, die Zeile ist die Wahl — Klick hebt sie hervor, die Kästchen bleiben unberührt.</summary>
    [Fact]
    public void AW1_Die_Zeile_ist_die_Wahl_und_wird_hervorgehoben()
    {
        var cut = Aufbauen(DreiPumpen());
        var liste = cut.FindAll(".epos-raster")[0];
        Assert.Empty(liste.QuerySelectorAll(".epos-zeilenwahl, .epos-anlagenwahl, input[type=radio]"));

        Projektwahl(cut, 2).Click();

        var zeilen = cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr").ToList();
        Assert.Single(zeilen, tr => tr.ClassList.Contains("epos-zeile--gewaehlt"));
        Assert.Contains("epos-zeile--gewaehlt", zeilen[2].ClassList);
        Assert.Equal("true", zeilen[2].QuerySelector(".epos-zeilenzelle")!.GetAttribute("aria-current"));
        Assert.Equal("WP C", cut.Find(".epos-zweispalten-satzname").TextContent);
        Assert.Equal(0, cut.Instance.ProjektWahl.Anzahl);
    }

    /// <summary>AW1: Die Projektliste ist ein Tabulatorhalt; ↑ ↓ Pos1 Ende bewegen die Wahl.</summary>
    [Fact]
    public void AW1_Die_Pfeiltasten_bewegen_die_Wahl()
    {
        var cut = Aufbauen(DreiPumpen());
        Projektwahl(cut, 0).Click();
        var huelle = cut.Find(".epos-zweispalten-bereich--projekt .epos-raster-huelle");
        Assert.Equal("0", huelle.GetAttribute("tabindex"));

        huelle.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal("WP B", cut.Find(".epos-zweispalten-satzname").TextContent);
        cut.Find(".epos-zweispalten-bereich--projekt .epos-raster-huelle").KeyDown(new KeyboardEventArgs { Key = "End" });
        Assert.Equal("WP C", cut.Find(".epos-zweispalten-satzname").TextContent);
        cut.Find(".epos-zweispalten-bereich--projekt .epos-raster-huelle").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        Assert.Equal("WP B", cut.Find(".epos-zweispalten-satzname").TextContent);
    }

    /// <summary>
    /// AW1: Beim Projektsatz sind Schloss… und Löschen weich gesperrt („Katalogsatz wählen"), Bearbeiten…
    /// öffnet die Projektkopie; beim Katalogsatz wirken sie wie bisher.
    /// </summary>
    [Fact]
    public void AW1_Sperren_folgen_der_Art_des_gewaehlten_Satzes()
    {
        var geloescht = new List<int>();
        var pruefung = new Schlosspruefung();
        var cut = Aufbauen(DreiPumpen(), schloss: pruefung.Weg(),
                           katalogLoeschen: id => { geloescht.Add(id); return ""; });
        Projektwahl(cut, 1).Click();

        foreach (string klasse in new[] { "epos-katalogschloss", "epos-knopf--loeschen" })
        {
            var knopf = AmSatz(cut, klasse);
            Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
            Assert.False(knopf.HasAttribute("disabled"));
            Assert.Equal(Resource.AUSWAHL_SPERRE_KATALOGSATZ, knopf.GetAttribute("title"));
        }
        AmSatz(cut, "epos-katalogschloss").Click();
        Assert.Equal(Resource.AUSWAHL_SPERRE_KATALOGSATZ, cut.Find(".epos-warnbanner-text").TextContent);
        Assert.Empty(pruefung.Aufrufe);
        Assert.Empty(geloescht);

        AmSatz(cut, "epos-knopf--bearbeiten-projekt").Click();
        Assert.True(cut.FindComponent<Zweispaltenauswahl>().Instance.SatzUeberlagerungOffen);
        Assert.Equal(Resource.AUSWAHL_MARKE_PROJEKTSATZ,
                     cut.Find(".epos-ueberlagerung--satz .epos-ueberlagerung-kopf .epos-zweispalten-marke--satz").TextContent);
        SatzZu(cut);

        KatalogzeileWaehlen(cut);
        foreach (string klasse in new[] { "epos-katalogschloss", "epos-knopf--loeschen" })
        {
            Assert.False(AmSatz(cut, klasse).HasAttribute("aria-disabled"));
            Assert.False(AmSatz(cut, klasse).HasAttribute("disabled"));
        }
        Assert.Empty(cut.FindAll(".epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-projekt"));
        Assert.Single(cut.FindAll(".epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-katalog"));
    }

    /// <summary>AW1: Der Stift einer Projektzeile tut, was „Bearbeiten…" am Satz für diese Zeile tut; der Stift der Detailzeile entfällt.</summary>
    [Fact]
    public void AW1_Der_Stift_je_Zeile_ist_Bearbeiten_fuer_diese_Zeile()
    {
        var cut = Aufbauen(DreiPumpen());
        Assert.Empty(cut.FindAll(".epos-zweispalten-satzkopf > .epos-zweispalten-bearbeiten"));
        Assert.Empty(cut.FindAll(".epos-zweispalten-bereich--projekt .epos-zweispalten-kopfleiste .epos-knopf--bearbeiten-projekt"));

        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-zeilenstift")[1].Click();

        Assert.True(cut.FindComponent<Zweispaltenauswahl>().Instance.SatzUeberlagerungOffen);
        Assert.Equal("WP B", cut.Find(".epos-ueberlagerung--satz .epos-ueberlagerung-titel").TextContent.Trim());
        Assert.Contains("epos-zeile--gewaehlt", cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr")[1].ClassList);
    }

    private const string FeldImKoerper = ".epos-satzueberlagerung-koerper .epos-modulparameter input[type=text]:not([readonly])";

    private static Rueckwegwege Rueckweg(IReadOnlyList<Rueckwegvorschlag> zeilen, List<IReadOnlyList<int>>? gefragt = null)
        => new()
        {
            Vorschau = ids => { gefragt?.Add(ids); return zeilen; },
            NameBelegt = _ => false,
            Uebernehmen = _ => new KatalogSpeicherErgebnis(true, "„WP Alpha“ übernommen", ""),
        };

    private static List<WaermepumpeAnlageDaten> DreiPumpen()
        => new() { Zeile("WP A"), Zeile("WP B", 14), Zeile("WP C", 16) };

    private static IElement AmSatz(IRenderedComponent<WaermepumpenDialog> cut, string klasse)
        => cut.Find(".epos-zweispalten-satzknoepfe ." + klasse);

    // =================================================================================
    // AW2: EINE Satz-Ueberlagerung - Satz und Anlage zusammengefuehrt, fuer Projekt- und Katalogsatz gleich
    // =================================================================================

    /// <summary>Die Abschnitte der Satz-Überlagerung in ihrer Folge.</summary>
    private static List<string> Abschnitte(IElement koerper)
        => koerper.QuerySelectorAll(".epos-wp-abschnitt")
                  .Select(a => a.ClassList.First(k => k.StartsWith("epos-wp-abschnitt--"))).ToList();

    private static readonly string[] ABSCHNITTE =
        { "epos-wp-abschnitt--auslegung", "epos-wp-abschnitt--kenndaten", "epos-wp-abschnitt--kennlinien" };

    [Fact]
    public void AW2_Projektsatz_und_Katalogsatz_haben_denselben_Aufbau()
    {
        var cut = Aufbauen(kosten: (_, _) => Task.CompletedTask, katalog: ZweiSaetze(),
                           katalogSpeichern: (_, _) => new KatalogSpeicherErgebnis(true, "", ""));

        var projekt = Satz(cut);
        Assert.Equal(ABSCHNITTE, Abschnitte(projekt));
        Assert.Single(projekt.QuerySelectorAll(".epos-wp-satz-kostenzeile .epos-kostenleiste"));
        Assert.Contains("Auslegung für Verteilung", projekt.TextContent);
        Assert.Contains("Wärmepumpen Kenndaten", projekt.TextContent);
        Assert.Empty(projekt.QuerySelectorAll(".epos-knopf--nurprojekt"));
        Assert.Equal(2, projekt.QuerySelectorAll(".epos-wp-abschnitt--kennlinien .epos-reiter-knopf").Length);
        SatzZu(cut);

        KatalogzeileWaehlen(cut);
        var katalog = Satz(cut);
        Assert.Equal(ABSCHNITTE, Abschnitte(katalog));
        Assert.Contains("Auslegung für Verteilung", katalog.TextContent);
        Assert.Contains("Wärmepumpen Kenndaten", katalog.TextContent);
        Assert.Equal(2, katalog.QuerySelectorAll(".epos-wp-abschnitt--kennlinien .epos-reiter-knopf").Length);

        // Was nur im Projekt Daten hat, steht an derselben Stelle weich gesperrt: drei Kostenknoepfe und Konfiguration…
        var gesperrt = katalog.QuerySelectorAll(".epos-knopf--nurprojekt");
        Assert.Equal(4, gesperrt.Length);
        Assert.All(gesperrt, k => Assert.Equal("true", k.GetAttribute("aria-disabled")));
        Assert.All(gesperrt, k => Assert.Equal(Resource.WPV_NUR_IM_PROJEKT, k.GetAttribute("title")));
        Assert.Contains(Resource.WPV_NUR_IM_PROJEKT, katalog.QuerySelector(".epos-wp-abschnitt--auslegung")!.TextContent);
        gesperrt[0].Click();
        Assert.Equal(Resource.WPV_NUR_IM_PROJEKT, cut.Instance.Meldung);
    }

    [Fact]
    public void AW2_Kein_Anlage_Knopf_keine_Auswahlliste_und_jedes_Feld_einmal()
    {
        var cut = Aufbauen(kosten: (_, _) => Task.CompletedTask);
        Assert.Empty(cut.FindAll(".epos-knopf--anlage"));
        var koerper = Satz(cut);
        Assert.Empty(cut.FindAll(".epos-knopf--anlage"));
        Assert.Empty(koerper.QuerySelectorAll(".epos-wp-auswahl, .epos-wp-auswahlliste"));
        Assert.DoesNotContain("Modul-Katalog", koerper.TextContent);
        Assert.Empty(koerper.QuerySelectorAll(".epos-modulparameter, .epos-stammblatt"));
        Assert.Contains("Kennliniendaten Ansicht/Bearbeiten", koerper.TextContent);
        Assert.Contains("Konfiguration…", koerper.TextContent);

        // Jede Beschriftung der Feldgruppe steht genau einmal.
        var texte = koerper.QuerySelectorAll(".epos-wp-abschnitt--kenndaten .epos-feld-text")
                           .Select(t => t.TextContent.Trim()).Where(t => t.Length > 0).ToList();
        Assert.NotEmpty(texte);
        Assert.Equal(texte.Count, texte.Distinct().Count());
    }

    [Fact]
    public void AW2_OK_uebernimmt_Anlagenfelder_aus_einem_Guss()
    {
        var zeile = Zeile("WP Alpha");
        zeile.Vorlauf = 55;
        zeile.Ruecklauf = 45;
        var uebernommen = new List<WaermepumpeAnlageDaten>();
        var cut = Aufbauen(new List<WaermepumpeAnlageDaten> { zeile }, uebernehmen: uebernommen.Add);

        var koerper = Satz(cut);
        koerper.QuerySelector(".epos-wp-abschnitt--auslegung input")!.Input("40");
        Assert.Equal(40, zeile.Ruecklauf);
        Ok(cut);

        Assert.False(cut.Instance.SatzUeberlagerungOffen);
        Assert.Single(uebernommen);
        Assert.Same(zeile, uebernommen[0]);
        Assert.Equal(40, zeile.Ruecklauf);
    }

    [Fact]
    public void AW2_Abbrechen_verwirft_alles_auch_die_Anlagenfelder()
    {
        var zeile = Zeile("WP Alpha");
        zeile.Vorlauf = 55;
        zeile.Ruecklauf = 45;
        var uebernommen = new List<WaermepumpeAnlageDaten>();
        var cut = Aufbauen(new List<WaermepumpeAnlageDaten> { zeile }, uebernehmen: uebernommen.Add);

        Satz(cut).QuerySelector(".epos-wp-abschnitt--auslegung input")!.Input("40");
        SatzZu(cut);

        Assert.False(cut.Instance.SatzUeberlagerungOffen);
        Assert.Empty(uebernommen);
        Assert.Equal(45, zeile.Ruecklauf);
    }

    [Fact]
    public void AW2_OK_mit_Mangel_haelt_die_Ueberlagerung_mit_dem_Grund_offen()
    {
        var uebernommen = new List<WaermepumpeAnlageDaten>();
        var cut = Aufbauen(mangel: "Rücklauf zu hoch", uebernehmen: uebernommen.Add);
        Satz(cut);
        Ok(cut);

        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Empty(uebernommen);
        Assert.Contains("Rücklauf zu hoch", cut.Find(".epos-satzueberlagerung-fuss").TextContent);
    }

    /// <summary>
    /// KT-7: Wirft der Übernahmeweg (Modell und Trägerzuordnung), nennt der Banner den Grund, das Protokoll hat einen
    /// Vermerk, und die Satz-Überlagerung bleibt mit den Eingaben offen; der Dialog schließt nicht.
    /// </summary>
    [Fact]
    public void KT7_wirft_die_Uebernahme_nennt_der_Banner_den_Grund_und_die_Ueberlagerung_bleibt_offen()
    {
        using var vermerke = new Ausnahmevermerke();
        bool? geschlossen = null;
        var zeilen = new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") };
        var cut = Aufbauen(zeilen, uebernehmen: _ => throw new InvalidOperationException("Modell gesperrt"),
                           geschlossen: ok => geschlossen = ok);

        Satz(cut);
        Ok(cut);

        Assert.Contains(cut.FindAll(".epos-warnbanner-text"), b => b.TextContent.Contains("Modell gesperrt"));
        Assert.True(vermerke.Enthaelt("Modell gesperrt"));
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.True(cut.Instance.DetailOffen);
        Assert.Null(geschlossen);
    }

    [Fact]
    public void KT7_ohne_Ausnahme_hat_die_Uebernahme_keinen_Vermerk()
    {
        using var vermerke = new Ausnahmevermerke();
        var cut = Aufbauen(uebernehmen: _ => { });

        Satz(cut);
        Ok(cut);

        Assert.False(cut.Instance.SatzUeberlagerungOffen);
        Assert.True(vermerke.Leer);
    }

    /// <summary>KT-7: Wirft das Lesen der Katalogkennlinien, öffnet der Editor nicht, das Band nennt den Grund.</summary>
    [Fact]
    public void KT7_wirft_das_Lesen_der_Katalogkennlinien_nennt_das_Band_den_Grund()
    {
        using var vermerke = new Ausnahmevermerke();
        var cut = Aufbauen(katalog: ZweiSaetze(),
                           katalogSpeichern: (_, _) => new KatalogSpeicherErgebnis(true, "", ""),
                           katalogKennlinien: _ => throw new InvalidOperationException("Kenndaten gesperrt"),
                           katalogKennlinienAbgleichen: (_, _) => true);
        KatalogzeileWaehlen(cut);
        Satz(cut).QuerySelector(".epos-wp-katalogkennlinien")!.Click();

        Assert.Contains(cut.FindAll(".epos-warnbanner-text"), b => b.TextContent.Contains("Kenndaten gesperrt"));
        Assert.True(vermerke.Enthaelt("Kenndaten gesperrt"));
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
    }

    /// <summary>KT-7: Wirft der Abgleich der Katalogkennlinien, nennt das Band den Grund; die Überlagerung bleibt.</summary>
    [Fact]
    public void KT7_wirft_der_Abgleich_der_Katalogkennlinien_nennt_das_Band_den_Grund()
    {
        using var vermerke = new Ausnahmevermerke();
        var cut = Aufbauen(katalog: ZweiSaetze(),
                           katalogSpeichern: (_, _) => new KatalogSpeicherErgebnis(true, "", ""),
                           katalogKennlinien: _ => new List<KennlinienZeile> { new() },
                           katalogKennlinienAbgleichen: (_, _) => throw new InvalidOperationException("Abgleich gesperrt"));
        KatalogzeileWaehlen(cut);
        Satz(cut).QuerySelector(".epos-wp-katalogkennlinien")!.Click();
        cut.FindAll(".epos-ueberlagerung").Last().QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == Resource.ALLG_BTN_OK).Click();

        Assert.Contains(cut.FindAll(".epos-warnbanner-text"), b => b.TextContent.Contains("Abgleich gesperrt"));
        Assert.True(vermerke.Enthaelt("Abgleich gesperrt"));
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
    }

    [Fact]
    public void AW2_Gesperrter_Katalogsatz_steht_nur_lesend_und_wird_nie_ueberschrieben()
    {
        int geschrieben = 0;
        var cut = Aufbauen(katalog: ZweiSaetze(1),
                           katalogSpeichern: (_, _) => { geschrieben++; return new KatalogSpeicherErgebnis(true, "", ""); });
        KatalogzeileWaehlen(cut);
        var koerper = Satz(cut);

        Assert.Empty(cut.FindAll(".epos-satzueberlagerung-ok"));
        Assert.Single(cut.FindAll(".epos-satzueberlagerung-schliessen"));
        Assert.Contains(Resource.ADM_SCHLOSS_ERST_AUFHEBEN, cut.Find(".epos-satzueberlagerung-fuss").TextContent);
        var felder = koerper.QuerySelectorAll(".epos-wp-abschnitt--kenndaten input");
        Assert.NotEmpty(felder);
        Assert.All(felder, f => Assert.True(f.HasAttribute("disabled") || f.HasAttribute("readonly")));
        SatzZu(cut);
        Assert.Equal(0, geschrieben);
    }

    [Fact]
    public void AW2_Ungesperrter_Katalogsatz_schreibt_beim_OK_den_Katalogsatz()
    {
        var geschrieben = new List<(WaermepumpeStammDaten Satz, bool Neu)>();
        var cut = Aufbauen(katalog: ZweiSaetze(),
                           katalogSpeichern: (s, neu) => { geschrieben.Add((s, neu)); return new KatalogSpeicherErgebnis(true, "gespeichert", s.Name); });
        KatalogzeileWaehlen(cut);
        var koerper = Satz(cut);
        var hersteller = koerper.QuerySelectorAll(".epos-wp-abschnitt--kenndaten input")
                                .First(i => i.GetAttribute("value") == "Alpha");
        hersteller.Input("Gamma");
        Ok(cut);

        Assert.False(cut.Instance.SatzUeberlagerungOffen);
        Assert.Single(geschrieben);
        Assert.Equal("Gamma", geschrieben[0].Satz.Firma);
        Assert.False(geschrieben[0].Neu);
    }

    [Fact]
    public void AW2_Abbrechen_schreibt_eine_im_Editor_geaenderte_Katalogkennlinie_zurueck()
    {
        var urstand = new List<KennlinienZeile> { new() };
        var abgeglichen = new List<IReadOnlyList<KennlinienZeile>>();
        var cut = Aufbauen(katalog: ZweiSaetze(),
                           katalogSpeichern: (_, _) => new KatalogSpeicherErgebnis(true, "", ""),
                           katalogKennlinien: _ => urstand,
                           katalogKennlinienAbgleichen: (_, z) => { abgeglichen.Add(z); return true; });
        KatalogzeileWaehlen(cut);
        Satz(cut).QuerySelector(".epos-wp-katalogkennlinien")!.Click();
        cut.FindAll(".epos-ueberlagerung").Last().QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == Resource.ALLG_BTN_OK).Click();
        Assert.Single(abgeglichen);              // der Editor schreibt bei seinem OK

        SatzZu(cut);                             // Abbrechen der Satz-Ueberlagerung nimmt es zurueck
        Assert.Equal(2, abgeglichen.Count);
        Assert.Equal(urstand.Count, abgeglichen[1].Count);
    }

    [Fact]
    public void AW2_Der_Stift_je_Zeile_ist_Bearbeiten()
    {
        var cut = Aufbauen(new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha"), Zeile("WP Beta", 20) });
        cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody .epos-zeilenstift")[1].Click();

        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.True(cut.Instance.DetailOffen);
        Assert.Equal("WP Beta", cut.Instance.Gewaehlt!.Bezeichner);
    }

    // =================================================================================
    // AW2-Nachtrag: Knopfleiste und Schlussleiste wie in den anderen Projektdialogen
    // =================================================================================

    [Fact]
    public void AW2_Die_Knopfleiste_am_Satz_traegt_Vergleichen_Schloss_Loeschen_Bearbeiten_und_Neu()
    {
        var cut = Aufbauen(DreiPumpen(), schloss: new Schlosspruefung().Weg(), katalogLoeschen: _ => "",
                           neuGaben: () => new Dictionary<string, object>());
        var leiste = cut.Find(".epos-zweispalten-satzknoepfe");
        string[] folge = leiste.QuerySelectorAll("button")
            .Select(b => b.ClassList.First(k => k is "epos-knopf--vergleichen" or "epos-katalogschloss" or "epos-knopf--loeschen"
                                                 or "epos-knopf--bearbeiten-projekt" or "epos-knopf--bearbeiten-katalog"
                                                 or "epos-knopf--neu")).ToArray();
        Assert.Equal(new[] { "epos-knopf--vergleichen", "epos-katalogschloss", "epos-knopf--loeschen",
                             "epos-knopf--bearbeiten-projekt", "epos-knopf--neu" }, folge);
    }

    [Fact]
    public void AW2_Vergleichen_wirkt_auf_die_angekreuzten_Zeilen_des_Bereichs_und_ist_unter_zwei_gesperrt()
    {
        var pumpen = DreiPumpen();
        for (int i = 0; i < pumpen.Count; i++) pumpen[i].IdWp = i + 1;
        var cut = Aufbauen(pumpen, katalog: ZweiSaetze());
        Assert.True(AmSatz(cut, "epos-knopf--vergleichen").HasAttribute("disabled"));

        // Projektsatz: zwei angekreuzte Projektzeilen verschiedener Geraete
        ProjektAnkreuzen(cut, 0, 1);
        Assert.False(AmSatz(cut, "epos-knopf--vergleichen").HasAttribute("disabled"));

        // Katalogsatz: die Zeile waehlt, das Ankreuzen waehlt nicht (Anwenderentscheid)
        KatalogzeileWaehlen(cut);
        Assert.Equal(Satzmarke.Katalogsatz, cut.FindComponent<Zweispaltenauswahl>().Instance.SatzArt);
        Assert.True(AmSatz(cut, "epos-knopf--vergleichen").HasAttribute("disabled"));
        KatalogAnkreuzen(cut, 0, 1);
        Assert.Equal(Satzmarke.Katalogsatz, cut.FindComponent<Zweispaltenauswahl>().Instance.SatzArt);
        Assert.False(AmSatz(cut, "epos-knopf--vergleichen").HasAttribute("disabled"));
        AmSatz(cut, "epos-knopf--vergleichen").Click();
        Assert.NotEmpty(cut.FindAll(".epos-vergleich-name"));
    }

    [Fact]
    public void AW2_Neu_oeffnet_den_Katalogeditor_im_Modus_Neu_auch_beim_Projektsatz()
    {
        int gerufen = 0;
        var cut = Aufbauen(neuGaben: () => { gerufen++; return new Dictionary<string, object>(); });
        Assert.Equal(Satzmarke.Projektsatz, cut.FindComponent<Zweispaltenauswahl>().Instance.SatzArt);
        AmSatz(cut, "epos-knopf--neu").Click();
        Assert.Equal(1, gerufen);
        Assert.True(cut.Instance.NeuOffen);
    }

    [Fact]
    public void AW2_Die_Schlussleiste_ist_die_der_anderen_Projektdialoge()
    {
        var cut = Render<WaermepumpenDialog>(p => p
            .Add(x => x.Zeilen, new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") })
            .Add(x => x.Katalog, () => Katalog));
        var fuss = cut.FindComponents<SpeichernLeiste>();
        Assert.Single(fuss);
        var texte = fuss[0].FindAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Contains(Resource.ALLG_BTN_ABBRECHEN, texte);
        Assert.Contains(Resource.ALLG_BTN_OK, texte);
        Assert.DoesNotContain(texte, t => t.Contains('❌') || t.Contains('✕'));
    }
}
