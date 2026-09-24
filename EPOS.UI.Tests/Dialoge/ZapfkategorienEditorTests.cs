using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Überlagerung „Zapfkategorien und Streuung" des Zapfprofils (Umsetzungskonzept
/// Zapfprofilgenerator 4.4, 5.3, 5.7; Stufe Z4, Gruppe 2b): das Raster mit Reihenfolge und den
/// sechs Werten, der Vorgabesatz, die Regeln des Kerns (Ablehnung hält OK an, Σ ≠ 1 nur ein
/// Hinweis), eine gesperrte Nutzungsart nur lesbar und erst als Kopie bearbeitbar, der Rückweg und
/// der Fall ohne Gaben — dazu die Sicht des Assistenten.
///
/// <para>Prüfung und Schreibweg kommen aus Prüfdelegaten, die die Regeln des Kerns nachstellen
/// (Summe 0 ist ein Verstoß, Σ ≠ 1 ein Hinweis). Kultur de-DE, alle Werte erfunden.</para>
/// </summary>
public class ZapfkategorienEditorTests : EposBunitContext
{
    public ZapfkategorienEditorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // =================================================================================
    // Prüfdaten (erfunden)
    // =================================================================================

    private static ZapfprofilKategorieDaten Kategorie(string name, double v, int dauer, double anteil, double sigma, double? kappung = null)
        => new() { Name = name, VolumenstromLJeMin = v, DauerMin = dauer, Anteil = anteil, StreuungLJeMin = sigma,
                   KappungLJeMin = kappung, Herkunft = "Testquelle" };

    private static ZapfprofilKategorienEditorDaten Daten(bool frei = true) => new()
    {
        Nutzungsart = "Wohnen A · TEST-1",
        KatalogversionVorschlag = frei ? "" : "TEST-1-E1",
        Stand = new ZapfprofilKategorienDaten
        {
            IdNutzungsart = 7,
            Frei = frei,
            Sperrgrund = frei ? "" : "Die Nutzungsart gehört zur Auslieferung — Kopie.",
            SummeAnteil = 1.0,
            Kategorien = { Kategorie("Kurz", 1, 1, 0.6, 0.5), Kategorie("Dusche", 8, 5, 0.4, 2, kappung: 12) },
            Vorgabe = { Kategorie("V1", 2, 1, 0.5, 1), Kategorie("V2", 6, 4, 0.3, 1), Kategorie("V3", 10, 10, 0.2, 2) }
        }
    };

    /// <summary>Die Regeln des Kerns im Kleinen: Summe 0 lehnt ab, Σ ≠ 1 ist ein Hinweis.</summary>
    private static ZapfprofilKategorienPruefungDaten Pruefen(IReadOnlyList<ZapfprofilKategorieDaten> k)
    {
        double summe = k.Sum(x => x.Anteil ?? 0);
        return new ZapfprofilKategorienPruefungDaten
        {
            SummeAnteil = summe,
            Hinweis = summe > 0 && Math.Abs(summe - 1) > 1e-9 ? "Die Anteile summieren zu " + summe.ToString("0.###") + " statt 1; die Rechnung normiert sie." : "",
            Ablehnung = summe > 0 ? null : new ZapfprofilMeldung("ZPG_SATZ_KATEGORIE_ANTEIL_NULL", "",
                "Die Anteile der Zapfkategorien summieren zu 0 — mindestens eine Kategorie braucht einen Anteil.", ZapfprofilMeldungsart.Ablehnung)
        };
    }

    private IRenderedComponent<ZapfkategorienEditor> Aufbauen(
        ZapfprofilKategorienEditorDaten? daten = null,
        Func<IReadOnlyList<ZapfprofilKategorieDaten>, string, ZapfprofilKategorienErgebnis>? speichern = null,
        Action<ZapfprofilKategorienErgebnis?>? geschlossen = null)
        => Render<ZapfkategorienEditor>(p => p
            .Add(x => x.Daten, daten ?? Daten())
            .Add(x => x.Texte, new ZapfprofilTexte())
            .Add(x => x.Pruefen, Pruefen)
            .Add(x => x.Speichern, speichern)
            .Add(x => x.Geschlossen, e => geschlossen?.Invoke(e)));

