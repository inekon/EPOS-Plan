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
/// Die Überlagerung „Tagesgang bearbeiten" des Zapfprofils (Umsetzungskonzept
/// Zapfprofilgenerator 5.1, 5.3, 5.7; Stufe Z4, Gruppe 2b): 24 Stundenfelder je Tagtyp, der
/// Tagtypwechsel, die Summenkontrollen samt Vorschau der Normierung, Tag kopieren/einfügen,
/// Vorlage und Zurücksetzen, die gesperrte Nutzungsart nur als Kopie, der Rückweg (OK mit
/// Ergebnis, ohne Änderung und Abbrechen <c>null</c>, Ablehnung als Banner) und der Fall ohne
/// Gaben — dazu die Sicht des Assistenten.
///
/// <para>Der Schreibweg kommt aus einem Prüfdelegaten — der Editor schreibt nicht selbst. Kultur
/// de-DE, alle Werte erfunden.</para>
/// </summary>
public class TagesgangEditorTests : EposBunitContext
{
    public TagesgangEditorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // =================================================================================
    // Prüfdaten (erfunden)
    // =================================================================================

    /// <summary>Ein Satz, der je Tagtyp die Hälfte auf zwei Stunden legt (Werktag 7/19, Samstag 9/20 …).</summary>
    private static ZapfprofilTagesgangsatzDaten Satz(int id, string name, int versatz = 0, bool waehlbar = true)
    {
        var s = new ZapfprofilTagesgangsatzDaten { Id = id, Name = name, Status = "eigen", Waehlbar = waehlbar,
                                                   Sperrgrund = waehlbar ? "" : "unvollständig" };
        for (int t = 0; t < 4; t++)
        {
            s.Anteile[t][6 + 2 * t + versatz] = 0.5;
            s.Anteile[t][18 + t] = 0.5;
            s.Herkunft[t] = "Testquelle " + (t + 1);
        }
        return s;
    }

    private static ZapfprofilTagesgangDaten Daten(bool kopie = false) => new()
    {
        IdNutzungsart = 7,
        Nutzungsart = "Wohnen A · TEST-1",
        Satz = Satz(1, "Testsatz · TEST-1"),
        Wochenfaktoren = new[] { 0.2, 0.2, 0.2, 0.2, 0.2, 0.0, 0.0 },
        WochengangHerkunft = "Testquelle Woche",
        Vorlagen = { Satz(1, "Testsatz · TEST-1"), Satz(2, "Anderer Satz · TEST-1", versatz: 1), Satz(3, "Halber Satz · TEST-1", waehlbar: false) },
        Kopie = kopie,
        Sperrgrund = kopie ? "Die Nutzungsart gehört zur Auslieferung — Kopie." : "",
        KatalogversionVorschlag = kopie ? "TEST-1-E1" : ""
    };

    private IRenderedComponent<TagesgangEditor> Aufbauen(
        ZapfprofilTagesgangDaten? daten = null,
        Func<ZapfprofilTagesgangEingabeDaten, ZapfprofilTagesgangErgebnis>? speichern = null,
        Action<ZapfprofilTagesgangErgebnis?>? geschlossen = null)
        => Render<TagesgangEditor>(p => p
            .Add(x => x.Daten, daten ?? Daten())
            .Add(x => x.Texte, new ZapfprofilTexte())
            .Add(x => x.Speichern, speichern)
            .Add(x => x.Geschlossen, e => geschlossen?.Invoke(e)));

    private static IReadOnlyList<IElement> Stundenfelder(IRenderedComponent<TagesgangEditor> cut)
        => cut.FindAll(".epos-zapfprofil-tagesgangraster input");

