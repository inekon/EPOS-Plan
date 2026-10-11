using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Der Erzeugerdialog „Kältemaschinen im Projekt"</b> (KU3-4c) gegen einen Stub der Hülle: eine gespeicherte
/// Anlage, zwei Stammgeräte, die Prüfregeln des Kerns (<c>KaeltemaschineAnlageHuelle.Pruefen</c>, ohne
/// Datenbank) und Schreibwege als Listen. Geschrieben wird erst mit „OK".
///
/// <para>Die Kultur ist auf de-DE gepinnt; die englische Oberfläche prüft
/// <see cref="KaeltemaschineAnlageDialogEnglischTests"/>.</para>
/// </summary>
public class KaeltemaschineAnlageDialogTests : EposBunitContext
{
    public KaeltemaschineAnlageDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    internal const int STROM = 1;
    internal const int OEKOSTROM = 2;

    /// <summary>Der Stub der Hülle.</summary>
    internal sealed class Projekt
    {
        internal readonly List<KaeltemaschineAnlageDaten> Gespeicherte = new()
        {
            new KaeltemaschineAnlageDaten
            {
                AnlagenId = 7, GeraetId = 70, Bezeichner = "KM Bestand", Anzahl = 2,
                Geraet = new KaeltemaschineGeraetwerte
                {
                    Bezeichner = "Kältemaschine 100 kW", Firma = "Neutral", Rueckkuehlart = R.KM_RUECKKUEHLART_LUFT,
                    Nennkaelteleistung = 100, NennEer = 3.2, KaltwasserMin = 5,
                    Kaltwasserstuetzstellen = new[] { 6.0, 12.0, 18.0 }
                }
            }
        };
        internal readonly List<(int Stamm, string Name)> Angelegt = new();
        internal readonly List<KaeltemaschineAnlageDaten> Gespeichert = new();
        internal readonly List<int> Geloescht = new();
        internal bool? Geschlossen;

        internal IReadOnlyList<Katalogfilterzeile> Katalog() => new[]
        {
            Zeile(11, "Kältemaschine 50 kW", 50, 3.0),
            Zeile(12, "Kältemaschine 200 kW", 200, 4.1)
        };

        private static Katalogfilterzeile Zeile(int id, string name, double kw, double eer)
            => new Katalogfilterzeile(id, name) { Schluessel = id.ToString(System.Globalization.CultureInfo.InvariantCulture) }
                   .MitText(Katalogfilterprofil.SpBezeichner, name)
                   .MitZahl(Katalogfilterprofil.SpNennkaelteleistung, kw, 1)
                   .MitZahl(Katalogfilterprofil.SpEer, eer, 2)
                   .MitText(Katalogfilterprofil.SpRueckkuehlart, R.KM_RUECKKUEHLART_WASSER);

        internal KaeltemaschineGeraetwerte? Werte(int stamm) => stamm == 12
            ? new KaeltemaschineGeraetwerte { Bezeichner = "Kältemaschine 200 kW", Nennkaelteleistung = 200, NennEer = 4.1,
                                              Kaltwasserstuetzstellen = new[] { 7.0, 10.0 } }
            : null;

        internal int Anlegen(int stamm, string name)
        {
            Angelegt.Add((stamm, name));
            return stamm == 12 ? 99 : -1;
        }

        internal string? Speichern(KaeltemaschineAnlageDaten d)
        {
            string grund = KaeltemaschineAnlageHuelle.Pruefen(d);
            if (grund != null) return grund;
            Gespeichert.Add(d.Kopie());
            return null;
        }
    }

    internal static IRenderedComponent<KaeltemaschineAnlageDialog> Aufbauen(BunitContext ctx, Projekt p)
        => ctx.Render<KaeltemaschineAnlageDialog>(b => b
            .Add(x => x.Anlagen, () => p.Gespeicherte.Select(a => a.Kopie()).ToList())
            .Add(x => x.Katalogzeilen, p.Katalog)
            .Add(x => x.Katalogwerte, p.Werte)
            .Add(x => x.Stromtraeger, new[] { (STROM, "Strom"), (OEKOSTROM, "Ökostrom") })
            .Add(x => x.ProjektStromtraeger, STROM)
            .Add(x => x.Pruefen, KaeltemaschineAnlageHuelle.Pruefen)
            .Add(x => x.Anlegen, p.Anlegen)
            .Add(x => x.Speichern, p.Speichern)
            .Add(x => x.Loeschen, id => p.Geloescht.Add(id))
            .Add(x => x.Geschlossen, ok => p.Geschlossen = ok));