    private static IElement Knopf(IRenderedComponent<ZapfkategorienEditor> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static IElement Feld(IRenderedComponent<ZapfkategorienEditor> cut, string bezeichnung)
        => cut.FindAll("label").First(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == bezeichnung).QuerySelector("input")!;

    // =================================================================================
    // Anzeige
    // =================================================================================

    [Fact]
    public void Das_Raster_zeigt_Reihenfolge_und_die_sechs_Werte_je_Kategorie()
    {
        var cut = Aufbauen();

        Assert.Equal("Zapfkategorien und Streuung", cut.Find("h1.epos-dialog-titel").TextContent);
        Assert.Contains("Wohnen A · TEST-1 · 2 Kategorien · Σ Anteile 1", cut.Find(".epos-kontextzeile").TextContent);
        string[] koepfe = cut.FindAll(".epos-zapfprofil-kategorienraster th").Select(t => t.TextContent.Trim()).ToArray();
        foreach (string s in new[] { "Reihenfolge", "Kategorie", "Volumenstrom μ [l/min]", "Dauer [min]", "Anteil [–]",
                                     "Streuung σ [l/min]", "Kappung [l/min]", "Herkunft", "Aktion" })
            Assert.Contains(koepfe, k => k.StartsWith(s, StringComparison.Ordinal));
        Assert.Equal("Dusche", Feld(cut, "Kategorie, Zeile 2").GetAttribute("value"));
        Assert.Equal("12", Feld(cut, "Kappung [l/min], Zeile 2").GetAttribute("value"));
        Assert.Equal("keine", Feld(cut, "Kappung [l/min], Zeile 1").GetAttribute("placeholder"));
        Assert.Contains("Testquelle", cut.Markup);
        Assert.Empty(cut.FindAll(".epos-zapfprofil-kategorienhinweis"));
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_der_Editor_leer_und_OK_schliesst_ohne_Schreibweg()
    {
        int gerufen = 0;
        ZapfprofilKategorienErgebnis? ergebnis = new(true, 1, false, null);
        var cut = Render<ZapfkategorienEditor>(p => p.Add(x => x.Geschlossen, e => { gerufen++; ergebnis = e; }));

        Assert.Contains("Die Nutzungsart führt keine Zapfkategorien", cut.Markup);
        Knopf(cut, "OK").Click();
        Assert.Equal(1, gerufen);
        Assert.Null(ergebnis);
    }

    // =================================================================================
    // Regeln, Vorgabesatz, Zeilenaktionen
    // =================================================================================

    [Fact]
    public void Der_Vorgabesatz_ersetzt_die_Zeilen_und_eine_Summe_neben_eins_ist_nur_ein_Hinweis()
    {
        IReadOnlyList<ZapfprofilKategorieDaten>? gesendet = null;
        var cut = Aufbauen(speichern: (k, v) => { gesendet = k; return new ZapfprofilKategorienErgebnis(true, 7, false, null); });
        Knopf(cut, "Vorgabesatz laden").Click();

        Assert.Equal(new[] { "V1", "V2", "V3" }, cut.Instance.Zeilen.Select(k => k.Name).ToArray());
        Feld(cut, "Anteil [–], Zeile 3").Input("0,1");
        Assert.Contains("Die Anteile summieren zu 0,9 statt 1", cut.Find(".epos-zapfprofil-kategorienhinweis").TextContent);
        Assert.Null(cut.Instance.Pruefung.Ablehnung);

        Knopf(cut, "OK").Click();
        Assert.NotNull(gesendet);
        Assert.Equal(0.1, gesendet![2].Anteil);
    }

    [Fact]
    public void Eine_Summe_null_ist_eine_benannte_Ablehnung_und_haelt_OK_an()
    {
        int gespeichert = 0, geschlossen = 0;
        var cut = Aufbauen(speichern: (_, _) => { gespeichert++; return new ZapfprofilKategorienErgebnis(true, 7, false, null); },
                           geschlossen: _ => geschlossen++);
        Feld(cut, "Anteil [–], Zeile 1").Input("0");
        Feld(cut, "Anteil [–], Zeile 2").Input("0");

        IElement verstoss = cut.Find(".epos-zapfprofil-kategorienverstoss");
        Assert.Equal("ZPG_SATZ_KATEGORIE_ANTEIL_NULL", verstoss.GetAttribute("data-kennung"));
        Knopf(cut, "OK").Click();
        Assert.Equal(0, gespeichert);
        Assert.Equal(0, geschlossen);
        Assert.Equal("ZPG_SATZ_KATEGORIE_ANTEIL_NULL", cut.Instance.Meldung?.Kennung);
        Assert.Contains("summieren zu 0", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Neu_Reihenfolge_und_Entfernen_wirken_die_letzte_Kategorie_bleibt()
    {
        var cut = Aufbauen();
        Knopf(cut, "Kategorie hinzufügen").Click();
        Assert.Equal("Kategorie 3", cut.Instance.Zeilen[2].Name);

        // „Nach oben" der ersten Zeile ist weich gesperrt; die dritte wandert nach oben.
        IElement hoch1 = cut.FindAll("button[aria-label='Nach oben, Zeile 1']").Single();
        Assert.Equal("true", hoch1.GetAttribute("aria-disabled"));
        hoch1.Click();
        Assert.Equal("Die Kategorie steht schon am Rand der Liste.", cut.Instance.Hinweis);
        cut.FindAll("button[aria-label='Nach oben, Zeile 3']").Single().Click();
        Assert.Equal(new[] { "Kurz", "Kategorie 3", "Dusche" }, cut.Instance.Zeilen.Select(k => k.Name).ToArray());

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Entfernen").Click();
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Entfernen").Click();
        Assert.Single(cut.Instance.Zeilen);
        IElement letzte = cut.FindAll("button").First(b => b.TextContent.Trim() == "Entfernen");
        Assert.Equal("true", letzte.GetAttribute("aria-disabled"));
        letzte.Click();
        Assert.Single(cut.Instance.Zeilen);
        Assert.StartsWith("Die letzte Kategorie bleibt", cut.Instance.Hinweis);
    }

    // =================================================================================
    // Gesperrte Nutzungsart: nur lesbar, erst als Kopie bearbeitbar
    // =================================================================================

    [Fact]
    public void Eine_gesperrte_Nutzungsart_zeigt_die_Zeilen_schreibgeschuetzt_und_speichert_als_Kopie()
    {
        string? version = null;
        ZapfprofilKategorienErgebnis? ergebnis = null;
        var cut = Aufbauen(Daten(frei: false),
                           (k, v) => { version = v; return new ZapfprofilKategorienErgebnis(true, 70, true, null); },
                           e => ergebnis = e);

        Assert.Contains("Die Nutzungsart gehört zur Auslieferung — Kopie.", cut.Find(".epos-zapfprofil-editorsperre").TextContent);
        Assert.Empty(cut.FindAll(".epos-zapfprofil-kategorienraster input"));
        Assert.NotEmpty(cut.FindAll(".epos-zapfprofil-kategorienraster .epos-schloss"));
        IElement neu = Knopf(cut, "Kategorie hinzufügen");
        Assert.Equal("true", neu.GetAttribute("aria-disabled"));
        neu.Click();
        Assert.Equal(2, cut.Instance.Zeilen.Count);

        Knopf(cut, "Als eigene Kopie bearbeiten…").Click();
        Assert.NotEmpty(cut.FindAll(".epos-zapfprofil-kategorienraster input"));
        Assert.Equal("TEST-1-E1", Feld(cut, "Katalogversion der Kopie").GetAttribute("value"));
        Feld(cut, "Streuung σ [l/min], Zeile 1").Input("0,8");
        Knopf(cut, "OK").Click();

        Assert.Equal("TEST-1-E1", version);
        Assert.Equal(70, ergebnis?.IdNutzungsart);
        Assert.True(ergebnis?.NeueZeile);
    }

    [Fact]
    public void Ohne_Aenderung_und_mit_Abbrechen_schreibt_der_Editor_nichts()
    {
        int gespeichert = 0, gerufen = 0;
        var cut = Aufbauen(speichern: (_, _) => { gespeichert++; return new ZapfprofilKategorienErgebnis(true, 7, false, null); },
                           geschlossen: _ => gerufen++);
        Knopf(cut, "OK").Click();
        Feld(cut, "Dauer [min], Zeile 1").Input("3");
        Knopf(cut, "Abbrechen").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal(0, gespeichert);
        Assert.Equal(3, gerufen);
    }

    [Fact]
    public void Eine_Ablehnung_des_Schreibwegs_bleibt_als_Banner()
    {
        var meldung = new ZapfprofilMeldung("ZPG_KATEG_GRUND_NAME_BELEGT", "", "Die Zapfkategorien wurden nicht gespeichert — belegt.",
                                            ZapfprofilMeldungsart.Fehler);
        int geschlossen = 0;
        var cut = Aufbauen(speichern: (_, _) => new ZapfprofilKategorienErgebnis(false, 7, false, meldung), geschlossen: _ => geschlossen++);
        Feld(cut, "Dauer [min], Zeile 1").Input("3");
        Knopf(cut, "OK").Click();

        Assert.Equal(0, geschlossen);
        Assert.Contains("belegt", cut.Find(".epos-warnbanner").TextContent);
    }

    // =================================================================================
    // Der Hilfe-Assistent
    // =================================================================================

    private static KiFeldzugang Zugang(string feld) => KiMaskenbruecke.Feldzugang(KiMaskennamen.ZAPFKATEGORIEN, feld)!;

    [Fact]
    public void Der_Editor_meldet_die_Kategorien_als_Spalten_an_und_setzt_nur_bedienbar()
    {
        KiMaskenbruecke.Leeren();
        var cut = Aufbauen(Daten(frei: false));
        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.ZAPFKATEGORIEN));

        Assert.Equal(1.0, Zugang("volumenstrom_1").Lesen());
        Assert.Contains("Dusche", Zugang("dauer_2").Feld.Anzeigename, StringComparison.Ordinal);
        var gesperrt = Assert.Throws<InvalidOperationException>(() => Zugang("anteil_1").Setzen(0.5));
        Assert.Contains("Als eigene Kopie bearbeiten", gesperrt.Message);

        Knopf(cut, "Als eigene Kopie bearbeiten…").Click();
        Zugang("anteil_1").Setzen(0.5);
        Assert.Equal(0.5, cut.Instance.Zeilen[0].Anteil);
        Assert.Throws<InvalidOperationException>(() => Zugang("dauer_1").Setzen(2000));
        Zugang("dauer_1").Setzen(2);
        Assert.Equal(2, cut.Instance.Zeilen[0].DauerMin);
        Zugang("katalogversion").Setzen("TEST-1-E5");
        Assert.Equal("TEST-1-E5", Zugang("katalogversion").Lesen());

        cut.Instance.Dispose();
        Assert.False(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.ZAPFKATEGORIEN));
    }
}
