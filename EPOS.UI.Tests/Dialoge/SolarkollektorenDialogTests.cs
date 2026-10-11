using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Solarthermie;
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
/// Eingabe der Solarkollektoren (iU9-W7.7). Soll ist die Feldkarte von
/// <c>Form_SolarKollektoren</c>: zwei Listen mit den beiden Pfeilen, der Modulblock
/// mit sechs Anzeigefeldern und die Gruppe „Kollektor" mit sechs Bedienelementen.
/// </summary>
public class SolarkollektorenDialogTests : EposBunitContext
{

    /// <summary>
    /// Der Filterstand DIESES Prüfstands. Ohne ihn nähme der Dialog den aus dem
    /// <c>Katalogfilterregister</c> — der lebt prozessweit, und xunit fährt
    /// Testklassen nebeneinander. Dass das Register wirklich teilt, prüft
    /// <c>KatalogfilterstandTests</c>.
    /// </summary>
    private readonly Katalogfilterstand _filterstand = new();
    /// <summary>
    /// Das PROFIL des Projektdialogs (W14a-E-10 / S2.1): dieselben sechs Spalten wie
    /// in der Verwaltung, dazu die siebte „im Projekt verwendet" (Q12).
    /// </summary>
    private static readonly Katalogfilterprofil Profil =
        Katalogfilterprofil.MitVerwendung(Anlagenart.Solarkollektoren,
            s => Resource.ResourceManager.GetString(s) ?? s);

    private static IReadOnlyList<Katalogfilterzeile> Katalogzeilen() => new[]
    {
        new Katalogfilterzeile(11, "Vitosol 200")
            .MitText(Katalogfilterprofil.SpBezeichner, "Vitosol 200")
            .MitText(Katalogfilterprofil.SpHersteller, "Viessmann")
            .MitText(Katalogfilterprofil.SpKollektortyp, "Flach")
            .MitZahl(Katalogfilterprofil.SpApertur, 2.31, 2)
            .MitZahl(Katalogfilterprofil.SpEtaNull, 0.8, 3)
            .MitZahl(Katalogfilterprofil.SpK1, 3.5, 2),

        new Katalogfilterzeile(12, "Vitosol 300")
            .MitText(Katalogfilterprofil.SpBezeichner, "Vitosol 300")
            .MitText(Katalogfilterprofil.SpHersteller, "Viessmann")
            .MitText(Katalogfilterprofil.SpKollektortyp, "Röhre")
            .MitZahl(Katalogfilterprofil.SpApertur, 3.0, 2)
            .MitZahl(Katalogfilterprofil.SpEtaNull, 0.64, 3)
            .MitZahl(Katalogfilterprofil.SpK1, 1.0, 2),
    };

    public SolarkollektorenDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static ErzeugerZeile Zeile(int schluessel, string name, int geraetId = 11) => new()
    {
        Schluessel = schluessel,
        Bezeichner = name,
        GeraetId = geraetId,
        AnzahlModule = 4,
        Neigung = 30,
        Azimut = 0,
        Vorlauf = 60,
        Ruecklauf = 40
    };

    private static ErzeugerDetail Detail(string name) => new(
        name, "",
        new[] { ("Kollektor:", "Flach"), ("Hersteller :", "Viessmann"),
                ("Beschreibung :", "Flachkollektor"), ("Aperturfläche:", "2,31") });

    /// <summary>
    /// Die Felder des Aufklappers „Alle Daten anzeigen" — der Feldsatz des
    /// Kollektorprofils in Kurzform: der Bezeichner nur lesbar, die übrigen editierbar.
    /// </summary>
    private static List<BrowserFeldwert> Katalogfelder(string name) => new()
    {
        new BrowserFeldwert
        {
            Schluessel = KatalogBrowserProfil.FeldBezeichner, Bezeichnung = "Name:",
            Art = BrowserFeldArt.Text, Editierbar = false, Wert = name
        },
        new BrowserFeldwert
        {
            Schluessel = KatalogBrowserProfil.FeldKollektortyp, Bezeichnung = "Kollektor:",
            Art = BrowserFeldArt.Text, Editierbar = true, Wert = "Flach"
        },
        new BrowserFeldwert
        {
            Schluessel = KatalogBrowserProfil.FeldInvestitionskosten,
            Bezeichnung = "Investitionskosten:", Einheit = "€",
            Art = BrowserFeldArt.Zahl, Editierbar = true, Wert = "850"
        }
    };

    /// <summary>
    /// UeS2: Das Fragment des Satzes steht nur in der Satz-Überlagerung — „Bearbeiten" der
    /// Detailzeile öffnet sie (falls zu); zurück kommt ihr Körper.
    /// </summary>
    /// <summary>„Bearbeiten…" der Knopfleiste am gewählten Satz (AW1), je nach Satzart.</summary>
    private const string BEARBEITEN_AM_SATZ =
        ".epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-projekt, .epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-katalog";

    private static AngleSharp.Dom.IElement Satz(IRenderedComponent<SolarkollektorenDialog> cut)
    {
        if (cut.FindAll(".epos-satzueberlagerung-koerper").Count == 0)
            cut.Find(BEARBEITEN_AM_SATZ).Click();
        return cut.Find(".epos-satzueberlagerung-koerper");
    }

    /// <summary>UeS2: Das OK der Satz-Überlagerung — speichert und übernimmt, wie früher „Speichern"/„Übernehmen".</summary>
    private static void Ok(IRenderedComponent<SolarkollektorenDialog> cut)
        => cut.Find(".epos-satzueberlagerung-ok").Click();

    /// <summary>UeS2: Schließt eine offene Satz-Überlagerung wie Abbrechen bzw. Schließen.</summary>
    private static void SatzZu(IRenderedComponent<SolarkollektorenDialog> cut)
    {
        var knopf = cut.FindAll(".epos-satzueberlagerung-abbrechen, .epos-satzueberlagerung-schliessen");
        if (knopf.Count > 0) knopf[0].Click();
    }