    private static IElement Knopf(IRenderedComponent<KaeltemaschineAnlageDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static IElement Fussknopf(IRenderedComponent<KaeltemaschineAnlageDialog> cut, string text)
        => cut.FindAll(".epos-kaeltemaschine-anlage > .epos-leiste button").First(b => b.TextContent.Trim() == text);

    private static IElement Feld(IRenderedComponent<KaeltemaschineAnlageDialog> cut, string bezeichnung)
        => cut.FindAll("label").First(l => l.TextContent.Trim().StartsWith(bezeichnung, StringComparison.Ordinal))
              .QuerySelector("input, select")
           ?? cut.FindAll(".epos-feld").First(f => f.TextContent.Trim().StartsWith(bezeichnung, StringComparison.Ordinal))
                 .QuerySelector("input, select")!;

    [Fact]
    public void Die_Liste_zeigt_Name_Anzahl_und_Nennkaelteleistung_und_das_Geraet_zum_Lesen()
    {
        var p = new Projekt();
        var cut = Aufbauen(this, p);

        Assert.Equal(R.KMA_TITEL, cut.Find(".epos-dialog-titel").TextContent);
        string zeile = cut.Find(".epos-kaeltemaschine-anlagen tbody tr").TextContent;
        Assert.Contains("KM Bestand", zeile);
        Assert.Contains("200", zeile);                                   // 2 × 100 kW
        Assert.Contains(R.KMA_HINWEIS_KATALOG, cut.Markup);
        Assert.Contains(R.KMA_HINWEIS_REIHENFOLGE, cut.Markup);
        Assert.Contains(string.Format(R.KMA_HINWEIS_VORLAUF_MIN, "5"), cut.Markup);
        Assert.Contains(string.Format(R.KMA_PLATZHALTER_VORLAUF, "6"), cut.Markup);
        Assert.Contains(R.KM_RUECKKUEHLART_LUFT, cut.Markup);
        Assert.Equal(new[] { R.KMA_BTN_HINZU, R.KMA_BTN_LOESCHEN, R.ALLG_BTN_ABBRECHEN, R.ALLG_BTN_OK },
                     cut.FindAll(".epos-kaeltemaschine-anlage > .epos-leiste button").Select(b => b.TextContent.Trim()).ToArray());
    }

    /// <summary>
    /// KB-B (E117 F2): Die Betriebseingaben stehen in der Komponente <see cref="KaeltemaschineKonfiguration"/> —
    /// derselben wie im Bereich „Kälte“ der Simulationskonfiguration —, und die Liste zieht einer Eingabe nach.
    /// </summary>
    [Fact]
    public void Die_Betriebseingaben_sind_die_Komponente_der_Simulationskonfiguration()
    {
        var p = new Projekt();
        var cut = Aufbauen(this, p);

        IRenderedComponent<KaeltemaschineKonfiguration> komponente = cut.FindComponent<KaeltemaschineKonfiguration>();
        Assert.Same(cut.Instance.Arbeitsstand, komponente.Instance.Daten);

        Feld(cut, R.KMA_LBL_ANZAHL).Input("3");
        Assert.Contains("300", cut.Find(".epos-kaeltemaschine-anlagen tbody tr").TextContent);   // 3 × 100 kW
    }

    /// <summary>
    /// KM3-E3-b (Fachkonzept Teillast und Takten 7.2): die Lesewerte der Teillastrechnung der Projektkopie und der
    /// Hinweis „Folgeschaltung von n Maschinen“ - nur bei Anzahl &gt; 1 und gesetztem Weg.
    /// </summary>
    [Fact]
    public void Die_Teillast_Lesewerte_stehen_und_die_Folgeschaltung_nur_mit_Weg_und_mehreren_Maschinen()
    {
        var p = new Projekt();
        KaeltemaschineGeraetwerte alt = p.Gespeicherte[0].Geraet!;
        p.Gespeicherte[0].Geraet = new KaeltemaschineGeraetwerte
        {
            Bezeichner = alt.Bezeichner, Nennkaelteleistung = alt.Nennkaelteleistung, KaltwasserMin = alt.KaltwasserMin,
            Kaltwasserstuetzstellen = alt.Kaltwasserstuetzstellen,
            TeillastWeg = R.KM_TT_WEG_VORGABEKURVE, Verdichterregelung = R.KM_REGELUNG_STUFEN,
            Taktverlustfaktor = R.KM_PH_CD_VORGABE, Kennfeldrand = R.KM_RANDWEG_GUETEGRAD, TeillastGesetzt = true
        };
        var cut = Aufbauen(this, p);
        Assert.Equal(R.KM_TT_WEG_VORGABEKURVE, Feld(cut, R.KM_LBL_TEILLAST_WEG).GetAttribute("value"));
        Assert.Equal(R.KM_REGELUNG_STUFEN, Feld(cut, R.KM_LBL_VERDICHTERREGELUNG).GetAttribute("value"));
        Assert.Equal(R.KM_PH_CD_VORGABE, Feld(cut, R.KM_LBL_TAKTVERLUST_CD).GetAttribute("value"));
        Assert.Equal(R.KM_RANDWEG_GUETEGRAD, Feld(cut, R.KM_LBL_KENNFELD_RANDWEG).GetAttribute("value"));
        string folge = string.Format(System.Globalization.CultureInfo.CurrentCulture, R.KMA_HINWEIS_FOLGESCHALTUNG, 2);
        Assert.Contains(folge, cut.Markup);

        // Eine Maschine: kein Hinweis.
        Feld(cut, R.KMA_LBL_ANZAHL).Input("1");
        Assert.DoesNotContain(folge, cut.Markup);
        Assert.DoesNotContain(string.Format(System.Globalization.CultureInfo.CurrentCulture, R.KMA_HINWEIS_FOLGESCHALTUNG, 1), cut.Markup);

        // Ohne Weg (Bestand): kein Hinweis, auch bei zwei Maschinen.
        var q = new Projekt();
        var ohne = Aufbauen(this, q);
        Assert.DoesNotContain(folge, ohne.Markup);
    }

    /// <summary>KM3-E3-b: die Hülle liest die Lesewerte über den Kern (Kaeltemaschinenteillast), wie der Lauf.</summary>
    [Fact]
    public void Die_Huelle_liest_Weg_mit_Herkunft_Regelung_und_Cd()
    {
        KaeltemaschineGeraetwerte w = KaeltemaschineAnlageHuelle.Werte(new KaeltemaschineModel
        {
            Teillast_Weg = KaeltemaschineTeillastSchema.WEG_KURVE, Verdichterregelung = KaeltemaschineTeillastSchema.REGELUNG_DREHZAHL
        });
        Assert.True(w.TeillastGesetzt);
        Assert.Equal(R.KM_TT_WEG_VORGABEKURVE, w.TeillastWeg);
        Assert.Equal(R.KM_REGELUNG_DREHZAHL, w.Verdichterregelung);
        Assert.Equal(R.KM_PH_CD_VORGABE, w.Taktverlustfaktor);
        Assert.Equal(R.KM_PH_RANDWEG_VORGABE, w.Kennfeldrand);
        KaeltemaschineGeraetwerte b = KaeltemaschineAnlageHuelle.Werte(new KaeltemaschineModel());
        Assert.False(b.TeillastGesetzt);
        Assert.Equal(R.KM_TEILLAST_WEG_BESTAND, b.TeillastWeg);
    }

    [Fact]
    public void Die_Katalogwahl_legt_die_Projektkopie_erst_beim_OK_an()
    {
        var p = new Projekt();
        var cut = Aufbauen(this, p);

        Knopf(cut, R.KMA_BTN_HINZU).Click();
        Assert.True(cut.Instance.KatalogOffen);
        Assert.Equal(2, cut.FindAll(".epos-katalogliste tbody tr").Count);
        Knopf(cut, R.KMA_BTN_UEBERNEHMEN).Click();
        Assert.Contains(R.KMA_KATALOG_KEINE_WAHL, cut.Markup);       // ohne Wahl nichts übernommen

        Zeilenklick.Zeile(cut, 1);
        Knopf(cut, R.KMA_BTN_UEBERNEHMEN).Click();
        Assert.False(cut.Instance.KatalogOffen);
        Assert.Equal(new[] { "KM Bestand", "Kältemaschine 200 kW" }, cut.Instance.Namensliste);
        Assert.Equal(12, cut.Instance.Arbeitsstand!.StammId);
        Assert.Contains(R.KMA_HINWEIS_NEU, cut.Markup);
        Assert.Empty(p.Angelegt);                                     // nichts ohne OK

        Fussknopf(cut, R.ALLG_BTN_OK).Click();
        Assert.Equal(new[] { (12, "Kältemaschine 200 kW") }, p.Angelegt);
        Assert.Equal(99, p.Gespeichert.Single().AnlagenId);
        Assert.True(p.Geschlossen);
    }

    [Fact]
    public void Eine_neue_Anlage_traegt_die_Vorauswahl_des_Kuehltraegers()
    {
        var p = new Projekt();
        var cut = Render<KaeltemaschineAnlageDialog>(b => b
            .Add(x => x.Anlagen, () => p.Gespeicherte.Select(a => a.Kopie()).ToList())
            .Add(x => x.Katalogzeilen, p.Katalog)
            .Add(x => x.Katalogwerte, p.Werte)
            .Add(x => x.Stromtraeger, new[] { (STROM, "Strom"), (OEKOSTROM, "Ökostrom") })
            .Add(x => x.ProjektStromtraeger, STROM)
            .Add(x => x.KuehltraegerVorauswahl, STROM)
            .Add(x => x.Anlegen, p.Anlegen)
            .Add(x => x.Speichern, p.Speichern));

        cut.InvokeAsync(() => cut.Instance.KatalogUebernehmen(12, "Kältemaschine 200 kW"));

        Assert.Equal(STROM, cut.Instance.Arbeitsstand!.KuehlCarrierId);
    }

    [Fact]
    public void Ohne_Vorauswahl_bleibt_der_Kuehltraeger_einer_neuen_Anlage_leer()
    {
        var p = new Projekt();
        var cut = Aufbauen(this, p);
        cut.InvokeAsync(() => cut.Instance.KatalogUebernehmen(12, "Kältemaschine 200 kW"));
        Assert.Null(cut.Instance.Arbeitsstand!.KuehlCarrierId);
    }

    [Fact]
    public void Ein_fehlender_Katalogsatz_wird_beim_OK_gemeldet()
    {
        var p = new Projekt();
        var cut = Aufbauen(this, p);
        cut.InvokeAsync(() => cut.Instance.KatalogUebernehmen(11, "Kältemaschine 50 kW"));

        Fussknopf(cut, R.ALLG_BTN_OK).Click();
        Assert.Equal(string.Format(R.KMA_MSG_ANLEGEN_FEHLT, "Kältemaschine 50 kW"), cut.Instance.Meldung);
        Assert.Null(p.Geschlossen);
    }

    [Fact]
    public void Die_Pruefregeln_melden_Anzahl_0_und_Hilfsstrom_100_Prozent_und_schreiben_nichts()
    {
        var p = new Projekt();
        var cut = Aufbauen(this, p);

        Feld(cut, R.KMA_LBL_ANZAHL).Input("0");
        Fussknopf(cut, R.ALLG_BTN_OK).Click();
        Assert.Equal(R.KM_ANLAGE_ANZAHL_UNGUELTIG, cut.Instance.Meldung);

        Feld(cut, R.KMA_LBL_ANZAHL).Input("3");
        Feld(cut, R.KMA_LBL_HILFSSTROM).Input("100");
        Fussknopf(cut, R.ALLG_BTN_OK).Click();
        Assert.Equal(R.KM_ANLAGE_HILFSSTROM_UNGUELTIG, cut.Instance.Meldung);
        Assert.Empty(p.Gespeichert);
        Assert.Null(p.Geschlossen);
    }

    [Fact]
    public void OK_speichert_die_Eingaben_und_Abbrechen_verwirft()
    {
        var p = new Projekt();
        var cut = Aufbauen(this, p);
        Feld(cut, R.KMA_LBL_ANZAHL).Input("3");
        Feld(cut, R.KMA_LBL_VORLAUF).Input("9");
        Feld(cut, R.KMA_LBL_HILFSSTROM).Input("4");
        Fussknopf(cut, R.ALLG_BTN_OK).Click();

        KaeltemaschineAnlageDaten d = p.Gespeichert.Single();
        Assert.Equal((7, 3, 9.0), (d.AnlagenId, d.Anzahl!.Value, d.KuehlVorlauf!.Value));
        Assert.Equal(0.04, d.KuehlHilfsstromanteil!.Value, 9);
        Assert.True(p.Geschlossen);

        var q = new Projekt();
        var zwei = Aufbauen(this, q);
        Feld(zwei, R.KMA_LBL_ANZAHL).Input("5");
        Fussknopf(zwei, R.ALLG_BTN_ABBRECHEN).Click();
        Assert.Empty(q.Gespeichert);
        Assert.False(q.Geschlossen);

        var r = new Projekt();
        var drei = Aufbauen(this, r);
        Fussknopf(drei, R.ALLG_BTN_OK).Click();                       // unverändert: nichts zu schreiben
        Assert.Empty(r.Gespeichert);
        Assert.True(r.Geschlossen);
    }

    [Fact]
    public void Loeschen_fragt_zurueck_und_loescht_erst_beim_OK()
    {
        var p = new Projekt();
        var cut = Aufbauen(this, p);

        Fussknopf(cut, R.KMA_BTN_LOESCHEN).Click();
        Assert.True(cut.Instance.Loeschfrage);
        Assert.Contains("„KM Bestand“", cut.Find(".epos-rueckfrage").TextContent);
        cut.Find(".epos-rueckfrage").QuerySelectorAll(".epos-knopf").First(b => b.TextContent.Trim() == R.ALLG_BTN_NEIN).Click();
        Assert.Single(cut.Instance.Namensliste);

        Fussknopf(cut, R.KMA_BTN_LOESCHEN).Click();
        cut.Find(".epos-rueckfrage").QuerySelectorAll(".epos-knopf").First(b => b.TextContent.Trim() == R.ALLG_BTN_JA).Click();
        Assert.Empty(cut.Instance.Namensliste);
        Assert.Empty(p.Geloescht);                                    // vorgemerkt

        Fussknopf(cut, R.ALLG_BTN_OK).Click();
        Assert.Equal(new[] { 7 }, p.Geloescht);
        Assert.True(p.Geschlossen);
    }

    [Fact]
    public void Kein_Anzeigetext_ist_Steuerwert_der_Kuehltraeger_geht_ueber_die_Id()
    {
        var p = new Projekt();
        var cut = Aufbauen(this, p);

        IElement auswahl = Feld(cut, R.KMA_LBL_TRAEGER);
        Assert.Contains("Ökostrom", auswahl.TextContent);
        Assert.DoesNotContain(auswahl.QuerySelectorAll("option"), o => (o.GetAttribute("value") ?? "").Contains("strom", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(R.KMA_HINWEIS_ZAEHLER, cut.Markup);
        auswahl.Change(OEKOSTROM.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.DoesNotContain(R.KMA_HINWEIS_ZAEHLER, cut.Markup);       // abweichend: der Zähler wirkt
        Fussknopf(cut, R.ALLG_BTN_OK).Click();

        Assert.Equal(OEKOSTROM, p.Gespeichert.Single().KuehlCarrierId);
    }

    [Fact]
    public void Esc_verwirft_und_die_KI_Sicht_schreibt_in_den_Arbeitsstand()
    {
        var p = new Projekt();
        var cut = Aufbauen(this, p);
        KaeltemaschineAnlageDaten arbeit = cut.Instance.Arbeitsstand!;
        var sicht = new KaeltemaschineAnlageKiSicht
        {
            AnlageLesen = () => cut.Instance.Gewaehlt,
            ArbeitLesen = () => arbeit,
            Stromtraeger = () => new[] { (STROM, "Strom"), (OEKOSTROM, "Ökostrom") }
        };
        sicht.Anzahl = 4;
        sicht.Hilfsstrom = 5;
        sicht.Kuehltraeger = "2";
        Assert.Equal((4, 0.05, (int?)OEKOSTROM), (arbeit.Anzahl!.Value, arbeit.KuehlHilfsstromanteil!.Value, arbeit.KuehlCarrierId));
        sicht.Kuehltraeger = "Ökostrom";                              // ein Anzeigetext steuert nichts
        Assert.Null(arbeit.KuehlCarrierId);
        Assert.Equal(new[] { "1", "2" }, sicht.KuehltraegerWahl.Select(w => w.Schluessel).ToArray());

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(p.Geschlossen);
        Assert.Empty(p.Gespeichert);
    }
}

/// <summary>Derselbe Dialog auf der englischen Oberfläche.</summary>
public sealed class KaeltemaschineAnlageDialogEnglischTests : EposBunitContext
{
    public KaeltemaschineAnlageDialogEnglischTests() : base("en-US")
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    [Fact]
    public void Die_englische_Oberflaeche_traegt_englische_Texte()
    {
        var p = new KaeltemaschineAnlageDialogTests.Projekt();
        var cut = KaeltemaschineAnlageDialogTests.Aufbauen(this, p);

        Assert.Equal("Chillers in the project", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Chilled water supply", cut.Markup);
        Assert.Contains("The chillers are dispatched after the heat pumps in cooling mode.", cut.Markup);
        Assert.Contains("Add…", cut.Find(".epos-kaeltemaschine-anlage > .epos-leiste").TextContent);
    }
}

/// <summary>
/// <b>Die Wege zum Katalog in der Katalogwahl „Hinzufügen…"</b>: „Typkennfelder laden…" über die Gabe der Hülle
/// und „Katalogverwaltung…" über die Naht <c>Dienste.Navigation</c> — bei leerem Katalog unter dem Leertext, bei
/// gefülltem als Nebenknöpfe unter der Liste. Der Fall tauscht <c>Dienste.Navigation</c> und steht deshalb in der
/// seriellen Sammlung.
/// </summary>
[Collection("KiDialogweg")]
public class KaeltemaschineAnlageKatalogwegeTests : EposBunitContext
{
    private readonly INavigation _vorher;

    public KaeltemaschineAnlageKatalogwegeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        _vorher = WindowsFormsApplication1.Dienste.Navigation;
        Navigation = new TestNavigation();
        WindowsFormsApplication1.Dienste.Navigation = Navigation;
    }

    /// <summary>Die Mitschrift der Maskenaufrufe.</summary>
    private TestNavigation Navigation { get; }

    protected override void Dispose(bool disposing)
    {
        if (disposing) WindowsFormsApplication1.Dienste.Navigation = _vorher;
        base.Dispose(disposing);
    }

    /// <summary>Der Katalog des Stubs — leer, bis die Typkennfelder geladen sind (oder von Anfang an gefüllt).</summary>
    private sealed class Katalogstand
    {
        internal readonly KaeltemaschineAnlageDialogTests.Projekt Projekt = new();
        internal bool Gefuellt;
        internal int Ladungen;

        internal IReadOnlyList<Katalogfilterzeile> Zeilen()
            => Gefuellt ? Projekt.Katalog() : Array.Empty<Katalogfilterzeile>();

        internal KaeltemaschineTypkennfelderErgebnis Laden()
        {
            Ladungen++;
            Gefuellt = true;
            return new KaeltemaschineTypkennfelderErgebnis(true, 2, 0, "");
        }
    }

    private IRenderedComponent<KaeltemaschineAnlageDialog> Aufbauen(Katalogstand k, bool mitTypkennfeldern = true)
        => Render<KaeltemaschineAnlageDialog>(b =>
        {
            b.Add(x => x.Anlagen, () => k.Projekt.Gespeicherte.Select(a => a.Kopie()).ToList())
             .Add(x => x.Katalogzeilen, k.Zeilen)
             .Add(x => x.Katalogwerte, k.Projekt.Werte)
             .Add(x => x.Stromtraeger, new[] { (KaeltemaschineAnlageDialogTests.STROM, "Strom") })
             .Add(x => x.ProjektStromtraeger, KaeltemaschineAnlageDialogTests.STROM)
             .Add(x => x.Pruefen, KaeltemaschineAnlageHuelle.Pruefen)
             .Add(x => x.Anlegen, k.Projekt.Anlegen)
             .Add(x => x.Speichern, k.Projekt.Speichern)
             .Add(x => x.Geschlossen, ok => k.Projekt.Geschlossen = ok);
            if (mitTypkennfeldern) b.Add(x => x.TypkennfelderLaden, k.Laden);
        });

    private static IElement Knopf(IRenderedComponent<KaeltemaschineAnlageDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static string[] Katalogwege(IRenderedComponent<KaeltemaschineAnlageDialog> cut)
        => cut.FindAll(".epos-kaeltemaschine-katalogwege button").Select(b => b.TextContent.Trim()).ToArray();

    [Fact]
    public void Leerer_Katalog_zeigt_den_Leertext_und_beide_Knoepfe()
    {
        var k = new Katalogstand();
        var cut = Aufbauen(k);

        Knopf(cut, R.KMA_BTN_HINZU).Click();

        Assert.Contains(R.KMA_KATALOG_LEER, cut.Markup);
        Assert.Empty(cut.FindAll(".epos-katalogliste"));
        Assert.Equal(new[] { R.KM_BTN_TYPKENNFELDER, R.KM_ANL_BTN_VERWALTUNG }, Katalogwege(cut));
    }

    [Fact]
    public void Typkennfelder_laden_fuellt_die_Liste_ueber_die_Gabe_und_meldet_im_Banner()
    {
        var k = new Katalogstand();
        var cut = Aufbauen(k);
        Knopf(cut, R.KMA_BTN_HINZU).Click();

        Knopf(cut, R.KM_BTN_TYPKENNFELDER).Click();

        Assert.Equal(1, k.Ladungen);
        Assert.True(cut.Instance.KatalogOffen);
        Assert.Equal(2, cut.FindAll(".epos-katalogliste tbody tr").Count);
        Assert.DoesNotContain(R.KMA_KATALOG_LEER, cut.Markup);
        Assert.Contains(string.Format(System.Globalization.CultureInfo.CurrentCulture, R.KM_MSG_TYPKENNFELDER, 2, 0),
                        cut.Markup);

        // Das geladene Gerät lässt sich gleich übernehmen.
        Zeilenklick.Zeile(cut, 1);
        Knopf(cut, R.KMA_BTN_UEBERNEHMEN).Click();
        Assert.False(cut.Instance.KatalogOffen);
        Assert.Equal(12, cut.Instance.Arbeitsstand!.StammId);
    }

    [Fact]
    public void Ein_gescheitertes_Laden_meldet_den_Grund_des_Kerns()
    {
        var k = new Katalogstand();
        var cut = Render<KaeltemaschineAnlageDialog>(b => b
            .Add(x => x.Anlagen, () => k.Projekt.Gespeicherte.Select(a => a.Kopie()).ToList())
            .Add(x => x.Katalogzeilen, k.Zeilen)
            .Add(x => x.TypkennfelderLaden, () => new KaeltemaschineTypkennfelderErgebnis(false, 0, 0, "Grund des Kerns")));
        Knopf(cut, R.KMA_BTN_HINZU).Click();

        Knopf(cut, R.KM_BTN_TYPKENNFELDER).Click();

        Assert.Contains("Grund des Kerns", cut.Markup);
        Assert.Contains(R.KMA_KATALOG_LEER, cut.Markup);
    }

    [Fact]
    public void Katalogverwaltung_schliesst_die_Ueberlagerung_und_ruft_die_Naht()
    {
        var k = new Katalogstand();
        var cut = Aufbauen(k);
        Knopf(cut, R.KMA_BTN_HINZU).Click();

        Knopf(cut, R.KM_ANL_BTN_VERWALTUNG).Click();

        Assert.Equal(new[] { EPOS.UI.Seiten.Seitenschluessel.KaeltemaschineKatalog }, Navigation.Masken);
        Assert.False(cut.Instance.KatalogOffen);

        // Der nächste Klick auf „Hinzufügen…" liest den Katalog neu.
        k.Gefuellt = true;
        Knopf(cut, R.KMA_BTN_HINZU).Click();
        Assert.Equal(2, cut.FindAll(".epos-katalogliste tbody tr").Count);
    }

    [Fact]
    public void Lehnt_die_Plattform_ab_bleibt_die_Ueberlagerung_offen_und_nennt_es()
    {
        Navigation.Antwort = false;
        var cut = Aufbauen(new Katalogstand());
        Knopf(cut, R.KMA_BTN_HINZU).Click();

        Knopf(cut, R.KM_ANL_BTN_VERWALTUNG).Click();

        Assert.Single(Navigation.Masken);
        Assert.True(cut.Instance.KatalogOffen);
        Assert.Contains(R.KM_ANL_MSG_VERWALTUNG_NICHT, cut.Markup);
    }

    [Fact]
    public void Ungespeicherte_Aenderungen_halten_die_Katalogverwaltung_an()
    {
        var k = new Katalogstand { Gefuellt = true };
        var cut = Aufbauen(k);
        Knopf(cut, R.KMA_BTN_HINZU).Click();
        Zeilenklick.Zeile(cut, 1);
        Knopf(cut, R.KMA_BTN_UEBERNEHMEN).Click();          // neue Anlage im Arbeitsstand, noch ohne OK

        Knopf(cut, R.KMA_BTN_HINZU).Click();
        Knopf(cut, R.KM_ANL_BTN_VERWALTUNG).Click();

        Assert.Empty(Navigation.Masken);
        Assert.True(cut.Instance.KatalogOffen);
        Assert.Contains(R.KM_ANL_MSG_UNGESPEICHERT, cut.Markup);
    }

    [Fact]
    public void Gefuellter_Katalog_zeigt_die_Knoepfe_als_Nebenknoepfe_unter_der_Liste()
    {
        var cut = Aufbauen(new Katalogstand { Gefuellt = true });
        Knopf(cut, R.KMA_BTN_HINZU).Click();

        var wahl = cut.Find(".epos-kaeltemaschine-katalogwahl");
        var kinder = wahl.Children.ToList();
        int liste = kinder.FindIndex(e => e.QuerySelector(".epos-katalogliste") is not null
                                          || e.ClassList.Contains("epos-katalogliste"));
        int wege = kinder.FindIndex(e => e.ClassList.Contains("epos-kaeltemaschine-katalogwege"));
        Assert.True(liste >= 0 && wege > liste);
        Assert.Equal(new[] { R.KM_BTN_TYPKENNFELDER, R.KM_ANL_BTN_VERWALTUNG }, Katalogwege(cut));
        Assert.Empty(cut.FindAll(".epos-kaeltemaschine-katalogwege .epos-knopf--primaer"));
        Assert.DoesNotContain(R.KMA_KATALOG_LEER, cut.Markup);
    }

    [Fact]
    public void Ohne_Gabe_kein_Knopf_Typkennfelder_laden()
    {
        var cut = Aufbauen(new Katalogstand(), mitTypkennfeldern: false);
        Knopf(cut, R.KMA_BTN_HINZU).Click();

        Assert.Equal(new[] { R.KM_ANL_BTN_VERWALTUNG }, Katalogwege(cut));
    }
}
