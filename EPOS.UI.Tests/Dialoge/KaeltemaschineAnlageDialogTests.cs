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
/// <b>Der Erzeugerdialog „Kältemaschinen im Projekt"</b> als Wirt der Zweispaltenauswahl (Katalogauswahl V1,
/// Stufe 5) gegen einen Stub der Hülle: eine gespeicherte Anlage, zwei Stammgeräte, die Prüfregeln des Kerns
/// (<c>KaeltemaschineAnlageHuelle.Pruefen</c>, ohne Datenbank) und Schreibwege als Listen. „In das Projekt
/// übernehmen" legt sofort an, Abbrechen räumt ab; die Satz-Überlagerung schreibt mit ihrem OK.
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
                AnlagenId = 7, GeraetId = 70, Bezeichner = "KM Bestand", Anzahl = 2, KuehlCarrierId = STROM,
                Geraet = new KaeltemaschineGeraetwerte
                {
                    Bezeichner = "Kältemaschine 100 kW", Firma = "Neutral", Rueckkuehlart = R.KM_RUECKKUEHLART_LUFT,
                    Nennkaelteleistung = 100, NennEer = 3.2, KaltwasserMin = 5, Saisonkennzahl = 5.1, SaisonkennzahlArt = "SEER",
                    Kaltwasserstuetzstellen = new[] { 6.0, 12.0, 18.0 }
                }
            }
        };
        internal readonly List<(int Stamm, string Name)> Angelegt = new();
        internal readonly List<KaeltemaschineAnlageDaten> Gespeichert = new();
        internal readonly List<int> Geloescht = new();
        internal readonly List<int> KatalogGeloescht = new();
        internal bool Gesperrt;
        internal string? Wurf;            // Anlegen, Satzlesen, Speichern und Katalogloeschen werfen mit diesem Text
        internal bool? Geschlossen;

        internal IReadOnlyList<Katalogfilterzeile> Katalog() => new[]
        {
            Zeile(11, "Kältemaschine 50 kW", 50, 3.0, Gesperrt),
            Zeile(12, "Kältemaschine 200 kW", 200, 4.1, false)
        };

        private static Katalogfilterzeile Zeile(int id, string name, double kw, double eer, bool gesperrt)
            => new Katalogfilterzeile(id, name) { Schluessel = id.ToString(System.Globalization.CultureInfo.InvariantCulture), Geschuetzt = gesperrt }
                   .MitText(Katalogfilterprofil.SpBezeichner, name)
                   .MitText(Katalogfilterprofil.SpHersteller, "Neutral")
                   .MitZahl(Katalogfilterprofil.SpNennkaelteleistung, kw, 1)
                   .MitZahl(Katalogfilterprofil.SpEer, eer, 2)
                   .MitText(Katalogfilterprofil.SpRueckkuehlart, R.KM_RUECKKUEHLART_WASSER);

        internal KaeltemaschineGeraetwerte? Werte(int stamm) => Wurf is not null ? throw new InvalidOperationException(Wurf + " Satz") : stamm == 12
            ? new KaeltemaschineGeraetwerte { Bezeichner = "Kältemaschine 200 kW", Nennkaelteleistung = 200, NennEer = 4.1,
                                              Kaltwasserstuetzstellen = new[] { 7.0, 10.0 } }
            : null;

        /// <summary>Liest die Anlagen wie die Hülle: gespeicherte und die angelegten.</summary>
        internal IReadOnlyList<KaeltemaschineAnlageDaten> Lesen() => Gespeicherte.Select(a => a.Kopie()).ToList();

        internal int Anlegen(int stamm, string name)
        {
            if (Wurf is not null) throw new InvalidOperationException(Wurf + " Anlegen");
            Angelegt.Add((stamm, name));
            if (stamm != 12) return -1;
            Gespeicherte.Add(new KaeltemaschineAnlageDaten
            {
                AnlagenId = 99, GeraetId = 990, Bezeichner = name, Anzahl = 1, KuehlCarrierId = STROM, Geraet = Werte(12)
            });
            return 99;
        }

        internal string? Speichern(KaeltemaschineAnlageDaten d)
        {
            if (Wurf is not null) throw new InvalidOperationException(Wurf + " Speichern");
            string grund = KaeltemaschineAnlageHuelle.Pruefen(d);
            if (grund != null) return grund;
            Gespeichert.Add(d.Kopie());
            return null;
        }

        internal void Loeschen(int id)
        {
            Geloescht.Add(id);
            Gespeicherte.RemoveAll(a => a.AnlagenId == id);
        }
    }

    internal static IRenderedComponent<KaeltemaschineAnlageDialog> Aufbauen(BunitContext ctx, Projekt p,
        Action<ComponentParameterCollectionBuilder<KaeltemaschineAnlageDialog>>? mehr = null)
        => ctx.Render<KaeltemaschineAnlageDialog>(b =>
        {
            b.Add(x => x.Anlagen, p.Lesen)
             .Add(x => x.Katalogzeilen, p.Katalog)
             .Add(x => x.Katalogwerte, p.Werte)
             .Add(x => x.Stromtraeger, new[] { (STROM, "Strom"), (OEKOSTROM, "Ökostrom") })
             .Add(x => x.ProjektStromtraeger, STROM)
             .Add(x => x.Pruefen, KaeltemaschineAnlageHuelle.Pruefen)
             .Add(x => x.Anlegen, p.Anlegen)
             .Add(x => x.Speichern, p.Speichern)
             .Add(x => x.Loeschen, p.Loeschen)
             .Add(x => x.KatalogLoeschen, id => { if (p.Wurf is not null) throw new InvalidOperationException(p.Wurf + " Loeschen"); p.KatalogGeloescht.Add(id); return ""; })
             .Add(x => x.Geschlossen, ok => p.Geschlossen = ok);
            mehr?.Invoke(b);
        });

    private static IElement Fussknopf(IRenderedComponent<KaeltemaschineAnlageDialog> cut, string text)
        => cut.FindAll(".epos-kaeltemaschine-anlage > .epos-leiste button").First(b => b.TextContent.Trim() == text);

    internal static IElement Feld(IRenderedComponent<KaeltemaschineAnlageDialog> cut, string bezeichnung)
        => cut.FindAll("label").First(l => l.TextContent.Trim().StartsWith(bezeichnung, StringComparison.Ordinal))
              .QuerySelector("input, select")
           ?? cut.FindAll(".epos-feld").First(f => f.TextContent.Trim().StartsWith(bezeichnung, StringComparison.Ordinal))
                 .QuerySelector("input, select")!;

    private static void Uebernehmen(IRenderedComponent<KaeltemaschineAnlageDialog> cut)
        => cut.Find(".epos-zweispalten-knopf--uebernehmen").Click();

    private static void Entfernen(IRenderedComponent<KaeltemaschineAnlageDialog> cut)
        => cut.Find(".epos-zweispalten-knopf--entfernen").Click();

    /// <summary>Öffnet die Satz-Überlagerung über den Stift der Projektzeile <paramref name="i"/>.</summary>
    private static IElement ProjektStift(IRenderedComponent<KaeltemaschineAnlageDialog> cut, int i = 0)
    {
        cut.Find(".epos-zweispalten-bereich--projekt").QuerySelectorAll(".epos-zeilenstift")[i].Click();
        return cut.Find(".epos-satzueberlagerung-koerper");
    }

    private static Dictionary<string, string> Angaben(IRenderedComponent<KaeltemaschineAnlageDialog> cut)
        => cut.FindAll(".epos-satzzusammenfassung-angabe")
              .ToDictionary(a => a.QuerySelector("dt")!.TextContent, a => a.QuerySelector("dd")!.TextContent);

    [Fact]
    public void Der_Aufbau_ist_der_der_Erzeuger_Projekt_oben_Katalog_unten()
    {
        var cut = Aufbauen(this, new Projekt(), b => b.Add(x => x.Vergleichsparameter, _ => Array.Empty<Parameterwert>())
                                                         .Add(x => x.TypkennfelderLaden, () => new KaeltemaschineTypkennfelderErgebnis(true, 0, 0, "")));

        var p = cut.Find(".epos-zweispalten-bereich--projekt > .epos-zweispalten-kopfleiste");
        Assert.Contains(R.KMA_SUMME, p.TextContent);
        Assert.Contains("200", p.QuerySelector(".epos-zweispalten-summe")!.TextContent);       // 2 × 100 kW
        Assert.Null(p.QuerySelector(".epos-knopf--bearbeiten-projekt"));                       // AW3b: nur am Satz
        Assert.NotNull(p.QuerySelector(".epos-zweispalten-knopf--entfernen"));
        Assert.DoesNotContain(cut.FindAll("button"), k => k.TextContent.Contains("Datenbank übernehmen"));

        string zeile = cut.Find(".epos-zweispalten-bereich--projekt tbody tr").TextContent;
        foreach (string teil in new[] { "KM Bestand", "100", "3,2 / SEER 5,1", R.KM_RUECKKUEHLART_LUFT, "2", "Strom", R.ALLG_BTN_NEIN })
            Assert.Contains(teil, zeile);
        Assert.Equal(2, Zeilenklick.Zeilen(cut).Count);
        Assert.NotNull(cut.Find(".epos-zweispalten-bereich--katalog > .epos-zweispalten-kopfleiste .epos-zweispalten-knopf--uebernehmen"));

        Assert.Empty(cut.FindAll(".epos-zweispalten-fussleiste"));                               // AW3b: Knopfleiste am Satz
        string fuss = cut.Find(".epos-zweispalten-satzknoepfe").TextContent;
        foreach (string knopf in new[] { R.AUSWAHL_BTN_VERGLEICHEN, R.KMA_BTN_LOESCHEN, R.AUSWAHL_BTN_BEARBEITEN, R.KM_BTN_TYPKENNFELDER, R.KM_ANL_BTN_VERWALTUNG })
            Assert.Contains(knopf, fuss);
        Assert.Empty(cut.FindAll(".epos-ueberlagerung--offen .epos-katalogliste"));             // keine Katalogüberlagerung mehr
    }

    [Fact]
    public void In_das_Projekt_uebernehmen_legt_sofort_an_und_Abbrechen_raeumt_ab()
    {
        var p = new Projekt();
        var cut = Aufbauen(this, p);

        Zeilenklick.Zeile(cut, 1);
        Uebernehmen(cut);
        Assert.Equal(new[] { (12, "Kältemaschine 200 kW") }, p.Angelegt);                      // sofort angelegt
        Assert.Equal(new[] { "KM Bestand", "Kältemaschine 200 kW" }, cut.Instance.Namensliste);
        Assert.Equal(99, cut.Instance.Arbeitsstand!.AnlagenId);                                // gewählt, bearbeitbar
        Assert.Equal(new[] { 99 }, cut.Instance.Angelegte);
        Assert.Contains("400", cut.Find(".epos-zweispalten-summe").TextContent);

        Fussknopf(cut, R.ALLG_BTN_ABBRECHEN).Click();
        Assert.Equal(new[] { 99 }, p.Geloescht);                                               // Projektkopievormerkung
        Assert.False(p.Geschlossen);

        var q = new Projekt();
        var zwei = Aufbauen(this, q);
        Zeilenklick.Zeile(zwei, 1);
        Uebernehmen(zwei);
        Fussknopf(zwei, R.ALLG_BTN_OK).Click();
        Assert.Empty(q.Geloescht);
        Assert.True(q.Geschlossen);
    }

    [Fact]
    public void Ein_fehlender_Katalogsatz_wird_gemeldet_und_nichts_kommt_hinzu()
    {
        var p = new Projekt();
        var cut = Aufbauen(this, p);
        Zeilenklick.Zeile(cut, 0);
        Uebernehmen(cut);
        Assert.Contains(string.Format(R.KMA_MSG_ANLEGEN_FEHLT, "Kältemaschine 50 kW"), cut.Markup);
        Assert.Single(cut.Instance.Namensliste);
    }

    [Fact]
    public void Entfernen_merkt_vor_und_loescht_beim_OK_eine_neue_Anlage_geht_sofort()
    {
        var p = new Projekt();
        var cut = Aufbauen(this, p);
        Entfernen(cut);
        Assert.Empty(cut.Instance.Namensliste);
        Assert.Empty(p.Geloescht);                                                            // vorgemerkt
        Fussknopf(cut, R.ALLG_BTN_OK).Click();
        Assert.Equal(new[] { 7 }, p.Geloescht);

        var q = new Projekt();
        var zwei = Aufbauen(this, q);
        Zeilenklick.Zeile(zwei, 1);
        Uebernehmen(zwei);
        Entfernen(zwei);                                                                       // die neue, gewählte
        Assert.Equal(new[] { 99 }, q.Geloescht);
        Fussknopf(zwei, R.ALLG_BTN_ABBRECHEN).Click();
        Assert.Equal(new[] { 99 }, q.Geloescht);                                               // nichts doppelt
    }

    [Fact]
    public void Die_Zusammenfassung_nennt_Anlage_und_Katalogsatz()
    {
        var k = System.Globalization.CultureInfo.CurrentCulture;
        var cut = Aufbauen(this, new Projekt(), b => b.Add(x => x.Kostensumme, _ => (12345.0, 678.0)));
        cut.Find(".epos-zweispalten-satzzeile").Click();
        var a = Angaben(cut);
        Assert.Equal("100 kW", a[R.KM_LBL_NENNKAELTELEISTUNG]);
        Assert.Equal("200 kW", a[R.AUSWAHL_ZF_LEISTUNG_GESAMT]);
        Assert.Equal("3,2 / SEER 5,1", a[R.KMA_SP_EER]);
        Assert.Equal(R.KM_RUECKKUEHLART_LUFT, a[R.KM_LBL_RUECKKUEHLART]);
        Assert.Equal("2", a[R.AUSWAHL_ZF_ANZAHL]);
        Assert.Equal("Strom", a[R.KMA_LBL_TRAEGER]);
        Assert.Equal(R.ALLG_BTN_NEIN, a[R.KMA_LBL_ZAEHLER]);
        Assert.Equal(string.Format(k, R.AUSWAHL_ZF_EURO, 12345.0), a[R.AUSWAHL_ZF_INVEST]);
        Assert.Equal(string.Format(k, R.AUSWAHL_ZF_EURO_JAHR, 678.0), a[R.AUSWAHL_ZF_BETRIEB]);
        Assert.Empty(cut.FindAll(".epos-satzzusammenfassung " + "input"));                    // Felder nur in der Überlagerung

        Zeilenklick.Zeile(cut, 1);
        a = Angaben(cut);
        Assert.Equal(R.AUSWAHL_MARKE_KATALOGSATZ, cut.Find(".epos-zweispalten-satzzeile .epos-zweispalten-marke--satz").TextContent);
        Assert.Contains("Neutral", a.Values);
        Assert.Contains(a.Values, w => w.Contains("200"));
        Assert.Contains(R.KM_RUECKKUEHLART_WASSER, a.Values);
    }

    /// <summary>
    /// KT-8c: Wirft die Kostensumme, fehlt die Angabe in der Zusammenfassung, der Banner nennt den
    /// Grund (<c>WURZEL_SATZ_FEHLER</c>), und das Protokoll hat einen Vermerk — vorher verschluckte
    /// ein leerer <c>catch</c> die Ausnahme still.
    /// </summary>
    [Fact]
    public void KT8c_wirft_die_Kostensumme_nennt_der_Banner_den_Grund()
    {
        using var vermerke = new Ausnahmevermerke();
        var cut = Aufbauen(this, new Projekt(),
                           b => b.Add(x => x.Kostensumme, _ => throw new InvalidOperationException("Kosten gesperrt")));
        cut.Find(".epos-zweispalten-satzzeile").Click();

        Assert.Contains(cut.FindAll(".epos-warnbanner-text"), e => e.TextContent.Contains("Kosten gesperrt"));
        Assert.True(vermerke.Enthaelt("Kosten gesperrt"));
    }

    /// <summary>
    /// KT-8c: „Kosten…" läuft über die Fangstelle (<c>Katalogladung.SchreibenAsync</c>): Wirft der Weg,
    /// nennt der Banner den Grund, das Protokoll hat einen Vermerk, und die Überlagerung bleibt offen.
    /// </summary>
    [Fact]
    public void KT8c_wirft_der_Kostenweg_nennt_der_Banner_den_Grund_und_der_Dialog_bleibt_offen()
    {
        using var vermerke = new Ausnahmevermerke();
        var p = new Projekt();
        var cut = Aufbauen(this, p, b => b.Add(x => x.KostenOeffnen,
                                               (_, _) => throw new InvalidOperationException("Kostenverwaltung gesperrt")));
        IElement satz = ProjektStift(cut);

        satz.QuerySelectorAll(".epos-kostenleiste button.epos-knopf")[0].Click();

        cut.WaitForAssertion(() =>
            Assert.Contains(cut.FindAll(".epos-warnbanner-text"), e => e.TextContent.Contains("Kostenverwaltung gesperrt")));
        Assert.True(vermerke.Enthaelt("Kostenverwaltung gesperrt"));
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Null(p.Geschlossen);
    }

    [Fact]
    public void KT8c_ohne_Ausnahme_zeigt_der_Kostenweg_keinen_Banner()
    {
        using var vermerke = new Ausnahmevermerke();
        int geoeffnet = 0;
        var cut = Aufbauen(this, new Projekt(), b => b
            .Add(x => x.KostenOeffnen, (_, _) => { geoeffnet++; return Task.CompletedTask; })
            .Add(x => x.Kostensumme, _ => (1.0, 2.0)));
        IElement satz = ProjektStift(cut);

        satz.QuerySelectorAll(".epos-kostenleiste button.epos-knopf")[0].Click();

        Assert.Equal(1, geoeffnet);
        Assert.Empty(cut.FindAll(".epos-warnbanner-text"));
        Assert.True(vermerke.Leer);
    }

    [Fact]
    public void Der_Stift_oeffnet_die_Ueberlagerung_OK_schreibt_ueber_den_Fangweg()
    {
        var p = new Projekt();
        var cut = Aufbauen(this, p, b => b.Add(x => x.KostenOeffnen, (_, _) => Task.CompletedTask));
        IElement satz = ProjektStift(cut);
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.NotEmpty(satz.QuerySelectorAll(".epos-kostenleiste button.epos-knopf"));
        Assert.Contains(R.KMA_GRUPPE_GERAET, satz.TextContent);
        Feld(cut, R.KMA_LBL_ANZAHL).Input("3");
        Feld(cut, R.KMA_LBL_VORLAUF).Input("9");
        cut.Find(".epos-satzueberlagerung-ok").Click();

        Assert.False(cut.Instance.SatzUeberlagerungOffen);
        KaeltemaschineAnlageDaten d = p.Gespeichert.Single();
        Assert.Equal((7, 3, 9.0), (d.AnlagenId, d.Anzahl!.Value, d.KuehlVorlauf!.Value));
        Assert.Contains("300", cut.Find(".epos-zweispalten-summe").TextContent);

        Fussknopf(cut, R.ALLG_BTN_ABBRECHEN).Click();                                         // Stand beim Öffnen zurück
        Assert.Equal(2, p.Gespeichert.Count);
        Assert.Equal(2, p.Gespeichert[1].Anzahl);
        Assert.False(p.Geschlossen);
    }

    [Fact]
    public void Abbrechen_der_Ueberlagerung_stellt_die_Anlagenfelder_wieder_her()
    {
        var p = new Projekt();
        var cut = Aufbauen(this, p);
        ProjektStift(cut);
        Feld(cut, R.KMA_LBL_ANZAHL).Input("5");
        cut.Find(".epos-satzueberlagerung-abbrechen").Click();
        Assert.Equal(2, cut.Instance.Arbeitsstand!.Anzahl);
        Assert.Empty(p.Gespeichert);
        Fussknopf(cut, R.ALLG_BTN_OK).Click();                                                // nichts zu schreiben
        Assert.Empty(p.Gespeichert);
        Assert.True(p.Geschlossen);
    }

    [Fact]
    public void Die_Pruefregeln_halten_die_Ueberlagerung_offen_und_schreiben_nichts()
    {
        var p = new Projekt();
        var cut = Aufbauen(this, p);
        ProjektStift(cut);
        Feld(cut, R.KMA_LBL_ANZAHL).Input("0");
        cut.Find(".epos-satzueberlagerung-ok").Click();
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Empty(p.Gespeichert);

        Feld(cut, R.KMA_LBL_ANZAHL).Input("1");
        Feld(cut, R.KMA_LBL_HILFSSTROM).Input("100");
        cut.Find(".epos-satzueberlagerung-ok").Click();
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Empty(p.Gespeichert);
    }

    [Fact]
    public void Ein_Wurf_beim_Speichern_meldet_ueber_den_Fangweg()
    {
        var p = new Projekt();
        var cut = Aufbauen(this, p);
        ProjektStift(cut);
        p.Wurf = "Platte voll";
        Feld(cut, R.KMA_LBL_ANZAHL).Input("3");
        cut.Find(".epos-satzueberlagerung-ok").Click();
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Contains("Platte voll", cut.Markup);
    }

    [Fact]
    public void Ein_Wurf_beim_Anlegen_und_beim_Satzlesen_meldet_im_Banner()
    {
        var p = new Projekt();
        var cut = Aufbauen(this, p);
        p.Wurf = "kaputt";
        Zeilenklick.Zeile(cut, 1);
        Assert.Contains("kaputt Satz", cut.Markup);
        Uebernehmen(cut);
        Assert.Contains("kaputt Anlegen", cut.Markup);
        Assert.Single(cut.Instance.Namensliste);
    }

    [Fact]
    public void Der_gesperrte_Katalogsatz_oeffnet_nur_lesend_der_freie_den_Katalogeditor()
    {
        var p = new Projekt { Gesperrt = true };
        var cut = Aufbauen(this, p, b => b.Add(x => x.KatalogGaben, () => new Dictionary<string, object>
        {
            ["Katalogzeilen"] = new Func<IReadOnlyList<Katalogfilterzeile>>(p.Katalog)
        }));
        cut.FindAll(".epos-katalogliste .epos-zeilenstift")[0].Click();
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.False(cut.Instance.EditorOffen);
        Assert.Contains(R.ADM_SCHLOSS_ERST_AUFHEBEN, cut.Find(".epos-satzueberlagerung-koerper").TextContent);
        cut.FindAll(".epos-satzueberlagerung-abbrechen, .epos-satzueberlagerung-schliessen")[0].Click();

        cut.FindAll(".epos-katalogliste .epos-zeilenstift")[1].Click();
        Assert.True(cut.Instance.EditorOffen);
        Assert.Equal(12, cut.Instance.EditorVorwahl);
        Assert.NotNull(cut.Find(".epos-ueberlagerung .epos-kaeltemaschine-katalog, .epos-ueberlagerung [class*=kaeltemaschine]"));
    }

    [Fact]
    public void Loeschen_im_Katalog_fragt_zurueck_und_geht_ueber_den_Fangweg()
    {
        var p = new Projekt();
        var cut = Aufbauen(this, p);
        Zeilenklick.Zeile(cut, 1);
        cut.Find(".epos-zweispalten-satzknoepfe .epos-knopf--loeschen").Click();
        Assert.True(cut.Instance.Loeschfrage);
        Assert.Contains("„Kältemaschine 200 kW“", cut.Find(".epos-rueckfrage").TextContent);
        cut.Find(".epos-rueckfrage").QuerySelectorAll(".epos-knopf").First(b => b.TextContent.Trim() == R.ALLG_BTN_JA).Click();
        Assert.Equal(new[] { 12 }, p.KatalogGeloescht);

        var q = new Projekt();
        var zwei = Aufbauen(this, q);
        Zeilenklick.Zeile(zwei, 1);
        q.Wurf = "gesperrt im Kern";
        zwei.Find(".epos-zweispalten-satzknoepfe .epos-knopf--loeschen").Click();
        zwei.Find(".epos-rueckfrage").QuerySelectorAll(".epos-knopf").First(b => b.TextContent.Trim() == R.ALLG_BTN_JA).Click();
        Assert.Contains("gesperrt im Kern", zwei.Markup);
    }

    [Fact]
    public void Die_Teillast_Lesewerte_und_die_Folgeschaltung_stehen_in_der_Ueberlagerung()
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
        ProjektStift(cut);
        Assert.Equal(R.KM_TT_WEG_VORGABEKURVE, Feld(cut, R.KM_LBL_TEILLAST_WEG).GetAttribute("value"));
        Assert.Equal(R.KM_REGELUNG_STUFEN, Feld(cut, R.KM_LBL_VERDICHTERREGELUNG).GetAttribute("value"));
        Assert.Equal(R.KM_PH_CD_VORGABE, Feld(cut, R.KM_LBL_TAKTVERLUST_CD).GetAttribute("value"));
        Assert.Equal(R.KM_RANDWEG_GUETEGRAD, Feld(cut, R.KM_LBL_KENNFELD_RANDWEG).GetAttribute("value"));
        string folge = string.Format(System.Globalization.CultureInfo.CurrentCulture, R.KMA_HINWEIS_FOLGESCHALTUNG, 2);
        Assert.Contains(folge, cut.Markup);
        Feld(cut, R.KMA_LBL_ANZAHL).Input("1");
        Assert.DoesNotContain(folge, cut.Markup);
    }

    /// <summary>KM3-E3-b: die Hülle liest die Lesewerte über den Kern (Kaeltemaschinenteillast), wie der Lauf.</summary>
    [Fact]
    public void Die_Huelle_liest_Weg_mit_Herkunft_Regelung_und_Cd()
    {
        KaeltemaschineGeraetwerte w = KaeltemaschineAnlageHuelle.Werte(new KaeltemaschineModel
        {
            Teillast_Weg = KaeltemaschineTeillastSchema.WEG_KURVE, Verdichterregelung = KaeltemaschineTeillastSchema.REGELUNG_DREHZAHL,
            Saisonkennzahl = 5.5, Saisonkennzahl_Art = "SEER"
        });
        Assert.True(w.TeillastGesetzt);
        Assert.Equal(R.KM_TT_WEG_VORGABEKURVE, w.TeillastWeg);
        Assert.Equal(R.KM_REGELUNG_DREHZAHL, w.Verdichterregelung);
        Assert.Equal(R.KM_PH_CD_VORGABE, w.Taktverlustfaktor);
        Assert.Equal(R.KM_PH_RANDWEG_VORGABE, w.Kennfeldrand);
        Assert.Equal((5.5, "SEER"), (w.Saisonkennzahl!.Value, w.SaisonkennzahlArt));
        KaeltemaschineGeraetwerte b = KaeltemaschineAnlageHuelle.Werte(new KaeltemaschineModel());
        Assert.False(b.TeillastGesetzt);
        Assert.Equal(R.KM_TEILLAST_WEG_BESTAND, b.TeillastWeg);
        Assert.Equal("", b.SaisonkennzahlArt);
    }

    [Fact]
    public void Kein_Anzeigetext_ist_Steuerwert_der_Kuehltraeger_geht_ueber_die_Id()
    {
        var p = new Projekt();
        var cut = Aufbauen(this, p);
        ProjektStift(cut);
        IElement auswahl = Feld(cut, R.KMA_LBL_TRAEGER);
        Assert.Contains("Ökostrom", auswahl.TextContent);
        Assert.DoesNotContain(auswahl.QuerySelectorAll("option"), o => (o.GetAttribute("value") ?? "").Contains("strom", StringComparison.OrdinalIgnoreCase));
        auswahl.Change(OEKOSTROM.ToString(System.Globalization.CultureInfo.InvariantCulture));
        cut.Find(".epos-satzueberlagerung-ok").Click();
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

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(p.Geschlossen);
        Assert.Empty(p.Gespeichert);
    }

    [Fact]
    public void OK_schreibt_die_Eingaben_des_Assistenten()
    {
        var p = new Projekt();
        var cut = Aufbauen(this, p);
        cut.Instance.Arbeitsstand!.Anzahl = 3;
        Fussknopf(cut, R.ALLG_BTN_OK).Click();
        Assert.Equal(3, p.Gespeichert.Single().Anzahl);
        Assert.True(p.Geschlossen);
    }

    /// <summary>
    /// KM4-E3-a: Die Zeile „Zulässigkeit“ (E3-b, aus <c>KaeltemaschineKonfiguration</c>) steht im Wirt in der
    /// Satzüberlagerung genau einmal — der Anlagendialog baut keine zweite.
    /// </summary>
    [Fact]
    public void Die_Zulaessigkeitszeile_steht_im_Satz_genau_einmal()
    {
        const string ZEILE = "Zulässigkeit des Kältemittels: ohne Inbetriebnahmejahr nicht geprüft";
        var p = new Projekt();
        p.Gespeicherte[0].Zulaessigkeit = ZEILE;
        var cut = Aufbauen(this, p);

        IElement satz = ProjektStift(cut);
        Assert.Single(satz.QuerySelectorAll(".epos-herleitung"), h => h.TextContent.Trim() == ZEILE);
        Assert.Single(cut.FindAll(".epos-herleitung"), h => h.TextContent.Trim() == ZEILE);
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
        Assert.Contains("Chillers in the database:", cut.Markup);
        Assert.Contains("Total rated cooling capacity [kW]:", cut.Markup);
        cut.Find(".epos-zweispalten-bereich--projekt .epos-zeilenstift").Click();
        Assert.Contains("Chilled water supply", cut.Markup);
    }
}