    private IRenderedComponent<SolarkollektorenDialog> Aufbauen(
        List<ErzeugerZeile>? zeilen = null,
        Func<int, AufnahmeErgebnis>? aufnehmen = null,
        Action<ErzeugerZeile>? entfernen = null,
        Action<ErzeugerZeile>? uebernehmen = null,
        Func<string, bool, IReadOnlyDictionary<string, object>>? editorGaben = null,
        Func<int, string>? katalogLoeschen = null,
        Func<string, IReadOnlyList<BrowserFeldwert>?>? katalogfelder = null,
        Func<string, IReadOnlyList<BrowserFeldwert>, KatalogSpeicherErgebnis>? felderSpeichern = null,
        Func<ErzeugerZeile?, bool, Task>? kostenOeffnen = null,
        bool wizard = false,
        Action<bool>? geschlossen = null,
        Satzbearbeitungswege? projektsatzWege = null,
        Satzbearbeitungswege? katalogsatzWege = null,
        Rueckwegwege? rueckwegWege = null,
        Func<IReadOnlyList<Katalogfilterzeile>>? katalog = null,
        Schlossweg? schloss = null)
        => Render<SolarkollektorenDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<ErzeugerZeile> { Zeile(1, "Vitosol 200") })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, katalog ?? Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Detail, Detail)
            .Add(x => x.Modulflaeche, _ => 2.5)
            .Add(x => x.ProjektsatzWege, projektsatzWege)
            .Add(x => x.KatalogsatzWege, katalogsatzWege)
            .Add(x => x.RueckwegWege, rueckwegWege)
            .Add(x => x.Aufnehmen, aufnehmen ?? (_ => new AufnahmeErgebnis(Zeile(9, "Vitosol 300", 12))))
            .Add(x => x.Entfernen, entfernen)
            .Add(x => x.Uebernehmen, uebernehmen)
            .Add(x => x.EditorGaben, editorGaben)
            .Add(x => x.KatalogLoeschen, katalogLoeschen ?? (_ => ""))
            .Add(x => x.Katalogfelder, katalogfelder)
            .Add(x => x.KatalogfelderSpeichern, felderSpeichern)
            .Add(x => x.KostenOeffnen, kostenOeffnen)
            .Add(x => x.Wizard, wizard)
            .Add(x => x.Schloss, schloss)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IElement Knopf(IRenderedComponent<SolarkollektorenDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Der_Feldbestand_der_Karte_steht()
    {
        var cut = Aufbauen(editorGaben: (n, b) => new Dictionary<string, object>());

        Assert.Equal(2, cut.FindAll(".epos-raster").Count);
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-knopf--richtung").Count);

        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung
        // Die Felder der Anlage stehen in EINER Gruppe beim Projektsatz; den Modulblock gibt es nicht mehr.
        var gruppen = cut.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Kollektor" }, gruppen);

        var knoepfe = cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Contains("Bearbeiten...", knoepfe);
        Assert.DoesNotContain("Kollektor in DB ändern...", knoepfe);
        Assert.Contains("Neu…", knoepfe);
        Assert.Contains("Löschen", knoepfe);
        Assert.Contains(Resource.ALLG_BTN_OK, knoepfe);   // UeS2: das OK der Satz-Ueberlagerung uebernimmt
        Assert.Contains("Vergleichen…", knoepfe);
    }

    /// <summary>
    /// <b>Katalogauswahl V1, Stufe 3 (4.2, 4.9): die Knöpfe an ihrem Ort.</b> D: Kontext im Dialogkopf; P: Summe
    /// Module, Bearbeiten… und Rückweg nur mit ihren Wegen; K-Fuss: Vergleichen, Schloss, Löschen, Bearbeiten, rechts
    /// Neu….
    /// </summary>
    [Fact]
    public void S3a_Die_Knoepfe_stehen_an_ihrem_Ort()
    {
        var cut = Aufbauen(projektsatzWege: Wege(), katalogsatzWege: Wege(), rueckwegWege: Rueckweg(Array.Empty<Rueckwegvorschlag>()),
                           editorGaben: (n, b) => new Dictionary<string, object>());

        Assert.Equal("Eingabe der Solarkollektoren", cut.Find(".epos-dialog-kopf > .epos-dialog-kontext").TextContent);
        Assert.Empty(cut.FindAll(".epos-kontextzeile"));

        var projekt = cut.Find(".epos-zweispalten-bereich--projekt > .epos-zweispalten-kopfleiste");
        Assert.Contains("4", projekt.QuerySelector(".epos-zweispalten-summe")!.TextContent);
        Assert.Null(projekt.QuerySelector(".epos-knopf--bearbeiten-projekt"));   // AW1: nur noch am Satz
        Assert.NotNull(projekt.QuerySelector(".epos-knopf--rueckweg"));

        // AW1: keine Fussleiste unter dem Katalog - die Knoepfe stehen in dieser Folge am gewaehlten Satz.
        Assert.Empty(cut.FindAll(".epos-zweispalten-fussleiste"));
        Assert.Empty(cut.FindAll(".epos-zweispalten-satzkopf > .epos-zweispalten-bearbeiten"));
        var fuss = cut.Find(".epos-zweispalten-satzkopf > .epos-zweispalten-satzknoepfe").TextContent;
        int vergleichen = fuss.IndexOf("Vergleichen", StringComparison.Ordinal);
        int loeschen = fuss.IndexOf("Löschen", StringComparison.Ordinal);
        int bearbeiten = fuss.IndexOf("Bearbeiten", StringComparison.Ordinal);
        int neu = fuss.IndexOf("Neu…", StringComparison.Ordinal);
        Assert.True(vergleichen >= 0 && vergleichen < loeschen && loeschen < bearbeiten && bearbeiten < neu, fuss);
    }

    [Fact]
    public void S3a_Ohne_Wege_fehlen_Bearbeiten_und_Rueckweg_im_Projekt()
    {
        var cut = Aufbauen(wizard: true);
        // AW1: „Bearbeiten…" steht nur noch am Satz (ohne Weg zeigt es den Projektsatz in der Ueberlagerung).
        Assert.Empty(cut.FindAll(".epos-zweispalten-kopfleiste .epos-knopf--bearbeiten-projekt"));
        Assert.Empty(cut.FindAll(".epos-knopf--rueckweg"));
    }

    [Fact]
    public void S3a_Die_Summe_zaehlt_die_Module_und_folgt_dem_Uebernehmen()
    {
        var a = Zeile(1, "Vitosol 200");
        var b = Zeile(2, "Vitosol 300", 12);
        b.AnzahlModule = 6;
        var cut = Aufbauen(new List<ErzeugerZeile> { a, b });

        Assert.Equal("10", cut.Instance.Summe);
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung
        cut.FindAll(".epos-gruppenkopf-koerper")[0].QuerySelectorAll("input")[0].Input("8");
        Ok(cut);   // UeS2: das OK der Satz-Ueberlagerung uebernimmt
        Assert.Equal("14", cut.Instance.Summe);
        Assert.Contains("14", cut.Find(".epos-zweispalten-summe").TextContent);
    }

    [Fact]
    public void S3a_Die_Kostenknoepfe_stehen_nur_beim_Projektsatz()
    {
        var cut = Aufbauen(kostenOeffnen: (_, _) => Task.CompletedTask);
        Assert.Empty(cut.FindAll(".epos-kostenleiste button"));   // UeS2: zu steht nur die Zusammenfassung
        Satz(cut);
        Assert.Equal(2, cut.FindAll(".epos-kostenleiste button").Count);
        SatzZu(cut);

        KatalogZeileWaehlen(cut, 0);
        Satz(cut);
        Assert.Empty(cut.FindAll(".epos-kostenleiste button"));
    }

    [Fact]
    public void S3a_Alle_Daten_zeigt_beim_Projektsatz_die_Projektkopie_ueber_die_Geraete_ID()
    {
        var gelesen = new List<int>();
        var cut = Aufbauen(projektsatzWege: new Satzbearbeitungswege
        {
            Lesen = id => { gelesen.Add(id); return Katalogfelder("Kopie"); },
            Speichern = _ => new KatalogSpeicherErgebnis(true, "ok", "")
        });

        Assert.Equal(11, gelesen.Single());
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung
        Assert.Single(cut.FindAll(".epos-modulparameter-knopf"));
        Assert.Equal(3, cut.Find(".epos-modulparameter").QuerySelectorAll(".epos-feld").Length);
    }

    [Fact]
    public void S3a_Speichern_der_Projektkopie_geht_an_die_Projektsatzwege()
    {
        var gespeichert = new List<(int Id, IReadOnlyList<BrowserFeldwert> Felder)>();
        var cut = Aufbauen(projektsatzWege: Wege(gespeichert));

        Satz(cut);   // UeS2: „Alle Daten" stehen in der Satz-Ueberlagerung, ihr OK speichert
        cut.Find(".epos-modulparameter").QuerySelectorAll("input:not([readonly])")[0].Input("Röhre");
        Ok(cut);

        Assert.Equal(11, gespeichert.Single().Id);
    }

    [Fact]
    public void S3a_Die_Projektzeile_liest_ihr_Detail_ueber_die_Zeile()
    {
        var gefragt = new List<int>();
        var cut = Render<SolarkollektorenDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Vitosol 200", 77) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Detail, _ => throw new InvalidOperationException("Katalog statt Kopie"))
            .Add(x => x.ProjektDetail, z => { gefragt.Add(z.GeraetId); return Detail("Kopie"); })
            .Add(x => x.ProjektModulflaeche, _ => 3.0));

        Assert.Equal(77, gefragt.Single());
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung
        Assert.Equal("12", cut.FindAll(".epos-gruppenkopf-koerper")[0].QuerySelectorAll("input")[1].GetAttribute("value"));
    }

    [Fact]
    public void Die_Kenndaten_sind_reine_Anzeige()
    {
        var cut = Aufbauen();
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung
        var raster = cut.FindAll(".epos-formularraster")[0];

        // Name plus die vier Detailfelder.
        Assert.Equal(5, raster.QuerySelectorAll("input[readonly]").Length);
        Assert.Empty(raster.QuerySelectorAll("input:not([readonly])"));
    }

    [Fact]
    public void Die_Kollektorgruppe_traegt_vier_Bedienelemente()
    {
        var cut = Aufbauen();
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung
        var kollektor = cut.FindAll(".epos-gruppenkopf-koerper")[0];

        // Anzahl, Neigung, Azimut plus die gerechnete Flaeche - Vor- und Ruecklauf fuehrt
        // die Gruppe nicht, sie haetten beim Kollektor keinen Rechenweg. Dazu der Solarkreis
        // (Welle M2): Pumpe, Verluste, Graedigkeit, Spreizung und die Wahl der Arbeitstemperatur;
        // dazu die Albedo (PV4).
        Assert.Equal(8, kollektor.QuerySelectorAll("input:not([readonly])").Length);
        Assert.Single(kollektor.QuerySelectorAll("select"));
        Assert.DoesNotContain("Vorlauf", kollektor.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Rücklauf", kollektor.TextContent, StringComparison.Ordinal);
        Assert.Single(kollektor.QuerySelectorAll("input[readonly]"));
        // UeS2: die Anlagenfelder uebernimmt das OK der Satz-Ueberlagerung - kein eigener Knopf.
        Assert.Empty(kollektor.QuerySelectorAll("button.epos-knopf--uebernehmen-anlage"));
    }

    /// <summary>Die Maske ist lokalisiert (22 englische Texte, W7.9).</summary>
    [Fact]
    public void Die_englischen_Texte_lassen_sich_setzen()
    {
        var cut = Render<SolarkollektorenDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Vitosol 200") })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Detail, Detail)
            .Add(x => x.TitelText, "Entering the solar panels")
            .Add(x => x.LabelAnzahl, "Modules:"));

        Assert.Equal("Entering the solar panels", cut.Find(".epos-dialog-titel").TextContent);
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung
        Assert.Contains("Modules:", cut.FindAll(".epos-feld-text").Select(e => e.TextContent));
    }

    [Fact]
    public void Im_Assistenten_fehlt_die_OK_Leiste()
    {
        var cut = Aufbauen(wizard: true);
        Assert.Empty(cut.FindAll(".epos-status"));
    }

    // =================================================================================
    // Die Kollektorgruppe erscheint nur bei einer Projektzeile
    // =================================================================================

    [Fact]
    public void Eine_Katalogzeile_zeigt_das_Detail_OHNE_Kollektorgruppe()
    {
        // dataGridView1_Click:289 blendete groupBox_Kollektor aus.
        var cut = Aufbauen();
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung
        Assert.Single(cut.FindAll(".epos-gruppenkopf-titel"));
        SatzZu(cut);

        KatalogZeileWaehlen(cut, 0);
        Satz(cut);

        Assert.Empty(cut.FindAll(".epos-gruppenkopf-titel"));
        Assert.Equal("Vitosol 200", cut.FindAll(".epos-formularraster")[0].QuerySelector("input")!.GetAttribute("value"));
    }

    [Fact]
    public void Eine_Projektzeile_fuellt_die_Kollektorgruppe()
    {
        var cut = Aufbauen();
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung
        var werte = cut.FindAll(".epos-gruppenkopf-koerper")[0]
                       .QuerySelectorAll("input").Select(e => e.GetAttribute("value")).ToList();

        // Reihenfolge: Anzahl, Aperturflaeche (gerechnet), Neigung, Azimut, Albedo (leer = 0,2),
        // dann der Solarkreis (Pumpe, Verluste, Graedigkeit, Spreizung) - leer = Vorgabe.
        Assert.Equal(9, werte.Count);
        Assert.Equal("4", werte[0]);
        Assert.Equal("10", werte[1]);            // 2,5 m² x 4
        Assert.Equal("30", werte[2]);
        Assert.Equal("0", werte[3]);
        Assert.All(werte.Skip(4), w => Assert.True(string.IsNullOrEmpty(w)));
    }

    /// <summary>
    /// <b>Die Aperturfläche steht gerundet da</b> — höchstens zwei Nachkommastellen, in der
    /// Kultur des Anwenders. 2,51 m² × 3 ist als Gleitkommazahl 7,529999999999999; ohne
    /// Rundung stand genau das im Feld.
    /// </summary>
    [Theory]
    [InlineData("de-DE", "7,53")]
    [InlineData("en-US", "7.53")]
    public void Die_Aperturflaeche_steht_gerundet_in_der_Kultur_des_Anwenders(string kultur, string erwartet)
    {
        using var _ = new Kulturvorrichtung(kultur);
        var zeile = Zeile(1, "Vitosol 200");
        zeile.AnzahlModule = 3;

        var cut = Render<SolarkollektorenDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { zeile })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Detail, Detail)
            .Add(x => x.Modulflaeche, _ => 2.51));

        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung
        Assert.Equal(erwartet, cut.FindAll(".epos-gruppenkopf-koerper")[0]
                                  .QuerySelectorAll("input")[1].GetAttribute("value"));
    }

    [Fact]
    public void Die_Aperturflaeche_folgt_der_Modulanzahl_live()
    {
        // textBox_Anzahl_TextChanged:367 rechnete bei jedem Tastendruck nach.
        var cut = Aufbauen();
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung
        var kollektor = cut.FindAll(".epos-gruppenkopf-koerper")[0];

        kollektor.QuerySelectorAll("input")[0].Input("6");

        Assert.Equal("15", cut.FindAll(".epos-gruppenkopf-koerper")[0]
                              .QuerySelectorAll("input")[1].GetAttribute("value"));
    }

    // =================================================================================
    // Aufnehmen, Entfernen, Uebernehmen
    // =================================================================================

    [Fact]
    public void Der_linke_Pfeil_ist_ohne_Katalogwahl_gesperrt()
    {
        var cut = Aufbauen();
        var pfeile = new[] { cut.Find(".epos-zweispalten-knopf--uebernehmen"), cut.Find(".epos-zweispalten-knopf--entfernen") };

        Assert.True(pfeile[0].HasAttribute("disabled"));    // ◀ ohne Katalogwahl
        Assert.False(pfeile[1].HasAttribute("disabled"));   // ▶ mit Projektzeile
    }

    [Fact]
    public void Der_linke_Pfeil_legt_eine_Zeile_an_und_waehlt_sie()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Vitosol 200") };
        int gerufen = 0;
        var cut = Aufbauen(zeilen, aufnehmen: id =>
        {
            gerufen = id;
            return new AufnahmeErgebnis(Zeile(9, "Vitosol 300", 12));
        });

        KatalogZeileWaehlen(cut, 1);  // Katalogzeile 2
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();

        Assert.Equal(12, gerufen);
        Assert.Equal(2, zeilen.Count);
        Assert.Equal("Vitosol 300", cut.Instance.Projektzeile!.Bezeichner);
    }

    [Fact]
    public void Eine_Ablehnung_beim_Aufnehmen_meldet_und_legt_nichts_an()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Vitosol 200") };
        var cut = Aufbauen(zeilen, aufnehmen: _ =>
            new AufnahmeErgebnis(null, "Der Datensatz konnte nicht in das Projekt übernommen werden.", true));

        KatalogZeileWaehlen(cut, 0);
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();

        Assert.Single(zeilen);
        Assert.Contains("nicht in das Projekt", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Der_rechte_Pfeil_entfernt_genau_die_gewaehlte_Zeile()
    {
        var a = Zeile(1, "Vitosol 200");
        var b = Zeile(2, "Vitosol 300", 12);
        var zeilen = new List<ErzeugerZeile> { a, b };

        var entfernt = new List<ErzeugerZeile>();
        var cut = Aufbauen(zeilen, entfernen: z => entfernt.Add(z));

        cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr")[1]
           .QuerySelector("button")!.Click();
        cut.FindAll(".epos-zweispalten-knopf--entfernen")[0].Click();

        Assert.Single(zeilen);
        Assert.Same(a, zeilen[0]);
        Assert.Same(b, entfernt.Single());
    }

    [Fact]
    public void Uebernehmen_schreibt_die_drei_Ganzzahlen_und_meldet()
    {
        var zeile = Zeile(1, "Vitosol 200");
        var uebernommen = new List<ErzeugerZeile>();
        var cut = Aufbauen(new List<ErzeugerZeile> { zeile }, uebernehmen: z => uebernommen.Add(z));

        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung
        var kollektor = cut.FindAll(".epos-gruppenkopf-koerper")[0];
        kollektor.QuerySelectorAll("input")[0].Input("6");     // Anzahl
        kollektor.QuerySelectorAll("input")[2].Input("35");    // Neigung
        kollektor.QuerySelectorAll("input")[3].Input("15");    // Azimut
        Ok(cut);   // UeS2: das OK der Satz-Ueberlagerung uebernimmt

        Assert.Equal(6, zeile.AnzahlModule);
        Assert.Equal(35, zeile.Neigung);
        Assert.Equal(15, zeile.Azimut);
        // Vor- und Ruecklauf der Zeile fasst der Dialog nicht an.
        Assert.Equal(60, zeile.Vorlauf);
        Assert.Equal(40, zeile.Ruecklauf);
        Assert.Same(zeile, uebernommen.Single());

        // A-24: Der 500-ms-Bildblitz mit Thread.Sleep wird ein Hinweis.
        Assert.Contains("übernommen", cut.Find(".epos-warnbanner").TextContent);
    }

    /// <summary>
    /// KT-7: Wirft der Übernahmeweg beim OK der Satz-Überlagerung, nennt der Banner den Grund, das
    /// Protokoll hat einen Vermerk, die Überlagerung und der Dialog bleiben offen.
    /// </summary>
    [Fact]
    public void KT7_wirft_die_Uebernahme_nennt_der_Banner_den_Grund_und_der_Dialog_bleibt_offen()
    {
        using var vermerke = new Ausnahmevermerke();
        bool? geschlossen = null;
        var zeile = Zeile(1, "Vitosol 200");
        var cut = Aufbauen(new List<ErzeugerZeile> { zeile },
                           uebernehmen: _ => throw new InvalidOperationException("Modell gesperrt"),
                           geschlossen: ok => geschlossen = ok);

        Satz(cut);
        cut.FindAll(".epos-gruppenkopf-koerper")[0].QuerySelectorAll("input")[0].Input("6");
        Ok(cut);

        Assert.Contains("Modell gesperrt", cut.Markup);
        Assert.True(vermerke.Enthaelt("Modell gesperrt"));
        Assert.DoesNotContain("übernommen", string.Join("\n", cut.FindAll(".epos-warnbanner-text").Select(b => b.TextContent)));
        Assert.NotEmpty(cut.FindAll(".epos-satzueberlagerung-koerper"));
        Assert.Null(geschlossen);
    }

    [Fact]
    public void KT7_ohne_Ausnahme_hat_die_Uebernahme_keinen_Vermerk()
    {
        using var vermerke = new Ausnahmevermerke();
        var zeile = Zeile(1, "Vitosol 200");
        var cut = Aufbauen(new List<ErzeugerZeile> { zeile }, uebernehmen: _ => { });

        Satz(cut);
        cut.FindAll(".epos-gruppenkopf-koerper")[0].QuerySelectorAll("input")[0].Input("6");
        Ok(cut);

        Assert.DoesNotContain("wurde nicht gespeichert", cut.Markup);
        Assert.True(vermerke.Leer);
    }

    /// <summary>
    /// Der Solarkreis (Welle M2): „Übernehmen" trägt Pumpe, Verluste, Arbeitstemperatur aus dem
    /// Speicher, Grädigkeit und Spreizung in die Zeile; Grädigkeit und Spreizung sind nur mit der
    /// Arbeitstemperatur aus dem Speicher frei.
    /// </summary>
    [Fact]
    public void Uebernehmen_schreibt_den_Solarkreis()
    {
        var zeile = Zeile(1, "Vitosol 200");
        var cut = Aufbauen(new List<ErzeugerZeile> { zeile });

        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung
        var kollektor = cut.FindAll(".epos-gruppenkopf-koerper")[0];
        // input[4] ist die Albedo (PV4), dahinter der Solarkreis.
        Assert.True(kollektor.QuerySelectorAll("input")[7].HasAttribute("disabled"));   // Grädigkeit gesperrt
        kollektor.QuerySelectorAll("input")[5].Input("75");     // Pumpe
        kollektor.QuerySelectorAll("input")[6].Input("6");      // Verluste
        cut.FindAll(".epos-gruppenkopf-koerper")[0].QuerySelector("select")!.Change("1");

        kollektor = cut.FindAll(".epos-gruppenkopf-koerper")[0];
        Assert.False(kollektor.QuerySelectorAll("input")[7].HasAttribute("disabled"));
        kollektor.QuerySelectorAll("input")[7].Input("10");     // Grädigkeit
        Ok(cut);   // UeS2: das OK der Satz-Ueberlagerung uebernimmt

        Assert.Equal(75.0, zeile.SolarPumpenleistungW);
        Assert.Equal(6.0, zeile.SolarkreisverlusteProzent);
        Assert.True(zeile.SolarArbeitstemperaturAusSpeicher);
        Assert.Equal(10.0, zeile.SolarGraedigkeitK);
        Assert.Null(zeile.SolarSpreizungK);
    }

    [Fact]
    public void Ein_leeres_Ganzzahlfeld_gilt_als_Null()
    {
        // Program.GanzzahlPruefen(..., leerErlaubt: true) im Vorlaeufer.
        var zeile = Zeile(1, "Vitosol 200");
        var cut = Aufbauen(new List<ErzeugerZeile> { zeile });

        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung
        cut.FindAll(".epos-gruppenkopf-koerper")[0].QuerySelectorAll("input")[3].Input("");
        Ok(cut);   // UeS2: das OK der Satz-Ueberlagerung uebernimmt

        Assert.Equal(0, zeile.Azimut);
    }

    // =================================================================================
    // Katalogpflege
    // =================================================================================

    [Fact]
    public void Katalog_aendern_und_loeschen_sind_ohne_Katalogwahl_gesperrt()
    {
        // AW1: ohne Projektzeile ist kein Satz gewaehlt - beim Projektsatz waere Loeschen weich gesperrt.
        var cut = Aufbauen(new List<ErzeugerZeile>(), editorGaben: (n, b) => new Dictionary<string, object>());

        Assert.True(Knopf(cut, "Bearbeiten...").HasAttribute("disabled"));
        Assert.True(Knopf(cut, "Löschen").HasAttribute("disabled"));
        Assert.False(Knopf(cut, "Neu…").HasAttribute("disabled"));
    }

    [Fact]
    public void Kollektor_aendern_zeigt_den_Editor_in_der_Ueberlagerung()
    {
        string? name = null;
        bool? neu = null;
        var cut = Aufbauen(editorGaben: (n, b) =>
        {
            name = n; neu = b;
            return new Dictionary<string, object> { ["Daten"] = new SolarkollektorKatalogDaten { Name = n } };
        });

        KatalogZeileWaehlen(cut, 0);
        Knopf(cut, "Bearbeiten...").Click();

        Assert.True(cut.Instance.EditorOffen);
        Assert.Equal("Vitosol 200", name);
        Assert.False(neu);
    }

    /// <summary>
    /// Die Katalogeditor-Ueberlagerung ist seit dem Anwenderentscheid 15.09.2026
    /// ("das Kreuz steht beim Titel") nicht mehr Schliessbar="false": Ihr eigenes
    /// Kreuz verwirft den Editor - derselbe Weg wie ihr <c>Geschlossen</c>
    /// (<c>_editorGaben = null</c>).
    /// </summary>
    [Fact]
    public void Ueberlagerungskreuz_verwirft_den_Katalogeditor()
    {
        var cut = Aufbauen(editorGaben: (n, b) =>
            new Dictionary<string, object> { ["Daten"] = new SolarkollektorKatalogDaten { Name = n } });

        KatalogZeileWaehlen(cut, 0);
        Knopf(cut, "Bearbeiten...").Click();
        Assert.True(cut.Instance.EditorOffen);

        cut.Find(".epos-ueberlagerung-zu").Click();
        Assert.False(cut.Instance.EditorOffen);
    }

    /// <summary>
    /// Ein Titel, eine Stelle (Befund „Doppeltes Kreuz dürfen nicht sein!"): Die
    /// Ueberlagerung trägt Titel UND Kreuz, der eingebettete Katalogeditor keins von
    /// beiden — <c>TitelText=""</c> steht RECHTS vom Parametersatz der Hülle und gilt
    /// deshalb auch dann, wenn dieser einen Titel mitbrächte.
    /// </summary>
    [Fact]
    public void Die_Ueberlagerung_Katalogeditor_zeigt_nur_ein_Kreuz()
    {
        var cut = Aufbauen(editorGaben: (n, b) =>
            new Dictionary<string, object> { ["Daten"] = new SolarkollektorKatalogDaten { Name = n } });

        KatalogZeileWaehlen(cut, 0);
        Knopf(cut, "Bearbeiten...").Click();

        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
    }

    [Fact]
    public void Kollektor_neu_fragt_erst_den_Namen()
    {
        string? name = null;
        bool? neu = null;
        var cut = Aufbauen(editorGaben: (n, b) =>
        {
            name = n; neu = b;
            return new Dictionary<string, object> { ["Daten"] = new SolarkollektorKatalogDaten { Name = n } };
        });

        Knopf(cut, "Neu…").Click();
        Assert.False(cut.Instance.EditorOffen);

        cut.Find(".epos-ueberlagerung input").Input("Neuer Kollektor");
        cut.Find(".epos-ueberlagerung").QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == "OK").Click();

        Assert.True(cut.Instance.EditorOffen);
        Assert.Equal("Neuer Kollektor", name);
        Assert.True(neu);
    }

    [Fact]
    public void Kollektor_loeschen_fragt_nach()
    {
        var geloescht = new List<int>();
        var cut = Aufbauen(katalogLoeschen: id => { geloescht.Add(id); return ""; });

        KatalogZeileWaehlen(cut, 0);
        Knopf(cut, "Löschen").Click();

        Assert.Single(cut.FindAll(".epos-rueckfrage"));
        Assert.Empty(geloescht);

        cut.Find(".epos-rueckfrage").QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == "Ja").Click();

        Assert.Equal(11, geloescht.Single());
    }

    // =================================================================================
    // Abschluss und Tastatur
    // =================================================================================

    [Fact]
    public void OK_und_Abbrechen_melden_das_Ergebnis()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);
        Knopf(cut, "OK").Click();
        Assert.True(ergebnis);

        ergebnis = null;
        var cut2 = Aufbauen(geschlossen: b => ergebnis = b);
        Knopf(cut2, "Abbrechen").Click();
        Assert.False(ergebnis);
    }

    [Fact]
    public void Esc_schliesst_nur_ohne_offene_Rueckfrage()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        KatalogZeileWaehlen(cut, 0);
        Knopf(cut, "Löschen").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(ergebnis);
    }

    /// <summary>Das Schliesskreuz im Kopf wirkt wie Abbrechen/Esc: schliesst mit false.</summary>
    [Fact]
    public void Kreuz_schliesst_mit_false()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog-zu").Click();
        Assert.False(ergebnis);
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

    // =================================================================================
    //  Stufe S2.1 / S2.3 / S2.5 - Anwenderentscheid W14a-E-10 vom 07.09.2026
    // =================================================================================

    /// <summary>Die Katalogliste - das UNTERE der beiden Raster.</summary>
    private static IReadOnlyList<AngleSharp.Dom.IElement> Katalogzeilen(
        IRenderedComponent<SolarkollektorenDialog> cut)
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

        _filterstand.Setzen(Katalogfilterprofil.SpKollektortyp, "Röhre");
        cut.Render();

        Assert.Single(Katalogzeilen(cut));
        Assert.Equal("1 von 2 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);
        Assert.Contains("Vitosol 300", Katalogzeilen(cut)[0].TextContent);

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

        _filterstand.Sortieren(Katalogfilterprofil.SpApertur);
        cut.Render();
        Assert.Contains("Vitosol 200", Katalogzeilen(cut)[0].TextContent);

        _filterstand.Sortieren(Katalogfilterprofil.SpApertur);
        cut.Render();
        Assert.False(_filterstand.Aufsteigend);
        Assert.Contains("Vitosol 300", Katalogzeilen(cut)[0].TextContent);

        _filterstand.Sortieren(Katalogfilterprofil.SpApertur);
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
        _filterstand.Setzen(Katalogfilterprofil.SpKollektortyp, "Röhre");
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
        var cut = Aufbauen(zeilen: new List<ErzeugerZeile> { Zeile(1, "Vitosol 200") });

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
        Assert.Contains("Vitosol 200", traegt.TextContent);
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
        _filterstand.Setzen(Katalogfilterprofil.SpKollektortyp, "Röhre");
        ersterAufbau.Render();
        Assert.Single(Katalogzeilen(ersterAufbau));

        // Der Dialog geht zu und wieder auf - derselbe Stand, dieselbe Sicht.
        var zweiterAufbau = Aufbauen();

        Assert.Single(Katalogzeilen(zweiterAufbau));
        Assert.Equal("1 von 2 Sätzen", zweiterAufbau.Find(".epos-katalog-treffer").TextContent);
        Assert.Single(zweiterAufbau.FindAll(".epos-katalog-ruecksetzer"));
    }

    // =================================================================================
    // Der Aufklapper „Alle Daten anzeigen" (Anwenderentscheid 15.09.2026)
    // =================================================================================

    [Fact]
    public void Der_Aufklapper_steht_nur_mit_Weg()
    {
        var ohne = Aufbauen();
        KatalogZeileWaehlen(ohne, 0);
        Satz(ohne);   // UeS2: das Fragment steht in der Satz-Ueberlagerung
        Assert.Empty(ohne.FindAll(".epos-modulparameter-knopf"));

        var mit = Aufbauen(katalogfelder: Katalogfelder);
        Satz(mit);
        Assert.Empty(mit.FindAll(".epos-modulparameter-knopf"));   // erste Projektzeile, ohne Projektsatzwege
        SatzZu(mit);

        KatalogZeileWaehlen(mit, 0);
        Satz(mit);
        Assert.Single(mit.FindAll(".epos-modulparameter-knopf"));
    }

    /// <summary>
    /// <b>Geholt wird mit der WAHL des Satzes</b> (Anwenderentscheid 16.09.2026: „Unter
    /// Bearbeiten sollen alle Parameter angezeigt werden und bearbeitbar sein"). Der
    /// Aufklapper steht offen, sobald ein Katalogsatz gewählt ist — genau EINE Abfrage
    /// je Satz; ohne gewählten Satz gibt es den Block nicht.
    /// </summary>
    [Fact]
    public void Die_Felder_kommen_mit_der_Wahl_des_Satzes()
    {
        int rufe = 0;
        var cut = Aufbauen(katalogfelder: n => { rufe++; return Katalogfelder(n); });

        Assert.Equal(0, rufe);
        Assert.Empty(cut.FindAll(".epos-modulparameter"));

        KatalogZeileWaehlen(cut, 0);

        Assert.Equal(1, rufe);
        Assert.True(cut.Instance.ParameterOffen);
        Satz(cut);   // UeS2: „Alle Daten" stehen in der Satz-Ueberlagerung
        Assert.Equal(1, rufe);   // der offene Aufklapper liest nicht erneut
        Assert.Equal("true", cut.Find(".epos-modulparameter-knopf").GetAttribute("aria-expanded"));
        Assert.Equal(3, cut.Find(".epos-modulparameter").QuerySelectorAll(".epos-feld").Length);
    }

    /// <summary>
    /// <b>Zuklappen geht weiterhin</b> — der Knopf bleibt, was er war, nur seine Vorgabe
    /// hat sich gedreht.
    /// </summary>
    [Fact]
    public void Der_Aufklapper_laesst_sich_weiterhin_zuklappen()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder);

        KatalogZeileWaehlen(cut, 0);
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung

        cut.Find(".epos-modulparameter-knopf").Click();

        Assert.False(cut.Instance.ParameterOffen);
        Assert.Equal("false", cut.Find(".epos-modulparameter-knopf").GetAttribute("aria-expanded"));
        Assert.Empty(cut.Find(".epos-modulparameter").QuerySelectorAll(".epos-feld"));

        cut.Find(".epos-modulparameter-knopf").Click();

        Assert.True(cut.Instance.ParameterOffen);
        Assert.NotEmpty(cut.Find(".epos-modulparameter").QuerySelectorAll(".epos-feld"));
    }

    [Fact]
    public void Die_Knopfzeile_steht_oben_in_der_Satzflaeche()
    {
        var cut = Aufbauen(kostenOeffnen: (_, _) => Task.CompletedTask);

        Assert.Empty(cut.FindAll(".epos-zweispalten-satz .epos-leiste"));   // UeS2: zu steht nur die Zusammenfassung
        var leiste = Satz(cut).QuerySelector(".epos-leiste")!;
        var teile = leiste.Children.ToList();
        Assert.Contains("epos-kostenleiste", teile[0].ClassList);
        Assert.Contains("epos-leiste-fueller", teile[1].ClassList);
        Assert.Contains("epos-berechnungshilfe", teile[2].ClassList);
    }

    /// <summary>
    /// „Speichern" reicht die GEÄNDERTEN Felder an den Schreibweg — und ist vorher
    /// gesperrt: Ohne Änderung gibt es nichts zu schreiben.
    /// </summary>
    [Fact]
    public void Speichern_reicht_die_geaenderten_Felder_an_den_Schreibweg()
    {
        IReadOnlyList<BrowserFeldwert>? gesehen = null;
        string? name = null;
        var cut = Aufbauen(katalogfelder: Katalogfelder,
                           felderSpeichern: (n, f) =>
                           {
                               name = n; gesehen = f;
                               return new KatalogSpeicherErgebnis(true, "Datensatz gespeichert", n);
                           });

        KatalogZeileWaehlen(cut, 0);
        Satz(cut);   // UeS2: „Alle Daten" stehen in der Satz-Ueberlagerung, ihr OK speichert

        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Speichern");
        cut.Find(".epos-modulparameter").QuerySelectorAll("input[inputmode=decimal]")[0].Input("900");
        Assert.False(cut.Find(".epos-satzueberlagerung-ok").HasAttribute("disabled"));
        Ok(cut);

        Assert.Equal("Vitosol 200", name);
        Assert.NotNull(gesehen);
        Assert.Equal("900",
            gesehen!.First(f => f.Schluessel == KatalogBrowserProfil.FeldInvestitionskosten).Wert);
        // Der Erfolg schliesst die Ueberlagerung - kein Band im Dialogkopf.
        Assert.Empty(cut.FindAll(".epos-warnbanner"));
        Assert.False(cut.Instance.SatzUeberlagerungOffen);
    }

    /// <summary>
    /// „Speichern" des Assistenten nach einer Feldänderung geht den Weg des Knopfes — und
    /// der Assistent erhält die Meldung des Katalogs, keinen leeren Text: Das Band ist nach
    /// dem Erfolg leer, die Rückmeldung steht nur am Knopf.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task Der_Assistent_erhaelt_nach_dem_Speichern_die_Katalogmeldung()
    {
        int schreibvorgaenge = 0;
        var cut = Aufbauen(katalogfelder: Katalogfelder,
                           felderSpeichern: (n, _) =>
                           {
                               schreibvorgaenge++;
                               return new KatalogSpeicherErgebnis(true, "Datensatz gespeichert", n);
                           });

        KatalogZeileWaehlen(cut, 0);
        Satz(cut);   // UeS2: „Alle Daten" stehen in der Satz-Ueberlagerung
        cut.Find(".epos-modulparameter").QuerySelectorAll("input[inputmode=decimal]")[0].Input("900");

        KiKern.KiErgebnis ergebnis =
            await KiMaskenbruecke.Haken(KiMaskennamen.SOLARKOLLEKTOREN_PROJEKT).Speichern!();

        Assert.True(ergebnis.Erfolg, ergebnis.Text);
        Assert.Equal("Datensatz gespeichert", ergebnis.Text);
        Assert.Equal(1, schreibvorgaenge);
    }

    /// <summary>
    /// <b>Der Vermerk am Knopf:</b> Ohne Änderung ist „Speichern" weich gesperrt, ein Klick
    /// nennt den Grund statt zu schreiben, und die nächste Eingabe nimmt den Vermerk zurück.
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

        KatalogZeileWaehlen(cut, 0);
        Satz(cut);   // UeS2: „Alle Daten" stehen in der Satz-Ueberlagerung, ihr OK speichert
        cut.Find(".epos-modulparameter").QuerySelectorAll("input[inputmode=decimal]")[0].Input("900");
        Ok(cut);
        Assert.Equal(1, schreibvorgaenge);
        Assert.False(cut.Instance.SatzUeberlagerungOffen);

        // Wieder geoeffnet und ohne Aenderung bestaetigt: kein zweiter Schreibvorgang.
        Satz(cut);
        Ok(cut);
        Assert.Equal(1, schreibvorgaenge);

        // Die naechste Eingabe schreibt wieder.
        Satz(cut);
        cut.Find(".epos-modulparameter").QuerySelectorAll("input[inputmode=decimal]")[0].Input("901");
        Ok(cut);
        Assert.Equal(2, schreibvorgaenge);
    }

    /// <summary>
    /// Ohne Schreibweg ist der Aufklapper reine ANZEIGE: kein Speichern-Knopf, kein
    /// beschreibbares Feld. Das ist die Lage des Katalogs, keine Entscheidung des
    /// Dialogs — er liest sie am Delegaten ab.
    /// </summary>
    [Fact]
    public void Ohne_Schreibweg_zeigt_der_Aufklapper_nur_an()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder);

        KatalogZeileWaehlen(cut, 0);
        Satz(cut);   // UeS2: „Alle Daten" stehen in der Satz-Ueberlagerung

        var block = cut.Find(".epos-modulparameter");
        Assert.DoesNotContain(block.QuerySelectorAll("button").Select(b => b.TextContent.Trim()),
                              t => t == "Speichern");
        Assert.Empty(block.QuerySelectorAll("input:not([readonly])"));
    }

    /// <summary>
    /// Eine Fehleingabe in einem Zahlenfeld sperrt „Speichern" — der Dialog schriebe
    /// sonst den Stand VOR der angefangenen Zahl zurück.
    /// </summary>
    [Fact]
    public void Eine_Fehleingabe_sperrt_den_Speichern_Knopf()
    {
        bool geschrieben = false;
        var cut = Aufbauen(katalogfelder: Katalogfelder,
                           felderSpeichern: (n, _) =>
                           {
                               geschrieben = true;
                               return new KatalogSpeicherErgebnis(true, "ok", n);
                           });

        KatalogZeileWaehlen(cut, 0);
        Satz(cut);   // UeS2: „Alle Daten" stehen in der Satz-Ueberlagerung, ihr OK speichert

        // Erst eine gueltige Aenderung - das OK ist frei.
        cut.Find(".epos-modulparameter").QuerySelectorAll("input[inputmode=decimal]")[0].Input("900");
        Assert.False(cut.Find(".epos-satzueberlagerung-ok").HasAttribute("disabled"));

        // Dann die Fehleingabe: das OK geht zu.
        cut.Find(".epos-modulparameter").QuerySelectorAll("input[inputmode=decimal]")[0].Input("neunhundert");

        Assert.True(cut.Find(".epos-satzueberlagerung-ok").HasAttribute("disabled"));
        Assert.False(geschrieben);
    }

    /// <summary>
    /// Ein abgelehnter Schreibweg (Schreibschutz der Auslieferung) meldet den Grund und
    /// lässt den geänderten Stand stehen — der Anwender soll ihn verbessern können.
    /// </summary>
    [Fact]
    public void Eine_Ablehnung_meldet_den_Grund()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder,
                           felderSpeichern: (_, __) =>
                               new KatalogSpeicherErgebnis(false, "Schreibgeschützt.", ""));

        KatalogZeileWaehlen(cut, 0);
        Satz(cut);   // UeS2: „Alle Daten" stehen in der Satz-Ueberlagerung, ihr OK speichert
        cut.Find(".epos-modulparameter").QuerySelectorAll("input[inputmode=decimal]")[0].Input("900");
        Ok(cut);

        Assert.Contains("Schreibgeschützt.", cut.Find(".epos-warnbanner").TextContent);
        Assert.True(cut.Instance.ParameterOffen);

        // Der Grund steht auch im Fuss der Ueberlagerung, sie bleibt offen.
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Equal("Schreibgeschützt.", cut.Find(".epos-satzueberlagerung-fuss [role=alert]").TextContent.Trim());
    }

    // =================================================================================
    // Die Kostenknöpfe im Modulbereich
    // =================================================================================

    /// <summary>
    /// <b>Zwei Knöpfe, kein dritter</b> (Anwenderentscheid 15.09.2026): Investitions-
    /// und Betriebskosten führen in DIESELBE Maske und unterscheiden sich nur im
    /// Schalter; „Energiekosten…" gibt es nicht — die Sonne ist kein gekaufter Träger.
    /// Ohne Delegat fehlt die Leiste ganz, im Assistenten ebenso.
    /// </summary>
    [Fact]
    public void Die_Kostenleiste_steht_nur_mit_Weg_und_traegt_zwei_Knoepfe()
    {
        var ohne = Aufbauen();
        Satz(ohne);   // UeS2: das Fragment steht in der Satz-Ueberlagerung
        Assert.Empty(ohne.FindAll(".epos-kostenleiste button"));

        var gerufen = new List<bool>();
        Func<ErzeugerZeile?, bool, Task> weg = (_, betrieb) =>
        {
            gerufen.Add(betrieb);
            return Task.CompletedTask;
        };

        var assistent = Aufbauen(kostenOeffnen: weg, wizard: true);
        Satz(assistent);
        Assert.Empty(assistent.FindAll(".epos-kostenleiste button"));

        var cut = Aufbauen(kostenOeffnen: weg);
        Satz(cut);
        Assert.Equal(2, cut.FindAll(".epos-kostenleiste button").Count);

        cut.FindAll(".epos-kostenleiste button")[0].Click();
        cut.FindAll(".epos-kostenleiste button")[1].Click();
        Assert.Equal(new[] { false, true }, gerufen);
    }

    /// <summary>Die Kostenleiste bekommt die GEWÄHLTE Projektzeile mit.</summary>
    [Fact]
    public void Die_Kostenleiste_reicht_die_gewaehlte_Projektzeile_durch()
    {
        ErzeugerZeile? gesehen = null;
        var cut = Aufbauen(kostenOeffnen: (zeile, _) =>
        {
            gesehen = zeile;
            return Task.CompletedTask;
        });

        Satz(cut);   // UeS2: die Kostenknoepfe stehen in der Satz-Ueberlagerung
        cut.FindAll(".epos-kostenleiste button")[0].Click();

        Assert.NotNull(gesehen);
        Assert.Equal(1, gesehen!.Schluessel);
    }

    /// <summary>Wählt die Katalogzeile mit dieser Nummer in der rechten Liste.</summary>
    private static void KatalogZeileWaehlen(IRenderedComponent<SolarkollektorenDialog> cut, int nr)
        => cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[nr].Click();

    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F1)
    // =====================================================================

    /// <summary>
    /// Nach dem Zeichnen steht die Maske an der <c>KiMaskenbruecke</c>; die Brücke
    /// liest die Neigung aus dem ARBEITSSTAND der Kollektorgruppe und setzt sie — und
    /// der neue Wert steht danach im Eingabefeld, nicht erst in der Zeile.
    /// </summary>
    /// <remarks>
    /// <b>Der Arbeitsstand ist das Daten-Objekt, und das ist der Punkt.</b> Die Zeile
    /// bekommt die fünf Zahlen erst mit „Übernehmen"; eine Setzung in die Zeile bliebe
    /// auf der Maske unsichtbar und würde vom nächsten „Übernehmen" überschrieben.
    /// </remarks>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_setzt_die_Neigung()
    {
        ErzeugerZeile zeile = Zeile(1, "Vitosol 200");
        var cut = Aufbauen(zeilen: new List<ErzeugerZeile> { zeile });
        Satz(cut);   // UeS2: die Anlagenfelder stehen in der Satz-Ueberlagerung, ihr OK uebernimmt

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.SOLARKOLLEKTOREN_PROJEKT));

        KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.SOLARKOLLEKTOREN_PROJEKT, "neigung");
        Assert.NotNull(zugang);
        Assert.Equal(30, zugang.Lesen());

        Assert.True(zugang.Setzbar);
        zugang.Setzen(35);

        // Der Arbeitsstand trägt den neuen Wert - die Zeile noch nicht.
        Assert.Equal(35, zugang.Lesen());
        Assert.Equal(30, zeile.Neigung);

        // Das OK der Satz-Ueberlagerung traegt ihn hinueber - derselbe Weg („Übernehmen"), den der Speicherhaken geht.
        Ok(cut);
        Assert.Equal(35, zeile.Neigung);
    }

    // =================================================================================
    // Senken der Anlage (Anwenderentscheid 23.09.2026)
    // =================================================================================

    /// <summary>
    /// Die PROJEKTzeile zeigt ihre Senken in der Gruppe „Kollektor" — bei der
    /// Solarthermie entscheidet die Senke, ob das Kollektorfeld überhaupt einen Abnehmer
    /// findet.
    /// </summary>
    [Fact]
    public void Die_Projektzeile_zeigt_ihre_Senken()
    {
        ErzeugerZeile zeile = Zeile(1, "Vitosol 200");
        zeile.Senken = "Senken: Heizkreis (Heizung + Warmwasser); Prozesswärme";
        var cut = Aufbauen(new List<ErzeugerZeile> { zeile });

        cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr")[0]
           .QuerySelector("button")!.Click();
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung

        Assert.Contains(cut.FindAll(".epos-formularraster .epos-herleitung-text"),
                        e => e.TextContent == zeile.Senken);
    }

    /// <summary>Ohne Senkentext steht keine Zeile.</summary>
    [Fact]
    public void Ohne_Senkentext_steht_keine_Senkenzeile()
    {
        var cut = Aufbauen();

        cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr")[0]
           .QuerySelector("button")!.Click();

        Assert.DoesNotContain(cut.FindAll(".epos-herleitung-text"),
                              e => e.TextContent.StartsWith("Senken", StringComparison.Ordinal));
    }

    // =================================================================================
    // Bodenalbedo (Entscheidungsvorlage Modellgrenzen, PV4) - nur das Feld
    // =================================================================================

    /// <summary>
    /// Die Kollektorgruppe trägt die Albedo samt Auswahlhilfe; „Übernehmen" schreibt einen Wert
    /// von 0 bis 1 in die Zeile, einen Wert außerhalb als leer (= 0,2).
    /// </summary>
    [Fact]
    public void Uebernehmen_schreibt_die_Albedo()
    {
        var zeile = Zeile(1, "Vitosol 200");
        var cut = Aufbauen(new List<ErzeugerZeile> { zeile }, uebernehmen: _ => { });

        Satz(cut);   // UeS2: die Anlagenfelder stehen in der Satz-Ueberlagerung, ihr OK uebernimmt
        var kollektor = cut.FindAll(".epos-gruppenkopf-koerper")[0];
        Assert.Contains("Schnee", kollektor.TextContent, StringComparison.Ordinal);
        kollektor.QuerySelectorAll("input")[4].Input("0.6");
        Ok(cut);   // UeS2: das OK der Satz-Ueberlagerung uebernimmt
        Assert.Equal(0.6, zeile.Albedo);

        // Außerhalb 0 … 1 färbt das Feld und wird nicht übernommen; leer heißt 0,2.
        Satz(cut);
        cut.FindAll(".epos-gruppenkopf-koerper")[0].QuerySelectorAll("input")[4].Input("1.5");
        Ok(cut);   // UeS2: das OK der Satz-Ueberlagerung uebernimmt
        Assert.Equal(0.6, zeile.Albedo);
        Satz(cut);
        cut.FindAll(".epos-gruppenkopf-koerper")[0].QuerySelectorAll("input")[4].Input("");
        Ok(cut);   // UeS2: das OK der Satz-Ueberlagerung uebernimmt
        Assert.Null(zeile.Albedo);
    }

    // =================================================================================
    // Katalogauswahl V1, Stufe 3: Mehrfachwahl, Bearbeiten je Bereich, Rückweg
    // =================================================================================

    private static Satzbearbeitungswege Wege(List<(int Id, IReadOnlyList<BrowserFeldwert> Felder)>? gespeichert = null)
        => new()
        {
            Lesen = _ => Katalogfelder("Satz"),
            Speichern = l => { gespeichert?.AddRange(l); return new KatalogSpeicherErgebnis(true, "ok", ""); }
        };

    private static Rueckwegwege Rueckweg(IReadOnlyList<Rueckwegvorschlag> zeilen, List<Rueckwegwahl>? geschrieben = null,
                                         List<IReadOnlyList<int>>? gefragt = null)
        => new()
        {
            Vorschau = ids => { gefragt?.Add(ids); return zeilen; },
            NameBelegt = _ => false,
            Uebernehmen = w => { geschrieben?.AddRange(w); return new KatalogSpeicherErgebnis(true, "1 Satz übernommen", ""); },
        };

    private static IReadOnlyList<Katalogfilterzeile> MitSchloss(params int[] gesperrt)
    {
        var zeilen = Katalogzeilen();
        foreach (var z in zeilen) z.Geschuetzt = gesperrt.Contains(z.Id);
        return zeilen;
    }

    private static void KatalogAnkreuzen(IRenderedComponent<SolarkollektorenDialog> cut, params int[] zeilen)
    {
        foreach (int i in zeilen)
            cut.FindAll(".epos-raster")[1].QuerySelectorAll("td .epos-kaestchenzelle input")[i].Change(true);
    }

    private static void ProjektAnkreuzen(IRenderedComponent<SolarkollektorenDialog> cut, params int[] zeilen)
    {
        foreach (int i in zeilen)
            cut.FindAll(".epos-raster")[0].QuerySelectorAll("td .epos-wahlkaestchen")[i].Click();
    }

    [Fact]
    public void S3a_Sammeluebernahme_legt_je_angekreuztem_Satz_eine_Zeile_an()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Vitosol 200") };
        var gerufen = new List<int>();
        int n = 50;
        var cut = Aufbauen(zeilen, aufnehmen: id =>
        {
            gerufen.Add(id);
            return new AufnahmeErgebnis(Zeile(n++, "Satz " + id, id));
        });

        KatalogAnkreuzen(cut, 0, 1);
        cut.Find(".epos-zweispalten-knopf--uebernehmen").Click();

        Assert.Equal(new[] { 11, 12 }, gerufen);
        Assert.Equal(3, zeilen.Count);
        Assert.Equal(0, cut.Instance.KatalogWahl.Anzahl);
    }

    [Fact]
    public void S3a_Entfernen_wirkt_auf_alle_angekreuzten_Projektzeilen()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "A"), Zeile(2, "B", 12), Zeile(3, "C", 13) };
        var entfernt = new List<string>();
        var cut = Aufbauen(zeilen, entfernen: z => entfernt.Add(z.Bezeichner));

        ProjektAnkreuzen(cut, 0, 2);
        cut.Find(".epos-zweispalten-knopf--entfernen").Click();

        Assert.Equal(new[] { "A", "C" }, entfernt);
        Assert.Equal("B", zeilen.Single().Bezeichner);
    }

    [Fact]
    public void S3a_Ein_ungesperrter_Katalogsatz_oeffnet_den_Katalogeditor()
    {
        var cut = Aufbauen(katalogsatzWege: Wege(), editorGaben: (n, b) =>
            new Dictionary<string, object> { ["Daten"] = new SolarkollektorKatalogDaten { Name = n } });

        KatalogZeileWaehlen(cut, 0);
        cut.Find(".epos-knopf--bearbeiten-katalog").Click();

        Assert.True(cut.Instance.EditorOffen);
        Assert.Null(cut.Instance.Bearbeitung);
    }

    [Fact]
    public void S3a_Mehrere_Katalogsaetze_oeffnen_die_Satzbearbeitung_gesperrte_werden_markiert()
    {
        var cut = Aufbauen(katalogsatzWege: Wege(), katalog: () => MitSchloss(12),
                           editorGaben: (n, b) => new Dictionary<string, object>());

        KatalogAnkreuzen(cut, 0, 1);
        KatalogsatzWaehlen(cut);   // AW1: die Katalogknoepfe wirken, wenn ein Katalogsatz gewaehlt ist
        cut.Find(".epos-knopf--bearbeiten-katalog").Click();

        var b = cut.Instance.Bearbeitung!.Value;
        Assert.Equal(Satzmarke.Katalogsatz, b.Art);
        Assert.Equal(2, b.Saetze.Count);
        Assert.True(b.Saetze.Single(s => s.Id == 12).Gesperrt);
        Assert.False(cut.Instance.EditorOffen);
    }

    [Fact]
    public void S3a_Ein_gesperrter_Katalogsatz_oeffnet_nur_lesend()
    {
        var cut = Aufbauen(katalogsatzWege: Wege(), katalog: () => MitSchloss(11),
                           editorGaben: (n, b) => new Dictionary<string, object>());

        KatalogZeileWaehlen(cut, 0);
        cut.Find(".epos-knopf--bearbeiten-katalog").Click();

        Assert.False(cut.Instance.EditorOffen);
        Assert.True(cut.Instance.Bearbeitung!.Value.Saetze.Single().Gesperrt);
    }

    [Fact]
    public void S3a_Projekt_Bearbeiten_nimmt_jede_Projektkopie_einmal()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "A", 100), Zeile(2, "A", 100), Zeile(3, "B", 101) };
        var cut = Aufbauen(zeilen, projektsatzWege: Wege());

        ProjektAnkreuzen(cut, 0, 1, 2);
        cut.Find(".epos-knopf--bearbeiten-projekt").Click();

        var b = cut.Instance.Bearbeitung!.Value;
        Assert.Equal(Satzmarke.Projektsatz, b.Art);
        Assert.Equal(new[] { 100, 101 }, b.Saetze.Select(s => s.Id).ToArray());
    }

    [Fact]
    public void S3a_Loeschen_mehrerer_ueberspringt_gesperrte_und_nennt_sie()
    {
        var geloescht = new List<int>();
        var cut = Aufbauen(katalog: () => MitSchloss(12), katalogLoeschen: id => { geloescht.Add(id); return ""; });

        KatalogAnkreuzen(cut, 0, 1);
        KatalogsatzWaehlen(cut);   // AW1: die Katalogknoepfe wirken, wenn ein Katalogsatz gewaehlt ist
        cut.Find(".epos-knopf--loeschen").Click();
        Assert.Contains("Vitosol 300", cut.Find(".epos-rueckfrage").TextContent);
        cut.Find(".epos-rueckfrage").QuerySelectorAll("button").First(b => b.TextContent.Trim() == "Ja").Click();

        Assert.Equal(new[] { 11 }, geloescht);
    }

    [Fact]
    public void S3a_Die_Loeschfrage_nennt_den_Satznamen()
    {
        var cut = Aufbauen();
        KatalogZeileWaehlen(cut, 1);
        cut.Find(".epos-knopf--loeschen").Click();
        Assert.Contains("Vitosol 300", cut.Find(".epos-rueckfrage").TextContent);
    }

    [Fact]
    public void S3a_Neu_waehlt_nach_dem_Editor_den_neuen_Satz()
    {
        var liste = Katalogzeilen().ToList();
        var cut = Aufbauen(katalog: () => liste,
                           editorGaben: (n, b) => new Dictionary<string, object> { ["Daten"] = new SolarkollektorKatalogDaten { Name = n } });

        Knopf(cut, "Neu…").Click();
        cut.Find(".epos-ueberlagerung input").Input("Neuer Kollektor");
        cut.Find(".epos-ueberlagerung").QuerySelectorAll("button").First(b => b.TextContent.Trim() == "OK").Click();
        Assert.True(cut.Instance.EditorOffen);

        liste.Add(new Katalogfilterzeile(13, "Neuer Kollektor").MitText(Katalogfilterprofil.SpBezeichner, "Neuer Kollektor"));
        cut.Find(".epos-ueberlagerung-zu").Click();

        Assert.False(cut.Instance.EditorOffen);
        Assert.Equal("Neuer Kollektor", cut.Instance.Katalogzeile?.Bezeichner);
    }

    [Fact]
    public void S3b_Der_Rueckweg_fragt_je_Projektkopie_einmal_und_meldet()
    {
        var gefragt = new List<IReadOnlyList<int>>();
        var geschrieben = new List<Rueckwegwahl>();
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "A", 100), Zeile(2, "A", 100), Zeile(3, "B", 101) };
        var cut = Aufbauen(zeilen, projektsatzWege: Wege(), rueckwegWege: Rueckweg(
            new[] { new Rueckwegvorschlag(100, "A", "", Rueckwegsperre.UrsprungUnbekannt, "A") }, geschrieben, gefragt));

        var leiste = cut.Find(".epos-zweispalten-bereich--projekt > .epos-zweispalten-kopfleiste");
        var knoepfe = leiste.QuerySelectorAll("button").ToList();
        int bearbeiten = knoepfe.FindIndex(k => k.ClassList.Contains("epos-knopf--bearbeiten-projekt"));
        int rueckweg = knoepfe.FindIndex(k => k.ClassList.Contains("epos-knopf--rueckweg"));
        // AW1: „Bearbeiten…" steht nicht mehr in der Projektleiste - der Rueckweg ist ihr erster Knopf.
        Assert.True(bearbeiten < 0 && rueckweg == 0);

        ProjektAnkreuzen(cut, 0, 1, 2);
        cut.Find(".epos-knopf--rueckweg").Click();

        Assert.Equal(new[] { 100, 101 }, gefragt.Single().ToArray());
        Assert.NotNull(cut.Instance.Rueckweg);
    }

    [Fact]
    public void S3b_Ohne_Projektkopie_gibt_es_keine_Rueckfrage()
    {
        var gefragt = new List<IReadOnlyList<int>>();
        var cut = Aufbauen(new List<ErzeugerZeile> { Zeile(1, "A", 0) },
                           rueckwegWege: Rueckweg(Array.Empty<Rueckwegvorschlag>(), gefragt: gefragt));

        cut.Find(".epos-knopf--rueckweg").Click();

        Assert.Empty(gefragt);
        Assert.Null(cut.Instance.Rueckweg);
    }

    [Fact]
    public void S3a_Esc_schliesst_nicht_bei_offener_Satzbearbeitung()
    {
        bool? ergebnis = null;
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "A", 100), Zeile(2, "B", 101) };
        var cut = Aufbauen(zeilen, projektsatzWege: Wege(), geschlossen: b => ergebnis = b);
        ProjektAnkreuzen(cut, 0, 1);
        cut.Find(".epos-knopf--bearbeiten-projekt").Click();
        Assert.NotNull(cut.Instance.Bearbeitung);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(ergebnis);
    }

    // =================================================================================
    // UeS1: Satz-Ueberlagerung in voller Hoehe
    // =================================================================================

    private const string FeldImKoerper = ".epos-satzueberlagerung-koerper .epos-modulparameter input[type=text]:not([readonly])";

    /// <summary>UeS1: „Bearbeiten…" EINER Projektkopie öffnet die Satz-Überlagerung, keine Satzbearbeitung.</summary>
    [Fact]
    public void UeS1_Bearbeiten_einer_Projektkopie_oeffnet_die_Satzueberlagerung()
    {
        var cut = Aufbauen(projektsatzWege: Wege());

        cut.Find(".epos-knopf--bearbeiten-projekt").Click();

        Assert.Null(cut.Instance.Bearbeitung);
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Equal("Vitosol 200", cut.Find(".epos-ueberlagerung--satz .epos-ueberlagerung-titel").TextContent);
        Assert.Equal(Resource.AUSWAHL_MARKE_PROJEKTSATZ,
                     cut.Find(".epos-ueberlagerung--satz .epos-ueberlagerung-kopf .epos-zweispalten-marke").TextContent);
        Assert.Single(cut.FindAll(".epos-modulparameter"));
        Assert.Single(cut.FindAll(".epos-satzueberlagerung-koerper .epos-modulparameter"));
        // OK der Ueberlagerung speichert und uebernimmt - keine zweiten Knoepfe im Koerper.
        Assert.Empty(cut.FindAll(".epos-satzueberlagerung-koerper .epos-speichervermerk"));
        Assert.Empty(cut.FindAll(".epos-satzueberlagerung-koerper .epos-knopf--uebernehmen-anlage"));
    }

    /// <summary>UeS1: OK schreibt die Projektkopie und übernimmt die geänderten Anlagenfelder.</summary>
    [Fact]
    public void UeS1_OK_schreibt_die_Projektkopie_und_uebernimmt_die_Anlage()
    {
        var gespeichert = new List<(int Id, IReadOnlyList<BrowserFeldwert> Felder)>();
        var uebernommen = new List<ErzeugerZeile>();
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Vitosol 200", 100) };
        var cut = Aufbauen(zeilen, projektsatzWege: Wege(gespeichert), uebernehmen: uebernommen.Add);

        cut.Find(".epos-knopf--bearbeiten-projekt").Click();
        cut.Find(FeldImKoerper).Input("Röhre");
        cut.Find(".epos-satzueberlagerung-koerper .epos-gruppenkopf-koerper").QuerySelectorAll("input")[0].Input("6");
        cut.Find(".epos-satzueberlagerung-ok").Click();

        var satz = Assert.Single(gespeichert);
        Assert.Equal(100, satz.Id);
        Assert.Equal("Röhre", satz.Felder.First(f => f.Schluessel == KatalogBrowserProfil.FeldKollektortyp).Wert);
        Assert.Single(uebernommen);
        Assert.Equal(6, zeilen[0].AnzahlModule);
        Assert.False(cut.Instance.SatzUeberlagerungOffen);
        Assert.Empty(cut.FindAll(".epos-modulparameter"));   // UeS2: zu steht nur die Zusammenfassung
    }

    /// <summary>UeS1: Wirft der Speicherweg, bleibt die Überlagerung mit dem Grund offen; die Anlage bleibt unberührt.</summary>
    [Fact]
    public void UeS1_Ein_werfender_Speicherweg_haelt_die_Satzueberlagerung_offen()
    {
        var uebernommen = new List<ErzeugerZeile>();
        var wege = new Satzbearbeitungswege
        {
            Lesen = _ => Katalogfelder("Satz"),
            Speichern = _ => throw new InvalidOperationException("Datenbank gesperrt")
        };
        var cut = Aufbauen(projektsatzWege: wege, uebernehmen: uebernommen.Add);

        cut.Find(".epos-knopf--bearbeiten-projekt").Click();
        cut.Find(FeldImKoerper).Input("Röhre");
        cut.Find(".epos-satzueberlagerung-ok").Click();

        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.False(string.IsNullOrWhiteSpace(cut.Find(".epos-satzueberlagerung-fuss [role=alert]").TextContent));
        Assert.Equal("Röhre", cut.Find(FeldImKoerper).GetAttribute("value"));
        Assert.Empty(uebernommen);
    }

    /// <summary>UeS1: Abbrechen verwirft die Feldänderung und stellt die Anlagenfelder wie beim Öffnen her.</summary>
    [Fact]
    public void UeS1_Abbrechen_verwirft_Feld_und_stellt_die_Anlagenfelder_her()
    {
        var gespeichert = new List<(int Id, IReadOnlyList<BrowserFeldwert> Felder)>();
        var uebernommen = new List<ErzeugerZeile>();
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Vitosol 200", 100) };
        var cut = Aufbauen(zeilen, projektsatzWege: Wege(gespeichert), uebernehmen: uebernommen.Add);

        cut.Find(".epos-knopf--bearbeiten-projekt").Click();
        cut.Find(FeldImKoerper).Input("Röhre");
        cut.Find(".epos-satzueberlagerung-koerper .epos-gruppenkopf-koerper").QuerySelectorAll("input")[0].Input("6");
        cut.Find(".epos-satzueberlagerung-abbrechen").Click();

        Assert.Empty(gespeichert);
        Assert.Empty(uebernommen);
        Assert.Equal(4, zeilen[0].AnzahlModule);
        Assert.False(cut.Instance.SatzUeberlagerungOffen);
        // UeS2: wieder geoeffnet steht der verworfene Wert nicht mehr da.
        Assert.Equal("4", Satz(cut).QuerySelector(".epos-gruppenkopf-koerper")!.QuerySelectorAll("input")[0].GetAttribute("value"));
        Assert.Equal("Flach", cut.Find(".epos-satzueberlagerung-koerper .epos-modulparameter input[type=text]:not([readonly])").GetAttribute("value"));
    }

    /// <summary>UeS1: Ein gesperrter Katalogsatz allein öffnet die Satz-Überlagerung nur lesend.</summary>
    [Fact]
    public void UeS1_Ein_gesperrter_Katalogsatz_oeffnet_die_Ueberlagerung_nur_lesend()
    {
        var cut = Aufbauen(katalogsatzWege: Wege(), katalog: () => MitSchloss(11),
                           katalogfelder: Katalogfelder, felderSpeichern: (_, _) => new KatalogSpeicherErgebnis(true, "ok", ""),
                           editorGaben: (n, b) => new Dictionary<string, object>());

        KatalogZeileWaehlen(cut, 0);
        cut.Find(".epos-knopf--bearbeiten-katalog").Click();

        Assert.False(cut.Instance.EditorOffen);
        Assert.Null(cut.Instance.Bearbeitung);
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Contains(Resource.ADM_SCHLOSS_ERST_AUFHEBEN, cut.Find(".epos-satzueberlagerung-hinweis").TextContent);
        Assert.Empty(cut.FindAll(".epos-satzueberlagerung-ok"));
        Assert.Empty(cut.FindAll(".epos-satzueberlagerung-koerper input[type=text]:not([readonly])"));
        cut.Find(".epos-satzueberlagerung-schliessen").Click();
        Assert.False(cut.Instance.SatzUeberlagerungOffen);
    }

    // =================================================================================
    // UeS2: Zusammenfassung der Detailzeile und Stift je Zeile (Anwenderentscheid 10.10.2026)
    // =================================================================================

    private static Dictionary<string, string> Angaben(IRenderedComponent<SolarkollektorenDialog> cut)
        => cut.FindAll(".epos-satzzusammenfassung-angabe")
              .ToDictionary(a => a.QuerySelector("dt")!.TextContent, a => a.QuerySelector("dd")!.TextContent);

    [Fact]
    public void UeS2_Die_Zusammenfassung_des_Projektsatzes_nennt_die_Anlagendaten()
    {
        var zeile = Zeile(1, "Vitosol 200");
        zeile.SolarArbeitstemperaturAusSpeicher = true;
        zeile.SolarSpreizungK = 8;
        zeile.Senken = string.Format(Resource.ANL_SENKEN_ZEILE, "Heizkreis");
        var cut = Render<SolarkollektorenDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { zeile })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.Detail, Detail)
            .Add(x => x.Modulflaeche, _ => 2.5)
            .Add(x => x.KostenOeffnen, (_, _) => Task.CompletedTask)
            .Add(x => x.Kostensumme, _ => (12345.0, 678.0)));

        cut.Find(".epos-zweispalten-satzzeile").Click();

        var k = CultureInfo.CurrentCulture;
        var angaben = Angaben(cut);
        Assert.Equal("4", angaben[Resource.AUSWAHL_ZF_ANZAHL]);
        Assert.Equal(Satzangaben.Zahl(10, "m²"), angaben[Resource.AUSWAHL_ZF_FLAECHE]);   // 2,5 m² x 4
        Assert.Equal("30° / 0°", angaben[Resource.AUSWAHL_ZF_AUSRICHTUNG]);
        Assert.Equal(Resource.SKV_OPT_ARBEITSTEMPERATUR_SPEICHER + ", Δ 8 K", angaben[Resource.AUSWAHL_ZF_SOLARKREIS]);
        Assert.Equal(string.Format(k, Resource.AUSWAHL_ZF_EURO, 12345.0), angaben[Resource.AUSWAHL_ZF_INVEST]);
        Assert.Equal(string.Format(k, Resource.AUSWAHL_ZF_EURO_JAHR, 678.0), angaben[Resource.AUSWAHL_ZF_BETRIEB]);
        Assert.Equal("Heizkreis", angaben[Resource.AUSWAHL_ZF_SENKEN]);
        // Kostenknoepfe und Kollektorfelder stehen nicht in der Zusammenfassung, sondern in der Ueberlagerung.
        Assert.Empty(cut.FindAll(".epos-kostenleiste button"));
        Assert.Empty(cut.FindAll(".epos-gruppenkopf-koerper"));
        Assert.NotEmpty(Satz(cut).QuerySelectorAll(".epos-kostenleiste button"));
    }

    [Fact]
    public void UeS2_Fester_Arbeitstemperaturweg_und_ohne_Kostenweg()
    {
        var cut = Aufbauen();
        cut.Find(".epos-zweispalten-satzzeile").Click();

        var angaben = Angaben(cut);
        Assert.Equal(Resource.SKV_OPT_ARBEITSTEMPERATUR_FEST, angaben[Resource.AUSWAHL_ZF_SOLARKREIS]);
        Assert.False(angaben.ContainsKey(Resource.AUSWAHL_ZF_INVEST));
        Assert.False(angaben.ContainsKey(Resource.AUSWAHL_ZF_BETRIEB));
        Assert.False(angaben.ContainsKey(Resource.AUSWAHL_ZF_SENKEN));
    }

    [Fact]
    public void UeS2_Die_Zusammenfassung_des_Katalogsatzes_nennt_Hersteller_Flaeche_und_Kennwerte()
    {
        var cut = Aufbauen();
        KatalogZeileWaehlen(cut, 0);
        cut.Find(".epos-zweispalten-satzzeile").Click();

        var texte = cut.FindAll(".epos-satzzusammenfassung-angabe dd").Select(e => e.TextContent).ToList();
        Assert.Equal(4, texte.Count);
        Assert.Equal("Viessmann", texte[0]);
        Assert.EndsWith(" m²", texte[1]);
        Assert.StartsWith("2", texte[1]);
        Assert.EndsWith(" W/(m²·K)", texte[3]);
        Assert.DoesNotContain("Flach", texte);   // nur die vier Angaben des Auftrags
    }

    [Fact]
    public void UeS2_Der_Stift_der_Projektzeile_oeffnet_die_Projektkopie_in_der_Ueberlagerung()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Vitosol 200", 100), Zeile(2, "Vitosol 300", 200) };
        var cut = Aufbauen(zeilen, projektsatzWege: Wege());

        var stifte = cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-zeilenstift");
        Assert.Equal(2, stifte.Length);
        stifte[1].Click();

        Assert.Equal(200, cut.Instance.Projektzeile!.GeraetId);
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Equal(Resource.AUSWAHL_MARKE_PROJEKTSATZ,
                     cut.Find(".epos-ueberlagerung--satz .epos-ueberlagerung-kopf .epos-zweispalten-marke--satz").TextContent);
        Assert.NotEmpty(cut.FindAll(".epos-satzueberlagerung-koerper .epos-gruppenkopf-koerper input"));
        Assert.Single(cut.FindAll(".epos-satzueberlagerung-ok"));
    }

    [Fact]
    public void UeS2_Ohne_Weg_der_Projektkopie_traegt_die_Projektliste_keinen_Stift_und_das_OK_uebernimmt_die_Anlage()
    {
        var zeile = Zeile(1, "Vitosol 200");
        var cut = Aufbauen(new List<ErzeugerZeile> { zeile }, uebernehmen: _ => { });
        Assert.Empty(cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-zeilenstift"));
        Assert.Equal(2, cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenstift").Length);

        // Der Assistent hat keinen Weg der Projektkopie - die Anlagenfelder uebernimmt trotzdem das OK.
        Satz(cut).QuerySelector(".epos-gruppenkopf-koerper")!.QuerySelectorAll("input")[0].Input("7");
        Ok(cut);
        Assert.Equal(7, zeile.AnzahlModule);
    }

    [Fact]
    public void UeS2_Der_Stift_einer_gesperrten_Katalogzeile_oeffnet_nur_lesend()
    {
        var cut = Aufbauen(katalog: () => MitSchloss(11), katalogfelder: Katalogfelder,
                           felderSpeichern: (_, _) => new KatalogSpeicherErgebnis(true, "ok", ""),
                           editorGaben: (n, b) => new Dictionary<string, object>());

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenstift")[0].Click();

        Assert.Equal("Vitosol 200", cut.Instance.Katalogzeile!.Bezeichner);
        Assert.False(cut.Instance.EditorOffen);
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Contains(Resource.ADM_SCHLOSS_ERST_AUFHEBEN, cut.Find(".epos-satzueberlagerung-hinweis").TextContent);
        Assert.Empty(cut.FindAll(".epos-satzueberlagerung-ok"));
    }

    [Fact]
    public void UeS2_Der_Stift_einer_ungesperrten_Katalogzeile_oeffnet_den_Katalogeditor()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder, editorGaben: (n, b) => new Dictionary<string, object>());

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenstift")[1].Click();

        Assert.Equal("Vitosol 300", cut.Instance.Katalogzeile!.Bezeichner);
        Assert.True(cut.Instance.EditorOffen);
        Assert.False(cut.Instance.SatzUeberlagerungOffen);
    }

    /// <summary>UeS2b: „Grundlagen" und „Berechnung" im Kopf der Detailzeile, offen in der Überlagerung — je Ansicht einmal.</summary>
    [Fact]
    public void UeS2b_Die_Infoknoepfe_stehen_im_Kopf_der_Detailzeile_und_nie_doppelt()
    {
        var cut = Aufbauen(projektsatzWege: Wege());
        string[] schluessel = { "Form_SolarKollektoren.Grundlagen", "Form_SolarKollektoren.Berechnung" };

        foreach (string s in schluessel)
            Assert.Single(cut.FindComponents<InfoKnopf>(), k => k.Instance.Schluessel == s);
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-satzkopf .epos-zweispalten-satzkopfknoepfe .epos-hilfepille").Count);

        Satz(cut);
        foreach (string s in schluessel)
            Assert.Single(cut.FindComponents<InfoKnopf>(), k => k.Instance.Schluessel == s);
        Assert.Empty(cut.FindAll(".epos-zweispalten-satzkopfknoepfe"));
    }

    // =================================================================================
    // AW1 (AW3a): eine Wahl je Zeile, Knopfleiste am gewaehlten Satz (Anwenderentscheide 11.10.2026)
    // =================================================================================

    private static AngleSharp.Dom.IElement AmSatz(IRenderedComponent<SolarkollektorenDialog> cut, string klasse)
        => cut.Find(".epos-zweispalten-satzknoepfe ." + klasse);

    private static void KatalogsatzWaehlen(IRenderedComponent<SolarkollektorenDialog> cut)
        => cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[0].Click();

    private static List<ErzeugerZeile> DreiZeilen()
        => new() { Zeile(1, "Kollektor A", 100), Zeile(2, "Kollektor B", 101), Zeile(3, "Kollektor C", 102) };

    /// <summary>AW1: Die Projektliste hat keinen Wahlknopf mehr — die Zeile ist die Wahl, das Kästchen die Mehrfachwahl.</summary>
    [Fact]
    public void AW1_Die_Projektliste_traegt_keinen_Wahlknopf()
    {
        var cut = Aufbauen(DreiZeilen());
        var liste = cut.FindAll(".epos-raster")[0];

        Assert.Empty(liste.QuerySelectorAll(".epos-zeilenwahl, .epos-anlagenwahl, input[type=radio]"));
        Assert.Equal(3, liste.QuerySelectorAll("td .epos-wahlkaestchen").Length);
        Assert.Equal(3, liste.QuerySelectorAll("td.epos-spalte-zeilenzelle > .epos-zeilenzelle").Length);
        Assert.DoesNotContain(liste.QuerySelectorAll("th"), th => th.TextContent.Trim() == "Wahl");
    }

    /// <summary>
    /// AW1: Ein Klick in die Zeile wählt sie — farbig hervorgehoben mit der Klasse der Katalogliste,
    /// <c>aria-current</c> an der Zelle, die Detailzeile nennt sie —, die Kästchen bleiben unberührt.
    /// </summary>
    [Fact]
    public void AW1_Ein_Klick_in_die_Zeile_waehlt_und_hebt_hervor()
    {
        var cut = Aufbauen(DreiZeilen());

        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-projektzeile")[1].Click();

        var zeilen = cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr").ToList();
        Assert.Single(zeilen, tr => tr.ClassList.Contains("epos-zeile--gewaehlt"));
        Assert.Contains("epos-zeile--gewaehlt", zeilen[1].ClassList);
        Assert.Equal("true", zeilen[1].QuerySelector(".epos-zeilenzelle")!.GetAttribute("aria-current"));
        Assert.Equal("Kollektor B", cut.Find(".epos-zweispalten-satzname").TextContent);
        Assert.Equal(Resource.AUSWAHL_MARKE_PROJEKTSATZ, cut.Find(".epos-zweispalten-satzzeile .epos-zweispalten-marke--satz").TextContent);
        Assert.Equal(0, cut.Instance.ProjektWahl.Anzahl);
        Assert.NotNull(cut.Find(".epos-zweispalten-bereich--projekt .epos-zweispalten-inhalt--zeilenwahl"));
    }

    /// <summary>AW1: Die Projektliste ist ein Tabulatorhalt; ↑ ↓ Pos1 Ende bewegen die Wahl.</summary>
    [Fact]
    public void AW1_Die_Pfeiltasten_bewegen_die_Wahl_der_Projektliste()
    {
        var cut = Aufbauen(DreiZeilen());
        var huelle = cut.Find(".epos-zweispalten-bereich--projekt .epos-raster-huelle");
        Assert.Equal("0", huelle.GetAttribute("tabindex"));

        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-projektzeile")[0].Click();
        cut.Find(".epos-zweispalten-bereich--projekt .epos-raster-huelle").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal("Kollektor B", cut.Find(".epos-zweispalten-satzname").TextContent);
        cut.Find(".epos-zweispalten-bereich--projekt .epos-raster-huelle").KeyDown(new KeyboardEventArgs { Key = "End" });
        Assert.Equal("Kollektor C", cut.Find(".epos-zweispalten-satzname").TextContent);
        cut.Find(".epos-zweispalten-bereich--projekt .epos-raster-huelle").KeyDown(new KeyboardEventArgs { Key = "Home" });
        Assert.Equal("Kollektor A", cut.Find(".epos-zweispalten-satzname").TextContent);
        Assert.Contains("epos-zeile--gewaehlt", cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr")[0].ClassList);
    }

    /// <summary>
    /// AW1: Ist ein Projektsatz gewählt, sind Schloss… und Löschen weich gesperrt — fokussierbar,
    /// <c>aria-disabled</c>, Grund „Katalogsatz wählen" als Kurztext, ein Klick meldet ihn und
    /// schreibt nichts. Bearbeiten… öffnet die Projektkopie in der Überlagerung.
    /// </summary>
    [Fact]
    public void AW1_Beim_Projektsatz_sind_Schloss_und_Loeschen_weich_gesperrt()
    {
        var geloescht = new List<int>();
        var pruefung = new Schlosspruefung();
        var cut = Aufbauen(DreiZeilen(), projektsatzWege: Wege(), schloss: pruefung.Weg(),
                           katalogLoeschen: id => { geloescht.Add(id); return ""; });
        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-projektzeile")[0].Click();
        KatalogAnkreuzen(cut, 0);   // auch angekreuzte Katalogzeilen heben die Sperre nicht auf

        foreach (string klasse in new[] { "epos-katalogschloss", "epos-knopf--loeschen" })
        {
            var knopf = AmSatz(cut, klasse);
            Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
            Assert.False(knopf.HasAttribute("disabled"));
            Assert.Equal(Resource.AUSWAHL_SPERRE_KATALOGSATZ, knopf.GetAttribute("title"));
        }

        AmSatz(cut, "epos-knopf--loeschen").Click();
        Assert.Equal(Resource.AUSWAHL_SPERRE_KATALOGSATZ, cut.Find(".epos-warnbanner-text").TextContent);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
        AmSatz(cut, "epos-katalogschloss").Click();
        Assert.Empty(pruefung.Aufrufe);
        Assert.Empty(geloescht);

        Assert.Empty(cut.FindAll(".epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-katalog"));
        AmSatz(cut, "epos-knopf--bearbeiten-projekt").Click();
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Equal(Resource.AUSWAHL_MARKE_PROJEKTSATZ,
                     cut.Find(".epos-ueberlagerung--satz .epos-ueberlagerung-kopf .epos-zweispalten-marke--satz").TextContent);
    }

    /// <summary>AW1: Beim Katalogsatz wirken Schloss…, Löschen und Bearbeiten… auf den Katalog.</summary>
    [Fact]
    public void AW1_Beim_Katalogsatz_wirken_Schloss_und_Loeschen_auf_den_Katalog()
    {
        var pruefung = new Schlosspruefung();
        var cut = Aufbauen(new List<ErzeugerZeile>(), schloss: pruefung.Weg(), katalogfelder: n => Katalogfelder(n),
                           katalogLoeschen: _ => "");
        KatalogsatzWaehlen(cut);

        foreach (string klasse in new[] { "epos-katalogschloss", "epos-knopf--loeschen" })
        {
            var knopf = AmSatz(cut, klasse);
            Assert.False(knopf.HasAttribute("aria-disabled"));
            Assert.False(knopf.HasAttribute("disabled"));
        }
        Assert.Empty(cut.FindAll(".epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-projekt"));
        Assert.NotNull(AmSatz(cut, "epos-knopf--bearbeiten-katalog"));

        AmSatz(cut, "epos-knopf--loeschen").Click();
        Assert.NotEmpty(cut.FindAll(".epos-rueckfrage"));
    }

    /// <summary>
    /// AW1: Der Stift einer Projektzeile tut, was „Bearbeiten…" am Satz für diese Zeile tut — die
    /// Zeile wird der gewählte Satz, die Überlagerung zeigt ihre Projektkopie. Der Stift der
    /// Detailzeile ist entfallen, die Fussleiste unter dem Katalog auch.
    /// </summary>
    [Fact]
    public void AW1_Der_Stift_je_Zeile_ist_Bearbeiten_fuer_diese_Zeile()
    {
        var cut = Aufbauen(DreiZeilen(), projektsatzWege: Wege());
        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-projektzeile")[0].Click();
        Assert.Empty(cut.FindAll(".epos-zweispalten-satzkopf > .epos-zweispalten-bearbeiten"));
        Assert.Empty(cut.FindAll(".epos-zweispalten-fussleiste"));
        Assert.Null(cut.Find(".epos-zweispalten-bereich--projekt > .epos-zweispalten-kopfleiste")
                       .QuerySelector(".epos-knopf--bearbeiten-projekt"));

        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-zeilenstift")[2].Click();

        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Equal("Kollektor C", cut.Find(".epos-ueberlagerung--satz .epos-ueberlagerung-titel").TextContent.Trim());
        Assert.Contains("epos-zeile--gewaehlt", cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr")[2].ClassList);
    }

    /// <summary>
    /// AW1: Vergleichen… wirkt im Bereich des gewählten Satzes — beim Projektsatz die angekreuzten
    /// Projektkopien (je Gerät einmal), unter zwei gesperrt.
    /// </summary>
    [Fact]
    public void AW1_Vergleichen_beim_Projektsatz_vergleicht_die_angekreuzten_Projektkopien()
    {
        var cut = Aufbauen(DreiZeilen(), projektsatzWege: Wege());
        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-projektzeile")[0].Click();
        Assert.True(AmSatz(cut, "epos-knopf--vergleichen").HasAttribute("disabled"));

        foreach (int i in new[] { 0, 2 })
            cut.FindAll(".epos-raster")[0].QuerySelectorAll("td .epos-wahlkaestchen")[i].Click();
        Assert.False(AmSatz(cut, "epos-knopf--vergleichen").HasAttribute("disabled"));
        AmSatz(cut, "epos-knopf--vergleichen").Click();

        var koepfe = cut.FindAll(".epos-vergleich thead th").Select(th => th.TextContent).ToList();
        Assert.Equal(new[] { "Kollektor A", "Kollektor C" }, koepfe.Skip(1).ToArray());
        Assert.NotEmpty(cut.FindAll(".epos-vergleich tbody tr"));
    }
}
