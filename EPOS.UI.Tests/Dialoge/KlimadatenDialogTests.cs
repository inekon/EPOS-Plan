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
public class KlimadatenDialogTests : EposBunitContext
{
    private static readonly byte[] BILD = { 1, 2, 3, 4 };

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
        Func<KlimaImportAuftrag, Task<KlimaVorschauErgebnis>>? regionErmitteln = null)
    {
        IReadOnlyList<Katalogfilterzeile> liste = regionen ?? Regionen();
        return Render<KlimadatenDialog>(p => p
            .Add(x => x.Regionen, () => Task.FromResult(liste))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
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
                "PVGIS-SARAH3", 13.4, 52.5, BILD, BILD, ""));
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
        // Temperaturbild, nach dem Wechsel das Sonnenwinkelbild.
        Assert.Single(cut.FindComponents<EPOS.UI.Standards.ChartBild>());
        Assert.Equal("Jahrestemperatur Verlauf",
                     cut.FindComponent<EPOS.UI.Standards.ChartBild>().Instance.Alt);

        cut.FindAll(".epos-reiter-knopf")[1].Click();
        cut.WaitForAssertion(() => Assert.Equal("Sonnenwinkel Verlauf",
                     cut.FindComponent<EPOS.UI.Standards.ChartBild>().Instance.Alt),
                             TimeSpan.FromSeconds(10));
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
}
