using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Export;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Exportdialog „Gebäude exportieren (gbXML)" (Gebäudesimulation G7a, Welle W3): Format und Umfang
/// fest als Text, die freiwillige Postleitzahl, die Meldungsliste vor dem Schreiben, die Bestätigung vor
/// dem Speichern (weiche Sperre mit Grund), die Ablehnung mit Grund und ohne Speicherknopf, der Rückweg
/// — Abbrechen, Esc und das Kreuz rufen den Speicherweg NIE —, der Hinweis „gespeicherter Stand", der
/// Dialog ohne Gaben und der Assistent (Postleitzahl setzbar, Bestätigung nur zu lesen).
///
/// <para>Die Kultur ist auf de-DE gepinnt: Die Erwartungswerte sind deutsche Beschriftungen.</para>
/// </summary>
public class GebaeudeExportDialogTests : EposBunitContext
{
    public GebaeudeExportDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static GebaeudeExportAnsicht Schreibbar(string plz) => new(new[]
    {
        new GebaeudeExportMeldung(WarnStufe.Warnung, "Warnung", "Das Bauteil „Tür“ wird masselos geschrieben.", "GEXP_PROT_MASSELOS"),
        new GebaeudeExportMeldung(WarnStufe.Hinweis, "Info", plz.Trim().Length == 0 ? "Ohne Ort." : "Ort " + plz.Trim(),
                                  plz.Trim().Length == 0 ? "GEXP_PROT_OHNE_ORT" : "GEXP_PROT_ORT"),
    }, null);

    private static GebaeudeExportAnsicht Abgelehnt(string _) => new(new[]
    {
        new GebaeudeExportMeldung(WarnStufe.Fehler, "Fehler", "Zu wenig Flächen.", "GEXP_PROT_ZU_WENIG_FLAECHEN"),
    }, "Das Gebäude trägt 2 Flächen; gbXML verlangt mindestens 4.");

    /// <summary>Der Prüfstand: zählt die Aufrufe beider Delegaten und merkt die Postleitzahlen.</summary>
    private sealed class Stand
    {
        internal readonly List<string> Vorbereitet = new();
        internal readonly List<string> Gespeichert = new();
        internal readonly List<string> Formatfolge = new();
        internal readonly List<string> GespeichertFormat = new();
        internal int Zusagen;
        internal string? ZusageAntwort;
        internal readonly List<GebaeudeExportErgebnis?> Geschlossen = new();
        internal Func<string, GebaeudeExportAnsicht> Ansicht = Schreibbar;
        internal GebaeudeExportErgebnis Ergebnis = new(true, false, "Gespeichert: C:\\Probe\\haus.xml (1234 Byte).");
    }

    private IRenderedComponent<GebaeudeExportDialog> Aufbauen(Stand s, bool gespeicherterStand = false,
                                                              GebaeudeExportTexte? texte = null,
                                                              IReadOnlyList<GebaeudeExportFormat>? formate = null)
        => Render<GebaeudeExportDialog>(p => p
            .Add(x => x.Vorbereiten, e =>
            {
                s.Vorbereitet.Add(e.Plz);
                s.Formatfolge.Add(e.Format);
                return Task.FromResult(s.Ansicht(e.Plz));
            })
            .Add(x => x.Speichern, e =>
            {
                s.Gespeichert.Add(e.Plz);
                s.GespeichertFormat.Add(e.Format);
                return Task.FromResult(s.Ergebnis);
            })
            .Add(x => x.Formate, formate ?? Array.Empty<GebaeudeExportFormat>())
            .Add(x => x.ZusageOeffnen, () => { s.Zusagen++; return Task.FromResult(s.ZusageAntwort); })
            .Add(x => x.GespeicherterStand, gespeicherterStand)
            .Add(x => x.Entprellung, 0)
            .Add(x => x.Texte, texte ?? new GebaeudeExportTexte())
            .Add(x => x.Geschlossen, e => s.Geschlossen.Add(e)));

    private static IElement Primaer(IRenderedComponent<GebaeudeExportDialog> cut)
        => cut.Find(".epos-gebexport-dialog > .epos-leiste button.epos-knopf--primaer");

