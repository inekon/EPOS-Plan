using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Import;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Der Gebäudeimport</b> (Stufe G4c, Welle 2; Softwarearchitektur 3.4, Datenaustauschkonzept
/// 2.4) — EINE Komponente für jedes Gebäudeformat, gefahren mit synthetischen Gaben: Die
/// Datenseite (Lesen, Zuordnen, Prüfen) steht hier als Delegat, wie die Hülle sie reicht.
///
/// <para>Geprüft wird: Feldbestand (Baualtersklasse als erstes Feld), Rückweg mit Ergebnis und
/// mit <c>null</c>, der abgehakte Haken, die Handänderung (Herkunft „manuell" aus den Gaben,
/// Markierung weg), der Klassenwechsel mit erhaltenen Handänderungen, der Raumhaken, die
/// Größenablehnung vor dem Lesen, die weiche Sperre ohne Schreibdelegat, das Zeichnen ohne
/// Gaben, zwei Profile mit derselben Komponente und die Wache gegen Formatnamen im Quelltext.</para>
/// </summary>
public class GebaeudeImportDialogTests : EposBunitContext
{
    public GebaeudeImportDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // =====================================================================
    //  Prüfstand — die Gaben, wie die Hülle sie reicht
    // =====================================================================

    private static readonly GebaeudeImportProfilDaten ProfilA =
        new("Format Alpha", "(*.alpha)|*.alpha", "25 MB", new[] { "Regel eins – eine Zone" }, "Form_Alpha.btn_Help");

    private static readonly GebaeudeImportProfilDaten ProfilB =
        new("Format Beta", "(*.beta)|*.beta", "10 MB", new[] { "Regel zwei" }, "Form_Beta.btn_Help");

    private static readonly IReadOnlyList<string> Klassen =
        Enumerable.Range(0, 21).Select(i => "Klasse " + (char)('A' + i)).ToList();

    /// <summary>Was die Delegaten gesehen haben.</summary>
    private sealed class Protokoll
    {
        public List<string> Filter { get; } = new();
        public List<string> Gelesen { get; } = new();
        public List<GebaeudeZuordnungsanfrage> Anfragen { get; } = new();
        public List<GebaeudeImportErgebnis> Geprueft { get; } = new();
        public List<GebaeudeImportErgebnis> Uebernommen { get; } = new();
        public List<GebaeudeImportErgebnis?> Geschlossen { get; } = new();
    }

    /// <summary>Ein Stand wie aus dem Kern: sechs Zeilen, zwei Räume, eine Meldung — die Vorgabe hängt an der Klasse.</summary>
    private static GebaeudeImportStand Stand(GebaeudeZuordnungsanfrage a)
    {
        bool zweiBeheizt = a.BeheiztUebersteuert.TryGetValue("r-2", out bool b) && b;
        string vorgabe = a.Baualtersklasse is int k ? "Vorgabe-" + (char)('A' + k) : "";
        return new GebaeudeImportStand
        {
            Kopftext = "Kopf-Probe",
            Vorschlagsname = "Haus Probe " + a.Gebaeudeindex,
            ManuellHerkunftText = "Hand-Probe",
            Raeume = new[]
            {
                new GebaeudeRaumzeileDaten("r-1", "Wohnen", "50 m²", true, true, "Grund-1", false),
                new GebaeudeRaumzeileDaten("r-2", "Keller", "30 m²", zweiBeheizt, false, zweiBeheizt ? "umgestellt" : "Grund-2", zweiBeheizt),
            },
            Zeilen = new[]
            {
                Zeile("NUTZ", "Kenngrößen", "Nutzfläche", zweiBeheizt ? 80 : 50, "m²", haken: true, eingebbar: true),
                Zeile("BAUART", "Kenngrößen", "Bauart", null, "", haken: true, eingebbar: false, text: "Schwere Probe"),
                Zeile("UAW", "Außenwand", "U-Wert Außenwand", 0.4, "W/(m²K)", haken: true, eingebbar: true) with { Vorgabe = vorgabe },
                Zeile("FGES", "Fenster", "Gesamte Fensterfläche", 23, "m²", haken: false, eingebbar: false, setzbar: false),
                Zeile("FNORD", "Fenster", "Fensterfläche Nord", 4, "m²", haken: true, eingebbar: true) with { Markierung = GebaeudeZeilenmarkierung.Gelb },
                Zeile("AWFL", "Außenwand", "Fläche Außenwand", 0, "m²", haken: true, eingebbar: true) with
                {
                    Markierung = GebaeudeZeilenmarkierung.Rot, Beleg = "Beleg-rot",
                },
            },
            Meldungen = new[] { new GebaeudeImportMeldung(WarnStufe.Warnung, "Warnung", "Meldung-Probe", "IMP_X") },
        };
    }

    private static GebaeudeFeldzeileDaten Zeile(string ziel, string gruppe, string feld, double? wert, string einheit,
                                                bool haken, bool eingebbar, bool setzbar = true, string? text = null)
        => new()
        {
            Zielfeld = ziel, Gruppe = gruppe, Feld = feld, Wert = wert, Einheit = einheit,
            Textwert = text is null ? null : "STEUERWERT", WertText = text ?? (wert?.ToString(CultureInfo.CurrentCulture) ?? "—"),
            Beleg = "Beleg-" + ziel, HerkunftText = "HK-Datei", HerkunftSchluessel = "DATEI",
            Haken = haken, Eingebbar = eingebbar, HakenSetzbar = setzbar,
        };

    private static GebaeudeLesestand Gelesen(params string[] gebaeude)
        => new(true, new GebaeudeImportKopf("haus.alpha", "Format Alpha", "Schema 1", "20,1 KB", "Regel eins – eine Zone"),
               gebaeude.Length == 0 ? new[] { "Haus 1" } : gebaeude, Array.Empty<GebaeudeImportMeldung>());

    private IRenderedComponent<GebaeudeImportDialog> Bauen(
        Protokoll p,
        GebaeudeImportProfilDaten? profil = null,
        Func<string, GebaeudeDateiwahl?>? wahl = null,
        GebaeudeLesestand? lesestand = null,
        Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>? pruefen = null,
        Func<GebaeudeImportErgebnis, string?>? uebernehmen = null,
        bool ohneUebernehmen = false)
    {
        GebaeudeImportProfilDaten pr = profil ?? ProfilA;
        return Render<GebaeudeImportDialog>(c =>
        {
            c.Add(x => x.Profil, pr);
            c.Add(x => x.Baualtersklassen, Klassen);
            c.Add(x => x.DateiWaehlen, (Func<string, Task<GebaeudeDateiwahl?>>)(f =>
            {
                p.Filter.Add(f);
                return Task.FromResult(wahl is null ? new GebaeudeDateiwahl("C:/ablage/haus.alpha", "haus.alpha", 20555) : wahl(f));
            }));
            c.Add(x => x.Lesen, (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)((pfad, melder, _) =>
            {
                p.Gelesen.Add(pfad);
                melder.Report(new GebaeudeImportFortschritt(0.5, "Liest …"));
                return Task.FromResult(lesestand ?? Gelesen());
            }));
            c.Add(x => x.Zuordnen, (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)(a => { p.Anfragen.Add(a); return Stand(a); }));
            c.Add(x => x.Pruefen, (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)(e =>
            {
                p.Geprueft.Add(e);
                return pruefen?.Invoke(e) ?? Array.Empty<GebaeudeImportMeldung>();
            }));
            if (!ohneUebernehmen)
                c.Add(x => x.Uebernehmen, (Func<GebaeudeImportErgebnis, Task<string?>>)(e =>
                {
                    p.Uebernommen.Add(e);
                    return Task.FromResult(uebernehmen?.Invoke(e));
                }));
            c.Add(x => x.Geschlossen, EventCallback.Factory.Create<GebaeudeImportErgebnis?>(this, e => p.Geschlossen.Add(e)));
        });
    }

    /// <summary>Klickt „Datei wählen…" und wartet, bis die Zuordnung gezeichnet ist.</summary>
    private static void Einlesen(IRenderedComponent<GebaeudeImportDialog> cut)
    {
        cut.FindAll("button").First(k => k.TextContent.Contains("Datei wählen")).Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-gebimport-zeilen tbody tr")));
    }

    private static IElement ZeileVon(IRenderedComponent<GebaeudeImportDialog> cut, string zielfeld)
        => cut.Find(".epos-gebimport-zeilen tr[data-zielfeld=\"" + zielfeld + "\"]");

    private static void Ok(IRenderedComponent<GebaeudeImportDialog> cut)
        => cut.Find(".epos-leiste button.epos-knopf--primaer").Click();

    private static void Abbrechen(IRenderedComponent<GebaeudeImportDialog> cut)
        => cut.FindAll(".epos-leiste button").First(k => k.TextContent == "Abbrechen").Click();

    // =====================================================================
    //  Feldbestand und Zeichnen ohne Gaben
    // =====================================================================

    [Fact]
    public void Feldbestand_die_Baualtersklasse_ist_das_erste_Feld()
    {
        var p = new Protokoll();
        var cut = Bauen(p);

        Assert.Equal("Gebäude importieren", cut.Find("h1.epos-dialog-titel").TextContent);
        Assert.Single(cut.FindAll(".epos-dialog-kopf button.epos-dialog-zu"));
        IElement erstesFeld = cut.FindAll(".epos-feld")[0];
        Assert.Contains("Baualtersklasse", erstesFeld.TextContent);
        Assert.Equal(22, erstesFeld.QuerySelectorAll("option").Length);   // „— keine —" und 21 Klassen
        Assert.Contains("Größte Datei: 25 MB", cut.Markup);
        Assert.Contains("Noch keine Datei gelesen", cut.Markup);

        Einlesen(cut);

        Assert.Equal(new[] { "(*.alpha)|*.alpha" }, p.Filter);
        Assert.Equal(new[] { "C:/ablage/haus.alpha" }, p.Gelesen);
        Assert.Contains("Format Alpha", cut.Find("dl.epos-gebimport-kopf").TextContent);
        Assert.Contains("Schema 1", cut.Find("dl.epos-gebimport-kopf").TextContent);
        Assert.Contains(cut.FindAll("input"), i => i.GetAttribute("value") == "Haus Probe 0");   // Name aus der Datei
        Assert.Equal(2, cut.FindAll(".epos-gebimport-raeume tbody tr").Count);
        Assert.Equal(6, cut.FindAll(".epos-gebimport-zeilen tbody tr").Count);
        Assert.Contains("Kopf-Probe", cut.Markup);

        // Eingebbar: Zahlenfeld; nicht eingebbar: Anzeigetext; nicht setzbar: Haken gesperrt.
        Assert.NotNull(ZeileVon(cut, "NUTZ").QuerySelector("input.epos-eingabe"));
        Assert.Null(ZeileVon(cut, "BAUART").QuerySelector("input.epos-eingabe"));
        Assert.Contains("Schwere Probe", ZeileVon(cut, "BAUART").TextContent);
        Assert.True(ZeileVon(cut, "FGES").QuerySelector("input[type=checkbox]")!.HasAttribute("disabled"));
        Assert.Contains("epos-gebimport-zeile--gelb", ZeileVon(cut, "FNORD").ClassName);
        Assert.Contains("epos-gebimport-zeile--rot", ZeileVon(cut, "AWFL").ClassName);
        Assert.Contains("Außenwand", ZeileVon(cut, "UAW").TextContent);   // Gruppenkopf der Gruppe

        IElement meldung = Assert.Single(cut.FindAll(".epos-gebimport-meldungen tbody tr"));
        Assert.Contains("epos-gebimport-meldung--warnung", meldung.ClassName);
        Assert.Contains("Meldung-Probe", meldung.TextContent);

        Assert.Equal(2, cut.FindAll(".epos-leiste button").Count);   // Abbrechen, OK
        Assert.Null(cut.Find(".epos-leiste button.epos-knopf--primaer").GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog_und_OK_ist_weich_gesperrt()
    {
        var geschlossen = new List<GebaeudeImportErgebnis?>();
        var cut = Render<GebaeudeImportDialog>(c => c
            .Add(x => x.Geschlossen, EventCallback.Factory.Create<GebaeudeImportErgebnis?>(this, e => geschlossen.Add(e))));

        Assert.Equal("Gebäude importieren", cut.Find("h1").TextContent);
        Assert.Contains("Noch keine Datei gelesen", cut.Markup);
        Assert.DoesNotContain(cut.FindAll("button"), k => k.TextContent.Contains("Datei wählen"));   // kein Delegat, kein Knopf
        Assert.Empty(cut.FindAll(".epos-gebimport-zeilen"));

        IElement ok = cut.Find(".epos-leiste button.epos-knopf--primaer");
        Assert.Equal("true", ok.GetAttribute("aria-disabled"));
        Assert.Equal("Übernehmen geht erst, wenn eine Gebäudedatei gelesen ist.", ok.GetAttribute("title"));
        ok.Click();
        cut.WaitForAssertion(() => Assert.Equal("Übernehmen geht erst, wenn eine Gebäudedatei gelesen ist.", cut.Instance.Meldung));
        Assert.Empty(geschlossen);

        Abbrechen(cut);
        cut.WaitForAssertion(() => Assert.Equal(new GebaeudeImportErgebnis?[] { null }, geschlossen));
    }

    // =====================================================================
    //  Rückweg
    // =====================================================================

    [Fact]
    public void OK_prueft_uebernimmt_und_gibt_alle_Zeilen_zurueck()
    {
        var p = new Protokoll();
        var cut = Bauen(p);
        Einlesen(cut);

        Ok(cut);

        cut.WaitForAssertion(() => Assert.Single(p.Geschlossen));
        GebaeudeImportErgebnis e = Assert.Single(p.Uebernommen);
        Assert.Same(e, p.Geschlossen[0]);
        Assert.Same(e, Assert.Single(p.Geprueft));
        Assert.Equal(6, e.Zeilen.Count);                 // ALLE Zeilen, auch die unveränderten
        Assert.Equal("Haus Probe 0", e.Gebaeudename);
        Assert.Equal(0, e.Gebaeudeindex);
        Assert.Null(e.Baualtersklasse);
        Assert.Empty(e.BeheiztUebersteuert);
        Assert.True(e.Zeile("NUTZ")!.Haken);
        Assert.False(e.Zeile("FGES")!.Haken);
        Assert.Equal("STEUERWERT", e.Zeile("BAUART")!.Textwert);
        Assert.Equal("DATEI", e.Zeile("UAW")!.HerkunftSchluessel);
    }

    [Theory]
    [InlineData("abbrechen")]
    [InlineData("kreuz")]
    [InlineData("esc")]
    public void Abbrechen_Kreuz_und_Esc_liefern_null_und_uebernehmen_nie(string weg)
    {
        var p = new Protokoll();
        var cut = Bauen(p);
        Einlesen(cut);

        switch (weg)
        {
            case "abbrechen": Abbrechen(cut); break;
            case "kreuz": cut.Find("button.epos-dialog-zu").Click(); break;
            default: cut.Find(".epos-gebimport").KeyDown(new KeyboardEventArgs { Key = "Escape" }); break;
        }

        cut.WaitForAssertion(() => Assert.Equal(new GebaeudeImportErgebnis?[] { null }, p.Geschlossen));
        Assert.Empty(p.Uebernommen);
        Assert.Empty(p.Geprueft);
    }

    [Fact]
    public void Ein_abgehakter_Haken_haelt_die_Zeile_aus_der_Uebernahme()
    {
        var p = new Protokoll();
        var cut = Bauen(p);
        Einlesen(cut);

        ZeileVon(cut, "UAW").QuerySelector("input[type=checkbox]")!.Change(false);
        Ok(cut);

        cut.WaitForAssertion(() => Assert.Single(p.Geschlossen));
        GebaeudeFeldzeileDaten uaw = p.Geschlossen[0]!.Zeile("UAW")!;
        Assert.False(uaw.Haken);
        Assert.Equal(0.4, uaw.Wert);
        Assert.Equal("DATEI", uaw.HerkunftSchluessel);   // nur der Haken, die Herkunft bleibt
    }

    [Fact]
    public void Die_Handaenderung_macht_die_Herkunft_manuell_und_loescht_die_Markierung()
    {
        var p = new Protokoll();
        var cut = Bauen(p);
        Einlesen(cut);
        Assert.Contains("HK-Datei", ZeileVon(cut, "AWFL").TextContent);   // Herkunftstext aus den Gaben

        ZeileVon(cut, "AWFL").QuerySelector("input.epos-eingabe")!.Input("12,5");

        cut.WaitForAssertion(() => Assert.DoesNotContain("epos-gebimport-zeile--rot", ZeileVon(cut, "AWFL").ClassName ?? ""));
        IElement zeile = ZeileVon(cut, "AWFL");
        Assert.Contains("Hand-Probe", zeile.TextContent);                  // auch „manuell" kommt aus den Gaben
        Assert.DoesNotContain("Beleg-rot", zeile.TextContent);
        Assert.NotNull(zeile.QuerySelector(".epos-gebimport-herkunft--manuell"));

        Ok(cut);
        cut.WaitForAssertion(() => Assert.Single(p.Geschlossen));
        GebaeudeFeldzeileDaten awfl = p.Geschlossen[0]!.Zeile("AWFL")!;
        Assert.Equal(12.5, awfl.Wert);
        Assert.Equal(GebaeudeHerkunftSchluessel.Manuell, awfl.HerkunftSchluessel);
        Assert.Equal(GebaeudeZeilenmarkierung.Keine, awfl.Markierung);
        Assert.True(awfl.Haken);
    }

    [Fact]
    public void Der_Klassenwechsel_ordnet_neu_zu_und_behaelt_die_Handaenderungen()
    {
        var p = new Protokoll();
        var cut = Bauen(p);
        Einlesen(cut);
        Assert.Single(p.Anfragen);

        ZeileVon(cut, "NUTZ").QuerySelector("input.epos-eingabe")!.Input("77");
        ZeileVon(cut, "FNORD").QuerySelector("input[type=checkbox]")!.Change(false);
        cut.FindAll(".epos-feld")[0].QuerySelector("select")!.Change("4");   // Klasse E

        cut.WaitForAssertion(() => Assert.Equal(2, p.Anfragen.Count));
        Assert.Equal(4, p.Anfragen[1].Baualtersklasse);
        cut.WaitForAssertion(() => Assert.Contains("Vorgabe-E", ZeileVon(cut, "UAW").TextContent));

        GebaeudeFeldzeileDaten nutz = cut.Instance.Zeilen.Single(z => z.Zielfeld == "NUTZ");
        Assert.Equal(77, nutz.Wert);
        Assert.Equal(GebaeudeHerkunftSchluessel.Manuell, nutz.HerkunftSchluessel);
        Assert.False(cut.Instance.Zeilen.Single(z => z.Zielfeld == "FNORD").Haken);
        Assert.Equal("77", ZeileVon(cut, "NUTZ").QuerySelector("input.epos-eingabe")!.GetAttribute("value"));

        Ok(cut);
        cut.WaitForAssertion(() => Assert.Single(p.Geschlossen));
        Assert.Equal(4, p.Geschlossen[0]!.Baualtersklasse);
    }

    [Fact]
    public void Der_Raumhaken_ordnet_mit_der_Uebersteuerung_neu_zu()
    {
        var p = new Protokoll();
        var cut = Bauen(p);
        Einlesen(cut);

        cut.FindAll(".epos-gebimport-raeume tbody tr")[1].QuerySelector("input[type=checkbox]")!.Change(true);

        cut.WaitForAssertion(() => Assert.Equal(2, p.Anfragen.Count));
        Assert.Equal(new KeyValuePair<string, bool>("r-2", true), Assert.Single(p.Anfragen[1].BeheiztUebersteuert));
        cut.WaitForAssertion(() => Assert.Contains("epos-gebimport-raum--umgestellt",
                                                   cut.FindAll(".epos-gebimport-raeume tbody tr")[1].ClassName ?? ""));
        Assert.Equal(80, cut.Instance.Zeilen.Single(z => z.Zielfeld == "NUTZ").Wert);

        // Zurück auf den Stand der Datei: keine Übersteuerung mehr.
        cut.FindAll(".epos-gebimport-raeume tbody tr")[1].QuerySelector("input[type=checkbox]")!.Change(false);
        cut.WaitForAssertion(() => Assert.Equal(3, p.Anfragen.Count));
        Assert.Empty(p.Anfragen[2].BeheiztUebersteuert);
    }

    [Fact]
    public void Die_Gebaeudewahl_steht_nur_bei_mehreren_Gebaeuden_und_wechselt_den_Kontext()
    {
        var p = new Protokoll();
        var cut = Bauen(p);
        Einlesen(cut);
        Assert.DoesNotContain(cut.FindAll(".epos-feld"), f => f.TextContent.Contains("Gebäude der Datei"));

        var p2 = new Protokoll();
        var cut2 = Bauen(p2, lesestand: Gelesen("Haus 1", "Haus 2"));
        Einlesen(cut2);
        ZeileVon(cut2, "NUTZ").QuerySelector("input.epos-eingabe")!.Input("99");

        IElement wahl = cut2.FindAll(".epos-feld").Single(f => f.TextContent.Contains("Gebäude der Datei"));
        wahl.QuerySelector("select")!.Change("1");

        cut2.WaitForAssertion(() => Assert.Equal(1, p2.Anfragen.Last().Gebaeudeindex));
        // Ein anderes Gebäude ist ein Kontextwechsel: die Handänderung fällt, der Name folgt.
        cut2.WaitForAssertion(() => Assert.Equal(50, cut2.Instance.Zeilen.Single(z => z.Zielfeld == "NUTZ").Wert));
        Assert.Contains(cut2.FindAll("input"), i => i.GetAttribute("value") == "Haus Probe 1");
    }

    // =====================================================================
    //  Größenablehnung, Prüfung, Schreibweg
    // =====================================================================

    [Fact]
    public void Die_Groessenablehnung_steht_vor_dem_Lesen()
    {
        var p = new Protokoll();
        var cut = Bauen(p, wahl: _ => new GebaeudeDateiwahl("C:/ablage/gross.alpha", "gross.alpha", 99_000_000,
                                                            "Die Datei ist zu groß — sie wird nicht gelesen."));

        cut.FindAll("button").First(k => k.TextContent.Contains("Datei wählen")).Click();

        cut.WaitForAssertion(() => Assert.Contains("zu groß", cut.Find(".epos-warnbanner").TextContent));
        Assert.Contains("epos-warnbanner--fehler", cut.Find(".epos-warnbanner").ClassName);
        Assert.Equal(WarnStufe.Fehler, cut.Instance.MeldungStufe);
        Assert.Empty(p.Gelesen);
        Assert.Empty(p.Anfragen);
        Assert.Empty(cut.FindAll(".epos-gebimport-zeilen"));
    }

    [Fact]
    public void Ein_Pruefungsfehler_haelt_den_Dialog_offen()
    {
        var p = new Protokoll();
        var cut = Bauen(p, pruefen: _ => new[]
        {
            new GebaeudeImportMeldung(WarnStufe.Warnung, "Warnung", "nur ein Hinweis"),
            new GebaeudeImportMeldung(WarnStufe.Fehler, "Fehler", "Raumhöhe fehlt"),
        });
        Einlesen(cut);

        Ok(cut);

        cut.WaitForAssertion(() => Assert.Contains("Raumhöhe fehlt", cut.Instance.Meldung));
        Assert.StartsWith("Nicht übernommen", cut.Instance.Meldung);
        Assert.DoesNotContain("nur ein Hinweis", cut.Instance.Meldung);
        Assert.Single(p.Geprueft);
        Assert.Empty(p.Uebernommen);
        Assert.Empty(p.Geschlossen);
    }

    [Fact]
    public void Ein_Schreibfehler_haelt_den_Dialog_offen()
    {
        var p = new Protokoll();
        var cut = Bauen(p, uebernehmen: _ => "Schreiben gescheitert");
        Einlesen(cut);

        Ok(cut);

        cut.WaitForAssertion(() => Assert.Equal("Schreiben gescheitert", cut.Instance.Meldung));
        Assert.Single(p.Uebernommen);
        Assert.Empty(p.Geschlossen);
    }

    [Fact]
    public void Ohne_Schreibdelegat_ist_OK_weich_gesperrt_und_meldet_den_Versuch()
    {
        var p = new Protokoll();
        var cut = Bauen(p, ohneUebernehmen: true);
        Einlesen(cut);

        IElement ok = cut.Find(".epos-leiste button.epos-knopf--primaer");
        Assert.Equal("true", ok.GetAttribute("aria-disabled"));
        Assert.False(ok.HasAttribute("disabled"));
        Assert.Equal("Die Übernahme in den Gebäudekatalog ist in dieser Fassung noch nicht angebunden.", ok.GetAttribute("title"));

        ok.Click();

        cut.WaitForAssertion(() => Assert.Equal(ok.GetAttribute("title"), cut.Instance.Meldung));
        Assert.Empty(p.Geprueft);
        Assert.Empty(p.Geschlossen);
    }

    [Fact]
    public void Esc_waehrend_des_Lesens_bricht_den_Lauf_ab_statt_zu_schliessen()
    {
        var geschlossen = new List<GebaeudeImportErgebnis?>();
        CancellationToken? marke = null;
        var cut = Render<GebaeudeImportDialog>(c => c
            .Add(x => x.Profil, ProfilA)
            .Add(x => x.Baualtersklassen, Klassen)
            .Add(x => x.DateiWaehlen, (Func<string, Task<GebaeudeDateiwahl?>>)(_ =>
                Task.FromResult<GebaeudeDateiwahl?>(new GebaeudeDateiwahl("C:/a.alpha", "a.alpha", 1))))
            .Add(x => x.Lesen, (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)(
                async (_, __, abbruch) => { marke = abbruch; await Task.Delay(Timeout.Infinite, abbruch); return Gelesen(); }))
            .Add(x => x.Zuordnen, (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)Stand)
            .Add(x => x.Geschlossen, EventCallback.Factory.Create<GebaeudeImportErgebnis?>(this, e => geschlossen.Add(e))));

        cut.FindAll("button").First(k => k.TextContent.Contains("Datei wählen")).Click();
        cut.WaitForAssertion(() => Assert.NotNull(marke));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("button.epos-dialog-zu")));   // während des Laufs kein Kreuz

        cut.Find(".epos-gebimport").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        cut.WaitForAssertion(() => Assert.True(marke!.Value.IsCancellationRequested));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("button.epos-dialog-zu")));
        Assert.Empty(geschlossen);
        Assert.Empty(cut.FindAll(".epos-gebimport-zeilen"));
    }

    // =====================================================================
    //  Zwei Profile, eine Komponente — und kein Formatname im Quelltext
    // =====================================================================

    [Theory]
    [InlineData("A")]
    [InlineData("B")]
    public void Derselbe_Dialog_traegt_zwei_Profile(string welches)
    {
        GebaeudeImportProfilDaten profil = welches == "A" ? ProfilA : ProfilB;
        var p = new Protokoll();
        var cut = Bauen(p, profil: profil);

        Assert.Contains("Größte Datei: " + profil.Groessengrenze, cut.Markup);
        Assert.Equal(profil.HilfeSchluessel, cut.FindComponent<InfoKnopf>().Instance.Schluessel);

        Einlesen(cut);
        Assert.Equal(new[] { profil.Dateifilter }, p.Filter);
    }

    [Fact]
    public void Im_Quelltext_der_Komponente_steht_kein_Formatname()
    {
        var funde = new List<string>();
        foreach (string datei in new[] { "GebaeudeImportDialog.razor", "GebaeudeImportDaten.cs" })
        {
            string text = File.ReadAllText(Path.Combine(Wurzel(), "EPOS.UI", "Dialoge", "Import", datei));
            Assert.True(text.Length > 1000, datei + " ist nicht gelesen worden.");
            foreach (string name in new[] { "gbXML", "GBXML", "IFC", "Ifc" })
                if (text.Contains(name, StringComparison.Ordinal)) funde.Add(datei + ": " + name);
        }
        Assert.True(funde.Count == 0, "Formatnamen im Quelltext der Komponente:\n" + string.Join("\n", funde));
    }

    private static string Wurzel()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;
        Assert.NotNull(d);
        return d!.FullName;
    }
}
