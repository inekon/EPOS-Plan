using System.Globalization;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Klimadaten;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
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
public class KlimadatenDialogTests : EposBunitContext
{
    /// <summary>
    /// Das Zeichenmodell EINES der beiden Bilder — seit Etappe E2 führt die
    /// <c>Regionsansicht</c> Modelle statt PNG-Bytes, und der Baustein
    /// <c>DiagrammSvg</c> zeichnet sie als Elemente in den Baum.
    ///
    /// <para>Es ist dasselbe Modell, das die Windows-Hülle baut
    /// (<c>ChartRenderer.JahresgangModell</c>, 8 760 Stundenwerte) — ein
    /// Ersatzmodell hätte weder Legende noch Zeichenfläche und damit nichts, woran
    /// die Bedienung hängt.</para>
    /// </summary>
    private static readonly Zeichenmodell MODELL = Bild();

    private static Zeichenmodell Bild()
    {
        var werte = new double[8760];
        for (int i = 0; i < werte.Length; i++)
            werte[i] = 10.0 - 12.0 * Math.Cos(2 * Math.PI * i / 8760.0);

        return ChartRenderer.JahresgangModell(
            "Jahrestemperatur Verlauf",
            new[] { new ChartRenderer.Reihe("Temperatur", werte, ChartRenderer.C_AUSSENTEMPERATUR) },
            "Stunde des Jahres", "Temperatur [°C]");
    }

    /// <summary>
    /// Die drei Regionen der Liste — seit Auftrag KL-4 Katalogzeilen mit sieben
    /// Spalten (Bezeichner, Quelle, Standort, Longitude, Latitude, Importdatum,
    /// Schreibschutz), wie sie <c>KlimaregionStammCtrl.Katalogfilterzeilen</c> liefert
    /// und die Hülle sie übersetzt.
    /// </summary>
    private static IReadOnlyList<Katalogfilterzeile> Regionen() => new List<Katalogfilterzeile>
    {
        Zeile(17, "Berlin", "PVGIS-Testreferenzjahr (weltweit)", "Berlin, Deutschland",
              13.3951, 52.5174, "2026-09-18", false),
        Zeile(42, "Stuttgart", "DWD-Testreferenzjahr aus Datei", "9,1800° / 48,7700°",
              9.18, 48.77, "2026-09-12", false),
        Zeile(3, "Auslieferung Nord", "", "Hamburg, Deutschland",
              10.0, 53.5, "", true)
    };

    /// <summary>Eine Katalogzeile der Regionsliste — der Aufbau der Hülle in einer Zeile.</summary>
    private static Katalogfilterzeile Zeile(int id, string name, string quelle, string standort,
                                            double lon, double lat, string datum, bool geschuetzt)
    {
        var z = new Katalogfilterzeile(id, name) { Geschuetzt = geschuetzt };
        return z
            .MitText(Katalogfilterprofil.SpBezeichner, name)
            .MitText(Katalogfilterprofil.SpQuelle, quelle)
            .MitText(Katalogfilterprofil.SpStandort, standort)
            .MitZahl(Katalogfilterprofil.SpLongitude, lon, 4)
            .MitZahl(Katalogfilterprofil.SpLatitude, lat, 4)
            .MitText(Katalogfilterprofil.SpImportdatum, datum)
            .MitKennzeichen(Katalogfilterprofil.SpSchreibschutz, geschuetzt);
    }