    private static IElement Knopf(IRenderedComponent<GebaeudeExportDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static void Bestaetigen(IRenderedComponent<GebaeudeExportDialog> cut, bool wert = true)
        => cut.Find(".epos-gebexport-dialog input[type=checkbox]").Change(wert);

    // =====================================================================
    //  Feldbestand
    // =====================================================================

    [Fact]
    public void Format_und_Umfang_stehen_fest_und_die_Meldungen_vor_dem_Schreiben()
    {
        var s = new Stand();
        var cut = Aufbauen(s);

        Assert.Equal(new[] { "" }, s.Vorbereitet);
        string kopf = cut.Find(".epos-gebexport-kopf").TextContent;
        Assert.Contains("gbXML (Schemafassung 6.01)", kopf);
        Assert.Contains(R.GEXP_STUFE_DATEN, kopf);
        Assert.Contains("Postleitzahl (freiwillig)", cut.Markup);
        Assert.Single(cut.FindAll("input.epos-eingabe"));

        IReadOnlyList<IElement> zeilen = cut.FindAll(".epos-gebexport-meldungen tbody tr").ToList();
        Assert.Equal(2, zeilen.Count);
        Assert.Contains("epos-gebexport-meldung--warnung", zeilen[0].ClassName);
        Assert.Equal("GEXP_PROT_MASSELOS", zeilen[0].GetAttribute("data-schluessel"));
        Assert.Contains("masselos", zeilen[0].TextContent);
        Assert.Contains("Ich habe die Meldungen gelesen", cut.Markup);
        Assert.Equal("Speichern…", Primaer(cut).TextContent.Trim());
        Assert.DoesNotContain("epos-gebexport-gespeichert", cut.Markup);
    }

    [Fact]
    public void Eine_geaenderte_Zeile_sagt_dass_der_gespeicherte_Stand_exportiert_wird()
    {
        var cut = Aufbauen(new Stand(), gespeicherterStand: true);
        Assert.Contains("Exportiert wird der gespeicherte Stand", cut.Find(".epos-gebexport-gespeichert").TextContent);
    }

    [Fact]
    public void Die_Postleitzahl_bildet_den_Plan_neu()
    {
        var s = new Stand();
        var cut = Aufbauen(s);

        cut.Find("input.epos-eingabe").Input("01067");

        Assert.Equal(new[] { "", "01067" }, s.Vorbereitet);
        Assert.Contains("Ort 01067", cut.Find(".epos-gebexport-meldungen").TextContent);
        Assert.Empty(s.Gespeichert);
    }

    // =====================================================================
    //  Bestaetigen, dann Speichern
    // =====================================================================

    [Fact]
    public void Ohne_Bestaetigung_ist_Speichern_weich_gesperrt_und_nennt_den_Grund()
    {
        var s = new Stand();
        var cut = Aufbauen(s);

        IElement ok = Primaer(cut);
        Assert.Equal("true", ok.GetAttribute("aria-disabled"));
        Assert.Equal("Erst die Meldungen bestätigen.", ok.GetAttribute("title"));

        ok.Click();
        Assert.Empty(s.Gespeichert);
        Assert.Empty(s.Geschlossen);
        Assert.Equal("Erst die Meldungen bestätigen.", cut.Instance.Meldung);

        Bestaetigen(cut);
        Assert.Null(Primaer(cut).GetAttribute("aria-disabled"));
        Assert.Equal("", cut.Instance.Meldung);
    }

    [Fact]
    public void Bestaetigt_speichert_mit_der_Postleitzahl_und_gibt_die_Rueckmeldung_zurueck()
    {
        var s = new Stand();
        var cut = Aufbauen(s);

        cut.Find("input.epos-eingabe").Input("10115");
        Bestaetigen(cut);
        Primaer(cut).Click();

        Assert.Equal(new[] { "10115" }, s.Gespeichert);
        GebaeudeExportErgebnis? e = Assert.Single(s.Geschlossen);
        Assert.NotNull(e);
        Assert.True(e!.Gespeichert);
        Assert.Contains("haus.xml", e.Text);
    }

    [Fact]
    public void Eine_abgebrochene_Dateiwahl_laesst_den_Dialog_offen_und_still()
    {
        var s = new Stand { Ergebnis = GebaeudeExportErgebnis.Abbruch };
        var cut = Aufbauen(s);

        Bestaetigen(cut);
        Primaer(cut).Click();

        Assert.Single(s.Gespeichert);
        Assert.Empty(s.Geschlossen);
        Assert.Equal("", cut.Instance.Meldung);
    }

    [Fact]
    public void Ein_Schreibfehler_bleibt_im_Dialog_stehen()
    {
        var s = new Stand { Ergebnis = new GebaeudeExportErgebnis(false, false, "Die Datei konnte nicht geschrieben werden: gesperrt") };
        var cut = Aufbauen(s);

        Bestaetigen(cut);
        Primaer(cut).Click();

        Assert.Empty(s.Geschlossen);
        Assert.Equal(WarnStufe.Fehler, cut.Instance.MeldungStufe);
        Assert.Contains("gesperrt", cut.Find(".epos-warnbanner").TextContent);
    }

    // =====================================================================
    //  Ablehnung
    // =====================================================================

    [Fact]
    public void Eine_Ablehnung_zeigt_den_Grund_und_keinen_Speicherknopf()
    {
        var s = new Stand { Ansicht = Abgelehnt };
        var cut = Aufbauen(s);

        Assert.Contains("Das Gebäude lässt sich nicht exportieren: Das Gebäude trägt 2 Flächen",
                        cut.Find(".epos-warnbanner").TextContent);
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Contains("Speichern"));
        Assert.Empty(cut.FindAll("input[type=checkbox]"));

        IElement schliessen = Primaer(cut);
        Assert.Equal("Schließen", schliessen.TextContent.Trim());
        schliessen.Click();
        Assert.Empty(s.Gespeichert);
        Assert.Null(Assert.Single(s.Geschlossen));
    }