    private static IElement Knopf(IRenderedComponent<TagesgangEditor> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static void Tagtyp(IRenderedComponent<TagesgangEditor> cut, string eintrag, int wert)
        => cut.Find("fieldset[aria-label='Tagtyp']").QuerySelectorAll("label.epos-option")
              .First(l => l.TextContent.Trim() == eintrag).QuerySelector("input")!.Change(wert.ToString());

    private static IElement Ok(IRenderedComponent<TagesgangEditor> cut) => Knopf(cut, "OK");

    // =================================================================================
    // Anzeige
    // =================================================================================

    [Fact]
    public void Der_Editor_zeigt_24_Stunden_Tagtypen_Wochenfaktoren_und_die_Summe_in_Ordnung()
    {
        var cut = Aufbauen();

        Assert.Equal("Tagesgang bearbeiten", cut.Find("h1.epos-dialog-titel").TextContent);
        Assert.Contains("Wohnen A · TEST-1", cut.Find(".epos-kontextzeile").TextContent);
        Assert.Contains("Testsatz · TEST-1", cut.Find(".epos-kontextzeile").TextContent);
        Assert.Equal(24, Stundenfelder(cut).Count);
        Assert.Equal(4, cut.Find("fieldset[aria-label='Tagtyp']").QuerySelectorAll("input").Length);
        Assert.Contains("Ruhetag", cut.Find("fieldset[aria-label='Tagtyp']").TextContent);
        Assert.Equal(50.0, cut.Instance.Stunden[6]);
        Assert.Equal(20.0, cut.Instance.Woche[0]);
        Assert.Contains("Herkunft: Testquelle 1", cut.Markup);
        Assert.Equal(2, cut.FindAll(".epos-kohaerenz--ok").Count);
        Assert.Contains("summiert zu 100 %", cut.Markup);
        Assert.Empty(cut.FindAll(".epos-zapfprofil-normiert"));
        Assert.Contains("„OK“ schreibt an Ort und Stelle", cut.Markup);
        Assert.Empty(cut.FindAll(".epos-zapfprofil-editorsperre"));
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_der_Editor_leer_und_OK_schliesst_ohne_Schreibweg()
    {
        ZapfprofilTagesgangErgebnis? ergebnis = null;
        int gerufen = 0;
        var cut = Render<TagesgangEditor>(p => p.Add(x => x.Geschlossen, e => { gerufen++; ergebnis = e; }));

        Assert.Equal(24, Stundenfelder(cut).Count);
        Assert.Contains("Summe 0 %", cut.Markup);
        Ok(cut).Click();
        Assert.Equal(1, gerufen);
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Der_Tagtypwechsel_zeigt_die_Werte_des_anderen_Tagtyps()
    {
        var cut = Aufbauen();
        Tagtyp(cut, "Samstag", 1);

        Assert.Equal(1, cut.Instance.Tagtyp);
        Assert.Equal(50.0, cut.Instance.Stunden[8]);
        Assert.Equal(0.0, cut.Instance.Stunden[6]);
        Assert.Contains("Herkunft: Testquelle 2", cut.Markup);
        Assert.Equal("9", cut.FindAll(".epos-zapfprofil-tagesgangraster label")[8].QuerySelector(".epos-feld-text")!.TextContent.Trim());
    }

    // =================================================================================
    // Summenkontrolle und Normierung
    // =================================================================================

    [Fact]
    public void Eine_Summe_neben_100_Prozent_meldet_rot_zeigt_die_Normierung_und_Normieren_setzt_sie()
    {
        var cut = Aufbauen();
        Stundenfelder(cut)[6].Input("25");       // Werktag: 25 + 50 = 75 %

        IElement abweichend = cut.Find(".epos-kohaerenz--abweichend");
        Assert.Contains("Summe 75,00 % — korrigieren oder normieren", abweichend.TextContent);
        IElement vorschau = cut.Find(".epos-zapfprofil-normiert");
        Assert.Contains("7: 33,3", vorschau.TextContent);
        Assert.Contains("19: 66,7", vorschau.TextContent);

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Normieren").Click();
        Assert.Equal(100.0, cut.Instance.Stunden.Sum(w => w ?? 0), 9);
        Assert.Equal(100.0 / 3.0, cut.Instance.Stunden[6]!.Value, 9);
        Assert.Empty(cut.FindAll(".epos-zapfprofil-normiert"));
    }

    [Fact]
    public void Tag_kopieren_und_einfuegen_uebertragen_die_Stunden_eines_Tagtyps()
    {
        var cut = Aufbauen();
        IElement einfuegen = Knopf(cut, "Tag einfügen");
        Assert.Equal("true", einfuegen.GetAttribute("aria-disabled"));
        einfuegen.Click();
        Assert.Equal("Zuerst einen Tag kopieren.", cut.Instance.Hinweis);

        Knopf(cut, "Tag kopieren").Click();
        Tagtyp(cut, "Ruhetag", 3);
        Assert.Equal(0.0, cut.Instance.Stunden[6]);
        Knopf(cut, "Tag einfügen").Click();
        Assert.Equal(50.0, cut.Instance.Stunden[6]);
        Assert.Equal(50.0, cut.Instance.Stunden[18]);
    }

    [Fact]
    public void Vorlage_laden_und_Zuruecksetzen_ersetzen_die_vier_Tagesgaenge()
    {
        var cut = Aufbauen();
        IElement laden = Knopf(cut, "Vorlage laden");
        Assert.Equal("true", laden.GetAttribute("aria-disabled"));

        IElement wahl = cut.FindAll("label").First(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == "Tagesgangsatz")
                           .QuerySelector("select")!;
        IElement halb = wahl.QuerySelectorAll("option").First(o => o.TextContent.StartsWith("Halber Satz"));
        Assert.True(halb.HasAttribute("disabled"));
        wahl.Change("2");
        Knopf(cut, "Vorlage laden").Click();
        Assert.Equal(0.0, cut.Instance.Stunden[6]);
        Assert.Equal(50.0, cut.Instance.Stunden[7]);
        Assert.Equal(20.0, cut.Instance.Woche[0]);          // die Wochenfaktoren bleiben

        Knopf(cut, "Zurücksetzen").Click();
        Assert.Equal(50.0, cut.Instance.Stunden[6]);
        Assert.Equal(0.0, cut.Instance.Stunden[7]);
    }

    // =================================================================================
    // Gesperrte Nutzungsart: nur als Kopie
    // =================================================================================

    [Fact]
    public void Eine_gesperrte_Nutzungsart_zeigt_nur_lesbar_mit_Schloss_und_gibt_die_Kopie_frei()
    {
        var cut = Aufbauen(Daten(kopie: true));

        Assert.Contains("Die Nutzungsart gehört zur Auslieferung — Kopie.", cut.Find(".epos-zapfprofil-editorsperre").TextContent);
        Assert.NotNull(cut.Find(".epos-zapfprofil-editorsperre .epos-schloss"));
        Assert.All(Stundenfelder(cut), f => Assert.True(f.HasAttribute("disabled")));
        IElement normieren = cut.FindAll("button").First(b => b.TextContent.Trim() == "Normieren");
        Assert.Equal("true", normieren.GetAttribute("aria-disabled"));
        normieren.Click();
        Assert.Equal("Nur lesbar — bearbeiten lässt sich eine eigene Kopie.", cut.Instance.Hinweis);

        Knopf(cut, "Als eigene Kopie bearbeiten…").Click();
        Assert.All(Stundenfelder(cut), f => Assert.False(f.HasAttribute("disabled")));
        IElement version = cut.FindAll("label").First(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == "Katalogversion der Kopie")
                              .QuerySelector("input")!;
        Assert.Equal("TEST-1-E1", version.GetAttribute("value"));
        Assert.Contains("legt die Nutzungsart unter dieser Katalogversion neu an", cut.Markup);
    }

    // =================================================================================
    // Rückweg
    // =================================================================================

    [Fact]
    public void OK_uebernimmt_als_Anwenderkopie_ueber_den_Schreibweg_und_gibt_das_Ergebnis_zurueck()
    {
        ZapfprofilTagesgangEingabeDaten? gesendet = null;
        ZapfprofilTagesgangErgebnis? ergebnis = null;
        var cut = Aufbauen(Daten(kopie: true),
                           e => { gesendet = e; return new ZapfprofilTagesgangErgebnis(true, 70, 71, true, null); },
                           e => ergebnis = e);
        Knopf(cut, "Als eigene Kopie bearbeiten…").Click();
        Stundenfelder(cut)[0].Input("10");
        Ok(cut).Click();

        Assert.NotNull(gesendet);
        Assert.Equal(7, gesendet!.IdNutzungsart);
        Assert.Equal("TEST-1-E1", gesendet.Katalogversion);
        Assert.Equal(10.0, gesendet.TagesgaengeProzent[0][0]);
        Assert.Equal(50.0, gesendet.TagesgaengeProzent[0][6]);
        Assert.Equal(20.0, gesendet.WochenfaktorenProzent[0]);
        Assert.NotNull(ergebnis);
        Assert.Equal(70, ergebnis!.IdNutzungsart);
        Assert.True(ergebnis.NeueZeile);
    }

    /// <summary>
    /// Z4, Gruppe 2b Punkt 1: Ein unveränderter Tagtyp zeigt seine Prozente unverändert und trägt
    /// im Rückweg die Originalanteile bitgleich mit — die Hülle reicht sie durch, statt sie über
    /// den Umweg Prozent neu zu berechnen (sonst gälte eine unveränderte, nicht runde Reihe als
    /// geändert). Ein geänderter Tagtyp trägt keine Originalanteile.
    /// </summary>
    [Fact]
    public void Eine_unveraenderte_Reihe_zeigt_ihre_Prozente_und_traegt_die_Originalanteile_im_Rueckweg()
    {
        ZapfprofilTagesgangEingabeDaten? gesendet = null;
        var cut = Aufbauen(speichern: e => { gesendet = e; return new ZapfprofilTagesgangErgebnis(true, 7, 1, false, null); });

        Assert.Equal(50.0, cut.Instance.Stunden[6]);
        Assert.Equal(50.0, cut.Instance.Stunden[18]);

        // Nur eine Stunde des Werktags ändern — die anderen Tagtypen und die Woche bleiben unverändert.
        Stundenfelder(cut)[0].Input("10");
        Ok(cut).Click();

        Assert.NotNull(gesendet);
        ZapfprofilTagesgangsatzDaten stand = Daten().Satz;
        Assert.Null(gesendet!.TagesgaengeOriginal[0]);                        // Werktag geändert
        Assert.Equal(stand.Anteile[1], gesendet.TagesgaengeOriginal[1]);      // Samstag unverändert
        Assert.Equal(stand.Anteile[2], gesendet.TagesgaengeOriginal[2]);      // Sonn-/Feiertag unverändert
        Assert.Equal(stand.Anteile[3], gesendet.TagesgaengeOriginal[3]);      // Ruhetag unverändert
        Assert.Equal(Daten().Wochenfaktoren, gesendet.WochenfaktorenOriginal);
        Assert.Null(gesendet.Vorlage);
    }

    /// <summary>
    /// Z4, Gruppe 2b Punkt 1: „Vorlage laden" gibt die Id der Vorlage mit dem Rückweg mit, damit
    /// eine dadurch geänderte Reihe im Kern deren Herkunft statt Eigenkonstruktion tragen kann.
    /// </summary>
    [Fact]
    public void Vorlage_laden_schickt_ihre_Id_zur_Herkunftszuordnung_im_Rueckweg()
    {
        ZapfprofilTagesgangEingabeDaten? gesendet = null;
        var cut = Aufbauen(speichern: e => { gesendet = e; return new ZapfprofilTagesgangErgebnis(true, 7, 1, false, null); });

        IElement wahl = cut.FindAll("label").First(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == "Tagesgangsatz")
                           .QuerySelector("select")!;
        wahl.Change("2");
        Knopf(cut, "Vorlage laden").Click();
        Ok(cut).Click();

        Assert.NotNull(gesendet);
        Assert.Equal(2, gesendet!.Vorlage);
    }

    [Fact]
    public void Eine_Ablehnung_bleibt_als_Banner_und_der_Editor_offen()
    {
        int geschlossen = 0;
        var meldung = new ZapfprofilMeldung("ZPG_TGE_GRUND_NAME_BELEGT", "", "Der Tagesgang wurde nicht gespeichert — belegt.",
                                            ZapfprofilMeldungsart.Fehler);
        var cut = Aufbauen(speichern: _ => new ZapfprofilTagesgangErgebnis(false, 7, 0, false, meldung),
                           geschlossen: _ => geschlossen++);
        Stundenfelder(cut)[0].Input("5");
        Ok(cut).Click();

        Assert.Equal(0, geschlossen);
        Assert.Equal("ZPG_TGE_GRUND_NAME_BELEGT", cut.Instance.Meldung?.Kennung);
        Assert.Contains("Der Tagesgang wurde nicht gespeichert — belegt.", cut.Markup);
    }

    [Fact]
    public void OK_ohne_Aenderung_und_Abbrechen_schreiben_nichts()
    {
        int gespeichert = 0, gerufen = 0;
        ZapfprofilTagesgangErgebnis? ergebnis = new(true, 0, 0, false, null);
        var cut = Aufbauen(speichern: _ => { gespeichert++; return new ZapfprofilTagesgangErgebnis(true, 7, 1, false, null); },
                           geschlossen: e => { gerufen++; ergebnis = e; });
        Ok(cut).Click();
        Assert.Equal(0, gespeichert);
        Assert.Equal(1, gerufen);
        Assert.Null(ergebnis);

        Stundenfelder(cut)[0].Input("5");
        Knopf(cut, "Abbrechen").Click();
        Assert.Equal(0, gespeichert);
        Assert.Equal(2, gerufen);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(3, gerufen);
    }

    [Fact]
    public void Eine_Fehleingabe_haelt_OK_an_und_nennt_das_Feld()
    {
        int gespeichert = 0;
        var cut = Aufbauen(speichern: _ => { gespeichert++; return new ZapfprofilTagesgangErgebnis(true, 7, 1, false, null); });
        Stundenfelder(cut)[2].Input("x");
        Ok(cut).Click();

        Assert.Equal(0, gespeichert);
        Assert.Contains("Werktag · Stunde 3", cut.Instance.Meldung?.Text);
    }

    /// <summary>
    /// Z4, Gruppe 2b Punkt 2: Ein Tagtypwechsel entfernt die Fehleingabe des alten Tagtyps aus
    /// <c>_fehlfelder</c> — sonst hielte eine Fehleingabe, die der Anwender nie mehr sieht, „OK"
    /// dauerhaft an.
    /// </summary>
    [Fact]
    public void Ein_Tagtypwechsel_raeumt_die_Fehleingabe_des_alten_Tagtyps()
    {
        ZapfprofilTagesgangEingabeDaten? gesendet = null;
        var cut = Aufbauen(speichern: e => { gesendet = e; return new ZapfprofilTagesgangErgebnis(true, 7, 1, false, null); });

        Stundenfelder(cut)[2].Input("x");        // Werktag · Stunde 3: ungültig
        Tagtyp(cut, "Samstag", 1);                // Tagtypwechsel — das Feld wird nicht mehr gezeigt
        Stundenfelder(cut)[0].Input("10");        // eine echte Änderung im neuen Tagtyp

        Ok(cut).Click();

        Assert.NotNull(gesendet);                 // OK schreibt statt einer hängenden Fehlermeldung
        Assert.Null(cut.Instance.Meldung);
    }

    // =================================================================================
    // Der Hilfe-Assistent
    // =================================================================================

    private static KiFeldzugang Zugang(string feld) => KiMaskenbruecke.Feldzugang(KiMaskennamen.TAGESGANG_EDITOR, feld)!;

    [Fact]
    public void Der_Editor_meldet_sich_an_und_setzt_wie_die_Felder_nur_wo_sie_bedienbar_sind()
    {
        KiMaskenbruecke.Leeren();
        var cut = Aufbauen(Daten(kopie: true));
        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.TAGESGANG_EDITOR));

        Assert.Equal(0, Zugang("tagtyp").Lesen());
        Zugang("tagtyp").Setzen(2);
        Assert.Equal(2, cut.Instance.Tagtyp);

        // Gesperrt: die Stunden sind erst nach „Als eigene Kopie bearbeiten…" setzbar.
        var reihe = Enumerable.Repeat((double?)(100.0 / 24.0), 24).ToArray();
        var gesperrt = Assert.Throws<InvalidOperationException>(() => Zugang("stunden").Setzen(reihe));
        Assert.Contains("Als eigene Kopie bearbeiten", gesperrt.Message);
        Assert.Throws<InvalidOperationException>(() => Zugang("katalogversion").Setzen("X"));

        Knopf(cut, "Als eigene Kopie bearbeiten…").Click();
        Zugang("stunden").Setzen(reihe);
        Assert.Equal(100.0 / 24.0, cut.Instance.Stunden[0]!.Value, 9);
        Assert.Throws<InvalidOperationException>(() => Zugang("wochenfaktoren").Setzen(new double?[] { 1, 2 }));
        Zugang("wochenfaktoren").Setzen(new double?[] { 10, 10, 10, 10, 10, 25, 25 });
        Assert.Equal(25.0, cut.Instance.Woche[6]);
        Zugang("katalogversion").Setzen("TEST-1-E9");
        Assert.Equal("TEST-1-E9", Zugang("katalogversion").Lesen());

        // Eine unvollständige Vorlage nennt ihren Grund.
        var halb = Assert.Throws<InvalidOperationException>(() => Zugang("vorlage").Setzen(3));
        Assert.Equal("unvollständig", halb.Message);
        Zugang("vorlage").Setzen(2);
        Assert.Equal(2, Zugang("vorlage").Lesen());

        cut.Instance.Dispose();
        Assert.False(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.TAGESGANG_EDITOR));
    }
}