/// <summary>
/// <b>Die Wege zum Katalog rechts im Katalogfuß</b>: „Typkennfelder laden…" über die Gabe der Hülle und
/// „Katalogverwaltung…" — mit Gaben des Katalogeditors als Überlagerung, sonst über die Naht
/// <c>Dienste.Navigation</c>. Der Fall tauscht <c>Dienste.Navigation</c> und steht deshalb in der seriellen Sammlung.
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

        internal IReadOnlyList<Katalogfilterzeile> Zeilen()
            => Gefuellt ? Projekt.Katalog() : Array.Empty<Katalogfilterzeile>();

        internal KaeltemaschineTypkennfelderErgebnis Laden()
        {
            Gefuellt = true;
            return new KaeltemaschineTypkennfelderErgebnis(true, 2, 0, "");
        }
    }

    private IRenderedComponent<KaeltemaschineAnlageDialog> Aufbauen(Katalogstand k, bool mitTypkennfeldern = true)
        => Render<KaeltemaschineAnlageDialog>(b =>
        {
            b.Add(x => x.Anlagen, k.Projekt.Lesen)
             .Add(x => x.Katalogzeilen, k.Zeilen)
             .Add(x => x.Katalogwerte, k.Projekt.Werte)
             .Add(x => x.Stromtraeger, new[] { (KaeltemaschineAnlageDialogTests.STROM, "Strom") })
             .Add(x => x.ProjektStromtraeger, KaeltemaschineAnlageDialogTests.STROM)
             .Add(x => x.Pruefen, KaeltemaschineAnlageHuelle.Pruefen)
             .Add(x => x.Anlegen, k.Projekt.Anlegen)
             .Add(x => x.Speichern, k.Projekt.Speichern)
             .Add(x => x.Loeschen, k.Projekt.Loeschen)
             .Add(x => x.Geschlossen, ok => k.Projekt.Geschlossen = ok);
            if (mitTypkennfeldern) b.Add(x => x.TypkennfelderLaden, k.Laden);
        });

    private static IElement Knopf(IRenderedComponent<KaeltemaschineAnlageDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static string[] Katalogwege(IRenderedComponent<KaeltemaschineAnlageDialog> cut)
        => cut.FindAll(".epos-kaeltemaschine-typkennfelder, .epos-kaeltemaschine-verwaltung").Select(b => b.TextContent.Trim()).ToArray();

    [Fact]
    public void Typkennfelder_laden_fuellt_die_Liste_ueber_die_Gabe_und_meldet_im_Banner()
    {
        var k = new Katalogstand();
        var cut = Aufbauen(k);
        Assert.Empty(Zeilenklick.Zeilen(cut));
        Knopf(cut, R.KM_BTN_TYPKENNFELDER).Click();
        Assert.Equal(2, Zeilenklick.Zeilen(cut).Count);
        Assert.Contains(string.Format(System.Globalization.CultureInfo.CurrentCulture, R.KM_MSG_TYPKENNFELDER, 2, 0), cut.Markup);
    }

    [Fact]
    public void Ein_gescheitertes_Laden_meldet_den_Grund_des_Kerns()
    {
        var k = new Katalogstand();
        var cut = Render<KaeltemaschineAnlageDialog>(b => b
            .Add(x => x.Anlagen, k.Projekt.Lesen)
            .Add(x => x.Katalogzeilen, k.Zeilen)
            .Add(x => x.TypkennfelderLaden, () => new KaeltemaschineTypkennfelderErgebnis(false, 0, 0, "Grund des Kerns")));
        Knopf(cut, R.KM_BTN_TYPKENNFELDER).Click();
        Assert.Contains("Grund des Kerns", cut.Markup);
    }

    [Fact]
    public void Katalogverwaltung_ohne_Editorgaben_ruft_die_Naht()
    {
        var cut = Aufbauen(new Katalogstand());
        Knopf(cut, R.KM_ANL_BTN_VERWALTUNG).Click();
        Assert.Equal(new[] { EPOS.UI.Seiten.Seitenschluessel.KaeltemaschineKatalog }, Navigation.Masken);
    }

    [Fact]
    public void Lehnt_die_Plattform_ab_nennt_es_der_Banner()
    {
        Navigation.Antwort = false;
        var cut = Aufbauen(new Katalogstand());
        Knopf(cut, R.KM_ANL_BTN_VERWALTUNG).Click();
        Assert.Single(Navigation.Masken);
        Assert.Contains(R.KM_ANL_MSG_VERWALTUNG_NICHT, cut.Markup);
    }

    [Fact]
    public void Ungespeicherte_Aenderungen_halten_die_Naht_an()
    {
        var k = new Katalogstand { Gefuellt = true };
        var cut = Aufbauen(k);
        Zeilenklick.Zeile(cut, 1);
        cut.Find(".epos-zweispalten-knopf--uebernehmen").Click();     // angelegt, Abbrechen nähme es zurück
        Knopf(cut, R.KM_ANL_BTN_VERWALTUNG).Click();
        Assert.Empty(Navigation.Masken);
        Assert.Contains(R.KM_ANL_MSG_UNGESPEICHERT, cut.Markup);
    }

    [Fact]
    public void Die_Wege_stehen_rechts_im_Katalogfuss_ohne_Gabe_kein_Typkennfelderknopf()
    {
        var cut = Aufbauen(new Katalogstand { Gefuellt = true });
        Assert.Equal(new[] { R.KM_BTN_TYPKENNFELDER, R.KM_ANL_BTN_VERWALTUNG }, Katalogwege(cut));
        Assert.Empty(cut.FindAll(".epos-kaeltemaschine-katalogwahl"));
        var ohne = Aufbauen(new Katalogstand(), mitTypkennfeldern: false);
        Assert.Equal(new[] { R.KM_ANL_BTN_VERWALTUNG }, Katalogwege(ohne));
    }
}
