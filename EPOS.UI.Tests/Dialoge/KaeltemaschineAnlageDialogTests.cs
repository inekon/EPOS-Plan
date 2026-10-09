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
