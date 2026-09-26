using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Export;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

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
        internal readonly List<GebaeudeExportErgebnis?> Geschlossen = new();
        internal Func<string, GebaeudeExportAnsicht> Ansicht = Schreibbar;
        internal GebaeudeExportErgebnis Ergebnis = new(true, false, "Gespeichert: C:\\Probe\\haus.xml (1234 Byte).");
    }

    private IRenderedComponent<GebaeudeExportDialog> Aufbauen(Stand s, bool gespeicherterStand = false,
                                                              GebaeudeExportTexte? texte = null)
        => Render<GebaeudeExportDialog>(p => p
            .Add(x => x.Vorbereiten, plz => { s.Vorbereitet.Add(plz); return Task.FromResult(s.Ansicht(plz)); })
            .Add(x => x.Speichern, plz => { s.Gespeichert.Add(plz); return Task.FromResult(s.Ergebnis); })
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
        Assert.Contains("Daten ohne Geometrie", kopf);
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

        Assert.Contains("Gebäude exportieren (gbXML)", cut.Find(".epos-dialog-titel").TextContent);
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
}