    // =====================================================================
    //  Rueckweg
    // =====================================================================

    [Fact]
    public void Abbrechen_Esc_und_Kreuz_rufen_nie_den_Speicherweg()
    {
        var s = new Stand();
        var cut = Aufbauen(s);
        Bestaetigen(cut);

        Knopf(cut, "Abbrechen").Click();
        cut.Find(".epos-gebexport-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        cut.Find(".epos-gebexport-dialog .epos-dialog-zu").Click();

        Assert.Empty(s.Gespeichert);
        Assert.Equal(3, s.Geschlossen.Count);
        Assert.All(s.Geschlossen, Assert.Null);
    }

    [Fact]
    public void Ohne_Gaben_steht_der_Dialog_und_speichert_nicht()
    {
        var cut = Render<GebaeudeExportDialog>(p => p.Add(x => x.Entprellung, 0));

        Assert.Equal("Gebäude exportieren", cut.Find(".epos-dialog-titel").TextContent.Trim());
        Assert.Contains("Die Daten des Gebäudes werden gelesen", cut.Find(".epos-gebexport-vorbereitung").TextContent);
        Assert.NotNull(Primaer(cut).GetAttribute("disabled"));
    }

    [Fact]
    public void Die_Beschriftung_des_Speicherknopfs_kommt_aus_dem_Buendel()
    {
        var cut = Aufbauen(new Stand(), texte: new GebaeudeExportTexte { Speichern = "Speichern und teilen…" });
        Assert.Equal("Speichern und teilen…", Primaer(cut).TextContent.Trim());
    }

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Sie bindet über die Sichtklasse
    /// <c>GebaeudeExportKiSicht</c>: die Postleitzahl setzbar, die Bestätigung nur zu lesen — bestätigen,
    /// die Meldungen gelesen zu haben, kann nur der Anwender.
    /// </summary>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an()
    {
        var s = new Stand();
        var cut = Aufbauen(s);

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.GEBAEUDE_EXPORT));
        KiFeldzugang plz = KiMaskenbruecke.Feldzugang(KiMaskennamen.GEBAEUDE_EXPORT, "plz");
        Assert.NotNull(plz);
        Assert.True(plz.Setzbar);
        plz.Setzen("80331");
        cut.WaitForAssertion(() => Assert.Contains("80331", s.Vorbereitet));
        Assert.Equal("80331", plz.Lesen());

