using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Zapfprofil-Dialog als WIRT der Editoren der Stufe Experte (Umsetzungskonzept
/// Zapfprofilgenerator 5.1, 5.3; Stufe Z4, Gruppe 2b): „Tagesgang bearbeiten…" und „Zapfkategorien
/// und Streuung…" öffnen ihre Überlagerung zur Nutzungsart der gewählten Zone, Esc schließt nur
/// sie, ein geschriebener Editor stellt die Zone auf die Nutzungsart, die jetzt die Werte trägt,
/// und liest den Katalog neu; dazu der Vorschlag der Ladeleistung (4.7) am Feld der Stufe Erweitert.
///
/// <para>Alle Delegaten sind Prüfdelegaten — der Dialog rechnet und schreibt nicht. Kultur de-DE,
/// Werte erfunden.</para>
/// </summary>
public partial class ZapfprofilDialogStufenTests
{
    /// <summary>Der Katalog nach dem Schreibweg: dazu die Anwenderkopie 99 „Wohnen A · TEST-1-E1".</summary>
    private static ZapfprofilKatalogstandDaten NeuerKatalog()
    {
        ZapfprofilNutzungsartDaten kopie = Art(99, "Wohnen A", wohnen: true);
        kopie.Katalogversion = "TEST-1-E1";
        return new ZapfprofilKatalogstandDaten
        {
            Katalog = { Art(1, "Wohnen A", wohnen: true), Art(2, "Büro B", wohnen: false), kopie },
            Tagesgangsaetze = { new ZapfprofilKatalogeintragDaten { Id = 1, Name = "Testsatz · TEST-1" },
                                new ZapfprofilKatalogeintragDaten { Id = 5, Name = "Testsatz · TEST-1-E1" } }
        };
    }

    /// <summary>Ein freier Tagesgang: in jedem Tagtyp alles in Stunde 7, Woche gleich verteilt.</summary>
    private static ZapfprofilTagesgangDaten Tagesgangstand()
    {
        var satz = new ZapfprofilTagesgangsatzDaten { Id = 1, Name = "Testsatz · TEST-1" };
        for (int t = 0; t < ZapfprofilTagesgangsatzDaten.TAGTYPEN; t++) satz.Anteile[t][6] = 1.0;
        return new ZapfprofilTagesgangDaten
        {
            IdNutzungsart = 1,
            Nutzungsart = "Wohnen A · TEST-1",
            Satz = satz,
            Wochenfaktoren = Enumerable.Repeat(1.0 / 7.0, 7).ToArray()
        };
    }

    private (int Nutzungsart, int? Satz)? _tagesgangGeoeffnet;
    private int? _kategorienGeoeffnet;

    /// <summary>Der Dialog mit den Delegaten der Editoren wie die Hülle: jeder Schreibweg legt die Kopie 99 an.</summary>
    private IRenderedComponent<ZapfprofilDialog> MitEditoren(
        ZapfprofilDaten? daten = null,
        Action<ZapfprofilErgebnisDaten?>? geschlossen = null,
        Func<ZapfprofilEingabeDaten, ZapfprofilSchaetzhilfeDaten?>? ladevorschlag = null)
        => Render<ZapfprofilDialog>(p => p
            .Add(x => x.Daten, daten ?? Daten())
            .Add(x => x.Texte, new ZapfprofilTexte())
            .Add(x => x.Vorschau, Vorschau)
            .Add(x => x.Pruefen, _ => Array.Empty<ZapfprofilMeldung>())
            .Add(x => x.EntprellungMs, 0)
            .Add(x => x.TagesgangGaben, (n, s) =>
            {
                _tagesgangGeoeffnet = (n, s);
                return new Dictionary<string, object>
                {
                    ["Daten"] = Tagesgangstand(),
                    ["Texte"] = new ZapfprofilTexte(),
                    ["Speichern"] = new Func<ZapfprofilTagesgangEingabeDaten, ZapfprofilTagesgangErgebnis>(
                        _ => new ZapfprofilTagesgangErgebnis(true, 99, 5, true, null))
                };
            })
            .Add(x => x.KategorienGaben, n =>
            {
                _kategorienGeoeffnet = n;
                return new Dictionary<string, object>
                {
                    ["Daten"] = new ZapfprofilKategorienEditorDaten
                    {
                        Nutzungsart = "Wohnen A · TEST-1",
                        Stand = new ZapfprofilKategorienDaten
                        {
                            IdNutzungsart = n,
                            Frei = true,
                            Kategorien = { new ZapfprofilKategorieDaten { Name = "Kurz", VolumenstromLJeMin = 1, DauerMin = 1, Anteil = 1, StreuungLJeMin = 0.5 } }
                        }
                    },
                    ["Texte"] = new ZapfprofilTexte(),
                    ["Pruefen"] = new Func<IReadOnlyList<ZapfprofilKategorieDaten>, ZapfprofilKategorienPruefungDaten>(
                        _ => new ZapfprofilKategorienPruefungDaten { SummeAnteil = 1 }),
                    ["Speichern"] = new Func<IReadOnlyList<ZapfprofilKategorieDaten>, string, ZapfprofilKategorienErgebnis>(
                        (_, _) => new ZapfprofilKategorienErgebnis(true, 99, true, null))
                };
            })
            .Add(x => x.Katalogstand, NeuerKatalog)
            .Add(x => x.Ladevorschlag, ladevorschlag)
            .Add(x => x.Geschlossen, e => geschlossen?.Invoke(e)));

    [Fact]
    public void Tagesgang_bearbeiten_oeffnet_die_Ueberlagerung_zur_Zone_und_Esc_schliesst_nur_sie()
    {
        int geschlossen = 0;
        var cut = MitEditoren(geschlossen: _ => geschlossen++);
        Stufe(cut, "Experte");

        IElement knopf = Knopf(cut, "Tagesgang bearbeiten…");
        Assert.Null(knopf.GetAttribute("aria-disabled"));
        knopf.Click();
        Assert.True(cut.Instance.TagesgangOffen);
        Assert.Equal((1, (int?)null), _tagesgangGeoeffnet);
        Assert.NotNull(cut.Find(".epos-ueberlagerung .epos-zapfprofil-tagesgang"));

        // Esc am Wirt schließt den Wirt nicht, solange die Überlagerung steht; Esc im Editor schließt nur sie.
        cut.Find(".epos-zapfprofil").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(0, geschlossen);
        Assert.True(cut.Instance.TagesgangOffen);
        cut.Find(".epos-zapfprofil-tagesgang").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(cut.Instance.TagesgangOffen);
        Assert.Equal(0, geschlossen);
        Assert.Equal(1, cut.Instance.Eingabe.Zonen[0].IdNutzungsart);
        Assert.Empty(cut.FindAll(".epos-zapfprofil-editorstatus"));
    }

    [Fact]
    public void Ein_geschriebener_Tagesgang_stellt_die_Zone_auf_die_neue_Katalogversion()
    {
        var cut = MitEditoren();
        Stufe(cut, "Experte");
        Knopf(cut, "Tagesgang bearbeiten…").Click();
        cut.FindAll(".epos-zapfprofil-tagesgangraster input")[0].Input("5");
        cut.Find(".epos-zapfprofil-tagesgang").QuerySelectorAll("button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.False(cut.Instance.TagesgangOffen);
        Assert.Equal(99, cut.Instance.Eingabe.Zonen[0].IdNutzungsart);
        Assert.Contains(cut.Instance.KatalogAktuell, n => n.Id == 99);
        Assert.Equal("Tagesgang gespeichert — die Zone „Wohnen“ rechnet mit „Wohnen A · TEST-1-E1“.",
                     cut.Find(".epos-zapfprofil-editorstatus").TextContent.Trim());
        // Die zweite Zone bleibt, wie sie war.
        Assert.Equal(2, cut.Instance.Eingabe.Zonen[1].IdNutzungsart);
    }

    [Fact]
    public void Die_Zapfkategorien_oeffnen_zur_Zone_und_der_Schreibweg_stellt_die_Zone_um()
    {
        var cut = MitEditoren();
        Stufe(cut, "Experte");
        Knopf(cut, "Zapfkategorien und Streuung…").Click();
        Assert.True(cut.Instance.KategorienOffen);
        Assert.Equal(1, _kategorienGeoeffnet);

        cut.FindAll("label").First(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == "Streuung σ [l/min], Zeile 1")
           .QuerySelector("input")!.Input("0,7");
        cut.Find(".epos-zapfprofil-kategorien").QuerySelectorAll("button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.False(cut.Instance.KategorienOffen);
        Assert.Equal(99, cut.Instance.Eingabe.Zonen[0].IdNutzungsart);
        Assert.Equal("Zapfkategorien gespeichert — die Zone „Wohnen“ rechnet mit „Wohnen A · TEST-1-E1“.",
                     cut.Find(".epos-zapfprofil-editorstatus").TextContent.Trim());
    }

    [Fact]
    public void Ohne_Nutzungsart_der_Zone_stehen_die_Editoren_weich_gesperrt_mit_Grund()
    {
        ZapfprofilDaten daten = Daten();
        daten.Eingabe.Zonen[0].IdNutzungsart = 0;
        var cut = MitEditoren(daten);
        Stufe(cut, "Experte");

        IElement knopf = Knopf(cut, "Zapfkategorien und Streuung…");
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Equal("Zuerst eine Nutzungsart wählen.", knopf.GetAttribute("title"));
        knopf.Click();
        Assert.False(cut.Instance.KategorienOffen);
        Assert.Null(_kategorienGeoeffnet);
        Assert.Equal("Zuerst eine Nutzungsart wählen.", cut.Instance.Hinweis);
    }

    [Fact]
    public void Der_Vorschlag_der_Ladeleistung_steht_ab_Erweitert_und_wird_der_manuelle_Wert()
    {
        int gerufen = 0;
        var cut = MitEditoren(ladevorschlag: _ =>
        {
            gerufen++;
            return new ZapfprofilSchaetzhilfeDaten { Auto = true, Vorschlag = 12.5, Angesetzt = 12.5, Einheit = "kW", Rechenweg = "Ladeweg der Probe" };
        });
        Assert.Equal(0, gerufen);                     // Einfach zeigt die Schätzhilfe nicht
        Stufe(cut, "Erweitert");
        Assert.True(gerufen >= 1);

        IElement zeile = cut.FindAll(".epos-vorschlagszeile").First(z => z.TextContent.Contains("Vorschlag: 12,5 kW"));
        Assert.Contains("Rechenweg: Ladeweg der Probe", cut.Markup);
        Assert.Contains("Angesetzt: 12,5 kW (auto)", cut.Markup);
        zeile.QuerySelector("button")!.Click();

        Assert.False(cut.Instance.Eingabe.Gebaeude!.LadeAuto);
        Assert.Equal(12.5, cut.Instance.Eingabe.Gebaeude.LadeManuellKw);
    }

    [Fact]
    public void Ohne_Delegat_des_Vorschlags_nennt_die_Ladeleistung_den_Grund()
    {
        var cut = MitEditoren();
        Stufe(cut, "Erweitert");
        Assert.Contains("Einen Vorschlag gibt es erst mit einer gerechneten Vorschau.", cut.Markup);
        Assert.Null(cut.Instance.Eingabe.Gebaeude?.LadeManuellKw);
    }
}
