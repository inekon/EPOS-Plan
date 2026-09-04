using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Klimadaten;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Klimadaten (iU9-W14c.7, Entscheid E-3). Soll ist die Feldkarte der gelöschten Maske
/// <c>Form_Klimadaten</c> (27 Steuerelemente, drei Ebenen tief): Regionsliste,
/// Löschknopf, Ortsfeld mit freier Eingabe, Longitude/Latitude/Bezeichnung, der
/// Importknopf mit Fortschrittsbalken, zwei Reiter mit je einem Diagramm und das
/// Detailfeld.
///
/// <para>Die Kultur ist auf de-DE gepinnt (Regel seit W8).</para>
/// </summary>
public class KlimadatenDialogTests : BunitContext
{
    private static readonly byte[] BILD = { 1, 2, 3, 4 };

    private static List<KlimadatenDialog.Regionszeile> Regionen() => new()
    {
        new("Berlin", false),
        new("Stuttgart", false),
        new("Auslieferung Nord", true)
    };

    public KlimadatenDialogTests()
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
    }

    private IRenderedComponent<KlimadatenDialog> Zeige(
        List<KlimadatenDialog.Regionszeile>? regionen = null,
        Func<string, Task<KlimadatenDialog.Regionsansicht>>? ansicht = null,
        Func<KlimaImportAuftrag, IProgress<ImportFortschritt>, Task<KlimaImportErgebnis>>? importieren = null,
        Action? abbrechen = null,
        Func<string, Task<bool>>? loeschen = null,
        IReadOnlyList<string>? orte = null,
        Action<bool>? geschlossen = null)
    {
        List<KlimadatenDialog.Regionszeile> liste = regionen ?? Regionen();
        return Render<KlimadatenDialog>(p => p
            .Add(x => x.Regionen, () => Task.FromResult(liste))
            .Add(x => x.Ansicht, ansicht ?? (n => Task.FromResult(
                new KlimadatenDialog.Regionsansicht("Details " + n, 9.18, 48.77, BILD, BILD, ""))))
            .Add(x => x.Importieren, importieren ?? ((_, _) => Task.FromResult(
                new KlimaImportErgebnis
                {
                    Ausgang = KlimaImportAusgang.Erfolg,
                    Bezeichner = "Berlin",
                    Stundenwerte = 8760,
                    Tageswerte = 365,
                    Meldung = "Die Klimaregion „Berlin\" ist angelegt."
                })))
            .Add(x => x.Abbrechen, abbrechen)
            .Add(x => x.Loeschen, loeschen ?? (_ => Task.FromResult(true)))
            .Add(x => x.Ortsvorschlaege, orte ?? Array.Empty<string>())
            .Add(x => x.Geschlossen, geschlossen ?? (_ => { })));
    }

    // =====================================================================
    //  Feldbestand (Feldkarte Form_Klimadaten)
    // =====================================================================

    [Fact]
    public void Der_Dialog_zeigt_Liste_Eingaben_und_zwei_Reiter()
    {
        var cut = Zeige();

        // Die drei Regionen der Liste.
        Assert.Equal(3, cut.FindAll("button.epos-anlagenwahl").Count);
        Assert.Contains("Stuttgart", cut.Markup);

        // Zwei Reiter mit je einem Bild.
        var reiter = cut.FindAll(".epos-reiter-knopf").Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Temperatur", "Sonnenwinkel" }, reiter);

        // Ortsfeld (freie Eingabe MIT Vorschlagsliste), zwei Zahlenfelder, Bezeichnung.
        Assert.Single(cut.FindAll("input[list=epos-klimaregion-orte]"));
        Assert.Equal(2, cut.FindAll("input[inputmode=decimal]").Count);

        // Loeschen, Daten einlesen, Beenden.
        var knoepfe = cut.FindAll("button").Select(e => e.TextContent.Trim()).ToList();
        Assert.Contains("Löschen", knoepfe);
        Assert.Contains("Daten einlesen", knoepfe);
        Assert.Contains("Beenden", knoepfe);
    }

    /// <summary>
    /// Befund W14c-B15 / Entscheid E-7: <b>Fehlt die Ortsliste, öffnet der Dialog
    /// trotzdem</b> — er zeigt dann keine Vorschläge, das Feld bleibt frei
    /// beschreibbar. Der Vorläufer warf in <c>Load</c> und öffnete gar nicht.
    /// </summary>
    [Fact]
    public void Ohne_Ortsliste_oeffnet_der_Dialog_und_das_Feld_bleibt_frei()
    {
        var cut = Zeige(orte: Array.Empty<string>());

        Assert.Empty(cut.FindAll("#epos-klimaregion-orte option"));
        Assert.False(cut.Find("input[list=epos-klimaregion-orte]").HasAttribute("readonly"));
    }

    [Fact]
    public void Mit_Ortsliste_stehen_die_Vorschlaege_da()
    {
        var cut = Zeige(orte: new[] { "Berlin", "Hamburg", "München" });

        var vorschlaege = cut.FindAll("#epos-klimaregion-orte option")
                             .Select(o => o.GetAttribute("value")).ToList();
        Assert.Equal(new[] { "Berlin", "Hamburg", "München" }, vorschlaege);
    }

    // =====================================================================
    //  Auswahl, Bilder und der Leerfall (Befunde W14c-B19/B22)
    // =====================================================================

    [Fact]
    public void Die_Auswahl_holt_Details_und_beide_Bilder()
    {
        string? gefragt = null;
        var cut = Zeige(ansicht: n =>
        {
            gefragt = n;
            return Task.FromResult(new KlimadatenDialog.Regionsansicht(
                "PVGIS-SARAH3", 13.4, 52.5, BILD, BILD, ""));
        });

        cut.FindAll("button.epos-anlagenwahl")[0].Click();

        Assert.Equal("Berlin", gefragt);
        Assert.Equal("Berlin", cut.Instance.Gewaehlt);
        Assert.Contains("PVGIS-SARAH3", cut.Markup);

        // Der Baustein Reiter zeichnet nur das AKTIVE Blatt - erst das
        // Temperaturbild, nach dem Wechsel das Sonnenwinkelbild.
        Assert.Single(cut.FindComponents<EPOS.UI.Standards.ChartBild>());
        Assert.Equal("Jahrestemperatur Verlauf",
                     cut.FindComponent<EPOS.UI.Standards.ChartBild>().Instance.Alt);

        cut.FindAll(".epos-reiter-knopf")[1].Click();
        Assert.Equal("Sonnenwinkel Verlauf",
                     cut.FindComponent<EPOS.UI.Standards.ChartBild>().Instance.Alt);
    }

    /// <summary>
    /// Befund W14c-B19: Eine Region ohne Stundenwerte meldet sich — der Vorläufer
    /// brach mit <c>InvalidOperationException</c> ab („Sequence contains no elements").
    /// </summary>
    [Fact]
    public void Eine_Region_ohne_Stundenwerte_meldet_sich_statt_abzubrechen()
    {
        var cut = Zeige(ansicht: _ => Task.FromResult(
            new KlimadatenDialog.Regionsansicht("", null, null, null, null,
                                                 "Für diese Region liegen keine Stundenwerte vor.")));

        cut.FindAll("button.epos-anlagenwahl")[0].Click();

        Assert.Contains("keine Stundenwerte", cut.Instance.Meldung);
    }

    // =====================================================================
    //  Loeschen - A-7 (Rueckfrage) und A-8 (Kaskade)
    // =====================================================================

    [Fact]
    public void Ohne_Auswahl_bleibt_Loeschen_gesperrt()
    {
        var cut = Zeige();

        var loeschen = cut.FindAll("button").First(b => b.TextContent.Trim() == "Löschen");
        Assert.True(loeschen.HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>Der Vorläufer löschte OHNE Rückfrage</b> (Befund W14c-B23) — und ohne die
    /// 8 760 + 365 Datenzeilen. Beides ist jetzt anders; die Frage sagt es.
    /// </summary>
    [Fact]
    public void Loeschen_fragt_zuerst_und_betont_Nein()
    {
        var geloescht = new List<string>();
        var cut = Zeige(loeschen: n => { geloescht.Add(n); return Task.FromResult(true); });

        cut.FindAll("button.epos-anlagenwahl")[0].Click();
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Löschen").Click();

        var frage = cut.FindComponent<EPOS.UI.Bausteine.Rueckfrage>();
        Assert.True(frage.Instance.Offen);
        Assert.True(frage.Instance.VorgabeNein);
        Assert.Contains("Berlin", frage.Instance.Frage);
        Assert.Contains("Tageswerte", frage.Instance.Frage);      // die Kaskade steht im Text

        frage.FindAll("button").First(b => b.TextContent.Trim() == "Nein").Click();
        Assert.Empty(geloescht);

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Löschen").Click();
        cut.FindComponent<EPOS.UI.Bausteine.Rueckfrage>()
           .FindAll("button").First(b => b.TextContent.Trim() == "Ja").Click();
        Assert.Equal(new[] { "Berlin" }, geloescht);
    }

    [Fact]
    public void Ein_Auslieferungssatz_wird_nicht_geloescht()
    {
        int geloescht = 0;
        var cut = Zeige(loeschen: _ => { geloescht++; return Task.FromResult(true); });

        cut.FindAll("button.epos-anlagenwahl")[2].Click();       // "Auslieferung Nord"
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Löschen").Click();

        Assert.Equal(0, geloescht);
        Assert.Contains("schreibgeschützt", cut.Instance.Meldung);
        Assert.False(cut.FindComponent<EPOS.UI.Bausteine.Rueckfrage>().Instance.Offen);
    }

    // =====================================================================
    //  Import - die zwei Auspraegungen, Fortschritt und Abbrechen (A-4)
    // =====================================================================

    [Fact]
    public void Ohne_Eingabe_bleibt_der_Importknopf_gesperrt()
    {
        var cut = Zeige();

        var einlesen = cut.FindAll("button").First(b => b.TextContent.Trim() == "Daten einlesen");
        Assert.True(einlesen.HasAttribute("disabled"));
    }

    [Fact]
    public void Ein_Ortsname_reicht_fuer_den_Import()
    {
        KlimaImportAuftrag? auftrag = null;
        var cut = Zeige(importieren: (a, _) =>
        {
            auftrag = a;
            return Task.FromResult(new KlimaImportErgebnis
            {
                Ausgang = KlimaImportAusgang.Erfolg, Bezeichner = "Lyon", Meldung = "fertig"
            });
        });

        cut.Find("input[list=epos-klimaregion-orte]").Input("Lyon");
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Daten einlesen").Click();

        Assert.NotNull(auftrag);
        Assert.Equal(KlimaImportArt.AusOrtsname, auftrag!.Art);
        Assert.Equal("Lyon", auftrag.Ortsname);
    }

    /// <summary>
    /// Der Handeingabe-Zweig braucht ALLE DREI Angaben — Longitude, Latitude und die
    /// Bezeichnung (wörtlich die Leerprüfung des Vorläufers).
    /// </summary>
    [Fact]
    public void Der_Handzweig_braucht_alle_drei_Angaben()
    {
        KlimaImportAuftrag? auftrag = null;
        var cut = Zeige(importieren: (a, _) =>
        {
            auftrag = a;
            return Task.FromResult(new KlimaImportErgebnis
            {
                Ausgang = KlimaImportAusgang.Erfolg, Bezeichner = "Eigen", Meldung = "fertig"
            });
        });

        var zahlen = cut.FindAll("input[inputmode=decimal]");
        zahlen[0].Input("9,18");
        Assert.True(cut.FindAll("button").First(b => b.TextContent.Trim() == "Daten einlesen")
                       .HasAttribute("disabled"));

        cut.FindAll("input[inputmode=decimal]")[1].Input("48,77");
        Assert.True(cut.FindAll("button").First(b => b.TextContent.Trim() == "Daten einlesen")
                       .HasAttribute("disabled"));

        cut.FindAll("input[type=text]").Last().Input("Eigen");
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Daten einlesen").Click();

        Assert.NotNull(auftrag);
        Assert.Equal(KlimaImportArt.AusKoordinaten, auftrag!.Art);
        Assert.Equal("Eigen", auftrag.Bezeichnung);
        Assert.Equal(9.18, auftrag.Longitude);
        Assert.Equal(48.77, auftrag.Latitude);
    }

    [Fact]
    public void Ein_gescheiterter_Import_meldet_sich_und_die_Liste_bleibt()
    {
        var cut = Zeige(importieren: (_, _) => Task.FromResult(new KlimaImportErgebnis
        {
            Ausgang = KlimaImportAusgang.Dublette,
            Meldung = "Die Klimaregion „Berlin\" gibt es bereits."
        }));

        cut.Find("input[list=epos-klimaregion-orte]").Input("Berlin");
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Daten einlesen").Click();

        Assert.Contains("gibt es bereits", cut.Instance.Meldung);
        Assert.False(cut.Instance.Laeuft);
    }

    /// <summary>
    /// A-4: <b>Der Import lässt sich abbrechen</b> — ohne Rückruf bleibt der Knopf
    /// weg (der Vorläufer hatte gar keinen).
    /// </summary>
    [Fact]
    public void Ohne_Abbruchruf_bleibt_der_Abbrechenknopf_weg()
    {
        var cut = Zeige(abbrechen: null);
        Assert.Empty(cut.FindAll(".epos-fortschritt button"));
    }

    [Fact]
    public void Der_Fortschritt_meldet_die_Schritte_und_laesst_sich_abbrechen()
    {
        int abgebrochen = 0;
        var tcs = new TaskCompletionSource<KlimaImportErgebnis>();

        var cut = Zeige(
            abbrechen: () => abgebrochen++,
            importieren: (_, melder) =>
            {
                melder.Report(new ImportFortschritt(2.0 / 7.0, "KLIMA_SCHRITT_ABRUF"));
                return tcs.Task;
            });

        cut.Find("input[list=epos-klimaregion-orte]").Input("Lyon");
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Daten einlesen").Click();

        Assert.True(cut.Instance.Laeuft);

        // Progress<T> meldet ueber den Synchronisationskontext - der Text steht
        // erst nach dem naechsten Zeichnen da.
        cut.WaitForAssertion(() => Assert.Contains("Klimadaten abrufen", cut.Markup));

        cut.Find(".epos-fortschritt button").Click();
        Assert.Equal(1, abgebrochen);

        tcs.SetResult(new KlimaImportErgebnis
        {
            Ausgang = KlimaImportAusgang.Abgebrochen,
            Meldung = "Der Import wurde abgebrochen."
        });
        cut.WaitForState(() => !cut.Instance.Laeuft);
        Assert.Contains("abgebrochen", cut.Instance.Meldung);
    }

    // =====================================================================
    //  Schluss
    // =====================================================================

    [Fact]
    public void Beenden_liefert_OK()
    {
        bool? antwort = null;
        var cut = Zeige(geschlossen: b => antwort = b);

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Beenden").Click();

        Assert.True(antwort);
    }

    [Fact]
    public void Esc_schliesst_nur_ohne_offene_Frage()
    {
        bool? antwort = null;
        var cut = Zeige(geschlossen: b => antwort = b);

        cut.FindAll("button.epos-anlagenwahl")[0].Click();
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Löschen").Click();
        cut.Find("div.epos-klimaregion").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(antwort);

        cut.FindComponent<EPOS.UI.Bausteine.Rueckfrage>()
           .FindAll("button").First(b => b.TextContent.Trim() == "Nein").Click();
        cut.Find("div.epos-klimaregion").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(antwort);
    }
}