    public KlimadatenDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<KlimadatenDialog> Zeige(
        IReadOnlyList<Katalogfilterzeile>? regionen = null,
        Func<string, Task<KlimadatenDialog.Regionsansicht>>? ansicht = null,
        Func<KlimaImportAuftrag, IProgress<ImportFortschritt>, Task<KlimaImportErgebnis>>? importieren = null,
        Action? abbrechen = null,
        Func<string, Task<bool>>? loeschen = null,
        IReadOnlyList<string>? orte = null,
        Action<bool>? geschlossen = null,
        Func<string, Task<string?>>? dateiWaehlen = null,
        Func<KlimaImportAuftrag, Task<KlimaVorschauErgebnis>>? regionErmitteln = null,
        Func<Farbrolle, Farbe, Task>? farbeSetzen = null,
        Func<Farbrolle, Task>? farbeZuruecksetzen = null)
    {
        IReadOnlyList<Katalogfilterzeile> liste = regionen ?? Regionen();
        return Render<KlimadatenDialog>(p => p
            .Add(x => x.Regionen, () => Task.FromResult(liste))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Ansicht, ansicht ?? (n => Task.FromResult(
                new KlimadatenDialog.Regionsansicht("Details " + n, 9.18, 48.77, MODELL, MODELL, ""))))
            .Add(x => x.FarbeSetzen, farbeSetzen)
            .Add(x => x.FarbeZuruecksetzen, farbeZuruecksetzen)
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
            .Add(x => x.DateiWaehlen, dateiWaehlen)
            .Add(x => x.RegionErmitteln, regionErmitteln)
            .Add(x => x.Geschlossen, geschlossen ?? (_ => { })));
    }

    /// <summary>Der Importknopf der Leiste.</summary>
    private static AngleSharp.Dom.IElement Einlesen(IRenderedComponent<KlimadatenDialog> cut)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == "Daten einlesen");

    /// <summary>Schaltet die Klimaquelle über ihr Optionsfeld um.</summary>
    private static void QuelleWaehlen(IRenderedComponent<KlimadatenDialog> cut, KlimaQuelle quelle)
        => cut.FindAll("input[type=radio]")[(int)quelle].Change(true);

    /// <summary>Der Knopf der Regionsvorschau (Auftrag KL-3); null, wenn er fehlt.</summary>
    private static AngleSharp.Dom.IElement? Regionsknopf(IRenderedComponent<KlimadatenDialog> cut)
        => cut.FindAll("button").FirstOrDefault(b => b.TextContent.Trim() == "Region ermitteln");

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
                "PVGIS-SARAH3", 13.4, 52.5, MODELL, MODELL, ""));
        });

        cut.FindAll("button.epos-anlagenwahl")[0].Click();

        // W16b-O-2 (Gate 10.09.2026): bunits Click() gibt das Ereignis nur an
        // den Zeichner ab und wartet nicht auf den Ereignisbehandler - erst
        // das gezeichnete Markup belegt, dass Waehlen/AnsichtLaden fertig sind.
        cut.WaitForAssertion(() => Assert.Contains("PVGIS-SARAH3", cut.Markup),
                             TimeSpan.FromSeconds(10));
        Assert.Equal("Berlin", gefragt);
        Assert.Equal("Berlin", cut.Instance.Gewaehlt);

        // Der Baustein Reiter zeichnet nur das AKTIVE Blatt - erst das
        // Temperaturbild, nach dem Wechsel das Sonnenwinkelbild. Beide stehen
        // im Baustein DiagrammSvg.
        Assert.Single(cut.FindComponents<DiagrammSvg>());
        Assert.Equal("Jahrestemperatur Verlauf",
                     cut.FindComponent<DiagrammSvg>().Instance.Bezeichnung);
        Assert.Equal("klima-temperatur",
                     cut.FindComponent<DiagrammSvg>().Instance.Kennung);

        cut.FindAll(".epos-reiter-knopf")[1].Click();
        cut.WaitForAssertion(() => Assert.Equal("Sonnenwinkel Verlauf",
                     cut.FindComponent<DiagrammSvg>().Instance.Bezeichnung),
                             TimeSpan.FromSeconds(10));
        Assert.Equal("klima-sonnenwinkel",
                     cut.FindComponent<DiagrammSvg>().Instance.Kennung);
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

        // W16b-O-2 (Gate 10.09.2026): erst auf die gezeichnete Meldung warten,
        // statt sofort nach dem Click zu pruefen - siehe die Begruendung unten
        // bei Der_Fortschritt_meldet_die_Schritte_und_laesst_sich_abbrechen.
        cut.WaitForAssertion(() => Assert.Contains("keine Stundenwerte", cut.Instance.Meldung),
                             TimeSpan.FromSeconds(10));
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

        // W16b-O-2 (Gate 10.09.2026): bunits Click() wartet nicht auf den
        // Ereignisbehandler - erst auf die gezeichnete Rueckfrage warten.
        cut.WaitForState(() => cut.FindComponent<EPOS.UI.Bausteine.Rueckfrage>().Instance.Offen,
                         TimeSpan.FromSeconds(10));
        var frage = cut.FindComponent<EPOS.UI.Bausteine.Rueckfrage>();
        Assert.True(frage.Instance.VorgabeNein);
        Assert.Contains("Berlin", frage.Instance.Frage);
        Assert.Contains("Tageswerte", frage.Instance.Frage);      // die Kaskade steht im Text

        frage.FindAll("button").First(b => b.TextContent.Trim() == "Nein").Click();
        Assert.Empty(geloescht);        // BeiLoeschen ruft den Delegaten auf diesem Weg nie - kein Wettlauf

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Löschen").Click();
        cut.FindComponent<EPOS.UI.Bausteine.Rueckfrage>()
           .FindAll("button").First(b => b.TextContent.Trim() == "Ja").Click();
        cut.WaitForAssertion(() => Assert.Equal(new[] { "Berlin" }, geloescht),
                             TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Ein_Auslieferungssatz_wird_nicht_geloescht()
    {
        int geloescht = 0;
        var cut = Zeige(loeschen: _ => { geloescht++; return Task.FromResult(true); });

        cut.FindAll("button.epos-anlagenwahl")[2].Click();       // "Auslieferung Nord"
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Löschen").Click();

        // W16b-O-2 (Gate 10.09.2026): BeiLoeschen setzt die Meldung synchron,
        // aber der Ereignisbehandler laeuft erst, nachdem bunits Click()
        // zurueckgekehrt ist - deshalb auf das gezeichnete Ergebnis warten.
        cut.WaitForAssertion(() => Assert.Contains("schreibgeschützt", cut.Instance.Meldung),
                             TimeSpan.FromSeconds(10));
        Assert.Equal(0, geloescht);
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

        // W16b-O-2 (Gate 10.09.2026): der Auftrag entsteht im Ereignisbehandler
        // von BeiImport - bunits Click() wartet nicht darauf, also auf das
        // Ergebnis warten statt sofort zu pruefen.
        cut.WaitForAssertion(() => Assert.NotNull(auftrag), TimeSpan.FromSeconds(10));
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

        // W16b-O-2 (Gate 10.09.2026): siehe Ein_Ortsname_reicht_fuer_den_Import -
        // erst auf den Auftrag warten statt sofort nach dem Click zu pruefen.
        cut.WaitForAssertion(() => Assert.NotNull(auftrag), TimeSpan.FromSeconds(10));
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

        // W16b-O-2 (Gate 10.09.2026): BeiImport meldet und setzt _laeuft im
        // finally-Block - erst auf das gezeichnete Ergebnis warten.
        cut.WaitForAssertion(() => Assert.Contains("gibt es bereits", cut.Instance.Meldung),
                             TimeSpan.FromSeconds(10));
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

        // W16b-O-2 (Gate 10.09.2026): bunits Click() gibt das Ereignis nur an
        // den Zeichner ab und wartet nicht auf BeiImport - "_laeuft" steht erst,
        // wenn der Ereignisbehandler wirklich durchgelaufen ist.
        cut.WaitForState(() => cut.Instance.Laeuft, TimeSpan.FromSeconds(10));

        // Progress<T> meldet ueber den Synchronisationskontext - der Text steht
        // erst nach dem naechsten Zeichnen da. Die bunit-Vorgabe von einer Sekunde
        // reicht auf einem ausgelasteten CI-Laeufer nicht immer (Kern-Lauf
        // 33866130448: "Check count: 0" bei 1 867 parallel laufenden Faellen);
        // zehn Sekunden wie in KapitalwertVerlaufDialogTests.
        cut.WaitForAssertion(() => Assert.Contains("Klimadaten abrufen", cut.Markup),
                             TimeSpan.FromSeconds(10));

        cut.Find(".epos-fortschritt button").Click();

        // W16b-O-2 (Gate 10.09.2026): Fortschritt.AbbruchGeklickt ruft den
        // Abbruch ueber EventCallback.InvokeAsync auf dem Renderer-Dispatcher -
        // bunits synchrones Click() gibt das Ereignis nur an den Zeichner ab
        // und wartet NICHT auf den Ereignisbehandler (nur ClickAsync taete
        // das, vgl. ProjektTransferDialogTests, W16b-O-2). Im vollen Lauf
        // (3 360 Faelle, zwei Threads) stand der Abbruchzaehler direkt nach
        // dem Click() deshalb noch auf 0 - jetzt wird auf das Ergebnis
        // gewartet statt sofort geprueft.
        cut.WaitForAssertion(() => Assert.Equal(1, abgebrochen), TimeSpan.FromSeconds(10));

        tcs.SetResult(new KlimaImportErgebnis
        {
            Ausgang = KlimaImportAusgang.Abgebrochen,
            Meldung = "Der Import wurde abgebrochen."
        });
        cut.WaitForState(() => !cut.Instance.Laeuft, TimeSpan.FromSeconds(10));
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

        // W16b-O-2 (Gate 10.09.2026): BeiSchliessen ruft Geschlossen.InvokeAsync
        // ueber den Renderer-Dispatcher - bunits Click() wartet nicht darauf
        // (Vorbild ProjektTransferDialogTests.Schliessen_meldet_ob_ein_Import_gelungen_ist).
        cut.WaitForAssertion(() => Assert.True(antwort), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Esc_schliesst_nur_ohne_offene_Frage()
    {
        bool? antwort = null;
        var cut = Zeige(geschlossen: b => antwort = b);

        cut.FindAll("button.epos-anlagenwahl")[0].Click();
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Löschen").Click();
        cut.Find("div.epos-katalog-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(antwort);      // BeiTaste liefert bei offener Rueckfrage nichts - kein Wettlauf

        cut.FindComponent<EPOS.UI.Bausteine.Rueckfrage>()
           .FindAll("button").First(b => b.TextContent.Trim() == "Nein").Click();
        cut.Find("div.epos-katalog-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        // W16b-O-2 (Gate 10.09.2026): BeiTaste ruft hier Geschlossen.InvokeAsync(false)
        // ueber den Renderer-Dispatcher - derselbe Wettlauf wie bei Beenden_liefert_OK.
        cut.WaitForAssertion(() => Assert.False(antwort), TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// <b>„Das Kreuz steht beim Titel"</b> (Anwenderentscheid 15.09.2026): Das ✕ der
    /// Kopfzeile wirkt genau wie Esc — es schließt ohne Übernahme und meldet
    /// <c>false</c>.
    /// </summary>
    [Fact]
    public void Das_Kreuz_im_Kopf_schliesst_wie_Esc()
    {
        bool? antwort = null;
        var cut = Zeige(geschlossen: b => antwort = b);

        cut.Find(".epos-dialog-zu").Click();

        // Derselbe Wettlauf wie oben: Geschlossen.InvokeAsync laeuft ueber den Dispatcher.
        cut.WaitForAssertion(() => Assert.False(antwort), TimeSpan.FromSeconds(10));
    }

    // =====================================================================
    //  Klimaquelle: PVGIS, TRY-Datei, TRY-Regionaldaten (Auftrag KL1-B)
    // =====================================================================

    /// <summary>
    /// Die drei Quellen stehen als Optionsgruppe da — PVGIS ist die Vorgabe, und bei
    /// ihr zeigt der Dialog KEIN TRY-Feld.
    /// </summary>
    [Fact]
    public void Die_drei_Klimaquellen_stehen_zur_Wahl_und_PVGIS_ist_die_Vorgabe()
    {
        var cut = Zeige();

        var optionen = cut.FindAll("input[type=radio]");
        Assert.Equal(3, optionen.Count);
        Assert.True(optionen[0].HasAttribute("checked"));
        Assert.Equal(KlimaQuelle.PvgisTmy, cut.Instance.Quelle);

        Assert.Contains("PVGIS", cut.Markup);
        Assert.Contains("DWD-Testreferenzjahr aus Datei", cut.Markup);
        Assert.Contains("TRY-Regionaldaten (Deutschland)", cut.Markup);

        // Ohne TRY-Quelle steht weder eine Dateiwahl noch Jahr oder Szenario.
        Assert.Empty(cut.FindComponents<EPOS.UI.Standards.Dateiwahl>());
        Assert.Empty(cut.FindAll("select"));
    }

    /// <summary>
    /// <b>Die TRY-Datei zeigt IHR Feld</b> — eine Dateiwahl, kein Jahr, kein Szenario.
    /// </summary>
    [Fact]
    public void Die_TRY_Datei_zeigt_die_Dateiwahl()
    {
        var cut = Zeige(dateiWaehlen: _ => Task.FromResult<string?>("/tmp/probe.dat"));

        QuelleWaehlen(cut, KlimaQuelle.TryDatei);

        Assert.Equal(KlimaQuelle.TryDatei, cut.Instance.Quelle);
        Assert.Single(cut.FindComponents<EPOS.UI.Standards.Dateiwahl>());
        Assert.Empty(cut.FindAll("select"));
        Assert.Contains("TRY-Datei", cut.Markup);
    }

    /// <summary>
    /// <b>Die Regionaldaten zeigen Jahr, Szenario und die Wahl des Pakets.</b> Die
    /// Jahre sind 2015 und 2045, die Szenarien die drei des Pakets.
    /// </summary>
    [Fact]
    public void Die_Regionaldaten_zeigen_Jahr_Szenario_und_Paketwahl()
    {
        var cut = Zeige(dateiWaehlen: _ => Task.FromResult<string?>("/tmp/data.zip"));

        QuelleWaehlen(cut, KlimaQuelle.TryRegional);

        Assert.Equal(KlimaQuelle.TryRegional, cut.Instance.Quelle);

        var listen = cut.FindAll("select");
        Assert.Equal(2, listen.Count);

        var jahre = listen[0].QuerySelectorAll("option").Select(o => o.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "2015", "2045" }, jahre);

        var szenarien = listen[1].QuerySelectorAll("option").Select(o => o.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "mittleres Jahr", "sommerwarm", "winterkalt" }, szenarien);

        Assert.Single(cut.FindComponents<EPOS.UI.Standards.Dateiwahl>());
    }

    /// <summary>
    /// <b><c>ImportErlaubt</c> je Quelle.</b> Bei PVGIS und den Regionaldaten genügt der
    /// Standort; die TRY-Datei braucht zusätzlich einen Pfad.
    /// </summary>
    [Fact]
    public void Die_TRY_Datei_braucht_Standort_UND_Pfad()
    {
        var cut = Zeige(dateiWaehlen: _ => Task.FromResult<string?>("/tmp/probe.dat"));

        QuelleWaehlen(cut, KlimaQuelle.TryDatei);
        Assert.True(Einlesen(cut).HasAttribute("disabled"));          // weder noch

        cut.Find("input[list=epos-klimaregion-orte]").Input("Lyon");
        Assert.True(Einlesen(cut).HasAttribute("disabled"));          // Standort allein reicht nicht

        // Der Waehler der Plattform liefert den Pfad.
        cut.FindComponent<EPOS.UI.Standards.Dateiwahl>()
           .FindAll("button").First(b => b.TextContent.Trim().StartsWith("Durchsuchen")).Click();

        cut.WaitForAssertion(() => Assert.False(Einlesen(cut).HasAttribute("disabled")),
                             TimeSpan.FromSeconds(10));
    }

    /// <summary>Die Regionaldaten kommen ohne Paketdatei aus — dann gilt die Adresse.</summary>
    [Fact]
    public void Die_Regionaldaten_brauchen_nur_den_Standort()
    {
        var cut = Zeige();

        QuelleWaehlen(cut, KlimaQuelle.TryRegional);
        Assert.True(Einlesen(cut).HasAttribute("disabled"));

        cut.Find("input[list=epos-klimaregion-orte]").Input("Bremerhaven");
        Assert.False(Einlesen(cut).HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>Der Auftrag trägt die Quelle und ihre Angaben</b> — Pfad bei der Datei,
    /// Jahr, Szenario und Paketpfad bei den Regionaldaten.
    /// </summary>
    [Fact]
    public void Der_Auftrag_traegt_Quelle_Pfad_Jahr_und_Szenario()
    {
        KlimaImportAuftrag? auftrag = null;
        var cut = Zeige(
            importieren: (a, _) =>
            {
                auftrag = a;
                return Task.FromResult(new KlimaImportErgebnis
                {
                    Ausgang = KlimaImportAusgang.Erfolg, Bezeichner = "Berlin", Meldung = "fertig"
                });
            },
            dateiWaehlen: _ => Task.FromResult<string?>("/tmp/data.zip"));

        QuelleWaehlen(cut, KlimaQuelle.TryRegional);
        cut.Find("input[list=epos-klimaregion-orte]").Input("Bremerhaven");

        var listen = cut.FindAll("select");
        listen[0].Change("2045");
        cut.FindAll("select")[1].Change(((int)TrySzenario.Winterkalt)
                                        .ToString(CultureInfo.InvariantCulture));

        cut.FindComponent<EPOS.UI.Standards.Dateiwahl>()
           .FindAll("button").First(b => b.TextContent.Trim().StartsWith("Durchsuchen")).Click();

        cut.WaitForAssertion(() => Assert.False(Einlesen(cut).HasAttribute("disabled")),
                             TimeSpan.FromSeconds(10));
        Einlesen(cut).Click();

        cut.WaitForAssertion(() => Assert.NotNull(auftrag), TimeSpan.FromSeconds(10));
        Assert.Equal(KlimaQuelle.TryRegional, auftrag!.Quelle);
        Assert.Equal(2045, auftrag.TryJahr);
        Assert.Equal(TrySzenario.Winterkalt, auftrag.Szenario);
        Assert.Equal("/tmp/data.zip", auftrag.TryPaketPfad);
        Assert.Equal("", auftrag.TryPfad);
    }

    /// <summary>
    /// Bei PVGIS sieht der Auftrag aus wie vor KL1-B: Quelle <c>PvgisTmy</c>, kein Pfad,
    /// Vorgabejahr und mittleres Jahr.
    /// </summary>
    [Fact]
    public void Bei_PVGIS_bleibt_der_Auftrag_wie_bisher()
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
        Einlesen(cut).Click();

        cut.WaitForAssertion(() => Assert.NotNull(auftrag), TimeSpan.FromSeconds(10));
        Assert.Equal(KlimaQuelle.PvgisTmy, auftrag!.Quelle);
        Assert.Equal("", auftrag.TryPfad);
        Assert.Equal("", auftrag.TryPaketPfad);
        Assert.Equal(KlimaImportAuftrag.TRY_JAHR_VORGABE, auftrag.TryJahr);
        Assert.Equal(TrySzenario.MittleresJahr, auftrag.Szenario);
    }

    /// <summary>
    /// <b>Ohne Wähler der Plattform bleibt der Knopf weg</b> (Regel des Bausteins
    /// <c>Dateiwahl</c>) — das Pfadfeld steht trotzdem da.
    /// </summary>
    [Fact]
    public void Ohne_Dateiwaehler_bleibt_der_Knopf_weg()
    {
        var cut = Zeige(dateiWaehlen: null);

        QuelleWaehlen(cut, KlimaQuelle.TryDatei);

        var wahl = cut.FindComponent<EPOS.UI.Standards.Dateiwahl>();
        Assert.Empty(wahl.FindAll("button"));
        Assert.Single(wahl.FindAll("input[type=text]"));
    }

    // =====================================================================
    //  Standort aus der TRY-Datei (Auftrag KL-2)
    // =====================================================================

    /// <summary>
    /// Schreibt eine kurze, SYNTHETISCHE TRY-Datei in den Temp-Ordner — nur der Kopf,
    /// denn mehr liest der Dialog nicht. Rechtswert 3 929 310 / Hochwert 2 478 193
    /// ergeben 9,0000° O / 49,0000° N.
    /// </summary>
    private static string ProbeSchreiben(string name, bool mitStandort = true)
    {
        string pfad = System.IO.Path.Combine(System.IO.Path.GetTempPath(), name + ".dat");
        var z = new List<string>
        {
            "Musterdatensatz Testreferenzjahr - synthetische Probe",
            "Art des TRY: Jahr (mittleres Jahr)"
        };
        if (mitStandort)
        {
            z.Add("Rechtswert: 3929310 Meter");
            z.Add("Hochwert: 2478193 Meter");
        }
        z.Add("*** ");
        System.IO.File.WriteAllText(pfad, string.Join("\r\n", z) + "\r\n");
        return pfad;
    }

    /// <summary>
    /// <b>Die Dateiwahl belegt den Standort vor</b> (A-KL2-1): Longitude, Latitude und
    /// — weil sie leer war — die Bezeichnung aus dem Dateinamen. Darunter steht, woher
    /// die Zahlen kommen, und <c>Daten einlesen</c> ist ohne weitere Eingabe frei.
    /// </summary>
    [Fact]
    public void Die_Dateiwahl_belegt_den_Standort_aus_dem_Dateikopf_vor()
    {
        string pfad = ProbeSchreiben("epos_kl2_" + Guid.NewGuid().ToString("N"));
        try
        {
            var cut = Zeige(dateiWaehlen: _ => Task.FromResult<string?>(pfad));

            QuelleWaehlen(cut, KlimaQuelle.TryDatei);
            cut.FindComponent<EPOS.UI.Standards.Dateiwahl>()
               .FindAll("button").First(b => b.TextContent.Trim().StartsWith("Durchsuchen")).Click();

            cut.WaitForAssertion(() => Assert.NotEmpty(cut.Instance.Standortzeile),
                                 TimeSpan.FromSeconds(10));

            var zahlen = cut.FindAll("input[inputmode=decimal]");
            Assert.Equal("9", zahlen[0].GetAttribute("value"));
            Assert.Equal("49", zahlen[1].GetAttribute("value"));

            // Die Bezeichnung ist der Dateiname OHNE Endung.
            Assert.Equal(System.IO.Path.GetFileNameWithoutExtension(pfad),
                         cut.FindAll("input[type=text]").Last().GetAttribute("value"));

            // Die Herkunftszeile nennt Rechts- und Hochwert und sagt, dass es änderbar ist.
            Assert.Contains("3929310", cut.Instance.Standortzeile);
            Assert.Contains("2478193", cut.Instance.Standortzeile);
            Assert.Contains("änderbar", cut.Instance.Standortzeile);
            Assert.Contains("3929310", cut.Markup);

            Assert.False(Einlesen(cut).HasAttribute("disabled"));
        }
        finally
        {
            try { System.IO.File.Delete(pfad); } catch { /* aufraeumen darf scheitern */ }
        }
    }

    /// <summary>
    /// <b>Der Anwender überschreibt die Vorbelegung</b> (A-KL2-2): Sein Wert geht in den
    /// Auftrag, und die Herkunftszeile der Datei verschwindet — sie stimmte nicht mehr.
    /// </summary>
    [Fact]
    public void Der_Anwender_kann_den_vorbelegten_Standort_ueberschreiben()
    {
        string pfad = ProbeSchreiben("epos_kl2u_" + Guid.NewGuid().ToString("N"));
        KlimaImportAuftrag? auftrag = null;
        try
        {
            var cut = Zeige(
                importieren: (a, _) =>
                {
                    auftrag = a;
                    return Task.FromResult(new KlimaImportErgebnis
                    {
                        Ausgang = KlimaImportAusgang.Erfolg, Bezeichner = "Eigen", Meldung = "fertig"
                    });
                },
                dateiWaehlen: _ => Task.FromResult<string?>(pfad));

            QuelleWaehlen(cut, KlimaQuelle.TryDatei);
            cut.FindComponent<EPOS.UI.Standards.Dateiwahl>()
               .FindAll("button").First(b => b.TextContent.Trim().StartsWith("Durchsuchen")).Click();

            cut.WaitForAssertion(() => Assert.NotEmpty(cut.Instance.Standortzeile),
                                 TimeSpan.FromSeconds(10));

            // Der Anwender trägt einen anderen Punkt ein.
            cut.FindAll("input[inputmode=decimal]")[0].Input("13,4");
            cut.FindAll("input[inputmode=decimal]")[1].Input("52,52");

            Assert.Equal("", cut.Instance.Standortzeile);

            Einlesen(cut).Click();
            cut.WaitForAssertion(() => Assert.NotNull(auftrag), TimeSpan.FromSeconds(10));

            Assert.Equal(KlimaQuelle.TryDatei, auftrag!.Quelle);
            Assert.Equal(pfad, auftrag.TryPfad);
            Assert.Equal(13.4, auftrag.Longitude);
            Assert.Equal(52.52, auftrag.Latitude);
        }
        finally
        {
            try { System.IO.File.Delete(pfad); } catch { /* aufraeumen darf scheitern */ }
        }
    }

    /// <summary>
    /// <b>Ein Kopf ohne Lambertwerte ist kein Fehler</b>: Es bleibt bei einem Hinweis,
    /// die Felder bleiben leer, und <c>ImportErlaubt</c> urteilt wie bisher — ohne
    /// Standort bleibt der Knopf gesperrt.
    /// </summary>
    [Fact]
    public void Eine_Datei_ohne_Standort_im_Kopf_meldet_das_und_laesst_die_Felder_stehen()
    {
        string pfad = ProbeSchreiben("epos_kl2o_" + Guid.NewGuid().ToString("N"),
                                     mitStandort: false);
        try
        {
            var cut = Zeige(dateiWaehlen: _ => Task.FromResult<string?>(pfad));

            QuelleWaehlen(cut, KlimaQuelle.TryDatei);
            cut.FindComponent<EPOS.UI.Standards.Dateiwahl>()
               .FindAll("button").First(b => b.TextContent.Trim().StartsWith("Durchsuchen")).Click();

            cut.WaitForAssertion(() => Assert.NotEmpty(cut.Instance.Meldung),
                                 TimeSpan.FromSeconds(10));

            Assert.Contains("nicht lesbar", cut.Instance.Meldung);
            Assert.Equal("", cut.Instance.Standortzeile);

            var zahlen = cut.FindAll("input[inputmode=decimal]");
            Assert.Equal("", zahlen[0].GetAttribute("value"));
            Assert.Equal("", zahlen[1].GetAttribute("value"));

            Assert.True(Einlesen(cut).HasAttribute("disabled"));
        }
        finally
        {
            try { System.IO.File.Delete(pfad); } catch { /* aufraeumen darf scheitern */ }
        }
    }

    // =====================================================================
    //  Die REGIONSVORSCHAU (Auftrag KL-3, Anwenderentscheid 19.09.2026 b)
    // =====================================================================

    /// <summary>
    /// <b>Der Knopf steht NUR im Regionaldaten-Zweig.</b> Bei PVGIS und bei einer
    /// TRY-Datei gäbe es keine Region zu ermitteln — ein Knopf ohne Wirkung wäre eine
    /// Behauptung, die nicht stimmt.
    /// </summary>
    [Fact]
    public void Der_Regionsknopf_steht_nur_bei_den_Regionaldaten()
    {
        var cut = Zeige(regionErmitteln: _ => Task.FromResult(new KlimaVorschauErgebnis()));

        Assert.Null(Regionsknopf(cut));                    // PVGIS

        QuelleWaehlen(cut, KlimaQuelle.TryDatei);
        Assert.Null(Regionsknopf(cut));

        QuelleWaehlen(cut, KlimaQuelle.TryRegional);
        Assert.NotNull(Regionsknopf(cut));
    }

    /// <summary>
    /// <b>Ohne Standort bleibt der Knopf gesperrt</b>, mit Ortsnamen ist er frei — und
    /// er braucht, anders als „Daten einlesen", KEINE Bezeichnung: Die Vorschau legt
    /// nichts an.
    /// </summary>
    [Fact]
    public void Der_Regionsknopf_braucht_nur_einen_Standort()
    {
        var cut = Zeige(regionErmitteln: _ => Task.FromResult(new KlimaVorschauErgebnis()));

        QuelleWaehlen(cut, KlimaQuelle.TryRegional);
        Assert.True(Regionsknopf(cut)!.HasAttribute("disabled"));

        cut.Find("input[list=epos-klimaregion-orte]").Input("Geislingen");
        Assert.False(Regionsknopf(cut)!.HasAttribute("disabled"));
    }

    /// <summary>Ohne Delegat bleibt der Knopf gesperrt — einlesen darf der Anwender
    /// trotzdem; die Vorschau ist eine Auskunft, keine Bedingung.</summary>
    [Fact]
    public void Ohne_Delegat_bleibt_der_Regionsknopf_gesperrt()
    {
        var cut = Zeige();

        QuelleWaehlen(cut, KlimaQuelle.TryRegional);
        cut.Find("input[list=epos-klimaregion-orte]").Input("Geislingen");

        Assert.True(Regionsknopf(cut)!.HasAttribute("disabled"));
        Assert.False(Einlesen(cut).HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>Die Ergebniszeile nennt Region, Station und Entfernung</b> — mit dem NAMEN
    /// der Repräsentanzstation, denn „Region 14" allein sagt einem Anwender nichts.
    /// </summary>
    [Fact]
    public void Die_Ergebniszeile_nennt_Region_Station_und_Entfernung()
    {
        KlimaImportAuftrag? gefragt = null;
        var cut = Zeige(regionErmitteln: a =>
        {
            gefragt = a;
            return Task.FromResult(new KlimaVorschauErgebnis
            {
                Ausgang = KlimaImportAusgang.Erfolg,
                Meldung = "Region 14 Stötten, Station 48,6600 / 9,8600, Entfernung 12 km"
            });
        });

        QuelleWaehlen(cut, KlimaQuelle.TryRegional);
        cut.Find("input[list=epos-klimaregion-orte]").Input("Geislingen");
        Regionsknopf(cut)!.Click();

        cut.WaitForAssertion(() => Assert.Contains("Stötten", cut.Instance.Regionsvorschau),
                             TimeSpan.FromSeconds(10));

        Assert.Contains("Region 14 Stötten", cut.Markup);
        Assert.Contains("Entfernung 12 km", cut.Markup);

        // Der Auftrag der Vorschau traegt die Quelle und das Bezugsjahr der Auswahl.
        Assert.NotNull(gefragt);
        Assert.Equal(KlimaQuelle.TryRegional, gefragt!.Quelle);
        Assert.Equal("Geislingen", gefragt.Ortsname);
        Assert.Equal(KlimaImportAuftrag.TRY_JAHR_VORGABE, gefragt.TryJahr);
    }

    /// <summary>
    /// <b>Ein Fehlschlag steht als Zeile da und sperrt nichts</b>: außerhalb der
    /// 300-km-Grenze, kein Bereichsabruf, Ort unbekannt — einlesen darf der Anwender
    /// weiterhin.
    /// </summary>
    [Fact]
    public void Ein_Fehlschlag_der_Vorschau_steht_als_Zeile_da()
    {
        var cut = Zeige(regionErmitteln: _ => Task.FromResult(new KlimaVorschauErgebnis
        {
            Ausgang = KlimaImportAusgang.Dateifehler,
            Meldung = "Der nächste TRY-Regionsmittelpunkt liegt 900 km entfernt (Grenze 300 km)."
        }));

        QuelleWaehlen(cut, KlimaQuelle.TryRegional);
        cut.Find("input[list=epos-klimaregion-orte]").Input("Madrid");
        Regionsknopf(cut)!.Click();

        cut.WaitForAssertion(() => Assert.Contains("900 km", cut.Instance.Regionsvorschau),
                             TimeSpan.FromSeconds(10));

        Assert.Contains("900 km", cut.Markup);
        Assert.False(Einlesen(cut).HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>Eine geänderte Eingabe verwirft die Zeile</b> — sie gälte sonst für einen
    /// Standort, den es im Dialog nicht mehr gibt.
    /// </summary>
    [Fact]
    public void Eine_geaenderte_Eingabe_verwirft_die_Ergebniszeile()
    {
        var cut = Zeige(regionErmitteln: _ => Task.FromResult(new KlimaVorschauErgebnis
        {
            Ausgang = KlimaImportAusgang.Erfolg,
            Meldung = "Region 14 Stötten, Station 48,6600 / 9,8600, Entfernung 12 km"
        }));

        QuelleWaehlen(cut, KlimaQuelle.TryRegional);
        cut.Find("input[list=epos-klimaregion-orte]").Input("Geislingen");
        Regionsknopf(cut)!.Click();

        cut.WaitForAssertion(() => Assert.Contains("Stötten", cut.Instance.Regionsvorschau),
                             TimeSpan.FromSeconds(10));

        cut.Find("input[list=epos-klimaregion-orte]").Input("Hamburg");
        Assert.Equal("", cut.Instance.Regionsvorschau);
    }

    // =====================================================================
    //  Auftrag KL-4 - Fussleiste, Importfreigabe und die Katalogliste
    // =====================================================================

    /// <summary>
    /// <b>EINE Fußleiste mit vier Knöpfen</b> (Anwenderwunsch 19.09.2026, Punkt 1):
    /// „Daten einlesen — Füller — Löschen — Beenden", Beenden primär. Die Liste
    /// trägt KEINEN Aktionsknopf mehr: Die listenlokale Leiste mit „Löschen" lag am
    /// Ende der Listenspalte und stieß bei schmalem Fenster an die Reiterleiste.
    /// </summary>
    [Fact]
    public void Eine_Fussleiste_traegt_die_vier_Knoepfe_in_der_Reihenfolge()
    {
        var cut = Zeige();

        var fuss = cut.FindAll("div.epos-leiste").Last();

        Assert.Equal(new[] { "Daten einlesen", "Löschen", "Beenden" },
                     fuss.QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray());
        Assert.Single(fuss.QuerySelectorAll("span.epos-leiste-fueller"));
        Assert.Contains("epos-knopf--primaer",
                        fuss.QuerySelectorAll("button").Last().ClassName ?? "");

        // Die Listenspalte ist knopffrei - bis auf die Bedienelemente der
        // Katalogliste selbst (Wahlknopf, Sortierpfeil, Trichter).
        var liste = cut.Find("div.epos-katalog-liste");
        Assert.DoesNotContain("Löschen",
            liste.QuerySelectorAll("button").Select(b => b.TextContent.Trim()));
    }

    /// <summary>
    /// <b>„Daten einlesen" bleibt nach einem Erfolg frei</b> (Anwenderwunsch
    /// 19.09.2026, Punkt 2: „‚Daten Einlesen' verschwindet nach dem Einlesen").
    /// Der Knopf war nie weg — er war GESPERRT, weil der Erfolgsfall Ortsname und
    /// Bezeichnung leerte und <c>ImportErlaubt</c> damit seinen Standort verlor.
    /// </summary>
    [Fact]
    public void Nach_einem_Erfolg_bleiben_die_Felder_und_der_Knopf_frei()
    {
        var cut = Zeige(importieren: (_, _) => Task.FromResult(new KlimaImportErgebnis
        {
            Ausgang = KlimaImportAusgang.Erfolg, Bezeichner = "Lyon", Meldung = "fertig"
        }));

        cut.Find("input[list=epos-klimaregion-orte]").Input("Lyon");
        Einlesen(cut).Click();

        cut.WaitForAssertion(() => Assert.Contains("fertig", cut.Instance.Meldung),
                             TimeSpan.FromSeconds(10));

        Assert.Equal("Lyon", cut.Find("input[list=epos-klimaregion-orte]").GetAttribute("value"));
        Assert.False(Einlesen(cut).HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>Der Dublettenschutz ist der Schutz vor Doppelimport</b>, nicht das Leeren
    /// der Felder: Ein zweiter Klick meldet sich, und die Liste bleibt, wie sie war.
    /// </summary>
    [Fact]
    public void Ein_zweiter_Klick_meldet_die_Dublette_und_die_Liste_bleibt()
    {
        int laeufe = 0;
        var cut = Zeige(importieren: (_, _) =>
        {
            laeufe++;
            return Task.FromResult(laeufe == 1
                ? new KlimaImportErgebnis
                  { Ausgang = KlimaImportAusgang.Erfolg, Bezeichner = "Lyon", Meldung = "fertig" }
                : new KlimaImportErgebnis
                  { Ausgang = KlimaImportAusgang.Dublette,
                    Meldung = "Die Klimaregion gibt es bereits." });
        });

        cut.Find("input[list=epos-klimaregion-orte]").Input("Lyon");
        Einlesen(cut).Click();
        cut.WaitForAssertion(() => Assert.Contains("fertig", cut.Instance.Meldung),
                             TimeSpan.FromSeconds(10));

        Einlesen(cut).Click();
        cut.WaitForAssertion(() => Assert.Contains("gibt es bereits", cut.Instance.Meldung),
                             TimeSpan.FromSeconds(10));

        Assert.Equal(2, laeufe);
        Assert.Equal(3, cut.FindAll("button.epos-anlagenwahl").Count);
    }

    /// <summary>
    /// <b>Die Regionsliste ist die EINE Katalogliste des Hauses</b> (Anwenderwunsch
    /// 19.09.2026, Punkt 3): Suchfeld, Trefferzeile, sieben Spalten samt Quelle und
    /// Standort — und KEIN Vergleichsknopf: Eine Klimaregion ist kein Gerät mit
    /// Kennwerten.
    /// </summary>
    [Fact]
    public void Die_Liste_zeigt_Suche_sieben_Spalten_und_keinen_Vergleich()
    {
        var cut = Zeige();

        Assert.Single(cut.FindAll("label.epos-katalog-suchfeld input"));
        Assert.Contains("3", cut.Find("span.epos-katalog-treffer").TextContent);
        Assert.Empty(cut.FindAll("button.epos-katalog-vergleichknopf"));

        var koepfe = cut.FindAll("span.epos-spaltenkopf-text")
                        .Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Klimaregion", "Quelle", "Standort", "Longitude", "Latitude",
                             "Importdatum", "Schreibschutz" }, koepfe);

        // Quelle und Standort stehen als Text in den Zeilen.
        Assert.Contains("DWD-Testreferenzjahr aus Datei", cut.Markup);
        Assert.Contains("Berlin, Deutschland", cut.Markup);
    }

    /// <summary>
    /// <b>Die Spalte „Quelle" sagt das ganze Wetterjahr</b> (A-KL6-1,
    /// Anwenderentscheid 19.09.2026): „TRY-Regionaldaten (Deutschland) · 2045 ·
    /// sommerwarm". Den Satz baut der Kern
    /// (<c>KlimaregionStammCtrl.Katalogfilterzeilen</c> über <c>KlimaAnzeige</c>); der
    /// Dialog zeigt ihn und lässt darüber suchen — „2045" wird damit zum Filter auf
    /// das Bezugsjahr, ohne eine achte Spalte.
    ///
    /// <para>Ein Altbestand ohne Herkunft bekommt den Halbgeviertstrich der
    /// Katalogliste, nie eine erfundene Quelle.</para>
    /// </summary>
    [Fact]
    public void Die_Spalte_Quelle_zeigt_Bezugsjahr_und_Szenario()
    {
        var cut = Zeige(new List<Katalogfilterzeile>
        {
            Zeile(17, "Berlin", "PVGIS-Testreferenzjahr (weltweit)", "Berlin, Deutschland",
                  13.3951, 52.5174, "2026-09-18", false),
            Zeile(46, "hagelloch", "TRY-Regionaldaten (Deutschland) · 2045 · sommerwarm",
                  "9,0500° / 48,5200°", 9.05, 48.52, "2026-09-19", false),
            Zeile(3, "Auslieferung Nord", "", "Hamburg, Deutschland", 10.0, 53.5, "", true)
        });

        Assert.Contains("TRY-Regionaldaten (Deutschland) · 2045 · sommerwarm", cut.Markup);
        Assert.Contains("–", cut.Markup);

        // Ueber das Bezugsjahr laesst sich suchen - ohne eine eigene Spalte.
        cut.Find("label.epos-katalog-suchfeld input").Input("2045");
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("button.epos-anlagenwahl")),
                             TimeSpan.FromSeconds(10));
        Assert.Contains("hagelloch", cut.Markup);
        Assert.DoesNotContain("Berlin, Deutschland", cut.Markup);
    }

    /// <summary>
    /// <b>Die Wahl überlebt einen Filterwechsel</b>: Sie hängt am Bezeichner, nicht
    /// an der Zeilennummer — dieselbe Regel wie in den vierzehn anderen Katalogen.
    /// </summary>
    [Fact]
    public void Die_Wahl_ueberlebt_einen_Filterwechsel()
    {
        var cut = Zeige();

        cut.FindAll("button.epos-anlagenwahl")[1].Click();       // Stuttgart
        cut.WaitForAssertion(() => Assert.Equal("Stuttgart", cut.Instance.Gewaehlt),
                             TimeSpan.FromSeconds(10));

        cut.Find("label.epos-katalog-suchfeld input").Input("stutt");
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("button.epos-anlagenwahl")),
                             TimeSpan.FromSeconds(10));
        Assert.Equal("Stuttgart", cut.Instance.Gewaehlt);

        cut.Find("label.epos-katalog-suchfeld input").Input("");
        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("button.epos-anlagenwahl").Count),
                             TimeSpan.FromSeconds(10));
        Assert.Equal("Stuttgart", cut.Instance.Gewaehlt);
    }

    /// <summary>
    /// <b>Die Dateizeile steht in EINER Zeile</b>: Beschriftung, Feld und
    /// „Durchsuchen" — das leistet das <c>Formularraster</c>
    /// (<c>.epos-dateiwahl &gt; .epos-knopf</c>). Vorher lag der Knopf unter seinem
    /// Feld, weil der Block in einer Klasse ohne Stilblatt stand.
    /// </summary>
    [Fact]
    public void Die_Dateizeile_traegt_Beschriftung_Feld_und_Knopf_in_einer_Zeile()
    {
        var cut = Zeige(dateiWaehlen: _ => Task.FromResult<string?>("/tmp/x.dat"));

        QuelleWaehlen(cut, KlimaQuelle.TryDatei);

        Assert.Equal(2, cut.FindAll("div.epos-formularraster--einspaltig").Count);

        var zeile = cut.Find("span.epos-feld-zeile.epos-dateiwahl");
        var feld = zeile.ParentElement!;
        Assert.Equal("LABEL", feld.TagName);
        Assert.Contains("epos-feld", feld.ClassName ?? "");
        Assert.Single(feld.QuerySelectorAll("span.epos-feld-text"));
        Assert.Single(zeile.QuerySelectorAll("input"));
        Assert.Single(zeile.QuerySelectorAll("button.epos-knopf"));
    }

    /// <summary>
    /// <b>Die Virtualisierungsschwelle gilt auch hier</b> (Hausregel W6-B-2: ab 120
    /// gefilterten Zeilen). 150 Regionen sind mehr, als ein Anwender je anlegt —
    /// aber die Liste ist derselbe Baustein wie die mit 20 749 PV-Modulen, und der
    /// Fall muss gezeichnet werden, ohne dass die Wahl verlorengeht.
    /// </summary>
    [Fact]
    public void Hundertfuenfzig_Regionen_virtualisieren_und_die_Wahl_bleibt()
    {
        var viele = new List<Katalogfilterzeile>();
        for (int i = 0; i < 150; i++)
            viele.Add(Zeile(1000 + i, "Region " + i.ToString("D3", CultureInfo.InvariantCulture),
                            "PVGIS-Testreferenzjahr (weltweit)", "Ort " + i,
                            7.0 + i / 100.0, 47.0 + i / 100.0, "2026-09-18", false));

        var cut = Zeige(regionen: viele);

        Assert.Contains("150", cut.Find("span.epos-katalog-treffer").TextContent);

        cut.Find("label.epos-katalog-suchfeld input").Input("Region 007");
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("button.epos-anlagenwahl")),
                             TimeSpan.FromSeconds(10));

        cut.FindAll("button.epos-anlagenwahl")[0].Click();
        cut.WaitForAssertion(() => Assert.Equal("Region 007", cut.Instance.Gewaehlt),
                             TimeSpan.FromSeconds(10));
    }

    // =====================================================================
    //  KL-5 — der Bau der Maske (die Überlagerung)
    // =====================================================================

    /// <summary>
    /// <b>Der Befund KL‑5.</b> Bei 1 180 × 780 und 35 Regionen malten Reiterleiste
    /// und Diagrammkasten über die Listenzeilen, der Eingabeblock über die
    /// Fußleiste: Von „Löschen" war nur „…öschen" zu lesen, „Beenden" halb
    /// verdeckt. Die Ursache lag nicht hier, sondern im gemeinsamen
    /// <c>Katalograhmen</c> (gemessen in <c>Proben/Rasterprobe/katalogprobe.mjs</c>);
    /// behoben ist sie dort.
    ///
    /// <para>Dieser Fall hält den BAU der Maske fest, auf dem die Behebung ruht:
    /// Die Liste steht in ihrer Rasterhülle IM Listenblock, die Reiter stehen im
    /// Eingabeblock, und die Fußleiste steht AUSSERHALB des Rahmens. Wandert eines
    /// davon, greift die Regel des Rahmens nicht mehr.</para>
    ///
    /// <para>Die MASSE prüft nur der Browser — bunit hat kein Layout und sieht eine
    /// Überlagerung grundsätzlich nicht.</para>
    /// </summary>
    [Fact]
    public void Liste_Reiter_und_Fussleiste_stehen_an_ihren_Plaetzen()
    {
        var cut = Zeige();

        // Die Liste steht in ihrer Huelle IM Listenblock des Rahmens - die Huelle
        // traegt die Hoechsthoehe (1,3 x --epos-listenhoehe, siehe KatalograhmenTests).
        Assert.Single(cut.FindAll(".epos-katalog-paar > .epos-katalog-liste .epos-raster-huelle"));

        // Die Reiter mit den zwei Diagrammen stehen im EINGABEblock, nicht daneben.
        Assert.Single(cut.FindAll(".epos-katalog-paar > .epos-katalog-eingabe .epos-reiter-leiste"));
        Assert.Equal(2, cut.FindAll(".epos-katalog-paar > .epos-katalog-eingabe .epos-reiter-knopf").Count);

        // Die Fussleiste ist ein direktes Kind der Dialogwurzel und steht damit
        // UNTER dem Rahmen - nie in ihm.
        var fuss = cut.FindAll(".epos-katalog-dialog > .epos-leiste").Last();
        var beschriftungen = fuss.QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Contains("Daten einlesen", beschriftungen);
        Assert.Contains("Beenden", beschriftungen);
        var imRahmen = cut.FindAll(".epos-katalog-paar .epos-leiste button")
                          .Select(b => b.TextContent.Trim()).ToList();
        Assert.DoesNotContain("Daten einlesen", imRahmen);
        Assert.DoesNotContain("Beenden", imRahmen);
    }

    /// <summary>
    /// „Daten einlesen" ist ein GEWÖHNLICHER Knopf. Der Anwender sah ihn „vor
    /// Elementen" stehen — das kam von der gestauchten Rasterreihe des Rahmens und
    /// nicht von einer eigenen Lage. Ein <c>position</c> oder <c>z-index</c> am
    /// Knopf wäre die falsche Antwort darauf und bleibt deshalb hier verboten.
    /// </summary>
    [Fact]
    public void Daten_einlesen_traegt_keine_eigene_Lage()
    {
        var cut = Zeige();

        var knopf = cut.FindAll("button").First(b => b.TextContent.Trim() == "Daten einlesen");

        Assert.Equal("epos-knopf", knopf.ClassName);
        Assert.False(knopf.HasAttribute("style"));
    }


    // =====================================================================
    //  Die Farbe einer Reihe am Bild (Farbrollen, Bedienung Teil 2)
    // =====================================================================

    /// <summary>Der Baustein des GEZEIGTEN Reiters — je Reiter steht genau einer.</summary>
    private static DiagrammSvg Bild(IRenderedComponent<KlimadatenDialog> cut)
        => cut.FindComponents<DiagrammSvg>()[0].Instance;

    /// <summary>
    /// Wählt die Region Nr. <paramref name="nr"/> — ein Bild gibt es erst mit einer
    /// gewählten Region, ohne sie steht der Platzhalter.
    /// </summary>
    private static void Region(IRenderedComponent<KlimadatenDialog> cut, int nr)
        => cut.FindAll("button.epos-anlagenwahl")[nr].Click();

    /// <summary>
    /// <b>Beide Reiter zeigen ein SVG, nicht ein Bild.</b> Der Zoom auf der
    /// Zeitachse liegt damit im Browser: Über jedem Bild stehen „Bereich" und
    /// „1:1", darunter die Zeile mit den Werten am Mauszeiger.
    /// </summary>
    [Fact]
    public void Beide_Reiter_zeigen_das_Diagramm_als_SVG()
    {
        var cut = Zeige();
        Region(cut, 0);

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("svg.epos-flaeche")),
                             TimeSpan.FromSeconds(10));
        Assert.Equal(new[] { "Bereich", "1:1" },
                     cut.FindAll("button.epos-diagramm-knopf")
                        .Select(k => k.TextContent.Trim()).ToArray());
        Assert.Single(cut.FindAll(".epos-diagramm-zeigerzeile"));

        cut.FindAll("button[role='tab']")[1].Click();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("svg.epos-flaeche")),
                             TimeSpan.FromSeconds(10));
        Assert.Equal(new[] { "Bereich", "1:1" },
                     cut.FindAll("button.epos-diagramm-knopf")
                        .Select(k => k.TextContent.Trim()).ToArray());
    }

    /// <summary>
    /// <b>Die Einheit steht je Reiter</b> — Grad Celsius am Temperaturbild, Grad am
    /// Sonnenwinkel. Sie ist das, was in der Zeigerzeile hinter dem Wert steht.
    /// </summary>
    [Fact]
    public void Jeder_Reiter_traegt_seine_Einheit()
    {
        var cut = Zeige();
        Region(cut, 0);

        cut.WaitForAssertion(() => Assert.Equal("°C", Bild(cut).Einheit),
                             TimeSpan.FromSeconds(10));

        cut.FindAll("button[role='tab']")[1].Click();

        cut.WaitForAssertion(() => Assert.Equal("°", Bild(cut).Einheit),
                             TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// <b>Ohne den Delegaten gibt es keinen Farbwähler.</b> Ein Wähler, dessen Wahl
    /// niemand speichert, wäre eine Behauptung — dieselbe Hausregel wie überall:
    /// kein Delegat, kein Bedienelement.
    /// </summary>
    [Fact]
    public void Ohne_FarbeSetzen_bleibt_der_Farbwaehler_weg()
    {
        var cut = Zeige();
        Region(cut, 0);

        cut.WaitForAssertion(() => Assert.False(Bild(cut).FarbwahlErlaubt),
                             TimeSpan.FromSeconds(10));
        Assert.Empty(cut.FindAll(".epos-legende-farbfeld"));
    }

    /// <summary>
    /// <b>Mit dem Delegaten kommt die Wahl bei der Hülle an — mit Rolle und
    /// Farbe.</b> Der Dialog rechnet nichts um: Die Rolle steht im Modell, die
    /// Farbe kommt aus dem Wähler.
    /// </summary>
    [Fact]
    public async Task FarbeSetzen_bekommt_Rolle_und_Farbe()
    {
        var rufe = new List<(Farbrolle Rolle, Farbe Farbe)>();

        var cut = Zeige(farbeSetzen: (r, f) => { rufe.Add((r, f)); return Task.CompletedTask; });
        Region(cut, 0);

        cut.WaitForAssertion(() => Assert.True(Bild(cut).FarbwahlErlaubt),
                             TimeSpan.FromSeconds(10));

        // Das Farbfeld des Legendeneintrags oeffnet den Waehler; dort meldet das
        // Farbfeld des Hauses die gewaehlte Farbe.
        cut.FindAll(".epos-legende-farbfeld")[0].Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-farbwahl")),
                             TimeSpan.FromSeconds(10));

        await cut.InvokeAsync(() =>
            cut.Find(".epos-farbwahl input.epos-farbfeld-waehler").Change("#123456"));

        Assert.Single(rufe);
        Assert.Equal(Farbrolle.AUSSENTEMPERATUR, rufe[0].Rolle);
        Assert.Equal(0x12, rufe[0].Farbe.R);
        Assert.Equal(0x34, rufe[0].Farbe.G);
        Assert.Equal(0x56, rufe[0].Farbe.B);

        // Die DECKUNG bleibt die der Hausfarbe - der Anwender waehlt den Farbton.
        Assert.Equal(Farbpalette.Vorgabe[Farbrolle.AUSSENTEMPERATUR].A, rufe[0].Farbe.A);
    }

    /// <summary>
    /// <b>„Hausfarbe" nimmt die Wahl zurück</b> — die Rolle fällt aus der
    /// Einstellung, und eine später geänderte Hausfarbe erreicht den Anwender
    /// wieder.
    /// </summary>
    [Fact]
    public void Hausfarbe_meldet_die_Rolle_zurueck()
    {
        var rollen = new List<Farbrolle>();

        var cut = Zeige(farbeSetzen: (_, _) => Task.CompletedTask,
                        farbeZuruecksetzen: r => { rollen.Add(r); return Task.CompletedTask; });
        Region(cut, 0);

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-legende-farbfeld")),
                             TimeSpan.FromSeconds(10));

        cut.FindAll(".epos-legende-farbfeld")[0].Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-farbwahl")),
                             TimeSpan.FromSeconds(10));

        cut.Find(".epos-farbwahl button.epos-knopf").Click();

        Assert.Equal(new[] { Farbrolle.AUSSENTEMPERATUR }, rollen);
    }

    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F3)
    // =====================================================================

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Sie bindet über die
    /// Sichtklasse <c>KlimadatenKiSicht</c>: Die Brücke setzt den Standort, und die
    /// Maske trägt ihn danach in ihren Feldern.
    /// </summary>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_setzt_den_Standort()
    {
        var cut = Zeige();

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.KLIMADATEN));

        WindowsFormsApplication1.KiFeldzugang laenge =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.KLIMADATEN, "laengengrad");
        Assert.NotNull(laenge);
        Assert.True(laenge.Setzbar);

        laenge.Setzen(9.18);
        cut.Render();
        Assert.Equal(9.18, laenge.Lesen());

        WindowsFormsApplication1.KiFeldzugang bezeichnung =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.KLIMADATEN, "bezeichnung");
        bezeichnung.Setzen("Stuttgart");
        cut.Render();
        Assert.Equal("Stuttgart", bezeichnung.Lesen());

        // Der Dateipfad kommt aus dem Dateidialog der Plattform - er ist nur lesbar.
        WindowsFormsApplication1.KiFeldzugang datei =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.KLIMADATEN, "try_datei");
        Assert.NotNull(datei);
        Assert.False(datei.Setzbar);
    }

    /// <summary>
    /// <b>Die Quelle ist ein WAHLFELD</b> (KI-D-Q6): Gesetzt wird über ihren
    /// Anzeigetext, und danach zeigt die Maske die Felder dieser Quelle.
    /// </summary>
    [Fact]
    public void Der_Assistent_waehlt_die_Datenquelle_ueber_ihren_Text()
    {
        var cut = Zeige();

        WindowsFormsApplication1.KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.KLIMADATEN, "quelle");
        Assert.NotNull(zugang);
        Assert.Equal((int)KlimaQuelle.PvgisTmy, zugang.Lesen());

        KiFeldumsetzung umsetzung =
            KiFeldwandler.Wandle(zugang,
                                 WindowsFormsApplication1.MyResource.Resource.KLIMA_QUELLE_TRY_DATEI);
        Assert.True(umsetzung.Ok, umsetzung.Grund);
        zugang.Setzen(umsetzung.Wert);
        cut.Render();

        Assert.Equal((int)KlimaQuelle.TryDatei, zugang.Lesen());
    }
}