        KiFeldzugang bestaetigt = KiMaskenbruecke.Feldzugang(KiMaskennamen.GEBAEUDE_EXPORT, "bestaetigt");
        Assert.NotNull(bestaetigt);
        Assert.False(bestaetigt.Setzbar);
        Assert.Equal(false, bestaetigt.Lesen());
        Bestaetigen(cut);
        Assert.Equal(true, bestaetigt.Lesen());
        Assert.Empty(s.Gespeichert);
    }

    // =====================================================================
    //  Formatwahl (Stufe G7c): gbXML oder IFC, Abbruch des Schreibens, Exportzusage
    // =====================================================================

    /// <summary>Die Formate der Hülle — Steuerwerte und Ressourcentexte, wie der Gebäudedialog sie reicht.</summary>
    private static IReadOnlyList<GebaeudeExportFormat> Formate() => GebaeudeExportHuelle.Formate();

    private static IReadOnlyList<IElement> Formatknoepfe(IRenderedComponent<GebaeudeExportDialog> cut)
        => cut.FindAll(".epos-gebexport-dialog input[type=radio]").ToList();

    [Fact]
    public void Die_Formatwahl_steht_mit_Vorgabe_gbXML_und_reicht_den_Steuerwert()
    {
        var s = new Stand();
        var cut = Aufbauen(s, formate: Formate());

        IReadOnlyList<IElement> knoepfe = Formatknoepfe(cut);
        Assert.Equal(2, knoepfe.Count);
        Assert.True(knoepfe[0].HasAttribute("checked"));
        Assert.Contains("gbXML (Schemafassung 6.01)", cut.Markup);
        Assert.Contains("IFC 4 (semantisch)", cut.Markup);
        Assert.Contains("Daten mit schematischer Raumgeometrie", cut.Find(".epos-gebexport-kopf").TextContent);
        Assert.Equal(new[] { "GBXML" }, s.Formatfolge);
        Assert.Equal("GBXML", cut.Instance.Formatwert);
        Assert.Empty(cut.FindAll(".epos-gebexport-zusageknopf"));
    }

    [Fact]
    public void Ein_Formatwechsel_bildet_den_Plan_neu_nimmt_die_Bestaetigung_zurueck_und_speichert_im_Format()
    {
        var s = new Stand();
        var cut = Aufbauen(s, formate: Formate());
        Bestaetigen(cut);
        Assert.True(cut.Instance.Bestaetigt);

        Formatknoepfe(cut)[1].Change("1");

        Assert.Equal(new[] { "GBXML", "IFC" }, s.Formatfolge);
        Assert.Equal("IFC", cut.Instance.Formatwert);
        Assert.False(cut.Instance.Bestaetigt);
        Assert.Contains(R.GEXP_STUFE_DATEN, cut.Find(".epos-gebexport-kopf").TextContent);
        Assert.NotNull(cut.Find(".epos-gebexport-zusageknopf"));

        Bestaetigen(cut);
        Primaer(cut).Click();
        Assert.Equal(new[] { "IFC" }, s.GespeichertFormat);
        Assert.Single(s.Geschlossen);
    }

    [Fact]
    public void Ein_abgebrochenes_Schreiben_zeigt_Fehler_und_Meldungen_und_schliesst_nicht()
    {
        var abbruch = new[]
        {
            new GebaeudeExportMeldung(WarnStufe.Fehler, "Fehler", "IfcWall: Name fehlt.", "GEXP_PROT_IFC_SCHEMA"),
            new GebaeudeExportMeldung(WarnStufe.Fehler, "Fehler", "IFC-Export abgebrochen: 1 Verstoß.", "GEXP_PROT_IFC_ABGEBROCHEN"),
        };
        var s = new Stand { Ergebnis = new GebaeudeExportErgebnis(false, false, R.GEXP_MSG_NICHTS_GESCHRIEBEN, abbruch) };
        var cut = Aufbauen(s, formate: Formate());
        Formatknoepfe(cut)[1].Change("1");
        Bestaetigen(cut);

        Primaer(cut).Click();

        Assert.Equal(new[] { "IFC" }, s.GespeichertFormat);
        Assert.Empty(s.Geschlossen);
        Assert.Equal(WarnStufe.Fehler, cut.Instance.MeldungStufe);
        Assert.Contains("es wurde keine Datei geschrieben", cut.Find(".epos-warnbanner").TextContent);
        IReadOnlyList<IElement> zeilen = cut.FindAll(".epos-gebexport-meldungen tbody tr").ToList();
        Assert.Equal(2, zeilen.Count);
        Assert.Equal("GEXP_PROT_IFC_ABGEBROCHEN", zeilen[1].GetAttribute("data-schluessel"));
        Assert.Contains("epos-gebexport-meldung--fehler", zeilen[1].ClassName);
    }

    [Fact]
    public void Der_Zusageknopf_ruft_die_Naht_und_nennt_einen_Grund()
    {
        var s = new Stand();
        var cut = Aufbauen(s, formate: Formate());
        Formatknoepfe(cut)[1].Change("1");

        IElement knopf = cut.Find(".epos-gebexport-zusageknopf");
        Assert.Equal("Exportzusage (IDS) öffnen…", knopf.TextContent.Trim());
        knopf.Click();
        Assert.Equal(1, s.Zusagen);
        Assert.Equal("", cut.Instance.Meldung);

        s.ZusageAntwort = "Die Exportzusage liegt nicht vor: /app/Vorlage/EPOS_Export.ids";
        cut.Find(".epos-gebexport-zusageknopf").Click();
        Assert.Equal(2, s.Zusagen);
        Assert.Contains("liegt nicht vor", cut.Find(".epos-warnbanner").TextContent);
        Assert.Empty(s.Gespeichert);
    }

    [Fact]
    public void Ohne_Formatliste_steht_das_Format_als_Text()
    {
        var s = new Stand();
        var cut = Aufbauen(s);
        Assert.Empty(Formatknoepfe(cut));
        Assert.Equal(new[] { "" }, s.Formatfolge);
        Assert.Empty(cut.FindAll(".epos-gebexport-zusageknopf"));
    }

    [Fact]
    public void Kein_Anzeigetext_ist_ein_Steuerwert()
    {
        var cut = Aufbauen(new Stand(), formate: Formate());
        IReadOnlyList<string> beschriftungen = cut.FindAll(".epos-gebexport-dialog label").Select(l => l.TextContent.Trim()).ToList();
        foreach (GebaeudeExportFormat f in Formate())
        {
            Assert.NotEqual(f.Wert, f.Text);
            Assert.DoesNotContain(f.Wert, beschriftungen);
            Assert.Contains(beschriftungen, b => b.Contains(f.Text, StringComparison.Ordinal));
        }
        Assert.Equal(new[] { "GBXML", "IFC" }, Formate().Select(f => f.Wert));
        Assert.Equal(new[] { false, true }, Formate().Select(f => f.MitZusage));
    }

    /// <summary>Die neuen Texte der Formatwahl stehen in beiden Sprachen, nicht leer und übersetzt.</summary>
    [Theory]
    [InlineData("GEXP_TITEL_FORMATWAHL")]
    [InlineData("GEXP_FORMAT_IFC")]
    [InlineData("GEXP_STUFE_SCHEMATISCH")]
    [InlineData("GEXP_DATEITYP_GBXML")]
    [InlineData("GEXP_DATEITYP_IFC")]
    [InlineData("GEXP_MSG_NICHTS_GESCHRIEBEN")]
    [InlineData("GEXP_BTN_ZUSAGE")]
    [InlineData("GEXP_BTN_ZUSAGE_IOS")]
    [InlineData("GEXP_ZUSAGE_HINWEIS")]
    [InlineData("GEXP_MSG_ZUSAGE_FEHLT")]
    [InlineData("GEXP_MSG_ZUSAGE_FEHLER")]
    public void Die_Texte_der_Formatwahl_stehen_in_beiden_Sprachen(string schluessel)
    {
        string? de = R.ResourceManager.GetString(schluessel, new System.Globalization.CultureInfo("de-DE"));
        string? en = R.ResourceManager.GetString(schluessel, new System.Globalization.CultureInfo("en-US"));
        Assert.False(string.IsNullOrWhiteSpace(de), schluessel + " (de)");
        Assert.False(string.IsNullOrWhiteSpace(en), schluessel + " (en)");
        Assert.NotEqual(de, en);
    }

    // =====================================================================
    //  Anreicherung der Originaldatei (Stufe G7d)
    // =====================================================================

    /// <summary>Der Prüfstand der Anreicherung: Quelle vorhanden?, Antwort der Dateiwahl, Ergebnis des Wegs.</summary>
    private sealed class Anreicherungsstand
    {
        internal bool Moeglich = true;
        internal int Wahlen;
        internal GebaeudeAnreicherungWahl? Wahl = Frei();
        internal readonly List<GebaeudeExportEingabe> Angereichert = new();
        internal GebaeudeExportErgebnis Ergebnis =
            new(true, false, "Die angereicherte Datei wurde gespeichert: C:\\Probe\\haus_EPOS.ifc (2048 Byte). Objekte: 5, ergänzt: 3, ersetzt: 1, übersprungen: 1.");
    }

    private static GebaeudeAnreicherungWahl Frei() => new("haus.ifc", "haus.ifc, importiert am 01.10.2026 10:00", new[]
    {
        new GebaeudeExportMeldung(WarnStufe.Warnung, "Warnung", "Sie geben eine fremde Datei verändert weiter.",
                                  "GEXP_PROT_ANR_BEIPACK_FREMDDATEI"),
    }, null);

    private static GebaeudeAnreicherungWahl Gesperrt() => new("anders.ifc", "haus.ifc, importiert am 01.10.2026 10:00", new[]
    {
        new GebaeudeExportMeldung(WarnStufe.Fehler, "Fehler", "Rückgabe verweigert: Die gewählte Datei ist nicht die importierte Datei.",
                                  "GEXP_PROT_ANR_HASH"),
    }, "Rückgabe verweigert: Die gewählte Datei ist nicht die importierte Datei.");

    private IRenderedComponent<GebaeudeExportDialog> AufbauenMitAnreicherung(Stand s, Anreicherungsstand a,
                                                                             GebaeudeExportTexte? texte = null)
        => Render<GebaeudeExportDialog>(p => p
            .Add(x => x.Vorbereiten, e => { s.Vorbereitet.Add(e.Plz); s.Formatfolge.Add(e.Format); return Task.FromResult(s.Ansicht(e.Plz)); })
            .Add(x => x.Speichern, e => { s.Gespeichert.Add(e.Plz); s.GespeichertFormat.Add(e.Format); return Task.FromResult(s.Ergebnis); })
            .Add(x => x.Formate, Formate())
            .Add(x => x.AnreicherungMoeglich, () => Task.FromResult(a.Moeglich))
            .Add(x => x.OriginalWaehlen, () => { a.Wahlen++; return Task.FromResult(a.Wahl); })
            .Add(x => x.Anreichern, e => { a.Angereichert.Add(e); return Task.FromResult(a.Ergebnis); })
            .Add(x => x.Entprellung, 0)
            .Add(x => x.Texte, texte ?? new GebaeudeExportTexte())
            .Add(x => x.Geschlossen, e => s.Geschlossen.Add(e)));

    private static void FormatIfc(IRenderedComponent<GebaeudeExportDialog> cut)
        => cut.FindAll(".epos-gebexport-dialog input[type=radio]")[1].Change("1");

    private static void WegAnreichern(IRenderedComponent<GebaeudeExportDialog> cut)
        => cut.FindAll(".epos-gebexport-dialog input[type=radio]")[3].Change("1");

    private static void PlanBestaetigen(IRenderedComponent<GebaeudeExportDialog> cut)
        => cut.FindAll(".epos-gebexport-dialog input[type=checkbox]").Last().Change(true);

    private static void BeipackBestaetigen(IRenderedComponent<GebaeudeExportDialog> cut)
        => cut.Find(".epos-gebexport-anreicherung input[type=checkbox]").Change(true);

    [Fact]
    public void Die_Wegwahl_steht_nur_bei_IFC_und_mit_Importquelle()
    {
        var s = new Stand();
        var cut = AufbauenMitAnreicherung(s, new Anreicherungsstand());
        Assert.DoesNotContain("Originaldatei anreichern", cut.Markup);   // gbXML

        FormatIfc(cut);
        Assert.Contains("Eigene Datei schreiben", cut.Markup);
        Assert.Contains("Originaldatei anreichern", cut.Markup);
        Assert.False(cut.Instance.Anreichernd);                           // Vorgabe: eigene Datei
        Assert.Empty(cut.FindAll(".epos-gebexport-originalknopf"));

        var ohne = AufbauenMitAnreicherung(new Stand(), new Anreicherungsstand { Moeglich = false });
        FormatIfc(ohne);
        Assert.DoesNotContain("Originaldatei anreichern", ohne.Markup);
        Assert.Equal(2, ohne.FindAll(".epos-gebexport-dialog input[type=radio]").Count);
    }

    [Fact]
    public void Eine_Verweigerung_zeigt_den_Grund_und_wechselt_auf_die_eigene_Datei()
    {
        var s = new Stand();
        var a = new Anreicherungsstand { Wahl = Gesperrt() };
        var cut = AufbauenMitAnreicherung(s, a);
        FormatIfc(cut);
        cut.Find("input.epos-eingabe").Input("01067");
        WegAnreichern(cut);
        Knopf(cut, "Originaldatei wählen…").Click();

        Assert.Equal(1, a.Wahlen);
        Assert.Equal("anders.ifc", cut.Find(".epos-gebexport-originalname").TextContent.Trim());
        Assert.Contains("haus.ifc, importiert am", cut.Find(".epos-gebexport-quelle").TextContent);
        Assert.Contains("Die Originaldatei kann nicht angereichert werden: Rückgabe verweigert", cut.Markup);
        Assert.Equal("GEXP_PROT_ANR_HASH", cut.Find(".epos-gebexport-anrmeldungen tbody tr").GetAttribute("data-schluessel"));
        Assert.Empty(cut.FindAll(".epos-gebexport-anreicherung input[type=checkbox]"));   // keine Bestätigung

        PlanBestaetigen(cut);
        Primaer(cut).Click();
        Assert.Empty(a.Angereichert);
        Assert.Empty(s.Gespeichert);
        Assert.Contains("Rückgabe verweigert", cut.Instance.Meldung);

        cut.Find(".epos-gebexport-stattknopf").Click();
        Assert.False(cut.Instance.Anreichernd);
        Primaer(cut).Click();
        Assert.Equal(new[] { "01067" }, s.Gespeichert);                   // dieselben Eingaben
        Assert.Equal(new[] { "IFC" }, s.GespeichertFormat);
        Assert.Empty(a.Angereichert);
    }

    [Fact]
    public void Die_Freigabe_verlangt_die_Bestaetigung_des_Beipackzettels()
    {
        var s = new Stand();
        var a = new Anreicherungsstand();
        var cut = AufbauenMitAnreicherung(s, a);
        FormatIfc(cut);
        WegAnreichern(cut);
        PlanBestaetigen(cut);

        Primaer(cut).Click();                                             // ohne Datei
        Assert.Equal("Bitte wählen Sie zuerst die Originaldatei.", cut.Instance.Meldung);

        Knopf(cut, "Originaldatei wählen…").Click();
        Assert.Contains("Sie geben eine fremde Datei verändert weiter", cut.Find(".epos-gebexport-anrmeldungen").TextContent);
        Assert.Equal("Bitte bestätigen Sie, dass Sie die fremde Datei verändert weitergeben.",
                     Primaer(cut).GetAttribute("title"));
        Primaer(cut).Click();
        Assert.Empty(a.Angereichert);
        Assert.Equal("Bitte bestätigen Sie, dass Sie die fremde Datei verändert weitergeben.", cut.Instance.Meldung);
        Assert.Empty(s.Geschlossen);

        BeipackBestaetigen(cut);
        Assert.True(cut.Instance.BeipackBestaetigt);
        Primaer(cut).Click();
        GebaeudeExportEingabe e = Assert.Single(a.Angereichert);
        Assert.Equal("IFC", e.Format);
        Assert.Empty(s.Gespeichert);
        GebaeudeExportErgebnis? zurueck = Assert.Single(s.Geschlossen);
        Assert.Contains("Objekte: 5, ergänzt: 3, ersetzt: 1, übersprungen: 1", zurueck!.Text);
    }

    [Fact]
    public void Eine_neue_Dateiwahl_nimmt_die_Bestaetigung_zurueck_und_ein_Abbruch_laesst_sie_stehen()
    {
        var a = new Anreicherungsstand();
        var cut = AufbauenMitAnreicherung(new Stand(), a);
        FormatIfc(cut);
        WegAnreichern(cut);
        Knopf(cut, "Originaldatei wählen…").Click();
        BeipackBestaetigen(cut);

        a.Wahl = null;                                                    // Dateiwahl abgebrochen
        Knopf(cut, "Originaldatei wählen…").Click();
        Assert.True(cut.Instance.BeipackBestaetigt);
        Assert.Equal("haus.ifc", cut.Instance.Wahl!.Dateiname);

        a.Wahl = Frei();
        Knopf(cut, "Originaldatei wählen…").Click();
        Assert.False(cut.Instance.BeipackBestaetigt);
    }

    [Fact]
    public void Eine_Verweigerung_beim_Schreiben_bleibt_im_Dialog_mit_den_Meldungen()
    {
        var s = new Stand();
        var a = new Anreicherungsstand
        {
            Ergebnis = new(false, false, "Die Anreicherung wurde verweigert; es ist keine Datei entstanden.", new[]
            {
                new GebaeudeExportMeldung(WarnStufe.Fehler, "Fehler", "Entität fehlt.", "GEXP_PROT_ANR_ENTITAET_FEHLT"),
            }),
        };
        var cut = AufbauenMitAnreicherung(s, a);
        FormatIfc(cut);
        WegAnreichern(cut);
        Knopf(cut, "Originaldatei wählen…").Click();
        BeipackBestaetigen(cut);
        PlanBestaetigen(cut);
        Primaer(cut).Click();

        Assert.Single(a.Angereichert);
        Assert.Empty(s.Geschlossen);
        Assert.Equal(WarnStufe.Fehler, cut.Instance.MeldungStufe);
        Assert.Contains("keine Datei entstanden", cut.Instance.Meldung);
        Assert.Equal("GEXP_PROT_ANR_ENTITAET_FEHLT",
                     cut.Find(".epos-gebexport-meldungen tbody tr").GetAttribute("data-schluessel"));
    }

    [Fact]
    public void Die_Wegwahl_traegt_Texte_keine_Steuerwerte_auch_englisch()
    {
        var texte = new GebaeudeExportTexte
        {
            WegEigen = R.ResourceManager.GetString("GEXP_ANR_WEG_EIGEN", new System.Globalization.CultureInfo("en-US"))!,
            WegAnreichern = R.ResourceManager.GetString("GEXP_ANR_WEG_ANREICHERN", new System.Globalization.CultureInfo("en-US"))!,
            OriginalWaehlen = R.ResourceManager.GetString("GEXP_ANR_BTN_WAEHLEN", new System.Globalization.CultureInfo("en-US"))!,
        };
        var cut = AufbauenMitAnreicherung(new Stand(), new Anreicherungsstand(), texte);
        FormatIfc(cut);
        IReadOnlyList<string> beschriftungen = cut.FindAll(".epos-gebexport-dialog label").Select(l => l.TextContent.Trim()).ToList();
        Assert.Contains(beschriftungen, b => b.Contains("Write own file", StringComparison.Ordinal));
        Assert.Contains(beschriftungen, b => b.Contains("Enrich original file", StringComparison.Ordinal));
        foreach (string steuer in new[] { "0", "1", "IFC", "GBXML" })
            Assert.DoesNotContain(steuer, beschriftungen);
        WegAnreichern(cut);
        Assert.Equal("Choose original file…", cut.Find(".epos-gebexport-originalknopf").TextContent.Trim());
        Assert.True(Formate().Single(f => f.Wert == "IFC").MitAnreicherung);
        Assert.False(Formate().Single(f => f.Wert == "GBXML").MitAnreicherung);
    }

    /// <summary>Die Texte der Anreicherung stehen in beiden Sprachen, nicht leer und übersetzt.</summary>
    [Theory]
    [InlineData("GEXP_ANR_LBL_WEG")]
    [InlineData("GEXP_ANR_WEG_EIGEN")]
    [InlineData("GEXP_ANR_WEG_ANREICHERN")]
    [InlineData("GEXP_ANR_HINWEIS")]
    [InlineData("GEXP_ANR_BTN_WAEHLEN")]
    [InlineData("GEXP_ANR_DATEIDIALOG_TITEL")]
    [InlineData("GEXP_ANR_LBL_DATEI")]
    [InlineData("GEXP_ANR_LBL_QUELLE")]
    [InlineData("GEXP_ANR_QUELLE")]
    [InlineData("GEXP_ANR_KEINE_QUELLE")]
    [InlineData("GEXP_ANR_VERWEIGERT")]
    [InlineData("GEXP_ANR_BTN_EIGENE")]
    [InlineData("GEXP_ANR_BESTAETIGEN")]
    [InlineData("GEXP_ANR_SPERRE_BESTAETIGEN")]
    [InlineData("GEXP_ANR_SPERRE_DATEI")]
    [InlineData("GEXP_ANR_MSG_LESEFEHLER")]
    [InlineData("GEXP_ANR_MSG_NICHTS_GESCHRIEBEN")]
    [InlineData("GEXP_ANR_MSG_GESPEICHERT")]
    public void Die_Texte_der_Anreicherung_stehen_in_beiden_Sprachen(string schluessel)
        => Die_Texte_der_Formatwahl_stehen_in_beiden_Sprachen(schluessel);
}
