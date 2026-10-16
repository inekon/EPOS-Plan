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
        Func<Farbrolle, Task>? farbeZuruecksetzen = null,
        EPOS.UI.Bausteine.Schlossweg? schloss = null)
    {
        IReadOnlyList<Katalogfilterzeile> liste = regionen ?? Regionen();
        return Render<KlimadatenDialog>(p => p
            .Add(x => x.Regionen, () => Task.FromResult(liste))
            .Add(x => x.Schloss, schloss)
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

    /// <summary>
    /// „Import…" in der Fußleiste öffnet die Überlagerung „Klimadaten einlesen"
    /// (Neuordnung Stufe 4, V14) — erst dann stehen Quelle, Standort und „Daten einlesen".
    /// </summary>
    private static void ImportOeffnen(IRenderedComponent<KlimadatenDialog> cut)
    {
        cut.Find("button.epos-importknopf").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-einlesen")), TimeSpan.FromSeconds(10));
    }

    /// <summary>Der Dialog mit offener Überlagerung „Klimadaten einlesen".</summary>
    private IRenderedComponent<KlimadatenDialog> ZeigeImport(
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
        var cut = Zeige(regionen, ansicht, importieren, abbrechen, loeschen, orte, geschlossen,
                        dateiWaehlen, regionErmitteln);
        ImportOeffnen(cut);
        return cut;
    }

    /// <summary>Schaltet die Klimaquelle über ihr Optionsfeld um.</summary>
    private static void QuelleWaehlen(IRenderedComponent<KlimadatenDialog> cut, KlimaQuelle quelle)
        => cut.FindAll("input[type=radio]")[(int)quelle].Change(true);

    // =====================================================================
    //  Feldbestand (Feldkarte Form_Klimadaten)
    // =====================================================================

    [Fact]
    public void Der_Dialog_zeigt_Liste_Eingaben_und_zwei_Reiter()
    {
        var cut = Zeige();

        // Die drei Regionen der Liste; die erste steht beim Oeffnen im Stammblatt
        // (Konzept 3.3: das Blatt ist nie leer, solange die Liste Treffer hat).
        Assert.Equal(3, Zeilenklick.Zeilen(cut).Count);
        Assert.Contains("Stuttgart", cut.Markup);
        cut.WaitForAssertion(() => Assert.Equal("Berlin", cut.Instance.Gewaehlt), TimeSpan.FromSeconds(10));

        // Zwei Reiter mit je einem Bild - in der Gruppe Jahresverlauf des Stammblatts.
        var reiter = cut.FindAll(".epos-stammblatt .epos-reiter-knopf").Select(e => e.TextContent.Trim()).ToList();
        // KD-8 Teil A (KD-Q13): dazu der dritte Reiter „Weitere Reihen“.
        Assert.Equal(new[] { "Temperatur", "Sonnenwinkel", "Weitere Reihen" }, reiter);

        // Die Eingaben des Imports stehen NICHT im Dialog, sondern in der
        // Ueberlagerung hinter "Import..." (V14).
        Assert.Empty(cut.FindAll("input[list=epos-klimaregion-orte]"));
        Assert.Empty(cut.FindAll("input[inputmode=decimal]"));

        // Loeschen in der Auswahlleiste, Import... und Beenden in der Fussleiste.
        var knoepfe = cut.FindAll("button").Select(e => e.TextContent.Trim()).ToList();
        Assert.Contains("Löschen", cut.Find(".epos-auswahlleiste").TextContent);
        Assert.Contains("Import…", knoepfe);
        Assert.Contains("Beenden", knoepfe);
        Assert.DoesNotContain("Daten einlesen", knoepfe);

        // In der Ueberlagerung: Ortsfeld (freie Eingabe MIT Vorschlagsliste), zwei
        // Zahlenfelder, Bezeichnung und "Daten einlesen".
        ImportOeffnen(cut);
        Assert.Single(cut.FindAll(".epos-ueberlagerung input[list=epos-klimaregion-orte]"));
        Assert.Equal(3, cut.FindAll(".epos-ueberlagerung input[inputmode=decimal]").Count);   // Longitude, Latitude, Hoehe (KD-3)
        Assert.Contains("Daten einlesen", cut.FindAll(".epos-ueberlagerung button").Select(b => b.TextContent.Trim()));
    }

    /// <summary>
    /// Befund W14c-B15 / Entscheid E-7: <b>Fehlt die Ortsliste, öffnet der Dialog
    /// trotzdem</b> — er zeigt dann keine Vorschläge, das Feld bleibt frei
    /// beschreibbar. Der Vorläufer warf in <c>Load</c> und öffnete gar nicht.
    /// </summary>
    [Fact]
    public void Ohne_Ortsliste_oeffnet_der_Dialog_und_das_Feld_bleibt_frei()
    {
        var cut = ZeigeImport(orte: Array.Empty<string>());

        Assert.Empty(cut.FindAll("#epos-klimaregion-orte option"));
        Assert.False(cut.Find("input[list=epos-klimaregion-orte]").HasAttribute("readonly"));
    }

    [Fact]
    public void Mit_Ortsliste_stehen_die_Vorschlaege_da()
    {
        var cut = ZeigeImport(orte: new[] { "Berlin", "Hamburg", "München" });

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

        Zeilenklick.Zeile(cut, 0);

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

        Zeilenklick.Zeile(cut, 0);

        // W16b-O-2 (Gate 10.09.2026): erst auf die gezeichnete Meldung warten,
        // statt sofort nach dem Click zu pruefen - siehe die Begruendung unten
        // bei Der_Fortschritt_meldet_die_Schritte_und_laesst_sich_abbrechen.
        cut.WaitForAssertion(() => Assert.Contains("keine Stundenwerte", cut.Instance.Meldung),
                             TimeSpan.FromSeconds(10));
    }

    // =====================================================================
    //  Loeschen - A-7 (Rueckfrage) und A-8 (Kaskade)
    // =====================================================================

    /// <summary>
    /// Ohne Regionen gibt es keine Fokuszeile — an der Stelle der Auswahlleiste steht
    /// eine leise Zeile, und „Löschen" gibt es nicht (Konzept 3.3).
    /// </summary>
    [Fact]
    public void Ohne_Auswahl_bleibt_Loeschen_gesperrt()
    {
        var cut = Zeige(regionen: Array.Empty<Katalogfilterzeile>());

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-auswahlleiste .epos-auswahlleiste-leise")),
                             TimeSpan.FromSeconds(10));
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Löschen");
        Assert.Equal("", cut.Instance.Gewaehlt);
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

        Zeilenklick.Zeile(cut, 0);
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

        Zeilenklick.Zeile(cut, 2);       // "Auslieferung Nord"
        cut.WaitForAssertion(() => Assert.True(cut.Instance.Auslieferungssatz), TimeSpan.FromSeconds(10));

        // Stufe 4 (V13): Loeschen ist WEICH gesperrt - aria-disabled mit dem Grund im
        // Kurztext, ohne das Wort "Duplizieren" (Klimadaten kennen es nicht).
        var loeschen = cut.FindAll(".epos-auswahlleiste button").First(b => b.TextContent.Trim() == "Löschen");
        Assert.Equal("true", loeschen.GetAttribute("aria-disabled"));
        Assert.False(loeschen.HasAttribute("disabled"));
        Assert.Equal("Auslieferungssatz – Löschen gesperrt. Zuerst „Schloss aufheben...“.", loeschen.GetAttribute("title"));

        loeschen.Click();

        // W16b-O-2 (Gate 10.09.2026): bunits Click() wartet den Ereignisbehandler
        // nicht ab - deshalb auf das gezeichnete Ergebnis warten.
        cut.WaitForAssertion(() => Assert.Contains("Löschen gesperrt", cut.Instance.Meldung),
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
        var cut = ZeigeImport();

        var einlesen = cut.FindAll("button").First(b => b.TextContent.Trim() == "Daten einlesen");
        Assert.True(einlesen.HasAttribute("disabled"));
    }

    [Fact]
    public void Ein_Ortsname_reicht_fuer_den_Import()
    {
        KlimaImportAuftrag? auftrag = null;
        var cut = ZeigeImport(importieren: (a, _) =>
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
        var cut = ZeigeImport(importieren: (a, _) =>
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

        cut.FindAll("input[type=text]:not([inputmode])").Last().Input("Eigen");
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
        var cut = ZeigeImport(importieren: (_, _) => Task.FromResult(new KlimaImportErgebnis
        {
            Ausgang = KlimaImportAusgang.Dublette,
            Meldung = "Die Klimaregion „Berlin\" gibt es bereits."
        }));

        cut.Find("input[list=epos-klimaregion-orte]").Input("Berlin");
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Daten einlesen").Click();

        // W16b-O-2 (Gate 10.09.2026): BeiImport meldet und setzt _laeuft im
        // finally-Block - erst auf das gezeichnete Ergebnis warten.
        cut.WaitForAssertion(() => Assert.Contains("gibt es bereits", cut.Instance.Importmeldung),
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
        var cut = ZeigeImport(abbrechen: null);
        Assert.Empty(cut.FindAll(".epos-fortschritt button"));
    }

    [Fact]
    public void Der_Fortschritt_meldet_die_Schritte_und_laesst_sich_abbrechen()
    {
        int abgebrochen = 0;
        var tcs = new TaskCompletionSource<KlimaImportErgebnis>();

        var cut = ZeigeImport(
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
        Assert.Contains("abgebrochen", cut.Instance.Importmeldung);
        Assert.True(cut.Instance.ImportOffen);
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

        Zeilenklick.Zeile(cut, 0);
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Löschen").Click();
        cut.Find("div.epos-katalog-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(antwort);      // BeiTaste liefert bei offener Rueckfrage nichts - kein Wettlauf

        cut.FindComponent<EPOS.UI.Bausteine.Rueckfrage>()
           .FindAll("button").First(b => b.TextContent.Trim() == "Nein").Click();
        cut.Find("div.epos-katalog-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        // W16b-O-2 (Gate 10.09.2026): BeiTaste ruft hier Geschlossen.InvokeAsync
        // ueber den Renderer-Dispatcher - derselbe Wettlauf wie bei Beenden_liefert_OK.
        // Esc wirkt wie "Beenden" und meldet true (Konzept Administrationsdialoge, V15).
        cut.WaitForAssertion(() => Assert.True(antwort), TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// <b>„Das Kreuz steht beim Titel"</b> (Anwenderentscheid 15.09.2026): Das ✕ der
    /// Kopfzeile wirkt genau wie Esc — und beide wie „Beenden" (Konzept
    /// Administrationsdialoge, V15): Es meldet <c>true</c>.
    /// </summary>
    [Fact]
    public void Das_Kreuz_im_Kopf_schliesst_wie_Esc()
    {
        bool? antwort = null;
        var cut = Zeige(geschlossen: b => antwort = b);

        cut.Find(".epos-dialog-zu").Click();

        // Derselbe Wettlauf wie oben: Geschlossen.InvokeAsync laeuft ueber den Dispatcher.
        cut.WaitForAssertion(() => Assert.True(antwort), TimeSpan.FromSeconds(10));
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
        var cut = ZeigeImport();

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
        var cut = ZeigeImport(dateiWaehlen: _ => Task.FromResult<string?>("/tmp/probe.dat"));

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
        var cut = ZeigeImport(dateiWaehlen: _ => Task.FromResult<string?>("/tmp/data.zip"));

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
        var cut = ZeigeImport(dateiWaehlen: _ => Task.FromResult<string?>("/tmp/probe.dat"));

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
        var cut = ZeigeImport();

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
        var cut = ZeigeImport(
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
        var cut = ZeigeImport(importieren: (a, _) =>
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
        var cut = ZeigeImport(dateiWaehlen: null);

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
            var cut = ZeigeImport(dateiWaehlen: _ => Task.FromResult<string?>(pfad));

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
                         cut.FindAll("input[type=text]:not([inputmode])").Last().GetAttribute("value"));

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
            var cut = ZeigeImport(
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
            var cut = ZeigeImport(dateiWaehlen: _ => Task.FromResult<string?>(pfad));

            QuelleWaehlen(cut, KlimaQuelle.TryDatei);
            cut.FindComponent<EPOS.UI.Standards.Dateiwahl>()
               .FindAll("button").First(b => b.TextContent.Trim().StartsWith("Durchsuchen")).Click();

            cut.WaitForAssertion(() => Assert.NotEmpty(cut.Instance.Importmeldung),
                                 TimeSpan.FromSeconds(10));

            Assert.Contains("nicht lesbar", cut.Instance.Importmeldung);
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
    //  DATENSATZ SUCHEN und die VORSCHAUKARTE (Auftrag KD-3, Konzept
    //  Klimadatenimport § 4, Entscheide KD-Q1, KD-Q3, KD-Q9)
    // =====================================================================

    /// <summary>Der Knopf „Datensatz suchen"; null, wenn er fehlt.</summary>
    private static AngleSharp.Dom.IElement? Suchknopf(IRenderedComponent<KlimadatenDialog> cut)
        => cut.FindAll("button").FirstOrDefault(b => b.TextContent.Trim() == "Datensatz suchen");

    /// <summary>Das Feld „Höhe des Ortes (m)" — das dritte Zahlenfeld des Standorts.</summary>
    private static AngleSharp.Dom.IElement Hoehenfeld(IRenderedComponent<KlimadatenDialog> cut)
        => cut.FindAll("input[inputmode=decimal]")[2];

    /// <summary>
    /// Eine Vorschau, wie sie der Kern liefert: TRY 2045 sommerwarm, Stuttgart-Vaihingen
    /// gegen die Repräsentanzstation Stötten (Nachrechnung Konzept 1.2).
    /// </summary>
    private static KlimaVorschauErgebnis Treffer(double entfernungKm = 56, double? bezugHoeheM = 734,
                                                 bool gefunden = true, double? standortHoeheM = null)
    {
        var kz = new KlimaKennzahlen
        {
            Stunden = 8760, Tage = 365, TemperaturMittel = 9.9, TemperaturMin = -12.4, TemperaturMax = 34.3,
            GlobalstrahlungKwhM2 = 1147, GradtagzahlKdA = 3105, Heiztage = 209
        };
        for (int m = 0; m < 12; m++)
        {
            kz.MonatsmittelTemperatur[m] = 10 - 9 * Math.Cos(2 * Math.PI * (m + 0.5) / 12);
            kz.MonatssummeGlobalstrahlungKwhM2[m] = 95 - 70 * Math.Cos(2 * Math.PI * (m + 0.5) / 12);
        }
        return new KlimaVorschauErgebnis
        {
            Ausgang = KlimaImportAusgang.Erfolg,
            Quelle = KlimaQuelle.TryRegional,
            Bezugsjahr = 2045,
            Szenario = TrySzenario.Sommerwarm,
            AusfuehrungGefunden = gefunden,
            Grund = gefunden ? "" : "Das Paket führt für diese Region nur das mittlere Jahr.",
            Ausfuehrungen = new List<TryAusfuehrung>
            {
                new() { Jahr = 2015, Szenario = TrySzenario.MittleresJahr },
                new() { Jahr = 2045, Szenario = TrySzenario.MittleresJahr }
            },
            Standort = new KlimaStandort
            {
                Latitude = 48.7289, Longitude = 9.1085, Anzeigename = "Vaihingen, Stuttgart, Deutschland",
                AusGeokodierung = true, HoeheM = standortHoeheM
            },
            Bezugspunkt = new KlimaBezugspunkt
            {
                Art = KlimaBezugspunktArt.Repraesentanzstation, Name = "Region 14 Stötten",
                Latitude = 48.6536, Longitude = 9.8666, HoeheM = bezugHoeheM, EntfernungKm = entfernungKm
            },
            Kennzahlen = gefunden ? kz : null,
            Parameterbestand = new List<KlimaParameterEintrag>
            {
                new() { Parameter = KlimaParameter.Lufttemperatur, Stand = KlimaParameterStand.Gefuehrt },
                new() { Parameter = KlimaParameter.DirektstrahlungNormal, Stand = KlimaParameterStand.Gerechnet },
                new() { Parameter = KlimaParameter.QualitaetStrahlung, Stand = KlimaParameterStand.Fehlt }
            },
            Repraesentanzhinweis = true,
            Hinweise = new List<string> { WindowsFormsApplication1.MyResource.Resource.KLIMA_VORSCHAU_REPRAESENTANZ }
        };
    }

    /// <summary>Der Dialog mit offenem Import, Regionaldaten 2045 sommerwarm und Ort „Stuttgart-Vaihingen".</summary>
    private IRenderedComponent<KlimadatenDialog> ZeigeSuche(
        Func<KlimaImportAuftrag, Task<KlimaVorschauErgebnis>> suche,
        Func<KlimaImportAuftrag, IProgress<ImportFortschritt>, Task<KlimaImportErgebnis>>? importieren = null)
    {
        var cut = ZeigeImport(importieren: importieren, regionErmitteln: suche);
        QuelleWaehlen(cut, KlimaQuelle.TryRegional);
        cut.FindAll("select")[0].Change("2045");
        cut.FindAll("select")[1].Change(((int)TrySzenario.Sommerwarm).ToString(CultureInfo.InvariantCulture));
        cut.Find("input[list=epos-klimaregion-orte]").Input("Stuttgart-Vaihingen");
        return cut;
    }

    /// <summary>Sucht und wartet, bis die Karte steht.</summary>
    private static void Suchen(IRenderedComponent<KlimadatenDialog> cut)
    {
        Suchknopf(cut)!.Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Vorschau), TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// <b>„Datensatz suchen" gilt für ALLE drei Quellen</b> (KD-3) — PVGIS, TRY-Datei und
    /// Regionaldaten; bei der TRY-Datei erst mit Pfad.
    /// </summary>
    [Fact]
    public void Datensatz_suchen_steht_bei_allen_drei_Quellen()
    {
        var cut = ZeigeImport(regionErmitteln: _ => Task.FromResult(Treffer()));
        cut.Find("input[list=epos-klimaregion-orte]").Input("Geislingen");

        Assert.False(Suchknopf(cut)!.HasAttribute("disabled"));     // PVGIS

        QuelleWaehlen(cut, KlimaQuelle.TryDatei);
        Assert.True(Suchknopf(cut)!.HasAttribute("disabled"));      // ohne Datei

        QuelleWaehlen(cut, KlimaQuelle.TryRegional);
        Assert.False(Suchknopf(cut)!.HasAttribute("disabled"));
        Assert.Null(cut.FindAll("button").FirstOrDefault(b => b.TextContent.Trim() == "Region ermitteln"));
    }

    /// <summary>Ohne Standort bleibt der Knopf gesperrt; ohne Delegat auch — und „Daten einlesen" ist dann frei.</summary>
    [Fact]
    public void Ohne_Standort_oder_Delegat_bleibt_der_Suchknopf_gesperrt()
    {
        var mit = ZeigeImport(regionErmitteln: _ => Task.FromResult(Treffer()));
        Assert.True(Suchknopf(mit)!.HasAttribute("disabled"));

        var ohne = ZeigeImport();
        ohne.Find("input[list=epos-klimaregion-orte]").Input("Geislingen");
        Assert.True(Suchknopf(ohne)!.HasAttribute("disabled"));
        Assert.False(Einlesen(ohne).HasAttribute("disabled"));
        Assert.False(Einlesen(ohne).HasAttribute("aria-disabled"));
    }

    /// <summary>
    /// <b>Ein Treffer</b>: Kennzeichen „Datensatz gefunden: TRY 2045 · sommerwarm", Standort
    /// und Bezugspunkt mit Entfernung und Höhe, Kennzahlen, Bild aus den Monatsreihen,
    /// Parameterbestand — und Longitude, Latitude und Bezeichnung stehen in den Feldern.
    /// „Daten einlesen" ist frei.
    /// </summary>
    [Fact]
    public void Ein_Treffer_zeigt_die_Karte_und_befuellt_die_Felder()
    {
        KlimaImportAuftrag? gefragt = null;
        var cut = ZeigeSuche(a => { gefragt = a; return Task.FromResult(Treffer()); });
        Assert.Equal("true", Einlesen(cut).GetAttribute("aria-disabled"));

        Suchen(cut);

        Assert.Equal(KlimaQuelle.TryRegional, gefragt!.Quelle);
        Assert.Equal(2045, gefragt.TryJahr);
        Assert.Equal(TrySzenario.Sommerwarm, gefragt.Szenario);

        string karte = cut.Find(".epos-klimavorschau").TextContent;
        Assert.Contains("Datensatz gefunden: TRY 2045 · sommerwarm", karte);
        Assert.Single(cut.FindAll(".epos-klimavorschau-treffer--ja"));
        Assert.Contains("Region 14 Stötten", karte);
        Assert.Contains("56 km", karte);
        Assert.Contains("734 m", karte);
        Assert.Contains("1.147 kWh/m²a", karte);
        Assert.Contains("3.105 Kd/a", karte);
        Assert.Contains("209", karte);
        Assert.Contains("Temperatur", karte);
        Assert.Contains("(gerechnet)", karte);
        Assert.Contains("(fehlt)", karte);
        Assert.NotEmpty(cut.FindAll(".epos-klimavorschau svg"));

        // Die Felder: Vorschlag aus dem Standort, die Bezeichnung heisst wie der Ort.
        var zahlen = cut.FindAll("input[inputmode=decimal]");
        Assert.StartsWith("9,1085", zahlen[0].GetAttribute("value"));
        Assert.StartsWith("48,7289", zahlen[1].GetAttribute("value"));
        Assert.Equal("Stuttgart-Vaihingen", cut.FindAll("input[type=text]:not([inputmode])").Last().GetAttribute("value"));
        Assert.Contains("aus der Suche vorbelegt", cut.Markup);

        Assert.True(cut.Instance.VorschauGueltig);
        Assert.False(Einlesen(cut).HasAttribute("aria-disabled"));
    }

    /// <summary>
    /// <b>Kein Treffer</b> (das Szenario fehlt im Paket): Kennzeichen „nicht im Paket" mit
    /// Grund und den vorhandenen Ausführungen als Wahlknöpfen; ein Klick setzt Jahr und
    /// Szenario und sucht neu. Ohne Treffer bleibt „Daten einlesen" weich gesperrt.
    /// </summary>
    [Fact]
    public void Kein_Treffer_bietet_die_vorhandenen_Ausfuehrungen()
    {
        var auftraege = new List<KlimaImportAuftrag>();
        var cut = ZeigeSuche(a =>
        {
            auftraege.Add(a);
            return Task.FromResult(Treffer(gefunden: a.Szenario == TrySzenario.MittleresJahr));
        });

        Suchen(cut);

        string karte = cut.Find(".epos-klimavorschau").TextContent;
        Assert.Contains("TRY 2045 · sommerwarm ist nicht im Paket.", karte);
        Assert.Contains("nur das mittlere Jahr", karte);
        Assert.Single(cut.FindAll(".epos-klimavorschau-treffer--nein"));
        Assert.Equal("true", Einlesen(cut).GetAttribute("aria-disabled"));
        Assert.False(cut.Instance.VorschauGueltig);

        cut.FindAll("button.epos-klimavorschau-wahl").First(b => b.TextContent.Trim() == "TRY 2015 · mittleres Jahr").Click();

        cut.WaitForAssertion(() => Assert.Equal(2, auftraege.Count), TimeSpan.FromSeconds(10));
        Assert.Equal(2015, auftraege[1].TryJahr);
        Assert.Equal(TrySzenario.MittleresJahr, auftraege[1].Szenario);
        cut.WaitForAssertion(() => Assert.True(cut.Instance.VorschauGueltig), TimeSpan.FromSeconds(10));
        Assert.False(Einlesen(cut).HasAttribute("aria-disabled"));
    }

    /// <summary>
    /// <b>Eine geänderte Eingabe lässt die Karte sichtbar VERFALLEN</b> — Ort, Quelle,
    /// Jahr oder Szenario; „Daten einlesen" ist dann wieder weich gesperrt.
    /// </summary>
    [Fact]
    public void Eine_geaenderte_Eingabe_laesst_die_Karte_verfallen()
    {
        var cut = ZeigeSuche(_ => Task.FromResult(Treffer()));
        Suchen(cut);
        Assert.True(cut.Instance.VorschauGueltig);

        cut.FindAll("select")[1].Change(((int)TrySzenario.Winterkalt).ToString(CultureInfo.InvariantCulture));
        Assert.True(cut.Instance.VorschauVerfallen);
        Assert.Empty(cut.FindAll(".epos-klimavorschau"));
        Assert.Contains("die Vorschau ist verfallen", cut.Markup);
        Assert.Equal("true", Einlesen(cut).GetAttribute("aria-disabled"));

        // Zurueck auf die gesuchte Eingabe: die Karte gilt wieder.
        cut.FindAll("select")[1].Change(((int)TrySzenario.Sommerwarm).ToString(CultureInfo.InvariantCulture));
        Assert.True(cut.Instance.VorschauGueltig);

        cut.Find("input[list=epos-klimaregion-orte]").Input("Hamburg");
        Assert.True(cut.Instance.VorschauVerfallen);

        cut.Find("input[list=epos-klimaregion-orte]").Input("Stuttgart-Vaihingen");
        QuelleWaehlen(cut, KlimaQuelle.PvgisTmy);
        Assert.True(cut.Instance.VorschauVerfallen);
    }

    /// <summary>
    /// <b>Ohne gültige Vorschau ist „Daten einlesen" WEICH gesperrt</b>: Der Knopf bleibt
    /// anklickbar (aria-disabled, title), liest aber nicht — die Leiste nennt den Grund.
    /// </summary>
    [Fact]
    public void Import_ohne_Vorschau_ist_weich_gesperrt()
    {
        int gelesen = 0;
        var cut = ZeigeSuche(_ => Task.FromResult(Treffer()),
                             (_, _) => { gelesen++; return Task.FromResult(new KlimaImportErgebnis()); });

        var knopf = Einlesen(cut);
        Assert.False(knopf.HasAttribute("disabled"));
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Contains("Datensatz suchen", knopf.GetAttribute("title"));

        knopf.Click();
        Assert.Equal(0, gelesen);
        Assert.Contains("ohne gültige Vorschau wird nichts übernommen", cut.Find(".epos-einlesen > .epos-leiste").TextContent);
    }

    /// <summary>
    /// <b>Die Höhe des Ortes geht in den Auftrag</b> — in die Suche und in „Daten einlesen"
    /// (<see cref="KlimaImportAuftrag.StandortHoeheM"/>); sie lässt die Karte nicht verfallen.
    /// </summary>
    [Fact]
    public void Die_Hoehe_geht_in_den_Auftrag()
    {
        KlimaImportAuftrag? gesucht = null, gelesen = null;
        var cut = ZeigeSuche(a => { gesucht = a; return Task.FromResult(Treffer()); },
                             (a, _) => { gelesen = a; return Task.FromResult(new KlimaImportErgebnis
                             {
                                 Ausgang = KlimaImportAusgang.Erfolg, Bezeichner = "Stuttgart-Vaihingen"
                             }); });

        Hoehenfeld(cut).Input("450");
        Suchen(cut);
        Assert.Equal(450.0, gesucht!.StandortHoeheM);

        Hoehenfeld(cut).Input("460");
        Assert.True(cut.Instance.VorschauGueltig);

        Einlesen(cut).Click();
        cut.WaitForAssertion(() => Assert.NotNull(gelesen), TimeSpan.FromSeconds(10));
        Assert.Equal(460.0, gelesen!.StandortHoeheM);
        Assert.Equal(KlimaQuelle.TryRegional, gelesen.Quelle);
    }

    /// <summary>
    /// <b>Der Hinweis „nicht ortstypisch" ab 50 km ODER 200 m</b> (KD-Q1, KD-Q9) — mit der
    /// aktuellen Höhe des Ortes; die Zeile zur Repräsentanzstation steht im Regionalweg immer.
    /// </summary>
    [Fact]
    public void Der_Hinweis_nicht_ortstypisch_steht_ab_50_km_oder_200_m()
    {
        const string HINWEIS = "Bezugspunkt nicht ortstypisch";

        var weit = ZeigeSuche(_ => Task.FromResult(Treffer(entfernungKm: 56)));
        Suchen(weit);
        Assert.Contains(HINWEIS, weit.Markup);
        Assert.Contains("Nächste Repräsentanzstation", weit.Markup);

        var nah = ZeigeSuche(_ => Task.FromResult(Treffer(entfernungKm: 12, bezugHoeheM: 734)));
        Suchen(nah);
        Assert.DoesNotContain(HINWEIS, nah.Markup);          // Hoehe unbekannt

        Hoehenfeld(nah).Input("700");                         // 34 m: kein Hinweis
        Assert.DoesNotContain(HINWEIS, nah.Markup);
        Assert.Contains("+34 m", nah.Find(".epos-klimavorschau").TextContent);

        Hoehenfeld(nah).Input("534");                         // genau 200 m: Hinweis
        Assert.Contains(HINWEIS, nah.Markup);
    }

    /// <summary>
    /// <b>Ein Fehlschlag steht als Zeile da und sperrt nur weich</b>: außerhalb der
    /// 300-km-Grenze, Ort unbekannt — der Knopf bleibt anklickbar.
    /// </summary>
    [Fact]
    public void Ein_Fehlschlag_der_Suche_steht_als_Zeile_da()
    {
        var cut = ZeigeSuche(_ => Task.FromResult(new KlimaVorschauErgebnis
        {
            Ausgang = KlimaImportAusgang.Dateifehler,
            Meldung = "Der nächste TRY-Regionsmittelpunkt liegt 900 km entfernt (Grenze 300 km)."
        }));

        Suchen(cut);

        Assert.Contains("900 km", cut.Markup);
        Assert.Empty(cut.FindAll(".epos-klimavorschau"));
        Assert.False(Einlesen(cut).HasAttribute("disabled"));
        Assert.Equal("true", Einlesen(cut).GetAttribute("aria-disabled"));
    }

    // =====================================================================
    //  Auftrag KL-4 - Fussleiste, Importfreigabe und die Katalogliste
    // =====================================================================

    /// <summary>
    /// <b>EINE Fußleiste</b> (Anwenderwunsch 19.09.2026, Punkt 1; Neuordnung Stufe 4,
    /// V14/V15): Statuszeile — „Import…" — „Beenden", Beenden primär und zuletzt.
    /// „Löschen" steht in der Auswahlleiste über dem Stammblatt (V8), das Einlesen in
    /// der Überlagerung hinter „Import…". Die Liste trägt KEINEN Aktionsknopf.
    /// </summary>
    [Fact]
    public void Eine_Fussleiste_traegt_die_vier_Knoepfe_in_der_Reihenfolge()
    {
        var cut = Zeige();

        var fuss = cut.FindAll(".epos-katalog-dialog > div.epos-leiste").Last();

        Assert.Equal(new[] { "Import…", "Beenden" },
                     fuss.QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray());
        var fueller = fuss.QuerySelectorAll("span.epos-leiste-fueller");
        Assert.Single(fueller);
        Assert.Equal("status", fueller[0].GetAttribute("role"));
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
        var cut = ZeigeImport(importieren: (_, _) => Task.FromResult(new KlimaImportErgebnis
        {
            Ausgang = KlimaImportAusgang.Erfolg, Bezeichner = "Lyon", Meldung = "fertig"
        }));

        cut.Find("input[list=epos-klimaregion-orte]").Input("Lyon");
        Einlesen(cut).Click();

        cut.WaitForAssertion(() => Assert.Contains("fertig", cut.Instance.Status),
                             TimeSpan.FromSeconds(10));

        // V14: Nach dem Erfolg ist die Ueberlagerung zu, die neue Region gewaehlt -
        // und beim naechsten Oeffnen stehen die Felder noch da.
        Assert.False(cut.Instance.ImportOffen);
        ImportOeffnen(cut);

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
        var cut = ZeigeImport(importieren: (_, _) =>
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
        cut.WaitForAssertion(() => Assert.Contains("fertig", cut.Instance.Status),
                             TimeSpan.FromSeconds(10));

        ImportOeffnen(cut);
        Einlesen(cut).Click();
        cut.WaitForAssertion(() => Assert.Contains("gibt es bereits", cut.Instance.Importmeldung),
                             TimeSpan.FromSeconds(10));
        Assert.True(cut.Instance.ImportOffen);

        Assert.Equal(2, laeufe);
        Assert.Equal(3, Zeilenklick.Zeilen(cut).Count);
    }

    /// <summary>
    /// <b>Die Regionsliste ist die EINE Katalogliste des Hauses</b> (Anwenderwunsch
    /// 19.09.2026, Punkt 3): Suchfeld, Trefferzeile, sieben Spalten samt Quelle und
    /// Standort — und KEIN Vergleichsknopf: Eine Klimaregion ist kein Gerät mit
    /// Kennwerten.
    /// </summary>
    [Fact]
    public void Die_Liste_zeigt_Suche_sechs_Spalten_und_keinen_Vergleich()
    {
        var cut = Zeige();

        Assert.Single(cut.FindAll("label.epos-katalog-suchfeld input"));
        Assert.Contains("3", cut.Find("span.epos-katalog-treffer").TextContent);
        Assert.Empty(cut.FindAll("button.epos-katalog-vergleichknopf"));

        var koepfe = cut.FindAll("span.epos-spaltenkopf-text")
                        .Select(e => e.TextContent.Trim()).ToList();
        // Der Schreibschutz ist keine Spalte mehr, sondern das Schloss hinter dem
        // Namen (Konzept Administrationsdialoge, V10) - ohne "Duplizieren" im Kurztext.
        Assert.Equal(new[] { "Klimaregion", "Quelle", "Standort", "Longitude [°]", "Latitude [°]",
                             "Importdatum" }, koepfe);
        var schloss = cut.FindAll(".epos-katalogliste .epos-schloss");
        Assert.Single(schloss);
        Assert.Equal("Auslieferungssatz – nur lesen", schloss[0].GetAttribute("title"));

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
        cut.WaitForAssertion(() => Assert.Single(Zeilenklick.Zeilen(cut)),
                             TimeSpan.FromSeconds(10));
        // Gemessen an der LISTE: Das Stammblatt zeigt weiter die Fokuszeile.
        Assert.Contains("hagelloch", cut.Find(".epos-katalogliste").TextContent);
        Assert.DoesNotContain("Berlin, Deutschland", cut.Find(".epos-katalogliste").TextContent);
    }

    /// <summary>
    /// <b>Die Wahl überlebt einen Filterwechsel</b>: Sie hängt am Bezeichner, nicht
    /// an der Zeilennummer — dieselbe Regel wie in den vierzehn anderen Katalogen.
    /// </summary>
    [Fact]
    public void Die_Wahl_ueberlebt_einen_Filterwechsel()
    {
        var cut = Zeige();

        Zeilenklick.Zeile(cut, 1);       // Stuttgart
        cut.WaitForAssertion(() => Assert.Equal("Stuttgart", cut.Instance.Gewaehlt),
                             TimeSpan.FromSeconds(10));

        cut.Find("label.epos-katalog-suchfeld input").Input("stutt");
        cut.WaitForAssertion(() => Assert.Single(Zeilenklick.Zeilen(cut)),
                             TimeSpan.FromSeconds(10));
        Assert.Equal("Stuttgart", cut.Instance.Gewaehlt);

        cut.Find("label.epos-katalog-suchfeld input").Input("");
        cut.WaitForAssertion(() => Assert.Equal(3, Zeilenklick.Zeilen(cut).Count),
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
        var cut = ZeigeImport(dateiWaehlen: _ => Task.FromResult<string?>("/tmp/x.dat"));

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
        cut.WaitForAssertion(() => Assert.Single(Zeilenklick.Zeilen(cut)),
                             TimeSpan.FromSeconds(10));

        Zeilenklick.Zeile(cut, 0);
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

        // Die Reiter mit den drei Diagrammen (KD-8: dazu „Weitere Reihen“) stehen im STAMMBLATT (Stufe 4), in dessen
        // Gruppe Jahresverlauf - nicht neben der Liste.
        Assert.Single(cut.FindAll(".epos-katalog-paar > .epos-katalog-stammblatt .epos-reiter-leiste"));
        Assert.Equal(3, cut.FindAll(".epos-katalog-paar > .epos-katalog-stammblatt .epos-reiter-knopf").Count);

        // Die Fussleiste ist ein direktes Kind der Dialogwurzel und steht damit
        // UNTER dem Rahmen - nie in ihm.
        var fuss = cut.FindAll(".epos-katalog-dialog > .epos-leiste").Last();
        var beschriftungen = fuss.QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Contains("Import…", beschriftungen);
        Assert.Contains("Beenden", beschriftungen);
        var imRahmen = cut.FindAll(".epos-katalog-paar .epos-leiste button")
                          .Select(b => b.TextContent.Trim()).ToList();
        Assert.DoesNotContain("Import…", imRahmen);
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

        // "Import..." der Fussleiste - ein gewoehnlicher Knopf ohne eigene Lage.
        var import = cut.Find("button.epos-importknopf");
        Assert.Equal("epos-knopf epos-importknopf", import.ClassName);
        Assert.False(import.HasAttribute("style"));

        // "Daten einlesen" in der Ueberlagerung: der primaere Knopf ihrer Leiste.
        ImportOeffnen(cut);
        var knopf = cut.FindAll("button").First(b => b.TextContent.Trim() == "Daten einlesen");
        Assert.Equal("epos-knopf epos-knopf--primaer", knopf.ClassName);
        Assert.False(knopf.HasAttribute("style"));
    }

    // =====================================================================
    //  Stufe 4 der Neuordnung: Stammblatt, Auswahlleiste, Einlesen als
    //  Ueberlagerung (V3 V6 V8 V9 V12 V13 V14)
    // =====================================================================

    /// <summary>
    /// <b>Das Stammblatt der Fokuszeile</b> (V9): Kopf mit Name, Herkunftszeile und
    /// drei Kennzahlen der Temperatur; die Gruppen Jahresverlauf und Herkunft, keine
    /// Kenndaten und keine Kosten (Konzept 3.6 Punkt 2).
    /// </summary>
    [Fact]
    public void Das_Stammblatt_traegt_Jahresverlauf_und_Herkunft_und_drei_Kennzahlen()
    {
        var cut = Zeige(ansicht: n => Task.FromResult(
            new KlimadatenDialog.Regionsansicht("ERA5 - PVGIS-SARAH3: " + n, 13.4, 52.5, MODELL, MODELL, "")
            {
                Jahresmittel = 9.44, Tiefstwert = -12.3, Hoechstwert = 31.06
            }));

        cut.WaitForAssertion(() => Assert.Equal("Berlin", cut.Find(".epos-stammblatt-nametext").TextContent),
                             TimeSpan.FromSeconds(10));

        var titel = cut.FindAll(".epos-stammblatt .epos-stammblattgruppe-titel").Select(e => e.TextContent).ToList();
        Assert.Equal(new[] { "Jahresverlauf", "Herkunft" }, titel);

        var kz = cut.FindAll(".epos-stammblatt-kennzahl");
        Assert.Equal(3, kz.Count);
        Assert.Equal("9,4 °C", kz[0].QuerySelector("dd")!.TextContent);
        Assert.Equal("Jahresmittel", kz[0].QuerySelector("dt")!.TextContent);
        Assert.Equal("-12,3 °C", kz[1].QuerySelector("dd")!.TextContent);
        Assert.Equal("31,1 °C", kz[2].QuerySelector("dd")!.TextContent);

        // Die Herkunftszeile: Quelle, Standort, eigener Satz.
        Assert.Equal("PVGIS-Testreferenzjahr (weltweit) · Berlin, Deutschland · eigener Satz",
                     cut.Find(".epos-stammblatt-unter").TextContent);
    }

    /// <summary>
    /// <b>Die Gruppe Herkunft ist TEXT</b> (V13): Quelle, Importdatum, Standort, Lage
    /// und der Herkunftsvermerk als Liste aus Name und Wert — kein Eingabefeld.
    /// </summary>
    [Fact]
    public void Die_Herkunft_steht_als_Text_ohne_Eingabefeld()
    {
        var cut = Zeige(ansicht: n => Task.FromResult(
            new KlimadatenDialog.Regionsansicht("ERA5 - PVGIS-SARAH3: Berlin", 13.4, 52.5, MODELL, MODELL, "")));

        cut.WaitForAssertion(() => Assert.Contains("ERA5 - PVGIS-SARAH3: Berlin", cut.Markup),
                             TimeSpan.FromSeconds(10));

        var herkunft = cut.FindAll(".epos-stammblattgruppe").Single(g => g.GetAttribute("aria-label") == "Herkunft");
        Assert.Empty(herkunft.QuerySelectorAll("input, textarea, select"));
        var namen = herkunft.QuerySelectorAll(".epos-stammblattwert dt").Select(e => e.TextContent).ToList();
        Assert.Equal(new[] { "Quelle", "Importdatum", "Standort", "Longitude [°]", "Latitude [°]", "Herkunftsvermerk" }, namen);
        var werte = herkunft.QuerySelectorAll(".epos-stammblattwert dd").Select(e => e.TextContent).ToList();
        Assert.Equal("PVGIS-Testreferenzjahr (weltweit)", werte[0]);
        Assert.Equal("2026-09-18", werte[1]);
        Assert.Equal("13,3951 °", werte[3]);
        Assert.Equal("ERA5 - PVGIS-SARAH3: Berlin", werte[5]);
    }

    /// <summary>
    /// <b>Ein Auslieferungssatz</b> (V13): Schloss im Kopf mit dem Kurztext OHNE
    /// „Duplizieren", der Hinweis in Worten sagt „nicht löschbar" statt „duplizieren".
    /// </summary>
    [Fact]
    public void Eine_Auslieferungsregion_traegt_Schloss_und_Hinweis_ohne_Duplizieren()
    {
        var cut = Zeige();

        Zeilenklick.Zeile(cut, 2);       // "Auslieferung Nord"
        cut.WaitForAssertion(() => Assert.Equal("Auslieferung Nord", cut.Find(".epos-stammblatt-nametext").TextContent),
                             TimeSpan.FromSeconds(10));

        var schloss = cut.Find(".epos-stammblatt-name .epos-schloss");
        Assert.Equal("Auslieferungssatz – nur lesen", schloss.GetAttribute("title"));
        Assert.Equal("Auslieferungssatz – nur lesen, nicht löschbar. Zum Ändern in der Auswahlleiste das Schloss aufheben.",
                     cut.Find(".epos-stammblatt-schutz").TextContent);
        Assert.DoesNotContain("uplizier", cut.Find(".epos-stammblatt").TextContent);
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Contains("Duplizieren"));
    }

    /// <summary>
    /// <b>„Import…" öffnet die Überlagerung „Klimadaten einlesen"</b> (V14) mit Titel
    /// und Schließkreuz — das Kreuz steht beim Titel, die Überlagerung trägt genau
    /// eines. Kreuz, Esc und „Abbrechen" schließen sie, ohne einzulesen.
    /// </summary>
    [Fact]
    public void Import_oeffnet_die_Ueberlagerung_und_Kreuz_Esc_Abbrechen_schliessen_sie()
    {
        int laeufe = 0;
        bool? geschlossen = null;
        var cut = Zeige(importieren: (_, _) => { laeufe++; return Task.FromResult(new KlimaImportErgebnis()); },
                        geschlossen: b => geschlossen = b);

        Assert.False(cut.Instance.ImportOffen);
        ImportOeffnen(cut);
        Assert.True(cut.Instance.ImportOffen);
        var ueberlagerung = cut.Find(".epos-ueberlagerung");
        Assert.Equal("Klimadaten einlesen", ueberlagerung.QuerySelector(".epos-ueberlagerung-titel")!.TextContent);
        Assert.Single(ueberlagerung.QuerySelectorAll(".epos-ueberlagerung-zu"));
        Assert.Empty(ueberlagerung.QuerySelectorAll(".epos-dialog-zu"));

        // Das Kreuz.
        cut.Find(".epos-ueberlagerung-zu").Click();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".epos-einlesen")), TimeSpan.FromSeconds(10));
        Assert.False(cut.Instance.ImportOffen);

        // Esc schliesst die Ueberlagerung - nicht den Dialog.
        ImportOeffnen(cut);
        cut.Find(".epos-ueberlagerung").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        cut.WaitForAssertion(() => Assert.False(cut.Instance.ImportOffen), TimeSpan.FromSeconds(10));
        Assert.Null(geschlossen);

        // "Abbrechen" der Ueberlagerung.
        ImportOeffnen(cut);
        cut.FindAll(".epos-einlesen button").First(b => b.TextContent.Trim() == "Abbrechen").Click();
        cut.WaitForAssertion(() => Assert.False(cut.Instance.ImportOffen), TimeSpan.FromSeconds(10));
        Assert.Equal(0, laeufe);
        Assert.Null(geschlossen);
    }

    /// <summary>
    /// <b>Ohne Importweg kein „Import…"</b> (Hausregel „Kein Delegat, kein Knopf").
    /// </summary>
    [Fact]
    public void Ohne_Importweg_steht_kein_Importknopf()
    {
        var cut = Render<KlimadatenDialog>(p => p
            .Add(x => x.Regionen, () => Task.FromResult(Regionen()))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand()));

        Assert.Empty(cut.FindAll("button.epos-importknopf"));
        Assert.Contains("Beenden", cut.FindAll("button").Select(b => b.TextContent.Trim()));
    }

    /// <summary>
    /// <b>Nach dem Einlesen</b> (V14): Die Überlagerung ist zu, die neue Region steht
    /// gewählt im Stammblatt, die Statuszeile nennt sie; gesetzte Kästchen fallen.
    /// </summary>
    [Fact]
    public void Nach_dem_Einlesen_steht_die_neue_Region_gewaehlt_und_die_Statuszeile_nennt_sie()
    {
        var liste = new List<Katalogfilterzeile>(Regionen());
        var cut = Zeige(regionen: liste,
            importieren: (_, _) =>
            {
                liste.Add(Zeile(99, "Lyon", "PVGIS-Testreferenzjahr (weltweit)", "Lyon, Frankreich",
                                4.83, 45.76, "2026-09-23", false));
                return Task.FromResult(new KlimaImportErgebnis
                {
                    Ausgang = KlimaImportAusgang.Erfolg, Bezeichner = "Lyon", Meldung = ""
                });
            });

        ImportOeffnen(cut);
        cut.Find("input[list=epos-klimaregion-orte]").Input("Lyon");
        Einlesen(cut).Click();

        cut.WaitForAssertion(() => Assert.Equal("Lyon", cut.Instance.Gewaehlt), TimeSpan.FromSeconds(10));
        Assert.False(cut.Instance.ImportOffen);
        Assert.Equal("„Lyon“ eingelesen.", cut.Instance.Status);
        Assert.Equal("„Lyon“ eingelesen.", cut.Find(".epos-katalog-dialog > .epos-leiste .epos-status").TextContent);
        Assert.Equal("Lyon", cut.Find(".epos-stammblatt-nametext").TextContent);
    }

    /// <summary>
    /// <b>Nach dem Einlesen rollt die Liste zur neuen Region</b> — derselbe Weg wie jede
    /// Übernahme (<c>Zeilenauswahl.Uebernommen</c> → <c>Zeigeanlass</c>): die Katalogliste
    /// ruft <c>zeileZeigen</c> mit der Stelle der neuen Region. Das Öffnen rollt nicht.
    /// </summary>
    [Fact]
    public void Nach_dem_Einlesen_rollt_die_Liste_zur_neuen_Region()
    {
        var modul = JSInterop.SetupModule(Katalogliste.MODUL);
        modul.SetupVoid("anmelden", _ => true);
        modul.SetupVoid("zeileZeigen", _ => true);

        var liste = new List<Katalogfilterzeile>(Regionen());
        var cut = Zeige(regionen: liste,
            importieren: (_, _) =>
            {
                liste.Add(Zeile(99, "Lyon", "PVGIS-Testreferenzjahr (weltweit)", "Lyon, Frankreich",
                                4.83, 45.76, "2026-09-23", false));
                return Task.FromResult(new KlimaImportErgebnis
                {
                    Ausgang = KlimaImportAusgang.Erfolg, Bezeichner = "Lyon", Meldung = ""
                });
            });
        cut.WaitForAssertion(() => Assert.Contains(modul.Invocations, i => i.Identifier == "anmelden"),
                             TimeSpan.FromSeconds(10));

        ImportOeffnen(cut);
        cut.Find("input[list=epos-klimaregion-orte]").Input("Lyon");
        Assert.DoesNotContain(modul.Invocations, i => i.Identifier == "zeileZeigen");

        Einlesen(cut).Click();

        cut.WaitForAssertion(() => Assert.Equal("Lyon", cut.Instance.Gewaehlt), TimeSpan.FromSeconds(10));
        int stelle = cut.FindAll(".epos-katalogliste tbody tr").ToList()
                        .FindIndex(z => z.TextContent.Contains("Lyon", StringComparison.Ordinal));
        Assert.True(stelle >= 0);

        cut.WaitForAssertion(() =>
        {
            var zeigen = modul.Invocations.Where(i => i.Identifier == "zeileZeigen").ToList();
            Assert.Single(zeigen);
            Assert.Equal(stelle, zeigen[0].Arguments[1]);
        }, TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// <b>Löschen mehrerer Regionen</b> (AD-Q9): Die Rückfrage nennt die gelöschten und
    /// die, die als Auslieferungssatz stehen bleiben; die Statuszeile nennt beide Zahlen.
    /// </summary>
    [Fact]
    public void Loeschen_mehrerer_Regionen_laesst_die_Auslieferung_stehen()
    {
        var geloescht = new List<string>();
        var cut = Zeige(loeschen: n => { geloescht.Add(n); return Task.FromResult(true); });

        var kaestchen = cut.FindAll(".epos-katalogliste tbody td.epos-spalte-kaestchen input");
        kaestchen[0].Change(true);
        cut.FindAll(".epos-katalogliste tbody td.epos-spalte-kaestchen input")[2].Change(true);
        cut.WaitForAssertion(() => Assert.Equal(2, cut.Instance.Kaestchen.Count), TimeSpan.FromSeconds(10));
        Assert.StartsWith("2 gewählt", cut.Find(".epos-auswahlleiste-was").TextContent);

        cut.FindAll(".epos-auswahlleiste button").First(b => b.TextContent.Trim() == "Löschen").Click();
        cut.WaitForState(() => cut.FindComponent<EPOS.UI.Bausteine.Rueckfrage>().Instance.Offen,
                         TimeSpan.FromSeconds(10));
        string frage = cut.FindComponent<EPOS.UI.Bausteine.Rueckfrage>().Instance.Frage;
        Assert.Contains("Berlin", frage);
        Assert.Contains("Stehen bleiben (Auslieferungssatz): Auslieferung Nord", frage);

        cut.FindComponent<EPOS.UI.Bausteine.Rueckfrage>()
           .FindAll("button").First(b => b.TextContent.Trim() == "Ja").Click();
        cut.WaitForAssertion(() => Assert.Equal(new[] { "Berlin" }, geloescht), TimeSpan.FromSeconds(10));
        cut.WaitForAssertion(() => Assert.Equal("1 gelöscht, 1 stehen geblieben", cut.Instance.Status),
                             TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// <b>Vergleichen</b> (V12): ab zwei Kästchen im Stammblatt — die Spalten der Liste
    /// und die Herkunft nebeneinander.
    /// </summary>
    [Fact]
    public void Vergleichen_zeigt_zwei_Regionen_im_Stammblatt()
    {
        var cut = Zeige();

        cut.FindAll(".epos-katalogliste tbody td.epos-spalte-kaestchen input")[0].Change(true);
        cut.FindAll(".epos-katalogliste tbody td.epos-spalte-kaestchen input")[1].Change(true);
        cut.WaitForAssertion(() => Assert.Equal(2, cut.Instance.Kaestchen.Count), TimeSpan.FromSeconds(10));

        cut.FindAll(".epos-auswahlleiste button").First(b => b.TextContent.Trim() == "Vergleichen").Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.Vergleicht), TimeSpan.FromSeconds(10));

        var namen = cut.Instance.Vergleichszeilen.Select(z => z.Name).ToList();
        Assert.Contains("Quelle", namen);
        Assert.Contains("Standort", namen);
        Assert.Equal("Herkunft", namen.Last());
        Assert.Single(cut.FindAll(".epos-stammblatt .epos-vergleichstabelle"));
    }

    /// <summary>
    /// <b>„groß…" vertieft die Gruppe Jahresverlauf</b>: dasselbe Modell breit in einer
    /// Überlagerung, mit EIGENER Kennung (dasselbe Bild steht zugleich im Blatt).
    /// </summary>
    [Fact]
    public void Gross_zeigt_das_Bild_breit_mit_eigener_Kennung()
    {
        var cut = Zeige();
        cut.WaitForAssertion(() => Assert.Contains(cut.FindAll(".epos-stammblatt button"),
                                                   b => b.TextContent.Trim() == "groß…"),
                             TimeSpan.FromSeconds(10));

        cut.FindAll(".epos-stammblatt button").First(b => b.TextContent.Trim() == "groß…").Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.GrossOffen), TimeSpan.FromSeconds(10));

        var kennungen = cut.FindComponents<DiagrammSvg>().Select(d => d.Instance.Kennung).ToList();
        Assert.Contains("klima-temperatur", kennungen);
        Assert.Contains("klima-gross-temperatur", kennungen);
        Assert.Equal("Jahresverlauf – Berlin",
                     cut.FindAll(".epos-ueberlagerung-titel").Last().TextContent);
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
        => Zeilenklick.Zeile(cut, nr);

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
        // Die Leiste: Bereich, 1:1 und - am Jahresbild - die Wahl "Datum | Stunde im Jahr".
        Assert.Equal(new[] { "Bereich", "1:1", "Datum", "Stunde im Jahr" },
                     cut.FindAll("button.epos-diagramm-knopf")
                        .Select(k => k.TextContent.Trim()).ToArray());
        Assert.Single(cut.FindAll(".epos-diagramm-zeigerzeile"));

        cut.FindAll("button[role='tab']")[1].Click();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("svg.epos-flaeche")),
                             TimeSpan.FromSeconds(10));
        Assert.Equal(new[] { "Bereich", "1:1", "Datum", "Stunde im Jahr" },
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
    // =================================================================================
    // „Schloss setzen…" / „Schloss aufheben…" (Entscheid AD-Q15)
    // =================================================================================

    /// <summary>
    /// <b>Das Schloss einer Region aufheben</b> (AD-Q15): Kein Duplizieren, aber die
    /// Handlung steht vor „Löschen"; nach dem „Ja" liest der Dialog die Regionen neu, das
    /// Schloss ist fort, das Band steht im Stammblatt und „Löschen" ist frei.
    /// </summary>
    [Fact]
    public void Schloss_aufheben_nach_Rueckfrage_gibt_Loeschen_frei()
    {
        IReadOnlyList<Katalogfilterzeile> zeilen = Regionen();
        var schloss = new Schlosspruefung(3);
        var cut = Zeige(regionen: zeilen, schloss: schloss.Weg(zeilen: zeilen));

        Zeilenklick.Zeile(cut, 2);       // "Auslieferung Nord"
        cut.WaitForAssertion(() => Assert.True(cut.Instance.Auslieferungssatz), TimeSpan.FromSeconds(10));
        Assert.Equal(new[] { "Vergleichen", "Schloss aufheben...", "Löschen" }, Schlosspruefung.Handlungen(cut));

        Schlosspruefung.Knopf(cut).Click();
        Assert.True(cut.Instance.Schlossfrage);
        Schlosspruefung.Ja(cut);

        cut.WaitForAssertion(() => Assert.False(cut.Instance.Auslieferungssatz), TimeSpan.FromSeconds(10));
        Assert.Equal(new[] { 3 }, schloss.Aufrufe.Single().Ids);
        Assert.True(Schlosspruefung.Band(cut));
        var loeschen = cut.FindAll(".epos-auswahlleiste button").First(b => b.TextContent.Trim() == "Löschen");
        Assert.Null(loeschen.GetAttribute("aria-disabled"));
        Assert.Equal("Schloss von „Auslieferung Nord“ aufgehoben.", cut.Instance.Status);
    }

    // =====================================================================
    //  KD-8 Teil A: weitere Reihen in Vorschau und Jahresverlauf
    // =====================================================================

    /// <summary>Eine Region mit gespeicherten Rechenspalten: Feuchte und Bedeckung NULL (führt nicht).</summary>
    private static KlimadatenDialog.Regionsansicht AnsichtMitReihen(string n)
    {
        var zeilen = Enumerable.Range(0, 8760).Select(i => new SolardatenModel
        {
            Außen_Temp = 5, Globalstrahlung = i % 24 * 10, Direktstrahlung = i % 24 * 6, Diffusstrahlung = i % 24 * 4,
            Gegenstrahlung = 300, Sol_Sued = i % 24 * 5, Sol_West = i % 24 * 3, Sol_Ost = i % 24 * 2, Sol_Nord = i % 24
        }).ToList();
        return new KlimadatenDialog.Regionsansicht("Details " + n, 9.18, 48.77, MODELL, MODELL, "")
        {
            Reihenbestand = KlimaReihenkatalog.AusRechenspalten(zeilen)
        };
    }

    private IRenderedComponent<KlimadatenDialog> ZeigeWeitere(
        Func<Zeichenmodell, string, Zeitraster, Task>? csv = null)
    {
        IReadOnlyList<Katalogfilterzeile> liste = Regionen();
        var cut = Render<KlimadatenDialog>(p => p
            .Add(x => x.Regionen, () => Task.FromResult(liste))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Ansicht, n => Task.FromResult(AnsichtMitReihen(n)))
            .Add(x => x.CsvSpeichern, csv)
            .Add(x => x.Geschlossen, _ => { }));
        cut.WaitForAssertion(() => Assert.Equal("Berlin", cut.Instance.Gewaehlt), TimeSpan.FromSeconds(10));
        cut.FindAll(".epos-stammblatt .epos-reiter-knopf").First(e => e.TextContent.Trim() == "Weitere Reihen").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-kennung=klima-jahr] button.epos-reihenwahl-chip")));
        return cut;
    }

    private static AngleSharp.Dom.IElement Chip(IRenderedComponent<KlimadatenDialog> cut, string kennung, string schluessel)
        => cut.Find("[data-kennung=" + kennung + "] button.epos-reihenwahl-chip[data-schluessel=" + schluessel + "]");

    private static DiagrammSvg Reihenbild(IRenderedComponent<KlimadatenDialog> cut)
        => cut.FindComponents<DiagrammSvg>().Single(d => d.Instance.Kennung == "klima-reihen").Instance;

    [Fact]
    public void KD8_Weitere_Reihen_Vorgabe_Global_nicht_gefuehrte_Reihen_gesperrt()
    {
        var cut = ZeigeWeitere();

        Assert.Equal("true", Chip(cut, "klima-jahr", KlimaReihenkatalog.R_GLOBAL).GetAttribute("aria-pressed"));
        var feuchte = Chip(cut, "klima-jahr", KlimaReihenkatalog.R_LUFTFEUCHTE);
        Assert.Equal("true", feuchte.GetAttribute("aria-disabled"));
        Assert.Equal("Quelle führt die Größe nicht", feuchte.GetAttribute("title"));
        // Temperatur und Sonnenwinkel haben eigene Reiter.
        Assert.Empty(cut.FindAll("[data-kennung=klima-jahr] [data-schluessel=R_TEMPERATUR]"));
        Assert.Empty(cut.FindAll("[data-kennung=klima-jahr] [data-schluessel=F_SONNENWINKEL]"));
        Assert.Single(Reihenbild(cut).Modell!.Reihen);
    }

    [Fact]
    public void KD8_Gleiche_Einheit_fuegt_hinzu_vierte_gesperrt_andere_Einheit_ersetzt()
    {
        var cut = ZeigeWeitere();

        Assert.Equal("Reihe hinzufügen", Chip(cut, "klima-jahr", KlimaReihenkatalog.R_GEGENSTRAHLUNG).GetAttribute("title"));
        Chip(cut, "klima-jahr", KlimaReihenkatalog.R_DIFFUS).Click();
        Chip(cut, "klima-jahr", KlimaReihenkatalog.F_SUED).Click();
        Assert.Equal(3, Reihenbild(cut).Modell!.Reihen.Count);
        Assert.Equal("true", Chip(cut, "klima-jahr", KlimaReihenkatalog.F_NORD).GetAttribute("aria-disabled"));
        Assert.Equal("Höchstens drei Reihen je Bild; erst eine abwählen.",
                     Chip(cut, "klima-jahr", KlimaReihenkatalog.F_NORD).GetAttribute("title"));
    }

    [Fact]
    public void KD8_Einheitenwechsel_wird_angesagt_und_zeigt_die_Reihe_allein()
    {
        // Eine Region mit Feuchte (%) neben der Globalstrahlung (W/m²).
        IReadOnlyList<Katalogfilterzeile> liste = Regionen();
        var zeilen = Enumerable.Range(0, 8760).Select(i => new SolardatenModel
        {
            Globalstrahlung = i % 24 * 10, Luftfeuchte = 70 + i % 5
        }).ToList();
        var cut2 = Render<KlimadatenDialog>(p => p
            .Add(x => x.Regionen, () => Task.FromResult(liste))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Ansicht, n => Task.FromResult(new KlimadatenDialog.Regionsansicht("", null, null, MODELL, MODELL, "")
            {
                Reihenbestand = KlimaReihenkatalog.AusRechenspalten(zeilen)
            }))
            .Add(x => x.Geschlossen, _ => { }));
        cut2.WaitForAssertion(() => Assert.Equal("Berlin", cut2.Instance.Gewaehlt), TimeSpan.FromSeconds(10));
        cut2.FindAll(".epos-stammblatt .epos-reiter-knopf").First(e => e.TextContent.Trim() == "Weitere Reihen").Click();

        var feuchte = Chip(cut2, "klima-jahr", KlimaReihenkatalog.R_LUFTFEUCHTE);
        Assert.Equal("Andere Einheit: zeigt Relative Luftfeuchte allein", feuchte.GetAttribute("title"));
        Assert.Null(feuchte.GetAttribute("aria-disabled"));
        feuchte.Click();

        Assert.Equal("true", Chip(cut2, "klima-jahr", KlimaReihenkatalog.R_LUFTFEUCHTE).GetAttribute("aria-pressed"));
        Assert.Equal("false", Chip(cut2, "klima-jahr", KlimaReihenkatalog.R_GLOBAL).GetAttribute("aria-pressed"));
        var bild = cut2.FindComponents<DiagrammSvg>().Single(d => d.Instance.Kennung == "klima-reihen").Instance;
        Assert.Equal("Relative Luftfeuchte", Assert.Single(bild.Modell!.Reihen).Name);
        Assert.Contains("jetzt: %", cut2.Find("[data-kennung=klima-jahr] .epos-reihenwahl-kopf").TextContent);
    }

    [Fact]
    public async Task KD8_Neue_Wahl_neues_Modell_Zoom_kehrt_auf_x1_zurueck()
    {
        var cut = ZeigeWeitere();
        var svg = cut.FindComponents<DiagrammSvg>().Single(d => d.Instance.Kennung == "klima-reihen");
        Zeichenmodell vorher = svg.Instance.Modell!;
        await cut.InvokeAsync(() => svg.Instance.ZoomGemeldet(4));
        Assert.Equal(4, svg.Instance.Stufe);

        Chip(cut, "klima-jahr", KlimaReihenkatalog.R_DIFFUS).Click();

        Assert.NotSame(vorher, svg.Instance.Modell);
        Assert.Equal(1, svg.Instance.Stufe);
    }

    [Fact]
    public void KD8_CSV_schreibt_die_gewaehlten_Reihen()
    {
        Zeichenmodell? geschrieben = null;
        var cut = ZeigeWeitere((m, _, _) => { geschrieben = m; return Task.CompletedTask; });
        Chip(cut, "klima-jahr", KlimaReihenkatalog.R_DIFFUS).Click();

        cut.FindAll("div.epos-diagramm-leiste button.epos-diagramm-csv")
           .Single(b => b.Closest(".epos-reiterblatt, [role=tabpanel]") is { } blatt && blatt.QuerySelector("[data-kennung=klima-jahr]") is not null)
           .Click();

        cut.WaitForAssertion(() => Assert.NotNull(geschrieben));
        Assert.Equal(new[] { "Globalstrahlung", "Diffusstrahlung" }, geschrieben!.Reihen.Select(r => r.Name));
    }

    // ---- Vorschau ----

    private static KlimaVorschauErgebnis TrefferMitReihen()
    {
        KlimaVorschauErgebnis erg = Treffer();
        var kz = erg.Kennzahlen!;
        kz.Monatsreihen[KlimaReihenkatalog.R_TEMPERATUR] = kz.MonatsmittelTemperatur;
        kz.Monatsreihen[KlimaReihenkatalog.R_GLOBAL] = kz.MonatssummeGlobalstrahlungKwhM2;
        kz.Monatsreihen[KlimaReihenkatalog.R_DIFFUS] = kz.MonatssummeGlobalstrahlungKwhM2.Select(w => w / 2).ToArray();
        kz.Monatsreihen[KlimaReihenkatalog.R_LUFTFEUCHTE] = Enumerable.Repeat(75.0, 12).ToArray();
        kz.Monatsreihen[KlimaReihenkatalog.R_BEDECKUNG] = Enumerable.Repeat(5.0, 12).ToArray();
        kz.Reihenstunden[KlimaReihenkatalog.R_BEDECKUNG] = 4000;
        return erg;
    }

    private IRenderedComponent<KlimadatenDialog> ZeigeVorschau()
    {
        var cut = ZeigeSuche(_ => Task.FromResult(TrefferMitReihen()));
        Suchen(cut);
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-kennung=klima-vorschau] button.epos-reihenwahl-chip")));
        return cut;
    }

    private static Zeichenmodell Vorschaubild(IRenderedComponent<KlimadatenDialog> cut)
        => cut.FindComponents<DiagrammSvg>().Single(d => d.Instance.Kennung == "klima-vorschau").Instance.Modell!;

    [Fact]
    public void KD8_Rohwerte_bekommen_vor_der_Freischaltung_keinen_Chip_Gegenprobe_frei()
    {
        // Die fünf Rohwert-Größen mit Bild - Monatsreihen und Stundenwerte liegen vor, der
        // Parameterbestand führt sie; bis Teil B (Rohwerttabelle) bekommen sie dennoch keinen Chip.
        string[] roh = KlimaReihenkatalog.Reihen
            .Where(r => r.Herkunft == KlimaReihenherkunft.Rohwert && r.InVorschau).Select(r => r.Schluessel).ToArray();
        Assert.Equal(5, roh.Length);

        IRenderedComponent<KlimadatenDialog> Vorschau()
        {
            var erg = TrefferMitReihen();
            foreach (string k in roh) erg.Kennzahlen!.Monatsreihen[k] = Enumerable.Repeat(3.0, 12).ToArray();
            erg.Parameterbestand = Enum.GetValues<KlimaParameter>()
                .Select(p => new KlimaParameterEintrag { Parameter = p, Stand = KlimaParameterStand.Gefuehrt }).ToList();
            var cut = ZeigeSuche(_ => Task.FromResult(erg));
            Suchen(cut);
            cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-kennung=klima-vorschau] button.epos-reihenwahl-chip")));
            return cut;
        }

        IRenderedComponent<KlimadatenDialog> Jahr()
        {
            IReadOnlyList<Katalogfilterzeile> liste = Regionen();
            var cut = Render<KlimadatenDialog>(p => p
                .Add(x => x.Regionen, () => Task.FromResult(liste))
                .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
                .Add(x => x.Ansicht, n =>
                {
                    var a = AnsichtMitReihen(n);
                    var bestand = a.Reihenbestand.Concat(roh.Select(k => new KlimaReihenwerte
                    {
                        Reihe = KlimaReihenkatalog.Reihe(k), Werte = Enumerable.Repeat(3.0, 8760).ToArray(), Gefuehrt = true
                    })).ToList();
                    return Task.FromResult(a with { Reihenbestand = bestand });
                })
                .Add(x => x.Geschlossen, _ => { }));
            cut.WaitForAssertion(() => Assert.Equal("Berlin", cut.Instance.Gewaehlt), TimeSpan.FromSeconds(10));
            cut.FindAll(".epos-stammblatt .epos-reiter-knopf").First(e => e.TextContent.Trim() == "Weitere Reihen").Click();
            cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-kennung=klima-jahr] button.epos-reihenwahl-chip")));
            return cut;
        }

        int Rohchips(IRenderedComponent<KlimadatenDialog> cut, string kennung)
            => cut.FindAll($"[data-kennung={kennung}] button.epos-reihenwahl-chip")
                  .Count(c => c.GetAttribute("data-schluessel")!.StartsWith(KlimaReihenkatalog.ROH_PREFIX));

        Assert.False(KlimaReihenkatalog.RohwerteFreigeschaltet);
        Assert.Equal(0, Rohchips(Vorschau(), "klima-vorschau"));
        Assert.Equal(0, Rohchips(Jahr(), "klima-jahr"));

        // Gegenprobe: freigeschaltet erscheinen die fünf Chips - frei schaltbar, mit Marke „R“.
        KlimaReihenkatalog.RohwerteFreigeschaltet = true;
        try
        {
            var v = Vorschau();
            Assert.Equal(5, Rohchips(v, "klima-vorschau"));
            var j = Jahr();
            Assert.Equal(5, Rohchips(j, "klima-jahr"));
            var druck = Chip(j, "klima-jahr", KlimaReihenkatalog.ROH_PREFIX + "Luftdruck_hPa");
            Assert.Null(druck.GetAttribute("aria-disabled"));
            Assert.NotNull(druck.QuerySelector(".epos-reihenwahl-marke"));
        }
        finally
        {
            KlimaReihenkatalog.RohwerteFreigeschaltet = false;
        }
    }

    [Fact]
    public void KD8_Vorschau_Vorgabe_Global_und_Temperatur_unter_dem_Bild()
    {
        var cut = ZeigeVorschau();

        var chips = cut.FindAll("[data-kennung=klima-vorschau] button.epos-reihenwahl-chip")
                       .Where(c => !c.GetAttribute("data-schluessel")!.StartsWith(KlimaReihenkatalog.ROH_PREFIX)).ToList();
        Assert.Equal(new[] { "R_TEMPERATUR", "R_LUFTFEUCHTE", "R_GLOBAL", "R_DIFFUS", "R_BEDECKUNG" },
                     chips.Select(c => c.GetAttribute("data-schluessel")));
        Assert.Equal(new[] { "R_TEMPERATUR", "R_GLOBAL" },
                     chips.Where(c => c.GetAttribute("aria-pressed") == "true").Select(c => c.GetAttribute("data-schluessel")));
        Assert.Equal(2, Vorschaubild(cut).Reihen.Count);
        Assert.Contains("4.000 von 8.760 Stunden", Chip(cut, "klima-vorschau", KlimaReihenkatalog.R_BEDECKUNG).GetAttribute("title"));
        Assert.Empty(cut.FindAll("datalist[id^=epos-reihen]"));
    }

    [Fact]
    public void KD8_Vorschau_Zuschalten_dritte_Einheit_gesperrt_letzte_bleibt_Vorgabe()
    {
        var cut = ZeigeVorschau();

        Chip(cut, "klima-vorschau", KlimaReihenkatalog.R_DIFFUS).Click();
        Assert.Equal(3, Vorschaubild(cut).Reihen.Count);

        var feuchte = Chip(cut, "klima-vorschau", KlimaReihenkatalog.R_LUFTFEUCHTE);
        Assert.Equal("true", feuchte.GetAttribute("aria-disabled"));
        Assert.StartsWith("Höchstens zwei Einheiten zugleich (kWh/m², °C)", feuchte.GetAttribute("title"));
        feuchte.Click();
        Assert.Equal(3, Vorschaubild(cut).Reihen.Count);

        Chip(cut, "klima-vorschau", KlimaReihenkatalog.R_TEMPERATUR).Click();
        Chip(cut, "klima-vorschau", KlimaReihenkatalog.R_GLOBAL).Click();
        Assert.Equal("Diffusstrahlung [kWh/m²]", Assert.Single(Vorschaubild(cut).Reihen).Name);
        Chip(cut, "klima-vorschau", KlimaReihenkatalog.R_DIFFUS).Click();       // letzte bleibt
        Assert.Single(Vorschaubild(cut).Reihen);
        Assert.Equal("Mindestens eine Reihe bleibt gewählt.",
                     cut.Find("[data-kennung=klima-vorschau] .epos-reihenwahl-leise").TextContent);

        cut.Find("[data-kennung=klima-vorschau] .epos-reihenwahl-vorgabe").Click();
        Assert.Equal(2, Vorschaubild(cut).Reihen.Count);
    }
}
